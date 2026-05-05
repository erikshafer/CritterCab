---
name: marten-aggregates
description: "Designing event-sourced aggregates with Marten in CritterCab — sealed-record aggregate shape, static Create/Apply methods, stream identity from event metadata, and the decider-pattern split between handler decisions and aggregate evolution. Use when implementing or reviewing any event-sourced aggregate."
cluster: marten
tags: [marten, aggregates, event-sourcing, decider-pattern, a-frame, immutable, fp]
---

# Marten Aggregates

Designing and implementing event-sourced aggregates with Marten in CritterCab. The aggregate is a `sealed record` that folds events over itself via static `Apply` methods. Decisions live in handlers; the aggregate is pure state + evolution. This is the decider pattern as it lands natively in the 2026 Critter Stack.

The generic Marten event-sourcing mechanics — schema, event store internals, snapshot APIs, projection registration — are documented authoritatively in JasperFx ai-skills. **This skill documents the aggregate shape and the project-specific decisions that govern aggregate design in CritterCab.** Handler patterns that produce events live in `marten-wolverine-aggregates`; projections live in `marten-projections`.

## When to apply this skill

Use this skill when:

- Designing a new event-sourced aggregate.
- Adding `Apply` methods for new domain events.
- Reviewing aggregate design in PRs or workshops.
- Choosing between event-sourced and document-stored data for a domain concept.
- Naming aggregate fields or deciding what state belongs on the aggregate.

Do NOT use this skill for:

- Handler patterns that load and produce events (`[WriteAggregate]`, `MartenOps.StartStream`, multi-stream handlers) — see `marten-wolverine-aggregates` (Phase 2).
- Inline and async projections — see `marten-projections` (Phase 2).
- LINQ queries against documents and event stores — see `marten-querying` (Phase 2).
- Async daemon configuration — see `marten-async-daemon` (Phase 2).
- DCB-tagged event writes — see `dynamic-consistency-boundary` (Phase 2).
- Polecat aggregates — see `polecat-event-sourcing` (Phase 4); the patterns mirror Marten's but the API surface differs.

---

## When to Reach for an Event-Sourced Aggregate

Not every domain concept earns an aggregate. Event sourcing carries real cost — every state question requires hydration from the stream, schema evolution requires versioning, and the operational surface (async daemon, projections, snapshots) is meaningful. Use it when the value justifies the cost.

**Reach for an event-sourced aggregate when:**

- The history of state changes has business meaning. *Trip*, *RideOffer*, *DriverApplication* — the sequence of events IS the artifact.
- Multiple downstream consumers need to react to state changes. Trips emits events that Pricing, Payments, Ratings, and Operations consume.
- Audit trail and time-travel are real requirements, not aspirational ones.
- The aggregate has a clear lifecycle with well-defined start and end events.

**Reach for a document instead when:**

- The current state is what matters; history is incidental. *DriverProfile* (mostly static, occasionally edited), *RiderPreferences*, *VehicleRegistry* often fit better as documents.
- The data is reference/lookup-shaped — you read it more than you write it, and writes are full replacements.
- There's no business meaning to "how did we get here" — only "what is the answer now."

When in doubt, prefer documents. Promoting a document to an event-sourced aggregate later is straightforward; demoting an aggregate to a document means losing the audit trail.

---

## Canonical Aggregate Shape

This is the modern Critter Stack idiom. Every event-sourced aggregate in CritterCab follows this shape.

```csharp
using System.Collections.Immutable;
using Marten.Events;

namespace CritterCab.Trips;

public sealed record Trip(
    Guid Id,
    Guid DriverId,
    Guid RiderId,
    GeoLocation StartLocation,
    GeoLocation? EndLocation,
    TripStatus Status,
    ImmutableList<Waypoint> Waypoints,
    Fare? FinalFare,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt)
{
    // Create — invoked when the first event in a stream is persisted.
    // Wolverine assigns the stream ID at append time; we read it from metadata.
    public static Trip Create(IEvent<TripStarted> @event) =>
        new(
            Id: @event.StreamId,
            DriverId: @event.Data.DriverId,
            RiderId: @event.Data.RiderId,
            StartLocation: @event.Data.StartLocation,
            EndLocation: null,
            Status: TripStatus.InProgress,
            Waypoints: ImmutableList<Waypoint>.Empty,
            FinalFare: null,
            StartedAt: @event.Data.StartedAt,
            CompletedAt: null,
            CancelledAt: null);

    // Apply — folds subsequent events over the current aggregate.
    public static Trip Apply(WaypointRecorded @event, Trip current) =>
        current with
        {
            Waypoints = current.Waypoints.Add(new Waypoint(@event.Location, @event.RecordedAt))
        };

    public static Trip Apply(DriverArrived @event, Trip current) =>
        current with
        {
            Status = TripStatus.Arrived
        };

    public static Trip Apply(TripCompleted @event, Trip current) =>
        current with
        {
            Status = TripStatus.Completed,
            EndLocation = @event.EndLocation,
            FinalFare = @event.FinalFare,
            CompletedAt = @event.CompletedAt
        };

    public static Trip Apply(TripCancelled @event, Trip current) =>
        current with
        {
            Status = TripStatus.Cancelled,
            CancelledAt = @event.CancelledAt
        };
}
```

Six conventions visible above:

- **`sealed record` with a positional constructor.** All state captured in one place; mutations go through `with`.
- **`Create` is `static`, takes `IEvent<TFirstEvent>`, returns the aggregate.** This is the "evolve" function for the first event in the stream.
- **Stream ID comes from `@event.StreamId`**, not from a command. Wolverine assigns the stream ID when the first event is persisted; we read it from metadata.
- **`Apply` methods are `static`, take `(TEvent @event, Trip current)`, return the new aggregate via `with`.** This is the "evolve" function for subsequent events.
- **`Immutable*` collections for state**, `IReadOnly*` collections on the events themselves. See `csharp-coding-standards` § Collection Patterns for the rationale.
- **No business validation, no decision methods, no behavior on the aggregate.** Just state and evolution. Decisions are the handler's job (see `marten-wolverine-aggregates`).

---

## The Decider Pattern in Critter Stack Idiom

The decider pattern (Jérémie Chassaing's framing) splits a state machine into two pure functions:

- **`decide(command, state) → events`** — the handler. Takes the current aggregate state and a command, returns the events that should be appended.
- **`evolve(event, state) → state`** — the aggregate. Takes the current state and an event, returns the new state.

CritterCab's Wolverine + Marten integration realizes this split natively:

| Decider concept | CritterCab realization |
|---|---|
| `decide(command, state) → events` | A handler with `[WriteAggregate(nameof(Cmd.SomeId))]` parameter that returns the event(s) to append. See `marten-wolverine-aggregates`. |
| `evolve(event, state) → state` for first event | `static Trip Create(IEvent<TripStarted> @event)` |
| `evolve(event, state) → state` for subsequent events | `static Trip Apply(SomeEvent @event, Trip current)` |
| Initial state | The aggregate doesn't have one in the formal sense — it doesn't exist until `Create` runs. |

This shape means:

- **Handlers are testable as pure functions.** Given a command and an aggregate state (which is itself just data), the handler's output is deterministic.
- **Aggregates are testable as pure functions.** Given an event and a current state, the result is the new state — no I/O, no side effects, no time.
- **The decider pattern's "command-handler" and "event-handler" are clearly separated**: command-handler = the Wolverine handler; event-handler = `Apply` methods on the aggregate.

For the substantive decider-pattern theory, see Jérémie Chassaing's [Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) — it's the canonical reference and the lineage of this idiom.

---

## Stream Identity and the `Create` Method

Wolverine assigns the stream ID at append time. The handler that produces the first event doesn't generate an ID; it lets Marten/Wolverine handle assignment, and `Create` reads the assigned ID from `@event.StreamId`.

```csharp
// Handler — no stream ID generation
public static (TripStarted, OutgoingMessages) Handle(
    StartTrip cmd,
    TimeProvider time)
{
    var started = new TripStarted(
        cmd.DriverId, cmd.RiderId, cmd.StartLocation, time.GetUtcNow());

    var outgoing = new OutgoingMessages();
    outgoing.Add(new Integration.TripStarted(/* ... */));

    return (started, outgoing);
}

// Aggregate — receives the assigned ID via metadata
public static Trip Create(IEvent<TripStarted> @event) =>
    new(Id: @event.StreamId, /* ... */);
```

This contrasts with the older idiom where commands carried a generated ID, the handler used it as the stream ID, and `Create` took the bare event type. The modern shape is cleaner because:

- The handler doesn't have to know about stream-identity mechanics.
- The aggregate's `Id` field is **identical** to the stream ID by construction — they cannot drift.
- Clients that originate commands don't need to generate or pass IDs (the gRPC method's response carries the assigned ID back).

### When the aggregate ID needs to be deterministic (UUID v5)

Some aggregates have natural keys that need ID stability across multiple producers. Example: a `DriverApplication` aggregate where the `DriverId` is the Microsoft Entra user ID — multiple producers may want to start the same stream and they need to converge on the same ID.

For these cases, generate the stream ID as UUID v5 (deterministic from a namespace + name) at the handler and pass it to `MartenOps.StartStream<T>(streamId, firstEvent)`:

```csharp
public static (DriverApplicationSubmitted, IStartStream) Handle(
    SubmitDriverApplication cmd,
    TimeProvider time)
{
    var streamId = Uuid5.Create(DriverApplicationNamespace, cmd.EntraUserId.ToString());
    var submitted = new DriverApplicationSubmitted(cmd.EntraUserId, /* ... */, time.GetUtcNow());
    var stream = MartenOps.StartStream<DriverApplication>(streamId, submitted);

    return (submitted, stream);
}
```

The `Create` method still reads `@event.StreamId` — Marten's stream-ID assignment respects the explicit value when supplied. The aggregate doesn't know or care whether the ID is auto-assigned or deterministic.

For UUID v7 (auto-assigned, time-ordered) vs UUID v5 (deterministic) decision details, see `csharp-coding-standards` § GUIDs.

---

## Apply Method Conventions

A few conventions worth pinning to keep `Apply` methods consistent and easy to read.

### One Apply per event type

Don't merge multiple event types into a single `Apply` with conditional branches. Marten's convention scanner picks each `Apply(TEvent, TAggregate)` overload separately by event type; merging them defeats the convention.

```csharp
// ❌ DON'T
public static Trip Apply(object @event, Trip current) => @event switch
{
    TripCompleted c => current with { Status = TripStatus.Completed, /* ... */ },
    TripCancelled c => current with { Status = TripStatus.Cancelled, /* ... */ },
    _ => current
};

// ✅ DO
public static Trip Apply(TripCompleted @event, Trip current) =>
    current with { Status = TripStatus.Completed, /* ... */ };

public static Trip Apply(TripCancelled @event, Trip current) =>
    current with { Status = TripStatus.Cancelled, /* ... */ };
```

### Parameter order: event first, current second

Both orders are accepted by Marten's convention. CritterCab pins event-first because the event is the active subject — "this happened, here's how the state evolves."

### Use `IEvent<TEvent>` only when metadata is needed

Most `Apply` methods take the bare event type. Use `IEvent<TEvent>` only when the body needs the event's metadata (`StreamId`, `Version`, `Timestamp`, `Sequence`). The `Create` method always uses `IEvent<TFirstEvent>` because stream identity is metadata.

```csharp
// Bare event type — most Apply methods
public static Trip Apply(WaypointRecorded @event, Trip current) =>
    current with { Waypoints = current.Waypoints.Add(@event.Location) };

// IEvent<T> — when timestamp metadata is needed (rarely)
public static Trip Apply(IEvent<RouteCorrected> @event, Trip current) =>
    current with
    {
        Waypoints = @event.Data.NewWaypoints.ToImmutableList(),
        LastCorrectedAt = @event.Timestamp        // metadata, not in @event.Data
    };
```

### Apply methods don't throw

If `Apply` is called with an event that produces an invalid state, that's a bug in the handler — the handler's `Validate` method should have caught it before the event was appended. `Apply` is pure evolution; it assumes the event is valid for the current state.

If you find yourself wanting to throw inside `Apply`, the right fix is one of: move the check into the handler's `Validate` method; introduce a missing event type that captures the failure mode explicitly; or accept that the state machine has gaps and add the missing transitions.

### Apply methods are commutative within a stream batch

Inline projections fold events in stream order. If two events that arrive in the same `SaveChangesAsync` batch reference each other in their `Apply` methods, the result depends on stream order. This is fine — stream order is durable — but it's worth being aware of when designing event sequences.

---

## Aggregate Field Conventions

A few field-shape decisions worth pinning.

### `Id` field

Always present. Always `Guid`. Always populated from `@event.StreamId` in `Create`. Never `Guid?`.

### Status enum

Most aggregates have a status enum representing their lifecycle stage. Define it in the same file as the aggregate or in a shared file in the same project (`Shared/TripStatus.cs` per `vertical-slice-organization` if multiple files reference it).

```csharp
public enum TripStatus
{
    InProgress,
    Arrived,
    Completed,
    Cancelled
}
```

Per `csharp-coding-standards` § Status Enums: prefer a single status enum over multiple booleans. Impossible states should be impossible.

### Nullable timestamps for terminal events

Lifecycle events (`StartedAt`, `CompletedAt`, `CancelledAt`) are commonly `DateTimeOffset?` because they're populated only when the corresponding event has been applied. The `null`-vs-populated distinction encodes the lifecycle stage redundantly with the status enum, and that's fine — it makes both projections and queries simpler.

### Value objects in aggregate state

Use them. `GeoLocation`, `Fare`, `LicensePlate`, `Waypoint` — these belong on the aggregate as value objects rather than primitives. See `csharp-coding-standards` § Value Object Pattern.

### Don't put navigation-style references on the aggregate

References to other aggregates are IDs, not embedded objects:

```csharp
// ✅ CORRECT
public sealed record Trip(Guid Id, Guid DriverId, Guid RiderId, /* ... */);

// ❌ WRONG — navigation property
public sealed record Trip(Guid Id, Driver Driver, Rider Rider, /* ... */);
```

If a handler needs both Trip and Driver state, it loads each independently. Cross-aggregate consistency is a Wolverine handler concern, not an aggregate-shape concern.

---

## Snapshot Strategies

Marten supports two aggregation strategies. The choice is per-aggregate.

| Strategy | When | Tradeoff |
|---|---|---|
| **Live aggregation** | Default for most aggregates | Aggregate is rebuilt from the event stream on every load. Cost is O(stream length); typically negligible for streams under a few hundred events. |
| **Inline projection** | Aggregates with long streams (>1000 events) or hot read paths | Aggregate is persisted as a document and updated synchronously when events are appended. Loads are O(1) at the cost of extra writes. |

Configuration in `Program.cs` per `service-bootstrap`:

```csharp
opts.Projections.LiveStreamAggregation<Trip>();          // most aggregates
opts.Projections.Snapshot<DriverProfile>(SnapshotLifecycle.Inline);  // hot or long
```

CritterCab's default: live aggregation. Switch to inline only when profiling identifies an aggregate's load latency as a real bottleneck. The premature-optimization risk here is real — every inline projection is a write amplification, and most Cab aggregates don't have streams long enough to justify the cost.

For async (rebuild-from-daemon) projection registration, see `marten-projections` and `marten-async-daemon` (Phase 2).

---

## Worked Example: RideOffer

A second canonical example covering a different shape — short-lived aggregate with a state machine driven by external decisions. Lives in Dispatch.

```csharp
using System.Collections.Immutable;
using Marten.Events;

namespace CritterCab.Dispatch;

public sealed record RideOffer(
    Guid Id,
    Guid RideRequestId,
    Guid DriverId,
    GeoLocation PickupLocation,
    OfferStatus Status,
    DateTimeOffset DispatchedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RespondedAt,
    string? RejectionReason)
{
    public static RideOffer Create(IEvent<OfferDispatched> @event) =>
        new(
            Id: @event.StreamId,
            RideRequestId: @event.Data.RideRequestId,
            DriverId: @event.Data.DriverId,
            PickupLocation: @event.Data.PickupLocation,
            Status: OfferStatus.Pending,
            DispatchedAt: @event.Data.DispatchedAt,
            ExpiresAt: @event.Data.ExpiresAt,
            RespondedAt: null,
            RejectionReason: null);

    public static RideOffer Apply(OfferAccepted @event, RideOffer current) =>
        current with
        {
            Status = OfferStatus.Accepted,
            RespondedAt = @event.AcceptedAt
        };

    public static RideOffer Apply(OfferRejected @event, RideOffer current) =>
        current with
        {
            Status = OfferStatus.Rejected,
            RespondedAt = @event.RejectedAt,
            RejectionReason = @event.Reason
        };

    public static RideOffer Apply(OfferExpired @event, RideOffer current) =>
        current with
        {
            Status = OfferStatus.Expired,
            RespondedAt = @event.ExpiredAt
        };
}
```

What's worth noting compared to `Trip`:

- **No collections at all** — `RideOffer` is small, short-lived, terminal. Live aggregation is obviously the right choice.
- **Mutually-exclusive terminal states** — once Status leaves `Pending`, no further Apply methods can run on a well-formed stream. The handler's `Validate` enforces this.
- **`RejectionReason` is nullable on the aggregate but non-nullable on `OfferRejected`.** The aggregate carries `null` for non-rejection terminal states; the event itself only exists when there IS a reason.

---

## Common Pitfalls

- **Instance `Apply` methods using `this`.** Older Marten idiom. Modern idiom is static. If you see `public Trip Apply(SomeEvent @event)` in a PR, that's the old shape — convert to `public static Trip Apply(SomeEvent @event, Trip current)`.
- **Generating stream IDs in the handler when auto-assignment is appropriate.** The handler returns the first event; Marten/Wolverine assigns the stream ID; `Create` reads it from `@event.StreamId`. Generating an explicit ID is only correct for deterministic-key aggregates (UUID v5).
- **Putting business validation on the aggregate.** Decisions live in the handler. The aggregate is pure state + evolution. If you find yourself adding a `CanReserve(...)` predicate on the aggregate, the predicate belongs in the handler's `Validate` method.
- **Forgetting `AddEventType<T>()` for a domain event.** The event type isn't registered, the stream loads but the event is silently skipped, the aggregate state is wrong. `UseMandatoryStreamTypeDeclaration = true` (set in `service-bootstrap`) makes this fail loudly at append time.
- **Returning the wrong type from `Apply`.** `Apply` must return `T` (the aggregate type). Not `void`, not the event, not a tuple. If Marten's convention scanner doesn't pick up an `Apply` method, the signature is wrong.
- **Mutable collection types on the aggregate.** `List<T>`, `Dictionary<TKey, TValue>`, `HashSet<T>` — all wrong on aggregates. Use `Immutable*` for aggregate state. See `csharp-coding-standards` § Collection Patterns.
- **`IReadOnlyList<T>` on aggregate state.** Works mechanically but leads to noisy `Apply` syntax (`[..current.Items, newItem]`) compared to `ImmutableList<T>.Add(...)`. Use `ImmutableList<T>` for aggregate state; reserve `IReadOnlyList<T>` for events and DTOs.
- **`Apply` that depends on the current time.** Time isn't available inside `Apply` (`TimeProvider` isn't injected — `Apply` is a pure function). If an event needs a timestamp, the timestamp belongs ON the event, captured by the handler at append time.
- **Throwing in `Apply`.** If an event is invalid for the current state, the handler should have rejected it. `Apply` is evolution, not validation.

---

## See also

**Upstream** — load these first:

- `csharp-coding-standards` — sealed records, positional constructors, `Immutable*` collections, value objects.
- `domain-event-conventions` — event naming, slim domain events vs. rich integration events, registration with Marten.
- `vertical-slice-organization` — aggregate file lives alongside its commands and events at the project root.
- `service-bootstrap` — `AddMarten` configuration, event-type registration, snapshot strategy registration.

**Downstream** — natural follow-ups:

- `marten-wolverine-aggregates` — handler patterns that load aggregates and produce events: `[WriteAggregate]`, `MartenOps.StartStream`, multi-stream handlers (Phase 2).
- `marten-projections` — inline and async projections; how aggregate snapshots are persisted (Phase 2).
- `marten-querying` — querying aggregates and projections via LINQ (Phase 2).
- `marten-async-daemon` — async-daemon configuration and projection rebuilds (Phase 2).
- `dynamic-consistency-boundary` — DCB-tagged event writes and decision queries that span aggregate boundaries (Phase 2).
- `wolverine-handlers` — the handler-side of the decider-pattern split.

**External:**

- ai-skills `marten-aggregate-handler-workflow` — the full Marten + Wolverine aggregate workflow reference.
- ai-skills `marten-event-sourcing-fundamentals` — generic Marten event-store mechanics.
- All ai-skills installed via `npx skills add` (license required).
- [Jérémie Chassaing — Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) — the canonical decider-pattern reference.
- [Marten Event Sourcing Documentation](https://martendb.io/events/).
