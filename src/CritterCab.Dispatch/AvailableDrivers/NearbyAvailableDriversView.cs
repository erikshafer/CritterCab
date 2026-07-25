using CritterCab.Dispatch.CandidateSelection;
using Marten;

namespace CritterCab.Dispatch.AvailableDrivers;

// The adapter that finally backs slice 5.3's stub seam with real data (W006 §6.5).
//
// This is the payoff for having built INearbyAvailableDriversSource as a port back when there was
// nothing to put behind it: CandidateSelectionAutomation does not change, at all, to go from
// hardcoded drivers to a live cross-service feed. W001 §5.3's "No external call — operates entirely
// on already-available views" also survives intact, and it survives BECAUSE ADR-018 chose the local
// projection over a Telemetry gRPC query. The rejected option would have made that line false.
//
// IQuerySession, not IDocumentSession: this path only reads. The two handlers in this folder write.
public sealed class NearbyAvailableDriversView(IQuerySession session) : INearbyAvailableDriversSource
{
    // Invented at implementation time, and flagged as such rather than buried.
    //
    // W001 §5.3 locks match-score as inverse straight-line distance for v1 and defers road-network
    // ETA to "a future gRPC counterparty" — so the spec gives a DISTANCE but never an ETA, and this
    // view has no ETA source. Deriving one from an assumed urban average speed keeps the field
    // meaningful and keeps the seam ready: when the ETA service lands, it replaces this one
    // expression and no event shape changes. 30 km/h is a common urban-average figure and is a
    // placeholder, not a measurement — the same honest treatment PR #45 gave its 100m accuracy
    // threshold. Do not tune it as though it were calibrated.
    private const double AssumedUrbanSpeedMetersPerSecond = 30_000.0 / 3_600.0;

    public async Task<IReadOnlyList<NearbyDriver>> GetDriversAsync(
        Location pickup,
        int searchRadiusMeters,
        VehicleClass vehicleClassRequired,
        CancellationToken ct = default)
    {
        // The k-ring must be computed at the SAME H3 resolution Telemetry published at, and that
        // resolution is a TelemetryPolicy value Dispatch does not own. Rather than hold a constant
        // that must silently match another service's config, read it off the stream: the most
        // recently ingested document carries the resolution in force when it was published.
        //
        // The alternative — a Dispatch-side constant — is a cross-service coupling with no
        // enforcement, where a Telemetry policy change breaks these queries with no error at all,
        // just an empty result that reads as "no drivers nearby".
        //
        // Note resolution 0 is a VALID H3 resolution, so a FirstOrDefaultAsync returning 0 could
        // not be told apart from "no documents". Take(1) into a list and branch on emptiness
        // instead — the distinction is the whole point, and a default value cannot carry it.
        var resolutions = await session.Query<AvailableDriver>()
            .OrderByDescending(d => d.ServerReceivedAt)
            .Select(d => d.H3Resolution)
            .Take(1)
            .ToListAsync(ct);

        // No documents means no drivers, which short-circuits before any H3 work. This is also the
        // steady state on a cold start before the first Kafka message arrives.
        if (resolutions.Count == 0)
            return [];

        var resolution = resolutions[0];
        var pickupCell = H3KRing.TryComputeCell(pickup.Lat, pickup.Lon, resolution);

        if (pickupCell is null)
            return [];

        var cells = H3KRing.CellsWithin(pickupCell, resolution, searchRadiusMeters);

        if (cells.Count == 0)
            return [];

        // Marten translates Contains() over a captured collection to `= ANY(:param)` — a SINGLE
        // array parameter against the duplicated h3_cell column, not an N-term IN list. That is
        // what makes a ~1,000-cell ring affordable in one round trip.
        //
        // The availability predicates are the fork-1 exclusion rule expressed as SQL. A driver whose
        // AvailabilityState is null has never been heard about from Driver Profile, and `d.X == v`
        // is not true for a null X, so those drivers fall out here without a special case. Until the
        // ASB half is built that is EVERY driver, and the empty result is correct rather than
        // broken: Dispatch genuinely cannot know who is dispatchable yet.
        var candidates = await session.Query<AvailableDriver>()
            .Where(d => cells.Contains(d.H3Cell)
                        && d.AvailabilityState == DriverAvailabilityState.Available
                        && d.VehicleClass == vehicleClassRequired)
            .ToListAsync(ct);

        // The exact-distance filter that trims the k-ring's deliberate over-approximation back to
        // the true radius. Ordering by distance here rather than in SQL is deliberate: the distance
        // is a great-circle computation over two columns, so Postgres would have to compute it for
        // every row of the ring anyway, and the ring is already bounded to a single pickup radius.
        return candidates
            .Select(d => new
            {
                Driver = d,
                Distance = H3KRing.DistanceMeters(pickup.Lat, pickup.Lon, d.Lat, d.Lon)
            })
            .Where(x => x.Distance <= searchRadiusMeters)
            .OrderBy(x => x.Distance)
            .Select(x => new NearbyDriver(
                DriverId: x.Driver.Id,
                DistanceMeters: (int)Math.Round(x.Distance),
                EtaSeconds: (int)Math.Round(x.Distance / AssumedUrbanSpeedMetersPerSecond),
                // Non-null by construction: the query above filters to a concrete vehicle class.
                VehicleClass: x.Driver.VehicleClass!.Value))
            .ToList();
    }
}
