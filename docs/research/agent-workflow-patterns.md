# Agent Workflow Patterns — Notes for CritterCab

> **Source series:** Four blog posts by Nick Tune, January–March 2026.
> - [Coding Agent Development Workflows](https://nick-tune.me/blog/2026-01-07-coding-agent-development-workflows/) — Jan 7
> - [Dev Workflows as Code](https://nick-tune.me/blog/2026-01-17-dev-workflows-as-code/) — Jan 17
> - [Hook-driven Dev Workflows with Claude Code](https://nick-tune.me/blog/2026-02-28-hook-driven-dev-workflows-with-claude-code/) — Feb 28
> - [Event-sourced Claude Code Workflows](https://nick-tune.me/blog/2026-03-04-event-sourced-claude-code-workflows/) — Mar 4

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

## Synthesized Patterns

These are the patterns that appear consistently across all four articles and warrant direct consideration for CritterCab:

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

---

## Actionable Recommendations

These are concrete steps worth considering for CritterCab, ordered by implementation proximity:

1. **Reference skills from review tooling.** When a CI code review tool is adopted (CodeRabbit or equivalent), configure it to read from `docs/skills/` rather than maintaining separate rule sets.

2. **Enforce the narrative → prompt dependency structurally.** A prompt document that does not reference a narrative is an incomplete artifact. Consider a simple validation (naming convention check, frontmatter field, or CI gate) that surfaces this early.

3. **Adopt the 10-section task format for prompts.** CritterCab's prompt template is close. Formalizing the sections as required (especially acceptance criteria, edge cases, and verification commands) closes the gap between what was intended and what an agent can verify.

4. **Model the session workflow as a state machine.** Even informally, naming the states (Planning, Implementing, Reviewing, Closed) and defining what can happen in each state makes the workflow enforceable and communicable.

5. **Consider a session event log.** Even a simple append-only JSONL file per session capturing phase transitions and key decisions gives you a retrospective foundation that is both human-readable and machine-queryable.
