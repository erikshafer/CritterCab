# Retrospective — Implement Slice 5.3: CandidatesSelected

## Metadata

- **Triggering prompt:** [`docs/prompts/implementations/005-dispatch-slice-5-3-candidates-selected.md`](../../prompts/implementations/005-dispatch-slice-5-3-candidates-selected.md)
- **Status:** Complete
- **Date authored:** 2026-06-16
- **Output artifacts:**
  - `src/CritterCab.Dispatch/CandidateSelection/ICandidateSelectionOutcome.cs` — marker interface (third instance of the pattern in Dispatch)
  - `src/CritterCab.Dispatch/CandidateSelection/CandidatesSelected.cs` — domain event + `CandidateEntry` + `SearchParameters` records
  - `src/CritterCab.Dispatch/CandidateSelection/NoCandidatesAvailable.cs` — domain event + `NoCandidatesReason` enum
  - `src/CritterCab.Dispatch/CandidateSelection/INearbyAvailableDriversSource.cs` — interface + `NearbyDriver` record + `NearbyAvailableDriversStub`
  - `src/CritterCab.Dispatch/CandidateSelection/DispatchPolicySnapshot.cs` — DI record with `Default` sentinel; name reserves `DispatchPolicy` for Slice 11
  - `src/CritterCab.Dispatch/CandidateSelection/CandidateSelectionAutomation.cs` — static handler reacting to `FareQuoted`, `[WriteAggregate]` on second stream event, returns `ICandidateSelectionOutcome`
  - `src/CritterCab.Dispatch/CandidateSelection/RequestRoundsProjection.cs` — `partial class`, `SingleStreamProjection<RequestRounds, Guid>`; consumed by Slice 9
  - `src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs` — extended with `Apply(IEvent<CandidatesSelected>)` and `Apply(IEvent<NoCandidatesAvailable>)`
  - `src/CritterCab.Dispatch/Program.cs` — two new event registrations, `RequestRoundsProjection` inline, `INearbyAvailableDriversSource` stub singleton, `DispatchPolicySnapshot.Default` singleton
  - `tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs` — `NearbyDriversSource` property + `ForwardingNearbyDriversSource` inner class + `DispatchPolicySnapshot.Default` registered
  - `tests/CritterCab.Dispatch.Tests/CandidateSelection/Slice53CandidatesSelectedTests.cs` — three Alba tests, one per W001 §5.3 GWT; `IDisposable` teardown
  - `tests/CritterCab.Dispatch.Tests/FareQuoting/Slice52FareQuotedHappyPathTests.cs` — updated to expect 3 events (automation now runs after `FareQuoted`); `NearbyDriversSource` reset added to arrange
  - `tests/CritterCab.Dispatch.Tests/FareQuoting/Slice52FareQuotedFailurePathTests.cs` — transient-recovery test updated to expect 3 events; other two failure-path tests unaffected (emit `FareQuoteFailed`, automation does not trigger)
  - `docs/narratives/002-driver-accepts-a-ride.md` — `## Document History` v0.2 entry (spec-delta closure-loop step 4, primary)
  - `docs/workshops/001-dispatch-event-model.md` — `## Document History` v0.7 entry (spec-delta closure-loop step 4, secondary)
  - `docs/prompts/implementations/005-dispatch-slice-5-3-candidates-selected.md` — Status field updated Pending → Complete
  - This retro + `docs/retrospectives/README.md` index entry
- **Outcome:** Slice 5.3 implemented end-to-end. 11/11 tests green (1 smoke + 3 slice 5.1 + 1 slice 5.2 happy + 3 slice 5.2 failure/transient + 3 slice 5.3). `[WriteAggregate]` on a non-first stream event verified against Wolverine source and confirmed working. Third substantive forward exercise of the spec-delta closure-loop convention; both named amendments landed.

---

## Framing

First slice of the dispatch-round arc (slices 5.3–5.5). `CandidateSelectionAutomation` is the second automation in Dispatch and the first to trigger on a non-first stream event (`FareQuoted`, the stream's second event). This session establishes the `INearbyAvailableDriversSource` stub seam (parking-lot #4 deferred) and the `RequestRoundsProjection` foundation consumed by Slice 9 re-dispatch. Also the **third substantive forward exercise** of the spec-delta closure-loop convention.

---

## Outcome summary

`CandidateSelectionAutomation` reacts to `FareQuoted` via `UseFastEventForwarding`, loads `RideRequest` via `[WriteAggregate(nameof(FareQuoted.RideRequestId))]` (first use of this attribute on a non-first stream event in the codebase), queries `INearbyAvailableDriversSource`, applies in-process vehicle-class filtering, and emits one of two Klefter decision events:

- **`CandidatesSelected`** — ≥1 eligible driver; top N by inverse-distance match score (1/distanceMeters, floored at 1m), capped to `maxCandidatesPerRound`
- **`NoCandidatesAvailable`** — 0 eligible drivers; reason distinguishes `NoDriversInRange` (allDrivers == 0) from `NoCapableDriversInRange` (allDrivers > 0, vehicleClass-filtered == 0)

`RequestRoundsProjection` (inline, `partial`) tracks round history. `RequestTimelineProjection` extended for both outcome events. All three W001 §5.3 GWTs have Alba coverage.

---

## What worked

- **`[WriteAggregate]` on a non-first stream event was verified before coding.** The jasperfx-source-verifier confirmed that the attribute's `FindIdentity` path is trigger-agnostic: it resolves the stream ID from the named property (`FareQuoted.RideRequestId`) and calls `FetchForWriting<RideRequest>(id)` regardless of which event opened the stream. The generated proof in `RaiseHandler488373842.cs` (a stream opened by `LetterStarted`, subsequent `Raise` handler using `[WriteAggregate]`) matched the exact scenario. Zero surprises at runtime — the pattern transferred from `FareQuoteAutomation` without modification.

- **Marker-interface return type pattern (`ICandidateSelectionOutcome`) continued to work cleanly.** Wolverine's `DetermineEventCaptureHandling` recognizes any non-`IEnumerable<object>` return as a single-event append via `eventStream.AppendOne(<runtimeType>)`. The concrete type (`CandidatesSelected` or `NoCandidatesAvailable`) is what Marten persists; the interface is purely compile-time documentation. Consistent with `IFareQuoteOutcome` from slice 5.2.

- **`ForwardingNearbyDriversSource` indirection worked first-try.** Same pattern as `ForwardingPricingClient` — a stable singleton that delegates to a fixture property on every call. Tests can swap stubs after host construction without rebuilding Alba. No lifecycle friction.

- **In-process vehicle-class filtering logic was clean.** The stub ignores `vehicleClassRequired` and returns all registered drivers. The automation calls once, filters in-process, and uses `allDrivers.Count` vs `eligible.Count` to determine the reason enum — one call, no two-pass design, no contract ambiguity about what the interface is responsible for filtering.

- **Spec-delta closure-loop convention held a third time** with no structural friction. Two checkbox confirmations below.

---

## What was harder than expected

- **Existing slice 5.2 tests broke on `events.Count.ShouldBe(2)`.** The prompt's "all prior tests still pass" requirement and its "no opportunistic edits to other files" rule are in tension when a new automation extends the chain through previously-tested behavior. The fix was correct and necessary — two tests needed updates (the slice 5.2 happy-path test and the transient-recovery failure-path test, which both emit `FareQuoted` and now trigger the automation). The two pure-failure-path tests (`FareQuoteFailed`) are unaffected because the automation only listens to `FareQuoted`. This update is not an opportunistic edit; it is a direct consequence of the session's required chain extension and is defensible under the "necessary update, not opportunistic cleanup" principle. Flagged here as a pattern: *any subsequent automation that reacts to an event emitted by a prior automation will similarly require updating tests that assert on event count for that event type.*

- **GWT 3 missing `RequestTimeline` assertion** was caught by the critter-test-architect Phase 2 review. Added before committing the tests. The gap was minor but inconsistent with GWT 1 and GWT 2 coverage levels.

---

## Methodology refinements

- **Extend-chain test updates are in-scope when they are required by the session's own definition of done.** The "no opportunistic edits" rule targets unrelated cleanups, not updates forced by the session's mechanical changes to the automation chain. When a session adds an automation that fires on an event already present in the test baseline, updating the affected `events.Count` assertions is mandatory, not opportunistic.

- **Phase 2 audit after coding (not just after committing) would catch the GWT 3 gap sooner.** Running critter-test-architect while the test file is still open is cheaper than amending the commit.

- **ICandidateSelectionOutcome is the third marker-interface occurrence.** Retro 004 flagged this pattern as a candidate for a skill-file entry after the third instance. This is that third instance. The encoding session is still deferred, but the threshold has been met. Recommend a `tidy: skills` PR to encode the pattern into `wolverine-handlers` or a new `wolverine-marten-automation` skill file before slice 5.4.

---

## Outstanding items / next-session inputs

- **Skill-file gap: marker-interface union return type.** `ICandidateSelectionOutcome` / `IFareQuoteOutcome` pattern has appeared three times. Encode in `wolverine-handlers` or a new skill file in the next `tidy: skills` session.
- **Skill-file gap: `CandidateSelectionAutomation` shape for event-triggered automations with `[WriteAggregate]`.** The `Handle(DomainEvent @event, [WriteAggregate(...)] Aggregate, ...)` shape has no CritterCab skill. Both automation handlers use it; a third will solidify it as a named pattern.
- **Workshop §5.2 *Reads* list inconsistency** — carried from retro 004; not this session's scope.
- **`"CritterBids API"` Swagger title in `Program.cs:94`** — pre-existing; not introduced by this session; carry to a `tidy: housekeeping` session.
- **Bundling-rule encoding** — past the encoding threshold; still queued for a `tidy:` session.
- **Slice 5.4 (`OfferSent` / fan-out)** is the next dispatch-round arc slice; it consumes `CandidatesSelected`.

---

## Spec delta — landed?

Both named amendments landed as planned:

- ✅ **`docs/narratives/002-driver-accepts-a-ride.md` `## Document History` v0.2 entry** — authorial-call amendment recording that `CandidatesSelected` (the "upstream of his vantage" machinery Moment 1 references) now has runnable Alba coverage. No narrative-prose change needed; amendment is in the authorial-call layer.
- ✅ **`docs/workshops/001-dispatch-event-model.md` `## Document History` v0.7 entry** — names all three GWTs covered, both new events, `INearbyAvailableDriversSource` stub seam, `DispatchPolicySnapshot` DI record, `RequestRoundsProjection`, and the `[WriteAggregate]`-on-second-event first use.
