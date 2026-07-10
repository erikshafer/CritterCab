# Workshop 006 — Telemetry Event Model

**Status:** Complete (v0.1, 2026-06-30) pending cross-workshop amendments in this PR (§13.5). Scope confirmed; **§3 modeling shape locked — stream-processing, CritterCab's fourth modeling shape**; §4 UL + §5 Event List; five slices walked with per-slice sign-off; §7–§12 cross-references + ADR candidates + parking lot; §13 retrospective. All design leans pre-resolved by the grill-with-docs front-loading pass (R1–R8; see triggering note).
**Started:** 2026-06-30.
**Facilitator / modeler:** Erik Shafer (solo).
**AI collaborator:** Claude (Opus 4.8), rotating through Facilitator, Developer, Skeptic, and Domain-Expert personas per `docs/research/event-modeling-workshop-guide.md` Lesson 8.
**Triggering note:** [`docs/planning/2026-06-25-w006-telemetry-grill-resolutions.md`](../planning/2026-06-25-w006-telemetry-grill-resolutions.md) — the grill-with-docs front-loading pass (eight resolutions R1–R8) that preceded this workshop and resolved every design call. No separate prompt doc; the grill note *is* the prompt-equivalent (W005 precedent).
**Methodology reference:** `docs/research/event-modeling-workshop-guide.md`.
**Adjunct patterns:** `docs/research/agents-in-event-models.md` (Klefter translation-decision events; Bruun temporal automation). `docs/research/ride-sharing-lessons-learned.md` (in-matcher supply-index precedent).
**Structural constraints honored:** `docs/rules/structural-constraints.md` — ADR-002, **ADR-005** (Kafka for telemetry / gRPC for streaming), ADR-009, **ADR-011** (config-as-events), **[ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md)** — this workshop's parent decision (candidate-projection ownership + telemetry geospatial supply).
**Workshop 001/002/004/005 inheritance:** All conventions carry forward without re-litigation (W001 §12.6; W005 §X). EM-direct — no DS pre-step (Telemetry is machine-to-machine and structurally independent; DS's human-language-boundary payoff is low — same call W005 made).
**Modeling shape (NEW for W006):** **stream-processing** — CritterCab's fourth modeling shape, alongside aggregate-per-invariant (W001/W002), Process Manager via Handlers (W004), and ACL-translation-dominant (W005).

---

## 1. Session Log

| Session | Date | Duration | Phases covered | Notes |
|---|---|---|---|---|
| 1 | 2026-06-30 | In progress | Scope confirmation (grill-locked); §3 stream-processing sidebar; §4 UL; §5 Event List scaffold; slice ordering; slice walk | Fifth EM workshop; **first non-event-sourced BC** (stream-processing). Resumes the W006 design after the 2026-06-25 grill-with-docs pause (R1–R8) and the sign-off of ADR-018. EM-direct. Eight grill resolutions pre-locked the design: local-projection-not-remote-query (R1); availability-half-is-ASB forward-constraint (R2); eventual-consistency-reconciled-at-acceptance (R3); stream-processing-not-event-sourced (R4); windowed client-streaming ingest (R5); cell-change-or-heartbeat throttle + H3 + config-as-events (R6); Kafka partition-by-driverId + serverReceivedAt dedup (R7); availability-agnostic (R8). |

---

## 2. Scope Statement

### 2.1 In scope

Telemetry's **location-of-record lifecycle** for actively-pinging drivers, modeled as a stream-processing BC:

- **GPS ingest** — gRPC **client-streaming** `ReportLocations(stream LocationPing) → LocationIngestAck`, processed in flight; **windowed** (short-lived per-window calls, not one stream per shift — R5).
- **In-flight throttle / cell-change** detection — the publish trigger (cell-change OR heartbeat interval — R6).
- **`DriverLocationUpdated` → Kafka** — `telemetry.driver-location-updated`, partitioned by `driverId`, carrying the H3 cell id + `serverReceivedAt` (R1, R7).
- **`LastKnownPosition`** document — overwrite-in-place location-of-record (R4) + **heartbeat-absence eviction** (R8).
- **`TelemetryPolicyConfigured`** — the one event-sourced stream (config-as-events per [ADR-011](../decisions/011-configuration-as-events-bootstrap.md)): publish cadence, H3 resolution, heartbeat interval.

### 2.2 At the boundary

- **Mobile → Telemetry (ingest).** gRPC client-streaming `ReportLocations`. `driverId` comes from the authenticated principal, never the payload (R5; same identity discipline as W005's "raw provider `sub` never published").
- **Telemetry → Dispatch (publication).** `DriverLocationUpdated` over Kafka, consumed by Dispatch's `NearbyAvailableDrivers` local projection per **[ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md)**. Supplier side committed here; locks [context-map edge #5](../context-map/README.md#5-telemetry--dispatch-deferred).

### 2.3 Out of scope

- **The ASB availability half** — Driver Profile's eventual workshop (ADR-018 forward-constraint; [context-map edge #6](../context-map/README.md#6-intra-actor-topology-deferred)). Locking an un-workshopped BC's publication shape would be cross-BC over-reach.
- **Availability semantics entirely** — Telemetry is availability-agnostic (R8). "Active" = *pinging*, never *available*.
- **Surge-pricing** Kafka consumption — post-MVP (ADR-005's second Kafka consumer).
- **Bidirectional streaming ingest** — v2 (R5 holds client-streaming for v1).
- **Staleness-ceiling eviction** beyond heartbeat-absence — v2 (R3 keeps v1 honest/simple).
- **Implementation code; proto authorship; per-provider modeling** — pure design. Protos named in §10 (ADR-009), not authored inline.

### 2.4 Structural constraints honored

- High-volume telemetry → Kafka; service-to-service streaming → gRPC ([ADR-005](../decisions/005-transport-selection-by-flow-type.md)).
- Candidate-projection ownership + telemetry supply per [ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md) (this workshop's parent decision).
- Config reached via configuration-as-events ([ADR-011](../decisions/011-configuration-as-events-bootstrap.md)).
- New protobuf contracts named in §10, not authored inline ([ADR-009](../decisions/009-protobuf-contracts-as-first-class-artifacts.md)).

### 2.5 Decisions locked during scope-setting (all grill-resolved)

| Decision | Resolution | Grill |
|---|---|---|
| Candidate-projection ownership | Dispatch-owned local projection; Telemetry supplies location over Kafka | R1 / ADR-018 |
| Availability half | ASB, forward-constraint to Driver Profile (not locked here) | R2 / ADR-018 |
| Consistency | Eventual, last-writer-wins per driver (`serverReceivedAt`), reconciled at acceptance | R3 / ADR-018 |
| Modeling shape | Stream-processing; fourth CritterCab modeling shape | R4 |
| Ingest shape | Windowed gRPC client-streaming (not per-shift) | R5 |
| Throttle trigger | Cell-change OR heartbeat; H3 cell system; config-as-events | R6 |
| Kafka topic | `telemetry.driver-location-updated`; partition `driverId`; dedup `serverReceivedAt` | R7 |
| Availability awareness | Availability-agnostic; "active" = pinging | R8 |

---

## 3. Modeling shape — stream-processing (R4)

### 3.1 Why this section exists

Per W001 §12.6 #2 / W002 §3 / W004 §3 / W005 §3, the modeling-shape decision shapes every slice. Telemetry's sidebar settles a shape none of the prior three cover: **a BC whose domain core is not event-sourced at all.** Grill R4 resolved it; this section commits the resolution.

### 3.2 Decision — stream-processing, not event-sourced (R4)

**Telemetry is modeled as a stream-processing BC.** Raw GPS pings are processed *in flight* (gRPC client-streaming in → throttle / cell-change → Kafka publish) and are **never event-sourced** — a per-ping-per-driver event store would detonate at the volumes ADR-005 anticipates. The **last-known-position document** (overwrite-in-place) is Telemetry's location-of-record; the **Kafka topic is the breadcrumb history**. Marten is deliberately *not* the load-bearing core here.

This makes Telemetry **CritterCab's fourth modeling shape**, alongside aggregate-per-invariant (W001/W002), Process Manager via Handlers (W004), and ACL-translation-dominant (W005). The shape is a firm §11 ADR candidate.

### 3.3 `LastKnownPosition` — the overwrite-in-place location-of-record

```csharp
public class LastKnownPosition   // Marten document, overwrite-in-place — NOT an event stream
{
    public Guid Id { get; set; }                 // driverId — from the authenticated principal, NEVER the ping payload (R5)
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string H3Cell { get; set; }           // resolution from TelemetryPolicyConfigured (R6)
    public DateTimeOffset ServerReceivedAt { get; set; }  // server-stamped; the LWW / dedup key (R7)
    public DateTimeOffset DeviceTimestamp { get; set; }   // client clock; informational, skews
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    // availability-agnostic (R8): no "isAvailable" — that's Driver Profile's, joined in Dispatch
}
```

The document exists as **location-of-record and eviction substrate** — not invariant protection. One document per driver (`driverId`). No concurrency boundary is defended; last-writer-wins on `serverReceivedAt` is the whole concurrency story.

### 3.4 The one event-sourced stream — `TelemetryPolicyConfigured`

The **single** place this stream-processing BC touches an event store is the throttle-policy singleton stream (config-as-events, [ADR-011](../decisions/011-configuration-as-events-bootstrap.md)). This demonstrates that **config-as-events is orthogonal to whether the domain core is event-sourced** — a configurable BC can reach a valid policy state via an event stream while its actual workload runs entirely off documents and Kafka. Chosen over appsettings for reference-architecture parity with Dispatch's `DispatchPolicy`.

### 3.5 What's split off

| Concern | Where | Why |
|---|---|---|
| Driver availability (online / break / offline / vehicle) | Driver Profile BC | R8 / ADR-018 — Telemetry is availability-agnostic; the join lives in Dispatch. |
| The `NearbyAvailableDrivers` view | Dispatch BC | ADR-018 — Dispatch-owned local projection, not a Telemetry index. |
| Surge-pricing signal consumption | Pricing BC (post-MVP) | ADR-005's second Kafka consumer; out of scope. |
| Authentication / `driverId` minting | Identity BC | `driverId` arrives via the authenticated principal (R5). |

### 3.6 ADR-evidence framing

- **[ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md)** — first exercise; this workshop realizes the Telemetry (Kafka, supplier) half the ADR locked.
- **[ADR-005](../decisions/005-transport-selection-by-flow-type.md)** — first concrete Kafka and first gRPC client-streaming surface in CritterCab; the transport-by-flow-shape decision made real.
- **[ADR-011](../decisions/011-configuration-as-events-bootstrap.md)** — third-BC instance (after Dispatch, Onboarding); first instance where the *only* event stream in the BC is the config stream.
- **NEW §11 candidate** — stream-processing as the fourth modeling shape.

---

## 4. Ubiquitous Language

| Term | Meaning |
|---|---|
| `LocationPing` | Inbound gRPC message: `{ lat, lng, deviceTimestamp, accuracyMeters, speed?, heading? }`. `driverId` is NOT in the payload — it comes from the authenticated principal (R5). |
| `LocationIngestAck` | gRPC response: `{ acceptedCount, serverTime, throttlePolicyVersion? }`. `serverTime` enables client clock-skew correction. |
| `DriverLocationUpdated` | The Kafka publication (throttled/cell-changed grain). Carries H3 cell id + `serverReceivedAt`. Not stored in an event store. |
| `LastKnownPosition` | Overwrite-in-place document; Telemetry's location-of-record. |
| **H3 cell** | Uber's hexagonal geospatial index. The cell id is **published-language** — both BCs agree on cell system + resolution (contract vocabulary, like ADR-013's canonical id). |
| **cell-change** | The driver crossed an H3 cell boundary at the configured resolution — a publish trigger. |
| **heartbeat** | A publish on the configured interval even absent a cell-change — lets "last-known" mean "live" and rails the deferred v2 staleness-ceiling. |
| **throttle policy** | The cadence + cell resolution + heartbeat interval, sourced from `TelemetryPolicyConfigured`. Versioned via `throttlePolicyVersion`. |
| `serverReceivedAt` | Server-stamped, monotonic. The LWW / dedup key (R7). |
| `deviceTimestamp` | Client clock; skews, client-controllable; informational only — never the dedup key. |
| **ingest window** | A bounded, comparable unit of streaming ingest (short-lived gRPC call). Not per-shift (R5). |
| **driver session** | A *logical* concept, decoupled from any physical stream. |
| **active** | *Pinging*, never *available* (R8). Eviction is by heartbeat-absence. |

---

## 5. Event List

| Event | Stored? | Transport | Notes |
|---|---|---|---|
| `TelemetryPolicyConfigured` | **Yes** — event-sourced singleton stream | — | The *only* event-sourced stream (ADR-011). |
| `DriverLocationUpdated` | No — published, not stored | **Kafka** | Throttled/cell-changed grain; the breadcrumb history lives in the topic. |

`LocationPing` / `LocationIngestAck` are gRPC transport messages, not domain events; the `LastKnownPosition` write is a document upsert, not an event. The sparse event list is the stream-processing shape's signature: a first-class Critter Stack BC with almost no event sourcing in it.

---

## 6. Slice Walk

> Slice ordering (grill skeleton): (1) `TelemetryPolicyConfigured` · (2) GPS ingest · (3) `DriverLocationUpdated` publish · (4) `LastKnownPosition` store + eviction · (5) W001 §5.3 amendment. Each slice commits only after explicit sign-off, closes with a decisions-locked table, and proposes speculative projections (proactive-projections discipline).

### 6.1 Slice 1 — `TelemetryPolicyConfigured` (configuration-as-events)

**Pattern:** Configuration-as-events ([ADR-011](../decisions/011-configuration-as-events-bootstrap.md)), singleton aggregate stream. **The one event-sourced stream in the BC** (§3.4).
**Lane:** Telemetry. **Trigger:** operator admin command (`ConfigureTelemetryPolicy`); plus the migration-time bootstrap seed.

#### Command + Event (full-replacement semantics, per ADR-011)

| Field | Shape | Notes |
|---|---|---|
| `h3Resolution` | int | H3 cell size; tunable in H3's 0–15 range. Operator lever, not hardcoded. |
| `heartbeatIntervalSeconds` | int | Publish even absent a cell-change (R6); rails the deferred v2 staleness-ceiling. |
| `minPublishIntervalSeconds` | int | Throttle floor — caps cell-boundary "thrash" (R6 cadence). |
| `operatorId` | string | `"system-bootstrap"` for the migration seed (ADR-011 audit marker). |
| `reason` | string | `"Initial deployment defaults"` for the seed. |
| `configuredAt` | timestamp | Migration execution time for the seed; operator action time otherwise. |

Full-replacement command/event semantics inherited from ADR-011 (the `DispatchPolicy` / `TripsPolicy` shape).

#### View fed — `TelemetryPolicy`

Latest-policy projection read by the in-flight processor (slices 2/3). Carries the three parameters plus a **`throttlePolicyVersion`** derived from the singleton stream's version (free from Marten, monotonic). That version flows into `LocationIngestAck` (slice 2) so clients can detect a policy change, and onto `DriverLocationUpdated` (slice 3) so Dispatch can interpret cell ids across a resolution change.

#### Bootstrap (inherited, not re-decided)

Migration-time idempotent seed per [ADR-011](../decisions/011-configuration-as-events-bootstrap.md) Option A: load the singleton stream state; if empty, append `TelemetryPolicyConfigured` with documented defaults, `operatorId = "system-bootstrap"`, `reason = "Initial deployment defaults"`. Re-running the migration is a no-op. **Third-BC instance** of ADR-011 (after Dispatch, Onboarding); first where the config stream is the *only* stream in the BC.

**Documented seed defaults** (ops-tunable, not modeling locks): `h3Resolution: 9` (≈ city-block granularity — fine enough to bucket within a ~5 km pickup search, coarse enough to keep cell-change frequency sane), `heartbeatIntervalSeconds: 30`, `minPublishIntervalSeconds: 5`. The modeling decision is "these are tunable policy parameters"; the exact integers are expected to be tuned against real ingest volume.

#### Validation (at the boundary, not the aggregate)

Cross-parameter checks via Wolverine.HTTP FluentValidation `Before()`/`Validate()`: all intervals positive; `heartbeatIntervalSeconds ≥ minPublishIntervalSeconds`; `h3Resolution` within a sane bound. The singleton has no state-transition invariant to defend beyond full-replacement, so the aggregate stays thin.

#### GWT sketches

```
Bootstrap:   Given empty TelemetryPolicy stream
             When the deployment migration runs
             Then TelemetryPolicyConfigured { defaults, operatorId: "system-bootstrap",
                                              reason: "Initial deployment defaults" } is appended

Reconfigure: Given TelemetryPolicy at version 1
             When ConfigureTelemetryPolicy { h3Resolution: 8, heartbeatIntervalSeconds: 20, ... }
             Then TelemetryPolicyConfigured v2 is appended (full replacement);
                  throttlePolicyVersion advances to 2

Reject:      Given ConfigureTelemetryPolicy { heartbeatIntervalSeconds: 0 }
             When validated at the boundary
             Then rejected (ProblemDetails); no event appended
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Stream shape | Singleton `TelemetryPolicy` stream, full-replacement semantics (inherited ADR-011). |
| Bootstrap | Migration-time idempotent seed (ADR-011 Option A). |
| `throttlePolicyVersion` | Derived from the Marten singleton stream version; carried into `LocationIngestAck` and `DriverLocationUpdated`. |
| H3 resolution as policy | Operator-tunable parameter, not hardcoded — cell size is an ops lever. |
| Validation site | HTTP boundary (Wolverine FluentValidation); aggregate stays thin. |
| Seed defaults | `h3Resolution: 9`, `heartbeatIntervalSeconds: 30`, `minPublishIntervalSeconds: 5` — documented, tunable. |

#### Proactive projections

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `TelemetryPolicy` | In-flight processor (slices 2/3) | Latest policy params + `throttlePolicyVersion` | `TelemetryPolicyConfigured` | **Build now** — slice 1 produces it. |
| `TelemetryPolicyTimeline` | Ops / audit | Ordered policy-change history ("what was the policy at T?") | `TelemetryPolicyConfigured` | **Defer** — the event log answers it inline; materialize only if ops demands a view. |

#### Cross-references

- **Forward (Slice 2):** `LocationIngestAck` carries `throttlePolicyVersion` from this slice's view.
- **Forward (Slice 3):** `DriverLocationUpdated` carries the H3 cell id at `h3Resolution`, plus `throttlePolicyVersion` for interpretation across a resolution change.
- **ADR-011:** third-BC instance; first BC where the config stream is the only event stream.

---

### 6.2 Slice 2 — GPS ingest (gRPC client-streaming)

**Pattern:** Stream-processing ingest over gRPC **client-streaming** — not a Klefter decision-event, not aggregate-per-invariant. Many `LocationPing`s in, one `LocationIngestAck` out per window. No event is stored; the slice's job is in-flight processing + the publish-trigger decision. **First gRPC surface in CritterCab** (the WolverineFx.Grpc client-streaming shape that motivated the project).
**Lane:** Telemetry. **Trigger:** a mobile client opens a windowed `ReportLocations` stream.

#### Contract (WolverineFx.Grpc)

```
service Telemetry {
  rpc ReportLocations(stream LocationPing) returns (LocationIngestAck);   // client-streaming
}

LocationPing      { lat, lng, deviceTimestamp, accuracyMeters, speed?, heading? }
                  // driverId is NOT in the payload — it comes from the authenticated principal (R5)
LocationIngestAck { acceptedCount, serverTime, throttlePolicyVersion }
```

Contract artifact → a `.proto` is owed under `/protos/crittercab/telemetry/v1/` (named in §10, authored in a later proto session per [ADR-009](../decisions/009-protobuf-contracts-as-first-class-artifacts.md) + the PR #4 precedent, not inline here).

#### In-flight processing (per ping)

1. **Stamp `serverReceivedAt`** server-side — the monotonic LWW/dedup key (R7). `deviceTimestamp` is retained but never trusted for ordering.
2. **Validate** (lat/lng range, `accuracyMeters` threshold). Invalid pings are **silently dropped**, not errored — `acceptedCount` reflects passes. No per-ping error frame in v1.
3. **Compute the H3 cell** at the policy's `h3Resolution` (from the `TelemetryPolicy` view).
4. **Evaluate the publish trigger** against the driver's `LastKnownPosition`:

```
shouldPublish = heartbeatDue OR (cellChanged AND throttleFloorElapsed)
   where  cellChanged          = currentCell != lastPublishedCell
          heartbeatDue          = now - lastPublishedAt >= heartbeatIntervalSeconds
          throttleFloorElapsed  = now - lastPublishedAt >= minPublishIntervalSeconds
```

On `shouldPublish`, the trigger fans out to **slice 3** (publish `DriverLocationUpdated` → Kafka) and **slice 4** (upsert `LastKnownPosition`). Otherwise the ping is accepted-but-absorbed. Note `heartbeatDue` subsumes the throttle floor by construction (slice-1 validation enforces `heartbeatInterval ≥ minPublishInterval`), so the floor only ever gates *cell-change* publishes — preventing a driver hovering on a cell boundary from flooding Kafka.

#### Window semantics (R5)

Windowed client-streaming: the client opens a stream, sends pings over a bounded period, half-closes to receive the ack. **Window length is client-controlled, bounded by a server max-duration guard** — a window is a bounded, *comparable* unit (R5's rationale for rejecting per-shift streams). The "driver session" remains a logical concept, decoupled from any single physical window.

#### GWT sketches

```
Happy publish:  Given TelemetryPolicy { h3Resolution: 9, heartbeat: 30s, minPublish: 5s }
                  And LastKnownPosition { driverId: D, cell: C1, lastPublishedAt: now-10s }
                When a LocationPing for D arrives that maps to cell C2 (C2 != C1)
                Then the ping is accepted; shouldPublish = true (cellChanged AND floor elapsed);
                     DriverLocationUpdated + LastKnownPosition upsert fire (slices 3/4)

Throttled:      Given LastKnownPosition { driverId: D, cell: C1, lastPublishedAt: now-2s }
                When a LocationPing for D maps to cell C2 (C2 != C1)
                Then the ping is accepted (acceptedCount++); shouldPublish = false (floor not elapsed);
                     no publish, no store

Heartbeat:      Given LastKnownPosition { driverId: D, cell: C1, lastPublishedAt: now-31s }
                When a LocationPing for D maps to the SAME cell C1
                Then shouldPublish = true (heartbeatDue); DriverLocationUpdated + upsert fire

Window close:   Given a client streamed N pings, M of which passed validation
                When the client half-closes the stream
                Then LocationIngestAck { acceptedCount: M, serverTime, throttlePolicyVersion } returns
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Transport / shape | gRPC client-streaming, windowed; v1 only (bidirectional deferred to v2, R5). |
| Window control | Client-controlled length, server max-duration guard; comparable bounded unit. |
| `driverId` source | Authenticated principal, never the payload (R5). |
| Ack cadence | Per window-close: `{ acceptedCount, serverTime, throttlePolicyVersion }`. |
| `serverReceivedAt` | Server-stamped per ping; the LWW/dedup key (R7). |
| Publish trigger | `heartbeatDue OR (cellChanged AND throttleFloorElapsed)`, evaluated against `LastKnownPosition`. |
| Invalid pings | Silently dropped, not errored; `acceptedCount` reflects passes. |
| Trigger-state caching | **Cache-within-window, authoritative re-read on window open** — safe because the driver's own pings are the sole writer during the window; saves a document read per ping. |

#### Proactive projections

| "Read model" | Audience | Shape | Status |
|---|---|---|---|
| `IngestThroughputStats` | Ops dashboard | pings/sec, accepted vs dropped, active-window count, publish ratio | **OpenTelemetry metrics, not a Marten projection** — no event to fold; a Wolverine OTEL meter. |

Finding: in a stream-processing BC, the proactive-projections question often resolves to "this is a metrics/OTEL concern, not a projection," because there are no domain events to fold. The discipline still runs — it lands on a different Critter Stack primitive (Wolverine OTEL meters) than in event-sourced BCs.

#### Cross-references

- **Backward (Slice 1):** reads `TelemetryPolicy` (`h3Resolution`, intervals, `throttlePolicyVersion`).
- **Forward (Slices 3/4):** the `shouldPublish` trigger fans out to the Kafka publish and the document upsert.
- **ADR-005:** first gRPC surface and first client-streaming in CritterCab.
- **§10:** `telemetry/v1` proto authorship owed (`ReportLocations` service + `LocationPing` / `LocationIngestAck`).

---

### 6.3 Slice 3 — `DriverLocationUpdated` → Kafka publish

**Pattern:** Kafka publication of a high-volume telemetry stream ([ADR-005](../decisions/005-transport-selection-by-flow-type.md)); the supplier half of [ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md). **Published, not stored** — the topic *is* the breadcrumb history (§3.2). **First Kafka topic in CritterCab.**
**Lane:** Telemetry → Dispatch (over Kafka). **Trigger:** the slice-2 `shouldPublish` decision.

#### Event shape — `DriverLocationUpdated`

| Field | Notes |
|---|---|
| `driverId` | **Kafka partition key** (R7) → per-driver ordering becomes a transport property. |
| `lat`, `lng` | The published position. |
| `h3Cell` | The H3 index at the policy resolution — **published-language** (both BCs agree on the cell system). |
| `h3Resolution` | Carried explicitly for consumer clarity (H3 self-encodes resolution, but clarity > bit-decoding). |
| `serverReceivedAt` | The **dedup / LWW key** (R7). |
| `throttlePolicyVersion` | Provenance — which policy produced this cell. |
| `speed?`, `heading?` | Optional pass-through. |

#### Kafka specifics (R7, via WolverineFx.Kafka)

- **Topic:** `telemetry.driver-location-updated` — extends [ADR-014](../decisions/014-asb-topic-naming-convention.md)'s `<source-bc>.<event-name-kebab>` shape (ADR-014 is currently ASB-scoped; generalizing it is a §11 candidate, below).
- **Partition key = `driverId`** → per-driver ordering is guaranteed by the transport, so R3's monotonic field demotes to a **dedup backstop** rather than the primary ordering mechanism.
- **Dedup / last-writer key = `serverReceivedAt`** — server-stamped, monotonic; the consumer dedups on `(driverId, serverReceivedAt)` against at-least-once redelivery.

#### Publish/store coupling — no outbox, publish-first (consistency note)

In the event-sourced BCs an ASB publish is outbox-coordinated with the Marten event append (ADR-014). Here the paired write is a *document upsert* (`LastKnownPosition`, slice 4), not an event append, and the consistency model is explicitly eventual/LWW (R3). **Decision: no outbox; publish-to-Kafka first, then upsert.** The failure modes are asymmetric and publish-first picks the benign one:

- *Publish succeeds, upsert fails* → next ping re-evaluates against a stale baseline and may **republish** → consumer `serverReceivedAt` dedup absorbs it. Cheap.
- *Upsert succeeds, publish fails* → Dispatch **misses** this cell-change until the next heartbeat → brief staleness, reconciled at acceptance (R3).

A dedup-absorbed duplicate is strictly cheaper than a staleness-causing miss, so publish-first is the safer ordering; the **heartbeat is the self-healing backstop** (any dropped publish is corrected within `heartbeatIntervalSeconds`). Recorded explicitly as a stream-processing-shape property: **telemetry trades the outbox's exactly-once for throughput, because LWW + heartbeat self-heals.**

#### GWT sketches

```
Publish:   Given the slice-2 trigger fired shouldPublish for driver D at cell C2
           When the publish runs
           Then DriverLocationUpdated { driverId: D, lat, lng, h3Cell: C2, h3Resolution: 9,
                serverReceivedAt, throttlePolicyVersion } is published to
                telemetry.driver-location-updated, partitioned by D

Dedup:     Given at-least-once redelivery of the same { driverId: D, serverReceivedAt: T }
           When the Dispatch consumer receives the duplicate
           Then it dedups on (D, T); the projection applies the position at most once
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Topic | `telemetry.driver-location-updated` (extends ADR-014; generalization is a separate ADR candidate). |
| Partition key | `driverId` — per-driver ordering as a transport property. |
| Dedup / LWW key | `serverReceivedAt`; consumer dedups on `(driverId, serverReceivedAt)`. |
| Payload | `driverId, lat, lng, h3Cell, h3Resolution, serverReceivedAt, throttlePolicyVersion, speed?, heading?`. |
| Publish/store coupling | No outbox; publish-first, then upsert; LWW dedup + heartbeat self-heal (contrast with ASB outbox path). |
| Storage | Published, not stored — the topic is the breadcrumb history. |

#### Proactive projections

Nothing new on the Telemetry side (this is a publish). The **consumer-side** projection (Dispatch's `NearbyAvailableDrivers`) is resolved in slice 5 / the W001 §5.3 amendment — the Kafka half of ADR-018's multi-transport projection.

#### Cross-references

- **Backward (Slice 2):** fired by the `shouldPublish` trigger.
- **Forward (Slice 4):** paired document upsert (publish-first ordering).
- **Forward (Slice 5):** Dispatch consumes this topic into `NearbyAvailableDrivers`.
- **§11 candidate fired:** Kafka topic-naming convention (first Kafka topic lands) — generalize ADR-014 transport-agnostically; own ADR, not folded here.

---

### 6.4 Slice 4 — `LastKnownPosition` store + heartbeat-absence eviction

**Pattern:** Overwrite-in-place document (location-of-record) + periodic eviction. Stream-processing, no events (R4, R8). Document sketched in §3.3.
**Lane:** Telemetry. **Trigger:** the slice-2/3 publish-trigger (upsert paired with the Kafka publish, *publish-first* per slice 3); eviction is a separate periodic sweep.

#### The upsert — and a sharper definition of "last known"

On `shouldPublish`, overwrite the driver's document with the new position, `h3Cell`, and `serverReceivedAt`. **The document is upserted only on a publish, not on every ping.** So `LastKnownPosition` is precisely the driver's **last *published* position** — not their last *received* ping. That is correct: within a cell the position is "good enough" (cell granularity is the point), and the heartbeat guarantees freshness within `heartbeatIntervalSeconds`. The document doubles as (a) Telemetry's location-of-record and (b) the slice-2 trigger baseline (`lastPublishedCell` / `lastPublishedAt`) — one document serving both roles, with no per-ping write. Upsert-on-publish keeps the document store proportional to the *throttled* rate, not raw GPS, and keeps the slice-2 trigger self-consistent (it compares against the last thing actually published).

#### Eviction (R8) — the v1/v2 boundary

A driver who stops pinging (off-shift, app crash, lost signal) should have their document evicted. **Mechanism: a periodic sweep handler** (Wolverine recurring/scheduled message) that deletes `LastKnownPosition` documents whose `serverReceivedAt` is older than the eviction threshold. No per-driver timers.

- **Eviction threshold:** `3 × heartbeatIntervalSeconds` (3 consecutive missed heartbeats), as a **documented constant — not** a 4th `TelemetryPolicy` parameter in v1. Keeps slice 1's policy shape stable and ties eviction semantically to the heartbeat (eviction *is* "heartbeat absence"). Promote to a policy param in v2 if ops needs to tune it independently.
- **Eviction does NOT publish a staleness event in v1.** Telemetry evicting its own document does not, by itself, tell Dispatch the driver went stale. Per **R3** (staleness-ceiling deferred to v2) and **R8** (Dispatch's availability filter is the net), eviction is **Telemetry's own storage housekeeping only**; propagation to Dispatch is the **v2 staleness-ceiling** (Dispatch ages out old positions in its own projection). For v1:
  - A driver who genuinely went off-shift also emits a Driver Profile availability transition over ASB → Dispatch's availability filter excludes them regardless of the stale Telemetry position.
  - The rare *app-crash-without-offline-event* case is **accepted v1 staleness**, reconciled at acceptance (`OfferRevoked` / the `OfferAccepted` guard, R3).
  - Eviction's v1 purpose is therefore bounded: Telemetry storage hygiene + ensuring a **returning** driver's first ping is treated as a fresh cell-change (no baseline document ⇒ slice-2 trigger publishes immediately).

#### GWT sketches

```
Upsert:    Given the slice-2 trigger fired shouldPublish for driver D at cell C2
           When the publish-first ordering completes
           Then LastKnownPosition[D] is overwritten with { lat, lng, h3Cell: C2, serverReceivedAt }

No-write:  Given a ping for D is accepted but shouldPublish = false
           Then LastKnownPosition[D] is unchanged (no per-ping write)

Evict:     Given LastKnownPosition[D].serverReceivedAt is older than 3 × heartbeatIntervalSeconds
           When the periodic sweep runs
           Then LastKnownPosition[D] is deleted; no staleness event is published (v1)

Return:    Given D was evicted and pings again
           When the slice-2 trigger evaluates with no baseline document
           Then shouldPublish = true (absent baseline); D republishes immediately
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Document write cadence | Upsert on publish only, not per ping — writes proportional to the throttled rate. |
| "Last known" semantics | Last *published* position (cell-granularity location-of-record), not last received ping. |
| Eviction mechanism | Periodic sweep handler (no per-driver timers). |
| Eviction threshold | `3 × heartbeatIntervalSeconds` (3 missed heartbeats), documented constant — not a v1 policy param. |
| Eviction propagation | No staleness event in v1; deferred to the v2 staleness-ceiling (R3); availability filter is the v1 net (R8). |
| Returning driver | Absent baseline ⇒ slice-2 trigger publishes immediately (consistent with slice 2). |

#### Proactive projections

| Projection | Audience | Shape | Feeders | Status |
|---|---|---|---|---|
| `ActiveDriversByCell` | Operations live-map / ops dashboard | H3 cell → count of recently-pinging drivers | `LastKnownPosition` (Marten aggregation) | **Defer to Operations BC** — the one genuinely useful read model over the document store; cross-cutting Operations territory. Named now per the discipline. |

#### Cross-references

- **Backward (Slices 2/3):** upsert paired with the Kafka publish (publish-first); the document is the slice-2 trigger baseline.
- **Forward (Slice 5):** Dispatch's `NearbyAvailableDrivers` is the *consumer-side* projection; Telemetry's `LastKnownPosition` is the *supplier-side* location-of-record — distinct documents in distinct BCs.
- **v2 deferrals:** staleness-ceiling propagation; eviction-threshold-as-policy-param; bidirectional streaming (R5).

---

### 6.5 Slice 5 — W001 §5.3 amendment: Dispatch's `NearbyAvailableDrivers` population (Kafka ⋈ ASB)

**Pattern:** Multi-transport local view in Dispatch — the consumer half of [ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md). Resolves [W001 §10 parking-lot #4](001-dispatch-event-model.md).
**Lane:** Dispatch (consumer). **Feeders:** `DriverLocationUpdated` (Kafka, slice 3) ⋈ Driver Profile availability events (ASB, forward-constraint).

#### The view — a precise statement of what it is

`NearbyAvailableDrivers` is a **Dispatch-local document view**: per-driver `AvailableDriver` documents maintained by two sets of Wolverine message handlers (one Kafka, one ASB) via LWW upsert. It is **not** a Marten event projection — event-sourcing every inbound `DriverLocationUpdated` would reimport the per-ping volume the telemetry throttle exists to suppress, onto Dispatch's event store. So Dispatch consumes the telemetry stream the same way Telemetry produces it: document upsert, not event sourcing. An event-sourced BC holds a deliberately non-event-sourced view for this one high-volume input — the stream-processing shape leaking across the boundary, by necessity.

```
AvailableDriver {
  driverId,                        // join key
  h3Cell, lat, lng,                // location side  — Kafka DriverLocationUpdated (slice 3)
  serverReceivedAt,                // location LWW / dedup key
  availabilityState, vehicleClass, // availability side — ASB (Driver Profile, forward-constraint)
  availabilityUpdatedAt            // availability LWW key
}
```

This **vindicates** W001 §5.3's original "No external call — operates entirely on already-available views" framing: under Option A (Telemetry gRPC query) that line would have become false; under Option B the view is locally handler-maintained, so candidate selection still makes no synchronous external call. H3 makes "radius" cheap: a pickup-radius search is an H3 k-ring around the pickup cell + exact-distance filter, bucketed by the same published cell id Telemetry emits.

#### Consistency & the stub seam

- **Eventual, LWW per driver per side** — location LWW on `serverReceivedAt`, availability LWW on its own ordering key; dedup on `(driverId, serverReceivedAt)` for the Kafka side (R3, R7).
- **Insertion point exists:** slice 5.3 built `INearbyAvailableDriversSource` as a stub seam precisely so a real source could slot in without reshaping `CandidateSelectionAutomation`. This document-backed view *is* that source; the stub (`NearbyAvailableDriversStub`) is replaced, handlers untouched. The decliners-exclusion stays a per-request filter on the view (W001 §5.3).

#### What this slice amends in W001 (cross-workshop close)

| W001 §5.3 element | Amendment |
|---|---|
| Parking-lot #4 ("view population transport deferred") | **Closed** — Kafka ⋈ ASB local document view per ADR-018. |
| "Decisions locked" → *View population transport* row | Resolved: Kafka (location) ⋈ ASB (availability), Dispatch-local document view. |
| "No external call. Operates entirely on already-available views." | **Retained and explained** — locally handler-maintained; selection still makes no synchronous call. |
| Translation-in sources table | Confirmed: Telemetry `DriverLocationUpdated` (Kafka) + Driver Profile availability (ASB). |

#### GWT sketches

```
Location update: Given DriverLocationUpdated { driverId: D, h3Cell: C, serverReceivedAt: T } from Kafka
                 When the Dispatch Kafka handler runs and T > AvailableDriver[D].serverReceivedAt (LWW)
                 Then AvailableDriver[D] location side is upserted to (C, lat, lng, T)

Availability:    Given DriverWentOffline { driverId: D } from ASB (Driver Profile)
                 When the Dispatch ASB handler runs
                 Then AvailableDriver[D].availabilityState = Offline

Selection read:  Given a FareQuoted for pickup P, vehicleClass STANDARD
                 When CandidateSelectionAutomation queries NearbyAvailableDrivers
                 Then it reads AvailableDriver docs in the H3 k-ring around P,
                      filtered to available + STANDARD-capable, exact-distance ranked
```

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| View nature | Dispatch-local **document view** (per-driver docs), handler-maintained, LWW upsert — not a Marten event projection (volume). |
| Feeders | Kafka `DriverLocationUpdated` (location) ⋈ ASB availability events (availability, forward-constraint). |
| Join key | `driverId`. |
| Radius query | H3 k-ring over published cell ids + exact-distance filter. |
| Consistency | Eventual, LWW per driver per side; dedup on `(driverId, serverReceivedAt)`. |
| Stub seam | `INearbyAvailableDriversSource` stub replaced by the document-backed view; handlers untouched. |

#### Proactive projections

| Projection | Audience | Shape | Status |
|---|---|---|---|
| `CandidatePoolHealth` | Operations | available-driver count per cell × vehicleClass | **Defer to Operations BC** — overlaps `ActiveDriversByCell` (slice 4) but on Dispatch's *joined* view; named now. |

#### Cross-references

- **Backward (Slice 3):** consumes `telemetry.driver-location-updated`.
- **Boundary:** ASB availability half is a forward-constraint to Driver Profile's workshop (ADR-018; [context-map edge #6](../context-map/README.md#6-intra-actor-topology-deferred)).
- **Locks** [context-map edge #5](../context-map/README.md#5-telemetry--dispatch-deferred) supplier side; **resolves** W001 §10 #4 / §11 #3.

---

**Slice walk complete (5 slices).**

---

## 7. Translation Slices at BC Boundaries (cross-reference)

| Slice | Direction | Counterparty | Local effect | Klefter? |
|---|---|---|---|---|
| 5.5 (W006) | in | Telemetry (Kafka) + Driver Profile (ASB) | `NearbyAvailableDrivers` document view (Dispatch-local) | **Non-Klefter** — pure view maintenance, no local decision-event |

Telemetry itself is translation-light: ingest (slice 2) is a gRPC transport surface, not domain translation. The one consumer-side translation is slice 5, on Dispatch's side, and it is the first **non-Klefter, non-event-sourced** Translation-in in CritterCab (document-view maintenance rather than a `*Recorded` decision-event — the volume forbids event-sourcing the inbound).

## 8. Temporal Automation (cross-reference)

| Mechanism | Slice | Pattern |
|---|---|---|
| Heartbeat publish | 2/3 | Time-triggered publish absent a cell-change (R6); recurring, not a per-entity Bruun timeout. |
| Eviction sweep | 4 | Periodic sweep deleting `LastKnownPosition` docs older than `3 × heartbeatIntervalSeconds`. |

Distinct from W004's Bruun let-state-decide timeouts: these are recurring schedules over the whole document set, not per-aggregate scheduled messages.

## 9. Configuration-as-Events (cross-reference)

| Stream | Slice | Parameters | Bootstrap |
|---|---|---|---|
| `TelemetryPolicy` (singleton) | 1 | `h3Resolution`, `heartbeatIntervalSeconds`, `minPublishIntervalSeconds` | Migration-seeded (ADR-011 Option A); `operatorId = "system-bootstrap"` |

Third-BC instance of [ADR-011](../decisions/011-configuration-as-events-bootstrap.md) (after Dispatch, Onboarding); **first BC where the config stream is the only event-sourced stream** — config-as-events proven orthogonal to whether the domain core is event-sourced.

## 10. Candidate Protobuf Contract Surface

Authored in a follow-up proto session (PR #4 precedent), not inline ([ADR-009](../decisions/009-protobuf-contracts-as-first-class-artifacts.md)). Under `/protos/crittercab/telemetry/v1/`:

| Artifact | Shape |
|---|---|
| `Telemetry` service — `ReportLocations(stream LocationPing) returns (LocationIngestAck)` | gRPC client-streaming ingest (slice 2) |
| `LocationPing` | `{ lat, lng, deviceTimestamp, accuracyMeters, speed?, heading? }` — `driverId` NOT in payload (R5) |
| `LocationIngestAck` | `{ acceptedCount, serverTime, throttlePolicyVersion }` |
| `DriverLocationUpdated` | `{ driverId, lat, lng, h3Cell, h3Resolution, serverReceivedAt, throttlePolicyVersion, speed?, heading? }` — Kafka payload (slice 3); H3 cell id is published-language |

## 11. ADR Candidates Surfaced by This Workshop

The three candidates the front-loading grill carried, now triggered or firm:

1. **Kafka topic-naming convention.** Generalize [ADR-014](../decisions/014-asb-topic-naming-convention.md) (currently ASB-scoped) to a transport-agnostic `<source-bc>.<event-name-kebab>` convention. **Trigger fired:** first Kafka topic lands (slice 3 / the W006 build). Own ADR (one-decision-per-ADR), *not* folded into ADR-018.
2. **Stream-processing as the fourth modeling shape.** Telemetry as a non-event-sourced stream-processing BC, parallel to the shape candidates W004 (Process Manager via Handlers) and W005 (ACL-translation-dominant) minted. **Firm.** Trigger: first Telemetry implementation slice.
3. **Windowed gRPC client-streaming ingest pattern.** The windowed-vs-per-shift convention (R5). Trigger: first gRPC ingest slice. **May fit better as a `docs/skills/` skill than an ADR** — decide at authoring.

## 12. Parking Lot / Open Questions

| Item | Disposition |
|---|---|
| v2 staleness-ceiling exclusion (Dispatch ages out stale positions; propagates Telemetry eviction) | Deferred to v2 (R3). |
| Bidirectional streaming ingest | Deferred to v2 (R5; v1 is client-streaming). |
| Eviction threshold as a `TelemetryPolicy` parameter | v2, if ops wants to tune independently of heartbeat (slice 4). |
| Surge-pricing Kafka consumer | Post-MVP — ADR-005's second Kafka consumer; out of scope. |
| Driver Profile availability publication shape (ASB) | Forward-constraint; locks at the Driver Profile workshop (ADR-018, context-map edge #6). |
| `ActiveDriversByCell` / `CandidatePoolHealth` ops projections | Defer to Operations BC. |
| Multi-vehicle / vehicleClass switching mid-window | Out of scope (W001 §5.3: one vehicle in service at a time for v1). |

## 13. Retrospective

### 13.1 Intent vs. outcome

Resume the paused W006 design (grill R1–R8, 2026-06-25) after signing off ADR-018. Outcome: ADR-018 accepted; five slices walked and signed off; cross-workshop amendments queued (same PR). EM-direct, grill-front-loaded — no re-grilling.

### 13.2 What worked

- **The grill front-loading paid off exactly as W005 predicted.** Every hard call (R1–R8) was pre-resolved; the slice walk was authoring, not deliberation. Genuine slice-level micro-decisions (ack cadence, eviction threshold, publish/store coupling) surfaced and resolved cleanly with leaning opinions.
- **The 4th-shape framing (§3) clarified every slice.** Once "not event-sourced" was locked, the sparse event list, the document upsert, and the consumer-side document view all followed without friction.
- **The slice-5.3 stub seam (`INearbyAvailableDriversSource`) made the consumer side a clean insertion.** The seam was built for exactly this; slice 5 fills it with no handler reshaping.

### 13.3 Patterns established for future workshops

- **Stream-processing modeling shape** — non-event-sourced domain core; documents + transport streams; config-as-events as the lone event stream.
- **Document-view-on-the-consumer-side** — an event-sourced BC may hold a deliberately non-event-sourced document view for a high-volume inbound stream (volume forbids event-sourcing it).
- **Publish-first, no-outbox for telemetry** — trade exactly-once for throughput when LWW + heartbeat self-heals (contrast with the ASB business-event outbox path).
- **H3 cell id as published-language** — agreed cell system + resolution is contract vocabulary (like ADR-013's canonical id), enabling cheap k-ring radius queries on the consumer side.
- **Proactive-projections-resolve-to-OTEL-metrics in stream-processing BCs** — with no events to fold, the ops read model is a Wolverine OTEL meter, not a Marten projection. The discipline still runs; it lands on a different primitive.

### 13.4 ADR evidence

- **[ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md)** — first exercise (this workshop realizes the Telemetry/Kafka supplier half + the Dispatch consumer view).
- **[ADR-005](../decisions/005-transport-selection-by-flow-type.md)** — first concrete Kafka topic *and* first gRPC client-streaming surface in CritterCab.
- **[ADR-011](../decisions/011-configuration-as-events-bootstrap.md)** — third-BC instance; first where the config stream is the only event stream.

### 13.5 Follow-ups generated

- **3 ADR candidates** (§11) with triggers.
- **Telemetry protobuf authorship** (§10) — `telemetry/v1` service + messages; bundled-proto session per PR #4 precedent.
- **Cross-workshop amendments (this PR):** W001 §5.3 (parking-lot #4 closed); context-map edges #5/#6; vision-doc override note.
- **Dispatch implementation follow-up:** replace `NearbyAvailableDriversStub` with the Kafka ⋈ ASB document view (the first real transport wiring in the codebase — see `docs/planning/2026-06-25-state-of-the-repo-transport-and-critterwatch.md`).

### 13.6 Workshop status

**Complete (v0.1, 2026-06-30)** pending the cross-workshop amendments landing in this PR (§13.5). Event model for the Telemetry bounded context is ready to serve as input to narrative authoring and implementation prompts.

---

## Document History

- **v0.1** (2026-06-30): Full workshop authored in one session, resuming the 2026-06-25 grill-with-docs pause (R1–R8) after ADR-018 sign-off. Scope, stream-processing shape sidebar (§3, CritterCab's fourth modeling shape), UL, event list, five slices walked with per-slice sign-off, cross-reference tables, three ADR candidates, parking lot, retrospective. Cross-workshop amendments (W001 §5.3, context-map edges #5/#6, vision-doc override) land in the same PR.
- **2026-07-10** — §6.1 (Slice 1) **realized in code**. The `CritterCab.Telemetry` service skeleton and the `TelemetryPolicyConfigured` config-as-events singleton landed: `ConfigureTelemetryPolicy` command with boundary FluentValidation, the `TelemetryPolicy` self-aggregating live-stream view carrying `throttlePolicyVersion` (typed `long` from the Marten stream version), and the config-as-events bootstrap seed via Marten's `IInitialData` idempotent seam (defaults `h3Resolution: 9`, `heartbeatIntervalSeconds: 30`, `minPublishIntervalSeconds: 5`; `operatorId = "system-bootstrap"`). **Open reconciliation:** §6.1 locks "ADR-011 **Option A** (migration-time seed)"; `IInitialData` realizes Option A's intent under the JasperFx `resources setup` deploy step but also self-seeds at host start (Option B's timing) — benign here (idempotent guard + full-replacement), flagged for an ADR-011 amendment describing the Marten realization (retro + `docs/skills/DEBT.md`). Three Alba GWTs (bootstrap / reconfigure / reject) cover the slice. **First config-as-events instance realized in code** in the repo (ADR-011's third instance overall, after Dispatch and Onboarding — both design-only) and **first FluentValidation use in code**. The Telemetry BC is CritterCab's second service. Transport slices (2 gRPC ingest, 3 Kafka publish, 4 `LastKnownPosition`, 5 Dispatch consumer) remain pending; none of §11's three ADR candidates fired this session (all later-arc). Session: [`prompts/implementations/006-telemetry-skeleton-and-slice-1-config.md`](../prompts/implementations/006-telemetry-skeleton-and-slice-1-config.md).
- **2026-07-04** — §10 candidate protobuf surface **realized**. The Telemetry `v1` contracts were authored under `protos/crittercab/telemetry/v1/`: `TelemetryService.ReportLocations(stream LocationPing) → LocationIngestAck` (client-streaming ingest, §6.2) and `DriverLocationUpdated` (Kafka event, §6.3). The ubiquitous-language message names `LocationPing`/`LocationIngestAck` (§4) were preserved via a file-scoped `buf.yaml` lint exception rather than renamed to buf-`STANDARD` `...Request`/`...Response`. Implementation forward-constraint recorded (not a design change): WolverineFx.Grpc has no client-streaming auto-codegen adapter on the verified 5.x line, so §6.2's "first gRPC client-streaming surface" may need hand-wiring against `IMessageBus` (re-verify against 6.8 when implementation begins). Session: [`prompts/decisions/003-protobuf-telemetry-v1.md`](../prompts/decisions/003-protobuf-telemetry-v1.md).
