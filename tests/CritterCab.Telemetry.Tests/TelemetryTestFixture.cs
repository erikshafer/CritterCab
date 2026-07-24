using System.Collections.Concurrent;
using Alba;
using CritterCab.Telemetry.LastKnownPosition;
using CritterCab.Telemetry.ReportLocations;
using CritterCab.Telemetry.TelemetryPolicy;
using CritterCab.Telemetry.V1;
using Grpc.Net.Client;
using Marten;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wolverine;
using Xunit;
using LastKnownPositionDocument = global::CritterCab.Telemetry.LastKnownPosition.LastKnownPosition;

namespace CritterCab.Telemetry.Tests;

// Postgres-backed Alba host for slice-1 (config-as-events) integration tests. Unlike
// DispatchTestFixture there are no stub dependencies to forward — slice 1 is pure
// Marten + HTTP, so the fixture only supplies the connection string. The bootstrap
// seed (IInitialData) runs on host start, so a fresh container already carries the
// default TelemetryPolicy before any test acts.
public class TelemetryTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .Build();

    public IAlbaHost Host { get; private set; } = null!;

    // Exposed so a test can build a second host with the production wiring intact — the fixture
    // host deliberately strips the eviction timer, so nothing else would cover its registration.
    public string ConnectionString => _postgres.GetConnectionString();

    // Stands in for the slice-3 Kafka publish so slice-2 tests can assert on the publish trigger.
    public RecordingDriverLocationPublisher Publisher { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting("ConnectionStrings:crittercab_telemetry", _postgres.GetConnectionString());

            // ConfigureTestServices, not ConfigureServices: this hook runs AFTER the entry point's
            // own registrations, which is the only ordering where removing and replacing them
            // works.
            builder.ConfigureTestServices(services =>
            {
                // Drop the slice-4 eviction timer. The BackgroundService is the deliberately
                // untested half of slice 4 — all its logic lives in EvictStalePositionsHandler —
                // and leaving it ticking would let a background sweep race the eviction tests'
                // own explicit invocations.
                var timer = services.FirstOrDefault(
                    d => d.ImplementationType == typeof(LastKnownPositionEvictionService));

                if (timer is not null)
                    services.Remove(timer);

                // Swap the PR B logging stub for a recorder so slice-2 tests can assert what was
                // published. This is the same seam PR C swaps for the real Kafka producer, which
                // is the point of it being a seam.
                services.AddSingleton<IDriverLocationPublisher>(Publisher);
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // The policy is a singleton, so slice-1 tests share one stream and must isolate: wipe the
    // event store and re-run the bootstrap seed to return to the known v1 default before each
    // test acts. Re-invokes the same IInitialData the host runs on startup.
    public async Task ResetToSeedAsync()
    {
        var store = Host.Services.GetRequiredService<IDocumentStore>();
        await store.Advanced.Clean.DeleteAllEventDataAsync();
        await new TelemetryPolicyBootstrap().Populate(store, CancellationToken.None);
    }

    // LastKnownPosition is a plain document, so it survives ResetToSeedAsync (which only clears
    // event data). Slice-4 tests wipe it separately to start from a known-empty store.
    public async Task ResetPositionsAsync()
    {
        var store = Host.Services.GetRequiredService<IDocumentStore>();
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(LastKnownPositionDocument));
    }

    // Sends a message through Wolverine exactly the way production does. IMessageBus is registered
    // scoped, so it must be resolved from a scope rather than the root provider — the same reason
    // LastKnownPositionEvictionService creates a scope per tick. InvokeAsync is inline and awaited,
    // so there is no async commit to wait out and no tracked session needed.
    public async Task InvokeAsync(object message)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.InvokeAsync(message);
    }

    // Gate 11: a GrpcChannel over Alba's host rather than the raw WebApplication+UseTestServer
    // recipe Wolverine's own client-streaming fixture uses. Alba wraps WebApplicationFactory,
    // which runs on TestServer underneath, so GetTestServer().CreateHandler() reaches the same
    // in-memory transport — meaning CritterCab keeps its Alba-first default and gains gRPC
    // without a second, parallel host recipe.
    public GrpcChannel CreateGrpcChannel() =>
        GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = Host.GetTestServer().CreateHandler()
        });
}

// Records what slice 2's publish trigger fired, standing in for slice 3's Kafka producer.
public sealed class RecordingDriverLocationPublisher : IDriverLocationPublisher
{
    private readonly ConcurrentQueue<DriverLocationUpdated> _published = new();

    public IReadOnlyList<DriverLocationUpdated> Published => [.. _published];

    public void Clear() => _published.Clear();

    public Task PublishAsync(DriverLocationUpdated update, CancellationToken ct)
    {
        _published.Enqueue(update);
        return Task.CompletedTask;
    }
}

[CollectionDefinition("Telemetry")]
public class TelemetryCollection : ICollectionFixture<TelemetryTestFixture>;
