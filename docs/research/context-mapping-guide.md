# Context Mapping: A Guide for CritterCab's First Context Mapping Session

Curated research notes on Context Mapping as a DDD strategic design technique — what it is, how it differs from tactical DDD, the nine relationship patterns and when each applies, how to run a session, common mistakes, and how it feeds into Domain Storytelling and Event Modeling. The focus is practical guidance: what to do in CritterCab's first Context Mapping session, with enough theory behind each recommendation that departures are deliberate rather than accidental.

Context Mapping was established as a named practice in Eric Evans' *Domain-Driven Design: Tackling Complexity in the Heart of Software* (the blue book, 2003). Much of the practical workshop guidance has evolved through the DDD community since then — particularly through the ddd-crew GitHub organization (Nick Tune, Alberto Brandolini, and collaborators), Vaughn Vernon's *Implementing Domain-Driven Design*, and the Context Mapper open-source project.

---

## About this document

### What counts as a good source

Context Mapping has a smaller and more stable source surface than Event Modeling. The blue book is the original formulation; Vaughn Vernon's *IDDD* is the most practical expansion. The ddd-crew GitHub organization has produced the most actionable workshop-era material (Bounded Context Canvas, Domain Message Flow Modeling). Sources were tiered as follows:

- **Tier 1 — Evans' blue book and Vaughn Vernon's IDDD.** The canonical formulations. All nine patterns originate here.
- **Tier 2 — ddd-crew GitHub organization.** Nick Tune's Bounded Context Canvas, Domain Message Flow Modeling, and the context-mapping reference cheat sheet. These are the practical successors to the theoretical formulations.
- **Tier 3 — Community practitioners.** InfoQ strategic DDD articles, the Context Mapper open-source DSL and its documentation, the Global App Testing domain cartographers case study (a detailed account of a real seven-session context mapping exercise), and the arhohuttunen.com DDD series.
- **Tier 4 — LLM/AI assistance in strategic design.** ddd.academy workshop material on using LLMs in strategic DDD sessions.

### How to read the "applicability to CritterCab" notes

CritterCab has 11 tentative bounded contexts (as of vision doc v0.3), no runnable code, and one primary contributor. The applicability notes take these facts seriously. The technique is not described in the abstract; it is described in terms of what CritterCab's first session should actually produce.

---

## Primary sources consulted

Eric Evans — *Domain-Driven Design: Tackling Complexity in the Heart of Software* (Addison-Wesley, 2003). The original formulation of all nine patterns.

Vaughn Vernon — *Implementing Domain-Driven Design* (Addison-Wesley, 2013). The most detailed practical expansion of context mapping, including the upstream/downstream power dynamics that Evans describes more briefly.

ddd-crew organization (GitHub):
- *context-mapping* reference card — ddd-crew/context-mapping, team relationship types and nine patterns.
- *bounded-context-canvas* — ddd-crew/bounded-context-canvas. Nick Tune's per-BC design tool.
- *domain-message-flow-modelling* — ddd-crew/domain-message-flow-modelling. Nick Tune's technique for modeling inter-BC communication.

Context Mapper project — contextmapper.org. Open-source DSL for expressing context maps in code. Documentation used as pattern reference.

Community writing:
- Alberto Brandolini, *Strategic Domain Driven Design with Context Mapping* — InfoQ, 2009.
- Global App Testing engineering blog, *Domain Cartographers — how to draw a Context Map* — a first-person account of a twelve-BC context mapping exercise across seven sessions.
- Arho Huttunen, *Context Mapping in Domain-Driven Design* — arhohuttunen.com.
- Context Mapper pattern documentation — contextmapper.org/docs.

AI-assisted strategic design:
- ddd.academy, *Accelerate your Strategic Design with Large Language Models* — workshop curriculum.

---

## Lesson 1 — What Context Mapping is and what it is not

Context Mapping is a **strategic** DDD practice. It operates at the level of bounded contexts and their relationships — who influences whom, how models cross boundaries, and what organizational dynamics shape the software. It is not about the internals of any one context. It is not about data schemas, aggregate designs, or event names. Those are **tactical** concerns and come later.

The core problem Context Mapping addresses: when a system has multiple bounded contexts, those contexts do not exist in isolation. They depend on each other. A change in one model propagates downstream whether or not anyone planned it. Teams have power dynamics — an upstream team can ignore a downstream team's needs; a downstream team can protect itself by inserting a translation layer. Without a visible map of these relationships, the dependencies are still there, they are just invisible. Invisible dependencies are more dangerous than visible ones.

The InfoQ formulation is exact: a context map "captures the existing terrain." The key word is *existing*. A context map describes how things *are*, not how they *should* be. This is its most commonly violated principle — see Lesson 9.

A context map has two levels of content:

**Bounded contexts as nodes.** Each bounded context is a box. The box has a name; inside the name lives a ubiquitous language. Two boxes with the same word in their language mean different things by that word — that is the whole point.

**Relationships as edges.** Each edge between two bounded contexts has a direction (upstream → downstream) and a pattern label (one of the nine patterns). The pattern label is what makes the map actionable: it tells you what kind of integration exists and what the dependency costs are.

A context map is **not** a deployment diagram, a service mesh, or an API inventory. All of those may look similar. The difference is that a context map encodes *power dynamics* and *model translation* boundaries, not just data flows or network topology.

### Applicability to CritterCab

- **CritterCab's context map is a greenfield map.** There is no existing code whose implicit relationships need to be reverse-engineered. This is advantageous: the map can describe the intended architecture from the start rather than documenting accidental coupling. Greenfield maps carry a different risk — see the AS-IS vs TO-BE discussion in Lesson 7.
- **The map's primary output for CritterCab is the swim-lane input for Event Modeling.** ADR-004 sequences Context Mapping before Event Modeling specifically so the Event Model's swim lanes reflect real domain boundaries rather than accidental groupings. The context map is the source of truth for "which context owns what."
- **The 11 tentative bounded contexts in the vision doc (v0.3) are the starting population.** They are explicitly described as pre-Event-Modeling candidates. The context mapping session may split, merge, or rename some of them.

---

## Lesson 2 — The nine patterns

The nine patterns are not arbitrary categories. They are organized along two axes: *how much leverage the downstream has over the upstream* and *how much model corruption the downstream is willing to tolerate*. Understanding this organizing principle makes the choice between patterns intuitive rather than a lookup table.

**Partnership.** Two contexts with a mutual dependency on each other's success. Both teams coordinate releases, plan together, and share integration responsibility. The relationship is symmetric; neither is purely upstream or downstream. Use it when the failure of either context means failure for both — and when both teams are willing to pay the coordination cost. It is the highest-trust pattern and the highest-communication-overhead pattern.

**Shared Kernel.** A small, explicitly agreed-upon subset of the domain model is shared between two contexts. Both teams co-own the shared code. Any change to the kernel requires coordination with the partner team. Smaller is better: the value of a Shared Kernel shrinks as it grows, because the coordination cost of change grows with it. Use it when keeping two independent model definitions in sync would be more expensive than managing shared ownership.

**Customer/Supplier.** An upstream context (supplier) provides something a downstream context (customer) needs. The relationship is asymmetric: the downstream depends on the upstream, and the upstream can ignore the downstream's needs in principle — but the Customer/Supplier pattern formalizes a commitment that the upstream will factor downstream priorities into its planning. Customers can request changes; suppliers decide the implementation and timeline. This pattern works when the upstream team delivers in good faith. It breaks when the upstream team prioritizes its own roadmap over customer needs — at which point the downstream's choices narrow to Conformist or Anticorruption Layer.

**Conformist.** The downstream adopts the upstream's model wholesale, with no translation. The downstream accepts the upstream's vocabulary and data structures directly. Use it when the upstream model maps cleanly enough to the downstream domain that the cognitive cost of adopting it is lower than the cost of maintaining a translation layer. Common with third-party or external systems whose model is stable and well-designed. Dangerous when the upstream model is poorly structured or semantically incompatible — the upstream's conceptual debt becomes the downstream's conceptual debt.

**Anticorruption Layer (ACL).** The downstream inserts a translation layer between itself and the upstream. The translation layer speaks the upstream's language outward and the downstream's language inward. The downstream model is protected from upstream changes; when the upstream changes, only the ACL needs to be updated, not the downstream's domain logic. Use it whenever the upstream model is poorly structured, semantically incompatible, or controlled by a team with no incentive to accommodate downstream needs. The ACL costs maintenance; the benefit is model integrity.

**Open Host Service (OHS).** The upstream defines a stable, well-documented protocol (an API or message format) that makes its functionality available to all comers. The OHS is a deliberate act of generosity and discipline on the upstream's part: instead of asking each downstream to integrate with internal implementation details, it publishes a clean interface. OHS is typically paired with Published Language. Use it when multiple downstream contexts need to integrate with the same upstream, and when the upstream team is willing to maintain a stable external interface independently of its internal evolution.

**Published Language (PL).** A well-documented, widely understood language is used as the common medium for information exchange between contexts. Published Language answers the question "in what format do we exchange data?" while Open Host Service answers "how do we access the upstream?" They are often paired: an OHS expresses its functionality in a PL. Classic examples include iCalendar, vCard, and HL7. For CritterCab, **Protobuf is the Published Language** for gRPC integrations (see ADR-009). Protobuf schemas are the CML for inter-service communication.

**Separate Ways.** Two bounded contexts have no meaningful relationship and are deliberately kept independent. There is no integration, no shared model, no coordination. Use it when the cost of integration exceeds the benefit, or when the contexts serve genuinely orthogonal domains. Separate Ways is often the right answer for contexts that initially appear related but whose relationship is merely coincidental. On a context map, Separate Ways edges are typically omitted (their presence adds noise without adding information).

**Big Ball of Mud.** A degenerate pattern: an area of the system with mixed models, inconsistent boundaries, and accumulated technical debt. Named on the context map not to celebrate it but to demarcate it — to acknowledge its existence and prevent its inconsistency from propagating. A Big Ball of Mud boundary on the map says: "this thing exists; integrate with it carefully; do not let its chaos leak inward." Use an ACL on the downstream side of any Big Ball of Mud relationship.

### Applicability to CritterCab

- **The most immediately relevant patterns for CritterCab are ACL, Customer/Supplier, and Open Host Service + Published Language.** Identity is an ACL by design (ADR-006). The Telemetry → Dispatch → Trips chain is a series of Customer/Supplier or Upstream/Downstream relationships. gRPC service definitions are Open Host Services expressed in the Published Language of Protobuf.
- **Shared Kernel is a pattern to avoid for CritterCab.** ADR-002 prohibits shared application-layer code between services. Any relationship that looks like it might need a Shared Kernel is a signal to re-examine the boundary, not to introduce shared code.
- **Conformist is appropriate for some external system integrations.** When CritterCab integrates with Entra External ID at the token-validation level (standard OIDC JWT claims), it is effectively Conformist — it adopts the standard claim names without a translation layer. The ACL sits at the lifecycle event boundary, not the token validation boundary.
- **Big Ball of Mud has no place in CritterCab's first-party contexts.** This pattern is reserved for describing legacy external systems CritterCab must integrate with if any emerge.

---

## Lesson 3 — Upstream/downstream dynamics and team relationships

Every relationship on a context map has a direction. The direction is not arbitrary — it encodes a power asymmetry. The upstream team's decisions ripple downstream. The downstream team must adapt to upstream changes or protect itself from them. This is true whether or not anyone intended it.

The ddd-crew cheat sheet formalizes three types of team relationship that govern how the power asymmetry plays out:

**Mutually Dependent.** Both teams need each other's output to deliver. Changes require coordination. Communication overhead is high. This maps to Partnership on the pattern side.

**Upstream/Downstream.** The upstream can succeed independently of the downstream. The downstream is affected by upstream changes. The downstream has two choices: adapt (Conformist, Customer/Supplier) or protect (ACL). The downstream team's leverage determines which is appropriate.

**Free.** The contexts operate independently, with no organizational or technical link. This maps to Separate Ways on the pattern side.

Two practical diagnostics for identifying upstream/downstream relationships:

**Who changes first?** If Context A's model changes and Context B must respond, A is upstream. If it is truly symmetric — both teams must coordinate on every change — that is Mutually Dependent.

**Who can ship independently?** An upstream context can ship a release without coordinating with downstream. A downstream context typically cannot ship a release that touches the shared boundary without knowing what the upstream looks like.

The upstream/downstream arrow on a context map points from upstream to downstream. It is an *influence flow*, not a data flow. A context map with all arrows pointing the same direction should raise a flag: real systems have cycles of influence, even when the formal relationships are asymmetric.

### Applicability to CritterCab

- **Telemetry is upstream of Dispatch.** Telemetry produces the location index that Dispatch queries. Dispatch depends on Telemetry; Telemetry has no opinion about Dispatch.
- **Dispatch is upstream of Trips.** When Dispatch assigns a driver, it hands off to Trips. Trips depends on Dispatch's assignment outcome; Dispatch has no opinion about what happens to a trip after handoff.
- **Identity is upstream of Rider Profile, Driver Profile, and Onboarding.** Identity emits lifecycle events; the other contexts consume them. None of those downstream contexts can influence what Identity's model looks like.
- **Trips is upstream of Payments and Ratings.** A trip completion triggers both the payment capture and the rating prompt. Payments and Ratings depend on Trips' completion event; they do not influence Trips' model.
- **Operations is downstream of almost everything.** It consumes read models from most other contexts and issues administrative commands. It has no upstream contexts within the CritterCab system.
- **Entra External ID is upstream of Identity.** This is the most consequential external upstream: Entra controls the model, Identity must adapt. The ACL (Identity BC) is what prevents Entra's model from propagating into the rest of the system.

---

## Lesson 4 — The Bounded Context Canvas: designing each context before mapping

Before or during the context mapping session, it is useful to characterize each bounded context individually before trying to map the relationships between them. The Bounded Context Canvas (Nick Tune, ddd-crew) is the standard tool for this.

The canvas is a single-page template with eleven sections:

1. **Name.** The context's identity. Naming is harder than it sounds: the name should reflect the bounded context's primary responsibility, not an implementation artifact.
2. **Purpose.** A one-sentence business-focused description of why this context exists.
3. **Strategic Classification.** Three dimensions: importance (core domain, supporting domain, or generic capability), business model role (revenue generator, engagement creator, or compliance enforcer), and evolution stage (genesis, custom-built, product, or commodity). This classification influences how much investment the context deserves and whether building it in-house is justified.
4. **Domain Roles.** Whether the context analyzes data, executes workflows, coordinates other contexts, or plays other recognizable archetypes.
5. **Inbound Communication.** What messages (commands, queries, events) arrive at this context, from whom, and what relationship type governs each.
6. **Outbound Communication.** What messages this context sends, to whom, and what relationship type governs each.
7. **Ubiquitous Language.** Key terms and their meanings *within this context*. The same word in a neighboring context's canvas with a different definition is a boundary discovered.
8. **Business Decisions.** The key business rules and policies this context enforces.
9. **Assumptions.** What the team is taking for granted in the current design — visible assumptions are addressable; invisible assumptions are time bombs.
10. **Verification Metrics.** How the design decisions will be validated.
11. **Open Questions.** Unanswered design questions, visible at a glance.

The canvas is not mandatory before a context mapping session, but the Ubiquitous Language, Inbound Communication, and Outbound Communication sections directly feed the map. For a project with 11 bounded contexts, completing an abbreviated canvas for each context (Name, Purpose, Strategic Classification, Ubiquitous Language, and Inbound/Outbound) before the mapping session makes the relationship-labeling step significantly faster.

The Strategic Classification section is particularly valuable for CritterCab's first session. Identifying which bounded contexts are **core** (the domain capabilities that differentiate CritterCab from a generic message-passing demo), which are **supporting** (necessary but not unique), and which are **generic** (commoditized, could be bought rather than built) determines where modeling effort should be concentrated. Dispatch is clearly a core domain. Identity (as an ACL) is supporting. Payments is likely generic. That classification should be visible on the canvas.

### Applicability to CritterCab

- **Abbreviated canvases for all 11 BCs are a useful pre-session artifact.** Completing just Name, Purpose, Strategic Classification, and Ubiquitous Language for each context before the mapping session takes 30–45 minutes and pays back in the session itself by pre-identifying the language boundaries that Context Mapping needs to surface.
- **The Strategic Classification will help prioritize Event Modeling effort.** Core domain contexts (Dispatch, Trips) get the deepest Event Modeling treatment. Supporting and generic contexts get lighter treatment. The canvas makes this visible before the workshop starts.
- **The Ubiquitous Language section is where "trip" disambiguation happens.** Writing down what "trip" means in Dispatch (a matching lifecycle), in Trips (an event-sourced journey), in Payments (a billable transaction), and in Operations (a historical record) — side by side in separate canvases — is the exercise that prevents Event Modeling from stalling on naming disputes.

---

## Lesson 5 — Domain Message Flow Modeling: the bridge to Event Modeling

Context Mapping tells you *what the relationships are*. Domain Message Flow Modeling (Nick Tune, ddd-crew) tells you *how the contexts communicate* within each relationship. It is the step that sits between Context Mapping and Event Modeling in the design sequence.

A Domain Message Flow Diagram is a single-scenario visualization showing the sequence of messages — commands, events, and queries — between actors, bounded contexts, and external systems. It is similar in vocabulary to Event Modeling but simpler in scope: it models a single business scenario end-to-end rather than the whole system on a timeline.

The notation is minimal: actors (users or systems initiating the flow), bounded contexts (boxes), messages (labeled with name, type, and key data), and arrows showing direction and sequence. The message types correspond to the Event Modeling vocabulary: commands are requests to change state, events are past-tense facts that resulted, queries are requests for information.

The key contribution of Domain Message Flow Modeling relative to Event Modeling is feedback on the proposed context boundaries. A flow diagram for the "rider requests and is matched to a driver" scenario — crossing Identity, Rider Profile, Dispatch, Telemetry, and Trips — makes the integration seams visible at a level of detail that the context map alone does not provide. If the flow requires too many hops, or if a context appears in every scenario, those are signals that the boundary placement may be wrong.

The technique recommends 5–9 messages per diagram (Miller's Law) before splitting into sub-scenarios. This constraint forces the modeler to choose the right grain for each scenario rather than modeling the entire system in one diagram.

### Applicability to CritterCab

- **Domain Message Flow Modeling is the right intermediate step between CritterCab's context map and its Event Modeling workshop.** ADR-004 sequences Context Mapping → Domain Storytelling → Event Modeling. Domain Message Flow Modeling fits most naturally after Context Mapping and before or during the Event Modeling storyboard step — it provides the scenario-level clarity that Domain Storytelling provides in narrative form, in a more structured diagrammatic format.
- **Draw at least one flow diagram for each of the four candidate narratives** identified in the Event Modeling research guide (Lesson 13): the rider-requests-trip spine, the driver-goes-online-and-completes flow, the operator-suspends-driver flow, and the surge-pricing flow. Each of these crosses multiple bounded contexts in a way that will surface integration questions that the context map alone leaves implicit.
- **The "too many hops" smell applies directly to CritterCab.** If a flow diagram for the rider-request scenario requires the message to touch 6+ bounded contexts before a driver is offered, that is a signal. Either the boundaries are wrong, or the flow genuinely requires that many hops — in which case the Event Modeling swim-lane step needs to acknowledge the latency and failure-mode implications.

---

## Lesson 6 — Running a Context Mapping session

Unlike Event Modeling, which has a well-defined seven-step workshop format, Context Mapping does not have a canonical session structure. The technique is more analytical than generative — you are making implicit relationships explicit, not brainstorming a timeline. The Global App Testing case study (twelve bounded contexts, seven sessions, three months) is the most detailed first-person account available.

The key variables are:

**Scope.** Decide whether you are mapping the whole system or a subset. For CritterCab's first session, map the whole system at relationship-identification depth. Deep-dive into the most complex relationships afterward.

**State.** Decide whether you are capturing the AS-IS (current reality) or designing a TO-BE (target state). CritterCab is greenfield, so there is no AS-IS to reverse-engineer. The first map is a TO-BE — but it should be treated as a design hypothesis, not a commitment. See Lesson 7.

**Depth per relationship.** First pass: identify all relationships and label them with the pattern name. Second pass: characterize each relationship — what messages cross it, what translation happens, what triggers it. The Bounded Context Canvas's Inbound/Outbound sections and Domain Message Flow Diagrams provide the second-pass depth.

A practical session structure for CritterCab's first context mapping session:

**Part 1 — Populate the board (30 minutes).** Place all 11 bounded contexts as boxes. Add the known external actors: Entra External ID, mobile client (rider app, driver app), and Operations users. No edges yet.

**Part 2 — Identify relationships (45 minutes).** For each pair of contexts that has any interaction, draw an arrow. Do not label patterns yet. Focus on getting all relationships visible. For each arrow, note the direction: who influences whom?

**Part 3 — Label patterns (30 minutes).** For each arrow, apply the most appropriate pattern label. This is where the discussion happens: "Is Identity → Rider Profile a Customer/Supplier or a Publish/Subscribe event flow?" The answer shapes whether Rider Profile is conformist (subscribes to Identity events and adapts its model) or protected by an ACL (translates Identity's events into its own language).

**Part 4 — Validate with flow scenarios (30 minutes).** Draw one Domain Message Flow Diagram for the most complex scenario (rider requests a trip). Walk each bounded context the message touches and verify that the context map relationships explain each hop. If a hop requires a pattern that is not on the map, add it. If the pattern labels turn out to be wrong, update them.

**Part 5 — Abbreviated Bounded Context Canvases (30–45 minutes, can be async).** For each bounded context, record Name, Purpose, Strategic Classification, and Ubiquitous Language. This can be done asynchronously before the session and reviewed during it, or produced during the session as a closing exercise.

Total: 2.5–3 hours.

### Applicability to CritterCab

- **Solo facilitation applies.** Same persona rotation discipline as the Event Modeling workshop guide recommends. The Domain Expert persona is the most valuable one for Part 3 (pattern labeling) because the right pattern often depends on domain knowledge, not just technical judgment.
- **Claude-as-Skeptic between sessions applies.** After the session, export the draft map to a markdown description and prompt Claude as Skeptic: "what relationships did I miss? where is the pattern choice likely wrong? what context-boundary ambiguity would cause trouble in Event Modeling?" This is the highest-leverage AI use for this step.
- **The context map is committed to `docs/workshops/` at session close**, not "later." The artifact format is a markdown file with: the BC list, a textual description of each relationship and its pattern label, the reasoning for non-obvious pattern choices, one or more Domain Message Flow Diagram descriptions, and the abbreviated Bounded Context Canvas summaries. A visual diagram (Miro, draw.io, or ASCII) can be linked or embedded, but the markdown file is the durable artifact.

---

## Lesson 7 — AS-IS vs TO-BE maps, and the greenfield case

Context mapping literature distinguishes two map types:

**AS-IS.** Captures the current state — the actual relationships, actual integration patterns, and actual power dynamics as they exist today, including messy ones. The Global App Testing article's key lesson: "a frequent mistake is attempting to fix the reality and create a Map of contexts that we would like to have, instead of mapping the existing landscape." An AS-IS map is honest about Big Balls of Mud, accidental conformist relationships, and upstream teams that do not factor downstream priorities into planning.

**TO-BE.** Captures the intended target state. Used for greenfield design (like CritterCab) or for planning a migration away from a problematic current state.

The greenfield case has a specific risk: a TO-BE map without the discipline of AS-IS mapping tends to be optimistic. All the relationships look clean. All the patterns look deliberate. The Big Balls of Mud and the accidental conformist relationships are absent because they have not happened yet.

The discipline for a greenfield TO-BE map is to treat it as a **design hypothesis** and explicitly mark anything that is assumed rather than proven. The Bounded Context Canvas's Assumptions section is exactly the right place for this. "We assume that Dispatch can query Telemetry synchronously within the latency budget of an offer fan-out" is an assumption that belongs on the Dispatch canvas, visible at a glance, before the Event Modeling workshop treats it as settled.

A greenfield context map should also be produced in both an AS-IS and a TO-BE variant when legacy external systems are involved. CritterCab integrates with Entra External ID. The AS-IS map for that integration shows the raw Microsoft Graph change notification API crossing a boundary into CritterCab. The TO-BE map shows that boundary mediated by the Identity ACL. The difference between the two is the ACL's value: the AS-IS shows what the system would look like without it.

### Applicability to CritterCab

- **CritterCab's first context map is a TO-BE design hypothesis.** Label it as such in the artifact. Mark assumptions explicitly.
- **The Entra External ID integration is the one place where an AS-IS perspective adds value.** Document what the raw Microsoft Graph integration looks like before the Identity ACL mediates it, then document how the ACL transforms it. This makes the ACL's value visible in the artifact rather than merely asserted.
- **Revisit the context map after the Event Modeling workshop.** The swim-lane step (ADR-004, Step 3) will likely cause at least one boundary refinement. When it does, the context map should be updated in the same PR as the Event Model update.

---

## Lesson 8 — AI/LLM assistance in Context Mapping

LLM assistance in Context Mapping is less mature than in Event Modeling — the tooling ecosystem is thinner, and the technique is more analytical (pattern-labeling, relationship identification) than generative (brain-dump of events). But several modes are genuinely useful:

**Mode 1: Domain knowledge acquisition.** Before the session, use Claude with the ride-sharing-lessons-learned.md document in context to ask: "For a ride-sharing system with these bounded contexts, what are the most likely upstream/downstream relationships? What are the likely ACL candidates? Where have real ride-sharing systems found boundary placement to be wrong?" This is the Domain Expert persona in a specific analytical role.

**Mode 2: Pattern diagnosis.** After placing relationships on the map, describe each relationship to Claude and ask which pattern most accurately characterizes it. The pattern choice is often contested — especially between Customer/Supplier and Conformist, or between Open Host Service and plain Upstream/Downstream. An external assessment of the trade-offs is valuable even when you end up disagreeing with it.

**Mode 3: Skeptic review.** After the session, export the draft map to a textual description and prompt Claude as Skeptic to review it: "What context boundaries look wrong? Where is the pattern choice likely to cause problems? What scenario would break the assumed relationships? What did I miss?" This is the same LLM-as-Skeptic pattern that the Event Modeling guide recommends between sessions.

**Mode 4: Ubiquitous Language disambiguation.** List the shared terms that appear across multiple bounded context vocabularies and ask Claude to surface where the same word might mean different things in the ride-sharing domain. Feed this output into the Domain Storytelling session that follows.

One significant guardrail: LLMs pattern-match to training data. For novel domain boundaries — where the right answer is genuinely not obvious from precedent — the LLM will produce a plausible-looking answer that may be wrong. Use LLM output as the Skeptic's challenge, not as the authoritative answer.

### Applicability to CritterCab

- **Claude with ride-sharing-lessons-learned.md in context is a real capability for Mode 1.** The research document carries Uber/Grab/DoorDash architectural knowledge. Loading it during a context mapping session and invoking Domain Expert mode produces responses grounded in production ride-sharing experience.
- **Modes 2 and 3 are the highest-leverage uses.** Pattern diagnosis for the Identity/Rider Profile and Dispatch/Telemetry relationships specifically — where the pattern choice has downstream consequences for Event Modeling and service topology — benefits from an independent assessment.
- **Do not use LLM output to skip the session.** Asking Claude to produce the full context map from the vision doc would generate a plausible-looking map, but the value of the mapping session is in the thinking, not in the artifact. The session surfaces assumptions that would otherwise stay implicit. The map is the evidence that the thinking happened.

---

## Lesson 9 — Common mistakes to catch

**Mapping the ideal, not the actual.** The single most common mistake. A context map that shows only clean, well-intentioned relationships is aspirational documentation, not a design tool. Even in a greenfield system, the honest map includes the messy external systems (Entra External ID with its own model and its own change cadence), the assumed relationships that might not hold, and the boundaries that are genuinely uncertain.

**Conflating context maps with deployment diagrams.** A context map and a service topology are related but not identical. Two bounded contexts can share a deployment unit (ADR-002) while still having a context map relationship with a pattern label. The map captures model relationships; the service topology captures deployment boundaries. Conflating them produces either an over-specified map or an under-specified topology.

**Missing the direction of the arrow.** An undirected edge between two contexts loses the most important information: who influences whom. Every relationship has a direction. If it genuinely seems symmetric, it is either Partnership (label it as such) or the direction has not been thought through carefully enough.

**Treating pattern labels as permanent.** A Customer/Supplier relationship can degrade into Conformist if the upstream team stops factoring downstream priorities into planning. A Conformist relationship can be upgraded to an ACL relationship if the upstream model turns out to be more corrupting than expected. Pattern labels are diagnoses of current reality, not architectural commitments. Update them when reality changes.

**Creating one big map instead of multiple focused ones.** For a system with 12 bounded contexts, a single map has 66 potential relationships. Most of them are Separate Ways. The useful information is in the non-Separate-Ways relationships, and one big map buries them. The Global App Testing case study solved this by creating per-context maps (12 maps, each showing one context's relationships with all others), then merging them. For CritterCab, a per-context view may be more useful than a single whole-system view.

**Skipping the Ubiquitous Language section.** The language boundary is what makes a context map useful. If the map does not record what the key terms mean in each context, a future contributor cannot determine whether a given relationship requires translation (ACL) or direct adoption (Conformist). Language is the map's data; the pattern labels are the analysis.

**Over-using Shared Kernel.** Shared Kernel is attractive because it feels clean — "we share just this one thing." In practice, every Shared Kernel grows. What starts as a shared Address type becomes a shared Customer concept becomes a shared data model. The cost of the kernel's coordination overhead grows with its size. For CritterCab, ADR-002 already prohibits shared application-layer code between services, which forecloses Shared Kernel in the deployment-boundary sense. The pattern can still appear in the context map as a conceptual marker, but it should not produce a shared library.

**Not updating the map when implementation reveals a gap.** ADR-003's spec-anchored regime requires retrospectives to catch drift between specification and code. The context map is part of the specification. If an implementation session reveals that what was drawn as a Customer/Supplier relationship is actually a Conformist relationship in practice, the map and the retrospective should both record the correction.

---

## What NOT to copy from the broader Context Mapping ecosystem

**Don't model organizational structure as a proxy for context boundaries.** Conway's Law (systems mirror organizational communication structures) is a descriptive law, not a prescriptive design tool. Drawing bounded context boundaries to match team boundaries is fine if the team boundaries reflect the domain. It is problematic if the team boundaries are accidental (historical org structure, not domain structure). CritterCab has no org structure to conform to, so this trap is less immediate — but Event Modeling's swim-lane step (which applies Conway's Law) should draw boundaries from domain analysis, not from "what would be natural to hand to separate teams."

**Don't treat the Context Mapper DSL (CML) as a required deliverable.** Context Mapper is a powerful tool for expressing context maps in a machine-readable DSL and generating diagrams, documentation, and service skeletons from them. It is valuable for teams that want to keep their context map in version control alongside code and generate downstream artifacts from it. For CritterCab's first session, a markdown file with a textual description and a linked diagram is sufficient. Adopting CML adds tooling overhead that is not justified by the project's current needs.

**Don't produce a context map and then discard it.** The map is a living artifact. It evolves as the Event Modeling workshop refines boundaries, as implementation reveals incorrect assumptions, and as the system grows. A context map written before the Event Modeling workshop and never updated is a snapshot of early thinking, not a design reference.

**Don't conflate Published Language with a transport protocol.** Protobuf is CritterCab's Published Language for gRPC integrations. HTTP/REST is a transport protocol, not a Published Language. gRPC is a transport; Protobuf is the language. A Published Language can be expressed over multiple transports; a transport protocol does not imply a Published Language.

---

## Guidance for CritterCab's first Context Mapping session

The following is a concrete plan for the first context mapping session. Adapt deliberately, not by accident.

**Scope.** The full system: 11 tentative bounded contexts plus the known external actors (Entra External ID, mobile clients, Microsoft Graph, Operations users). One session at relationship-identification depth; Domain Message Flow Diagrams for the two most complex scenarios (rider-requests-trip spine, driver-accepts-and-completes).

**Pre-work (before the session, 30–45 minutes async).** For each of the 11 BCs, write a two-sentence abbreviated canvas: Name, Purpose, Strategic Classification (core / supporting / generic), and three to five key terms in the Ubiquitous Language. This pre-work surfaces language boundaries before the session rather than during it. Load the completed canvases into the session artifact at the start.

**Session structure.** Single 2.5–3-hour session.

1. **Populate the board (15 minutes).** Place all 11 BCs and external actors as nodes. No edges.
2. **Draw all non-Separate-Ways edges (45 minutes).** For each BC, identify which other BCs or external actors it has a meaningful relationship with. Draw directed edges showing influence direction (upstream → downstream). Do not label patterns yet.
3. **Label patterns (45 minutes).** For each edge, apply the most appropriate pattern. Record the reasoning for any non-obvious choice in the artifact. Pay particular attention to: Identity → Rider Profile, Identity → Driver Profile (are these Customer/Supplier with event subscription, or is the downstream Conformist to Identity's model?), Dispatch → Telemetry (Customer/Supplier with synchronous query?), Entra External ID → Identity (ACL is certain; what is the upstream pattern?).
4. **Draw two Domain Message Flow Diagrams (30 minutes).** Scenario 1: Rider requests a trip (actor → Rider Profile / Identity → Dispatch → Telemetry → Dispatch → Driver Profile). Scenario 2: Driver accepts an offer and a trip starts (Dispatch → Trips, Trips → Payments, Trips → Ratings). Walk through each and verify that the pattern labels on the context map are consistent with the flows.
5. **Record assumptions and open questions (15 minutes).** Any pattern choice that is contested or uncertain should be recorded as an assumption with a trigger for resolution. Any boundary that might merge or split during Event Modeling should be flagged.

**Tool.** Miro (free tier) or draw.io for the visual map. Markdown file at `docs/workshops/001-context-map.md` as the durable artifact. The Miro board can be linked from the markdown file but is not the deliverable.

**Participants.** Erik, rotating through the Domain Expert, Skeptic, and Facilitator personas. Claude invoked after the session as Skeptic with ride-sharing-lessons-learned.md in context. Claude invoked during step 3 for pattern diagnosis on contested relationships.

**Output artifact at `docs/workshops/001-context-map.md`.** Contents:
- Session log (date, duration, persona rotation notes)
- External actors list
- Bounded context list with abbreviated canvases (Name, Purpose, Strategic Classification, Ubiquitous Language)
- Relationship list: for each relationship, the two contexts, the direction (upstream/downstream), the pattern label, and one sentence of reasoning
- Domain Message Flow Diagrams (two scenarios, described in text or as embedded ASCII diagrams)
- Assumptions list (explicit, with resolution triggers)
- Open questions (items that require Domain Storytelling or Event Modeling to resolve)
- Retrospective (what the session confirmed, what it changed from the vision doc's tentative BC list, what to watch for in Domain Storytelling)

**Expected context map shape for CritterCab.** Based on the vision doc and ride-sharing lessons learned, the map is likely to show:
- A cluster of downstream contexts around Identity (Rider Profile, Driver Profile, Onboarding all downstream)
- A Telemetry → Dispatch → Trips chain (Telemetry is pure upstream; Trips is where business events accumulate)
- Trips as the main upstream for Payments, Ratings, and Operations
- Identity as an ACL between Entra External ID and the domain
- Dispatch as the most connected context in the system — multiple upstream dependencies (Telemetry, Identity/Driver Profile) and the most latency-sensitive integration surface
- Operations as purely downstream, consuming read models from most other contexts with no upstream influence

---

## Open questions this research surfaces for CritterCab

**1. Should the context map use AS-IS and TO-BE variants for the Entra External ID integration?** The recommended answer is yes: document the raw Microsoft Graph integration shape (AS-IS) and the Identity ACL-mediated shape (TO-BE) side by side. This makes the ACL's value visible rather than assumed.

**2. Does Pricing remain inside Trips or separate from day one?** This is flagged as an open question in the vision doc. The context mapping session will clarify whether Pricing has its own ubiquitous language (fare, surge factor, calculation policy) distinct enough from Trips' language (trip lifecycle, state machine) to justify a separate context from day one.

**3. Is the Dispatch → Telemetry relationship synchronous query (Customer/Supplier) or event subscription (Upstream/Downstream publish)?** The answer has architectural consequences. Synchronous query means Dispatch's offer fan-out latency includes a Telemetry round-trip. Event subscription means Dispatch maintains a local cache of location data, with staleness risk. The Domain Message Flow Diagram for the rider-requests-trip scenario will surface which pattern is load-bearing.

**4. Is there a Published Language candidate for non-gRPC inter-service events?** ADR-009 captures Erik's aspiration to use Protobuf as the schema language for all boundaries, not just gRPC. The context map session is a good place to identify which boundary crossings are candidates for this experiment — specifically the ASB business-event integrations, where Protobuf as the message schema would require explicit support from the ASB transport configuration.

**5. Does the Onboarding BC justify its own context, or does it fold into Driver Profile?** The vision doc separates them on the grounds that the lifecycle and domain language are different. The Ubiquitous Language exercise in the context map session will either confirm that separation (Onboarding's terms are genuinely distinct from Driver Profile's terms) or challenge it.

---

## Recommended follow-up reading

If only two resources can be consumed before the session:

1. **ddd-crew/context-mapping (GitHub README and reference card).** The most compact authoritative reference for all nine patterns and the three team relationship types. Read it the day before the session. Available at github.com/ddd-crew/context-mapping.

2. **ddd-crew/bounded-context-canvas (GitHub README).** The canvas template and explanations of all eleven sections. Use it to produce the abbreviated pre-work canvases. Available at github.com/ddd-crew/bounded-context-canvas.

Secondary reading, ordered by CritterCab relevance:

- **Vaughn Vernon, *Implementing Domain-Driven Design*, Chapter 3.** The deepest practical treatment of context mapping patterns, including the team dynamics that determine which pattern applies.
- **Global App Testing, *Domain Cartographers — how to draw a Context Map*.** The best first-person case study of a real context mapping exercise. The seven-session structure and the "per-context view" approach are both worth reading before planning the session.
- **ddd-crew/domain-message-flow-modelling (GitHub README).** The notation guide for Domain Message Flow Diagrams. Short. Read before drawing the scenario diagrams in step 4.
- **Alberto Brandolini, *Strategic Domain Driven Design with Context Mapping* (InfoQ, 2009).** The oldest and most influential practitioner article on the technique. Still accurate on the fundamentals despite its age.

---

## Document history

- **v0.1** (2026-04-23): Initial research pass on Context Mapping as a strategic DDD technique. Covers the nine patterns, upstream/downstream dynamics, the Bounded Context Canvas and Domain Message Flow Modeling as complementary tools, session structure, AS-IS vs TO-BE maps, AI assistance, and common mistakes. Intended to precede CritterCab's first Context Mapping session and complement `docs/research/event-modeling-workshop-guide.md`. Primary sources: Eric Evans (blue book), Vaughn Vernon (IDDD), ddd-crew GitHub organization (context-mapping, bounded-context-canvas, domain-message-flow-modelling), Context Mapper documentation, Global App Testing domain cartographers case study.
