---
name: marten-async-daemon
description: "Configuring and operating Marten's async projection daemon in CritterCab — daemon modes (Solo, HotCold, Wolverine-managed distribution), error handling, rebuilds, the high-water mark, the adaptive event loader, and health checks. Use when registering an async projection, deploying multi-node, debugging stale projections, or rebuilding a projection in production."
cluster: marten
tags: [marten, async-daemon, projections, high-water-mark, hot-cold, wolverine-managed-distribution, rebuilds, health-checks, eventual-consistency]
---

# Marten Async Daemon

The async daemon runs `ProjectionLifecycle.Async` projections and Wolverine event subscriptions in the background. It tracks a per-projection sequence cursor against the event store's high-water mark, processes events in batches, and persists the projected documents on its own schedule. Getting daemon registration right is the single highest-leverage Marten configuration knob in a CritterCab service — a misconfigured daemon either doesn't run at all, runs on the wrong node, or fights another daemon for shard ownership.

The generic mechanics — leader election, advisory locks, the slicing-and-applying pipeline, batch-to-PostgreSQL command coalescing — live in JasperFx ai-skills and Marten's own daemon documentation. **This skill documents Cab-specific decisions: which daemon mode to use, what the production rules are, how rebuilds work safely, and what the 2026 daemon improvements (adaptive event loader, opt-in event-type index, `EnrichEventsAsync`) actually buy you.**

## When to apply this skill

Use this skill when:

- Registering a new async projection or subscription with `service-bootstrap`.
- Deploying a Cab service across multiple nodes.
- Debugging stale or stuck async projections.
- Rebuilding a projection after a bug fix or schema change.
- Tuning daemon throughput when a projection's high-water lag is real.
- Adding async-daemon health checks.

Do NOT use this skill for:

- Projection shape and registration — see `marten-projections`.
- Aggregate shape and `Apply` methods — see `marten-aggregates`.
- Query-side reads against projected documents — see `marten-querying`.
- Test-side patterns including `WaitForNonStaleProjectionDataAsync` and tracked sessions — see `testing-integration` (Phase 2).
- Wolverine durability and message-store distribution generally — see `wolverine-handlers` and `service-bootstrap`.
- Polecat's parallel daemon — see `polecat-event-sourcing` (Phase 4); shape mirrors Marten's.

---

## What the Daemon Is Doing

The daemon's runtime model is worth pinning before the configuration discussion.

A **projection shard** is a logical segment of events processed independently. Most projections have one shard (`Trip:All`), but multi-stream and partitioned projections can have several. Each shard tracks its own progression cursor.

The **high-water mark** is the highest event sequence number the daemon believes is "safe to process" — meaning every sequence at or below it is committed and contiguous. Marten determines this with a separate background agent that polls the event store. The high-water mark frequently lags the absolute-newest sequence by a few hundred milliseconds because the daemon waits for any in-flight transactions to either commit or be deemed abandoned (the `StaleSequenceThreshold`, default 3 seconds).

For each shard, the loop is:

1. Compare the shard's cursor against the high-water mark.
2. If the shard is behind, fetch a batch of events (default 500, tunable via `AsyncOptions.BatchSize`).
3. Slice the batch into per-aggregate event groups.
4. Apply each slice to its document, batching all the resulting database operations into one PostgreSQL round trip.
5. Advance the shard's cursor and notify subscribers via the `ShardStateTracker`.

This loop runs continuously while the daemon is active. When events stop flowing the daemon backs off to slow polling (`SlowPollingTime`, default 1 second); when activity is heavy it polls fast (`FastPollingTime`, default 250ms).

---

## Daemon Modes

Three modes, registered via `.AddAsyncDaemon(DaemonMode.X)` or by setting `opts.Projections.AsyncMode = DaemonMode.X`.

| Mode | When | Behavior |
|---|---|---|
| `Disabled` | Default — async projections registered but not running | The daemon is dormant. Useful for read-only services or when another node owns processing. |
| `Solo` | Single-node deployments, dev environments, integration tests | The daemon starts at boot and runs every shard on this node. No leader election. |
| `HotCold` | Multi-node deployments without Wolverine integration | All nodes try to acquire a Postgres advisory lock; the winner runs every shard, the others stand by. On crash, another node takes over. |

**Wolverine-managed event subscription distribution** is a fourth path that supersedes `HotCold` when `IntegrateWithWolverine()` is wired:

```csharp
services.AddMarten(opts =>
{
    opts.Connection(connectionString);
    opts.Projections.Add<DriverLifetimeStatsProjection>(ProjectionLifecycle.Async);
})
.IntegrateWithWolverine(m =>
{
    m.UseWolverineManagedEventSubscriptionDistribution = true;
});

builder.Host.UseWolverine(opts =>
{
    // Required: Wolverine durability mode must be Balanced for distribution to work
    opts.Durability.Mode = DurabilityMode.Balanced;
});
```

Under this mode, Wolverine's agent infrastructure assigns shards across all running nodes — instead of concentrating every shard on one "hot" node, work spreads across the cluster. This is the production-grade default when a service uses `IntegrateWithWolverine()`.

### Cab default

- **Local development and integration tests:** `DaemonMode.Solo`. No leader election overhead, fast startup.
- **Single-node production (the Hetzner VPS deployment target initially):** `DaemonMode.Solo` is fine; no other node is competing.
- **Multi-node production with `IntegrateWithWolverine()`:** Wolverine-managed distribution, with `DurabilityMode.Balanced`. This is the long-term target for any service that scales horizontally.
- **Multi-node production without Wolverine:** `DaemonMode.HotCold`. Rare in Cab — most services integrate with Wolverine.

### Critical rule: do NOT combine `AddAsyncDaemon(HotCold)` with Wolverine-managed distribution

```csharp
// ❌ WRONG — two competing daemon managers
services.AddMarten(opts => { /* ... */ })
    .AddAsyncDaemon(DaemonMode.HotCold)
    .IntegrateWithWolverine(m =>
    {
        m.UseWolverineManagedEventSubscriptionDistribution = true;
    });
```

These are two different shard-assignment schemes. Wiring both produces a fight: Marten's HotCold leader election picks one node as hot, while Wolverine tries to distribute shards across all nodes. The observable result is non-deterministic shard ownership, duplicate projection work, and drifting `wolverine_node_assignments` records. Pick exactly one — for Cab, that's almost always Wolverine-managed distribution.

When using Wolverine-managed distribution, omit `AddAsyncDaemon` entirely; Wolverine starts and stops the daemon as part of its agent lifecycle.

---

## Error Handling Defaults

The daemon has separate error policies for **continuous** processing (running normally) and **rebuild** mode. Defaults are chosen so the daemon survives bad data in production but fails loudly during rebuilds where you can react to it.

```csharp
services.AddMarten(opts =>
{
    // Continuous processing — tolerate poison events so the daemon doesn't halt
    opts.Projections.Errors.SkipApplyErrors = true;          // default: true
    opts.Projections.Errors.SkipSerializationErrors = true;  // default: true
    opts.Projections.Errors.SkipUnknownEvents = true;        // default: true

    // Rebuild mode — no skipping; defaults all false
    opts.Projections.RebuildErrors.SkipApplyErrors = false;
    opts.Projections.RebuildErrors.SkipSerializationErrors = false;
    opts.Projections.RebuildErrors.SkipUnknownEvents = false;
});
```

Cab inherits these defaults. The asymmetry is deliberate: in production you never want one bad event to stop the daemon (which would silently halt every async projection), but during a rebuild you want to discover bad data instead of silently writing wrong projections.

When `SkipApplyErrors` is true, failing events are recorded to a dead-letter queue (`DeadLetterEvent`) and tagged with the projection name. Operations tooling can replay them after the underlying bug is fixed.

### Tombstone events and sequence gaps

Failed event-append transactions still consume sequence numbers — Postgres allocates them before the transaction commits. The daemon's high-water-mark detector handles these gaps with `StaleSequenceThreshold` (default 3 seconds): if a sequence number is "missing" for longer than that, the detector treats it as abandoned and advances past it. Marten 8 also writes "tombstone" events for explicit append failures that further reduce the gap problem.

If a service has long-running write transactions (over the 3-second threshold), bump `StaleSequenceThreshold` higher to avoid premature gap-skipping:

```csharp
opts.Projections.Daemon.StaleSequenceThreshold = 10.Seconds();
```

This is rare in Cab — most write transactions complete in well under a second.

---

## Throughput and Performance Knobs

Default daemon performance is fine for the vast majority of services. The knobs below are **second-line adjustments**, applied after profiling identifies a real bottleneck.

### `AsyncOptions.BatchSize` (default 500)

The maximum number of events fetched per daemon batch, set per-projection:

```csharp
opts.Projections.Add(new TripSummaryProjection(), ProjectionLifecycle.Async, asyncConfig: o =>
{
    o.BatchSize = 2000;
});
```

Larger batches reduce overhead but increase memory pressure and latency between event-append and projection visibility. Default is fine until profiling shows otherwise.

### `AsyncOptions.CacheLimitPerTenant` (default 0 — no cache)

Most-recently-used cache of aggregate documents during async projection. When set, the daemon avoids re-loading the same aggregate inside a batch — meaningful for projections with high fan-in (one document touched by many events in a single batch).

```csharp
opts.Projections.Add(new TripSummaryProjection(), ProjectionLifecycle.Async, asyncConfig: o =>
{
    o.CacheLimitPerTenant = 1000;
});
```

Memory grows with cache size; tune to the working set, not to "as high as possible."

### `opts.Events.UseIdentityMapForAggregates`

Caches aggregates within a session for `FetchForWriting` → `FetchLatest` round trips. Saves a round-trip when a handler reads its own write back in the same session. Cheap to enable; on by default in newer Marten versions for new applications. Verify per-service in `service-bootstrap`.

### `opts.Events.EnableAdvancedAsyncTracking`

Enables a finer-grained `mt_event_progression_skipping` table the daemon uses for high-water-mark detection. Trades a small amount of write overhead for better progression accuracy under high-throughput loads. Most Cab services don't need this; reach for it if `ProgressionProgressOutOfOrderException` appears in logs.

### `opts.Events.EnableEventSkippingInProjectionsOrSubscriptions`

Adds an `is_skipped` column to `mt_events` to mark events the daemon has decided to skip. Required for `Marten 8.6+` if you want skip behavior to be visible to ops tooling and the `skipped` metric. Recommended on for production services that care about poison-event diagnosis.

### `opts.Events.EnableEventTypeIndex` (Marten 8.29+, the April 2026 addition)

Opt-in composite `(type, seq_id)` B-tree index on `mt_events`. The single most consequential daemon performance addition of 2026: when projections filter on a small subset of event types and the event store has millions of events, **rebuilds without this index can time out** scanning past non-matching events.

```csharp
opts.Events.EnableEventTypeIndex = true;
```

Reach for this when:

- A projection rebuild times out or runs orders of magnitude slower than expected.
- The Cab service has accumulated millions of events and the projection only consumes a handful of types.
- The daemon log emits "Event loading timed out... Falling back to {SkipAhead|WindowStep}" — the adaptive loader (below) is doing its job, but the index removes the need.

Trade-off: every additional index slows event inserts. Don't enable speculatively. If the daemon emits the timeout warning, that's the signal.

### Adaptive event loader (Marten 8.29+, no configuration)

Even without `EnableEventTypeIndex`, the daemon now adapts when an event-loading query times out. It falls through progressively simpler strategies:

1. **Normal:** `seq_id` range + type filter, ordered by `seq_id`, limit `BatchSize`.
2. **Skip-ahead:** find the next matching event via `MIN(seq_id)`, fetch from there.
3. **Window-step:** advance through the sequence in 10K fixed windows until events are found.

The strategy resets to Normal when events flow normally again. No configuration is needed — this is automatic — but the warning log line (`Consider enabling opts.Events.EnableEventTypeIndex for better performance`) is the diagnostic signal that the index is now worth adding.

---

## Rebuilds

A projection rebuild truncates the projected documents and replays all matching events from sequence 0 (or the projection's configured floor). Rebuilds happen on schema changes, after bug fixes that produced wrong state, or as part of introducing a new projection to a system with existing events.

### Full rebuild via the daemon

```csharp
public sealed class RebuildOperationsDashboard
{
    public static async Task RunAsync(
        IDocumentStore store,
        CancellationToken ct)
    {
        await using var daemon = await store.BuildProjectionDaemonAsync();
        await daemon.RebuildProjectionAsync<OperationsDashboardProjection>(ct);
    }
}
```

`RebuildProjectionAsync<T>(ct)` blocks until the rebuild completes (default timeout: 5 minutes per shard; pass an explicit `TimeSpan` for longer rebuilds). For non-trivial event stores, run rebuilds out of the request path — through a background job, an admin endpoint, or `RunOaktonCommandsAsync` per `service-bootstrap`.

### Single-stream rebuild

For healing one aggregate's read model without touching every other stream — e.g., after a bug corrupts the projection for one specific Trip — Marten exposes:

```csharp
await store.Advanced.RebuildSingleStreamAsync<Trip>(tripId);
```

Replays only that stream's events. Cheap compared to a full rebuild; use this whenever the failure is scoped to a known aggregate ID.

### Rebuild teardown

`AsyncOptions.TeardownDataOnRebuild` (default `true`) controls whether the rebuild deletes existing projected documents before replaying. Leave it on unless you have a specific reason to merge new events into stale data. Custom teardown rules — for projections that write to multiple document types or external tables — are configured per-projection:

```csharp
opts.Projections.Add(new MultiTableProjection(), ProjectionLifecycle.Async, asyncConfig: o =>
{
    o.DeleteViewTypeOnTeardown<TripSummary>();
    o.DeleteDataInTableOnTeardown("custom_table");
});
```

### Position strategies for new projections

When introducing a new projection to a service that already has events, you may not want to replay history. Three strategies, declared in the projection's `AsyncConfiguration`:

```csharp
opts.Projections.Add(new NewDashboardProjection(), ProjectionLifecycle.Async, asyncConfig: o =>
{
    // Skip all existing events — only project events appended after subscription starts
    o.SubscribeFromPresent();

    // OR: start from a specific timestamp
    o.SubscribeFromTime(DateTimeOffset.UtcNow.AddDays(-30));

    // OR: start from a specific sequence number
    o.SubscribeFromSequence(1_500_000);
});
```

A fourth helper exists for the specific case of promoting an existing inline projection to async without rebuilding:

```csharp
o.SubscribeAsInlineToAsync();
```

This checks for prior async progress; if none is found, it starts from the highest current event sequence (since the inline projection has been keeping up all along). Switch a projection from `Inline` to `Async` cleanly.

---

## Async-Only Performance Hooks

Two features that materially change how async projections are written, both verified against current Marten source.

### `EnrichEventsAsync` for batch reference-data lookups

Without enrichment, projections that look up reference data per-event hit the N+1 query problem at high throughput:

```csharp
// ❌ N+1: one LoadAsync per event
public async Task Apply(IEvent<TripCompleted> e, TripReceipt receipt, IQuerySession session)
{
    var rider = await session.LoadAsync<RiderProfile>(e.Data.RiderId);
    receipt.RiderName = rider.DisplayName;
}
```

`EnrichEventsAsync` runs once per slice group before per-event `Apply` calls, giving you a single place to batch-load everything the slice needs:

```csharp
public class TripReceiptProjection : MultiStreamProjection<TripReceipt, Guid>
{
    public override async Task EnrichEventsAsync(
        SliceGroup<TripReceipt, Guid> group,
        IQuerySession session,
        CancellationToken ct)
    {
        var riderIds = group.Events()
            .OfType<IEvent<TripCompleted>>()
            .Select(e => e.Data.RiderId)
            .Distinct()
            .ToArray();

        var riders = await session.LoadManyAsync<RiderProfile>(ct, riderIds);
        var lookup = riders.ToDictionary(r => r.Id);

        foreach (var evt in group.Events().OfType<IEvent<TripCompleted>>())
        {
            if (lookup.TryGetValue(evt.Data.RiderId, out var rider))
            {
                group.Reference(rider);  // Make available during Apply
            }
        }
    }
}
```

Available on `SingleStreamProjection`, `MultiStreamProjection`, and (since Marten 8.29) `EventProjection`.

**Important limitation:** `EnrichEventsAsync` runs *only* in the async daemon's pipeline. It does **not** fire during `FetchForWriting` or live aggregations. Code paths that build aggregates synchronously (`[WriteAggregate]` handlers, `FetchLatest` calls) must resolve reference data themselves; relying on enrichment in those paths produces empty fields.

### Composite projections for staged dependencies

When projection B genuinely needs the output of projection A, run them as a single composite:

```csharp
opts.Projections.CompositeProjectionFor("TripDashboard", composite =>
{
    composite.Stage1.Add<TripSummaryProjection>();
    composite.Stage1.Add<DriverActivityProjection>();
    composite.Stage2.Add<RegionalDashboardProjection>();
});
```

Stage 1 completes before stage 2 begins, and the daemon shares one event-fetch between all projections in the composite. Always run async; never rebuild a constituent projection of a composite independently — the whole composite rebuilds as a unit:

```csharp
await daemon.RebuildProjectionAsync("TripDashboard", ct);
```

Cab uses composite projections sparingly. Reach for it only when one projection genuinely needs the outputs of another and independent projections with overlapping reads aren't a clean alternative.

---

## Health Checks

The async daemon ships with an ASP.NET Core health check (`Marten.AspNetCore` package) that verifies no projection's progression lags more than a configured threshold behind the high-water mark:

```csharp
services.AddHealthChecks()
    .AddMartenAsyncDaemonHealthCheck(maxEventLag: 1000);
```

Default `maxEventLag` is 100 events. Tune to the service's tolerance — a `Trips` service that processes thousands of events per second may legitimately sit 1000 events behind under load, while an `Operations` service should stay near zero.

When deploying multi-node with Kubernetes or another containerized environment, the health check is the right signal for "should this pod be in the load balancer." Combine with the standard `/alive` endpoint per `service-bootstrap`'s `MapDefaultEndpoints()`.

`AddMartenAsyncDaemonHealthCheck` accepts an optional `TimeProvider` argument for tests:

```csharp
services.AddHealthChecks()
    .AddMartenAsyncDaemonHealthCheck(maxEventLag: 1000, gracePeriod: TimeSpan.FromSeconds(30));
```

The `gracePeriod` exempts the daemon from the lag check during the configured window after startup, when the daemon is naturally catching up. Without it, a service that just restarted reports unhealthy until the daemon catches up — which can cause Kubernetes to kill it before it's stable.

---

## Observability

The daemon emits OpenTelemetry traces and metrics with a configurable prefix (default `marten`). Per-projection metrics are emitted using the projection's configured name:

| Metric | Type | Meaning |
|---|---|---|
| `marten.{projection}.all.processed` | Counter | Events processed by a projection shard |
| `marten.{projection}.all.gap` | Histogram | Lag between projection cursor and high-water mark |
| `marten.{projection}.all.skipped` | Counter | Events skipped due to apply errors, serialization errors, or unknown event types |
| `marten.daemon.highwatermark` | Span | High-water-mark calculation step |

The `gap` histogram is the most operationally consequential metric — it's what should drive alerting. A sustained gap over the service's tolerance (typically a few seconds of events) means projections are stale and reads from them will return outdated data.

Spans the daemon emits per shard (useful in trace UIs):

| Span | Description |
|---|---|
| `marten.{name}.all.execution` | Processing one batch of events |
| `marten.{name}.all.loading` | Fetching events for a batch |
| `marten.{name}.all.grouping` | Grouping step (multi-stream projections) |

Configure the OTel prefix and `ActivitySource` via `opts.Projections.Daemon`:

```csharp
opts.Projections.Daemon.OtelPrefix = "crittercab.trips.daemon";
opts.Projections.Daemon.ActivitySource = new ActivitySource("CritterCab.Trips");
```

---

## Common pitfalls

- **`AddAsyncDaemon(HotCold)` + Wolverine-managed distribution.** Two competing schedulers. Pick one; for Cab with `IntegrateWithWolverine()`, omit `AddAsyncDaemon` entirely.
- **`DurabilityMode.Solo` in production.** Loses leader election, shard distribution, and crash recovery. Always `Balanced` for production; `Solo` is dev-and-test only. Different from `DaemonMode.Solo`, which is fine for single-node deployments.
- **Forgetting `WaitForNonStaleProjectionDataAsync` in async-projection tests.** Silent intermittent failures that look like flakiness. The test passes locally because the daemon caught up; CI fails because it didn't. See `testing-integration` (Phase 2).
- **Querying projected documents inside the same transaction as the events that produced them.** Async projections are eventually consistent — there's no `SaveChangesAsync` synchronization point. The handler that wrote the events doesn't see the projection it caused.
- **Speculative tuning of `BatchSize` and `CacheLimitPerTenant`.** Defaults are tuned. Profile first; the gap metric and shard-loading span tell you which knob to turn.
- **`EnrichEventsAsync` in synchronous paths.** Only runs in the async daemon. Aggregate write models built via `[WriteAggregate]` or `FetchForWriting` see no enrichment — resolve reference data in the handler instead.
- **Turning on `EnableEventTypeIndex` speculatively.** Adds an index to `mt_events`; insert performance suffers. Only enable when the daemon log emits the timeout-and-fallback warning, or when rebuilds genuinely time out.
- **Rebuilding a constituent of a composite projection independently.** Doesn't work; the whole composite rebuilds as a unit. `daemon.RebuildProjectionAsync("CompositeName", ct)` is the only valid path.
- **Skipping events silently in production without diagnosis.** `SkipApplyErrors = true` is the right default, but every skipped event lands in the dead-letter queue or the `skipped` metric. Treat sustained skip rates as a bug, not as "the system handling poison events." Investigate, fix, replay.

---

## See also

**Upstream** — load these first:

- `marten-projections` — projection lifecycles, snapshot vs. evolve, single-stream and multi-stream patterns; what the daemon is running.
- `marten-aggregates` — aggregate shape and live aggregation; the lower-cost alternative when the daemon would be overkill.
- `service-bootstrap` — `AddMarten`, `IntegrateWithWolverine`, `AddAsyncDaemon`, `DurabilityMode`; where most daemon configuration lands.

**Sibling skills:**

- `marten-querying` — eventual-consistency consequences for read-side endpoints; `WaitForNonStaleProjectionDataAsync`.
- `marten-wolverine-aggregates` — handler-side aggregate workflow that produces the events the daemon consumes.
- `dynamic-consistency-boundary` — DCB write paths are inline; the daemon doesn't directly affect them, but DCB-tagged events still flow through async projections like any other event.

**Downstream:**

- `testing-integration` — `WaitForNonStaleProjectionDataAsync`, tracked-session helpers (`PauseThenCatchUpOnMartenDaemonActivity`, `WaitForNonStaleDaemonDataAfterExecution`), `DaemonMode.Solo` for fixtures (Phase 2).
- `observability-tracing` — wiring the daemon's OTel signals into the Cab observability stack (Phase 3).
- `polecat-event-sourcing` — Polecat's parallel daemon for SQL Server services (Phase 4).

**External:**

- ai-skills `marten-event-sourcing-fundamentals` — generic event-store mechanics; complements this skill.
- ai-skills `marten-aggregate-handler-workflow` — full read-and-write workflow context.
- All ai-skills installed via `npx skills add` (license required).
- [Marten Async Daemon Documentation](https://martendb.io/events/projections/async-daemon.html) — the canonical daemon reference.
- [Marten Async Daemon Health Checks](https://martendb.io/events/projections/healthchecks.html).
- [Marten 8.29 Release Notes — Adaptive EventLoader and EnableEventTypeIndex (Jeremy Miller, April 7, 2026)](https://jeremydmiller.com/2026/04/07/marten-polecat-and-wolverine-releases/) — the most consequential 2026 daemon improvements.
- [Marten's Aggregation Projection Subsystem (Jeremy Miller, January 22, 2026)](https://jeremydmiller.com/2026/01/22/martens-aggregation-projection-subsystem/) — daemon runtime model walkthrough.
- [Easier Query Models with Marten (Jeremy Miller, January 20, 2026)](https://jeremydmiller.com/2026/01/20/easier-query-models-with-marten/) — composite projections and `EnrichEventsAsync`.
- [Testing Asynchronous Projections in Marten (Jeremy Miller, March 26, 2024)](https://jeremydmiller.com/2024/03/26/testing-asynchronous-projections-in-marten/) — the original `WaitForNonStaleProjectionDataAsync` reference.
