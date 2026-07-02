---
name: wolverine-marten-automation
description: "Event-triggered automation handlers in CritterCab: static *Automation classes that react to a domain event already appended to a Marten stream (not an inbound command), the two independent registration prerequisites that make them fire, and the marker-interface union return-type pattern for multi-outcome decisions. Use when authoring or reviewing a *Automation class."
cluster: wolverine
tags: [wolverine, marten, automation, event-sourcing, decider-pattern]
---

# Wolverine + Marten Automation Handlers

> Event-triggered handlers that react to a domain event already committed to a Marten stream, plus the marker-interface pattern for their multi-outcome decisions.

## When to apply this skill

Use this skill when:

- Authoring a static handler that reacts to a domain event forwarded from a Marten stream — not an inbound HTTP request or bus message.
- Reviewing a PR that adds or modifies a class named `*Automation`.
- Deciding whether a handler's decision has 2+ mutually exclusive terminal outcomes that warrant a marker-interface return type.

Do NOT use this skill when:

- Authoring a command handler triggered by HTTP or an inbound message — see `wolverine-handlers` and `marten-wolverine-aggregates`. Automations and command handlers share the `[WriteAggregate]` mechanic but are registered and discovered differently (see below).

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

## Common pitfalls

- **Forgetting either registration prerequisite.** See § Registration above — the failure mode is silence, not an exception. If an automation isn't firing, check `UseFastEventForwarding` and `CustomizeHandlerDiscovery` before debugging the handler body.
- **Naming an automation `*Handler`.** Collides with the command-handler naming lane and — critically — falls outside `WithNameSuffix("Automation")`, so the class is silently never discovered.
- **Expecting the marker interface to change what gets persisted.** It doesn't. Persistence is entirely determined by the runtime type Wolverine sees at the return statement; the interface is a compile-time-only decision marker.

---

## See also

**Upstream** — load these first if unfamiliar:

- `wolverine-handlers` — general Wolverine handler shape and return-type orientation this skill assumes.
- `marten-wolverine-aggregates` — `[WriteAggregate]` mechanics this skill layers event-forwarding on top of.

**Downstream** — natural follow-ups:

- `testing-fundamentals` — unit-testing a pure `Handle` method's decision logic, including the marker-interface outcome shape.

**External:**

- ai-skills `wolverine-handlers-declarative-persistence` — generic `[Entity]` / `[WriteAggregate]` mechanics (license required, install via `npx skills add`).
