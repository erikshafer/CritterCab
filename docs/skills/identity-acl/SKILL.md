---
name: identity-acl
description: "CritterCab's identity anti-corruption layer pattern per ADR-006. Covers the Identity bounded context as the single integration point with identity providers (Entra External ID in production, OpenIddict in demo mode, Keycloak as a local-dev alternative), standard OIDC JWT Bearer token validation at every service, domain events carrying domain identifiers rather than provider object IDs, Microsoft Graph change notification ingestion via ASB, per-service claims-to-domain translation, service-to-service auth with Managed Identity, the OpenIddict demo-mode bootstrap for contributor-friendly local development, and the dev-mode token flow for grpcurl and Evans testing."
cluster: identity
tags: [identity, acl, entra, openiddict, jwt-bearer, oidc, managed-identity, microsoft-graph, anti-corruption-layer, adr-006]
---

# Identity Anti-Corruption Layer

CritterCab's identity architecture has one governing rule: **the Identity bounded context is the single point of contact with the identity provider**. No other service subscribes to provider lifecycle webhooks, parses provider-specific JWT claims, or holds provider configuration. This rule is codified in ADR-006 and the structural constraints.

The Identity BC is intentionally thin. Its primary job is translation — converting provider-specific events (Entra External ID user registrations, Microsoft Graph change notifications) into domain events that other BCs consume (`RiderRegistered`, `DriverAccountCreated`). It is an anti-corruption layer in the DDD sense: a boundary that prevents an upstream system's model from leaking into the domain.

Token validation at each service is deliberately NOT part of the ACL. Every Cab service validates incoming JWT Bearer tokens against the provider's JWKS endpoint using standard OIDC middleware. This is a security boundary, not a domain coupling — the service extracts generic OIDC claims (`sub`, `email`, `roles`) and never touches provider-specific types. The distinction matters: swapping from Entra External ID to OpenIddict changes the Identity BC's translation logic and the JWKS endpoint URL, but every other service's token-validation code stays the same.

**Prerequisite packages not yet committed:** `Directory.Packages.props` does not include OpenIddict packages. When the Identity service is implemented, the specific OpenIddict packages and version must be added.

## When to apply this skill

**Use this skill when:**

- Implementing the Identity BC's provider integration (Entra External ID, OpenIddict, or Keycloak).
- Adding JWT Bearer token validation to a Cab service's `Program.cs`.
- Designing the claims-to-domain translation at a service boundary.
- Setting up OpenIddict demo mode for local development and contributor onboarding.
- Configuring service-to-service authentication (Managed Identity in Azure, alternatives in dev).
- Obtaining a dev-mode token for grpcurl or Evans testing.

**Do NOT use this skill for:**

- Transport configuration for domain events published by Identity — see `wolverine-azure-service-bus`.
- Projections that consume Identity's domain events — see `marten-projections`.
- gRPC handler middleware patterns — see `wolverine-grpc-handlers`.
- HTTP handler validation patterns — see `wolverine-http-handlers`.
- Aspire orchestration of the Identity service — see `aspire` and `service-bootstrap`.

## The provider landscape

ADR-006 commits to a swappable provider model with three tiers:

| Environment | Provider | Role | Token issuer |
|---|---|---|---|
| Production (Azure) | Entra External ID | Rider and driver identity | `login.microsoftonline.com/{tenant}` |
| Local dev (alternative) | Keycloak | Full-featured OIDC provider, no Azure dependency | `localhost:{port}/realms/crittercab` |
| Demo mode | OpenIddict | Minimal token issuer embedded in the Identity service | `localhost:{port}/` |

**Entra External ID** is the production provider. It handles user registration, MFA, social logins, and user-lifecycle events. Microsoft Graph change notifications deliver user-lifecycle events over ASB to the Identity BC.

**Keycloak** is the expected local-dev alternative for contributors who need a full-featured OIDC provider (user management UI, social login testing, custom scopes) without an Azure tenant. It runs as a Docker container alongside the Cab services.

**OpenIddict** is the demo-mode provider. It runs inside the Identity service itself, issuing short-lived tokens with minimal configuration. Its purpose is contributor friendliness: a developer cloning the repo can run all services and obtain valid tokens without configuring any external provider. OpenIddict is not a replacement for Entra or Keycloak — it issues tokens, but it does not manage users, handle MFA, or process lifecycle events.

The Identity BC's translation logic is the only code that changes between providers. Every other service's token validation points to a configurable OIDC authority URL and works identically regardless of which provider issued the token.

### Operations users (parked)

Operations users (internal staff) authenticate against a workforce Entra ID tenant, separate from the External ID tenant used by riders and drivers. This integration is explicitly parked per ADR-006 — it will be addressed when the Operations BC is actively built.

## Token validation at every service

Every Cab service that accepts external requests (HTTP or gRPC) validates incoming JWT Bearer tokens. This is standard ASP.NET Core middleware, not Wolverine-specific and not Identity-BC-specific.

### Bootstrap pattern

```csharp
// Program.cs — any Cab service that accepts authenticated requests
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // The authority URL is the OIDC discovery endpoint.
        // In production: Entra External ID.
        // In demo mode: the local OpenIddict server.
        // Aspire injects this via configuration.
        options.Authority = builder.Configuration["Identity:Authority"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Identity:Audience"],
        };
    });

builder.Services.AddAuthorization();

// ...

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

The `Authority` URL is the only configuration that changes between environments. ASP.NET Core's JWT Bearer middleware fetches the JWKS keys from `{Authority}/.well-known/openid-configuration` automatically — no manual key management.

### Standard OIDC claims — and nothing else

Services extract these claims from validated tokens:

| Claim | OIDC standard name | Cab usage |
|---|---|---|
| Subject | `sub` | The user's unique identifier at the provider level |
| Email | `email` | Display and notification routing |
| Roles | `roles` or `role` | Authorization policies (e.g., `rider`, `driver`, `admin`) |
| Name | `name` | Display name |

Services must **not** parse provider-specific claims. Entra External ID includes claims like `oid`, `tid`, `idp`, and `tfp` — these are invisible to every service except Identity. If a service needs provider-specific information, it requests it from Identity via a domain event or gRPC call, not by parsing the token.

### Auth on gRPC endpoints

Wolverine gRPC endpoints receive auth context through ASP.NET Core's middleware pipeline. The `[Authorize]` attribute works on the generated service stub, and `ServerCallContext` carries the authenticated `ClaimsPrincipal`:

```csharp
// Auth middleware for Wolverine gRPC — runs before every handler in the chain
public class AuthContextMiddleware
{
    public static (ClaimsPrincipal?, ProblemDetails?) Before(
        ServerCallContext context)
    {
        var user = context.GetHttpContext().User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return (null, new ProblemDetails { Status = 401 });
        }
        return (user, null);
    }
}

// Register service-wide via WolverineGrpcOptions
builder.Services.AddWolverineGrpc(options =>
{
    options.AddMiddleware<AuthContextMiddleware>();
});
```

The authenticated `ClaimsPrincipal` can then be injected into handlers via Wolverine's IoC resolution — the principal flows through the DI container, not through handler parameters parsed from the protobuf message.

### Auth on HTTP endpoints

Wolverine HTTP endpoints use standard ASP.NET Core `[Authorize]` attributes:

```csharp
[WolverinePost("/api/trips/start"), Authorize(Roles = "rider")]
public static (TripStarted, OutgoingMessages) Post(
    StartTripRequest request,
    ClaimsPrincipal user)
{
    var riderId = user.FindFirstValue("sub");
    // ...
}
```

### Current auth posture

No roadmap or milestone document commits to a specific `[AllowAnonymous]` posture. The structural constraints (ADR-006) define the rules; the timeline for enforcing auth on every endpoint is not yet pinned. During early development, endpoints may carry `[AllowAnonymous]` to unblock implementation — but this is a scaffolding convenience, not a design decision.

## The Identity BC's internal architecture

### Provider event ingestion

In production, the Identity BC subscribes to Microsoft Graph change notifications delivered over Azure Service Bus. The flow:

```
Entra External ID
    │
    ▼ (Microsoft Graph change notification)
Azure Service Bus topic
    │
    ▼ (subscription: "identity")
Identity BC handler
    │
    ▼ (translates provider event → domain event)
ASB topic: "identity.rider-registered"
    │
    ▼ (subscriptions)
Onboarding, Trips, Dispatch, ...
```

The Identity BC handler receives a Graph change notification (a provider-specific payload), extracts the relevant data, maps provider object IDs to domain identifiers, and publishes a domain event. No other service sees the Graph payload.

### Projection for provider events

The Identity BC projects provider lifecycle events into local domain documents using Marten's `AddProjectionWithServices<T>` (per `marten-projections`). The projection IoC-injects the Microsoft Graph client to enrich incoming events:

```csharp
// Identity service — projection that enriches Graph events into domain documents
public class RiderProfileProjection : CustomProjection<RiderProfile, string>
{
    private readonly GraphServiceClient _graph;

    public RiderProfileProjection(GraphServiceClient graph)
    {
        _graph = graph;
    }

    // Projection receives the domain event (already translated by the handler),
    // enriches it from Graph if needed, and upserts the local document.
}
```

The `GraphServiceClient` is IoC-injected because the projection needs to call Microsoft Graph for enrichment (e.g., fetching profile photos, group memberships) that aren't included in the change notification payload. This projection registers as `ProjectionLifecycle.Async` because the Graph call is I/O-bound.

### Domain identifiers, not provider object IDs

Domain events published by Identity carry **domain identifiers** that Identity assigns, not Entra's `oid` or Graph's `id`. The mapping from provider ID to domain ID lives inside the Identity BC:

```csharp
public record RiderRegistered(
    Guid RiderId,          // Cab's domain identifier — assigned by Identity
    string Email,
    string DisplayName,
    DateTimeOffset RegisteredAt);
// No Entra oid, no Graph id, no tenant id.
```

Other BCs receive `RiderId` and never need to know which provider it came from. If the provider changes (Entra → Keycloak), the `RiderId` mapping changes inside Identity, but `RiderRegistered` events look the same to every consumer.

## Demo mode: OpenIddict

### Purpose

OpenIddict demo mode exists so that a contributor can clone the repo, run `aspire run`, and interact with authenticated endpoints immediately — no Azure tenant, no Keycloak configuration, no external dependencies. The Identity service hosts an embedded OpenIddict server that issues short-lived tokens.

### Bootstrap in the Identity service

```csharp
// Identity service Program.cs — demo mode only
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenIddict()
        .AddServer(options =>
        {
            options.SetTokenEndpointUris("/connect/token");

            // Client credentials flow for service-to-service.
            // Resource owner password flow for quick dev-mode tokens.
            options.AllowClientCredentialsFlow()
                   .AllowResourceOwnerPasswordCredentialsFlow();

            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();

            options.UseAspNetCore()
                   .EnableTokenEndpointPassthrough();
        })
        .AddValidation(options =>
        {
            options.UseLocalServer();
            options.UseAspNetCore();
        });
}
```

`AddDevelopmentEncryptionCertificate()` and `AddDevelopmentSigningCertificate()` generate ephemeral certificates — valid for the lifetime of the process, no file or key-vault dependency. `UseLocalServer()` on the validation side means the Identity service validates its own tokens in-process.

Other Cab services point their JWT Bearer `Authority` to the Identity service's URL (Aspire-injected), and ASP.NET Core's OIDC discovery fetches the signing keys automatically.

### Obtaining a dev-mode token

With the Identity service running (via `aspire run`), request a token from the OpenIddict token endpoint:

```bash
# Resource owner password flow — quick dev-mode token
curl -X POST https://localhost:{identity-port}/connect/token \
  -d "grant_type=password" \
  -d "client_id=crittercab-dev" \
  -d "client_secret=dev-secret" \
  -d "username=rider@example.com" \
  -d "password=Password1!" \
  -d "scope=openid profile email"

# Client credentials flow — service-to-service token
curl -X POST https://localhost:{identity-port}/connect/token \
  -d "grant_type=client_credentials" \
  -d "client_id=trips-service" \
  -d "client_secret=trips-dev-secret" \
  -d "scope=crittercab-api"
```

The response includes an `access_token` JWT. Use it with grpcurl or Evans (per `cli-grpc-tooling`):

```bash
TOKEN=$(curl -s -X POST https://localhost:{identity-port}/connect/token \
  -d "grant_type=password&client_id=crittercab-dev&client_secret=dev-secret&username=rider@example.com&password=Password1!&scope=openid profile email" \
  | jq -r '.access_token')

grpcurl -insecure \
  -H "authorization: Bearer $TOKEN" \
  -d '{"trip_id": "..."}' \
  localhost:7233 \
  crittercab.trips.v1.Trips/StartTrip
```

The client IDs, secrets, and test users are seeded by the Identity service at startup in demo mode. They are hard-coded development credentials, not secrets — they appear in source code and are usable only against the local OpenIddict instance.

### Packages to add

OpenIddict packages are not yet in `Directory.Packages.props`. When the Identity service is implemented, add:

```xml
<PackageVersion Include="OpenIddict.AspNetCore" Version="{verify-latest}" />
<PackageVersion Include="OpenIddict.EntityFrameworkCore" Version="{verify-latest}" />
```

Check [OpenIddict releases](https://github.com/openiddict/openiddict-core/releases) for the current stable version compatible with .NET 10. If the Identity service uses Marten instead of EF Core for OpenIddict's storage, `OpenIddict.EntityFrameworkCore` may be replaced with a Marten-backed store — this is an implementation decision deferred to the Identity BC's implementation prompt.

## Service-to-service authentication

Not all requests come from end users. Services call each other over gRPC (per `wolverine-grpc-handlers`) and need to authenticate those calls.

### Managed Identity in Azure

In production, each Cab service runs with an Azure Managed Identity. When the Trips service calls the Pricing service over gRPC, it acquires a token from the Managed Identity endpoint:

```csharp
// Client-side — Trips calling Pricing
var credential = new DefaultAzureCredential();
var token = await credential.GetTokenAsync(
    new TokenRequestContext(new[] { "api://crittercab-pricing/.default" }));

var headers = new Metadata
{
    { "authorization", $"Bearer {token.Token}" }
};

var response = await pricingClient.RequestQuoteAsync(request, headers);
```

The Pricing service's JWT Bearer middleware validates the token against Entra ID's JWKS endpoint. The audience (`api://crittercab-pricing`) ensures the token was issued for the Pricing service specifically.

### Dev-mode alternatives

In local development, service-to-service auth uses one of:

- **OpenIddict client credentials flow** — each service has a pre-seeded client ID and secret. The calling service requests a token from the local OpenIddict server before making the gRPC call. This mirrors the production pattern but against a local issuer.
- **No auth (early development)** — during initial implementation, service-to-service calls may skip auth entirely. This is a scaffolding convenience, not a long-term pattern.

### The Aspire wiring

Aspire injects the Identity service URL into every other service via `WithReference()`. Each service reads the authority URL from configuration:

```csharp
// Trips service — gets the identity authority from Aspire-injected config
options.Authority = builder.Configuration["Identity:Authority"];
// Resolves to the Identity service's URL in dev, Entra External ID in production.
```

This is the same mechanism that makes the provider swap transparent: changing the authority URL from `https://localhost:{identity-port}` (OpenIddict) to `https://login.microsoftonline.com/{tenant}/v2.0` (Entra) is a configuration change, not a code change.

## Per-service claims-to-domain translation

Each service that needs user context translates standard OIDC claims into its own domain concepts at the boundary. This translation is minimal — it extracts claims and maps them to value objects or IDs the handler understands:

```csharp
// Trips service — boundary translation, not an ACL
public static class CallerIdentity
{
    public static Guid GetRiderId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Missing sub claim");

        // The sub claim is the domain RiderId that Identity assigned.
        // In production (Entra), Identity maps the Entra oid to this RiderId.
        // In demo mode (OpenIddict), the seeded user's sub IS the RiderId.
        return Guid.Parse(sub);
    }

    public static bool IsDriver(ClaimsPrincipal user)
        => user.IsInRole("driver");
}
```

This is **not** an anti-corruption layer — the service is conformist to the OIDC standard. The real ACL is the Identity BC, which ensures that `sub` carries a domain `RiderId` regardless of provider. Each consuming service trusts the `sub` claim because Identity guarantees its semantics.

## Common pitfalls

- **Parsing provider-specific claims outside Identity.** Entra tokens include `oid`, `tid`, `idp`, `tfp`, and other provider-proprietary claims. Only the Identity BC reads these. Every other service uses `sub`, `email`, `roles`, and `name` — the standard OIDC set. This is codified in the structural constraints.

- **Confusing token validation with the anti-corruption layer.** Token validation at each service is a security boundary (Conformist to OIDC). The ACL is the Identity BC's translation of provider lifecycle events into domain events. They are separate concerns — swapping providers changes the ACL, not the token validation code (only the authority URL changes).

- **Publishing domain events with provider object IDs.** `RiderRegistered` must carry a `RiderId` (domain identifier assigned by Identity), not an Entra `oid`. If a consumer needs to correlate back to the provider, it calls Identity — it never parses the provider ID itself.

- **Hard-coding the OIDC authority URL.** The authority must come from configuration (Aspire-injected in dev, app settings in production). Hard-coding `login.microsoftonline.com` or `localhost:5001` breaks the provider-swap guarantee.

- **Enabling resource-owner-password flow in production.** The ROPC flow in OpenIddict demo mode is a convenience for obtaining dev tokens quickly. It should never be enabled in production — use authorization code flow with PKCE for end users and client credentials for services.

- **Treating OpenIddict as a user management system.** OpenIddict issues tokens. It does not manage users, handle MFA, or process lifecycle events. In demo mode, users are seeded at startup. For full user-management features in local dev, use Keycloak.

- **Adding `[Authorize]` without configuring the JWT Bearer middleware.** The `[Authorize]` attribute requires `AddAuthentication().AddJwtBearer()` and `app.UseAuthentication(); app.UseAuthorization();` in the pipeline. Without them, every request is rejected with a 401 that has no `WWW-Authenticate` challenge header — a confusing failure mode.

- **Skipping `ValidateAudience` in production.** A token issued for Service A should not be accepted by Service B. `ValidAudience` ensures each service only accepts tokens explicitly issued for its own audience. In demo mode with a single OpenIddict server, a shared audience is acceptable; in production with Entra, per-service audiences are required.

- **Using Managed Identity tokens in local dev.** `DefaultAzureCredential` can resolve Visual Studio or Azure CLI credentials locally, but the audience (`api://crittercab-pricing`) only exists in the Azure tenant. For local dev, use OpenIddict's client credentials flow instead — it produces tokens the local JWT Bearer middleware can validate.

- **Forgetting that OpenIddict packages are not in Directory.Packages.props.** The Identity service will not compile until the packages are added. Check the current OpenIddict release and pin the version in central package management before implementing.

- **Implementing Operations user auth.** Workforce Entra ID tenant integration is explicitly parked per ADR-006. Do not build it until the Operations BC is actively under development.

## See also

### Upstream

- `transport-selection` — the decision framework; ASB carries Identity's domain events, gRPC carries service-to-service calls.
- `wolverine-azure-service-bus` — the ASB transport configuration for domain events that Identity publishes (`RiderRegistered`, `DriverAccountCreated`).
- `wolverine-grpc-handlers` — gRPC handler middleware where auth context extraction runs; the `[WolverineBefore]` and `WolverineGrpcOptions.AddMiddleware<T>()` patterns.
- `service-bootstrap` — the `Program.cs` composition pattern that authentication middleware extends.

### Sibling skills

- `cli-grpc-tooling` — grpcurl's `-H 'authorization: Bearer ...'` pattern for exercising authenticated endpoints with dev-mode tokens.
- `marten-projections` — the `AddProjectionWithServices<T>` pattern used by Identity to project Microsoft Graph events with an IoC-injected Graph client.

### Downstream

- `observability-tracing` (Phase 3) — traces that span authenticated requests, including the token-validation middleware span.
- `wolverine-sagas` (Phase 4) — long-running processes that may need to refresh or propagate auth context across steps.
- `testing-advanced` (Phase 4) — integration tests with authenticated requests, test-token factories.

### External

- ADR-006 in [`docs/decisions/`](../../decisions/) — the binding decision for the swappable identity provider pattern.
- [OpenIddict documentation](https://documentation.openiddict.com/) — server and validation configuration reference.
- [OpenIddict samples](https://github.com/openiddict/openiddict-samples) — canonical OAuth 2.0 flow implementations.
- [Entra External ID documentation](https://learn.microsoft.com/en-us/entra/external-id/) — production provider for rider and driver identity.
- [Microsoft Graph change notifications](https://learn.microsoft.com/en-us/graph/change-notifications-overview) — the webhook mechanism Identity uses to ingest provider events.
- ai-skills: `identity-openiddict`, `identity-entra` (if/when JasperFx publishes them).
