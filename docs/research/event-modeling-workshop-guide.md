# Event Modeling: A Guide for CritterCab's First Workshop

Curated research notes on Event Modeling as a workshop-driven system design methodology — what it actually is, how it is run, how to run it digitally, where AI and LLM agents fit in, and how its outputs map to the Spec-Driven Development and Narrative-Driven Development vocabularies CritterCab's vision doc already commits to. The focus is practical guidance: what to do the first time CritterCab runs an Event Modeling workshop, with enough theory behind each recommendation that departures from the guidance are deliberate rather than accidental.

Event Modeling was created by Adam Dymitruk of Adaptech Group. Adam is an online friend of the maintainer, which biases the source selection slightly — his own writing and talks are treated as primary, and the critical work of others in the ecosystem (Sebastian Bortz's cheat sheet, the Confluent course authored by Bobby Calderwood, the prooph board material, Alexandra Moxin's Linux.com piece) is triangulated against it. Where the ecosystem material contradicts Adam's original formulation, Adam's original is the tie-breaker.

---

## About this document

### What counts as a good source

Event Modeling has a smaller primary-source surface than Event Storming, which means the signal-to-noise ratio for search results is unusually clean. The sources here were tiered as follows:

- **Tier 1 — Adam Dymitruk's own writing, talks, and interviews.** The canonical *Event Modeling: What is it?* article on eventmodeling.org, the *Event Modeling Traditional Systems* follow-up, the SE Radio 539 episode with Jeff Doolittle, the InfoQ founder interview, the Semaphore interview, and the GOTO YOW! 2023 *Event Modeling from Beginner to Expert* recording.
- **Tier 2 — Adaptech Group staff and close collaborators.** Alexandra Moxin's *Why You Need to Know About Event Modeling* on Linux.com (CSO of Adaptech). Sebastian Bortz's *Event Modeling Cheat Sheet* on eventmodeling.org. Martin Dilger's material associated with the Event Modeling podcast and the prooph board product.
- **Tier 3 — Established practitioners using Event Modeling in production.** The Confluent *Practical Event Modeling* course authored by Bobby Calderwood. The prooph board wiki and product documentation (Alexander Miertsch and team). Oskar Dudycz's *event-driven.io* writing on vertical slices. James Eastham's *Event Modeling By Example*.
- **Tier 4 — Adjacent methodologies with explicit Event Modeling framing.** Sam Hatoum's Narrative-Driven Development material at narrativedriven.org and on.auto. The Xolvio NDK repository. Spec-Driven Development material from Thoughtworks, Augment Code, Zencoder, and the Panaversity research paper.

Secondary "what is Event Modeling" blog posts and LinkedIn summaries were read to triangulate and are not cited directly.

### How to read the "applicability to CritterCab" notes

Every lesson ends with a concrete note on how the insight lands in CritterCab's specific situation — a reference-architecture project in its design phase, staffed primarily by one engineer (who is also the maintainer), targeting 6 to 8 deployable services, committed to an NDD-informed narrative layer sitting between workshop output and prompt documents. The applicability notes take those facts seriously rather than describing what an idealized enterprise team would do.

---

## Primary sources consulted

eventmodeling.org — official site:
- *Event Modeling: What is it?* — Adam Dymitruk, 2019.
- *Event Modeling Traditional Systems* — Adam Dymitruk.
- *Event Modeling Cheat Sheet* — Sebastian Bortz, 2020.

Adam Dymitruk talks and interviews:
- *Event Modeling from Beginner to Expert* — YOW! Australia 2023.
- SE Radio Episode 539 — with Jeff Doolittle, 2022.
- InfoQ interview — *Interview with Event Modeling Founder - Adam Dymitruk*, 2020.
- Semaphore interview — *Adam Dymitruk on How to Upgrade Your Toolbox with Event Modeling*, 2024.

Adaptech Group and close collaborators:
- *Why You Need to Know About Event Modeling: An Intro* — Alexandra Moxin, Linux.com, 2024.

Confluent:
- *Practical Event Modeling* course — Bobby Calderwood. Introduction, Workshop module, and subsequent modules on brainstorming, plot formulation, storyboarding, commands, views, swim lanes, scenarios.
- *learn-practical-event-modeling* exercise repository — confluentinc/learn-practical-event-modeling on GitHub.

Prooph board ecosystem:
- prooph-board.com product pages (Remote Event Modeling, MCP integration).
- wiki.prooph-board.com — *Why Event Modeling*, *What is Event Storming*, *Prepare Big Picture Session*, *How To: Event Modeling*, *SDVB Cycle* documentation.
- Cody Play / Cody Engine repositories on GitHub.

Narrative-Driven Development:
- narrativedriven.org — origin story, principles.
- on.auto — Auto platform product material.
- xolvio/NDK repository on GitHub — *The Narrative Development Kit*.
- Sam Hatoum writing on Medium, LinkedIn, and X.

Practitioner writing:
- *Event Modeling By Example* — James Eastham, 2021.
- *Vertical Slices in practice* — Oskar Dudycz, event-driven.io, 2023.
- *Difference between Event Storming and Event Modelling* — Crafting Tech Teams substack, 2023.
- *Event Modelling Guide: Principles, Patterns & Best Practices* — pradhan.is, 2026.
- *Vertical Slices doesn't mean "Share Nothing"* — CodeOpinion (Derek Comartin), 2026.

Spec-Driven Development context:
- *Spec-driven development* — Thoughtworks, 2025.
- *What Is Spec-Driven Development?* — Augment Code, 2026.
- *A Practical Guide to Spec-Driven Development* — Zencoder.
- *Spec-Driven Development with Claude Code* — Panaversity, February 2026.
- *Spec-Driven Development Is Eating Software Engineering* — Vishal Mysore, Medium, March 2026.

Community artifacts:
- *awesome-eventmodeling* — MateuszNaKodach on GitHub.
- Event Modeling Open Space 2024, Berlin — session archives.

---

## Lesson 1 — What Event Modeling is, and what distinguishes it from Event Storming

Event Modeling is a methodology for describing an information system as a timeline of state changes, told as a story from the user's perspective. Its output is a visual blueprint that reads like a movie storyboard: screens across the top, commands and events in the middle arranged left-to-right in chronological order, views across the bottom, all tied together by arrows showing where each field originates and where it is consumed. The blueprint is the primary deliverable. Working software is downstream.

Adam Dymitruk's own summary, across the SE Radio interview and the InfoQ piece, is that he did not set out to invent a new methodology. Event Modeling is a distillation of techniques Adam and his collaborators had already been using at Adaptech Group for a decade, arranged in a way that makes them teachable. The three primary influences are explicit:

1. **Greg Young's long-running process specifications** from CQRS/ES work — specifically, the practice of drawing a system's behavior as commands producing events on a timeline.
2. **Alberto Brandolini's Event Storming** — specifically, the sticky-note-based collaborative workshop format.
3. **UI/UX storyboarding** — specifically, the convention of putting screens across the top of the diagram and treating the diagram as a movie's key frames.

The first thing to internalize is that Event Modeling and Event Storming are not the same thing and do not have the same purpose, despite sharing notation and vocabulary.

Event Storming is a discovery technique. It is intentionally chaotic, optimized for exploring the problem space, surfacing hotspots, and building shared understanding among people who have never talked to each other before. A Big Picture Event Storming is valuable even if nothing is built afterward. Alberto Brandolini's original framing is closer to a business design tool than a software design tool.

Event Modeling is a design technique. It is intentionally structured, optimized for producing a deliverable specification of how an information system will work. An Event Model is not useful until the loop closes: every field on every screen traces back to an event that produced it, every event traces back to a command that caused it, every command traces back to a user interaction or an automated trigger. The model is the blueprint for the work.

The Crafting Tech Teams comparison puts it cleanly: Event Storming builds trust between business and developers, while Event Modeling prioritizes parallel delivery tasks from highest to lowest value. A team can do both — many do, running an Event Storming Big Picture session first to explore and then transitioning to Event Modeling to design. They are complementary, not competing.

The second thing to internalize is that Event Modeling is not specifically about event sourcing. The methodology works for traditional CRUD systems, message-based systems, event-sourced systems, and anything in between. Adam wrote a dedicated *Event Modeling Traditional Systems* article specifically to make this point. What Event Modeling requires is that state changes in the system be describable as discrete facts that happened on a timeline. Whether those facts are persisted as events in an event store, written into database rows, or emitted as messages to a queue is an implementation concern that comes later.

That said, Event Modeling maps onto CQRS and event sourcing more naturally than any other methodology because its building blocks (events, commands, views) are the building blocks of CQRS/ES. The Critter Stack is opinionated toward event sourcing via Marten, which means Event Modeling output in a CritterCab workshop will slot into implementation with unusually little translation overhead — but that is a consequence of the stack choice, not a prerequisite of Event Modeling itself.

### Applicability to CritterCab

- **CritterCab's vision already names Event Modeling as the primary design tool and Domain Storytelling as a complementary technique.** This research supports that choice and adds nuance: if time permits, running a lightweight Event Storming Big Picture session first — even a solo one — would surface domain events and relationships faster than starting cold in the structured Event Modeling format. The CritterBids and CritterSupply Event Modeling workshops Erik has already run provide a baseline of facilitation experience that reduces the need for a warm-up Storming session.
- **The fact that Event Modeling works for non-event-sourced systems matters for CritterCab's service mix.** Payments is the clearest example: the vision doc flags Payments as the most likely Polecat-on-SQL-Server candidate specifically because transactional integrity dominates over event-sourcing flexibility. The same Event Model still describes Payments accurately; what changes is the implementation target for each slice, not the workshop output.
- **The blueprint-as-deliverable framing is the right one for CritterCab.** The project already commits to "Event Modeling first, code second" as a design principle. The practical interpretation is: the workshop is not successful until the board would be comprehensible to someone who wasn't in the room, and every field on every screen traces back to an event.

---

## Lesson 2 — The four building blocks have strict semantics

Event Modeling uses a deliberately small vocabulary. Sebastian Bortz's cheat sheet on eventmodeling.org is the canonical reference for the building blocks; Adam confirms the same set in every interview.

**Trigger (white).** A wireframe, a screen, or — in the case of an automated process — a gear or robot icon. Triggers describe where state changes start. For interactive flows, the trigger is the UI the user sees at the moment they want to change the system's state. For automated flows, the trigger is a scheduled process, an observed threshold, or an external signal.

**Command (blue).** A named intent to change state. Commands are always named in imperative form: *BookRoom*, *CancelTrip*, *SettleInvoice*. The command carries the data that the system needs to decide whether the intended change is allowed and, if so, what events to record. Commands can fail — they are the only place in an Event Model where rejection is possible.

**Event (orange, sometimes yellow).** A past-tense, immutable fact that happened. Events are named in past tense: *RoomBooked*, *TripCancelled*, *InvoiceSettled*. Events are what the system stores (whether literally as event-sourced records or logically as database state changes). They are the only durable content of the system's history. A field that cannot be derived from events on the timeline is a field the model is missing.

**View / Read Model (green).** A projection of events back into a shape that some consumer needs — a screen, a report, an automated process monitoring a todo list. Views are pure functions of the events upstream of them. Views cannot reject events. Views cannot produce events (that is what automations do, downstream of a view).

The arrangement on the board is rigid, by design. Reading left-to-right across the timeline gives a chronological story. Reading top-to-bottom through a single slice gives `UI → Command → Event → View → UI` — the canonical information loop. Swim lanes group related events, typically by aggregate or bounded context. Arrows show field-level data flow: where each piece of information on a screen originated and where it goes.

The most important discipline in step-one Event Modeling is resisting the temptation to mix these building blocks up. Three specific failures show up in every beginner workshop:

**Events named as commands.** *CreateOrder* is a command; *OrderCreated* is the event. Writing *CreateOrder* on an orange sticky betrays that the person is thinking in procedures, not in facts. The fix is immediate renaming, ideally with the facilitator gently asking "and after we created it, what do we say happened?".

**Commands named as events.** *OrderCreated* on a blue sticky is the inverse. The person is thinking about what they want to record without thinking about the user's intent that caused the recording. The fix is asking "what did the user actually do? They pressed a button — the button does what?".

**Views that can't be derived from the events present.** A screen shows the user's current loyalty points balance, but no event on the timeline talks about loyalty points. Either there is a missing event (*LoyaltyPointsEarned* is omitted), or the screen is showing data from a system outside the model's scope (in which case a Translation slice is missing — see Lesson 3). Views that cannot be derived from upstream events are the single most common sign of an incomplete model.

### Applicability to CritterCab

- **The strict semantics make the Event Model directly implementable in the Critter Stack.** Commands correspond to Wolverine message handlers. Events correspond to Marten event-sourced records (or, for non-event-sourced BCs, domain events emitted from handlers). Views correspond to Marten projections or Redis-backed read models. UI wireframes correspond to the BFF/frontend layer.
- **CritterCab's plan to use gRPC as a first-class transport does not disturb the building blocks.** A gRPC unary call still expresses a command (request) that produces events. A gRPC server-streaming call (dispatch offer fan-out to drivers) is a view delivered over streaming rather than pull. A gRPC client-streaming call (GPS ingest from driver app) is a sequence of commands writing events. A bidirectional stream is interleaved commands and views on the same channel. The transport is an implementation detail that the Event Model does not need to encode, which is exactly the insight behind Adam's point that the methodology is transport-agnostic.
- **For Erik specifically, the relevant anti-pattern to guard against is over-decorating the board with implementation detail during the workshop.** Noting which BC a swim lane belongs to is useful. Noting that a command produces a Kafka message is premature — the decision of "which transport" comes after the workshop, during prompt authoring and skill selection.

---

## Lesson 3 — The four patterns are the four slice shapes

A vertical slice in Event Modeling is one complete loop through the building blocks. The eventmodeling.org cheat sheet and the Confluent course both identify four canonical patterns; everything a real system does is one of these or a composition of them. Understanding the four patterns in advance saves hours in the workshop because participants stop asking "is this a new kind of thing?" every time a new flow appears — it's always one of the four.

**The Command Pattern.** Trigger → Command → Event(s). A user presses a button, the system decides whether the action is allowed, one or more events are recorded. This is the write-side slice. Every state change in the system is captured by a Command Pattern slice somewhere. Example: the rider submits a trip request, the Dispatch service validates the request and stores *TripRequested*.

**The View Pattern.** Events → View (read model) → Trigger that consumes it. Events that have already been stored are projected into a shape that a consumer needs, and some UI or process consumes that shape. This is the read-side slice. Every screen that shows non-trivial information is the downstream of a View Pattern slice. Example: the rider's "active trip" screen is a view projected from *TripRequested*, *DriverAssigned*, *DriverArrived*, and *TripStarted*.

**The Automation Pattern.** View → automated trigger (gear/robot) → Command → Event(s). The cheat sheet calls this out as the same shape as a Command Pattern slice, except the trigger is a robot rather than a user, and the view the robot consumes is typically a "todo list" — a read model projecting *work that needs to be done*. The robot observes the todo list, calls a command for each item, and the resulting event marks the item done (which the todo-list view reflects on its next projection). Example: a Dispatch offer that has timed out needs to be re-dispatched. The todo-list view projects offers past their acceptance deadline; an automation observes the list and calls a *ReDispatch* command; the resulting *OfferTimedOut* event removes the item from the todo list.

**The Translation Pattern.** External events or state imported or exported, with a mapping layer. Also called external state import and internal state export in the Confluent course material. A Translation is a slice that lives at the edge of the system and turns something foreign (another system's events, a third-party webhook, an incoming message on a different bus) into the internal vocabulary — or the reverse. Example: Microsoft Graph sending an Entra user-creation notification into ASB, where CritterCab's Identity BC receives it and records an internal *UserProvisioned* event.

The cheat sheet is explicit about one consequence of this decomposition: **every slice is a unit of work a developer can implement in isolation**. Adam uses this as the load-bearing argument for fixed-price estimation in the Semaphore interview: once a system is modeled, the number of slices is the unit of work, and each slice is roughly the same size. Estimation becomes counting.

The practical test for "is this a real slice?" comes from the jwilger agent-skills library and the Confluent course material: a slice's acceptance criterion must be observable at the system's external boundary. If the Given/When/Then you write for the slice can be satisfied by calling an internal function in a unit test, it is a component-level specification, not a slice. A real slice exercises the full path from external input (HTTP call, message receipt, scheduled trigger) to externally observable output (HTTP response, emitted event, projected view visible to a consumer).

### Applicability to CritterCab

- **The four-pattern decomposition is the right mental model for CritterCab's workshop.** Every flow the team draws will fit one of them; having that vocabulary in everyone's heads during the session saves the time that would otherwise be spent inventing taxonomies on the fly.
- **Translation slices are going to be more common in CritterCab than in CritterSupply or CritterBids.** The vision doc commits to Entra External ID, Microsoft Graph change notifications, ASB as a business-event backbone, and the Go polyglot service — every one of those is a Translation slice waiting to be modeled. For the first workshop, flagging Translation slices explicitly (and *not* trying to model the internals of the external system they translate from) keeps the scope contained.
- **The "every slice is roughly the same size" property is what makes CritterCab's session-driven workflow work.** The CritterBids prompt-document-plus-retrospective pattern already assumes each session is a bounded unit. Slices are those units, one or two per session. A workshop that produces 50 slices produces 50 sessions' worth of work, and the ADR/prompt/retro cadence scales to that naturally.
- **Automation slices are where Wolverine sagas earn their slot.** An Auction Closing saga in CritterBids is an Automation slice implemented as a Wolverine `Saga`. CritterCab's Trip lifecycle and Dispatch offer lifecycle will follow the same pattern — the todo-list view is a read model of open trips/offers; the saga is the robot; the saga's actions are commands. Recognizing this correspondence during the workshop lets the team draw the saga boundary on the Event Model directly, which the prompt-author can then lift into an implementation plan without re-inferring it.

---

## Lesson 4 — The seven-step workshop, canonically

Adam's original *Event Modeling: What is it?* article describes the workshop as seven steps. Alexandra Moxin's Linux.com piece reproduces the seven-step formulation. Some popularizations (including the workshop skill Erik already has loaded in Claude) compress the seven into four or five phases by merging steps that blend in practice. Both framings are legitimate; the seven-step version is canonical, and it is worth running CritterCab's first workshop with all seven steps named explicitly because naming them helps the facilitator know which question they are asking at any given moment.

**Step 1 — Brainstorming.** At least one representative from each involved function envisions how the system would look and behave, starting from screens or pieces of information they can imagine having happened. The facilitator introduces the concept of state-changing information. Events accumulate on the board without order, without judgment, without worrying about duplication. This is brain-dump phase; see Lesson 5 for the full treatment.

**Step 2 — Formulate the Plot.** The brain-dump events are arranged into a plausible left-to-right walkthrough. The team walks the timeline together and asks, at each step, "does this make a coherent story?". Gaps are missing events. Forks are alternative scenarios that either get their own parallel row or get noted for a later pass.

**Step 3 — Create the Storyboard.** Wireframes — even the barest black-and-white box sketches — are added above the timeline. For every screen, the team records the source and destination of each field the user sees. This is the step that forces the question "where does this data come from?", which is the step that surfaces missing events most aggressively.

**Step 4 — Identify What the User Can Do.** Commands (blue) are introduced as the inputs the user provides via each screen. Each command links the information the user enters to the events that get stored. This is when the model starts to look like an information system rather than a series of screens.

**Step 5 — Identify What the User Sees.** Views (green) are introduced as the read models that accumulate from stored events and project back into the UI. Every UI field that was not originally entered by the user is now traced to a view; every view is traced to the events feeding it.

**Step 6 — Apply Conway's Law.** Events (and by extension commands and views) are organized into swim lanes that reflect the organizational or team boundaries around the system. This is the step that turns an Event Model into a deployment map. Adam references Conway's Law (systems mirror the communication structures of the organizations that built them) as the organizing principle for the swim lanes: a lane per team, per bounded context, per subsystem, or per external integration — whatever the right unit of ownership turns out to be.

**Step 7 — Elaborate Scenarios for Testing.** Given-When-Then (or Given-Then for views) scenarios are written for every slice, collaboratively, at the board. Each scenario is tied to exactly one command or view. This is the step that turns an Event Model into an executable specification; see Lesson 7 for why GWT is the right format and Lesson 9 for what happens next.

The seven-step framing compresses into the familiar five-phase summary as follows: Brain Dump (step 1), Plot and Storyboard (steps 2 and 3 — hard to separate in practice, because adding wireframes reveals missing events on the plot), Commands and Views (steps 4 and 5 — often interleaved), Conway's Law (step 6), Scenarios (step 7). Either framing is fine; what matters is that the team walks through all seven activities at some point, in something like this order.

Two pieces of practical guidance from the Confluent course are worth lifting directly:

**The workshop itself is long.** Adam's own reference workshops run 4 to 8 hours for medium-size systems and multiple days for larger ones. The Confluent course cites 4 to 8 hours as typical and notes that facilitators who have run with as many as 50 participants agree that 25 is a better ceiling. For CritterCab, the right framing is multiple bounded sessions rather than one heroic marathon.

**The output is not a single artifact.** A finished Event Modeling workshop produces the event model (the board itself), a set of slice-scoped specifications, a cross-reference of UI fields to their sources, and implicitly a backlog of slices ordered by priority. Planning for all four output types from the start prevents the workshop from ending with a beautiful board and no plan for what to do with it.

### Applicability to CritterCab

- **For CritterCab's first workshop, all seven steps should be named out loud.** Erik is effectively both facilitator and sole domain expert for the first session (see Lesson 8 on solo and small-team workshops). Naming each step announces which question is on the table, which prevents the session from drifting between phases in a way that is easy when the facilitator is also the participant.
- **The "Apply Conway's Law" step is disproportionately important for CritterCab.** The vision doc lists 11 tentative bounded contexts collapsing to 6–8 deployable services, with the final mapping explicitly deferred to modeling work. Step 6 is exactly where that collapse happens: the Event Model's swim lanes *are* the service boundaries, and drawing them consciously is how Event Modeling decides what CritterCab's services are. Skipping or rushing step 6 would throw away the single clearest payoff Event Modeling has to offer this project.
- **Step 7 (scenarios) is where Event Modeling hands off to CritterCab's narrative and skill layers.** The GWT scenarios produced at the board become the seeds of narratives in `docs/narratives/`. That's where the NDD-informed workflow begins. See Lesson 12 and Lesson 13.

---

## Lesson 5 — Brain dump, demystified

The brain dump is step 1 of the seven-step workshop, and it is the single step that new participants find the hardest to do correctly. The phrase sounds like free-form ideation; the actual activity is narrower and more disciplined than that.

The brain dump has exactly one deliverable: **a population of candidate events, expressed as past-tense business facts, with no ordering, no deduplication, and no judgment**. The events go on orange stickies. They do not need to be complete sentences. They do not need to be unique. They do not need to be correctly spelled. They are raw material for step 2.

Three rules from Adam's *Event Modeling: What is it?* article and Alexandra Moxin's Linux.com walkthrough:

**Past tense, always.** *UserRegistered* is an event. *RegisterUser* is not an event, it is a command, and it belongs on a different colored sticky in a different phase. The discipline of past-tense naming is the discipline of thinking about what the system has recorded rather than what someone wants to do next.

**Business meaning, not technical implementation.** *LoginClicked* describes a UI mechanic, not a system fact. *UserAuthenticated* describes a fact. *RowInsertedIntoUsersTable* describes an implementation. *UserRegistered* describes a fact that matters to the business. When in doubt, ask: would a domain expert say this out loud in a meeting? If yes, it is a business event. If no, it is probably something else.

**Only state-changing information.** The facilitator's job (Lesson 6) includes gently stopping contributors from writing stickies for things that aren't state changes. "The user saw the home page" is not a state change; the user's visit to the home page did not change anything durable about the system. "The user placed an order" is a state change, because the system now has an order it didn't have before.

The single most useful facilitator technique during a brain dump is **the icebreaker event**. The prooph board wiki names this explicitly. The first event is the hardest because the group is still calibrating. The facilitator picks one, writes it on the board, and asks "what had to happen before this?" and "what happened next?". Those two questions, repeated, generate the second through fifth events, which generates group momentum, which breaks the silence. The icebreaker technique works for both remote and in-person workshops.

A second useful technique is **pre-seeded events from documentation**. For CritterCab specifically, the vision doc describes an 11-BC candidate list. Before the workshop, the facilitator can write one obvious event per bounded context on stickies in advance (*DriverOnboarded* for Onboarding, *GpsPingRecorded* for Telemetry, *TripRequested* for Dispatch, etc.). These are seeded on the board at the start of step 1, the group is told "these are placeholders, you can challenge any of them, and they are here to show you the vocabulary of events." Reading them breaks the blank-canvas anxiety and teaches the format by example.

A specific anti-pattern to avoid: treating the brain dump as a complete enumeration. The team does not need to generate every event the system will ever have during step 1. They need to generate enough to start formulating a plot in step 2. If step 2 surfaces missing events, those get added to the brain dump retroactively. The phases are not watertight.

### Applicability to CritterCab

- **For CritterCab's first workshop, the facilitator should pre-seed one or two obvious events per tentative bounded context.** The vision doc already named the BCs, which is a prerequisite most projects do not have at brain-dump time. Using that as a head start is legitimate; the brain dump is not a test of purity.
- **The brain dump is the phase where solo facilitation breaks down first.** Event brainstorming depends on multiple viewpoints generating events the others would not have thought of. See Lesson 8 on solo workshops; the short version is that Erik will need to rotate personas deliberately during step 1 to simulate the group dynamic.
- **Set a time box.** Adam's Confluent course framing is 30 to 90 minutes for a brain dump, depending on system size. CritterCab's first workshop, covering most of an 11-BC system at brain-dump depth, should probably budget 60 to 90 minutes for step 1 alone. A brain dump that runs too long produces diminishing returns; the board gets crowded with near-duplicates and the group loses energy.

---

## Lesson 6 — Slicing is the primary output, and a slice isn't a slice until it's vertical

Slicing — the decomposition of the event model into independently deliverable vertical cuts — is arguably the single most important output of an Event Modeling workshop. Everything else is inputs to slicing or consequences of it. The prooph board wiki says it explicitly: the design is made of bricks, the bricks are slices, and each slice takes roughly the same time to implement, which is what makes Event Modeling's estimation story work.

The Sebastian Bortz cheat sheet defines a slice as the smallest piece of work a developer can take on independently because it contains everything the developer needs to know. That's a test, not a definition. The actual definition comes from the four patterns in Lesson 3: a slice is one full instance of one of the four patterns. A Command slice is a trigger plus a command plus the events it produces. A View slice is the events plus the view plus the trigger that consumes it. An Automation slice is a view plus a robot trigger plus a command plus the events. A Translation slice is the external-to-internal mapping plus the internal recording.

The vertical in "vertical slice" is load-bearing. A slice spans every layer the implementation will have — presentation (the UI or external API), application (the command or view handler), domain (the business logic and aggregate), infrastructure (persistence, messaging, external integrations). A slice that only implements the domain logic without presentation or infrastructure is not a slice; it is a component. The jwilger agent-skills library and the Confluent exercises are both explicit on this point.

Oskar Dudycz's *Vertical Slices in practice* at event-driven.io adds nuance that matters at CritterCab's scale. Vertical slices do not mean "share nothing." A workflow that goes through multiple slices (dispatch → acceptance → trip start → trip completion) can share an aggregate, can share a domain model, can share infrastructure plumbing. What slices reject is *unexpected* sharing: a change unrelated to one slice affecting another slice's behavior. Derek Comartin's *Vertical Slices doesn't mean "Share Nothing"* at CodeOpinion says the same thing from a different direction: slices are about managing coupling, not eliminating it.

Four practical rules for slicing during a CritterCab workshop:

**A slice produces an observable behavior change at the system boundary.** If a reviewer cannot see, from outside the service, that the slice was deployed (either because a response shape changed, an event was emitted to a downstream topic, a view became available, or a UI element now renders), the slice is incomplete. This is the test the jwilger skill enforces.

**A slice names one command or one view, never both.** If the team finds themselves drawing a slice that contains both a command and a view, they are drawing two slices that happen to share an aggregate. Separating them is almost always the right answer.

**A slice fits in one session.** In CritterCab's terms, a slice should be implementable by one prompt document, closed with one retrospective, delivered as one PR. If a slice looks like it needs three sessions, it is probably three slices that were drawn too coarsely.

**Slices are orderable.** Once slices are identified, they are prioritized. The prooph board wiki calls this the SDVB Cycle (Specify, Design, Verify, Build). The workshop itself produces the candidate slices; a subsequent conversation — probably with a product lens on, not a modeling lens — orders them into a delivery sequence. For a reference architecture like CritterCab, the ordering criterion is different from a customer-driven product: the first slices should demonstrate the architectural spine (one slice that exercises gRPC unary service-to-service, one slice that exercises Kafka publish-and-consume, one slice that exercises event sourcing with Marten projections), because the architectural story is the product.

### Applicability to CritterCab

- **The slice-to-session mapping is the clearest win Event Modeling gives CritterCab.** CritterBids' prompt-document-driven workflow is already a session-shaped workflow; Event Modeling slices are session-shaped work items. The handoff from workshop output to session prompt is, in the happy case, mechanical: one slice becomes one prompt doc in `docs/prompts/`, referencing one or more narratives in `docs/narratives/`.
- **The "slice fits in one session" heuristic is a useful check against over-scoping.** CritterCab sessions are bounded by the same things CritterBids sessions are bounded by: context window, human attention span, and the need to write a retrospective at the end. If the workshop produces a slice that obviously doesn't fit that budget, it is two slices.
- **The architectural-spine framing is right for CritterCab's first slices but should be an explicit decision, captured in an ADR.** The first handful of slices should not be the most business-valuable ones (the domain is fake anyway); they should be the ones that demonstrate the architectural commitments — gRPC streaming, Kafka transport, Marten event sourcing, Wolverine saga orchestration — each in a single clear end-to-end path. Event Modeling identifies the candidates; slicing order is a separate decision.
- **The "not share-nothing" point applies specifically to Trips and Dispatch in CritterCab.** These two BCs will share concepts (the trip ID is the dispatch offer ID is the settlement reference). Sharing that aggregate key, sharing the concept of a trip lifecycle, and sharing some infrastructure plumbing is fine and expected. What is not fine is cross-slice coupling on behavior — Dispatch deciding what a completed trip means, Trips deciding what a dispatch timeout does. Slicing keeps ownership of behavior clear; the workshop is where those boundaries get drawn.

---

## Lesson 7 — Given-When-Then scenarios are the durable contract between the model and the code

Step 7 of the seven-step workshop — Elaborate Scenarios for Testing — is the step where Event Modeling output becomes executable. The Given-When-Then format is borrowed from BDD; the content is specific to Event Modeling and is what makes the output easy to transfer into code.

Two scenario shapes, one per slice type:

**Command scenarios.** *Given* a set of prior events establishing system state. *When* a specific command is invoked with concrete data. *Then* either a specific set of events is produced, or a specific error is produced — never both. This is the canonical write-side test. In event-sourced systems it maps directly onto testing an aggregate's `Decide` function: fold the prior events to build state, execute the command, assert on the resulting events.

**View scenarios.** *Given* a specific projection state. *When* a specific event arrives. *Then* a specific new projection state results. Views cannot reject events (if they could, they would be commands). This is the canonical read-side test, and it maps directly onto testing a Marten projection's `Apply` methods or a Wolverine handler that updates a read model.

Two rules distinguish GWT scenarios at Event Modeling granularity from BDD scenarios at feature granularity:

**Each scenario is tied to exactly one command or one view.** The *What Is It?* article emphasizes this. A scenario that says "Given X, when we do A and then B, then Y" is two scenarios. This discipline is what makes each scenario into a unit-of-work specification rather than a narrative test.

**Business rules go in scenarios, validation does not.** The jwilger agent-skills library articulates this clearly: if the type system can make an invalid state unrepresentable, that is not a scenario. Scenarios test *state-dependent* rules — you cannot settle an invoice that has already been settled, you cannot dispatch a driver who is already on a trip, you cannot complete a trip that was never started. Format and structure checks (the trip ID is a UUID, the price is non-negative) belong in the type system or in input validation, not in GWT scenarios.

For a Marten-plus-Wolverine project like CritterCab, GWT scenarios written at the workshop are almost trivially convertible into tests. The Alba library handles the external HTTP boundary; the Wolverine test harness handles message-handler invocation; Marten's event store supports seeding prior events. A well-written workshop GWT scenario for a command slice translates to an Alba-driven HTTP test that seeds the aggregate's prior events, posts the command over HTTP, and asserts on the emitted events — with no translation layer in between except mechanical ceremony. That is the specific reason Erik's CritterBids workflow has found Event Modeling output so useful: the scenarios become tests, the tests drive the implementation, and the retrospective closes the loop.

### Applicability to CritterCab

- **GWT scenarios from the workshop become the seeds of narratives.** In CritterCab's NDD-informed layering, a narrative is a journey-scoped spec; a journey contains multiple slices; each slice has one or more GWT scenarios. Writing scenarios at the board is the first draft of the narrative; polishing them into `docs/narratives/` after the workshop is the second.
- **The "exactly one command or view per scenario" rule is a check against fake slices.** If a workshop produces a slice whose GWT scenario cannot be expressed without chaining multiple commands, the slice is too coarse. Split it.
- **The business-rule-vs-validation distinction has concrete consequences in a C# 14 / .NET 10 codebase.** C# 14 has enough type-level expressiveness (records, non-nullable reference types, required members, collection expressions) that most validation can be made unrepresentable at compile time. The practical rule for CritterCab: if you find yourself writing a GWT scenario whose "Then" branch tests that the command returned "invalid format" or "missing field," go upstream and fix the type system instead. Scenarios should be testing whether *this action is allowed given the history*, not whether *this input is well-formed*.
- **The Wolverine testing patterns Erik has already codified in CritterBids and CritterSupply apply unchanged.** The `[WriteAggregate]` pattern, the `TestAuthHandler` and `AddTestAuthentication()` utilities, Alba integration tests — all of these already implement what workshop GWT scenarios need. The skill file work already done on CritterBids and CritterSupply is reusable for CritterCab.

---

## Lesson 8 — Who to invite, and how to handle the small-team / solo case

The Confluent *Practical Event Modeling* course, authored by Bobby Calderwood, lays out the participant criteria cleanly. A workshop needs **skill-set diversity** and **organizational-perspective diversity**, balanced against group size and the practical reality that every participant is taking time away from their regular work. The canonical list of skill sets to represent:

- Business subject-matter experts (the domain experts who know what actually happens)
- Product managers (the people who know why the system is being built at all)
- UI/UX designers (the people who know what the user should see)
- Software developers and architects (the people who will build it)
- Data scientists and analysts (where relevant — for CritterCab, Dispatch and Pricing specifically benefit from this voice)
- Operations, SRE, and security professionals (the people who will run it)

Organizational perspective matters separately from skill set. Someone needs to bring the perspective of *this system's immediate business value*, and someone else needs to bring the perspective of *how this system fits into the broader strategic context*. In a company these are often different people; on a reference-architecture project with one maintainer, they are the same person wearing different hats.

The size ceilings worth knowing:

- **Above 25 participants, the workshop becomes a theater rather than a working session.** Bobby Calderwood reports facilitating sessions of up to 50 people but says they work best capped at 25. The reason is simple: in a group of 50, most people don't contribute, and those who do contribute don't get challenged by others. The workshop loses its core mechanic, which is productive disagreement.
- **Below 4 participants, the workshop loses the multi-viewpoint property entirely.** This is where the small-team / solo case lives, and it needs deliberate compensation.

The compensation for small-team or solo workshops is **persona rotation**. The facilitator — who, in a solo workshop, is also the participant — deliberately switches roles and speaks from different perspectives. The event-modeling-workshop skill names five core personas worth rotating through:

- **Facilitator.** Keeps the workshop moving. Asks clarifying questions. Resolves naming disputes. Time-boxes each step.
- **Domain expert.** Owns the business language. Corrects event names. Challenges assumptions. Provides real-world examples.
- **Developer.** Thinks in implementation. Asks "how would we query that?" and "what triggers this?". Flags technical debt.
- **Skeptic.** Stress-tests the model. Asks "what if this fails?" and "what about the edge case where...?". Earns their keep in step 2 (storytelling) especially.
- **User / customer.** Grounds the design in reality. Asks "but why would I care about this screen?" and "what does this tell me?". Most active in step 3 (storyboarding).

The discipline is announcing the persona out loud (to yourself, to the session log, to the LLM collaborator if there is one) before each contribution. The announcement forces the mental switch that produces different answers. A solo Event Modeling session done this way is materially better than a solo session done in a single voice; it is not as good as a real group session but it is closer than intuition would predict. This is one of the places where the "multi-persona facilitation" mode that Claude supports fits naturally — see Lesson 11.

Two further participation notes worth internalizing:

**Observers are not participants.** The DSDM framework's Chapter 9 on workshops distinguishes observers (audit role, training role, or gather-background role) from participants, and specifies that observers do not contribute to content. The same distinction matters in Event Modeling. A note-taker who is not also a domain expert is an observer. A junior engineer who is attending to learn is an observer. Observers can sit in; they should not write on the board.

**The sponsor is usually not in the workshop.** The person who asked for the system to be built (a product VP, a department head, a client CEO) typically does not attend the working sessions. They attend the kickoff, they attend the close-out, and they receive the artifact. The working-session attendees are the people who will actually build and operate the system plus the domain experts they need to get it right.

### Applicability to CritterCab

- **For CritterCab's first workshop, Erik plays all five personas, rotating deliberately.** This is not a compromise, it is the method. The vision doc names Erik as the maintainer and does not assume a team; the workshop strategy needs to match that reality.
- **The persona rotation pattern pairs with LLM collaboration in a specific way.** See Lesson 11; the short version is that an LLM instance prompted to play the Skeptic or the Domain Expert persona is materially better than Erik playing both in quick succession, because the LLM does not know what Erik was just thinking.
- **If and when the CritterCab community grows — Discord contributors, fellow speakers, co-authors — the next workshop should be a real multi-person session.** Even a group of four (Erik plus three others) with genuinely different backgrounds (one UX-leaning, one ops-leaning, one DDD-leaning) would produce a materially different model than a solo session. The ride-sharing-lessons-learned document surfaces a specific angle here: the Uber/Grab/DoorDash material contains observations from people who have operated production ride-sharing systems, and borrowing their voice as a "what would the SRE have said?" persona is a legitimate facilitation technique.
- **For the record, Erik should not list himself as "the product owner" in the workshop artifact.** Nobody signs off on CritterCab's deliverables except Erik, which is true, but labeling the role in the output makes the artifact portable: someone else could pick up CritterCab later and the workshop output would still tell them who did what.

---

## Lesson 9 — The facilitator's job, and why it is distinct from the domain expert's

Facilitation is the largest underacknowledged skill in Event Modeling. Adam's interviews make this clear by omission: he talks a lot about the method but relatively little about the soft skills required to make the method work in a real room. The skill exists; it is just not what is being sold.

The DSDM framework, the NN/g workshop facilitation guide, and the prooph board Big Picture preparation page converge on the same set of facilitator responsibilities. The clearest formulation is that a facilitator is responsible for *the process*, while participants are responsible for *the content*. A facilitator who contributes content is doing the participants' job; a participant who takes over the process is undermining the facilitator. These roles can be held by the same person in a solo or small-team workshop — but the person needs to know which role they are in at any given moment.

The specific duties of the Event Modeling facilitator, synthesized across the sources:

**Design the process.** Decide how long each step takes, what tools are used, how the workshop is structured across sessions, what homework (if any) is assigned between sessions. This is preparation, not improvisation.

**Set and hold the frame.** At the opening, state what the workshop will produce, who is participating, what is and is not in scope, how disagreements will be handled. Refer back to the frame whenever the session drifts.

**Enforce the process.** The step the workshop is in is the step the workshop is in. If the team is in step 2 (plot) and someone wants to start writing commands, the facilitator politely defers commands to step 4. If the team is in step 5 (views) and someone wants to argue about whether an event name is right, the facilitator notes the concern in a parking lot and moves on. Steps are not watertight, but the facilitator is the person who decides when a step-jump is productive and when it is drift.

**Enforce the vocabulary.** When someone writes *CreateOrder* on an orange sticky, the facilitator says "let's check — that's what the user wants to do, which makes it a command. The fact that gets stored is *OrderCreated*." Vocabulary enforcement is gentle but constant.

**Manage time.** Every step is time-boxed. The facilitator is the person with the timer. Running 20 minutes over on the brain dump means 20 minutes less for scenarios; the facilitator is the one person tracking the trade.

**Manage participation balance.** In a group, some people talk too much and some not enough. The facilitator invites the quiet ones ("you've been listening for a while — what would you add?") and throttles the loud ones ("that's a good point, let's note it in the parking lot and hear from others"). In a solo workshop, the analogous move is persona rotation (Lesson 8).

**Manage the parking lot.** Every workshop generates issues the group cannot resolve in the current session: naming disagreements, scope creep candidates, edge cases that derail the plot. A visible parking lot (a dedicated area on the board or in the tool) is where these go. The facilitator's job is to put them there and come back to them. The parking lot is also the first draft of the workshop's follow-up questions.

**Produce the durable artifact.** The session ends with an artifact that is not "the whiteboard at the end of day two." The facilitator (or a delegated scribe) is responsible for exporting the board into a form that will outlive the session: a PDF, a set of markdown files, a versioned tool artifact. The pradhan.is *Event Modelling Guide* is explicit: an Event Model that only exists on a whiteboard does not survive the week after the workshop.

Two specific traps to avoid:

**The facilitator who is also the domain expert.** This is CritterCab's default situation. It is workable — persona rotation handles the split — but the trap is that the facilitator's domain knowledge causes them to skip steps mentally. "I know the answer, let me just write it down" is the pattern. The fix is to externalize every decision to the board before writing it, even when the board has an audience of one.

**The facilitator who neutralizes their own judgment.** The DSDM framework says the facilitator should be neutral to outcomes. In a solo or small-team workshop, neutralizing all judgment produces a workshop that cannot make decisions. The better frame for the solo case: the facilitator is neutral to *which answer wins*, not neutral to *whether there is an answer*. Forcing a decision is the facilitator's job; preferring a specific decision is not.

### Applicability to CritterCab

- **For CritterCab's first workshop, the facilitator role is explicitly one of the personas Erik rotates through.** The "Facilitator" persona in the event-modeling-workshop skill is the right shape for this. When Erik is in Facilitator mode, he is asking "what step are we in, what have we missed, what did we park?" — not "what's the right answer to this naming question?".
- **Session length and break cadence matter more in a solo workshop than in a group one.** A group workshop has natural interruptions (someone asks a question, someone goes to the bathroom, someone challenges an event name). A solo workshop has none of those unless the facilitator manufactures them. The prooph board wiki's guidance — 3-hour remote sessions maximum, 10-minute breaks every 50 minutes — is the right cadence for CritterCab solo sessions.
- **The parking lot should be captured as a dedicated section of the workshop artifact.** In CritterCab's document layering, the parking lot becomes either a follow-up-research artifact in `docs/research/` or an open-question list appended to the workshop output in `docs/workshops/`. The vision doc already has an "Open Questions" section; the workshop parking lot feeds into it.
- **Produce the durable artifact at session close, not "later."** This is session-close discipline. The CritterBids retrospective pattern already enforces this (the retro is part of the PR, not a follow-up). The workshop deliverable follows the same pattern: the workshop is not done until `docs/workshops/<n>-<slug>.md` is committed.

---

## Lesson 10 — Running it digitally: tools and tradeoffs

Adam's InfoQ interview is explicit that Event Modeling was designed to work remotely. The structured phase progression, the rigidity of the building blocks, and the handoff format (slices as units of work) all happen to be properties that hold up under remote facilitation better than Event Storming's more exploratory conventions do. Running Event Modeling digitally is not a compromise; it is one of the supported modes.

The tooling landscape in 2026 has three clear tiers.

**General-purpose whiteboards.** Miro, Mural, and Lucidspark are the market leaders. They give you infinite canvas, sticky-note objects, color palettes, arrow-drawing, and multi-user real-time cursors. The prooph board wiki's own history of remote Event Storming notes that early experiments in 2019 ran successfully on what was then called realtimeboard (now Miro) and that for occasional Event Modeling sessions a general-purpose whiteboard is sufficient. The tradeoffs are predictable: no enforcement of Event Modeling conventions, no structured export (the artifact is an image or a link to a board), no integration with code or tickets.

**Event-Modeling-specific tools.** prooph board is the most mature. The tool is explicitly built around Event Modeling conventions (cards have types corresponding to the building blocks; slices are first-class objects; the tree view structures slices as chapters). It integrates with Cody Play, a prototyping sandbox that runs an in-browser approximation of the modeled system directly from the board. It exposes an MCP server for integration with AI coding agents (see Lesson 11). It supports an SDVB (Specify, Design, Verify, Build) cycle explicitly in the workflow. The tradeoff is cost and learning curve: it is a paid product, and the tool's own vocabulary layers on top of Event Modeling's vocabulary.

**Local / offline tools.** Excalidraw, draw.io / diagrams.net, and even plain markdown with ASCII can support Event Modeling. The tradeoff is also predictable: zero collaboration, no real-time sharing, and the discipline of staying on-pattern falls entirely to the modeler. For a solo workshop where the modeler already knows the patterns, this can be fine, and the output is version-controllable alongside code, which general-purpose whiteboards are not.

Three pieces of practical guidance from the prooph board and DDD Practitioners material that apply across tool choices:

**Preparation is more important remotely than in-person.** A physical workshop has a natural warm-up (people arrive, get coffee, chat, settle). A remote workshop opens with "can you see my screen?" and then the first event. Pre-seeding the board with structure (frames for each step, a parking lot area, an agenda card, a "who is here" area with name labels), sending the basic-concepts link with the invitation, and writing the goals of the session into the first frame all compress the warm-up time that remote sessions lose.

**Shorter sessions, repeated.** A remote session should not run longer than 3 hours. Participants lose focus faster remotely than in person; the prooph board wiki calls this out specifically. Splitting a workshop across multiple half-day sessions is not only acceptable, it is preferable, and the break between sessions ("sleep on it and come back") tends to surface events and edge cases that a single long session would not have produced.

**Asynchronous contribution is a feature, not a compromise.** Between sessions, participants can review the board, add stickies, flag issues. A good digital tool supports this directly; a general-purpose whiteboard supports it acceptably. The synchronous session then focuses on resolving disagreements and making decisions rather than on the brain-dump-style activities that async handles fine.

### Applicability to CritterCab

- **For CritterCab's first workshop, Miro with the free tier is probably the right starting point, not prooph board.** The reason is specifically that Erik is running this solo: the real-time collaboration features prooph board is optimized for are not used, and the annual cost of a paid tool that one person uses intermittently is hard to justify for a reference-architecture project. Miro's free tier supports enough canvases and stickies for CritterCab's scope, and the discipline of staying on-pattern is Erik's anyway, not the tool's.
- **The second-workshop choice is a live decision.** If CritterCab gains collaborators, if Erik is running public workshops at conferences (the Nebraska Code / KCDC / Explore DDD circuit), or if the prooph board MCP integration starts demonstrably saving time in the prompt-author handoff, the case for prooph board becomes stronger. Parking that decision is fine; making it early is premature.
- **The workshop artifact committed to `docs/workshops/` is a markdown file, not a link to a board.** This is explicit: the board (wherever it lives) is a working surface; the `docs/workshops/<n>-<slug>.md` file is the durable artifact. That file contains the event list, the command list, the view list, the swim-lane assignments, the slices, the scenarios, and the parking lot — all as text. A board export (PDF, PNG) can be linked from the file but is not a substitute for the file. This matters because the downstream consumers of workshop output (narrative authors, prompt authors, LLM agents) read text, not images.
- **The session structure should be three-hour remote sessions spanning multiple days for CritterCab's first workshop.** A practical cadence: Session 1 (steps 1–2, brain dump and plot), Session 2 (steps 3–5, storyboards, commands, views), Session 3 (steps 6–7, swim lanes and scenarios). Each session closes with a short retrospective and a durable artifact update; the full workshop closes with the final `docs/workshops/` commit and an ADR for any architectural decisions the swim-laning surfaced.

---

## Lesson 11 — AI/LLM-assisted Event Modeling is happening, and the shape is emerging

As of April 2026, AI assistance in Event Modeling is past the "possible in principle" stage and into the "product is shipping" stage. Three distinct modes have emerged from the current crop of tools, and they are largely complementary — not competing — approaches.

**Mode 1: Conversational facilitation assistance.** An LLM instance acts as a collaborator during the workshop itself, playing one or more of the personas from Lesson 8. The event-modeling-workshop skill Erik already has loaded in Claude is explicitly designed for this mode: Claude plays Facilitator, Domain Expert, Developer, Skeptic, or User/Customer on request, with each persona announced and the shift in perspective deliberate. The value of this mode is that a solo modeler gets productive disagreement and multi-viewpoint brain-dumping that would otherwise require actual other humans.

**Mode 2: Model generation from natural-language description.** An LLM converts a textual description of a domain (a vision doc, a business requirements document, a transcript) into a first-draft Event Model. Xolvio's Auto platform ships this as its "Modeling Agent" feature (in development at the time of this research); prooph board's MCP integration allows Claude Code, Codex, or GitHub Copilot to generate model content directly into a board via the Model Context Protocol. The Auto platform additionally advertises a "Hot Mic Mode" that listens to a workshop conversation and extracts events, commands, and views in real time. The value of this mode is speed: a rough first pass that a human refines is faster to produce than starting from blank.

**Mode 3: Slice-to-code generation.** An LLM reads a completed Event Model (including GWT scenarios) and produces implementation code that satisfies the slice. This is where Auto's headline feature lives — its tagline is that it delivers vertical slices from an event model directly to production. prooph board's MCP API is pitched to AI coding agents for the same purpose. In the Critter Stack's native workflow, a prompt document in `docs/prompts/` plus the relevant skill files in `docs/skills/` plus the narrative in `docs/narratives/` already constitutes the inputs for this mode; Claude Code or a similar agent produces the code.

Three caveats worth keeping in mind:

**The generated-model-from-text mode produces a first draft, not a final model.** The Thoughtworks Spec-Driven Development post makes the same observation for code: LLM-generated specs need human review and iteration. The same applies to LLM-generated Event Models. Treat the LLM's output as the team's first-pass brain-dump, not as the artifact.

**The generated-code-from-model mode depends on the model being complete.** The jwilger agent-skills library, which has a particularly disciplined take on this, treats incomplete models (missing GWT scenarios, undefined automations) as blocking gates — the agent refuses to generate code until the model is complete. This is the right posture; it prevents an agent from filling in gaps with plausible-looking code that violates invariants the model never specified.

**The persona-facilitation mode does not replace actual domain expertise.** If an LLM is playing "Domain Expert" and the real domain is ride-sharing, the LLM's answers are drawn from whatever ride-sharing material was in its training data plus whatever context has been loaded. The ride-sharing-lessons-learned.md file already in `docs/research/` is exactly the kind of context that makes LLM Domain Expert mode materially better. For a novel or proprietary domain, LLM domain expertise is shallow and should be challenged aggressively.

Two specific workflow patterns that have emerged:

**The LLM-as-Skeptic pattern.** After a human draft of an Event Model, an LLM is prompted to review the model as Skeptic: "what edge case is missing? where does this model fail under concurrent access? what event is implied by the vision doc but not on the board?". This is the highest-leverage use of an LLM for Event Modeling today and is usable with any tool, not just Event-Modeling-specific platforms. Claude's *event-modeling-workshop* skill supports this mode directly.

**The MCP-connected board pattern.** prooph board exposes an MCP server that lets an AI coding agent read the current model state and commit generated code back against the slice it came from. The workflow is: the human models on the board, the agent reads slices, the agent generates code for one slice at a time and updates slice status on the board. This is effectively Mode 3 wired into a tool that maintains two-way traceability between model and code. CritterCab's prompt-and-retrospective session pattern is manually doing the same thing; the difference is that the manual pattern does not currently update the source model when implementation reveals modeling gaps.

### Applicability to CritterCab

- **Claude's event-modeling-workshop skill is the current best fit for CritterCab.** Erik already has it loaded. Using it during CritterCab's first workshop, with explicit persona rotation, is the highest-leverage AI use available for this project right now.
- **The LLM-as-Skeptic pattern should be applied between sessions.** After each 3-hour session, the session's board is exported to a markdown summary, Claude is prompted as Skeptic to review it, and the skeptic's questions go into the parking lot for the next session. This is zero additional tool overhead (Claude is already in the workflow) and it compensates for the solo-workshop lack of productive disagreement.
- **The prooph board MCP integration is interesting but premature for CritterCab's first workshop.** The value of the MCP integration is in multi-session, ongoing model-to-code traceability; CritterCab has not yet generated any code, which means there is nothing for the MCP to bridge to. Reconsidering this decision after the first 3–5 slices are implemented is reasonable; committing now is not.
- **The Auto platform and the xolvio/NDK are worth studying as reference material, not adopting.** Auto is a commercial product with its own opinions about what a specification looks like (it does not use Event Modeling's four building blocks directly; it uses its own Narrative notation). The NDK is TypeScript and targets a different runtime than the Critter Stack. CritterCab's methodology is NDD-*informed*, not NDD-implemented — see Lesson 13.
- **For solo sessions, Claude-as-Domain-Expert with ride-sharing-lessons-learned.md in context is a real capability.** The research document already in `docs/research/` carries a lot of Uber/Grab/DoorDash domain knowledge. Loading it into Claude's context during an Event Modeling session and then invoking the Domain Expert persona produces responses grounded in how real ride-sharing systems actually behave, which is materially better than Claude's base training alone.

---

## Lesson 12 — Mapping Event Modeling output to Spec-Driven Development

Spec-Driven Development (SDD) coalesced as a named methodology during 2025 and accelerated hard in 2026. GitHub's Spec Kit (72,000+ stars on GitHub as of the early-2026 count in the Loadsys.com survey), AWS's Kiro IDE, Thoughtworks' December 2025 essay, the February 2026 Panaversity research paper on SDD-with-Claude-Code, and Vishal Mysore's March 2026 Medium piece mapping 30+ frameworks all point to the same shift: the industry has decided that the answer to "how do we use AI coding agents in production?" is to put a structured specification in between the human's intent and the agent's code. "Vibe coding" has become the pejorative for the old pattern; SDD is the replacement.

The core insight of SDD is not new to people who have done BDD, TDD, DDD, or Event Modeling. What is new is that the specification is no longer primarily for other humans or for automated tests — it is for the AI agent that will produce the code. This shifts what a good specification looks like. The Augment Code guide articulates the six elements every SDD spec needs: outcomes, scope boundaries, constraints, prior decisions, task breakdown, and verification criteria. The Zencoder guide adds that a spec is a *contract*: the agent is bound to the scope, and the human is bound to writing a spec before the agent starts.

Event Modeling output maps onto the SDD vocabulary almost one-to-one, with one qualification.

**Event Model slices are SDD "tasks."** Each slice is one unit of work that produces an observable change at the system boundary. This is the same unit SDD tools call a task. The Augment Code spec's "task breakdown" element is what Event Modeling produces as an inherent output of step 6 (Conway's Law) and the slicing process.

**Event Model GWT scenarios are SDD "verification criteria."** Each slice's scenarios are testable, have Given-When-Then structure, and tie to exactly one command or view. They are the slice's acceptance criteria. The Augment Code spec's "verification criteria" element is what step 7 produces directly.

**Event Model swim lanes and the broader system blueprint are SDD "prior decisions" and "constraints."** When a slice's prompt is handed to an agent, the agent needs to know what bounded context it is in, what aggregate it belongs to, what upstream and downstream services exist. The swim-lane structure of the Event Model captures this. Cross-BC contracts, ACL patterns, and transport choices are SDD constraints; they live in the architecture documentation and are referenced from each slice's spec.

**Event Model blueprint-as-a-whole is SDD "scope boundaries."** The board's edges are what's in scope; everything outside the board is either "out of scope for now" (with a parking lot entry) or "out of scope forever" (with an explicit non-goal statement).

The qualification: Event Modeling does not natively capture *outcomes* in the SDD sense. An Event Model describes *how the system behaves*; it does not describe *why building this slice is worth doing*. The Thoughtworks Spec-Driven Development essay is explicit that a good spec carries the business motivation for the work, not just the behavioral description. For CritterCab, this means that a slice's prompt document needs to include, in addition to the Event Modeling artifacts, a concise statement of what the slice demonstrates about the Critter Stack — because CritterCab's product *is* the architectural story, so "this slice demonstrates X" is its outcome.

Two practical patterns that emerge when you combine Event Modeling with SDD:

**The slice-to-prompt pipeline.** For each slice identified in the workshop, a prompt document is authored that contains: (a) the slice's GWT scenarios verbatim from the workshop, (b) the architectural context (which BC, which aggregate, which transport), (c) the outcome statement, (d) a reference to the relevant skill files in `docs/skills/` for implementation patterns, (e) a reference to the relevant narrative in `docs/narratives/` for journey context. This is exactly the shape CritterBids' prompt docs already have; Event Modeling just identifies which prompts to author and in what order.

**The spec-as-source vs spec-anchored spectrum.** The early-2026 SDD arXiv paper (referenced by Loadsys.com) defines three levels of spec rigor: spec-first (spec written before code, but code is authoritative), spec-anchored (spec and code coexist, spec is reference), spec-as-source (spec is authoritative, code is derived). Event Modeling output plus the Critter Stack's skill-file discipline lands in the spec-anchored regime: the Event Model is the architectural reference, the code is what runs, and the two are kept in sync via retrospectives rather than automated derivation. This is deliberate. Spec-as-source (Auto, Amazon Kiro) requires a commercial platform or an ecosystem lock-in that CritterCab's reference-architecture mission does not justify.

### Applicability to CritterCab

- **The slice-to-prompt pipeline is already most of what CritterBids does.** CritterCab inherits the pattern. What Event Modeling adds is the identification of slices in bulk at a workshop, rather than slice-by-slice during prompt authoring, which is how CritterBids discovered them. A good workshop produces a backlog of prompts; prompt authoring then becomes adaptation and ordering rather than invention.
- **CritterCab operates in the spec-anchored regime and should name that as a design decision.** An ADR — say, ADR 006 — stating "CritterCab uses spec-anchored development: the Event Model and narratives are the architectural reference; code is authoritative for runtime behavior; drift is detected by retrospective, not by automated derivation" would be worth writing early. It closes off the ambiguity about whether future contributors should expect the Event Model to be kept rigorously in sync (the answer is: yes, via retrospectives, not via tooling).
- **The outcome element missing from Event Modeling needs to be added explicitly to each prompt.** This is a small addition to the existing prompt template (already documented in `docs/prompts/README.md`): a one-sentence "what this slice demonstrates" line near the top. Without it, slices risk being prioritized by readiness-to-implement rather than by their contribution to the architectural story.
- **SDD's "task breakdown" maps exactly onto the slice list from the workshop.** This saves CritterCab from importing another tool's task-breakdown format; the Event Modeling swim-lane-and-slice output is the format.

---

## Lesson 13 — Mapping Event Modeling output to Narrative-Driven Development

CritterCab's vision doc (v0.2) explicitly commits to an NDD-informed approach. NDD was created by Sam Hatoum at Xolvio, grew out of Xolvio's XSpecs platform work from 2016 onward, and became the product framing behind Xolvio's Narrative and Auto platforms in 2024–2025. NDD is not in opposition to Event Modeling; it is downstream of Event Modeling in CritterCab's document layering. Understanding exactly what NDD adds and where the seams are matters because it determines what belongs in `docs/workshops/` versus `docs/narratives/`.

The NDD core thesis, from narrativedriven.org's origin story: modern teams suffer from information asymmetry — every team member holds a fragment of truth, and no methodology reliably puts those fragments back together. BDD, DDD, Event Storming, Specification by Example, and User Story Mapping each solve part of the problem; none of them thread the fragments into a complete picture. NDD's answer is to frame requirements as *narratives*: sequences of moments through time, told from the user's perspective, where each moment has context (what's true so far), interaction (what the user does), and response (what the system does back). The narratives form a complete picture when read aloud.

The practical NDD artifact is a structured, machine-parseable specification that serves as the single medium for all collaboration — visual for designers, textual for product teams, code-generation input for developers. Xolvio's commercial offering (Narrative/Auto) implements this as a Zod-backed schema; the open-source xolvio/NDK on GitHub implements it as a TypeScript DSL. CritterCab adopts the principles but not the specific format (the vision is explicit on that choice).

Event Modeling and NDD are compatible — they attack the same problem from different directions, and the overlap is substantial.

**Event Modeling is system-centric.** It describes how the system behaves on its timeline. The user is represented by the screens they interact with and the commands they issue; the user's story is implicit in the left-to-right reading of the timeline.

**NDD is user-centric.** It describes what the user is trying to accomplish, across moments in time. The system is represented by the responses it produces to the user's interactions; the system's internal state changes are implicit in the responses.

Read together, Event Modeling and NDD describe the same thing from two angles: Event Modeling's blueprint and NDD's narrative are *projections* of the same underlying truth. A well-formed Event Model plus a well-formed NDD narrative agree on every moment: the narrative's "user does X, system shows Y" corresponds to the Event Model's "command X produces events that update view Y." This is not an accident; NDD was designed aware of Event Modeling and lists EventStorming as one of its direct ancestors.

The specific mapping from Event Modeling output to CritterCab's NDD-informed narrative layer:

**A narrative spans multiple slices.** A single Event Modeling slice is one command or one view; a user journey typically involves several in sequence. *A rider books, is matched, watches the driver approach, is picked up, rides to their destination, pays, and rates* is one narrative spanning many slices (TripRequested → DriverAssigned → DriverArrived → TripStarted → TripCompleted → PaymentCaptured → RatingSubmitted). The narrative is the story; each slice is a moment within it.

**A narrative's moments are NDD's "context/interaction/response" triples.** Context is what the Event Model's prior-event history establishes. Interaction is the command (for user-initiated moments) or the system trigger (for automated moments). Response is the events produced and the resulting view state. The same information is present in both vocabularies; the NDD format puts the user's perspective first, while the Event Model puts the timeline first.

**A narrative includes cross-cutting concerns the Event Model leaves implicit.** Error states, accessibility requirements, internationalization, performance expectations, telemetry needs — these are often not drawn on the Event Modeling board (they don't have a natural sticky color) but belong in the narrative. This is one of the specific additions NDD makes: it is a better home for the "the user also needs" conversation than the Event Model is.

**Narratives are durable; Event Models drift.** The vision doc's "Capture intent in durable, structured form" design principle applies here. An Event Model of a system evolves as the system evolves; an old snapshot is not particularly useful. A narrative of *what the user was trying to do* is more stable, because user intent tends to outlive implementation changes. CritterCab's narrative layer is expected to be the longest-lived of the documentation artifacts.

Two workflow patterns emerge from the combination:

**Event Modeling first, narratives second.** The workshop produces the Event Model. From the Event Model's slices and swim lanes, user journeys are identified and written up as narratives. The narratives cite the slices they span; the slices, via prompts, cite the narratives they implement. The NDD-informed layer lives between workshop output and prompt documents, which is exactly what the vision doc says.

**Narratives evolve; Event Model doesn't.** Once narratives are written, most refinement happens at the narrative level — new moments get added, edge cases get captured, cross-cutting concerns get noted. The Event Model is updated when a slice's shape changes structurally (a command splits into two, an event is renamed, a swim lane moves). Day-to-day evolution is narrative evolution.

### Applicability to CritterCab

- **The `docs/workshops/` and `docs/narratives/` directories serve distinct purposes, and the distinction follows the mapping above.** Workshops produce point-in-time snapshots of the Event Model and the slice set. Narratives are the durable journey-scoped specs that persist and evolve. This is already the vision's intent; the research confirms it.
- **For the first workshop, a small number of target narratives should be identified during step 7 (scenarios) and sketched at the board.** Candidate narratives for CritterCab: *Rider requests and completes a trip* (the spine), *Driver goes online, accepts an offer, completes, and gets paid*, *Operator suspends a driver after complaint*, *System surge-prices during a demand spike*. Each of these narratives spans multiple slices and multiple BCs, which is exactly what narratives should do.
- **The format decision is a live open question in the vision doc and should remain live.** Options considered: freeform markdown with structured headers (like the ride-sharing-lessons-learned.md format but with sections for moments), Gherkin-flavored templates, schema-backed formats like Xolvio's Narrative. The research does not force a decision; running one or two narratives in a couple of candidate formats and seeing which holds up in practice is the right move. Resolving the format is a good retrospective topic after the first narrative is authored.
- **The xolvio/NDK library is not the right target runtime for CritterCab**, but reading its code is worthwhile because it demonstrates how the NDD principles translate into types, decorators, and handlers. The Critter Stack (Wolverine, Marten) already provides equivalent primitives; CritterCab does not need the NDK. The principles transfer; the library does not.

---

## Common mistakes to catch during CritterCab workshops

The pradhan.is *Event Modelling Guide*, Daniel Whittaker's *6 Code Smells with your CQRS Events*, the Kurrent *What's in an (event) name?* post, and the event-modeling-workshop skill converge on a compact list of failure modes that show up in almost every Event Modeling session. This is the list to scan during the workshop and during retrospectives.

**Events named as commands, and vice versa.** Covered in Lesson 2. The fix is automatic renaming and a short explanation of why.

**Events named as database or UI operations.** *FormSubmitted* is a UI event, not a domain event. *UserRecordUpdated* is a database operation, not a business fact. The fix is asking: what actually changed from the business's perspective? *UserEmailChanged*, *UserAddressCorrected*, *UserPreferenceSet* are business facts; pick the right one.

**Events that are too granular.** *FirstNameUpdated* and *LastNameUpdated* as separate events when the user changes both at once in a single interaction. The event should match the grain of the business fact: *NameUpdated* with both fields. Over-granularity creates noise and makes GWT scenarios unnecessarily complex.

**Events that are too coarse.** *CustomerDataChanged* without specifying what changed. A consumer of this event cannot determine what to do; they must re-derive all state every time. The fix is identifying the specific facts the business cares about and naming each: *CustomerAddressUpdated*, *CustomerPhoneUpdated*, etc.

**Views that cannot be derived from events on the board.** Covered in Lesson 2. The fix is either adding the missing events (most common) or recognizing that the view reads from an external system and adding a Translation slice.

**Slices too large to deliver independently.** A slice that says "a rider books a trip and gets matched and takes the ride and pays" is five slices, not one. The test is Lesson 6's: can this fit in one session? If not, cut it.

**Scenarios that test infrastructure rather than behavior.** A scenario that says "Given the Postgres container is up, when the handler runs, then the row appears in the database" is testing infrastructure plumbing, not the domain. Domain scenarios stay at the command-and-event layer; infrastructure tests are a separate concern.

**Parking-lot items that never come back.** Every workshop generates parking-lot items. A lot of them get forgotten. The fix is a workshop-close retrospective item: walk the parking lot, decide what becomes an open question, what becomes a follow-up research task, what becomes a scheduled-for-next-session item, and what can be dropped.

**Premature commitment to implementation detail.** Writing "this event goes to the Kafka topic `rides.events`" on the Event Model board is premature. The transport decision is not a modeling concern. The fix is noting transport/persistence in a separate area (the swim lane's metadata, an implementation-notes column) rather than on the events themselves.

**Treating the workshop as a one-shot deliverable.** The model lives. It evolves. A workshop that produces a perfect snapshot and then gets filed away has wasted most of its value. The fix is the retrospective discipline: every session's retro asks "what did the implementation of this slice teach us about the model?".

**Failing to write the artifact before the energy fades.** The Event Model in the facilitator's head is not the deliverable. The file committed to `docs/workshops/` is. If the workshop ends and the file is not written that day, the cost of writing it doubles every day thereafter. The fix is closing each session with the artifact write, not with the last discussion.

---

## What NOT to copy from the broader Event Modeling ecosystem

A short list of things that are popular in Event Modeling discussions but are actively wrong for CritterCab at its scale and purpose.

**Don't buy a commercial AI-SDD platform for the first workshop.** Auto, Kiro, and similar platforms are impressive and are solving real problems at team-and-enterprise scale. For a solo reference-architecture project, the commercial cost is not justified and the platform lock-in would undermine the open-source reference-architecture goal. Claude with the event-modeling-workshop skill plus markdown-in-git is sufficient.

**Don't adopt the xolvio/NDK library.** It is TypeScript, it targets a different runtime than the Critter Stack, and it implements NDD in a way that is opinionated about its own abstractions. CritterCab is NDD-*informed*, not NDD-implemented. Read the README; don't take the dependency.

**Don't run a 50-participant workshop.** Even if such a group could be assembled (which it cannot for CritterCab), the workshop would not be more productive than a group of 10. The Confluent course material is clear that 25 is the ceiling; real productivity lives at 5 to 15. Plan for the small group.

**Don't try to complete the Event Model in one day.** For anything larger than a single-slice demo, a full Event Modeling workshop takes multiple sessions. Scheduling three half-day sessions across a week produces a better model than one ten-hour marathon, and the between-session time lets missing events surface naturally.

**Don't insist on the prooph board tooling for a solo workshop.** The tool is excellent for teams; its value for a single modeler is largely stylistic. Use the free tools first; revisit if and when collaboration scales.

**Don't let the board replace the git-committed artifact.** A whiteboard or a Miro link is ephemeral. A markdown file in `docs/workshops/` is the artifact. Export discipline matters more than modeling tool discipline.

**Don't skip the swim-lane step.** Step 6 (Apply Conway's Law) is the step most often rushed because everyone is tired by then. For CritterCab specifically, this is the step that decides what the services are. Skipping it or rushing it undoes the single biggest payoff the methodology has for this project.

**Don't write GWT scenarios for every possible edge case before the first slice is implemented.** Scenario-writing is high-leverage but has diminishing returns past the "happy path plus the two or three most obvious failure modes." Additional edge cases surface during implementation and should be added to the scenario set in the retrospective. Writing all possible scenarios up front is Big-Up-Front-Design in a new costume.

---

## Guidance for CritterCab's first workshop

The following is a concrete, opinionated plan for CritterCab's first Event Modeling workshop. The plan is based on the research above; it is not the only valid plan. Deviate deliberately, not by accident.

**Scope.** The whole system at brain-dump-plus-plot depth, Auctions-style deep dive into one bounded context (Dispatch is the strongest candidate, because Dispatch is the gravitational center of the ride-sharing-lessons-learned research and exercises gRPC most heavily). Other BCs get event-level and command-level coverage; only Dispatch goes all the way to GWT scenarios and slice identification in the first workshop. This is the same strategy CritterBids used (whole-system brain dump, then deep dive into Auctions BC), and it works.

**Session structure.** Three half-day sessions across one week, 3 hours each, with a fourth session reserved as a buffer.

- Session 1 (Steps 1–2): Brain dump for the whole system, then plot formulation. Time box: 90 minutes brain dump, 60 minutes plot, 30 minutes break and parking-lot review.
- Session 2 (Steps 3–5): Storyboard, commands, and views, focused on Dispatch. Time box: 45 minutes storyboards, 60 minutes commands, 60 minutes views, 15 minutes wrap.
- Session 3 (Steps 6–7): Swim lanes (for the whole system) and scenarios (for Dispatch slices only). Time box: 90 minutes swim lanes, 75 minutes scenarios, 15 minutes retrospective.
- Session 4 (Buffer): Reserved for overflow, parking-lot resolution, and initial narrative sketches.

**Participants.** Erik, rotating through the five personas from the event-modeling-workshop skill. Claude invoked between sessions as Skeptic (to review each session's output and surface missed edge cases) and during Session 3 as Domain Expert (with ride-sharing-lessons-learned.md loaded into context to ground domain-expert responses in real ride-sharing-engineering experience).

**Tool.** Miro (free tier) for the board. Markdown file in `docs/workshops/001-first-event-modeling-workshop.md` as the durable artifact, updated at the close of each session. The Miro board is linked from the markdown file but is not the deliverable.

**Pre-seeding.** Before Session 1, pre-populate the Miro board with:
- One obvious event per tentative bounded context from the vision doc (11 stickies): *RiderRegistered*, *DriverOnboarded*, *RiderProfileCreated*, *DriverProfileCreated*, *GpsPingRecorded*, *TripRequested*, *TripStarted*, *FareCalculated*, *PaymentCaptured*, *RatingSubmitted*, *DriverSuspended*.
- One wireframe per BC showing a canonical screen (hand-drawn boxes are fine).
- A frame labeled "Parking Lot" for unresolved items.
- A frame labeled "Narrative Candidates" for user-journey sketches that come out of the scenarios step.
- A frame labeled "ADR Candidates" for architectural decisions the workshop surfaces.

**Output.** A single markdown file at `docs/workshops/001-first-event-modeling-workshop.md` containing:
- Session log (date, duration, persona rotation notes)
- Event list (with swim-lane assignments)
- Command list (tied to triggering UI)
- View list (tied to consuming UI)
- Swim-lane assignments → candidate service boundaries (feeds into a future ADR on service topology)
- Slice list for Dispatch (with priority ordering)
- GWT scenarios for the highest-priority 3–5 Dispatch slices
- Parking lot (raw list)
- Narrative candidates (one-line sketches; expanded later in `docs/narratives/`)
- ADR candidates (one-line sketches; expanded later in `docs/decisions/`)
- Retrospective (what worked, what didn't, what to change next time)

**First slices after the workshop.** The goal of the first handful of slices is to demonstrate the architectural spine, not to exercise domain complexity. Candidates:
- A Dispatch Command slice that exercises Wolverine gRPC unary (rider requests a trip, Dispatch records *TripRequested*).
- A Telemetry Command slice that exercises Wolverine's Kafka transport (driver pings location, Telemetry writes to Kafka, Telemetry projection consumes it).
- A Dispatch-to-Trips handoff slice that exercises cross-service messaging (Dispatch's *DriverAssigned* triggers Trips' aggregate creation).
- A Trips View slice that exercises Marten event sourcing and read-model projection (rider watches trip status update through event replay).

These slices trace to the primary and secondary goals in the vision doc; each one closes with a retrospective; each retrospective updates the Event Model and the narrative.

**Commit sequence for the workshop.** Following the CritterBids convention:
1. Pre-workshop preparation commit (the pre-seeded `docs/workshops/001-*.md` file, empty session-log entries).
2. End-of-Session-1 commit (brain dump and plot captured).
3. End-of-Session-2 commit (storyboards, commands, views captured).
4. End-of-Session-3 commit (swim lanes, scenarios, slice list, retrospective captured).
5. ADR-drafting commit (if the workshop surfaces architectural decisions that deserve their own ADR — service topology is the most likely candidate).
6. Narrative-drafting commits (the first narratives written from workshop output, committed to `docs/narratives/`).

---

## Open questions this research surfaces for CritterCab

Questions that are currently unresolved and that should be discussed at workshop-close, in an ADR, or in a later research pass.

**1. Does CritterCab commit to spec-anchored or spec-as-source?** Lesson 12 recommends spec-anchored. Confirming that in an ADR would close the ambiguity and set contributor expectations.

**2. What is the durable narrative format?** The vision doc v0.2 already lists this as an open question. The research does not resolve it; running one or two narratives in two candidate formats (freeform markdown with structured headers vs. Gherkin-flavored) and comparing them after implementation is the right way to pick.

**3. When does CritterCab switch from Miro to a dedicated Event Modeling tool?** The research recommends Miro for the first workshop. The trigger for switching to prooph board or similar should be named explicitly: a second modeler joining, a conference demo that needs live collaboration, or the MCP integration demonstrably saving prompt-authoring time.

**4. How is the Event Model kept in sync with code as implementation proceeds?** Spec-anchored regime implies retrospectives are the sync mechanism. What does that look like in practice? A retrospective item that asks "did this slice's implementation teach us anything that should update the Event Model?" is one answer. Whether the Event Model itself is re-exported to the markdown artifact in each retrospective, or whether it stays as the workshop snapshot and the narrative layer carries evolution, is a choice worth making deliberately.

**5. Should CritterCab run a pre-workshop Event Storming Big Picture session?** Erik's existing CritterBids and CritterSupply experience may make this unnecessary; skipping directly to Event Modeling worked for those projects. But CritterCab's domain (ride-sharing) is different enough, and the research is fresh enough, that an exploratory Storming pass might surface events that the structured Event Modeling approach would start too rigid to discover. Budget: one 2-hour session before Session 1 of the Event Modeling workshop, with a hard cutover if it doesn't produce new value quickly.

**6. Which persona does Claude play during which session?** The research recommends Skeptic between sessions and Domain Expert during Session 3. The full-coverage version would have Claude play Developer during Session 2 (commands and views), Skeptic during Session 3's swim-lane step, and Facilitator whenever Erik needs an outside voice. Deciding the persona schedule in advance reduces the cognitive load of persona rotation during the session.

**7. Is there a pre-existing Event Modeling reference model for ride-sharing that CritterCab can learn from?** The research did not surface one publicly. Searching Adaptech Group's non-public material, the Confluent course exercise repository, and the awesome-eventmodeling community list more carefully would be worth 30 minutes before Session 1.

**8. How does the Go polyglot service participate in Event Modeling?** The Go service's swim lane on the Event Model will look like every other swim lane — same events, same commands, same views — but its Translation slices (if any) will need to be carefully modeled because the wire format (Protobuf over gRPC) is the only thing the .NET side and the Go side share. The workshop should flag the Go boundary explicitly; CritterCab's first Go-implemented slice will depend on this being clean.

---

## Recommended follow-up reading

If only three resources can be consumed before the first workshop:

1. **Adam Dymitruk, *Event Modeling: What is it?* (eventmodeling.org, 2019).** The primary source. Roughly a 17-minute read. Read it twice: once for the methodology, once with the CritterCab vision doc open in another tab to note which parts will be most exercised.

2. **Alexandra Moxin, *Why You Need to Know About Event Modeling: An Intro* (Linux.com, 2024).** The cleanest seven-step walkthrough, by Adaptech's CSO. Read before pre-seeding the Miro board.

3. **Sebastian Bortz, *Event Modeling Cheat Sheet* (eventmodeling.org, 2020).** The reference for the four building blocks and four patterns. Print it and keep it visible during the workshop.

Secondary reading, ordered by CritterCab relevance:

- **Bobby Calderwood, *Practical Event Modeling* course (Confluent Developer).** The Workshop module in particular covers who to invite, how to prepare, and how long to plan for. Free to watch. About 30 minutes in total for the first three modules.
- **Adam Dymitruk, *Event Modeling Traditional Systems* (eventmodeling.org).** For the non-event-sourced case that Payments will exercise.
- **SE Radio Episode 539 — Adam Dymitruk on Event Modeling (with Jeff Doolittle, 2022).** Two hours of podcast. Useful for the historical context and for catching the tone Adam uses when explaining the method — which helps calibrate the facilitator voice.
- **prooph board wiki — *Prepare Big Picture Session*.** Even though the first CritterCab workshop will likely use Miro, the prooph board team's remote-workshop preparation checklist is the best one on the internet and applies to any tool.
- **pradhan.is, *Event Modelling Guide: Principles, Patterns & Best Practices* (March 2026).** The best single post-2025 synthesis, with a particularly sharp section on what goes wrong. Useful as a retrospective checklist.
- **Sam Hatoum / narrativedriven.org — origin story and principles pages.** For NDD context. CritterCab does not adopt the commercial Narrative/Auto product but the principles are load-bearing in the vision doc, and the origin story is short and worth reading before writing the first CritterCab narrative.
- **Oskar Dudycz, *Vertical Slices in practice* (event-driven.io, 2023) + Derek Comartin, *Vertical Slices doesn't mean "Share Nothing"* (CodeOpinion, 2026).** Read together. The first one shows a vertical slice in Marten and C#; the second clears up the most common confusion about what vertical slices actually require.

Tertiary — useful if time permits:

- **Crafting Tech Teams, *Difference between Event Storming and Event Modelling*.** Short, useful if someone asks "why not Event Storming?".
- **Event Modeling YouTube channel — Adam Dymitruk's live streams.** Particularly the Open Spaces ComoCamp sessions. For watching the method in motion rather than reading about it.

---

## Document history

- **v0.1** (2026-04-22): Initial research pass on Event Modeling as a workshop methodology, digital and AI-assisted facilitation patterns, and mapping to Spec-Driven and Narrative-Driven Development. Primary sources: Adam Dymitruk's own writing and talks (eventmodeling.org, SE Radio 539, InfoQ, Semaphore, YOW! 2023), Adaptech Group collaborators (Alexandra Moxin, Sebastian Bortz, Martin Dilger), the Confluent *Practical Event Modeling* course, the prooph board ecosystem, Sam Hatoum's NDD material, and the 2025–2026 Spec-Driven Development literature. Intended to complement `docs/vision/README.md` and `docs/research/ride-sharing-lessons-learned.md` and to guide CritterCab's first Event Modeling workshop. Companion artifact: the Claude `event-modeling-workshop` user skill already in Erik's skill library.
