# Prompt 003 — Implement Slice 5.2: FareQuoted (Happy Path)

| Field | Value |
|---|---|
| **Status** | Pending (authored 2026-05-19; awaiting review before execution) |
| **Authored** | 2026-05-19 |
| **Target artifacts** | `protos/crittercab/pricing/v1/` (new directory + `get_fare_quote.proto`); `src/CritterCab.Dispatch/FareQuoting/` (new vertical-slice folder); `src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs` (same-file extension to fold `FareQuoted`); `tests/CritterCab.Dispatch.Tests/FareQuoting/` (new test folder); `docs/narratives/001-rider-books-a-ride.md` (`## Document History` v0.3 entry — closure-loop's fourth step); `docs/workshops/001-dispatch-event-model.md` (`## Document History` entry — happy-path GWT covered); `docs/prompts/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md` (this prompt); `docs/prompts/README.md` (index entry); `docs/retrospectives/implementations/003-dispatch-slice-5-2-fare-quoted-happy-path.md` (retro); `docs/retrospectives/README.md` (index entry) |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §5.2 (slice spec); [`docs/narratives/001-rider-books-a-ride.md`](../../narratives/001-rider-books-a-ride.md) Moment 2 (journey); [`docs/decisions/009-protobuf-contracts-as-first-class-artifacts.md`](../../decisions/009-protobuf-contracts-as-first-class-artifacts.md); [`docs/prompts/implementations/002-dispatch-slice-5-1-ride-requested.md`](./002-dispatch-slice-5-1-ride-requested.md) + [retro](../../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md) (closest structural precedent) |
| **Workflow position** | Second vertical-slice implementation in Dispatch. **First session in CritterCab's history with a substantive (non-null) `## Spec delta`** — the spec-delta closure-loop convention's first non-edge-case forward exercise (the housekeeping micro-PR #19 exercised the edge case "no spec delta"). Happy-path only; the three FareQuote*Failed* alternate-path GWTs defer to a follow-up slice 5.2 session that adds retry budget tracking via `FareQuoteAttempts` projection. |

---

## Spec delta

- **`docs/narratives/001-rider-books-a-ride.md` `## Document History` gains a v0.3 entry**: Moment 2's happy path implemented (slice 5.2 happy-path GWT confirmed via Alba). Three FareQuoteFailed alternate paths deferred to a follow-up slice 5.2 session.
- **`docs/workshops/001-dispatch-event-model.md` `## Document History` gains an entry**: §5.2 happy-path GWT has runnable test coverage; three failure-path GWTs (§5.2 *Transient failure with retry recovery*, *Exhausted retries*, *Non-transient failure*) remain awaiting implementation.
- **`/protos/crittercab/pricing/v1/`** is established as the second protobuf directory under ADR-009, with `get_fare_quote.proto` defining the `GetFareQuote` request/response pair. Second exercise of ADR-009 after PR #14's Dispatch business-event protos.

Closure-loop's fourth step lands in narrative 001's `## Document History` (primary, since the narrative drives this slice's user-perspective story) and in W001's `## Document History` (secondary, since the workshop is the slice's GWT spec).

---

## Framing

Slice 5.2 is Dispatch's first **automation** slice — no rider action triggers it; a Wolverine event handler reacts to `RideRequested` and reaches across a BC boundary to Pricing for an authoritative quote. The slice exercises three new shapes that slice 5.1 did not:

1. **Wolverine event handler** triggered by domain events (vs. slice 5.1's HTTP command handler).
2. **External gRPC call** crossing a BC boundary. Pricing BC is not yet workshopped; this session uses a stubbed `IPricingClient` registered in DI to keep the seam clean for the eventual real implementation.
3. **First protobuf authored outside Dispatch's `v1/` directory** — `GetFareQuote` lives under `pricing/v1/` because the contract semantically belongs to Pricing (the published-language owner); Dispatch is the consumer.

This is also the first session whose prompt was authored *under* the spec-delta closure-loop convention (encoded in PR #18). Per Session A's retro, the next session after the housekeeping micro-PR was named as the first opportunity to exercise the convention on a **substantive** spec delta. This session is that.

---

## Goal

Implement the happy-path `FareQuoted` flow: a `FareQuoteAutomation` Wolverine event handler reacts to `RideRequested`, calls the stubbed `IPricingClient.GetFareQuoteAsync`, and emits `FareQuoted` to the rider's stream. Extend `RequestTimeline` to fold `FareQuoted`. One Alba integration test covering W001 §5.2's happy-path GWT.

---

## Orientation files (read in order)

1. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §5.2** — the slice spec. Event shapes (`FareQuoted`), the happy-path GWT (only one in scope), automation pattern, view dependencies (`RequestTimeline` extended; `FareQuoteAttempts` deferred; `DispatchPolicy` deferred). Also read §13 (cross-BC translation slices) to ground the Klefter pattern.
2. **[`docs/narratives/001-rider-books-a-ride.md`](../../narratives/001-rider-books-a-ride.md) Moment 2** — Maya's first system-driven moment. The narrator describes Pricing answering "on the first attempt with an authoritative fare: $21.50, broken down into base, distance, time, and no additional fees." The Alba test's canned stub response should mirror these specifics so the test reads as the narrative's claim.
3. **[`docs/prompts/implementations/002-dispatch-slice-5-1-ride-requested.md`](./002-dispatch-slice-5-1-ride-requested.md) + its [retro](../../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md)** — closest structural precedent. What worked (narrative-to-code alignment, GWT-to-test translation), what was hard (JasperFx/Marten namespace extractions — now fixed in PR #7's skill-tidy).
4. **[`docs/decisions/009-protobuf-contracts-as-first-class-artifacts.md`](../../decisions/009-protobuf-contracts-as-first-class-artifacts.md)** + the [first proto-authoring retro](../../retrospectives/decisions/001-protobuf-ride-assigned.md) — ADR-009's discipline + the established `/protos/` directory layout this session extends.
5. **Skill files**: [`docs/skills/wolverine-messaging-handlers/SKILL.md`](../../skills/wolverine-messaging-handlers/SKILL.md) (event handler patterns), [`docs/skills/marten-wolverine-aggregates/SKILL.md`](../../skills/marten-wolverine-aggregates/SKILL.md) (stream operations), [`docs/skills/marten-projections/SKILL.md`](../../skills/marten-projections/SKILL.md) (extending `RequestTimeline`), [`docs/skills/protobuf-contracts/SKILL.md`](../../skills/protobuf-contracts/SKILL.md) (ADR-009 discipline), [`docs/skills/vertical-slice-organization/SKILL.md`](../../skills/vertical-slice-organization/SKILL.md) (Shared/ promotion threshold), [`docs/skills/testing-integration/SKILL.md`](../../skills/testing-integration/SKILL.md) (Alba test fixture patterns).

---

## Working pattern

Per [[feedback_static_endpoints_alba_first]] (Alba-first integration tests) and the slice 5.1 precedent: **one happy-path Alba test, RED → GREEN → REFACTOR.** The test exercises the full automation flow end-to-end:

1. Submit a ride request via the slice 5.1 HTTP endpoint (causes `RideRequested` to land on the stream).
2. Wait for the `FareQuoteAutomation` event handler to react (Wolverine's in-process message queue; test fixture uses `IHost.TrackActivity` or equivalent to await handler completion).
3. Assert that `FareQuoted` is on the rider's stream with fields matching the stubbed `IPricingClient` response.
4. Assert `RequestTimeline` reflects both `RideRequested` and `FareQuoted` events in order.

**Stubbed `IPricingClient`** registered in the Alba test fixture's DI returns the canned $21.50 STANDARD-class response that mirrors narrative 001 Moment 2. Production DI registration is the stub for now; real `PricingClient` lands when Pricing BC is built.

**Cadence:** bundle the prompt + prompts/README index entry into one commit (per Session A retro's bundling-instruction-salience refinement, now on its second forward exercise — third confirmation of the pattern would make it a candidate for explicit rule encoding). Bundle the retro + retros/README index entry. Per-file commits for the proto, the implementation code, the test code, and the spec amendments (narrative 001 + W001 Document History entries can bundle together as the "closure-loop's fourth step" commit).

**Commit subjects** use slice-shaped phrasing per the precedent set by slice 5.1 — *not* `tidy:` prefix. Examples:
- `feat(dispatch): slice 5.2 happy-path FareQuoted automation`
- `feat(protos): add pricing/v1/get_fare_quote.proto`
- `test(dispatch): slice 5.2 happy-path Alba integration test`
- `docs(specs): slice 5.2 spec amendments — narrative 001 + W001 Document History`

---

## Deliverable plan

1. **`protos/crittercab/pricing/v1/get_fare_quote.proto`** — `GetFareQuote` request + response message types per ADR-009. Buf workspace registration. Generates Dispatch client + (placeholder) Pricing server code via `buf generate`.
2. **`src/CritterCab.Dispatch/FareQuoting/`** — new vertical-slice folder containing:
   - `FareQuoted.cs` — domain event matching W001 §5.2 event shape (rideRequestId, fareAmount, currency, fareBreakdown, vehicleClass, quotedAt, validUntil, pricingPolicyVersion).
   - `IPricingClient.cs` — interface with one method: `Task<FareQuoteResponse> GetFareQuoteAsync(...)`. Return type wraps the proto-generated response so the domain handler doesn't directly handle gRPC types.
   - `PricingClientStub.cs` — canned-response test double. Returns the $21.50 STANDARD-class response. Registered in DI for now; production impl lands when Pricing BC is built.
   - `FareQuoteAutomation.cs` — Wolverine event handler reacting to `RideRequested`. Calls `IPricingClient.GetFareQuoteAsync`, then appends `FareQuoted` to the existing `RideRequest` stream via `IDocumentSession` (matches slice 5.1's session pattern from its retro's Design Decisions §1).
3. **`src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs`** — same-file edit; extend the projection to fold `FareQuoted` events. In-bounds per the no-opportunistic-edits rule because `RequestTimeline` is named in W001 §5.2 as a view this slice feeds.
4. **`src/CritterCab.Dispatch/Program.cs`** — same-file edit; register `IPricingClient` → `PricingClientStub` in DI. In-bounds because composition root registration is part of any vertical slice's implementation.
5. **`tests/CritterCab.Dispatch.Tests/FareQuoting/Slice52FareQuotedHappyPathTests.cs`** — one Alba integration test. The fixture extends `DispatchTestFixture` (from slice 5.1) with the `IPricingClient` stub registration.
6. **`docs/narratives/001-rider-books-a-ride.md`** — `## Document History` v0.3 entry naming this prompt and what its spec delta added. Closure-loop's fourth step (primary).
7. **`docs/workshops/001-dispatch-event-model.md`** — `## Document History` entry naming the slice 5.2 happy-path GWT confirmation. Closure-loop's fourth step (secondary).
8. **Prompt + prompts/README index entry** bundled commit.
9. **Retro + retros/README index entry** bundled commit. Retro must include `Spec delta — landed?` line — **first substantive forward exercise of the convention.**

### Definition of done

- Happy-path Alba test passes (RED → GREEN → REFACTOR walked).
- `GetFareQuote` proto generates Dispatch client and placeholder Pricing server code via `buf generate`.
- `RequestTimeline` projection assertions in the test include the `FareQuoted` event.
- Both spec-amendment Document History entries committed (narrative 001 + W001).
- All three spec-delta-named items confirmed in the retro's `Spec delta — landed?` line.
- No Claude attribution on commits or PR per established convention.

---

## Out of scope

- **`FareQuoteFailed` alternate paths.** Three GWT scenarios remain — *Transient failure with retry recovery*, *Exhausted retries*, *Non-transient failure*. All three defer to a follow-up slice 5.2 session that adds retry budget tracking. The deferred items are the next session's spec delta candidates.
- **`FareQuoteAttempts` projection.** Only matters once retry logic lands. Deferred with FareQuoteFailed paths.
- **`DispatchPolicy` view + `DispatchPolicyConfigured` event** (slice 11). Slice 5.2's happy path doesn't exercise retry config; hardcoded defaults are sufficient. Slice 11 lands the policy configuration mechanism; the slice-5.2-completion session will swap the hardcoded defaults for the view's values.
- **Real Pricing service implementation.** Pricing BC is not workshopped. This session stubs `IPricingClient` with a canned-response test double. Production implementation lands when Pricing BC is workshopped and built.
- **Candidate selection (slice 5.3) and downstream.** All future-slice implementations.
- **Cross-BC protobuf consumption pattern documentation.** A skill file capturing the IPricingClient stub-vs-real seam pattern might be warranted if it recurs; defer to a follow-up tidy session if a second cross-BC consumer surfaces a similar shape.
