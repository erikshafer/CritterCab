namespace CritterCab.Dispatch.RideRequesting;

public sealed record RideRequested(
    Guid RideRequestId,
    Guid RiderId,
    Location Pickup,
    Location Dropoff,
    VehicleClass VehicleClass,
    string? NotesForDriver,
    DateTimeOffset RequestedAt);
