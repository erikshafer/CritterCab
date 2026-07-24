using CritterCab.Telemetry.ReportLocations;
using H3;
using H3.Extensions;
using H3.Model;
using NetTopologySuite.Geometries;
using Shouldly;
using Xunit;

namespace CritterCab.Telemetry.Tests.ReportLocations;

// Pins down the H3 binding's axis order AND units together. A naive "the cell is valid" assertion
// would pass with lat and lon swapped, and would also pass with degrees fed into a radians API —
// both produce a perfectly well-formed cell for the wrong place on Earth. These tests are
// therefore built to fail on either mistake.
//
// Pure unit tests: no host, no container. H3 is a pure function.
public class H3CellIndexerTests
{
    // Chicago, the Art Institute.
    private const double LatDegrees = 41.8781d;
    private const double LonDegrees = -87.6298d;
    private const int Resolution = 9;

    [Fact]
    public void the_degrees_coordinate_path_agrees_with_the_radians_latlng_path()
    {
        // The production path: NetTopologySuite Coordinate is (X, Y) = (lon, lat) in DEGREES.
        var viaCoordinate = new Coordinate(LonDegrees, LatDegrees).ToH3Index(Resolution);

        // The other path, with the conversion done by hand: H3's own LatLng is (lat, lon) in
        // RADIANS. The two paths are mirror opposites on BOTH axes, so they can only agree if
        // production has the order and the units right — two wrongs cannot accidentally cancel.
        var viaLatLng = H3Index.FromLatLng(
            new LatLng(LatDegrees * Math.PI / 180d, LonDegrees * Math.PI / 180d),
            Resolution);

        viaCoordinate.ShouldBe(viaLatLng);
        viaCoordinate.IsValidCell.ShouldBeTrue();
    }

    [Fact]
    public void swapping_the_axes_yields_a_different_cell()
    {
        // This is what makes the test above meaningful: a swapped Coordinate still produces a
        // valid cell, just one somewhere off the coast of Somalia. Validity alone proves nothing.
        var correct = new Coordinate(LonDegrees, LatDegrees).ToH3Index(Resolution);
        var swapped = new Coordinate(LatDegrees, LonDegrees).ToH3Index(Resolution);

        swapped.ShouldNotBe(correct);
    }

    [Fact]
    public void the_indexer_returns_the_cell_computed_by_the_degrees_path()
    {
        var expected = new Coordinate(LonDegrees, LatDegrees).ToH3Index(Resolution);

        H3CellIndexer.TryComputeCell(LatDegrees, LonDegrees, Resolution)
            .ShouldBe(expected.ToString());
    }

    [Fact]
    public void an_out_of_range_resolution_yields_no_cell_rather_than_throwing()
    {
        // H3 signals bad input by returning H3Index.Invalid, never by throwing — so the indexer
        // branches on it instead of catching. 15 is MAX_H3_RES, which is also the upper bound
        // slice 1's policy validator enforces.
        H3CellIndexer.TryComputeCell(LatDegrees, LonDegrees, 16).ShouldBeNull();
    }

    [Fact]
    public void a_non_finite_coordinate_yields_no_cell()
    {
        // The second safety net behind per-ping validation: even if a NaN slipped through, the
        // ping is dropped rather than indexed to nowhere.
        H3CellIndexer.TryComputeCell(double.NaN, LonDegrees, Resolution).ShouldBeNull();
    }
}
