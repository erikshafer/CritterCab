# ADR-012: Aggregate-per-Invariant

**Status:** Accepted  
**Date:** 2026-05-10

## Context

CritterCab models its bounded contexts using DDD aggregates as the unit of consistency. The question of how to draw aggregate boundaries — when does a noun in the domain language deserve its own aggregate, and when does it belong inside another aggregate's stream? — has surfaced in two workshops with consistent reasoning across both.

Workshop 001 §5.5 modeled `RideRequest` as a single aggregate with `Offer` as a sub-entity. The invariant under protection is *at-most-one `OfferAccepted` per request*: the moment one offer is accepted, all sibling offers must be revoked, and no concurrent acceptance can succeed. By keeping all offer-lifecycle events (`OfferSent`, `OfferAccepted`, `OfferDeclined`, `OfferExpired`, `OfferRevoked`) on the `RideRequest` stream, Marten's optimistic concurrency on the stream version enforces the invariant natively — no distributed locking, no cross-stream coordination.

Workshop 002 §3 modeled `Trip` as a single aggregate with driver-progression states as sub-states (status enum plus nullable transition timestamps), not sub-entities. The invariant under protection is *lifecycle-monotonicity*: the Trip walks a strict state machine (matched → en-route-to-pickup → at-pickup → in-progress → completed) with terminal off-ramps to cancellation and no-show. No skipping, no reversing. Marten optimistic concurrency on the Trip stream enforces the invariant: every state-transition handler loads the aggregate, validates the proposed transition against current state, and the version check rejects concurrent attempts.

Two BCs, two different invariants, the same modeling maneuver. The aggregate boundary in both cases is drawn around *the invariant being protected*, not around a noun in the domain language. This ADR codifies that as the canonical pattern for CritterCab.

## Options Considered

### Option A — Aggregate-per-noun

Every domain noun is its own aggregate. `RideRequest` and `Offer` would each have their own stream; `Trip` and `Pickup` and `TripExecution` would each have their own stream. Cross-noun invariants (at-most-one acceptance, lifecycle monotonicity) are enforced by coordinating across streams: distributed locks, two-phase commits, or saga-pattern compensations after the fact.

This is the default modeling instinct when DDD is taught at the noun-by-noun level — "what are the entities?" precedes "what are the invariants?" — and it produces aggregates that map cleanly to ER-style data models. The cost appears at the invariant boundary: enforcing "at most one `OfferAccepted` per `RideRequest`" across separate `Offer` and `RideRequest` aggregates requires holding a lock against every outstanding offer or running compensation logic to revoke siblings retroactively. Both add complexity that does not exist in the chosen design. For Trips specifically, an aggregate-per-noun split would draw `Pickup` and `TripExecution` as separate aggregates referenced by `tripId`; lifecycle-monotonicity then requires each handler to query peer aggregates to validate that the prior state has been reached — a cross-stream read for every transition.

### Option B — Aggregate-per-invariant

The aggregate boundary is drawn around the invariant being protected. Sub-things below the aggregate boundary are expressed in one of two ways:

- **Sub-entities** when they have identity worth tracking separately. Workshop 001's `Offer` has its own `offerId`, its own lifecycle (sent / accepted / declined / expired / revoked), and its own rejection reasons. Events for it append to the parent `RideRequest` stream rather than living on a separate `Offer` stream.
- **Sub-states** when the sub-thing is a phase of the parent rather than a distinct entity. Workshop 002's driver-progression states are status-enum values plus nullable transition timestamps recorded on the Trip aggregate itself, not separate entities with independent lifecycles.

Both are valid expressions of the pattern. The choice between them is whether the sub-thing has identity worth tracking separately: sub-entity reference IDs are local to the aggregate's stream (`offerId` lives on `OfferSent` events on the `RideRequest` stream); sub-state values live as fields on the parent. Marten's optimistic concurrency on the parent stream protects the invariant natively in both cases — no distributed locks, no cross-stream coordination, no saga compensations.

## Decision

**Option B.** CritterCab draws aggregate boundaries around invariants, not nouns.

When a new aggregate is being modeled, the first question is "what is the invariant this aggregate exists to protect?" — not "what entities are involved?" The invariant determines the boundary; the entities and states organize themselves under that boundary as either sub-entities (with their own identity, sharing the parent's stream) or sub-states (status enum plus nullable timestamps on the parent).

This applies wherever Marten's stream-version optimistic concurrency is sufficient to protect the invariant — which covers every case that has surfaced so far in CritterCab. Aggregates with invariants that genuinely span streams or BCs (e.g., a payment that must coordinate with a trip across the Payments / Trips boundary) are out of scope for this pattern. Those cases remain saga territory and are handled by Wolverine's saga support, not by extending the aggregate boundary across BCs.

## Consequences

The design effort for each new aggregate moves from "name the entities" to "name the invariants." For modeling discipline, the question "what is this aggregate's load-bearing invariant?" is asked first; the entities and states fall out of the answer. This is the modeling move Workshop 001 §12.6 already recommended for future workshops ("pre-workshop sidebar on aggregate identity") and that Workshop 002 §3 carried out before slice walking; the discipline is now ADR-backed rather than convention-only.

Cross-stream coordination is avoided for invariants that fit within a single aggregate. There is no `TripExecution` aggregate to query when the Trip transitions to `InProgress`; there is no `Offer` aggregate to lock when an `AcceptOffer` command arrives. The reads and writes the invariant requires happen on the parent stream, and Marten's optimistic concurrency does the rest.

Sub-entity reference IDs are local to the parent's stream. `offerId` appears on `OfferSent` events appended to the `RideRequest` stream — there is no separate `Offer` stream to query. Code that needs to find an offer by ID either reads the `RideRequest` stream and locates the offer events, or queries a projection that indexes offers by ID. The index is a projection, not a stream.

The pattern is silent on cases where invariants genuinely span aggregates or bounded contexts (cross-BC coordination, multi-aggregate transactions). Those cases fall to other patterns — sagas, process managers, eventual-consistency reconciliation — and warrant separate ADRs as they arise.
