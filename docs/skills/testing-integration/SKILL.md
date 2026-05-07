---
name: testing-integration
description: "Integration testing for CritterCab services — the per-service Alba+Testcontainers TestFixture pattern, ExecuteAndWaitAsync, tracked-session configuration (Timeout, IncludeExternalTransports, AlsoTrack, DoNotAssertOnExceptionsDetected), event-sourcing race conditions, async projection waiting (WaitForNonStaleProjectionDataAsync, WaitForConditionAsync, PauseThenCatchUpOnMartenDaemonActivity), HTTP scenarios via Alba, scheduled message testing (PlayScheduledMessagesAsync), Testcontainers for Postgres/SQL Server/Kafka/ServiceBus, IInitialData seeding, and parallelization strategy. Use when authoring any test that needs the Wolverine pipeline, real Marten/Polecat, an HTTP scenario, or real broker infrastructure."
cluster: testing
tags: [testing, integration, alba, testcontainers, wolverine-tracking, async-projections, race-conditions, scheduled-messages, postgres, sqlserver, kafka, servicebus]
---

# Testing Integration

Integration tests in Cab boot the real service host, register real Marten or Polecat against a real database in Docker, and exercise full request flows through Wolverine's pipeline. They cost more than unit tests but earn it: they catch handler-discovery bugs, projection-shape bugs, race conditions, and routing mistakes that no amount of mocking can surface.

This skill picks up where `testing-fundamentals` left off. If a test only exercises pure handlers, validators, or aggregate `Apply` methods — that's fundamentals territory. If a test needs the Wolverine pipeline, real Marten, the async daemon, an HTTP scenario, or a real broker — you're in the right place.

The core pattern is **per-service `TestFixture` + Alba composition over `Program.cs` + Testcontainers for storage and brokers**. The fixture boots the same `Program.cs` the service runs in production, with surgical overrides for connection strings and disabled external transports. Tests never construct a parallel DI container; they always exercise the real one.

---

## When to apply this skill

Use this skill when:

- Authoring an integration test that exercises Wolverine handlers end-to-end.
- Setting up the `TestFixture` and collection definition for a new service's test project.
- Asserting on integration messages routed across services (RabbitMQ-equivalent, Kafka, ASB).
- Asserting on async projection state after events are appended.
- Testing HTTP scenarios via Alba.
- Testing scheduled messages that would otherwise wait wall-clock time.
- Diagnosing flaky tests that look like timing issues — usually the race condition in §"The race condition every event-sourced test hits."

Do NOT use this skill for:

- Pure-handler unit tests, validator tests, or aggregate `Apply` tests — `testing-fundamentals`.
- Multi-host or multi-tenant fixture orchestration, gRPC streaming test harnesses, RabbitMQ vhost isolation — `testing-advanced` (Phase 4).
- Aspire-orchestrated local dev composition — `aspire` (Phase 2).
- Running Cab CLI commands in tests — `cli-jasperfx` (Phase 2).

---

## The integration test mental model

Three things distinguish a good Cab integration test from a flaky one.

**Compose against `Program.cs`, not a parallel container.** Per Jeremy Miller's "use the actual application bootstrapping" guidance, the fixture builds the real service host via `AlbaHost.For<Program>(b => b.ConfigureServices(...))`. Overrides go in `ConfigureServices` because `Program.cs` reads connection strings inline (`builder.Configuration.GetConnectionString("postgres")`) before the test factory's `ConfigureAppConfiguration` callbacks have a chance to apply. `ConfigureServices` runs after `Program.cs` and wins by last-registration semantics.

**Real infrastructure via Testcontainers.** Cab's test stack (per `testing-fundamentals`) commits Testcontainers for Postgres, SQL Server, Kafka, and Azure Service Bus — all four. Tests run against a real Postgres container, not an in-memory fake. The cost is ~3–5 seconds per fixture cold-start; the gain is catching schema drift, projection bugs, and SQL generation issues before production.

**Wait for work to complete; never `Task.Delay`.** Wolverine commits transactions asynchronously after handlers return. Marten's async daemon catches up after `SaveChangesAsync`. Both produce the same failure mode: an HTTP POST returns 200, the test queries the result, and the data isn't there yet because the transaction or projection hasn't committed. The `ExecuteAndWaitAsync` and `WaitForNonStaleProjectionDataAsync` APIs exist precisely for this — `Task.Delay` is never the right answer.

---

## The per-service TestFixture pattern

Every Cab service has one paired test project containing one `TestFixture` per storage backend. For Marten services this is `<Service>TestFixture` provisioning a Postgres container; for Polecat services it's the same shape with a SQL Server container.

```csharp
// tests/CritterCab.Trips.Tests/Fixtures/TripsTestFixture.cs
using Alba;
using JasperFx.CommandLine;
using JasperFx.Events;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wolverine;
using Wolverine.Marten;

namespace CritterCab.Trips.Tests.Fixtures;

public sealed class TripsTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithName($"trips-test-{Guid.NewGuid():N}")
        .WithCleanUp(true)
        .Build();

    public IAlbaHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Required when the service uses RunJasperFxCommands for CLI dispatch.
        // Without this, the host won't start in test factory scenarios.
        JasperFxEnvironment.AutoStartHost = true;

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Register Marten with the Testcontainers connection string.
                // Program.cs's AddMarten(...) is null-guarded on the Aspire connection string,
                // which is absent in tests. ConfigureServices runs after Program.cs, so this
                // registration always wins for IDocumentStore resolution.
                services.AddMarten(opts =>
                {
                    opts.Connection(_postgres.GetConnectionString());
                    opts.DatabaseSchemaName = "public";
                    opts.Events.AppendMode = EventAppendMode.Quick;
                    opts.DisableNpgsqlLogging = true;
                })
                .UseLightweightSessions()
                .ApplyAllDatabaseChangesOnStartup()
                .IntegrateWithWolverine();

                // Critter Stack testing posture: solo mode, no external transports.
                services.RunWolverineInSoloMode();
                services.DisableAllExternalWolverineTransports();

                // Force daemon to Solo regardless of production HotCold/Wolverine-managed
                // configuration — faster startup, no leader election in tests.
                services.MartenDaemonModeIsSolo();
            });
        });
    }

    public async Task DisposeAsync()
    {
        if (Host is not null)
        {
            try
            {
                await Host.StopAsync();
                await Host.DisposeAsync();
            }
            catch (ObjectDisposedException) { }
            catch (TaskCanceledException) { }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e =>
                e is OperationCanceledException or ObjectDisposedException)) { }
        }

        await _postgres.DisposeAsync();
    }

    // Convenience helpers used by every test class.
    public IDocumentSession LightweightSession() =>
        Host.DocumentStore().LightweightSession();

    public Task CleanAllMartenDataAsync() => Host.CleanAllMartenDataAsync();
    public Task ResetAllMartenDataAsync() => Host.ResetAllMartenDataAsync();
}
```

### Why every line is there

- `PostgreSqlBuilder().WithImage("postgres:17-alpine").WithName($"trips-test-{Guid.NewGuid():N}")` — pinning a specific image avoids surprise upgrades; the unique name prevents container-name collisions when two fixtures happen to start in parallel.
- `JasperFxEnvironment.AutoStartHost = true` — required because Cab service `Program.cs` files use `RunJasperFxCommands` for CLI dispatch. Without this static toggle, the host doesn't start when Alba composes against `Program`.
- `services.AddMarten(...).IntegrateWithWolverine()` in `ConfigureServices` — overrides Program.cs's inline `GetConnectionString` registration. The connection string from Aspire is null in tests; without this override, Marten doesn't get registered at all.
- `RunWolverineInSoloMode()` — disables Wolverine's leader election. One node, one set of agents. Faster startup; no Postgres advisory lock contention.
- `DisableAllExternalWolverineTransports()` — prevents Wolverine from connecting to RabbitMQ, Kafka, ASB. External-transport messages are dispatched into Wolverine's tracking system without leaving the test process.
- `MartenDaemonModeIsSolo()` — forces `DaemonMode.Solo` even if Program.cs configures HotCold or Wolverine-managed distribution (per `marten-async-daemon`). Solo is correct for tests; the production mode would either spin up election infrastructure unnecessarily or fight with the test's single-node assumption.
- The `DisposeAsync` exception swallows are pragmatic — Alba and Wolverine occasionally race on shutdown; suppressing `ObjectDisposedException`/`TaskCanceledException` in the dispose path avoids spurious test-suite teardown failures.

### Collection definition

```csharp
// tests/CritterCab.Trips.Tests/Fixtures/TripsTestCollection.cs
[CollectionDefinition(Name)]
public sealed class TripsTestCollection : ICollectionFixture<TripsTestFixture>
{
    public const string Name = "Trips Tests";
}
```

One collection per fixture. Every test class in the service's test project belongs to this collection (`[Collection(TripsTestCollection.Name)]`), sharing one host for the entire test run. The fixture is constructed once at collection start, disposed at collection end.

### Test class lifecycle

```csharp
[Collection(TripsTestCollection.Name)]
public sealed class start_trip_endpoint_tests : IAsyncLifetime
{
    private readonly TripsTestFixture _fixture;

    public start_trip_endpoint_tests(TripsTestFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.CleanAllMartenDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task starts_trip_and_persists_event_stream()
    {
        // ...
    }
}
```

`CleanAllMartenDataAsync()` runs in `InitializeAsync`, never `DisposeAsync`. xUnit doesn't guarantee class execution order; cleaning on exit doesn't protect the next class from inherited state. Cleaning on entry is the correct invariant.

When the service registers async projections, swap to `ResetAllMartenDataAsync()` — it pauses the daemon, clears data, and resumes. Skipping the pause means the daemon keeps processing stale events while cleanup happens, producing intermittent "phantom" projections.

| Method | When to use |
|---|---|
| `CleanAllMartenDataAsync()` | Standard cleanup — services with no async projections. |
| `ResetAllMartenDataAsync()` | Services with async projections registered. |

---

## The race condition every event-sourced test hits

Wolverine's transactional middleware commits asynchronously. `AutoApplyTransactions()` schedules the transaction to commit after the handler returns, but the HTTP response races with the commit:

```csharp
// ❌ WRONG — race condition
await _fixture.Host.Scenario(s =>
{
    s.Post.Json(new StartTrip(...)).ToUrl("/api/trips");
    s.StatusCodeShouldBe(204);
});

// Transaction may not be committed yet; this query reads stale state.
await using var session = _fixture.LightweightSession();
var trip = await session.Events.AggregateStreamAsync<Trip>(tripId);
trip.ShouldNotBeNull();  // FLAKY — sometimes null
```

The fix is to drive the handler through Wolverine's tracked-session machinery, which waits for all transaction commits and cascaded message handling to complete:

```csharp
// ✅ CORRECT — wait for full commit
await _fixture.Host.InvokeMessageAndWaitAsync(new StartTrip(tripId, ...));

await using var session = _fixture.LightweightSession();
var trip = await session.Events.AggregateStreamAsync<Trip>(tripId);
trip.ShouldNotBeNull();
trip.Status.ShouldBe(TripStatus.Active);
```

`InvokeMessageAndWaitAsync` invokes a message through Wolverine's pipeline and returns an `ITrackedSession` once every transaction commits and every cascaded message has been handled (or has timed out). It is the canonical way to test command handlers when the goal is asserting persisted state.

For HTTP-flavored tests where you specifically want to exercise the endpoint surface (status codes, content negotiation, validation), wrap the Alba scenario in `ExecuteAndWaitAsync`:

```csharp
public async Task<(ITrackedSession, IScenarioResult)> TrackedHttpCall(
    Action<Scenario> configuration)
{
    IScenarioResult result = null!;
    var tracked = await Host.ExecuteAndWaitAsync(async () =>
    {
        result = await Host.Scenario(configuration);
    });
    return (tracked, result);
}
```

This pattern is borrowed verbatim from Wolverine's own test suite — the outer `ExecuteAndWaitAsync` waits for full message propagation while the inner `Host.Scenario` exercises the HTTP layer.

### Choosing the right tool

| Test goal | API |
|---|---|
| Aggregate state transitions | `InvokeMessageAndWaitAsync` + query event store directly |
| HTTP contract (status codes, validation, content negotiation) | `Host.Scenario` directly (no tracking needed) |
| HTTP scenario that publishes integration messages | `TrackedHttpCall` (Alba scenario inside `ExecuteAndWaitAsync`) |
| Cascading flows where the test needs to observe downstream effects | `InvokeMessageAndWaitAsync` + assertions on `tracked.Sent`, `tracked.Received` |

`Task.Delay()` is never on this list. Timing-based fixes pass on a developer laptop and fail on a loaded CI machine. If a test still flakes after using the right tracking API, the bug is real — usually a missing routing rule or an async projection the test forgot to wait on.

### Void-handler endpoints return 204

Wolverine HTTP endpoints that return `void` respond with **204 No Content**, not 200. Same for `[WriteAggregate]` endpoints that cascade events without a returned body:

```csharp
[WolverinePost("/api/trips/{tripId}/complete"), EmptyResponse]
public static (IResult, TripCompleted) Handle(CompleteTrip cmd, [WriteAggregate] Trip trip)
    => (Results.NoContent(), new TripCompleted(trip.Id, ...));

// Test:
await _fixture.Host.Scenario(s =>
{
    s.Post.Json(new CompleteTrip(tripId)).ToUrl($"/api/trips/{tripId}/complete");
    s.StatusCodeShouldBe(204);  // Not 200!
});
```

---

## Tracked-session configuration

`TrackActivity()` returns a `TrackedSessionConfiguration` builder for fine-tuning. Four knobs come up in practice:

```csharp
var session = await Host.TrackActivity()
    .Timeout(30.Seconds())                    // Default 5s; extend for slow flows
    .IncludeExternalTransports()              // Track messages routed to disabled transports
    .AlsoTrack(otherHost)                     // Multi-host scenarios
    .DoNotAssertOnExceptionsDetected()        // Inspect exceptions yourself
    .InvokeMessageAndWaitAsync(command);
```

### `Timeout(TimeSpan)`

Default is 5 seconds. Cab CI runners under load routinely exceed this for flows that traverse async daemon catch-up or cross-service messaging. Apply per-test rather than globally — a 30-second blanket timeout would just mask real flakes:

```csharp
var session = await Host.TrackActivity()
    .Timeout(30.Seconds())
    .InvokeMessageAndWaitAsync(new StartTrip(...));
```

### `IncludeExternalTransports()`

By default, tracked sessions ignore messages routed to external transports (Kafka, ASB, RabbitMQ-style). Cab's test fixture disables external transports entirely via `DisableAllExternalWolverineTransports()` — but the tracked-session default still excludes their `Sent` records. Enable explicitly when asserting on integration messages destined for external transports:

```csharp
var session = await Host.TrackActivity()
    .IncludeExternalTransports()
    .InvokeMessageAndWaitAsync(new CompleteTrip(...));

session.Sent.MessagesOf<TripCompletedNotification>().ShouldHaveSingleItem();
```

### `AlsoTrack(IHost)` and `AlsoTrack(IServiceProvider)`

For multi-host scenarios — testing a flow that crosses two service boundaries — register the additional host. Each host's tracking is observed; the session reports completion when both have quiesced. Most Cab tests are single-service and don't need this; reach for it when authoring a Trips→Pricing handoff test that boots both services in the same fixture (rare, advanced territory).

### `DoNotAssertOnExceptionsDetected()`

By default, exceptions thrown inside handlers cause the tracked session to throw on `await`. Disable this when the test is specifically validating a failure path:

```csharp
var session = await Host.TrackActivity()
    .DoNotAssertOnExceptionsDetected()
    .InvokeMessageAndWaitAsync(new StartTrip(/* invalid input */));

session.AllExceptions().Any(e => e is InvariantViolationException).ShouldBeTrue();
```

Prefer fixing the handler or asserting against `ProblemDetails` short-circuits (which don't throw, per `testing-fundamentals`) over reaching for this knob. It's an escape hatch for genuinely-exceptional paths.

### Asserting on tracked messages

Three buckets matter on a returned `ITrackedSession`:

- `tracked.Sent` — outgoing messages dispatched through a routing rule.
- `tracked.NoRoutes` — outgoing messages with no routing rule (cascaded events that were never destined anywhere).
- `tracked.Received` — incoming messages handled during the session.

`tracked.Sent.MessagesOf<T>()` returns `IEnumerable<T>`; `tracked.Sent.SingleMessage<T>()` asserts count = 1 and returns the payload in one step. Prefer the latter for clarity:

```csharp
var notification = session.Sent.SingleMessage<TripCompletedNotification>();
notification.TripId.ShouldBe(tripId);
notification.FinalFare.ShouldBe(2150m);
```

If `tracked.Sent.MessagesOf<T>()` returns 0 unexpectedly, the message likely has no routing rule and landed in `tracked.NoRoutes` instead. Diagnose with `dotnet run -- describe-routing` (per `cli-jasperfx`) or check `tracked.NoRoutes`.

---

## Testing scheduled messages

Wolverine's `ScheduleAsync`, `DelayedFor`, and `ScheduledAt` produce messages that aren't executed by `InvokeMessageAndWaitAsync` — they sit in the inbox until their scheduled time arrives. Two test patterns:

### Assert the schedule, don't run it

```csharp
[Fact]
public async Task complete_trip_schedules_payment_settlement()
{
    var session = await _fixture.Host.InvokeMessageAndWaitAsync(
        new CompleteTrip(tripId, ...));

    var scheduled = session.Scheduled.SingleMessage<SettlePayment>();
    scheduled.TripId.ShouldBe(tripId);
}
```

Fast and deterministic — the test verifies the schedule was set up correctly without waiting for the scheduled time.

### Fast-forward with `PlayScheduledMessagesAsync` (Wolverine 4.12+)

When the test needs to verify the **downstream effects** of the scheduled handler:

```csharp
[Fact]
public async Task settle_payment_after_trip_completion_marks_paid()
{
    var initial = await _fixture.Host.InvokeMessageAndWaitAsync(
        new CompleteTrip(tripId, ...));
    initial.Scheduled.SingleMessage<SettlePayment>().ShouldNotBeNull();

    // Fast-forward — execute the scheduled handler immediately
    var played = await initial.PlayScheduledMessagesAsync();

    // Assert downstream effects of SettlePayment
    await using var session = _fixture.LightweightSession();
    var trip = await session.Events.AggregateStreamAsync<Trip>(tripId);
    trip!.PaymentStatus.ShouldBe(PaymentStatus.Settled);
}
```

This is the canonical pattern for testing trip-cleanup timeouts, dispatch retry timers, or any scheduled business logic without burning wall-clock seconds. See Jeremy Miller's [scheduled-messaging post (September 15, 2025)](https://jeremydmiller.com/2025/09/15/working-and-testing-against-scheduled-messages-with-wolverine/) for the full surface.

---

## Testing async projections

Async projections run on the projection daemon after `SaveChangesAsync` returns — they are NOT updated inline (per `marten-async-daemon`). Tests that append events and immediately query projected documents will see empty results unless they wait for the daemon to catch up.

### `WaitForNonStaleProjectionDataAsync` — the blanket wait

Waits for every running projection in the store to catch up to the current high-water mark:

```csharp
[Fact]
public async Task trip_started_updates_active_trips_view()
{
    await _fixture.Host.InvokeMessageAndWaitAsync(new StartTrip(tripId, ...));

    await _fixture.Host.DocumentStore()
        .WaitForNonStaleProjectionDataAsync(5.Seconds());

    await using var session = _fixture.LightweightSession();
    var view = await session.LoadAsync<ActiveTripView>(tripId);
    view.ShouldNotBeNull();
    view!.Status.ShouldBe(TripStatus.Active);
}
```

Default for "I don't care which projection, just make sure the daemon is caught up." Available on `IHost`, `IDocumentStore`, and `IMartenDatabase` per Marten 7.5+ — verified against current `Marten.Events.AsyncProjectionTestingExtensions`.

### `WaitForConditionAsync` — condition-based polling

When the blanket wait is too coarse (other projection work delays the test) or too broad (daemon-wide catch-up is more than the test needs), poll a specific condition with a bounded timeout:

```csharp
[Fact]
public async Task trip_started_updates_driver_activity_view()
{
    await _fixture.Host.InvokeMessageAndWaitAsync(
        new StartTrip(tripId, riderId, driverId, ...));

    await _fixture.Host.WaitForConditionAsync(async () =>
    {
        await using var session = _fixture.LightweightSession();
        var activity = await session.LoadAsync<DriverActivityView>(driverId);
        return activity?.ActiveTripCount == 1;
    }, timeout: 10.Seconds());
}
```

`WaitForConditionAsync` is the right replacement for `Task.Delay(500)` patterns. It polls with a bounded timeout and fails the test cleanly with a useful message if the condition never becomes true.

### Tracked-session daemon helpers (`Wolverine.Marten.TestingExtensions`)

When the test uses tracked sessions and async projections together, three extensions on `TrackedSessionConfiguration` make the integration cleaner — verified at `C:\Code\JasperFx\wolverine\src\Persistence\Wolverine.Marten\TestingExtensions.cs`:

```csharp
var session = await Host.TrackActivity()
    .Timeout(30.Seconds())
    .ResetAllMartenDataFirst()                       // Pause + reset before invoking
    .PauseThenCatchUpOnMartenDaemonActivity()        // Pause daemon, run, catch up
    .WaitForNonStaleDaemonDataAfterExecution(10.Seconds())
    .InvokeMessageAndWaitAsync(command);
```

- `ResetAllMartenDataFirst()` — resets all Marten data before invoking the message; cleaner than separate cleanup calls.
- `PauseThenCatchUpOnMartenDaemonActivity()` — pauses the daemon during invocation, runs the message, then catches up. Eliminates a class of races where the daemon processes events from the test's setup phase mid-execution.
- `WaitForNonStaleDaemonDataAfterExecution(timeout)` — calls `WaitForNonStaleProjectionDataAsync` after the tracked session completes.

These compose; pick whichever combination matches the test's needs. For most tests, `WaitForNonStaleDaemonDataAfterExecution` alone is sufficient.

See Jeremy Miller's [faster-projection-testing post (August 19, 2025)](https://jeremydmiller.com/2025/08/19/faster-more-reliable-integration-testing-against-marten-projections-or-subscriptions/) for the rationale and full surface.

---

## HTTP scenarios via Alba

Alba composes against `Program.cs` in-process — no real network. `Host.Scenario` runs an HTTP scenario; `TrackedHttpCall` wraps it in tracking when integration messages are involved.

```csharp
[Fact]
public async Task post_trip_with_invalid_fare_returns_400()
{
    await _fixture.Host.Scenario(s =>
    {
        s.Post.Json(new StartTrip(tripId, riderId, driverId,
            PickupLocation: new GeoPoint(41.2565, -95.9345),
            FareEstimate: -100m))
            .ToUrl("/api/trips");

        s.StatusCodeShouldBe(400);
        s.ContentShouldContain("FareEstimate");
    });
}

[Fact]
public async Task complete_trip_publishes_settlement_request()
{
    await SeedActiveTrip(tripId);

    var (tracked, _) = await _fixture.TrackedHttpCall(s =>
    {
        s.Post.Json(new CompleteTrip(tripId)).ToUrl($"/api/trips/{tripId}/complete");
        s.StatusCodeShouldBe(204);
    });

    var request = tracked.Sent.SingleMessage<SettlePayment>();
    request.TripId.ShouldBe(tripId);
}
```

Alba's `Scenario` API includes:
- `s.Get.Url(...)`, `s.Post.Json(payload).ToUrl(...)`, `s.Put.Json(...)`, `s.Delete.Url(...)` — request shape.
- `s.StatusCodeShouldBe(int)` — status code assertion.
- `s.ContentShouldContain(string)`, `s.ContentTypeShouldBe(string)` — body and header assertions.
- `s.WithRequestHeader(name, value)` — add headers (auth tokens, correlation IDs).

For richer body assertions, deserialize from the result:

```csharp
var result = await _fixture.Host.Scenario(s =>
{
    s.Get.Url($"/api/trips/{tripId}");
    s.StatusCodeShouldBe(200);
});

var trip = result.ReadAsJson<TripView>();
trip!.Status.ShouldBe(TripStatus.Active);
```

---

## Testcontainers patterns

Cab's stack commits four Testcontainers libraries. The fixture pattern adapts cleanly across all four.

### Postgres (Marten services — the canonical case)

Shown in the `TripsTestFixture` example above. Image pin to `postgres:17-alpine` for fast cold start.

### SQL Server (Polecat services)

```csharp
private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
    .WithImage("mcr.microsoft.com/mssql/server:2025-CU1-ubuntu-24.04")
    .WithPassword("CritterCab#Test2025!")
    .WithName($"telemetry-test-{Guid.NewGuid():N}")
    .WithCleanUp(true)
    .Build();

// In ConfigureServices:
services.AddPolecat(opts =>
{
    opts.Connection(_sqlServer.GetConnectionString());
    // ...
}).IntegrateWithWolverine();
```

The shape mirrors the Marten case exactly; only the container builder and the `AddPolecat` registration differ. `RunWolverineInSoloMode()`, `DisableAllExternalWolverineTransports()`, and the daemon-mode override apply identically. See `polecat-event-sourcing` (Phase 4) for Polecat-specific test concerns.

### Kafka (Telemetry-style services)

```csharp
private readonly KafkaContainer _kafka = new KafkaBuilder()
    .WithImage("confluentinc/cp-kafka:7.6.1")
    .Build();

// In ConfigureServices — DON'T call DisableAllExternalWolverineTransports for this fixture:
services.AddWolverine(opts =>
{
    opts.UseKafka(_kafka.GetBootstrapAddress())
        .AutoProvision();

    opts.PublishMessage<TelemetryReceived>().ToKafkaTopic("trip-telemetry");
});
```

When the test specifically exercises Kafka routing — not the default for most tests — disable the blanket transport suppression and let Wolverine connect to the real (containerized) Kafka. Tests that just need to assert "the handler emitted the message" should stay on the disabled-transports path with `IncludeExternalTransports()` on the tracked session.

### Azure Service Bus emulator

```csharp
private readonly ServiceBusContainer _serviceBus = new ServiceBusBuilder()
    .WithAcceptLicenseAgreement(true)
    .Build();

// In ConfigureServices:
services.AddWolverine(opts =>
{
    opts.UseAzureServiceBus(_serviceBus.GetConnectionString())
        .AutoProvision();
});
```

The emulator is licensed; `WithAcceptLicenseAgreement(true)` is required and signals Microsoft EULA acceptance. The emulator supports queues, topics, and subscriptions — sufficient for routing-rule tests. Production-fidelity edge cases (dead-lettering with extreme volume, per-message TTL races) are not perfectly emulated; production-only validation is appropriate for those.

### Parallel container startup

When a single fixture needs multiple containers (Postgres + Kafka, for example), start them in parallel:

```csharp
public async Task InitializeAsync()
{
    await Task.WhenAll(
        _postgres.StartAsync(),
        _kafka.StartAsync()
    );
    // ... boot host
}
```

Saves ~3–5 seconds per additional container on cold runs. Sequential startup is acceptable but avoidable.

### `PullPolicy.Missing` for CI

Always set `WithPullPolicy(PullPolicy.Missing)` in CI configurations. Without it, Testcontainers re-pulls the image on every test run, adding 10–30 seconds per fixture:

```csharp
private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
    .WithImage("postgres:17-alpine")
    .WithPullPolicy(PullPolicy.Missing)  // Use cached image when present
    .Build();
```

Local dev runs benefit too, though Docker's layer cache covers most cases. CI is where the difference is dramatic.

---

## `IInitialData` seeding

When a service has reference data every test class depends on (canonical riders, route definitions, fare schedules), `IInitialData` populates it after schema creation and before any test code runs:

```csharp
public sealed class CanonicalRiders : IInitialData
{
    public static readonly Guid AliceId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    public static readonly Guid BobId   = Guid.Parse("00000000-0000-0000-0000-000000000011");

    public async Task Populate(IDocumentStore store, CancellationToken cancellation)
    {
        await using var session = store.LightweightSession();
        session.Store(
            new RiderProfile { Id = AliceId, DisplayName = "Alice Test" },
            new RiderProfile { Id = BobId,   DisplayName = "Bob Test"   });
        await session.SaveChangesAsync(cancellation);
    }
}

// In the fixture's AddMarten lambda:
services.AddMarten(opts =>
{
    opts.Connection(_postgres.GetConnectionString());
    opts.InitialData.Add(new CanonicalRiders());
})
.UseLightweightSessions()
.ApplyAllDatabaseChangesOnStartup();
```

Seed data survives `CleanAllMartenDataAsync` only if the fixture re-runs `Populate` afterward. The clean-and-reseed helper:

```csharp
public async Task CleanAndReseedAsync()
{
    await Host.CleanAllMartenDataAsync();
    await new CanonicalRiders().Populate(Host.DocumentStore(), CancellationToken.None);
}
```

Test classes that depend on the seed data call `CleanAndReseedAsync()` in `InitializeAsync` rather than the bare `CleanAllMartenDataAsync()`.

When seed data must survive every cleanup operation across the full test suite — rare — register a custom `IDocumentStore.Advanced.Clean.IgnoredDocumentTypes` policy. Most Cab services don't need this; inline test seeding is the dominant pattern.

---

## Parallelization strategy

Cab integration tests share a Postgres container per fixture. Two safe strategies for handling parallelism:

### Strategy 1 — Sequential within a collection (Cab default)

```csharp
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TripsTestCollection : ICollectionFixture<TripsTestFixture>
{
    public const string Name = "Trips Tests";
}
```

Every test class in the collection runs sequentially, sharing one fixture and one container. Simple to reason about; no test-method interference. The cost is wall-clock test runtime — but Cab service test projects are small enough (per-service scope) that this is acceptable.

### Strategy 2 — Unique IDs per test, allow parallelism

When Cab service test projects grow large enough that sequential runs become a bottleneck, drop `DisableParallelization` and use `Guid.CreateVersion7()` for every aggregate ID:

```csharp
[Fact]
public async Task starts_trip_with_unique_id()
{
    var tripId = Guid.CreateVersion7();   // Unique per invocation — no collision
    await _fixture.Host.InvokeMessageAndWaitAsync(new StartTrip(tripId, ...));
    // ...
}
```

xUnit then runs test methods within a class in parallel. The fixture's host is shared (`ICollectionFixture` semantics); tests don't step on each other because they don't share state.

### Project-wide baseline

For services where Strategy 2 isn't fully verified, set a project-level baseline:

```csharp
// AssemblyInfo.cs
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

Sequential collection execution at the project level. Opt in to parallelism once unique-ID discipline is verified across the test suite.

### Tracked-session timeouts under parallel load

Parallel tests contend for Testcontainer resources and daemon catch-up. The default 5-second tracked-session timeout will expire mid-test under load. Bump per-test:

```csharp
var session = await Host.TrackActivity()
    .Timeout(30.Seconds())  // Generous for loaded CI machines
    .InvokeMessageAndWaitAsync(command);
```

---

## Common pitfalls

- **`Task.Delay` to "fix" race conditions.** Never the right answer. The right APIs are `InvokeMessageAndWaitAsync`, `WaitForNonStaleProjectionDataAsync`, `WaitForConditionAsync`, and `PlayScheduledMessagesAsync`.
- **Cleaning data in `DisposeAsync`.** xUnit doesn't guarantee class execution order. Clean in `InitializeAsync` so the class starts with a known state.
- **Forgetting `ResetAllMartenDataAsync` for services with async projections.** `CleanAllMartenDataAsync` doesn't pause the daemon; the daemon may keep processing events from the prior test mid-cleanup. Use `ResetAllMartenDataAsync` whenever async projections are registered.
- **Asserting on `tracked.Sent` for cascaded events with no routing rule.** Those land in `tracked.NoRoutes`. Check both buckets when in doubt; use `dotnet run -- describe-routing` to verify routing.
- **Forgetting `JasperFxEnvironment.AutoStartHost = true`.** Without it, the host doesn't start in WebApplicationFactory/Alba scenarios because Cab Program.cs files use `RunJasperFxCommands` for CLI dispatch.
- **Using `ConfigureAppConfiguration` to override connection strings.** Doesn't work — Program.cs reads connection strings inline before `ConfigureAppConfiguration` callbacks apply. Use `ConfigureServices` with `services.AddMarten(opts => opts.Connection(...))` instead.
- **Sharing a Testcontainer name across fixtures without `Guid.NewGuid()`.** Container-name collisions when multiple test runs overlap. Always include a unique suffix.
- **Skipping `RunWolverineInSoloMode()`.** Without it, Wolverine attempts leader election on every test startup. Slower; sometimes flaky on CI.
- **Skipping `DisableAllExternalWolverineTransports()` when tests don't actually exercise the transports.** Wolverine will try to connect to RabbitMQ/Kafka/ASB during host startup and fail. Always disable unless the test explicitly provisions a real broker container.
- **Treating the default 5-second timeout as universal.** It's too tight for flows that traverse async daemon catch-up, scheduled-message playback, or multi-host AlsoTrack scenarios. Bump per-test where it matters.
- **Hard-coded GUIDs that collide under parallelism.** Use `Guid.CreateVersion7()` per test invocation; reserve well-known IDs for `IInitialData` reference data only.
- **Using `WaitForNonStaleProjectionDataAsync` when the test only cares about one specific projection.** The blanket wait blocks on every running projection. `WaitForConditionAsync` is more surgical when other projections are slow or unrelated.

---

## See also

**Upstream** — generic Wolverine + Marten integration testing fundamentals this skill builds on. ai-skills (license required, install via `npx skills add`):

- `wolverine-testing-integration` (primary) — baseline integration testing patterns: `IAlbaHost.For<Program>`, `ExecuteAndWaitAsync`/`InvokeMessageAndWaitAsync`, tracked-session API, `RunWolverineInSoloMode`, `DisableAllExternalWolverineTransports`. Cab's skill applies these with project-specific framing (per-service TestFixture pattern with Testcontainer-per-fixture, the `JasperFxEnvironment.AutoStartHost` requirement, `ConfigureServices`-not-`ConfigureAppConfiguration` connection-string override rule, `MartenDaemonModeIsSolo()` for solo daemon, dispose-path exception swallows for Alba/Wolverine shutdown races).
- `wolverine-testing-integration-marten` — Marten-specific integration testing: `CleanAllMartenDataAsync` vs `ResetAllMartenDataAsync` (when async projections are registered), `WaitForNonStaleProjectionDataAsync` for projection catch-up, `IInitialData` seeding patterns, the race condition between Wolverine's transactional middleware and the HTTP response.
- `wolverine-testing-with-testcontainers` — Testcontainers-driven integration testing: Postgres/SQL Server/Kafka/ServiceBus container builders, image pinning, unique container naming for parallel test runs, the lifecycle integration with `IAsyncLifetime`.
- `wolverine-testing-with-aspire` — Aspire-orchestrated integration testing: composing tests against an Aspire AppHost rather than per-service Testcontainers, when each strategy is appropriate.
- `wolverine-testing-test-parallelization` — xUnit parallelization strategies: `[CollectionDefinition(DisableParallelization = true)]` for sequential-within-collection, unique-ID discipline for cross-test parallelism, project-level `CollectionBehavior` baseline, tracked-session timeout adjustments under parallel load. Cab's skill calls out both Strategy 1 (sequential, default) and Strategy 2 (parallel with unique IDs) explicitly.

**Prerequisites** — Cab-internal skills to load first:

- `testing-fundamentals` — committed test stack, xUnit lifecycle, unit testing pure handlers, Shouldly conventions, `FakeTimeProvider`. Read first.
- `service-bootstrap` — `AddMarten`, `IntegrateWithWolverine`, `AddAsyncDaemon`, `DurabilityMode`; the Program.cs surface fixtures compose against.
- `marten-async-daemon` — daemon modes (Solo, HotCold, Wolverine-managed); error handling; rebuild patterns. Critical for understanding why `MartenDaemonModeIsSolo()` matters in fixtures.
- `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers` — handler shapes being exercised end-to-end.
- `marten-projections` — projection lifecycles; what async projections need waiting on.

**Sibling skills:**

- `marten-querying` — read-side consequences of eventual consistency; `WaitForNonStaleProjectionDataAsync` rationale.
- `marten-wolverine-aggregates` — `[WriteAggregate]`/`[Aggregate]` handler shapes and the `EmptyResponse` / 204 status convention.
- `dynamic-consistency-boundary` — DCB write-path tests; `[BoundaryModel]` setup uses the same fixture pattern.

**Downstream:**

- `aspire` (Phase 2) — local dev wiring; integration test fixtures may compose against the same Aspire-orchestrated `Program.cs`.
- `cli-jasperfx` (Phase 2) — `describe-routing`, `codegen-preview` for diagnosing failing tests; same CLI surface that test fixtures verify by working at all.
- `wolverine-grpc-handlers` (Phase 3) — gRPC-streaming integration tests need extensions to this skill's HTTP-scenario patterns.
- `wolverine-kafka` (Phase 3) — Kafka transport tests; `Testcontainers.Kafka` setup beyond the brief example here.
- `wolverine-azure-service-bus` (Phase 3) — ASB emulator tests; same.
- `wolverine-sagas` (Phase 4) — saga timeout tests via `PlayScheduledMessagesAsync`.
- `testing-advanced` (Phase 4) — multi-host scenarios, RabbitMQ vhost isolation, dynamic-database-per-fixture patterns, gRPC streaming test harnesses.

**External:**

- [Wolverine's Baked In Integration Testing Support (Jeremy Miller, March 25, 2024)](https://jeremydmiller.com/2024/03/25/wolverines-baked-in-integration-testing-support/) — the testing philosophy behind `ExecuteAndWaitAsync` and `InvokeMessageAndWaitAsync`.
- [Integration Testing an HTTP Service that Publishes a Wolverine Message (Jeremy Miller, July 9, 2023)](https://jeremydmiller.com/2023/07/09/integration-testing-an-http-service-that-publishes-a-wolverine-message/) — the canonical `TrackedHttpCall` pattern.
- [Testing Asynchronous Projections in Marten (Jeremy Miller, March 26, 2024)](https://jeremydmiller.com/2024/03/26/testing-asynchronous-projections-in-marten/) — `WaitForNonStaleProjectionDataAsync` + `FakeTimeProvider` for event timestamps.
- [Faster, More Reliable Integration Testing Against Marten Projections (Jeremy Miller, August 19, 2025)](https://jeremydmiller.com/2025/08/19/faster-more-reliable-integration-testing-against-marten-projections-or-subscriptions/) — `PauseThenCatchUpOnMartenDaemonActivity` and the Marten 8.8 / Wolverine 4.10 testing improvements.
- [Working and Testing Against Scheduled Messages with Wolverine (Jeremy Miller, September 15, 2025)](https://jeremydmiller.com/2025/09/15/working-and-testing-against-scheduled-messages-with-wolverine/) — `PlayScheduledMessagesAsync` (Wolverine 4.12+).
- [Marten Async Projection Testing Documentation](https://martendb.io/events/projections/async-daemon.html#testing-asynchronous-projections) — `WaitForNonStaleProjectionDataAsync` reference.
- [Alba Documentation](https://jasperfx.github.io/alba/) — HTTP scenario testing API.
- [Testcontainers .NET Documentation](https://dotnet.testcontainers.org/) — container builder reference for Postgres, SQL Server, Kafka, ServiceBus.
