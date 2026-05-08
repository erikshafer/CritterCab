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

### `marten-projections`

| Gap | Retro source |
|---|---|
| `IEvent<T>` namespace is `JasperFx.Events`, not `Marten.Events`. Marten 8.x extracted event interfaces to the `JasperFx.Events` package. | [`retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md`](../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md) |
| `SingleStreamProjection<T>` shape is actually `SingleStreamProjection<TDoc, TId>` (two type parameters). Skill shows a single type parameter. | [`retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md`](../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md) |
| `SingleStreamProjection` lives in `Marten.Events.Aggregation`, not `Marten.Events.Projections`. `MultiStreamProjection` lives in `Marten.Events.Projections`, not `Marten.Events.Aggregation`. The two namespaces are swapped from what the skill shows. | [`retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md`](../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md) |
| `ProjectionLifecycle` namespace is `JasperFx.Events.Projections`, not `Marten.Events.Projections`. Same JasperFx extraction as `IEvent<T>`. | [`retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md`](../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md) |

### `marten-wolverine-aggregates`

| Gap | Retro source |
|---|---|
| `IEvent<T>` namespace is `JasperFx.Events`, not `Marten.Events`. Same Marten 8.x / JasperFx extraction. | [`retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md`](../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md) |

### `service-bootstrap`

| Gap | Retro source |
|---|---|
| Missing `AddWolverineHttp()` prerequisite for `MapWolverineEndpoints()`. Without it, `MapWolverineEndpoints` throws at startup. | [`retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md`](../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md) |
| `TimeProvider` must be registered in DI (`builder.Services.AddSingleton(TimeProvider.System)`) for handlers that inject it. Wolverine HTTP handlers fail at runtime otherwise. | [`retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md`](../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md) |

---

## Recently drained

*(none yet — first tidy session pending.)*

When a tidy session lands, briefly list the rows it closed under a session-dated heading here (last 1–2 sessions only) so reviewers can sanity-check the diff against the retro evidence. Older entries drop off; the retros and commits remain authoritative.

---

## Out of scope for this file

- **Author-time conventions** (style, structure, voice). Those belong in [`docs/skills/README.md`](./README.md) and [`_template/SKILL.md`](./_template/SKILL.md).
- **Cross-skill consistency tasks** (e.g., reconciling overlapping content between two skills). Wider scope than a debt row; warrants its own prompt rather than a one-line entry here.
- **Lean-out work to avoid overlap with JasperFx `ai-skills`.** A deliberate authoring decision, not a reactive debt item; track it in the relevant skill's authoring history or a dedicated prompt.
- **Phase 6 placeholder cleanup** (the 14 skills tagged during phase 5 reconciliation). That work has its own scope and lives in the skills-foundation phase plan, not here.

---

## Document history

- **2026-05-08.** Initial authoring. Five entries from the post-D→B→C session — four `marten-*` Marten 8.x / JasperFx namespace extractions plus two `service-bootstrap` registration prerequisites. Three other gaps from the same session (`RunOaktonCommandsAsync` → `RunJasperFxCommands`, `protobuf-contracts` directory layout, `service-bootstrap`/`aspire` connection-string contradiction) were fixed in-flight under the session-runner-blocking exception and do not appear here.
