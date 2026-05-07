---
name: adding-a-service
description: "How to scaffold a new deployable service in CritterCab: solution layout, project file baseline, database isolation, Aspire registration, paired test project, and the order of operations. Use when creating a service from scratch."
cluster: distributed-services
tags: [services, bootstrap, aspire, dotnet, infrastructure, setup, scaffolding]
---

# Adding a Service

Scaffolding a new deployable service in CritterCab. Per ADR-002, every bounded context (or every group of BCs that has been deliberately collapsed into a single deployable) becomes its own service. This skill covers the mechanical steps: project file, solution inclusion, database, Aspire wiring, paired tests. The substantive what-goes-inside-Program.cs content lives in `service-bootstrap` (Phase 2); this skill is the entry point that gets a new service to the point where `service-bootstrap` takes over.

## When to apply this skill

Use this skill when:

- Creating a new bounded-context service from scratch.
- Splitting a previously-merged BC out of a shared service into its own deployable.
- Setting up the directory and project skeleton before any handler code is written.
- Adding the Aspire wiring for a service that already exists in code but hasn't been registered with the AppHost.

Do NOT use this skill for:

- The composition-root content of `Program.cs` — see `service-bootstrap` (Phase 2).
- Per-service Wolverine/Marten/Polecat configuration details — see `service-bootstrap`, `marten-aggregates`, `polecat-event-sourcing` (Phase 2).
- Aspire programming model in depth — see `aspire` (Phase 2).
- File and directory organization within a service — see `vertical-slice-organization` (Phase 2).
- Test patterns and fixtures — see `testing-fundamentals` (Phase 2).

---

## What a CritterCab Service Is

A CritterCab service is a self-contained .NET project that:

- **Has its own deployable boundary.** No project references to other services. Cross-service communication is gRPC or Wolverine messages over the wire only (per ADR-002 and `structural-constraints.md`).
- **Owns its data store exclusively.** Each service has its own database — Postgres for Marten services, SQL Server for Polecat services. No cross-database queries. No shared schemas.
- **Hosts Wolverine.** Wolverine handles message dispatch, gRPC services, and HTTP endpoints. Every service runs Wolverine.
- **Is registered with the Aspire AppHost.** Aspire provisions the service's database, wires connection strings, and orchestrates startup in local dev. In production, Aspire publishes deployment artifacts (per ADR-007).
- **Has a paired test project.** Tests live in `tests/CritterCab.<ServiceName>.Tests/`, alongside the service's source.

Most services use `Microsoft.NET.Sdk.Web` — they expose HTTP endpoints (health checks at minimum, gRPC services where applicable, BFF endpoints if any). Library-style services using `Microsoft.NET.Sdk` are rare in this project.

---

## Order of Operations

Work in this order. Each step has a natural exit condition; don't move on until the current step is complete.

1. **Confirm the service is actually needed.** Per ADR-002, splits are motivated by domain, scaling, or ownership boundaries — not by service-count being a virtue. If in doubt, the BC stays inside an existing service until a reason to split appears.
2. **Author the proto contract** (if the service exposes gRPC or publishes messages other services consume). Proto first, code second — see `protobuf-contracts`.
3. **Create the service project directory and `.csproj`.** Conventions in [Solution and Folder Layout](#solution-and-folder-layout) below.
4. **Add the project to `CritterCab.slnx`.** Either via `dotnet sln add` or by editing the `.slnx` XML directly.
5. **Provision the service's database in the Aspire AppHost.** Postgres for Marten services, SQL Server for Polecat services. Naming convention in [Database Isolation and Naming](#database-isolation-and-naming).
6. **Register the service project in the Aspire AppHost.** Wire the database reference and any transport dependencies (Kafka, ASB) the service consumes.
7. **Author `Program.cs`** as a stub composition root with health checks and the minimum Wolverine/store wiring. The full pattern is in `service-bootstrap` (Phase 2).
8. **Create the paired test project.** Conventions in [Test Project Pairing](#test-project-pairing).
9. **Write a per-service `README.md`** — what the service owns, what its proto contract is (if any), and what its dependencies are.
10. **Run `aspire run`** and verify the service starts, registers with the dashboard, and connects to its database.

After step 10 the service exists in a runnable form with no domain logic. Domain handlers, projections, and gRPC services land via `service-bootstrap`, `marten-aggregates`, `wolverine-message-handlers`, and the relevant transport skills.

---

## Solution and Folder Layout

CritterCab uses the `.slnx` solution format with two top-level source folders.

```
CritterCab/
├── CritterCab.slnx                       # solution file (XML format)
├── Directory.Build.props                 # shared compiler settings
├── Directory.Packages.props              # central package versions
├── README.md
├── docs/                                 # design artifacts
├── protos/                               # cross-service contracts (per ADR-009)
├── src/
│   ├── AppHost/                          # Aspire AppHost (single-file apphost.cs)
│   │   └── apphost.cs
│   ├── CritterCab.Trips/
│   │   ├── CritterCab.Trips.csproj
│   │   ├── Program.cs
│   │   ├── README.md
│   │   ├── appsettings.json
│   │   ├── Trip.cs                       # aggregate
│   │   ├── StartTrip.cs                  # command + handler
│   │   ├── TripStarted.cs                # event
│   │   └── ...                           # see vertical-slice-organization
│   ├── CritterCab.Dispatch/
│   │   └── ...
│   └── CritterCab.<ServiceName>/
│       └── ...
└── tests/
    ├── CritterCab.Trips.Tests/
    │   └── CritterCab.Trips.Tests.csproj
    └── CritterCab.<ServiceName>.Tests/
        └── ...
```

### Project naming conventions

| Project type | Convention | Example |
|---|---|---|
| Service project | `CritterCab.<ServiceName>` | `CritterCab.Trips` |
| Test project | `CritterCab.<ServiceName>.Tests` | `CritterCab.Trips.Tests` |
| AppHost (single per repo) | `AppHost` (project name) | `src/AppHost/apphost.cs` |

Service names use the bounded-context name from `docs/vision/README.md` § Tentative Bounded Contexts, in PascalCase. Database names follow a different convention — see below.

### Adding a project to `.slnx`

Two ways, both supported:

```bash
# Via dotnet CLI (works with both .sln and .slnx)
dotnet sln CritterCab.slnx add src/CritterCab.Trips/CritterCab.Trips.csproj
dotnet sln CritterCab.slnx add tests/CritterCab.Trips.Tests/CritterCab.Trips.Tests.csproj
```

Or by editing the `.slnx` XML directly:

```xml
<Folder Name="/src/">
    <Project Path="src/CritterCab.Trips/CritterCab.Trips.csproj" />
</Folder>
<Folder Name="/tests/">
    <Project Path="tests/CritterCab.Trips.Tests/CritterCab.Trips.Tests.csproj" />
</Folder>
```

---

## The Service Project File

Service `.csproj` files inherit shared compiler settings from `Directory.Build.props` (LangVersion 14, TargetFramework net10.0, Nullable enable, ImplicitUsings enable) and use centrally-managed package versions from `Directory.Packages.props`. Service .csproj files therefore only need to declare what packages this service uses — no version numbers in the `<PackageReference>` elements.

### Marten-based service (most services)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <PackageReference Include="WolverineFx" />
    <PackageReference Include="WolverineFx.Http" />
    <PackageReference Include="WolverineFx.Grpc" />
    <PackageReference Include="WolverineFx.Marten" />
    <!-- Add transport packages as the service's flows require them. -->
    <PackageReference Include="WolverineFx.AzureServiceBus" />
    <PackageReference Include="WolverineFx.Kafka" />
  </ItemGroup>

</Project>
```

### Polecat-based service (Payments, etc.)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <PackageReference Include="WolverineFx" />
    <PackageReference Include="WolverineFx.Http" />
    <PackageReference Include="WolverineFx.Grpc" />
    <PackageReference Include="WolverineFx.Polecat" />
    <PackageReference Include="WolverineFx.AzureServiceBus" />
  </ItemGroup>

</Project>
```

### Notes on the project file

- **SDK is `Microsoft.NET.Sdk.Web`.** Services expose HTTP endpoints (health checks, gRPC, BFF). Library SDK is wrong for a service.
- **No `<TargetFramework>` element.** Inherited from `Directory.Build.props`.
- **No version numbers on `PackageReference` elements.** Central package management is enabled in `Directory.Packages.props`. Services only declare what they reference.
- **Only include transport packages the service actually uses.** A service that never publishes to ASB shouldn't reference `WolverineFx.AzureServiceBus`. The package list is part of the service's contract surface — extra references signal capabilities the service doesn't actually have.
- **Project references between services are forbidden.** Per ADR-002. The csproj should never contain a `<ProjectReference>` to another service.

---

## Database Isolation and Naming

Each service owns one database. No exceptions.

### Naming convention

| Store | Database naming | Examples |
|---|---|---|
| Postgres (Marten services) | `crittercab_<service_name>` (lowercase, snake_case) | `crittercab_trips`, `crittercab_dispatch`, `crittercab_onboarding` |
| SQL Server (Polecat services) | `CritterCab_<ServiceName>` (PascalCase) | `CritterCab_Payments` |

The naming difference reflects each store's native conventions. Postgres is case-folding-by-default; SQL Server traditionally uses PascalCase for database names.

### Schema isolation, not database sharing

There is one database per service. Per `structural-constraints.md`:

> Each service owns its data store exclusively. Reading data from another service requires a gRPC call or a Wolverine message to that service — not a direct query against its database.

Schemas within a single shared database — common in modular-monolith projects — are not used in CritterCab. Distinct databases are the boundary. This means:

- No cross-database joins.
- No "convenience" reads from another service's tables.
- If a service needs data from another service, it makes a gRPC call or subscribes to an integration event (per `transport-selection`).

The Aspire AppHost provisions each database independently; the service receives its connection string via Aspire-injected configuration.

---

## Aspire AppHost Registration

The AppHost lives at `src/AppHost/apphost.cs` as a single-file Aspire application (per the .NET 10 file-based application support and our Aspire 13 single-file apphost commitment). Each service registers with the AppHost, declares its database, and references any transport infrastructure it needs.

A new Marten-based service is added to `apphost.cs` roughly like this (conceptual — exact single-file syntax for Aspire 13.2 is documented in the `aspire` skill, Phase 2):

```csharp
// In apphost.cs

var builder = DistributedApplication.CreateBuilder(args);

// Shared infrastructure (added once, referenced by services)
var postgres = builder.AddPostgres("postgres");
var sqlserver = builder.AddSqlServer("sqlserver");

// Per-service database
var tripsDb = postgres.AddDatabase("crittercab_trips");

// The service itself
builder.AddProject<Projects.CritterCab_Trips>("trips")
    .WithReference(tripsDb)
    .WaitFor(tripsDb);

builder.Build().Run();
```

For services that consume Kafka or ASB, add the transport resource and `.WithReference(...)` it from the service.

The `aspire` skill (Phase 2) documents:

- The exact single-file apphost syntax (file-based application directives).
- Connection-string injection patterns.
- Adding the EH Emulator and ASB Emulator as resources.
- `aspire run` and the dashboard.

For now, this skill's responsibility ends at "the service is registered and its database is provisioned."

---

## Test Project Pairing

Every service has a paired test project. The test project lives at `tests/CritterCab.<ServiceName>.Tests/` and uses the project-naming convention in the layout table above.

### Test project file

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CritterCab.Trips\CritterCab.Trips.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <!-- Add as the service's tests need them: -->
    <PackageReference Include="Alba" />
    <PackageReference Include="Testcontainers.PostgreSql" />
  </ItemGroup>

</Project>
```

### Test stack at a glance

The test stack is committed in `Directory.Packages.props`:

| Concern | Package | Notes |
|---|---|---|
| Test runner | xUnit (v2) + xunit.runner.visualstudio | xUnit v2.x line, not v3. |
| Assertions | Shouldly | The project's assertion library. |
| HTTP integration tests | Alba | For services with HTTP endpoints. |
| Container-backed integration tests | Testcontainers.{PostgreSql, MsSql, ServiceBus, Kafka} | Real infrastructure in tests. |

The detailed test patterns — fixtures, lifecycle, parallelization, the integration-test layout — are in `testing-fundamentals` and `testing-integration` (Phase 2). This skill's responsibility ends at "the test project exists, references the service project, and is on the committed test stack."

---

## Composition Root (Pointer)

`Program.cs` for a new service is the entry point for the service-bootstrap conversation, not for this one. The minimum viable bootstrap is roughly:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine(opts =>
{
    // Service-specific Wolverine configuration goes here.
    // See service-bootstrap for the full pattern.
});

// Marten or Polecat configuration here. See marten-aggregates / polecat-event-sourcing.

var app = builder.Build();

app.MapHealthChecks("/health");
// Map gRPC services and HTTP endpoints as the service's contract requires.

return await app.RunJasperFxCommands(args);
```

Everything between the comment placeholders — Wolverine configuration, Marten event-type registration, transport routing, gRPC service mapping, observability bootstrap — lives in `service-bootstrap`, `marten-aggregates`, the per-transport skills, and `observability-tracing`.

The order of skills to follow once the service exists in skeleton form:

1. `service-bootstrap` — composition root patterns.
2. `marten-aggregates` or `polecat-event-sourcing` — store wiring.
3. `domain-event-conventions` — for the event types the service will register.
4. `wolverine-handlers` — general handler shape, validation pipeline.
5. `wolverine-http-handlers` and `wolverine-messaging-handlers` — protocol-specific patterns based on what the service exposes.
6. `observability-tracing` — distributed tracing setup.
7. The relevant transport skill(s) for the service's flows.

---

## Per-Service README

Every service has its own `README.md` at `src/CritterCab.<ServiceName>/README.md`. Keep it short. Suggested sections:

- **What this service owns.** One paragraph naming the bounded context's responsibility.
- **Proto contract.** Path to the service's `.proto` file (if any).
- **Store.** Marten + Postgres database name, or Polecat + SQL Server database name.
- **Transports.** Which of gRPC, Kafka, ASB this service uses.
- **Cross-service dependencies.** Other services this one calls (gRPC) or subscribes to (ASB topics).
- **Local development.** A reminder that running `aspire run` from the repo root brings the service up; service-specific dev notes only when there's something non-default.

The per-service README complements `docs/vision/README.md` § Tentative Bounded Contexts (which describes the BC at the project level) and `docs/narratives/` (which describes the journeys the service participates in).

---

## Common Pitfalls

- **Adding a `<ProjectReference>` to another service.** Forbidden by ADR-002. If the temptation arises, the answer is gRPC or a Wolverine message — not a project reference.
- **Sharing a database across services with separate schemas.** Modular-monolith pattern, not Cab's pattern. Each service has its own database.
- **Pinning package versions in service `.csproj` files.** Central package management is on. Versions live in `Directory.Packages.props`. PRs that pin versions in service files will conflict with the central management model.
- **Authoring `Program.cs` before the proto contract.** If the service exposes gRPC or publishes cross-service messages, the proto comes first (per ADR-009 and `protobuf-contracts`). Code-first proto generation is not used in CritterCab.
- **Forgetting to add the service to `CritterCab.slnx`.** The project compiles standalone but won't be picked up by solution-wide commands (`dotnet build`, `dotnet test`, IDE solution explorer). Add it.
- **Using `Microsoft.NET.Sdk` instead of `Microsoft.NET.Sdk.Web`.** Library SDK doesn't include the ASP.NET Core hosting model. Use Web SDK for services.
- **Including transport packages the service doesn't use.** A service that never publishes to Kafka shouldn't reference `WolverineFx.Kafka`. Keep package references aligned with the service's actual flow shape.
- **Provisioning the database manually instead of through the AppHost.** Aspire's database provisioning is the canonical path for local dev. Manual `psql` or SSMS database creation will work in the moment but skips the wiring that makes the service runnable from `aspire run`.

---

## See also

**Upstream** — load these first:

- `transport-selection` — which transports the service will use, established before the service is created.
- `protobuf-contracts` — required when the service exposes gRPC or publishes cross-service messages.
- `domain-event-conventions` — for the event types the service will register with Marten.

**Downstream** — natural follow-ups once the service skeleton exists:

- `service-bootstrap` — composition root patterns inside `Program.cs` (Phase 2).
- `aspire` — single-file apphost programming model and resource wiring (Phase 2).
- `cli-aspire` — `aspire run`, `aspire describe`, `aspire logs` (Phase 2).
- `vertical-slice-organization` — file and directory organization within the service (Phase 2).
- `marten-aggregates` — for Marten-based services (Phase 2).
- `polecat-event-sourcing` — for Polecat-based services (Phase 4).
- `wolverine-handlers` — handler shapes (Phase 2).
- `observability-tracing` — OpenTelemetry distributed tracing setup (Phase 2).
- `testing-fundamentals` — what the paired test project should actually contain (Phase 2).

**External:**

- ADR-002 in [`docs/decisions/`](../../decisions/) — services per bounded context.
- ADR-007 in [`docs/decisions/`](../../decisions/) — Azure as deployment target.
- [`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md) § Service Boundaries.
- [`docs/vision/README.md`](../../vision/README.md) § Tentative Bounded Contexts.
- ai-skills `critterstack-arch-new-project-wolverine-marten` — generic Critter Stack project bootstrap with Marten. Install via `npx skills add` (license required).
- ai-skills `critterstack-arch-new-project-wolverine-polecat` — generic Critter Stack project bootstrap with Polecat. Install via `npx skills add` (license required).
