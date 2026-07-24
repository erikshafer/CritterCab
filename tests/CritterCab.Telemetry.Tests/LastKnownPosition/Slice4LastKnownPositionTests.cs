using Alba;
using CritterCab.Telemetry.LastKnownPosition;
using CritterCab.Telemetry.TelemetryPolicy;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;
using LastKnownPositionDocument = global::CritterCab.Telemetry.LastKnownPosition.LastKnownPosition;

namespace CritterCab.Telemetry.Tests.LastKnownPosition;

// W006 §6.4 GWTs for the LastKnownPosition store and the heartbeat-absence eviction sweep.
// CritterCab's first tests over a non-event-sourced document, and its first Wolverine message
// invocation from a test.
//
// The §6.4 "No-write" GWT (ping accepted, shouldPublish = false, document unchanged) is NOT here:
// deciding *not* to write is slice 2's publish trigger, which lands in the next chunk. What is
// testable now is the store's own contract — overwrite-in-place, a policy-derived eviction
// threshold, and the absent-baseline state a returning driver finds.
[Collection("Telemetry")]
public class Slice4LastKnownPositionTests
{
    // Two adjacent H3 resolution-9 cells over Chicago. Opaque string literals on purpose: slice 4
    // only ever compares and stores cells, so nothing here should depend on H3 arithmetic. The
    // real cell computation (and its lat/lon axis-order trap) belongs to slice 2.
    private const string CellC1 = "8a2a1072b59ffff";
    private const string CellC2 = "8a2a1072b5affff";

    private readonly TelemetryTestFixture _fixture;

    public Slice4LastKnownPositionTests(TelemetryTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task an_upsert_overwrites_the_drivers_document_in_place()
    {
        // Given driver D has a stored position at cell C1
        await _fixture.ResetPositionsAsync();
        var driverId = Guid.CreateVersion7();
        await StoreAsync(PositionFor(driverId, CellC1, DateTimeOffset.UtcNow.AddSeconds(-10)));

        // When a later publish stores D's position at cell C2
        var secondPublish = DateTimeOffset.UtcNow;
        await StoreAsync(PositionFor(driverId, CellC2, secondPublish));

        // Then the row is replaced rather than added to — one document per driver, LWW on
        // ServerReceivedAt, no history retained (this is a document, not a stream)
        var stored = await LoadAsync(driverId);
        stored.ShouldNotBeNull();
        stored.H3Cell.ShouldBe(CellC2);
        stored.ServerReceivedAt.ShouldBe(secondPublish, TimeSpan.FromMilliseconds(1));

        (await CountPositionsAsync(driverId)).ShouldBe(1);
    }

    [Fact]
    public async Task the_sweep_evicts_a_position_older_than_three_heartbeats()
    {
        // Given the seeded policy (heartbeat 30s, so the eviction threshold is 90s)
        await _fixture.ResetToSeedAsync();
        await _fixture.ResetPositionsAsync();

        // And driver D last published five minutes ago
        var driverId = Guid.CreateVersion7();
        await StoreAsync(PositionFor(driverId, CellC1, DateTimeOffset.UtcNow.AddMinutes(-5)));

        // When the periodic sweep runs
        await _fixture.InvokeAsync(new EvictStalePositions());

        // Then D's document is deleted
        (await LoadAsync(driverId)).ShouldBeNull();

        // And no staleness event is published (v1, R3/R8) — eviction is storage hygiene only, so
        // the event store still holds nothing but the single bootstrap seed event
        (await CountAllEventsAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task the_sweep_leaves_a_position_inside_the_threshold_untouched()
    {
        // Given the seeded policy (threshold 90s), a stale driver and a fresh one
        await _fixture.ResetToSeedAsync();
        await _fixture.ResetPositionsAsync();

        var staleDriver = Guid.CreateVersion7();
        var freshDriver = Guid.CreateVersion7();
        await StoreAsync(
            PositionFor(staleDriver, CellC1, DateTimeOffset.UtcNow.AddMinutes(-5)),
            PositionFor(freshDriver, CellC2, DateTimeOffset.UtcNow.AddSeconds(-10)));

        // When the sweep runs
        await _fixture.InvokeAsync(new EvictStalePositions());

        // Then only the stale driver is evicted — the sweep is threshold-driven, not a blanket wipe
        (await LoadAsync(staleDriver)).ShouldBeNull();
        (await LoadAsync(freshDriver)).ShouldNotBeNull();
    }

    [Fact]
    public async Task a_returning_driver_finds_no_baseline_and_re_establishes_one()
    {
        // Given driver D was evicted by the sweep
        await _fixture.ResetToSeedAsync();
        await _fixture.ResetPositionsAsync();

        var driverId = Guid.CreateVersion7();
        await StoreAsync(PositionFor(driverId, CellC1, DateTimeOffset.UtcNow.AddMinutes(-5)));
        await _fixture.InvokeAsync(new EvictStalePositions());

        // When D pings again, the slice-2 trigger looks up D's baseline and finds none. That
        // absent baseline is precisely what makes shouldPublish true on a returning driver's first
        // ping (§6.4) — the trigger half of this GWT lands with slice 2.
        (await LoadAsync(driverId)).ShouldBeNull();

        // And when that immediate republish stores a position, D has a fresh baseline again
        var returnPublish = DateTimeOffset.UtcNow;
        await StoreAsync(PositionFor(driverId, CellC2, returnPublish));

        var baseline = await LoadAsync(driverId);
        baseline.ShouldNotBeNull();
        baseline.H3Cell.ShouldBe(CellC2);
        baseline.ServerReceivedAt.ShouldBe(returnPublish, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task the_eviction_threshold_follows_the_configured_heartbeat_interval()
    {
        // Given the policy is reconfigured to a 1-second heartbeat, so the threshold drops to 3s
        await _fixture.ResetToSeedAsync();
        await _fixture.ResetPositionsAsync();

        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new ConfigureTelemetryPolicy(
                H3Resolution: 9,
                HeartbeatIntervalSeconds: 1,
                MinPublishIntervalSeconds: 1,
                OperatorId: "ops-alice",
                Reason: "Tight heartbeat")).ToUrl("/api/telemetry/policy");
            s.StatusCodeShouldBeOk();
        });

        // And a position 10 seconds old — comfortably fresh under the seeded 90s threshold, and
        // the exact age that survives in the test above
        var driverId = Guid.CreateVersion7();
        await StoreAsync(PositionFor(driverId, CellC1, DateTimeOffset.UtcNow.AddSeconds(-10)));

        // When the sweep runs
        await _fixture.InvokeAsync(new EvictStalePositions());

        // Then it is evicted: the threshold is read from the policy view each sweep, not hardcoded
        (await LoadAsync(driverId)).ShouldBeNull();
    }

    [Fact]
    public async Task the_host_starts_with_the_eviction_timer_registered()
    {
        // The fixture host strips the eviction BackgroundService for determinism, and the smoke
        // test runs without a connection string (so the registration is skipped with the whole
        // Marten block). Without this test nothing would cover the production wiring at all.
        //
        // It matters because a BackgroundService is a SINGLETON: every constructor dependency has
        // to resolve from the root provider. Injecting Wolverine's IMessageBus directly would not
        // — it is registered scoped (Wolverine HostBuilderExtensions.cs:232) — which is exactly why
        // LastKnownPositionEvictionService takes an IServiceScopeFactory and opens a scope per tick.
        // Resolving the hosted services here forces construction, so a regression on that shape
        // fails here rather than at deploy time.
        await using var host = await AlbaHost.For<Program>(builder =>
            builder.UseSetting("ConnectionStrings:crittercab_telemetry", _fixture.ConnectionString));

        host.Services.GetServices<IHostedService>()
            .OfType<LastKnownPositionEvictionService>()
            .ShouldHaveSingleItem();
    }

    private static LastKnownPositionDocument PositionFor(
        Guid driverId,
        string h3Cell,
        DateTimeOffset serverReceivedAt) =>
        new()
        {
            Id = driverId,
            Lat = 41.8781,
            Lon = -87.6298,
            H3Cell = h3Cell,
            ServerReceivedAt = serverReceivedAt
        };

    private async Task StoreAsync(params LastKnownPositionDocument[] positions)
    {
        var store = _fixture.Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        session.Store(positions);
        await session.SaveChangesAsync();
    }

    private async Task<LastKnownPositionDocument?> LoadAsync(Guid driverId)
    {
        var store = _fixture.Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.QuerySession();
        return await session.LoadAsync<LastKnownPositionDocument>(driverId);
    }

    private async Task<int> CountPositionsAsync(Guid driverId)
    {
        var store = _fixture.Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.QuerySession();
        return await session.Query<LastKnownPositionDocument>().CountAsync(x => x.Id == driverId);
    }

    private async Task<int> CountAllEventsAsync()
    {
        var store = _fixture.Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.QuerySession();
        return await session.Events.QueryAllRawEvents().CountAsync();
    }
}
