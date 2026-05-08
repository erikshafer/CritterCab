# CritterCab Prompts Index

Prompts are the **session-trigger artifacts** in CritterCab's session-driven workflow. Each prompt captures the goal, context, working pattern, and deliverables for a single working session — workshop, narrative authoring, ADR drafting, skill-file authoring, or implementation. The session produces a corresponding artifact in `docs/workshops/`, `docs/narratives/`, `docs/decisions/`, `docs/skills/`, or in code, plus a retrospective.

This directory is part of the **`narrative → prompt → execute → retrospective`** loop documented in [`CLAUDE.md`](../../CLAUDE.md). The prompt is the durable, version-controlled record of intent for a session; the retrospective closes the loop after the session completes.

---

## Subdirectory layout

Prompts are organized by the **kind of artifact** they trigger, mirroring the structure of `docs/`:

| Subdirectory | Triggers a session that produces |
|---|---|
| [`workshops/`](./workshops/) | An Event Modeling or Domain Storytelling workshop artifact in `docs/workshops/`. |
| [`narratives/`](./narratives/) | An NDD-informed narrative in `docs/narratives/`. |
| [`skills/`](./skills/) | A component-scoped skill file in `docs/skills/`. |
| [`decisions/`](./decisions/) | An ADR in `docs/decisions/`. |
| [`implementations/`](./implementations/) | A code-implementation session targeting one or more slices from a workshop / narrative pair. |

Subdirectories appear as their first prompt lands; an empty subdirectory is not pre-created.

---

## Naming convention

- **Three-digit numeric prefix per subdirectory.** Each subdirectory has its own `001-...`, `002-...` series. Counters do not cross subdirectory boundaries.
- **Slug matches the target artifact's slug** when the prompt targets a single artifact. The prompt at `prompts/narratives/001-rider-books-a-ride.md` produces `narratives/001-rider-books-a-ride.md` — same slug, same number, symmetric naming.
- **Descriptive slug for slice-targeted or multi-artifact prompts.** Implementation prompts that target a single slice can include the slice number, e.g., `prompts/implementations/001-rider-submits-request-slice-5-1.md`.
- **No status suffix on the filename.** A completed prompt stays in place. Status is recorded in the prompt's metadata block and confirmed by the existence of the corresponding artifact + retrospective.

---

## Cross-references

Each prompt cross-references its target artifact (and vice versa). The artifact's "Document History" or session-log section names the prompt that drove the session; the prompt's metadata block names the artifact it produced.

When a session re-runs (rare — typically only when the original deliverable was abandoned and re-authored), the new prompt gets the next numeric prefix in its subdirectory rather than overwriting the original. The prompt-history is itself part of the project's record.

---

## When to create a new prompt vs. extend an existing one

Create a new prompt for any new session, including follow-ups. Do not edit a prompt after the session it triggered has run — the prompt is a historical record of intent at session start, not a living document. If a session's scope expands mid-flight, capture the expansion in that session's retrospective and (if warranted) author a follow-up prompt for the additional work.

---

## Session and PR cadence

A prompt corresponds to one session, and a session corresponds to one PR. The PR contains the prompt's deliverables plus its retrospective. This keeps PR scope predictable, makes review tractable, and preserves the prompt → artifact → retro audit trail.

### One prompt, one PR

The PR for a session contains exactly the artifacts produced by that session's prompt plus its retrospective. Do not absorb other sessions' prompts into the same PR.

**Two named exceptions:**

- **Skeleton + first slice.** When a service skeleton is bootstrapped, the first slice may share its PR — a defensible reading of the "blueprint architecture" step (hand-build a representative slice manually before turning the per-slice loop loose). Beyond the first slice, slices are one-per-PR.
- **Session-runner-blocking skill fixes.** A skill-file fix that the current session's session-runner *had* to make to complete its work rides in that session's PR. Larger skill-file rewrites — including gaps merely *surfaced* by the session — go in a dedicated `tidy: skills` PR. Surfaced-but-not-fixed gaps are registered in [`docs/skills/DEBT.md`](../skills/DEBT.md).

If a session's scope expands mid-flight beyond what the prompt named, capture the expansion in the retrospective and (if warranted) author a follow-up prompt for the additional work — do not retroactively re-scope the prompt or fold unrelated work into the PR.

### Design-return cadence

[ADR-004](../decisions/004-design-phase-workflow-sequence.md) frames the workflow as two phases: a one-time pre-code design phase (Context Map → Domain Storytelling → Event Modeling) and a per-slice implementation loop (Narrative → Prompt → Implementation+Retro). The per-slice loop is permitted to fan out across many sessions, but it should not run indefinitely without revisiting the design phase.

**Working rule:** after every 2–3 implementation PRs against a single bounded context, the next PR is one of:

- A new narrative for that BC (extending journey coverage to un-narrativized slices),
- The next bounded context's workshop,
- A skill-tidy or design-tidy PR that drains accumulated debt.

A fourth consecutive implementation PR against the same BC without a design-or-tidy interleave is a signal to pause and ask whether the design has drifted. The retrospective can override this rule when implementation pressure clearly warrants — but the override should be explicit, not silent.

---

## Format conventions inside a prompt file

Each prompt file should include, at minimum:

- **Metadata block** at the top: status, target artifact (path or planned slug), date authored, optionally a one-line outcome once the session completes.
- **Framing** — one or two sentences explaining why this session exists in the project's arc.
- **Goal** — a single declarative sentence stating what the session produces.
- **Orientation files** — ordered list of files the session-runner should read before starting.
- **Working pattern** — interactive cadence, sign-off discipline, what gets committed when.
- **Deliverable plan** — what files the session should produce or modify.
- **Out of scope** — explicit list of things the session should not pull in opportunistically.

Subsequent sections are prompt-specific. Existing prompts in this directory serve as references for shape.

---

## Current contents

### Skill-library tidy and phase-level prompts (root)

- [`skills-tidy-marten-and-bootstrap.md`](./skills-tidy-marten-and-bootstrap.md) — First skill-tidy session. Drained the 7 open `DEBT.md` rows surfaced by PR #4: Marten 8.x / JasperFx namespace extractions in `marten-projections` and `marten-wolverine-aggregates`, plus `service-bootstrap` Wolverine HTTP and `TimeProvider` registration prerequisites. Status: complete (2026-05-08). Produced retro at [`retrospectives/skills-tidy-marten-and-bootstrap.md`](../retrospectives/skills-tidy-marten-and-bootstrap.md).

### Workshops

- [`workshops/001-dispatch-event-modeling.md`](./workshops/001-dispatch-event-modeling.md) — Captured retroactively after the session ran. Produced [`docs/workshops/001-dispatch-event-model.md`](../workshops/001-dispatch-event-model.md). Status: complete (2026-04-24).

### Narratives

- [`narratives/001-rider-books-a-ride.md`](./narratives/001-rider-books-a-ride.md) — Author the first NDD-informed narrative covering the rider's happy-path journey through Dispatch. Status: complete (2026-04-25). Produced [`docs/narratives/001-rider-books-a-ride.md`](../narratives/001-rider-books-a-ride.md).
- [`narratives/002-driver-accepts-a-ride.md`](./narratives/002-driver-accepts-a-ride.md) — Author the driver-side companion narrative covering Dani's offer-receipt-through-acceptance journey. Pairs structurally with narrative 001. Status: complete (2026-05-04). Produced [`docs/narratives/002-driver-accepts-a-ride.md`](../narratives/002-driver-accepts-a-ride.md).

### Decisions

- [`decisions/001-protobuf-ride-assigned.md`](./decisions/001-protobuf-ride-assigned.md) — Author the three Dispatch business-event protobuf contracts (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`) and establish the `/protos/` directory. First exercise of ADR-009. Status: complete (2026-05-07). Produced proto files in `/protos/crittercab/dispatch/v1/` and `/protos/crittercab/common/v1/`.

### Implementations

- [`implementations/001-dispatch-service-skeleton.md`](./implementations/001-dispatch-service-skeleton.md) — Bootstrap the Dispatch service as a runnable but logic-free skeleton. First code in the repository. Status: complete (2026-05-07). Produced `src/CritterCab.Dispatch/`, `tests/CritterCab.Dispatch.Tests/`, `apphost.cs`.
- [`implementations/002-dispatch-slice-5-1-ride-requested.md`](./implementations/002-dispatch-slice-5-1-ride-requested.md) — First vertical slice: `RideRequested` command, aggregate, event, projections, HTTP endpoint, integration tests. Status: complete (2026-05-07).
