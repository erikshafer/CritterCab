# Retrospective — Implement Slice 5.2: FareQuoted (Happy Path)

## Metadata

- **Triggering prompt:** [`docs/prompts/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md`](../../prompts/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md)
- **Status:** Complete
- **Date authored:** 2026-05-19
- **Output artifacts:**
  - `protos/crittercab/pricing/v1/get_fare_quote.proto` — `PricingService.GetFareQuote` unary RPC + request/response + BC-owned `VehicleClass` and `FareBreakdown`
  - `src/CritterCab.Dispatch/FareQuoting/FareQuoted.cs` — domain event + `FareBreakdown` record
  - `src/CritterCab.Dispatch/FareQuoting/IPricingClient.cs` — pricing-client interface + domain-level `GetFareQuoteRequest`/`GetFareQuoteResponse`
  - `src/CritterCab.Dispatch/FareQuoting/PricingClientStub.cs` — canned $21.50 STANDARD-class response mirroring narrative 001 Moment 2
  - `src/CritterCab.Dispatch/FareQuoting/FareQuoteAutomation.cs` — Wolverine event handler reacting to `RideRequested`, using `[WriteAggregate]` to load `RideRequest` and append `FareQuoted`
  - `src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs` — extended to fold `FareQuoted`
  - `src/CritterCab.Dispatch/Program.cs` — `FareQuoted` event registration, `UseFastEventForwarding`, `IPricingClient → PricingClientStub` DI, `WithNameSuffix("Automation")` handler discovery
  - `tests/CritterCab.Dispatch.Tests/FareQuoting/Slice52FareQuotedHappyPathTests.cs` — one Alba integration test covering W001 §5.2's happy-path GWT
  - `docs/narratives/001-rider-books-a-ride.md` — `## Document History` v0.3 entry (closure-loop's fourth step, primary)
  - `docs/workshops/001-dispatch-event-model.md` — `## Document History` v0.5 entry (closure-loop's fourth step, secondary)
  - `docs/prompts/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md` — Status field updated Pending → Complete
  - `docs/prompts/README.md` — index entry status updated pending → complete
  - This retro + `docs/retrospectives/README.md` index entry
- **Outcome:** Second vertical slice implemented end-to-end. All 5 tests pass (1 smoke + 3 slice 5.1 + 1 slice 5.2). First substantive forward exercise of the spec-delta closure-loop convention; all three prompt-named amendments landed as named.

---

## Framing

Slice 5.2 is Dispatch's first **automation** slice — no rider action triggers it; a Wolverine event handler reacts to `RideRequested` and reaches across a BC boundary to Pricing for an authoritative quote. The slice introduced three new shapes: a Wolverine event handler (vs. slice 5.1's HTTP command handler), an external gRPC seam (stubbed for now), and the first protobuf authored outside Dispatch's `v1/` directory. It is also **the first session whose prompt was authored under the spec-delta closure-loop convention** encoded in PR #18, and the first to forward-confirm a substantive spec delta.

---

## Outcome summary

A Wolverine event handler now reacts to every `RideRequested` event committed to a `RideRequest` stream: `FareQuoteAutomation.Handle` calls `IPricingClient.GetFareQuoteAsync` (stubbed to the canned $21.50 STANDARD response) and returns `FareQuoted`, which Wolverine appends to the same stream via the `[WriteAggregate]` attribute. `RequestTimeline` now folds both events into the per-request timeline. One Alba integration test exercises the full HTTP-trigger-to-stream-tail chain through Wolverine's tracked `ExecuteAndWaitAsync`. The `PricingService.GetFareQuote` proto is in place under `/protos/crittercab/pricing/v1/` as the second exercise of ADR-009.

---

## What worked

- **Wolverine.Marten event-forwarding as the slice-1-to-slice-2 bridge.** `IntegrateWithWolverine(i => i.UseFastEventForwarding = true)` cleanly forwarded `RideRequested` from the stream commit to the `FareQuoteAutomation` handler without touching slice 5.1's `SubmitRideRequest`. The seam lives in the composition root; the upstream handler stays untouched.
- **Narrative-to-code alignment.** Moment 2's "$21.50, broken into base/distance/time, no additional fees" landed directly as the `PricingClientStub` canned response (500 + 1200 + 450 = 2150 minor units, fees null). The Alba test asserts against the same number the narrator quotes — the test reads as the narrative's claim.
- **Domain naming preserved through the C# layer.** W001 §5.2 pinned `FareQuoteAutomation` as the slice's canonical term (avoiding "Coordinator"/"Orchestrator"). Wolverine's default handler discovery wants `*Handler`/`*Consumer` suffixes, so the domain name would have been mangled. One line of discovery customization in `Program.cs` — `opts.Discovery.CustomizeHandlerDiscovery(d => d.Includes.WithNameSuffix("Automation"))` — preserved the workshop's term as the C# class name. Codifies the project's automation-naming convention at the composition root.
- **TDD RED → GREEN → REFACTOR walked cleanly.** Test compiled and ran on first try (after one missing `using Wolverine.Tracking`), failed with "events.Count = 1, expected 2" (RED — only the trigger event present). Diagnosis via `--verbosity=detailed` surfaced `Wolverine found no handlers`; the discovery-rule fix made it pass (GREEN). One-line refactor (the discovery customization is the refactor of choice over per-type `IncludeType`).
- **Spec-delta closure-loop convention held its lightweight intent.** Three single-bullet entries in the prompt's `## Spec delta` translated directly into three checkbox-style confirmations below. No structural friction, no expansion required.

---

## What was harder than expected

- **`Wolverine found no handlers` was a silent warning, not an exception.** The test failed at `events.Count == 2` (1 actual, 1 expected delta) rather than at handler registration. Without `--verbosity=detailed`, the warning was buried in INFO-level Npgsql noise. Worth knowing that Wolverine's handler-discovery failures land as warnings, not exceptions, when the trigger source (event forwarding) succeeds.
- **`EventForwardingToWolverine()` is deprecated.** The CritterCab skill files mention `UseFastEventForwarding` only on the Polecat side; the Marten-side API documentation pointed at the deprecated extension method. The replacement is the lambda form `IntegrateWithWolverine(integration => { integration.UseFastEventForwarding = true; })` — same shape as Polecat's documented API. Build-time `CS0618` warning caught it; without that I'd have shipped a deprecated call.
- **First non-HTTP Wolverine handler in the codebase.** The `*Endpoint` suffix in slice 5.1 (`SubmitRideRequestEndpoint`) is a Wolverine.HTTP convention discovered via the `[WolverinePost]` attribute. For message handlers there's no attribute equivalent in the codebase yet; the `*Handler`/`*Consumer` suffix default would have mangled the workshop's "Automation" naming. Surfaced this as a small but recurring decision for every future Automation slice.
- **`buf generate` verification gap.** `buf` is not installed locally and ADR-009's Definition-of-Done line ("generates Dispatch client and placeholder Pricing server code via `buf generate`") wasn't directly exercised. The C# implementation uses domain types for `IPricingClient.GetFareQuoteAsync`, not proto-generated types, so the slice doesn't depend on generation — but the proto contract itself is unverified beyond visual review against the precedent in `dispatch/v1/`. Worth either a CI lint step or a developer-prereq note in the protobuf-contracts skill.

---

## Skill-file gaps surfaced

1. **`Wolverine.EventForwardingToWolverine()` → `IntegrateWithWolverine(i => i.UseFastEventForwarding = true)` API migration.** Marten-side equivalent of Polecat's documented `UseFastEventForwarding` property. The `service-bootstrap` or a Marten-specific skill should document the current API and flag the deprecated extension.
2. **Wolverine handler discovery customization for non-default suffixes.** `opts.Discovery.CustomizeHandlerDiscovery(d => d.Includes.WithNameSuffix("...")` is the lever for project-specific naming conventions like CritterCab's "Automation". The `wolverine-messaging-handlers` skill could document this in a "Handler discovery customization" subsection — useful for every future automation slice.
3. **`[WriteAggregate]` in event-forwarded handlers (vs. command handlers).** The `marten-wolverine-aggregates` skill documents `[WriteAggregate]` for HTTP/command handlers exclusively; the same shape works for Wolverine event handlers that receive a forwarded event and append a new event to the same stream. Worth a short worked example.

---

## Methodology refinements that emerged

- **Bundling-instruction-salience refinement is on its third confirmation.** Commit 394fa56 (prompt + prompts/README index bundle) was the second confirmation per the prompt's own framing. This session's final commit will bundle the retro + retros/README index entry — the third. Per the `encode-tidy-methodology-refinements` retro's threshold (two consecutive successes = candidate for encoding), three confirmations is past the trigger. **Candidate for explicit encoding** in `docs/prompts/README.md` as a documented bundling rule for prompt+index and retro+index commits. A future tidy session should pick this up.
- **Spec-delta closure-loop convention held — ADR-016 trigger advances.** The convention's lightweight intent ("name the deltas in the prompt; forward-confirm in the retro") landed cleanly with no structural friction. ADR-016's deferral trigger (2–3 substantive exercises) advances by one; one more substantive exercise (the follow-up slice 5.2 session that lands the FareQuoteFailed paths) would meet the threshold.
- **Workshop-term-to-C#-class-name discipline.** When W001 pins a term (e.g., "FareQuoteAutomation"), the codebase should preserve it at the C# layer. This may conflict with framework conventions (Wolverine wants `*Handler`); customization at the composition root is the right resolution. May be worth a one-line entry in `docs/skills/wolverine-messaging-handlers/SKILL.md` once the second `*Automation` slice ships.
- **Methodology observation: post-session prompt-Status update.** PR #9 encoded "do not edit a prompt after the session it triggered has run." Slice 5.1's prompt has `Status: Complete (2026-05-07)` in its metadata — an apparent edit. The user signed off on this session matching slice 5.1's precedent (update Status; flag the rule tension). The literal rule and the operative pattern are inconsistent. A future tidy session should resolve by either (a) carving status updates out of the rule explicitly, or (b) reverting slice 5.1 to honor the rule. Not load-bearing for this slice — flagged for resolution.

---

## Outstanding items / next-session inputs

- **Three FareQuoteFailed alternate-path GWTs deferred to a follow-up slice 5.2 session.** W001 §5.2 names *Transient failure with retry recovery*, *Exhausted retries*, and *Non-transient failure*. The follow-up session also lands the `FareQuoteAttempts` projection (per-request retry counter, survives restart) and the retry-budget configuration that `DispatchPolicy` will eventually source (Slice 11). The deferred items are the next session's spec-delta candidates.
- **v0.2 slot in narrative 001's Document History is deliberately unclaimed.** Reserved for a retroactive entry covering slice 5.1's implementation (PR #4), which predates the spec-delta closure-loop convention encoded in PR #18. Whoever picks up the backfill should land v0.2 between v0.1 and v0.3.
- **Bundling-rule encoding candidate.** Three confirmations of the prompt+index and retro+index bundling pattern accumulated; explicit encoding in `docs/prompts/README.md` is now warranted. Pencil in a tidy session.
- **Skill-file gaps (3 items above).** Candidates for a Wolverine-focused skill-tidy session: the `EventForwardingToWolverine` deprecation, handler-discovery customization for `*Automation`, and `[WriteAggregate]` in event-forwarded handlers.
- **Prompt-Status-edit rule tension.** Flagged for resolution per the user's sign-off on the matching-precedent decision.
- **`buf generate` verification gap.** Proto contract authored but not generation-verified; either install `buf` locally or add a CI lint/generate step.

---

## Spec delta — landed?

Three amendments named in the prompt's `## Spec delta`:

1. ✅ **`docs/narratives/001-rider-books-a-ride.md` `## Document History` v0.3 entry** — landed in this PR (commit 6764630). Names Moment 2's happy-path implementation, the three deferred FareQuoteFailed paths, and the deliberately-unclaimed v0.2 slot.
2. ✅ **`docs/workshops/001-dispatch-event-model.md` `## Document History` entry** — landed as v0.5 in this PR (commit 6764630). Names §5.2's happy-path GWT confirmation, the three deferred failure-path GWTs, and the end-to-end exercise of the §5.2 *Automation naming* decision via Wolverine discovery customization.
3. ✅ **`/protos/crittercab/pricing/v1/` established with `get_fare_quote.proto`** — landed (commit 48fa2f8). `PricingService.GetFareQuote` unary RPC + `GetFareQuoteRequest`/`GetFareQuoteResponse` pair + BC-owned `VehicleClass` and `FareBreakdown` per ADR-009. Second protobuf directory after `dispatch/v1/`.

All three confirmed as named, no divergence. **First retro in CritterCab's history to forward-confirm a substantive (non-null) prompt-named spec delta.** The convention's lightweight intent held — three single-bullet prompt entries became three checkbox confirmations with no structural overhead.
