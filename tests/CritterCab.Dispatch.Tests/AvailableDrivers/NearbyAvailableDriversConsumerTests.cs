using CritterCab.Dispatch.AvailableDrivers;
using CritterCab.Telemetry.V1;
using JasperFx.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Wolverine;
using Wolverine.Tracking;
using Xunit;
using ProtoTimestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace CritterCab.Dispatch.Tests.AvailableDrivers;

// W006 §6.5's Location-update and Availability GWTs, plus §6.3's Dedup GWT — the one PR C could not
// write, because it asserts CONSUMER behaviour against at-least-once redelivery and there was no
// consumer until this slice.
//
// This is CritterCab's first cross-service assertion. Everything here goes over a real broker: the
// fixture produces the same binary-protobuf record Telemetry's publisher produces, and the service's
// own production listener wiring picks it up. Nothing is stubbed on the transport path.
[Collection("DispatchKafka")]
public class NearbyAvailableDriversConsumerTests
{
    private const double LoopLat = 41.8827d, LoopLon = -87.6233d;
    private const int Resolution = 9;

    private readonly DispatchKafkaTestFixture _fixture;

    public NearbyAvailableDriversConsumerTests(DispatchKafkaTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task a_published_position_lands_in_the_available_driver_document()
    {
        await _fixture.ResetDriversAsync();

        var driverId = Guid.CreateVersion7();
        var at = DateTimeOffset.UtcNow;

        await ProduceAndWaitAsync(PositionOf(driverId, LoopLat, LoopLon, at));

        var driver = await _fixture.LoadDriverAsync(driverId);

        driver.ShouldNotBeNull("No AvailableDriver document was written — if the handler stopped "
                               + "being discovered, the message is never even deserialized.");
        driver.H3Cell.ShouldBe(H3KRing.TryComputeCell(LoopLat, LoopLon, Resolution));
        driver.H3Resolution.ShouldBe(Resolution);
        driver.Lat.ShouldBe(LoopLat);
        driver.Lon.ShouldBe(LoopLon);

        // The availability side stays empty — this driver has a position and no capability, which
        // is the steady state until Driver Profile ships (W006 §6.5 fork 1).
        driver.AvailabilityState.ShouldBeNull();
        driver.VehicleClass.ShouldBeNull();
    }

    // W006 §6.3's Dedup GWT.
    [Fact]
    public async Task a_redelivered_position_is_applied_at_most_once()
    {
        await _fixture.ResetDriversAsync();

        var driverId = Guid.CreateVersion7();
        var at = DateTimeOffset.UtcNow;
        var position = PositionOf(driverId, LoopLat, LoopLon, at);

        await ProduceAndWaitAsync(position);
        var afterFirst = await _fixture.LoadDriverAsync(driverId);
        afterFirst.ShouldNotBeNull();

        // Byte-for-byte the same record, exactly as a consumer-group rebalance would replay it.
        await ProduceAndWaitAsync(PositionOf(driverId, LoopLat, LoopLon, at));

        var afterSecond = await _fixture.LoadDriverAsync(driverId);
        afterSecond.ShouldNotBeNull();

        // Equal serverReceivedAt means an equal revision, and Marten's guard is a strict `<`, so
        // the duplicate is discarded in the database rather than reapplied. The version standing
        // still is the observable proof: had the guard been missing (or had the document been
        // registered IRevisioned instead of ILongVersioned, truncating the revision), this would
        // have incremented.
        afterSecond.Version.ShouldBe(afterFirst.Version);
        afterSecond.ServerReceivedAt.ToUnixTimeMilliseconds()
            .ShouldBe(at.ToUnixTimeMilliseconds());
    }

    // The redelivery case that actually regresses a view: not a duplicate, but an OLDER position
    // arriving after a newer one was already applied.
    [Fact]
    public async Task a_stale_position_never_overwrites_a_newer_one()
    {
        await _fixture.ResetDriversAsync();

        var driverId = Guid.CreateVersion7();
        var newer = DateTimeOffset.UtcNow;
        var older = newer.AddSeconds(-30);

        await ProduceAndWaitAsync(PositionOf(driverId, LoopLat, LoopLon, newer));

        // A different cell, so a successful overwrite would be unmistakable in the assertion below.
        const double StaleLat = 41.9d, StaleLon = -87.7d;
        await ProduceAndWaitAsync(PositionOf(driverId, StaleLat, StaleLon, older));

        var driver = await _fixture.LoadDriverAsync(driverId);

        driver.ShouldNotBeNull();
        driver.Lat.ShouldBe(LoopLat);
        driver.Lon.ShouldBe(LoopLon);
        driver.ServerReceivedAt.ToUnixTimeMilliseconds().ShouldBe(newer.ToUnixTimeMilliseconds());
    }

    // W006 §6.5's Availability GWT. The ASB transport does not exist, so the handler is invoked
    // in-process — which is exactly what fork 1 committed to: the landing site ships, the feeder
    // does not.
    [Fact]
    public async Task an_availability_event_fills_the_other_side_without_disturbing_the_location()
    {
        await _fixture.ResetDriversAsync();

        var driverId = Guid.CreateVersion7();
        var at = DateTimeOffset.UtcNow;

        await ProduceAndWaitAsync(PositionOf(driverId, LoopLat, LoopLon, at));

        // IMessageBus is scoped, so it cannot come from the root provider.
        using var scope = _fixture.Host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await bus.InvokeAsync(new DriverAvailabilityChanged
        {
            DriverId = driverId,
            AvailabilityState = DriverAvailabilityState.Available,
            VehicleClass = VehicleClass.Standard,
            AvailabilityUpdatedAt = at.AddSeconds(1)
        });

        var driver = await _fixture.LoadDriverAsync(driverId);

        driver.ShouldNotBeNull();
        driver.AvailabilityState.ShouldBe(DriverAvailabilityState.Available);
        driver.VehicleClass.ShouldBe(VehicleClass.Standard);

        // The location side survived the availability write. The two handlers share one document
        // and one revision column, so a blind whole-document write on either side would erase the
        // other — this is the assertion that catches it.
        driver.H3Cell.ShouldBe(H3KRing.TryComputeCell(LoopLat, LoopLon, Resolution));
        driver.Lat.ShouldBe(LoopLat);
    }

    // The regression test for the defect a two-axis code review found: with ONE revision column
    // shared by both sides, a heartbeat position stamped later than an availability transition
    // would silently discard that transition — leaving an offline driver dispatchable, with no
    // error anywhere. Per-side LWW is what W006 §6.5 locks, and this is what it buys.
    [Fact]
    public async Task an_availability_transition_is_applied_even_when_a_later_position_arrived_first()
    {
        await _fixture.ResetDriversAsync();

        var driverId = Guid.CreateVersion7();
        var t = DateTimeOffset.UtcNow;

        // The exact interleaving that broke under a single shared revision column: a position whose
        // timestamp is AHEAD of the availability transition lands FIRST. Under the old scheme that
        // position set the revision to t+10s, and the Offline transition stamped t+5s then failed
        // the `mt_version < ?` guard and was discarded server-side — silently leaving an offline
        // driver dispatchable. Per-side LWW compares the transition against the AVAILABILITY
        // side's own clock, which is empty here, so it applies.
        await ProduceAndWaitAsync(PositionOf(driverId, LoopLat, LoopLon, t));
        await ProduceAndWaitAsync(PositionOf(driverId, LoopLat, LoopLon, t.AddSeconds(10)));

        await ApplyAvailabilityAsync(driverId, DriverAvailabilityState.Offline, t.AddSeconds(5));

        var driver = await _fixture.LoadDriverAsync(driverId);

        driver.ShouldNotBeNull();
        driver.AvailabilityState.ShouldBe(DriverAvailabilityState.Offline);

        // And the location side kept the newest position — neither write clobbered the other.
        driver.ServerReceivedAt.ToUnixTimeMilliseconds()
            .ShouldBe(t.AddSeconds(10).ToUnixTimeMilliseconds());
    }

    // Mirror image: a stale availability transition must not win over a newer one just because it
    // arrived later. Each side is ordered on its own clock.
    [Fact]
    public async Task a_stale_availability_transition_never_overwrites_a_newer_one()
    {
        await _fixture.ResetDriversAsync();

        var driverId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;

        await ProduceAndWaitAsync(PositionOf(driverId, LoopLat, LoopLon, now));
        await ApplyAvailabilityAsync(driverId, DriverAvailabilityState.Offline, now);
        await ApplyAvailabilityAsync(driverId, DriverAvailabilityState.Available, now.AddSeconds(-30));

        var driver = await _fixture.LoadDriverAsync(driverId);

        driver.ShouldNotBeNull();
        driver.AvailabilityState.ShouldBe(DriverAvailabilityState.Offline);
    }

    // Pins a known, deliberate limitation rather than a desired behaviour — see
    // DriverAvailabilityChangedHandler for why it is a deferral and what closing it would require
    // from Driver Profile. Here so the drop cannot start or stop happening silently.
    [Fact]
    public async Task an_availability_event_for_an_unseen_driver_is_dropped_not_buffered()
    {
        await _fixture.ResetDriversAsync();

        var driverId = Guid.CreateVersion7();
        var at = DateTimeOffset.UtcNow;

        // Availability first, with no position ever received for this driver.
        await ApplyAvailabilityAsync(driverId, DriverAvailabilityState.Available, at);

        (await _fixture.LoadDriverAsync(driverId)).ShouldBeNull();

        // A position arrives later. The availability side does NOT reappear — the earlier event is
        // gone, not queued.
        await ProduceAndWaitAsync(PositionOf(driverId, LoopLat, LoopLon, at.AddSeconds(5)));

        var driver = await _fixture.LoadDriverAsync(driverId);

        driver.ShouldNotBeNull();
        driver.AvailabilityState.ShouldBeNull();
    }

    private async Task ApplyAvailabilityAsync(
        Guid driverId, DriverAvailabilityState state, DateTimeOffset at)
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await bus.InvokeAsync(new DriverAvailabilityChanged
        {
            DriverId = driverId,
            AvailabilityState = state,
            VehicleClass = VehicleClass.Standard,
            AvailabilityUpdatedAt = at
        });
    }

    // Produces the record and waits for Dispatch's listener to finish handling it.
    //
    // WaitForMessageToBeReceivedAt is the API that waits on an ARRIVAL. IncludeExternalTransports()
    // is deliberately absent: it governs whether OUTGOING sends stay open pending a receipt, and
    // there is no outgoing Wolverine send here — the fixture produces with a raw ProducerBuilder,
    // so there is nothing for the tracked session to observe on the way out.
    private Task ProduceAndWaitAsync(DriverLocationUpdated message) =>
        _fixture.Host
            .TrackActivity()
            .Timeout(30.Seconds())
            .WaitForMessageToBeReceivedAt<DriverLocationUpdated>(_fixture.Host)
            .ExecuteAndWaitAsync(_ => _fixture.ProduceAsync(message));

    private static DriverLocationUpdated PositionOf(
        Guid driverId, double lat, double lon, DateTimeOffset at) =>
        new()
        {
            DriverId = driverId.ToString(),
            Lat = lat,
            Lon = lon,
            H3Cell = H3KRing.TryComputeCell(lat, lon, Resolution)!,
            H3Resolution = Resolution,
            ServerReceivedAt = ProtoTimestamp.FromDateTimeOffset(at),
            ThrottlePolicyVersion = 1L
        };
}
