using System.Net;
using Alba;
using CritterCab.Dispatch.CandidateSelection;
using CritterCab.Dispatch.FareQuoting;
using CritterCab.Dispatch.RideRequesting;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Wolverine.Tracking;
using Xunit;

namespace CritterCab.Dispatch.Tests.CandidateSelection;

[Collection("Dispatch")]
public class Slice53CandidatesSelectedTests : IDisposable
{
    private readonly DispatchTestFixture _fixture;
    private readonly IAlbaHost _host;

    public Slice53CandidatesSelectedTests(DispatchTestFixture fixture)
    {
        _fixture = fixture;
        _host = fixture.Host;
    }

    public void Dispose() =>
        _fixture.NearbyDriversSource = new NearbyAvailableDriversStub();

    [Fact]
    public async Task candidates_selected_picks_top_five_ordered_by_match_score()
    {
        // W001 §5.3 GWT 1: 6 STANDARD drivers; policy maxCandidatesPerRound = 5 → 6th excluded.
        // Match score = 1 / distanceMeters, so closest driver gets highest score.
        _fixture.NearbyDriversSource = new NearbyAvailableDriversStub
        {
            Drivers = new[]
            {
                new NearbyDriver(Guid.CreateVersion7(), DistanceMeters: 300,  EtaSeconds: 60,  VehicleClass.Standard),
                new NearbyDriver(Guid.CreateVersion7(), DistanceMeters: 500,  EtaSeconds: 90,  VehicleClass.Standard),
                new NearbyDriver(Guid.CreateVersion7(), DistanceMeters: 800,  EtaSeconds: 120, VehicleClass.Standard),
                new NearbyDriver(Guid.CreateVersion7(), DistanceMeters: 1200, EtaSeconds: 180, VehicleClass.Standard),
                new NearbyDriver(Guid.CreateVersion7(), DistanceMeters: 1500, EtaSeconds: 240, VehicleClass.Standard),
                new NearbyDriver(Guid.CreateVersion7(), DistanceMeters: 2000, EtaSeconds: 300, VehicleClass.Standard),
            }
        };

        var response = await SubmitRideAndAwaitOutcome(VehicleClass.Standard);

        await using var session = _host.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        var events = await session.Events.FetchStreamAsync(response.RideRequestId);

        events.Count.ShouldBe(3);
        events[0].Data.ShouldBeOfType<RideRequested>();
        events[1].Data.ShouldBeOfType<FareQuoted>();

        var selected = events[2].Data.ShouldBeOfType<CandidatesSelected>();
        selected.RideRequestId.ShouldBe(response.RideRequestId);
        selected.RoundNumber.ShouldBe(1);
        selected.DispatchPolicyVersion.ShouldBe("default-v1");

        // Exactly 5 candidates; 6th driver (2000m) excluded.
        selected.Candidates.Count.ShouldBe(5);

        // Ordered by matchScore desc = ascending distance.
        var distances = selected.Candidates.Select(c => c.DistanceMeters).ToArray();
        distances.ShouldBe(new[] { 300, 500, 800, 1200, 1500 });
        selected.Candidates[0].MatchScore.ShouldBeGreaterThan(selected.Candidates[1].MatchScore);

        var rounds = await session.LoadAsync<RequestRounds>(response.RideRequestId);
        rounds.ShouldNotBeNull();
        rounds.Rounds.Count.ShouldBe(1);
        rounds.Rounds[0].RoundNumber.ShouldBe(1);
        rounds.Rounds[0].Outcome.ShouldBe(nameof(CandidatesSelected));
        rounds.Rounds[0].NoCandidatesReason.ShouldBeNull();

        var timeline = await session.LoadAsync<RequestTimeline>(response.RideRequestId);
        timeline.ShouldNotBeNull();
        timeline.Entries.Count.ShouldBe(3);
        timeline.Entries[2].EventType.ShouldBe(nameof(CandidatesSelected));
    }

    [Fact]
    public async Task no_drivers_in_range_emits_no_candidates_with_no_drivers_reason()
    {
        // W001 §5.3 GWT 2: empty stub → NoCandidatesAvailable { reason: NoDriversInRange }.
        // NearbyDriversSource defaults to NearbyAvailableDriversStub with Drivers = [].

        var response = await SubmitRideAndAwaitOutcome(VehicleClass.Standard);

        await using var session = _host.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        var events = await session.Events.FetchStreamAsync(response.RideRequestId);

        events.Count.ShouldBe(3);
        events[0].Data.ShouldBeOfType<RideRequested>();
        events[1].Data.ShouldBeOfType<FareQuoted>();

        var noCandidates = events[2].Data.ShouldBeOfType<NoCandidatesAvailable>();
        noCandidates.RideRequestId.ShouldBe(response.RideRequestId);
        noCandidates.RoundNumber.ShouldBe(1);
        noCandidates.Reason.ShouldBe(NoCandidatesReason.NoDriversInRange);

        var rounds = await session.LoadAsync<RequestRounds>(response.RideRequestId);
        rounds.ShouldNotBeNull();
        rounds.Rounds.Count.ShouldBe(1);
        rounds.Rounds[0].Outcome.ShouldBe(nameof(NoCandidatesAvailable));
        rounds.Rounds[0].NoCandidatesReason.ShouldBe(nameof(NoCandidatesReason.NoDriversInRange));

        var timeline = await session.LoadAsync<RequestTimeline>(response.RideRequestId);
        timeline.ShouldNotBeNull();
        timeline.Entries.Count.ShouldBe(3);
        timeline.Entries[2].EventType.ShouldBe(nameof(NoCandidatesAvailable));
    }

    [Fact]
    public async Task accessible_request_with_only_standard_drivers_emits_no_capable_drivers_reason()
    {
        // W001 §5.3 GWT 3: 8 STANDARD drivers in range, 0 ACCESSIBLE → NoCapableDriversInRange.
        // Automation receives all drivers from the stub (stub ignores vehicleClassRequired),
        // then filters in-process: allDrivers > 0 but eligible (ACCESSIBLE) == 0.
        _fixture.NearbyDriversSource = new NearbyAvailableDriversStub
        {
            Drivers = Enumerable.Range(1, 8)
                .Select(i => new NearbyDriver(Guid.CreateVersion7(), i * 300, i * 60, VehicleClass.Standard))
                .ToArray()
        };

        var response = await SubmitRideAndAwaitOutcome(VehicleClass.Accessible);

        await using var session = _host.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        var events = await session.Events.FetchStreamAsync(response.RideRequestId);

        events.Count.ShouldBe(3);
        var noCandidates = events[2].Data.ShouldBeOfType<NoCandidatesAvailable>();
        noCandidates.RideRequestId.ShouldBe(response.RideRequestId);
        noCandidates.RoundNumber.ShouldBe(1);
        noCandidates.Reason.ShouldBe(NoCandidatesReason.NoCapableDriversInRange);

        var rounds = await session.LoadAsync<RequestRounds>(response.RideRequestId);
        rounds.ShouldNotBeNull();
        rounds.Rounds.Count.ShouldBe(1);
        rounds.Rounds[0].Outcome.ShouldBe(nameof(NoCandidatesAvailable));
        rounds.Rounds[0].NoCandidatesReason.ShouldBe(nameof(NoCandidatesReason.NoCapableDriversInRange));

        var timeline = await session.LoadAsync<RequestTimeline>(response.RideRequestId);
        timeline.ShouldNotBeNull();
        timeline.Entries.Count.ShouldBe(3);
        timeline.Entries[2].EventType.ShouldBe(nameof(NoCandidatesAvailable));
    }

    private async Task<RideRequestResponse> SubmitRideAndAwaitOutcome(VehicleClass vehicleClass)
    {
        var riderId = Guid.CreateVersion7();
        var command = new SubmitRideRequest(
            RiderId: riderId,
            Pickup: new Location(40.7128, -74.0060, "123 Main St"),
            Dropoff: new Location(40.7580, -73.9855, "Eastbridge Library"),
            VehicleClass: vehicleClass,
            NotesForDriver: null);

        IScenarioResult httpResult = null!;
        await _host.ExecuteAndWaitAsync(async () =>
        {
            httpResult = await _host.Scenario(s =>
            {
                s.Post.Json(command).ToUrl("/api/rides/request");
                s.StatusCodeShouldBe(HttpStatusCode.Created);
            });
        });

        var response = httpResult.ReadAsJson<RideRequestResponse>();
        response.ShouldNotBeNull();
        return response;
    }
}
