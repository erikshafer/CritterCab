---
name: cli-azure-messaging
description: "CLI and GUI tools for working with Azure Service Bus and Azure Event Hubs in CritterCab. Covers az servicebus for managing namespaces, queues, topics, subscriptions, and subscription rules; az eventhubs for Event Hubs management when running Kafka transport against EH; the Azure Portal's built-in Service Bus Explorer for peeking messages, inspecting dead-letter queues, and replaying failed messages; ServiceBusExplorer (desktop) for richer inspection and bulk operations; the Aspire dashboard for container health and trace visibility; and the ASB Emulator interaction patterns for local development."
cluster: infrastructure
tags: [cli, azure-service-bus, event-hubs, dead-letter, subscription-rules, peek, emulator, aspire-dashboard, debugging, service-bus-explorer]
---

# Azure Messaging CLI Tooling

Four tools and the Aspire dashboard cover the operational surface for Azure messaging in CritterCab:

- **`az servicebus`** — the Azure CLI's Service Bus module. Manages namespaces, queues, topics, subscriptions, and subscription rules from the command line. This is the management-plane tool — it creates and configures entities but does not peek at or send individual messages.
- **`az eventhubs`** — the Azure CLI's Event Hubs module. Manages Event Hubs namespaces and event hubs for the Kafka transport's cloud backing store. Relevant when Cab's Kafka topics run against Azure Event Hubs rather than a local Kafka container.
- **Azure Portal Service Bus Explorer** — the built-in browser tool in the Azure Portal for peeking messages, inspecting dead-letter queues, sending test messages, and replaying failed messages. This is the everyday message-level inspection tool that requires no local installation.
- **ServiceBusExplorer (desktop)** — a Windows desktop application for advanced ASB operations: bulk message inspection, import/export, topic and queue management, and session-aware browsing. Richer than the Portal tool; installable via `winget`.
- **Aspire dashboard** — shows ASB Emulator container health, service-to-resource dependencies, and OpenTelemetry traces that flow through ASB. Not a message browser.

The split mirrors the Kafka tooling pattern: `az servicebus` and `az eventhubs` are management-plane (like `kafka-topics`), the Portal Explorer and ServiceBusExplorer are message-plane (like kcat), and the Aspire dashboard is infrastructure visibility.

## When to apply this skill

**Use this skill when:**

- Creating or inspecting ASB queues, topics, and subscriptions from the command line.
- Managing subscription rules (SQL filters, correlation filters) outside of Wolverine's `AutoProvision`.
- Peeking at messages in a queue, subscription, or dead-letter queue.
- Replaying dead-lettered messages back to the main queue.
- Managing Event Hubs namespaces and event hubs for Kafka transport.
- Checking ASB Emulator status or the Aspire dashboard's ASB resource view.

**Do NOT use this skill for:**

- Wolverine ASB transport configuration (routing rules, listeners, publishers) — see `wolverine-azure-service-bus`.
- Wolverine Kafka transport wiring against Event Hubs — see `wolverine-kafka`.
- Kafka CLI tooling (kcat, kafka-console-*) — see `cli-kafka-tooling`.
- Aspire AppHost configuration or CLI — see `aspire` and `cli-aspire`.
- Deciding which transport a flow belongs on — see `transport-selection`.

---

## Tools at a glance

| Tool | Primary use | Cab daily-driver scenario |
|---|---|---|
| `az servicebus` | Manage namespaces, queues, topics, subscriptions, rules | "Create this topic in staging" / "Show subscription rule filters" |
| `az eventhubs` | Manage EH namespaces and event hubs | "Create the telemetry event hub in Azure" / "List consumer groups" |
| Portal Explorer | Peek messages, inspect DLQ, send test messages, replay | "What's in the dead-letter queue?" / "Resend this failed message" |
| ServiceBusExplorer | Advanced inspection, bulk operations, session browsing | "Export all DLQ messages to a file" / "Browse session state" |
| Aspire dashboard | Container health, resource graph, traces | "Is the emulator up?" / "Trace this event through ASB" |

---

## `az servicebus` — Azure CLI

### Installation

The `az servicebus` commands are part of the Azure CLI. Install the Azure CLI if not already present:

```bash
# macOS
brew install azure-cli

# Windows (winget)
winget install Microsoft.AzureCLI

# Linux (one-liner)
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

Verify and log in:

```bash
az version
az login
```

### Namespace operations

```bash
# List namespaces in a resource group
az servicebus namespace list --resource-group crittercab-rg --output table

# Show a specific namespace (including connection strings endpoint)
az servicebus namespace show --name crittercab-messaging --resource-group crittercab-rg

# Get the primary connection string (for local dev / emulator config)
az servicebus namespace authorization-rule keys list \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString -o tsv
```

### Topic operations

```bash
# List topics
az servicebus topic list \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --output table

# Create a topic
az servicebus topic create \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --name trips.trip-completed \
  --default-message-time-to-live P7D

# Show topic details (including message counts)
az servicebus topic show \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --name trips.trip-completed

# Delete a topic
az servicebus topic delete \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --name trips.trip-completed
```

### Subscription operations

```bash
# List subscriptions on a topic
az servicebus topic subscription list \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --topic-name trips.trip-completed \
  --output table

# Create a subscription
az servicebus topic subscription create \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --topic-name trips.trip-completed \
  --name payments \
  --max-delivery-count 10

# Show subscription details (includes active, dead-letter, and scheduled message counts)
az servicebus topic subscription show \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --topic-name trips.trip-completed \
  --name payments \
  --query "{Active:countDetails.activeMessageCount, DeadLetter:countDetails.deadLetterMessageCount, Scheduled:countDetails.scheduledMessageCount}"
```

The `countDetails` query is the fastest way to check whether messages are accumulating in the dead-letter queue without opening the Portal.

### Queue operations

```bash
# List queues
az servicebus queue list \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --output table

# Create a queue
az servicebus queue create \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --name payments.process-payment \
  --max-delivery-count 10 \
  --lock-duration PT2M

# Show queue details (including message counts and DLQ count)
az servicebus queue show \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --name payments.process-payment \
  --query "{Active:countDetails.activeMessageCount, DeadLetter:countDetails.deadLetterMessageCount}"
```

### Subscription rule management

```bash
# List rules on a subscription
az servicebus topic subscription rule list \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --topic-name trips.trip-completed \
  --subscription-name pricing

# Create a SQL filter rule
az servicebus topic subscription rule create \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --topic-name trips.trip-completed \
  --subscription-name pricing \
  --name high-fare-filter \
  --filter-sql-expression "fare_amount > 5000"

# Delete the default "accept all" rule (after adding a specific filter)
az servicebus topic subscription rule delete \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --topic-name trips.trip-completed \
  --subscription-name pricing \
  --name '$Default'
```

When Wolverine's `AutoProvision()` manages subscription rules, it reconciles them at startup. Rules created manually via `az servicebus` may be deleted by Wolverine on the next startup if they don't match the configured `CreateRuleOptions`. Coordinate rule ownership — either Wolverine owns all rules or the CLI does, not both.

---

## `az eventhubs` — Event Hubs CLI

When Cab's Kafka transport runs against Azure Event Hubs in the cloud (per `wolverine-kafka`), the Event Hubs management CLI handles entity provisioning. Wolverine connects via Kafka protocol, but the management plane is Azure-native.

```bash
# List event hubs in a namespace
az eventhubs eventhub list \
  --namespace-name crittercab-telemetry \
  --resource-group crittercab-rg \
  --output table

# Create an event hub (Kafka topic equivalent)
az eventhubs eventhub create \
  --namespace-name crittercab-telemetry \
  --resource-group crittercab-rg \
  --name telemetry.location-pings \
  --partition-count 6 \
  --message-retention 7

# Show consumer groups
az eventhubs eventhub consumer-group list \
  --namespace-name crittercab-telemetry \
  --resource-group crittercab-rg \
  --eventhub-name telemetry.location-pings \
  --output table

# Get the connection string (for Kafka bootstrap)
az eventhubs namespace authorization-rule keys list \
  --namespace-name crittercab-telemetry \
  --resource-group crittercab-rg \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString -o tsv
```

The `--partition-count` and `--message-retention` flags map to Kafka's partition count and retention period. For Kafka-level operations (produce, consume, inspect offsets), use kcat per `cli-kafka-tooling` — `az eventhubs` is strictly management-plane.

---

## Azure Portal Service Bus Explorer

The Portal's built-in Service Bus Explorer is the most accessible message-level tool — no installation, works from any browser, authenticates via the Portal session.

### Accessing the Explorer

Navigate to the Azure Portal → your Service Bus namespace → select a queue or subscription → click **Service Bus Explorer** in the left nav.

### Peeking at messages

Select **Peek from start** (or **Peek** mode in the dropdown) to view messages without consuming them. The Explorer shows up to 100 messages at a time with:

- **Sequence number** — the broker-assigned monotonic ID.
- **Message ID** — Wolverine's `Envelope.Id`.
- **Subject** — Wolverine's message type name (e.g., `CritterCab.Trips.Integration.TripCompleted`).
- **Body** — the serialized message content (JSON by default).
- **Application properties** — all Wolverine headers (`correlation-id`, custom headers, etc.).
- **System properties** — enqueue time, delivery count, session ID, etc.

The **Subject** column is particularly useful — since Wolverine maps message type to `Subject`, you can scan a topic's messages by type without deserializing.

### Inspecting the dead-letter queue

Navigate to the queue or subscription → Service Bus Explorer → select **Dead-letter** in the subqueue dropdown. Dead-lettered messages show:

- **DeadLetterReason** — why the message was dead-lettered (e.g., `MaxDeliveryCountExceeded`).
- **DeadLetterErrorDescription** — the exception message from the last failed processing attempt.
- **Original body and properties** — everything from the original message.

### Replaying dead-lettered messages

Select one or more messages in the dead-letter view → click **Resend selected messages**. This re-enqueues the messages at the front of the main queue (or subscription) for reprocessing. This is the standard "fix the bug, then replay the failed messages" workflow.

For bulk replay, ServiceBusExplorer (desktop) is more efficient — the Portal tool handles up to 100 messages at a time.

### Sending test messages

Select **Send messages** mode → compose a JSON body → set properties (Subject for message type, SessionId if the queue requires sessions) → send. This is the ASB equivalent of kcat's producer mode — useful for exercising a handler without wiring up the full publishing service.

When sending a test message to a Wolverine consumer, set the **Subject** to the full message type name (e.g., `CritterCab.Trips.Integration.TripCompleted`) and the **Content Type** to `application/json`. Wolverine resolves the handler from the Subject field.

---

## ServiceBusExplorer (desktop)

The [ServiceBusExplorer](https://github.com/paolosalvatori/ServiceBusExplorer) is a Windows desktop application for advanced ASB operations.

### Installation

```bash
# Windows (winget)
winget install paolosalvatori.ServiceBusExplorer
```

Version 6.2.0 (current as of early 2026) supports Entra ID authentication and connection strings.

### Key capabilities beyond the Portal

- **Bulk message operations** — export all DLQ messages to a file, import and replay from file.
- **Session browsing** — list active sessions, peek at messages within a specific session, inspect session state.
- **Subscription rule editing** — create, modify, and test SQL and correlation filter rules with an inline expression editor.
- **Deferred message inspection** — browse messages in the deferred state (messages that have been deferred by a handler but not yet reprocessed).
- **Entity management** — create, delete, and configure queues, topics, and subscriptions with full property control.

### Connecting to the ASB Emulator

ServiceBusExplorer connects to the local ASB Emulator using the emulator's connection string (typically `Endpoint=sb://localhost;SharedAccessKeyName=...`). The emulator exposes the same management API as the cloud service, so all ServiceBusExplorer features work locally.

### When to reach for ServiceBusExplorer vs the Portal

- **Quick peek at a few messages** → Portal Explorer. No installation, browser-based.
- **Bulk DLQ replay (hundreds of messages)** → ServiceBusExplorer. Export/import is faster than the Portal's 100-at-a-time limit.
- **Session debugging** → ServiceBusExplorer. The Portal doesn't expose session browsing.
- **Offline or emulator-only work** → ServiceBusExplorer. The Portal requires an Azure subscription; ServiceBusExplorer connects directly to any endpoint.

---

## Aspire dashboard — operational visibility

When the Cab AppHost includes `AddAzureServiceBus("messaging").RunAsEmulator()`, the Aspire dashboard shows:

### Resource status

The ASB Emulator container appears as a resource tile with health status and the exposed endpoint. If a service fails to connect, check here first — the emulator might still be starting.

### Resource graph

The resource graph shows which services depend on the `messaging` resource (via `.WithReference(messaging)` in the AppHost). A quick visual confirmation that Trips, Payments, Ratings, and Operations are all wired to the same ASB instance.

### Traces

OpenTelemetry traces spanning ASB publish and consume operations appear in the Traces view. A `TripCompleted` event published by Trips and consumed by Payments shows as a single distributed trace with spans for both sides.

The Aspire dashboard does **not** show queue depths, message contents, or subscription details. Those require `az servicebus`, the Portal Explorer, or ServiceBusExplorer.

---

## ASB Emulator interaction

The ASB Emulator is a Docker container that implements the full ASB API surface locally. Aspire orchestrates it via `RunAsEmulator()` (see `wolverine-azure-service-bus` § Local development).

### Connection string

The emulator's connection string is injected by Aspire via `WithReference(messaging)`. To connect manually (for CLI tools or ServiceBusExplorer outside Aspire):

```bash
# Find the emulator container
docker ps --filter "label=aspire-resource-name=messaging" --format "{{.ID}} {{.Ports}}"

# The emulator exposes port 5672 (AMQP) mapped to a host port.
# The connection string format:
# Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true
```

### Pre-provisioning entities

When using Aspire, entities are pre-provisioned via `AddServiceBusTopic()` and `AddServiceBusQueue()` in the AppHost. When using the emulator directly (e.g., in CI with Testcontainers), use `az servicebus` against the emulator or let Wolverine's `AutoProvision()` create them at startup — the emulator supports the management API.

### Emulator limitations

The ASB Emulator supports the core Service Bus features (queues, topics, subscriptions, sessions, dead-lettering, scheduled delivery, duplicate detection). Premium-tier features (large messages over 256 KB, virtual network integration, geo-disaster recovery) are not available. For Cab's domain-event workloads, no emulator limitations are expected to be blocking.

---

## Common patterns

### Checking for dead-lettered messages after a deploy

```bash
# Quick check: are there DLQ messages on the payments subscription?
az servicebus topic subscription show \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --topic-name trips.trip-completed \
  --name payments \
  --query "countDetails.deadLetterMessageCount" -o tsv

# If non-zero, open the Portal → subscription → Service Bus Explorer → Dead-letter
# to inspect DeadLetterReason and decide whether to fix-and-replay.
```

### Replaying dead-lettered messages after a bug fix

After deploying the fix:

- **Small number (< 100):** Portal Service Bus Explorer → Dead-letter → select messages → Resend.
- **Large number:** ServiceBusExplorer desktop → export DLQ messages to file → verify the fix handles them → import back to the main subscription.

### Verifying subscription rules

```bash
# List rules on the pricing subscription
az servicebus topic subscription rule list \
  --namespace-name crittercab-messaging \
  --resource-group crittercab-rg \
  --topic-name trips.trip-completed \
  --subscription-name pricing \
  --output table

# Expected: a SQL filter rule for high-fare trips, no $Default rule.
# If $Default is present alongside a filter, the subscription receives ALL messages
# (the filter is additive, not replacing).
```

### Provisioning Event Hubs for Kafka transport

```bash
# Create the event hub namespace (Standard tier for Kafka protocol support)
az eventhubs namespace create \
  --name crittercab-telemetry \
  --resource-group crittercab-rg \
  --sku Standard \
  --enable-kafka true

# Create event hubs matching Cab's Kafka topics
az eventhubs eventhub create --namespace-name crittercab-telemetry \
  --resource-group crittercab-rg --name telemetry.location-pings --partition-count 6
az eventhubs eventhub create --namespace-name crittercab-telemetry \
  --resource-group crittercab-rg --name telemetry.demand-signals --partition-count 3
az eventhubs eventhub create --namespace-name crittercab-telemetry \
  --resource-group crittercab-rg --name pricing.surge-updates --partition-count 3
```

---

## Common pitfalls

- **Using `az servicebus` to peek at messages.** `az servicebus` is management-plane only — it creates and configures entities but cannot peek at, send, or receive messages. Use the Portal Service Bus Explorer or ServiceBusExplorer desktop for message-level operations.

- **Leaving the `$Default` subscription rule when adding a SQL filter.** New subscriptions start with a `$Default` rule that accepts all messages. Adding a SQL filter creates an additional rule — the subscription then receives messages matching the filter AND all messages via `$Default`. Delete `$Default` after adding your filter, or the filter has no effect.

- **Replaying DLQ messages without fixing the root cause.** Replayed messages go through the same handler. If the bug isn't fixed, they'll fail again and return to the DLQ, wasting the `MaxDeliveryCount` budget. Fix first, replay second.

- **Manually creating subscription rules that Wolverine also manages.** Wolverine's `AutoProvision()` reconciles subscription rules at startup — it deletes rules it doesn't recognize. If you create rules via `az servicebus` and Wolverine also manages rules on the same subscription, Wolverine will delete your manual rules on the next restart.

- **Confusing `az servicebus` and `az eventhubs` scopes.** ASB is for domain events (queues, topics, subscriptions). Event Hubs is for Kafka transport (event hubs, consumer groups, partitions). They are separate Azure services with separate namespaces, even though Cab uses both.

- **Forgetting `--enable-kafka` when creating an Event Hubs namespace.** Without this flag, the Kafka protocol endpoint is not enabled. Wolverine's Kafka transport will fail to connect.

- **Using ReceiveAndDelete mode in the Portal Explorer on a production queue.** The Portal Explorer's receive mode is destructive — messages are removed from the queue. Use **Peek** mode for inspection. Reserve **Receive** mode for intentional consumption (e.g., draining test messages).

- **Expecting the Aspire dashboard to show queue depths.** The dashboard shows container health, resource graph, and traces — not queue depths or message counts. Use `az servicebus queue show` or the Portal for entity-level metrics.

- **Sending a test message without setting the Subject field.** Wolverine resolves the handler from the ASB message's `Subject` property (which carries the message type). A test message sent via the Portal Explorer without a Subject produces a "No handler for message type" warning in the Wolverine logs.

---

## See also

### Upstream

- `wolverine-azure-service-bus` — the Wolverine transport configuration that produces the ASB entities these tools inspect. Read this first.
- `wolverine-kafka` — the Kafka transport wiring against Azure Event Hubs; the `az eventhubs` section here handles the management plane for that transport.
- `transport-selection` — the decision framework that routes flows to ASB or Kafka; context for which entities exist and why.
- `aspire` — Aspire orchestration of the ASB Emulator and the dashboard.

### Sibling skills

- `cli-kafka-tooling` — kcat and kafka-console-* for Kafka message inspection; the Kafka counterpart to this skill.
- `cli-aspire` — the Aspire CLI for orchestrating; complements this skill for the dev inner loop.
- `cli-jasperfx` — Wolverine's in-process diagnostics; the server-side complement to ASB-side inspection.

### Downstream

- `observability-tracing` (Phase 3) — the OTel trace pipeline that produces the traces visible in the Aspire dashboard.
- `testing-advanced` (Phase 4) — integration tests against ASB using `Testcontainers.ServiceBus`.

### External

- [Azure CLI Service Bus reference](https://learn.microsoft.com/en-us/cli/azure/servicebus) — full `az servicebus` command reference.
- [Azure CLI Event Hubs reference](https://learn.microsoft.com/en-us/cli/azure/eventhubs) — full `az eventhubs` command reference.
- [Azure Portal Service Bus Explorer](https://learn.microsoft.com/en-us/azure/service-bus-messaging/explorer) — message peeking, sending, and DLQ inspection.
- [ServiceBusExplorer on GitHub](https://github.com/paolosalvatori/ServiceBusExplorer) — desktop tool for advanced ASB operations.
- [ASB Emulator documentation](https://learn.microsoft.com/en-us/azure/service-bus-messaging/test-locally-with-service-bus-emulator) — local emulator setup and limitations.
