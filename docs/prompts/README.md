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

### Workshops

- [`workshops/001-dispatch-event-modeling.md`](./workshops/001-dispatch-event-modeling.md) — Captured retroactively after the session ran. Produced [`docs/workshops/001-dispatch-event-model.md`](../workshops/001-dispatch-event-model.md). Status: complete (2026-04-24).

### Narratives

- [`narratives/001-rider-books-a-ride.md`](./narratives/001-rider-books-a-ride.md) — Author the first NDD-informed narrative covering the rider's happy-path journey through Dispatch. Status: complete (2026-04-25). Produced [`docs/narratives/001-rider-books-a-ride.md`](../narratives/001-rider-books-a-ride.md).
- [`narratives/002-driver-accepts-a-ride.md`](./narratives/002-driver-accepts-a-ride.md) — Author the driver-side companion narrative covering Dani's offer-receipt-through-acceptance journey. Pairs structurally with narrative 001. Status: complete (2026-05-04). Produced [`docs/narratives/002-driver-accepts-a-ride.md`](../narratives/002-driver-accepts-a-ride.md).

### Decisions

- [`decisions/001-protobuf-ride-assigned.md`](./decisions/001-protobuf-ride-assigned.md) — Author the three Dispatch business-event protobuf contracts (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`) and establish the `/protos/` directory. First exercise of ADR-009. Status: complete (2026-05-07). Produced proto files in `/protos/crittercab/dispatch/v1/` and `/protos/crittercab/common/v1/`.
