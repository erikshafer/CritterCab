using System.Net;
using Alba;
using CritterCab.Dispatch.RideRequesting;
using Shouldly;
using Xunit;

namespace CritterCab.Dispatch.Tests.RideRequesting;

[Collection("Dispatch")]
public class SubmitRideRequestTests
{
    private readonly IAlbaHost _host;

    public SubmitRideRequestTests(DispatchTestFixture fixture) => _host = fixture.Host;

    [Fact]
    public async Task happy_path_creates_ride_request()
    {
        var riderId = Guid.CreateVersion7();
        var command = new SubmitRideRequest(
            RiderId: riderId,
            Pickup: new Location(40.7128, -74.0060, "123 Main St"),
            Dropoff: new Location(40.7580, -73.9855, "Eastbridge Library"),
            VehicleClass: VehicleClass.Standard,
            NotesForDriver: "meet at side entrance");

        var result = await _host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/rides/request");
            s.StatusCodeShouldBe(HttpStatusCode.Created);
        });

        var response = result.ReadAsJson<RideRequestResponse>();
        response.ShouldNotBeNull();
        response.RideRequestId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task rider_with_active_request_is_rejected()
    {
        var riderId = Guid.CreateVersion7();
        var command = new SubmitRideRequest(
            RiderId: riderId,
            Pickup: new Location(40.7128, -74.0060),
            Dropoff: new Location(40.7580, -73.9855),
            VehicleClass: VehicleClass.Standard,
            NotesForDriver: null);

        // First request succeeds
        await _host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/rides/request");
            s.StatusCodeShouldBe(HttpStatusCode.Created);
        });

        // Second request for same rider is rejected
        var secondCommand = new SubmitRideRequest(
            RiderId: riderId,
            Pickup: new Location(40.7200, -74.0100),
            Dropoff: new Location(40.7600, -73.9900),
            VehicleClass: VehicleClass.Premium,
            NotesForDriver: null);

        await _host.Scenario(s =>
        {
            s.Post.Json(secondCommand).ToUrl("/api/rides/request");
            s.StatusCodeShouldBe(HttpStatusCode.Conflict);
        });
    }

    [Fact]
    public async Task different_riders_can_submit_simultaneously()
    {
        var rider1 = Guid.CreateVersion7();
        var rider2 = Guid.CreateVersion7();

        var command1 = new SubmitRideRequest(
            RiderId: rider1,
            Pickup: new Location(40.7128, -74.0060),
            Dropoff: new Location(40.7580, -73.9855),
            VehicleClass: VehicleClass.Standard,
            NotesForDriver: null);

        var command2 = new SubmitRideRequest(
            RiderId: rider2,
            Pickup: new Location(40.7300, -74.0200),
            Dropoff: new Location(40.7700, -73.9700),
            VehicleClass: VehicleClass.Accessible,
            NotesForDriver: null);

        await _host.Scenario(s =>
        {
            s.Post.Json(command1).ToUrl("/api/rides/request");
            s.StatusCodeShouldBe(HttpStatusCode.Created);
        });

        await _host.Scenario(s =>
        {
            s.Post.Json(command2).ToUrl("/api/rides/request");
            s.StatusCodeShouldBe(HttpStatusCode.Created);
        });
    }
}
