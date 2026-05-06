---
name: grpc-vs-other-transports
description: "Finer-grained decision aid for ambiguous gRPC-vs-other-transport cases in CritterCab. Covers four subtle axes that transport-selection doesn't drill into: gRPC streaming vs Kafka (per-call lifecycle vs persisted log; when they compose rather than compete — client-streaming for ingest, Kafka for distribution), gRPC unary vs ASB request-reply (synchronous low-latency vs broker-mediated retry semantics), gRPC vs HTTP for browser-facing surfaces (Cab's HTTP-for-browser, gRPC-for-everything-else posture and why gRPC-Web isn't committed), and gRPC services vs Wolverine's gRPC transport (ListenAtGrpcPort / ToGrpcEndpoint as a peer-to-peer envelope ferry — why Cab doesn't use it under ADR-005's three-transport ceiling). Use after transport-selection has narrowed the choice to gRPC and a sibling, or when a flow looks like it could fit multiple transports plausibly."
cluster: grpc
tags: [grpc, transport-selection, decision-framework, kafka, azure-service-bus, http, grpc-web, request-reply, listen-at-grpc-port, streaming-composition, browser-facing, adr-005]
---

# gRPC vs Other Transports

This skill is the finer-grained companion to `transport-selection`. The framework in `transport-selection` answers the first-order question — flow shape determines transport, and most flows fit cleanly into one of three buckets. This skill drills into the cases where the framework's answer feels right but a sibling transport is plausibly competing for the same flow, or where the flow would naturally span more than one transport.

The bottom line: **when gRPC and a sibling transport both feel plausible, the right answer is usually that they compose, not that one of them wins.** Client-streaming and Kafka aren't alternatives for ingesting GPS pings — client-streaming is the mobile-to-service hop, Kafka is the service-to-service distribution. gRPC server-streaming and ASB topic fan-out aren't alternatives for trip-completion notifications — server-streaming pushes to the rider client, ASB fans to other services. Most "gRPC vs X" debates dissolve once the design separates the hops the flow actually has.

This skill assumes `transport-selection`'s framework is settled. It does NOT relitigate the three-transport ceiling, the flow-shape table, or the anti-patterns section — those are the foundation. Read `transport-selection` first if any of those feel unsettled; this skill goes one level deeper on top of that foundation.

---

## When to apply this skill

Use this skill when:

- A flow has surfaced in design where gRPC and a sibling transport (Kafka or ASB) both feel plausible.
- A flow looks like it might span multiple transports — particularly when an external client is the entry point and downstream consumers are services.
- A browser-facing surface is being added and the choice between gRPC, gRPC-Web, and HTTP is on the table.
- Someone has proposed reaching for `ListenAtGrpcPort` / `ToGrpcEndpoint` as a way to use gRPC as a Wolverine messaging substrate.
- A PR review has the comment "couldn't this just be ASB request-reply?" or "couldn't this just be a Kafka topic?" and the author is searching for the principled response.

Do NOT use this skill for:

- The first-order transport choice for a clearly-shaped flow — `transport-selection`. This skill assumes that framework has narrowed the choice to gRPC and a sibling.
- Choosing between gRPC streaming modes (unary, server-streaming, client-streaming, bidirectional) — `transport-selection` § Decision Framework table is canonical for that.
- Implementation patterns for gRPC handlers — `wolverine-grpc-handlers` for unary and server-streaming, `wolverine-grpc-bidirectional-handlers` for client-streaming and bidirectional.
- Implementation patterns for Kafka or ASB transports — `wolverine-kafka` and `wolverine-azure-service-bus`.
- ADR-005 itself — the three-transport ceiling lives in the ADR; this skill operates inside that ceiling.

---

## Axis 1: gRPC streaming vs Kafka

These look like alternatives because both are streams of items. They behave nothing alike.

| Property | gRPC streaming | Kafka |
|---|---|---|
| Lifecycle | Per-call: open, exchange, close | Persistent: log lives across consumers |
| Caller addressing | Specific caller / specific callee | One producer, many independent consumers |
| Consumer state | Stateless (the call IS the state) | Per-consumer offset tracked in the broker |
| Replay | None — call ends, items are gone | Yes — rewind to any retained offset |
| Ordering guarantee | In-order within the single call | In-order within a partition |
| Failure recovery | Reconnect from scratch, items in flight are lost | Resume from last committed offset, no items lost |
| Throughput ceiling | Per-connection HTTP/2 limits | Partitioned, scales horizontally |

The decision rule that follows: **per-call lifecycle → gRPC; persistent log with multiple independent consumers → Kafka.**

### When they compose, not compete

The trap is treating "the flow has a stream of items" as the whole question. Most streaming flows in Cab have an ingest hop AND a distribution hop, and the answer is one transport per hop:

```
Driver mobile client                 Telemetry service                   Dispatch
    │                                       │                                ▲
    │  client-streaming                     │  Kafka topic                   │
    │  (PushTelemetry)                      │  telemetry.location-pings      │
    └──────────► gRPC ◄────────────────────┐│                                │
                  │                         ││                                │
                  │ accumulate / batch       │└──────► Kafka ─────────────────┘
                  └────────────────────────►│                                │
                                            │                            Pricing
                                            │                                ▲
                                            └──────► Kafka ───────────────────┘
```

The mobile client's ingest hop is gRPC client-streaming because the caller-callee semantics are tight: one driver's phone, one Telemetry instance, per-call lifecycle bounded by trip duration. The downstream distribution hop is Kafka because Dispatch and Pricing want independent consumption of the same log with replay capability, and the volume justifies the partitioned model.

`transport-selection` § "I have a high-volume event that's also a business event" gestures at this composition pattern in passing. The principle generalizes: **whenever an external client streams items into a Cab service, the ingest transport and the inter-service transport are different transports for different reasons.** Don't try to make one transport carry both hops.

### When gRPC streaming alone is enough

When the consumers ARE the original callers, no inter-service distribution exists, and so no Kafka is needed:

- `WatchTripStatus` (server-streaming): the rider client opens a stream and consumes it. Other services don't subscribe to "this one rider's trip status updates" — they subscribe to `TripCompleted` on ASB when the trip ends. Server-streaming wins outright; Kafka would be ceremony.
- `StreamDriverOffers` (server-streaming): the driver's phone opens a stream; the offers exist for that driver and that driver only. No fan-out.

The litmus test: **does any service other than the immediate caller need to consume these items?** If yes, the inter-service hop is Kafka or ASB. If no, gRPC streaming is the entire story.

### Edge case: moderate volume, multiple consumers, per-call lifecycle

Sometimes a flow looks like it wants persistent-log semantics (multiple consumers, replay) AND per-call lifecycle (specific caller waits for the stream). This is usually two flows wearing one name:

- A persistent-log "stream of facts" that consumers replay → Kafka.
- A per-caller "subscribe to recent and live updates" surface → gRPC server-streaming, often implemented by tailing the Kafka topic on the server side and forwarding into the call.

Cab does not currently have this shape committed. If it surfaces (e.g., an Operations dashboard that wants both replay history and a live tail), the design is a Kafka topic for the persistent log plus a gRPC server-streaming RPC that wraps a tailing consumer. Don't try to make Kafka the user-facing API; consumers don't have correlation ids, retry semantics, or per-caller filtering, and exposing offset management to external clients is a deep mistake.

---

## Axis 2: gRPC unary vs ASB request-reply

ASB supports request-reply: a sender publishes to a queue with a reply-to address, and the consumer publishes a response to that address. Wolverine implements this transparently via `bus.InvokeAsync<TResponse>` working over any transport. So why isn't every cross-service call ASB request-reply?

| Property | gRPC unary | ASB request-reply |
|---|---|---|
| Latency | Few ms typical (no broker) | Tens of ms minimum (broker round-trip × 2) |
| Failure mode | Fail fast: caller sees `Unavailable` immediately | Retry: broker keeps message, redelivers if consumer crashes |
| Discoverability | Typed clients via proto, IntelliSense everywhere | Loose-typed envelopes, type matching at runtime |
| Backpressure | HTTP/2 flow control per call | Queue depth, broker-level throttling |
| Operational visibility | Distributed traces (per `observability-tracing`) | Same traces + queue depth, DLQ inspection |
| Schema governance | `.proto` + `buf breaking` per ADR-009 | None at the transport layer |

The decision rule: **synchronous low-latency reads or commands with a fast failure mode → gRPC unary. Directed commands where retry semantics, DLQ, or scheduled delivery have value → ASB queue.**

### gRPC unary territory in Cab

- `RequestRide` (Trips → Dispatch): the rider's mobile client expects a near-immediate response. If Dispatch is unhealthy, fail fast and let the client retry visibly.
- `RequestQuote` (Trips → Pricing): Trips needs the quote before responding to the rider. Latency and discoverability both matter.
- `GetLatestPosition` (Operations → Telemetry): a query, by definition synchronous.
- `CompleteTrip` (Trips → itself, but the client sees it as a synchronous command): the rider waits for the receipt.

### ASB queue territory in Cab

- `ProcessPayment` (Trips → Payments): a directed command. Trips doesn't need an immediate response — Payments will publish `PaymentCaptured` on success or `PaymentFailed` on failure. The broker holds the message if Payments is restarting; retries happen automatically; the DLQ catches genuine failures for operations.
- `IssueRefund` (Operations → Payments): same shape — directed, retryable, DLQ-able.
- Anything that benefits from `ScheduledEnqueueTime` (e.g., "remind me in 30 seconds if this offer hasn't been accepted") — gRPC has no concept of broker-scheduled delivery.

### The shape that confuses people

Sometimes a flow LOOKS synchronous because the caller writes `await bus.InvokeAsync<TResponse>(...)` and waits for the response. That's request-reply, and it works over both gRPC and ASB. The choice between transports doesn't depend on whether the caller awaits — it depends on what the caller would want to happen if the callee is unavailable.

- If the answer is "fail fast and let the user see the error" → gRPC unary.
- If the answer is "queue it; retry; alert if it lands in the DLQ" → ASB queue.

Most Cab flows that feel synchronous to the user (rider waits for a response) are gRPC unary because the user CAN see the error and it's the right surface to expose it on. Most Cab flows that feel synchronous to the developer (one method awaits another) but where the user wouldn't notice a broker round-trip are ASB queues because the operational benefits — retry, DLQ, replay — outweigh the latency cost.

### Anti-pattern: ASB request-reply for synchronous reads

An ASB request-reply for "what's the current state of X?" is the wrong shape. The broker hop adds latency for no benefit; reads don't benefit from retry semantics (the caller will just retry the read); DLQ has no use case for queries. The right transport for synchronous reads is gRPC unary.

This is the inverse of `transport-selection` § "Don't use gRPC for fire-and-forget event publishing." The two anti-patterns face the same direction: pick the transport whose semantics you actually want, not the one that's already wired up.

---

## Axis 3: gRPC vs HTTP for browser-facing services

Browser clients can't speak vanilla gRPC. The wire protocol relies on HTTP/2 trailing headers, which browser fetch APIs don't expose, and on framing the browser can't generate. **gRPC-Web** is the browser-compatible variant — a different wire protocol, supported by Envoy or by ASP.NET Core's built-in `Grpc.AspNetCore.Web` middleware, with substantive limitations: client-streaming and bidirectional methods aren't supported in the browser; only unary and server-streaming work. (True bidirectional in the browser is gated on `duplex: 'full'` in the Fetch API or on WebTransport, neither of which is universally shipped in stable browsers as of 2026.)

Cab's posture, consistent with the existing `wolverine-http-handlers` skill: **HTTP/JSON for browser-facing surfaces, gRPC for native mobile and service-to-service.** The handler bodies are the same; the transport boundary is different. A `RequestRide` Wolverine handler dispatches the same way regardless of whether the request arrived as HTTP/JSON from a web client or as gRPC from the mobile app.

The reasons:

- **Discoverability for browser developers.** OpenAPI/Swagger plus typed TypeScript clients work cleanly with HTTP/JSON. gRPC-Web requires a separate code-gen toolchain and a proxy.
- **Streaming asymmetry.** Cab's streaming surfaces (`WatchTripStatus`, `StreamDriverOffers`) target the native mobile client where gRPC server-streaming is fully supported. The browser dashboard surface (Operations live map) uses Server-Sent Events over HTTP, which the existing HTTP handler shape supports without a separate transport.
- **Proxy footprint.** Adding gRPC-Web means adding Envoy or a Connect proxy or another sidecar. ADR-005's three-transport ceiling is about messaging, not about proxies, but the same "don't add infrastructure without commensurate value" logic applies.
- **Authentication symmetry.** Cab's identity-ACL flow (per `identity-acl`) uses bearer tokens that work identically on both HTTP and gRPC. There's no auth advantage to gRPC-Web for the browser.

### When to revisit

If a future Cab feature needs bidirectional streaming from a browser (e.g., a real-time collaborative dispatcher console), the choice would be WebSocket via the existing HTTP handler shape, NOT gRPC-Web. WebSocket support in browsers is universal; gRPC-Web bidirectional support requires WebTransport, which has uneven browser availability.

The "gRPC-Web for everything" alternative would unify the contract surface (one proto file for both browser and mobile) at the cost of a proxy and a more complex tooling story. Cab does not currently commit to that path. If the schema-unification benefit becomes compelling, it would be a new ADR — at minimum a successor to ADR-009 expanding the protobuf-as-unified-schema-language note in `protobuf-contracts`.

### Anti-pattern: same service surface in both HTTP and gRPC for inter-service calls

When a Cab service exposes BOTH HTTP and gRPC endpoints, the HTTP surface is for browser clients and the gRPC surface is for native mobile and inter-service callers. A Cab service calling another Cab service should always use gRPC, never HTTP — even though both work and the handler is the same. Reasons: discoverability via proto, exception → status mapping symmetry (per AIP-193 from `wolverine-grpc-handlers`), and operational visibility through gRPC-aware tooling.

---

## Axis 4: gRPC services vs gRPC transport

There are two completely different things in Wolverine that both have "gRPC" in the name. Conflating them is the most common confusion in this whole space.

**gRPC services** are what `wolverine-grpc-handlers` and `wolverine-grpc-bidirectional-handlers` cover. A service developer authors a `.proto` file per `protobuf-contracts`. Wolverine generates a wrapper that maps each RPC to `bus.InvokeAsync<T>` or `bus.StreamAsync<T>`. Callers — mobile clients, browser clients via HTTP, other Cab services — speak the protocol described by the `.proto`.

**gRPC transport** is `opts.ListenAtGrpcPort(port)` and `publishing.ToGrpcEndpoint(host, port)` — a Wolverine messaging transport that ferries Wolverine envelopes between two Wolverine peers over gRPC as the wire protocol. There is no `.proto` file. Messages are serialized however Wolverine serializes (default JSON), envelope metadata travels in headers, and the receiving Wolverine handler is matched by message type just as it would be for any other transport. The transport supports `EndpointMode.Inline` and `EndpointMode.BufferedInMemory` only — no durable outbox.

The two surfaces are unrelated. A service can use gRPC services (proto-described user-facing RPCs) without ever touching the gRPC transport. The gRPC transport can in principle be used without any proto-described services.

### Why Cab doesn't use the gRPC transport

ADR-005 commits Cab to three messaging transports: gRPC services for cross-service synchronous calls, Kafka for high-volume append-only streams, ASB for cross-service domain events. The gRPC transport is a fourth — a peer-to-peer Wolverine envelope ferry. Adding it would violate the three-transport ceiling without earning its keep:

- **No new flow shape.** The flow shapes the gRPC transport handles — directed commands and pub/sub between Wolverine peers — are already covered by ASB queues and topics. Reaching for the gRPC transport instead means swapping ASB's reliability, DLQ, and operational visibility for "no broker hop." That tradeoff doesn't fit any flow Cab has identified.
- **Schema invisibility.** Wolverine envelopes over gRPC have no proto contract. Cab's contract governance (ADR-009, `protobuf-contracts`) doesn't apply — there's no buf-breaking gate, no shared types directory, no breaking-change classification in PRs.
- **Operational simplicity loss.** Three transports already require operators to know three sets of CLI tools (`cli-grpc-tooling`, `cli-kafka-tooling`, `cli-azure-messaging`). A fourth transport is a fourth toolchain.
- **No durable outbox.** The gRPC transport's `EndpointMode` is Inline or BufferedInMemory only. Wolverine's durable outbox guarantees — which Cab relies on for cross-service domain events on ASB — are not available.

### When the gRPC transport's value proposition would matter

The gRPC transport's clearest value is "broker-less Wolverine peer-to-peer messaging." A project that runs a small cluster of Wolverine services and wants to skip the broker complexity can wire `ListenAtGrpcPort` and `ToGrpcEndpoint` and call it a day. That's a legitimate architecture for the right project — but it isn't Cab. Cab is deliberately a distributed-services reference architecture exercising all three of ADR-005's transports; "skip the broker" is the opposite of the project's purpose.

If you find yourself reaching for `ListenAtGrpcPort` while working in Cab, the right answer is almost always one of: a Wolverine messaging handler over ASB, a Wolverine messaging handler over Kafka, or a Wolverine gRPC service handler with a proto contract. The fourth option doesn't exist.

---

## Common pitfalls

- **Treating "stream of items" as the whole question.** gRPC streaming and Kafka both stream items; the answer to "which one?" depends on lifecycle, multiplicity, and replay semantics, not on the word "stream." Walk Axis 1's table when uncertain.

- **Trying to make one transport carry both ingest and distribution.** When an external client sends data and multiple Cab services consume it, the answer is two transports — one for the ingest hop, one for the distribution hop. Trying to make Kafka the mobile-to-service ingest path or trying to make gRPC streaming the inter-service distribution path violates each transport's design assumptions.

- **Choosing ASB request-reply because the caller awaits.** The await syntax doesn't determine the transport; the desired failure semantics do. Synchronous user-facing reads → gRPC unary; directed commands with retry/DLQ value → ASB queue.

- **Adding gRPC-Web for the browser surface.** The HTTP handler shape covers the browser case with the same handler bodies as gRPC. gRPC-Web adds a proxy and a separate tooling story for benefits Cab doesn't currently need. If schema unification across browser and mobile becomes compelling, it's a new ADR conversation.

- **Calling another Cab service over HTTP because "the endpoint is there."** Inter-service calls go over gRPC, even when the callee also exposes HTTP for browser clients. The HTTP surface is for browser clients; the gRPC surface is for native mobile and other Cab services.

- **Reaching for `ListenAtGrpcPort` to "skip the broker."** ADR-005's three-transport ceiling is structural, not advisory. The gRPC transport is a fourth transport whose value proposition (broker-less peer-to-peer) is the opposite of Cab's purpose as a distributed-services reference architecture.

- **Confusing the gRPC transport with the gRPC service surface.** They share a name and nothing else. The transport ferries Wolverine envelopes peer-to-peer; the service surface exposes proto-described RPCs to external callers. A service using `[WolverineGrpcService]` stubs is using the service surface, not the transport.

- **Putting the user-facing surface ON Kafka.** Kafka is an inter-service distribution transport. Exposing offset management, partition keys, and consumer-group semantics to mobile or browser clients is a deep mistake — caller addressing, retry, and per-caller filtering aren't Kafka concerns. The user-facing surface is gRPC (or HTTP for browsers); Kafka sits between services.

- **Using gRPC unary when the work is genuinely fire-and-forget.** The caller waits for the response even though it has nothing to do with it. Trips publishing `TripCompleted` to Payments via gRPC unary is the wrong shape — Payments doesn't need to respond synchronously, the broker should mediate, retries should happen automatically. ASB topic, not gRPC unary. (`transport-selection`'s anti-patterns section names this; this skill reinforces it.)

- **Picking gRPC because "it's faster."** gRPC has a latency profile but isn't a magic-fast option. The right reason to pick gRPC is the flow shape (caller-callee semantics, schema-described contract, per-call lifecycle), not perceived speed. ASB queues across the same machine have very low latency; gRPC across the internet does not.

- **Forgetting that transport choice can be revisited per major version.** A flow that started as gRPC and grew into needing replay (a sign Kafka may be the better fit now) doesn't have to stay gRPC forever. Migrating a flow between transports is a routing-configuration change in `Program.cs` plus possibly a contract version bump — not a rewrite of handler code. `transport-selection` § "Transport in Routing, Not in Handlers" makes this concrete.

---

## See also

**Upstream** — load these first:

- `transport-selection` — the first-order decision framework. This skill assumes that framework is settled and goes one level deeper for ambiguous cases.
- `protobuf-contracts` — the contract conventions that make gRPC's discoverability advantage real. Without `.proto` governance, gRPC's schema-described surface is a claim, not a property.
- `domain-event-conventions` — informs the gRPC unary vs ASB queue distinction at the design level (slim domain events, rich integration events).

**Sibling skills:**

- `wolverine-grpc-handlers` — the handler patterns for unary and server-streaming RPCs once the transport is chosen.
- `wolverine-grpc-bidirectional-handlers` — the handler patterns for client-streaming and bidirectional RPCs.
- `wolverine-kafka` — Kafka transport wiring once Axis 1 routes a flow there.
- `wolverine-azure-service-bus` — ASB transport wiring once Axis 2 routes a flow there.
- `wolverine-http-handlers` — HTTP endpoint handlers; the browser-facing surface that pairs with gRPC under Axis 3.

**Downstream:**

- `service-bootstrap` — where transport routing configuration lives in each service.
- `cli-grpc-tooling` — buf, grpcurl, Evans against gRPC services and the proto governance gate.
- `cli-kafka-tooling`, `cli-azure-messaging` — the operational surfaces that ASB and Kafka bring with them, part of the cost analysis when comparing to gRPC.
- `observability-tracing` — distributed traces span all three transports and let post-hoc analysis confirm the chosen transport behaves as expected.

**External:**

- ADR-005 in [`docs/decisions/`](../../decisions/) — the three-transport ceiling that bounds this skill's recommendations.
- ADR-009 in [`docs/decisions/`](../../decisions/) — proto-first contract governance that supports gRPC's discoverability advantage.
- [`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md) § Transport Selection — the immutable rules.
- [gRPC-Web specification](https://github.com/grpc/grpc/blob/master/doc/PROTOCOL-WEB.md) — the browser-compatible variant Cab does not currently commit to.
- [Wolverine gRPC documentation](https://wolverinefx.net/guide/grpc/) — covers both the gRPC service surface (`[WolverineGrpcService]`) and, in the messaging-transports section, the `ListenAtGrpcPort` / `ToGrpcEndpoint` peer-to-peer transport that Cab does not use under ADR-005.
- ai-skills `wolverine-integrations-grpc` — generic Wolverine gRPC patterns if/when JasperFx publishes one.
