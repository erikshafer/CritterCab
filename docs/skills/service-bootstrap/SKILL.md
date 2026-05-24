---
name: service-bootstrap
description: "Composition root patterns for a CritterCab service Program.cs — Aspire-injected configuration, Wolverine + Marten/Polecat wiring, transport routing rules, observability bootstrap, health checks, and the per-service hosting contract. Use when authoring or modifying any service's Program.cs."
cluster: distributed-services
tags: [bootstrap, program-cs, composition-root, aspire, wolverine, marten, polecat, hosting]
---

# Service Bootstrap

Composition root patterns for a CritterCab service. This skill governs **what `Program.cs` actually contains** for a service in this project — distinct from `adding-a-service`, which establishes the project skeleton and its csproj/database/Aspire registration.

The boundary between the two skills:

- `adding-a-service` answers "how do I add a new service to the repo?" — directory layout, csproj template, database provisioning, Aspire AppHost registration, paired test project.
- `service-bootstrap` (this skill) answers "what goes inside `Program.cs` for that service?" — dependency wiring, Wolverine configuration, store wiring, routing rules, observability bootstrap, health checks.

## When to apply this skill

Use this skill when:

- Authoring `Program.cs` for a new service (after `adding-a-service` produces the project skeleton).
- Modifying an existing service's `Program.cs` to add a transport, change store wiring, or update observability.
- Reviewing PRs that change a service's composition root.
- Diagnosing startup-time issues: missing routing rules, store not wired, observability not exporting.

Do NOT use this skill for:

- Project-level setup (csproj, database creation, Aspire registration) — see `adding-a-service`.
- Aspire AppHost wiring — see `aspire` (Phase 2).
- Per-handler patterns — see `wolverine-handlers` and the protocol-specific siblings.
- Per-store patterns — see `marten-aggregates` (Phase 2) or `polecat-event-sourcing` (Phase 4).
- Per-transport routing-rule syntax — see `wolverine-kafka`, `wolverine-azure-service-bus`, `wolverine-grpc-handlers` (Phase 3).

---

## The Service Composition Contract

Every CritterCab service's `Program.cs` does the same six things, in roughly the same order:

1. **Build the host** with `WebApplication.CreateBuilder(args)` and pull Aspire-injected configuration.
2. **Register Wolverine** with the service's specific configuration (handlers, transports, routing rules).
3. **Wire the store** — Marten or Polecat — including event-type registration and Wolverine integration.
4. **Configure observability** — distributed tracing, metrics, logging, all Aspire-aware.
5. **Map endpoints** — health checks, gRPC services, HTTP endpoints (per service's contract surface).
6. **Run** the host.

Services are free to layer additional configuration in (CORS, auth, rate-limiting, custom middleware) but those six are the universal floor. The patterns below show the canonical shape of each.

---

## Marten-Backed Service: Canonical Program.cs

The full shape for a Marten-backed service. Trips is used as the example because it's the most representative — event-sourced aggregate, gRPC server-streaming for live trip updates, ASB for the cross-service `TripCompleted` integration event.

```csharp
using CritterCab.Trips;
using JasperFx;
using Marten;
using Marten.Events.Projections;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.Marten;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

// Aspire-injected configuration — service discovery, OpenTelemetry, health-check defaults.
builder.AddServiceDefaults();

// Marten + event sourcing
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("crittercab_trips")
        ?? throw new InvalidOperationException("Missing connection string: crittercab_trips"));

    // Per ADR-005, schema management is automatic in development, explicit in production.
    opts.AutoCreateSchemaObjects = builder.Environment.IsDevelopment()
        ? AutoCreate.CreateOrUpdate
        : AutoCreate.None;

    // Event type registration — every domain event in the Trips event stream.
    // See domain-event-conventions § Marten Event Type Registration.
    opts.Events.AddEventType<TripStarted>();
    opts.Events.AddEventType<DriverArrived>();
    opts.Events.AddEventType<TripCompleted>();
    opts.Events.AddEventType<TripCancelled>();
    opts.Events.AddEventType<WaypointRecorded>();

    // Mandatory event-type declaration. Omitting an AddEventType causes silent null
    // returns from AggregateStreamAsync<T> for streams containing the unregistered type.
    opts.Events.UseMandatoryStreamTypeDeclaration = true;

    // Inline projections; async projections registered per-projection. See marten-projections.
    opts.Projections.LiveStreamAggregation<Trip>();
})
.IntegrateWithWolverine()
.UseLightweightSessions()
.AddAsyncDaemon(DaemonMode.HotCold);

// Wolverine
builder.Host.UseWolverine(opts =>
{
    // Service-internal queue: where the service handles its own commands.
    opts.LocalRoutingConventionDisabled = false;

    // Cross-service publication: every integration event must be routed.
    // See wolverine-messaging-handlers § The Routing Rule Pre-Flight.
    opts.PublishMessage<Integration.TripCompleted>()
        .ToAzureServiceBusTopic("trips.trip-completed");

    opts.PublishMessage<Integration.TripCancelled>()
        .ToAzureServiceBusTopic("trips.trip-cancelled");

    // Cross-service subscription: this service consumes Dispatch's OfferAccepted.
    opts.UseAzureServiceBus(builder.Configuration.GetConnectionString("azure-service-bus")
        ?? throw new InvalidOperationException("Missing connection string: azure-service-bus"))
        .UseTopicAndSubscriptionConventionalRouting();

    // Service-location policy: warn in dev, hard-fail in CI/prod.
    // See wolverine-handlers § Anti-Pattern: Lambda Factory Service Registrations.
    opts.ServiceLocationPolicy = builder.Environment.IsDevelopment()
        ? ServiceLocationPolicy.AllowedButWarn
        : ServiceLocationPolicy.NotAllowed;
});

builder.Services.CritterStackDefaults(x =>
{
    x.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
    x.Production.GeneratedCodeMode = TypeLoadMode.Static;
    x.Production.AssertAllPreGeneratedTypesExist = true;
});

var app = builder.Build();

// Health checks — Aspire dashboard reads from /health and /alive.
app.MapDefaultEndpoints();

// gRPC services — registered via Wolverine.Grpc per-service convention.
// See wolverine-grpc-handlers (Phase 3) for the handler shape.
// (gRPC service mapping placeholder until Phase 3 lands.)

return await app.RunJasperFxCommands(args);
```

> **Greenfield Marten + Wolverine recommendations beyond what's shown above.** ai-skills `critterstack-arch-new-project-wolverine-marten` documents additional greenfield-recommended options Cab's example doesn't enumerate: `EventAppendMode.Quick` (~50% throughput improvement), `UseArchivedStreamPartitioning`, `EnableEventSkippingInProjectionsOrSubscriptions`, `Projections.UseIdentityMapForAggregates`, `Projections.EnableAdvancedAsyncTracking`, `DisableNpgsqlLogging`, plus Wolverine durability optimizations (`Durability.EnableInboxPartitioning`, `InboxStaleTime`/`OutboxStaleTime`, `UnknownMessageBehavior = DeadLetterQueue`) and the `Policies.AutoApplyTransactions()` policy. For document-store index registration (which Cab's bootstrap example doesn't cover), see ai-skills `marten-advanced-indexes-and-query-optimization`. The `WolverineFx.Http.Marten` metapackage convention and the version-alignment warning across all `WolverineFx.*` packages are also documented there.

**Key conventions visible above:**

- **`builder.AddServiceDefaults()`** is Aspire's hook for OpenTelemetry, service discovery, and health-check defaults. Every service calls it; it's the single line that wires the Aspire ecosystem.
- **Connection strings come from configuration** with explicit null checks. Aspire injects them via the `WithReference()` calls in the AppHost. The null-check throws at startup if Aspire isn't wired correctly — fail fast, don't run with broken config.
- **Schema management is environment-aware.** Development services auto-create; non-development services run schema migrations explicitly (typically as a `dotnet run -- db-apply` command, not at startup).
- **`UseMandatoryStreamTypeDeclaration = true`** is on by default in CritterCab. Omitting an `AddEventType<T>()` registration produces silent null returns from aggregate loads — the most consequential Marten footgun, fully prevented by this setting.
- **Routing rules are explicit at the composition root**, one per integration event type. The skill responsible for catching missing routing rules is `wolverine-messaging-handlers` § The Routing Rule Pre-Flight.
- **`ServiceLocationPolicy` is environment-aware.** Development tolerates fallbacks (you discover them in the log); CI and production hard-fail. This catches lambda-factory regressions before they reach production.
- **`CritterStackDefaults` configures code-generation modes.** Dynamic in development (fast iteration); static in production (no startup latency). `AssertAllPreGeneratedTypesExist = true` is the safety net that fails startup if a handler shipped without its generated adapter.
- **`app.RunJasperFxCommands(args)` is the entry point**, not `app.Run()`. This enables the JasperFx CLI surface (`db-apply`, `codegen-write`, `wolverine-diagnostics`, etc.). See `cli-jasperfx`. Returns `Task<int>`, so use `return await app.RunJasperFxCommands(args);` in top-level statements.

---

## Polecat-Backed Service: The Differences

Most of the bootstrap above carries over to a Polecat-backed service. The differences:

```csharp
using Wolverine.Polecat;
using Polecat;

builder.Services.AddPolecat(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("CritterCab_Payments")
        ?? throw new InvalidOperationException("Missing connection string: CritterCab_Payments"));

    opts.AutoCreateSchemaObjects = builder.Environment.IsDevelopment()
        ? AutoCreate.CreateOrUpdate
        : AutoCreate.None;

    // Event type registration — same shape as Marten.
    opts.Events.AddEventType<PaymentInitiated>();
    opts.Events.AddEventType<PaymentAuthorized>();
    opts.Events.AddEventType<PaymentCaptured>();
    opts.Events.AddEventType<PaymentRefunded>();

    opts.Events.UseMandatoryStreamTypeDeclaration = true;
})
.IntegrateWithWolverine();
```

**Mechanical differences:**

- **`AddPolecat` instead of `AddMarten`.** Connection string targets SQL Server (`CritterCab_<ServiceName>`, PascalCase per `adding-a-service` § Database Isolation and Naming).
- **Connection-string casing follows the database naming convention.** Postgres uses `crittercab_<service_name>` (snake_case); SQL Server uses `CritterCab_<ServiceName>` (PascalCase).
- **Async daemon API differs slightly.** Polecat's daemon configuration follows its own surface; see `polecat-event-sourcing` (Phase 4).
- **Handlers use `PolecatOps.StartStream<T>` instead of `MartenOps.StartStream<T>`.** See `wolverine-handlers` § Anti-Pattern: Starting a New Stream Without Returning IStartStream.

The rest of the composition root — Wolverine configuration, transport routing, observability, health checks, `RunJasperFxCommands` — is identical between Marten and Polecat services.

---

## Typed Configuration with `IOptions<T>` and `ValidateOnStart`

Connection strings flow through `IConfiguration` as single values read directly with `GetConnectionString(...)` (as shown in the canonical Program.cs above). Richer configuration — feature flags, external API credentials, tuning parameters, BC-specific behavioral toggles — should bind to a typed options class with validation that fails at startup if any required value is missing or malformed.

The pattern:

```csharp
// Configuration class — sealed record with required properties and validation attributes.
public sealed record PaymentsGatewayOptions
{
    [Required, Url]
    public required string BaseUrl { get; init; }

    [Required, MinLength(32)]
    public required string ApiKey { get; init; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    [Required]
    public required string MerchantId { get; init; }
}

// Registration in Program.cs — bind, validate, fail at startup if invalid.
builder.Services
    .AddOptions<PaymentsGatewayOptions>()
    .BindConfiguration("PaymentsGateway")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Consumption — inject IOptions<T>.
public sealed class PaymentsGatewayClient
{
    private readonly PaymentsGatewayOptions _options;

    public PaymentsGatewayClient(IOptions<PaymentsGatewayOptions> options)
    {
        _options = options.Value;
    }
    // ...
}
```

**Why this pattern, not raw `IConfiguration` reads:**

- **`ValidateOnStart()` fails the host at boot time.** A missing or malformed configuration value raises before the first request is served, with a clear exception identifying which field failed validation. Without it, you discover the missing config when a downstream call returns 401 or the first `NullReferenceException` fires deep in handler code.
- **Data-annotation validation runs once, at startup.** `[Required]`, `[Url]`, `[Range]`, `[MinLength]` etc. are checked when the host starts; consumers can trust the values are well-formed without re-validating.
- **The options class is the documentation.** A developer reading the service to understand its configuration surface reads one record, not the union of every `builder.Configuration["..."]` call scattered through Program.cs.
- **Refactoring is safer.** Renaming a configuration key shows up as a compile error in the options class, not as a silent null at runtime.

**When to use `IOptions<T>`, `IOptionsMonitor<T>`, or `IOptionsSnapshot<T>`:**

| Lifetime needed | Inject |
|---|---|
| Singleton consumer; options never change after startup | `IOptions<T>` |
| Singleton consumer; need to observe runtime changes | `IOptionsMonitor<T>` |
| Scoped consumer (e.g., per HTTP request); pick up reloaded values | `IOptionsSnapshot<T>` |

For the vast majority of CritterCab cases, `IOptions<T>` is correct — service configuration is read once at startup and doesn't change. Use `IOptionsMonitor<T>` only when a value genuinely needs to be hot-reloadable (a feature flag, a rate limit) and you've decided that runtime reconfiguration is a supported behavior of that service.

**Connection strings stay with `GetConnectionString(...)`.** The `?? throw` pattern in the canonical Program.cs above is correct for connection strings, which are first-class in `IConfiguration` and read directly. The typed-options pattern is for richer configuration sections — anything that would otherwise be a clump of `builder.Configuration["Foo:Bar"]` reads.

**Aspire AppHost emits the values; the service binds them.** Per the `aspire` skill, the AppHost translates resource outputs into explicit configuration via `.WithReference(...)` (for connection strings) or `.WithEnvironment("PaymentsGateway__ApiKey", ...)` (for typed-options sections). The double-underscore separator maps to the colon-separated configuration path, so `PaymentsGateway__BaseUrl` becomes `PaymentsGateway:BaseUrl`, which `BindConfiguration("PaymentsGateway")` picks up. The service stays Aspire-agnostic — it binds to configuration keys, not to Aspire abstractions.

---

## Per-Service Configuration Variation

Beyond the canonical shape, each service makes a small number of decisions specific to its contract surface. The composition root encodes those decisions in one place.

### Service that exposes gRPC

Add `Wolverine.Grpc`, register gRPC services after `app.Build()`. Detailed patterns for the handler shapes live in `wolverine-grpc-handlers` (Phase 3); the bootstrap-side wiring is straightforward:

```csharp
// In the UseWolverine block:
opts.UseGrpc();   // enables Wolverine.Grpc

// After app.Build():
app.MapWolverineGrpcServices();   // maps all gRPC handlers in the assembly
```

### Service that exposes Wolverine.HTTP endpoints

Add `WolverineFx.Http`, register HTTP services before building the host, and map the endpoints after `app.Build()`. The `AddWolverineHttp()` registration is required — without it, `MapWolverineEndpoints()` throws at startup.

```csharp
// Before app.Build():
builder.Services.AddWolverineHttp();

// After app.Build():
app.MapWolverineEndpoints();   // maps all [WolverinePost]/[WolverineGet]/etc. handlers
```

Detailed handler shapes for HTTP endpoints live in `wolverine-http-handlers`.

### Service that consumes Kafka (against Event Hubs)

Add `WolverineFx.Kafka`, configure the Kafka transport against the Event Hubs connection string. The Event Hubs connection string format and emulator wiring details belong to `wolverine-kafka` and `cli-azure-messaging` (both Phase 3); the bootstrap-side hook:

```csharp
opts.UseKafka(builder.Configuration.GetConnectionString("event-hubs")
    ?? throw new InvalidOperationException("Missing connection string: event-hubs"));

// Subscription on inbound topic:
opts.ListenToKafkaTopic("telemetry.location-pings")
    .ProcessInline();
```

### Service that publishes only, doesn't consume

A service that only publishes integration events (no inbound subscriptions from cross-service traffic) still requires the routing rules but does not register inbound listeners. The `UseAzureServiceBus(...)` call without `UseTopicAndSubscriptionConventionalRouting()` is appropriate.

### BFF service (gRPC client to other services)

A service acting as a backend-for-frontend that calls other services via gRPC registers the gRPC client(s) it consumes. The pattern is per-callee:

```csharp
builder.Services.AddGrpcClient<DispatchService.DispatchServiceClient>(opts =>
{
    opts.Address = new Uri(builder.Configuration["DispatchServiceUrl"]
        ?? throw new InvalidOperationException("Missing DispatchServiceUrl"));
});
```

The actual BFF patterns and gRPC client conventions are out of scope for this skill — see `wolverine-grpc-handlers` (Phase 3) for the handler-side, and a future BFF-specific skill for the BFF architecture.

---

## Observability Bootstrap

`builder.AddServiceDefaults()` handles the bulk: OpenTelemetry tracing, metrics export to OTLP (Aspire dashboard or production OTLP collector), structured logging, the `/health` and `/alive` endpoints. Each service can layer additional instrumentation on top.

```csharp
// In Program.cs, after AddServiceDefaults:
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
{
    // Add service-specific instrumentation sources.
    tracing.AddSource("CritterCab.Trips");
});
```

The detailed distributed-tracing patterns — span attributes, context propagation across gRPC, correlation between Marten-recorded events and traces — belong to `observability-tracing` (Phase 2). The bootstrap-side decision is "always call `AddServiceDefaults` first; layer service-specific sources after."

---

## Health Check Conventions

`MapDefaultEndpoints()` from `AddServiceDefaults` exposes `/health` (liveness + readiness) and `/alive` (liveness only). Each service can add custom checks:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("dispatch-grpc", () => HealthCheckResult.Healthy(),
        tags: new[] { "ready" });
```

Health check tags determine which endpoint they appear under:

- Untagged or `"live"` tag → `/alive` and `/health`
- `"ready"` tag → `/health` only

The Aspire dashboard polls these endpoints and surfaces service state. Production deployments typically map the same endpoints to load-balancer health probes — readiness gates traffic, liveness gates restart.

---

## Test Project Composition

The paired test project in `tests/CritterCab.<ServiceName>.Tests/` typically uses `Alba` to compose against the same `Program.cs`. The test fixture pattern lives in `testing-fundamentals` (Phase 2); the bootstrap-relevant point is that `Program.cs` should be importable from the test project — which means the implicit `Main` method generated by top-level statements works fine for Alba's `AlbaHost.For<Program>()`.

There is no separate "test bootstrap" — tests use the production composition root with overrides applied via Alba's hooks.

---

## Common Pitfalls

- **Missing routing rule for a published integration event.** The most consequential composition-root mistake. The handler appears to publish; nothing reaches the bus; tests fail with `tracked.Sent.MessagesOf<T>() == 0`. Mandatory pre-flight: every `OutgoingMessages.Add(new SomeIntegrationEvent(...))` must have a matching `opts.PublishMessage<SomeIntegrationEvent>()` rule. See `wolverine-messaging-handlers`.
- **Missing `AddEventType` for a domain event.** Silent null returns from aggregate loads. Every event the service appends must be registered. `UseMandatoryStreamTypeDeclaration = true` makes this fail loudly at append time; if it's set to false, failures surface only at load time and are nearly impossible to diagnose.
- **`app.Run()` instead of `RunJasperFxCommands(args)`.** Service starts but the JasperFx CLI is unavailable. `dotnet run -- describe`, `dotnet run -- db-apply`, etc. all fail with "command not found." Always use `return await app.RunJasperFxCommands(args);`.
- **Auto-schema in production.** `AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate` in production means the service mutates schema on every deploy. Production should use `AutoCreate.None` and run schema migrations explicitly via `dotnet run -- db-apply` as a deploy step.
- **`ServiceLocationPolicy.AlwaysAllowed` to silence warnings.** Hides regressions. The right path is to fix the registration (concrete type, not lambda) or add an allow-list entry: `opts.CodeGeneration.AlwaysUseServiceLocationFor<IRefitClient>()`.
- **Missing `AddServiceDefaults`.** Service starts but doesn't register with the Aspire dashboard, doesn't export OpenTelemetry, has no health endpoints. Always call it; it's the cheapest single line in the bootstrap.
- **Missing `TimeProvider` DI registration.** Handlers that inject `TimeProvider` (the canonical timestamp pattern per `csharp-coding-standards`) fail at runtime if `TimeProvider.System` isn't registered in DI. Register explicitly: `builder.Services.AddSingleton(TimeProvider.System);`.
- **Missing `AddWolverineHttp()` for services that map Wolverine endpoints.** `MapWolverineEndpoints()` throws at startup if `AddWolverineHttp()` wasn't called. See § Per-Service Configuration Variation, "Service that exposes Wolverine.HTTP endpoints".
- **Hardcoding connection strings.** Aspire injects them via configuration. Hardcoding works in dev for the wrong reasons (the Aspire-provisioned database happens to listen on the same port) and fails on every deploy. Always read from `builder.Configuration.GetConnectionString(...)`.
- **Calling `IntegrateWithWolverine()` before `AddMarten`/`AddPolecat`.** Order matters; the `IntegrateWithWolverine` call extends the registration that came immediately before it. If the call chain is interrupted, the integration silently doesn't wire.

- **Omitting `.ValidateOnStart()` on typed options.** Validation deferred to first-use means a missing or malformed config value surfaces as a confusing downstream failure (401 response, null reference, malformed URI) instead of as a clear startup error. Always call `.ValidateOnStart()` after `.ValidateDataAnnotations()`; the cost is one extra line and the benefit is failing fast at boot rather than three layers deep at runtime.

---

## See also

**Upstream** — generic Wolverine + Marten/Polecat bootstrap fundamentals this skill builds on. ai-skills (license required, install via `npx skills add`):

- `critterstack-arch-new-project-wolverine-marten` (primary) — Wolverine + Marten greenfield bootstrap: NuGet metapackage convention with version-alignment warning, full canonical Program.cs with greenfield-recommended event-store options (`EventAppendMode.Quick`, `UseArchivedStreamPartitioning`, `EnableEventSkippingInProjectionsOrSubscriptions`, `Projections.UseIdentityMapForAggregates`, `Projections.EnableAdvancedAsyncTracking`, `DisableNpgsqlLogging`), Wolverine durability optimizations (`Durability.EnableInboxPartitioning`, `InboxStaleTime`/`OutboxStaleTime`, `UnknownMessageBehavior = DeadLetterQueue`), `Policies.AutoApplyTransactions()`, full Alba test fixture pattern with vertical slice example, anti-patterns (repository over Marten, missing AutoApplyTransactions, manual SaveChangesAsync). Cab's skill adds project-specific framing (6-step Service Composition Contract, Aspire-injected configuration with explicit null checks, environment-aware schema/policy decisions, connection-string casing conventions, per-service variation patterns for gRPC/Kafka/publish-only/BFF, Polecat-backed differences, Cab-specific pitfalls).
- `critterstack-arch-new-project-wolverine-polecat` — Wolverine + Polecat greenfield bootstrap. Equivalent surface for SQL-Server-backed services.
- `wolverine-handlers-ioc-and-service-optimization` — deep reference for `ServiceLocationPolicy`, codegen modes (`TypeLoadMode.Dynamic`/`Static`), pre-generation (`AssertAllPreGeneratedTypesExist`), and IoC bits this skill summarizes.

**Prerequisites** — Cab-internal skills to load first:

- `adding-a-service` — establishes the project skeleton and database; this skill picks up at `Program.cs`.
- `csharp-coding-standards` — sealed records, `TimeProvider`, modern guard clauses.
- `domain-event-conventions` — event registration is referenced in this skill.
- `transport-selection` — which transport routes which message; reading this before authoring routing rules avoids pre-flight churn.
- `wolverine-handlers` — the handler shape these bootstrap patterns enable.
- `wolverine-messaging-handlers` — routing-rule pre-flight; reinforces what `Program.cs` must contain for cross-service publication to work.

**Downstream** — natural follow-ups:

- `aspire` (Phase 2) — AppHost wiring, single-file `apphost.cs`, resource composition.
- `cli-aspire` (Phase 2) — `aspire run`, `aspire describe`, dashboard.
- `cli-jasperfx` (Phase 2) — `db-apply`, `codegen-write`, `wolverine-diagnostics`, the full JasperFx CLI surface this bootstrap exposes.
- `marten-aggregates` (Phase 2) — what to do once `AddMarten` is wired.
- `polecat-event-sourcing` (Phase 4) — what to do once `AddPolecat` is wired.
- `marten-projections` (Phase 2) — async-daemon configuration and projection registration.
- `observability-tracing` (Phase 2) — distributed tracing patterns layered on `AddServiceDefaults`.
- `testing-fundamentals` (Phase 2) — Alba-based test fixtures over this `Program.cs`.

**External:**

- ADR-002 in [`docs/decisions/`](../../decisions/) — services per bounded context.
- ADR-005 in [`docs/decisions/`](../../decisions/) — transport selection.
- ADR-007 in [`docs/decisions/`](../../decisions/) — Azure as deployment target.
- [Aspire Service Defaults documentation](https://learn.microsoft.com/dotnet/aspire/fundamentals/service-defaults).
