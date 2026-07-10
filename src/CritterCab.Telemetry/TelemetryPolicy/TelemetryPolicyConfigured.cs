namespace CritterCab.Telemetry.TelemetryPolicy;

// The one event-sourced stream in this stream-processing BC (W006 §3.4): a singleton
// config-as-events stream (ADR-011), full-replacement semantics. No aggregate-id field —
// the stream is a well-known singleton (see TelemetryPolicyStream.Id). operatorId is
// "system-bootstrap" for the migration seed (ADR-011 audit marker).
public sealed record TelemetryPolicyConfigured(
    int H3Resolution,
    int HeartbeatIntervalSeconds,
    int MinPublishIntervalSeconds,
    string OperatorId,
    string Reason,
    DateTimeOffset ConfiguredAt);
