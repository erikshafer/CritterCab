---
name: dynamic-consistency-boundary
description: "Designing Dynamic Consistency Boundary (DCB) handlers in CritterCab — multi-stream consistency without sagas. Covers the canonical [BoundaryModel] pattern, the manual FetchForWritingByTags fallback, EventTagQuery construction, tag-type registration with the .NET-10 Guid-wrapper requirement, and DcbConcurrencyException handling. Use when a command's invariants span multiple event streams selected by query."
cluster: marten
tags: [marten, dcb, event-sourcing, multi-stream-decision, tags, boundary-model, wolverine]
---

# Dynamic Consistency Boundary (DCB)

Designing DCB handlers in CritterCab. The Dynamic Consistency Boundary pattern (Sara Pellegrini, [dcb.events](https://dcb.events/)) lets a single consistency boundary span **multiple event streams**, selected dynamically at command-handling time via **event tags** — multi-stream invariants enforced atomically without saga coordination.

The generic mechanics — the relational tag tables, sequence-number consistency assertions, codegen for `[BoundaryModel]` — live in the JasperFx ai-skills library and Marten's own DCB documentation. **This skill documents the patterns, gotchas, and Cab-specific decisions on top of those mechanics.** The aggregate-shape conventions live in `marten-aggregates`; handler-side `[WriteAggregate]` patterns live in `marten-wolverine-aggregates`; query-side concerns including session selection live in `marten-querying`.

## When to apply this skill

Use this skill when:

- A command's invariants genuinely span two or more event streams, and the streams are selected by query rather than by known IDs on the command.
- You're considering a saga to coordinate a single atomic decision across entities, and the saga feels like accidental complexity.
- Reviewing a handler that takes `[BoundaryModel]` or calls `FetchForWritingByTags<T>`.
- Registering a new tag type or boundary state class.

Do NOT use this skill for:

- Single-aggregate stream commands — use `marten-wolverine-aggregates` (`[WriteAggregate]`).
- Cross-stream commands where both stream IDs are on the command — use multiple `[WriteAggregate]` parameters per `marten-wolverine-aggregates`.
- Long-running coordination, retries, external side effects, or compensating actions — use `wolverine-sagas` (Phase 4).
- Polecat-side DCB — the API mirrors Marten's almost exactly (tag types, `EventTagQuery`, `IEventBoundary<T>`, `RegisterTagType` are all parallel), but the namespace is `Polecat.Events.Dcb`. Phase 4 `polecat-event-sourcing` will cover the small differences.

---

## When to Reach for DCB

DCB earns its place when a command must enforce invariants that naturally span two or more entities, and the streams involved are selected by query rather than by known IDs. The bar is real — DCB adds tag-table writes, multi-stream concurrency assertions, and a separate exception type to handle. Don't reach for it speculatively.

| Scenario | Tool |
|---|---|
| Single aggregate stream, single invariant boundary | `marten-aggregates` + `marten-wolverine-aggregates` (`[WriteAggregate]`) |
| Two or more **known** streams with IDs on the command | Multiple `[WriteAggregate]` parameters (see `marten-wolverine-aggregates`) |
| Multiple streams, one BC, one immediate decision, streams selected by tag query | **DCB** |
| Long-running workflow, cross-BC coordination, retries, external side effects | Saga / process manager |

The split that matters is **known IDs vs. dynamic selection**. If the command carries the stream IDs and you load fixed streams, multiple `[WriteAggregate]` parameters are simpler. If the set of streams is selected by a tag query at handler time ("any active offer for this driver", "every pending ride request in this region"), DCB is the right tool.

### Cab's primary DCB scenario: offer acceptance

When a driver accepts a dispatch offer, three invariants must hold simultaneously:

- The offer is still pending (Offer stream).
- The driver isn't already committed to a different active offer (Driver stream).
- The ride request hasn't been cancelled or already matched (Ride Request stream).

A saga coordinating Offer + Driver + RideRequest is overkill for a single atomic decision. DCB is the right fit: tag the relevant events with `RideOfferStreamId`, `DriverStreamId`, and `RideRequestStreamId`, project them into an `OfferAcceptanceState`, and let Marten enforce optimistic concurrency across all three tag axes at append time.

---

## Two Patterns, One Preferred

Wolverine exposes two ways to write a DCB handler. Pick deliberately:

- **Canonical — `[BoundaryModel]` pattern.** Attribute-driven. Two static methods (`Load` returning `EventTagQuery`, `Handle` taking a `[BoundaryModel]` state parameter), plus an optional pre-handler hook. Wolverine codegen wires up the `FetchForWritingByTags` call and the atomic append. **Default to this.**
- **Manual — `FetchForWritingByTags` pattern.** Imperative. A single `Handle` method taking `IDocumentSession` directly, calling `session.Events.FetchForWritingByTags<T>(query)`, and working against the returned `IEventBoundary<T>`. The escape hatch — useful when you need direct control (conditional appends, idempotency on `boundary.Aggregate is not null`, or events whose tag types aren't carried as native fields).

`[WriteAggregate]` also composes with DCB (the Wolverine repo demonstrates this side-by-side in `ChangeCourseCapacity.cs` — three handler shapes for one command), but it's not a DCB-specific entry point. Reserve `[WriteAggregate]` for classic per-stream loads; reach for `[BoundaryModel]` when the boundary spans tag queries.

---

## The Canonical `[BoundaryModel]` Pattern

Two static methods on the handler class. An optional pipeline hook for early bailout.

### `Load()` — return the `EventTagQuery`

```csharp
public static EventTagQuery Load(AcceptOffer command)
    => EventTagQuery
        .For(new RideOfferStreamId(command.OfferId))
        .AndEventsOfType<OfferDispatched, OfferAccepted, OfferRejected, OfferExpired>()
        .Or(new DriverStreamId(command.DriverId))
        .AndEventsOfType<OfferAccepted, OfferRejected, OfferExpired>()
        .Or(new RideRequestStreamId(command.RideRequestId))
        .AndEventsOfType<RideRequested, RideRequestCancelled, RideRequestMatched>();
```

`Load` is static, takes the command, returns `EventTagQuery`. Wolverine calls it before `Handle`, uses the result to fetch matching events, and projects them into the boundary state. Wolverine accepts `Load`, `LoadAsync`, `Before`, or `BeforeAsync` as the method name — Cab convention is `Load` for the tag-query method to match the canonical Wolverine examples.

### `Handle()` — take the boundary state, return events

```csharp
public static OfferAccepted Handle(
    AcceptOffer command,
    [BoundaryModel] OfferAcceptanceState state,
    TimeProvider time)
{
    if (state.OfferStatus is not OfferStatus.Pending)
        throw new InvalidOperationException("Offer is no longer pending.");

    if (state.DriverHasActiveCommitment)
        throw new InvalidOperationException("Driver is already committed to a different offer.");

    if (state.RideRequestStatus is RideRequestStatus.Cancelled)
        throw new InvalidOperationException("Ride request has been cancelled.");

    if (state.RideRequestStatus is RideRequestStatus.Matched)
        throw new InvalidOperationException("Ride request has already been matched.");

    return new OfferAccepted(
        command.OfferId,
        command.DriverId,
        command.RideRequestId,
        time.GetUtcNow());
}
```

The shape:

- **`[BoundaryModel]` goes on the state parameter of `Handle`** — never on a sibling method's state parameter.
- **Return the event(s) directly.** Wolverine codegen routes the return value through `IEventBoundary<T>.AppendOne` (or `AppendMany` for collections). No manual `boundary.AppendOne(...)` call required.
- **Decisions throw or return null.** Throwing `InvalidOperationException` is the loud-failure path for invariant violations; returning a nullable event (`OfferAccepted?`) is the silent no-op path for idempotent dispatches.
- **`TimeProvider` and other DI parameters are still resolved normally** — `[BoundaryModel]` doesn't restrict the parameter list otherwise.

### Return-value shapes Wolverine recognizes

| Return shape | Behavior |
|---|---|
| Single event object (`OfferAccepted`) | Appended via `AppendOne` |
| Nullable event (`OfferAccepted?`) | `null` is a no-op; nothing appended |
| `IEnumerable<object>` or `Events` | Each item appended via `AppendMany` |
| `IAsyncEnumerable<object>` | Async enumeration appended via `AppendMany` |
| `OutgoingMessages` | Cascading messages — not events; see `wolverine-messaging-handlers` |

The codegen path is verified in `Wolverine.Marten/BoundaryModelAttribute.cs` (the `DetermineEventCaptureHandling` method).

### Optional pipeline hook — `Validate()`

For pre-handler bailout that doesn't throw:

```csharp
public static HandlerContinuation Validate(
    AcceptOffer command,
    OfferAcceptanceState state,
    ILogger logger)
{
    if (state.OfferStatus is not OfferStatus.Pending)
    {
        logger.LogDebug("Offer {OfferId} is no longer pending; status={Status}",
            command.OfferId, state.OfferStatus);
        return HandlerContinuation.Stop;     // Handle never runs; nothing appended.
    }
    return HandlerContinuation.Continue;
}
```

`HandlerContinuation.Stop` short-circuits the pipeline cleanly — no exception, no stack trace. Use `Validate` for expected rejection paths (idempotency, stale offers); reserve exceptions for invariant violations that genuinely shouldn't happen.

**Critical:** do NOT put `[BoundaryModel]` on the `Validate()` state parameter. Wolverine codegen produces error CS0128 (duplicate local variable) when the attribute appears on both `Validate` and `Handle`. The pipeline hook receives the state by plain-parameter injection automatically.

### `ValidateAsync` does not compose with `[BoundaryModel]`

A handler combining `ValidateAsync` (returning `Task<HandlerContinuation>`) with a `Handle` that takes `[BoundaryModel]` fails handler discovery silently. Fold validation into `Handle` directly — return early or throw — when the `[BoundaryModel]` attribute is in play. Synchronous `Validate` works as documented above.

---

## The Manual `FetchForWritingByTags` Pattern

When the canonical pattern doesn't fit — typically when contract events expose primitive `Guid`s rather than wrapped tag types, or when conditional/idempotent append logic needs direct boundary inspection — drop to the manual pattern.

```csharp
[WolverineIgnore]
public static class AcceptOfferManualHandler
{
    public static async Task Handle(
        AcceptOffer command,
        IDocumentSession session,
        TimeProvider time,
        CancellationToken ct)
    {
        var query = new EventTagQuery()
            .Or<OfferDispatched, RideOfferStreamId>(new(command.OfferId))
            .Or<OfferAccepted, RideOfferStreamId>(new(command.OfferId))
            .Or<OfferAccepted, DriverStreamId>(new(command.DriverId))
            .Or<RideRequestCancelled, RideRequestStreamId>(new(command.RideRequestId));

        var boundary = await session.Events.FetchForWritingByTags<OfferAcceptanceState>(query);

        var state = boundary.Aggregate ?? new OfferAcceptanceState();
        if (state.OfferStatus is OfferStatus.Accepted)
            return;        // Idempotent: this driver has already accepted; treat as success.

        Decide(command, state);

        var accepted = session.Events.BuildEvent(
            new OfferAccepted(command.OfferId, command.DriverId, command.RideRequestId, time.GetUtcNow()));
        accepted.WithTag(
            new RideOfferStreamId(command.OfferId),
            new DriverStreamId(command.DriverId),
            new RideRequestStreamId(command.RideRequestId));

        boundary.AppendOne(accepted);
    }
}
```

What's different from the canonical pattern:

- No `[BoundaryModel]` attribute. The handler takes `IDocumentSession` directly.
- The handler returns `Task`, not the event. The append is explicit via `boundary.AppendOne(...)`.
- Tags are applied manually via `session.Events.BuildEvent(...).WithTag(...)`. The canonical pattern handles this automatically when contract events carry the tag types as native fields.
- The handler is decorated with `[WolverineIgnore]` if a canonical sibling exists for the same command, so Wolverine picks one up unambiguously.

`IEventBoundary<T>` exposes:

- `T? Aggregate` — the state projected from matching events, or `null` if no events matched.
- `long LastSeenSequence` — the sequence-number marker used for the consistency assertion.
- `IReadOnlyList<IEvent> Events` — the loaded events, ordered by sequence.
- `void AppendOne(object @event)` and `void AppendMany(...)` — append events through the boundary; the consistency assertion fires at `SaveChangesAsync`.

---

## `EventTagQuery` — the Shared DSL

Both patterns build the same `EventTagQuery` (in `JasperFx.Events.Tags`). Two equivalent construction styles:

### Fluent (preferred for `[BoundaryModel]` handlers)

```csharp
EventTagQuery
    .For(new RideOfferStreamId(command.OfferId))
    .AndEventsOfType<OfferDispatched, OfferAccepted, OfferRejected, OfferExpired>()
    .Or(new DriverStreamId(command.DriverId))
    .AndEventsOfType<OfferAccepted, OfferExpired>()
    .Or(new RideRequestStreamId(command.RideRequestId))
    .AndEventsOfType<RideRequested, RideRequestCancelled, RideRequestMatched>();
```

- `For(tag)` opens the query and sets the current tag context.
- `AndEventsOfType<T1, T2, ...>()` adds one OR-condition per event type, anchored to the current tag. Up to six event types per call (the source has overloads for `T1` through `T1..T6`).
- `Or(tag)` switches the current tag and starts a new arm. Each arm alternates `Or` and `AndEventsOfType` until the query is complete.

### Imperative (preferred for manual handlers)

```csharp
new EventTagQuery()
    .Or<OfferDispatched, RideOfferStreamId>(new(command.OfferId))
    .Or<OfferAccepted, RideOfferStreamId>(new(command.OfferId))
    .Or<OfferAccepted, DriverStreamId>(new(command.DriverId))
    .Or<RideRequestCancelled, RideRequestStreamId>(new(command.RideRequestId));
```

Each `.Or<TEvent, TTag>(tagValue)` adds one `EventTagQueryCondition`. More verbose but more explicit — every condition names its event type and tag type inline.

Both forms produce the same internal condition list. Pick whichever reads cleaner for the query at hand.

### `AndEventsOfType` is required after `For()` and `Or(tag)`

In the fluent form, calling `.For(tagValue)` or `.Or(tagValue)` alone creates a tag-only condition with no event-type filter. Following with `.AndEventsOfType<...>()` replaces the tag-only condition with explicit event-type conditions. Without the follow-up, the query is over-broad and `FetchForWritingByTags` may throw `ArgumentException` at runtime. The imperative form (`.Or<TEvent, TTag>(...)`) doesn't have this trap because event type and tag are specified together.

---

## The Boundary State Aggregate

A plain class with per-event `Apply(T)` methods. The state projects events from **multiple logical streams** because the event store loads by tag, not by stream ID.

```csharp
public class OfferAcceptanceState
{
    // Required under Marten 8 — see § Tag Type Registration.
    public Guid Id { get; set; }

    // From RideOffer stream
    public OfferStatus OfferStatus { get; private set; } = OfferStatus.Unknown;
    public Guid? OfferDriverId { get; private set; }

    // From Driver stream
    public bool DriverHasActiveCommitment { get; private set; }

    // From RideRequest stream
    public RideRequestStatus RideRequestStatus { get; private set; } = RideRequestStatus.Unknown;

    public void Apply(OfferDispatched e)
    {
        OfferStatus = OfferStatus.Pending;
        OfferDriverId = e.DriverId;
    }

    public void Apply(OfferAccepted e)
    {
        OfferStatus = OfferStatus.Accepted;
        // Cross-tag: this same event tags the Driver stream too,
        // so it lands here for both arms of the query.
        DriverHasActiveCommitment = true;
    }

    public void Apply(OfferRejected _) => OfferStatus = OfferStatus.Rejected;
    public void Apply(OfferExpired _)  => OfferStatus = OfferStatus.Expired;

    public void Apply(RideRequested e)         => RideRequestStatus = RideRequestStatus.Pending;
    public void Apply(RideRequestCancelled _)  => RideRequestStatus = RideRequestStatus.Cancelled;
    public void Apply(RideRequestMatched _)    => RideRequestStatus = RideRequestStatus.Matched;
}
```

A few conventions:

- **Plain class with private-setter properties.** Boundary state is a Marten-managed projection-style document; it doesn't follow the `sealed record` aggregate convention from `marten-aggregates`. Mutability inside `Apply(T)` is fine and idiomatic here.
- **`public Guid Id { get; set; }` is required.** Marten 8 registers the boundary state type as a document via `RegisterTagType<TTag>(alias).ForAggregate<TState>()`, and document types must have an identity property. Omitting it surfaces as `InvalidDocumentException` — typically at fixture teardown rather than at boot. The canonical Wolverine `SubscriptionState` examples omit `Id`, but those are tested under conditions where the missing `Id` doesn't surface; in CritterCab on Marten 8, include it. Marten populates it automatically.
- **One `Apply(T)` per event type.** Don't merge with switches — Marten's convention scanner picks each overload separately.
- **The state can also use `Evolve(IEvent e)`** with a switch on `e.Data` if access to event metadata (sequence, version, timestamp) is needed. Mix only one shape per state class.
- **No business decisions on the state itself.** The state exposes computed booleans (`DriverHasActiveCommitment`, `WouldExceedCapacity`); the handler reads them and decides. Keep `Apply` methods pure.

### Nullable `[BoundaryModel]` parameter

When the `EventTagQuery` matches zero events, Wolverine passes `null` to the `[BoundaryModel]` state parameter. Two ways to handle it:

```csharp
// Option 1 — declare nullable, handle in Handle
public static OfferAccepted Handle(
    AcceptOffer command,
    [BoundaryModel] OfferAcceptanceState? state)
{
    state ??= new OfferAcceptanceState();
    // ...
}

// Option 2 — declare non-nullable, set Required = true
public static OfferAccepted Handle(
    AcceptOffer command,
    [BoundaryModel(Required = true)] OfferAcceptanceState state)
{
    // Wolverine returns 404 (or stops the pipeline) if no events matched.
}
```

Use `Required = true` when "no matching events" is genuinely an error; use the nullable shape when it's a legitimate "first time" case.

---

## Tag Type Registration

DCB tag types are registered at bootstrap (see `service-bootstrap`) on the Marten store options. The pattern is uniform across Marten and Polecat:

```csharp
opts.Events.RegisterTagType<RideOfferStreamId>("offer")
    .ForAggregate<OfferAcceptanceState>();
opts.Events.RegisterTagType<DriverStreamId>("driver")
    .ForAggregate<OfferAcceptanceState>();
opts.Events.RegisterTagType<RideRequestStreamId>("request")
    .ForAggregate<OfferAcceptanceState>();
```

`.ForAggregate<TState>()` binds the tag type to the boundary state; the same tag type can be bound to multiple state types if different handlers project different shapes from the same tag axis.

### Tag types must be single-property records — raw `Guid` does NOT work

Marten resolves a tag type via `JasperFx.Core.Reflection.ValueTypeInfo.ForType`, which requires the type to have **exactly one public, gettable instance property**. `Guid` has multiple public instance properties (notably `Variant` and `Version` since .NET 10), so registering `Guid` directly throws `InvalidValueTypeException` at boot.

The fix: wrap each tag type in a single-property record:

```csharp
public sealed record RideOfferStreamId(Guid Value);
public sealed record DriverStreamId(Guid Value);
public sealed record RideRequestStreamId(Guid Value);
```

Plus the underlying tag type (the `Guid Value` here) is recovered by `ValueTypeInfo` for storage and lookup. The wrapper is type-safety pure overhead at runtime.

This is the same pattern documented in `csharp-coding-standards` § GUIDs for value-typed identifiers across the codebase, applied here for the specific reason that DCB tag-type registration enforces it.

---

## Tagging Writes

The canonical `[BoundaryModel]` handler tags returned events automatically — the codegen scans the event for properties whose types match registered tag types and applies the tags. **Outside the canonical handler, every code path that appends DCB-relevant events must tag them explicitly** — including the manual handler pattern, test seeding, migration scripts, and any non-DCB handler that writes a tagged event.

The canonical seeding shape (verified against the Wolverine repo's `boundary_model_workflow_tests.cs`):

```csharp
var dispatched = session.Events.BuildEvent(
    new OfferDispatched(offerId, driverId, rideRequestId, time.GetUtcNow()));
dispatched.WithTag(new RideOfferStreamId(offerId));
session.Events.Append(offerId, dispatched);
```

Two interchangeable APIs (both live on `IEvent` in `JasperFx.Events`, identical on Marten and Polecat):

```csharp
// AddTag — void, mutates the event
wrapped.AddTag(new RideOfferStreamId(offerId));

// WithTag — fluent, returns IEvent for chaining
wrapped.WithTag(new RideOfferStreamId(offerId));

// Variadic — multiple tags at once
wrapped.WithTag(new RideOfferStreamId(offerId), new DriverStreamId(driverId));
```

`AddTag` and `WithTag` are equivalent; `WithTag` simply wraps `AddTag` and returns `IEvent`. Pick whichever reads cleaner.

### `StartStream` drops tags

`MartenOps.StartStream` (and `session.Events.StartStream`) wraps raw event objects internally — pre-wrapped `IEvent` instances passed to `StartStream` lose their tags. For tagged events, use `session.Events.Append(streamId, wrapped)` instead. Streams are created implicitly on first append, so the loss of `StartStream`'s declarative shape is rarely a problem in practice.

For boundary-state aggregates that need stream-type declaration under `UseMandatoryStreamTypeDeclaration = true` (the Cab default per `service-bootstrap`), the seeding pattern is:

```csharp
session.Events.StartStream<RideOffer>(offerId, dispatched);                          // declare stream type
session.PendingChanges.Streams().Single().Events.Single().AddTag(new RideOfferStreamId(offerId));
await session.SaveChangesAsync();
```

For subsequent appends to the same stream, switch to `BuildEvent` + `AddTag` + `Append(streamKey, wrapped)`.

---

## Concurrency

Wolverine and Marten enforce optimistic concurrency at `SaveChangesAsync` time using the same tag query that loaded the events. If any matching event was appended between the boundary load and the save, Marten throws `DcbConcurrencyException` (in `Marten.Events.Dcb`, a subclass of `MartenException`).

`DcbConcurrencyException` and `JasperFx.ConcurrencyException` are **siblings, not parent-child.** A retry policy that catches one does not catch the other. Both need explicit registration:

```csharp
opts.OnException<ConcurrencyException>()
    .RetryWithCooldown(100.Milliseconds(), 250.Milliseconds());
opts.OnException<DcbConcurrencyException>()
    .RetryWithCooldown(100.Milliseconds(), 250.Milliseconds());
```

A blanket `OnException<MartenException>` catches `DcbConcurrencyException` but also catches unrelated Marten failures — too broad. Stay specific to the two concurrency exceptions.

For Polecat services (Phase 4), the equivalent type is `Polecat.Events.Dcb.DcbConcurrencyException` — same name, different namespace.

---

## Testing

### Unit tests — no infrastructure

Canonical `[BoundaryModel]` handlers receive plain state objects, so the decision logic is unit-testable without an event store:

```csharp
var state = new OfferAcceptanceState();
state.Apply(new OfferDispatched(offerId, driverId, rideRequestId, dispatchedAt));
state.Apply(new RideRequested(rideRequestId, riderId, requestedAt));

var result = AcceptOfferHandler.Handle(
    new AcceptOffer(offerId, driverId, rideRequestId),
    state,
    TimeProvider.System);

result.ShouldBeOfType<OfferAccepted>();
```

This exercises only the decide step, not the tag query or append. Pair with integration tests for end-to-end coverage.

### Integration tests — through Wolverine

When tag selection, boundary load correctness, or concurrency behavior is under test, invoke through Wolverine with tag types and event types registered on the store:

```csharp
// Fixture setup
opts.Events.RegisterTagType<RideOfferStreamId>("offer").ForAggregate<OfferAcceptanceState>();
opts.Events.RegisterTagType<DriverStreamId>("driver").ForAggregate<OfferAcceptanceState>();
opts.Events.RegisterTagType<RideRequestStreamId>("request").ForAggregate<OfferAcceptanceState>();
opts.Events.AddEventType<OfferDispatched>();
opts.Events.AddEventType<RideRequested>();
// ... every event type the tag query loads must be registered

// Test body
await SeedOfferAndRideRequest(offerId, driverId, rideRequestId);
await host.InvokeMessageAndWaitAsync(new AcceptOffer(offerId, driverId, rideRequestId));

await using var session = store.LightweightSession();
var events = await session.Events.QueryByTagsAsync(
    new EventTagQuery().Or<RideOfferStreamId>(new(offerId)));
events.ShouldContain(e => e.Data is OfferAccepted);
```

`session.Events.QueryByTagsAsync` is the read-side companion to `FetchForWritingByTags` — same query DSL, no consistency assertion. Use it freely in test assertions to verify what was tagged where.

For full testing patterns (fixture lifecycle, `WaitForNonStaleProjectionDataAsync`, tracked sessions), see `testing-integration` (Phase 2).

---

## Implementation Checklist

When introducing DCB to a service, walk these steps in order.

1. **Define strong-typed tag ID records.** One per stream type. Single-property records over `Guid` (per § Tag Type Registration).
2. **Register tag types in the store options.** `opts.Events.RegisterTagType<TagType>(alias).ForAggregate<TState>()` for each tag axis.
3. **Register every event type loaded by the tag query.** `opts.Events.AddEventType<TEvent>()` — `UseMandatoryStreamTypeDeclaration = true` (the Cab default) makes this fail loudly at append time if missed.
4. **Define the boundary state class** with `public Guid Id { get; set; }`, per-event `Apply(T)` methods, and computed predicates the handler reads. Plain class, private setters, no business decisions on the state.
5. **Add retry policies for both `ConcurrencyException` and `DcbConcurrencyException`.** Sibling exception types; both need explicit registration.
6. **Write the DCB handler using the canonical `[BoundaryModel]` pattern.** Static `Load(command) => EventTagQuery`, static `Handle(command, [BoundaryModel] TState state, ...)` returning the event(s). Optional `Validate` for `HandlerContinuation`-based bailout.
7. **Tag every write outside the canonical handler.** Test seeding, migration scripts, and any non-DCB handler that writes a DCB-relevant event must use `BuildEvent` + `WithTag` + `Append`.
8. **Cover the boundary with integration tests.** Verify tag selection loads the intended state, decision logic produces the expected event(s), and concurrent matching writes trigger `DcbConcurrencyException`.

Reach for the manual `FetchForWritingByTags` pattern only when the canonical pattern doesn't fit — typically when contract events expose primitive `Guid` fields rather than wrapped tag types and the auto-tagging codegen path can't infer.

---

## Common pitfalls

- **Registering raw `Guid` as a tag type.** `RegisterTagType<Guid>("...")` throws `InvalidValueTypeException` at boot. Always wrap in a single-property record.
- **Forgetting `public Guid Id { get; set; }` on the boundary state class.** Surfaces as `InvalidDocumentException` at fixture teardown rather than at boot; easy to miss until tests fail. Marten 8 registers boundary state types as documents and documents need identities.
- **`[BoundaryModel]` on a `Validate` parameter.** Causes Wolverine codegen error CS0128. The attribute belongs on `Handle` alone; pipeline hooks receive the projected state by plain-parameter injection.
- **`ValidateAsync` + `[BoundaryModel]` together.** Handler discovery silently fails. Fold validation into `Handle` when `[BoundaryModel]` is in play.
- **Forgetting `AndEventsOfType` after `For`/`Or` in the fluent form.** Produces a tag-only condition; `FetchForWritingByTags` throws at runtime. The imperative `.Or<TEvent, TTag>(...)` form doesn't have this trap.
- **Catching only `ConcurrencyException` in retry policies.** `DcbConcurrencyException` is a sibling; must be registered separately.
- **Tagging a `StartStream` event.** `StartStream` re-wraps the event and drops tags. Use `Append` for tagged events; let streams be created implicitly on first append.
- **Two methods named `Handle*` on the same handler class.** Wolverine discovery throws `NoHandlerForEndpointException`. If you keep a pure-function decide method alongside the bus handler, name it something that doesn't start with `Handle` (e.g., `Decide`).
- **Two handlers for the same command without `[WolverineIgnore]`.** Discovery is ambiguous and codegen fails. Mark the manual variant with `[WolverineIgnore]` when keeping both for comparison or migration.

---

## See also

**Upstream** — generic Marten DCB mechanics this skill defers to. ai-skills (license required, install via `npx skills add`):

- `marten-advanced-dynamic-consistency-boundary` — the basic three-part pattern (state + Load + Handle), `EventTagQuery` fluent API, `IEventBoundary<T>` brief mention, DCB-vs-standard-multi-stream comparison table, return-value handling, decision guidance. **Cab's coverage substantially exceeds ai-skills' coverage** — Cab adds the manual `FetchForWritingByTags` pattern, `Validate`/`HandlerContinuation` hook, `ValidateAsync` incompatibility, boundary state `Id` requirement under Marten 8, tag-type single-property record requirement under .NET 10, `StartStream`-drops-tags behavior, `DcbConcurrencyException`-vs-`ConcurrencyException` sibling rule, testing patterns, and an 8-step implementation checklist. Load this for the basic three-part pattern intro; the rest of this Cab skill is the operational layer.

**Prerequisites** — Cab-internal skills to load first if unfamiliar:

- `marten-aggregates` — aggregate shape and the decider pattern; the boundary state is a related but distinct shape.
- `marten-wolverine-aggregates` — `[WriteAggregate]` and `MartenOps`; the single-stream complement to `[BoundaryModel]`.
- `domain-event-conventions` — slim domain events vs. integration events; events that participate in a boundary tag-query are slim by definition.
- `csharp-coding-standards` — single-property records, sealed records, `required` properties; the wrapper-record pattern that tag types depend on.
- `service-bootstrap` — `RegisterTagType`, `AddEventType`, retry policy registration; where this skill's bootstrap requirements land.

**Sibling skills:**

- `marten-querying` — query-side patterns; `session.Events.QueryByTagsAsync` is the read-side counterpart to `FetchForWritingByTags`.
- `marten-projections` — DCB events flow through projections like any other event; lifecycle and registration concerns there apply unchanged.

**Downstream:**

- `testing-integration` — `WaitForNonStaleProjectionDataAsync`, tracked sessions, fixture patterns for DCB-heavy tests (Phase 2).
- `wolverine-sagas` — when DCB doesn't fit because the workflow is genuinely long-running or cross-BC (Phase 4).
- `polecat-event-sourcing` — Polecat's parallel DCB API for SQL Server services; same shape, different namespace (Phase 4).

**External:**

- ai-skills `marten-aggregate-handler-workflow` — the broader aggregate workflow context that DCB sits within.
- [Sara Pellegrini — *Killing the Aggregate*](https://sara.event-thinking.io/2023/04/kill-aggregate-chapter-1-I-will-tell-you-a-story.html) — the originating essay for the DCB pattern.
- [dcb.events](https://dcb.events/) — the pattern specification.
- [Marten Dynamic Consistency Boundary](https://martendb.io/events/dcb.html) — Marten's DCB documentation.
- [Wolverine + Marten: DCB](https://wolverinefx.io/guide/durability/marten/event-sourcing.html#dynamic-consistency-boundary-dcb) — the `[BoundaryModel]` pattern reference.
- Canonical code in the Wolverine repo: `src/Persistence/MartenTests/Dcb/University/` — `BoundaryModelSubscribeStudentToCourse.cs` is the `#region sample_wolverine_dcb_boundary_model_handler` target; `ChangeCourseCapacity.cs` shows three parallel handler shapes for one command (manual, `[BoundaryModel]`, `[WriteAggregate]`); `boundary_model_workflow_tests.cs` is the canonical seeding and integration-test reference.
