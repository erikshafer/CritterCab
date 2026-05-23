# Spec-Driven Development — Notes for CritterCab

> **Source article:** [A Simple Guide to Spec-Driven Development](https://www.linkedin.com/pulse/simple-guide-spec-driven-development-martin-dilger-qotqf) by Martin Dilger. Also republished with expansions as the [March 2026 eventmodelers.de version](https://www.eventmodelers.de/docs/blog/spec-driven-development/) — annotated alongside this note in the [canonical sources doc](./event-modeling-canonical-sources.md).

---

## Why This Matters for CritterCab

Dilger's Spec-Driven Development (SDD) is the closest published methodology to what CritterCab's workflow is building toward. It names the same primitives already in place — skill files, event modeling, given/when/then specifications — and gives them a closed-loop implementation structure: the event model drives the agent, the skill files constrain it, the tests confirm it, and a learnings file makes it progressively smarter. CritterCab's narrative → prompt → execute → retrospective loop is a close relative; this article provides the vocabulary and mechanics to tighten it.

## What This Note Extends (Dymitruk 2019 Lineage)

Dilger's Spec-Driven Development extends two primitives Adam Dymitruk introduced in his June 2019 *[What is Event Modeling?](https://eventmodeling.org/posts/what-is-event-modeling/)*:

- **Given-When-Then specifications** — Dymitruk introduces these as the behavioural contract per slice (via BDD). SDD operationalizes them as the AI agent's source of truth, status-gated by humans so the agent picks up slices only when their GWT rules are complete.
- **The 7-step workshop format** — Dymitruk's seven steps (brainstorming, the plot, time travel, identifying inputs and outputs, applying Conway's Law, elaborating scenarios) inform the discovery half of SDD's *"Step Two — Event Model as the Source of Truth."*

The eventmodelers.de March 2026 republication of the SDD article (annotated alongside this note in the canonical-sources doc) adds an origin-story opener and renames the Ralph Loop step to *"The Night Shift."* The methodology's substance — what this note documents — is unchanged.

For the full corpus context, see [Event Modeling Canonical Sources](./event-modeling-canonical-sources.md).

---

## The Core Philosophy

SDD separates two kinds of work that AI-assisted development frequently conflates:

- **Human work**: thinking, architecture, design decisions, business rules — everything that requires judgment
- **Agent work**: the mechanical translation of a clear specification into working code — everything that is tedious but deterministic once the spec is precise

The agent is framed not as a collaborator that reasons about the domain, but as "a very disciplined, very tireless junior developer." It does not decide what to build. It does not evaluate trade-offs. It implements exactly what the specification says, according to the patterns the skill files define.

This framing has a significant implication: **agent quality is a function of specification quality, not agent capability.** Investing in the event model and skill files is investing in output quality. Attempting to compensate for a weak specification by prompting more cleverly is the wrong lever.

---

## The Four Steps

### Step 1 — Blueprint Architecture

Before the agent touches any code, a human implements 2–3 representative slices manually. This takes 2–3 hours of focused work and produces:

- The canonical API layer structure for this project
- The pattern for organizing state-change slices
- The test case conventions (naming, arrangement, assertion style)
- The command handler design

These hand-coded examples answer a question that no amount of general-purpose agent instruction can answer: *what does "good" look like in this specific codebase?* The answer is different for every project.

The implemented examples are then codified into **skill files** — precise, structured descriptions of how to build things in this context. The skill files are not documentation about the code; they are operational instructions the agent reads before implementing any slice.

### Step 2 — Event Model as Source of Truth

The specification lives in the event model — not in markdown files, not in tickets, not in conversation history. The event model is exported as structured JSON directly into the repository, versioned alongside the code it specifies. This means specifications and implementations share a version history.

Each slice in the event model carries:
- Its business capability (what this slice represents in the domain)
- **Given/when/then rules** defining what the slice should do, allow, and reject
- A **status field** that governs the agent's behavior toward it

Typical slice specifications contain 3–30 given/when/then rules. The rules are behavioral — they describe outcomes, not implementation steps. The skill files handle the how; the given/when/then rules handle the what.

**The status field is the human-to-agent handoff signal.** A slice remains in design until a human judges that its specification is complete and precise enough to implement. At that point, they change status to `"planned"`. This is a commitment: the spec is ready. The agent picks up any slice marked `"planned"`, sets it to `"in progress"`, and begins.

### Step 3 — The Agent Loop (The Ralph Loop)

The agent runs continuously in a named loop: **the Ralph Loop**. Its operating cycle:

```
Find slices with status "planned"
  → Set status to "in progress"
  → Read skill files
  → Implement according to given/when/then rules
  → Run tests
  → Record learnings
  → Clear context
  → Repeat
```

**Context clearing is load-bearing.** Dilger is explicit: "the longer a session runs, the worse the output gets." Allowing the agent to accumulate context across slices compounds errors and causes the agent to drift from the skill file conventions. Context is cleared after every slice. The agent starts each iteration fresh.

**Learnings persist across context clears.** A separate learnings file records corrections, edge cases, and discovered rules from previous iterations. This file grows quickly at first — each new slice tends to surface something unexpected — then stabilizes as the patterns become well-established. The agent reads the learnings file at the start of each iteration, so early mistakes do not recur in later slices. The agent "gets smarter the longer you run it" precisely because learnings accumulate while context does not.

### Step 4 — The Check

The morning review: if the tests are green, move forward. If not, investigate.

This is not the review-everything workflow. Quality was enforced upstream:
- The blueprint architecture defined what good looks like
- The skill files encoded that definition operationally
- The given/when/then rules specified the expected behaviors
- The agent implemented exactly that

The result is code that "looks like code you would have written yourself, because in a very real sense, you wrote the rules it followed." Review effort concentrates on specification quality — did the given/when/then rules correctly capture the intent? — rather than on implementation details.

---

## Handling Change

Requirements change. In traditional approaches, this triggers rework: code that was written for the old requirement must be partially or fully replaced. In SDD, it triggers a spec update:

1. Find the affected slice in the event model
2. Update its given/when/then rules to reflect the new requirement
3. Set its status back to `"planned"`
4. The agent re-implements it on the next loop iteration

The underlying architecture — defined by the blueprint and encoded in skill files — remains stable. Only the behavioral specification changes. This is why Dilger says SDD lets teams "stop fearing change": the cost of change is proportional to the scope of the spec change, not to the amount of code that was written.

---

## Key Named Concepts

| Concept | Definition |
|---|---|
| **Slices** | Small, distinct business capabilities as the unit of implementation — one slice = one deliverable behavior |
| **Blueprint Architecture** | 2–3 hand-coded reference slices that define project standards and become the basis for skill files |
| **Skill Files** | Structured instructions encoding project-specific architectural patterns; the agent's authoritative guide to "how we build things here" |
| **Event Model** | The source of truth for all specifications; exported as versioned JSON into the repository |
| **Given/When/Then Rules** | Behavioral specifications per slice (3–30 rules); define what the slice does, allows, and rejects |
| **Status Field** | The `planned` / `in progress` / `done` lifecycle signal on each slice; `"planned"` is the human handoff to the agent |
| **Ralph Loop** | The agent's operating cycle: find planned → implement → test → learn → clear context → repeat |
| **Learnings File** | A persistent, cross-iteration record of corrections and discovered rules; grows then stabilizes |

---

## Comparison with CritterCab's Current Workflow

| CritterCab step | SDD equivalent | Gap |
|---|---|---|
| Event Modeling / Domain Storytelling workshop | Event model authoring | CritterCab's event model output should be the formal spec, not just workshop notes |
| Narrative authoring | Given/when/then rule authoring per slice | Narratives could adopt the given/when/then format explicitly |
| Skill file authoring (`docs/skills/`) | Blueprint architecture → skill files | Skill files already exist; need to be kept operationally precise, not documentary |
| Prompt authoring | Slice status → `"planned"` | Status change is simpler and more explicit than a separate prompt document |
| Execute (implementation) | Ralph Loop iteration | Context clearing and learnings persistence are not yet formalized in CritterCab's loop |
| Retrospective | Learnings file entry | Retrospectives and learnings serve the same function; could be unified or linked |

The most significant structural difference: CritterCab uses a separate prompt document as the implementation trigger, while SDD uses a status field on the slice. The prompt document carries richer context (narrative references, skill references, acceptance criteria), which is valuable — but the status-field model makes the handoff boundary crisp and machine-readable. Both can coexist if the prompt document is the specification artifact and the status field is the trigger.

---

## Application to CritterCab

### Given/When/Then in Narratives

CritterCab's narratives describe domain behaviors in prose. SDD suggests formalizing the behavioral content of each narrative slice as explicit given/when/then rules. These rules are:
- Precise enough to be implementable without interpretation
- Small enough to be individually testable
- Complete enough to define the boundary between correct and incorrect behavior

For CritterCab, a narrative covering "Driver accepts a ride request" would carry rules like:
```
Given: a ride request exists with status "offered" and the driver is the assigned candidate
When: the driver accepts
Then: the ride request status transitions to "accepted"
      a DriverAccepted event is published
      the offer expiry timer is cancelled

Given: a ride request exists with status "offered"
When: a different driver attempts to accept
Then: the command is rejected with reason "not the assigned driver"

Given: a ride request exists with status "accepted"
When: the assigned driver attempts to accept again
Then: the command is rejected with reason "already accepted"
```

This level of specificity is what separates a narrative that guides implementation from a narrative that describes it after the fact.

### Skill Files as Operational, Not Documentary

CritterCab's `docs/skills/` files currently serve as implementation guidance — patterns and conventions for a given component or cross-cutting concern. SDD's skill files are narrower: they encode exactly what the agent needs to produce consistent, architecture-conforming code. The distinction is between "here is the context and reasoning" (documentary) and "here is the pattern to follow" (operational).

As CritterCab moves toward implementation, skill files should include:
- Canonical code examples for each slice type (command handler, query, event projection, gRPC endpoint)
- File naming and project structure rules
- Test structure and assertion patterns
- Wolverine-specific conventions (handler shape, message routing, middleware)
- Marten-specific conventions (session usage, event appending, aggregate loading)

The blueprint architecture step — implementing 2–3 reference slices by hand first — is the mechanism for producing these. It should not be skipped.

### The Learnings File and Retrospectives

CritterCab's retrospectives capture session-level learning: what went well, what to improve, what decisions were made. SDD's learnings file is iteration-level: what specific rule did the agent get wrong, what correction was applied, what edge case was discovered. These are complementary. Retrospectives operate at the session grain; learnings files operate at the slice grain.

A CritterCab learnings file could be structured as:
- One entry per discovered rule or correction
- Linked to the slice that surfaced it
- Persisted across sessions (unlike the retrospective, which closes a session)

Over time, stable learnings from this file migrate into skill files, where they are formalized as conventions rather than corrections.

### The Status Field as Explicit Handoff

CritterCab's current handoff from design to implementation is the prompt document. Prompt authoring is valuable — it consolidates the narrative references, skill references, and acceptance criteria the agent needs. But the trigger for implementation is implicit: the existence of a prompt document in the right location.

A status field on the narrative or slice itself makes the handoff explicit: when the narrative's given/when/then rules are complete and the referenced skill files exist, the human sets status to `"planned"`. The agent (or the next session's prompt) knows unambiguously what is ready. This could be as simple as a frontmatter field in the narrative document.
