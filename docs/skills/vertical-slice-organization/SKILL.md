---
name: vertical-slice-organization
description: "How to organize files and folders within a CritterCab service: feature folders not technical layers, command + validator + handler colocated in one file, events alongside the slice. Use when adding a new feature, reviewing organization, or scaffolding within a service."
cluster: distributed-services
tags: [organization, vertical-slices, conventions, feature-folders, naming, structure]
---

# Vertical Slice Organization

How files and folders are organized **inside** a CritterCab service. The principle: when a developer or AI agent opens a folder, they should immediately understand **what the system does** — not what kinds of technical artifacts it contains.

This skill governs intra-service organization. Inter-service organization (the boundary at which one service ends and another begins) is governed by ADR-002 and `adding-a-service`. The two skills compose: each service's csproj is one vertical slab; this skill describes how the slab's contents are arranged.

## When to apply this skill

Use this skill when:

- Adding a new feature, command, query, or event to an existing service.
- Naming a file, class, or folder inside a service.
- Reviewing PRs that change a service's project layout.
- Scaffolding a service for the first time (use after `adding-a-service` produces the empty csproj).
- Spotting and refactoring a technical-layer folder structure that's drifted in.

This skill does NOT govern:

- Inter-service organization, csproj layout, solution structure — see `adding-a-service`.
- Domain event naming and shape — see `domain-event-conventions`.
- Cross-service contract files — see `protobuf-contracts`.

---

## Core Principle

**Organize by what the system does, not by what kinds of artifacts it contains.**

For VSA fundamentals — what vertical slices are, why Wolverine improves on MediatR, "why not Clean/Onion/Hexagonal," slice testing examples, and the broader decision guidance — see ai-skills `critterstack-arch-vertical-slice-fundamentals`. This Cab skill focuses on the project-specific *how* of organization within a CritterCab service.

### ✅ Feature-oriented organization

```
CritterCab.Trips/
  TripExecution/                 # ← Business capability
    StartTrip.cs                 # Command + Validator + Handler
    CompleteTrip.cs              # Command + Validator + Handler
    Trip.cs                      # Aggregate
    TripStarted.cs               # Domain event
    TripCompleted.cs             # Domain event
```

**What this tells you:** "This service handles trip execution."

### ❌ Technical-layer organization

```
CritterCab.Trips/
  Commands/                      # ← Technical layer
    StartTrip.cs
    CompleteTrip.cs
  Events/
    TripStarted.cs
    TripCompleted.cs
  Handlers/
    StartTripHandler.cs
    CompleteTripHandler.cs
  Aggregates/
    Trip.cs
```

**What this tells you:** "This service has commands, events, handlers, and aggregates." (What does it actually *do*? You have to read the files to know.)

There are no `Commands/`, `Events/`, `Handlers/`, `Validators/`, `Queries/`, `Aggregates/`, `Data/`, or `Entities/` folders anywhere in CritterCab.

---

## Folder Naming

Folders are named after **business capabilities**, not technical roles.

| ✅ Capability | ❌ Technical role |
|---|---|
| `TripExecution/` | `Commands/` |
| `OfferDispatch/` | `Handlers/` |
| `Onboarding/` | `Validators/` |
| `Telemetry/` | `Events/` |
| `SurgePolicy/` | `Queries/` |
| `Payments/` | `Data/`, `Entities/` |
| `Vetting/` | `Persistence/` |

For very small services, top-level files at the project root are also acceptable — feature folders are introduced when there are enough files to warrant grouping (rough rule of thumb: 5+ files cluster naturally into folders; 10+ requires it).

---

## File Naming

### The rule

**Each command, query, and aggregate gets one file named after the operation or domain concept it represents.** Validators and handlers for a command live in the same file as the command.

### File naming reference

| Type | File name | Class names in that file | Notes |
|---|---|---|---|
| Command | `{Operation}.cs` | `{Operation}` (record), `{Operation}Validator` (nested), `{Operation}Handler` (static) | All three colocated in one file. |
| Query | `{Query}.cs` | `{Query}` (record), `{Query}Handler` (static) | Validator usually unnecessary for queries. |
| Domain event | `{Event}.cs` | `{Event}` (record) | Separate file. Colocate a subscription handler in the same file if the event has one (see below). |
| Aggregate | `{Aggregate}.cs` | `{Aggregate}` (record) | Separate file. |
| Value object | `{Name}.cs` | `{Name}` (record) | Separate file, or colocated with its primary aggregate when only used there. |
| Saga | `{Saga}.cs` | `{Saga}` (record/class), supporting types | Separate file. May colocate state-transition events if tightly bound to the saga. |

### Why command + validator + handler in one file

The three are **1:1 by design**. The validator validates the command's shape; the handler implements the command's behavior. Splitting them across three files means three opens per feature change, three places to keep in sync, and three files that may get out of order in code review. Colocation makes the unit of change one file, and "what does `AcceptOffer` do" gets answered without navigation.

```csharp
// File: AcceptOffer.cs
namespace CritterCab.Dispatch;

public sealed record AcceptOffer(Guid OfferId, Guid DriverId)
{
    public class AcceptOfferValidator : AbstractValidator<AcceptOffer>
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
    public static OfferAccepted Handle(
        AcceptOffer command,
        RideOffer offer,
        TimeProvider time)
    {
        if (time.GetUtcNow() > offer.ExpiresAt)
            throw new InvalidOperationException("Offer has expired");

        return new OfferAccepted(offer.Id, command.DriverId, time.GetUtcNow());
    }
}
```

### Anti-pattern: type-grouped files

**DO NOT group multiple commands, events, or validators into files named by type.**

```csharp
// ❌ WRONG — DispatchCommands.cs
public sealed record RequestRide(...);
public sealed record CancelRideRequest(...);
public sealed record AcceptOffer(...);
public sealed record RejectOffer(...);
public sealed record ExpireOffer(...);
// ... 10+ records in one file
```

```csharp
// ❌ WRONG — DispatchEvents.cs
public sealed record RideRequested(...);
public sealed record OfferDispatched(...);
public sealed record OfferAccepted(...);
public sealed record OfferRejected(...);
public sealed record OfferExpired(...);
// ... 10+ records in one file
```

This breaks the vertical-slice principle (related types are scattered across multiple files instead of colocated), produces merge conflicts when multiple developers work on different features in the same BC, and obscures intent. The folder shows you `DispatchCommands.cs` and `DispatchEvents.cs` — you learn that the BC has commands and events, but not what it does.

---

## Domain Events

### One event per file

Domain events live in their own file by default — separate from the command that produces them, in the same feature folder.

```
TripExecution/
  StartTrip.cs                   # Command + Validator + Handler
  TripStarted.cs                 # Event (separate file)
  CompleteTrip.cs                # Command + Validator + Handler
  TripCompleted.cs               # Event (separate file)
```

Reasoning: events are immutable contracts, change less frequently than the commands that produce them, and may be consumed by multiple commands or projections within the same service. Keeping them in their own file makes the contract visible without scrolling past the command-and-handler implementation that produced this particular instance.

### Colocate when an event has a subscription handler

When a domain event has a Marten event-subscription handler (e.g., translating the domain event into an integration event published over ASB), colocate them in the same file:

```csharp
// File: TripCompleted.cs
namespace CritterCab.Trips;

// Domain event in the local stream
public sealed record TripCompleted(
    Guid TripId,
    DateTimeOffset CompletedAt);

// Subscription handler — translates to integration event
public static class TripCompletedSubscription
{
    public static async Task Handle(
        IEvent<TripCompleted> @event,
        Trip trip,
        IMessageBus bus,
        CancellationToken ct)
    {
        // Publish integration event (lives in CritterCab.Trips.Integration namespace)
        await bus.PublishAsync(new Integration.TripCompleted(
            @event.StreamId,
            trip.RiderId,
            trip.DriverId,
            trip.FareTotal,
            trip.DistanceMeters,
            @event.Data.CompletedAt));
    }
}
```

The subscription handler is tightly coupled to the event (it's the cross-service translation point); colocation makes the integration boundary explicit at the file level.

For the slim-vs-rich event distinction underlying this pattern, see `domain-event-conventions` § Slim Domain Events vs. Rich Integration Events.

---

## Slices Stay Within One Service

A slice in CritterCab is a feature inside a single service: command + validator + handler + events + view. **Slices never cross service boundaries.** A slice that produces an event consumed by another service is still one slice — the boundary at which other services consume the event is a *contract* (proto file or ASB integration message), not part of the slice.

This is structurally enforced. ADR-002 forbids project references between services. A slice that "spans" two services is two slices, one in each service, communicating via a wire-level contract. The wire contract lives in `/protos/` (per ADR-009) or as an integration event class in the service's `Integration/` namespace — see `domain-event-conventions` § Naming Collisions Between Domain and Integration Events.

The practical effect: when designing a new slice, the question "which service does this go in?" is answered first (often during Event Modeling), and the question "where in the service does it go?" is answered second (using this skill). The two questions don't blur.

---

## Shared Types Within a Service

When a value object, enum, or domain constant is used by 3+ slices within the same service, extract it to a `Shared/` folder at the project root:

```
CritterCab.Trips/
  Shared/
    Fare.cs                      # Used by FareCalculation, TipAdjustment, FareDispute
    TripStatus.cs                # Used by Trip aggregate + 3 read-side projections
  TripExecution/
    StartTrip.cs
    Trip.cs
  Cancellation/
    CancelTrip.cs
  Dispute/
    OpenDispute.cs
```

Rules:

- **`Shared/` is for service-internal sharing.** It does not cross service boundaries (cross-service shared types live in `/protos/common/` per `protobuf-contracts`).
- **Don't preemptively create `Shared/`.** Wait until duplication actually appears. Three+ references is the threshold; one or two stays colocated with the primary use site.
- **`Shared/` is named for its purpose, not for its technical role.** It's the one acceptable structural-purpose folder name; even here, prefer a more specific name (e.g., `Money/`, `Identifiers/`) when the contents allow.

---

## No Migration-Suffix Naming

**Don't include persistence or pattern suffixes in file or class names.** `*ES` (event sourcing), `*V2`, `*New`, `*Refactor`, `*Polecat` are all wrong.

```
❌ CreateTripES.cs              → ✅ CreateTrip.cs
❌ CreateTripPolecat.cs         → ✅ CreateTrip.cs  (the persistence is internal)
❌ AcceptOfferV2.cs             → ✅ AcceptOffer.cs (and reserve V2 for protocol versioning, not file naming)
```

The rationale: file names should describe the business operation, not the persistence strategy or migration history. If a service migrates from one store to another, the file names stay stable; only the handler internals change.

When migration runs old and new implementations in parallel, the temporary suffix is acceptable for the duration of the migration and gets cleaned up before the milestone closes. Don't let migration artifacts persist past the migration.

---

## What "Acceptable Technical Folder" Means

The principle is "no technical-layer folders," but a few non-feature folders are legitimate and named for their purpose:

| Folder | When it's appropriate | Naming guidance |
|---|---|---|
| `Shared/` | 3+ slices reference the same value object or constant | See above |
| `Migrations/` | EF Core migrations (rare in CritterCab — only services using EF Core for read models) | Standard EF Core convention |
| `Integration/` | Cross-service integration event classes (per `domain-event-conventions`) | Already documented; folder name allowed |

Generic technical names are still wrong even when the contents are mixed. `Data/`, `Entities/`, `Models/`, `Persistence/` — these tell you nothing about what they contain. If a folder genuinely needs to exist, it gets a name that signals **why**, not **what**.

---

## AI Agent Defaults — Counter-Pressure

AI agents (and humans new to vertical-slice architecture) default to technical organization without explicit guidance:

```
Commands/
Events/
Queries/
Handlers/
Models/
```

This is the most common pattern in .NET tutorials, blog posts, and training materials. It's wrong for CritterCab. When scaffolding a new service or feature, ignore the layered-architecture instinct and follow this skill's conventions.

If a generated folder structure includes any of `Commands/`, `Events/`, `Queries/`, `Handlers/`, `Validators/`, `Aggregates/`, `Data/`, `Entities/`, `Models/`, or `Persistence/` — that structure is wrong. Reorganize before proceeding.

---

## Worked Example: Trips Service

Putting it together for a hypothetical mid-sized Trips service:

```
CritterCab.Trips/
  CritterCab.Trips.csproj
  Program.cs
  README.md

  Shared/
    Fare.cs                      # Money value object specific to fares
    TripStatus.cs                # Enum used by aggregate + projections

  TripExecution/                 # ← Business capability
    StartTrip.cs                 # Command + Validator + Handler
    CompleteTrip.cs              # Command + Validator + Handler
    Trip.cs                      # Aggregate
    TripStarted.cs               # Domain event
    TripCompleted.cs             # Domain event + subscription handler (publishes integration event)

  Cancellation/                  # ← Business capability
    CancelTrip.cs                # Command + Validator + Handler
    TripCancelled.cs             # Domain event + subscription handler
    CancellationReason.cs        # Enum used only here

  RouteTracking/                 # ← Business capability
    RecordWaypoint.cs            # Command + Handler
    Waypoint.cs                  # Value object used by Trip aggregate
    WaypointRecorded.cs          # Domain event

  Queries/
    GetTrip.cs                   # Query + Handler
    GetTripsForRider.cs          # Query + Handler
    GetActiveTripForDriver.cs    # Query + Handler

  Integration/
    TripCompleted.cs             # Integration event class (rich payload)
    TripCancelled.cs             # Integration event class
```

**Wait — `Queries/`?**

That's the only intentionally-bent rule and worth calling out. Queries don't fit a single business capability; a query like `GetTrip` is read-only and may be invoked from many UI surfaces and many services. Grouping queries in a `Queries/` folder is acceptable when there are 3+ queries that don't naturally cluster into a feature — though if a query is tightly coupled to a single feature (e.g., `GetActiveCancellation` for the Cancellation feature), it stays in that feature folder.

For services with very few queries (1–2), they can sit at the project root or alongside the most-related feature. The rule's underlying intent is "make it easy to navigate"; rigid application to a 2-query service produces a folder with 2 files, which doesn't earn its keep.

---

## Common Pitfalls

- **Defaulting to `Commands/Events/Handlers/`** because it's the layered-architecture pattern. Wrong for CritterCab; reorganize.
- **Type-grouped files** like `DispatchCommands.cs` containing 10+ records. Each command gets its own file; the validator and handler colocate in the same file as the command.
- **Splitting validator into its own file** for Marten-based services. The single-file colocation is the canonical pattern. (The CritterSupply three-file pattern for EF Core BCs does NOT carry over — Cab's primary stores are Marten and Polecat.)
- **Preemptively creating `Shared/`** before duplication appears. Wait for 3+ references to the same type before extracting.
- **Migration-suffixed names** that persist past the migration. Clean up before the milestone closes.
- **Treating `Integration/` as where to put cross-service "shared" code.** It's only for outbound integration event classes (per `domain-event-conventions`). It is not a synonym for `Shared/`.
- **Forgetting to apply the "what does this folder do?" test.** If a folder name doesn't tell you what business capability lives there, the name is wrong.

---

## See also

**Upstream** — generic VSA fundamentals this skill builds on. ai-skills (license required, install via `npx skills add`):

- `critterstack-arch-vertical-slice-fundamentals` (primary) — VSA principle, Wolverine-vs-MediatR comparison, complete vertical slice example, aggregate file convention, "why not Clean/Onion/Hexagonal" rationale, slice testing examples, query slices, message handler slices, decision guidance. Cab's skill applies these fundamentals with project-specific naming conventions, file colocation rules, anti-patterns, and the AI-agent counter-pressure framing.

**Prerequisites** — Cab-internal skills to load first if unfamiliar:

- `csharp-coding-standards` — sealed records, init-only properties, validators-as-nested-classes pattern.
- `domain-event-conventions` — naming, file placement at the project root level (this skill aligns with it).
- `adding-a-service` — establishes the project skeleton this skill organizes within.

**Downstream** — natural follow-ups:

- `wolverine-handlers` — handler shapes that this organization supports (Phase 2).
- `marten-aggregates` — the aggregate file referenced in feature folders (Phase 2).
- `wolverine-sagas` — how saga files fit the convention (Phase 4).

**External:**

- ADR-002 in [`docs/decisions/`](../../decisions/) — service-per-bounded-context (the inter-service boundary that complements this skill's intra-service organization).
- ADR-009 in [`docs/decisions/`](../../decisions/) — protobuf contracts as first-class artifacts (governs cross-service shared types).
