# CritterCab

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Wolverine](https://img.shields.io/badge/Wolverine-5.38%2B-512BD4)](https://wolverine.netlify.app/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Marten-336791?logo=postgresql&logoColor=white)](https://martendb.io/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-Polecat-CC2927?logo=microsoftsqlserver&logoColor=white)](https://polecat.netlify.app/)
[![Kafka](https://img.shields.io/badge/Kafka-Transport-231F20?logo=apachekafka&logoColor=white)](https://kafka.apache.org/)
[![gRPC](https://img.shields.io/badge/gRPC-Streaming-4285F4)](https://grpc.io/)

> An open-source ride-sharing reference architecture built on the [Critter Stack](https://wolverine.netlify.app/), showcasing Wolverine's gRPC feature set alongside event-driven messaging, event sourcing, and more.

---

## About

CritterCab is an open-source reference architecture for a ride-sharing platform, built on the Critter Stack, a family of .NET libraries maintained by JasperFx. Its distinguishing focus is **Wolverine's gRPC feature set**, which shipped in Wolverine 5.32. Ride-sharing was chosen because its natural shape (GPS streaming, dispatch matching, trip lifecycle) exercises gRPC in all four modes while leaving room for event sourcing, high-volume telemetry, and multi-transport messaging.

### Technology Versions

| Concern | Package | Version |
|---|---|---|
| Messaging, HTTP, handlers | Wolverine | 5.38+ |
| Event sourcing (PostgreSQL) | Marten | 8.35+ |
| Document store (SQL Server) | Polecat | 3.1+ |
| Local dev orchestration | Aspire | 13.3 |
| Database | PostgreSQL | 18 |

## What's Distinctive

- **gRPC as a design concern.** All four modes (unary, server-streaming, client-streaming, bidirectional) exercised against natural domain use cases.
- **Multi-transport messaging.** gRPC, Kafka (for high-volume telemetry), and likely Azure Service Bus (for business events) in one system, chosen per flow rather than defaulted.
- **Distributed services.** Each bounded context deploys as a separate service, communicating exclusively through messages or gRPC calls.
- **Polyglot participation.** At least one non-.NET service (Go, most likely) participates over gRPC, keeping the contract honest at the wire level.

## Status

Early development. The Dispatch service has its first vertical slice (`RideRequested`) implemented with Marten event sourcing, Wolverine HTTP, and integration tests.

## Learn More

For the comprehensive project overview (goals, tentative bounded contexts, technology stack, design principles, parked decisions, open questions), see [`docs/vision/`](docs/vision/README.md).

The rest of the documentation is organized as layered artifacts:

- [Workshops](docs/workshops): Event Modeling and Domain Storytelling session output.
- [Narratives](docs/narratives): journey-scoped domain specs (NDD-informed).
- [Skills](docs/skills): component-scoped implementation patterns and conventions.
- [Rules](docs/rules): AI-optimized encodings of structural constraints for implementation sessions.
- [Prompts](docs/prompts) and [Retrospectives](docs/retrospectives): the session-driven implementation workflow.
- [ADRs](docs/decisions): significant architectural decisions with rationale.
- [Research](docs/research): exploratory work and spikes.

## About the Critter Stack

The [Critter Stack](https://github.com/JasperFx) is a family of open-source .NET libraries maintained by JasperFx: Wolverine (messaging, handlers, and now gRPC), Marten (PostgreSQL document store and event sourcing), Polecat (SQL Server document store), Weasel (database schema management), and Alba (integration testing).

## License

TBD.

---

## Maintainer

**Erik "Faelor" Shafer**

[LinkedIn](https://www.linkedin.com/in/erikshafer/) · [Blog](https://www.event-sourcing.dev) · [YouTube](https://www.youtube.com/@event-sourcing) · [Bluesky](https://bsky.app/profile/erikshafer.bsky.social)
