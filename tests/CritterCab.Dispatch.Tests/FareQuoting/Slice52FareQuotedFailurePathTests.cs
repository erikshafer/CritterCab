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
public class Slice52FareQuotedFailurePathTests : IDisposable
{
    private readonly DispatchTestFixture _fixture;
    private readonly IAlbaHost _host;

    public Slice52FareQuotedFailurePathTests(DispatchTestFixture fixture)
    {
        _fixture = fixture;
        _host = fixture.Host;
    }

    public void Dispose() =>
        // Leave the fixture's pricing client in a non-failing state so test
        // classes that don't arrange a stub (e.g., SubmitRideRequestTests) see
        // the default canned response.
        _fixture.PricingClient = new PricingClientStub();

    [Fact]
    public async Task transient_failure_with_retry_recovery_emits_fare_quoted_only()
    {
        // W001 §5.2 GWT: first attempt times out (transient), second succeeds
        // → FareQuoted emitted, no FareQuoteFailed.
        _fixture.PricingClient = new PricingClientStub(attempt => attempt switch
        {
            1 => throw new TransientPricingException("simulated pricing timeout"),
            _ => DefaultPricingResponse()
        });

        var response = await SubmitRideAndAwaitOutcome();

        await using var session = _host.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        var events = await session.Events.FetchStreamAsync(response.RideRequestId);

        events.Count.ShouldBe(2);
        events[0].Data.ShouldBeOfType<RideRequested>();
        events[1].Data.ShouldBeOfType<FareQuoted>();

        var timeline = await session.LoadAsync<RequestTimeline>(response.RideRequestId);
        timeline.ShouldNotBeNull();
        timeline.Entries.Count.ShouldBe(2);
        timeline.Entries[1].EventType.ShouldBe(nameof(FareQuoted));

        // FareQuoteAttempts records the terminal outcome; the in-flight attempt
        // count lives in the handler's loop variable. Per W001 §5.2's "Retry
        // attempts themselves are NOT emitted as events" decision, the
        // projection's AttemptCount for a successful quote is the natural floor
        // (1), not the actual retry count (2).
        var attempts = await session.LoadAsync<FareQuoteAttempts>(response.RideRequestId);
        attempts.ShouldNotBeNull();
        attempts.Outcome.ShouldBe(FareQuoteOutcome.Quoted);
        attempts.AttemptCount.ShouldBe(1);
        attempts.FailureReason.ShouldBeNull();
    }

    [Fact]
    public async Task exhausted_retries_emit_fare_quote_failed_pricing_unavailable()
    {
        // W001 §5.2 GWT: 3 consecutive transient failures
        // → FareQuoteFailed { PricingUnavailable, attemptCount: 3 }.
        _fixture.PricingClient = new PricingClientStub(_ =>
            throw new TransientPricingException("simulated pricing outage"));

        var response = await SubmitRideAndAwaitOutcome();

        await using var session = _host.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        var events = await session.Events.FetchStreamAsync(response.RideRequestId);

        events.Count.ShouldBe(2);
        events[0].Data.ShouldBeOfType<RideRequested>();
        var failed = events[1].Data.ShouldBeOfType<FareQuoteFailed>();
        failed.RideRequestId.ShouldBe(response.RideRequestId);
        failed.Reason.ShouldBe(FareQuoteFailureReason.PricingUnavailable);
        failed.AttemptCount.ShouldBe(3);

        var timeline = await session.LoadAsync<RequestTimeline>(response.RideRequestId);
        timeline.ShouldNotBeNull();
        timeline.Entries.Count.ShouldBe(2);
        timeline.Entries[1].EventType.ShouldBe(nameof(FareQuoteFailed));
        timeline.Entries[1].Summary.ShouldContain("PricingUnavailable");
        timeline.Entries[1].Summary.ShouldContain("3");

        var attempts = await session.LoadAsync<FareQuoteAttempts>(response.RideRequestId);
        attempts.ShouldNotBeNull();
        attempts.Outcome.ShouldBe(FareQuoteOutcome.Failed);
        attempts.AttemptCount.ShouldBe(3);
        attempts.FailureReason.ShouldBe(FareQuoteFailureReason.PricingUnavailable);
    }

    [Fact]
    public async Task non_transient_failure_emits_fare_quote_failed_immediately()
    {
        // W001 §5.2 GWT: NO_COVERAGE on the first call
        // → FareQuoteFailed { NoCoverage, attemptCount: 1 }; no further attempts.
        var callCount = 0;
        _fixture.PricingClient = new PricingClientStub(attempt =>
        {
            Interlocked.Increment(ref callCount);
            throw new NonTransientPricingException(
                FareQuoteFailureReason.NoCoverage,
                "pickup/dropoff outside Pricing's coverage");
        });

        var response = await SubmitRideAndAwaitOutcome();

        callCount.ShouldBe(1, "non-transient failures must not be retried");

        await using var session = _host.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        var events = await session.Events.FetchStreamAsync(response.RideRequestId);

        events.Count.ShouldBe(2);
        var failed = events[1].Data.ShouldBeOfType<FareQuoteFailed>();
        failed.Reason.ShouldBe(FareQuoteFailureReason.NoCoverage);
        failed.AttemptCount.ShouldBe(1);

        var timeline = await session.LoadAsync<RequestTimeline>(response.RideRequestId);
        timeline.ShouldNotBeNull();
        timeline.Entries[1].EventType.ShouldBe(nameof(FareQuoteFailed));
        timeline.Entries[1].Summary.ShouldContain("NoCoverage");

        var attempts = await session.LoadAsync<FareQuoteAttempts>(response.RideRequestId);
        attempts.ShouldNotBeNull();
        attempts.Outcome.ShouldBe(FareQuoteOutcome.Failed);
        attempts.AttemptCount.ShouldBe(1);
        attempts.FailureReason.ShouldBe(FareQuoteFailureReason.NoCoverage);
    }

    private async Task<RideRequestResponse> SubmitRideAndAwaitOutcome()
    {
        var riderId = Guid.CreateVersion7();
        var command = new SubmitRideRequest(
            RiderId: riderId,
            Pickup: new Location(40.7128, -74.0060, "123 Main St"),
            Dropoff: new Location(40.7580, -73.9855, "Eastbridge Library"),
            VehicleClass: VehicleClass.Standard,
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

    private static GetFareQuoteResponse DefaultPricingResponse() => new(
        FareAmountMinorUnits: 2150,
        Currency: "USD",
        FareBreakdown: new FareBreakdown(
            BaseMinorUnits: 500,
            DistanceMinorUnits: 1200,
            TimeMinorUnits: 450),
        ValidUntil: DateTimeOffset.MaxValue,
        PricingPolicyVersion: "stub-v1");
}
