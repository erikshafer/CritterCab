# Workshop 004 — Onboarding Event Model

**Status:** In progress (v0.1, 2026-05-27). Scope confirmed at Option A (tight — exact mirror of W003's three stories). Pre-walk vocabulary scan against W001 + W002 complete with zero blocking collisions. §3 Modeling pattern and process state sidebar locked: **Process Manager via Handlers** per the Wolverine VitePress guide. §4 Ubiquitous Language bootstrap from W003 §5.1 + §3 sidebar. §5 Event List scaffolded by category. Slice walk pending (11 slices proposed; 10 base + 1 sub-slice paired).
**Started:** 2026-05-27.
**Facilitator / modeler:** Erik Shafer (solo).
**AI collaborator:** Claude (Opus 4.7), rotating through Facilitator, Developer, Skeptic, and Domain-Expert personas per `docs/research/event-modeling-workshop-guide.md` Lesson 8.
**Triggering prompt:** [`docs/prompts/workshops/004-onboarding-event-modeling.md`](../prompts/workshops/004-onboarding-event-modeling.md).
**Methodology reference:** `docs/research/event-modeling-workshop-guide.md`.
**Adjunct patterns:** `docs/research/agents-in-event-models.md` (Klefter translation-decision events; Bruun temporal-automation slice pattern).
**Pattern reference (NEW for W004):** Wolverine [Process Manager via Handlers guide](https://wolverinefx.net/guide/durability/marten/process-manager-via-handlers.html) and the [`ProcessManagerViaHandlers` sample project](https://github.com/JasperFx/wolverine/tree/main/src/Samples/ProcessManagerViaHandlers) — the canonical reference for Onboarding's modeling shape. First in-repo reference implementation of the pattern.
**Structural constraints honored:** `docs/rules/structural-constraints.md` (ADR-002, ADR-005, ADR-006, ADR-009).
**Workshop 001 + 002 inheritance:** All conventions from [`001-dispatch-event-model.md`](./001-dispatch-event-model.md) and [`002-trips-event-model.md`](./002-trips-event-model.md) carry forward without re-litigation, including W001 §12.6 all-five adjustments and W002 §14.6 all-five additional adjustments. This workshop adds an additional §X "DS findings handled" section (the convention for future DS-fed EM workshops).
**Upstream input (NEW for W004):** [`docs/workshops/003-onboarding-domain-story.md`](003-onboarding-domain-story.md) — first EM workshop to consume a DS artifact as primary upstream input. W003's three stories scaffold W004's slice ordering; W003 §5.3's 14 W004-scoped open questions land in §X "DS findings handled."

---

## 1. Session Log

| Session | Date | Duration | Phases covered | Notes |
|---|---|---|---|---|
| 1 | 2026-05-27 | Pending completion | Scope confirmation (Option A); pre-walk vocabulary scan; §3 modeling-pattern + ApplicationState sidebar; §4 UL bootstrap; §5 Event List scaffold; slice ordering proposal | First EM workshop to consume a DS artifact as primary upstream input. First in-repo Process Manager via Handlers instance. Workshop convened per ADR-004's design-return cadence rule after W003 PR closed; closes W003 §5.3's "Workshop 004 — Onboarding event model" follow-up. Four prompt-authoring grills locked the substance ahead of session: PM via Handlers (grills #2/#4); `applicationId == driverProfileId` shared canonical UUIDv7 per ADR-013 (grill #1); OQ-10 split into a/b/c/d/e with Notifications forward-constraint (grill #3); two separate paired §11 ADR candidates per CritterCab's one-decision-per-ADR convention (grill #4). |

---

## 2. Scope Statement

### 2.1 In scope

The Onboarding bounded context's **driver-vetting lifecycle**, from intake of Identity's `identity.driver-registered` business event through either (a) terminal success via approval + Driver Profile activation, (b) terminal failure via background-check rejection + FCRA two-phase notice, (c) terminal abandonment via no-activity timeout, or (d) terminal withdrawal via applicant-initiated action. Specifically:

- **Translation-in intake from Identity.** Consume `identity.driver-registered` (forward-constraint on Identity's eventual workshop); create the Application's process stream via `MartenOps.StartStream<ApplicationState>` with canonical UUIDv7 minted by Onboarding (== future `driverProfileId` per ADR-013).
- **Applicant input gates.** Personal info; SSN + FCRA-compliant background-check consent; document upload + re-upload.
- **Document verification (Translation-in pair).** Submit each document to the document-verification vendor; translate vendor response (verified | rejected with translated reason code per W003 V8) into events on the Application's process stream. Per-document state machine per `ApplicationState.Documents` dictionary.
- **Background-check (Translation-in pair).** Submit to background-check vendor with SSN + DOB + name under the consent authorization; translate vendor's sync ack (`BackgroundCheckCaseOpened`) and async webhook status updates (`BackgroundCheckStatusReceived` with Pending → Clear | Consider | Suspended enum) into events on the process stream.
- **Adjudication (translated from human actor).** When BG-check returns Consider, queue an adjudication case for the Onboarding adjudicator; record claim, decision (Clear | Reject) as events on the process stream. First manual-human actor in any CritterCab workshop.
- **FCRA two-phase notice + scheduled dispute-window expiry.** On adjudicator-Reject, emit pre-adverse-action notice + outbound ASB publication for Notifications; schedule dispute-window expiry self-message via `OutgoingMessages.Delay` (window length from `OnboardingPolicyConfigured` per ADR-011); on let-state-decide resolution if no dispute filed, emit final-adverse-action notice + outbound publication + `ApplicationRejected`.
- **Approval pathway + Translation-out to Driver Profile.** When all gates clear (personal-info + consent + documents-section + BG-check-clear), the handler observing prerequisites emits `ApplicationApproved` (terminal) → `ApplicantNotifiedOfApproval` (side effect) → `DriverActivationPublished` (outbound `onboarding.driver-approved` to Driver Profile per ADR-014). Three distinct events per W003 V4 disambiguation.
- **Application-rejected / -abandoned / -withdrawn terminals.** Rejected terminal has NO outbound DP publication (W003 §5.2 B2 asymmetry). Abandoned via PM let-state-decide on `ApplicationOpened`-scheduled timeout. Withdrawn via applicant-initiated command.
- **Onboarding policy configuration as events.** Operator-tunable parameters event-sourced via `OnboardingPolicyConfigured`. Bruun configuration-as-events pattern; third BC adopting ADR-011.

### 2.2 At the boundary (modeled as Translation slices)

- **Identity → Onboarding (intake).** `identity.driver-registered` over ASB. Identity's full event model is a separate (pending) workshop; Onboarding treats the topic as a forward-constraint contract.
- **Onboarding ↔ document-verification vendor.** Per W003 B4. ACL translation at the boundary. Vendor reason-code strings normalize to a canonical CritterCab enum per W003 V8.
- **Onboarding ↔ background-check vendor.** Per W003 B4. ACL translation at the boundary. Vendor's "candidate" vocabulary translates to CritterCab's "applicant" at the boundary; vendor's status enum (Pending | Clear | Consider | Suspended) carries as-is per W003 V5/V6.
- **Onboarding → Notifications (FCRA adverse-action delivery).** Per W003 OQ-10d / grill #3. Outbound ASB publication `onboarding.adverse-action-notice-required` (or slice-walk-picked slug) on both notice-issued events. Consumer pending — Notifications BC is not currently in vision-doc's tentative-BC list; forward-constraint surfaces here.
- **Onboarding → Driver Profile (approval terminal).** Cross-BC publication via ASB `onboarding.driver-approved` per W003 V1 / OQ-1. Asymmetric — no equivalent on rejection / abandonment / withdrawal terminals per W003 §5.2 B2 and OQ-10a/b/c leans.

### 2.3 Out of scope

- **Re-application policy** (W003 OQ-11). Deferred per scope decision A. Flagged as parking-lot if surfaces.
- **Vehicle as sub-domain** (W003 B7 / OQ-13). Deferred per scope decision A. Vehicle treated as a document type for W004.
- **Suspension / reinstatement / deactivation** (W003 OQ-14). Vision-doc-escalated; not flagged as parking-lot — already lives at vision-doc level.
- **Implementation code** — pure design. No C#, no Go, no proto edits.
- **ADR authorship** — locked ADRs gain evidence; new candidates named but not authored. Per W001 §12.6 #4 + ADR-009-style precedent.
- **Skill-file edits** — gaps surfaced here become DEBT.md rows or follow-up `tidy: skills` PR; not edited in-session.

### 2.4 Structural constraints honored

- All Identity / document-verification vendor / background-check vendor / Driver Profile / Notifications boundary crossings are gRPC calls or Wolverine messages over ASB — no shared databases, no in-process cross-service calls (ADR-002).
- Cross-BC business events go via Azure Service Bus per `<source-bc>.<event-name-kebab>` topic convention (ADR-005, ADR-014).
- No provider-specific identity details appear in Onboarding events or projections (ADR-006).
- New Protobuf contracts implied by the model are named in §10 rather than authored inline; proto authorship is a downstream task (ADR-009, per W001 §12.6 #4).

### 2.5 Decisions locked during scope-setting

| Decision | Resolution |
|---|---|
| Scope | Option A — tight, exact mirror of W003's three stories. Locked at prompt-authoring time. |
| Re-application | Out of scope (W003 OQ-11). |
| Vehicle as sub-domain | Out of scope (W003 B7 / OQ-13). Vehicle = document type for W004. |
| Suspension lifecycle | Out of scope (W003 OQ-14; vision-doc-escalated). |
| Modeling pattern | **Process Manager via Handlers** (locked at prompt-authoring per grills #2/#4). See §3. |
| Cross-BC identifier | `applicationId == driverProfileId` shared canonical UUIDv7 per ADR-013 (locked at prompt-authoring per grill #1). |
| FCRA outbound publication shape | `onboarding.adverse-action-notice-required` (or slice-walk-picked slug) to Notifications via ASB per ADR-014 (locked at prompt-authoring per grill #3 / W003 OQ-10d). |
| §11 ADR candidate authoring | Two separate paired ADRs (PM via Handlers + multi-vendor ACL absorbed inside it) per CritterCab's one-decision-per-ADR convention (locked at prompt-authoring per grill #4). |

---

## 3. Modeling pattern and process state

### 3.1 Why this section exists

Per W001 §12.6 #2 and W002 §3.1, the modeling-shape decision shapes every subsequent slice and is hard to retrofit cleanly. Workshops 001 and 002 used this pre-walk sidebar slot to settle aggregate identity — *what is the load-bearing invariant?*, *single aggregate or cluster?*, *sub-entities or sub-states?*. Onboarding's shape forces this section to do a different job: settle the **modeling pattern itself** before settling any state-machine substance. Onboarding is not aggregate-cluster-shaped; forcing it into ADR-012's vocabulary would impose invariant-protection framing on a coordination-dominated shape and lose the framework's first-class timeout primitives.

This section commits Onboarding to **Wolverine's Process Manager via Handlers pattern**, captures the `ApplicationState` process state's design, scaffolds the event catalogue on the Application's process stream, enumerates the four scheduled timeouts using the *let-state-decide* discipline, and frames the ADR evidence honestly (ADR-012 is *not* gaining a third evidence point here; two new paired candidates surface instead).

### 3.2 Why Process Manager — three framings considered

W003's characterization of Onboarding — *"multi-vendor ACL aggregator with multiple parallel gates and a long-running coordination surface punctuated by external events and time-based terminals"* (§5.1 + §5.2) — is a textbook Process Manager characterization. Three aggregate framings were considered during prompt-authoring grilling (grills #2 and #4 in the prompt) and the chosen shape rejected the other two:

| Framing | Why rejected (or selected) |
|---|---|
| **Plural aggregates** (Application + AdjudicationCase + BackgroundCheckCase as peer aggregates) | Rejected. Splits streams along *consistency surface* and *anti-corruption surface* lines that ADR-012 does not codify. Multiplies lifecycle bookkeeping — per-peer abandonment timers, compensation choreography, terminal cascades — for no domain-correctness benefit. The "consistency-surface" reasoning, on closer look, was a post-hoc justification for an aggregate split, not an honest reading of invariants. |
| **Sub-entity-on-Application** (RideRequest+Offer-style — Document, AdjudicationCase, BackgroundCheckCase as sub-entities on a single Application aggregate, all events appending to the Application stream) | Rejected. Closer to honest than plural aggregates but still imposes invariant-protection framing on a coordination-dominated shape. Loses the framework's first-class timeout primitives (`OutgoingMessages.Delay` / `Schedule`); FCRA dispute-window expiry would have to be modeled as a Bruun temporal automation against a todo-list view rather than as a scheduled self-message with let-state-decide resolution. The shape would work, but it's working against the framework's grain rather than with it. |
| **Process Manager via Handlers** | **Selected.** Single process stream per application. All coordination events from external sources (applicant inputs, vendor translations, adjudicator decisions, FCRA notices, Bruun temporal automations, terminal events) appending to one stream. `OutgoingMessages.Delay` / `Schedule` for FCRA window, abandonment, vendor no-response, adjudicator-claim timeouts. Let-state-decide pattern for scheduled timer resolution (no cancel-the-timer API needed). Terminal-state guard plus step-level idempotency guard on every continue handler. |

The deeper reframe that locked the selection (grill #4): *streams are the treasure, aggregates are a means to an end*. Onboarding's value is in the canonical, replayable, audit-grade record of "what happened to this application" across all its parallel coordination surfaces. The Process Manager via Handlers pattern is the framework's first-class way to model exactly that shape — every coordination event becomes a fact appended to the process stream after translation at the boundary; the process state is a projected snapshot used for decision-making, not the durable substrate.

### 3.3 Decision

**Onboarding is modeled as a Process Manager via Handlers** per the [Wolverine VitePress guide](https://wolverinefx.net/guide/durability/marten/process-manager-via-handlers.html) and the [`ProcessManagerViaHandlers` sample project](https://github.com/JasperFx/wolverine/tree/main/src/Samples/ProcessManagerViaHandlers).

Onboarding becomes the **first in-repo reference implementation** of the pattern across the Critter Stack — sibling pedagogical value to Dispatch's gRPC slices. This is pedagogically aligned with CritterCab's reference-architecture mission per `CLAUDE.md`: the project exists to demonstrate Wolverine's gRPC features *and* to make other Critter Stack patterns concrete via worked examples.

The pattern decision lands ahead of any specific state-machine substance. State shape, event catalogue, terminal events, and scheduled timeouts all derive from this pattern decision — they would have looked materially different under either of the rejected framings.

### 3.4 `ApplicationState` — the process state

The Process Manager pattern operates over a plain C# class whose `Apply(EventType e)` methods derive current snapshot from the stream. Marten's `FetchForWriting<ApplicationState>` loads the stream, replays it through these methods, and returns an `IEventStream<ApplicationState>` containing the projected state. The `[AggregateHandler]` middleware wires this for continue-handlers; start-handlers use `MartenOps.StartStream<ApplicationState>` instead (asymmetry per VitePress guide §3 / §4).

```csharp
public class ApplicationState
{
    // Required by Marten/FetchForWriting: registers the type as a document type.
    public Guid Id { get; set; }                        // canonical UUIDv7, == driverProfileId on activation per ADR-013
    public Guid SubjectId { get; set; }                 // Identity-vocabulary actor handle, carried alongside

    // Applicant input gates
    public bool PersonalInfoComplete { get; set; }
    public bool BackgroundCheckConsentGiven { get; set; }

    // Per-document state — derived; supports the W003 V6 "under-verification" vocabulary
    public Dictionary<Guid, DocumentState> Documents { get; set; } = new();
    public bool DocumentsSectionComplete =>
        Documents.Count >= RequiredDocumentCount &&
        Documents.Values.All(d => d == DocumentState.Verified);

    // Background-check gate
    public VendorBackgroundCheckStatus? BackgroundCheckStatus { get; set; }   // null | Pending | Clear | Consider | Suspended
    public Guid? AdjudicationCaseId { get; set; }                              // set on AdjudicationCaseQueued (BG-check Consider)
    public Guid? AdjudicatorId { get; set; }                                   // set on AdjudicationCaseClaimed
    public AdjudicationOutcome? AdjudicationOutcome { get; set; }              // null until adjudication terminal

    // FCRA two-phase notice phase
    public FcraPhase? FcraPhase { get; set; }                                  // null | PreAdverseIssued | DisputeWindowOpen | FinalAdverseIssued
    public DateTimeOffset? DisputeWindowExpiresAt { get; set; }                // carry-the-value per Bruun; survives policy changes

    // Activity timestamps (drive abandonment-timeout reset)
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset LastSignificantActivityAt { get; set; }              // bumped by applicant-input events

    // Terminal status
    public ApplicationStatus Status { get; set; }                              // Opened | Approved | Rejected | Abandoned | Withdrawn

    public bool IsTerminal => Status is
        ApplicationStatus.Approved or
        ApplicationStatus.Rejected or
        ApplicationStatus.Abandoned or
        ApplicationStatus.Withdrawn;

    // Apply methods — fleshed out per slice during the walk
    public void Apply(ApplicationOpened e) {
        Id = e.ApplicationId;
        SubjectId = e.SubjectId;
        Status = ApplicationStatus.Opened;
        OpenedAt = e.OpenedAt;
        LastSignificantActivityAt = e.OpenedAt;
    }
    public void Apply(PersonalInfoSubmitted e) { PersonalInfoComplete = true; LastSignificantActivityAt = e.OccurredAt; }
    public void Apply(BackgroundCheckConsentGiven e) { BackgroundCheckConsentGiven = true; LastSignificantActivityAt = e.OccurredAt; }
    public void Apply(DocumentUploaded e) { Documents[e.DocumentId] = DocumentState.UnderVerification; LastSignificantActivityAt = e.OccurredAt; }
    public void Apply(DocumentVerified e) { Documents[e.DocumentId] = DocumentState.Verified; LastSignificantActivityAt = e.OccurredAt; }
    public void Apply(DocumentRejected e) { Documents[e.DocumentId] = DocumentState.Rejected; LastSignificantActivityAt = e.OccurredAt; }
    public void Apply(DocumentReuploaded e) { Documents[e.DocumentId] = DocumentState.UnderVerification; LastSignificantActivityAt = e.OccurredAt; }
    public void Apply(BackgroundCheckStatusReceived e) => BackgroundCheckStatus = e.Status;
    public void Apply(AdjudicationCaseQueued e) => AdjudicationCaseId = e.AdjudicationCaseId;
    public void Apply(AdjudicationCaseClaimed e) => AdjudicatorId = e.AdjudicatorId;
    public void Apply(AdjudicationCaseDecided e) => AdjudicationOutcome = e.Outcome;
    public void Apply(PreAdverseActionNoticeIssued e) { FcraPhase = FcraPhase.PreAdverseIssued; DisputeWindowExpiresAt = e.WindowExpiresAt; }
    public void Apply(FinalAdverseActionNoticeIssued _) => FcraPhase = FcraPhase.FinalAdverseIssued;
    public void Apply(ApplicationApproved _) => Status = ApplicationStatus.Approved;
    public void Apply(ApplicationRejected _) => Status = ApplicationStatus.Rejected;
    public void Apply(ApplicationAbandoned _) => Status = ApplicationStatus.Abandoned;
    public void Apply(ApplicationWithdrawn _) => Status = ApplicationStatus.Withdrawn;
}

public enum DocumentState { UnderVerification, Verified, Rejected }
public enum ApplicationStatus { Opened, Approved, Rejected, Abandoned, Withdrawn }
public enum VendorBackgroundCheckStatus { Pending, Clear, Consider, Suspended }
public enum AdjudicationOutcome { Clear, Reject }
public enum FcraPhase { PreAdverseIssued, DisputeWindowOpen, FinalAdverseIssued }
```

**Refinements beyond the prompt's sketch** (committed in this sidebar):

1. **`Documents` as `Dictionary<Guid, DocumentState>`** — per-document state derived from event ordering, indexed by `documentId`. W003 OQ-2 / OQ-3 lean commits to no separate Document aggregate; this is the data shape that realizes the lean. `DocumentsSectionComplete` becomes derived rather than stored.
2. **`LastSignificantActivityAt`** — bumped by each applicant-input event. Drives the abandonment-timeout's let-state-decide check.
3. **`DisputeWindowExpiresAt` on the state** — carries the FCRA window's expiry from `PreAdverseActionNoticeIssued`. Bruun *carry-the-value* discipline applied a third time across CritterCab (W001 §5.4 → W002 §6.3 → W004 §3.4).
4. **`AdjudicationCaseId` and `AdjudicatorId` on the state** rather than as event-only correlation fields. Lets continue handlers reason about adjudication-in-flight without re-walking the stream.

The required `Guid Id` property is mandatory per Marten (PM-via-Handlers guide §3, step 1). The `IsTerminal` helper is a clean read-site for the 2N guard discipline (§3.6). Per W001 §12.5's Decider-pattern preference, mutable property assignment in `Apply` is the Marten norm for plain process-state types; immutable-record-with-`with` is appropriate for invariant-protected aggregates (W001/W002) but not for this pattern's process-state-as-projected-snapshot semantics.

### 3.5 Event catalogue on the Application's process stream

W003's three stories produce the following events scaffolded by category. Full payload shapes and decisions-locked tables fill in per slice during the walk.

| Category | Events | Notes |
|---|---|---|
| **Initial** | `ApplicationOpened` | Mints canonical UUIDv7 == `driverProfileId` on approval activation per ADR-013. `SubjectId` carried alongside. |
| **Applicant inputs** | `PersonalInfoSubmitted`, `BackgroundCheckConsentGiven`, `DocumentUploaded`, `DocumentReuploaded`, `ApplicantWithdrew` | Each bumps `LastSignificantActivityAt`. `ApplicantWithdrew` triggers `ApplicationWithdrawn` terminal in the same handler. |
| **Document-verification (translated from vendor)** | `DocumentVerified`, `DocumentRejected` | Each carries `documentId` + translated reason code per W003 V8. `DocumentRejected` is per-document (not application-terminal — W003 V7). |
| **Background-check (translated from vendor)** | `BackgroundCheckRequested`, `BackgroundCheckCaseOpened`, `BackgroundCheckStatusReceived` | `BackgroundCheckCaseOpened` carries vendor case ID (W003 V5 prefix discipline). Status enum: `Pending` → `Clear` \| `Consider` \| `Suspended`. |
| **Adjudication (translated from human actor)** | `AdjudicationCaseQueued`, `AdjudicationCaseClaimed`, `AdjudicationCaseDecided` | Queued fires when BG-check returns `Consider`. Decided carries outcome enum: `Clear` \| `Reject`. |
| **FCRA two-phase notice** | `PreAdverseActionNoticeIssued`, `DisputeWindowExpired`, `FinalAdverseActionNoticeIssued` | Both notice-issued events emit outbound ASB publications per ADR-014 (`onboarding.adverse-action-notice-required` or similar — slice walk picks exact slug) for the Notifications BC's eventual workshop to consume. Per W003 OQ-10d / grill #3. |
| **Approval pathway** | `ApplicationApproved`, `ApplicantNotifiedOfApproval`, `DriverActivationPublished` | Three distinct events per W003 V4 disambiguation. `DriverActivationPublished` is the outbound `onboarding.driver-approved` to Driver Profile. |
| **Terminal events** | `ApplicationApproved` (happy), `ApplicationRejected` (sad), `ApplicationAbandoned` (Bruun no-activity timeout), `ApplicationWithdrawn` (applicant-initiated) | Four distinct terminals. `ApplicationRejected` carries a cause enum (document-terminal, FCRA-final-adverse, etc.) per slice walk. |

### 3.6 The 2N guard discipline (terminal-state + step-level idempotency)

Per the Wolverine guide §3 step 5 and §5 friction-point #3, **every continue handler carries two guards**:

```csharp
public static IEnumerable<object> Handle(SomeEvent @event, ApplicationState state)
{
    if (state.IsTerminal) yield break;           // terminal-state guard — prevents late-arriving messages from corrupting finished processes
    if (state.SomeStepAlreadyHappened) yield break;  // step-level idempotency guard — prevents at-least-once redelivery from double-recording

    yield return @event;
    // optional: cascade to terminal events when prerequisites are satisfied
}
```

Both guards appear in **every** continue handler — the slice walk commits this discipline verbatim per slice. The guide's friction-point note that this is "mechanical and essential; a missed guard corrupts data" is taken seriously; sample handler tests should verify guard behavior explicitly.

**Start-handler asymmetry**: `ApplicationOpened`'s handler does *not* use `[AggregateHandler]`; it returns `(IStartStream, OutgoingMessages)` via `MartenOps.StartStream<ApplicationState>(...)`. Applying `[AggregateHandler]` to the start handler causes the middleware to short-circuit before the handler runs (guide §3 step 4 + §5 friction-point #4). The start slice (Slice 6.1) commits this explicitly.

### 3.7 Scheduled timeouts — first-class via `OutgoingMessages.Delay` / `Schedule`

The Process Manager pattern handles timeouts via `OutgoingMessages.Delay` from handlers — no `IMessageBus` injection, no `Cancel()` API to maintain. The let-state-decide discipline (guide §3 step 6, *"Let state decide. Do not cancel the timer."*) is committed for all four of Onboarding's timeouts:

| Timeout | Scheduled by | Self-message that fires | Let-state-decide resolution |
|---|---|---|---|
| **FCRA dispute window expiry** | `PreAdverseActionNoticeIssued` handler — `outgoing.Delay(new DisputeWindowExpired(...), policy.FcraDisputeWindow)` | `DisputeWindowExpired` | Handler checks if applicant disputed during the window; if not, yields `FinalAdverseActionNoticeIssued` + `ApplicationRejected`. If dispute was filed (or terminal already), yields nothing. |
| **Application abandonment** | `ApplicationOpened` handler (and re-scheduled on each significant activity event) — `outgoing.Delay(new ApplicationAbandonmentTimeout(...), policy.AbandonmentWindow)` | `ApplicationAbandonmentTimeout` | Handler checks `state.IsTerminal` and `state.LastSignificantActivityAt`; if recent activity has occurred since the timeout was scheduled, yields nothing (the let-state-decide pattern eliminates the need to cancel the prior timer). If genuinely abandoned, yields `ApplicationAbandoned`. |
| **Vendor no-response (BG-check)** | `BackgroundCheckCaseOpened` handler — `outgoing.Delay(new BackgroundCheckVendorNoResponse(...), policy.VendorNoResponseWindow)` | `BackgroundCheckVendorNoResponse` | Handler checks if `state.BackgroundCheckStatus` has been set since open; if so, yields nothing. If vendor genuinely silent, escalates per Operations BC concern (specific shape decided at slice walk). |
| **Adjudicator claim expiry** | `AdjudicationCaseClaimed` handler — `outgoing.Delay(new AdjudicationClaimExpired(...), policy.AdjudicatorClaimWindow)` | `AdjudicationClaimExpired` | Handler checks if `state.AdjudicationOutcome` is set or `state.AdjudicatorId` has changed; if so, yields nothing. If claim genuinely stale, yields a release-back-to-queue event (slice walk picks event name). |

The cleanest ergonomic win of PM via Handlers shows up here: **no Bruun todo-list view + asterisk-suffix projection + clock-rewind-glyph automation is needed for these timeouts.** The timer-as-self-message lives in Wolverine's outbox; the resolution happens in a continue handler that's a pure function over current state. The Bruun pattern is still applicable for the FCRA dispute-window's *projection-side* representation (`AdverseActionDisputeWindow*` todo-list for ops visibility) but is no longer the *mechanism* driving the timeout — that's the Process Manager's self-scheduled message.

### 3.8 Multi-vendor ACL integration

Per W003 §5.1 cross-cutting pattern and the new §11 paired ADR candidate (multi-vendor ACL absorbed inside the Process Manager). Three vendor edges, each a Translation slice:

| Vendor edge | Translation slice produces | Notes |
|---|---|---|
| **OIDC provider** (referenced from Identity per ADR-006) | Identity publishes `identity.driver-registered`; Onboarding's intake slice consumes and creates the Application stream via `MartenOps.StartStream` | Inherited ACL; not Onboarding's primary translation surface. |
| **Document-verification vendor** | Translation handler receives vendor response (webhook or polled); emits `DocumentVerified` or `DocumentRejected` onto the Application's process stream after vendor reason-code → CritterCab enum translation (W003 V8) | Per W003 §5.1 ACL pattern. Vendor "rejection reason" strings translate to a canonical CritterCab enum at the boundary. |
| **Background-check vendor** | Translation handler receives vendor webhook; emits `BackgroundCheckCaseOpened` (sync ack with vendor case ID) or `BackgroundCheckStatusReceived` (async status updates) | Per W003 §5.1 ACL pattern. Vendor's "candidate" vocabulary translates to CritterCab's "applicant" at the boundary; vendor's Pending/Clear/Consider/Suspended status enum carries as-is (W003 V5/V6 disambiguation already committed the enum). |

The translation is the ACL boundary; the translated event is a fact appended to the process stream. This is the absorption of W003 §5.1's multi-vendor ACL pattern *inside* the Process Manager pattern — surfaces in §11 as the paired ADR candidate alongside the Process Manager ADR (per grill #4: two separate paired ADRs).

### 3.9 Cross-BC identifier — locked

`applicationId == driverProfileId` shared canonical UUIDv7 per ADR-013 (grill #1 outcome). Minted by Onboarding at `ApplicationOpened`. Inherited by Driver Profile on approval activation. `SubjectId` (Identity-vocabulary) carries alongside as the actor handle, distinct from the lifecycle ID.

**Onboarding becomes the fourth participant in CritterCab's shared-canonical-ID chain** after Dispatch → Trips → (Pricing) → (Ratings). This reinforces ADR-013's pattern; the lifecycle "stages crossing BCs in a deterministic order" reading from ADR-013's scope statement applies cleanly to Onboarding → Driver Profile.

### 3.10 What's split off entirely (not part of Onboarding's process stream)

| Concern | Where it lives | Rationale |
|---|---|---|
| **Identity's authentication / OIDC subject** | Identity BC (ADR-006) | The provider-side translation is Identity's job; Onboarding receives `identity.driver-registered` as a known signal. |
| **Driver Profile's post-activation lifecycle** | Driver Profile BC | Onboarding hands off via `onboarding.driver-approved` and ceases ownership. DP's profile-state, vehicle roster, availability, etc. are not modeled here. |
| **Vehicle as a separately-modeled sub-domain** | Deferred (W003 B7 / scope decision A) | Treated as a document type for W004. Multi-vehicle scenarios + per-vehicle approval lifecycle remain a parking-lot item for a future vehicle BC consideration. |
| **Operations BC's adjudicator-tooling surface** | Operations BC | `AdjudicatorQueueView*` reads from Onboarding's process streams but the queue UI / claim flow / SLA tracking is Operations' concern. Onboarding owns the events; Operations owns the tooling. |
| **Notifications delivery (email/SMS)** | Notifications BC (eventual) | FCRA notice events emit `onboarding.adverse-action-notice-required` publications per OQ-10d / grill #3. Notifications consumes and dispatches via SendGrid/Twilio. Onboarding's responsibility ends at the publication. |
| **High-frequency operations metrics** | Operations BC | Onboarding publishes facts on its process streams; Operations projections aggregate. `OnboardingFunnelView` (operations metrics) is a multi-stream projection owned downstream. |

### 3.11 ADR-evidence framing

| ADR | Status after W004 sidebar |
|---|---|
| **ADR-011 (configuration-as-events bootstrap)** | **Third BC adopting the pattern** via `OnboardingPolicyConfigured` (FCRA dispute window length, abandonment timeout, vendor no-response timeouts, adjudicator-claim expiry windows, document-rejection retry budgets). Pattern confirmed across three BCs. |
| **ADR-012 (aggregate-per-invariant)** | **NOT a new evidence point.** Onboarding is a Process Manager instance — a different modeling pattern with weaker parent-level invariants and coordination-dominated semantics. ADR-012's evidence base stays at W001 + W002. The honest reframe surfaces in §11 with reasoning. |
| **ADR-013 (shared cross-BC identifier)** | **Fourth participant in the canonical-ID chain** (`applicationId == driverProfileId` UUIDv7). Reinforces; not a divergent shape. |
| **ADR-014 (ASB topic naming)** | **Third instance** via `onboarding.driver-approved` after Dispatch's `dispatch.ride-assigned` and Trips' four outbound topics. New companion: `onboarding.adverse-action-notice-required` (or slice-walk-picked slug) for the Notifications forward-constraint. |
| **ADR-015 (driver-app projection timing budget)** | **Second instance** — applies to the driver-app transition from "application pending" to "ready to drive" at approval. Softer numeric budget than Trips' p95 < 200ms warranted (the applicant is not in a time-pressured in-progress workflow; driver may not even have the app open). Slice walk picks a number with explicit measurement endpoints per ADR-015's template. |
| **NEW §11 candidate #1: Process Manager via Handlers as third CritterCab modeling pattern** | First in-repo evidence point. Sibling to ADR-012's aggregate-per-invariant. Delineates when to reach for PM via Handlers vs. an aggregate-cluster — primary signal is *coordination-dominance* (parallel external events, temporal automation, multiple distinct terminals including abandonment, weak parent-level invariants). |
| **NEW §11 candidate #2: Multi-vendor ACL pattern absorbed inside the Process Manager** | Three vendor edges (OIDC via Identity per ADR-006; document-verification; background-check). Translation slices feeding the process stream rather than freestanding architecture. **Authored as a separate paired ADR** alongside candidate #1 per grill #4. |

### 3.12 Friction points from the Wolverine guide §5 worth flagging up-front

Per W001 §12.6 #3 cadence calibration, these are the friction points the slice walk should be aware of before they bite. Each is named so the slice walk doesn't re-discover them mid-walk:

1. **No single home for the process.** Five-plus trigger messages = five-plus handler files. No framework linkage tying them together. The §3 sidebar is the canonical narrative anchor — slice walks reference back here for the process's overall shape.
2. **Distributed completion logic.** The terminal-event-when-prerequisites-met condition appears in *every* continue handler. The slice walk commits a clear predicate (factoring `state.AllGatesClear` onto `ApplicationState` if the predicate becomes complex enough across 10+ events).
3. **2N guard lines per process.** Every continue handler carries both guards. The pattern is mechanical; missing a guard corrupts data. Slice tests should verify guard behavior explicitly per slice.
4. **Asymmetric start handler shape.** `ApplicationOpened`'s handler is plain static; continue handlers use `[AggregateHandler]`. The shape difference is small but requires explanation — Slice 6.1's decisions-locked table calls this out explicitly.
5. **Silent failure if `[AggregateHandler]` applied to start handler.** Middleware short-circuits before the handler runs; no exception. Slice 6.1's test plan includes a regression test for this anti-pattern.
6. **Nullable single-event returns unsafe.** Returning `TEvent?` produces `AppendOne(null)`. All handlers use `IEnumerable<object>` with `yield break` for no-ops.
7. **Inline snapshot projection is a silent dependency.** Step-level idempotency guards rely on `SnapshotLifecycle.Inline` registration. Service bootstrap commits this explicitly with a comment naming the dependency.
8. **No first-class test helper for scheduled timeouts.** `InvokeMessageAndWaitAsync` doesn't track delayed messages. Onboarding's integration tests need a polling helper (or fake-clock injection) for FCRA, abandonment, vendor-no-response, and adjudicator-claim timeouts.

DCB via `[BoundaryModel]` (guide §7) is **not warranted** for Onboarding's slice walk — the invariants W003 surfaced live entirely within the single application's process stream; nothing requires cross-stream invariant enforcement. If a future slice surfaces a genuinely cross-stream invariant (e.g., "no driver can have two open applications"), DCB enters the conversation. Out of scope for W004 as currently scoped.

### 3.13 Decisions locked in this section

| Decision | Resolution |
|---|---|
| Modeling pattern | **Process Manager via Handlers**. First in-repo reference implementation. Sibling pedagogical value to Dispatch's gRPC slices per CLAUDE.md's reference-architecture mission. |
| Process state type | `ApplicationState`, plain C# class, mutable `Apply` methods, `Guid Id` mandatory per Marten. `IsTerminal` derived helper. `Documents` as `Dictionary<Guid, DocumentState>`. `LastSignificantActivityAt` for abandonment-timeout let-state-decide. `DisputeWindowExpiresAt` carry-the-value per Bruun. |
| Per-document state model | Per-document `DocumentState` enum on the parent state's `Documents` dictionary. No separate Document aggregate. Honors W003 V6 (under-verification rename) and W003 V7 (document-rejected ≠ application-rejected). Resolves OQ-2 / OQ-3. |
| Adjudication-case storage | `AdjudicationCaseId` correlation field on parent state + `AdjudicationCase*` events on the parent stream. No separate AdjudicationCase aggregate. Adjudicator queue projection (`AdjudicatorQueueView*`) is a multi-stream projection. Resolves OQ-4. |
| 2N guard discipline | Every continue handler carries terminal-state guard + step-level idempotency guard. Mechanical, essential, tested per slice. |
| Start-handler asymmetry | `ApplicationOpened` handler is plain static; returns `(IStartStream, OutgoingMessages)` via `MartenOps.StartStream<ApplicationState>(...)`. Continue handlers use `[AggregateHandler]`. Slice 6.1 commits this explicitly. |
| Scheduled timeouts | Four self-scheduled timeouts via `OutgoingMessages.Delay`; let-state-decide resolution in continue handlers. No Bruun todo-list-driven mechanism for the timeout itself; Bruun applies for projection-side ops visibility (`AdverseActionDisputeWindow*`). |
| Cross-BC identifier | `applicationId == driverProfileId` shared canonical UUIDv7 per ADR-013. Onboarding is fourth participant in the chain. `SubjectId` carries alongside as actor handle, distinct from lifecycle ID. Resolves grill #1. |
| Multi-vendor ACL integration | Three vendor edges, each a Translation slice emitting events onto the process stream after translation. Absorbed inside the PM pattern, not freestanding. Surfaces as §11 paired ADR candidate. |
| ADR-evidence framing | ADR-011 (third instance), ADR-013 (fourth participant), ADR-014 (third instance), ADR-015 (second instance) gain evidence. ADR-012 explicitly NOT a new evidence point (honest reframe). Two new paired §11 candidates: PM via Handlers + multi-vendor ACL absorbed inside it. |
| DCB consideration | Not warranted for W004's scope. Single-stream pattern suffices. Re-enters conversation only if a future cross-stream invariant surfaces. |
| What's split off | Identity authentication, Driver Profile post-activation, Vehicle sub-domain (W003 B7 deferred), Operations adjudicator tooling, Notifications delivery (FCRA notices via ASB), high-frequency operations metrics. |

---

## 4. Ubiquitous Language

Populated further as the slice walk forces decisions. Each term gets a one-line definition and, where relevant, a note on what it is *not*.

| Term | Definition | Notes |
|---|---|---|
| **Application** | The driver-vetting process state and its event stream. Identified by `applicationId` (canonical UUIDv7). Lives in the Onboarding BC. | W003 §5.1 R-resolution. Not a "Driver Profile" (post-activation BC). |
| **Application ID** | Canonical opaque identifier (UUIDv7) minted by Onboarding at `ApplicationOpened`. **Equals `driverProfileId` on activation per ADR-013.** | Onboarding is the fourth participant in CritterCab's shared-canonical-ID chain. |
| **Subject ID** | The Identity-vocabulary actor handle that flows alongside `applicationId`. Distinct from the lifecycle ID. | Per W003 Story 1 step 4. Identity-domain identifier; Onboarding does not parse provider-specific claims (ADR-006). |
| **Prospective Driver / Applicant / Driver** | The human actor's three-state vocabulary lifecycle. Transition to *applicant* at `ApplicationOpened`; transition to *driver* at Driver Profile activation. | Per W003 §5.1 R1/R2. Rejection terminal: applicant remains "applicant" — no "rejected applicant" sub-state. |
| **Onboarding Adjudicator** | Human-in-the-loop reviewer of background-check `Consider` cases. Inside Onboarding BC. First manual-human actor in any CritterCab workshop. | Per W003 §5.2 B5. Their queue UI / claim flow / SLA tracking is Operations BC's concern; Onboarding owns the events. |
| **`ApplicationState`** | Plain C# class projecting the Application's event stream into a snapshot used for decision-making by continue handlers. | Per §3.4 design. Required `Guid Id`. `IsTerminal` derived helper. |
| **Documents Section** | The applicant's collection of required uploaded documents (driver's license, vehicle registration, insurance certificate). | Per W003 V2. Each document has its own state machine (`UnderVerification | Verified | Rejected`); the section is complete when all required documents are `Verified`. |
| **Document** | An uploaded artifact (license / registration / insurance). Identity preserved through rejection cycle (per W003 Story 2 Q2a.2 — "mutate"). | Per W003 V6 "under-verification" rename. Per W003 V7 distinguishes `DocumentRejected` from `ApplicationRejected`. |
| **Background Check Case** | A correlation handle for the vendor-side check, identified by `backgroundCheckCaseId`. Carries vendor case ID at `BackgroundCheckCaseOpened`. | W003 V5 prefix discipline. NOT an aggregate or sub-entity; correlation ID on events appended to the Application's process stream. |
| **Adjudication Case** | A correlation handle for the human-reviewer-side decision, identified by `adjudicationCaseId`. Created when BG-check returns `Consider`. | W003 V5 prefix discipline. NOT an aggregate; same pattern as Background Check Case. The queue projection (`AdjudicatorQueueView*`) reads these correlations across all Application process streams. |
| **FCRA Pre-Adverse-Action Notice** | The notice that starts the dispute window after an adjudicator-rejection decision. Carries the BG-check report itself per FCRA. | Per W003 Story 3 step 24 / §5.1 V4. Distinct event from final adverse-action notice; both emit outbound ASB publications for the Notifications BC's eventual workshop to consume (OQ-10d). |
| **FCRA Final-Adverse-Action Notice** | The final rejection notification fired after the dispute window expires without a dispute. | Per W003 Story 3 step 27. Triggers `ApplicationRejected` terminal. |
| **Dispute Window** | The FCRA-required interval between pre-adverse and final-adverse notices. Window length from `OnboardingPolicyConfigured` per ADR-011. | Carried on the event itself (Bruun *carry-the-value* per §3.4); survives mid-flight policy changes. Per W003 OQ-6's Bruun temporal-automation lean — but the *mechanism* is PM-self-scheduled message + let-state-decide, not Bruun todo-list. |
| **Onboarding Policy** | Operator-tunable parameters event-sourced via `OnboardingPolicyConfigured`. Parameters include FCRA dispute window length, abandonment timeout, vendor no-response timeouts, document-rejection retry budgets, adjudicator-claim expiry. | Singleton aggregate per ADR-011's third BC adoption. Migration-time seed event per ADR-011's bootstrap strategy. |
| **Application Lifecycle** | The four-terminal coordination shape: `Opened → {Approved | Rejected | Abandoned | Withdrawn}`. | Per §3.4. Distinct from the trip lifecycle (W002) — Onboarding's lifecycle is coordination-dominated, weak parent-level invariants. |
| **Three "Approved" moments** | `ApplicationApproved` (process-stream terminal) → `ApplicantNotifiedOfApproval` (notification side effect on the stream) → `DriverActivationPublished` (outbound `onboarding.driver-approved` to Driver Profile). | Per W003 V4 disambiguation. Each is a distinct event for distinct semantics — the pattern from W001 §5.8/§5.9 (distinct events for distinct semantics) applied a fourth time. |
| **Translation Slice** | A slice converting a foreign system's signal (vendor webhook, human-actor decision via UI, Identity publication) into a CritterCab-domain event appended to the process stream. | Three vendor edges (OIDC via Identity, document-verification, background-check); two human-actor edges (applicant via UI, adjudicator via UI). The multi-vendor ACL pattern is absorbed inside the PM via Handlers pattern per §3.8. |
| **Process Stream** | The Marten event stream for one Application, keyed by `applicationId`. All Application-related events (applicant inputs, vendor translations, adjudicator decisions, FCRA notices, terminals) append here. | Per §3.3 — *streams are the treasure, aggregates are a means to an end*. |

Additional terms are added to this glossary as the slice walk forces vocabulary decisions.

---

## 5. Event List (chronological — populated per slice)

Per the §3.5 catalogue. Full payload shapes and decisions-locked tables fill in per slice during the walk.

| # | Event | Slice | Key payload | Lane |
|---|---|---|---|---|
| 1 | `ApplicationOpened` | 6.1 | `applicationId` (UUIDv7, == `driverProfileId` on activation per ADR-013), `subjectId`, `openedAt` | Onboarding (birth event; recorded from Translation-in handler) |
| 2 | `PersonalInfoSubmitted` | 6.2 | `applicationId`, `legalName`, `dateOfBirth`, `address`, `cityOfOperation`, `phoneNumber`, `occurredAt` | Onboarding (applicant-Command; gate-bit flip on `ApplicationState.PersonalInfoComplete`) |
| 3 | `BackgroundCheckConsentGiven` | 6.3 | `applicationId`, `ssnHash`, `consentVersion`, `consentSignedAt`, `occurredAt` | Onboarding (applicant-Command; gate-bit flip on `ApplicationState.BackgroundCheckConsentGiven`; raw SSN in separate encrypted vault per sensitive-PII handling) |
| 4 | `DocumentUploaded` | 6.4 | `applicationId`, `documentId` (UUIDv7), `documentType` enum, `storageHandle` (blob ref), `occurredAt` | Onboarding (applicant-Command; per-document state machine entry — `Documents[id]=UnderVerification`) |
| 5 | `DocumentVerified` | 6.4 | `applicationId`, `documentId`, `vendorCaseId` (opaque), `occurredAt` | Onboarding (Klefter translation-in from document-verification vendor — `Documents[id]=Verified`) |
| 6 | `DocumentRejected` | 6.4b | `applicationId`, `documentId`, `reason` (`DocumentRejectionReason` enum — canonical per W003 V8 normalization), `vendorCaseId`, `occurredAt` | Onboarding (Klefter translation-in; per-document, distinct from `ApplicationRejected` per W003 V7 — `Documents[id]=Rejected`) |
| 7 | `DocumentReuploaded` | 6.4b | `applicationId`, `documentId` (same id as the rejected document — identity preserved per W003 Story 2 Q2a.2), `storageHandle` (new blob ref), `occurredAt` | Onboarding (applicant-Command; per-document state transition `Rejected → UnderVerification`) |
| 8 | `BackgroundCheckRequested` | 6.5 | `applicationId`, `requestedAt` | Onboarding (cascade from `DocumentVerified` when all three approval-gates satisfied; atomic dual-emit per W001 §5.5 / W002 §6.6 precedent) |
| 9 | `BackgroundCheckCaseOpened` | 6.5 | `applicationId`, `vendorCaseId` (opaque BG-check vendor case ID per W003 V5 prefix discipline), `openedAt` | Onboarding (Klefter translation-in from BG-check vendor sync ack; `BackgroundCheckVendorCaseIndex` inline projection load-bearing for webhook routing) |
| 10 | `BackgroundCheckStatusReceived` | 6.5 / 6.6 | `applicationId`, `vendorCaseId`, `status` (`Pending | Clear | Consider | Suspended` enum per W003 V5/V6), `occurredAt` | Onboarding (Klefter translation-in from BG-check vendor webhook; one event surface for all status receipts per W003 V5/V6 enum-as-vocabulary discipline) |
| 11 | `AdjudicationCaseQueued` | 6.6 | `applicationId`, `adjudicationCaseId` (UUIDv7 per W003 V5 prefix discipline), `subjectId`, `vendorFindings` (opaque), `queuedAt` | Onboarding (cascade from `BackgroundCheckStatusReceived(Consider)`; first manual-human actor handling in any CritterCab workshop) |
| 12 | `AdjudicationCaseClaimed` | 6.6 | `applicationId`, `adjudicationCaseId`, `adjudicatorId` (opaque; Operations BC workforce-identity model is forward-constraint), `claimedAt` | Onboarding (adjudicator-Command via Operations BC tooling; schedules claim-expiry timeout per §3.7 let-state-decide) |
| 13 | `AdjudicationCaseDecided` | 6.6 | `applicationId`, `adjudicationCaseId`, `adjudicatorId`, `decision` (`Clear | Reject` enum), `notes?`, `decidedAt` | Onboarding (adjudicator-Command via Operations BC tooling; `Clear` cascades fresh `BackgroundCheckStatusReceived(Clear)` overriding vendor `Consider`; `Reject` cascades `PreAdverseActionNoticeIssued` in Slice 6.7) |
| 14 | `PreAdverseActionNoticeIssued` | 6.7 | `applicationId`, `adjudicationCaseId`, `reportSnapshotHandle` (FCRA-required content; out-of-band blob ref), `windowExpiresAt` (Bruun carry-the-value), `policyVersion`, `occurredAt` | Onboarding (cascaded from `AdjudicationCaseDecided(Reject)`; atomic triple-emit with scheduled `DisputeWindowExpired` self-message + outbound ASB publication `onboarding.adverse-action-notice-required` phase: PreAdverse) |
| 15 | `DisputeWindowExpired` | 6.7b | `applicationId`, `occurredAt` | Onboarding (self-message arrival as fact-on-stream; canonical let-state-decide pattern; emitted only when dispute window elapses without dispute filed) |
| 16 | `FinalAdverseActionNoticeIssued` | 6.7b | `applicationId`, `occurredAt` | Onboarding (atomic quadruple-emit with `DisputeWindowExpired` + `ApplicationRejected` + second outbound ASB publication `onboarding.adverse-action-notice-required` phase: FinalAdverse) |
| 17 | `ApplicationRejected` | 6.7b | `applicationId`, `cause` (`ApplicationRejectionCause` BC-owned enum; W004 materializes only `FcraFinalAdverseAction`), `occurredAt` | Onboarding (terminal — `state.Status = Rejected`; NO outbound DP publication per W003 §5.2 B2 / OQ-10a; absence-of-publication locked explicitly in Slice 6.9) |
| 18 | `ApplicationApproved` | 6.8 | `applicationId`, `approvedAt` | Onboarding (terminal — `state.Status = Approved`; first of three "approved" moments per W003 V4) |
| 19 | `ApplicantNotifiedOfApproval` | 6.8 | `applicationId`, `subjectId`, `occurredAt` | Onboarding (notification side-effect-on-stream; second of three "approved" moments; cross-BC publication via separate forward-constraint deferred to W004 close) |
| 20 | `DriverActivationPublished` | 6.8 | `applicationId`, `driverProfileId` (== `applicationId` per ADR-013 — fourth-participant loop-closure), `subjectId`, `topic` (`onboarding.driver-approved`), `occurredAt` | Onboarding (third of three "approved" moments; records outbound ASB publication fact; atomic quadruple-emit with the three events above + outbound publication on `onboarding.driver-approved`) |
| 21 | `OnboardingPolicyConfigured` | 6.10 | `operatorId`, full parameter set (FCRA dispute window, abandonment window, vendor no-response, adjudicator claim window, document-rejection retry budget, etc.), `reason?`, `configuredAt` | Onboarding (singleton aggregate stream per ADR-011; third-BC adoption of configuration-as-events pattern; migration-time seed event per ADR-011 bootstrap strategy) |

Scaffold by category (from §3.5):

| Category | Events | First slice |
|---|---|---|
| Initial | `ApplicationOpened` | 6.1 |
| Applicant inputs | `PersonalInfoSubmitted`, `BackgroundCheckConsentGiven`, `DocumentUploaded`, `DocumentReuploaded`, `ApplicantWithdrew` | 6.2, 6.3, 6.4, 6.4b, post-walk |
| Document-verification (translated) | `DocumentVerified`, `DocumentRejected` | 6.4, 6.4b |
| Background-check (translated) | `BackgroundCheckRequested`, `BackgroundCheckCaseOpened`, `BackgroundCheckStatusReceived` | 6.5 |
| Adjudication (translated) | `AdjudicationCaseQueued`, `AdjudicationCaseClaimed`, `AdjudicationCaseDecided` | 6.6 |
| FCRA two-phase notice | `PreAdverseActionNoticeIssued`, `DisputeWindowExpired`, `FinalAdverseActionNoticeIssued` | 6.7, 6.7b |
| Approval pathway | `ApplicationApproved`, `ApplicantNotifiedOfApproval`, `DriverActivationPublished` | 6.8 |
| Terminal events | `ApplicationApproved` / `ApplicationRejected` / `ApplicationAbandoned` / `ApplicationWithdrawn` | 6.8 / 6.7b / let-state-decide / post-walk |
| Configuration | `OnboardingPolicyConfigured` | 6.10 |

---

## 6. Slice Walk

Each slice section carries: pattern type (Command / View / Automation / Translation / Continue Handler / Start Handler), trigger, command(s), event(s), view(s) read or produced, GWT sketches, decisions locked, and cross-reference notes. Slices are walked in the order locked at the slice-ordering proposal (§1's session log); ordering rationale follows W003's three-story spine plus W001 §12.6 + W002 §14.6 cadence rules.

### Proposed slice ordering

| Slice | Pattern | Trigger | Story source | Events emitted |
|---|---|---|---|---|
| **6.1 Intake** | Translation-in (Klefter) + start-handler | Identity publishes `identity.driver-registered`; Onboarding consumes | W003 Story 1 steps 1–5 | `ApplicationOpened` (start-handler asymmetry — Slice 6.1 commits the `MartenOps.StartStream<ApplicationState>` pattern explicitly) |
| **6.2 Personal info** | Command | Applicant submits personal info via UI | W003 Story 1 step 6 | `PersonalInfoSubmitted` |
| **6.3 BG-check consent** | Command | Applicant submits SSN + FCRA-compliant authorization via UI | W003 Story 1 step 7 | `BackgroundCheckConsentGiven` |
| **6.4 Document upload + verification (happy)** | Command + Translation-out + Translation-in (paired with 6.4b) | Applicant uploads document → Onboarding submits to vendor → vendor returns "verified" | W003 Story 1 steps 9–14 | `DocumentUploaded`, `DocumentVerified` ×3 (per document type) |
| **6.4b Document rejection + re-upload (recovery)** | Command + Translation-in + Command (paired with 6.4) | Vendor returns "rejected" → Onboarding notifies applicant → applicant re-uploads → vendor returns "verified" | W003 Story 2 steps 9–17 | `DocumentRejected`, `DocumentReuploaded`, `DocumentVerified` |
| **6.5 Background check (Clear path)** | Command + Translation-out + Translation-in (paired with 6.6) | Onboarding requests check → vendor returns case ID + Pending → vendor pushes Clear via webhook | W003 Story 1 steps 15–19 | `BackgroundCheckRequested`, `BackgroundCheckCaseOpened`, `BackgroundCheckStatusReceived (Clear)` |
| **6.6 Background check Consider → adjudication** | Translation-in + Command + Translation-in (paired with 6.5) | Vendor pushes Consider via webhook → Onboarding queues adjudication case → adjudicator claims and decides | W003 Story 3 steps 18–23 | `BackgroundCheckStatusReceived (Consider)`, `AdjudicationCaseQueued`, `AdjudicationCaseClaimed`, `AdjudicationCaseDecided` |
| **6.7 FCRA pre-adverse-action notice** | Command (cascaded from 6.6 reject decision) + scheduled self-message (paired with 6.7b) | Adjudication decides reject → Onboarding issues pre-adverse notice → schedules dispute-window timeout via `OutgoingMessages.Delay` | W003 Story 3 steps 24–25 | `PreAdverseActionNoticeIssued` + outbound ASB publication for Notifications BC (OQ-10d) |
| **6.7b FCRA dispute-window expiry (PM let-state-decide timeout)** | Continue handler over self-scheduled timeout (paired with 6.7) | `DisputeWindowExpired` self-message fires after window length elapsed | W003 Story 3 steps 26–27 | `DisputeWindowExpired`, `FinalAdverseActionNoticeIssued` + outbound ASB publication, `ApplicationRejected` (terminal) |
| **6.8 Approval pathway + Translation-out to Driver Profile** | Continue handler when all gates clear (cascaded terminal) | Personal-info + consent + documents-section + BG-check-clear all satisfied → handler observes prerequisites and emits cascade | W003 Story 1 steps 20–23 / Story 2 reference-block | `ApplicationApproved` (terminal), `ApplicantNotifiedOfApproval`, `DriverActivationPublished` + outbound ASB publication (`onboarding.driver-approved`) for Driver Profile BC |
| **6.9 Application-rejected terminal (no DP publication)** | Terminal-event commitment | W003 §5.2 B2 asymmetry: rejection terminal has no DP publication (per OQ-10a). Slice locks the absence explicitly. | W003 Story 3 step 28 | (No new event — `ApplicationRejected` already emitted in 6.7b; this slice locks the *non-publication* with documented reasoning, OQ-10a/b/c/e dispositions) |
| **6.10 OnboardingPolicyConfigured (configuration-as-events)** | Command on singleton aggregate stream | Operator updates onboarding policy via admin UI | Inferred from W003 OQ-6 + ADR-011 precedent | `OnboardingPolicyConfigured` (full-replacement semantics; third BC adopting ADR-011) |

**Cadence calibration per W001 §12.6 #3:**

- **Heavy slices:** 6.1, 6.4 + 6.4b paired, 6.6, 6.7 + 6.7b paired, 6.8.
- **Light slices:** 6.2, 6.3, 6.9, 6.10.

**Slices implicit in the pattern but not separately walked:**

- **`ApplicationAbandoned` (Bruun no-activity timeout via PM let-state-decide)** — modeled in §3.7's scheduled-timeout table. Slice walk references back. If during walk this surfaces as warranting its own slice, sub-slice it (e.g., 6.X-abandonment).
- **`ApplicantWithdrew` → `ApplicationWithdrawn`** — applicant-initiated terminal. Brief callout in Slice 6.9.

**Slices NOT walked (out of scope per Option A — locked at §2.5):**

- Re-application policy (W003 OQ-11)
- Multi-vehicle / vehicle as sub-domain (W003 B7 / OQ-13)
- Suspension / reinstatement / deactivation (W003 OQ-14)

### 6.1 Slice 1 — ApplicationOpened (Translation-in from Identity + start-handler)

**Pattern:** Translation-in (Klefter) + start-handler (asymmetric per Wolverine PM-via-Handlers guide §3 step 4).
**Lane:** Onboarding for the handler and local events; ASB inbound from Identity.
**Trigger:** ASB message on topic `identity.driver-registered` (session-keyed by `subjectId`).

#### Flow on the board

```
   ┌─────────────────────────────────────┐
   │ Identity BC  [pending workshop]     │
   │  Identity-side translation from     │
   │  OIDC provider per ADR-006:         │
   │  prospective-driver completes       │
   │  account-creation arc on            │
   │  driver-signup landing page         │
   │  + magic-link verification          │
   │  + OIDC subject committed locally   │
   │  → Identity publishes               │
   │    identity.driver-registered       │
   │    (forward-constraint contract)    │
   └──────────────┬──────────────────────┘
                  │ ASB
                  │ Topic: identity.driver-registered
                  │ Session key: subjectId
                  │ At-least-once delivery
                  │ Wire format: Protobuf DriverRegistered
                  │   (pending Identity workshop / proto authorship)
                  ▼
   ┌─────────────────────────────────────┐
   │ Onboarding Translation-in handler   │
   │ (plain static Wolverine handler;    │
   │  NOT [AggregateHandler])            │
   │ Idempotent on subjectId             │
   │ Klefter: local decision recording   │
   │   — accepting responsibility        │
   │ Mints canonical applicationId       │
   │   (UUIDv7) per ADR-013              │
   └──────────────┬──────────────────────┘
                  │ MartenOps.StartStream<ApplicationState>(
                  │   applicationId, ApplicationOpened {...})
                  │ + OutgoingMessages.Delay(
                  │   ApplicationAbandonmentTimeout,
                  │   policy.AbandonmentWindow)
                  ▼
   ┌─────────────────────────────────────┐
   │ ApplicationOpened                   │  orange event
   │ (Application stream; first event)   │  (process state's birth)
   └──────────────┬──────────────────────┘
                  │
                  ▼
   Views fed:
     • ApplicantDashboardView      (inline; per-applicant status)
     • OnboardingFunnelView        (async; operations metrics)
     • ApplicationsBySubject       (async; idempotency lookup substrate)
```

#### Inbound contract

| Aspect | Value |
|---|---|
| Source | Identity BC, post-OIDC-subject-creation (Identity workshop pending) |
| Transport | Azure Service Bus (ADR-005) |
| Topic | `identity.driver-registered` per ADR-014 — third instance of the convention after `dispatch.ride-assigned` and Trips' four outbound topics |
| Session key | `subjectId` (Identity-vocabulary actor handle) |
| Delivery | At-least-once |
| Wire format | Protobuf (`DriverRegistered`); to be authored at `/protos/crittercab/identity/v1/driver_registered.proto` per Identity's workshop + PR #4 precedent |
| Treatment in this workshop | **Forward-constraint** on Identity's eventual workshop. Onboarding commits the receiver-side shape now; Identity workshop honors or explicitly overrides on its own walk. |

**Forward-constraint on Identity's eventual workshop:** Identity must publish `identity.driver-registered` carrying at minimum `subjectId` (canonical actor handle) and `registeredAt` (server clock at OIDC-subject creation). Any additional fields (display name, email, etc.) are Identity's call; Onboarding intentionally does not require them and will not parse provider-specific claims (ADR-006).

#### Idempotency contract

Handler keyed on `subjectId`. The `ApplicationsBySubject` projection answers "does an Application stream already exist for this `subjectId`?" before `MartenOps.StartStream` runs. If a stream exists at the supplied `subjectId`, the handler is a no-op — at-least-once redelivery does not spawn a duplicate Application and does not re-emit `ApplicationOpened`.

Two failure modes the slice locks the handling of:

1. **Race-condition on first redelivery** — `ApplicationsBySubject` projection is async; first delivery and a near-simultaneous redelivery could both observe an empty result before either commits. Mitigation: catch `ExistingStreamIdCollisionException` from `MartenOps.StartStream` and silent-ack (per the Wolverine guide §3 step 4 explicit guidance — *"Duplicate start commands fail with `ExistingStreamIdCollisionException`. The first start wins; catch the exception if your trigger source guarantees at-least-once delivery."*).
2. **`SnapshotLifecycle.Inline` registration is mandatory** — Slice 6.1's test plan includes a service-bootstrap regression test that fails if inline registration is forgotten. Per §3.12 friction-point #7 (silent dependency).

#### Klefter decision-event recording

The act of accepting responsibility for vetting this driver — distinct from Identity's `identity.driver-registered` which only records that OIDC-subject creation happened — is captured as the local `ApplicationOpened` event. Klefter pattern (per [`docs/research/agents-in-event-models.md`](../research/agents-in-event-models.md)): even though Identity's event store has its own record of the OIDC-subject creation, Onboarding records the *consequence* (Application stream's birth) in its own event store, decoupled from Identity's vocabulary and retention policy.

This is also the structural basis for every projection Onboarding builds (`ApplicantDashboardView`, `OnboardingFunnelView`, etc.) — they project from `ApplicationOpened` and subsequent Application events, not from Identity's `identity.driver-registered`. Cross-BC traceability flows through `subjectId` (carried on `ApplicationOpened`), not through cross-store joins.

#### Canonical UUIDv7 minting — ADR-013 fourth-instance materialization

`applicationId` is server-assigned UUIDv7 at handler-fire time, opaque at the application level, time-ordered at the byte level for Marten stream-index locality per ADR-013's wire-format commitment. **The minted value becomes `driverProfileId` on activation** (Slice 6.8); Driver Profile inherits it on the `onboarding.driver-approved` publication, becoming the fourth participant in CritterCab's shared-canonical-ID chain after Dispatch → Trips → (Pricing) → (Ratings).

`subjectId` carries alongside as the actor handle, distinct from the lifecycle ID. Onboarding's events all carry both; downstream consumers (Driver Profile, Notifications, eventually T&S) receive both and use whichever fits their access pattern.

#### Start-handler asymmetry — explicit commit

```csharp
// NO [AggregateHandler] — applying it would silently short-circuit the handler.
public static class ApplicationOpenedHandler
{
    public static (IStartStream, OutgoingMessages) Handle(
        DriverRegistered command,              // the translated inbound from Identity
        TimeProvider clock,
        OnboardingPolicy policy)               // injected via [Entity] or composed read
    {
        var applicationId = Guid.CreateVersion7(clock.GetUtcNow());

        var opened = new ApplicationOpened(
            ApplicationId: applicationId,
            SubjectId: command.SubjectId,
            OpenedAt: clock.GetUtcNow());

        var outgoing = new OutgoingMessages();
        outgoing.Delay(
            new ApplicationAbandonmentTimeout(applicationId),
            policy.AbandonmentWindow);

        return (
            MartenOps.StartStream<ApplicationState>(applicationId, opened),
            outgoing);
    }
}
```

The handler is plain static — **no `[AggregateHandler]` attribute**. The PM-via-Handlers guide §5 friction-point #4 + #5 are taken seriously: a missed asymmetry here is a silent-failure mode (middleware short-circuits; no exception thrown; handler never runs). Slice 6.1's test plan includes a regression assertion that catches `[AggregateHandler]` being mistakenly applied to this start handler.

Application-abandonment timeout is scheduled here at start. Re-scheduled on each significant activity event per §3.7's table; the let-state-decide pattern eliminates the need to cancel prior timers (state authoritative; handler checks `state.LastSignificantActivityAt` before yielding `ApplicationAbandoned`).

#### Event — `ApplicationOpened`

Past-tense fact. Birth event for the Application's process stream.

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 (opaque) | Canonical lifecycle ID. Stream key for the Application process stream. **Equals `driverProfileId` on activation per ADR-013.** Minted at handler-fire time. |
| `subjectId` | UUIDv7 (opaque) | Identity-vocabulary actor handle. Carried for traceability and idempotency. |
| `openedAt` | timestamp | Server clock at handler-fire time. |

Lean payload — the canonical-ID and subject-handle are everything Onboarding needs to bootstrap; subsequent slices add the substance. Mirrors the W001 §5.1 `RideRequested` precedent of carrying only what's needed at the moment of birth, with subsequent events accumulating context.

#### Projection lifecycle

| Question | Decision |
|---|---|
| `ApplicantDashboardView` update lifecycle on `ApplicationOpened` | **Inline.** Marten projection runs in the same transactional commit as the event append. Applicant's gRPC server-streaming view (driver-app dashboard) receives the new "application started" payload immediately on commit. Mirrors W002 §6.1's `DriverTripView` inline pattern. |
| Other projections (`OnboardingFunnelView`, `ApplicationsBySubject`) | **Async** (Marten async daemon). Operations metrics + idempotency lookup substrate; neither on a latency-sensitive applicant-facing path. |
| Timing-budget target | **Softer than ADR-015's p95 < 200ms.** The applicant is not in a time-pressured workflow; the dashboard re-render at "application started" doesn't have a tap-and-render constraint. Proposed: **p95 < 1s**, measured as `identity.driver-registered` ASB-commit → `ApplicationOpened` Onboarding-commit. ADR-015's second-instance evidence point lands here with explicit softer budget. |
| `MatchingLatencyMetrics` equivalent | **`OnboardingIntakeLatencyMetrics`** — defer but pin. Parallel to W002 §6.1's pattern. Carries `identity.driver-registered.publishedAt` (from the inbound ASB message metadata) vs. `openedAt` (server clock on commit). |

#### Forward-constraints generated (consolidated in §X+1)

This slice generates two forward-constraints on Identity's eventual workshop:

- **Identity must publish `identity.driver-registered` over ASB** at OIDC-subject-creation terminal, session-keyed by `subjectId`, with at minimum `subjectId` + `registeredAt` payload. (Specific proto authorship via PR #4 precedent.)
- **Identity must guarantee at-least-once delivery semantics** consistent with Wolverine + ASB defaults. Onboarding's idempotency contract assumes redelivery and handles it; Identity does not need to guarantee exactly-once.

#### GWT sketches

**Happy path — first delivery**
```
Given: no Application stream exists for subjectId S
  And: identity.driver-registered { subjectId: S, registeredAt: T0 } arrives over ASB
When: Onboarding's translation-in handler receives the message
Then: ApplicationOpened { applicationId: A (new UUIDv7), subjectId: S, openedAt: T1 } is emitted
  And: Application stream is created at applicationId A with state.Status = Opened
  And: ApplicationAbandonmentTimeout(A) scheduled for T1 + policy.AbandonmentWindow
  And: ApplicantDashboardView updated inline (status: Opened)
  And: OnboardingFunnelView, ApplicationsBySubject updated async
```

**At-least-once redelivery (idempotency — stream-exists branch)**
```
Given: Application stream exists for subjectId S (created on prior delivery)
When: identity.driver-registered { subjectId: S, ... } arrives again (redelivery)
Then: handler observes ApplicationsBySubject[S] → existing applicationId
  And: ASB message is acked silently
  And: no second ApplicationOpened emitted; no new stream
  And: handler logs an observability signal for redelivery rate (no domain event)
```

**Race-condition redelivery (ExistingStreamIdCollisionException branch)**
```
Given: no Application stream exists for subjectId S
  And: ApplicationsBySubject projection is async-behind
When: two redelivered identity.driver-registered messages for S race past
       the idempotency-lookup check and both reach MartenOps.StartStream
Then: the first commit wins (first event commits, stream created)
  And: the second commit fails with ExistingStreamIdCollisionException
  And: the second handler catches the exception and silent-acks
  And: net result is one ApplicationOpened, one stream, one abandonment timer
```

**Malformed payload (transport-grade error)**
```
Given: ASB message arrives with payload that fails proto deserialization
       or carries a subjectId that violates UUIDv7 format
When: Wolverine handler receives the message
Then: standard Wolverine error handling — retry-with-backoff per ADR-005;
       persistent failure → dead-letter queue
  And: no domain event emitted; no stream created
  And: ops-grade observability signal logged for malformed-message rate
```

#### Rejection / error reasons

Onboarding's intake handler does NOT reject domain-meaningfully — it records what Identity validated. Three failure modes:

1. **Malformed payload** → Wolverine retry + DLQ (transport-grade).
2. **Idempotency hit (stream exists)** → silent ack (expected race, not an error).
3. **`ExistingStreamIdCollisionException`** → silent ack (race-condition redelivery edge, expected per VitePress guide §3 step 4).

There are no domain-grade rejection reasons because validation concerns ("the OIDC subject is invalid", "the actor is suspended at Identity-level") are upstream concerns Identity already cleared before publishing. Onboarding records, does not re-validate. Mirrors W002 §6.1's discipline.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `ApplicantDashboardView` | Applicant (driver-app applicant-dashboard surface; gRPC server-streaming) | Per-`applicationId`: `{ applicationId, subjectId, status, personalInfoComplete, consentGiven, documentsSectionComplete, backgroundCheckStatus?, fcraPhase?, openedAt, lastSignificantActivityAt }` | `ApplicationOpened` (this slice) + all subsequent Application events | **Slice-6.1 inline.** Honors the softer ADR-015 second-instance timing budget. |
| `ApplicationsBySubject` | Idempotency lookup substrate (internal) + Identity-side audit | Per-`subjectId`: most-recent `applicationId` and stream's terminal status | `ApplicationOpened` (this slice) + terminal events | **Slice-6.1 async.** Load-bearing for idempotency contract. Re-used for OQ-11 re-application policy lookup (deferred). |
| `OnboardingFunnelView` | Operations metrics / SRE / product KPI | Counts per lifecycle stage, conversion rate, drop-off analysis, FCRA-rejection rate, abandonment rate, time-in-stage distributions | All Application events | **Slice-6.1 async.** Operations metrics surface; not a per-applicant projection. |
| `OnboardingIntakeLatencyMetrics` | SRE, ops, product KPI | Per-Application: `identity.driver-registered.publishedAt → openedAt` interval distribution | `ApplicationOpened` carrying inbound ASB message metadata | **Defer but pin** — ADR-015 second-instance evidence point's observability hook. Parallel to W002 §6.1's `MatchingLatencyMetrics`. |
| `AdjudicatorQueueView*` (Bruun todo-list) | Onboarding adjudicators (Operations BC tooling consumes) | Per-`adjudicationCaseId`: `{ adjudicationCaseId, applicationId, subjectId, queuedAt, claimedAt?, adjudicatorId?, claimExpiresAt? }` | `AdjudicationCaseQueued`, `AdjudicationCaseClaimed`, `AdjudicationCaseDecided` (Slice 6.6) | **Slice-6.1 named; populated from Slice 6.6.** Bruun todo-list with asterisk suffix; consumed by Operations BC's adjudicator-tooling surface (Operations workshop pending). |
| `AdverseActionDisputeWindow*` (Bruun todo-list, projection-side) | Ops visibility into open FCRA windows | Per-`applicationId`: `{ applicationId, fcraPhase, disputeWindowExpiresAt }` rows | `PreAdverseActionNoticeIssued`, `DisputeWindowExpired`, `FinalAdverseActionNoticeIssued` (Slice 6.7 / 6.7b) | **Slice-6.1 named; populated from Slice 6.7.** Bruun pattern for ops-side visibility; *not* the mechanism driving the timeout (PM self-scheduled message + let-state-decide is). |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Translation-in (Klefter) + start-handler (asymmetric — plain static, NOT `[AggregateHandler]`). |
| Inbound transport | ASB topic `identity.driver-registered`, session-keyed by `subjectId`, at-least-once delivery. Forward-constraint on Identity workshop. |
| Idempotency key | `subjectId` looked up via `ApplicationsBySubject` projection + `ExistingStreamIdCollisionException` catch as race-condition fallback. |
| `applicationId` minting | Server-assigned `Guid.CreateVersion7(clock.GetUtcNow())`. Equals `driverProfileId` on activation per ADR-013. Onboarding fourth participant in canonical-ID chain. |
| `subjectId` carriage | On every Onboarding event; distinct from `applicationId`; never used as stream key (that role is `applicationId`'s). |
| Start-handler shape | `(IStartStream, OutgoingMessages) Handle(...)` returning `MartenOps.StartStream<ApplicationState>(...)`. NO `[AggregateHandler]`. Test plan includes regression assertion against the silent-failure mode. |
| `ApplicationAbandonmentTimeout` scheduled at start | Via `OutgoingMessages.Delay` with window from `OnboardingPolicy.AbandonmentWindow`. Let-state-decide resolution per §3.7. |
| Event payload | `ApplicationOpened { applicationId, subjectId, openedAt }`. Lean — substance accumulates on subsequent events. |
| `ApplicantDashboardView` projection lifecycle | Inline (same commit as event append). Applicant-dashboard latency-sensitive but not as tight as W002's driver-app. |
| Other projections | Async (Marten daemon). `ApplicationsBySubject` load-bearing for idempotency. |
| Timing-budget ADR | Second instance of ADR-015 applies. Proposed softer budget: p95 < 1s. Slice walk picks; explicit measurement endpoints `identity.driver-registered.publishedAt → openedAt`. `OnboardingIntakeLatencyMetrics` projection pinned. |
| Rejection reasons | None (domain-grade). Three transport-grade modes: malformed payload (Wolverine retry + DLQ), idempotency hit (silent ack), `ExistingStreamIdCollisionException` (silent ack). |
| `SnapshotLifecycle.Inline` registration | Mandatory per §3.12 friction-point #7. Service bootstrap test asserts presence; tests fail loudly if registration is missed. |

#### Cross-references and ripples

- **Backward (cross-BC):** Triggered by Identity's `identity.driver-registered` publication (Identity workshop pending; forward-constraint generated for §X+1).
- **Forward (Slices 6.2 - 6.10):** Every subsequent slice loads the Application's process state via `[AggregateHandler]` + `FetchForWriting<ApplicationState>`; the start-handler asymmetry is unique to this slice.
- **Forward (Slice 6.8):** When the application reaches the `Approved` terminal, the outbound `onboarding.driver-approved` publication carries the same `applicationId` (now `driverProfileId`) — closing the loop on ADR-013's fourth-participant materialization.
- **Forward (§3.7's abandonment timeout):** Scheduled here, re-scheduled on each significant activity event, resolved via let-state-decide check on `state.LastSignificantActivityAt`.
- **ADR-013:** Fourth-instance materialization lands here. Shared-canonical-ID chain confirmed across four BCs (Dispatch → Trips → Onboarding → Driver Profile).
- **ADR-014:** Third-instance materialization lands here on the inbound side (`identity.driver-registered` topic naming). New companion topic `onboarding.adverse-action-notice-required` lands at Slice 6.7.
- **ADR-015:** Second-instance evidence point with softer numeric budget. Slice locks p95 < 1s as the proposed target; ADR-015 amendment or supersession follows when implementation lands and measurements become available.
- **New ADR candidates §11 #1 + #2:** First in-repo realization of the PM via Handlers pattern lands here; the multi-vendor ACL pattern's first vendor edge (OIDC via Identity, inherited per ADR-006) is also realized. Slices 6.4 + 6.4b + 6.5 + 6.6 will realize the other two vendor edges; together they form the evidence base for both paired ADRs.
- **Forward-constraints status:** Two generated (consolidated in §X+1 at session close).

### 6.2 Slice 2 — PersonalInfoSubmitted (Command from applicant)

**Pattern:** Command (continue handler).
**Lane:** Onboarding.
**Trigger:** Applicant submits personal-info form on driver-app applicant-dashboard.

#### Flow on the board

```
   ┌────────────────────────────────────┐
   │ Applicant fills personal-info form │
   │ on driver-app applicant-dashboard  │
   │ (gRPC unary call to Onboarding)    │
   └──────────────┬─────────────────────┘
                  │ SubmitPersonalInfo command
                  ▼
   ┌────────────────────────────────────┐
   │ [AggregateHandler] loads            │
   │ ApplicationState via                │
   │ FetchForWriting<ApplicationState>   │
   │                                     │
   │ Guards (2N discipline):             │
   │   if (state.IsTerminal) yield break;│
   │   if (state.PersonalInfoComplete)   │
   │     yield break;                    │
   └──────────────┬─────────────────────┘
                  │
                  ▼
   ┌────────────────────────────────────┐
   │ PersonalInfoSubmitted              │  orange event
   │ (Application stream)               │
   └──────────────┬─────────────────────┘
                  │
                  ▼
   Views updated:
     • ApplicantDashboardView      (inline; status visible to applicant)
     • OnboardingFunnelView        (async; stage transition recorded)
```

#### Command — `SubmitPersonalInfo`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | The Application whose personal-info is being submitted. Authenticated applicant context (from `subjectId` claim) must own this Application. |
| `legalName` | `{ first, middle?, last }` | Required. Validation at HTTP boundary (per project's `feedback_validation_at_http_boundary` discipline). |
| `dateOfBirth` | date | Required. Age-validation gating not modeled here. |
| `address` | structured address | Required. Format-validation at HTTP boundary. |
| `cityOfOperation` | string | Required. Enum-validation against operator-configured operating cities is post-MVP. |
| `phoneNumber` | string | Required. E.164 format validation at HTTP boundary. |
| `submittedAt` | timestamp | Server clock at command receipt. |

#### Event — `PersonalInfoSubmitted`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | |
| `legalName` | `{ first, middle?, last }` | Echoed for projection convenience. |
| `dateOfBirth` | date | |
| `address` | structured address | |
| `cityOfOperation` | string | |
| `phoneNumber` | string | |
| `occurredAt` | timestamp | Server clock at handler-fire time. Equals `submittedAt` from the command. |

Payload denormalized — Application's process stream is the system-of-record for what the applicant submitted.

#### Handler shape (PM-via-Handlers continue-handler discipline per §3.6)

```csharp
[AggregateHandler]
public static class SubmitPersonalInfoHandler
{
    public static IEnumerable<object> Handle(
        SubmitPersonalInfo command,
        ApplicationState state,
        TimeProvider clock)
    {
        // Guard #1: terminal-state guard
        if (state.IsTerminal) yield break;

        // Guard #2: step-level idempotency guard
        if (state.PersonalInfoComplete) yield break;

        yield return new PersonalInfoSubmitted(
            ApplicationId: command.ApplicationId,
            LegalName: command.LegalName,
            DateOfBirth: command.DateOfBirth,
            Address: command.Address,
            CityOfOperation: command.CityOfOperation,
            PhoneNumber: command.PhoneNumber,
            OccurredAt: clock.GetUtcNow());

        // Note: ApplicationAbandonmentTimeout is re-scheduled by the abandonment-handler's
        // let-state-decide check reading state.LastSignificantActivityAt; no explicit
        // re-schedule needed here. (See §3.7's let-state-decide discipline.)
    }
}
```

#### Rejection reasons

`APPLICATION_NOT_FOUND`, `APPLICATION_ALREADY_TERMINAL` (caught by guard #1; silent no-op + observability signal), `PERSONAL_INFO_ALREADY_SUBMITTED` (caught by guard #2; silent no-op + observability signal), `NOT_THE_APPLICANT` (authenticated subject doesn't match `state.SubjectId`).

Per `feedback_validation_at_http_boundary`: format/structure validation (E.164 phone format, ISO-3166 country code, etc.) lives in the HTTP endpoint's `Before`/`Validate` middleware via FluentValidation. The aggregate handler only rejects illegal state transitions, not malformed data.

#### GWT sketches

**Happy path**
```
Given: ApplicationOpened { applicationId: A, subjectId: S, ... } exists
  And: state.PersonalInfoComplete = false; state.Status = Opened
When: SubmitPersonalInfo { applicationId: A, ..., submittedAt: T }
       arrives from authenticated subject S
Then: PersonalInfoSubmitted { applicationId: A, ...full payload..., occurredAt: T } is emitted
  And: state.PersonalInfoComplete = true (via Apply)
  And: state.LastSignificantActivityAt = T
  And: ApplicantDashboardView updated inline (personalInfoComplete: true)
```

**Idempotent re-submission**
```
Given: PersonalInfoSubmitted already exists; state.PersonalInfoComplete = true
When: SubmitPersonalInfo { applicationId: A, ... } arrives again
Then: handler guard #2 fires; yield break
  And: no second PersonalInfoSubmitted emitted
  And: HTTP response surfaces as "already submitted" (200 idempotent or 409 — project convention to be locked at implementation time)
```

**Stale submission (application already terminal)**
```
Given: state.Status = Withdrawn (or Abandoned / Approved / Rejected)
When: SubmitPersonalInfo arrives
Then: handler guard #1 fires; yield break
  And: HTTP response surfaces as "application no longer active"
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Command via continue handler with `[AggregateHandler]`. Standard PM-via-Handlers shape per §3.6. |
| Trigger | Driver-app applicant-dashboard explicit form submission. gRPC unary. |
| Idempotency | Aggregate-level via Marten optimistic concurrency + step-level idempotency guard on `state.PersonalInfoComplete`. Re-submission → silent no-op. |
| Validation | HTTP boundary (FluentValidation via Wolverine.HTTP `Before`/`Validate`); handler only rejects illegal state transitions. |
| 2N guard discipline | Both guards present (terminal-state + step-level idempotency). Slice test plan asserts both. |
| Rejection reasons | `APPLICATION_NOT_FOUND`, `APPLICATION_ALREADY_TERMINAL` (silent no-op), `PERSONAL_INFO_ALREADY_SUBMITTED` (silent no-op), `NOT_THE_APPLICANT`. |
| Event payload | Denormalized — full personal-info on the event for projection convenience and audit. |
| `LastSignificantActivityAt` bump | Via `Apply(PersonalInfoSubmitted)` — re-arms abandonment-timeout's let-state-decide check on the next abandonment-timer fire. |

#### Cross-references and ripples

- **Backward:** Slice 6.1's `ApplicationOpened` is the predecessor; this slice's handler loads the aggregate it created.
- **Forward (Slice 6.8):** This slice satisfies one of the three approval-gate prerequisites; cascaded `ApplicationApproved` cannot fire until `state.PersonalInfoComplete = true`.
- **Abandonment-timer interaction (§3.7):** Each significant activity event re-arms the let-state-decide check via `state.LastSignificantActivityAt`; no explicit re-schedule needed.

### 6.3 Slice 3 — BackgroundCheckConsentGiven (Command from applicant)

**Pattern:** Command (continue handler). Structurally identical to Slice 6.2.
**Lane:** Onboarding.
**Trigger:** Applicant submits SSN + FCRA-compliant background-check consent on driver-app applicant-dashboard.

#### Flow on the board

Structurally identical to Slice 6.2's flow with the obvious substitutions:
- Command: `GiveBackgroundCheckConsent` instead of `SubmitPersonalInfo`
- Event: `BackgroundCheckConsentGiven` instead of `PersonalInfoSubmitted`
- Gate bit: `state.BackgroundCheckConsentGiven` instead of `state.PersonalInfoComplete`

#### Command — `GiveBackgroundCheckConsent`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | |
| `ssn` | string | **Sensitive PII.** Validated at HTTP boundary (format only). Stored encrypted-at-rest per ADR-009 future protobuf scalar choices. |
| `consentVersion` | string | The version of the FCRA-compliant authorization text the applicant signed. Operator-tunable via `OnboardingPolicyConfigured`. |
| `consentSignedAt` | timestamp | Server clock at signature receipt. |

#### Event — `BackgroundCheckConsentGiven`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | |
| `ssnHash` | string | **Not the raw SSN.** Hashed at the boundary; raw SSN persisted separately encrypted per ADR-009 + project's standard sensitive-PII discipline (to be authored when the first implementation lands). The hash on the event is for vendor-correlation purposes (re-resolving raw SSN via Onboarding's vault). |
| `consentVersion` | string | |
| `consentSignedAt` | timestamp | |
| `occurredAt` | timestamp | Server clock at handler-fire time. Equals `consentSignedAt`. |

**Sensitive-PII handling refinement:** The event carries `ssnHash`, not the raw SSN. Raw SSN persists in a separate encrypted vault (out of scope for this workshop — vault implementation is a follow-up). This protects the event log from being a high-value PII target while still enabling vendor-submission flows (Slice 6.5) that need the raw value via vault-lookup.

#### Handler shape

Structurally identical to Slice 6.2's `SubmitPersonalInfoHandler` with the obvious substitutions. Both 2N guards present (terminal-state + `state.BackgroundCheckConsentGiven` step-level idempotency).

#### GWT sketches

**Happy path** — structurally identical to Slice 6.2's happy path; substitutes `BackgroundCheckConsentGiven` for `PersonalInfoSubmitted` and `state.BackgroundCheckConsentGiven = true` for `state.PersonalInfoComplete = true`.

**Idempotent re-submission** — same structure; silent no-op via guard #2.

**Stale submission** — same structure; silent no-op via guard #1.

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Same as Slice 6.2 — Command via continue handler. |
| SSN handling | Event carries `ssnHash`, not raw SSN. Raw SSN in separate encrypted vault. Vault implementation deferred to follow-up; sensitive-PII discipline ADR/skill is a follow-up trigger. |
| `consentVersion` carriage | Yes — explicit on event for FCRA audit-trail purposes ("which consent text version did this applicant sign?"). |
| `consentSignedAt` vs `occurredAt` | Same value at first-fire; carried as separate fields for future use cases where signature pre-dates handler-fire (e.g., admin replay scenarios). |
| 2N guard discipline | Both guards present. |
| Rejection reasons | `APPLICATION_NOT_FOUND`, `APPLICATION_ALREADY_TERMINAL` (silent), `CONSENT_ALREADY_GIVEN` (silent), `NOT_THE_APPLICANT`. |

#### Cross-references and ripples

- **Backward:** Slice 6.1's `ApplicationOpened`. (No dependency on Slice 6.2 — personal-info and consent are independent gates; either order valid.)
- **Forward (Slice 6.5):** Background-check request fires only when *both* personal-info-complete AND consent-given (plus documents-section-complete). This slice is one of the three prerequisites.
- **Forward (Slice 6.8):** Approval-gate prerequisite.
- **New parking-lot item:** Sensitive-PII vault implementation + ADR/skill. Trigger: first implementation session for this slice.
- **Abandonment-timer interaction (§3.7):** Same as Slice 6.2.

### 6.4 Slice 4 — DocumentUploaded + DocumentVerified (happy path; paired with 6.4b)

**Pattern:** Command (continue handler) + Translation-out (gRPC to vendor) + Translation-in (Klefter decision-event recording on response).
**Lane:** Onboarding for handler + local events; external document-verification vendor for the translation-out call.
**Trigger:** Applicant uploads document via driver-app applicant-dashboard.

The paired walk with 6.4b covers happy-path and recovery-path together; both share the same Translation-out + Translation-in handler pair. Heavy slice per W001 §12.6 #3 — first realized vendor edge of the multi-vendor ACL pattern.

#### Flow on the board

```
   ┌──────────────────────────────────┐
   │ Applicant uploads document via   │
   │ driver-app (gRPC unary)          │
   └──────────────┬───────────────────┘
                  │ UploadDocument command
                  │ { applicationId, documentId, type, bytes }
                  ▼
   ┌──────────────────────────────────┐
   │ [AggregateHandler]                │
   │ loads ApplicationState            │
   │ Guards: terminal + per-doc step   │
   │ idempotency (no existing doc      │
   │ entry at documentId)              │
   └──────────────┬───────────────────┘
                  │ yield return DocumentUploaded
                  │ (Document storage to blob, out of model)
                  ▼
   ┌──────────────────────────────────┐
   │ DocumentUploaded                 │  orange event
   │ (Application stream)             │  state.Documents[id]=UnderVerification
   └──────────────┬───────────────────┘
                  │ Automation triggered by event:
                  │ DocumentVerificationAutomation
                  ▼
   ┌──────────────────────────────────┐
   │ DocumentVerificationAutomation   │  gear sticky (event-driven)
   │ Reads document blob via doc-     │
   │ storage; calls vendor gRPC unary │
   └──────────────┬───────────────────┘
                  │ gRPC unary call
                  │ VerifyDocument(documentId, bytes)
                  ▼
   ┌──────────────────────────────────┐
   │ document-verification vendor     │  external actor
   │ (Onfido / Veriff / Persona /     │
   │  Jumio class)                    │
   │ Returns: { status, reasonCode? } │
   └──────────────┬───────────────────┘
                  │ vendor returns "verified, authentic"
                  ▼
   ┌──────────────────────────────────┐
   │ DocumentVerificationAutomation   │
   │ (continues on response)          │
   │ Klefter: translation at boundary │
   │ vendor reason → CritterCab enum  │
   └──────────────┬───────────────────┘
                  │ Issues VerifyDocumentResult command
                  ▼
   ┌──────────────────────────────────┐
   │ [AggregateHandler]                │
   │ Guards: terminal + per-doc step   │
   │ (doc still UnderVerification)     │
   └──────────────┬───────────────────┘
                  │ yield return DocumentVerified
                  ▼
   ┌──────────────────────────────────┐
   │ DocumentVerified                 │  orange event
   │ (Application stream)             │  state.Documents[id]=Verified
   └──────────────────────────────────┘
                  │
                  ▼
   Views updated:
     • ApplicantDashboardView      (inline; document marked verified)
     • OnboardingFunnelView        (async)
```

#### Command — `UploadDocument`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | The Application receiving the document. |
| `documentId` | UUIDv7 | Server-assigned when this is a first upload; client-supplied when it's a re-upload (per Slice 6.4b's identity-preservation discipline). The handler validates the documentId-vs-state relationship. |
| `documentType` | enum `{ DRIVERS_LICENSE, VEHICLE_REGISTRATION, INSURANCE_CERTIFICATE }` | Required. Determines downstream vendor routing if multi-vendor (post-MVP). |
| `bytes` | binary or upload-URL handle | Document binary. Out-of-band storage (blob store) is out of model scope; the event carries a storage handle, not the bytes themselves. |
| `uploadedAt` | timestamp | Server clock at command receipt. |

#### Event — `DocumentUploaded`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | |
| `documentId` | UUIDv7 | Per-document identity; preserved through any rejection cycle per W003 Story 2 Q2a.2. |
| `documentType` | enum | Echoed. |
| `storageHandle` | string (blob ref) | Reference to where the document binary lives (e.g., Azure Blob Storage URL or pseudo-URL). Out-of-band; event carries the handle, not the bytes. |
| `occurredAt` | timestamp | Server clock at handler-fire time. |

#### Translation-out: `VerifyDocument` (gRPC unary to vendor)

| Aspect | Value |
|---|---|
| Transport | gRPC unary (ADR-005's service-to-service shape) |
| Target | Document-verification vendor (external; outside any CritterCab BC per W003 §5.2 B4) |
| Request payload | `{ documentId, bytes (via storageHandle resolution), documentType, applicationId }` |
| Response payload | `{ status: "verified" \| "rejected", reasonCode?: vendor-specific string, vendorCaseId: opaque }` |
| Retry policy | Wolverine-managed retry per ADR-005; exhausted retries → DLQ + observability signal. |
| Timeout | From `OnboardingPolicy.DocumentVendorTimeout` (operator-tunable per ADR-011) |

#### Klefter decision-event recording on translation-in

The `DocumentVerificationAutomation`'s response handler translates the vendor's response into a CritterCab-domain decision-event. Klefter pattern applies: even though the vendor has its own record of the verification, Onboarding records the *consequence* (`DocumentVerified` or `DocumentRejected`) in its own event store, decoupled from the vendor's vocabulary and retention policy. Future vendor swaps (Onfido → Veriff, etc.) leave Onboarding's events unchanged.

#### Canonical CritterCab `DocumentRejectionReason` enum (W003 V8 normalization)

Vendor-specific reason strings (e.g., Onfido's `image_quality_low`, Veriff's `BLURRY_PHOTO`, Persona's `IMG_QUALITY`) translate to a CritterCab-domain enum at the ACL boundary:

```csharp
public enum DocumentRejectionReason
{
    PoorImageQuality,           // blurry, glare, lighting, low resolution
    DocumentTypeMismatch,       // applicant uploaded the wrong document type
    DocumentExpired,            // license/registration expired
    DocumentDamagedOrAltered,   // tampering, water damage, etc.
    InformationMismatch,        // name/DOB on document doesn't match application
    UnsupportedDocumentFormat,  // vendor cannot process this document
    Other                       // free-form fallback for unmatched vendor reasons
}
```

The translation lives in `DocumentVerificationAutomation`; a per-vendor mapping table (vendor reason-code string → CritterCab enum) is a follow-up artifact (translation table is a skill-file or DEBT.md row). The mapping is part of the ACL boundary's discipline.

#### Handler shape (continue handler for `VerifyDocumentResult` command)

```csharp
[AggregateHandler]
public static class VerifyDocumentResultHandler
{
    public static IEnumerable<object> Handle(
        VerifyDocumentResult command,
        ApplicationState state,
        TimeProvider clock)
    {
        // Guard #1: terminal-state
        if (state.IsTerminal) yield break;

        // Guard #2: per-document step idempotency
        // (document must be in UnderVerification state to receive a verification result)
        if (!state.Documents.TryGetValue(command.DocumentId, out var docState))
            yield break;
        if (docState != DocumentState.UnderVerification) yield break;

        if (command.Status == VendorStatus.Verified)
        {
            yield return new DocumentVerified(
                ApplicationId: state.Id,
                DocumentId: command.DocumentId,
                VendorCaseId: command.VendorCaseId,
                OccurredAt: clock.GetUtcNow());
        }
        else // Rejected
        {
            yield return new DocumentRejected(
                ApplicationId: state.Id,
                DocumentId: command.DocumentId,
                Reason: command.TranslatedReason,         // CritterCab enum
                VendorCaseId: command.VendorCaseId,
                OccurredAt: clock.GetUtcNow());
        }
    }
}
```

#### Event — `DocumentVerified`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | |
| `documentId` | UUIDv7 | |
| `vendorCaseId` | opaque | Vendor's case ID for traceability. ACL discipline: vendor's term carried as opaque value, not interpreted. |
| `occurredAt` | timestamp | |

#### GWT sketches

**Happy path — first upload verifies**
```
Given: state.Documents is empty; state.Status = Opened
When: UploadDocument { applicationId: A, documentId: D, type: DRIVERS_LICENSE, ... } arrives
Then: DocumentUploaded { ... } emitted
  And: state.Documents[D] = UnderVerification
  And: DocumentVerificationAutomation triggered, calls vendor gRPC
  And: vendor returns { status: verified, vendorCaseId: V1 }
  And: VerifyDocumentResult { documentId: D, status: Verified, vendorCaseId: V1 } issued
  And: DocumentVerified { applicationId: A, documentId: D, vendorCaseId: V1, ... } emitted
  And: state.Documents[D] = Verified
  And: ApplicantDashboardView shows DRIVERS_LICENSE verified
```

**Idempotent re-verification (vendor webhook redelivery)**
```
Given: state.Documents[D] = Verified (already verified)
When: VerifyDocumentResult { documentId: D, status: Verified, ... } arrives again
Then: guard #2 fires (docState != UnderVerification); yield break
  And: no second DocumentVerified emitted
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Command + Translation-out (gRPC to vendor) + Translation-in (Klefter decision-event recording on response). |
| Vendor transport | gRPC unary (ADR-005 service-to-service shape) for the verification call; webhook-style response handling for async vendor responses (if vendor uses webhooks instead of synchronous gRPC, a webhook-receiver endpoint feeds into the same `VerifyDocumentResult` command flow). |
| Document identity | UUIDv7 minted server-side at first upload. Preserved through any rejection cycle per W003 V7. |
| Event-store substrate for document state | Per-document state derived from event ordering on the parent Application's process stream. No separate Document aggregate. Per §3.4 / W003 OQ-2/OQ-3 resolution. |
| Storage of document bytes | Out-of-band blob store; event carries storage handle only. Blob store implementation deferred. |
| Vendor reason-code translation | At the ACL boundary in `DocumentVerificationAutomation`. Canonical CritterCab `DocumentRejectionReason` enum committed here. Per-vendor mapping table is a follow-up artifact. |
| Translation-out event | None — vendor call is gRPC unary, not an event. Per Klefter Post 3: routing-only translations don't get local events; decision-translations do. Verification *result* is a decision (the local event); the *call out* is routing. |
| 2N guard discipline | Both guards present. Guard #2 specialized: check document is in `UnderVerification` state. |
| Idempotency | Per-document via state-machine check + vendor `vendorCaseId` correlation for cross-event audit. |

#### Cross-references and ripples

- **Backward:** Slice 6.1's `ApplicationOpened`; Slice 6.2's `PersonalInfoSubmitted` is a prerequisite (per W003 Story 1 ordering).
- **Forward (Slice 6.4b):** When vendor returns "rejected" instead, the recovery path activates.
- **Forward (Slice 6.5):** BG-check vendor's translation follows the same pattern — vendor reason → CritterCab enum at ACL boundary. The discipline committed here is re-applied verbatim.
- **Forward (Slice 6.8):** Documents-section-complete is one of the three approval-gate prerequisites; satisfied when all required documents are `Verified`.
- **Multi-vendor ACL pattern §11 candidate:** First *realized* vendor edge (OIDC was inherited per ADR-006). The reason-code translation discipline + `vendorCaseId` opaque carriage are the pattern's signature.
- **DEBT.md follow-ups:** (a) per-vendor mapping table (vendor reason → CritterCab enum); (b) blob-storage handle resolution skill/discipline.

### 6.4b Slice 4b — DocumentRejected + DocumentReuploaded (recovery; paired with 6.4)

**Pattern:** Translation-in (vendor returns rejected) + Command (applicant re-uploads) + Translation-in (re-verification).
**Lane:** Onboarding.
**Trigger:** Vendor returns "rejected" on a previously-uploaded document.

#### Flow on the board

```
   (Slice 6.4 flow continues from DocumentVerificationAutomation calling vendor)
                  │ vendor returns "rejected, image quality"
                  ▼
   ┌──────────────────────────────────┐
   │ Translation: vendor reason →     │
   │ CritterCab DocumentRejectionReason│
   │ enum (W003 V8 normalization)      │
   └──────────────┬───────────────────┘
                  │ VerifyDocumentResult { status: Rejected, ... }
                  ▼
   ┌──────────────────────────────────┐
   │ DocumentRejected                 │  orange event
   │ state.Documents[D] = Rejected     │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ Applicant notification           │
   │ (driver-app surfaces the         │
   │  translated rejection reason)    │
   │ Inline projection update:        │
   │   ApplicantDashboardView shows   │
   │   "Photo too blurry — try again" │
   └──────────────┬───────────────────┘
                  │
                  │ Applicant re-uploads
                  ▼
   ┌──────────────────────────────────┐
   │ ReuploadDocument command          │
   │ { applicationId, documentId,      │
   │   bytes }                         │
   │ documentId MATCHES the previously │
   │ rejected document — identity      │
   │ preserved per W003 Story 2 Q2a.2 │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ [AggregateHandler]                │
   │ Guards: terminal + step-level    │
   │ (state.Documents[D] = Rejected)   │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ DocumentReuploaded               │  orange event
   │ state.Documents[D] =              │
   │   UnderVerification (transition)  │
   └──────────────┬───────────────────┘
                  │ Re-triggers
                  │ DocumentVerificationAutomation
                  │ (vendor re-call, may return verified or rejected again)
                  ▼
            (continues as Slice 6.4 from VerifyDocument call)
```

#### Command — `ReuploadDocument`

Structurally identical to `UploadDocument` but the `documentId` field is required (not server-assigned). The handler validates that `documentId` references an existing rejected document on this Application.

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | |
| `documentId` | UUIDv7 | **Required, client-supplied.** Must match a `state.Documents[id] = Rejected` entry. Per W003 Story 2 Q2a.2 identity preservation. |
| `bytes` | binary or upload-URL handle | New attempt's document binary. |
| `reuploadedAt` | timestamp | Server clock at command receipt. |

#### Event — `DocumentRejected`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | |
| `documentId` | UUIDv7 | Per-document identity preserved. |
| `reason` | `DocumentRejectionReason` enum | Translated at ACL boundary per §6.4's canonical enum. |
| `vendorCaseId` | opaque | Vendor's case ID for this rejection attempt. Each rejection attempt has its own `vendorCaseId`; re-uploads generate new vendor calls and thus new `vendorCaseId`s. |
| `occurredAt` | timestamp | |

#### Event — `DocumentReuploaded`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | |
| `documentId` | UUIDv7 | Same documentId as the previously rejected upload. |
| `storageHandle` | string | New blob reference for the re-upload's binary. |
| `occurredAt` | timestamp | |

#### Handler shape — `ReuploadDocumentHandler`

```csharp
[AggregateHandler]
public static class ReuploadDocumentHandler
{
    public static IEnumerable<object> Handle(
        ReuploadDocument command,
        ApplicationState state,
        TimeProvider clock)
    {
        // Guard #1: terminal-state
        if (state.IsTerminal) yield break;

        // Guard #2: document must be in Rejected state to be re-uploadable
        if (!state.Documents.TryGetValue(command.DocumentId, out var docState))
            yield break;
        if (docState != DocumentState.Rejected) yield break;

        yield return new DocumentReuploaded(
            ApplicationId: state.Id,
            DocumentId: command.DocumentId,
            StorageHandle: command.StorageHandle,
            OccurredAt: clock.GetUtcNow());
    }
}
```

`Apply(DocumentReuploaded)` transitions `state.Documents[D]` from `Rejected` back to `UnderVerification`. The re-upload re-triggers `DocumentVerificationAutomation`, which calls the vendor with the new attempt's bytes. The vendor's response flows through the same `VerifyDocumentResult` handler from §6.4.

#### Document history persistence — W003 OQ-3 resolution

The event-stream ordering on the Application's process stream **is** the document's history. Reading the stream filtered by `documentId`:

```
DocumentUploaded { documentId: D, occurredAt: T0 }       — first upload
DocumentRejected { documentId: D, reason: PoorImageQuality, occurredAt: T1 }
DocumentReuploaded { documentId: D, occurredAt: T2 }     — re-upload
DocumentVerified { documentId: D, occurredAt: T3 }       — final terminal
```

No separate document-history projection is required for replay — the event stream is the history. Optional: a `DocumentTimeline` per-document projection for ops tooling that wants to query history without re-walking the stream. **Deferred but pinned.**

#### GWT sketches

**Recovery path — reject then re-upload verifies**
```
Given: DocumentUploaded { documentId: D } emitted earlier; state.Documents[D] = UnderVerification
When: VerifyDocumentResult { documentId: D, status: Rejected, reason: PoorImageQuality, vendorCaseId: V1 }
Then: DocumentRejected { documentId: D, reason: PoorImageQuality, vendorCaseId: V1, ... } emitted
  And: state.Documents[D] = Rejected
  And: ApplicantDashboardView shows D as rejected with translated reason "Photo too blurry — try again"
When: ReuploadDocument { documentId: D, bytes: new-attempt } arrives
Then: DocumentReuploaded { documentId: D, ... } emitted
  And: state.Documents[D] = UnderVerification (transition back)
  And: DocumentVerificationAutomation re-triggers; calls vendor with new bytes
  And: vendor returns { status: Verified, vendorCaseId: V2 }
  And: DocumentVerified { documentId: D, vendorCaseId: V2, ... } emitted
  And: state.Documents[D] = Verified
```

**Re-upload before vendor responds (race condition)**
```
Given: state.Documents[D] = UnderVerification (vendor call still in flight)
When: ReuploadDocument { documentId: D } arrives
Then: guard #2 fires (docState != Rejected); yield break
  And: HTTP response surfaces as "wait for current verification to complete"
```

**Repeated rejections (vendor rejects re-upload too)**
```
Given: DocumentRejected emitted; state.Documents[D] = Rejected
When: ReuploadDocument arrives → DocumentReuploaded emitted → state.Documents[D] = UnderVerification
  And: vendor returns Rejected again with reason: DocumentExpired
Then: second DocumentRejected emitted; state.Documents[D] = Rejected again
  Note: no retry-budget enforced at modeling level. OnboardingPolicy may define
        max-rejection-attempts in the future; deferred.
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Document identity preservation | Per-`documentId` identity maintained through any number of rejection cycles. Re-upload requires existing `documentId`. Per W003 Story 2 Q2a.2 + W003 V7 distinction (`DocumentRejected` ≠ `ApplicationRejected`). |
| Document history persistence | Event-stream ordering filtered by `documentId`. No separate projection required for history; optional `DocumentTimeline` projection deferred but pinned for ops-tooling future need. Resolves W003 OQ-3. |
| Re-upload command shape | `ReuploadDocument` distinct from `UploadDocument` — different guard (Rejected vs. not-yet-uploaded). Distinct event (`DocumentReuploaded` distinct from `DocumentUploaded`) makes history readable. |
| Retry budget | Not enforced at modeling level. Future `OnboardingPolicy.MaxDocumentRejectionAttempts` is a deferred parameter; if set, would trigger `ApplicationRejected` after N rejections per document. Out of scope for W004. |
| Vendor reason on event | Translated CritterCab enum; original vendor string discarded after translation. Per ACL discipline. |
| 2N guard discipline | Both guards present per slice (terminal + per-document-state). |
| Applicant notification | Surfaced via inline projection update on `ApplicantDashboardView`; no separate `ApplicantNotifiedOfRejection` event modeled (the notification is the projection-driven UI surface, not a domain event). |

#### Cross-references and ripples

- **Pair with 6.4:** Shares all infrastructure (`DocumentVerificationAutomation`, `VerifyDocumentResult` handler, vendor translation). Differs only in the response path chosen.
- **Forward (Slice 6.5):** Reinforces the translation-at-ACL-boundary discipline.
- **Forward (Slice 6.8):** Documents-section-complete prerequisite — satisfied only when *all* documents are `Verified` (regardless of intermediate rejection history).
- **W003 OQ-2 + OQ-3 resolution lands here** with concrete realization. Both leans confirmed: events on process stream, no separate Document aggregate; event-stream is the history.
- **DEBT.md follow-up:** retry-budget enforcement (if/when OnboardingPolicy adds the parameter).

### 6.5 Slice 5 — BackgroundCheckRequested + BackgroundCheckCaseOpened + BackgroundCheckStatusReceived (Clear path; paired with 6.6)

**Pattern:** Continue handler (cascaded from `DocumentVerified` completing documents-section) + Translation-out (gRPC to BG-check vendor) + Translation-in pair (sync `BackgroundCheckCaseOpened` + async webhook `BackgroundCheckStatusReceived`).
**Lane:** Onboarding for handlers + local events; external background-check vendor for the translation-out call + async webhook delivery.
**Trigger:** All three prerequisites satisfied — `state.PersonalInfoComplete && state.BackgroundCheckConsentGiven && state.DocumentsSectionComplete`. Per W003 Story 1 step 15.

#### Structural parallelism with §6.4 — what's the same, what's different

Same:
- Multi-vendor ACL pattern (translation at boundary; opaque `vendorCaseId` carriage; Klefter decision-event recording).
- 2N guard discipline.
- Vendor-reason translation at the ACL boundary (this slice's `BackgroundCheckStatus` enum is the analog of 6.4's `DocumentRejectionReason`).
- Wolverine-managed retries on the outbound gRPC; DLQ on exhausted retries.

Different:
- **Vendor response shape is async-by-default.** Where 6.4's vendor returns synchronously, the BG-check vendor returns a sync ack (`BackgroundCheckCaseOpened` carrying vendor case ID + `Pending` status) and then pushes status updates async via webhook (the `BackgroundCheckStatusReceived` event family). Two distinct translation-in handlers.
- **Vendor status enum is richer.** Per W003 V5/V6: `Pending`, `Clear`, `Consider`, `Suspended`. The `Consider` path triggers Slice 6.6's adjudication flow; `Clear` cascades to approval gate-bit; `Suspended` requires applicant action (out of W004 scope).
- **Vendor no-response timeout scheduled** at `BackgroundCheckCaseOpened` via `OutgoingMessages.Delay` per §3.7.

#### Cascade from §6.4

`VerifyDocumentResultHandler` extended: when a document verifies and the cascade-condition is satisfied (documents-section becomes complete + personal-info-complete + consent-given + no BG-check yet), the same handler atomically yields `DocumentVerified` AND `BackgroundCheckRequested`. Atomic dual-emit per W001 §5.5 / W002 §6.6 precedent — third application of the pattern across CritterCab.

#### Three events — BackgroundCheckRequested, BackgroundCheckCaseOpened, BackgroundCheckStatusReceived

| Event | Key fields |
|---|---|
| `BackgroundCheckRequested` | `applicationId`, `requestedAt`. Lean — substance lives on subsequent events. |
| `BackgroundCheckCaseOpened` | `applicationId`, `vendorCaseId` (opaque vendor ID), `openedAt`. Schedules `BackgroundCheckVendorNoResponse` self-message via `OutgoingMessages.Delay`. |
| `BackgroundCheckStatusReceived` | `applicationId`, `vendorCaseId`, `status` (`Pending | Clear | Consider | Suspended` enum), `occurredAt`. **One event type with the enum payload**, not separate events per status — per W003 V5/V6 the enum is part of the vocabulary; one event surface for all status-receipt occurrences. |

#### `BackgroundCheckVendorCaseIndex` — load-bearing inline projection

The vendor's webhook carries `vendorCaseId` but does NOT carry `applicationId`. The webhook receiver resolves `applicationId` via the **`BackgroundCheckVendorCaseIndex`** inline projection:

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `BackgroundCheckVendorCaseIndex` | Onboarding webhook-receiver (internal) | Per-`vendorCaseId`: `applicationId` | `BackgroundCheckCaseOpened` | **Slice-6.5 inline.** Load-bearing for webhook routing. |

The projection MUST be inline-updated on `BackgroundCheckCaseOpened` or the webhook can't route. **Second silent dependency** analogous to §3.12 friction-point #7 — added to friction-point list as #9: *BG-check webhook routing requires inline-updated `BackgroundCheckVendorCaseIndex` projection.*

#### Vendor no-response timeout per §3.7

`BackgroundCheckCaseOpenedHandler` schedules a `BackgroundCheckVendorNoResponse` self-message. Let-state-decide on fire: if `state.BackgroundCheckStatus` set since open, yields nothing. If vendor genuinely silent, escalation to operations (specific shape deferred to Operations BC workshop — forward-constraint).

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Continue handler cascade (gate-trigger) + Translation-out (gRPC) + Translation-in pair (sync ack + async webhook). |
| Vendor response shape | Async-by-default: sync ack returns `vendorCaseId`+`Pending`; webhook delivers terminal status. Two distinct translation-in handlers; one event type with enum payload for status receipt. |
| Status enum | `Pending`, `Clear`, `Consider`, `Suspended` per W003 V5/V6. `Consider` triggers Slice 6.6 adjudication; `Suspended` deferred (W003 mentions but Story 1/2/3 don't exercise; W004 treats as out-of-walk — handler yields event and application stays in pending; resolution deferred). Parking-lot item. |
| `applicationId` resolution from webhook | Via `BackgroundCheckVendorCaseIndex` inline projection. Load-bearing dependency added to §3.12 friction-point list (silent dependency #2). |
| Vendor no-response timeout | Scheduled at `BackgroundCheckCaseOpened` via `OutgoingMessages.Delay` per §3.7; let-state-decide resolution; escalation shape deferred to Operations BC's eventual workshop (forward-constraint). |
| Approval gate-trigger cascade | `VerifyDocumentResultHandler` extended to cascade `BackgroundCheckRequested` when documents-section completes and other two gates already satisfied. Atomic dual-emit per W001 §5.5 / W002 §6.6 precedent. Third application of the pattern. |

#### Cross-references and ripples

- **Backward:** Slices 6.2 + 6.3 + 6.4 + 6.4b's gates must all be satisfied for `BackgroundCheckRequested` to cascade.
- **Forward (Slice 6.6):** `Consider` status triggers adjudication queue.
- **Forward (Slice 6.8):** `Clear` status satisfies the BG-check approval-gate.
- **Forward (Slice 6.10):** `OnboardingPolicy.VendorNoResponseWindow` consumed here.
- **Multi-vendor ACL pattern §11 candidate:** Second realized vendor edge. The pattern's discipline (translation-at-boundary, opaque vendor IDs, Klefter decision-event recording) is now confirmed across two vendor edges.
- **New parking-lot items:** (a) Suspended-status applicant-action flow (out of W004 scope); (b) BG-check vendor escalation shape via Operations BC (forward-constraint on Operations' eventual workshop).
- **§3.12 friction-point #9 (new):** Inline `BackgroundCheckVendorCaseIndex` projection is a silent dependency; webhook routing breaks if not registered inline.

### 6.6 Slice 6 — Background-check Consider → Adjudication (paired with 6.5)

**Pattern:** Translation-in (vendor webhook with `Consider` status) + Command (Onboarding queues adjudication case) + Translation-in (adjudicator decision via human-actor UI).
**Lane:** Onboarding.
**Trigger:** `BackgroundCheckStatusReceived` with `status: Consider` per W003 Story 3 step 18.

**Novel weight in this slice:** First manual-human actor in any CritterCab workshop (Onboarding adjudicator per W003 §5.2 B5). First cross-stream multi-stream projection (`AdjudicatorQueueView*` reads from all Application process streams). First slice where the adjudicator's *queue UI / claim flow / SLA tracking is Operations BC's concern* while *the events are Onboarding's*.

#### Flow on the board

When `BackgroundCheckStatusReceived(Consider)` commits, the status-receipt handler also cascades `AdjudicationCaseQueued`. The adjudicator (via Operations BC tooling, gRPC unary into Onboarding) issues `ClaimAdjudicationCase` → `AdjudicationCaseClaimed`. After review, `DecideAdjudicationCase` → `AdjudicationCaseDecided`. Decision cascades:
- `Clear` → fresh `BackgroundCheckStatusReceived(Clear)` event (overrides vendor `Consider`); downstream gates flow as if vendor had returned Clear directly.
- `Reject` → `PreAdverseActionNoticeIssued` cascade in Slice 6.7.

#### Cross-stream multi-stream projection — AdjudicatorQueueView*

**First instance in CritterCab** of a projection whose rows aggregate across N event streams. Per Marten's multi-stream projection pattern:

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `AdjudicatorQueueView*` | Onboarding adjudicators (via Operations BC tooling) | Per-`adjudicationCaseId` (across all Application streams): `{ adjudicationCaseId, applicationId, subjectId, queuedAt, claimedAt?, adjudicatorId?, claimExpiresAt?, decision?, decidedAt? }` | `AdjudicationCaseQueued`, `AdjudicationCaseClaimed`, `AdjudicationCaseDecided` from every Application stream | **Slice-6.6 async** (Marten async daemon). Bruun todo-list with asterisk suffix. UI/claim-flow/SLA-tracking is Operations BC's concern (forward-constraint). |

The pattern enters the workshop here; future multi-stream projections (Operations queue views, cross-trip ops dashboards) reuse it.

#### Three events — AdjudicationCaseQueued, AdjudicationCaseClaimed, AdjudicationCaseDecided

W003 V5 prefix discipline: events use `AdjudicationCase*` (distinguished from `BackgroundCheckCase*` of Slice 6.5) so the case-homonym does not surface in event names.

| Event | Key fields |
|---|---|
| `AdjudicationCaseQueued` | `applicationId`, `adjudicationCaseId` (UUIDv7), `subjectId`, `vendorFindings` (opaque), `queuedAt` |
| `AdjudicationCaseClaimed` | `applicationId`, `adjudicationCaseId`, `adjudicatorId` (opaque; Operations BC workforce-identity model is forward-constraint), `claimedAt` |
| `AdjudicationCaseDecided` | `applicationId`, `adjudicationCaseId`, `adjudicatorId`, `decision` (`Clear | Reject` enum), `notes?`, `decidedAt` |

#### Adjudicator claim expiry timeout per §3.7

`AdjudicationCaseClaimedHandler` schedules `AdjudicationClaimExpired` via `outgoing.Delay(..., policy.AdjudicatorClaimWindow)`. Let-state-decide on fire: if outcome set, yields nothing. If stale, yields `AdjudicationCaseReleased` (clears `state.AdjudicatorId`); queue projection sees the release.

#### `Clear` override cascade

`DecideAdjudicationCaseHandler` on `Clear` emits both `AdjudicationCaseDecided` AND a fresh `BackgroundCheckStatusReceived(Clear)`. Adjudicator overrides vendor's `Consider`. Downstream gate-check flows identically to vendor-direct-Clear.

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Three handlers in sequence: cascade from Slice 6.5's `Consider`; Command (claim); Command (decide). |
| Cross-stream multi-stream projection | `AdjudicatorQueueView*` — first instance in CritterCab. Bruun todo-list with asterisk suffix. Forward-constraint on Operations BC's eventual workshop for UI/claim-flow/SLA. |
| Adjudicator identity | `adjudicatorId` carried opaque; canonical-actor identity model for Operations staff is Operations BC's concern (forward-constraint). Onboarding does not validate beyond "same claimer == decider". |
| Claim-expiry timeout | Scheduled at `AdjudicationCaseClaimed` per §3.7. Let-state-decide; if stale, yields `AdjudicationCaseReleased`. |
| `Clear` override cascade | Adjudicator-`Clear` emits fresh `BackgroundCheckStatusReceived(Clear)` alongside `AdjudicationCaseDecided`. |
| `Reject` cascade | Separate handler in Slice 6.7 reacting to `AdjudicationCaseDecided(Reject)` emits `PreAdverseActionNoticeIssued`. |
| Adjudicator vocabulary | `Onboarding adjudicator` per W003 §5.2 B5. First manual-human actor in any CritterCab workshop. Inside Onboarding BC; tooling outside (Operations). |
| 2N guard discipline | All three handlers carry both guards. |

#### Cross-references and ripples

- **Backward (Slice 6.5):** Triggered by `BackgroundCheckStatusReceived(Consider)`.
- **Forward (Slice 6.7):** Reject decision cascades to FCRA pre-adverse-action notice flow.
- **Forward (Slice 6.8):** Clear decision contributes to approval-gate satisfaction (via the fresh `BackgroundCheckStatusReceived(Clear)` event).
- **Forward-constraint on Operations BC workshop:** Adjudicator queue UI / claim flow / SLA tracking — Operations consumes `AdjudicatorQueueView*`; specific tooling shape is Operations' workshop concern.
- **Forward-constraint on Identity workshop (or wherever workforce identity lives):** Operations staff identity model — `adjudicatorId` opaque carriage assumes Operations BC owns canonical-actor model for workforce identity. Per ADR-006's parked decision on workforce Entra tenant.
- **Multi-vendor ACL pattern §11 candidate:** Third realized edge (human-actor translation rather than vendor-system; pattern still applies — vendor "Consider" + adjudicator decision both translate to CritterCab events at the boundary).
- **New parking-lot item:** Adjudicator workforce-identity authority model — how `adjudicatorId` is validated (which staff can claim/decide). Deferred to Operations BC workshop.

### 6.7 Slice 7 — PreAdverseActionNoticeIssued + outbound ASB publication (paired with 6.7b)

**Pattern:** Continue handler (cascaded from `AdjudicationCaseDecided(Reject)`) + scheduled self-message via `OutgoingMessages.Delay` + outbound ASB publication. **Atomic triple-emit.**
**Lane:** Onboarding for handler + local event; ASB outbound to Notifications BC (eventual workshop pending — forward-constraint).
**Trigger:** `AdjudicationCaseDecided` with `decision: Reject` per W003 Story 3 step 24.

#### Atomic triple-emit

The handler emits THREE things in one transactional commit:

1. **Local event** `PreAdverseActionNoticeIssued` on the Application's process stream.
2. **Scheduled self-message** `DisputeWindowExpired` via `outgoing.Delay(..., policy.FcraDisputeWindow)`.
3. **Outbound ASB publication** `AdverseActionNoticeRequired` to topic `onboarding.adverse-action-notice-required`.

All three commit atomically via Wolverine's outbox + Marten's stream append. The Notifications BC sees the publication only if local commit succeeds. Mirrors W001 §5.5's atomic dual-emit pattern; extended to three emits per the FCRA's audit requirements.

#### Bruun carry-the-value applied a third time

`PreAdverseActionNoticeIssued` carries `windowExpiresAt` on the event itself, computed as `now() + policy.FcraDisputeWindow` at handler-fire time. **Survives mid-flight `OnboardingPolicyConfigured` changes** — a policy change after this event commits does NOT retroactively shift this application's dispute-window deadline. Mirrors W001 §5.4 (`OfferExpired.expiresAt`) and W002 §6.3 (`DriverArrivedAtPickup.expiresAt`). Third application of the pattern across CritterCab.

#### Outbound ASB publication — Notifications BC forward-constraint

Per W003 OQ-10d / grill #3, FCRA notices need to result in actual email/SMS delivery; the reference-architecture-aligned shape is cross-BC publication to ASB rather than Onboarding-internal `IFcraNoticeSender` integration.

| Aspect | Value |
|---|---|
| Topic | `onboarding.adverse-action-notice-required` (ADR-014 convention) |
| Session key | `applicationId` (ADR-013 / ADR-014 combination) |
| Wire format | Protobuf `AdverseActionNoticeRequired` (deferred authorship — names a new contract for Notifications BC's workshop) |
| Payload | `{ applicationId, subjectId, phase: PreAdverse | FinalAdverse, reportSnapshotHandle, windowExpiresAt }` |
| Consumer | Notifications BC (eventual workshop pending) — consumes publication, dispatches email/SMS via SendGrid/Twilio class vendor |

**Forward-constraint generated:** Notifications BC's eventual workshop must consume `onboarding.adverse-action-notice-required` and dispatch the notification per FCRA's content requirements. (The FCRA-required content lives in `reportSnapshotHandle`; Notifications BC interprets and renders.)

#### Event — `PreAdverseActionNoticeIssued`

| Field | Shape | Notes |
|---|---|---|
| `applicationId` | UUIDv7 | |
| `adjudicationCaseId` | UUIDv7 | The case whose Reject decision triggered this notice. |
| `reportSnapshotHandle` | string (blob ref) | FCRA-required: BG-check report content the applicant has the right to receive. Out-of-band; event carries handle. |
| `windowExpiresAt` | timestamp | `now() + policy.FcraDisputeWindow`. Bruun carry-the-value. |
| `policyVersion` | opaque | Which `OnboardingPolicy` version was in effect at handler-fire time. Audit trail. |
| `occurredAt` | timestamp | |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Continue handler + atomic triple-emit (event + scheduled self-message + outbound ASB publication). |
| Outbound publication | `onboarding.adverse-action-notice-required` ASB topic per ADR-014 + W003 OQ-10d / grill #3. Notifications BC consumer pending (forward-constraint). |
| Bruun carry-the-value | `windowExpiresAt` carried on event; survives policy changes. Third application across CritterCab. |
| `policyVersion` on event | Yes — audit trail for which policy was in effect at notice issuance. |
| Dispute window scheduling | `OutgoingMessages.Delay(DisputeWindowExpired, policy.FcraDisputeWindow)` per §3.7. Let-state-decide resolution in 6.7b. |
| Outbound publication payload | `reportSnapshotHandle` for FCRA-required content; out-of-band blob storage. |
| 2N guard discipline | Both guards present. Guard #2: `state.FcraPhase == null` (no prior notice issued). |
| Atomic triple-emit | Wolverine outbox + Marten append; all three commit atomically or unwind together. |

#### Cross-references and ripples

- **Backward (Slice 6.6):** Triggered by `AdjudicationCaseDecided(Reject)`.
- **Forward (Slice 6.7b):** Scheduled `DisputeWindowExpired` fires; let-state-decide resolution.
- **Forward-constraints on Notifications BC workshop:** (a) Consume `onboarding.adverse-action-notice-required`; render FCRA-compliant notification. (b) Wire format Protobuf `AdverseActionNoticeRequired` contract to be authored.
- **ADR-013:** Fourth-instance materialization on the publication side (`applicationId` is session key).
- **ADR-014:** Topic naming convention applied — `onboarding.adverse-action-notice-required` is the fourth Onboarding-prefixed topic if `onboarding.driver-approved` (Slice 6.8) counts.
- **Bruun carry-the-value pattern:** Third application across CritterCab. Portable beyond temporal-automation contexts.

### 6.7b Slice 7b — DisputeWindowExpired + FinalAdverseActionNoticeIssued + ApplicationRejected (canonical let-state-decide; paired with 6.7)

**Pattern:** Continue handler over self-scheduled timeout. **Canonical let-state-decide demonstration per Wolverine guide §3 step 6.**
**Lane:** Onboarding.
**Trigger:** `DisputeWindowExpired` self-message arrives via Wolverine outbox after `policy.FcraDisputeWindow` elapses (per the schedule from 6.7).

#### Let-state-decide — the cleanest ergonomic win of PM via Handlers

State is authoritative. **No "cancel the timer" call exists or is needed.** Three branches when the timer fires:

1. **Application already terminal** (withdrew / abandoned / approved via some other path) → guard #1 fires; yield break. No harm.
2. **Applicant filed a dispute during the window** → `state.FcraPhase` indicates dispute received; guard #2 fires; yield break. Dispute-handling flow takes over (modeled out of W004 scope).
3. **Window elapsed without dispute** → emit `FinalAdverseActionNoticeIssued` + outbound publication + `ApplicationRejected` (terminal cascade).

```csharp
[AggregateHandler]
public static class DisputeWindowExpiredHandler
{
    public static IEnumerable<object> Handle(
        DisputeWindowExpired message,
        ApplicationState state,
        TimeProvider clock)
    {
        if (state.IsTerminal) yield break;
        if (state.FcraPhase != FcraPhase.PreAdverseIssued) yield break;

        yield return new DisputeWindowExpired(
            ApplicationId: state.Id,
            OccurredAt: clock.GetUtcNow());

        yield return new FinalAdverseActionNoticeIssued(
            ApplicationId: state.Id,
            OccurredAt: clock.GetUtcNow());

        yield return new ApplicationRejected(
            ApplicationId: state.Id,
            Cause: ApplicationRejectionCause.FcraFinalAdverseAction,
            OccurredAt: clock.GetUtcNow());

        // Outbox queues the second outbound publication
        // onboarding.adverse-action-notice-required with phase: FinalAdverse.
    }
}
```

#### `ApplicationRejectionCause` enum (BC-owned per `feedback_bc_owned_enums`)

```csharp
public enum ApplicationRejectionCause
{
    FcraFinalAdverseAction,       // Slice 6.7b path
    DocumentRejectionTerminal,    // Future: if OnboardingPolicy.MaxDocumentRejectionAttempts exceeded
    AdjudicatorRejectNonFcra,     // Future: non-BG-check adjudication rejection paths
    Other
}
```

W004 scope only materializes `FcraFinalAdverseAction`; other values reserved for future scope expansion.

#### Atomic quadruple-emit

`DisputeWindowExpired` (the self-message-as-fact), `FinalAdverseActionNoticeIssued`, `ApplicationRejected`, and the second outbound `onboarding.adverse-action-notice-required` (phase: FinalAdverse) all commit in one transactional commit.

#### GWT sketches

**Happy path (sad for applicant) — window elapses without dispute**
```
Given: PreAdverseActionNoticeIssued committed at T0; windowExpiresAt = T0 + 5 days
  And: state.FcraPhase = PreAdverseIssued
When: DisputeWindowExpired self-message arrives at T0 + 5 days
Then: DisputeWindowExpired event emitted on stream
  And: FinalAdverseActionNoticeIssued emitted
  And: ApplicationRejected(cause: FcraFinalAdverseAction) emitted
  And: state.FcraPhase = FinalAdverseIssued
  And: state.Status = Rejected
  And: outbound onboarding.adverse-action-notice-required (phase: FinalAdverse) published
  And: ApplicantDashboardView updated inline (status: Rejected)
```

**Terminal via other path before window elapsed**
```
Given: applicant withdrew → state.Status = Withdrawn (terminal)
When: DisputeWindowExpired self-message arrives (timer fires regardless)
Then: guard #1 fires; yield break — let-state-decide handles cleanly
```

**Dispute filed (modeling-deferred path)**
```
Given: applicant disputes (future event family transitioning state.FcraPhase to non-PreAdverseIssued)
When: DisputeWindowExpired self-message arrives
Then: guard #2 fires; yield break
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Continue handler over self-scheduled timeout. **Canonical let-state-decide demonstration.** |
| 2N guard discipline | Both guards present. Guard #2 specialized: check `state.FcraPhase == PreAdverseIssued`. |
| Atomic quadruple-emit | `DisputeWindowExpired` + `FinalAdverseActionNoticeIssued` + `ApplicationRejected` + outbound ASB publication. |
| `DisputeWindowExpired` as event | Yes — captures timer-fire as fact on stream for audit. Distinct from the self-message that triggered the handler. |
| `FinalAdverseActionNoticeIssued` payload | Lean — no need to repeat report content (on PreAdverse event); just records final notice fired. |
| `ApplicationRejected` cause enum | `ApplicationRejectionCause` BC-owned enum per `feedback_bc_owned_enums`. W004 materializes only `FcraFinalAdverseAction`. |
| Outbound publication on final notice | Yes — same topic, `phase: FinalAdverse`. Notifications BC dispatches second email/SMS. |
| Dispute flow | **Out of W004 scope.** Modeled-not-walked. Parking-lot item. |

#### Cross-references and ripples

- **Backward (Slice 6.7):** Scheduled `DisputeWindowExpired` self-message.
- **Forward (Slice 6.9):** `ApplicationRejected` terminal — locks absence-of-DP-publication per W003 §5.2 B2 / OQ-10a.
- **PM-via-Handlers pattern §11 candidate:** Canonical let-state-decide demonstration lands here. In-repo reference.
- **Bruun pattern note:** `AdverseActionDisputeWindow*` projection (named in Slice 6.1's catalogue) async-populated from these events. **Projection is for ops visibility, NOT the timeout mechanism.** Slice walk locks the distinction explicitly: Bruun-pattern-as-projection ≠ Bruun-pattern-as-mechanism.
- **Forward-constraint on Notifications BC workshop:** Second publication on `onboarding.adverse-action-notice-required` with `phase: FinalAdverse`. Notifications must handle both phases.
- **Parking-lot item:** Dispute-filing flow (FCRA's "applicant disputes during window" path). Out of W004 scope.

### 6.8 Slice 8 — Approval cascade + Translation-out to Driver Profile

**Pattern:** Continue handler (gate-observer cascading three events + outbound ASB publication). **Atomic quadruple-emit.**
**Lane:** Onboarding for handlers + local events; ASB outbound to Driver Profile (forward-constraint).
**Trigger:** Any gate-completing event whose Apply makes all three approval-gates simultaneously satisfied — concretely, `BackgroundCheckStatusReceived(Clear)` (whether vendor-direct from 6.5 or adjudicator-override from 6.6) is the most common trigger.

#### Three "approved" moments — three distinct events per W003 V4

| Moment | Event | What it captures |
|---|---|---|
| **Application reaches approval terminal** | `ApplicationApproved` | State transition. `state.Status = Approved`. |
| **Applicant is notified** | `ApplicantNotifiedOfApproval` | Notification side effect on the stream. Cross-BC delivery via separate forward-constraint (deferred per slice walk scope). |
| **Driver Profile activation publication** | `DriverActivationPublished` | Cross-BC handoff. Triggers outbound `onboarding.driver-approved` ASB publication. **ADR-013 fourth-participant materialization fires here.** |

W001 §5.8/§5.9 + W002 §6.9 *distinct-events-for-distinct-semantics* pattern applied a fourth time across CritterCab.

#### Atomic quadruple-emit

`ApplicationApproved` + `ApplicantNotifiedOfApproval` + `DriverActivationPublished` + outbound ASB publication `onboarding.driver-approved`. All four commit atomically via Wolverine outbox + Marten append. **Pattern: atomic N-emit for terminals + cross-BC publications.** W001 §5.5 (dual), W002 §6.6 (triple), W004 §6.7 (triple), W004 §6.7b (quadruple), W004 §6.8 (quadruple) — the pattern grows with semantic richness; the discipline (Wolverine outbox guarantees) stays the same.

#### Outbound publication — `onboarding.driver-approved`

| Aspect | Value |
|---|---|
| Topic | `onboarding.driver-approved` per ADR-014 + W003 V1 / OQ-1 (V1's first recommendation accepted) |
| Session key | `driverProfileId` (== `applicationId`) per ADR-013/ADR-014 |
| Wire format | Protobuf `DriverApproved` (deferred authorship) |
| Payload | `{ driverProfileId, subjectId, subjectVocabularyTransition: "applicant → driver", approvedAt }` |
| Consumer | Driver Profile BC (eventual workshop pending) |
| Forward-constraint | Driver Profile consumes, creates/activates DriverProfile aggregate at inherited canonical UUIDv7. **W003 B3 / OQ-12 resolution: approval-push lifecycle-start timing.** |

#### ADR-013 fourth-participant materialization (loop closure)

Slice 6.1 minted `applicationId`. Slices 6.1 → 6.7 flowed it through the process stream untouched. **Slice 6.8 flows it out across the BC boundary as `driverProfileId` on the `onboarding.driver-approved` publication.** Driver Profile workshop (pending) inherits the UUIDv7 as DP's stream key. **Onboarding is the fourth participant in CritterCab's shared-canonical-ID chain** (Dispatch → Trips → Onboarding → Driver Profile). ADR-013's pattern confirmed across a different lifecycle shape (PM via Handlers + cross-BC handoff at terminal).

#### Events

| Event | Key fields |
|---|---|
| `ApplicationApproved` | `applicationId`, `approvedAt`. Lean. |
| `ApplicantNotifiedOfApproval` | `applicationId`, `subjectId`, `occurredAt`. |
| `DriverActivationPublished` | `applicationId`, `driverProfileId` (== `applicationId`), `subjectId`, `topic`, `occurredAt`. |

#### ADR-015 second instance — softer numeric budget

Applicant-dashboard transition from "application pending" to "ready to drive":

| Question | Decision |
|---|---|
| Projection lifecycle | **Inline.** Same commit as event append. |
| Timing-budget target | **p95 < 2s.** Softer than ADR-015's p95 < 200ms because applicant is not actively interacting at this moment. |
| Observability projection | `OnboardingApprovalLatencyMetrics` — defer but pin. Parallel to W002 §6.1's `MatchingLatencyMetrics`. |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Continue handler (gate-observer) + atomic quadruple-emit (three local events + outbound ASB publication). |
| Three "approved" moments | Three distinct events per W003 V4. W001 §5.8/§5.9 + W002 §6.9 distinct-events-for-distinct-semantics pattern applied fourth time. |
| Outbound topic | `onboarding.driver-approved` per W003 V1 / OQ-1 resolution. |
| ADR-013 fourth-participant materialization | `driverProfileId` carried on event + publication; equals `applicationId`. Loop closes here. |
| W003 B3 / OQ-12 resolution | Approval-push lifecycle-start timing. Onboarding publishes; DP reacts. |
| `ApplicantNotifiedOfApproval` cross-BC publication | **Named-but-deferred.** Onboarding records fact-on-stream; cross-BC delivery via separate topic `onboarding.applicant-approval-notification-required` (if Notifications BC consumes) is a future commitment. W004 closes without locking this third onboarding outbound topic. |
| ADR-015 second instance | p95 < 2s soft budget; `OnboardingApprovalLatencyMetrics` projection deferred-but-pinned. |
| Atomic quadruple-emit | Mirrors growing-N-emit pattern. |
| 2N guard discipline | Both guards present. Guard #2: `state.Status != Approved`. |

#### Cross-references and ripples

- **Backward:** Any Slice 6.2-6.6 event that completes the third approval-gate triggers cascade.
- **Forward (Slice 6.9):** This slice's outbound DP-publication is *present*; Slice 6.9 commits the *absence* on non-approval terminals to close the asymmetry per W003 §5.2 B2.
- **Forward-constraints on Driver Profile workshop:** (a) Consume `onboarding.driver-approved`; (b) Author Protobuf `DriverApproved` contract; (c) Approval-push lifecycle-start timing.
- **W003 V1, V4, B3, OQ-1, OQ-12** all resolve here.
- **ADR-013:** Fourth-participant chain closes.
- **ADR-014:** `onboarding.driver-approved` topic naming.
- **ADR-015:** Second instance with softer numeric budget.
- **PM-via-Handlers §11 candidate:** Approval cascade as the canonical multi-event-emit-when-prerequisites-satisfied example. §3.12 friction-point #2 (distributed completion logic) materializes; `state.AllApprovalGatesSatisfied` helper-method refactor is a candidate.

### 6.9 Slice 9 — Application-rejected/abandoned/withdrawn terminals: absence of cross-BC publications (locking W003 §5.2 B2 + OQ-10a/b/c/e)

**Pattern:** Terminal-event commitment (locks an *absence* of cross-BC publication).
**Lane:** Onboarding.
**Trigger:** Any of the three non-approval terminal events (`ApplicationRejected`, `ApplicationAbandoned`, `ApplicationWithdrawn`).

#### What this slice commits

W003 §5.2 finding B2 surfaced the asymmetry: cross-BC publication exists for the approval terminal but NOT for the rejection terminal. W004 explicitly commits the absence rather than letting it surface implicitly — closing the asymmetry W003 named.

#### Disposition for each non-approval terminal

| Terminal | Outbound publication? | Reason | OQ resolution |
|---|---|---|---|
| `ApplicationRejected` (FCRA-final-adverse path) | **No DP publication.** | Application never became a driver. FCRA notification delivery already handled via Slice 6.7's separate publications. T&S consumes via projection pull (OQ-10e). | OQ-10a: no publication. |
| `ApplicationAbandoned` (Bruun no-activity timeout) | **No DP publication.** | Parallel reasoning. No FCRA; no T&S push; Operations metrics via `OnboardingFunnelView`. | OQ-10b: no publication. |
| `ApplicationWithdrawn` (applicant-initiated) | **No DP publication.** | Parallel reasoning. Operations metrics via `OnboardingFunnelView`. | OQ-10c: no publication. |

#### T&S consumption pattern — pull-via-projection (OQ-10e resolution)

T&S's eventual workshop consumes Onboarding's terminal facts by **reading projections, not by receiving push publications**:

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `OnboardingTerminalFactsView` | T&S BC (eventual workshop) | Per-`applicationId`: `{ applicationId, subjectId, terminalKind: Rejected\|Abandoned\|Withdrawn, terminalCause?, terminatedAt }` | All three non-approval terminal events | **Slice 6.9 named; populated from terminal events.** Multi-stream projection; T&S consumes via Marten query / async daemon shard. |

**Forward-constraint on T&S BC's eventual workshop:** Pull-via-projection pattern. T&S subscribes to `OnboardingTerminalFactsView`; no push publications.

#### Push vs. pull modeling-pattern decision

**Push for cross-BC workflow continuation; pull for cross-BC information flow.** The PM-via-Handlers approach makes pull-via-projection more natural: process stream is canonical; projections are derived; consumers read derivations. Push publications make sense for triggering workflow continuation (DP activation, FCRA delivery); they don't make sense for informational flow (T&S learning terminal facts for pattern detection). Slice 6.9 locks this distinction at the workshop level; future cross-BC consumer relationships re-apply.

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| `ApplicationRejected` outbound publication | **None.** Per W003 §5.2 B2 + OQ-10a. |
| `ApplicationAbandoned` outbound publication | **None.** Per OQ-10b. |
| `ApplicationWithdrawn` outbound publication | **None.** Per OQ-10c. |
| T&S consumption pattern | **Pull via projection** (`OnboardingTerminalFactsView`). Per OQ-10e. Forward-constraint on T&S workshop. |
| Operations consumption | Via `OnboardingFunnelView` async projection. No new publication required. |
| Push vs. pull modeling pattern | **Push for workflow continuation; pull for information flow.** New CritterCab modeling pattern decision. |

#### Cross-references and ripples

- **Backward (Slice 6.7b):** `ApplicationRejected` emitted from FCRA path.
- **Backward (§3.7's abandonment timeout):** `ApplicationAbandoned` emitted from let-state-decide handler.
- **Backward (modeled-not-walked `ApplicantWithdrew` flow):** `ApplicationWithdrawn` emitted from applicant-withdrew Command (Command `WithdrawApplication` + `[AggregateHandler]` continuation + 2N guards + emits `ApplicantWithdrew` and cascaded `ApplicationWithdrawn` terminal).
- **Forward-constraint on T&S BC workshop:** Pull-via-projection consumption.
- **W003 §5.2 B2 + OQ-10a/b/c/d/e all resolved.**
- **New modeling-pattern decision:** Push vs. pull discipline. Candidate sub-discipline; lives in the multi-vendor ACL / PM-via-Handlers ADR pair rather than a freestanding ADR.

### 6.10 Slice 10 — OnboardingPolicyConfigured (configuration-as-events; ADR-011 third-BC adoption)

**Pattern:** Command on singleton aggregate stream + configuration-as-events.
**Lane:** Onboarding.
**Trigger:** Operator updates onboarding policy via admin UI (Operations BC tooling; gRPC unary into Onboarding's policy endpoint).

#### ADR-011 third-BC adoption — near-mechanical reuse of W001 §5.11 / W002 §6.11

```csharp
public record OnboardingPolicy(
    TimeSpan AbandonmentWindow,                  // Slice 6.1 — abandonment timeout
    TimeSpan FcraDisputeWindow,                  // Slice 6.7 — FCRA window length
    TimeSpan VendorNoResponseWindow,             // Slice 6.5 — BG-check vendor no-response timeout
    TimeSpan AdjudicatorClaimWindow,             // Slice 6.6 — adjudicator claim expiry
    TimeSpan DocumentVendorTimeout,              // Slice 6.4 — document-verification vendor request timeout
    int MaxDocumentRejectionAttempts,            // Slice 6.4b parking-lot — retry budget (deferred)
    string CurrentFcraConsentVersion             // Slice 6.3 — current consent text version
);
```

#### Migration-time seed event per ADR-011 bootstrap strategy

Seed defaults (proposed):
- `AbandonmentWindow`: 30 days
- `FcraDisputeWindow`: 5 business days (regulatorily-canonical for U.S. FCRA)
- `VendorNoResponseWindow`: 7 days (Checkr SLA-aligned per W003 Phase 0 note §4.1)
- `AdjudicatorClaimWindow`: 24 hours
- `DocumentVendorTimeout`: 30 seconds
- `MaxDocumentRejectionAttempts`: 5 (parameter named; enforcement deferred per Slice 6.4b)
- `CurrentFcraConsentVersion`: `v1.0.0`

Defaults committed alongside migration; operator-updates via subsequent `OnboardingPolicyConfigured` events.

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Singleton aggregate stream + configuration-as-events. Full-replacement command/event semantics per ADR-011 + W001 §5.11 / W002 §6.11 precedent. |
| Parameters | Seven parameters as listed above. |
| Migration-time seed | Per ADR-011 Option A. Idempotent guard. Seed defaults committed alongside migration. |
| `MaxDocumentRejectionAttempts` enforcement | Parameter named; enforcement deferred per Slice 6.4b parking-lot. |

#### Cross-references and ripples

- **Consumed by:** Slice 6.1 (`AbandonmentWindow`), Slice 6.3 (`CurrentFcraConsentVersion`), Slice 6.4 (`DocumentVendorTimeout`), Slice 6.4b (`MaxDocumentRejectionAttempts` deferred), Slice 6.5 (`VendorNoResponseWindow`), Slice 6.6 (`AdjudicatorClaimWindow`), Slice 6.7 (`FcraDisputeWindow`).
- **ADR-011 third-BC adoption.** Pattern confirmed across three BCs (Dispatch, Trips, Onboarding).
- **No new forward-constraints generated.**

---

## 10. Candidate Protobuf Contract Surface

Names only, per ADR-009 and W001 §12.6 #4. Proto-file authorship is a downstream task captured in §11 follow-ups.

**Inbound (forward-constraint on Identity workshop):**

| Contract | Direction | Topic / RPC | Notes |
|---|---|---|---|
| `DriverRegistered` | Identity → Onboarding | ASB topic `identity.driver-registered`, session-ordered by `subjectId` | Forward-constraint on Identity workshop (Slice 6.1). To be authored at `/protos/crittercab/identity/v1/driver_registered.proto`. At minimum carries `subjectId` + `registeredAt`. |

**Outbound (new, deferred authorship):**

| Contract | Direction | Topic | Notes |
|---|---|---|---|
| `DriverApproved` | Onboarding → Driver Profile | ASB topic `onboarding.driver-approved`, session-ordered by `driverProfileId` (== `applicationId`) | Slice 6.8. Payload: `{ driverProfileId, subjectId, subjectVocabularyTransition: "applicant → driver", approvedAt }`. ADR-013 fourth-participant chain closure. |
| `AdverseActionNoticeRequired` | Onboarding → Notifications BC (pending) | ASB topic `onboarding.adverse-action-notice-required`, session-ordered by `applicationId` | Slices 6.7 + 6.7b. Two phases per publication: `PreAdverse` (first notice) + `FinalAdverse` (post-window). Payload: `{ applicationId, subjectId, phase, reportSnapshotHandle, windowExpiresAt? }`. Forward-constraint on Notifications workshop. |
| `ApplicantApprovalNotificationRequired` *(named-but-deferred)* | Onboarding → Notifications BC (pending) | ASB topic `onboarding.applicant-approval-notification-required`, session-ordered by `applicationId` | Slice 6.8 — named-but-deferred. Surfaces if Notifications BC consumes the third onboarding outbound topic for approval-notification delivery. W004 closes without locking this commitment. |

**Translation-out (gRPC unary, not events):**

| Contract | Direction | RPC | Notes |
|---|---|---|---|
| `VerifyDocument` | Onboarding → document-verification vendor | gRPC unary; vendor-side proto contract | Slices 6.4 + 6.4b. External vendor's contract; CritterCab does not author. |
| `SubmitBackgroundCheck` | Onboarding → background-check vendor | gRPC unary; vendor-side proto contract | Slice 6.5. External vendor's contract; CritterCab does not author. |

All outbound topics follow `<source-bc>.<event-name-kebab>` convention per ADR-014. Files will live under `/protos/crittercab/onboarding/v1/` once authored. Authorship sessions per PR #4 precedent; captured in §11 follow-ups.

---

## X. DS findings handled (the upstream input — W003 §5.1 + §5.2 + §5.3 + §5.1 cross-cutting pattern)

Per W002 §14.6 #2 + W004's new convention. Documents disposition (honored / overridden / partially-honored / deferred) for each of W003's findings. Becomes the convention for future DS-fed EM workshops.

### X.1 BC findings (W003 §5.2 — 7 findings, B1–B7)

| Finding | W004 disposition | Where landed |
|---|---|---|
| **B1: Identity → Onboarding handoff structurally consistent** | **Honored.** | Slice 6.1 inbound contract; forward-constraint on Identity workshop. |
| **B2: Onboarding → DP handoff gated by approval terminal, not BC** | **Honored explicitly.** Approval-terminal-gating realized at Slice 6.8 outbound; non-approval terminals lock the absence at Slice 6.9. The asymmetry W003 named is closed. | Slices 6.8 + 6.9. |
| **B3: Driver Profile lifecycle start unresolved** | **Resolved — approval-push.** Onboarding publishes; DP reacts. | Slice 6.8; W003 OQ-12 also resolves here. |
| **B4: Vendor boundaries are first-class actors** | **Honored.** Two realized translation slices (6.4 + 6.4b for document-verification vendor; 6.5 for background-check vendor). OIDC inherited via ADR-006. Three vendor edges total. | Slices 6.4 / 6.4b / 6.5; §11 multi-vendor ACL ADR candidate. |
| **B5: Onboarding adjudicator is first manual-human actor** | **Honored.** Adjudicator placed inside Onboarding BC; events live in Onboarding, tooling in Operations (forward-constraint). | Slice 6.6. |
| **B6: No T&S lasso emerged** | **Out of scope; vision-doc-escalated.** OQ-14 lives at vision-doc level. | Out of scope per §2.3; OQ-14 disposition below. |
| **B7: Vehicle as sub-domain question** | **Deferred per scope decision A.** Vehicle treated as a document type for W004. Parking-lot if vehicle-specific complexity surfaces. | Out of scope per §2.3. |

### X.2 Vocabulary items (W003 §5.1 — 8 items, V1–V8)

| # | W003 recommendation | W004 disposition |
|---|---|---|
| **V1** | `onboarding.driver-approved` vs. `onboarding.application-approved` | **`onboarding.driver-approved` chosen.** Per OQ-1 lean. Slice 6.8. |
| **V2** | "Documents section" naming | **Confirmed.** UL §4 entry. |
| **V3** | "Adjudication case" naming | **Confirmed as correlation ID on process stream events.** Per §3.4 / §3.8. |
| **V4** | Three "approved" moments need distinct names | **Encoded as three distinct events.** `ApplicationApproved` + `ApplicantNotifiedOfApproval` + `DriverActivationPublished`. Slice 6.8. |
| **V5** | "Case" homonym (vendor case vs. adjudication case) | **Encoded as `BackgroundCheckCase*` and `AdjudicationCase*` event prefixes.** Slices 6.5 + 6.6. |
| **V6** | "Under-review" → "under-verification" | **Renamed.** Per `DocumentState.UnderVerification` enum + per-document state machine. Slices 6.4 + 6.4b. |
| **V7** | "Rejected" — per-document vs. application-terminal | **Encoded as `DocumentRejected` vs. `ApplicationRejected`.** Slices 6.4b + 6.7b. |
| **V8** | Vendor reason-code normalization enum | **Realized as `DocumentRejectionReason` enum at ACL boundary.** Slice 6.4. (BG-check vendor's status enum carries as-is per V5/V6 disambiguation.) |

### X.3 Open questions (W003 §5.3 — 14 W004-scoped + 5 deferred + OQ-14 vision-doc-escalated)

| OQ | W003 question | W004 disposition |
|---|---|---|
| **OQ-1** | Topic name for approval event | `onboarding.driver-approved` (Slice 6.8 / V1). |
| **OQ-2** | Document-state storage model | **Events on the Application's process stream** with `documentId` correlation. No separate Document aggregate. Per §3.4 + Slice 6.4. |
| **OQ-3** | Document history persistence | **Event-stream ordering filtered by `documentId`.** History IS the event log; no separate projection required. Optional `DocumentTimeline` projection deferred-but-pinned. Slice 6.4b. |
| **OQ-4** | Adjudication-case storage | **Correlation field + events on process stream.** No separate AdjudicationCase aggregate. Multi-stream `AdjudicatorQueueView*` projection. Per §3.4 + Slice 6.6. |
| **OQ-5** | Pre-adverse vs. final-adverse events | **Two distinct events.** `PreAdverseActionNoticeIssued` + `FinalAdverseActionNoticeIssued`. Slices 6.7 + 6.7b. |
| **OQ-6** | Window-expiry firing the final adverse-action | **PM-self-scheduled `OutgoingMessages.Delay` + let-state-decide,** NOT Bruun-todo-list-mechanism. Bruun pattern applies for *projection-side* (`AdverseActionDisputeWindow*` ops visibility) but is no longer the mechanism. Slice 6.7b. |
| **OQ-7** | Three "approved" moments distinct events | **Yes** — three distinct events (V4 resolved). Slice 6.8. |
| **OQ-8** | Document state "under-verification" rename | **Yes** (V6 resolved). Per `DocumentState.UnderVerification` enum. Slice 6.4. |
| **OQ-9** | Vendor reason-code normalization enum | **`DocumentRejectionReason` canonical CritterCab enum** at ACL boundary (V8 resolved). Slice 6.4. |
| **OQ-10a** | `ApplicationRejected` outbound publication | **None.** Slice 6.9 locks absence. |
| **OQ-10b** | `ApplicationAbandoned` outbound publication | **None.** Slice 6.9. |
| **OQ-10c** | `ApplicationWithdrawn` outbound publication | **None.** Slice 6.9. |
| **OQ-10d** | FCRA notice events outbound publication for delivery | **Yes — `onboarding.adverse-action-notice-required` ASB publication** for Notifications BC's eventual workshop to consume. Slices 6.7 + 6.7b. Forward-constraint on Notifications. |
| **OQ-10e** | T&S future consumption pattern | **Pull via projection, not push via publication.** `OnboardingTerminalFactsView`. Slice 6.9. Forward-constraint on T&S. |
| **OQ-11** | Re-application policy | **Out of scope** (scope decision A). Parking-lot if surfaces. |
| **OQ-12** | Driver Profile creation timing | **Approval-push.** B3 / OQ-12 resolved at Slice 6.8. |
| **OQ-13** | Vehicle as sub-domain | **Out of scope** (B7 / scope decision A). |
| **OQ-14** | Suspension / reinstatement / deactivation | **Out of scope** (vision-doc-escalated). |

### X.4 Cross-cutting pattern (W003 §5.1 — multi-vendor ACL aggregator)

**Absorbed inside the Process Manager pattern** per §3.8. Three realized vendor edges (OIDC via Identity per ADR-006 — inherited; document-verification vendor — Slices 6.4 + 6.4b; background-check vendor — Slice 6.5). Plus a fourth translation surface that fits the pattern at the boundary even though the actor is human (adjudicator via UI — Slice 6.6).

Pattern surfaces in §11 as a paired ADR candidate with the Process Manager ADR — together they delineate "how Process Managers integrate with multi-vendor external boundaries in CritterCab." Trigger to author both ADRs: W004 ships with the Process Manager shape; first Onboarding implementation slice exercises it.

---

## X+1. Forward-constraints generated (per W002 §14.6 #2 — outbound constraints on un-modeled BCs)

Consolidated outbound forward-constraints W004 generates on un-modeled BCs' future workshops. Each carries the constraint shape + slice source.

| # | Forward-constraint | Source slice | Target BC workshop |
|---|---|---|---|
| 1 | Identity must publish `identity.driver-registered` over ASB at OIDC-subject-creation terminal, session-keyed by `subjectId`, with at minimum `subjectId` + `registeredAt` payload. | Slice 6.1 | Identity workshop (pending) |
| 2 | Identity must guarantee at-least-once delivery semantics consistent with Wolverine + ASB defaults. | Slice 6.1 | Identity workshop |
| 3 | Identity must author `DriverRegistered` Protobuf contract at `/protos/crittercab/identity/v1/driver_registered.proto`. | Slice 6.1 | Identity workshop (PR #4 precedent) |
| 4 | Driver Profile must consume `onboarding.driver-approved` and create/activate DriverProfile aggregate at inherited canonical UUIDv7 (`driverProfileId == applicationId`). **Approval-push lifecycle-start timing.** | Slice 6.8 | Driver Profile workshop (pending) |
| 5 | Driver Profile must author `DriverApproved` Protobuf contract at `/protos/crittercab/onboarding/v1/driver_approved.proto`. | Slice 6.8 | Driver Profile workshop |
| 6 | Notifications BC must consume `onboarding.adverse-action-notice-required` (both `PreAdverse` and `FinalAdverse` phases) and dispatch FCRA-compliant notifications via email/SMS vendor (SendGrid/Twilio class). | Slices 6.7 + 6.7b | Notifications BC workshop (pending; not in vision-doc tentative-BC list) |
| 7 | Notifications BC must author `AdverseActionNoticeRequired` Protobuf contract at `/protos/crittercab/onboarding/v1/adverse_action_notice_required.proto`. | Slices 6.7 + 6.7b | Notifications BC workshop |
| 8 | T&S BC must consume Onboarding's non-approval terminal facts via pull-via-projection pattern (`OnboardingTerminalFactsView`), not push publications. | Slice 6.9 | T&S BC workshop (pending; not in vision-doc tentative-BC list) |
| 9 | Operations BC's adjudicator queue UI / claim flow / SLA tracking consumes `AdjudicatorQueueView*` multi-stream projection. | Slice 6.6 | Operations BC workshop (pending) |
| 10 | Operations BC owns canonical-actor identity model for `adjudicatorId` (workforce identity authority). Per ADR-006's parked decision on workforce Entra tenant. | Slice 6.6 | Operations BC workshop |
| 11 | Operations BC consumes Onboarding's vendor no-response escalation signal (BG-check vendor silent past timeout). Specific shape (event vs. ops alert vs. dashboard counter) is Operations' workshop call. | Slice 6.5 | Operations BC workshop |
| 12 | Notifications BC (or Operations BC, depending on workflow ownership) may consume `onboarding.applicant-approval-notification-required` if W004 commits the third onboarding outbound topic. **W004 names-but-defers** this commitment. | Slice 6.8 | Notifications BC workshop (or Operations; deferred) |

**Aggregate count:** 12 forward-constraints generated across 5 target BC workshops (Identity, Driver Profile, Notifications, T&S, Operations). Of these, 4 (#3 + #5 + #7 + [implicit Identity]) are protobuf authorship triggers — candidates for bundled-proto authorship sessions per PR #4 precedent.

**Notifications BC is not currently in CritterCab's vision-doc tentative-BC list.** W004 surfaces it as a workshop-need-trigger via forward-constraint #6. Whether Notifications becomes a permanent BC vs. lives inside Operations is a vision-doc-level open question that may surface as a result of W004; the workshop does not commit to a vision-doc bump on this point.

---

## 11. ADR Candidates Surfaced by This Workshop

Pre-seeded with candidates inheriting from W001 + W002 (re-listed at session close with post-W004 status) plus the two new candidates surfaced during the walk.

**Locked ADRs gaining evidence in W004 (post-workshop status):**

- **ADR-011 (configuration-as-events bootstrap strategy)** — **Third BC adoption.** `OnboardingPolicyConfigured` (Slice 6.10) with seven tunable parameters + migration-time seed defaults. Pattern confirmed across three BCs (Dispatch, Trips, Onboarding).
- **ADR-013 (shared cross-BC identifier)** — **Fourth participant.** `applicationId == driverProfileId` chain closes at Slice 6.8's outbound `onboarding.driver-approved` publication. Confirmed across PM via Handlers in addition to aggregate-cluster patterns. Onboarding's "deterministic-order lifecycle stages crossing BCs" reading from ADR-013's scope is the cleanest realization yet.
- **ADR-014 (ASB topic naming)** — **Third + fourth instances.** `onboarding.driver-approved` (Slice 6.8) is the third Onboarding-outbound topic if `onboarding.adverse-action-notice-required` (Slices 6.7 + 6.7b) counts as the fourth (it's a distinct topic, two-phase payload). Convention reused without modification.
- **ADR-015 (driver-app projection timing budget)** — **Second instance with softer numeric budget.** Onboarding's applicant-dashboard transitions (intake at Slice 6.1 with p95 < 1s; approval at Slice 6.8 with p95 < 2s) both softer than Trips' p95 < 200ms because the applicant is not in a tap-and-render workflow. Two new observability projections deferred-but-pinned (`OnboardingIntakeLatencyMetrics`, `OnboardingApprovalLatencyMetrics`). ADR-015 amendment or supersession when implementation lands and measurements become available.

**Locked ADR explicitly NOT a new evidence point:**

- **ADR-012 (aggregate-per-invariant)** — **NOT a new evidence point.** Onboarding is a Process Manager via Handlers instance, not an aggregate-cluster. ADR-012's evidence base remains at W001 + W002 (two BCs). The honest reframe is documented in §3.11 (sidebar's ADR-evidence framing): forcing Onboarding into ADR-012's vocabulary would have imposed invariant-protection framing on a coordination-dominated shape and lost the framework's first-class timeout primitives. Locked ADR is unchanged; CritterCab's modeling pattern repertoire grows by one (PM via Handlers) without revising ADR-012's claim.

**New ADR candidates surfaced by this workshop (paired):**

1. **NEW candidate #1 — Process Manager via Handlers as CritterCab's third modeling pattern.** Sibling to ADR-012's aggregate-per-invariant. Delineates when to reach for PM via Handlers vs. an aggregate-cluster — primary signal is *coordination-dominance* (parallel external events, temporal automation, multiple distinct terminals including abandonment, weak parent-level invariants). Evidence base after W004: one realized instance (Onboarding). Friction points captured in §3.12 are inputs to the ADR. Trigger to author: first Onboarding implementation slice exercises the pattern.

2. **NEW candidate #2 — Multi-vendor ACL pattern absorbed inside the Process Manager.** Generalizes ADR-006's OIDC-as-ACL stance to a project-wide pattern covering all third-party vendor edges; positions the integration as Translation slices feeding the Process Manager's process stream rather than as freestanding architecture. Evidence base after W004: three realized vendor edges (OIDC inherited per ADR-006, document-verification at 6.4 + 6.4b, background-check at 6.5) plus a fourth translation surface that fits at the boundary (adjudicator via UI at 6.6 — human-actor translation). **Authored as a separate paired ADR** alongside candidate #1 per CritterCab's one-decision-per-ADR convention (grill #4). Trigger to author: same as candidate #1.

**Sub-discipline candidate (lives inside the paired ADRs; not freestanding):**

- **Push vs. pull cross-BC consumption.** *Push for cross-BC workflow continuation; pull for cross-BC information flow.* Lands in Slice 6.9. Sub-discipline of the multi-vendor ACL / PM via Handlers ADR pair; lean: too small for freestanding ADR; lives as a documented discipline in the paired ADRs.

**Aggregate ADR status after W004:**

- 4 locked ADRs gain evidence (ADR-011, ADR-013, ADR-014, ADR-015).
- 1 locked ADR is honestly NOT a new evidence point with reframe documented (ADR-012).
- 2 new paired ADR candidates surface (Process Manager + multi-vendor ACL).
- 1 sub-discipline candidate (push vs. pull) lives inside the paired ADRs.

Trigger to author both new candidates: first Onboarding implementation slice exercises the pattern. Author as a follow-up session per PR #4 precedent + W002 §14.8 → ADR-011/12/13/14/15 bundled-authorship session pattern.

---

## 12. Retrospective

Mirrors W001 §12 / W002 §14's nine-subsection convention. Section numbering reflects this workshop's structure (additional §3 Modeling pattern, §X DS findings handled, §X+1 Forward-constraints generated); the nine-subsection retrospective shape is preserved, with §12.10 added for the two W004-specific methodology questions. The full CritterCab-format session retrospective lives at [`docs/retrospectives/workshops/004-onboarding-event-modeling.md`](../retrospectives/workshops/004-onboarding-event-modeling.md); this §12 is the workshop-scoped modeling retrospective.

### 12.1 Workshop intent vs. outcome

Stated goal at session start: produce a structurally-parallel artifact for Onboarding to W002's Trips artifact, with W003's three-story spine as slice ordering and W003's 14 open questions as the explicit work agenda; pilot Process Manager via Handlers as CritterCab's third modeling pattern; test the DS→EM input mechanism.

**Outcome:** Complete event model for Onboarding covering 10 base slices + 1 sub-slice paired — 21 events, 4 scheduled timeouts (let-state-decide), 3 realized multi-vendor ACL translation edges + 1 human-actor translation surface, 1 cross-stream multi-stream projection (first in CritterCab), 1 configuration-as-events slice, FCRA two-phase notice with scheduled dispute-window. All 14 W003 W004-scoped OQs dispositioned in §X; all 8 V-items + 7 B-findings handled; 12 forward-constraints generated across 5 target BC workshops. 4 locked ADRs gained evidence; ADR-012 honestly NOT a new evidence point (reframe documented); 2 new paired ADR candidates surfaced. **Goal met.**

### 12.2 What worked

- **§3 modeling-pattern sidebar (replacing W002's aggregate-identity sidebar) applied cleanly.** Settling the *pattern* (PM via Handlers) before any state-machine substance shaped every subsequent slice, exactly as W001 §12.6 #2 predicted for aggregate identity. Pattern decision being prompt-locked (grills #2/#4) meant the sidebar committed the design rather than debating it.
- **DS-as-upstream front-loaded all vocabulary work.** Zero mid-walk vocabulary scrambles — contrast W002's three-iteration first-state-vocabulary scramble. (See §12.10 Q1.)
- **Let-state-decide pattern is the cleanest ergonomic win.** Slice 6.7b's FCRA dispute-window timeout is the canonical demonstration: no cancel-the-timer API, state authoritative, handler a pure function.
- **Paired walks (6.4+6.4b, 6.5+6.6, 6.7+6.7b) accelerated cadence without losing rigor.** Each slice kept its own decisions table + GWT + cross-references; the *walk* was paired. W002 §14.6 #3 confirmed portable.
- **Multi-vendor ACL discipline transferred verbatim from 6.4 to 6.5,** shrinking the second vendor-edge slice substantially.
- **Atomic N-emit pattern scaled smoothly.** W001 §5.5 (dual) → W002 §6.6 (triple) → W004 §6.7b/§6.8 (quadruple). Wolverine outbox guarantee held the discipline constant as N grew.
- **Bruun carry-the-value applied a third time** (`windowExpiresAt` at Slice 6.7).
- **The honest ADR-012 reframe.** Resisting the temptation to count Onboarding as a third aggregate-per-invariant data point kept ADR-012's evidence base honest and grew CritterCab's modeling-pattern repertoire by one.

### 12.3 What was hard / friction

- **The session ran long.** Largest workshop artifact in CritterCab to date. Paired-walk cadence helped but the volume is real. Future PM-via-Handlers workshops should budget for similar length.
- **The webhook-routing-requires-additional-inline-projections friction is not in the Wolverine guide.** Slice 6.5's `BackgroundCheckVendorCaseIndex` inline projection (silent dependency #9) is a genuine friction the guide's single-process sample doesn't exercise. (See §12.10 Q2.)
- **The distributed-completion-logic friction (guide §5 #2) materialized as predicted at Slice 6.8,** biting harder with four parallel gates than the guide's three-gate sample suggests.
- **`ApplicantNotifiedOfApproval` cross-BC publication ended named-but-deferred** (Slice 6.8). A small loose end consciously left open.
- **Confounding of the DS-as-upstream finding by prompt-grilling.** Much of W004's smoothness traces to four prompt-authoring grills, not DS-as-upstream alone. The honest Q1 finding disentangles both. (See §12.10 Q1.)

### 12.4 Decisions about how to model (meta-decisions worth carrying forward)

- Slices commit only after explicit sign-off. (Re-confirmed.)
- Each slice closes with a "decisions locked" table. (Re-confirmed.)
- Paired walks for structurally-similar slices. (Re-confirmed from W002.)
- **NEW:** Settling the *modeling pattern* (not just aggregate identity) in the pre-walk sidebar when the BC isn't aggregate-cluster-shaped. The §3 "three framings considered, two rejected" structure is the reusable shape for future pattern-selection sidebars.
- **NEW:** Bruun-pattern-as-projection ≠ Bruun-pattern-as-mechanism.
- **NEW:** Push vs. pull cross-BC consumption discipline (Slice 6.9). Push for workflow continuation; pull for information flow.
- **NEW:** §X "DS findings handled" as the upstream-input-handling section for DS-fed EM workshops.

### 12.5 Patterns established for future workshops

- **Process Manager via Handlers** as CritterCab's third modeling pattern (first in-repo reference implementation).
- **Multi-vendor ACL absorbed inside the PM pattern** — translation slices feeding the process stream.
- **Let-state-decide for scheduled timeouts** — four instances; canonical demonstration at Slice 6.7b.
- **Atomic N-emit for terminals + cross-BC publications** — scaled to quadruple.
- **Cross-stream multi-stream projection** (`AdjudicatorQueueView*`) — first instance in CritterCab.
- **§X "DS findings handled" section** — convention for DS-fed EM workshops.
- **Modeling-pattern-selection sidebar** (three-framings-considered structure).

### 12.6 Adjustments for the next BC workshop

- **Budget for length when the BC is PM-shaped.**
- **When a vendor webhook doesn't carry the process stream's ID, name the routing-index inline projection early** (in the sidebar, not mid-walk).
- **The §3 sidebar's friction-point enumeration (Wolverine guide §5) paid off.** Carry forward, and add the W004-discovered ninth (webhook-routing-inline-projection).
- **W001 §12.6 + W002 §14.6 all adjustments continue to apply.**

### 12.7 Quality signal from the session

User signed off "as drafted" at every gate. Consistent forward-momentum direction. No redirects on the slice walk — consistent with the prompt's heavy grilling having front-loaded the substantive decisions and with well-calibrated leans. The zero-redirect signal is positive but (per W003 retro Q1's caveat) hard to distinguish from lean-confirmation mode without deliberate red-teaming.

Calibration: no new feedback memories warranted. All applicable feedback already in `MEMORY.md`; all reused successfully without reinforcement.

### 12.8 Follow-ups generated

- **2 new paired ADR candidates** (Process Manager via Handlers + multi-vendor ACL). Trigger: first Onboarding implementation slice.
- **Onboarding business-event Protobuf authorship** — `DriverApproved`, `AdverseActionNoticeRequired` (+ named-but-deferred `ApplicantApprovalNotificationRequired`); plus forward-constraint that Identity authors `DriverRegistered`. PR #4 precedent.
- **12 forward-constraints** across 5 target BC workshops — see §X+1.
- **Notifications BC is not in the vision-doc tentative-BC list.** Vision-doc inventory-drift candidate for a future tidy session (parallel to the T&S inventory-drift flag in the context map's §Pending workshops).
- **Parking-lot items:** sensitive-PII vault (6.3); document retry-budget enforcement (6.4b); Suspended-status applicant-action flow (6.5); dispute-filing flow (6.7b); adjudicator workforce-identity authority model (6.6); re-application policy (OQ-11); vehicle-as-sub-domain (OQ-13).
- **DEBT.md follow-ups:** per-vendor reason-code mapping table; blob-storage handle resolution skill.
- **Methodology log entry 005** written (Q1 — DS-as-upstream). Entry 006 (Q2 — PM webhook-routing friction) NOT written; that friction lives in the §11 PM ADR candidate instead.

### 12.9 Workshop status

**Complete (v0.9, 2026-05-27).** Event model for the Onboarding bounded context is ready to serve as input to narrative authoring and implementation prompts. First in-repo Process Manager via Handlers reference implementation; first DS-fed EM workshop.

### 12.10 Methodology questions (Q1 + Q2)

**Q1. Did DS-as-upstream produce materially higher-quality EM artifact than narratives-only would have?**

**Yes, materially — with one honest confound.** Evidence: zero mid-walk vocabulary scrambles (vs. W002's three-iteration first-state scramble); all 7 B-findings encoded with zero fresh debate; all 14 OQs answerable with leans traced to W003 findings; the multi-vendor ACL cross-cutting pattern gave W004 a ready-made architectural framing. The confound: much of W004's smoothness traces to four prompt-authoring grills, not DS-as-upstream alone. Disentangled: DS produced a richer vocabulary-and-findings surface than narratives would have (the W003-retro-Q2 vocabulary-divergence findings were invisible from vision-doc reading), AND the grilling amplified it. Both contributed; the DS contribution (vocabulary front-loading) is real and isolable. **Implication: DS earns a place as a default pre-EM step for vocabulary-rich BCs. Methodology log entry 005 written.**

**Q2. Did PM-via-Handlers fit Onboarding's shape as predicted, or surface friction the guide doesn't capture?**

**Fit as predicted, with one W004-specific friction.** Let-state-decide felt materially cleaner than the rejected Bruun-as-mechanism framing; `ApplicationState` richer than the `OrderFulfillmentState` sample but the richness is honest domain complexity; distributed-completion-logic + start-handler-asymmetry frictions materialized as the guide predicted. **The friction the guide doesn't capture: when an external vendor's webhook doesn't carry the process stream's ID, the PM pattern needs an additional inline projection to route the webhook** (`BackgroundCheckVendorCaseIndex`, silent dependency #9). The guide's single-process OrderFulfillment sample doesn't exercise external-vendor-webhook routing. **This friction lives in the §11 Process Manager via Handlers ADR candidate** (pattern-specific, so future PM workshops find it there) rather than in a methodology log entry — per the methodology log's own "when to write an entry" criteria favoring cross-cutting *process* observations.

---

## Document History

- **v0.1** (2026-05-27): Header + §1 Session Log + §2 Scope Statement (Option A locked) + §3 Modeling pattern and process state sidebar (Process Manager via Handlers locked; `ApplicationState` design; event catalogue scaffolded; four scheduled timeouts via `OutgoingMessages.Delay` with let-state-decide; multi-vendor ACL integration; cross-BC identifier per ADR-013; ADR-evidence framing including ADR-012 honest-reframe; eight friction-point flags from Wolverine guide §5) + §4 Ubiquitous Language bootstrap from W003 §5.1 + §3 sidebar + §5 Event List scaffold by category + §6 Slice Walk proposed ordering (10 base slices + 1 sub-slice paired). Pre-walk vocabulary scan against W001 + W002 complete with zero blocking collisions. Slice walk pending.
- **v0.2** (2026-05-27): Slice 6.1 walked and committed — `ApplicationOpened` Translation-in from Identity, with start-handler asymmetry (plain static, NOT `[AggregateHandler]`; returns `(IStartStream, OutgoingMessages)` via `MartenOps.StartStream<ApplicationState>(...)`), canonical UUIDv7 minting per ADR-013 (Onboarding fourth participant in shared-canonical-ID chain), idempotency contract via `ApplicationsBySubject` projection + `ExistingStreamIdCollisionException` catch as race-condition fallback, `ApplicationAbandonmentTimeout` scheduled at start via `OutgoingMessages.Delay`, Klefter decision-event recording, three GWT sketches (happy / redelivery / race-condition / malformed), six projection candidates with five named-and-status, two forward-constraints on Identity workshop generated, ADR-013 + ADR-014 + ADR-015 evidence-point materialization, PM-via-Handlers + multi-vendor ACL first-instance evidence. Event List updated with event #1. Slices 6.2–6.10 pending.
- **v0.3** (2026-05-27): Slices 6.2 + 6.3 walked and committed as paired walk (light slices per W001 §12.6 #3 cadence calibration) — `PersonalInfoSubmitted` and `BackgroundCheckConsentGiven` Command-pattern slices with continue-handler shape exercising the 2N guard discipline (terminal-state + step-level idempotency) for the first time in the workshop. Both events bump `ApplicationState.LastSignificantActivityAt` to re-arm the abandonment-timeout's let-state-decide check. Validation lives at HTTP boundary per `feedback_validation_at_http_boundary`; handler rejects only illegal state transitions. Slice 6.3 surfaces a sensitive-PII handling refinement (event carries `ssnHash`, not raw SSN; raw SSN in separate encrypted vault) and generates a new parking-lot item for the vault implementation + ADR/skill. Event List updated with events #2 and #3. Slices 6.4–6.10 pending.
- **v0.4** (2026-05-27): Slices 6.4 + 6.4b walked and committed as paired walk (heavy slices per W001 §12.6 #3 cadence calibration) — `DocumentUploaded` + `DocumentVerified` (happy) + `DocumentRejected` + `DocumentReuploaded` (recovery). First *realized* multi-vendor ACL edge (OIDC inherited per ADR-006 was the first; this is the first edge Onboarding *itself* translates). Canonical CritterCab `DocumentRejectionReason` enum committed at the ACL boundary (W003 V8 normalization realized): `PoorImageQuality`, `DocumentTypeMismatch`, `DocumentExpired`, `DocumentDamagedOrAltered`, `InformationMismatch`, `UnsupportedDocumentFormat`, `Other`. Per-document state machine realized via `ApplicationState.Documents` dictionary: `UnderVerification → Verified | Rejected → UnderVerification (on re-upload) → Verified`. Document identity preserved through rejection cycle per W003 Story 2 Q2a.2 and W003 V7 (`DocumentRejected` ≠ `ApplicationRejected`). Event-stream ordering = document history (W003 OQ-3 resolved). `DocumentVerificationAutomation` is event-driven gear sticky; vendor call is gRPC unary (Translation-out is routing-only per Klefter Post 3, no local event); vendor response is decision-translation (local event emitted). `vendorCaseId` carried opaque per ACL discipline. Two DEBT.md follow-ups generated: per-vendor mapping table (vendor reason → CritterCab enum), blob-storage handle resolution skill. Event List updated with events #4-#7. Slices 6.5-6.10 pending.
- **v0.5** (2026-05-27): Slices 6.5 + 6.6 walked and committed as paired walk (heavy slices per W001 §12.6 #3 cadence calibration) — `BackgroundCheckRequested` + `BackgroundCheckCaseOpened` + `BackgroundCheckStatusReceived` (Clear path, Slice 6.5) + `AdjudicationCaseQueued` + `AdjudicationCaseClaimed` + `AdjudicationCaseDecided` (Consider→adjudication path, Slice 6.6). Slice 6.5: Second *realized* multi-vendor ACL edge (BG-check vendor); structural parallelism with 6.4 commits the discipline as a transferable pattern. `BackgroundCheckVendorCaseIndex` inline projection added (silent dependency #9 in §3.12 friction-point list). Approval gate-trigger cascade via atomic dual-emit (`DocumentVerified` + `BackgroundCheckRequested`) — third application of W001 §5.5 pattern across CritterCab. Vendor no-response timeout scheduled at `BackgroundCheckCaseOpened` per §3.7 let-state-decide. `Suspended` status surfaces as parking-lot item (out-of-walk for W004). Slice 6.6: First manual-human actor handling in any CritterCab workshop (Onboarding adjudicator per W003 §5.2 B5). First cross-stream multi-stream projection in CritterCab (`AdjudicatorQueueView*` — Bruun todo-list aggregating across all Application streams). `Clear` adjudicator-override cascade emits fresh `BackgroundCheckStatusReceived(Clear)` alongside `AdjudicationCaseDecided`. Adjudicator claim-expiry timeout scheduled at `AdjudicationCaseClaimed` per §3.7. Two new forward-constraints on Operations BC's eventual workshop (adjudicator queue UI/claim-flow/SLA; adjudicator workforce-identity authority model). Multi-vendor ACL pattern §11 candidate gains second + third realized-edge evidence (BG-check vendor + adjudicator-via-UI human-actor translation). Event List updated with events #8-#13. Slices 6.7-6.10 pending.
- **v0.6** (2026-05-27): Slices 6.7 + 6.7b walked and committed as paired walk (heavy slices per W001 §12.6 #3 cadence calibration) — `PreAdverseActionNoticeIssued` + outbound ASB publication (Slice 6.7) + `DisputeWindowExpired` + `FinalAdverseActionNoticeIssued` + `ApplicationRejected` + second outbound publication (Slice 6.7b, canonical let-state-decide demonstration). Slice 6.7: Atomic triple-emit (event + scheduled self-message + outbound ASB publication). New ASB topic `onboarding.adverse-action-notice-required` per ADR-014; consumer is Notifications BC's eventual workshop (forward-constraint generated per W003 OQ-10d / grill #3). Bruun carry-the-value applied a third time across CritterCab (`windowExpiresAt` survives mid-flight policy changes). Slice 6.7b: Canonical PM-via-Handlers let-state-decide demonstration — no cancel-the-timer API; state is authoritative; three resolution branches (terminal / dispute-state / window-elapsed-without-dispute). `ApplicationRejectionCause` BC-owned enum committed (`FcraFinalAdverseAction`, `DocumentRejectionTerminal`, `AdjudicatorRejectNonFcra`, `Other`; W004 materializes only the first). Atomic quadruple-emit (`DisputeWindowExpired` + `FinalAdverseActionNoticeIssued` + `ApplicationRejected` + outbound publication). **Bruun-pattern-as-projection ≠ Bruun-pattern-as-mechanism** distinction locked: `AdverseActionDisputeWindow*` is for ops visibility, NOT the timeout mechanism (which is the PM self-scheduled message + let-state-decide). Two new forward-constraints on Notifications BC's eventual workshop (consume `onboarding.adverse-action-notice-required`; author Protobuf `AdverseActionNoticeRequired` contract). Dispute-filing flow modeled-not-walked (parking-lot item). Event List updated with events #14-#17. Slices 6.8-6.10 pending.
- **v0.7** (2026-05-27): Slices 6.8, 6.9, 6.10 walked and committed (final three slices of the walk). Slice 6.8 (heavy): Approval cascade + Translation-out to Driver Profile. Three "approved" moments as three distinct events per W003 V4 (`ApplicationApproved` + `ApplicantNotifiedOfApproval` + `DriverActivationPublished`); W001 §5.8/§5.9 + W002 §6.9 distinct-events-for-distinct-semantics pattern applied fourth time. Atomic quadruple-emit (three local events + outbound `onboarding.driver-approved` publication on ASB). **ADR-013 fourth-participant chain closes here**: `driverProfileId == applicationId` carried on outbound publication; Driver Profile inherits the canonical UUIDv7. W003 V1, V4, B3, OQ-1, OQ-12 all resolve. ADR-015 second instance with softer p95 < 2s budget; `OnboardingApprovalLatencyMetrics` projection deferred-but-pinned. Three forward-constraints on Driver Profile workshop. `ApplicantNotifiedOfApproval` cross-BC publication named-but-deferred (third onboarding outbound topic could land if Notifications consumes). Slice 6.9 (light): Application-rejected/abandoned/withdrawn terminals lock the *absence* of cross-BC publications per W003 §5.2 B2 + OQ-10a/b/c/e. T&S forward-constraint generated via pull-via-projection (`OnboardingTerminalFactsView`). New CritterCab modeling-pattern decision committed: **push for workflow continuation, pull for information flow**. Slice 6.10 (light): `OnboardingPolicyConfigured` ADR-011 third-BC adoption near-mechanically; seven tunable parameters; migration-time seed defaults committed. Event List updated with events #18-#21. **Slice walk complete: 10 base slices + 1 sub-slice paired; 21 events total.** Session close work pending: §10 Protobuf surface, §X DS findings handled, §X+1 Forward-constraints generated, §11 ADR Candidates, §12 Retrospective, README updates.
- **v0.8** (2026-05-27): §10 Candidate Protobuf Contract Surface (5 contracts named + 2 vendor gRPC RPCs; deferred authorship per ADR-009 + W001 §12.6 #4) + §X DS findings handled (W003's 7 B-findings + 8 V-items + 14 OQs + 1 cross-cutting pattern all dispositioned — the new convention for DS-fed EM workshops, parallel to W002 §13's forward-constraints-handled for narrative-fed workshops) + §X+1 Forward-constraints generated (12 constraints across 5 target BC workshops: Identity, Driver Profile, Notifications, T&S, Operations) + §11 ADR Candidates (4 locked ADRs gain evidence: 011/013/014/015; ADR-012 honest non-evidence-point reframe; 2 new paired candidates: Process Manager via Handlers + multi-vendor ACL absorbed inside it; 1 sub-discipline candidate: push-vs-pull). Notifications BC inventory-drift flagged (not in vision-doc tentative-BC list). §12 Retrospective pending.
- **v0.9** (2026-05-27): §12 Retrospective committed (nine-subsection shape per W001 §12 / W002 §14 + §12.10 methodology questions). Q1 (DS-as-upstream): **yes, materially higher-quality EM than narratives-only would have produced, with the honest confound that four prompt-authoring grills amplified the DS contribution** — methodology log entry 005 written. Q2 (PM-via-Handlers fit): **fit as predicted with one W004-specific friction the Wolverine guide doesn't capture** (vendor-webhook-routing requires additional inline projection when the external system doesn't carry the process stream's ID) — friction lives in the §11 PM ADR candidate rather than a methodology log entry, per the log's own entry criteria. **Workshop status: complete.** All 14 W003 W004-scoped OQs dispositioned; §3 sidebar PM-via-Handlers reframe with ADR-012 + ADR-013 evidence framing called out; §11 re-lists locked ADRs with post-workshop status; §12 retrospective complete with DS-as-upstream-input efficacy subsection. Definition of done met. Remaining session-close deliverables (separate retrospective file, methodology log entry 005, README updates) land in further commits on this branch per the path-2 combined-PR decision.
