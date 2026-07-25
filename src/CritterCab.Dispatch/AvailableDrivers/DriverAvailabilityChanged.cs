using Marten;

namespace CritterCab.Dispatch.AvailableDrivers;

// The ASB half of ADR-018's join — as a landing site, not as a transport.
//
// READ THIS BEFORE EXTENDING IT. Driver Profile has not been workshopped. It owns the real
// availability vocabulary, and W001 §5.3 anticipates FOUR distinct events from it (DriverCameOnline,
// DriverWentOnBreak, DriverWentOffline, DriverVehicleChanged). This type is deliberately none of
// those. It carries exactly the four availability-side fields W006 §6.5 already locks on the
// AvailableDriver document and nothing more, so it describes the SHAPE OF THE HOLE rather than
// guessing at the vocabulary that will fill it. When Driver Profile ships, its published events
// replace this type and this handler becomes their translation target; inventing transitions here
// now would pre-empt that workshop and make the replacement a migration instead of a swap.
//
// It is a Dispatch-local message: not a proto, not a published contract, not registered as a Marten
// event type. Nothing publishes it and NO TRANSPORT IS BOUND TO IT. Until ASB is wired, the only
// callers are tests driving it in-process through IMessageBus — which is enough to prove the join
// and the exclusion rule work, without pretending the feeder exists.
public sealed record DriverAvailabilityChanged
{
    public required Guid DriverId { get; init; }

    public required DriverAvailabilityState AvailabilityState { get; init; }

    // The driver's in-service vehicle class at the moment of the transition. W001 §5.3 locks
    // one vehicle in service at a time for v1, and each Driver Profile transition carries the
    // capability — which is why capability rides on the availability side rather than being a
    // fifth field of its own.
    public required VehicleClass VehicleClass { get; init; }

    // The availability side's own LWW key. A separate clock from the location side's
    // ServerReceivedAt: different service, different transport, independently ordered.
    public required DateTimeOffset AvailabilityUpdatedAt { get; init; }
}

// Mirror image of DriverLocationUpdatedHandler: writes the availability side and must leave the
// location side untouched.
public static class DriverAvailabilityChangedHandler
{
    public static async Task Handle(
        DriverAvailabilityChanged message,
        IDocumentSession session,
        CancellationToken ct)
    {
        var existing = await session.LoadAsync<AvailableDriver>(message.DriverId, ct);

        // An availability event can legitimately arrive before Dispatch has ever seen a position
        // for this driver — a driver who comes on shift indoors, or whose first ping is still in
        // Telemetry's throttle window. There is no location side to write yet, and inventing one
        // (0,0 is in the Gulf of Guinea) would put a real driver in a real k-ring somewhere. Drop
        // it: Telemetry publishes on the driver's first cell change or heartbeat regardless, so
        // the document appears within one heartbeat interval, carrying this state if it arrives
        // first. Consistent with W006's heartbeat-as-backstop reasoning throughout.
        if (existing is null)
            return;

        var updated = existing with
        {
            AvailabilityState = message.AvailabilityState,
            VehicleClass = message.VehicleClass,
            AvailabilityUpdatedAt = message.AvailabilityUpdatedAt
        };

        // Same database-side LWW guard as the location side, on this side's own clock. Note both
        // sides share ONE revision column, so an availability update and a location update compete
        // for it. That is acceptable while the availability feed is unbuilt and low-rate, and it is
        // the first thing to revisit when ASB lands: if the two clocks interleave under load, the
        // sides need independent guards rather than one shared revision.
        var revision = Math.Max(1, message.AvailabilityUpdatedAt.ToUnixTimeMilliseconds());

        session.TryUpdateRevision(updated, revision);

        await session.SaveChangesAsync(ct);
    }
}
