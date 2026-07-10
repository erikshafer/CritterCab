# Retrospective — Telemetry Narrative-Layer Decision

- **Triggering intent record (prompt-equivalent):** `docs/planning/2026-07-10-telemetry-narrative-decision-handoff.md` — a disposable planning handoff (left untracked) that scoped this single decision. No separate prompt document was authored; the handoff served as the prompt-equivalent, per the W005/W006 precedent (a planning/grill note standing in for a prompt). Guardrail #3 of the handoff explicitly permitted retro-direct for a decision this small.
- **Status:** Complete (2026-07-10).
- **Output artifacts:** [`docs/narratives/README.md`](../narratives/README.md) — new "When the narrative layer does not apply" section (criterion + empirical test + "Skipped narratives (recorded)" table), an Index note, and a v0.3 Document-history entry. This retro.
- **Outcome:** Telemetry's narrative step is **explicitly skipped** — the narrative layer does not apply — recorded durably in the narratives layer's own operational manual.

## Framing

Per the session workflow ([CLAUDE.md](../../CLAUDE.md) §Session Workflow, step 4), a narrative is normally authored between an Event Modeling workshop and its implementation prompt(s). Workshop 006 (Telemetry) is locked, its `v1` protobuf contracts shipped (PR #39), and the first transport build is queued next. This session resolved the one open pre-implementation question: does a thin driver-device narrative apply to Telemetry, or is the narrative step skipped with recorded rationale? The workflow forbids silently dropping the step, so the decision had to be made and recorded — as its own PR — before implementation begins.

## The decision and how it was reached

**Decision: the narrative layer does not apply to Telemetry.** No narrative is authored.

The handoff mandated an *empirical* check rather than a presumption: re-read W006 §6.2–§6.5 (and the Ubiquitous Language) for any driver-app-perceivable detail — a "location sharing is on" indicator, a permission prompt, a status surface, or an action the driver takes or witnesses. The scan found none:

- **§6.2 (GPS ingest):** the trigger is "a mobile client opens a windowed `ReportLocations` stream" — the device/software acts, not the driver; `driverId` comes from the authenticated principal, never the payload; all processing is server-side and in-flight.
- **§6.3 / §6.4 / §6.5:** Kafka publish, document upsert + eviction sweep, and the Dispatch-side consumer view — all inter-service plumbing or server-side housekeeping.
- **§4 (UL):** *driver session* is defined as "a **logical** concept, decoupled from any physical stream"; *active* means *pinging*, not anything the driver sees.

Applying the narratives README's own rubric, the load-bearing gate is **Voice and perspective**: the omniscient narrator dramatizes *what the protagonist actually perceives*. Telemetry's protagonist perceives nothing across the whole BC — Context, Interaction, and Response all collapse to empty. Forcing a narrative would require **inventing** a UX surface that neither W006 nor ADR-018 names, violating the "do not carry: UX/UI design" guardrail and the two-layer-fidelity "no confabulation" discipline. This is the same reasoning W006 used one layer up when it took the EM-direct path and skipped Domain Storytelling (machine-to-machine; no human language boundary).

The rationale was recorded in `docs/narratives/README.md` (Option A of the two the handoff floated) rather than folded into the not-yet-written first implementation prompt (Option B). Option A lands the decision durably in the most stable artifact in the chain, is discoverable exactly where a future reader would look ("where's the Telemetry narrative?"), generalizes into a reusable criterion for future headless BCs, and lets this session ship self-contained. Option B would have split "decide" from "record" across two PRs and buried a durable cross-BC decision in a transient artifact. User signed off on Option A.

## What worked

- **The empirical-test discipline the handoff insisted on.** Reading the workshop for a concrete perceivable surface — rather than reasoning abstractly from "Telemetry is machine-to-machine" — made the call dispositive and defensible, and produced the exact citations the recorded rationale needed (§6.2 server-side processing; §4 "logical concept").
- **The README already contained the deciding rubric.** The Voice-and-perspective rule and the UX/UI-design guardrail settled the question without inventing new criteria; the new section layers *on top of* the existing "warranted / not warranted" split rather than competing with it (variations-of-a-journey vs. no-journey-at-all).
- **A generalized "Skipped narratives (recorded)" table** turns a one-off decision into a reusable convention with a home for the next headless BC, and mirrors the workshop-layer EM-direct precedent for structural consistency across layers.

## What was harder than expected

- **Deciding the session's own paperwork shape.** With the deliverable being a single README amendment, the questions "does this need a prompt?" and "where does the retro live?" carried more deliberation than the design decision itself. Resolved by precedent: planning-note-as-prompt-equivalent (W005/W006) → no prompt; README-convention amendment → root-level retro (as with the spec-delta-closure-loop encoding session).
- **Handoff-doc lifecycle.** The 2026-07-10 and 2026-07-04 handoffs are untracked working notes ("disposable by design"). Rather than commit spent scaffolding, they were left untracked; the durable record lives in the README + this retro. The 2026-07-04 handoff remains the live orientation for the next session (its item 2).

## Methodology refinements that emerged

- **"When the narrative layer does not apply" is now a named, tested convention.** Future headless / machine-to-machine BCs run the same empirical test and record the skip in the same table — the narrative step can be discharged without silently dropping it.
- **Layer symmetry made explicit.** The narrative-skip-for-headless-BC decision is the same shape as the workshop-layer EM-direct (Domain-Storytelling-skip) decision. Both hinge on "no perceiving protagonist / no human language boundary." Naming the parallel in the README keeps the two design layers reasoning consistently.

## Outstanding items / next-session inputs

- **The first real transport build** (item 2 of the 2026-07-04 post-PR-39 handoff, `docs/planning/2026-07-04-post-pr39-telemetry-next-steps-handoff.md`, the live orientation note) is now unblocked at the design layer and is the next session (its own PR, or skeleton + first slice sharing one per the named exception). Carry forward its **load-bearing forward-constraint**: WolverineFx.Grpc had no client-streaming auto-codegen adapter on the verified 5.x line; `ReportLocations` must be hand-wired against `IMessageBus` or re-verified against 6.8 first. Do not "upgrade" R5's client-streaming to bidirectional to dodge the gap.
- **The 2026-07-10 handoff is fully discharged** and may be deleted at will (disposable by design).

## Spec delta — landed?

**Landed as planned.** This session amends no narrative or workshop (none applies — that *is* the decision), so there is no narrative/workshop `## Document History` amendment. The spec-shaped delta is the convention itself: `docs/narratives/README.md` gains the "When the narrative layer does not apply" section, the Index note, and the v0.3 Document-history entry recording the Telemetry skip. The README's own Document history is the closure-loop record for this session.
