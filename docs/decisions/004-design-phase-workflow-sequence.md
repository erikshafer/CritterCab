# ADR-004: Design-Phase Workflow Sequence

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab is in its design phase. No runnable code exists yet. Several design techniques are already committed — Event Modeling as the primary design tool, Domain Storytelling as a complementary technique, DDD strategic design including context maps, and an NDD-informed narrative layer sitting between workshop output and implementation (ADR-003). The question is in what order these techniques are applied and how their outputs connect.

Without a defined sequence, two failure modes are common. The first is premature implementation: coding begins before the domain language is stable, service boundaries are drawn by accident rather than by domain reasoning, and Event Modeling (if it happens at all) is used to document what was built rather than to design what should be built. The second is analysis paralysis: the design work expands without constraint, workshops are repeated without converging, and the project never reaches code because the model is never "done enough."

A defined sequence addresses both by establishing what happens in what order, what each step's output is, and when a step is finished enough to move to the next. It also makes the feedback path explicit — the sequence is not a waterfall, and the right mechanism for updating upstream artifacts is the retrospective, not a new design session.

## Options Considered

### Option A — Code-first with lightweight design

Implementation begins when there is enough understanding to start. Design artifacts are produced opportunistically: an event name is picked when a handler is written, a bounded context boundary is drawn when a service project is created, a narrative is written if and when a contributor finds it useful. The Event Modeling workshop, if it happens, happens after some code exists and uses the code as a reference.

This is the default mode for most software projects. It is fast to start and requires no upfront investment in methodology. The cost appears gradually: naming inconsistencies accumulate, service boundaries calcify around accidental groupings rather than domain boundaries, and contributors who arrive later find no durable record of why things are shaped the way they are. For CritterCab specifically, an Event Modeling workshop run after code exists is primarily a documentation exercise rather than a design exercise — the most valuable output (swim-lane assignments driving service boundaries) has already been settled by implementation.

### Option B — Event Modeling only, without preliminary strategic design

The Event Modeling workshop is the first structured activity. Context mapping and domain storytelling are omitted or deferred indefinitely. The workshop surfaces bounded contexts and language boundaries as a side effect of modeling the timeline.

Event Modeling is capable of surfacing these things — the swim-lane step (Step 6, Apply Conway's Law) is where service boundaries are drawn, and the brain dump naturally forces language choices. The limitation is that discovering linguistic disagreements during an Event Modeling session is expensive: a naming dispute that surfaces in Step 2 (plot formulation) when "trip" turns out to mean different things to the rider, driver, and payments flows stalls the workshop and requires re-work on stickies that have already been placed. Context mapping and domain storytelling are cheaper tools for surfacing exactly these issues before the Event Model is populated.

Skipping preliminary strategic design is a reasonable tradeoff for a domain with clear, well-understood boundaries. Ride-sharing is not that domain: the overlap between Identity, Onboarding, and Profile; the ambiguity in what "trip" means across Dispatch, Trips, Payments, and Ratings; and the cross-cutting nature of Telemetry relative to Dispatch are all genuine linguistic and boundary ambiguities that will cost more to resolve inside the Event Modeling workshop than before it.

### Option C — Staged sequence with feedback loop

Design activities are ordered to minimize rework. Each step's output is the input to the next. The sequence is linear for the initial pass; retrospectives provide the feedback mechanism for updating any upstream artifact when implementation reveals a gap.

The sequence:

1. **Context Mapping.** Name the upstream/downstream relationships between bounded contexts. Identify anti-corruption layers (Identity is the clearest case), published languages, and conformist relationships. Output: a context map that informs Event Modeling swim lanes and feeds the service topology decision.
2. **Domain Storytelling.** Surface language boundaries between bounded contexts by telling domain stories with actors, work objects, and activities. Where the same word means different things to different actors — "trip" to a rider is a journey; "trip" to Dispatch is a matching lifecycle; "trip" to Payments is a billable event — Domain Storytelling surfaces the disagreement without requiring a fully populated Event Model. Output: stable, context-specific vocabulary for Event Modeling.
3. **Event Modeling.** The primary design tool. A multi-session workshop producing events, commands, views, swim lanes, slices, and GWT scenarios. Swim lanes are drawn with the benefit of Context Mapping's boundary clarity and Domain Storytelling's stable vocabulary. Output: a blueprint, a slice backlog, and GWT scenarios for the highest-priority slices.
4. **NDD-Informed Narratives.** Journey-scoped domain specs authored from Event Modeling slice output. Each narrative spans multiple slices and captures the user's perspective across the full journey. Captured in `docs/narratives/`. Output: durable, journey-scoped specifications that persist across sessions (ADR-003).
5. **Prompt Authoring.** Task-scoped build orders referencing narratives and skill files. One slice per prompt, one prompt per session. Output: a prompt document in `docs/prompts/` that drives a specific implementation session.
6. **Implementation and Retrospective.** Code produced in session, closed with a retrospective. The retrospective asks whether the slice's implementation revealed anything that should update the Event Model or narrative. If yes, those updates are part of the same PR. Output: committed code, a retrospective in `docs/retrospectives/`, and any upstream artifact updates the session surfaced.

## Decision

**Option C.** CritterCab's design phase follows the staged sequence above.

The sequence is the *initial* order of operations, not a waterfall. Steps 1–3 run once before the first line of code. Steps 4–6 iterate: each slice is a loop through narrative → prompt → implementation → retrospective. Retrospectives are the feedback mechanism and may update any upstream artifact — a narrative, an Event Model entry, or even a context map boundary — when the implementation reveals something the design did not anticipate.

## Consequences

Context Mapping and Domain Storytelling are prerequisites for the Event Modeling workshop, not optional preliminaries. Skipping them means accepting the cost of surfacing linguistic disagreements and boundary ambiguities inside the workshop, where rework is more expensive.

The service topology decision (ADR-010) is a downstream output of this sequence: it is made after the Event Modeling workshop's swim-lane step, not before. Committing to a specific service topology before that step would short-circuit the most valuable output the workshop has to offer for a system of CritterCab's scope.

The narrative format (ADR-011) is left open deliberately. Step 4 produces narratives; what those narratives look like is resolved by writing the first two or three in candidate formats and comparing what holds up in practice. The sequence does not depend on the format being decided in advance.

Each step's output is a committed artifact: context map in `docs/workshops/`, Domain Storytelling output in `docs/workshops/`, Event Model and slice backlog in `docs/workshops/`, narratives in `docs/narratives/`, prompts in `docs/prompts/`, retrospectives in `docs/retrospectives/`. A step is finished enough to move forward when its artifact is committed, not when the discussion feels complete.
