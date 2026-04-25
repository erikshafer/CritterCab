# Prompt 001 — First Event Modeling Workshop

| Field | Value |
|---|---|
| **Status** | Complete |
| **Triggered session date** | 2026-04-24 |
| **Captured** | 2026-04-25 (retroactively, for provenance) |
| **Produced artifact** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) |
| **Target BC** | Selected during session — Dispatch (out of Dispatch / Trips / Onboarding / Identity candidates) |
| **Outcome** | 12 slices walked, 8 ADR candidates surfaced, 14 parking-lot items, 5 durable feedback memories captured |

---

## Note on retroactive capture

The CritterCab project's `docs/prompts/` directory was established *after* this session ran, as part of the workshop's own follow-up work. This file preserves the prompt that triggered the session for provenance and for future contributors who want to see how the first workshop was framed. The text below is verbatim from the session opening; nothing has been edited for hindsight or polish. Errors of framing or scope are part of the historical record.

---

## Original prompt (verbatim)

I want to run CritterCab's first Event Modeling workshop in this session.

PROJECT ORIENTATION (read these, in order, before doing anything else):

1. C:\Code\CritterCab\CLAUDE.md — the routing layer. Skim for conventions
   and non-negotiables.
2. C:\Code\CritterCab\docs\vision\README.md — canonical project overview.
   Read fully. Note the tentative bounded contexts and the parked decisions.
3. C:\Code\CritterCab\docs\research\event-modeling-workshop-guide.md — the
   methodology reference. Long. Read fully. Pay attention to Lesson 2
   (notation: events, commands, views, automations, translations),
   Lesson 3 (Translation slices — more common in CritterCab than average),
   and the applicability notes.
4. C:\Code\CritterCab\docs\research\agents-in-event-models.md — extends
   the workshop guide with two patterns worth keeping in mind even for a
   first workshop: (a) agents as automations (don't invent new notation),
   and (b) the temporal-automation slice pattern from Jake Bruun's board
   (todo-list read model with asterisk suffix, clock-rewind glyph on
   time-driven automations, configuration-as-events).
5. C:\Code\CritterCab\docs\rules\structural-constraints.md — structural
   rules you must not violate while modelling.
6. C:\Code\CritterCab\docs\research\sdd-event-model-to-code.md — the
   workflow downstream of this workshop. You don't need to execute it,
   but understand that this workshop's output is the source of truth
   that later sessions will implement against.

CURRENT STATE:

- Design phase. Zero production code. Zero narratives written yet.
- docs/workshops/README.md is a one-line placeholder. This session will
  produce the first real workshop artifact there.
- docs/narratives/, docs/skills/ are READMEs only.
- One ADR exists (ADR-011 on Critter Stack); more will be written as
  decisions crystallise.

OBJECTIVE FOR THIS SESSION:

Run the first Event Modeling workshop end-to-end and produce a durable
artifact. The deliverable is:

- A new file under docs/workshops/ capturing the event model for ONE
  bounded context (the choice of which BC is the first decision of the
  workshop — propose 2-3 candidates with tradeoffs, then let me pick).
- The artifact should include: the chosen BC's scope statement, the
  event timeline (events in chronological order), the commands producing
  each event, the views/read models derived, any automations
  (including temporal ones if relevant), any translation slices at the
  BC boundary, and an open-questions section for anything unresolved.
- Format: markdown with a structured textual representation of the board
  (the research doc has examples — tables are fine; ASCII diagrams
  optional). No images required.

WORKSHOP DISCIPLINES TO HONOR:

- Timeline is left-to-right, chronological. Every event answers "what
  happened?" in past tense.
- Every view must be derivable from events upstream of it. If it isn't,
  either an event is missing or a translation slice is needed.
- Automations are modeled as green stickies regardless of implementation
  (deterministic handler or future agentic LLM). Agent internal reasoning
  is never on the board.
- Translation slices: when a slice coordinates external systems AND a
  decision is made locally, promote the decision to a local event (the
  Klefter "SupportEscalated" pattern — see agents-in-event-models.md).
- Temporal automations use the todo-list read model pattern (name ends
  with asterisk) — see the Bruun board transcription.

WHAT NOT TO DO:

- Do not write any C# code. This is pure design.
- Do not make architectural decisions that warrant an ADR without first
  proposing the ADR — prefer flagging as open questions.
- Do not invent new Event Modeling notation. The existing vocabulary
  handles every case that comes up in this workshop.
- Do not skip the scope-statement step. A BC with unclear scope produces
  an unclear model.

START BY:

1. Reading the files above.
2. Proposing 2-3 candidate bounded contexts from the vision doc for the
   first workshop, with one-line tradeoffs each. Let me pick.
3. Once I pick, proposing a scope statement for that BC. Let me adjust.
4. Then walking the timeline slice by slice, pausing after each slice so
   I can correct or extend.

Don't batch the whole workshop into one output. We do this interactively.

---

## Outcome summary (post-session, for cross-reference)

The session selected **Dispatch** as the first workshop's bounded context (out of four candidates: Dispatch, Trips, Onboarding, Identity). The chosen BC was the architectural spine of the system — the gravitational center per the ride-sharing research and the richest exercise of the methodology vocabulary.

The session walked 12 slices interactively, with sign-off after each. The artifact accumulated:

- 13 events plus alternates across the Ride Request lifecycle.
- 6 Translation slices touching 4 counterparty BCs (Pricing, Telemetry, Driver Profile, Trips).
- 2 Bruun-pattern temporal automations (offer expiry, request abandonment watchdog).
- 1 configuration-as-events slice (`DispatchPolicyConfigured` consolidating 9 operator-tunable parameters).
- 8 ADR candidates with named triggers.
- 14 parking-lot items with dispositions.
- One full Protobuf message authored inline (`RideAssigned` business event).
- 5 durable feedback memories captured (proactive projections, Critter Stack primitives, BC-owned enums, Decider Pattern correspondence, communication preferences).

The retrospective in §12 of the produced artifact captures the methodology learnings for the next BC workshop. The final document was committed at v0.2 (2026-04-24).
