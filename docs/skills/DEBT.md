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

### `wolverine-handlers` (or a new `wolverine-marten-automation` skill)

Both rows encode the same automation idiom and should be drained together; the tidy session decides whether they extend [`wolverine-handlers`](./wolverine-handlers/SKILL.md) or warrant a new `wolverine-marten-automation` skill.

- **Marker-interface union return type.** No skill encodes the pattern where an automation returns a marker interface (`ICandidateSelectionOutcome`, `IFareQuoteOutcome`) implemented by 2+ concrete events. Wolverine's `DetermineEventCaptureHandling` treats any non-`IEnumerable<object>` return as a single-event append of the runtime type, so the interface is purely compile-time documentation of a decision's possible outcomes. Pattern has appeared **3×** (the encoding threshold). Retro source: [implementations/005](../retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md) (first flagged in 004).
- **Event-triggered automation handler shape.** No skill encodes the shape `Handle(DomainEvent @event, [WriteAggregate(nameof(...))] Aggregate, ...)` — a static automation reacting to a domain event (via `UseFastEventForwarding`) that loads the aggregate by the named stream-id property, which works even when the trigger is a non-first stream event. Used by `FareQuoteAutomation` and `CandidateSelectionAutomation`. Retro source: [implementations/005](../retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md).

---

## Recently drained

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
