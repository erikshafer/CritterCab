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

---

## Open debt

*(none — the initial 7-row backlog from PR #4 was drained on 2026-05-08; see [Recently drained](#recently-drained).)*

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
