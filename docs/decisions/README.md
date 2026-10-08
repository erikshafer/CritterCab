# CritterCab ADR Index

Architectural Decision Records (ADRs) capture significant decisions, the options considered, and the rationale for the chosen path. They are the canonical answer to "why does the system work this way?"

For the template and guidelines on when to write an ADR, see [ADR-001](./001-record-architecture-decisions.md).

**ADRs 001 to 019 are the v1 record.** CritterCab v1 closed at the `v1` tag (`4d8bde4`) and v2 was declared in place on 2026-10-08 ([`docs/planning/2026-10-05-crittercab-v2-evaluation.md`](../planning/2026-10-05-crittercab-v2-evaluation.md) § Decision). The statuses below are as each ADR was written and are not changed by that boundary; each ADR is re-affirmed, amended or superseded by the first v2 PR that touches its subject, and until then binds as written. ADR-020 (the v2 topology: five contexts, five deployables, a build order) is proposed in the evaluation and is not authored until it is signed off.

| ADR | Title | Status |
|-----|-------|--------|
| [001](./001-record-architecture-decisions.md) | Record Architecture Decisions | Accepted |
| [002](./002-distributed-services-per-bounded-context.md) | Distributed Services per Bounded Context | Accepted |
| [003](./003-spec-anchored-development.md) | Spec-Anchored Development | Accepted |
| [004](./004-design-phase-workflow-sequence.md) | Design-Phase Workflow Sequence | Accepted |
| [005](./005-transport-selection-by-flow-type.md) | Transport Selection by Flow Type | Accepted |
| [006](./006-identity-provider-as-swappable-anti-corruption-layer.md) | Identity Provider as Swappable Anti-Corruption Layer | Accepted |
| [007](./007-azure-as-deployment-target.md) | Azure as Deployment Target | Accepted |
| [008](./008-mit-license.md) | MIT License | Accepted |
| [009](./009-protobuf-contracts-as-first-class-artifacts.md) | Protobuf Contracts as First-Class Artifacts | Accepted |
| [010](./010-critter-stack-as-foundational-technology.md) | Critter Stack as Foundational Technology | Accepted |
| [011](./011-configuration-as-events-bootstrap.md) | Configuration-as-Events Bootstrap Strategy | Accepted |
| [012](./012-aggregate-per-invariant.md) | Aggregate-per-Invariant | Accepted |
| [013](./013-shared-cross-bc-identifier.md) | Shared Cross-BC Identifier | Accepted |
| [014](./014-asb-topic-naming-convention.md) | Azure Service Bus Topic Naming Convention | Accepted |
| [015](./015-driver-app-projection-timing-budget.md) | Driver-App Projection Timing Budget | Accepted |
| [016](./016-frontend-live-update-transport.md) | Frontend Live-Update Transport | Accepted |
| [017](./017-rabbitmq-for-critterwatch.md) | RabbitMQ for CritterWatch | Accepted |
| [018](./018-candidate-projection-ownership-and-telemetry-geospatial-supply.md) | Candidate-Projection Ownership and Telemetry Geospatial Supply | Accepted |
| [019](./019-transport-agnostic-topic-naming.md) | Transport-Agnostic Topic Naming | Accepted |
