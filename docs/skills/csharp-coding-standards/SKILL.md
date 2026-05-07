---
name: csharp-coding-standards
description: "C# language idioms for CritterCab: records, required/init-only, value objects, guard clauses, async/streams, time provider, GUIDs, decimal rounding. Use when authoring or reviewing C# code, or designing domain models."
cluster: core
tags: [csharp, dotnet, conventions, language, domain-modeling]
---

# C# Coding Standards

C# language features, code style, and best practices for CritterCab.

## When to apply this skill

Use this skill when:

- Authoring any new C# code in this repo (commands, queries, events, aggregates, handlers, value objects).
- Reviewing C# code for convention compliance.
- Designing a new domain model or value object.
- Working with money, ratings, distances, GPS coordinates, or any numeric domain values.
- Adding new identifiers (GUID generation strategy).

This skill covers project-wide language and idiom conventions. Skills downstream of this one apply these conventions to specific Critter Stack patterns.

## Core Principles

1. **Immutability by default** — records, readonly collections, `with` expressions
2. **Sealed by default** — prevent unintended inheritance
3. **Value objects for domain concepts** — wrap primitives with validation
4. **Pure functions where possible** — separate decisions from side effects

---

## Records and Immutability

Use records for commands, queries, events, DTOs, and value objects:

```csharp
// Commands
public sealed record RequestRide(Guid RideRequestId, Guid RiderId, GeoLocation Pickup, GeoLocation Dropoff);

// Domain events
public sealed record TripStarted(Guid TripId, Guid DriverId, Guid RiderId, GeoLocation StartLocation, DateTimeOffset StartedAt);

// Value objects
public sealed record Fare(decimal Amount, string Currency);

// DTOs / view models
public sealed record TripSummaryView(Guid Id, Guid RiderId, Guid DriverId, decimal FareTotal, int DistanceMeters, string Status);
```

Use `with` expressions for immutable aggregate updates. For aggregates, this happens inside static `Apply` methods (the decider-pattern “evolve” function) — covered in detail in `marten-aggregates`:

```csharp
public sealed record Trip(
    Guid Id,
    Guid DriverId,
    Guid RiderId,
    TripStatus Status,
    GeoLocation StartLocation,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt)
{
    public static Trip Create(IEvent<TripStarted> @event) =>
        new(
            @event.StreamId,
            @event.Data.DriverId,
            @event.Data.RiderId,
            TripStatus.InProgress,
            @event.Data.StartLocation,
            @event.Data.StartedAt,
            CompletedAt: null);

    public static Trip Apply(TripCompleted @event, Trip current) =>
        current with
        {
            Status = TripStatus.Completed,
            CompletedAt = @event.CompletedAt
        };
}
```

Three conventions visible above and pinned in `marten-aggregates`:

- **`Create` is `static`, takes `IEvent<TFirstEvent>`**, and pulls the aggregate ID from `@event.StreamId` — Wolverine assigns the stream ID when the first event is persisted, so the handler doesn't generate it.
- **`Apply` methods are `static`, take `(TEvent @event, TAggregate current)`**, and return the new aggregate via `with`. No instance methods, no `void` returns, no `this`.
- **No business validation on the aggregate.** Decisions live in handlers (the decider pattern's “decide” function); the aggregate is pure state + evolution.

### Required Properties

Use the `required` modifier (C# 11+) instead of `null!` placeholders for properties that must be supplied during construction. The compiler enforces this at every construction site and the type signature accurately reflects the contract.

```csharp
// ✅ MODERN — compiler enforces all required properties at every construction site
public sealed record RideOffer
{
    public required Guid OfferId { get; init; }
    public required Guid RideRequestId { get; init; }
    public required Guid DriverId { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    private RideOffer() { }
}

// ❌ OLDER — `null!` is a compiler suppression, not enforcement
public sealed record RideOffer
{
    public Guid OfferId { get; init; }
    public Guid RideRequestId { get; init; }
    public Guid DriverId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }

    private RideOffer() { }
}
```

**Recommended:** `required` for all init-only properties on commands, events, queries, view models, and aggregates that must be set during construction.

**The `null!` pattern is acceptable when:**
- A custom `JsonConverter<T>` constructs the type via a factory method (see Value Object Pattern). The converter performs validation, so `required` would be redundant and may interact awkwardly with the converter's bypass of normal property-setting.
- A framework constructs the type via reflection in a way that `required` would block. Most modern Marten, Wolverine, and EF Core versions honor `required`; if you hit a deserialization error, this is the place to look.

When in doubt, prefer `required`.

---

## Sealed by Default

All commands, queries, events, and models must be `sealed`:

```csharp
// ✅ CORRECT
public sealed record RequestRide(Guid RideRequestId, Guid RiderId, GeoLocation Pickup, GeoLocation Dropoff);

// ❌ WRONG — allows unintended inheritance
public record RequestRide(Guid RideRequestId, Guid RiderId, GeoLocation Pickup, GeoLocation Dropoff);
```

---

## Collection Patterns

The collection-type convention depends on whether the type is a DTO (over the wire, on disk, or as a parameter) or aggregate state (folded over events with `with`).

### Domain events, integration messages, commands, queries, and view models — use `IReadOnly*`

These are DTOs. They're constructed once, never mutated, and shape the wire format (or the JSON in the event store).

```csharp
// ✅ CORRECT — DTO with read-only collections
public sealed record TripCompleted(
    Guid TripId,
    Guid DriverId,
    Guid RiderId,
    IReadOnlyList<GeoLocation> RouteWaypoints,   // ordered
    IReadOnlyList<FareLineItem> FareBreakdown);  // ordered

// ❌ WRONG — externally mutable
public sealed record TripCompleted(
    Guid TripId,
    Guid DriverId,
    Guid RiderId,
    List<GeoLocation> RouteWaypoints,
    List<FareLineItem> FareBreakdown);
```

**Prefer:**
- `IReadOnlyList<T>` for ordered collections
- `IReadOnlyCollection<T>` for unordered collections
- `IReadOnlyDictionary<TKey, TValue>` for key-value pairs

Empty collection shorthand (C# 12+):

```csharp
IReadOnlyList<GeoLocation> waypoints = [];
```

### Aggregates — use `Immutable*`

Aggregate state is folded over events. Each `Apply` returns a new aggregate via `with`, so collection state on aggregates needs structural sharing and mutation-returning APIs to fold cleanly.

```csharp
// ✅ Aggregate state — ImmutableDictionary supports mutation-returning operations
public sealed record DriverProfile(
    Guid Id,
    string Name,
    ImmutableDictionary<Guid, Certification> Certifications,
    ImmutableHashSet<Guid> ActiveSuspensions,
    ImmutableList<TripReference> RecentTrips,
    DateTimeOffset LastUpdated)
{
    public static DriverProfile Apply(CertificationGranted @event, DriverProfile current) =>
        current with
        {
            Certifications = current.Certifications.SetItem(@event.CertificationId,
                new Certification(@event.Type, @event.GrantedAt)),
            LastUpdated = @event.GrantedAt
        };
}
```

**Choice by collection shape:**
- `ImmutableDictionary<TKey, TValue>` — ID-keyed state (reservations, certifications, by-ID maps)
- `ImmutableList<T>` — ordered state (recent trips, audit history)
- `ImmutableHashSet<T>` — unordered uniqueness-bearing sets (active flags, role membership)

All three are from `System.Collections.Immutable`. They're persistent data structures — `current.Certifications.SetItem(...)` allocates only the changed path through the tree, not the whole collection.

### Why the split

`Immutable*` types pair naturally with `with` expressions: `current.Items.SetItem(id, value)` returns a new collection without copying. `IReadOnlyList<T>` requires explicit copy syntax (`[..current.Items, newItem]`) which gets noisy in `Apply` methods. DTOs don't need either capability — they're constructed in one place and never mutated — so the lighter `IReadOnlyList<T>` is the right shape there.

### Performance escape hatch

For aggregates with very large or very hot collections (>10,000 items or hot-loop folding at high frequency), `Immutable*`'s O(log N) ops can compound. If profiling shows it as a real bottleneck on a specific aggregate, fall back to `IReadOnlyList<T>` for that aggregate with explicit copy syntax in `Apply`:

```csharp
// Performance fallback — document why on the aggregate
public static SomeAggregate Apply(ItemAdded @event, SomeAggregate current) =>
    current with { Items = [..current.Items, @event.Item] };
```

In practice this rarely applies in CritterCab — if an aggregate's collection grows that large, the aggregate boundary is usually the actual problem and the right fix is to split the aggregate, not to optimize the collection type.

---

## Value Object Pattern

Use value objects to wrap primitives with domain-specific validation.

**Standard structure:**
1. `sealed record` with a `Value` property
2. `From(value)` factory method with validation
3. Private parameterless constructor for Marten/JSON deserialization
4. Implicit conversion operator for seamless queries
5. `JsonConverter` for transparent serialization

```csharp
[JsonConverter(typeof(LicensePlateJsonConverter))]
public sealed record LicensePlate
{
    private const int MaxLength = 12;
    private static readonly Regex AllowedChars = new(@"^[A-Z0-9\- ]+$", RegexOptions.Compiled);

    public string Value { get; init; } = null!;
    private LicensePlate() { }

    public static LicensePlate From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > MaxLength)
            throw new ArgumentException($"License plate cannot exceed {MaxLength} characters", nameof(value));
        if (!AllowedChars.IsMatch(normalized))
            throw new ArgumentException("License plate contains invalid characters", nameof(value));

        return new LicensePlate { Value = normalized };
    }

    public static implicit operator string(LicensePlate p) => p.Value;
    public override string ToString() => Value;
}

public sealed class LicensePlateJsonConverter : JsonConverter<LicensePlate>
{
    public override LicensePlate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() is { } value ? LicensePlate.From(value) : throw new JsonException("LicensePlate cannot be null");

    public override void Write(Utf8JsonWriter writer, LicensePlate value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
```

Value objects serialize as plain strings — not wrapped objects:

```json
{ "licensePlate": "NEB-CAB-1968", "vehicleClass": "Standard" }
```

**When to use value objects:**
- Identity values with constraints (license plate, driver ID badge)
- Domain concepts with rules (rating 1–5, fare currency, geo-coordinate validity)
- Values requiring validation (money, percentage, distance ≥ 0)

**When NOT to use:**
- Strings with no constraints (descriptions, free text comments)
- Primitives with no business rules (counts, flags)

### Geospatial Values

`GeoLocation` (latitude/longitude) is one of CritterCab's most-used value objects, used by Telemetry, Dispatch, and Trips. The validation rules — latitude in [-90, 90], longitude in [-180, 180], plus optional accuracy bounds — are domain-meaningful and the agent should NOT pass raw `(double, double)` tuples around. Use a single `GeoLocation` record everywhere.

```csharp
public sealed record GeoLocation
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    private GeoLocation() { }

    public static GeoLocation From(double latitude, double longitude)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90);
        ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180);
        return new GeoLocation { Latitude = latitude, Longitude = longitude };
    }
}
```

---

## FluentValidation

Nest validators inside the command record:

```csharp
public sealed record RequestRide(Guid RideRequestId, Guid RiderId, GeoLocation Pickup, GeoLocation Dropoff)
{
    public class RequestRideValidator : AbstractValidator<RequestRide>
    {
        public RequestRideValidator()
        {
            RuleFor(x => x.RideRequestId).NotEmpty();
            RuleFor(x => x.RiderId).NotEmpty();
            RuleFor(x => x.Pickup).NotNull();
            RuleFor(x => x.Dropoff).NotNull();
            RuleFor(x => x).Must(r => !PointsAreEqual(r.Pickup, r.Dropoff))
                           .WithMessage("Pickup and dropoff must differ");
        }

        private static bool PointsAreEqual(GeoLocation a, GeoLocation b)
            => a.Latitude == b.Latitude && a.Longitude == b.Longitude;
    }
}
```

---

## Modern Guard Clauses

.NET 8+ ships argument-validation helpers that replace the manual `if/throw` pattern. Use them in factory methods and at handler boundaries — they capture the parameter name automatically and read more cleanly.

```csharp
// ✅ MODERN — concise, parameter name captured automatically via [CallerArgumentExpression]
ArgumentException.ThrowIfNullOrWhiteSpace(value);
ArgumentNullException.ThrowIfNull(provider);
ArgumentOutOfRangeException.ThrowIfNegative(distanceMeters);
ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90);
ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90);

// ❌ OLDER — verbose, easy to forget the nameof() argument
if (string.IsNullOrWhiteSpace(value))
    throw new ArgumentException("Value cannot be empty", nameof(value));
if (provider is null)
    throw new ArgumentNullException(nameof(provider));
if (distanceMeters < 0)
    throw new ArgumentOutOfRangeException(nameof(distanceMeters));
```

The most-used helpers (not exhaustive):

- `ArgumentException.ThrowIfNullOrEmpty(string)`
- `ArgumentException.ThrowIfNullOrWhiteSpace(string)`
- `ArgumentNullException.ThrowIfNull(object)`
- `ArgumentOutOfRangeException.ThrowIfNegative(value)` / `ThrowIfNegativeOrZero` / `ThrowIfZero`
- `ArgumentOutOfRangeException.ThrowIfLessThan(value, min)` / `ThrowIfLessThanOrEqual`
- `ArgumentOutOfRangeException.ThrowIfGreaterThan(value, max)` / `ThrowIfGreaterThanOrEqual`
- `ObjectDisposedException.ThrowIf(condition, instance)`

Use the manual `throw` pattern only when validation is genuinely domain-specific and the helpers don't capture the intent (regex pattern checks, cross-field consistency rules, custom error messages with domain framing).

---

## Status Enums

Use a single status enum over multiple booleans. Impossible states should be impossible.

```csharp
// ✅ CORRECT — single source of truth
public enum TripStatus { Requested, Assigned, EnRoute, Arrived, InProgress, Completed, Cancelled }

public sealed record Trip(Guid Id, TripStatus Status, /* ... */)
{
    public bool IsTerminal => Status is TripStatus.Completed or TripStatus.Cancelled;
    public bool IsActive => Status is TripStatus.Assigned or TripStatus.EnRoute or TripStatus.Arrived or TripStatus.InProgress;
}

// ❌ WRONG — multiple booleans create ambiguous combinations
public sealed record Trip(Guid Id, bool IsAssigned, bool IsEnRoute, bool IsCompleted, bool IsCancelled);
```

---

## Factory Methods

Use static factory methods for object creation. Keep constructors private. Accept `TimeProvider` as the last parameter (or second-to-last when a `CancellationToken` is present) when the factory needs the current time so callers can supply a `FakeTimeProvider` in tests (see the `TimeProvider` section below).

```csharp
public sealed record RideRequest
{
    public required Guid Id { get; init; }
    public required Guid RiderId { get; init; }
    public required GeoLocation Pickup { get; init; }
    public required GeoLocation Dropoff { get; init; }
    public required DateTimeOffset RequestedAt { get; init; }

    private RideRequest() { }

    public static RideRequest Create(
        Guid riderId,
        GeoLocation pickup,
        GeoLocation dropoff,
        TimeProvider time) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            RiderId = riderId,
            Pickup = pickup,
            Dropoff = dropoff,
            RequestedAt = time.GetUtcNow()
        };
}
```

---

## Nullable Reference Types

Enable nullable reference types. Be explicit about nullability.

```csharp
public sealed record SurgePricingConfig(
    bool Enabled,
    decimal BaseMultiplier,
    decimal MaxMultiplier,
    int? CooldownMinutes);  // Explicitly nullable — null means no cooldown
```

### Message Record Field Nullability

Fields on commands, events, and integration messages that are always populated must be **required and non-nullable**. Optional-with-default is a code smell that hides type-system guarantees and invites construction-time omissions.

```csharp
// ❌ WRONG — implies DriverId might be absent; it never is on this event
public sealed record TripCompleted(
    Guid TripId,
    Guid RiderId,
    Guid? DriverId = null);

// ✅ CORRECT — compiler enforces population at all construction sites
public sealed record TripCompleted(
    Guid TripId,
    Guid DriverId,
    Guid RiderId,
    decimal FareTotal,
    int DistanceMeters,
    DateTimeOffset CompletedAt);
```

**Nullable fields ARE appropriate when:**
- The field is genuinely optional by business logic (`string? CancellationReason` on `TripCancelled`)
- The field was added in a later version for backward compatibility with existing serialized messages

---

## Pattern Matching

Use modern pattern matching over type checks and null guards:

```csharp
// Type patterns
if (result is OfferAccepted accepted)
    yield return new TripStartingNotification(accepted.TripId, accepted.DriverId, ...);

// Property patterns
if (trip is { Status: TripStatus.InProgress, DistanceMeters: > 0, FareTotal: > 0m })
    return TripDecision.EligibleForCompletion;

// Switch expressions
var displayStatus = trip.Status switch
{
    TripStatus.Requested  => "Looking for a driver",
    TripStatus.Assigned   => "Driver on the way",
    TripStatus.EnRoute    => "Driver en route",
    TripStatus.Arrived    => "Driver has arrived",
    TripStatus.InProgress => "On the way to destination",
    TripStatus.Completed  => "Completed",
    TripStatus.Cancelled  => "Cancelled",
    _                     => "Unknown"
};
```

---

## Async/Await

Follow async conventions consistently:

```csharp
// ✅ CORRECT — async all the way, with cancellation
public static async Task<Trip?> Handle(GetTrip query, IDocumentSession session, CancellationToken ct)
    => await session.LoadAsync<Trip>(query.TripId, ct);

// ✅ CORRECT — return Task directly when no await needed
public static Task<Trip?> Handle(GetTrip query, IDocumentSession session, CancellationToken ct)
    => session.LoadAsync<Trip>(query.TripId, ct);

// ❌ WRONG — blocks async, deadlock risk
public static Trip? Handle(GetTrip query, IDocumentSession session)
    => session.LoadAsync<Trip>(query.TripId).Result;
```

---

## `IAsyncEnumerable<T>` Patterns

`IAsyncEnumerable<T>` shows up frequently in CritterCab: gRPC server-streaming and bidirectional methods, Marten's streaming query APIs, and any handler that produces a stream of outbound responses. The conventions here are small but easy to get wrong.

### Consuming a stream

Always pass the cancellation token through with `WithCancellation(ct)`:

```csharp
// ✅ CORRECT — token flows to the producer
await foreach (var location in driverLocations.WithCancellation(ct))
{
    await broadcaster.PublishAsync(location, ct);
}

// ❌ WRONG — token does NOT flow; producer cannot honor cancellation
await foreach (var location in driverLocations)
{
    await broadcaster.PublishAsync(location, ct);
}
```

### Authoring a stream

Use `[EnumeratorCancellation]` on the cancellation-token parameter so callers' tokens are honored when they pass one to `WithCancellation`:

```csharp
using System.Runtime.CompilerServices;

public static async IAsyncEnumerable<DriverLocation> StreamLocations(
    Guid driverId,
    IDocumentSession session,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    var query = session.Query<DriverLocation>()
        .Where(l => l.DriverId == driverId)
        .OrderBy(l => l.RecordedAt);

    await foreach (var location in query.ToAsyncEnumerable(ct))
    {
        yield return location;
    }
}
```

Without `[EnumeratorCancellation]`, callers' tokens are silently ignored — the producer keeps running until something else cancels it. This is the most common mistake when authoring async-iterator methods.

**Note on Marten:** Marten's `ToAsyncEnumerable(ct)` accepts a `CancellationToken` directly, so chaining `.WithCancellation(ct)` after it is redundant. Use `WithCancellation(ct)` only when consuming an `IAsyncEnumerable<T>` whose producer didn't take a token (the parameter case in the previous example).

### gRPC streaming context

For Wolverine's gRPC streaming support, the handler return type is `IAsyncEnumerable<TResponse>` (server-streaming) or accepts `IAsyncEnumerable<TRequest>` as a parameter (client-streaming). The patterns above apply directly. See `wolverine-grpc-handlers` and `wolverine-grpc-bidirectional-handlers` for the handler shapes.

---

## GUIDs

Use `Guid.CreateVersion7()` for new identifiers — time-ordered, better for database index performance:

```csharp
var tripId = Guid.CreateVersion7();
var rideRequestId = Guid.CreateVersion7();
```

Reserve UUID v5 (deterministic, SHA-1) for natural-key aggregates where multiple handlers need to resolve the same stream ID without a database lookup. See `marten-aggregates`.

---

## DateTimeOffset

Always use `DateTimeOffset` instead of `DateTime`. Always UTC.

```csharp
// ✅ CORRECT
public DateTimeOffset StartedAt { get; init; }
public DateTimeOffset? CompletedAt { get; init; }

var now = DateTimeOffset.UtcNow;

// ❌ WRONG — loses timezone information
public DateTime StartedAt { get; init; }
```

---

## `TimeProvider` for Testable Time

Inject `TimeProvider` instead of calling `DateTimeOffset.UtcNow` directly in code that needs to be tested deterministically. Read the current time via `TimeProvider.GetUtcNow()`. Use `TimeProvider.System` as the default in non-test code; tests use `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider`.

CritterCab has many time-sensitive flows: dispatch offer timeouts, surge cooldowns, trip duration calculations, ETA decay. Hardcoded `DateTimeOffset.UtcNow` calls make these flows untestable without sleeping in tests, which is slow, flaky, and uninformative when it fails.

```csharp
// ✅ CORRECT — testable; current time is an injected dependency
public static class AcceptOfferHandler
{
    // RideOffer is loaded for this command by Wolverine + Marten's aggregate handler middleware.
    public static OfferAccepted Handle(
        AcceptOffer command,
        RideOffer offer,
        TimeProvider time)
    {
        if (time.GetUtcNow() > offer.ExpiresAt)
            throw new InvalidOperationException("Offer has expired");

        return new OfferAccepted(offer.Id, offer.DriverId, time.GetUtcNow());
    }
}

// ❌ WRONG — untestable; production wall-clock leaks into the test
public static class AcceptOfferHandler
{
    public static OfferAccepted Handle(AcceptOffer command, RideOffer offer)
    {
        if (DateTimeOffset.UtcNow > offer.ExpiresAt)  // <-- locked to wall-clock
            throw new InvalidOperationException("Offer has expired");

        return new OfferAccepted(offer.Id, offer.DriverId, DateTimeOffset.UtcNow);
    }
}
```

In tests:

```csharp
var fakeTime = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-04T12:00:00Z"));
// arrange offer with ExpiresAt = 12:00:30Z
fakeTime.Advance(TimeSpan.FromSeconds(45));
// time.GetUtcNow() now reads 12:00:45Z, which is > 12:00:30Z → handler rejects
```

**`DateTimeOffset.UtcNow` directly is acceptable for:**

- One-off scripts and seeders that don't need deterministic time.
- Aspire AppHost configuration where `TimeProvider` injection is impractical.
- The `Guid.CreateVersion7()` runtime call (the runtime embeds time internally; you don't supply it).

For everything else — handlers, sagas, projection logic, factory methods called from handlers — inject `TimeProvider`. See `wolverine-handlers` and `marten-aggregates` for the concrete handler shapes.

---

## Decimal and Financial Calculations

### ⚠️ CRITICAL: Banker's Rounding

`Math.Round()` uses **banker's rounding** (round-to-even) by default, not round-away-from-zero.

```csharp
Math.Round(6.825m, 2)  // → 6.82 (rounds DOWN to even) — not 6.83!
Math.Round(6.835m, 2)  // → 6.84 (rounds UP to even)
Math.Round(4.5m, 0)    // → 4   (rounds DOWN to even)
Math.Round(5.5m, 0)    // → 6   (rounds UP to even)
```

This affects fare calculations, surge multipliers, driver-payout splits, and any percentage-based math. Errors accumulate across bulk calculations and produce test failures when the expected value assumes traditional rounding.

**When you need round-away-from-zero, be explicit:**

```csharp
var fare = Math.Round(baseFare * surgeMultiplier, 2, MidpointRounding.AwayFromZero);
```

**Best practices:**
- Document the rounding mode in all financial calculation methods.
- Test with midpoint values (X.X25, X.X75) to catch rounding assumptions.
- Use the same rounding mode consistently across a calculation pipeline (a fare quote that uses one mode and a final fare that uses another will diverge in production reports).
- Check regulatory requirements for the jurisdiction — some mandate specific rules.

---

## .NET 10 Gotcha: `Guid.Variant` and `Guid.Version`

In .NET 10, `System.Guid` gained `Variant` and `Version` as public instance properties. This breaks Marten's DCB `ValueTypeInfo` validation, which requires exactly one public instance property on tag type records. Raw `Guid` can no longer be used as a DCB tag type.

```csharp
// ❌ WRONG — Guid has 2 public instance properties in .NET 10
opts.Events.RegisterTagType<Guid>("trip");

// ✅ CORRECT — single-property wrapper record
public sealed record TripStreamId(Guid Value);
opts.Events.RegisterTagType<TripStreamId>("trip").ForAggregate<Trip>();
```

This is the same wrapper-record pattern used for value objects, but the motivation here is a framework constraint rather than domain modeling.

---

## Naming Conventions

| Type | Convention | Example |
|---|---|---|
| Commands | Verb + Noun | `RequestRide`, `AcceptOffer`, `StartTrip`, `CompleteTrip` |
| Queries | Get + Noun | `GetTrip`, `GetActiveDrivers`, `GetRiderHistory` |
| Domain events | Noun + Past Verb | `RideRequested`, `TripStarted`, `TripCompleted`, `OfferAccepted` |
| Integration messages | Noun + Past Verb | `TripCompleted`, `PaymentCaptured`, `DriverApproved` |
| Handlers | Command/Query + Handler | `RequestRideHandler`, `StartTripHandler` |
| Validators | Command/Query + Validator | `RequestRideValidator` |
| Aggregates | Domain noun | `Trip`, `RideRequest`, `DriverProfile` |
| Sagas | Domain noun + Saga | `OfferDispatchSaga`, `TripPaymentSaga` |

**Event naming rules specific to CritterCab:**
- No "Event" suffix — `TripStarted` not `TripStartedEvent`
- Past tense — `TripStarted` not `StartTrip` (commands use the imperative form)
- Aggregate ID always the first property
- See `domain-event-conventions` for the full vocabulary reference and per-BC naming guidance.

---

## See Also

**Downstream** — natural follow-ups when applying these conventions to specific patterns:

- `domain-event-conventions` — naming and shape rules specific to events, plus per-BC vocabulary.
- `marten-aggregates` — applies these conventions to event-sourced aggregates (`Trip` is the primary case).
- `wolverine-handlers` — applies these conventions to handler shape, validation, and conventions across HTTP, messaging, and gRPC.

**External:**

- ADR-001 in [`docs/decisions/`](../../decisions/) — tradeoffs as explicit principle.
