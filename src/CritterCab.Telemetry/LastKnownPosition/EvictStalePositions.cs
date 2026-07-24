using CritterCab.Telemetry.TelemetryPolicy;
using Marten;
using TelemetryPolicyView = global::CritterCab.Telemetry.TelemetryPolicy.TelemetryPolicy;

namespace CritterCab.Telemetry.LastKnownPosition;

// The eviction tick (W006 §6.4). Parameterless by design: the sweep's only inputs are the clock
// and the policy, both resolved handler-side. Raised by LastKnownPositionEvictionService on a
// timer rather than by a Marten stream, so this is a plain *Handler, NOT a *Automation (the
// wolverine-marten-automation shape is for event-triggered work).
public sealed record EvictStalePositions;

public static class EvictStalePositionsHandler
{
    // "Heartbeat absence" means 3 consecutive missed heartbeats (§6.4). A documented constant
    // rather than a fourth TelemetryPolicy parameter in v1: it keeps slice 1's policy shape stable
    // and ties eviction semantically to the heartbeat — eviction IS heartbeat absence. Promote it
    // to a policy param in v2 only if ops needs to tune it independently.
    public const int MissedHeartbeatsBeforeEviction = 3;

    public static async Task Handle(
        EvictStalePositions command,
        IDocumentSession session,
        TimeProvider time,
        CancellationToken ct)
    {
        var policy = await session.Events.AggregateStreamAsync<TelemetryPolicyView>(
            TelemetryPolicyStream.Id, token: ct);

        // No policy means the ADR-011 bootstrap seed has not run yet. Sweeping against a guessed
        // threshold could evict live drivers, so skip this tick and let the next one pick it up.
        if (policy is null)
            return;

        var threshold = time.GetUtcNow()
            .AddSeconds(-MissedHeartbeatsBeforeEviction * policy.HeartbeatIntervalSeconds);

        // HardDeleteWhere, not DeleteWhere. DeleteWhere is conditional: if LastKnownPosition were
        // ever configured for soft-deletes it would silently switch to setting mt_deleted and the
        // row would survive — breaking §6.4's "Return" GWT, which requires an evicted driver to
        // find NO baseline and republish immediately. HardDeleteWhere makes "the row is gone"
        // immune to a future soft-delete configuration change.
        session.HardDeleteWhere<LastKnownPosition>(x => x.ServerReceivedAt < threshold);
        await session.SaveChangesAsync(ct);

        // No staleness event is published (v1 — R3/R8). Eviction is Telemetry's own storage
        // hygiene; propagating staleness to Dispatch is the v2 staleness-ceiling. A driver who
        // genuinely went off-shift also emits a Driver Profile availability transition, and
        // Dispatch's availability filter is the v1 net.
    }
}
