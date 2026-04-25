# CritterCab Workshops Index

Workshop artifacts produced from Event Modeling and (future) Domain Storytelling sessions. Each workshop captures the event model for one bounded context (or a well-scoped slice of the system).

Each file is the durable artifact for its workshop — not a replacement for the live modeling surface, but the text record that downstream artifacts (narratives, prompts, skill files, ADRs) reference.

## Workshops

- [001 — Dispatch Event Model](001-dispatch-event-model.md) — CritterCab's first Event Modeling workshop. Scope: Dispatch bounded context (ride-request lifecycle from submission through handoff to Trips, or terminal failure). 12 slices covering command, view, automation, temporal-automation (Bruun), and translation patterns. Complete (v0.2, 2026-04-24).

## Conventions

- Workshop files use three-digit numeric prefixes (`001-`, `002-`, ...) for ordering independent of slug.
- Each workshop artifact carries a Scope Statement, Ubiquitous Language glossary, Event List, per-slice walkthrough, cross-reference tables (Translation slices, temporal automations, configuration-as-events, Protobuf surface), a Parking Lot / Open Questions section, ADR candidates, and a Retrospective that closes the session.
- See `docs/research/event-modeling-workshop-guide.md` for the methodology reference and `docs/research/agents-in-event-models.md` for the Klefter decision-event and Bruun temporal-automation pattern extensions.