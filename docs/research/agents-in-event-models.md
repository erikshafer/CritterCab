# Agents in Event Models — Notes for CritterCab

> **Source posts (all LinkedIn, January 2026):**
> - Marc Klefter — *Agent as automation in an event model* ([post](https://www.linkedin.com/feed/update/urn:li:activity:7450796519370444800))
> - Marc Klefter — *Unifying agent session events and domain events in a single store* ([post](https://www.linkedin.com/feed/update/urn:li:activity:7447961517154770944))
> - Marc Klefter — *Capturing translation decisions as events* ([post](https://www.linkedin.com/feed/update/urn:li:activity:7447235053689995264))
> - Jake Bruun — *Five Event Modeling slices for temporary account lockout* ([post](https://www.linkedin.com/feed/update/urn:li:activity:7444425884981432320))
>
> **Author context.** Marc Klefter works at AxonIQ and his posts recommend AxonIQ specifically. Jake Bruun is the creator of *evntd*. Neither is in the Critter Stack orbit. The vendor coupling in their posts is stripped out here; the modelling ideas transfer directly to Marten + Wolverine.
>
> **Image access caveat.** The LinkedIn posts each reference "the figure below" — in Klefter's case, an event model diagram; in Bruun's case, the entire post is a five-slice board. The research fetch tool used for these notes could not ingest the image content itself, only the surrounding text. The distillation below works from the written arguments. The Bruun post specifically deserves a separate pass with the board image in hand before its implications are fully captured.

---

## Why This Matters for CritterCab

CritterCab's vision commits to an Event-Modeling-first design phase, a Marten event store, and Wolverine as the runtime substrate. The Klefter posts answer a question that is about to become load-bearing: **when an AI agent participates in a CritterCab workflow — as a dispatcher, a support triage assistant, or an operator tool — how does it appear in the event model, and where does its state live?**

Three possible answers exist, and only one of them is coherent with the architecture already chosen:

1. The agent is external and invisible to the model — it just calls the API.
2. The agent is external, but its session log lives in a separate vendor-managed store (Anthropic's `getSession(id)`, LangGraph's checkpointer, etc.).
3. The agent is a first-class automation in the event model, and its session events are appended to the same event store as the domain events.

Klefter's argument is that only answer (3) preserves the properties CritterCab wants: single system of record, queryable decision history, durable agent execution, and a model whose blueprint matches the runtime reality. The other answers leave decisions fragmented across systems or opaque to the model.

Bruun's post is adjacent but differently focused — it demonstrates that a small, bounded automation capability (temporary account lockout with a timed expiry) can be decomposed into five slices on an event model board. It is useful as a concrete reference for *what a complete slice set looks like* for an automation-heavy capability, which is exactly the shape CritterCab's dispatch and trust/safety flows will take.

---

## Klefter Post 1 — Agents as Automations in the Event Model

### The claim

An agent can be represented in an Event Model in the same way any other automation is: a process sticky on the timeline that reads one or more views, issues one or more commands in response, and produces events as consequences. It is not a new notation. It is not a special swim lane. It is just an automation whose internal reasoning happens to be an LLM.

### The concrete scenario he walks through

A stock reservation fails. That failure is an event on the timeline. Downstream of it:

1. **Agent session begins** — triggered by the failure event (the same way any automation is triggered by an event).
2. **Agent queries read models** — for stock levels across warehouses, historical substitution patterns, customer preferences, whatever it needs to reason about resolution. These are the same read models any other automation or query-side consumer uses.
3. **Agent invokes tools that issue commands** — to place replenishment orders, propose substitutions, reassign reservations to other warehouses. Each tool invocation is a command on the model; each command produces an event; each event is on the timeline.
4. **Agent invokes a `ProposeResolution` tool for human approval** — a human-in-the-loop gate. The proposed resolution is written to a read model that a human operator sees in an approval UI. The human's decision (accept / reject / modify) flows back through another command.

### The `AgentUpdates` read model

Klefter flags this as a **general requirement for agentic scenarios in event-sourced systems**, not a one-off: an `AgentUpdates` projection that the agent writes into (or, more precisely, that is built from agent-emitted events). Its job is the feedback channel — it communicates the agent's current thinking, intermediate findings, and proposed actions to any human or system that needs to observe or interrupt the session. Without it, the session is a black box.

The read model is derived from events the agent emits during reasoning (tool calls, intermediate conclusions, proposed actions). These are agent *session* events, not domain events — which is exactly what Post 2 is about.

### The caveat he explicitly calls out

> Emitted session events by the agent are **not** included in this event model.

This is the important modelling discipline. The event model shows the agent's *boundary interactions* — the commands it issues, the events those commands produce, the views it reads, the proposals it writes. The agent's *internal* session events (tool call traces, intermediate reasoning, prompt/response pairs) are not on the model. If they were, the model would drown in noise that has no bearing on the domain behaviour.

Put differently: the model shows what the agent *does to the domain*, not how the agent *thinks*. The session event log (Post 2's topic) is where the internal events live.

### Applicability to CritterCab

The domain automations in CritterCab's vision that are most likely to become agentic are not commodity CRUD flows — they are the ones with irreducible uncertainty: dispatch rebalancing during supply/demand shocks, fraud pattern recognition in the Trust & Safety BC, escalation handling in Customer Support, dynamic surge pricing decisions. These are exactly the shape of Klefter's stock-shortage example: a domain event signals that something non-trivial has happened; the response requires judgement over multiple read models; the action taken should be auditable.

For the first workshop, this means: **when the board calls for an automation that is too complex for a deterministic rule but too consequential to ignore, draw it as a normal automation sticky and annotate it "agentic".** The notation does not change. The implementation will eventually route that automation's decision step to an agent rather than a scripted handler, but the model need not care yet.

What the model *must* capture, following Klefter's discipline:
- The triggering event (the domain event that starts the session).
- The read models the agent consults (named, not glossed).
- The commands the agent can issue (each one a known, deterministic command on some aggregate — the agent does not invent commands).
- Any human-in-the-loop proposal step (as its own command + read model + view).
- The read model that surfaces agent status to humans (the `AgentUpdates` equivalent).

What the model should *not* try to capture:
- The internal token-by-token reasoning.
- Individual tool-use turns within the session.
- Prompt templates or model versions.

---

## Klefter Post 2 — Unified Event Store for Session and Domain

### The problem he names

Anthropic's Managed Agents design (and equivalent LangGraph/OpenAI Responses patterns) treats the session log as a *separate* durability concern. Klefter quotes the framing directly:

> The session log sits outside the harness; nothing needs to survive crashes.

Restart semantics are: `getSession(id)` retrieves the log, the harness replays it, the agent resumes.

This works in isolation, but it creates a problem the moment the agent issues a tool call that maps to a domain command. The example Klefter uses: a `RequestBike` tool invocation produces a `BikeRequested` event in the *domain* event store. Meanwhile, the agent's `tool_call` turn is recorded in the *session* event store. The agent's reasoning trace and the domain state change it caused are now in two different systems of record.

The consequences:

| Problem | What it looks like in practice |
|---|---|
| Manual correlation | Forensic queries need agent session ID → domain event store → session store joins across systems. Possible, but ad-hoc and fragile. |
| Divergent retention policies | Session logs typically have shorter retention than domain events. Losing the session log loses the *why* of a domain decision. |
| Two sources of truth for "what the agent did" | The session log says the agent called `RequestBike`. The domain log says `BikeRequested`. If they disagree (e.g., command rejected), reconciling is non-trivial. |
| No single replay boundary | Durable execution needs to replay both session state (to resume reasoning) and domain state (to reconcile with reality). Two stores mean two replay concerns. |

### The proposal

A single event store backs both concerns. Agent session events and domain events are both appended to the same global stream (or, more practically, to streams within the same store). Events carry an agent identifier when they are attributable to a session. The Dynamic Consistency Boundary (DCB) pattern is used to query events by tag combinations — so:

- The **decision model** for "what is the current state of reservation 42" is built by querying events tagged `reservation-42`.
- The **agent state** for "resume session abc" is built by querying events tagged `agent-session-abc`.
- Both reads hit the same store. Both write paths append to the same store. There is one system of record.

This is what Klefter means by *durable execution on an event-sourced foundation*: the same substrate that makes the domain recoverable makes the agent recoverable.

### Mapping this to Marten + Wolverine (CritterCab's stack)

Klefter's post name-drops AxonIQ because AxonIQ's session component is vendor-built to do exactly this. CritterCab is not using AxonIQ, but the pattern transfers without awkwardness:

**Marten already is the global event store.** Streams in Marten are just tag-delimited views over an append-only table; tagging events with agent session identifiers is mechanically trivial. A session ID becomes just another stream identifier, and agent session events become a stream in the same store that holds `ReservationCreated` and `RideDispatched`.

**DCB-style reads are native to Marten.** Marten's event projections can filter by event type, by stream, or by tag. The decision model for a domain aggregate is built from its events; the reasoning state for an agent session is built from its events; both use the same `IDocumentSession.Events.FetchStream` or projection infrastructure.

**Wolverine supplies the command/event plumbing.** The agent's tool calls become Wolverine commands. The handlers that back them produce domain events. Wolverine's correlation ID flows naturally from the agent session through to the resulting events, which gives CritterCab the tagging it needs without a custom correlation fabric.

**The agent's `getSession(id)` equivalent is just `FetchStreamAsync("agent-session-abc")`.** The harness calls Marten to replay the session; the replay produces the same tool-call/tool-result sequence that Anthropic's Managed Agents replay produces; the agent resumes. The difference is that the same replay engine also services domain aggregate loads.

### Applicability to CritterCab

This is the most actionable of the four posts. It does not mandate anything yet — CritterCab has not shipped a single agent — but it argues for a specific design decision when the first agentic automation appears:

**Decision rule:** *when an agent is introduced into a CritterCab bounded context, its session events are persisted to Marten, in the same database as that bounded context's domain events, tagged with the session ID. They are not persisted to a vendor-managed session store.*

Justification:
- It keeps the architecture coherent with "each service owns its own data store" (no extra agent store to provision per service).
- It makes agent-initiated decisions auditable by the same query paths as any other decision.
- It avoids committing to a vendor's session format before CritterCab knows which agent frameworks it will use long-term.

Candidate ADR topic when this decision has to be made: *Agent session persistence in event-sourced bounded contexts*. Not urgent, but worth naming now so it is not defaulted into quietly.

---

## Klefter Post 3 — Translation Decisions as First-Class Events

### The claim

When a system's job is to coordinate external systems of record, the naive modelling response is to draw a Translation slice and move on — the translation is "just plumbing." Klefter's argument: if the translation involves a *decision*, the decision itself should be captured as an event in the local event store, regardless of whether any state-change in the local system is strictly required.

### The scenario he walks through

A support ticket is resolved. The agent evaluates the customer's history and decides the circumstances warrant escalation to their Customer Success Manager — both to assess the current support package and to explore an upsell opportunity. Two external systems are involved: the ticketing system (where the ticket lives) and the CRM (where the CSM assignment lives).

**The naive Event Modeling treatment** is the left-hand side of his figure: a Translation slice. The local system is a coordinator. It reads from the ticket system, writes to the CRM, and holds no state of its own. Nothing is event-sourced locally because the local system "owns nothing".

**Klefter's proposed treatment** is the right-hand side: introduce a `SupportEscalated` event in the local event store. The event captures:
- The decision itself (escalation occurred).
- The reasoning behind it (why this customer, why now, what was considered).
- The external systems involved and the actions taken on each.

The local system now has a canonical record of the decision, which can be projected into downstream read models (Klefter specifically mentions a **Context Graph** — a richly relational read model over domain entities — as a consumer that benefits from this).

### Why this matters

Klefter's underlying diagnosis is that in integrated architectures, *decision traces end up fragmented across a multitude of systems*. The ticket system knows the ticket was resolved. The CRM knows a CSM was assigned. Neither knows *why*. The reasoning lived in some logs, some Slack threads, some agent session traces — all of which decay, rotate, and disappear on different schedules.

A translation event is cheap (one event type, one projection) and solves the fragmentation: a single shared history of *decisions made* is durably captured, independently of the external systems whose state those decisions mutated.

### Applicability to CritterCab

The vision doc already commits CritterCab to several translation-heavy boundaries: Entra External ID for identity, Microsoft Graph change notifications, ASB as a business-event backbone, and the Go polyglot service participating over gRPC. Every one of these is a candidate for Klefter's treatment.

Two CritterCab-specific cases where a translation decision event is probably the right call:

**Identity reconciliation in the Identity BC.** When an Entra External ID user is linked to a CritterCab rider profile (or a driver profile), that link is a decision — often one requiring heuristics (email match, phone match, identity assertion). The link itself is state in the domain, so this already warrants an event. But the *reasoning* — which signals matched, which were missing, whether a human reviewed it — is exactly the kind of thing that falls out of the model if it is not explicitly captured. A `RiderIdentityReconciled { signals, reviewer, confidence }` event would survive the eventual day when Entra is swapped out.

**Cross-BC risk decisions in Trust & Safety.** When the Trust & Safety BC decides to flag a driver based on signals from Dispatch, Ride, and Payments — each of which is a separate system — the decision is a translation in Klefter's sense. The external systems don't change state immediately (the driver's flag is local to T&S), but the *decision* references signals from all three. A `DriverFlagged { signals, policy_version, decision_path }` event is the canonical record. This composes neatly with the Context Graph idea: the flag event, the signal events it references, and the driver entity all become nodes in a queryable relational projection.

### The crosswalk to the Event Modeling workshop guide

The existing workshop guide describes Translation slices as "coordinating an external system without holding local state" (Lesson 3). Klefter's addition is not in tension with that — it is a refinement: **when the translation involves a decision rather than just a routing or lookup, promote the decision to a first-class local event even when the aggregate state change is entirely external.**

A simple workshop heuristic: for every Translation slice drawn during the session, ask "was a decision made here, or is this just carrying data across the boundary?" If a decision was made, the slice gains an event.

---

## Bruun Post — Five Slices for Temporary Account Lockout

### The board

Bruun's evntd board is a reference-quality Event Model of a complete temporal automation capability. Transcribed from the image (reading left-to-right along the timeline):

**Top row — wireframes and automations:**

| Element | Type | What it is |
|---|---|---|
| **Security Settings** (top-left) | UI / wireframe | Account Lockout config form — checkbox to enable, numeric "Lock after N failed attempts", radio toggle between Permanent / Duration modes, numeric "Lockout duration M minutes", Save button. |
| **Account Locker** (gear icon, top-middle) | Automation | A conventional process automation — triggered by a read model, issues a command. |
| **Account Unlocker** (gear icon with clock-rewind glyph, top-right) | Automation | A temporal automation — the clock glyph signals "time-driven" rather than event-driven. Its trigger is the passage of time plus a matching todo-list entry, not a domain event. |

**Middle row — commands (blue) and read models (green):**

| Element | Type | Contents on board |
|---|---|---|
| **Configure Account Lockout** | Command (blue) | `enabled: true`, `failed_attempts: 5`, `lockout_mode: duration`, `lockout_duration_minutes: 5` |
| **Failed AuthN Attempts** | Read model (green) | `failures_by_account: {"a1": 5}`, `lockout_duration_minutes: 5`, `failed_attempts: 5` — a per-account failure counter *combined with the current lockout policy*. |
| **Temporarily Lock Account** | Command (blue) | `account_id: a1`, `lock_expires_at: 2026/03/30 12:05:00` |
| **Accounts To Unlock\*** | Read model (green, asterisk suffix) | `account_id: a1`, `unlock_after: 2026/03/30 12:05:00` — the asterisk is the Event Modeling convention for a **todo-list read model**: a projection shaped specifically for an automation to poll for work. |
| **Unlock Account** | Command (blue) | `account_id: a1` |

**Bottom row — events (orange):**

| Element | Contents |
|---|---|
| **AccountLockoutConfigured** | `enabled: true`, `failed_attempts: 5`, `lockout_mode: duration`, `lockout_duration_minutes: 5` |
| **AuthenticationFailed** (×5) | `account_id: a1`, `reason: wrong password`, `at: 2026/03/30 12:00` — four prior stickies plus one labelled one, representing the fifth failure that trips the threshold. |
| **AccountTemporarilyLocked** | `account_id: a1`, `lock_expires_at: 2026/03/30 12:05:00` |
| **Account Unlocked** | `account_id: a1` |

**Arrows and flow:**

1. Security Settings form → `Configure Account Lockout` command → `AccountLockoutConfigured` event.
2. `AccountLockoutConfigured` + the five `AuthenticationFailed` events → feed into the `Failed AuthN Attempts` read model (one orange curve from the config event, one from the failed-auth events).
3. The `Failed AuthN Attempts` read model triggers the **Account Locker** automation (green arrow).
4. Account Locker issues the `Temporarily Lock Account` command → `AccountTemporarilyLocked` event.
5. `AccountTemporarilyLocked` + `AccountLockoutConfigured` → feed into the `Accounts To Unlock*` todo-list view.
6. The `Accounts To Unlock*` view triggers the **Account Unlocker** automation when `unlock_after` elapses (green arrow).
7. Account Unlocker issues the `Unlock Account` command → `Account Unlocked` event.

### The five slices

The capability decomposes into five vertical slices, each with one command, zero-to-one events, and the read model it contributes to:

| # | Slice | Command | Event | Read model consumed/produced |
|---|---|---|---|---|
| 1 | Configure lockout policy | Configure Account Lockout | AccountLockoutConfigured | feeds Failed AuthN Attempts and Accounts To Unlock\* |
| 2 | Count failed auth attempts (translation/input) | *(none — AuthenticationFailed is upstream)* | AuthenticationFailed (×N) | feeds Failed AuthN Attempts |
| 3 | Temporarily lock account (automation) | Temporarily Lock Account (issued by Account Locker) | AccountTemporarilyLocked | consumes Failed AuthN Attempts; feeds Accounts To Unlock\* |
| 4 | Track accounts awaiting unlock (projection) | *(none — pure projection)* | *(none)* | Accounts To Unlock\* (todo-list) |
| 5 | Unlock account on expiry (temporal automation) | Unlock Account (issued by Account Unlocker) | Account Unlocked | consumes Accounts To Unlock\* |

### What the board teaches that is reusable

Three patterns are demonstrated here that CritterCab will need repeatedly.

**The todo-list read model as an automation's work queue.** The `Accounts To Unlock*` view is not a query for humans — no UI reads it. It exists solely so the Account Unlocker automation has something to poll or subscribe to. Its shape is dictated by the automation's needs: one row per account currently awaiting unlock, a timestamp field the automation compares against `now()`, a key field the automation uses as the command target. The asterisk is the notational cue that "this read model is somebody's todo list, not somebody's view." Every CritterCab temporal automation (offer expiry, auth hold expiry, verification token expiry, driver session reaping) will want exactly this pattern.

**Configuration as events, not as settings.** `AccountLockoutConfigured` is a first-class event in the event store — not a row in a settings table. This means: the policy has history (you can ask "what was the lockout threshold when account X was locked on date Y?"), the policy fits naturally into read-model projections (the Failed AuthN Attempts view joins failure counts with the *then-current* threshold), and policy changes are auditable. This is worth internalising because "just put it in a config table" is the tempting default, and the default is worse than the event-sourced version.

**Two automations, each with a different trigger shape.** Account Locker is event-driven (threshold crossed → lock). Account Unlocker is time-driven (clock reaches a stored timestamp → unlock). Both are drawn with the same green automation sticky, but the Unlocker's gear icon carries a clock-rewind glyph — a subtle but important visual distinction. An Event Model that needs to distinguish "fires on event" from "fires on time" should adopt this convention: the sticky is the same, the glyph is the tell.

### Applicability to CritterCab

This board is the reference decomposition for every timer-driven expiry in CritterCab's scope. The concrete candidates:

| CritterCab flow | Config event | Trigger read model | Todo-list read model | Terminal event |
|---|---|---|---|---|
| Ride offer expires if not accepted | `OfferExpiryPolicyConfigured` | (none — direct from offer) | `OffersAwaitingExpiry*` | `OfferExpired` |
| Payment auth hold released if unused | `AuthHoldPolicyConfigured` | (none) | `HoldsAwaitingRelease*` | `AuthHoldReleased` |
| Driver session times out on inactivity | `SessionTimeoutPolicyConfigured` | `DriverActivity` view | `SessionsAwaitingExpiry*` | `DriverSessionExpired` |
| Verification token expires | `TokenPolicyConfigured` | (none) | `TokensAwaitingExpiry*` | `VerificationTokenExpired` |

The pattern is identical to Bruun's board in each row: config event → optional trigger view → state-change command → "event that schedules the expiry" → todo-list projection → temporal automation → terminal event. Once one of these is modelled in a workshop, the others are near-mechanical.

This deserves promotion: the "temporal automation slice pattern" is worth adding as a named pattern in the Event Modeling workshop guide before CritterCab's first workshop. Bruun's board is the reference. The notation convention — green sticky for automation, clock-rewind glyph for time-driven, asterisk on the read model it consumes — is worth adopting verbatim.

---

## Synthesised Design Principles

These are the principles that emerge when the four posts are read together, in the order they should be applied during a CritterCab Event Modeling workshop:

### 1. Agents are automations; do not invent new notation

An agent on the Event Model is a green automation sticky. It is triggered by an event, reads views, issues commands, and produces events just like any other automation. The fact that its internal decision is made by an LLM is an implementation detail of the automation's handler, not a modelling primitive. Resist any temptation to introduce an "agent lane" or an "AI lane" — the notation already handles it.

### 2. Model the boundary, not the thinking

Show the agent's inputs (view reads), outputs (commands, proposals), and feedback channel (the `AgentUpdates`-style read model). Do not attempt to model intermediate tool calls, reasoning steps, or prompt contents. If the agent's internal activity matters for audit or replay, it belongs in the session event stream (see principle 4), not on the model.

### 3. Every agent needs an observer read model

For any agentic automation, a read model that reports "what the agent is currently doing and what it has proposed" is not optional — it is the interruption and approval surface. In CritterCab terms: before an agentic automation is drawn on the board, identify the read model that an operator would consult to see what the agent is doing. If you cannot name it, the design is not yet complete.

### 4. Session events live in the same event store as domain events

When CritterCab introduces its first agentic automation, the session event log is persisted to the same Marten instance as that bounded context's domain events, tagged with a session identifier. This is a design commitment to make once, early. The alternative — vendor-managed session stores — fragments the system of record and creates the correlation problems Klefter describes.

### 5. Promote decisions to events, even across pure-translation boundaries

When a Translation slice involves a *decision* (not just routing or enrichment), introduce a local event to capture the decision and its reasoning. This keeps decision history durable independently of the external systems whose state was mutated, and makes the decision projectable into downstream read models (Context Graphs, audit views, escalation dashboards).

### 6. Temporal automations are first-class slices

Time-driven transitions (timer expiries, scheduled unlocks, offer timeouts, authorisation holds) are drawn with the same stickies as any other automation. They are not sidebar concerns. The Bruun account-lockout reference is a candidate model for how these slices arrange when they need explicit start + expiry + effect modelling.

---

## Open Questions

These are the decisions CritterCab does not yet need to make, but will need to make when the first agentic flow arrives. Naming them explicitly so they are not defaulted into:

1. **Which bounded context hosts the first agentic automation?** Dispatch (surge / rebalancing), Trust & Safety (fraud triage), and Customer Support (escalation) are the most plausible candidates. The choice affects which service's Marten instance holds the session store.
2. **How are agent session identifiers allocated?** Wolverine correlation IDs are a natural fit, but a session spans many messages and may need its own identifier distinct from per-message correlation.
3. **What is the retention policy for agent session events?** Domain events are retained indefinitely (standard event-sourcing practice). Session events may not need to be, particularly if they contain prompt/response content that raises privacy or cost concerns. A distinct archival policy for tag=agent-session events is probably warranted.
4. **Does the Context Graph idea justify its own research note?** Klefter references it in passing as a downstream read model consumer. It is a pattern worth exploring independently — a relational projection over event-sourced data, likely in a graph store or graph-shaped Marten projection. Not urgent, but flagged.
5. **At what point does CritterCab need a `ProposeResolution`-style human approval pattern?** The moment the first agent is allowed to issue a command that has user-visible consequences (charge a card, suspend a driver, reassign a ride), a generalised approval pattern becomes necessary. Better to model it once, in one bounded context, than to invent it separately in each.

---

## Related CritterCab Research

- [Event Modeling: A Guide for CritterCab's First Workshop](./event-modeling-workshop-guide.md) — Translation slices, automations, and the view/command/event notation this note extends.
- [Agent Workflow Patterns](./agent-workflow-patterns.md) — Agent *development* workflows (Nick Tune, Anthropic). This note is the sibling covering agent *runtime* participation in the domain, rather than the build process.
- [Spec-Driven Development: Event Model to Code](./sdd-event-model-to-code.md) — Martin Dilger's SDD methodology. Bruun's slice-decomposition discipline and the given/when/then specifications SDD names are the same practice from different angles.
- [Context Mapping: A Guide for CritterCab's First Session](./context-mapping-guide.md) — Translation relationships between bounded contexts, which is where Klefter's "decision events across translation boundaries" guidance lands when formalised.
