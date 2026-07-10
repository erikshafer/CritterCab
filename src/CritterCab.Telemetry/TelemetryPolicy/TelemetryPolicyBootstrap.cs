using Marten;
using Marten.Schema;

namespace CritterCab.Telemetry.TelemetryPolicy;

// Config-as-events bootstrap seed (ADR-011) via Marten's IInitialData — the idiomatic
// initial-data seam — using ADR-011's idempotent guard (FetchStreamStateAsync → append only
// if the singleton policy stream is empty). CritterCab's first config-as-events seed realized
// in code (ADR-011's third instance; Dispatch and Onboarding are design-only so far).
//
// A/B RECONCILIATION (flagged — retro §"design meets code" + docs/skills/DEBT.md): W006 §6.1
// locks "ADR-011 Option A (migration-time seed)". IInitialData realizes Option A's intent when
// run at deploy time via the JasperFx `resources setup` CLI command — but .InitializeWith<T>()
// ALSO runs it on every host start (Option B's execution timing), so absent a deploy-time
// resources-setup step it carries Option B's multi-instance seed race. Benign here (idempotent
// guard + full-replacement make a double-seed converge), and fine for single-instance MVP.
// Whether ADR-011 should be amended to describe this Marten realization is an open ADR decision.
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
