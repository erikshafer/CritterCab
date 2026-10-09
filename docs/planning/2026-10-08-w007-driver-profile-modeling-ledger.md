# W007 Driver Profile — running session ledger (source for minutes; not the minutes)

Session: 2026-10-08. main @ 547f082. Method: docs/skills/event-modeling/SKILL.md. Canvas: EventModelers.AI.
No driverprofile.emodel.yaml authored (waits on CritterMart Orders experiment).

## Grill — boundary question 1: four transitions vs DriverAvailabilityChanged placeholder

Swap test: whatever DP publishes must let Dispatch fill AvailableDriver's four availability-side fields
(DriverId, AvailabilityState, VehicleClass, AvailabilityUpdatedAt) with no change to the document,
NearbyAvailableDriversView or CandidateSelectionAutomation.

- 1a LOCKED (A): DP publishes transitions as distinct integration events, one ASB topic each (ADR-014
  `<source-bc>.<event-name-kebab>`), EACH SELF-SUFFICIENT (driverId + vehicleClass + time; state implied by type).
  Dispatch: one Translation slice, per-type handlers into the existing availability-side upsert; DP vocabulary
  collapses to Dispatch's 3-value DriverAvailabilityState at the boundary. Placeholder deleted.
  Rejected: B (single ECST snapshot — DP would collapse vocabulary for Dispatch; overrides W001/ADR-018/vision/context-map),
  C (multi-type single topic — breaks ADR-014).
  Note: cross-topic ordering is NOT a broker property (ASB sessions are per subscription); ordering comes from the
  availability LWW clock, already built.
- 1b LOCKED (option 1): ChangeVehicle refused unless driver Offline. DriverVehicleChanged is DP-internal, NOT published.
  Published contract = THREE events: DriverCameOnline, DriverWentOnBreak, DriverWentOffline.
  Reason: partial (vehicleClass-only) update under a shared availability LWW clock reproduces the
  "offline driver stays dispatchable" bug (VehicleChanged@:07 arrives before WentOffline@:05 -> :05 discarded);
  DriverCameOnline already carries vehicleId + vehicleClass (W001 §5.3 locked); completeness check: a published
  VehicleChanged.vehicleClass has no destination that changes anything.
  WentOnBreak / WentOffline still carry vehicleClass (in-service vehicle) for self-sufficiency.
  REVISION TO RECORD: W001 §5.3, ADR-018, context map edge #6, vision all name four published events; canvas cut to three.
  ADR-018 explicitly left DP's contract to DP's workshop, so this is in-scope revision, not override.
- 1c LOCKED (i): LWW key = DP server-stamped `occurredAt` (aggregate's recorded event time) -> Dispatch AvailabilityUpdatedAt.
  Same type, so swap. Rejected ii (per-driver stream version: stronger, but a document migration buying protection
  against a race the domain can't produce — per-driver transitions are serialized by the aggregate and human-paced),
  iii (device time; ADR-018 precedent). Symmetry: each side's clock is server-stamped by the context owning that side.
  Skeptic: equal timestamp = no-op under `<=` (right for redelivery; same-ms distinct transitions impossible).

## Grill — boundary question 2: availability-before-location drop

Finding: not an edge case — in the first v2 slice ("driver comes online") the likely interleaving is
DriverCameOnline (ASB) before the first throttled position (Kafka); today's drop breaks that happy path.

- 2 LOCKED (c): Dispatch buffers, the AvailableDriver document IS the buffer. Location side becomes nullable,
  mirroring the availability side ("null location = never heard from Telemetry" ↔ "null availability = never heard
  from Driver Profile"). Early availability event creates an availability-only document; location handler's existing
  carry-forward merges it. Selection already excludes null H3Cell (not in k-ring set).
  Rejected: a (DP republish on demand — reverse edge Dispatch→DP, supplier compensating for consumer's join; gRPC
  variant puts sync call on Kafka hot path), b (DP availability heartbeat — volume, delay, wrong owner),
  d (separate PendingAvailability doc — two docs, second load).
  Grounds: ADR-018 "the join is Dispatch's"; Q1 self-sufficiency means one held event fully establishes state.
  Pinning test flips: an_availability_event_for_an_unseen_driver_is_dropped_not_buffered -> held-until-position.
  Skeptic findings to carry:
   1. NearbyAvailableDriversView.cs:480 resolution read OrderByDescending(ServerReceivedAt).Take(1) — Postgres DESC
      sorts NULLS FIRST; availability-only doc would win -> null resolution. Needs ServerReceivedAt != null filter.
   2. Orphan availability-only docs (online, never pings) persist; unselectable, overwritten by next WentOffline;
      Telemetry eviction doesn't reach Dispatch. Named deferral, not a slice.

## Side findings (not grilled)
- ADR-014 Consequences claims cross-topic publish-order for a consumer of dispatch.ride-assigned + trips.trip-completed;
  ASB sessions scope to one subscription/queue, so this holds only with forwarding into one entity. VERIFY against
  ASB docs before stating in minutes.

## Seven-step pass

### Step 1 — Brainstorming (Erik asked for a recommendation; accepted as recommended — note in minutes: ratified, not redirected)
Pile of 19 candidates proposed; pruned to SIX events by "no destination in this chapter → defer or strike":
1. DriverProfileOpened — replaces Created+Activated (no pre-active state with Onboarding parked). Operator back-office
   stands in for parked Onboarding. Not "DriverRegistered" (collides with identity.driver-registered).
   DP MINTS driverId (UUIDv7) as first context in the driver lifecycle, per ADR-013 Option B (ADR-013 leaves Identity
   user IDs per-BC). Minutes PR touches ADR-013's subject -> must re-affirm ADR-013. Onboarding's return reopens minting.
2. VehicleRegistered — absorbs VehicleClassAssigned; operator attests class (inspection parked); no self-declared ACCESSIBLE.
   Reclassify deferred (workaround: re-register).
3. DriverCameOnline (published) — VEHICLE CHOSEN ON GO-ONLINE SCREEN from registered-vehicles view.
   REFINES 1b: DriverVehicleChanged eliminated entirely (not merely unpublished); "only while Offline" holds by construction.
4. DriverWentOnBreak (published)
5. DriverReturnedFromBreak (published) — NEW. Distinct person decision, distinct screen, no vehicle choice; reusing
   CameOnline would overload meaning and make every break-return look like a shift start (breaks future shift-hours rule).
   REVISES GRILL COUNT: published contract = FOUR (CameOnline, WentOnBreak, ReturnedFromBreak, WentOffline). Erik signed off.
6. DriverWentOffline (published)
Deferred (named): Suspended/Deactivated (W003 OQ-14, parked w/ Onboarding — consequence: every open profile trusted in v2);
VehicleRetired (later DP chapter, becomes a Go-Online refusal); ServiceArea* (DP is location-agnostic; partial vision scope —
minutes must say so); ShiftLimitReached/ForcedOffline/DriverProfilePolicyConfigured (own "shift limits" Bruun chapter).
Struck: DriverWentOfflineMidTrip (DP is occupancy-agnostic; Trips arc's concern).
Forward notes: GHOST DRIVER (app dies; Telemetry evicts; Dispatch AvailableDriver keeps stale location + Available; DP can't
see silence) -> ADR-018's deferred staleness ceiling, Dispatch's, now due in v2. Identity seam (PR #45) must present DP's
driverId for the Telemetry⋈availability join; stub supplies until Identity arc.

### Step 2 — The Plot (accepted)
Timeline: Opened → VehicleRegistered → CameOnline (picks vehicle) → WentOnBreak → ReturnedFromBreak → WentOffline.
State machine: Offline -CameOnline-> Online -WentOnBreak-> OnBreak -ReturnedFromBreak-> Online; WentOffline from Online OR OnBreak.
Only CameOnline chooses a vehicle; all other transitions carry it forward (=> self-sufficiency).
Gap hunt: vehicle before profile (no, fixed order); sign-in (Identity seam, not DP); rides/break-mid-trip (Dispatch/Trips;
OnBreak exclusion already = "no new requests"); VehicleRegistered while Online (allowed, harmless, shows on next Go Online);
OnBreak→Offline (covered); app crash (ghost driver, forwarded to Dispatch).
FINDING: the plot added no events. Dispatch's side is a VIEW update (AvailableDriver doc), not Dispatch events.
Fact: VehicleClass vocab = Standard/Premium/Accessible; each proto package declares its own enum (dispatch, pricing) — DP's will too.

### Step 3 — The Story Board (accepted)
W1 Back office · Drivers: entered Full name → OpenDriverProfile; displayed Name/DriverId/Vehicle count ← DriverRoster.
W2 Back office · Register vehicle: header driver ← DriverRoster row; entered Make/Model/Plate/Class(Std/Prem/Acc, operator-attested) → RegisterVehicle.
W3 Driver app · Offline: "You're offline" ← DriverShiftStatus; vehicle picker ← RegisteredVehicles (default = last used);
   Go online → GoOnline{vehicleId}; empty state "Contact support", button disabled.
W4 Online: "Online since hh:mm", "Driving make model · plate" ← DriverShiftStatus; Take a break → TakeBreak; Go offline → GoOffline.
W5 On break: "On break since hh:mm", Driving ← DriverShiftStatus; End break → EndBreak; Go offline → GoOffline.
M1 no screen: machine actor — DP availability publisher (4 events → 4 ASB topics, session = driverId).
M2 no screen: machine actor — Dispatch availability translation (→ AvailableDriver availability side; doc-as-buffer).
Provisional slices (11): Commands DriverProfileOpened(W1), VehicleRegistered(W2), DriverCameOnline(W3), DriverWentOnBreak(W4),
DriverReturnedFromBreak(W5), DriverWentOffline(W4+W5, one slice two screens); Views DriverRoster(W1,W2), RegisteredVehicles(W3),
DriverShiftStatus(W3-5); Translations: availability publication (DP out, M1), availability reception (Dispatch in, M2).
Publication is its own slice though outbox fuses it into the command handler (transaction boundary = impl choice; W006 slice 3 precedent).
UX choices accepted: (1) picker defaults to last used (origin: last DriverCameOnline.vehicleId); (2) "since" = latest transition
(total shift time deferred to shift-limits chapter); (3) no audit fields openedBy/registeredBy (no destination; deferred with
operator identity); (4) no contact details/colour (rider-facing Trips arc or PII parked with Onboarding vault).

### Step 4 — Identify Inputs (accepted)
Origins vocabulary: screen / visible view / session (Identity seam) / system (minted ids, timestamps) / aggregate state.
C1 OpenDriverProfile{fullName←W1} → DriverProfileOpened{driverId←system UUIDv7 (ADR-013 first minter), fullName, occurredAt}
C2 RegisterVehicle{driverId←DriverRoster row, make/model/plate/vehicleClass←W2} → VehicleRegistered{driverId, vehicleId←system
   UUIDv7 (intra-BC, ADR-013 carve-out), make, model, plate, vehicleClass, occurredAt}
C3 GoOnline{driverId←session, vehicleId←RegisteredVehicles picker} → DriverCameOnline{driverId, vehicleId, vehicleClass←AGGREGATE
   STATE (registered class; NEVER client), occurredAt}
C4-6 TakeBreak/EndBreak/GoOffline{driverId←session} → WentOnBreak/ReturnedFromBreak/WentOffline{driverId, vehicleClass←aggregate
   state (in-service vehicle), occurredAt}. No vehicleId on C4-6 (no destination).
Refusals: blank/invalid fields at HTTP boundary (FluentValidation); unknown driver (no stream); duplicate plate for THIS driver;
GoOnline not Offline / vehicle not mine (covers no-vehicles); TakeBreak not Online; EndBreak not OnBreak; GoOffline already Offline.
Aggregate: ONE stream per driver (profile + vehicles + availability) — GoOnline's two invariants need both (ADR-012).
Plate uniqueness per-driver only; cross-driver = DCB (Polecat supports) — named deferral, no domain rule demands it.
Carried to step 5: vehicleId on published DriverCameOnline (W001 §5.3 lock = downstream-as-origin mistake); double-tap idempotency → step 7.

### Step 5 — Identify Outputs + completeness check (accepted, F1–F3 accepted)
V1 DriverRoster (operator): driverId←Opened → W1 col, W2 header, RegisterVehicle.driverId; fullName←Opened → W1, W2; vehicleCount←count VehicleRegistered → W1.
V2 RegisteredVehicles (driver): vehicles[].vehicleId←VehicleRegistered → GoOnline.vehicleId; make/model/plate/class → W3 labels;
   lastUsedVehicleId←DriverCameOnline.vehicleId → W3 default.
V3 DriverShiftStatus (driver): state ← Offline at Opened, then latest transition type → which of W3/W4/W5; since ← latest
   transition occurredAt → W4/W5; inServiceVehicle{make,model,plate} ← CameOnline.vehicleId ⋈ VehicleRegistered (same stream) → W4/W5.
M1 published contract (all four messages identical): driver_id→Id; message type→AvailabilityState; vehicle_class (DP proto enum)
   →VehicleClass (Dispatch enum); occurred_at→AvailabilityUpdatedAt. One ASB topic each, SessionId=driverId.
M2 Dispatch mapping: CameOnline→Available, ReturnedFromBreak→Available, WentOnBreak→OnBreak, WentOffline→Offline; LWW on
   AvailabilityUpdatedAt; no doc → availability-only doc (buffer).
Completeness findings:
 F1 published vehicleId: no destination → OMITTED from contract (internal event keeps it). REVISES W001 §5.3 locked row
    ("DriverCameOnline carries vehicleId and vehicleClass") — downstream-as-origin. Trips arc asks later if it needs it.
 F2 occurredAt on DriverProfileOpened / VehicleRegistered: no destination → DROPPED from payload (store metadata timestamp
    is not a model field). Kept on four transitions (since + Dispatch LWW).
 F3 vehicle_class UNSPECIFIED / unparseable driver_id / missing occurred_at → Dispatch DROPS (mirrors DriverLocationUpdatedHandler.cs:229).
 F4 vehicleClass on WentOffline: inert destination, KEPT for self-sufficiency (1b) — minutes must say so to prevent "optimizing" it away.
 F5 session-sourced driverId: origin outside model (Identity seam) — existing forward note.
 F6 DriverShiftStatus.state initial: origin = existence of DriverProfileOpened; needs a step-7 scenario.
Result: every field has origin + destination; contract narrower than internal events (outbound Translation earns its slice).

### Step 6 — Apply Conway's Law (accepted, D1–D4 accepted)
Lanes: Operator (W1,W2 + DriverRoster) / Driver (W3–W5 + RegisteredVehicles, DriverShiftStatus) / DRIVER PROFILE (Polecat,
SQL Server: all commands, 6 events, 3 views, M1) / ASB (4 topics, SessionId=driverId) / DISPATCH (M2 → AvailableDriver
availability side ⋈ location side ← Telemetry Kafka) / IDENTITY seam (dashed; session supplies driverId).
Lane check: ONLY M1/M2 cross a system lane — boundary confirmed, not discovered.
D1 slug `driverprofile` (only spelling in repo: driverprofile.emodel.yaml; proto package can't hyphenate; ADR-019 proto/topic
   alignment; ADR-014 regex ok). Topics: driverprofile.driver-came-online / .driver-went-on-break / .driver-returned-from-break /
   .driver-went-offline. Protos at protos/crittercab/driverprofile/v1/ (4 messages + DP-owned VehicleClass enum) — authored at the slice.
D2 context-map edge #6 DP→Dispatch: dashed → SOLID, Customer–Supplier + Published Language; Dispatch translates to own enum;
   one-directional (Q2 rejected reverse edge). Update docs/context-map/README.md in minutes PR.
D3 M2 belongs to Dispatch lane; recorded in W007 as cross-workshop amendment to W001 §5.3 + W006 §6.5 (append-only minutes,
   W006 §6.5 precedent). Amendment table: four events (no VehicleChanged; +ReturnedFromBreak); no published vehicleId (F1);
   doc-as-buffer (Q2); occurredAt clock (1c).
D4 back office = DP's own operator endpoints (no Operations BC in v2); operator auth = Identity-seam stub; Onboarding replaces W1/W2 on return.
IDENTITY-ARC HANDOVER: vision says DP downstream of Identity, but DP now MINTS driverId and the session must PRESENT it (F5) →
   Identity consumes DP's id, or the app resolves it from DP after sign-in. Edge #3's Identity→DP direction may invert or go
   two-way. The Identity arc decides; minutes hand it over explicitly.

### Step 7 — Elaborate Scenarios (accepted; names FINAL)
Final slice names + patterns (11):
  Command: DriverProfileOpened (W1), VehicleRegistered (W2), DriverCameOnline (W3), DriverWentOnBreak (W4),
           DriverReturnedFromBreak (W5), DriverWentOffline (W4+W5)
  View:    DriverRoster (W1,W2), RegisteredVehicles (W3), DriverShiftStatus (W3–W5)
  Translation: AvailabilityPublication (DP outbound; no screen: machine actor), AvailabilityIntake (Dispatch inbound; no screen: machine actor)
Double-tap ruling: same-state repeats (GoOnline when Online, TakeBreak when OnBreak, GoOffline when Offline) are REFUSED,
  no event; driver app renders "already in that state" as success (W001 §5.7 silent-race reasoning).
GWT highlights (full set in session transcript; reproduce in minutes):
  - DriverProfileOpened: happy; blank name refused at boundary.
  - VehicleRegistered: happy (Accessible); allowed while Online (in-service vehicle unchanged); duplicate plate for this
    driver refused; unknown driver refused; blank/unknown class refused at boundary.
  - DriverCameOnline: happy (class from registration); class can't be forged (no class field on GoOnline); no vehicles →
    refused "vehicle not registered"; another driver's vehicle refused; already Online refused (no event); OnBreak refused
    ("end the break instead").
  - DriverWentOnBreak: happy carries Premium from in-service; Offline refused; already OnBreak refused.
  - DriverReturnedFromBreak: happy, no vehicle choice, V1 carries over; Online refused; Offline refused.
  - DriverWentOffline: happy from Online and from OnBreak; already Offline (incl. never-online profile) refused.
  - DriverRoster: counts vehicles; zero-vehicle row.
  - RegisteredVehicles: list; lastUsed after CameOnline/WentOffline; empty → "Contact support".
  - DriverShiftStatus: initial Offline (F6); Online since T1 driving V1; OnBreak since T2; Online since T3 (clock restarts); Offline.
  - AvailabilityPublication: each transition → its topic, {driver_id, vehicle_class, occurred_at}, session D, no vehicle_id;
    Opened/VehicleRegistered publish nothing; refused command publishes nothing.
  - AvailabilityIntake: known driver → availability set, location untouched; ReturnedFromBreak → Available; BUFFER (no doc
    → availability-only, unselectable); MERGE (position arrives → both sides, selectable); REORDER (WentOffline@:05 arriving
    after ReturnedFromBreak@:07 → discarded, CORRECTLY, return happened later); redelivery no-op; malformed dropped (F3).
  - NearbyAvailableDrivers amendment (W006 §6.5, per D3): resolution read ignores availability-only docs; availability-only
    never a candidate.
  - CHAPTER ACCEPTANCE (first v2 slice): Opened + VehicleRegistered(Std) + DriverCameOnline reaching Dispatch BEFORE first
    position + position within 5000m + DispatchPolicyConfigured(5000, 5) → FareQuoted(STANDARD) → CandidatesSelected
    includes D. Written in the interleaving that fails today, so Q2 can't be skipped.

## Session close

### Revisions this chapter makes to earlier artifacts (all recorded in W007, none edited in place except the context map)
- W001 §5.3 Translation-in sources + "Driver capability" locked row: four events incl. VehicleChanged → four events
  CameOnline/WentOnBreak/ReturnedFromBreak/WentOffline; DriverVehicleChanged eliminated; published DriverCameOnline does
  NOT carry vehicleId (F1).
- W006 §6.5 + ADR-018 forward-constraint: DP's ASB contract now designed (4 topics, 3 fields, occurredAt clock); availability-
  before-location resolved as doc-as-buffer (location side nullable); resolution-read null filter.
- ADR-018 / vision / context map edge #6 wording "DriverCameOnline / WentOnBreak / WentOffline / VehicleChanged" superseded.
- Vision Driver Profile scope: chapter covers availability + registered vehicles; service area DEFERRED (minutes must say so).
- ADR-013: DP is first minter of driverId in the driver lifecycle — minutes PR re-affirms ADR-013 (v1 ADR binding rule).
- Context map edge #6 DP→Dispatch: SOLID, Customer–Supplier + Published Language (EDIT in minutes PR, per handoff).

### Handed forward
- To Dispatch (next Dispatch work / first v2 slice): GHOST DRIVER — ADR-018's deferred staleness ceiling, now due in v2.
- To the Identity arc: session must present DP's driverId; edge #3 Identity→DP direction may invert / go two-way.
- To Onboarding (parked): on return, replaces W1/W2 operator stand-in and reopens driverId minting (W004 had Onboarding mint).
- To Trips arc: whether riders need vehicle make/model/plate (would be a new published field, asked for then).
- Later DP chapters: VehicleRetired (Go-Online refusal); service area; shift limits (ShiftLimitReached/ForcedOffline/
  DriverProfilePolicyConfigured — Bruun temporal); suspension/deactivation (parked w/ Onboarding, W003 OQ-14);
  vehicle reclassification; cross-driver plate uniqueness (DCB) — only if a domain rule demands it.
- Operator identity / audit fields (openedBy, registeredBy) — deferred, no destination in v2.

### Realization deltas for the implementation PRs (not this session; for the "driver comes online" slice prompt)
Dispatch: delete DriverAvailabilityChanged placeholder; AvailabilityIntake handlers for 4 driverprofile.* topics (map to
DriverAvailabilityState, DP enum → Dispatch VehicleClass); AvailableDriver location side `required` → nullable; availability
handler creates availability-only doc instead of returning on null; NearbyAvailableDriversView.cs:480 add
ServerReceivedAt != null filter (Postgres DESC NULLS FIRST); F3 wire guards; flip
an_availability_event_for_an_unseen_driver_is_dropped_not_buffered → held-until-position; existing W006 GWTs unchanged.
Driver Profile: one Polecat stream per driver (decider: profile + vehicles + availability); protos at
protos/crittercab/driverprofile/v1/ (4 messages + VehicleClass enum, buf lint); outbox per ADR-014; FluentValidation at
HTTP boundary. Store/AppHost/ASB enter at skeleton + transport-groundwork PRs per handoff sequence.

### VERIFIED 2026-10-08 — ADR-014 ASB ordering claim (ctx7 /microsoftdocs/azure-docs, message-sessions.md + auto-forwarding)
- Docs: "To achieve FIFO processing when processing messages from Service Bus queues or subscriptions, use sessions";
  "On session-aware queues or subscriptions, sessions exist when…"; "The APIs for sessions exist on queue and subscription
  clients"; session enablement is per subscription (--enable-session). Auto-forwarding chains a queue/subscription into
  another queue/topic (only way to merge sources) — docs give NO cross-source interleaving guarantee.
- VERDICT: ADR-014 OVERSTATES in two places. Decision ("all events for a single ride arrive at any one consumer in publish
  order") holds only per topic subscription. Consequences (consumer of dispatch.ride-assigned + trips.trip-completed sees
  publication order without timestamps) is FALSE as a broker property — two subscriptions, two independent sessions; also two
  independent publishers have no shared publish order. Holds in practice only by domain causality.
- Impact: W007 unaffected (design already orders by LWW occurredAt; AvailabilityIntake reorder scenario is this claim as a
  test). No code affected (ASB unbuilt). ADR-014 per-canonical-id session keying remains correct within a topic.
- Recommendation: minutes PR adopts ADR-014 for driverprofile.* so it touches the subject and must re-affirm (AGENTS.md v1-ADR
  rule) — amend there: scope ordering to "per topic subscription, per session"; replace cross-topic claim with "consumers
  needing cross-topic order reconstruct it from a server-stamped occurrence time". Erik to choose: minutes PR vs own tidy:.

### Methodology observations (for the retro / methodology-log)
- From step 1 on, Erik asked for recommendations and ratified each without redirect. Per the skill: "a session that only
  ratified is a finding" — record it. Mitigation that did happen: the grill's four questions each came with rejected
  alternatives argued from code; redirects came from the BOARD against prior decisions (1b count 3→4 via ReturnedFromBreak;
  1b refined via vehicle-on-GoOnline; W001 §5.3 vehicleId lock overturned by completeness check).
- The Plot added no events: pruning in step 1 had already cut along the chapter's edges.
- Conway's Law confirmed rather than discovered the boundary (only Translations cross lanes).
- Completeness check caught F1 (a downstream-authored field on an upstream event) before any proto existed — same defect
  class as W001 §5.2's long-open reads inconsistency.

### Outputs still owed (later, own PR — per handoff)
1. Canvas export from EventModelers.AI, committed.
2. docs/workshops/007-driver-profile-event-model.md, written as minutes (minutes rule at top of docs/workshops/README.md),
   built from this ledger; includes the W001 §5.3 / W006 §6.5 amendment table (D3).
3. docs/context-map/README.md edge #6 → solid (D2).
4. No driverprofile.emodel.yaml (waits on CritterMart Orders experiment).
5. ADR-014 AMENDMENT — DECIDED (Erik, 2026-10-08): rides in the minutes PR (not a separate tidy:). The PR re-affirms ADR-014
   for driverprofile.* topics and amends it: (a) Decision's ordering guarantee scoped to "per topic subscription, per session";
   (b) Consequences' cross-topic claim (dispatch.ride-assigned + trips.trip-completed in publication order without timestamps)
   replaced with "consumers needing order across topics reconstruct it from a server-stamped occurrence time"; cite
   message-sessions.md; name domain causality as why ride-lifecycle flows hold in practice. Follow docs/decisions/ format;
   amendment needs Erik's sign-off on wording in that PR. Also re-affirm ADR-013 (DP mints driverId) in the same PR.
