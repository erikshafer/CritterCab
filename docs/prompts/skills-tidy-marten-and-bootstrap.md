# Prompt — Skill Tidy: Marten 8.x / JasperFx Namespaces and Service-Bootstrap Registration Prerequisites

| Field | Value |
|---|---|
| **Status** | Pending (sketched 2026-05-08; awaiting review before execution) |
| **Authored** | 2026-05-08 |
| **Target artifacts** | `docs/skills/marten-projections/SKILL.md`, `docs/skills/marten-wolverine-aggregates/SKILL.md`, `docs/skills/service-bootstrap/SKILL.md`, `docs/skills/DEBT.md` (rows drained), `docs/retrospectives/skills-tidy-marten-and-bootstrap.md` (new) |
| **Source-of-truth dependencies** | [`docs/skills/DEBT.md`](../skills/DEBT.md) (rows to drain); [`docs/retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md`](../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md) (gap evidence); the working code in `src/CritterCab.Dispatch/` (canonical reference for what the actual API looks like) |
| **Workflow position** | First skill-tidy session. First exercise of the **dedicated `tidy: skills` PR** convention from [`docs/prompts/README.md`](./README.md#session-and-pr-cadence). Drains all 7 currently-open DEBT.md rows. |

---

## Framing — why this session exists

The post-D→B→C session (PR #4, slice 5.1 specifically) surfaced 7 skill-file gaps that did not block the session-runner — the runner adapted to the actual Marten 8.x / Wolverine 5.38 API surface inline. All 7 gaps were registered in [`docs/skills/DEBT.md`](../skills/DEBT.md) instead of being absorbed into the implementation PR.

Per the **Session and PR cadence** rule in [`docs/prompts/README.md`](./README.md#session-and-pr-cadence), surfaced-but-not-blocking gaps drain via dedicated `tidy: skills` PRs. This session drains the entire current backlog in one PR — small enough to be reviewable, related-enough by root cause to be coherent (Marten 8.x / JasperFx namespace extractions across two skills, plus Wolverine HTTP/DI registration prerequisites in one).

This is also the **first interleave** under the design-return cadence rule: after the D→B→C run of three implementation-adjacent sessions, the next PR is intentionally not another implementation slice. After this tidy, the working lean is to return to the design phase with the Trips workshop (Workshop §12.8 follow-up A).

---

## Goal

Update three skill files to reflect the actual Marten 8.x / Wolverine 5.38 API surface as exercised by the working Dispatch service. Remove drained rows from `DEBT.md`. Author a retrospective. No opportunistic edits.

---

## Source-of-truth precedence

For each gap, the source of truth is **the working code in `src/CritterCab.Dispatch/`**, then the retro evidence, then external docs. The skill body is what's being corrected; it cannot also be its own reference.

When the working code and the retro disagree, the working code wins (the retro was a snapshot at session close; the code is the result of the runner's actual adaptations). When the working code and external docs disagree, **stop and surface the conflict** rather than guessing.

---

## Orientation files (read in order)

1. **[`docs/skills/DEBT.md`](../skills/DEBT.md)** — the 7 rows to drain, grouped by skill.
2. **[`docs/retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md`](../retrospectives/implementations/002-dispatch-slice-5-1-ride-requested.md)** — original gap evidence, including which call-sites in Dispatch revealed each issue.
3. **[`docs/prompts/README.md`](./README.md#session-and-pr-cadence)** — Session and PR cadence rules; this is the first tidy under them.
4. **`src/CritterCab.Dispatch/RideRequesting/RideRequest.cs`**, **`ActiveRideRequest.cs`**, **`RequestTimeline.cs`** — working examples of the corrected Marten 8.x API (aggregate, multi-stream projection, single-stream projection, `IEvent<T>`, `ProjectionLifecycle`).
5. **`src/CritterCab.Dispatch/Program.cs`** — working example of the corrected service-bootstrap composition (`AddWolverineHttp()` registration, `TimeProvider` registration, `RunJasperFxCommands`).
6. The current state of the three target skill files (read each immediately before editing it).

---

## Working pattern

- **One session, one PR.** PR title: `tidy: skills — Marten 8.x / JasperFx namespaces and service-bootstrap registrations`. All 7 fixes plus the retro plus the DEBT.md updates ship together.
- **Per-skill batching inside the PR.** Group the diff so each affected skill is reviewable as a coherent unit (one commit per skill is reasonable; or one combined commit if commits are not load-bearing). Mention which DEBT rows each commit drains in the commit body.
- **Search-and-verify per gap.** Namespace gaps are systematic — Marten 8.x's JasperFx extraction may recur in places the retro didn't enumerate. For each fix, grep the affected skill (and only the affected skill) for *every* instance of the old pattern, not just the first one. Do not grep across other skills opportunistically; that's out of scope.
- **Verify each fix against the working code before edit.** The code is the reference; the skill is what's being corrected.
- **No opportunistic edits.** If a non-DEBT issue surfaces during the tidy, capture it as a **new** DEBT.md row for a future tidy session. Do not expand the present session's scope.
- **DEBT.md rows are removed in the same PR.** Move drained rows to `## Recently drained` under a `### 2026-05-08 — initial tidy` heading; update the document history line.
- **Retrospective committed in the same PR**, at `docs/retrospectives/skills-tidy-marten-and-bootstrap.md`. Per [`docs/retrospectives/README.md`](../retrospectives/README.md), this is a root-level retro because the session spans multiple skills.

---

## Deliverable plan

1. **`docs/skills/marten-projections/SKILL.md`** — fix 4 gaps:
   - `IEvent<T>` namespace → `JasperFx.Events`.
   - `SingleStreamProjection<T>` → `SingleStreamProjection<TDoc, TId>` (two type parameters).
   - `SingleStreamProjection` lives in `Marten.Events.Aggregation` (not `Marten.Events.Projections`); `MultiStreamProjection` lives in `Marten.Events.Projections` (not `Marten.Events.Aggregation`).
   - `ProjectionLifecycle` namespace → `JasperFx.Events.Projections`.
2. **`docs/skills/marten-wolverine-aggregates/SKILL.md`** — fix 1 gap:
   - `IEvent<T>` namespace → `JasperFx.Events`.
3. **`docs/skills/service-bootstrap/SKILL.md`** — fix 2 gaps:
   - Add the `AddWolverineHttp()` prerequisite for `MapWolverineEndpoints()` to the composition-root pattern.
   - Add the `TimeProvider` DI registration note (`builder.Services.AddSingleton(TimeProvider.System)`) for handlers that inject `TimeProvider`.
4. **`docs/skills/DEBT.md`** — remove the 7 drained rows; add `### 2026-05-08 — initial tidy` heading under `## Recently drained` with one-line summaries; update the document history.
5. **`docs/retrospectives/skills-tidy-marten-and-bootstrap.md`** — new retrospective per the format in [`docs/retrospectives/README.md`](../retrospectives/README.md). Sections: framing, outcome summary, what worked, what was harder than expected, methodology refinements (especially: anything learned about how this first tidy session shaped the convention), outstanding items / next-session inputs.

---

## Out of scope

- **Lean-out work to reduce overlap with JasperFx `ai-skills`.** Deliberate authoring decision per `DEBT.md`'s "Out of scope" carve-out. Capture in a separate prompt if pursued.
- **Phase 6 placeholder cleanup** (the 14 skills tagged during Phase 5 reconciliation). Has its own scope outside this file.
- **Cross-skill consistency reconciliation** between `marten-projections` and `marten-wolverine-aggregates` (e.g., shared examples, parallel structure). Would warrant its own prompt.
- **Style, clarity, or restructuring edits** to the affected skills beyond what each DEBT row strictly requires. If a clarity issue is noticed, file it as a new DEBT row for a future tidy.
- **Skill-template updates** (`_template/SKILL.md`). Not affected by these gaps.
- **Application code changes** in `src/CritterCab.Dispatch/`. The code is already correct — that is precisely why it serves as the reference for this tidy.
- **Adding new examples** to any skill. Existing examples are corrected; new examples are not added in this session.

---

## Decisions to flag during the session

1. **Whether Marten 8.x's `MultiStreamProjection` namespace is genuinely `Marten.Events.Projections`** (per the retro) or whether the JasperFx extraction has moved it further. The Dispatch code is the tiebreaker; if the code uses something different from what the retro says, follow the code and note the discrepancy in the retro.
2. **Whether the `service-bootstrap` skill's existing composition example needs minimal restructuring to make `AddWolverineHttp()` and `TimeProvider` reads naturally**, or whether they slot in without disturbing the example. Lean: minimal restructuring is acceptable when it serves clarity of the fix; full restructuring is out of scope.
3. **Whether to also note the `RunJasperFxCommands` rename in `service-bootstrap`** even though that gap was fixed in-flight during the D→B→C session. If the skill's prose still references `RunOaktonCommandsAsync` in any narrative passage (not just the code example), correct it as part of this fix — the runtime correction without the prose alignment is a half-fix.

---

## What this prompt's retrospective should specifically capture

Beyond the standard retro shape, this session's retro is the **methodology specimen** for future tidy sessions. Capture explicitly:

- Whether the "no opportunistic edits" rule held, and at what cost (did anything that should have been fixed get punted to a new DEBT row?).
- Whether one PR for all 7 fixes was the right scope, or whether splitting per-skill would have been better.
- Whether the source-of-truth precedence (working code → retro → external docs) was sufficient or surfaced gaps.
- Which conventions, if any, should be lifted from this session's experience into a permanent rule (e.g., in `DEBT.md`'s conventions section, or in `prompts/README.md`'s cadence section).

These observations feed the methodology log, not just the retro.
