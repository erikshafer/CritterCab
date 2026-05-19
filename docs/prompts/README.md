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

### Scope: no opportunistic edits to other files

A session's edits stay within the files named in its prompt's deliverable plan. Edits to *other* files — even small clarifications, typo fixes, or related improvements that surface during the session — are out of bounds and become a new session's scope (or a new DEBT row if applicable).

**Same-file edits are in-bounds.** Correcting a factual error in a doc-history line of the same file as a session's primary deliverable is fine; correcting the same kind of typo in a different file is not. The rule is about *which files* the session touches, not *which lines*.

**Why:** opportunistic edits expand session scope unpredictably, dilute PR review, and prevent cleanly reverting individual changes. The retro is the right place to capture issues observed but not in the session's scope; new sessions or DEBT rows are the right place to fix them.

The rule originated as a refinement during the first skill-tidy session (PR #7) and was confirmed in practice by the PR #4 housekeeping session (PR #8) where the in-bounds same-file path enabled an index-section rename to ride alongside the new entry it was motivated by. For tidy-session-specific application, see [`docs/skills/DEBT.md`](../skills/DEBT.md) § Conventions.

### Spec delta cadence

Every prompt names its **spec delta**: what the canonical spec (the narrative or workshop the session is satisfying) will gain when the session ships. The spec delta is expressed in spec-shaped terms (new moment, new slice, new forward-constraint, amended GWT, new translation slice, new ADR cross-reference) — distinct from the process-shaped session intent the rest of the prompt captures.

The format is lightweight: 2–4 lines per prompt, named under a `## Spec delta` heading near the top of the prompt (after the metadata block, before the framing prose). Bulleted lines are fine; structured sub-schemas are not — the discipline lives in the naming, not in the formatting.

At session close, the retrospective confirms whether the planned delta landed (see [retrospectives README § Format conventions](../retrospectives/README.md#format-conventions-inside-a-retro-file)). The narrative or workshop the session satisfies records the amendment in its `## Document History` section (see [narratives README § Body structure](../narratives/README.md#body-structure)). Together the four steps — prompt's spec delta → session executes → retro confirms → spec's document-history records — close the loop opened by [ADR-003 spec-anchored development](../decisions/003-spec-anchored-development.md), which committed to keeping specs current but did not name *how* per-session deltas are tracked.

Pattern borrowed from OpenSpec's change-proposal payload; CritterCab does not adopt OpenSpec wholesale. The borrow is the discipline of capturing per-session spec amendments in spec-shaped terms, expressed inside the artifacts CritterCab already writes.

### Commit subjects: `tidy:` for maintenance sessions

Use `tidy: <area> — <details>` as the commit and PR subject for sessions whose deliverable is **maintenance of existing artifacts** rather than new ones. Established areas:

- `tidy: skills` — draining `DEBT.md` rows by amending skill files.
- `tidy: housekeeping` — README updates, index entries, handoff-note annotations.
- `tidy: encode-<rule>` — lifting an established refinement into a convention file (`docs/prompts/README.md`, `docs/skills/DEBT.md`, etc.).
- `tidy: skill-template` — additions or refinements to `docs/skills/_template/SKILL.md`.

The prefix signals review intent: a `tidy:` PR consolidates, clarifies, or fixes existing material; it does not introduce new architectural commitments. Sessions producing new workshops, narratives, ADRs, or implementation slices do **not** use `tidy:` — those carry their own subjects (the artifact name, slice number, or descriptive intent).

The convention emerged organically across PRs #7–#10. A new area joins the list when a second `tidy:` session in that area lands; one-off maintenance subjects are fine without joining the established list.

---

## Format conventions inside a prompt file

Each prompt file should include, at minimum:

- **Metadata block** at the top: status, target artifact (path or planned slug), date authored, optionally a one-line outcome once the session completes.
- **Framing** — one or two sentences explaining why this session exists in the project's arc.
- **Goal** — a single declarative sentence stating what the session produces.
- **Spec delta** — 2–4 lines named in spec-shaped terms, capturing what the canonical narrative or workshop will gain when the session ships. See [§ Session and PR cadence ‣ Spec delta cadence](#spec-delta-cadence).
- **Orientation files** — ordered list of files the session-runner should read before starting.
- **Working pattern** — interactive cadence, sign-off discipline, what gets committed when.
- **Deliverable plan** — what files the session should produce or modify.
- **Out of scope** — explicit list of things the session should not pull in opportunistically.

Subsequent sections are prompt-specific. Existing prompts in this directory serve as references for shape.

---

## Current contents

### Multi-artifact prompts (root)

- [`skills-tidy-marten-and-bootstrap.md`](./skills-tidy-marten-and-bootstrap.md) — First skill-tidy session. Drained the 7 open `DEBT.md` rows surfaced by PR #4: Marten 8.x / JasperFx namespace extractions in `marten-projections` and `marten-wolverine-aggregates`, plus `service-bootstrap` Wolverine HTTP and `TimeProvider` registration prerequisites. Status: complete (2026-05-08). Produced retro at [`retrospectives/skills-tidy-marten-and-bootstrap.md`](../retrospectives/skills-tidy-marten-and-bootstrap.md).
- [`housekeeping-pr4-followups.md`](./housekeeping-pr4-followups.md) — First housekeeping micro-PR after the skill-tidy session. Added the workshop §12.8 follow-ups index to `docs/workshops/README.md` and annotated the post-D→B→C handoff note as acted-on. Status: complete (2026-05-08). Produced retro at [`retrospectives/housekeeping-pr4-followups.md`](../retrospectives/housekeeping-pr4-followups.md).
- [`encode-tidy-methodology-refinements.md`](./encode-tidy-methodology-refinements.md) — Second housekeeping micro-PR. Encodes two methodology refinements from the skill-tidy retro into permanent rules: "no opportunistic edits to other files" lifted into `prompts/README.md` § Session and PR cadence; source-of-truth precedence (working code → retro → external docs) lifted into `docs/skills/DEBT.md` § Conventions. Status: complete (2026-05-08). Produced retro at [`retrospectives/encode-tidy-methodology-refinements.md`](../retrospectives/encode-tidy-methodology-refinements.md).
- [`skill-template-namespaces-pattern.md`](./skill-template-namespaces-pattern.md) — Third and final housekeeping micro-PR. Lifts the Namespaces cheat-sheet pattern (used in `marten-projections` and `marten-wolverine-aggregates` during PR #7) into an optional section in `docs/skills/_template/SKILL.md`. Future skill-authoring sessions can use the section directly; future tidy sessions adding namespace rows have a predictable structural home. Status: complete (2026-05-08). Produced retro at [`retrospectives/skill-template-namespaces-pattern.md`](../retrospectives/skill-template-namespaces-pattern.md).
- [`refresh-claude-md-and-encode-tidy-convention.md`](./refresh-claude-md-and-encode-tidy-convention.md) — Routing-layer refresh session. Fixed three drifts in `CLAUDE.md` (stale "no runnable code yet" status; Session Workflow underspecified relative to ADR-004's two-phase pipeline; Technology Stack table with dated Wolverine version and missing Aspire/PostgreSQL rows) and encoded the `tidy:` commit-subject convention used across PRs #7–#10 as a new subsection of `docs/prompts/README.md` § Session and PR cadence. Status: complete (2026-05-19). Produced retro at [`retrospectives/refresh-claude-md-and-encode-tidy-convention.md`](../retrospectives/refresh-claude-md-and-encode-tidy-convention.md).
- [`context-map-foundation.md`](./context-map-foundation.md) — Foundation context-map artifact authoring session. Rolled up cross-BC relationships from ADRs 006, 013, 014 and Workshops 001 and 002 into [`docs/context-map/README.md`](../context-map/README.md), naming each relationship in DDD strategic-design vocabulary (customer-supplier, conformist, anti-corruption layer, published language). Six edges authored — two locked (Dispatch ↔ Trips), one partly-locked (Identity), three deferred with explicit pending-workshop markers (Telemetry, Onboarding/Driver Profile/Rider Profile intra-actor topology, Trips outbound fan-out to seven downstream consumers). Bumped vision doc to v0.4 with cross-reference. ADR-016 ("Context Map as Living Artifact") deferred per session-start lean — trigger to revisit is W003 exercising the cadence rule. Closes a methodology commitment open since the vision doc's v0.1 (ADR-004 step #1 — Context Mapping). Status: complete (2026-05-19). Produced retro at [`retrospectives/context-map-foundation.md`](../retrospectives/context-map-foundation.md).

### Workshops

- [`workshops/001-dispatch-event-modeling.md`](./workshops/001-dispatch-event-modeling.md) — Captured retroactively after the session ran. Produced [`docs/workshops/001-dispatch-event-model.md`](../workshops/001-dispatch-event-model.md). Status: complete (2026-04-24).
- [`workshops/002-trips-event-modeling.md`](./workshops/002-trips-event-modeling.md) — Author CritterCab's second Event Modeling workshop covering the Trips bounded context. Returns to the design phase per ADR-004's design-return cadence rule; closes the "Trips workshop pending" entry from Workshop 001's §12.8 follow-ups index; creates the second canonical data point for ADR candidates #6 and #7. Status: authored (2026-05-08).

### Narratives

- [`narratives/001-rider-books-a-ride.md`](./narratives/001-rider-books-a-ride.md) — Author the first NDD-informed narrative covering the rider's happy-path journey through Dispatch. Status: complete (2026-04-25). Produced [`docs/narratives/001-rider-books-a-ride.md`](../narratives/001-rider-books-a-ride.md).
- [`narratives/002-driver-accepts-a-ride.md`](./narratives/002-driver-accepts-a-ride.md) — Author the driver-side companion narrative covering Dani's offer-receipt-through-acceptance journey. Pairs structurally with narrative 001. Status: complete (2026-05-04). Produced [`docs/narratives/002-driver-accepts-a-ride.md`](../narratives/002-driver-accepts-a-ride.md).

### Decisions

- [`decisions/001-protobuf-ride-assigned.md`](./decisions/001-protobuf-ride-assigned.md) — Author the three Dispatch business-event protobuf contracts (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`) and establish the `/protos/` directory. First exercise of ADR-009. Status: complete (2026-05-07). Produced proto files in `/protos/crittercab/dispatch/v1/` and `/protos/crittercab/common/v1/`.

### Implementations

- [`implementations/001-dispatch-service-skeleton.md`](./implementations/001-dispatch-service-skeleton.md) — Bootstrap the Dispatch service as a runnable but logic-free skeleton. First code in the repository. Status: complete (2026-05-07). Produced `src/CritterCab.Dispatch/`, `tests/CritterCab.Dispatch.Tests/`, `apphost.cs`.
- [`implementations/002-dispatch-slice-5-1-ride-requested.md`](./implementations/002-dispatch-slice-5-1-ride-requested.md) — First vertical slice: `RideRequested` command, aggregate, event, projections, HTTP endpoint, integration tests. Status: complete (2026-05-07).
