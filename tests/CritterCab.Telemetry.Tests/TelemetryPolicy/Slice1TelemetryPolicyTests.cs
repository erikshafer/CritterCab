using Alba;
using CritterCab.Telemetry.TelemetryPolicy;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CritterCab.Telemetry.Tests.TelemetryPolicy;

// W006 §6.1 GWTs for the TelemetryPolicyConfigured config-as-events slice.
[Collection("Telemetry")]
public class Slice1TelemetryPolicyTests
{
    private readonly TelemetryTestFixture _fixture;

    public Slice1TelemetryPolicyTests(TelemetryTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task bootstrap_seeds_the_documented_defaults()
    {
        // Given the deployment migration ran (IInitialData on host start)
        await _fixture.ResetToSeedAsync();

        // Then the default policy exists at version 1 with the system-bootstrap marker
        var store = _fixture.Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var policy = await session.Events.AggregateStreamAsync<Telemetry.TelemetryPolicy.TelemetryPolicy>(
            TelemetryPolicyStream.Id);

        policy.ShouldNotBeNull();
        policy.H3Resolution.ShouldBe(9);
        policy.HeartbeatIntervalSeconds.ShouldBe(30);
        policy.MinPublishIntervalSeconds.ShouldBe(5);
        policy.OperatorId.ShouldBe("system-bootstrap");
        policy.Reason.ShouldBe("Initial deployment defaults");
        policy.Version.ShouldBe(1L);
    }

    [Fact]
    public async Task reconfigure_full_replaces_and_advances_the_version()
    {
        // Given TelemetryPolicy at version 1 (seed)
        await _fixture.ResetToSeedAsync();

        // When ConfigureTelemetryPolicy arrives with new values
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new ConfigureTelemetryPolicy(
                H3Resolution: 8,
                HeartbeatIntervalSeconds: 20,
                MinPublishIntervalSeconds: 4,
                OperatorId: "ops-alice",
                Reason: "Tuning for downtown density")).ToUrl("/api/telemetry/policy");
            s.StatusCodeShouldBeOk();
        });

        // Then a full-replacement event is appended and throttlePolicyVersion advances to 2
        var body = result.ReadAsJson<TelemetryPolicyResponse>();
        body.ShouldNotBeNull();
        body.H3Resolution.ShouldBe(8);
        body.HeartbeatIntervalSeconds.ShouldBe(20);
        body.MinPublishIntervalSeconds.ShouldBe(4);
        body.ThrottlePolicyVersion.ShouldBe(2L);
    }

    [Fact]
    public async Task invalid_policy_is_rejected_at_the_boundary_with_no_event_appended()
    {
        // Given the seed at version 1
        await _fixture.ResetToSeedAsync();

        // When a zero heartbeat interval is submitted
        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new ConfigureTelemetryPolicy(
                H3Resolution: 9,
                HeartbeatIntervalSeconds: 0,
                MinPublishIntervalSeconds: 5,
                OperatorId: "ops-alice",
                Reason: "Bad config")).ToUrl("/api/telemetry/policy");

            // Then it is rejected at the boundary (ProblemDetails 400)
            s.StatusCodeShouldBe(400);
        });

        // And no event is appended — the stream is still just the seed
        var store = _fixture.Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();
        var state = await session.Events.FetchStreamStateAsync(TelemetryPolicyStream.Id);
        state.ShouldNotBeNull();
        state.Version.ShouldBe(1L);
    }
}
