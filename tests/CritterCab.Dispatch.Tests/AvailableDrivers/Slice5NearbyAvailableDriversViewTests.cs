using CritterCab.Dispatch.AvailableDrivers;
using CritterCab.Dispatch.CandidateSelection;
using CritterCab.Telemetry.V1;
using JasperFx.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Wolverine;
using Wolverine.Tracking;
using Xunit;
using ProtoTimestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace CritterCab.Dispatch.Tests.AvailableDrivers;

// W006 §6.5's Selection-read GWT, and the fork-1 exclusion rule that governs it.
//
// These exercise the REAL INearbyAvailableDriversSource registration — the one that replaced the
// stub — resolved from the running host, so the k-ring query, the exact-distance filter and the
// availability join are all under test as production wires them.
[Collection("DispatchKafka")]
public class Slice5NearbyAvailableDriversViewTests
{
    // Chicago's Loop, and two points at known distances from it. The near driver sits a few hundred
    // metres away, the far one several kilometres — comfortably either side of a 2km radius, so the
    // test is not sensitive to the exact great-circle arithmetic.
    private const double PickupLat = 41.8827d, PickupLon = -87.6233d;
    private const double NearLat = 41.8850d, NearLon = -87.6250d;
    private const double FarLat = 41.9400d, FarLon = -87.6900d;
    private const int Resolution = 9;
    private const int RadiusMeters = 2_000;

    private readonly DispatchKafkaTestFixture _fixture;

    public Slice5NearbyAvailableDriversViewTests(DispatchKafkaTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task drivers_in_range_and_capable_are_returned_nearest_first()
    {
        await _fixture.ResetDriversAsync();

        var near = await AvailableDriverAt(NearLat, NearLon, VehicleClass.Standard);
        var far = await AvailableDriverAt(FarLat, FarLon, VehicleClass.Standard);

        var found = await QueryAsync(RadiusMeters, VehicleClass.Standard);

        // The far driver is inside the k-ring — the ring deliberately over-approximates — and is
        // trimmed by the exact-distance filter. That is the assertion that proves the two-stage
        // query works rather than the ring accidentally being tight enough.
        found.Select(d => d.DriverId).ShouldBe([near]);
        found[0].DistanceMeters.ShouldBeLessThan(RadiusMeters);

        // ETA is derived from distance against an invented urban-speed constant (W006 §6.5 fork 3).
        // Asserted as a relationship, not a value: pinning the number here would freeze a
        // placeholder that a real ETA service is meant to replace.
        found[0].EtaSeconds.ShouldBeGreaterThan(0);

        far.ShouldNotBe(near);
    }

    [Fact]
    public async Task a_driver_of_the_wrong_vehicle_class_is_excluded()
    {
        await _fixture.ResetDriversAsync();

        await AvailableDriverAt(NearLat, NearLon, VehicleClass.Standard);

        var found = await QueryAsync(RadiusMeters, VehicleClass.Accessible);

        // W001 §5.3's ACCESSIBLE-scarcity path: the driver is present, in range, and available, but
        // cannot serve the request. The filter is a real SQL predicate over a duplicated column.
        found.ShouldBeEmpty();
    }

    [Fact]
    public async Task a_driver_who_is_not_available_is_excluded()
    {
        await _fixture.ResetDriversAsync();

        await AvailableDriverAt(NearLat, NearLon, VehicleClass.Standard, DriverAvailabilityState.Offline);

        var found = await QueryAsync(RadiusMeters, VehicleClass.Standard);

        found.ShouldBeEmpty();
    }

    // The fork-1 rule, and the one most worth locking down: this is the state EVERY driver is in
    // until Driver Profile ships, so if it ever silently flipped to "included", Dispatch would
    // start offering rides based on a capability it never learned.
    [Fact]
    public async Task a_driver_with_a_position_but_no_availability_is_excluded()
    {
        await _fixture.ResetDriversAsync();

        // A location update only — no availability event follows it.
        var driverId = Guid.CreateVersion7();
        await PublishPositionAsync(driverId, NearLat, NearLon);

        var stored = await _fixture.LoadDriverAsync(driverId);
        stored.ShouldNotBeNull("the position itself should still have landed");
        stored.AvailabilityState.ShouldBeNull();

        var found = await QueryAsync(RadiusMeters, VehicleClass.Standard);

        found.ShouldBeEmpty();
    }

    [Fact]
    public async Task an_empty_store_returns_no_drivers_without_guessing_a_resolution()
    {
        await _fixture.ResetDriversAsync();

        // With no documents there is no resolution to derive, and resolution 0 is itself valid —
        // so the view must short-circuit on document ABSENCE rather than on a default value.
        var found = await QueryAsync(RadiusMeters, VehicleClass.Standard);

        found.ShouldBeEmpty();
    }

    private async Task<Guid> AvailableDriverAt(
        double lat,
        double lon,
        VehicleClass vehicleClass,
        DriverAvailabilityState state = DriverAvailabilityState.Available)
    {
        var driverId = Guid.CreateVersion7();

        await PublishPositionAsync(driverId, lat, lon);

        // IMessageBus is scoped, so it cannot come from the root provider. Standing in for the ASB
        // transport that would deliver this in a built system (W006 §6.5 fork 1).
        using var scope = _fixture.Host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await bus.InvokeAsync(new DriverAvailabilityChanged
        {
            DriverId = driverId,
            AvailabilityState = state,
            VehicleClass = vehicleClass,
            AvailabilityUpdatedAt = DateTimeOffset.UtcNow
        });

        return driverId;
    }

    private Task PublishPositionAsync(Guid driverId, double lat, double lon)
    {
        var message = new DriverLocationUpdated
        {
            DriverId = driverId.ToString(),
            Lat = lat,
            Lon = lon,
            H3Cell = H3KRing.TryComputeCell(lat, lon, Resolution)!,
            H3Resolution = Resolution,
            ServerReceivedAt = ProtoTimestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            ThrottlePolicyVersion = 1L
        };

        return _fixture.Host
            .TrackActivity()
            .Timeout(30.Seconds())
            .WaitForMessageToBeReceivedAt<DriverLocationUpdated>(_fixture.Host)
            .ExecuteAndWaitAsync(_ => _fixture.ProduceAsync(message));
    }

    // Resolves the source from the host rather than newing it up, so the test exercises whatever
    // Program.cs registered. If the stub were ever wired back in by accident, these tests would
    // start passing for the wrong reason — hence the scoped resolution and the type assertion.
    private async Task<IReadOnlyList<NearbyDriver>> QueryAsync(
        int radiusMeters, VehicleClass vehicleClass)
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<INearbyAvailableDriversSource>();

        source.ShouldBeOfType<NearbyAvailableDriversView>();

        return await source.GetDriversAsync(
            new Location(PickupLat, PickupLon), radiusMeters, vehicleClass);
    }
}
