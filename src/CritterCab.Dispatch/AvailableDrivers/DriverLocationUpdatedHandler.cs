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
        // Guard the wire before trusting it. proto3 has no required fields, so a malformed or
        // partially-populated record deserializes happily with "" and null defaults — Guid.Parse
        // would throw FormatException and ServerReceivedAt would NRE, both of which dead-letter a
        // message that is simply not addressed to us. Drop instead, matching W006 §6.2's treatment
        // of invalid pings on the producing side: bad input is a value to branch on, not an error.
        if (!Guid.TryParse(message.DriverId, out var driverId) || message.ServerReceivedAt is null)
            return;

        var serverReceivedAt = message.ServerReceivedAt.ToDateTimeOffset();

        // Read the existing document to preserve the availability side. The two sides have separate
        // writers and separate clocks (W006 §6.5), so a blind whole-document write here would erase
        // whatever Driver Profile last told us about this driver. Null on first sight is the normal
        // case, not an error — a driver's first published position precedes any availability event
        // roughly as often as it follows one.
        var existing = await session.LoadAsync<AvailableDriver>(driverId, ct);

        // The LOCATION side's last-writer-wins guard, compared against the location side's OWN
        // clock — W006 §6.5 locks "LWW per driver per side", and the two sides are ordered
        // independently because they arrive from different services over different transports.
        //
        // This is deliberately NOT the document's revision. An earlier cut of this handler used
        // serverReceivedAt AS the Marten revision, which collapsed the two sides onto one ordering
        // key and produced a real dispatch bug: a driver going Offline at 12:00:05 could have that
        // write silently discarded by a heartbeat position stamped 12:00:07 that had already raised
        // the revision — leaving an offline driver dispatchable, with no error anywhere. Business
        // ordering and write concurrency are two different problems and need two different guards.
        //
        // Equality is a no-op, not an update: that IS §6.3's dedup semantics ("the projection
        // applies the position at most once"). A strictly-older timestamp is the redelivery case
        // that actually regresses the view — on a consumer-group rebalance, uncommitted offsets
        // replay, so an older position can arrive after a newer one was already applied.
        if (existing is not null && serverReceivedAt <= existing.ServerReceivedAt)
            return;

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

        // The document revision guards CONCURRENCY, not business ordering: it closes the window
        // between the LoadAsync above and this write, during which the availability handler could
        // have committed its own side. UpdateRevision throws ConcurrencyException on a losing race
        // rather than swallowing it, and Program.cs registers a Wolverine retry policy that runs
        // the handler again — reloading, re-evaluating the LWW guard, and merging against fresh
        // state. Without this the two handlers would lost-update each other: both load at revision
        // N, both write the whole document, and whichever commits second erases the other's side.
        session.UpdateRevision(updated, (existing?.Version ?? 0) + 1);

        await session.SaveChangesAsync(ct);
    }
}
