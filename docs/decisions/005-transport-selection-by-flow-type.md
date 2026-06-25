# ADR-005: Transport Selection by Flow Type

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab is a distributed system (ADR-002) whose services communicate exclusively over the wire. The choice of transport is not incidental: different flows have different shapes — volume, latency sensitivity, ordering requirements, failure modes — and no single transport handles all of them well.

The ride-sharing domain surfaces three distinct flow shapes:

**High-volume, append-only telemetry.** GPS pings from active drivers arrive continuously and at scale. The flow is unidirectional, append-only, and tolerates brief consumer lag. It feeds downstream consumers (Dispatch, surge pricing signals) as a stream, not as individual addressed messages.

**Service-to-service calls and streaming interactions.** Dispatch offers fanned out to driver candidates, ride requests from riders, real-time driver-rider communication during a trip, and the GPS ingest path from the mobile client all require a request-response or streaming model with per-call semantics and low latency. These flows have a clear caller and callee; they are not broadcast.

**Business events crossing service boundaries.** Domain events — rider registered, driver approved, trip completed — need reliable delivery, topic-based routing, and operational features like dead-letter queues and session ordering. These flows are not latency-sensitive but they are correctness-sensitive; lost or out-of-order business events have domain consequences.

The question is how many transports to commit to and which shapes each one handles.

## Options Considered

### Option A — Single transport for all flows

One transport handles all communication. Kafka is the strongest candidate for a single-transport approach: it can absorb high-volume telemetry, carry business events as topics, and serve as a command bus.

A single transport eliminates the operational burden of running and reasoning about multiple brokers. It is a defensible choice when the flows are similar enough that one transport's strengths cover the others' gaps.

For CritterCab, the fit breaks down in two places. Kafka's partition-and-offset model is not a natural fit for service-to-service request-response or streaming interactions (GPS ingest, offer fan-out, real-time driver-rider communication). Implementing those flows over Kafka requires wrapping a point-to-point protocol on top of a log, which produces more complexity than running gRPC for the flows it fits. The single-transport simplicity is illusory once the workarounds accumulate.

### Option B — Two transports: gRPC and Kafka

gRPC handles service-to-service calls and streaming. Kafka handles high-volume telemetry and, by extension, business events.

This is a reasonable pairing. gRPC covers the latency-sensitive interactive flows cleanly. Kafka covers telemetry. The gap is in the business-event path: Kafka can carry domain events, but it does not natively provide dead-letter queues, session-ordered delivery, or scheduled message delivery. Implementing those features on top of Kafka is possible but puts accidental complexity onto the project.

The stronger objection is that the project's Azure alignment (ADR-007) makes Azure Service Bus a natural fit for the business-event backbone. Choosing Kafka for business events to avoid a third transport would mean doing more work to achieve a worse fit, while paying a coupling cost to Kafka for flows that are not high-volume.

### Option C — Three transports, each matched to its flow shape

gRPC (via Wolverine 5.32) for service-to-service calls and streaming. Kafka (via Wolverine's Kafka transport) for high-volume telemetry. Azure Service Bus for the business-event backbone.

Each transport earns its place by fitting the flow shape it handles:

- gRPC's four modes (unary, server-streaming, client-streaming, bidirectional) map directly onto the interactive flows in the ride-sharing domain: ride requests, offer fan-out, GPS ingest, and real-time trip communication.
- Kafka's log model is the right fit for GPS telemetry and surge-pricing signal ingestion — high volume, append-only, consumed by multiple downstream services as a stream.
- ASB's topic subscriptions, dead-letter queues, and session-ordered delivery are the right fit for business events that require reliable cross-service delivery with operational visibility.

The cost is operational: three brokers to run, configure, and reason about. The rollout is phased to keep the early development surface manageable.

## Decision

**Option C.** CritterCab uses three transports. Transport is chosen per flow shape, not defaulted.

**gRPC** (via Wolverine 5.32) for all service-to-service calls and streaming surfaces:
- Unary for commands and queries between services
- Server-streaming for offer delivery to driver candidates and live operations dashboards
- Client-streaming for GPS ingest from mobile clients into Telemetry
- Bidirectional where interactive flows justify it

**Kafka** (via Wolverine's Kafka transport) for high-volume, append-only streams:
- GPS pings and breadcrumb trails from Telemetry
- Stream-processing inputs for surge pricing signals

**Azure Service Bus** for the business-event backbone:
- Cross-service domain events (rider registered, driver approved, trip completed) where dead-lettering, session ordering, and topic-based routing matter
- Entra External ID user-lifecycle events arriving via Microsoft Graph change notifications

The rollout is phased. Kafka is introduced first, when Telemetry is built — it is the most immediate use case and can be exercised independently. ASB is introduced when the business-event backbone is needed, typically when Entra integration lands or when the first cross-service domain event flow requires it. gRPC is present from the first cross-service slice.

Phasing is an implementation schedule, not a deferral of the ASB commitment. Azure Service Bus is a committed transport. If that decision were ever reversed, a new ADR would supersede this one.

## Consequences

Each transport requires its own local development infrastructure. Kafka needs a local broker (Docker Compose). ASB needs the Azure Service Bus emulator. gRPC requires no additional infrastructure.

Wolverine's multi-transport support means transport selection is expressed in routing configuration, not in handler code. Handlers do not know or care which transport delivered a message; the bus configuration determines the path. This keeps the transport choice localized to configuration and makes it possible to shift a message type between transports without touching handlers.

The three-transport commitment narrows the scope of flow-type decisions for future bounded contexts: when a new flow is identified, the question is which of the three existing transports fits it, not whether a fourth transport is needed.

RabbitMQ is not used **for domain flows**. It is not that it is inappropriate for the domain; the existing three transports cover the required shapes, and adding a fourth would bring cost without new capability.

*(Amended 2026-06-25 — [ADR-017](./017-rabbitmq-for-critterwatch.md).)* This domain-transport conclusion is unchanged, but it is not a blanket ban on the broker: CritterCab provisions RabbitMQ as the telemetry/control backplane for the **CritterWatch** monitoring console, which depends on it. That is tooling infrastructure, not a domain flow — the same kind of out-of-scope category that [ADR-016](./016-frontend-live-update-transport.md) admitted for browser-client push (SignalR). No domain event, command, query, or stream is routed over RabbitMQ.
