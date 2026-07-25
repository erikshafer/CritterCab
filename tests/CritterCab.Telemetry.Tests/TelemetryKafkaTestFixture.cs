using Alba;
using CritterCab.Telemetry.LastKnownPosition;
using DotNet.Testcontainers.Images;
using Grpc.Net.Client;
using Marten;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Xunit;
using LastKnownPositionDocument = global::CritterCab.Telemetry.LastKnownPosition.LastKnownPosition;

namespace CritterCab.Telemetry.Tests;

// A second, heavier fixture that stands up a REAL Kafka broker alongside Postgres, so slice 3's
// publish can be asserted on the wire rather than at a seam.
//
// Deliberately separate from TelemetryTestFixture rather than folded into it. The slice-1, -2 and
// -4 suites have no interest in a broker, and widening the shared fixture would make every one of
// them wait on a Kafka container. The split is also the more honest arrangement: the shared
// fixture swaps the publisher seam for a recorder to test the TRIGGER, while this one leaves the
// production wiring intact to test the TRANSPORT. Two different questions, two different hosts.
public class TelemetryKafkaTestFixture : IAsyncLifetime
{
    // Unique container names: this project now starts TWO Postgres containers (this fixture and
    // TelemetryTestFixture), and xUnit runs their collections in parallel, so a fixed name would
    // collide. Same reason the Kafka container below is named.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithName($"telemetry-kafka-pg-{Guid.NewGuid():N}")
        .WithImagePullPolicy(PullPolicy.Missing)
        .Build();

    // Image pinned explicitly, like the Postgres container above — Testcontainers has deprecated
    // its parameterless builders, and an unpinned broker is a poor thing to depend on for a
    // wire-format test.
    //
    // It must be a cp-kafka image, NOT confluentinc/confluent-local. KafkaBuilder injects its own
    // startup script built around cp-kafka's entrypoint; confluent-local runs KRaft and expects
    // its log directory to have been formatted by kafka-storage.sh first, so the two combined
    // produce a broker that reads zookeeper.properties, finds no meta.properties, and exits 1.
    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.6.1")
        .WithName($"telemetry-kafka-{Guid.NewGuid():N}")
        .WithImagePullPolicy(PullPolicy.Missing)
        .Build();

    public IAlbaHost Host { get; private set; } = null!;

    public string BootstrapServers { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

        // Testcontainers reports the address with a protocol prefix (PLAINTEXT://host:port);
        // Confluent's ProducerConfig/ConsumerConfig want a bare host:port list, so strip it.
        BootstrapServers = _kafka.GetBootstrapAddress().Replace("PLAINTEXT://", string.Empty);

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting("ConnectionStrings:crittercab_telemetry", _postgres.GetConnectionString());

            // Supplying this key is what flips Program.cs from the logging fallback to the real
            // KafkaDriverLocationPublisher — the same guarded branch Aspire drives in local dev.
            // Nothing in this fixture registers the publisher itself; the point is to exercise
            // the production wiring end to end, AutoProvision included.
            builder.UseSetting("ConnectionStrings:kafka", BootstrapServers);

            builder.ConfigureTestServices(services =>
            {
                // Same reasoning as the shared fixture: the eviction timer is the untested half of
                // slice 4, and a background sweep racing these tests would only add flake.
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
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _kafka.DisposeAsync().AsTask());
    }

    public GrpcChannel CreateGrpcChannel() =>
        GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = Host.GetTestServer().CreateHandler()
        });

    public async Task ResetPositionsAsync()
    {
        var store = Host.Services.GetRequiredService<IDocumentStore>();
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(LastKnownPositionDocument));
    }
}

[CollectionDefinition("TelemetryKafka")]
public class TelemetryKafkaCollection : ICollectionFixture<TelemetryKafkaTestFixture>;
