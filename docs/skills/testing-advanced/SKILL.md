---
name: testing-advanced
description: "Advanced integration-test patterns in CritterCab — the techniques that go beyond the per-service TestFixture pattern in testing-integration. Covers multi-host scenarios via Wolverine's TrackActivity().AlsoTrack(host2, host3) for cross-BC saga and event-flow tests; gRPC streaming test harnesses (in-process clients via WebApplicationFactory<Program> for unary, server-streaming, bidirectional, and the client-streaming hand-written-stub case from wolverine-grpc-bidirectional-handlers); dynamic database-per-fixture naming for both Marten (PostgreSQL schema-per-fixture) and Polecat (SQL Server database-per-fixture or schema-per-fixture); RabbitMQ vhost isolation as the canonical parallel-fixture-isolation technique that generalizes to other transport namespaces; Testcontainers patterns for Kafka (Testcontainers.Kafka) and ASB (Testcontainers.ServiceBus) integration tests including IncludeExternalTransports() to make Wolverine tracking observe transport activity; test-token factories that issue OpenIddict-shaped JWTs for identity-acl boundary testing; advanced saga timeout testing with PlayScheduledMessagesAsync, multi-saga coordination, and cross-host saga scenarios; span and metric verification using the OpenTelemetry in-memory exporter and MeterListener for trace-tree assertions and counter/histogram observation; polyglot boundary tests that run cab-go alongside .NET test hosts via Testcontainers to exercise cross-language gRPC, Kafka, OTel propagation, and identity flows; failure-injection patterns including DoNotAssertOnExceptionsDetected() for resilience testing and middleware-based fault injection. Pulls on every Phase 4 skill plus testing-fundamentals and testing-integration; not a tutorial but a compendium of patterns for the harder cases."
cluster: testing
tags: [testing, multi-host, grpc-streaming, web-application-factory, dynamic-database, vhost-isolation, testcontainers, kafka, azure-service-bus, test-tokens, saga-timeout, scheduled-messages, otel-verification, in-memory-exporter, polyglot-tests, failure-injection]
---

# Advanced Integration Testing

`testing-fundamentals` covers unit testing pure handlers, validators, and Shouldly conventions. `testing-integration` covers the per-service `TestFixture` pattern, the canonical race condition every event-sourced test hits, tracked sessions, scheduled messages, async projections, Alba HTTP scenarios, and Testcontainers basics. **This skill covers what's left**: cross-service scenarios, streaming gRPC harnesses, parallel-safe database isolation, transport namespace isolation, identity-flow testing, OTel signal verification, polyglot boundaries, and failure injection.

The single most useful framing: **`testing-advanced` is a compendium of techniques, not a tutorial.** Each pattern here addresses a specific class of test that the per-service `TestFixture` shape from `testing-integration` can't cleanly express. You reach for these patterns deliberately when a test crosses a boundary the simpler shape doesn't span — multiple Wolverine hosts, multiple databases-per-fixture for parallel isolation, multiple transports including external brokers via Testcontainers, multiple languages via the polyglot boundary, or multiple OTel signals as the assertion target.

The Wolverine 5.32+ `Wolverine.Tracking` namespace is the foundation. `host.TrackActivity().AlsoTrack(otherHost)` is the first-class multi-host primitive — a single tracked session observes message activity across multiple Wolverine applications running in the same process. This is the linchpin pattern: most "advanced" tests in Cab are some combination of multi-host tracking plus a transport-specific or auth-specific concern.

This skill assumes `testing-fundamentals` for the test stack (xUnit 2.9.3, Shouldly 4.3.0, Alba 8.5.2, Testcontainers 4.11.0) and `testing-integration` for the `TestFixture` shape, the `IInitialData` seeding pattern, and the standard tracked-session API. It assumes — but does not re-derive — every Phase 3 and Phase 4 skill the patterns reference.

**Prerequisite packages.** The patterns below depend on packages already committed in `Directory.Packages.props` (Testcontainers.PostgreSql / .MsSql / .Kafka / .ServiceBus 4.11.0, Alba 8.5.2). The OpenTelemetry verification patterns add `OpenTelemetry.Exporter.InMemory` to what `observability-tracing` and `observability-metrics` flag — surface that prerequisite when those tests land. The test-token factory patterns depend on the OpenIddict packages `identity-acl` flagged.

---

## When to apply this skill

Use this skill when:

- A test needs to span two or more Wolverine hosts in one process — saga that crosses BCs, end-to-end flow through Trips → Pricing → Payments.
- The test target is gRPC streaming (server-streaming, bidirectional, or the hand-written client-streaming pattern) — `WebApplicationFactory<Program>` plus an in-process gRPC channel is the canonical harness.
- Parallel test execution is hitting database collisions — schema-per-fixture for Marten, database-or-schema-per-fixture for Polecat.
- The test depends on a real Kafka or ASB broker rather than the in-memory transport — Testcontainers patterns plus `IncludeExternalTransports()` make tracking work.
- The handler-under-test enforces an auth boundary — test-token factories that issue OpenIddict-shaped JWTs let you exercise the boundary without standing up the full identity service.
- The assertion target is a span tree or a metric counter — the OpenTelemetry in-memory exporter and `MeterListener` make these tractable.
- The flow crosses the polyglot boundary — `cab-go` running in a Testcontainer alongside .NET test hosts.
- The test is exercising failure paths — `DoNotAssertOnExceptionsDetected()` plus middleware-based fault injection.

Do NOT use this skill for:

- Unit tests of pure handlers, validators, or domain logic — `testing-fundamentals`.
- Single-service integration tests that fit the standard `TestFixture` shape — `testing-integration`.
- Saga test fundamentals — `testing-integration` § Testing scheduled messages plus `wolverine-sagas` § Testing.
- HTTP scenario assembly via Alba — `testing-integration` § HTTP scenarios via Alba.
- Aspire orchestration of test fixtures — Aspire is for local dev, not for `xUnit`-driven tests; `testing-integration` § Testcontainers patterns is canonical.

---

## Multi-host scenarios

The most common reason to reach for `testing-advanced` is a flow that legitimately spans services. Cab examples:

- **`TripCompletionSaga` across Trips, Pricing, and Payments** — three hosts, the saga in Trips orchestrates calls to Pricing for fare calculation and Payments for capture.
- **`RiderOnboardingSaga` across Identity and Rider Profile** — two hosts, identity issues a token, profile creates the rider record.
- **Driver position propagation: Telemetry → Dispatch** — Telemetry produces a Kafka message, Dispatch consumes it and updates its read model.

The Wolverine.Tracking primitive (test class injects three `IClassFixture<{BC}Fixture>` parameters — `_trips`, `_pricing`, `_payments` — in the standard xUnit shape):

```csharp
[Fact]
public async Task completing_trip_charges_rider_and_credits_driver()
{
    var tripId = Guid.NewGuid();
    await _trips.SeedActiveTripAsync(tripId, riderId: ..., driverId: ...);

    // Single tracked session observes activity across all three hosts
    var tracked = await _trips.Host.TrackActivity()
        .AlsoTrack(_pricing.Host, _payments.Host)
        .Timeout(30.Seconds())
        .WaitForMessageToBeReceivedAt<PaymentCaptured>(_payments.Host)
        .InvokeMessageAndWaitAsync(new CompleteTrip(tripId));

    tracked.MessageSucceeded.SingleEnvelope<FareCalculated>().ShouldNotBeNull();
    tracked.MessageSucceeded.SingleEnvelope<PaymentCaptured>().ShouldNotBeNull();

    var payment = await _payments.Session.LoadAsync<Payment>(...);
    payment.Status.ShouldBe(PaymentStatus.Captured);
}
```

Three pieces matter:

- **`AlsoTrack(host2, host3)`** is the entry point. Returns the same `TrackedSessionConfiguration` so chaining continues.
- **`WaitForMessageToBeReceivedAt<T>(host)`** is the per-host wait condition. Without it, the tracked session uses its default completion heuristic (no message activity for a quiet period), which can complete prematurely if the inter-host hop takes longer than the heuristic's quiet window.
- **`Timeout(...)`** should be generous on multi-host scenarios. Default is 5 seconds; cross-host flows with real transports need 15–30 seconds. Erring high here is cheap — the timeout fires only on hung tests.

### Sequential stages with `AddStage`

When the test needs to do "send X, wait for it; then send Y, wait for it" rather than firing everything at once, use nested stages:

```csharp
var tracked = await _trips.Host.TrackActivity()
    .AlsoTrack(_pricing.Host, _payments.Host)
    .AddStage(async (runtime, context, ct) =>
    {
        // First stage already tracked completes; this stage runs and is also tracked
        await context.SendAsync(new RefundTrip(tripId));
    })
    .InvokeMessageAndWaitAsync(new CompleteTrip(tripId));
```

The first invocation (`InvokeMessageAndWaitAsync`) runs and is tracked. After it completes, the registered stage runs against an `IMessageContext` and is also tracked. Use `AddStage` when the test logically has "act, then act again" semantics rather than "act once, observe everything."

### Across-host fixture composition

Multi-host fixtures are slightly more delicate than single-host. The convention Cab follows:

- Each BC gets its own `IClassFixture<{BC}Fixture>`. `xUnit`'s class-fixture scoping lets multiple test classes share each fixture's setup cost.
- Fixtures DO NOT share state. Each owns its own database (or schema, see § Dynamic database per fixture below) and its own Wolverine host.
- Cross-BC tests inject all relevant fixtures as constructor parameters. xUnit handles the dependency graph; the test class instances are short-lived per-test.
- Transport wiring: when two BCs talk via a real transport (Kafka, ASB), the fixtures must agree on the broker. In single-process xUnit runs, the simplest pattern is a shared `ICollectionFixture<BrokerFixture>` that owns one Testcontainer-managed broker and exposes the connection details to all BC fixtures.

---

## gRPC streaming test harnesses

`testing-integration` covers HTTP scenarios via Alba but defers gRPC-specific patterns. The canonical Cab harness uses `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint>` to host the service in-process, then opens a gRPC channel against the in-process server.

### Unary and server-streaming

The Cab harness extends `WebApplicationFactory<Program>`, overrides `ConfigureWebHost` to swap any production registrations that need test substitutes (e.g., binding Wolverine to in-memory transports for tests), and exposes a `CreateClient()` method that wraps `base.CreateClient()` in a `GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions { HttpClient = httpClient })` to produce a strongly-typed gRPC client.

Unary tests look like any C# gRPC client test — `await client.RequestRideAsync(...)` and assert on the response. Server-streaming tests iterate the response stream:

```csharp
[Fact]
public async Task subscribe_dispatch_stream_emits_events()
{
    var client = _fixture.CreateClient();
    using var call = client.SubscribeDispatchEvents(new SubscribeRequest { ... });

    var received = new List<DispatchEvent>();
    await foreach (var evt in call.ResponseStream.ReadAllAsync()
        .WithCancellation(new CancellationTokenSource(5.Seconds()).Token))
    {
        received.Add(evt);
        if (received.Count == 3) break;
    }

    received.Count.ShouldBe(3);
}
```

The cancellation-token wrap is important — `ReadAllAsync` blocks indefinitely if the server keeps the stream open, so wrap it with a timeout to fail fast on broken tests.

### Bidirectional streaming

Bidi tests assert on both inbound and outbound message flow. The `WriteAsync` / `ReadAllAsync` shape is symmetric: send N requests via `call.RequestStream.WriteAsync(...)`, complete the request stream, and read responses via `await foreach (var update in call.ResponseStream.ReadAllAsync())`. Per `wolverine-grpc-bidirectional-handlers` § Critical bidi semantics, Wolverine invokes the handler **once per inbound request**, not once per stream — the test should pin that invariant: three inbound `SubscribeRequest` messages → at least three handler invocations → potentially more outbound messages depending on each handler's response.

### Client-streaming (hand-written stub case)

Per `wolverine-grpc-bidirectional-handlers` § Client-streaming workaround, client-streaming RPCs in Wolverine require a hand-written stub. Test the stub the same way — `WebApplicationFactory<Program>` works regardless. The difference is the assertion target: instead of asserting on `ResponseStream`, you assert on the cumulative effect of all inbound requests on whatever side-channel the stub uses (typically `IMessageBus.PublishAsync`).

```csharp
[Fact]
public async Task push_telemetry_publishes_one_message_per_inbound()
{
    var tracked = await _telemetry.Host.TrackActivity()
        .Timeout(10.Seconds())
        .ExecuteAndWaitAsync(async ctx =>
        {
            var client = _fixture.CreateClient();
            using var call = client.PushTelemetry();

            for (int i = 0; i < 100; i++)
            {
                await call.RequestStream.WriteAsync(new GpsPing { ... });
            }
            await call.RequestStream.CompleteAsync();
            await call;     // wait for server to ack the stream end
        });

    tracked.MessageSucceeded.MessagesOf<DriverPositionRecorded>().Count().ShouldBe(100);
}
```

Combining `TrackActivity` with `WebApplicationFactory` requires care: the tracked session must be bound to the host the gRPC server runs in. Resolve the host from the fixture's `Services` and call `TrackActivity()` on it; the call won't observe gRPC framing but will observe the message-bus side effects the hand-written stub publishes.

### Concrete-stub fallback

`wolverine-grpc-handlers` § Concrete stub fallback documents the path where a hand-written gRPC stub doesn't go through Wolverine at all (just calls `IMessageBus` directly). The test for that path is a normal in-process gRPC test plus tracked sessions on the message-bus side channel — same harness as client-streaming above.

---

## Dynamic database per fixture

Parallel test execution requires that fixtures don't share database state. The Marten and Polecat conventions:

### Marten — schema per fixture

Marten supports multi-schema isolation cheaply. Each fixture creates its own schema with a UUID suffix:

```csharp
public class TripsFixture : IAsyncLifetime
{
    private readonly string _schemaName = $"trips_test_{Guid.NewGuid():N}";

    public IHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var hostBuilder = Host.CreateApplicationBuilder();
        // ... standard service-bootstrap config

        hostBuilder.Services.AddMarten(opts =>
        {
            opts.Connection(SharedTestcontainers.PostgresConnectionString);
            opts.DatabaseSchemaName = _schemaName;     // <-- per-fixture
            opts.AutoCreateSchemaObjects = AutoCreate.All;
        });

        Host = hostBuilder.Build();
        await Host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        // Optional: drop the schema at teardown to avoid leaking
        await using var session = Host.Services.GetRequiredService<IDocumentStore>()
            .LightweightSession();
        await session.Connection!.ExecuteAsync($"DROP SCHEMA \"{_schemaName}\" CASCADE");

        await Host.StopAsync();
        await Host.DisposeAsync();
    }
}
```

The `Guid.NewGuid():N` suffix produces collision-free schema names. Several thousand parallel fixtures can coexist on one PostgreSQL instance without name conflicts.

### Polecat — schema per fixture (or database per fixture)

The Polecat path is structurally identical to the Marten one above: a `Guid.NewGuid():N` schema-name suffix, `opts.DatabaseSchemaName = _schemaName`, `opts.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate`, plus `.IntegrateWithWolverine()`. The shared SQL Server Testcontainer (below) provides the connection string. Per-fixture schema is the default; reach for full database-per-fixture only when the test exercises database-level features (filegroups, recovery models).

### Shared Testcontainer for the engine

Both patterns assume one PostgreSQL Testcontainer or one SQL Server Testcontainer is shared across all fixtures via `ICollectionFixture<>`. The container starts once per test run and is torn down at the end:

```csharp
public class SharedTestcontainersFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine").Build();

    public MsSqlContainer SqlServer { get; } = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();
        await SqlServer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await SqlServer.DisposeAsync();
    }
}

[CollectionDefinition("Containers")]
public class ContainersCollection : ICollectionFixture<SharedTestcontainersFixture> { }
```

Per-fixture schemas (Marten and Polecat) use the connection strings from this collection-fixture-scoped container.

---

## RabbitMQ vhost isolation

The vhost-per-fixture pattern is RabbitMQ-specific but the technique generalizes — any transport with a namespace concept (Kafka topics with prefixes, ASB queues with prefixes) supports the same shape: each fixture gets its own namespace so concurrent fixtures don't see each other's messages.

```csharp
public class RabbitMqFixture : IAsyncLifetime
{
    private readonly string _vhost = $"test-{Guid.NewGuid():N}";
    public RabbitMqContainer Container { get; } = new RabbitMqBuilder()
        .WithImage("rabbitmq:4-management-alpine").Build();

    public string ConnectionString => $"{Container.GetConnectionString()}/{_vhost}";

    public async Task InitializeAsync()
    {
        await Container.StartAsync();

        // Create the per-fixture vhost via the management API
        await CreateVhostAsync(_vhost);
    }

    public async Task DisposeAsync()
    {
        // Optional: delete the vhost at teardown
        await DeleteVhostAsync(_vhost);
        await Container.DisposeAsync();
    }

    private async Task CreateVhostAsync(string name) { /* HTTP PUT to /api/vhosts/{name} */ }
    private async Task DeleteVhostAsync(string name) { /* HTTP DELETE to /api/vhosts/{name} */ }
}
```

In the BC fixture's host configuration, point Wolverine at `Container.ConnectionString + "/" + _vhost` rather than the shared connection string.

The principle: any test fixture that uses a real broker should isolate its messages from concurrent fixtures by namespace. RabbitMQ vhost is the cleanest example because RabbitMQ has explicit vhost support; for Kafka use a topic-name prefix; for ASB use a queue-name prefix. The generalization is *namespace-per-fixture*; the vhost case is the canonical illustration.

---

## Testcontainers patterns for Kafka and ASB

`testing-integration` § Testcontainers patterns covers the basics. The advanced concerns:

### Tracked sessions need `IncludeExternalTransports`

By default Wolverine's `TrackActivity` does not observe activity flowing through external transports — it watches the in-process message graph. For tests that depend on Kafka or ASB delivery, opt in:

```csharp
var tracked = await _telemetry.Host.TrackActivity()
    .AlsoTrack(_dispatch.Host)
    .IncludeExternalTransports()      // <-- observe Kafka/ASB delivery
    .Timeout(30.Seconds())
    .WaitForMessageToBeReceivedAt<DriverPositionUpdated>(_dispatch.Host)
    .InvokeMessageAndWaitAsync(new GpsPingReceived(...));
```

Without this, the tracked session waits only on in-process activity and may complete before the cross-host transport delivery happens. The symptom is intermittent test failures on slower machines or under load.

### Kafka via `Testcontainers.Kafka`

```csharp
public class KafkaFixture : IAsyncLifetime
{
    public KafkaContainer Container { get; } = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.7.0").Build();

    public string BootstrapServers => Container.GetBootstrapAddress();

    public async Task InitializeAsync() => await Container.StartAsync();
    public async Task DisposeAsync() => await Container.DisposeAsync();
}
```

In the BC fixture, configure `UseKafka(Container.BootstrapServers)` per `wolverine-kafka` § Configuration. Topic names should be prefixed with the test-run identifier (or per-fixture identifier) to isolate concurrent tests on the same broker. See `cli-kafka-tooling` § `kafka-topics --list` for verification commands.

### ASB via `Testcontainers.ServiceBus`

```csharp
public class AzureServiceBusFixture : IAsyncLifetime
{
    public AzureServiceBusContainer Container { get; } = new AzureServiceBusBuilder()
        .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest").Build();

    public string ConnectionString => Container.GetConnectionString();
}
```

The official Azure Service Bus emulator container ships per-test queue/topic configurations via a JSON config file. Per `cli-azure-messaging` § Emulator configuration, the test fixture mounts the config into the container at startup. Each fixture provides its own queue/topic names with the per-fixture identifier prefix.

### Choosing in-memory vs Testcontainers

Wolverine ships in-memory transports (`UseInMemory()`) that simulate the messaging shape without external infrastructure. Use the in-memory variant when:

- The test is asserting on Wolverine handler behavior, not on transport-specific delivery semantics.
- The test runs frequently in a fast feedback loop (TDD inner loop).

Use Testcontainers when:

- The test depends on transport-specific behavior (Kafka partition assignment, ASB session correlation, dead-letter queue mechanics).
- The test exercises connection-failure or broker-restart scenarios.
- The test verifies serialization wire-compatibility.

Cab convention: in-memory transports for ~80% of integration tests, Testcontainers for the ~20% that genuinely need broker behavior.

---

## Test-token factories for `identity-acl`

Per `identity-acl`, Cab's identity surface is OpenIddict-backed and services validate JWTs at their HTTP/gRPC boundaries. For tests that exercise authenticated endpoints, you need to issue a JWT that the service accepts — without spinning up the full identity service.

The pattern: a test-time `JwtTokenFactory` that signs tokens with a key the service trusts. In `Program.cs` (under a test environment flag), the JWT validation parameters point at the test factory's signing key rather than the production OpenIddict authority.

The factory generates a fresh RSA key per instance and exposes `Issue(subject, audience, params (string type, string value)[] claims)` returning a signed JWT plus an `RsaSecurityKey` property the fixture wires into `JwtBearerOptions.TokenValidationParameters.IssuerSigningKey`. In tests, set `Authorization: Bearer {token}` on the HTTP/gRPC client before calling the authenticated endpoint.

```csharp
var token = _testTokens.Issue(
    subject: riderId.ToString(),
    audience: "trips-bc",
    ("scope", "trips:write"),
    ("tenant", "primary"));

var response = await scenario.SendAsync(client =>
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client.PostAsJsonAsync("/api/trips", new RequestRide(...));
});
```

The factory pattern parallels the demo-mode token issuance from `identity-acl` § Demo mode. Demo mode is for local dev; the test factory is for tests. Both bypass the full OpenIddict flow; both target the same JWT validation boundary. Each fixture should own its own factory and key — sharing keys couples tests and creates surprising failures when a parallel fixture rotates its key.

---

## Saga timeout testing

`testing-integration` § Testing scheduled messages covers `host.MessageBus().PublishAsync(...)` plus `tracked.PlayScheduledMessagesAsync`. The advanced cases:

### Multi-step saga timeouts

A saga that schedules a follow-up after each step needs the test to advance through each scheduled boundary. Chain `PlayScheduledMessagesAsync` calls to advance the saga one timeout at a time:

```csharp
[Fact]
public async Task dispatch_offer_timeout_escalates_after_three_failures()
{
    var tracked = await _dispatch.Host.TrackActivity()
        .Timeout(15.Seconds())
        .InvokeMessageAndWaitAsync(new RequestRide(rideId, ...));

    // First timeout fires — driver didn't respond
    var afterFirst = await tracked.PlayScheduledMessagesAsync(10.Seconds());
    (await LoadSagaAsync(rideId)).Status.ShouldBe(DispatchOfferStatus.SecondAttempt);

    // Second timeout fires
    var afterSecond = await afterFirst.PlayScheduledMessagesAsync(10.Seconds());
    (await LoadSagaAsync(rideId)).Status.ShouldBe(DispatchOfferStatus.ThirdAttempt);

    // Third timeout escalates
    var afterThird = await afterSecond.PlayScheduledMessagesAsync(10.Seconds());
    (await LoadSagaAsync(rideId)).Status.ShouldBe(DispatchOfferStatus.Escalated);
}
```

The test asserts on intermediate state without depending on real wall-clock waits.

### Cross-host saga scenarios

When the saga's timeouts dispatch messages to other hosts, multi-host tracking applies. Combine `AlsoTrack` with `PlayScheduledMessagesAsync`:

```csharp
var tracked = await _trips.Host.TrackActivity()
    .AlsoTrack(_payments.Host)
    .Timeout(30.Seconds())
    .InvokeMessageAndWaitAsync(new CompleteTrip(tripId));

// Saga schedules a payment-capture-timeout 5 minutes out
var afterTimeout = await tracked.PlayScheduledMessagesAsync(15.Seconds());

// Verify the timeout dispatched a message to Payments
afterTimeout.MessageSucceeded.MessagesOf<PaymentCaptureTimedOut>()
    .ShouldHaveSingleItem();
```

`PlayScheduledMessagesAsync` advances scheduled messages on the host the tracked session was started on. To advance scheduled messages on other hosts, call `PlayScheduledMessagesAsync` on a tracked session started against that host. (`testing-integration` § Testing scheduled messages covers single-host basics; this skill is the multi-host extension.)

---

## OpenTelemetry signal verification

`observability-tracing` and `observability-metrics` document what signals Cab services emit. Tests that assert on those signals use:

### In-memory trace exporter

Register `OpenTelemetry.Exporter.InMemory`'s `InMemoryTraceExporter` as a `SimpleActivityExportProcessor` on the tracing pipeline; it captures every `Activity` the SDK emits during the test. Assertions target the parent-child structure (cross-BC trace propagation), the tags, or the duration distribution:

```csharp
[Fact]
public async Task complete_trip_emits_payment_span_under_trips_root()
{
    await _trips.Host.TrackActivity()
        .AlsoTrack(_payments.Host)
        .InvokeMessageAndWaitAsync(new CompleteTrip(tripId));

    var activities = _trips.TraceExporter.GetExportedActivities();
    var rootActivity = activities.Single(a => a.OperationName == "CompleteTrip");
    var paymentActivity = activities.Single(a => a.OperationName.StartsWith("PaymentCaptured"));

    paymentActivity.ParentSpanId.ShouldBe(rootActivity.SpanId);
}
```

Reset between tests via the exporter's `Reset` / `Clear` method, or scope the exporter per-fixture rather than per-test-run — otherwise spans from prior tests pollute the assertion.

### Metric verification with `MeterListener`

Counters and histograms surface through `System.Diagnostics.Metrics.MeterListener`. The Wolverine Meter name is per-service-suffixed (per `observability-metrics`), so the listener filter is `instrument.Meter.Name.StartsWith("Wolverine:")` rather than exact-match:

```csharp
var increments = new ConcurrentBag<(string instrument, long value)>();
using var listener = new MeterListener
{
    InstrumentPublished = (instrument, l) =>
    {
        if (instrument.Meter.Name.StartsWith("Wolverine:"))
            l.EnableMeasurementEvents(instrument);
    }
};
listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
    increments.Add((instrument.Name, value)));
listener.Start();

await _trips.Host.InvokeMessageAndWaitAsync(new CompleteTrip(tripId));

increments.Where(x => x.instrument == "wolverine-messages-succeeded")
    .Sum(x => x.value).ShouldBeGreaterThanOrEqualTo(1);
```

For histograms (`wolverine-effective-time`), use `SetMeasurementEventCallback<double>` with the same filter shape.

---

## Polyglot boundary tests with `cab-go`

Per `polyglot-go-service`, `cab-go` is the Go-side matchmaking service Dispatch consumes. Integration tests that span the polyglot boundary need `cab-go` running alongside the .NET test hosts. The pattern: a `CabGoFixture` that runs a pre-built `cab-go:test` Docker image as a generic Testcontainer (`new ContainerBuilder().WithImage(...).WithPortBinding(50051, true).WithEnvironment(...)`), exposing the gRPC endpoint URI to consuming tests.

In the test, Dispatch's gRPC client points at `_cabGo.Endpoint`, and assertions verify the cross-language flow:

```csharp
[Fact]
public async Task dispatch_finds_nearest_drivers_via_cab_go()
{
    // Seed driver positions via Kafka (the Telemetry → cab-go path)
    await _kafka.PublishGpsPingsAsync(driverPings);
    await Task.Delay(2.Seconds());     // wait for cab-go to consume

    var client = new MatchmakerService.MatchmakerServiceClient(
        GrpcChannel.ForAddress(_cabGo.Endpoint));
    var response = await client.FindNearestDriversAsync(new FindNearestDriversRequest
    {
        Origin = new GeoPoint { Lat = 41.26, Lon = -95.94 },
        K = 3,
    });

    response.Candidates.Count.ShouldBe(3);
}
```

For trace-propagation assertions across the .NET → Go boundary, the in-memory trace exporter on the .NET side captures only the .NET-emitted spans — the Go-side spans go to whatever OTLP receiver the test fixture provides. The cleanest assertion target is the `traceparent` header propagation: assert that the .NET-side request span and the Go-side request handler share the same trace ID. A mock OTLP receiver fixture that captures Go-side OTLP exports complements the in-memory .NET exporter for full-tree assertions.

Pre-build the `cab-go:test` image in CI (or a `dotnet test` setup script) so the container starts from a local image — first-run pulls add minutes to test setup.

---

## Failure injection

Tests for resilience features (retry policies, circuit breakers, dead-letter queue routing) need controlled failures. Two patterns:

### `DoNotAssertOnExceptionsDetected`

By default, `TrackActivity()` asserts that no exceptions were thrown during the tracked window. For tests that *expect* exceptions, opt out:

```csharp
var tracked = await _trips.Host.TrackActivity()
    .DoNotAssertOnExceptionsDetected()
    .Timeout(10.Seconds())
    .InvokeMessageAndWaitAsync(new CompleteTrip(tripId));

tracked.AllExceptions().Count.ShouldBe(2);          // expected — first two retries failed
tracked.MessageSucceeded.MessagesOf<TripCompleted>().ShouldHaveSingleItem();
```

### Middleware-injected faults

For "fail the first N invocations" patterns, inject a fault-recording middleware in the fixture's host config:

```csharp
public class FailFirstN<T> : IWolverineMiddleware
{
    private int _remaining;
    public FailFirstN(int n) => _remaining = n;

    public Task InvokeAsync(IMessageContext context)
    {
        if (context.Envelope.Message is T && Interlocked.Decrement(ref _remaining) >= 0)
            throw new InvalidOperationException("Injected test failure");
        return Task.CompletedTask;
    }
}

// In the fixture's host config:
opts.Policies.AddMiddleware(typeof(FailFirstN<CompleteTrip>), new FailFirstN<CompleteTrip>(2));
```

The test asserts the retry policy correctly recovers after the injected failures.

For network-level faults (transport unavailability), Testcontainers' `PauseContainerAsync` / `UnpauseContainerAsync` (or stopping the broker container mid-test) simulates broker outages. Reach for that only when the test genuinely requires transport-level fault injection — the middleware approach covers most cases more cleanly.

---

## Common pitfalls

- **Forgetting `IncludeExternalTransports()` on Kafka/ASB tests.** `TrackActivity` ignores external transports by default. Tests that depend on cross-host delivery via real brokers complete before the delivery happens unless the option is set. The symptom is intermittent failures on slow machines or under parallel load.

- **Sharing `WebApplicationFactory<Program>` across test classes without `IClassFixture`.** Each `WebApplicationFactory` instance starts its own host. Forgetting class-fixture scoping creates one host per test method, multiplying setup cost and exhausting Testcontainer resources. Use `IClassFixture<TFixture>` or `ICollectionFixture<TFixture>` per the `testing-integration` per-service fixture pattern.

- **Using `host.TrackActivity()` against a host that hasn't started.** The tracked session resolves `IWolverineRuntime` from the host. If the host is built but not started, the runtime is partially initialized and tracking misbehaves. Always `await host.StartAsync()` in the fixture's `InitializeAsync` before any tracked-session code runs.

- **Not setting `Timeout()` on multi-host tracked sessions.** The default 5-second timeout is fine for in-process tests but tight for cross-host scenarios with real transports. Bump to 15–30 seconds for multi-host. Erring high is cheap; the timeout fires only on hung tests, not on success.

- **Asserting on `tracked.Sent.MessagesOf<T>()` when expecting `MessageSucceeded`.** `Sent` records publication; `MessageSucceeded` records handler completion. A message can be sent and then fail handling. For "the message was processed correctly" assertions, use `MessageSucceeded.MessagesOf<T>()`.

- **Schema-per-fixture without `AutoCreateSchemaObjects`.** Per-fixture schemas don't exist until something creates them. Marten's `AutoCreate.All` and Polecat's `AutoCreate.CreateOrUpdate` (per `polecat-event-sourcing`) handle this. Setting `AutoCreate.None` in a fixture is a guaranteed test failure.

- **Reusing the same JWT signing key across `TestTokenFactory` instances.** Each fixture or test class should own its own factory and key. Sharing keys couples tests and creates surprising failures when a parallel fixture rotates its key. Inject the factory; don't make it a static singleton.

- **Calling `PlayScheduledMessagesAsync` on the wrong tracked session.** The method advances scheduled messages on the host the tracked session was bound to. For multi-host scenarios, each host has its own scheduled-message store. Per-host calls are required if multiple hosts have pending timeouts.

- **In-memory OTel exporter capturing spans from previous tests.** The `InMemoryTraceExporter` (and `InMemoryMetricExporter`) accumulate across the lifetime of the SDK. Reset between tests via the exporter's `Reset` / `Clear` method, or scope the exporter per-fixture rather than per-test-run.

- **`MeterListener` not seeing instruments.** Wolverine's per-service Meter name is `Wolverine:{ServiceName}` (per `observability-metrics`). The listener's `InstrumentPublished` callback must match the prefix, not the literal `"Wolverine"`. Same care with custom Cab Meters — match `CritterCab.Trips`, not `CritterCab`.

- **`cab-go` Testcontainer pulled at test time on slow connections.** First-run image pulls add minutes to test setup. Pre-build the `cab-go:test` image in the test project's `dotnet test` setup script (or in CI's pre-test step) so the container starts from a local image.

- **Assuming hand-written client-streaming tests can use `WaitForMessageToBeReceivedAt`.** Hand-written stubs publish via `IMessageBus`, not via the gRPC framework's outbound stream. The wait condition target is the published in-process message, not the gRPC-framing-level event. Pin the assertion on `tracked.MessageSucceeded.MessagesOf<T>()` instead.

- **Using `DoNotAssertOnExceptionsDetected` to silence unexpected exceptions.** The flag is for tests that *expect* exceptions as part of the resilience scenario. Using it to make a flaky test pass masks a real bug. If a test legitimately needs the flag, it should also assert on `tracked.AllExceptions()` to verify the *expected* exceptions were the ones thrown.

- **Polluting parallel runs with shared-broker test data.** RabbitMQ vhost isolation, Kafka topic prefixes, and ASB queue prefixes all serve the same purpose: namespace-per-fixture. Skipping the namespace is fine on a serial run but produces silent test contamination under parallel execution. Default to namespace isolation; treat the shared-namespace case as the special configuration.

---

## See also

**Upstream** — ai-skills covers the foundation (Alba, integration testing, parallelization, Testcontainers) that this skill extends with advanced multi-host, streaming, polyglot, and verification patterns. ai-skills (license required, install via `npx skills add`):

- `wolverine-testing-alba` — Alba HTTP scenario testing patterns: `IAlbaHost`, `Scenario` builder, JSON request/response handling, `TrackedHttpCall` integration. Cab's testing-integration relies on these patterns; testing-advanced extends to multi-host scenarios via `TrackActivity().AlsoTrack(otherHost)`.
- `wolverine-testing-integration` — the baseline integration testing surface (covered as Skill 38's primary counterpart). Cab's testing-advanced builds on the tracked-session API documented there.
- `wolverine-testing-test-parallelization` — xUnit parallelization fundamentals. Cab's testing-advanced extends with namespace-per-fixture isolation (RabbitMQ vhost, Kafka topic prefix, ASB queue prefix; PostgreSQL schema-per-fixture and SQL Server database-per-fixture for the storage side).
- `wolverine-testing-with-testcontainers` — Testcontainers fundamentals. Cab's testing-advanced extends with cross-language Testcontainer composition (`cab-go` alongside .NET hosts) and broker-specific patterns (Kafka, ASB) including `IncludeExternalTransports()`.

Most of testing-advanced's surface is genuinely advanced and Cab-specific: multi-host `TrackActivity().AlsoTrack()` scenarios, gRPC streaming test harnesses (in-process clients via `WebApplicationFactory<Program>` for unary/server-streaming/bidirectional/client-streaming), dynamic database-per-fixture isolation, OpenTelemetry in-memory exporter verification of span trees and metric counters, polyglot boundary tests, OpenIddict-shaped JWT test-token factories, and middleware-based fault injection. These don't have direct ai-skills counterparts.

**Prerequisites** — Cab-internal skills to load first:

- `testing-fundamentals` — the test stack (xUnit 2.9.3, Shouldly, Alba, Testcontainers 4.11.0), unit testing patterns, FakeTimeProvider. Foundational.
- `testing-integration` — the per-service `TestFixture` pattern, the canonical race condition, tracked sessions basics, Alba HTTP scenarios, IInitialData, parallelization. This skill extends those patterns to the multi-host and cross-cutting cases.
- `service-bootstrap` — the production `Program.cs` shape that test fixtures parallel.

**Sibling skills:**

- `wolverine-grpc-handlers` — unary and server-streaming handler patterns; this skill's gRPC test harness section is the test side of those.
- `wolverine-grpc-bidirectional-handlers` — bidi and client-streaming patterns; this skill's harness covers both, including the hand-written-stub case.
- `wolverine-kafka` — the Kafka client side; this skill's Testcontainers Kafka section is the test-fixture side of that wiring.
- `wolverine-azure-service-bus` — the ASB client side; same relationship.
- `wolverine-sagas` — saga base class and timeout patterns; this skill's saga timeout section extends the testing patterns there.
- `distributed-saga-considerations` — multi-saga coordination, failure modes; this skill provides the test patterns those concepts need.
- `identity-acl` — the JWT validation boundary; this skill's test-token factory exercises it.
- `polecat-event-sourcing` and `polecat-document-store` — Polecat configuration patterns this skill's per-fixture schema isolation builds on.
- `polyglot-go-service` — `cab-go` setup; this skill's polyglot boundary test section is the corresponding test pattern.
- `observability-tracing` and `observability-metrics` — the signal definitions; this skill's verification patterns target those signals.

**Downstream:**

- `cli-jasperfx` — `db-apply`, `describe`, `storage counts` for fixture diagnostics during test failures.
- `cli-grpc-tooling` — `grpcurl` and `evans` for ad-hoc verification when a streaming test fails and you need to bisect.
- `cli-kafka-tooling` — Kafka topic inspection commands for verifying test message flow.
- `cli-azure-messaging` — ASB queue/topic inspection commands.

**External:**

- [Wolverine.Tracking documentation](https://wolverinefx.net/guide/testing/) — `TrackActivity`, `AlsoTrack`, `PlayScheduledMessagesAsync`, `WaitForMessageToBeReceivedAt`. Canonical reference.
- [Testcontainers .NET documentation](https://dotnet.testcontainers.org/) — module catalog, lifecycle hooks, network configuration.
- [WebApplicationFactory<TEntryPoint> documentation](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests) — in-process ASP.NET Core hosting.
- [OpenTelemetry .NET testing documentation](https://opentelemetry.io/docs/languages/dotnet/) — in-memory exporter, `MeterListener` patterns.
- [Alba documentation](https://jasperfx.github.io/alba/) — HTTP scenario assembly.
