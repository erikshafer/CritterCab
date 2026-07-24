using Alba;
using CritterCab.Telemetry.LastKnownPosition;
using CritterCab.Telemetry.TelemetryPolicy;
using Marten;
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

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting("ConnectionStrings:crittercab_telemetry", _postgres.GetConnectionString());

            // Drop the slice-4 eviction timer from the test host. The BackgroundService is the
            // deliberately untested half of slice 4 — all its logic lives in
            // EvictStalePositionsHandler — and leaving it ticking would let a background sweep
            // race the eviction tests' own explicit invocations.
            builder.ConfigureServices(services =>
            {
                var timer = services.FirstOrDefault(
                    d => d.ImplementationType == typeof(LastKnownPositionEvictionService));

                if (timer is not null)
                    services.Remove(timer);
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
}

[CollectionDefinition("Telemetry")]
public class TelemetryCollection : ICollectionFixture<TelemetryTestFixture>;
