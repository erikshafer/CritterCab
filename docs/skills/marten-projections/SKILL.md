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

Independent of the recipe, every projection runs at one of three lifecycles. The full mechanic surface (Inline/Async/Live behavior, per-projection registration) is documented in ai-skills `marten-projections-single-stream` § Projection lifecycles. The Cab-specific framing:

| Lifecycle | Cab default and use case |
|---|---|
| **Live** | Default for write-side aggregates (`Trip`, `RideOffer`, `DriverApplication`) loaded via `[WriteAggregate]` in handlers. Zero write amplification; load is O(stream length). |
| **Inline** | Reserved for measured hot paths only. Speculative inline is a projection footgun — every event-append pays the projection cost. Reads are O(1). |
| **Async** | Read models that fan out across multiple streams (operations dashboard, lifetime stats), have IoC dependencies, or are expensive enough to warrant deferral. Requires the async daemon. |

```csharp
// Self-aggregating live aggregation — most common
opts.Projections.LiveStreamAggregation<Trip>();

// Self-aggregating inline snapshot — when O(1) loads matter
opts.Projections.Snapshot<DriverProfile>(SnapshotLifecycle.Inline);

// Self-aggregating async snapshot — when the projection has IoC dependencies or is expensive
opts.Projections.Snapshot<TripSummary>(SnapshotLifecycle.Async);

// Explicit projection class — instance form when the constructor does anything
opts.Projections.Add(new DriverLifetimeStatsProjection(), ProjectionLifecycle.Async);

// Generic form — only when the constructor is parameterless and trivial
opts.Projections.Add<SimpleAuditProjection>(ProjectionLifecycle.Async);
```

`LiveStreamAggregation<T>` and `Snapshot<T>(SnapshotLifecycle)` work only with self-aggregating types (Shape 1 below); for explicit `SingleStreamProjection<TDoc, TId>` subclasses, use `Add(...)`.

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
    public TripProjection() { Name = "Trip"; }

    public Trip Create(IEvent<TripStarted> @event) => new(/* ... */, Id: @event.StreamId);
    public Trip Apply(TripCompleted @event, Trip current) => current with { /* ... */ };
}

// Registration — instance form when the constructor does anything:
opts.Projections.Add(new TripProjection(), ProjectionLifecycle.Inline);
```

**Footgun:** the generic `Add<T>(...)` overload requires `T` to have a parameterless `new()` constructor. If your projection's constructor sets `Name`, captures dependencies, or does any other work, use the instance form `Add(new TProjection(), lifecycle)` — the compiler enforces the `new()` constraint, but the diagnostic is easy to misread.

Use this shape when:
- You need IoC-injected dependencies during projection (use `AddProjectionWithServices<T>`; see § IoC-Injected Projections below).
- The projection logic is complex enough to warrant its own class.
- Multiple projections target the same document type with different lifecycles or filters.
- You want versioning, custom slicing, or the explicit `Evolve` method instead of multiple `Apply` overloads.

### The single-method `Evolve` alternative (Jeremy's March 2026 post)

`SingleStreamProjection<TDoc, TId>` exposes an explicit-code `Evolve` method that handles all event types in one switch expression. The mechanic is documented in ai-skills `marten-projections-single-stream`. Cab decision criteria:

**When `Evolve` fits over multiple `Apply` methods:**
- The dispatch is straightforward and the switch reads cleaner than five separate methods.
- You're using subclass-event hierarchies that confuse convention dispatch.
- You want explicit fall-through or no-op behavior on unrecognized events.

**When multiple `Apply` methods fit better:**
- Each event's logic is non-trivial.
- You want Marten's filter optimization (only events with matching `Apply` methods are scanned at projection time).
- The aggregate IS the projection (Shape 1).

For most Cab aggregates, Shape 1 is correct. Reach for Shape 2 with explicit `Evolve` when those Shape-1 conditions break.

---

## Multi-Stream Projections

A multi-stream projection aggregates events from many streams into a single document. The classic Cab example: a driver's lifetime stats document, where the document's ID is the driver's ID but the events come from many different `Trip` streams.

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

**Cab pin: multi-stream projections almost always run async.** The cross-stream slicing happens in the daemon's slicing step; running it inline would tie every Trip-stream event-append to a multi-stream slice computation in the same transaction. `Identity<TEvent>(...)` (one event → one document) and `Identities<TEvent>(...)` (one event → many documents, fan-out) are the routing primitives. For the full mechanic surface — fan-out patterns, custom groupers with DB lookups, ViewProjection, time-segmented projections, composite identity keys — see ai-skills `marten-projections-multi-stream`.

**Multi-stream gotcha worth highlighting:** if a later event in the stream doesn't carry the foreign key (e.g., `WaypointRecorded` doesn't carry the driver ID that `TripStarted` did), you need a lookup pattern. The lookup-document pattern is the lightest fix; the grouper pattern is the general one. See § Lookup-Document Pattern below and Marten's docs for the substantive coverage.

---

## Event Projections

`EventProjection` is for the simple case: one event produces one (or a few) documents. Useful when you want a denormalized view of events for query convenience. **Note:** `EventProjection` doesn't track per-document state across events (no `snapshot` parameter); for "modify existing document based on new event," use single-stream or multi-stream instead.

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

Two ways to delete the document on a specific event:

**Marker form (preferred for the simple case)** — in an explicit `SingleStreamProjection<TDoc, TId>` subclass, mark events that delete the document via `DeleteEvent<T>()`:

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

**Null-return form** — returning `null` from `Evolve` (or `Apply` that returns `T?`) tells Marten to delete. Use this for conditional deletes (e.g., delete only if some predicate holds); prefer `DeleteEvent<T>()` for simple unconditional cases. See ai-skills `marten-projections-single-stream` § Conditional deletes for the full surface.

---

## Lookup-Document Pattern (Multi-Stream Cross-Reference)

A common multi-stream challenge: events on one stream reference an aggregate that lives on a different stream. The lookup-document pattern is the simplest fix and works for many Cab cases.

The pattern at a glance:

1. **An inline single-stream projection** maintains a small lookup document mapping external-key to aggregate-id.
2. **A multi-stream projection** consults the lookup during grouping to find the right aggregate-id for events that don't carry it.
3. **A grouper-with-cache** combines in-flight batch-internal lookups with the persisted lookup, avoiding the same-batch race that pure-lookup-document hits.

For full mechanic coverage and code examples, see ai-skills `marten-projections-multi-stream` § Custom groupers with DB lookups, plus [Marten's Multi-Stream Projections docs](https://martendb.io/events/projections/multi-stream-projections.html).

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

**Upstream** — generic Marten projection mechanics this skill defers to. ai-skills (license required, install via `npx skills add`):

- `marten-projections-single-stream` — single-stream projection mechanics: Apply/Create method conventions, the explicit `Evolve` method, lifecycle behavior (Inline/Async/Live), conditional deletes, rebuilds, testing.
- `marten-projections-multi-stream` — Identity routing, fan-out patterns (`Identities<T>`), custom groupers with DB lookups, ViewProjection, time-segmented projections, composite identity keys.
- `marten-projections-event-enrichment` — `EnrichEventsAsync` to avoid N+1 queries; declarative enrichment API. **No Cab parallel** — load this directly when a projection needs reference data beyond the raw event.
- `marten-projections-composite` — composite projections, staged execution, `Updated<T>`/`ProjectionDeleted<T>`/`References<T>` synthetic events, chained projections. **No Cab parallel** — load this directly when building chained read models.
- `marten-projections-raise-side-effects` — `RaiseSideEffects` override for publishing messages, appending events, or enqueuing work atomically with a projection update. **No Cab parallel** — load this directly when a projection needs to trigger downstream effects.

**Prerequisites** — Cab-internal skills to load first if unfamiliar:

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

- [Marten Aggregation Projection Subsystem (Jeremy Miller, January 22, 2026)](https://jeremydmiller.com/2026/01/22/martens-aggregation-projection-subsystem/) — the canonical 2026 framing of "snapshot" and "evolve" alignment with Chassaing.
- [New Option for Simple Projections in Marten or Polecat (Jeremy Miller, March 24, 2026)](https://jeremydmiller.com/2026/03/24/new-option-for-simple-projections-in-marten-or-polecat/) — the explicit `Evolve` method on `SingleStreamProjection<TDoc, TId>`.
- [Easier Query Models with Marten (Jeremy Miller, January 20, 2026)](https://jeremydmiller.com/2026/01/20/easier-query-models-with-marten/) — multi-stream projection improvements and composite/chained projections.
- [Marten Projections Documentation](https://martendb.io/events/projections/).
- [Marten Single-Stream Projections](https://martendb.io/events/projections/single-stream-projections.html).
- [Marten Multi-Stream Projections](https://martendb.io/events/projections/multi-stream-projections.html).
- [Marten Aggregate Projections](https://martendb.io/events/projections/aggregate-projections.html).
- [Marten IoC and Projections](https://martendb.io/events/projections/ioc.html).
- [Jérémie Chassaing — Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) — the canonical decider-pattern reference for the "snapshot" and "evolve" terminology.
