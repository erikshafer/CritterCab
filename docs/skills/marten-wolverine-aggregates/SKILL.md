---
name: marten-wolverine-aggregates
description: "The handler-side of the Critter Stack decider pattern: [WriteAggregate] for loading, MartenOps.StartStream for first events, optimistic concurrency, and the cascading return shapes that commit events + integration messages atomically. Use when authoring or reviewing any aggregate command handler."
cluster: marten
tags: [marten, wolverine, handlers, aggregates, decider-pattern, write-aggregate, martenops, optimistic-concurrency]
---

# Marten + Wolverine Aggregate Handlers

The handler side of the decider pattern. The aggregate skill (`marten-aggregates`) covers the `evolve` function — the static `Apply` methods that fold events over the aggregate. This skill covers the `decide` function — the Wolverine handler that loads the current aggregate state, validates the command against it, and returns events to append.

The pairing is structurally enforced by Wolverine's code generation. A handler with a `[WriteAggregate]` parameter and a return type Wolverine recognizes (event, `Events`, `(Event, OutgoingMessages)`, `IStartStream`, etc.) commits atomically: events appended, integration messages enrolled in the outbox, the aggregate's stream version advanced, all in one transaction.

## When to apply this skill

Use this skill when:

- Authoring a command handler that operates on an event-sourced aggregate.
- Reviewing PRs that add `[WriteAggregate]` parameters or `MartenOps.StartStream` calls.
- Choosing between optimistic and exclusive concurrency for an aggregate handler.
- Writing handlers that produce events and integration messages atomically.
- Designing handlers that span multiple aggregate streams.

Do NOT use this skill for:

- Aggregate shape, `Apply` methods, `Create` method — see `marten-aggregates`.
- General handler shape, validation pipeline, return-type orientation — see `wolverine-handlers`.
- HTTP-endpoint patterns layered on top of aggregate handlers — see `wolverine-http-handlers`.
- Routing rules for outbound integration messages — see `wolverine-messaging-handlers`.
- Polecat aggregate handlers — see `polecat-event-sourcing` (Phase 4); the API parallels Marten's via `PolecatOps`.
- Multi-stream DCB writes that don't load a single aggregate — see `dynamic-consistency-boundary` (Phase 2).

---

## The Two Canonical Shapes

Aggregate handlers come in two shapes: starting a new stream, or appending events to an existing one. Every handler lands in one of these.

### Starting a new stream

The handler returns `IStartStream` (typically via `MartenOps.StartStream<T>`). Wolverine assigns the stream ID, persists the events, and the aggregate's `Create` method receives `IEvent<TFirstEvent>` with `@event.StreamId` populated.

```csharp
namespace CritterCab.Trips;

public sealed record StartTrip(
    Guid DriverId,
    Guid RiderId,
    GeoLocation StartLocation);

public static class StartTripHandler
{
    public static (IStartStream, OutgoingMessages) Handle(
        StartTrip cmd,
        TimeProvider time)
    {
        var started = new TripStarted(
            DriverId: cmd.DriverId,
            RiderId: cmd.RiderId,
            StartLocation: cmd.StartLocation,
            StartedAt: time.GetUtcNow());

        // MartenOps.StartStream returns IStartStream; Wolverine intercepts and persists.
        // No stream ID generation here — Wolverine assigns it.
        var stream = MartenOps.StartStream<Trip>(started);

        var outgoing = new OutgoingMessages();
        outgoing.Add(new Integration.TripStarted(/* ... */));

        return (stream, outgoing);
    }
}
```

### Appending events to an existing stream

The handler takes a `[WriteAggregate(...)]` parameter. Wolverine loads the aggregate from the event store, runs `Validate` if present, calls `Handle` with the loaded aggregate, and appends the returned event(s).

```csharp
namespace CritterCab.Trips;

public sealed record CompleteTrip(
    Guid TripId,
    GeoLocation EndLocation,
    Fare FinalFare);

public static class CompleteTripHandler
{
    public static ProblemDetails Validate(CompleteTrip cmd, Trip trip)
    {
        if (trip.Status != TripStatus.InProgress && trip.Status != TripStatus.Arrived)
            return new ProblemDetails { Detail = "Trip is not in a completable state", Status = 409 };
        return WolverineContinue.NoProblems;
    }

    public static (TripCompleted, OutgoingMessages) Handle(
        CompleteTrip cmd,
        [WriteAggregate(nameof(CompleteTrip.TripId))] Trip trip,
        TimeProvider time)
    {
        var completed = new TripCompleted(
            EndLocation: cmd.EndLocation,
            FinalFare: cmd.FinalFare,
            CompletedAt: time.GetUtcNow());

        var outgoing = new OutgoingMessages();
        outgoing.Add(new Integration.TripCompleted(
            trip.Id, trip.RiderId, trip.DriverId, cmd.FinalFare, /* ... */));

        return (completed, outgoing);
    }
}
```

The `Handle` method is the happy path. If `Validate` returned a non-`NoProblems` `ProblemDetails`, the handler short-circuits with 400/409 and `Handle` never runs. See `wolverine-handlers` § Validate vs ValidateAsync for the full validation pipeline.

---

## `[WriteAggregate]` Parameter Resolution

The attribute resolves the aggregate ID from the command (or HTTP route, header, etc.) and instructs Wolverine to load the aggregate into the parameter before `Handle` runs.

### Identity resolution

Cab's convention pins the explicit positional override with `nameof()` even when the convention fallback would resolve correctly. The `nameof()` reference makes the command-to-aggregate linkage refactor-safe — if `CompleteTrip.TripId` is renamed to `CompleteTrip.TripIdentifier`, the compiler catches it.

```csharp
[WriteAggregate(nameof(CompleteTrip.TripId))] Trip trip
```

For Wolverine's full resolution chain (positional override → HTTP route segment → `<AggregateTypeName>Id`/`Id` convention → Marten natural-key projection), see ai-skills `marten-aggregate-handler-workflow` § Aggregate identity conventions.

### Concurrency style

`LoadStyle` controls how the aggregate is loaded and how concurrency is enforced. **Cab's pin: Optimistic** — Marten checks the stream version on append and throws if it has advanced since load. Switch to `ConcurrencyStyle.Exclusive` (advisory lock for the handler's duration) only for long-running handlers, contended streams, or when optimistic-retry isn't acceptable.

```csharp
// Optimistic is the default; shown for clarity:
[WriteAggregate(nameof(CompleteTrip.TripId), LoadStyle = ConcurrencyStyle.Optimistic)] Trip trip
```

For the Version-property auto-detection and `VersionSource` override surface, see ai-skills `marten-aggregate-handler-workflow` § Optimistic concurrency.

### `AlwaysEnforceConsistency`

If `true`, Marten enforces the optimistic-concurrency check **even if the handler decides not to emit any new events.** Useful for handlers that read the aggregate's state to make a decision but want guaranteed read isolation.

```csharp
[WriteAggregate(nameof(Cmd.Id), AlwaysEnforceConsistency = true)] SomeAggregate agg
```

Default is `false`. Use only when the handler's decision depends on the aggregate not having advanced under it.

---

## Return-Type Cheat Sheet

Wolverine recognizes these return types from aggregate handlers. The shape of the tuple — and the order of elements within it — determines what Wolverine does. The table below extends ai-skills `marten-aggregate-handler-workflow` § Return types for events with the tuple combinations Cab handlers commonly use.

| Return type | What Wolverine does |
|---|---|
| `TEvent` | Appends the event to the loaded aggregate's stream. |
| `Events` | Appends multiple events to the stream (events is a `List<object>`). |
| `IStartStream` | Starts a new stream with the events specified to `MartenOps.StartStream<T>`. |
| `IMartenOp` (any single `IMartenOp`) | Side-effect; e.g., `MartenOps.Store<T>(doc)`, `MartenOps.Insert<T>(doc)`, `MartenOps.Delete<T>(id)`. |
| `IEnumerable<IMartenOp>` | Multiple side-effects. |
| `(TEvent, OutgoingMessages)` | Append event + publish integration messages atomically. **Most common Cab shape.** |
| `(IStartStream, OutgoingMessages)` | Start stream + publish integration messages atomically. **Most common Cab shape for new aggregates.** |
| `(IResult, ...)` | HTTP endpoint return — IResult always first. See `wolverine-http-handlers`. |
| `Task<T>` of any of the above | All return types support async via `Task<>`. |

Atomicity guarantee: when a handler returns a tuple combining persistence (events, `IStartStream`, `IMartenOp`) and outbox messages (`OutgoingMessages`), all of it commits in one Marten + Wolverine transaction. Either everything commits or nothing does.

For per-transport routing rule details on the `OutgoingMessages` side, see `wolverine-messaging-handlers`.

---

## Multi-Stream Handlers

A handler can write to or read from multiple aggregate streams. Each `[WriteAggregate]` and `[ReadAggregate]` parameter resolves independently.

```csharp
public sealed record TransferRiderToDriver(
    Guid TripId,
    Guid FromDriverId,
    Guid ToDriverId);

public static class TransferRiderToDriverHandler
{
    public static (Events, OutgoingMessages) Handle(
        TransferRiderToDriver cmd,
        [WriteAggregate(nameof(TransferRiderToDriver.TripId))] Trip trip,
        [ReadAggregate(nameof(TransferRiderToDriver.ToDriverId))] DriverProfile incomingDriver,
        TimeProvider time)
    {
        // Read incomingDriver to validate readiness; write to Trip.
        var events = new Events
        {
            new DriverChanged(cmd.FromDriverId, cmd.ToDriverId, time.GetUtcNow())
        };

        var outgoing = new OutgoingMessages();
        outgoing.Add(new Integration.TripDriverChanged(/* ... */));

        return (events, outgoing);
    }
}
```

`[ReadAggregate]` loads the aggregate without enrolling it for write. `[WriteAggregate]` loads with concurrency enforcement.

### Multi-write with separate `VersionSource`

When multiple `[WriteAggregate]` parameters share a single handler, each needs its own version variable for optimistic concurrency. Wolverine looks for a `version` parameter by default; specify `VersionSource` to override.

This pattern is rare and the substantive coverage lives in `dynamic-consistency-boundary` (Phase 2). For Cab's typical aggregate handler, single `[WriteAggregate]` is the shape.

---

## Stream Identity: Auto-Assigned vs Deterministic

The aggregate skill (`marten-aggregates` § Stream Identity) covers the principle: Wolverine assigns the stream ID at append time; `Create` reads it from `@event.StreamId`. This skill covers the two handler-side choices.

### Auto-assigned (default)

Most aggregates auto-assign. The handler returns `IStartStream` from `MartenOps.StartStream<T>(events)` with no ID argument; Wolverine generates a UUID v7 and writes it.

```csharp
public static (IStartStream, OutgoingMessages) Handle(StartTrip cmd, TimeProvider time)
{
    var started = new TripStarted(/* ... */);
    var stream = MartenOps.StartStream<Trip>(started);   // Wolverine assigns the ID
    /* ... */
}
```

### Deterministic via UUID v5

Some aggregates have natural keys that need ID stability across multiple producers — typically when the same external key (Entra user ID, SKU+warehouse, license plate) needs to converge on the same stream regardless of which producer creates it first.

```csharp
public static (IStartStream, OutgoingMessages) Handle(SubmitDriverApplication cmd, TimeProvider time)
{
    var streamId = DriverApplicationStreamId.Compute(cmd.EntraUserId);

    var submitted = new DriverApplicationSubmitted(/* ... */, time.GetUtcNow());
    var stream = MartenOps.StartStream<DriverApplication>(streamId, submitted);

    /* ... */
}

// Stream-ID computation lives on a static helper class for the aggregate.
public static class DriverApplicationStreamId
{
    private static readonly Guid Namespace =
        Guid.Parse("3f8a2c1e-b4d6-4f7a-9c2e-8d1f5b3a6e8d"); // generate once per aggregate type

    public static Guid Compute(Guid entraUserId) =>
        Uuid.NewNameBased(Namespace, entraUserId.ToString());
}
```

The `Uuid.NewNameBased(...)` call uses the [UUIDNext](https://www.nuget.org/packages/UUIDNext) library, which is the convention pick for UUID v5 in CritterCab — .NET 10 doesn't have `Guid.CreateVersion5` natively yet, and UUIDNext is actively maintained and supports .NET 10's file-based applications explicitly.

> **Don't use MD5-based deterministic IDs.** MD5 is cryptographically weak and the legacy pattern (hashing key strings into a `new Guid(hash)` constructor) doesn't produce a valid UUID v5 — the version and variant bits aren't set correctly per RFC 9562. UUID v5 (SHA-1, with proper version/variant marking) is the right primitive.

For the UUID v7 vs v5 decision and the broader GUID conventions, see `csharp-coding-standards` § GUIDs.

---

## Anti-Pattern: Manual Session Calls Inside the Handler

Calling `session.Events.Append(...)` or `session.SaveChangesAsync(...)` directly inside an aggregate handler does not work — Wolverine's code generation intercepts persistence based on the handler's *return value*, and manual session calls bypass interception entirely. Same root cause as the "Starting a new stream without IStartStream" anti-pattern from `wolverine-handlers`.

```csharp
// ❌ WRONG — events silently dropped
public static async Task Handle(
    CompleteTrip cmd,
    [WriteAggregate(nameof(CompleteTrip.TripId))] Trip trip,
    IDocumentSession session)
{
    session.Events.Append(trip.Id, new TripCompleted(/* ... */));   // not committed
    await session.SaveChangesAsync();   // also not what runs
}

// ✅ CORRECT — return the event(s); Wolverine handles persistence
public static TripCompleted Handle(
    CompleteTrip cmd,
    [WriteAggregate(nameof(CompleteTrip.TripId))] Trip trip) =>
    new TripCompleted(/* ... */);
```

If a handler genuinely needs `IDocumentSession` for a query that can't be expressed via `[ReadAggregate]` or LINQ helpers, the session is still injectable — just don't try to write to it. See ai-skills `marten-aggregate-handler-workflow` § Common anti-patterns for the parallel "Manually calling FetchForWriting when Wolverine does it for you" framing.

---

## Anti-Pattern: Generating the Stream ID for an Auto-Assigned Aggregate

```csharp
// ❌ WRONG — generating a UUID v7 manually when Wolverine would assign one
public static (IStartStream, OutgoingMessages) Handle(StartTrip cmd, TimeProvider time)
{
    var tripId = Guid.CreateVersion7();   // unnecessary
    var stream = MartenOps.StartStream<Trip>(tripId, new TripStarted(/* ... */));
    /* ... */
}

// ✅ CORRECT — let Wolverine assign
public static (IStartStream, OutgoingMessages) Handle(StartTrip cmd, TimeProvider time)
{
    var stream = MartenOps.StartStream<Trip>(new TripStarted(/* ... */));
    /* ... */
}
```

The auto-assignment path produces UUID v7 (time-ordered, database-friendly). Generating one manually is no faster, no more correct, and adds a noise variable to the handler. Reserve manual ID generation for deterministic-key aggregates (UUID v5).

If the handler needs to know the assigned ID before returning (to populate an integration event, for example), Wolverine exposes the assigned ID via the `Action`-overload of `MartenOps.StartStream`:

```csharp
public static (IStartStream, OutgoingMessages) Handle(StartTrip cmd, TimeProvider time)
{
    Guid assignedId = default;

    var stream = MartenOps.StartStream<Trip>(s =>
    {
        assignedId = s.Id;   // captured during Wolverine's persistence
    }, new TripStarted(/* ... */));

    var outgoing = new OutgoingMessages();
    // assignedId is populated by the time the message is enqueued
    outgoing.Add(new Integration.TripStarted(assignedId, /* ... */));

    return (stream, outgoing);
}
```

This pattern is used sparingly. Most handlers don't need the ID in the integration message body — the `Integration/` event payload usually carries the `TripId` field that the consumer reads from `@event.StreamId` once it's loaded.

---

## Anti-Pattern: `[WriteAggregate]` on a Handler That Doesn't Append

If a handler takes `[WriteAggregate]` but returns nothing aggregate-related, it's likely the wrong attribute. `[WriteAggregate]` carries optimistic-concurrency overhead; `[ReadAggregate]` doesn't.

```csharp
// ❌ WRONG — handler reads but never writes; concurrency check is wasted
public static class GetTripSummaryHandler
{
    public static TripSummary Handle(
        GetTripSummary q,
        [WriteAggregate(nameof(GetTripSummary.TripId))] Trip trip)
    {
        return new TripSummary(trip.Id, trip.Status, /* ... */);
    }
}

// ✅ CORRECT — read-only access via [ReadAggregate]
public static class GetTripSummaryHandler
{
    public static TripSummary Handle(
        GetTripSummary q,
        [ReadAggregate(nameof(GetTripSummary.TripId))] Trip trip)
    {
        return new TripSummary(trip.Id, trip.Status, /* ... */);
    }
}
```

The exception is `AlwaysEnforceConsistency = true` — a deliberate read-with-isolation. Use `[ReadAggregate]` everywhere else for queries.

---

## Worked Example: AcceptOffer (existing-stream + integration event)

A representative Dispatch handler. Loads `RideOffer`, validates the state machine, appends `OfferAccepted`, publishes the integration event for Trips to start a new trip.

```csharp
namespace CritterCab.Dispatch;

public sealed record AcceptOffer(Guid OfferId, Guid DriverId)
{
    public sealed class AcceptOfferValidator : AbstractValidator<AcceptOffer>
    {
        public AcceptOfferValidator()
        {
            RuleFor(x => x.OfferId).NotEmpty();
            RuleFor(x => x.DriverId).NotEmpty();
        }
    }
}

public static class AcceptOfferHandler
{
    public static ProblemDetails Validate(AcceptOffer cmd, RideOffer offer, TimeProvider time)
    {
        if (offer.Status != OfferStatus.Pending)
            return new ProblemDetails { Detail = "Offer is no longer pending", Status = 409 };
        if (time.GetUtcNow() > offer.ExpiresAt)
            return new ProblemDetails { Detail = "Offer has expired", Status = 409 };
        if (offer.DriverId != cmd.DriverId)
            return new ProblemDetails { Detail = "Offer was dispatched to a different driver", Status = 403 };
        return WolverineContinue.NoProblems;
    }

    public static (OfferAccepted, OutgoingMessages) Handle(
        AcceptOffer cmd,
        [WriteAggregate(nameof(AcceptOffer.OfferId))] RideOffer offer,
        TimeProvider time)
    {
        var accepted = new OfferAccepted(AcceptedAt: time.GetUtcNow());

        var outgoing = new OutgoingMessages();
        outgoing.Add(new Integration.OfferAccepted(
            offer.Id, offer.RideRequestId, cmd.DriverId, offer.PickupLocation, time.GetUtcNow()));

        return (accepted, outgoing);
    }
}
```

What this handler illustrates:

- **`Validate` runs first**, with both the command and the loaded `RideOffer` available. Three independent state checks; each returns a domain-meaningful HTTP status.
- **`Handle` is the happy path.** No defensive null checks, no re-validation. By the time `Handle` runs, the offer is loaded, the state is valid, and the command is authorized.
- **The domain event (`OfferAccepted`) is slim.** Just `AcceptedAt` — everything else (offer ID, driver ID, ride request ID) is reconstructible from the stream.
- **The integration event is rich.** Carries `RideRequestId`, `DriverId`, and `PickupLocation` so the Trips service can start a new trip stream without a callback to Dispatch.
- **Atomic commit.** The `OfferAccepted` event lands in the `RideOffer` stream and the `Integration.OfferAccepted` enrolls in the outbox — both commit together or both roll back.

---

## Common Pitfalls

- **Calling `session.Events.Append` or `session.SaveChangesAsync` directly.** Bypasses Wolverine's persistence interception. Return events from the handler instead.
- **Generating UUID v7 manually for new streams.** Unnecessary noise. Let Wolverine assign. Reserve manual generation for UUID v5 deterministic IDs.
- **Using `[WriteAggregate]` on a query handler.** Carries concurrency-check overhead with no benefit. Use `[ReadAggregate]` for read-only access.
- **Forgetting the routing rule for the integration event.** Most consequential silent failure in CritterCab — see `wolverine-messaging-handlers` § The Routing Rule Pre-Flight. Every `OutgoingMessages.Add(new SomeIntegrationEvent(...))` must have a matching `opts.PublishMessage<SomeIntegrationEvent>()` rule in the publishing service's `Program.cs`.
- **Wrong tuple order on HTTP-aggregate handlers.** `(IResult, TEvent, OutgoingMessages)` not `(TEvent, IResult, OutgoingMessages)` — see `wolverine-http-handlers` § Wrong Tuple Order on HTTP Endpoints.
- **MD5-based deterministic stream IDs.** Cryptographically weak; doesn't produce a valid UUID v5 per RFC 9562. Use UUIDNext's `Uuid.NewNameBased(namespace, name)`.
- **Setting `AlwaysEnforceConsistency = true` reflexively.** Adds a write-time round-trip even when the handler emitted no events. Use only when the handler's decision depends on the aggregate not having advanced.
- **Trying to throw a domain exception from `Apply`.** Wrong layer. Validation lives in the handler's `Validate`; `Apply` is pure evolution. See `marten-aggregates` § Apply Method Conventions.
- **Loading the aggregate manually via `IDocumentSession` to side-step `[WriteAggregate]`.** Loses the optimistic-concurrency guarantee and complicates testing. Use the attribute.

---

## See also

**Upstream** — generic Wolverine + Marten aggregate-handler mechanics this skill defers to. ai-skills (license required, install via `npx skills add`):

- `marten-aggregate-handler-workflow` — the full Marten + Wolverine aggregate handler workflow: FetchForWriting automation, `[WriteAggregate]` vs `[AggregateHandler]`, return types, optimistic concurrency via Version property + VersionSource override, multi-stream patterns, missing-aggregate handling (Required/OnMissing/MissingMessage), HTTP `[Aggregate]` integration, ProblemDetails validation, testing patterns (StubEventStream).
- `wolverine-handlers-declarative-persistence` — broader `[Entity]`/`[WriteAggregate]`/`[ReadAggregate]` declarative-persistence surface.
- `wolverine-handlers-fundamentals` — generic handler shape, return-types overview, IoC patterns.

**Prerequisites** — Cab-internal skills to load first if unfamiliar:

- `wolverine-handlers` — general handler shape, validation pipeline, return-types overview, the `IStartStream` general pattern, lambda-factory anti-pattern.
- `marten-aggregates` — the aggregate side: `sealed record`, static `Create(IEvent<T>)`, static `Apply(@event, current)`, no business validation on the aggregate.
- `domain-event-conventions` — slim domain events vs. rich integration events.
- `csharp-coding-standards` — sealed records, `TimeProvider`, `Immutable*` collections.

**Sibling skills:**

- `wolverine-http-handlers` — HTTP-specific patterns layered on aggregate handlers (`[EmptyResponse]`, IResult-first tuple order).
- `wolverine-messaging-handlers` — routing rules, `OutgoingMessages` outbox semantics — load alongside this skill when authoring any handler that publishes integration events.
- `dynamic-consistency-boundary` — multi-stream DCB writes that span aggregate boundaries (Phase 2).

**Downstream** — natural follow-ups:

- `marten-projections` — inline projections that snapshot aggregates; how the snapshot enum landed in `service-bootstrap` connects (Phase 2).
- `marten-querying` — querying aggregates via LINQ (Phase 2).
- `marten-async-daemon` — async-daemon configuration (Phase 2).
- `wolverine-sagas` — saga state machines using a similar concurrency model (Phase 4).
- `polecat-event-sourcing` — `PolecatOps.StartStream` and the parallel API surface (Phase 4).
- `cli-jasperfx` — `db-apply`, `codegen-preview`, the diagnostic CLI (Phase 2).

**External:**

- [Marten Event Sourcing Documentation](https://martendb.io/events/).
- [Wolverine Aggregate Handler Workflow](https://wolverinefx.net/guide/durability/marten/event-sourcing/).
- [UUIDNext](https://www.nuget.org/packages/UUIDNext) — the convention library for UUID v5 in CritterCab.
- [Jérémie Chassaing — Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider).
