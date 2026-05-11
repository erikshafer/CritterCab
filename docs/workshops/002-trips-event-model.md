# Workshop 002 — Trips Event Model

**Status:** Complete (v0.13, 2026-05-10). All 12 slices walked and committed; retrospective closes the session. v0.12 closed §11 items #1 and #2 in-PR (Workshop 001 §5.12 v0.3 amendment). v0.13 §12 cross-reference update — five ADR candidates lifted to authored ADRs ([ADR-011](../decisions/011-configuration-as-events-bootstrap.md) / [ADR-012](../decisions/012-aggregate-per-invariant.md) / [ADR-013](../decisions/013-shared-cross-bc-identifier.md) / [ADR-014](../decisions/014-asb-topic-naming-convention.md) / [ADR-015](../decisions/015-driver-app-projection-timing-budget.md)).
**Started:** 2026-05-09.
**Facilitator / modeler:** Erik Shafer (solo).
**AI collaborator:** Claude (Opus 4.7), rotating through Facilitator, Developer, Skeptic, and Domain-Expert personas per `docs/research/event-modeling-workshop-guide.md` Lesson 8.
**Methodology reference:** `docs/research/event-modeling-workshop-guide.md`.
**Adjunct patterns:** `docs/research/agents-in-event-models.md` (Klefter translation-decision events; Bruun temporal-automation slice pattern).
**Structural constraints honored:** `docs/rules/structural-constraints.md` (ADR-002, ADR-005, ADR-006, ADR-009).
**Workshop 001 inheritance:** All conventions from [`001-dispatch-event-model.md`](./001-dispatch-event-model.md) carry forward without re-litigation. This workshop is the canonical second data point for ADR-candidates #6 (aggregate-per-invariant) and #7 (shared cross-BC identifier); ADR-candidates #5 (config-as-events bootstrap) and #8 (ASB topic naming) are conditional fires whose status is settled at slice walk.
**Forward-constraints honored from narratives 001 + 002:** see §13.

---

## 1. Session Log

| Session | Date | Duration | Steps covered | Notes |
|---|---|---|---|---|
| 1 | 2026-05-09 | Full session (single sitting) | Scope confirmation (Option B), aggregate-identity sidebar (§3), Ubiquitous Language bootstrap (§4), all 12 slices walked, cross-references populated, retrospective | Solo facilitation with Claude as collaborator. Workshop convened per ADR-004's design-return cadence rule after PR #10 closed Workshop 001's housekeeping queue. Workshop 001's §12.8 entry "Trips BC workshop pending" closes with this artifact. First-state vocabulary (`Matched`) grounded in real ride-sharing API conventions (Uber, Lyft, system-design literature) per mid-sidebar research turn. Two paired walks executed (slices 6.7+6.8 cancellations; slices 6.9+6.10 translation-out). One forward-only slice amendment (slice 6.12 amends slice 6.1 via cross-reference). One deliberate override of a Workshop 001 preference (slice 6.9 vs. §5.12). Methodology log entry 004 written this session. |

---

## 2. Scope Statement

### 2.1 In scope

The Trips bounded context's **post-acceptance ride lifecycle**, from intake of Dispatch's `RideAssigned` business event through either (a) terminal success via trip completion, (b) terminal cancellation via rider or driver pre-pickup cancellation, or (c) terminal abandonment via no-show timeout at pickup. Specifically:

- **Translation-in intake from Dispatch.** Consume `RideAssigned` (proto already authored at `/protos/crittercab/dispatch/v1/ride_assigned.proto`); idempotent on `tripId` (= `rideRequestId` per Workshop 001 ADR-candidate #7); local `TripMatched` event recorded as Klefter decision-event.
- **Driver-progression states.** En-route to pickup → at pickup → in progress → completed. Driver-app interaction surfaces (taps "I'm here", "Start trip", "End trip") trigger commands; events transition the Trip aggregate through the lifecycle.
- **No-show temporal automation (Bruun pattern).** Driver waits at pickup for a configurable timeout; if the rider hasn't joined, the system abandons the trip. Todo-list view + clock-rewind glyph automation per Workshop 001's Bruun convention; paired in narrative order with the at-pickup slice per Workshop 001 §12.6 #6.
- **Pre-pickup cancellation paths.** Rider-initiated cancellation; driver-initiated cancellation. Both terminate the Trip via `Cancelled` state; distinct events (`TripCancelledByRider`, `TripCancelledByDriver`) with downstream consumers electing which signal they care about.
- **Translation-out to Dispatch.** Post-terminal outcome event (event name TBD at slice walk) consumed by Dispatch's slice 5.12 Translation-in handler. Workshop 001 §5.12 explicitly waits on Trips committing this side.
- **Trips policy configuration as events.** No-show timeout interval, configurable thresholds — operator-tunable via `TripsPolicyConfigured`. Bruun configuration-as-events pattern; second BC adopting it, fires Workshop 001 ADR-candidate #5.
- **Driver-trip-view projection.** Projection consumed by driver-app trip-mode UI; surfaces rider display name (eventually-consistent enrichment from Identity per forward-constraint #2 / option 2c), pickup, dropoff, ETA.

### 2.2 At the boundary (modeled as Translation slices, not as internal behavior)

- **Dispatch → Trips (intake).** `RideAssigned` over ASB. Workshop 001's outbound contract (already-authored proto, PR #4) is the receiver-side starting point.
- **Identity / Rider Profile → Trips (eventually-consistent enrichment).** Identity-published business events (`RiderRegistered`, `RiderProfileUpdated`, or equivalents) feed `DriverTripView` projection's rider-name surface. Per forward-constraint #2 lean (option 2c). Identity's full event model is a separate workshop.
- **Trips → Dispatch (post-terminal outcome signal).** Trips' terminal events feed Dispatch's slice 5.12 Translation-in. Specific event shape decided at slice walk; Workshop 001's preferred shape was a single `TripTerminatedEarly` with reason enum but Trips workshop is empowered to choose otherwise.
- **Trips → Pricing.** `TripCompleted` triggers fare finalization. Cancellation events feed cancellation-fee logic. Modeled as named publication; Pricing's internals not modeled.
- **Trips → Payments.** Trip-start triggers payment authorization (offstage); `TripCompleted` triggers capture; cancellation triggers void/refund. Modeled as named publication; Payments' internals not modeled.
- **Trips → Ratings.** `TripCompleted` triggers post-trip rating invitations. Modeled as named publication.
- **Telemetry → (out of Trips aggregate).** High-frequency driver-position pings are Kafka-shaped, separate BC. Trips' aggregate references driver location only via consumed projections, never via direct Telemetry data.

### 2.3 Out of scope

- **Mid-trip cancellation paths** (rider, driver, emergency). Has its own compensation/driver-protection surface that warrants a dedicated slice family — held as parking-lot.
- **In-trip route deviations.** Rider asks for stop; driver takes wrong turn. Post-MVP.
- **Payment-authorization failures at trip start.** Payments BC owns; Trips assumes auth precondition has been satisfied.
- **Driver-position telemetry during the trip.** Kafka, separate BC, separate workshop.
- **Operator manual reassignment.** Operations BC concern.
- **Surge / fare adjustments mid-trip.** Pricing BC.
- **Identity / Rider Profile internals.** Forward-constraint #2 surfaces an Identity-side projection requirement (publishing `RiderRegistered` / `RiderProfileUpdated` business events Trips subscribes to); Identity's full workshop is a separate session.
- **Trips' internal projections beyond the slice walk's reach.** Speculative projections are named (per Workshop 001 §12.6 #1 from slice 1), not implemented in this workshop's deliverable.

### 2.4 Structural constraints honored

- All Dispatch / Identity / Pricing / Payments / Ratings / Telemetry boundary crossings are gRPC calls or Wolverine messages — no shared databases, no in-process cross-service calls (ADR-002).
- Cross-BC business events go via Azure Service Bus per `<source-bc>.<event-name-kebab>` topic convention (ADR-005; established in Workshop 001 §5.10; Workshop 001 ADR-candidate #8 fires here as the second data point).
- No provider-specific identity details appear in Trips events or projections; rider profile data flows through Identity ACL (ADR-006).
- Any new Protobuf contract implied by the model is named in §10 rather than authored inline; proto authorship is a downstream task (ADR-009, per Workshop 001 §12.6 #4).

### 2.5 Decisions locked during scope-setting

| Decision | Resolution |
|---|---|
| Cancellation scope | Pre-pickup only (rider + driver). Mid-trip cancellation deferred. |
| No-show timeout | In scope. Bruun temporal automation, paired with at-pickup slice in narrative order. |
| Outbound contract to Dispatch | In scope. Slice 5.12 of Workshop 001 has a stub waiting. |
| Inbound contract from Dispatch | Already authored (`/protos/crittercab/dispatch/v1/ride_assigned.proto`, PR #4). Input, not re-authored. |
| Trips policy configuration | In scope. Configuration-as-events; second BC adopting the pattern. |
| Rider-name surfacing on driver-app | Eventually-consistent via Identity event subscription (forward-constraint #2 lean: option 2c). Revisitable at slice walk if timing budget proves infeasible. |
| First-state vocabulary | `Matched` / `TripMatched`. Grounded in system-design literature canonical naming (sources cited in §1's mid-sidebar research note). |

---

## 3. Aggregate Identity

### 3.1 Why this section exists

Per Workshop 001 §12.6 #2, the aggregate-identity decision shapes every subsequent slice and is hard to retrofit. This section captures the sidebar's outcome before the slice walk begins.

This is also the **canonical second data point for ADR-candidate #6** (aggregate-per-invariant, not aggregate-per-noun). Workshop 001 named the pattern via `RideRequest` + `Offer`; Trips' decision either confirms the pattern (lifting #6 from candidate to authored ADR) or surfaces a counter-shape that revises it.

### 3.2 Candidate load-bearing invariants

| # | Invariant | Verdict |
|---|---|---|
| 1 | **Lifecycle-monotonicity.** Trip walks a strict state machine (matched → en-route-to-pickup → at-pickup → in-progress → completed/cancelled/no-show). No skipping, no reversing. | **Load-bearing.** This is the invariant the aggregate exists to protect. |
| 2 | **At-most-one-trip-per-`tripId`.** Idempotent intake from Dispatch's `RideAssigned`; at-least-once redelivery cannot spawn a duplicate trip. | **Structural consequence**, not separately load-bearing. Stream identity in Marten enforces this natively given #1 and the shared-ID decision (ADR-candidate #7). |
| 3 | **Single-active-trip-per-driver.** A driver can only be on one trip at a time. | **Not load-bearing on Trips.** Enforced upstream at Dispatch's offer-acceptance step (Workshop 001 §5.5 — sibling offers revoked at acceptance) and by Driver Profile's availability state. Trips is downstream of that enforcement. |
| 4 | **In-trip cancellation rules.** Once rider is in vehicle, cancellation paths differ (compensation, driver protection). | **Transition rule within #1**, not a separate invariant. The state machine restricts which cancellation events are legal at each lifecycle stage; out of this workshop's scope per §2.3. |

### 3.3 Decision

**Single `Trip` aggregate. Full lifecycle on one stream. No sub-entities.**

Stream keyed on `tripId` (= `rideRequestId` per Workshop 001 ADR-candidate #7). All lifecycle events — intake, driver-progression transitions, completion, cancellation, no-show abandonment — append to the same stream. Marten optimistic concurrency on the stream enforces the lifecycle-monotonicity invariant: every state-transition handler loads the aggregate, validates the proposed transition against current state, and the version check rejects concurrent attempts.

**Driver-progression states are lifecycle transitions, not sub-entities.** The Trip aggregate carries a status enum and nullable transition timestamps:

```
status: Matched | EnRouteToPickup | AtPickup | InProgress | Completed | Cancelled | NoShow
matchedAt           : timestamp        (set on TripMatched)
enRouteAt?          : timestamp?       (set on en-route transition)
arrivedAtPickupAt?  : timestamp?       (set on at-pickup transition)
tripStartedAt?      : timestamp?       (set on in-progress transition)
completedAt?        : timestamp?       (set on Completed terminal)
cancelledAt?        : timestamp?       (set on Cancelled terminal; either rider or driver event)
noShowedAt?         : timestamp?       (set on NoShow terminal)
```

Per the `marten-aggregates` skill convention, the aggregate is an immutable record with `static` `Apply` methods using C# `with` (per memory `feedback_decider_pattern_aggregate_workflow.md`). Per-transition event names (e.g., what fires the `Matched → EnRouteToPickup` transition) are slice-walk decisions, not sidebar decisions.

### 3.4 What is split off (out of aggregate)

Naming exclusions explicitly so the boundary reads as deliberate:

| Concern | Where it lives | Rationale |
|---|---|---|
| **High-frequency driver-position telemetry** | Telemetry BC (Kafka stream) | Operationally incompatible with Trips' write profile. ADR-002 names Telemetry as never-co-deployed; same operational logic applies to keeping its data out of Trips' streams. |
| **Payment events** (authorization, capture, refund) | Payments BC | Distinct transactional-integrity profile; likely Polecat-on-SQL-Server per vision. Trips publishes lifecycle outcomes; Payments owns the money. |
| **Rating events** (rider-rates-driver, driver-rates-rider) | Ratings BC | Cleanly separable; lives post-`Completed`. Trips publishes the trigger event; Ratings consumes. |
| **Driver assignment / matching decisions** | Dispatch BC | Already-modeled. Trips' aggregate begins at intake; never decides who drives. |
| **Rider profile data** (display name, photo, rating snapshot) | Identity / Rider Profile BC | Per ADR-006 identity-as-ACL discipline. Trips' driver-trip-view projection enriches asynchronously by subscribing to Identity-published events (forward-constraint #2 / option 2c). |

### 3.5 Compare-and-contrast with Workshop 001 (ADR-#6 evidence framing)

**Workshop 001's `RideRequest` + `Offer`:**
- *Invariant:* at most one `OfferAccepted` per `rideRequestId`.
- *Aggregate:* `RideRequest`. Offers are sub-entities — no lifecycle independent of the Request; offer-lifecycle events all append to the Request stream. Marten optimistic concurrency protects the invariant natively.
- *Counter-shape rejected:* drawing `Offer` as its own aggregate would require distributed locking to enforce the at-most-one invariant across N parallel offer streams.

**Workshop 002's `Trip`:**
- *Invariant:* lifecycle-monotonicity. State transitions follow a strict order; no skipping, no reversing.
- *Aggregate:* `Trip`. Driver-progression states are sub-states (status enum + nullable timestamps), not sub-entities — no independent lifecycle. All progression events append to the Trip stream. Marten optimistic concurrency protects the invariant natively.
- *Counter-shape rejected:* drawing per-stage entities (e.g., a `Pickup` sub-aggregate, a `TripExecution` sub-aggregate) with reference IDs would require cross-stream coordination to enforce ordering — exactly what the single-stream design avoids.

**Both BCs apply the same pattern: the aggregate boundary follows the invariant, not the noun.** Offer-as-sub-entity-of-Request and driver-progression-as-sub-state-of-Trip are structurally the same maneuver, with consistent reasoning. Two data points; the pattern holds.

**Recommendation: ADR-candidate #6 is ready to be lifted to authored ADR in a follow-up authorship session** (per Workshop 001's convention that ADR authoring is its own session, not in-workshop drafting).

### 3.6 Decisions locked in this section

| Decision | Resolution |
|---|---|
| Aggregate identity | Single `Trip` aggregate, stream keyed on `tripId` (= `rideRequestId`). |
| Sub-entities vs. sub-states | Driver-progression as sub-states (status enum + nullable timestamps), not sub-entities. |
| Out-of-aggregate concerns | Telemetry (Kafka), Payments (separate BC), Ratings (separate BC), Identity/Rider Profile (separate BC, ACL). |
| Aggregate shape primitive | Immutable record, `static` `Apply` methods, C# `with` for state evolution. Per `marten-aggregates` skill convention. |
| First state name | `Matched`. Birth event: `TripMatched`. Grounded in system-design literature canonical naming; avoids CRUD vocabulary; distinct from every Workshop 001 verb. |
| Cancellation terminal state | Single `Cancelled` state. Two distinct events feed it: `TripCancelledByRider` and `TripCancelledByDriver` — analogous to Workshop 001's pattern where distinct semantic loads get distinct events even when they share a downstream-consumer surface. |
| No-show terminal state | Distinct `NoShow` state, distinct event. System-driven (Bruun temporal automation), parallel to Workshop 001's `RideRequestAbandoned`. Specific event name set at slice walk. |
| ADR-candidate #6 evidence | Confirmed across two BCs with consistent reasoning. Recommend lift to authored ADR in follow-up authorship session. |

---

## 4. Ubiquitous Language

Bootstrapped from Workshop 001's UL plus the aggregate-identity sidebar; populated further as the slice walk forces decisions. Each term gets a one-line definition and, where relevant, a note on what it is *not*.

| Term | Definition | Notes |
|---|---|---|
| **Trip** | The post-acceptance entity, lifecycle-monotonic. Lives on the Trips timeline. Identified by `tripId`. | Same noun used in Workshop 001's UL. Pre-acceptance equivalent is Ride Request (Dispatch). |
| **Trip ID** | Canonical opaque identifier shared with Dispatch's `rideRequestId`, Pricing's payment reference, and Ratings' ride ID. | Per Workshop 001 ADR-candidate #7. |
| **Trip Lifecycle** | The state machine: `Matched → EnRouteToPickup → AtPickup → InProgress → Completed` (happy path), with terminal off-ramps to `Cancelled` (pre-pickup, rider or driver) and `NoShow` (system-driven temporal). | Lifecycle-monotonicity is the load-bearing aggregate invariant (§3). |
| **Trip Status** | The current lifecycle stage of a Trip. Status enum + nullable timestamps recorded on the aggregate. | Per `marten-aggregates` skill convention. |
| **Driver Progression** | Driver's lifecycle transitions (en-route → at-pickup → in-progress → completed). Sub-states of the Trip aggregate, not sub-entities. | Compare Workshop 001's `Offer` as sub-entity-of-`RideRequest`: same aggregate-per-invariant pattern. |
| **No-Show Timeout** | The configurable interval after `AtPickup` after which an unjoined rider triggers system abandonment. Operator-tunable via `TripsPolicyConfigured`. | Bruun temporal automation slice pattern. |
| **Trip Outcome** | The terminal disposition of a Trip — completed, cancelled, no-show. | Distinct from Trip Status: outcome is the *kind* of terminal; status is the lifecycle position (which happens to be terminal at outcome time). |
| **Driver Trip View** | Projection consumed by the driver-app trip-mode UI. Surfaces rider display name (eventually-consistent from Identity), pickup, dropoff, ETA. | Forward-constraint #2 from narrative 002. |
| **Trips Policy** | Operator-tunable parameters (no-show timeout interval, etc.) event-sourced via `TripsPolicyConfigured`. | Singleton aggregate analogous to `DispatchPolicy` in Workshop 001. |
| **Trip Matched** | Past-tense fact: a trip has come into existence as a result of Dispatch's assignment. Birth event for the Trip aggregate. | Grounded in system-design literature ("ride transitions to MATCHED"). Distinct from Dispatch's `OfferAccepted` (the driver's act) and `RideAssigned` (Dispatch's act). |

Additional terms are added to this glossary as the slice walk forces vocabulary decisions.

---

## 5. Event List (chronological)

Populated slice-by-slice. Each entry carries: event name, producing slice reference, key payload fields at domain grain, and lane assignment.

| # | Event | Slice | Key payload | Lane |
|---|---|---|---|---|
| 1 | `TripMatched` | 6.1 | `tripId`, `rideRequestId`, `riderId`, `driverId`, `pickup`, `dropoff`, `vehicleClass`, `fareAmount`, `currency`, `fareBreakdown`, `pricingPolicyVersion`, `notesForDriver?`, `dispatchAssignedAt`, `matchedAt` | Trips (birth event; recorded from Translation-in handler) |
| 2 | `TripDeparted` | 6.2 | `tripId`, `driverId`, `enRouteAt` | Trips (driver-app Command; transition Matched → EnRouteToPickup) |
| 3 | `DriverArrivedAtPickup` | 6.3 | `tripId`, `driverId`, `arrivedAtPickupAt`, `expiresAt`, `tripsPolicyVersion` | Trips (driver-app Command; transition EnRouteToPickup → AtPickup; carries no-show `expiresAt` per Bruun) |
| 4 | `TripAbandonedAsNoShow` | 6.4 | `tripId`, `driverId`, `riderId`, `expiresAt`, `noShowedAt` | Trips (Bruun temporal automation; transition AtPickup → NoShow) |
| 5 | `TripStarted` | 6.5 | `tripId`, `driverId`, `riderId`, `tripStartedAt` | Trips (driver-app Command; transition AtPickup → InProgress; disarms slice 6.4) |
| 6 | `TripCompleted` | 6.6 | `tripId`, `driverId`, `riderId`, `completedAt` | Trips (driver-app Command + atomic cross-BC publications; terminal InProgress → Completed) |
| 7 | `TripCancelledByRider` | 6.7 | `tripId`, `riderId`, `driverId`, `reason`, `notes?`, `priorStatus`, `cancelledAt` | Trips (rider-app Command + atomic cross-BC publications; terminal pre-pickup → Cancelled) |
| 8 | `TripCancelledByDriver` | 6.8 | `tripId`, `driverId`, `riderId`, `reason`, `notes?`, `priorStatus`, `cancelledAt` | Trips (driver-app Command + atomic cross-BC publications; terminal pre-pickup → Cancelled) |
| 9 | `TripsPolicyConfigured` | 6.11 | `operatorId`, `noShowTimeoutSeconds`, `reason?`, `configuredAt` | Trips (singleton aggregate stream; operator-initiated; configuration-as-events) |

---

## 6. Slice Walk

Each slice section carries: pattern type (Command / View / Automation / Translation), trigger, command(s), event(s), view(s) read or produced, GWT sketches, decisions locked, and cross-reference notes. Slices are walked in the order locked at §1's slice-ordering proposal; ordering rationale tied to Workshop 001 §12.6 #6 (temporal-automation pairing) and §12.6 #5 (sub-slice notation).

### 6.1 Slice 1 — TripMatched (Translation-in from Dispatch)

**Pattern:** Translation (in) + Klefter decision-event.
**Lane:** Trips for the handler and local events; ASB inbound from Dispatch.
**Trigger:** ASB message on topic `dispatch.ride-assigned` (session-keyed by `rideRequestId` = `tripId`).

#### Flow on the board

```
   ┌─────────────────────────────────────┐
   │ Dispatch BC                         │
   │  [Workshop 001 §5.10]               │
   │  Atomic emission in AcceptOffer:    │
   │    OfferAccepted + OfferRevoked×N   │
   │    + RideAssigned                   │
   │  + outgoing ASB publication         │
   └──────────────┬──────────────────────┘
                  │ ASB
                  │ Topic: dispatch.ride-assigned
                  │ Session key: rideRequestId
                  │ At-least-once delivery
                  │ Wire format: Protobuf RideAssigned
                  │   (already-authored, PR #4)
                  ▼
   ┌─────────────────────────────────────┐
   │ Trips Translation-in handler        │
   │ (Wolverine message handler)         │
   │ Idempotent on tripId                │
   │ Klefter: local decision recording   │
   └──────────────┬──────────────────────┘
                  │ create Trip aggregate
                  │ if not already exists
                  ▼
   ┌─────────────────────────────────────┐
   │ TripMatched                         │  orange event
   │ (Trip stream; first event)          │  (Trip aggregate's birth)
   └──────────────┬──────────────────────┘
                  │
                  ▼
   Views fed:
     • DriverTripView      (inline; latency-critical)
     • TripTimeline        (async)
     • ActiveTripsByDriver (async)
     • ActiveTripsByRider  (async)
```

#### Inbound contract

| Aspect | Value |
|---|---|
| Source | Dispatch BC, `AcceptOffer` handler atomic emission (Workshop 001 §5.5 / §5.10) |
| Transport | Azure Service Bus (ADR-005) |
| Topic | `dispatch.ride-assigned` |
| Session key | `rideRequestId` (= `tripId` per ADR-candidate #7) |
| Delivery | At-least-once |
| Wire format | Protobuf (`RideAssigned`); already authored at `/protos/crittercab/dispatch/v1/ride_assigned.proto` (PR #4) |
| Treatment in this workshop | Input. Not re-authored, not re-shaped. |

#### Idempotency contract — forward-constraint #1 honored

Handler keyed on `tripId`. If a Trip aggregate already exists at the supplied `tripId`, the handler is a no-op — at-least-once redelivery does not spawn a duplicate trip and does not re-emit `TripMatched`. Per Workshop 001 §5.10's idempotency promise to consumers.

This **honors forward-constraint #1** from narratives 001 + 002 ("Trips' intake is idempotent on `rideRequestId` (= `tripId`)").

#### Klefter decision-event recording

The act of accepting responsibility for the trip — distinct from Dispatch's `RideAssigned` which only records that the assignment *happened* — is captured as the local `TripMatched` event. Klefter pattern: even though the upstream system has its own record of the assignment, Trips records the *consequence* in its own event store, decoupled from Dispatch's vocabulary and retention policy.

This is also the structural basis for forward-constraints #2 and #3: every projection Trips builds (`DriverTripView`, etc.) projects from `TripMatched` (and subsequent Trip events), not from Dispatch's `RideAssigned`.

#### Event — `TripMatched`

Past-tense fact. Birth event for the Trip aggregate.

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | = `rideRequestId` from inbound. Stream key for the Trip aggregate. |
| `rideRequestId` | opaque ID | Echoed for traceability back to Dispatch's stream. (Same value as `tripId`; redundant by design — this is the value's "Dispatch face.") |
| `riderId` | opaque ID | From `RideAssigned`. |
| `driverId` | opaque ID | From `RideAssigned`. |
| `pickup` | `{ lat, lon, streetAddress? }` | From `RideAssigned`. |
| `dropoff` | `{ lat, lon, streetAddress? }` | From `RideAssigned`. |
| `vehicleClass` | enum | From `RideAssigned`. |
| `fareAmount` | integer (minor units) | From `RideAssigned`. Locked at this point; trip-time fare adjustments belong to Trips' own logic post-completion. |
| `currency` | ISO 4217 code | From `RideAssigned`. |
| `fareBreakdown` | `{ base, distance, time, fees? }` | From `RideAssigned`. Carried for transparency and post-completion audit. |
| `pricingPolicyVersion` | opaque | From `RideAssigned`. Enables cross-BC fare-dispute audit. |
| `notesForDriver` | string? | From `RideAssigned`. Surfaced to driver via `DriverTripView`. |
| `dispatchAssignedAt` | timestamp | `assignedAt` from `RideAssigned`. The moment the assignment originated upstream. Explicitly named with origin BC to avoid accidental conflation if Trips later has its own "assigned" semantics. |
| `matchedAt` | timestamp | Server clock at Trips' handler-fire time. The moment Trips recorded the trip locally. |

The `dispatchAssignedAt` / `matchedAt` separation enables timing-budget observability (forward-constraint #3 signal). The gap between them is the cross-BC latency from Dispatch's commit to Trips' commit — analogous to Workshop 001's `expiresAt` / `expiredAt` distinction in `OfferExpired`.

#### Projection lifecycle (forward-constraint #3 partially honored)

| Question | Decision |
|---|---|
| `DriverTripView` update lifecycle on `TripMatched` | **Inline.** Marten projection runs in the same transactional commit as the event append. Driver-app's gRPC server-streaming view receives the new trip-mode payload as soon as the commit lands. |
| Other projections (`TripTimeline`, `ActiveTripsByDriver`, `ActiveTripsByRider`) | **Async** (Marten async daemon). Consumed by ops dashboards and rider-app, neither of which is on the latency-sensitive driver-app path. |
| Timing-budget target | **Subjectively instantaneous trip-mode transition.** Concretely: the round-trip from the driver's tap on Accept (Workshop 001 §5.5) to the driver-app trip-mode UI rendering must complete before the driver reads any visual lag. |
| Whether the timing budget warrants its own ADR | **Yes — new ADR candidate.** Achievable with current tooling (inline projection + ASB low-latency mode + gRPC server-streaming push) but warrants documentation as a cross-cutting non-functional requirement. Captured in §12. |

This **partially honors forward-constraint #3**: the projection mechanism is locked, the timing budget is named, but explicit ADR authorship is deferred.

#### Forward-constraint #2 partial landing

`DriverTripView`'s shape on first commit:

```
{ tripId, driverId, riderId,
  riderDisplayName: null,        ← enriched async by slice 6.12
  pickup, dropoff, vehicleClass,
  status: Matched, eta, ... }
```

Slice 6.12 layers in the eventually-consistent enrichment from Identity events. This slice locks the projection's *shape*; slice 6.12 locks the *enrichment-source*. **Partially honors forward-constraint #2** (option 2c). Driver-app handles `riderDisplayName: null` gracefully (placeholder text such as "Rider" until the Identity event lands).

#### GWT sketches

**Happy path — first delivery**
```
Given: no Trip aggregate exists at tripId X
When: Trips receives ASB message dispatch.ride-assigned for X
       with full RideAssigned payload, dispatchAssignedAt = T
Then: TripMatched { tripId: X, ...full payload..., dispatchAssignedAt: T,
                    matchedAt: now() } is emitted
  And: Trip aggregate is created at tripId X with status: Matched
  And: DriverTripView is updated inline (riderDisplayName: null)
  And: ActiveTripsByDriver, ActiveTripsByRider, TripTimeline updated async
```

**At-least-once redelivery (idempotency)**
```
Given: TripMatched { tripId: X, ... } already exists on the stream
When: Trips receives ASB message dispatch.ride-assigned for X again (redelivery)
Then: handler observes existing aggregate at tripId X
  And: ASB message is acked silently
  And: no second TripMatched emitted; no projection updates
  And: handler logs an observability signal for redelivery rate (no domain event)
```

**Malformed payload (transport-grade error)**
```
Given: ASB message arrives with payload that fails proto deserialization
       or carries a tripId that violates UUID v7 format
When: Wolverine handler receives the message
Then: standard Wolverine error handling — retry-with-backoff per ADR-005;
       persistent failure → dead-letter queue
  And: no domain event emitted; no aggregate created
  And: ops-grade observability signal logged for malformed-message rate
```

#### Rejection / error reasons

Trips' Translation-in handler does NOT reject domain-meaningfully — it records what Dispatch validated. Three failure modes:

1. **Malformed payload** → Wolverine retry + DLQ (transport-grade).
2. **Idempotency hit (Trip already exists)** → silent ack (expected race, not an error).
3. **Database / infrastructure failure** → Wolverine retry; the message is not acked until commit succeeds.

There are no domain-grade rejection reasons because validation concerns ("the rider is invalid", "the driver is suspended") are upstream concerns Dispatch already cleared before emitting `RideAssigned`. Trips records, does not re-validate.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `DriverTripView` | Driver app (gRPC server-streaming) | `{ tripId, driverId, riderId, riderDisplayName?, pickup, dropoff, status, eta }` | `TripMatched` + Identity events (slice 6.12) + driver-progression events (slices 6.2-6.6) | **Slice-1 inline; enriched async**. Forward-constraints #2 + #3 land here. |
| `TripTimeline` | Ops, customer support tooling, post-trip review | Per-`tripId`: chronological event log | All Trip events | **Slice-1 async**. |
| `ActiveTripsByDriver` | Driver app, Driver Profile, ops | Per-`driverId`: currently active trip (at most one) | `TripMatched`, all terminal events | **Slice-1 async**. Reused across all subsequent slices. |
| `ActiveTripsByRider` | Rider app, Rider Profile, ops | Per-`riderId`: currently active trip (at most one) | `TripMatched`, all terminal events | **Slice-1 async**. Reused. |
| `MatchingLatencyMetrics` | Ops, SRE, product KPI | Per-trip: `dispatchAssignedAt → matchedAt` interval distribution | `TripMatched` | **Defer but pin** — forward-constraint #3 timing-budget observability. Parallel to Workshop 001's `ExpirerSlippageAudit`. |
| `CrossBcTripTimeline` | Ops, support tooling | Per-`tripId`: joined timeline across Dispatch, Trips, Pricing, Payments, Ratings | Cross-BC | **Defer** — Workshop 001 §5.10 already pinned this as candidate Context Graph projection. |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Translation-in + Klefter decision-event. |
| Idempotency key | `tripId`. Honors forward-constraint #1. |
| Idempotency mechanism | Stream-existence check on Marten. At-least-once redelivery → silent ack. |
| `TripMatched` payload | Denormalized — full assignment context preserved. Trips has everything to stand up the aggregate without calling back into Dispatch. Mirrors Workshop 001 §5.10's denormalization principle. |
| Timestamp naming | `dispatchAssignedAt` (explicit BC origin) and `matchedAt` (server clock). Two timestamps preserved for timing-budget observability. |
| `DriverTripView` projection lifecycle | Inline (same commit as event append). Driver-app trip-mode transition is latency-critical. |
| Other projections | Async (Marten daemon). Not on latency-critical path. |
| Domain rejection | None. Trips records what Dispatch validated. |
| `riderDisplayName` on first commit | Null. Enriched asynchronously by slice 6.12 from Identity-published events. **Honors forward-constraint #2 / option 2c.** |
| Timing-budget ADR | New ADR candidate — captured in §12. |

#### Cross-references and ripples

- **Backward (cross-BC):** Triggered by Dispatch's `RideAssigned` ASB publication (Workshop 001 §5.10).
- **Forward (slices 6.2-6.6):** All driver-progression slices load the Trip aggregate created here; lifecycle-monotonicity invariant enforced at each.
- **Forward (slice 6.12):** `DriverTripView` is enriched with `riderDisplayName` via Identity-event subscription. Projection shape committed here; slice 6.12 layers in the enrichment-source.
- **Forward (slice 6.9):** When the Trip reaches a terminal, the outbound publication to Dispatch references the same `tripId` — closing the loop on ADR-candidate #7's shared-identifier principle.
- **ADR-candidate #7:** Second concrete data point. Shared-identifier principle confirmed across the full Dispatch ↔ Trips round-trip; recommend lift to authored ADR alongside #6 in follow-up authorship session.
- **New ADR candidate (driver-app projection timing budget):** Captured in §12.
- **Forward-constraints status:** #1 honored (idempotency); #2 partially honored (projection shape committed, null on first commit); #3 partially honored (mechanism + budget locked; explicit ADR deferred). Tracked in §13.

### 6.2 Slice 2 — TripDeparted (Command from driver-app)

**Pattern:** Command.
**Lane:** Trips.
**Trigger:** Driver-app trip-mode UI activation. The driver-app sends `ConfirmTripDeparture` automatically once it has rendered trip-mode and begun navigation toward pickup. No explicit driver tap; trip-mode load is the implicit confirmation, mirroring real ride-sharing UX (Uber, Lyft).

#### Flow on the board

```
   ┌────────────────────────────────────┐
   │ Driver-app receives DriverTripView │
   │ inline update from slice 6.1's     │
   │ TripMatched commit. Trip-mode UI   │
   │ renders; navigation activates.     │
   └──────────────┬─────────────────────┘
                  │ gRPC unary
                  │ (driver-app → Trips)
                  ▼
   ┌────────────────────────────────────┐
   │ ConfirmTripDeparture               │  blue command
   └──────────────┬─────────────────────┘
                  │  [AggregateHandler] loads Trip
                  │  validates current status = Matched
                  ▼
   ┌────────────────────────────────────┐
   │ TripDeparted                       │  orange event (Trip stream)
   └──────────────┬─────────────────────┘
                  │
                  ▼
   Views updated:
     • DriverTripView      (status: EnRouteToPickup)
     • TripTimeline        (async)
     • ActiveTripsByDriver (async; status visible)
     • ActiveTripsByRider  (async; rider-app status updates)
```

#### Command — `ConfirmTripDeparture`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | The trip whose departure is being confirmed. |
| `driverId` | opaque ID | Authenticated driver context; must match the Trip's `driverId`. |
| `departedAt` | timestamp | Server clock at command receipt. |

#### Event — `TripDeparted`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | Echoed for projection convenience. |
| `enRouteAt` | timestamp | Sets the aggregate's `enRouteAt` timestamp. Equal to `departedAt` from the command. |

Lean payload — the trip's full context is already on `TripMatched`; this event records the state transition only.

#### Aggregate transition

`Matched → EnRouteToPickup`. The `[AggregateHandler]` loads the Trip and validates the current status is `Matched`. Lifecycle-monotonicity invariant enforced: a `ConfirmTripDeparture` against an aggregate already past Matched is rejected.

#### Rejection reasons

`TRIP_NOT_FOUND`, `TRIP_ALREADY_DEPARTED`, `TRIP_ALREADY_TERMINATED` (any of `Cancelled` / `NoShow` / `Completed`), `NOT_THE_TRIPS_DRIVER`.

#### GWT sketches

**Happy path**
```
Given: TripMatched { tripId: X, driverId: D1, ... } exists; aggregate status = Matched
When: ConfirmTripDeparture { tripId: X, driverId: D1, departedAt: T }
Then: TripDeparted { tripId: X, driverId: D1, enRouteAt: T } is emitted
  And: aggregate status = EnRouteToPickup
  And: DriverTripView updates inline (status: EnRouteToPickup)
  And: TripTimeline, ActiveTripsByDriver, ActiveTripsByRider updated async
```

**Idempotent re-confirmation (driver-app retries the gRPC call)**
```
Given: TripDeparted for X already exists; aggregate status = EnRouteToPickup
When: ConfirmTripDeparture { tripId: X, driverId: D1, departedAt: T2 }
Then: command is rejected with TRIP_ALREADY_DEPARTED
  And: no second TripDeparted emitted
  Note: driver-app should treat this rejection as a successful no-op
        (it's the desired idempotent outcome).
```

**Stale departure (trip terminated meanwhile)**
```
Given: TripMatched for X; rider cancels via slice 6.7 emitting TripCancelledByRider
       before driver-app loads trip-mode
When: ConfirmTripDeparture { tripId: X, driverId: D1, departedAt: T }
Then: command is rejected with TRIP_ALREADY_TERMINATED
  And: driver-app's trip-mode UI surfaces the cancellation to the driver
       (returning the driver to offer-mode).
```

#### Views fed

- **`DriverTripView`** — status field updated to `EnRouteToPickup`. Inline update on the latency-critical path.
- **`TripTimeline`** — adds `TripDeparted`. Async.
- **`ActiveTripsByDriver`** — status field updated. Async.
- **`ActiveTripsByRider`** — status field updated; rider-app surfaces "your driver is on the way." Async.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `DriverAppActivationLatency` | SRE, Driver Profile | Per-trip: `matchedAt → enRouteAt` interval | `TripMatched`, `TripDeparted` | **Defer but pin** — observability for driver-app cold-start / trip-mode load time. Parallel to slice 6.1's `MatchingLatencyMetrics`. |
| `AcceptanceFraudSignals` | Trust & Safety, Driver Profile | Per-driver: count of trips where `enRouteAt` never followed `matchedAt` (or with abnormal slippage) | `TripMatched`, `TripDeparted`, `TripCancelledByDriver` | Defer — Trust & Safety BC concern. |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Trigger source | Driver-app, on trip-mode UI activation. No explicit driver tap; trip-mode load is the implicit confirmation. |
| Pattern | Command (driver-app → Trips), gRPC unary (consistent with Workshop 001's `AcceptOffer` / `DeclineOffer` pattern). |
| Event name | `TripDeparted`. Concise, verb-form clean, ride-sharing-domain natural. |
| Idempotency | Aggregate-level via Marten optimistic concurrency. Re-confirmation rejected with `TRIP_ALREADY_DEPARTED`; driver-app treats rejection as desired no-op. |
| Two-timestamp pattern continued | `matchedAt` (slice 6.1) + `enRouteAt` (here). Slippage signal for `DriverAppActivationLatency`. |
| Domain rejection reasons | `TRIP_NOT_FOUND`, `TRIP_ALREADY_DEPARTED`, `TRIP_ALREADY_TERMINATED`, `NOT_THE_TRIPS_DRIVER`. |

#### Cross-references and ripples

- **Backward:** Triggered by driver-app receiving `DriverTripView` inline update from slice 6.1.
- **Forward (slice 6.3):** Driver progresses from `EnRouteToPickup` to `AtPickup`.
- **Forward (slices 6.7, 6.8):** Rider or driver cancellation pre-pickup is legal during `EnRouteToPickup` state.
- **Observability:** New candidate projection `DriverAppActivationLatency` pinned. First slice in this workshop where the lifecycle-monotonicity invariant (§3) is actively load-bearing.

### 6.3 Slice 3 — DriverArrivedAtPickup (Command from driver-app)

**Pattern:** Command.
**Lane:** Trips.
**Trigger:** Driver-app — driver taps "I'm here" / "Arrived" at the pickup location. Geofence auto-detection is a future UX enhancement, not in v1.

#### Flow on the board

```
   ┌────────────────────────────────────┐
   │ Driver-app trip-mode UI            │
   │ Driver taps "I'm here" at pickup   │
   └──────────────┬─────────────────────┘
                  │ gRPC unary
                  ▼
   ┌────────────────────────────────────┐
   │ ConfirmArrivedAtPickup             │  blue command
   └──────────────┬─────────────────────┘
                  │  [AggregateHandler] loads Trip
                  │  validates current status = EnRouteToPickup
                  │  reads TripsPolicy.noShowTimeoutSeconds
                  │  computes expiresAt (Bruun carry-the-value)
                  ▼
   ┌────────────────────────────────────┐
   │ DriverArrivedAtPickup              │  orange event (Trip stream)
   │   carries expiresAt on the event   │
   └──────────────┬─────────────────────┘
                  │
                  ▼
   Views updated:
     • DriverTripView          (status: AtPickup)
     • TripTimeline            (async)
     • ActiveTripsByDriver     (async)
     • ActiveTripsByRider      (async; rider-app says "your driver is here")
     • RidersAwaitingBoarding* (async; Bruun todo-list — feeder for slice 6.4)
```

#### Command — `ConfirmArrivedAtPickup`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | Authenticated driver context; must match Trip's `driverId`. |
| `arrivedAt` | timestamp | Server clock at command receipt. |

#### Event — `DriverArrivedAtPickup`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | Echoed for projection convenience. |
| `arrivedAtPickupAt` | timestamp | Sets the aggregate's `arrivedAtPickupAt` timestamp. |
| `expiresAt` | timestamp | Computed at handler-fire time as `arrivedAtPickupAt + TripsPolicy.noShowTimeoutSeconds`. Carried on the event itself (Bruun carry-the-value). |
| `tripsPolicyVersion` | opaque | Policy version in effect at carry-the-value computation. Audit trail. |

#### Aggregate transition

`EnRouteToPickup → AtPickup`. Lifecycle-monotonicity enforced. A `ConfirmArrivedAtPickup` against `Matched`, `AtPickup`, `InProgress`, `Completed`, `Cancelled`, or `NoShow` is rejected.

#### Feeds the no-show timeout (slice 6.4) — Bruun pattern feeder

This event populates `RidersAwaitingBoarding*` — the asterisk-suffix Bruun todo-list view that slice 6.4's temporal automation reads. The `expiresAt` is computed once *here*, at handler-fire time, and carried on the event itself. **Mid-flight policy changes (slice 6.11) do not retroactively shift this trip's no-show deadline.** Mirrors Workshop 001 §5.4's `OfferSent.expiresAt` discipline.

#### Rejection reasons

`TRIP_NOT_FOUND`, `TRIP_NOT_EN_ROUTE` (status not `EnRouteToPickup`), `TRIP_ALREADY_TERMINATED`, `NOT_THE_TRIPS_DRIVER`.

#### GWT sketches

**Happy path**
```
Given: Trip aggregate at status = EnRouteToPickup
  And: TripsPolicy.noShowTimeoutSeconds = 300
When: ConfirmArrivedAtPickup { tripId: X, driverId: D1, arrivedAt: T }
Then: DriverArrivedAtPickup { tripId: X, driverId: D1,
                               arrivedAtPickupAt: T, expiresAt: T+300s,
                               tripsPolicyVersion } is emitted
  And: aggregate status = AtPickup
  And: DriverTripView updates inline (status: AtPickup)
  And: RidersAwaitingBoarding* row added: { tripId: X, expiresAt: T+300s }
  And: TripTimeline, ActiveTripsByDriver, ActiveTripsByRider updated async
```

**Driver tries to mark arrival too early (still in Matched)**
```
Given: TripMatched for X; aggregate status = Matched
       (driver-app trip-mode never activated — slice 6.2 didn't fire)
When: ConfirmArrivedAtPickup { tripId: X, driverId: D1, arrivedAt: T }
Then: command is rejected with TRIP_NOT_EN_ROUTE
```

**Stale arrival (trip terminated meanwhile)**
```
Given: Trip in EnRouteToPickup state; rider cancels via slice 6.7
When: ConfirmArrivedAtPickup { tripId: X, driverId: D1, arrivedAt: T }
Then: command is rejected with TRIP_ALREADY_TERMINATED
  And: driver-app surfaces the cancellation; driver returned to offer-mode.
```

#### Views fed

- **`DriverTripView`** — status field updated to `AtPickup`. Inline.
- **`TripTimeline`** — adds `DriverArrivedAtPickup`. Async.
- **`ActiveTripsByDriver`** — status field updated. Async.
- **`ActiveTripsByRider`** — rider-app surfaces "your driver has arrived." Async.
- **`RidersAwaitingBoarding*`** — Bruun todo-list view (asterisk suffix). Rows: `{ tripId, driverId, riderId, arrivedAtPickupAt, expiresAt }`. Consumed exclusively by slice 6.4's `NoShowTimeoutAutomation`. Rows removed on slice 6.5 (`TripStarted`), slice 6.7/6.8 (cancellations), or slice 6.4 (no-show terminal). Async.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `DriverArrivalAccuracy` | Driver Profile, ML, ops | Per-trip: comparison of geofenced pickup location vs. event location signal | `DriverArrivedAtPickup` + Telemetry geofence data | Defer — useful for fraud detection ("driver tapped arrived from 2 blocks away"); requires Telemetry coupling. |
| `PickupETAAccuracy` | Ops, product, ML | Per-trip: predicted ETA at `TripDeparted` vs. actual elapsed `enRouteAt → arrivedAtPickupAt` | `TripDeparted`, `DriverArrivedAtPickup` | Defer but pin — core product KPI. |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Trigger source | Driver-app explicit tap. Geofence auto-detection deferred to post-MVP UX enhancement. |
| Pattern | Command (driver-app → Trips), gRPC unary. |
| Event name | `DriverArrivedAtPickup`. Driver-prefix reflects that arrival is the driver's act. |
| Carry-the-value for `expiresAt` | Yes — computed from `TripsPolicy.noShowTimeoutSeconds` at handler-fire time and carried on the event. Bruun pattern; mirrors Workshop 001 §5.4. Mid-flight policy changes do not retroactively shift this trip's deadline. |
| `tripsPolicyVersion` on event | Yes — captures which policy version was in effect at carry-the-value time. Audit trail; mirrors Workshop 001 §5.10's `pricingPolicyVersion`. |
| Idempotency | Aggregate-level via Marten optimistic concurrency. |
| Rejection reasons | `TRIP_NOT_FOUND`, `TRIP_NOT_EN_ROUTE`, `TRIP_ALREADY_TERMINATED`, `NOT_THE_TRIPS_DRIVER`. |

#### Cross-references and ripples

- **Backward:** Triggered by driver's explicit tap "I'm here" in driver-app trip-mode UI.
- **Forward (slice 6.4):** `RidersAwaitingBoarding*` populated here; slice 6.4's `NoShowTimeoutAutomation` reads against it. **§12.6 #6 narrative-pairing rule applies** — slice 6.4 walks immediately after this one.
- **Forward (slice 6.5):** `TripStarted` (rider boards) removes the `RidersAwaitingBoarding*` row.
- **Forward (slices 6.7, 6.8):** Cancellation pre-pickup is legal during `AtPickup` state; cancellation also removes the `RidersAwaitingBoarding*` row.
- **Forward (slice 6.11):** `TripsPolicy.noShowTimeoutSeconds` is consumed here; slice 6.11 defines the policy event and ADR-candidate #5 fires.

### 6.4 Slice 4 — TripAbandonedAsNoShow (Bruun temporal automation)

**Pattern:** Automation Pattern, time-driven. Bruun notation: clock-rewind glyph on the automation sticky, asterisk suffix on the todo-list it consumes.
**Lane:** Trips.
**Trigger:** Time reaching any row's `expiresAt` in `RidersAwaitingBoarding*` (populated by slice 6.3).

#### Flow on the board

```
   (upstream: rows flow in via DriverArrivedAtPickup (6.3);
    disposed rows leave via 6.4/6.5/6.7/6.8)

      ┌────────────────────────────────────┐
      │  RidersAwaitingBoarding*           │  green sticky, asterisk (Bruun)
      │  Rows: { tripId, driverId,         │  automation work queue — no UI
      │          riderId, arrivedAtPickupAt,│
      │          expiresAt }               │
      └──────────────┬─────────────────────┘
                     │ rows where now() >= expiresAt
                     ▼
          ┌──────────────────────────┐
          │   NoShowTimeoutAutomation│  gear + clock-rewind glyph
          │   (⚙ + ⏪)                │  time-driven trigger
          └──────────┬───────────────┘
                     │ per stale row
                     ▼
          ┌──────────────────────┐
          │   AbandonAsNoShow    │  blue command
          └──────────┬───────────┘
                     │  [AggregateHandler] validates Trip still AtPickup
                     ▼
          ┌──────────────────────────┐
          │   TripAbandonedAsNoShow  │  orange event (Trip stream)
          └──────────────────────────┘
```

#### Automation — `NoShowTimeoutAutomation`

**Sticky:** gear + clock-rewind glyph. Mirrors Workshop 001 §5.7's `OfferExpirer`.

**Reads:** `RidersAwaitingBoarding*` — rows where `now() >= expiresAt`.

**Emits:** one `AbandonAsNoShow` command per stale row.

**Implementation mechanism:** Wolverine scheduled messages or Marten async daemon (Critter Stack primitives). Specific choice is a skill-file decision, not a modeling concern.

#### Command — `AbandonAsNoShow`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `noShowedAt` | timestamp | Server clock at automation-fire time. |

#### Event — `TripAbandonedAsNoShow`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | Echoed for projection convenience and Driver Profile reliability tracking. |
| `riderId` | opaque ID | Echoed for Rider Profile no-show tracking. |
| `expiresAt` | timestamp | Echoed from `DriverArrivedAtPickup` — the originally-scheduled timeout. Self-contained for audit. |
| `noShowedAt` | timestamp | When the automation actually fired. Slippage signal: `noShowedAt - expiresAt`. |

Lean payload — full trip context already on `TripMatched` and `DriverArrivedAtPickup`. Mirrors Workshop 001 §5.7's stream-bloat-avoidance discipline.

#### Aggregate transition

`AtPickup → NoShow`. Lifecycle-monotonicity enforced.

#### Silent race-rejection (expected, not an error)

When the handler loads the Trip and finds it already has a terminal disposition (`InProgress` after slice 6.5, `Cancelled` after slices 6.7/6.8, or `NoShow` after a sibling automation invocation), the command is silently dropped. No event emitted; no alarm surfaces. Logged for SRE visibility only.

**Possible race partners** (all resolve via Marten optimistic concurrency on the Trip stream):

1. **Rider boards just before timeout fires** — slice 6.5 emits `TripStarted`; handler sees `InProgress`, silently drops.
2. **Rider cancels just before timeout fires** — slice 6.7. Handler sees `Cancelled`, silently drops.
3. **Driver cancels just before timeout fires** — slice 6.8. Handler sees `Cancelled`, silently drops.
4. **Two timeout-automation invocations fire near-simultaneously** — first wins via stream version; second silently drops.

#### GWT sketches

**Happy path — no-show timeout fires cleanly**
```
Given: DriverArrivedAtPickup { tripId: X, expiresAt: T+300s }
  And: no terminal disposition for X
  And: current time advances to T+300s
When: NoShowTimeoutAutomation finds X stale and issues
       AbandonAsNoShow { tripId: X, noShowedAt: T+300s }
Then: TripAbandonedAsNoShow { tripId: X, driverId, riderId,
                               expiresAt: T+300s, noShowedAt: T+300s } is emitted
  And: aggregate status = NoShow
  And: RidersAwaitingBoarding* row removed
  And: DriverTripView, ActiveTripsByDriver, ActiveTripsByRider, TripTimeline updated
```

**Race — rider boards just before timeout commits**
```
Given: DriverArrivedAtPickup { tripId: X, expiresAt: T+300s }
  And: TripStarted { tripId: X } committed at T+299.9s (slice 6.5)
When: NoShowTimeoutAutomation picks up X from a stale todo-list snapshot
       and issues AbandonAsNoShow at T+300.05s
  And: handler loads Trip and finds status = InProgress
Then: command is silently dropped
  And: no TripAbandonedAsNoShow emitted
```

**Race — rider cancels just before timeout fires**
```
Given: DriverArrivedAtPickup { tripId: X, expiresAt: T+300s }
  And: TripCancelledByRider { tripId: X } committed at T+299.5s (slice 6.7)
When: NoShowTimeoutAutomation issues AbandonAsNoShow at T+300.05s
Then: command silently dropped (TRIP_ALREADY_TERMINATED)
```

**Policy change mid-flight (Bruun carry-the-value confirmed)**
```
Given: DriverArrivedAtPickup { tripId: X, expiresAt: T+300s }
       (computed at slice 6.3 from noShowTimeoutSeconds = 300)
  And: at T+60s, operator emits TripsPolicyConfigured { noShowTimeoutSeconds: 120 }
When: NoShowTimeoutAutomation scans at T+121s
Then: X is NOT treated as expired (its carried expiresAt is still T+300s)
  And: X expires at its originally-scheduled T+300s
Confirms: policy changes do not retroactively shift in-flight no-show deadlines.
```

#### Views affected

- `RidersAwaitingBoarding*` — row removed.
- `DriverTripView` — status: NoShow. Driver-app trip-mode terminates; driver returns to offer-mode.
- `TripTimeline` — adds `TripAbandonedAsNoShow`.
- `ActiveTripsByDriver` — trip removed (no longer active).
- `ActiveTripsByRider` — trip removed.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `RiderNoShowRate` | Rider Profile, Trust & Safety, ML ranking | Per-rider × time-window: count of no-show events | `TripAbandonedAsNoShow` | Defer — Rider Profile owns. |
| `DriverNoShowExposureMetrics` | Driver Profile, ops, Payments | Per-driver: count of trips abandoned as no-show through no fault of the driver; wait-time interval | `TripAbandonedAsNoShow`, `DriverArrivedAtPickup` | Defer but pin — driver-protection signal; Payments may compensate driver for wait time. |
| `NoShowGeographicHotspots` | Ops, product, Pricing (surge signals) | Per-region × hour: no-show concentration | `TripAbandonedAsNoShow` | Defer — supply/demand signal. |
| `NoShowSlippageAudit` (observability) | SRE | `noShowedAt - expiresAt` distribution; automation-health telemetry | `TripAbandonedAsNoShow` | Defer but pin — parallel to Workshop 001's `ExpirerSlippageAudit`. |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Automation Pattern, time-driven. Bruun notation: clock-rewind glyph + asterisk-suffix todo-list. |
| Event name | `TripAbandonedAsNoShow`. Mirrors Workshop 001's `RideRequestAbandoned` system-driven terminal naming. The `Abandoned`-prefix family is now the cross-BC convention for system-driven terminals. |
| Command name | `AbandonAsNoShow`. Mirrors Workshop 001 §5.9's `AbandonRideRequest`. |
| Implementation mechanism | Deferred to skill-file / implementation time. |
| Payload | Minimal — IDs + `expiresAt` + `noShowedAt`. Stream-bloat-avoidance per Workshop 001 §5.7. |
| Silent race-rejection | Confirmed. Mirrors Workshop 001 §5.7's discipline. |
| Automation cadence / SLA | No number locked; observability via `NoShowSlippageAudit`. |
| Cross-BC publication | Deferred to slices 6.9 (Translation-out to Dispatch) and 6.10 (downstream consumers). |

#### Notation notes

- `RidersAwaitingBoarding*` — asterisk suffix (Bruun convention; Workshop 002's first occurrence).
- `NoShowTimeoutAutomation` — clock-rewind glyph on the gear.
- §8 Temporal Automation cross-reference table populated with this slice. **First confirmation that Workshop 001's Bruun pattern is portable across BCs.**

#### Cross-references

- **Backward:** `DriverArrivedAtPickup` (slice 6.3) feeds `RidersAwaitingBoarding*`.
- **Removal feeders:** Slices 6.5 (TripStarted), 6.7 (rider cancels), 6.8 (driver cancels) all remove rows.
- **Forward (slice 6.9):** `TripAbandonedAsNoShow` contributes to outbound publication to Dispatch (slice 5.12 mirror).
- **Cross-BC consumers (deferred to slice 6.10):** Driver Profile, Rider Profile, Trust & Safety, Operations, Pricing — subscribe via ASB.
- **Consumed parameter:** `TripsPolicy.noShowTimeoutSeconds` — read at slice 6.3 and carried; not re-read here. **Bruun carry-the-value confirmed working.**
- **Temporal automation cross-ref (§8):** First entry.

### 6.5 Slice 5 — TripStarted (Command from driver-app)

**Pattern:** Command.
**Lane:** Trips.
**Trigger:** Driver-app — driver taps "Start trip" / "Begin ride" after rider has boarded.

#### Flow on the board

```
   ┌────────────────────────────────────┐
   │ Driver-app trip-mode UI            │
   │ Driver taps "Start trip" with      │
   │ rider in vehicle                   │
   └──────────────┬─────────────────────┘
                  │ gRPC unary
                  ▼
   ┌────────────────────────────────────┐
   │ ConfirmTripStart                   │  blue command
   └──────────────┬─────────────────────┘
                  │  [AggregateHandler] loads Trip
                  │  validates current status = AtPickup
                  ▼
   ┌────────────────────────────────────┐
   │ TripStarted                        │  orange event (Trip stream)
   └──────────────┬─────────────────────┘
                  │
                  ▼
   Views updated:
     • DriverTripView          (status: InProgress; navigation to dropoff)
     • TripTimeline            (async)
     • ActiveTripsByDriver     (async)
     • ActiveTripsByRider      (async; rider-app status)
     • RidersAwaitingBoarding* (row REMOVED — no-show timeout disarmed)
```

#### Command — `ConfirmTripStart`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | Authenticated driver context; must match Trip's `driverId`. |
| `startedAt` | timestamp | Server clock at command receipt. |

#### Event — `TripStarted`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | |
| `riderId` | opaque ID | Echoed for projection convenience and downstream Payments capture. |
| `tripStartedAt` | timestamp | Sets the aggregate's `tripStartedAt` timestamp. |

#### Aggregate transition

`AtPickup → InProgress`. Lifecycle-monotonicity enforced.

#### Race resolution

This slice is one of the three race partners against slice 6.4's `NoShowTimeoutAutomation`. If `ConfirmTripStart` commits before the no-show timeout fires, the row is removed from `RidersAwaitingBoarding*` and slice 6.4's handler will silently drop when it loads the aggregate and finds `InProgress` status. Workshop 001 §5.5's `OfferAccepted` vs. `OfferExpired` race has the same shape.

#### Rejection reasons

`TRIP_NOT_FOUND`, `TRIP_NOT_AT_PICKUP` (status not `AtPickup`), `TRIP_ALREADY_TERMINATED`, `NOT_THE_TRIPS_DRIVER`.

#### GWT sketches

**Happy path**
```
Given: Trip aggregate at status = AtPickup; RidersAwaitingBoarding* contains
       row { tripId: X, expiresAt: T+300s }
When: ConfirmTripStart { tripId: X, driverId: D1, startedAt: T+45s }
Then: TripStarted { tripId: X, driverId: D1, riderId, tripStartedAt: T+45s } is emitted
  And: aggregate status = InProgress
  And: RidersAwaitingBoarding* row removed
  And: DriverTripView updates inline (status: InProgress; navigation to dropoff)
  And: TripTimeline, ActiveTripsByDriver, ActiveTripsByRider updated async
```

**Race won against no-show timeout (just before T+300)**
```
Given: Trip at AtPickup; RidersAwaitingBoarding* row expiresAt = T+300s
When: ConfirmTripStart commits at T+299.5s
  And: NoShowTimeoutAutomation issues AbandonAsNoShow at T+300.05s
       (from a stale snapshot of RidersAwaitingBoarding*)
Then: TripStarted commits successfully (aggregate status = InProgress)
  And: AbandonAsNoShow handler loads aggregate, finds InProgress, silently drops
  And: trip continues normally; no-show terminal never fires.
```

**Driver tries to start without arriving (in EnRouteToPickup)**
```
Given: Trip in EnRouteToPickup state (slice 6.3 not yet emitted)
When: ConfirmTripStart { tripId: X, driverId: D1, startedAt: T }
Then: command is rejected with TRIP_NOT_AT_PICKUP
```

#### Views fed

- `DriverTripView` — status: InProgress; trip-mode UI shifts to dropoff navigation. Inline.
- `TripTimeline` — adds `TripStarted`. Async.
- `ActiveTripsByDriver` — status updated. Async.
- `ActiveTripsByRider` — rider-app shifts to "on your way to <destination>." Async.
- `RidersAwaitingBoarding*` — **row removed** (disarms slice 6.4's no-show timeout). Async.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `BoardingTimeDistribution` | Ops, product, ML | Per-trip: `arrivedAtPickupAt → tripStartedAt` interval (rider boarding lag) | `DriverArrivedAtPickup`, `TripStarted` | Defer but pin — operational efficiency signal. |
| `PaymentAuthAnchorEvents` | Payments | Identifies the moment payment capture should anchor to | `TripStarted` | Defer — Payments concern; pin to slice 6.10 publication mapping. |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Trigger source | Driver-app explicit tap. The "rider has boarded" signal is judgment-based; driver tap is canonical. |
| Pattern | Command (driver-app → Trips), gRPC unary. |
| Event name | `TripStarted`. Matches Lyft/Uber convention; concise; ride-sharing-domain natural. |
| `riderId` echoed on event | Yes — Payments capture downstream needs it without re-reading `TripMatched`. |
| Race against no-show timeout | First-to-commit wins via Marten optimistic concurrency. Slice 6.4's automation silently drops on race loss. |
| Idempotency | Aggregate-level via Marten optimistic concurrency. |
| Rejection reasons | `TRIP_NOT_FOUND`, `TRIP_NOT_AT_PICKUP`, `TRIP_ALREADY_TERMINATED`, `NOT_THE_TRIPS_DRIVER`. |

#### Cross-references and ripples

- **Backward:** Triggered by driver tap "Start trip" after rider boards.
- **Forward (slice 6.6):** `TripCompleted` is the natural terminal from `InProgress`.
- **Forward (slices 6.7, 6.8):** Cancellation pre-pickup is **not legal** during `InProgress` — workshop scope §2.3 defers mid-trip cancellation; lifecycle-monotonicity rejects it.
- **Race resolution:** Wins races against slice 6.4 via stream-version ordering.
- **Disarms slice 6.4:** Removes `RidersAwaitingBoarding*` row.

### 6.6 Slice 6 — TripCompleted (Command from driver-app, terminal)

**Pattern:** Command, with atomic cross-BC publications queued in the same handler (Wolverine `Events` + `OutgoingMessages`). Mirrors Workshop 001 §5.5.
**Lane:** Trips, with ASB publications crossing into Dispatch / Pricing / Payments / Ratings.
**Trigger:** Driver-app — driver taps "End trip" / "Complete" at dropoff.

#### Flow on the board

```
   ┌────────────────────────────────────┐
   │ Driver-app trip-mode UI            │
   │ Driver taps "End trip" at dropoff  │
   └──────────────┬─────────────────────┘
                  │ gRPC unary
                  ▼
   ┌────────────────────────────────────┐
   │ ConfirmTripCompletion              │  blue command
   └──────────────┬─────────────────────┘
                  │  [AggregateHandler] loads Trip
                  │  validates current status = InProgress
                  │  Wolverine: Events + OutgoingMessages atomic
                  ▼
   ┌────────────────────────────────────┐
   │ TripCompleted (local)              │  orange event (Trip stream)
   │ + outgoing ASB publications        │  outbox-coordinated
   │   (slice 6.9 to Dispatch,          │
   │    slice 6.10 fan-out)             │
   └────────────────────────────────────┘
```

#### Command — `ConfirmTripCompletion`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | Authenticated; must match Trip's driverId. |
| `completedAt` | timestamp | Server clock at command receipt. |

#### Event — `TripCompleted`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | |
| `riderId` | opaque ID | Echoed for projection convenience and downstream consumers. |
| `completedAt` | timestamp | Sets the aggregate's `completedAt`. |

Lean payload — full trip context already on `TripMatched`. Cross-BC publications (slices 6.9, 6.10) read from the loaded aggregate when constructing their payloads.

#### Aggregate transition

`InProgress → Completed`. Lifecycle-monotonicity enforced.

#### Atomic cross-BC publications (sketched here; full at slices 6.9, 6.10)

The same handler that emits `TripCompleted` queues outbound ASB publications via Wolverine's `OutgoingMessages` — outbox-coordinated with the local event append. **No intermediate automation; mirrors Workshop 001 §5.5's atomic dual-emit.** Slices 6.9 and 6.10 describe the contract surface of those publications; this slice flags that they exist atomically.

#### Rejection reasons

`TRIP_NOT_FOUND`, `TRIP_NOT_IN_PROGRESS` (status not `InProgress`), `TRIP_ALREADY_TERMINATED`, `NOT_THE_TRIPS_DRIVER`.

#### GWT sketches

**Happy path**
```
Given: Trip aggregate at status = InProgress with full TripMatched context loaded
When: ConfirmTripCompletion { tripId: X, driverId: D1, completedAt: T }
Then: TripCompleted { tripId: X, driverId: D1, riderId, completedAt: T } is emitted
  And: aggregate status = Completed
  And: outgoing ASB publications queued atomically (slices 6.9, 6.10 detail)
  And: DriverTripView updates inline (status: Completed)
  And: TripTimeline, ActiveTripsByDriver, ActiveTripsByRider updated async
```

**Driver tries to complete before starting**
```
Given: Trip in AtPickup state (rider hasn't boarded)
When: ConfirmTripCompletion { tripId: X, driverId: D1, completedAt: T }
Then: command is rejected with TRIP_NOT_IN_PROGRESS
```

#### Views fed

- `DriverTripView` — status: Completed. Trip-mode terminates; driver returns to offer-mode availability. Inline.
- `TripTimeline` — adds `TripCompleted`. Async.
- `ActiveTripsByDriver` — trip removed (no longer active). Async.
- `ActiveTripsByRider` — trip removed. Async.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `TripDurationDistribution` | Ops, product, Pricing | Per-trip: `tripStartedAt → completedAt` interval | `TripStarted`, `TripCompleted` | Defer but pin — core operational metric. |
| `DriverProductivityMetrics` | Driver Profile, ops | Per-driver per period: completed-trip count, hours active | `TripCompleted` + Driver Profile online events | Defer. |
| `RouteDeviationDetection` | Trust & Safety, ops | Compares planned vs. actual route (Telemetry-joined) | `TripStarted`, `TripCompleted`, Telemetry | Defer — requires Telemetry coupling. |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Trigger | Driver-app tap "End trip" at dropoff. |
| Atomic cross-BC publication | Yes — Wolverine `Events` + `OutgoingMessages` outbox-coordinated. Same pattern as Workshop 001 §5.5. |
| Event name | `TripCompleted`. Universal ride-sharing convention. |
| Local payload | Minimal — IDs + `completedAt`. Cross-BC publications read full payload from the loaded aggregate. |
| Rejection reasons | `TRIP_NOT_FOUND`, `TRIP_NOT_IN_PROGRESS`, `TRIP_ALREADY_TERMINATED`, `NOT_THE_TRIPS_DRIVER`. |

#### Cross-references and ripples

- **Backward:** Triggered by driver tap at dropoff.
- **Forward (slice 6.9):** Outbound Translation-out to Dispatch via slice 5.12 stub mirror.
- **Forward (slice 6.10):** Fan-out publication to Pricing / Payments / Ratings.
- **Cross-BC publication mechanism:** Atomic with local event append; mirrors Workshop 001 §5.5 with outbox coordination via Wolverine.

### 6.7 Slice 7 — TripCancelledByRider (Command from rider-app, terminal)

**Pattern:** Command, with atomic cross-BC publications queued via Wolverine `OutgoingMessages`. Mirrors Workshop 001 §5.8.
**Lane:** Trips, with ASB publications crossing into Dispatch / Pricing / Driver Profile / Operations.
**Trigger:** Rider-app — rider taps "Cancel ride" while trip is in `Matched`, `EnRouteToPickup`, or `AtPickup`.

#### Flow on the board

```
   ┌────────────────────────────────────┐
   │ Rider-app status screen            │
   │ Rider taps "Cancel" with reason    │
   └──────────────┬─────────────────────┘
                  │ gRPC unary
                  ▼
   ┌────────────────────────────────────┐
   │ CancelTripByRider                  │  blue command
   └──────────────┬─────────────────────┘
                  │  [AggregateHandler] loads Trip
                  │  validates current status is pre-pickup
                  │  Wolverine: Events + OutgoingMessages atomic
                  ▼
   ┌────────────────────────────────────┐
   │ TripCancelledByRider               │  orange event (Trip stream)
   │ + outgoing ASB publication         │  outbox-coordinated
   │   (slice 6.10 contract surface)    │
   └────────────────────────────────────┘
```

#### Command — `CancelTripByRider`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `riderId` | opaque ID | Authenticated; must match Trip's `riderId`. |
| `reason` | enum | Required (Trips-owned enum). |
| `notes` | string? | Required when `reason = OTHER`; optional otherwise. |
| `cancelledAt` | timestamp | Server clock. |

#### Event — `TripCancelledByRider`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `riderId` | opaque ID | |
| `driverId` | opaque ID | Echoed for downstream Driver Profile (driver compensation) and Operations. |
| `reason` | enum `{ CHANGED_MIND, WAITING_TOO_LONG, WRONG_PICKUP_LOCATION, FOUND_ALTERNATE_TRANSPORT, OTHER }` | Trips-owned. Overlaps semantically with Workshop 001's `RideRequestCancelled.reason` but is BC-owned per memory `feedback_bc_owned_enums.md`. |
| `notes` | string? | |
| `priorStatus` | enum | The status at cancellation time (`Matched` / `EnRouteToPickup` / `AtPickup`). Audit + analytics signal. |
| `cancelledAt` | timestamp | |

#### Aggregate transition

Any pre-pickup state (`Matched | EnRouteToPickup | AtPickup`) → `Cancelled`. Lifecycle-monotonicity enforced.

If prior state was `AtPickup`, the `RidersAwaitingBoarding*` row is removed (disarms slice 6.4's no-show timeout — third race partner alongside slice 6.5 and the timeout itself).

#### Cross-BC publication (atomic)

Topic: `trips.trip-cancelled-by-rider`. Consumers: Dispatch (slice 5.12 mirror), Pricing (cancellation-fee logic), Driver Profile (driver compensation), Operations. Full payload shape detailed at slice 6.10.

#### Rejection reasons

`TRIP_NOT_FOUND`, `TRIP_IN_PROGRESS_OR_LATER`, `TRIP_ALREADY_TERMINATED`, `NOT_THE_TRIPS_RIDER`, `NOTES_REQUIRED` (when `reason = OTHER` without notes).

#### GWT sketches

**Happy path — cancel during EnRouteToPickup**
```
Given: Trip aggregate at status = EnRouteToPickup
When: CancelTripByRider { tripId: X, riderId: R, reason: CHANGED_MIND, cancelledAt: T }
Then: TripCancelledByRider { tripId: X, riderId: R, driverId: D1,
                             reason: CHANGED_MIND, priorStatus: EnRouteToPickup,
                             cancelledAt: T } is emitted
  And: aggregate status = Cancelled
  And: outgoing ASB publication trips.trip-cancelled-by-rider queued atomically
  And: DriverTripView updates inline (status: Cancelled — driver returned to offer-mode)
```

**Cancel during AtPickup — disarms no-show**
```
Given: Trip at AtPickup; RidersAwaitingBoarding* row exists
When: CancelTripByRider { tripId: X, riderId: R, reason: WAITING_TOO_LONG, cancelledAt: T }
Then: TripCancelledByRider emitted; priorStatus: AtPickup
  And: RidersAwaitingBoarding* row removed (disarms slice 6.4)
```

**Mid-trip cancel rejected**
```
Given: Trip at status = InProgress
When: CancelTripByRider { tripId: X, riderId: R, reason: CHANGED_MIND, cancelledAt: T }
Then: command is rejected with TRIP_IN_PROGRESS_OR_LATER
  And: rider-app surfaces "trip in progress; contact driver to coordinate"
  Note: mid-trip cancellation is out of scope (§2.3); held as parking-lot.
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Command + atomic cross-BC publication. Mirrors Workshop 001 §5.8. |
| Reason enum | Trips-owned per `feedback_bc_owned_enums.md`. Distinct from Dispatch's pre-acceptance enum even where values overlap. |
| `priorStatus` echoed on event | Yes — captures which pre-pickup state the cancellation interrupted. Useful for analytics (cancel-during-en-route vs. cancel-during-at-pickup carry different operational implications). |
| Mid-trip cancellation | **Out of scope** per §2.3. Lifecycle-monotonicity rejects with `TRIP_IN_PROGRESS_OR_LATER`. |
| Notes required for `OTHER` | Yes — `NOTES_REQUIRED` rejection when missing. Mirrors Workshop 001 §5.8. |
| Cross-BC publication | Atomic with local commit; topic `trips.trip-cancelled-by-rider`; full shape at slice 6.10. |

#### Cross-references and ripples

- **Backward:** Triggered by rider tap "Cancel ride" while trip is pre-pickup.
- **Forward (slice 6.9, 6.10):** Outbound publication contract surface defined.
- **Disarms slice 6.4:** When prior status was `AtPickup`, `RidersAwaitingBoarding*` row removed.
- **Race partner:** Races against slice 6.4's `NoShowTimeoutAutomation` when prior status was `AtPickup`. First-to-commit wins; timeout silently drops on race loss.

### 6.8 Slice 8 — TripCancelledByDriver (Command from driver-app, terminal)

**Pattern:** Command, with atomic cross-BC publications queued via Wolverine `OutgoingMessages`. Structurally mirrors slice 6.7 with rider/driver inverted and reason enum differences.
**Lane:** Trips, with ASB publications crossing into Dispatch / Driver Profile / Trust & Safety (future) / Operations.
**Trigger:** Driver-app — driver taps "Cancel" while trip is in `Matched`, `EnRouteToPickup`, or `AtPickup`.

#### Flow on the board

Mirrors slice 6.7 with `Driver-app` and `CancelTripByDriver`/`TripCancelledByDriver` substituted.

#### Command — `CancelTripByDriver`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | Authenticated; must match Trip's `driverId`. |
| `reason` | enum | Required (Trips-owned enum). |
| `notes` | string? | Required when `reason = OTHER`; optional otherwise. |
| `cancelledAt` | timestamp | Server clock. |

#### Event — `TripCancelledByDriver`

| Field | Shape | Notes |
|---|---|---|
| `tripId` | opaque ID | |
| `driverId` | opaque ID | |
| `riderId` | opaque ID | Echoed for downstream Rider Profile and Operations. |
| `reason` | enum `{ CANNOT_REACH_PICKUP, RIDER_NOT_RESPONSIVE, VEHICLE_ISSUE, OTHER }` | Trips-owned. `SAFETY_CONCERN` deferred to Trust & Safety per Workshop 001 §5.6 precedent. |
| `notes` | string? | |
| `priorStatus` | enum | The status at cancellation time. |
| `cancelledAt` | timestamp | |

#### Aggregate transition

Same as slice 6.7: pre-pickup → `Cancelled`. Same `RidersAwaitingBoarding*` removal when prior was `AtPickup`.

#### Cross-BC publication (atomic)

Topic: `trips.trip-cancelled-by-driver`. Consumers: Dispatch (slice 5.12 mirror), Driver Profile (reliability scoring), Trust & Safety (future-pinned for pattern detection), Rider Profile (rider notification), Operations. Full payload shape detailed at slice 6.10.

#### Rejection reasons

Same as 6.7 with `NOT_THE_TRIPS_DRIVER` substituted for `NOT_THE_TRIPS_RIDER`.

#### GWT sketches

**Happy path — driver can't reach pickup (gridlock)**
```
Given: Trip at EnRouteToPickup
When: CancelTripByDriver { tripId: X, driverId: D1, reason: CANNOT_REACH_PICKUP,
                           notes: "blocked by accident on Main St", cancelledAt: T }
Then: TripCancelledByDriver { tripId: X, driverId: D1, riderId,
                               reason: CANNOT_REACH_PICKUP, notes,
                               priorStatus: EnRouteToPickup, cancelledAt: T } is emitted
  And: aggregate status = Cancelled
  And: outgoing ASB publication trips.trip-cancelled-by-driver queued atomically
```

**Driver cancel during AtPickup — rider not responsive (distinct from no-show)**
```
Given: Trip at AtPickup; RidersAwaitingBoarding* row exists
When: CancelTripByDriver { tripId: X, driverId: D1, reason: RIDER_NOT_RESPONSIVE,
                           cancelledAt: T+200s }
Then: TripCancelledByDriver emitted; priorStatus: AtPickup
  And: RidersAwaitingBoarding* row removed
  Note: distinct from slice 6.4's no-show terminal — no-show is system-driven
        after timeout; this is driver-initiated before timeout. Different events,
        different downstream-signal semantics.
```

#### Decisions locked in this slice

Mirror slice 6.7 with the following slice-specific differences:

| Decision | Resolution |
|---|---|
| Reason enum scope | `CANNOT_REACH_PICKUP`, `RIDER_NOT_RESPONSIVE`, `VEHICLE_ISSUE`, `OTHER`. `SAFETY_CONCERN` deferred to Trust & Safety per Workshop 001 §5.6 precedent. |
| Cross-BC publication | Topic `trips.trip-cancelled-by-driver`; consumer set differs from 6.7 (Driver Profile reliability scoring vs. Pricing cancellation-fee logic). |

#### Cross-references and ripples

- **Backward:** Triggered by driver tap "Cancel" while trip is pre-pickup.
- **Forward (slice 6.9, 6.10):** Outbound publication contract surface defined.
- **Disarms slice 6.4:** Same as 6.7.
- **Race partner:** Same as 6.7.
- **Distinct from slice 6.4:** Both events terminate at `AtPickup` state with `RidersAwaitingBoarding*` row removal, but the downstream-signal semantic differs (driver-initiated decision vs. system-driven timeout).

### 6.9 Slice 9 — Trip outcome translation-out to Dispatch (slice 5.12 mirror)

**Pattern:** Translation (out). Atomic cross-BC publications queued in slices 6.4, 6.6, 6.7, 6.8 land on ASB topics Dispatch subscribes to (Workshop 001 §5.12 stub mirror).
**Lane:** Trips outbound, ASB to Dispatch.
**Trigger:** Atomic with each terminal event commit in slices 6.4, 6.6, 6.7, 6.8.

#### Design decision: distinct outbound events vs. unified `TripTerminatedEarly` (override)

Workshop 001 §5.12 expressed a preference for a single `TripTerminatedEarly` event with reason enum. This workshop **overrides** that preference and publishes four distinct outbound events.

**Rationale:**
1. Mirrors Workshop 001's *own* pattern — Dispatch publishes distinct `RideRequestCancelled` (§5.8) and `RideRequestAbandoned` (§5.9), not a unified terminal-with-reason. Trips' design is consistent with that pattern.
2. ADR-candidate #8 topic convention (`<source-bc>.<event-name-kebab>`) works per-event-type. Per-topic subscriptions let consumers opt in to the signals they care about.
3. Dispatch's BC-owned `AssignmentOutcome` enum (per Workshop 001 §5.12's BC-owned-enum discipline) is mapped on *Dispatch's* side at translation-in time. Trips publishes its native vocabulary; Dispatch translates inbound.
4. Workshop 001 §5.12's preference was explicitly "preference, not constraint" — anticipated this override.

#### Outbound topics

| Source event (slice) | ASB topic | Wire format |
|---|---|---|
| `TripCompleted` (6.6) | `trips.trip-completed` | Protobuf (slice 6.10 names; defer authorship) |
| `TripCancelledByRider` (6.7) | `trips.trip-cancelled-by-rider` | Protobuf |
| `TripCancelledByDriver` (6.8) | `trips.trip-cancelled-by-driver` | Protobuf |
| `TripAbandonedAsNoShow` (6.4) | `trips.trip-abandoned-as-no-show` | Protobuf |

Session key on every topic: `tripId` (= `rideRequestId` per ADR-candidate #7). Strict per-trip ordering for downstream consumers.

#### Dispatch's slice 5.12 mapping (advisory; Dispatch's choice)

Dispatch subscribes via Wolverine handlers; each topic maps to Dispatch's BC-owned `AssignmentOutcome` enum. Two enum gaps surface in this mapping:

| Trips topic | Dispatch outcome (advisory) |
|---|---|
| `trips.trip-completed` | Workshop 001 §5.12's enum doesn't include a happy-path value. **Parking-lot:** Workshop 001 §5.12 enum needs `ASSIGNMENT_COMPLETED_NORMALLY`, OR happy path implicit (Dispatch only records non-success outcomes). |
| `trips.trip-cancelled-by-rider` | `RIDER_CANCELLED_POST_ASSIGNMENT` (already in Workshop 001 §5.12 enum). |
| `trips.trip-cancelled-by-driver` | `DRIVER_CANCELLED` (already in enum). |
| `trips.trip-abandoned-as-no-show` | Workshop 001 §5.12 enum has `DRIVER_NO_SHOW` (driver flaked) and `PICKUP_ABANDONED` (ambiguous), but no `RIDER_NO_SHOW`. **Parking-lot:** enum gap. Closest mapping is `PICKUP_ABANDONED`; cleanest fix is a new `RIDER_NO_SHOW` value in a Workshop 001 revision. |

#### ADR-candidate #8 — second-BC firing

Topic naming convention `<source-bc>.<event-name-kebab>` applied across four new topics. Two BCs now consistently apply the convention; ADR-candidate #8's trigger fires here. **Recommend lift to authored ADR** alongside #6 and #7 in follow-up authorship session.

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Outbound shape | Distinct events per terminal type. Override of Workshop 001 §5.12's preferred shape; rationale documented. |
| ASB topic naming | `<source-bc>.<event-name-kebab>` per ADR-candidate #8 (now fired across two BCs). |
| Session key | `tripId` (= `rideRequestId`). |
| Wire format | Protobuf per ADR-009; authorship deferred per §12.6 #4. |
| Outbox coordination | Wolverine outbox; atomic with local event commit (per slices 6.4, 6.6, 6.7, 6.8). |
| Dispatch enum mapping | Dispatch's concern at its slice 5.12 inbound handler. **Two parking-lot items:** Workshop 001 §5.12 enum needs `RIDER_NO_SHOW` (or analogous) and possibly `ASSIGNMENT_COMPLETED_NORMALLY`. |

#### Cross-references

- **Backward:** Each terminal slice (6.4, 6.6, 6.7, 6.8) atomically queues its outbound publication.
- **Forward (slice 6.10):** Full payload shapes / consumer fan-out for the four topics.
- **ADR-candidate #8:** Fires as second-BC firing. Recommend ADR authorship.
- **Parking-lot:** Workshop 001 §5.12 enum revision needed (add `RIDER_NO_SHOW`; possibly add `ASSIGNMENT_COMPLETED_NORMALLY`).

### 6.10 Slice 10 — Outbound publications fan-out (Pricing / Payments / Ratings / Operations)

**Pattern:** Translation (out, fan-out). Names the consumer set and per-topic payload shape; does not model consumer internals (per §2.3 scope).
**Lane:** Trips outbound, ASB to multiple BCs.
**Trigger:** Same as slice 6.9 — atomic with terminal event commits in slices 6.4, 6.6, 6.7, 6.8.

#### Per-topic contract surface (names only per §12.6 #4)

**`trips.trip-completed`**
- Consumers: Dispatch (slice 6.9), Pricing (fare finalization & capture trigger), Payments (capture authorization), Ratings (post-trip rating invitations), Operations (live-map archival), Driver Profile (productivity tracking).
- Payload (Protobuf, deferred authorship): `trip_id`, `rider_id`, `driver_id`, `pickup`, `dropoff`, `vehicle_class`, `fare_amount_minor_units`, `currency`, `fare_breakdown`, `pricing_policy_version`, `matched_at`, `trip_started_at`, `completed_at`. Denormalized — consumers don't need to call back into Trips.

**`trips.trip-cancelled-by-rider`**
- Consumers: Dispatch (slice 6.9), Pricing (cancellation-fee logic), Driver Profile (driver compensation for wasted time), Operations.
- Payload: `trip_id`, `rider_id`, `driver_id`, `reason`, `notes?`, `prior_status`, `matched_at`, `cancelled_at`. `prior_status` enables Pricing's fee logic to differentiate based on lifecycle stage.

**`trips.trip-cancelled-by-driver`**
- Consumers: Dispatch (slice 6.9), Driver Profile (reliability scoring), Trust & Safety (future-pinned), Rider Profile (rider notification), Operations.
- Payload: `trip_id`, `rider_id`, `driver_id`, `reason`, `notes?`, `prior_status`, `matched_at`, `cancelled_at`.

**`trips.trip-abandoned-as-no-show`**
- Consumers: Dispatch (slice 6.9), Driver Profile (driver compensation for wait time), Rider Profile (no-show tracking), Trust & Safety (pattern detection), Pricing (potential rider fee), Operations.
- Payload: `trip_id`, `rider_id`, `driver_id`, `arrived_at_pickup_at`, `expires_at`, `no_showed_at`, `matched_at`. Two-timestamp pattern preserved for slippage observability.

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Consumer fan-out | Per-topic subscription. Each event type is its own topic. |
| Payload denormalization | Consumers receive enough context to act without calling back into Trips. Mirrors Workshop 001 §5.10's denormalization principle. |
| Protobuf authorship | Deferred per §12.6 #4. Topic names + field-level payload sketch only. New PR for authorship per PR #4 precedent. Captured in §11 follow-ups. |
| `matched_at` echoed on all four topics | Enables downstream consumers to reason about original trip context regardless of which terminal landed. |

#### Cross-references

- **Backward (slices 6.4, 6.6, 6.7, 6.8):** Each terminal slice atomically queues its publication.
- **Forward (Protobuf authorship session):** New PR per PR #4 precedent. Adds `trips/v1/*.proto` files under `/protos/crittercab/trips/v1/`. Captured in §11 follow-ups.
- **Cross-BC consumers' workshops:** Pricing, Payments, Ratings, Driver Profile, Rider Profile, Trust & Safety, Operations — each consumer's internal handling is out of scope per §2.3.

### 6.11 Slice 11 — ConfigureTripsPolicy (configuration-as-events)

**Pattern:** Command Pattern, operator-initiated. Singleton aggregate. Mirrors Workshop 001 §5.11.
**Lane:** Trips.
**Trigger:** Operator admin console — operator adjusts Trips policy.

#### Aggregate — `TripsPolicy` (singleton for v1)

- **Stream ID:** well-known opaque constant (e.g., named key `"trips-policy"`). Same singleton pattern as Workshop 001 §5.11's `DispatchPolicy`.
- **State:** projection of the latest policy values.
- **Concurrency:** Marten optimistic concurrency; concurrent operator edits serialize.

#### Command — `ConfigureTripsPolicy`

| Field | Shape | Notes |
|---|---|---|
| `operatorId` | opaque ID | Authenticated upstream (API gateway + Operations BC). |
| `noShowTimeoutSeconds` | int (bounded 60–1800) | The only knob in v1. Default seeded via bootstrap (ADR-candidate #5). |
| `reason` | string? | Operator note explaining the change. |
| `configuredAt` | timestamp | |

#### Event — `TripsPolicyConfigured`

Payload mirrors the command exactly. **Full-replacement semantics** per Workshop 001 §5.11.

#### Bootstrap (ADR-candidate #5 fires)

Per ADR-candidate #5, on first deployment a seed event is appended with documented defaults:

```
TripsPolicyConfigured {
  operatorId: "system-bootstrap",
  noShowTimeoutSeconds: 300,        // 5 minutes
  reason: "Initial deployment defaults",
  configuredAt: <deployment timestamp>
}
```

Exact bootstrap mechanism (migration-time seed, startup self-seed, refuse-until-configured) is the substance of ADR-candidate #5's decision. **Trigger fires this slice; ADR authorship recommended in follow-up session.**

#### Cross-parameter validation

For v1, only one parameter — no cross-parameter constraints. Type-level bounds (60–1800 seconds) enforced via C# 14 records and Wolverine's `Validate()` per Workshop 001 §5.11. Cross-parameter constraints will emerge as parameters are added post-MVP.

#### Views fed

- **`TripsPolicy`** — current policy projection; consumed by slice 6.3 (no-show carry-the-value).
- **`TripsPolicyHistory`** — reverse-chronological timeline for audit (mirrors Workshop 001 §5.11's `PolicyHistory`).

#### GWT sketches

**Happy path**
```
Given: prior TripsPolicyConfigured (or bootstrap seed) exists
When: ConfigureTripsPolicy { operatorId: OP1, noShowTimeoutSeconds: 600,
                              reason: "Tighten during low supply" }
Then: TripsPolicyConfigured { ..., noShowTimeoutSeconds: 600 } is emitted
  And: TripsPolicy view updates
  And: in-flight no-show timers (with carried expiresAt from prior policy)
       retain their originally-scheduled expiry — Bruun carry-the-value confirmed
       (slice 6.3 already locked this; slice 6.4 already validated it in GWT).
```

**Out-of-bounds rejection**
```
Given: any prior policy state
When: ConfigureTripsPolicy { noShowTimeoutSeconds: 30 }  (below lower bound 60)
Then: command rejected with OUT_OF_RANGE (type-level)
```

**Concurrent edit race**
```
Given: two operators editing simultaneously
When: both submit ConfigureTripsPolicy
Then: first to commit wins the TripsPolicy stream version
  And: the second receives a clean rejection (POLICY_CHANGED_CONCURRENTLY).
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Aggregate model for v1 | Singleton (one well-known stream). Regional-singleton is post-MVP. |
| Command semantics | Full replacement — every command carries all parameters. |
| Parameter set for v1 | `noShowTimeoutSeconds` only. Other parameters deferred per §11. |
| Bootstrap mechanism | Deferred to ADR-candidate #5. **Trigger fires here as second-BC adoption of configuration-as-events.** |
| Authentication / authorization | Out of Trips. API gateway + Operations BC upstream. |
| Cross-BC publication | None. `TripsPolicyConfigured` is internal to Trips. |

#### Cross-references

- **Consumed by:** Slice 6.3 (`noShowTimeoutSeconds` read at handler-fire time and carried via Bruun pattern).
- **ADR-candidate #5:** **Fires this slice.** Recommendation: lift to authored ADR alongside #6, #7, #8.
- **§9 Configuration-as-Events cross-reference:** Populated.

### 6.12 Slice 12 — DriverTripView enrichment from Identity events (Translation-in + View)

**Pattern:** Translation-in (from Identity) + View (projection updates). **No local Klefter event** — pure enrichment propagation, not a decision. First non-Klefter Translation-in slice in CritterCab.
**Lane:** Trips for projection updates; ASB inbound from Identity.
**Trigger:** ASB messages from Identity on topics `identity.rider-registered` and `identity.rider-profile-updated`.

#### Forward-constraint #2 fully honored (option 2c)

This slice fully addresses forward-constraint #2 ("Trips surfaces rider name to driver post-acceptance") with option 2c (eventually-consistent enrichment via Identity event subscription).

#### Forward-constraints on Identity's eventual workshop

This slice presupposes Identity publishes:
- `RiderRegistered` — new rider account; carries `riderId`, initial `displayName`.
- `RiderProfileUpdated` — display-name (or other surfaceable fields) changes.

These become forward-constraints on Identity's workshop, the same way narrative-002's authorial calls forward-constrained this workshop. **Workshops forward-constraining other workshops is a new pattern this slice surfaces** — methodology log entry 004 candidate territory.

#### Architecture: snapshot + live-update

```
   ┌────────────────────────────┐
   │ Identity BC (offstage)     │
   │  [Identity workshop]       │
   └──────────────┬─────────────┘
                  │ ASB
                  │ identity.rider-registered
                  │ identity.rider-profile-updated
                  ▼
   ┌────────────────────────────┐
   │ Trips Translation-in       │
   │ (Wolverine handler)        │
   └──────┬─────────────────────┘
          │ updates TWO projections
          ▼
   ┌──────────────────────┐    ┌──────────────────────────────┐
   │ RiderProfileSnapshot │    │ DriverTripView (slice 6.1)   │
   │ Per-rider snapshot   │    │ For active trips of riderId  │
   │ {riderId,            │    │ Update riderDisplayName      │
   │  displayName,        │    │                              │
   │  lastUpdatedAt}      │    └──────────────────────────────┘
   └──────┬───────────────┘
          │ read at slice 6.1's
          │ TripMatched projection-update
          │ (slice-6.1 amendment)
          ▼
   slice 6.1's DriverTripView update
   reads RiderProfileSnapshot[riderId];
   populates riderDisplayName if present.
```

#### Slice 6.1 amendment (recorded here, applies to §6.1)

> On `TripMatched`, the `DriverTripView` projection update reads `RiderProfileSnapshot[riderId]`. If present, populates `riderDisplayName` from the snapshot. If absent (rider not yet registered with Identity, or Identity event hasn't arrived yet), commits null. Slice 6.12 catches the null case when the Identity event arrives.

This amendment is forward-only — introduced here, in slice 6.12, because `RiderProfileSnapshot` doesn't exist until this slice introduces it. Slice §6.1's `DriverTripView` description should be re-read with this amendment. **Captured in §13 as part of forward-constraint #2's full disposition.**

#### Handler behavior

For each inbound Identity event:

1. **Update `RiderProfileSnapshot`** with the new `displayName`, indexed by `riderId`. `lastUpdatedAt`-based conflict resolution: incoming with older timestamp is ignored.
2. **Look up active trips** for this `riderId` in `ActiveTripsByRider`.
3. **For each active trip**, update the `DriverTripView` row's `riderDisplayName` field.
4. **No local Trips event emitted** (projection-update only).

#### Idempotency

Handler is idempotent on `(eventId, riderId)`. At-least-once redelivery results in projection-overwrites (last-write-wins by `lastUpdatedAt`) which is harmless.

#### Race resolution

| Race | Resolution |
|---|---|
| Identity event arrives before `TripMatched` (common case for new riders) | `RiderProfileSnapshot` populated; `ActiveTripsByRider` empty for this rider; no `DriverTripView` updates yet. Snapshot persists; slice 6.1's amendment reads it on later `TripMatched`. **Race covered.** |
| Identity event arrives after `TripMatched` (mid-trip rename — rare) | `ActiveTripsByRider` contains the active trip; `DriverTripView` updated; driver-app pushes new name. **Race covered.** |
| `TripMatched` fires before any Identity event for the rider | Snapshot empty; `riderDisplayName: null` committed. Slice 6.12 catches it when Identity event eventually arrives. **Race covered.** |

#### Forward-constraint #3 timing budget interaction

This slice's projection-update is **async** (Marten daemon) — not on the latency-critical driver-app trip-mode-load path. Slice 6.1's *inline* projection-update is what matters for "subjectively instantaneous" trip-mode transition.

For the common case (rider registered before booking → snapshot populated), slice 6.1's inline read of the snapshot includes the name immediately; driver-app sees the rider name on first render. For edge cases, driver-app sees null/placeholder until slice 6.12's async update catches up. Both paths are acceptable per option 2c.

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Translation-in + View. **No local Trips event** (no Klefter decision). First non-Klefter Translation-in in CritterCab. |
| Identity events subscribed | `identity.rider-registered`, `identity.rider-profile-updated`. Forward-constraint on Identity workshop. |
| Projections maintained | `RiderProfileSnapshot` (per-rider snapshot) + updates to `DriverTripView` (active trips). |
| Slice 6.1 amendment | `DriverTripView` projection-update reads `RiderProfileSnapshot[riderId]` and populates `riderDisplayName` if present. |
| Handler lifecycle | Async (Marten daemon). Not on latency-critical path. |
| Conflict resolution on snapshot | `lastUpdatedAt`-based: incoming with older timestamp is ignored. |
| Idempotency | `(eventId, riderId)`-keyed; redelivery harmless. |
| Forward-constraint #2 | **Fully honored** — option 2c (eventually-consistent enrichment from Identity). |

#### Cross-references

- **Backward (forward-constraint on Identity):** Identity must publish `RiderRegistered` and `RiderProfileUpdated`; this is a forward-constraint on Identity's workshop. **First instance of workshop forward-constraining workshop** — methodology log entry 004 candidate.
- **Slice 6.1 amendment:** `DriverTripView` projection reads `RiderProfileSnapshot`. §13 captures the full forward-constraint #2 disposition.
- **§7 Translation Slices:** New row for slice 6.12 (in, non-Klefter — pure enrichment).
- **Forward-constraint #2:** **Fully honored.**
- **Forward-constraint #3:** No change (slice 6.1's inline-projection mechanism unchanged; this slice operates async on the non-critical path).

*(Slice walk complete. Twelve slices, all committed.)*

---

## 7. Translation Slices at BC Boundaries (cross-reference)

Separated out because they carry contract implications beyond Trips' internal behavior. Each translation is either *in* (we consume something external and produce a local event — Klefter decision-event pattern applies where a decision is made) or *out* (we publish an event consumed externally).

| Slice | Direction | Counterparty BC | Translation event (local) | Klefter decision? |
|---|---|---|---|---|
| 6.1 | in | Dispatch (via ASB) | `TripMatched` | Yes — the act of accepting responsibility for the trip is a decision worth keeping locally; decoupled from Dispatch's `RideAssigned` retention/vocabulary. Foundation for every projection Trips builds. |
| 6.6 | out | Dispatch / Pricing / Payments / Ratings (via ASB) | `TripCompleted` | Yes — the trip's terminal success is a decision worth keeping locally; full contract surface defined at slice 6.10. Atomic with the local commit per Workshop 001 §5.5 pattern. |
| 6.7 | out | Dispatch / Pricing / Driver Profile / Operations (via ASB) | `TripCancelledByRider` | Yes — rider-initiated cancellation is a decision worth keeping locally; full contract surface defined at slice 6.10. |
| 6.8 | out | Dispatch / Driver Profile / Trust & Safety (future) / Operations (via ASB) | `TripCancelledByDriver` | Yes — driver-initiated cancellation is a decision worth keeping locally; full contract surface defined at slice 6.10. |
| 6.12 | in | Identity (via ASB) | (none — projection updates only) | **No** — first non-Klefter Translation-in slice in CritterCab. Pure enrichment propagation; Identity's data flows to Trips' `RiderProfileSnapshot` and `DriverTripView` projections without a local decision-event. |

---

## 8. Temporal Automation Slices (cross-reference)

Separated out because they use the Bruun notation convention: clock-rewind glyph on the automation sticky, asterisk suffix on the todo-list read model it consumes.

| Slice | Todo-list view | Automation | Terminal event |
|---|---|---|---|
| fed by 6.3, consumed by 6.4 | `RidersAwaitingBoarding*` | `NoShowTimeoutAutomation` (slice 6.4, clock-rewind glyph) | `TripAbandonedAsNoShow` (slice 6.4) |

---

## 9. Configuration-as-Events (cross-reference)

Slices in which operator configuration is captured as event-sourced policy rather than a settings table. Each row names the configuration event, the consumers that join it into read models, and the parameters it carries.

| Config event | Consumers | Parameters (v1) |
|---|---|---|
| `TripsPolicyConfigured` | Slice 6.3 (`noShowTimeoutSeconds` carry-the-value) | `noShowTimeoutSeconds` |

Workshop 001 established the pattern via `DispatchPolicyConfigured`; this workshop applies it to `TripsPolicyConfigured`. Two-BC consistency fires ADR-candidate #5 (configuration-as-events bootstrap strategy).

---

## 10. Candidate Protobuf Contract Surface

Names only, per ADR-009 and Workshop 001 §12.6 #4. Proto-file authorship is a downstream task captured in §11 follow-ups.

**Inbound (already authored, treated as input):**

| Contract | Direction | Topic / RPC | Notes |
|---|---|---|---|
| `RideAssigned` | Dispatch → Trips | ASB topic `dispatch.ride-assigned`, session-ordered by `ride_request_id` | Already authored at `/protos/crittercab/dispatch/v1/ride_assigned.proto` (PR #4). Slice 6.1 input. |

**Outbound (new, deferred authorship):**

| Contract | Direction | Topic | Notes |
|---|---|---|---|
| `TripCompleted` | Trips → Dispatch / Pricing / Payments / Ratings / Driver Profile / Operations | ASB topic `trips.trip-completed`, session-ordered by `trip_id` | Slice 6.6 / 6.10. Full payload sketch in slice 6.10. |
| `TripCancelledByRider` | Trips → Dispatch / Pricing / Driver Profile / Operations | ASB topic `trips.trip-cancelled-by-rider`, session-ordered by `trip_id` | Slice 6.7 / 6.10. |
| `TripCancelledByDriver` | Trips → Dispatch / Driver Profile / Trust & Safety / Rider Profile / Operations | ASB topic `trips.trip-cancelled-by-driver`, session-ordered by `trip_id` | Slice 6.8 / 6.10. |
| `TripAbandonedAsNoShow` | Trips → Dispatch / Driver Profile / Rider Profile / Trust & Safety / Pricing / Operations | ASB topic `trips.trip-abandoned-as-no-show`, session-ordered by `trip_id` | Slice 6.4 / 6.10. |

All outbound topics follow the `<source-bc>.<event-name-kebab>` convention per ADR-candidate #8 (fired this workshop at slice 6.9). Files will live under `/protos/crittercab/trips/v1/` once authored. Authorship session per PR #4 precedent; captured in §11 follow-ups.

---

## 11. Parking Lot and Open Questions

Items accumulated during the slice walk. Each entry carries a disposition (defer / ADR-candidate / post-MVP / drop) named when it lands.

1. **Workshop 001 §5.12 enum gap — `RIDER_NO_SHOW`.** ~~Workshop 001's preferred `TerminationReason` enum has `DRIVER_NO_SHOW` and `PICKUP_ABANDONED` but no value for rider no-show specifically.~~ **Resolved 2026-05-09** in this same session (PR for Workshop 002): `RIDER_NO_SHOW` added to both `TerminationReason` (Trips-side) and `outcome` (Dispatch-local) enums per [Workshop 001 §5.12 amendment subsection](./001-dispatch-event-model.md#workshop-002-update-2026-05-09).
2. **Workshop 001 §5.12 enum gap — `ASSIGNMENT_COMPLETED_NORMALLY`.** ~~Workshop 001's enum is silent on the happy-path completion case.~~ **Resolved 2026-05-09** in the same amendment: deliberately *not* added; §5.12's original framing was "post-assignment **early termination** signal" — happy path is implicit (Dispatch records `AssignmentOutcomeRecorded` only for non-success terminals). Implicit-happy-path scoping decision now explicit in the §5.12 amendment.
3. **Mid-trip cancellation paths.** Out of this workshop's scope per §2.3. Has its own compensation/driver-protection surface (rider-cancel, driver-cancel, emergency-cancel). **Disposition:** dedicated follow-up workshop.
4. **In-trip route deviations.** Rider asks for stop, driver takes wrong turn. **Disposition:** post-MVP.
5. **Payment-authorization failures at trip start.** Payments BC owns the authorization flow. **Disposition:** Payments workshop concern; not Trips'.
6. **Geofence auto-detection of pickup arrival.** Slice 6.3's trigger is driver explicit tap; geofence auto-detect could assist. **Disposition:** post-MVP UX enhancement; no event-shape impact.
7. **Trip-time fare adjustments.** v1 fare is locked at intake (per Workshop 001). Future enhancement may allow trip-time adjustments. **Disposition:** post-MVP; no event-shape impact (fields already on `TripMatched`).
8. **Telemetry-coupled signals on Trips.** Several candidate projections (e.g., `DriverArrivalAccuracy`, `RouteDeviationDetection`) are deferred pending Telemetry coupling. **Disposition:** revisit when Telemetry workshop is run.

---

## 12. ADR Candidates Surfaced by This Workshop

Pre-seeded with candidates inheriting from Workshop 001 §11 (re-listed at session close with post-workshop status) plus any new candidates surfaced during the walk. Each candidate is a one-line statement of what would be decided, not the decision itself.

**New candidates from this workshop:**

1. **Driver-app projection timing budget.** *Authored as [ADR-015](../decisions/015-driver-app-projection-timing-budget.md) on 2026-05-10.* A non-functional cross-cutting requirement: the driver-app trip-mode transition (from offer-mode to trip-mode following Dispatch's `OfferAccepted` → Trips' `TripMatched`) must complete inside a "subjectively instantaneous" budget. Achievable with current tooling (inline projection + ASB low-latency mode + gRPC server-streaming push). Worth documenting as an ADR so the budget becomes measurable (via `MatchingLatencyMetrics` projection per slice 6.1) and survives non-functional pressure changes. **Trigger fired at slice 6.1**; defer authorship per Workshop 001 §12.6 #4.

**Inherited from Workshop 001 §11 (status updated at session close):**

- ADR-candidate #5 — Configuration-as-events bootstrap strategy. **Fired this workshop (slice 6.11).** Trips' `TripsPolicyConfigured` is the second BC adoption; pattern confirmed across two BCs. **Authored as [ADR-011](../decisions/011-configuration-as-events-bootstrap.md) on 2026-05-10.**
- ADR-candidate #6 — Aggregate-per-invariant pattern. **Fired this workshop (§3 sidebar).** **Authored as [ADR-012](../decisions/012-aggregate-per-invariant.md) on 2026-05-10.**
- ADR-candidate #7 — Shared ride identifier across BCs. **Fired this workshop (§3 + slice 6.1).** **Authored as [ADR-013](../decisions/013-shared-cross-bc-identifier.md) on 2026-05-10.**
- ADR-candidate #8 — ASB topic naming convention. **Fired this workshop (slice 6.9).** Two BCs now consistently apply the `<source-bc>.<event-name-kebab>` convention. **Authored as [ADR-014](../decisions/014-asb-topic-naming-convention.md) on 2026-05-10.**

---

## 13. Forward-Constraints Handled

Per methodology log entry 003 (two-layer fidelity convention), narratives 001 and 002 captured authorial-call assumptions about Trips that this workshop must honor or override with documented reasoning. Three forward-constraints land in this workshop; their dispositions are captured here as they're addressed and consolidated at session close.

| # | Forward-constraint | Source | Disposition | Where addressed |
|---|---|---|---|---|
| 1 | Trips' intake is idempotent on `rideRequestId` (= `tripId`). | Narrative 001 line 151; narrative 002 line 174 | **Honored.** Stream-existence check on Marten; at-least-once redelivery → silent ack. | Slice 6.1 |
| 2 | Trips surfaces rider name to driver post-acceptance. | Narrative 002, Moment 2 second `Response.` paragraph | **Honored** (option 2c). Slice 6.12's `RiderProfileSnapshot` projection (subscribed to Identity events) is read at slice 6.1's `DriverTripView` projection-update time; rider name surfaces immediately when snapshot is populated, falls back to null on edge cases (eventually-consistent). Slice 6.1 amended to read snapshot. Forward-constraint passed to Identity workshop: must publish `RiderRegistered` and `RiderProfileUpdated`. | Slice 6.1 + 6.12 |
| 3 | Trips' projection drives the driver-app transition from offer-mode to trip-mode. | Narrative 002, Moment 2 implications | **Honored.** Projection mechanism locked (inline `DriverTripView` update on `TripMatched` — slice 6.1); timing budget named ("subjectively instantaneous"); new ADR candidate captured in §12 ("Driver-app projection timing budget") with `MatchingLatencyMetrics` projection pinned for SLO measurability. Explicit ADR authorship deferred per §12.6 #4. | Slice 6.1 |

---

## 14. Retrospective

Mirrors Workshop 001 §12's nine-subsection convention. Section numbering reflects this workshop's overall structure (additional §3 Aggregate Identity and §13 Forward-Constraints sections); the nine-subsection retrospective shape is preserved.

### 14.1 Workshop intent vs. outcome

Stated goal at session start: produce a structurally parallel artifact for Trips to Workshop 001's Dispatch artifact, applying §12.6 methodology adjustments, creating second canonical data points for ADR candidates #5/#6/#7/#8, and testing the two-layer fidelity convention from methodology log entry 003.

**Outcome:** Workshop produced a complete event model for Trips covering 12 slices — 9 events plus alternates, 1 temporal automation (Bruun pattern reuse from Workshop 001 §5.7), 1 configuration-as-events slice, 5 translation slices across 2 BC boundaries (Dispatch in/out, Identity in), 1 new ADR candidate plus four Workshop 001 candidates fired (#5, #6, #7, #8), 8 parking-lot items, full Protobuf contract surface (4 new outbound topics named, deferred authorship). All three forward-constraints from narratives 001 + 002 honored or partially honored with documented disposition. **Goal met.**

### 14.2 What worked

- **Aggregate-identity sidebar (§3) applied per Workshop 001 §12.6 #2.** Resolved aggregate-per-invariant decision before slice walking; provided the second canonical data point for ADR-candidate #6 with explicit compare-and-contrast against Workshop 001's `RideRequest` + `Offer` precedent.
- **Mid-sidebar research grounding for `Matched` vocabulary.** Web search of Uber/Lyft/system-design-literature canonical naming caught a real vocabulary issue (`Intaken` was awkward; `Confirmed` was OK; `Matched` was industry-canonical). Workshop 001 §12.6 didn't anticipate this pattern; methodology log entry 004 candidate.
- **§12.6 #3 cadence calibration applied.** Heavy slices (6.1, 6.4, 6.9, 6.12) got extended discussion; light slices walked briskly. Paired walks for structurally-similar slices (6.7+6.8, 6.9+6.10) demonstrated the calibration in action and shipped two slice-pairs in single user-turns each.
- **§12.6 #6 narrative-pairing rule applied.** Slice 6.4 (no-show Bruun) walked immediately after slice 6.3 (its feeder). Read-flow `feeder → temporal slice → terminal event` was natural.
- **Bruun pattern reuse (§5.7 → 6.4).** Workshop 001's investment in establishing the pattern paid off — slice 6.4 was structurally near-identical to §5.7, race-condition surface mapped 1:1, and notation conventions applied verbatim. **First confirmation that the Bruun pattern is portable across BCs.**
- **Override of Workshop 001 §5.12 with documented rationale.** Slice 6.9's "publish four distinct outbound events" override was explicit, well-grounded in Workshop 001's *own* pattern (distinct events for distinct semantics in §5.8/§5.9), and surfaced two enum-gap parking-lot items for Workshop 001 revision.
- **All three forward-constraints from narratives 001+002 fully addressed.** First concrete confirm artifact for methodology log entry 003.
- **Slice 6.1 amendment via slice 6.12 cross-reference.** Forward-only amendment (slice 6.1 reads `RiderProfileSnapshot` from slice 6.12's projection) without retroactively editing §6.1's text. Demonstrates that workshops can self-amend mid-walk via cross-references rather than retroactive edits.
- **Non-Klefter Translation-in identified (slice 6.12).** First counter-example to "all translations are Klefter decision-events"; defends against drift toward over-applying Klefter pattern. Klefter's Post 3 figure has a left-hand-side and a right-hand-side; this slice is the left-hand-side case made explicit.
- **Atomic dual-emit pattern reuse from Workshop 001 §5.5.** Slice 6.6/6.7/6.8's `Events + OutgoingMessages` outbox-coordinated commits applied verbatim. Pattern is portable.

### 14.3 What was hard / friction

- **First-state vocabulary required two iterations.** `Intaken` → `Confirmed` → `Matched`. Mid-sidebar research turn was needed for industry-grounded vocabulary; convention-application alone wasn't sufficient. Workshop 001 §12.6's pre-walk sidebar could be expanded to include "vocabulary scan against industry conventions and prior workshop overload."
- **Slice 6.2's trigger source decision was unobvious.** Real ride-sharing apps don't have a discrete "I started navigating" event; finding the right framing (driver-app implicit on trip-mode load) required explicit decision rather than convention-application. Three options surfaced (driver-app implicit, driver tap, Telemetry-detected); user-driven choice resolved.
- **Workshop 001 §5.12 override surfaced enum gaps.** The override was clean but exposed `RIDER_NO_SHOW` and `ASSIGNMENT_COMPLETED_NORMALLY` gaps in Workshop 001's preferred enum. Means a Workshop 001 revision is now load-bearing — a workshop produced forward-constraints back on a prior workshop, which methodology log entry 003 didn't explicitly anticipate.
- **Slice 6.12's race-condition design needed depth.** The naive "subscribe-and-update" handler had a real bug (Identity events arriving before `TripMatched` would be lost). Required `RiderProfileSnapshot` + slice 6.1 amendment. Heavy slice; cadence calibration paid off but design depth was substantial.
- **§5/§6 numbering mistake at file-creation time.** Initial workshop file had §5 and §6 both labeled "Slice Walk" (duplicate). Caught and corrected. Lesson: structural parallelism with Workshop 001 needs validation at file-creation time, not after content lands.

### 14.4 Decisions about how to model (meta-decisions worth carrying forward)

- Slices commit only after explicit sign-off; no speculative artifact content. (Re-confirmed from Workshop 001.)
- Each slice closes with a "decisions locked" table. (Re-confirmed.)
- Cross-cutting concerns tracked in dedicated cross-reference tables. (Re-confirmed.)
- ADR candidates captured during modeling. (Re-confirmed.)
- Parking lot has dispositions named. (Re-confirmed.)
- **NEW:** Forward-only slice amendments (slice 6.12 amends slice 6.1 via cross-reference) work. Documented in the amending slice; original slice's text not retroactively edited. Reader cross-references; the workshop file is not a wiki.
- **NEW:** Paired walks for structurally-similar slices (6.7+6.8, 6.9+6.10) accelerate the cadence without losing per-slice rigor. Each slice still gets its own decisions table, GWT, cross-references; the *walk* is paired, not the artifact.
- **NEW:** Mid-sidebar research grounding (§3 first-state vocabulary, web search for ride-sharing API canonical naming) is a legitimate move when the team's vocabulary needs grounding against industry conventions. Workshop 001 §12.6 #2 didn't explicitly anticipate this.
- **NEW:** Override of a prior workshop's preference is a legitimate workshop output, not a deviation. Slice 6.9's override of Workshop 001 §5.12 was deliberate and rationale-documented. Workshop 001 §5.12 explicitly anticipated the override possibility ("preference, not constraint").

### 14.5 Patterns established for future workshops

Reusable assets this workshop produces or confirms:

- **Aggregate-per-invariant pattern confirmed across two BCs.** ADR-candidate #6 ready for authoring with two structurally-consistent data points.
- **Shared-identifier principle confirmed across two BCs (slice 6.1 + 6.9 round-trip).** ADR-candidate #7 ready for authoring.
- **ASB topic naming convention confirmed across two BCs.** ADR-candidate #8 ready for authoring.
- **Configuration-as-events confirmed across two BCs.** ADR-candidate #5 ready for authoring (with the bootstrap-strategy decision included).
- **Bruun temporal-automation pattern is portable across BCs.** Slice 6.4 reused Workshop 001 §5.7's design verbatim.
- **Distinct events for distinct semantics, even when terminal.** Workshop 001 §5.8/§5.9 set the precedent; Trips' four-distinct-terminals slice 6.9 confirms.
- **Carry-the-value (Bruun) for any value derived from policy.** Reused at slice 6.3 → 6.4 transition.
- **Atomic dual-emit for terminals + cross-BC publications.** Slices 6.6/6.7/6.8 follow Workshop 001 §5.5 pattern.
- **Forward-only slice amendments via cross-reference.** Slice 6.12 amends slice 6.1.
- **Non-Klefter Translation-in (slice 6.12).** Counter-example explicit.
- **Workshop overrides Workshop with documented rationale.** Slice 6.9 override.
- **Workshops forward-constraint other workshops** (slice 6.12 → Identity workshop). Methodology log entry 004 captures this.

### 14.6 Adjustments for the next BC workshop

- **Pre-walk vocabulary scan.** Identify any Workshop 001 vocabulary that might overload the new BC's vocabulary (`Accepted` was a candidate to avoid). Workshop 001 §12.6 #2's "pre-workshop sidebar on aggregate identity" could be expanded to include "vocabulary scan against prior workshops + industry conventions." Adjustment for next workshop: do this scan as part of the sidebar.
- **Add §13b Forward-Constraints Generated section.** This workshop's §13 captures *inbound* forward-constraints (from narratives 001+002). Slice 6.12 surfaced *outbound* forward-constraints (on Identity's eventual workshop) but didn't consolidate them. Future workshops should have an explicit section consolidating outbound constraints on un-modeled BCs' workshops.
- **Paired walks where structurally similar.** 6.7+6.8 and 6.9+6.10 worked well. Future workshops with parallel slices can do the same.
- **§12.6 adjustments continue.** All five Workshop 001 §12.6 adjustments applied successfully here; carry forward.
- **Mid-walk forward-amendments via cross-reference work.** Continue this pattern rather than retroactively editing prior slices.

### 14.7 Quality signal from the session

User feedback positive throughout. Three rounds of vocabulary refinement on the first state (`Intaken` → `Confirmed` → `Matched`) demonstrated user-driven naming work paying off; user explicitly asked for ride-sharing app examples mid-sidebar to ground the vocabulary in industry conventions. Sign-offs landed cleanly at every gate (sidebar, scope, twelve slices). The override of Workshop 001 §5.12 was approved with deliberate rationale.

Calibration captured: no new memories warranted. All applicable feedback (Critter Stack primitives, BC-owned enums, Decider Pattern, depth/UL/leans, explicit deferrals, validation at HTTP boundary, static endpoints, prune textureless detail, keep READMEs current) already in `MEMORY.md` from Workshop 001 + narratives sessions; all reused successfully here without needing reinforcement.

### 14.8 Follow-ups generated

- **5 ADR candidates** ready for follow-up authorship session: #5 (config-as-events bootstrap), #6 (aggregate-per-invariant), #7 (shared identifier), #8 (ASB topic naming), and the new **driver-app projection timing budget** candidate (slice 6.1).
- **Workshop 001 §5.12 revision** — ~~add `RIDER_NO_SHOW` to preferred `TerminationReason` enum; possibly add `ASSIGNMENT_COMPLETED_NORMALLY`~~. **Resolved 2026-05-09** in this same session. Both enum-gap parking-lot items closed; W001 v0.3 amendment subsection added to §5.12 acknowledging the override and documenting both enum decisions (`RIDER_NO_SHOW` added; `ASSIGNMENT_COMPLETED_NORMALLY` deliberately not added per scoping decision).
- **Trips business-event Protobuf authorship session** — author 4 new outbound topic protos under `/protos/crittercab/trips/v1/` per PR #4 precedent.
- **Identity workshop forward-constraints** — Identity must publish `RiderRegistered` and `RiderProfileUpdated` business events. New cross-workshop forward-constraint pattern; methodology log entry 004 captures this.
- **Mid-trip cancellation paths workshop** — held as parking-lot; deferred to a dedicated follow-up.
- **8 parking-lot items** for various BC concerns (mid-trip cancel, route deviations, payment-auth failures, geofence detection, trip-time fare adjustments, Telemetry coupling, etc.) — see §11.
- **Methodology log entry 004 written this session** — capturing two cross-cutting observations: (a) workshops forward-constraint other workshops via cross-BC publication contracts, and (b) mid-sidebar research grounding for vocabulary work.

### 14.9 Workshop status

**Complete (v0.11, 2026-05-09).** Event model for the Trips bounded context is ready to serve as input to narrative authoring (driver-decline narrative is the strongest candidate per narrative-002's adjustment list) and, in turn, to implementation prompt documents — per the `narrative → prompt → execute → retrospective` workflow in the project's `CLAUDE.md`.

---

## Document History

- **v0.1** (2026-05-09): Scope statement, aggregate identity, Ubiquitous Language bootstrap committed. Slice walk pending. First-state vocabulary (`Matched` / `TripMatched`) grounded in real ride-sharing API conventions per mid-sidebar research turn.
- **v0.2** (2026-05-09): Slice 6.1 walked and committed — `TripMatched` Translation-in from Dispatch, with forward-constraint #1 honored, #2 partial (projection shape), #3 partial (mechanism + budget). New ADR candidate captured (driver-app projection timing budget). Cross-references populated for §5, §7, §12, §13.
- **v0.3** (2026-05-09): Slice 6.2 walked and committed — `TripDeparted` Command from driver-app, transition Matched → EnRouteToPickup. First slice exercising lifecycle-monotonicity invariant. New candidate projection `DriverAppActivationLatency` pinned. Event List updated.
- **v0.4** (2026-05-09): Slice 6.3 walked and committed — `DriverArrivedAtPickup` Command from driver-app, transition EnRouteToPickup → AtPickup. First Bruun carry-the-value pattern in this workshop (`expiresAt` computed and carried on the event). Feeds slice 6.4's `RidersAwaitingBoarding*` todo-list. Event List updated.
- **v0.5** (2026-05-09): Slice 6.4 walked and committed — `TripAbandonedAsNoShow` Bruun temporal automation, transition AtPickup → NoShow. First confirmation that Workshop 001's Bruun pattern is portable across BCs. §8 Temporal Automations cross-reference populated. Event List updated.
- **v0.6** (2026-05-09): Slice 6.5 walked and committed — `TripStarted` Command from driver-app, transition AtPickup → InProgress. Disarms slice 6.4's no-show timeout via `RidersAwaitingBoarding*` row removal. Event List updated.
- **v0.7** (2026-05-09): Slice 6.6 walked and committed — `TripCompleted` Command from driver-app, terminal InProgress → Completed, with atomic cross-BC publications queued via Wolverine `OutgoingMessages` (full contract surface at slices 6.9, 6.10). First atomic dual-emit slice in this workshop. Event List updated.
- **v0.8** (2026-05-09): Slices 6.7 + 6.8 walked and committed as paired walk — `TripCancelledByRider` and `TripCancelledByDriver` Command-pattern slices with atomic cross-BC publications. Distinct events with shared `Cancelled` terminal state per §3 sidebar. Mid-trip cancellation rejected at runtime via lifecycle-monotonicity (out of scope per §2.3). §7 Translation Slices and Event List updated.
- **v0.9** (2026-05-09): Slices 6.9 + 6.10 walked and committed as paired walk — Translation-out to Dispatch (slice 5.12 mirror) + fan-out to Pricing / Payments / Ratings / Driver Profile / Trust & Safety / Operations. **Override of Workshop 001 §5.12's preferred unified-event shape** with documented rationale (mirrors Workshop 001's own pattern of distinct events for distinct semantics). ADR-candidate #8 fires (second-BC firing of ASB topic naming convention). §10 Protobuf Contract Surface populated. §11 Parking Lot populated with two Workshop 001 §5.12 enum-gap items (`RIDER_NO_SHOW`, `ASSIGNMENT_COMPLETED_NORMALLY`).
- **v0.10** (2026-05-09): Slices 6.11 + 6.12 walked and committed (final two slices). Slice 6.11 — `TripsPolicyConfigured` configuration-as-events; ADR-candidate #5 fires (second-BC adoption). §9 Configuration-as-Events cross-reference populated. Slice 6.12 — `DriverTripView` enrichment from Identity events; first non-Klefter Translation-in slice in CritterCab; surfaces forward-constraint on Identity workshop (workshops forward-constraining workshops is a new pattern). §13 forward-constraints all updated to honored status. **Slice walk complete (12 slices).** Pending: §11 final review, §12 ADR re-list with status, §14 retrospective, README update, conditional methodology log entry 004.
- **v0.11** (2026-05-09): §14 nine-subsection retrospective committed. Workshop close — all 12 slices walked, 5 ADR candidates surfaced (4 inherited from Workshop 001 + 1 new), 8 parking-lot items, full Protobuf contract surface, all three forward-constraints from narratives 001+002 honored. **Workshop status: complete.**
- **v0.12** (2026-05-09): Workshop 002 §11 items #1 and #2 closed in-PR. Workshop 001 §5.12 received v0.3 amendment (added `RIDER_NO_SHOW` enum value to both enums; documented Workshop 002 §6.9 override; made implicit-happy-path scoping decision explicit). First instance of a workshop's own follow-up landing in the same PR as the workshop itself — pattern worth noting for future bundling decisions.
- **v0.13** (2026-05-10): §12 cross-reference update. All five ADR candidates lifted to authored ADRs in the bundled-ADR session: the new candidate ([ADR-015 — Driver-App Projection Timing Budget](../decisions/015-driver-app-projection-timing-budget.md)) plus four inherited from Workshop 001 §11 ([ADR-011 — Configuration-as-Events Bootstrap Strategy](../decisions/011-configuration-as-events-bootstrap.md), [ADR-012 — Aggregate-per-Invariant](../decisions/012-aggregate-per-invariant.md), [ADR-013 — Shared Cross-BC Identifier](../decisions/013-shared-cross-bc-identifier.md), [ADR-014 — Azure Service Bus Topic Naming Convention](../decisions/014-asb-topic-naming-convention.md)). Each candidate entry in §12 carries an inline link to its now-authored ADR. No content change to the candidate descriptions themselves; the workshop's historical reasoning is preserved as authored. Status header bump from v0.11 → v0.13 catches up the v0.11/v0.12 inconsistency surfaced when this session touched the file.
