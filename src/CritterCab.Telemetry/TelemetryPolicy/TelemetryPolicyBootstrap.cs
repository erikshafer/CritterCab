using Marten;
using Marten.Schema;

namespace CritterCab.Telemetry.TelemetryPolicy;

// Config-as-events bootstrap seed (ADR-011) via Marten's IInitialData — the idiomatic
// initial-data seam — using ADR-011's idempotent guard (FetchStreamStateAsync → append only
// if the singleton policy stream is empty). CritterCab's first config-as-events seed realized
// in code (ADR-011's third instance; Dispatch and Onboarding are design-only so far).
//
// A/B reconciliation (RESOLVED — ADR-011 Amendment 2026-07-10): IInitialData is the canonical
// Marten realization of ADR-011 Option A. It seeds at the deploy-time apply step (`resources
// setup`) and idempotently at host start as a self-healing safety net; the multi-instance seed
// race is mitigated by this idempotent guard + full-replacement (a double-seed converges to
// identical state) and avoided by the deploy-time step / single-instance MVP posture.
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
