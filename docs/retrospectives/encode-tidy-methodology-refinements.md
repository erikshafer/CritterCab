# Retrospective — Encode Skill-Tidy Methodology Refinements 1+2 into Permanent Rules

## Metadata

- **Triggering prompt:** [`docs/prompts/encode-tidy-methodology-refinements.md`](../prompts/encode-tidy-methodology-refinements.md)
- **Status:** Complete
- **Date authored:** 2026-05-08
- **Output artifacts:**
  - `docs/prompts/README.md` — added `### Scope: no opportunistic edits to other files` subsection to `## Session and PR cadence`
  - `docs/skills/DEBT.md` — added two new bullets to `## Conventions` (source-of-truth precedence; no-opportunistic-edits tidy-specific restatement with cross-reference)
  - `docs/prompts/encode-tidy-methodology-refinements.md` (new) — the prompt that triggered this session
  - `docs/prompts/README.md` — added prompt entry to `Multi-artifact prompts (root)`
  - `docs/retrospectives/encode-tidy-methodology-refinements.md` (new, this file) — the session retro
  - `docs/retrospectives/README.md` — added retro entry to `Multi-artifact retros (root)`
- **Outcome:** Two methodology refinements from the skill-tidy retrospective are now encoded as permanent rules. The general "no opportunistic edits to other files" rule lives in `prompts/README.md` § Session and PR cadence; the tidy-specific source-of-truth precedence rule and a tidy-specific restatement of no-opportunistic-edits live in `DEBT.md` § Conventions with a cross-reference to the general rule. Future tidy sessions load both rules automatically rather than rediscovering them from a retro.

---

## Framing

The skill-tidy retrospective surfaced four numbered methodology refinements. This session encodes the first two (the rules); the latter two (observations: bundled prompt + index commit pattern; Namespaces cheat-sheet pattern) stay as captured-but-not-rules. Both encoded refinements went through two sessions of empirical testing — skill-tidy itself and the PR #4 housekeeping session — before being locked in. That two-test cadence is itself worth noting as a pattern for future refinements: observe in one retro, exercise across sessions, encode once the pattern has held twice.

---

## Outcome summary

| Rule | Home | Source |
|---|---|---|
| No opportunistic edits to other files (general) | `docs/prompts/README.md` § Session and PR cadence (new `### Scope` subsection) | Skill-tidy retro § Methodology refinement #1 |
| Source-of-truth precedence (working code → retro → external docs) | `docs/skills/DEBT.md` § Conventions (new bullet) | Skill-tidy retro § Methodology refinement #2 |
| No opportunistic edits — tidy-specific restatement with cross-reference | `docs/skills/DEBT.md` § Conventions (new bullet) | Cross-reference to the general rule above |

---

## What worked

- **Recursive enforcement of the rule being encoded.** This session was about encoding "no opportunistic edits to other files," and applying it to itself was a cleanly observable test. The temptation to tighten existing prose in the cadence section (which was right there) was real but resisted. The rule held while encoding the rule.
- **Two-test cadence before locking in.** Both encoded rules were tested in two sessions (skill-tidy + PR #4 housekeeping) before being lifted to permanence. That gave each rule an empirical track record before becoming canonical. Worth keeping as the default cadence for methodology refinements that surface from retros: don't lock in until the rule has held across at least two sessions.
- **Two-home arrangement reads cleanly.** The general rule lives in `prompts/README` (where session authors loading the cadence section will see it directly); the tidy-specific application lives in `DEBT.md` (where tidy-session authors are already reading). The cross-reference between them feels right; neither home duplicates the other's content.
- **Bundled prompt + index commit pattern held again.** Three sessions in a row applying the lesson from the skill-tidy retro. Treat as the established default.

---

## What was harder than expected

Nothing surprising — convention encoding is mechanical when the source content is well-articulated in the originating retro. The skill-tidy retro's `**Recommendation:**` blocks under refinements #1 and #2 carried verbatim-quotable phrasing that integrated cleanly with the cadence section's existing prose style.

The DEBT.md edit hit one minor friction: a freshness-check failure forced a re-Read before the Edit could apply (file content was unchanged but the tool's tracking was stale after a branch switch and pull). Recoverable in one extra step; worth noting as expected friction after `git pull --ff-only` or branch switches.

---

## Methodology refinements that emerged

None new. As anticipated in the prompt's "Retro emphasis" section, encoding existing rules tends not to surface new rules. The "two-test cadence" observation in `What worked` is descriptive of the pattern that already worked, not a new rule that needs enforcement — though future retros may want to lean on it explicitly when proposing rule encodings.

---

## Outstanding items / next-session inputs

- **PR-C: add Namespaces pattern** to `docs/skills/_template/SKILL.md`. Final housekeeping PR in the queue. Slightly bigger micro-PR (introduces an example block; the others have been mostly text additions).
- **Trips workshop** — major design session, deferred until the housekeeping queue is complete (PR-C is the last item).
- **Workshop-followups-index backporting** — when the next workshop is authored, its §12.8 follow-ups land in the new index in the same PR. Convention is in place; reminder lives in PR-A's retro.

---

## Quantitative summary

- **Commits:** 4 (prompt + prompts/README index entry; prompts/README cadence subsection; DEBT.md conventions additions; retro + retros/README index entry).
- **Lines changed:** ~25 across 4 files (~22 added, ~3 modified for index entries).
- **Edit footprint:** small. One new subsection in prompts/README cadence; two new bullets in DEBT.md Conventions; two new files (prompt, retro); two index-entry additions.
- **Out-of-scope items deferred:** PR-C (Namespaces pattern), refinements #3 and #4 from the skill-tidy retro (observations not rules), Phase 6 placeholder cleanup, ai-skills lean-out, Trips workshop.
