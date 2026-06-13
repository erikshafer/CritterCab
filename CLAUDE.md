# CritterCab — AI Development Guidelines

CritterCab is an open-source ride-sharing reference architecture built on the Critter Stack, showcasing Wolverine's gRPC feature set alongside event-driven messaging, event sourcing, and multi-transport messaging. It is structured as a set of **separately deployable services**, one per bounded context (or small group thereof), communicating exclusively via gRPC or Wolverine messages.

The Dispatch service has its first vertical slice (`RideRequested`) running end-to-end with Marten event sourcing, Wolverine.HTTP, and Alba integration tests. All other bounded contexts remain pre-workshop; the project is otherwise still in the design phase.

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

CritterCab's session-driven workflow follows the two-phase structure committed in [ADR-004](./docs/decisions/004-design-phase-workflow-sequence.md). The sequence is not a waterfall — retrospectives are the feedback mechanism that loops findings back into upstream artifacts.

**Pre-code design phase** (one-time per bounded context, in order):

1. **Context Mapping** — name cross-BC relationships in DDD strategic-design vocabulary (partnership, customer-supplier, conformist, shared-kernel, published-language, anti-corruption-layer, separate-ways, open-host-service). Artifact: `docs/context-map/` *(directory pending; foundation prompt authored at [`docs/prompts/context-map-foundation.md`](./docs/prompts/context-map-foundation.md))*.
2. **Domain Storytelling** — surface language boundaries between contexts before populating an event model. Complementary technique; not yet exercised in CritterCab.
3. **Event Modeling workshop** — multi-session workshop producing events, commands, views, swim lanes, slices, GWT scenarios. Artifact: `docs/workshops/`.

**Per-slice implementation loop** (iterates, with retro-driven feedback into upstream artifacts):

4. **Narrative** — NDD-informed journey-scoped spec threading multiple workshop slices into one user's coherent experience. Artifact: `docs/narratives/`.
5. **Prompt** — task-scoped build order referencing narrative(s) and skill files. Artifact: `docs/prompts/`.
6. **Execute + Retrospective** — implementation produces code or another design artifact; retrospective closes the session. Artifact: `docs/retrospectives/` plus the session's deliverables.

A session prompt and its retro share a slug so they sort together; the retro is part of the session's deliverable PR, not a follow-up. One prompt = one session = one PR — see [`docs/prompts/README.md`](./docs/prompts/README.md#session-and-pr-cadence) for the cadence rules, the two named exceptions, the design-return interleave that prevents implementation runs from drifting away from design, the no-opportunistic-edits scope rule, and the `tidy:` commit-subject convention for maintenance sessions.

Per-layer operational manuals:

- [`docs/workshops/README.md`](./docs/workshops/README.md) — workshop conventions and the cross-workshop follow-ups index.
- [`docs/narratives/README.md`](./docs/narratives/README.md) — narrative format (frontmatter schema, Moment body structure, two-layer fidelity).
- [`docs/prompts/README.md`](./docs/prompts/README.md) — session prompt template and Session-and-PR-cadence rules.
- [`docs/retrospectives/README.md`](./docs/retrospectives/README.md) — retrospective format and per-artifact taxonomy.

A **spec-delta** closure-loop discipline (borrowed in pattern from OpenSpec, not as a framework) is in flight: every prompt will name what the canonical spec gains when the session ships; every retro confirms what landed; the narrative's document history records the amendment. The encoding session is pending at [`docs/prompts/encode-spec-delta-closure-loop.md`](./docs/prompts/encode-spec-delta-closure-loop.md); convention details land in the per-layer READMEs when that session ships.

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

## Technology Stack

The Critter Stack is CritterCab's committed foundational technology (ADR-010). Key facts for implementation sessions:

| Concern | Tool |
|---|---|
| Language | C# 14 / .NET 10+ |
| Messaging, HTTP, gRPC, handlers, transports | Wolverine — 5.32+ floor (the gRPC release that motivated this project); currently on 5.39+ |
| Event sourcing and document store (PostgreSQL 18) | Marten 8.35+ |
| Document store (SQL Server) | Polecat 3.1+ |
| Database schema management | Weasel (implicit via Marten/Polecat) |
| Integration test host | Alba |
| Local-dev orchestration | Aspire 13.4.3 |
| High-volume telemetry transport | Kafka (via Wolverine's Kafka transport) |
| Business-event transport | Azure Service Bus |
| Polyglot service | Go (first non-.NET service, participates over gRPC) |

---

## Context7

For Wolverine, Marten, Polecat, and Alba capabilities beyond what skill files already cover:

- Wolverine: `/jasperfx/wolverine`
- Marten: `/jasperfx/marten`
- Polecat: `/jasperfx/polecat`
- Alba: `/jasperfx/alba`

---

## Companion: JasperFx ai-skills

CritterCab's [`docs/skills/`](./docs/skills/) library defers to JasperFx's [`ai-skills`](https://github.com/jasperfx/ai-skills) — a paid, proprietary collection of generic Critter Stack skills — for library mechanics. The bespoke skills under `docs/skills/` cover project-specific decisions and idioms layered on top; they do not duplicate ai-skills content.

When a CritterCab skill's `See Also → External` section names an ai-skills counterpart, treat the ai-skill as authoritative for library mechanics and the CritterCab skill as authoritative for project conventions. Where they conflict on project ground, CritterCab wins.

Contributors working with an ai-skills license install the skills globally; the install command and layering rationale are documented at [`docs/skills/README.md`](./docs/skills/README.md#companion-jasperfx-ai-skills).

---

## External Skills

CritterCab vendors a curated subset of [Matt Pocock's skills](https://github.com/mattpocock/skills) under `.agents/skills/`, tracked via [`skills-lock.json`](./skills-lock.json) which records the upstream source and content hash for each entry. These cover session-invoked behaviors not encoded in [`docs/skills/`](./docs/skills/). Do not hand-edit the vendored files — they will desync from the lockfile. Steer them via the precedence table below instead.

| Skill | Trigger | Purpose |
|---|---|---|
| `grill-me` | "grill me" / `/grill-me` | Relentless decision-tree interrogation of a plan |
| `grill-with-docs` | "grill me against the docs" / design-phase planning | `grill-me` with cross-referencing against existing domain language and ADRs |
| `improve-codebase-architecture` | "find refactoring opportunities", "deepen modules" | Hickey/Ousterhout deep-modules lens; most useful once implementation code exists |
| `tdd` | "use TDD", "red-green-refactor" | Vertical-slice TDD rhythm — one test, one impl, repeat |
| `zoom-out` | "zoom out" | Map the modules and callers in an unfamiliar area |

### Precedence overrides

Where a vendored skill conflicts with a CritterCab convention, the CritterCab convention wins. Apply these without prompting:

| Concern | External skill default | CritterCab override |
|---|---|---|
| ADR format | `grill-with-docs/ADR-FORMAT.md` | [`docs/decisions/`](./docs/decisions/) — use the format already established there |
| Domain language storage | Single root `CONTEXT.md`, created lazily | No root `CONTEXT.md`. Domain language lives in [`docs/workshops/`](./docs/workshops/) and [`docs/narratives/`](./docs/narratives/); cross-BC terms translate at boundaries (BCs own their own enums and language) |
| Skill authoring template | `write-a-skill/SKILL.md` (not installed in this repo) | [`docs/skills/_template/SKILL.md`](./docs/skills/_template/SKILL.md) |
| Architectural vocabulary | `improve-codebase-architecture/LANGUAGE.md` (module / interface / seam / adapter) | Layer the deep-modules vocabulary on top of CritterCab's own (service / bounded context / transport / handler / projection / aggregate) — use both, do not substitute one for the other |
| Inline file creation during grilling | Skill creates `CONTEXT.md` and `docs/adr/` lazily | Do not create new top-level files mid-session. ADRs go in [`docs/decisions/`](./docs/decisions/) and require explicit user sign-off (the three-criteria gate still applies) |

If a precedence override is unclear, ask the user before acting — do not silently follow the vendored default.

---

## Do Not

- Commit directly to `main` — branch and PR
- Share a database across services
- Add a service project reference to another service project
- Put code in prompt documents
