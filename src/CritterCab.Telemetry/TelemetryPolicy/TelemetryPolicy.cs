using JasperFx.Events;

namespace CritterCab.Telemetry.TelemetryPolicy;

// The latest-policy view read by the in-flight processor (W006 slices 2/3). Self-aggregating:
// the aggregate IS its own live-stream projection (registered via LiveStreamAggregation<T>).
// Full-replacement semantics (ADR-011) — each TelemetryPolicyConfigured wholly replaces prior
// policy state, so only the latest event's values survive.
public sealed record TelemetryPolicy(
    Guid Id,
    int H3Resolution,
    int HeartbeatIntervalSeconds,
    int MinPublishIntervalSeconds,
    string OperatorId,
    string Reason,
    DateTimeOffset ConfiguredAt)
{
    // Populated by Marten via name convention with the singleton stream version. Typed as long
    // (not int / IRevisioned) so it maps to the proto's int64 throttle_policy_version without
    // truncation. This is the throttlePolicyVersion carried into LocationIngestAck (slice 2) and
    // DriverLocationUpdated (slice 3).
    public long Version { get; set; }

    public static TelemetryPolicy Create(IEvent<TelemetryPolicyConfigured> @event)
    {
        var e = @event.Data;
        return new TelemetryPolicy(
            Id: @event.StreamId,
            H3Resolution: e.H3Resolution,
            HeartbeatIntervalSeconds: e.HeartbeatIntervalSeconds,
            MinPublishIntervalSeconds: e.MinPublishIntervalSeconds,
            OperatorId: e.OperatorId,
            Reason: e.Reason,
            ConfiguredAt: e.ConfiguredAt);
    }

    public static TelemetryPolicy Apply(TelemetryPolicyConfigured @event, TelemetryPolicy current) =>
        current with
        {
            H3Resolution = @event.H3Resolution,
            HeartbeatIntervalSeconds = @event.HeartbeatIntervalSeconds,
            MinPublishIntervalSeconds = @event.MinPublishIntervalSeconds,
            OperatorId = @event.OperatorId,
            Reason = @event.Reason,
            ConfiguredAt = @event.ConfiguredAt
        };
}
