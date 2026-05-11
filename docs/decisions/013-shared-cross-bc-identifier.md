# ADR-013: Shared Cross-BC Identifier

**Status:** Accepted  
**Date:** 2026-05-10

## Context

CritterCab's bounded contexts handle different stages of the same logical ride. Dispatch creates the ride request; Trips runs the post-acceptance lifecycle; Payments settles the fare; Ratings collects feedback. The same logical ride flows across these BCs as different aggregates with different lifecycles, but the *identity* of the underlying ride is the same throughout.

Two workshops have now confirmed how this identity is represented at the boundary. Workshop 001 §5.10's `RideAssigned` event — Dispatch's handoff payload to Trips — carries `rideRequestId` (= `tripId`); the equality is explicit on the event. Workshop 002 §3.6 keys the Trip aggregate's stream on `tripId = rideRequestId`, treating the value as flowing in from Dispatch unchanged. Workshop 002's UL entry for "Trip ID" names it as "the canonical opaque identifier shared with Dispatch's `rideRequestId`, Pricing's payment reference, and Ratings' ride ID" — the same value is expected to appear in every BC participating in this lifecycle.

The question this ADR settles is whether that shared-identity assumption is canonical project-wide policy or a per-BC choice. Codifying it now, before the third BC adopts the pattern (Pricing, Ratings, or Payments — whichever lands first), prevents per-BC drift on cross-BC identity continuity.

## Options Considered

### Option A — Per-BC IDs with cross-reference tables

Each BC mints its own internal ID for the entity it owns (`rideRequestId` for Dispatch, `tripId` for Trips, `paymentId` for Payments, `ratingId` for Ratings). Cross-BC references travel through a mapping artifact: an event payload field carrying the upstream ID, plus a projection or lookup table that joins the per-BC IDs.

This is the default modeling instinct when each BC owns its data — every aggregate gets its own ID space, and cross-references become explicit foreign keys. The cost appears at every cross-BC query: a question like "show me the full lifecycle of ride X across Dispatch, Trips, Payments, and Ratings" requires four lookups, each translating the ID through a mapping. Cross-BC analytics projections (e.g., a `CrossBcRideTimeline` showing the full lifecycle of a single ride) become coordination-heavy: every projection must maintain its own per-BC ID joins, and a missed mapping anywhere breaks the cross-BC query.

### Option B — Shared canonical identifier

The same opaque value flows across every BC that participates in a single ride's lifecycle. Dispatch mints the ID at `SubmitRideRequest`; every downstream BC receives the same value on the wire and uses it as its own primary stream key. Cross-BC queries are direct lookups on the same key in each BC's storage — no mapping table, no translation step.

The format is **UUID v7** — opaque at the application level (no BC reads structure into the value), but time-ordered at the byte level so index locality on append-heavy event streams improves over UUID v4. UUID v7 is supported by every storage system in the stack: PostgreSQL via Marten, SQL Server via Polecat, Kafka message keys, ASB session IDs. The protobuf wire representation is governed by ADR-009.

## Decision

**Option B.** CritterCab uses a single shared canonical identifier per lifecycle, flowing across every BC that participates in that lifecycle.

The first BC to mint the ID — Dispatch, at `SubmitRideRequest` — commits to the wire format every downstream BC accepts (UUID v7). Downstream BCs inherit the ID rather than mint their own; their own primary stream keys are this same value, exposed under per-BC field names (`rideRequestId` in Dispatch, `tripId` in Trips, payment-reference-equivalent in Payments, ride-ID-equivalent in Ratings). The field name is BC-owned, in keeping with the project's discipline of BC-local vocabulary at API surfaces; the value is shared.

This applies to lifecycles whose stages cross BCs in a deterministic order. For lifecycles internal to a single BC (Identity user IDs, Operations admin action IDs, Telemetry's own ping IDs), per-BC IDs remain appropriate — this ADR governs *cross-BC* identity continuity, not ID generation in general.

## Consequences

Cross-BC queries on a single ride are direct lookups on the same key in each BC's storage. A `CrossBcRideTimeline` projection joining Dispatch's `RideRequest` events with Trips' `Trip` events with Payments' settlement events uses the same canonical ID across all three reads. The mapping table that would otherwise mediate these joins does not exist.

Future BCs joining this lifecycle inherit the ID rather than minting their own. When Pricing is actively modeled, when Ratings is added, when Payments lands — each receives the canonical ID on its inbound contracts and uses it as its own primary stream key. The ID's wire format is fixed once (here, by Dispatch); changing it later is a coordinated multi-BC migration.

The cost is a one-way coupling on ID format: every BC accepting the canonical ID must accept whatever Dispatch mints. UUID v7 is chosen specifically to minimize this coupling — opaque at the application level so no BC reads structure into the value, time-ordered at the byte level for index locality on event streams, and supported natively by every storage system in the stack. If a future BC requires a different ID format for internal purposes, it may derive its own internal key but must continue to accept and store the canonical ID for cross-BC traceability.

The pattern is silent on lifecycles internal to a single BC. Identity's user IDs, Operations' admin action IDs, Telemetry's per-ping IDs, and other intra-BC identifiers remain BC-owned; this ADR governs only cross-BC identity continuity for shared lifecycles.
