# CritterCab Workshops Index

Workshop artifacts produced from Event Modeling and (future) Domain Storytelling sessions. Each workshop captures the event model for one bounded context (or a well-scoped slice of the system).

Each file is the durable artifact for its workshop — not a replacement for the live modeling surface, but the text record that downstream artifacts (narratives, prompts, skill files, ADRs) reference.

## Workshops

- [001 — Dispatch Event Model](001-dispatch-event-model.md) — CritterCab's first Event Modeling workshop. Scope: Dispatch bounded context (ride-request lifecycle from submission through handoff to Trips, or terminal failure). 12 slices covering command, view, automation, temporal-automation (Bruun), and translation patterns. Complete (v0.2, 2026-04-24).

## Workshop follow-ups

Each workshop's §12.8 ("Follow-ups generated") names concrete action items that fall out of the session. This index aggregates the *named* follow-ups across workshops so they don't drift between sessions.

Status values: **pending**, **done** (with link to closing PR/artifact), **superseded** (with one-line note).

### From [Workshop 001 — Dispatch Event Model](001-dispatch-event-model.md) §12.8

| Follow-up | Status |
|---|---|
| Dispatch business-event protobuf contracts (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`) under ADR-009. | **done** — PR #4 (2026-05-07) via [`prompts/decisions/001-protobuf-ride-assigned.md`](../prompts/decisions/001-protobuf-ride-assigned.md). |
| Trips BC workshop — slice 5.10's intake and slice 5.12's contract stub depend on Trips committing its side. | **pending** — current lean for the next major design session. |

> §11 ADR candidates and §10 parking-lot items are also follow-ups but live with their workshop. ADR candidates carry explicit triggers ("first implementation session", "second BC workshop", etc.); parking-lot items defer until their relevant context surfaces. Neither is enumerated here; revisit each workshop's §10/§11 when the triggers fire.

## Conventions

- Workshop files use three-digit numeric prefixes (`001-`, `002-`, ...) for ordering independent of slug.
- Each workshop artifact carries a Scope Statement, Ubiquitous Language glossary, Event List, per-slice walkthrough, cross-reference tables (Translation slices, temporal automations, configuration-as-events, Protobuf surface), a Parking Lot / Open Questions section, ADR candidates, and a Retrospective that closes the session.
- See `docs/research/event-modeling-workshop-guide.md` for the methodology reference and `docs/research/agents-in-event-models.md` for the Klefter decision-event and Bruun temporal-automation pattern extensions.