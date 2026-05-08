# Prompt — PR #4 Housekeeping: Workshop §12.8 Follow-ups Index and Handoff Note Annotation

| Field | Value |
|---|---|
| **Status** | Pending (sketched 2026-05-08; awaiting review before execution) |
| **Authored** | 2026-05-08 |
| **Target artifacts** | `docs/workshops/README.md` (new "Workshop follow-ups" section), `docs/planning/2026-05-07-post-d-b-c-handoff.md` (annotated as acted-on), `docs/prompts/housekeeping-pr4-followups.md` (this prompt), `docs/prompts/README.md` (index update), `docs/retrospectives/housekeeping-pr4-followups.md` (new retro), `docs/retrospectives/README.md` (index update) |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../workshops/001-dispatch-event-model.md) §12.8 (the named follow-ups); [`docs/planning/2026-05-07-post-d-b-c-handoff.md`](../planning/2026-05-07-post-d-b-c-handoff.md); [`docs/skills/DEBT.md`](../skills/DEBT.md) Recently drained (state of the skill-tidy follow-up); [`docs/planning/README.md`](../planning/README.md) ("disposable by design" convention) |
| **Workflow position** | First housekeeping micro-PR after the skill-tidy session. Closes PR #4-era loose ends. Establishes the workshop-followups-index pattern; tests "annotate in place" as a lighter alternative to archiving for planning notes. |

---

## Framing — why this session exists

The skill-tidy retrospective (PR #7) flagged two PR #4-era loose ends as deferrable but worth landing before the Trips workshop kicks off:

1. **Workshops/README §12.8 follow-ups index** — proposed during the skill-tidy prompt-sketching review. Aggregates the named follow-ups across workshops (currently just Workshop 001's §12.8) so they don't drift between sessions.
2. **Post-D→B→C handoff note annotation** — the note (`docs/planning/2026-05-07-post-d-b-c-handoff.md`) is now substantially acted-upon: skill-tidy is done (PR #7); the A vs E lean is acted upon as A (Trips workshop, deferred to its own session). Per the planning/README "disposable by design" convention, the note should be annotated as acted-on or archived.

These pair naturally because both are "PR #4-era housekeeping" — items that surfaced during the protobuf + skeleton + slice 5.1 work but didn't fit any subsequent session's primary scope.

---

## Goal

Land both housekeeping items in one micro-PR. Add a "Workshop follow-ups" aggregation section to `docs/workshops/README.md` that surfaces the named follow-ups so they don't get lost between sessions. Annotate `docs/planning/2026-05-07-post-d-b-c-handoff.md` with an "Acted on" outcome line, keeping the note in place (not archiving) so its content remains visible until a subsequent handoff supersedes it.

---

## Orientation files (read in order)

1. **[`docs/workshops/README.md`](../workshops/README.md)** — current structure (just Workshops list and Conventions). The new section slots between them.
2. **[`docs/workshops/001-dispatch-event-model.md`](../workshops/001-dispatch-event-model.md) §12.8** — the source content for the index's first entry: protobuf authorship (done, PR #4); Trips BC workshop (pending); ADR candidates and parking-lot items (live with the workshop).
3. **[`docs/planning/2026-05-07-post-d-b-c-handoff.md`](../planning/2026-05-07-post-d-b-c-handoff.md)** — the note to annotate.
4. **[`docs/planning/README.md`](../planning/README.md)** — "disposable by design"; "feel free to move, fold into another artifact, or delete it" once acted-upon.

---

## Working pattern

- **One PR for both deliverables.** PR title: `tidy: housekeeping — workshop §12.8 follow-ups index and PR #4 handoff annotation`.
- **Bundle the prompt + prompts/README index update into one commit** — explicit lesson from the skill-tidy session retro (where prompt and index ended up split into two commits because the prompt was committed before index work began).
- **Annotate the handoff note in place; don't archive.** The note's framing content (artifacts inventory, A vs E decision-point write-up) is still useful reference until the next handoff supersedes it. An `**Acted on:**` line near the top is the lightest-touch close-out.
- **§12.8 index format follows the version sketched during the skill-tidy prompt review:** compact, named-action-items only; ADR candidates and parking-lot items get a deferral pointer rather than enumeration.
- **No opportunistic edits to other files.** This applies the "no opportunistic edits to *other files*" refinement that the skill-tidy retro proposed (and that PR-B will encode into permanent rules).

---

## Deliverable plan

1. **`docs/workshops/README.md`** — new `## Workshop follow-ups` section between `## Workshops` and `## Conventions`. Contains:
   - Brief intro paragraph framing what the index is and what it aggregates.
   - Status legend (`pending` / `done` / `superseded`).
   - One subsection per workshop (currently just Workshop 001) with a 2-row table of named follow-ups.
   - Closing deferral note: ADR candidates and parking-lot items live with their workshop, are not enumerated.

2. **`docs/planning/2026-05-07-post-d-b-c-handoff.md`** — add an `**Acted on:**` line near the top of the file (after the title and `> Purpose:` line, before the first `---`) summarizing the outcome:
   - Skill-tidy done via PR #7 (2026-05-08).
   - A-vs-E decision: lean A (Trips workshop) selected; deferred to its own session.
   - Note remains in place per planning/README "disposable by design" convention; will be archived or superseded by the next handoff.

3. **`docs/prompts/housekeeping-pr4-followups.md`** (this prompt) — committed alongside the prompts/README index entry.

4. **`docs/prompts/README.md`** — add a new entry under "Skill-library tidy and phase-level prompts (root)" — wait, this isn't skill-library. **Open question:** does the root-level subsection need renaming to be more general? Lean: rename to "Tidy and housekeeping prompts (root)" or similar so it accommodates both. Decide during execution; if renaming, that's the only structural change.

5. **`docs/retrospectives/housekeeping-pr4-followups.md`** — retro per the format conventions in `docs/retrospectives/README.md`. Notably shorter than the skill-tidy retro (less methodology to capture; this is a small two-deliverable session). Should still hit metadata, framing, outcome, what worked, what was harder, methodology refinements (if any), outstanding items.

6. **`docs/retrospectives/README.md`** — add retro entry under the same root-level subsection (matching whatever rename happens in deliverable #4).

---

## Decisions to flag during the session

1. **Rename "Skill-library tidy and phase-level prompts (root)"** to a more general label like "Tidy and housekeeping prompts (root)" or "Cross-cutting prompts (root)" so it accommodates both skill-tidy and housekeeping work. Lean: yes, rename now while there are only two entries (the skill-tidy and this one); cheaper to rename now than later when more accumulate. Same change in retros/README. Mechanical.

2. **In-place annotation vs archive for the handoff note.** Lean: in-place annotation — the note's content is still useful reference. Archive (move to `docs/planning/archive/`) is the alternative if the planning README's "disposable by design" suggests stronger discardability. Either is fine; flag and decide during the session.

3. **Whether the §12.8 index should also note Workshop 001's "5 memory items captured during session"** as a follow-up row. Lean: no — those memory items are personal-memory artifacts, already folded into Erik's persistent memory at session close. Not the same shape as "named action item" the index is meant to track. Mention in the deferral note if needed.

---

## Out of scope

- **Trips workshop authoring.** Not a tidy/micro-PR; major design session deferred to its own scope.
- **Methodology refinements 1+2 from the skill-tidy retro.** Separate housekeeping PR (PR-B): encoding "no opportunistic edits to *other files*" + source-of-truth precedence into DEBT.md and prompts/README cadence section.
- **Namespaces pattern in skill template.** Separate housekeeping PR (PR-C): adding optional "Namespaces" section to `docs/skills/_template/SKILL.md`.
- **Phase 6 placeholder cleanup; ai-skills lean-out.** Long-term out-of-scope per `docs/skills/DEBT.md`'s "Out of scope" carve-out.
- **Modifying Workshop 001's §12.8 itself.** The index aggregates §12.8 content; it does not edit it. The §12.8 section in workshop 001 stays as the source of truth.
- **Forward-looking: backporting bidirectional cross-references from each ADR candidate / parking-lot item to the new index.** Out of scope; the deferral note covers them.

---

## Retro emphasis

Lighter than the skill-tidy retro. Capture:

- Whether bundling the prompt + index update into one commit (the explicit lesson from the prior retro) actually held under execution.
- Whether the "annotate in place" pattern for the handoff note feels right, or whether moving to `docs/planning/archive/` would have been better.
- Whether the rename of the prompts/README and retros/README root-level subsection (if executed) reads cleanly post-rename.
- Whether the workshop-followups-index format is something we want to lock in as a pattern, or whether we should iterate after a second workshop is added.
