# Prompt 009 — Dispatch consumes `telemetry.driver-location-updated` (W006 slice 5 / W001 §5.3 close)

| Field | Value |
|---|---|
| **Status** | **Complete (2026-07-24)** — executed as PR D; retro at [`retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md`](../../retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md). A **fourth** fork surfaced during execution and was escalated rather than absorbed: the listener's cold-start offset policy (`BeginAtLatest()`, with a fixture warm-up handshake as the test cost). Originally: Ready — three durable forks resolved by user sign-off 2026-07-24 (availability half: exclude-until-ASB with an unbound handler; H3 query resolution: track last-received; ETA: derived from distance via a named invented constant). All eight Verify-before-wiring gates closed at authoring time. Rides in the PR D session's PR alongside the implementation; not committed standalone. |
| **Authored** | 2026-07-24 |
| **Target artifacts** | `src/CritterCab.Dispatch/CritterCab.Dispatch.csproj` (Kafka + Protobuf + H3 + proto codegen), `src/CritterCab.Dispatch/AvailableDrivers/` (**new slice folder** — document, two handlers, the query adapter, the H3 helper), `src/CritterCab.Dispatch/Program.cs` (Kafka listener, Marten document schema, source registration swap), `tests/CritterCab.Dispatch.Tests/AvailableDrivers/` (**new** Kafka-backed fixture + suites), `apphost.cs` (Dispatch → Kafka reference), `docs/workshops/006-telemetry-event-model.md` (§6.5 Document History), `docs/workshops/001-dispatch-event-model.md` (§5.3 realization note), `docs/skills/wolverine-kafka/SKILL.md` (listener sections rewritten from shipped code — the deferred DEBT row), `docs/skills/DEBT.md` (close that row), `docs/prompts/README.md` (index entry), this prompt's retro. |
| **Source-of-truth dependencies** | [W006 §6.5 (the slice), §6.3's Dedup GWT (consumer-side, untestable until now), §3.2 (stream-processing shape)](../../workshops/006-telemetry-event-model.md); [W001 §5.3 (the amendment target) + §10 parking-lot #4](../../workshops/001-dispatch-event-model.md); [ADR-018](../../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md) (consumer half); [ADR-005](../../decisions/005-transport-selection-by-flow-type.md); [ADR-009](../../decisions/009-protobuf-contracts-as-first-class-artifacts.md); [ADR-019](../../decisions/019-transport-agnostic-topic-naming.md). Skills: **`wolverine-handlers`** (base shape), **`wolverine-messaging-handlers`** (the governing skill for both new handlers), **`wolverine-kafka`** (transport wiring — and the one this session corrects), `marten-querying`, `transport-selection`, `protobuf-contracts`, `vertical-slice-organization`, `csharp-coding-standards`, `testing-integration`, `testing-fundamentals`, `service-bootstrap`, `aspire`. **`wolverine-marten-automation` does *not* govern this session** — see § Skill corrections. |
| **Workflow position** | **PR D** — fourth implementation session of the W006 chain and the last pending W006 slice. First session to write code in `CritterCab.Dispatch` since slice 5.3 (prompt 005). Follows PR [#46](https://github.com/erikshafer/CritterCab/pull/46). **The next session must be a design-or-tidy return** — see § Follow-on PR sequence. |

---

## Framing — why this session exists

Every prior session in this chain built one side of a boundary. This one crosses it.

Until now "CritterCab has two services" has meant two services that have never spoken. Telemetry accepts a gRPC ping, throttles it, publishes `DriverLocationUpdated` to a Kafka topic — and nothing reads it. Dispatch quotes fares and selects candidates entirely in-process over Marten, from a hardcoded `NearbyAvailableDriversStub`. After this session a GPS ping entering Telemetry over gRPC comes out the other side as a row in Dispatch's document store. **That is CritterCab's first cross-service flow**, and it is the thing the whole project exists to demonstrate.

Three shifts make this session unlike the last three, and each one is a place to slow down:

1. **It lands in `CritterCab.Dispatch`, not Telemetry.** Dispatch has never had a transport. Its `Program.cs` has no `UseKafka*`, its `.csproj` has no protobuf codegen, and its test fixture has no broker. All three change here.
2. **Dispatch generates its own copy of a Telemetry-owned proto.** This is the first time two services compile the same `.proto`. It is *not* a shared assembly and must never become one — the no-shared-code constraint holds, and gate 3 below explains why Wolverine tolerates two independently generated copies of the same type.
3. **The ADR-018 join is only half-built by this PR, deliberately.** Slice 5 does the Kafka half (driver positions). The ASB half (Driver-Profile availability) is a forward-constraint to an un-workshopped BC. **Do not model or build it.** Fork 1 below settles exactly how far the availability side is allowed to go.

**No narrative anchors this session.** The consumer side of a telemetry feed has no protagonist-perceivable moment; PR #40's reasoning for Telemetry applies unchanged. W006 §6.5 and W001 §5.3 are the direct spec anchors.

**One negative instruction, as load-bearing as anything positive here:** `CandidateSelectionAutomation.cs` must not appear in the diff. W006 §6.5 promises "the stub is replaced, handlers untouched," and that promise is the entire payoff of having built `INearbyAvailableDriversSource` as a seam back in slice 5.3. If the session finds itself editing the automation, the adapter is the wrong shape.

---

## Goal

Stand up a Wolverine Kafka listener in Dispatch that consumes `telemetry.driver-location-updated` into a per-driver `AvailableDriver` document via atomic last-writer-wins upsert, back `INearbyAvailableDriversSource` with an H3 k-ring query over that document store, demote `NearbyAvailableDriversStub` from production registration to test double, and close W001 §5.3's parking-lot #4 in code.

---

## Spec delta

- **W006 §6.5 moves designed → realized in code.** The `AvailableDriver` document, the LWW-per-side consistency model, the H3 k-ring radius query, and the stub-seam replacement all become concrete. This is the **last pending W006 slice** — the workshop's slice walk closes.
- **W006 §6.3's Dedup GWT is finally exercised.** PR C structurally could not test it: it asserts *consumer* behaviour against at-least-once redelivery, and there was no consumer. It lands here as a real test against a real broker.
- **W001 §5.3's amendment moves from recorded to realized.** The 2026-06-30 W006 amendment already resolved parking-lot #4 *on paper*; this session adds the realization note saying the transport now runs, the view exists, and the stub is no longer in the production graph. **An amendment to an amendment — a realization record, not a new design decision.**
- **W006 §6.5 gains three implementation-time qualifiers it did not name** (the three forks below): what a driver with no availability side means before Driver Profile exists; where the H3 query resolution comes from when the resolution is Telemetry's to set; and how `EtaSeconds` is produced by a view with no ETA source. **Amendments, not corrections** — §6.5 is silent on all three rather than wrong about any.

---

## Orientation files (read in order)

1. This prompt, end to end, including § Decisions resolved.
2. [`docs/workshops/006-telemetry-event-model.md`](../../workshops/006-telemetry-event-model.md) **§6.5** (the slice), then **§6.3** (the publish this consumes + the Dedup GWT), then **§3.2/§3.3** (why none of this is event-sourced).
3. [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) **§5.3**, including its 2026-06-30 W006 amendment at the end — that is what this session realizes.
4. [`ADR-018`](../../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md) — why the view is Dispatch-owned rather than a Telemetry query.
5. `src/CritterCab.Telemetry/Program.cs` § Kafka block and `ReportLocations/KafkaDriverLocationPublisher.cs` — **the producer this consumer must mirror.** The listener's serialization and naming must match what ships there.
6. `src/CritterCab.Telemetry/LastKnownPosition/` — **the closest working precedent in the repo** for a plain-document, non-event-sourced write path, which is exactly what `AvailableDriver` is. Read it before designing the document.
7. `src/CritterCab.Dispatch/CandidateSelection/INearbyAvailableDriversSource.cs` and `CandidateSelectionAutomation.cs` — the port to satisfy and the consumer that must not change.
8. `src/CritterCab.Telemetry/ReportLocations/H3CellIndexer.cs` — **read the comment, not just the code.** The lat/lon axis-and-units footgun it documents applies identically to this session's k-ring helper.
9. `docs/skills/wolverine-kafka/SKILL.md` — publish-path sections are corrected and trustworthy; **§ Listening, § Consumer groups, § Batch processing and § DLQ are known-wrong** (they name a `LocationPing` → `telemetry.location-pings` pairing that never existed). Read them for mechanics only. Fixing them is this session's deliverable.
10. `tests/CritterCab.Telemetry.Tests/` Kafka fixture — the Testcontainers pattern to mirror on the Dispatch side.

---

## Working pattern

- Branch off `main`; never commit to `main`. One prompt = one session = one PR; the retro ships **inside** this PR.
- Run `critter-skill-auditor` Phase 1 before cutting code and Phase 2 after. Phase 1 materially corrected an already-source-verified prompt last session; assume it will again.
- Build and test locally — Docker works, so Testcontainers runs locally. **Do not lean on CI as the only gate**, and note that CI still cannot build `apphost.cs`, so run `dotnet build apphost.cs` by hand after touching it.
- Escalate rather than silently resolve: if a skill contradicts a spec, or the spec is silent on something load-bearing, put it to the user. Both prior sessions had authoring hypotheses overturned this way.
- Run `code-review` before opening the PR. Its two-axis form found three real defects in PR C after two auditor passes had already run.
- Surface the full PR URL on open.

---

## Verify before wiring (`jasperfx-source-verifier` — local `C:\Code\JasperFx\wolverine` @ `V6.21.0-12-ge08abdeb3`, `C:\Code\JasperFx\marten` @ `V9.18.0`)

**All eight gates were closed at authoring time** — the second consecutive prompt to achieve this. **Three gate premises were contradicted by source**, and each would have produced wrong code. Do not re-verify these; do not re-derive them.

| # | Gate | Outcome |
|---|---|---|
| 1 | Does `.UseProtobufSerialization()` bind on the Kafka **listener** side? | **Yes.** It is a generic extension on `IEndpointConfiguration<T>` (`Wolverine.Protobuf/WolverineProtobufSerializationExtensions.cs:33`), and `KafkaListenerConfiguration` satisfies the constraint through `InteroperableListenerConfiguration` → `ListenerConfiguration` → `IListenerConfiguration<T>`. **Caveat:** protobuf-over-Kafka is *unexercised upstream* — it appears on no Kafka endpoint anywhere in the Wolverine repo. CritterCab is its first integration test. |
| 2 | How does the listener learn the incoming message **type**? | **Premise contradicted.** The type does not have to come from endpoint config: the default `KafkaEnvelopeMapper` writes a `message-type` header on publish, and the shipped producer uses that default mapper, so the header **is** on the wire. `.DefaultIncomingMessage<T>()` (`ListenerConfiguration.cs:456`) exists and *replaces* the header mapping with a constant — use it as hardening on a single-type topic, not as a workaround. There is **no** `ListenToKafkaTopic<T>(...)` generic overload and no `.ReceivesMessage<T>()` fluent method. |
| 3 | Will Dispatch's independently generated `DriverLocationUpdated` agree with Telemetry's on the wire? | **Yes.** Wolverine's identity is `Type.FullName`-based and **assembly-agnostic** (`WolverineMessageNaming.cs:86-107`, `Envelope.cs:52`). Both resolve to `CritterCab.Telemetry.V1.DriverLocationUpdated`. **This is what makes the no-shared-assembly rule workable.** The fragile dependency is the proto's `csharp_namespace` matching across both builds, and the message staying **top-level** (a nested proto type gets a `DeclaringType_` prefix). |
| 4 | Does Dispatch's `CustomizeHandlerDiscovery(d => d.Includes.WithNameSuffix("Automation"))` **replace** the default conventions? | **Premise contradicted — it is additive.** `specifyConventionalHandlerDiscovery()` runs inside `FindCalls()` at bootstrap, *after* user customization (`HandlerDiscovery.cs:87-95, 205`), and `CompositeFilter.Matches` OR's the includes. **Name the consumer `DriverLocationUpdatedHandler`; no discovery change, no rename to `*Automation`.** Requirements that still bind: public, concrete, closed-generic, method named `Handle`/`Handles`/`Consume`/`Consumes`. |
| 5 | How does a test deterministically wait for a Kafka message to be consumed? | **Premise partly contradicted.** `IncludeExternalTransports()` exists but governs *outgoing* tracking, not arrivals. The waiter is **`WaitForMessageToBeReceivedAt<T>(IHost)`** (`TrackedSessionConfiguration.cs:183`). For a test that produces with a raw `ProducerBuilder`, drop `IncludeExternalTransports()` entirely — pattern at `Wolverine.Kafka.Tests/publish_and_receive_raw_json.cs:87-98`. Note `TrackedSession.IsCompleted()` short-circuits on the first satisfied condition, so you cannot assert "nothing else happened" from the same session. |
| 6 | Marten indexing + `Contains` translation for the k-ring query. | `Duplicate(...)` (real column + index) and `Index(...)` (computed JSONB index) are **different APIs, not synonyms** — use `Duplicate`. `.Where(d => cells.Contains(d.H3Cell))` translates to **`= ANY(:param)`** — a *single array parameter*, not an N-term `IN` list (`IsOneOf.cs:65-71`). Enums are special-cased and also translate. `Duplicate` calls chain safely (`Alter`'s setter appends). |
| 7 | Is read-then-conditionally-`Store` the best LWW upsert available? | **Premise contradicted — no.** `session.TryUpdateRevision(doc, revision)` (`IDocumentOperations.cs:125`) is an **atomic single-statement upsert** whose `where … mt_version < ?` guard makes a stale write a silent no-op, and it handles the document-absent case. **Two traps:** (a) the guard silently does nothing unless the document is registered `UseNumericRevisions(true)`; (b) `mt_version` is a monotonic `long`, **not a timestamp** — project `serverReceivedAt` to unix-ms yourself, and note revision `0` means "always win". |
| 8 | pocketken.H3 4.5.0.1 k-ring surface (probed by reflection, not a JasperFx gate). | `H3.Algorithms.Rings.GetKRing(H3Index origin, int k)` → `IEnumerable<H3Index>`. `H3Index.GetHexagonEdgeLengthAverageInM(int resolution)` → `double`, static. `new H3Index(string)` parses a cell id back. `LatLng.FromCoordinate(Coordinate)` + `GetGreatCircleDistanceInMeters(LatLng)` for the exact-distance filter. **The `new LatLng(lat, lon)` ctor takes RADIANS** — same footgun `H3CellIndexer` documents, so go through `FromCoordinate(new Coordinate(lon, lat))` and stay degrees-native. |

**Gate 2 + gate 4 interact, and the interaction is the sharpest edge in this session.** `HandlerPipeline` resolves the wire name through `HandlerGraph._messageTypes`, which is populated **only from discovered handler chains** (`HandlerGraph.cs:530`). If the handler is not discovered, the message is never deserialized at all — it short-circuits to `NoHandlerContinuation` and the failure looks like silence, not an error. **Handler discovery is a precondition for deserialization, not just for dispatch.** A test that sees no document and no exception should suspect discovery first.

---

## Skill corrections (`critter-skill-auditor` Phase 1, 2026-07-24)

The Phase 1 pass corrected this prompt after the source-verification pass had already closed all eight gates — the second consecutive session where convention discovery caught what source verification structurally could not. Applied above; recorded here so the reasoning survives.

- **`wolverine-marten-automation` does not govern this session, and naming it was a category error.** Its own scope statement (`SKILL.md:12-22`) restricts it to handlers reacting to a domain event *forwarded from a Marten stream*. Neither new handler is that shape — nothing in `AvailableDrivers/` touches `UseFastEventForwarding`. `wolverine-kafka` says so itself at line 14: *"Kafka is a transport wire, not a handler shape… a vanilla Wolverine messaging handler,"* and its § Prerequisites defers to `wolverine-messaging-handlers`. **Replaced with `wolverine-handlers` (base shape) + `wolverine-messaging-handlers` (inbound-message shape and idempotency).**
- **`marten-querying` was missing.** `NearbyAvailableDriversView` is a read-side query adapter, squarely its charter. It also settles a mechanical point: **inject `IQuerySession` in the view** (pure read) and `IDocumentSession` in the two handlers (they write).
- **`protobuf-contracts:418` names an anti-pattern whose *literal wording* this session's csproj wiring resembles** — "defining a shared type inside a service's package" and importing it cross-BC. This is **not** that violation: a producer's published *event contract* is a different relationship from an arbitrarily borrowed *shared value type*, and gate 3 establishes that the mechanics hold. But the skill's text does not draw that distinction anywhere, so **the csproj comment must name the tension explicitly** rather than leave a future reader to hit it cold.
- **Handler naming is confirmed on ubiquitous-language grounds, not just mechanical ones.** W001 §5.2 pins "Automation" to handlers reacting to a domain event already on a Marten stream. A Kafka-triggered handler is not that shape in Cab's vocabulary, so `*Handler` is correct — matching `csharp-coding-standards:710`.
- **`AvailableDrivers/` as a new folder is sanctioned**, grounded in ADR-018:36 ("a Dispatch-owned local projection") and `vertical-slice-organization`'s "what does this folder do?" test: maintaining the view and deciding candidates are different capabilities.

---

## Decisions resolved (user sign-off 2026-07-24)

### Fork 1 — the availability half: exclude, with an unbound handler

W006 §6.5's `AvailableDriver` joins a Kafka location side with an ASB availability side, but ASB is a forward-constraint to an un-workshopped Driver Profile BC — and `CandidateSelectionAutomation` filters on `VehicleClass`, which lives on the availability side.

**Resolved:** the Kafka handler writes the **location side only**. A `DriverAvailabilityChangedHandler` ships with **no transport bound to it** — tests drive it in-process via `IMessageBus`. **A driver with a location but no availability side is excluded from selection.**

*Why:* you cannot dispatch to a driver whose capability you do not know. Optimistic defaults (`Available` + `STANDARD`) would keep a demo alive by fabricating a capability claim Dispatch has no source for — and fabricating it *invisibly*, at the point of query. The accepted cost is that a real end-to-end run yields `NoCandidatesAvailable` until Driver Profile ships. **That is honest rather than broken:** ADR-018 says the view needs both feeders, and with one feeder it correctly reports that it cannot dispatch. The cross-service Kafka flow is still real and observable in the document store, which is what this PR exists to prove.

**Scope guard on the placeholder message.** `DriverAvailabilityChanged` is Dispatch-local and carries exactly the four availability-side fields W006 §6.5 already locks (`driverId`, `availabilityState`, `vehicleClass`, `availabilityUpdatedAt`) — **no more**. It is not a proto, it is not a published contract, and it does **not** pre-empt Driver Profile's workshop, which will dictate the real shape (W001 §5.3 anticipates four distinct events: `DriverCameOnline` / `WentOnBreak` / `WentOffline` / `VehicleChanged`). Mirroring §6.5's document columns 1:1 is the boundary; inventing transitions beyond them is not.

### Fork 2 — H3 query resolution: track last-received

Dispatch must compute the pickup's cell at the **same resolution Telemetry used**, and that resolution is a `TelemetryPolicy` value Dispatch does not own.

**Resolved:** store `h3Resolution` on each `AvailableDriver` document and derive the query resolution from the **most recently ingested** document. If the store is empty, short-circuit to an empty result rather than guessing a resolution.

*Why:* the alternative — a Dispatch-side constant that must match Telemetry's — is a silent cross-service coupling with no enforcement, where a Telemetry policy change breaks Dispatch's queries with no error, just an empty candidate set that reads as "no drivers nearby." Deriving it from the stream keeps the resolution flowing with the data that depends on it. A `TelemetryPolicy` roll produces a brief window where mixed-resolution documents are missed, **self-healed within one heartbeat — the same accepted-v1-staleness argument W006 §6.3 and §6.4 already make** for dropped publishes and for eviction. Union-across-resolutions was rejected as more code and an extra hot-path query to close a window the spec elsewhere accepts.

**Note the ambiguity trap:** resolution `0` is a *valid* H3 resolution, so a `FirstOrDefault()` returning `0` cannot be distinguished from "no documents." Branch on document existence, not on the resolution value.

### Fork 3 — `EtaSeconds`: derived from distance, invented constant named as such

`INearbyAvailableDriversSource` returns `NearbyDriver(DriverId, DistanceMeters, EtaSeconds, VehicleClass)`, and a location+availability view has no ETA source. W001 §5.3 locks match-score as inverse straight-line distance for v1 and defers road-network ETA to a future gRPC counterparty.

**Resolved:** `EtaSeconds = distanceMeters / assumedUrbanSpeed`, with the speed as a **single named constant documented as invented at implementation time** — the same honest treatment PR #45 gave its 100 m accuracy threshold.

*Why:* a `0` sentinel is a lie of a different kind (W001 §5.4's offer broadcast would show riders a 0-second ETA with nothing marking it absent rather than instant), and making the field nullable would touch `CandidatesSelected`'s locked event table — a spec amendment beyond this slice, which already carries two. The constant keeps the value meaningful and keeps the seam ready for a real ETA service to replace one method.

---

## Deliverable plan

### 1. Project wiring

- **`Directory.Packages.props`** — expected **unchanged**; every needed `PackageVersion` (`WolverineFx.Kafka`, `WolverineFx.Protobuf`, `Google.Protobuf`, `Grpc.Tools`, `pocketken.H3`, `Confluent.Kafka`) already exists from the Telemetry chain. Verify rather than assume.
- **`src/CritterCab.Dispatch/CritterCab.Dispatch.csproj`** — add `WolverineFx.Kafka`, `WolverineFx.Protobuf`, `Google.Protobuf`, `Grpc.Tools`, `pocketken.H3`, and a `<Protobuf Include="..\..\protos\crittercab\telemetry\v1\driver_location_updated.proto" ProtoRoot="..\..\protos" GrpcServices="None" />`. **Comment why this is not a shared assembly**: both services compile the same contract file independently, which is exactly what ADR-009 means by the proto being the artifact of record, and gate 3 is why Wolverine tolerates the two copies. **The same comment must address `protobuf-contracts:418`** — a producer's published event contract subscribed to by a consumer is a different relationship from a shared value type borrowed across packages, and the skill does not currently distinguish them.

### 2. New slice folder — `src/CritterCab.Dispatch/AvailableDrivers/`

A new folder rather than an addition to `CandidateSelection/`: this is W006 slice 5 with its own inbound transport, while `CandidateSelection/` is the W001 slice 5.3 that *consumes* it. The port (`INearbyAvailableDriversSource`) stays with its consumer; this folder supplies the adapter.

- **`AvailableDriver.cs`** — the per-driver document. Location side (`H3Cell`, `Lat`, `Lon`, `H3Resolution`, `ServerReceivedAt`) marked **`required`**; availability side (`AvailabilityState`, `VehicleClass`, `AvailabilityUpdatedAt`) plainly **nullable, not `required`** — absence is meaningful per fork 1, which is exactly the "genuinely optional by business logic" case `csharp-coding-standards:461-463` sanctions. Plus `Version` for numeric revisions.
- **`DriverLocationUpdatedHandler.cs`** — the Kafka consumer, taking `IDocumentSession`. Name fixed by gate 4. Reads the generated protobuf message, projects `ServerReceivedAt` to unix-ms, calls `TryUpdateRevision`. **Must not overwrite the availability side** — read-modify-write or a targeted update, not a whole-document replace.
- **`DriverAvailabilityChanged.cs` + `DriverAvailabilityChangedHandler.cs`** — the unbound ASB-half landing site, scoped per fork 1. `sealed record` with `required` properties per `csharp-coding-standards:116` (the DTO shape rule is project-wide, not event-sourcing-scoped). Mirror-image constraint: **must not overwrite the location side.**
- **`NearbyAvailableDriversView.cs`** — implements `INearbyAvailableDriversSource`, taking **`IQuerySession`** (pure read). Resolves the query resolution (fork 2), computes the pickup cell, derives `k` from `searchRadiusMeters`, queries by `= ANY` over the k-ring cells filtered to available + capable, applies the exact-distance filter, derives ETA (fork 3), returns `NearbyDriver` records.
- **`H3KRing.cs`** — wraps the k-derivation, the ring enumeration, and the great-circle distance. **Wrap the footguns the way `H3CellIndexer` does, and say why in a comment**: the degrees-vs-radians and lat/lon-vs-x/y hazards are identical here. Derive `k` as `ceil(radiusMeters / (edgeLengthM × √3))` and **round up generously** — the exact-distance filter runs afterward, so over-covering costs only a longer array while under-covering silently drops drivers.

### 3. `src/CritterCab.Dispatch/Program.cs`

- Marten: register `AvailableDriver` with `Duplicate(x => x.H3Cell)`, `Duplicate(x => x.VehicleClass)`, `Duplicate(x => x.AvailabilityState)` and **`UseNumericRevisions(true)`** (gate 7 trap (a) — without it `TryUpdateRevision` degrades silently to an unguarded upsert).
- Kafka: `UseKafkaUsingNamedConnection("kafka")` + `ListenToKafkaTopic("telemetry.driver-location-updated").ConfigureConsumer(c => c.GroupId = ...).UseProtobufSerialization().DefaultIncomingMessage<DriverLocationUpdated>()`. **Guard on the connection string** exactly the way Telemetry's `kafkaEnabled` flag does — one flag read once, used by both registration and transport wiring, so the two cannot drift. Remember `ConfigureConsumer` **replaces** the parent `ConsumerConfig`: bootstrap servers are inherited, other settings are not.
- Swap `INearbyAvailableDriversSource`: register `NearbyAvailableDriversView` when Marten is configured, `NearbyAvailableDriversStub` when it is not. **The stub is demoted to a test double, not deleted** — existing slice 5.2/5.3 suites override the registration through the fixture and must keep passing untouched.
- **`opts.Durability.UseSyncRetryBlock` is Telemetry-only and must not be copied here.** It exists to make §6.3's publish-first ordering real on the *producer*; a listener does not inherit it and does not need it.

### 4. `apphost.cs`

Add `.WithReference(kafka).WaitFor(kafka)` to the Dispatch resource. Do **not** add a second `AddKafka` — the broker exists at host port 5392. **Run `dotnet build apphost.cs` by hand**; CI cannot see this file, and it has broken silently twice.

### 5. Tests — `tests/CritterCab.Dispatch.Tests/AvailableDrivers/`

A **new fixture with its own Kafka Testcontainer**, mirroring Telemetry's split so the existing Dispatch suites never wait on a broker. Cover:

- **Location upsert** (W006 §6.5 GWT 1) — a published `DriverLocationUpdated` lands as an `AvailableDriver` location side.
- **Dedup** (W006 §6.3 GWT 2) — **the GWT PR C could not test.** Redeliver the same `(driverId, serverReceivedAt)`; assert the projection applies it at most once. Then send a *stale* update and assert it is silently discarded, which is the redelivery-after-rebalance case that motivates the revision guard.
- **Availability** (§6.5 GWT 2) — the unbound handler invoked in-process sets the availability side without disturbing the location side.
- **Selection read** (§6.5 GWT 3) — k-ring + exact-distance + vehicle-class filtering returns the right drivers in the right order.
- **Fork-1 exclusion** — a driver with a location but no availability side is not selected.
- **`H3KRing` unit tests** — pinning tests in the spirit of `H3CellIndexerTests`, locking the axis order, the units, and the k-derivation at a known resolution.

Use `WaitForMessageToBeReceivedAt<DriverLocationUpdated>(host)` per gate 5. **No `Thread.Sleep`.**

### 6. Documentation

- **`docs/workshops/006-telemetry-event-model.md`** — §6.5 Document History entry: slice 5 realized; the three fork resolutions recorded as **amendments** (§6.5 was silent, not wrong); the slice walk closes.
- **`docs/workshops/001-dispatch-event-model.md`** — §5.3 realization note under the existing 2026-06-30 amendment: the transport now runs, the view exists, the stub is out of the production graph.
- **`docs/skills/wolverine-kafka/SKILL.md`** — **rewrite § Listening, § Consumer groups, § Batch processing and § DLQ from this session's shipped code.** This is the deferred DEBT row, and PR D is the session that can finally do it honestly: the examples were left wrong in PR C because replacing speculative names with *different* speculative names would have been no better. Fold in the gate 1/2/4/5 findings — especially that discovery is a precondition for deserialization.
- **`docs/skills/DEBT.md`** — close that row; register anything new this session surfaces.
- **`docs/prompts/README.md`** — index entry.
- **Retro** at `docs/retrospectives/implementations/009-...`, shipping inside this PR.

---

## Out of scope

- **The ASB half of ADR-018.** No Azure Service Bus transport, no Driver Profile modeling, no availability events beyond the four fields fork 1 permits.
- **`CandidateSelectionAutomation.cs`** — must not appear in the diff.
- **Deleting `NearbyAvailableDriversStub`** — it is demoted, not removed.
- **The three decision-class DEBT rows** (test-class naming, `testing-integration` Gap B, `identity-acl` streaming exception). Each needs a call the user has not made; a routine tidy must not drain them.
- **The CI-cannot-build-`apphost.cs` gap.** Extending the "Verify solution completeness" step is its own session — CI changes carry their own blast radius.
- **`transport-selection`'s built-vs-modeled status axis** — a separate DEBT row.
- **CritterWatch.** It becomes genuinely useful *after* this PR creates real cross-service traffic, but it needs RabbitMQ and its trial licence expired 2026-07-10. Revisit as its own session.
- **Road-network ETA, the v2 staleness ceiling, adaptive radius widening (W001 slice 9), `DispatchPolicyConfigured` (slice 11).**

---

## Follow-on PR sequence (arc context; not this session)

**PR D closes W006.** All five slices realized; two of ADR-005's three transports live; the first cross-service flow running.

**The next session must be a design-or-tidy return.** Per ADR-004's design-return cadence, PR D is the fourth consecutive implementation PR in this chain — ADR-019 served as the interleave for PR C, and nothing covers PR D. Candidates, in rough order of pull: the **Driver Profile workshop** (which the ASB half now blocks on, and which fork 1 deliberately declined to pre-empt), a **`tidy: skills` session** draining the accumulated DEBT rows including the three that need decisions, or **CritterWatch** now that there is real traffic for it to render.

---

## Document history

- **2026-07-24** — Authored. Eight Verify-before-wiring gates closed at authoring time (second consecutive prompt); three gate premises contradicted by source (listener message-type inference, handler-discovery replacement, read-then-`Store` as the best LWW upsert). Three durable forks resolved by user sign-off: availability-half exclusion with an unbound handler, last-received H3 resolution tracking, distance-derived ETA with a named invented constant.
- **2026-07-24** — `critter-skill-auditor` Phase 1 corrections applied (see § Skill corrections): `wolverine-marten-automation` removed as a category error and replaced with `wolverine-handlers` + `wolverine-messaging-handlers`; `marten-querying` added along with the `IQuerySession`/`IDocumentSession` split; the `protobuf-contracts:418` wording tension made an explicit deliverable; `LastKnownPosition/` added as orientation reading. **Carry to the retro:** the existing DEBT row for "no skill covers a non-event-sourced document write path inside an event-sourced service" gets its second and sharper instance here — Telemetry is stream-processing throughout, whereas Dispatch is the project's canonical event-sourced BC now carrying a plain-document LWW view alongside its aggregates. Also note `DriverAvailabilityChanged` is a genuine hybrid with no governing skill (event-shaped, no stream, no transport) — a skill-authoring candidate if the pattern recurs when Driver Profile's real events land.
