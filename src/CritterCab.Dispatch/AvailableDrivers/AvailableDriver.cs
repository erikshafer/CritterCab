using JasperFx;

namespace CritterCab.Dispatch.AvailableDrivers;

// Dispatch's local view of who is out there and dispatchable (W006 §6.5, ADR-018 consumer half).
//
// A plain Marten document, NOT an event projection — and that is the load-bearing decision of this
// slice. Dispatch is CritterCab's canonical event-sourced BC: RideRequest is an aggregate, the
// timeline and rounds are projections, the whole decider apparatus is here. This document sits
// alongside all of that and is none of it. Event-sourcing the inbound location stream would
// reimport onto Dispatch's event store exactly the per-ping volume Telemetry's throttle exists to
// suppress. So Dispatch consumes the telemetry stream the same shape Telemetry produces it:
// document upsert, last-writer-wins, no history. The stream-processing shape crosses the BC
// boundary along with the data, by necessity.
//
// TWO SIDES, TWO WRITERS, TWO CLOCKS. The location side is fed by Kafka (DriverLocationUpdated,
// W006 slice 3); the availability side is fed by Azure Service Bus from Driver Profile — a BC that
// has not been workshopped, so that half is a forward-constraint (ADR-018). Each side is upserted
// independently by its own handler and neither may clobber the other. They are joined only by
// DriverId, which is why this is a document and not two.
public sealed record AvailableDriver : ILongVersioned
{
    // driverId. Marten takes `Id` as document identity by convention, so "one document per driver"
    // falls out of the shape. An external key that arrives on the wire, never minted here — hence
    // no Guid.CreateVersion7() (contrast the event rows in RideRequesting/).
    public required Guid Id { get; init; }

    // === Location side — written by DriverLocationUpdatedHandler, from Kafka ===

    // The H3 index at H3Resolution, as published by Telemetry. Published language: both BCs agree
    // on the cell system, and the cell id travels from Telemetry's H3CellIndexer to this column
    // unchanged. Duplicated to a real column in Program.cs so the k-ring `= ANY` query is indexed.
    public required string H3Cell { get; init; }

    // Carried alongside the cell rather than bit-decoded from it. Dispatch does not own the
    // TelemetryPolicy that sets this, so it cannot assume a value — it reads whatever Telemetry
    // published. NearbyAvailableDriversView derives the query resolution from the most recently
    // ingested document precisely because this field exists (W006 §6.5, fork 2).
    public required int H3Resolution { get; init; }

    public required double Lat { get; init; }

    // `Lon`, not `Lng` — driver_location_updated.proto is the contract and it says `lon` (ADR-009).
    // Matches Telemetry's LastKnownPosition for the same reason.
    public required double Lon { get; init; }

    // The location side's LWW / dedup key (W006 §6.3 R7). Server-stamped by Telemetry and
    // monotonic per driver. Stored for readability and assertions; the actual staleness guard runs
    // in the database off Marten's numeric revision, which carries this same instant as unix-ms.
    public required DateTimeOffset ServerReceivedAt { get; init; }

    // === Availability side — written by DriverAvailabilityChangedHandler, from ASB (unbuilt) ===
    //
    // Nullable rather than `required`, and the nullability is meaningful rather than incidental:
    // null means "Dispatch has never heard from Driver Profile about this driver", which is the
    // steady state until that BC ships. NearbyAvailableDriversView EXCLUDES such drivers — you
    // cannot dispatch to a driver whose capability you do not know (W006 §6.5, fork 1). Defaulting
    // these to Available/Standard would keep a demo alive by fabricating a capability claim
    // Dispatch has no source for, invisibly, at the point of query.

    public DriverAvailabilityState? AvailabilityState { get; init; }

    public VehicleClass? VehicleClass { get; init; }

    // The availability side's own LWW key. Deliberately NOT the same clock as ServerReceivedAt:
    // the two sides arrive over different transports from different services and are ordered
    // independently. W006 §6.5: "LWW per driver per side."
    public DateTimeOffset? AvailabilityUpdatedAt { get; init; }

    // Marten's numeric revision, guarding both sides against stale redelivery. Registered with
    // UseNumericRevisions(true) in Program.cs — without that registration TryUpdateRevision
    // silently degrades to an unguarded upsert, which is the failure mode this field exists to
    // prevent. See DriverLocationUpdatedHandler for how the revision is derived.
    //
    // ILongVersioned, NOT IRevisioned, and the difference is not cosmetic. IRevisioned is `int
    // Version` and backs an `integer` mt_version column; ILongVersioned is `long` and backs
    // `bigint`. The revision this slice stores is a unix-MILLISECOND timestamp (~1.7e12), which
    // overflowed int in 1970 — so IRevisioned would have silently truncated every guard value and
    // made the LWW comparison meaningless. The interface choice is what makes a timestamp usable
    // as a revision at all.
    public long Version { get; set; }
}

// Dispatch's own enum, not a shared one. Driver Profile will have its own availability vocabulary
// and the ASB translation handler maps into this at the boundary — BCs own their enums (the same
// rule VehicleClass follows). The three states are what W001 §5.3's four anticipated Driver Profile
// events collapse to from Dispatch's point of view: only Available is dispatchable.
public enum DriverAvailabilityState
{
    Available,
    OnBreak,
    Offline
}
