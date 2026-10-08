---
name: service-bootstrap
description: "Composition root patterns for a CritterCab service Program.cs — Aspire-injected configuration and the two connection-string guard shapes (mandatory vs optional), Wolverine + Marten/Polecat wiring, transport routing rules, observability bootstrap, health checks, and the per-service hosting contract. Use when authoring or modifying any service's Program.cs."
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
- Per-store patterns — see `marten-aggregates` (Phase 2) or `polecat-event-sourcing` (archived).
- Per-transport routing-rule syntax — see `wolverine-kafka`, `wolverine-grpc-handlers`, or `wolverine-azure-service-bus` (archived).

---

## The Service Composition Contract

Every CritterCab service's `Program.cs` does the same six things, in roughly the same order:

1. **Build the host** with `WebApplication.CreateBuilder(args)` and pull Aspire-injected configuration.
2. **Register Wolverine** with the service's specific configuration (handlers, transports, routing rules).
3. **Wire the store** — Marten or Polecat — including event-type registration and Wolverine integration.
4. **Configure observability** — distributed tracing, metrics, logging. Neither shipped service configures OpenTelemetry yet; see § Observability Bootstrap.
5. **Map endpoints** — health checks, gRPC services, HTTP endpoints (per service's contract surface).
6. **Run** the host.

Services are free to layer additional configuration in (CORS, auth, rate-limiting, custom middleware) but those six are the universal floor. The patterns below show the canonical shape of each.

---

## Marten-Backed Service: Canonical Program.cs

The full shape for a Marten-backed service, taken from `src/CritterCab.Telemetry/Program.cs` because it exercises the most of the contract — an event-sourced config stream, a gRPC ingest, a Kafka publish, and Wolverine.HTTP endpoints. Comments are abbreviated and two blocks are elided where marked. Dispatch (`src/CritterCab.Dispatch/Program.cs`) has the same shape with a Kafka listener in place of the publisher.

```csharp
using CritterCab.Telemetry.LastKnownPosition;
using CritterCab.Telemetry.ReportLocations;
using CritterCab.Telemetry.TelemetryPolicy;
using CritterCab.Telemetry.V1;
using JasperFx;
using Wolverine.Grpc;
using Marten;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Kafka;
using Wolverine.Marten;
using Wolverine.Protobuf;

var builder = WebApplication.CreateBuilder(args);

// Marten + event sourcing — optional guard; see § Connection-String Guards.
var connectionString = builder.Configuration.GetConnectionString("crittercab_telemetry");

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddMarten(opts =>
    {
        opts.Connection(connectionString);

        opts.AutoCreateSchemaObjects = builder.Environment.IsDevelopment()
            ? AutoCreate.CreateOrUpdate
            : AutoCreate.None;

        opts.Events.UseMandatoryStreamTypeDeclaration = true;

        opts.Events.AddEventType<TelemetryPolicyConfigured>();

        opts.Projections.LiveStreamAggregation<TelemetryPolicy>();
    })
    .IntegrateWithWolverine()
    .UseLightweightSessions()
    // Config-as-events bootstrap seed (ADR-011).
    .InitializeWith<TelemetryPolicyBootstrap>();

    // Inside the guard on purpose: the sweep needs a document store to sweep.
    builder.Services.AddHostedService<LastKnownPositionEvictionService>();
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();
builder.Services.AddWolverineHttp();

// gRPC ingest: AddWolverineGrpc registers the codegen behind the [WolverineGrpcService] stub.
builder.Services.AddGrpc();
builder.Services.AddWolverineGrpc();

// (elided: AddHttpContextAccessor + the IDriverPrincipalAccessor seam.)

// One flag, read once, used by both the registration below and the transport wiring in UseWolverine.
var kafkaEnabled = !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("kafka"));

if (kafkaEnabled)
{
    builder.Services.AddScoped<IDriverLocationPublisher, KafkaDriverLocationPublisher>();
}
else
{
    builder.Services.AddSingleton<IDriverLocationPublisher, LoggingDriverLocationPublisher>();
}

// (elided: JsonStringEnumConverter via ConfigureSystemTextJsonForWolverineOrMinimalApi.)

builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "Telemetry";

    opts.UseFluentValidation();

    // Guarded rather than early-returned: an early `return` here would silently swallow any
    // Wolverine configuration appended below it whenever no broker is configured.
    if (kafkaEnabled)
        ConfigureKafkaPublishing(opts);
});

static void ConfigureKafkaPublishing(WolverineOptions opts)
{
    // The broker address is read by NAME — Aspire injects it under "kafka".
    opts.UseKafkaUsingNamedConnection("kafka")
        .AutoProvision();

    opts.Durability.UseSyncRetryBlock = true;

    // Cross-service publication: every integration event must be routed.
    // See wolverine-messaging-handlers § The Routing Rule Pre-Flight.
    opts.PublishMessage<DriverLocationUpdated>()
        .ToKafkaTopic("telemetry.driver-location-updated")
        .SendInline()
        .UseIdempotentProducer()
        .UseProtobufSerialization();
}

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapWolverineEndpoints(opts => opts.UseFluentValidationProblemDetailMiddleware());

app.MapWolverineGrpcServices();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "CritterCab Telemetry API"));
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

return await app.RunJasperFxCommands(args);

public partial class Program { }
```

> **Greenfield Marten + Wolverine recommendations beyond what's shown above.** ai-skills `critterstack-arch-new-project-wolverine-marten` documents additional greenfield-recommended options Cab's example doesn't enumerate: `EventAppendMode.Quick` (~50% throughput improvement), `UseArchivedStreamPartitioning`, `EnableEventSkippingInProjectionsOrSubscriptions`, `Projections.UseIdentityMapForAggregates`, `Projections.EnableAdvancedAsyncTracking`, `DisableNpgsqlLogging`, plus Wolverine durability optimizations (`Durability.EnableInboxPartitioning`, `InboxStaleTime`/`OutboxStaleTime`, `UnknownMessageBehavior = DeadLetterQueue`) and the `Policies.AutoApplyTransactions()` policy. For document-store index registration (which Cab's bootstrap example doesn't cover), see ai-skills `marten-advanced-indexes-and-query-optimization`. The `WolverineFx.Http.Marten` metapackage convention and the version-alignment warning across all `WolverineFx.*` packages are also documented there.

**Key conventions visible above:**

- **No `AddServiceDefaults()`.** Cab has no `ServiceDefaults` project (`aspire-service-defaults` is archived). Each service registers `AddHealthChecks()` and maps `/health` itself; no service configures OpenTelemetry or Aspire's service-discovery extensions yet.
- **Connection strings come from configuration, read by the AppHost resource name, under one of two guard shapes.** Aspire injects them via the `WithReference()` calls in the AppHost. The rule that picks the shape: **mandatory `?? throw`** when the dependency is load-bearing on every code path; **optional `if (!string.IsNullOrEmpty(...))`** when there is a meaningful degraded mode the service is expected to run in. Both shipped services take the optional shape for Marten and Kafka. See § Connection-String Guards.
- **A dependency flag is read once.** `kafkaEnabled` drives both the service registration and the transport wiring inside `UseWolverine`, so the two cannot drift apart.
- **Schema management is environment-aware.** Development services auto-create; non-development services run schema migrations explicitly (typically as a `dotnet run -- db-apply` command, not at startup).
- **`UseMandatoryStreamTypeDeclaration = true`** is on in every Cab Marten store. Omitting an `AddEventType<T>()` registration produces silent null returns from aggregate loads — the most consequential Marten footgun, fully prevented by this setting.
- **Routing rules are explicit at the composition root**, one per integration event type. The skill responsible for catching missing routing rules is `wolverine-messaging-handlers` § The Routing Rule Pre-Flight.
- **`app.RunJasperFxCommands(args)` is the entry point**, not `app.Run()`. This enables the JasperFx CLI surface (`db-apply`, `codegen-write`, `wolverine-diagnostics`, etc.). See `cli-jasperfx`. Returns `Task<int>`, so use `return await app.RunJasperFxCommands(args);` in top-level statements.
- **`public partial class Program { }`** closes the file so the test projects can address the entry point with `AlbaHost.For<Program>()`.

**Two options neither shipped service sets.** An environment-aware `ServiceLocationPolicy` and `CritterStackDefaults` code-generation modes are common Wolverine bootstrap options, and the pitfalls below still refer to the first. No Cab service sets either today; the shape below is **illustrative** and must be re-verified against the pinned Wolverine version before a service adopts it:

```csharp
// Illustrative — not in any shipped Program.cs.
builder.Host.UseWolverine(opts =>
{
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
```

---

## Connection-String Guards: Two Shapes

Every connection-string read takes one of two shapes, and the choice is made per dependency, not per service.

**Optional — the service has a degraded mode it is expected to run in.** Register the dependency and its consumers inside a non-empty check, and register a fallback in the `else` branch. This is the shape both shipped services use for both of their dependencies:

```csharp
// src/CritterCab.Dispatch/Program.cs (abbreviated)
var connectionString = builder.Configuration.GetConnectionString("crittercab_dispatch");

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddMarten(opts => { opts.Connection(connectionString); /* ... */ })
        .IntegrateWithWolverine(/* ... */)
        .UseLightweightSessions();
}

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddScoped<INearbyAvailableDriversSource, NearbyAvailableDriversView>();
}
else
{
    builder.Services.AddSingleton<INearbyAvailableDriversSource, NearbyAvailableDriversStub>();
}

var kafkaEnabled = !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("kafka"));

builder.Host.UseWolverine(opts =>
{
    // ...
    if (kafkaEnabled)
        ConfigureKafkaListening(opts);
});
```

The degraded modes are what earn the shape: a database-less or broker-less `dotnet run` of one service still boots and does something useful (Telemetry logs what it would have published; Dispatch answers from a stub), and the non-Kafka test suites run without a broker. Three details travel with it:

- **Fallback lifetimes may differ from the real implementation, on purpose.** `KafkaDriverLocationPublisher` and `NearbyAvailableDriversView` are scoped because they depend on scoped services (`IMessageBus`, `IQuerySession`); `LoggingDriverLocationPublisher` and `NearbyAvailableDriversStub` hold nothing scoped and stay singletons.
- **Read the flag once.** A second, independent branch on the same connection string can drift into the state that fails silently — a real publisher registered against a transport that was never configured, which fails at the first publish rather than at startup.
- **Guard inside `UseWolverine`; do not early-`return`.** An early `return` in the configuration lambda silently drops every line appended after it whenever the dependency is absent.

**Mandatory — the dependency is load-bearing on every code path.** If no code path is useful without it, a missing value is a misconfiguration and the host should refuse to start:

```csharp
// Illustrative — no shipped service has a dependency without a degraded mode yet.
var connectionString = builder.Configuration.GetConnectionString("crittercab_<service_name>")
    ?? throw new InvalidOperationException("Missing connection string: crittercab_<service_name>");
```

Neither shape is forced by the test harness. The Alba fixtures supply connection strings with `builder.UseSetting("ConnectionStrings:...")`, which lands before `Program.cs` reads configuration, so a mandatory read sees the Testcontainer's value. The selector is the degraded mode, nothing else.

---

## Polecat-Backed Service: The Differences

No Polecat-backed service exists yet (Polecat enters with the Driver Profile service), so this section is **illustrative** and its APIs must be re-verified against the pinned Polecat version when that service lands. Most of the bootstrap above carries over. The snippet shows the mandatory guard shape for contrast with the Marten example; pick the real service's shape by § Connection-String Guards. The differences:

```csharp
// Illustrative — no Polecat service exists yet.
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
- **Async daemon API differs slightly.** Polecat's daemon configuration follows its own surface; see `polecat-event-sourcing` (archived).
- **Handlers use `PolecatOps.StartStream<T>` instead of `MartenOps.StartStream<T>`.** See `wolverine-handlers` § Anti-Pattern: Starting a New Stream Without Returning IStartStream.

The rest of the composition root — Wolverine configuration, transport routing, observability, health checks, `RunJasperFxCommands` — is identical between Marten and Polecat services.

---

## Typed Configuration with `IOptions<T>` and `ValidateOnStart`

Connection strings flow through `IConfiguration` as single values read directly with `GetConnectionString(...)` (as shown in the canonical Program.cs above). Richer configuration — feature flags, external API credentials, tuning parameters, BC-specific behavioral toggles — should bind to a typed options class with validation that fails at startup if any required value is missing or malformed.

No shipped service binds a typed options section yet; the example below is illustrative. The pattern:

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

**Connection strings stay with `GetConnectionString(...)`,** under one of the two shapes in § Connection-String Guards — they are first-class in `IConfiguration` and read directly. The typed-options pattern is for richer configuration sections — anything that would otherwise be a clump of `builder.Configuration["Foo:Bar"]` reads.

**Aspire AppHost emits the values; the service binds them.** Per the `aspire` skill, the AppHost translates resource outputs into explicit configuration via `.WithReference(...)` (for connection strings) or `.WithEnvironment("PaymentsGateway__ApiKey", ...)` (for typed-options sections). The double-underscore separator maps to the colon-separated configuration path, so `PaymentsGateway__BaseUrl` becomes `PaymentsGateway:BaseUrl`, which `BindConfiguration("PaymentsGateway")` picks up. The service stays Aspire-agnostic — it binds to configuration keys, not to Aspire abstractions.

---

## Per-Service Configuration Variation

Beyond the canonical shape, each service makes a small number of decisions specific to its contract surface. The composition root encodes those decisions in one place.

### Service that exposes gRPC

Add `WolverineFx.Grpc`, register the gRPC services before building the host, and map them after `app.Build()`. Nothing goes in the `UseWolverine` block. Detailed patterns for the handler shapes live in `wolverine-grpc-handlers`; the bootstrap-side wiring, as Telemetry ships it:

```csharp
// Before app.Build():
builder.Services.AddGrpc();
builder.Services.AddWolverineGrpc();   // codegen: abstract [WolverineGrpcService] stub → concrete service

// After app.Build():
app.MapWolverineGrpcServices();   // discovers the stubs and maps the generated services
```

gRPC rides the service's HTTPS endpoint via Kestrel HTTP/2; no extra endpoint is declared in the AppHost.

### Service that exposes Wolverine.HTTP endpoints

Add `WolverineFx.Http`, register HTTP services before building the host, and map the endpoints after `app.Build()`. The `AddWolverineHttp()` registration is required — without it, `MapWolverineEndpoints()` throws at startup.

```csharp
// Before app.Build():
builder.Services.AddWolverineHttp();

// After app.Build():
app.MapWolverineEndpoints();   // maps all [WolverinePost]/[WolverineGet]/etc. handlers
```

Detailed handler shapes for HTTP endpoints live in `wolverine-http-handlers`.

### Service that consumes Kafka

Add `WolverineFx.Kafka` and read the broker by **name** with `UseKafkaUsingNamedConnection("kafka")` — Aspire injects the address under the `kafka` key, and the same code then runs against the local container, the test Testcontainer, and Azure Event Hubs. Both shipped services wire Kafka inside a local function called under the `kafkaEnabled` guard (§ Connection-String Guards). Dispatch's listener, abbreviated:

```csharp
// src/CritterCab.Dispatch/Program.cs
static void ConfigureKafkaListening(WolverineOptions opts)
{
    opts.UseKafkaUsingNamedConnection("kafka");   // no AutoProvision: the producer owns the topic

    opts.ListenToKafkaTopic("telemetry.driver-location-updated")
        .ConfigureConsumer(c => c.GroupId = "dispatch")
        .BeginAtLatest()
        .UseProtobufSerialization()
        .DefaultIncomingMessage<DriverLocationUpdated>();
}
```

Consumer-group, cold-start position, and serialization choices are explained in `wolverine-kafka`.

### Service that publishes only, doesn't consume

A service that only publishes integration events (no inbound subscriptions from cross-service traffic) still requires the routing rules but does not register inbound listeners. Telemetry is this shape on Kafka: `UseKafkaUsingNamedConnection("kafka").AutoProvision()` plus one `PublishMessage<DriverLocationUpdated>().ToKafkaTopic("telemetry.driver-location-updated")` rule, and no `ListenToKafkaTopic` — see the canonical Program.cs above.

### BFF service (gRPC client to other services)

A service acting as a backend-for-frontend that calls other services via gRPC registers the gRPC client(s) it consumes. No such service exists yet; the snippet is illustrative, and it uses the mandatory guard shape because a BFF with no callee address has no useful degraded mode. The pattern is per-callee:

```csharp
// Illustrative — no shipped service registers a gRPC client yet.
builder.Services.AddGrpcClient<DispatchService.DispatchServiceClient>(opts =>
{
    opts.Address = new Uri(builder.Configuration["DispatchServiceUrl"]
        ?? throw new InvalidOperationException("Missing DispatchServiceUrl"));
});
```

The actual BFF patterns and gRPC client conventions are out of scope for this skill — see `wolverine-grpc-handlers` (Phase 3) for the handler-side, and a future BFF-specific skill for the BFF architecture.

---

## Observability Bootstrap

Neither shipped service configures observability beyond ASP.NET Core's default logging. There is no `ServiceDefaults` project and no `AddServiceDefaults()` call, so services export no OpenTelemetry traces or metrics to the Aspire dashboard; it shows their resource state and console logs. Step 4 of the contract is therefore unfilled in shipped code.

When it is filled, the shape is an OpenTelemetry registration in each service (or a shared `ServiceDefaults` project) that exports over the OTLP endpoint Aspire injects. The detailed patterns — span attributes, context propagation across gRPC, correlation between Marten-recorded events and traces — live in `observability-tracing` and `aspire-service-defaults`, both archived until the first exercising code lands; re-source them against the pinned packages then.

---

## Health Check Conventions

Each service registers health checks and maps one endpoint itself:

```csharp
// Both shipped Program.cs files.
builder.Services.AddHealthChecks();

// After app.Build():
app.MapHealthChecks("/health");
```

No custom checks are registered and there is no separate `/alive` endpoint. When a service needs a liveness/readiness split, add tagged checks and a second mapping filtered by tag — illustrative only, nothing ships it:

```csharp
// Illustrative — no shipped service does this yet.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
```

Production deployments typically map such endpoints to load-balancer health probes — readiness gates traffic, liveness gates restart.

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
- **Choosing the connection-string guard shape by habit.** `?? throw` on a dependency that has a degraded mode makes a broker-less or database-less run impossible; an optional guard on a dependency with no degraded mode boots a service that fails at its first request instead of at startup. Pick by § Connection-String Guards.
- **Branching on the same connection string twice.** One branch registers the real implementation, a second wires the transport, and the two drift. Read a flag once (`kafkaEnabled`) and use it for both.
- **Early `return` inside `UseWolverine` when a dependency is absent.** Every line appended after it is silently skipped in the degraded mode. Guard the call (`if (kafkaEnabled) ConfigureKafkaPublishing(opts);`) instead.
- **Missing `TimeProvider` DI registration.** Handlers that inject `TimeProvider` (the canonical timestamp pattern per `csharp-coding-standards`) fail at runtime if `TimeProvider.System` isn't registered in DI. Register explicitly: `builder.Services.AddSingleton(TimeProvider.System);`.
- **Missing `AddWolverineHttp()` for services that map Wolverine endpoints.** `MapWolverineEndpoints()` throws at startup if `AddWolverineHttp()` wasn't called. See § Per-Service Configuration Variation, "Service that exposes Wolverine.HTTP endpoints".
- **Hardcoding connection strings.** Aspire injects them via configuration. Hardcoding works in dev for the wrong reasons (the Aspire-provisioned database happens to listen on the same port) and fails on every deploy. Always read from `builder.Configuration.GetConnectionString(...)`.
- **Calling `IntegrateWithWolverine()` before `AddMarten`/`AddPolecat`.** Order matters; the `IntegrateWithWolverine` call extends the registration that came immediately before it. If the call chain is interrupted, the integration silently doesn't wire.

- **Omitting `.ValidateOnStart()` on typed options.** Validation deferred to first-use means a missing or malformed config value surfaces as a confusing downstream failure (401 response, null reference, malformed URI) instead of as a clear startup error. Always call `.ValidateOnStart()` after `.ValidateDataAnnotations()`; the cost is one extra line and the benefit is failing fast at boot rather than three layers deep at runtime.

---

## See also

**Upstream** — generic Wolverine + Marten/Polecat bootstrap fundamentals this skill builds on. ai-skills (license required, install via `npx skills add`):

- `critterstack-arch-new-project-wolverine-marten` (primary) — Wolverine + Marten greenfield bootstrap: NuGet metapackage convention with version-alignment warning, full canonical Program.cs with greenfield-recommended event-store options (`EventAppendMode.Quick`, `UseArchivedStreamPartitioning`, `EnableEventSkippingInProjectionsOrSubscriptions`, `Projections.UseIdentityMapForAggregates`, `Projections.EnableAdvancedAsyncTracking`, `DisableNpgsqlLogging`), Wolverine durability optimizations (`Durability.EnableInboxPartitioning`, `InboxStaleTime`/`OutboxStaleTime`, `UnknownMessageBehavior = DeadLetterQueue`), `Policies.AutoApplyTransactions()`, full Alba test fixture pattern with vertical slice example, anti-patterns (repository over Marten, missing AutoApplyTransactions, manual SaveChangesAsync). Cab's skill adds project-specific framing (6-step Service Composition Contract, Aspire-injected configuration with the two connection-string guard shapes, environment-aware schema/policy decisions, connection-string casing conventions, per-service variation patterns for gRPC/Kafka/publish-only/BFF, Polecat-backed differences, Cab-specific pitfalls).
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

- `aspire` — AppHost wiring, the `src/CritterCab.AppHost` project, resource composition, port allocation.
- `cli-aspire` (Phase 2) — `aspire run`, `aspire describe`, dashboard.
- `cli-jasperfx` (Phase 2) — `db-apply`, `codegen-write`, `wolverine-diagnostics`, the full JasperFx CLI surface this bootstrap exposes.
- `marten-aggregates` (Phase 2) — what to do once `AddMarten` is wired.
- `polecat-event-sourcing` (archived) — what to do once `AddPolecat` is wired.
- `marten-projections` (Phase 2) — projection registration. Every shipped projection is inline or live; no service calls `AddAsyncDaemon` (`marten-async-daemon` is archived).
- `observability-tracing` (archived) — distributed tracing patterns, for when a service first configures OpenTelemetry.
- `aspire-service-defaults` (archived) — a shared `ServiceDefaults` project; none exists.
- `testing-fundamentals` (Phase 2) — Alba-based test fixtures over this `Program.cs`.

**External:**

- ADR-002 in [`docs/decisions/`](../../decisions/) — services per bounded context.
- ADR-005 in [`docs/decisions/`](../../decisions/) — transport selection.
- ADR-007 in [`docs/decisions/`](../../decisions/) — Azure as deployment target.
- [Aspire Service Defaults documentation](https://learn.microsoft.com/dotnet/aspire/fundamentals/service-defaults).
