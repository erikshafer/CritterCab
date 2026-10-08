using CritterCab.Telemetry.ReportLocations;
using CritterCab.Telemetry.V1;
using Grpc.Core;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using LastKnownPositionDocument = global::CritterCab.Telemetry.LastKnownPosition.LastKnownPosition;

namespace CritterCab.Telemetry.Tests.ReportLocations;

// W006 §6.3's publish-first/no-outbox consistency note, pinned as behavior.
//
// §6.3 chose publish-before-store by comparing two failure modes: a failed store after a good
// publish costs a duplicate (absorbed by the consumer's dedup), while a failed publish after a
// good store costs a MISS that Dispatch cannot detect. That argument only holds if a failed
// publish actually prevents the store — which is precisely why slice 3 configures the Kafka
// endpoint SendInline with a synchronous retry block. Under Wolverine's defaults the publish is
// buffered and its failure swallowed, the store would land anyway, and the system would sit in
// the branch §6.3 rejected while looking correct.
//
// No broker here on purpose. What is under test is the HANDLER's ordering contract, so a
// publisher that throws is a truer and faster instrument than a broker that has to be broken.
// The other half of the claim — that Wolverine's inline sender surfaces a broker rejection as an
// exception at all — rests on source verification and the endpoint configuration, not on this
// test; see the retrospective.
[Collection("Telemetry")]
public class DriverLocationPublishOrderingTests
{
    private const double LoopLat = 41.8827d, LoopLon = -87.6233d;

    private readonly TelemetryTestFixture _fixture;

    public DriverLocationPublishOrderingTests(TelemetryTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task a_failed_publish_leaves_the_baseline_unwritten_so_the_next_ping_republishes()
    {
        await _fixture.ResetToSeedAsync();
        await _fixture.ResetPositionsAsync();
        _fixture.Publisher.Clear();

        var driverId = Guid.CreateVersion7();

        // Given a driver with no baseline, whose next publish will be rejected by the broker
        _fixture.Publisher.FailNextPublish = true;

        // When they report a position
        var call = () => StreamAsync(driverId, PingAt(LoopLat, LoopLon));

        // Then the window fails rather than reporting a success it cannot vouch for. A silent
        // recovery here would be worse than the error: the driver's client would believe its
        // position was delivered.
        await Should.ThrowAsync<RpcException>(call);

        // And — the load-bearing assertion — no baseline was written. Publish-first means the
        // store is downstream of a publish that never happened.
        var stored = await LoadAsync(driverId);
        stored.ShouldBeNull();

        // So the driver is still, correctly, unknown to the system: their next ping finds no
        // baseline and publishes immediately (§6.4 "Return"), which is how a dropped publish
        // self-heals without any retry state being kept anywhere.
        var ack = await StreamAsync(driverId, PingAt(LoopLat, LoopLon));

        ack.AcceptedCount.ShouldBe(1);
        _fixture.Publisher.Published.ShouldHaveSingleItem()
            .DriverId.ShouldBe(driverId.ToString());

        // And only now does the baseline appear.
        var recovered = await LoadAsync(driverId);
        recovered.ShouldNotBeNull();
        recovered.H3Cell.ShouldBe(H3CellIndexer.TryComputeCell(LoopLat, LoopLon, 9));
    }

    private Task<LocationIngestAck> StreamAsync(Guid driverId, params LocationPing[] pings) =>
        ReportLocationsClient.StreamAsync(_fixture.CreateGrpcChannel, driverId, pings);

    private async Task<LastKnownPositionDocument?> LoadAsync(Guid driverId)
    {
        var store = _fixture.Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.QuerySession();
        return await session.LoadAsync<LastKnownPositionDocument>(driverId);
    }

    private static LocationPing PingAt(double lat, double lon) =>
        ReportLocationsClient.PingAt(lat, lon);
}
