# ADR-014: Azure Service Bus Topic Naming Convention

**Status:** Accepted  
**Date:** 2026-05-10

## Context

Business events crossing service boundaries travel via Azure Service Bus per ADR-005's transport-selection decision. With two bounded contexts now publishing to ASB, seven cross-BC topics exist:

- Dispatch publishes `dispatch.ride-assigned` (W001 §5.10), `dispatch.ride-request-cancelled` (W001 §5.8), and `dispatch.ride-request-abandoned` (W001 §5.9).
- Trips publishes `trips.trip-completed` (W002 §6.6), `trips.trip-cancelled-by-rider` (W002 §6.7), `trips.trip-cancelled-by-driver` (W002 §6.8), and `trips.trip-abandoned-as-no-show` (W002 §6.4).

The naming shape is consistent across all seven: `<source-bc>.<event-name-kebab>`. Workshop 001 §5.10 named the convention; Workshop 002 §6.9 inherited it without re-litigation. This ADR codifies the convention so future BCs (Identity, Payments, Pricing, Ratings) adopt it by default rather than re-deriving the choice.

The ADR also locks two operational decisions that travel with the topic name: how messages are session-keyed for ordering, and how the outbox coordinates topic publication with the local event-store append.

## Options Considered

### Option A — Per-team or ad-hoc naming

Topic names emerge organically as each BC publishes its first event. No project-wide rule; each BC owner picks names that read well to them.

This is the lowest-ceremony option and works for systems with a single owner and a small topic count. The cost shows up at consumer-side subscription configuration and at cross-BC operational tooling: a consumer subscribing to "all events from Dispatch" has no naming convention to glob against; a topic-listing tool surfaces names with no shared structure for grouping; ownership questions ("which BC owns this topic?") require lookup against external documentation. For a project explicitly designed to grow more BCs over time, the cost compounds with each addition.

### Option B — Event-name only

Topics are named after the event itself, in kebab-case: `ride-assigned`, `ride-request-cancelled`, `trip-completed`. The source BC is implicit in the event name's vocabulary (only Dispatch emits `RideAssigned`, only Trips emits `TripCompleted`).

This avoids the source-BC prefix's appearance of being a namespace marker that does not in fact scope anything — ASB topics live in a flat namespace; the `dispatch.` prefix is a string convention, not a real grouping primitive. The cost is loss of ownership signal: a topic named `ride-request-cancelled` does not announce its publisher. When two BCs evolve independently and one chooses an event name that overlaps another's vocabulary, collision is possible. Ownership-tooling that wants to surface "all topics published by BC X" has to consult external documentation rather than the topic name itself.

### Option C — `<source-bc>.<event-name-kebab>`

Topic names are `<source-bc>.<event-name-kebab>` — the source BC's slug, a literal period, then the event name in kebab-case. Examples: `dispatch.ride-assigned`, `trips.trip-completed`, `identity.rider-registered`.

The source-BC prefix makes ownership explicit at the topic name itself. Glob-style subscription patterns become natural (`dispatch.*` for all Dispatch events). Topic-listing tools group naturally by BC without external joins. The kebab-case event name matches HTTP-style conventions and the project's other naming choices (the `protobuf-contracts` skill uses kebab-case for proto file names; HTTP routes follow the same shape). Name collisions across BCs are impossible by construction.

## Decision

**Option C.** CritterCab's ASB topics are named `<source-bc>.<event-name-kebab>`.

Two operational decisions travel with the convention:

- **Session keying.** Messages are published with `SessionId = <canonical-id>` per ADR-013 — for ride-lifecycle events that means `rideRequestId` / `tripId`. ASB's session-ordered delivery then guarantees that all events for a single ride arrive at any one consumer in publish order, even when multiple consumer instances are subscribed. Across sessions (across rides), no ordering is guaranteed; consumers that need cross-ride ordering must reconstruct from event timestamps.
- **Outbox coordination.** Topic publications are queued in the same handler as the local event append, via Wolverine's `OutgoingMessages` collection. Wolverine's outbox commits the local stream append and the outbound topic message in the same transaction, so a successful ASB publish implies a successful local append (and vice versa). This pattern was established in Workshop 001 §5.5's `RideAssigned` handler and reused in Workshop 002 §6.6, §6.7, §6.8.

This applies to every ASB topic CritterCab publishes. Existing topics already follow the convention; future BCs adopt it for new topics by default.

## Consequences

Topic names are predictable from the source BC and event name without lookup. Adding a new business event to Dispatch means publishing to `dispatch.<event-name-kebab>`; consumers wanting Dispatch's events know to subscribe under `dispatch.*` without consulting documentation.

Cross-BC contracts are discoverable via topic-name conventions. A new contributor looking for "what events does Trips emit?" can list ASB topics matching `trips.*` and read the contract surface from there; ownership is unambiguous from the topic name alone.

Future BCs follow the convention without re-deciding. When Identity ships its first ASB publication (`identity.rider-registered`, `identity.rider-profile-updated`), it inherits the convention from this ADR rather than re-deriving the source-BC prefix question. The same applies to Payments (`payments.fare-settled`), Pricing (`pricing.policy-updated`), and Ratings (`ratings.ride-rated`) as those BCs land.

Session keying on the canonical ID (per ADR-013) means cross-BC ride-lifecycle event flows are session-ordered per ride. A consumer that processes both `dispatch.ride-assigned` and `trips.trip-completed` for the same ride sees the events in publication order without needing per-event timestamps to reconstruct the lifecycle. ADR-013 and ADR-014 together produce this property; neither alone is sufficient.

The convention is mechanically enforceable. ASB topic creation can be gated by a regex check on the topic name (`^[a-z][a-z0-9-]*\.[a-z][a-z0-9-]*$`); CI tooling that creates topics from declarative configuration can enforce the shape automatically. This ADR does not specify the enforcement mechanism — that is implementation territory and lands when the first BC's deployment scaffolding is built — but the convention's regular shape makes mechanical enforcement straightforward when that scaffolding arrives.
