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

        // An availability event can arrive before Dispatch has ever seen a position for this driver
        // — a driver who comes on shift indoors, or whose first ping is still inside Telemetry's
        // throttle window. There is no location side to write, and inventing one is not an option
        // (0,0 is in the Gulf of Guinea, and a fabricated position would put a real driver in a
        // real k-ring).
        //
        // BE PRECISE ABOUT WHAT HAPPENS HERE: the event is DROPPED, not deferred. Telemetry's
        // heartbeat will create the document within heartbeatIntervalSeconds, but it creates it
        // with a null availability side — the location handler carries forward whatever it finds,
        // and it finds nothing. So the driver stays excluded from selection until Driver Profile
        // sends its NEXT transition, which for a driver who simply came on shift and stayed on
        // shift may be hours.
        //
        // Shipped as an explicit deferral rather than solved, because solving it means deciding
        // something that belongs to Driver Profile's un-workshopped contract: either it republishes
        // current state on demand (a snapshot/replay endpoint), or Dispatch buffers unmatched
        // availability events and applies them when a position lands. Both are real designs; W006
        // §6.5 chose neither because it assumed both feeders existed. Recorded as a forward-
        // constraint on that workshop; pinned by a test so the behaviour cannot change silently.
        if (existing is null)
            return;

        // The AVAILABILITY side's last-writer-wins guard, on its OWN clock (W006 §6.5: "LWW per
        // driver per side"). Mirror image of the location handler's guard — and deliberately not
        // the document revision, which cannot order two independent clocks. See that handler for
        // the dispatch bug this separation exists to prevent.
        if (existing.AvailabilityUpdatedAt is not null
            && message.AvailabilityUpdatedAt <= existing.AvailabilityUpdatedAt)
        {
            return;
        }

        var updated = existing with
        {
            AvailabilityState = message.AvailabilityState,
            VehicleClass = message.VehicleClass,
            AvailabilityUpdatedAt = message.AvailabilityUpdatedAt
        };

        // Concurrency guard only, exactly as on the location side: this closes the window between
        // the LoadAsync above and this write, so the two handlers cannot lost-update each other's
        // side. A losing race throws and Wolverine retries the handler against fresh state.
        session.UpdateRevision(updated, existing.Version + 1);

        await session.SaveChangesAsync(ct);
    }
}
