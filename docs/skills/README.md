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
| Phase 2 | First service implementation: composition root, store wiring, handlers, testing, observability | In progress (4 skills authored: `vertical-slice-organization`, `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers`) |
| Phase 3 | First cross-service flow: gRPC services, transports, identity ACL | Pending |
| Phase 4 | Complexity arrives: sagas, advanced patterns, polyglot, complete observability | Pending |
| Phase 5 | Reconciliation pass — cross-check against ai-skills, eliminate duplication, contribute generic patterns upstream | Pending |

The phase plan, including which skills land in each phase and why, is captured in the conversation history that produced this library. Each phase wraps with a README update like this one.

## Skill index by cluster

CritterCab's skill clusters split into product/library clusters and topic/concern clusters. The disambiguation rule when a skill spans both axes: pick the cluster that captures the primary value of the skill — the secondary axis goes in `tags`. See `_template/SKILL.md` for the full cluster vocabulary and disambiguation rule.

### Product/library clusters

| Cluster | Authored | Planned |
|---|---|---|
| `core` | `csharp-coding-standards`, `domain-event-conventions`, `event-modeling` | — |
| `wolverine` | `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers` | `wolverine-sagas` |
| `marten` | — | `marten-aggregates`, `marten-wolverine-aggregates`, `marten-projections`, `marten-querying`, `marten-async-daemon`, `dynamic-consistency-boundary` |
| `polecat` | — | `polecat-event-sourcing`, `polecat-document-store` |
| `aspire` | — | `aspire` |

### Topic/concern clusters

| Cluster | Authored | Planned |
|---|---|---|
| `grpc` | `protobuf-contracts` | `wolverine-grpc-handlers`, `wolverine-grpc-bidirectional-handlers`, `grpc-vs-other-transports` |
| `transports` | `transport-selection` | `wolverine-kafka`, `wolverine-azure-service-bus` |
| `distributed-services` | `adding-a-service` | `service-bootstrap`, `vertical-slice-organization`, `distributed-saga-considerations` |
| `identity` | — | `identity-acl` |
| `polyglot` | — | `polyglot-go-service` |
| `testing` | — | `testing-fundamentals`, `testing-integration`, `testing-advanced` |
| `observability` | — | `observability-tracing`, `observability-metrics` |
| `cli-tooling` | — | `cli-jasperfx`, `cli-aspire`, `cli-grpc-tooling`, `cli-kafka-tooling`, `cli-azure-messaging` |

## Entry-point hubs

When starting a task, the entry-point skill is the first to load. Upstream skills load if unfamiliar; downstream skills load as the work progresses.

| Task | Entry-point skill | Loads upstream | Loads downstream as work progresses |
|---|---|---|---|
| Designing a new feature or journey | `event-modeling` | — | `domain-event-conventions`, eventual implementation skills |
| Authoring or reviewing C# code | `csharp-coding-standards` | — | `domain-event-conventions`, plus the relevant Phase 2+ skill |
| Designing a domain event | `domain-event-conventions` | `csharp-coding-standards` | `marten-aggregates` (Phase 2), transport skills (Phase 3) |
| Designing a cross-service contract | `protobuf-contracts` | `csharp-coding-standards`, `domain-event-conventions` | `cli-grpc-tooling` (Phase 3), `wolverine-grpc-handlers` (Phase 3) |
| Choosing a transport for a cross-service flow | `transport-selection` | `protobuf-contracts`, `domain-event-conventions` | per-transport implementation skills (Phase 3) |
| Adding a new service from scratch | `adding-a-service` | `transport-selection`, `protobuf-contracts`, `domain-event-conventions` | `service-bootstrap`, `vertical-slice-organization`, store and observability skills (Phase 2) |

As Phase 2 lands, additional entry-point hubs will be added: `marten-aggregates` for event-sourced aggregate work, `wolverine-handlers` (with HTTP and messaging siblings) for handler-shape work, `testing-fundamentals` for any test work.

## Cross-reference graph (Phase 1)

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

The diagram shows direct upstream → downstream relationships among the Phase 1 skills. As phases land, the diagram extends — Phase 2 skills will form a second wave hanging off `domain-event-conventions`, `adding-a-service`, and the still-to-author `service-bootstrap`.

## Companion: JasperFx ai-skills

Several CritterCab skills cross-reference the JasperFx [`ai-skills`](https://github.com/jasperfx/ai-skills) library — a paid, proprietary collection of generic Critter Stack skills (Wolverine, Marten, Polecat). CritterCab's skills are deliberately designed to **defer to ai-skills for generic mechanics** and **document project-specific decisions on top.**

Where applicable, CritterCab skills name their ai-skills counterparts in the `External` section of `See Also`. Contributors with an ai-skills license install them at the user level so they're available alongside CritterCab's project-local skills:

```bash
# Install all ai-skills globally (license required)
npx skills add https://github.com/jasperfx/ai-skills/tree/v1.1.0/skills --skill '*' -g -a claude-code
```

CritterCab does not duplicate or paraphrase ai-skills content. The composition is layered, not extracted.

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
- **Domain examples**: ground in CritterCab's actual bounded contexts (Trips, Dispatch, Telemetry, etc.) — not generic placeholders.
- **No back-references to CritterBids or CritterSupply**: these are sibling reference projects, not CritterCab's source of truth.
- **README update at each phase boundary**: this README is the navigation hub. It's updated at the end of every phase to reflect newly-authored skills and any topology shifts.
