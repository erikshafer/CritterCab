---
name: wolverine-azure-service-bus
description: "Wolverine's Azure Service Bus transport for cross-service domain events in CritterCab. Covers UseAzureServiceBus bootstrap (connection string and Managed Identity), topics and subscriptions for fan-out, queues for point-to-point, subscription rules (SQL and correlation filters), sessions for ordered delivery, native dead-letter queues, scheduled delivery, retry and circuit-breaker policies, convention-based routing, interop with MassTransit and NServiceBus, the ASB Emulator via Aspire and Testcontainers, and the Aspire.Hosting.Azure.ServiceBus package prerequisite."
cluster: wolverine
tags: [azure-service-bus, wolverine, messaging, transport, topics, subscriptions, dead-letter, sessions, scheduled-delivery, domain-events, adr-005]
---

# Wolverine Azure Service Bus Transport

Azure Service Bus is CritterCab's transport for cross-service domain events — the events that announce a state change in one bounded context to all interested parties in other contexts. `RiderRegistered`, `DriverApproved`, `TripCompleted`, `PaymentCaptured`, `RatingSubmitted` — these all travel over ASB. The `transport-selection` skill defines the decision framework that routes these flows to ASB rather than Kafka or gRPC; this skill covers the Wolverine wiring.

Where Kafka optimizes for high-volume append-only streams with partition-based ordering, ASB optimizes for **reliability of individual messages**: native dead-letter queues, message sessions for FIFO delivery, subscription-level filtering, scheduled enqueue, and broker-managed TTL. These features make ASB the right transport when every message matters and the consumer needs infrastructure-level delivery guarantees — exactly the profile of cross-service domain events.

As with Kafka, **handlers don't know they're consuming from ASB**. A handler processing `TripCompleted` from an ASB subscription is a vanilla Wolverine messaging handler per `wolverine-messaging-handlers`. The transport choice lives in `Program.cs` routing configuration, never in the handler.

**Prerequisite package not yet committed:** Cab's `Directory.Packages.props` does not yet include `Aspire.Hosting.Azure.ServiceBus`. When ASB local-dev orchestration lands, this package must be added to enable the emulator container in the AppHost.

## When to apply this skill

**Use this skill when:**

- Wiring a Wolverine service to publish domain events to an ASB topic.
- Wiring a Wolverine service to subscribe to domain events from an ASB topic.
- Configuring point-to-point queues for command-style messages between services.
- Setting up sessions for ordered delivery (per-rider or per-trip event ordering).
- Configuring dead-letter queues, subscription rules, or scheduled delivery.
- Understanding the ASB Emulator vs production ASB connection differences.

**Do NOT use this skill for:**

- Deciding whether a flow belongs on ASB vs Kafka vs gRPC — see `transport-selection`.
- Writing the handler that processes an ASB message — see `wolverine-messaging-handlers`.
- Aspire orchestration of the ASB emulator container — see `aspire`.
- CLI tooling for inspecting queues, topics, and dead-letter messages — see `cli-azure-messaging` (Phase 3).
- Kafka transport wiring — see `wolverine-kafka`.

## Mental model

```
Trips service                    ASB topic                         Consuming services
┌────────────────┐        ┌───────────────────┐
│ TripCompleted  │─pub──► │ trips.             │───subscription──► Payments
│ emitted from   │        │ trip-completed     │───subscription──► Ratings
│ aggregate      │        │                    │───subscription──► Operations
└────────────────┘        └───────────────────┘───subscription──► Pricing
```

The Trips service publishes `Integration.TripCompleted` to the `trips.trip-completed` topic. Payments, Ratings, Operations, and Pricing each maintain an independent subscription on that topic. Each subscription gets its own copy of every message — this is fan-out. If Pricing only cares about trips above a certain fare threshold, it configures a subscription rule (SQL filter) to receive only matching messages.

Queues are the other primitive. Where topics fan out, queues are point-to-point: one sender, one receiver, competing consumers if scaled horizontally. Cab uses queues for directed commands (e.g., `ProcessPayment` sent from Trips to Payments) and for Wolverine's internal system queues (response routing, retry).

## Bootstrap

### Connection string (local dev, ASB Emulator)

The simplest bootstrap passes a connection string:

```csharp
builder.Host.UseWolverine(opts =>
{
    var connectionString = builder.Configuration.GetConnectionString("messaging");
    opts.UseAzureServiceBus(connectionString)
        .AutoProvision();

    opts.PublishMessage<Integration.TripCompleted>()
        .ToAzureServiceBusTopic("trips.trip-completed");

    opts.PublishMessage<Integration.TripCancelled>()
        .ToAzureServiceBusTopic("trips.trip-cancelled");
});
```

`GetConnectionString("messaging")` reads the connection string that Aspire injects via `WithReference()`. The `"messaging"` key matches the resource name used in the AppHost's `AddAzureServiceBus("messaging")`.

### Managed Identity (production Azure)

In production, connection strings with shared keys are a security liability. Use `TokenCredential` instead:

```csharp
opts.UseAzureServiceBus(
    "<namespace>.servicebus.windows.net",
    new DefaultAzureCredential());
```

`DefaultAzureCredential` resolves the right credential for the environment — Managed Identity in Azure, Visual Studio / Azure CLI credentials in dev. The fully qualified namespace (not a connection string) tells the SDK to use token-based auth.

Other credential overloads exist for `AzureNamedKeyCredential` and `AzureSasCredential`, but `DefaultAzureCredential` is the Cab default because it works across environments without code changes.

### AutoProvision

`AutoProvision()` creates missing queues, topics, and subscriptions at startup using the ASB management API (`ServiceBusAdministrationClient`). If an entity already exists, the creation is silently skipped. This is safe for local dev and CI but requires `Manage` permission on the namespace — production environments may restrict this.

```csharp
opts.UseAzureServiceBus(connectionString)
    .AutoProvision();
```

For production environments where the service identity lacks `Manage` permission, omit `AutoProvision()` and provision entities through infrastructure-as-code (Bicep, Terraform) or the Azure CLI.

### AutoPurgeOnStartup (testing only)

```csharp
opts.UseAzureServiceBus(connectionString)
    .AutoProvision()
    .AutoPurgeOnStartup();
```

`AutoPurgeOnStartup()` drains all messages from every queue and subscription at startup. This is for integration tests that need a clean slate — never enable it outside test configurations.

### System queues

Wolverine creates internal system queues for response routing and deferred-message retry:

- `wolverine.response.{serviceName}.{nodeNumber}` — reply routing (auto-deleted after 5 minutes idle).
- `wolverine.retries.{serviceName}` — deferred message requeue.

These are created automatically when `AutoProvision()` is active. If the service identity lacks create permission and you're provisioning externally, disable system queues:

```csharp
opts.UseAzureServiceBus(connectionString)
    .SystemQueuesAreEnabled(false);
```

## Topics and subscriptions

Topics with subscriptions are Cab's primary ASB pattern — every cross-service domain event uses fan-out delivery.

### Publishing to a topic

```csharp
opts.PublishMessage<Integration.TripCompleted>()
    .ToAzureServiceBusTopic("trips.trip-completed");

opts.PublishMessage<Integration.RiderRegistered>()
    .ToAzureServiceBusTopic("identity.rider-registered");
```

Topic naming follows the same `<bc>.<event-name>` convention as Kafka topics (see `wolverine-kafka` § Topic naming convention). ASB topic names are lowercased automatically by Wolverine.

Configure the topic's broker-level properties during auto-provision:

```csharp
opts.PublishMessage<Integration.TripCompleted>()
    .ToAzureServiceBusTopic("trips.trip-completed")
    .ConfigureTopic(topic =>
    {
        topic.DefaultMessageTimeToLive = TimeSpan.FromDays(7);
    });
```

### Subscribing to a topic

Subscriptions are two-step: name the subscription, then specify which topic it reads from:

```csharp
opts.ListenToAzureServiceBusSubscription("payments")
    .FromTopic("trips.trip-completed");
```

This creates a subscription named `payments` on the `trips.trip-completed` topic. The handler is a standard messaging handler:

```csharp
public static class TripCompletedHandler
{
    public static async Task Handle(
        Integration.TripCompleted completed,
        IPaymentService payments)
    {
        await payments.CaptureAsync(completed.TripId, completed.FareAmount);
    }
}
```

Multiple subscriptions on the same topic get independent copies of every message — Payments, Ratings, Operations, and Pricing each process `TripCompleted` independently.

### Subscription rules (SQL and correlation filters)

By default, a subscription receives all messages published to the topic. SQL filters narrow the stream:

```csharp
opts.ListenToAzureServiceBusSubscription(
        "pricing",
        configureSubscriptionRule: rule =>
        {
            rule.Filter = new SqlRuleFilter("fare_amount > 5000");
        })
    .FromTopic("trips.trip-completed");
```

The `SqlRuleFilter` evaluates against the message's `ApplicationProperties` (where Wolverine stores custom headers). For this to work, the publishing side must set the property:

```csharp
await bus.PublishAsync(new Integration.TripCompleted(tripId, fareAmount),
    new DeliveryOptions
    {
        Headers = { ["fare_amount"] = fareAmount.ToString() }
    });
```

Correlation filters match exact property values and are faster than SQL filters when equality is all you need:

```csharp
rule.Filter = new CorrelationRuleFilter
{
    ApplicationProperties = { ["region"] = "us-west" }
};
```

When `AutoProvision()` is active, Wolverine reconciles subscription rules at startup: it updates rules whose name matches but whose filter changed, and deletes unknown rules.

### Configuring subscription properties

```csharp
opts.ListenToAzureServiceBusSubscription(
        "payments",
        configureSubscriptions: sub =>
        {
            sub.MaxDeliveryCount = 10;
            sub.LockDuration = TimeSpan.FromMinutes(2);
            sub.DeadLetteringOnMessageExpiration = true;
        })
    .FromTopic("trips.trip-completed");
```

`MaxDeliveryCount` is ASB's built-in retry count — after this many failed delivery attempts, the message moves to the subscription's built-in dead-letter sub-queue. This is separate from Wolverine's error-handling policies and acts as a last-resort safety net.

### Convention-based topic routing

When topic and subscription names follow a derivable pattern, convention routing eliminates per-message-type configuration:

```csharp
opts.UseAzureServiceBus(connectionString)
    .AutoProvision()
    .UseTopicAndSubscriptionConventionalRouting(convention =>
    {
        convention.TopicNameForSender(type => type.Name.ToLower());
        convention.SubscriptionNameForListener(type => type.Name.ToLower());
    });
```

Cab uses explicit topic routing because topic names follow `<bc>.<event-name>` and can't be derived from the type name alone. Convention routing is available for projects where the type name maps 1:1 to the topic name.

## Queues

Queues are point-to-point: one logical receiver per queue (competing consumers if horizontally scaled). Cab uses queues for directed commands between services.

### Publishing to a queue

```csharp
opts.PublishMessage<ProcessPayment>()
    .ToAzureServiceBusQueue("payments.process-payment");
```

### Listening to a queue

```csharp
opts.ListenToAzureServiceBusQueue("payments.process-payment");
```

### Configuring queue properties

```csharp
opts.ListenToAzureServiceBusQueue("payments.process-payment")
    .ConfigureQueue(queue =>
    {
        queue.MaxDeliveryCount = 10;
        queue.LockDuration = TimeSpan.FromMinutes(2);
        queue.DefaultMessageTimeToLive = TimeSpan.FromDays(14);
        queue.RequiresDuplicateDetection = true;
        queue.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(5);
    });
```

`RequiresDuplicateDetection` with `DuplicateDetectionHistoryTimeWindow` enables broker-level deduplication — ASB discards messages with a duplicate `MessageId` within the window. Since Wolverine maps `Envelope.Id` to `MessageId`, this provides infrastructure-level idempotency for the detection window.

## Sessions and ordered delivery

ASB sessions provide FIFO delivery within a session identifier. Messages with the same `SessionId` are delivered in order to a single consumer — no reordering, no competing consumers within the session.

### When to use sessions

Sessions fit when events for a single entity must be processed in order across services. Per-rider event ordering (`RiderRegistered` → `RiderVerified` → `RiderSuspended`) or per-trip ordering (`TripStarted` → `DriverArrived` → `TripCompleted`) are the canonical Cab examples.

### Enabling sessions on a queue

```csharp
opts.ListenToAzureServiceBusQueue("rider-events")
    .RequireSessions(listenerCount: 5);
```

`RequireSessions` sets `RequiresSession = true` on the queue and configures the number of concurrent session pumps. Each pump accepts one session at a time via `AcceptNextSessionAsync` — so `listenerCount: 5` means up to 5 sessions are processed concurrently.

### Setting the session ID when publishing

Wolverine maps `Envelope.GroupId` to ASB's `SessionId`. Set it via `DeliveryOptions`:

```csharp
await bus.PublishAsync(new RiderVerified(riderId),
    new DeliveryOptions { GroupId = riderId.ToString() });
```

All messages for the same rider land in the same session and are delivered in publish order.

### Sessions on subscriptions

```csharp
opts.ListenToAzureServiceBusSubscription(
        "rider-ordering",
        configureSubscriptions: sub => sub.RequiresSession = true)
    .FromTopic("identity.rider-events")
    .RequireSessions(listenerCount: 5);
```

### ExclusiveNodeWithSessions

For scenarios where session processing must be pinned to a single node (e.g., in-memory caches that must stay consistent):

```csharp
opts.ListenToAzureServiceBusQueue("rider-events")
    .ExclusiveNodeWithSessions(maxParallelSessions: 10);
```

This combines session-based listening with Wolverine's exclusive-node assignment.

## Dead-letter queues and error handling

ASB's native dead-letter queue support is one of the primary reasons `transport-selection` routes domain events to ASB rather than Kafka.

### Native dead-lettering

Every ASB queue and subscription has a built-in dead-letter sub-queue (`$DeadLetterQueue`). When a message exhausts its `MaxDeliveryCount` or expires past its TTL, ASB moves it to the DLQ automatically — no application code or Wolverine configuration needed.

The DLQ message retains the original body plus diagnostic properties: `DeadLetterReason`, `DeadLetterErrorDescription`, and all original `ApplicationProperties`. These are inspectable via the Azure Portal, Service Bus Explorer, or the Azure CLI (see `cli-azure-messaging` in Phase 3).

### Wolverine's dead-letter routing

In addition to ASB's native DLQ, Wolverine can route failed messages to a separate dead-letter queue:

```csharp
opts.ListenToAzureServiceBusQueue("payments.process-payment")
    .ConfigureDeadLetterQueue("payments.process-payment.dead-letters",
        configure: dlq =>
        {
            dlq.Options.MaxDeliveryCount = 3;
        });
```

To disable Wolverine's DLQ routing and rely solely on ASB's native dead-lettering:

```csharp
opts.ListenToAzureServiceBusQueue("payments.process-payment")
    .DisableDeadLetterQueueing();
```

The Cab default is to let ASB's native DLQ handle failures — it's simpler and the DLQ messages are inspectable through standard ASB tooling. Wolverine's `ConfigureDeadLetterQueue` is the escape hatch for custom DLQ routing.

### Retry policies

Wolverine's error-handling policies apply to ASB messages the same way they apply to all transports:

```csharp
opts.Policies.OnException<PaymentGatewayTimeoutException>()
    .RetryTimes(3)
    .Then.MoveToErrorQueue();
```

ASB's `MaxDeliveryCount` is a separate, broker-level retry count. The two interact: Wolverine's retry policy retries in-process (without releasing the message lock), while ASB's delivery count increments each time the message is abandoned back to the broker. Set both deliberately — a Wolverine retry of 3 with an ASB `MaxDeliveryCount` of 5 means the message gets 3 in-process retries on up to 5 broker deliveries.

### Circuit breakers

```csharp
opts.ListenToAzureServiceBusQueue("payments.process-payment")
    .CircuitBreaker(cb =>
    {
        cb.MinimumThreshold = 10;
        cb.PauseTime = TimeSpan.FromMinutes(2);
    });
```

When failures exceed the threshold, Wolverine pauses the listener. After `PauseTime`, consumption resumes. This prevents a downstream outage from churning through retries and exhausting `MaxDeliveryCount` on every message.

### Scheduled delivery

ASB supports native scheduled enqueue — the broker holds the message and delivers it at the specified time:

```csharp
await bus.ScheduleAsync(
    new ExpireOfferIfNotAccepted(offerId),
    TimeSpan.FromSeconds(15));
```

Wolverine maps `Envelope.ScheduledTime` to ASB's `ScheduledEnqueueTime`. The message is invisible to consumers until the scheduled time arrives. This is broker-managed — if the publishing service crashes after the schedule call, the message still delivers on time.

## Serialization and interop

### Default envelope mapping

Wolverine maps envelope properties to ASB's native message properties:

| Envelope | ASB message property |
|---|---|
| `Id` | `MessageId` |
| `MessageType` | `Subject` |
| `CorrelationId` | `CorrelationId` |
| `ContentType` | `ContentType` |
| `GroupId` | `SessionId` |
| `ScheduledTime` | `ScheduledEnqueueTime` |
| All other headers | `ApplicationProperties` dictionary |

The `Subject` field carrying the message type is significant — it's visible in Azure Portal and Service Bus Explorer without deserializing the body. This makes ASB message inspection easier than Kafka, where the message type is buried in binary headers.

### Custom envelope mapper

Implement `IAzureServiceBusEnvelopeMapper` for full control over the mapping:

```csharp
opts.ListenToAzureServiceBusQueue("external-events")
    .InteropWith(new ExternalSystemMapper());
```

### MassTransit and NServiceBus interop

For services migrating from MassTransit or NServiceBus to Wolverine, interop mappers translate the envelope format:

```csharp
// MassTransit interop
opts.ListenToAzureServiceBusQueue("legacy-commands")
    .UseMassTransitInterop(mt => { /* configure if needed */ });

// NServiceBus interop
opts.ListenToAzureServiceBusQueue("legacy-events")
    .UseNServiceBusInterop();
```

These are migration aids, not long-term patterns. Once the publishing service is migrated to Wolverine, switch to the default envelope mapping.

## Local development — ASB Emulator

### Aspire integration

The ASB Emulator runs as a Docker container orchestrated by Aspire. This requires `Aspire.Hosting.Azure.ServiceBus` — **not yet in Cab's `Directory.Packages.props`**. Add it when ASB local-dev orchestration lands:

```xml
<PackageVersion Include="Aspire.Hosting.Azure.ServiceBus" Version="13.2.2" />
```

In the AppHost:

```csharp
var messaging = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

// Pre-provision topics and queues in the emulator
messaging.AddServiceBusTopic("trips.trip-completed");
messaging.AddServiceBusTopic("trips.trip-cancelled");
messaging.AddServiceBusTopic("identity.rider-registered");
messaging.AddServiceBusQueue("payments.process-payment");

// Wire services
builder.AddProject<Projects.CritterCab_Trips>("trips")
    .WithReference(messaging)
    .WaitFor(messaging);

builder.AddProject<Projects.CritterCab_Payments>("payments")
    .WithReference(messaging)
    .WaitFor(messaging);
```

`RunAsEmulator()` starts the ASB Emulator container. `AddServiceBusTopic` and `AddServiceBusQueue` pre-provision entities in the emulator configuration. On the service side, Aspire injects the connection string as `"messaging"` — the same key passed to `GetConnectionString("messaging")` in the service's `Program.cs`.

### Testcontainers

For integration tests that don't use Aspire, `Testcontainers.ServiceBus` (already in `Directory.Packages.props` at 4.11.0) provides the emulator:

```csharp
var container = new AzureServiceBusBuilder()
    .Build();

await container.StartAsync();
var connectionString = container.GetConnectionString();
```

The test host then passes this connection string to `UseAzureServiceBus(connectionString)`.

### What changes between local and production

| Concern | Local (emulator) | Production (Azure) |
|---|---|---|
| Connection | Connection string via Aspire | `DefaultAzureCredential` + namespace |
| Provisioning | `AutoProvision()` + Aspire's `AddServiceBusTopic` | Bicep / Terraform / Azure CLI |
| System queues | Created automatically | May need pre-provisioning if `Manage` is restricted |
| Sessions | Supported | Supported |
| DLQ | Supported | Supported |

The handler and routing code is identical across environments.

## Tracing

Wolverine's ASB transport propagates OpenTelemetry trace context through ASB message properties automatically. Publish and consume spans appear as part of the same distributed trace in the Aspire dashboard. Full OTel configuration — exporters, sampling, custom attributes — is covered in `observability-tracing` (Phase 3).

## Common pitfalls

- **Forgetting that MaxDeliveryCount and Wolverine retries are separate.** ASB's `MaxDeliveryCount` is a broker-level delivery counter. Wolverine's `RetryTimes(n)` retries in-process without releasing the lock. If Wolverine retries exhaust and the handler still throws, ASB increments the delivery count. A Wolverine retry of 3 inside an ASB delivery count of 5 means the message gets up to 15 processing attempts total (3 retries x 5 deliveries). Set both deliberately.

- **Forgetting to set GroupId when publishing to a session-enabled queue.** If the queue requires sessions and the message has no `SessionId` (no `GroupId` on the envelope), ASB rejects the send. Always set `DeliveryOptions.GroupId` when the target queue or subscription has `RequiresSession = true`.

- **Using Kafka for domain events that need dead-lettering and replay.** Kafka's DLT is a Wolverine-layer construct that produces to a separate topic. ASB's DLQ is broker-native with per-message `DeadLetterReason`, inspectable through standard tooling, and resubmittable. If DLQ inspection and replay are important, the flow belongs on ASB per `transport-selection`.

- **Assuming AutoProvision works in production.** `AutoProvision()` requires `Manage` permission on the ASB namespace. Production service identities typically have `Send` and `Listen` only. Provision entities through infrastructure-as-code and omit `AutoProvision()` in production configuration.

- **Putting transport concerns in the handler.** A handler should never reference `ServiceBusReceivedMessage`, session IDs, or dead-letter properties. Access envelope metadata through `Envelope` if absolutely needed, but prefer keeping handlers transport-agnostic.

- **Confusing topics and queues.** Topics are fan-out: every subscription gets a copy. Queues are point-to-point: each message goes to one consumer. Publishing a domain event to a queue means only one service receives it — use a topic.

- **Using AutoPurgeOnStartup outside tests.** `AutoPurgeOnStartup()` drains every queue and subscription at startup. In a non-test environment, this silently destroys in-flight messages. Guard it behind a test-only configuration flag.

- **Not adding Aspire.Hosting.Azure.ServiceBus to Directory.Packages.props.** Unlike Kafka (`Aspire.Hosting.Kafka` is already committed), the ASB hosting package is not yet in Cab's central package management. The AppHost won't compile until it's added.

- **Setting RequiresDuplicateDetection after the queue exists.** Duplicate detection can only be enabled at queue creation time — it can't be toggled on an existing queue. If you need it, set it in the initial `ConfigureQueue` call before the first `AutoProvision()` run.

- **Ignoring subscription rule reconciliation.** When `AutoProvision()` is active, Wolverine reconciles subscription rules at startup — it updates rules whose name matches but whose filter changed, and deletes rules it doesn't recognize. If another tool or team manages rules outside Wolverine, this reconciliation can delete their rules. Coordinate rule ownership.

- **Forgetting the Aspire.Hosting.Azure.ServiceBus package uses Azure.ServiceBus in the name.** The NuGet package is `Aspire.Hosting.Azure.ServiceBus` (with dots), not `Aspire.Hosting.AzureServiceBus`. Getting the name wrong in `Directory.Packages.props` produces a confusing "package not found" restore error.

- **Disabling system queues without understanding the consequences.** `SystemQueuesAreEnabled(false)` disables response routing and deferred-message retry. Request-reply patterns and `ScheduleAsync` stop working. Only disable system queues when the service identity genuinely cannot create queues and you've provisioned the system queues externally.

## See also

### Upstream

- `transport-selection` — the decision framework that routes domain events to ASB. Read this first to understand why a flow lands on ASB rather than Kafka.
- `wolverine-messaging-handlers` — the handler shape for all messaging transports, including ASB. Handlers don't change based on transport.
- `service-bootstrap` — the `Program.cs` composition pattern that `UseAzureServiceBus` extends.
- `domain-event-conventions` — the event-naming and payload conventions for the integration events ASB carries.
- `aspire` — Aspire orchestration; eventually pairs with `Aspire.Hosting.Azure.ServiceBus` for the emulator.

### Sibling skills

- `wolverine-kafka` — Kafka transport wiring; the other messaging transport Cab uses, optimized for high-volume streams.
- `cli-azure-messaging` (Phase 3) — Azure CLI, Service Bus Explorer, and Aspire dashboard for inspecting ASB entities and dead-letter queues.

### Downstream

- `observability-tracing` (Phase 3) — full OTel pipeline configuration, including trace propagation across ASB.
- `identity-acl` (Phase 3) — service-to-service auth that applies to ASB-connected services.
- `wolverine-sagas` (Phase 4) — long-running processes that coordinate across ASB topics.
- `testing-advanced` (Phase 4) — integration tests against ASB using `Testcontainers.ServiceBus`.

### External

- [Wolverine ASB transport docs](https://wolverinefx.net/guide/messaging/transports/azureservicebus/)
- [Azure Service Bus documentation](https://learn.microsoft.com/en-us/azure/service-bus-messaging/)
- [Aspire ASB hosting integration](https://learn.microsoft.com/en-us/dotnet/aspire/messaging/azure-service-bus-integration)
- ADR-005 — transport selection rationale
- ai-skills: `wolverine-integrations-azure-service-bus`
