# Retrospective — Implement Slice 5.1: RideRequested

## Metadata

- **Triggering prompt:** [`docs/prompts/implementations/002-dispatch-slice-5-1-ride-requested.md`](../../prompts/implementations/002-dispatch-slice-5-1-ride-requested.md)
- **Status:** Complete
- **Date authored:** 2026-05-07
- **Output artifacts:**
  - `src/CritterCab.Dispatch/Shared/Location.cs` — value object
  - `src/CritterCab.Dispatch/Shared/VehicleClass.cs` — enum
  - `src/CritterCab.Dispatch/RideRequesting/SubmitRideRequest.cs` — command + handler + HTTP endpoint
  - `src/CritterCab.Dispatch/RideRequesting/RideRequest.cs` — aggregate (self-aggregating, Create from `IEvent<RideRequested>`)
  - `src/CritterCab.Dispatch/RideRequesting/RideRequested.cs` — domain event
  - `src/CritterCab.Dispatch/RideRequesting/ActiveRideRequest.cs` — projection document + `MultiStreamProjection` (inline)
  - `src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs` — projection document + `SingleStreamProjection` (inline)
  - `tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs` — Testcontainers + Alba shared fixture
  - `tests/CritterCab.Dispatch.Tests/RideRequesting/SubmitRideRequestTests.cs` — 3 integration tests
- **Outcome:** First vertical slice implemented end-to-end. All 4 tests pass (1 smoke + 3 slice tests). First real domain logic in the repository.

---

## Framing

Slice 5.1 is the simplest command slice with the cleanest narrative coverage (narrative 001 Moment 1). This was the first real vertical slice end-to-end: Wolverine.HTTP endpoint receives the rider's request, creates a Marten event-sourced `RideRequest` aggregate with a `RideRequested` event, projects to `ActiveRequestsByRider` and `RequestTimeline`.

---

## Outcome summary

The `POST /api/rides/request` endpoint creates a Marten event-sourced `RideRequest` aggregate, emits `RideRequested`, and projects to `ActiveRequestsByRider` (one-active-request-per-rider guard) and `RequestTimeline`. Three Alba integration tests with Testcontainers verify the happy path, duplicate-rider rejection, and concurrent riders.

---

## What worked

- **Narrative-to-code alignment.** Narrative 001 Moment 1 mapped cleanly to the slice implementation. No divergence detected.
- **Workshop slice spec as implementation guide.** The Event Model's GWT scenarios for slice 5.1 translated directly into the three integration tests.

---

## What was harder than expected

- **Marten/JasperFx namespace extraction.** Multiple types have moved from `Marten.*` namespaces to `JasperFx.*` namespaces in Marten 8.x. The skills documented the old namespaces.

---

## Skill-file gaps surfaced

1. **`IEvent<T>` moved from `Marten.Events` to `JasperFx.Events`.** Marten 8.x extracted event interfaces to the JasperFx.Events package. The `marten-wolverine-aggregates` and `marten-projections` skills show `using Marten.Events;` but the actual namespace is `JasperFx.Events`.
2. **`SingleStreamProjection<T>` is `SingleStreamProjection<TDoc, TId>` in `Marten.Events.Aggregation`.** The skills show a single type parameter; the actual API requires two. Also, `SingleStreamProjection` is in `Marten.Events.Aggregation`, not `Marten.Events.Projections`. And `MultiStreamProjection` is in `Marten.Events.Projections`, not `Marten.Events.Aggregation` — the namespaces are counterintuitive.
3. **`ProjectionLifecycle` moved from `Marten.Events.Projections` to `JasperFx.Events.Projections`.** Same JasperFx extraction.
4. **`AddWolverineHttp()` is required before `MapWolverineEndpoints()`.** The `service-bootstrap` skill doesn't mention this registration step. Without it, `MapWolverineEndpoints` throws at startup.
5. **`TimeProvider` must be registered in DI.** Wolverine HTTP handlers that inject `TimeProvider` fail at runtime if it's not registered. `builder.Services.AddSingleton(TimeProvider.System)` is needed.

---

## Design decisions

- **`IDocumentSession` over `MartenOps.StartStream` return-value pattern.** The handler has conditional read-then-write logic (check `ActiveRequestsByRider` → start stream). The `MartenOps.StartStream` tuple pattern doesn't support conditional responses cleanly. Direct session usage with explicit `SaveChangesAsync` is the pragmatic choice for this handler shape.
- **Both projections are inline.** `ActiveRequestsByRider` must be inline because the handler queries it in the same request that creates the stream. If it were async, the guard would race. `RequestTimeline` is inline for consistency; it could be async later if needed.
- **`Shared/` created from the start.** Workshop makes clear `Location` and `VehicleClass` are used across 5+ slices. The 3-reference threshold from `vertical-slice-organization` is met by design.

---

## Outstanding items / next-session inputs

- The D → B → C sequence is complete. Next decision: **A** (Trips workshop) or **E** (more Dispatch narratives).
