# CritterCab Workshops Index

Workshop artifacts produced from Event Modeling and (future) Domain Storytelling sessions. Each workshop captures the event model for one bounded context (or a well-scoped slice of the system).

Each file is the durable artifact for its workshop — not a replacement for the live modeling surface, but the text record that downstream artifacts (narratives, prompts, skill files, ADRs) reference.

## Workshops

- [001 — Dispatch Event Model](001-dispatch-event-model.md) — CritterCab's first Event Modeling workshop. Scope: Dispatch bounded context (ride-request lifecycle from submission through handoff to Trips, or terminal failure). 12 slices covering command, view, automation, temporal-automation (Bruun), and translation patterns. Complete (v0.3, 2026-05-09; §5.12 amended per Workshop 002 §6.9 — added `RIDER_NO_SHOW` enum value, documented override of preferred unified-event shape).
- [002 — Trips Event Model](002-trips-event-model.md) — CritterCab's second Event Modeling workshop. Scope: Trips bounded context (post-acceptance ride lifecycle from `TripMatched` intake through `TripCompleted`, plus rider/driver pre-pickup cancellation and Bruun no-show timeout). 12 slices including aggregate-identity sidebar (§3) and forward-constraints disposition (§13). Second canonical data point for ADR candidates #5/#6/#7/#8 (all fired); one new ADR candidate (driver-app projection timing budget). All three forward-constraints from narratives 001+002 honored or partially honored. Complete (v0.11, 2026-05-09).
- [003 — Onboarding Domain Story](003-onboarding-domain-story.md) — CritterCab's **first Domain Storytelling artifact** (distinct from the Event Modeling artifacts above). Scope: Onboarding bounded context driver-vetting lifecycle from interest signal through Driver-Profile activation, with two failure paths (recoverable document-rejection; terminal background-check rejection via human adjudication and FCRA two-phase notice). Three stories captured; vocabulary disambiguation pass; 7 BC-boundary findings (vision-doc BC split survives empirical testing); 19 open questions queued for Workshop 004 (event model). Establishes notation conventions for future DS sessions (markdown sequence; three notation devices — repeat-aggregation, time-passage, reference-block). **Methodology pilot landed as Exercised** per [`docs/vision/README.md`](../vision/README.md) v0.5. **First exercise of the context-map's [§Update cadence](../context-map/README.md#update-cadence) convention** (edge #6 prose amendment landed in same PR). Complete (v0.1, 2026-05-26).

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
| Workshop 001 §5.12 revision — add `RIDER_NO_SHOW` value to preferred `TerminationReason` enum; consider `ASSIGNMENT_COMPLETED_NORMALLY`. | **done** — landed in the same PR as Workshop 002 (2026-05-09). Workshop 001 v0.3; §5.12 amendment subsection. `RIDER_NO_SHOW` added to both enums; `ASSIGNMENT_COMPLETED_NORMALLY` deliberately not added per scoping decision (explicit-implicit-happy-path resolution). |
| Identity workshop forward-constraints — Identity's eventual workshop must publish `RiderRegistered` and `RiderProfileUpdated` business events for slice 6.12's enrichment to work. | **pending** — Identity workshop hasn't been scheduled. |
| Mid-trip cancellation paths workshop — held out of Workshop 002 scope per §2.3. | **pending** — dedicated follow-up workshop when mid-trip rider/driver/emergency cancellation becomes load-bearing. |

### From [Workshop 003 — Onboarding Domain Story](003-onboarding-domain-story.md) §5.3

| Follow-up | Status |
|---|---|
| Workshop 004 — Onboarding event model. Inherits 19 open questions from W003 §5.3 (OQ-1 through OQ-9 event-modeling concerns; OQ-10 through OQ-13 BC-boundary concerns; OQ-14 escalated to vision-doc level; OQ-15 through OQ-19 deferred to future sessions). Recommended W004 facilitator agenda. | **pending** — natural successor session for the Onboarding BC. |
| Vision-doc OQ on suspension / reinstatement / deactivation BC placement (W003 OQ-14 escalated). | **pending** — vision-doc v0.5 §Open Questions; trigger to resolve is the first session that models the suspension lifecycle. |
| Codification of DS notation conventions as a skill file (Q4 in W003 retro). | **deferred** — trigger is "one more DS session exercises the conventions without modification." Likely target: `docs/skills/methodology/domain-storytelling/SKILL.md`. |
| Identity BC's outbound `identity.driver-registered` contract (W003 Story 1 step 4) — companion to the existing rider-side `identity.rider-registered` forward-constraint from W002 §13 #2. | **pending** — Identity workshop hasn't been scheduled. |

> §12 ADR candidates and §11 parking-lot items are also follow-ups but live with their workshop. ADR candidates carry explicit triggers; parking-lot items defer until their relevant context surfaces. Neither is enumerated here; revisit each workshop's §11/§12 when the triggers fire.

## Conventions

- Workshop files use three-digit numeric prefixes (`001-`, `002-`, ...) for ordering independent of slug.
- Each workshop artifact carries a Scope Statement, Ubiquitous Language glossary, Event List, per-slice walkthrough, cross-reference tables (Translation slices, temporal automations, configuration-as-events, Protobuf surface), a Parking Lot / Open Questions section, ADR candidates, and a Retrospective that closes the session.
- See `docs/research/event-modeling-workshop-guide.md` for the methodology reference and `docs/research/agents-in-event-models.md` for the Klefter decision-event and Bruun temporal-automation pattern extensions.