---
name: event-modeling
description: "Run and record Event Modeling the way Adam Dymitruk defined it: the seven steps, the four slice patterns (Command, View, Automation, Translation) with Klefter's and Bruun's refinements, the field-level information-completeness check, and a slice definition of done. Use when planning, facilitating or simulating a modelling session, cutting slices, writing Given/When/Then scenarios, reviewing a model or its minutes, or naming a slice's pattern."
cluster: core
tags: [event-modeling, design, workshops, methodology, slices, ddd]
---

# Event Modeling

Event Modeling is a method created by **Adam Dymitruk** (Adaptech Group; first published as "What is Event Modeling?", eventmodeling.org, June 2019) for describing an information system as a timeline of state changes, told as a story. The result is a blueprint that reads left to right like a storyboard: screens or automations along the top, commands and events through the middle, views that inform the next step, all connected by the fields that flow between them.

It is not specific to event sourcing. Any system whose state changes can be told as discrete facts on a timeline can be modelled this way; event sourcing, CQRS and message-driven systems simply map onto it with very little translation.

Everything above the "In this repository" heading is project-neutral and can be copied between repositories as is.

## When to apply this skill

- Planning, running, simulating or facilitating a modelling session.
- Cutting a model into slices, or checking that an existing slice is one slice.
- Writing Given/When/Then scenarios for a slice.
- Reviewing a model, its export, or the minutes of a session.
- Deciding which pattern a slice is, or naming a slice.

It is a methodology skill. Implementation skills consume its output; they are not activated by it.

---

## Building blocks

Dymitruk counts **three** building blocks, drawn against a backdrop of wireframes:

| Block | Colour | What it is |
|---|---|---|
| **Event** | Orange | A fact that happened, named in the past tense, immutable. The only durable content of the system's history. |
| **Command** | Blue | An intention to change the system, carrying the fields the change needs. |
| **View** (read model) | Green | State derived from events, shown to a person or read by a process. |

**Wireframes** (screens) sit above the timeline and show where each command's fields come from and where each view's fields go. Dymitruk's phrasing is "three types of building blocks as well as traditional wireframes": they are not counted as a block, but the method does not work without them. Where no person is involved, the top lane shows an **automation** (a gear) or an **external system** instead. **Swim lanes** separate the actors and systems; once Conway's Law is applied (step 6) they mark who owns which events.

Later writers count the blocks differently (some promote screens to a fourth block). This skill keeps Dymitruk's count because nothing in the method depends on the count; what matters is that every slice says what its screen is, or says that it has none.

---

## The four patterns

Every slice is exactly one of Dymitruk's four patterns. They are the shapes a vertical slice of the timeline can take.

| Pattern | Shape | A slice of this pattern answers |
|---|---|---|
| **Command** | Screen (or automation) → command → event(s) | "How does this state change get into the system, and when is it refused?" |
| **View** | Event(s) → view → screen (or automation) | "What does someone need to see, and which events produce it?" |
| **Automation** | View (a to-do list) → automation → command → event(s) | "What does the system do on its own, triggered by state it can see?" |
| **Translation** | External system's data ↔ internal events | "How does a fact cross the system's edge, in either direction, into or out of our vocabulary?" |

A slice that appears to need two patterns is two slices. A command handler that also reacts to its own event by issuing another command is a Command slice followed by an Automation slice, even when the implementation commits both in one transaction: the model describes behaviour, and the transaction boundary is an implementation choice that comes later.

### Refinements (not additions)

Later contributions refine Dymitruk's Translation and Automation. They are named so they can be cited, but they are not new patterns and never take the pattern slot.

- **Translation-decision events** (Marc Klefter, 2026), a refinement of **Translation**. When a slice asks an external system something and then decides locally on the answer, the decision is recorded as a first-class local event ("we asked X, got Y, decided Z"). The external response is not re-read later; the local event is the audit trail and what downstream slices consume.
- **Agents as automations** (Marc Klefter, 2026), a refinement of **Automation**. A language-model agent is drawn as an automation like any other process: it reads a view, issues commands, and its decisions land as events on the same timeline.
- **Temporal automations** (Jake Bruun, 2026), a refinement of **Automation**. When the trigger is the passage of time, the automation reads a to-do-list view whose rows carry a due time and remove themselves when the work is done. The board marks this with a clock glyph on the automation and an asterisk on the view's name (`ItemsAwaitingExpiry*`). A single capability often decomposes into several slices: configure, act, schedule, expire.
- **Configuration as events**, shown in Bruun's temporal-automation board, where the lockout policy is itself an event (`AccountLockoutConfigured`) that later views and automations read. Operator-tunable policy is a Command slice that appends to a single policy stream; the current policy is the latest event, which gives history and audit for free and lets automations react to a change instead of polling a settings table. This refines how **Automation** (and Command) slices obtain their parameters.

---

## The seven steps

Dymitruk's workshop runs in seven steps. Run all seven for every new chapter of the model; skipping the storyboard or the completeness check is how models end up with slices nobody can build.

1. **Brainstorming.** Everyone writes events, past tense, as fast as possible. No order, no filtering. Output: an unordered pile of candidate events.
2. **The Plot.** Arrange the events into one plausible story along the timeline. Gaps ("what happened between these two?") become new events. Output: an ordered timeline.
3. **The Story Board.** Add the wireframes above the timeline, one per step a person takes, with the actual fields on them. For steps no person takes, place an automation or an external system instead and say so. Output: the story told screen by screen.
4. **Identify Inputs.** Add the commands that carry each screen's fields into the events. Every field on a command must come from the screen or from a view the actor could see. Output: command → event links.
5. **Identify Outputs.** Add the views that carry event fields back to the screens. Every field on a view must come from an event on the timeline. Output: event → view → screen links.
6. **Apply Conway's Law.** Split the events into swim lanes by the system or team that owns them. The lanes are the candidate boundaries between services; a slice that crosses a lane is usually a Translation. Output: ownership.
7. **Elaborate Scenarios.** For every slice, write Given/When/Then scenarios: the happy path first, then each refusal and edge case. Output: the specification each slice is built and tested against.

### Given/When/Then

| Pattern | Given | When | Then |
|---|---|---|---|
| Command | Prior events on the stream | The command, with its fields | New events, or a refusal |
| View | Events | (nothing) | The view's state |
| Automation | The to-do view's state (and, for temporal automations, the time) | The automation runs | The command it issues and the resulting events |
| Translation | The external input (or the internal events, outbound) | It is translated | The internal events (or the outbound message) |

Scenarios state domain facts. "The message was delivered" is not a scenario.

---

## Information completeness

Dymitruk's completeness check: **every field has an origin and a destination.** At the end of step 5 every field in the model is accounted for.

Run it field by field, not slice by slice:

- For each **command field**: which screen field, or which field of a view the actor could see, supplies it? If none, a screen is missing a field or a view is missing.
- For each **event field**: which command or translated input supplies it? If none, the event invents data.
- For each **view field**: which event field does it come from? If none, an event is missing (or a Translation that brings the data in).
- For each **screen field** that is displayed: which view supplies it? For each field the user enters: which command carries it?
- A field with an origin but **no destination** is either unneeded or a sign that a view or downstream slice is missing.

Discrepancies between a slice's listed reads and the fields it actually uses are exactly what this check exists to catch. Record each fix in the minutes.

## Slice definition of done

A slice is ready to build when all of these hold:

1. It is **one pattern**: Command, View, Automation or Translation, named in the slice.
2. It has a **wireframe**, or an explicit statement **"no screen: machine actor"** naming the automation or external system that drives it.
3. It passes the **field-level completeness check** above.
4. It has **Given/When/Then scenarios**: the happy path and each refusal.
5. It has a **name**, taken from its event or view in the ubiquitous language. The name is the slice's identity; renaming a slice is a refactor of everything that refers to it, so names are chosen once, deliberately.

A slice never carries a status (planned, in progress, done). Whether it is built is derived from the running system and its tests, not asserted in the model.

---

## Vocabulary discipline

- **The pattern slot holds only the four pattern names.** Framework vocabulary describes how a slice is implemented, not what it is: "aggregate handler", "start handler", "continue handler", "saga", "consumer", "overwrite-in-place document", "projection" and transport names never stand in for a pattern. Write "Command" and, separately if useful, "implemented as …".
- **Use the domain's words**, from the people who do the work, for events, commands, views and slices. Where a framework convention and the workshop's vocabulary disagree, the workshop wins.
- **Martin Dilger's State Change / State View vocabulary is not used here; the correspondence is kept at portfolio level as a crosswalk, not as synonyms.**

### Mapping to the Critter Stack

JasperFx describes a running system's slices with the same four words (`JasperFx.Events.EventModeling.SlicePattern`: `Command`, `View`, `Automation`, `Translation`; verified at JasperFx 2.69.3). A slice's pattern there is derived from the running application, never declared by hand. The usual realization on Wolverine and Marten:

| Dymitruk pattern | `SlicePattern` | Typical realization |
|---|---|---|
| Command | `Command` | An HTTP endpoint or message handler deciding against an aggregate and appending events (the aggregate handler workflow). |
| View | `View` | A projection (inline, live or async) or a queried document; the read side a screen or automation reads. |
| Automation | `Automation` | A handler triggered by a forwarded event or a schedule, issuing a command; a timer-driven loop for temporal automations. |
| Translation | `Translation` | A transport listener or outbound publisher at the service edge, mapping between a contract and internal events. |

The right-hand column is a starting point, not a rule. A slice is classified by what it does in the model, and the implementation follows.

---

## Common mistakes

- **Events named as commands** (`PlaceOrder` as an event) or with an `Event` suffix (`OrderPlacedEvent`).
- **A view with a field no event supplies.** An event or a Translation is missing.
- **A command with no screen and no automation.** The trigger is missing; add the wireframe or say "no screen: machine actor".
- **Two patterns in one slice**, usually a Command that quietly includes an Automation. Cut it in two.
- **Handler-shaped slices**, cut where a framework's transaction boundary falls ("atomic triple-emit") instead of where the behaviour changes.
- **A framework or transport word in the pattern slot.**
- **Mechanical events confused with decisions.** "The reservation expired" (a clock fired) and "the customer cancelled" (a person decided) are both events, with different authority and different consequences.
- **A downstream context modelled as the origin of upstream data.**
- **Scenarios that test infrastructure** instead of domain facts.
- **Status written into the model.**

## Facilitating with personas

When one person (or one person and an AI) runs a session, rotate through distinct voices so the model gets challenged:

| Voice | Leans on |
|---|---|
| Facilitator | Pace, small slices, one pattern per slice, the next step. |
| Domain expert | The words people actually use; what really happens. |
| Architect | Swim lanes, ownership, which slice is a Translation. |
| Developer | "How would we build that?"; what a field's source really is. |
| Skeptic (QA) | Failures, races, timing, refusals; the completeness check. |
| UX | What each screen needs to show, field by field. |

The storyboard (step 3) and the completeness check (after step 5) are where the Skeptic and UX voices earn their keep. If every proposal is accepted without a redirect, say so in the minutes: a session that only ratified is a finding.

## See also (external)

- Adam Dymitruk, ["What is Event Modeling?"](https://eventmodeling.org/posts/what-is-event-modeling/), eventmodeling.org, 2019: the primary source for the blocks, the four patterns, the seven steps and the completeness check.
- Marc Klefter and Jake Bruun's 2026 posts on translation decisions, agents as automations and temporal automation slicing.

---

## In this repository

Everything above copies between repositories unchanged. This section is CritterCab's.

**Where the model lives.** The authored model will be a curated `*.emodel.yaml` per service beside its `Program.cs` (none exists yet). New chapters are modelled on the EventModelers.AI canvas and the export is committed. The markdown files in [`docs/workshops/`](../../workshops/) are the **minutes** of modelling sessions, not the record ([`docs/workshops/README.md`](../../workshops/README.md)). Test classes are named by slice so they line up with slice names.

**Examples from the minutes** ([Workshop 001 — Dispatch](../../workshops/001-dispatch-event-model.md), [Workshop 004 — Onboarding](../../workshops/004-onboarding-event-model.md)):

- **Command with a wireframe:** W001 §5.1 `RideRequested`, triggered from the rider app's "Request a Ride" screen. It is the only slice in the minutes with a drawn wireframe; every slice modelled from now on carries one or a "no screen" statement.
- **Translation with a Klefter decision event:** W001 §5.2 `FareQuoted`. Dispatch asks Pricing for a fare and records the quote as its own event, rather than re-reading Pricing later.
- **Temporal automation (Bruun):** W001 §5.7 `OfferExpired`. The `OfferExpirer` automation reads the to-do view `OffersAwaitingExpiry*` and issues `ExpireOffer` when an offer's `expiresAt` passes.
- **Configuration as events:** W001 §5.11 `ConfigureDispatchPolicy` and W004 §6.10 `OnboardingPolicyConfigured`, each a Command slice appending to a single policy stream that earlier slices read.
- **Two patterns in one slice, to be re-cut:** W001 §5.5 (`OfferAccepted` with a sibling-revocation cascade "that subsumes what would otherwise be a separate Automation") and W001 §5.9 (two automations reaching one terminal event).
- **Framework words in the pattern slot:** W004 writes slice patterns as "Command (continue handler)" and "Translation-in (Klefter) + start-handler", and cuts §6.8 as an "atomic quadruple-emit". Those are Wolverine process-manager terms and a transaction boundary, not patterns; a re-model states the pattern alone.
- **What the completeness check would have caught:** W001 §5.2's reads list, which disagrees with the fields the slice uses (carried as an open inconsistency since the slice was built).

**Related:** [`domain-event-conventions`](../domain-event-conventions/SKILL.md) (naming events), [`marten-wolverine-aggregates`](../marten-wolverine-aggregates/SKILL.md) (Command slices), [`marten-projections`](../marten-projections/SKILL.md) (View slices), [`wolverine-marten-automation`](../wolverine-marten-automation/SKILL.md) (Automation slices), [`protobuf-contracts`](../protobuf-contracts/SKILL.md) (Translation contracts). Background reading: [`docs/research/event-modeling-canonical-sources.md`](../../research/event-modeling-canonical-sources.md), [`docs/research/agents-in-event-models.md`](../../research/agents-in-event-models.md), [`docs/research/event-modeling-workshop-guide.md`](../../research/event-modeling-workshop-guide.md).
