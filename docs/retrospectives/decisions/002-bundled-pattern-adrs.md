# Retrospective — Bundled Pattern + NFR ADR Authorship (011–015)

## Metadata

- **Triggering prompt:** [`docs/prompts/decisions/002-bundled-pattern-adrs.md`](../../prompts/decisions/002-bundled-pattern-adrs.md)
- **Status:** Complete
- **Date authored:** 2026-05-10
- **Output artifacts:**
  - `docs/decisions/011-configuration-as-events-bootstrap.md` — new ADR
  - `docs/decisions/012-aggregate-per-invariant.md` — new ADR
  - `docs/decisions/013-shared-cross-bc-identifier.md` — new ADR
  - `docs/decisions/014-asb-topic-naming-convention.md` — new ADR
  - `docs/decisions/015-driver-app-projection-timing-budget.md` — new ADR
  - `docs/decisions/README.md` — index updated with five new rows
  - `docs/workshops/001-dispatch-event-model.md` — §11 cross-references updated; bumped v0.3 → v0.4
  - `docs/workshops/002-trips-event-model.md` — §12 cross-references updated; bumped v0.11 → v0.13 (catches up a pre-existing v0.11/v0.12 inconsistency in the status header)
- **Outcome:** Five ADRs at status `Accepted` covering four cross-BC patterns (configuration-as-events bootstrap, aggregate-per-invariant, shared cross-BC identifier, ASB topic naming) and one cross-cutting NFR (driver-app projection timing budget). Workshop cross-references resolved from "candidate #N" placeholders to "ADR-0NN" links.

---

## Framing

Workshop 001 §11 (2026-04-24) named eight ADR candidates with explicit triggers. Four of those triggers fired during Workshop 002 (2026-05-09) — #5 (config-as-events bootstrap), #6 (aggregate-per-invariant), #7 (shared cross-BC identifier), #8 (ASB topic naming). Workshop 002 also surfaced a new candidate (driver-app projection timing budget) at slice 6.1 that was structurally an NFR rather than a pattern. The two-BC codification threshold meant the pattern ADRs were ready for authoring with high confidence; the NFR was ready with one substantive open question (the numeric SLO target). This session paid down the accumulated debt in one bundled PR per Workshop 001 §12.6 #4.

---

## Outcome summary

Five ADRs authored in 011 → 015 order, each at ~50–70 lines, each pausing for explicit sign-off before commit:

| ADR | Status | Shape | Open question? |
|---|---|---|---|
| 011 — Configuration-as-Events Bootstrap | Accepted | Three-option (migration-seed chosen) | Yes (bootstrap mechanism); lean pre-confirmed at session start |
| 012 — Aggregate-per-Invariant | Accepted | Two-option (invariant-driven chosen) | No (pure codification) |
| 013 — Shared Cross-BC Identifier | Accepted | Two-option (shared canonical chosen) | No (pure codification) |
| 014 — ASB Topic Naming Convention | Accepted | Three-option (source-bc.event-name-kebab chosen) | No (pure codification) |
| 015 — Driver-App Projection Timing Budget | Accepted | Three-option (p95 < 200ms single-region chosen) | Yes (SLO target); lean pre-confirmed at session start |

Workshop cross-references resolved (W001 §11 items #5/#6/#7/#8 and W002 §12 new candidate + four inherited). ADR index updated. No code changes, no protobuf changes.

---

## What worked

- **The "lean before draft" loop, per `feedback_communication_depth_ubiquitous_language.md` memory.** ADRs 011 and 015 had load-bearing open questions; surfacing the lean writeup *before* authoring the full ADR kept the session on rails. Pre-confirmation of both leans at session start (carried over from the previous session's hand-off notes) meant the leans did not need re-litigating mid-session.
- **Two-BC codification cadence paid off.** Each pattern ADR landed at `Accepted` with high confidence because the W001 ↔ W002 evidence was consistent in shape (singleton aggregate + full-replacement semantics for config-as-events; sub-entities vs. sub-states distinction for aggregate-per-invariant; round-trip ID continuity for shared identifier; seven topics matching the convention for ASB naming). If any pattern had been one-BC-only, the corresponding ADR would have been `Proposed` rather than `Accepted` — the discipline produced confidence, not just artifacts.
- **Voice match to ADR-005 across all five ADRs.** Paragraph-form prose, three-option (or two-option) shape with rationale per option, Decision section that names the choice and rationale, Consequences enumerated. ADR-008's two-option precedent enabled ADRs 012 and 013 to stay tight without forcing synthetic third options.
- **Bundling rationale held throughout.** ADR-013 and ADR-014 reference each other (session keying on the canonical ID is what produces joint cross-BC ordering); authoring them in one session kept the cross-reference consistent without retrofit. Workshop §11/§12 updates landed atomically with the ADRs in the same PR.
- **Skill-file gap discipline applied uniformly.** ADR-011 flagged a `migration-bootstrap` skill gap; ADR-015 flagged an inline-vs-async projection-placement skill gap. Both were named in the ADR's Consequences and parked for DEBT.md ledger entry, per the prompt's "no skill-file authoring in this session" scope rule. No scope creep into skill authoring.
- **Reactive context-expansion on user request.** When the user paused mid-session to ask the provenance question on ADR-011 ("how did we get to this point?"), the layered explanation (Bruun origin → CritterCab adoption pattern → comparison to real ride-sharing platforms → why bootstrap is the seam this ADR addresses) produced a richer answer than the ADR would otherwise have carried. Two of the layers (Bruun attribution + carry-the-value structural relationship; the "seam exists because we chose event sourcing" framing) were folded back into ADR-011's Context section as durable enhancements.

---

## What was harder than expected

- **User signaled mental block mid-session.** During the branch-naming step and again after ADR-011 sign-off, the user asked the session-runner to take more initiative ("you can go ahead and decide on the branch's name; I am mentally hitting a block"). Cadence shifted toward driver-mode for branch-naming and pre-confirmation of subsequent sign-offs, while preserving the per-ADR pause-for-content-sign-off discipline. The shift worked but is worth recording as a methodology pattern (next entry).
- **W002 status header was out of sync with Document History.** The header read `v0.11` while a `v0.12` entry already existed in the history (added during the W001 §5.12 amendment session that landed in PR #12). Caught when touching the file for this session's `v0.13` bump. Fixed in one move — bumped the header directly to `v0.13`, with a note in the new history entry explaining the catch-up. Per `feedback_keep_readmes_current.md`'s discipline of fixing surfaced inconsistencies in the same session that touches the file.
- **The provenance question forced a larger context-surfacing turn than the prompt anticipated.** The prompt's working pattern assumed each ADR's open question would be resolved with a lean-surfacing turn; the user instead asked for the *origin story* of the configuration-as-events pattern. The answer (~2000 words across five layered sections) was longer than the prompt's per-ADR budget but produced durable enhancements to ADR-011 and demonstrated that the explanatory output style's depth is appropriate when explicitly invited.

---

## Methodology refinements that emerged

- **"Lean before draft" applies to ADRs with open questions; pure codification ADRs can be written directly.** ADRs 011 and 015 had load-bearing open questions and benefited from lean-surfacing before authoring. ADRs 012, 013, and 014 were pure codifications of two-BC-confirmed patterns; surfacing leans for them would have been ceremony, not signal. The discipline should be: *only surface a lean when there is a real choice to be made*. Otherwise, draft the ADR directly and pause for content sign-off.
- **When a user signals mental block, switch to driver-mode within stated guardrails.** Branch-naming, commit messages, and authoring choices can be the session-runner's call when the user defers. Sign-offs on durable artifacts (ADR content, commits to main-targeting branches, PR creation) remain user-confirmed. The boundary is "things that change the repo state durably" vs "things that are bookkeeping or in-session choices."
- **Workshop status header vs. Document History inconsistency is a tidying-in-session opportunity.** When a session touches a workshop file and the header is stale relative to history, fix it in the same session rather than leaving the inconsistency. The catch-up is recorded in the new history entry for transparency.
- **The narrative → workshop → ADR pipeline produces forward-anticipation that pays off at ADR authorship time.** Concrete example from this session: W002 §6.1's `dispatchAssignedAt` / `matchedAt` timestamp pair was designed in anticipation of ADR-015's SLO observability; the workshop authors built the dual-timestamp shape into the event payload *before* the ADR existed. ADR-015 then makes the anticipation pay off by promoting `MatchingLatencyMetrics` from "defer but pin" to "must-author when implementation starts." This is a coherence property of the layered design phase worth naming as a deliberate pattern, not just a happy accident.
- **The "ADR backs an already-practiced workshop discipline" move is a quiet but durable benefit of two-BC codification.** ADR-012's Consequences section explicitly notes that W001 §12.6 already recommended a "pre-workshop sidebar on aggregate identity" as a future-workshop convention, and W002 followed that recommendation before ADR-012 existed. ADR-012 elevates the convention from "recommendation in a workshop retrospective" to "ADR-backed project discipline." Future workshops inherit the discipline from `docs/decisions/`, not from the W001 retrospective they may not read.

---

## Outstanding items / next-session inputs

- **Skill-file gaps for DEBT.md ledger.** Two skill files flagged but not authored in this session per scope:
  - `migration-bootstrap` — codifying the idempotent-guard + seed-event migration template for ADR-011's pattern. To land when the first BC's migration is written during implementation.
  - Inline-vs-async projection placement skill — codifying latency-criticality decision criteria, server-streaming push patterns, Marten inline-projection idioms with Wolverine handlers (ADR-015). To land alongside the first latency-critical projection implementation.
- **ADR-015 multi-region target deferred.** Cross-region SLO will require either amending ADR-015 or superseding it when cross-region deployment lands on the roadmap. No action until that point.
- **ADR-candidates from W001 §11 remaining open.** Items #1–#4 (service-topology, fan-out-topology, candidate-projection-ownership, pricing-location) remain candidates with explicit triggers; this session intentionally did not author them because the prompt's scope was the four "fired across two BCs" candidates plus the new NFR. Future ADR-authoring sessions tackle them as their triggers fire.
- **Identity, Pricing, Ratings, Payments workshops still pending.** Each will produce new ADR candidates as the modeling reveals patterns; the two-BC codification cadence then triggers ADR authorship for any pattern that recurs across BCs.

---

## Quantitative summary

- **5 ADRs authored** (avg ~58 lines per ADR; range 49–70).
- **9 commits** on the branch (5 ADR commits + ADR index + W001 §11 + W002 §12 + retrospective).
- **3 documentation files updated** beyond the new ADRs: `docs/decisions/README.md`, `docs/workshops/001-dispatch-event-model.md`, `docs/workshops/002-trips-event-model.md`.
- **2 skill-file gaps** recorded for DEBT.md ledger follow-up.
- **0 code changes**, **0 protobuf changes**, per the prompt's scope rule.
- **Session interaction shape:** five sign-off cycles (one per ADR) plus three companion-edit cycles (index, W001, W002); one extended provenance-context turn (mid-session, on the user's request); one driver-mode block-handling adjustment (branch-naming + pre-confirmed leans).
