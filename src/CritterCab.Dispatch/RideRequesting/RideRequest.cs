using JasperFx.Events;

namespace CritterCab.Dispatch.RideRequesting;

public sealed record RideRequest(
    Guid Id,
    Guid RiderId,
    Location Pickup,
    Location Dropoff,
    VehicleClass VehicleClass,
    string? NotesForDriver,
    DateTimeOffset RequestedAt)
{
    public static RideRequest Create(IEvent<RideRequested> @event)
    {
        var e = @event.Data;
        return new RideRequest(
            Id: @event.StreamId,
            RiderId: e.RiderId,
            Pickup: e.Pickup,
            Dropoff: e.Dropoff,
            VehicleClass: e.VehicleClass,
            NotesForDriver: e.NotesForDriver,
            RequestedAt: e.RequestedAt);
    }
}
