# Prompt — Encode Spec-Delta Closure-Loop Discipline

| Field | Value |
|---|---|
| **Status** | Pending (authored 2026-05-15; awaiting review before execution) |
| **Authored** | 2026-05-15 |
| **Target artifacts** | `docs/prompts/README.md` (new format-convention requirement for a `Spec delta` section in prompts; possibly a new `### Spec delta cadence` subsection under `## Session and PR cadence`); `docs/retrospectives/README.md` (new format-convention requirement for a `Spec delta — landed?` outcome confirmation in retros); `docs/narratives/README.md` (clarification on how the existing `## Document History` section should read under spec-delta discipline — *no backfill of narratives 001/002*); `docs/decisions/0NN-spec-delta-as-closure-loop-discipline.md` *(optional — session-start judgment call; ADR number depends on whether Session B's ADR-016 landed)*; `docs/decisions/README.md` (index update, only if the ADR lands); `docs/prompts/README.md` (this prompt's index entry, bundled with the prompt commit per the [skill-tidy retro lesson](../retrospectives/skills-tidy-marten-and-bootstrap.md)); `docs/retrospectives/encode-spec-delta-closure-loop.md` (retro); `docs/retrospectives/README.md` (index entry) |
| **Source-of-truth dependencies** | `docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md` §Decisions made this session #2 (the spec-delta decision); [`docs/decisions/003-spec-anchored-development.md`](../decisions/003-spec-anchored-development.md) (the existing SDD commitment that spec-delta *refines*, not replaces); [`docs/decisions/004-design-phase-workflow-sequence.md`](../decisions/004-design-phase-workflow-sequence.md) (the workflow-sequence commitment that spec-delta plugs into); [`docs/prompts/README.md`](./README.md) and [`docs/retrospectives/README.md`](../retrospectives/README.md) (the two READMEs being amended); [`docs/narratives/README.md`](../narratives/README.md) (the third README touched, lightly); OpenSpec project README (referenced for borrowed-pattern lineage only; *not* adopted as a framework) |
| **Workflow position** | Second of the two follow-up sessions named in the 2026-05-15 handoff doc. Sequenced after Session B (context-map foundation) so the spec-delta convention can be tested retroactively against Session B's closed retro as its first real exercise. This is the last methodology-refinement session before the project either resumes implementation work or runs its next workshop (Identity is the strongest near-term candidate). |

---

## Framing — why this session exists

Prior CritterX showcases (CritterBids, CritterSupply) carried stale areas of the project without clear reflection on what changed and why. The **spec delta** discipline — borrowed in pattern, not in framework, from OpenSpec's closure-loop affordance — is a structural defense against that failure mode. It is the user's explicit motivation, not academic methodology completeness.

The handoff doc settles the *what*: every prompt names what the canonical spec (narrative or workshop) will gain when the session ships, in spec-shaped terms (distinct from the process-shaped session intent the rest of the prompt already captures); every retro confirms whether that planned delta landed, names any divergence, and updates the narrative's document-history accordingly. This session settles the *where* (which README sections get the convention) and the *how* (what the convention's lightweight format actually looks like).

The closure loop becomes: **prompt's `Spec delta` section → session executes → retro confirms what landed → narrative document-history records the amendment → planning state advances.** Today, the first three steps run informally and the fourth runs not at all. This session makes all four explicit.

**This is rule encoding, not rule discovery.** The substantive decision (adopt spec-delta, don't adopt OpenSpec) was already made in the handoff session. This session lifts that decision into the README conventions where future session-runners will load it automatically, without re-deriving it from a planning note that is destined for deletion.

---

## Goal

Lift the spec-delta closure-loop discipline into the three README conventions that govern CritterCab's session workflow: prompts, retrospectives, and narratives. Author the convention's lightweight format (2–4 lines per prompt, single outcome-confirmation line per retro, structured document-history entry shape for narratives). Optionally land an ADR codifying spec-delta as a project-wide discipline if session-start judgment calls for it.

---

## Scope question to settle at session start

**One bundled PR for all three README amendments + optional ADR + this prompt and its retro.** Defensible as a single methodology-encoding session per the one-prompt-one-PR cadence and per the precedent of [`encode-tidy-methodology-refinements.md`](./encode-tidy-methodology-refinements.md) (which encoded two refinements in one PR across two convention files).

The ADR question is session-start judgment per the planning handoff §Follow-up sessions #Session A. **Lean: defer**, on the same reasoning as Session B's ADR-016 lean — the discipline is best ADR-ified after it has been exercised on 2–3 real sessions, not before. The convention's existence in the three READMEs is what triggers exercise; ADR-status comes later, if at all, via a follow-up session.

ADR numbering: if Session B's ADR-016 landed, this would be ADR-017. If Session B's ADR-016 deferred, this would be ADR-016. Confirm at session start by reading [`docs/decisions/README.md`](../decisions/README.md).

---

## Pre-work — handoff document inaccuracy to flag

The handoff doc that motivated this session (`docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md` §Decisions made this session #2) names "Narratives gain a Document-History section. Workshops already have one; narratives currently do not. Backfill 001 and 002 in the same session that introduces the convention" as a session deliverable. **This is wrong.**

Both [narrative 001](../narratives/001-rider-books-a-ride.md) (v0.1, 2026-04-25) and [narrative 002](../narratives/002-driver-accepts-a-ride.md) (v0.1, 2026-05-04) already have populated `## Document History` sections; the [narratives README](../narratives/README.md) §Body structure already names it as section #7. The backfill deliverable is **dropped from this session's scope** per the 2026-05-15 question-batch decision (handoff inaccuracy surfaced during context-map prompt authoring; backfill dropped, not retained).

What the narratives README *does* need is a small clarification on how document-history entries should read under spec-delta discipline (what kind of detail the entry should capture so it serves as the closure-loop's fourth step). That clarification is light — not a backfill — and is captured in deliverable #3.

Surface this in the retro under "what was harder than expected" so the planning-doc convention learns from the slip.

---

## Orientation files (read in order)

1. **`docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md`** — full handoff context. The decision is already made (don't adopt OpenSpec as a framework; do borrow its closure-loop pattern as "spec delta"). This session executes; it does not re-decide.
2. **[`docs/prompts/README.md`](./README.md)** — current state of the README being amended. Specifically: §Session and PR cadence (where a `### Spec delta cadence` subsection might slot, paired with the existing `### Scope: no opportunistic edits to other files` subsection); §Format conventions inside a prompt file (where the new "Spec delta" bullet lands in the minimum-required list).
3. **[`docs/retrospectives/README.md`](../retrospectives/README.md)** — current state of the second README being amended. Specifically: §Format conventions inside a retro file (where the new "Spec delta — landed?" outcome confirmation lands in the minimum-required list).
4. **[`docs/narratives/README.md`](../narratives/README.md)** — current state of the third README being touched (lightly). Specifically: §Format ‣ Body structure (item #7 names `## Document History` already); the clarification on what entries should *capture* lives near that mention.
5. **[`docs/decisions/003-spec-anchored-development.md`](../decisions/003-spec-anchored-development.md)** — the existing SDD commitment. Spec-delta is a refinement, not a replacement. The framing prose should be explicit about this: ADR-003 says specs are kept current via retrospective discipline; spec-delta names *what* that discipline operates on, prompt-by-prompt.
6. **[`docs/decisions/004-design-phase-workflow-sequence.md`](../decisions/004-design-phase-workflow-sequence.md)** — the workflow-sequence commitment. Spec-delta lives at step #6 (Implementation and Retrospective) and at step #5 (Prompt Authoring); it does not change the sequence, only the discipline at those two steps.
7. **[`docs/prompts/encode-tidy-methodology-refinements.md`](./encode-tidy-methodology-refinements.md)** — closest structural precedent for this session. Same shape: rule-encoding session that touches multiple README files in one PR, light retro, mechanical lifts from a source document (here, the handoff doc) into permanent convention files.
8. **OpenSpec project README** — *reference only*, accessible via web search if needed. The decision is to borrow the closure-loop pattern, not the framework. The handoff doc captures the rationale; the original OpenSpec README is referenced if the session-runner wants to see the borrowed-pattern source firsthand. **Do not adopt OpenSpec primitives wholesale; do not add a `change/` folder pattern; do not introduce a "tasks.md" or "spec.md" parallel artifact.** CritterCab's narratives and workshops *are* the canonical spec — spec-delta operates on them, not on a parallel OpenSpec-style spec layer.

---

## Working pattern

The session walks **one README at a time**, in deliberate order — matching the [`encode-tidy-methodology-refinements.md`](./encode-tidy-methodology-refinements.md) mechanical-lift rhythm rather than the per-edge per-ADR walk used by `decisions/002-bundled-pattern-adrs.md` or by Session B (context-map foundation). The substantive decision is already made; this is encoding work.

Order:

1. **`docs/prompts/README.md`** first. This is the heaviest amendment (two candidate landing spots: §Session and PR cadence as a new subsection, and §Format conventions inside a prompt file as a new minimum-required bullet). Authoring decision: do both, or one-or-the-other? Lean: **both** — the cadence subsection explains *why* the spec-delta section exists and how the closure loop runs; the format-conventions bullet enforces it on every new prompt. Asymmetric: cadence subsection is prose-heavy (~10–15 lines, like `### Scope: no opportunistic edits to other files`); format-conventions bullet is one bulleted line referencing the cadence subsection.
2. **`docs/retrospectives/README.md`** second. Single amendment: new bullet in §Format conventions inside a retro file naming the `Spec delta — landed?` outcome confirmation. Brief — points back to the prompts README for the definition. This is the closure-loop's third step encoded; deliberately short so the discipline reads as a confirmation, not a paragraph.
3. **`docs/narratives/README.md`** third. Lightest amendment: clarification near the existing §Body structure mention of `## Document History` (item #7) on what entries should *capture* — specifically, that an entry should name the prompt's spec-delta and confirm whether it landed (the closure-loop's fourth step). One-or-two-sentence clarification, not a new subsection.
4. **ADR question.** Settle at session start (lean: defer). If it lands, author after the three README amendments are signed off; if it defers, capture the trigger condition ("revisit after 2–3 sessions have exercised the discipline") in the retro.
5. **Prompt + index entry bundled commit.** Per the [skill-tidy retro lesson](../retrospectives/skills-tidy-marten-and-bootstrap.md) and the confirmation in [`skill-template-namespaces-pattern.md`](./skill-template-namespaces-pattern.md)'s retro.
6. **Retro + retros README index entry.**

For each README amendment:
- Pre-author the candidate prose (especially the prompts README cadence subsection's full wording).
- Surface the wording for sign-off, with the explicit watch-out from the handoff: *do not over-specify the `Spec delta` section's format; 2–4 lines per prompt is the target; codifying a rigid sub-schema defeats the lightweight intent.*
- Apply the edit; commit; move to the next README.

---

## The convention's substantive content (pre-authored leans)

This section pre-authors the leaning wording for each amendment, surfacing the parts that need sign-off rather than batching them. Per the [`002-bundled-pattern-adrs.md`](./decisions/002-bundled-pattern-adrs.md) precedent: don't ask for sign-off on leans without offering one.

### 1. `docs/prompts/README.md` — new `### Spec delta cadence` subsection

**Insertion point:** new subsection under `## Session and PR cadence`, slotting after `### Scope: no opportunistic edits to other files` (the current last subsection).

**Lean wording (slate; session can refine):**

> ### Spec delta cadence
>
> Every prompt names its **spec delta**: what the canonical spec (the narrative or workshop the session is satisfying) will gain when the session ships. The spec delta is expressed in spec-shaped terms (new moment, new slice, new forward-constraint, amended GWT, new translation slice, new ADR cross-reference) — distinct from the process-shaped session intent the rest of the prompt captures.
>
> The format is lightweight: 2–4 lines per prompt, named under a `## Spec delta` heading near the top of the prompt (after the metadata block, before the framing prose). Bulleted lines are fine; structured sub-schemas are not — the discipline lives in the naming, not in the formatting.
>
> At session close, the retrospective confirms whether the planned delta landed (see [retrospectives README § Format conventions](../retrospectives/README.md#format-conventions-inside-a-retro-file)). The narrative or workshop the session satisfies records the amendment in its `## Document History` section (see [narratives README § Body structure](../narratives/README.md#body-structure)). Together the four steps — prompt's spec delta → session executes → retro confirms → spec's document-history records — close the loop opened by [ADR-003 spec-anchored development](../decisions/003-spec-anchored-development.md), which committed to keeping specs current but did not name *how* per-session deltas are tracked.
>
> Pattern borrowed from OpenSpec's change-proposal payload; CritterCab does not adopt OpenSpec wholesale. The borrow is the discipline of capturing per-session spec amendments in spec-shaped terms, expressed inside the artifacts CritterCab already writes.

**Lean wording (format-conventions bullet, added to §Format conventions inside a prompt file's existing minimum-required list):**

> - **Spec delta** — 2–4 lines named in spec-shaped terms, capturing what the canonical narrative or workshop will gain when the session ships. See [§ Session and PR cadence ‣ Spec delta cadence](#spec-delta-cadence).

**Decisions to flag in-session:**
- (1a) Confirm subsection title is "Spec delta cadence" vs. alternatives ("Spec delta closure loop", "Per-prompt spec delta"). Lean: "Spec delta cadence" — matches sibling subsection naming pattern (`### Scope: no opportunistic edits`, `### Design-return cadence`, `### One prompt, one PR`).
- (1b) Confirm `## Spec delta` heading lives "after metadata block, before framing prose" vs. inside the metadata block as a metadata field. Lean: top-level heading — the spec delta is substantive content, not metadata; the metadata block stays for status/dates/target-artifacts.
- (1c) Confirm "2–4 lines" target. Lean: yes per handoff explicit watch-out ("over-specifying defeats lightweight intent").

### 2. `docs/retrospectives/README.md` — new format-conventions bullet

**Insertion point:** new bullet in §Format conventions inside a retro file's minimum-required list, after `Outstanding items / next-session inputs`.

**Lean wording:**

> - **Spec delta — landed?** — single line or short paragraph confirming whether the prompt's spec delta landed as planned, naming any divergence, and citing the spec amendment(s) made to the narrative or workshop in this session's PR. See [prompts README § Spec delta cadence](../prompts/README.md#spec-delta-cadence).

**Decisions to flag in-session:**
- (2a) Confirm bullet phrasing — "landed?" with question mark vs. "outcome" vs. "confirmation". Lean: "landed?" — the question mark matches the rhetorical shape of the existing "did this slice teach us anything that should update the Event Model or narrative?" framing from ADR-003.

### 3. `docs/narratives/README.md` — clarification near `## Document History` mention

**Insertion point:** in §Body structure, after the bullet for item #7 (`## Document History` — version log). One or two sentences, no new section.

**Lean wording (slate; session can refine):**

> Each document-history entry should name the prompt whose session produced the amendment and confirm what the prompt's spec delta added (a new moment, a forward-constraint update, an amended GWT cross-reference, etc.). This is the closure-loop's fourth step from [prompts README § Spec delta cadence](../prompts/README.md#spec-delta-cadence); the entry is the artifact's own record of the cumulative spec-delta history.

**Decisions to flag in-session:**
- (3a) Confirm this is a clarification under the existing item #7 bullet vs. a new top-level section in the narratives README. Lean: clarification — the item #7 bullet already exists; expanding it is lighter touch than introducing a new section.
- (3b) Confirm no backfill of existing entries in narratives 001 and 002 (per the §Pre-work inaccuracy flag and the 2026-05-15 question-batch decision). Lean: no backfill — spec-delta starts forward-only; the existing entries are minimal but not wrong.

---

## Deliverable plan

1. **`docs/prompts/README.md`** — new `### Spec delta cadence` subsection in §Session and PR cadence; one new bullet in §Format conventions inside a prompt file. ~20–30 lines added net.
2. **`docs/retrospectives/README.md`** — one new bullet in §Format conventions inside a retro file. ~2–3 lines added net.
3. **`docs/narratives/README.md`** — one-or-two-sentence clarification near §Body structure item #7. ~2–4 lines added net. **No backfill of narratives 001 or 002.**
4. **`docs/decisions/0NN-spec-delta-as-closure-loop-discipline.md`** *(optional, session-start decision)* — ADR per the canonical template if the discipline warrants ADR status. Lean: defer until exercised on 2–3 real sessions. Number depends on whether Session B's ADR-016 landed (would be ADR-017 if so, ADR-016 if not).
5. **`docs/decisions/README.md`** — add ADR row only if #4 lands.
6. **`docs/prompts/README.md`** — add this prompt's entry under `Multi-artifact prompts (root)`. Bundle with the #1 amendment commit per the [skill-tidy retro lesson](../retrospectives/skills-tidy-marten-and-bootstrap.md).
7. **`docs/retrospectives/encode-spec-delta-closure-loop.md`** — retro per the [retrospectives README](../retrospectives/README.md) format. **This is the first retro in CritterCab's history that should itself include the `Spec delta — landed?` line** — a self-referential exercise of the new convention. The line confirms whether the three README amendments landed as planned and whether the spec-delta convention is what the prompt named it would be.
8. **`docs/retrospectives/README.md`** — add retro entry under `Multi-artifact retros (root)`.

### Definition of done

- Three README amendments committed.
- ADR decision made and acted on (authored or deferred with reasoning recorded in the retro).
- Prompt + prompts-index entry bundled commit.
- Retro + retros-index entry committed, with the retro itself exercising the new `Spec delta — landed?` discipline.
- Bundled PR opened with all changes.

---

## Decisions to flag during the session

In addition to the per-amendment leans named above (§The convention's substantive content):

1. **ADR question — land now or defer?** Lean: defer until 2–3 real sessions have exercised the discipline. The convention's existence in the three READMEs is what triggers exercise; ADR-status comes later if at all.

2. **Self-reference handling.** This session's own prompt (this file) does NOT include a `Spec delta` section, because the convention doesn't exist yet at the prompt's authoring time. **This is fine.** The retro will exercise the new convention as its first real test (see deliverable #7). The same self-reference held in [`encode-tidy-methodology-refinements.md`](./encode-tidy-methodology-refinements.md) — the prompt that introduced the no-opportunistic-edits rule wasn't itself constrained by the rule until after its session shipped. The pattern repeats here cleanly.

3. **Retroactive application to Session B's retro?** Session B (context-map foundation) closes *before* this session runs per the recommended ordering. Question: should this session amend Session B's retro to add the `Spec delta — landed?` line retroactively? Lean: **no.** Retros are durable session-close records (per the retrospectives README: *"a retrospective is by definition a record of a completed session"*); amending Session B's retro after-the-fact would violate the "do not edit a prompt after the session it triggered has run" sibling principle as applied to retros. Instead, this session's retro can include a forward-looking note: "first session whose prompt will include a Spec delta section is the next session after this one."

4. **The "Spec delta" heading location in prompts.** Lean: top-level `## Spec delta` heading after the metadata block, before framing prose. Alternative: nested under framing (`## Framing — why this session exists` → `### Spec delta`). Surface for sign-off; the top-level placement signals load-bearing-ness; the nested placement reads as background.

5. **Whether to add a `Spec delta` cross-reference to ADR-003.** ADR-003 (spec-anchored development) is the parent commitment; spec-delta is its operational refinement. Tempting to update ADR-003's Consequences section to name the spec-delta discipline. Lean: **don't**. ADRs are durable artifacts; the [no-opportunistic-edits rule](./README.md#scope-no-opportunistic-edits-to-other-files) says edits to other files are out of bounds — and amending ADR-003 to retroactively mention a discipline introduced three weeks after its acceptance is exactly the kind of edit the rule was authored to prevent. If the ADR-003 ↔ spec-delta relationship needs to be made bidirectional, the spec-delta ADR (deliverable #4, if authored) is the right place to cite ADR-003; the reverse-link can land in a future ADR-003 revision when one is independently warranted.

---

## Out of scope

- **OpenSpec framework adoption.** The decision is to borrow the closure-loop *pattern*, not the framework. Do not introduce OpenSpec's `change/` folder pattern, parallel `spec.md` or `tasks.md` artifacts, or any other OpenSpec primitive. CritterCab's narratives and workshops *are* the canonical spec; spec-delta operates on them.
- **Backfill of narratives 001 and 002's `## Document History` sections.** Existing entries are minimal but not wrong. Spec-delta starts forward-only.
- **Amendment of Session B's retro to add a retroactive `Spec delta — landed?` line.** Retros are durable session-close records; the forward-looking note in this session's retro is sufficient.
- **Amendment of ADR-003 to cross-reference spec-delta.** Out of bounds per the no-opportunistic-edits rule; the spec-delta ADR (if it lands) is the right place to cite ADR-003.
- **Workshop or narrative authorship.** No new workshops, no new narratives. Methodology encoding only.
- **Spec-delta-driven re-review of past prompts/retros.** Prior prompts (workshop 001, narrative 001, narrative 002, the implementations, the ADR sessions) ran without spec-delta. They are not retroactively amended; spec-delta starts with the next prompt authored after this session ships.
- **Service-shaped or implementation-bearing additions to the convention.** Spec-delta is a documentation/methodology discipline; it does not bind on code structure, test conventions, or transport choices.

---

## Memory inheritance

The applicable feedback memories carry into this session via `MEMORY.md`. No restatement needed:

- **Communication depth + ubiquitous language + leaning opinions** — relevant throughout. Pre-author the lean wording for each README amendment; surface for sign-off the parts where the wording is the substantive call.
- **Critter Stack primitives** — not directly applicable (no code in this session) but applies if the convention prose tempts a reference to a hand-rolled spec-tracking mechanism; lead with Marten/Wolverine/Alba primitives where any mechanism is named.
- **Explicit deferrals during artifact authoring** — load-bearing for the ADR question (defer with named trigger condition) and for the no-backfill decision (explicit rather than silent).
- **Keep READMEs current alongside session work** — *intensely* load-bearing for this session because the three READMEs are themselves the deliverables. The discipline being encoded is itself "keep READMEs current with what sessions produce."
- **No Claude attribution on commits or PRs** — relevant at commit/PR time. Omit `Co-Authored-By: Claude` trailers and "Generated with Claude Code" PR footers.
- **Static endpoints, Alba-first tests; validation at HTTP boundary** — not directly applicable.
- **Prune textureless detail in narrative prose** — applies to the convention wording. Tight sentences; no atmospheric flavor. The wording is rule-encoding; rule-encoding is dense by nature.
- **Driver-mode when user signals a mental block** — applies to non-durable wording choices (subsection title casing, bullet phrasing micro-decisions). Keep sign-off discipline for the substantive convention calls (what spec-delta captures, what triggers retro-side confirmation, what entry shape narratives use).

---

## Starting move

When the new session begins:

1. **Read the orientation files in order** (§Orientation files above). Pay particular attention to the handoff doc's §Decisions made this session #2 (the spec-delta decision is settled there; this session executes it).
2. **Confirm scope** — one bundled PR for three README amendments + optional ADR + this prompt and its retro. Validate with the user; surface any movement.
3. **Confirm ADR-numbering** by reading [`docs/decisions/README.md`](../decisions/README.md). If Session B's ADR-016 landed, this would be ADR-017. If deferred, this would be ADR-016. The optional ADR's filename and number depend on this read.
4. **Settle the ADR question** at session start. Lean: defer with parking-lot trigger ("revisit after 2–3 sessions have exercised the discipline"). If user prefers to author now, the ADR slots into the per-README walk's tail end after all three amendments are signed off.
5. **Walk amendment #1 (`docs/prompts/README.md`)** first. Surface the pre-authored cadence-subsection wording for sign-off; settle decisions (1a), (1b), (1c) from §The convention's substantive content. Apply the edit; commit.
6. **Walk amendments #2 (`docs/retrospectives/README.md`) and #3 (`docs/narratives/README.md`)** in sequence. Each gets surface-the-lean → sign-off → edit → commit. These should walk briskly — both are short amendments referencing #1's longer prose.
7. **Compose the retro** per the retrospectives README. **The retro itself exercises the new `Spec delta — landed?` discipline** — the spec delta this session's prompt did not include (because the convention didn't exist yet) gets named retroactively in the retro as "the three READMEs gained the spec-delta convention; the narratives README clarified the document-history entry shape; the cadence loop is now four-step instead of three-step." Surface the self-reference as a methodology refinement in the retro.
8. **Open bundled PR.** Title: descriptive ("Encode spec-delta closure-loop discipline" or similar). Body: per-README summary + ADR disposition + the self-reference note about the retro exercising the new convention + test plan (read-through of the three READMEs verifying internal links are correct).

Don't batch the whole session into one output. README-amendment sessions are interactive — per-amendment sign-off keeps the wording honest. Total session estimate: 45–75 minutes (lighter than Session B; this is mostly mechanical lifts from pre-authored leans).
