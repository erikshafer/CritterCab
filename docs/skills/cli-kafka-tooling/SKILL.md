---
name: cli-kafka-tooling
description: "CLI tools for working with Kafka in CritterCab: kcat (formerly kafkacat) for producing, consuming, listing topics, inspecting offsets, and managing consumer groups from the command line; kafka-console-* bundled scripts for admin operations inside the Kafka container; and the Aspire dashboard for operational visibility over the Kafka resource, traces, and logs. Covers per-tool installation, the canonical Cab invocations against Aspire-orchestrated Kafka, Event Hubs connection differences, and common debugging patterns (inspecting a topic after a failing handler, checking consumer group lag, replaying from an offset)."
cluster: infrastructure
tags: [cli, kafka, kcat, consumer-groups, offsets, partitions, aspire-dashboard, event-hubs, debugging, telemetry]
---

# Kafka CLI Tooling

Two command-line tools and the Aspire dashboard cover the operational surface for Kafka work in CritterCab:

- **kcat** (formerly kafkacat) — the primary CLI. A non-JVM Swiss-army knife for Kafka: produces messages, consumes messages, lists topics and partitions, inspects offsets, and queries consumer-group state. Think of it as netcat for Kafka. This is the everyday tool for "what's on this topic?" and "why isn't my handler seeing messages?"
- **kafka-console-\*** — the bundled scripts that ship inside the Kafka container (`kafka-topics`, `kafka-console-producer`, `kafka-console-consumer`, `kafka-consumer-groups`). Useful for admin operations (topic creation, consumer-group offset resets) when kcat isn't available or when the operation needs the Kafka admin API directly.
- **Aspire dashboard** — the resource dashboard that Aspire starts alongside the Kafka container. Shows container health, logs, endpoints, the resource-dependency graph, and OpenTelemetry traces that flow through Kafka. It is not a topic browser — it's operational visibility at the infrastructure layer.

The three are complementary. **kcat** is the daily-driver CLI for message-level inspection. **kafka-console-\*** tools handle admin-plane operations inside the container. The **Aspire dashboard** shows the infrastructure context around Kafka — which services depend on it, whether it's healthy, and the traces that pass through it.

This skill operationalizes the tooling side that `wolverine-kafka` deliberately deferred: the command-line patterns for inspecting topics, debugging message flow, and verifying partition distribution.

## When to apply this skill

**Use this skill when:**

- Listing Kafka topics to confirm routing rules took effect.
- Consuming messages from a topic to verify a handler is publishing correctly.
- Producing a test message to exercise a handler without wiring up the full publishing service.
- Inspecting consumer-group lag to diagnose a handler falling behind.
- Replaying messages from a specific offset for debugging.
- Checking the Aspire dashboard for Kafka container health or trace flow.

**Do NOT use this skill for:**

- Wolverine transport configuration (routing rules, listeners, publishers) — see `wolverine-kafka`.
- Deciding which transport a flow belongs on — see `transport-selection`.
- Aspire AppHost configuration (`AddKafka`, `WithReference`) — see `aspire`.
- Aspire CLI commands (`aspire run`, `aspire init`) — see `cli-aspire`.
- Azure Service Bus CLI tooling — see `cli-azure-messaging` (Phase 3).

---

## Tools at a glance

| Tool | Primary use | Cab daily-driver scenario |
|---|---|---|
| `kcat` | Produce, consume, list topics, inspect offsets | "What messages are on `telemetry.location-pings`?" / "Is the partition key correct?" |
| `kafka-console-*` | Topic admin, consumer-group management | "Create this topic before starting the EH Emulator test" / "Reset consumer-group offsets" |
| Aspire dashboard | Container health, logs, traces | "Is the Kafka container up?" / "Trace this request through Kafka" |

---

## kcat — primary CLI

### Installation

kcat is a native C binary linked against librdkafka. Installation differs by platform:

```bash
# macOS
brew install kcat

# Ubuntu / Debian
sudo apt-get install kcat

# Fedora
sudo dnf install kcat
```

On **Windows**, kcat does not have a scoop or winget package. Two practical options:

```bash
# Option A: Docker (works everywhere, no local build)
docker run --rm -it --network host edenhill/kcat:1.7.1 -b localhost:9092 -L

# Option B: WSL (install via apt inside WSL, then call directly)
wsl kcat -b localhost:9092 -L
```

The Docker approach is the path of least resistance for Windows developers on Cab. Alias it in your shell profile to reduce typing:

```bash
# ~/.bashrc or PowerShell profile
alias kcat='docker run --rm -it --network host edenhill/kcat:1.7.1'
```

Verify the installation:

```bash
kcat -V
# kcat - Apache Kafka producer and consumer tool
# ...
# builtin.features: gzip,snappy,ssl,sasl,...
```

### Metadata mode — listing topics and partitions

The `-L` flag queries broker metadata:

```bash
# List all topics and their partitions
kcat -b localhost:9092 -L

# Output (abbreviated):
# Metadata for all topics (broker 1: localhost:9092/1):
#  topic "telemetry.location-pings" with 6 partitions:
#    partition 0, leader 1, replicas: 1, isrs: 1
#    partition 1, leader 1, replicas: 1, isrs: 1
#    ...
#  topic "telemetry.demand-signals" with 3 partitions:
#    ...
#  topic "wolverine-dead-letter-queue" with 1 partitions:
#    ...
```

Filter to a single topic with `-t`:

```bash
kcat -b localhost:9092 -L -t telemetry.location-pings
```

This is the first thing to run when a handler isn't receiving messages — confirm the topic exists and has the expected partition count.

### Consumer mode — reading messages

The `-C` flag starts a consumer:

```bash
# Consume from the beginning of all partitions
kcat -b localhost:9092 -C -t telemetry.location-pings -o beginning

# Consume only the last 5 messages (tail)
kcat -b localhost:9092 -C -t telemetry.location-pings -o -5

# Consume from a specific offset on partition 2
kcat -b localhost:9092 -C -t telemetry.location-pings -p 2 -o 1042
```

By default, kcat prints the message value (the JSON body for Wolverine messages). To see headers, keys, and partition metadata — critical for debugging Wolverine envelope issues — use the `-f` format string:

```bash
# Show partition, offset, key, headers, and value
kcat -b localhost:9092 -C -t telemetry.location-pings -o beginning \
  -f 'P:%p O:%o K:%k H:%h\n%s\n---\n'
```

Format placeholders:

| Placeholder | Meaning |
|---|---|
| `%p` | Partition number |
| `%o` | Offset |
| `%k` | Message key (the Wolverine partition key or envelope ID) |
| `%h` | Headers (all key=value pairs) |
| `%s` | Message value (the body) |
| `%T` | Timestamp (milliseconds since epoch) |
| `%t` | Topic name |

The `%h` output shows Wolverine's envelope headers — `message-type`, `id`, `correlation-id`, `content-type`, and any custom headers. When a handler isn't matching a message type, inspecting `%h` is the fastest path to the root cause.

### Producer mode — sending test messages

The `-P` flag starts a producer:

```bash
# Produce a single message (type the JSON, then Ctrl+D to send)
kcat -b localhost:9092 -P -t telemetry.location-pings

# Produce from a file
kcat -b localhost:9092 -P -t telemetry.location-pings < test-ping.json

# Produce with a specific key (partition key)
kcat -b localhost:9092 -P -t telemetry.location-pings \
  -K : <<< "driver-abc:$(cat test-ping.json)"
```

The `-K :` flag sets the key delimiter. Everything before the first `:` in each line becomes the Kafka message key; everything after becomes the value. For Cab's GPS pings, the key is the `driver_id` — setting it here ensures the test message lands in the same partition as production traffic for that driver.

When producing test messages for a Wolverine consumer, be aware that the default Wolverine envelope mapper expects envelope metadata in Kafka headers. A raw JSON message produced by kcat won't have those headers. If the Wolverine listener uses the default mapper, it will reject the message. Two options:

- Configure the listener with `.ReceiveRawJson<LocationPing>()` (per `wolverine-kafka`) so it accepts headerless JSON.
- Add headers with kcat's `-H` flag to simulate a Wolverine envelope:

```bash
kcat -b localhost:9092 -P -t telemetry.location-pings \
  -H "message-type=CritterCab.Telemetry.LocationPing" \
  -H "content-type=application/json" \
  -H "id=$(uuidgen)" \
  < test-ping.json
```

### Consumer groups and offsets

kcat's `-G` flag starts a high-level consumer that joins a consumer group:

```bash
# Join consumer group "dispatch-service" on the location-pings topic
kcat -b localhost:9092 -G dispatch-service telemetry.location-pings
```

This is useful for verifying that a service's consumer group is correctly configured — if kcat can join the group and receive messages, the Wolverine listener should too.

To inspect consumer-group offsets without consuming, use `kafka-consumer-groups` (see below) — kcat doesn't have a dedicated offset-inspection mode.

### Connecting to Azure Event Hubs

When the Kafka broker is Azure Event Hubs (or the EH Emulator with SASL), kcat needs SASL configuration:

```bash
kcat -b <namespace>.servicebus.windows.net:9093 \
  -X security.protocol=SASL_SSL \
  -X sasl.mechanism=PLAIN \
  -X sasl.username='$ConnectionString' \
  -X sasl.password='Endpoint=sb://...' \
  -L
```

For the local EH Emulator (when not using Aspire's standard Kafka container):

```bash
kcat -b localhost:<emulator-kafka-port> \
  -X security.protocol=SASL_PLAINTEXT \
  -X sasl.mechanism=PLAIN \
  -X sasl.username='$ConnectionString' \
  -X sasl.password='<emulator-connection-string>' \
  -L
```

Against Aspire's `AddKafka("kafka")` container (a real Kafka broker, no SASL), kcat connects with just `-b localhost:<port>` — no security configuration needed.

---

## kafka-console-\* — bundled Kafka scripts

These scripts ship inside the Kafka container that Aspire orchestrates via `AddKafka("kafka")`. Access them by `docker exec` into the running container.

### Finding the container

```bash
# List running containers — look for the Kafka container started by Aspire
docker ps --filter "ancestor=confluentinc/confluent-local" --format "{{.ID}} {{.Names}}"
```

The container name depends on Aspire's naming convention; it typically includes `kafka` in the name.

### Topic management

```bash
# List topics
docker exec -it <container> kafka-topics --bootstrap-server localhost:9092 --list

# Describe a specific topic (shows partitions, replicas, configs)
docker exec -it <container> kafka-topics --bootstrap-server localhost:9092 \
  --describe --topic telemetry.location-pings

# Create a topic (useful when AutoProvision isn't available, e.g., EH Emulator)
docker exec -it <container> kafka-topics --bootstrap-server localhost:9092 \
  --create --topic telemetry.location-pings --partitions 6 --replication-factor 1
```

### Consumer-group inspection

```bash
# List consumer groups
docker exec -it <container> kafka-consumer-groups --bootstrap-server localhost:9092 --list

# Describe a specific group (shows per-partition lag)
docker exec -it <container> kafka-consumer-groups --bootstrap-server localhost:9092 \
  --describe --group dispatch-service

# Output:
# GROUP            TOPIC                       PARTITION  CURRENT-OFFSET  LOG-END-OFFSET  LAG
# dispatch-service telemetry.location-pings    0          1042            1050            8
# dispatch-service telemetry.location-pings    1          987             987             0
# ...
```

The LAG column is the gap between the consumer's committed offset and the topic's end offset. A growing lag on one partition means that partition's handler is falling behind — likely a slow downstream dependency or a partition with disproportionate traffic (hot key).

### Offset reset

```bash
# Reset consumer group to the beginning of all partitions (requires group to be inactive)
docker exec -it <container> kafka-consumer-groups --bootstrap-server localhost:9092 \
  --group dispatch-service --topic telemetry.location-pings \
  --reset-offsets --to-earliest --execute

# Reset to a specific offset on partition 2
docker exec -it <container> kafka-consumer-groups --bootstrap-server localhost:9092 \
  --group dispatch-service --topic telemetry.location-pings:2 \
  --reset-offsets --to-offset 500 --execute
```

The consumer group must be **inactive** (all consumers stopped) before resetting offsets. In practice, this means stopping the Wolverine service, resetting, then restarting. In local dev this is straightforward; in production it requires coordination.

### Produce and consume (quick-and-dirty)

```bash
# Produce to a topic (type messages, one per line, Ctrl+D to finish)
docker exec -it <container> kafka-console-producer \
  --bootstrap-server localhost:9092 --topic telemetry.location-pings

# Consume from a topic (from the beginning)
docker exec -it <container> kafka-console-consumer \
  --bootstrap-server localhost:9092 --topic telemetry.location-pings --from-beginning
```

For anything beyond the simplest produce/consume, kcat is more ergonomic — it runs outside the container, supports format strings, and handles keys and headers natively.

---

## Aspire dashboard — operational visibility

When the Cab AppHost runs (`aspire run` or via the IDE), the Aspire dashboard starts at `https://localhost:17220` (or the port Aspire reports). The dashboard provides three layers of Kafka visibility:

### Resource status

The **Resources** page shows the Kafka container as a resource tile with its health status (healthy/unhealthy), the exposed endpoint (e.g., `localhost:9092`), and container logs. If a service fails to connect to Kafka, check here first — the container might not have finished starting, or the health check might be failing.

### Resource graph

The **Resource Graph** view shows which services depend on the Kafka resource (via `.WithReference(kafka)` in the AppHost). This is a quick way to confirm that the Telemetry, Dispatch, and Pricing services are all wired to the same Kafka instance.

### Traces

The **Traces** page shows OpenTelemetry traces that span Kafka publish and consume operations. Wolverine emits spans for both sides; the Aspire dashboard correlates them. A publish span in the Telemetry service and a consume span in the Dispatch service appear as part of the same distributed trace — this is the most powerful debugging view for "did my message get from A to B?"

The Aspire dashboard does **not** provide a topic browser, message inspector, or consumer-group viewer. Those operations require kcat or `kafka-consumer-groups` inside the container. The dashboard's role is infrastructure and trace visibility, not message-level inspection.

---

## Common patterns

### Inspecting a topic after a failing handler

When a Wolverine handler isn't processing messages as expected:

```bash
# 1. Confirm the topic exists and has the expected partitions
kcat -b localhost:9092 -L -t telemetry.location-pings

# 2. Peek at the latest messages with headers
kcat -b localhost:9092 -C -t telemetry.location-pings -o -3 \
  -f 'P:%p O:%o K:%k\nHeaders: %h\nBody: %s\n---\n'

# 3. Check whether the message-type header matches what the handler expects
# If the header says "CritterCab.Telemetry.LocationPing" but the handler
# expects "LocationPing" (without namespace), the handler won't fire.
```

### Verifying partition distribution

After publishing a batch of GPS pings with `PartitionKey = driverId`:

```bash
# Consume all messages and tally by partition
kcat -b localhost:9092 -C -t telemetry.location-pings -o beginning -e \
  -f '%p\n' | sort | uniq -c | sort -rn

# Expected: messages for the same driver_id cluster in the same partition.
# If distribution is perfectly uniform, partition keys aren't being set
# (Wolverine is falling back to the envelope GUID as the key).
```

### Replaying from a specific offset

When debugging a specific failing message identified in logs:

```bash
# The log shows: "Failed processing offset 1042 on partition 2"
kcat -b localhost:9092 -C -t telemetry.location-pings -p 2 -o 1042 -c 1 \
  -f '%s\n'

# This prints exactly one message (-c 1) starting at offset 1042 on partition 2.
```

### Checking consumer-group lag

```bash
docker exec -it <container> kafka-consumer-groups --bootstrap-server localhost:9092 \
  --describe --group dispatch-service

# Look for:
# - LAG > 0 on specific partitions → handler is behind on that partition
# - CURRENT-OFFSET = - (dash) → consumer hasn't committed yet (first start or misconfigured)
# - No active members → Wolverine service isn't running or isn't connecting
```

---

## Common pitfalls

- **Producing headerless JSON to a Wolverine default-mapper listener.** kcat sends raw bytes; Wolverine's default mapper expects envelope metadata in Kafka headers (`message-type`, `content-type`, `id`). Without them, the listener can't resolve a handler. Either configure the listener with `.ReceiveRawJson<T>()` or add `-H` flags when producing.

- **Using `-b localhost:9092` against the Event Hubs Emulator.** The EH Emulator requires SASL authentication even locally. Use `-X security.protocol=SASL_PLAINTEXT` and the emulator's connection string. Against Aspire's standard Kafka container (`AddKafka`), plain `localhost:9092` works.

- **Forgetting `-o beginning` when consuming.** Without an offset flag, kcat defaults to consuming from the end of the topic (latest). New messages will appear, but existing ones won't. Use `-o beginning` to see all messages, `-o -N` for the last N, or `-o <offset>` for a specific position.

- **Running `kafka-consumer-groups --reset-offsets` while the consumer is active.** The Kafka broker rejects offset resets when consumer-group members are connected. Stop the Wolverine service first, reset, then restart.

- **Confusing container-internal and host-visible ports.** Inside the Docker container, Kafka listens on `localhost:9092`. From the host machine, the port might be mapped differently (Aspire's `AddKafka` maps it dynamically). Use `docker ps` or the Aspire dashboard to find the host-visible port.

- **Running `kafka-topics --create` against the Event Hubs Emulator.** The EH Emulator doesn't support the Kafka admin API. Topic creation requires the EH management plane (REST API or Azure CLI). Against Aspire's standard Kafka container, `kafka-topics --create` works.

- **Expecting the Aspire dashboard to show topic contents.** The dashboard shows container health, resource dependencies, and OTel traces — not a topic browser. Use kcat for message-level inspection.

- **Forgetting the `-e` (exit) flag when piping kcat output.** Without `-e`, kcat keeps consuming indefinitely, waiting for new messages. When piping to `wc -l` or `sort | uniq -c`, the pipeline never completes. Add `-e` to exit when the end of the topic is reached.

- **Using kcat's `-G` (consumer group) mode for debugging and leaving a ghost consumer.** kcat's `-G` mode joins a real consumer group and triggers a rebalance. If the Wolverine service is also running, kcat steals partitions from it. Use `-C` (simple consumer) for read-only inspection — it doesn't join a consumer group.

- **Not checking the Wolverine dead letter topic.** When messages fail processing, they may land in `wolverine-dead-letter-queue` (or whatever DLT name is configured per `wolverine-kafka`). Inspect it with kcat:

```bash
kcat -b localhost:9092 -C -t wolverine-dead-letter-queue -o beginning \
  -f 'K:%k\nHeaders: %h\nBody: %s\n---\n'
```

The headers include `exception-type`, `exception-message`, and `exception-stack` — the full failure context.

---

## See also

### Upstream

- `wolverine-kafka` — the Wolverine transport configuration that produces the topics and consumer groups these tools inspect. Read this first to understand what you're looking at.
- `transport-selection` — the decision framework that routes flows to Kafka; context for why certain topics exist.
- `aspire` — Aspire orchestration of the Kafka container (`AddKafka("kafka")`) and the dashboard that shows its health.

### Sibling skills

- `cli-aspire` — the Aspire CLI for `aspire run` and `aspire init`; complements this skill for the dev inner loop.
- `cli-jasperfx` — Wolverine's in-process diagnostics (`describe-routing`, `describe-system`); the server-side complement to kcat's client-side inspection.

### Downstream

- `cli-azure-messaging` (Phase 3) — Azure Service Bus CLI tooling; the ASB counterpart to this skill.
- `observability-tracing` (Phase 3) — the OTel trace pipeline that produces the traces visible in the Aspire dashboard.
- `testing-advanced` (Phase 4) — integration tests against Kafka using `Testcontainers.Kafka`, where these CLI patterns inform test verification.

### External

- [kcat on GitHub](https://github.com/edenhill/kcat) — the canonical reference for kcat flags, format strings, and installation.
- [Confluent kcat usage guide](https://docs.confluent.io/platform/current/tools/kafkacat-usage.html) — Confluent's kcat tutorial with examples.
- [Aspire Kafka hosting integration](https://learn.microsoft.com/en-us/dotnet/aspire/messaging/kafka-integration) — the `AddKafka` API and dashboard integration.
- [Apache Kafka CLI tools reference](https://kafka.apache.org/documentation/#quickstart) — the bundled `kafka-topics`, `kafka-console-producer`, `kafka-console-consumer`, and `kafka-consumer-groups` documentation.
