using CritterCab.Telemetry.TelemetryPolicy;
using CritterCab.Telemetry.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Marten;
using LastKnownPositionDocument = global::CritterCab.Telemetry.LastKnownPosition.LastKnownPosition;
using TelemetryPolicyView = global::CritterCab.Telemetry.TelemetryPolicy.TelemetryPolicy;

namespace CritterCab.Telemetry.ReportLocations;

// W006 §6.2 — the windowed GPS ingest. The whole client stream is the message: Wolverine's
// generated wrapper hands the entire inbound RPC stream in as IAsyncEnumerable<LocationPing> and
// returns this method's result as the single response on half-close.
//
// The per-ping pipeline lives INSIDE this method rather than at the boundary, and that is forced,
// not stylistic: Wolverine cannot weave Before/Validate middleware for client-streaming, because
// a before-frame needs a concrete request instance at method entry and a stream cannot provide
// one. So unlike slice 1's ConfigureTelemetryPolicy — which validates at the HTTP boundary with
// FluentValidation — validation and the publish trigger are handler concerns here. Do not try to
// add a boundary validator to this RPC.
public static class ReportLocationsHandler
{
    // §6.2 step 2 names an accuracyMeters threshold but fixes no value, and it is not a
    // TelemetryPolicy parameter. Documented constant, same call §6.4 made for the 3x-heartbeat
    // eviction threshold: it keeps slice 1's policy shape stable. Promote it to a policy param in
    // v2 if ops needs to tune it. 100m is a permissive urban-GPS quality gate — it rejects
    // wildly-uncertain fixes without discarding ordinary city-canyon noise.
    public const double MaxAccuracyMeters = 100d;

    public static async Task<LocationIngestAck> Handle(
        IAsyncEnumerable<LocationPing> pings,
        IDriverPrincipalAccessor principal,
        IDriverLocationPublisher publisher,
        IDocumentSession session,
        TimeProvider time,
        CancellationToken ct)
    {
        // R5: identity comes from the principal, never the payload. No driver, no window.
        var driverId = principal.CurrentDriverId
            ?? throw new RpcException(new Status(
                StatusCode.Unauthenticated,
                "No driver identity was presented on this stream."));

        // Read policy once at window open and hold it for the window's duration (§6.2). A policy
        // reconfigured mid-window takes effect on the next window, which is what makes
        // throttlePolicyVersion in the ack meaningful: it names the policy that actually governed
        // these pings.
        var policy = await session.Events.AggregateStreamAsync<TelemetryPolicyView>(
                         TelemetryPolicyStream.Id, token: ct)
                     ?? throw new RpcException(new Status(
                         StatusCode.FailedPrecondition,
                         "No telemetry policy is configured."));

        // Read the trigger baseline once, then keep it current in memory as this window publishes.
        // Safe because within a window the driver's own pings are the sole writer of their own
        // document. A null baseline — new driver, or one evicted by the §6.4 sweep — makes the
        // first ping publish immediately.
        var baseline = await session.LoadAsync<LastKnownPositionDocument>(driverId, ct);

        var acceptedCount = 0;

        await foreach (var ping in pings.WithCancellation(ct))
        {
            // Server-stamped on arrival: the monotonic LWW/dedup key (R7). deviceTimestamp is
            // retained on the wire but never trusted for ordering — client clocks skew.
            var serverReceivedAt = time.GetUtcNow();

            if (!IsAcceptable(ping))
                continue; // silently dropped, not errored — no per-ping error frame in v1 (§6.2)

            var h3Cell = H3CellIndexer.TryComputeCell(ping.Lat, ping.Lon, policy.H3Resolution);
            if (h3Cell is null)
                continue; // second safety net; H3 returns Invalid rather than throwing

            acceptedCount++;

            if (!ShouldPublish(baseline, h3Cell, serverReceivedAt, policy))
                continue; // accepted but absorbed — no publish, and no document write (§6.4)

            // Publish-first, then store (§6.3). The orderings fail differently and this one fails
            // benignly: publish-then-failed-store means the next ping re-evaluates against a stale
            // baseline and may republish, which the consumer's (driverId, serverReceivedAt) dedup
            // absorbs. Store-then-failed-publish would instead make Dispatch MISS this cell change
            // until the next heartbeat. A duplicate is cheaper than a miss, and the heartbeat is
            // the self-healing backstop either way. No outbox — the paired write is a document
            // upsert, not an event append, and the consistency model is explicitly eventual/LWW.
            await publisher.PublishAsync(
                BuildUpdate(driverId, ping, h3Cell, serverReceivedAt, policy), ct);

            baseline = new LastKnownPositionDocument
            {
                Id = driverId,
                Lat = ping.Lat,
                Lon = ping.Lon,
                H3Cell = h3Cell,
                ServerReceivedAt = serverReceivedAt
            };

            session.Store(baseline);
            await session.SaveChangesAsync(ct);
        }

        // Returned once, on half-close (§6.2 window semantics).
        return new LocationIngestAck
        {
            // Only pings that passed validation — the client can compare this against what it sent
            // to detect a systematically bad sensor.
            AcceptedCount = acceptedCount,
            // Lets the client correct for clock skew.
            ServerTime = Timestamp.FromDateTimeOffset(time.GetUtcNow()),
            // The policy singleton's stream version, so a client can detect a policy change
            // across windows.
            ThrottlePolicyVersion = policy.Version
        };
    }

    // shouldPublish = heartbeatDue OR (cellChanged AND throttleFloorElapsed)  — §6.2
    //
    // heartbeatDue subsumes the throttle floor by construction, because slice 1's boundary
    // validation enforces heartbeatInterval >= minPublishInterval. So the floor only ever gates
    // CELL-CHANGE publishes — which is precisely its job: stopping a driver who is hovering on a
    // cell boundary from flooding Kafka.
    private static bool ShouldPublish(
        LastKnownPositionDocument? baseline,
        string h3Cell,
        DateTimeOffset serverReceivedAt,
        TelemetryPolicyView policy)
    {
        // No baseline: a new driver, or one the eviction sweep removed. Publish immediately so a
        // returning driver reappears to Dispatch at once (§6.4 "Return").
        if (baseline is null)
            return true;

        // The document is written only on publish, so its stamp IS the last publish (§6.4).
        var sinceLastPublish = serverReceivedAt - baseline.ServerReceivedAt;

        var heartbeatDue = sinceLastPublish >= TimeSpan.FromSeconds(policy.HeartbeatIntervalSeconds);
        var cellChanged = !string.Equals(h3Cell, baseline.H3Cell, StringComparison.Ordinal);
        var throttleFloorElapsed =
            sinceLastPublish >= TimeSpan.FromSeconds(policy.MinPublishIntervalSeconds);

        return heartbeatDue || (cellChanged && throttleFloorElapsed);
    }

    private static bool IsAcceptable(LocationPing ping) =>
        double.IsFinite(ping.Lat) && ping.Lat is >= -90d and <= 90d &&
        double.IsFinite(ping.Lon) && ping.Lon is >= -180d and <= 180d &&
        double.IsFinite(ping.AccuracyMeters) &&
        ping.AccuracyMeters is >= 0d and <= MaxAccuracyMeters;

    private static DriverLocationUpdated BuildUpdate(
        Guid driverId,
        LocationPing ping,
        string h3Cell,
        DateTimeOffset serverReceivedAt,
        TelemetryPolicyView policy)
    {
        var update = new DriverLocationUpdated
        {
            DriverId = driverId.ToString(),
            Lat = ping.Lat,
            Lon = ping.Lon,
            H3Cell = h3Cell,
            // Carried explicitly rather than bit-decoded from the index: H3 self-encodes its
            // resolution, but consumer clarity wins (§6.3).
            H3Resolution = policy.H3Resolution,
            ServerReceivedAt = Timestamp.FromDateTimeOffset(serverReceivedAt),
            ThrottlePolicyVersion = policy.Version
        };

        // Optional pass-through — proto3 explicit presence, so only forward what was actually sent.
        if (ping.HasSpeed)
            update.Speed = ping.Speed;

        if (ping.HasHeading)
            update.Heading = ping.Heading;

        return update;
    }
}
