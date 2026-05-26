# Prompt 004 — Fourth Event Modeling Workshop (Onboarding)

| Field | Value |
|---|---|
| **Status** | Authored (not yet run); session pending |
| **Authored** | 2026-05-26 |
| **Target artifact** | `docs/workshops/004-onboarding-event-model.md` |
| **Companion artifacts** | [`docs/workshops/README.md`](../../workshops/README.md) (Workshops list + § Workshop follow-ups — close "Workshop 004 — Onboarding event model" row from W003 §5.3); conditionally [`docs/research/methodology-log.md`](../../research/methodology-log.md) (entry 005 if warranted); conditionally [`docs/context-map/README.md`](../../context-map/README.md) (further edge refinements if W004 surfaces them) |
| **Source-of-truth dependencies** | [`docs/workshops/003-onboarding-domain-story.md`](../../workshops/003-onboarding-domain-story.md) (the canonical DS input — three stories, 7 BC findings, 8 vocabulary items, 14 W004-scoped open questions, 1 cross-cutting pattern); [`docs/research/ride-sharing-driver-onboarding-domain-note.md`](../../research/ride-sharing-driver-onboarding-domain-note.md) (frozen Phase 0 grounding from W003; reused as-is per the prompt-authoring decision); [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) and [`docs/workshops/002-trips-event-model.md`](../../workshops/002-trips-event-model.md) (shape conventions and methodology refinements); ADRs [011](../../decisions/011-configuration-as-events-bootstrap.md) / [012](../../decisions/012-aggregate-per-invariant.md) / [013](../../decisions/013-shared-cross-bc-identifier.md) / [014](../../decisions/014-asb-topic-naming-convention.md) / [015](../../decisions/015-driver-app-projection-timing-budget.md) (all locked; W004 applies, does not relitigate); [`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) (locked Identity boundary; W003 §5.1 surfaced the multi-vendor ACL pattern as a §11 candidate generalizing this) |
| **Workflow position** | Third Event Modeling workshop; fourth design-phase artifact overall (after W001 EM, W002 EM, W003 DS). First workshop to consume a DS artifact as its primary upstream input rather than narratives. Closes the W003 §5.3 "Workshop 004 — Onboarding event model" follow-up; provides the third canonical data point for ADR-012 (aggregate-per-invariant) and ADR-013 (shared cross-BC identifier). Returns to the **design phase** per ADR-004's design-return cadence rule after the W003 PR. |

---

## Spec delta

This session is **spec-creating** for Onboarding's event model, **spec-confirming** for W003's DS findings, and **spec-amending** for W001 v0.4's ADR-candidate inheritance pattern (W004 is the third workshop in the lineage).

- **`docs/workshops/004-onboarding-event-model.md` is created** — first event model for the Onboarding BC. Inherits W003's three-story spine as slice ordering; commits event names, command shapes, aggregate boundaries, projection candidates, and Translation-slice contracts where W003 left vocabulary candidates.
- **W003 §5.3's 14 W004-scoped open questions are answered or explicitly deferred.** OQ-1 through OQ-13 (and OQ-14 as out-of-scope reference) get explicit dispositions in a §X "DS findings handled" section.
- **`docs/workshops/README.md` § Workshop follow-ups closes the W003 §5.3 row** for Workshop 004 with the closing artifact link; adds a new § "From W004 §X.8 Follow-ups" subsection if W004 generates fresh follow-ups.
- **Three locked ADRs may gain new evidence entries.** ADR-012 (aggregate-per-invariant) gains its third canonical data point via Onboarding's likely-plural aggregates (Application + AdjudicationCase + optionally BackgroundCheckCase). ADR-013 (shared cross-BC identifier) gains evidence on whether Onboarding uses Identity's `subjectId` as its cross-BC identifier or mints its own `applicationId`. ADR-014 (ASB topic naming) gains the third instance (`onboarding.driver-approved` joins `dispatch.ride-assigned` and Trips' four outbound topics).
- **One new §11 ADR candidate is expected** — the multi-vendor ACL pattern surfaced in W003 §5.1, generalizing ADR-006's OIDC-as-ACL stance to a project-wide pattern covering OIDC + document-verification + background-check vendors. Trigger to author: when a fourth vendor edge surfaces, or when Onboarding's first implementation slice forces the pattern to be concrete.
- **No existing spec is amended retroactively.** W001, W002, W003 stay locked; W004 honors or overrides via documented reasoning in §X "DS findings handled," not via edits to prior artifacts.

---

## Framing — why this session exists

W003 surfaced enough vocabulary disambiguation and BC-boundary confirmation to make event modeling for Onboarding low-risk on the language side. Three stories with 79 numbered steps, three actor types including the first manual-human reviewer in any CritterCab workshop, two new work-object types (adjudication case), and a cross-cutting multi-vendor ACL pattern — all give W004 a richer starting position than W001 or W002 had. W003's retrospective §Q2 explicitly named four vocabulary divergences that would have been invisible without the DS walk; W004 inherits the resolutions and turns them into event names.

W003 §5.3 enumerated 14 open questions with the explicit label "clear W004 scope" (OQ-1 through OQ-9 event-modeling concerns; OQ-10 through OQ-13 BC-boundary concerns). Closing them is W004's primary work. Five additional OQs (OQ-15 through OQ-19) are explicitly deferred to future sessions; OQ-14 is vision-doc-escalated. W004 honors these scope boundaries.

Three secondary jobs:

1. **Test the DS → EM input mechanism.** This is the first time CritterCab has run an event modeling workshop with a DS artifact as the primary upstream input (W001 and W002 consumed narratives). Methodology log entry 005 is conditional on observing whether DS produces materially higher-quality EM artifacts than narratives-only input, or whether the value was front-loaded in DS itself.
2. **Generate the third canonical data point for ADR-012.** Onboarding's aggregate-identity sidebar is the third workshop to exercise aggregate-per-invariant. If Onboarding's aggregates fall out cleanly as separate-by-invariant (Application + AdjudicationCase + possibly BackgroundCheckCase), the pattern is reinforced. If they collapse to one aggregate (e.g., Application owns everything as sub-streams), that's a counter-shape worth documenting.
3. **Pilot W004 §X "DS findings handled" as the upstream-input-handling section.** Parallel to W002's §13 "Forward-constraints handled" but adapted for DS's richer surface (findings, vocabulary, OQs, cross-cutting patterns). Convention establishes for future DS-fed EM workshops.

---

## Goal

Run CritterCab's third Event Modeling workshop end-to-end and produce a durable artifact for the **Onboarding bounded context**, structurally parallel to [`002-trips-event-model.md`](../../workshops/002-trips-event-model.md), with W003's three-story spine as the slice ordering and W003's 14 open questions as the explicit work agenda.

---

## Scope question to settle at session start

**Locked at prompt-authoring time to: Tight — exact mirror of W003's three stories.**

| Option | Span | Decision |
|---|---|---|
| **A. Tight (locked)** | W003 Stories 1–3: happy path, document-rejection recoverable, background-check terminal-rejection. Plus Translation-out to Driver Profile on approval. | **Locked.** Closes DS→EM loop with one-to-one fidelity. Symmetric with W002's Option B precedent. Likely 12–15 slices. |
| **B. Tight + re-application** | Adds OQ-11 resolution (re-application policy: waiting period, re-vetting, fresh aggregate vs. amended). | Deferred. Adds 2–4 slices; pushes scope into a question W003 explicitly deferred. |
| **C. Tight + vehicle sub-domain exploration** | Adds W003 B7 vehicle-as-sub-domain decision (multi-vehicle scenarios, per-vehicle approval lifecycle). | Deferred. Forces an aggregate-boundary call that warrants its own session or a dedicated vehicle BC workshop. |
| **D. Broad (full lifecycle + suspension)** | Adds OQ-14 escalation resolution (suspension / reinstatement / deactivation). | Out of scope. Vision-doc-level open question; trigger to resolve is first session modeling suspension. |

If during the walk a slice from Option B or C surfaces as load-bearing for W004's coherence, capture it as a parking-lot item rather than expanding scope.

---

## What this session inherits from W001 + W002 (no re-litigation)

The following are **locked** by prior workshops and the ADRs that followed them. Do not propose alternatives:

- **Workshop artifact shape** — Scope Statement, Aggregate Identity sidebar, Ubiquitous Language glossary, Event List, per-slice walkthrough, cross-reference tables (Translation slices, temporal automations, configuration-as-events, Protobuf surface), DS findings handled (new — §X), Parking Lot / Open Questions, ADR Candidates, Retrospective. See [`docs/workshops/README.md`](../../workshops/README.md) § Conventions plus the W002 §14.6 refinements below.
- **Slice notation conventions** — events past tense; commands imperative; views/projections named ubiquitously; automations as green stickies; temporal automations with the asterisk-suffix todo-list view per Bruun; clock-rewind glyph on time-driven automations.
- **Klefter decision-event pattern** — when a slice coordinates external systems AND a decision is made locally, promote the decision to a local event. Onboarding has multiple Klefter candidates: vendor verification responses (translated decisions), background-check Consider→reject adjudication (decision-event), FCRA window-expiry (temporal-automation-driven decision).
- **Bruun temporal-automation pattern** — todo-list read model with asterisk suffix, clock-rewind glyph on time-driven automations, configuration-as-events for tunable thresholds. Onboarding's primary candidate: FCRA dispute window expiry firing the final adverse-action notice (W003 OQ-6).
- **Configuration-as-events pattern (ADR-011)** — operator-tunable parameters consolidated into `*PolicyConfigured` events. Onboarding's candidate policy: vendor selection routing, FCRA dispute window length, document-rejection retry budgets, approval auto-flow thresholds. Likely an `OnboardingPolicyConfigured` slice. **Third BC to adopt; the ADR-011 pattern is locked.**
- **Aggregate-per-invariant (ADR-012)** — aggregates emerge from invariant boundaries, not from noun categories. Onboarding's plural-aggregate candidates (see §Aggregate-identity sidebar below) are the third canonical data point.
- **Shared cross-BC identifier (ADR-013)** — `tripId == rideRequestId` was the W001/W002 instance. Onboarding's question: does `applicationId` need to equal Identity's `subjectId`, or are they separate identifiers linked by reference? Locked pattern requires deliberate decision per BC; sidebar settles it.
- **ASB topic naming convention (ADR-014)** — `<source-bc>.<event-name-kebab>`. Onboarding's `onboarding.driver-approved` (W003 V1 / OQ-1) is the third instance after `dispatch.ride-assigned` and Trips' four outbound topics.
- **Driver-app projection timing budget (ADR-015)** — Trips' driver-app transition from offer-mode to trip-mode was the first instance. Onboarding's analogue: driver-app transition from "application pending" to "ready to drive" on approval. Likely a softer timing budget than Trips' (the user is not in the middle of an in-progress workflow) but worth flagging during the relevant slice.
- **Identity ACL boundary (ADR-006)** — provider-specific identity details do not appear in Onboarding events or projections. `subjectId` and `driverRegistered` are the available signals.
- **Per-workshop methodology refinements** — W001 §12.6 (all five) and W002 §14.6 (all five) carry forward. See §"What this session adopts as new practice" below.
- **§12.7 calibration** — pair open questions with leaning opinions; prefer informative depth over brevity; use ubiquitous language; assume DDD / CQRS / Event Sourcing / EDA as working background.

If any of these need to flex during the walk, that is a methodology log entry, not per-workshop re-debate.

---

## What this session adopts as new practice (per W001 §12.6 + W002 §14.6)

**W001 §12.6 — all five carry forward:**

1. **Proactive projections from slice 1.** Name candidate projections (read models, downstream consumer views) as flows are walked, from the very first slice. Onboarding's expected projections include `ApplicantDashboardView`, `AdjudicatorQueueView*` (todo-list), `OnboardingFunnelView` (operations metrics), `DocumentVerificationStatusView`.
2. **Pre-walk aggregate-identity sidebar** — see §Aggregate-identity sidebar below. Run before slice 1.
3. **Calibrate cadence to slice complexity.** Heavy slices (aggregate-identity decisions, FCRA two-phase notice + temporal automation, multi-vendor ACL translation slices) warrant extended discussion; lighter slices walked briskly.
4. **Defer Protobuf authorship.** Any new Onboarding business-event proto contracts (the `onboarding.driver-approved` event for Driver Profile and possibly others) defer to a follow-up authorship session under ADR-009.
5. **Number sub-slices explicitly when cascades occur** (5.5a / 5.5b convention).
6. **Pair temporal-automation slices with their feeder slice in narrative order.** FCRA dispute-window temporal automation pairs with the pre-adverse-action notice slice.

**W002 §14.6 — all five carry forward, with one substitution for the DS-as-upstream input:**

1. **Pre-walk vocabulary scan against prior workshops + industry conventions.** Run during the aggregate-identity sidebar. W003 §5.1 already did most of this work (V1–V8); W004's job is to confirm no new collisions with W001/W002 vocabulary. Watch specifically for: `Accepted` (W001 — offer accepted; W004 — document accepted), `Approved` (W004 — application approved vs. driver activated; three moments per V4), `Active` (W001 — active request; W002 — active trip; W004 — active driver, active application).
2. **Add §X "DS findings handled" section** — adapted from W002 §13 "Forward-constraints handled." Documents disposition (honored / overridden / partially-honored / deferred) for each of W003's 7 BC findings, 8 V-items, 14 OQs, and 1 cross-cutting pattern.
3. **Paired walks where structurally similar.** Onboarding candidates for pairing: the two document-verification slices (happy + rejection-recovery from W003 Stories 1 + 2); the two background-check slices (Clear + Consider→adjudicate from W003 Stories 1 + 3); the two notification slices (pre-adverse-action + final adverse-action under FCRA).
4. **Mid-walk forward-amendments via cross-reference.** Slice 6.12 in W002 amended slice 6.1 via cross-reference rather than retroactive edit. W004 may need this if (e.g.) the multi-vendor ACL pattern surfaces a need to amend earlier vendor slices.
5. **Mid-sidebar research grounding** when vocabulary or aggregate-identity decisions need industry baseline — W002 used Uber/Lyft/system-design literature for `Matched`. W004's likely candidates: per-document state-machine vocabulary, FCRA two-phase notice canonical names, adjudication-case lifecycle naming.

**One W004-specific new practice:**

- **Forward-constraints generated section** — W002 §14.6 #2 called for an explicit consolidation of *outbound* forward-constraints (on un-modeled BCs' future workshops). W003 already generated three for the Identity workshop (`identity.driver-registered`, `identity.rider-registered` from W002 was the second). W004 should consolidate its own outbound forward-constraints on Driver Profile's eventual workshop, on the (still-pending) Operations workshop if any adjudicator-tooling concerns surface, and on any future Trust & Safety workshop.

---

## Aggregate-identity sidebar (run BEFORE slice 1)

Per W001 §12.6 #2, the aggregate-identity decision shapes every subsequent slice. This workshop's sidebar serves a second purpose: it is the **third canonical data point for ADR-012 (aggregate-per-invariant)**. W001 named the pattern via `RideRequest` + `Offer` (parent + sub-entity); W002 confirmed with `Trip` (single aggregate, full lifecycle on one stream); W004 either confirms with plural aggregates (Application + AdjudicationCase + possibly BackgroundCheckCase) or surfaces a counter-shape.

**Candidate load-bearing invariants for Onboarding, with leans:**

1. **One application per actor at a time (idempotent intake).** A prospective driver cannot have two concurrent applications. Aggregate = `Application`, stream keyed by `applicationId`. Re-application after terminal rejection (OQ-11) is deferred; if eventually allowed, the question is fresh-aggregate-with-new-id vs. amend-rejected-aggregate (lean: fresh, but out of scope here).
2. **Application lifecycle monotonicity.** Application walks a state machine (opened → personal-info recorded → docs in progress → docs complete → BG check in progress → BG check complete → approved | rejected). No skipping, no reversing. If load-bearing, the application aggregate carries the lifecycle invariant.
3. **Per-document state-machine integrity.** Each document has its own lifecycle (under-verification → accepted | rejected → re-uploaded-replaces-rejected). W003 OQ-2 asks: sub-entity on Application (analogous to Offer on RideRequest) vs. separate `Document` aggregate referencing Application. **Lean: sub-entity on Application** — documents are not separately addressable from outside Onboarding, and the per-document lifecycle is a sub-machine of the application's docs-complete invariant.
4. **Adjudication-case integrity.** W003 Story 3 introduced `AdjudicationCase` as a new work-object. It has its own lifecycle (queued → under-review → adjudicated reject | adjudicated approve), references the Application + vendor findings, and has its own actor (Onboarding adjudicator). W003 OQ-4 asks: separate aggregate vs. sub-stream of the Application. **Lean: separate aggregate** — the adjudicator's review is a different invariant boundary (one human reviewer's decision, not the application's lifecycle), and the adjudication queue is a different consistency surface (operator tooling).
5. **Background-check case integrity.** The vendor's case has its own identifier (vendor case ID), its own async lifecycle (Pending → Clear | Consider | Suspended), and spans real time. **Lean: separate aggregate** referencing Application, with the vendor case ID as part of its identifier. Reason: the BG-check case is the natural anti-corruption surface for vendor-side webhooks and async status updates; collapsing it into Application would couple Application's invariant to the vendor's asynchronous timing.

**Lean for the sidebar's outcome:**

Three aggregates: `Application` (primary, multi-section state machine, owns documents as sub-entities), `AdjudicationCase` (separate, references Application by ID, holds the human-reviewer surface), `BackgroundCheckCase` (separate, references Application by ID, holds the vendor-async surface). The plural-aggregate shape is the third evidence point for ADR-012 — different from W002's single-aggregate `Trip` and parallel to W001's `RideRequest` + sub-entity `Offer` but more aggressive (sub-entity → separate-aggregate for two of the three concerns).

**Two related questions the sidebar must answer:**

- **What's split off entirely?** Identity's authentication / OIDC subject (separate BC, ADR-006). Driver Profile's post-activation lifecycle (separate BC; Onboarding hands off via ASB business event on approval). Vehicle as a separately-modeled sub-domain (W003 B7; lean: defer per scope decision A, treat vehicle as a document type for now). High-frequency operations metrics (Operations BC; Onboarding publishes counters but does not aggregate).
- **What's the cross-BC identifier?** ADR-013 requires deliberate choice. Onboarding's `applicationId` is internal. Identity's `subjectId` is the natural cross-BC handle for the actor. **Lean: `applicationId` is Onboarding-internal; `subjectId` carries across BCs.** When `onboarding.driver-approved` publishes, it carries `subjectId` (the new driver's stable identifier), `applicationId` (for audit), and possibly `driverProfileId` (if DP minted one at activation). Different from W001/W002's `tripId == rideRequestId` shape — Onboarding's is a *graduated handoff* (application identifier dies at approval; subject identifier persists across BCs).

The sidebar's output goes into the workshop artifact as §3 "Aggregate Identity" (mirroring W002 §3). Decision recorded with rationale and ADR-012 / ADR-013 evidence framing called out.

---

## DS findings inherited from W003 (the upstream input)

W003 is the primary input to W004 — a richer upstream than W002's narrative-pair input. Four categories of finding, each with explicit handling expectations:

### 1. BC findings (W003 §5.2 — 7 findings, B1–B7)

| Finding | W003 disposition | W004 expected handling |
|---|---|---|
| B1: Identity → Onboarding handoff is structurally consistent | Confirmed | Encode in Slice 1 (intake). Onboarding consumes `identity.driver-registered`. |
| B2: Onboarding → Driver Profile handoff gated by approval terminal, not BC | Confirmed; context-map edge #6 amended in W003 PR | Encode in Translation-out slice. **Rejection terminal has no DP publication** (asymmetric). |
| B3: Driver Profile lifecycle start unresolved | Deferred to W004 | W004 chooses one of three orderings (research note §6.2): application-start placeholder / approval push / lazy on first action. **Lean: approval push** (Onboarding emits, Driver Profile reacts and creates its aggregate). |
| B4: Vendor boundaries are first-class actors | Confirmed | Encode as Translation slices per vendor edge. Three vendor edges total (OIDC referenced from Identity, document-verification, background-check). |
| B5: Onboarding adjudicator is first manual-human actor | Confirmed; placed in Onboarding BC | Encode as a human actor in adjudication slices; not a separate BC. |
| B6: No Trust & Safety lasso emerged | Vision-doc-escalated as OQ-14 | Out of scope for W004 (vetting lifecycle only). |
| B7: Vehicle as sub-domain question | Deferred | Out of scope for W004 (treat vehicle as a document type per scope decision A). Flag as parking-lot if vehicle-specific complexity surfaces. |

### 2. Vocabulary items (W003 §5.1 — 8 items, V1–V8)

| # | W003 recommendation | W004 expected handling |
|---|---|---|
| V1 | `onboarding.driver-approved` vs. `onboarding.application-approved` | Pick. **Lean: `onboarding.driver-approved`** — the consumer (Driver Profile) cares about the actor's new state, not the application's terminal. |
| V2 | "Documents section" naming | Confirm at slice walk; minor. |
| V3 | "Adjudication case" naming | Confirm at slice walk; sidebar's separate-aggregate decision (#4 above) commits the noun. |
| V4 | Three "approved" moments need distinct names | Encode: `ApplicationApproved` (application aggregate terminal) → `ApplicantNotifiedOfApproval` (notification side effect) → `DriverActivated` (Driver Profile's intake, modeled here as outbound publication, not Onboarding-internal). |
| V5 | "Case" homonym (vendor case vs. adjudication case) | Encode as `BackgroundCheckCase` and `AdjudicationCase` — full prefixes prevent homonym in event/aggregate names. |
| V6 | Document state "under-review" → "under-verification" | Rename. Pre-walk vocabulary scan confirms no collision. |
| V7 | "Rejected" — per-document vs. application-terminal | Encode as `DocumentRejected` (per-document event) vs. `ApplicationRejected` (application-terminal event). |
| V8 | Vendor reason-code normalization enum | Define canonical CritterCab vocabulary at the relevant Translation slice (likely document-verification slice + background-check Consider slice). |

### 3. Open questions (W003 §5.3 — 14 W004-scoped, 5 deferred + OQ-14 escalated)

W004's primary work. Pre-leans:

| OQ | Question | Lean |
|---|---|---|
| OQ-1 | Topic name for approval event | `onboarding.driver-approved` (per V1). |
| OQ-2 | Document-state storage model | Sub-entity on Application (per sidebar #3). |
| OQ-3 | Document history persistence | Event-sourced on Application stream (`DocumentUploaded`, `DocumentRejected`, `DocumentReuploaded`, `DocumentAccepted` — history is the event log). |
| OQ-4 | Adjudication-case storage | Separate aggregate (per sidebar #4). |
| OQ-5 | Pre-adverse vs. final adverse-action events | **Lean: two distinct events** (`PreAdverseActionNoticeIssued`, `FinalAdverseActionNoticeIssued`) — different consumer interest, different audit shape. |
| OQ-6 | Window-expiry firing the final adverse-action | **Lean: Bruun temporal automation** with `AdverseActionDisputeWindow*` todo-list view and `now() >= windowExpiresAt` trigger. Third Bruun pattern instance in CritterCab. |
| OQ-7 | Three "approved" moments distinct events | Yes, per V4 (already covered). |
| OQ-8 | Document state "under-verification" rename | Yes, per V6 (already covered). |
| OQ-9 | Vendor reason-code normalization enum | Define inline at relevant slice, per V8 (already covered). |
| OQ-10 | Rejection outbound publications | **Lean: none.** No cross-BC publication on rejection terminal. Reasons: (a) symmetric with B2's finding that no rejection-side handoff to DP exists; (b) FCRA notices are direct-to-applicant, not cross-BC; (c) T&S / Operations consumption is speculative. Surfaces a forward-constraint that any future T&S workshop must negotiate. |
| OQ-11 | Re-application policy | Out of scope (scope decision A). Flag as parking-lot. |
| OQ-12 | Driver Profile creation timing | **Lean: approval push** (per B3 lean above). Onboarding publishes; DP reacts and creates. |
| OQ-13 | Vehicle as sub-domain | Out of scope (scope decision A). Flag as parking-lot. |
| OQ-14 | Suspension / reinstatement / deactivation | Out of scope (vision-doc-escalated). Not flagged as parking-lot — already lives at vision-doc level. |

### 4. Cross-cutting pattern (W003 §5.1 — multi-vendor ACL aggregator)

W003 surfaced three vendor edges sharing the same ACL-translation pattern (OIDC via Identity, document-verification, background-check) and named the pattern explicitly. **Expected W004 handling:** surface as a new §11 ADR candidate generalizing ADR-006. Trigger to author the ADR: when a fourth vendor edge surfaces, or when Onboarding's first implementation slice forces the pattern to be concrete. Do not author the ADR inside W004's PR.

The workshop artifact's §X "DS findings handled" section consolidates the above into a single audit surface. Becomes the convention for future DS-fed EM workshops.

---

## ADR triggers that fire at this workshop

Five locked ADRs apply; one new candidate expected. All five locked ADRs gain evidence at W004 but are not relitigated.

**Locked ADRs gaining new evidence:**

- **ADR-011 (configuration-as-events bootstrap)** — Onboarding's policy slice (`OnboardingPolicyConfigured`) is the third BC adopting the pattern. Confirms.
- **ADR-012 (aggregate-per-invariant)** — Onboarding's plural-aggregate sidebar outcome (Application + AdjudicationCase + BackgroundCheckCase) is the third canonical data point. Reinforces if sidebar lands at plural; counter-shape worth documenting if it collapses to one aggregate.
- **ADR-013 (shared cross-BC identifier)** — Onboarding's `subjectId` (carries) vs. `applicationId` (Onboarding-internal) is the third decision under this ADR. *Different* shape from W001/W002 (graduated handoff vs. shared-throughout).
- **ADR-014 (ASB topic naming)** — `onboarding.driver-approved` is the third instance. Confirms.
- **ADR-015 (driver-app projection timing budget)** — Onboarding's approval → driver-app transition is the second instance. Likely softer budget than Trips' (no in-progress workflow context). Worth flagging at the relevant slice.

**Expected new §11 ADR candidate:**

- **ADR-candidate: Multi-vendor ACL pattern.** Generalizes ADR-006's OIDC-as-ACL stance to a project-wide pattern covering all third-party vendor edges. Trigger: fourth vendor edge surfaces, or first Onboarding implementation slice forces concretization. Evidence so far: Identity ↔ OIDC (ADR-006), Onboarding ↔ document-verification (W003 Story 2), Onboarding ↔ background-check (W003 Stories 1, 3). Three instances; one more triggers authorship.

The workshop's §11 should re-list these with post-workshop status: *evidence added, locked pattern confirmed* (for ADR-011, ADR-013, ADR-014, ADR-015) / *evidence added, candidate proposes generalization* (for ADR-012) / *new candidate surfaced, awaiting trigger* (for the multi-vendor ACL).

---

## Cast

- **Onboarding** — the workshop's primary BC.
- **Identity** — upstream sender. Pre-committed per ADR-006. Onboarding consumes `identity.driver-registered` at intake. Identity's full event model is a separate (pending) workshop; Onboarding treats it as a known signal with a forward-constraint shape.
- **Driver Profile** — downstream consumer. Receives `onboarding.driver-approved` and creates / activates the driver. Lifecycle-start timing (B3) settles in W004 with downstream implications for DP's eventual workshop.
- **Document-verification vendor** — external actor. Translation slice; ACL boundary per W003 B4 + the multi-vendor ACL candidate.
- **Background-check vendor** — external actor. Translation slice with two response patterns: synchronous Pending ack, async webhook delivery of Clear / Consider / Suspended. The Consider response triggers human adjudication (W003 Story 3 Phase 4).
- **Onboarding adjudicator** — manual-human actor inside Onboarding BC (per W003 B5). First in any CritterCab workshop; second canonical data point — none yet — for whether manual-human actors warrant special workshop treatment.
- **Applicant** — the human actor in the prospective driver → applicant → driver vocabulary lifecycle (per W003 §5.1). Vocabulary transition at application begin; second transition at Driver Profile activation (lives outside Onboarding's terminal). Rejection terminal: applicant remains "applicant" — no "rejected applicant" sub-state.
- **Operations** — speculative downstream consumer of adjudicator-tooling concerns. Modeled as a forward-constraint generator if W004 surfaces adjudicator-queue projection concerns that imply Operations-side tooling; not modeled as an active actor in any slice.

---

## Orientation files (read in order before starting)

1. **[`CLAUDE.md`](../../../CLAUDE.md)** — the routing layer. Skim if recently read; pay attention to Architectural Non-Negotiables and the Companion: JasperFx ai-skills section.
2. **[`docs/vision/README.md`](../../vision/README.md) §Tentative Bounded Contexts → Onboarding + §Open Questions → OQ-14 (suspension lifecycle)** — Onboarding's vision-doc-level characterization and the explicitly-out-of-scope OQ.
3. **[`docs/workshops/003-onboarding-domain-story.md`](../../workshops/003-onboarding-domain-story.md)** — **the canonical upstream input.** Read in full. Pay particular attention to: §4 (the three stories), §5.1 (vocabulary findings + cross-cutting pattern), §5.2 (BC-boundary findings), §5.3 (the 14 W004-scoped OQs), §3 (notation conventions — markdown-as-DS-substrate adaptations).
4. **[`docs/retrospectives/workshops/003-onboarding-domain-storytelling.md`](../../retrospectives/workshops/003-onboarding-domain-storytelling.md)** — W003's retro. Pay attention to §Solo-DS adaptation Q1–Q6 (especially Q2 on vocabulary divergence surfaced) and §Outstanding items / next-session inputs (this prompt's source).
5. **[`docs/research/ride-sharing-driver-onboarding-domain-note.md`](../../research/ride-sharing-driver-onboarding-domain-note.md)** — frozen Phase 0 grounding from W003. Reused as-is per the prompt-authoring decision. Pay attention to §6 (boundary candidates) and §4 (background-check vendor patterns including Consider intermediate status).
6. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md)** — read for shape conventions and §12.6 methodology adjustments. Particular attention: §3 (Ubiquitous Language structure), §5.7 (Bruun temporal automation precedent), §5.11 (configuration-as-events precedent), §12.6 (all five refinements).
7. **[`docs/workshops/002-trips-event-model.md`](../../workshops/002-trips-event-model.md)** — closer template than W001 (aggregate-identity sidebar in §3, forward-constraints handled in §13, methodology refinements in §14.6). Particular attention: §3 (sidebar structure), §13 (forward-constraints handled — W004's §X "DS findings handled" mirrors this), §14.6 (all five additional refinements).
8. **[`docs/workshops/README.md`](../../workshops/README.md)** — workshop conventions + § Workshop follow-ups (W003 §5.3 entry W004 closes).
9. **[`docs/research/event-modeling-workshop-guide.md`](../../research/event-modeling-workshop-guide.md)** — methodology reference. Refresh on Lesson 2 (notation), Lesson 3 (Translation slices), Lesson 8 (collaborator personas). Onboarding is translation-heavy on three edges (Identity intake, two vendors).
10. **[`docs/research/agents-in-event-models.md`](../../research/agents-in-event-models.md)** — Bruun temporal-automation pattern (FCRA dispute-window slice will use it) and Klefter decision-event pattern (multiple uses across vendor translations + adjudication).
11. **[`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md)** — structural rules. Identity-as-ACL is load-bearing for the multi-vendor ACL candidate.
12. **ADRs [011](../../decisions/011-configuration-as-events-bootstrap.md), [012](../../decisions/012-aggregate-per-invariant.md), [013](../../decisions/013-shared-cross-bc-identifier.md), [014](../../decisions/014-asb-topic-naming-convention.md), [015](../../decisions/015-driver-app-projection-timing-budget.md), [006](../../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md)** — all six locked; reference during sidebar and ADR-triggers slice.
13. **[`docs/context-map/README.md`](../../context-map/README.md)** — edge #6 (Onboarding → Driver Profile) carries the W003 amendment. Further amendments possible if W004 surfaces new edge shape (e.g., the Operations forward-constraint).
14. **[`docs/research/methodology-log.md`](../../research/methodology-log.md)** — recent entries on two-layer fidelity (003) and forward-constraints (004). W004 may produce entry 005 if a cross-cutting observation surfaces about DS-as-upstream input.
15. **[`docs/decisions/004-design-phase-workflow-sequence.md`](../../decisions/004-design-phase-workflow-sequence.md)** — design-return cadence rule that triggered this session.
16. **[`docs/prompts/workshops/002-trips-event-modeling.md`](./002-trips-event-modeling.md)** — second-EM-workshop prompt for shape comparison; closest template.
17. **[`docs/prompts/README.md`](../README.md)** — prompts conventions including the new `## Spec delta` cadence.

---

## Working pattern

Same interactive cadence as Workshop 002, with three W004-specific adaptations:

- **Lock scope at session start.** Locked to Option A (tight — exact mirror of W003's three stories); confirm with user before sidebar.
- **Run the aggregate-identity sidebar (§Aggregate-identity sidebar above) before Slice 1.** Output captured as §3 of the workshop artifact. State ADR-012 + ADR-013 evidence framing explicitly. Include the pre-walk vocabulary scan against W001 + W002 (per W002 §14.6 #1).
- **Walk slices section-by-section.** Spine ordering follows W003's three-story narrative: intake (W003 Story 1 steps 1–4 → Onboarding intake from Identity), personal-info + consent, document upload + verification (happy + recovery paired), background check (Clear + Consider→adjudicate paired), FCRA two-phase notice (pre-adverse + window-expiry temporal automation + final adverse, paired in narrative order per W001 §12.6 #6), approval terminal + notification + DP handoff, application-rejected terminal (no DP publication).
- **Pair open questions with leaning opinions** per §12.7 calibration. Most W003 OQs have prompt-authored leans above; surface departures explicitly.
- **Name candidate projections from Slice 1** per W001 §12.6 #1. Onboarding's expected projections: `ApplicantDashboardView` (per-applicant status), `AdjudicatorQueueView*` (todo-list for adjudication cases pending review), `AdverseActionDisputeWindow*` (todo-list for FCRA window-expiry temporal automation), `OnboardingFunnelView` (operations metrics — counts per stage, conversion rate, drop-off analysis).
- **Number sub-slices explicitly** when cascades occur (e.g., document upload + verification as 4.4a / 4.4b for happy vs. recovery).
- **Pair temporal-automation slices with their feeder slice in narrative order** per W001 §12.6 #6. FCRA dispute-window temporal automation slice immediately follows the pre-adverse-action notice slice.
- **Defer protobuf authorship** per W001 §12.6 #4. Name the protos needed; capture as a post-workshop authorship session under ADR-009.
- **§X "DS findings handled"** — at session close, walk the four W003 finding categories (BC findings B1–B7, vocabulary V1–V8, OQs OQ-1 through OQ-13, cross-cutting pattern). Each gets explicit disposition.
- **§X+1 "Forward-constraints generated"** — consolidate any outbound forward-constraints W004 generates on un-modeled BCs' future workshops (Identity, Driver Profile, possibly Operations, possibly Trust & Safety). Per W002 §14.6 #2.
- **§11 ADR Candidates** — re-list locked ADRs (011, 012, 013, 014, 015) with post-workshop evidence; surface new candidate (multi-vendor ACL pattern).
- **§12-equivalent retrospective** — same nine-subsection shape as W001 §12 / W002 §14. Pay attention to the methodology question: did DS-as-upstream produce materially higher-quality EM artifact than narratives-only? Conditional methodology log entry 005 depends on the answer.
- **Commit only after explicit sign-off** per phase.

---

## Deliverable plan

| File | Status | Purpose |
|---|---|---|
| `docs/workshops/004-onboarding-event-model.md` | New | Workshop artifact, structurally parallel to W002. Includes §3 Aggregate Identity sidebar output, §X "DS findings handled," §X+1 "Forward-constraints generated," §11 ADR Candidates re-list + new candidate, §12-equivalent nine-subsection retrospective. |
| `docs/workshops/README.md` | Edit | Workshops list row for W004. Close W003 §5.3 "Workshop 004 — Onboarding event model" row in § Workshop follow-ups with link to closing artifact. Add new § "From Workshop 004 §X.8 Follow-ups" subsection enumerating any new follow-ups generated. **Also: same-PR opportunity to tidy the stale W002 §14.8 row entries** — ADRs 011–015 are authored; the index entries can be marked **done** in this PR per the in-bounds same-file edit rule. |
| `docs/retrospectives/workshops/004-onboarding-event-modeling.md` | New | Retro. Includes the methodology-specific subsection on DS-as-upstream-input efficacy. Second entry in `retrospectives/workshops/`. |
| `docs/retrospectives/README.md` | Edit | New index entry. |
| `docs/research/methodology-log.md` | Edit *(conditional)* | Entry 005 *if* a cross-cutting observation surfaces meeting entry 001's three criteria (spans / wouldn't-fit-in-retro / predicts). Strong candidate: confirming or disconfirming that DS-as-upstream produces materially higher-quality EM than narratives-only. If no entry warranted, none is written. |
| `docs/context-map/README.md` | Edit *(conditional)* | Document-history entry only if W004 surfaces a boundary realignment affecting a named edge (likely candidate: new Onboarding → Operations edge if adjudicator-tooling forward-constraint warrants one; new Onboarding → Driver Profile edge prose update if B3 lifecycle-start decision changes shape). Honest "no context-map impact" is a fine outcome. |

### Definition of done

- Workshop artifact committed at v0.1 (or whatever version the session-close warrants), structurally parallel to W002.
- All 14 W003 W004-scoped open questions handled with explicit disposition in §X "DS findings handled."
- Aggregate-identity sidebar captured as §3 with ADR-012 + ADR-013 evidence framing called out.
- §11 re-lists locked ADRs (011, 012, 013, 014, 015) with post-workshop status; surfaces multi-vendor ACL as new candidate.
- §12-equivalent retrospective complete with DS-as-upstream-input efficacy subsection.
- `docs/workshops/README.md` updated with W004 list row + W003 follow-up closure + new follow-ups subsection.
- Methodology log entry 005 written *if* warranted; silence is fine if not.

---

## What this session deliberately does NOT carry

- **Protobuf authorship** — deferred per W001 §12.6 #4 and ADR-009. Name protos needed (`onboarding.driver-approved` is the obvious one; pre-adverse / final adverse notices may need their own contracts if T&S is a future consumer); do not author. New authorship is a separate session (PR-4-style precedent).
- **Driver Profile / Operations / Trust & Safety BC modeling** — Onboarding publishes business events to these BCs; they consume. Their own workshops are separate sessions.
- **Identity BC modeling** — the `identity.driver-registered` forward-constraint surfaces an Identity-side projection requirement, but Identity's full event model is a separate (still-pending) workshop.
- **Re-application policy** — OQ-11 deferred per scope decision A.
- **Vehicle as sub-domain** — OQ-13 / W003 B7 deferred per scope decision A. Treat vehicle as a document type.
- **Suspension / reinstatement / deactivation** — OQ-14 vision-doc-escalated. Not flagged as parking-lot — already at vision-doc level.
- **Implementation code** — pure design. No C#, no Go, no proto edits.
- **Skill-file edits** — Onboarding workshop may surface gaps in skill files (e.g., `marten-aggregates` worked examples for multi-aggregate BCs). Capture as DEBT.md rows or as a follow-up `tidy: skills` PR; do not edit skill files in-session.
- **ADR authorship** — locked ADRs gain evidence; the multi-vendor ACL candidate is named but not authored. Authoring is a follow-up session per ADR-009-style precedent.
- **Updates to the Phase 0 research note** — frozen by design per W003's decision. Any contradictions surface in §X "DS findings handled" or as retro entries, never as edits to the note.
- **Vision-doc edits** — W003 already bumped to v0.5 with the methodology Exercised entry. W004 should not produce a vision-doc bump unless something genuinely warranting one surfaces (e.g., a BC realignment); same-PR opportunity is in-bounds per the no-opportunistic-edits rule but should be justified, not reflexive.

---

## Memory inheritance

The applicable feedback memories carry into this session via `MEMORY.md`. No restatement needed:

- Proactive projection proposals during artifact authoring (W001 §12.6 #1 — applied from Slice 1).
- Critter Stack primitives over bespoke alternatives.
- BC-owned enums (per `TerminationReason` precedent across W001 + W002).
- Wolverine Aggregate Workflow = Decider Pattern with immutable-Apply preference.
- Communication preferences (depth, ubiquitous language, leaning opinions, DDD-background).
- Explicit deferrals during artifact authoring with cumulative-tracking nuance.
- Keep READMEs current alongside session work — `docs/workshops/README.md` § Workshop follow-ups closure is the W004-specific application.

---

## Methodology question to resolve in the retro

W004 is CritterCab's first event modeling workshop with DS as the primary upstream input. The retro should answer one focused question alongside the standard nine-subsection close-out:

**Did DS-as-upstream produce materially higher-quality EM artifact than narratives-only would have?**

Evidence shape: count vocabulary surprises resolved without re-litigation, count BC findings encoded as slice invariants, count OQs answerable with lean rather than requiring fresh debate. Compare against W002's narrative-pair upstream input. If the answer is "yes, materially," that's the trigger for methodology log entry 005 and for considering DS as a default pre-EM step for vocabulary-rich BCs going forward.

If the answer is "no, equivalent" or "DS value was front-loaded and EM didn't reuse it," that's an equally valuable finding — DS earns its keep on its own terms (per W003 retro) but doesn't necessarily warrant a permanent pre-EM placement.

---

## Starting move

When the new session begins:

1. **Read the orientation files in order** (§Orientation files above). W003 § 4 + § 5 are load-bearing — read in full.
2. **Confirm scope.** Locked at prompt-authoring time to Option A (tight — exact mirror of W003's three stories). Validate with user; if any movement, surface as the first decision before the sidebar runs.
3. **Run the pre-walk vocabulary scan** against W001 + W002 vocabulary (per W002 §14.6 #1). Confirm no new collisions beyond W003's V1–V8.
4. **Run the aggregate-identity sidebar** (§Aggregate-identity sidebar above). ~30 minutes. Capture output as §3 of the workshop artifact. State ADR-012 + ADR-013 evidence framing explicitly.
5. **Propose the slice ordering.** Spine ordering follows W003's narrative structure: intake → personal-info + consent → document upload + verification (paired happy/recovery) → BG check (paired Clear/Consider→adjudicate) → FCRA two-phase notice (with temporal automation paired in narrative order) → approval terminal + DP handoff → application-rejected terminal. Translation-out slices come after the lifecycle spine. Configuration-as-events slice (`OnboardingPolicyConfigured`) slotted at the appropriate point per W001 §5.11 precedent.
6. **Walk slices.** Pause for sign-off after each. Calibrate cadence per W001 §12.6 #3 (heavy slices: aggregate-identity-impacted slices, multi-vendor ACL translation slices, FCRA temporal automation; lighter slices: confirmations of W003 already-locked vocabulary).
7. **At session close:** §X "DS findings handled" walk; §X+1 "Forward-constraints generated" consolidation; §11 ADR Candidates re-list; §12-equivalent retrospective; update `docs/workshops/README.md` (W004 list row + W003 follow-up closure + new follow-ups subsection); optionally append methodology log entry 005; optionally amend context map.

Don't batch the whole workshop into one output. Workshop sessions are interactive, slice by slice — W001 + W002 cadence is the precedent.
