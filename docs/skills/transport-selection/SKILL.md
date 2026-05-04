---
name: transport-selection
description: "Decision framework for choosing between gRPC, Kafka (via Event Hubs), and Azure Service Bus per flow shape in CritterCab. Use when designing any cross-service flow, before reaching for a transport-specific implementation skill."
cluster: transports
tags: [transports, grpc, kafka, asb, event-hubs, wolverine, adr-005, decision-framework]
---

# Transport Selection

A decision framework for choosing the transport for a cross-service flow in CritterCab. CritterCab uses three transports — gRPC, Kafka (against Azure Event Hubs in cloud and the EH Emulator locally), and Azure Service Bus — and ADR-005 fixes which transport handles which kind of flow. This skill makes that decision explicit, so the choice is mechanical at design time rather than ad hoc at implementation time.

The bottom line: **flow shape determines transport.** Do not default to a transport because it is familiar or because it is what's wired up. Identify the shape first, then choose.

## When to apply this skill

Use this skill when:

- Designing any new flow that crosses a service boundary.
- Reviewing an Event Modeling slice that has an integration step (cross-BC handoff).
- Choosing where a `Wolverine.PublishMessage<T>()` call should route in service startup configuration.
- Reviewing a PR that adds or moves a routing rule.
- Sanity-checking a transport choice that "feels off" during implementation.

This skill answers *which* transport. The transport-specific implementation patterns (handler shapes, configuration, error handling) live in their own skills, called out in See Also.

## The Three Transports

CritterCab commits to three transports. Each fits a specific flow shape; none is a default.

| Transport | Implementation | Flow shape it owns |
|---|---|---|
| **gRPC** | Wolverine 5.32+ | Service-to-service calls and streaming interactions where the caller awaits a response or stream from a specific callee. |
| **Kafka** | Wolverine's Kafka transport, against Azure Event Hubs (cloud) or the EH Emulator (local) | High-volume, append-only streams broadcast to multiple downstream consumers. |
| **Azure Service Bus** | Wolverine's ASB transport, against ASB (cloud) or the ASB Emulator (local) | Cross-service domain events that need reliable delivery, dead-lettering, session ordering, or topic-based routing. |

RabbitMQ is deliberately excluded. The three transports cover all required flow shapes; adding a fourth would bring cost without new capability. ADR-005 makes this decision explicit.

---

## Flow Shape Decision Framework

The decision proceeds by elimination. Walk it in order; the first match wins.

### 1. Is this a request-response or streaming interaction with a specific caller and callee?

Signals: there's a known producer that wants a response (or a stream of responses) from a known consumer. Latency matters. The flow has caller-callee semantics, not pub/sub semantics.

→ **gRPC.** All four streaming modes are valid. Choose by message direction:

| Direction | gRPC mode | CritterCab examples |
|---|---|---|
| One request, one response | Unary | `RequestRide` → Dispatch; `AcceptOffer` → Dispatch; `CompleteTrip` → Trips; query methods. |
| One request, stream of responses | Server-streaming | `StreamDriverOffers` (Dispatch fans offers to a candidate driver client); `WatchTripStatus` (Trips streams updates to the rider client); the Operations live map. |
| Stream of requests, one response | Client-streaming | `PushTelemetry` (mobile client streams GPS pings into the Telemetry service — the GPS ingest path). |
| Stream of requests, stream of responses | Bidirectional | Real-time driver-rider communication during a trip, where justified. |

### 2. Is this a high-volume, append-only stream of records consumed by potentially multiple downstream services?

Signals: the producer fires data continuously, the consumers process the stream in order, brief consumer lag is acceptable, and the same data may feed several different downstream concerns. Volume is high enough that per-message overhead matters.

→ **Kafka.** The two flows in this category in CritterCab are GPS pings (Telemetry → Dispatch and Pricing) and surge-pricing demand signals.

### 3. Is this a domain event that crosses a service boundary?

Signals: a service has produced a fact other services need to know about. The flow is event-driven (no synchronous response is awaited). Reliability matters: the event must be delivered, dead-lettered if it fails, and possibly delivered in session order. Consumers may be added later without changing the producer.

→ **Azure Service Bus.** Most cross-service domain events in CritterCab live here: `RiderRegistered`, `DriverApproved`, `TripCompleted`, `PaymentCaptured`, `RatingSubmitted`, etc. External provider events (Microsoft Graph user-lifecycle notifications) also land on ASB.

If none of the three categories fit, the flow probably doesn't actually cross a service boundary, or the design is unclear. Resolve that first.

---

## Why Each Transport Fits Its Flow Shape

### gRPC (via Wolverine 5.32+)

- **Per-call semantics.** The caller and callee are explicit. The four streaming modes match the interactive flow shapes that ride-sharing produces (`RequestRide` is unary; offer fan-out is server-streaming; GPS ingest is client-streaming).
- **Low latency.** No broker hop. Direct service-to-service over HTTP/2.
- **Strongly typed contracts.** Service and message definitions are protobuf, governed by `protobuf-contracts` and ADR-009. The contract is the design.
- **Cross-language by construction.** The Go service (per the vision doc's polyglot goal) consumes the same protos.
- **Wolverine integration.** Handlers look like normal Wolverine handlers; the gRPC streaming primitives surface as `IAsyncEnumerable<T>` parameters and return types. See `wolverine-grpc-services` (Phase 3).

### Kafka (against Azure Event Hubs)

- **Log model.** Append-only, partition-ordered. Producers don't address a specific consumer; consumers track their own offset and read at their own pace.
- **High throughput, low per-message overhead.** Designed for millions of messages per second across a partitioned topic.
- **Multiple consumers, independent offsets.** GPS pings can be consumed by Dispatch (for matching) and Pricing (for surge signals) independently — each consumer group reads the same stream at its own pace.
- **Replay and time-travel.** Consumers can rewind to a past offset for backfills, debugging, or new-consumer onboarding.
- **CritterCab-specific:** the broker is Azure Event Hubs in cloud and the EH Emulator (Docker) locally. Wolverine speaks Kafka protocol against both. **Production EH constraint:** the EH Emulator only supports Kafka producer and consumer APIs — admin operations like topic creation use the management API or `az eventhubs`, not Kafka admin protocol. See `wolverine-kafka` (Phase 3).

### Azure Service Bus

- **Topic-and-subscription model.** Producers publish to a topic; consumers subscribe with their own filters. New consumers don't require producer changes.
- **Dead-letter queues built in.** Failed messages don't disappear — they land on a DLQ where they can be inspected, re-processed, or discarded explicitly.
- **Session ordering when needed.** Messages within a session are delivered in order to a single consumer; sessions across the same topic are processed in parallel. Useful for "all `TripCompleted` events for a single trip in order, but trips don't block each other."
- **Scheduled message delivery.** Native support for "deliver this message at time X." Used by sagas that need delayed message dispatch (e.g., a check-in reminder N minutes after a trip starts).
- **Operational visibility.** Service Bus Explorer (Windows GUI for cloud namespaces) and `az servicebus` (cross-platform CLI) provide inspection without writing code.
- **CritterCab-specific:** the broker is Azure Service Bus in cloud and the ASB Emulator (Docker) locally. The ASB Emulator gained management API support in early 2026 — see `wolverine-azure-service-bus` (Phase 3) and `cli-azure-messaging` (Phase 3) for the operational details.

---

## Anti-Patterns: Deliberate Non-Fits

These are the wrong answers that look right under pressure. Each reflects a real pull toward "use what's already there" that the framework deliberately resists.

**Don't use Kafka for business domain events.** Kafka's log model lacks dead-letter queues, session ordering, scheduled delivery, and the operational features that business events require. Putting `TripCompleted` on Kafka because GPS pings are already on Kafka is a category error — they have different shapes. ADR-005 names this explicitly.

**Don't use ASB for high-volume telemetry.** ASB's per-message overhead, queue depth concerns, and topic-subscription cost model are not designed for thousands of messages per second per producer. Kafka is the right fit.

**Don't use gRPC for fire-and-forget event publishing.** gRPC's value is in the response (or response stream). If the producer doesn't care about the consumer's response, gRPC is the wrong shape — even if the destination is a single service. Use ASB and let the consumer subscribe to the topic. The producer publishes once; consumers (including future ones) wire themselves up at their leisure.

**Don't reach for a fourth transport.** RabbitMQ, NATS, MQTT, and similar may all fit a particular flow shape better than the three Cab uses. None of them earns a fourth slot. The cost of a fourth broker — local infrastructure, deployment, operational knowledge, on-call expertise — is not justified by any of the flows the project has identified.

---

## Edge Cases

A few flow shapes are not as obvious as they look. The framework still produces a clear answer; this section names the cases.

### "I have a local domain event AND I want to publish to other services"

This is the common case. The handler emits two things:

1. The slim **domain event** (e.g., `TripCompleted` in the Trips event stream — see `domain-event-conventions`).
2. The rich **integration event** published over ASB to the consumers.

Wolverine's outgoing-message envelope handles both. The domain event lands in Marten via the aggregate workflow; the integration event is routed by the bus configuration. Handlers are transport-agnostic — see "Transport in Routing, Not in Handlers" below.

### "I have a high-volume event that's also a business event"

Almost never both. If it really is both, the flow is two flows that happen to share a name:

- The **high-volume signal** (every GPS ping, every surge-eligible demand pulse) goes to Kafka.
- The **sampled or aggregated business event** (e.g., "driver entered surge zone" emitted once per zone-entry rather than per ping) goes to ASB.

Don't try to make one transport carry both volumes. Split the flow.

### "I want to broadcast something to many recipients"

The recipients determine the transport:

- **Recipients are end clients (rider/driver mobile apps, ops dashboard browser).** That's gRPC server-streaming. Each client opens a stream; the producer fans out.
- **Recipients are other services.** That's ASB topic fan-out. Each service subscribes; the broker handles distribution.
- **Recipients are stream-processing consumers (downstream pipelines).** That's Kafka. Each consumer group reads the same partitioned stream.

### "I want to send a message in 30 seconds"

ASB has native scheduled delivery via `ScheduledEnqueueTime`. Kafka does not, and gRPC has no concept of scheduled delivery at all. Use ASB.

If the delay is part of a saga's timeout-and-retry pattern, see `wolverine-sagas` (Phase 4) for the saga-level scheduling primitives.

### "I want a request-response between services but the response stream is long-lived"

That's still gRPC — server-streaming if responses come from a single callee, bidirectional if requests and responses interleave. Long-lived gRPC streams are normal in Cab (the Operations live map and live trip dashboards both use them).

### "I'm not sure if this is high-volume enough for Kafka"

Default to ASB. Kafka's value over ASB is at high volume; below that threshold, ASB's operational features (DLQ, sessions, visibility) outweigh the throughput advantage. The two committed Kafka flows (GPS pings, surge demand) are clearly high-volume by the domain's nature; if a flow's volume is in question, it's almost certainly an ASB candidate.

---

## Transport in Routing, Not in Handlers

Wolverine's multi-transport support means transport selection is expressed in the bus's *routing configuration*, not in handler code:

```csharp
// In a service's Program.cs or composition root
opts.PublishMessage<TripCompleted>().ToAzureServiceBusTopic("trips.events");
opts.PublishMessage<LocationPing>().ToKafkaTopic("telemetry.pings");
// gRPC services declared via Wolverine's gRPC API (see wolverine-grpc-services)
```

Handlers do not know or care which transport delivered a message. This means:

- A flow can be moved between transports by changing one routing line. Handler code does not change.
- The transport choice is auditable in one place per service (the composition root).
- Routing reviews can be focused on transport correctness without distractions from handler logic.

The full per-service composition pattern is documented in `service-bootstrap` (Phase 2). The per-transport routing details are in `wolverine-grpc-services`, `wolverine-kafka`, and `wolverine-azure-service-bus` (all Phase 3).

---

## Local Development Infrastructure

Each transport has a local-development story. All three are wired into the Aspire AppHost for one-command spin-up.

| Transport | Local infrastructure | Notes |
|---|---|---|
| gRPC | None — Wolverine handles it in-process | TLS not required locally; HTTP/2 over plaintext is fine for dev. |
| Kafka | Azure Event Hubs Emulator (Docker, with Azurite) | Producer and consumer APIs only; admin ops use management API on port 5300 or `az eventhubs`. |
| Azure Service Bus | Azure Service Bus Emulator (Docker, with SQL Server) | Management API support landed in early 2026; `az servicebus` works against the emulator's port 5300 with a special connection string. |

Connection strings, port mappings, and Aspire wiring details are documented in `aspire` (Phase 2) and `cli-azure-messaging` (Phase 3).

---

## Phasing

ADR-005 commits all three transports, but rollout is phased so the early development surface stays manageable.

| Transport | Lands when | Rationale |
|---|---|---|
| gRPC | First cross-service slice (Phase 3) | Foundational. Cab's reason for existing. |
| Kafka | When Telemetry is built (Phase 3+) | The most immediate Kafka use case; can be exercised independently. |
| Azure Service Bus | When Entra integration lands or first cross-service domain event flow needs reliable delivery (Phase 3+) | Tied to identity + business-event work. |

Phasing is an *implementation schedule*, not a deferral of the commitment. All three are committed transports. Reversing any of them would require a new ADR superseding ADR-005.

---

## Common Pitfalls

- **Defaulting to whichever transport you wrote a handler for last.** Identify the flow shape first; then look at the table.
- **Choosing Kafka because it's "the new thing in this project."** Kafka has two specific flows in CritterCab. Most cross-service messaging is ASB.
- **Choosing ASB because business events are the most common case.** Make sure the flow IS a business event. If it's a request-response or a streaming interaction with a specific consumer, it's gRPC.
- **Treating gRPC as a synchronous fallback for "I need this to feel fast."** gRPC has its own latency profile but isn't a magic-fast option. The right reason to choose gRPC is the flow shape (caller-callee), not perceived speed.
- **Putting transport choice into handler code.** The handler is transport-agnostic. The choice lives in the routing configuration.
- **Mixing high-volume signals and business events in one topic.** Split. Kafka for the signal, ASB for the event.

---

## See also

**Upstream** — load these first if unfamiliar:

- `protobuf-contracts` — required reading before designing a gRPC flow.
- `domain-event-conventions` — slim domain events vs. rich integration events; informs the gRPC-vs-ASB distinction at the design level.

**Downstream** — natural follow-ups by transport once selection is made:

- `wolverine-grpc-services` — handler patterns for unary and server-streaming RPCs (Phase 3).
- `wolverine-grpc-client-streaming` — handler patterns for client-streaming and bidirectional RPCs (Phase 4).
- `wolverine-kafka` — Wolverine's Kafka transport against Azure Event Hubs and the EH Emulator (Phase 3).
- `wolverine-azure-service-bus` — Wolverine's ASB transport against ASB and the ASB Emulator (Phase 3).
- `grpc-vs-other-transports` — finer-grained decision aid for ambiguous gRPC-vs-other cases (Phase 4).
- `service-bootstrap` — where routing configuration lives in each service (Phase 2).
- `cli-azure-messaging` — `az servicebus`, `az eventhubs`, and emulator operational details (Phase 3).
- `aspire` — local infrastructure wiring via the AppHost (Phase 2).

**External:**

- ADR-005 in [`docs/decisions/`](../../decisions/) — transport selection by flow type.
- ADR-007 in [`docs/decisions/`](../../decisions/) — Azure as deployment target (drives the EH and ASB choices).
- [`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md) § Transport Selection — the immutable rules.
- ai-skills `wolverine-integrations-kafka` — generic Wolverine Kafka mechanics. Install via `npx skills add` (license required).
- ai-skills `wolverine-integrations-azure-service-bus` — generic Wolverine ASB mechanics. Install via `npx skills add` (license required).
