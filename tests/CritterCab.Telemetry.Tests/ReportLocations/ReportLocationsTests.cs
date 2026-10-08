using Alba;
using CritterCab.Telemetry.ReportLocations;
using CritterCab.Telemetry.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using LastKnownPositionDocument = global::CritterCab.Telemetry.LastKnownPosition.LastKnownPosition;

namespace CritterCab.Telemetry.Tests.ReportLocations;

// W006 §6.2 GWTs, driven over a REAL gRPC client stream against the Alba host — CritterCab's
// first gRPC traffic in a test. Also covers the §6.4 "No-write" GWT, which slice 4 could not
// reach on its own because deciding not to write is this slice's trigger.
[Collection("Telemetry")]
public class ReportLocationsTests
{
    // Two Chicago points roughly 7km apart — comfortably different cells at resolution 9, whose
    // edges are ~174m. Cells are computed rather than hardcoded so the tests state the intent
    // ("a different cell") instead of reciting opaque H3 ids.
    private const double LoopLat = 41.8827d, LoopLon = -87.6233d;
    private const double WrigleyLat = 41.9484d, WrigleyLon = -87.6553d;

    private readonly TelemetryTestFixture _fixture;

    public ReportLocationsTests(TelemetryTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task a_cell_change_past_the_throttle_floor_publishes_and_stores()
    {
        // Given the seeded policy { h3: 9, heartbeat: 30s, minPublish: 5s }
        // And driver D last published from the Wrigley cell 10 seconds ago
        var driverId = await ArrangeAsync(
            baselineLat: WrigleyLat, baselineLon: WrigleyLon, lastPublishedSecondsAgo: 10);

        // When a ping arrives from the Loop cell
        var ack = await StreamAsync(driverId, PingAt(LoopLat, LoopLon));

        // Then the ping is accepted, and shouldPublish is true (cell changed AND floor elapsed)
        ack.AcceptedCount.ShouldBe(1);
        ack.ThrottlePolicyVersion.ShouldBe(1L);

        // And the publish fires with the published-language payload Dispatch will consume
        var published = _fixture.Publisher.Published.ShouldHaveSingleItem();
        published.DriverId.ShouldBe(driverId.ToString());
        published.H3Cell.ShouldBe(CellAt(LoopLat, LoopLon));
        published.H3Resolution.ShouldBe(9);

        // And the document is upserted to the new cell (publish-first, then store)
        var stored = await LoadAsync(driverId);
        stored.ShouldNotBeNull();
        stored.H3Cell.ShouldBe(CellAt(LoopLat, LoopLon));
    }

    [Fact]
    public async Task a_cell_change_inside_the_throttle_floor_is_accepted_but_absorbed()
    {
        // Given D last published from the Wrigley cell only 2 seconds ago (floor is 5s)
        var driverId = await ArrangeAsync(
            baselineLat: WrigleyLat, baselineLon: WrigleyLon, lastPublishedSecondsAgo: 2);
        var baselineBefore = await LoadAsync(driverId);

        // When a ping arrives from the Loop cell
        var ack = await StreamAsync(driverId, PingAt(LoopLat, LoopLon));

        // Then the ping still counts as accepted — throttling is not rejection
        ack.AcceptedCount.ShouldBe(1);

        // But nothing is published: the floor gates cell-change publishes, which is what stops a
        // driver hovering on a cell boundary from flooding Kafka
        _fixture.Publisher.Published.ShouldBeEmpty();

        // And — the §6.4 "No-write" GWT — the document is untouched, because it is written only
        // on publish, never per ping
        var baselineAfter = await LoadAsync(driverId);
        baselineAfter.ShouldNotBeNull();
        baselineAfter.H3Cell.ShouldBe(baselineBefore!.H3Cell);
        baselineAfter.ServerReceivedAt.ShouldBe(baselineBefore.ServerReceivedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task a_due_heartbeat_publishes_even_without_a_cell_change()
    {
        // Given D last published from the Loop cell 31 seconds ago (heartbeat is 30s)
        var driverId = await ArrangeAsync(
            baselineLat: LoopLat, baselineLon: LoopLon, lastPublishedSecondsAgo: 31);

        // When a ping arrives from the SAME cell
        var ack = await StreamAsync(driverId, PingAt(LoopLat, LoopLon));

        // Then it publishes anyway — heartbeatDue is independent of cell change, and is what makes
        // a dropped publish self-heal within one heartbeat
        ack.AcceptedCount.ShouldBe(1);
        _fixture.Publisher.Published.ShouldHaveSingleItem()
            .H3Cell.ShouldBe(CellAt(LoopLat, LoopLon));

        // And the document's stamp advances, restarting both the heartbeat and the floor
        var stored = await LoadAsync(driverId);
        stored.ShouldNotBeNull();
        stored.ServerReceivedAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public async Task the_window_ack_counts_only_pings_that_passed_validation()
    {
        // Given a driver with no baseline at all
        var driverId = await ArrangeAsync(baselineLat: null, baselineLon: null, lastPublishedSecondsAgo: 0);

        // When five pings are streamed, of which two are invalid
        var ack = await StreamAsync(
            driverId,
            PingAt(LoopLat, LoopLon),
            PingAt(999d, LoopLon),                            // latitude out of range
            PingAt(LoopLat, LoopLon),
            PingAt(LoopLat, LoopLon, accuracyMeters: 5000d),  // beyond the accuracy threshold
            PingAt(LoopLat, LoopLon));

        // Then the ack reports only the three that passed. Invalid pings are silently dropped, not
        // errored — there is no per-ping error frame in v1, and the stream is not torn down.
        ack.AcceptedCount.ShouldBe(3);

        // And exactly one publish happened: the first ping had no baseline so it published
        // immediately (§6.4 "Return"), and the rest fell inside the throttle floor
        _fixture.Publisher.Published.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task a_stream_with_no_driver_identity_is_rejected()
    {
        await ResetAsync();

        // R5: driverId comes from the principal and is never carried in the payload — which is why
        // the proto has no driver_id field. A stream that presents no identity cannot be attributed.
        var exception = await Should.ThrowAsync<RpcException>(() =>
            ReportLocationsClient.StreamWithoutIdentityAsync(
                _fixture.CreateGrpcChannel, PingAt(LoopLat, LoopLon)));

        exception.StatusCode.ShouldBe(StatusCode.Unauthenticated);
        _fixture.Publisher.Published.ShouldBeEmpty();
    }

    // Resets policy, documents and recorded publishes, then optionally seeds D's trigger baseline.
    private async Task<Guid> ArrangeAsync(double? baselineLat, double? baselineLon, int lastPublishedSecondsAgo)
    {
        await ResetAsync();

        var driverId = Guid.CreateVersion7();

        if (baselineLat is null || baselineLon is null)
            return driverId;

        var store = _fixture.Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();

        session.Store(new LastKnownPositionDocument
        {
            Id = driverId,
            Lat = baselineLat.Value,
            Lon = baselineLon.Value,
            H3Cell = CellAt(baselineLat.Value, baselineLon.Value),
            ServerReceivedAt = DateTimeOffset.UtcNow.AddSeconds(-lastPublishedSecondsAgo)
        });

        await session.SaveChangesAsync();

        return driverId;
    }

    private async Task ResetAsync()
    {
        await _fixture.ResetToSeedAsync();
        await _fixture.ResetPositionsAsync();
        _fixture.Publisher.Clear();
    }

    private Task<LocationIngestAck> StreamAsync(Guid driverId, params LocationPing[] pings) =>
        ReportLocationsClient.StreamAsync(_fixture.CreateGrpcChannel, driverId, pings);

    private async Task<LastKnownPositionDocument?> LoadAsync(Guid driverId)
    {
        var store = _fixture.Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.QuerySession();
        return await session.LoadAsync<LastKnownPositionDocument>(driverId);
    }

    private static string CellAt(double lat, double lon) =>
        H3CellIndexer.TryComputeCell(lat, lon, 9)!;

    private static LocationPing PingAt(double lat, double lon, double accuracyMeters = 8d) =>
        ReportLocationsClient.PingAt(lat, lon, accuracyMeters);
}
