# Prompt — Encode Skill-Tidy Methodology Refinements 1+2 into Permanent Rules

| Field | Value |
|---|---|
| **Status** | Pending (sketched 2026-05-08; awaiting review before execution) |
| **Authored** | 2026-05-08 |
| **Target artifacts** | `docs/prompts/README.md` (new "Scope" subsection in Session and PR cadence), `docs/skills/DEBT.md` (two new Conventions bullets), `docs/prompts/encode-tidy-methodology-refinements.md` (this prompt), `docs/prompts/README.md` (index update), `docs/retrospectives/encode-tidy-methodology-refinements.md` (new retro), `docs/retrospectives/README.md` (index update) |
| **Source-of-truth dependencies** | [`docs/retrospectives/skills-tidy-marten-and-bootstrap.md`](../retrospectives/skills-tidy-marten-and-bootstrap.md) § Methodology refinements (refinements #1 and #2 are the source content); [`docs/prompts/README.md`](./README.md) § Session and PR cadence (where the general rule slots in); [`docs/skills/DEBT.md`](../skills/DEBT.md) § Conventions (where the tidy-specific restatement slots in) |
| **Workflow position** | Second housekeeping micro-PR after the skill-tidy session. Encodes two methodology refinements that were captured in the skill-tidy retrospective but never landed as rules. Rule encoding outlives any single session — once these rules are in `prompts/README` and `DEBT.md`, future tidy sessions load them automatically rather than rediscovering them from a retro. |

---

## Framing — why this session exists

The skill-tidy retrospective surfaced four numbered methodology refinements as inputs to future tidy sessions. The first two are rules and the last two are observations:

1. **"No opportunistic edits to *other files*"** — refinement of the original "no opportunistic edits" rule. Same-file edits (typo fixes, factual corrections in doc-history lines, etc.) during a session are in-bounds; other-file edits are not.
2. **Source-of-truth precedence for tidy sessions** — working code → retro evidence → external docs. The retro is evidence the gap once existed, not proof it still does.
3. *(observation)* Bundled prompt + index commit pattern is reproducible. Already captured in retros; not a rule that needs enforcement.
4. *(observation)* "Namespaces" cheat-sheet pattern. Already captured; PR-C will lift this into the skill template separately.

This session encodes refinements #1 and #2. Refinements #3 and #4 stay as observations.

The encoding has been deferred until now because both refinements were tested in subsequent sessions before being locked in. Refinement #1 was exercised in the PR #4 housekeeping session (PR #8) where the in-bounds same-file path was used (the rename-the-section-while-adding-the-entry case). Refinement #2 was load-bearing in the original skill-tidy session itself. Both have empirical track records before becoming rules.

---

## Goal

Land both rule encodings in one micro-PR. Add a new `### Scope: no opportunistic edits to other files` subsection to the `## Session and PR cadence` section of `docs/prompts/README.md`. Add two new convention bullets to `docs/skills/DEBT.md`'s `## Conventions` section: one for source-of-truth precedence, one as a tidy-specific restatement of the no-opportunistic-edits rule with a cross-reference to the prompts/README home.

---

## Orientation files (read in order)

1. **[`docs/prompts/README.md`](./README.md) § Session and PR cadence** — current state of the section that gets the new "Scope" subsection. Currently has two subsections (One prompt, one PR; Design-return cadence); the new one slots after them.
2. **[`docs/skills/DEBT.md`](../skills/DEBT.md) § Conventions** — current state of the bulleted conventions list that gets two new bullets.
3. **[`docs/retrospectives/skills-tidy-marten-and-bootstrap.md`](../retrospectives/skills-tidy-marten-and-bootstrap.md) § Methodology refinements** — source content for the rule wording. Refinement #1 has the rule text and the same-file in-bounds exception; refinement #2 has the source-of-truth precedence ordering and the rationale ("the retro is evidence the gap once existed, not proof it still does").
4. **[`docs/retrospectives/housekeeping-pr4-followups.md`](../retrospectives/housekeeping-pr4-followups.md)** — confirms the same-file-edit case held in practice (the rename was an in-bounds opportunistic-feeling edit to the file actively being modified).

---

## Working pattern

- **One PR for both rule encodings** plus the prompt + retro per the cadence rule.
- **Bundle prompt + prompts/README index entry** into one commit. Two for two now (skill-tidy retro lesson; PR #8 confirmation); treat as default.
- **Per-file commits** beyond the bundled prompt-and-index commit: one for the prompts/README cadence-section addition, one for the DEBT.md conventions additions, one closing commit with retro + retros/README index entry.
- **Wording precedence: retro source over freelance phrasing.** The retro's recommendation lines (`**Recommendation:**` blocks under refinements #1 and #2) carry the canonical phrasing. Lift verbatim where it fits cleanly; only paraphrase to integrate with surrounding prose style.
- **No opportunistic edits to other files.** Recursive enforcement: this session is encoding the rule, so it had better follow it. The two rule files plus the prompt + retro + index entries are the entire scope.

---

## Deliverable plan

1. **`docs/prompts/README.md`** — new `### Scope: no opportunistic edits to other files` subsection added to the `## Session and PR cadence` section. Slots after `### Design-return cadence` (the existing last subsection). Content (concise; matches surrounding subsection style):

   - Rule statement: a session's edits stay within the files named in its prompt's deliverable plan.
   - Same-file in-bounds exception, with concrete example shape (the doc-history correction case from the skill-tidy retro).
   - Why: the rationale (scope creep, dilute review, revertability).
   - Provenance line: lifted from skill-tidy retro § refinement #1; confirmed in practice by PR #8's housekeeping session.

2. **`docs/skills/DEBT.md`** — two new bullets added to the `## Conventions` section. Insertion point: after the existing five convention bullets (just before the closing `---` of that section, or in whatever ordering reads cleanest with the existing rules).

   - Bullet A — **source-of-truth precedence:** "Tidy sessions verify each row against current state before fixing." Precedence order: working code → retro evidence → external docs. The retro is evidence the gap once existed, not proof it still does. Provenance: skill-tidy retro § refinement #2.
   - Bullet B — **no opportunistic edits:** tidy-specific restatement, with a cross-reference link to the new `prompts/README` § Scope subsection. Short — primary home is prompts/README; this is a tidy-specific pointer.

3. **`docs/prompts/encode-tidy-methodology-refinements.md`** (this prompt) — bundled with deliverable #4 in one commit per the lesson.

4. **`docs/prompts/README.md`** — add prompt entry under `Multi-artifact prompts (root)`. Bundled with deliverable #3.

5. **`docs/retrospectives/encode-tidy-methodology-refinements.md`** — retro per the format conventions. Lighter than the skill-tidy retro (this is convention-encoding, not skill-fixing); should still hit metadata, framing, outcome, what worked, what was harder, methodology refinements (probably none — encoding rules tends not to surface new ones), outstanding items.

6. **`docs/retrospectives/README.md`** — add retro entry under `Multi-artifact retros (root)`.

---

## Decisions to flag during the session

1. **Primary home of the no-opportunistic-edits rule.** Lean: `prompts/README.md` § Session and PR cadence (general session-scope rule applicable to any session with named deliverables). DEBT.md gets a tidy-specific restatement + pointer. Alternative would be DEBT.md as primary — but the rule applies beyond tidy sessions (any session whose prompt names deliverables), so cadence is the more general home.

2. **Whether the new subsection title should be "Scope" or "Scope discipline" or "No opportunistic edits to other files."** Lean: `### Scope: no opportunistic edits to other files` — descriptive, matches the existing pattern (`### One prompt, one PR`, `### Design-return cadence`). Mechanical.

3. **Whether the source-of-truth precedence bullet should mention non-skill-tidy sessions.** Lean: keep it tidy-scoped in DEBT.md (since DEBT.md is itself the tidy-session ledger). If it ever applies to a non-tidy session, that session can cite the rule from here.

4. **Bullet ordering in DEBT.md `Conventions` section.** Existing order is roughly: row format → row organization → registration → removal → priority disclaimer. The two new bullets fit in different places: source-of-truth precedence is about the *fix step*, no-opportunistic-edits is about *fix scope*. Reasonable insertion: after the registration/removal bullets, before the priority disclaimer; or at the end as "tidy execution rules." Lean: at the end, grouped together, prefaced with a short transition.

---

## Out of scope

- **PR-C's Namespaces pattern** in `docs/skills/_template/SKILL.md` — separate micro-PR.
- **Refinements #3 and #4 from the skill-tidy retro** (bundled prompt + index commit pattern; structural-shape index naming) — observations, not rules; not encoded here.
- **Refinements from the PR #4 housekeeping retro** — those were observations as well; PR-A already captured them in its retro.
- **Restructuring the cadence section's existing subsections** — only adding the new subsection; existing prose stays.
- **Restructuring DEBT.md's existing conventions** — only adding the new bullets.
- **Phase 6 placeholder cleanup; ai-skills lean-out** — long-term out-of-scope.
- **Trips workshop** — major design session, deferred until housekeeping queue is drained (PR-C is last).

---

## Retro emphasis

Light. This is convention-encoding, not skill-fixing or methodology-discovery. The retro should still capture:

- Whether the rule wording reads cleanly when integrated with surrounding prose, or whether it needs softening/sharpening.
- Whether the cross-reference between prompts/README (primary) and DEBT.md (tidy-specific pointer) feels right, or whether duplication / single-home would be cleaner.
- Any unexpected friction in the encoding pass that future rule-encoding sessions should anticipate.

Probably no new methodology refinements emerge — encoding existing rules tends to be mechanical. If new refinements DO emerge, capture them as inputs to a follow-up session, not as in-line rule additions in this PR.
