# Retrospective — Add Optional Namespaces Section to Skill Template

## Metadata

- **Triggering prompt:** [`docs/prompts/skill-template-namespaces-pattern.md`](../prompts/skill-template-namespaces-pattern.md)
- **Status:** Complete
- **Date authored:** 2026-05-08
- **Output artifacts:**
  - `docs/skills/_template/SKILL.md` — added new `## Namespaces` section between `## Prerequisites` and `## (Core content sections...)` placeholder
  - `docs/prompts/skill-template-namespaces-pattern.md` (committed earlier in 91187fb) — the prompt that triggered this session
  - `docs/prompts/README.md` — added prompt entry to `Multi-artifact prompts (root)` (commit 572eee6)
  - `docs/retrospectives/skill-template-namespaces-pattern.md` (new, this file) — the session retro
  - `docs/retrospectives/README.md` — added retro entry to `Multi-artifact retros (root)`
- **Outcome:** The Namespaces cheat-sheet pattern is now an optional template section. Future skill-authoring sessions copy the template and either fill in the table or delete the section; future tidy sessions adding namespace rows have a predictable structural home. Closes the housekeeping queue surfaced by the skill-tidy retrospective.

---

## Framing

Third and final housekeeping micro-PR after the skill-tidy session. Encodes methodology refinement #4 from the skill-tidy retro (the Namespaces cheat-sheet pattern as a structural pattern). Pairs with PR #9 which encoded refinements #1 and #2 (the rules); refinement #3 (bundled prompt + index commit) is process-observation that doesn't need structural encoding.

The Namespaces pattern earned its template promotion via the same two-test cadence as PR #9's rules: used in `marten-projections` (4-row table) and `marten-wolverine-aggregates` (1-row table + deferral note) during PR #7, with empirical evidence that the pattern was more durable than scattered namespace mentions through prose.

---

## Outcome summary

| Deliverable | Approach |
|---|---|
| New optional `## Namespaces` section in skill template | HTML comment block with when-to-use / when-to-delete / table format / when-to-add-paragraph / provenance guidance, plus a placeholder body table with `\| <TypeName> \| <Namespace.Path> \|` shape. Two example rows in the HTML comment drawn from `marten-projections` precedent (`IEvent<T>`, `SingleStreamProjection<TDoc, TId>`). |
| Placement decision | Between `## Prerequisites` and `## (Core content sections...)`. Reasoning: orientation flows highest-level → type-reference → substance. Alternative placement (between When-to-apply and Prerequisites) was undisambiguated by precedents (marten-* skills have no Prerequisites section) so the principled ordering won. |
| Provenance reference | HTML comment includes "(precedents: marten-projections § Namespaces with a 4-row table; marten-wolverine-aggregates § Namespaces with a 1-row table + forward-deferral note. Established in the first skill-tidy session, PR #7; see retrospectives/skills-tidy-marten-and-bootstrap.md § Methodology refinements #4)" — keeps the audit trail one reference away from the template itself. |

---

## What worked

- **Recursive enforcement of "no opportunistic edits to other files" held.** This was the first non-encoding session to test the rule from PR #9. Considered-and-resisted edits during execution: tightening some "Optional. Delete..." phrasing in adjacent template sections; making the `## Common pitfalls` placeholder example more concrete. Both were structurally tempting given the template was open in the editor; both were resisted because they're outside the prompt's deliverable plan. The rule actively shaped behavior. **First non-rule-encoding test of the rule passed.**
- **Section content followed established precedents cleanly.** The marten-projections and marten-wolverine-aggregates Namespaces sections supplied verbatim-quotable content for the example table inside the HTML comment. Lifting from precedent rather than re-deriving the format was load-bearing for keeping the addition compact.
- **Two-test cadence before locking in held a third time.** PR #9 used it for refinements #1 and #2; this session used it for refinement #4. That's three rules/patterns now lifted to permanence after passing two-session empirical tests. The cadence has earned its place as a default — observe → exercise across two sessions → encode.

---

## What was harder than expected

- **The bundled prompt + index commit pattern broke this round.** The skill-tidy retrospective's lesson — bundle the prompt + prompts/README index entry into one commit so they don't split — succeeded in PRs #8 and #9 but broke here. The user committed the prompt file independently (commit `91187fb`) between prompt-sketch sign-off and Claude's execution start, which forced the index entry to ride alone in commit `572eee6` after the user's commit. Same break shape as PR #7, where commit `b84e943` was the user's prompt-only commit and Claude's fix-up index commit followed.

  The pattern is now: when **Claude** sequences both commits, bundling holds; when the **user** commits the prompt independently before Claude starts, the bundle splits. Worth recording as a methodology observation; possibly worth an explicit convention for future sessions (see Methodology refinements below).

- **The user's commit message in `91187fb` described the prompt's content rather than the commit's actual diff.** The message reads "Add optional Namespaces section to skill template..." but the diff only contains the prompt file (no template change). Recoverable in a moment of `git show`, but mildly confusing during execution because it suggested the template change was already done. Likely an artifact of using the prompt's framing text as the commit message rather than describing the actual changes. Not a methodology issue; just a small noise factor for sessions where the user pre-commits the prompt.

---

## Methodology refinements that emerged

- **"Bundling holds when Claude sequences; bundling breaks when the user pre-commits."** The skill-tidy retro's bundling lesson is reproducible *only when Claude is the one doing both commits in sequence*. When the user commits the prompt independently, the bundle splits and a fix-up commit is needed.

  **Recommendation for future sessions:** the convention should be explicit about who commits what. Options:

  - **(a)** "After sign-off, Claude commits the prompt + prompts/README index entry as the first commit of execution. The user does not pre-commit the prompt." — most prescriptive; keeps bundling reliable.
  - **(b)** "Either party may commit the prompt; if the user commits it independently, the prompts/README index update follows as a separate fix-up commit." — accepts the split as the cost of user flexibility; relaxes the bundling rule.

  This refinement is **not** encoded in this PR. It needs the same two-test cadence as the previous rule encodings (this is the first session where the issue was explicitly named; the PR #7 instance was implicit). If the same break recurs in a future session, a follow-up housekeeping PR can encode the chosen convention into `prompts/README.md` § Session and PR cadence.

- **The "rule earned via two-test cadence" pattern is itself a methodology pattern worth naming.** Not encoded here either; observation only. Future rule-encoding sessions can reference it explicitly: "this rule was tested in [session X] and [session Y] and is now ready for permanent encoding."

---

## Outstanding items / next-session inputs

- **Trips workshop** — major design session. Next up. The housekeeping queue surfaced by the skill-tidy retrospective is now drained. No more housekeeping items pending.
- **Bundling-break refinement** — see Methodology refinements #1 above. Encode-or-not decision deferred until a third instance arrives. Possible third-time data point will tell whether (a) or (b) is the right convention.
- **Workshop-followups-index backporting** — when the Trips workshop is authored, its §12.8 follow-ups should land in the `docs/workshops/README.md` index in the same PR. Convention is in place from PR #8.
- **Backporting the Namespaces pattern to other existing skills** — explicitly out of scope per the prompt. Tidy sessions add namespace rows when gaps surface; this is *not* a retroactive populate-empty-sections pass.

---

## Quantitative summary

- **Commits:** 3 on this branch (prompt-only by Erik in `91187fb`; prompts/README index by Claude in `572eee6`; template Namespaces section by Claude in `581210b`); the closing commit with this retro + retros/README index entry will make 4. Plus one additional commit in the user's pre-commit before the bundling-break observation surfaced — in retrospect that should have been the bundled commit per the PR #8 / PR #9 pattern.
- **Lines changed:** ~50 across 3 files (1 new template section ~38 lines; 1 prompts/README index entry; 1 retros/README index entry).
- **Edit footprint:** small. One new template section with HTML comment guidance + placeholder content; two index-entry additions.
- **Out-of-scope items deferred:** backporting Namespaces to other skills, restructuring template's existing sections, updating `docs/skills/README.md` Conventions reference, Phase 6 placeholder cleanup, ai-skills lean-out, Trips workshop.
- **No-opportunistic-edits resisted edits captured during execution:** tightening adjacent "Optional. Delete..." phrasing; concretizing `## Common pitfalls` placeholder example. Both deferred to potential future template-tidy session if warranted.
