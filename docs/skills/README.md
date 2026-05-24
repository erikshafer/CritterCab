# CritterCab Skills

Implementation pattern documents for CritterCab. Each skill encodes hard-won conventions for a specific aspect of building, designing, or testing the project — so contributors and AI agents don't rediscover known solutions every session.

The skill library follows the [agentskills.io](https://agentskills.io/specification) open standard. Every skill lives in its own directory with a `SKILL.md` containing YAML frontmatter and Markdown instructions. Optional `references/` subdirectories hold deep-dive material loaded on demand.

## How to use this library

Skills are loaded into context by the Claude agent (or read manually by humans) when relevant to the current task. The frontmatter `description` field on each skill is loaded at agent startup to enable activation matching; the body is loaded when the skill is activated.

On top of agent-side activation, the README exposes three navigation affordances for human and agent readers: the [skill index by cluster](#skill-index-by-cluster) (skills grouped by primary-value axis), the [skill index by tag](#skill-index-by-tag) (skills grouped by cross-cutting concern), and the [entry-point hubs](#entry-point-hubs) (task-keyed routing into the skill graph with explicit upstream prerequisites and downstream follow-ups). The [cross-reference graph](#cross-reference-graph) further visualizes the dependency topology phase-by-phase.

When working on CritterCab:

- Identify the task type (designing, implementing, testing, deciding).
- Find the relevant entry-point skill in the [Entry-point hubs](#entry-point-hubs) section.
- Load that skill, plus any `Upstream` skills it names.
- Follow `Downstream` references as the work progresses.

Cross-references between skills are explicit. Each skill's `See Also` section names upstream prerequisites and downstream follow-ups, plus external references (ADRs, vision doc, JasperFx ai-skills).

## Status

| Phase | Description | Status |
|---|---|---|
| Phase 1 | Pre-implementation foundations: language standards, design conventions, contract governance, transport decisions, service skeleton | **Complete** (6 skills) |
| Phase 2 | First service implementation: composition root, store wiring, handlers, projections, testing, local-dev orchestration | **Complete** (17 skills) |
| Phase 3 | First cross-service flow: gRPC services, Kafka and ASB transports, identity ACL, distributed observability | **Complete** (8 skills) |
| Phase 4 | Complexity arrives: sagas, advanced patterns, Polecat event sourcing, polyglot Go service, complete observability, advanced testing | **Complete** (9 skills) |
| Phase 5 | Reconciliation pass — cross-check against ai-skills, eliminate duplication, contribute generic patterns upstream | **Complete** (25 / 40 reconciled; 15 placeholder-cleanup items tracked as Phase 6) |

**All five phases complete as of 2026-05-06.** 40 skills authored across Phases 1–4; Phase 5 — the reconciliation pass against [JasperFx ai-skills](#companion-jasperfx-ai-skills) — closed at its substantive deliverable: every counterpart-rich skill reconciled, 53 upstream-contribution candidates flagged, 16 Cab coverage gaps documented, 2 ai-skills content drift entries surfaced. The remaining 15 skills (project-specific patterns, Microsoft tooling, generic ecosystem CLIs with no ai-skills counterpart) are scoped as a Phase 6 placeholder-cleanup pass. See the [skills-foundation-phase-5 retrospective](../retrospectives/skills-foundation-phase-5.md) for the full reconciliation record, the upstream-contribution roadmap, and the Phase 6 follow-up plan.

## Skill index by cluster

CritterCab's skill clusters split into product/library clusters and topic/concern clusters. The disambiguation rule when a skill spans both axes: pick the cluster that captures the primary value of the skill — the secondary axis goes in `tags`. See `_template/SKILL.md` for the full cluster vocabulary and disambiguation rule.

### Product/library clusters

| Cluster | Authored | Planned |
|---|---|---|
| `core` | `csharp-coding-standards`, `domain-event-conventions`, `event-modeling` | — |
| `wolverine` | `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers`, `wolverine-grpc-handlers`, `wolverine-grpc-bidirectional-handlers`, `wolverine-kafka`, `wolverine-azure-service-bus`, `wolverine-sagas` | — |
| `marten` | `marten-aggregates`, `marten-wolverine-aggregates`, `marten-projections`, `marten-querying`, `marten-async-daemon`, `dynamic-consistency-boundary` | — |
| `polecat` | `polecat-event-sourcing`, `polecat-document-store` | — |
| `infrastructure` | `aspire`, `aspire-service-defaults`, `cli-aspire`, `cli-jasperfx`, `cli-grpc-tooling`, `cli-kafka-tooling`, `cli-azure-messaging` | — |

### Topic/concern clusters

| Cluster | Authored | Planned |
|---|---|---|
| `distributed-services` | `adding-a-service`, `service-bootstrap`, `vertical-slice-organization`, `distributed-saga-considerations` | — |
| `grpc` | `protobuf-contracts`, `grpc-vs-other-transports` | — |
| `transports` | `transport-selection` | — |
| `identity` | `identity-acl` | — |
| `polyglot` | `polyglot-go-service` | — |
| `testing` | `testing-fundamentals`, `testing-integration`, `testing-advanced` | — |
| `observability` | `observability-tracing`, `observability-metrics` | — |
| `security` | — | `security-headers` |
| `api-documentation` | — | `dotnet-openapi` |

## Skill index by tag

Tags are the complementary discovery surface to clusters. Where a cluster captures a skill's primary-value axis, tags surface its secondary dimensions — useful for cross-cutting searches like "all skills that touch Aspire" or "everything tagged `event-sourcing`". The high-frequency tags below are curated for navigation; for the full per-skill tag list, see each skill's frontmatter.

### Stack

| Tag | Skills |
|---|---|
| `wolverine` | `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers`, `wolverine-grpc-handlers`, `wolverine-grpc-bidirectional-handlers`, `wolverine-kafka`, `wolverine-azure-service-bus`, `wolverine-sagas`, `marten-wolverine-aggregates`, `dynamic-consistency-boundary`, `service-bootstrap`, `transport-selection`, `observability-tracing`, `observability-metrics` |
| `marten` | `marten-aggregates`, `marten-wolverine-aggregates`, `marten-projections`, `marten-querying`, `marten-async-daemon`, `dynamic-consistency-boundary`, `domain-event-conventions`, `service-bootstrap`, `wolverine-sagas`, `observability-tracing`, `observability-metrics` |
| `polecat` | `polecat-event-sourcing`, `polecat-document-store`, `service-bootstrap`, `wolverine-sagas`, `observability-metrics` |
| `aspire` | `aspire`, `aspire-service-defaults`, `cli-aspire`, `adding-a-service`, `service-bootstrap`, `polyglot-go-service` |
| `grpc` | `wolverine-grpc-handlers`, `wolverine-grpc-bidirectional-handlers`, `protobuf-contracts`, `grpc-vs-other-transports`, `polyglot-go-service`, `cli-grpc-tooling`, `transport-selection` |
| `kafka` | `wolverine-kafka`, `cli-kafka-tooling`, `aspire`, `transport-selection`, `grpc-vs-other-transports`, `testing-integration`, `testing-advanced` |
| `azure-service-bus` | `wolverine-azure-service-bus`, `cli-azure-messaging`, `aspire`, `grpc-vs-other-transports`, `testing-advanced` |
| `event-hubs` | `wolverine-kafka`, `cli-kafka-tooling`, `cli-azure-messaging`, `transport-selection` |
| `opentelemetry` | `aspire-service-defaults`, `observability-tracing`, `observability-metrics` |
| `dotnet` | `csharp-coding-standards`, `adding-a-service` |
| `go` | `polyglot-go-service` |

### Patterns

| Tag | Skills |
|---|---|
| `event-sourcing` | `domain-event-conventions`, `marten-aggregates`, `polecat-event-sourcing`, `dynamic-consistency-boundary` |
| `decider-pattern` | `marten-aggregates`, `marten-wolverine-aggregates`, `marten-projections`, `testing-fundamentals` |
| `dcb` (dynamic consistency boundary) | `dynamic-consistency-boundary`, `polecat-event-sourcing` |
| `projections` | `marten-projections`, `marten-async-daemon`, `cli-jasperfx` |
| `handlers` | `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers`, `marten-wolverine-aggregates` |
| `saga` / `process-manager` | `wolverine-sagas`, `distributed-saga-considerations` |
| `decision-framework` | `transport-selection`, `grpc-vs-other-transports` |
| `conventions` | `csharp-coding-standards`, `domain-event-conventions`, `wolverine-handlers`, `vertical-slice-organization` |
| `domain-modeling` | `csharp-coding-standards`, `domain-event-conventions` |
| `anti-corruption-layer` | `identity-acl` |

### Activities

| Tag | Skills |
|---|---|
| `testing` | `testing-fundamentals`, `testing-integration`, `testing-advanced` |
| `cli` | `cli-aspire`, `cli-jasperfx`, `cli-grpc-tooling`, `cli-kafka-tooling`, `cli-azure-messaging` |
| `ci` | `cli-aspire`, `cli-jasperfx`, `cli-grpc-tooling` |
| `bootstrap` | `service-bootstrap`, `adding-a-service` |
| `debugging` | `cli-kafka-tooling`, `cli-azure-messaging` |
| `event-modeling` | `event-modeling` |

### ADR cross-references

| ADR | Skills tagged |
|---|---|
| `adr-005` (transport selection) | `transport-selection`, `grpc-vs-other-transports`, `wolverine-kafka`, `wolverine-azure-service-bus` |
| `adr-006` (identity provider design) | `identity-acl` |
| `adr-009` (protobuf contracts as first-class artifacts) | `protobuf-contracts`, `cli-grpc-tooling` |

## Entry-point hubs

When starting a task, the entry-point skill is the first to load. Upstream skills load if unfamiliar; downstream skills load as the work progresses.

### Design and contract tasks

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Designing a new feature or journey | `event-modeling` | — | `domain-event-conventions`, eventual implementation skills |
| Authoring or reviewing C# code | `csharp-coding-standards` | — | `domain-event-conventions`, plus the relevant implementation skill |
| Designing a domain event | `domain-event-conventions` | `csharp-coding-standards` | `marten-aggregates`, transport skills |
| Designing a cross-service contract | `protobuf-contracts` | `csharp-coding-standards`, `domain-event-conventions` | `cli-grpc-tooling`, `wolverine-grpc-handlers` |
| Choosing a transport for a cross-service flow | `transport-selection` | `protobuf-contracts`, `domain-event-conventions` | `wolverine-kafka`, `wolverine-azure-service-bus`, `wolverine-grpc-handlers` |
| Choosing between gRPC and other transports for a specific flow | `grpc-vs-other-transports` | `transport-selection`, `protobuf-contracts` | `wolverine-grpc-handlers`, `wolverine-grpc-bidirectional-handlers`, `wolverine-kafka` |

### Service implementation

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Adding a new service from scratch | `adding-a-service` | `transport-selection`, `protobuf-contracts`, `domain-event-conventions` | `service-bootstrap`, `vertical-slice-organization` |
| Bootstrapping a service's `Program.cs` | `service-bootstrap` | `csharp-coding-standards`, `adding-a-service` | `wolverine-handlers`, `marten-aggregates`, `aspire`, `observability-tracing` |
| Authoring or modifying the shared ServiceDefaults library | `aspire-service-defaults` | `service-bootstrap`, `aspire` | `observability-tracing`, `testing-integration` |
| Organizing code within a service | `vertical-slice-organization` | `service-bootstrap` | handler and aggregate skills |

### Wolverine handler authoring

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Authoring a Wolverine handler (general) | `wolverine-handlers` | `service-bootstrap`, `vertical-slice-organization` | `wolverine-http-handlers`, `wolverine-messaging-handlers`, `marten-wolverine-aggregates`, `testing-fundamentals` |
| Authoring an HTTP endpoint | `wolverine-http-handlers` | `wolverine-handlers` | `marten-wolverine-aggregates`, `testing-integration` |
| Authoring a messaging handler | `wolverine-messaging-handlers` | `wolverine-handlers` | `marten-wolverine-aggregates`, `testing-integration` |

### Cross-service flows (gRPC, Kafka, ASB)

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Implementing a gRPC service | `wolverine-grpc-handlers` | `protobuf-contracts`, `service-bootstrap` | `cli-grpc-tooling`, `identity-acl`, `observability-tracing`, `wolverine-grpc-bidirectional-handlers` |
| Implementing a bidirectional or server-streaming gRPC service | `wolverine-grpc-bidirectional-handlers` | `wolverine-grpc-handlers`, `protobuf-contracts` | `cli-grpc-tooling`, `testing-advanced` |
| Wiring Kafka for high-volume streams | `wolverine-kafka` | `transport-selection`, `wolverine-messaging-handlers` | `cli-kafka-tooling`, `observability-tracing` |
| Wiring ASB for domain events | `wolverine-azure-service-bus` | `transport-selection`, `wolverine-messaging-handlers` | `cli-azure-messaging`, `observability-tracing` |
| Testing gRPC endpoints from CLI | `cli-grpc-tooling` | `wolverine-grpc-handlers`, `protobuf-contracts` | `identity-acl` for auth tokens |
| Inspecting Kafka topics and messages | `cli-kafka-tooling` | `wolverine-kafka` | `cli-azure-messaging` for Event Hubs management |
| Inspecting ASB queues, topics, and DLQ | `cli-azure-messaging` | `wolverine-azure-service-bus` | — |

### Sagas and orchestration

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Implementing a Wolverine saga | `wolverine-sagas` | `wolverine-messaging-handlers`, `marten-wolverine-aggregates` | `distributed-saga-considerations`, `polecat-event-sourcing` (for Polecat-backed sagas), `testing-advanced` |
| Designing a saga that spans services | `distributed-saga-considerations` | `wolverine-sagas`, `transport-selection` | `testing-advanced` |

### Identity and auth

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Adding auth to a Cab service | `identity-acl` | `service-bootstrap`, `wolverine-grpc-handlers` | `observability-tracing` |
| Obtaining a dev-mode token for CLI testing | `identity-acl` | `cli-grpc-tooling` | — |

### Marten event-sourced and document work

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Implementing an event-sourced aggregate | `marten-aggregates` | `domain-event-conventions`, `service-bootstrap` | `marten-wolverine-aggregates`, `marten-projections` |
| Wiring an aggregate to a Wolverine handler | `marten-wolverine-aggregates` | `marten-aggregates`, `wolverine-handlers` | `dynamic-consistency-boundary`, `testing-integration` |
| Implementing a write path that spans multiple streams | `dynamic-consistency-boundary` | `marten-wolverine-aggregates`, `marten-projections` | `testing-integration` |
| Building a projection (live or async) | `marten-projections` | `marten-aggregates`, `domain-event-conventions` | `marten-async-daemon`, `marten-querying` |
| Querying projected read models | `marten-querying` | `marten-projections` | `wolverine-http-handlers` |
| Configuring the async daemon | `marten-async-daemon` | `marten-projections`, `service-bootstrap` | `testing-integration`, `cli-jasperfx` |

### Polecat event-sourced and document work

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Implementing a Polecat event-sourced aggregate (SQL Server) | `polecat-event-sourcing` | `domain-event-conventions`, `service-bootstrap` | `polecat-document-store`, `wolverine-sagas`, `observability-metrics`, `testing-advanced` |
| Storing documents in Polecat (SQL Server) | `polecat-document-store` | `polecat-event-sourcing` | `testing-advanced` |

### Polyglot services

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Adding a non-.NET service to the system | `polyglot-go-service` | `wolverine-grpc-handlers`, `wolverine-kafka`, `identity-acl`, `observability-tracing` | `testing-advanced` |

### Testing

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Writing a unit test for a handler or aggregate | `testing-fundamentals` | `wolverine-handlers` (or `marten-aggregates`) | `testing-integration` when the test grows beyond pure logic |
| Writing an integration test | `testing-integration` | `testing-fundamentals`, `service-bootstrap`, `marten-async-daemon` | `testing-advanced` for multi-host, gRPC streaming, vhost isolation, and polyglot patterns |
| Writing an advanced integration test | `testing-advanced` | `testing-integration`, plus the Phase 4 implementation skill the test exercises | — (terminal) |

### Infrastructure and tooling

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Setting up local-dev orchestration | `aspire` | `service-bootstrap` | `cli-aspire` |
| Operating the AppHost from a terminal or CI | `cli-aspire` | `aspire` | `cli-jasperfx` for service-internal commands |
| Running CLI commands inside a Cab service | `cli-jasperfx` | `service-bootstrap` | `cli-aspire` for AppHost-level orchestration |

### Observability

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Setting up distributed tracing | `observability-tracing` | `service-bootstrap`, `aspire` | `observability-metrics`, `testing-advanced` |
| Setting up metrics (counters, histograms, observable gauges) | `observability-metrics` | `observability-tracing`, `service-bootstrap` | `testing-advanced` |
| Understanding Wolverine's trace spans | `observability-tracing` | `wolverine-handlers` | — |

## Cross-reference graph

### Phase 1 — Foundations

```mermaid
graph LR
    EM[event-modeling]
    CCS[csharp-coding-standards]
    DEC[domain-event-conventions]
    PC[protobuf-contracts]
    TS[transport-selection]
    AS[adding-a-service]

    EM --> DEC
    CCS --> DEC
    DEC --> PC
    DEC --> TS
    DEC --> AS
    PC --> TS
    PC --> AS
    TS --> AS

    classDef phase1 fill:#90ee90,stroke:#333,stroke-width:2px
    class EM,CCS,DEC,PC,TS,AS phase1
```

### Phase 2 — First service implementation

```mermaid
graph TB
    %% Anchors from Phase 1
    AS[adding-a-service<br/><i>Phase 1</i>]
    DEC[domain-event-conventions<br/><i>Phase 1</i>]

    %% Service composition root
    SB[service-bootstrap]
    VSO[vertical-slice-organization]

    AS --> SB
    AS --> VSO
    SB --> VSO

    %% Wolverine handler family
    WH[wolverine-handlers]
    WHH[wolverine-http-handlers]
    WMH[wolverine-messaging-handlers]

    SB --> WH
    VSO --> WH
    WH --> WHH
    WH --> WMH

    %% Marten cluster
    MA[marten-aggregates]
    MWA[marten-wolverine-aggregates]
    MP[marten-projections]
    MQ[marten-querying]
    MAD[marten-async-daemon]
    DCB[dynamic-consistency-boundary]

    DEC --> MA
    SB --> MA
    MA --> MWA
    WH --> MWA
    MA --> MP
    MWA --> DCB
    MP --> DCB
    MP --> MQ
    MP --> MAD
    WHH --> MQ

    %% Testing
    TF[testing-fundamentals]
    TI[testing-integration]

    WH --> TF
    MA --> TF
    TF --> TI
    SB --> TI
    MAD --> TI

    %% Infrastructure
    ASP[aspire]
    CLA[cli-aspire]
    CLJ[cli-jasperfx]

    SB --> ASP
    ASP --> CLA
    SB --> CLJ
    MAD --> CLJ

    classDef phase1 fill:#90ee90,stroke:#333,stroke-width:2px
    classDef phase2 fill:#87ceeb,stroke:#333,stroke-width:2px

    class AS,DEC phase1
    class SB,VSO,WH,WHH,WMH,MA,MWA,MP,MQ,MAD,DCB,TF,TI,ASP,CLA,CLJ phase2
```

### Phase 3 — First cross-service flow

```mermaid
graph TB
    %% Anchors from earlier phases
    TS[transport-selection<br/><i>Phase 1</i>]
    PC[protobuf-contracts<br/><i>Phase 1</i>]
    WMH[wolverine-messaging-handlers<br/><i>Phase 2</i>]
    SB[service-bootstrap<br/><i>Phase 2</i>]
    ASP[aspire<br/><i>Phase 2</i>]

    %% gRPC flow
    WGH[wolverine-grpc-handlers]
    CGT[cli-grpc-tooling]

    PC --> WGH
    SB --> WGH
    WGH --> CGT

    %% Kafka flow
    WK[wolverine-kafka]
    CKT[cli-kafka-tooling]

    TS --> WK
    WMH --> WK
    WK --> CKT

    %% ASB flow
    WASB[wolverine-azure-service-bus]
    CAM[cli-azure-messaging]

    TS --> WASB
    WMH --> WASB
    WASB --> CAM

    %% Identity
    IACL[identity-acl]

    SB --> IACL
    WGH --> IACL
    WASB --> IACL

    %% Observability
    OT[observability-tracing]

    SB --> OT
    ASP --> OT

    classDef phase1 fill:#90ee90,stroke:#333,stroke-width:2px
    classDef phase2 fill:#87ceeb,stroke:#333,stroke-width:2px
    classDef phase3 fill:#ffd700,stroke:#333,stroke-width:2px

    class TS,PC phase1
    class WMH,SB,ASP phase2
    class WGH,WK,WASB,CGT,CKT,CAM,IACL,OT phase3
```

The Phase 3 graph shows three transport-specific vertical flows (gRPC, Kafka, ASB) anchored on Phase 1 decision skills (`transport-selection`, `protobuf-contracts`) and Phase 2 handler and composition skills (`wolverine-messaging-handlers`, `service-bootstrap`). Identity and observability cut across the transport flows — `identity-acl` draws from gRPC handlers and ASB, while `observability-tracing` draws from the service bootstrap and Aspire foundation. Phase 4 extends this with sagas, Polecat as an alternative event store, the polyglot Go service, bidirectional gRPC, and the metrics half of the observability story.

### Phase 4 — Sagas, Polecat, polyglot, complete observability and testing

```mermaid
graph TB
    %% Anchors from earlier phases
    DEC[domain-event-conventions<br/><i>Phase 1</i>]
    TS[transport-selection<br/><i>Phase 1</i>]
    PC[protobuf-contracts<br/><i>Phase 1</i>]
    SB[service-bootstrap<br/><i>Phase 2</i>]
    WMH[wolverine-messaging-handlers<br/><i>Phase 2</i>]
    MWA[marten-wolverine-aggregates<br/><i>Phase 2</i>]
    TI[testing-integration<br/><i>Phase 2</i>]
    WGH[wolverine-grpc-handlers<br/><i>Phase 3</i>]
    WK[wolverine-kafka<br/><i>Phase 3</i>]
    IACL[identity-acl<br/><i>Phase 3</i>]
    OT[observability-tracing<br/><i>Phase 3</i>]

    %% Bidirectional gRPC and transport comparison
    WGBH[wolverine-grpc-bidirectional-handlers]
    GVOT[grpc-vs-other-transports]

    PC --> WGBH
    WGH --> WGBH
    TS --> GVOT

    %% Sagas and distributed orchestration
    WS[wolverine-sagas]
    DSC[distributed-saga-considerations]

    WMH --> WS
    MWA --> WS
    WS --> DSC

    %% Polecat — SQL Server alternative event store
    PES[polecat-event-sourcing]
    PDS[polecat-document-store]

    DEC --> PES
    SB --> PES
    PES --> PDS

    %% Polyglot Go service
    PGS[polyglot-go-service]

    WGH --> PGS
    WK --> PGS
    IACL --> PGS
    OT --> PGS

    %% Metrics — the companion to Phase 3 tracing
    OM[observability-metrics]

    OT --> OM

    %% Advanced testing — the terminal node every Phase 4 skill funnels into
    TA[testing-advanced]

    TI --> TA
    WGBH --> TA
    WS --> TA
    PES --> TA
    PGS --> TA
    OM --> TA

    classDef phase1 fill:#90ee90,stroke:#333,stroke-width:2px
    classDef phase2 fill:#87ceeb,stroke:#333,stroke-width:2px
    classDef phase3 fill:#ffd700,stroke:#333,stroke-width:2px
    classDef phase4 fill:#dda0dd,stroke:#333,stroke-width:2px

    class DEC,TS,PC phase1
    class SB,WMH,MWA,TI phase2
    class WGH,WK,IACL,OT phase3
    class WGBH,GVOT,WS,DSC,PES,PDS,PGS,OM,TA phase4
```

The Phase 4 graph shows complexity arriving from multiple directions. Sagas (`wolverine-sagas`, `distributed-saga-considerations`) build on the Phase 2 messaging-handler and aggregate-handler skills. Polecat (`polecat-event-sourcing`, `polecat-document-store`) anchors on the Phase 1 domain-event conventions and Phase 2 service-bootstrap as the SQL-Server alternative to Marten on PostgreSQL. The polyglot Go service (`polyglot-go-service`) braids Phase 3 cross-service primitives (gRPC, Kafka, identity, tracing) into a non-.NET runtime. `observability-metrics` is the metric companion to Phase 3's `observability-tracing`. `wolverine-grpc-bidirectional-handlers` and `grpc-vs-other-transports` complete the gRPC story. `testing-advanced` is the terminal node — every other Phase 4 implementation skill funnels into it because the test patterns it documents (multi-host scenarios, streaming gRPC harnesses, dynamic database-per-fixture, vhost isolation, polyglot boundary tests, OTel signal verification) are how each new capability gets exercised end-to-end.

Phase 5 was the reconciliation pass against [JasperFx ai-skills](#companion-jasperfx-ai-skills). Phase 4 deliberately authored some skills (notably `wolverine-sagas`, `polecat-event-sourcing`, `polecat-document-store`) ahead of comparable ai-skills coverage where Cab needed the conventions to make implementation decisions. The Phase 5 pass identified what's truly Cab-specific (kept here), what's generic enough to contribute upstream (captured in the retrospective's 53-entry roadmap), and what overlapped and was deduplicated by deferring to ai-skills with a thinner Cab-specific layer on top.

## Companion: JasperFx ai-skills

Several CritterCab skills cross-reference the JasperFx [`ai-skills`](https://github.com/jasperfx/ai-skills) library — a paid, proprietary collection of generic Critter Stack skills (Wolverine, Marten, Polecat). CritterCab's skills are deliberately designed to **defer to ai-skills for generic mechanics** and **document project-specific decisions on top.**

Where applicable, CritterCab skills name their ai-skills counterparts in the `External` section of `See Also`. Contributors with an ai-skills license install them at the user level so they're available alongside CritterCab's project-local skills:

```bash
# Install all ai-skills globally (license required)
npx skills add https://github.com/jasperfx/ai-skills/tree/v1.1.0/skills --skill '*' -g -a claude-code
```

CritterCab does not duplicate or paraphrase ai-skills content. The composition is layered, not extracted. Phase 5 of the skill plan was a dedicated reconciliation pass that closed 2026-05-06: cross-checked against ai-skills, eliminated duplication where it crept in, and produced a 53-entry upstream-contribution roadmap captured in the [skills-foundation-phase-5 retrospective](../retrospectives/skills-foundation-phase-5.md).

## Authoring new skills

Use `_template/SKILL.md` as the starting point:

```bash
cp -r docs/skills/_template docs/skills/<your-skill-name>
```

Then:

1. Update the frontmatter (`name`, `description`, `cluster`, `tags`).
2. Replace placeholder content with the actual skill body.
3. Wire `See Also` references with upstream/downstream/external links.
4. Update this README's cluster index, tag index, and entry-point hubs; if the new skill changes the topology meaningfully, update the cross-reference graph as well.

The template's inline comments document the conventions in detail (frontmatter fields, length guideline, section structure, cross-reference format).

## Conventions reference

- **Skill organization**: per [agentskills.io](https://agentskills.io/specification) — directory + `SKILL.md` + optional `references/`.
- **Length guideline**: aim for `SKILL.md` under 500 lines; pragmatic, not strict. Move conditionally-loaded deep-dive content to `references/`.
- **Domain examples**: ground in CritterCab's actual bounded contexts (Trips, Dispatch, Telemetry, Pricing, Identity, etc.) — not generic placeholders.
- **No back-references to CritterBids or CritterSupply**: these are sibling reference projects, not CritterCab's source of truth.
- **README update on each new skill addition**: this README is the navigation hub. Update the cluster index, tag index, entry-point hubs, and (if the new skill changes the topology meaningfully) the cross-reference graph in the same PR that adds the skill. The five-phase scaffolding is complete; subsequent skills land incrementally.
- **Skill-file gaps surfaced during sessions go in [`DEBT.md`](./DEBT.md)**: a retro that names a skill-file gap should add the corresponding row to `DEBT.md` in the same PR. Gaps are drained by dedicated `tidy: skills` PRs per the **Session and PR cadence** rule in [`docs/prompts/README.md`](../prompts/README.md#session-and-pr-cadence). Session-runner-blocking fixes are the exception and may ride in the surfacing session's PR.
