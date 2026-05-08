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

