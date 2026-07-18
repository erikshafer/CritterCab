# CritterCab

[![CI](https://github.com/erikshafer/CritterCab/actions/workflows/dotnet.yml/badge.svg)](https://github.com/erikshafer/CritterCab/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Wolverine](https://img.shields.io/badge/Wolverine-6.19-512BD4)](https://wolverine.netlify.app/)
[![Marten](https://img.shields.io/badge/Marten-9.15-512BD4)](https://martendb.io/)
[![Polecat](https://img.shields.io/badge/Polecat-4.x-512BD4)](https://polecat.jasperfx.net/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2025-CC2927?logo=microsoftsqlserver&logoColor=white)](https://learn.microsoft.com/en-us/sql/sql-server/)
[![Kafka](https://img.shields.io/badge/Kafka-Transport-231F20?logo=apachekafka&logoColor=white)](https://kafka.apache.org/)
[![gRPC](https://img.shields.io/badge/gRPC-Streaming-4285F4)](https://grpc.io/)

> An open-source ride-sharing reference architecture built on the [Critter Stack](https://wolverine.netlify.app/), showcasing Wolverine's gRPC feature set alongside event-driven messaging, event sourcing, and more.

---

## About

CritterCab is an open-source reference architecture for a ride-sharing platform, built on the Critter Stack, a family of .NET libraries maintained by JasperFx. Its distinguishing focus is **Wolverine's gRPC feature set**, which shipped in Wolverine 5.32. Ride-sharing was chosen because its natural shape (GPS streaming, dispatch matching, trip lifecycle) exercises gRPC in all four modes while leaving room for event sourcing, high-volume telemetry, and multi-transport messaging.

### Technology Versions

Versions are managed centrally in [`Directory.Packages.props`](Directory.Packages.props). Marten, Polecat, and the JasperFx/Weasel libraries resolve **transitively** through the `WolverineFx.*` integration packages rather than being pinned directly, so the versions below are the currently-resolved values.

| Concern | Package | Version |
|---|---|---|
| Messaging, HTTP, gRPC, handlers | Wolverine (`WolverineFx`) | 6.19.0 (5.32 gRPC floor that motivated the project) |
| Event sourcing + document store (PostgreSQL) | Marten | 9.15 (via `WolverineFx.Marten` 6.19) |
| Event sourcing + document store (SQL Server) | Polecat | 4.x (via `WolverineFx.Polecat` 6.19; pinned, first SQL Server service pending) |
| Relational engine — Marten | PostgreSQL | 18 |
| Relational engine — Polecat | SQL Server | 2025 |
| Local-dev orchestration | Aspire | 13.4.6 |
| Integration test host | Alba | 8.5.3 |

## What's Distinctive

- **gRPC as a design concern.** All four modes (unary, server-streaming, client-streaming, bidirectional) exercised against natural domain use cases.
- **Multi-transport messaging.** gRPC, Kafka (for high-volume telemetry), and likely Azure Service Bus (for business events) in one system, chosen per flow rather than defaulted.
- **Distributed services.** Each bounded context deploys as a separate service, communicating exclusively through messages or gRPC calls.
- **Polyglot participation.** At least one non-.NET service (Go, most likely) participates over gRPC, keeping the contract honest at the wire level.

## Status

Early development. Two services exist, both backed by Marten event sourcing, Wolverine.HTTP, and Alba integration tests:

**Dispatch** — the first bounded-context service, with three vertical slices implemented end-to-end:

- **Slice 5.1** — `SubmitRideRequest` → `RideRequested` (HTTP entry point, aggregate, projections).
- **Slice 5.2** — `RideRequested` → `FareQuoteAutomation` → `FareQuoted` / `FareQuoteFailed` (Wolverine-driven automation against a stub `IPricingClient`; the Pricing BC itself is pending its own workshop).
- **Slice 5.3** — `CandidateSelectionAutomation` → `CandidatesSelected` / `NoCandidatesAvailable` (driver-matching rounds against a stub `INearbyAvailableDriversSource`).

**Telemetry** — the second service (stream-processing shape), currently a skeleton plus slice 1: `TelemetryPolicyConfigured` (config-as-events). Its Kafka transport is designed but not yet wired.

Transports (gRPC, Kafka, Azure Service Bus) are richly designed but not yet built in code — every flow to date runs in-process over HTTP + Marten. All other bounded contexts remain pre-workshop. See [`docs/vision/README.md`](docs/vision/README.md) for the full scope and [`docs/decisions/`](docs/decisions/) for committed decisions.

## Prerequisites

To clone, build, and run CritterCab locally you will need:

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** — the project targets `net10.0` and uses C# 14.
- **[Docker](https://www.docker.com/products/docker-desktop/)** (Docker Desktop or an equivalent OCI runtime) — Aspire spins up PostgreSQL 18 as a container for local dev, and the integration tests use [Testcontainers](https://testcontainers.com/) for ephemeral PostgreSQL, SQL Server, Kafka, and Azure Service Bus instances.
- **An IDE with C# tooling** (optional but recommended) — JetBrains Rider, Visual Studio, or VS Code with the C# Dev Kit.

No global tool installs are required; the AppHost is a [file-based .NET 10 program](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk#file-based-programs) (`apphost.cs`) that pulls its Aspire dependencies via `#:package` directives.

## Running Locally

Clone the repository and start the Aspire AppHost. This boots the PostgreSQL container and the Dispatch and Telemetry services together, and opens the Aspire dashboard at `https://localhost:5300` (pinned — see the [`aspire` skill](docs/skills/aspire/SKILL.md) § Port allocation for CritterCab's `53xx` local-dev port band).

```bash
git clone https://github.com/erikshafer/CritterCab.git
cd CritterCab

# Boot Postgres + Dispatch + Telemetry via the Aspire AppHost
dotnet run apphost.cs
```

Run the full test suite (unit + Alba integration tests, the latter backed by Testcontainers):

```bash
dotnet test CritterCab.slnx
```

The Dispatch service exposes `POST /ride-requests` for slice 5.1 and a health endpoint at `GET /health`. Probe shape and behavior are pinned by the Alba tests under [`tests/CritterCab.Dispatch.Tests/`](tests/CritterCab.Dispatch.Tests/).

## Repository Structure

```
.
├── apphost.cs               # File-based Aspire AppHost (.NET 10 file-based program)
├── Properties/              # AppHost launchSettings.json — dashboard/OTLP/MCP port pins (5300-5307)
├── CritterCab.slnx          # Solution
├── Directory.Build.props    # Shared MSBuild props (TFM, lang version, nullable)
├── Directory.Packages.props # Central package versions
├── protos/                  # Protobuf contracts (buf-managed)
├── src/
│   ├── CritterCab.Dispatch/  # First bounded-context service (RideRequesting, FareQuoting, CandidateSelection)
│   └── CritterCab.Telemetry/ # Second service (stream-processing shape, TelemetryPolicy)
├── tests/
│   ├── CritterCab.Dispatch.Tests/
│   └── CritterCab.Telemetry.Tests/
└── docs/                     # Layered design artifacts (see below)
```

The `docs/` directory is the heart of the project — design happens there before code does. See the [Documentation](#documentation) section below.

## Documentation

For the comprehensive project overview (goals, tentative bounded contexts, technology stack, design principles, parked decisions, open questions), see [`docs/vision/`](docs/vision/README.md).

The rest of the documentation is organized as layered artifacts:

- [Workshops](docs/workshops): Event Modeling and Domain Storytelling session output.
- [Narratives](docs/narratives): journey-scoped domain specs (NDD-informed).
- [Skills](docs/skills): component-scoped implementation patterns and conventions.
- [Rules](docs/rules): AI-optimized encodings of structural constraints for implementation sessions.
- [Prompts](docs/prompts) and [Retrospectives](docs/retrospectives): the session-driven implementation workflow.
- [Context Map](docs/context-map): DDD strategic-design cross-BC relationships.
- [ADRs](docs/decisions): significant architectural decisions with rationale.
- [Research](docs/research): exploratory work and spikes.

## About the Critter Stack

The [Critter Stack](https://github.com/JasperFx) is a family of open-source .NET libraries maintained by JasperFx: Wolverine (messaging, handlers, and now gRPC), Marten (PostgreSQL document store and event sourcing), Polecat (SQL Server document store), Weasel (database schema management), and Alba (integration testing).

## Companion Library: JasperFx ai-skills

Alongside the open-source Critter Stack libraries, JasperFx publishes [`ai-skills`](https://github.com/jasperfx/ai-skills) — a paid, proprietary collection of generic Critter Stack skills (Wolverine, Marten, Polecat) authored by the maintainers. CritterCab's own [skill library](docs/skills/) is deliberately layered on top: it defers to ai-skills for library mechanics and documents project-specific decisions, idioms, and trade-offs that the generic skills can't predict. CritterCab does not duplicate or paraphrase ai-skills content. Contributors with a license install them globally so they sit alongside the project-local skills. See [`docs/skills/README.md`](docs/skills/README.md#companion-jasperfx-ai-skills) for the install command and the layering rationale.

## Contributing

CritterCab is a reference architecture, so the way it grows matters as much as what it grows into. Before opening a PR:

- Read the [project vision](docs/vision/README.md) and the relevant [ADR(s)](docs/decisions/) so changes stay aligned with committed decisions.
- For implementation work, follow the session-driven workflow described in [`docs/prompts/README.md`](docs/prompts/README.md) — one prompt, one session, one PR, paired with a retrospective.
- All contributors are expected to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

Bug reports and discussion are welcome via [GitHub Issues](https://github.com/erikshafer/CritterCab/issues).

## License

[MIT](LICENSE) — see [ADR-008](docs/decisions/008-mit-license.md) for the rationale.

---

## Maintainer

**Erik "Faelor" Shafer**

[LinkedIn](https://www.linkedin.com/in/erikshafer/) · [Blog](https://www.event-sourcing.dev) · [YouTube](https://www.youtube.com/@event-sourcing) · [Bluesky](https://bsky.app/profile/erikshafer.bsky.social)
