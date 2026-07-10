using Marten;
using Marten.Schema;

namespace CritterCab.Telemetry.TelemetryPolicy;

// ADR-011 Option A: migration-time idempotent seed. Marten's IInitialData runs after schema
// creation on host start; if the singleton policy stream is empty, it appends
// TelemetryPolicyConfigured with the documented defaults. Re-running is a no-op (idempotent
// guard on stream state). This is CritterCab's first config-as-events bootstrap seed realized
// in code (ADR-011's third instance, after Dispatch and Onboarding — both design-only so far).
public sealed class TelemetryPolicyBootstrap : IInitialData
{
    // Documented, ops-tunable seed defaults (W006 §6.1). h3Resolution 9 ≈ city-block
    // granularity; the exact integers are expected to be tuned against real ingest volume.
    public const int DefaultH3Resolution = 9;
    public const int DefaultHeartbeatIntervalSeconds = 30;
    public const int DefaultMinPublishIntervalSeconds = 5;

    public async Task Populate(IDocumentStore store, CancellationToken cancellation)
    {
        await using var session = store.LightweightSession();

        var state = await session.Events.FetchStreamStateAsync(TelemetryPolicyStream.Id, cancellation);
        if (state is not null)
            return; // already seeded — idempotent no-op

        // Seeders may read the wall clock directly (csharp-coding-standards § TimeProvider).
        var seed = new TelemetryPolicyConfigured(
            H3Resolution: DefaultH3Resolution,
            HeartbeatIntervalSeconds: DefaultHeartbeatIntervalSeconds,
            MinPublishIntervalSeconds: DefaultMinPublishIntervalSeconds,
            OperatorId: "system-bootstrap",
            Reason: "Initial deployment defaults",
            ConfiguredAt: DateTimeOffset.UtcNow);

        session.Events.StartStream<TelemetryPolicy>(TelemetryPolicyStream.Id, seed);
        await session.SaveChangesAsync(cancellation);
    }
}
