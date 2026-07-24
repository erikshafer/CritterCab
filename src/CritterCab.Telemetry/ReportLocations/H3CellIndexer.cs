using H3;
using H3.Extensions;
using NetTopologySuite.Geometries;

namespace CritterCab.Telemetry.ReportLocations;

// Wraps the single line of H3 binding that computes a cell from a ping (W006 §6.2 step 3).
// It is wrapped rather than inlined because that line has a footgun on BOTH axes, and the two
// available API paths are mirror opposites on each:
//
//   H3.Model.LatLng           is (lat, lon) in RADIANS
//   NetTopologySuite.Coordinate is (lon, lat) = (X, Y) in DEGREES
//
// The proto carries lat/lon as degrees, so the Coordinate path is degrees-native and removes the
// unit conversion entirely — collapsing the risk to one axis-order line, which the pinning test
// in H3CellIndexerTests locks down. NetTopologySuite is pulled in by pocketken.H3 itself, so this
// path costs no additional dependency.
//
// DO NOT switch this to H3Index.FromLatLng via its (double, double) tuple overload. The tuple's
// implicit conversion to LatLng performs NO degree-to-radian conversion, so passing degrees is a
// ~57x scale error that still yields a valid-LOOKING cell. (The library's own prose claims the
// tuple accepts degrees; the code disagrees, and the code wins.)
public static class H3CellIndexer
{
    // Returns null rather than throwing when the cell cannot be computed. H3 signals bad input by
    // returning H3Index.Invalid — an out-of-range resolution or a non-finite coordinate — and
    // never throws, so "invalid" is a value to branch on, not an exception to catch. A valid
    // TelemetryPolicy keeps the resolution in range, which makes this a second safety net behind
    // per-ping validation rather than the primary guard. Callers treat null as the silent-drop
    // path (§6.2: invalid pings are dropped, not errored).
    public static string? TryComputeCell(double latDegrees, double lonDegrees, int resolution)
    {
        // X = lon FIRST, Y = lat second. This argument order is the whole footgun.
        var cell = new Coordinate(lonDegrees, latDegrees).ToH3Index(resolution);

        return cell.IsValidCell ? cell.ToString() : null;
    }
}
