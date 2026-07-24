namespace CritterCab.Telemetry.LastKnownPosition;

// Telemetry's location-of-record (W006 §3.3): a plain Marten document, overwrite-in-place.
// NOT an event stream and NOT a projection — this stream-processing BC event-sources only the
// TelemetryPolicy config singleton (§3.4), which makes this CritterCab's first non-event-sourced
// write path. Nothing here is source-generated, so no `partial` is required.
//
// The document defends no invariant, so there is no aggregate boundary and no optimistic
// concurrency: last-writer-wins on ServerReceivedAt is the entire concurrency story (§3.3). A
// driver's own pings are the sole writer for that driver's document.
//
// A record rather than §3.3's mutable class sketch: every write replaces the whole row via
// session.Store(), so nothing is ever mutated in place and immutability costs nothing.
public sealed record LastKnownPosition
{
    // driverId — resolved from the authenticated principal, NEVER the ping payload (R5). Marten
    // takes the `Id` property as document identity by convention, so "one document per driver"
    // falls out of the shape. Not minted here, hence no Guid.CreateVersion7(): the value is an
    // external key that arrives with the request.
    public required Guid Id { get; init; }

    public required double Lat { get; init; }

    // `Lon`, not §3.3's `Lng` — report_locations.proto is the contract and it says `lon` (ADR-009).
    public required double Lon { get; init; }

    // The H3 index at the policy's H3Resolution (R6) — published language shared with Dispatch
    // (ADR-018). Kept in string form so the cell travels to Kafka unchanged in slice 3.
    public required string H3Cell { get; init; }

    // Server-stamped receipt time of the ping that caused the last publish. ONE field serving TWO
    // roles, deliberately:
    //   - §6.2's `lastPublishedAt` trigger baseline (heartbeatDue / throttleFloorElapsed), and
    //   - §6.4's eviction key (swept once older than 3 x heartbeatIntervalSeconds).
    // They are the same instant by construction, because §6.4 locks upsert-on-publish-only: the
    // document is written only when a publish happens, so its stamp IS the moment of last publish.
    // A separate LastPublishedAt would hold a duplicate value in v1 and force eviction and the
    // trigger to read different names for one instant. Omitted deliberately — revisit only if
    // writes ever become per-ping, where the two would genuinely diverge.
    public required DateTimeOffset ServerReceivedAt { get; init; }
}
