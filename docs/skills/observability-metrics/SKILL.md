---
name: observability-metrics
description: "OpenTelemetry metrics across CritterCab services — counters, histograms, and gauges that complement the distributed traces documented in observability-tracing. Covers the four-layer registration pattern (Aspire AddServiceDefaults + Wolverine's per-service Meter named 'Wolverine:{ServiceName}' which requires the wildcard form AddMeter('Wolverine:*'), Marten's 'Marten' Meter with opt-in TrackEventCounters() and custom ExportCounterOnChangeSets hook, and Polecat 3.1's 'Polecat' Meter exposed via opts.OpenTelemetry.Meter for custom instruments), the full Wolverine instrument catalog (wolverine-messages-sent / -received / -succeeded counters; wolverine-execution-failure counter; wolverine-dead-letter-queue counter; wolverine-execution-time and wolverine-effective-time histograms; wolverine-inbox-count / -outbox-count / -scheduled-count observable gauges from PersistenceMetrics with optional .{databaseName} suffix for multi-store services; tags message.type / message.destination / tenant.id / exception.type / source), Marten's opt-in marten.event.append counter, the Polecat 3.1 IDocumentStoreUsageSource discovery for CritterWatch monitoring (DocumentStoreUsage and EventStoreUsage descriptors with TagTypeDescriptor list, services.GetServices<IDocumentStoreUsageSource>() bridge, EnableExtendedProgressionTracking adding heartbeat/agent_status/pause_reason/running_on_node columns to pc_event_progression for daemon health alerting), the ASP.NET Core / HTTP client / gRPC instrumentation pulled in by AddServiceDefaults, per-BC custom Meter conventions paralleling the per-BC ActivitySource pattern, the Aspire dashboard metrics view, cumulative aggregation and view bucket boundary configuration, high-cardinality cardinality risks (especially message.type and tenant.id), and cross-language metrics from cab-go flowing through the same OTLP pipeline. Documents the wildcard AddMeter('Wolverine:*') registration form that observability-tracing was updated to use in concert with this skill, because Wolverine's Meter name is per-service-suffixed (Wolverine:{ServiceName})."
cluster: observability
tags: [opentelemetry, metrics, counters, histograms, observable-gauges, wolverine, marten, polecat, aspire-dashboard, otlp, critter-watch, progression-tracking, per-bc-meters, cardinality]
---

# Observability — Metrics

Every CritterCab service emits OpenTelemetry metrics alongside the distributed traces documented in `observability-tracing`. Counters track message throughput, histograms track latency distributions, and observable gauges expose live state like outbox depth. The Aspire dashboard renders the metrics in local development; a production OTLP collector (Prometheus via the OTLP receiver, Grafana Mimir, Azure Monitor) ingests them in deployed environments.

This skill is the metrics companion to `observability-tracing`. The two together form Cab's full observability story: traces show *what happened* in a single distributed flow; metrics show *how the system is performing* across many flows. Read both — they share the same Aspire bootstrap, the same OTLP exporter pipeline, and most of the same sampling decisions, but they diverge in the per-source registration shape and especially in cardinality concerns.

The single most important detail — and one that `observability-tracing` was updated to reflect in concert with this skill: **Wolverine's Meter name is per-service-suffixed**. The runtime creates `new Meter("Wolverine:" + options.ServiceName, ...)`, so in a Cab Trips service the meter is named `"Wolverine:CritterCab.Trips"`. Registering `AddMeter("Wolverine")` does NOT match this dynamic name and silently drops all Wolverine metrics. The correct registration is `AddMeter("Wolverine:*")` with the wildcard suffix, which the OpenTelemetry .NET SDK supports as a prefix match. Marten and Polecat have stable Meter names (`"Marten"` and `"Polecat"`) without suffixes, so those AddMeter calls are exact.

**Prerequisite packages not yet committed.** `Directory.Packages.props` does not include the OpenTelemetry SDK or instrumentation packages. The same packages flagged by `observability-tracing` are required here plus the metric-specific instrumentation modules. Surface them when the metrics bootstrap lands.

---

## When to apply this skill

Use this skill when:

- Adding OTel metrics to a new or existing Cab service's `Program.cs` — registering Wolverine, Marten, and (for Payments) Polecat Meters.
- Diagnosing throughput, latency, or saturation issues using the Aspire dashboard metrics view.
- Understanding what counters and histograms Wolverine emits and what tags they carry — particularly the per-tenant and per-message-type tag-cardinality risks.
- Adding custom Meters per BC for domain-specific metrics.
- Configuring view bucket boundaries on Wolverine's `wolverine-execution-time` histogram for service-specific latency budgets.
- Wiring CritterWatch (or any monitoring tool that consumes Polecat 3.1's `IDocumentStoreUsageSource` descriptors) into a Polecat-backed service.
- Diagnosing why metrics emitted by the Go service (`cab-go`) aren't surfacing alongside the .NET service metrics in the dashboard.

Do NOT use this skill for:

- Distributed traces (spans) — `observability-tracing` is canonical. The bootstrap shape is shared but the per-source registration and the cardinality concerns differ.
- Aspire orchestration or the dashboard itself — `aspire` and `cli-aspire`.
- Wolverine handler shapes — `wolverine-messaging-handlers`, `wolverine-grpc-handlers`, `wolverine-grpc-bidirectional-handlers`.
- Polecat schema and Weasel migration of `pc_event_progression` columns — `polecat-event-sourcing` § Schema management. This skill assumes the schema includes the extended columns when you opt into them.
- Production metric backend selection (Prometheus vs Mimir vs Azure Monitor) — out of scope; OTLP exports cleanly to all three.

---

## Bootstrap pattern

The full registration in a Cab service's `Program.cs`, paralleling the tracing block from `observability-tracing` and adding the metric instrument selectors:

```csharp
builder.AddServiceDefaults();    // Aspire foundation — OTel SDK, OTLP exporter, ASP.NET Core + HttpClient instrumentation

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Wolverine")          // exact-match — span source
            .AddSource("Marten")
            .AddSource("Polecat")            // for Polecat-backed services (Payments)
            .AddSource("CritterCab.Trips");  // per-BC custom spans
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Wolverine:*")         // WILDCARD — Meter is "Wolverine:{ServiceName}"
            .AddMeter("Marten")              // exact — opt-in via opts.OpenTelemetry.TrackEventCounters()
            .AddMeter("Polecat")             // exact — exposed for custom instruments only
            .AddMeter("CritterCab.Trips");   // per-BC custom Meter

        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
    });
```

`AddServiceDefaults()` (from the Aspire service-defaults project) wires the OTel SDK, the OTLP exporter pointed at `OTEL_EXPORTER_OTLP_ENDPOINT` (Aspire injects this), and a baseline of ASP.NET Core and HTTP-client tracing. The metrics block layers Cab-specific Meters on top.

**The wildcard pattern.** `AddMeter` accepts wildcards via `*` per the OpenTelemetry .NET SDK. `"Wolverine:*"` matches `Wolverine:CritterCab.Trips`, `Wolverine:CritterCab.Payments`, etc. Without the wildcard, `AddMeter("Wolverine")` matches only a literal-named meter `"Wolverine"` — which Wolverine never creates. `observability-tracing`'s bootstrap block uses the same wildcard form for consistency.

**Marten's Meter is exact-named** because Marten's `OpenTelemetryOptions.Meter` is created as `new Meter("Marten")` regardless of how many Marten stores a service registers. If you have multiple Marten stores in one process (rare in Cab — each BC has at most one), they all publish to the same `"Marten"` meter, distinguished by tags rather than meter names.

**Polecat's Meter is also exact-named** (`"Polecat"`) and exposed via `opts.OpenTelemetry.Meter` — but Polecat ships with **zero auto-emitted instruments**. Registering it in `AddMeter` is harmless but only matters if you attach custom instruments via `opts.OpenTelemetry.Meter.CreateCounter<T>(...)`. The Polecat-side observability story is mostly the v3.1 `IDocumentStoreUsageSource` descriptors documented below — separate from OTel metrics.

### Packages to add

Additive to what `observability-tracing` flagged:

```xml
<PackageVersion Include="OpenTelemetry.Instrumentation.Runtime" Version="{verify-latest}" />
<PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="{verify-latest}" />
<PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="{verify-latest}" />
```

`AddRuntimeInstrumentation()` provides .NET runtime metrics (GC, thread pool, JIT, exceptions) — essentially mandatory for production observability and free of cardinality concerns. `AddAspNetCoreInstrumentation()` and `AddHttpClientInstrumentation()` are also tracing-side packages that expose metric instruments on the same registration; depending on your `observability-tracing` setup they may already be transitively present. Verify in `Directory.Packages.props` before adding.

---

## Wolverine instruments

The `Wolverine:{ServiceName}` Meter exposes 11 instruments, all source-verified against `Wolverine\MetricsConstants.cs`, `Wolverine\Runtime\WolverineRuntime.cs`, and `Wolverine\Persistence\PersistenceMetrics.cs`:

| Instrument | Type | Unit | Notes |
|---|---|---|---|
| `wolverine-messages-sent` | `Counter<int>` | Messages | Increments on every successful publish |
| `wolverine-messages-received` | `Counter<int>` | Messages | Increments on every external (non-local) listener receipt |
| `wolverine-messages-succeeded` | `Counter<int>` | Messages | Increments after a handler completes without exception |
| `wolverine-execution-failure` | `Counter<int>` | Messages | Increments when a handler throws (note: name is "execution-failure" not "messages-failed") |
| `wolverine-dead-letter-queue` | `Counter<int>` | Messages | Increments when a message is moved to the DLQ after retry exhaustion |
| `wolverine-execution-time` | `Histogram<long>` | Milliseconds | Wall-clock from envelope dequeue to handler completion |
| `wolverine-effective-time` | `Histogram<double>` | Milliseconds | End-to-end from `SentAt` timestamp to handler completion (includes transport time and queueing) |
| `wolverine-inbox-count` | `ObservableGauge<int>` | Messages | Live inbox depth, polled at `DurabilitySettings.UpdateMetricsPeriod` |
| `wolverine-outbox-count` | `ObservableGauge<int>` | Messages | Live outbox depth |
| `wolverine-scheduled-count` | `ObservableGauge<int>` | Messages | Live scheduled-message count |

The three observable gauges optionally carry a `.{databaseName}` suffix when a service has multiple message-store databases (e.g., `wolverine-inbox-count.payments` for a Polecat-backed Payments service whose Wolverine message store is on its own database). The single-store case has no suffix.

### Tags on every Wolverine instrument

Wolverine writes these tag keys consistently across all message-related instruments (defined in `MetricsConstants`):

| Tag key | Value source | Cardinality risk |
|---|---|---|
| `message.type` | `envelope.GetMessageTypeName()` | Bounded by the number of distinct message types in the service — typically dozens |
| `message.destination` | `envelope.Destination` URI | Bounded by transport endpoints — typically a handful |
| `tenant.id` | `envelope.TenantId` | **HIGH** — bounded by the number of tenants. Cap or drop when the tenant set is unbounded (see § Cardinality below) |
| `exception.type` | Exception's `FullNameInCode()` | Bounded; only on `wolverine-execution-failure` and `wolverine-dead-letter-queue` |
| `source` | Service name | Constant per process |

The `tenant.id` tag is the dominant cardinality concern. Cab's Payments BC is multi-tenant; the same dashboards with the tenant tag intact give per-tenant visibility but multiply the metric series count by the tenant count. For services where per-tenant breakdown is essential (Payments, Trips), keep the tag. For services where it isn't (Telemetry — the metric is interesting in aggregate, not per-tenant), use a Metric View to drop the tag at SDK level rather than relying on backend aggregation.

### Multi-database observable gauges

The `.{databaseName}` suffix on `wolverine-inbox-count`/`wolverine-outbox-count`/`wolverine-scheduled-count` happens only when `WolverineRuntime` constructs the metrics for a non-default database. In practice this matters for ancillary Marten/Polecat stores. The standard single-store case emits the unsuffixed names. If you see `wolverine-inbox-count.foo` in the dashboard for a service that should have one store, something registered an ancillary store unintentionally.

---

## Marten instruments

Marten ships with **opt-in** metrics. The `"Marten"` Meter exists from the moment `AddMarten` is called, but no instruments are created automatically. To emit any metrics, configure `opts.OpenTelemetry`:

```csharp
services.AddMarten(opts =>
{
    opts.Connection(connectionString);

    // Opt-in: emit a counter on every appended event
    opts.OpenTelemetry.TrackEventCounters();
});
```

`TrackEventCounters()` registers a counter named `marten.event.append` (units: `events`) that increments once per event appended. Each increment carries tags:

| Tag key | Value |
|---|---|
| `event.type` | The event type name |
| `tenant.id` | Tenant ID from the change set |

Both tags can be high-cardinality — the `event.type` count is bounded but `tenant.id` is the same multi-tenant concern as Wolverine's. Apply the same Metric View strategy if needed.

### Custom Marten metrics

For domain-specific metrics on Marten commits, use `ExportCounterOnChangeSets`:

```csharp
opts.OpenTelemetry.ExportCounterOnChangeSets<long>(
    "trips.documents.changed",
    "documents",
    (counter, commit) =>
    {
        counter.Add(commit.Updated.Count(),
            new TagList { { "operation", "update" } });
    });
```

The hook fires after every successful `SaveChangesAsync`. Use it sparingly — every commit pays the callback cost. For high-frequency event streams, prefer `TrackEventCounters` over hand-rolled counters.

### Connection-tracking is tracing, not metrics

`opts.OpenTelemetry.TrackConnections = TrackLevel.Normal` (or `Verbose`) emits **spans** for connection lifecycle and command execution — covered in `observability-tracing` § Marten spans. It doesn't add metric instruments. Don't conflate the two; setting `TrackConnections` won't make any counters appear in the dashboard.

---

## Polecat instruments and the v3.1 monitoring discovery

Polecat 3.0+ ships a Meter named `"Polecat"` exposed via `opts.OpenTelemetry.Meter`, but with **zero auto-emitted instruments**. The Meter exists for users to attach custom instruments:

```csharp
services.AddPolecat(opts =>
{
    opts.Connection(connectionString);

    var paymentsCommitCounter = opts.OpenTelemetry.Meter.CreateCounter<long>(
        "payments.commits",
        "commits",
        "Successful Polecat commits in the Payments service");

    // Wire the counter into a session listener or commit hook
});
```

`opts.OpenTelemetry.TrackConnections` (also `None | Normal | Verbose`) is tracing-only, parallel to Marten's setting. Connection metrics for Polecat aren't a built-in feature — derive them from Wolverine's persistence metrics or instrument by hand.

### v3.1 IDocumentStoreUsageSource discovery (the CritterWatch story)

Polecat 3.1 added a separate observability surface that's NOT OpenTelemetry metrics — it's a **descriptor publishing mechanism** for monitoring tools. `IDocumentStore` now implements `IDocumentStoreUsageSource` (from `JasperFx.Events`), and the `AddPolecat` registration auto-bridges this so that:

```csharp
// In a monitoring tool or diagnostic endpoint
var sources = serviceProvider.GetServices<IDocumentStoreUsageSource>();
foreach (var source in sources)
{
    var usage = await source.TryCreateUsage(cancellationToken);
    // Inspect: usage.DatabaseSchemaName, usage.AutoCreateSchemaObjects,
    //         usage.Database.MainDatabase, usage.DocumentMappings, etc.
}
```

resolves the running Polecat store. Two consumers Cab cares about:

- **CritterWatch** — JasperFx's monitoring tool. Discovers Polecat (and Marten) stores in a running service and visualizes their schema, projections, daemon state, and registered DCB tag types.
- **Wolverine `ServiceCapabilities.readDocumentStores`** — Wolverine's introspection API used by tooling to enumerate document stores attached to a Wolverine host.

The descriptors expose:

- `DocumentStoreUsage` — database identity, schema name, auto-create policy, per-document `DocumentMappingDescriptor` entries with the SQL Server DDL each mapping emits.
- `EventStoreUsage` — registered DCB tag types via a typed `TagTypeDescriptor` list, projection registrations, async daemon configuration.

This is purely tooling-discovery infrastructure — no traces, no metric instruments, no runtime cost beyond the descriptor build at the moment a tool calls `TryCreateUsage`. It complements OpenTelemetry rather than competing with it: traces and metrics tell you *what's happening*; the usage descriptors tell tools *what's configured*.

### EnableExtendedProgressionTracking — daemon health signals

A separate Polecat opt-in adds extra columns to `pc_event_progression` for projection daemon health:

```csharp
services.AddPolecat(opts =>
{
    opts.Connection(connectionString);
    opts.Events.EnableExtendedProgressionTracking = true;
});
```

The four added columns:

| Column | Purpose |
|---|---|
| `heartbeat` | UTC timestamp of the daemon's most recent progression update |
| `agent_status` | Current state of the daemon agent (running, paused, errored) |
| `pause_reason` | Free-text reason when an agent is paused |
| `running_on_node` | Identifier of the host process currently owning the projection |

CritterWatch reads these columns to alert when a projection's `heartbeat` goes stale or when an `agent_status` flips to errored. They also help diagnose multi-node scenarios — `running_on_node` reveals which Aspire-orchestrated instance owns each projection. The columns are additive to the base `pc_event_progression` schema — Weasel migrations apply them via `db-apply` (per `cli-jasperfx`) when the opt-in is enabled.

For Cab Payments BC, default to enabling extended tracking. The cost is one column-write per progression update (negligible) and the operational value is real.

---

## ASP.NET Core, HTTP client, and runtime instrumentation

`AddServiceDefaults()` from Aspire pulls these in at the tracing layer; the metrics SDK exposes them automatically when `WithMetrics` is configured. The key counters and histograms:

| Source | Instruments |
|---|---|
| ASP.NET Core | `http.server.request.duration` (histogram), `http.server.active_requests` (gauge), `aspnetcore.routing.match_attempts`, `aspnetcore.diagnostics.exceptions` |
| HTTP client | `http.client.request.duration`, `http.client.open_connections` |
| .NET runtime | `dotnet.gc.heap.size`, `dotnet.gc.collections`, `dotnet.thread_pool.queue.length`, `dotnet.thread_pool.thread.count`, `dotnet.exceptions`, `dotnet.jit.compiled_il.size` |

Tag conventions on the HTTP instruments follow the OpenTelemetry semantic conventions for HTTP — `http.request.method`, `http.response.status_code`, `url.scheme`, `url.path` (with route templates, not raw paths, to bound cardinality).

**gRPC instrumentation:** Wolverine's gRPC integration emits the same `wolverine-*` instruments above, attributing to the gRPC transport via the `messaging.system="grpc"` span tag rather than separate gRPC-specific metrics. For .NET-side gRPC client metrics on calls *out* of a Cab service, `AddHttpClientInstrumentation()` covers it (gRPC.NET runs over HTTP/2 + HttpClient). Server-side gRPC requests show up under `http.server.request.duration` with `http.request.method=POST` and the gRPC service path in `url.path`.

---

## Per-BC custom Meters

Cab's convention parallels the per-BC `ActivitySource` pattern from `observability-tracing` § Per-BC ActivitySource conventions:

```csharp
// CritterCab.Trips/Domain/TripMetrics.cs
public static class TripMetrics
{
    public static readonly Meter Meter = new("CritterCab.Trips", "1.0.0");

    public static readonly Counter<long> TripsStarted =
        Meter.CreateCounter<long>("trips.started", "trips");

    public static readonly Counter<long> TripsCompleted =
        Meter.CreateCounter<long>("trips.completed", "trips");

    public static readonly Histogram<double> RideMatchingDuration =
        Meter.CreateHistogram<double>(
            "trips.ride_matching.duration",
            "ms",
            "Time from ride request to driver assignment");
}

// In a handler
TripMetrics.TripsStarted.Add(1, new TagList
{
    { "tenant.id", tenantId },
    { "city", cityCode },
});
```

Naming convention:

- Meter name = `CritterCab.{BC}` exactly matching the ActivitySource name from `observability-tracing` for consistency.
- Instrument names use lowercase dot-separated nouns: `trips.started`, `dispatch.matchmaking.requests`, `payments.captures`. Avoid Wolverine's `wolverine-foo-bar` hyphenated style for custom Cab instruments — keeps a clear visual distinction in the dashboard between framework and domain metrics.
- Units use the OTel-conventional short forms: `ms`, `s`, `bytes`, `requests`, `events`, plus domain-specific units like `trips`, `payments`.

Register the per-BC Meter in `Program.cs`:

```csharp
.WithMetrics(metrics =>
{
    metrics
        .AddMeter("Wolverine:*")
        .AddMeter("Marten")
        .AddMeter("CritterCab.Trips");  // Same name as TripMetrics.Meter
});
```

---

## Aspire dashboard metrics view

The Aspire dashboard surfaces metrics in the same UI as traces, accessible via the "Metrics" tab on each resource. What you see:

- **Per-resource metric list** — all instruments emitted by the selected resource. Wolverine's per-service Meter shows up as one source per service.
- **Time-series view** — line charts for counters and histograms. Tag filters appear as legend entries.
- **Histogram percentiles** — automatic p50/p90/p99 derivation from histogram instruments. `wolverine-execution-time` and `wolverine-effective-time` show up here directly.
- **Cross-resource navigation** — clicking a metric value drills into the matching trace window if traces are sampled at that timestamp.

Refresh interval is dashboard-side (defaults to a few seconds in Aspire 13.2). Metric data points are buffered in the dashboard's in-memory store; restarting the AppHost clears history. For longer-retention metrics (production), point the OTLP exporter at a real backend.

The dashboard renders cumulative-aggregation counters as rate-of-change automatically — what you see is "messages per second" rather than the raw cumulative count. Histograms render with bucket boundaries the SDK chose; override them via Metric Views (below) if the defaults don't fit Cab's latency profile.

---

## Aggregation, view bucket boundaries, and cardinality

OpenTelemetry .NET defaults to **cumulative aggregation** — counters report cumulative-since-process-start, the OTLP exporter handles delta calculation downstream. Aspire's dashboard expects cumulative; production OTLP receivers (Prometheus via OTLP, Mimir, Azure Monitor) handle both.

Default histogram bucket boundaries are sensible for HTTP-latency-shaped workloads but suboptimal for the Wolverine `wolverine-execution-time` histogram if Cab's handlers run under 5ms typically. Override via a Metric View:

```csharp
.WithMetrics(metrics =>
{
    metrics
        .AddMeter("Wolverine:*")
        .AddView(
            instrumentName: "wolverine-execution-time",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0.5, 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000 }
            });
});
```

These boundaries (in milliseconds) put the bulk of Cab's expected handler-execution distribution in well-resolved buckets. Adjust per service — Telemetry's Kafka-driven handlers run faster than Trips's saga-orchestrating handlers.

### Cardinality — the dominant production risk

Every distinct combination of tag values produces a new metric series. The SDK and OTLP receiver handle thousands of series cheerfully but degrade beyond that. Wolverine's `tenant.id` tag is the leading risk in multi-tenant Cab BCs.

Strategies, from least to most aggressive:

- **Drop the tag entirely** via a Metric View `TagKeys` filter — keeps the metric, loses the per-tenant breakdown:
  ```csharp
  metrics.AddView("wolverine-messages-sent", new MetricStreamConfiguration
  {
      TagKeys = new[] { "message.type", "message.destination", "exception.type", "source" }
      // tenant.id omitted
  });
  ```
- **Bucket the tenants** — derive a coarser tag (e.g., tenant tier) at envelope-creation time and tag with the bucketed value instead of the raw tenant ID.
- **Cap the unique values** — a custom processor that drops series beyond a configured cardinality limit. Heavyweight; reach for it only if the bucketing strategy doesn't fit.

Cab's default: keep `tenant.id` on all metrics in Payments and Trips (per-tenant operational visibility matters); drop it via View on Telemetry's high-volume `wolverine-messages-received` (the metric is interesting in aggregate, not per-tenant).

---

## Cross-language metrics from `cab-go`

The Go service emits metrics through the same OTLP pipeline (per `polyglot-go-service` § OpenTelemetry). The dashboard groups them under the `cab-go` resource alongside the .NET services. Custom instruments worth wiring on the Go side:

| Instrument | Type | Purpose |
|---|---|---|
| `matchmaker.kafka_consumer_lag` | Observable gauge | Driver-position topic lag — alerts if catch-up falls behind |
| `matchmaker.spatial_index_size` | Observable gauge | Number of drivers currently in the in-memory index |
| `matchmaker.find_nearest_drivers.duration` | Histogram | gRPC handler latency for `FindNearestDrivers` |

Go's `go.opentelemetry.io/otel/metric` package provides the equivalent of .NET's `Meter` API; the OTLP gRPC exporter ships metrics to the same Aspire dashboard endpoint. The result: a single dashboard view spans .NET-emitted Wolverine metrics and Go-emitted matchmaker metrics, with the resource selector switching between them.

---

## Common pitfalls

- **`AddMeter("Wolverine")` instead of `AddMeter("Wolverine:*")`.** Wolverine's Meter name is `Wolverine:{ServiceName}` (e.g., `Wolverine:CritterCab.Trips`). The exact-match form silently drops every Wolverine metric. The wildcard pattern `"Wolverine:*"` is the correct registration. `observability-tracing`'s bootstrap block carries the same form.

- **Expecting Marten metrics to appear without `TrackEventCounters()`.** Marten ships zero metrics by default — registering the `"Marten"` Meter does nothing on its own. Call `opts.OpenTelemetry.TrackEventCounters()` (or attach custom counters via `ExportCounterOnChangeSets`) to actually emit anything.

- **Expecting Polecat metrics at all.** Polecat ships zero auto-emitted instruments. The `"Polecat"` Meter exists for custom instruments only. Don't troubleshoot "missing Polecat metrics" — they aren't there to find. Use Wolverine's persistence metrics for outbox/inbox state on a Polecat-backed service, and the v3.1 `IDocumentStoreUsageSource` descriptor surface for monitoring-tool discovery.

- **Confusing `wolverine-execution-failure` with `wolverine-messages-failed`.** The constant `MetricsConstants.MessagesFailed` resolves to the string `"wolverine-execution-failure"`, not the obvious `"wolverine-messages-failed"`. Dashboards searching for the latter find nothing. Always reference instrument names by the actual emitted string, not the constant identifier.

- **Confusing `wolverine-execution-time` with `wolverine-effective-time`.** Both are histograms in milliseconds. `wolverine-execution-time` measures handler wall-clock from envelope dequeue to handler completion. `wolverine-effective-time` measures end-to-end from the originating `SentAt` timestamp through transport queueing to handler completion. The difference exposes transport latency. Production dashboards usually want both — execution-time for handler health, effective-time for end-to-end SLO tracking.

- **High-cardinality `tenant.id` blowing up the dashboard.** Wolverine tags every message-related counter and histogram with `tenant.id`. In Cab Payments where the tenant set grows over time, this multiplies metric series count without bound. Use a Metric View `TagKeys` filter to drop the tag on services where per-tenant visibility isn't worth the cardinality, or bucket tenants into tiers before tagging.

- **Treating `TrackConnections` as a metrics setting.** Both Marten's and Polecat's `OpenTelemetryOptions.TrackConnections` settings produce **spans**, not counters. They're documented in `observability-tracing`, not here. Setting them won't affect what the metrics view shows.

- **Forgetting `EnableExtendedProgressionTracking` on a Polecat-backed service that wants daemon health.** Without the opt-in, `pc_event_progression` doesn't carry the `heartbeat`/`agent_status`/`pause_reason`/`running_on_node` columns that CritterWatch reads. Default to enabling it on Cab's Payments service.

- **Building dashboards on `wolverine-inbox-count` and seeing zero in single-store services that previously had `wolverine-inbox-count.payments`.** The `.{databaseName}` suffix appears only when the service uses an ancillary store. The single-store case emits the unsuffixed form. Don't hard-code the suffixed name in a query unless you know the service has multiple stores.

- **Mixing `Meter.CreateCounter<long>` and `Meter.CreateCounter<int>` for the same logical metric across BCs.** The OTel SDK distinguishes by name AND generic argument. A counter named `trips.started` of `int` type and one of `long` type are two different instruments. Settle on `long` as the Cab default for counters; reserve `int` for genuinely small-bounded counts.

- **Letting handler-internal metric state race with concurrent envelope handling.** Per-BC metric state (`TripMetrics.TripsStarted` etc.) is process-wide. The instruments themselves are thread-safe but if you wrap them in custom logic that accumulates state, that wrapper needs to be too. Stick to direct `counter.Add(...)` and `histogram.Record(...)` calls; resist the temptation to add wrapper objects with mutable state.

- **Using Aspire dashboard metrics for production capacity planning.** The dashboard is dev-time visualization with in-memory retention. Production capacity planning needs a real metrics backend (Prometheus via OTLP, Mimir, Azure Monitor) with multi-day retention and PromQL/MetricsQL-style queries. The OTLP exporter lights both up — same instrumentation, different exporters.

---

## See also

**Upstream** — load these first:

- `observability-tracing` — the canonical tracing companion. Same bootstrap shape, same sampling concerns. Both skills register Wolverine's Meter via the wildcard form `AddMeter("Wolverine:*")` — Wolverine's Meter name is per-service-suffixed (`Wolverine:{ServiceName}`), and the wildcard is the correct prefix-match registration.
- `service-bootstrap` — the `Program.cs` shape this skill plugs into. The `AddOpenTelemetry().WithMetrics(...)` block layers cleanly on top of the bootstrap pattern documented there.
- `aspire` — the AppHost foundation. The OTLP endpoint Aspire injects flows into the metric exporter the same way it flows into the trace exporter.

**Sibling skills:**

- `polecat-event-sourcing` — the v3.1 monitoring discovery section here resolves the forward reference from `polecat-event-sourcing` § v3.1 monitoring discovery. The `IDocumentStoreUsageSource` story is observability-side; the schema/configuration side stays in the Polecat skill.
- `polecat-document-store` — Polecat's lack of auto-emitted metrics applies symmetrically to the document-store side. Custom instruments via `opts.OpenTelemetry.Meter` work the same way.
- `polyglot-go-service` — cross-language metrics from `cab-go` flow through the same OTLP pipeline. The matchmaker's Go-side instruments surface in the same dashboard.
- `wolverine-handlers`, `wolverine-messaging-handlers`, `wolverine-grpc-handlers` — the message types and handler shapes that produce the `wolverine-*` metrics documented here.

**Downstream:**

- `testing-advanced` (Phase 4) — verifying metric emission in integration tests. The OTel SDK's `MeterListener` and the in-memory exporter make assertions tractable.
- `cli-jasperfx` — `db-apply` applies the `EnableExtendedProgressionTracking` columns to `pc_event_progression`; `describe` surfaces projection daemon state including the new columns.

**External:**

- [OpenTelemetry .NET Metrics documentation](https://opentelemetry.io/docs/languages/dotnet/instrumentation/#metrics) — the canonical reference for the SDK's metric API and `AddMeter` wildcard semantics.
- [OpenTelemetry semantic conventions for messaging](https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/) — the `messaging.*` tag conventions Wolverine follows.
- [OpenTelemetry semantic conventions for HTTP](https://opentelemetry.io/docs/specs/semconv/http/http-metrics/) — the `http.*` instruments ASP.NET Core and HttpClient instrumentation produce.
- [Aspire dashboard documentation](https://aspire.dev/) — the metrics view, refresh behavior, and OTLP receiver configuration.
- [Wolverine telemetry documentation](https://wolverinefx.net/) — Wolverine's official OTel guidance; verify metric names against current docs at the time of integration.
- [Marten OpenTelemetry options](https://martendb.io/) — the `TrackEventCounters` and `ExportCounterOnChangeSets` API surface.
- [Polecat OpenTelemetry options](https://polecat.jasperfx.net/) — the parallel API for SQL-Server-backed stores.
- ai-skills `observability-metrics` — generic patterns from JasperFx if/when published; complements this skill's Cab specifics.
