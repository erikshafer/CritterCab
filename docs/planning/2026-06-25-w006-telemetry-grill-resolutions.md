# W006 Telemetry — Front-Loaded Design Grill Resolutions — 2026-06-25

> **Purpose:** Durable capture of the `grill-with-docs` front-loading pass that preceded the W006
> Telemetry Event Modeling workshop (EM-direct, W005 playbook). Records the eight architectural
> decisions (R1–R8) the grill resolved, the **ADR-018** decision statement those decisions imply,
> the §11 ADR candidates surfaced, the workshop skeleton, and what the eventual W006 PR owes
> upstream. **Status: design front-loaded; ADR-018 drafting + the slice-by-slice event model are
> deferred to a later focused session.** Disposable once W006 ships.

---

## Why this exists

Per [ADR-004](../decisions/004-design-phase-workflow-sequence.md) and the W005 precedent
("a grill-with-docs pass preceded the workshop and front-loaded every design call"), the Kafka-vs-gRPC
geospatial-supply question — parked since [W001 §10 parking-lot #4](../workshops/001-dispatch-event-model.md)
and [§11 ADR-candidate #3](../workshops/001-dispatch-event-model.md), and dashed on
[context-map edge #5](../context-map/README.md#5-telemetry--dispatch-deferred) — was stress-tested
against the existing model **before** modeling Telemetry's slices. This note is the output of that
grill. The actual Event Modeling workshop (events, commands, views, swim lanes, slices, GWT) and the
ADR-018 authoring both resume next session, grounded by these resolutions.

**Method note:** EM-direct (no Domain Storytelling pre-step). Telemetry is machine-to-machine and
structurally independent, so DS's human-language-boundary payoff is low — same call W005 made.
Context Mapping was **already done** (edge #5 is named, expected-CS); W006 *locks* the dashed edge,
it doesn't re-name it.

---

## The eight resolutions (R1–R8)

### R1 — Local projection, not remote query (the root fork)
`NearbyAvailableDrivers` is a **Dispatch-owned local projection**, not a Telemetry-owned queryable
index. Telemetry's **location** supply flows over **Kafka** (`DriverLocationUpdated`, throttled /
cell-changed grain); mobile→Telemetry GPS ingest is **gRPC client-streaming**.

- **Decisive reasons:** (a) Kafka is the project's hardest-to-demonstrate priority and the local
  projection gives it a *real consumer* in the first Telemetry build — under the gRPC-query
  alternative, Kafka would be a producer with no consumer until surge pricing (post-MVP);
  (b) Dispatch autonomy on its critical matching path (mirrors hyperscale matchers' in-matcher
  supply indexes, per `docs/research/ride-sharing-lessons-learned.md`); (c) the throttled grain
  already bounds ingest cost.
- **Override recorded:** this contradicts the vision doc's "the geospatial index *that Dispatch
  queries*" phrasing. [W001 §5.3](../workshops/001-dispatch-event-model.md) parking-lot #4 had
  already reopened it; ADR-018 records the override explicitly.

### R2 — The availability half is ASB, and Driver Profile stays unlocked
`NearbyAvailableDrivers` is a **join**. Per [ADR-005](../decisions/005-transport-selection-by-flow-type.md),
location pings are high-volume telemetry → **Kafka**, but availability transitions
(`DriverCameOnline` / `WentOnBreak` / `WentOffline` / `VehicleChanged`) are low-volume,
correctness-sensitive **business events** → **ASB**. So the view is a **multi-transport projection**:
Kafka (geo) + ASB (availability) folding into one Dispatch-local view — the cleanest possible
embodiment of ADR-005's "transport chosen per flow type."

- ADR-018 locks **only Telemetry's (Kafka) half**. The Driver Profile (ASB) half is a
  **forward-constraint** to Driver Profile's eventual workshop — locking an un-workshopped BC's
  publication contract would be a cross-BC over-reach (the trap the context-map discipline exists to
  prevent). Informs [context-map edge #6](../context-map/README.md#6-intra-actor-topology-deferred)
  (Driver Profile → Dispatch, the structural pair of edge #5).

### R3 — Eventual consistency, reconciled at acceptance
Guarantee: **eventual consistency, last-writer-wins per driver** (monotonic ordering field), no
read-your-writes. Justified because the model **already absorbs candidate staleness** at the
acceptance layer ([W001 parking-lot #11](../workshops/001-dispatch-event-model.md) `OfferRevoked` +
aggregate-per-invariant `OfferAccepted` guard). Selection optimizes for *recall* from a local view;
acceptance enforces *correctness* (exactly-one assignment). **Staleness-ceiling exclusion deferred to
v2** (keep v1 honest/simple/working before tuning staleness).

### R4 — Telemetry is a stream-processing BC, not event-sourced
Raw GPS pings are processed **in flight** (gRPC client-streaming in → throttle/cell-change → Kafka
publish); they are **never event-sourced** (per-ping-per-driver would detonate any event store). A
**last-known-position document** (overwrite-in-place) is Telemetry's location-of-record; the **Kafka
topic is the breadcrumb history**. **Marten is deliberately not the load-bearing core** here — this
is CritterCab's **fourth modeling shape** (alongside aggregate-per-invariant W001/W002, Process
Manager via Handlers W004, ACL-translation-dominant W005). → §11 ADR candidate.

### R5 — Windowed client-streaming ingest
**Windowed** gRPC client-streaming (short-lived per-window calls), **not one stream per shift** —
shifts are long and variable, making per-shift streams unbounded and incomparable for metrics; a
window is a bounded, comparable unit. Holds ADR-005's client-streaming assignment for v1
(bidirectional = v2).

- `LocationPing { lat, lng, deviceTimestamp, accuracyMeters, speed?, heading? }` — **`driverId`
  comes from the authenticated principal, never the payload** (same identity discipline as W005's
  "raw provider `sub` never published").
- `LocationIngestAck { acceptedCount, serverTime, throttlePolicyVersion? }` — `serverTime` for client
  clock-skew correction. "Driver session" is a *logical* concept, decoupled from any physical stream.

### R6 — Throttle/cell policy: cell-change-or-heartbeat, H3 in the contract, config-as-events
- **Publish trigger:** on **cell-change OR heartbeat interval** (heartbeat lets "last-known" mean
  "live" and lays the rail for the deferred v2 staleness-ceiling).
- **Cell system: H3** (Uber's hexagonal index). The **H3 cell id is carried in `DriverLocationUpdated`
  as published-language** — both BCs must agree on cell system + resolution because Dispatch indexes/
  buckets by the same cell id for radius queries (it's contract vocabulary, like
  [ADR-013](../decisions/013-shared-cross-bc-identifier.md)'s canonical id, not an implementation
  detail).
- **Throttle policy via configuration-as-events** ([ADR-011](../decisions/011-configuration-as-events-bootstrap.md)):
  a `TelemetryPolicyConfigured` singleton stream (cadence + cell resolution + heartbeat interval)
  feeds `throttlePolicyVersion`. The **one** place this stream-processing BC touches an event stream
  — demonstrating config-as-events is *orthogonal* to whether the domain core is event-sourced.
  Chosen over appsettings for reference-architecture parity with Dispatch's `DispatchPolicy`.

### R7 — Kafka topic: partition by driverId, serverReceivedAt dedup, ADR-014 by extension
- **Partition key = `driverId`** → per-driver ordering becomes a *transport property*; R3's monotonic
  field is reduced to a **dedup backstop** against at-least-once redelivery.
- **Dedup / last-writer key = `serverReceivedAt`** (server-stamped, monotonic — not `deviceTimestamp`,
  which skews and is client-controllable).
- **Topic = `telemetry.driver-location-updated`**, extending
  [ADR-014](../decisions/014-asb-topic-naming-convention.md)'s `<source-bc>.<event-name-kebab>` pattern.
  ADR-014 is currently **ASB-scoped**; generalizing it is its own §11 ADR candidate (see below).

### R8 — Telemetry is availability-agnostic
"Active" = **actively pinging**, never *available*. Telemetry tracks location for any authenticated,
pinging driver; **evicts / marks stale by heartbeat absence** (a ping-presence concept); **never
consumes Driver Profile**. The location ⋈ availability join lives **only** in Dispatch's
`NearbyAvailableDrivers` projection. Accepted cost: Telemetry may briefly hold a location for a
just-went-off-shift driver — harmless, because *availability is not Telemetry's truth to tell* and
Dispatch's availability filter catches it. *(v2/v3 may revisit under growth — as may every decision
here.)*

---

## ADR-018 decision statement (ready to author next session)

> **ADR-018 — Candidate-Projection Ownership & Telemetry Geospatial Supply.** The
> `NearbyAvailableDrivers` view is a **Dispatch-owned local projection**, not a Telemetry-owned
> queryable index. Telemetry supplies driver **location** as an eventually-consistent stream over
> **Kafka** (`telemetry.driver-location-updated`, partitioned by `driverId`); Driver Profile supplies
> **availability** over **ASB** (forward-constraint, locked at Driver Profile's workshop). Dispatch
> joins them locally. Guarantee: **eventual consistency, last-writer-wins per driver**
> (`serverReceivedAt`), reconciled at the acceptance layer. Explicitly overrides the vision doc's
> "index that Dispatch queries" phrasing.

**Three-criteria gate (all met):** hard to reverse (plumbed into both BCs + the proto contract);
surprising without context (Dispatch owns its own geo projection, against the vision doc); a real
trade-off (local-projection/Kafka vs. remote-query/gRPC — autonomy + showcase + boundary-purity in
genuine tension). **Resolves [W001 §11 ADR-candidate #3](../workshops/001-dispatch-event-model.md);
locks [context-map edge #5](../context-map/README.md#5-telemetry--dispatch-deferred).** Requires
explicit sign-off at authoring per the CritterCab ADR gate.

---

## §11 ADR candidates surfaced (all three carried)

| Candidate | Trigger | Note |
|---|---|---|
| **Kafka-topic-naming convention** | First Kafka topic lands (W006 build). | Generalize ADR-014 from ASB-only to a transport-agnostic topic-naming convention. Own ADR (one-decision-per-ADR), *not* folded into ADR-018. |
| **Stream-processing as the 4th modeling shape** | First Telemetry implementation slice. | Telemetry as a non-event-sourced stream-processing BC, parallel to the shape-ADR candidates W004/W005 minted. |
| **Windowed client-streaming ingest pattern** | First gRPC ingest slice. | The windowed-vs-per-shift convention (R5). **May fit better as a `docs/skills/` skill than an ADR** — decide at authoring. |

---

## W006 workshop skeleton (thin BC, W005-sized)

| Slice | Shape |
|---|---|
| `TelemetryPolicyConfigured` | Configuration-as-events (ADR-011) — throttle cadence, H3 resolution, heartbeat interval. The one event-sourced stream. |
| GPS ingest | gRPC **client-streaming** `ReportLocations(stream LocationPing) → LocationIngestAck`; in-flight throttle/cell-change. |
| `DriverLocationUpdated` publish | → **Kafka**, partition `driverId`, carries H3 cell id + `serverReceivedAt`. |
| `LastKnownPosition` store + eviction | Overwrite-in-place document; heartbeat-absence eviction; availability-agnostic. |
| W001 §5.3 amendment | Dispatch's `NearbyAvailableDrivers` population mechanism resolved (Kafka ⋈ ASB). |

---

## What the eventual W006 PR owes upstream (cross-workshop amendments)

The W006 design PR is **not** self-contained — per the [context-map §Update cadence](../context-map/README.md#update-cadence)
and the W002-amends-W001 precedent, it must also:

1. **Author ADR-018** in `docs/decisions/` (with sign-off) — resolves W001 ADR-candidate #3.
2. **Amend [W001 §5.3](../workshops/001-dispatch-event-model.md)** — the "population mechanism
   deferred (parking-lot #4)" line and the "No external call" framing are now *resolved*
   (Kafka location ⋈ ASB availability, local projection). Update parking-lot #4 to closed.
3. **Flip [context-map edge #5](../context-map/README.md#5-telemetry--dispatch-deferred)** from dashed
   to **solid on the Telemetry side** (supplier-side locked); note edge #6 (Driver Profile → Dispatch)
   informed-but-still-dashed.
4. **Surface the three §11 ADR candidates** in the W006 artifact with their triggers.
5. **Proto authorship follow-up** — `telemetry/v1/driver_location_updated.proto` + the gRPC
   `ReportLocations` service contract under `/protos/` (likely a bundled-proto session per the PR #4
   precedent, not necessarily in the workshop PR).

---

## Resume instructions (next session)

The grill is **complete** — do **not** re-grill. Go straight to:
1. **Draft ADR-018** from the decision statement above (sign-off gate applies).
2. **Walk the W006 slices** (table above) interactively, slice-by-slice with sign-off, per the
   [W001 retro meta-decisions](../workshops/001-dispatch-event-model.md) (commit only after sign-off;
   "decisions locked" table per slice; proactive-projections from slice 1).
3. Close with the cross-workshop amendments (§"owes upstream") in the same PR.

**Grounding re-reads if context is cold:**
[ADR-005](../decisions/005-transport-selection-by-flow-type.md) (transport-by-flow-type),
[W001 §5.3 / §10#4 / §11#3](../workshops/001-dispatch-event-model.md) (the `CandidatesSelected` slice
+ the parked question), [context-map edge #5](../context-map/README.md#5-telemetry--dispatch-deferred),
and this note.

---

## Repo state at checkpoint

- **Branch:** `design/w006-telemetry-event-model` (off `main` @ `2cd3387`; carries none of the ci-guard
  changes).
- **Sibling open PR:** **#36** (`ci/solution-completeness-guard`) — green, awaiting review/merge.
  Independent of this branch.
- **This branch:** holds only this checkpoint note so far. The full W006 workshop + ADR-018 + the
  cross-workshop amendments land here next session, as one cohesive W006 design PR.

---

## Document history

- **2026-06-25.** Authored at session pause after the W006 front-loading grill (R1–R8) completed.
  Crystallisation (ADR-018 + slice-walk) deferred to a focused follow-up session per the user's
  "checkpoint and pause" call.
