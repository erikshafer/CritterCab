# Post Slice 5.3 Handoff — 2026-06-16

> **Purpose:** Session-handoff note after implementing Dispatch slice 5.3 (CandidatesSelected). Captures what landed, outstanding debt, and the next-session candidate. Disposable once the next session orients.

---

## What landed (PR #34, squash-merged `bda8363`)

**Slice 5.3 — CandidatesSelected + NoCandidatesAvailable.** Full vertical slice for the first step of the dispatch-round arc.

Key deliverables:
- `CandidateSelectionAutomation` — static Wolverine handler reacting to `FareQuoted` (via `UseFastEventForwarding`), loading `RideRequest` via `[WriteAggregate(nameof(FareQuoted.RideRequestId))]` (first use of this attribute on a non-first stream event in the codebase), returning `ICandidateSelectionOutcome`
- `CandidatesSelected` + `NoCandidatesAvailable` events + `ICandidateSelectionOutcome` marker interface (third marker-interface instance in Dispatch)
- `INearbyAvailableDriversSource` + `NearbyAvailableDriversStub` — external seam for nearby driver data (parking-lot #4 deferred: no real data source yet)
- `DispatchPolicySnapshot` — DI record with `Default` sentinel; name deliberately reserves `DispatchPolicy` for Slice 11
- `RequestRoundsProjection` — `partial class SingleStreamProjection<RequestRounds, Guid>`; tracks per-stream round history; consumed by Slice 9 re-dispatch
- `RequestTimelineProjection` extended for both outcome events
- Three GWT-aligned Alba tests (`Slice53CandidatesSelectedTests.cs`), one per W001 §5.3 GWT
- Slice 5.2 tests updated for chain extension (automation now appends a 3rd event after every `FareQuoted`)
- Spec-delta closure-loop: narrative 002 v0.2 + W001 v0.7 `## Document History` entries committed

Full artifact list: [`docs/retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md`](../retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md)

---

## Current repo state

- **Branch:** `main`
- **HEAD:** `bda8363` — `feat(dispatch): slice 5.3 CandidatesSelected + NoCandidatesAvailable (#34)`
- **Working tree:** clean
- **Stale branch:** `slice/5-3-candidates-selected` deleted (squash-merge confirmed)
- **Test count (Dispatch):** 11 passing — 1 smoke + 3 slice 5.1 + 1 slice 5.2 happy + 3 slice 5.2 failure/transient + 3 slice 5.3

---

## Outstanding items from retro 005

These are carried forward from [`docs/retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md`](../retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md) — reference it for full rationale.

### Skill-file debt at threshold — prioritize

| Gap | Notes |
|---|---|
| **Marker-interface union return type** (`ICandidateSelectionOutcome` / `IFareQuoteOutcome`) | Pattern has appeared **3 times** — threshold for encoding was set at 3. Goes in `wolverine-handlers` or a new `wolverine-marten-automation` skill. Do this in a `tidy: skills` PR before slice 5.4. |
| **Event-triggered automation shape** (`Handle(DomainEvent @event, [WriteAggregate(...)] Aggregate, ...)`) | Two automations use this shape. A third will solidify it. Encodes alongside the marker-interface pattern. |

### Carried housekeeping (pre-existing, not introduced by slice 5.3)

| Item | Notes |
|---|---|
| **Workshop §5.2 Reads list inconsistency** | Carried from retro 004. Not this session's scope. |
| `"CritterBids API"` Swagger title in `Program.cs:94` | Pre-existing; rename to `"CritterCab Dispatch API"`. Queue for a `tidy: housekeeping` PR. |
| **Bundling-rule encoding** | Past the encoding threshold; still queued for a `tidy:` session. |

---

## Verified knowledge from this session

**`[WriteAggregate]` on a non-first stream event works in Wolverine 5.39+.** The jasperfx-source-verifier confirmed that `FindIdentity` is trigger-agnostic — it resolves the stream ID from the named property and calls `FetchForWriting<T>(id)` regardless of stream position. No guards, no workarounds needed. Pattern transfers directly from `FareQuoteAutomation` to `CandidateSelectionAutomation`.

**`ForwardingNearbyDriversSource` indirection pattern** (same shape as `ForwardingPricingClient` from slice 5.2) — stable singleton wraps a `Func<INearbyAvailableDriversSource>` pointing at the fixture property. Per-test stub swaps without host rebuild. Ready to copy for slice 5.4's external seam.

**Extend-chain test updates are in scope** when a new automation reacts to an event already present in the test baseline — `events.Count` assertions in upstream tests must be updated. This is a necessary consequence, not an opportunistic edit. The same issue will recur in slice 5.4 tests that set up `CandidatesSelected` as a precondition.

---

## Next-session candidate

**Slice 5.4 — OfferSent (fan-out).** Dispatch-round arc continuation. `OfferSentAutomation` reacts to `CandidatesSelected` and emits one `OfferSent` event per `CandidateEntry` (fan-out). This is the first fan-out automation in Dispatch.

The implementation prompt does not yet exist. Before writing the prompt:
1. Confirm W001 §5.4 GWTs are sufficient or add gap scenarios
2. Consider whether `OfferSent` needs a marker interface (probably yes — Slice 5.5 consumes it and may diverge)
3. `INearbyAvailableDriversSource` stub from slice 5.3 will need to be set up in slice 5.4 tests too (it is now always triggered)

**Alternative: `tidy: skills` PR first.** The skill-file debt above is at threshold. Encoding the marker-interface pattern and the automation handler shape before slice 5.4 is low-risk and will sharpen the critter-skill-auditor's Phase 1 audit for the next implementation session.

---

## Marten 9 / Wolverine 6 migration note

The repo is on the 2026 package line but migration is incomplete. See memory note: `project_marten9_wolverine6_migration_in_progress.md`. Conventional projections must be `partial` (no runtime codegen) — `RequestRoundsProjection` was authored correctly. Broader v9/v6 audit + version doc-drift + Grpc/Polecat 5.38-vs-6.8 still owed.

---

## Suggested skills for next session

| Skill | When |
|---|---|
| `/critter-skill-auditor` | Phase 1 before any implementation; Phase 2 after. |
| `/critter-test-architect` | When scaffolding slice 5.4 tests — fan-out pattern (one event per candidate) is new in Dispatch. |
| `/jasperfx-source-verifier` | If slice 5.4 uses a new Wolverine API surface for fan-out (e.g., `PublishAsync` vs return collection vs `IEnumerable<object>` return type). |
| `grill-me` / `/grill-me` | Before writing the slice 5.4 prompt if the fan-out shape is uncertain. |

---

## Artifacts inventory (updated)

| Layer | Count | Notes |
|---|---|---|
| Workshops | 2 | Dispatch (v0.7), Trips (in design) |
| Narratives | 2 | 001 rider books a ride, 002 driver accepts a ride (v0.2) |
| Skills | 39 (local) | Skill-file debt at threshold — see above |
| ADRs | 16 committed | Last: ADR-016 Frontend Live-Update Transport |
| Prompts (complete) | 8 impl + others | 005 marked Complete |
| Retrospectives | 8+ | retro 005 now indexed |
| Service projects | 1 | Dispatch (11 tests, 3 slices) |
| Test projects | 1 | 11 tests passing |

---

## Document history

- **2026-06-16.** Authored at session close after slice 5.3 completion and `/post-merge` confirming `bda8363` on `main`.
