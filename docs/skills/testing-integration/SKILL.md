---
name: testing-integration
description: "Integration testing for CritterCab services — the per-service Alba+Testcontainers TestFixture pattern, ExecuteAndWaitAsync, tracked-session configuration (Timeout, IncludeExternalTransports, AlsoTrack, DoNotAssertOnExceptionsDetected), event-sourcing race conditions, async projection waiting (WaitForNonStaleProjectionDataAsync, WaitForConditionAsync, PauseThenCatchUpOnMartenDaemonActivity), HTTP scenarios via Alba, scheduled message testing (PlayScheduledMessagesAsync), Testcontainers for Postgres and Kafka, CI image pre-pull, IInitialData seeding, inline per-test reset, and parallelization strategy. Use when authoring any test that needs the Wolverine pipeline, real Marten, an HTTP scenario, or real broker infrastructure."
cluster: testing
tags: [testing, integration, alba, testcontainers, wolverine-tracking, async-projections, race-conditions, scheduled-messages, postgres, kafka, ci]
---

# Testing Integration

Integration tests in Cab boot the real service host, register real Marten against a real database in Docker, and exercise full request flows through Wolverine's pipeline. They cost more than unit tests but earn it: they catch handler-discovery bugs, projection-shape bugs, race conditions, and routing mistakes that no amount of mocking can surface.

This skill picks up where `testing-fundamentals` left off. If a test only exercises pure handlers, validators, or aggregate `Apply` methods — that's fundamentals territory. If a test needs the Wolverine pipeline, real Marten, the async daemon, an HTTP scenario, or a real broker — you're in the right place.

The core pattern is **per-service `TestFixture` + Alba composition over `Program.cs` + Testcontainers for storage and brokers**. The fixture boots the same `Program.cs` the service runs in production, with connection strings supplied through `UseSetting` and surgical service replacements in `ConfigureTestServices`. Tests never construct a parallel DI container; they always exercise the real one.

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
- Multi-host or multi-tenant fixture orchestration, gRPC streaming test harnesses, RabbitMQ vhost isolation — `testing-advanced` (archived).
- Aspire-orchestrated local dev composition — `aspire` (Phase 2).
- Running Cab CLI commands in tests — `cli-jasperfx` (Phase 2).

---

## The integration test mental model

Three things distinguish a good Cab integration test from a flaky one.

**Compose against `Program.cs`, not a parallel container.** Per Jeremy Miller's "use the actual application bootstrapping" guidance, the fixture builds the real service host via `AlbaHost.For<Program>(...)`. The container's connection string goes in through `builder.UseSetting("ConnectionStrings:<name>", ...)` under the same key Aspire injects, so `Program.cs`'s own guarded `GetConnectionString(...)` branch registers Marten (and, when the `kafka` key is supplied, the Kafka transport) exactly as it does in local dev. Service replacements and removals go in `ConfigureTestServices`, which runs after the entry point's own registrations — the only ordering in which removing or replacing them works.

**Real infrastructure via Testcontainers.** The repo pins `Testcontainers.PostgreSql` and `Testcontainers.Kafka` (4.13.0). Tests run against a real Postgres container, not an in-memory fake, and the Kafka fixtures against a real broker. The cost is a few seconds per fixture cold-start; the gain is catching schema drift, projection bugs, SQL generation issues, and wire-format mistakes before production.

**Wait for work to complete; never `Task.Delay`.** Wolverine commits transactions asynchronously after handlers return. Marten's async daemon catches up after `SaveChangesAsync`. Both produce the same failure mode: an HTTP POST returns 200, the test queries the result, and the data isn't there yet because the transaction or projection hasn't committed. The `ExecuteAndWaitAsync` and `WaitForNonStaleProjectionDataAsync` APIs exist precisely for this — `Task.Delay` is never the right answer.

---

## The per-service TestFixture pattern

Every Cab service has one paired test project. It holds one default `<Service>TestFixture` (Postgres only) and, when the service has a transport worth asserting on the wire, a second, heavier `<Service>KafkaTestFixture` (Postgres + Kafka). The split is deliberate: suites that have no interest in a broker should not wait on a Kafka container, and the two hosts answer different questions — the default fixture swaps a seam for a recorder or stub to test the *decision*; the Kafka fixture leaves the production wiring intact to test the *transport*. The shipped set is `TelemetryTestFixture`, `TelemetryKafkaTestFixture`, `DispatchTestFixture`, and `DispatchKafkaTestFixture`, each at the root of its test project.

```csharp
// tests/CritterCab.Telemetry.Tests/TelemetryTestFixture.cs (abridged)
using Alba;
using DotNet.Testcontainers.Images;          // PullPolicy
using Marten;
using Microsoft.AspNetCore.TestHost;         // ConfigureTestServices
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace CritterCab.Telemetry.Tests;

public class TelemetryTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithName($"telemetry-test-{Guid.NewGuid():N}")
        .WithImagePullPolicy(PullPolicy.Missing)
        .Build();

    public IAlbaHost Host { get; private set; } = null!;

    public RecordingDriverLocationPublisher Publisher { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Host = await AlbaHost.For<Program>(builder =>
        {
            // Same key Aspire injects — Program.cs's own guarded branch registers Marten.
            builder.UseSetting("ConnectionStrings:crittercab_telemetry", _postgres.GetConnectionString());

            builder.ConfigureTestServices(services =>
            {
                // Remove a hosted service whose background work would race the tests.
                var timer = services.FirstOrDefault(
                    d => d.ImplementationType == typeof(LastKnownPositionEvictionService));
                if (timer is not null)
                    services.Remove(timer);

                // Replace a seam with a recorder the tests can assert against.
                services.AddSingleton<IDriverLocationPublisher>(Publisher);
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // Per-test reset helpers — see § Test class lifecycle.
    public async Task ResetToSeedAsync() { /* DeleteAllEventDataAsync + re-run the IInitialData seed */ }
    public async Task ResetPositionsAsync() { /* DeleteDocumentsByTypeAsync(typeof(...)) */ }
}
```

### Why every line is there

- `new PostgreSqlBuilder("postgres:18-alpine")` — on Testcontainers 4.13.0 the image is a **constructor argument**; the parameterless builders are deprecated. Pinning the image avoids surprise upgrades, and the literal is what CI's pre-pull guard reads (§ "CI pre-pull").
- `.WithName($"telemetry-test-{Guid.NewGuid():N}")` — a test project that starts two Postgres containers (default fixture + Kafka fixture) runs their collections in parallel, so a fixed name collides. Name a container with a `Guid.NewGuid():N` suffix, or leave it unnamed and let Testcontainers generate one (`DispatchTestFixture` does); never give it a fixed name.
- `.WithImagePullPolicy(PullPolicy.Missing)` — use the local image when present (§ "`PullPolicy.Missing`").
- `builder.UseSetting("ConnectionStrings:...", ...)` — feeds the container into the same guarded `GetConnectionString` branch `Program.cs` uses under Aspire, so the test host's Marten registration is the production one. The Kafka fixtures do the same with `ConnectionStrings:kafka`, which is what flips `Program.cs` from "no transport" to the real Kafka wiring.
- `ConfigureTestServices` — runs after the entry point's registrations, so `services.Remove(...)` and replacement registrations take effect. Use it whenever the fixture removes or replaces something `Program.cs` registered. (`DispatchTestFixture` uses `ConfigureServices` because it only adds forwarding singletons whose registrations win by last-registration semantics.)
- Removing a hosted service — Telemetry's eviction timer is the deliberately untested half of its slice; left running, a background sweep would race the tests' own explicit invocations.

The shipped fixtures do not call `RunWolverineInSoloMode()`, `DisableAllExternalWolverineTransports()`, `MartenDaemonModeIsSolo()`, or set `JasperFxEnvironment.AutoStartHost`. Transports are already off unless the fixture supplies the `kafka` connection string, and every projection in the repo is inline or live, so there is no async daemon to pin. Reach for those calls if a service ever configures a transport unconditionally or registers an async projection.

### Collection definitions

Each fixture has exactly one collection, named by a bare string literal:

```csharp
// Bottom of tests/CritterCab.Telemetry.Tests/TelemetryTestFixture.cs
[CollectionDefinition("Telemetry")]
public class TelemetryCollection : ICollectionFixture<TelemetryTestFixture>;
```

| Collection | Fixture | Defined at |
|---|---|---|
| `"Dispatch"` | `DispatchTestFixture` | `tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs` |
| `"DispatchKafka"` | `DispatchKafkaTestFixture` | `tests/CritterCab.Dispatch.Tests/DispatchKafkaTestFixture.cs` |
| `"Telemetry"` | `TelemetryTestFixture` | `tests/CritterCab.Telemetry.Tests/TelemetryTestFixture.cs` |
| `"TelemetryKafka"` | `TelemetryKafkaTestFixture` | `tests/CritterCab.Telemetry.Tests/TelemetryKafkaTestFixture.cs` |

Test classes opt in with `[Collection("Telemetry")]` and take the fixture through their constructor. There is no `const Name`, no `DisableParallelization`, and the collection definition lives in the fixture's file. The fixture is constructed once per collection and disposed when the collection finishes. How the collections run relative to each other is § "Parallelization strategy".

### Test class lifecycle

Test classes do not implement `IAsyncLifetime` — only fixtures do. Per-test reset is **inline**: the first statement of each `[Fact]` is the fixture's reset call (or a private arrange helper whose first act is that call, as in `ReportLocationsTests`).

```csharp
[Collection("Telemetry")]
public class TelemetryPolicyConfiguredTests
{
    private readonly TelemetryTestFixture _fixture;

    public TelemetryPolicyConfiguredTests(TelemetryTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task reconfigure_full_replaces_and_advances_the_version()
    {
        await _fixture.ResetToSeedAsync();   // first statement, every test

        // ...
    }
}
```

Resetting per test rather than per class means no test inherits another's state, whichever order xUnit runs them in. The reset helpers live on the fixture and are scoped to what the slice actually writes:

| Fixture helper | What it does | Used by |
|---|---|---|
| `ResetToSeedAsync()` | `store.Advanced.Clean.DeleteAllEventDataAsync()`, then re-runs the service's `IInitialData` seeder (`TelemetryPolicyBootstrap.Populate`) to restore the bootstrap stream. | Telemetry tests that touch the policy stream. |
| `ResetPositionsAsync()` | `store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(LastKnownPositionDocument))` — plain documents survive an event-data wipe. | Telemetry tests that touch `LastKnownPosition`. |
| `ResetDriversAsync()` | `DeleteDocumentsByTypeAsync(typeof(AvailableDriver))`. | `"DispatchKafka"` tests. |
| *(none)* | The `"Dispatch"` collection does no data reset; every test creates its own stream with `Guid.CreateVersion7()` ids, so tests never read each other's data. Tests that depend on the fixture's swappable stubs (`PricingClient`, `NearbyDriversSource`) assign them in their arrange step, and classes that swap them restore the default in `IDisposable.Dispose()` (`CandidatesSelectedTests`, `FareQuotedFailurePathTests`). | `"Dispatch"` tests. |

Targeted deletes are preferred over `CleanAllMartenDataAsync()` because they say what the test depends on and leave seeded data alone. If a service ever registers async projections, its reset must pause the daemon first (`ResetAllMartenDataAsync()`), or the daemon keeps projecting stale events mid-cleanup.

**Alternative, when tests must share state.** A class whose tests deliberately build on shared setup can implement `IAsyncLifetime` and reset once in `InitializeAsync` — never in `DisposeAsync`, since xUnit does not guarantee class order and cleaning on exit does not protect the next class. Nothing in the repo needs this today; the Cab default is the inline per-test reset above.

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

By default, tracked sessions ignore messages routed to external transports (Kafka, ASB, RabbitMQ-style). Cab's default fixtures run with no external transport configured at all (they never supply the `kafka` connection string), and a fixture that disables transports with `DisableAllExternalWolverineTransports()` gets the same exclusion — the tracked-session default leaves their `Sent` records out. Enable explicitly when asserting on integration messages destined for external transports:

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

Async projections run on the projection daemon after `SaveChangesAsync` returns — they are NOT updated inline (per `marten-async-daemon`, archived). No shipped projection is async — every one is inline or live — so this section applies once one is added. Tests that append events and immediately query projected documents will see empty results unless they wait for the daemon to catch up.

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

The repo pins two Testcontainers libraries, `Testcontainers.PostgreSql` and `Testcontainers.Kafka`, both at 4.13.0. Every builder takes its image as a constructor argument.

### Postgres (Marten services — the canonical case)

Shown in the `TelemetryTestFixture` example above: `new PostgreSqlBuilder("postgres:18-alpine")`, uniquely named, `PullPolicy.Missing`.

### Kafka fixtures

```csharp
// tests/CritterCab.Telemetry.Tests/TelemetryKafkaTestFixture.cs (abridged)
private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.6.1")
    .WithName($"telemetry-kafka-{Guid.NewGuid():N}")
    .WithImagePullPolicy(PullPolicy.Missing)
    .Build();

public async Task InitializeAsync()
{
    await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

    // Testcontainers reports PLAINTEXT://host:port; Confluent's configs want bare host:port.
    BootstrapServers = _kafka.GetBootstrapAddress().Replace("PLAINTEXT://", string.Empty);

    Host = await AlbaHost.For<Program>(builder =>
    {
        builder.UseSetting("ConnectionStrings:crittercab_telemetry", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:kafka", BootstrapServers);   // flips Program.cs to real Kafka
        builder.ConfigureTestServices(services => { /* remove the eviction timer */ });
    });
}
```

The Kafka fixtures exercise the production transport wiring end to end — no transport is registered in the fixture itself. The shipped material that makes them reliable:

- **cp-kafka, not confluent-local.** `KafkaBuilder` injects a startup script built around cp-kafka's entrypoint; confluent-local runs KRaft and expects a formatted log directory, and the combination exits at startup.
- **Strip the `PLAINTEXT://` prefix** from `GetBootstrapAddress()` before handing it to `ConnectionStrings:kafka` or a Confluent `ProducerConfig`/`ConsumerConfig` (`TelemetryKafkaTestFixture.cs`, `DispatchKafkaTestFixture.cs`).
- **Pre-create the topic on the consumer side.** Dispatch does not `AutoProvision()` — the producer owns the topic — so `DispatchKafkaTestFixture` creates `telemetry.driver-location-updated` through the Confluent `AdminClient` before booting the host, standing in for the Telemetry host that would have provisioned it. Without it the listener subscribes to a topic that does not exist and waits.
- **Warm up a `BeginAtLatest()` listener before producing.** Dispatch's listener starts at the tail on a cold start, so a record produced before the consumer group finishes joining is legitimately missed. `DispatchKafkaTestFixture.WarmUpListenerAsync()` produces throwaway records inside a tracked session (`WaitForMessageToBeReceivedAt<DriverLocationUpdated>(Host)` with a 10s timeout), retrying until one is observably handled (60s deadline), then deletes the warm-up driver. Retry, don't sleep: group-join time varies.
- **Unique verifier consumer group per run.** A test that reads the topic directly (`DriverLocationPublishedTests`) builds its own Confluent consumer with `GroupId = "slice3-verifier-" + Guid.NewGuid().ToString("N")` and `AutoOffsetReset.Earliest`, so a re-run never resumes a committed offset and finds nothing. Subscribe before producing.
- **Remove hosted services that would race the tests** in `ConfigureTestServices` (`services.Remove(...)` on the descriptor whose `ImplementationType` matches), as the default fixture does.

Tests that only need to assert "the handler decided to publish" stay on the default fixture and assert against the recorder seam (`RecordingDriverLocationPublisher`), not the broker.

### SQL Server and the Azure Service Bus emulator

Not used in the repo: no Polecat service and no ASB transport exists, so there is no SQL Server or Service Bus fixture and neither Testcontainers package is pinned. When one enters, follow the same shape — image as constructor argument, unique name, `PullPolicy.Missing`, `UseSetting` for the connection string, and the image added to CI's pre-pull list. The ASB emulator additionally requires `WithAcceptLicenseAgreement(true)`.

### Parallel container startup

A fixture with more than one container starts them together:

```csharp
await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());
```

Both Kafka fixtures do this, and dispose the same way (`Task.WhenAll(_postgres.DisposeAsync().AsTask(), _kafka.DisposeAsync().AsTask())`).

### `PullPolicy.Missing`

Every shipped fixture container except `DispatchTestFixture`'s sets `WithImagePullPolicy(PullPolicy.Missing)` — use the cached image when present. `PullPolicy` lives in `DotNet.Testcontainers.Images`:

```csharp
using DotNet.Testcontainers.Images;

private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
    .WithName($"dispatch-kafka-pg-{Guid.NewGuid():N}")
    .WithImagePullPolicy(PullPolicy.Missing)
    .Build();
```

### CI pre-pull

`.github/workflows/dotnet.yml` pre-pulls every fixture image **serially, before `dotnet test`**. The two test assemblies run in parallel, each with its own Testcontainers session (a Ryuk reaper plus its fixtures' containers); pulling all of them at once from Docker Hub timed out mid-pull and surfaced as a misleading `DockerApiException` inside `ResourceReaper.GetAndStartNewAsync` in whichever suite lost the race. After the pre-pull step the images are local and `PullPolicy.Missing` makes every fixture pull a no-op.

Two rules follow:

1. **A new fixture image goes into the `docker pull` list in the same PR.** A guard step (`Verify pre-pull list covers fixture images`) greps every `*Fixture.cs` under `tests/` for image literals passed to a `*Builder("...")` constructor or to `.WithImage("...")`, and fails the build if any is missing from the list. Keep image literals in that form so the guard can see them.
2. **Re-read the Ryuk tag on every Testcontainers bump.** The list also pulls `testcontainers/ryuk:0.14.0`, which is Testcontainers' own internal pin for 4.13.0, not a choice the repo makes — the guard does not check it because no fixture names it. If the package moves and the line does not, CI pre-pulls an image nothing uses and the reaper goes back to racing for its own.

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

// In the service's Program.cs, on the AddMarten chain:
builder.Services.AddMarten(opts => { /* ... */ })
    .UseLightweightSessions()
    .InitializeWith<CanonicalRiders>();
```

In the shipped code the seeder is registered by the service itself, not the fixture — Telemetry's `Program.cs` chains `.InitializeWith<TelemetryPolicyBootstrap>()` onto `AddMarten(...)`, so a fresh test container already carries the seed when the host starts, exactly as a deployment does.

Seed data does not survive a wipe unless the reset re-runs `Populate`. The shipped clean-and-reseed helper:

```csharp
// tests/CritterCab.Telemetry.Tests/TelemetryTestFixture.cs
public async Task ResetToSeedAsync()
{
    var store = Host.Services.GetRequiredService<IDocumentStore>();
    await store.Advanced.Clean.DeleteAllEventDataAsync();
    await new TelemetryPolicyBootstrap().Populate(store, CancellationToken.None);
}
```

Tests that depend on the seed call it as the first statement of each `[Fact]` (§ "Test class lifecycle"). Write the seeder idempotently (`TelemetryPolicyBootstrap` checks `FetchStreamStateAsync` and returns if the stream exists) so host start and test reset can both run it.

When seed data must survive every cleanup operation across the full test suite — rare — register a custom `IDocumentStore.Advanced.Clean.IgnoredDocumentTypes` policy. No Cab service needs this today.

---

## Parallelization strategy

Cab runs on xUnit's defaults — no `CollectionBehavior` attribute, no `xunit.runner.json`, no `DisableParallelization` on any collection. What that means for the shipped suites:

| Scope | Behavior |
|---|---|
| Collections within one test assembly | Run **in parallel** (`"Telemetry"` alongside `"TelemetryKafka"`, `"Dispatch"` alongside `"DispatchKafka"`). |
| Test classes within one collection | Run **serially**, sharing the collection's fixture. |
| Test methods within one class | Run serially (xUnit v2 never parallelizes within a class). |
| The two test assemblies | Run **in parallel** under `dotnet test CritterCab.slnx`. |

Parallel collections are safe because of **per-fixture container isolation**: each fixture owns its own Postgres (and, for the Kafka fixtures, its own Kafka broker) and its own Alba host. Two collections never share a database, a topic, or a host, so nothing one does is visible to the other. Within a collection, safety comes from the inline per-test reset or, in `"Dispatch"`, from every test using fresh `Guid.CreateVersion7()` ids.

The rules that keep this safe:

- **A new fixture owns its containers.** Never point a second fixture at another fixture's container. Name containers uniquely (or leave them unnamed) so parallel fixtures do not collide.
- **A new test class joins exactly one existing collection**, or brings its own fixture and collection. A class with no `[Collection]` gets an implicit per-class collection and no fixture.
- **Shared mutable fixture state is reset at the top of the test that uses it** — data via the reset helpers, swappable stubs by reassignment.
- **Generate ids with `Guid.CreateVersion7()`** in every test; reserve well-known ids for seed data.

### Alternative: serial collections, when tests must share state

If two fixtures ever had to share a resource (one database, one topic), parallel collections would interfere. The xUnit levers are `[CollectionDefinition("...", DisableParallelization = true)]` on a collection, or `[assembly: CollectionBehavior(DisableTestParallelization = true)]` for the whole assembly. The repo uses neither; prefer giving the fixture its own container.

### Tracked-session timeouts under parallel load

Parallel collections and assemblies contend for CPU and Docker. The default 5-second tracked-session timeout can expire under load. Bump per-test where the flow needs it:

```csharp
var session = await Host.TrackActivity()
    .Timeout(30.Seconds())  // Generous for loaded CI machines
    .InvokeMessageAndWaitAsync(command);
```

---

## Common pitfalls

- **`Task.Delay` to "fix" race conditions.** Never the right answer. The right APIs are `InvokeMessageAndWaitAsync`, `WaitForNonStaleProjectionDataAsync`, `WaitForConditionAsync`, and `PlayScheduledMessagesAsync`.
- **Resetting anywhere but the top of the test.** Cleaning in a class's `DisposeAsync` doesn't protect the next class (xUnit doesn't guarantee class order), and a once-per-class reset lets one test's writes leak into the next. Call the fixture's reset helper as the first statement of each `[Fact]`.
- **Forgetting `ResetAllMartenDataAsync` for services with async projections.** `CleanAllMartenDataAsync` doesn't pause the daemon; the daemon may keep processing events from the prior test mid-cleanup. Use `ResetAllMartenDataAsync` whenever async projections are registered.
- **Asserting on `tracked.Sent` for cascaded events with no routing rule.** Those land in `tracked.NoRoutes`. Check both buckets when in doubt; use `dotnet run -- describe-routing` to verify routing.
- **Registering Marten (or a transport) in the fixture instead of feeding `Program.cs` its connection string.** A parallel registration tests the fixture's wiring, not the service's. Supply `builder.UseSetting("ConnectionStrings:<name>", ...)` under the key Aspire injects and let `Program.cs`'s own guarded branch register it.
- **Removing or replacing a registration in `ConfigureServices`.** Use `ConfigureTestServices`, which runs after the entry point's registrations; removal only works from there.
- **A fixed Testcontainer name.** Collections run in parallel and a project may start two Postgres containers; a fixed name collides. Suffix with `Guid.NewGuid():N` or leave the container unnamed.
- **Old Testcontainers builder forms.** On 4.13.0 the image is a constructor argument (`new PostgreSqlBuilder("postgres:18-alpine")`), and the pull-policy method is `WithImagePullPolicy(PullPolicy.Missing)` with `PullPolicy` from `DotNet.Testcontainers.Images`.
- **Adding a fixture image without adding it to CI's pre-pull list.** The `Verify pre-pull list covers fixture images` step fails the build. Add the `docker pull` line in the same PR, and re-read the Ryuk tag whenever Testcontainers is bumped.
- **Producing to a `BeginAtLatest()` listener before its consumer group has joined.** The record is legitimately skipped and the test waits forever. Warm the listener up first (`DispatchKafkaTestFixture.WarmUpListenerAsync`).
- **Treating the default 5-second timeout as universal.** It's too tight for flows that traverse async daemon catch-up, scheduled-message playback, or multi-host AlsoTrack scenarios. Bump per-test where it matters.
- **Hard-coded GUIDs that collide under parallelism.** Use `Guid.CreateVersion7()` per test invocation; reserve well-known IDs for `IInitialData` reference data only.
- **Using `WaitForNonStaleProjectionDataAsync` when the test only cares about one specific projection.** The blanket wait blocks on every running projection. `WaitForConditionAsync` is more surgical when other projections are slow or unrelated.

---

## See also

**Upstream** — generic Wolverine + Marten integration testing fundamentals this skill builds on. ai-skills (license required, install via `npx skills add`):

- `wolverine-testing-integration` (primary) — baseline integration testing patterns: `IAlbaHost.For<Program>`, `ExecuteAndWaitAsync`/`InvokeMessageAndWaitAsync`, tracked-session API, `RunWolverineInSoloMode`, `DisableAllExternalWolverineTransports`. Cab's skill applies these with project-specific framing (per-service TestFixture pattern with Testcontainer-per-fixture, `UseSetting` connection strings feeding `Program.cs`'s own guarded registration, `ConfigureTestServices` for removals and replacements, inline per-test reset, CI image pre-pull).
- `wolverine-testing-integration-marten` — Marten-specific integration testing: `CleanAllMartenDataAsync` vs `ResetAllMartenDataAsync` (when async projections are registered), `WaitForNonStaleProjectionDataAsync` for projection catch-up, `IInitialData` seeding patterns, the race condition between Wolverine's transactional middleware and the HTTP response.
- `wolverine-testing-with-testcontainers` — Testcontainers-driven integration testing: Postgres/SQL Server/Kafka/ServiceBus container builders, image pinning, unique container naming for parallel test runs, the lifecycle integration with `IAsyncLifetime`.
- `wolverine-testing-with-aspire` — Aspire-orchestrated integration testing: composing tests against an Aspire AppHost rather than per-service Testcontainers, when each strategy is appropriate.
- `wolverine-testing-test-parallelization` — xUnit parallelization strategies: `[CollectionDefinition(DisableParallelization = true)]` for sequential-within-collection, unique-ID discipline for cross-test parallelism, project-level `CollectionBehavior` baseline, tracked-session timeout adjustments under parallel load. Cab runs on xUnit's defaults (parallel collections, per-fixture container isolation) and keeps the serial levers only as a labelled alternative.

**Prerequisites** — Cab-internal skills to load first:

- `testing-fundamentals` — committed test stack, xUnit lifecycle, unit testing pure handlers, Shouldly conventions, `FakeTimeProvider`. Read first.
- `service-bootstrap` — `AddMarten`, `IntegrateWithWolverine`, `AddAsyncDaemon`, `DurabilityMode`; the Program.cs surface fixtures compose against.
- `marten-async-daemon` (archived) — daemon modes (Solo, HotCold, Wolverine-managed); error handling; rebuild patterns. Relevant once a service registers an async projection and its fixture needs `MartenDaemonModeIsSolo()`.
- `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers` — handler shapes being exercised end-to-end.
- `marten-projections` — projection lifecycles; what async projections need waiting on.

**Sibling skills:**

- `marten-querying` — read-side consequences of eventual consistency; `WaitForNonStaleProjectionDataAsync` rationale.
- `marten-wolverine-aggregates` — `[WriteAggregate]`/`[Aggregate]` handler shapes and the `EmptyResponse` / 204 status convention.
- `dynamic-consistency-boundary` (archived) — DCB write-path tests; `[BoundaryModel]` setup uses the same fixture pattern.

**Downstream:**

- `aspire` (Phase 2) — local dev wiring; integration test fixtures may compose against the same Aspire-orchestrated `Program.cs`.
- `cli-jasperfx` (Phase 2) — `describe-routing`, `codegen-preview` for diagnosing failing tests; same CLI surface that test fixtures verify by working at all.
- `wolverine-grpc-handlers` (Phase 3) — gRPC-streaming integration tests need extensions to this skill's HTTP-scenario patterns.
- `wolverine-kafka` — the Kafka transport wiring the Kafka fixtures exercise end to end.
- `wolverine-azure-service-bus` (archived) — ASB emulator tests, when ASB enters.
- `wolverine-sagas` (archived) — saga timeout tests via `PlayScheduledMessagesAsync`.
- `testing-advanced` (archived) — multi-host scenarios, RabbitMQ vhost isolation, dynamic-database-per-fixture patterns, gRPC streaming test harnesses.

**External:**

- [Wolverine's Baked In Integration Testing Support (Jeremy Miller, March 25, 2024)](https://jeremydmiller.com/2024/03/25/wolverines-baked-in-integration-testing-support/) — the testing philosophy behind `ExecuteAndWaitAsync` and `InvokeMessageAndWaitAsync`.
- [Integration Testing an HTTP Service that Publishes a Wolverine Message (Jeremy Miller, July 9, 2023)](https://jeremydmiller.com/2023/07/09/integration-testing-an-http-service-that-publishes-a-wolverine-message/) — the canonical `TrackedHttpCall` pattern.
- [Testing Asynchronous Projections in Marten (Jeremy Miller, March 26, 2024)](https://jeremydmiller.com/2024/03/26/testing-asynchronous-projections-in-marten/) — `WaitForNonStaleProjectionDataAsync` + `FakeTimeProvider` for event timestamps.
- [Faster, More Reliable Integration Testing Against Marten Projections (Jeremy Miller, August 19, 2025)](https://jeremydmiller.com/2025/08/19/faster-more-reliable-integration-testing-against-marten-projections-or-subscriptions/) — `PauseThenCatchUpOnMartenDaemonActivity` and the Marten 8.8 / Wolverine 4.10 testing improvements.
- [Working and Testing Against Scheduled Messages with Wolverine (Jeremy Miller, September 15, 2025)](https://jeremydmiller.com/2025/09/15/working-and-testing-against-scheduled-messages-with-wolverine/) — `PlayScheduledMessagesAsync` (Wolverine 4.12+).
- [Marten Async Projection Testing Documentation](https://martendb.io/events/projections/async-daemon.html#testing-asynchronous-projections) — `WaitForNonStaleProjectionDataAsync` reference.
- [Alba Documentation](https://jasperfx.github.io/alba/) — HTTP scenario testing API.
- [Testcontainers .NET Documentation](https://dotnet.testcontainers.org/) — container builder reference for Postgres and Kafka.
