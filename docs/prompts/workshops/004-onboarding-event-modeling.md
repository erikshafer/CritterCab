# Prompt 004 — Fourth Event Modeling Workshop (Onboarding)

| Field | Value |
|---|---|
| **Status** | Authored (not yet run); session pending |
| **Authored** | 2026-05-26 |
| **Target artifact** | `docs/workshops/004-onboarding-event-model.md` |
| **Companion artifacts** | [`docs/workshops/README.md`](../../workshops/README.md) (Workshops list + § Workshop follow-ups — close "Workshop 004 — Onboarding event model" row from W003 §5.3); conditionally [`docs/research/methodology-log.md`](../../research/methodology-log.md) (entry 005 if warranted); conditionally [`docs/context-map/README.md`](../../context-map/README.md) (further edge refinements if W004 surfaces them) |
| **Source-of-truth dependencies** | [`docs/workshops/003-onboarding-domain-story.md`](../../workshops/003-onboarding-domain-story.md) (the canonical DS input — three stories, 7 BC findings, 8 vocabulary items, 14 W004-scoped open questions, 1 cross-cutting pattern); [`docs/research/ride-sharing-driver-onboarding-domain-note.md`](../../research/ride-sharing-driver-onboarding-domain-note.md) (frozen Phase 0 grounding from W003; reused as-is per the prompt-authoring decision); [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) and [`docs/workshops/002-trips-event-model.md`](../../workshops/002-trips-event-model.md) (shape conventions and methodology refinements); ADRs [011](../../decisions/011-configuration-as-events-bootstrap.md) / [012](../../decisions/012-aggregate-per-invariant.md) / [013](../../decisions/013-shared-cross-bc-identifier.md) / [014](../../decisions/014-asb-topic-naming-convention.md) / [015](../../decisions/015-driver-app-projection-timing-budget.md) (all locked; W004 applies, does not relitigate); [`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) (locked Identity boundary); Wolverine's [Process Manager via Handlers guide](https://wolverinefx.net/guide/durability/marten/process-manager-via-handlers.html) and the [`ProcessManagerViaHandlers` sample](https://github.com/JasperFx/wolverine/tree/main/src/Samples/ProcessManagerViaHandlers) (the canonical reference for Onboarding's modeling shape — see §Modeling-pattern sidebar) |
| **Workflow position** | Third Event Modeling workshop; fourth design-phase artifact overall (after W001 EM, W002 EM, W003 DS). First workshop to consume a DS artifact as its primary upstream input rather than narratives. **First CritterCab instance of the Process Manager via Handlers pattern** — sibling to W001's parent-with-sub-entity pattern and W002's single-aggregate-full-lifecycle pattern. Closes the W003 §5.3 "Workshop 004 — Onboarding event model" follow-up; provides a new evidence point for ADR-013 (shared cross-BC identifier) and seeds two new §11 ADR candidates (Process Manager as third CritterCab modeling pattern; multi-vendor ACL integration absorbed inside the Process Manager). Returns to the **design phase** per ADR-004's design-return cadence rule after the W003 PR. |

---

## Spec delta

This session is **spec-creating** for Onboarding's event model, **spec-confirming** for W003's DS findings, and **pattern-introducing** for CritterCab as a whole — Onboarding is modeled as the project's first **Process Manager via Handlers** instance, alongside the two aggregate-cluster patterns already in evidence (W001 parent-with-sub-entity; W002 single-aggregate-full-lifecycle).

- **`docs/workshops/004-onboarding-event-model.md` is created** — first event model for the Onboarding BC. Inherits W003's three-story spine as slice ordering; commits event names, command shapes, process-state design, projection candidates, and Translation-slice contracts where W003 left vocabulary candidates.
- **W003 §5.3's 14 W004-scoped open questions are answered or explicitly deferred.** OQ-1 through OQ-13 (and OQ-14 as out-of-scope reference) get explicit dispositions in a §X "DS findings handled" section. Several OQs (OQ-2 document storage, OQ-3 document history, OQ-4 adjudication-case storage) resolve to "events on the Application's process stream with correlation IDs as event fields" rather than aggregate-shape decisions.
- **`docs/workshops/README.md` § Workshop follow-ups closes the W003 §5.3 row** for Workshop 004 with the closing artifact link; adds a new § "From W004 §X.8 Follow-ups" subsection if W004 generates fresh follow-ups.
- **Locked ADRs gain evidence (revised under Process Manager framing):**
  - **ADR-011 (configuration-as-events bootstrap)** — applies via `OnboardingPolicyConfigured` (FCRA dispute window length, application-abandonment timeout, vendor no-response timeout, document-rejection retry budgets). Third BC adopting the pattern.
  - **ADR-012 (aggregate-per-invariant)** — **NOT a third evidence point**. Onboarding is a different modeling pattern (Process Manager), not an aggregate-cluster. Honest reframe surfaces in §11 with reasoning.
  - **ADR-013 (shared cross-BC identifier)** — applies. Application's canonical UUIDv7 == DriverProfile's identifier on activation; Onboarding becomes the fourth participant in CritterCab's shared-canonical-ID chain.
  - **ADR-014 (ASB topic naming)** — applies via `onboarding.driver-approved`. Third instance after Dispatch's `dispatch.ride-assigned` and Trips' four outbound topics.
  - **ADR-015 (driver-app projection timing budget)** — applies (likely with a softer numeric budget) to the driver-app transition from "application pending" to "ready to drive" at approval.
- **Two new §11 ADR candidates are expected:**
  - **NEW candidate #1 — Process Manager via Handlers as CritterCab's third modeling pattern.** Onboarding-instance. Delineates when to reach for Process Manager vs Aggregate (per ADR-012). Trigger to author: W004 ships with this shape; first Onboarding implementation slice exercises it.
  - **NEW candidate #2 — Multi-vendor ACL pattern (W003 §5.1) absorbed inside the Process Manager** rather than freestanding. Three vendor edges become translation handlers that emit domain events appended to the Application's process stream. Generalizes ADR-006 from OIDC-only to all vendor edges. **Authored as a separate paired ADR** alongside candidate #1 — CritterCab's convention is one decision per ADR (see `docs/decisions/`); the two candidates are load-bearing for each other (the Process Manager is the *shape*; the multi-vendor ACL is *how it integrates* with external vendors) but each warrants its own decision document for traceability.
- **One new forward-constraint generated on the Notifications BC's eventual workshop.** OQ-10 (grill #3) surfaced that FCRA notice events (`PreAdverseActionNoticeIssued`, `FinalAdverseActionNoticeIssued`) need to result in actual email/SMS delivery, and the reference-architecture-aligned shape is cross-BC publication to ASB per ADR-014 rather than Onboarding-internal `IFcraNoticeSender` integration. Candidate topic: `onboarding.adverse-action-notice-required` (slice walk picks exact slug). Consumer pending the (unscheduled) Notifications BC workshop. Notifications BC is not currently in the vision-doc's tentative-BC list; the forward-constraint surfaces as a §X+1 entry without requiring a vision-doc edit.
- **No existing spec is amended retroactively.** W001, W002, W003 stay locked; W004 honors or overrides via documented reasoning in §X "DS findings handled," not via edits to prior artifacts.

---

## Grill-with-docs resolution history

The prompt's leans encode four `grill-with-docs` resolutions applied during prompt-authoring (commits `e764fde`, `1895889`, `70ee249` on the branch `workshop/004-onboarding-event-modeling`). The session-runner does not need to re-litigate these — they are locked decisions captured here so the prompt is self-contained. References to "grill #N" elsewhere in the prompt point back to this table.

| Grill | What was challenged | Resolution | Where the resolution lands |
|---|---|---|---|
| **#1** | Original lean framed the Onboarding → DriverProfile handoff as a "graduated handoff" with `applicationId` Onboarding-internal and `subjectId` carrying across BCs. Reasoning didn't engage with ADR-013's stated scope ("lifecycles whose stages cross BCs in a deterministic order"). | **ADR-013 applies.** `applicationId == driverProfileId` shared canonical UUIDv7 minted by Onboarding at `ApplicationOpened`, inherited by DriverProfile on activation. `subjectId` (Identity-vocabulary) carries alongside as the actor handle, distinct from the lifecycle ID. Onboarding becomes the fourth participant in CritterCab's shared-canonical-ID chain. | §What this session inherits → ADR-013 bullet; §Modeling-pattern sidebar → Cross-BC identifier subsection; §ADR triggers → ADR-013 entry |
| **#2 / #4** | Original lean placed Onboarding as a three-aggregate cluster (Application + AdjudicationCase + BackgroundCheckCase) for the §11 ADR-012 third-evidence-point. Justifications used "consistency surface" and "anti-corruption surface" framings that ADR-012 does not codify. User raised the structural-lifecycle concern (streams without proper terminals leak) and then surfaced the deeper reframe: "streams are the treasure, aggregates are a means to an end" — pointing at Wolverine's Process Manager via Handlers pattern. | **Onboarding is modeled as a Process Manager via Handlers.** Single canonical UUIDv7 per application stream. All coordination events (applicant inputs, vendor translations, adjudicator decisions, FCRA notices, Bruun temporal automations, terminal events) appending to one stream. `OutgoingMessages.Delay` for timeouts. Let-state-decide for scheduled timer resolution. ADR-012 evidence base stays at W001 + W002 (Onboarding is honestly *not* a third aggregate-per-invariant data point). Two new paired §11 ADR candidates surface: Process Manager pattern; multi-vendor ACL absorbed inside it. | §Modeling-pattern sidebar (full section); §What this session inherits → ADR-012 reframe; §ADR triggers (restructured); §Spec delta pattern-introducing framing |
| **#3** | Original OQ-10 lean ("no rejection outbound publications") was correct in conclusion but under-specified in scope — it addressed only `ApplicationRejected`, leaving three other terminal-and-intermediate events (`ApplicationAbandoned`, `ApplicationWithdrawn`, FCRA notice events) implicit. | **OQ-10 split into OQ-10a through OQ-10e.** Confirms no-publication for the three terminal events. Surfaces FCRA notice events as needing cross-BC publication to ASB per ADR-014 (`onboarding.adverse-action-notice-required`) with consumer pending the eventual Notifications BC workshop — generates a new forward-constraint. Sharpens T&S consumption to pull-via-projection rather than push-via-publication. | §DS findings OQ-10a–e rows; §Modeling-pattern sidebar FCRA notice event-catalogue entry; §Spec delta Notifications forward-constraint bullet; §Cast Notifications BC entry; §Working pattern Forward-constraints generated bullet |
| **#4** | The two new paired §11 ADR candidates (Process Manager + multi-vendor ACL) were left with a hedge — "may be authored together or as two complementary ADRs." | **Two separate paired ADRs.** Per CritterCab's existing one-decision-per-ADR convention (see `docs/decisions/`). The two candidates are load-bearing for each other (Process Manager is the *shape*; multi-vendor ACL is *how it integrates* with external vendors) but each warrants its own decision document for traceability. | §Spec delta candidate #2 description; §ADR triggers candidate #2 description; §ADR triggers post-workshop-status closing sentence |

All four resolutions are committed to the branch; the workshop session inherits them as locked design context, not as open questions.

---

## Framing — why this session exists

W003 surfaced enough vocabulary disambiguation and BC-boundary confirmation to make event modeling for Onboarding low-risk on the language side. Three stories with 79 numbered steps, three actor types including the first manual-human reviewer in any CritterCab workshop, two new work-object types (adjudication case), and a cross-cutting multi-vendor ACL pattern — all give W004 a richer starting position than W001 or W002 had. W003's retrospective §Q2 explicitly named four vocabulary divergences that would have been invisible without the DS walk; W004 inherits the resolutions and turns them into event names.

W003 §5.3 enumerated 14 open questions with the explicit label "clear W004 scope" (OQ-1 through OQ-9 event-modeling concerns; OQ-10 through OQ-13 BC-boundary concerns). Closing them is W004's primary work. Five additional OQs (OQ-15 through OQ-19) are explicitly deferred to future sessions; OQ-14 is vision-doc-escalated. W004 honors these scope boundaries.

W003's characterization of Onboarding — "multi-vendor ACL aggregator with multiple parallel gates and a long-running coordination surface punctuated by external events and time-based terminals" — is a textbook **Process Manager** shape, not an aggregate-cluster shape. W004 commits Onboarding to the **Process Manager via Handlers** pattern (Wolverine's built-in primitives, no `ProcessManager` base class), making Onboarding the canonical in-repo reference for that pattern across the Critter Stack. This is pedagogically aligned with CritterCab's reference-architecture mission: the project exists to demonstrate Wolverine's gRPC features *and* to make other Critter Stack patterns concrete via worked examples. Onboarding-as-Process-Manager fills the latter surface deliberately — sibling pedagogical value to the gRPC slices in Dispatch.

Three secondary jobs:

1. **Test the DS → EM input mechanism.** This is the first time CritterCab has run an event modeling workshop with a DS artifact as the primary upstream input (W001 and W002 consumed narratives). Methodology log entry 005 is conditional on observing whether DS produces materially higher-quality EM artifacts than narratives-only input, or whether the value was front-loaded in DS itself.
2. **Pilot Process Manager via Handlers as a CritterCab modeling pattern.** First in-repo instance; sibling to W001's parent-with-sub-entity and W002's single-aggregate-full-lifecycle. The W004 retro's methodology subsection captures whether the pattern fit Onboarding's shape as predicted by the VitePress guide, or surfaced friction the guide doesn't capture. Outcome seeds the new §11 ADR candidate that delineates Process Manager vs Aggregate selection criteria.
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

- **Workshop artifact shape** — Scope Statement, **Modeling-pattern + ApplicationState sidebar** (Process Manager via Handlers; structurally parallel to W002 §3 Aggregate Identity, content adapted for the process-state framing), Ubiquitous Language glossary, Event List, per-slice walkthrough, cross-reference tables (Translation slices, temporal automations, configuration-as-events, Protobuf surface), DS findings handled (new — §X), Parking Lot / Open Questions, ADR Candidates, Retrospective. See [`docs/workshops/README.md`](../../workshops/README.md) § Conventions plus the W002 §14.6 refinements below.
- **Slice notation conventions** — events past tense; commands imperative; views/projections named ubiquitously; automations as green stickies; temporal automations with the asterisk-suffix todo-list view per Bruun; clock-rewind glyph on time-driven automations.
- **Klefter decision-event pattern** — when a slice coordinates external systems AND a decision is made locally, promote the decision to a local event. Onboarding has multiple Klefter candidates: vendor verification responses (translated decisions), background-check Consider→reject adjudication (decision-event), FCRA window-expiry (temporal-automation-driven decision).
- **Bruun temporal-automation pattern** — todo-list read model with asterisk suffix, clock-rewind glyph on time-driven automations, configuration-as-events for tunable thresholds. Onboarding's primary candidate: FCRA dispute window expiry firing the final adverse-action notice (W003 OQ-6).
- **Configuration-as-events pattern (ADR-011)** — operator-tunable parameters consolidated into `*PolicyConfigured` events. Onboarding's candidate policy: vendor selection routing, FCRA dispute window length, document-rejection retry budgets, approval auto-flow thresholds. Likely an `OnboardingPolicyConfigured` slice. **Third BC to adopt; the ADR-011 pattern is locked.**
- **Aggregate-per-invariant (ADR-012)** — aggregates emerge from invariant boundaries. **Onboarding is NOT a third evidence point for ADR-012** — it is a Process Manager instance (a different modeling pattern with weaker parent-level invariants and coordination-dominated semantics). ADR-012's evidence base stays at two instances (W001 + W002). The honest reframe is captured in §11 ADR Candidates plus the new candidate that delineates Process Manager vs Aggregate selection.
- **Shared cross-BC identifier (ADR-013)** — `tripId == rideRequestId` was the W001/W002 instance. **Onboarding's resolution (locked at prompt-authoring per grill #1): `applicationId == driverProfileId`** — single canonical UUIDv7 minted by Onboarding at `ApplicationOpened`, inherited by DriverProfile on activation. `subjectId` (Identity-vocabulary) carries alongside as the actor handle, distinct from the lifecycle ID. Onboarding becomes the fourth participant in CritterCab's shared-canonical-ID chain.
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
2. **Pre-walk modeling-pattern + ApplicationState sidebar** — see §Modeling-pattern sidebar below. Run before slice 1. Adapts W001 §12.6 #2's aggregate-identity sidebar to the Process Manager framing.
3. **Calibrate cadence to slice complexity.** Heavy slices (modeling-pattern sidebar, FCRA two-phase notice + temporal automation, multi-vendor ACL translation slices, terminal-and-compensation enumeration) warrant extended discussion; lighter slices walked briskly.
4. **Defer Protobuf authorship.** Any new Onboarding business-event proto contracts (the `onboarding.driver-approved` event for Driver Profile and possibly others) defer to a follow-up authorship session under ADR-009.
5. **Number sub-slices explicitly when cascades occur** (5.5a / 5.5b convention).
6. **Pair temporal-automation slices with their feeder slice in narrative order.** FCRA dispute-window temporal automation pairs with the pre-adverse-action notice slice.

**W002 §14.6 — all five carry forward, with one substitution for the DS-as-upstream input:**

1. **Pre-walk vocabulary scan against prior workshops + industry conventions.** Run during the modeling-pattern + ApplicationState sidebar. W003 §5.1 already did most of this work (V1–V8); W004's job is to confirm no new collisions with W001/W002 vocabulary. Watch specifically for: `Accepted` (W001 — offer accepted; W004 — document accepted), `Approved` (W004 — application approved vs. driver activated; three moments per V4), `Active` (W001 — active request; W002 — active trip; W004 — active driver, active application).
2. **Add §X "DS findings handled" section** — adapted from W002 §13 "Forward-constraints handled." Documents disposition (honored / overridden / partially-honored / deferred) for each of W003's 7 BC findings, 8 V-items, 14 OQs, and 1 cross-cutting pattern.
3. **Paired walks where structurally similar.** Onboarding candidates for pairing: the two document-verification slices (happy + rejection-recovery from W003 Stories 1 + 2); the two background-check slices (Clear + Consider→adjudicate from W003 Stories 1 + 3); the two notification slices (pre-adverse-action + final adverse-action under FCRA).
4. **Mid-walk forward-amendments via cross-reference.** Slice 6.12 in W002 amended slice 6.1 via cross-reference rather than retroactive edit. W004 may need this if (e.g.) the multi-vendor ACL pattern surfaces a need to amend earlier vendor slices.
5. **Mid-sidebar research grounding** when vocabulary or modeling-shape decisions need industry baseline — W002 used Uber/Lyft/system-design literature for `Matched`. W004's likely candidates: per-document state-machine vocabulary, FCRA two-phase notice canonical names, adjudication-case lifecycle naming, Process-Manager state-type naming conventions (e.g., `ApplicationState` vs. `ApplicationProcess` vs. `OnboardingApplicationState`).

**One W004-specific new practice:**

- **Forward-constraints generated section** — W002 §14.6 #2 called for an explicit consolidation of *outbound* forward-constraints (on un-modeled BCs' future workshops). W003 already generated three for the Identity workshop (`identity.driver-registered`, `identity.rider-registered` from W002 was the second). W004 should consolidate its own outbound forward-constraints on: (a) Driver Profile's eventual workshop (lifecycle-start timing per OQ-12; canonical-ID inheritance per ADR-013); (b) the **Notifications BC's eventual workshop** (consumption of FCRA notice publications per OQ-10d — Notifications BC is not yet in the vision-doc tentative-BC list; the constraint surfaces as a §X+1 entry without requiring a vision-doc edit); (c) the (still-pending) Operations workshop if any adjudicator-queue tooling concerns surface; (d) any future Trust & Safety workshop (pull-via-projection pattern per OQ-10e).

---

## Modeling-pattern + ApplicationState sidebar (run BEFORE slice 1)

Per W001 §12.6 #2, the modeling-shape decision shapes every subsequent slice. This sidebar replaces W002's aggregate-identity sidebar with a Process Manager-shaped equivalent. **The pattern decision is locked at prompt-authoring time: Onboarding is modeled as a Process Manager via Handlers, per the [Wolverine VitePress guide](https://wolverinefx.net/guide/durability/marten/process-manager-via-handlers.html) and the [`ProcessManagerViaHandlers` sample project](https://github.com/JasperFx/wolverine/tree/main/src/Samples/ProcessManagerViaHandlers).** What this sidebar produces is the `ApplicationState` design, the event catalogue on the Application's process stream, the terminal-event enumeration, and the scheduled-timeout enumeration. The sidebar's output goes into the workshop artifact as §3 "Modeling pattern and process state."

### Why Process Manager (the lock's rationale)

Onboarding's shape — long-running coordination across parallel external events, multiple vendor and human-actor integration boundaries, FCRA temporal automation, four distinct terminals (approval, rejection, abandonment, withdrawal), weak parent-level invariants — fits the Process Manager via Handlers pattern essentially. W003's own characterization ("multi-vendor ACL aggregator with multiple parallel gates and a long-running coordination surface punctuated by external events and time-based terminals") is a textbook Process Manager characterization. Forcing Onboarding into an aggregate-cluster vocabulary (per ADR-012) would require justifying invariants that don't honestly exist at the parent level, and would multiply stream-lifecycle bookkeeping (compensation choreography, per-peer abandonment timers, terminal cascades) for no domain-correctness benefit.

The aggregate framings considered and rejected during prompt-authoring grilling:

- **Plural aggregates** (Application + AdjudicationCase + BackgroundCheckCase) — rejected. Splits the streams along *consistency surface* and *anti-corruption surface* lines that ADR-012 does not codify; multiplies lifecycle bookkeeping.
- **Sub-entity-on-Application** (Document + AdjudicationCase + BackgroundCheckCase as sub-entities on a single Application aggregate, RideRequest+Offer-style) — rejected. Closer to honest but still imposes invariant-protection framing on a coordination-dominated shape; loses the pattern's first-class timeout primitives.
- **Process Manager via Handlers** — selected. Single process stream per application; coordination events from external sources land on the process stream after translation; `OutgoingMessages.Delay` / `Schedule` for FCRA window, abandonment, vendor no-response timeouts; terminal-state guard plus step-level idempotency guard on every continue handler; let-state-decide pattern for timeouts (no cancel-the-timer API needed).

### `ApplicationState` design — the process state

The Process Manager pattern operates over a plain C# class whose `Apply` methods derive current snapshot from the stream. Sketch:

```csharp
public class ApplicationState
{
    // Required by Marten: FetchForWriting registers the type as a document type.
    public Guid Id { get; set; }                        // canonical UUIDv7, == driverProfileId on activation per ADR-013
    public Guid SubjectId { get; set; }                 // Identity-vocabulary actor handle, carried alongside

    public bool PersonalInfoComplete { get; set; }
    public bool BackgroundCheckConsentGiven { get; set; }
    public bool DocumentsSectionComplete { get; set; }  // derived from per-document state

    public VendorBackgroundCheckStatus? BackgroundCheckStatus { get; set; }     // Pending | Clear | Consider | Suspended
    public AdjudicationOutcome? AdjudicationOutcome { get; set; }                // null until adjudication terminal
    public FcraPhase? FcraPhase { get; set; }                                    // null | PreAdverseIssued | DisputeWindowOpen | FinalAdverseIssued
    public ApplicationStatus Status { get; set; }                                 // Opened | Approved | Rejected | Abandoned | Withdrawn

    public bool IsTerminal => Status is ApplicationStatus.Approved
        or ApplicationStatus.Rejected
        or ApplicationStatus.Abandoned
        or ApplicationStatus.Withdrawn;

    public void Apply(ApplicationOpened e) { Id = e.ApplicationId; SubjectId = e.SubjectId; Status = ApplicationStatus.Opened; }
    public void Apply(PersonalInfoSubmitted _) => PersonalInfoComplete = true;
    public void Apply(BackgroundCheckConsentGiven _) => BackgroundCheckConsentGiven = true;
    public void Apply(DocumentVerified _) { /* update per-document state; recompute DocumentsSectionComplete */ }
    public void Apply(BackgroundCheckStatusReceived e) => BackgroundCheckStatus = e.Status;
    public void Apply(AdjudicationCaseDecided e) => AdjudicationOutcome = e.Outcome;
    public void Apply(PreAdverseActionNoticeIssued _) => FcraPhase = FcraPhase.PreAdverseIssued;
    public void Apply(ApplicationApproved _) => Status = ApplicationStatus.Approved;
    // ... etc.
}
```

The slice walk fleshes this out; the sidebar establishes the *shape* and ensures W003's vocabulary findings (V1–V8) map onto the state type cleanly.

### Event catalogue on the Application's process stream

W004 commits the full event catalogue at slice walk; sidebar pre-enumerates by category so the slices have a target:

| Category | Candidate events |
|---|---|
| **Initial** | `ApplicationOpened` (mints canonical UUIDv7 == driverProfileId on approval per ADR-013) |
| **Applicant inputs** | `PersonalInfoSubmitted`, `BackgroundCheckConsentGiven`, `DocumentUploaded`, `DocumentReuploaded`, `ApplicantWithdrew` |
| **Document-verification (translated from vendor)** | `DocumentVerified`, `DocumentRejected` (W003 V7 distinguishes from `ApplicationRejected`) — each carries `documentId` + translated reason code per W003 V8 |
| **Background-check (translated from vendor)** | `BackgroundCheckRequested`, `BackgroundCheckCaseOpened` (carries vendor case ID), `BackgroundCheckStatusReceived` (Pending → Clear \| Consider \| Suspended; W003 V5/V6 disambiguation) |
| **Adjudication (translated from human actor)** | `AdjudicationCaseQueued` (carries adjudicationCaseId; fires when BG-check returns Consider), `AdjudicationCaseClaimed` (carries adjudicatorId), `AdjudicationCaseDecided` (carries outcome enum) |
| **FCRA two-phase notice** | `PreAdverseActionNoticeIssued`, `DisputeWindowExpired` (Bruun temporal automation; W003 OQ-6), `FinalAdverseActionNoticeIssued` — both notice-issued events emit outbound ASB publications per ADR-014 (`onboarding.adverse-action-notice-required` or similar; slice walk picks slug) for the eventual Notifications BC to consume and dispatch via email/SMS. See OQ-10d for the publication-shape reasoning. |
| **Approval pathway** | `ApplicationApproved`, `ApplicantNotifiedOfApproval` (W003 V4 disambiguation), `DriverActivationPublished` (outbound to Driver Profile per W003 V1 / OQ-1: `onboarding.driver-approved`) |
| **Terminal events** | `ApplicationApproved` (happy), `ApplicationRejected` (sad — cause enum: document-terminal, FCRA-final-adverse, etc.), `ApplicationAbandoned` (Bruun no-activity timeout), `ApplicationWithdrawn` (applicant-initiated) |

Every continue handler carries both guards per the VitePress guide §5: `if (state.IsTerminal) yield break;` plus the step-level idempotency guard appropriate for the message.

### Scheduled timeouts — first-class via `OutgoingMessages.Delay`

The Process Manager pattern handles timeouts via `OutgoingMessages.Delay` / `Schedule` from handlers, no `IMessageBus` injection. Onboarding's scheduled timeouts:

| Timeout | Scheduled by | Fires | Resolution |
|---|---|---|---|
| **FCRA dispute window expiry** | `PreAdverseActionNoticeIssued` handler | `DisputeWindowExpired` self-message (window length from `OnboardingPolicyConfigured` per ADR-011) | Let-state-decide: handler checks if dispute was filed; if not, emits `FinalAdverseActionNoticeIssued` → `ApplicationRejected` |
| **Application abandonment** | `ApplicationOpened` handler (renewed on each significant activity event) | `ApplicationAbandonmentTimeout` self-message (window from policy) | Let-state-decide: handler checks if Application is terminal or has recent activity; if abandoned, emits `ApplicationAbandoned` |
| **Vendor no-response (BG-check)** | `BackgroundCheckCaseOpened` handler | `BackgroundCheckVendorNoResponse` self-message (window from policy) | Let-state-decide: handler checks if vendor has returned status; if not, escalates (Operations-side surface; slice walk decides specifics) |
| **Adjudicator claim expiry** | `AdjudicationCaseClaimed` handler | `AdjudicationClaimExpired` self-message | Let-state-decide: if no decision recorded, emits release-back-to-queue event |

The let-state-decide pattern is the cleanest ergonomic win of the Process Manager via Handlers approach (per VitePress §6 "Let state decide. Do not cancel the timer.") — every scheduled timeout reads current state and yields no events if the timer is no longer relevant.

### Multi-vendor ACL integration

Three vendor edges (per W003 §5.1 cross-cutting pattern, generalizing ADR-006):

- **OIDC provider** (referenced from Identity per ADR-006) — Identity publishes `identity.driver-registered`; Onboarding's intake slice consumes.
- **Document-verification vendor** — Translation slice receives vendor response; emits `DocumentVerified` or `DocumentRejected` onto the Application's process stream after translation per W003 V8 enum.
- **Background-check vendor** — Translation slice receives vendor webhook; emits `BackgroundCheckStatusReceived` onto the Application's process stream after translation (vendor's "candidate" vocabulary translates to CritterCab's actor vocabulary at the boundary per W003 §5.1).

Each translation slice is an ACL boundary; the translated event is a fact appended to the process stream. This is the absorption of W003 §5.1's multi-vendor ACL pattern *inside* the Process Manager pattern — surfaces in §11 as the paired ADR candidate with the Process Manager ADR.

### Cross-BC identifier — locked

`applicationId == driverProfileId` shared canonical UUIDv7 per ADR-013 (grill #1 outcome). Minted by Onboarding at `ApplicationOpened`. Inherited by DriverProfile on approval activation. `subjectId` (Identity-vocabulary) flows alongside as the actor handle. Onboarding becomes the fourth participant in CritterCab's shared-canonical-ID chain.

### What's split off entirely (not part of Onboarding's process stream)

- **Identity's authentication / OIDC subject** — separate BC per ADR-006.
- **Driver Profile's post-activation lifecycle** — separate BC; Onboarding hands off via the `onboarding.driver-approved` ASB business event.
- **Vehicle as a separately-modeled sub-domain** — W003 B7; deferred per scope decision A (treat vehicle as a document type for now).
- **Operations BC's adjudicator-tooling surface** — Adjudicator queue projection (`AdjudicatorQueueView*`) reads from Onboarding process streams but the queue UI / claim flow / SLA tracking is Operations' concern.
- **High-frequency operations metrics** — Operations BC; Onboarding publishes facts on its process streams that Operations projections aggregate.

### ADR-evidence framing

The sidebar's output explicitly captures:

- **ADR-012 (aggregate-per-invariant) — NOT a third evidence point.** Onboarding is a Process Manager, not an aggregate-cluster. Evidence base stays at W001 + W002. Honest reframe.
- **ADR-013 (shared cross-BC identifier) — fourth participant in the canonical-ID chain.** Reinforces the pattern; Onboarding's graduation handoff fits the same shape as Dispatch → Trips.
- **New §11 candidate #1: Process Manager via Handlers as third CritterCab modeling pattern.** Sidebar's output is the first canonical evidence point.
- **New §11 candidate #2: Multi-vendor ACL pattern absorbed inside the Process Manager.** Three vendor edges translate at the boundary; pattern integrated, not freestanding.

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
| V3 | "Adjudication case" naming | Confirm at slice walk; sidebar's process-stream event-shape decision commits the noun as a correlation field on `AdjudicationCase*` events. |
| V4 | Three "approved" moments need distinct names | Encode: `ApplicationApproved` (process-stream terminal event) → `ApplicantNotifiedOfApproval` (notification side effect on process stream) → `DriverActivationPublished` (outbound `onboarding.driver-approved` to Driver Profile). |
| V5 | "Case" homonym (vendor case vs. adjudication case) | Encode as `BackgroundCheckCase*` and `AdjudicationCase*` event prefixes (BackgroundCheckCaseOpened, AdjudicationCaseQueued, etc.) — full prefixes prevent homonym in event names. Both are correlation IDs on the Application's process stream, not separate aggregates. |
| V6 | Document state "under-review" → "under-verification" | Rename. Pre-walk vocabulary scan confirms no collision. |
| V7 | "Rejected" — per-document vs. application-terminal | Encode as `DocumentRejected` (per-document event) vs. `ApplicationRejected` (application-terminal event). |
| V8 | Vendor reason-code normalization enum | Define canonical CritterCab vocabulary at the relevant Translation slice (likely document-verification slice + background-check Consider slice). |

### 3. Open questions (W003 §5.3 — 14 W004-scoped, 5 deferred + OQ-14 escalated)

W004's primary work. Pre-leans:

| OQ | Question | Lean |
|---|---|---|
| OQ-1 | Topic name for approval event | `onboarding.driver-approved` (per V1). |
| OQ-2 | Document-state storage model | **Events on the Application's process stream** with `documentId` as correlation field (per Process Manager sidebar). No separate Document aggregate, no sub-entity — per-document state derives from `Apply(DocumentUploaded)`, `Apply(DocumentVerified)`, `Apply(DocumentRejected)`, `Apply(DocumentReuploaded)` on `ApplicationState`. |
| OQ-3 | Document history persistence | Event-sourced on the Application's process stream — history *is* the event log filtered by `documentId`. Re-upload sequence (Story 2) reads as event ordering. |
| OQ-4 | Adjudication-case storage | **Events on the Application's process stream** with `adjudicationCaseId` as correlation field (per Process Manager sidebar). No separate AdjudicationCase aggregate. Adjudicator queue projection (`AdjudicatorQueueView*`) is a multi-stream projection that reads `AdjudicationCaseQueued` events from any Application process stream. |
| OQ-5 | Pre-adverse vs. final adverse-action events | **Lean: two distinct events** (`PreAdverseActionNoticeIssued`, `FinalAdverseActionNoticeIssued`) — different consumer interest, different audit shape. |
| OQ-6 | Window-expiry firing the final adverse-action | **Lean: Bruun temporal automation** with `AdverseActionDisputeWindow*` todo-list view and `now() >= windowExpiresAt` trigger. Third Bruun pattern instance in CritterCab. |
| OQ-7 | Three "approved" moments distinct events | Yes, per V4 (already covered). |
| OQ-8 | Document state "under-verification" rename | Yes, per V6 (already covered). |
| OQ-9 | Vendor reason-code normalization enum | Define inline at relevant slice, per V8 (already covered). |
| OQ-10a | `ApplicationRejected` (terminal) outbound publication | **Lean: none.** Symmetric with W003 B2 (no DP creation on rejection terminal). FCRA notices are direct-to-applicant (handled by OQ-10d, not by `ApplicationRejected` itself). No push consumer identified. T&S reads via projection (OQ-10e). |
| OQ-10b | `ApplicationAbandoned` (Bruun timeout terminal) outbound publication | **Lean: none.** Parallel reasoning to OQ-10a. No DP creation, no FCRA (abandonment isn't an adverse action), no T&S push concern. Operations metrics read via `OnboardingFunnelView` projection. |
| OQ-10c | `ApplicationWithdrawn` (applicant-initiated terminal) outbound publication | **Lean: none.** Parallel reasoning to OQ-10a/b. No DP, no FCRA, no T&S push concern. |
| OQ-10d | FCRA notice events (`PreAdverseActionNoticeIssued`, `FinalAdverseActionNoticeIssued`) outbound publication for delivery | **Lean: publish to ASB per ADR-014** as `onboarding.adverse-action-notice-required` (or similar; slice walk picks exact slug). Consumer pending: **Notifications BC workshop (forward-constraint generated)**. Reasons: (a) reference-architecture alignment — BCs communicate via business events, not internal `IFcraNoticeSender` calls; (b) Process Manager pattern alignment — the process stream captures facts, downstream effects (notification delivery via SendGrid/Twilio) are modeled as cross-BC publications; (c) topic naming convention is already locked by ADR-014, so we incur zero new architectural debate; (d) precedent — Dispatch's `dispatch.ride-assigned` was committed before Trips' workshop ran; same shape inverted here (publication committed before consumer BC is workshopped). |
| OQ-10e | Trust & Safety future consumption pattern (forward-constraint shape) | **Lean: pull via projection, not push via publication.** Forward-constraint generated for T&S eventual workshop: T&S consumes Onboarding's rejection / abandonment / withdrawal facts by reading `OnboardingTerminalFactsView` (or equivalent multi-stream projection); Onboarding does not publish to T&S directly. Reflects the Process Manager pattern's natural shape — process stream is source of truth; projections derive views; consumers read projections. Push-publication only if T&S's eventual workshop specifies it. |
| OQ-11 | Re-application policy | Out of scope (scope decision A). Flag as parking-lot. |
| OQ-12 | Driver Profile creation timing | **Lean: approval push** (per B3 lean above). Onboarding publishes; DP reacts and creates. |
| OQ-13 | Vehicle as sub-domain | Out of scope (scope decision A). Flag as parking-lot. |
| OQ-14 | Suspension / reinstatement / deactivation | Out of scope (vision-doc-escalated). Not flagged as parking-lot — already lives at vision-doc level. |

### 4. Cross-cutting pattern (W003 §5.1 — multi-vendor ACL aggregator)

W003 surfaced three vendor edges sharing the same ACL-translation pattern (OIDC via Identity, document-verification, background-check) and named the pattern explicitly. **Expected W004 handling:** absorb as integration architecture *inside* the Process Manager pattern (per §Modeling-pattern sidebar) and surface in §11 as a paired ADR candidate with the Process Manager ADR — together they delineate "how Process Managers integrate with multi-vendor external boundaries in CritterCab." Trigger to author both ADRs: W004 ships with the Process Manager shape; first Onboarding implementation slice exercises it. Do not author either ADR inside W004's PR.

The workshop artifact's §X "DS findings handled" section consolidates the above into a single audit surface. Becomes the convention for future DS-fed EM workshops.

---

## ADR triggers that fire at this workshop

Four locked ADRs gain evidence; one locked ADR is honestly *not* a new evidence point; two new candidates expected.

**Locked ADRs gaining new evidence:**

- **ADR-011 (configuration-as-events bootstrap)** — Onboarding's policy slice (`OnboardingPolicyConfigured`) is the third BC adopting the pattern. Onboarding's tunable parameters include FCRA dispute window length, application-abandonment timeout, vendor no-response timeouts, document-rejection retry budgets, adjudicator-claim expiry windows. Confirms.
- **ADR-013 (shared cross-BC identifier)** — `applicationId == driverProfileId` shared canonical UUIDv7 per grill #1 outcome. Onboarding becomes the fourth participant in CritterCab's shared-canonical-ID chain (after Dispatch → Trips → Pricing → Ratings). Reinforces; not a divergent shape.
- **ADR-014 (ASB topic naming)** — `onboarding.driver-approved` is the third instance after Dispatch's `dispatch.ride-assigned` and Trips' four outbound topics. Confirms.
- **ADR-015 (driver-app projection timing budget)** — Onboarding's approval → driver-app transition ("application pending" → "ready to drive") is the second instance. Likely softer numeric budget than Trips' p95 < 200ms (the user is not in a time-pressured in-progress workflow; driver may not even have the app open). Slice walk picks a number with explicit measurement endpoints per ADR-015's template.

**Locked ADR explicitly NOT a new evidence point:**

- **ADR-012 (aggregate-per-invariant)** — Onboarding is a Process Manager via Handlers instance, not an aggregate-cluster. The ADR-012 pattern remains W001 + W002 only; Onboarding is the *first instance of a different pattern* (see new candidate #1 below). §11 captures the honest reframe: ADR-012's evidence base is unchanged at two instances.

**Expected new §11 ADR candidates (paired):**

- **NEW candidate #1 — Process Manager via Handlers as CritterCab's third modeling pattern.** Sibling to ADR-012's aggregate-per-invariant (W001 + W002 evidence). Delineates when to reach for Process Manager via Handlers vs. when to model with an aggregate-cluster — primary signal is *coordination-dominance* (parallel external events, temporal automation, multiple distinct terminals including abandonment, weak parent-level invariants). Evidence so far: Onboarding (W004). Trigger to author: first Onboarding implementation slice exercises the pattern. Likely future instances: Operations BC, Trust & Safety BC, possibly Payments lifecycle.
- **NEW candidate #2 — Multi-vendor ACL pattern absorbed inside the Process Manager.** Generalizes ADR-006's OIDC-as-ACL stance to a project-wide pattern covering all third-party vendor edges; positions the integration as Translation slices feeding the Process Manager's process stream rather than as a freestanding architectural feature. Evidence so far: Identity ↔ OIDC (ADR-006), Onboarding ↔ document-verification (W003 Story 2), Onboarding ↔ background-check (W003 Stories 1, 3). Three instances. **Authored as a separate paired ADR** alongside candidate #1 (one-decision-per-ADR convention; see grill #4 reasoning). Trigger to author: same as candidate #1.

The workshop's §11 should re-list these with post-workshop status: *evidence added, locked pattern confirmed* (for ADR-011, ADR-013, ADR-014, ADR-015) / *honestly not a new evidence point, reframe documented* (for ADR-012) / *new candidate surfaced, separate-paired-ADR authoring committed, trigger pinned to first implementation slice* (for both new candidates).

---

## Cast

- **Onboarding** — the workshop's primary BC.
- **Identity** — upstream sender. Pre-committed per ADR-006. Onboarding consumes `identity.driver-registered` at intake. Identity's full event model is a separate (pending) workshop; Onboarding treats it as a known signal with a forward-constraint shape.
- **Driver Profile** — downstream consumer. Receives `onboarding.driver-approved` and creates / activates the driver. Lifecycle-start timing (B3) settles in W004 with downstream implications for DP's eventual workshop.
- **Document-verification vendor** — external actor. Translation slice; ACL boundary per W003 B4 + the multi-vendor ACL candidate.
- **Background-check vendor** — external actor. Translation slice with two response patterns: synchronous Pending ack, async webhook delivery of Clear / Consider / Suspended. The Consider response triggers human adjudication (W003 Story 3 Phase 4).
- **Onboarding adjudicator** — manual-human actor inside Onboarding BC (per W003 B5). First in any CritterCab workshop. Under the Process Manager framing, the adjudicator's decision arrives as an integration event (`AdjudicationCaseClaimed`, `AdjudicationCaseDecided`) that gets appended to the Application's process stream after translation — *not* a separate aggregate's actor. The adjudicator's queue UI / claim flow / SLA tracking is Operations BC's concern; Onboarding owns the events.
- **Applicant** — the human actor in the prospective driver → applicant → driver vocabulary lifecycle (per W003 §5.1). Vocabulary transition at application begin; second transition at Driver Profile activation (lives outside Onboarding's terminal). Rejection terminal: applicant remains "applicant" — no "rejected applicant" sub-state.
- **Operations** — speculative downstream consumer of adjudicator-tooling concerns. Modeled as a forward-constraint generator if W004 surfaces adjudicator-queue projection concerns that imply Operations-side tooling; not modeled as an active actor in any slice.
- **Notifications (future BC)** — eventual consumer of `onboarding.adverse-action-notice-required` publications (per OQ-10d / grill #3). Not currently in CritterCab's vision-doc tentative-BC list; surfaces in W004 only as a forward-constraint shape. The actual delivery channel (SendGrid, Twilio, etc.) is Notifications BC's concern, not Onboarding's. W004 commits the publication shape; the consumer's workshop addresses how to consume.

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
11. **Wolverine [Process Manager via Handlers guide](https://wolverinefx.net/guide/durability/marten/process-manager-via-handlers.html) — the canonical reference for Onboarding's modeling shape.** Read in full. Pay particular attention to: §1 (Saga-vs-Process-Manager picker — Onboarding hits every "pick Process Manager" bullet), §2 (building blocks — `IEventStream<T>`, `[AggregateHandler]`, `MartenOps.StartStream`, `OutgoingMessages.Delay`), §3 (the recipe — step-by-step), §5 (friction points — be aware before the slice walk), §6 (when to use Saga instead — calibrate Onboarding's choice against this).
12. **Wolverine [`ProcessManagerViaHandlers` sample project](https://github.com/JasperFx/wolverine/tree/main/src/Samples/ProcessManagerViaHandlers)** — worked code companion to the guide. Skim the `OrderFulfillment/` folder (state, events, commands, handlers) for the shape Onboarding's slice walk produces; the test suite shows the unit-vs-integration pattern Onboarding's slices will mirror.
13. **[`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md)** — structural rules. Identity-as-ACL is load-bearing for the multi-vendor ACL candidate.
14. **ADRs [011](../../decisions/011-configuration-as-events-bootstrap.md), [012](../../decisions/012-aggregate-per-invariant.md), [013](../../decisions/013-shared-cross-bc-identifier.md), [014](../../decisions/014-asb-topic-naming-convention.md), [015](../../decisions/015-driver-app-projection-timing-budget.md), [006](../../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md)** — all six locked; reference during sidebar and ADR-triggers slice. ADR-012 reference is for the honest reframe (Onboarding is not a third evidence point); the other five apply directly.
15. **[`docs/context-map/README.md`](../../context-map/README.md)** — edge #6 (Onboarding → Driver Profile) carries the W003 amendment. Further amendments possible if W004 surfaces new edge shape (e.g., the Operations forward-constraint).
16. **[`docs/research/methodology-log.md`](../../research/methodology-log.md)** — recent entries on two-layer fidelity (003) and forward-constraints (004). W004 may produce entry 005 if a cross-cutting observation surfaces about DS-as-upstream input or about Process Manager as a modeling pattern.
17. **[`docs/decisions/004-design-phase-workflow-sequence.md`](../../decisions/004-design-phase-workflow-sequence.md)** — design-return cadence rule that triggered this session.
18. **[`docs/prompts/workshops/002-trips-event-modeling.md`](./002-trips-event-modeling.md)** — second-EM-workshop prompt for shape comparison; closest template.
19. **[`docs/prompts/README.md`](../README.md)** — prompts conventions including the new `## Spec delta` cadence.

---

## Working pattern

Same interactive cadence as Workshop 002, with three W004-specific adaptations:

- **Lock scope at session start.** Locked to Option A (tight — exact mirror of W003's three stories); confirm with user before sidebar.
- **Run the modeling-pattern + ApplicationState sidebar (§Modeling-pattern sidebar above) before Slice 1.** Output captured as §3 "Modeling pattern and process state" of the workshop artifact. State ADR-012 honest-reframe + ADR-013 fourth-instance evidence framing explicitly. Capture `ApplicationState` design, event-catalogue scaffolding, terminal-event enumeration, and scheduled-timeout enumeration. Include the pre-walk vocabulary scan against W001 + W002 (per W002 §14.6 #1).
- **Walk slices section-by-section.** Spine ordering follows W003's three-story narrative: intake (W003 Story 1 steps 1–4 → Onboarding intake from Identity), personal-info + consent, document upload + verification (happy + recovery paired), background check (Clear + Consider→adjudicate paired), FCRA two-phase notice (pre-adverse + window-expiry temporal automation + final adverse, paired in narrative order per W001 §12.6 #6), approval terminal + notification + DP handoff, application-rejected terminal (no DP publication).
- **Pair open questions with leaning opinions** per §12.7 calibration. Most W003 OQs have prompt-authored leans above; surface departures explicitly.
- **Name candidate projections from Slice 1** per W001 §12.6 #1. Onboarding's expected projections: `ApplicantDashboardView` (per-applicant status), `AdjudicatorQueueView*` (todo-list for adjudication cases pending review), `AdverseActionDisputeWindow*` (todo-list for FCRA window-expiry temporal automation), `OnboardingFunnelView` (operations metrics — counts per stage, conversion rate, drop-off analysis).
- **Number sub-slices explicitly** when cascades occur (e.g., document upload + verification as 4.4a / 4.4b for happy vs. recovery).
- **Pair temporal-automation slices with their feeder slice in narrative order** per W001 §12.6 #6. FCRA dispute-window temporal automation slice immediately follows the pre-adverse-action notice slice.
- **Defer protobuf authorship** per W001 §12.6 #4. Name the protos needed; capture as a post-workshop authorship session under ADR-009.
- **§X "DS findings handled"** — at session close, walk the four W003 finding categories (BC findings B1–B7, vocabulary V1–V8, OQs OQ-1 through OQ-13, cross-cutting pattern). Each gets explicit disposition.
- **§X+1 "Forward-constraints generated"** — consolidate any outbound forward-constraints W004 generates on un-modeled BCs' future workshops (Identity, Driver Profile, possibly Operations, possibly Trust & Safety). Per W002 §14.6 #2.
- **§11 ADR Candidates** — re-list locked ADRs (011, 013, 014, 015 with new evidence; 012 with honest reframe); surface two new paired candidates (Process Manager via Handlers as third CritterCab modeling pattern; multi-vendor ACL pattern absorbed inside the Process Manager).
- **§12-equivalent retrospective** — same nine-subsection shape as W001 §12 / W002 §14. Pay attention to **two** methodology questions: (1) did DS-as-upstream produce materially higher-quality EM artifact than narratives-only? (2) did Process Manager via Handlers fit Onboarding's shape as predicted by the VitePress guide, or did W004 surface friction the guide doesn't capture? Conditional methodology log entry 005 depends on the answers to one or both.
- **Commit only after explicit sign-off** per phase.

---

## Deliverable plan

| File | Status | Purpose |
|---|---|---|
| `docs/workshops/004-onboarding-event-model.md` | New | Workshop artifact, structurally parallel to W002 with one section adapted. Includes §3 "Modeling pattern and process state" sidebar output (replaces W002 §3 Aggregate Identity), §X "DS findings handled," §X+1 "Forward-constraints generated," §11 ADR Candidates re-list + two new paired candidates (Process Manager + Multi-vendor ACL), §12-equivalent nine-subsection retrospective. |
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

## Methodology questions to resolve in the retro

W004 is CritterCab's first event modeling workshop with DS as the primary upstream input *and* the first instance of the Process Manager via Handlers pattern. The retro should answer two focused questions alongside the standard nine-subsection close-out:

**Q1. Did DS-as-upstream produce materially higher-quality EM artifact than narratives-only would have?**

Evidence shape: count vocabulary surprises resolved without re-litigation, count BC findings encoded as slice invariants, count OQs answerable with lean rather than requiring fresh debate. Compare against W002's narrative-pair upstream input. If the answer is "yes, materially," that's the trigger for considering DS as a default pre-EM step for vocabulary-rich BCs going forward.

If the answer is "no, equivalent" or "DS value was front-loaded and EM didn't reuse it," that's an equally valuable finding — DS earns its keep on its own terms (per W003 retro) but doesn't necessarily warrant a permanent pre-EM placement.

**Q2. Did Process Manager via Handlers fit Onboarding's shape as predicted by the VitePress guide, or did W004 surface friction the guide doesn't capture?**

Evidence shape: which of the VitePress §5 friction points (no single home, distributed completion logic, 2N guard lines, start-handler asymmetry, etc.) bit hardest during the slice walk; whether the let-state-decide pattern for timeouts felt as clean in modeling as the guide claims; whether `ApplicationState` ended up cleaner or messier than the OrderFulfillmentState sample; whether any slice surfaced a need to reach for DCB (`[BoundaryModel]` per VitePress §7) for cross-stream invariants. If the answer is "fit as predicted with minor friction," the §11 Process Manager ADR candidate proceeds to authoring as-is. If the answer is "surfaced friction the guide doesn't capture," the ADR captures the additional friction so future Process Manager instances inherit the learning.

Conditional methodology log entry 005 may capture *either* answer if a cross-cutting observation meeting entry 001's three criteria surfaces. Both answers could trigger separate log entries if both yield cross-cutting insight.

---

## Starting move

**Branch state at session start:** You are working on `workshop/004-onboarding-event-modeling`, four commits ahead of `main`. The branch is pushed to origin. No PR is open yet. This session's workshop output (workshop artifact, retrospective, README index updates, optionally methodology log entry 005, optionally context-map amendment) ships in further commits on this same branch and lands as **one combined PR** per the path-2 decision made during prompt-authoring (prompt + workshop output bundled). The four pre-existing commits encode the prompt's authoring and grilling history; do not amend or rewrite them.

When the new session begins:

1. **Read the orientation files in order** (§Orientation files above). W003 § 4 + § 5 are load-bearing — read in full. The §Grill-with-docs resolution history table above summarizes the four locked decisions the prompt carries; treat them as inherited context, not as open questions to re-litigate.
2. **Confirm scope.** Locked at prompt-authoring time to Option A (tight — exact mirror of W003's three stories). Validate with user; if any movement, surface as the first decision before the sidebar runs.
3. **Run the pre-walk vocabulary scan** against W001 + W002 vocabulary (per W002 §14.6 #1). Confirm no new collisions beyond W003's V1–V8.
4. **Run the modeling-pattern + ApplicationState sidebar** (§Modeling-pattern sidebar above). ~30–45 minutes (longer than W002's because the pattern is new to CritterCab and the state-type sketch wants depth). Capture output as §3 "Modeling pattern and process state" of the workshop artifact. State the Process Manager pattern lock, the `ApplicationState` design, the event catalogue scaffolding, the terminal-event enumeration, the scheduled-timeout enumeration, the multi-vendor ACL integration framing, and the ADR-012 honest-reframe + ADR-013 fourth-instance evidence framing explicitly.
5. **Propose the slice ordering.** Spine ordering follows W003's narrative structure: intake → personal-info + consent → document upload + verification (paired happy/recovery) → BG check (paired Clear/Consider→adjudicate) → FCRA two-phase notice (with temporal automation paired in narrative order) → approval terminal + DP handoff → application-rejected terminal. Translation-out slices come after the lifecycle spine. Configuration-as-events slice (`OnboardingPolicyConfigured`) slotted at the appropriate point per W001 §5.11 precedent — Onboarding's tunable parameters include FCRA dispute window length, abandonment timeout, vendor no-response timeouts, adjudicator-claim expiry.
6. **Walk slices.** Pause for sign-off after each. Calibrate cadence per W001 §12.6 #3 (heavy slices: modeling-pattern-impacted slices, multi-vendor ACL translation slices, FCRA temporal automation, terminal-and-compensation enumeration; lighter slices: confirmations of W003 already-locked vocabulary). Each slice produces events on the Application's process stream — the slice's modeling output is "this event, on this stream, carrying these correlation IDs, derived through this Apply method."
7. **At session close:** §X "DS findings handled" walk; §X+1 "Forward-constraints generated" consolidation; §11 ADR Candidates re-list; §12-equivalent retrospective; update `docs/workshops/README.md` (W004 list row + W003 follow-up closure + new follow-ups subsection); optionally append methodology log entry 005; optionally amend context map.

Don't batch the whole workshop into one output. Workshop sessions are interactive, slice by slice — W001 + W002 cadence is the precedent.
