# Retrospective — Refresh CLAUDE.md Routing Layer and Encode `tidy:` Commit Convention

## Metadata

- **Triggering prompt:** [`docs/prompts/refresh-claude-md-and-encode-tidy-convention.md`](../prompts/refresh-claude-md-and-encode-tidy-convention.md)
- **Status:** Complete
- **Date authored:** 2026-05-19
- **Output artifacts:**
  - `CLAUDE.md` — three edits:
    - Stale "no runnable code yet" line replaced with current-state prose (Dispatch slice running end-to-end; other BCs pre-workshop).
    - `## Session Workflow` rewritten to name the two-phase pipeline from ADR-004 (Context Mapping → Domain Storytelling → Event Modeling as pre-code design phase; Narrative → Prompt → Execute + Retro as per-slice loop). Per-layer README pointers retained as a sub-list. New paragraph forward-referencing the in-flight spec-delta closure-loop discipline and pointing at the pending encoding prompt.
    - `## Technology Stack` table reconciled: Wolverine row now states "5.32+ floor (the gRPC release that motivated this project); currently on 5.39+"; Marten gets "8.35+" with PostgreSQL 18 co-located in the row label; Polecat gets "3.1+"; new row for Aspire 13.3.
  - `docs/prompts/README.md` — new `### Commit subjects: \`tidy:\` for maintenance sessions` subsection added at the end of `## Session and PR cadence`.
  - `docs/prompts/refresh-claude-md-and-encode-tidy-convention.md` (new) — the prompt that triggered this session (authored retroactively per the workshop 001 precedent).
  - `docs/prompts/README.md` — added prompt entry to `Multi-artifact prompts (root)`.
  - `docs/retrospectives/refresh-claude-md-and-encode-tidy-convention.md` (new, this file) — the session retro.
  - `docs/retrospectives/README.md` — added retro entry to `Multi-artifact retros (root)`.
- **Outcome:** `CLAUDE.md` now accurately reflects current project state, names the two-phase pipeline a fresh AI on a different harness needs to understand, and forward-references the in-flight spec-delta discipline. The `tidy:` commit-subject convention is encoded in `docs/prompts/README.md` § Session and PR cadence where future maintenance sessions will load it automatically rather than inferring it from `git log`.

---

## Framing

The session was motivated by a user-initiated audit question: would a fresh AI on a different harness, with no memory or session transcript, be able to pick up CritterCab's pipeline from `CLAUDE.md` and the per-layer READMEs alone? The audit surfaced three drifts in `CLAUDE.md` (stale status, pipeline underspecified, version-table dated) plus an undocumented `tidy:` commit convention used across PRs #7–#10. The fix scope was deliberately narrow — just the drifts named in the audit — to avoid the opportunistic-edits failure mode while a routing-layer refresh was open.

The prompt was authored retroactively after the edits landed in the working tree. This is precedented by `workshops/001-dispatch-event-modeling.md` (also retroactive) and was the cleanest way to honor the one-prompt-one-PR cadence given the in-conversation iteration that produced the edits.

---

## Outcome summary

| Drift / Gap | Home of fix | Source of correct content |
|---|---|---|
| Stale "no runnable code yet" line | `CLAUDE.md` (one-line replacement) | `README.md` § Status |
| Pipeline underspecified (only narrative loop named; Context Mapping / Domain Storytelling / Event Modeling absent) | `CLAUDE.md` § Session Workflow (full rewrite) | `docs/decisions/004-design-phase-workflow-sequence.md` Option C |
| Spec-delta discipline not referenced anywhere a fresh AI would look | `CLAUDE.md` § Session Workflow (new forward-reference paragraph) | `docs/prompts/encode-spec-delta-closure-loop.md` (the pending encoding session) |
| Version-table drift (Wolverine 5.32+ vs reality 5.39+; Marten/Polecat unversioned; Aspire/PostgreSQL missing) | `CLAUDE.md` § Technology Stack (table edit) | `README.md` version table; `Directory.Packages.props` current state |
| `tidy:` commit convention undocumented despite four PRs using it | `docs/prompts/README.md` § Session and PR cadence (new `### Commit subjects` subsection) | PRs #7–#10 commit subjects (empirical) |

---

## What worked

- **Audit-then-scope discipline.** The session opened with a read-through audit of the routing-layer docs against a specific bar ("fresh AI on a different harness, no memory"). The audit produced a punch list; the fixes addressed exactly that list. The `tidy:` convention surfaced during the audit as a fifth item but was unambiguously in-scope (it's a `docs/prompts/README.md` convention encoding, not an opportunistic edit). No scope drift.
- **Two-file scope held cleanly.** `CLAUDE.md` and `docs/prompts/README.md` were the only files touched by substantive edits. The narratives README, workshops README, retrospectives README, and skills README were all audited and confirmed current; resisting the urge to make even small drive-by improvements there was the explicit no-opportunistic-edits-to-other-files rule in action.
- **Forward references over premature encoding.** The spec-delta paragraph in `CLAUDE.md` is a *forward reference* to the pending encoding session, not a restatement of the discipline. When that session ships, the forward-reference paragraph becomes the natural swap point for the landed convention — without requiring `CLAUDE.md` to be edited again as part of *that* session.
- **Reading ADR-004 verbatim for the pipeline rewrite.** ADR-004 Option C carries the canonical phrasing for the two-phase structure. Lifting verbatim where it integrated cleanly (and paraphrasing only to fit surrounding prose) avoided introducing inconsistency between routing-layer prose and the ADR it routes to.
- **Descriptive encoding of `tidy:`.** The convention as encoded describes what PRs #7–#10 already established, with a small "how a new area joins the list" rule (a second `tidy:` session in that area). No retconning, no inventing.

---

## What was harder than expected

- **The spec-delta forward-reference paragraph stretched the routing-layer brief.** `CLAUDE.md` is explicitly a routing layer, not a manual. The forward-reference paragraph for the in-flight spec-delta discipline is the longest single-paragraph addition in this session — five sentences. It earns its weight because the user-prompt language ("change/impl deltas") signals the concept matters to them, and a fresh AI on a different harness would not find spec-delta anywhere in core docs without this forward reference. But it does push CLAUDE.md slightly toward "manual" territory. Worth watching whether subsequent forward references (e.g., for the pending context-map artifact) accumulate to the point that CLAUDE.md needs a "Pending methodology work" subsection rather than inline forward references in their natural sections.
- **Slug naming overhead.** Naming the session was disproportionately effortful — the work spans "refresh stale routing-layer doc" + "encode commit convention" and no single short verb captures both. The chosen slug (`refresh-claude-md-and-encode-tidy-convention`) is clear but verbose. The cleaner shape would have been one session per concern (one for the CLAUDE.md refresh, one for the `tidy:` encoding), each with a tighter slug. Combined here because the user authorized "lift now" as a single move.

---

## Methodology refinements that emerged

None new as locked rules. One observation worth keeping:

- **Routing-layer audits decay differently than artifact audits.** Artifact files (workshops, narratives, ADRs) are append-only and stay correct by construction — they accumulate but rarely lie. Routing-layer files (`CLAUDE.md`, the per-layer READMEs) carry summary claims that decay silently as the project moves. A periodic routing-layer audit against a "fresh-AI-on-a-different-harness" bar is a natural cadence — perhaps prompted at every Nth implementation PR, or whenever a planning handoff doc is written (the audit catches drift the planning doc reveals indirectly).

Not lifted to a rule. If the audit cadence holds across two more sessions, candidate for encoding then.

---

## Outstanding items / next-session inputs

- **`docs/vision/README.md` v0.3 → v0.4 bump.** Vision doc has its own drift (BC section still says v0.1; ADRs 010–015 unreflected). Already scoped to land in the context-map-foundation session per its prompt's deliverable plan.
- **Context-map artifact authoring.** [`docs/prompts/context-map-foundation.md`](../prompts/context-map-foundation.md) is authored and pending execution. When it ships, the CLAUDE.md Session Workflow's parenthetical "directory pending" on Context Mapping can be removed.
- **Spec-delta encoding.** [`docs/prompts/encode-spec-delta-closure-loop.md`](../prompts/encode-spec-delta-closure-loop.md) is authored and pending execution. When it ships, the CLAUDE.md forward-reference paragraph swaps for a shorter pointer to the landed conventions in the per-layer READMEs.
- **Missing index entries for two pending prompts.** During this session's index-update pass, surfaced that neither [`context-map-foundation.md`](../prompts/context-map-foundation.md) nor [`encode-spec-delta-closure-loop.md`](../prompts/encode-spec-delta-closure-loop.md) is indexed in `docs/prompts/README.md` § Multi-artifact prompts despite both being committed (commit `2f9ca8e`). The omission appears to be a slip in that commit. Held out of this session's scope (the prompt named one index entry, not three); candidate for a small `tidy: housekeeping` micro-PR or for backporting when either pending session actually ships.
- **Audit cadence as a candidate rule.** If two more routing-layer audits surface drift on a similar cadence, encode "routing-layer drift audit every N implementation PRs" or "after every planning handoff" as a methodology rule.

---

## Quantitative summary

- **Commits:** 4 expected (prompt + prompts/README index entry; CLAUDE.md three-edit pass; prompts/README cadence subsection; retro + retros/README index entry).
- **Files touched:** 4 (CLAUDE.md, docs/prompts/README.md twice [substantive + index], docs/retrospectives/README.md [index]) plus 2 new files (prompt, retro).
- **Lines changed in CLAUDE.md:** ~40 (one line replaced; ~25-line Session Workflow rewrite replacing ~8 lines; one table row expanded and one new row added).
- **Lines added to docs/prompts/README.md:** ~15 (the new `### Commit subjects` subsection).
- **Out-of-scope items deferred:** vision doc v0.4 bump (deferred to context-map-foundation session); spec-delta encoding (its own session); context-map artifact authoring (its own session); audits of other per-layer READMEs (none surfaced drift).
