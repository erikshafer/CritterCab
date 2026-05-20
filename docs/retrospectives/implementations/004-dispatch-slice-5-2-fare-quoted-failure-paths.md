# Retrospective — Implement Slice 5.2: FareQuoted Failure Paths

## Metadata

- **Triggering prompt:** [`docs/prompts/implementations/004-dispatch-slice-5-2-fare-quoted-failure-paths.md`](../../prompts/implementations/004-dispatch-slice-5-2-fare-quoted-failure-paths.md)
- **Status:** Complete
- **Date authored:** 2026-05-19
- **Output artifacts:**
  - `src/CritterCab.Dispatch/FareQuoting/IFareQuoteOutcome.cs` — marker interface implemented by both terminal events; the handler's return type
  - `src/CritterCab.Dispatch/FareQuoting/FareQuoteFailed.cs` — domain event matching W001 §5.2 (`RideRequestId`, `Reason`, `AttemptCount`, `FailedAt`)
  - `src/CritterCab.Dispatch/FareQuoting/FareQuoteAttempts.cs` — terminal-events-only projection (`Pending` from `Create(RideRequested)`, then `Quoted` or `Failed` from terminal `Apply`)
  - `src/CritterCab.Dispatch/FareQuoting/FareQuoteRetryPolicy.cs` — DI-injected retry budget record with `Default = (3 attempts, 2-second cooldown)` per W001 §5.2's hardcoded values
  - `src/CritterCab.Dispatch/FareQuoting/IPricingClient.cs` — extended with `FareQuoteFailureReason` enum and `TransientPricingException` / `NonTransientPricingException(reason)` types
  - `src/CritterCab.Dispatch/FareQuoting/PricingClientStub.cs` — extended with optional per-call `Func<int, GetFareQuoteResponse>` decision delegate; default preserves the canned $21.50 STANDARD response
  - `src/CritterCab.Dispatch/FareQuoting/FareQuoteAutomation.cs` — manual retry loop with two `TransientPricingException` catches (the `when (attempt < retry.MaxAttempts)` filter for the retry branch, an unfiltered catch for the exhausted branch); returns `IFareQuoteOutcome`
  - `src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs` — extended to fold `FareQuoteFailed` with summary line including reason + attempt count
  - `src/CritterCab.Dispatch/Program.cs` — registered `FareQuoteFailed` event, `FareQuoteAttemptsProjection` (inline), and `FareQuoteRetryPolicy.Default` singleton
  - `tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs` — added `PricingClient` and `RetryPolicy` per-test override properties; pricing-client registration wraps a `ForwardingPricingClient` so mutations after host construction take effect on the next call
  - `tests/CritterCab.Dispatch.Tests/FareQuoting/Slice52FareQuotedFailurePathTests.cs` — three Alba tests, one per W001 §5.2 failure GWT
  - `tests/CritterCab.Dispatch.Tests/FareQuoting/Slice52FareQuotedHappyPathTests.cs` — added explicit stub-reset in arrange + `FareQuoteAttempts` projection assertions for the happy path
  - `docs/narratives/001-rider-books-a-ride.md` — `## Document History` v0.4 entry (closure-loop's fourth step, primary)
  - `docs/workshops/001-dispatch-event-model.md` — `## Document History` v0.6 entry (closure-loop's fourth step, secondary)
  - `docs/prompts/implementations/004-dispatch-slice-5-2-fare-quoted-failure-paths.md` — Status field updated Pending → Complete
  - `docs/prompts/README.md` — index entry status updated pending → complete, refined to enumerate the artifacts actually produced
  - This retro + `docs/retrospectives/README.md` index entry
- **Outcome:** Slice 5.2's three deferred FareQuoteFailed alternate paths landed end-to-end. All 8 tests pass (1 smoke + 3 slice 5.1 + 1 slice 5.2 happy + 3 slice 5.2 failure). Second substantive forward exercise of the spec-delta closure-loop convention; both prompt-named amendments landed as named.

---

## Framing

Slice 5.2 closure session — completes the three FareQuoteFailed alternate paths deferred from slice 003's happy-path PR (#20). This is the **second substantive forward exercise** of the spec-delta closure-loop convention, meeting ADR-016's 2–3-exercises deferral trigger. It is also the second implementation PR in a row against Dispatch; per the prompt's framing the next session should be a design return (Pricing BC workshop, a skill-tidy, or a housekeeping micro-PR) before slice 5.3 starts.

---

## Outcome summary

`FareQuoteAutomation` now runs a 3-attempt retry loop with a 2-second cooldown (test override 10ms via `FareQuoteRetryPolicy` DI). The handler returns the new `IFareQuoteOutcome` marker interface so the compiler enforces "one of the two terminal events". Three failure-path tests exercise the GWTs in order: transient-recovery (1 transient + success → `FareQuoted`); exhausted-retries (3 transient → `FareQuoteFailed { PricingUnavailable, 3 }`); non-transient (first call throws `NonTransientPricingException(NoCoverage)` → `FareQuoteFailed { NoCoverage, 1 }` with `callCount.ShouldBe(1)` proving no retry). `FareQuoteAttempts` is implemented as a terminal-events-only projection per the workshop-inconsistency resolution; `RequestTimeline` folds both terminal outcomes.

---

## What worked

- **Two design questions surfaced and decided up-front, before any code.** Prompt design decision #2 hardcoded the 2-second cooldown; without intervention this would have added ~6s to every CI run. Flagged as a concrete blocker per the prompt's rule #2; user chose the `FareQuoteRetryPolicy` DI record option. Similarly, the handler return type was promoted from `object` (prompt-literal) to `IFareQuoteOutcome` (marker interface). Both upgrades were small but durable — the policy record pre-shapes Slice 11's seam, and the marker reads as the workshop's "terminal outcome" vocabulary.
- **TDD shape held even with multi-test code shared across the suite.** RED came from running the three new tests after the scaffolding compiled; two passed immediately (transient-recovery, non-transient) and one failed (exhausted-retries) for an instructive reason — not "wrong assertion" but "unhandled exception leaked through the try/catch". GREEN was a one-edit fix (add a second `catch (TransientPricingException)` for the exhausted case). Refactor was implicit: the loop already had its final shape.
- **`ForwardingPricingClient` indirection cleanly resolved the singleton-cache-vs-mutable-fixture-property tension.** Prompt's working-pattern sketch flagged the lifecycle concern as "validate this; switch to a scoped factory if it doesn't work" — the right fix turned out to be neither factory lifecycle nor scope but a stable singleton dispatching every call through the fixture's current `PricingClient`. Tests can swap stubs after host construction without rebuilding Alba. Pattern is reusable for any future test-controlled DI seam.
- **Test-class teardown via `IDisposable`** localized the cleanup responsibility to the failure-path test class. The happy-path test and `SubmitRideRequestTests` remain ignorant of failure-injection state. Per-test class instantiation in xUnit + collection-fixture state-sharing make this the cleanest pattern.
- **Spec-delta closure-loop convention held a second time, with no structural friction.** Two prompt-named amendments translated directly into two checkbox confirmations below. The convention's lightweight intent ("name in the prompt; confirm in the retro") survived its second substantive exercise.
- **Bundling-instruction-salience pattern hit fourth confirmation if this PR bundles the retro + index commit as expected.** Past the encoding threshold by two more exercises.

---

## What was harder than expected

- **Prompt-supplied code snippet had a control-flow bug.** Prompt design decision #2's snippet used `catch (TransientPricingException) when (attempt < MaxAttempts)` — but when `attempt == MaxAttempts`, the `when` filter fails, the exception propagates out of the for loop, and the snippet's fallback `return BuildFareQuoteFailed(...)` after the loop becomes **unreachable**. The exhausted-retries test surfaced this immediately: Wolverine's `ExecuteAndWaitAsync` asserts no exceptions were thrown during message processing, and the leaked `TransientPricingException` failed the assertion. Lesson: prompts that include code-shaped commits should be evaluated for *runnability*, not just intent — design-decision snippets in prompts are guidance, not specifications, and the implementer is responsible for the executable shape.
- **`Wolverine.Tracking.AssertNoExceptionsWereThrown()` propagates exceptions an in-handler catch would otherwise swallow.** The tracked-session contract is stricter than "did the message handler return normally?" — it's "did anything inside the handler ever throw, even if caught?" The handler's exception was caught (the `when (attempt < MaxAttempts)` filter), but only for attempts 1 and 2; attempt 3 propagated up. Worth knowing for any future handler that catches exceptions internally as part of retry logic — the tests will fail loudly if a catch path is missed.
- **`FareQuoteAttempts` for the `Quoted` outcome needed a fabricated `AttemptCount`.** W001 §5.2 says the workshop's happy path doesn't carry an attempt count on `FareQuoted` (only on `FareQuoteFailed`), but the terminal-only projection still needs to populate the field. "1" is the natural floor — but on the transient-recovery test, the *actual* attempt count was 2 (1 transient + 1 success), and the projection records 1. This is the consequence of "retry attempts themselves are NOT emitted as events": the projection cannot know how many real attempts happened. Flagged as a deliberate documentation gap — `FareQuoteAttempts.AttemptCount` is "1 or the failure's attemptCount", not "the literal number of pricing calls". Worth noting in any future operational dashboard built from this view.
- **xUnit collection-fixture state-sharing required an explicit reset in the happy-path test.** Adding mutable state (`PricingClient` property) to the collection fixture meant the existing `Slice52FareQuotedHappyPathTests` could be poisoned by a previously-run failure test in the same collection. The fix was two changes: (a) explicit `_fixture.PricingClient = new PricingClientStub()` in the happy-path test's arrange phase, and (b) `IDisposable.Dispose` on the failure-path test class restoring the default stub. Order independence achieved, but the pattern is a recurring tax on any test that adds mutable fixture state. `SubmitRideRequestTests` was left alone because (a) it doesn't check stream contents and (b) the failure-path tests' Dispose leaves a default stub anyway.

---

## Skill-file gaps surfaced

1. **DI forwarder pattern for per-test seam control.** The `ForwardingPricingClient` shape — a stable singleton that dispatches through a mutable fixture property — generalizes to any test-controllable DI seam (clock, message queue, external service). Could land in `testing-integration` as a "Per-test DI overrides" subsection alongside the existing Alba fixture patterns.
2. **`IFareQuoteOutcome` marker interface as a Wolverine handler-return idiom.** Wolverine inspects the runtime type of cascading returns to decide which event-type-registration to use when appending to a stream; a marker interface gives the compiler a type-check without constraining Wolverine. Worth a one-paragraph note in `wolverine-messaging-handlers` for any future automation that has multiple terminal outcomes (likely most of them).
3. **Exception-as-failure-signal vs. Result-type in cross-BC seams.** The decision to throw from `IPricingClient` (idiomatic .NET, plays well with Wolverine retry surfaces) vs. returning a discriminated `Result<Ok, Failure>` is a design choice that recurs at every cross-BC seam. Worth a paragraph in `protobuf-contracts` (where the seam abstraction lives) or a new short skill if the pattern proves divergent across BCs.

---

## Methodology refinements that emerged

- **Prompt-supplied code is guidance, not specification.** This session caught a control-flow bug in a snippet the prompt presented as a design commitment. Future implementation sessions should treat prompt code blocks the way they treat external-doc API examples: read for intent, write for correctness, flag in the retro when the intent has a runnability gap. ADR-016's "prompts should not contain code" guidance (parking-lot only) may eventually warrant explicit encoding; this is the first concrete piece of evidence for it.
- **Implementation-level design-decision commits in the prompt deserve a one-question pre-check.** Three of this session's deliverables (`FareQuoteRetryPolicy` DI record, `IFareQuoteOutcome` marker interface, the prompt's choice of `Task<object>` return type) were addressed by two pre-implementation questions to the user. Each took one question + one selection and saved measurable downstream rework. The pattern: when the prompt commits an implementation mechanism but the implementer sees a cheaper or more domain-aligned alternative, ask before coding. Not every prompt commitment merits a question — but the test of whether to ask is "would the alternative change the persisted shape of the code (return types, DI records, public APIs)?" — yes warrants a question; "would it only change internal mechanics" — no, just do it.
- **Bundling-instruction-salience pattern is at fourth confirmation.** Per slice 003's retro, the encoding threshold was met after the third. This session's commit cadence is once again: prompt + prompts/README index entry as one commit (already shipped on this branch as the prompt-bundling commit `e80bcc7`); retro + retros/README index entry as one commit at session close. **Recommended: codify in `docs/prompts/README.md` § Session and PR cadence as an explicit bundling subsection.** A future tidy session (next-up candidate per the design-return cadence rule) should pick this up.
- **xUnit collection-fixture mutable state needs a discipline.** Adding mutable properties to a collection fixture creates implicit test-ordering dependencies. The discipline that emerged this session: (a) any test that depends on a default fixture state explicitly resets it in arrange; (b) any test that mutates fixture state restores the default in teardown (`IDisposable.Dispose` on the test class). The combination eliminates ordering sensitivity without forcing every test to know about every mutable property. Could warrant a one-paragraph note in `testing-integration` alongside the fixture patterns.

---

## Outstanding items / next-session inputs

- **Workshop §5.2 *Reads* line inconsistency flagged for resolution.** W001 §5.2's `Reads` list names `FareQuoteAttempts` as consumed by the automation pre-retry — but terminal-only projection plus the "retry attempts themselves are NOT emitted as events" rule make pre-retry consultation impossible. Resolution candidates for a workshop-tidy session: (a) drop `FareQuoteAttempts` from §5.2's *Reads* list; or (b) evolve the projection to per-attempt event-fed if the pre-retry consultation is genuinely needed for crash-mid-retry budget protection (the workshop's stated motivation). v0.6 of W001's Document History notes the inconsistency.
- **Slice 11 (`DispatchPolicyConfigured`) will swap the hardcoded `FareQuoteRetryPolicy.Default` for the projected `DispatchPolicy` values.** The DI seam already exists; slice 11 only needs to replace the singleton's value with one read from the projection.
- **Slice 9 re-dispatch / abandonment automation** is the downstream consumer of `FareQuoteFailed`. This session lands the event; slice 9 reads it.
- **Real `PricingClient` (gRPC)** lands when Pricing BC is workshopped. The stub's per-call decision callable supports both production-canned and test-injected responses; the real client replaces it.
- **Bundling-rule encoding** is past the encoding threshold by two more exercises. Pencil for the next tidy session.
- **Prompt-Status-edit rule tension** flagged in slice 003's retro remains unresolved. This session followed the same precedent (Status: Pending → Complete edit on the prompt). The methodology session that encodes the bundling rule should resolve the rule tension at the same time — both are about the prompt as a historical record vs. an index-entry-supporting metadata block.
- **Three Wolverine skill-file gaps from slice 003's retro** remain open: `UseFastEventForwarding` Marten-side API, `*Automation` handler-discovery customization, `[WriteAggregate]` in event-forwarded handlers. This session did not surface a fourth gap but accumulated one new candidate (the marker-interface return-type idiom). A Wolverine-focused skill-tidy session covers all four cleanly.
- **`buf generate` verification gap** from slice 003's retro still open; no proto changes this session.
- **v0.2 slot in narrative 001's Document History** still deliberately unclaimed.
- **Design-return cadence rule satisfied.** Per the prompt's framing and `docs/prompts/README.md` § Design-return cadence, two consecutive implementation PRs against Dispatch (#20 and this PR) mean the **next session should be a design return** before slice 5.3 starts. Candidates: Pricing BC workshop (would also unblock the real `PricingClient`), a Wolverine-focused skill-tidy (would drain the four gaps named above), the bundling-rule encoding methodology session, or any of the housekeeping micro-PRs queued in slice 003's retro.

---

## Spec delta — landed?

Two amendments named in the prompt's `## Spec delta` (the prompt explicitly notes no proto delta this session since `FareQuoteFailed` is Dispatch-local, not a cross-BC integration event):

1. ✅ **`docs/narratives/001-rider-books-a-ride.md` `## Document History` v0.4 entry** — landed in this PR. Names that all four W001 §5.2 GWTs now have runnable Alba coverage; explicitly preserves Moment 2's narrative-prose scope as unchanged; marks the *Pricing fails to quote* deferred-from-this-narrative item as implementation-complete (with narrative-prose treatment continuing to defer per the deferred-section convention).
2. ✅ **`docs/workshops/001-dispatch-event-model.md` `## Document History` v0.6 entry** — landed in this PR. Names all three failure-path GWTs as runnable; documents the manual retry loop, `FareQuoteRetryPolicy` DI seam, terminal-only projection design, and `IFareQuoteOutcome` marker. Surfaces the §5.2 *Reads*-list inconsistency for a future workshop-tidy session.

Both confirmed as named, no divergence. Two substantive forward exercises of the convention now logged (slice 003 and this slice); ADR-016's 2–3-exercises deferral trigger is met. The convention's lightweight intent continues to hold — single-bullet prompt entries translate cleanly into checkbox-style retro confirmations with no structural overhead.
