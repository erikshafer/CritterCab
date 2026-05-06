---
name: observability-tracing
description: "OpenTelemetry distributed tracing across CritterCab services. Covers the Aspire AddServiceDefaults foundation, layering Wolverine and Marten ActivitySources, the full bootstrap pattern, Wolverine's send/receive/process spans with their semantic attributes, Marten's connection span and verbose events, trace context propagation across gRPC, Kafka, and ASB transports, per-BC ActivitySource conventions, the Aspire dashboard trace view, parent-based head sampling, suppressing traces per endpoint, and the OTel SDK packages that must be added to Directory.Packages.props."
cluster: observability
tags: [opentelemetry, tracing, spans, aspire-dashboard, otlp, wolverine, marten, distributed-tracing, sampling, activity-source]
---

# Observability — Distributed Tracing

Every CritterCab service emits OpenTelemetry traces. A single rider request — arriving over gRPC, dispatching across Kafka to Telemetry, publishing a domain event over ASB to Payments — appears as one distributed trace with spans from every service it touches. The Aspire dashboard visualizes these traces in local development; a production OTLP collector (Jaeger, Grafana Tempo, Azure Monitor) receives them in deployed environments.

Tracing in Cab is built on three layers:

- **Aspire's `AddServiceDefaults()`** — the foundation. Wires the OTel SDK, the OTLP exporter (pointed at the Aspire dashboard in dev), ASP.NET Core instrumentation, and HTTP client instrumentation. Every Cab service calls this first (per `service-bootstrap`).
- **Wolverine's `"Wolverine"` ActivitySource** — emits spans for message send, receive, and handler execution. Every message flowing through a Wolverine handler produces trace spans with semantic messaging attributes.
- **Marten's `"Marten"` ActivitySource** — emits spans for database connections and, in verbose mode, per-command and per-event-append events within those spans.

**The single most important idea:** neither Wolverine nor Marten ship an `AddWolverineInstrumentation()` or `AddMartenInstrumentation()` helper. You register their ActivitySources manually with `.AddSource("Wolverine")` and `.AddSource("Marten")` on the `TracerProviderBuilder`. Miss either one and those spans are silently dropped.

**Prerequisite packages not yet committed:** `Directory.Packages.props` does not include the OpenTelemetry SDK or exporter packages. When the observability bootstrap lands, these must be added.

## When to apply this skill

**Use this skill when:**

- Adding OTel tracing to a new or existing Cab service's `Program.cs`.
- Debugging a distributed flow using the Aspire dashboard trace view.
- Understanding what spans Wolverine and Marten emit and what attributes they carry.
- Adding custom spans or attributes for domain-specific tracing.
- Configuring sampling strategy for production deployments.
- Suppressing traces on high-volume or internal endpoints.

**Do NOT use this skill for:**

- Metrics (counters, histograms) — see `observability-metrics` (Phase 4).
- Aspire orchestration or the dashboard itself — see `aspire` and `cli-aspire`.
- Wolverine handler shapes — see `wolverine-messaging-handlers`, `wolverine-grpc-handlers`.
- Transport-specific configuration — see `wolverine-kafka`, `wolverine-azure-service-bus`.

## Bootstrap pattern

### The full registration

Every Cab service follows this OTel registration pattern in `Program.cs`:

```csharp
builder.AddServiceDefaults();    // Aspire foundation — OTel SDK, OTLP, health checks

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Wolverine")         // Wolverine message send/receive/process spans
            .AddSource("Marten")            // Marten DB connection spans
            .AddSource("CritterCab.Trips"); // Per-BC custom spans (optional)
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Wolverine:*");   // Wolverine message counters and execution histograms (per-service Meter, hence the wildcard)
    });
```

`AddServiceDefaults()` (from the Aspire service defaults project) handles the baseline: the OTel SDK, the OTLP exporter pointed at `OTEL_EXPORTER_OTLP_ENDPOINT` (injected by Aspire), ASP.NET Core request instrumentation, and HTTP client instrumentation. The `.AddOpenTelemetry()` block layers Cab-specific sources on top.

### Why AddSource is manual

Wolverine and Marten both create their `ActivitySource` internally:

```csharp
// Inside Wolverine (WolverineTracing.cs)
public static ActivitySource ActivitySource { get; } = new("Wolverine", ...);

// Inside Marten (MartenTracing.cs)
internal static ActivitySource ActivitySource { get; } = new("Marten", ...);
```

The OTel SDK only collects spans from sources explicitly registered via `.AddSource()`. Without registration, the `ActivitySource.StartActivity()` calls inside Wolverine and Marten return `null` and no spans are created. This is by design — it lets applications opt in to exactly the sources they want. But it means a missing `.AddSource("Wolverine")` silently drops all Wolverine traces.

### Packages to add

The following packages are not yet in `Directory.Packages.props` and must be added when the observability bootstrap lands:

```xml
<PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="{verify-latest}" />
<PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="{verify-latest}" />
<PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="{verify-latest}" />
<PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="{verify-latest}" />
<PackageVersion Include="OpenTelemetry.Instrumentation.GrpcNetClient" Version="{verify-latest}" />
```

Check [OpenTelemetry .NET releases](https://github.com/open-telemetry/opentelemetry-dotnet/releases) for the current stable versions compatible with .NET 10. The Aspire service defaults project may already pull some of these transitively — verify before adding explicit references to avoid version conflicts.

## Wolverine spans

Wolverine emits three primary spans for every message flowing through the pipeline, plus operational spans for infrastructure events.

### Primary spans

| Span name | Activity kind | When emitted |
|---|---|---|
| `"send"` | `Producer` | When a message is published to a transport (Kafka topic, ASB topic/queue, gRPC) |
| `"receive"` | `Consumer` | When a message arrives from a transport and enters the handler pipeline |
| `{MessageType}` | `Internal` | When the handler executes — the span name is the message type name (e.g., `"LocationPing"`, `"TripCompleted"`) |

The `"send"` span is started in the sending pipeline and carries the destination attributes. The `"receive"` span is started when the listener picks up the message. The handler execution span uses the message type as its name, making it immediately identifiable in the Aspire dashboard's trace waterfall.

If the message type cannot be resolved, the handler span falls back to `"process"`.

### Semantic attributes on every span

Wolverine writes these tags on every send, receive, and process span via `envelope.WriteTags(activity)`:

| Attribute key | Value | OTel semantic convention |
|---|---|---|
| `messaging.system` | Transport scheme (e.g., `"kafka"`, `"azureservicebus"`, `"grpc"`) | Messaging conventions |
| `messaging.destination` | Destination URI | Messaging conventions |
| `messaging.message_id` | Envelope ID (GUID) | Messaging conventions |
| `messaging.conversation_id` | Correlation ID | Messaging conventions |
| `messaging.message_type` | Message type name | Custom (Wolverine-specific) |
| `messaging.message_payload_size_bytes` | Payload size in bytes | Messaging conventions |
| `tenant.id` | Tenant ID (when multi-tenancy is active) | Custom |

### Handler-specific attributes

On single-handler chains, Wolverine's code-generated middleware adds:

| Attribute key | Value |
|---|---|
| `message.handler` | Full handler type name (e.g., `CritterCab.Trips.Handlers.LocationPingHandler`) |
| `handler.type` | Same as `message.handler` |

### Saga attributes

When the handler is part of a saga, additional tags are set:

| Attribute key | Value |
|---|---|
| `wolverine.saga.id` | Saga identity value |
| `wolverine.saga.type` | Full saga type name |

### Activity events (within spans)

Wolverine adds events to the current span for significant handler-pipeline outcomes:

| Event name | Meaning |
|---|---|
| `wolverine.envelope.discarded` | Message was deliberately discarded (no handler, or discard policy) |
| `wolverine.error.queued` | Message moved to the dead-letter queue after failures |
| `wolverine.no.handler` | No handler found for the message type |
| `wolverine.envelope.requeued` | Message requeued for retry |
| `wolverine.envelope.retried` | Message retried inline (in-process retry) |
| `wolverine.envelope.rescheduled` | Message scheduled for later retry |
| `wolverine.paused.listener` | Listener paused (circuit breaker triggered) |

These events are visible in the Aspire dashboard's span detail view as timestamped annotations within the parent span. The `wolverine.error.queued` event is the most operationally significant — it means a message failed all retries and landed in the DLQ.

### Operational spans

| Span name | When emitted |
|---|---|
| `wolverine.streaming` | During streaming handler execution |
| `wolverine_node_assignments` | Node agent health-check and assignment evaluation |
| `wolverine.stopping.listener` | Listener draining during shutdown |
| `wolverine.pausing.listener` | Listener paused due to backpressure |
| `wolverine.sending.pausing` | Sender paused due to transport failures |
| `wolverine.sending.resumed` | Sender resumed after circuit breaker reset |

The `wolverine_node_assignments` span can be noisy in long-running services. Suppress it with:

```csharp
opts.Durability.NodeAssignmentHealthCheckTracingEnabled = false;
// Or sample it:
opts.Durability.NodeAssignmentHealthCheckTraceSamplingPeriod = TimeSpan.FromMinutes(10);
```

## Marten spans

### Connection span

Marten emits one span per database session lifecycle:

| Span name | When emitted |
|---|---|
| `marten.connection` | When an `EventTracingConnectionLifetime` wraps a session's database connection |

This span covers the full lifetime of a Marten session — from opening the connection through all commands executed in that session to the final commit or rollback. Individual SQL commands appear as events within this span, not as separate spans.

### Attributes on the connection span

| Attribute key | Value |
|---|---|
| `database.uri` | PostgreSQL connection URI |
| `tenant.id` | Tenant ID (when multi-tenancy is active) |

### Verbose mode events

By default, Marten emits only the connection span. In verbose mode, per-command and per-event events are added within the span:

```csharp
builder.Services.AddMarten(opts =>
{
    opts.OpenTelemetry.TrackConnections = TrackLevel.Verbose;
});
```

| Event name | Meaning |
|---|---|
| `marten.command.execution.started` | Individual `NpgsqlCommand` execution |
| `marten.batch.execution.started` | Batch command execution |
| `marten.batch.pages.execution.started` | Session commit (batch pages) |
| `marten.append.event` | Per-event append (tag: `Type` = event type name) |
| `marten.{role}` | Per-operation (role = `delete`, `update`, etc.; tag: `Type` = document type) |

Verbose mode is useful for diagnosing slow Marten operations but adds significant trace volume. Enable it for targeted debugging sessions, not as a default.

## Trace context propagation across transports

### How it works

Wolverine propagates W3C trace context through its envelope system. When a message is published, the current `Activity.Id` is stored as `Envelope.ParentId`. When the message is received by another service, Wolverine starts a new `Activity` with that `ParentId` as the parent, linking the consumer's span to the producer's trace:

```
Telemetry service                    Kafka                        Dispatch service
┌──────────────────┐          ┌──────────────┐          ┌──────────────────┐
│ send span        │─ParentId─│ Kafka headers│─ParentId─│ receive span     │
│ (Producer)       │          │              │          │ (Consumer)       │
└──────────────────┘          └──────────────┘          │                  │
                                                        │ LocationPing     │
                                                        │ span (Internal)  │
                                                        └──────────────────┘
```

The `ParentId` is carried in transport-specific headers:
- **Kafka:** stored in Kafka message headers alongside other Wolverine envelope metadata.
- **ASB:** stored in ASB `ApplicationProperties` alongside other Wolverine envelope metadata.
- **gRPC:** propagated through gRPC metadata headers using the standard W3C `traceparent` format.

### Correlation ID

In addition to `ParentId`, Wolverine initializes `Envelope.CorrelationId` from `Activity.Current?.RootId`. This means all messages in a causal chain (a request that triggers a handler that publishes an event that triggers another handler) share the same correlation ID — the root trace ID. This is the attribute tagged as `messaging.conversation_id` on every span.

## Per-BC ActivitySource conventions

Each bounded context can register a custom `ActivitySource` for domain-specific spans that don't fit Wolverine's or Marten's built-in instrumentation:

```csharp
// In the Trips service
public static class TripsDiagnostics
{
    public static readonly ActivitySource Source = new("CritterCab.Trips");
}

// Usage in a handler or service
using var activity = TripsDiagnostics.Source.StartActivity("compute-eta");
activity?.SetTag("trip.id", tripId.ToString());
activity?.SetTag("driver.id", driverId.ToString());
// ... compute ETA ...
```

Register the source in `Program.cs`:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Wolverine")
            .AddSource("Marten")
            .AddSource("CritterCab.Trips");
    });
```

The naming convention is `CritterCab.{BcName}` — one ActivitySource per bounded context, mirroring the service naming convention. This keeps the Aspire dashboard's trace source filter clean and lets operators filter to a specific BC's custom spans.

Custom spans should be used sparingly. Wolverine and Marten already instrument the messaging and data-access layers. Add custom spans only for domain-specific operations that are both latency-significant and invisible in the Wolverine/Marten instrumentation — ETA computation, pricing algorithm execution, external API calls to payment processors, etc.

## The Aspire dashboard trace view

In local development, the Aspire dashboard at `https://localhost:17220` (or the port Aspire reports) shows all OTel traces emitted by Cab services.

### Reading a trace

The **Traces** page shows a list of traces with summary information: root span name, duration, span count, and participating services. Clicking a trace opens the waterfall view:

- Each row is a span — `send` spans from the publisher, `receive` spans from the consumer, `{MessageType}` spans for handler execution, `marten.connection` spans for database work.
- Nested spans show parent-child relationships — a gRPC request span contains the Wolverine handler span, which contains the Marten connection span, which contains the ASB send span for the outbound domain event.
- Span attributes are visible in the detail panel — `messaging.destination`, `messaging.message_type`, `message.handler`, etc.

### Filtering

The dashboard supports filtering by:
- **Service name** — show only spans from the Trips service.
- **Trace source** — show only `"Wolverine"` or `"Marten"` spans.
- **Duration** — find slow traces.

For production trace inspection, the data flows to whatever OTLP collector is configured (Jaeger, Tempo, Azure Monitor). The query surface is richer there; the Aspire dashboard is for local dev.

## Sampling strategy

### Parent-based head sampling (the Cab default)

The OTel SDK defaults to `ParentBasedSampler(new AlwaysOnSampler())` — if the incoming request has a `traceparent` header with a sampled flag, the trace continues; if there's no parent, always sample. This is the right default for local development where you want every trace.

For production, configure a lower sampling rate to control trace volume:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetSampler(new ParentBasedSampler(
            new TraceIdRatioBasedSampler(0.1))); // Sample 10% of new traces
    });
```

`ParentBasedSampler` wraps any root sampler — it respects the parent's sampling decision for child spans (so a sampled trace remains fully sampled through all services) and applies the root sampler only to traces that originate at this service.

### Per-endpoint suppression

For high-volume internal endpoints (health checks, node-agent heartbeats) that would drown the trace store, suppress tracing entirely:

```csharp
// Suppress Wolverine's node-agent health-check traces
opts.Durability.NodeAssignmentHealthCheckTracingEnabled = false;

// Suppress traces on a specific Wolverine endpoint
// (via the Endpoint.TelemetryEnabled flag)
opts.ListenToKafkaTopic("telemetry.location-pings")
    .ConfigureEndpoint(e => e.TelemetryEnabled = false);
```

Suppress GPS-ping listener tracing only if the trace volume is genuinely problematic. In local dev, keep everything on — the volume is manageable and the traces are the primary debugging tool.

## Audit tags

Wolverine's `[Audit]` attribute on handler parameters writes custom tags to the current `Activity`:

```csharp
public static class StartTripHandler
{
    public static TripStarted Handle(
        StartTripRequest request,
        [Audit("trip.rider_id")] string riderId)
    {
        // The "trip.rider_id" tag is set on the current span automatically.
        // ...
    }
}
```

The `[Audit]` attribute is code-generated middleware — it calls `Activity.Current?.SetTag("trip.rider_id", riderId)` before the handler body executes. This is a lightweight way to enrich traces with domain-specific values without writing custom spans.

## Common pitfalls

- **Forgetting `.AddSource("Wolverine")` or `.AddSource("Marten")`.** Without registration, the OTel SDK never creates listeners for those ActivitySources. `StartActivity()` returns `null`, no spans are created, and no error is logged. The symptom is "I see ASP.NET Core request spans but no Wolverine handler or Marten database spans in the trace." This is the single most common tracing misconfiguration.

- **Calling `.AddSource()` but misspelling the name.** The source name must match exactly: `"Wolverine"` (capital W) and `"Marten"` (capital M). `"wolverine"` or `"marten"` won't match.

- **Not adding the OTel packages to `Directory.Packages.props`.** The OTel SDK packages are not yet committed. Without them, `AddOpenTelemetry()` won't compile. Add them before implementing the tracing bootstrap.

- **Assuming Aspire's `AddServiceDefaults()` registers Wolverine and Marten sources.** `AddServiceDefaults()` registers ASP.NET Core and HTTP client instrumentation. It does not know about Wolverine or Marten. You must add those sources explicitly.

- **Using `AddMeter("Wolverine")` without the service name suffix.** The actual Wolverine meter name is `"Wolverine:{ServiceName}"`. `AddMeter("Wolverine")` uses prefix matching and catches all Wolverine meters, which is the correct registration. But if you try to query for the meter by exact name, you'll need the full `"Wolverine:{ServiceName}"` form.

- **Enabling Marten verbose tracing in production.** `TrackLevel.Verbose` adds an event for every SQL command and every event append. In a service processing hundreds of messages per second, this generates thousands of trace events per second. Use verbose mode for targeted debugging, not as a default.

- **Suppressing tracing globally instead of per-endpoint.** Setting `TelemetryEnabled = false` on `WolverineOptions` suppresses all Wolverine traces. Use per-endpoint suppression (`endpoint.TelemetryEnabled = false`) to target specific high-volume endpoints.

- **Confusing the Aspire dashboard with a production trace store.** The Aspire dashboard stores traces in memory — they're lost when the dashboard restarts. For production, configure an OTLP collector (Jaeger, Tempo, Azure Monitor) that persists traces. The `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable controls where traces go; Aspire sets it to the dashboard in dev, deployment config sets it to the collector in production.

- **Not propagating trace context in custom HTTP calls.** The OTel `HttpClient` instrumentation (from `AddServiceDefaults`) automatically propagates `traceparent` headers on outbound HTTP calls. But if you use raw `HttpClient` without the DI-registered factory, the instrumentation handler isn't attached and trace context breaks. Always use `IHttpClientFactory`.

- **Adding custom spans for every handler invocation.** Wolverine already emits send/receive/process spans with message type, handler, and messaging attributes. A custom span wrapping the handler body is redundant. Reserve custom spans for domain-specific operations within the handler (external API calls, computation steps) that are invisible in the Wolverine instrumentation.

- **Sampling at 100% in production.** The Cab default (parent-based always-on) is correct for local dev. In production, a rate of 1–10% is typical for services processing hundreds of requests per second. Adjust based on trace-store budget and the operational value of full-fidelity traces.

## See also

### Upstream

- `service-bootstrap` — the `Program.cs` composition pattern where `AddServiceDefaults()` and the OTel registration live. The bootstrap-side decision is "always call `AddServiceDefaults` first; layer Wolverine and Marten sources after."
- `aspire` — Aspire orchestration; the dashboard that visualizes traces in local dev, and the `OTEL_EXPORTER_OTLP_ENDPOINT` injection.
- `wolverine-handlers` — the handler pipeline that Wolverine instruments with send/receive/process spans.

### Sibling skills

- `wolverine-kafka` — Kafka transport; trace context propagated through Kafka message headers.
- `wolverine-azure-service-bus` — ASB transport; trace context propagated through ASB `ApplicationProperties`.
- `wolverine-grpc-handlers` — gRPC transport; trace context propagated through gRPC metadata.
- `identity-acl` — auth middleware emits spans that appear in the same trace pipeline.

### Downstream

- `observability-metrics` (Phase 4) — Wolverine and Marten metrics (counters, histograms) alongside the traces documented here.
- `testing-advanced` (Phase 4) — verifying span and metric emission in integration tests.

### External

- [OpenTelemetry .NET documentation](https://opentelemetry.io/docs/languages/dotnet/)
- [OpenTelemetry Messaging Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/messaging/)
- [Aspire telemetry fundamentals](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry)
- [Aspire dashboard trace visualization](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/explore-traces)
- ai-skills: `opentelemetry-dotnet`, `aspire-observability`
