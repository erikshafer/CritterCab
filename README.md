# CritterCab

[![CI](https://github.com/erikshafer/CritterCab/actions/workflows/dotnet.yml/badge.svg)](https://github.com/erikshafer/CritterCab/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> An open-source ride-sharing reference architecture built on the [Critter Stack](https://github.com/JasperFx): separately deployed services, one per bounded context, talking only over gRPC and Wolverine messages.

---

## What it demonstrates

A driver's phone streams GPS pings into the **Telemetry** service over a Wolverine gRPC client stream (`TelemetryService.ReportLocations`). Telemetry drops low-accuracy fixes, computes each ping's H3 cell, and publishes only on a cell change or a heartbeat: `DriverLocationUpdated`, as binary protobuf, to the Kafka topic `telemetry.driver-location-updated`, partitioned by driver, before it records the driver's last-known position. The **Dispatch** service consumes that topic into an `AvailableDriver` document (last writer wins per driver, guarded by a revision), and when a rider submits a ride request (`POST /api/rides/request`), Dispatch quotes a fare (against a stub Pricing client; Pricing is not built) and selects candidates with an H3 k-ring query over those documents. The availability half of `AvailableDriver` has no feeder yet: no service publishes driver availability, so a real end-to-end run honestly ends in `NoCandidatesAvailable` rather than fabricating available drivers. The integration tests drive both halves, including availability, in-process.

## Where the model lives

Each service's Event Model will be a curated `*.emodel.yaml` committed beside its `Program.cs`, reviewed in the PR like code and compared against the model derived from the running application. Their placement, naming and schema follow what CritterMart's Orders experiment settles; until a service has one, the design reasoning for its slices is in the workshop minutes under [`docs/workshops/`](docs/workshops/).

## Running it

Prerequisites: the [.NET SDK](https://dotnet.microsoft.com/download) for the target framework in [`Directory.Build.props`](Directory.Build.props) and Docker (or another OCI runtime). Aspire starts PostgreSQL and a Kafka broker as containers; the integration tests use [Testcontainers](https://testcontainers.com/) for theirs.

```bash
git clone https://github.com/erikshafer/CritterCab.git
cd CritterCab

# PostgreSQL, Kafka, Dispatch and Telemetry, with the Aspire dashboard on https://localhost:5300
dotnet run --project src/CritterCab.AppHost

# Unit and Alba integration tests (Testcontainers-backed)
dotnet test CritterCab.slnx
```

Local ports sit in CritterCab's `53xx` band: dashboard `5300`–`5307`, Dispatch `5310` (https) / `5311` (http), Telemetry `5315` / `5316`, PostgreSQL `5390`, Kafka `5392` (see the [`aspire` skill](docs/skills/aspire/SKILL.md) § Port allocation). Each service also boots on its own with `dotnet run`; without a database or broker it degrades rather than failing (Dispatch serves a stub candidate source, Telemetry logs what it would have published).

## Repository layout

```
.
├── CritterCab.slnx            # The solution; CI builds and tests exactly what it lists
├── Directory.Packages.props   # Every package version, pinned centrally
├── protos/                    # Protobuf contracts (buf-linted in CI)
├── src/
│   ├── CritterCab.AppHost/    # Aspire AppHost: containers, services, ports
│   ├── CritterCab.Dispatch/   # RideRequesting, FareQuoting, CandidateSelection, AvailableDrivers
│   └── CritterCab.Telemetry/  # TelemetryPolicy, ReportLocations, LastKnownPosition
├── tests/                     # One Alba + Testcontainers test project per service
└── docs/                      # Design record (below)
```

## Design history

CritterCab's first version (April to July 2026) is tagged [`v1`](https://github.com/erikshafer/CritterCab/tree/v1). The repository continued in place from that tag; [`docs/vision/README.md`](docs/vision/README.md) states what CritterCab is for and where it is going.

- [Workshops](docs/workshops/): minutes of the Event Modeling and Domain Storytelling sessions.
- [Decisions](docs/decisions/): ADRs, with a preface on how the v1 record binds.
- [Context map](docs/context-map/README.md): relationships between bounded contexts.
- [Rules](docs/rules/) and [skills](docs/skills/): structural constraints and implementation conventions, each grounded in code in this repository.
- [Prompts](docs/prompts/) and [retrospectives](docs/retrospectives/): one per working session.
- [Narratives](docs/narratives/) and [research](docs/research/): journey write-ups and background reading.

## Contributing

Read the [vision](docs/vision/README.md) and any [ADR](docs/decisions/) that governs the area you are changing. Work follows the session cadence in [`docs/prompts/README.md`](docs/prompts/README.md): one prompt, one session, one PR, with its retrospective inside the PR. Everyone is expected to follow the [Code of Conduct](CODE_OF_CONDUCT.md). Issues and discussion are welcome on [GitHub Issues](https://github.com/erikshafer/CritterCab/issues).

CritterCab's skills defer to JasperFx's [`ai-skills`](https://github.com/jasperfx/ai-skills) (a paid collection of generic Critter Stack skills) for library mechanics and record only this project's conventions; see [`docs/skills/README.md`](docs/skills/README.md#companion-jasperfx-ai-skills).

## License

[MIT](LICENSE); see [ADR-008](docs/decisions/008-mit-license.md).

---

## Maintainer

**Erik "Faelor" Shafer**

[LinkedIn](https://www.linkedin.com/in/erikshafer/) · [Blog](https://www.event-sourcing.dev) · [YouTube](https://www.youtube.com/@event-sourcing) · [Bluesky](https://bsky.app/profile/erikshafer.bsky.social)
