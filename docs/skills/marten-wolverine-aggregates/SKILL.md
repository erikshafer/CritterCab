---
name: marten-wolverine-aggregates
description: "The handler-side of the Critter Stack decider pattern: [WriteAggregate] for loading, MartenOps.StartStream for first events, optimistic concurrency, and the cascading return shapes that commit events + integration messages atomically. Also covers the two shipped write paths that sit outside the aggregate-handler workflow and call the session directly: the configuration-as-events singleton (IInitialData seed + last-writer-wins reconfigure) and plain non-event-sourced documents (Store, HardDeleteWhere, numeric-revision merges). Use when authoring or reviewing any aggregate command handler, a config-as-events singleton, or a handler that writes a plain Marten document."
cluster: marten
tags: [marten, wolverine, handlers, aggregates, decider-pattern, write-aggregate, martenops, optimistic-concurrency, config-as-events, iinitialdata, documents, numeric-revisions]
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
- Adding operator-tunable configuration to a service as a configuration-as-events singleton (seed + reconfigure) — see § Configuration-as-Events Seed (Singleton Stream).
- Writing a plain Marten document from a handler, including inside an otherwise event-sourced service — see § Plain Documents (Not Event-Sourced).

Do NOT use this skill for:

- Aggregate shape, `Apply` methods, `Create` method — see `marten-aggregates`.
- General handler shape, validation pipeline, return-type orientation — see `wolverine-handlers`.
- HTTP-endpoint patterns layered on top of aggregate handlers — see `wolverine-http-handlers`.
- Routing rules for outbound integration messages — see `wolverine-messaging-handlers`.
- Polecat aggregate handlers — see `polecat-event-sourcing` (archived); the API parallels Marten's via `PolecatOps`.
- Multi-stream DCB writes that don't load a single aggregate — see `dynamic-consistency-boundary` (archived).

---

## Namespaces

| Type | Namespace |
|---|---|
| `IEvent<T>` | `JasperFx.Events` (extracted from `Marten.Events` in Marten 8.0, January 2026) |

Other primitives this skill uses (`MartenOps`, `[WriteAggregate]`, `[ReadAggregate]`, `OutgoingMessages`, etc.) live in their respective Wolverine namespaces; see ai-skills `marten-aggregate-handler-workflow` for the full namespace surface.

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

If `true`, Marten enforces the optimistic-concurrency check **even if the handler decides not to emit any new events.** This is the underlying flag for the read-then-decide pattern; in practice Cab uses the attribute shortcuts `[ConsistentAggregate]` (parameter-level) and `[ConsistentAggregateHandler]` (handler-level) instead of setting this flag directly. See § `[ConsistentAggregate]` and `[ConsistentAggregateHandler]` below for the preferred surface, decision criteria, and Cab BC scenarios.

```csharp
[WriteAggregate(nameof(Cmd.Id), AlwaysEnforceConsistency = true)] SomeAggregate agg
```

Default is `false`.

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

This pattern is rare and the substantive coverage lives in `dynamic-consistency-boundary` (archived). For Cab's typical aggregate handler, single `[WriteAggregate]` is the shape.

---

## `[ConsistentAggregate]` and `[ConsistentAggregateHandler]` — read-then-decide consistency

`AlwaysEnforceConsistency = true` (above) is the underlying flag. In practice Cab uses the **attribute-driven shortcuts** that wrap it:

- `[ConsistentAggregateHandler]` at the handler/class level — equivalent to `[AggregateHandler(AlwaysEnforceConsistency = true)]`.
- `[ConsistentAggregate]` at the parameter level — equivalent to `[WriteAggregate(...)]` with `AlwaysEnforceConsistency = true`.

Both attributes are for handlers where the aggregate is **read for a decision** but the decision may produce zero events. Without consistency enforcement on the no-event path, a concurrent change between load and decision is silently overruled — the handler reports success against state that no longer exists. The `[Consistent…]` attributes close that hole.

### When to use which

| Need | Attribute |
|---|---|
| Append events based on aggregate state | `[WriteAggregate]` (default — version check fires automatically when events are appended) |
| Read state, never append, just need the snapshot | `[ReadAggregate]` (no lock, no consistency check) |
| Read state, sometimes append nothing, **still need concurrency guarantee** on the no-op path | **`[ConsistentAggregate]`** (parameter-level) or **`[ConsistentAggregateHandler]`** (whole class) |

`[ConsistentAggregateHandler]` applies to every aggregate parameter on the handler class — use it when *all* aggregate loads in the handler are for read-then-decide. `[ConsistentAggregate]` applies to a single parameter — use it when one parameter is a primary write and another is read-only validation that still needs version protection.

### Handler-level: `[ConsistentAggregateHandler]`

The idempotency-guard pattern. A handler can be invoked twice for the same logical action (network retry, at-least-once delivery from a driver mobile app); the second invocation should be a no-op, but the no-op decision still has to be based on a current view of the aggregate.

```csharp
namespace CritterCab.Dispatch;

public sealed record ConfirmDriverArrival(Guid OfferId);

[ConsistentAggregateHandler]
public static class ConfirmDriverArrivalHandler
{
    public static IEnumerable<object> Handle(ConfirmDriverArrival cmd, RideOffer offer)
    {
        // Already confirmed (retry, late callback) → no-op.
        // Already cancelled or expired → also no-op (race lost).
        if (offer.Status is OfferStatus.DriverArrived
            or OfferStatus.Cancelled
            or OfferStatus.Expired)
            yield break;

        yield return new DriverArrivedAtPickup(cmd.OfferId, DateTimeOffset.UtcNow);
    }
}
```

The `yield break` no-op branches still get a version check at save time. Without `[ConsistentAggregateHandler]`, a concurrent `OfferCancelled` (the rider cancelled while the driver was arriving) that lands between load and decision is silently overruled — the handler reports success on an offer that no longer exists in the state we read.

`[ConsistentAggregateHandler]` is shorthand for `[AggregateHandler(AlwaysEnforceConsistency = true)]`. Both forms are equivalent; prefer `[ConsistentAggregateHandler]` for readability.

### Parameter-level: `[ConsistentAggregate]`

The protect-the-secondary-stream pattern. A handler may load a primary stream to write events plus a secondary stream purely to read and validate. Events are only appended to the primary, but the secondary still needs a version check so a concurrent change can't silently invalidate the precondition.

```csharp
namespace CritterCab.Trips;

public sealed record AssignDriverToTrip(Guid TripId, Guid DriverId);

public static class AssignDriverToTripHandler
{
    public static (DriverAssigned, OutgoingMessages) Handle(
        AssignDriverToTrip cmd,
        [WriteAggregate(nameof(AssignDriverToTrip.TripId))] Trip trip,
        [ConsistentAggregate(nameof(AssignDriverToTrip.DriverId))] Driver driver,
        TimeProvider time)
    {
        if (driver.Status is not DriverStatus.Available)
            throw new InvalidOperationException("Driver is not available for assignment.");

        // Events appended only to Trip stream. Driver stream is read for the
        // precondition; [ConsistentAggregate] keeps the version check active so
        // a concurrent DriverSuspended that races with this assignment fails
        // with ConcurrencyException instead of being silently overruled.
        var assigned = new DriverAssigned(cmd.DriverId, time.GetUtcNow());

        var outgoing = new OutgoingMessages();
        outgoing.Add(new Integration.DriverAssignedToTrip(cmd.TripId, cmd.DriverId));

        return (assigned, outgoing);
    }
}
```

Plain `[WriteAggregate]` on `driver` would work for the read but skip the version check on the no-event path (no events ever land on `driver` here). Plain `[ReadAggregate]` would skip the version check entirely. `[ConsistentAggregate]` is the right tool: load + version-check at save time, no events appended.

### Two more patterns where these earn their place

- **Authorization precondition** (Onboarding BC, `ApproveDriverApplication` on the `DriverApplication` aggregate). Approval only proceeds when all required documents are `Verified`. The decline path (`yield break` when some doc is still `Pending`) must run under `[ConsistentAggregateHandler]` — a concurrent `DocumentRejected` that lands between load and decision should invalidate the no-op rather than be silently overruled.
- **Capacity / reservation** (Dispatch BC, `AcceptZoneAssignment` on the `DispatchZone` aggregate). The zone has `MaxConcurrentDrivers`. The decline path (`yield break` when capacity is full) must run under `[ConsistentAggregateHandler]` — a concurrent `DriverLeftZone` that lands just before the decline should invalidate "zone is full" against state that's already changed and free a slot.

In both, the dangerous branch is the no-op. The append branch is already covered by the default `[WriteAggregate]` version check; the `[Consistent…]` attribute extends that protection to the no-event paths.

### Cross-store: same shape on Polecat

`[ConsistentAggregate]` and `[ConsistentAggregateHandler]` work identically for Polecat-backed handlers (Cab Payments BC). The attribute names, semantics, and `JasperFx.ConcurrencyException` retry pattern transfer unchanged. See `polecat-event-sourcing` (archived) § Wolverine integration for the Polecat-side handler shape and ai-skills `polecat-cross-stream-operations` for the canonical scenario set these patterns are drawn from.

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

## Configuration-as-Events Seed (Singleton Stream)

Operator-tunable parameters live as **configuration-as-events** ([ADR-011](../../decisions/011-configuration-as-events-bootstrap.md), including its 2026-07-10 amendment): one event-sourced singleton stream per bounded context, at a well-known id, carrying full-replacement `<BC>PolicyConfigured` events. Every change is an audited event; the current policy is the fold of the stream. Because the stream is a singleton with no identity on the command, it does **not** use the aggregate-handler workflow — both the seed and the reconfigure write call the session directly. This section is one of the two sanctioned exceptions to § Anti-Pattern: Manual Session Calls Inside an Aggregate Handler.

The reference implementation is Telemetry's throttle policy, `src/CritterCab.Telemetry/TelemetryPolicy/`:

| Piece | File | Role |
|---|---|---|
| `TelemetryPolicyStream.Id` | `TelemetryPolicyStream.cs` | The singleton's fixed stream id. |
| `TelemetryPolicyConfigured` | `TelemetryPolicyConfigured.cs` | The full-replacement event. No aggregate-id field — the stream is well-known. |
| `TelemetryPolicy` | `TelemetryPolicy.cs` | The aggregate, which is also the view (self-aggregating). |
| `TelemetryPolicyBootstrap` | `TelemetryPolicyBootstrap.cs` | The `IInitialData` seed. |
| `ConfigureTelemetryPolicy` + `ConfigureTelemetryPolicyEndpoint` | `ConfigureTelemetryPolicy.cs` | The operator command, its boundary validator, and the reconfigure endpoint. |

### A fixed stream id

```csharp
// src/CritterCab.Telemetry/TelemetryPolicy/TelemetryPolicyStream.cs
public static class TelemetryPolicyStream
{
    public static readonly Guid Id = Guid.Parse("7e1e3e77-b0b0-4a5a-9c9c-000000000001");
}
```

A true singleton needs no lookup and no natural key to hash, so a constant is enough — the seed, the reconfigure endpoint, and every reader resolve the same stream directly. This is the degenerate case of § Deterministic via UUID v5: reach for UUID v5 when there is a key to converge on, a constant when there is exactly one stream.

### The view is the aggregate

```csharp
// src/CritterCab.Telemetry/Program.cs
opts.Events.AddEventType<TelemetryPolicyConfigured>();
opts.Projections.LiveStreamAggregation<TelemetryPolicy>();
```

`TelemetryPolicy` is a `sealed record` with a static `Create(IEvent<TelemetryPolicyConfigured>)` and a static `Apply` that replaces every field with `with` — full replacement means only the latest event's values survive. It registers as its own live-stream aggregation: no separate projection class, and no `partial` (Marten 9's source generation requires `partial` only for subclassed projections, not self-aggregating aggregates). Live aggregation makes a reconfigure visible read-after-write.

The aggregate carries `public long Version { get; set; }`, populated by Marten's name convention with the stream version. `long`, not `int`, because it travels to the proto's `int64 throttle_policy_version` as the policy version clients see. Readers fold the stream on demand:

```csharp
var policy = await session.Events.AggregateStreamAsync<TelemetryPolicy>(
    TelemetryPolicyStream.Id, token: ct);
```

`ReportLocationsHandler` and `EvictStalePositionsHandler` both read the policy this way, and both treat `null` as "the seed has not run" rather than guessing defaults.

### The seed: `IInitialData` with an idempotent guard

```csharp
// src/CritterCab.Telemetry/TelemetryPolicy/TelemetryPolicyBootstrap.cs
public sealed class TelemetryPolicyBootstrap : IInitialData
{
    public const int DefaultH3Resolution = 9;
    public const int DefaultHeartbeatIntervalSeconds = 30;
    public const int DefaultMinPublishIntervalSeconds = 5;

    public async Task Populate(IDocumentStore store, CancellationToken cancellation)
    {
        await using var session = store.LightweightSession();

        var state = await session.Events.FetchStreamStateAsync(TelemetryPolicyStream.Id, cancellation);
        if (state is not null)
            return; // already seeded — idempotent no-op

        // Seeders may read the wall clock directly (csharp-coding-standards § TimeProvider).
        var seed = new TelemetryPolicyConfigured(
            H3Resolution: DefaultH3Resolution,
            HeartbeatIntervalSeconds: DefaultHeartbeatIntervalSeconds,
            MinPublishIntervalSeconds: DefaultMinPublishIntervalSeconds,
            OperatorId: "system-bootstrap",
            Reason: "Initial deployment defaults",
            ConfiguredAt: DateTimeOffset.UtcNow);

        session.Events.StartStream<TelemetryPolicy>(TelemetryPolicyStream.Id, seed);
        await session.SaveChangesAsync(cancellation);
    }
}
```

```csharp
// src/CritterCab.Telemetry/Program.cs
builder.Services.AddMarten(opts => { /* ... */ })
    .IntegrateWithWolverine()
    .UseLightweightSessions()
    .InitializeWith<TelemetryPolicyBootstrap>();
```

- **`IInitialData` is the Marten realization of ADR-011's preferred option.** It runs at the deploy-time apply step and again, idempotently, at host start as a self-healing safety net.
- **The guard is `FetchStreamStateAsync` → return if non-null.** A stream that already exists is never re-seeded, so the seed never overwrites an operator's later configuration.
- **The multi-instance seed race is mitigated, not eliminated.** Two instances that both pass the guard append two identical seeds; full replacement makes them converge to the same policy (an extra version increment, not a divergent state). Running the deploy-time step before scaling out avoids the race.
- **Audit markers are fixed:** `OperatorId: "system-bootstrap"`, `Reason: "Initial deployment defaults"`. A reader of the stream can tell a seeded policy from an operator's.
- **Defaults are public constants on the bootstrap class**, so they are documented in one place and tests can assert against them.
- **The wall clock is read directly.** A seeder runs outside any request and has nothing to inject a `TimeProvider` into; `csharp-coding-standards` § `TimeProvider` for Testable Time permits it for seeders.
- **`session.Events.StartStream`, not `MartenOps.StartStream`.** No Wolverine handler is involved — `IInitialData` runs against the store outside Wolverine's pipeline — so there is no return value to intercept and the seed commits its own session.

### Reconfigure: manual append, last-writer-wins

```csharp
// src/CritterCab.Telemetry/TelemetryPolicy/ConfigureTelemetryPolicy.cs
public static class ConfigureTelemetryPolicyEndpoint
{
    [WolverinePost("/api/telemetry/policy")]
    public static async Task<TelemetryPolicyResponse> Handle(
        ConfigureTelemetryPolicy command,
        IDocumentSession session,
        TimeProvider time,
        CancellationToken ct)
    {
        var @event = new TelemetryPolicyConfigured(
            H3Resolution: command.H3Resolution,
            HeartbeatIntervalSeconds: command.HeartbeatIntervalSeconds,
            MinPublishIntervalSeconds: command.MinPublishIntervalSeconds,
            OperatorId: command.OperatorId,
            Reason: command.Reason,
            ConfiguredAt: time.GetUtcNow());

        session.Events.Append(TelemetryPolicyStream.Id, @event);
        await session.SaveChangesAsync(ct);

        var policy = await session.Events.AggregateStreamAsync<TelemetryPolicy>(TelemetryPolicyStream.Id, token: ct);
        return TelemetryPolicyResponse.From(policy!);
    }
}
```

- **No `[WriteAggregate]`.** The aggregate-handler workflow binds stream identity from a command property, route segment, header, or claim. The singleton's id is a constant that appears in none of those, so the workflow does not apply (ADR-011 amendment). The append goes to the well-known id directly.
- **No expected version — last-writer-wins is the intended semantic.** Each event is a full-replacement record and there is no state-transition invariant to defend: a later configuration always wins. Do not add an expected-version append or a `[ConsistentAggregate]` reflexively; there is no conflict for it to detect.
- **`ConfiguredAt` is server-stamped from `TimeProvider`**, never supplied by the caller. Unlike the seeder, an endpoint has a `TimeProvider` to inject.
- **Validation is at the HTTP boundary.** `ConfigureTelemetryPolicy` nests an `AbstractValidator<ConfigureTelemetryPolicy>` holding the cross-parameter rules (resolution range, heartbeat ≥ publish floor); the aggregate stays thin. See `wolverine-http-handlers` § FluentValidation at the HTTP Boundary for the two-call wiring that makes the validator fire.
- **The endpoint commits its own work and returns the view.** Telemetry does not turn on `AutoApplyTransactions`, so a handler that takes `IDocumentSession` without an aggregate attribute calls `SaveChangesAsync` itself; it then folds the stream to return the new policy and its version.

### Tests: reset to the seed

The policy is a singleton, so every test in the collection shares one stream. Tests isolate by wiping event data and re-running the same `IInitialData` the host runs on startup:

```csharp
// tests/CritterCab.Telemetry.Tests/TelemetryTestFixture.cs
public async Task ResetToSeedAsync()
{
    var store = Host.Services.GetRequiredService<IDocumentStore>();
    await store.Advanced.Clean.DeleteAllEventDataAsync();
    await new TelemetryPolicyBootstrap().Populate(store, CancellationToken.None);
}
```

`DeleteAllEventDataAsync` clears events only. Plain documents in the same store (Telemetry's `LastKnownPosition`) survive it and need their own reset — see the next section.

---

## Plain Documents (Not Event-Sourced)

Not every write in a Marten-backed service is an event. A **plain document** — stored whole with `session.Store`, overwritten in place, no history — is the shape for state that defends no invariant and whose history nobody needs: a location-of-record, a local copy of another service's high-volume feed. The handler calls the session directly; this is the second sanctioned exception to § Anti-Pattern: Manual Session Calls Inside an Aggregate Handler.

Two shipped instances:

| Document | Service | Writers | Concurrency |
|---|---|---|---|
| `LastKnownPosition` (`src/CritterCab.Telemetry/LastKnownPosition/`) | Telemetry — a stream-processing BC whose only event-sourced stream is the policy singleton | One per document: a driver's own pings | None. Last-writer-wins. |
| `AvailableDriver` (`src/CritterCab.Dispatch/AvailableDrivers/`) | Dispatch — otherwise fully event-sourced | Two per document: the location side and the availability side | Numeric revision as a concurrency token, plus a Wolverine retry policy. |

### Choosing a document over a stream or a projection

`AvailableDriver` is the instructive case, because it sits inside CritterCab's most event-sourced service and is deliberately none of it — not a stream, not a projection. Dispatch receives driver positions from Telemetry's Kafka feed. Event-sourcing that inbound feed would reimport onto Dispatch's event store exactly the per-ping volume Telemetry's throttle exists to suppress (W006 §6.5; the reasoning is recorded at `src/CritterCab.Dispatch/Program.cs:49-52`). It is not a projection either: there is no Dispatch event stream behind it to project from. The stream-processing shape crosses the bounded-context boundary along with the data.

Reach for a plain document when all of these hold: the state defends no business invariant, its history has no audit or replay value, and the write rate would make an event stream a liability. If any fails, the state belongs in an aggregate (`marten-aggregates`) or a projection (`marten-projections`).

### Shape

```csharp
// src/CritterCab.Telemetry/LastKnownPosition/LastKnownPosition.cs
public sealed record LastKnownPosition
{
    public required Guid Id { get; init; }   // driverId — document identity by convention
    public required double Lat { get; init; }
    public required double Lon { get; init; }
    public required string H3Cell { get; init; }
    public required DateTimeOffset ServerReceivedAt { get; init; }
}
```

- **`sealed record` with `required` init properties.** Every write replaces the whole row, so nothing is mutated in place and immutability costs nothing.
- **`Id` by convention.** Marten takes the `Id` property as document identity; "one document per driver" falls out of the shape. The id is an external key that arrives with the request, so it is never minted — no `Guid.CreateVersion7()`.
- **No `partial`.** Nothing about a plain document is source-generated.
- **No schema registration** unless the document needs one. `LastKnownPosition` has none; `AvailableDriver` registers duplicated columns and numeric revisions (below).

### One writer: `Store` and last-writer-wins

```csharp
// src/CritterCab.Telemetry/ReportLocations/ReportLocationsHandler.cs
baseline = new LastKnownPositionDocument
{
    Id = driverId,
    Lat = ping.Lat,
    Lon = ping.Lon,
    H3Cell = h3Cell,
    ServerReceivedAt = serverReceivedAt
};

session.Store(baseline);
await session.SaveChangesAsync(ct);
```

`session.Store` is an upsert of the whole document. There is no concurrency control, and none is needed: a driver's own pings are the sole writer of that driver's document, so last-writer-wins *is* the concurrency story.

### Deleting: `HardDeleteWhere`

```csharp
// src/CritterCab.Telemetry/LastKnownPosition/EvictStalePositions.cs
session.HardDeleteWhere<LastKnownPosition>(x => x.ServerReceivedAt < threshold);
await session.SaveChangesAsync(ct);
```

Prefer `HardDeleteWhere` whenever the domain requires the row to be **gone**. `DeleteWhere` is conditional: if the type is ever configured for soft deletes, it silently switches to setting `mt_deleted` and the row survives. Here the domain needs an evicted driver to find no baseline and republish at once, so a future soft-delete configuration must not be able to change the outcome.

### Two writers: business ordering and write concurrency are separate guards

`AvailableDriver` has two sides with two writers on two clocks: the **location** side from Kafka (`DriverLocationUpdatedHandler`) and the **availability** side (`DriverAvailabilityChangedHandler`). Each handler loads the document, merges its own side, carries the other side forward, and writes the whole document back. That needs two different guards, and conflating them was a real bug.

**Registration:**

```csharp
// src/CritterCab.Dispatch/Program.cs
opts.Schema.For<AvailableDriver>()
    .Duplicate(x => x.H3Cell)
    .Duplicate(x => x.VehicleClass)
    .Duplicate(x => x.AvailabilityState)
    .Duplicate(x => x.ServerReceivedAt)
    .UseNumericRevisions(true);
```

- **Duplicated columns, not computed JSONB indexes.** The nearby-drivers query filters `H3Cell = ANY(...)` together with the availability predicates, and `= ANY` over a real indexed column is what makes a thousand-cell k-ring affordable in one round trip. `ServerReceivedAt` is duplicated because the read side sorts on it.
- **`UseNumericRevisions(true)` is load-bearing and silent if omitted.** Without it the revision guard degrades to an unguarded upsert, and the two writers can lost-update each other with no error anywhere.
- **The document implements `ILongVersioned`** (`public long Version { get; set; }`, a `bigint` revision column) — not `IRevisioned`, which is `int`.

**The location writer:**

```csharp
// src/CritterCab.Dispatch/AvailableDrivers/DriverLocationUpdatedHandler.cs
var existing = await session.LoadAsync<AvailableDriver>(driverId, ct);

// Business ordering: this side's own clock. Equality is the dedup no-op.
if (existing is not null && serverReceivedAt <= existing.ServerReceivedAt)
    return;

var updated = new AvailableDriver
{
    Id = driverId,
    H3Cell = message.H3Cell,
    H3Resolution = message.H3Resolution,
    Lat = message.Lat,
    Lon = message.Lon,
    ServerReceivedAt = serverReceivedAt,

    // Carried forward untouched.
    AvailabilityState = existing?.AvailabilityState,
    VehicleClass = existing?.VehicleClass,
    AvailabilityUpdatedAt = existing?.AvailabilityUpdatedAt
};

// Write concurrency: closes the window between LoadAsync and this write.
session.UpdateRevision(updated, (existing?.Version ?? 0) + 1);

await session.SaveChangesAsync(ct);
```

The availability writer (`DriverAvailabilityChanged.cs`) is the mirror image: it compares `AvailabilityUpdatedAt` against the stored value, builds `existing with { AvailabilityState = ..., VehicleClass = ..., AvailabilityUpdatedAt = ... }`, and calls `session.UpdateRevision(updated, existing.Version + 1)`.

| Guard | Problem it solves | Mechanism |
|---|---|---|
| Business ordering | A stale or duplicate message for one side (Kafka redelivers uncommitted offsets on a consumer-group rebalance) | Compare the incoming timestamp against **that side's own** stored timestamp; `<=` returns without writing. |
| Write concurrency | The other writer committing its side between this handler's `LoadAsync` and its write | `UpdateRevision(updated, existing.Version + 1)` — the revision is a plain incrementing token; a losing race throws `ConcurrencyException`. |

**Do not use a business timestamp as the revision.** An earlier cut used the location side's `ServerReceivedAt` as the Marten revision, which collapsed both sides onto one ordering key: a driver going Offline at 12:00:05 could have that write discarded by a heartbeat position stamped 12:00:07 that had already raised the revision — leaving an offline driver dispatchable, with no error anywhere. The account is in the comment at `DriverLocationUpdatedHandler.cs:49-63` and in [retrospective 009](../../retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md).

**The retry policy is the other half of the concurrency guard.** The revision turns a losing race into a `ConcurrencyException` instead of a silent lost update; the policy turns that exception into a re-run, so the handler reloads, re-evaluates its ordering guard, and re-merges against the winner's state:

```csharp
// src/CritterCab.Dispatch/Program.cs
// usings: Marten.Exceptions (ConcurrencyException), JasperFx.Core (.Milliseconds()),
//         Wolverine.ErrorHandling (OnException / RetryWithCooldown)
opts.Policies.OnException<ConcurrencyException>()
    .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());
```

Three short pauses, because the contention window is a single round trip; a conflict that survives three retries is a symptom of something else.

### Read side

Writers load through their `IDocumentSession`; a read-only consumer takes `IQuerySession`. `NearbyAvailableDriversView(IQuerySession session)` implements Dispatch's `INearbyAvailableDriversSource` port and is registered **scoped** (it depends on a scoped session) when a connection string is present. Its k-ring query uses `cells.Contains(d.H3Cell)`, which Marten translates to a single `= ANY(:param)` array parameter against the duplicated column. See `marten-querying` for the query shapes.

### Tests

A plain document is not event data, so `DeleteAllEventDataAsync` does not clear it. Telemetry's fixture resets it separately with `store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(LastKnownPosition))` (`tests/CritterCab.Telemetry.Tests/TelemetryTestFixture.cs`).

---

## Anti-Pattern: Manual Session Calls Inside an Aggregate Handler

**Scope:** this applies to handlers in the aggregate-handler workflow — a `[WriteAggregate]`, `[ConsistentAggregate]`, or `[ReadAggregate]` parameter, `[AggregateHandler]`/`[ConsistentAggregateHandler]` on the class, or a handler that starts a stream by returning `IStartStream`. In those handlers Wolverine owns loading and persistence. It does **not** apply to the two shipped write paths that sit outside the workflow and call the session by design: § Configuration-as-Events Seed (Singleton Stream) and § Plain Documents (Not Event-Sourced).

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

If an aggregate handler genuinely needs `IDocumentSession` for a query that can't be expressed via `[ReadAggregate]` or LINQ helpers, the session is still injectable — just don't try to write the aggregate's stream through it. See ai-skills `marten-aggregate-handler-workflow` § Common anti-patterns for the parallel "Manually calling FetchForWriting when Wolverine does it for you" framing.

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

- **Calling `session.Events.Append` or `session.SaveChangesAsync` directly in an aggregate-workflow handler.** Bypasses Wolverine's persistence interception. Return events from the handler instead. Out of scope: the config-as-events seed and reconfigure, and plain-document writers, which call the session by design — see § Configuration-as-Events Seed (Singleton Stream) and § Plain Documents (Not Event-Sourced).
- **Generating UUID v7 manually for new streams.** Unnecessary noise. Let Wolverine assign. Reserve manual generation for UUID v5 deterministic IDs.
- **Using `[WriteAggregate]` on a query handler.** Carries concurrency-check overhead with no benefit. Use `[ReadAggregate]` for read-only access.
- **Forgetting the routing rule for the integration event.** Most consequential silent failure in CritterCab — see `wolverine-messaging-handlers` § The Routing Rule Pre-Flight. Every `OutgoingMessages.Add(new SomeIntegrationEvent(...))` must have a matching `opts.PublishMessage<SomeIntegrationEvent>()` rule in the publishing service's `Program.cs`.
- **Wrong tuple order on HTTP-aggregate handlers.** `(IResult, TEvent, OutgoingMessages)` not `(TEvent, IResult, OutgoingMessages)` — see `wolverine-http-handlers` § Wrong Tuple Order on HTTP Endpoints.
- **MD5-based deterministic stream IDs.** Cryptographically weak; doesn't produce a valid UUID v5 per RFC 9562. Use UUIDNext's `Uuid.NewNameBased(namespace, name)`.
- **Setting `AlwaysEnforceConsistency = true` reflexively.** Adds a write-time round-trip even when the handler emitted no events. Use only when the handler's decision depends on the aggregate not having advanced.
- **Trying to throw a domain exception from `Apply`.** Wrong layer. Validation lives in the handler's `Validate`; `Apply` is pure evolution. See `marten-aggregates` § Apply Method Conventions.
- **Loading the aggregate manually via `IDocumentSession` to side-step `[WriteAggregate]`.** Loses the optimistic-concurrency guarantee and complicates testing. Use the attribute whenever the stream id comes from the command, route, header, or claim. The config-as-events singleton is the exception (a constant id the workflow cannot bind, and last-writer-wins by design); plain documents are not aggregates and load with `session.LoadAsync<T>` (§ Plain Documents).
- **Adding an expected version to the config-singleton reconfigure.** Full-replacement events defend no invariant, so there is no conflict to detect; last-writer-wins is the intended semantic (ADR-011 amendment).
- **Using a business timestamp as a document's numeric revision.** With two writers on two clocks it lets one side's newer timestamp silently discard the other side's write. Order by each side's own timestamp; use the revision as a plain `existing.Version + 1` concurrency token, backed by a `ConcurrencyException` retry policy.
- **Registering a revision-guarded document without `UseNumericRevisions(true)`.** The guard degrades to an unguarded upsert with no error.
- **`DeleteWhere` when the row must be gone.** It silently becomes a soft delete if the type is ever configured for soft deletes. Use `HardDeleteWhere`.

---

## See also

**Upstream** — generic Wolverine + Marten aggregate-handler mechanics this skill defers to. ai-skills (license required, install via `npx skills add`):

- `marten-aggregate-handler-workflow` — the full Marten + Wolverine aggregate handler workflow: FetchForWriting automation, `[WriteAggregate]` vs `[AggregateHandler]`, return types, optimistic concurrency via Version property + VersionSource override, multi-stream patterns, missing-aggregate handling (Required/OnMissing/MissingMessage), HTTP `[Aggregate]` integration, ProblemDetails validation, testing patterns (StubEventStream).
- `wolverine-handlers-declarative-persistence` — broader `[Entity]`/`[WriteAggregate]`/`[ReadAggregate]` declarative-persistence surface.
- `wolverine-handlers-fundamentals` — generic handler shape, return-types overview, IoC patterns.
- `marten-migration-v8-to-v9` — the `IRevisioned` (`int`) vs `ILongVersioned` (`long`) versioning model behind `AvailableDriver`'s revision column.

**Prerequisites** — Cab-internal skills to load first if unfamiliar:

- `wolverine-handlers` — general handler shape, validation pipeline, return-types overview, the `IStartStream` general pattern, lambda-factory anti-pattern.
- `marten-aggregates` — the aggregate side: `sealed record`, static `Create(IEvent<T>)`, static `Apply(@event, current)`, no business validation on the aggregate.
- `domain-event-conventions` — slim domain events vs. rich integration events.
- `csharp-coding-standards` — sealed records, `TimeProvider`, `Immutable*` collections.

**Sibling skills:**

- `wolverine-http-handlers` — HTTP-specific patterns layered on aggregate handlers (`[EmptyResponse]`, IResult-first tuple order).
- `wolverine-messaging-handlers` — routing rules, `OutgoingMessages` outbox semantics — load alongside this skill when authoring any handler that publishes integration events.
- `dynamic-consistency-boundary` (archived) — multi-stream DCB writes that span aggregate boundaries.
- `wolverine-kafka` — the Kafka listener that feeds `AvailableDriver`'s location side.
- `wolverine-marten-automation` — § Recurring Work (Timer-Driven), the timer that raises the `LastKnownPosition` eviction sweep.

**Downstream** — natural follow-ups:

- `marten-projections` — inline projections that snapshot aggregates; how the snapshot enum landed in `service-bootstrap` connects (Phase 2).
- `marten-querying` — querying aggregates via LINQ (Phase 2).
- `marten-async-daemon` (archived) — async-daemon configuration.
- `wolverine-sagas` (archived) — saga state machines using a similar concurrency model.
- `polecat-event-sourcing` (archived) — `PolecatOps.StartStream` and the parallel API surface.
- `cli-jasperfx` — `db-apply`, `codegen-preview`, the diagnostic CLI (Phase 2).

**External:**

- [Marten Event Sourcing Documentation](https://martendb.io/events/).
- [Wolverine Aggregate Handler Workflow](https://wolverinefx.net/guide/durability/marten/event-sourcing/).
- [UUIDNext](https://www.nuget.org/packages/UUIDNext) — the convention library for UUID v5 in CritterCab.
- [Jérémie Chassaing — Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider).
