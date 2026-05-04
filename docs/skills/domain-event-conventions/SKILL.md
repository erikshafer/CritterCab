---
name: domain-event-conventions
description: "Naming, file placement, and shape conventions for domain events in CritterCab services: past-tense naming, slim payloads, aggregate-id-first, Marten event-type registration. Use when adding an event to a service's event stream, or when reviewing event design."
cluster: core
tags: [events, event-sourcing, marten, naming, conventions, domain-modeling]
---

# Domain Event Conventions

Conventions for naming, placing, and structuring **domain events** in CritterCab services. A domain event is a fact recorded in a service's event stream — consumed by the aggregate's `Apply()` methods and by the service's own projections.

Cross-service **integration events** that cross deployment boundaries (e.g., a `TripCompleted` published over Azure Service Bus to Pricing, Payments, Ratings, and Operations) are out of scope here; their wire format and contract location are governed by ADR-005 and ADR-009. See `transport-selection` and `wolverine-azure-service-bus` (Phase 3).

## When to apply this skill

Use this skill when:

- Adding a new event to a service's Marten event stream.
- Reviewing event design in a workshop, narrative, or PR.
- Naming, placing, or shaping a domain event.
- Deciding what fields belong on the domain event vs. somewhere else.

Do NOT use this skill for:

- Designing cross-service integration events. Those have their own conventions; see `wolverine-azure-service-bus` and `transport-selection`.
- Naming gRPC service methods or proto messages. See `protobuf-contracts`.

---

## 1. Naming

Past-tense verb + noun:

```csharp
RideRequested            // ✅ verb=Requested, noun=Ride
OfferAccepted            // ✅ verb=Accepted, noun=Offer
TripStarted              // ✅ verb=Started, noun=Trip
DriverArrived            // ✅ verb=Arrived, noun=Driver
TripCompleted            // ✅ verb=Completed, noun=Trip
PaymentCaptured          // ✅ verb=Captured, noun=Payment
```

Rules:

- **No `Event` suffix.** `TripCompletedEvent` is wrong. The namespace and usage context make the role obvious.
- **Describe the fact that happened, not the command that caused it.** `TripStarted` not `StartTripRequested`. Commands use the imperative form (`StartTrip`); events use past tense (`TripStarted`).
- **Noun is the aggregate or primary entity; verb is the state transition or fact recorded.**

---

## 2. File and Namespace Placement

One event per file, named identically to the type. Events live alongside the commands, queries, and handlers that produce or consume them — organized by feature or use case, not by technical type.

```
src/CritterCab.Trips/
  Trip.cs                       // aggregate
  TripStatus.cs                 // enum
  StartTrip.cs                  // command + handler
  TripStarted.cs                // ✅ event, alongside its command
  CompleteTrip.cs               // command + handler
  TripCompleted.cs              // ✅
  CancelTrip.cs                 // command + handler
  TripCancelled.cs              // ✅
  GetTrip.cs                    // query + handler
```

For larger services, group files into feature directories (`TripExecution/`, `Cancellation/`) — never by technical type. There are no `Events/`, `Commands/`, or `Handlers/` directories anywhere in CritterCab. See `vertical-slice-organization` (Phase 2) for the broader convention.

Namespace: `CritterCab.{ServiceName}` — the service that owns the event stream.

```csharp
namespace CritterCab.Trips;

public sealed record TripStarted(...);
```

**Domain events are never published directly to other services.** When an aggregate decision produces a fact that other services need to know about, the handler emits the slim domain event for the local stream AND an integration event for the wire (over ASB). The two are different types living in different namespaces — see §5.

---

## 3. Type Shape

```csharp
// ✅ Canonical shape — from CritterCab.Trips
public sealed record TripStarted(
    Guid TripId,                              // aggregate ID first — see §4
    Guid DriverId,
    Guid RiderId,
    GeoLocation StartLocation,                // value objects from csharp-coding-standards
    DateTimeOffset StartedAt);                // DateTimeOffset — never DateTime
```

Rules:

- `sealed record` — no exceptions.
- Properties are positional `init`-only — consistent with the rest of the service.
- `DateTimeOffset` for all timestamps — never `DateTime`.
- `IReadOnlyList<T>` for collections — never `List<T>`.
- Value objects (`GeoLocation`, `LicensePlate`, `Fare`) for domain concepts with rules. See `csharp-coding-standards` § Value Object Pattern.
- No navigation properties, no methods, no behavior. Events are facts, not actors.

---

## 4. Aggregate ID Field Naming

Use `{AggregateTypeName}Id` — unambiguous when events are read in isolation by projections, handlers, or async daemons:

```csharp
// Aggregate type: Trip → ID field: TripId
public sealed record TripCompleted(
    Guid TripId,              // ✅
    DateTimeOffset CompletedAt);

// NOT:
public sealed record TripCompleted(
    Guid Id,                  // ❌ — ambiguous when read in projections or handlers
    DateTimeOffset CompletedAt);
```

For aggregates whose ID name doesn't trivially derive from the type name, use the domain-meaningful name. `RideRequest` aggregate's ID is `RideRequestId` (`RequestId` would collide too easily with other request-shaped concepts in the system).

---

## 5. Slim Domain Events vs. Rich Integration Events

**This is the most important convention in this document.**

Domain events carry only the data needed to reconstruct aggregate state. They live in the service's event stream (Marten) and are consumed only inside the service that owns them.

Integration events that cross service boundaries (ASB) carry richer payloads sized for downstream consumers. They are a separate concern with their own conventions — see `transport-selection` and `wolverine-azure-service-bus`.

```csharp
// CritterCab.Trips.Events — slim domain event in the Trip stream
public sealed record TripCompleted(
    Guid TripId,
    DateTimeOffset CompletedAt);
// Just enough for the Trip aggregate to transition to Completed state in Apply().

// The same handler ALSO publishes an integration event over ASB to Pricing,
// Payments, Ratings, and Operations. That integration event carries the rich
// payload — final fare, distance, route waypoints, driver and rider IDs — that
// downstream services need. It lives in a different namespace, has different
// shape, and is governed by the cross-service contract conventions, not this
// skill.
```

**Rationale:**

- Keeps the event stream compact — only state-reconstruction data lives in the stream.
- Prevents downstream services from coupling to aggregate internals.
- Allows the integration event to evolve independently from the domain event (different versioning cadences).

**Implication:** the aggregate must hold sufficient state at the moment of the integration event to construct its richer payload. If `TripCompleted` (integration) needs `FareTotal`, `DistanceMeters`, and route waypoints, the `Trip` aggregate must already have those fields populated by the time `CompleteTrip` runs. **Plan aggregate fields and integration event fields together when modeling.**

The reverse is also a convention: **don't put fields on the domain event just because the integration event needs them.** The domain event is the contract for the aggregate's `Apply()` method, not for the wire. If a field isn't needed to rebuild aggregate state, it doesn't belong on the domain event.

---

## 6. Marten Event Type Registration

Every domain event that appears in a Marten event stream must be registered in the service's `ConfigureMarten()` call:

```csharp
// In the Trips service — Program.cs or the service's bootstrap module
services.ConfigureMarten(opts =>
{
    opts.Events.AddEventType<RideRequested>();
    opts.Events.AddEventType<OfferAccepted>();
    opts.Events.AddEventType<DriverArrived>();
    opts.Events.AddEventType<TripStarted>();
    opts.Events.AddEventType<TripCompleted>();
    opts.Events.AddEventType<TripCancelled>();
});
```

`UseMandatoryStreamTypeDeclaration` is set globally per service. Omitting a registration causes **silent `null` returns** from `AggregateStreamAsync<T>` for streams that include the unregistered event type — no compile-time error, no startup error. Register every event type in the same commit that introduces it.

See `service-bootstrap` for the per-service `ConfigureMarten()` call site convention (Phase 2).

---

## 7. Naming Collisions Between Domain and Integration Events

When a domain event and an integration event share the same simple name (e.g., both called `TripCompleted` in different namespaces), alias the integration event in the handler file:

```csharp
// In CompleteTripHandler.cs — handler in the Trips service
using TripCompletedIntegration = CritterCab.Trips.Integration.TripCompleted;

// Handler references both without ambiguity:
events.Add(new TripCompleted(trip.Id, time.GetUtcNow()));                          // domain event
outgoing.Add(new TripCompletedIntegration(trip.Id, trip.RiderId, /* ... */));      // integration event
```

In configuration files (`Program.cs`, transport setup), use fully qualified names rather than aliases to keep the source namespace visible at the registration site:

```csharp
opts.PublishMessage<CritterCab.Trips.Integration.TripCompleted>()
    .ToAzureServiceBusTopic("trips.events");
```

The exact namespace and project organization for integration events is governed by ADR-009 and the cross-service contract conventions. Until those are finalized, treat the namespace `CritterCab.{ServiceName}.Integration` as a placeholder.

---

## 8. Enum Types That Appear in Events

Enum types used in domain events are defined in the service's own namespace. They never cross service boundaries:

```csharp
// src/CritterCab.Trips/TripStatus.cs — ✅ service-owned enum
namespace CritterCab.Trips;

public enum TripStatus
{
    Requested, Assigned, EnRoute, Arrived, InProgress, Completed, Cancelled
}
```

If a downstream service needs to interpret a categorical field, the integration event carries a `string` representation — not the enum:

```csharp
// In an integration event payload (not in this skill's scope, but illustrative):
string Status,   // ✅ "Completed" or "Cancelled" — consumers define their own enum if needed
```

**Cross-service enum sharing is not permitted.** Per ADR-002, services share no application-layer code; an enum used by both the producer and a consumer is application-layer code. The string-on-the-wire convention is the alternative.

The same rule applies to value objects in events. `GeoLocation` is fine inside the Trips service's own domain events because Trips owns both the producer and consumer. If a `GeoLocation`-equivalent appears on an integration event, the wire representation is a primitive shape (e.g., two doubles, or a protobuf message defined in `/protos/`) — not the C# `GeoLocation` type from any one service.

---

## See also

**Upstream** — load these first if unfamiliar:

- `csharp-coding-standards` — sealed records, init-only properties, DateTimeOffset, value objects, required properties.

**Downstream** — natural follow-ups:

- `vertical-slice-organization` — file and directory organization within a service: feature-based, not type-based (Phase 2).
- `marten-aggregates` — how these events are consumed by `Apply()` methods to rebuild aggregate state (Phase 2).
- `marten-wolverine-aggregates` — handler patterns that produce these events (Phase 2).
- `wolverine-handlers` — handler shape and validation pipeline; messaging-specific routing patterns for cross-service publication (Phase 2).
- `transport-selection` — cross-service integration events and which transport carries them (Phase 3).
- `wolverine-azure-service-bus` — integration event publishing patterns over ASB (Phase 3).

**External:**

- ai-skills `marten-aggregate-handler-workflow` — generic handler-events-aggregate cycle. Install via `npx skills add` (license required).
- ADR-005 in [`docs/decisions/`](../../decisions/) — transport selection (governs cross-service integration events).
- ADR-009 in [`docs/decisions/`](../../decisions/) — protobuf contracts (governs cross-service wire format).
- [`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md) — service-boundary rules underlying the slim/rich split.
