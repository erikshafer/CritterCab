using CritterCab.Dispatch.AvailableDrivers;
using Shouldly;
using Xunit;

namespace CritterCab.Dispatch.Tests.AvailableDrivers;

// Pinning tests for the H3 binding, in the same spirit as Telemetry's H3CellIndexerTests and for
// the same reason: every failure mode in this file is SILENT. A swapped lat/lon, a degrees-for-
// radians mix-up, or a k that is one ring too small all produce plausible-looking output — a valid
// cell id, a finite distance, a non-empty ring — and the only symptom downstream is a dispatch that
// quietly finds no drivers.
//
// No broker and no database: these are pure functions.
public class H3KRingTests
{
    // Chicago's Loop. Positive latitude, NEGATIVE longitude — chosen deliberately, because the sign
    // asymmetry is what makes an axis swap detectable at all.
    private const double LoopLat = 41.8827d, LoopLon = -87.6233d;

    [Fact]
    public void a_cell_matches_the_one_telemetry_would_publish_for_the_same_point()
    {
        // The literal is the pin. Telemetry computes the same value through H3CellIndexer, and the
        // join between the two services is string equality on exactly this — if either side's
        // binding drifts, the k-ring stops matching stored cells and the view silently empties.
        H3KRing.TryComputeCell(LoopLat, LoopLon, 9).ShouldBe("892664c1a97ffff");
    }

    [Fact]
    public void arguments_are_lat_then_lon_not_x_then_y()
    {
        // The swapped point is in the Southern Ocean off Antarctica; it is still a VALID cell, which
        // is precisely why the mistake survives a smoke test.
        var correct = H3KRing.TryComputeCell(LoopLat, LoopLon, 9);
        var swapped = H3KRing.TryComputeCell(LoopLon, LoopLat, 9);

        correct.ShouldNotBe(swapped);
    }

    [Fact]
    public void an_out_of_range_resolution_yields_null_rather_than_throwing()
    {
        // H3 signals bad input by returning an invalid index, never by throwing — so "invalid" is a
        // value to branch on. Matches H3CellIndexer's contract on the Telemetry side.
        H3KRing.TryComputeCell(LoopLat, LoopLon, 42).ShouldBeNull();
    }

    [Fact]
    public void distance_is_in_metres_and_degree_denominated()
    {
        // Roughly 1 degree of latitude ~ 111km. If the inputs were being read as radians this would
        // come back off by a factor of ~57, so the assertion is a units check as much as a
        // distance check.
        var meters = H3KRing.DistanceMeters(LoopLat, LoopLon, LoopLat + 1.0d, LoopLon);

        meters.ShouldBeInRange(110_000d, 112_000d);
    }

    [Fact]
    public void distance_to_the_same_point_is_zero()
    {
        H3KRing.DistanceMeters(LoopLat, LoopLon, LoopLat, LoopLon).ShouldBeLessThan(0.001d);
    }

    [Theory]
    // A ring advances edge x 1.5 metres in the worst case, not edge x sqrt(3) — see H3KRing for
    // why the axis-aligned spacing over-states the coverage. At resolution 9 that is ~302m.
    //
    // These numbers are pinned against MEASURED grid distances, not against the formula: a point
    // 4982m due north of the Loop sits at grid distance 17, so k=17 is the true requirement at 5km
    // and the +1 is real margin on top. The earlier sqrt(3) form produced k=16 here and silently
    // excluded that point.
    [InlineData(9, 1_000, 5)]   // ceil(1000/302) = 4, +1
    [InlineData(9, 5_000, 18)]  // ceil(5000/302) = 17, +1
    [InlineData(8, 5_000, 8)]   // coarser cells (~797m per ring), fewer rings
    public void k_covers_the_radius_with_one_ring_to_spare(int resolution, int radius, int expectedK)
    {
        H3KRing.DeriveK(resolution, radius).ShouldBe(expectedK);
    }

    [Theory]
    // The real invariant behind DeriveK, asserted end to end rather than through the arithmetic: a
    // point at the very edge of the radius must fall in a cell the ring contains. This is what
    // would fail if the sqrt(3) spacing factor were "simplified" away, or if the edge-length basis
    // were swapped for the smaller documented figure.
    //
    // 5000m is DispatchPolicySnapshot.Default's production search radius, so that row is the one
    // that actually protects live dispatch. ~0.009 degrees of latitude is ~1000m due north.
    [InlineData(1_000, 0.0089d)]
    [InlineData(5_000, 0.0448d)]
    public void the_ring_actually_reaches_the_radius_it_claims_to(int radius, double latOffset)
    {
        var originCell = H3KRing.TryComputeCell(LoopLat, LoopLon, 9)!;
        var cells = H3KRing.CellsWithin(originCell, 9, radius);

        var edgeCell = H3KRing.TryComputeCell(LoopLat + latOffset, LoopLon, 9)!;

        cells.ShouldContain(edgeCell);
    }

    [Fact]
    public void the_ring_always_contains_its_own_origin()
    {
        var originCell = H3KRing.TryComputeCell(LoopLat, LoopLon, 9)!;

        H3KRing.CellsWithin(originCell, 9, 500).ShouldContain(originCell);
    }

    [Fact]
    public void an_unparseable_origin_yields_an_empty_ring_rather_than_throwing()
    {
        H3KRing.CellsWithin("not-a-cell", 9, 1_000).ShouldBeEmpty();
    }
}
