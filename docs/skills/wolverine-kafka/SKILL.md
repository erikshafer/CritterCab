---
name: wolverine-kafka
description: "Wolverine's Kafka transport against Azure Event Hubs (cloud) and the EH Emulator (local). Covers UseKafka / UseKafkaUsingNamedConnection bootstrap, topic naming, publishing with partition keys, single- and multi-topic listeners, ProcessInline for high-throughput streams, consumer groups, dead letter topics, raw JSON interop, batch processing, and the EH Emulator constraints on admin operations."
cluster: wolverine
tags: [kafka, wolverine, messaging, transport, event-hubs, partitioning, consumer-groups, telemetry, ordering, adr-005]
---

# Wolverine Kafka Transport

Kafka is CritterCab's transport for high-volume, append-only streams — GPS pings from drivers, surge-pricing demand signals, and any flow where ordering within a partition matters more than per-message reliability guarantees. The `transport-selection` skill defines the decision framework; this skill covers the Wolverine wiring that makes it work.

In production Cab runs Kafka protocol against **Azure Event Hubs**. Locally, Aspire orchestrates a **Kafka container** (or optionally the Event Hubs Emulator for EH-specific integration tests). Wolverine connects identically to both — same code, different connection string. The one meaningful difference is that the EH Emulator does not support Kafka admin APIs for topic creation; `AutoProvision()` works against real Kafka but requires the management API (or pre-provisioned topics) against the emulator.

**The single most important idea:** Kafka is a transport wire, not a handler shape. A handler consuming messages from a Kafka topic is a vanilla Wolverine messaging handler — the same `Handle(LocationPing ping)` shape you would write for Azure Service Bus or an in-memory message. The transport choice lives in `Program.cs` routing configuration, never in the handler. The `wolverine-messaging-handlers` skill covers handler shapes; this skill covers the routing and transport plumbing that sits underneath.

## When to apply this skill

**Use this skill when:**

- Wiring a Wolverine service to publish messages to a Kafka topic.
- Wiring a Wolverine service to consume messages from one or more Kafka topics.
- Configuring partition keys, consumer groups, or dead letter topics for Kafka endpoints.
- Setting up raw JSON interop with non-Wolverine Kafka producers or consumers.
- Understanding the Event Hubs / EH Emulator differences that affect bootstrap.

**Do NOT use this skill for:**

- Deciding whether a flow belongs on Kafka vs ASB vs gRPC — see `transport-selection`.
- Writing the handler that processes a Kafka message — see `wolverine-messaging-handlers`.
- Aspire orchestration of the Kafka container — see `aspire`, the Aspire local-dev orchestration skill.
- CLI tooling for inspecting topics and messages — see `cli-kafka-tooling` (Phase 3).
- Azure Service Bus transport wiring — see `wolverine-azure-service-bus` (Phase 3).

## Mental model

```
Telemetry service                           Dispatch service
┌─────────────────┐                        ┌─────────────────┐
│ GPS handler     │──publish──►            │ LocationPing    │
│ (Wolverine)     │           │            │ handler         │
└─────────────────┘           │            └─────────────────┘
                              ▼                     ▲
                    ┌─────────────────┐             │
                    │  Kafka topic    │──consume────┘
                    │  telemetry.     │──consume────┐
                    │  location-pings │             │
                    └─────────────────┘             ▼
                                           ┌─────────────────┐
                                           │ Pricing service │
                                           │ LocationPing    │
                                           │ handler         │
                                           └─────────────────┘
```

The Telemetry service publishes `LocationPing` messages to a Kafka topic partitioned by `driver_id`. Dispatch and Pricing each consume the same topic with independent consumer groups. Each handler is a plain messaging handler — it receives a `LocationPing` and does its work. The Kafka-specific concerns (partitioning, consumer groups, offsets) are configured in `Program.cs`, invisible to the handler.

## Bootstrap

### Aspire-injected connection (the Cab default)

Cab services use Aspire for local orchestration. The AppHost registers Kafka with `AddKafka("kafka")` (see `aspire`), and each consuming service gets `.WithReference(kafka)`. On the service side, `UseKafkaUsingNamedConnection` reads the connection string from `IConfiguration`:

```csharp
// Telemetry service Program.cs
builder.Host.UseWolverine(opts =>
{
    opts.UseKafkaUsingNamedConnection("kafka")
        .AutoProvision();

    opts.PublishMessage<LocationPing>()
        .ToKafkaTopic("telemetry.location-pings");
});
```

`UseKafkaUsingNamedConnection("kafka")` resolves the `"kafka"` connection string from the configuration system — the same key Aspire injects via `WithReference()`. This is the correct pattern for any Aspire-orchestrated service. The optional second and third parameters (`configureConsumers`, `configureProducers`) allow tuning `ConsumerConfig` and `ProducerConfig` at registration time.

### Direct connection

For scripts, test harnesses, or services not managed by Aspire, pass the bootstrap servers directly:

```csharp
opts.UseKafka("localhost:9092")
    .AutoProvision();
```

### AutoProvision

`AutoProvision()` calls the Kafka admin API (`IAdminClient.CreateTopicsAsync`) to create any topics that don't yet exist at startup. This works against a real Kafka broker or the Confluent containers. It **does not work against the Azure Event Hubs Emulator** — the EH Emulator only supports Kafka producer and consumer APIs, not admin APIs. See the "Azure Event Hubs" section below for the workaround.

Optionally pass an `Action<AdminClientConfig>` to tune the admin client used for provisioning:

```csharp
opts.UseKafkaUsingNamedConnection("kafka")
    .AutoProvision(admin =>
    {
        admin.RequestTimeoutMs = 10_000;
    });
```

### ConsumeOnly

Services that only consume from Kafka (never publish) can disable the producer health-check ping:

```csharp
opts.UseKafkaUsingNamedConnection("kafka")
    .ConsumeOnly();
```

This avoids creating a producer connection that would never be used.

## Topic naming convention

Cab topics follow the pattern `<bc>.<descriptive-name>`, lowercase with hyphens separating words within a segment and dots separating the bounded-context prefix from the topic name:

| Topic | Publisher | Consumers | Partition key |
|---|---|---|---|
| `telemetry.location-pings` | Telemetry | Dispatch, Pricing | `driver_id` |
| `telemetry.demand-signals` | Telemetry | Pricing | `zone_id` |
| `pricing.surge-updates` | Pricing | Dispatch | `zone_id` |

This mirrors the proto package hierarchy (`crittercab.<bc>.v<n>`) minus the `crittercab.` prefix — Kafka topics are cluster-scoped, so the org prefix adds no disambiguation value and wastes characters in every log line.

Topics carry a descriptive name rather than a message-type name. A topic like `telemetry.location-pings` may carry `LocationPing` messages today and an enriched `LocationPingV2` tomorrow; the topic name describes the stream, not the current payload shape.

## Publishing

### Named topic routing

The standard pattern maps a message type to a specific topic:

```csharp
opts.PublishMessage<LocationPing>()
    .ToKafkaTopic("telemetry.location-pings");

opts.PublishMessage<DemandSignal>()
    .ToKafkaTopic("telemetry.demand-signals");
```

This is an explicit routing rule: every `LocationPing` published from this service goes to `telemetry.location-pings`. The `ToKafkaTopic` call is the Kafka-specific counterpart to `ToAzureServiceBusTopic` — same position in the routing chain, different wire.

### Convention-based routing

When message types map 1:1 to topics by name, Wolverine can derive the topic name from the message type:

```csharp
opts.PublishAllMessages().ToKafkaTopics();
```

This publishes each message type to a topic named after its Wolverine message identity. Named topic routing is preferred in Cab because topic names follow a `<bc>.<descriptive-name>` convention that doesn't match type names.

### Partition keys

Set a partition key when publishing to control which partition receives the message. Messages with the same partition key land in the same partition and are consumed in order:

```csharp
await bus.PublishAsync(new LocationPing(driverId, lat, lng, timestamp),
    new DeliveryOptions { PartitionKey = driverId.ToString() });
```

If no partition key is set, Wolverine uses the envelope's message ID (a GUID), which distributes messages randomly across partitions. For GPS pings, partitioning by `driver_id` ensures a single driver's location stream stays ordered through the Telemetry -> Dispatch path — critical for computing heading, speed, and ETA.

For surge-pricing demand signals, partitioning by `zone_id` keeps all demand events for a geographic zone ordered, so the Pricing service sees monotonically increasing demand counts without reordering artifacts.

## Listening

### Single-topic listeners

```csharp
opts.ListenToKafkaTopic("telemetry.location-pings");
```

This creates a Kafka consumer that subscribes to the named topic and routes incoming messages through the Wolverine handler pipeline. The handler is a standard messaging handler:

```csharp
public static class LocationPingHandler
{
    public static void Handle(LocationPing ping, ILogger logger)
    {
        // Handler doesn't know this came from Kafka.
        // Partition key, offset, and consumer group are invisible here.
        logger.LogDebug("Ping from driver {DriverId} at {Lat},{Lng}",
            ping.DriverId, ping.Latitude, ping.Longitude);
    }
}
```

### Multi-topic listeners (topic groups)

When a service consumes several related topics, a topic group shares one Kafka consumer across them, reducing consumer-group rebalance churn:

```csharp
opts.ListenToKafkaTopics(
    "telemetry.location-pings",
    "telemetry.demand-signals");
```

Each topic still routes to its own handler based on message type. The difference is operational: one consumer connection, one consumer-group membership, fewer TCP sockets against the broker.

### ProcessInline for high-throughput streams

For streams like GPS pings where throughput matters more than durability guarantees, `ProcessInline()` bypasses the durable inbox and processes messages synchronously in the Kafka consumer loop:

```csharp
opts.ListenToKafkaTopic("telemetry.location-pings")
    .ProcessInline();
```

Without `ProcessInline()`, Wolverine stores incoming messages in the durable inbox (the PostgreSQL or SQL Server-backed transactional inbox) before processing. That's the right default for domain events on ASB where reliability trumps throughput. For GPS pings arriving at hundreds per second per driver, the inbox write is unnecessary overhead — a lost ping is replaced by the next one in seconds.

### Batch processing

For handlers that benefit from processing many messages at once — aggregating GPS pings into a per-driver summary, or computing demand across a zone — Wolverine supports batch consumption:

```csharp
opts.ListenToKafkaTopic("telemetry.location-pings");
opts.BatchMessagesOf<LocationPing>();
```

The handler receives an array:

```csharp
public static class LocationPingBatchHandler
{
    public static void Handle(LocationPing[] pings, ILogger logger)
    {
        var byDriver = pings.GroupBy(p => p.DriverId);
        foreach (var group in byDriver)
        {
            logger.LogDebug("Batch of {Count} pings for driver {DriverId}",
                group.Count(), group.Key);
        }
    }
}
```

Batch processing reduces per-message handler invocation overhead and pairs naturally with high-volume Kafka topics where individual-message processing is wasteful.

## Consumer groups

### Default group ID

Wolverine sets the Kafka consumer group ID to the **service name** (`WolverineOptions.ServiceName`) by default. In Cab, each service has a unique name, so Dispatch and Pricing each get their own consumer group on `telemetry.location-pings` automatically — no explicit configuration needed for the standard fan-out pattern.

### Transport-level override

Override the default group ID for all topics in the service:

```csharp
opts.UseKafkaUsingNamedConnection("kafka")
    .ConfigureConsumers(c => c.GroupId = "dispatch-telemetry");
```

### Per-topic override

Override the group ID for a single topic. Note that `ConfigureConsumer` on a topic **replaces** the parent `ConsumerConfig` entirely rather than merging — bootstrap servers are inherited automatically, but any other parent-level settings must be re-applied:

```csharp
opts.ListenToKafkaTopic("telemetry.location-pings")
    .ConfigureConsumer(config =>
    {
        config.GroupId = "dispatch-location-group";
        config.AutoOffsetReset = AutoOffsetReset.Latest;
    });
```

### GroupId stamping on envelopes

By default, Wolverine stamps the consumer group ID onto `Envelope.GroupId` for every received message. If your handler cascades outbound messages and you want them to carry a business-meaningful partition key instead, disable the stamping:

```csharp
opts.ListenToKafkaTopic("telemetry.location-pings")
    .DisableConsumerGroupIdStamping();
```

This is also required when using global partitioned aggregate processing, where the originating partition key must propagate through cascaded messages.

## Serialization and interop

### Default envelope serialization

Wolverine's default serialization wraps the message body in the Kafka message value and stores all envelope metadata (message ID, correlation ID, content type, message type name) as UTF-8-encoded Kafka message headers. Both sides must be Wolverine services. This is the Cab default for service-to-service Kafka communication.

### Raw JSON interop

When one side is not a Wolverine service — a third-party GPS device pushing JSON directly to Kafka, or an analytics pipeline consuming raw JSON — use raw JSON mode.

Publisher side (outbound):

```csharp
opts.PublishMessage<LocationPing>()
    .ToKafkaTopic("telemetry.location-pings")
    .PublishRawJson();
```

Listener side (inbound — must declare the expected message type):

```csharp
opts.ListenToKafkaTopic("telemetry.location-pings")
    .ReceiveRawJson<LocationPing>();
```

Raw JSON mode strips all Wolverine envelope headers. The listener must know the message type at configuration time because there is no `message-type` header to resolve it dynamically.

### Custom envelope mapper

For full control over how Wolverine maps to and from Kafka `Message<string, byte[]>`, implement `IKafkaEnvelopeMapper` and register it on the listener:

```csharp
opts.ListenToKafkaTopic("telemetry.location-pings")
    .UseInterop((endpoint, runtime) => new TelemetryKafkaMapper(endpoint));
```

This is an escape hatch for CloudEvents, Avro with a schema registry, or other wire formats that don't fit the default mapping or the raw JSON shortcut.

### Schema Registry serializers

Wolverine ships `SchemaRegistryAvroSerializer` and `SchemaRegistryJsonSerializer` in the Kafka transport package for Confluent Schema Registry integration. These are outside Cab's current scope — Cab uses Wolverine's default JSON serialization. The `protobuf-contracts` skill's forward-looking note on protobuf-as-unified-schema-language may revisit serialization in a future phase; until then, default JSON is the right choice.

## Dead letter topics and error handling

### Enabling native dead letter topics

Kafka has no built-in dead-letter queue. Wolverine implements dead-letter routing as a separate Kafka topic. Opt in per listener:

```csharp
opts.ListenToKafkaTopic("telemetry.location-pings")
    .EnableNativeDeadLetterQueue();
```

The default DLT topic name is `wolverine-dead-letter-queue`. Override it globally:

```csharp
opts.UseKafkaUsingNamedConnection("kafka")
    .DeadLetterQueueTopicName("crittercab-dlq");
```

When a message fails and the error policy routes it to the error queue, Wolverine produces the message to the DLT topic with four diagnostic headers: `exception-type`, `exception-message`, `exception-stack`, and `failed-at` (Unix epoch milliseconds).

### Retry policies

Retry and error-handling policies are Wolverine-native, not Kafka-specific. Combine retries with dead-letter routing:

```csharp
opts.Policies.OnException<TelemetryProcessingException>()
    .RetryTimes(3)
    .Then.MoveToErrorQueue();
```

`MoveToErrorQueue()` sends the failed message to the DLT when retries are exhausted. Without `EnableNativeDeadLetterQueue()` on the listener, `MoveToErrorQueue` routes to Wolverine's database-backed dead-letter storage instead of a Kafka topic.

### Circuit breakers

Each listener supports a circuit breaker that pauses consumption when failures exceed a threshold:

```csharp
opts.ListenToKafkaTopic("telemetry.location-pings")
    .CircuitBreaker(cb =>
    {
        cb.MinimumThreshold = 20;
        cb.PauseTime = TimeSpan.FromMinutes(1);
    });
```

When the circuit opens, the listener stops pulling messages. After `PauseTime` elapses, Wolverine resumes consumption. This prevents a downstream outage from causing unbounded reprocessing of failing messages.

### Poison pill handling

If a message fails deserialization (a poison pill), Wolverine commits the offset past it to avoid blocking the consumer forever. The message is lost unless you pair deserialization with a DLT. For Kafka streams like GPS pings, a single lost message is less damaging than a stuck consumer. For streams where every message matters, enable the native DLT and monitor it.

## Azure Event Hubs — cloud and local

### What changes between environments

Nothing in the handler or routing code. Wolverine speaks Kafka protocol; Azure Event Hubs accepts Kafka protocol. The only difference is the connection string:

| Environment | Bootstrap servers | Authentication |
|---|---|---|
| Local (Kafka container via Aspire) | `localhost:<port>` | None |
| Local (EH Emulator) | `localhost:<port>` | SASL/PLAIN |
| Azure (Event Hubs) | `<namespace>.servicebus.windows.net:9093` | SASL/OAUTHBEARER or SASL/PLAIN with connection string |

Aspire injects the correct connection string per environment via `UseKafkaUsingNamedConnection("kafka")`, so `Program.cs` never hard-codes an address.

### EH Emulator constraints

The Event Hubs Emulator supports Kafka **producer and consumer** APIs but **not the Kafka admin API**. This means:

- `AutoProvision()` will fail against the emulator because it calls `IAdminClient.CreateTopicsAsync()`.
- Topics must be pre-provisioned through the EH management plane or pre-created in the emulator configuration.
- In local development, Aspire's `AddKafka("kafka")` starts a real Kafka container (not the EH Emulator), which does support admin APIs. This is the simplest path for local dev — save the EH Emulator for integration tests that need to verify Event Hubs-specific behavior.

When targeting the EH Emulator specifically (e.g., in a CI pipeline), omit `AutoProvision()` and provision topics through the emulator's REST management API or Azure CLI.

### ConfigureClient for Event Hubs authentication

Azure Event Hubs over Kafka protocol requires SASL configuration. In non-Aspire deployments where you manage the connection yourself:

```csharp
opts.UseKafka("<namespace>.servicebus.windows.net:9093")
    .ConfigureClient(config =>
    {
        config.SecurityProtocol = SecurityProtocol.SaslSsl;
        config.SaslMechanism = SaslMechanism.Plain;
        config.SaslUsername = "$ConnectionString";
        config.SaslPassword = eventHubsConnectionString;
    });
```

`ConfigureClient` applies to `ConsumerConfig`, `ProducerConfig`, and `AdminClientConfig` simultaneously. In Aspire-managed environments the SASL configuration is injected automatically; this override is for direct Azure deployments.

## Tombstones and log compaction

Kafka supports log compaction: retaining only the latest message per key. To delete a key from a compacted topic, publish a **tombstone** — a null-valued message with the target key:

```csharp
await bus.PublishAsync(new KafkaTombstone(driverId.ToString()));
```

`KafkaTombstone` is a Wolverine type that produces a Kafka message with `Value = null` and `Key` set to the tombstone's key. This is relevant for topics like a hypothetical `telemetry.driver-status` where compaction retains only the latest status per driver and tombstones remove entries for drivers who go offline permanently.

Most Cab Kafka topics (GPS pings, demand signals) use time-based retention, not compaction. Tombstones are the exception, not the rule.

## Tracing

Wolverine's Kafka transport propagates OpenTelemetry trace context through Kafka message headers automatically. A publish operation starts a span; the consumer continues the same trace. The Aspire dashboard shows these traces in local development — a single request from a rider's phone can be followed through gRPC into Dispatch, across Kafka into Telemetry, and back. Full OTel configuration — exporters, sampling, custom attributes — is covered in `observability-tracing` (Phase 3).

## Common pitfalls

- **Calling AutoProvision against the Event Hubs Emulator.** The EH Emulator does not support Kafka admin APIs. `AutoProvision()` will throw. Use Aspire's `AddKafka` for local dev (which starts a real Kafka container) and provision topics through the management plane for EH Emulator environments.

- **Forgetting a partition key on ordered streams.** Without a partition key, Wolverine uses the envelope's GUID, scattering messages randomly across partitions. GPS pings without `PartitionKey = driverId` lose their per-driver ordering guarantee. Always set a partition key for streams where ordering matters.

- **Assuming ConfigureConsumer merges with the parent.** `ConfigureConsumer` on a per-topic listener **replaces** the parent `ConsumerConfig`. Bootstrap servers are auto-inherited, but other settings (SASL, timeouts) from the transport-level config are lost. Re-apply them in the per-topic override if needed.

- **Using Kafka for domain events that need dead-lettering.** Kafka is append-only; dead-letter routing is a Wolverine-layer construct that produces to a separate topic. Azure Service Bus has native dead-letter queues with built-in inspection, replay, and session support. If your flow needs robust DLQ semantics, it probably belongs on ASB per `transport-selection`.

- **Putting transport concerns in the handler.** A handler should never reference `KafkaTopic`, partition IDs, or offsets. If you need envelope metadata (e.g., the partition key for a cascaded message), access it through `Envelope` — but question whether the handler truly needs it or whether the routing configuration should handle it.

- **Mixing ProcessInline with durable-inbox expectations.** `ProcessInline()` skips the durable inbox. If the service crashes mid-processing, the message is lost (Kafka has committed the offset but the handler didn't finish). This is acceptable for GPS pings; it is not acceptable for payment events. Match the durability to the flow.

- **Using a single consumer group across services.** Wolverine defaults the consumer group ID to the service name, which is correct — each service gets independent consumption. If you override the group ID to match another service, both services share a single consumer group and each message goes to only one of them (competing consumers), breaking the fan-out.

- **Hard-coding bootstrap servers in Program.cs.** Aspire injects the connection string via `IConfiguration`. Use `UseKafkaUsingNamedConnection("kafka")` so the same code works locally and in Azure without environment-specific branching.

- **Forgetting ReceiveRawJson requires a type parameter.** Raw JSON mode strips the Wolverine message-type header. The listener must declare the expected type at configuration time with `ReceiveRawJson<T>()`. Without it, Wolverine cannot resolve a handler for the message.

- **Publishing high-volume streams to ASB instead of Kafka.** GPS pings at hundreds per second per driver will overwhelm ASB's per-message cost and throughput ceiling. The `transport-selection` decision framework routes high-volume append-only streams to Kafka. Revisit that skill if uncertain.

- **Ignoring the poison-pill commit behavior.** When deserialization fails, Wolverine commits past the bad message to avoid blocking the consumer. If you need to capture these failures, enable the native DLT — otherwise the message vanishes silently.

- **Disabling AutomaticFailureAcks manually.** Wolverine's `UseKafka` and `UseKafkaUsingNamedConnection` both set `EnableAutomaticFailureAcks = false` because automatic acks don't interact correctly with Kafka serialization failures. Don't re-enable this flag.

## See also

### Upstream

- `transport-selection` — the decision framework that routes flows to Kafka vs ASB vs gRPC. Read this first to understand why a flow lands on Kafka.
- `wolverine-messaging-handlers` — the handler shape for all messaging transports, including Kafka. Handlers don't change based on transport.
- `service-bootstrap` — the `Program.cs` composition pattern that `UseKafkaUsingNamedConnection` extends.
- `aspire` — Aspire orchestration of the Kafka container (`AddKafka("kafka")`) and connection-string injection.

### Sibling skills

- `wolverine-azure-service-bus` (Phase 3) — ASB transport wiring; the other messaging transport Cab uses, optimized for domain events with rich DLQ support.
- `cli-kafka-tooling` (Phase 3) — kcat, console tools, and Aspire dashboard for inspecting Kafka topics and messages.

### Downstream

- `observability-tracing` (Phase 3) — full OTel pipeline configuration, including trace propagation across Kafka.
- `wolverine-sagas` (Phase 4) — long-running processes that may span Kafka and ASB transports.
- `testing-advanced` (Phase 4) — integration tests against Kafka using `Testcontainers.Kafka`.

### External

- [Wolverine Kafka transport docs](https://wolverinefx.net/guide/messaging/transports/kafka.html)
- [Azure Event Hubs for Apache Kafka](https://learn.microsoft.com/en-us/azure/event-hubs/azure-event-hubs-kafka-overview)
- ADR-005 — transport selection rationale
- ai-skills: `wolverine-integrations-kafka`
