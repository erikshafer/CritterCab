# Prompt — Add Optional Namespaces Section to Skill Template

| Field | Value |
|---|---|
| **Status** | Pending (sketched 2026-05-08; awaiting review before execution) |
| **Authored** | 2026-05-08 |
| **Target artifacts** | `docs/skills/_template/SKILL.md` (new optional Namespaces section), `docs/prompts/skill-template-namespaces-pattern.md` (this prompt), `docs/prompts/README.md` (index update), `docs/retrospectives/skill-template-namespaces-pattern.md` (new retro), `docs/retrospectives/README.md` (index update) |
| **Source-of-truth dependencies** | [`docs/retrospectives/skills-tidy-marten-and-bootstrap.md`](../retrospectives/skills-tidy-marten-and-bootstrap.md) § Methodology refinements (#4 — Namespaces cheat-sheet pattern); [`docs/skills/marten-projections/SKILL.md`](../skills/marten-projections/SKILL.md) § Namespaces (precedent #1, 4-row table); [`docs/skills/marten-wolverine-aggregates/SKILL.md`](../skills/marten-wolverine-aggregates/SKILL.md) § Namespaces (precedent #2, 1-row table) |
| **Workflow position** | Third and final housekeeping micro-PR after the skill-tidy session. Lifts the Namespaces cheat-sheet pattern from two-skill empirical use into a template-level optional section. Future skill-authoring sessions can use the section directly; future tidy sessions adding namespace rows have a predictable structural home. |

---

## Framing — why this session exists

The skill-tidy retro flagged four methodology refinements. PR #9 encoded the two that were rules; this PR encodes the one that was a *structural pattern* (refinement #4 — Namespaces cheat-sheet). The remaining refinement (#3 — bundled prompt + index commit pattern) is a process observation that doesn't need a structural home; it's already implicitly enforced by the `Multi-artifact prompts (root)` index convention.

The Namespaces pattern earned its place by passing the same two-test cadence as PR #9's rules: it was used in `marten-projections` (4 rows, more elaborate) and `marten-wolverine-aggregates` (1 row, with deferral note) during PR #7, and the empirical evidence showed it was more durable than scattered namespace mentions through prose.

This PR is also the first non-encoding micro-PR to test the "no opportunistic edits to other files" rule (encoded in PR #9, just merged). Recursive enforcement remains in effect: the rule applies to this session.

---

## Goal

Add an optional `## Namespaces` section to `docs/skills/_template/SKILL.md` between the existing "Prerequisites" section and the "(Core content sections...)" placeholder. Section follows the established optional-section style of the template (HTML comment with usage guidance, placeholder content, deletable when not applicable). Future skill-authoring sessions copy the template and either fill in the table or delete the section; future tidy sessions adding namespace rows have a predictable structural home.

---

## Orientation files (read in order)

1. **[`docs/skills/_template/SKILL.md`](../skills/_template/SKILL.md)** — current state. Note the optional-section pattern used by `Prerequisites` and `Common pitfalls` (HTML comment explaining when to delete; placeholder content with shape clear).
2. **[`docs/skills/marten-projections/SKILL.md`](../skills/marten-projections/SKILL.md)** — first precedent. The Namespaces section sits immediately after "When to apply this skill" with a 4-row table and an "easy to get wrong" rationale paragraph above the table plus a "pin to the table" closing line below.
3. **[`docs/skills/marten-wolverine-aggregates/SKILL.md`](../skills/marten-wolverine-aggregates/SKILL.md)** — second precedent. Smaller 1-row table with a forward-deferral note for primitives not yet tabulated. Demonstrates the pattern's flexibility for skills that only need to flag one or two namespaces.
4. **[`docs/retrospectives/skills-tidy-marten-and-bootstrap.md`](../retrospectives/skills-tidy-marten-and-bootstrap.md) § Methodology refinements** (refinement #4) — source rationale. Recommendation line: *"Could become an optional section in `docs/skills/_template/SKILL.md`. Future tidies that surface namespace gaps then add rows to existing tables rather than authoring new sections."*

---

## Working pattern

- **One PR for the template addition.** PR title: `tidy: skill-template — add optional Namespaces section`.
- **Bundled prompt + prompts/README index entry** into one commit (four sessions in a row applying this — established default).
- **Single `tidy:` commit** for the template change.
- **Closing commit** with retro + retros/README index entry.
- **Recursive enforcement of the no-opportunistic-edits rule** (encoded in PR #9) — first test of the rule on a non-encoding session. The temptation to also tidy the template's existing `Common pitfalls` placeholder text (or any other small thing) is the test. Resist; capture in retro if anything's worth a future session.

---

## Deliverable plan

1. **`docs/skills/_template/SKILL.md`** — new `## Namespaces` section. Placement: **between** the existing `## Prerequisites` section and the `## (Core content sections — replace heading and structure to fit the skill)` placeholder. Content shape:

   - HTML comment block at the top explaining: (a) when to use the section, (b) when to delete it, (c) the established table format, (d) how to add an above-table rationale paragraph or below-table closing line when warranted, (e) where the pattern came from (PR #7 / skill-tidy retro § refinement #4).
   - A placeholder table with one row showing the format, matching the precedent's `| Type | Namespace |` header.
   - Optional placeholder for the brief above- or below-table note (one line, italicized "optional" indicator).

2. **`docs/prompts/skill-template-namespaces-pattern.md`** (this prompt) — bundled with the prompts/README index entry in one commit.

3. **`docs/prompts/README.md`** — add prompt entry under `Multi-artifact prompts (root)`.

4. **`docs/retrospectives/skill-template-namespaces-pattern.md`** — retro per the format conventions. Light retro per the prompt's emphasis section (template-addition is mechanical when source content is well-articulated in the precedents).

5. **`docs/retrospectives/README.md`** — add retro entry under `Multi-artifact retros (root)`.

---

## Decisions to flag during the session

1. **Section placement.** Lean: between `## Prerequisites` and `## (Core content sections...)`. Alternative: between `## When to apply this skill` and `## Prerequisites`. The marten-* precedents have no Prerequisites section, so empirical evidence doesn't disambiguate. Reasoning for the Prerequisites→Namespaces→Core ordering: Prerequisites is "what to load first"; Namespaces is "what types/where they live"; Core is "the substance." Orientation material flows from highest-level (skills) to lowest-level (types) to substance. Mechanical.

2. **Whether the HTML comment should include the precedent table verbatim, or just describe the format.** Lean: include a 1- or 2-row example table inside the HTML comment so the reader has a concrete shape without leaving the file. Smaller than the marten-projections 4-row table; keep the comment from being too long.

3. **Whether to mention the PR #7 / skill-tidy retro provenance in the HTML comment.** Lean: yes — a one-line "(pattern from PR #7 / skill-tidy retro § refinement #4)" reference. Retros are the project's audit trail; pointing future readers there from inside the template is the convention's natural extension.

4. **Whether the placeholder body content should include a placeholder row or be empty.** Lean: include a placeholder row (`| <TypeName> | <Namespace.Path> |`) so a skill-author who keeps the section knows where to start typing. Matches how the Prerequisites and See Also sections show placeholder content.

---

## Out of scope

- **Backporting the Namespaces section to other existing skills.** Tidy sessions add namespace rows to existing skills when gaps surface; this is *not* a retroactive populate-empty-sections pass. The skill template addition makes the section *available* for future skill authoring, not *mandatory*.
- **Updating `docs/skills/README.md`'s "Conventions reference" section to enumerate the new template section.** That would be an opportunistic edit to another file. The README's conventions reference doesn't enumerate every template section; it lists meta-level conventions. Skip.
- **Restructuring the template's existing sections.** Only adding the new section. The existing Prerequisites, Common pitfalls, and See Also sections stay as-is.
- **Phase 6 placeholder cleanup; ai-skills lean-out.** Long-term out of scope per `docs/skills/DEBT.md`'s "Out of scope" carve-out.
- **Trips workshop.** Major design session, scheduled to be the next session after this PR closes the housekeeping queue.

---

## Retro emphasis

Light. This is template addition, not skill authoring or rule encoding. Expected captures:

- Whether the placement (between Prerequisites and Core content) reads cleanly with the surrounding sections.
- Whether the HTML comment's guidance is calibrated correctly — not too prescriptive (which would discourage skills that need a single-row table from using the section), not too vague (which would leave the skill author guessing).
- Whether the example table inside the HTML comment + placeholder row in the body strikes the right balance, or whether one should be reduced to leave room for the other.
- Whether the recursive enforcement of "no opportunistic edits to other files" held — this is the first non-encoding session to test the rule. The retro should explicitly note any edits considered-but-resisted.

If new methodology refinements emerge, capture them as inputs to a future encoding session — not as in-line additions in this PR.
