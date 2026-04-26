---
slug: 001-rider-books-a-ride
status: accepted
journey: rider
perspective: single-rider
scope: happy-path
bounded_contexts: [Dispatch]
boundaries_touched: [Pricing, Telemetry, Driver Profile, Trips]
slices_implemented: [5.1, 5.2, 5.3, 5.4, 5.5, 5.10]
canonical_id: rideRequestId
---

# Rider Books a Ride — Happy Path

A spine narrative: the rider submits a ride request, the system locks a fare, finds eligible drivers, sends offers, and a driver accepts. Single rider, single driver, single-leg trip, no failures along the way. Failure paths — cancel, decline, expire, abandon — belong to subsequent narratives, not as branches inside this story.

This narrative implements the happy-path slices of the Dispatch event model (workshop 001). It cites slice numbers; it does not restate the workshop's Given/When/Then scenarios.

## Cast

- **Maya Okafor** — the rider. Authenticated CritterCab user, has a payment method on file, has no active Ride Request when the story opens. Maya is the only protagonist; the narrative is told entirely from her vantage point.
- **Dani Rivera** — the driver who ultimately gets the assignment. Offstage participant. Maya never sees Dani deliberate over the offer; she only sees the outcome — that an assignment happened and a specific driver is on the way.
- **Dispatch** — the narrating system. Owns the Ride Request lifecycle from submission through assignment, and emits the cross-BC handoff that wakes Trips up.
- **Pricing** — offstage. Returns the authoritative fare when Dispatch asks. Pricing's internals are not narrated.
- **Telemetry and Driver Profile** — offstage. Their projections (driver locations, availability, vehicle-class capability) populate Dispatch's candidate-selection view. The rider never observes these directly; they shape the cast of candidates the system can choose from.
- **Trips** — offstage. Wakes up only at acceptance, when Dispatch publishes the assignment as a business event Trips consumes to stand up its own Trip aggregate. The handoff is the last thing this narrative covers; everything Trips does next is its own narrative.

## Setting

A weekday afternoon in a metropolitan service area with healthy STANDARD-class supply — a normal Tuesday at 2pm, not surge hours, not the morning rush. Maya is standing at the curb in front of her apartment; her destination is the Eastbridge Library, roughly twelve minutes away by car. She has the CritterCab app open.

Dispatch policy is at bootstrap defaults: a five-kilometer search radius, up to five candidates per round, twenty-second offer expiry, up to three rounds before abandonment. The defaults shape several moments in this story; subsequent narratives will explore the same journey under tighter or looser policy.

No surge is in effect. Pricing returns its standard fare on first attempt; no retries are needed. There are more than enough STANDARD-capable drivers within five kilometers; candidate selection finds its full quota on the first round. The first driver who sees the offer accepts within ten seconds. This is the cleanest possible run through Dispatch — every other narrative documents what happens when reality is messier.

## Moment 1 — Maya asks for a ride

**Implements:** slice 5.1.

**Context.** Maya stands at the curb with the CritterCab app open. She is authenticated, her account is in good standing, and she has no active Ride Request. The pickup field has already pre-filled with her current GPS coordinates. A pre-submission fare estimate hovers near the bottom of the screen — a non-authoritative range queried directly from Pricing for display purposes; it isn't yet a fact about her ride and it isn't recorded anywhere.

**Interaction.** Maya types "Eastbridge Library" into the dropoff field. She leaves vehicle class on STANDARD — the default. In the optional notes field she types "meet at side entrance" so her driver won't circle the wrong lot. She taps Request Ride.

**Response.** Dispatch treats her command as the first event in a new lifecycle. A fresh `rideRequestId` is server-assigned — the canonical identifier that will travel with this ride across every bounded context it touches, all the way through to whatever Trips, Pricing, and Ratings will eventually do with it. Dispatch records `RideRequested` on a new stream keyed on that ID, capturing pickup, dropoff, vehicle class, the notes for driver, and the server-clock `requestedAt` timestamp. Two views pick up the new fact: `ActiveRequestsByRider` registers Maya as having an outstanding request, and `RequestTimeline` opens its first row for this ride. Maya's screen transitions to the request-status view: "Request received — finding your ride."

**Why this matters to the rider.** Maya now holds the system's only outstanding Ride Request slot for her account. If she were to submit another request before this one reaches a terminal state, Dispatch would reject the second submission. The slot only releases when the system declares this ride assigned, cancelled, or abandoned — none of which has happened yet. From this moment forward, everything that follows is bound to this one `rideRequestId`.

## Moment 2 — The fare is locked

**Implements:** slice 5.2.

**Context.** `RideRequested` is on the Ride Request stream and Maya's status screen still reads "Request received — finding your ride." Pricing is offstage but reachable. This is the first of several moments Maya does not personally drive — from here through assignment, the system is acting on its own initiative in response to facts it has just recorded. Maya watches.

**Interaction.** Dispatch's fare-quote automation reacts to `RideRequested`. It assembles a single question for Pricing — pickup, dropoff, vehicle class, the moment of request — and sends it across the BC boundary as a synchronous request out.

**Response.** Pricing answers on the first attempt with an authoritative fare: $21.50, broken down into base, distance, time, and no additional fees. Dispatch treats the answer as a decision worth keeping and records it as `FareQuoted` on Maya's Ride Request stream — the local event captures the amount, the breakdown, the pricing-policy version Pricing used, and a `validUntil` timestamp that, for the v1 system, effectively never lapses. The fare is now Dispatch's committed truth about what this ride costs. Maya's status screen ticks forward: "Fare confirmed — $21.50."

**Why this matters to the rider.** From this moment on, $21.50 is the price. No driver's acceptance, no candidate selection, no later round of dispatch can re-open the number. If Pricing's algorithm changes, if surge starts, if the route shifts — none of it touches her quote. The fare on Maya's screen is the fare she will pay (modulo legitimate trip-time adjustments, which belong to Trips, not to Dispatch). The pre-submission estimate has done its job; this authoritative number replaces it.

## Moment 3 — The system finds candidate drivers

**Implements:** slice 5.3.

**Context.** `RideRequested` and `FareQuoted` are both on the Ride Request stream. Maya's status screen still reads "Fare confirmed — $21.50." The Setting's policy defaults are now load-bearing: a five-kilometer search radius and a per-round candidate cap of five. Two views maintained inside Dispatch from upstream BCs are about to be consulted: `NearbyAvailableDrivers`, kept fresh by Telemetry's location updates and Driver Profile's availability and capability state, and `DispatchPolicy`, projected from the policy-events stream. Maya sees none of this; she sees a status screen.

**Interaction.** Dispatch's candidate-selection automation reacts to `FareQuoted` and asks the question Maya's request reduces to: *which available, capable drivers are close enough to be worth offering this ride to?* It reads `NearbyAvailableDrivers` filtered by Maya's pickup location and her STANDARD vehicle class, and it reads `DispatchPolicy` for the search radius and the per-round candidate cap.

**Response.** The view returns six candidates within five kilometers of Maya's curb, all in STANDARD-class vehicles, all currently online and eligible. The automation orders them by match score — for v1, the inverse of straight-line distance — and keeps the top five. Dispatch records this as `CandidatesSelected` on Maya's stream: round number 1, the five chosen drivers with their scores, distances, and ETAs, the search parameters that produced them, and the policy version that governed the search. Like the fare, the selection is treated as a decision worth keeping. Maya's status screen ticks forward: "Notifying nearby drivers."

**Why this matters to the rider.** The candidate set is now closed for this round. Drivers who came online a heartbeat too late, drivers who left the radius right before the search ran, drivers in the wrong vehicle class — none of them are in the running. The five who were selected will all hear from Dispatch in the next moment. The driver who ultimately accepts will come from this set or, if no one in the set says yes, from a fresh round Dispatch would try with a wider radius. Maya doesn't experience the choosing as choice; the system has already decided who could possibly be her driver.

## Moment 4 — Five offers go out at once

**Implements:** slice 5.4.

**Context.** `CandidatesSelected` is on Maya's stream — five drivers chosen for round 1, ordered by match score. Maya's status screen reads "Notifying nearby drivers." The Setting's twenty-second offer expiry is about to come into play.

**Interaction.** Dispatch's offer-dispatch automation reacts to `CandidatesSelected` and reaches for the facts each candidate will need: the originating `RideRequested` (for pickup, dropoff, vehicle class, the notes Maya wrote for her driver), the `FareQuoted` decision (for the locked $21.50 and its full breakdown), and the current `DispatchPolicy` (for the offer-expiry window). It then issues one send per candidate — five sends in parallel, broadcast topology, first-to-accept wins.

**Response.** Each successful send produces an `OfferSent` event on Maya's Ride Request stream — five events, each with its own `offerId` but all sharing the same `rideRequestId`, round number, fare, fare breakdown, pickup, dropoff, vehicle class, and Maya's "meet at side entrance" note. Each offer carries an `expiresAt` of `sentAt + 20 seconds`, computed once at send-time and *carried on the event itself*: a later policy change to a tighter or looser expiry would not retroactively shift these five offers' deadlines. The five offers populate the system's three offer-tracking views in parallel: `ActiveOffersForDriver` (per-driver projection of outstanding offers, streamed live to each candidate's driver app), `OffersAwaitingExpiry*` (the system's todo-list of offers to expire if nothing else disposes of them first), and `OfferRegister` (per-offer status for ops dashboards). Five drivers — Dani among them — receive Maya's offer on their phones with twenty seconds to accept or decline. Maya's status screen reads "Five drivers notified."

**Why this matters to the rider.** Maya's request is now five simultaneous propositions in five different drivers' hands. Any one of them can take it; whichever one acts first wins and the other four are revoked by Dispatch in the same atomic moment. The window is twenty seconds: if all five let the timer run out, Dispatch rolls into a fresh round. Maya doesn't see who got the offers, doesn't see the countdown clock running down, and — crucially — has no way to direct the assignment toward a particular driver. The narrative arc has split into five parallel offers; whichever resolves first collapses the story back into one.

## Moment 5 — Dani accepts; the assignment is made

**Implements:** slices 5.5 and 5.10.

**Context.** Five `OfferSent` events are on Maya's stream. Five outstanding offers are alive in `OffersAwaitingExpiry*`. Maya's status screen reads "Five drivers notified." The twenty-second clock has started. None of the five has yet accepted or declined.

**Interaction.** Eight seconds in, Dani Rivera — second-closest of the five candidates by match score — taps Accept on his phone.

**Response.** Dispatch's accept-offer handler loads Maya's Ride Request as a single aggregate and validates Dani's tap against everything that could disqualify it: the request hasn't already been assigned, hasn't been cancelled, isn't past its offer's expiry, and Dani is in fact the driver to whom this offer was sent. All checks pass. In one atomic commit against Maya's Ride Request stream, the system emits four facts at once: `OfferAccepted` for Dani's winning offer; four sibling `OfferRevoked` events — one per other candidate, all carrying `reason: SIBLING_ACCEPTED` — for the offers that just lost the race; and `RideAssigned`, the terminal event that closes the Ride Request lifecycle. Alongside these four events, the same commit places an outbound business event headed for Trips, carrying the assignment payload keyed on the same `rideRequestId` that has been the through-line since Moment 1. The four sibling drivers see their offer cards disappear from their phones; their rows leave `OffersAwaitingExpiry*` and `ActiveOffersForDriver`; the offer expirer never fires for them because their disposition is already final. Maya's status screen changes for the last time in this narrative: "Dani Rivera is on her way — 4 minutes."

The outbound message lands on Trips' subscription a moment later. Trips creates its `Trip` aggregate keyed on the shared identifier — `tripId` equals `rideRequestId`, the same value Dispatch has carried since Moment 1 and the same value Trips, Pricing, Ratings, and any future BC concerned with this ride will use as the canonical lookup. Trips' intake is idempotent on that identifier, so an at-least-once redelivery would not spawn a duplicate trip. The ride's center of gravity has moved out of Dispatch and onto Trips' timeline.

**Why this matters to the rider.** The system has collapsed five parallel offers into one assignment. Dani is Maya's driver; she has no veto and no choice over the four she didn't get. The fare is locked, the route is committed, the driver is named. Her app's behavior changes: she now tracks Dani's approach, watches the ETA tick down, and waits at the curb she stood at less than a minute ago. Everything Dispatch was for has happened — the Ride Request is now a Trip, on a different bounded context's timeline, in a different chapter of Maya's story.

## Deferred from this narrative

The following were deliberately not narrated in this happy-path spine. Each is named with its disposition so future sessions can pull from this list when scoping the next narrative, ADR, skill file, or implementation prompt. Items here are not bugs or omissions — they are *consciously deferred* and traceable.

### Alternate-path failure modes (each warrants its own narrative)

- **Pricing fails to quote.** `FareQuoteFailed` after exhausted retries; non-transient errors like `NO_COVERAGE` or `INVALID_ROUTE`. Workshop slice 5.2.
- **No candidates available in round 1.** `NoCandidatesAvailable` with reasons `NO_DRIVERS_IN_RANGE`, `NO_CAPABLE_DRIVERS_IN_RANGE`, `ALL_CAPABLE_DRIVERS_OCCUPIED`. Workshop slice 5.3.
- **Re-dispatch with adaptive radius widening; eventual abandonment.** `RideRequestAbandoned` if rounds exhaust. Workshop slice 5.9.
- **Offer expires without acceptance** (temporal automation; Bruun pattern). `OfferExpired` feeds round disposition. Workshop slice 5.7.
- **Driver declines an offer.** `OfferDeclined` with curated-enum reason (`TOO_FAR`, `ROUTE_UNDESIRABLE`, `FARE_TOO_LOW`, `DRIVER_GOING_OFFLINE`, `PREFER_NEXT_OFFER`, `OTHER`) plus free-text notes when `OTHER`. Workshop slice 5.6.
- **Rider cancels before assignment.** `RideRequestCancelled` with sibling `OfferRevoked { reason: RIDER_CANCELLED }` cascade. Workshop slice 5.8.
- **Concurrent-accept race** — two drivers tap Accept simultaneously; optimistic concurrency on the Request stream resolves cleanly; loser is rejected with `REQUEST_ALREADY_ASSIGNED`. Workshop slice 5.5.
- **Stale acceptance / stale decline** (clock crosses `expiresAt` between offer and tap). Workshop slices 5.5 and 5.6.
- **Driver no longer eligible at accept time** (`DRIVER_NO_LONGER_ELIGIBLE`). Workshop slice 5.5.
- **Best-effort partial send failure** (one of N candidates' send throws transiently; remaining candidates still race). Workshop slice 5.4.
- **Driver-went-offline-between-selection-and-send** race. Workshop slice 5.4.
- **Rider-side rejections at submission** — `DUPLICATE_REQUEST`, `RIDER_HAS_ACTIVE_REQUEST`. Workshop slice 5.1.

### Separate narratives (other journey perspectives — narrative #2 candidates)

- **Driver-side journey** — Dani's perspective from going online through receiving the offer, accepting, en-route, completion. **Strongest candidate for narrative #2.**
- **Rider-cancellation journey** — full rider-arc treatment of the cancel path (subset of the cancellation alternate-path above, but as a complete journey rather than a failure mode).
- **Request-abandonment journey** — request times out across rounds without any acceptance.
- **Trust & Safety flag journey** — driver or rider triggers a safety concern; flagged for separate path in workshop slice 5.6.
- **Operator manual reassignment journey** — Operations BC overrides Dispatch; deferred per workshop §2.3.
- **Post-acceptance rider journey** — en-route, arrived, started, in progress, completed, payment, rating. Lives entirely on Trips' timeline.

### Separate workshops (other BCs not yet modeled)

- **Trips' intake handler and trip lifecycle** — referenced at handoff in Moment 5; full treatment belongs to the Trips workshop.
- **Pricing's internal fare-calculation logic** — referenced at the Translation-out boundary in Moment 2.
- **Telemetry's GPS ingest and location-of-record projection** — feeder of `NearbyAvailableDrivers` (Moment 3).
- **Driver Profile's availability and capability state** — feeder of `NearbyAvailableDrivers` and `DriverCapabilities` (Moment 3).
- **Identity / Rider Profile authentication** — referenced as upstream-cleared at Moment 1.

### Post-MVP enhancements (deferred per workshop)

- **Surge pricing** signals via `RequestFailurePatternsByRegion` projection and Pricing surge logic.
- **`OfferDelivered` event** as monitoring-grade signal for offer-delivery success/failure (workshop slice 5.4).
- **Re-quoting cycle** when fares should be refreshable (currently `validUntil ≈ ∞`).
- **Multi-vehicle switching** (driver in-service vehicle changes mid-shift).
- **Discrimination-mitigation policy** for `notesForDriver` visibility (visible only after accept, etc.).
- **Scheduled rides** (event-payload extension on `RideRequested`).
- **Road-network ETA** as match-score v2 (replaces inverse straight-line distance without event-shape change).

### Implementation details (skill files / ADRs, not narratives)

- **Transports per flow** — gRPC unary (Pricing call), gRPC server-streaming (`ActiveOffersForDriver`), ASB business-event publication (Trips handoff). ADR-005 / skill-file territory.
- **ASB topic naming convention** (`<source-bc>.<event-name-kebab>`) and session ordering keyed on the canonical entity ID. Workshop slice 5.10; ADR candidate.
- **Wolverine and Marten primitives** — `[AggregateHandler]`, `Events`, `OutgoingMessages`, compound handlers, scheduled messages, outbox coordination, optimistic concurrency, async daemon. Skill files.
- **Protobuf contract authorship** — `dispatch/v1/ride_assigned.proto` and the two sibling business-event protos. Workshop §9 / dedicated authorship session.
- **View-population mechanism** for cross-BC views (`NearbyAvailableDrivers`, `DriverCapabilities`) — local projection vs. remote query. Parking-lot #4 in workshop; ADR candidate.
- **Trips' idempotent intake keying** on `rideRequestId = tripId`. Trips workshop.
- **Bootstrap policy seeding** mechanism for `DispatchPolicyConfigured`. ADR candidate per workshop §11 item 5.

### UX and UI details (app design, not narratives)

- **Pre-submission fare-estimate range UI.**
- **Request-status screen** state-transition design.
- **Driver-app offer-card UI** with countdown clock and accept/decline gestures.
- **Rider tracking screen** post-assignment (ETA tick-down, vehicle make/model display, driver photo).
- **Notes-for-driver visibility patterns** in the driver app.

## Retrospective

### Narrative intent vs. outcome

Stated goal at session start: pilot CritterCab's narrative document layer; settle the format dialect; produce a journey-shaped spec for the rider's happy path through Dispatch that downstream prompt documents can lift directly from.

**Outcome:** A five-Moment narrative covering slices 5.1, 5.2, 5.3, 5.4, 5.5, and 5.10 of workshop 001 — `RideRequested` through the cross-BC `RideAssigned` handoff. Format dialect locked (Candidate C: structured frontmatter + typed body sections with prose-paragraph Moment bodies). Two guardrails locked (prose-paragraph bodies; bounded frontmatter vocabulary). Single-rider perspective convention locked. Cumulative deferral-tracking discipline established. Five Moments, one Deferred-from-this-narrative section, one retrospective. Goal met.

### What worked

- **Format candidates with sample fragments.** Showing the same Moment 1 in three dialects made the choice concrete; the Gherkin-vs-NDD distinction landed via comparison rather than abstract description. Cost: extra round at session start. Net positive.
- **Moment-by-moment sign-off cadence.** Mirrors workshop 001's slice-by-slice rhythm. Authorial calls flagged before commit; revisions caught at the right grain (e.g., the "five minutes ago" inaccuracy in Moment 5 caught at sign-off, not after).
- **The "decision worth keeping" framing for Klefter's decision-event pattern.** Established in Moment 2; paid off again in Moments 3 and 5 without re-explaining. Demonstrates that pattern-as-prose can replace pattern-as-jargon when the rendering is consistent.
- **Bruun carry-the-value rendered as plain English** ("computed once at send-time and carried on the event itself") in Moment 4. Same playbook as the Klefter framing.
- **Naming Dani at Moment 4 to plant for Moment 5.** Single-rider POV preserved via narrator omniscience; pattern available for future single-perspective narratives where another actor needs foreshadowing.
- **Two-paragraph `Response.` block for multi-slice Moments.** Moment 5 spans slices 5.5 and 5.10; honoring both without fracturing the rider's experience required extending the format gently. Solution: paragraphs grow, labels don't.
- **Per-Moment "Things deliberately not included" subsection + cumulative aggregation at session close.** Caught here as a feedback memory mid-walk; retroactively the right move from Moment 1.
- **Setting carries policy parameters once.** Twenty-second offer expiry, five-kilometer search radius, max-five-candidates — declared in Setting, inherited by Moments. Subsequent narratives diverge here when policy posture differs.

### What was hard / friction

- **Format-vs-NDK confusion at session start.** User initially named "Candidate B" while describing the NDK-inspired option (which was C). Caught and resolved. Lesson: when proposing alternatives, label *and* name them in the prose so misalignments are visible.
- **Slice 5.3's rider-invisibility tension.** The densest workshop slice (Translation-in from two BCs, Klefter decision-event, dispatch-policy parameters consumed) is the *least visible* to the rider. The Moment had to honor structural richness without dramatizing things Maya can't see. Resolution: render the system facts in the Response paragraph as background, keep the rider's experience to the status-screen update.
- **Slice 5.5 + 5.10 in one beat.** Rider experiences assignment and the Trips handoff as one moment; workshop separates them as two slices. Forced an ad-hoc format extension (multi-paragraph Response). Resolution worked but is worth pinning as convention so narrative #2 doesn't re-litigate it.
- **The "five minutes ago" inaccuracy** in Moment 5's Why-matters paragraph. Caught at sign-off. Lesson: elapsed-time references in narrative voice are easy to over-dramatize. Default toward "less than a minute ago" / "moments ago" / dropping the timestamp.
- **Deferred-section sizing.** Comprehensive vs. relevant tension. Resolution: bucketed by disposition, trimmed implementation-details to bucket level rather than per-primitive. Still substantial; that's the practice working as designed.

### Decisions about how to author (meta-decisions worth carrying forward)

- Moments commit only after explicit sign-off; no speculative artifact content.
- Each Moment closes with authorial-calls + things-deliberately-not-included sections in the proposal phase, captured to the file as part of the locked Moment.
- Format conventions (frontmatter schema, body structure, label vocabulary) are locked early and applied uniformly.
- Cross-cutting deferrals are aggregated at session close; per-Moment omissions feed the cumulative section.
- The narrator is omniscient about the system; the rider's POV governs which experiences are *dramatized*, not what is *revealed*.

### Patterns established for future narratives

Reusable assets this session produces for every subsequent narrative:

- **Frontmatter schema (v1):** `slug, status, journey, perspective, scope, bounded_contexts, boundaries_touched, slices_implemented, canonical_id`. New keys require a `docs/narratives/README.md` revision.
- **Moment body structure:** prose paragraphs labeled `Context.` / `Interaction.` / `Response.` / optional `Why this matters to <protagonist>.` Multi-slice Moments grow in paragraphs under existing labels, not in new labels.
- **Code-style backticks for domain events and named projections; plain text for ordinary domain nouns.** `RideRequested`, `ActiveRequestsByRider`, `DispatchPolicy` get backticks; Ride Request, Trip, Offer do not.
- **Single-named protagonist per narrative.** Other actors live in Cast as offstage participants; the narrator may name them in Moments as system facts, but their POV is not dramatized.
- **Setting declares policy posture once.** Subsequent narratives in the same domain differ at Setting (tight supply, surge active, policy reduced, etc.), not at every Moment.
- **Klefter decision-event pattern rendered as "decision worth keeping."** Bruun carry-the-value rendered as "computed once at <moment>, carried on the event itself."
- **Cross-BC handoff folded into the same Moment as the triggering acceptance**, rendered as a second paragraph under `Response.`.
- **Per-Moment "Things deliberately not included"** with disposition tags (defer / post-MVP / separate-narrative / separate-workshop / implementation-detail / alternate-path-failure / UX-or-UI-detail), aggregated into a `## Deferred from this narrative` section at session close.
- **`## Deferred from this narrative` section bucketed by disposition**, mirroring workshop 001's `§10 Parking Lot` and `§11 ADR Candidates` at the narrative layer.

### Adjustments for narrative #2

- **Adopt the per-Moment deferred subsection from Moment 1**, not mid-walk. (Moment 1 of narrative #1 had the practice in retrospect; narrative #2 starts with it.)
- **Pre-walk sidebar on POV asymmetry.** For the chosen journey, identify which slices the protagonist directly experiences vs. observes vs. is unaware of. Drives the Context/Interaction/Response balance per Moment. Analog to workshop 001's "pre-workshop sidebar on aggregate identity" lesson.
- **Format conventions are now stable.** Narrative #2 doesn't re-litigate format candidates; it inherits Candidate C with both guardrails and the patterns established here.
- **Multi-slice Moment convention is pinned.** Use it where the protagonist experiences slices as a single beat.
- **Lean toward dropping elapsed-time references** unless the time is structurally load-bearing (the twenty-second offer expiry in Moment 4 is load-bearing; "less than a minute ago" in Moment 5 is decorative).

### Quality signal from the session

User feedback was clean throughout: every Moment locked as proposed with no revision rounds; explicit appreciation for the deferral discipline ("growing lists we can discuss and assign in the future") which became a feedback memory mid-session. The leaning-opinions-on-questions practice inherited from workshop 001 continued to land.

Calibration captured during the session:

- Explicit deferrals are intended to feed cumulative, assignable backlogs. Memory updated to reflect this nuance.
- A long deferred list is a *feature*, not a smell — workshop and narrative artifact layers each surface different categories of deferred intent.

### Follow-ups generated

- **Narrative #2** — driver-side journey (strongest candidate). Will pair structurally with narrative #1; will absorb several items from the deferred list.
- **`docs/narratives/README.md` update** — encode format conventions established this session (in this same session's deliverable PR).
- **Memory captured (1 during session, 1 reinforced):** explicit-deferrals practice with cumulative-backlog framing.
- **No new ADR candidates surfaced.** The narrative layer doesn't make architectural decisions; ADR candidates remain at the workshop layer.

### Narrative #2 candidate list

In rough order of structural readiness:

1. **Driver-side journey** — Dani's perspective, going online through accepting and beginning the trip. Pairs structurally with narrative #1; consumes many deferred items (driver receives offer, deliberates, accepts; driver-side UX). The strongest candidate.
2. **Rider-cancellation journey** — full rider-arc treatment of cancellation pre-assignment. Subset of slice 5.8, but as a complete journey rather than a failure branch.
3. **Request-abandonment journey** — rider waits while rounds exhaust without acceptance. Tests the narrative format under a "waiting and watching" arc rather than an action-driven one.
4. **Trust & Safety flag journey** — separate handling path per workshop §5.6; involves cross-BC paths to Trust & Safety. Highest novelty; valuable to defer until at least one cross-BC narrative pair is established.

Driver-side narrative #2 is the recommended next session.

### Narrative status

**Complete (v0.1, 2026-04-25).** Five Moments, deferred section, retrospective. Format conventions and patterns ready to be applied to narrative #2 without re-litigation. The narrative is ready to serve as input to implementation prompt documents covering the happy-path slices.

---

## Document History

- **v0.1** (2026-04-25): Initial authoring. Five-Moment spine through happy-path slices 5.1, 5.2, 5.3, 5.4, 5.5, and 5.10. Format dialect locked (Candidate C with two guardrails). Single-rider POV locked. Deferred section + retrospective committed.
