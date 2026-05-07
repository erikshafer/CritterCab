---
name: event-modeling
description: "Facilitate an Event Modeling workshop. Use when running, simulating, planning, or guiding any phase — brain dump, timeline, slicing, scenario writing — or when multi-persona facilitation is needed for system design."
cluster: core
tags: [event-modeling, design, workshops, methodology, slices, ddd]
---

# Event Modeling Workshop

Event Modeling is a collaborative workshop technique created by Adam Dymitruk (Adaptech Group) for designing information systems. It produces a visual, timeline-based blueprint showing how data flows through a system — from user intent through state changes to read-side projections. It works for any information system, not just event-sourced ones, but maps naturally onto CQRS and event sourcing patterns.

CritterCab's vision (per ADR-004) commits to "Event Modeling first, code second." Workshop output flows into narratives (`docs/narratives/`), narratives flow into prompts (`docs/prompts/`), and prompts drive implementation. This skill covers the workshop technique itself.

## When to apply this skill

Use this skill when:

- Running, simulating, or facilitating an Event Modeling session.
- Planning a workshop scope (which BCs, which journey).
- Defining slices from a complete event model.
- Writing Given/When/Then scenarios from slice definitions.
- Verifying or extending the event vocabulary against a user journey.
- Using multi-persona facilitation to surface conflicts and edge cases.

This skill is a methodology skill. The implementation skills downstream (`marten-aggregates`, `wolverine-message-handlers`, etc.) consume its outputs but are not themselves activated by it.

## The Four Building Blocks

| Block | Color | Meaning |
|---|---|---|
| **Events** | Orange | Facts that occurred — past tense, immutable |
| **Commands** | Blue | User intentions or system requests that cause events |
| **Views / Read Models** | Green | Projections of event data back to the UI |
| **UI Wireframes / Screens** | White | What the user actually sees and interacts with |

Arrange these in chronological order on a horizontal timeline, in swim lanes:
`UI → Command → Event Stream → View → UI`

---

## Workshop Phases

### Phase 1 — Brain Dump

Everyone writes events as fast as possible — no ordering, no judgment. Events are facts: past tense, concrete, meaningful to the domain.

**Input:** A domain or feature area to explore (e.g., "rider requests a ride during surge", "Trips BC internals from acceptance through completion").
**Process:** Each persona calls out events. No filtering, no sequencing — volume over accuracy.
**Output:** Unordered list of candidate events (expect 15–60 for a single bounded context).

> When CritterCab develops an established event vocabulary (in `docs/vision/` or a dedicated event-vocabulary doc), Phase 1 of journey workshops becomes a **verification pass** — walk the journey and confirm the vocabulary accounts for everything. Add missing events as discovered.

### Phase 2 — Storytelling

Arrange events into a coherent narrative on the timeline. Ask: *"What happened first? What does this enable next?"* Gaps in the story reveal missing events.

**Input:** Unordered event list from Phase 1 (or verified vocabulary for journey workshops).
**Process:** Place events left-to-right on the timeline. Fill gaps: "What happened between X and Y?"
**Output:** Chronologically ordered event timeline with gap markers resolved.

### Phase 3 — Storyboarding

Add UI wireframes above the timeline and views below. Connect them to their triggering commands and resulting events. This makes the full user journey visible.

**Input:** Ordered event timeline from Phase 2.
**Process:** For each event, ask "What UI triggered this?" (add screen above) and "What does the user see after?" (add view below). Connect with commands.
**Output:** Full storyboard: `UI → Command → Event(s) → View → UI` for the entire flow.

### Phase 4 — Identify Slices

Draw vertical cuts through the model — each slice is one complete feature: `UI → Command → Event(s) → View`. Slices become work units (narratives, prompts, PRs).

**Input:** Complete storyboard from Phase 3.
**Process:** Draw vertical lines. Each slice must be independently deliverable and testable.
**Output:** Slice table (see Structured Output Format below).

### Phase 5 — Scenarios (Given/When/Then)

For each slice, write acceptance scenarios:
- **Given**: the events already in the stream (preconditions).
- **When**: the command issued.
- **Then**: the new events produced and/or the view state.

**Input:** Slice definitions from Phase 4.
**Process:** Write happy path first, then edge cases and failure modes per slice.
**Output:** Given/When/Then scenarios per slice.

---

## Two Workshop Types

CritterCab uses two complementary workshop formats.

### User Journey Workshop

Walks a cross-cutting scenario (e.g., a rider requesting a ride, a driver completing onboarding) end-to-end. Touches multiple BCs. Produces horizontal coverage — the sequence of handoffs and integration events across the system.

**Best for:** Validating the integration topology, defining narrative scope, confirming the event vocabulary covers a complete user scenario.

**Tradeoff:** Does not produce aggregate internals, saga state machine details, or deep failure/compensation paths within a single BC.

### BC-Focused Workshop

Deep-dives into a single bounded context. Produces vertical depth — aggregate design, saga state transitions, DCB boundary model details, compensation events, and edge cases.

**Best for:** Implementation-ready designs for a specific BC. Produces the Given/When/Then scenarios that become test cases.

**Tradeoff:** Does not validate cross-BC integration or end-to-end user experience.

**Recommended sequence:** Run one or two user journey workshops first to establish the horizontal map, then run BC-focused workshops to fill in vertical depth before implementation.

---

## Structured Output Format for Slices

| # | Slice Name | Command | Events | View | BC | Priority |
|---|-----------|---------|--------|------|----|----------|
| 1 | Rider requests a ride | `RequestRide` | `RideRequested` | `RideRequestStatusView` (searching for drivers) | Dispatch | P0 |
| 2 | Driver accepts an offer | `AcceptOffer` | `OfferAccepted`, `TripStarted` | `ActiveTripView` (rider sees driver en route) | Dispatch → Trips | P0 |
| 3 | Trip completes | `CompleteTrip` | `TripCompleted`, `FareCalculated` | `TripSummaryView` (fare, route, receipt) | Trips → Pricing | P0 |
| 4 | Driver background check | *(scheduled / external)* | `BackgroundCheckCompleted` | `OnboardingStatusView` | Onboarding | P1 |

**Column definitions:**
- **Slice Name**: Human-readable feature name.
- **Command**: The command that enters the system (user or system-initiated). Use *(scheduled)* for time-triggered slices and *(external)* for slices triggered by an upstream provider event.
- **Events**: Domain events produced (comma-separated if multiple). Cross-service events show the producer-consumer chain with `→` (e.g., `Dispatch → Trips`).
- **View**: The read model or UI state updated after the event.
- **BC**: Bounded context that owns this slice. Verify against the BC list in [`docs/vision/README.md`](../../vision/README.md). For slices that span BCs, list the producer-then-consumer chain.
- **Priority**: P0 = must-have for first vertical demo, P1 = should-have, P2 = nice-to-have.

---

## Adjunct Patterns

Beyond the four core building blocks, three named event-modeling patterns recur across event-sourced systems. Naming them here lets workshop prose, narrative authoring, and ADRs refer to each by its published-literature name rather than re-deriving the shape each time.

Sources: Adam Dymitruk (Adaptech Group, the core method), Filip Klefter (translation-decision events), and Anders Bruun Olsen (temporal-automation slice pattern, configuration-as-events).

### Klefter Translation-Decision Events

When a slice coordinates with an external system AND a decision is made locally based on the external input, the local decision is captured as a first-class event in the BC's stream. Names the BC's authority over the decision even though the input came from outside; the event is the audit trail of "I asked X, got Y, decided Z."

**Pattern signal:** an outbound query whose result the BC commits as a local event before any further processing.

**CritterCab example:** the Onboarding BC's background-check decision. When a driver application enters the vetting workflow, Onboarding sends the applicant's information to an external background-check provider. The provider responds with a decision and supporting data. Onboarding commits the decision as a local event — `BackgroundCheckCompleted` carrying the provider's reference, the determination (`Pass`/`Fail`/`NeedsReview`), and a reason code if applicable. Downstream Onboarding logic (approve, reject, request more documents) consumes the local event. The provider's response is never read again outside Onboarding.

A second candidate: Payments' authorization decision. At trip start, Payments calls the payment provider for an auth hold. The result lands as `PaymentAuthorized` (with provider auth code) or `PaymentAuthFailed` (with reason). The Trips service consumes the decision via integration event without ever touching the payment provider directly. The decision is Payments' authority; the audit trail is the local event.

### Bruun Temporal-Automation Slice Pattern

A slice whose trigger is the passage of time, not an incoming domain event. The slice fires when a clock condition is met (`now() >= scheduledFor`) on a row in a todo-list read model. Boards render the pattern with two distinguishing marks: a clock-rewind glyph on the gear (automation) sticky, and an asterisk suffix on the read model's name (e.g., `OffersAwaitingAcceptance*`).

**Pattern signal:** an automation whose trigger is clock state, consuming a todo-list read model whose rows self-remove when the work completes.

**CritterCab example:** the Dispatch offer-timeout. When an offer is dispatched to a candidate driver, the dispatch saga schedules a timeout (e.g., 15 seconds for a flash offer). If the driver doesn't respond before the timer fires, the saga commits `OfferExpired` and re-dispatches to the next candidate. The todo-list projection `OffersAwaitingAcceptance*` carries rows added on `OfferDispatched` and removed on either `OfferAccepted` or `OfferExpired`. The asterisk convention marks it as a temporal-automation source.

A second candidate: Trips' arrival-timeout. When a trip transitions to `EnRoute`, a saga can schedule a check at the projected arrival time. If `DriverArrived` hasn't fired by then, the saga commits a domain-meaningful event (`TripArrivalDelayed` or similar) that triggers downstream logic — rider notification, support escalation, etc.

### Configuration-as-Events (Bruun)

Operator-tunable policy parameters represented as events on a singleton stream rather than rows in a settings table. Each configuration change is an event; the current policy is the latest event's payload. Provides audit trail, version history, and natural integration with event-driven downstream consumers.

**Pattern signal:** policy that needs an audit trail and version history, where downstream consumers should react to changes rather than periodically re-read a settings table.

**CritterCab candidate:** Pricing's surge-policy parameters — base multiplier, max multiplier, geographic zone definitions, demand thresholds — could land as `SurgePolicyConfigured` events on a singleton stream. The Pricing BC's `SurgeActivated` payload would carry the policy version governing the activation, so a mid-period policy change does not retroactively affect in-flight surge windows.

A second candidate: Onboarding's vetting-policy parameters — acceptable background-check providers, required document types, expiration windows. As configuration-as-events, these provide the audit trail required for compliance review and let downstream logic react to policy changes (e.g., re-vetting drivers whose previous check used a now-deprecated provider).

This section names patterns; it does not commit CritterCab to implement any of them. Naming makes the model legible when the project encounters these patterns during workshops or when a future ADR proposes adopting one for a specific BC.

---

## Output Artifacts

- **The Event Model** — the full visual blueprint (primary deliverable, captured in `docs/workshops/`).
- **Slice definitions** — vertical feature cuts, each independently deliverable.
- **Given/When/Then scenarios** — acceptance criteria per slice.
- **Narrative drafts** — slices group into journey-scoped narratives in `docs/narratives/` (per ADR-003).
- **API contracts** — command shapes, read model schemas, and proto-message candidates emerge naturally.
- **Aggregate / projection sketches** — implementation starting points for the Phase 2 skills.

---

## Multi-Persona Facilitation

When facilitating a workshop, invoke distinct personas to represent different stakeholder perspectives. This surfaces conflicts, blind spots, and richer domain understanding than a single voice would produce.

### Persona Roles

The persona roster typically includes the roles below. Specific persona profiles for CritterCab will be authored in `docs/personas/` as workshops begin; the roles themselves are project-agnostic.

| Role | Voice in Workshop |
|---|---|
| **Facilitator** | Leads the workshop, maintains flow, keeps slices small, synthesizes output. |
| **Domain Expert** | Owns the business language; corrects names, validates against ride-sharing conventions and operator-side reality. |
| **Architect** | Flags BC boundaries, aggregate design, projection feasibility, transport choices, Critter Stack patterns. |
| **Backend Developer** | Asks "how would we build that?", flags implementation concerns, validates handler/saga shapes. |
| **Frontend Developer** | Grounds the model in the rider/driver UI; asks what users see at each step. |
| **QA** | Stress-tests the model; asks about failures, edge cases, race conditions, timing windows. |
| **Product Owner** | Guards scope, prioritizes slices, enforces demo-first constraints. |
| **UX** | Advocates for rider, driver, and operator experience; read model legibility. |

### Which Personas Lead Each Phase

| Phase | Primary Voices | Why |
|---|---|---|
| **Brain Dump** | Facilitator + Domain Expert + Architect | Facilitator keeps pace; Domain Expert knows business events; Architect knows technical/integration events. |
| **Storytelling** | All eight — QA earns their keep here | QA finds gaps; UX maps events to user moments; everyone contributes to sequencing. |
| **Storyboarding** | Frontend Developer + UX + Backend Developer | Frontend designs screens; UX validates experience; Backend confirms view feasibility. |
| **Slicing** | Facilitator + Product Owner + Backend Developer | Facilitator keeps slices crisp; PO prioritizes; Backend validates deliverability. |
| **Scenarios** | Facilitator + QA + Backend Developer + Domain Expert | QA writes edge cases; Backend validates feasibility; Domain Expert validates accuracy. |

### How to Run Multi-Persona Mode

```
[@Facilitator] Let's verify the brain dump. Walk me through what happens
  from the moment a rider taps "Request Ride" in the app.

[@DomainExpert] The rider has an active session — they're authenticated.
  The tap produces a RequestRide command. Dispatch picks it up. That's
  RideRequested. Dispatch then needs to find candidate drivers.

[@Architect] RideRequested is a Dispatch BC event. Telemetry has the
  driver-locations read model that Dispatch queries — that's a gRPC call
  in Cab, not a shared database read. Worth flagging on the timeline as
  a cross-service interaction.

[@QA] What if there are no candidate drivers in range? Do we fail the
  request immediately, or does Dispatch keep searching with an expanding
  radius? What's the timeout?

[@Facilitator] Good question. Park it as a candidate slice — "no drivers
  available" is its own scenario with its own command/event/view. Continue
  with the happy path.

[@FrontendDeveloper] After RequestRide, the rider sees a "Searching for
  drivers..." view. That's a read model — RideRequestStatusView or similar.
  Updates as candidates respond.
```

Personas may agree, disagree, and build on each other. The goal is productive tension — not consensus for its own sake.

---

## CritterCab Integration

### How Workshop Outputs Connect to CritterCab Artifacts

| Workshop Output | CritterCab Artifact | Location |
|---|---|---|
| **Workshop session record** | Markdown capture of the session | [`docs/workshops/`](../../workshops/) |
| **Slices** | Narrative drafts (journey-scoped); prompts (task-scoped) | [`docs/narratives/`](../../narratives/), [`docs/prompts/`](../../prompts/) |
| **Scenarios (Given/When/Then)** | Test specifications | `tests/` per service |
| **BC boundary changes** | Update or verify | [`docs/vision/README.md`](../../vision/README.md) § Tentative Bounded Contexts |
| **Event vocabulary changes** | Update or verify | [`docs/vision/README.md`](../../vision/README.md) (or future event-vocabulary doc) |
| **Architectural decisions** | ADR markdown files | [`docs/decisions/`](../../decisions/) |
| **Command / event shapes** | C# records in service projects | `src/CritterCab.<ServiceName>/` |
| **View / read model designs** | Marten or Polecat projections per service | `src/CritterCab.<ServiceName>/` |
| **Cross-service contracts** | `.proto` files (per ADR-009) | `/protos/` (repo root) |

### Existing Documents to Load

| Document | When to load |
|---|---|
| [`docs/vision/README.md`](../../vision/README.md) | Always — verify BC ownership, technology choices, design principles. |
| [`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md) | Always — service-boundary rules, transport selection, identity ACL. |
| [`docs/narratives/`](../../narratives/) | Journey workshops — load relevant narratives if the journey extends an existing one. |
| [`docs/decisions/`](../../decisions/) | When the workshop touches a topic an ADR governs (transport, contracts, identity). |
| [`docs/personas/README.md`](../../personas/) | When persona files exist; the multi-persona technique is the same regardless. |

---

## Quick Reference: Common Mistakes to Catch

- **Events named as commands.** "RequestRide" is wrong as an event — "RideRequested" is correct.
- **"Event" suffix.** "TripCompletedEvent" is wrong — "TripCompleted" is correct. See `domain-event-conventions`.
- **Missing the "why" behind a command.** Add a UI wireframe to show the trigger.
- **Views that can't be derived from the events on the board.** You're missing events.
- **Slices too large to deliver independently.** Keep slicing. A slice that takes more than one prompt to implement is too large.
- **Scenarios that test infrastructure instead of behavior.** Focus on domain facts, not on whether ASB delivers messages.
- **Assigning a slice to the wrong BC.** Verify against [`docs/vision/README.md`](../../vision/README.md) § Tentative Bounded Contexts.
- **Skipping the QA voice.** Edge cases found late are expensive to fix.
- **Conflating mechanical events with business decisions.** `OfferExpired` (clock fired) is mechanical; `OfferRejected` (driver said no) is a business decision. Both are events; they have different authority and different downstream consequences.
- **Treating a downstream BC as the originator of upstream data.** Operations doesn't originate trip data — it consumes it. Pricing doesn't originate trip facts — it consumes them and emits pricing facts.

---

## See also

**Downstream** — natural follow-ups when workshop output is in hand:

- `domain-event-conventions` — naming and shape rules for the events identified in workshops.
- `marten-aggregates` — implementing event-sourced aggregates from workshop output (Phase 2).
- `marten-wolverine-aggregates` — implementing handlers that produce workshop-identified events (Phase 2).
- `wolverine-sagas` — implementing the temporal-automation slices identified by the Bruun pattern (Phase 4).
- `protobuf-contracts` — implementing the cross-service contracts implied by cross-BC slices (Phase 1).

**External:**

- [Adam Dymitruk's Event Modeling site](https://eventmodeling.org/) — the canonical reference for the technique.
- [`docs/vision/README.md`](../../vision/README.md) § Methodology — CritterCab's commitment to Event Modeling and Domain Storytelling.
- ADR-003 in [`docs/decisions/`](../../decisions/) — capture intent in durable, structured form.
- ADR-004 in [`docs/decisions/`](../../decisions/) — Event Modeling first, code second.
