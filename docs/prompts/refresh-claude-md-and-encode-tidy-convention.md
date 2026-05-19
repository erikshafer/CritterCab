# Prompt — Refresh CLAUDE.md Routing Layer and Encode `tidy:` Commit Convention

| Field | Value |
|---|---|
| **Status** | Authored retroactively after the session ran (2026-05-19). Edits to `CLAUDE.md` and `docs/prompts/README.md` were already in the working tree when this prompt was drafted; the prompt formalizes the session per the one-prompt-one-PR cadence. |
| **Authored** | 2026-05-19 |
| **Target artifacts** | `CLAUDE.md` (three edits: stale-status fix, Session Workflow expansion naming the two-phase pipeline from ADR-004, Technology Stack version reconciliation + Aspire/PostgreSQL additions); `docs/prompts/README.md` (new `### Commit subjects: tidy: for maintenance sessions` subsection under Session and PR cadence); `docs/prompts/refresh-claude-md-and-encode-tidy-convention.md` (this prompt); `docs/prompts/README.md` (index entry); `docs/retrospectives/refresh-claude-md-and-encode-tidy-convention.md` (retro); `docs/retrospectives/README.md` (index entry) |
| **Source-of-truth dependencies** | [`CLAUDE.md`](../../CLAUDE.md) (the routing layer being refreshed); [`README.md`](../../README.md) (correct project state, version table); [`docs/decisions/004-design-phase-workflow-sequence.md`](../decisions/004-design-phase-workflow-sequence.md) (canonical pipeline phase definitions lifted into CLAUDE.md); [`docs/prompts/encode-spec-delta-closure-loop.md`](./encode-spec-delta-closure-loop.md) (the pending spec-delta encoding session forward-referenced from CLAUDE.md); [`docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md`](../planning/2026-05-15-spec-delta-and-context-map-handoff.md) (the handoff that motivated the spec-delta and context-map artifact decisions); commit history (PRs #7–#10 establish the `tidy:` commit-subject convention being encoded) |
| **Workflow position** | Methodology-refresh session interleaved between the two pending design-phase sessions (`context-map-foundation` and `encode-spec-delta-closure-loop`) and the next implementation or workshop session. Motivated by an audit question: would a fresh AI on a different harness, with no memory or session transcript, be able to pick up the project's pipeline from `CLAUDE.md` and the per-layer READMEs alone? The audit surfaced drift in CLAUDE.md's status line, version table, and Session Workflow section, plus an undocumented `tidy:` commit convention used across PRs #7–#10. |

---

## Framing — why this session exists

CritterCab's `CLAUDE.md` is explicitly framed as a routing layer ("This file is a routing layer, not a manual"). Routing layers decay differently than artifact docs: artifacts are append-only and stay correct by construction, but routing docs lie quietly — they remain plausible-looking long after the project has moved past them. Three specific drifts had accumulated by 2026-05-19:

1. **Stale project status.** `CLAUDE.md` said "the project is currently in the design phase. No runnable code yet." Reality: the Dispatch service has its first vertical slice (`RideRequested`) running end-to-end (PR #4), with Marten event sourcing, Wolverine.HTTP, and Alba integration tests.
2. **Pipeline underspecified.** The Session Workflow section named only the `narrative → prompt → execute → retrospective` loop. It did not name the two-phase structure from ADR-004 (one-time pre-code design phase: Context Mapping → Domain Storytelling → Event Modeling; per-slice loop: Narrative → Prompt → Execute + Retro). A fresh AI reading `CLAUDE.md` would not learn that Context Mapping is a defined pipeline step — they would only find it by opening ADR-004.
3. **Technology-stack drift.** Wolverine listed as 5.32+ (the historically-significant gRPC release floor) while the project is now on 5.39+. Marten and Polecat had no versions; Aspire and PostgreSQL were missing entirely. The README's version table was correct; CLAUDE.md's was not.

Separately, the `tidy:` commit-subject convention had emerged organically across PRs #7–#10 (`tidy: skills`, `tidy: housekeeping`, `tidy: encode-<rule>`, `tidy: skill-template`) without ever being defined in a convention file. A fresh AI seeing `tidy:` subjects in `git log` would have to infer the convention.

This session lifts all four into the appropriate routing-layer or convention-file home.

---

## Goal

Refresh `CLAUDE.md` against the three drifts above. Encode the `tidy:` commit-subject convention as a new subsection of `docs/prompts/README.md` § Session and PR cadence. Bundle both into one PR with prompt + retro per the one-prompt-one-PR cadence.

---

## Orientation files (read in order)

1. **[`CLAUDE.md`](../../CLAUDE.md)** — the routing layer being refreshed. Current state has the three drifts named in the framing.
2. **[`README.md`](../../README.md)** — the correct project state and version table. CLAUDE.md's refreshed values mirror this doc's table where they overlap.
3. **[`docs/decisions/004-design-phase-workflow-sequence.md`](../decisions/004-design-phase-workflow-sequence.md)** — the canonical pipeline definition. The Session Workflow rewrite lifts the two-phase structure verbatim (Option C as decided), not paraphrased.
4. **[`docs/prompts/encode-spec-delta-closure-loop.md`](./encode-spec-delta-closure-loop.md)** — the pending spec-delta encoding session. CLAUDE.md's forward-reference paragraph points here; the framing in CLAUDE.md mirrors this prompt's framing.
5. **[`docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md`](../planning/2026-05-15-spec-delta-and-context-map-handoff.md)** — context for the spec-delta forward reference. Not lifted directly; informs the framing.
6. **[`docs/prompts/README.md`](./README.md) § Session and PR cadence** — current state of the cadence section that gets the new `tidy:` subsection. Has three subsections (One prompt, one PR; Design-return cadence; Scope: no opportunistic edits to other files); the new one slots after them.
7. **`git log` for PRs #7–#10** — empirical source for the `tidy:` convention's established areas.

---

## Working pattern

- **One PR for all changes** plus the prompt + retro per the cadence rule. Two files touched by the substantive changes (CLAUDE.md + prompts/README.md), plus the prompt, retro, and two README index entries.
- **Bundle prompt + prompts/README index entry** into one commit per the skill-tidy retro lesson (now four sessions in a row applying it; treat as default).
- **Per-file commits** beyond the bundled prompt-and-index commit: one for the CLAUDE.md edits (three edits to one file = one commit), one for the prompts/README.md `tidy:` subsection, one closing commit with retro + retros/README index entry.
- **Wording precedence:** ADR-004's Option C carries the canonical phrasing for the two-phase structure; lift verbatim where it integrates cleanly. The `tidy:` subsection is descriptive — describe what PRs #7–#10 already established, do not invent new sub-conventions.
- **No opportunistic edits to other files.** The two substantive files plus the prompt + retro + two index entries are the entire scope. No README cleanup elsewhere even if it surfaces.

---

## Deliverable plan

1. **`CLAUDE.md`** — three edits:
   - Replace the "no runnable code yet" line with current-state prose mirroring `README.md`'s Status section.
   - Rewrite the `## Session Workflow` section to name the two-phase pipeline (Context Mapping → Domain Storytelling → Event Modeling → Narrative → Prompt → Execute + Retro), keep the per-layer README pointers, and add a forward-reference paragraph on the in-flight spec-delta discipline pointing at the pending encoding prompt.
   - Update the `## Technology Stack` table: Wolverine row to "5.32+ floor (the gRPC release that motivated this project); currently on 5.39+"; Marten gets "8.35+" with PostgreSQL 18 co-located; Polecat gets "3.1+"; new row for Aspire 13.3.

2. **`docs/prompts/README.md`** — new `### Commit subjects: \`tidy:\` for maintenance sessions` subsection added at the end of `## Session and PR cadence` (after `### Scope: no opportunistic edits to other files`). Content:
   - One-sentence rule statement (use `tidy: <area> — <details>` for maintenance of existing artifacts).
   - Established areas (4 bullets matching PRs #7–#10).
   - What `tidy:` is *not* used for (new workshops, narratives, ADRs, implementation slices).
   - How a new area joins the list (second session in that area lands).

3. **`docs/prompts/refresh-claude-md-and-encode-tidy-convention.md`** (this prompt) — bundled with deliverable #4 in one commit per the lesson.

4. **`docs/prompts/README.md`** — add prompt entry under `Multi-artifact prompts (root)`. Bundled with deliverable #3.

5. **`docs/retrospectives/refresh-claude-md-and-encode-tidy-convention.md`** — retro per the format conventions. Light (this is routing-layer refresh + convention encoding, not skill-fixing or methodology-discovery).

6. **`docs/retrospectives/README.md`** — add retro entry under `Multi-artifact retros (root)`.

---

## Decisions to flag during the session

1. **Where the spec-delta forward reference lives in CLAUDE.md.** Lean: at the end of the Session Workflow section as a standalone paragraph, with a clear "in flight" framing and a pointer to the pending encoding prompt. Alternative would be inline within the per-slice loop description — but the discipline applies across phases, so a separate paragraph reads cleaner. Chosen: standalone paragraph.

2. **Wolverine version cell phrasing.** Three candidates: (a) just the current version (5.39+); (b) just the historically-significant floor (5.32+); (c) both with framing. Lean: (c) — "5.32+ floor (the gRPC release that motivated this project); currently on 5.39+". The historical floor explains *why* CritterCab exists; the current version is operationally correct. Chosen: (c).

3. **Whether to add an explicit `Status` heading to CLAUDE.md to mirror README.md.** Lean: no. CLAUDE.md is a routing layer; status belongs in one sentence at the top, not in a section. The fix is a one-line replacement, not a new section. Chosen: one-line replacement.

4. **Whether the `tidy:` subsection should reference specific PR numbers.** Lean: reference the *convention* (areas, what `tidy:` is and isn't), not specific PRs. PR numbers are operationally fragile (rebases, merges); the convention is durable. Chosen: areas-only.

5. **Whether to lift the per-layer README pointer list out of Session Workflow into its own section.** Lean: keep inside Session Workflow as a sub-list — the pointers are operational manuals for the workflow steps, so they belong adjacent to the workflow definition. Chosen: keep as sub-list.

---

## Out of scope

- **`docs/vision/README.md` v0.3 → v0.4 bump.** The vision doc itself has drift (BC section still says v0.1; ADRs 010–015 unreflected). Out of scope here — was already scoped to land in the [context-map-foundation session](./context-map-foundation.md). Do not lift into this PR.
- **Spec-delta encoding session.** That's [`encode-spec-delta-closure-loop.md`](./encode-spec-delta-closure-loop.md)'s job. This session only adds a *forward reference* in CLAUDE.md to that pending session.
- **Context-map artifact authoring.** That's [`context-map-foundation.md`](./context-map-foundation.md)'s job. This session only mentions the pending artifact in CLAUDE.md's Session Workflow.
- **Updates to other per-layer READMEs.** The narratives README, retrospectives README, workshops README, and skills README all remain as-is. No drift in those was surfaced by the audit.
- **Restructuring the existing CLAUDE.md sections** beyond the three drifts named. Architectural Non-Negotiables, Context7, External Skills + Precedence overrides, and Do Not sections all stay as-is.
- **Phase 6 placeholder cleanup, ai-skills lean-out, any workshop authorship** — long-term out-of-scope.

---

## Retro emphasis

Light. This is routing-layer refresh + convention encoding, not methodology discovery. The retro should capture:

- Whether the two-phase pipeline rewrite reads cleanly when integrated with the surrounding CLAUDE.md prose, or whether it feels like a foreign body grafted in.
- Whether the spec-delta forward reference holds its weight given the encoding session hasn't shipped, or whether it should have been deferred until after that session.
- Whether any drift surfaced during the edit pass that wasn't in the original audit scope (kept for a follow-up tidy session, not lifted into this PR).
- Confirmation that the `tidy:` convention as encoded matches what PRs #7–#10 actually established (not retconned).

Probably no new methodology refinements emerge — this is mechanical encoding. If new refinements do surface, capture them as inputs to a follow-up session.
