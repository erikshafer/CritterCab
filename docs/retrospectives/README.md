# CritterCab Retrospectives Index

Retrospectives are the **session-close artifacts** in CritterCab's session-driven workflow. Each retrospective records what a working session actually produced, what worked, what was harder than expected, what methodology refinements emerged, and what the next session inherits. The retro closes the loop opened by the corresponding prompt in [`docs/prompts/`](../prompts/).

This directory is part of the **`narrative → prompt → execute → retrospective`** loop documented in [`CLAUDE.md`](../../CLAUDE.md). Per CLAUDE.md, *"a session prompt and its retro share a slug so they sort together. The retro is part of the session's deliverable PR, not a follow-up."*

---

## Subdirectory layout

Retrospectives mirror the [`docs/prompts/`](../prompts/) subdirectory taxonomy: per-artifact retros live in subdirectories matching the kind of artifact the session produced; cross-cutting and phase-level retros live at the root.

| Location | Houses |
|---|---|
| `skills/` | Retros from sessions that produced a single skill file in `docs/skills/`. |
| `narratives/` | Retros from sessions that produced a single narrative in `docs/narratives/`. |
| `workshops/` | Retros from sessions that produced a single workshop artifact in `docs/workshops/`. |
| `decisions/` | Retros from sessions that produced a single ADR in `docs/decisions/`. |
| `implementations/` | Retros from code-implementation sessions targeting one or more slices. |
| (root) | Cross-cutting / phase-level retros covering sessions that produced multiple artifacts of mixed types. |

Subdirectories appear as their first retro lands; an empty subdirectory is not pre-created — the same convention as `docs/prompts/`.

The phase-level skill-library retros (e.g., `skills-foundation-phase-4.md`) sit at the root because the sessions that produced them spanned **many** skill files plus README updates plus the next phase's prompt. They are not "per-skill retros." A future per-skill session — for example, a one-off session to author a single new skill after the foundation plan closes — would produce a retro in `skills/`.

---

## Naming convention

- **Slug match with the triggering prompt.** The retrospective's filename slug matches the prompt's slug minus any role-suffix (e.g., `-handoff`). Phase 4's prompt is `prompts/skills-foundation-phase-4-handoff.md`; its retro is `retrospectives/skills-foundation-phase-4.md`. Both files share the topic stem `skills-foundation-phase-4` and sort adjacently when both directories are listed.
- **Three-digit numeric prefix per subdirectory.** Retros inside `skills/`, `narratives/`, etc. inherit the same numeric-prefix scheme as their prompts: `prompts/skills/001-some-skill.md` → `retrospectives/skills/001-some-skill.md`. Counters do not cross subdirectory boundaries.
- **Root-level retros use the prompt's topic stem directly** without a numeric prefix, matching the corresponding prompt's filename pattern. The prompts at the root use descriptive multi-word filenames (e.g., `skills-foundation-phase-{N}-handoff.md`); root-level retros do the same minus the role-suffix.
- **No status suffix on the filename.** A retrospective is by definition a record of a completed session. Its mere existence indicates the session ran. Status is tracked inside the retro's metadata block.

---

## Cross-references

Every retro names its triggering prompt in the metadata block at the top — typically a relative link to `../prompts/...`. The triggering prompt does NOT need to be edited to point back at the retro; per the `docs/prompts/README.md` convention, *"do not edit a prompt after the session it triggered has run — the prompt is a historical record of intent at session start, not a living document."* The retro is the forward link; the prompt remains as-it-was-written.

When a retro identifies follow-up work (open items, methodology gaps, unresolved questions), those items typically become inputs to the **next** session's prompt — captured in that prompt's "starting state" or "inputs from prior session" section. Retros do not directly trigger new sessions; they inform the next prompt.

---

## When to write a retrospective

Write the retrospective at session close, before the session's deliverable PR is opened. Per CLAUDE.md: *"the retro is part of the session's deliverable PR, not a follow-up."* The retro and the session's other artifacts (skills, narratives, code, etc.) ship together.

If a session ended without a retro — possibly because it was abandoned mid-flight, or because the session-runner deferred — write the retro as soon as the session-runner re-engages, before any new work begins. A retro authored after-the-fact is fine; a retro that never lands leaves a gap in the project record.

---

## Format conventions inside a retro file

Retro files are working documents, not essays. Each retro includes, at minimum:

- **Metadata block** at the top: triggering prompt (path), status (Complete / Partially complete / Abandoned), date authored, output artifacts (files produced or modified), one-line outcome summary.
- **Framing** — one or two sentences explaining what the session was for and how it fit the project's arc.
- **Outcome summary** — concise list or table of what the session produced.
- **What worked** — methodology elements, tools, conventions, or decisions that paid off. Specific, not vague.
- **What was harder than expected** — challenges encountered with the lesson each surfaced. Honest, not defensive.
- **Methodology refinements that emerged** — process changes the next session-runner should adopt. These are the durable lessons; capture them explicitly.
- **Outstanding items / next-session inputs** — explicit list of things the next session inherits.
- **Quantitative summary** *(optional, for larger sessions)* — counts, sizes, durations, or other measurable outcomes worth tracking.

Subsequent sections are session-specific. Existing retros in this directory serve as references for shape.

---

## Current contents

### Multi-artifact retros (root)

- [`skills-foundation-phase-4.md`](./skills-foundation-phase-4.md) — Phase 4 of the skill-library foundation plan: 9 new skills (sagas, Polecat event sourcing and document store, polyglot Go service, bidirectional gRPC, advanced testing, metrics observability, transport comparison, distributed saga considerations) plus the README close-out and the Phase 5 handoff prompt. Triggered by [`prompts/skills-foundation-phase-4-handoff.md`](../prompts/skills-foundation-phase-4-handoff.md). Status: complete (2026-05-06).
- [`skills-tidy-marten-and-bootstrap.md`](./skills-tidy-marten-and-bootstrap.md) — First skill-tidy session. Drained the 7 open `DEBT.md` rows surfaced by PR #4: Marten 8.x / JasperFx namespace extractions in `marten-projections` and `marten-wolverine-aggregates`; `service-bootstrap` Wolverine HTTP and `TimeProvider` registration prerequisites; plus one prose-pass (Oakton → JasperFx residual). Triggered by [`prompts/skills-tidy-marten-and-bootstrap.md`](../prompts/skills-tidy-marten-and-bootstrap.md). Status: complete (2026-05-08).
- [`housekeeping-pr4-followups.md`](./housekeeping-pr4-followups.md) — First housekeeping micro-PR after the skill-tidy session. Added the workshop §12.8 follow-ups index to `docs/workshops/README.md` and annotated the post-D→B→C handoff note as acted-on; renamed the root-level subsection in this README and in prompts/README to `Multi-artifact` for future-proofing. Triggered by [`prompts/housekeeping-pr4-followups.md`](../prompts/housekeeping-pr4-followups.md). Status: complete (2026-05-08).
- [`encode-tidy-methodology-refinements.md`](./encode-tidy-methodology-refinements.md) — Second housekeeping micro-PR. Encoded skill-tidy retro methodology refinements 1+2 as permanent rules: "no opportunistic edits to other files" lifted into `docs/prompts/README.md` § Session and PR cadence (new `### Scope` subsection); source-of-truth precedence (working code → retro → external docs) and a tidy-specific restatement of no-opportunistic-edits added to `docs/skills/DEBT.md` § Conventions with cross-reference. Triggered by [`prompts/encode-tidy-methodology-refinements.md`](../prompts/encode-tidy-methodology-refinements.md). Status: complete (2026-05-08).
- [`skill-template-namespaces-pattern.md`](./skill-template-namespaces-pattern.md) — Third and final housekeeping micro-PR. Lifted the Namespaces cheat-sheet pattern (used in `marten-projections` and `marten-wolverine-aggregates` during PR #7) into an optional section in `docs/skills/_template/SKILL.md`. First non-encoding session to test the no-opportunistic-edits rule from PR #9 — passed. Surfaced a methodology observation about prompt + index commit bundling breaking when the user pre-commits the prompt; observation captured for potential encoding after a third instance. Triggered by [`prompts/skill-template-namespaces-pattern.md`](../prompts/skill-template-namespaces-pattern.md). Status: complete (2026-05-08).
- [`refresh-claude-md-and-encode-tidy-convention.md`](./refresh-claude-md-and-encode-tidy-convention.md) — Routing-layer refresh session triggered by a user audit question ("would a fresh AI on a different harness understand our pipeline from CLAUDE.md alone?"). Fixed three drifts in `CLAUDE.md` (stale project status, pipeline underspecified relative to ADR-004's two-phase structure, Technology Stack table) and encoded the `tidy:` commit-subject convention used across PRs #7–#10. Surfaced one observation worth tracking: routing-layer files decay differently than artifact files — periodic audits against a "fresh-AI" bar may warrant a cadence rule if the pattern recurs. Triggered by [`prompts/refresh-claude-md-and-encode-tidy-convention.md`](../prompts/refresh-claude-md-and-encode-tidy-convention.md). Status: complete (2026-05-19).

Phase 1–3 retrospectives were not authored at the time those phases ran (the retrospective convention solidified during Phase 4). They may be reconstructed from the working transcripts and skill artifacts if needed; otherwise they remain a known gap in the project record.

### Per-skill retros (`skills/`)

*(none yet — will appear when the first single-skill authoring session lands.)*

### Per-decision retros (`decisions/`)

- [`001-protobuf-ride-assigned.md`](./decisions/001-protobuf-ride-assigned.md) — First proto-authoring session. Established `/protos/` directory layout and buf configuration. Four proto files authored per ADR-009. Triggered by [`prompts/decisions/001-protobuf-ride-assigned.md`](../prompts/decisions/001-protobuf-ride-assigned.md). Status: complete (2026-05-07).
- [`002-bundled-pattern-adrs.md`](./decisions/002-bundled-pattern-adrs.md) — Bundled ADR authorship session lifting four Workshop 001 §11 candidates plus one new Workshop 002 §12 candidate to authored ADRs (011–015). Four pattern ADRs with two-BC evidence (config-as-events bootstrap, aggregate-per-invariant, shared cross-BC identifier, ASB topic naming) plus one cross-cutting NFR (driver-app projection timing budget). Triggered by [`prompts/decisions/002-bundled-pattern-adrs.md`](../prompts/decisions/002-bundled-pattern-adrs.md). Status: complete (2026-05-10).

### Per-implementation retros (`implementations/`)

- [`001-dispatch-service-skeleton.md`](./implementations/001-dispatch-service-skeleton.md) — Bootstrap Dispatch service skeleton: composition root, health checks, Alba smoke test, Aspire AppHost. First runnable code in the repository. Triggered by [`prompts/implementations/001-dispatch-service-skeleton.md`](../prompts/implementations/001-dispatch-service-skeleton.md). Status: complete (2026-05-07).
- [`002-dispatch-slice-5-1-ride-requested.md`](./implementations/002-dispatch-slice-5-1-ride-requested.md) — First vertical slice: `RideRequested` command, aggregate, event, two projections, three Alba integration tests. Triggered by [`prompts/implementations/002-dispatch-slice-5-1-ride-requested.md`](../prompts/implementations/002-dispatch-slice-5-1-ride-requested.md). Status: complete (2026-05-07).

### Per-narrative, per-workshop retros

*(none yet — corresponding subdirectories will appear with their first retro.)*
