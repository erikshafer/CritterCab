using Alba;
using Confluent.Kafka;
using CritterCab.Dispatch.AvailableDrivers;
using DotNet.Testcontainers.Images;
using Google.Protobuf;
using JasperFx.Core;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Tracking;
// Confluent.Kafka ships its own Timestamp, which collides with protobuf's well-known type.
// Aliased rather than fully qualified, matching how the Telemetry suite handles the same clash.
using ProtoTimestamp = Google.Protobuf.WellKnownTypes.Timestamp;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Xunit;

namespace CritterCab.Dispatch.Tests;

// A second, heavier Dispatch fixture that stands up a REAL Kafka broker alongside Postgres, so
// slice 5's consumer can be asserted on the wire rather than at a seam.
//
// Deliberately separate from DispatchTestFixture, mirroring the split the Telemetry project made
// for the same reason: the slice 5.1/5.2/5.3 suites have no interest in a broker, and widening the
// shared fixture would make every one of them wait on a Kafka container. The split is also more
// honest about what each host is for — the shared fixture swaps INearbyAvailableDriversSource for a
// stub to test the DECISION, this one leaves the production wiring intact to test the TRANSPORT and
// the view behind it.
public class DispatchKafkaTestFixture : IAsyncLifetime
{
    // The topic name is written out rather than imported from the service. There is no shared
    // constant by design (no shared assembly between the services), so the test asserts against the
    // literal ADR-019 name and would catch a rename in Program.cs rather than silently follow it.
    public const string Topic = "telemetry.driver-location-updated";

    // Unique container names: this project now starts TWO Postgres containers (this fixture and
    // DispatchTestFixture) and xUnit runs their collections in parallel, so a fixed name collides.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithName($"dispatch-kafka-pg-{Guid.NewGuid():N}")
        .WithImagePullPolicy(PullPolicy.Missing)
        .Build();

    // cp-kafka, NOT confluentinc/confluent-local — KafkaBuilder injects a startup script built
    // around cp-kafka's entrypoint, while confluent-local runs KRaft and expects its log directory
    // to have been formatted by kafka-storage.sh first. The combination exits 1 at startup. Same
    // pinned image and same reasoning as the Telemetry fixture.
    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.6.1")
        .WithName($"dispatch-kafka-{Guid.NewGuid():N}")
        .WithImagePullPolicy(PullPolicy.Missing)
        .Build();

    public IAlbaHost Host { get; private set; } = null!;

    public string BootstrapServers { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

        // Testcontainers reports the address with a protocol prefix (PLAINTEXT://host:port);
        // Confluent's ProducerConfig wants a bare host:port list, so strip it.
        BootstrapServers = _kafka.GetBootstrapAddress().Replace("PLAINTEXT://", string.Empty);

        // Dispatch does not AutoProvision — the producer owns the topic (ADR-018 supplier half) —
        // so nothing in the service creates it. Create it here, standing in for the Telemetry host
        // that would have done so in a real deployment. Without this the listener subscribes to a
        // topic that does not exist and simply waits.
        await CreateTopicAsync();

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting("ConnectionStrings:crittercab_dispatch", _postgres.GetConnectionString());

            // Supplying this key is what flips Program.cs from "no transport" to the real Kafka
            // listener — the same guarded branch Aspire drives in local dev. Nothing in this
            // fixture registers a handler or a listener itself; the point is to exercise the
            // production wiring end to end.
            builder.UseSetting("ConnectionStrings:kafka", BootstrapServers);
        });

        await WarmUpListenerAsync();
    }

    // Produces throwaway records until one is observably handled, then clears it.
    //
    // This exists because the listener declares BeginAtLatest() — it starts at the TAIL on a cold
    // start, so anything produced before the consumer group has finished joining is legitimately
    // missed. In production that window is invisible: Telemetry's heartbeat republishes every
    // driver within heartbeatIntervalSeconds, which is exactly why Latest is the right cold-start
    // policy there. A test has no heartbeat, so it produces once and waits forever on a record the
    // broker never delivered.
    //
    // Retrying rather than sleeping a fixed interval: group join time varies with broker startup,
    // and a sleep long enough to be safe on a slow machine wastes that time on every run. This
    // returns as soon as the round trip demonstrably works, which is the actual condition every
    // test depends on.
    private async Task WarmUpListenerAsync()
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        var warmUpDriverId = Guid.CreateVersion7();

        while (DateTimeOffset.UtcNow < deadline)
        {
            var message = new CritterCab.Telemetry.V1.DriverLocationUpdated
            {
                DriverId = warmUpDriverId.ToString(),
                Lat = 0d,
                Lon = 0d,
                H3Cell = "8f754e64992d6d8",
                H3Resolution = 15,
                ServerReceivedAt = ProtoTimestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
                ThrottlePolicyVersion = 1L
            };

            try
            {
                await Host
                    .TrackActivity()
                    .Timeout(10.Seconds())
                    .WaitForMessageToBeReceivedAt<CritterCab.Telemetry.V1.DriverLocationUpdated>(Host)
                    .ExecuteAndWaitAsync(_ => ProduceAsync(message));

                // The round trip works. Remove the warm-up driver so no test sees it.
                await ResetDriversAsync();
                return;
            }
            catch (TimeoutException)
            {
                // Group not joined yet. Produce again — under BeginAtLatest the previous record is
                // already behind the tail and will never arrive, so retrying is the only option.
            }
        }

        throw new TimeoutException(
            "Dispatch's Kafka listener never consumed a warm-up record. The consumer group did not "
            + "join within 60s, or the listener is not configured.");
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _kafka.DisposeAsync().AsTask());
    }

    // Produces a raw binary-protobuf record exactly as Telemetry's publisher would, standing in for
    // the Telemetry service without running it.
    //
    // Note what is written by hand: the partition key (driverId, per W006 §6.3 R7) and the
    // content-type header. The `message-type` header is deliberately NOT written — the listener
    // declares .DefaultIncomingMessage<DriverLocationUpdated>(), so this also proves that hardening
    // works and that the consumer would survive a producer that dropped the header.
    public async Task ProduceAsync(CritterCab.Telemetry.V1.DriverLocationUpdated message)
    {
        using var producer = new ProducerBuilder<string, byte[]>(new ProducerConfig
        {
            BootstrapServers = BootstrapServers
        }).Build();

        await producer.ProduceAsync(Topic, new Message<string, byte[]>
        {
            Key = message.DriverId,
            Value = message.ToByteArray(),
            Headers = [new Header("content-type", "binary/protobuf"u8.ToArray())]
        });

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    public async Task<AvailableDriver?> LoadDriverAsync(Guid driverId)
    {
        await using var session = Host.Services.GetRequiredService<IDocumentStore>().QuerySession();
        return await session.LoadAsync<AvailableDriver>(driverId);
    }

    public async Task ResetDriversAsync()
    {
        var store = Host.Services.GetRequiredService<IDocumentStore>();
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AvailableDriver));
    }

    private async Task CreateTopicAsync()
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = BootstrapServers
        }).Build();

        await admin.CreateTopicsAsync([
            new Confluent.Kafka.Admin.TopicSpecification
            {
                Name = Topic,
                NumPartitions = 1,
                ReplicationFactor = 1
            }
        ]);
    }
}

[CollectionDefinition("DispatchKafka")]
public class DispatchKafkaCollection : ICollectionFixture<DispatchKafkaTestFixture>;
