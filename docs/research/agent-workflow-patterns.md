# Agent Workflow Patterns — Notes for CritterCab

> **Source series A:** Four blog posts by Nick Tune, January–March 2026.
> - [Coding Agent Development Workflows](https://nick-tune.me/blog/2026-01-07-coding-agent-development-workflows/) — Jan 7
> - [Dev Workflows as Code](https://nick-tune.me/blog/2026-01-17-dev-workflows-as-code/) — Jan 17
> - [Hook-driven Dev Workflows with Claude Code](https://nick-tune.me/blog/2026-02-28-hook-driven-dev-workflows-with-claude-code/) — Feb 28
> - [Event-sourced Claude Code Workflows](https://nick-tune.me/blog/2026-03-04-event-sourced-claude-code-workflows/) — Mar 4
>
> **Source series B:** Three Anthropic blog posts on multi-agent architecture, 2025.
> - [Common Workflow Patterns for AI Agents and When to Use Them](https://claude.com/blog/common-workflow-patterns-for-ai-agents-and-when-to-use-them)
> - [Building Multi-Agent Systems: When and How to Use Them](https://claude.com/blog/building-multi-agent-systems-when-and-how-to-use-them)
> - [Multi-Agent Coordination Patterns](https://claude.com/blog/multi-agent-coordination-patterns)

---

## Why This Matters for CritterCab

CritterCab's session workflow (narrative → prompt → execute → retrospective) already frames implementation as a disciplined loop. Tune's series is a detailed engineering treatment of the same intuition: that agent behaviour becomes reliable only when the *process itself* is modelled, enforced, and observable — not just described in markdown and hoped at. His vocabulary is DDD-native (aggregates, commands, events, event sourcing), making the ideas directly portable.

---

## The Problem: Why Naive Agent Orchestration Fails

Tune's starting point is a failure he calls **wobbling**: when you instruct a main agent to orchestrate a multi-step pipeline (run lint, then review, then submit PR), it improvises. It skips steps, interprets instructions loosely, and competes for context window space between orchestration reasoning and domain reasoning. The more steps, the less reliable the result.

The root cause: agents are not deterministic orchestrators. They are reasoners. Giving a reasoning model a mechanical sequence to execute is a category error.

---

## The Evolution: Four Stages

### Stage 1 — Coding Agent Development Workflows (Jan 7)

The first article maps out what a complete task-completion pipeline should cover, and identifies the conventions and tooling needed to make it work.

**The pipeline:**
```
Assign ticket
  → Create feature branch
  → /complete-task command
      → Verify gate (build, lint, test)
      → Code-review subagent
      → Task-check subagent (QA against requirements)
      → Submit-PR subagent
      → Wait on CI (CodeRabbit, SonarQube, custom checks)
      → Autonomous fixes on feedback
```

**Subagents as context isolation.** Each subagent gets a fresh context window with a single clear focus. This prevents the main agent from spending its context budget on orchestration instead of domain reasoning, and gives reviewers clean, unpolluted context to work in.

**The single source of truth for conventions.** Tune's most directly actionable insight: maintain one set of convention documents, and wire *every* automated consumer to that same source. Both the local AI review agent and CodeRabbit (or any other tool) read the same files:

```yaml
# .coderabbit.yaml
knowledge_base:
  code_guidelines:
    enabled: true
    filePatterns:
      - docs/conventions/software-design.md
      - docs/conventions/testing.md
      - docs/architecture/overview.md
```

If local review and CI review disagree, it is because they read different things. Fix the source, not the tool.

**Structured task format.** Task tickets are rejected unless they include all ten sections:
1. Deliverable specification
2. Context
3. PRD traceability
4. Acceptance criteria
5. Edge case scenarios
6. Implementation guidelines
7. Testing strategy
8. Verification commands
9. (additional sections per Tune's template)

This maps closely to CritterCab's prompt template and narrative structure.

**The continuous improvement mindset.** Every manual intervention or agent error is a process failure, not a one-off. The remediation question is always: *how do I encode this so it cannot happen next time?* Either a lint rule, a convention addition, or a gate improvement.

---

### Stage 2 — Dev Workflows as Code (Jan 17)

The second article addresses wobbling directly by removing the agent from the orchestration role entirely.

**Three-tier architecture:**

| Tier | Responsibility | Technology |
|---|---|---|
| Claude command layer | Entry point; Claude runs one command and receives JSON | Claude Code slash command |
| TypeScript orchestration | Deterministic workflow engine; no LLM overhead | TypeScript + Zod |
| Claude SDK invocations | LLM called only for reasoning tasks (code review, bug scan) | Anthropic Claude SDK |

Claude runs: `pnpm nx run dev-workflow:complete-task`. It gets back a structured JSON result. It does not orchestrate — it delegates.

**Step anatomy:**
```typescript
type Step = (context: WorkflowContext) => Promise<StepResult>

type StepResult =
  | { status: 'success'; nextAction: ...; details: ... }
  | { status: 'failure'; reason: ...; details: ... }
```

Every step is composable, typed, and independently testable.

**Zod schemas** enforce strong typing at each stage boundary. A misconfigured step fails loudly at startup, not silently mid-workflow.

**Parallel reviewers.** Multiple review agents (e.g., `code-review`, `bug-scanner`) run in parallel, each reading their instructions from `.claude/agents/<name>.md`. Results are aggregated before the PR step.

**Git hook enforcement.** A git hook prevents the agent from calling `git push` or `gh pr create` directly. Attempts to bypass the workflow are redirected to `/complete-task`. The workflow cannot be short-circuited.

---

### Stage 3 — Hook-driven Dev Workflows (Feb 28)

The third article introduces a DDD-native framing for the workflow engine itself.

**The core analogy:**

| DDD concept | Workflow equivalent |
|---|---|
| Aggregate | The workflow engine (protects its own internal state) |
| Event | A Claude Code hook firing (something happened) |
| Command | An operation on the workflow facade (can be rejected) |
| Invariant | A phase transition rule (e.g., cannot commit before reviewing) |

Claude Code hooks become the event boundary. The workflow engine is a proper aggregate that:
- Receives hook events (PreToolUse, PostToolUse, Notification, etc.)
- Updates internal state only through valid transitions
- Rejects commands that violate phase invariants

**State machine phases:**
```
planning → developing → reviewing → committing
```

Each phase has an `agentInstructions` field pointing to a markdown file that is injected into the agent's context on every state transition. This is how the agent knows what to do next without relying on a polluted main-context conversation history. The instructions are contextually fresh on every hook.

**100% test coverage becomes possible.** Because the workflow is now a proper TypeScript module — an aggregate with typed commands and events — it can be unit tested completely. State transition logic, invariant enforcement, and hook handling are all ordinary code.

**The legacy codebase case.** Tune notes this approach solves a specific problem: applying consistent workflows to codebases where you do not control lint rules, git hooks, or CI configuration. The workflow engine enforces its own gates regardless of the underlying project's tooling.

---

### Stage 4 — Event-sourced Claude Code Workflows (Mar 4)

The fourth article applies event sourcing to the workflow engine itself — a suggestion Tune credits to Yves Reynhout.

**The change:** instead of storing current state and mutating it on transitions, the workflow engine stores only the stream of events. Current state is derived by replaying the event log.

**Implementation:** SQLite, deliberately simple. The point is not the storage technology but what the event log unlocks.

**What event sourcing enables:**

| Capability | Description |
|---|---|
| Session timelines | Full visualization of workflow progression for any past session |
| Rejection and denial counts | Track how often the agent hit guards or tried to bypass phases |
| AI-powered analysis | Feed event logs to an LLM for insights on agent behaviour patterns |
| Journal entries | Contextual documentation attached to specific events |
| Cross-session trend analysis | Compare agent performance across many sessions; identify drift |

This turns the workflow engine from a guardrail into an **observability instrument**. You can understand not just whether a session succeeded, but where time was spent, where the agent struggled, and how behaviour is trending across weeks of work.

---

## The Cost Constraint: When Multi-Agent Systems Earn Their Keep

The Tune series focuses on making agent pipelines reliable. The Anthropic series adds the dimension Tune does not address: **cost**. Multi-agent systems typically consume 3–10× more tokens than a single-agent approach. That is not a reason to avoid them — it is a reason to have a deliberate justification before introducing them.

Three scenarios genuinely justify the overhead:

**Context protection.** When information accumulated by one subtask pollutes the reasoning quality of the next, isolated subagents with separate context windows improve output. The example from the Anthropic series: a support agent retrieving 2,000+ tokens of order history while simultaneously diagnosing a technical problem — the retrieval context degrades the diagnostic reasoning. CritterCab's analogy: an agent holding full Event Modeling session output while simultaneously writing a specific handler. Separating those tasks protects the handler-writing context from irrelevant noise.

**Parallelisation.** Some search spaces are too large for a single agent to cover serially within a useful time budget. Parallel subagents exploring different facets simultaneously — for example, independent review passes for security, coverage, style, and architecture — produce better aggregate coverage than a single agent asked to do all four.

**Specialisation.** An agent with 20+ tools or asked to switch between conflicting behavioural modes (empathetic support versus precise code review) produces inconsistent results. Focused subagents with scoped toolsets and single-purpose system prompts are more reliable. This directly supports the principle from Tune's Stage 2: `code-review` and `bug-scanner` are separate agents, not modes of a single agent.

The heuristic: reach for a single agent first. Move to multi-agent when you can name which of these three scenarios applies. If none applies, the architecture is complexity for its own sake.

---

## A Practitioner Vocabulary for Coordination Patterns

The Tune series describes *how to build* reliable agent workflows. The Anthropic coordination patterns series names *what you are building* when you reach a certain structural shape. Having the names matters: they make design conversations precise and make it possible to recognise when a workflow has evolved past the pattern it started with.

**Generator-Verifier.** A generator produces output; a verifier evaluates it against explicit criteria and either accepts or returns feedback for revision. Use when output quality is critical and evaluation criteria can be made explicit. Key failure mode: the verifier is only as good as its criteria — vague criteria fail silently. Implement maximum iteration limits with a fallback (human escalation, or return best attempt with caveats). This maps to CritterCab's code-review and task-check subagents in Stage 1 of the Tune series.

**Orchestrator-Subagent.** A lead agent plans work, delegates bounded tasks to specialised subagents, and synthesises results. Subagents terminate after one task; their context resets between invocations. Use when decomposition is clear and subtasks have minimal interdependence. The widest-applicability starting pattern — handle most workflows here before escalating to something more complex.

**Agent Teams.** A coordinator spawns persistent workers who claim tasks from a shared queue and retain context across multiple assignments. Use when parallel work benefits from sustained, multi-step context accumulation — a framework migration where each worker handles one service independently, for example. Independence between workers is the critical requirement; shared resources introduce conflict risk.

**Message Bus.** Agents publish and subscribe to event topics; a router delivers matching messages. Workflow emerges from events rather than from orchestration logic. Use when the pipeline is genuinely event-driven and the agent ecosystem is likely to grow. Failure mode: misclassified events produce no visible error — the system fails silently. This pattern requires careful logging and event tracing from the start.

**Shared State.** Agents read and write to a persistent store autonomously, with no central coordinator. Each agent's discoveries become available to others without explicit handoff. Use when agents must share intermediate findings and decentralised coordination is an advantage. Failure mode: reactive loops without explicit termination conditions. Requires a convergence threshold or time limit.

**Evolution guidance.** Start with Orchestrator-Subagent — it handles the widest range with the least overhead. Move to Agent Teams when subagents need accumulated state across invocations. Move to Message Bus when the orchestrator's conditional routing logic has grown complex enough to obscure the workflow. Move to Shared State when agents must inform each other's reasoning without sequential handoffs.

Production systems often compose these: an Orchestrator-Subagent outer loop with Shared-State inner coordination, or a Message Bus routing to Agent Team workers.

---

## Synthesized Patterns

These are the patterns that appear consistently across all sources and warrant direct consideration for CritterCab:

### 1. Separate reasoning from orchestration
Agents reason well. They orchestrate poorly. Any multi-step mechanical sequence (run build, wait, collect results, branch on status) should be code, not a prompt instruction. Agents should receive a single entry point, do their reasoning, and hand back a structured result.

### 2. Single source of truth for conventions
Pick one canonical location for all behavioral conventions. Wire every agent, review tool, and CI check to read from that same location. CritterCab's `docs/skills/` files are the natural candidate — they need only to be referenced by anything doing review, not duplicated into tool-specific configs.

### 3. Model the workflow as an aggregate
A workflow with phases and invariants is a bounded domain. Model it accordingly: typed state, typed commands, typed events, invariant enforcement in the transition logic. This makes the workflow testable, observable, and evolvable without fear.

### 4. Inject context per phase, not per session
Rather than relying on the conversation history to carry forward what the agent should know, inject fresh, phase-appropriate instructions on every state transition. This fights context drift and makes each phase behaviorally coherent even in long sessions.

### 5. Event-source the process for observability
When the workflow is event-sourced, every decision and transition is permanently recorded. This enables retrospective analysis (mapping well to CritterCab's existing retrospective practice), identification of recurring failure patterns, and AI-assisted process improvement.

### 6. Decompose by context required, not by problem type
This principle from the Anthropic series is the most directly applicable to domain-model-aware agent design. When deciding where to draw agent task boundaries, ask what context each task requires — not what category of problem it is. A task that writes a feature handler and a task that writes that handler's tests share context (the narrative, the bounded context's ubiquitous language, the aggregate's interface). Separating them creates constant coordination overhead and telephone-game degradation at the handoff. A task that writes a Telemetry handler and a task that writes a Dispatch handler do not share context — they belong to different bounded contexts and should be separate agents.

The implication: bounded context boundaries are natural agent task partitioning boundaries. An agent working within a single bounded context operates with coherent, non-polluting context. An agent asked to span multiple bounded contexts either needs a very broad context window or should be decomposed into per-context subagents coordinated by an orchestrator.

### 7. Trust, failure modes, and verification quality are first-class design concerns
The Anthropic series introduces two failure modes not named by Tune that warrant explicit treatment:

**The telephone game.** At each handoff between agents, fidelity degrades. A detail accurate in the generator's output becomes slightly distorted in the verifier's feedback, and further distorted in the next iteration. Long chains of sequential agents accumulate this degradation. Minimise handoff depth; favour parallel over serial decomposition when the tasks are independent.

**The early victory problem.** A verification subagent asked "did the tests pass?" will confirm passing tests without running the full suite. A verification subagent asked "does this implementation satisfy all acceptance criteria?" will affirm satisfaction after minimal inspection. Verifier prompts must specify the complete scope of verification explicitly. "Run the complete test suite" is a requirement, not a courtesy. For CritterCab's task-check subagent, this means acceptance criteria must enumerate the verification steps, not just the expected outcomes.

---

## Application to CritterCab's Session Workflow

CritterCab's existing loop — narrative → prompt → execute → retrospective — already has the right phases. The Nick Tune series suggests how to make those phases enforceable and observable.

### Phase mapping

| CritterCab phase | Tune equivalent | Enforcement mechanism |
|---|---|---|
| Narrative authoring | Planning | Gate: narrative doc must exist before prompt doc is created |
| Prompt authoring | Task creation | Gate: prompt must reference at least one narrative + one skill |
| Execute (implementation) | Developing | Gate: no commit before local verify gate passes |
| Code review | Reviewing | Gate: review subagent must complete before PR is submitted |
| Retrospective | Committing + reflection | Gate: retro doc must exist before session is closed |

### Skills as the single source of truth

CritterCab's `docs/skills/` files are the conventions layer. Every review agent, every lint rule, and (when adopted) any external CI review tool should reference these files rather than duplicating their content. The skills are not documentation — they are the specification that reviewers enforce.

### Wolverine and Marten as natural workflow hooks

Once implementation begins, Wolverine provides its own hook points (middleware, message bus interception) that could be used to enforce domain invariants at runtime in a manner analogous to Tune's Claude Code hooks at the dev-process level. The same aggregate pattern applies: a Wolverine saga is already an aggregate, and its handler invocations are already event-driven.

### Observability of design decisions

CritterCab currently captures design decisions in ADRs and retrospectives. Applying event sourcing to the session workflow would allow cross-session analysis: which narratives led to the most design pivots? Which bounded contexts generated the most retrospective notes? This kind of analysis becomes trivial if session transitions are stored as a queryable event log.

### The domain architecture mirrors agent coordination patterns

CritterCab's technical choices map onto two of the five coordination patterns in ways worth naming explicitly.

**Azure Service Bus as a Message Bus for agents.** CritterCab's business-event backbone (ADR-005) is an Azure Service Bus topic subscription model: producers publish, consumers subscribe, the broker routes by topic. A Message Bus coordination pattern for agents uses the same structure. If CritterCab adopts multi-agent coordination for implementation or analysis tasks, the same ASB infrastructure could serve as the agent coordination medium — domain events and agent coordination events travelling on the same bus, distinguished by topic. This is not a recommendation to do it, but a recognition that the plumbing is already committed.

**Marten's event store as Shared State for agents.** The Shared State pattern requires a persistent store that multiple agents can read and write without central coordination. Marten's event stream is exactly this: an append-only, queryable log accessible to any consumer with a projection. An agent that analyses Telemetry event patterns and an agent that analyses Dispatch matching outcomes could share a Marten projection as their coordination surface, each appending observations that the other can consume. Again — not a current action item, but a natural affordance of the chosen stack.

The broader observation: a system designed around event-driven bounded contexts is already structured the way sound multi-agent coordination is structured. The domain boundaries are agent task boundaries. The event streams are agent communication channels. The Wolverine bus is an agent dispatch mechanism. If and when CritterCab moves beyond single-agent implementation sessions toward multi-agent workflows, the architecture will not need to be retrofitted to support it.

---

## Actionable Recommendations

These are concrete steps worth considering for CritterCab, ordered by implementation proximity:

1. **Reference skills from review tooling.** When a CI code review tool is adopted (CodeRabbit or equivalent), configure it to read from `docs/skills/` rather than maintaining separate rule sets.

2. **Enforce the narrative → prompt dependency structurally.** A prompt document that does not reference a narrative is an incomplete artifact. Consider a simple validation (naming convention check, frontmatter field, or CI gate) that surfaces this early.

3. **Adopt the 10-section task format for prompts.** CritterCab's prompt template is close. Formalizing the sections as required (especially acceptance criteria, edge cases, and verification commands) closes the gap between what was intended and what an agent can verify.

4. **Model the session workflow as a state machine.** Even informally, naming the states (Planning, Implementing, Reviewing, Closed) and defining what can happen in each state makes the workflow enforceable and communicable.

5. **Consider a session event log.** Even a simple append-only JSONL file per session capturing phase transitions and key decisions gives you a retrospective foundation that is both human-readable and machine-queryable.

6. **Start with Orchestrator-Subagent before reaching for more complex coordination.** When CritterCab moves toward multi-agent implementation sessions, the orchestrator-subagent pattern handles the widest range of tasks with the least overhead. Only evolve toward Agent Teams, Message Bus, or Shared State when you can name a specific constraint the simpler pattern is failing to handle.

7. **Write verifier acceptance criteria as explicit verification steps, not outcomes.** Any subagent asked to verify correctness (task-check, code-review) must have its criteria expressed as a checklist of actions to perform, not just conditions to confirm. "Run the complete test suite and confirm all pass" rather than "confirm the implementation is correct." This is the direct mitigation for the early victory problem.
