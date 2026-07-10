namespace CritterCab.Telemetry.TelemetryPolicy;

// The config-as-events policy is a singleton (one per BC, ADR-011), so its stream lives at
// a well-known, fixed id rather than a minted one. Both the bootstrap seed and the reconfigure
// endpoint resolve the same stream without a lookup (the UUIDv5-style natural-key rationale in
// csharp-coding-standards; a fixed constant is sufficient for a true singleton).
public static class TelemetryPolicyStream
{
    public static readonly Guid Id = Guid.Parse("7e1e3e77-b0b0-4a5a-9c9c-000000000001");
}
