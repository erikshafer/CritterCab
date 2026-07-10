using Alba;
using CritterCab.Telemetry.TelemetryPolicy;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

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

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting("ConnectionStrings:crittercab_telemetry", _postgres.GetConnectionString());
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
}

[CollectionDefinition("Telemetry")]
public class TelemetryCollection : ICollectionFixture<TelemetryTestFixture>;
