# Event Modeling Canonical Sources — Notes for CritterCab

> **Primary source.** Adam Dymitruk, ["What is Event Modeling?"](https://eventmodeling.org/posts/what-is-event-modeling/), June 23, 2019.
>
> **Community corpus.** Nebulit Eventmodelers blog (eventmodelers.de), 2023–2026 — ten articles spanning the methodology's pedagogical baseline, doctrinal frontier, AI-integration layer, and adjacent doctrine. URLs cited inline in the community-corpus section.
>
> **Adjacent corpus notes on disk.** [Agents in Event Models](./agents-in-event-models.md) (Marc Klefter and Jake Bruun); [SDD: Event Model to Code](./sdd-event-model-to-code.md) (Martin Dilger). These notes carry their own lineage anchors per the post-2019 extensions section.

---

## Why This Matters for CritterCab

CritterCab's design phase rests on Event Modeling — workshop output drives narratives, narratives drive prompts, prompts drive implementation. Decisions made about Event Modeling's *vocabulary* and *methodology* therefore ripple through every artifact CritterCab produces. Most of those decisions have been implicit: the vision doc and workshop conventions inherited the methodology's primitives without naming whose primitives they are or when those primitives were established.

That implicit inheritance was fine while CritterCab's source material was small. With ten community articles, two on-disk research notes covering three external authors (Klefter, Bruun, Dilger), and a foundational 2019 piece behind all of them, the methodology's history now spans seven years across multiple voices that don't always agree. The "field has resolved to four building blocks" claim was a small example: plausible from one corner of the corpus, contradicted by another, and quietly accepted as fact because no doc surfaced the disagreement.

This doc surfaces what's there: where Event Modeling's primitives come from, what each subsequent voice extended or renamed, what fell off, and where the corpus genuinely disagrees. CritterCab's vision doc, skill files, and ADRs do the *deciding*; this doc gives them a clean inventory to decide against.

## The Reading Discipline

The doc's organizing thesis is **continuity and extension**, not "things that aged." The corpus evidence supports it: Dymitruk's 2019 primitives — Events, Commands, Views, Slices, Swimlanes, Translation, Automation, Given-When-Then, the 7-step workshop — travel intact into the 2026 community work. Klefter's translation-decision events extend Dymitruk's Translation pattern. Bruun's temporal-automation slicing extends Dymitruk's automation block. Dilger's Spec-Driven Development operationalizes Dymitruk's Given-When-Then specs with an AI agent loop. None of these replace the 2019 foundation; they build on it.

What aged is small and scope-bounded: a handful of Dymitruk's idiosyncratic terms (Y-valve, side-car) that the field never standardized on, his "flat cost curve" doctrine that survives in name but with softened force, and his standalone positioning (no DDD, no CQRS, no Event Sourcing citations in the 2019 piece) that the community has since glued tightly to all three. These observations get a section of their own near the end of the doc, but they are not the spine.

Where the corpus genuinely disagrees with itself — most consequentially on the building-blocks count — the doc documents the disagreement rather than picking a winner. CritterCab's position on contested vocabulary belongs in the vision doc; the building-blocks plurality section surfaces this as an explicit Open Question for the next vision-doc-touching session.

## What This Doc Is

A research-grade annotation of the canonical and community sources that underpin Event Modeling as practiced in CritterCab's vicinity. Per `docs/research/README.md`, research docs are "exploratory work, curated notes, and spikes that inform CritterCab's design decisions without being prescriptive artifacts." This doc honours that constraint: it surfaces lineage, vocabulary, and doctrine; it does not commit CritterCab to any of them.

The voice tilts toward narrative warmth — readers should be able to follow the lineage as a *story*, not just retrieve facts from an index. Tables appear where structural comparison earns its keep (the vocabulary status table, the building-blocks plurality grid, the doctrine-status survey), but they are support material, not the spine. The spine is the lineage.

## What This Doc Is Not

- **Not a methodology primer.** It assumes familiarity with Event Modeling's basic shape. Readers who need the methodology itself should start with the [Event Modeling workshop guide](./event-modeling-workshop-guide.md).
- **Not a CritterCab style guide.** Decisions about which vocabulary CritterCab adopts live in the vision doc and skill files. This doc supplies the inventory those decisions choose from.
- **Not a CritterCab roadmap.** The deferral callouts at the end of the doc name follow-up sessions that depend on this doc as input, but those sessions land separately.

---

## Dymitruk, June 2019 — The Originating Article

[Adam Dymitruk's "What is Event Modeling?"](https://eventmodeling.org/posts/what-is-event-modeling/) was published on June 23, 2019 and remains the field's primary source. The article opens with an unusual framing — Moore's Law and human memory as the motivating problem, not software architecture — and works downward from there to the methodology's primitives. Seven years later, those primitives are still recognizable in every community piece that follows: a fact that gives this doc's continuity thesis its grounding.

### The Primitives Dymitruk Establishes

The article enumerates **three building blocks**: *Events* ("facts... stored on a timeline"), *Commands* ("Intentions to change the system are encapsulated in a command"), and *Views* (also called Read Models — "the ability to inform the user about the state of the system"). Wireframes appear alongside them throughout the methodology but are explicitly framed as backdrop: *"we will only use 3 types of building blocks as well as traditional wireframes or mockups."* This footnote matters — it is the seed of the building-blocks plurality the doc will document later. Dymitruk treats wireframes as foundational-but-not-counted; the 2024 *Seven Insights* piece will later promote them (renamed Screens) to a fourth block.

Beyond the three blocks, the article establishes the vocabulary the community has since inherited largely intact:

- **Swimlanes**, which the eventmodelers.de tutorial will later translate as *= Bounded Contexts*. Dymitruk himself does not make that translation explicit.
- **Slices** as the unit of work — a stack of blocks that represent one cohesive workflow step.
- **Story Boards** as the model's overall artifact.
- **Translation** as the integration pattern for external data — the pattern Klefter will later extend by promoting translation *decisions* themselves to first-class events.
- **Automation** ("todo list pattern for background processors") — the abstraction Klefter will later argue covers LLM agents as well, and that Bruun will later decompose into multi-slice temporal automations.
- **Given-When-Then specifications** as the behavioural contract per slice — the format Dilger's SDD will operationalize as the agent's source of truth.
- **The 7-step workshop format**: brainstorming, the plot, time travel, identifying inputs, identifying outputs, applying Conway's Law, elaborating scenarios. This sequence carries forward into every Event Modeling workshop guide the community has since produced.

### Standalone Positioning, and Where the Field Has Drifted

Dymitruk's 2019 article does not cite Domain-Driven Design, Event Sourcing, CQRS, or Event Storming. The methodology is positioned as standalone — informed by Behaviour-Driven Development, Conway's Law (named explicitly), and (critically) Test-Driven Development, but not nested inside the ES/CQRS/DDD stack the community has since glued it to. This is one of the more substantial drifts the doctrine-status section will record: the methodology's *positioning* shifted from method-agnostic to ES/CQRS-coupled, even as its *primitives* travelled forward unchanged.

The article's other load-bearing doctrine is the **flat cost curve** — the claim that disciplined Event Modeling produces constant per-workflow-step implementation effort, in contrast to traditional development's exponential coupling cost. Dilger's November 2025 *AI Enabler* piece will explicitly reclaim this term, framing it as the property AI-assisted Event Modeling preserves. The flat cost curve is the rare 2019 doctrine that survived without softening — possibly because AI-era development gave it a second motivation that wasn't there in 2019.

### Two Terms That Aged

Two pieces of vocabulary from Dymitruk's Legacy Systems section did not propagate into the community pedagogy: **Y-valve redirection** and **side-car**. The 2019 quote runs *"Events can be gathered from the database of the old system and make views of that state — employing the translate pattern described previously. Y-valve redirection of user action can add new functionality in the side solution."* Neither term appears anywhere in the eventmodelers.de corpus. The broader software field had already standardized on Fowler's "strangler fig" (2004) for incremental-migration patterns, and the community pedagogy simply uses neither vocabulary — it doesn't argue against Y-valve and side-car; it doesn't acknowledge them. The concepts survive; the terms did not.

---

## The Community Corpus (eventmodelers.de, 2023–2026)

The Nebulit Eventmodelers blog at eventmodelers.de carries the community's evolving pedagogy and doctrine. The ten articles relevant to this annotation span late 2023 through early 2026 and divide cleanly along authorship lines that shape how each piece should be read.

### Authorial Pattern: Doctrine vs. Pedagogy

Martin Dilger — founder of Nebulit and the most active named voice in the corpus — bylines the articles that introduce new claims about how Event Modeling should be done. The [AI as Event Modeling Enabler](https://www.eventmodelers.de/docs/blog/ai-event-modeling-enabler/) piece (Nov 2025), the [Internal Fix-Price Model](https://www.eventmodelers.de/docs/blog/internal-fixprice-model/) (Nov 2025), and the [Event Sourcing and Async UX](https://www.eventmodelers.de/docs/blog/event-sourcing-async-ux/) essay (Dec 2025) all carry his explicit byline. These are the corpus's **doctrine pieces**: each makes an argument, takes a position, and propagates new vocabulary.

The remaining articles — the [tutorial](https://www.eventmodelers.de/docs/event-modeling-tutorial/), the [Seven Insights](https://www.eventmodelers.de/docs/blog/seven-event-modeling-insights/) retrospective, [80% Planning](https://www.eventmodelers.de/docs/blog/80-percent-planning/), [Ralph Loop](https://www.eventmodelers.de/docs/blog/ralph-loop-ai-agents/), [Vibe Coding](https://www.eventmodelers.de/docs/blog/vibe-coding-event-sourcing/), [Spec-Driven Development](https://www.eventmodelers.de/docs/blog/spec-driven-development/), [Documenting Software](https://www.eventmodelers.de/docs/blog/documenting-software-with-event-modeling/) — are published without bylines, attributable only to Nebulit collectively. These are the **pedagogy pieces**: they teach the methodology, retrospect on practice, or warn against anti-patterns. Their voice is plural and didactic; they do not propose changes to the methodology so much as transmit it.

This authorial split is more than a cataloguing detail — it tells the reader where doctrine pressure originates in the corpus. New claims (the four-block reframe in *Seven Insights*; the State-Change/State-View macro-block reframe in *Fix-Price Model*; the Ralph Loop's "Night Shift" rename in the March 2026 SDD republication) appear in pieces tied to a single author's argument, even when the byline is anonymous. The pedagogy pieces follow more conservatively, slowly absorbing the doctrinal frontier's vocabulary or sometimes ignoring it entirely.

### The Pedagogical Baseline: Tutorial + Documenting Software

The corpus's earliest piece is *Documenting Software with Event Modeling* (October 2023), and its anonymity is significant — this is where the community's voice first stabilizes. The article is the only eventmodelers.de piece besides the *AI Enabler* that cites Adam Dymitruk by name (twice, regarding original definition and extended patterns), and one of the two pieces in the corpus that uses the **three-block vocabulary** Dymitruk established (Commands, Events, Read Models — with Read Models tinted *orange* here, a colour choice that drifts to yellow in the later tutorial). Its argument is that Event Modeling produces always-current documentation because *"we can simply read the diagram from left to right"* — the timeline metaphor inherited directly from Dymitruk.

The undated **Event Modeling Tutorial** sits alongside it as the corpus's standing pedagogical front door. Like *Documenting Software*, it teaches three blocks (Commands, Events, Read Models — yellow this time). It also locks in the corpus's most consequential vocabulary translation: *"slices belong to swimlanes, which correspond to Bounded Contexts in Domain-Driven Design."* Dymitruk himself never made this translation explicit. The community did, presumably to interoperate with the DDD audience the methodology had drifted toward.

Together, these two pieces form the **pedagogical baseline**: three blocks, swimlanes-as-Bounded-Contexts, slices-as-Lego-units, Given-When-Then for behavioural specification, and the unbroken left-to-right timeline. A reader entering the corpus through either piece inherits Dymitruk 2019 nearly verbatim, with only the swimlane-DDD translation added.

What the pedagogical baseline *does not* contain is anything that would prepare the reader for the doctrinal frontier or the AI-integration layer. The four-block reframe, the State-Change/State-View macro-blocks, the Ralph Loop, the skill-files / rules-files distinction — none of these surface here. A reader who only reads the tutorial and the documenting-software piece would conclude that Event Modeling in 2026 looks exactly like Event Modeling in 2019. They would be substantially right about the methodology and substantially wrong about the surrounding doctrine.

### The Doctrinal Frontier: Seven Insights + Fix-Price Model

The pedagogical baseline transmits Dymitruk 2019 nearly verbatim. The doctrinal frontier doesn't.

The first piece to commit a substantive methodology change is *[7 Insights I Learned Building Event Models Since 2021](https://www.eventmodelers.de/docs/blog/seven-event-modeling-insights/)*, published anonymously in Nebulit's November 2024 newsletter. Most of its claims are pedagogical refinements — "use words from the business," "don't hide processes behind commands," "intentionally create bad screens." But buried in the building-blocks footing, almost in passing, the piece treats Event Modeling as having **four** components rather than three: Events, Commands, Read Models, **and Screens**. Wireframes — Dymitruk's foundational-but-not-counted backdrop — are promoted here to first-class membership. The article does not flag this as a doctrinal innovation. It writes as though the count has always been four.

The corpus does not, in fact, have always-been-four consensus. The tutorial still teaches three; *Documenting Software* still teaches three; Dymitruk explicitly enumerates three. The *Seven Insights* piece is doctrinal pressure operating quietly, the way doctrinal pressure usually operates in technical communities — by writing as though the new convention is already established.

A year later, Dilger goes further. *[The Internal Fix-Price Model](https://www.eventmodelers.de/docs/blog/internal-fixprice-model/)* (November 2025) is a piece about pricing software work, but its block-counting move is the most substantial departure from Dymitruk's vocabulary anywhere in the corpus. Dilger reframes the methodology around **State Change** (information entry point — what would otherwise be Command-plus-Event) and **State View** (information exit point — what would otherwise be Read-Model-plus-Screen). Two macro-categories, with slices as the unit of work. The three-or-four-block debate disappears beneath this reframe; the question is no longer "how many blocks?" but "what does a slice produce or consume?"

This is also the piece that introduces **Workflows** and **Chapters** as collections of slices — vocabulary that does not appear in Dymitruk and is not picked up elsewhere in the corpus. And it explicitly rejects Story Points (*"in favour of simple third-grade math"*), which is the corpus's first piece to argue against an external agile convention rather than just propagating Event Modeling's own.

Reading the doctrinal frontier in sequence makes the corpus's open vocabulary disagreement visible: *Seven Insights* says four; Dilger's *Fix-Price* says two macro-categories; pedagogy says three. None of these positions is wrong. They are different commitments by different voices about how to count the same primitives.

### The AI-Integration Layer: Vibe Coding's Cautionary Tale and the Three Answers to It

The AI-integration layer of the corpus is best read against a single anchor piece: *[Every Solution the AI Suggested Was Technically Correct...](https://www.eventmodelers.de/docs/blog/vibe-coding-event-sourcing/)* (March 2026), anonymous, written in the wake of an AI-led architecture experiment that failed. The author used Claude to build a web-based Event Modeling toolkit and ended up with a monolithic JSON blob that fractured into three desynchronized stores held together by 2-phase commits and change-listener chains. The article's core sentence — *"Event sourcing isn't better because it's elegant. It's better because it's honest"* — is the methodological lesson the rest of the AI-integration layer answers to.

The cautionary thesis: AI does not introduce new architectural mistakes. It **accelerates pre-existing wrong instincts** (*"the AI reproduced every wrong instinct that developers have been having for decades"*). LLMs optimize for immediate functionality — every commit was technically correct — while remaining indifferent to brittleness under concurrency, multi-user sync, and failure. Without an architectural backbone the AI cannot supply on its own, vibe-coded systems converge on the same anti-patterns developers were warned against in the 2000s.

The corpus offers three answers to this problem. Each takes a different cut at "what backbone do you give the AI."

**Dilger's *[AI as Event Modeling Enabler](https://www.eventmodelers.de/docs/blog/ai-event-modeling-enabler/)*** (November 2025) is the most direct answer: the Event Model itself is the backbone. *"Crystal-clear alignment for humans"* is what AI gets fed; *"80-90% of backend code automatically with minimal intervention"* is what it produces. This piece is also the only one in the corpus besides *Documenting Software* that cites Adam Dymitruk by name — Dilger names him as *"founder of Event Modeling itself"* and frames the AI Enabler's claim as a reclamation of Dymitruk's **flat cost curve** doctrine. The pre-2019 architectural ideas haven't aged; AI just needs them to scale. The piece also introduces **Rules Files** as the AI's architectural instructions — a sibling vocabulary to the *Skill Files* that appear in the SDD piece. The corpus does not reconcile whether Rules Files and Skill Files are the same artefact under different names or distinct concepts; presumably the former.

**The *[Ralph Loop](https://www.eventmodelers.de/docs/blog/ralph-loop-ai-agents/)*** (January 2026, anonymous) is the operational answer. Where the AI Enabler argues *that* AI needs an Event Model, Ralph Loop describes *how* the AI works through one. It credits **Geoffrey Huntley** for popularizing the underlying technique — a continuous bash loop that picks the next planned slice, implements it, runs tests, records learnings to `Agents.md`, clears context, and repeats. The article's most quotable claim — *"Manually writing Code is no longer a profession… it's like writing on a typewriter"* — is generational rather than methodological, but the operational mechanics matter regardless. (The on-disk note [SDD: Event Model to Code](./sdd-event-model-to-code.md) covers the Ralph Loop in depth and is the better entry point for implementation specifics.)

**The *[Spec-Driven Development](https://www.eventmodelers.de/docs/blog/spec-driven-development/)* article** (March 2026) is the synthesis. It carries Dilger's argument structure even without his byline — the human-vs-agent work split, the four-step methodology (Blueprint Architecture → Event Model as Source of Truth → Night Shift → Check), the skill files as agent-facing operational instructions, the learnings file persisting across context clears. This is a republication-plus-expansion of an earlier LinkedIn Pulse article; the eventmodelers.de version adds an origin-story opener (*"It Started With One Client Project"*), renames the agent loop step from *"The Agent Loop (The Ralph Loop)"* to **"The Night Shift"** (operations vocabulary over engineering vocabulary), and adds a *"What This Means for Your Team"* closer that the earlier version doesn't have. The existing on-disk note [SDD: Event Model to Code](./sdd-event-model-to-code.md) covers the methodology's substance; readers who want the full doctrine should start there.

Read in sequence — Vibe Coding's diagnosis, then AI Enabler's prescription, then Ralph Loop's mechanics, then SDD's synthesis — the AI-integration layer reads as a coherent argument: AI accelerates wrong instincts, Event Modeling is the architectural backbone that fixes the acceleration, the Ralph Loop is the operational shape, and SDD is the framework that names all of it.

### Adjacent Doctrine: Async UX + 80% Planning

Two articles in the corpus are not about Event Modeling per se but adjacent doctrine that the methodology touches.

Dilger's *[Event Sourcing and Async UX](https://www.eventmodelers.de/docs/blog/event-sourcing-async-ux/)* (December 2025) addresses the friction between event-sourced architectures and user-experience expectations inherited from CRUD systems. Dilger's framing — *"Event Sourcing doesn't make UX harder — it makes it different"* — argues the friction is psychological, not technical. He credits **Greg Young** with an unspecified video that shifted his thinking, and offers a four-tier heuristic for UX-aware event-sourced systems: **Redirect Attention** (shift user focus away from waiting), **Optimistic UI** (show changes immediately and sync later), **Reload Button** (permit manual refresh as acceptable UX), and **Notification Channel System** (technical solution using logged user IDs as subscription keys). The heuristic is the article's main contribution; its general claim is that async latency *"forces you to be more thoughtful"* about user journeys rather than defaulting to the immediate-data-visibility expectations of CRUD.

The anonymous *[80% Planning](https://www.eventmodelers.de/docs/blog/80-percent-planning/)* (November 2025) makes an argument about the planning-to-execution ratio in software engineering. Its central claim, verbatim: *"If you are in Software Engineering — 80% of your work should be planning."* The piece critiques *"move fast and break things"* and *"emergent design"* as producing *"constant distractions,"* and names three anti-patterns it argues against: TBD requirements left for implementation, treating architectural questions as implementation details, and assumption-based documentation versus explicit decisions. The article does not cite Dymitruk and does not engage with the building-blocks debate, but its methodology — *"all questions are cleared and you only need to execute"* — is recognizably the same posture Dilger takes in SDD and the AI Enabler.

Neither of these articles propagates new core Event Modeling vocabulary. They appear in the corpus because the community's doctrine extends to engineering-adjacent topics — UX, planning, pricing, AI tooling — wherever the methodology has implications. Reading them is optional for understanding Event Modeling; reading them is useful for understanding *the community's broader posture toward software work*.

---

## Post-2019 Extensions: Klefter, Bruun, Dilger

This doc's continuity-and-extension thesis depends on a concrete mapping: which Dymitruk 2019 primitive does each post-2019 community innovation extend? The table below names the mapping. The on-disk research notes [Agents in Event Models](./agents-in-event-models.md) and [SDD: Event Model to Code](./sdd-event-model-to-code.md) carry the full annotation of each extension; this section is the genealogical map that points to them.

| Dymitruk 2019 primitive | Post-2019 extension | Author | On-disk note |
|---|---|---|---|
| Translation pattern (Integration section) | Translation decisions as first-class events | Marc Klefter (Jan 2026) | [Agents in Event Models](./agents-in-event-models.md) |
| Automation ("todo list pattern") | Agent-as-automation: LLM is just another process sticky on the timeline | Marc Klefter (Jan 2026) | [Agents in Event Models](./agents-in-event-models.md) |
| Automation ("todo list pattern") | Temporal automations decomposed into multi-slice boards (five-slice account-lockout example) | Jake Bruun (Jan 2026) | [Agents in Event Models](./agents-in-event-models.md) |
| Given-When-Then specifications | Behavioural specs as the AI agent's source of truth, status-gated by humans | Martin Dilger (LinkedIn Pulse; eventmodelers.de Mar 2026 republication) | [SDD: Event Model to Code](./sdd-event-model-to-code.md) |
| 7-step workshop format | Workshop facilitation via AI: validation prototypes, code generation, surgical maintenance updates | Martin Dilger (Nov 2025) | (annotated in the AI-integration layer above) |
| Flat cost curve | Reclaimed as the property AI-assisted Event Modeling preserves | Martin Dilger (Nov 2025) | (annotated in the AI-integration layer above) |
| Story Boards as the model artifact | Event Model exported as versioned JSON into the repository | Martin Dilger (SDD) | [SDD: Event Model to Code](./sdd-event-model-to-code.md) |

The mapping is asymmetric in two ways worth noting:

- **Klefter and Bruun extend Dymitruk's two integration primitives** (Translation and Automation). Their work assumes the rest of the methodology — blocks, slices, swimlanes — is intact and operates on it.
- **Dilger extends Dymitruk's specification and workshop apparatus**, not the primitives themselves. SDD operationalizes Given-When-Then; AI Enabler operationalizes the workshop; neither argues the methodology's primitives need changing. (Dilger's *Fix-Price Model* introduces the State Change / State View reframe in passing, but as pricing-doctrine vocabulary, not as a methodology revision he is championing systematically.)

What no post-2019 author has substantively extended: the three-block enumeration itself, the swimlane primitive, or the slice unit. The methodology's *spatial* vocabulary (what blocks exist, how they relate on a swimlane, how they group into a slice) is stable. The methodology's *processual* vocabulary (how integration works, how specifications drive implementation, how the workshop facilitates the model) is where extension is happening.

The two on-disk notes carry a *"What this extends"* preamble below their *"Why this matters for CritterCab"* opener — a lineage anchor that points each note back to the Dymitruk primitive it operates on. Readers entering through either note will see the anchor before the body.

---

## Vocabulary Status Survey

The corpus's vocabulary divides into five status categories. The categories are descriptive, not prescriptive — they capture what the corpus does with each term, not what CritterCab should do.

- **Canonical**: introduced and consistently used across the corpus.
- **Faded**: introduced but used with diminishing emphasis; survives in name without doctrinal force.
- **Superseded**: replaced by a later term that the field standardized on.
- **Never-canonical**: introduced by one author but did not propagate; the term never reached community-wide use.
- **Contested**: actively disagreed about across the corpus; no consensus.

| Term | Introduced by | Status | Observed in CritterCab |
|---|---|---|---|
| Events | Dymitruk 2019 | Canonical | Used throughout vision doc, workshops, narratives |
| Commands | Dymitruk 2019 | Canonical | Used throughout |
| Views / Read Models | Dymitruk 2019 | Canonical (2019 uses "Views"; tutorial uses "Read Models" — same primitive, terminology drifted) | Used as Read Models |
| Screens / Wireframes | Dymitruk 2019 (as backdrop) | **Contested** — backdrop in Dymitruk + tutorial + *Documenting Software*; promoted to first-class block in *Seven Insights* (2024) | Implicit; no committed position |
| Slices | Dymitruk 2019 | Canonical | Used throughout narratives, workshops, prompts |
| Swimlanes | Dymitruk 2019 (community translation: = Bounded Contexts) | Canonical | Used in workshops alongside Bounded Context vocabulary |
| Story Boards | Dymitruk 2019 | Faded — concept survives as "Event Model board"; the original term is uncommon | Not used; CritterCab says "event model" |
| Translation pattern | Dymitruk 2019 | Canonical (extended by Klefter) | Not yet exercised; will surface at the first integration ACL |
| Automation | Dymitruk 2019 | Canonical (extended by Klefter + Bruun) | Implicit in narrative authoring; not yet named explicitly |
| Given-When-Then | Dymitruk 2019 (via BDD) | Canonical (operationalized by Dilger SDD) | Used in workshop slices; not yet adopted in narratives |
| 7-step workshop format | Dymitruk 2019 | Canonical (operationalized by Dilger AI Enabler) | CritterCab workshops draw on the format implicitly |
| Flat cost curve | Dymitruk 2019 | Canonical (reclaimed by Dilger Nov 2025) | Not invoked yet |
| Done is Done Done Right | Dymitruk 2019 | Faded — no successor; no corpus piece picks it up | Not used |
| Y-valve redirection | Dymitruk 2019 | **Never-canonical** — Dymitruk-idiosyncratic; field never standardized on the term | Not used |
| Side-car | Dymitruk 2019 (Legacy Systems) | **Never-canonical** — same pattern | Not used |
| Strangler fig | Fowler 2004 (outside corpus) | **Never-canonical within Event Modeling corpus** — industry-standard term, but the eventmodelers.de corpus uses no term at all for the same concept | Not yet relevant |
| State Change / State View | Dilger *Fix-Price* (Nov 2025) | **Contested** — macro-block reframe; not picked up by other corpus pieces | Not used |
| Workflows / Chapters | Dilger *Fix-Price* (Nov 2025) | **Contested** — Dilger-specific vocabulary; not propagated | Not used |
| Skill Files | Dilger SDD | Canonical within the SDD lineage | `docs/skills/` directory uses this convention |
| Rules Files | Dilger *AI Enabler* (Nov 2025) | **Contested** — possibly synonymous with Skill Files; corpus does not reconcile | Not used as a distinct concept |
| Ralph Loop | Dilger SDD (credits Geoffrey Huntley) | Canonical within the SDD lineage | Not yet adopted; CritterCab uses a session-bounded prompt+retro cycle |
| Night Shift | Dilger SDD (Mar 2026 republication) | **Never-canonical** — appears in one piece as a rename of Ralph Loop's Step 3 | Not used |
| Agents.md / progress.txt / Learnings File | Dilger SDD / Ralph Loop | Canonical within the SDD lineage | Partially analogous to CritterCab retrospective files |

A reader scanning the table for **contested** entries finds the doc's most actionable observations: the building-blocks count (Screens), the Dilger-*Fix-Price* macro-block reframe (State Change / State View), the Workflows-Chapters cluster, and the Skill-Files-vs-Rules-Files ambiguity. The next section walks the most consequential of these — the building-blocks count — in case-study detail.

---

## The Building-Blocks Plurality (Case Study)

The vocabulary status survey above flagged the building-blocks count as **contested**. The contest is significant enough to warrant a section of its own — not because the count itself is contentious in the abstract, but because CritterCab inherits the count implicitly every time a narrative, workshop, or skill file describes "the model's primitives."

### Three Active Positions

The corpus, taken seriously, supports three positions on how to enumerate Event Modeling's building blocks. None of them is the field's settled consensus; all three live concurrently in pieces published between October 2023 and March 2026.

| Position | Block count | Articles holding it | What's a "block" |
|---|---|---|---|
| **Three blocks + foundational backdrop** | 3 | Dymitruk 2019; eventmodelers.de tutorial; *Documenting Software* (Oct 2023) | Commands, Events, Read Models. Wireframes/Screens are *backdrop* — present throughout the methodology but explicitly not counted (*"we will only use 3 types of building blocks as well as traditional wireframes or mockups"*) |
| **Four blocks (Screens promoted)** | 4 | *Seven Insights* (Nov 2024) | Events, Commands, Read Models, **Screens**. Wireframes/Screens promoted from backdrop to first-class membership; no explicit argument made for the promotion |
| **Two macro-categories (State Change / State View)** | 2 | Dilger *Fix-Price Model* (Nov 2025) | **State Change** (information entry point — Commands + Events) and **State View** (information exit point — Read Models + Screens). The three-or-four-block question dissolves; slices are the unit of work; blocks are categories of what a slice produces or consumes |

The slice-centric framings — SDD, Ralph Loop, AI Enabler — sidestep the question entirely. They reference "slices" as the agent's unit of work without enumerating constituent blocks. Whether their authors hold position one, two, or three is not visible from those articles.

### Why This Matters for CritterCab

CritterCab's vocabulary inherits a building-blocks count *implicitly* through:

- **Workshop documents** that describe slices in terms of their constituent blocks ("the slice contains a command, an event, and a read model").
- **Narrative documents** that render slice behaviour and may or may not treat the UI rendering (a Screen) as part of the slice or as backdrop.
- **Skill files** that codify "how we build slices here" and therefore commit (or fail to commit) to a count by what they enumerate.

Right now, CritterCab's workshops and narratives operate in the three-block tradition without naming it as a position — the position is held by default rather than by decision. The 2026-05-21 advisory thread that scoped this doc had implicitly accepted the four-block reading ("the field has resolved to four"), based on incomplete evidence. The fuller corpus shows the field hasn't resolved; CritterCab will need to take a position deliberately.

### Open Question for the Vision Doc

> **Open Question — Building-blocks vocabulary commitment.** Which building-blocks vocabulary does CritterCab commit to: three-blocks-plus-backdrop (Dymitruk-canonical), four-blocks-with-Screens (Nebulit doctrinal frontier), or two-macro-categories (Dilger *Fix-Price*)? The position affects every workshop's slice rendering, every narrative's Moment description, and every skill file's "what a slice contains" specification.
>
> **Why this needs deciding before further pedagogy lands:** the question is propagating implicitly into in-progress workshop and narrative work. A deliberate commitment up front prevents per-session re-negotiation and unintentional drift toward whichever position the most recently-read article happens to favour.
>
> **Where this is captured for follow-up:** the vision doc, on its next opening. The deferrals section below records this as a high-priority input to that session.

---

## Doctrine Status: What Survived, What Aged, What's New

The vocabulary survey categorized terms; this section categorizes doctrine. The two are related but not identical: a term can survive while its underlying doctrine softens, or a doctrine can survive while its original vocabulary fades (incremental migration survives Dymitruk's Y-valve naming but doesn't have a replacement term inside the corpus).

### Survived from Dymitruk 2019

Most of Dymitruk's 2019 doctrine travels intact into the 2026 community work:

- **The three-block primitive set as load-bearing methodology vocabulary** — even where the community has added doctrinal pressure (the *Seven Insights* four-block reframe, the *Fix-Price* macro-block reframe), the underlying primitives Dymitruk named are recognizable in every reframe.
- **Slices as the unit of work** — universal; the three slice-centric AI-integration pieces lean on this as the foundational abstraction.
- **Swimlanes** — extended by the community's *= Bounded Contexts* translation; the primitive itself survives.
- **Given-When-Then per slice** — operationalized by SDD as the agent's source of truth; the structural form is Dymitruk's.
- **The 7-step workshop format** — operationalized by AI Enabler for AI-facilitated workshops; the sequence is Dymitruk's.
- **Translation pattern** — extended by Klefter; the integration discipline is Dymitruk's.
- **Automation as todo-list pattern** — extended by Klefter (agents) and Bruun (temporal slicing); the primitive is Dymitruk's.
- **The flat cost curve** — reclaimed verbatim by Dilger's AI Enabler as the property AI-assisted Event Modeling preserves. This is the rare doctrine that survived without softening.

### Aged

The aged-doctrine list is short and scope-bounded. The concepts behind each entry typically survive; the vocabulary did not.

- **Y-valve redirection** (Dymitruk's Legacy Systems section) — the term did not propagate. The eventmodelers.de corpus uses no term for the same incremental-migration pattern; the broader software field uses Fowler's "strangler fig" (2004), which also does not appear in the corpus.
- **Side-car** (Dymitruk's Legacy Systems section) — same. The term acquired separate canonical meaning in the microservices community for a different pattern, which may have contributed to its falling out of Event Modeling vocabulary.
- **"Done is Done Done Right"** (Dymitruk's project-management doctrine) — faded. No successor doctrine; no piece in the corpus reclaims or argues against it.
- **Standalone positioning** (Dymitruk's 2019 article cited neither DDD, ES, CQRS, nor Event Storming) — substantially drifted. The community has since tightly coupled Event Modeling to all three. This is a positioning shift, not a primitive shift.

### Newly Canonical (Post-2019)

The doctrinal frontier and the AI-integration layer have added substantial doctrine that is now load-bearing in the 2026 community pedagogy:

- **Skill Files** (Dilger SDD) — operational instructions the AI agent reads before implementing any slice; encode "how we build things here."
- **The Ralph Loop** (Dilger SDD; credits Huntley) — the agent's operating cycle of find-slice → implement → test → record learnings → clear context → repeat.
- **Agents.md and progress.txt** (Ralph Loop) — persistent agent state across context clears, the mechanism by which "the agent gets smarter the longer you run it."
- **80% planning ratio** (anonymous Nov 2025) — the doctrine that planning is most of the work; "move fast and break things" and "emergent design" are named anti-patterns.
- **"Vibe coding" as anti-pattern label** (anonymous Mar 2026) — AI-led architecture without structural backbone; the corpus names this as a thing to avoid.
- **Four-tier async UX heuristic** (Dilger Dec 2025) — Redirect Attention, Optimistic UI, Reload Button, Notification Channel; not Event Modeling vocabulary per se but adjacent doctrine for event-sourced systems.
- **Translation-decision events** (Klefter Jan 2026) — extends Dymitruk's Translation pattern by promoting translation choices to first-class events.
- **Agent-as-automation framing** (Klefter Jan 2026) — extends Dymitruk's Automation primitive to cover LLM agents.
- **Temporal automation slicing** (Bruun Jan 2026) — extends Dymitruk's Automation primitive into multi-slice board decomposition.

The shape of the additions tells a small story: most newly-canonical doctrine concerns either AI integration (Skill Files, Ralph Loop, Agents.md, vibe-coding-as-anti-pattern) or extensions of Dymitruk's two integration primitives (translation-decision events, agent-as-automation, temporal slicing). The methodology's *spatial* primitives — events, commands, views, slices, swimlanes — have not received new canonical doctrine. They remain at their 2019 settings.

---

## What This Doc Defers

Three follow-up sessions are downstream of this annotation. Each is named here so its dependency on this doc is auditable.

### Spec-Delta and Skills Amendments (A1–A15)

The 2026-05-21 advisory thread that originally scoped this doc identified fifteen amendments (A1–A15) to CritterCab's skill files that derive from the canonical-sources annotations gathered here. Those amendments are conversation-only drafts at the time of writing — they are not yet on disk and are not enumerated in this doc. They will land in a separate session whose prompt slug is provisionally **`tidy: skills`**, scoped after this doc is committed.

This deferral is *not* high-priority — the amendments are tidying work that follows naturally from the corpus annotations, not a blocker for further design-phase work.

### SDD-Instantiation Calibration

A calibration question that surfaced during the 2026-05-21 advisory thread but was deliberately not resolved: should CritterCab's session-driven workflow lean toward Dilger's compact SDD form (Event Model → JSON → Agent Loop, with slices status-gated) or hold its current multi-layer form (workshop → narrative → prompt → execute → retrospective)?

The question is the territory of the **next research session — the SDD-landscape doc** — which will hold the eventmodelers.de SDD republication and the existing on-disk SDD note next to CritterCab's current workflow and decide where the loops align and where they diverge. This doc surfaces the question; it does not resolve it.

### Building-Blocks Vocabulary Position (High Priority)

The building-blocks plurality section above surfaces an Open Question that CritterCab will need to commit on: three-blocks-plus-backdrop, four-blocks-with-Screens, or two-macro-categories. The position belongs in the vision doc, not in a reference note.

This deferral is **high-priority** because the question is propagating implicitly into in-progress workshop and narrative work. The next session that opens the vision doc for any reason inherits this Open Question as input.
