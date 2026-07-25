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
│ Ingest handler  │──publish──►            │ DriverLocation  │
│ (Wolverine)     │           │            │ handler         │
└─────────────────┘           │            └─────────────────┘
                              ▼                     ▲
                    ┌─────────────────┐             │
                    │  Kafka topic    │──consume────┘
                    │  telemetry.     │──consume────┐
                    │  driver-        │             │
                    │  location-      │             ▼
                    │  updated        │    ┌─────────────────┐
                    └─────────────────┘    │ Pricing service │
                                           │ (future)        │
                                           │ handler         │
                                           └─────────────────┘
```

The Telemetry service publishes `DriverLocationUpdated` messages to a Kafka topic partitioned by `driverId`. Dispatch consumes it into its `AvailableDriver` view; any future consumer would read the same topic under its own consumer group. Each handler is a plain messaging handler — it receives the message and does its work. The Kafka-specific concerns (partitioning, consumer groups, offsets, cold-start position) are configured in `Program.cs`, invisible to the handler.

> **Reconciled against shipped code 2026-07-24.** Both halves of this topic now run: Telemetry publishes (PR C) and Dispatch consumes (PR D). Every named topic, message type and handler in this skill exists in the codebase. Where a section illustrates a mechanic Cab does **not** currently use — `ProcessInline`, batching, raw JSON, tombstones, custom envelope mappers — it says so inline.

## Bootstrap

### Aspire-injected connection (the Cab default)

Cab services use Aspire for local orchestration. The AppHost registers Kafka with `AddKafka("kafka")` (see `aspire`), and each consuming service gets `.WithReference(kafka)`. On the service side, `UseKafkaUsingNamedConnection` reads the connection string from `IConfiguration`:

```csharp
// Telemetry service Program.cs
builder.Host.UseWolverine(opts =>
{
    opts.UseKafkaUsingNamedConnection("kafka")
        .AutoProvision();

    opts.PublishMessage<DriverLocationUpdated>()
        .ToKafkaTopic("telemetry.driver-location-updated");
});
```

`UseKafkaUsingNamedConnection("kafka")` resolves the `"kafka"` connection string from the configuration system — the same key Aspire injects via `WithReference()`. This is the correct pattern for any Aspire-orchestrated service. The optional second and third parameters (`configureConsumers`, `configureProducers`) allow tuning `ConsumerConfig` and `ProducerConfig` at registration time.

### Direct connection

For non-Aspire scenarios (scripts, test harnesses), use `opts.UseKafka("localhost:9092")` directly. See ai-skills `wolverine-integrations-kafka` § Setup and connection for the full configuration surface (`ConfigureClient`, `ConfigureConsumers`, `ConfigureProducers`).

### AutoProvision

`AutoProvision()` calls the Kafka admin API to create topics at startup. **It does not work against the Azure Event Hubs Emulator** — the EH Emulator only supports Kafka producer and consumer APIs, not admin APIs. See the "Azure Event Hubs" section below for the workaround. Optionally pass an `Action<AdminClientConfig>` to tune the admin client.

### ConsumeOnly

For services that only consume (never publish), call `.ConsumeOnly()` to skip the producer connection. Generic mechanic — see ai-skills `wolverine-integrations-kafka` § Consume-only mode.

## Topic naming convention

**Governed by [ADR-019](../../decisions/019-transport-agnostic-topic-naming.md).** Cab topics are named `<source-bc>.<event-name-kebab>` — the source bounded context's slug, a dot, then the event name in kebab-case. The same rule applies on every transport; Kafka does not get its own convention.

| Topic | Publisher | Consumers | Partition key | Status |
|---|---|---|---|---|
| `telemetry.driver-location-updated` | Telemetry | Dispatch (slice 5) | `driverId` | **Shipped** |

This mirrors the proto package hierarchy (`crittercab.<bc>.v<n>`) minus the `crittercab.` prefix — Kafka topics are cluster-scoped, so the org prefix adds no disambiguation value and wastes characters in every log line.

> **Corrected 2026-07-24 (first Kafka topic shipped).** This section previously proposed a *different* rule for Kafka — `<bc>.<descriptive-name>`, on the reasoning that a topic should describe the stream rather than the current payload shape, illustrated with speculative topics (`telemetry.location-pings`, `telemetry.demand-signals`, `pricing.surge-updates`) that were never published. ADR-019 weighed that argument on its merits and rejected it for Cab specifically: contract versioning lives in the proto package path (ADR-009), so payload evolution is handled a layer below the topic name, and a break large enough to make the event name misleading needs a new proto package and a deliberate consumer migration anyway. Do not reintroduce the descriptive-name rule; the topics above are the shipped reality.

## Publishing

### Named topic routing

Map a message type to a specific topic via `PublishMessage<T>().ToKafkaTopic("...")`. Cab's standard pattern uses one routing rule per Cab message type per service:

```csharp
opts.PublishMessage<DriverLocationUpdated>()
    .ToKafkaTopic("telemetry.driver-location-updated");
```

The shipped Telemetry rule carries three more calls, each load-bearing — see § Serialization for `UseProtobufSerialization` and § Common pitfalls for why `SendInline` alone is not enough:

```csharp
opts.Durability.UseSyncRetryBlock = true;   // process-global

opts.PublishMessage<DriverLocationUpdated>()
    .ToKafkaTopic("telemetry.driver-location-updated")
    .SendInline()             // await the broker ack, don't batch
    .UseIdempotentProducer()  // enable.idempotence=true, acks=all
    .UseProtobufSerialization();
```

For the broader routing surface (`Specification` for partition count + replication factor, named brokers for multi-region), see ai-skills `wolverine-integrations-kafka` § Topic binding.

### Convention-based routing

Wolverine supports `opts.PublishAllMessages().ToKafkaTopics()` for derive-topic-name-from-message-type publishing. **Cab does not use this.** ADR-019's `<source-bc>.<event-name-kebab>` is close enough to a type-name derivation to make the shortcut tempting, and that is exactly why it is worth declining: the derivation would bind a wire-visible topic name to a C# type name, so a routine refactor rename would silently repoint the producer at a new topic while consumers stayed on the old one. The BC prefix also has no type to derive from. Named topic routing (above) keeps the topic an explicit, reviewable string — which is what a cross-BC contract should be.

### Partition keys

Set a partition key when publishing to control which partition receives the message. Messages with the same partition key land in the same partition and are consumed in order:

```csharp
await bus.PublishAsync(update, new DeliveryOptions { PartitionKey = update.DriverId });
```

If no partition key is set, Wolverine uses the envelope's message ID (a GUID), which distributes messages randomly across partitions. For GPS pings, partitioning by `driver_id` ensures a single driver's location stream stays ordered through the Telemetry -> Dispatch path — critical for computing heading, speed, and ETA.

For surge-pricing demand signals, partitioning by `zone_id` keeps all demand events for a geographic zone ordered, so the Pricing service sees monotonically increasing demand counts without reordering artifacts.

## Listening

> **Rewritten 2026-07-24 from shipped code (PR D).** This section and § Consumer groups previously illustrated with a `LocationPing` → `telemetry.location-pings` pairing that never existed — `LocationPing` is the gRPC *ingest* message (W006 §6.2) and is never a Kafka payload. Everything below is now the real Dispatch consumer.

### The shipped listener, whole

Cab's only Kafka listener lives in `CritterCab.Dispatch`. Read it as the reference for every clause:

```csharp
opts.UseKafkaUsingNamedConnection("kafka");   // no AutoProvision — the producer owns the topic

opts.ListenToKafkaTopic("telemetry.driver-location-updated")
    .ConfigureConsumer(c => c.GroupId = "dispatch")
    .BeginAtLatest()
    .UseProtobufSerialization()
    .DefaultIncomingMessage<DriverLocationUpdated>();
```

```csharp
public static class DriverLocationUpdatedHandler
{
    public static async Task Handle(
        DriverLocationUpdated message, IDocumentSession session, CancellationToken ct)
    { /* ... */ }
}
```

The handler is a **vanilla Wolverine messaging handler** — nothing in it references Kafka, a topic, an offset or a partition. Handler *shape* is `wolverine-messaging-handlers`' subject, not this skill's.

### Handler discovery is a precondition for deserialization

The single most surprising failure mode, and the first thing to suspect when a listener goes quiet with **no error at all**.

Wolverine resolves an incoming message's wire type name through `HandlerGraph._messageTypes`, which is populated *from discovered handler chains*. If no handler is discovered for a message type, the pipeline short-circuits to `NoHandlerContinuation` and the payload is **never deserialized** — so a handler that is made `internal`, renamed out of convention, or moved to an unscanned assembly does not produce a deserialization error. It produces silence.

Note also that `CustomizeHandlerDiscovery(...)` is **additive**: Wolverine appends its built-in conventions (`*Handler`, `*Consumer`, `Saga`, `IWolverineHandler`, `[WolverineHandler]`) at bootstrap, *after* your customization, and the include filters are OR'd. Dispatch registers a `*Automation` suffix and still discovers `DriverLocationUpdatedHandler` by the built-in one. Only `DisableConventionalDiscovery()` changes this.

### Declaring the incoming message type

`.DefaultIncomingMessage<T>()` pins the message type at the endpoint. It is **hardening, not a requirement**: Wolverine's default Kafka envelope mapper writes a `message-type` header on publish, so a Wolverine producer's type resolves from the wire without it. Declaring it *replaces* that header mapping with a constant, which makes the listener immune to a producer that omits or misspells the header — appropriate for a single-type topic, wrong for a shared one.

There is **no** `ListenToKafkaTopic<T>(...)` generic overload and no `.ReceivesMessage<T>()` fluent method; `DefaultIncomingMessage<T>()` is the API.

### Cold-start position — `BeginAtLatest()` / `BeginAtEarliest()`

Both apply **only when the consumer group has no committed offset**. Once the group commits, it resumes from its committed position and these are ignored.

State one explicitly. `ConfigureConsumer` replaces the parent `ConsumerConfig`, so leaving it unset falls through to Confluent's default (`Latest`) rather than to anything Wolverine chose for you.

Cab's telemetry consumer uses `BeginAtLatest()`: a stale position is worthless (W006 §6.4 evicts positions older than three heartbeats), and the heartbeat refills the view within `heartbeatIntervalSeconds` regardless — whereas `BeginAtEarliest()` would replay however many hours of retained telemetry the topic holds on a first deploy to arrive at the same state. Prefer `BeginAtEarliest()` instead when a topic carries facts that are *not* self-healing and a cold-start gap would lose them permanently.

**Testing consequence:** under `BeginAtLatest()`, anything produced before the consumer group finishes joining is legitimately missed. Invisible in production; a guaranteed hang in a test that produces once and waits. Cab's `DispatchKafkaTestFixture` performs a warm-up handshake — producing throwaway records until one is observably handled — before any test runs. Do not paper over this with a fixed `Thread.Sleep`.

### Multi-topic listeners (topic groups)

For consuming several related topics from a single consumer (reducing rebalance churn), use `opts.ListenToKafkaTopics("...", "...")`. Each topic still routes to its own handler based on message type. Generic mechanic — see ai-skills `wolverine-integrations-kafka` § Topic binding.

### ProcessInline for high-throughput streams

For streams like GPS pings where throughput matters more than durability guarantees, `ProcessInline()` bypasses the durable inbox and processes messages synchronously in the Kafka consumer loop:

```csharp
opts.ListenToKafkaTopic("telemetry.driver-location-updated")
    .ProcessInline();
```

Without `ProcessInline()`, Wolverine stores incoming messages in the durable inbox (the PostgreSQL or SQL Server-backed transactional inbox) before processing. That's the right default for domain events on ASB where reliability trumps throughput. For a throttled position feed, a lost message is replaced by the next heartbeat in seconds, so the inbox write buys little.

**Cab's shipped listener does not use it.** `telemetry.driver-location-updated` is already throttled to cell-change-or-heartbeat, not raw GPS, so its volume does not justify giving up the inbox. Reach for `ProcessInline()` when a topic carries genuinely per-ping volume — and note the durability trade in § Common pitfalls.

### Batch processing

For handlers that benefit from processing many messages at once, use `opts.BatchMessagesOf<T>()` paired with the listener:

```csharp
opts.ListenToKafkaTopic("telemetry.driver-location-updated");
opts.BatchMessagesOf<DriverLocationUpdated>();

public static class DriverLocationUpdatedBatchHandler
{
    public static void Handle(DriverLocationUpdated[] updates, ILogger logger)
    {
        var byDriver = updates.GroupBy(u => u.DriverId);
        foreach (var group in byDriver)
            logger.LogDebug("Batch of {Count} positions for driver {DriverId}",
                group.Count(), group.Key);
    }
}
```

Batch processing pairs naturally with high-volume topics where per-message invocation overhead is wasteful. **Cab does not currently batch** — the shipped consumer handles one position at a time, because its per-message work is a single guarded upsert and batching would only complicate the last-writer-wins guard. Illustrated here as the mechanic, not as Cab's practice.

## Consumer groups

### Default group ID

Wolverine sets the Kafka consumer group ID to the **service name** by default — `ConsumerConfig.GroupId ??= runtime.Options.ServiceName` in `KafkaTransport`. Each Cab service has a unique name, so several services listening to `telemetry.driver-location-updated` would each get their own consumer group automatically, which is the standard fan-out pattern.

**Cab's shipped listener pins it explicitly anyway** (`c.GroupId = "dispatch"`), and the reason is worth copying: a group id derived from the service name silently changes if the service is ever renamed, and a *new* group under `BeginAtLatest()` starts at the tail — quietly discarding the old group's committed position with no error. A literal is cheap insurance against an invisible offset reset.

### Transport-level override

`opts.UseKafkaUsingNamedConnection("kafka").ConfigureConsumers(c => c.GroupId = "...")` overrides the group ID for all topics in the service. Generic — see ai-skills `wolverine-integrations-kafka` § Consumer groups.

### Per-topic override

`ListenToKafkaTopic(...).ConfigureConsumer(...)` overrides the group ID and other consumer settings for a single topic. **Important:** the per-topic call **replaces** the parent `ConsumerConfig` entirely — bootstrap servers are inherited automatically, but other parent-level settings must be re-applied. See ai-skills `wolverine-integrations-kafka` § Setup and connection (the "completely overwrites" note).

### GroupId stamping on envelopes

Wolverine stamps the consumer group ID onto `Envelope.GroupId` for every received message by default. To disable (e.g., when cascading messages should carry a business-meaningful partition key), call `.DisableConsumerGroupIdStamping()` on the listener. Required for global partitioned aggregate processing — see ai-skills `wolverine-integrations-kafka` § Consumer groups + § Partition-based sequential processing.

## Serialization and interop

### Choosing a serializer

Cab uses **two** serializers on Kafka, chosen by whether the payload is a generated protobuf contract:

| Payload | Serializer | How |
|---|---|---|
| A protoc-generated type from `protos/` | Binary protobuf | `.UseProtobufSerialization()` on the publishing rule |
| A hand-authored C# record | Wolverine's default envelope JSON | nothing to configure |

**Endpoint-scoped, always.** `WolverineFx.Protobuf` ships two overloads: one on `WolverineOptions` that replaces the app's `DefaultSerializer` globally, and one on an endpoint configuration. Use the endpoint one — the global overload would take the service's HTTP surface off JSON with it.

```csharp
opts.PublishMessage<DriverLocationUpdated>()
    .ToKafkaTopic("telemetry.driver-location-updated")
    .UseProtobufSerialization();
```

The Kafka wire payload is `Message<string, byte[]>`, so binary protobuf is natively expressible — no base64 wrapping. **A consumer of a protobuf topic must carry the same serializer on its listener endpoint**: `ProtobufMessageSerializer.ReadFromData(byte[])` throws `NotSupportedException`, and only the `(Type, Envelope)` overload works, so the type information has to come from the endpoint configuration.

> **Corrected 2026-07-24 (first Kafka topic shipped).** This section previously read "Cab uses Wolverine's default JSON serialization" and deferred protobuf to "a future phase," cross-referencing `protobuf-contracts`' forward-looking note. That phase arrived: `DriverLocationUpdated` is generated from the `.proto` that ADR-009 makes the contract of record, and JSON-serializing a protoc-generated class produces bloated, non-canonical output while leaving the contract governing the C# type but not the wire.

### Default envelope serialization

For non-protobuf payloads, Wolverine's default envelope serialization carries the message body in the Kafka value and envelope metadata (message ID, correlation ID, content type, message type name) in UTF-8 headers. Both sides must be Wolverine services.

### Raw JSON interop

For interop with non-Wolverine producers/consumers (third-party GPS devices, analytics pipelines), use raw JSON mode. Listener must declare the expected message type at config time:

```csharp
opts.PublishMessage<TMessage>().ToKafkaTopic("<source-bc>.<event-name-kebab>").PublishRawJson();
opts.ListenToKafkaTopic("<source-bc>.<event-name-kebab>").ReceiveRawJson<TMessage>();
```

Raw JSON strips Wolverine envelope headers — see ai-skills `wolverine-integrations-kafka` § Raw JSON interoperability for the full publisher/listener semantics.

**⚠ `PublishRawJson()` silently destroys `DeliveryOptions.PartitionKey`.** Raw-JSON mode installs `JsonOnlyMapper`, whose `MapEnvelopeToOutgoing` assigns `outgoing.Key = envelope.GroupId` — and it runs *after* the transport has already set the key from `PartitionKey`. Combining the two produces records whose key is the consumer group id (or null), which does not fail, does not log, and only shows up as lost per-partition ordering. Never combine `PublishRawJson()` with a partition key; if a raw-JSON topic needs keyed ordering, set `GroupId` instead and document why.

### Custom envelope mapper

For wire formats that don't fit the default mapping or raw JSON (CloudEvents, Avro with a schema registry, custom wrappers), implement `IKafkaEnvelopeMapper` and register via `.UseInterop(...)` on the listener. Escape hatch — Cab does not currently use it.

### Schema Registry serializers

Wolverine ships `SchemaRegistryAvroSerializer` and `SchemaRegistryJsonSerializer` in the Kafka transport package for Confluent Schema Registry integration. These remain outside Cab's scope: Cab's schema authority is the `protos/` tree under ADR-009, not a registry, and `.UseProtobufSerialization()` (above) already gives binary protobuf on the wire without one. Revisit only if a non-Cab producer needs registry-mediated compatibility checks.

## Dead letter topics and error handling

### Enabling native dead letter topics

Kafka has no built-in DLQ. Wolverine implements dead-letter routing as a separate Kafka topic; opt in per listener with `.EnableNativeDeadLetterQueue()`. Override the default name (`wolverine-dead-letter-queue`) globally with `.DeadLetterQueueTopicName("crittercab-dlq")`. Wolverine stamps four diagnostic headers on dead-lettered messages (`exception-type`, `exception-message`, `exception-stack`, `failed-at`). See ai-skills `wolverine-integrations-kafka` § Dead letter queue.

### Retry policies

Retry and error-handling policies are Wolverine-native, not Kafka-specific. Combine retries with dead-letter routing via `opts.Policies.OnException<T>().RetryTimes(N).Then.MoveToErrorQueue()`. Without `EnableNativeDeadLetterQueue()` on the listener, `MoveToErrorQueue` routes to Wolverine's database-backed dead-letter storage instead of the Kafka DLT. See ai-skills `wolverine-messaging-resiliency-policies` for the full retry/DLQ surface.

### Circuit breakers

Each listener supports a circuit breaker (`.CircuitBreaker(cb => { cb.MinimumThreshold = ...; cb.PauseTime = ...; })`) that pauses consumption when failures exceed a threshold. See ai-skills `wolverine-messaging-resiliency-policies` § Circuit breakers for the full configuration surface.

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

For compacted topics, publish a tombstone (null-valued message with the target key) to remove the entry: `await bus.BroadcastToTopicAsync("topic", new KafkaTombstone(key))`. **Most Cab Kafka topics (GPS pings, demand signals) use time-based retention, not compaction** — tombstones are the exception, not the rule. See ai-skills `wolverine-integrations-kafka` § Tombstone messages for the mechanic.

## Tracing

Wolverine's Kafka transport propagates OpenTelemetry trace context through Kafka message headers automatically. A publish operation starts a span; the consumer continues the same trace. The Aspire dashboard shows these traces in local development — a single request from a rider's phone can be followed through gRPC into Dispatch, across Kafka into Telemetry, and back. Full OTel configuration — exporters, sampling, custom attributes — is covered in `observability-tracing` (Phase 3).

## Common pitfalls

- **Calling AutoProvision against the Event Hubs Emulator.** The EH Emulator does not support Kafka admin APIs. `AutoProvision()` will throw. Use Aspire's `AddKafka` for local dev (which starts a real Kafka container) and provision topics through the management plane for EH Emulator environments.

- **Forgetting a partition key on ordered streams.** Without a partition key, Wolverine uses the envelope's GUID, scattering messages randomly across partitions. GPS pings without `PartitionKey = driverId` lose their per-driver ordering guarantee. Always set a partition key for streams where ordering matters — and assert it in an integration test, because the failure is invisible: the records still arrive, only the ordering breaks.

- **Assuming a publish reached the broker when `PublishAsync` returns.** A Kafka publishing endpoint defaults to `BufferedInMemory`, which batches into an in-process queue and returns before the broker has seen anything. If a flow's correctness depends on the publish landing *before* some local write, `SendInline()` is required — and even inline is not enough on its own, because Wolverine's default async retry block swallows the send failure, logs it, re-posts to a background block and returns success. Pair `SendInline()` with `opts.Durability.UseSyncRetryBlock = true` (process-global) so a broker rejection actually throws. `telemetry.driver-location-updated` does exactly this for W006 §6.3's publish-before-store ordering.

- **Assuming ConfigureConsumer merges with the parent.** `ConfigureConsumer` on a per-topic listener **replaces** the parent `ConsumerConfig`. Bootstrap servers are auto-inherited, but other settings (SASL, timeouts) from the transport-level config are lost. Re-apply them in the per-topic override if needed. Note this is also why an unset cold-start position falls through to Confluent's default rather than to a Wolverine one — see § Listening.

- **A silent listener is a discovery problem before it is a serialization problem.** If messages are demonstrably on the topic and nothing happens — no handler invocation, no exception, no dead letter — check that the handler is `public`, concrete, conventionally named, and in a scanned assembly *before* looking at serializers. Wolverine resolves the wire type name from discovered handler chains, so an undiscovered handler means the payload is never deserialized at all, and the failure mode is silence rather than an error.

- **Producing before the consumer group has joined, in a test.** Under `BeginAtLatest()` (Cab's default for the telemetry feed) a record published before the group finishes joining is behind the tail and will never be delivered. Production never notices because the heartbeat republishes; a test hangs until its timeout. Warm the listener with a throwaway round trip first — and retry rather than sleeping, because group-join time varies with broker startup.

- **Registering a document for `TryUpdateRevision` without `UseNumericRevisions(true)`.** The last-writer-wins guard only engages when the document is configured for numeric revisions; without it the call degrades to a plain unguarded upsert and stale redeliveries start overwriting fresh state, with no error anywhere. If the revision is a timestamp, the document must also implement `ILongVersioned` (`long`) rather than `IRevisioned` (`int`) — unix-milliseconds overflowed `int` in 1970.

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

**Upstream** — generic Wolverine Kafka mechanics this skill defers to. ai-skills (license required, install via `npx skills add`):

- `wolverine-integrations-kafka` — generic Wolverine + Kafka transport: setup, topic binding, consumer groups, partition-based sequential processing, delivery semantics (at-least-once vs at-most-once), raw JSON interop, tombstones, multi-region named brokers.
- `wolverine-messaging-resiliency-policies` — retry strategies, circuit breakers, dead letter queues, compensating actions. Cab does not currently have a parallel skill — load this directly when configuring failure policies.

**Prerequisites** — Cab-internal skills to load first if unfamiliar:

- `transport-selection` — the decision framework that routes flows to Kafka vs ASB vs gRPC. Read this first to understand why a flow lands on Kafka.
- `wolverine-messaging-handlers` — the handler shape for all messaging transports, including Kafka. Handlers don't change based on transport.
- `service-bootstrap` — the `Program.cs` composition pattern that `UseKafkaUsingNamedConnection` extends.
- `aspire` — Aspire orchestration of the Kafka container (`AddKafka("kafka")`) and connection-string injection.

**Sibling skills:**

- `wolverine-azure-service-bus` (Phase 3) — ASB transport wiring; the other messaging transport Cab uses, optimized for domain events with rich DLQ support.
- `cli-kafka-tooling` (Phase 3) — kcat, console tools, and Aspire dashboard for inspecting Kafka topics and messages.

**Downstream:**

- `observability-tracing` (Phase 3) — full OTel pipeline configuration, including trace propagation across Kafka.
- `wolverine-sagas` (Phase 4) — long-running processes that may span Kafka and ASB transports.
- `testing-advanced` (Phase 4) — integration tests against Kafka using `Testcontainers.Kafka`.

**External:**

- [Wolverine Kafka transport docs](https://wolverinefx.net/guide/messaging/transports/kafka.html)
- [Azure Event Hubs for Apache Kafka](https://learn.microsoft.com/en-us/azure/event-hubs/azure-event-hubs-kafka-overview)
- ADR-005 — transport selection rationale
