# CritterCab Skills

Implementation pattern documents for CritterCab. Each skill encodes hard-won conventions for a specific aspect of building, designing, or testing the project — so contributors and AI agents don't rediscover known solutions every session.

The skill library follows the [agentskills.io](https://agentskills.io/specification) open standard. Every skill lives in its own directory with a `SKILL.md` containing YAML frontmatter and Markdown instructions. Optional `references/` subdirectories hold deep-dive material loaded on demand.

## How to use this library

Skills are loaded into context by the Claude agent (or read manually by humans) when relevant to the current task. The frontmatter `description` field on each skill is loaded at agent startup to enable activation matching; the body is loaded when the skill is activated.

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
| Phase 2 | First service implementation: composition root, store wiring, handlers, projections, testing, local-dev orchestration | **Complete** (16 skills) |
| Phase 3 | First cross-service flow: gRPC services, Kafka and ASB transports, identity ACL, distributed observability | Pending |
| Phase 4 | Complexity arrives: sagas, advanced patterns, Polecat event sourcing, polyglot Go service, complete observability, advanced testing | Pending |
| Phase 5 | Reconciliation pass — cross-check against ai-skills, eliminate duplication, contribute generic patterns upstream | Pending |

22 skills authored across Phases 1 and 2. The phase plan, including which skills land in each phase and why, is captured in the conversation history that produced this library. Each phase wraps with a README update like this one.

## Skill index by cluster

CritterCab's skill clusters split into product/library clusters and topic/concern clusters. The disambiguation rule when a skill spans both axes: pick the cluster that captures the primary value of the skill — the secondary axis goes in `tags`. See `_template/SKILL.md` for the full cluster vocabulary and disambiguation rule.

### Product/library clusters

| Cluster | Authored | Planned |
|---|---|---|
| `core` | `csharp-coding-standards`, `domain-event-conventions`, `event-modeling` | — |
| `wolverine` | `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers` | `wolverine-grpc-handlers`, `wolverine-grpc-bidirectional-handlers`, `wolverine-kafka`, `wolverine-azure-service-bus`, `wolverine-sagas` |
| `marten` | `marten-aggregates`, `marten-wolverine-aggregates`, `marten-projections`, `marten-querying`, `marten-async-daemon`, `dynamic-consistency-boundary` | — |
| `polecat` | — | `polecat-event-sourcing`, `polecat-document-store` |
| `infrastructure` | `aspire`, `cli-aspire`, `cli-jasperfx` | `cli-grpc-tooling`, `cli-kafka-tooling`, `cli-azure-messaging` |

### Topic/concern clusters

| Cluster | Authored | Planned |
|---|---|---|
| `distributed-services` | `adding-a-service`, `service-bootstrap`, `vertical-slice-organization` | `distributed-saga-considerations` |
| `grpc` | `protobuf-contracts` | `grpc-vs-other-transports` |
| `transports` | `transport-selection` | — |
| `identity` | — | `identity-acl` |
| `polyglot` | — | `polyglot-go-service` |
| `testing` | `testing-fundamentals`, `testing-integration` | `testing-advanced` |
| `observability` | — | `observability-tracing`, `observability-metrics` |

## Entry-point hubs

When starting a task, the entry-point skill is the first to load. Upstream skills load if unfamiliar; downstream skills load as the work progresses.

### Design and contract tasks

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Designing a new feature or journey | `event-modeling` | — | `domain-event-conventions`, eventual implementation skills |
| Authoring or reviewing C# code | `csharp-coding-standards` | — | `domain-event-conventions`, plus the relevant implementation skill |
| Designing a domain event | `domain-event-conventions` | `csharp-coding-standards` | `marten-aggregates`, transport skills (Phase 3) |
| Designing a cross-service contract | `protobuf-contracts` | `csharp-coding-standards`, `domain-event-conventions` | `cli-grpc-tooling` (Phase 3), `wolverine-grpc-handlers` (Phase 3) |
| Choosing a transport for a cross-service flow | `transport-selection` | `protobuf-contracts`, `domain-event-conventions` | per-transport implementation skills (Phase 3) |

### Service implementation

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Adding a new service from scratch | `adding-a-service` | `transport-selection`, `protobuf-contracts`, `domain-event-conventions` | `service-bootstrap`, `vertical-slice-organization` |
| Bootstrapping a service's `Program.cs` | `service-bootstrap` | `csharp-coding-standards`, `adding-a-service` | `wolverine-handlers`, `marten-aggregates`, `aspire` |
| Organizing code within a service | `vertical-slice-organization` | `service-bootstrap` | handler and aggregate skills |

### Wolverine handler authoring

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Authoring a Wolverine handler (general) | `wolverine-handlers` | `service-bootstrap`, `vertical-slice-organization` | `wolverine-http-handlers`, `wolverine-messaging-handlers`, `marten-wolverine-aggregates`, `testing-fundamentals` |
| Authoring an HTTP endpoint | `wolverine-http-handlers` | `wolverine-handlers` | `marten-wolverine-aggregates`, `testing-integration` |
| Authoring a messaging handler | `wolverine-messaging-handlers` | `wolverine-handlers` | `marten-wolverine-aggregates`, `testing-integration` |

### Marten event-sourced and document work

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Implementing an event-sourced aggregate | `marten-aggregates` | `domain-event-conventions`, `service-bootstrap` | `marten-wolverine-aggregates`, `marten-projections` |
| Wiring an aggregate to a Wolverine handler | `marten-wolverine-aggregates` | `marten-aggregates`, `wolverine-handlers` | `dynamic-consistency-boundary`, `testing-integration` |
| Implementing a write path that spans multiple streams | `dynamic-consistency-boundary` | `marten-wolverine-aggregates`, `marten-projections` | `testing-integration` |
| Building a projection (live or async) | `marten-projections` | `marten-aggregates`, `domain-event-conventions` | `marten-async-daemon`, `marten-querying` |
| Querying projected read models | `marten-querying` | `marten-projections` | `wolverine-http-handlers` |
| Configuring the async daemon | `marten-async-daemon` | `marten-projections`, `service-bootstrap` | `testing-integration`, `cli-jasperfx` |

### Testing

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Writing a unit test for a handler or aggregate | `testing-fundamentals` | `wolverine-handlers` (or `marten-aggregates`) | `testing-integration` when the test grows beyond pure logic |
| Writing an integration test | `testing-integration` | `testing-fundamentals`, `service-bootstrap`, `marten-async-daemon` | `wolverine-grpc-handlers` and other Phase 3 skills |

### Infrastructure and tooling

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Setting up local-dev orchestration | `aspire` | `service-bootstrap` | `cli-aspire` |
| Operating the AppHost from a terminal or CI | `cli-aspire` | `aspire` | `cli-jasperfx` for service-internal commands |
| Running CLI commands inside a Cab service | `cli-jasperfx` | `service-bootstrap` | `cli-aspire` for AppHost-level orchestration |

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

The Phase 2 graph shows direct upstream → downstream relationships among the 16 Phase 2 skills, with two Phase 1 anchors (`adding-a-service`, `domain-event-conventions`) included to show where the cluster attaches. As Phase 3 lands, a third graph will show the cross-service flow skills (gRPC, Kafka, ASB, identity ACL) hanging off `wolverine-messaging-handlers`, `protobuf-contracts`, and `transport-selection`.

## Companion: JasperFx ai-skills

Several CritterCab skills cross-reference the JasperFx [`ai-skills`](https://github.com/jasperfx/ai-skills) library — a paid, proprietary collection of generic Critter Stack skills (Wolverine, Marten, Polecat). CritterCab's skills are deliberately designed to **defer to ai-skills for generic mechanics** and **document project-specific decisions on top.**

Where applicable, CritterCab skills name their ai-skills counterparts in the `External` section of `See Also`. Contributors with an ai-skills license install them at the user level so they're available alongside CritterCab's project-local skills:

```bash
# Install all ai-skills globally (license required)
npx skills add https://github.com/jasperfx/ai-skills/tree/v1.1.0/skills --skill '*' -g -a claude-code
```

CritterCab does not duplicate or paraphrase ai-skills content. The composition is layered, not extracted. Phase 5 of the skill plan is a dedicated reconciliation pass: cross-check against ai-skills, eliminate any duplication that crept in, and contribute generic patterns upstream where appropriate.

## Authoring new skills

Use `_template/SKILL.md` as the starting point:

```bash
cp -r docs/skills/_template docs/skills/<your-skill-name>
```

Then:

1. Update the frontmatter (`name`, `description`, `cluster`, `tags`).
2. Replace placeholder content with the actual skill body.
3. Wire `See Also` references with upstream/downstream/external links.
4. Update this README's cluster index and (if the new skill changes the topology meaningfully) the cross-reference graph.

The template's inline comments document the conventions in detail (frontmatter fields, length guideline, section structure, cross-reference format).

## Conventions reference

- **Skill organization**: per [agentskills.io](https://agentskills.io/specification) — directory + `SKILL.md` + optional `references/`.
- **Length guideline**: aim for `SKILL.md` under 500 lines; pragmatic, not strict. Move conditionally-loaded deep-dive content to `references/`.
- **Domain examples**: ground in CritterCab's actual bounded contexts (Trips, Dispatch, Telemetry, Pricing, Identity, etc.) — not generic placeholders.
- **No back-references to CritterBids or CritterSupply**: these are sibling reference projects, not CritterCab's source of truth.
- **README update at each phase boundary**: this README is the navigation hub. It's updated at the end of every phase to reflect newly-authored skills and any topology shifts.
