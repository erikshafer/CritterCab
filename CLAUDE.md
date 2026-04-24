# CritterCab — AI Development Guidelines

CritterCab is an open-source ride-sharing reference architecture built on the Critter Stack, showcasing Wolverine's gRPC feature set alongside event-driven messaging, event sourcing, and multi-transport messaging. It is structured as a set of **separately deployable services**, one per bounded context (or small group thereof), communicating exclusively via gRPC or Wolverine messages.

The project is currently in the design phase. No runnable code yet.

> For the canonical project overview (goals, tentative bounded contexts, technology stack, design principles, parked decisions, open questions), see [`docs/vision/README.md`](./docs/vision/README.md). **This file is a routing layer, not a manual.**

---

## Quick Start

1. **Read the vision doc.** [`docs/vision/README.md`](./docs/vision/README.md) is the source of truth for what CritterCab is, where it is going, and why.
2. **Understand the document layers.** The documentation is organized as layered artifacts, each with a distinct job:
   - **Workshops** (`docs/workshops/`) capture Event Modeling and Domain Storytelling session output.
   - **Narratives** (`docs/narratives/`) are journey-scoped domain specs (NDD-informed).
   - **Skills** (`docs/skills/`) are component-scoped implementation patterns and conventions.
   - **Rules** (`docs/rules/`) are AI-optimized encodings of structural constraints for implementation sessions.
   - **Prompts** (`docs/prompts/`) and **Retrospectives** (`docs/retrospectives/`) capture the session-driven implementation workflow.
   - **ADRs** (`docs/decisions/`) capture significant architectural decisions.
   - **Research** (`docs/research/`) captures exploratory work and spikes.
3. **Before implementing anything**, load the relevant skill file(s) from `docs/skills/`, the relevant rule file(s) from `docs/rules/`, and reference the narrative(s) the prompt is satisfying.

---

## Session Workflow

CritterCab implementation work runs through a **narrative → prompt → execute → retrospective** loop. Domain behavior is captured as narratives; prompts reference the narrative(s) they implement; implementation produces code; retrospectives close the session.

- [`docs/narratives/README.md`](./docs/narratives/README.md) — what narratives are and how they inform prompts.
- [`docs/prompts/README.md`](./docs/prompts/README.md) — session prompt template and conventions. Read before authoring a new prompt.
- [`docs/retrospectives/README.md`](./docs/retrospectives/README.md) — retrospective template and conventions. Read before writing a retro at session close.

A session prompt and its retro share a slug so they sort together. The retro is part of the session's deliverable PR, not a follow-up.

---

## Architectural Non-Negotiables

These are structural rules already committed. More will be added as bounded contexts and services firm up.

- **Services are deployed separately per bounded context (or small group).** Each service owns its own data store. Services never reference each other's internals.
- **Cross-service communication is gRPC calls or Wolverine messages, nothing else.** No shared databases. No shared application-layer code between services. No direct handler-to-handler calls.
- **Transport is chosen per flow type, not defaulted.** High-volume telemetry goes to Kafka. Business events go to Azure Service Bus (if adopted). Service-to-service calls and streaming go to gRPC.
- **Identity provider is swappable.** The Identity BC is an anti-corruption layer between the provider (Entra External ID in production, alternatives in local dev) and domain events. Other services do not couple to provider-specific types.
- **Protobuf contracts are first-class artifacts.** Reviewed and evolved with the care given to API contracts.

For AI-optimized rule encodings of these decisions, see [`docs/rules/structural-constraints.md`](./docs/rules/structural-constraints.md). See [`docs/vision/README.md`](./docs/vision/README.md) for the full set of design principles and their rationale.

---

## Preferred Tools and Stack

See the vision doc for the tentative stack in full. Key facts, to save a trip:

| Concern | Tool |
|---|---|
| Language | C# 14 / .NET 10+ |
| Messaging, gRPC, handlers | Wolverine 5.32+ |
| Event sourcing and document store (PostgreSQL) | Marten |
| Document store (SQL Server) | Polecat |
| High-volume telemetry transport | Kafka (via Wolverine's Kafka transport) |
| Business-event transport (likely) | Azure Service Bus |
| Polyglot service | Go (first non-.NET service, participates over gRPC) |

---

## Context7

For Wolverine, Marten, and Polecat capabilities beyond what skill files already cover:

- Wolverine: `/jasperfx/wolverine`
- Marten: `/jasperfx/marten`
- Polecat: `/jasperfx/polecat`

---

## Do Not

- Commit directly to `main` — branch and PR
- Share a database across services
- Add a service project reference to another service project
- Put code in prompt documents
