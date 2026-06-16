# Prompt 005 — Implement Slice 5.3: CandidatesSelected

| Field | Value |
|---|---|
| **Status** | Complete |
| **Authored** | 2026-06-16 |
| **Target artifacts** | `src/CritterCab.Dispatch/CandidateSelection/` (new directory: `CandidatesSelected.cs`, `NoCandidatesAvailable.cs`, `ICandidateSelectionOutcome.cs`, `INearbyAvailableDriversSource.cs`, `NearbyAvailableDriversStub.cs`, `CandidateSelectionAutomation.cs`, `DispatchPolicySnapshot.cs`, `RequestRoundsProjection.cs`); `src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs` (fold `CandidatesSelected` + `NoCandidatesAvailable`); `src/CritterCab.Dispatch/Program.cs` (register new event types + projections + stubs); `tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs` (add `INearbyAvailableDriversSource` override + forwarding wrapper); `tests/CritterCab.Dispatch.Tests/CandidateSelection/Slice53CandidatesSelectedTests.cs` (three Alba tests, one per GWT); `docs/narratives/002-driver-accepts-a-ride.md` (`## Document History` v0.2 entry); `docs/workshops/001-dispatch-event-model.md` (`## Document History` v0.7 entry); this prompt; `docs/prompts/README.md` (index entry); `docs/retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md` (retro); `docs/retrospectives/README.md` (index entry) |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §5.3 (three GWTs + event shapes + decisions locked); [`docs/narratives/002-driver-accepts-a-ride.md`](../../narratives/002-driver-accepts-a-ride.md) §Setting and §Moment 1 (driver-side journey whose upstream machinery this session implements); [`docs/prompts/implementations/004-dispatch-slice-5-2-fare-quoted-failure-paths.md`](./004-dispatch-slice-5-2-fare-quoted-failure-paths.md) + its [retro](../../retrospectives/implementations/004-dispatch-slice-5-2-fare-quoted-failure-paths.md) (direct precedent — code patterns this session mirrors) |
| **Workflow position** | Fourth vertical-slice implementation in Dispatch. First slice of the new dispatch-round arc (slices 5.3–5.5). **Third substantive forward exercise of the spec-delta closure-loop convention** — meets ADR-016's 2–3-exercises deferral trigger for a follow-up encoding decision (still deferred; trigger is now met but encoding is not blocking). |

---

## Spec delta

- **`docs/narratives/002-driver-accepts-a-ride.md` `## Document History` gains a v0.2 entry**: Moment 1's "upstream of his vantage" machinery — `CandidatesSelected` and the two `NoCandidatesAvailable` paths — now has runnable Alba coverage. Slice 5.3 is not in narrative 002's `slices_implemented` list (narrative 002 implements 5.4, 5.5, and 5.10); the v0.2 entry records that the upstream precondition slice Moment 1 references has implemented GWT coverage, and that no narrative-prose change is needed — this note belongs to the authorial-call layer, not the prose layer.
- **`docs/workshops/001-dispatch-event-model.md` `## Document History` gains a v0.7 entry**: All three W001 §5.3 GWTs (happy path + no drivers in range + vehicle-class gap) have runnable Alba coverage. Entry names: `CandidatesSelected` and `NoCandidatesAvailable` event shapes, `INearbyAvailableDriversSource` stub seam, `DispatchPolicySnapshot` DI record (hardcoded defaults; Slice 11 swaps to event-sourced `DispatchPolicy` view), `RequestRoundsProjection` inline projection, and `[WriteAggregate]` binding on `FareQuoted` (second event on stream — first use of this pattern in the codebase).
- **No proto artifact.** Both `CandidatesSelected` and `NoCandidatesAvailable` are Dispatch-local — they feed Slice 4 (offer broadcast) and Slice 9 (re-dispatch) intra-BC. No `/protos/` changes.

---

## Framing

Slices 5.1 (`RideRequested`) and 5.2 (`FareQuoted`, both paths) are the only implemented Dispatch slices. This session implements slice 5.3 — the moment where `CandidateSelectionAutomation` reacts to `FareQuoted`, queries the (stubbed) `NearbyAvailableDrivers` view, and records the selection decision locally as a Klefter event. Unlike the previous two slices, this automation makes no external call: it is a pure in-memory computation over view data. The boundary abstraction (`INearbyAvailableDriversSource`) stubs out the Telemetry + Driver Profile translation-in whose transport is deferred to parking-lot #4.

The design-return interleave specified in prompt 004's retro was satisfied by ADR-016 (frontend architecture, PR #33) and five other non-implementation PRs since PR #28. No further design-return obligation is carried into this session.

---

## Goal

Implement W001 §5.3 end-to-end: `CandidateSelectionAutomation` reacts to `FareQuoted`, queries `INearbyAvailableDriversSource`, and emits either `CandidatesSelected` (≥1 eligible candidate) or `NoCandidatesAvailable` (0 eligible candidates), with `RequestRoundsProjection` tracking the round result and `RequestTimeline` extended to summarize both outcomes. Three Alba integration tests, one per GWT.

---

## Orientation files (read in order)

1. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §5.3** — authoritative source for the three GWTs, both event shapes, the views consumed and fed, the decisions locked (match-score algorithm, empty-candidate-set treatment, driver concurrency tolerance, vehicle capability model, transport deferral).
2. **[`docs/narratives/002-driver-accepts-a-ride.md`](../../narratives/002-driver-accepts-a-ride.md) §Setting and §Moment 1** — driver-side perspective; §Moment 1's "upstream of his vantage" paragraph names `CandidatesSelected` as one of the three committed events Dani cannot see but which exist before his offer arrives.
3. **[Prompt 004](./004-dispatch-slice-5-2-fare-quoted-failure-paths.md) + [its retro](../../retrospectives/implementations/004-dispatch-slice-5-2-fare-quoted-failure-paths.md)** — direct precedent. The `IFareQuoteOutcome` marker interface, `FareQuoteRetryPolicy` DI record, `ForwardingPricingClient` indirection, per-test `IDisposable` teardown, and the `[WriteAggregate]` automation shape all carry forward structurally. The retro's `ForwardingPricingClient` note, the `IFareQuoteOutcome` marker note, and the xUnit fixture-mutable-state discipline are especially load-bearing for this session.
4. **`src/CritterCab.Dispatch/FareQuoting/FareQuoteAutomation.cs`** — canonical automation shape this session mirrors.
5. **`tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs`** — fixture extension point this session extends with a `NearbyAvailableDriversSource` override.
6. **`src/CritterCab.Dispatch/Program.cs`** — registration target for new event types, projection, and stubs.

---

## Design decisions committed by this prompt

Four implementation-mechanism choices are made up-front to keep the session focused on the slice's spec rather than re-deriving design:

### 1. Boundary stub — `INearbyAvailableDriversSource` interface with canned `NearbyAvailableDriversStub`

```csharp
public interface INearbyAvailableDriversSource
{
    Task<IReadOnlyList<NearbyDriver>> GetDriversAsync(
        Location pickup,
        int searchRadiusMeters,
        VehicleClass vehicleClassRequired,
        CancellationToken ct = default);
}

public sealed record NearbyDriver(
    Guid DriverId,
    int DistanceMeters,
    int EtaSeconds,
    VehicleClass VehicleClass);

public sealed class NearbyAvailableDriversStub : INearbyAvailableDriversSource
{
    public IReadOnlyList<NearbyDriver> Drivers { get; set; } = [];

    public Task<IReadOnlyList<NearbyDriver>> GetDriversAsync(
        Location pickup,
        int searchRadiusMeters,
        VehicleClass vehicleClassRequired,
        CancellationToken ct = default) =>
        Task.FromResult(Drivers);
}
```

Rationale: mirrors the `IPricingClient` / `PricingClientStub` seam established in Slice 5.2. The transport decision for this view (Kafka-fed local projection vs. gRPC remote query) is parking-lot #4 in W001 — the interface is the correct seam to introduce without committing to either. The stub ignores `searchRadiusMeters` and `vehicleClassRequired` — filtering is done by the automation from the returned set, matching how the real view would behave (it returns only eligible candidates already filtered by those parameters). **Note to implementer:** when the real `INearbyAvailableDriversSource` lands, filtering may move to the query; the stub's contract should not be misread as the interface's filtering responsibility.

### 2. `DispatchPolicySnapshot` DI record with hardcoded defaults

```csharp
public sealed record DispatchPolicySnapshot(
    int SearchRadiusMeters,
    int MaxCandidatesPerRound,
    string PolicyVersion)
{
    public static readonly DispatchPolicySnapshot Default =
        new(SearchRadiusMeters: 5000, MaxCandidatesPerRound: 5, PolicyVersion: "default-v1");
}
```

Rationale: mirrors `FareQuoteRetryPolicy.Default` established in Slice 5.2. The name `DispatchPolicySnapshot` reserves `DispatchPolicy` for the eventual Marten inline projection fed by `DispatchPolicyConfigured` events in Slice 11. Slice 11 will swap `services.AddSingleton(DispatchPolicySnapshot.Default)` for a projection-sourced value — the DI seam is the same; only the population mechanism changes. The `PolicyVersion` field is needed on every `CandidatesSelected` event for Klefter auditability; "default-v1" is the hardcoded sentinel until Slice 11.

### 3. `ICandidateSelectionOutcome` marker interface as handler return type

```csharp
public interface ICandidateSelectionOutcome { }

public sealed record CandidatesSelected(...) : ICandidateSelectionOutcome;
public sealed record NoCandidatesAvailable(...) : ICandidateSelectionOutcome;
```

Rationale: mirrors `IFareQuoteOutcome` — the pattern is now established (per retro 004's skill-file-gap note). The compiler enforces "one of the two terminal events"; Wolverine inspects the runtime type for stream appending. If the codebase ends up with many automations using marker interfaces for terminal outcomes, the pattern warrants a skill-file entry (flagged in retro 004 but not yet encoded). Use this session's retro to record whether this is the second or third occurrence.

### 4. `[WriteAggregate]` binding on `FareQuoted` (second event on stream)

```csharp
public static class CandidateSelectionAutomation
{
    public static async Task<ICandidateSelectionOutcome> Handle(
        FareQuoted @event,
        [WriteAggregate(nameof(FareQuoted.RideRequestId))] RideRequest rideRequest,
        INearbyAvailableDriversSource nearbyDrivers,
        DispatchPolicySnapshot policy,
        TimeProvider time,
        CancellationToken ct)
    { ... }
}
```

Rationale: `FareQuoteAutomation` used `[WriteAggregate(nameof(RideRequested.RideRequestId))]` on the stream's first event. This automation triggers on `FareQuoted`, the stream's second event, but the aggregate stream ID is the same field (`FareQuoted.RideRequestId`). The attribute tells Wolverine to load the `RideRequest` aggregate for that ID and allow appending new events. **This is the first time `[WriteAggregate]` is bound to a non-first stream event in CritterCab.** Before coding, verify with the jasperfx-source-verifier that `[WriteAggregate]` on a forwarded event that is not the stream-opening event works as expected in Wolverine 5.39+. If it does not, the fallback is to inject `IQuerySession` and load `RideRequest` manually (read-only) while appending via Wolverine's `[Marten]` / cascading-events approach. Either way, the `rideRequest.Pickup` value is needed to build the `NearbyAvailableDriversSource` query.

---

## Automation logic (pseudocode intent, not implementation spec)

The automation receives `FareQuoted` and needs:
1. `rideRequest.Pickup` — to pass as the center point for the nearby-drivers query.
2. `@event.VehicleClass` — to filter by capability (or let the stub filter).
3. Policy values for `searchRadiusMeters` and `maxCandidatesPerRound`.

**Happy path (≥1 driver eligible):**
- Call `nearbyDrivers.GetDriversAsync(rideRequest.Pickup, policy.SearchRadiusMeters, @event.VehicleClass, ct)`.
- Sort results ascending by `DistanceMeters` (= descending match score per the inverse-distance algorithm locked in W001 §5.3).
- Take up to `policy.MaxCandidatesPerRound` candidates.
- Compute `MatchScore` as `1.0 / DistanceMeters` (floating point; informational for broadcast; avoid division-by-zero if distance is 0 by flooring at 1m).
- Emit `CandidatesSelected`.

**Empty-set path (0 drivers eligible after vehicle-class filter):**
- Determine `reason`:
  - If `GetDriversAsync` with `vehicleClassRequired` returns 0 but calling without the filter (or checking the unfiltered set) returns ≥1 → `NO_CAPABLE_DRIVERS_IN_RANGE`.
  - If 0 regardless of vehicle class → `NO_DRIVERS_IN_RANGE`.
  - `ALL_CAPABLE_DRIVERS_OCCUPIED` and `OTHER` are reserved for future use; the stub does not exercise them.
- Emit `NoCandidatesAvailable`.

**Note:** the `vehicleClassRequired` filter in W001 §5.3's GWT 3 (ACCESSIBLE scarcity) distinguishes between "no drivers at all" and "drivers exist but wrong class." The stub must support returning drivers with a specific `VehicleClass` to allow the automation to make this distinction. The automation is responsible for the split logic, not the stub.

For the v1 implementation, the split logic is: call the stub once (unfiltered), check total count vs. class-filtered count to determine the right reason enum. This keeps the interface contract clean (one call, return all eligible drivers in range; filtering by vehicle class happens in-process).

---

## Event shapes (from W001 §5.3)

### `CandidatesSelected`

```csharp
public sealed record CandidatesSelected(
    Guid RideRequestId,
    int RoundNumber,
    IReadOnlyList<CandidateEntry> Candidates,
    SearchParameters SearchParameters,
    string DispatchPolicyVersion,
    DateTimeOffset SelectedAt) : ICandidateSelectionOutcome;

public sealed record CandidateEntry(
    Guid DriverId,
    double MatchScore,
    int DistanceMeters,
    int EtaSeconds);

public sealed record SearchParameters(
    int SearchRadiusMeters,
    int MaxCandidatesPerRound,
    VehicleClass VehicleClassRequired);
```

### `NoCandidatesAvailable`

```csharp
public sealed record NoCandidatesAvailable(
    Guid RideRequestId,
    int RoundNumber,
    SearchParameters SearchParameters,
    NoCandidatesReason Reason,
    DateTimeOffset SelectedAt) : ICandidateSelectionOutcome;

public enum NoCandidatesReason
{
    NoDriversInRange,
    NoCapableDriversInRange,
    AllCapableDriversOccupied,
    Other
}
```

Use `required` keyword on `record` properties per project convention. The enum values above use PascalCase; the `JsonStringEnumConverter` already registered in `Program.cs` will serialize them as strings on the wire.

---

## `RequestRoundsProjection`

New inline `SingleStreamProjection` tracking per-request dispatch rounds. Consumed by Slice 9 (re-dispatch / abandonment) and ops dashboards.

```csharp
public class RequestRounds
{
    public Guid Id { get; init; }
    public List<RoundEntry> Rounds { get; init; } = [];
}

public sealed record RoundEntry(
    int RoundNumber,
    string Outcome,          // "CandidatesSelected" | "NoCandidatesAvailable"
    string? NoCandidatesReason,
    DateTimeOffset OccurredAt);

public partial class RequestRoundsProjection : SingleStreamProjection<RequestRounds, Guid>
{
    public RequestRounds Create(IEvent<RideRequested> @event) =>
        new() { Id = @event.StreamId };

    public void Apply(IEvent<CandidatesSelected> @event, RequestRounds view) =>
        view.Rounds.Add(new RoundEntry(
            @event.Data.RoundNumber,
            nameof(CandidatesSelected),
            null,
            @event.Data.SelectedAt));

    public void Apply(IEvent<NoCandidatesAvailable> @event, RequestRounds view) =>
        view.Rounds.Add(new RoundEntry(
            @event.Data.RoundNumber,
            nameof(NoCandidatesAvailable),
            @event.Data.Reason.ToString(),
            @event.Data.SelectedAt));
}
```

The projection must be declared `partial` per the Marten 9 source-gen requirement (established in PR #32 for `RequestTimelineProjection` and `FareQuoteAttemptsProjection`).

---

## GWT tests

**GWT 1 — Happy path: ≥1 eligible candidate, capped to policy maximum**
```
Given: RideRequested { rideRequestId: X, vehicleClass: STANDARD, pickup: P, ... }
  And: FareQuoted { rideRequestId: X, vehicleClass: STANDARD, ... }
  And: NearbyAvailableDriversStub.Drivers = 6 STANDARD-capable drivers at distances [300, 500, 800, 1200, 1500, 2000]m
  And: DispatchPolicySnapshot { searchRadiusMeters: 5000, maxCandidatesPerRound: 5 }
When: CandidateSelectionAutomation reacts to FareQuoted
Then: CandidatesSelected { rideRequestId: X, roundNumber: 1,
                           candidates: [top 5 by matchScore — i.e., 5 closest],
                           searchParameters, dispatchPolicyVersion: "default-v1", selectedAt } emitted
  And: candidates are ordered by matchScore desc (closest driver first)
  And: the 6th driver (2000m) is excluded
  And: RequestRounds.Rounds has one entry with Outcome = "CandidatesSelected"
  And: RequestTimeline has a summary entry for CandidatesSelected
```

**GWT 2 — No drivers in range**
```
Given: RideRequested + FareQuoted for X (vehicleClass: STANDARD)
  And: NearbyAvailableDriversStub.Drivers = [] (empty)
When: CandidateSelectionAutomation reacts to FareQuoted
Then: NoCandidatesAvailable { rideRequestId: X, roundNumber: 1,
                               reason: NoDriversInRange, selectedAt } emitted
  And: RequestRounds.Rounds has one entry with Outcome = "NoCandidatesAvailable", NoCandidatesReason = "NoDriversInRange"
  And: RequestTimeline has a summary entry for NoCandidatesAvailable
```

**GWT 3 — Vehicle-class gap (ACCESSIBLE scarcity)**
```
Given: RideRequested + FareQuoted for X (vehicleClass: ACCESSIBLE)
  And: NearbyAvailableDriversStub.Drivers = 8 STANDARD-class drivers within range, 0 ACCESSIBLE
When: CandidateSelectionAutomation reacts to FareQuoted
Then: NoCandidatesAvailable { rideRequestId: X, roundNumber: 1,
                               reason: NoCapableDriversInRange, selectedAt } emitted
  And: RequestRounds.Rounds has one entry with Outcome = "NoCandidatesAvailable", NoCandidatesReason = "NoCapableDriversInRange"
```

---

## Working pattern

**One Alba test per GWT, RED → GREEN → REFACTOR.** Same shape as sessions 003 and 004: submit a ride request via the `POST /dispatch/ride-requests` endpoint, await `ExecuteAndWaitAsync` for the full chain (slice 5.1 `SubmitRideRequest` → `RideRequested` → `FareQuoteAutomation` → `FareQuoted` → `CandidateSelectionAutomation` → `CandidatesSelected` or `NoCandidatesAvailable`), assert on stream tail + `RequestRounds` + `RequestTimeline`. The stub's `Drivers` property is the sole arrange lever across the three tests.

**Fixture extension.** Add to `DispatchTestFixture`:
```csharp
public INearbyAvailableDriversSource NearbyDriversSource { get; set; } = new NearbyAvailableDriversStub();
```
And in `InitializeAsync`'s `ConfigureServices`:
```csharp
services.AddSingleton<INearbyAvailableDriversSource>(
    _ => new ForwardingNearbyDriversSource(() => NearbyDriversSource));
services.AddSingleton(DispatchPolicySnapshot.Default);
```
The `ForwardingNearbyDriversSource` follows the exact same stable-singleton-dispatching-through-fixture-property shape as `ForwardingPricingClient`. Per retro 004's xUnit-fixture-mutable-state discipline: GWT 2 and GWT 3 tests that set a non-default `NearbyDriversSource` must restore the default in `IDisposable.Dispose`; GWT 1 (happy path) must reset to a known driver set in its arrange phase.

**Stub setup for GWT 3.** The automation's vehicle-class split logic requires the stub to return drivers of the wrong class. Assign `NearbyAvailableDriversStub.Drivers = 8 STANDARD drivers`; the automation sees the total > 0 but vehicleClass-filtered count == 0 and chooses `NoCapableDriversInRange`. The stub's `GetDriversAsync` signature ignores `vehicleClassRequired` and returns all entries — the automation applies the filter in-process.

**Commit cadence** (per the thrice-confirmed bundling pattern):
- This prompt + `docs/prompts/README.md` index entry → one bundled commit (land before implementation begins).
- Per-file commits during implementation: events, automation, projections, test fixture extension, tests, spec amendments (narrative 002 + W001 bundled), retro + retros/README index bundled.

**Commit subjects:**
- `feat(dispatch): slice 5.3 CandidatesSelected + NoCandidatesAvailable events`
- `feat(dispatch): slice 5.3 INearbyAvailableDriversSource stub + DispatchPolicySnapshot`
- `feat(dispatch): slice 5.3 CandidateSelectionAutomation + RequestRoundsProjection`
- `test(dispatch): slice 5.3 Alba integration tests (3 GWTs)`
- `docs(specs): slice 5.3 completion spec amendments — narrative 002 + W001 Document History`

---

## Deliverable plan

1. **`src/CritterCab.Dispatch/CandidateSelection/ICandidateSelectionOutcome.cs`** — marker interface.
2. **`src/CritterCab.Dispatch/CandidateSelection/CandidatesSelected.cs`** — domain event + `CandidateEntry` + `SearchParameters` records. All properties `required`.
3. **`src/CritterCab.Dispatch/CandidateSelection/NoCandidatesAvailable.cs`** — domain event + `NoCandidatesReason` enum. All properties `required`.
4. **`src/CritterCab.Dispatch/CandidateSelection/INearbyAvailableDriversSource.cs`** — interface + `NearbyDriver` record + `NearbyAvailableDriversStub` class.
5. **`src/CritterCab.Dispatch/CandidateSelection/DispatchPolicySnapshot.cs`** — DI record with `Default`. Name avoids colliding with eventual Slice 11 `DispatchPolicy` projection.
6. **`src/CritterCab.Dispatch/CandidateSelection/CandidateSelectionAutomation.cs`** — static class, `Handle` method. Returns `ICandidateSelectionOutcome`. Pure computation (no `await` if `GetDriversAsync` is synchronous for the stub, but the interface is `Task`-returning so `async Task<ICandidateSelectionOutcome>` is correct).
7. **`src/CritterCab.Dispatch/CandidateSelection/RequestRoundsProjection.cs`** — `RequestRounds` document + `partial class RequestRoundsProjection : SingleStreamProjection<RequestRounds, Guid>`. Declared `partial` per Marten 9 source-gen requirement.
8. **`src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs`** — add `Apply(IEvent<CandidatesSelected>)` and `Apply(IEvent<NoCandidatesAvailable>)` methods.
9. **`src/CritterCab.Dispatch/Program.cs`**:
   - `opts.Events.AddEventType<CandidatesSelected>()`
   - `opts.Events.AddEventType<NoCandidatesAvailable>()`
   - `opts.Projections.Add(new RequestRoundsProjection(), ProjectionLifecycle.Inline)`
   - `builder.Services.AddSingleton<INearbyAvailableDriversSource, NearbyAvailableDriversStub>()`
   - `builder.Services.AddSingleton(DispatchPolicySnapshot.Default)`
10. **`tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs`** — add `NearbyDriversSource` property + `ForwardingNearbyDriversSource` inner class + register both in `InitializeAsync`.
11. **`tests/CritterCab.Dispatch.Tests/CandidateSelection/Slice53CandidatesSelectedTests.cs`** — three tests (one per GWT). Class implements `IClassFixture<DispatchTestFixture>` + is in `[Collection("Dispatch")]`.
12. **`docs/narratives/002-driver-accepts-a-ride.md`** `## Document History` v0.2 entry (spec-delta closure-loop step 4).
13. **`docs/workshops/001-dispatch-event-model.md`** `## Document History` v0.7 entry (spec-delta closure-loop step 4).
14. **`docs/prompts/implementations/005-dispatch-slice-5-3-candidates-selected.md`** — Status field updated Pending → Complete.
15. **`docs/prompts/README.md`** — index entry.
16. **`docs/retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md`** — retro. Must include `Spec delta — landed?` line confirming the two named amendments (narrative 002 v0.2 + W001 v0.7).
17. **`docs/retrospectives/README.md`** — index entry.

### Definition of done

- Three new tests pass (RED → GREEN → REFACTOR walked for each).
- All prior tests still pass (8 tests: 1 smoke + 3 slice 5.1 + 1 slice 5.2 happy + 3 slice 5.2 failure).
- `RequestRounds` projection assertions in each test confirm the correct `Outcome` and (for GWT 2/3) `NoCandidatesReason`.
- `RequestTimeline` projection assertions confirm a summary entry for `CandidatesSelected` or `NoCandidatesAvailable` as appropriate.
- GWT 1 assertion: exactly 5 candidates returned (not 6), ordered by matchScore desc (= ascending distance).
- `[WriteAggregate]` binding on `FareQuoted` verified to work — or the `IQuerySession` fallback is implemented and flagged in retro.
- Both spec-delta-named Document History entries committed (narrative 002 v0.2 + W001 v0.7).
- Both spec-delta items confirmed in the retro's `Spec delta — landed?` line.
- No Claude attribution on commits or PR per established convention.

---

## Out of scope

- **Slice 11 (`DispatchPolicyConfigured`)** — `DispatchPolicySnapshot` stays hardcoded; Slice 11 swaps it for the `DispatchPolicy` projection fed by `DispatchPolicyConfigured` events.
- **Real `INearbyAvailableDriversSource` implementation** — stub only. The real implementation (Kafka-fed local projection or gRPC remote query) is parking-lot #4 in W001; it lands when transport is decided.
- **Telemetry and Driver Profile translation-in events** (`DriverLocationUpdated`, `DriverCameOnline`, etc.) — not modeled or implemented here. The stub seeds `NearbyDriversSource.Drivers` directly in test arrange.
- **Slice 5.4 (`OfferSent` / fan-out)** — fires on `CandidatesSelected`; not this session's scope. This session lands `CandidatesSelected`; Slice 4 reads it.
- **Slice 9 (re-dispatch/abandonment)** — downstream consumer of `NoCandidatesAvailable`. This session lands the event and the `RequestRoundsProjection` that Slice 9 reads; the automation is not this session's scope.
- **`buf generate` verification gap** — still open from session 003's retro; no proto changes this session.
- **`RequestRounds` multi-round scenarios** — only `roundNumber: 1` is exercised in this session's GWTs. Multiple rounds via Slice 9's widening are Slice 9's test territory.
- **`ALL_CAPABLE_DRIVERS_OCCUPIED` and `Other` reason enum values** — deferred; no GWT exercises them in W001 §5.3.
- **`DriverCapabilities` projection** (mentioned in W001 §5.3 translation-in sources) — its shape is deferred with parking-lot #4.
- **Skill-file encoding** of the `ICandidateSelectionOutcome` marker-interface pattern as a named Wolverine idiom — flagged in retro 004; still unencoded. If this is the second or third occurrence, flag again in this session's retro; encode in a forthcoming tidy session.
- **Workshop §5.2 *Reads* line inconsistency** — unresolved from retro 004; not this session's scope.
- **Bundling-rule encoding** — past the encoding threshold; remains queued for a tidy session.
