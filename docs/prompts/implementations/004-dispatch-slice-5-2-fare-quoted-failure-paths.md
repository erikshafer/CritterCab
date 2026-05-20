# Prompt 004 — Implement Slice 5.2: FareQuoted Failure Paths

| Field | Value |
|---|---|
| **Status** | Pending (authored 2026-05-19; awaiting review before execution) |
| **Authored** | 2026-05-19 |
| **Target artifacts** | `src/CritterCab.Dispatch/FareQuoting/` (extends slice 003: new `FareQuoteFailed.cs`, new `FareQuoteAttempts.cs`, modifications to `FareQuoteAutomation.cs`, `IPricingClient.cs`, `PricingClientStub.cs`); `src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs` (fold `FareQuoteFailed`); `src/CritterCab.Dispatch/Program.cs` (register new event type + projection); `tests/CritterCab.Dispatch.Tests/FareQuoting/` (three new test classes or one combined with the slice 003 happy-path test); `tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs` (per-test `IPricingClient` override mechanism); `docs/narratives/001-rider-books-a-ride.md` (`## Document History` v0.4 entry); `docs/workshops/001-dispatch-event-model.md` (`## Document History` v0.6 entry); this prompt; `docs/prompts/README.md` (index entry); `docs/retrospectives/implementations/004-dispatch-slice-5-2-fare-quoted-failure-paths.md` (retro); `docs/retrospectives/README.md` (index entry) |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §5.2 (three failure GWTs + retry-counter-durability decision); [`docs/prompts/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md`](./003-dispatch-slice-5-2-fare-quoted-happy-path.md) + its [retro](../../retrospectives/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md) (direct precedent — code this session extends); [`docs/narratives/001-rider-books-a-ride.md`](../../narratives/001-rider-books-a-ride.md) (Moment 2's happy-path scope unchanged; failure paths belong to a subsequent narrative per the deferred section) |
| **Workflow position** | Third vertical-slice implementation in Dispatch. Closes slice 5.2's deferred alternate paths from session 003. **Second substantive forward exercise of the spec-delta closure-loop convention** — meets ADR-016's 2–3-exercises deferral trigger. Two-implementations-in-a-row note: 5.2 completion is the natural continuation of 5.2 happy-path, not a fresh implementation arc; the next session after this one should be a design return (Pricing BC workshop, a skill-tidy, or one of the housekeeping micro-PRs queued in 003's retro) before slice 5.3 starts. |

---

## Spec delta

- **`docs/narratives/001-rider-books-a-ride.md` `## Document History` gains a v0.4 entry**: All four W001 §5.2 GWTs (happy + three failure) now have runnable Alba coverage. Moment 2's narrative-prose scope is unchanged — failure paths still belong to a subsequent narrative per the deferred section — but the v0.4 entry records that the slice's full GWT spec is now implemented and that the deferred-from-this-narrative items numbered 1–N for slice 5.2 are now closed (or which remain).
- **`docs/workshops/001-dispatch-event-model.md` `## Document History` gains a v0.6 entry**: All four §5.2 GWTs have test coverage. `FareQuoteAttempts` projection is implemented as a terminal-events-only view per the resolution of the workshop's retry-counter-durability ambiguity (see `## Framing` below). `DispatchPolicy` view + `DispatchPolicyConfigured` event remain deferred to Slice 11; retry config is hardcoded in this session (3 attempts, 2-second cooldown per the workshop's defaults).
- **No new proto artifact.** `FareQuoteFailed` is Dispatch-local — it feeds Slice 9's re-dispatch/abandonment automation (intra-BC), not a cross-BC integration event. The session does not touch `/protos/`.

---

## Framing

Slice 5.2's happy path shipped in PR #20 with three failure GWTs explicitly deferred. This session closes them. The three GWTs (transient retry recovery, exhausted retries, non-transient failure) translate to three test scenarios, one shared retry mechanism in `FareQuoteAutomation`, and one new projection (`FareQuoteAttempts`).

**One workshop inconsistency to resolve in this session.** W001 §5.2 says two things that don't fully reconcile:

1. *"Retry attempts themselves are NOT emitted as events. Only the terminal outcome reaches the stream."*
2. *"`FareQuoteAttempts` view — per-request retry counter. Survives service restart; prevents double-spending retry budget."* — and lists this view in the automation's **Reads** section.

If retries don't emit events, the projection can't be event-fed in-flight, which means consulting it pre-retry can't prevent double-spending under crash-mid-retry. The resolution this session commits to: **terminal-only `FareQuoteAttempts` projection.** Wolverine's durable inbox + envelope `Attempts` metadata handles in-flight retry budgeting; the projection captures the terminal `attemptCount` value for ops dashboards (the audit trail "how many tries did this request take?"). The workshop's "consult FareQuoteAttempts before retry" line is over-spec for the v1 implementation and should be amended in a future workshop-tidy session — flag it in this session's retro.

This is the second substantive forward exercise of the spec-delta closure-loop convention. ADR-016's deferral trigger (2–3 substantive exercises) is met after this session.

---

## Goal

Implement the three FareQuoteFailed alternate paths end-to-end:
1. Transient failure with retry recovery (1 fail, then succeed) → `FareQuoted` emitted, no `FareQuoteFailed`.
2. Exhausted retries (3 consecutive transient fails) → `FareQuoteFailed { reason: PricingUnavailable, attemptCount: 3 }`.
3. Non-transient failure (NO_COVERAGE on first call) → `FareQuoteFailed { reason: NoCoverage, attemptCount: 1 }`, no further attempts.

Add `FareQuoteAttempts` terminal-events-only projection. Make `PricingClientStub` configurable per-test for failure injection. Three Alba integration tests.

---

## Orientation files (read in order)

1. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §5.2** — re-read the three failure GWT sketches, the `FareQuoteFailed` event shape (`rideRequestId`, `reason` enum, `attemptCount`, `failedAt`), the retry-counter-durability decision, and the cross-references to Slice 9 (re-dispatch consumer) and Slice 11 (`DispatchPolicyConfigured`).
2. **[Prompt 003](./003-dispatch-slice-5-2-fare-quoted-happy-path.md) + [its retro](../../retrospectives/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md)** — direct precedent. The handler shape, the event-forwarding configuration, the `WithNameSuffix("Automation")` discovery customization, the Alba test fixture, and the `[WriteAggregate]` pattern all carry forward. The retro's Outstanding items section names this session as the follow-up.
3. **Skill files (same as 003 plus one):** [`wolverine-messaging-handlers`](../../skills/wolverine-messaging-handlers/SKILL.md), [`marten-wolverine-aggregates`](../../skills/marten-wolverine-aggregates/SKILL.md), [`marten-projections`](../../skills/marten-projections/SKILL.md), [`testing-integration`](../../skills/testing-integration/SKILL.md). For the retry-policy primitive Wolverine offers as an alternative to the manual-loop approach this prompt commits to, ai-skills `wolverine-messaging-resiliency-policies` is the ai-skills-side reference.

---

## Design decisions committed by this prompt

Three implementation-mechanism choices are made up-front to keep the session focused on the slice's spec rather than re-deriving design:

### 1. `IPricingClient` signals failure via exception types, not a Result-type

```csharp
public interface IPricingClient
{
    Task<GetFareQuoteResponse> GetFareQuoteAsync(GetFareQuoteRequest request, CancellationToken ct = default);
}

public sealed class TransientPricingException : Exception { ... }
public sealed class NonTransientPricingException(FareQuoteFailureReason reason, string message) : Exception(message)
{
    public FareQuoteFailureReason Reason { get; } = reason;
}

public enum FareQuoteFailureReason
{
    Unspecified,
    PricingUnavailable,
    InvalidRoute,
    NoCoverage,
    Other
}
```

Rationale: external-call seams whose failure modes are semantically exceptional (timeout, service-down, domain-level reject) idiomatically throw in .NET. Wolverine's retry-policy surface keys off exception types. A Result-type alternative would force the handler to branch internally and re-throw to trigger retry, which is awkward.

### 2. Retry behavior lives in `FareQuoteAutomation.Handle` as a manual loop, not as a Wolverine chain policy

```csharp
public static async Task<object> Handle(
    RideRequested @event,
    [WriteAggregate(nameof(RideRequested.RideRequestId))] RideRequest rideRequest,
    IPricingClient pricing,
    TimeProvider time,
    CancellationToken ct)
{
    const int MaxAttempts = 3;
    var cooldown = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= MaxAttempts; attempt++)
    {
        try
        {
            var response = await pricing.GetFareQuoteAsync(BuildRequest(@event), ct);
            return BuildFareQuoted(rideRequest, response, time);
        }
        catch (NonTransientPricingException ex)
        {
            return BuildFareQuoteFailed(rideRequest, ex.Reason, attempt, time);
        }
        catch (TransientPricingException) when (attempt < MaxAttempts)
        {
            await Task.Delay(cooldown, ct);
        }
    }
    return BuildFareQuoteFailed(rideRequest, FareQuoteFailureReason.PricingUnavailable, MaxAttempts, time);
}
```

Rationale: Wolverine's `OnException<T>().RetryWithCooldown(...)` chain policy is the more "primitive-aligned" choice ([[feedback_critter_stack_primitives]]) but adds a "what to do on exhausted retries" tax — by default Wolverine moves the envelope to the DLQ, and emitting `FareQuoteFailed` from that path requires either a continuation-with-publish or a separate error-queue handler. The manual loop:
- Keeps the terminal-failure emit path adjacent to the retry logic (readable).
- Is deterministically testable without configuring Wolverine retry policies in the test fixture.
- Makes the `attemptCount` value trivially correct (it's the loop variable).

The trade is that Wolverine's `Envelope.Attempts` metadata isn't used, and the durable-inbox redrive semantics don't compose with the in-handler retry loop — if the process crashes mid-loop, the redrive restarts attempt counting from 1. For this slice (synchronous external call, hardcoded 3-attempt budget, no cross-process state), that's acceptable. Slice 11 may revisit when `DispatchPolicyConfigured` makes the budget configurable.

The return type is `object` because the handler can return either `FareQuoted` or `FareQuoteFailed` — Wolverine's cascading-return inspection treats the runtime type as the event to append. If `object` proves too loose, switch to a marker base type or two separate handler methods discriminated by exception unwinding.

### 3. `FareQuoteAttempts` projection is terminal-events-only (not per-attempt event-projected)

```csharp
public class FareQuoteAttempts
{
    public Guid Id { get; init; }
    public int AttemptCount { get; init; }
    public FareQuoteOutcome Outcome { get; init; }
    public FareQuoteFailureReason? FailureReason { get; init; }
    public DateTimeOffset SettledAt { get; init; }
}

public enum FareQuoteOutcome { Pending, Quoted, Failed }

public class FareQuoteAttemptsProjection : SingleStreamProjection<FareQuoteAttempts, Guid>
{
    public FareQuoteAttempts Create(IEvent<RideRequested> e) => new()
    {
        Id = e.StreamId,
        AttemptCount = 0,
        Outcome = FareQuoteOutcome.Pending,
        SettledAt = e.Data.RequestedAt
    };

    public FareQuoteAttempts Apply(IEvent<FareQuoted> e, FareQuoteAttempts view) =>
        view with { AttemptCount = 1, Outcome = FareQuoteOutcome.Quoted, SettledAt = e.Data.QuotedAt };

    public FareQuoteAttempts Apply(IEvent<FareQuoteFailed> e, FareQuoteAttempts view) =>
        view with
        {
            AttemptCount = e.Data.AttemptCount,
            Outcome = FareQuoteOutcome.Failed,
            FailureReason = e.Data.Reason,
            SettledAt = e.Data.FailedAt
        };
}
```

Rationale: the workshop's "Retry attempts themselves are NOT emitted as events" rules out per-attempt events. The projection is fed from terminal events only; the in-flight attempt count lives in the handler's loop variable. The view's purpose is operations observability ("how many tries did this request take?") and Slice 9's re-dispatch automation lookup (it consumes `FareQuoteFailed` and may want the attempt count for policy decisions). Note that for `FareQuoted` the `AttemptCount` field is set to 1 — the workshop's happy path doesn't carry attempt count on `FareQuoted`, but the projection still needs a value; "1" is the natural floor.

**Workshop inconsistency to flag in retro:** the workshop's §5.2 *Reads* section lists `FareQuoteAttempts` as a view the automation reads, implying pre-retry consultation. With terminal-only projection that's not possible (the view is empty until terminal). Either the workshop's *Reads* line is over-spec, or the implementation should evolve to per-attempt events later. Flag for a workshop-tidy session.

---

## Working pattern

**One Alba test per GWT, RED → GREEN → REFACTOR.** Same shape as 003 — submit a ride request via slice 5.1's HTTP endpoint, `ExecuteAndWaitAsync` for the full chain, assert on stream tail + `RequestTimeline` + `FareQuoteAttempts`.

**Stub failure injection.** The cleanest shape: `PricingClientStub` takes a constructor parameter — a `Func<int, GetFareQuoteResponse>` (attempt-number → response, or throws to signal failure) — that the stub invokes per call with an internal call counter. The test fixture's `ConfigureServices` callback overrides `IPricingClient` with a test-specific stub per scenario. This is the **first session to extend `DispatchTestFixture` with per-test DI override**, unlike slice 5.2 happy path which inherited the production stub registration.

The fixture extension shape (informal sketch — finalize during implementation):

```csharp
public class DispatchTestFixture : IAsyncLifetime
{
    // ... existing PostgreSQL container ...
    public IPricingClient PricingClient { get; set; } = new PricingClientStub();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting("ConnectionStrings:crittercab_dispatch", _postgres.GetConnectionString());
            builder.ConfigureServices(services =>
                services.AddSingleton<IPricingClient>(_ => PricingClient));
        });
    }
}
```

Each test assigns `_fixture.PricingClient = new ConfigurablePricingStub(...)` in its arrange phase. The `ConfigureServices` lambda runs once per fixture (per Alba host construction), but the singleton resolution dereferences `_fixture.PricingClient` at handler-injection time. Validate this lifecycle works as expected; if it doesn't, switch to a scoped factory.

**Commit cadence.** Per the now-thrice-confirmed bundling pattern:
- This prompt + `docs/prompts/README.md` index entry → one bundled commit (already shipped on this branch as the prompt-bundling commit).
- Per-file commits during the session for the implementation code, the test code, the spec amendments (narrative + W001 bundled), and the retro + retros/README index bundled.

**Commit subjects** continue slice-shaped phrasing from 003:
- `feat(dispatch): slice 5.2 FareQuoteFailed event + projection`
- `feat(dispatch): slice 5.2 manual-retry loop in FareQuoteAutomation`
- `test(dispatch): slice 5.2 failure-path Alba integration tests`
- `docs(specs): slice 5.2 completion spec amendments — narrative 001 + W001 Document History`

---

## Deliverable plan

1. **`src/CritterCab.Dispatch/FareQuoting/FareQuoteFailed.cs`** — domain event matching W001 §5.2 (`RideRequestId`, `Reason: FareQuoteFailureReason`, `AttemptCount`, `FailedAt`). Co-locate `FareQuoteFailureReason` enum here or in `IPricingClient.cs` per design decision #1 — pick one and stay consistent.
2. **`src/CritterCab.Dispatch/FareQuoting/FareQuoteAttempts.cs`** — projection document + `SingleStreamProjection<FareQuoteAttempts, Guid>` per design decision #3. Registered as inline (matches slice 5.1 + slice 5.2 happy-path projections).
3. **`src/CritterCab.Dispatch/FareQuoting/IPricingClient.cs`** — add `TransientPricingException`, `NonTransientPricingException(FareQuoteFailureReason)`, `FareQuoteFailureReason` enum per design decision #1. The interface signature is unchanged from slice 003 — only the failure contract is added.
4. **`src/CritterCab.Dispatch/FareQuoting/PricingClientStub.cs`** — extend to accept a per-call decision callable (constructor param). Default callable preserves the existing canned $21.50 happy-path response for slice 003's test (no regression).
5. **`src/CritterCab.Dispatch/FareQuoting/FareQuoteAutomation.cs`** — manual retry loop per design decision #2. Hardcoded 3 attempts + 2-second cooldown. Return type `object` (or refactor to a marker base — decide on first compile).
6. **`src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs`** — fold `FareQuoteFailed` into the timeline projection.
7. **`src/CritterCab.Dispatch/Program.cs`** — register `FareQuoteFailed` event type via `opts.Events.AddEventType<FareQuoteFailed>()`; register `FareQuoteAttemptsProjection` as inline.
8. **`tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs`** — add per-test `IPricingClient` override mechanism per the Working Pattern sketch.
9. **`tests/CritterCab.Dispatch.Tests/FareQuoting/`** — three new tests (one per GWT). Decide between three classes (one per scenario) and one class with three `[Fact]` methods based on what reads better; the existing `Slice52FareQuotedHappyPathTests` is a single-class precedent.
10. **`docs/narratives/001-rider-books-a-ride.md`** + **`docs/workshops/001-dispatch-event-model.md`** Document History entries (bundled commit per closure-loop's fourth step).
11. **Retro + retros/README index entry** bundled commit. Retro must include `Spec delta — landed?` line forward-confirming the two named amendments (no proto delta this session).

### Definition of done

- Three new tests pass (RED → GREEN → REFACTOR walked for each).
- All slice 5.1 + 5.2 happy-path tests still pass (no regression).
- `FareQuoteAttempts` projection assertions in each test confirm the terminal `Outcome` and `AttemptCount`.
- `RequestTimeline` projection assertions confirm `FareQuoteFailed` appears with appropriate summary line for the two failure-path tests.
- Both spec-amendment Document History entries committed (narrative 001 v0.4 + W001 v0.6).
- Both spec-delta-named items confirmed in the retro's `Spec delta — landed?` line.
- Workshop inconsistency flagged in retro for a future workshop-tidy session.
- No Claude attribution on commits or PR per established convention.

---

## Out of scope

- **`DispatchPolicy` view + `DispatchPolicyConfigured` event** (Slice 11). Retry config stays hardcoded; Slice 11 lands the policy mechanism. The slice-11 session will swap the hardcoded `MaxAttempts = 3` and `cooldown = 2 seconds` for the projected values from `DispatchPolicy`.
- **Slice 9 re-dispatch/abandonment automation** — the downstream consumer of `FareQuoteFailed`. This session lands `FareQuoteFailed`; Slice 9 reads it.
- **Real `PricingClient`** — stub remains, just now with failure injection. The real gRPC implementation lands when Pricing BC is workshopped.
- **Cross-BC `FareQuoteFailed` proto** — Dispatch-local event, not a cross-BC integration event. No `/protos/` changes.
- **`buf generate` verification** — orthogonal infrastructure concern flagged in 003's retro; belongs to a separate housekeeping micro-PR.
- **Workshop §5.2 *Reads* line revision** — flag in retro, defer to a workshop-tidy session.
- **v0.2 narrative-001 Document History backfill** — flagged in 003's retro, defer to a separate housekeeping micro-PR.
- **The three Wolverine skill-file gaps** flagged in 003's retro — defer to a Wolverine-focused skill-tidy session.
- **Bundling-rule encoding** (third confirmation hit threshold per 003's retro) — defer to a methodology session.
- **Prompt-Status-edit rule tension resolution** — defer to the same methodology session as bundling-rule encoding.
