# Domain Discovery Playbook — Notes for CritterCab

> **Source articles:**
> - [Beyond Problem and Solution Space](https://nick-tune.me/blog/2024-01-09-beyond-problem-and-solution-space-better-models-for-modern-p/) — Jan 2024
> - [Domain Discovery Facilitation: Make Scale Explicit](https://nick-tune.me/blog/2022-09-20-domain-discovery-facilitation-make-scale-explicit/) — Sep 2022
> - [Learning Diverse Question Formats to Get Better Insights](https://nick-tune.me/blog/2022-07-24-learning-diverse-question-formats-to-get-better-insights/) — Jul 2022
> - [From Consultant to Facilitator](https://nick-tune.me/blog/2022-01-22-from-consultant-to-facilitator/) — Jan 2022

---

## Why This Matters for CritterCab

CritterCab's workflow places workshops and narratives before any implementation. These four articles form the methodology for running those sessions well — how to frame the vocabulary of discovery, how to adopt a facilitator's stance rather than a consultant's, how to ask questions that surface what's hidden, and how to calibrate questions to surface quantitative insights that redirect priorities. The techniques apply directly to CritterCab's Event Modeling sessions, Domain Storytelling sessions, and the narrative authoring that follows them.

---

## Part 1: The Vocabulary of Discovery

*Source: Beyond Problem and Solution Space (Jan 2024)*

### The Problem with "Problem Space"

"Problem space" and "solution space" are used so inconsistently across product and engineering conversations that the terms create confusion rather than clarity. Different participants in the same room hear different things. Time is spent arguing about what the terms mean rather than about the domain itself.

This matters for CritterCab's workshop sessions: if the vocabulary participants use to frame discussions is ambiguous, the narratives and bounded context definitions that emerge from those sessions inherit that ambiguity.

### Better Models

**Indi Young's Three-Space Model**

| Space | Purpose |
|---|---|
| Problem Space | Understanding people — their needs, contexts, mental models. Listening techniques. |
| Strategy Space | Choosing which needs to address and what product types to develop. |
| Solution Space | Discovery (building and testing hypotheses) + Development (implementing features). |

Young's model separates the *choosing* (strategy) from both the *understanding* (problem) and the *building* (solution). This matters: a team spending time in strategy space is doing different work than one in problem space, even if both would casually describe themselves as "working on the problem."

**Teresa Torres' Opportunity-Solution Tree**

Torres argues that "problem" is too narrow a frame: products don't only solve problems, they create possibilities. Her model connects:

```
Business Outcomes → Opportunities → Solutions → Bets
```

"Opportunity" replaces "problem" as the mid-level concept. An opportunity is anything worth addressing — a frustration, a desire, an unmet need, or an unexploited capability. This framing is more honest about what modern product work actually involves.

**Tune's Implementation Space**

When developers refer to "solution space," they usually mean *implementing known requirements* — writing code to specification. Tune proposes calling this "implementation space" to distinguish it from the hypothesis-testing work of solution discovery. This avoids conflating design exploration with execution.

**John Cutler's Mandate Levels**

A nine-level framework for team autonomy, from Level I (full autonomy — build and discover anything) to Level A (told exactly what to build). A team's mandate level shapes their perception of which spaces they operate in. Feature factory teams perceive only implementation space as their domain. Teams with Level I autonomy operate across all three of Young's spaces.

For CritterCab: each service team should understand which mandate level it operates at. The architecture is built around autonomous bounded contexts, which implies high mandate levels — teams own their data, their domain model, and their trade-offs.

### Practical Recommendation

Replace "problem space" and "solution space" in workshop conversations with the specific model being used. Before a session, agree on vocabulary:

- Are we doing *problem space* work (listening, understanding users)?
- Are we doing *strategy space* work (deciding which opportunities to pursue)?
- Are we doing *solution discovery* (designing and testing hypotheses)?
- Are we doing *implementation* (delivering a known spec)?

CritterCab's narrative phase maps to problem and strategy space. Prompts map to the boundary between solution discovery and implementation.

---

## Part 2: Facilitator vs. Consultant

*Source: From Consultant to Facilitator (Jan 2022)*

### The Distinction

| Mode | Design intent | Role of expert |
|---|---|---|
| Consultant | Extract information → make proposals and decisions | Central: leads and recommends |
| Facilitator | Create space for attendees to lead themselves | Peripheral: enables and holds space |

A consultant enters a workshop knowing what they want to find out, and designs the session to surface it. A facilitator enters knowing what conditions will help participants discover things for themselves, and designs the session around those conditions.

The failure mode of the consultant approach: the workshop surfaces only what the consultant thought to look for. The implicit knowledge that participants hold — the things they would have said if asked differently — never emerges.

### The Shift in Practice

Applying a facilitator mindset means:

- **Postponing direct advice.** When participants are stuck, resist the impulse to provide the answer. Ask a question instead.
- **Questionnaires where clients answer their own questions.** Instead of asking "what are your biggest problems?", design a questionnaire that leads participants through a reflection process that surfaces the same answer from their own experience.
- **Grounding reflection in participants' actual work.** Learning based on peer experiences and real examples is more actionable than theory or constructed scenarios.
- **Personal sharing activities.** These build connection and change the ambient trust level in the room in ways that purely analytical activities do not. Participants engage differently after sharing something personal, and this carries through to the quality of domain discussion.

### Liberating Structures

Liberating Structures is a collection of facilitation microstructures that shift control toward participants. They range from simple conversational techniques (1-2-4-All, which builds ideas in progressively larger groups) to more complex activities (Troika Consulting, Conversation Café). The unifying principle is that engagement and insight quality improve when participants have more agency over how they interact.

For CritterCab sessions: a domain modeling session structured as a lecture followed by Q&A will surface less than one structured as small-group discovery followed by synthesis. Design for participant agency.

---

## Part 3: Making Scale Explicit

*Source: Domain Discovery Facilitation: Make Scale Explicit (Sep 2022)*

### The Core Technique

Domain discovery workshops surface a great deal of qualitative information — the shapes of processes, the actors involved, the events that matter. But they systematically underweight *quantitative* information about how often things happen, how many people are involved, how much time things take, and how volume affects behavior.

Making scale explicit means introducing questions that force participants to put numbers on things they usually describe qualitatively. The insights that emerge often redirect priorities more sharply than any structural analysis of the domain.

"The scale of something influences the importance and how we treat it."

This technique works especially well during Event Storming, where any event or aggregate on the board can be probed for scale. It works even when the facilitator lacks industry knowledge — the questions are transferable to any domain.

### Eight Scale Question Patterns

**1. How many people assume this role?**

When a process step is performed by a human, quantify the staffing level before designing automation. A five-person team performing a step manually has different automation economics than a five-hundred-person team. This question prevents wasting design effort on problems affecting minimal staff.

**2. How many different roles can the same person play?**

Role overlap reveals where skilled employees are absorbing low-value work. When someone with specialized expertise routinely performs tasks well below their skill level, there is both a frustration problem and an opportunity problem. This question uncovers it.

**3. How often does this happen?**

Frequency questions expose misconceptions between teams. A developer who has seen this event three times in a month assumes it is rare. A customer support agent who fields it daily knows it is not. Making frequency explicit creates a shared model and repositions priorities.

**4. What is the frequency of each scenario?**

When a process branches into multiple paths, the distribution of that branching matters. A scenario that occurs 90% of the time versus one that occurs 10% of the time should be treated with very different investment levels. Understanding the distribution before designing for all paths changes the design.

**5. How many at a given point in time?**

Single-user process maps miss concurrency. A process that looks simple when diagrammed for one user may be extremely complex at scale — competing for resources, racing to update shared state, managing backpressure. This question reveals concurrency concerns that linear diagrams hide. "Orders per minute at peak time" is a fundamentally different design input than "a customer places an order."

**6. How can the duration vary?**

Time variability carries hidden business impact. A step that usually takes ten seconds but occasionally takes three hours has a very different effect on upstream and downstream flows than its average time suggests. Making the range explicit, not just the average, surfaces the tail that matters most to system design.

**7. How has and will the scale change over time?**

Systems designed for 1,000 users often fail at 50,000. Making historical and forward-looking scale explicit contextualizes current design decisions and exposes when a design that was once adequate has become a liability. This question also reveals whether current constraints are real constraints or inherited assumptions.

**8. What if this number was higher or lower?**

Challenge any specific number visible in the domain model. "Why £200 and not £150?" exposes whether a threshold, limit, or configuration value was the product of deliberate analysis or arbitrary convention. Many domain parameters that look like business rules turn out to be accidents.

### Application to CritterCab Sessions

During Event Modeling or Domain Storytelling workshops for CritterCab's bounded contexts, any event on the board is a candidate for scale questioning:

- **Ride requested**: How many per minute at peak? How has this changed over the lifetime of the business? What happens to dispatch latency when volume doubles?
- **Driver assigned**: How many concurrent open requests does a driver typically have? What is the timeout before reassignment?
- **Payment processed**: What proportion fail? What is the distribution across payment methods? What is the p99 processing time?

These questions are not about implementation — they inform bounded context sizing, event bus topology choices, and SLA expectations that belong in narratives before a line of code is written.

---

## Part 4: Diverse Question Formats

*Source: Learning Diverse Question Formats to Get Better Insights (Jul 2022)*

### Why Format Matters

Direct questions produce direct answers. Direct answers reflect what participants already consciously know and are comfortable saying. The more interesting insights — the ones that change what gets built — often live in the adjacent space: feelings about the work, inversions of the current thinking, associative responses to images or metaphors.

Diverse question formats create conditions for these insights to surface. They are more engaging, disrupt habitual response patterns, and lead to deeper reflection.

### When to Use Structured Questioning

Surveys and questionnaires apply at five points in the session lifecycle:

| Moment | Purpose |
|---|---|
| Pre-workshop | Understand attendees, calibrate session design |
| Workshop | Synchronous activities (typically 5-minute timeboxes) |
| Post-workshop | Feedback, determine next steps |
| Sense-making | Understand feelings and improvement opportunities in a business area |
| New engagement | Explore fit and mutual understanding with a new team or client |

For CritterCab: a pre-session questionnaire for a bounded context workshop surfaces what participants already believe about the domain before anyone has drawn an event map. A post-session survey captures whether the session changed those beliefs.

### Six Question Formats

**1. Complete the Sentence**

Prompts emotional and associative responses that direct questions suppress.

*Example:* "One thing about this process that makes me most uncertain is ______."

In a domain modeling context: "One assumption about this bounded context that I'm least confident in is ______." Completes differently for different participants; gaps and divergences between completions reveal the edges of shared understanding.

**2. Choose an Emotion**

Participants select from an emotion or feelings wheel to describe their response to a topic. This shifts engagement from purely cognitive to emotional-cognitive, which surfaces different information — particularly around pain, frustration, and excitement that participants would not volunteer in a direct question.

**3. Pick an Image**

Visual selection (e.g., Ethnographica Deck) stimulates novel thinking. Abstract domain concepts — "what does the handoff between Dispatch and Rides feel like?" — become accessible when participants can respond with an image rather than a definition.

**4. Worst Possible**

Invert the question to lower the stakes and unlock creative thinking.

*Example:* "What would be the worst possible design decision for this bounded context?" 

Participants are more willing to voice concerns framed as hypothetical bad ideas than as direct criticisms. The responses often name the exact risks and anti-patterns that matter most.

**5. Just for Fun**

A lighthearted question that creates psychological safety and signals that creative thinking is welcome.

*Example:* "If this service were a person, what would their personality be?"

Lowers the ambient seriousness of technical discussions and often produces memorable characterizations of bounded contexts that carry through the project.

**6. Devils Advocate**

Constructively challenges a belief that the group has settled on — not to oppose it, but to pressure-test it.

*Example:* "Let's assume the opposite is true. What would we have to believe for that to be correct?"

Applied after the group has converged on a model, this surfaces the assumptions that are doing the most work and identifies which ones are worth validating before committing to a design.

---

## Synthesized Facilitation Approach for CritterCab Workshops

These four articles converge on a coherent facilitation philosophy. For CritterCab's workshops:

### Before the session

- Agree on vocabulary (problem/strategy/solution/implementation) so that framing questions don't confuse participants about what kind of work they are doing
- Understand participants' mandate levels — what decisions they own and what they surface to others
- Prepare scale questions for the events and aggregates most likely to surface in the session
- Design a pre-workshop questionnaire using at least one non-direct format (Complete the Sentence or Choose an Emotion) to surface implicit beliefs before anyone has influenced them

### During the session

- Hold a facilitator's stance: ask questions that create space for participants to discover rather than presenting analysis for them to respond to
- When a process step or event surfaces, probe it for scale: frequency, volume, concurrency, time variability, historical trend, what-if
- When the group converges prematurely on a design, apply Devils Advocate to surface the load-bearing assumptions
- When energy drops, use a Just for Fun question to reset

### After the session

- Use a post-session survey to distinguish between what changed for participants and what remained fixed — this is input for the next iteration of the narrative
- Treat scale data collected during the session as first-class input to narrative authoring, not background color

### The link to narratives

CritterCab's narratives are the formal output of discovery. The quality of a narrative is bounded by the quality of the discovery that preceded it. A narrative that does not reflect scale ("the driver receives a ride request" without any quantitative context) is a weaker specification than one that does ("at peak, a driver may have three concurrent unacknowledged requests, each with a 30-second response window before reassignment"). Making scale explicit during workshops is how that specificity enters the narratives.
