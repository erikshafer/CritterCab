# CritterCab Workshops Index

Workshop artifacts produced from Event Modeling and (future) Domain Storytelling sessions. Each workshop captures the event model for one bounded context (or a well-scoped slice of the system).

Each file is the durable artifact for its workshop — not a replacement for the live modeling surface, but the text record that downstream artifacts (narratives, prompts, skill files, ADRs) reference.

## Workshops

- [001 — Dispatch Event Model](001-dispatch-event-model.md) — CritterCab's first Event Modeling workshop. Scope: Dispatch bounded context (ride-request lifecycle from submission through handoff to Trips, or terminal failure). 12 slices covering command, view, automation, temporal-automation (Bruun), and translation patterns. Complete (v0.2, 2026-04-24).
- [002 — Trips Event Model](002-trips-event-model.md) — CritterCab's second Event Modeling workshop. Scope: Trips bounded context (post-acceptance ride lifecycle from `TripMatched` intake through `TripCompleted`, plus rider/driver pre-pickup cancellation and Bruun no-show timeout). 12 slices including aggregate-identity sidebar (§3) and forward-constraints disposition (§13). Second canonical data point for ADR candidates #5/#6/#7/#8 (all fired); one new ADR candidate (driver-app projection timing budget). All three forward-constraints from narratives 001+002 honored or partially honored. Complete (v0.11, 2026-05-09).

## Workshop follow-ups

Each workshop's §12.8 ("Follow-ups generated") names concrete action items that fall out of the session. This index aggregates the *named* follow-ups across workshops so they don't drift between sessions.

Status values: **pending**, **done** (with link to closing PR/artifact), **superseded** (with one-line note).

### From [Workshop 001 — Dispatch Event Model](001-dispatch-event-model.md) §12.8

| Follow-up | Status |
|---|---|
| Dispatch business-event protobuf contracts (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`) under ADR-009. | **done** — PR #4 (2026-05-07) via [`prompts/decisions/001-protobuf-ride-assigned.md`](../prompts/decisions/001-protobuf-ride-assigned.md). |
| Trips BC workshop — slice 5.10's intake and slice 5.12's contract stub depend on Trips committing its side. | **done** — closed by [Workshop 002 — Trips Event Model](002-trips-event-model.md) (v0.11, 2026-05-09). Slice 6.1 honors the intake idempotency contract; slice 6.9 mirrors §5.12 with documented override (distinct events vs. unified `TripTerminatedEarly`). |

### From [Workshop 002 — Trips Event Model](002-trips-event-model.md) §14.8

| Follow-up | Status |
|---|---|
| Trips business-event Protobuf authorship — 4 new outbound topics under `/protos/crittercab/trips/v1/` (`trip-completed`, `trip-cancelled-by-rider`, `trip-cancelled-by-driver`, `trip-abandoned-as-no-show`). | **pending** — new PR per PR #4 precedent. |
| ADR authorship session — 4 inherited Workshop 001 candidates fired (#5 config-as-events bootstrap, #6 aggregate-per-invariant, #7 shared cross-BC identifier, #8 ASB topic naming) plus 1 new (driver-app projection timing budget). | **pending** — strongly indicated; recommended bundled authorship session. |
| Workshop 001 §5.12 revision — add `RIDER_NO_SHOW` value to preferred `TerminationReason` enum; consider `ASSIGNMENT_COMPLETED_NORMALLY`. | **pending** — Workshop 001 follow-up; tactical revision PR. |
| Identity workshop forward-constraints — Identity's eventual workshop must publish `RiderRegistered` and `RiderProfileUpdated` business events for slice 6.12's enrichment to work. | **pending** — Identity workshop hasn't been scheduled. |
| Mid-trip cancellation paths workshop — held out of Workshop 002 scope per §2.3. | **pending** — dedicated follow-up workshop when mid-trip rider/driver/emergency cancellation becomes load-bearing. |

> §12 ADR candidates and §11 parking-lot items are also follow-ups but live with their workshop. ADR candidates carry explicit triggers; parking-lot items defer until their relevant context surfaces. Neither is enumerated here; revisit each workshop's §11/§12 when the triggers fire.

## Conventions

- Workshop files use three-digit numeric prefixes (`001-`, `002-`, ...) for ordering independent of slug.
- Each workshop artifact carries a Scope Statement, Ubiquitous Language glossary, Event List, per-slice walkthrough, cross-reference tables (Translation slices, temporal automations, configuration-as-events, Protobuf surface), a Parking Lot / Open Questions section, ADR candidates, and a Retrospective that closes the session.
- See `docs/research/event-modeling-workshop-guide.md` for the methodology reference and `docs/research/agents-in-event-models.md` for the Klefter decision-event and Bruun temporal-automation pattern extensions.