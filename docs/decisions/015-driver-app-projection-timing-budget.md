# ADR-015: Driver-App Projection Timing Budget

**Status:** Accepted  
**Date:** 2026-05-10

## Context

The driver-app trip-mode transition is the most operationally sensitive moment of a driver's shift: the round-trip from tapping Accept on an offer to the device rendering the trip-mode UI. When a driver taps Accept (Workshop 001 §5.5's `AcceptOffer`), Dispatch atomically commits `OfferAccepted` + `RideAssigned` + sibling `OfferRevoked` events + an outbound ASB publication on `dispatch.ride-assigned`. Trips' Translation-in handler receives the message, idempotently creates the Trip aggregate, and updates the `DriverTripView` projection inline (Workshop 002 §6.1). The driver-app's gRPC server-streaming subscription to `DriverTripView` pushes the trip-mode payload to the device, which renders the trip-mode UI.

Narrative 002 Moment 2 named the budget for the full round-trip as *subjectively instantaneous* — the driver should experience no visible lag between the tap and the trip-mode UI. A driver who experiences a one-second pause doubts the system received the tap; a driver who experiences a three-second pause stops trusting the offer-acceptance flow. Forward-constraint #3 from narrative 002 (W002 §13) carried this requirement into the Trips workshop, where slice 6.1 honored it by locking the projection mechanism but deferring the explicit ADR.

Workshop 002 §6.1 already settled the mechanism: `DriverTripView` is an **inline** Marten projection (same transactional commit as the `TripMatched` event append); other projections (`TripTimeline`, `ActiveTripsByDriver`, `ActiveTripsByRider`) run on Marten's async daemon since they are not on the latency-critical path. What this ADR settles is whether the timing budget remains qualitative judgment or becomes a measurable SLO — and if measurable, what number operationalizes "subjectively instantaneous."

## Options Considered

### Option A — No numeric target; qualitative budget only

The budget exists as design intent ("subjectively instantaneous"); operational tooling and review judgment carry it forward. No SLO number is committed.

This is the lowest-ceremony option and avoids the trap of picking a number that turns out to be wrong for the actual system. The cost is that "subjectively instantaneous" is unmeasurable: there is no breach signal, no regression detection, no observability anchor that can fire when latency drifts. Performance regressions surface only through user complaints or operational incidents — both lagging signals that arrive after drivers have already lost trust in the flow.

### Option B — Hard single numeric target (p95 < 200ms, single-region for v1)

The SLO is committed as a single hard number applied to the v1 single-region deployment: **p95 of the round-trip latency must be under 200ms**, measured as the interval between Dispatch's `RideAssigned` commit (timestamp `assignedAt`, carried into `TripMatched` as `dispatchAssignedAt`) and Trips' `TripMatched` commit (timestamp `matchedAt`). Workshop 002 §6.1 already preserves both timestamps on every `TripMatched` event; the `MatchingLatencyMetrics` projection from §6.1's candidate projections table feeds the SLO measurement.

The 200ms number sits within the industry-referenced range for tap-to-render UI transitions (typically p95 < 100–250ms) and is achievable with the locked mechanism: inline projection + ASB low-latency mode + gRPC server-streaming push at single-region scale. Multi-region targets are post-MVP and are parked rather than committed here; when cross-region deployment becomes a live concern, this ADR is amended or superseded.

### Option C — Tiered target with multi-region from the start (p95 < 200ms in-region, p95 < 500ms cross-region)

The SLO is committed as a tier that anticipates post-MVP multi-region deployment. The cross-region tier becomes measurable from day one.

The cost is committing to a multi-region target before the multi-region deployment exists. The 500ms cross-region number is speculative — until real cross-region traffic exists, the number has no measurement basis and may turn out to be wrong (too tight or too loose) when the cross-region path is actually exercised. Locking the tier prematurely commits design effort to satisfying a number that has not been pressure-tested.

## Decision

**Option B.** CritterCab commits to **p95 < 200ms** for the round-trip from Dispatch's `RideAssigned` commit to Trips' `TripMatched` commit, applied to the v1 single-region deployment.

The mechanism is locked (already from Workshop 002 §6.1):

- `DriverTripView` is an **inline** Marten projection — runs in the same transactional commit as the `TripMatched` event append.
- All other Trips projections run on the **async** Marten daemon. The latency-critical path carries only one projection.
- Cross-BC publication uses ASB session-keyed delivery per ADR-014 with low-latency mode enabled; gRPC server-streaming pushes the projection update to the driver-app device.

The observability hook is the `MatchingLatencyMetrics` projection from Workshop 002 §6.1's candidate projections table, measuring the `dispatchAssignedAt → matchedAt` interval distribution per trip. **Its status is promoted from *defer but pin* to *must-author when Trips' implementation starts*** — the SLO needs the projection to be measurable from day one.

Multi-region targets are explicitly deferred. When cross-region deployment lands on the roadmap, this ADR is amended or superseded with a tiered target informed by actual cross-region measurements rather than speculation.

## Consequences

The SLO is measurable rather than aspirational. The `MatchingLatencyMetrics` projection produces a continuous signal; SLO breaches surface in operational dashboards and alerting before they reach the lagging signal of user complaints or trust erosion. This is the property Workshop 002 §6.1 named ("forward-constraint #3 timing-budget observability") and that Option A would not deliver.

Future projection-design work on latency-critical surfaces inherits the same discipline. When the rider-app live-tracking view is designed (post-MVP), or when any other UI surface becomes latency-critical, the same shape applies: name the round-trip endpoints; pick or reuse a numeric target; commit the observability projection alongside the design. ADR-015 is the template; future latency NFRs follow it.

The single-region scope is honest about what v1 commits to. Multi-region deployment introduces network paths whose latency characteristics are not known until measured; committing now to a cross-region number would be speculation. The deferral keeps the ADR's claim within evidence — single-region p95 < 200ms is achievable with the locked mechanism; cross-region is a separate NFR conversation when its time comes.

A skill file codifying inline-vs-async projection placement decisions (latency criticality threshold, server-streaming push pattern, Marten inline-projection idioms with Wolverine handlers) is identified as a follow-up. It is parked, not authored in this session, and is recorded for the DEBT.md ledger.

ADR-015 is the first cross-cutting NFR ADR in CritterCab. Future cross-cutting NFRs (availability targets, max ride-request latency, payment-settlement deadline) follow the same shape: name the user experience the NFR exists to deliver; name the measurement endpoints; commit a numeric target with explicit scope (single-region, single-tenant, etc.); promote the observability hook from "defer but pin" to "must-author." The pattern ADRs in this bundle (011–014) codified existing patterns observed across two BCs; ADR-015 codifies a *forward-looking* discipline that future ADRs of its kind will inherit.
