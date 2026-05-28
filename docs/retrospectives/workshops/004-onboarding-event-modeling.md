# Retrospective — Workshop 004 Onboarding Event Model

| Field | Value |
|---|---|
| **Triggering prompt** | [`docs/prompts/workshops/004-onboarding-event-modeling.md`](../../prompts/workshops/004-onboarding-event-modeling.md) |
| **Status** | Complete |
| **Date** | 2026-05-27 |
| **Output artifacts** | [`docs/workshops/004-onboarding-event-model.md`](../../workshops/004-onboarding-event-model.md) (new — first EM event model for Onboarding; first in-repo Process Manager via Handlers reference implementation); this retro (new — second entry in `retrospectives/workshops/`); [`docs/research/methodology-log.md`](../../research/methodology-log.md) (entry 005 — DS-as-upstream efficacy); [`docs/workshops/README.md`](../../workshops/README.md) (W004 list row + W003 §5.3 follow-up closure + new W004 follow-ups subsection + W002 §14.8 stale-ADR-row tidy); [`docs/retrospectives/README.md`](../README.md) (index entry); [`docs/context-map/README.md`](../../context-map/README.md) (v0.2 → v0.3 — edge #6 supplier-side lock + lifecycle-start resolution, new §7 Onboarding → Operations / Notifications edges, Notifications inventory-drift flag) |
| **One-line outcome** | First DS-fed Event Modeling workshop in CritterCab and first in-repo Process Manager via Handlers instance — 21 events across 11 slices, all 14 W003 W004-scoped open questions dispositioned, two new paired ADR candidates surfaced, and a confirmed finding that DS-as-upstream materially raises EM-artifact quality for vocabulary-rich BCs. |

---

## Framing

W004 is CritterCab's third Event Modeling workshop and fourth design-phase artifact (after W001 EM, W002 EM, W003 DS). Two firsts converge in this session: it is the **first EM workshop to consume a Domain Storytelling artifact as its primary upstream input** (W001 and W002 consumed narratives) and the **first CritterCab instance of the Process Manager via Handlers pattern** — sibling to W001's parent-with-sub-entity and W002's single-aggregate-full-lifecycle aggregate-cluster patterns. It closes the W003 §5.3 "Workshop 004 — Onboarding event model" follow-up and returns to the design phase per ADR-004's design-return cadence after the W003 PR.

The prompt was unusually heavily prepared: four `grill-with-docs` resolutions (committed across branch commits before the session ran) locked the substance ahead of time — Process Manager via Handlers (grills #2/#4), the `applicationId == driverProfileId` shared canonical UUIDv7 (grill #1), the OQ-10 split into a/b/c/d/e with the Notifications forward-constraint (grill #3), and two separate paired §11 ADR candidates per CritterCab's one-decision-per-ADR convention (grill #4). The session's job was to *exercise* these locks slice-by-slice, not relitigate them.

---

## Outcome summary

- **One workshop event model authored** (`004-onboarding-event-model.md`, v0.9) — §2 Scope (Option A locked), §3 Modeling-pattern + ApplicationState sidebar (Process Manager via Handlers), §4 Ubiquitous Language, §5 Event List (21 events), §6 Slice Walk (10 base slices + 1 sub-slice paired), §10 Protobuf surface, §X DS findings handled, §X+1 Forward-constraints generated, §11 ADR Candidates, §12 Retrospective.
- **First in-repo Process Manager via Handlers reference implementation.** `ApplicationState` process state; four scheduled timeouts via `OutgoingMessages.Delay` with let-state-decide resolution; start-handler asymmetry (`MartenOps.StartStream`, not `[AggregateHandler]`); 2N guard discipline on every continue handler.
- **All 14 W003 W004-scoped open questions dispositioned** in §X "DS findings handled" (the new convention for DS-fed EM workshops, parallel to W002 §13's forward-constraints-handled for narrative-fed workshops). Plus all 8 V-items + 7 B-findings + 1 cross-cutting pattern handled.
- **12 forward-constraints generated** across 5 target BC workshops (Identity, Driver Profile, Notifications, T&S, Operations).
- **4 locked ADRs gained evidence** (011 third-BC, 013 fourth-participant, 014 third-instance, 015 second-instance). **ADR-012 honestly NOT a new evidence point** — reframe documented (Onboarding is PM via Handlers, not an aggregate-cluster).
- **2 new paired ADR candidates surfaced** (Process Manager via Handlers as third CritterCab modeling pattern; multi-vendor ACL absorbed inside it) + 1 sub-discipline candidate (push-vs-pull cross-BC consumption).
- **Methodology log entry 005 written** (Q1 — DS-as-upstream efficacy).
- **Notifications BC inventory-drift flagged** — not in the vision-doc tentative-BC list; surfaced as a workshop-need-trigger via forward-constraint #6.

---

## What worked

- **The §3 modeling-pattern sidebar (replacing W002's aggregate-identity sidebar) applied cleanly.** Settling the *pattern* (PM via Handlers) before any state-machine substance shaped every subsequent slice. The "three framings considered, two rejected" structure (plural aggregates / sub-entity-on-Application / PM via Handlers) is a reusable shape for future pattern-selection sidebars — captured in §12.4 as a new meta-decision.
- **DS-as-upstream front-loaded all vocabulary work.** Zero mid-walk vocabulary scrambles, in sharp contrast to W002's three-iteration first-state-vocabulary scramble (`Intaken → Confirmed → Matched` requiring mid-sidebar web research). W003's §5.1 V1–V8 covered every internal disambiguation; the pre-walk scan confirmed zero W001/W002 collisions. See §Methodology Q1.
- **Let-state-decide for scheduled timeouts proved the cleanest ergonomic win of PM via Handlers.** Slice 6.7b's FCRA dispute-window timeout is the canonical demonstration — no cancel-the-timer API, state authoritative, handler a pure function. Materially simpler than the rejected Bruun-as-mechanism framing the prompt grilling had set aside.
- **Paired walks (6.4+6.4b, 6.5+6.6, 6.7+6.7b) accelerated cadence without losing rigor** — confirming W002 §14.6 #3's paired-walk portability across a third workshop.
- **Multi-vendor ACL discipline transferred verbatim from Slice 6.4 to 6.5** (vendor reason → CritterCab enum at boundary; opaque vendor case ID; Klefter decision-event recording), shrinking the second vendor-edge slice substantially.
- **Atomic N-emit scaled smoothly** — W001 §5.5 (dual) → W002 §6.6 (triple) → W004 §6.7b/§6.8 (quadruple). The Wolverine outbox guarantee held the discipline constant as N grew with semantic richness.
- **The honest ADR-012 reframe.** Resisting the temptation to count Onboarding as a third aggregate-per-invariant data point — and instead surfacing PM via Handlers as a distinct pattern — kept ADR-012's evidence base honest while growing CritterCab's modeling-pattern repertoire by one. This is the kind of move that's easy to fudge for tidiness and worth getting right.

---

## What was harder than expected

- **The session ran long** — the largest workshop artifact in CritterCab to date. PM-via-Handlers workshops produce more events + more scheduled timeouts + more translation edges than aggregate-cluster workshops. The paired-walk cadence was essential, not optional. Lesson: future PM workshops (Operations, T&S, possibly Payments) should budget for similar length and lean hard on paired walks.
- **One friction the Wolverine guide doesn't capture surfaced at Slice 6.5.** When an external vendor's webhook doesn't carry the process stream's ID, the PM pattern needs an *additional* inline projection to route the webhook (`BackgroundCheckVendorCaseIndex`, silent dependency #9 in §3.12). The guide names the inline-snapshot-projection dependency (friction-point #7) but not the additional routing projections that multi-vendor-webhook integration requires. The guide's single-process OrderFulfillment sample doesn't exercise external-vendor-webhook routing. See §Methodology Q2.
- **The distributed-completion-logic friction (guide §5 #2) bit harder than the guide's three-gate sample suggests.** Onboarding has four parallel approval-gates; the gate-observation predicate appears across multiple continue handlers. The §3.12 `state.AllApprovalGatesSatisfied` refactor candidate addresses it, but the friction is real at four gates.
- **`ApplicantNotifiedOfApproval` cross-BC publication ended named-but-deferred** (Slice 6.8). The third onboarding outbound topic (`onboarding.applicant-approval-notification-required`) was consciously left as a loose end rather than fully resolved.
- **The DS-as-upstream finding is confounded by prompt-grilling.** Much of W004's smoothness traces to four prompt-authoring grills, not DS-as-upstream alone. The honest Q1 finding had to disentangle the two contributions. The confound doesn't undermine the finding (the DS contribution is isolable — the vocabulary front-loading) but it means W004 alone can't fully isolate the DS-vs-narrative variable.

---

## Methodology refinements that emerged

- **§X "DS findings handled" is now the convention for DS-fed EM workshops** — parallel to W002 §13's forward-constraints-handled for narrative-fed workshops. The four-category walk (BC findings / vocabulary items / open questions / cross-cutting pattern) is the reusable structure.
- **Modeling-pattern-selection sidebar (three-framings-considered structure)** for BCs that aren't aggregate-cluster-shaped. The §3 sidebar's job differs from W001/W002's aggregate-identity sidebar: it settles the *pattern* first.
- **Bruun-pattern-as-projection ≠ Bruun-pattern-as-mechanism.** A temporal automation can use a Bruun todo-list for *ops visibility* (`AdverseActionDisputeWindow*`) while using PM-self-scheduled-message + let-state-decide for the *mechanism*. The distinction matters when choosing PM via Handlers over an aggregate-cluster — and is a refinement on the Bruun pattern as W001/W002 used it.
- **Push vs. pull cross-BC consumption discipline** (Slice 6.9). Push for cross-BC workflow continuation (DP activation, FCRA delivery); pull for cross-BC information flow (T&S learning terminal facts for pattern detection). The PM-via-Handlers approach makes pull-via-projection natural because the process stream is canonical.
- **When a vendor webhook doesn't carry the process stream's ID, name the routing-index inline projection early** (in the sidebar's friction-point enumeration, not mid-walk). W004 discovered `BackgroundCheckVendorCaseIndex` at Slice 6.5; a future PM workshop should anticipate it.

---

## Outstanding items / next-session inputs

- **2 new paired ADR candidates** ready for follow-up authorship: Process Manager via Handlers (third CritterCab modeling pattern) + multi-vendor ACL absorbed inside it. Trigger: first Onboarding implementation slice. The Q2 webhook-routing friction is an input to the Process Manager ADR.
- **Onboarding business-event Protobuf authorship** — `DriverApproved`, `AdverseActionNoticeRequired` (+ named-but-deferred `ApplicantApprovalNotificationRequired`) under `/protos/crittercab/onboarding/v1/`; plus the forward-constraint that Identity authors `DriverRegistered`. PR #4 precedent; candidate bundled-proto authorship session.
- **12 forward-constraints** across 5 target BC workshops (Identity, Driver Profile, Notifications, T&S, Operations) — see workshop §X+1. Each is an input to that BC's eventual workshop.
- **Notifications BC inventory drift** — not in the vision-doc tentative-BC list. Vision-doc inventory-drift candidate for a future tidy session (parallel to the T&S inventory-drift flag already in the context map's §Pending workshops). W004 deliberately did not bump the vision doc on this point.
- **Parking-lot items:** sensitive-PII vault implementation (6.3); document retry-budget enforcement (6.4b); Suspended-status applicant-action flow (6.5); dispute-filing flow (6.7b); adjudicator workforce-identity authority model (6.6); re-application policy (OQ-11); vehicle-as-sub-domain (OQ-13).
- **DEBT.md follow-ups:** per-vendor reason-code mapping table; blob-storage handle resolution skill.
- **Onboarding narrative authoring** — the natural successor session for the per-slice implementation loop, threading W004's slices into a journey-scoped NDD narrative.

---

## Methodology questions — answering the prompt's Q1 + Q2

The prompt named two focused methodology questions alongside the standard nine-subsection close-out. The full answers live in the workshop's §12.10; summarized here for the session-level record.

### Q1. Did DS-as-upstream produce materially higher-quality EM artifact than narratives-only would have?

**Yes, materially — with one honest confound.** Evidence for "yes, materially": zero mid-walk vocabulary scrambles (vs. W002's three-iteration first-state scramble + web-research turn); all 7 B-findings encoded with zero fresh debate; all 14 OQs answerable with leans traced to W003 findings; the multi-vendor ACL cross-cutting pattern gave W004 a ready-made architectural framing for the §3 sidebar's "what was rejected" reasoning. The confound: much of the smoothness traces to four prompt-authoring grills, not DS-as-upstream alone. Disentangled — DS produced a richer vocabulary-and-findings surface than narratives would have (the W003-retro-Q2 vocabulary-divergence findings, H4/H5/multi-vendor-ACL/B2, were invisible from vision-doc reading), AND the grilling amplified it into a low-friction walk. Both contributed; the DS contribution (vocabulary front-loading) is real and isolable. **Implication: DS earns a place as a default pre-EM step for vocabulary-rich BCs.** This drove methodology log entry 005.

### Q2. Did PM-via-Handlers fit Onboarding's shape as predicted, or surface friction the guide doesn't capture?

**Fit as predicted, with one W004-specific friction the guide doesn't capture.** Let-state-decide felt materially cleaner than the rejected Bruun-as-mechanism framing; `ApplicationState` was richer than the `OrderFulfillmentState` sample but the richness is honest domain complexity; distributed-completion-logic and start-handler-asymmetry frictions materialized as the guide predicted. The uncaptured friction: vendor-webhook routing requires an additional inline projection when the external system doesn't carry the process stream's ID (`BackgroundCheckVendorCaseIndex`). **This friction lives in the §11 Process Manager via Handlers ADR candidate** (pattern-specific, so future PM workshops find it there) rather than in a methodology log entry — per the log's own "when to write an entry" criteria favoring cross-cutting *process* observations over pattern-specific frictions. Methodology log entry 006 was deliberately NOT written.

---

## Spec delta — landed?

**Yes — landed substantially as named.** The prompt's [§Spec delta](../../prompts/workshops/004-onboarding-event-modeling.md) named the session as spec-creating for Onboarding's event model, spec-confirming for W003's DS findings, and pattern-introducing for CritterCab. All five named landings landed:

| Prompt prediction | Landed? | Notes |
|---|---|---|
| `docs/workshops/004-onboarding-event-model.md` created | ✅ | First event model for Onboarding; first PM-via-Handlers in-repo instance. |
| W003 §5.3's 14 W004-scoped OQs answered or explicitly deferred in §X | ✅ | All 14 dispositioned; OQ-2/OQ-3/OQ-4 resolved to "events on the process stream with correlation IDs" as predicted. |
| `docs/workshops/README.md` § Workshop follow-ups closes the W003 §5.3 row | ✅ | Closed with link to the W004 artifact; new W004 follow-ups subsection added. |
| Locked ADRs gain evidence (011 third-BC, 012 honest-not-a-third-point, 013 fourth-participant, 014 third-instance, 015 softer-budget) | ✅ | All as predicted, including the ADR-012 honest reframe. |
| Two new paired §11 ADR candidates (Process Manager + multi-vendor ACL, separate paired ADRs) | ✅ | Both surfaced per grill #4's separate-paired-ADRs decision. |
| One new forward-constraint on Notifications BC (FCRA notices → ASB) | ✅ | `onboarding.adverse-action-notice-required` per OQ-10d / grill #3. Plus 11 other forward-constraints across 5 BCs. |
| No existing spec amended retroactively | ✅ | W001/W002/W003 stay locked; W004 honors/overrides via documented §X reasoning. **Same-PR tidy of W002 §14.8 stale ADR-row index entries is in-bounds** (same-file-edit rule; ADRs 011–015 authored) — done in the README, not by editing W002 itself. |

One named-but-deferred item: the `ApplicantNotifiedOfApproval` third onboarding outbound topic (Slice 6.8) was consciously left unresolved rather than committed — a small divergence from a fully-closed slice walk, flagged honestly in §12.3.

**Context-map amendment landed in the same PR** (v0.2 → v0.3) — the **second exercise** of the context-map's [§Update cadence](../../context-map/README.md#update-cadence) convention (W003 was the first). W004 touched three cross-BC relationships: edge #6's lifecycle-start question resolved (approval-push), plus two new dashed edges (Onboarding → Operations, Onboarding → Notifications). Per the cadence convention, these amend the artifact in the same PR. The amendment is consistent with the prompt's deliverable-plan "if applicable" qualifier and with the §Update cadence convention's explicit "workshops touching cross-BC relationships amend in the same PR" rule — both pointed the same way. The "honest no-context-map-impact is a fine outcome" caveat from the prompt did not apply because W004 genuinely had cross-BC-edge impact.

---

## Quantitative summary

| Metric | Count |
|---|---|
| Slices walked | 10 base + 1 sub-slice paired (6.4b) |
| Events committed | 21 |
| Scheduled timeouts (let-state-decide) | 4 (FCRA dispute window, abandonment, vendor no-response, adjudicator claim) |
| Multi-vendor ACL translation edges realized | 3 (OIDC inherited per ADR-006, document-verification, background-check) + 1 human-actor translation surface (adjudicator) |
| Cross-stream multi-stream projections | 1 (`AdjudicatorQueueView*` — first in CritterCab) |
| W003 OQs dispositioned | 14 (all W004-scoped) |
| W003 V-items handled | 8 |
| W003 B-findings handled | 7 |
| Forward-constraints generated | 12 (across 5 target BC workshops) |
| Locked ADRs gaining evidence | 4 (011, 013, 014, 015) |
| Locked ADRs explicitly NOT new evidence | 1 (012 — honest reframe) |
| New paired ADR candidates | 2 (Process Manager via Handlers + multi-vendor ACL) |
| Sub-discipline candidates | 1 (push-vs-pull cross-BC consumption) |
| Protobuf contracts named (deferred authorship) | 3 onboarding outbound + 1 identity inbound forward-constraint |
| Parking-lot items | 7 |
| DEBT.md follow-ups | 2 |
| Methodology log entries written | 1 (entry 005 — Q1) |
| Methodology log entries deliberately not written | 1 (entry 006 — Q2 friction lives in §11 ADR candidate) |
| New feedback memories warranted | 0 (all applicable already in MEMORY.md) |
| Sign-off gates | ~14 (scope, vocab scan, sidebar, UL+ordering, six slice gates, four close-out sections, retro) — all signed off "as drafted" |
| Workshop artifact final version | v0.9 |
