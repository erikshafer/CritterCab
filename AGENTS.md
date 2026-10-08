# CritterCab — Agent Guide

CritterCab is an open-source ride-sharing reference architecture on the Critter Stack (Wolverine, Marten, Polecat, Alba). It is a set of separately deployable .NET services, one per bounded context, that talk only over gRPC calls and Wolverine messages, with the transport for each flow chosen by the flow's shape.

This file says what the repository is and where things live. It asserts nothing about progress: what is built is read from `src/`, `tests/` and the running AppHost; what is intended is in [`docs/vision/README.md`](./docs/vision/README.md).

---

## Where things live

| Path | What it holds |
|---|---|
| `src/CritterCab.AppHost/` | Aspire AppHost project: the containers, the services, and their pinned local ports. `dotnet run --project src/CritterCab.AppHost`. |
| `src/CritterCab.<Service>/` | One deployable service per bounded context, organized in vertical-slice feature folders. |
| `tests/CritterCab.<Service>.Tests/` | One test project per service: Alba integration tests over Testcontainers, test classes named by slice. |
| `protos/` | Protobuf contracts (ADR-009), linted by `buf` in CI. Each consuming service compiles the `.proto` it needs; no shared assembly. |
| `Directory.Packages.props` | Every package version (Central Package Management). A pin exists only if a project references it. |
| `CritterCab.slnx` | The solution. CI restores, builds and tests exactly what it lists, and fails if a `.csproj` on disk is missing from it. |
| `docs/vision/` | What CritterCab is for, the bounded contexts it will build and in what order, the stack, the methodology. The source of truth for direction. |
| `docs/decisions/` | ADRs. Read the index preface: ADRs 001 to 019 are the v1 record and bind as written until the first PR touching their subject re-affirms, amends or supersedes them. |
| `docs/rules/` | Structural constraints encoded for implementation sessions. |
| `docs/skills/` | Implementation conventions, each grounded in code in this repository; code-less skills live in `docs/skills/archive/`. Gaps go to `docs/skills/DEBT.md`. |
| `docs/workshops/` | Minutes of Event Modeling and Domain Storytelling sessions (the minutes rule is in the index). |
| `docs/context-map/` | Relationships between bounded contexts, in DDD strategic-design vocabulary. |
| `docs/prompts/`, `docs/retrospectives/` | One prompt and one retrospective per working session, sharing a slug. |
| `docs/narratives/`, `docs/research/`, `docs/planning/` | Optional journey write-ups; background reading and the methodology log; disposable session handoffs. |

The Event Model's authored surface will be a curated `*.emodel.yaml` per service beside its `Program.cs`; none exists yet. Until one does, slice reasoning is cited from the workshop minutes.

---

## How work is done

1. **Before implementing**, load the skills in `docs/skills/` that govern the area, the rules in `docs/rules/`, and the workshop section (or declared file) the slice comes from.
2. **One prompt, one session, one PR.** The prompt names its deliverables, its spec delta and what is out of scope; the retrospective rides inside the PR. Cadence rules, the named exceptions, the no-opportunistic-edits rule and the `tidy:` commit-subject convention are in [`docs/prompts/README.md`](./docs/prompts/README.md#session-and-pr-cadence); the retro template is in [`docs/retrospectives/README.md`](./docs/retrospectives/README.md).
3. **Verify library claims against source** before wiring, and log the checked-out version. Run the skill-auditor before and after code and a two-axis code review before the PR.
4. **New chapters of the model** are drawn on the EventModelers.AI canvas, exported and committed; the markdown minutes go to `docs/workshops/` under the existing numbering.

---

## Architectural non-negotiables

- **Services are deployed separately per bounded context** (or a small group). Each service owns its data store; services never reference each other's internals.
- **Cross-service communication is gRPC calls or Wolverine messages, nothing else.** No shared databases, no shared application-layer code, no project reference from one service to another, no direct handler-to-handler calls.
- **Transport is chosen per flow type, not defaulted.** High-volume telemetry goes to Kafka; business events go to Azure Service Bus; service-to-service calls and streaming go to gRPC. RabbitMQ is never a domain transport (it is admitted only as CritterWatch's backplane, ADR-017).
- **The identity provider is swappable.** An Identity service is the anti-corruption layer between the provider and domain events; no other service couples to provider-specific types.
- **Protobuf contracts are first-class artifacts**, reviewed and evolved with the care given to API contracts.
- **Nothing is admitted ahead of code.** No package is pinned, no resource wired in the AppHost, no skill written and no ADR called committed unless code in this repository exercises it under CI in the same PR or the next.

AI-optimized encodings of these are in [`docs/rules/structural-constraints.md`](./docs/rules/structural-constraints.md); their rationale is in the vision and the ADRs.

---

## Stack

C# on .NET; Wolverine for handlers, HTTP, gRPC and transports; Marten on PostgreSQL for event sourcing and documents; Polecat on SQL Server when a service needs it; Alba, xUnit and Testcontainers for tests; Aspire for local orchestration; Kafka and Azure Service Bus through Wolverine; H3 for geospatial indexing. Exact versions are whatever `Directory.Packages.props` and the AppHost project pin.

Context7 library IDs for capabilities the skills do not cover: Wolverine `/jasperfx/wolverine`, Marten `/jasperfx/marten`, Polecat `/jasperfx/polecat`, Alba `/jasperfx/alba`.

---

## Companion: JasperFx ai-skills

CritterCab's [`docs/skills/`](./docs/skills/) defer to JasperFx's [`ai-skills`](https://github.com/jasperfx/ai-skills), a paid collection of generic Critter Stack skills, for library mechanics. Where a CritterCab skill names an ai-skills counterpart, the ai-skill is authoritative for mechanics and the CritterCab skill for project conventions; where they conflict on project ground, CritterCab wins. Install instructions: [`docs/skills/README.md`](./docs/skills/README.md#companion-jasperfx-ai-skills).

---

## Vendored external skills

A curated subset of [Matt Pocock's skills](https://github.com/mattpocock/skills) is vendored under `.agents/skills/` and tracked by [`skills-lock.json`](./skills-lock.json) (upstream source and content hash per entry). Do not hand-edit the vendored files; steer them with the precedence table below.

| Skill | Trigger | Purpose |
|---|---|---|
| `grill-me` | "grill me" | Relentless decision-tree interrogation of a plan |
| `grill-with-docs` | "grill me against the docs", design-phase planning | `grill-me` cross-referenced against existing domain language and ADRs |
| `improve-codebase-architecture` | "find refactoring opportunities", "deepen modules" | Deep-modules lens on existing code |
| `tdd` | "use TDD", "red-green-refactor" | Vertical-slice TDD rhythm |
| `zoom-out` | "zoom out" | Map the modules and callers in an unfamiliar area |

Where a vendored skill conflicts with a CritterCab convention, the CritterCab convention wins:

| Concern | Vendored default | CritterCab override |
|---|---|---|
| ADR format | `grill-with-docs/ADR-FORMAT.md` | The format already used in [`docs/decisions/`](./docs/decisions/) |
| Domain language storage | A root `CONTEXT.md`, created lazily | No root `CONTEXT.md`. Domain language lives in the workshop minutes and, once authored, the declared model files; each bounded context owns its language and enums, translated at the boundary |
| Skill authoring template | `write-a-skill/SKILL.md` | [`docs/skills/_template/SKILL.md`](./docs/skills/_template/SKILL.md) |
| Architectural vocabulary | module / interface / seam / adapter | Layer it on CritterCab's own (service, bounded context, transport, handler, projection, aggregate); use both |
| Files created mid-grill | `CONTEXT.md` and `docs/adr/` created lazily | No new top-level files mid-session. ADRs go in `docs/decisions/` and need explicit owner sign-off |

If an override is unclear, ask the owner rather than following the vendored default.

---

## Do not

- Commit directly to `main`; branch and open a PR.
- Share a database across services, or reference one service project from another.
- Pin a package, wire an AppHost resource or write a skill for something no code exercises.
- Assert status (built, done, in progress) in a routing file; it decays.
- Put code in prompt documents.
