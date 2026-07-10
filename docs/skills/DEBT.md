# Skill-File Debt

Rolling list of skill-file gaps surfaced during sessions but not yet fixed. Drained by dedicated `tidy: skills` PRs; rows are **removed** (not crossed out) when the corresponding skill is updated. The retros remain the durable record of how each gap was found.

This file is the working ledger between retros that surface gaps and the tidy sessions that fix them. Per [`docs/prompts/README.md`](../prompts/README.md#session-and-pr-cadence)'s **Session and PR cadence**, gaps are fixed in-session only when they block the session-runner; gaps merely *surfaced* by a session land here and are drained on a schedule.

---

## Conventions

- **Each row names the skill, the gap, and the retro source.** The retro is the durable evidence of how the gap was found, in case the fix needs to be re-grounded later.
- **Group rows by skill file.** A tidy session can then plan one PR per affected skill rather than touching everything at once.
- **Add rows in the same PR that adds the surfacing retro.** Authoring a retro that names skill-file gaps without registering them here is a workflow gap — the debt evaporates between sessions otherwise.
- **Remove rows when fixed.** This file is not a changelog. Commits and retros already record what changed and why.
- **A row's existence is not a commitment to fix it next.** Tidy sessions choose what to drain based on cluster, blast radius, and which upcoming sessions the fix would unblock.
- **Tidy sessions verify each row against current state before fixing** (source-of-truth precedence). Precedence: working code → retro evidence → external docs. The skill body itself is what's being corrected and cannot be its own reference. The retro is evidence the gap once existed, not proof it still does. (Lifted from the first skill-tidy retrospective: one of four `marten-projections` rows turned out to be already-superseded by the time the tidy ran.)
- **No opportunistic edits to other files during a tidy.** A tidy session's scope is the skills listed for fixing in this file plus the prompt + retro files actively being authored. Other files require their own session. See [`docs/prompts/README.md` § Scope: no opportunistic edits to other files](../prompts/README.md#scope-no-opportunistic-edits-to-other-files) for the general rule and rationale.

---

## Open debt

### config-as-events bootstrap-seed pattern (new skill or `marten-wolverine-aggregates` extension)

- **Gap:** No skill codifies the config-as-events seed — the Marten `IInitialData` idempotent guard (`FetchStreamStateAsync` → `StartStream<T>` only if empty) plus the `operatorId = "system-bootstrap"` / `reason = "Initial deployment defaults"` payload, and the full-replacement singleton-stream shape. [ADR-011](../decisions/011-configuration-as-events-bootstrap.md) (§ Consequences, final ¶) explicitly deferred codifying this "until the first migration is written during implementation." That reference impl now exists: `src/CritterCab.Telemetry/TelemetryPolicy/{TelemetryPolicyBootstrap,TelemetryPolicyStream,TelemetryPolicy,ConfigureTelemetryPolicy}.cs`.
- **Two design questions the reference impl raised are now RESOLVED** by the [ADR-011 Amendment (2026-07-10)](../decisions/011-configuration-as-events-bootstrap.md#amendment--2026-07-10-marten-realization-via-iinitialdata) — the skill can codify their answers rather than re-litigate them:
  1. **ADR-011 Option A vs B for the Marten idiom** — resolved: `IInitialData` (registered via `.InitializeWith<T>()`) is the canonical Marten realization of Option A; it seeds at the deploy-time apply step (`resources setup`) and idempotently at host start as a self-healing safety net; the multi-instance race is mitigated by the idempotent guard + full-replacement (a double-seed converges) and avoided by the deploy-time step / single-instance MVP.
  2. **Singleton write-path concurrency** — resolved: config-as-events singletons use **last-writer-wins** (no optimistic concurrency; full-replacement has no invariant to defend), and a manual `session.Events.Append(<well-known-constant-id>, event)` is the accepted reconfigure shape (`[WriteAggregate]` does not apply — no id field on the command).
- **Remaining open debt (the skill only):** ground the seed pattern from the reference impl per the amendment's answers.
- **Retro source:** [`retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md`](../retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md).

### Wolverine.HTTP FluentValidation boundary wiring (`wolverine-http-handlers` addendum)

- **Gap:** No skill documents the **two-call** wiring that HTTP boundary validation actually requires. `csharp-coding-standards` § FluentValidation shows the nested `AbstractValidator<T>` shape, but the boundary needs BOTH `opts.UseFluentValidation()` in `UseWolverine` (the `WolverineFx.FluentValidation` assembly-scan that *registers* `IValidator<>` into DI) **and** `opts.UseFluentValidationProblemDetailMiddleware()` in `MapWolverineEndpoints` (the `WolverineFx.Http.FluentValidation` middleware that *resolves* them into a 400 ProblemDetails). Wiring only the middleware silently passes invalid input through as 200 — a footgun CI caught on slice-1's first run (`src/CritterCab.Telemetry/Program.cs` is the repo's first FluentValidation instance and the reference wiring). A tidy session should add this to `wolverine-http-handlers` (both packages, both calls, the DI-resolution dependency between them).
- **Retro source:** [`retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md`](../retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md) (§ "CI caught a bug local tooling structurally could not").

---

## Recently drained

### 2026-07-02 — wolverine-marten-automation tidy

2 rows drained via [`prompts/skills-tidy-wolverine-marten-automation.md`](../prompts/skills-tidy-wolverine-marten-automation.md), authoring a new skill rather than extending an existing one:

- **Marker-interface union return type** and **event-triggered automation handler shape** (both grouped under one heading in the prior entry). Fixed by authoring [`docs/skills/wolverine-marten-automation/SKILL.md`](./wolverine-marten-automation/SKILL.md) — a new skill rather than a `wolverine-handlers` bolt-on, per that skill's own trigger-agnostic charter. Grounded in both real examples (`FareQuoteAutomation`, `CandidateSelectionAutomation`) plus a second registration prerequisite (`CustomizeHandlerDiscovery(...WithNameSuffix("Automation"))`) the original retro didn't name. Retro at [`retrospectives/skills-tidy-wolverine-marten-automation.md`](../retrospectives/skills-tidy-wolverine-marten-automation.md).

### 2026-05-08 — initial tidy

7 rows drained across 3 skills via [`prompts/skills-tidy-marten-and-bootstrap.md`](../prompts/skills-tidy-marten-and-bootstrap.md):

- **`marten-projections` (4 rows):** `IEvent<T>` namespace; `SingleStreamProjection`/`MultiStreamProjection` namespace asymmetry; `ProjectionLifecycle` namespace; `SingleStreamProjection<T>` → `SingleStreamProjection<TDoc, TId>` type-parameter shape. Fixed by adding a "Namespaces" cheat-sheet table near the top of the skill. **Note:** the type-parameter half of one row was already correct in the skill body (likely fixed in an earlier pass, not re-verified at DEBT-row registration); only the namespace half required edits. Captured as a methodology learning in the retro.
- **`marten-wolverine-aggregates` (1 row):** `IEvent<T>` namespace. Fixed by adding a small "Namespaces" cheat-sheet listing `IEvent<T>`'s `JasperFx.Events` location.
- **`service-bootstrap` (2 rows):** Missing `AddWolverineHttp()` prerequisite for `MapWolverineEndpoints()`; `TimeProvider` DI registration. Fixed by adding a new "Service that exposes Wolverine.HTTP endpoints" subsection in Per-Service Configuration Variation, plus two new Common Pitfalls bullets. Prose-pass also corrected one residual "Oakton CLI surface" reference to "JasperFx CLI surface" (decision-to-flag #3 from the prompt).

Older entries drop off; the retros and commits remain authoritative.

---

## Out of scope for this file

- **Author-time conventions** (style, structure, voice). Those belong in [`docs/skills/README.md`](./README.md) and [`_template/SKILL.md`](./_template/SKILL.md).
- **Cross-skill consistency tasks** (e.g., reconciling overlapping content between two skills). Wider scope than a debt row; warrants its own prompt rather than a one-line entry here.
- **Lean-out work to avoid overlap with JasperFx `ai-skills`.** A deliberate authoring decision, not a reactive debt item; track it in the relevant skill's authoring history or a dedicated prompt.
- **Phase 6 placeholder cleanup** (the 14 skills tagged during phase 5 reconciliation). That work has its own scope and lives in the skills-foundation phase plan, not here.

---

## Document history

- **2026-05-08.** Initial authoring. Seven rows from the post-D→B→C session — five `marten-*` Marten 8.x / JasperFx namespace extractions plus two `service-bootstrap` registration prerequisites. Three other gaps from the same session (`RunOaktonCommandsAsync` → `RunJasperFxCommands`, `protobuf-contracts` directory layout, `service-bootstrap`/`aspire` connection-string contradiction) were fixed in-flight under the session-runner-blocking exception and do not appear here.
- **2026-05-08 (later same day).** Initial 7-row backlog drained via the first skill-tidy session. `Open debt` reset to empty. Retro at [`docs/retrospectives/skills-tidy-marten-and-bootstrap.md`](../retrospectives/skills-tidy-marten-and-bootstrap.md).
- **2026-06-25.** Registered two at-threshold rows surfaced by retro 005 (slice 5.3) and carried in the 2026-06-16 post-slice-5.3 handoff: the marker-interface union return type and the event-triggered automation handler shape, both grouped under `wolverine-handlers` (or a possible new `wolverine-marten-automation` skill). Registering, not fixing — the fix is a future `tidy: skills` session. The **bundling-rule encoding** gap (also flagged past-threshold in retro 005 and the handoff) was deliberately *not* registered: neither source names a target skill, and this file's convention requires a row to name the skill. It stays for a session that can ground the target.
- **2026-07-02.** Drained both 2026-06-25 rows via a new `docs/skills/wolverine-marten-automation/SKILL.md` skill (critter-skill-auditor Phase 1 discovery ruled out both `wolverine-handlers` and `marten-wolverine-aggregates` as bolt-on homes). `Open debt` reset to empty. Item 1 of the [post-W006 handoff](../planning/2026-07-02-post-w006-next-steps-handoff.md)'s ordered table. Retro at [`docs/retrospectives/skills-tidy-wolverine-marten-automation.md`](../retrospectives/skills-tidy-wolverine-marten-automation.md).
