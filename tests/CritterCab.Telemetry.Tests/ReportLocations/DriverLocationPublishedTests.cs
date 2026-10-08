using Confluent.Kafka;
using CritterCab.Telemetry.ReportLocations;
using CritterCab.Telemetry.V1;
using Shouldly;
using Xunit;
// Confluent.Kafka has its own Metadata and Timestamp types that collide with the gRPC and
// protobuf ones this test also needs. Aliased rather than fully qualified so the test body still
// reads like the slice-2 tests it mirrors.
using GrpcMetadata = Grpc.Core.Metadata;
using ProtoTimestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace CritterCab.Telemetry.Tests.ReportLocations;

// W006 §6.3's Publish GWT, asserted against a real broker.
//
// This is the one test in the suite that reads the Kafka record directly instead of trusting a
// seam, and that is the whole point of it: the slice-2 tests prove the publish DECISION, this one
// proves the publish actually reaches a topic, under the key and in the format Dispatch will
// depend on. Everything it asserts is a contract with another bounded context, so it is written
// against the literal topic name and the raw bytes rather than against any constant the producer
// could rename in lockstep with it.
//
// §6.3's second GWT (Dedup) is deliberately absent — it asserts CONSUMER behavior against
// at-least-once redelivery, and there is no consumer until PR D.
[Collection("TelemetryKafka")]
public class DriverLocationPublishedTests
{
    private const string Topic = "telemetry.driver-location-updated";
    private const double LoopLat = 41.8827d, LoopLon = -87.6233d;

    private readonly TelemetryKafkaTestFixture _fixture;

    public DriverLocationPublishedTests(TelemetryKafkaTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task a_published_position_lands_on_the_topic_keyed_by_driver_id()
    {
        await _fixture.ResetPositionsAsync();

        var driverId = Guid.CreateVersion7();

        // Subscribe BEFORE publishing. The consumer group is unique per run and starts at the
        // earliest offset, so the record is readable either way — but subscribing first keeps the
        // test from depending on retention if it is ever run against a shared broker.
        using var consumer = CreateConsumer();
        consumer.Subscribe(Topic);

        // Given a driver with no baseline, when a single ping arrives
        // Then §6.4's "Return" rule makes it publish immediately — no throttling to wait out.
        var ack = await StreamAsync(driverId, PingAt(LoopLat, LoopLon));
        ack.AcceptedCount.ShouldBe(1);

        // AutoProvision creates the topic at host start, and the first consume after a fresh
        // subscribe pays for a group join, so allow generous headroom. The send itself is inline
        // and already broker-acked by the time the ack above returned.
        var result = consumer.Consume(TimeSpan.FromSeconds(30));
        consumer.Close();

        result.ShouldNotBeNull("No record arrived on " + Topic + " within the timeout.");

        // The partition key is what makes per-driver ordering a transport property (R7). Asserting
        // it here is not incidental: without a key Wolverine falls back to the envelope GUID, the
        // records still arrive, and only the ORDERING silently breaks — so this assertion is the
        // only thing standing between a working system and a subtly wrong one.
        result.Message.Key.ShouldBe(driverId.ToString());

        // And the value is binary protobuf that the generated parser round-trips — the proof that
        // the .proto is governing the wire and not just the C# type (ADR-009). A JSON-serialized
        // payload would fail to parse here.
        var published = DriverLocationUpdated.Parser.ParseFrom(result.Message.Value);

        published.DriverId.ShouldBe(driverId.ToString());
        published.H3Cell.ShouldBe(H3CellIndexer.TryComputeCell(LoopLat, LoopLon, 9));
        published.H3Resolution.ShouldBe(9);
        published.ThrottlePolicyVersion.ShouldBe(1L);
        published.ServerReceivedAt.ToDateTimeOffset()
            .ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));

        // Optional fields the ping did not carry stay absent rather than defaulting to zero —
        // proto3 explicit presence, preserved across the wire.
        published.HasSpeed.ShouldBeFalse();
        published.HasHeading.ShouldBeFalse();
    }

    private IConsumer<string, byte[]> CreateConsumer() =>
        new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = _fixture.BootstrapServers,
            // Unique per run so a re-run never resumes a committed offset and finds nothing.
            GroupId = "slice3-verifier-" + Guid.NewGuid().ToString("N"),
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();

    private Task<LocationIngestAck> StreamAsync(Guid driverId, params LocationPing[] pings) =>
        ReportLocationsClient.StreamAsync(_fixture.CreateGrpcChannel, driverId, pings);

    private static LocationPing PingAt(double lat, double lon) =>
        ReportLocationsClient.PingAt(lat, lon);
}
