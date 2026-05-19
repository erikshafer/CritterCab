# Prompt — Housekeeping: Delete May 15 Spec-Delta + Context-Map Handoff Doc

| Field | Value |
|---|---|
| **Status** | Pending (authored 2026-05-19; awaiting review before execution) |
| **Authored** | 2026-05-19 |
| **Target artifacts** | `docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md` (delete); `docs/prompts/housekeeping-delete-may-15-handoff.md` (this prompt); `docs/prompts/README.md` (index entry); `docs/retrospectives/housekeeping-delete-may-15-handoff.md` (retro); `docs/retrospectives/README.md` (index entry) |
| **Source-of-truth dependencies** | [`docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md`](../planning/2026-05-15-spec-delta-and-context-map-handoff.md) (the file being deleted, including its own "Disposable" framing); [`docs/planning/README.md`](../planning/README.md) §Conventions (the directory's own lifecycle rule authorizing deletion); [`docs/retrospectives/encode-spec-delta-closure-loop.md`](../retrospectives/encode-spec-delta-closure-loop.md) §Outstanding items (where this micro-PR was named as a follow-up); [`docs/retrospectives/context-map-foundation.md`](../retrospectives/context-map-foundation.md) (confirms Session B's decisions also landed durably) |
| **Workflow position** | Housekeeping micro-PR following PR #17 (Session B — context-map foundation) and PR #18 (Session A — spec-delta closure-loop discipline). **First session in CritterCab's history authored *after* the spec-delta convention exists**, and therefore the first prompt to include a `## Spec delta` section. |

---

## Spec delta

- **No canonical spec is amended.** This session removes a disposable planning artifact whose decisions live durably elsewhere (PR #17: `docs/context-map/README.md` and vision-doc v0.4; PR #18: three READMEs — prompts, retrospectives, narratives).
- **The closure-loop's fourth step does not apply here.** No narrative or workshop's `## Document History` is updated, because no canonical spec is touched.
- **The discipline still exercises forward.** This prompt names "no spec delta" explicitly rather than omitting the section — proving the convention can describe housekeeping sessions cleanly without forcing confabulation.

---

## Framing — why this session exists

The May 15 spec-delta + context-map handoff doc was authored to carry decisions across a machine swap during the session that queued two follow-up sessions. Both follow-up sessions — Session B (PR #17, context-map foundation) and Session A (PR #18, spec-delta closure-loop discipline) — have now shipped. The handoff doc's own framing names this exact endpoint: *"Disposable: Delete this file once both follow-up sessions (Session A — spec-delta methodology, Session B — context-map artifact) have shipped."* That condition is met.

[`docs/planning/README.md`](../planning/README.md) §Conventions reinforces the lifecycle: *"Disposable by design. Once a note has been acted on, feel free to move, fold into another artifact, or delete it."* The deletion is exactly what the directory invites — not an opportunistic cleanup, but the directory's intended lifecycle endpoint.

Session A's retro §Outstanding items and Session B's retro §Outstanding items both named this housekeeping micro-PR as the natural follow-up. This session closes that loop.

---

## Goal

Delete `docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md`. Author the prompt and retro per the new spec-delta-aware convention so this housekeeping session becomes the first forward exercise of the closure-loop discipline.

---

## Orientation files

1. **[`docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md`](../planning/2026-05-15-spec-delta-and-context-map-handoff.md)** — the file being deleted. Confirm its "Disposable" framing and verify the two follow-up sessions it queued have shipped (PR #17 + PR #18).
2. **[`docs/planning/README.md`](../planning/README.md)** — the directory's own lifecycle convention (the rule that authorizes the deletion). Confirms no index amendment is needed because `docs/planning/` does not maintain a per-file index.
3. **[`docs/retrospectives/encode-spec-delta-closure-loop.md`](../retrospectives/encode-spec-delta-closure-loop.md) §Outstanding items** — where this housekeeping item was named.
4. **[`docs/prompts/README.md`](./README.md) § Spec delta cadence** — the new convention this prompt and its retro exercise for the first time forward.

---

## Working pattern

Single-pass session. No interactive per-decision sign-off needed beyond the prompt-itself sign-off — the deletion is mechanical, the file's "Disposable" framing pre-authorizes it, and the directory's `README.md` §Conventions explicitly invites it. Per the `tidy: housekeeping` commit-subject convention from [`docs/prompts/README.md`](./README.md#commit-subjects-tidy-for-maintenance-sessions), commits use that prefix.

The Session A retro's bundling-instruction-salience refinement (missed-bundle observation) applies here: when the deliverable plan instructs bundling and the target file matches a prior commit's, the session-runner explicitly checks the bundling instruction at commit-prep time.

---

## Deliverable plan

1. **Delete `docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md`.** Single `git rm` plus its own commit with subject `tidy: housekeeping — delete May 15 spec-delta + context-map handoff doc`.
2. **Add this prompt (`docs/prompts/housekeeping-delete-may-15-handoff.md`) bundled with its `prompts/README.md` index entry** under `Multi-artifact prompts (root)`. One commit per the [skill-tidy retro lesson](../retrospectives/skills-tidy-marten-and-bootstrap.md) and the [Session A retro's bundling-salience refinement](../retrospectives/encode-spec-delta-closure-loop.md#methodology-refinements-that-emerged).
3. **Author retro (`docs/retrospectives/housekeeping-delete-may-15-handoff.md`) bundled with its `retrospectives/README.md` index entry.** One commit. The retro's `Spec delta — landed?` line is the first *forward* exercise of the convention; it confirms the prompt-named "no spec delta" landed as the named-none.

### Definition of done

- May 15 handoff doc removed from the working tree.
- Prompt + prompts/README index entry bundled into one commit.
- Retro + retros/README index entry bundled into one commit.
- Bundled PR opened. No Claude attribution on commits or PR per the established convention.

---

## Out of scope

- **Deletion of the other two `docs/planning/` files** (`2026-05-07-orientation-and-next-steps.md` and `2026-05-07-post-d-b-c-handoff.md`). Both may or may not be disposable today; assessing each is a separate session's scope. The no-opportunistic-edits rule applies.
- **Any structural change to `docs/planning/README.md`.** Its current convention already authorizes this deletion; no amendment is needed.
- **Backporting `## Spec delta` to prior prompts.** Per Session A's retro §Methodology refinements ("self-reference seam is clean for convention-encoding sessions") and the prompts/README directive *"do not edit a prompt after the session it triggered has run"*, prior prompts are not retroactively amended.
- **ADR-016 reconsideration.** Both Session A and Session B deferred a candidate ADR-016 with named triggers. Neither trigger has fired (the housekeeping deletion does not count as an exercise of the spec-delta discipline in the way the trigger means it — "session that authored against the discipline and tested whether the lightweight intent held"). ADR-016 stays deferred.
