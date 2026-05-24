---
name: aspire-service-defaults
description: "The CritterCab.ServiceDefaults shared library that every Cab service references and calls via AddServiceDefaults() at the top of Program.cs. Centralizes the OpenTelemetry SDK baseline (logging + ASP.NET Core / HttpClient / Runtime instrumentation, with health endpoints filtered from traces), service-discovery registration, the /health and /alive endpoints with their tag-based readiness/liveness split, and HttpClient resilience defaults (retries, circuit breaker, timeout via AddStandardResilienceHandler combined with AddServiceDiscovery). Use when authoring the ServiceDefaults project initially, modifying what every service inherits, or understanding what is actually inside the AddServiceDefaults() call that service-bootstrap and observability-tracing both reference as a black-box foundation."
cluster: infrastructure
tags: [aspire, service-defaults, opentelemetry, health-checks, service-discovery, httpclient-resilience, shared-library, ihostapplicationbuilder, otlp]
---

# CritterCab.ServiceDefaults — The Shared Service Bootstrap Library

Every CritterCab service references one shared library — `CritterCab.ServiceDefaults` — and calls `builder.AddServiceDefaults()` as the first line of its `Program.cs`. That single call wires the cross-cutting concerns every service needs identically: the OpenTelemetry SDK baseline (logging, ASP.NET Core / HttpClient / Runtime instrumentation, OTLP exporter), service discovery for inter-service `HttpClient` calls, the standard `/health` and `/alive` endpoints with their tag-based readiness/liveness split, and a `ConfigureHttpClientDefaults` block that applies retries, circuit breaker, and timeout policies to every `HttpClient` registered after.

This skill governs **what lives inside `AddServiceDefaults()`** — the shared library, not the service-side call to it. Two adjacent skills reference this library as a black-box dependency: `service-bootstrap` describes the per-service Program.cs that calls it; `observability-tracing` layers Wolverine, Marten, and per-BC ActivitySources on top of the OTel baseline this skill provides. This skill is the one that documents what's actually in the box.

---

## When to apply this skill

Use this skill when:

- Authoring the `CritterCab.ServiceDefaults` project for the first time.
- Modifying what every Cab service inherits (e.g., adding a new instrumentation source to the baseline, changing the health-check tag convention, updating the resilience handler defaults).
- Diagnosing inheritance-shaped issues — telemetry not exporting, health endpoints missing, HttpClient calls not getting retried — when the failure mode points at the shared library rather than at a per-service Program.cs.
- Onboarding a new service: deciding what should live in the per-service `Program.cs` (per `service-bootstrap`) vs. what belongs in the shared library here.

Do NOT use this skill for:

- Per-service composition (Wolverine + Marten/Polecat wiring, routing rules, store registration) — see `service-bootstrap`.
- Adding Wolverine, Marten, or per-BC ActivitySources / Meters — see `observability-tracing` (tracing) and the eventual `observability-metrics` (metrics).
- AppHost-side orchestration (resource topology, `WithReference`, dashboard) — see `aspire`.
- Per-service custom health checks (database checks, downstream service checks, business-logic checks) — see `service-bootstrap` § Health Check Conventions.
- Test fixture composition over `Program.cs` — see `testing-integration` and `testing-fundamentals`.

---

## The CritterCab "no Aspire client packages" stance

Before the project structure, the load-bearing context: **CritterCab deliberately does not use Aspire client packages** like `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Aspire.StackExchange.Redis`, or `Aspire.Azure.Messaging.ServiceBus`. The data layer is Marten and Polecat directly against the connection strings Aspire injects via `WithReference(...)`. The messaging layer is Wolverine directly against the transport connection strings.

This is a conscious tradeoff:

- **What we lose:** Aspire client packages auto-register a health check, OpenTelemetry instrumentation, and resilience policies for each backing service. With Marten/Polecat directly, we configure those things ourselves.
- **What we gain:** A single coherent JasperFx-shaped composition root per service, no double-registration concerns, full control over Marten/Polecat configuration (which Aspire client packages can't expose), and no version-lockstep coupling between Aspire releases and JasperFx releases.

The implication for `ServiceDefaults`: it provides the **generic cross-cutting concerns** (OTel SDK baseline, service discovery, HTTP resilience, health endpoint mappings), not pre-configured clients. Per-service health checks (database connectivity, downstream service availability) are explicit in each service's `Program.cs` per `service-bootstrap`, not inherited from this library.

---

## Project structure

```
src/
  CritterCab.ServiceDefaults/
    CritterCab.ServiceDefaults.csproj
    Extensions.cs
```

One project, one extensions file. The project is referenced by every other Cab service's `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\CritterCab.ServiceDefaults\CritterCab.ServiceDefaults.csproj" />
</ItemGroup>
```

The Aspire AppHost (`apphost.cs` at the repository root) does **not** reference `ServiceDefaults` — the AppHost orchestrates, services consume. The `#:project` directives in `apphost.cs` point at the service projects; each service project transitively pulls `ServiceDefaults`.

---

## The .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsAspireSharedProject>true</IsAspireSharedProject>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />

    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />

    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="OpenTelemetry.Instrumentation.GrpcNetClient" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
  </ItemGroup>

</Project>
```

**Why each line is here:**

- **`<IsAspireSharedProject>true</IsAspireSharedProject>`** — the marker that tells the Aspire build system this is a shared library, not a service project. Required.
- **`FrameworkReference: Microsoft.AspNetCore.App`** — gives the library access to `WebApplication`, `HealthCheckOptions`, and the ASP.NET Core extension surface without making it a standalone web project.
- **`Microsoft.Extensions.Http.Resilience`** — supplies `AddStandardResilienceHandler` (retry, circuit breaker, timeout).
- **`Microsoft.Extensions.ServiceDiscovery`** — supplies `AddServiceDiscovery` and the `https+http://` URL resolver used by `HttpClient`.
- **`OpenTelemetry.Extensions.Hosting`** — the OTel SDK integration with the .NET host builder.
- **`OpenTelemetry.Exporter.OpenTelemetryProtocol`** — the OTLP exporter. Aspire dashboard locally; an OTLP collector (Azure Monitor, Jaeger, Grafana Tempo) in production.
- **`OpenTelemetry.Instrumentation.AspNetCore`** — request/response spans, ASP.NET Core metrics.
- **`OpenTelemetry.Instrumentation.Http`** — outbound `HttpClient` spans and metrics.
- **`OpenTelemetry.Instrumentation.GrpcNetClient`** — outbound gRPC spans (CritterCab is gRPC-first per ADR-009).
- **`OpenTelemetry.Instrumentation.Runtime`** — GC pauses, thread pool stats, exception counters.

Versions are pinned in `Directory.Packages.props` per CritterCab's central-version-management convention; this .csproj does not specify versions inline.

---

## Extensions.cs — the shape

The namespace is `Microsoft.Extensions.Hosting`. This is deliberate: callers see `builder.AddServiceDefaults()` available via the standard `using Microsoft.Extensions.Hosting;` they already have. The Aspire template uses this convention; Cab follows it.

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    private const string HealthEndpointPath  = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>
    /// Wires CritterCab's shared cross-cutting concerns into every service:
    /// OpenTelemetry baseline, service discovery, default health checks, and
    /// HttpClient resilience + service-discovery defaults.
    /// Called as the first line of every service's Program.cs, before any
    /// other service registration.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Standard resilience: retry, circuit breaker, attempt timeout, total timeout.
            http.AddStandardResilienceHandler();

            // Resolve https+http://service-name URLs through Aspire's discovery layer.
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Filter health-check requests out of traces.
                        // Without this, /health and /alive polling from the Aspire
                        // dashboard and production load balancers flood the trace
                        // store with low-value spans.
                        options.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments(HealthEndpointPath) &&
                            !ctx.Request.Path.StartsWithSegments(AlivenessEndpointPath);
                    })
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // OTLP endpoint is set by Aspire automatically in dev (points at the dashboard)
        // and by deployment configuration in production (Azure Monitor, Jaeger, Tempo).
        // If the env var is missing, OTel registers but exports nothing — fail-quiet
        // is the correct behavior here; the app still runs.
        var useOtlp = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlp)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // The floor: a single "self" check, tagged "live", that always passes
        // while the process is running. Per-service checks (DB connectivity,
        // downstream service availability) are layered in each service's
        // Program.cs per service-bootstrap, not here.
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps /health (readiness — all checks must pass) and /alive (liveness —
    /// only "live"-tagged checks). Called after WebApplication.Build() in every
    /// service. In production, responses are status-only (no check detail) to
    /// avoid leaking internal state through health endpoints.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        var statusOnly = !app.Environment.IsDevelopment();

        var readinessOptions = new HealthCheckOptions();
        var livenessOptions = new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        };

        if (statusOnly)
        {
            // Production response writer: just "Healthy" / "Degraded" / "Unhealthy"
            // as plain text. No per-check names, no exception details, no payloads.
            HealthCheckOptions WithStatusOnly(HealthCheckOptions o)
            {
                o.ResponseWriter = (ctx, report) =>
                {
                    ctx.Response.ContentType = "text/plain";
                    return ctx.Response.WriteAsync(report.Status.ToString());
                };
                return o;
            }

            readinessOptions = WithStatusOnly(readinessOptions);
            livenessOptions = WithStatusOnly(livenessOptions);
        }

        app.MapHealthChecks(HealthEndpointPath, readinessOptions);
        app.MapHealthChecks(AlivenessEndpointPath, livenessOptions);

        return app;
    }
}
```

---

## The health-endpoint exposure decision

CritterCab maps `/health` and `/alive` **unconditionally** — in every environment, including production. This is a deliberate departure from the Aspire template, which gates the mappings behind `IsDevelopment()`.

The rationale:

1. **Aspire's local dashboard polls these endpoints** to drive the resource state UI. Without them mapped in dev, the dashboard reports services as unknown.
2. **Production load balancers read these endpoints** to gate traffic (readiness via `/health`) and to decide whether to restart a pod (liveness via `/alive`). Hiding them in production means breaking the deployment contract.
3. **The security concern is real but addressable through the response writer**, not through hiding the endpoints. The production code path above replaces the default JSON-with-check-detail writer with a plain-text status-only writer — operators can tell the service is unhealthy without learning which downstream the failure traces back to.

If you ever need to expose richer health info to a specific audience (an internal SRE dashboard, an on-call diagnostic tool), do it on a *different* endpoint, not by removing the status-only protection on `/health` and `/alive`. The convention is: the canonical endpoints stay safe-for-public; anything richer is a new, intentionally-restricted endpoint.

---

## The readiness vs. liveness split

The tag convention:

- **`live` tag** → appears under `/alive`. These are checks that, if failing, mean "restart this process" (the runtime itself is dead/deadlocked/unable to serve at all).
- **No tag (default), or any other tag like `ready`** → appears under `/health`. These are checks that, if failing, mean "don't send traffic here yet" but a restart probably won't fix it (database connectivity, downstream service availability, schema migration not complete).

The Kubernetes-idiomatic distinction is the right mental model: liveness probes restart pods; readiness probes gate traffic. Even though CritterCab's production target is Azure-native rather than raw K8s (per ADR-007), the same logical split applies to Azure Container Apps' health probes and to App Service's deployment slots.

Each service then adds its own checks per `service-bootstrap`:

```csharp
// In a service's Program.cs, after AddServiceDefaults():
builder.Services.AddHealthChecks()
    // "ready" tag → /health only. Marten can't reach Postgres → don't take traffic.
    .AddCheck<MartenConnectivityCheck>("marten-postgres", tags: ["ready"])

    // Untagged → /health only. A downstream gRPC service is unreachable.
    .AddCheck<DispatchClientCheck>("dispatch-grpc");

    // "live" tag → /alive (and /health). Lock contention detected — restart fixes it.
    .AddCheck<DeadlockProbe>("deadlock", tags: ["live"]);
```

ServiceDefaults provides only the `"self"` (`live`) baseline. Per-service checks are explicit.

---

## The HttpClient resilience layer

`ConfigureHttpClientDefaults` applies to **every** `HttpClient` registered through `IHttpClientFactory` *after* `AddServiceDefaults()` is called. This is why `AddServiceDefaults()` is the first line in Program.cs and not the last — call order matters.

The standard resilience handler bundles four pipelines:

- **Rate limiter** — bounds concurrent outbound requests per client.
- **Total request timeout** — caps end-to-end attempt+retry duration (default 30s).
- **Retry** — exponential backoff with jitter, default 3 retries, only on transient failures (5xx, 408, network errors).
- **Circuit breaker** — opens after a configurable failure ratio, halts requests for a cooldown period.
- **Per-attempt timeout** — caps each individual attempt (default 10s).

These defaults are sensible starting points but project-specific tuning happens at the service level via `.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler(opts => ...))` overrides where needed. The ServiceDefaults baseline assumes services would rather get retries-with-jitter than silent failures; if a particular client needs different behavior (e.g., never retry POSTs on the payments path), that gets configured at the per-`HttpClient`-registration level.

**Critical scope caveat:** the resilience handler only attaches to `HttpClient` instances created through `IHttpClientFactory` (i.e., registered with `AddHttpClient` or `AddGrpcClient`). Raw `new HttpClient()` instances do **not** inherit these defaults. Cab's convention is "no raw HttpClient" — every outbound HTTP/gRPC client goes through DI.

---

## Service discovery in app code

`AddServiceDiscovery` in the `ConfigureHttpClientDefaults` block makes the `https+http://service-name` URL scheme work for inter-service `HttpClient` and `GrpcClient` calls:

```csharp
// In a service's Program.cs:
builder.Services.AddHttpClient<IDispatchClient, DispatchClient>(client =>
{
    client.BaseAddress = new Uri("https+http://dispatch");
});
```

Aspire's AppHost injects `services__dispatch__https__0` and `services__dispatch__http__0` configuration keys; the resolver picks the right one. The full mechanics live in `aspire` § Service discovery and connection-string injection; this skill's role is just to register the resolver so the scheme works.

**A note on production parity.** This `https+http://` convention is the Aspire-idiomatic pattern; it depends on the service-discovery resolver being present in the deployed environment too. Azure Container Apps' service discovery covers this; AKS via plain DNS does not (services would need to resolve via standard DNS hostnames from environment variables). If CritterCab's deployment target shifts away from Container Apps, the resolver registration here becomes the seam to revisit.

---

## What lives here vs. elsewhere

This boundary is the single most useful thing to internalize. When a contributor asks "should I add X to ServiceDefaults?" — the answer is almost always no, and the test is the table below.

| Concern | Lives in ServiceDefaults? | Lives where? |
|---|---|---|
| OTel SDK baseline (Logging.AddOpenTelemetry, WithMetrics, WithTracing root setup) | ✅ Yes | This skill |
| OTLP exporter registration | ✅ Yes | This skill |
| ASP.NET Core / HttpClient / Runtime / gRPC client instrumentation | ✅ Yes | This skill |
| Health-check trace filter | ✅ Yes | This skill |
| `"self"` health check (the `live` baseline) | ✅ Yes | This skill |
| `/health` and `/alive` endpoint mapping with production status-only writer | ✅ Yes | This skill |
| Service discovery resolver registration | ✅ Yes | This skill |
| HttpClient resilience defaults | ✅ Yes | This skill |
| `.AddSource("Wolverine")`, `.AddSource("Marten")`, `.AddSource("CritterCab.Trips")` | ❌ No | `observability-tracing` |
| `.AddMeter("Wolverine:*")` and Marten meter registration | ❌ No | `observability-tracing` (and eventually `observability-metrics`) |
| Per-service database connectivity health checks | ❌ No | `service-bootstrap` |
| Per-service downstream-service health checks | ❌ No | `service-bootstrap` |
| Marten `AddMarten(...)` registration | ❌ No | `service-bootstrap` |
| Polecat `AddPolecat(...)` registration | ❌ No | `service-bootstrap` |
| Wolverine `UseWolverine(...)` registration | ❌ No | `service-bootstrap` |
| Per-service `HttpClient<TInterface, TImpl>` registrations | ❌ No | `service-bootstrap` |
| `TimeProvider.System` registration | ❌ No | `service-bootstrap` (per the `csharp-coding-standards` convention) |
| Auth middleware, authn/authz config | ❌ No | `identity-acl` and per-service Program.cs |
| Per-BC custom ActivitySource definitions (`new ActivitySource("CritterCab.Trips")`) | ❌ No | The service that owns the BC |

The principle: ServiceDefaults provides the **environment** (the OTel SDK is present, the exporter is wired, health endpoints exist, HttpClient has resilience). Each service provides the **contents** (which sources to feed into that environment, which checks to register, which clients to configure).

---

## Usage in Program.cs

Detailed composition-root patterns live in `service-bootstrap`. The minimal shape ServiceDefaults expects:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();   // FIRST. Always first.

// ... per-service registrations: Marten, Wolverine, gRPC services, etc.

var app = builder.Build();

app.MapDefaultEndpoints();      // Maps /health and /alive with the right writers.

// ... per-service endpoint maps: app.MapWolverineEndpoints(), app.MapWolverineGrpcServices(), etc.

return await app.RunJasperFxCommands(args);
```

Worker services (without a `WebApplication`) follow the same pattern via `Host.CreateApplicationBuilder(args)`; `AddServiceDefaults()` works against `IHostApplicationBuilder` regardless of whether it's a web host or a generic host. `MapDefaultEndpoints()` is web-specific and only called from services that have HTTP/gRPC surfaces.

---

## Common pitfalls

- **Not adding the ServiceDefaults reference to a new service project.** The service builds, runs, and serves traffic, but emits no telemetry, has no health endpoints, and has no HttpClient resilience. The Aspire dashboard shows the resource as "starting" forever because `/health` returns 404. The new-service checklist in `adding-a-service` should include the `<ProjectReference>` line, but it's worth a CI grep too: every project under `src/CritterCab.*` (except `ServiceDefaults` itself) must reference `CritterCab.ServiceDefaults`.

- **Calling `AddServiceDefaults()` after services that use HttpClient.** `ConfigureHttpClientDefaults` only applies to clients registered *after* it. Putting `AddServiceDefaults()` later in Program.cs than `builder.Services.AddHttpClient<IDispatchClient, DispatchClient>(...)` means that client gets no resilience and no service discovery. Convention: ServiceDefaults is line 1 of `Program.cs` (after `var builder = ...`).

- **Removing the health-check trace filter "to debug something" and forgetting to put it back.** Without the filter, every `/health` poll from the Aspire dashboard (default every few seconds) and every load-balancer health probe in production becomes a span. Trace stores fill, costs rise, signal-to-noise collapses. If you genuinely need to trace health checks during a specific debugging session, do it with a per-endpoint suppression toggle or a feature flag — don't remove the filter.

- **Adding Wolverine, Marten, or per-BC ActivitySources here.** Tempting because "all the observability lives here," but wrong. ServiceDefaults is the baseline that every service inherits identically; `.AddSource("Wolverine")` and `.AddSource("Marten")` only make sense for services that actually use Wolverine and Marten. CritterCab's full topology is currently all-Marten/all-Wolverine, but the principle holds — and besides, `observability-tracing` is the authoritative skill for what the trace surface looks like. Don't bypass it.

- **Hardcoding the OTLP endpoint instead of reading `OTEL_EXPORTER_OTLP_ENDPOINT`.** Hardcoding works locally because Aspire happens to inject the value; it silently breaks in production where the deployment supplies a different endpoint. The pattern above (read the env var, opt in only if present) is the correct one.

- **Returning detailed health-check info from `/health` in production.** A health-check response that lists check names and exception messages — the ASP.NET Core default writer — is an information disclosure surface (downstream service names, internal endpoints, error patterns). The production status-only writer in `MapDefaultEndpoints` above is the safety net. If you write a custom response writer, preserve the dev/prod split.

- **Putting `AddServiceDefaults()` in a worker service that doesn't have `WebApplication`.** The extension method is generic on `IHostApplicationBuilder`, so it works. But `MapDefaultEndpoints()` is `WebApplication`-specific and won't compile against a generic `IHost`. Workers get the OTel + resilience + service-discovery half of ServiceDefaults but not the health endpoint mappings — they expose health through Wolverine's internal pinging or their own diagnostic endpoint per worker-specific need.

- **Bypassing `IHttpClientFactory` with `new HttpClient()`.** Raw `HttpClient` instances do not pick up `ConfigureHttpClientDefaults`. The retries, circuit breaker, timeout, and service-discovery resolver are all on the factory-managed clients only. Cab's convention is no raw `HttpClient`; if a code reviewer finds one, it's a finding.

- **Treating ServiceDefaults as the place for "any cross-cutting service registration."** It isn't a general bag for shared services — it's specifically the **Aspire-shaped** cross-cutting concerns (observability + discovery + resilience + health). Generic shared utilities (a common JSON serializer config, a shared `TimeProvider` registration, common DI registrations) live in their own shared library or in the per-service Program.cs, not here.

- **Forgetting to bump the OTel package versions in lockstep.** OpenTelemetry has historically had compatibility constraints across its instrumentation packages. When updating one OTel package in `Directory.Packages.props`, update all of them. Mixing major versions across OTel packages is the leading cause of "the build works but no traces appear" failures.

---

## See also

**Upstream** — generic ServiceDefaults baseline this skill builds on. ai-skills (license required, install via `npx skills add`):

- `wolverine-observability-opentelemetry-setup` — combined Wolverine + Marten OTel surface. Treats `AddServiceDefaults` as a baseline and layers JasperFx-specific sources/meters on top. This skill (`aspire-service-defaults`) is the half that comes before that layering; `observability-tracing` is the half that comes after.

**Prerequisites** — Cab-internal skills to load first:

- `csharp-coding-standards` — modern C# conventions (`required`, sealed records, `TimeProvider`) that the extension methods follow.

**Sibling skills:**

- `service-bootstrap` — what each service's `Program.cs` does after calling `AddServiceDefaults()`. The composition root the shared library makes possible.
- `observability-tracing` — Wolverine, Marten, and per-BC ActivitySource registration layered on top of the OTel baseline this skill provides.
- `aspire` — the AppHost that injects `OTEL_EXPORTER_OTLP_ENDPOINT`, `ConnectionStrings__*`, and `services__*` env vars that ServiceDefaults consumes. The two skills are halves of the same picture (AppHost side vs. service side).

**Downstream:**

- `observability-metrics` (Phase 4) — Wolverine and Marten metrics (counters, histograms) registered alongside the traces this skill enables.
- `testing-integration`, `testing-fundamentals` — Alba-based test fixtures that compose against `Program.cs` (which calls `AddServiceDefaults`). The shared library is exercised end-to-end by every integration test.
- `adding-a-service` — the new-service skeleton; the `<ProjectReference>` to `CritterCab.ServiceDefaults` is part of the template every new service starts from.

**External:**

- [Aspire Service Defaults documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults) — the canonical pattern this skill specializes for CritterCab.
- [Microsoft.Extensions.Http.Resilience overview](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) — what `AddStandardResilienceHandler` actually does, with the policy defaults and override surface.
- [OpenTelemetry .NET getting started](https://opentelemetry.io/docs/languages/dotnet/getting-started/) — the SDK this skill wires.
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks) — the tagging convention, response writers, and policy surface.
