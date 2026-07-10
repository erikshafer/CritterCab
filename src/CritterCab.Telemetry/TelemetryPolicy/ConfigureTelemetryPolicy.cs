using FluentValidation;
using Marten;
using Wolverine.Http;

namespace CritterCab.Telemetry.TelemetryPolicy;

// Operator admin command for the throttle policy. Full-replacement semantics (ADR-011):
// each command produces a TelemetryPolicyConfigured that fully replaces prior policy state.
// configuredAt is server-stamped in the endpoint (TimeProvider), not supplied by the caller.
public sealed record ConfigureTelemetryPolicy(
    int H3Resolution,
    int HeartbeatIntervalSeconds,
    int MinPublishIntervalSeconds,
    string OperatorId,
    string Reason)
{
    // Cross-parameter validation at the HTTP boundary (W006 §6.1) — the singleton aggregate
    // has no state-transition invariant to defend beyond full replacement, so it stays thin.
    public sealed class ConfigureTelemetryPolicyValidator : AbstractValidator<ConfigureTelemetryPolicy>
    {
        // H3 supports resolutions 0–15; the policy tunes within that range.
        private const int MinH3Resolution = 0;
        private const int MaxH3Resolution = 15;

        public ConfigureTelemetryPolicyValidator()
        {
            RuleFor(x => x.H3Resolution)
                .InclusiveBetween(MinH3Resolution, MaxH3Resolution)
                .WithMessage($"H3 resolution must be between {MinH3Resolution} and {MaxH3Resolution}.");

            RuleFor(x => x.HeartbeatIntervalSeconds)
                .GreaterThan(0);

            RuleFor(x => x.MinPublishIntervalSeconds)
                .GreaterThan(0);

            // Heartbeat must not be tighter than the publish floor — otherwise the floor could
            // gate a due heartbeat. W006 §6.2 relies on heartbeatDue subsuming the throttle floor.
            RuleFor(x => x.HeartbeatIntervalSeconds)
                .GreaterThanOrEqualTo(x => x.MinPublishIntervalSeconds)
                .WithMessage("Heartbeat interval must be greater than or equal to the minimum publish interval.");

            RuleFor(x => x.OperatorId).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty();
        }
    }
}

public static class ConfigureTelemetryPolicyEndpoint
{
    [WolverinePost("/api/telemetry/policy")]
    public static async Task<TelemetryPolicyResponse> Handle(
        ConfigureTelemetryPolicy command,
        IDocumentSession session,
        TimeProvider time,
        CancellationToken ct)
    {
        var @event = new TelemetryPolicyConfigured(
            H3Resolution: command.H3Resolution,
            HeartbeatIntervalSeconds: command.HeartbeatIntervalSeconds,
            MinPublishIntervalSeconds: command.MinPublishIntervalSeconds,
            OperatorId: command.OperatorId,
            Reason: command.Reason,
            ConfiguredAt: time.GetUtcNow());

        // The singleton stream already exists (bootstrap seed ran on startup); append the
        // full-replacement configuration event to the well-known id.
        session.Events.Append(TelemetryPolicyStream.Id, @event);
        await session.SaveChangesAsync(ct);

        var policy = await session.Events.AggregateStreamAsync<TelemetryPolicy>(TelemetryPolicyStream.Id, token: ct);
        return TelemetryPolicyResponse.From(policy!);
    }
}

public sealed record TelemetryPolicyResponse(
    int H3Resolution,
    int HeartbeatIntervalSeconds,
    int MinPublishIntervalSeconds,
    long ThrottlePolicyVersion)
{
    public static TelemetryPolicyResponse From(TelemetryPolicy p) =>
        new(p.H3Resolution, p.HeartbeatIntervalSeconds, p.MinPublishIntervalSeconds, p.Version);
}
