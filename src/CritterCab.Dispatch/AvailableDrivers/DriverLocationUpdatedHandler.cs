using CritterCab.Telemetry.V1;
using Marten;

namespace CritterCab.Dispatch.AvailableDrivers;

// CritterCab's first cross-service consumer (W006 §6.5, ADR-018 consumer half). A ping that entered
// Telemetry over gRPC, survived the throttle, and was published to telemetry.driver-location-updated
// lands here as a row in Dispatch's document store.
//
// A vanilla Wolverine message handler — Kafka is a transport wire, not a handler shape. Nothing
// about the topic appears in this file; the binding lives entirely in Program.cs.
//
// NOT named *Automation, and the reason is vocabulary rather than mechanics. W001 §5.2 pins
// "Automation" to handlers reacting to a domain event already on a Marten stream (FareQuoteAutomation
// off RideRequested). This reacts to an inbound message from another service, which is a different
// shape in Cab's language. Mechanically either name would be discovered: Wolverine's conventional
// discovery is ADDITIVE, so Program.cs's CustomizeHandlerDiscovery(WithNameSuffix("Automation"))
// widens the default set rather than replacing it, and the built-in "Handler" suffix still applies.
//
// Worth knowing when this goes quiet: handler discovery is a precondition for DESERIALIZATION, not
// just for dispatch. Wolverine resolves the wire type name through HandlerGraph._messageTypes, which
// is populated from discovered handler chains. If this class stops being discovered — made internal,
// renamed, moved out of the scanned assembly — the message is never deserialized and the failure
// presents as silence, not as an error.
public static class DriverLocationUpdatedHandler
{
    public static async Task Handle(
        DriverLocationUpdated message,
        IDocumentSession session,
        CancellationToken ct)
    {
        var driverId = Guid.Parse(message.DriverId);
        var serverReceivedAt = message.ServerReceivedAt.ToDateTimeOffset();

        // Read the existing document to preserve the availability side. The two sides have separate
        // writers and separate clocks (W006 §6.5), so a blind whole-document write here would erase
        // whatever Driver Profile last told us about this driver. Null on first sight is the normal
        // case, not an error — a driver's first published position precedes any availability event
        // roughly as often as it follows one.
        var existing = await session.LoadAsync<AvailableDriver>(driverId, ct);

        var updated = new AvailableDriver
        {
            Id = driverId,
            H3Cell = message.H3Cell,
            H3Resolution = message.H3Resolution,
            Lat = message.Lat,
            Lon = message.Lon,
            ServerReceivedAt = serverReceivedAt,

            // Carried forward untouched. See above.
            AvailabilityState = existing?.AvailabilityState,
            VehicleClass = existing?.VehicleClass,
            AvailabilityUpdatedAt = existing?.AvailabilityUpdatedAt
        };

        // The dedup/LWW guard W006 §6.3 asks for, enforced in the database rather than in this
        // method. TryUpdateRevision emits a single upsert whose WHERE clause compares the stored
        // revision, so a stale or duplicate delivery is discarded server-side and silently — no
        // exception, no second round trip, and no window between a read and a write for a
        // concurrent delivery to slip through.
        //
        // Why this matters despite the partition key: Kafka partitions by driverId, so per-driver
        // ordering IS guaranteed in steady state and a naive Store() would usually be fine. It is
        // redelivery that breaks the assumption — on a consumer-group rebalance, uncommitted offsets
        // replay, so an OLDER position can arrive after a newer one was already applied. That is a
        // real regression of the view, and the guard makes it free to prevent.
        //
        // The revision is unix-MILLISECONDS of the server-stamped receipt time, because Marten's
        // revision is a monotonic long and not a timestamp. Two consequences:
        //   - Equal timestamps are a no-op, which is exactly the dedup semantics §6.3 specifies
        //     ("the projection applies the position at most once").
        //   - Revision 0 means "always win" to Marten, so a zero-valued timestamp would defeat the
        //     guard entirely. Unreachable in practice (Telemetry server-stamps every publish), but
        //     the floor costs one Math.Max and removes the failure mode.
        var revision = Math.Max(1, serverReceivedAt.ToUnixTimeMilliseconds());

        session.TryUpdateRevision(updated, revision);

        await session.SaveChangesAsync(ct);
    }
}
