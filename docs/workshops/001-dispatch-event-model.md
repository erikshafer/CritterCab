# Workshop 001 — Dispatch Event Model

**Status:** Complete (v0.4, 2026-05-10). All 12 slices walked; v0.3 amendment to §5.12 applied per Workshop 002 §6.9 (added `RIDER_NO_SHOW` to outcome enums; documented override of preferred unified-event shape); v0.4 §11 cross-reference update — ADR candidates #5/#6/#7/#8 lifted to authored ADRs ([ADR-011](../decisions/011-configuration-as-events-bootstrap.md) / [ADR-012](../decisions/012-aggregate-per-invariant.md) / [ADR-013](../decisions/013-shared-cross-bc-identifier.md) / [ADR-014](../decisions/014-asb-topic-naming-convention.md)).
**Started:** 2026-04-24.
**Facilitator / modeler:** Erik Shafer (solo).
**AI collaborator:** Claude (Opus 4.7), rotating through Facilitator, Developer, Skeptic, and Domain-Expert personas per `docs/research/event-modeling-workshop-guide.md` Lesson 8.
**Methodology reference:** `docs/research/event-modeling-workshop-guide.md`.
**Adjunct patterns:** `docs/research/agents-in-event-models.md` (Klefter translation-decision events; Bruun temporal-automation slice pattern).
**Structural constraints honored:** `docs/rules/structural-constraints.md` (ADR-002, ADR-005, ADR-006, ADR-009).

---

## 1. Session Log

| Session | Date | Duration | Steps covered | Notes |
|---|---|---|---|---|
| 1 | 2026-04-24 | Full session (single sitting) | BC selection, scope statement, all 12 slices walked, cross-references populated, retrospective | Solo facilitation with Claude as collaborator. BC candidacy evaluated across Dispatch, Trips, Onboarding, and Identity; Dispatch selected on grounds that it is the architectural spine and the richest methodology exercise. Three durable feedback memories captured during the walk; two more captured at retrospective close. |

---

## 2. Scope Statement

### 2.1 In scope

The Dispatch bounded context's **ride-request lifecycle**, from rider submission through either (a) terminal success via handoff to Trips on acceptance, or (b) terminal failure via rider cancellation, offer exhaustion, or request abandonment. Specifically:

- Rider-initiated request intake (pickup and dropoff capture; validation; recording of the request as the first event in its stream).
- **Fare quoting at request time** as a Translation slice to Pricing. Pricing's physical location (inside Trips vs. separate BC) is parked.
- Candidate selection, via a projection of nearby-available drivers consulted at request time.
- Offer fan-out, with **broadcast topology** (first-to-accept wins) and **per-candidate `OfferSent` events** so that switching to serial or tiered later does not require an event-shape rename.
- Offer disposition: acceptance, decline (with curated-enum reason plus free-text notes), or temporal expiry (Bruun todo-list pattern).
- Re-dispatch when a round completes without acceptance; terminal abandonment when policy-defined limits are exhausted.
- Rider cancellation before a driver has accepted. Outstanding offers are revoked.
- Terminal success: `RideAssigned` published to Trips as a business event via ASB.
- **Dispatch policy configuration as events** (Bruun configuration-as-events pattern). Parameters include offer expiry, max candidates per round, max rounds, and request-abandonment timeout.

### 2.2 At the boundary (modeled as Translation slices, not as internal behavior)

- **Telemetry → Dispatch.** A projection of driver locations. Shape is modeled; Telemetry's internal production is not. Transport (gRPC query vs. Kafka-fed local projection) is a deferred architectural decision.
- **Driver Profile → Dispatch.** A projection of driver availability state (online / offline / on break / suspended). Same treatment.
- **Dispatch → Pricing (fare quote).** Translation out. Dispatch produces a `FareQuoted` local event capturing the returned quote and the decision context (Klefter translation-decision pattern). Pricing's internal behavior is not modeled.
- **Dispatch → Trips (handoff).** On acceptance, Dispatch publishes a business event that Trips consumes to create its `Trip` aggregate. The published event is modeled here; Trips' intake is not.
- **Trips → Dispatch (post-assignment cancellation signal).** Sketched as a placeholder slice for downstream metrics/learning. Full treatment deferred to the Trips workshop.
- **Identity → Dispatch (light).** Every ride request assumes a validated rider identity; the provider-side authentication flow is not modeled here.

### 2.3 Out of scope

- **Surge pricing.** Post-MVP. Revisit when Pricing is actively modeled.
- **Operations BC manual reassignment.** Operator-override commands belong on an Operations board; Dispatch merely receives them eventually.
- **Payment authorization.** Belongs to Payments. Dispatch assumes payment preconditions have been checked.
- **Driver-app and rider-app UX design.** Wireframes are sketched only to the extent needed to make commands and views derivable.
- **Multi-passenger / shared rides.** Single rider, single driver, single-leg trip for v1.
- **Trips' internal lifecycle.** We model Dispatch's handoff event and the contract surface; Trips' internal event timeline is out.

### 2.4 Structural constraints honored

- All Telemetry, Driver Profile, Pricing, and Trips boundary crossings are gRPC calls or Wolverine messages. No shared databases, no in-process cross-service calls (ADR-002).
- Dispatch → Trips handoff is modeled as an ASB-transport business event. Transport label is not drawn on stickies; it is recorded in the Translation-slice notes (ADR-005).
- No provider-specific identity details appear in Dispatch events or projections (ADR-006).
- Any Protobuf contract implied by the model is noted by name in §9 rather than sketched in C# types. Proto-file authorship is a downstream task (ADR-009).

### 2.5 Decisions locked during scope-setting

| Decision | Resolution |
|---|---|
| Event tense | Past tense, always. Technical-integration Translation-in slices may carry the external system's native naming on the foreign side of the boundary only. |
| Noun vocabulary | Pre-acceptance entity is a **Ride Request**. Post-acceptance entity is a **Trip**. Graduation happens at `RideAssigned`. |
| Fan-out topology | Broadcast, first-to-accept wins. `OfferSent` is per-candidate, not a batch event. |
| `OfferDeclined` reason | Curated enum (`TOO_FAR`, `ROUTE_UNDESIRABLE`, `FARE_TOO_LOW`, `DRIVER_GOING_OFFLINE`, `PREFER_NEXT_OFFER`, `OTHER`) with free-text `notes`. |
| Timeout vs. decline | Timeouts emit `OfferExpired`, not `OfferDeclined`. Distinct events, distinct signals. |
| Configuration | Dispatch policy is event-sourced (`DispatchPolicyConfigured`), not a settings table. |
| Post-assignment cancellation | Trips-domain event; Dispatch consumes a signal via Translation-in for metrics only. |

---

## 3. Ubiquitous Language

Populated as the slice walk forces decisions. Each term gets a one-line definition and, where relevant, a note on what it is *not*.

| Term | Definition | Notes |
|---|---|---|
| **Ride Request** | The pre-acceptance entity representing a rider's intent to travel from a pickup to a dropoff. Identified by `rideRequestId`. | Not yet a Trip. Graduates at `RideAssigned`. |
| **Trip** | The post-acceptance entity. Lives on the Trips timeline. | Referenced from Dispatch only at the handoff boundary (Slice 10). |
| **Vehicle Class** | Enum `{ STANDARD, PREMIUM, ACCESSIBLE }` carried on every Ride Request. Governs which drivers are eligible to receive an offer. | Driver capability is asserted in Driver Profile. `ACCESSIBLE` = wheelchair-accessible vehicle. |
| **Active Ride Request** | A Ride Request that has not reached a terminal state (`RideAssigned` \| `RideRequestCancelled` \| `RideRequestAbandoned`). | One-per-rider is enforced at command time on Slice 1. |
| **Offer** | A time-bounded proposal sent to a single candidate driver asking them to take a specific Ride Request. | Per-candidate, not batched. Carries its own expiry timestamp at send. |
| **Dispatch Round** | A single pass of candidate selection and offer fan-out against a Ride Request. A Request may go through multiple rounds before reaching terminal success or abandonment. | See Slice 9. |
| **Fare Quote** | The authoritative price committed to a Ride Request. Locked at submission-time for v1 (no re-quoting). | Quote freshness / re-quote cycle is a post-MVP concern. |
| **Fare Breakdown** | Decomposition of a fare into `base`, `distance`, `time`, and optional `fees` components, in minor units. | Captured on every `FareQuoted` event; UI may display total only. |
| **Dispatch Policy** | The set of operator-tunable parameters governing offer expiry, retry budgets, round limits, and abandonment timeouts. Event-sourced via `DispatchPolicyConfigured`. | Bootstrap default seeded at first deployment. See Slice 11 and ADR candidate in §11. |
| **Candidate** | A driver identified by `CandidateSelectionAutomation` as eligible to receive an offer for a specific Ride Request in a specific Round. | Eligibility = in range + online + vehicle-class capable + not suspended. |
| **Match Score** | A numeric ranking of a candidate for a Request. For v1, computed as inverse straight-line distance. Informational for broadcast; load-bearing for future serial/tiered topologies. | Road-network ETA is a future enhancement. |
| **Round Number** | The ordinal of a dispatch round against a single Ride Request (1 for first attempt, N+1 after each failed round). Used by re-dispatch widening in Slice 9. | |
| **Outstanding Offer** | An Offer that has been sent but not yet Accepted, Declined, Expired, or Revoked. | Lives in `OffersAwaitingExpiry*` and `ActiveOffersForDriver`. |
| **Offer Expiry** | The timestamp after which an Outstanding Offer is eligible for expiry by the `OfferExpirer` temporal automation (Slice 7). | Carried on `OfferSent` at send time (Bruun "carry the value, don't recompute"). |
| **Todo-list Read Model** | A projection shaped as an automation's work queue, not as a human-facing view. Marked with an asterisk suffix (Bruun convention). Rows exist only while there is work to do; they self-remove as the associated work completes. | First use: `OffersAwaitingExpiry*` in Slice 7. |
| **Temporal Automation** | An automation whose trigger is the passage of time (e.g., `now() >= expiresAt` on a todo-list row), not an incoming domain event. Notated with a clock-rewind glyph on the gear sticky (Bruun convention). | First use: `OfferExpirer` in Slice 7. Distinguishes from event-driven automations like `OfferDispatchAutomation`. |

---

## 4. Event List (chronological)

Populated slice-by-slice. Each entry will carry: event name, producing slice reference, key payload fields (at domain grain, not wire grain), and swim-lane assignment (Dispatch vs. Translation-in vs. Translation-out).

| # | Event | Slice | Key payload | Lane |
|---|---|---|---|---|
| 1 | `RideRequested` | 5.1 | `rideRequestId`, `riderId`, `pickup`, `dropoff`, `vehicleClass`, `notesForDriver?`, `requestedAt` | Dispatch |
| 2 | `FareQuoted` | 5.2 | `rideRequestId`, `fareAmount`, `currency`, `fareBreakdown`, `vehicleClass`, `quotedAt`, `validUntil`, `pricingPolicyVersion` | Dispatch (local record of Pricing decision) |
| 2-alt | `FareQuoteFailed` | 5.2 | `rideRequestId`, `reason`, `attemptCount`, `failedAt` | Dispatch (alternate path) |
| 3 | `CandidatesSelected` | 5.3 | `rideRequestId`, `roundNumber`, `candidates[{driverId, matchScore, distanceMeters, etaSeconds}]`, `searchParameters`, `dispatchPolicyVersion`, `selectedAt` | Dispatch (local decision record) |
| 3-alt | `NoCandidatesAvailable` | 5.3 | `rideRequestId`, `roundNumber`, `searchParameters`, `reason`, `selectedAt` | Dispatch (alternate path) |
| 4 | `OfferSent` | 5.4 | `offerId`, `rideRequestId`, `driverId`, `roundNumber`, `expiresAt`, `sentAt`, `fareAmount`, `currency`, `fareBreakdown`, `pickup`, `dropoff`, `vehicleClass`, `distanceMeters`, `etaSeconds`, `notesForDriver?` | Dispatch (one per candidate per round) |
| 5 | `OfferAccepted` | 5.5a | `offerId`, `rideRequestId`, `driverId`, `acceptedAt` | Dispatch (Request stream; atomic with `OfferRevoked` siblings + `RideAssigned`) |
| 5-sib | `OfferRevoked` | 5.5b | `offerId`, `rideRequestId`, `driverId`, `reason`, `revokedAt` | Dispatch (one per sibling; atomic with `OfferAccepted`) |
| 6 | `OfferDeclined` | 5.6 | `offerId`, `rideRequestId`, `driverId`, `reason`, `notes?`, `declinedAt` | Dispatch |
| 7 | `OfferExpired` | 5.7 | `offerId`, `rideRequestId`, `driverId`, `expiresAt`, `expiredAt` | Dispatch (emitted by temporal automation) |
| 8 | `RideRequestCancelled` | 5.8 | `rideRequestId`, `riderId`, `reason`, `notes?`, `cancelledAt` | Dispatch (atomic with any sibling `OfferRevoked { reason: RIDER_CANCELLED }`) |
| 8-sib | `OfferRevoked` (reason `RIDER_CANCELLED`) | 5.8 | same shape as 5-sib | Dispatch (one per outstanding offer at cancel time) |
| 9 | `RideRequestAbandoned` | 5.9 | `rideRequestId`, `reason`, `abandonedAt`, `finalRoundNumber` | Dispatch (terminal; emitted by re-dispatch or timeout automation) |
| 9-sib | `OfferRevoked` (reason `REQUEST_ABANDONED`) | 5.9 | same shape as 5-sib | Dispatch (one per outstanding offer at abandonment time) |
| 10 | `RideAssigned` | 5.10 | `rideRequestId` (= `tripId`), `offerId`, `riderId`, `driverId`, `pickup`, `dropoff`, `vehicleClass`, `fareAmount`, `currency`, `fareBreakdown`, `pricingPolicyVersion`, `notesForDriver?`, `assignedAt` | Dispatch (emitted atomically in Slice 5.5 handler; published to Trips via ASB) |
| 11 | `DispatchPolicyConfigured` | 5.11 | `operatorId`, full parameter set (see slice), `reason?`, `configuredAt` | Dispatch (singleton aggregate stream) |
| 12 | `AssignmentOutcomeRecorded` | 5.12 | `rideRequestId`, `outcome`, `observedAt`, `tripEventOccurredAt`, `tripEventReference?` | Dispatch (post-terminal annotation on the Ride Request stream; emitted by Translation-in handler from Trips) |

---

## 5. Slice Walk

Each slice section carries: pattern type (Command / View / Automation / Translation), trigger, command(s), event(s), view(s) read or produced, GWT sketches, and open-question flags. Slices are numbered in the order they appear on the timeline, not the order they were walked in this session.

### 5.1 Slice 1 — RideRequested (rider submits)

**Pattern:** Command.
**Lane:** Dispatch.
**Trigger:** Rider App — "Request a Ride" screen.

#### Wireframe

```
┌─ Rider App: Request a Ride ────────────────┐
│                                             │
│  Pickup:       [ current location      ▼ ] │
│  Dropoff:      [ search destination…    ] │
│  Vehicle:      [ Standard | Premium | WAV ]│
│                                             │
│  Estimated fare:  $18.40 – $22.10 *         │
│  * final fare confirmed at submission       │
│                                             │
│  Notes for driver (optional):               │
│  [                                        ] │
│                                             │
│  [        Request Ride         ]            │
│                                             │
└─────────────────────────────────────────────┘
```

Pre-submission fare-estimate display is a **query** against Pricing, not an event. It is not modeled. Only the authoritative post-submission fare (Slice 2) is recorded.

#### Command — `SubmitRideRequest`

| Field | Shape | Source | Notes |
|---|---|---|---|
| `rideRequestId` | opaque ID | server-assigned | UUID v7 likely; wire-level choice deferred to ADR-009 proto work. |
| `riderId` | domain rider identifier | authenticated context (API gateway) | Token validation upstream (ADR-006). Dispatch never parses provider claims. |
| `pickup` | `{ lat, lon, streetAddress? }` | rider input | `streetAddress` is display-only; lat/lon is authoritative. |
| `dropoff` | `{ lat, lon, streetAddress? }` | rider input | Same shape. |
| `vehicleClass` | `STANDARD` \| `PREMIUM` \| `ACCESSIBLE` | rider selection | Required. No default. Ripples into Slice 3 candidate filtering. |
| `notesForDriver` | `string?` (bounded length) | rider input, optional | E.g., "meet at side entrance." |
| `requestedAt` | timestamp | server clock on receipt | Server-assigned to avoid client clock drift. |

#### Event emitted — `RideRequested`

Past-tense fact. Payload mirrors the accepted command exactly.

#### Views fed by this slice

- **`ActiveRequestsByRider`** — rider → active-request set. Fed by `RideRequested`; rows removed on terminal events (Slices 8, 9, 10). Consumed by this slice's one-at-a-time rejection and by ops dashboards.
- **`RequestTimeline`** — per-request event history. Fed by every event in this lifecycle; consumed by the rider's request-status screen.

#### GWT sketches

**Happy path**
```
Given: no prior events for rideRequestId X
  And: rider R is authenticated (upstream; assumed)
When: SubmitRideRequest { rideRequestId: X, riderId: R, pickup, dropoff,
                          vehicleClass: STANDARD, requestedAt: T }
Then: RideRequested { rideRequestId: X, riderId: R, pickup, dropoff,
                      vehicleClass: STANDARD, requestedAt: T } is emitted
```

**Duplicate submission (idempotency)**
```
Given: RideRequested { rideRequestId: X, … } already exists
When: SubmitRideRequest { rideRequestId: X, … } is received again
Then: command is rejected with reason DUPLICATE_REQUEST; no new event emitted
```

**Rider already has an active request**
```
Given: rider R has a RideRequested that has not reached a terminal state
       ({ RideAssigned | RideRequestCancelled | RideRequestAbandoned })
When: SubmitRideRequest { riderId: R, rideRequestId: Y, … } for a different Y
Then: command is rejected with reason RIDER_HAS_ACTIVE_REQUEST; no event emitted
```
(Forward-reference: grounded once terminal events from Slices 8, 9, 10 are defined.)

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Vehicle class in v1 | In. Enum: `STANDARD`, `PREMIUM`, `ACCESSIBLE`. Required field. |
| Concurrent active requests per rider | One maximum. Enforced at command time by rejecting on `ActiveRequestsByRider` non-empty. |
| Rider suspension | Upstream concern (Identity / Rider Profile / API gateway). Not modeled in Dispatch. |
| Scheduled rides | Deferred post-MVP. Event shape does not carry `scheduledFor`. |
| Rider authentication | Rejected at the API gateway before reaching Dispatch. Dispatch assumes a validated `riderId`. Consistent with ADR-006. |

#### Cross-references and ripples

- **Ripple into Slice 3:** `vehicleClass` is a filter on candidate selection. Candidate drivers must have the corresponding capability asserted in Driver Profile.
- **Forward-reference to Slices 8, 9, 10:** terminal events close the "active request" and idempotency GWT sketches.

### 5.2 Slice 2 — FareQuoted (Translation out to Pricing)

**Pattern:** Translation (out). Klefter decision-event pattern applies — the fare *is* a decision; we record it locally.
**Lane:** Dispatch for the automation and local events; external call crosses into Pricing (Pricing's internals not modeled).
**Trigger:** `RideRequested` observed on the Dispatch stream (Slice 1 output).

#### Flow on the board

```
  Slice 1 emitted                            Slice 2 emits
       ▼                                          ▼
┌──────────────┐   ┌────────────────────┐   ┌──────────────┐
│ RideRequested│──►│ FareQuote          │──►│ FareQuoted   │
│  (orange)    │   │  Automation (⚙)    │   │  (orange)    │
└──────────────┘   └──────────┬─────────┘   └──────────────┘
                              │
                              │ gRPC unary (ADR-005)
                              ▼
                    ┌─────────────────┐
                    │  Pricing BC     │  ← outside Dispatch's
                    │  GetFareQuote() │    event store; not
                    │  (external)     │    modeled internally
                    └─────────────────┘
```

#### Automation — `FareQuoteAutomation`

Gear sticky on Dispatch's lane. Event-triggered (no clock-rewind glyph).

**Reads:**
- The triggering `RideRequested`.
- `DispatchPolicy` view (latest projection of `DispatchPolicyConfigured`) — for `fareQuoteRetryAttempts` and `fareQuoteTimeoutSeconds`.
- `FareQuoteAttempts` view — per-request retry counter.

**External call issued:**
- **`GetFareQuote(rideRequestId, pickup, dropoff, vehicleClass, requestedAt)`** — gRPC unary to Pricing.

**Events emitted locally:**
- On success: `FareQuoted`.
- On exhausted retries or non-transient error: `FareQuoteFailed`.

Retry attempts themselves are NOT emitted as events. Only the terminal outcome reaches the stream. (Transient retries are implementation detail; durable retry state lives in the `FareQuoteAttempts` projection.)

#### Event — `FareQuoted`

| Field | Shape | Notes |
|---|---|---|
| `rideRequestId` | opaque ID | Correlates with the triggering `RideRequested`. |
| `fareAmount` | integer (minor units) | Integer arithmetic; avoid floating-point money. |
| `currency` | ISO 4217 code | Always carried. `USD` in v1. |
| `fareBreakdown` | `{ base, distance, time, fees? }` | Minor units per component. Captured for transparency and audit. |
| `vehicleClass` | enum from Slice 1 | Echoed for projection convenience. |
| `quotedAt` | timestamp | Server time at response receipt. |
| `validUntil` | timestamp | For v1, effectively `∞` — the quote locks permanently. |
| `pricingPolicyVersion` | opaque | Identifier for the Pricing algorithm/version used. |

#### Event — `FareQuoteFailed` (alternate path)

| Field | Shape | Notes |
|---|---|---|
| `rideRequestId` | opaque ID | |
| `reason` | enum `{ PRICING_UNAVAILABLE, INVALID_ROUTE, NO_COVERAGE, OTHER }` | Curated enum, matches the `OfferDeclined` pattern. |
| `attemptCount` | int | Attempts made before giving up. |
| `failedAt` | timestamp | |

Terminal for the fare-quote step; not itself terminal for the Ride Request. Consumed by Slice 9's re-dispatch/abandonment automation.

#### Views consumed or fed

- **`RequestTimeline`** (fed).
- **`DispatchPolicy`** (consumed) — projection of the latest `DispatchPolicyConfigured`. Forward-reference to Slice 11.
- **`FareQuoteAttempts`** (fed and consumed by the automation) — event-projected retry counter. Survives service restart; prevents double-spending retry budget. Observability-from-day-one honored.

#### GWT sketches

**Happy path**
```
Given: RideRequested { rideRequestId: X, pickup, dropoff, vehicleClass: STANDARD, ... }
  And: DispatchPolicyConfigured { fareQuoteRetryAttempts: 3, fareQuoteTimeoutSeconds: 2 }
When: FareQuoteAutomation reacts
  And: GetFareQuote returns { fareAmount: 2150, currency: USD, breakdown, validUntil, version }
Then: FareQuoted { rideRequestId: X, fareAmount: 2150, currency: USD, breakdown,
                   vehicleClass: STANDARD, quotedAt, validUntil, pricingPolicyVersion } is emitted
```

**Transient failure with retry recovery**
```
Given: RideRequested for X
  And: DispatchPolicyConfigured { fareQuoteRetryAttempts: 3, fareQuoteTimeoutSeconds: 2 }
When: FareQuoteAutomation's first attempt times out
  And: the second attempt succeeds
Then: FareQuoted is emitted
  And: no FareQuoteFailed event is emitted
```

**Exhausted retries**
```
Given: RideRequested for X
  And: DispatchPolicyConfigured { fareQuoteRetryAttempts: 3, fareQuoteTimeoutSeconds: 2 }
When: FareQuoteAutomation attempts 3 times, all transient failures
Then: FareQuoteFailed { rideRequestId: X, reason: PRICING_UNAVAILABLE, attemptCount: 3, failedAt: T } is emitted
```

**Non-transient failure — no retry**
```
Given: RideRequested for X with pickup/dropoff outside Pricing's coverage
When: FareQuoteAutomation's first call returns a NO_COVERAGE domain error
Then: FareQuoteFailed { rideRequestId: X, reason: NO_COVERAGE, attemptCount: 1, failedAt: T } is emitted
  And: no further attempts are made
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Fare breakdown granularity | Full breakdown (`base + distance + time + fees?`) on every `FareQuoted` event. Transparency principle. |
| Quote freshness / re-quoting | Locked at submission for v1. `validUntil` effectively `∞`. Re-quote cycle post-MVP. |
| Retry configuration location | In `DispatchPolicyConfigured`. Fields: `fareQuoteRetryAttempts`, `fareQuoteTimeoutSeconds`. Bootstrap defaults seeded at deployment. |
| Retry counter durability | Event-projected (`FareQuoteAttempts` view). Observability-from-day-one. |
| Automation naming | `FareQuoteAutomation`. Avoids "Coordinator"/"Orchestrator" overloading from other ecosystems. |
| Currency handling | USD-only for v1; `currency` field always carried on event shape (zero-cost future-proofing). |

#### Cross-references and ripples

- **Backward:** Triggered by `RideRequested` (Slice 1).
- **Forward (Slice 9):** `FareQuoteFailed` feeds the re-dispatch/abandonment automation.
- **Forward (Slice 11):** `DispatchPolicyConfigured` adds `fareQuoteRetryAttempts` and `fareQuoteTimeoutSeconds` to its payload.
- **Protobuf surface (ADR-009):** `GetFareQuote` message pair — recorded in §9.
- **Bootstrap defaults:** Policy-bootstrap strategy is an ADR candidate (§11).

### 5.3 Slice 3 — CandidatesSelected (Translation-in with decision)

**Pattern:** Translation (in from Telemetry + Driver Profile) + Klefter decision-event. The selection is a decision; its search parameters and policy version are captured for auditability.
**Lane:** Dispatch for the automation and local events. The consumed view straddles the boundary; its population mechanism is deferred (parking-lot #4).
**Trigger:** `FareQuoted` on the Dispatch stream (Slice 2 output).

#### Flow on the board

```
            Slice 2 emitted
                   ▼
          ┌────────────────┐
          │  FareQuoted    │
          └───────┬────────┘
                  │
                  ▼
         ┌──────────────────┐
         │ CandidateSelection│──── reads ────►  NearbyAvailableDrivers  (view)
         │  Automation  (⚙) │                   DispatchPolicy          (view)
         └────────┬─────────┘
                  │
                  │ (Klefter: decision captured locally)
                  ▼
         ┌────────────────────┐    OR    ┌──────────────────────────┐
         │ CandidatesSelected │          │ NoCandidatesAvailable    │
         │   (happy path)     │          │   (empty-set path)       │
         └────────────────────┘          └──────────────────────────┘

  Upstream Translation-in sources (feeding NearbyAvailableDrivers):
    Telemetry ──(driver location events)──►  ┐
                                             │  NearbyAvailableDrivers
    Driver Profile ──(availability + capabilities)──┘
```

#### Translation-in sources

| Counterparty | Feeds view | Translation-in events |
|---|---|---|
| Telemetry | `NearbyAvailableDrivers` (location side) | `DriverLocationUpdated` — grain = throttled / cell-changed, not per-ping. |
| Driver Profile | `NearbyAvailableDrivers` (availability side) + `DriverCapabilities` | `DriverCameOnline`, `DriverWentOnBreak`, `DriverWentOffline`, `DriverVehicleChanged` — each carries `vehicleClass` capability at the transition. |

Transport for these inbound flows is deferred to parking-lot #4 (local projection vs. remote query).

#### Automation — `CandidateSelectionAutomation`

**Reads:**
- Triggering `FareQuoted` (for `rideRequestId`, `vehicleClass`).
- The corresponding `RideRequested` (for `pickup`).
- `NearbyAvailableDrivers` view — filtered by pickup-radius and `vehicleClass`.
- `DispatchPolicy` view — `searchRadiusMeters`, `maxCandidatesPerRound`.

**Emits:**
- On ≥1 eligible candidate: `CandidatesSelected`.
- On 0 eligible candidates: `NoCandidatesAvailable`.

No external call. Operates entirely on already-available views.

#### Event — `CandidatesSelected`

| Field | Shape | Notes |
|---|---|---|
| `rideRequestId` | opaque ID | |
| `roundNumber` | int (starts at 1) | Multiple rounds possible via Slice 9. |
| `candidates` | ordered list of `{ driverId, matchScore, distanceMeters, etaSeconds }` | Ordered by `matchScore` desc. For broadcast MVP order is informational; retained so serial/tiered upgrade needs no event-shape change. |
| `searchParameters` | `{ searchRadiusMeters, maxCandidatesPerRound, vehicleClassRequired }` | Snapshot of the policy values applied. Klefter transparency. |
| `dispatchPolicyVersion` | opaque | Matches the policy event in effect. |
| `selectedAt` | timestamp | |

#### Event — `NoCandidatesAvailable`

| Field | Shape | Notes |
|---|---|---|
| `rideRequestId` | opaque ID | |
| `roundNumber` | int | |
| `searchParameters` | as above | |
| `reason` | enum `{ NO_DRIVERS_IN_RANGE, NO_CAPABLE_DRIVERS_IN_RANGE, ALL_CAPABLE_DRIVERS_OCCUPIED, OTHER }` | Curated enum. |
| `selectedAt` | timestamp | |

Distinct event from `CandidatesSelected` for cleaner GWT branching and clearer downstream signal (Slice 9).

#### Views consumed

- **`NearbyAvailableDrivers`** — driver location + availability + vehicle-class capability. Shape modeled here; population mechanism deferred.
- **`DispatchPolicy`** — latest policy projection.

#### Views fed

- **`RequestTimeline`**.
- **`RequestRounds`** — per-request history of dispatch rounds; consumed by Slice 9 and by ops dashboards.

#### GWT sketches

**Happy path**
```
Given: FareQuoted { rideRequestId: X, vehicleClass: STANDARD, ... }
  And: RideRequested { rideRequestId: X, pickup: P, ... }
  And: DispatchPolicyConfigured { searchRadiusMeters: 5000, maxCandidatesPerRound: 5 }
  And: NearbyAvailableDrivers contains 6 STANDARD-capable drivers within 5000m of P
When: CandidateSelectionAutomation reacts
Then: CandidatesSelected { rideRequestId: X, roundNumber: 1,
                           candidates: [ top 5 by matchScore ],
                           searchParameters, dispatchPolicyVersion, selectedAt } is emitted
```

**No drivers in range**
```
Given: FareQuoted { rideRequestId: X, vehicleClass: STANDARD, ... }
  And: NearbyAvailableDrivers contains 0 drivers within searchRadiusMeters of pickup
When: CandidateSelectionAutomation reacts
Then: NoCandidatesAvailable { rideRequestId: X, roundNumber: 1, searchParameters,
                               reason: NO_DRIVERS_IN_RANGE, selectedAt } is emitted
```

**Vehicle-class gap (ACCESSIBLE scarcity)**
```
Given: FareQuoted { rideRequestId: X, vehicleClass: ACCESSIBLE, ... }
  And: NearbyAvailableDrivers contains 8 STANDARD drivers but 0 ACCESSIBLE drivers within range
When: CandidateSelectionAutomation reacts
Then: NoCandidatesAvailable { rideRequestId: X, roundNumber: 1, searchParameters,
                               reason: NO_CAPABLE_DRIVERS_IN_RANGE, selectedAt } is emitted
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Match-score algorithm | Straight-line distance (inverse) for v1. Road-network ETA is a future gRPC counterparty enhancement without event-shape change. |
| Empty candidate set | Separate event (`NoCandidatesAvailable`) for clearer downstream signal. General preference noted: lean toward two events when the signal differs meaningfully. |
| Search radius in this slice | Fixed from `DispatchPolicyConfigured.searchRadiusMeters`. Adaptive widening is Slice 9 territory, driven by `roundNumber`. |
| Driver concurrency | Tolerated. A driver may appear as a candidate for two requests at once; the race is resolved cleanly in Slice 5 via `OfferRevoked`. |
| Driver capability | One vehicle in service at a time for v1. `DriverCameOnline` carries the in-service `vehicleId` and its `vehicleClass`. Multi-vehicle switching is post-MVP (low priority). |
| View population transport | Model agnostically at the "view is available" level. Transport/population decision deferred to ADR (parking-lot #4). |

#### Cross-references

- **Backward:** Triggered by `FareQuoted` (Slice 2).
- **Forward (Slice 4):** `CandidatesSelected` triggers offer broadcast.
- **Forward (Slice 9):** `NoCandidatesAvailable` and round-exhaustion feed re-dispatch/abandonment with adaptive radius widening.
- **Forward (Slice 11):** Consumes `searchRadiusMeters` and `maxCandidatesPerRound` from `DispatchPolicyConfigured`.
- **Parking-lot #4:** View population transport decision deferred.
- **Parking-lot (new):** Road-network ETA service is a candidate future gRPC counterparty — see §10.

### 5.4 Slice 4 — OfferSent (per-candidate broadcast)

**Pattern:** Command Pattern applied per-candidate (drawn once on the board with "×N" annotation).
**Lane:** Dispatch.
**Trigger:** `CandidatesSelected` on the Dispatch stream (Slice 3 output).

#### Flow on the board

```
            Slice 3 emitted
                   ▼
         ┌──────────────────────┐
         │ CandidatesSelected   │
         │  candidates: [D1,...,Dn] │
         └──────────┬───────────┘
                    │
                    ▼
           ┌──────────────────┐
           │ OfferDispatch    │──── reads ──► DispatchPolicy (view),
           │  Automation (⚙)  │                RideRequested, FareQuoted
           └────────┬─────────┘
                    │ fan-out, per candidate
                    │
         ┌──────────┼──────────┐
         ▼          ▼          ▼
      SendOffer  SendOffer  SendOffer   (blue commands, ×N)
         │          │          │
         ▼          ▼          ▼
      OfferSent  OfferSent  OfferSent   (orange events, ×N)
         │          │          │
         └──────────┼──────────┘
                    ▼
          Views fed: ActiveOffersForDriver,
                     OffersAwaitingExpiry*,
                     OfferRegister
```

`ActiveOffersForDriver` is consumed by the driver app via **gRPC server-streaming** — the canonical Wolverine 5.32 showcase shape. Streaming is a view-delivery mechanism, not a sticky on the board.

#### Automation — `OfferDispatchAutomation`

Gear sticky, event-triggered.

**Reads:** triggering `CandidatesSelected`; corresponding `RideRequested` and `FareQuoted`; `DispatchPolicy` view for `offerExpirySeconds`.

**Emits:** one `SendOffer` command per candidate, each producing an `OfferSent` event. Sends are best-effort independent; failed sends are silent (no event emitted for that candidate). Retry within the offer window is not modeled for v1.

#### Command — `SendOffer`

| Field | Shape | Notes |
|---|---|---|
| `offerId` | server-assigned opaque ID (UUID v7) | Each offer has its own identity. |
| `rideRequestId` | opaque ID | |
| `driverId` | opaque ID | |
| `roundNumber` | int | Echoed from `CandidatesSelected`. |
| `expiresAt` | timestamp | Computed at send as `sentAt + DispatchPolicy.offerExpirySeconds`. Carried on the event — load-bearing for Slice 7's Bruun todo-list projection. |
| `sentAt` | timestamp | Server clock. |
| `fareAmount` | integer (minor units) | From `FareQuoted`. |
| `currency` | ISO 4217 code | |
| `fareBreakdown` | `{ base, distance, time, fees? }` | Embedded per the denormalization-for-point-in-time-commitments principle. Not ECST. |
| `pickup`, `dropoff` | `{ lat, lon, streetAddress? }` | From the Request. |
| `vehicleClass` | enum | |
| `distanceMeters`, `etaSeconds` | int | Driver's distance/ETA to pickup (from candidate record). |
| `notesForDriver` | string? | Visible on `OfferSent` for v1; flagged as post-MVP policy concern. |

#### Event — `OfferSent`

Past-tense fact; payload mirrors the accepted command exactly. One event per successful send.

#### Views fed

- **`ActiveOffersForDriver`** — per-driver projection of outstanding offers. Consumed via gRPC server-streaming. Rows removed as Slices 5/6/7/8 dispose offers.
- **`OffersAwaitingExpiry*`** — the Bruun todo-list (asterisk suffix). Rows: `{ offerId, rideRequestId, driverId, expiresAt }`. Consumed by Slice 7's `OfferExpirer` automation. Rows removed by any offer disposition.
- **`OfferRegister`** — per-offer status projection for ops dashboards.
- **`RequestTimeline`**.

#### GWT sketches

**Happy path — three candidates get three offers**
```
Given: CandidatesSelected { rideRequestId: X, roundNumber: 1,
                             candidates: [{D1, score, 300m, 45s}, {D2, ..., 500m, 70s}, {D3, ..., 800m, 110s}] }
  And: RideRequested, FareQuoted for X (with all attendant fields)
  And: DispatchPolicyConfigured { offerExpirySeconds: 20 }
When: OfferDispatchAutomation reacts
Then: three OfferSent events are emitted (one per candidate) with distinct offerIds
       each carrying expiresAt = sentAt + 20s
  And: three rows appear in OffersAwaitingExpiry*
  And: ActiveOffersForDriver updates for D1, D2, D3
```

**Best-effort partial failure**
```
Given: CandidatesSelected { candidates: [D1, D2, D3] }
  And: the handler for SendOffer to D2 throws a transient error
When: OfferDispatchAutomation reacts
Then: OfferSent is emitted for D1 and D3 only
  And: D2 does not receive an offer this round; no event emitted for the failed send
```

**Driver went offline between selection and send**
```
Given: CandidatesSelected { candidates: [D1] }
  And: DriverWentOffline { driverId: D1 } observed between selection and send (upstream)
When: OfferDispatchAutomation reacts
Then: OfferSent { offerId: O1, driverId: D1, ... } is emitted anyway
  And: the driver app is disconnected; no streaming delivery occurs
  And: the offer will expire naturally via Slice 7
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Offer ID generation | Server-assigned UUID v7. Default; reconsidered per-slice if a deterministic-ID use case appears. |
| `OfferDelivered` event | Deferred to post-MVP. Monitoring-grade signal; no demo-time audience. Will be added as an extension, not a breaking change. |
| Partial send failure | Silent. No `OfferSendFailed` event. Broadcast design means remaining candidates still race; retry adds complexity without changing rider outcomes. |
| Re-validate candidates at send | No. Send to all selected candidates; natural expiry handles staleness. Near-edge re-validation is post-MVP. |
| Offer payload | Embed fare breakdown on `OfferSent`. Denormalization of stable point-in-time commitment; distinct from ECST. |
| `notesForDriver` visibility | Visible on `OfferSent` for v1. Discrimination-mitigation policy (e.g., visible only after accept) flagged for post-MVP. |

#### Cross-references

- **Backward:** Triggered by `CandidatesSelected` (Slice 3).
- **Forward (Slice 5):** `OfferAccepted` terminates disposition; siblings revoked.
- **Forward (Slice 6):** `OfferDeclined`.
- **Forward (Slice 7):** `OfferExpired` — temporal automation over `OffersAwaitingExpiry*`.
- **Forward (Slice 8):** `RideRequestCancelled` revokes outstanding offers.
- **Forward (Slice 11):** Consumes `offerExpirySeconds` from `DispatchPolicyConfigured`.
- **Protobuf surface (ADR-009):** `ActiveOffersForDriver` server-streaming RPC surface — recorded in §9.

### 5.5 Slice 5 — OfferAccepted + sibling revocation cascade

**Pattern:** Command Pattern (5.5a) with a cascaded multi-event emission that subsumes what would otherwise be a separate Automation (5.5b).
**Lane:** Dispatch.
**Trigger:** Driver app — "Accept" action on an offer delivered via the `ActiveOffersForDriver` stream.

#### Concurrency invariant and aggregate choice

**The Ride Request is the aggregate.** Every offer lifecycle event is appended to the `RideRequest` stream keyed by `rideRequestId`. Offers are sub-entities of the Request, not independent aggregates — they have no lifecycle except as sub-states of the Request they belong to. The invariant *"at most one `OfferAccepted` per `rideRequestId`"* is enforced natively by Marten's optimistic concurrency on the Request stream. No distributed lock, no cross-stream coordination.

The `AcceptOffer` handler uses Wolverine's `[AggregateHandler]` to load the Request and atomically emits:
1. `OfferAccepted` for the winning offer.
2. `OfferRevoked { reason: SIBLING_ACCEPTED }` for each other outstanding offer.
3. `RideAssigned` (the terminal success event — Slice 10 handoff).
4. Outgoing ASB message to Trips (via Wolverine's `OutgoingMessages`, outbox-coordinated).

All four in one transactional commit against the same stream version. If a concurrent `AcceptOffer` for a sibling wins the race, this handler's version check fails; Wolverine surfaces the concurrency exception; the handler re-loads and issues a clean rejection.

This collapses Slices 5.5a and 5.5b into a single command handler at the implementation level. The Event Modeling board still shows acceptance and revocation as separate stickies — the modeling separation is preserved — but the physical handler is unified.

See ADR candidate §11 item 6 for the formal decision record.

#### Flow on the board

```
  Driver app (accept action)
           │
           ▼
     ┌──────────────┐
     │ AcceptOffer  │  blue command
     └──────┬───────┘
            │  [AggregateHandler] loads RideRequest
            │  validates clock, status, driver, request
            │
            ▼
     ┌───────────────────────────────────────┐
     │ Atomic multi-event emission:          │
     │   • OfferAccepted  (winner)           │
     │   • OfferRevoked ×N (siblings,        │
     │     reason: SIBLING_ACCEPTED)         │
     │   • RideAssigned  (terminal success)  │
     │   + outgoing ASB publication (Trips)  │
     └───────────────────────────────────────┘
            │
            ▼
   Views updated: OffersAwaitingExpiry* (remove),
                  ActiveOffersForDriver (remove for winner + siblings),
                  RequestTimeline, RequestRounds (round outcome = ACCEPTED)
```

#### 5.5a — `OfferAccepted`

**Command — `AcceptOffer`**

| Field | Shape | Notes |
|---|---|---|
| `offerId` | opaque ID | The offer being accepted. |
| `driverId` | opaque ID | Authenticated driver context; must match the `driverId` on `OfferSent`. |
| `acceptedAt` | timestamp | Server clock. |

**Event — `OfferAccepted`**

| Field | Shape | Notes |
|---|---|---|
| `offerId` | opaque ID | |
| `rideRequestId` | opaque ID | Derived from the offer; carried for projection convenience. |
| `driverId` | opaque ID | |
| `acceptedAt` | timestamp | |

Lean payload; the offer's full terms are already on `OfferSent`.

**Rejection reasons (enum on command rejection):**
`REQUEST_ALREADY_ASSIGNED`, `REQUEST_CANCELLED`, `OFFER_ALREADY_DISPOSED`, `OFFER_EXPIRED` (clock-based), `NOT_THE_OFFERED_DRIVER`, `DRIVER_NO_LONGER_ELIGIBLE`.

#### 5.5b — Sibling `OfferRevoked` (cascaded)

**Emitted by the same handler** as `OfferAccepted`, one event per sibling outstanding offer.

**Event — `OfferRevoked`**

| Field | Shape | Notes |
|---|---|---|
| `offerId` | opaque ID | Sibling being revoked. |
| `rideRequestId` | opaque ID | |
| `driverId` | opaque ID | Driver whose offer was revoked. |
| `reason` | enum `{ SIBLING_ACCEPTED, RIDER_CANCELLED, REQUEST_ABANDONED }` | `SIBLING_ACCEPTED` here (Slice 5.5); `RIDER_CANCELLED` in Slice 5.8; `REQUEST_ABANDONED` in Slice 5.9. Further values post-MVP. |
| `revokedAt` | timestamp | Matches `acceptedAt` (same transactional commit). |

#### GWT sketches

**Happy path (with two sibling revocations)**
```
Given: OfferSent O1 (D1), O2 (D2), O3 (D3), all for rideRequestId X, all outstanding
  And: no OfferAccepted exists for X
  And: current time < expiresAt for all three offers
When: AcceptOffer { offerId: O1, driverId: D1, acceptedAt: T+10s }
Then: the following events are emitted atomically on the RideRequest stream:
        OfferAccepted  { offerId: O1, rideRequestId: X, driverId: D1, acceptedAt: T+10s }
        OfferRevoked   { offerId: O2, rideRequestId: X, driverId: D2, reason: SIBLING_ACCEPTED, revokedAt: T+10s }
        OfferRevoked   { offerId: O3, rideRequestId: X, driverId: D3, reason: SIBLING_ACCEPTED, revokedAt: T+10s }
        RideAssigned   { rideRequestId: X, offerId: O1, driverId: D1, ... handoff payload ... }
  And: an outgoing ASB message is published to Trips carrying RideAssigned
```

**Concurrent-accept race (two drivers, one wins)**
```
Given: OfferSent O1 (D1) and O2 (D2) for rideRequestId X, both within expiry
When: AcceptOffer for O1 and AcceptOffer for O2 arrive concurrently
Then: exactly one AcceptOffer commits; the other fails optimistic-concurrency on the Request stream version
  And: exactly one OfferAccepted is emitted
  And: the losing command is rejected (after automatic re-load) with REQUEST_ALREADY_ASSIGNED
```

**Stale acceptance (clock expiry before event)**
```
Given: OfferSent { offerId: O1, expiresAt: T+20s }, no OfferExpired event yet
  And: current time is T+25s
When: AcceptOffer { offerId: O1, driverId: D1 }
Then: command is rejected with reason OFFER_EXPIRED
  And: no OfferAccepted emitted
Note: handler validates clock independently of Slice 7's expirer.
```

**Offer already disposed**
```
Given: OfferSent { offerId: O1 }
  And: OfferDeclined (or OfferExpired, or OfferRevoked) for O1 exists
When: AcceptOffer { offerId: O1 }
Then: command is rejected with OFFER_ALREADY_DISPOSED; no event emitted
```

**Wrong driver**
```
Given: OfferSent { offerId: O1, driverId: D1 }
When: AcceptOffer { offerId: O1, driverId: D2 }  (D2 ≠ D1)
Then: command is rejected with NOT_THE_OFFERED_DRIVER
```

**Request cancelled by rider**
```
Given: OfferSent { offerId: O1, rideRequestId: X, driverId: D1 }
  And: RideRequestCancelled { rideRequestId: X } exists (Slice 8)
When: AcceptOffer { offerId: O1, driverId: D1 }
Then: command is rejected with REQUEST_CANCELLED
```

**Driver no longer eligible**
```
Given: OfferSent { offerId: O1, driverId: D1 }
  And: Driver Profile / Identity upstream has flagged D1 as ineligible since the offer was sent
       (e.g., DriverSuspended, DriverWentOffline-and-has-not-returned)
When: AcceptOffer { offerId: O1, driverId: D1 }
Then: command is rejected with DRIVER_NO_LONGER_ELIGIBLE
  And: rejection is operator-audible (soft-reject, not silent drop)
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Stale-acceptance clock check | Yes. Handler validates `now() < expiresAt` independently of Slice 7. |
| `OfferRevoked.reason` enum values for v1 | `{ SIBLING_ACCEPTED, RIDER_CANCELLED }`. Others post-MVP. |
| Concurrency invariant location | RideRequest aggregate stream via Marten optimistic concurrency. Offers are sub-entities, not independent aggregates. |
| Sub-slice structure on the board | 5.5a (OfferAccepted) + 5.5b (sibling revocation cascade) — paired under 5.5. |
| Rejection reason set | `REQUEST_ALREADY_ASSIGNED`, `REQUEST_CANCELLED`, `OFFER_ALREADY_DISPOSED`, `OFFER_EXPIRED`, `NOT_THE_OFFERED_DRIVER`, `DRIVER_NO_LONGER_ELIGIBLE`. |
| `OfferAccepted` → `RideAssigned` atomicity | Atomic dual-emit in the same handler via Wolverine `Events` + `OutgoingMessages`. No intermediate automation. |

#### Notation notes

- `AcceptOffer` (blue) → `OfferAccepted` (orange). `RevokeOffer` is no longer drawn as a separate blue command — the revocation is in the same handler. The board shows `OfferRevoked` events branching off from the `OfferAccepted` sticky with a cascade annotation.
- The implementation-unified handler is documented in the slice text but does not change the board's sticky count.

#### Cross-references

- **Backward:** Triggered by `OfferSent` (Slice 4) and the driver's accept action.
- **Forward (Slice 10):** `RideAssigned` is emitted atomically here; Slice 10 documents its contract surface and the ASB handoff to Trips.
- **Forward (Slice 8):** `OfferRevoked { reason: RIDER_CANCELLED }` is the other use of the revocation event.
- **Consumed by Slice 7:** The `OfferExpirer` must also check for terminal dispositions before emitting `OfferExpired`.
- **ADR candidate §11 item 6:** RideRequest as single aggregate; Offer lifecycle events on its stream.

### 5.6 Slice 6 — OfferDeclined (driver declines)

**Pattern:** Command Pattern. Single-event terminal disposition for one offer; no cascade.
**Lane:** Dispatch.
**Trigger:** Driver app — driver selects a decline reason.

#### Flow on the board

```
  Driver app (decline with reason)
           │
           ▼
     ┌──────────────┐
     │ DeclineOffer │  blue command
     └──────┬───────┘
            │  [AggregateHandler] loads RideRequest
            │  validates exists, outstanding, right driver, clock, notes
            ▼
     ┌──────────────┐
     │ OfferDeclined│  orange event (on RideRequest stream)
     └──────────────┘
```

#### Command — `DeclineOffer`

| Field | Shape | Notes |
|---|---|---|
| `offerId` | opaque ID | |
| `driverId` | opaque ID | Authenticated driver; must match offer's `driverId`. |
| `reason` | enum | Required. |
| `notes` | string? (bounded) | Required when `reason = OTHER`; optional otherwise. |
| `declinedAt` | timestamp | Server clock. |

#### Event — `OfferDeclined`

| Field | Shape |
|---|---|
| `offerId` | opaque ID |
| `rideRequestId` | opaque ID (derived) |
| `driverId` | opaque ID |
| `reason` | enum `{ TOO_FAR, ROUTE_UNDESIRABLE, FARE_TOO_LOW, DRIVER_GOING_OFFLINE, PREFER_NEXT_OFFER, OTHER }` |
| `notes` | string? |
| `declinedAt` | timestamp |

#### Rejection reasons

`OFFER_NOT_FOUND`, `OFFER_ALREADY_DISPOSED`, `OFFER_EXPIRED` (clock-based), `NOT_THE_OFFERED_DRIVER`, `NOTES_REQUIRED` (reason=OTHER without notes).

#### GWT sketches

**Happy path**
```
Given: OfferSent { offerId: O1, rideRequestId: X, driverId: D1, expiresAt: T+20s }
  And: no terminal disposition for O1; current time < T+20s
When: DeclineOffer { offerId: O1, driverId: D1, reason: TOO_FAR, notes: null, declinedAt: T+5s }
Then: OfferDeclined { offerId: O1, rideRequestId: X, driverId: D1, reason: TOO_FAR, notes: null, declinedAt: T+5s } is emitted
  And: this offer is removed from OffersAwaitingExpiry* and ActiveOffersForDriver for D1
  And: siblings are unaffected
```

**Decline with OTHER + notes**
```
Given: OfferSent { offerId: O1, ... }
When: DeclineOffer { offerId: O1, driverId: D1, reason: OTHER, notes: "Airport detour in 10 min", declinedAt }
Then: OfferDeclined { ..., reason: OTHER, notes: "Airport detour in 10 min" } is emitted
```

**Decline with OTHER and no notes**
```
Given: OfferSent { offerId: O1, ... }
When: DeclineOffer { ..., reason: OTHER, notes: null }
Then: command is rejected with NOTES_REQUIRED
```

**Stale decline (clock expiry before event)**
```
Given: OfferSent { offerId: O1, expiresAt: T+20s }, no OfferExpired yet
  And: current time is T+25s
When: DeclineOffer { offerId: O1, driverId: D1, reason: TOO_FAR, ... }
Then: command is rejected with OFFER_EXPIRED
```

**Wrong driver**
```
Given: OfferSent { offerId: O1, driverId: D1 }
When: DeclineOffer { offerId: O1, driverId: D2, ... }
Then: command is rejected with NOT_THE_OFFERED_DRIVER
```

#### Views affected (strictly required by this slice)

- `OffersAwaitingExpiry*` — this offer's row removed.
- `ActiveOffersForDriver` — this offer removed from D's view.
- `OfferRegister` — terminal state `Declined` recorded.
- `RequestTimeline` — adds `OfferDeclined`.
- `RequestRounds` — per-round disposition tracking (feeds Slice 9).

#### Candidate projections for future consumers

Projections not strictly required by this slice but worth naming now so the `OfferDeclined` event shape carries the fields they'd need.

| Projection | Audience | Shape | Feeder events | Status |
|---|---|---|---|---|
| `DriverDeclineRateByReason` | Driver Profile, Trust & Safety, ML ranking | Per `driverId` × `reason`: rolling counter + windowed rates | `OfferDeclined` | Defer. Natural home is Driver Profile via ASB subscription. |
| `RequestFailurePatternsByRegion` | Operations, Pricing (surge signals), ML | Per geographic region × `vehicleClass` × outcome: distribution of offers that didn't convert | `OfferDeclined`, `OfferExpired`, `NoCandidatesAvailable`, `FareQuoteFailed` | Defer. Cross-BC analytical view; revisit in Pricing workshop. |
| `OfferOutcomeAudit` | Operations (customer support tooling), compliance | Per `rideRequestId` timeline: every offer's lifecycle event with driver, reason, timings | All offer-lifecycle events | Defer. Operations BC concern; likely a feature of its support tooling. |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| `notes` rule | Required when `reason = OTHER`, optional otherwise. Rejection `NOTES_REQUIRED`. |
| Clock-stale decline | Reject with `OFFER_EXPIRED`. Consistent with Slice 5. |
| Reason enum set for v1 | `TOO_FAR`, `ROUTE_UNDESIRABLE`, `FARE_TOO_LOW`, `DRIVER_GOING_OFFLINE`, `PREFER_NEXT_OFFER`, `OTHER`. `VEHICLE_ISSUE` subsumed by "go offline first." `SAFETY_CONCERN` deferred to Trust & Safety with its own handling path. |
| Decline-rate monitoring ownership | Driver Profile via ASB subscription. Dispatch stays focused on lifecycle. |
| `DeclineReasonCounters` view | Defer to Driver Profile. |
| Declined → excluded next round | Yes, for same `rideRequestId` only. Flagged to Slice 9 scope. |

#### Cross-references

- **Backward:** Triggered by `OfferSent` (Slice 4) and driver's decline action.
- **Forward (Slice 7):** Declining removes the offer from `OffersAwaitingExpiry*` so the expirer never fires for it.
- **Forward (Slice 9):** Round-disposition tracking counts declines; if all siblings are disposed without acceptance, re-dispatch decides widen-vs-abandon. Declined drivers are excluded from subsequent rounds for the same request.
- **Cross-BC (future):** Driver Profile subscribes to `OfferDeclined` on ASB for decline-rate analytics.

### 5.7 Slice 7 — OfferExpired (temporal automation; Bruun pattern)

**Pattern:** Automation Pattern, time-driven. Bruun notation: clock-rewind glyph on the automation sticky, asterisk suffix on the todo-list it consumes.
**Lane:** Dispatch.
**Trigger:** Time reaching any row's `expiresAt` in `OffersAwaitingExpiry*`.

#### Flow on the board

```
   (upstream: offers flow in via OfferSent;
    disposed offers leave via 5.5/5.6/5.7/5.8)

      ┌────────────────────────────────────┐
      │  OffersAwaitingExpiry*             │  green sticky, asterisk (Bruun)
      │  Rows: { offerId, rideRequestId,   │  automation work queue — no UI
      │          driverId, expiresAt }     │
      └──────────────┬─────────────────────┘
                     │ rows where now() >= expiresAt
                     ▼
          ┌──────────────────────┐
          │   OfferExpirer       │  gear + clock-rewind glyph
          │   (⚙ + ⏪)            │  time-driven trigger
          └──────────┬───────────┘
                     │ per stale row
                     ▼
          ┌──────────────────────┐
          │   ExpireOffer        │  blue command
          └──────────┬───────────┘
                     │  [AggregateHandler] validates offer still Outstanding
                     ▼
          ┌──────────────────────┐
          │   OfferExpired       │  orange event (RideRequest stream)
          └──────────────────────┘
```

#### Automation — `OfferExpirer`

**Sticky:** gear + clock-rewind glyph. First appearance of the temporal-automation notation in the workshop.

**Reads:** `OffersAwaitingExpiry*` — rows where `now() >= expiresAt`.

**Emits:** one `ExpireOffer` command per stale row.

**Implementation mechanism:** Wolverine scheduled messages or Marten async daemon (Critter Stack primitives). Specific choice is a skill-file decision, not a modeling concern.

#### Command — `ExpireOffer`

| Field | Shape | Notes |
|---|---|---|
| `offerId` | opaque ID | |
| `expiredAt` | timestamp | Server clock at automation-fire time. |

#### Event — `OfferExpired`

| Field | Shape | Notes |
|---|---|---|
| `offerId` | opaque ID | |
| `rideRequestId` | opaque ID | Derived. |
| `driverId` | opaque ID | Derived. |
| `expiresAt` | timestamp | Echoed from `OfferSent` — the scheduled expiry. |
| `expiredAt` | timestamp | When the expirer actually fired. Difference with `expiresAt` enables slippage analysis. |

Payload intentionally minimal. All other context is on `OfferSent`; no duplication.

#### Silent race-rejection (not an error)

When the handler loads the RideRequest and finds the offer already has a terminal disposition (Accepted / Declined / Revoked), the command is silently dropped. No `OfferExpired` is emitted; no alarm surfaces. This is the expected race-resolution — the expirer raced with another actor and lost. Logged for SRE visibility only.

#### GWT sketches

**Happy path — offer reaches expiry cleanly**
```
Given: OfferSent { offerId: O1, rideRequestId: X, driverId: D1, expiresAt: T+20s }
  And: no terminal disposition for O1
  And: current time advances to T+20s
When: OfferExpirer finds O1 stale and issues ExpireOffer { offerId: O1, expiredAt: T+20s }
Then: OfferExpired { offerId: O1, rideRequestId: X, driverId: D1, expiresAt: T+20s, expiredAt: T+20s } is emitted
  And: the offer leaves OffersAwaitingExpiry* and ActiveOffersForDriver
```

**Race — accepted before expirer commits**
```
Given: OfferSent { offerId: O1, expiresAt: T+20s }
  And: OfferAccepted { offerId: O1 } committed at T+19.9s
When: OfferExpirer picks up O1 from a stale todo-list snapshot and issues ExpireOffer at T+20.05s
  And: the handler loads RideRequest and finds OfferAccepted for O1
Then: the command is silently dropped (OFFER_ALREADY_DISPOSED)
  And: no OfferExpired is emitted
  And: the offer's terminal state is Accepted (from Slice 5.5)
```

**Batch expiry — broadcast round times out**
```
Given: OfferSent O1, O2, O3 in one round, all expiresAt = T+20s
  And: none were accepted or declined before T+20s
When: OfferExpirer scans at T+20s and finds all three stale
Then: three independent ExpireOffer commands are issued
  And: three OfferExpired events are emitted (one per offer)
  And: the round completes without acceptance; Slice 9 picks up from RequestRounds
```

**Policy change mid-flight (Bruun carry-the-value)**
```
Given: OfferSent { offerId: O1, expiresAt: T+20s } (computed from offerExpirySeconds = 20)
  And: at T+5s, operator emits DispatchPolicyConfigured { offerExpirySeconds: 10 }
When: OfferExpirer scans at T+11s
Then: O1 is NOT treated as expired (its carried expiresAt is still T+20s)
  And: O1 expires at its originally-scheduled T+20s
Confirms: policy changes do not retroactively shift in-flight expiry.
```

#### Views affected

- `OffersAwaitingExpiry*` — row removed.
- `ActiveOffersForDriver` — removed from driver's streaming view.
- `OfferRegister` — terminal state `Expired` recorded.
- `RequestTimeline` — adds `OfferExpired`.
- `RequestRounds` — disposition tracking; feeds Slice 9.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `OfferLatencyMetrics` | Operations, matching-quality analytics, ML | Per-offer intervals: send-to-disposition, send-to-delivery (post-MVP), `expiresAt → expiredAt` (expirer slippage). Aggregated by driver, region, vehicle class. | All offer-lifecycle events | Defer |
| `ExpiryHotspotsByRegion` | Operations, Pricing (future surge signals) | Per geographic region × hour: expiry rate. Supply/demand imbalance indicator. | `OfferExpired`, `OfferSent` | Defer; revisit during Pricing workshop |
| `DriverResponsiveness` | Driver Profile, Trust & Safety, ML ranking | Per `driverId`: accept / decline / expire (ignored) / revoke rates over a rolling window | All offer-lifecycle events | Defer; natural home is Driver Profile |
| `ExpirerSlippageAudit` | Operations, SRE | `expiredAt - expiresAt` distribution. Automation-health telemetry. | `OfferExpired` | Defer but pin — valuable observability when system runs under load |

Note: `ExpirerSlippageAudit` is an example of an **observability-grade projection** — the event stream used as telemetry about the system itself, not about the domain. Worth deliberate use whenever a slice introduces an automation with timing sensitivity.

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| `OfferExpired` payload | Minimal: IDs + `expiresAt` + `expiredAt`. All other context already on `OfferSent`; do not duplicate. Principle: avoid stream bloat; replicate only when it aids stream autonomy. |
| Implementation mechanism | Deferred to skill-file / implementation time. Will be Wolverine scheduled messages or Marten async daemon — Critter Stack primitives only, no hand-rolled polling. |
| Batching | One event per expired offer. Keeps stream grain consistent with other offer-lifecycle events; batching is a potential future optimization at most. |
| Silent race-rejection | Confirmed. No event on loss; log only. |
| Automation cadence / SLA | No number locked; observability via `ExpirerSlippageAudit`. |
| `OfferExpirer` as Saga or scheduled process | Skill-file decision; event model is agnostic. |

#### Notation notes

- `OffersAwaitingExpiry*` — asterisk suffix (first appearance).
- `OfferExpirer` — clock-rewind glyph on the gear (first appearance).
- Establishes the pattern for subsequent temporal slices in the workshop and beyond.

#### Cross-references

- **Backward:** `OfferSent` (Slice 4) feeds `OffersAwaitingExpiry*`.
- **Removal feeders:** Slices 5.5, 5.6, 5.8 all remove rows via their own dispositions.
- **Forward (Slice 9):** `OfferExpired` contributes to round disposition; all-expired rounds trigger re-dispatch evaluation.
- **Consumed parameter:** `DispatchPolicyConfigured.offerExpirySeconds` — read *once* at `OfferSent` (Slice 4) and carried; not re-read here.
- **Temporal automation cross-ref (§7):** First entry; establishes the pattern for future temporal slices (e.g., `RequestAbandonmentAutomation` in Slice 9).

### 5.8 Slice 8 — RideRequestCancelled (rider cancels pre-assignment)

**Pattern:** Command Pattern with cascaded revocation — structurally parallel to Slice 5.5 (driver-accept + sibling-revoke).
**Lane:** Dispatch.
**Trigger:** Rider app — rider taps "Cancel" on their active request.

#### Flow on the board

```
  Rider app (cancel action with reason)
           │
           ▼
     ┌──────────────────────┐
     │ CancelRideRequest    │  blue command
     └──────────┬───────────┘
                │  [AggregateHandler] loads RideRequest
                │  validates state, rider, notes
                ▼
     ┌───────────────────────────────────────┐
     │ Atomic multi-event emission:          │
     │   • RideRequestCancelled              │
     │   • OfferRevoked ×N (outstanding,     │
     │     reason: RIDER_CANCELLED)          │
     │   + outgoing ASB message (Payments/   │
     │     Pricing may consume for fee logic)│
     └───────────────────────────────────────┘
                │
                ▼
   Views updated: OffersAwaitingExpiry* (remove all outstanding),
                  ActiveOffersForDriver (remove for each affected driver),
                  ActiveRequestsByRider (remove for this rider),
                  RequestTimeline, OfferRegister
```

#### Command — `CancelRideRequest`

| Field | Shape | Notes |
|---|---|---|
| `rideRequestId` | opaque ID | |
| `riderId` | opaque ID | Authenticated rider; must match the Request's `riderId`. |
| `reason` | enum | Required. |
| `notes` | string? (bounded) | Required when `reason = OTHER`. |
| `cancelledAt` | timestamp | |

#### Event — `RideRequestCancelled`

| Field | Shape |
|---|---|
| `rideRequestId` | opaque ID |
| `riderId` | opaque ID |
| `reason` | enum `{ CHANGED_MIND, WAITING_TOO_LONG, FOUND_ALTERNATE_TRANSPORT, WRONG_PICKUP_LOCATION, OTHER }` |
| `notes` | string? |
| `cancelledAt` | timestamp |

#### Revocation cascade — `OfferRevoked { reason: RIDER_CANCELLED }`

Re-uses the shape defined in Slice 5.5b. Emitted once per outstanding offer (zero events if cancel arrives before any `OfferSent`). Same transactional commit as `RideRequestCancelled`.

#### Rejection reasons

`REQUEST_ALREADY_ASSIGNED`, `REQUEST_ALREADY_CANCELLED`, `REQUEST_ALREADY_ABANDONED`, `NOT_THE_RIDER`, `NOTES_REQUIRED`.

#### GWT sketches

**Happy path — cancellation mid-round with outstanding offers**
```
Given: RideRequested, FareQuoted, CandidatesSelected for X
  And: OfferSent O1 (D1), O2 (D2), O3 (D3); all outstanding
When: CancelRideRequest { rideRequestId: X, riderId: R, reason: CHANGED_MIND, cancelledAt: T+8s }
Then: the following events are emitted atomically on the RideRequest stream:
        RideRequestCancelled { ..., reason: CHANGED_MIND, cancelledAt: T+8s }
        OfferRevoked { offerId: O1, driverId: D1, reason: RIDER_CANCELLED, revokedAt: T+8s }
        OfferRevoked { offerId: O2, driverId: D2, reason: RIDER_CANCELLED, revokedAt: T+8s }
        OfferRevoked { offerId: O3, driverId: D3, reason: RIDER_CANCELLED, revokedAt: T+8s }
  And: outgoing ASB message published with RideRequestCancelled payload (consumed by Payments/Pricing for cancellation-fee policy)
```

**Cancel before any offers**
```
Given: RideRequested for X (FareQuoted pending, no offers yet)
When: CancelRideRequest { rideRequestId: X, riderId: R, reason: CHANGED_MIND }
Then: RideRequestCancelled emitted
  And: no OfferRevoked events (none exist to revoke)
  And: in-flight FareQuoteAutomation work for X is harmless — FareQuoted (if it later arrives) lands on a terminal Request stream and triggers no downstream automation.
```

**Cancel with OTHER — notes required**
```
Given: RideRequested for X, rider R
When: CancelRideRequest { ..., reason: OTHER, notes: null }
Then: rejected with NOTES_REQUIRED
```

**Cancel after assignment — rejected from Dispatch**
```
Given: RideAssigned for X has been emitted
When: CancelRideRequest for X
Then: rejected with REQUEST_ALREADY_ASSIGNED
  And: the rider's app is redirected to cancel via the Trip (Trips BC).
```

**Concurrent cancel + accept race**
```
Given: OfferSent O1 outstanding for X
When: CancelRideRequest for X and AcceptOffer for O1 arrive concurrently
Then: first-to-commit wins the Request stream version
  If cancel wins: RideRequestCancelled emitted; AcceptOffer rejected with REQUEST_CANCELLED
  If accept wins: RideAssigned emitted; CancelRideRequest rejected with REQUEST_ALREADY_ASSIGNED
```

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `RiderCancellationRateByReason` | Rider Profile, Trust & Safety, product analytics | Per `riderId` × `reason`: rolling counter | `RideRequestCancelled` | Defer to Rider Profile |
| `TimeToCancelDistribution` | Operations, Pricing, ML | Per request: `RideRequested → RideRequestCancelled` interval | `RideRequested`, `RideRequestCancelled` | Defer |
| `CancellationFeeEligibility` | Payments / Pricing | Per cancellation: timing, lifecycle stage, reason | `RideRequested`, `RideRequestCancelled`, `OfferAccepted` | Defer; Payments concern, not Dispatch |
| `DriverWastedEffortAudit` | Driver Profile, Operations | Per driver: offers revoked via rider cancel, with timing | `OfferSent`, `OfferRevoked { reason: RIDER_CANCELLED }` | Defer |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Reason enum for v1 | `CHANGED_MIND`, `WAITING_TOO_LONG`, `FOUND_ALTERNATE_TRANSPORT`, `WRONG_PICKUP_LOCATION`, `OTHER`. `SAFETY_CONCERN` deferred to Trust & Safety with distinct handling. |
| `WAITING_TOO_LONG` UX surfacing | UI-driven "cancel, taking too long" button after threshold. Pure UX; no modeling impact. |
| Cancellation-fee policy | Out of Dispatch scope. Payments/Pricing decide via ASB subscription. |
| Notes required for OTHER | Yes; reject with `NOTES_REQUIRED` when missing. |
| Race semantics | First-to-commit on the Request stream wins. Marten optimistic concurrency native. |
| Cross-BC publication | Publish `RideRequestCancelled` as an ASB business event alongside the local events, via Wolverine's `OutgoingMessages` + `Events` in the same handler (outbox-coordinated). |

#### Cross-references

- **Backward:** Triggered from any state between `RideRequested` and terminal events.
- **Forward:** Terminal event for the request on Dispatch's side.
- **Cross-BC:** Payments / Pricing consume via ASB for potential cancellation-fee logic.
- **Reuses:** `OfferRevoked` shape from Slice 5.5b with `reason = RIDER_CANCELLED`.

### 5.9 Slice 9 — Re-dispatch and RideRequestAbandoned

**Pattern:** Two distinct automations reaching a shared terminal event.
- **9a — Re-dispatch (event-driven automation).** Reacts to round completion without acceptance and decides widen-and-retry vs. abandon.
- **9b — Request-level timeout (temporal automation, second Bruun pattern).** Clock-rewind glyph + asterisk todo-list. "Stuck request watchdog" — guarantees no request lives forever regardless of round state.

**Lane:** Dispatch.

#### Flow on the board

```
  9a (event-driven, RedispatchAutomation):
    NoCandidatesAvailable | FareQuoteFailed | RequestRounds transition to NO_ACCEPTANCE
           │
           ▼
    ┌──────────────────────┐
    │ RedispatchAutomation │ (⚙)
    └──────────┬───────────┘
               │ decides: widen-retry or abandon
               │
       ┌───────┴────────┐
       ▼                ▼
  emit CandidatesSelected   AbandonRideRequest
  (round N+1, widened)      → RideRequestAbandoned
                              + OfferRevoked ×N


  9b (time-driven, RequestAbandonmentAutomation):
    RequestsAwaitingAbandonment*  (todo-list, fed by RideRequested)
           │
           │ now() >= abandonAt
           ▼
    ┌──────────────────────┐
    │ RequestAbandonment   │  (⚙ + ⏪)
    │ Automation           │
    └──────────┬───────────┘
               ▼
         AbandonRideRequest
         { reason: REQUEST_TIMEOUT }
         → RideRequestAbandoned
           + OfferRevoked ×N
```

Both paths converge on the same `AbandonRideRequest` command and `RideRequestAbandoned` event. Handler is idempotent — first-to-commit wins; the other silently observes a terminal state.

#### 5.9a — Re-dispatch decision logic

| Condition | Decision |
|---|---|
| `roundNumber < maxRounds` AND request still within global timeout AND a wider search may help | **Retry.** Emit `CandidatesSelected` for `roundNumber + 1` with widened radius; prior-round decliners excluded. |
| `roundNumber >= maxRounds` | **Abandon** with `MAX_ROUNDS_EXHAUSTED`. |
| `NoCandidatesAvailable { reason: NO_CAPABLE_DRIVERS_IN_RANGE }` in final allowed round | **Abandon** with `NO_CAPABLE_DRIVERS_AVAILABLE`. |
| `FareQuoteFailed` with exhausted attempts | **Abandon** with `FARE_QUOTE_FAILED`. |

**Widen-radius policy (linear):** `round N radius = searchRadiusMeters + (N-1) × searchRadiusStepMeters`, capped at `searchRadiusMaxMeters`. Upgrade path to exponential or ML-driven is a policy change with no event-shape impact.

**Decliners exclusion:** drivers who emitted `OfferDeclined` for this `rideRequestId` in any prior round are excluded from subsequent candidate pools. Implemented as a per-request filter on `NearbyAvailableDrivers`.

#### 5.9b — Request-level timeout (temporal)

- **Todo-list:** `RequestsAwaitingAbandonment*` — rows `{ rideRequestId, abandonAt }`. Fed by `RideRequested`; rows removed on any terminal event.
- **Automation:** `RequestAbandonmentAutomation` — gear + clock-rewind glyph.
- **Trigger:** `now() >= abandonAt`.
- **Hard cap:** no "extend timeout" escape hatch in v1. Advisory timeouts defeat the watchdog purpose.

#### Shared command — `AbandonRideRequest`

| Field | Shape |
|---|---|
| `rideRequestId` | opaque ID |
| `reason` | enum `{ MAX_ROUNDS_EXHAUSTED, NO_CAPABLE_DRIVERS_AVAILABLE, FARE_QUOTE_FAILED, REQUEST_TIMEOUT }` |
| `abandonedAt` | timestamp |

Issued by either `RedispatchAutomation` or `RequestAbandonmentAutomation`.

#### Shared event — `RideRequestAbandoned`

| Field | Shape | Notes |
|---|---|---|
| `rideRequestId` | opaque ID | |
| `reason` | enum (same as command) | |
| `abandonedAt` | timestamp | |
| `finalRoundNumber` | int | How many rounds were attempted. Useful for projections. |

#### Cascade on abandonment

Handler atomically emits `RideRequestAbandoned` + `OfferRevoked { reason: REQUEST_ABANDONED }` for any still-outstanding offers. `REQUEST_ABANDONED` was added to the `OfferRevoked.reason` enum for honest signal (not reusing `RIDER_CANCELLED` when the system decided).

#### GWT sketches

**9a — Max rounds exhausted**
```
Given: two rounds of offers fully disposed without acceptance
  And: DispatchPolicyConfigured { maxRounds: 2 }
When: RedispatchAutomation observes round 2 completion and roundNumber == maxRounds
Then: AbandonRideRequest { rideRequestId: X, reason: MAX_ROUNDS_EXHAUSTED }
       → RideRequestAbandoned { ..., finalRoundNumber: 2 }
       + outgoing ASB message
```

**9a — Successful widening**
```
Given: round 1 offers all declined/expired without acceptance
  And: DispatchPolicyConfigured { maxRounds: 3, searchRadiusMeters: 5000, searchRadiusStepMeters: 2500 }
When: RedispatchAutomation observes round 1 completion
Then: CandidatesSelected { rideRequestId: X, roundNumber: 2, searchParameters: { searchRadiusMeters: 7500, ... } } is emitted
  And: round 1 decliners excluded from the candidate pool
```

**9a — Immediate abandon on fare-quote failure**
```
Given: FareQuoteFailed { rideRequestId: X, reason: PRICING_UNAVAILABLE, attemptCount: 3 }
When: RedispatchAutomation reacts
Then: RideRequestAbandoned { ..., reason: FARE_QUOTE_FAILED, finalRoundNumber: 0 } is emitted
```

**9b — Request watchdog fires**
```
Given: RideRequested at T
  And: DispatchPolicyConfigured { maxRequestDurationSeconds: 180 }
  And: request still Outstanding at T+180s
When: RequestAbandonmentAutomation finds X stale
Then: RideRequestAbandoned { ..., reason: REQUEST_TIMEOUT } is emitted
  And: any outstanding offers revoked with reason REQUEST_ABANDONED
```

**Race — 9a and 9b fire near-simultaneously**
```
Given: max-rounds reached at nearly the same wall-clock time as timeout
When: both automations issue AbandonRideRequest concurrently
Then: first-to-commit wins the Request stream version
  And: the loser silently sees REQUEST_ALREADY_ABANDONED
  And: exactly one RideRequestAbandoned event exists
```

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `AbandonmentReasonBreakdown` | Operations, Pricing, ML | Per region × hour × reason | `RideRequestAbandoned` | Defer but pin |
| `RequestOutcomeFunnel` | Operations, product | Conversion funnel: Requested → Quoted → FirstCandidate → FirstOffer → terminal | All lifecycle events | Defer |
| `WideningEffectivenessAnalysis` | Operations, ML | Per-round × radius: conversion rate of widened rounds | `CandidatesSelected`, `OfferAccepted`, `RideRequestAbandoned` | Defer |
| `TimeoutSlippageAudit` (observability) | SRE | `abandonedAt - abandonAt` for `REQUEST_TIMEOUT` abandonments | `RideRequestAbandoned`, `RequestsAwaitingAbandonment*` history | Defer but pin (parallel to `ExpirerSlippageAudit`) |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Widen-radius policy | Linear: `baseline + (N-1) × step`, capped at max. |
| New `DispatchPolicyConfigured` parameters | `maxRounds`, `searchRadiusStepMeters`, `searchRadiusMaxMeters`, `maxRequestDurationSeconds`. |
| `OfferRevoked.reason` extension | Add `REQUEST_ABANDONED` for honest signal. Enum now `{ SIBLING_ACCEPTED, RIDER_CANCELLED, REQUEST_ABANDONED }`. |
| Shared command/event between 9a and 9b | Yes. Idempotent handler; first-to-commit wins. |
| Watchdog as hard cap | Yes. No extend-timeout escape hatch in v1. |
| 9a trigger shape | Projection-driven via `RequestRounds` view transitions (consistent with Automation Pattern: View → automated trigger). |
| Cross-BC publication | Publish `RideRequestAbandoned` as ASB business event. Operations, Pricing, Rider Profile are natural consumers. |

#### Cross-references

- **Backward:** `NoCandidatesAvailable` (Slice 3), `FareQuoteFailed` (Slice 2), round-completion from Slices 5.6, 5.7, 5.5b; `RideRequested` (Slice 1) populates the 9b watchdog todo-list.
- **Cross-BC:** Payments, Pricing, Rider Profile, Operations consume via ASB.
- **Consumed parameters (Slice 11):** `maxRounds`, `searchRadiusStepMeters`, `searchRadiusMaxMeters`, `maxRequestDurationSeconds`.
- **Temporal automation cross-ref (§7):** second entry — `RequestsAwaitingAbandonment*` + `RequestAbandonmentAutomation`.

### 5.10 Slice 10 — RideAssigned (handoff to Trips)

**Pattern:** Translation (out). The event itself is emitted atomically in Slice 5.5's handler; this slice documents the event shape, the cross-BC contract, and the shared-identifier principle.
**Lane:** Dispatch (event) with ASB publication crossing into Trips.
**Trigger:** Atomic emission from the `AcceptOffer` handler in Slice 5.5.

#### Flow on the board

```
  (Slice 5.5 handler atomically emits OfferAccepted + OfferRevoked×N + RideAssigned
   plus queues the outgoing ASB publication via Wolverine OutgoingMessages)
                                    │
                                    ▼
                          ┌───────────────────────┐
                          │ RideAssigned          │  orange event (Dispatch stream)
                          └──────────┬────────────┘
                                     │ outbox-coordinated
                                     ▼
                  ┌──────────────────────────────────────┐
                  │ ASB Topic: dispatch.ride-assigned    │
                  │ Session key: rideRequestId (= tripId)│
                  │ Payload: Protobuf contract (§9)      │
                  └────────────────┬─────────────────────┘
                                   │
                                   ▼
                  ┌──────────────────────────────────────┐
                  │ Trips BC — Translation-in slice      │
                  │  [modeled at the Trips workshop]     │
                  │  Idempotent intake: Trip aggregate   │
                  │  created with shared ID.             │
                  └──────────────────────────────────────┘
```

#### Event — `RideAssigned`

| Field | Shape | Notes |
|---|---|---|
| `rideRequestId` | opaque ID | **Same value as the future `tripId`** on Trips' side. Canonical ride identifier across all BCs. |
| `offerId` | opaque ID | The accepted offer — for audit traceability back through Slice 5.5. |
| `riderId` | opaque ID | |
| `driverId` | opaque ID | |
| `pickup` | `{ lat, lon, streetAddress? }` | |
| `dropoff` | `{ lat, lon, streetAddress? }` | |
| `vehicleClass` | enum | |
| `fareAmount` | integer (minor units) | |
| `currency` | ISO 4217 code | |
| `fareBreakdown` | `{ base, distance, time, fees? }` | Full transparency carried forward. |
| `pricingPolicyVersion` | opaque | Enables later fare-dispute audit. |
| `notesForDriver` | string? | |
| `assignedAt` | timestamp | Same moment as `acceptedAt` in Slice 5.5. |

Payload is deliberately denormalized — Trips has everything it needs to stand up its `Trip` aggregate without calling back into Dispatch.

#### Cross-BC publication contract

| Aspect | Value |
|---|---|
| Transport | Azure Service Bus (ADR-005: business event) |
| Topic | `dispatch.ride-assigned` (convention: `<source-bc>.<event-name-kebab>`) |
| Session key | `rideRequestId` — per-ride ordering guarantee |
| Ordering | Strict within a session (per ride); no guarantee across sessions |
| Delivery | At-least-once; Trips' intake handler is idempotent, keyed on `rideRequestId` (= `tripId`) |
| Outbox | Wolverine's outbox coordinates the local event append + ASB publication atomically |
| Protobuf contract | §9, full message shape recorded |

#### Shared identifier principle

Applies to all ride-related BCs across CritterCab:

| BC | Field name | Value |
|---|---|---|
| Dispatch | `rideRequestId` | generated at `SubmitRideRequest` receipt (UUID v7) |
| Trips | `tripId` | same opaque value |
| Payments | `paymentReference` (tentative) | same opaque value, possibly prefixed — Payments workshop decides |
| Ratings | `rideId` | same opaque value |

Rationale: single canonical identifier traversing all BCs means cross-BC queries about a specific ride are lookups, not joins against cross-reference tables. Documented as ADR candidate §11 item 7.

#### Trips' intake (flagged; modeled at the Trips workshop)

- Trips subscribes to `dispatch.ride-assigned` as a Wolverine message handler.
- Handler creates a `Trip` aggregate keyed on the shared ID.
- Idempotency: if `tripId` already exists on Trips' side, the handler no-ops (at-least-once delivery assumed).
- This is a Translation-in slice for Trips; full treatment belongs to that workshop.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `AssignmentLatencyMetrics` | Operations, SRE, product | Per-request: `RideRequested → RideAssigned` interval, bucketed by region, vehicle class, round number. | `RideRequested`, `RideAssigned` | Defer but pin — core product KPI |
| `CrossBcTraceProjection` | SRE, Operations, support tooling | Per `rideRequestId`: joined timeline across Dispatch, Trips, Payments, Ratings events. Answers "what happened with ride X." | `RideAssigned` + all downstream BCs' events referencing the shared ID | Defer; candidate for a separate research note on the "Context Graph" pattern from `docs/research/agents-in-event-models.md` |
| `DriverAcceptanceConversion` | Driver Profile, ML ranking | Per driver: offers received → accepted → trips completed | `OfferSent`, `OfferAccepted`, `RideAssigned`, + Trips' completion events | Defer; cross-BC |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Shared identifier | `rideRequestId = tripId = paymentReference(ish) = rideId`. One value across BCs. |
| ASB topic naming convention | `<source-bc>.<event-name-kebab>` — applies to all ASB publications in CritterCab. |
| Session ordering | Keyed on `rideRequestId`. Strict per-ride ordering for downstream consumers. |
| Protobuf contract | Full message shape authored in §9 as the first cross-BC contract. |
| Context Graph / cross-BC trace projection | Defer; candidate for a dedicated research note. |
| Trips intake idempotency key | `rideRequestId` (= `tripId`). Promised contract to downstream consumers. |

#### Cross-references

- **Backward:** Emitted atomically in Slice 5.5's handler alongside `OfferAccepted` and sibling `OfferRevoked` events.
- **Cross-BC:** Trips (primary), Operations (live ride map), Rider Profile (notify rider), Driver Profile (mark driver on-ride).
- **Translation slices §6:** new row — Dispatch → Trips, out, decision (the assignment *is* a decision).
- **Protobuf surface §9:** full message shape recorded.
- **ADR candidates §11:** shared-identifier principle, ASB topic naming convention.

### 5.11 Slice 11 — ConfigureDispatchPolicy (configuration-as-events)

**Pattern:** Command Pattern, operator-initiated. Consolidates the configuration event that nine earlier slices have been consuming.
**Lane:** Dispatch.
**Trigger:** Operator admin console — operator adjusts dispatch policy.

#### Flow on the board

```
  Operator admin console (policy editor UI)
           │
           ▼
     ┌──────────────────────────┐
     │ ConfigureDispatchPolicy  │  blue command
     └──────────┬───────────────┘
                │  [AggregateHandler] on the DispatchPolicy singleton aggregate
                │  Wolverine compound handler: Validate() + Before() + Handle()
                │  cross-parameter constraint validation
                ▼
     ┌──────────────────────────┐
     │ DispatchPolicyConfigured │  orange event (DispatchPolicy stream)
     └──────────┬───────────────┘
                ▼
       DispatchPolicy view (projection of latest policy)
       consumed by: Slices 5.2, 5.3, 5.4, 5.7, 5.9a, 5.9b
```

#### Command — `ConfigureDispatchPolicy`

| Field | Shape | Notes |
|---|---|---|
| `operatorId` | opaque ID | Authenticated upstream (API gateway + Operations BC). |
| `fareQuoteRetryAttempts` | int (bounded 0–10) | |
| `fareQuoteTimeoutSeconds` | int (bounded 1–30) | |
| `searchRadiusMeters` | int (bounded 500–20000) | |
| `searchRadiusStepMeters` | int (bounded 0–10000) | |
| `searchRadiusMaxMeters` | int (bounded 1000–50000) | |
| `maxCandidatesPerRound` | int (bounded 1–20) | |
| `offerExpirySeconds` | int (bounded 5–120) | |
| `maxRounds` | int (bounded 1–10) | |
| `maxRequestDurationSeconds` | int (bounded 30–600) | |
| `reason` | string? | Operator note explaining the change. |
| `configuredAt` | timestamp | |

**Full-replacement semantics:** every command carries all parameters; every event is a complete snapshot.

#### Event — `DispatchPolicyConfigured`

Payload mirrors the command exactly.

#### Aggregate — `DispatchPolicy` (singleton for v1)

- **Stream ID:** well-known opaque constant (e.g., fixed UUID `00000000-0000-0000-0000-000000000001` or named key `"dispatch-policy"`). Not derived from any entity.
- **State:** projection of the latest policy values.
- **Concurrency:** Marten optimistic concurrency; concurrent operator edits serialize.
- **Regional extension** (per-region policy) is a post-MVP enhancement that changes the stream-ID shape without changing the event shape.

#### Cross-parameter validation (Wolverine compound handler)

Implementation idiom: Wolverine compound handler with `Validate()` catching type-level and cross-parameter inconsistency before `Handle()` commits. Sample constraints:

| Constraint | Rejection reason |
|---|---|
| `searchRadiusMaxMeters >= searchRadiusMeters + (maxRounds-1) × searchRadiusStepMeters` | `INVALID_WIDENING_BOUNDS` |
| `maxRequestDurationSeconds > maxRounds × (offerExpirySeconds + slack)` | `TIMEOUT_TOO_TIGHT` |
| All integers within bounded ranges | `OUT_OF_RANGE` (type-level; rejected upstream) |

Type-level checks (ranges, required fields) are enforced by C# 14 records and attribute validation before the handler fires; handler-level `Validate()` catches cross-parameter rules that depend on parameter relationships.

#### Bootstrap default event

Per ADR candidate §11 item 5, on first deployment a seed event is appended with documented defaults. The exact mechanism (migration-time seed, startup self-seed, or refuse-until-configured) is deferred to the ADR. Illustrative seed:

```
DispatchPolicyConfigured {
  operatorId: "system-bootstrap",
  fareQuoteRetryAttempts: 3, fareQuoteTimeoutSeconds: 2,
  searchRadiusMeters: 5000, searchRadiusStepMeters: 2500, searchRadiusMaxMeters: 15000,
  maxCandidatesPerRound: 5, offerExpirySeconds: 20,
  maxRounds: 3, maxRequestDurationSeconds: 180,
  reason: "Initial deployment defaults",
  configuredAt: <deployment timestamp>
}
```

#### GWT sketches

**Happy path**
```
Given: prior DispatchPolicyConfigured (or bootstrap seed) exists on the stream
When: ConfigureDispatchPolicy { operatorId: OP1, offerExpirySeconds: 15, ..., reason: "Tighten during low supply" }
  And: all parameters within bounds; cross-parameter constraints hold
Then: DispatchPolicyConfigured { ... } is emitted
  And: the DispatchPolicy view updates
  And: in-flight offers (with carried expiresAt from prior policy) retain their originally-scheduled expiry. Bruun carry-the-value confirmed.
```

**Cross-parameter rejection — widening bounds inconsistent**
```
Given: any prior policy state
When: ConfigureDispatchPolicy { searchRadiusMeters: 5000, searchRadiusStepMeters: 5000, maxRounds: 5, searchRadiusMaxMeters: 10000, ... }
       (round 5 radius = 5000 + 4×5000 = 25000, exceeds max 10000)
Then: command rejected with INVALID_WIDENING_BOUNDS
```

**Cross-parameter rejection — timeout too tight**
```
Given: any prior policy state
When: ConfigureDispatchPolicy { maxRequestDurationSeconds: 30, maxRounds: 5, offerExpirySeconds: 20, ... }
       (5 × 20 = 100s minimum needed, exceeds 30s watchdog)
Then: command rejected with TIMEOUT_TOO_TIGHT
```

**Concurrent edit race**
```
Given: two operators editing simultaneously
When: both submit ConfigureDispatchPolicy
Then: first to commit wins the DispatchPolicy stream version
  And: the second receives a clean rejection (POLICY_CHANGED_CONCURRENTLY)
  And: the operator UI prompts the loser to refresh and re-apply.
```

#### Views fed

- **`DispatchPolicy`** — current policy projection; consumed by every policy-dependent automation.
- **`PolicyHistory`** — reverse-chronological timeline for audit.

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `PolicyAuditLog` | Operations admin console, compliance | Chronological change list with operator, timestamp, diff, reason | `DispatchPolicyConfigured` | Defer but pin |
| `PolicyVersionAtTime` | Audit, dispute resolution | Given a timestamp T, what policy was in effect? | `DispatchPolicyConfigured` | Defer but pin — load-bearing for later audit ("what expiry applied to offer X at T-5s?") |
| `PolicyChangeFrequencyByOperator` | Trust & Safety, Operations | Per operator × window: count of changes | `DispatchPolicyConfigured` | Defer |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Aggregate model for v1 | Singleton (one well-known stream). Regional-singleton is post-MVP. |
| Command semantics | Full replacement — every command carries all parameters. |
| Cross-parameter validation location | In the handler via Wolverine's compound-handler pattern (`Validate()` + `Handle()`). Type-level validation upstream. |
| Scheduled policy changes (`effectiveAt` future) | Post-MVP. v1 applies immediately. |
| Bootstrap mechanism specifics | Deferred to ADR (candidate §11 item 5). |
| Authentication/authorization | Out of Dispatch. API gateway + Operations BC upstream. |
| Cross-BC publication | None. `DispatchPolicyConfigured` is internal to Dispatch; Operations' admin UI reads the view via a Dispatch query. |

#### Cross-references

- **Consumed by:** Slices 5.2, 5.3, 5.4, 5.7, 5.9a, 5.9b — every automation with a policy-dependent parameter.
- **ADR candidates §11 item 5:** bootstrap strategy; decided when the first implementation session starts.
- **Operations BC interaction:** admin UI → Dispatch query → `DispatchPolicy` view; no ASB subscription.

### 5.12 Slice 12 — Post-assignment cancellation signal from Trips (Translation-in placeholder)

**Pattern:** Translation (in from Trips). **Contract stub** — full treatment belongs to the Trips workshop. Purpose here: name the expected contract surface and Dispatch's intake behavior so the Trips workshop has a target to satisfy.
**Lane:** Dispatch.
**Trigger:** ASB message from Trips signaling a post-assignment outcome (rider cancelled, driver no-show, pickup abandoned).

#### Flow on the board

```
   ┌──────────────────────────────────┐
   │ Trips BC                         │
   │   [modeled at Trips workshop]    │
   │                                  │
   │   Dispatch's preferred shape:    │
   │   single TripTerminatedEarly     │
   │   event with reason enum         │
   │   (not committed; Trips decides) │
   └──────────────┬───────────────────┘
                  │ ASB publication
                  │ session: rideRequestId (= tripId)
                  ▼
   ┌──────────────────────────────────┐
   │ Dispatch Translation-in handler  │
   │ (Wolverine message handler)      │
   │ Maps Trips' reason → Dispatch    │
   │ outcome enum (BC-owned).         │
   │ Idempotent.                      │
   └──────────────┬───────────────────┘
                  ▼
   ┌──────────────────────────────────┐
   │ AssignmentOutcomeRecorded        │  orange event (Dispatch-local,
   │                                  │  appended to Ride Request stream
   │                                  │  past the terminal RideAssigned —
   │                                  │  annotative, not state transition)
   └──────────────────────────────────┘
```

#### Trips-side contract (to be authored at Trips workshop)

**What Dispatch needs (interface specification, not implementation):**
- A stable event Trips publishes when an assigned trip terminates before normal completion.
- Carries `rideRequestId` (= `tripId`) matching Dispatch's assignment.
- Carries a reason category distinguishing rider-cancel, driver-no-show, pickup-abandonment.
- Delivered via ASB with session ordering on `rideRequestId`.

**Dispatch's preferred shape** (recorded as input to the Trips workshop, not committed):

```
message TripTerminatedEarly {
  string ride_request_id = 1;          // = tripId
  TerminationReason reason = 2;
  optional string detail = 3;
  google.protobuf.Timestamp terminated_at = 4;
  // ... other fields Trips decides
}

enum TerminationReason {
  TERMINATION_REASON_UNSPECIFIED = 0;
  RIDER_CANCELLED_POST_ASSIGNMENT = 1;
  DRIVER_NO_SHOW = 2;
  PICKUP_ABANDONED = 3;
  DRIVER_CANCELLED = 4;
  OTHER = 5;
  RIDER_NO_SHOW = 6;                    // added 2026-05-09 per Workshop 002 §6.9 amendment
}
```

A single event with a reason enum (rather than per-cause distinct events) is cleaner for Dispatch's subscription. Trips workshop is empowered to choose otherwise; this is preference, not constraint.

#### Dispatch-local event — `AssignmentOutcomeRecorded`

Emitted by the Translation-in handler when Trips' event is observed. **BC-owned enum** (per cross-BC enum discipline) decoupled from Trips' reason vocabulary.

| Field | Shape | Notes |
|---|---|---|
| `rideRequestId` | opaque ID | Correlates with `RideAssigned`. |
| `outcome` | enum (Dispatch-owned) `{ RIDER_CANCELLED_POST_ASSIGNMENT, DRIVER_NO_SHOW, PICKUP_ABANDONED, DRIVER_CANCELLED, OTHER, RIDER_NO_SHOW }` | Mapped from Trips' reason in the handler; allows independent enum evolution. **`RIDER_NO_SHOW` added 2026-05-09 per Workshop 002 §6.9 amendment** (see §5.12 amendment subsection below). |
| `observedAt` | timestamp | When Dispatch received the ASB message. |
| `tripEventOccurredAt` | timestamp | From the Trips event payload. |
| `tripEventReference` | opaque? | Optional identifier from Trips for cross-stream audit. |

Appended to the **Ride Request stream** past `RideAssigned`. The request status stays `Assigned`; this event is **annotative** — observation about post-terminal state, not a state transition. First (and only) place in the workshop where the Request stream extends past terminal.

#### Idempotency contract

Handler keyed on `(rideRequestId, tripEventReference)`. Re-deliveries are ack'd silently; no second `AssignmentOutcomeRecorded` emitted.

#### Error handling

Lean on Wolverine and Critter Stack defaults: retry-with-backoff, dead-letter queue on persistent failure. No custom error handling for v1.

#### GWT sketches (placeholder; firm at Trips workshop)

**Happy path — rider cancels post-assignment**
```
Given: RideAssigned { rideRequestId: X } on Dispatch's stream
  And: Trips publishes TripTerminatedEarly { rideRequestId: X, reason: RIDER_CANCELLED_POST_ASSIGNMENT, terminatedAt: T+60s }
When: Dispatch's Translation-in handler receives the ASB message
Then: AssignmentOutcomeRecorded {
        rideRequestId: X,
        outcome: RIDER_CANCELLED_POST_ASSIGNMENT,
        observedAt: now(),
        tripEventOccurredAt: T+60s
      } is emitted
```

**Duplicate delivery**
```
Given: AssignmentOutcomeRecorded { rideRequestId: X, tripEventReference: T1 } already exists
When: Trips re-delivers the same TripTerminatedEarly message
Then: handler observes prior event and acks silently
  And: no duplicate event emitted
```

#### Candidate projections for future consumers

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `AssignmentOutcomeMetrics` | Operations, product, ML | Per region × time × outcome: assignments completed vs. terminated early | `RideAssigned`, `AssignmentOutcomeRecorded`, + Trips' completion events | Defer but pin |
| `DriverReliabilityScore` | Driver Profile, Trust & Safety, ML ranking | Per driver: assignment-to-completion ratio; no-show rate | `RideAssigned`, `AssignmentOutcomeRecorded { outcome: DRIVER_NO_SHOW }` | Defer; Driver Profile owns |
| `WastedAssignmentAudit` | Payments, Driver Profile | Per driver: assignments terminated through no fault of theirs | `RideAssigned`, `AssignmentOutcomeRecorded { outcome: RIDER_CANCELLED_POST_ASSIGNMENT }` | Defer; Payments concern |
| `EarlyCancelHotspots` | Operations, Pricing | Geographic clusters of early cancellations; supply-demand mismatch signal | `AssignmentOutcomeRecorded` | Defer |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Local event emitted on observation | Yes — `AssignmentOutcomeRecorded` per Klefter "decisions as events." |
| BC-owned enums | Yes — Dispatch's outcome enum independent of Trips' reason enum; mapped at boundary. |
| Driver reliability scoring | Driver Profile owns the projection; Dispatch only emits the signal. |
| Scope at this workshop | Contract stub only. Trips workshop produces the upstream event; may require a return-pass on this slice. |
| Error handling | Wolverine retry + dead-letter defaults; no custom logic. |
| Trips-side event shape preference | Single `TripTerminatedEarly` with reason enum is Dispatch's preferred shape; Trips workshop is not bound. |

#### Notation notes

- Trips side drawn as a grey/dashed box with candidate event names listed inside, labeled "to be modeled at Trips workshop."
- Dispatch's Translation-in is a small gear sticky.
- `AssignmentOutcomeRecorded` is an orange event on Dispatch's Ride Request stream, drawn *past* `RideAssigned` with an annotation: "post-terminal annotation, not a state transition."
- This is the only place on the board where the Request stream extends past its terminal — visually noteworthy.

#### Cross-references

- **Backward (cross-BC):** Triggered by Trips' upstream event (finalized at Trips workshop — see [Workshop 002 §6.9](./002-trips-event-model.md) and the amendment subsection below).
- **On Dispatch's stream:** appended to the same Ride Request stream as `RideAssigned`; annotates the post-terminal story without transitioning state.
- **Cross-BC consumers:** Driver Profile, Payments, Operations subscribe to `AssignmentOutcomeRecorded` via ASB.
- **Trips workshop deliverable:** Trips authored its event model in [Workshop 002 (2026-05-09)](./002-trips-event-model.md); see the amendment subsection below for the override of §5.12's preferred unified-event shape.

#### Workshop 002 update (2026-05-09)

[Workshop 002 §6.9](./002-trips-event-model.md) ran the Trips workshop and **overrode this slice's preferred unified-event shape**. The override was deliberate, with documented rationale (mirrors Workshop 001's own pattern of distinct events for distinct semantics — `RideRequestCancelled` vs. `RideRequestAbandoned` in §5.8/§5.9). Workshop 001 §5.12's original framing explicitly anticipated this override possibility ("preference, not constraint" / "Trips workshop is empowered to choose otherwise").

**Trips' actual contract surface (4 distinct ASB topics, not 1 unified):**

| Trips topic | Maps to Dispatch `outcome` |
|---|---|
| `trips.trip-completed` | (Not mapped — see "Happy-path scope" below.) |
| `trips.trip-cancelled-by-rider` | `RIDER_CANCELLED_POST_ASSIGNMENT` |
| `trips.trip-cancelled-by-driver` | `DRIVER_CANCELLED` |
| `trips.trip-abandoned-as-no-show` | `RIDER_NO_SHOW` *(new enum value)* |

**Enum amendments (this revision):**

- `RIDER_NO_SHOW` **added** to both the Trips-side preferred `TerminationReason` enum and the Dispatch-local `outcome` enum. Resolves the semantic gap surfaced by Workshop 002 §11 — `TripAbandonedAsNoShow` is specifically rider no-show, distinct from `DRIVER_NO_SHOW` (driver flaked) and `PICKUP_ABANDONED` (catch-all). Original `PICKUP_ABANDONED` value retained for unanticipated cases.
- `ASSIGNMENT_COMPLETED_NORMALLY` **deliberately not added.** Workshop 002 §11 surfaced this as a question; the resolution is that Workshop 001 §5.12's original framing was "post-assignment **early termination** signal" — the happy path was always implicit. Dispatch records `AssignmentOutcomeRecorded` only for non-success terminals; `trips.trip-completed` is observed by Dispatch (for projections like `AssignmentLatencyMetrics`) but does not fire `AssignmentOutcomeRecorded`. Implicit-happy-path is now explicit in this amendment.

**Handler mapping (concrete):**

Dispatch's Translation-in handler now subscribes to **three** Trips topics (the cancellation pair + the no-show terminal); maps each to the appropriate `outcome` enum value; emits one `AssignmentOutcomeRecorded` per inbound event. The `trips.trip-completed` topic is observed by Dispatch's other consumers (projections) but not by this slice's `AssignmentOutcomeRecorded` handler.

**Decision history:** §5.12 v0.2 (2026-04-24) preferred the unified `TripTerminatedEarly` shape; Workshop 002 §6.9 (2026-05-09) overrode with four distinct events; this amendment formalizes the override on Dispatch's side and closes the two enum-gap parking-lot items from Workshop 002 §11.

---

## 6. Translation Slices at BC Boundaries (cross-reference)

Separated out because they carry contract implications beyond Dispatch's internal behavior. Each translation is either *in* (we consume something external and produce a local event — Klefter decision-event pattern applies where a decision is made) or *out* (we publish an event consumed externally).

| Slice | Direction | Counterparty BC | Translation event (local) | Klefter decision? |
|---|---|---|---|---|
| 5.2 | out | Pricing | `FareQuoted` / `FareQuoteFailed` | Yes — the fare is a computed decision; local event preserves it independently of Pricing. |
| 5.3 | in (decision) | Telemetry + Driver Profile | `CandidatesSelected` / `NoCandidatesAvailable` | Yes — the selection is a decision with search parameters and policy version captured. |
| 5.8 | out | Payments / Pricing (via ASB) | `RideRequestCancelled` | Yes — cancellation metadata informs cancellation-fee policy. |
| 5.9 | out | Operations / Pricing / Rider Profile (via ASB) | `RideRequestAbandoned` | Yes — abandonment reason informs supply-demand signals and rider notifications. |
| 5.10 | out | Trips (primary) + Operations / Rider Profile / Driver Profile (via ASB) | `RideAssigned` | Yes — assignment decision with full ride context denormalized; Trips creates its aggregate from this. |
| 5.12 | in | Trips (via ASB) | `AssignmentOutcomeRecorded` (BC-owned outcome enum mapped from Trips' reason) | Yes — observation of a post-terminal outcome captured locally for audit and projections. Annotative, not state-transitioning. |

---

## 7. Temporal Automation Slices (cross-reference)

Separated out because they use the Bruun notation convention: clock-rewind glyph on the automation sticky, asterisk suffix on the todo-list read model it consumes.

| Slice | Todo-list view | Automation | Terminal event |
|---|---|---|---|
| fed by 5.4, consumed by 5.7 | `OffersAwaitingExpiry*` | `OfferExpirer` (Slice 7, clock-rewind glyph) | `OfferExpired` (Slice 7) |
| fed by 5.1, consumed by 5.9b | `RequestsAwaitingAbandonment*` | `RequestAbandonmentAutomation` (Slice 9b, clock-rewind glyph) | `RideRequestAbandoned` (Slice 9, reason `REQUEST_TIMEOUT`) |

---

## 8. Configuration-as-Events (cross-reference)

Slices in which operator configuration is captured as event-sourced policy rather than a settings table. Each row names the configuration event, the consumers that join it into read models, and the parameters it carries.

| Config event | Consumers | Parameters (accumulated as slices are walked) |
|---|---|---|
| `DispatchPolicyConfigured` | Slices 5.2, 5.3, 5.4, 5.9 | `fareQuoteRetryAttempts`, `fareQuoteTimeoutSeconds`, `searchRadiusMeters`, `maxCandidatesPerRound`, `offerExpirySeconds`, `maxRounds`, `searchRadiusStepMeters`, `searchRadiusMaxMeters`, `maxRequestDurationSeconds` |

---

## 9. Candidate Protobuf Contract Surface

Names only, per ADR-009. Proto-file authorship happens downstream. This section is the workshop's output into the `.proto` design backlog.

| Contract | Direction | Likely RPC shape | Notes |
|---|---|---|---|
| `GetFareQuote` | Dispatch → Pricing | gRPC unary | Req: `rideRequestId`, `pickup`, `dropoff`, `vehicleClass`, `requestedAt`. Resp: `fareAmount`, `currency`, `breakdown`, `validUntil`, `pricingPolicyVersion`. Errors: `NO_COVERAGE`, `INVALID_ROUTE` as structured (non-transient); transient failures surface as gRPC status codes the automation retries. |
| `ActiveOffersForDriver` | Dispatch → Driver App | gRPC server-streaming | Driver app subscribes per session; receives a stream of the driver's current outstanding offers. Payload: offer envelope with offerId, ride request summary, fare, expiresAt, pickup/dropoff context. Wolverine 5.32 streaming showcase surface. |
| `RideAssigned` (business event) | Dispatch → Trips (+ fan-out to Operations / Rider Profile / Driver Profile) | ASB topic `dispatch.ride-assigned`, session-ordered by `rideRequestId` | Full message shape (see below). Trips is the primary consumer; others subscribe for projections. Outbox-coordinated with local event append. |
| `RideRequestCancelled` (business event) | Dispatch → Payments / Pricing (+ Operations, Rider Profile) | ASB topic `dispatch.ride-request-cancelled`, session-ordered by `rideRequestId` | Payload: rideRequestId, riderId, reason, notes?, cancelledAt. Consumers decide cancellation-fee logic / notifications. |
| `RideRequestAbandoned` (business event) | Dispatch → Operations / Pricing / Rider Profile | ASB topic `dispatch.ride-request-abandoned`, session-ordered by `rideRequestId` | Payload: rideRequestId, reason, abandonedAt, finalRoundNumber. Consumers use for supply-demand signals and rider notification. |

#### RideAssigned Protobuf message (full shape)

```protobuf
syntax = "proto3";
package crittercab.dispatch.v1;

import "google/protobuf/timestamp.proto";

// Business event published to ASB when a driver accepts an offer.
// Topic: dispatch.ride-assigned  |  Session key: ride_request_id
message RideAssigned {
  string ride_request_id = 1;      // Shared ride identifier (= tripId on Trips)
  string offer_id = 2;              // The accepted offer
  string rider_id = 3;
  string driver_id = 4;
  Location pickup = 5;
  Location dropoff = 6;
  VehicleClass vehicle_class = 7;
  int64 fare_amount_minor_units = 8;
  string currency = 9;              // ISO 4217
  FareBreakdown fare_breakdown = 10;
  string pricing_policy_version = 11;
  optional string notes_for_driver = 12;
  google.protobuf.Timestamp assigned_at = 13;
}

message Location {
  double lat = 1;
  double lon = 2;
  optional string street_address = 3;
}

enum VehicleClass {
  VEHICLE_CLASS_UNSPECIFIED = 0;    // proto3 convention
  VEHICLE_CLASS_STANDARD = 1;
  VEHICLE_CLASS_PREMIUM = 2;
  VEHICLE_CLASS_ACCESSIBLE = 3;
}

message FareBreakdown {
  int64 base_minor_units = 1;
  int64 distance_minor_units = 2;
  int64 time_minor_units = 3;
  optional int64 fees_minor_units = 4;
}
```

Proto file will live at `/protos/dispatch/v1/ride_assigned.proto` once the shared proto directory is established (ADR-009). Generated C# stubs are build artifacts, not checked in.

The `RideRequestCancelled` and `RideRequestAbandoned` business-event messages follow the same conventions (kebab-case topic, session key, minor-units for money, `google.protobuf.Timestamp` for timestamps). Their full shapes are straightforward and can be authored alongside `RideAssigned` at the same proto-authoring session.

---

## 10. Parking Lot and Open Questions

Pre-seeded with items from scope-setting. Additional items accumulate during the slice walk. Each entry carries a disposition decision at session close: become an open question in §10, become an ADR candidate in §11, become a follow-up research note, or drop.

1. **Pricing BC physical location.** Inside Trips at first, split out later, or separate from day one? Touches the `FareQuoted` Translation slice. Deferred.
2. **Fan-out topology upgrade path.** Broadcast is MVP. Reconsider serial or tiered when matching quality becomes a concern; the per-candidate `OfferSent` shape preserves that option.
3. **Surge pricing.** Post-MVP. Revisit during Pricing workshop.
4. **Candidate-selection projection ownership.** Does the nearby-available-drivers view live inside Dispatch (projected from upstream events) or inside Telemetry/Driver Profile (queried by gRPC)? Has downstream consequences for consistency semantics and transport selection.
5. **Post-assignment rider cancellation race window.** Between `RideAssigned` (Dispatch emit) and `TripCreated` (Trips emit), where does a cancellation land? Paved by routing post-assignment cancels to Trips only, with Dispatch rejecting them at the command layer. Verify at Trips workshop.
6. **Dispatch as a candidate for agentic automation.** Re-dispatch under supply-demand pressure is a plausible future agent host per `docs/research/agents-in-event-models.md`. Not modeled as such now; flagged for later.
7. **Scheduled rides (post-MVP).** Pre-booking a ride for a future time adds `scheduledFor` to the request shape and changes when fare quoting, candidate selection, and offer fan-out happen. Distinct enough to warrant its own milestone or second workshop.
8. **"Book a ride for someone else" (post-MVP).** Requires either multiple concurrent active requests per rider *or* a `requestedBy` vs. `rider` separation on the event. Defer until actively demanded.
9. **Rider-suspension propagation.** Suspension is currently assumed to be enforced at the API gateway via token revocation. If Rider Profile carries a suspension state independent of token lifetime, Dispatch needs either a Translation-in view or a gRPC query at command time. Revisit when Rider Profile is modeled.
10. **Road-network ETA as a future gRPC counterparty.** Broadcast MVP uses straight-line distance for match scoring. A future `GetRouteEta(driverLocation, pickup)` gRPC unary would give road-network-aware ranking. Open placement question: internal routing service, external provider via ACL, or a projection that Telemetry maintains on its existing location substrate. Revisit when serial/tiered topology is considered.
11. **Driver-candidate race during selection.** Same driver may be a candidate for two requests near-simultaneously. Tolerated by design; resolved at acceptance via `OfferRevoked` (Slice 5). Monitor under load to confirm no pathological starvation.
12. **`OfferDelivered` acknowledgment event.** Post-MVP. Distinguishes "emitted but not received" from "received but ignored" — valuable production monitoring, no demo audience until the system runs at scale. Adds a driver-app protocol addition (ACK frame on the streaming RPC), one new event, one view. Extension, not breaking.
13. **`notesForDriver` visibility policy.** Visible on `OfferSent` for v1. Post-MVP policy concern: some platforms hide pickup notes until after acceptance to mitigate bias/discrimination on the basis of note content. Revisit when the Trust & Safety BC is being actively modeled.
14. **`SAFETY_CONCERN` as a decline reason.** Deliberately not in v1's `OfferDeclined.reason` enum. When a driver has a safety concern (area, rider flag), the handling path should not be a normal decline (which gets aggregated into behavioral analytics). Belongs to Trust & Safety with a distinct command, event, and routing. Revisit when Trust & Safety BC is being modeled.

---

## 11. ADR Candidates Surfaced by This Workshop

Pre-seeded from scope-setting and slice discussion. Each candidate is a one-line statement of what would be decided, not the decision itself.

1. **Service-topology ADR.** Does Dispatch remain a single deployable, or does the candidate-offer-assignment layering (per DoorDash's DeepRed / `docs/research/ride-sharing-lessons-learned.md` §2) become a three-service split? Trigger: first implementation of the offer-fan-out slice.
2. **Fan-out-topology ADR.** Broadcast for MVP, with the serial/tiered upgrade path committed as a future decision. Documenting this now prevents the MVP choice being misread as permanent.
3. **Candidate-projection-ownership ADR.** Where the nearby-available-drivers view lives, and what consistency guarantee it offers to Dispatch.
4. **Pricing-location ADR.** When Pricing is actively modeled, decide inside-Trips vs. separate-BC.
5. **Configuration-as-events bootstrap strategy ADR.** *Authored as [ADR-011](../decisions/011-configuration-as-events-bootstrap.md) on 2026-05-10.* How does a configurable BC reach a valid policy state on first deployment? Candidate patterns: (a) seed event appended by migration/init script with documented defaults, (b) startup-time self-seed if the stream is empty, (c) refuse to serve traffic until at least one `*PolicyConfigured` event is present. This pattern will recur across every configurable BC in CritterCab; picking the canonical approach now and documenting it as an ADR prevents per-BC drift. Trigger: second BC that adopts configuration-as-events.
6. **RideRequest as single aggregate; Offer lifecycle events on its stream.** *Authored as [ADR-012](../decisions/012-aggregate-per-invariant.md) on 2026-05-10.* Offer is a sub-entity of RideRequest, not an independent aggregate. All offer events (`OfferSent`, `OfferAccepted`, `OfferDeclined`, `OfferExpired`, `OfferRevoked`) are appended to the RideRequest stream. Marten optimistic concurrency on the stream enforces the "at most one `OfferAccepted` per request" invariant natively. This pattern (aggregate-per-invariant, not aggregate-per-noun) will recur across Critter Stack use in CritterCab and deserves a canonical ADR before the second aggregate design surfaces elsewhere. Trigger: first implementation session touching the Dispatch aggregate, or the Trips BC modeling session.
7. **Shared ride identifier across BCs.** *Authored as [ADR-013](../decisions/013-shared-cross-bc-identifier.md) on 2026-05-10.* `rideRequestId` (Dispatch) = `tripId` (Trips) = `paymentReference`-equivalent (Payments) = `rideId` (Ratings). One canonical opaque UUID per ride, flowing through all ride-related BCs. Eliminates cross-reference tables and simplifies cross-BC queries. Trigger: Trips BC modeling session or the first cross-BC integration.
8. **ASB topic naming convention.** *Authored as [ADR-014](../decisions/014-asb-topic-naming-convention.md) on 2026-05-10.* `<source-bc>.<event-name-kebab>` — e.g., `dispatch.ride-assigned`, `dispatch.ride-request-cancelled`, `identity.rider-registered`. Established in this workshop; should be formalized as an ADR before the second BC publishes to ASB. Trigger: second BC's first ASB publication, or the Identity BC workshop.

---

## 12. Retrospective

### 12.1 Workshop intent vs. outcome

Stated goal at session start: pressure-test the methodology by tackling the architectural spine (Dispatch), establish workflows, learn what works, then repeat the pattern for subsequent BCs.

**Outcome:** Workshop produced a complete event model for Dispatch covering 12 slices — 13 events plus their alternates, 2 temporal automations (Bruun pattern), 1 configuration-as-events slice, 6 translation slices across 4 BC boundaries, 8 ADR candidates, 14 parking-lot items, and a candidate Protobuf surface with one full proto message authored inline. Notation patterns from Bruun (temporal automation, todo-list asterisks, configuration-as-events, carry-the-value) and Klefter (decisions-as-events, BC-owned enums, promoted translations) were applied and demonstrably load-bearing rather than decorative. Critter Stack idioms (aggregate handlers, Wolverine `Events`/`OutgoingMessages`, compound handlers, Marten optimistic concurrency) integrated cleanly with the modeling vocabulary. **Goal met.**

### 12.2 What worked

- **Slice-by-slice interactive cadence.** Walking → confirming → committing prevented retroactive churn; the artifact only ever reflects agreed-upon modeling. Cost: long sessions when slices were rich (5.5, 5.9). Net positive.
- **Pre-commit summary discussion + post-commit artifact update.** Two-stage rhythm meant decisions had reasoning attached when committed, not just verdicts.
- **Klefter decision-event pattern paid off six times** (Slices 5.2, 5.3, 5.5b, 5.8, 5.9, 5.10, 5.12). "Is there a decision happening here?" was a useful reach-for-a-local-event trigger.
- **Bruun carry-the-value principle was load-bearing on Slice 5.7** (offer expiry) and reused on Slice 5.9b (request abandonment). The discipline prevented retroactive-policy-change footguns.
- **Curated-enum + free-text-`OTHER` pattern reused four times** across `OfferDeclined`, `RideRequestCancelled`, `RideRequestAbandoned`, and `OfferRevoked`.
- **Aggregate-per-invariant modeling** (Slice 5.5) — the Critter Stack idiom ("RideRequest is the aggregate; Offers are sub-entities") cleanly resolved a non-trivial concurrency invariant without distributed locks.
- **Proactive projection proposals** (adopted mid-workshop, Slice 6 onward) — most deferred, but naming the audience + shape + feeders surfaced cross-BC dependencies that wouldn't have appeared in a strict "what does this slice need" analysis.
- **Atomic dual-emit via Wolverine `Events` + `OutgoingMessages`** in Slice 5.5 — the Critter Stack supports the modeling decision natively, no compromise needed.

### 12.3 What was hard / friction

- **Slice 5.5's aggregate-design question was load-bearing but not strictly Event-Modeling-flavored.** It's an implementation/architectural decision masquerading as a modeling decision. Future workshops may benefit from a pre-workshop or mid-workshop sidebar on aggregate identity before slice walking.
- **Cross-BC contract authorship in Slice 10 stretched workshop scope.** Authoring the full Protobuf message inline was an exception worth making once. Default should be deferring proto authorship to a follow-up session.
- **Sub-slice notation (5.5a/5.5b, 5.9a/5.9b) was needed twice** for tightly-coupled cascades. Convention worked; might benefit from a formal definition before the next workshop.
- **The proactive-projections directive landed mid-workshop.** Slices 5.1–5.5 don't have the speculative-projection subsections that 5.6–5.12 do. Future workshops should adopt from slice 1.
- **Slice complexity varied significantly** (5.2, 5.3, 5.5, 5.9 were rich; 5.6, 5.8 were lighter). The interactive cadence didn't differentiate. Future workshops could batch the lighter slices.

### 12.4 Decisions about how to model (meta-decisions worth carrying forward)

- Slices commit only after explicit sign-off; no speculative artifact content.
- Each slice closes with a "decisions locked" table capturing both *what* was decided and *why*.
- Cross-cutting concerns are tracked in dedicated cross-reference tables (Translation §6, Temporal §7, Configuration-as-events §8, Protobuf §9), populated incrementally.
- ADR candidates are captured during modeling, not deferred — surfaces architectural questions while context is fresh.
- Parking lot is not a dumping ground; each entry has a disposition (defer, ADR-candidate, post-MVP, drop) named when it lands there.

### 12.5 Patterns established for future workshops

Reusable assets this workshop produces for every subsequent BC workshop:

- **Curated-enum + free-text `OTHER`** for any user/system reason field.
- **BC-owned enums with translation at the boundary** for cross-BC events.
- **Atomic multi-event handlers** for state transitions with cascades (Slice 5.5 reference).
- **Aggregate-per-invariant**, not aggregate-per-noun.
- **Carry the value at decision time** for any value derived from policy that must survive policy changes (Bruun).
- **Singleton aggregate stream** for system-wide configuration events.
- **Translation-in handler emits a local `*Recorded` event** for cross-BC observations the local BC needs to audit / project.
- **ASB topic naming convention `<source-bc>.<event-name-kebab>`** with session ordering on the canonical entity ID.
- **Shared canonical identifier across BCs in the same lifecycle.**
- **Speculative-projections subsection per slice** (proactive-projections discipline).
- **Wolverine Aggregate Workflow follows the Decider Pattern.** Command handler = `decide (command, state) → events`; Marten Apply methods = `evolve (state, event) → state`; default constructor = `initial` state; Marten fold realizes `state = events.Aggregate(initial, evolve)`. Prefer immutable record aggregates with `static` expression-bodied `Apply` methods using C# `with`. Mutable aggregates are valid in Marten but not CritterCab's default.

### 12.6 Adjustments for the next BC workshop

- **Adopt proactive-projections from slice 1**, not mid-workshop.
- **Pre-workshop sidebar on aggregate identity.** For the chosen BC, identify the load-bearing invariant *before* slice walking. That decision shapes every subsequent slice and is hard to retrofit cleanly. 30 minutes well spent.
- **Differentiate slice complexity in the cadence.** Heavy slices (concurrency invariants, aggregate design, cross-BC handoff) warrant extended discussion; lighter slices can be walked briskly. Don't spend equal time on each; calibrate.
- **Defer Protobuf authorship** to a follow-up session unless the workshop explicitly scopes it in.
- **Number sub-slices explicitly when cascades occur** (5.5a/5.5b convention).
- **Pair temporal-automation slices with their feeder slice in narrative order**, not just at cross-reference. Slice 5.7 immediately following 5.4 made the pattern legible.

### 12.7 Quality signal from the session

User feedback at retrospective: experience was "better than expected, with more questions than I anticipated." The practice of pairing open questions with a leaning opinion was explicitly called out as valuable — *"many of the questions came with an opinion that was 'leaning' in a particular direction. That helped me a lot."*

Calibration guidance captured in memory:
- Prefer informative depth over brevity.
- Use ubiquitous language (Ride Request, Offer, Candidate, Dispatch Round) rather than generic software terms.
- Assume DDD / CQRS / Event Sourcing / Event-Driven Architecture as working background.
- Continue pairing questions with leaning opinions.

### 12.8 Follow-ups generated

- **8 ADR candidates** (§11) with specific triggers rather than dates.
- **14 parking-lot items** (§10) for post-MVP and other-BC workshops.
- **Protobuf authorship session** — author `dispatch/v1/ride_assigned.proto` and the two sibling business-event protos as the first concrete cross-BC contracts under ADR-009.
- **Trips BC workshop** — strongly indicated. Slice 5.10's intake and Slice 5.12's contract stub both depend on Trips committing its side.
- **Memory items captured (5 during session):** proactive projection proposals; Critter Stack primitives over bespoke alternatives; BC-owned enums; Decider Pattern / Aggregate Workflow correspondence with immutable-Apply preference; depth/ubiquitous-language/DDD-background communication preferences.

### 12.9 Workshop status

**Complete (v0.2, 2026-04-24).** Event model for the Dispatch bounded context is ready to serve as input to narrative authoring and, in turn, to implementation prompt documents — per the `narrative → prompt → execute → retrospective` workflow in the project's `CLAUDE.md`.

---

## Document History

- **v0.1** (2026-04-24): Scope statement locked. Slice walk pending.
- **v0.2** (2026-04-24): Slice walk complete (12 slices). Cross-references populated (§6 Translation, §7 Temporal, §8 Configuration-as-events, §9 Protobuf surface with full `RideAssigned` proto). 8 ADR candidates and 14 parking-lot items captured. Retrospective committed. Workshop status: complete.
- **v0.3** (2026-05-09): §5.12 amendment per Workshop 002 §6.9. Added `RIDER_NO_SHOW` to both `TerminationReason` (Trips-side preferred) and `outcome` (Dispatch-local) enums. Documented Workshop 002's override of the preferred unified-event shape (4 distinct topics instead of 1). Made the implicit-happy-path scoping decision explicit (`ASSIGNMENT_COMPLETED_NORMALLY` deliberately not added; happy path observed by other Dispatch consumers but does not fire `AssignmentOutcomeRecorded`). Closes Workshop 002 §11 parking-lot items #1 and #2.
- **v0.4** (2026-05-10): §11 cross-reference update. ADR candidates #5, #6, #7, #8 lifted to authored ADRs in the bundled-ADR session: [ADR-011 — Configuration-as-Events Bootstrap Strategy](../decisions/011-configuration-as-events-bootstrap.md), [ADR-012 — Aggregate-per-Invariant](../decisions/012-aggregate-per-invariant.md), [ADR-013 — Shared Cross-BC Identifier](../decisions/013-shared-cross-bc-identifier.md), [ADR-014 — Azure Service Bus Topic Naming Convention](../decisions/014-asb-topic-naming-convention.md). Each candidate entry in §11 carries an inline link to its now-authored ADR. No content change to the candidate descriptions themselves; the workshop's historical reasoning is preserved as authored.
- **v0.5** (2026-05-19): §5.2 happy-path GWT has runnable Alba coverage per [`docs/prompts/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md`](../prompts/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md). `FareQuoteAutomation` is implemented as a Wolverine event handler reacting to forwarded `RideRequested` events; `FareQuoted` is appended to the rider's stream with a stubbed `IPricingClient` returning a canned $21.50 STANDARD-class response. The three §5.2 failure-path GWTs (*Transient failure with retry recovery*, *Exhausted retries*, *Non-transient failure*) and the `FareQuoteAttempts` projection that feeds them remain awaiting implementation in a follow-up slice 5.2 session. The §5.2 *Automation naming* decision (FareQuoteAutomation, avoiding "Coordinator"/"Orchestrator") was exercised end-to-end — Wolverine's handler discovery was customized at the composition root with `Includes.WithNameSuffix("Automation")` so the workshop's term lands directly as the C# class name.
- **v0.7** (2026-06-16): All three §5.3 GWTs (*Happy path*, *No drivers in range*, *Vehicle-class gap — ACCESSIBLE scarcity*) now have runnable Alba coverage per [`docs/prompts/implementations/005-dispatch-slice-5-3-candidates-selected.md`](../prompts/implementations/005-dispatch-slice-5-3-candidates-selected.md). `CandidateSelectionAutomation` reacts to `FareQuoted` via fast-event forwarding, queries `INearbyAvailableDriversSource` (stub seam; transport deferred to parking-lot #4), applies in-process vehicle-class filtering, and emits either `CandidatesSelected` (≥1 eligible candidate, capped to `maxCandidatesPerRound`, ordered by inverse-distance match score) or `NoCandidatesAvailable` (with `NoDriversInRange` or `NoCapableDriversInRange` reason). `DispatchPolicySnapshot` DI record introduced with hardcoded defaults (`searchRadiusMeters: 5000`, `maxCandidatesPerRound: 5`, `policyVersion: "default-v1"`); Slice 11 swaps to `DispatchPolicyConfigured`-fed projection. `RequestRoundsProjection` inline projection introduced (consumed by Slice 9 re-dispatch). `[WriteAggregate]` bound to `FareQuoted` (second event on stream) — first use of this pattern in the codebase; verified against Wolverine 5.37 source that the attribute is trigger-agnostic (stream-position-independent). `ICandidateSelectionOutcome` marker interface is the third occurrence of this pattern in Dispatch (after `IFareQuoteOutcome`); skill-file encoding still queued.
- **v0.6** (2026-05-19): All three §5.2 failure-path GWTs now have runnable Alba coverage per [`docs/prompts/implementations/004-dispatch-slice-5-2-fare-quoted-failure-paths.md`](../prompts/implementations/004-dispatch-slice-5-2-fare-quoted-failure-paths.md). `FareQuoteAutomation` runs a manual 3-attempt retry loop (2-second cooldown; test override 10ms via DI-injected `FareQuoteRetryPolicy`) that emits `FareQuoted` on success, `FareQuoteFailed { reason, attemptCount }` on terminal failure (transient retries exhausted → `PricingUnavailable`; non-transient → carried reason on the first attempt with no further calls). Retry attempts themselves are not emitted as events per the §5.2 *Retry counter durability* decision; in-flight budgeting lives in the handler's loop variable, and `FareQuoteAttempts` is implemented as a **terminal-events-only** projection that records the final `attemptCount`/`outcome`/`failureReason` for operations observability. **Workshop inconsistency surfaced and resolved one-way for this implementation**: §5.2's `## Automation — FareQuoteAutomation` *Reads* list names `FareQuoteAttempts` as consumed pre-retry, but the *Decisions locked in this slice* row (`Retry counter durability` → event-projected) + the *Retry attempts themselves are NOT emitted as events* rule make pre-retry consultation impossible with terminal-only projection. This session committed to terminal-only and flagged the *Reads*-list inconsistency for a workshop-tidy session — either drop `FareQuoteAttempts` from §5.2's *Reads* or evolve the projection to per-attempt event-fed if the pre-retry consultation is genuinely needed. `DispatchPolicy` view + `DispatchPolicyConfigured` event remain deferred to Slice 11; retry config is hardcoded in this session via `FareQuoteRetryPolicy.Default` per the workshop's defaults. `IFareQuoteOutcome` marker interface introduced as the handler's return type so the compiler can type-check that the handler returns one of the two terminal events Wolverine appends to the stream.
