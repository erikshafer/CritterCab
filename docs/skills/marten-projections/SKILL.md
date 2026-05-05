---
name: marten-projections
description: "Designing and registering Marten projections in CritterCab — single-stream, multi-stream, and event projections; live vs inline vs async lifecycles; the snapshot/evolve idiom from Marten 8.0; explicit-code vs self-aggregating shapes; IoC-injected projections. Use when designing a read model or registering any projection."
cluster: marten
tags: [marten, projections, snapshots, single-stream, multi-stream, async-daemon, decider-pattern, evolve]
---

# Marten Projections

Designing and registering Marten projections in CritterCab. Projections turn the event stream into queryable read models — for a single aggregate (single-stream), an aggregated view across many streams (multi-stream), or one event to many documents (event projection).

This skill covers projection **shape and registration**. The async-daemon configuration that runs async projections is in `marten-async-daemon` (Phase 2). Querying projections lives in `marten-querying` (Phase 2). The aggregate-handler workflow that produces events for projections to consume lives in `marten-aggregates` and `marten-wolverine-aggregates`.

Marten 8.0 (released January 2026) aligned its API and terminology with [Jérémie Chassaing's Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider): **"snapshot"** is a persisted version of the projected view; **"evolve"** is the step of applying new events to a snapshot to produce the next snapshot. This skill uses that vocabulary throughout.

## When to apply this skill

Use this skill when:

- Designing a read model for a service.
- Choosing between live, inline, and async projection lifecycles.
- Registering a projection with `service-bootstrap`.
- Reviewing PRs that add or modify projections.
- Choosing between a self-aggregating snapshot type and an explicit `SingleStreamProjection<TDoc, TId>` subclass.
- Designing a multi-stream view that aggregates across many streams.

Do NOT use this skill for:

- The aggregate type itself (the "snapshot" model) when used by a single-stream projection — see `marten-aggregates` for shape conventions.
- Async daemon configuration, distribution, error handling — see `marten-async-daemon` (Phase 2).
- Querying the projected documents — see `marten-querying` (Phase 2).
- Testing projections (`WaitForNonStaleProjectionDataAsync`, `FakeTimeProvider`) — see `testing-integration` (Phase 2).
- Polecat projections — see `polecat-event-sourcing` (Phase 4); the Marten projection API was extended to Polecat with mostly identical shape.

---

## Three Projection Recipes

Marten supplies three primary projection recipes. Pick by the relationship between events and documents.

| Recipe | Relationship | Cab use case |
|---|---|---|
| **Single-stream projection** | One stream → one document | The Trip aggregate as a snapshot; an active-trip view per trip. |
| **Multi-stream projection** | Many streams → one document | A driver's lifetime stats aggregating across all their Trip streams; an operations dashboard rolling up active trips per region. |
| **Event projection** | One event → one or more documents | A `TripCompletedReceipt` document created from each `TripCompleted` event for the riding history. |

Most CritterCab projections are single-stream. Multi-stream is the right choice when the document's identity differs from any individual stream's identity. Event projection is rare — typically used for read models that mirror events 1:1 for query convenience.

---

## Three Lifecycles

Independent of the recipe, every projection runs at one of three lifecycles.

| Lifecycle | When events are applied | Tradeoff |
|---|---|---|
| **Live** | On every read; the snapshot is never persisted, only computed on demand. | Zero write amplification. Every load is O(stream length). Default for most CritterCab aggregates. |
| **Inline** | In the same transaction that captures the events. The snapshot is persisted and updated synchronously. | Reads are O(1) at the cost of write amplification. Use for hot read paths or long streams (>1000 events). |
| **Async** | In the background by the async daemon. The snapshot is eventually consistent. | Reads are O(1); writes are not amplified by projection cost. The daemon must be running. Use for multi-stream, IoC-dependent, or expensive projections. |

The lifecycle choice is per-projection, registered in `service-bootstrap` per the patterns in `marten-aggregates` § Snapshot Strategies.

```csharp
// Self-aggregating live aggregation — most common
opts.Projections.LiveStreamAggregation<Trip>();

// Self-aggregating inline snapshot — when O(1) loads matter
opts.Projections.Snapshot<DriverProfile>(SnapshotLifecycle.Inline);

// Self-aggregating async snapshot — when the projection has IoC dependencies or is expensive
opts.Projections.Snapshot<TripSummary>(SnapshotLifecycle.Async);

// Explicit projection class — use the instance form when the constructor does anything
opts.Projections.Add(new DriverLifetimeStatsProjection(), ProjectionLifecycle.Async);

// Or the generic form when the constructor is parameterless and trivial
opts.Projections.Add<SimpleAuditProjection>(ProjectionLifecycle.Async);
```

The `LiveStreamAggregation<T>` and `Snapshot<T>(SnapshotLifecycle)` calls work only with self-aggregating types (Shape 1 below). Marten throws an `InvalidOperationException` at startup if you try them with a `SingleStreamProjection<TDoc, TId>` subclass; for explicit projection classes, use `Add(...)`.

**CritterCab default:** live aggregation for write-side aggregates (the `Trip`, `RideOffer`, `DriverApplication` aggregates that handlers load via `[WriteAggregate]`). Read models that fan out across many streams (operations dashboard, lifetime stats) go async. Inline is reserved for measured hot paths.

---

## Single-Stream Projection: Two Shapes

Single-stream projections come in two authoring shapes. Pick by separation needs.

### Shape 1 — Self-aggregating snapshot (preferred for write-side aggregates)

The aggregate type itself carries `Create` and `Apply` methods. Marten convention-scans them. This is the shape from `marten-aggregates`:

```csharp
public sealed record Trip(/* ... */)
{
    public static Trip Create(IEvent<TripStarted> @event) => /* ... */;
    public static Trip Apply(TripCompleted @event, Trip current) => /* ... */;
    /* ... */
}

// In service-bootstrap (Program.cs):
opts.Projections.LiveStreamAggregation<Trip>();
```

Use this shape when:
- The same type is used as the write-side aggregate (loaded via `[WriteAggregate]` in handlers).
- The projection is straightforward — `Create` + `Apply` per event, no cross-cutting concerns.
- No IoC dependencies are needed during projection.

This is the dominant shape in CritterCab. Most event-sourced aggregates ARE their own projection.

### Shape 2 — Explicit `SingleStreamProjection<TDoc, TId>` subclass (for separation or advanced features)

The projection logic lives in a separate class that inherits from `SingleStreamProjection<TDoc, TId>`. The aggregate type stays clean of projection concerns.

```csharp
public sealed record Trip(/* ... */);   // No Create/Apply methods; pure data

public class TripProjection : SingleStreamProjection<Trip, Guid>
{
    public TripProjection()
    {
        Name = "Trip";
    }

    public Trip Create(IEvent<TripStarted> @event) =>
        new(/* ... */, Id: @event.StreamId);

    public Trip Apply(TripCompleted @event, Trip current) =>
        current with { /* ... */ };
}

// In service-bootstrap:
opts.Projections.Add(new TripProjection(), ProjectionLifecycle.Inline);
```

If the explicit projection type has a parameterless constructor and no constructor logic, the generic overload is also valid:

```csharp
opts.Projections.Add<TripProjection>(ProjectionLifecycle.Inline);
```

**Important:** the generic `Add<T>(...)` overload requires `T` to have a parameterless `new()` constructor. If your projection's constructor sets `Name`, captures dependencies, or does any other work, use the instance form `Add(new TProjection(), lifecycle)` instead. The compiler enforces the `new()` constraint.

Use this shape when:
- You need IoC-injected dependencies during projection (use `AddProjectionWithServices<T>` instead of `Add<T>`; see § IoC-Injected Projections below).
- The projection logic is complex enough to warrant its own class.
- Multiple projections target the same document type with different lifecycles or filters.
- You want versioning, custom slicing, or the explicit `Evolve` method instead of multiple `Apply` overloads.

### The single-method `Evolve` alternative (Jeremy's March 2026 post)

`SingleStreamProjection<TDoc, TId>` exposes an explicit-code `Evolve` method that handles all event types in one place. Useful when convention-based naming feels noisy or when subclass-event dispatch confuses Marten's convention scanner:

```csharp
public class TripProjection : SingleStreamProjection<Trip, Guid>
{
    public override Trip Evolve(Trip snapshot, Guid id, IEvent e)
    {
        snapshot ??= new Trip(Id: id, /* defaults */);
        return e.Data switch
        {
            TripStarted s    => snapshot with { /* ... */ },
            TripCompleted c  => snapshot with { Status = TripStatus.Completed, /* ... */ },
            TripCancelled c  => snapshot with { Status = TripStatus.Cancelled, /* ... */ },
            _ => snapshot
        };
    }
}
```

**When `Evolve` fits over multiple `Apply` methods:**
- The dispatch is straightforward and the switch reads cleaner than five separate methods.
- You're using subclass-event hierarchies that confuse convention dispatch.
- You want explicit fall-through or no-op behavior on unrecognized events.

**When multiple `Apply` methods fit better:**
- Each event's logic is non-trivial.
- You want Marten's filter optimization (only events with matching `Apply` methods are scanned at projection time).
- The aggregate IS the projection (Shape 1).

For most Cab aggregates, Shape 1 is correct. Reach for Shape 2 with explicit `Evolve` when those shape-1 conditions break.

---

## Multi-Stream Projections

A multi-stream projection aggregates events from many streams into a single document. The classic example: a driver's lifetime stats document, where the document's ID is the driver's ID but the events come from many different `Trip` streams.

```csharp
public class DriverLifetimeStats
{
    public Guid Id { get; init; }                // Driver ID
    public int TotalTrips { get; set; }
    public decimal TotalEarnings { get; set; }
    public DateTimeOffset? FirstTripAt { get; set; }
    public DateTimeOffset? LastTripAt { get; set; }
}

public class DriverLifetimeStatsProjection : MultiStreamProjection<DriverLifetimeStats, Guid>
{
    public DriverLifetimeStatsProjection()
    {
        Name = "DriverLifetimeStats";

        // Tell Marten how to find the document ID from each event.
        Identity<TripStarted>(e => e.DriverId);
        Identity<TripCompleted>(e => e.DriverId);
    }

    public void Apply(IEvent<TripStarted> @event, DriverLifetimeStats stats)
    {
        stats.FirstTripAt ??= @event.Timestamp;
        stats.LastTripAt = @event.Timestamp;
    }

    public void Apply(IEvent<TripCompleted> @event, DriverLifetimeStats stats)
    {
        stats.TotalTrips += 1;
        stats.TotalEarnings += @event.Data.FinalFare.Amount;
        stats.LastTripAt = @event.Timestamp;
    }
}

// In service-bootstrap:
opts.Projections.Add(new DriverLifetimeStatsProjection(), ProjectionLifecycle.Async);
```

Two things to internalize:

- **`Identity<TEvent>(...)`** tells Marten how to map each event type to the projected document's ID. Without it, Marten doesn't know which `DriverLifetimeStats` document to update for a given event.
- **Multi-stream projections almost always run async.** Marten's recommendation per the projection docs. The cross-stream slicing happens in the daemon's slicing step; running it inline would tie every Trip-stream event-append to a multi-stream slice computation in the same transaction.

**Multi-stream gotchas:**

- **Events without the foreign key.** If a later event in the stream doesn't carry the driver ID (it was set on `TripStarted` but not on `WaypointRecorded`), you need a lookup pattern. Marten's docs cover three patterns: lookup document, custom `IAggregateGrouper`, or batched grouper-with-cache. The grouper pattern is the recommended general fix; see [Marten's Multi-Stream Projections docs](https://martendb.io/events/projections/multi-stream-projections.html).
- **`Identities<TEvent>(e => list)`** for one-to-many mapping when a single event affects multiple documents.
- **Multi-stream projections cost more.** Each event requires both grouping (which document) and applying (mutate the document). Profile before assuming this is free.

If a Cab projection genuinely requires loading the aggregate document during grouping, multi-stream projections won't work — the docs note that grouping happens in parallel to building views as a performance optimization. That case requires a custom `IProjection` implementation, which is rare enough not to belong in this skill.

---

## Event Projections

`EventProjection` is for the simple case: one event produces one (or a few) documents. Useful when you want a denormalized view of events for query convenience.

```csharp
public sealed record TripReceipt(
    Guid Id,                            // = TripId
    Guid RiderId,
    Guid DriverId,
    decimal FareAmount,
    DateTimeOffset CompletedAt);

public class TripReceiptProjection : EventProjection
{
    public TripReceipt Create(IEvent<TripCompleted> @event) =>
        new(
            Id: @event.StreamId,
            RiderId: /* loaded from a sibling event or carried on TripCompleted */,
            DriverId: /* same */,
            FareAmount: @event.Data.FinalFare.Amount,
            CompletedAt: @event.Data.CompletedAt);
}

// In service-bootstrap:
opts.Projections.Add(new TripReceiptProjection(), ProjectionLifecycle.Async);
```

Use `EventProjection` when:
- The mapping is event → document (1:1 or 1:few).
- You don't need the prior state of the document — each event carries enough to construct or transform the document.
- The lifecycle suits async (most do; receipts and audit-style projections rarely earn inline).

`EventProjection` doesn't have a `snapshot` parameter on its methods — it doesn't track per-document state across events. If your projection needs to "the document already exists; modify it based on this new event," that's a single-stream or multi-stream projection, not an event projection.

---

## IoC-Injected Projections

When a projection needs services from the application's container — a price-lookup service, a Microsoft Graph client, an external read API — register it via `AddProjectionWithServices<T>`:

```csharp
public class TripPricingProjection : SingleStreamProjection<TripWithPricing, Guid>
{
    private readonly IPriceLookup _lookup;

    public TripPricingProjection(IPriceLookup lookup)
    {
        _lookup = lookup;
        Name = "TripWithPricing";
    }

    public override TripWithPricing Evolve(TripWithPricing snapshot, Guid id, IEvent e)
    {
        snapshot ??= new TripWithPricing { Id = id };
        if (e.Data is TripStarted started)
        {
            snapshot.Status = TripStatus.InProgress;
            snapshot.EstimatedFare = _lookup.EstimateFor(started.PickupZone, started.DropoffZone);
        }
        return snapshot;
    }
}

// In service-bootstrap:
services.AddSingleton<IPriceLookup, PriceLookup>();
services.AddMarten(opts => { /* ... */ })
    .AddProjectionWithServices<TripPricingProjection>(
        ProjectionLifecycle.Async,
        ServiceLifetime.Singleton);
```

**Important constraints** per Marten's IoC docs:

- `AddProjectionWithServices<T>` only works with projection types that **directly implement `IProjection`** OR inherit from `SingleStreamProjection<TDoc, TId>`, `MultiStreamProjection<TDoc, TId>`, `CustomAggregation<TDoc, TId>`, or `EventProjection`. The convention-based "self-aggregating" shape (Shape 1 above) cannot use injected services.
- The `ServiceLifetime` (`Singleton` vs `Scoped`) governs how the projection instance is created. **Singleton is preferred** when the dependencies allow it — Marten then builds the projection once at startup and reuses it. Scoped projections allocate a fresh container scope per slice processed.
- Inline projections do not share the request scope with the surrounding HTTP request or message handler. Don't try to access `HttpContext` or per-request state from a projection.

This is the path for the swappable-provider pattern in `identity-acl` (Phase 3) — when an Identity service projects Microsoft Graph user-lifecycle events into local domain documents, the Graph client is IoC-injected and the projection lives in `AddProjectionWithServices<T>`.

---

## Lifecycle Decision Guide

Quick decision flow when a new projection lands.

1. **Is the document the write-side aggregate itself (loaded via `[WriteAggregate]` in handlers)?**
   - **Yes:** Self-aggregating snapshot type (Shape 1). Default to `LiveStreamAggregation<T>`. Promote to `SnapshotLifecycle.Inline` only if profiling shows aggregate load latency as a real bottleneck.

2. **Is the document fanned out across multiple streams?**
   - **Yes:** Multi-stream projection. Run async unless you have a strong reason otherwise.

3. **Is the document a 1:1 or 1:few mirror of events?**
   - **Yes:** `EventProjection`. Run async; receipt-style projections rarely earn inline.

4. **Is the projection complex, requires IoC dependencies, or has soft-delete logic?**
   - **Yes:** Explicit `SingleStreamProjection<TDoc, TId>` subclass, registered via `AddProjectionWithServices<T>` if dependencies are involved. Lifecycle by need: inline for hot paths, async otherwise.

5. **Is the projection genuinely a hot path (high-frequency reads, low write volume)?**
   - **Yes:** Inline. Otherwise async.

When in doubt: live for write-side aggregates, async for everything else. Inline is for measured hot paths, not speculative ones.

---

## Soft-Delete Pattern

Some projections need to delete the document on a specific event. Two ways:

### Marker (preferred for the simple case)

In an explicit `SingleStreamProjection<TDoc, TId>` subclass, mark events that delete the document:

```csharp
public class RideOfferProjection : SingleStreamProjection<RideOfferView, Guid>
{
    public RideOfferProjection()
    {
        Name = "RideOfferView";
        DeleteEvent<OfferCancelled>();   // any OfferCancelled event deletes the document
    }

    public RideOfferView Create(IEvent<OfferDispatched> @event) => /* ... */;
    public RideOfferView Apply(OfferAccepted @event, RideOfferView current) => /* ... */;
}
```

`DeleteEvent<T>()` short-circuits projection processing for that event type — slightly more efficient than the alternative.

### Returning null from `Evolve` or `Apply`

In the explicit-code path, returning `null` from `Evolve` (or an `Apply` that returns `T?`) tells Marten to delete the document. Marker form (`DeleteEvent<T>()`) is preferred when applicable; `null` return is fallback for conditional deletes (e.g., delete only if some predicate holds).

---

## Lookup-Document Pattern (Multi-Stream Cross-Reference)

A common multi-stream challenge: events on one stream reference an aggregate that lives on a different stream. The Marten docs recommend three patterns; the lookup-document pattern is simplest and works for many Cab cases.

The pattern at a glance:

1. **An inline single-stream projection** maintains a small lookup document mapping external-key to aggregate-id.
2. **A multi-stream projection** consults the lookup during grouping to find the right aggregate-id for events that don't carry it.
3. **A grouper-with-cache** combines in-flight batch-internal lookups with the persisted lookup, avoiding the same-batch race that pure-lookup-document hits.

The substantive coverage and code examples for each pattern live in [Marten's Multi-Stream Projections docs](https://martendb.io/events/projections/multi-stream-projections.html). When this pattern lands in a Cab service, copy from there — Marten's documentation team revamped this section in early 2026 specifically to make the patterns clearer.

---

## Anti-Patterns

- **Multiple `Apply` methods AND an `Evolve` override on the same projection.** Pick one. The convention scanner picks up `Apply`; explicit code uses `Evolve`. Mixing produces undefined behavior.
- **Inline lifecycle for multi-stream projections.** Almost always wrong. Marten's grouping step is parallel-to-applying; running it inline serializes the whole pipeline into the event-append transaction. Use async.
- **Speculative inline projections.** Inline costs write amplification on every event-append. Use it only when profiling shows live-aggregation latency as a real read-side bottleneck.
- **Live aggregation for hot read paths.** Live recomputes from scratch on every load. If the read frequency is high or the stream is long, profile and consider promoting to inline or async.
- **Trying to use `AddProjectionWithServices<T>` with a self-aggregating type.** Doesn't work — only `IProjection`/`SingleStreamProjection`/`MultiStreamProjection`/`CustomAggregation`/`EventProjection` are supported. Convert to an explicit projection class.
- **Multi-stream projections that need to load the aggregate document during grouping.** The grouping step happens in parallel to view-building as a performance optimization; loading the aggregate during grouping breaks ordering. Use a custom `IProjection` instead, or a lookup-document pattern.
- **Forgetting to register the projection.** A `SingleStreamProjection<TDoc, TId>` subclass that's never added to `opts.Projections` does nothing. The compiler doesn't catch this — silent failure, like the most consequential CritterCab footgun in `wolverine-messaging-handlers`.
- **Mutating the input snapshot in `Apply`.** Use `with` expressions for record-typed snapshots; for class-typed snapshots, mutate properties (Marten allows it for projections, in contrast to the immutable-record convention for aggregates). The rule for which kind of snapshot type fits is in `marten-aggregates` — record for write-side aggregates, class is acceptable for projection-only views where allocation pressure matters.

---

## See also

**Upstream** — load these first:

- `marten-aggregates` — aggregate shape, snapshot/evolve naming, when an aggregate IS its own projection.
- `marten-wolverine-aggregates` — the handler workflow that produces events for projections to consume.
- `domain-event-conventions` — slim domain events vs. rich integration events.
- `service-bootstrap` — where projection registration lands (`opts.Projections.LiveStreamAggregation<T>`, `Snapshot<T>`, `Add<T>`, `AddProjectionWithServices<T>`).

**Sibling skills:**

- `marten-async-daemon` — daemon configuration, distribution mode (Solo/HotCold/Wolverine-managed), error handling, rebuild strategies (Phase 2).
- `marten-querying` — querying projected documents via LINQ, compiled queries, batched queries (Phase 2).
- `dynamic-consistency-boundary` — DCB-tagged event writes that span aggregate boundaries; how DCB and projections compose (Phase 2).

**Downstream:**

- `testing-integration` — `WaitForNonStaleProjectionDataAsync`, `FakeTimeProvider`, projection rebuild patterns in tests (Phase 2).
- `polecat-event-sourcing` — Polecat's parallel projection API for SQL Server services (Phase 4).
- `identity-acl` — uses `AddProjectionWithServices<T>` to project Microsoft Graph user-lifecycle events into local domain documents (Phase 3).

**External:**

- ai-skills `marten-aggregate-handler-workflow` — the canonical Marten + Wolverine workflow including projection patterns.
- ai-skills `marten-event-sourcing-fundamentals` — generic Marten event-store mechanics.
- All ai-skills installed via `npx skills add` (license required).
- [Marten Aggregation Projection Subsystem (Jeremy Miller, January 22, 2026)](https://jeremydmiller.com/2026/01/22/martens-aggregation-projection-subsystem/) — the canonical 2026 framing of "snapshot" and "evolve" alignment with Chassaing.
- [New Option for Simple Projections in Marten or Polecat (Jeremy Miller, March 24, 2026)](https://jeremydmiller.com/2026/03/24/new-option-for-simple-projections-in-marten-or-polecat/) — the explicit `Evolve` method on `SingleStreamProjection<TDoc, TId>`.
- [Easier Query Models with Marten (Jeremy Miller, January 20, 2026)](https://jeremydmiller.com/2026/01/20/easier-query-models-with-marten/) — multi-stream projection improvements and composite/chained projections.
- [Marten Projections Documentation](https://martendb.io/events/projections/).
- [Marten Single-Stream Projections](https://martendb.io/events/projections/single-stream-projections.html).
- [Marten Multi-Stream Projections](https://martendb.io/events/projections/multi-stream-projections.html).
- [Marten Aggregate Projections](https://martendb.io/events/projections/aggregate-projections.html).
- [Marten IoC and Projections](https://martendb.io/events/projections/ioc.html).
- [Jérémie Chassaing — Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) — the canonical decider-pattern reference for the "snapshot" and "evolve" terminology.
