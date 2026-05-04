---
name: wolverine-messaging-handlers
description: "Message-bus handler patterns in CritterCab — routing rules required for OutgoingMessages, transactional outbox semantics, InvokeAsync vs PublishAsync vs ScheduleAsync. Use when authoring or reviewing handlers triggered by messages, or any handler that publishes integration events."
cluster: wolverine
tags: [wolverine, handlers, messaging, integration-events, outbox, routing]
---

# Wolverine Messaging Handlers

Message-bus handler patterns in CritterCab. Covers handlers triggered by messages flowing through Wolverine's bus, and — critically — any handler (HTTP, gRPC, message) that publishes integration events for downstream consumers.

This skill assumes you've loaded `wolverine-handlers` first. The general handler shape, validation pipeline, and `IStartStream` mechanics live there. This skill picks up at messaging-specific decisions: how messages get routed, why `OutgoingMessages` and not `bus.PublishAsync`, when to use which `bus.*` method.

## When to apply this skill

Use this skill when:

- Authoring a handler triggered by an inbound message (cross-service event subscription, intra-service queue).
- Adding a new integration-event publication to **any** handler — including HTTP and gRPC handlers. The routing-rule requirement applies to publications regardless of how the handler was triggered.
- Choosing between `bus.InvokeAsync`, `bus.PublishAsync`, `bus.ScheduleAsync`, and the cascading-return-value pattern.
- Diagnosing "the handler ran but the downstream consumer didn't receive the message."

Do NOT use this skill for:

- General handler shape — see `wolverine-handlers` (load first).
- HTTP-specific patterns — see `wolverine-http-handlers`.
- Saga state machines and durable timeouts — see `wolverine-sagas` (Phase 4).
- Per-transport routing-rule syntax (Kafka, ASB, gRPC) — see `wolverine-kafka`, `wolverine-azure-service-bus`, `wolverine-grpc-handlers` (Phase 3).
- Choosing *which* transport to use for a flow — see `transport-selection`.

---

## The Routing Rule Pre-Flight

The single most consequential CritterCab handler footgun. The handler appears to work — no error, no warning — but the integration message never reaches its consumers, and `tracked.Sent.MessagesOf<T>()` always returns 0 in tests.

```csharp
// Handler emits the message — looks correct
public static (OfferAccepted, OutgoingMessages) Handle(...)
{
    var outgoing = new OutgoingMessages();
    outgoing.Add(new Integration.OfferAccepted(...));   // never reaches the bus
    return (new OfferAccepted(...), outgoing);
}

// Test assertion fails
tracked.Sent.MessagesOf<Integration.OfferAccepted>().ShouldHaveSingleItem();  // count: 0
```

**Root cause.** Wolverine's `PublishAsync` calls `Runtime.RoutingFor(type).RouteForPublish(...)`. With no routing rule configured for the message type, this returns an empty route array; `PublishAsync` records `MessageEventType.NoRoutes` in the message log and returns silently. The message never reaches the outbox, never enrolls in the transaction, never sends.

**Resolution.** Every integration message type referenced by a handler must have a routing rule in the publishing service's `Program.cs`. There are no exceptions and no defaults.

```csharp
// Program.cs — Dispatch service composition root
opts.PublishMessage<Integration.OfferAccepted>()
    .ToAzureServiceBusTopic("dispatch.offer-accepted");

opts.PublishMessage<Integration.OfferRejected>()
    .ToAzureServiceBusTopic("dispatch.offer-rejected");
```

The routing rule is paired with the message type at publication-site configuration. The transport choice (gRPC, Kafka, ASB) follows the decision in `transport-selection`; the per-transport routing API surface lives in `wolverine-azure-service-bus`, `wolverine-kafka`, and `wolverine-grpc-handlers` (all Phase 3).

### Pre-flight checklist for any PR adding an integration message type

1. **Define the message** in the service's `Integration/` namespace (per `domain-event-conventions`).
2. **Add the routing rule** in `Program.cs` for the publishing service.
3. **Verify** with `dotnet run -- wolverine-diagnostics describe-routing "<TypeName>"` that the rule is registered.
4. **Write the integration test** that asserts the message lands on the bus.

Skipping any step makes the failure silent. The `tracked.Sent` assertion is the canonical way to catch a missing routing rule before merge — see `testing-integration` (Phase 2).

This applies regardless of how the handler was triggered. An HTTP endpoint that publishes an integration event has the same routing-rule requirement as a message handler that does. The footgun lives here (in messaging) because the failure mode is in the message-routing layer.

---

## OutgoingMessages: Transactional Outbox

`OutgoingMessages` is Wolverine's transactional outbox primitive. Messages added to an `OutgoingMessages` collection returned from a handler are enrolled in the same transaction as the database write — they commit atomically with the event-stream append (or document save), and they're sent only after the transaction successfully commits.

```csharp
public static (OfferAccepted, OutgoingMessages) Handle(
    AcceptOffer cmd,
    [WriteAggregate(nameof(AcceptOffer.OfferId))] RideOffer offer,
    TimeProvider time)
{
    var accepted = new OfferAccepted(offer.Id, cmd.DriverId, time.GetUtcNow());

    var outgoing = new OutgoingMessages();
    outgoing.Add(new Integration.OfferAccepted(
        offer.Id, offer.RideRequestId, cmd.DriverId, time.GetUtcNow()));

    return (accepted, outgoing);
}
```

If the database transaction rolls back, the outgoing messages are discarded — they never reach the bus. This is the property that makes `OutgoingMessages` the correct primitive for cross-service integration events: downstream consumers never see an event that didn't actually happen.

---

## Anti-Pattern: bus.PublishAsync Inside a Handler

`bus.PublishAsync` sends *immediately*, outside the transactional middleware. If the handler's database transaction rolls back after the publish, the message has already left — downstream consumers see an event that didn't happen.

```csharp
// ❌ WRONG — published before the session commits; can fire on rollback
public static async Task Handle(SomeCmd cmd, IDocumentSession session, IMessageBus bus)
{
    session.Events.Append(...);
    await bus.PublishAsync(new Integration.SomethingHappened(...));   // outside the transaction
    await session.SaveChangesAsync();
}

// ✅ CORRECT — OutgoingMessages enrolled in the transactional outbox, commits atomically
public static (EventType, OutgoingMessages) Handle(SomeCmd cmd, [WriteAggregate] Aggregate agg)
{
    var outgoing = new OutgoingMessages();
    outgoing.Add(new Integration.SomethingHappened(...));
    return (new EventType(...), outgoing);
}
```

**Exception:** `bus.ScheduleAsync` is fine — delayed delivery cannot be expressed via `OutgoingMessages`. See § ScheduleAsync below.

---

## Anti-Pattern: bus.InvokeAsync for Fire-and-Forget Work

`InvokeAsync` runs the target handler **synchronously** and blocks the caller until completion. It's for request-reply and tightly-coupled local invocation, not "publish and move on."

```csharp
// ❌ WRONG — caller blocked on email send
public static async Task<IResult> Register(RegisterRider cmd, IMessageBus bus)
{
    // ... persist ...
    await bus.InvokeAsync(new SendWelcomeEmail(cmd.RiderId));   // synchronous wait
    return Results.Ok();
}

// ✅ CORRECT — cascading return value, queued via outbox, non-blocking
public static (IResult, RiderRegistered, OutgoingMessages) Register(RegisterRider cmd, ...)
{
    var outgoing = new OutgoingMessages();
    outgoing.Add(new SendWelcomeEmail(cmd.RiderId));
    return (Results.Ok(), new RiderRegistered(...), outgoing);
}
```

---

## bus.* Method Decision Matrix

| Caller intent | Use | Notes |
|---|---|---|
| Need the handler's return value (request-reply) | `bus.InvokeAsync<T>(msg)` | Synchronous; blocks until the handler completes. |
| Need to know the handler succeeded synchronously | `bus.InvokeAsync(msg)` (no generic) | Synchronous; no return value. |
| Publish and move on (handler emits the message) | Cascading return via `OutgoingMessages` | **Preferred.** Atomically committed with the database transaction. |
| Publish and move on (caller is not in a handler context) | `bus.PublishAsync(msg)` | Immediate send. Bypasses transactional outbox — only safe outside a transaction. |
| Schedule for later | `bus.ScheduleAsync(msg, delay)` | See § ScheduleAsync. |

The first three rows cover ~95% of CritterCab cases. `PublishAsync` is rarely the right answer inside a handler; it's the right answer in startup code, scheduled background work that doesn't coordinate with a database write, or test arrangement.

---

## ScheduleAsync for Delayed Delivery

`bus.ScheduleAsync` is the correct primitive for "send this message N seconds/minutes/hours from now." It's neither synchronous (like `InvokeAsync`) nor outbox-bypassing in the problematic way (`PublishAsync`'s issue) — Wolverine persists the scheduled envelope and delivers it when the time arrives.

```csharp
public static async Task<(OfferDispatched, OutgoingMessages)> Handle(
    DispatchOffer cmd,
    [WriteAggregate(nameof(DispatchOffer.OfferId))] RideOffer offer,
    IMessageBus bus,
    TimeProvider time,
    CancellationToken ct)
{
    var dispatched = new OfferDispatched(offer.Id, cmd.DriverId, time.GetUtcNow());

    // Schedule the timeout — fires in 15 seconds even if no other activity occurs
    await bus.ScheduleAsync(new ExpireOfferIfNotAccepted(offer.Id), TimeSpan.FromSeconds(15), ct);

    var outgoing = new OutgoingMessages();
    outgoing.Add(new Integration.OfferDispatched(...));

    return (dispatched, outgoing);
}
```

For sagas with structured timeouts and state transitions, see `wolverine-sagas` (Phase 4) — sagas have their own timeout primitives that compose with the saga state machine.

`ScheduleAsync` requires a routing rule for the message type, same as any published message.

---

## Inbound Message Handlers

A handler triggered by an inbound message (cross-service event subscription, intra-service queue) follows the same general shape as any Wolverine handler — see `wolverine-handlers` for the canonical form. Two things particular to inbound message handlers:

- **No HTTP response.** Return type is `(EventType, OutgoingMessages)`, `Events`, `OutgoingMessages`, or `void`. No `IResult`, no `CreationResponse`, no tuple-with-HTTP-result-first concern.
- **Idempotency matters.** Cross-service event subscriptions can deliver duplicates (at-least-once semantics on most transports). Handlers consuming integration events should be idempotent — typically by guarding on aggregate state or by using a deduplication key. See `wolverine-azure-service-bus` (Phase 3) and `wolverine-kafka` (Phase 3) for transport-specific delivery semantics.

```csharp
// Example: Trips service consumes Dispatch's OfferAccepted integration event
public static class OfferAcceptedConsumer
{
    public static async Task<(TripStarted, OutgoingMessages)> Handle(
        Integration.OfferAccepted message,
        IDocumentSession session,
        TimeProvider time,
        CancellationToken ct)
    {
        // Idempotency guard — if the trip stream already exists, skip
        var existing = await session.Events.AggregateStreamAsync<Trip>(message.RideRequestId, token: ct);
        if (existing is not null) return default;

        var started = new TripStarted(
            message.RideRequestId, message.DriverId, message.RiderId, message.PickupLocation, time.GetUtcNow());

        // Start the Trip stream; publish the trip-started integration event for further downstream
        var stream = MartenOps.StartStream<Trip>(message.RideRequestId, started);
        var outgoing = new OutgoingMessages();
        outgoing.Add(new Integration.TripStarted(message.RideRequestId, ...));

        return (started, outgoing);
    }
}
```

---

## Diagnosing Messaging Issues

```bash
# List all routing rules — first stop when an integration message isn't reaching consumers.
dotnet run -- wolverine-diagnostics describe-routing --all

# Routing for a specific type — use when tracked.Sent.MessagesOf<T>() returns 0.
dotnet run -- wolverine-diagnostics describe-routing "CritterCab.Dispatch.Integration.OfferAccepted"

# Preview the generated handler code — verify OutgoingMessages enrollment is wired correctly.
dotnet run -- wolverine-diagnostics codegen-preview --handler AcceptOffer
```

| Symptom | First check |
|---|---|
| `tracked.Sent.MessagesOf<T>()` returns 0 | `describe-routing "<TypeName>"` — missing routing rule. |
| Inbound handler not invoked | `describe-routing` for the inbound queue/topic; verify the handler is registered. |
| Message published outside transaction | Search the handler for `bus.PublishAsync` calls; replace with `OutgoingMessages` cascading return. |
| `InvokeAsync` blocking longer than expected | The target handler is doing real work — convert to `OutgoingMessages` if fire-and-forget is acceptable. |

For full CLI coverage, see `cli-jasperfx` (Phase 2).

---

## See also

**Upstream** — load these first:

- `wolverine-handlers` — general handler shape, validation pipeline, `IStartStream` semantics, logger convention.
- `transport-selection` — which transport routes which message type; pre-flight reading before authoring a routing rule.
- `domain-event-conventions` — `Integration/` namespace pattern this skill assumes.

**Sibling skills:**

- `wolverine-http-handlers` — HTTP-specific patterns; load when the handler is also an HTTP endpoint.
- `wolverine-grpc-handlers` (Phase 3) — gRPC unary and server-streaming.
- `wolverine-grpc-bidirectional-handlers` (Phase 4) — gRPC client-streaming and bidirectional.

**Downstream** — natural follow-ups:

- `wolverine-azure-service-bus` (Phase 3) — ASB-specific routing rules, delivery semantics, dead-lettering.
- `wolverine-kafka` (Phase 3) — Kafka-specific routing rules against Event Hubs (cloud) and the EH Emulator (local).
- `wolverine-sagas` (Phase 4) — saga state machines, durable timeouts, structured `ScheduleAsync` patterns.
- `marten-wolverine-aggregates` (Phase 2) — aggregate handler workflow, where `OutgoingMessages` commits with the event stream.
- `cli-jasperfx` (Phase 2) — full CLI surface for routing and message diagnostics.

**External:**

- ai-skills `wolverine-messaging-message-routing` — generic Wolverine routing-rule mechanics.
- ai-skills `wolverine-messaging-resiliency-policies` — retry, circuit breaker, dead-lettering policies.
- ai-skills `wolverine-handlers-fundamentals` — generic handler shape (covered in `wolverine-handlers` here, but ai-skills has the canonical reference).
- All ai-skills installed via `npx skills add` (license required).
- [Wolverine Messaging Guide](https://wolverinefx.net/guide/messaging/).
