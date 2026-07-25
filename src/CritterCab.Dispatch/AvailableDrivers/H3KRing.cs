using H3;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using NetTopologySuite.Geometries;

namespace CritterCab.Dispatch.AvailableDrivers;

// The geospatial half of W006 §6.5's radius query: "an H3 k-ring around the pickup cell + exact
// distance filter". Wrapped rather than inlined for the same reason Telemetry wraps H3CellIndexer
// — the binding has footguns on two axes and they are mirror opposites depending on the API path:
//
//   H3.Model.LatLng             is (lat, lon) in RADIANS
//   NetTopologySuite.Coordinate is (lon, lat) = (X, Y) in DEGREES
//
// Everything Dispatch holds is degrees (the proto's lat/lon, Location.Lat/Lon), so every path here
// goes through Coordinate and LatLng.FromCoordinate, which is degrees-native. That removes the unit
// conversion entirely and collapses the risk to argument ORDER, which H3KRingTests pins.
//
// DO NOT reach for `new LatLng(lat, lon)`. That constructor takes radians, and passing degrees is a
// ~57x scale error that still produces a plausible-looking distance rather than an exception.
public static class H3KRing
{
    // Metres covered per ring, as a multiple of the edge length. This is 1.5, and the derivation
    // matters because the obvious answer is wrong in the dangerous direction.
    //
    // Centre-to-centre spacing between adjacent hexagons is edge x sqrt(3) — but that is the
    // distance gained per hop only when travelling ALONG a lattice axis. The axes are 60 degrees
    // apart, so a bearing that falls between two of them advances less per hop, in the worst case
    // by a factor of cos(30) = sqrt(3)/2. Dividing a radius by the full axis spacing therefore
    // UNDER-estimates k, and under-estimating k silently drops drivers near the edge of the search
    // radius — a wrong dispatch decision with no error anywhere.
    //
    //   worst-case metres per ring = edge x sqrt(3) x sqrt(3)/2 = edge x 1.5
    //
    // Sanity check on the numbers, because two different "edge lengths" are in circulation:
    // pocketken's GetHexagonEdgeLengthAverageInM returns the edge of a REGULAR hexagon with the
    // average cell area (~201m at resolution 9). H3's published tables instead list ~174m, the
    // average of the actual distorted edges. Those two differ by exactly this sqrt(3)/2, so
    // 201 x 1.5 = 302m reproduces the published figure — and 302m is what the grid empirically
    // advances per ring. H3KRingTests pins the resulting k values against measured grid distances.
    private const double MetersPerRingPerEdge = 1.5;

    // ~3,700 cells at the boundary (3k(k+1)+1). Comfortably above anything a sane
    // resolution/radius pairing produces — resolution 9 at 5km needs 18 — and far below the point
    // where a single `= ANY` array becomes the problem. See DeriveK for why this refuses rather
    // than clamps.
    private const int MaxRingRadius = 35;

    // Computes the H3 cell containing a point, at the given resolution. Same Coordinate path as
    // Telemetry's H3CellIndexer, and it must stay the same path: a cell id computed differently
    // here would not match the ids Telemetry publishes, and the join would silently return nothing.
    public static string? TryComputeCell(double latDegrees, double lonDegrees, int resolution)
    {
        // X = lon FIRST, Y = lat second. This argument order is the whole footgun.
        var cell = new Coordinate(lonDegrees, latDegrees).ToH3Index(resolution);

        return cell.IsValidCell ? cell.ToString() : null;
    }

    // Every cell id within `radiusMeters` of the origin cell, as a coarse over-approximation.
    //
    // Deliberately over-covers. k is rounded UP and then widened by one further ring, because the
    // two error directions are not symmetric: over-covering costs a longer array in a single
    // `= ANY(...)` parameter and is then corrected by the exact-distance filter that runs after,
    // while under-covering silently drops drivers who really are in range and produces a wrong
    // dispatch decision with no signal. Cheap insurance against boundary arithmetic.
    //
    // Scale note for anyone tuning this: at resolution 9 a 5km radius yields k=16 and roughly 800
    // cells. That is affordable specifically because Marten translates Contains() to `= ANY(:param)`
    // — ONE array parameter — rather than to an N-term IN list.
    public static IReadOnlyList<string> CellsWithin(string originCell, int resolution, int radiusMeters)
    {
        var origin = new H3Index(originCell);

        if (!origin.IsValidCell)
            return [];

        var k = DeriveK(resolution, radiusMeters);

        // GridDiskDistances, not the older GetKRing — GetKRing is [Obsolete] as of H3 4.0 and this
        // is its named replacement. The per-cell Distance it also returns is the RING index (how
        // many hops out), not metres, so it is no substitute for the great-circle filter the caller
        // applies afterward; discarded here.
        //
        // Cell ids are stringified for comparison against the h3_cell column, where they live as
        // the same opaque strings Telemetry published. ToString() is nullable-typed, so the null
        // filter is a compiler requirement rather than a real case.
        return Rings.GridDiskDistances(origin, k)
            .Select(ring => ring.Index.ToString())
            .Where(cell => cell is not null)
            .Select(cell => cell!)
            .ToList();
    }

    // The ring count needed to cover a radius at a given resolution, over-approximated by one ring.
    // Internal rather than private so the pinning tests can assert the arithmetic directly instead
    // of inferring it from cell counts.
    internal static int DeriveK(int resolution, int radiusMeters)
    {
        if (radiusMeters <= 0)
            return 0;

        var edgeMeters = H3Index.GetHexagonEdgeLengthAverageInM(resolution);

        // Guard against a resolution H3 does not know: it returns 0 or NaN rather than throwing,
        // and dividing by it would yield an infinite k that GetKRing would try to enumerate.
        if (double.IsNaN(edgeMeters) || edgeMeters <= 0)
            return 0;

        var metersPerRing = edgeMeters * MetersPerRingPerEdge;
        var k = (int)Math.Ceiling(radiusMeters / metersPerRing) + 1;

        // A grid disk holds 3k(k+1)+1 cells, so k grows the array quadratically — and k itself is
        // driven by a resolution Dispatch does not control (it is read off whatever Telemetry last
        // published). At resolution 9 a 5km radius is k=18 and ~1,000 cells; at resolution 12 the
        // same radius is k≈369 and ~410,000 cells, which would be materialised into a single
        // `= ANY` parameter and would take the query down rather than return slowly.
        //
        // Refuse loudly instead of degrading. This is a misconfiguration — a telemetry resolution
        // that fine is not a tuning choice, it is a mistake — and an exception names it at the one
        // moment someone can act on it. Returning a truncated k would silently under-cover, which
        // is the failure mode this whole method is written to avoid.
        if (k > MaxRingRadius)
        {
            throw new InvalidOperationException(
                $"An H3 k-ring of {k} rings is required to cover {radiusMeters}m at resolution "
                + $"{resolution}, which exceeds the {MaxRingRadius}-ring ceiling. The cell "
                + "resolution published by Telemetry is too fine for this search radius — widen "
                + "the TelemetryPolicy resolution or narrow the radius.");
        }

        return k;
    }

    // Great-circle distance in metres between two degree-denominated points. Used for the exact
    // filter that trims the k-ring's deliberate over-approximation back to the true radius.
    public static double DistanceMeters(
        double fromLatDegrees, double fromLonDegrees,
        double toLatDegrees, double toLonDegrees)
    {
        // X = lon, Y = lat — again. FromCoordinate handles the degree-to-radian conversion that the
        // LatLng constructor does not.
        var from = LatLng.FromCoordinate(new Coordinate(fromLonDegrees, fromLatDegrees));
        var to = LatLng.FromCoordinate(new Coordinate(toLonDegrees, toLatDegrees));

        return from.GetGreatCircleDistanceInMeters(to);
    }
}
