using Marten;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace CritterCab.Dispatch.RideRequesting;

public sealed record SubmitRideRequest(
    Guid RiderId,
    Location Pickup,
    Location Dropoff,
    VehicleClass VehicleClass,
    string? NotesForDriver);

public static class SubmitRideRequestEndpoint
{
    [WolverinePost("/api/rides/request")]
    public static async Task<IResult> Handle(
        SubmitRideRequest command,
        IDocumentSession session,
        TimeProvider time)
    {
        var activeRequest = await session.LoadAsync<ActiveRideRequest>(command.RiderId);
        if (activeRequest is not null)
        {
            return Results.Problem(
                title: "Rider has an active request",
                detail: $"Rider {command.RiderId} already has active ride request {activeRequest.RideRequestId}.",
                statusCode: 409);
        }

        var rideRequestId = Guid.CreateVersion7();
        var requestedAt = time.GetUtcNow();

        var @event = new RideRequested(
            RideRequestId: rideRequestId,
            RiderId: command.RiderId,
            Pickup: command.Pickup,
            Dropoff: command.Dropoff,
            VehicleClass: command.VehicleClass,
            NotesForDriver: command.NotesForDriver,
            RequestedAt: requestedAt);

        session.Events.StartStream<RideRequest>(rideRequestId, @event);
        await session.SaveChangesAsync();

        return Results.Created($"/api/rides/{rideRequestId}", new RideRequestResponse(rideRequestId, requestedAt));
    }
}

public sealed record RideRequestResponse(Guid RideRequestId, DateTimeOffset RequestedAt);
