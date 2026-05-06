---
name: wolverine-sagas
description: "Long-running stateful coordinations in CritterCab using Wolverine 5.32+ sagas. Covers the Saga base class (IsCompleted / MarkCompleted / Version), the saga-specific handler-method conventions (Start/Starts, StartOrHandle/StartsOrHandles, Orchestrate/Orchestrates/Handle/Handles, NotFound), the saga-id resolution cascade ([SagaIdentity], [SagaIdentityFrom], <SagaTypeName>Id, SagaId, Id), valid saga-id types (Guid/int/long/string + strong-typed identifiers), TimeoutMessage scheduling and the saga-not-found return-no-op semantics, manual bus.ScheduleAsync timeouts, Marten-backed saga persistence via services.AddMarten().IntegrateWithWolverine() + opts.Policies.AutoApplyTransactions(), Polecat-backed saga persistence via services.AddPolecat().IntegrateWithWolverine() with the Polecat IDocumentSession parallel, optimistic concurrency via Marten's IRevisioned interface and SagaConcurrencyException, the asymmetry between static-allowed Start/NotFound and instance-required Handle/Orchestrate, and Cab use cases (DispatchOfferTimeoutSaga for the Bruun temporal-automation pattern, TripCompletionSaga for cross-BC orchestration, RiderOnboardingSaga for multi-step identity-to-first-trip flow). Use when authoring a long-running coordination that DCB can't atomically capture."
cluster: wolverine
tags: [saga, wolverine, marten, polecat, long-running-workflow, timeout-message, schedule-async, optimistic-concurrency, mark-completed, saga-identity, process-manager, temporal-automation]
---

# Wolverine Sagas

Sagas are CritterCab's mechanism for **long-running, stateful coordinations** — workflows that span time, retries, external side effects, or multiple bounded contexts. The Dispatch offer-timeout (`OfferDispatched` → 15-second wait → `OfferAccepted` or `OfferExpired` → re-dispatch) is the canonical Cab saga; the trip-completion orchestration (Trips → Pricing → Payments → Ratings) and the rider-onboarding flow (identity → profile → first-trip-incentive) are the other shapes that earn a saga.

The single most useful idea: **a Wolverine saga is a special-cased Wolverine messaging handler.** The saga class derives from `Wolverine.Saga`, which is just a base class with an `IsCompleted()` flag and a `Version` property; the handler methods on the saga look identical in shape to the message handlers covered by `wolverine-messaging-handlers`. What makes them sagas is that Wolverine's code-generation pipeline wraps each handler call with three machine-generated steps: load the saga state from storage by correlation ID, dispatch to the handler method (which mutates state), and save the saga state back (or delete it if `MarkCompleted()` was called). Everything else — cascading messages, return-tuples, exception handling, OpenTelemetry instrumentation — is the same as a regular handler.

The decision to use a saga belongs upstream of this skill. `dynamic-consistency-boundary` covers atomic multi-stream commands where a single transactional decision spans multiple streams; sagas are the right answer when **the work is genuinely long-running** — when there's a wall-clock gap between steps, when external systems must respond, when retries with backoff matter, or when compensating actions follow failures. If the problem can be expressed as one atomic command across multiple streams, DCB is lighter weight; if it genuinely takes time and survives restarts, it's a saga.

This skill assumes the bootstrap from `service-bootstrap` and the messaging-handler conventions from `wolverine-messaging-handlers`. It does **not** cover choreography vs orchestration tradeoffs, compensating-action design, or distributed-saga failure modes — those live in `distributed-saga-considerations` (Phase 4). Saga state inspection via `cli-jasperfx describe` and storage operations via `cli-jasperfx storage counts` are in `cli-jasperfx`. Saga timeout testing via `PlayScheduledMessagesAsync` is in `testing-integration` (with deeper streaming-test patterns in `testing-advanced`, Phase 4).

---

## When to apply this skill

Use this skill when:

- Authoring a workflow that survives across multiple inbound messages with persistent state between them (e.g., the Dispatch offer-timeout, a multi-step onboarding, a trip-lifecycle orchestration).
- Implementing a Bruun temporal-automation slice from `event-modeling` — the trigger is wall-clock time, not an incoming domain event.
- Coordinating a sequence across bounded contexts where each step's success or failure determines the next (Trips → Pricing → Payments → Ratings).
- Adding compensating-action paths to an existing saga.
- Configuring saga persistence (Marten or Polecat) and `AutoApplyTransactions`.
- Diagnosing a `SagaConcurrencyException` or an "Unknown saga" failure at runtime.

Do NOT use this skill for:

- Atomic multi-stream commands where the entire decision is one transactional unit — `dynamic-consistency-boundary` is the lighter-weight fit.
- Stateless message handlers that cascade work without persistent saga state — `wolverine-messaging-handlers`.
- Single-aggregate event-sourced commands — `marten-wolverine-aggregates`.
- Choreography vs orchestration design tradeoffs, compensating-action patterns, distributed failure modes — `distributed-saga-considerations` (Phase 4).
- Saga-state CLI inspection (`describe`, `storage counts`) — `cli-jasperfx`.
- Saga timeout test harnesses — `testing-integration`, with advanced patterns in `testing-advanced` (Phase 4).

---

## Mental model

A saga has three moving parts and one explicit lifecycle:

```
              ┌────────────────────────────────────────────┐
              │              Wolverine Saga                │
              │                                            │
inbound msg  ─►  ┌─────────────┐  ┌────────────┐  ┌─────┐  │
              │  │ Load by     │─►│ Dispatch   │─►│ Save│  │
              │  │ correlation │  │ to Handle( │  │ or  │  │
              │  │  ID         │  │ )/Start()/ │  │ Del │  │
              │  └─────────────┘  │ NotFound() │  │     │  │
              │                   └────────────┘  └─────┘  │
              │                          │                 │
              │                          ▼                 │
              │                   mutate state             │
              │                   cascade messages         │
              │                   call MarkCompleted()?    │
              └────────────────────────────────────────────┘
```

The user authors the **saga class** (state + handler methods); Wolverine generates the load/save/delete frames. The chain that wraps the handler is a `SagaChain`, a specialization of `HandlerChain` per `wolverine-messaging-handlers`. The same OpenTelemetry traces, retry policies, and exception handling apply.

### The five method-name conventions

Wolverine recognizes specific method names on the saga class and routes inbound messages accordingly:

| Method name (and async variant) | When invoked | Static allowed? |
|---|---|---|
| `Start` / `Starts` / `StartAsync` / `StartsAsync` | Saga doesn't yet exist; this message creates it | Yes (often `static` returning the new saga) |
| `StartOrHandle` / `StartsOrHandles` (+ Async) | Either creates the saga (if missing) or applies to an existing one | Yes for the create branch only |
| `Orchestrate` / `Orchestrates` / `Handle` / `Handles` / `Consume` / `Consumes` (+ Async) | Saga must already exist; this message advances state | **No** — instance methods only |
| `NotFound` (+ Async) | No saga found for this message's correlation ID | Yes (typically `static` for logging or routing) |

The synonyms are deliberate — `Orchestrate`, `Handle`, and `Consume` all mean the same thing, and Cab uses whichever reads best for the workflow. `Orchestrate` reads naturally for sagas that command other services; `Handle` reads naturally when applying an inbound event; `Consume` reads naturally for messages that arrive from a topic. Pick one per saga and stay consistent.

The static-method asymmetry is enforced by Wolverine at startup: `It is not legal to use static methods to operate on existing sagas. Use NotFound() for handling non-existent sagas for the identity` is the verbatim error message thrown by `SagaChain` when an `Orchestrate`/`Handle`/`Consume` method is declared static.

### Saga-ID resolution cascade

Wolverine determines which saga instance a message correlates to by walking six rules in order. The first match wins. This is from `SagaChain.DetermineSagaIdMember`:

1. A property or field on the message marked `[SagaIdentity]`.
2. A property or field whose name matches a handler parameter's `[SagaIdentityFrom("PropertyName")]` attribute.
3. A property or field named `<SagaTypeName>Id` (e.g., `DispatchOfferTimeoutSagaId` for `DispatchOfferTimeoutSaga`).
4. The same name with `Saga` stripped (e.g., `DispatchOfferTimeoutId` from `DispatchOfferTimeoutSaga`).
5. A property or field named `SagaId`.
6. A property or field named `Id` (case-insensitive).

If none match, startup fails. Cab's strong preference is rule 1 — the explicit `[SagaIdentity]` attribute — because it makes the correlation visible at the message-type definition site.

### Valid saga-ID types

`Guid`, `int`, `long`, `string`, plus strong-typed identifier types (any non-primitive non-enum type that the persistence provider knows how to serialize). For Marten, the saga-ID type is whatever Marten resolves the saga document's ID column to via `store.Options.FindOrResolveDocumentType(sagaType).IdType`. For Polecat, it's the type of the saga's `Id` property directly. Cab's convention is `Guid` (UUID v7 from the Critter Stack defaults) for sagas correlated by domain entities, and `string` for sagas correlated by external identifiers (e.g., a payment-provider transaction ID).

---

## Defining a saga

Cab's canonical example is the Dispatch offer-timeout saga from `event-modeling` § Bruun Temporal-Automation Slice Pattern. The flow: when an offer is dispatched to a candidate driver, schedule a 15-second timeout. If the driver accepts within the window, mark the saga complete. If the timeout fires first, emit `OfferExpired` and re-dispatch to the next candidate.

```csharp
// src/CritterCab.Dispatch/OfferTimeoutSaga/DispatchOfferTimeoutSaga.cs
using JasperFx.Core;
using Wolverine;

namespace CritterCab.Dispatch.OfferTimeoutSaga;

public class DispatchOfferTimeoutSaga : Saga
{
    public Guid Id { get; set; }                  // saga ID: the OfferId
    public Guid RideRequestId { get; set; }       // captured at start; used for re-dispatch
    public Guid DriverId { get; set; }            // the candidate this offer was sent to
    public List<Guid> ExhaustedDrivers { get; set; } = [];
    public DateTimeOffset DispatchedAt { get; set; }

    // Static Start methods are allowed; they create the saga.
    // Returning a tuple (Saga, OutgoingMessages) cascades messages alongside saga creation.
    public static (DispatchOfferTimeoutSaga, OfferTimeout) Start(
        OfferDispatched dispatched,
        TimeProvider clock)
    {
        var saga = new DispatchOfferTimeoutSaga
        {
            Id = dispatched.OfferId,
            RideRequestId = dispatched.RideRequestId,
            DriverId = dispatched.DriverId,
            DispatchedAt = clock.GetUtcNow(),
        };
        var timeout = new OfferTimeout(dispatched.OfferId);
        return (saga, timeout);
    }

    // Instance Handle method — required to be non-static.
    public void Handle(OfferAccepted accepted)
    {
        // Driver accepted within the window; the saga is complete.
        MarkCompleted();
    }

    // The timeout fired before OfferAccepted arrived. Emit OfferExpired and
    // either re-dispatch to the next candidate or mark exhausted.
    public OutgoingMessages Handle(
        OfferTimeout timeout,
        IDriverCandidatePool candidates)
    {
        var messages = new OutgoingMessages
        {
            new OfferExpired(Id, RideRequestId, DriverId)
        };

        ExhaustedDrivers.Add(DriverId);
        var next = candidates.NextCandidate(RideRequestId, ExhaustedDrivers);
        if (next is null)
        {
            // No more candidates — surface the failure and complete the saga.
            messages.Add(new RideRequestUnfulfillable(RideRequestId));
            MarkCompleted();
            return messages;
        }

        // Re-dispatch and reset the timeout.
        DriverId = next.Value;
        messages.Add(new DispatchOffer(Id, RideRequestId, next.Value));
        return messages;
    }

    // Static NotFound is invoked when an OfferAccepted arrives for an offer
    // whose timeout saga has already completed (rare but possible on retry).
    public static void NotFound(OfferAccepted accepted, ILogger<DispatchOfferTimeoutSaga> logger)
    {
        logger.LogInformation(
            "OfferAccepted for {OfferId} arrived after the timeout saga had completed; ignoring.",
            accepted.OfferId);
    }
}

// The timeout message — TimeoutMessage carries the schedule-after delay.
public record OfferTimeout(Guid OfferId) : TimeoutMessage(15.Seconds());
```

### What the example demonstrates

- **`Saga` base class.** The saga class inherits from `Wolverine.Saga`, which provides `IsCompleted()`, the protected `MarkCompleted()` method, and the `Version` property used for optimistic concurrency.
- **The saga ID is `Id`.** This matches resolution rule 6 (the case-insensitive `Id` fallback). Convention: name the property `Id` for sagas, even though `OfferTimeoutSagaId` would also work via rule 3.
- **Cascading from `Start`.** Returning `(Saga, OtherMessage)` from a static `Start` method tells Wolverine to create the saga AND emit the cascading message. The same tuple pattern works for `(Saga, Message1, Message2, ...)` and `(Saga1, Saga2, OutgoingMessages)` per `starting_saga_by_returning_it_from_handler.cs` in Wolverine's tests.
- **`OutgoingMessages` from a handler.** The `Handle(OfferTimeout)` method returns `OutgoingMessages`, which is Wolverine's batch-cascade type. Each entry routes through the bus per `service-bootstrap`'s routing configuration — saga-emitted messages are not "internal" to the saga.
- **`MarkCompleted()` deletes the saga.** When `IsCompleted()` returns true at the end of message handling, Wolverine emits a delete operation rather than an update. The saga state is gone after the next `SaveChangesAsync`.
- **`TimeoutMessage` schedules itself.** The `OfferTimeout` record inherits `TimeoutMessage` and supplies a `15.Seconds()` delay in its constructor. When emitted from the saga, Wolverine scheduling primitives deliver it at the specified time.

### Saga-ID correlation on the messages

The messages that drive the saga (`OfferAccepted`, `OfferTimeout`, `OfferDispatched`) all need to expose a saga ID. Cab's convention is `[SagaIdentity]` on the message:

```csharp
// src/CritterCab.Dispatch.Contracts/OfferAccepted.cs
using Wolverine.Persistence.Sagas;

public record OfferAccepted(
    [property: SagaIdentity] Guid OfferId,
    Guid DriverId,
    DateTimeOffset AcceptedAt);

public record OfferTimeout(Guid OfferId) : TimeoutMessage(15.Seconds());
// OfferTimeout's OfferId matches resolution rule 6 (the "Id" fallback);
// no [SagaIdentity] needed because OfferId is the only candidate.
```

For messages where multiple Guid properties exist, `[SagaIdentity]` removes ambiguity. Cab's strong convention is to use it explicitly anyway — it documents the correlation at the message definition site.

---

## Persistence

Sagas need durable storage so they survive process restarts. Cab supports both Marten and Polecat as saga stores; the choice typically follows the bounded context's primary persistence engine.

### Marten-backed saga persistence

The standard Cab path. Marten stores the saga as a document; Wolverine generates the `IDocumentSession.Insert`/`Update`/`Delete` operations and wraps the message-handling chain in a `SaveChangesAsync`.

```csharp
// src/CritterCab.Dispatch/Program.cs (excerpt)
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("dispatch")!);
    opts.DatabaseSchemaName = "dispatch";
    opts.UseSystemTextJsonForSerialization();
}).IntegrateWithWolverine();

builder.Host.UseWolverine(opts =>
{
    opts.Policies.AutoApplyTransactions();
    // ... per service-bootstrap
});
```

Two registrations matter:

- **`IntegrateWithWolverine()`** on the Marten configuration registers the `MartenPersistenceFrameProvider`, which supplies the `LoadDocumentFrame`, `DocumentSessionOperationFrame` (Insert/Update/Delete), and the `DocumentSessionSaveChanges` postprocessor that the saga chain depends on.
- **`opts.Policies.AutoApplyTransactions()`** ensures every chain — saga and non-saga — gets the transactional boundary (open session, save at end). Without it, saga handlers throw at runtime because the chain has no `SaveChangesAsync` postprocessor.

Marten resolves the saga ID type via its document-type registry (`store.Options.FindOrResolveDocumentType(sagaType).IdType`). Cab's convention of a `Guid Id` property on the saga class produces a Guid-typed document with a Guid-typed saga ID column.

### Polecat-backed saga persistence

The path Cab uses for SQL-Server-anchored bounded contexts (Payments per the Cab vision document). Mirrors Marten's shape almost exactly:

```csharp
// src/CritterCab.Payments/Program.cs (excerpt)
builder.Services.AddPolecat(opts =>
{
    opts.ConnectionString = builder.Configuration.GetConnectionString("payments")!;
    opts.DatabaseSchemaName = "payments";
}).IntegrateWithWolverine();

builder.Host.UseWolverine(opts =>
{
    opts.Policies.AutoApplyTransactions();
    // ...
});
```

The `PolecatPersistenceFrameProvider` parallels Marten's almost line-for-line: same `LoadDocumentFrame`, same `DocumentSessionOperationFrame` semantics, same `SaveChangesAsync` postprocessor — against Polecat's `IDocumentSession` rather than Marten's. The two `IDocumentSession` types share a name and shape but are distinct types in distinct namespaces (`Marten` vs `Polecat`); the codegen wires the right one per chain based on which provider's `IntegrateWithWolverine()` was called.

One difference: Polecat resolves the saga ID type by inspecting the saga's `Id` property type directly (`sagaType.GetProperty("Id").PropertyType`). Marten goes through its document-type registry. The practical consequence is that Cab's `Guid Id` convention on saga classes is even more important under Polecat — there's no document-type-registry indirection to recover from a deviating property name.

For the Polecat document-store and event-store APIs that wrap the saga storage, see `polecat-document-store` and `polecat-event-sourcing` (both Phase 4).

### In-memory persistence

Wolverine ships an in-memory saga store via `InMemoryPersistenceFrameProvider`. It's automatically used when no other provider is registered — useful for unit tests that don't want a real database. Don't ship in-memory-only configurations to production; sagas exist precisely because state needs to survive restarts.

---

## Concurrency

Sagas can be hit by concurrent messages. Two requests for the same offer arriving in quick succession both want to load, mutate, and save the same saga — without coordination, the second write overwrites the first.

### Optimistic concurrency via `IRevisioned` (Marten)

Marten's `IRevisioned` interface (from `Marten.Metadata`) opts a saga in to revision-based optimistic concurrency. The saga's stored revision is checked at update time; if it doesn't match the revision the chain loaded, `JasperFx.ConcurrencyException` is thrown and Wolverine's normal retry-or-DLQ policies apply.

```csharp
using Marten.Metadata;

public class DispatchOfferTimeoutSaga : Saga, IRevisioned
{
    public Guid Id { get; set; }
    public new int Version { get; set; }   // 'new' to shadow Saga.Version with the IRevisioned property
    // ... rest of state
}
```

The `MartenPersistenceFrameProvider` detects `IRevisioned` and emits the revision-aware `UpdateSagaRevisionFrame` (which calls `IDocumentSession.UpdateRevision(saga, expectedRevision)`) instead of a plain `IDocumentSession.Update`. The conflict surfaces as `JasperFx.ConcurrencyException` on `SaveChangesAsync`. Note that this is `JasperFx.ConcurrencyException` from the `JasperFx` namespace — not Wolverine's separate `SagaConcurrencyException`, which is thrown by the raw-RDBMS saga storage paths (Postgres/SQL Server/MySQL/Oracle when used as message-store-backed saga storage, not via Marten or Polecat).

### Optimistic concurrency via `Saga.Version` (provider-agnostic)

Wolverine's base `Saga` class exposes a `Version` property and a corresponding `SagaConcurrencyException`. Providers that implement version-aware updates check this property at save time. Marten's `IRevisioned` is the most-common path Cab uses; the raw-RDBMS saga storage paths (Postgres/SQL Server) throw `SagaConcurrencyException` on conflict. Polecat saga concurrency parallels Marten's at the API level but the specific revision-tracking semantics are verified in `polecat-event-sourcing` (Phase 4); don't assume the exact `IRevisioned` interface is the opt-in for Polecat sagas without checking that skill when it lands.

### Pessimistic alternatives

Wolverine has no first-class pessimistic-locking primitive for sagas. The right pessimistic answer is a Wolverine endpoint policy that serializes saga handling — `RequireSessions` on ASB (per `wolverine-azure-service-bus` § Sessions), or `ListenWithStrictOrdering` on a transport that supports it, ensures only one message per saga ID runs at a time. This trades throughput for simplicity and is the right tradeoff when collisions are expected to be common (e.g., a saga that genuinely receives bursts of correlated messages).

### Parallel to aggregate handlers

This concurrency surface mirrors the one in `marten-wolverine-aggregates` for `[WriteAggregate]` handlers — same trade-off (optimistic-with-retries vs pessimistic-with-serialization), same provider semantics, same `IRevisioned` opt-in for Marten. The difference is what's being protected: aggregate handlers protect a single event stream; sagas protect a saga state document. The mechanics line up because both rely on the underlying document store's optimistic-concurrency contract.

---

## Timeouts

Sagas need to schedule work to happen at a future time — the offer-timeout fires 15 seconds after dispatch, the trip-arrival check fires at the projected arrival time, the payment-retry fires after a backoff. Wolverine offers two patterns.

### `TimeoutMessage` for declarative timeouts

The `TimeoutMessage` abstract class carries a delay in its constructor:

```csharp
public record OfferTimeout(Guid OfferId) : TimeoutMessage(15.Seconds());
public record TripArrivalCheck(Guid TripId) : TimeoutMessage(/* dynamic */);
```

When a saga emits a `TimeoutMessage` (from a `Start` or `Handle` method), Wolverine's scheduling primitive delivers it back to the saga at the scheduled time. The handler matches by message type via the same saga-ID resolution rules.

A subtle but important behavior: when a `TimeoutMessage` arrives for a saga that no longer exists (because `MarkCompleted()` was called before the timeout fired), `SagaChain.DetermineSagaDoesNotExistSteps` short-circuits with a `ReturnFrame` — the message is silently dropped, NOT routed to `NotFound`. This is the design: timeouts that fire after their saga has completed normally aren't anomalies; they're stale-but-expected delivery. If you want explicit logging for late timeouts, log inside the saga handler when the saga is still alive, not via `NotFound`.

### `bus.ScheduleAsync` for ad-hoc scheduling

When the timeout delay is dynamic, or when the message scheduled is something other than a `TimeoutMessage`, schedule explicitly:

```csharp
public async Task Handle(
    OfferDispatched dispatched,
    IMessageBus bus)
{
    Id = dispatched.OfferId;
    var deadline = dispatched.DispatchedAt + 15.Seconds();
    await bus.ScheduleAsync(new OfferTimeout(Id), deadline);
}
```

`ScheduleAsync` is provider-agnostic — it works against ASB's native `ScheduledEnqueueTime` (per `wolverine-azure-service-bus`), against the durable-message inbox for in-process scheduling, and against any other transport that supports scheduling.

The two patterns coexist: `TimeoutMessage` is the declarative form for fixed delays; `ScheduleAsync` is the imperative form for dynamic ones. Use `TimeoutMessage` when you can — the scheduled-at time is visible at the message-definition site.

### Scheduling primitives interact with transport choice

`transport-selection` § "I want to send a message in 30 seconds" routes scheduled-delivery flows to ASB because ASB has native broker-side scheduling. For saga timeouts specifically, that decision is already made — the saga's transport is whatever the routing configuration sends saga messages to. If the saga's messages route through ASB, scheduling is broker-native; if they route locally, scheduling uses the durable inbox; if they route over Kafka, scheduling is not available (Kafka has no scheduled-delivery primitive) and the saga must use a different transport for its scheduled messages.

---

## Cab use cases

The skill leads with the Dispatch offer-timeout saga because `event-modeling` already commits to it. The other Cab sagas the project has named:

### `TripCompletionSaga` (Trips → Pricing → Payments → Ratings)

Long-running cross-BC orchestration. When a trip completes, the saga coordinates the post-trip flow:

1. `TripCompleted` (from Trips) starts the saga.
2. Saga emits `RequestQuoteFinalization` to Pricing.
3. Pricing responds with `QuoteFinalized`; saga emits `ProcessPayment` to Payments.
4. Payments responds with `PaymentCaptured` (or `PaymentFailed`, triggering compensation); saga emits `SolicitRating` to Ratings.
5. Ratings responds with `RatingSubmitted` (or a timeout fires after N hours); saga calls `MarkCompleted()`.

Each step has its own timeout and compensating action. The saga state holds enough context (trip ID, fare, payment status) to drive the next step or recover from a failure. Cross-BC orchestration is exactly what sagas exist for; trying to express this as a DCB handler would conflate "atomic decision" with "long-running workflow."

### `RiderOnboardingSaga` (Identity → Profile → first-trip incentive)

Multi-step user-journey orchestration. `RiderRegistered` (from Identity) starts the saga; the saga drives profile creation, eligibility checks, and the first-trip incentive grant. Each step may take indefinite time (the user might take days to complete profile information). The saga is the durable handle on the user's progress through the funnel.

### `TripArrivalSaga` (the second `event-modeling` temporal-automation example)

When a trip transitions to `EnRoute`, the saga schedules an arrival check at the projected arrival time. If `DriverArrived` hasn't fired by then, the saga commits a domain-meaningful event (`TripArrivalDelayed`) that triggers downstream effects — rider notification, support escalation, etc. Same temporal-automation shape as the offer-timeout, longer time horizons.

---

## Tooling

Cab's saga-aware CLI surfaces are part of `cli-jasperfx`:

- **`wolverine-diagnostics describe`** — lists every saga chain in the service, including the saga type, the message types that start vs handle vs orchestrate vs not-found, and the resolved saga-ID member per message. Useful when correlation isn't behaving as expected.
- **`wolverine-diagnostics describe-routing <MessageType>`** — confirms which saga (if any) the message routes to. A common mistake is assuming a message routes to a saga because the saga has a `Handle` method for it, when in fact the saga ID can't be resolved on the message.
- **`storage counts`** — reports row counts per saga storage table, useful for verifying that saga state is being persisted (and deleted on `MarkCompleted()`) as expected.
- **`describe-routing` for `TimeoutMessage` types** — verifies the scheduling primitive is wired through the expected transport (durable inbox, ASB scheduled delivery, etc.).

For testing, `testing-integration` covers the standard saga-test pattern: invoke a starting message via `host.InvokeMessageAndWaitAsync`, assert the saga state via the document store, then invoke `host.MessageBus().PlayScheduledMessagesAsync` to fast-forward the timeout. Advanced multi-saga and cross-host scenarios live in `testing-advanced` (Phase 4).

---

## Common pitfalls

- **Declaring `Handle` (or `Orchestrate` / `Consume`) as `static`.** `SagaChain` rejects this at startup with `It is not legal to use static methods to operate on existing sagas. Use NotFound() for handling non-existent sagas for the identity`. The reason: static methods have no instance to mutate. `Start` and `NotFound` can be static (they create or route around the missing instance); the handlers that advance an existing saga cannot.

- **Forgetting `opts.Policies.AutoApplyTransactions()`.** Without it, the saga chain has no `SaveChangesAsync` postprocessor and saga state never persists. The symptom is "saga seems to start but the state isn't there on the next message" — a confusing failure because the in-flight chain works fine; only persistence breaks.

- **Saga-ID resolution that the cascade doesn't catch.** When the saga ID property on the message has a name not covered by any of the six resolution rules, startup fails. The fastest fix is `[SagaIdentity]` on the property; the durable fix is naming the property to match rule 3 (`<SagaTypeName>Id`).

- **A message handled by multiple saga types without `MultipleHandlerBehavior.Separated`.** Wolverine throws `Multiple saga types (...) handle message X. Set MultipleHandlerBehavior to Separated to allow this.` at startup. Setting the option splits the chain into per-saga endpoints; the trade-off is per-saga endpoint configuration rather than a single chain.

- **Returning the saga from a `Handle` method.** `Start` methods may return the saga (that's how they create it); `Handle` methods must not. The behavior is undefined and Wolverine may treat the return as a cascading message of the saga's own type — which then dispatches as if it were a new message. Mutate `this` in `Handle`; don't return the saga.

- **Treating `NotFound` as the catch-all for late timeouts.** `TimeoutMessage` arriving for a completed saga is a no-op (`SagaChain.DetermineSagaDoesNotExistSteps` returns early); it does NOT route to `NotFound`. Logging "I got a late timeout" must happen inside the timeout handler while the saga is still alive, not in `NotFound`.

- **Missing the `Async` suffix on async saga methods.** Wolverine's `findByNames` matches both `Start` and `StartAsync`, both `Handle` and `HandleAsync`, etc. (see `SagaChain.findByNames`). The trap is the inverse: a method named `HandleStuff` (not matching any convention name) is silently ignored, and the saga appears to drop messages at runtime.

- **Forgetting `IRevisioned` when concurrent saga updates are expected.** Without optimistic concurrency, two concurrent messages for the same saga both succeed and one silently overwrites the other. Add `IRevisioned` from `Marten.Metadata` (or the equivalent for the chosen provider) and let `SagaConcurrencyException` surface the conflict.

- **`new int Version` instead of `int Version`.** The `Saga` base class declares `public int Version { get; set; }`. When implementing `IRevisioned`, the property must be declared with `new` to shadow the base class's property cleanly. Without `new`, the C# compiler warns and the persistence path may not pick up the right property. Cab's convention: always declare with `new int Version { get; set; }` when implementing `IRevisioned`.

- **Putting expensive IO in a `Start` method.** `Start` is invoked synchronously inside the message-handling chain; long IO blocks the inbound transport. Put IO in subsequent `Handle` methods triggered by the cascading messages from `Start`, not inside `Start` itself. The pattern from the offer-timeout: `Start` creates the saga and schedules the timeout; subsequent handlers do the heavy lifting.

- **Treating saga-emitted messages as "internal."** Cascading messages from a saga (returned from a handler, or sent via `bus.PublishAsync`/`bus.SendAsync` inside the handler) flow through Wolverine's normal routing per `service-bootstrap`. A saga in the Trips service emitting `ProcessPayment` routes to Payments via whatever transport `transport-selection` chose for that message. The saga doesn't know or care about the transport; its messages are first-class Wolverine messages.

- **Conflating saga lifecycle with aggregate lifecycle.** A saga's `Id` is a saga-state-document identifier; it is NOT an event stream's identifier (per `marten-aggregates`). Sagas can correlate on aggregate IDs (the offer-timeout's `Id` IS the offer's ID), but the storage is a saga document, not the aggregate's event stream. The two have different concurrency models, different persistence paths, and different deletion semantics.

- **Trying to `await` saga handler completion synchronously in upstream handlers.** A saga is fundamentally asynchronous — you fire-and-forget into it, and it emits messages back when work completes. An upstream handler that wants synchronous confirmation isn't talking to a saga; it's talking to a unary RPC or a request-reply queue per `grpc-vs-other-transports`.

- **Including `IDocumentSession` as a saga-handler parameter.** The saga chain already has the session in scope (the `CreateDocumentSessionFrame` runs as middleware). Declaring `IDocumentSession` as a parameter doesn't break anything but creates the appearance of a second session — confusing during code review. Inject only the services the saga genuinely needs (a domain service like `IDriverCandidatePool` from the offer-timeout example, or a `TimeProvider`).

- **Reusing a single saga class for too many message types.** Sagas with twelve `Handle` methods become "process managers that do everything" and lose the workflow clarity that justified the saga in the first place. If a saga's surface is sprawling, decompose into multiple sagas (one per workflow stage), each with a focused responsibility. The Trip-completion orchestration is one saga; the rider onboarding is another. Don't merge them just because they both touch riders.

---

## See also

**Upstream** — load these first:

- `wolverine-messaging-handlers` — the underlying handler shape that saga handler methods extend. Sagas are special-case messaging handlers with state.
- `service-bootstrap` — the `Program.cs` composition that includes `IntegrateWithWolverine()` and `AutoApplyTransactions()`.
- `domain-event-conventions` — informs how the messages that drive sagas (the events they react to and the integration messages they emit) are named and shaped.
- `event-modeling` — the Bruun temporal-automation slice pattern that maps directly to saga implementations; the offer-timeout and trip-arrival examples come from there.
- `transport-selection` — saga-emitted messages route through the transport chosen for each message type; saga timeouts compose with the transport's scheduling primitives.

**Sibling skills:**

- `marten-wolverine-aggregates` — the `[WriteAggregate]` aggregate handlers; same concurrency model (optimistic via `IRevisioned`, pessimistic via session ordering), different protected entity (event stream vs saga document).
- `dynamic-consistency-boundary` — when DCB fits (atomic multi-stream commands) vs when sagas fit (long-running workflows). DCB is the lighter-weight answer when the entire decision is one transactional unit.
- `wolverine-azure-service-bus` — sagas often coordinate across BCs over ASB; sessions provide saga-aware ordering when collisions matter.
- `wolverine-kafka` — sagas occasionally consume Kafka topics for event-driven triggers; Kafka has no scheduled-delivery primitive, so saga timeouts must use a different transport.
- `vertical-slice-organization` — saga files live in their own slice (`OfferTimeoutSaga/`), alongside the messages and handlers that participate.

**Downstream:**

- `distributed-saga-considerations` (Phase 4) — choreography vs orchestration tradeoffs, compensating-action design, distributed failure modes, idempotence requirements, outbox integration. Pairs with this skill the way `dynamic-consistency-boundary` pairs with `marten-wolverine-aggregates`.
- `polecat-event-sourcing` (Phase 4) — Polecat's saga storage uses the same `IDocumentSession`-based path as Marten's; Polecat-specific considerations (SQL Server connection strings, schema management) live there.
- `cli-jasperfx` — `describe`, `describe-routing`, `storage counts` against saga chains and saga storage.
- `testing-integration` — saga timeout tests via `PlayScheduledMessagesAsync`, fixture pattern for saga-state assertions.
- `testing-advanced` (Phase 4) — multi-saga coordination tests, cross-host saga scenarios.
- `identity-acl` — auth-context propagation across saga steps; the saga state may need to carry tenant/user context across asynchronous step boundaries.
- `observability-tracing` — the `SagaIdMember` is auto-audited on every chain, surfacing the saga ID in OpenTelemetry traces; saga step transitions are visible in the Aspire dashboard.

**External:**

- ai-skills `wolverine-sagas` — generic Wolverine saga patterns if/when JasperFx publishes one. Complements this skill.
- All ai-skills installed via `npx skills add` (license required).
- [Wolverine sagas documentation](https://wolverinefx.net/guide/durability/sagas.html) — upstream reference for the `Saga` base class, method conventions, and persistence integrations.
- [Marten optimistic concurrency documentation](https://martendb.io) — Marten's documentation site; the `IRevisioned` interface and `UpdateRevision()` method are the optimistic-concurrency entry points the Marten saga path uses.
- [Saga pattern (microservices.io)](https://microservices.io/patterns/data/saga.html) — Chris Richardson's canonical reference for the saga pattern in distributed systems.
