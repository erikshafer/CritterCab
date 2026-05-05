---
name: testing-fundamentals
description: "Test-stack baseline for CritterCab — the committed test packages, per-service test project organization, xUnit lifecycle (IAsyncLifetime, IClassFixture, ICollectionFixture), unit testing pure handlers (decider pattern testability), testing validators, time-dependent code via FakeTimeProvider, failure paths, Shouldly conventions, and naming. Use when authoring any new test, setting up a new service's test project, or onboarding to the test conventions. Stop here for fundamentals; integration concerns (Alba, Testcontainers, tracked sessions, async projection waiting) live in `testing-integration`."
cluster: testing
tags: [testing, xunit, shouldly, faketimeprovider, unit-tests, decider-pattern, validators, test-organization]
---

# Testing Fundamentals

Cab takes testability as a first-class architectural concern. The decider pattern, vertical slice organization, and `TimeProvider`-based time injection (per `csharp-coding-standards`) are all chosen partly for testability — they make handlers, validators, and time-dependent logic trivial to unit-test without infrastructure. This skill covers that "trivial unit test" surface and the test-stack baseline that supports it.

**Scope is deliberately narrow.** Anything that needs the Wolverine pipeline, a real database, the async projection daemon, or HTTP scenarios lives in `testing-integration`. This skill is for tests that exercise pure functions and the conventions every test in the suite shares. If you're authoring a test that doesn't need to bootstrap a host, you stop here.

---

## When to apply this skill

Use this skill when:

- Setting up the test project for a new service (per `adding-a-service`).
- Authoring unit tests for a handler's `Validate`, `Before`, or `Handle` static methods.
- Testing a domain aggregate's `Apply` or `Create` methods directly (no Marten round-trip).
- Testing a validator's rules.
- Writing tests that need to control time (`TimeProvider`-injected handlers).
- Onboarding a new contributor to the Cab test conventions.

Do NOT use this skill for:

- HTTP scenarios via Alba — `testing-integration` (next skill).
- Tests that need the Wolverine pipeline (`InvokeMessageAndWaitAsync`, tracked sessions) — `testing-integration`.
- Tests that need real Marten or the async daemon — `testing-integration`.
- Testcontainers patterns for Postgres, SQL Server, ASB Emulator, EH Emulator — `testing-integration`.
- Aspire test-host wiring — `aspire` and `testing-integration`.

---

## The committed test stack

These packages are pinned in `Directory.Packages.props` and apply uniformly across every Cab test project. Versions reflect what's currently committed; treat the file as the source of truth and update only deliberately.

| Package | Version | Role |
|---|---|---|
| `xunit` | 2.9.3 | Test framework. **Note: v2, not v3.** |
| `xunit.runner.visualstudio` | 3.1.5 | VS Test Explorer integration. |
| `Microsoft.NET.Test.Sdk` | 18.4.0 | MSTest SDK harness; required for `dotnet test`. |
| `Shouldly` | 4.3.0 | Assertion library — preferred over xUnit's `Assert.*`. |
| `Alba` | 8.5.2 | HTTP scenario testing. **Used in integration tests only.** |
| `Testcontainers.PostgreSql` | 4.11.0 | Real Postgres in Docker. **Integration tests only.** |
| `Testcontainers.MsSql` | 4.11.0 | Real SQL Server in Docker. **Integration tests only — Polecat services.** |
| `Testcontainers.ServiceBus` | 4.11.0 | Azure Service Bus emulator. **Integration tests only.** |
| `Testcontainers.Kafka` | 4.11.0 | Kafka in Docker. **Integration tests only.** |
| `Microsoft.Extensions.TimeProvider.Testing` | latest | Provides `FakeTimeProvider` for time-dependent tests. |

**xUnit v2.9.3 — not v3.** This matters because `IAsyncLifetime` returns `Task`, not `ValueTask`. Tooling and code generation that targets v3 will produce signatures that fail to compile against the committed version. Don't assume v3 surface.

`Microsoft.Extensions.TimeProvider.Testing` is added per test project that needs `FakeTimeProvider`. The `FakeTimeProvider` type lives in the `Microsoft.Extensions.Time.Testing` namespace (yes, the package and namespace differ) — verified against current Marten test sources.

The test stack does **not** include NSubstitute, Moq, or any other mocking library. Cab handlers are static functions; aggregates are immutable records. There's nothing to mock at the unit-test layer that isn't already addressable through plain object construction. Integration tests use real infrastructure via Testcontainers rather than mocks, per Critter Stack convention.

---

## Test project organization

Every Cab service ships with a paired test project, named `<Service>.Tests` and located alongside the service in `tests/`:

```text
src/
  CritterCab.Trips/
    CritterCab.Trips.csproj
    Program.cs
    Trips/
      Lifecycle/
        StartTrip.cs
        CompleteTrip.cs
      Pricing/
        ApplyDynamicPricing.cs

tests/
  CritterCab.Trips.Tests/
    CritterCab.Trips.Tests.csproj
    Trips/
      Lifecycle/
        start_trip_handler_tests.cs
        complete_trip_handler_tests.cs
      Pricing/
        apply_dynamic_pricing_handler_tests.cs
    Fixtures/
      TripsTestFixture.cs              # Phase 2 → testing-integration
      TripsTestCollection.cs
```

The folder structure under `tests/CritterCab.Trips.Tests/Trips/` mirrors `src/CritterCab.Trips/Trips/` exactly. Vertical slice organization carries over: no `Handlers/`, `Commands/`, or `UnitTests/` folders. A test for `StartTrip.cs` lives next to its production sibling under the same feature folder, just rooted under the test project.

`Fixtures/` is the one exception — host-bootstrapping fixtures and collection definitions for integration tests live in a flat `Fixtures/` folder at the test project root. Those are the subject of `testing-integration`; the folder is mentioned here so the layout is complete.

### Test class naming

Test class names are `<feature>_handler_tests` (snake_case), matching JasperFx core team convention used throughout the Marten and Wolverine test suites. The class file name matches.

```csharp
public sealed class start_trip_handler_tests
{
    // ...
}
```

### Test method naming

Test methods are also snake_case, written as readable sentences:

```csharp
[Fact]
public void valid_start_trip_command_returns_trip_started_event()

[Fact]
public void start_trip_with_inactive_driver_returns_problem_details()

[Fact]
public async Task complete_trip_advances_status_to_completed()
```

This is a convention rather than a rule — what matters is consistency within a service's test project. Cab adopts snake_case because it aligns with the upstream Critter Stack test code and reads cleanly in test runner output. PascalCase with underscores (e.g., `Valid_StartTripCommand_ReturnsTripStartedEvent`) is acceptable but not the default.

---

## xUnit lifecycle

xUnit v2 provides three lifecycle hooks worth knowing. The choice between them depends on what's being shared between tests.

### `IAsyncLifetime` — async setup and teardown for any test class

Implement `IAsyncLifetime` on any test class that needs `await`-able setup or teardown. xUnit calls `InitializeAsync()` before the first test runs and `DisposeAsync()` after the last test completes:

```csharp
public sealed class start_trip_handler_tests : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    // Tests follow...
}
```

Both methods return `Task` (not `ValueTask`) under xUnit 2.9.3. Most pure-handler unit tests don't need `IAsyncLifetime` — synchronous setup in the test body is fine. Reach for `IAsyncLifetime` when a test class needs to seed async data, await an async resource, or coordinate with infrastructure (the dominant case for integration tests).

### `IClassFixture<T>` — share one resource across tests in one class

xUnit creates one `T` instance per test class and disposes it after the class finishes. Inject it via the test class constructor:

```csharp
public sealed class TripFixtureData : IDisposable
{
    public Trip CanonicalActiveTrip { get; }

    public TripFixtureData()
    {
        CanonicalActiveTrip = TripFactory.CreateActive(
            tripId: Guid.CreateVersion7(),
            riderId: Guid.CreateVersion7(),
            driverId: Guid.CreateVersion7());
    }

    public void Dispose() { /* nothing to clean up */ }
}

public sealed class trip_state_query_tests : IClassFixture<TripFixtureData>
{
    private readonly TripFixtureData _data;

    public trip_state_query_tests(TripFixtureData data) => _data = data;

    [Fact]
    public void active_trip_has_started_status()
    {
        _data.CanonicalActiveTrip.Status.ShouldBe(TripStatus.Active);
    }
}
```

`IClassFixture<T>` is uncommon for unit tests — building immutable test data inline is cheaper than the fixture indirection. It earns its place when fixture construction is genuinely expensive (e.g., parsing a large JSON file once and reusing it across scenarios).

### `ICollectionFixture<T>` — share one resource across multiple test classes

Defines a fixture that lives for the duration of an xUnit "test collection" (one or more test classes grouped together). Most useful for integration tests where booting the host is expensive — many test classes share one host instance via collection fixture rather than each booting their own.

```csharp
[CollectionDefinition(Name)]
public sealed class TripsTestCollection : ICollectionFixture<TripsTestFixture>
{
    public const string Name = "Trips Tests";
}

[Collection(TripsTestCollection.Name)]
public sealed class start_trip_endpoint_tests : IAsyncLifetime
{
    private readonly TripsTestFixture _fixture;
    public start_trip_endpoint_tests(TripsTestFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.CleanAllMartenDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;
}
```

`ICollectionFixture<T>` belongs to integration testing; the example is here so the lifecycle picture is complete. Detailed coverage in `testing-integration`.

### Picking the right hook

| Need | Pick |
|---|---|
| Synchronous, per-test setup | Constructor + `IDisposable` |
| Async per-test-class setup | `IAsyncLifetime` |
| One expensive resource shared in one test class | `IClassFixture<T>` |
| One bootstrapped host shared across many integration test classes | `ICollectionFixture<T>` (see `testing-integration`) |

---

## Unit testing pure handlers

The decider pattern (per `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers`) makes handlers static functions over `(command, currentState) → result`. That signature is the easiest possible thing to unit-test — no DI, no transactions, no infrastructure, no mocking.

A typical Cab handler unit test exercises all three of `Validate`, `Before`, and `Handle` as separate methods:

```csharp
public sealed class start_trip_handler_tests
{
    [Fact]
    public void valid_command_passes_validation()
    {
        var cmd = new StartTrip(
            TripId: Guid.CreateVersion7(),
            RiderId: Guid.CreateVersion7(),
            DriverId: Guid.CreateVersion7(),
            PickupLocation: new GeoPoint(41.2565, -95.9345),
            FareEstimate: 1850m);

        var result = StartTripHandler.Validate(cmd);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void negative_fare_estimate_fails_validation()
    {
        var cmd = ValidStartTripCommand() with { FareEstimate = -100m };

        var result = StartTripHandler.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Reason.ShouldBe("FareEstimate must be non-negative");
    }

    [Fact]
    public void start_trip_for_inactive_driver_returns_problem_details()
    {
        var driver = DriverFactory.CreateInactive();
        var cmd = ValidStartTripCommand() with { DriverId = driver.Id };

        var problem = StartTripHandler.Before(cmd, driver);

        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Title.ShouldBe("Driver is not active");
    }

    [Fact]
    public void valid_start_trip_returns_trip_started_event()
    {
        var cmd = ValidStartTripCommand();
        var driver = DriverFactory.CreateActive(cmd.DriverId);

        var (events, messages) = StartTripHandler.Handle(cmd, driver, TimeProvider.System);

        events.OfType<TripStarted>().ShouldHaveSingleItem();
        messages.OfType<TripStartedNotification>().ShouldHaveSingleItem();
    }

    private static StartTrip ValidStartTripCommand() => new(
        TripId: Guid.CreateVersion7(),
        RiderId: Guid.CreateVersion7(),
        DriverId: Guid.CreateVersion7(),
        PickupLocation: new GeoPoint(41.2565, -95.9345),
        FareEstimate: 1850m);
}
```

Three things this pattern gets right:

1. **No infrastructure.** No host, no Marten, no Wolverine pipeline. The handler is a function; the test calls it.
2. **Each phase tested independently.** `Validate` rejection paths don't need state; `Before` short-circuits don't need to construct events; `Handle` happy paths don't need to exercise validation again.
3. **Deterministic.** No clocks, no GUIDs hidden in the production code, no hidden ordering. Inputs in, outputs out.

### Aggregate Apply tests

Aggregates use the decider pattern too — `Create` for new aggregate state and `Apply` for state transitions. Both are static and pure:

```csharp
public sealed class trip_apply_tests
{
    [Fact]
    public void trip_started_event_creates_active_trip()
    {
        var evt = new TripStarted(
            TripId: Guid.CreateVersion7(),
            RiderId: Guid.CreateVersion7(),
            DriverId: Guid.CreateVersion7(),
            StartedAt: DateTimeOffset.UtcNow);

        var trip = Trip.Create(evt);

        trip.Status.ShouldBe(TripStatus.Active);
        trip.RiderId.ShouldBe(evt.RiderId);
    }

    [Fact]
    public void trip_completed_event_advances_status()
    {
        var trip = TripFactory.CreateActive();
        var evt = new TripCompleted(trip.Id, DateTimeOffset.UtcNow, FinalFare: 2150m);

        var updated = Trip.Apply(trip, evt);

        updated.Status.ShouldBe(TripStatus.Completed);
        updated.FinalFare.ShouldBe(2150m);
    }
}
```

The aggregate's immutable `Immutable*` collections (per `marten-aggregates`) and `IReadOnly*` event/DTO collections mean every test produces a new instance — no shared mutable state to cross-contaminate.

---

## Testing validators

When validation logic lives in a `Validate` method on the handler (the dominant Cab pattern), validator tests are just unit tests of that static method. The `Unit testing pure handlers` examples above already cover the shape.

When validation lives in a separate `IValidator<T>` (FluentValidation), test the validator directly — no handler involvement:

```csharp
public sealed class start_trip_validator_tests
{
    private readonly StartTripValidator _validator = new();

    [Fact]
    public void valid_command_passes()
    {
        var cmd = ValidCommand();
        var result = _validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void empty_pickup_location_is_rejected()
    {
        var cmd = ValidCommand() with { PickupLocation = default };
        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(StartTrip.PickupLocation));
    }

    private static StartTrip ValidCommand() => /* ... */;
}
```

Cab's preference is for `Validate` on the handler over external `IValidator<T>` — closer to the work, no DI needed. FluentValidation is appropriate when validation rules are reused across multiple handlers or when the rule set genuinely benefits from FluentValidation's fluent DSL.

---

## Testing time-dependent code with `FakeTimeProvider`

Per `csharp-coding-standards`, every Cab handler that needs the current time receives `TimeProvider` as the **last constructor or method parameter** — never `DateTimeOffset.UtcNow` directly. This convention is what makes time-dependent logic testable: tests pass `FakeTimeProvider` from `Microsoft.Extensions.Time.Testing`, advance time deliberately, and assert against deterministic outcomes.

### Basic FakeTimeProvider usage

```csharp
using Microsoft.Extensions.Time.Testing;

public sealed class trip_timeout_handler_tests
{
    [Fact]
    public void trip_idle_for_less_than_timeout_is_unaffected()
    {
        var time = new FakeTimeProvider(startDateTime: new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero));
        var trip = TripFactory.CreateActive(lastActivity: time.GetUtcNow());

        time.Advance(TimeSpan.FromMinutes(4));   // Less than 5-minute timeout
        var result = TripTimeoutHandler.Handle(trip, time);

        result.ShouldBeNull();   // No timeout event
    }

    [Fact]
    public void trip_idle_past_timeout_emits_timeout_event()
    {
        var time = new FakeTimeProvider(startDateTime: new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero));
        var trip = TripFactory.CreateActive(lastActivity: time.GetUtcNow());

        time.Advance(TimeSpan.FromMinutes(6));   // Past 5-minute timeout
        var evt = TripTimeoutHandler.Handle(trip, time);

        evt.ShouldNotBeNull();
        evt.ShouldBeOfType<TripTimedOut>();
    }
}
```

`FakeTimeProvider` exposes:

- `GetUtcNow()` — current fake time, like `TimeProvider.GetUtcNow()`.
- `Advance(TimeSpan)` — moves the clock forward.
- `SetUtcNow(DateTimeOffset)` — jumps to a specific time.
- `Start` — the original start time, useful for asserting "this happened at the same instant the test started."

### Constructor parameter convention

When `TimeProvider` is on a record's last constructor parameter (the Cab convention), tests pass `FakeTimeProvider` directly:

```csharp
public sealed record StartTripHandler(
    IDriverDirectory Drivers,
    TimeProvider Time)   // ← last parameter
{
    public OutgoingMessages Handle(StartTrip cmd) { /* ... */ }
}

// Test:
var fakeTime = new FakeTimeProvider();
var handler = new StartTripHandler(driversStub, fakeTime);
```

For static handler methods that take `TimeProvider` as the last argument, pass `FakeTimeProvider` the same way:

```csharp
var (events, _) = StartTripHandler.Handle(cmd, driver, fakeTime);
```

### Don't share `FakeTimeProvider` instances across tests

Each test should construct its own `FakeTimeProvider`. Sharing one across tests via fixture is a footgun — one test advancing the clock affects every subsequent test, producing order-dependent flakes that don't reproduce in isolation. The cost of `new FakeTimeProvider()` is negligible; pay it per test.

If a test class genuinely needs many tests at the same starting time, use a private helper:

```csharp
private static FakeTimeProvider FreshClockAt2026Q2() =>
    new(startDateTime: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));
```

### `FakeTimeProvider` with Marten event timestamps

Marten 7.5+ supports overriding the event-timestamp `TimeProvider` per store. This is integration territory — covered in `testing-integration` — but worth knowing the surface exists:

```csharp
opts.Events.TimeProvider = fakeTimeProvider;
```

Verified against current Marten test source. Useful when integration tests assert against `IEvent.Timestamp` or `Created`-style metadata.

---

## Testing failure paths

Failure-path tests assert that the system rejects a request — through `Validate`, through `Before` short-circuiting with `ProblemDetails`, or through an exception. Each has a different shape.

### Validation failures

`Validate` returns a result; assert on the result, not on an exception:

```csharp
[Fact]
public void negative_fare_estimate_fails_validation()
{
    var cmd = ValidStartTripCommand() with { FareEstimate = -100m };

    var result = StartTripHandler.Validate(cmd);

    result.IsValid.ShouldBeFalse();
    result.Reason.ShouldBe("FareEstimate must be non-negative");
}
```

### `Before` short-circuit with `ProblemDetails`

`Before` returning a non-null `ProblemDetails` short-circuits the pipeline. In a unit test, assert the returned `ProblemDetails`:

```csharp
[Fact]
public void start_trip_for_offline_driver_returns_409()
{
    var driver = DriverFactory.CreateOffline();
    var cmd = ValidStartTripCommand() with { DriverId = driver.Id };

    var problem = StartTripHandler.Before(cmd, driver);

    problem.ShouldNotBeNull();
    problem.Status.ShouldBe(StatusCodes.Status409Conflict);
    problem.Title.ShouldBe("Driver is offline");
    problem.Detail.ShouldContain(driver.Id.ToString());
}
```

`ProblemDetails` does not throw — that's the whole point of the short-circuit pattern. Asserting `Should.Throw<>` here is a category error.

### Exceptions for genuinely exceptional cases

Exceptions are reserved for invariant violations (per `csharp-coding-standards`'s guard-clause guidance) — situations where the handler was given inputs that should be impossible if upstream code is correct. Tests for those use `Should.Throw` / `Should.ThrowAsync`:

```csharp
[Fact]
public void apply_to_completed_trip_throws()
{
    var trip = TripFactory.CreateCompleted();
    var evt = new TripStarted(/* ... */);

    var exception = Should.Throw<InvalidOperationException>(() => Trip.Apply(trip, evt));

    exception.Message.ShouldContain("already completed");
}

[Fact]
public async Task driver_directory_returns_failure_propagates()
{
    var stub = new FailingDriverDirectoryStub();
    var handler = new StartTripHandler(stub, TimeProvider.System);

    await Should.ThrowAsync<DriverDirectoryUnavailableException>(
        async () => await handler.Handle(ValidStartTripCommand()));
}
```

The mental model: `ProblemDetails` is for expected business-rule rejections (driver offline, fare too low, trip already completed); exceptions are for "this should never happen if the code upstream is right." Tests follow that distinction.

---

## Shouldly conventions

Shouldly assertions read like English and produce better failure messages than xUnit's `Assert.*`. Cab uses Shouldly uniformly; mixing in raw `Assert.Equal` or `Assert.True` is discouraged.

The patterns that come up most:

```csharp
// Equality
result.Status.ShouldBe(TripStatus.Active);
trip.FareEstimate.ShouldBe(1850m);

// Nullability
trip.ShouldNotBeNull();
problem.ShouldBeNull();

// Booleans
result.IsValid.ShouldBeTrue();
trip.IsCancelled.ShouldBeFalse();

// Collections
events.ShouldNotBeEmpty();
events.ShouldContain(e => e is TripStarted);
events.OfType<TripStarted>().ShouldHaveSingleItem();
events.Count.ShouldBe(2);

// Strings
problem.Detail.ShouldContain("driver");
problem.Title.ShouldStartWith("Trip");

// Numerics with tolerance
elapsed.TotalSeconds.ShouldBeInRange(4.9, 5.1);

// Type checks
result.ShouldBeOfType<TripStarted>();
result.ShouldBeAssignableTo<ITripEvent>();

// Exceptions
Should.Throw<InvalidOperationException>(() => /* sync code */);
await Should.ThrowAsync<TimeoutException>(async () => /* async code */);
```

`ShouldHaveSingleItem()` is preferred over `result.Count().ShouldBe(1).First()` — it's one assertion, returns the single element, and produces a cleaner failure message when the count is wrong.

`Should.Throw<T>` returns the exception instance, so message assertions chain naturally:

```csharp
var ex = Should.Throw<InvalidOperationException>(() => Trip.Apply(completed, evt));
ex.Message.ShouldContain("already completed");
```

### Don't fight Shouldly with custom matchers

If an assertion feels awkward in Shouldly, the test is usually checking too much at once. Split it. Cab's preference is several focused `ShouldBe`/`ShouldContain` calls over one elaborate predicate.

---

## Common pitfalls

- **Assuming xUnit v3 surface.** `IAsyncLifetime` returns `Task` under v2.9.3, not `ValueTask`. Snippets from blog posts targeting v3 won't compile.
- **Sharing `FakeTimeProvider` across tests.** Order-dependent flakes that don't reproduce in isolation. Construct one per test.
- **Asserting `Should.Throw` on `ProblemDetails` short-circuits.** `Before` returning `ProblemDetails` does not throw — assert the returned value, not an exception.
- **Mocking `TimeProvider` with NSubstitute or Moq.** `FakeTimeProvider` is the supported, deterministic option. Don't roll your own.
- **Using `DateTimeOffset.UtcNow` in tests to compute "now-relative" expectations.** Wall-clock time in tests is the same antipattern it is in production code. Inject `FakeTimeProvider`, set a fixed start, and compare against deterministic offsets.
- **Bringing in NSubstitute, Moq, or AutoFixture.** Cab's test stack doesn't include them. Pure handlers and immutable aggregates are constructible with `new`; stubs for ports (`IDriverDirectory`, etc.) are small enough to write by hand. If you find yourself reaching for a mocking library, that's a signal the test should be an integration test instead.
- **Mixing PascalCase and snake_case test names within one test project.** Pick snake_case (Cab default, JasperFx-aligned) and stay consistent. Inconsistent naming is harder to skim than either convention alone.
- **Calling `Should.ThrowAsync` on synchronous code.** Use `Should.Throw<T>(() => ...)` for sync. The async variant only matters when there's an actual `await` inside the lambda.
- **Putting `IAsyncLifetime` on a class that doesn't need async setup.** Empty `Task.CompletedTask` returns are noise. Skip the interface unless you actually `await` something in `InitializeAsync`.
- **Testing `Validate` and `Handle` together when they're separable.** If `Validate` rejects, `Handle` never runs — the integration is the pipeline's job. Unit tests should exercise each phase independently.

---

## See also

**Upstream** — load these first:

- `csharp-coding-standards` — `TimeProvider` injection convention; `required` over `null!`; modern guard clauses; `Guid.CreateVersion7()` for new IDs.
- `vertical-slice-organization` — feature folders and the absence of `Handlers/`/`Commands/` directories that test layout mirrors.
- `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers` — the handler shapes (`Validate`, `Before`, `Handle`) being unit-tested.
- `marten-aggregates` — the aggregate shape (`Create`, `Apply`, `Immutable*` collections) being unit-tested.

**Sibling skills:**

- `adding-a-service` — creates the paired test project per the layout above.
- `marten-wolverine-aggregates` — `[WriteAggregate]`/`[Aggregate]` handler shapes; integration territory once tests touch the pipeline.

**Downstream:**

- `testing-integration` (next, Phase 2) — the TestFixture pattern, Alba composition over `Program.cs`, Testcontainers patterns for Postgres/SQL Server/ASB Emulator/EH Emulator/Kafka, `ExecuteAndWaitAsync`, tracked sessions, async projection waiting (`WaitForNonStaleProjectionDataAsync`, `WaitForConditionAsync`), `IInitialData` seeding, parallelization strategy.
- `aspire` (Phase 2) — local dev host wiring; integration test fixtures may compose against the same `Program.cs`.
- `observability-tracing` (Phase 3) — verifying span and metric emission in integration tests.
- `testing-advanced` (Phase 4) — multi-host scenarios, RabbitMQ vhost isolation, dynamic-database-per-fixture patterns, gRPC streaming test harnesses.

**External:**

- ai-skills `critter-stack-testing-fundamentals` — generic Critter Stack testing baseline; complements this skill.
- All ai-skills installed via `npx skills add` (license required).
- [Wolverine's Baked In Integration Testing Support (Jeremy Miller, March 25, 2024)](https://jeremydmiller.com/2024/03/25/wolverines-baked-in-integration-testing-support/) — the testability philosophy that informs Cab's test conventions.
- [Testing Asynchronous Projections in Marten (Jeremy Miller, March 26, 2024)](https://jeremydmiller.com/2024/03/26/testing-asynchronous-projections-in-marten/) — `FakeTimeProvider` pattern for event timestamps; relevant when integration tests assert against `IEvent.Timestamp`.
- [Faster, More Reliable Integration Testing Against Marten Projections (Jeremy Miller, August 19, 2025)](https://jeremydmiller.com/2025/08/19/faster-more-reliable-integration-testing-against-marten-projections-or-subscriptions/) — Marten 8.8 / Wolverine 4.10 testing improvements.
- [Working and Testing Against Scheduled Messages with Wolverine (Jeremy Miller, September 15, 2025)](https://jeremydmiller.com/2025/09/15/working-and-testing-against-scheduled-messages-with-wolverine/) — `PlayScheduledMessagesAsync`; relevant for integration testing of scheduled messaging.
- [Shouldly Documentation](https://docs.shouldly.org/) — the assertion library used uniformly across Cab tests.
- [xUnit.net Documentation](https://xunit.net/docs/getting-started/v2/) — note v2 specifically; v3 surface differs.
