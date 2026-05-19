# Retrospective — Encode Spec-Delta Closure-Loop Discipline

| Field | Value |
|---|---|
| **Triggering prompt** | [`docs/prompts/encode-spec-delta-closure-loop.md`](../prompts/encode-spec-delta-closure-loop.md) |
| **Status** | Complete |
| **Date** | 2026-05-19 |
| **Output artifacts** | [`docs/prompts/README.md`](../prompts/README.md) (new `### Spec delta cadence` subsection + new `Spec delta` format-conventions bullet + index entry for this prompt); [`docs/retrospectives/README.md`](../retrospectives/README.md) (new `Spec delta — landed?` bullet + this retro's index entry); [`docs/narratives/README.md`](../narratives/README.md) (clarification paragraph on `## Document History` entries) |
| **One-line outcome** | Spec-delta closure-loop discipline is a CritterCab convention. The four-step loop (prompt's Spec delta → session executes → retro's Spec delta — landed? → spec's Document History) is encoded across the three operational READMEs; ADR-016 deferred until the discipline has been exercised 2–3 times. |

---

## Framing

This session executed Session A of the two follow-up sessions queued by the 2026-05-15 spec-delta + context-map handoff. Its job was lifting the spec-delta decision settled in the handoff session into the three READMEs that govern CritterCab's session workflow — prompts, retrospectives, narratives — so future sessions load the convention automatically rather than re-deriving it from a planning note that is destined for deletion.

Per the handoff doc's sequencing rationale, this session ran *after* Session B (context-map foundation, shipped as PR #17). That ordering was deliberate: this session's retro can itself exercise the new `Spec delta — landed?` discipline as its first real test — the self-referential opportunity named in the prompt's deliverable #7.

---

## Outcome summary

- **Three READMEs amended.** New `### Spec delta cadence` subsection in `docs/prompts/README.md` defining the four-step closure loop and the spec-shaped vs. process-shaped distinction; new minimum-required `**Spec delta**` bullet in the same README's `## Format conventions inside a prompt file` (paired with `Goal` as its spec-shaped sibling); new `**Spec delta — landed?**` bullet in `docs/retrospectives/README.md` `## Format conventions inside a retro file`; clarification paragraph in `docs/narratives/README.md` `### Body structure` on what `## Document History` entries should capture under spec-delta discipline.
- **ADR-016 (spec-delta-as-closure-loop-discipline) deferred** per session-start lean. Trigger to revisit: 2–3 sessions have exercised the discipline and the convention's lightweight intent has held (or required tightening).
- **Backfill of narratives 001/002 dropped from scope.** The handoff doc named this as a session deliverable; both narratives already had populated Document History entries (Session B independently confirmed this in its retro). The drop is forward-only — spec-delta starts with the next prompt authored after this session ships.
- **Index entries added** to `prompts/README.md` §Multi-artifact prompts (root) and `retrospectives/README.md` §Multi-artifact retros (root).

---

## What worked

- **Pre-authored leans on every amendment.** The prompt's §The convention's substantive content section pre-authored the wording for each of the three amendments along with named decisions to flag. That made each amendment's sign-off a focused yes/no/refine call rather than open-ended drafting — exactly the rhythm the [`encode-tidy-methodology-refinements.md`](./encode-tidy-methodology-refinements.md) precedent established and that the prompt itself cites as its structural twin.
- **Asymmetric amendment sizes matched the discipline's center of gravity.** prompts/README (heaviest, where the convention is *defined*) → retros/README (medium, where the convention is *confirmed*) → narratives/README (lightest, where the closure-loop's fourth step lands in an already-existing section). The size distribution correctly reflects the encoding's load: the convention's substantive prose lives at its definition site; the other two READMEs reference back.
- **Surfacing the stale-placement issue rather than blindly following the prompt's literal instruction.** The prompt (authored 2026-05-15) said "slot after `### Scope` (the current last subsection)" — but PR #16 added a `### Commit subjects` subsection between handoff-time and session-start, so `### Scope` was no longer the trailing subsection. Reading the file before applying caught the drift; surfacing both reasonable readings as a sign-off question preserved the prompt's spirit (slot after `### Scope` for conceptual sibling grouping with the other per-session-discipline subsections) without silently overriding its outdated literal placement note.
- **Driver-mode on non-durable placement decisions.** The format-conventions bullet's position in the minimum-required list (between `Goal` and `Orientation files`) was decided without a separate sign-off question — the spec-delta-pairs-with-Goal reasoning was non-substantive ergonomics, and the [[feedback_driver_mode_on_user_block]] memory authorizes driving these. The branch name (`encode-spec-delta-closure-loop`, matching the prompt slug) similarly went undriven.
- **Recursive enforcement of the no-opportunistic-edits rule.** This session touched three README files plus the prompts/README index entry (same-file, in-bounds) plus the new retro file plus the retros/README index entry (the retro's own home). No fourth file was touched. The same rule that the prior `encode-tidy` session encoded held cleanly for the session that encoded its successor.

---

## What was harder than expected

- **Missed the bundling instruction on commit #1.** The prompt's deliverable plan #6 said the prompts/README index entry should bundle with the cadence-subsection commit since both target the same file. I missed that and did the index entry as a separate commit (`512ddb8`) instead of folding it into `531b6af`. Recovery was clean (a new commit per CLAUDE.md's "prefer new commit over amend" guidance), but the bundling instruction was clearly there in the prompt and got overlooked because the bundling guidance lived in two places (deliverable plan #6 and working pattern §5) with slightly different framing — I anchored on the working-pattern phrasing that suggested per-amendment commits and missed the cross-reference back to deliverable #6. Lesson surfaced as a methodology refinement below.
- **Handoff-doc inaccuracy was real — and Session B already confirmed it.** The prompt's §Pre-work flagged the narratives-backfill deliverable as inaccurate; Session B's retro had independently confirmed both narratives have populated Document History sections (its §What was harder than expected #2). Loading both confirmations was redundant but not costly. Pattern worth carrying forward: when a follow-up session's prompt flags a planning-doc inaccuracy and the preceding session has already confirmed it, the second session's retro doesn't need to re-litigate — a one-line cross-reference to the prior retro's confirmation suffices, which is what this retro does.
- **Stale-placement risk in pre-authored prompts.** The prompt's "slot after `### Scope` (the current last subsection)" parenthetical was accurate at authoring time (2026-05-15) but stale by execution time (2026-05-19) because PR #16's refresh-claude-md session added `### Commit subjects` between. The session-runner reading the file at start catches it; the prompt-author writing literal-state parentheticals at pre-authoring time accumulates this stale-fact risk in proportion to the authoring-to-execution gap. Surfaced as a candidate refinement below.

---

## Methodology refinements that emerged

- **Bundling-instruction salience at commit-prep time.** When a deliverable plan instructs bundling and the target file matches a prior commit's, the session-runner should explicitly check the bundling instruction *at commit-prep time*, not just at session start. A self-prompt of "any bundling pending for this file?" before each commit would have caught the missed bundle on commit #1. Not new enough to warrant immediate rule encoding — this is the first missed-bundle observation, and the [encode-tidy retro's two-test cadence](./encode-tidy-methodology-refinements.md#what-worked) (observe once, exercise across sessions, encode if it holds twice) applies. Re-surfaces if it happens again.
- **Self-reference seam is clean for convention-encoding sessions.** This session's own prompt did NOT include a `## Spec delta` section because the convention did not exist at its authoring time. The retro's `Spec delta — landed?` section below names that absence retroactively without violating the no-edit-after-execution rule. The pattern matches the precedent set by [`encode-tidy-methodology-refinements.md`](./encode-tidy-methodology-refinements.md), whose prompt similarly predated the no-opportunistic-edits rule it introduced. Two clean precedents now stand; future convention-encoding sessions can cite both rather than re-justifying the seam each time.
- **Pre-authored prompts should prefer relative-state descriptions over snapshots.** The prompt's "the current last subsection" parenthetical drifted between authoring (2026-05-15) and execution (2026-05-19). Prompts authored well in advance of execution — especially handoff-doc-driven prompts that may sit pending for days — should phrase placement instructions relationally ("the relevant trailing per-session-discipline subsection") rather than as snapshots ("the current last subsection"). Captured as an observation for the next pre-authored prompt; not enforced.

---

## Outstanding items / next-session inputs

- **First forward exercise of the convention** lands in whichever session is authored next. Whether that is a Trips-narrative continuation, Identity workshop W003, or another tidy session, the next prompt authored must include a `## Spec delta` section per the new convention, and its retro must include the `Spec delta — landed?` line. The first three exercises serve as the evaluation period for ADR-016 (below).
- **ADR-016 (spec-delta-as-closure-loop-discipline) — deferred.** **Trigger to revisit:** 2–3 sessions have exercised the discipline and the lightweight intent has held (or required tightening). If after that exercise the convention reads as load-bearing project-wide discipline, the ADR is warranted; if it reads as workflow-internal mechanics, the ADR remains unnecessary. Note: this is the *second* ADR-016 deferral — Session B also deferred a candidate ADR-016 ("Context Map as Living Artifact"). When ADR-016 eventually lands, the slot belongs to whichever discipline's revisit-trigger fires first; the other becomes ADR-017.
- **May 15 handoff doc becomes disposable** per its own framing (*"Delete this file once both follow-up sessions [Session A and Session B] have shipped"*). With this PR's merge, both follow-up sessions will have shipped. The handoff doc's decisions now live durably in three READMEs (this session), `docs/context-map/README.md` and vision-doc v0.4 (Session B). A follow-up housekeeping micro-PR can delete `docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md`; no urgency, but the file's purpose is fully discharged.
- **Cross-references to add when ADR-016 lands** (if it does): bidirectional link with ADR-003 (since spec-delta is ADR-003's operational refinement); cross-reference from the cadence subsection in prompts/README to the ADR. Not added prospectively per the no-opportunistic-edits rule.

---

## Spec delta — landed?

**Landed, as planned.** The prompt's *implicit* spec delta (the prompt itself predates the convention and so could not name one in the convention's terms) was the three READMEs gaining the spec-delta closure-loop convention plus the four-step loop being named explicitly. All three amendments landed:

- `docs/prompts/README.md` gained `### Spec delta cadence` (defining the four-step loop) and the paired `**Spec delta**` minimum-required format-conventions bullet.
- `docs/retrospectives/README.md` gained the `**Spec delta — landed?**` minimum-required format-conventions bullet.
- `docs/narratives/README.md` gained the clarification paragraph on `## Document History` entries.

**This is the first retro in CritterCab's history to exercise the `Spec delta — landed?` line.** The exercise is unavoidably retrospective because the prompt could not include a `## Spec delta` section — the convention did not exist at the prompt's authoring time. Per the prompt's §Decisions to flag #2, this self-reference is fine and matches the precedent set by [`encode-tidy-methodology-refinements.md`](./encode-tidy-methodology-refinements.md)'s prompt-vs-rule timing. The first forward exercise — a prompt authored *after* this session ships, with a `## Spec delta` section, and a retro that confirms against it — lands with whichever session is next.

**Divergence from the planned delta:** none. ADR-016 was named as an optional artifact and explicitly deferred at session start; that defer is not a divergence — it is a settled decision recorded with a named revisit trigger.

**Spec amendments made to narratives or workshops in this session's PR:** none. Narratives 001 and 002 already had populated `## Document History` sections (handoff-doc inaccuracy; surfaced and dropped from scope). The narratives README itself gained the clarification on entry shape, but no narrative file was amended. The convention's discipline now reads forward-only — the *next* narrative or workshop amendment will be the first to exercise the closure-loop's fourth step on an actual canonical spec.

---

## Quantitative summary

| Metric | Count |
|---|---|
| READMEs amended | 3 (prompts, retrospectives, narratives) |
| New subsections | 1 (`### Spec delta cadence` in prompts/README) |
| New minimum-required bullets | 2 (Spec delta in prompts/README; Spec delta — landed? in retros/README) |
| New clarification paragraphs | 1 (Document History entry shape in narratives/README) |
| ADRs authored | 0 (ADR-016 deferred) |
| Commits before this retro | 4 (3 README amendments + 1 prompts/README index entry) |
| Net lines added across README amendments | ~14 (~11 in prompts/README, ~1 in retros/README, ~2 in narratives/README) |
| Missed-bundle observations | 1 (cadence subsection vs. index entry, both targeting prompts/README) |
| First-exercise self-references | 1 (this retro's `Spec delta — landed?` line) |
| Methodology refinements named for carry-forward | 3 (bundling-instruction salience, self-reference seam, relative-state vs. snapshot placement) |
