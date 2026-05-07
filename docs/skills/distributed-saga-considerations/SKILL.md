---
name: distributed-saga-considerations
description: "Distributed-systems concerns that shape CritterCab saga design — choreography vs orchestration tradeoffs, compensating-action patterns, idempotence requirements (transport redelivery, retry policies, replay during recovery), distributed failure modes (saga crashes, mid-step failures, unbounded waits, wedged sagas, network partitions), and how Wolverine's transactional outbox supports the saga (atomic commit of state-change + outbound messages, what it does and doesn't protect, the producer-vs-consumer-side responsibility split). Cab's posture: orchestration as the default for cross-BC sagas (TripCompletionSaga across Trips → Pricing → Payments → Ratings), choreography acceptable for simple reactive flows where no central coordinator earns its keep. Pattern-heavy, code-light companion to wolverine-sagas — pairs with that skill the way dynamic-consistency-boundary pairs with marten-wolverine-aggregates. Use during saga design, compensating-action design, or PR review of a new saga's failure-mode story."
cluster: distributed-services
tags: [saga, distributed-systems, choreography, orchestration, compensating-actions, idempotence, outbox, failure-modes, retry, dlq, process-manager, cross-bc]
---

# Distributed Saga Considerations

Designing distributed sagas in CritterCab. The implementation surface — the `Saga` base class, the handler-method conventions, the persistence wiring — lives in `wolverine-sagas`. **This skill documents the distributed-systems concerns that shape *what* the saga should do**, not *how* to write one in Wolverine. It pairs with `wolverine-sagas` the way `dynamic-consistency-boundary` pairs with `marten-wolverine-aggregates`: the implementation skill tells you the mechanics, this skill tells you the design decisions.

The recurring theme across every section: **a saga is the explicit place where distributed-systems failure becomes a first-class design concern.** Single-BC handlers can mostly pretend the world is reliable — the database transaction either commits or doesn't, and the next message either arrives or eventually retries. Sagas can't pretend. Step 3 of 5 succeeded; the broker delivered the response twice; the destination service was offline for 90 seconds; the timeout fired before the response arrived. Each of these is normal in distributed operation, and the saga's design has to anticipate them.

This skill covers five concerns: choreography vs orchestration as the topology decision; compensating actions for backward recovery; idempotence as a foundational requirement; distributed failure modes Cab actively designs for; and how Wolverine's transactional outbox supports the saga without solving the whole problem. The last one matters because the outbox guarantee is real but narrowly scoped — it makes the producer side atomic; it doesn't make consumers idempotent.

This skill assumes `wolverine-sagas` for the saga base class and persistence, `wolverine-messaging-handlers` for the outbox semantics it relies on, and `transport-selection` for the transport choices that affect retry, DLQ, and scheduled-delivery semantics. The Cab sagas referenced — `DispatchOfferTimeoutSaga`, `TripCompletionSaga`, `RiderOnboardingSaga`, `TripArrivalSaga` — are introduced in `wolverine-sagas` § Cab use cases and detailed in `event-modeling`.

---

## When to apply this skill

Use this skill when:

- Designing a new saga and choosing between choreography and orchestration.
- Designing the compensating-action paths for an existing saga's failure modes.
- Walking through a saga's failure-mode story before merge — what happens if step 3 crashes, what happens if step 2's response arrives twice, what happens if the saga itself crashes mid-handler.
- Reviewing a PR that adds or modifies a saga and the failure semantics aren't obvious.
- Diagnosing a wedged saga in production and reasoning about which compensating action to invoke (or whether manual intervention is the right call).
- Onboarding a new engineer to Cab's distributed-systems posture.

Do NOT use this skill for:

- Implementing the saga itself — the `Saga` base class, handler-method conventions, persistence wiring, `MarkCompleted()` lifecycle, `IRevisioned` concurrency. All in `wolverine-sagas`.
- Atomic multi-stream commands — `dynamic-consistency-boundary` is the right shape when the entire decision is one transactional unit.
- Single-handler outbox patterns and routing — `wolverine-messaging-handlers`.
- Transport choice for saga messages — `transport-selection`, with the gRPC-specific axes in `grpc-vs-other-transports`.
- Cross-BC contract design — `protobuf-contracts` and `domain-event-conventions`.

---

## Choreography vs orchestration

The first decision in any cross-BC saga: who's in charge?

| Property | Choreography | Orchestration |
|---|---|---|
| Coordinator | None — each service reacts to events from others | Single saga commands the participants and waits for responses |
| Coupling | Each service couples to the events it subscribes to | Saga couples to all participants; participants couple only to the saga |
| Observability | Distributed across services; no single timeline | One saga state document is the timeline |
| Failure visibility | Errors are local to the failing service; the missing reaction may be silent | Saga sees the failure directly and decides what to do |
| Change cost | Adding a step means a new subscription somewhere | Adding a step means editing the saga |
| Cognitive load | High — the workflow exists only as a sum of subscriptions | Low — the workflow exists as a saga class |
| Cycle risk | Real — subscriptions can form unintended loops | None — the saga drives the order explicitly |

The decision rule: **default to orchestration for cross-BC workflows; allow choreography for simple, local, well-bounded reactive flows.** Cab's posture is orchestration-first because the project deliberately exercises explicit cross-BC coordination as part of the reference-architecture story, and orchestration's observability and failure-visibility properties are the harder ones to retrofit later.

### When choreography is acceptable

Choreography earns its place when the flow is genuinely small (two or three services), each step is naturally a domain event the source BC was going to publish anyway, and no central decision needs to span the whole workflow. The shape: BC A publishes `Thing happened`, BC B subscribes and reacts, BC C subscribes to BC B's reaction, and the workflow is "done" when each subscriber has done its local thing.

In Cab, the publish-only outbound notifications fit choreography: when `TripCompleted` is published, Notifications subscribes to send the receipt email and Operations subscribes to update the live map. There's no orchestrator here because there's no decision to make — each subscriber's behavior is independent.

### When orchestration is required

Orchestration is required whenever any of these are true:

- A later step's behavior depends on the cumulative outcome of earlier steps (not just the most recent one).
- Compensating actions are needed when a step fails — somebody has to know what to undo and where.
- A timeout on the overall workflow (not just per-step) needs to fire centrally.
- The workflow needs a single observable state document for operations and support.
- The flow involves more than three or four BCs, where choreography's cognitive cost compounds quickly.

Cab's `TripCompletionSaga` (Trips → Pricing → Payments → Ratings) hits all five conditions: payment-failure compensation needs to refund the rider AND notify the driver; an unresolved rating after 24 hours times the saga out; the saga state IS the post-trip workflow's status. Orchestration is the only fit.

### The "process manager" terminology note

Some literature uses "process manager" for what Wolverine calls a saga, reserving "saga" for the original Garcia-Molina/Salem 1987 long-running-transaction concept. The Wolverine project itself acknowledges this distinction in passing but uses "saga" inclusively. **Cab follows Wolverine's terminology** — when this skill says "saga," it means a long-running, stateful coordination implemented as a `Wolverine.Saga` subclass, regardless of whether the original literature would call it a saga or a process manager.

---

## Compensating actions

When step N of an orchestrated saga fails after steps 1 through N-1 succeeded, the saga has to decide: undo the prior steps, retry step N, or surface the failure and accept partial completion. The undo path is the **compensating action**, and it's the single most subtle part of distributed-saga design.

### Compensation is not undo

A common mistake is treating compensation as inverse: if step 2 charged the rider, compensation un-charges. But the rider's bank statement now shows two transactions — a charge and a refund — and the rider knows the charge happened. The compensation is `IssueRefund`, not "make it as if the charge never happened." This matters because:

- **Compensating actions are themselves domain events** with their own audit trails. `RideRefunded` is a fact, not the absence of `RidePaid`.
- **Side effects are not reversible.** Email sent, SMS dispatched, external API called — none of these can be unsent. Compensating actions can issue corrective notifications, but the original side effect is in the world.
- **Order matters and is not always the inverse of the forward order.** If a saga did A → B → C and C fails, the compensation order is sometimes C-comp → B-comp → A-comp (the obvious inverse) but sometimes is A-comp → B-comp → C-comp because B's compensation depends on A still being in place. Decide explicitly per saga.

### Idempotent compensation

Compensating actions run in the same distributed environment as forward actions — they can be redelivered, retried, and replayed. A `IssueRefund` command must be idempotent: if it's delivered twice, only one refund happens. The same patterns from the idempotence section below apply, but compensation is where forgetting them hurts most because the recovery path is the path that's already been triggered by an earlier failure.

### Cab compensation examples

- **`TripCompletionSaga` payment failure.** If `ProcessPayment` returns `PaymentFailed`, the saga emits `ReleaseDriverHold` (compensating the implicit hold on the driver's slot for this trip), `NotifyRiderOfPaymentFailure`, and transitions to a `PaymentFailed` terminal state. The fare/quote computation isn't compensated — those are facts, not state to roll back.
- **`TripCompletionSaga` rating timeout.** If no rating arrives within 24 hours, the saga doesn't compensate — it just `MarkCompleted()` with the saga state recording "rating not provided." This is the right move because there's no harm to undo; the rating is genuinely optional.
- **`RiderOnboardingSaga` profile-creation failure.** If the profile service is down for an extended period, the saga emits `RiderRegistrationFailed` and the rider's identity record is left in a "pending profile" state. The Identity BC has its own cleanup policy for stale pending registrations. The saga doesn't try to delete the identity — that's not its responsibility, and the identity may be needed for retries.

### When compensation isn't possible

Some workflows have steps that cannot be compensated. In Cab, sending a push notification is one — once the rider's phone has buzzed, "uncalling" the notification is meaningless. The saga design has to acknowledge this and put the un-compensable step **at the latest possible point** in the workflow, after all other steps have succeeded. If sending the receipt email is un-compensable, do it AFTER the payment is captured AND the rating window is opened, not before. The general pattern: **arrange the saga so failures happen before un-compensable side effects, never after.**

---

## Idempotence

Idempotent: doing the same thing twice produces the same outcome as doing it once. In a distributed saga, every step and every emitted message has to assume it might be delivered or invoked more than once. There are three sources of duplicates the saga has to handle:

- **Transport redelivery.** ASB redelivers messages whose handlers throw. Kafka redelivers when the consumer group rebalances. The duplicate is the same message arriving twice with the same ID.
- **Wolverine retry policies.** A handler that throws `ConcurrencyException` is retried per `wolverine-sagas` § Concurrency. The duplicate is the same envelope, same handler, same payload.
- **Replay during recovery.** When the saga's process restarts mid-flow, scheduled timeouts and in-flight messages are re-issued from durable storage. The duplicate is the same logical step running twice.

### Idempotency keys

The right idempotency key depends on what's being deduplicated:

- **Per-message dedup** uses the Wolverine envelope ID (carried as `Wolverine-Id` and propagated across hops). ASB's `RequiresDuplicateDetection` (per `wolverine-azure-service-bus`) handles this at broker level for free; consumer-side deduplication via the inbox handles it for in-process scheduled messages.
- **Per-business-action dedup** uses the domain correlation ID — the `OfferId`, the `TripId`, the `PaymentRequestId`. This catches the case where two distinct messages arrive that are both trying to do the same business action (e.g., two `ProcessPayment` commands for the same trip due to a saga retry).
- **Per-saga-step dedup** uses the saga state itself — the saga records "step 3 done" before emitting step 4's command, so when step 4's response arrives twice, the second arrival sees the saga is already past step 4 and short-circuits.

Cab's strong convention: **the saga state is the source of truth for which steps have completed.** Don't rely on a side table or external idempotency store; the saga's own state document records what's been done. The first arrival advances state and emits the next command; the second arrival sees state has already advanced and silently completes (or logs and completes — whichever the chain's middleware is configured for).

### Idempotency at the destination, not the source

A saga emitting `ProcessPayment` to the Payments BC cannot make Payments idempotent — that's Payments' responsibility. The saga's job is:

1. Use a stable idempotency key (the `TripId` or a saga-generated `PaymentRequestId`) so duplicates are recognizable on the receiving side.
2. Tolerate Payments responding with a "this has already been processed" success — treat that response identically to a fresh success.
3. Don't assume Payments processes duplicates as duplicates; assume Payments' handler is idempotent and the response is the same regardless.

This means **every cross-BC command Cab sagas emit carries a stable correlation ID,** and every cross-BC handler receiving such commands implements idempotence explicitly. The contract between BCs includes the idempotency key; it's not optional.

### Where the saga can shortcut

Some operations are naturally idempotent: setting a status to `Completed`, publishing an event whose receivers are themselves idempotent, computing a value from existing state. For these, the saga doesn't need explicit dedup logic — the operation can be safely retried. The audit, however, still wants the duplicate visible somewhere; rely on the saga state and trace logs to record that a step was retried, even when the retry was a no-op.

---

## Distributed failure modes

The failure scenarios Cab actively designs sagas to handle:

### Saga process crash

The saga is durably stored after every message; a crash mid-handler loses only the in-flight handler invocation, which is retried on restart. The state-document model means there's no "saga currently in step 3" runtime state to lose; every step boundary is a database commit. This is the failure mode Wolverine's saga model handles essentially for free, provided `opts.Policies.AutoApplyTransactions()` is wired (per `wolverine-sagas` § Persistence).

### Mid-step destination unavailable

Step 3's destination BC is down or unreachable. The saga doesn't see this directly; the message sits in the broker's queue (ASB, Kafka). When the destination comes back, the message is delivered and the saga proceeds. **No saga-side handling is needed for transient unavailability** as long as the transport supports durable delivery — which all of Cab's three transports do. The saga only sees a problem if delivery exceeds the message's TTL or if the broker dead-letters the message; both are surface-able failures the saga handles via DLQ subscriptions or out-of-band alerting.

### Unbounded wait

A driver is offered a ride and never accepts; the saga is waiting for `OfferAccepted` that will never arrive. Solution: the saga schedules a `TimeoutMessage` at start (per `wolverine-sagas` § Timeouts), and either `OfferAccepted` or the timeout will arrive. The saga handles whichever comes first; the late one (typically the timeout if `OfferAccepted` arrived) is silently dropped per Wolverine's late-timeout semantics (`SagaChain.DetermineSagaDoesNotExistSteps`).

**Every step that depends on an external response must have a timeout.** A saga without timeouts can wedge indefinitely waiting for a response that never comes. Cab's convention: every cross-BC step has an explicit timeout, even when the expected response time is short — it's the only way to bound the saga's lifetime.

### Wedged saga

The saga has reached a state from which no automated path forward exists. Common causes: an inbound message has a payload the saga can't reconcile; a compensating action itself failed; a timeout fired but the next step's prerequisites weren't met. Cab's posture: **wedged sagas are visible, not hidden.** They surface via:

- **Saga state inspection** through `cli-jasperfx storage counts` and the saga's own status field.
- **Operations dashboard alerts** when sagas exceed an expected lifetime (Aspire-dashboard observability, per `observability-tracing`).
- **DLQ drainage** for messages that exhausted retries — these often correlate with wedged sagas.

The recovery path is operator action: read the saga state, decide what compensating action or manual update should run, and emit the corrective command. There is no automatic wedge recovery; sagas are not self-healing past their explicit compensation paths.

### Concurrent updates

Two messages for the same saga arriving simultaneously. Per `wolverine-sagas` § Concurrency, optimistic concurrency via `IRevisioned` is the standard answer; one wins, the other retries. Cab adds the `chain.OnException<ConcurrencyException>().RetryWithCooldown(...)` policy on saga chains where collisions are expected to be common — typically sagas that receive bursts of correlated messages (e.g., a payment saga receiving a flurry of webhook notifications).

### Network partition

The saga's database and the saga's message broker are on the same partition (typically: same Aspire-managed environment, same service mesh). A genuine partition between the saga's own DB and broker is rare in Cab's deployment topology. Cross-BC partitions are real — Trips' database and Payments' database can be on opposite sides of a partition — and the failure mode is "destination unavailable" above. The saga's own state stays consistent through any cross-BC partition; only the cross-BC progress stalls until the partition heals.

---

## Outbox integration

Wolverine's transactional outbox is the foundation that makes saga reliability possible. The guarantee, in plain terms: **when a saga handler returns, either (a) the saga state change AND every outbound message produced by the handler are committed atomically, or (b) nothing is committed and the handler's effects are rolled back.** No half-states.

The mechanics live in `wolverine-messaging-handlers` and the persistence-specific integrations (`marten-wolverine-aggregates`, the relevant Marten/Polecat integration in `wolverine-sagas`). What saga authors need to know:

### What the outbox protects

- **The saga's own state-document update** is committed in the same transaction as the outbound messages emitted from the handler.
- **Cascading messages from `OutgoingMessages`** participate in the outbox — they're held until commit and released on success.
- **`bus.PublishAsync` / `bus.SendAsync` from inside the handler** also enroll into the outbox transaction.
- **Scheduled messages** (`TimeoutMessage`, `bus.ScheduleAsync`) enroll into the durable scheduling table in the same transaction.

The atomic-commit pattern means a saga handler that does "advance state, emit `ProcessPayment`, schedule `PaymentTimeout`" either does all three or none. There's no race where state advances but the command is lost, or the command goes out but state didn't update.

### What the outbox does NOT protect

- **Consumer-side idempotence.** The outbox guarantees the message goes out exactly once from the producer's perspective — once committed, it WILL be delivered (potentially with retries). It does not guarantee the receiving handler runs exactly once. Receivers must be idempotent (per § Idempotence).
- **Cross-BC atomicity.** The outbox is per-database. A saga in the Trips service committing its state and an outbound `ProcessPayment` is one transaction; the Payments service receiving and processing `ProcessPayment` is a separate transaction. There is no distributed transaction. This is intentional and correct — distributed transactions are the wrong primitive for cross-BC workflows; sagas with idempotent compensation are the right one.
- **External side effects.** Calls to third-party APIs from inside a saga handler are NOT part of the outbox. If the handler calls a payment provider and then the transaction rolls back, the call has happened. Pattern: don't make external calls inline in a saga handler that also mutates state. Either move the external call to a downstream handler that runs after the saga's state commits, or use the "publish a command and let a downstream handler make the external call" pattern.

### Cab's posture

Cab sagas always run with `opts.Policies.AutoApplyTransactions()` (per `wolverine-sagas` § Persistence) and one of `IntegrateWithWolverine()` on Marten or `IntegrateWithWolverine()` on Polecat — this is non-negotiable. The atomic-commit guarantee is the foundation that makes everything else in this skill workable. A saga without the outbox is a saga without distributed-systems hygiene; the rest of the patterns in this skill assume the outbox is on.

---

## Cab use cases through the distributed-systems lens

The three Cab sagas, mapped to the concerns above:

- **`DispatchOfferTimeoutSaga`** — temporal automation, single BC. Topology: orchestration (the saga IS the timeout coordinator). Compensation: none — an expired offer just expires; no state to roll back. Idempotence: the saga state is the dedup mechanism (the second `OfferAccepted` for an already-completed saga is dropped via late-timeout semantics). Failure modes: process crash and concurrent `OfferAccepted`/`OfferTimeout` arrival are the live ones; both handled by saga state + `IRevisioned`.
- **`TripCompletionSaga`** — orchestration across Trips → Pricing → Payments → Ratings. Topology: orchestration (cross-BC coordination demands it). Compensation: payment-failure path emits `ReleaseDriverHold` and `NotifyRiderOfPaymentFailure`; rating-timeout path completes without compensating (no harm done). Idempotence: every cross-BC command carries the `TripId` as the correlation ID; receivers are idempotent on `TripId`. Failure modes: every cross-BC step has an explicit timeout; un-compensable side effects (the receipt email) happen at the latest possible point.
- **`RiderOnboardingSaga`** — long-horizon orchestration across Identity → Profile → first-trip incentive. Topology: orchestration. Compensation: profile-failure path doesn't try to delete the Identity record; it transitions to a "pending profile" state and lets Identity's own cleanup policy handle stale records. Idempotence: every step is idempotent on the `RiderId`. Failure modes: long horizon (days/weeks) is supported by saga durability; the Aspire dashboard surfaces sagas exceeding expected lifetimes for operator review.

---

## Common pitfalls

- **Choreography for workflows that need a central observable timeline.** When operations needs to answer "what's the status of the trip that just completed?", a saga's state document is the answer. Choreography distributes that state across services and forces operations to assemble it from logs. If the answer matters to operations, orchestration is the right shape.

- **Compensation as inverse.** Treating `IssueRefund` as "uncharge" misses that the rider knows the charge happened and may have already received a notification. Compensation is corrective, not negating; design the user-visible communication explicitly, not as an absence of the original event.

- **Putting un-compensable side effects early in the saga.** Sending the receipt email before the rating window closes means a rating-timeout-failed compensation can't unsend the email. Arrange the saga so failures happen before un-compensable side effects, never after.

- **Forgetting that compensation runs in the same distributed environment as forward steps.** A `IssueRefund` command can be redelivered, retried, and replayed exactly like a `ProcessPayment` command. Compensation handlers must be idempotent — and the failure mode of "the compensation itself failed" is real and needs a path (alert, manual recovery).

- **Relying on broker dedup as the only idempotence layer.** ASB's `RequiresDuplicateDetection` window is finite (typically minutes to hours); messages older than the window can deliver twice. Saga-state-based idempotence is the durable fallback — broker dedup is an optimization on top, not a substitute.

- **Implicit timeouts.** A saga step that depends on an external response without an explicit timeout will eventually wedge. Every cross-BC step needs an explicit timeout, sized to the expected response time plus an acceptable upper bound. If the response is "expected within 100ms," a 30-second timeout is fine; if the response is "expected within 24 hours," set the timeout accordingly. Don't omit the timeout because "the response always comes back fast."

- **External API calls inline in a saga handler.** Calling a payment provider's REST API from inside a saga handler that also mutates state breaks the outbox guarantee — the API call has effects that aren't part of the transaction. Pattern: emit a command (`CapturePaymentExternally`), let a downstream handler make the call, and have that handler emit the result back to the saga via a domain message.

- **Cross-BC distributed transactions.** No XA, no two-phase commit, no shared transactions across services. The whole point of the saga pattern is that distributed transactions are the wrong tool. If a design seems to need them, the answer is decomposing into a saga with idempotent compensation, not finding a way to make the distributed transaction work.

- **Wedged sagas left invisible.** A saga that's been in "waiting for response" state for 48 hours is almost certainly wedged. Without operator-facing observability, it stays wedged forever. Cab's convention is `observability-tracing` plus saga-lifetime alerts in the Aspire dashboard — surface stuck sagas the same way you'd surface any other stuck work.

- **Trying to make consumers of saga-emitted commands non-idempotent because the saga is "careful."** The saga can be careful and still cause duplicates — broker redelivery is real, retry policies are real, restart-replay is real. The receiver of every saga-emitted command must implement idempotence on its own; the saga's care doesn't substitute for the consumer's responsibility.

- **Conflating saga termination with workflow success.** `MarkCompleted()` ends the saga; it doesn't say the workflow succeeded. The saga state should record the terminal outcome (completed-with-success, completed-with-payment-failure, completed-with-rating-timeout) before `MarkCompleted()` runs. Otherwise the saga's last state is "I existed and now I don't" — an audit gap.

- **Treating the outbox as the whole distributed-systems story.** The outbox solves producer-side atomicity, which is one corner of the distributed-systems problem. Receivers still need idempotence; timeouts still need to be explicit; compensation still needs to be designed. The outbox is foundational, but it isn't sufficient.

---

## See also

**Upstream** — load these first:

- `wolverine-sagas` — the implementation companion. The `Saga` base class, handler-method conventions, persistence wiring, concurrency via `IRevisioned`, timeout patterns. Every concern in this skill assumes that skill's mechanics.
- `wolverine-messaging-handlers` — the canonical home for outbox semantics in Cab. The atomic-commit guarantee this skill builds on lives there.
- `transport-selection` — the transport choice for saga-emitted messages affects retry, DLQ, and scheduled-delivery semantics. Failure-mode analysis depends on which transport carries each step.
- `domain-event-conventions` — slim domain events vs rich integration events; the events sagas react to and emit follow these conventions.
- `event-modeling` — the workflow-modeling layer where saga shapes get committed. The Bruun temporal-automation pattern and the cross-BC workflow patterns originate there.

**Sibling skills:**

- `dynamic-consistency-boundary` — the pattern-decision skill for atomic multi-stream commands. Pairs with `marten-wolverine-aggregates` the way this skill pairs with `wolverine-sagas`. Use DCB when the entire decision is one transactional unit; use a saga when the work is genuinely long-running.
- `wolverine-azure-service-bus` — ASB's `RequiresDuplicateDetection`, sessions for saga-aware ordering, DLQ semantics. The transport details that affect saga design.
- `wolverine-kafka` — Kafka's at-least-once delivery, consumer-group rebalance redelivery, no broker-side scheduling. The transport details that affect saga design when Kafka carries saga messages.
- `grpc-vs-other-transports` — when a saga's commands route over gRPC unary (synchronous, fail-fast) vs ASB queue (broker-mediated retry/DLQ). The retry-and-DLQ properties of ASB are what make orchestration sagas tractable.

**Downstream:**

- `cli-jasperfx` — `describe`, `describe-routing`, `storage counts` against saga chains and saga storage. The operator surfaces for diagnosing wedged sagas and verifying the outbox is committing as expected.
- `observability-tracing` — distributed traces span the entire saga lifecycle including cross-BC steps; saga-state changes surface in the Aspire dashboard. The observability layer that makes wedged-saga detection workable.
- `testing-integration` — saga timeout tests via `PlayScheduledMessagesAsync`; multi-step saga assertions against state documents.
- `testing-advanced` (Phase 4) — multi-saga coordination tests, cross-host saga scenarios, failure-injection patterns.
- `identity-acl` — auth-context propagation across saga steps; saga state may need to carry tenant/user context across asynchronous step boundaries.

**External:**

- ai-skills `wolverine-sagas` and `distributed-saga-patterns` — generic patterns from JasperFx if/when published.
- [Wolverine sagas documentation](https://wolverinefx.net/guide/durability/sagas.html) — upstream reference; the "Low Ceremony Sagas with Wolverine" and "Multi Step Workflows with the Critter Stack" blog posts linked from there are the canonical Critter Stack saga walkthroughs.
- [microservices.io — Saga pattern](https://microservices.io/patterns/data/saga.html) — Chris Richardson's canonical reference for the pattern in distributed systems, including the choreography-vs-orchestration framing.
- [Sagas (Garcia-Molina & Salem, 1987)](https://dl.acm.org/doi/10.1145/38713.38742) — the original SIGMOD paper that named the pattern. Historical context for "saga" terminology vs "process manager."
- ADR-005 in [`docs/decisions/`](../../decisions/) — the three-transport ceiling that bounds which transports can carry saga messages.
- [`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md) — the immutable rules including saga-relevant constraints.
