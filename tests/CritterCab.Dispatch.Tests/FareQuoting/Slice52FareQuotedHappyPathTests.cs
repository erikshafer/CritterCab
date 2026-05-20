using System.Net;
using Alba;
using CritterCab.Dispatch.FareQuoting;
using CritterCab.Dispatch.RideRequesting;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Wolverine.Tracking;
using Xunit;

namespace CritterCab.Dispatch.Tests.FareQuoting;

[Collection("Dispatch")]
public class Slice52FareQuotedHappyPathTests
{
    private readonly DispatchTestFixture _fixture;
    private readonly IAlbaHost _host;

    public Slice52FareQuotedHappyPathTests(DispatchTestFixture fixture)
    {
        _fixture = fixture;
        _host = fixture.Host;
    }

    [Fact]
    public async Task fare_quote_automation_records_fare_quoted_on_stream()
    {
        var riderId = Guid.CreateVersion7();
        var command = new SubmitRideRequest(
            RiderId: riderId,
            Pickup: new Location(40.7128, -74.0060, "123 Main St"),
            Dropoff: new Location(40.7580, -73.9855, "Eastbridge Library"),
            VehicleClass: VehicleClass.Standard,
            NotesForDriver: "meet at side entrance");

        IScenarioResult httpResult = null!;
        var tracked = await _host.ExecuteAndWaitAsync(async () =>
        {
            httpResult = await _host.Scenario(s =>
            {
                s.Post.Json(command).ToUrl("/api/rides/request");
                s.StatusCodeShouldBe(HttpStatusCode.Created);
            });
        });

        var response = httpResult.ReadAsJson<RideRequestResponse>();
        response.ShouldNotBeNull();

        await using var session = _host.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        var events = await session.Events.FetchStreamAsync(response.RideRequestId);

        events.Count.ShouldBe(2);
        events[0].Data.ShouldBeOfType<RideRequested>();

        var fareQuoted = events[1].Data.ShouldBeOfType<FareQuoted>();
        fareQuoted.RideRequestId.ShouldBe(response.RideRequestId);
        fareQuoted.FareAmountMinorUnits.ShouldBe(2150L);
        fareQuoted.Currency.ShouldBe("USD");
        fareQuoted.VehicleClass.ShouldBe(VehicleClass.Standard);
        fareQuoted.FareBreakdown.BaseMinorUnits.ShouldBeGreaterThan(0);
        fareQuoted.FareBreakdown.DistanceMinorUnits.ShouldBeGreaterThan(0);
        fareQuoted.FareBreakdown.TimeMinorUnits.ShouldBeGreaterThan(0);
        fareQuoted.FareBreakdown.FeesMinorUnits.ShouldBeNull();
        fareQuoted.PricingPolicyVersion.ShouldNotBeNullOrWhiteSpace();

        var timeline = await session.LoadAsync<RequestTimeline>(response.RideRequestId);
        timeline.ShouldNotBeNull();
        timeline.Entries.Count.ShouldBe(2);
        timeline.Entries[0].EventType.ShouldBe(nameof(RideRequested));
        timeline.Entries[1].EventType.ShouldBe(nameof(FareQuoted));
        timeline.Entries[1].Summary.ShouldContain("$21.50");
    }
}
