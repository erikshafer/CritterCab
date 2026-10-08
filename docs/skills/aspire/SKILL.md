---
name: aspire
description: "Aspire 13.4 local-dev orchestration for CritterCab — the AppHost project at src/CritterCab.AppHost (Aspire.AppHost.Sdk under Central Package Management), what gets provisioned (Postgres, Kafka), how Cab services compose against it (WithReference, WaitFor), deterministic port pinning, connection-string injection and the Program.cs guard shapes, the dashboard, and integration with the Aspire MCP server for AI coding agents (`aspire agent init`). Use when authoring or modifying the Cab AppHost, adding a new service or infrastructure resource, debugging dev-time orchestration, or wiring a service's connection-string consumption."
cluster: infrastructure
tags: [aspire, apphost, local-dev, service-discovery, postgres, kafka, mcp, dotnet-10, central-package-management, port-allocation]
---

# Aspire — Local Dev Orchestration

Aspire is Cab's local development orchestration layer. One project, `src/CritterCab.AppHost`, declares every container Cab needs (Postgres and Kafka), every Cab service (Dispatch and Telemetry), and the relationships between them. `dotnet run --project src/CritterCab.AppHost` (or `aspire run`) provisions the containers, starts the services with connection strings injected, and serves a dashboard that lets you watch the system run.

Aspire is **local-dev only**. Production deployment is Azure-native per `ADR-007`; integration tests use Testcontainers per `testing-integration` and never bootstrap the AppHost. The AppHost's job is to make `F5` produce a fully-wired distributed system on a developer laptop and nothing more.

The Cab AppHost is built on Aspire 13.4.6. The 13.x traits worth flagging up front:

- **AppHost project on `Aspire.AppHost.Sdk`.** `<Project Sdk="Aspire.AppHost.Sdk/13.4.6">` carries `Aspire.Hosting.AppHost` implicitly; the hosting integrations take their versions from `Directory.Packages.props` like every other project in the solution.
- **Aspire MCP server** (`aspire agent init`) wires the running AppHost into Claude Code and other agents — first-class for Cab's Claude-driven workflow.
- **Service discovery** environment variable naming changed: keys are now scheme-based (`services__myservice__https__0`), not endpoint-name-based (breaking change from 13.1).
- **TypeScript AppHost** is preview and parked with the frontend — see § Parked: TypeScript AppHost for the frontend.

---

## When to apply this skill

Use this skill when:

- Modifying the Cab AppHost (`src/CritterCab.AppHost/AppHost.cs` or its `.csproj`).
- Adding a new Cab service to the AppHost.
- Adding a new infrastructure resource (SQL Server, Azure Service Bus emulator, Redis, etc.).
- Wiring a service's `Program.cs` to consume Aspire-injected connection strings.
- Diagnosing dev-time orchestration issues (services starting before dependencies, missing connection strings, dashboard URL).
- Setting up Aspire MCP integration for Claude Code.

Do NOT use this skill for:

- The Aspire CLI surface (`aspire run`, `aspire start`, `aspire describe`, `aspire wait`, `aspire doctor`) — `cli-aspire`.
- Integration test composition — `testing-integration`. The AppHost is not run in tests.
- Production deployment to Azure — `ADR-007` and forward-looking deployment skills.
- Wolverine transport configuration consumed by services — `wolverine-kafka`, `wolverine-grpc-handlers`.
- Service-side connection-string consumption beyond the immediate startup wiring — `service-bootstrap`.

---

## What Cab's AppHost does

Concretely, `src/CritterCab.AppHost` is the entry point for `aspire run`. Running it:

1. **Provisions infrastructure containers** — Postgres (one container, one database per Marten service) and a Kafka broker for the Telemetry → Dispatch position feed.
2. **Starts each Cab service** with connection strings, service-discovery configuration, and OTLP telemetry endpoints injected via environment variables.
3. **Coordinates startup ordering** — services wait for their dependencies via `.WaitFor(...)` before starting.
4. **Serves the Aspire dashboard** at a URL printed in the terminal (with a one-time login token), showing live resource state, structured logs, distributed traces, and metrics for the entire system.
5. **Exposes an MCP server** (when configured via `aspire agent init`) that lets Claude Code query resource state, read logs, and inspect telemetry directly from a running AppHost.

The dashboard and the MCP server are both manifestations of the same thing: Aspire knows the topology, knows the runtime state, and surfaces it to humans (dashboard) and agents (MCP).

---

## The committed Aspire 13.4 packages

The AppHost is an ordinary project under the repo's Central Package Management. The SDK version lives in the csproj's `Project` element; the hosting integrations are versionless `PackageReference`s pinned in `Directory.Packages.props`.

| Package | Pinned in | Version | Used for |
|---|---|---|---|
| `Aspire.AppHost.Sdk` | `<Project Sdk="...">` in `CritterCab.AppHost.csproj` | 13.4.6 | The AppHost SDK itself. Carries `Aspire.Hosting.AppHost` implicitly. |
| `Aspire.Hosting.PostgreSQL` | `Directory.Packages.props` | 13.4.6 | The Postgres container and the per-service databases. |
| `Aspire.Hosting.Kafka` | `Directory.Packages.props` | 13.4.6 | The Kafka broker container. |

> **Why `Aspire.Hosting.AppHost` appears nowhere.** The Aspire 13 SDK adds it as an implicit reference. Pinning it in `Directory.Packages.props` collides with that implicit reference (NU1009) and the AppHost fails to restore — verified empirically. Do not add a `PackageVersion` for it, and do not opt the AppHost out of CPM to work around the collision.

**Not yet committed; each enters with its first exercising code:**

- `Aspire.Hosting.SqlServer` — for Polecat, which enters with the Driver Profile service. Port `5391` is reserved for it (see § Port allocation).
- `Aspire.Hosting.Azure.ServiceBus` (emulator) — Azure Service Bus enters at the driver-availability slice. Port `5393` is reserved for it.

Keep the version line uniform: the `Sdk` attribute in the csproj and every `Aspire.Hosting.*` `PackageVersion` on the same 13.4.x version, bumped in the same change. The csproj carries a comment saying so.

---

## The AppHost project

The AppHost lives at `src/CritterCab.AppHost/` and is listed in `CritterCab.slnx`, so `dotnet build CritterCab.slnx` in CI builds it like any other project and the solution-completeness guard sees it.

```
src/CritterCab.AppHost/
  CritterCab.AppHost.csproj
  AppHost.cs
  Properties/launchSettings.json
```

### `CritterCab.AppHost.csproj`

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.4.6">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <UserSecretsId>crittercab-apphost</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.PostgreSQL" />
    <PackageReference Include="Aspire.Hosting.Kafka" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CritterCab.Dispatch\CritterCab.Dispatch.csproj" />
    <ProjectReference Include="..\CritterCab.Telemetry\CritterCab.Telemetry.csproj" />
  </ItemGroup>

</Project>
```

(The shipped file carries explanatory comments, elided here.)

- **`Sdk="Aspire.AppHost.Sdk/13.4.6"`** — the AppHost SDK. No `IsAspireHost` property and no `Aspire.Hosting.AppHost` reference: the 13.x SDK supplies both.
- **`UserSecretsId` is a stable literal**, not a generated GUID. Aspire stores generated parameters — the Postgres password among them — in user secrets; a stable id keeps them across machines and renames, so they keep matching the persistent containers between runs. Changing it loses the stored parameters, and the next run generates fresh ones that no longer match what the persistent containers were created with.
- **Versionless `PackageReference`s** — versions come from `Directory.Packages.props`.
- **One `ProjectReference` per orchestrated service.** Each generates a strongly-typed `Projects.*` accessor at build time; underscores in the type name correspond to dots in the project name (`CritterCab.Dispatch` → `Projects.CritterCab_Dispatch`).

### `AppHost.cs`

The resources, as shipped (comments abbreviated):

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// === Infrastructure ===

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("18-alpine")
    .WithHostPort(5390)
    .WithLifetime(ContainerLifetime.Persistent);

var dispatchDb = postgres.AddDatabase("crittercab_dispatch");
var telemetryDb = postgres.AddDatabase("crittercab_telemetry");

// The port is a constructor argument, not a .WithHostPort(...) call — on
// Aspire 13.4.6 WithHostPort belongs to the Kafka UI container resource, not the broker.
var kafka = builder.AddKafka("kafka", port: 5392)
    .WithLifetime(ContainerLifetime.Persistent);

// === Services ===
// launchProfileName: null — the AppHost-declared endpoints are authoritative.

builder.AddProject<Projects.CritterCab_Dispatch>("dispatch", launchProfileName: null)
    .WithHttpsEndpoint(port: 5310, name: "https")
    .WithHttpEndpoint(port: 5311, name: "http")
    .WithReference(dispatchDb)
    .WaitFor(dispatchDb)
    .WithReference(kafka)
    .WaitFor(kafka);

builder.AddProject<Projects.CritterCab_Telemetry>("telemetry", launchProfileName: null)
    .WithHttpsEndpoint(port: 5315, name: "https")
    .WithHttpEndpoint(port: 5316, name: "http")
    .WithReference(telemetryDb)
    .WaitFor(telemetryDb)
    .WithReference(kafka)
    .WaitFor(kafka);

builder.Build().Run();
```

The dashboard, OTLP, resource-service, and MCP endpoints (`5300–5307`) are pinned separately in `src/CritterCab.AppHost/Properties/launchSettings.json` — see § Port allocation.

Run it with `dotnet run --project src/CritterCab.AppHost` from the repo root, or `aspire run`.

### Resource registration patterns

- **`builder.AddPostgres("postgres")`** — adds a Postgres container resource named `postgres`. The name flows into the service-discovery namespace.
- **`postgres.AddDatabase("crittercab_dispatch")`** — declares a logical database within the Postgres container. Aspire creates it on startup; the database resource's name is the connection-string key the service reads (`GetConnectionString("crittercab_dispatch")`). The returned `IResourceBuilder<PostgresDatabaseResource>` is what a service `.WithReference(...)`s.
- **`builder.AddKafka("kafka", port: 5392)`** — Kafka broker container; resource name is the connection-string key (`GetConnectionString("kafka")`, which is also the name `UseKafkaUsingNamedConnection("kafka")` reads). The host port is a constructor argument.
- **`builder.AddProject<Projects.CritterCab_Dispatch>("dispatch", launchProfileName: null)`** — adds a Cab service project via the accessor its `ProjectReference` generated.
- **`.WithReference(resource)`** — injects connection-string and service-discovery configuration for `resource` into the project's environment.
- **`.WaitFor(resource)`** — delays project startup until `resource` reports healthy. Without it, the project starts before its dependencies and fails — or, for a service with an optional guard (see § The Program.cs guard pattern), starts against a dependency that is not ready yet.
- **`.WithLifetime(ContainerLifetime.Persistent)`** — keeps the container running across AppHost restarts. Both Cab containers use it: Postgres to avoid slow re-provisioning, Kafka so a restart does not discard the topic.

---

## Port allocation

CritterCab pins **deterministic local-dev ports** instead of taking Aspire's random high-port assignment. The reason is collision avoidance: several sibling projects run in parallel on the same developer machine, and Aspire's default random ports collide unpredictably when AppHosts spin up and down. Pinning a compact, project-specific band makes "is CritterCab already running?" answerable by looking at a port, and lets two projects' AppHosts run side by side without contention.

### Cross-project band registry

Each project on the machine owns a distinct `5Nxx` band. CritterCab is `53xx`.

| Band | Owner |
|---|---|
| `51xx` (services) + `*090` dashboard family | a sibling project |
| `52xx` | a sibling project |
| **`53xx`** | **CritterCab** |
| `54xx`, `55xx`, … | free |

### CritterCab's `53xx` map

```
AppHost / dashboard  (pinned in src/CritterCab.AppHost/Properties/launchSettings.json)
  5300 / 5301   dashboard          https / http
  5302 / 5303   OTLP               (ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL / _HTTP_…)
  5304 / 5305   resource service   https / http   (ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL)
  5306 / 5307   MCP endpoint       https / http   (ASPIRE_DASHBOARD_MCP_ENDPOINT_URL)

Services  (pinned in AppHost.cs via WithHttpsEndpoint / WithHttpEndpoint)
  5310 5311 5312   Dispatch        https / http / grpc-reserved
  5315 5316 5317   Telemetry       https / http / grpc-reserved
  5320 5321 5322   (next service)
  ...              (5-apart → 16 slots)

Infra host ports  (pinned in AppHost.cs)
  5390 Postgres     (WithHostPort)
  5392 Kafka        (AddKafka port: constructor argument)
  5391 SQL Server   reserved, no resource yet
  5393 ASB emulator reserved, no resource yet
```

### The two pinning mechanisms

Ports are pinned in two places:

1. **Service endpoints and infra host ports → `AppHost.cs` (code).** Each service is added with `launchProfileName: null` (the service has no launch profile, so the AppHost-declared endpoints are authoritative) and pinned with `.WithHttpsEndpoint(port: slot, name: "https")` / `.WithHttpEndpoint(port: slot + 1, name: "http")`. Postgres uses `.WithHostPort(5390)`. Kafka takes its host port as the `port:` argument to `AddKafka` — on 13.4.6 `.WithHostPort` on a Kafka builder binds to the Kafka UI resource, and `AddKafka("kafka").WithHostPort(5392)` fails with CS1929. Check each new integration's builder type before reaching for `WithHostPort`.

2. **Dashboard / OTLP / resource / MCP → `src/CritterCab.AppHost/Properties/launchSettings.json`.** This is the AppHost project's launch profile; `dotnet run` and `aspire run` apply the first profile (`https`) by default. The dashboard URL comes from `applicationUrl` (→ `ASPNETCORE_URLS`); the OTLP/resource/MCP endpoints come from the `ASPIRE_DASHBOARD_*` / `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL` environment variables. The second profile, `http`, pins the same band over plain HTTP and sets `ASPIRE_ALLOW_UNSECURED_TRANSPORT`.

### The slot convention (for `adding-a-service`)

> Each new service claims the **next free `+5` slot** starting at `5310`. Ports are `slot` (https), `slot + 1` (http), `slot + 2` (reserved gRPC). gRPC normally rides the HTTPS endpoint via Kestrel HTTP/2 multiplexing — Telemetry's `ReportLocations` ingest does exactly that — so `slot + 2` is reserved and used only if a service needs a dedicated gRPC listener.

Dispatch holds `5310`, Telemetry `5315`. The next service to land takes `5320`, the one after `5325`, and so on. The `adding-a-service` skill references this convention; pin the new service's endpoints in `AppHost.cs` as part of registering it with the AppHost.

---

## Service discovery and connection-string injection

Aspire injects resource configuration into each project's environment as standard `IConfiguration` keys. Two flavors matter for Cab.

### Connection strings

`.WithReference(postgresDatabase)` produces a configuration entry the service reads via `builder.Configuration.GetConnectionString(<resource-name>)`:

```csharp
// src/CritterCab.Dispatch/Program.cs
var connectionString = builder.Configuration.GetConnectionString("crittercab_dispatch");
```

The resource name in the AppHost (`AddDatabase("crittercab_dispatch")`) is the configuration key. Rename the resource and you must update every consumer — and the test fixtures, which supply the same key with `builder.UseSetting("ConnectionStrings:crittercab_dispatch", ...)`.

### Service-to-service URLs

No Cab service calls another over HTTP or a gRPC client yet; the only cross-service flow is Kafka. The shape below is **illustrative** of what Aspire provides when one does.

`.WithReference(otherService)` injects service-discovery configuration that lets `HttpClient` resolve a logical service name to its real URL at runtime. The calling service then uses the logical name in its base addresses:

```csharp
// Illustrative — no shipped service does this yet.
builder.Services.AddHttpClient<IDispatchClient, DispatchClient>(client =>
{
    client.BaseAddress = new Uri("https+http://dispatch");
});
```

The `https+http://` scheme tells Aspire's resolver to prefer HTTPS but fall back to HTTP. The host portion (`dispatch`) is the AppHost resource name. Aspire injects `services__dispatch__https__0` and `services__dispatch__http__0` configuration keys; the resolver picks the right one at request time. (Resolving that scheme needs Aspire's service-discovery extensions registered in the calling service, which Cab services do not register today — there is no `ServiceDefaults` project.)

**Aspire 13.2 breaking change worth knowing.** The service-discovery environment variable naming changed in 13.2: keys now use the endpoint *scheme* (`services__dispatch__https__0`), not the endpoint *name* as in 13.0/13.1. Code that read `services:dispatch:myendpoint:0` directly from `IConfiguration` needs updating. Code that uses `HttpClient` with `https+http://dispatch` URLs is unaffected — that path resolves through Aspire's service-discovery extensions, which handle the format change internally.

### The Program.cs guard pattern

Whether an absent connection string is fatal is a per-dependency decision, and `service-bootstrap` § Connection-String Guards owns the rule. In short:

- **Mandatory — `?? throw`** when the dependency is load-bearing on every code path; a run without it is a misconfiguration.
- **Optional — `if (!string.IsNullOrEmpty(...))`** when there is a meaningful degraded mode the service is expected to run in.

Both shipped services take the optional shape for both of their dependencies. From `src/CritterCab.Dispatch/Program.cs` (abbreviated):

```csharp
var connectionString = builder.Configuration.GetConnectionString("crittercab_dispatch");

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddMarten(opts =>
    {
        opts.Connection(connectionString);
        // ...
    })
    .IntegrateWithWolverine(/* ... */)
    .UseLightweightSessions();
}

// Read once; used for both service registration and transport wiring.
var kafkaEnabled = !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("kafka"));

builder.Host.UseWolverine(opts =>
{
    // ...
    // Guarded rather than early-returned, so a broker-less run cannot silently swallow any
    // Wolverine configuration appended after this line.
    if (kafkaEnabled)
        ConfigureKafkaListening(opts);
});
```

The degraded modes are real ones: a database-less or broker-less `dotnet run` of a single service still boots (Telemetry logs what it would have published; Dispatch answers nearby-driver queries from a stub), and the non-Kafka test suites run without a broker. Under the AppHost both services always receive both connection strings, because every `WithReference` is paired with a `WaitFor`.

The test harness is **not** what forces the guard: the fixtures supply connection strings through `builder.UseSetting("ConnectionStrings:...")`, which lands before `Program.cs` reads configuration. A service with no degraded mode can use `?? throw` and still be tested with Alba.

---

## The dashboard

When the AppHost starts, the terminal prints a dashboard URL with a one-time login token. Abbreviated:

```text
AppHost: src/CritterCab.AppHost/CritterCab.AppHost.csproj
Dashboard: https://localhost:5300/login?t=2b4a2ebc362b7fef9b5ccf73e702647b
Press CTRL+C to stop the apphost and exit.
```

The dashboard surfaces:

- **Resources** — live state for every container and project (running, starting, stopped, error), with health-check details.
- **Console logs** — per-resource and combined ("All") log streams with color-coded prefixes.
- **Structured logs** — searchable, filterable log records emitted via `ILogger`.
- **Traces** — distributed traces across services, viewable as waterfall diagrams.
- **Metrics** — counters, gauges, and histograms emitted by services and containers.
- **Resource graph** — visual topology of dependencies (which projects reference which resources).
- **Parameters** — set parameter values directly from the dashboard, optionally persisted to user secrets (Aspire 13.2 addition).

Traces and metrics fill only for what a service exports over OTLP. Cab services configure no OpenTelemetry today, so expect resource state and console logs from them and little else.

The dashboard host is pinned to `localhost:5300` (see § Port allocation), so it does not change per run; only the login token rotates. Bookmark `localhost:5300`.

For local-only dev runs without HTTPS hassles: Aspire generates a developer cert; trust it once with `dotnet dev-certs https --trust`. After that, every run in this repo trusts cleanly.

---

## Aspire MCP server (`aspire agent init`)

Aspire 13.2 ships a first-class MCP server that exposes the running AppHost to AI coding agents — Claude Code, GitHub Copilot, Cursor, and others. Cab's workflow is Claude-driven; this is the single highest-leverage Aspire integration for Cab's day-to-day.

### Setup

Run from the repository root; the CLI discovers the AppHost project under `src/`:

```bash
aspire agent init
```

The CLI detects supported agent environments (Claude Code, VS Code with GitHub Copilot, etc.) and writes the right config for each. It also offers to install an Aspire-specific `SKILL.md` at `.claude/skills/aspire/SKILL.md` (or the equivalent path for other agents) that teaches the agent how to use the Aspire CLI.

When the AppHost is up, Claude Code can:

- **Query resources** — list every running resource, its state, and its endpoints.
- **Read structured logs** — pull recent log entries for any resource, filtered by level or text match.
- **Inspect distributed traces** — fetch trace details for any span across the system.
- **Discover integrations** — `list_integrations` returns available Aspire hosting packages; `get_integration_docs` retrieves docs for one.
- **Switch AppHost contexts** — `select_apphost` (when multiple AppHosts exist in a workspace).

The MCP server connects via STDIO transport; Claude Code launches `aspire agent mcp` (or `aspire mcp start` on older configs) as a subprocess. No manual port management needed.

### What this means in practice

When debugging "why didn't this driver's position reach Dispatch?" with Claude:

- Claude can ask Aspire's MCP for the telemetry service's recent logs — including the logging publisher's output if the service came up without a broker.
- It can check whether the `kafka` resource is healthy and whether `dispatch` started after it.
- It can read Dispatch's console logs for the Kafka listener's errors.

This loop replaces "let me copy-paste these logs into chat" with "let me ask the running system."

`aspire agent mcp` (the deeper CLI surface) is covered in `cli-aspire`. The `init` step here is the one-time setup; everything else is just running the AppHost with Claude attached.

---

## Parked: TypeScript AppHost for the frontend

The Cab frontend is parked, and so is any AppHost change it would bring. When it returns, the lower-friction path is to keep the C# AppHost and add the frontend dev server as a JavaScript resource (`AddViteApp`, from Aspire's JavaScript hosting integration) alongside the .NET services. Aspire's TypeScript AppHost (preview as of 13.2) is the alternative, worth it only if the frontend's owners take over the AppHost. Re-source both against the pinned Aspire version before writing any of it.

---

## Common pitfalls

- **Pinning `Aspire.Hosting.AppHost` in `Directory.Packages.props`.** The `Aspire.AppHost.Sdk` already brings it in implicitly; a `PackageVersion` for it collides (NU1009) and the AppHost fails to restore. Leave it out — do not respond by turning CPM off for the AppHost.
- **Adding a service without a `ProjectReference`.** No reference, no `Projects.CritterCab_<Name>` accessor, and `AddProject<Projects.CritterCab_<Name>>` does not compile. The reference and the `AddProject` call land together.
- **Adding a project to disk without a `CritterCab.slnx` row.** CI's "Verify solution completeness" step fails the build for any `*.csproj` not listed in the slnx. Add the row in the same commit as the project.
- **`AddKafka("kafka").WithHostPort(...)`.** Fails with CS1929 on 13.4.6 — `WithHostPort` there binds to the Kafka UI resource. Pass the port to `AddKafka("kafka", port: ...)`.
- **Regenerating the `UserSecretsId`.** A new id loses the stored generated parameters (the Postgres password among them), so they stop matching what the persistent containers were created with. Keep `crittercab-apphost`.
- **Choosing the connection-string guard shape by habit.** `?? throw` and the optional guard are both sanctioned; the selector is whether the service has a degraded mode it is expected to run in. See `service-bootstrap` § Connection-String Guards.
- **Forgetting `WaitFor(...)`.** Services start before their dependencies are healthy and fail on connection. Every `WithReference(resource)` needs a paired `WaitFor(resource)` unless the service is genuinely fault-tolerant of its dependency being unavailable. (Dispatch's Kafka listener is — a listener that starts early retries — but the `WaitFor` stays as ordering hygiene; Telemetry's is load-bearing because it auto-provisions the topic at startup.)
- **Renaming a resource without updating consumers.** `AddDatabase("crittercab_dispatch")` → `GetConnectionString("crittercab_dispatch")` is a tight coupling. Renaming requires a search-replace across every service and test fixture that references it.
- **Assuming `services__name__myendpoint__0` env-var format from 13.1 still works.** Aspire 13.2 changed to scheme-based naming (`services__name__https__0`). Code that reads `IConfiguration` directly with the old key pattern silently returns null.
- **Running `aspire agent init` before the AppHost runs once.** The CLI needs an AppHost to anchor MCP configuration against. Run a sanity-check `aspire run` first, then `aspire agent init`.
- **Trusting `aspire agent init` to install Cab-specific skills.** It installs Aspire-specific skill files; Cab's `docs/skills/` library is separate. Don't expect overlap; both are useful and complementary.
- **Treating the AppHost as production infrastructure.** It isn't. Aspire is local-dev orchestration; production Cab runs on Azure per `ADR-007`. Don't put production-only secrets, real Azure connection strings, or anything you wouldn't share in a screenshot into `AppHost.cs`.
- **Running the AppHost during `dotnet test`.** Integration tests use Testcontainers, never Aspire. The AppHost isn't booted in the test process; tests have their own per-service fixtures per `testing-integration`. Mixing the two creates two competing container lifecycles fighting over the same ports.

---

## See also

**Upstream** — load these first:

- `service-bootstrap` — `Program.cs` shape; where `GetConnectionString(...)` is read; the two connection-string guard shapes and the rule that selects between them.
- `csharp-coding-standards` — `TimeProvider` injection convention; modern guard clauses; the conventions Cab service code follows.

**Sibling skills:**

- `adding-a-service` — the project skeleton, including the AppHost registration and the port slot.
- `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers` — handler shapes inside services orchestrated by the AppHost.
- `testing-integration` — Testcontainers-based test fixtures; the parallel infrastructure story used in tests rather than Aspire.
- `marten-async-daemon` (archived) — daemon configuration; every Cab projection is inline or live, so no service runs the daemon.
- `aspire-service-defaults` (archived) — a shared `ServiceDefaults` project; none exists.

**Downstream:**

- `cli-aspire` — the full Aspire CLI surface (`aspire run`, `aspire start --detach`, `aspire ps`, `aspire describe --follow`, `aspire wait`, `aspire doctor`, `aspire agent init`, `aspire export`); CI/CD usage with `--non-interactive` and `--format json`.
- `cli-jasperfx` — Cab service CLI surface (`describe`, `describe-routing`, `codegen-preview`); often run against an Aspire-orchestrated host during dev debugging.
- `wolverine-kafka` — wiring the Kafka client side; pairs with `Aspire.Hosting.Kafka` on the AppHost side.
- `wolverine-grpc-handlers` — the gRPC surface that rides each service's HTTPS endpoint.
- `wolverine-azure-service-bus` (archived) — the ASB client side; returns with `Aspire.Hosting.Azure.ServiceBus` when the emulator enters the AppHost.
- `observability-tracing` (archived) — OTLP export from services; no service configures OpenTelemetry yet.
- `polyglot-go-service` (archived) — the Go service was dropped.

**External:**

- ai-skills `wolverine-integrations-aspire` — generic Wolverine-on-Aspire baseline; complements this skill.
- All ai-skills installed via `npx skills add` (license required).
- [Aspire Documentation Home](https://aspire.dev/docs/) — the canonical entry point.
- [What's new in Aspire 13.2](https://aspire.dev/whats-new/aspire-13-2/) — TypeScript AppHost, new CLI commands, service-discovery breaking change, Microsoft Foundry transition.
- [What is the AppHost?](https://aspire.dev/get-started/app-host/?lang=csharp) — conceptual walkthrough of the resource model.
- [Aspire MCP server](https://aspire.dev/get-started/aspire-mcp-server/) — MCP tools, security model, agent integration details.
- [Service discovery](https://aspire.dev/fundamentals/service-discovery/) — `WithReference`, named endpoints, the `services:` config keys.
- [Inner-loop networking overview](https://aspire.dev/fundamentals/networking-overview/) — container bridge networks, host vs. container endpoint resolution, and the `host.docker.internal` story.
