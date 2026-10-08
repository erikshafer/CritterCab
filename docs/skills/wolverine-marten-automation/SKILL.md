---
name: wolverine-marten-automation
description: "Work a service starts on its own rather than in response to a caller. Two shapes: event-triggered automation handlers (static *Automation classes that react to a domain event already appended to a Marten stream, the two independent registration prerequisites that make them fire, and the marker-interface union return-type pattern for multi-outcome decisions), and timer-driven recurring work (a logic-free BackgroundService shell invoking a plain *Handler through a per-tick scoped IMessageBus). Use when authoring or reviewing a *Automation class or any periodic sweep."
cluster: wolverine
tags: [wolverine, marten, automation, event-sourcing, decider-pattern, recurring-work, background-service]
---

# Wolverine + Marten Automation Handlers

> Work a service starts on its own. Mostly event-triggered handlers that react to a domain event already committed to a Marten stream, plus the marker-interface pattern for their multi-outcome decisions. One addendum covers the other trigger, a timer, whose shape is different: it is not event-forwarded and its handler is not an `*Automation`.

## When to apply this skill

Use this skill when:

- Authoring a static handler that reacts to a domain event forwarded from a Marten stream — not an inbound HTTP request or bus message.
- Reviewing a PR that adds or modifies a class named `*Automation`.
- Deciding whether a handler's decision has 2+ mutually exclusive terminal outcomes that warrant a marker-interface return type.
- Adding recurring, timer-driven work (a periodic sweep or cleanup) — see § Recurring Work (Timer-Driven). No event triggers it, so the event-forwarding and `*Automation` discovery sections do not apply to it.

Do NOT use this skill when:

- Authoring a command handler triggered by HTTP or an inbound message — see `wolverine-handlers` and `marten-wolverine-aggregates`. Automations and command handlers share the `[WriteAggregate]` mechanic but are registered and discovered differently (see below).
- Scheduling a one-shot delayed message — that is `bus.ScheduleAsync`, see `wolverine-messaging-handlers` § ScheduleAsync for Delayed Delivery. It does not recur.

## Prerequisites

- `wolverine-handlers` — general Wolverine handler shape (static class, `Handle` as happy path, return-type orientation) this skill assumes and extends with a fourth trigger shape.
- `marten-wolverine-aggregates` — `[WriteAggregate(nameof(...))]` mechanics this skill layers event-forwarding on top of.

---

## Registration: two prerequisites, not one

An automation handler does not fire unless **both** of the following are configured. Missing either one produces no exception and no log line — the automation simply never runs, which makes this the single easiest automation bug to lose an afternoon to.

```csharp
// src/CritterCab.Dispatch/Program.cs
builder.Services.AddMarten(opts =>
{
    // ...
})
.IntegrateWithWolverine(integration =>
{
    // 1. Forward every appended Marten stream event to any Wolverine
    //    handler capable of handling it. Without this, automations never
    //    see the events they're meant to react to.
    integration.UseFastEventForwarding = true;
})
.UseLightweightSessions();

builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "Dispatch";

    // 2. Wolverine's default handler discovery looks for *Handler-suffixed
    //    classes. *Automation classes are invisible to it without this —
    //    the class compiles, the event forwards, and nothing happens.
    opts.Discovery.CustomizeHandlerDiscovery(d => d.Includes.WithNameSuffix("Automation"));
});
```

Both lines are independently necessary and independently silent when missing:

| Missing | Symptom |
|---|---|
| `UseFastEventForwarding = true` | The event is never forwarded to Wolverine at all — no handler in the app sees it, automation or otherwise. |
| `CustomizeHandlerDiscovery(...WithNameSuffix("Automation"))` | The event forwards fine, but Wolverine's handler discovery never registered the `*Automation` class as a handler for it — the class is dead code from Wolverine's perspective. |

If an automation "isn't firing," check both before anything else.

---

## Event-Triggered Automation Handler Shape

```csharp
// src/CritterCab.Dispatch/CandidateSelection/CandidateSelectionAutomation.cs
public static class CandidateSelectionAutomation
{
    public static async Task<ICandidateSelectionOutcome> Handle(
        FareQuoted @event,
        [WriteAggregate(nameof(FareQuoted.RideRequestId))] RideRequest rideRequest,
        INearbyAvailableDriversSource nearbyDrivers,
        DispatchPolicySnapshot policy,
        TimeProvider time,
        CancellationToken ct)
    {
        // ... query nearby drivers, decide the outcome ...
        return new CandidatesSelected(/* ... */);
        // or: return new NoCandidatesAvailable(/* ... */);
    }
}
```

Three things to internalize:

- **Naming: `<X>Automation`, not `<X>Handler`.** This is not cosmetic — it is exactly the suffix `CustomizeHandlerDiscovery` above matches on. Naming an automation `*Handler` makes it invisible to discovery (see § Common pitfalls).
- **`[WriteAggregate(nameof(TriggerEvent.StreamIdProperty))]` resolves the aggregate by a named property on the *triggering event*, not necessarily the stream's first event.** `CandidateSelectionAutomation` keys off `FareQuoted` — the **second** event appended to the `RideRequest` stream (`RideRequested` is first). The attribute works identically either way because it resolves the stream ID from the property named, not from stream position.
- **No `Validate`/`Before` method appears in either real example** (`FareQuoteAutomation`, `CandidateSelectionAutomation`). Automations react to a domain event *already committed* to the stream — there is no "reject the command" precondition step the way an inbound command handler has one against not-yet-applied input. Whether this is a deliberate convention or simply hasn't been needed yet is an open question; no design session has settled it. If an automation surfaces a genuine need to short-circuit before `Handle` runs, treat that as a new design question, not a precedent to copy silently.

Both current examples in the codebase:

| Automation | Trigger event | Aggregate keyed by | Outcome interface |
|---|---|---|---|
| `FareQuoteAutomation` | `RideRequested` (first stream event) | `RideRequest` | `IFareQuoteOutcome` |
| `CandidateSelectionAutomation` | `FareQuoted` (second stream event) | `RideRequest` | `ICandidateSelectionOutcome` |

---

## Marker-Interface Union Return Type

```csharp
// src/CritterCab.Dispatch/CandidateSelection/ICandidateSelectionOutcome.cs
public interface ICandidateSelectionOutcome;

// Implemented by exactly the automation's terminal outcomes:
public sealed record CandidatesSelected(/* ... */) : ICandidateSelectionOutcome;
public sealed record NoCandidatesAvailable(/* ... */) : ICandidateSelectionOutcome;
```

- **The pattern:** an automation whose `Handle` method has 2+ mutually exclusive terminal outcomes returns a shared marker interface (`public interface IXOutcome;` — no members) implemented by each concrete outcome event.
- **Mechanically inert.** Wolverine's `DetermineEventCaptureHandling` treats any non-`IEnumerable<object>` return as a single-event append of the *runtime* type. The marker interface has **zero effect on what gets persisted** — Wolverine never inspects it. It exists purely as compile-time documentation of the decision's possible outcomes, readable straight off the method signature (`Task<ICandidateSelectionOutcome>` tells a reader "this is a 2+-way decision" before they read a line of the body).
- **When to reach for it:** an automation with 2+ mutually exclusive terminal events representing a genuine decision node in the event model (a Klefter decision-event pair, in CritterCab's event-modeling vocabulary) — not a single-outcome automation, which should just return its one event type directly per `wolverine-handlers` § Handler Return Types.

Both real instances:

| Marker interface | Outcomes |
|---|---|
| `IFareQuoteOutcome` | `FareQuoted` \| `FareQuoteFailed` |
| `ICandidateSelectionOutcome` | `CandidatesSelected` \| `NoCandidatesAvailable` |

---

## Recurring Work (Timer-Driven)

Some work has no triggering event: it is due because time passed. Telemetry's `LastKnownPosition` eviction sweep is the shipped instance — every 30 seconds, delete positions older than three missed heartbeats. This is **not** an event-triggered automation. Nothing is forwarded from a Marten stream, neither registration prerequisite above is involved, and the handler is named `*Handler`, not `*Automation`.

**Wolverine has no first-class recurring-message primitive.** The natural first guesses are both wrong: `bus.ScheduleAsync` is one-shot delayed delivery, and `opts.PublishMessage<T>().ToLocalQueue()` is routing configuration, not a scheduler. The idiom — the one Wolverine's own internals use for their heartbeat — is a plain .NET `BackgroundService` that loops on `Task.Delay` and calls `IMessageBus` each tick. Wolverine contributes only the bus call inside the loop.

### Split the timer from the work

Two pieces, with every line of testable behavior in the second:

| Piece | Reference | Holds |
|---|---|---|
| Timer shell | `src/CritterCab.Telemetry/LastKnownPosition/LastKnownPositionEvictionService.cs` | The loop, the interval, the scope, error containment. No domain logic. |
| Handler | `EvictStalePositionsHandler` in `src/CritterCab.Telemetry/LastKnownPosition/EvictStalePositions.cs` | Everything else: reading the policy, computing the threshold, the delete. |

```csharp
// src/CritterCab.Telemetry/LastKnownPosition/LastKnownPositionEvictionService.cs
public sealed class LastKnownPositionEvictionService(
    IServiceScopeFactory scopeFactory,
    ILogger<LastKnownPositionEvictionService> logger)
    : BackgroundService
{
    public static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Delay first: nothing can be stale at host start.
                await Task.Delay(SweepInterval, stoppingToken);

                await using var scope = scopeFactory.CreateAsyncScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                await bus.InvokeAsync(new EvictStalePositions(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return; // normal shutdown, not a failure
            }
            catch (Exception e)
            {
                logger.LogError(e, "LastKnownPosition eviction sweep failed; retrying next tick.");
            }
        }
    }
}
```

```csharp
// src/CritterCab.Telemetry/LastKnownPosition/EvictStalePositions.cs
public sealed record EvictStalePositions;

public static class EvictStalePositionsHandler
{
    public static async Task Handle(
        EvictStalePositions command,
        IDocumentSession session,
        TimeProvider time,
        CancellationToken ct)
    {
        // ... read the policy, compute the threshold, HardDeleteWhere, SaveChangesAsync ...
    }
}
```

- **Take `IServiceScopeFactory`, never `IMessageBus`, and open a scope per tick.** A hosted service is a singleton; Wolverine registers `IMessageBus` as scoped. Injecting the bus into the constructor fails host construction under DI scope validation (`CallSiteValidator`) — confirmed by mutation in PR #45. The per-tick scope also gives the handler a fresh `IDocumentSession`.
- **`InvokeAsync`, awaited.** Inline execution means a slow sweep applies back-pressure to its own timer instead of overlapping the next tick. This is the sanctioned in-process exception to `wolverine-messaging-handlers` § Anti-Pattern: bus.InvokeAsync for Fire-and-Forget Work — that caution is about calling `InvokeAsync` from inside another handler, where it nests a second transaction. A timer loop is not a handler and has no outer transaction.
- **Delay before the first tick.** Nothing can be due at host start.
- **Return on cancellation; log and continue on anything else.** A failed sweep must never crash the host — the next tick retries.
- **The message is parameterless.** The sweep's only inputs are the clock and the policy, both resolved handler-side, so the message carries nothing.
- **The handler is `*Handler`, not `*Automation`.** Wolverine's default suffix discovers it. `*Automation` is reserved for handlers reacting to a domain event already on a Marten stream; naming a timer-raised handler `*Automation` would blur that line.
- **The sweep interval is not the staleness threshold.** `SweepInterval` is how often to look; the threshold (3 × `HeartbeatIntervalSeconds`) is read from the policy inside the handler.

### Registration

```csharp
// src/CritterCab.Telemetry/Program.cs — inside the `if (!string.IsNullOrEmpty(connectionString))` Marten guard
builder.Services.AddHostedService<LastKnownPositionEvictionService>();
```

Registered inside the Marten guard on purpose: the handler resolves an `IDocumentSession`, so without a document store there is nothing to sweep and the timer would only log a failure every interval.

### Testing

The shell is untested by design — there is nothing in it to assert that would not be asserting `Task.Delay`. Tests remove the hosted service, so a background tick cannot race a test's own invocation, and invoke the message directly:

```csharp
// tests/CritterCab.Telemetry.Tests/TelemetryTestFixture.cs
builder.ConfigureTestServices(services =>
{
    var timer = services.FirstOrDefault(
        d => d.ImplementationType == typeof(LastKnownPositionEvictionService));

    if (timer is not null)
        services.Remove(timer);
});
```

`ConfigureTestServices`, not `ConfigureServices`: it runs after `Program.cs`'s own registrations, which is the only order in which removing one works. The fixture's `InvokeAsync(object message)` helper then sends `new EvictStalePositions()` through a scoped `IMessageBus`, exactly as the shell does.

---

## Common pitfalls

- **Forgetting either registration prerequisite.** See § Registration above — the failure mode is silence, not an exception. If an automation isn't firing, check `UseFastEventForwarding` and `CustomizeHandlerDiscovery` before debugging the handler body.
- **Naming an automation `*Handler`.** Collides with the command-handler naming lane and — critically — falls outside `WithNameSuffix("Automation")`, so the class is silently never discovered.
- **Expecting the marker interface to change what gets persisted.** It doesn't. Persistence is entirely determined by the runtime type Wolverine sees at the return statement; the interface is a compile-time-only decision marker.
- **Reaching for `ScheduleAsync` or a local queue to make work recur.** Neither recurs. Use a `BackgroundService` shell — see § Recurring Work (Timer-Driven).
- **Injecting `IMessageBus` into a hosted service.** Singleton consuming scoped; the host fails to build. Take `IServiceScopeFactory` and create a scope per tick.
- **Putting logic in the timer shell.** It is untested by design, so anything placed there is untested behavior. Keep it in the handler.

---

## See also

**Upstream** — load these first if unfamiliar:

- `wolverine-handlers` — general Wolverine handler shape and return-type orientation this skill assumes.
- `marten-wolverine-aggregates` — `[WriteAggregate]` mechanics this skill layers event-forwarding on top of; § Plain Documents (Not Event-Sourced) for the `HardDeleteWhere` the eviction sweep performs.
- `wolverine-messaging-handlers` — the `bus.*` decision matrix and the `InvokeAsync` caution the timer shell is the carve-out from.

**Downstream** — natural follow-ups:

- `testing-fundamentals` — unit-testing a pure `Handle` method's decision logic, including the marker-interface outcome shape.

**External:**

- ai-skills `wolverine-handlers-declarative-persistence` — generic `[Entity]` / `[WriteAggregate]` mechanics (license required, install via `npx skills add`).
