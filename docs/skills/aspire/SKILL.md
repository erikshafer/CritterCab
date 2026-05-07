---
name: aspire
description: "Aspire 13.2 local-dev orchestration for CritterCab — the single-file apphost.cs (.NET 10 file-based application), what gets provisioned (Postgres, SQL Server, Kafka, eventually Azure Service Bus emulator), how Cab services compose against it (WithReference, WaitFor), service discovery and connection-string injection into Program.cs, the dashboard, integration with the Aspire MCP server for AI coding agents (`aspire agent init`), and the future TypeScript polyglot path for the frontend. Use when authoring or modifying the Cab AppHost, adding a new service or infrastructure resource, debugging dev-time orchestration, or wiring a service's connection-string consumption."
cluster: infrastructure
tags: [aspire, apphost, local-dev, service-discovery, postgres, sqlserver, kafka, azure-service-bus, mcp, dotnet-10, file-based-application, polyglot, typescript]
---

# Aspire — Local Dev Orchestration

Aspire is Cab's local development orchestration layer. One file, `apphost.cs`, declares every container Cab needs (Postgres, SQL Server, Kafka, eventually the Azure Service Bus emulator), every Cab service (Trips, Pricing, Identity, Onboarding, Dispatch, etc.), and the relationships between them. `aspire run` provisions the containers, starts the services with connection strings injected, and serves a dashboard that lets you watch the system run.

Aspire is **local-dev only**. Production deployment is Azure-native per `ADR-007`; integration tests use Testcontainers per `testing-integration` and never bootstrap the AppHost. The AppHost's job is to make `F5` produce a fully-wired distributed system on a developer laptop and nothing more.

The Cab AppHost is built on Aspire 13.2.2 — a substantially different shape from earlier Aspire versions. The biggest changes worth flagging up front:

- **Single-file `apphost.cs`** using .NET 10 file-based application directives (`#:sdk`, `#:package`, `#:project`) — no `.csproj`, no separate AppHost folder.
- **Unified `aspire.config.json`** replaces the old `apphost.run.json` + `.aspire/settings.json` split.
- **TypeScript AppHost** is preview — relevant later when the Cab frontend lands and the polyglot story matters; not relevant for the all-.NET state today.
- **Aspire MCP server** (`aspire agent init`) wires the running AppHost into Claude Code and other agents — first-class for Cab's Claude-driven workflow.
- **Service discovery** environment variable naming changed: keys are now scheme-based (`services__myservice__https__0`), not endpoint-name-based (breaking change from 13.1).

---

## When to apply this skill

Use this skill when:

- Authoring the initial Cab `apphost.cs`.
- Adding a new Cab service to the AppHost.
- Adding a new infrastructure resource (Azure Service Bus emulator, Redis, etc.).
- Wiring a service's `Program.cs` to consume Aspire-injected connection strings.
- Diagnosing dev-time orchestration issues (services starting before dependencies, missing connection strings, dashboard URL).
- Setting up Aspire MCP integration for Claude Code.
- Planning the future TypeScript AppHost path for the frontend.

Do NOT use this skill for:

- The Aspire CLI surface (`aspire run`, `aspire start`, `aspire describe`, `aspire wait`, `aspire doctor`) — `cli-aspire` (next, Phase 2).
- Integration test composition — `testing-integration`. The AppHost is not run in tests.
- Production deployment to Azure — `ADR-007` and forward-looking deployment skills.
- Wolverine transport configuration consumed by services — `wolverine-azure-service-bus`, `wolverine-kafka`, `wolverine-grpc-handlers` (Phase 3).
- Service-side connection-string consumption beyond the immediate startup wiring — `service-bootstrap`.

---

## What Cab's AppHost does

Concretely, `apphost.cs` is the entry point for `aspire run`. Running it:

1. **Provisions infrastructure containers** — Postgres for Marten services, SQL Server for Polecat services, Kafka for Telemetry-style services, Azure Service Bus emulator (when wired) for business-event routing.
2. **Starts each Cab service** with connection strings, service-discovery configuration, and OTLP telemetry endpoints injected via environment variables.
3. **Coordinates startup ordering** — services wait for their dependencies via `.WaitFor(...)` before starting.
4. **Serves the Aspire dashboard** at a URL printed in the terminal (with a one-time login token), showing live resource state, structured logs, distributed traces, and metrics for the entire system.
5. **Exposes an MCP server** (when configured via `aspire agent init`) that lets Claude Code query resource state, read logs, and inspect telemetry directly from a running AppHost.

The dashboard and the MCP server are both manifestations of the same thing: Aspire knows the topology, knows the runtime state, and surfaces it to humans (dashboard) and agents (MCP).

---

## The committed Aspire 13.2 packages

Cab's `Directory.Packages.props` pins:

| Package | Version | Used for |
|---|---|---|
| `Aspire.Hosting.AppHost` | 13.2.2 | The AppHost SDK itself; required. |
| `Aspire.Hosting.PostgreSQL` | 13.2.2 | Postgres containers for Marten services. |
| `Aspire.Hosting.SqlServer` | 13.2.2 | SQL Server containers for Polecat services. |
| `Aspire.Hosting.Kafka` | 13.2.2 | Kafka containers for Telemetry-style services. |

**Not yet committed but expected to land:**

- `Aspire.Hosting.AzureServiceBus` — for the ASB emulator container. Cab uses ASB as the business-event backbone; this package wires it into the AppHost. Add when the first service needs ASB-routed messaging in dev. The committed `Testcontainers.ServiceBus` covers integration tests independently per `testing-integration`.
- `Aspire.Hosting.NodeJs` (or Bun-equivalent) — for hosting the TypeScript frontend when that lands. Not needed for any current Cab service.

The version line is uniform — every Aspire package on `13.2.2`. When upgrading, prefer `aspire update` from the CLI (covered in `cli-aspire`), which keeps the SDK directive in `apphost.cs` and the package versions in `Directory.Packages.props` aligned.

---

## The single-file `apphost.cs` shape

Cab's AppHost lives at the repository root as `apphost.cs` — a single file, no `.csproj`. The .NET 10 SDK's file-based application support handles compilation; Aspire 13.2's `#:sdk` directive points the runtime at the AppHost SDK.

```csharp
#:sdk Aspire.AppHost.Sdk@13.2.2

#:package Aspire.Hosting.PostgreSQL@13.2.2
#:package Aspire.Hosting.SqlServer@13.2.2
#:package Aspire.Hosting.Kafka@13.2.2

#:project ./src/CritterCab.Trips/CritterCab.Trips.csproj
#:project ./src/CritterCab.Pricing/CritterCab.Pricing.csproj
#:project ./src/CritterCab.Identity/CritterCab.Identity.csproj

var builder = DistributedApplication.CreateBuilder(args);

// === Infrastructure ===

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("18-alpine")
    .WithLifetime(ContainerLifetime.Persistent);

var tripsDb     = postgres.AddDatabase("trips-db");
var pricingDb   = postgres.AddDatabase("pricing-db");
var identityDb  = postgres.AddDatabase("identity-db");

var sqlServer   = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent);

var paymentsDb  = sqlServer.AddDatabase("payments-db");

var kafka = builder.AddKafka("kafka")
    .WithLifetime(ContainerLifetime.Persistent);

// === Services ===

var identity = builder.AddProject<Projects.CritterCab_Identity>("identity")
    .WithReference(identityDb)
    .WaitFor(identityDb);

var pricing = builder.AddProject<Projects.CritterCab_Pricing>("pricing")
    .WithReference(pricingDb)
    .WaitFor(pricingDb);

var trips = builder.AddProject<Projects.CritterCab_Trips>("trips")
    .WithReference(tripsDb)
    .WithReference(kafka)
    .WithReference(identity)
    .WithReference(pricing)
    .WaitFor(tripsDb)
    .WaitFor(kafka)
    .WaitFor(identity)
    .WaitFor(pricing);

builder.Build().Run();
```

### What every directive does

- **`#:sdk Aspire.AppHost.Sdk@13.2.2`** — points the .NET 10 runtime at Aspire's AppHost SDK. Required as the first directive; equivalent to `<Project Sdk="Aspire.AppHost.Sdk/13.2.2">` in a traditional `.csproj`.
- **`#:package <Name>@<Version>`** — adds a NuGet package reference. Equivalent to `<PackageReference>`. One per Aspire integration package needed (Postgres, SqlServer, Kafka, etc.).
- **`#:project <relative-path-to-csproj>`** — adds a project reference. Equivalent to `<ProjectReference>`. One per Cab service the AppHost orchestrates.

The directives must appear before the first non-directive C# code. Top-level statements (`var builder = ...`) follow.

### Resource registration patterns

- **`builder.AddPostgres("postgres")`** — adds a Postgres container resource named `postgres`. The name flows into the connection-string config key (`ConnectionStrings:postgres`) and the service discovery namespace.
- **`postgres.AddDatabase("trips-db")`** — declares a logical database within the Postgres container. Aspire creates it on startup; `tripsDb` is a `IResourceBuilder<PostgresDatabaseResource>` you can `.WithReference(...)` from a service.
- **`builder.AddSqlServer("sqlserver").AddDatabase("payments-db")`** — same shape for SQL Server. Polecat services consume `payments-db` rather than `sqlserver` directly.
- **`builder.AddKafka("kafka")`** — Kafka container; resource name is the connection-string key.
- **`builder.AddProject<Projects.CritterCab_Trips>("trips")`** — adds a Cab service project, identified via the strongly-typed `Projects.*` accessor that's auto-generated from each `#:project` directive at build time.
- **`.WithReference(resource)`** — injects connection-string and service-discovery configuration for `resource` into the project's environment.
- **`.WaitFor(resource)`** — delays project startup until `resource` reports healthy. Without it, the project starts before its dependencies and fails.
- **`.WithLifetime(ContainerLifetime.Persistent)`** — keeps the container running across `aspire run` restarts. Recommended for databases (avoids slow re-provisioning); skip for ephemeral resources you want fresh each run.

The `#:project` directive generates the strongly-typed `Projects.CritterCab_Trips` accessor at build time. Underscores in the type name correspond to dots in the project name; the path in the directive is the source.

---

## Service discovery and connection-string injection

Aspire injects resource configuration into each project's environment as standard `IConfiguration` keys. Two flavors matter for Cab.

### Connection strings

`.WithReference(postgresDatabase)` produces a configuration entry the service reads via `builder.Configuration.GetConnectionString(<resource-name>)`:

```csharp
// In CritterCab.Trips/Program.cs
var tripsDbConnectionString = builder.Configuration.GetConnectionString("trips-db");

builder.Services.AddMarten(opts =>
{
    opts.Connection(tripsDbConnectionString!);
    opts.DatabaseSchemaName = "public";
}).IntegrateWithWolverine();
```

The resource name in the AppHost (`AddDatabase("trips-db")`) is the configuration key. Rename the resource and you must update every consumer.

### Service-to-service URLs

`.WithReference(otherService)` injects service-discovery configuration that lets HttpClient resolve a logical service name to its real URL at runtime. The Cab service then uses the logical name in its `HttpClient` base addresses:

```csharp
// In CritterCab.Trips/Program.cs
builder.Services.AddHttpClient<IIdentityClient, IdentityClient>(client =>
{
    client.BaseAddress = new Uri("https+http://identity");
});
```

The `https+http://` scheme tells Aspire's resolver to prefer HTTPS but fall back to HTTP. The host portion (`identity`) is the AppHost resource name. Aspire injects `services__identity__https__0` and `services__identity__http__0` configuration keys; the resolver picks the right one at request time.

**Aspire 13.2 breaking change worth knowing.** The service-discovery environment variable naming changed in 13.2: keys now use the endpoint *scheme* (`services__identity__https__0`), not the endpoint *name* as in 13.0/13.1. Code that read `services:identity:myendpoint:0` directly from `IConfiguration` needs updating. Code that uses `HttpClient` with `https+http://identity` URLs is unaffected — that path resolves through Aspire's service-discovery extensions, which handle the format change internally.

### The Program.cs guard pattern

Connection strings are absent at integration-test startup because the test fixture overrides them in `ConfigureServices` (per `testing-integration`). Service `Program.cs` files must not throw on absent connection strings — they need to compose cleanly when Aspire is in the picture and when it isn't.

```csharp
// ✅ CORRECT — guarded read; null when not running under Aspire
var tripsDbConnectionString = builder.Configuration.GetConnectionString("trips-db");

if (!string.IsNullOrEmpty(tripsDbConnectionString))
{
    builder.Services.AddMarten(opts =>
    {
        opts.Connection(tripsDbConnectionString);
        opts.DatabaseSchemaName = "public";
    }).IntegrateWithWolverine();
}

// ❌ WRONG — fires before test fixture's ConfigureServices override applies
var tripsDbConnectionString = builder.Configuration.GetConnectionString("trips-db")
    ?? throw new InvalidOperationException("trips-db connection string missing");
```

The test fixture runs `ConfigureServices` after `Program.cs` reads `IConfiguration`, so a `?? throw` fires before the override can apply and kills the test host. Always guard the registration block with a non-null check on the connection string, never `?? throw`.

This is the reason `service-bootstrap` registers Marten and Polecat behind `if (!string.IsNullOrEmpty(...))` blocks.

---

## The dashboard

When `aspire run` starts, the terminal prints a dashboard URL with a one-time login token:

```text
🔍 Finding apphosts... apphost.cs
🗄 Created settings file at 'aspire.config.json'.
AppHost: apphost.cs
Dashboard: https://localhost:17213/login?t=2b4a2ebc362b7fef9b5ccf73e702647b
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

The dashboard URL changes per run; bookmark the host (`localhost:17213`) only — the token rotates.

For local-only dev runs without HTTPS hassles: Aspire generates a developer cert; trust it once with `dotnet dev-certs https --trust`. After that, every `aspire run` in this repo trusts cleanly.

---

## Aspire MCP server (`aspire agent init`)

Aspire 13.2 ships a first-class MCP server that exposes the running AppHost to AI coding agents — Claude Code, GitHub Copilot, Cursor, and others. Cab's workflow is Claude-driven; this is the single highest-leverage Aspire integration for Cab's day-to-day.

### Setup

Run from the repository root, where `apphost.cs` lives:

```bash
aspire agent init
```

The CLI detects supported agent environments (Claude Code, VS Code with GitHub Copilot, etc.) and writes the right config for each. It also offers to install an Aspire-specific `SKILL.md` at `.claude/skills/aspire/SKILL.md` (or the equivalent path for other agents) that teaches the agent how to use the Aspire CLI.

When `aspire run` is up, Claude Code can:

- **Query resources** — list every running resource, its state, and its endpoints.
- **Read structured logs** — pull recent log entries for any resource, filtered by level or text match.
- **Inspect distributed traces** — fetch trace details for any span across the system.
- **Discover integrations** — `list_integrations` returns available Aspire hosting packages; `get_integration_docs` retrieves docs for one.
- **Switch AppHost contexts** — `select_apphost` (when multiple AppHosts exist in a workspace).

The MCP server connects via STDIO transport; Claude Code launches `aspire agent mcp` (or `aspire mcp start` on older configs) as a subprocess. No manual port management needed.

### What this means in practice

When debugging "why didn't my Trip event fire?" with Claude:

- Claude can ask Aspire's MCP for the trips service's recent error logs.
- It can pull the trace for the failing request and identify which downstream call timed out.
- It can query Postgres connectivity directly through the running AppHost.

This loop replaces "let me copy-paste these logs into chat" with "let me ask the running system." For a Claude-first workflow like Cab's, that's transformative.

`aspire agent mcp` (the deeper CLI surface) is covered in `cli-aspire`. The `init` step here is the one-time setup; everything else is just running the AppHost with Claude attached.

---

## Future: TypeScript AppHost for the frontend

Cab's frontend isn't built yet — that's far in the future per `userMemories`. When it lands, Aspire's TypeScript AppHost (preview as of 13.2) is the polyglot path.

The TypeScript AppHost uses the same app model as C# — resources, references, integrations — expressed via `createBuilder()`:

```typescript
// Hypothetical future apphost.ts
import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const postgres = await builder.addPostgres("postgres")
    .withImageTag("18-alpine");

const trips = await builder.addProject("trips", "../src/CritterCab.Trips")
    .withReference(postgres)
    .waitFor(postgres);

const frontend = await builder.addViteApp("frontend", "../frontend")
    .withBun()
    .withReference(trips);

await builder.build().run();
```

**Two viable paths when the time comes:**

1. **Stay on C# AppHost, add the Vite/Bun frontend resource via the JavaScript hosting integration.** `addViteApp` works from C# AppHost too — `Aspire.Hosting.NodeJs` (with `WithBun()` for Bun) provisions the frontend dev server alongside the .NET services. This is the lower-friction path; the AppHost stays in C# and just gains a JS resource.
2. **Migrate to TypeScript AppHost.** Worth it only if the frontend team genuinely owns the AppHost and prefers TypeScript. Cab's AppHost is currently maintained alongside .NET services, so C# is the natural fit.

For most foreseeable Cab states, option 1 is the right answer. The TypeScript AppHost is a real option to keep in mind, not a destination.

When the frontend lands, add `Aspire.Hosting.NodeJs` to `Directory.Packages.props`, then in `apphost.cs`:

```csharp
var frontend = builder.AddViteApp("frontend", "../frontend")
    .WithBun()
    .WithReference(trips)
    .WaitFor(trips);
```

`addViteApp` and `WithBun()` are first-class in Aspire 13.2 — verified against the 13.2 release notes. The frontend resource appears in the dashboard alongside .NET services with the same health-check, log, and trace surface.

---

## Common pitfalls

- **Putting `?? throw` on connection-string reads in `Program.cs`.** Fires before integration test fixtures override connection strings. Always guard the registration block with `if (!string.IsNullOrEmpty(...))`. The exception fires before `ConfigureServices` overrides apply.
- **Forgetting `WaitFor(...)`.** Services start before their dependencies are healthy and fail on connection. Every `WithReference(resource)` needs a paired `WaitFor(resource)` unless the service is genuinely fault-tolerant of its dependency being unavailable.
- **Renaming a resource without updating consumers.** `AddDatabase("trips-db")` → `GetConnectionString("trips-db")` is a tight coupling. Renaming requires a global search-replace across every service that references it.
- **Assuming `services__name__myendpoint__0` env-var format from 13.1 still works.** Aspire 13.2 changed to scheme-based naming (`services__name__https__0`). Code that reads `IConfiguration` directly with the old key pattern silently returns null. Use `HttpClient` with `https+http://serviceName` URLs — that path is format-agnostic.
- **Missing `#:sdk` directive.** Without it, the file-based app doesn't know it's an Aspire AppHost; runtime errors at startup. Always the first directive in `apphost.cs`.
- **Mixing `#:sdk` and `#:package` for the AppHost SDK.** `Aspire.AppHost.Sdk` is an SDK, not a NuGet package — use `#:sdk Aspire.AppHost.Sdk@13.2.2`, not `#:package Aspire.AppHost.Sdk@13.2.2`. The 13.2 release notes have a snippet that shows `#:package` for this; treat that as a doc typo and follow the workshop and 9.5 announcement which consistently use `#:sdk`.
- **Running `aspire agent init` without an existing `apphost.cs`.** The CLI needs an AppHost to anchor MCP configuration against. Create the AppHost first, run a sanity-check `aspire run`, then `aspire agent init`.
- **Trusting `aspire agent init` to install Cab-specific skills.** It installs Aspire-specific skill files; Cab's `docs/skills/` library is separate and managed under `agentskills.io` conventions. Don't expect overlap; both are useful and complementary.
- **Treating the AppHost as production infrastructure.** It isn't. Aspire is local-dev orchestration; production Cab runs on Azure per `ADR-007` (or the eventual deployment ADR). Don't put production-only secrets, real Azure connection strings, or anything you wouldn't share in a screenshot into `apphost.cs`.
- **Letting `apphost.cs` accumulate stale `#:project` directives.** When a service is renamed or removed, the old directive lingers and the build fails. Treat `#:project` directives as part of every service-rename PR.
- **Running the AppHost during `dotnet test`.** Integration tests use Testcontainers, never Aspire. The AppHost isn't booted in the test process; tests have their own per-service fixture per `testing-integration`. Mixing the two creates two competing container lifecycles fighting over the same ports.

---

## See also

**Upstream** — load these first:

- `service-bootstrap` — `Program.cs` shape; where `GetConnectionString(...)` is read; the guarded registration pattern.
- `csharp-coding-standards` — `TimeProvider` injection convention; modern guard clauses; the conventions Cab service code follows.

**Sibling skills:**

- `marten-async-daemon` — `MartenDaemonModeIsSolo()` and the daemon configuration that runs against the Aspire-injected Postgres connection.
- `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers` — handler shapes inside services orchestrated by the AppHost.
- `testing-integration` — Testcontainers-based test fixtures; the parallel infrastructure story used in tests rather than Aspire.

**Downstream:**

- `cli-aspire` (next, Phase 2) — the full Aspire CLI surface (`aspire run`, `aspire start --detach`, `aspire ps`, `aspire describe --follow`, `aspire wait`, `aspire doctor`, `aspire agent init`, `aspire export`); CI/CD usage with `--non-interactive` and `--format json`.
- `cli-jasperfx` (Phase 2) — Cab service CLI surface (`describe`, `describe-routing`, `codegen-preview`); often run against an Aspire-orchestrated host during dev debugging.
- `wolverine-azure-service-bus` (Phase 3) — wiring the ASB client side; eventually pairs with `Aspire.Hosting.AzureServiceBus` on the AppHost side when Cab adds the emulator container.
- `wolverine-kafka` (Phase 3) — wiring the Kafka client side; pairs with `Aspire.Hosting.Kafka` already committed.
- `wolverine-grpc-handlers` (Phase 3) — service-to-service gRPC; uses Aspire service discovery for endpoint resolution.
- `observability-tracing` (Phase 3) — the OTLP endpoint Aspire injects; how Cab services emit spans and metrics that surface in the dashboard.
- `polyglot-go-service` (Phase 4) — a hypothetical future Go service; same `WithReference`/`WaitFor` patterns apply via Aspire's container or executable resource types.

**External:**

- ai-skills — generic Aspire baseline if/when JasperFx publishes one; complements this skill.
- All ai-skills installed via `npx skills add` (license required).
- [Aspire Documentation Home](https://aspire.dev/docs/) — the canonical entry point.
- [What's new in Aspire 13.2](https://aspire.dev/whats-new/aspire-13-2/) — TypeScript AppHost, new CLI commands, service-discovery breaking change, Microsoft Foundry transition.
- [What is the AppHost?](https://aspire.dev/get-started/app-host/?lang=csharp) — conceptual walkthrough of the resource model.
- [Aspire MCP server](https://aspire.dev/get-started/aspire-mcp-server/) — MCP tools, security model, agent integration details.
- [Service discovery](https://aspire.dev/fundamentals/service-discovery/) — `WithReference`, named endpoints, the `services:` config keys.
- [Inner-loop networking overview](https://aspire.dev/fundamentals/networking-overview/) — container bridge networks, host vs. container endpoint resolution, and the `host.docker.internal` story.
- [Aspire Roadmap Q1 2026 update](https://github.com/microsoft/aspire/discussions/15662) — context on TypeScript AppHost, agent-native CLI, and what's coming next.
- [.NET 10 file-based applications announcement (Damian Edwards)](https://devblogs.microsoft.com/dotnet/) — background on the `#:sdk`/`#:package`/`#:project` directives.
