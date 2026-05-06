---
name: wolverine-handlers
description: "General Wolverine handler conventions in CritterCab — class shape, validation pipeline, return types overview, conventions, and diagnostics. Hub skill that points to HTTP, messaging, and gRPC siblings for protocol-specific patterns."
cluster: wolverine
tags: [wolverine, handlers, conventions, validation, hub]
---

# Wolverine Handlers

Hub skill for Wolverine handler authoring in CritterCab. Covers what every Wolverine handler in the project shares regardless of how it's triggered: class shape, validation pipeline, return type orientation, conventions, and diagnostics.

Protocol-specific patterns live in three sibling skills:

- `wolverine-http-handlers` — HTTP endpoint patterns, route binding, OpenAPI inference.
- `wolverine-messaging-handlers` — message-bus patterns, routing rules, transactional outbox semantics.
- `wolverine-grpc-handlers` (Phase 3) — gRPC unary and server-streaming handlers.
- `wolverine-grpc-bidirectional-handlers` (Phase 4) — gRPC client-streaming and bidirectional handlers.

The generic Wolverine handler mechanics — A-Frame architecture, the decider pattern, the full lifecycle method set, IoC optimization — are documented authoritatively in the JasperFx ai-skills library. Install ai-skills at the user level (`npx skills add`) and treat those as the upstream reference. **This skill documents the project-specific shape decisions that apply to handlers regardless of trigger.**

## When to apply this skill

Use this skill when:

- Authoring any Wolverine handler — load this first, then the relevant protocol-specific sibling.
- Reviewing handler PRs for shape and convention compliance.
- Deciding which protocol-specific sibling to consult next.
- Diagnosing handler issues at the general level (codegen, registration, lifecycle).

For protocol-specific patterns, jump to the sibling skill after loading this one.

---

## Cab-specific orientation

Every CritterCab service runs Wolverine and uses the same handler conventions, regardless of which store the service uses (Marten or Polecat) or which transport carries its messages (gRPC, Kafka, ASB). The vocabulary differences are mechanical:

- **Store:** `MartenOps.StartStream<T>` for Marten services, `PolecatOps.StartStream<T>` for Polecat services. The pattern is identical.
- **Aggregate ID:** `[WriteAggregate(nameof(Cmd.SomeId))]` parameter-level attribute is the canonical form everywhere. `[AggregateHandler]` class-level is not used in CritterCab.

If you're new to Wolverine handlers in general, start by reading these ai-skills (in order):

1. `wolverine-handlers-fundamentals` — basic shape and discovery conventions
2. `wolverine-handlers-pure-functions` — the decider pattern, why handlers are pure
3. `wolverine-handlers-a-frame-architecture` — infrastructure at edges, logic in the middle
4. `wolverine-handlers-railway-programming` — `Validate`/`ProblemDetails` short-circuit pattern
5. `wolverine-handlers-declarative-persistence` — `[Entity]`, `[WriteAggregate]`, `[ReadAggregate]`

Those five skills cover ~80% of what a Wolverine handler is. This skill picks up at the project-specific decisions.

---

## CritterCab Handler Shape

The canonical handler in CritterCab. Command + Validator + Handler colocated in one file (per `vertical-slice-organization`):

```csharp
namespace CritterCab.Dispatch;

public sealed record AcceptOffer(Guid OfferId, Guid DriverId)
{
    public sealed class AcceptOfferValidator : AbstractValidator<AcceptOffer>
    {
        public AcceptOfferValidator()
        {
            RuleFor(x => x.OfferId).NotEmpty();
            RuleFor(x => x.DriverId).NotEmpty();
        }
    }
}

public static class AcceptOfferHandler
{
    public static ProblemDetails Validate(AcceptOffer cmd, RideOffer? offer)
    {
        if (offer is null)
            return new ProblemDetails { Detail = "Offer not found", Status = 404 };
        if (offer.Status != OfferStatus.Pending)
            return new ProblemDetails { Detail = "Offer is no longer pending", Status = 409 };
        return WolverineContinue.NoProblems;
    }

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
}
```

Six things to internalize from this:

- **`sealed record` for the command**, with the validator nested inside it. The validator is part of the command's own contract, not a separate concern that lives elsewhere.
- **`static class` for the handler**, suffix `Handler`. CritterCab does not use instance handlers — the static-class form is mandatory unless explicitly justified, because instance handlers prevent code-gen optimizations and complicate testing.
- **`Validate` (not `Before`)** is the conventional name in CritterCab for synchronous precondition checks. Wolverine accepts both `Before` and `Validate`; `Validate` is preferred because the intent is clearer.
- **`WolverineContinue.NoProblems` is a reference-equality singleton.** Don't construct a `new ProblemDetails()` thinking it means "continue" — it doesn't, and the pipeline aborts. Always return the singleton.
- **`TimeProvider time` parameter** for any handler that reads the current time. See `csharp-coding-standards` § TimeProvider.
- **Domain event returned directly + `OutgoingMessages` for the integration event.** The two are different types in different namespaces — the slim domain event in the local Marten stream, the rich integration event published over the bus. See `domain-event-conventions` § Slim Domain Events vs. Rich Integration Events.

---

## Validate vs ValidateAsync

CritterCab uses three validation layers, in order:

1. **FluentValidation** (the nested `AbstractValidator<T>`) — structural rules on the command's fields. Runs first.
2. **`Validate(...)` synchronous method** — business rules against already-loaded state (the `[WriteAggregate]` aggregate, `[Entity]`-loaded documents). Runs second.
3. **`ValidateAsync(...)` async method** — only when the validation requires a database query that can't be expressed via `[Entity]` (typically uniqueness checks against a different aggregate type).

```csharp
public static class CompleteTripHandler
{
    // Sync — checks against already-loaded aggregate state
    public static ProblemDetails Validate(CompleteTrip cmd, Trip trip)
    {
        if (trip.Status != TripStatus.InProgress)
            return new ProblemDetails { Detail = "Trip is not in progress", Status = 409 };
        return WolverineContinue.NoProblems;
    }

    // Async — checks against a different aggregate (no [Entity] for it on this handler)
    public static async Task<ProblemDetails> ValidateAsync(
        CompleteTrip cmd,
        IQuerySession session,
        CancellationToken ct)
    {
        var driver = await session.LoadAsync<Driver>(cmd.DriverId, ct);
        if (driver is { Status: DriverStatus.Suspended })
            return new ProblemDetails { Detail = "Driver is suspended", Status = 409 };
        return WolverineContinue.NoProblems;
    }

    public static (TripCompleted, OutgoingMessages) Handle(
        CompleteTrip cmd,
        [WriteAggregate(nameof(CompleteTrip.TripId))] Trip trip,
        TimeProvider time)
    {
        // Happy path only — Validate and ValidateAsync have already run
    }
}
```

**`Handle` is always the happy path.** No null checks, no state-validity checks, no conditional returns. If execution reached `Handle`, the command is valid against everything that's been loaded. This is the discipline that keeps handlers pure and testable.

For deeper lifecycle coverage (compound `Load` methods, tuple threading, `OnException` recovery, post-handler `After` hooks, middleware scoping), see ai-skills `wolverine-handlers-middleware`.

---

## Handler Return Types — Orientation

The return type tells Wolverine what to do. Project-specific guidance for the most common shapes:

| What the handler does | Return type | Notes |
|---|---|---|
| Append a single event to a loaded aggregate | `EventName` (the event type directly) | HTTP endpoints have a footgun here — see `wolverine-http-handlers`. |
| Append events + publish integration message(s) | `(EventName, OutgoingMessages)` | Most common Cab handler shape. |
| Start a new event stream | `IStartStream` (from `MartenOps.StartStream<T>(...)` or `PolecatOps.StartStream<T>(...)`) | See § Starting a new stream below — silent-failure footgun. |
| Start stream + return HTTP creation response | `(CreationResponse<T>, IStartStream)` | HTTP response always first in the tuple — see `wolverine-http-handlers`. |
| Append events + publish messages + return HTTP response | `(IResult, EventName, OutgoingMessages)` | All three commit atomically. |
| No persistence, no messages | `void` or `Task` | Rare; typically only for read-only `Validate`-only endpoints. |

The exact set of return types Wolverine recognizes is broader than this table — `UpdatedAggregate`, `AcceptResponse`, multiple-event collections, etc. See ai-skills `wolverine-handlers-declarative-persistence` for the full reference.

For HTTP-specific return-type guidance (concrete vs `IResult`, OpenAPI inference, the bare-event-return footgun), see `wolverine-http-handlers`. For messaging-specific return-type guidance (`OutgoingMessages` semantics, routing rules, scheduled delivery), see `wolverine-messaging-handlers`.

---

## Anti-Pattern: Starting a New Stream Without Returning IStartStream

Calling `session.Events.StartStream<T>(...)` directly does nothing useful in a Wolverine handler — the events are not persisted, no exception is thrown.

```csharp
// ❌ WRONG — events silently discarded
public static async Task<CreationResponse<Guid>> Handle(StartTrip cmd, IDocumentSession session)
{
    var tripId = Guid.CreateVersion7();
    session.Events.StartStream<Trip>(tripId, new TripStarted(...));   // not persisted
    return new CreationResponse<Guid>($"/api/trips/{tripId}", tripId);
}

// ✅ CORRECT — IStartStream returned, Wolverine intercepts
public static (CreationResponse<Guid>, IStartStream) Handle(StartTrip cmd, TimeProvider time)
{
    var tripId = Guid.CreateVersion7();
    var stream = MartenOps.StartStream<Trip>(tripId,
        new TripStarted(tripId, cmd.DriverId, cmd.RiderId, cmd.StartLocation, time.GetUtcNow()));
    return (new CreationResponse<Guid>($"/api/trips/{tripId}", tripId), stream);
}
```

**Why direct calls fail.** Wolverine's persistence interception runs on the *return value* of the handler. `IStartStream` is the marker type Wolverine recognizes; `session.Events.StartStream` returns `void` (or the stream object, depending on overload), which Wolverine cannot intercept. The session never gets `SaveChangesAsync` called on it because Wolverine's transactional middleware never sees a reason to commit.

**Polecat services use the identical pattern with `PolecatOps`:**

```csharp
public static (CreationResponse<Guid>, IStartStream) Handle(...)
{
    var paymentId = Guid.CreateVersion7();
    var stream = PolecatOps.StartStream<Payment>(paymentId, new PaymentInitiated(...));
    return (new CreationResponse<Guid>(...), stream);
}
```

This anti-pattern lives in the general skill (rather than HTTP or messaging) because it applies to handlers regardless of trigger — the failure mode is the same whether the handler runs from HTTP, an inbound message, or a gRPC call.

---

## Service Registrations

Wolverine's codegen relies on concrete registrations to emit direct constructor calls. Lambda factories (`services.AddScoped<IFoo>(sp => new Foo(...))`) force runtime service location and prevent pipeline optimization. Prefer `services.AddScoped<IFoo, Foo>()`. When the lambda form is unavoidable (Refit proxies, factory-only third-party registrations), allow-list the specific type: `opts.CodeGeneration.AlwaysUseServiceLocationFor<IRefitClient>()`.

For the full IoC optimization story — `ServiceLocationPolicy` modes, code pre-generation, lifetime guidance, Lamar fallback — see ai-skills `wolverine-handlers-ioc-and-service-optimization`.

---

## Logger Convention

Inject `ILogger`, not `ILogger<T>` — Wolverine already tags log output with handler type context. (Reinforced in `csharp-coding-standards`; covered upstream in ai-skills `wolverine-handlers-fundamentals`.)

---

## Diagnosing Handler Issues

Three commands to know. Run from the relevant service's directory.

```bash
# Preview the generated handler adapter code. First stop for any "not behaving as expected" symptom.
dotnet run -- wolverine-diagnostics codegen-preview --handler AcceptOffer

# List all routing rules. Use when an integration message isn't reaching consumers.
dotnet run -- wolverine-diagnostics describe-routing --all

# Show routing for a specific type. Use when a tracked.Sent assertion returns 0.
dotnet run -- wolverine-diagnostics describe-routing "CritterCab.Dispatch.Integration.OfferAccepted"
```

| Symptom | First command |
|---|---|
| Handler runs but events not persisted | `codegen-preview --handler T` (look for `IStartStream` interception) |
| `tracked.Sent.MessagesOf<T>()` returns 0 | `describe-routing "<TypeName>"` |
| `IDocumentSession` not injected | `codegen-preview --handler T` (look for `SessionVariableSource`) |
| Service-location warnings at startup | `describe` (full app-config dump) |

For full CLI coverage, see `cli-jasperfx` (Phase 2).

---

## See also

**Upstream** — load these first if unfamiliar:

- `csharp-coding-standards` — sealed records, validators-as-nested-classes, `TimeProvider`, modern guard clauses.
- `vertical-slice-organization` — file placement, single-file colocation of command + validator + handler.
- `domain-event-conventions` — slim domain events vs. rich integration events; the `Integration/` namespace pattern this skill assumes.

**Sibling skills** — load alongside this one based on what triggers the handler:

- `wolverine-http-handlers` — HTTP endpoint patterns: route binding, mixed route+body, `[EmptyResponse]`, concrete return types vs `IResult`, OpenAPI inference, the bare-event-return footgun.
- `wolverine-messaging-handlers` — message-bus patterns: routing rules required (the `OutgoingMessages`-without-routing footgun), transactional outbox semantics, `InvokeAsync` vs `PublishAsync` vs `ScheduleAsync` decision matrix.
- `wolverine-grpc-handlers` (Phase 3) — gRPC unary and server-streaming handler shapes.
- `wolverine-grpc-bidirectional-handlers` (Phase 4) — gRPC client-streaming and bidirectional handler shapes.

**Downstream** — natural follow-ups:

- `marten-aggregates` — aggregate design and stream identity (Phase 2).
- `marten-wolverine-aggregates` — `[WriteAggregate]`, `MartenOps.StartStream`, multi-stream handlers (Phase 2).
- `dynamic-consistency-boundary` — DCB-tagged event writes and decision queries (Phase 2).
- `wolverine-sagas` — saga state machines, timeouts, `bus.ScheduleAsync` (Phase 4).
- `polecat-event-sourcing` — `PolecatOps.StartStream` (Phase 4).
- `cli-jasperfx` — full CLI surface for diagnostics (Phase 2).

**Upstream (ai-skills)** — generic Wolverine handler mechanics this skill defers to:

- `wolverine-handlers-fundamentals` — generic handler shape, discovery, return-type reference, Logger convention.
- `wolverine-handlers-pure-functions` — decider pattern, A-Frame, why handlers are pure.
- `wolverine-handlers-a-frame-architecture` — infrastructure-at-edges principle.
- `wolverine-handlers-railway-programming` — `Validate` / `ProblemDetails` pipeline.
- `wolverine-handlers-declarative-persistence` — `[Entity]`, `[WriteAggregate]`, `[ReadAggregate]`.
- `wolverine-handlers-middleware` — full lifecycle, `OnException`, `MiddlewareScoping`, custom policies.
- `wolverine-handlers-ioc-and-service-optimization` — `ServiceLocationPolicy`, codegen modes, pre-generation, Lamar fallback.
- `marten-aggregate-handler-workflow` — full aggregate workflow reference.

ai-skills is installed at the user level via `npx skills add` (license required).

**External:**

- [Wolverine Handlers Guide](https://wolverinefx.net/guide/handlers/).
