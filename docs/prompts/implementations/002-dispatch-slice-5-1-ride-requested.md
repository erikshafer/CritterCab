# Prompt 002 — Implement Slice 5.1: RideRequested

| Field | Value |
|---|---|
| **Status** | Complete (2026-05-07). All 4 tests pass (1 smoke + 3 slice tests). |
| **Authored** | 2026-05-07 |
| **Target artifacts** | `src/CritterCab.Dispatch/RideRequesting/` (command, handler, aggregate, event, projections), `tests/CritterCab.Dispatch.Tests/RideRequesting/` |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §5.1; [`docs/narratives/001-rider-books-a-ride.md`](../../narratives/001-rider-books-a-ride.md) Moment 1 |
| **Workflow position** | First domain-logic implementation; exercises the full SDD loop on real code |

---

## Framing

Slice 5.1 is the simplest command slice with the cleanest narrative coverage (narrative 001 Moment 1). This is the first real vertical slice end-to-end: Wolverine.HTTP endpoint receives the rider's request, creates a Marten event-sourced `RideRequest` aggregate with a `RideRequested` event, projects to `ActiveRequestsByRider` and `RequestTimeline`.

---

## Goal

Implement the `RideRequested` slice: HTTP endpoint, command, handler, aggregate, event, two projections, and Alba integration tests with Testcontainers.

---

## Out of scope

- Fare quoting (slice 5.2) — downstream automation triggered by `RideRequested`.
- Candidate selection (slice 5.3), offer fan-out (5.4), acceptance (5.5).
- Terminal events (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`) — projections will handle them when those slices land.
- gRPC service definitions.
- Authentication/authorization — `riderId` is passed in the request body for now.

---

## Retrospective

### What happened

First vertical slice implemented end-to-end. The `POST /api/rides/request` endpoint creates a Marten event-sourced `RideRequest` aggregate, emits `RideRequested`, and projects to `ActiveRequestsByRider` (one-active-request-per-rider guard) and `RequestTimeline`. Three Alba integration tests with Testcontainers verify the happy path, duplicate-rider rejection, and concurrent riders.

### File layout

```
src/CritterCab.Dispatch/
  Shared/
    Location.cs              — value object
    VehicleClass.cs          — enum
  RideRequesting/
    SubmitRideRequest.cs     — command + handler + HTTP endpoint
    RideRequest.cs           — aggregate (self-aggregating, Create from IEvent<RideRequested>)
    RideRequested.cs         — domain event
    ActiveRideRequest.cs     — projection document + MultiStreamProjection (inline)
    RequestTimeline.cs       — projection document + SingleStreamProjection (inline)
tests/CritterCab.Dispatch.Tests/
  DispatchTestFixture.cs     — Testcontainers + Alba shared fixture
  RideRequesting/
    SubmitRideRequestTests.cs — 3 integration tests
```

### Skill-file gaps surfaced

1. **`IEvent<T>` moved from `Marten.Events` to `JasperFx.Events`.** Marten 8.x extracted event interfaces to the JasperFx.Events package. The `marten-wolverine-aggregates` and `marten-projections` skills show `using Marten.Events;` but the actual namespace is `JasperFx.Events`.

2. **`SingleStreamProjection<T>` is `SingleStreamProjection<TDoc, TId>` in `Marten.Events.Aggregation`.** The skills show a single type parameter; the actual API requires two. Also, `SingleStreamProjection` is in `Marten.Events.Aggregation`, not `Marten.Events.Projections`. And `MultiStreamProjection` is in `Marten.Events.Projections`, not `Marten.Events.Aggregation` — the namespaces are counterintuitive.

3. **`ProjectionLifecycle` moved from `Marten.Events.Projections` to `JasperFx.Events.Projections`.** Same JasperFx extraction.

4. **`AddWolverineHttp()` is required before `MapWolverineEndpoints()`.** The `service-bootstrap` skill doesn't mention this registration step. Without it, `MapWolverineEndpoints` throws at startup.

5. **`TimeProvider` must be registered in DI.** Wolverine HTTP handlers that inject `TimeProvider` fail at runtime if it's not registered. `builder.Services.AddSingleton(TimeProvider.System)` is needed.

### Design decisions

- **`IDocumentSession` over `MartenOps.StartStream` return-value pattern.** The handler has conditional read-then-write logic (check `ActiveRequestsByRider` → start stream). The `MartenOps.StartStream` tuple pattern doesn't support conditional responses cleanly. Direct session usage with explicit `SaveChangesAsync` is the pragmatic choice for this handler shape.

- **Both projections are inline.** `ActiveRequestsByRider` must be inline because the handler queries it in the same request that creates the stream. If it were async, the guard would race. `RequestTimeline` is inline for consistency; it could be async later if needed.

- **`Shared/` created from the start.** Workshop makes clear `Location` and `VehicleClass` are used across 5+ slices. The 3-reference threshold from `vertical-slice-organization` is met by design.

### What's next

The D → B → C sequence is complete. Next decision: **A** (Trips workshop) or **E** (more Dispatch narratives).
