# Prompt — Skill Tidy: Wolverine + Marten Event-Triggered Automation Handler Shape

| Field | Value |
|---|---|
| **Status** | Pending (authored 2026-07-02) |
| **Authored** | 2026-07-02 |
| **Target artifacts** | `docs/skills/wolverine-marten-automation/SKILL.md` (new), `docs/skills/README.md` (cluster/tag/hub/graph updates), `docs/skills/DEBT.md` (two rows drained), `docs/retrospectives/skills-tidy-wolverine-marten-automation.md` (new) |
| **Source-of-truth dependencies** | [`docs/skills/DEBT.md`](../skills/DEBT.md) (rows to drain); [`docs/retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md`](../retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md) (gap evidence); the working code in `src/CritterCab.Dispatch/FareQuoting/` and `src/CritterCab.Dispatch/CandidateSelection/` (canonical reference) |
| **Workflow position** | Second skill-tidy session (first was PR #7, `skills-tidy-marten-and-bootstrap`). Drains the two at-threshold `DEBT.md` rows registered 2026-06-25. Item 1 of the [post-W006 handoff](../planning/2026-07-02-post-w006-next-steps-handoff.md)'s ordered table — a recommended pre-step ahead of the Telemetry proto authorship + implementation chain, chosen to sharpen the critter-skill-auditor before that larger session. |

---

## Framing — why this session exists

Retro 005 (Dispatch slice 5.3, `CandidateSelectionAutomation`) surfaced two skill-file gaps that did not block the session-runner — the runner adapted to the actual pattern inline, matching the precedent already established by `FareQuoteAutomation` in slice 5.2. Both gaps were registered in [`docs/skills/DEBT.md`](../skills/DEBT.md) instead of being absorbed into the implementation PR, per the **Session and PR cadence** rule in [`docs/prompts/README.md`](./README.md#session-and-pr-cadence).

This is also a **design-return-adjacent interleave**: the post-W006 handoff frames this tidy as the small, low-risk step to take before CritterCab's first real transport-wiring session (Telemetry gRPC + Kafka). Sharpening the skill library now — specifically the critter-skill-auditor's Phase 1 discovery for handler-shape questions — pays off most right before the biggest implementation session yet.

---

## Goal

Author a new `wolverine-marten-automation` skill encoding the event-triggered automation handler shape and the marker-interface union return-type pattern. Drain both `DEBT.md` rows. Update `docs/skills/README.md`'s navigation surfaces (cluster index, tag index, entry-point hubs, cross-reference graph) to register the new skill. Author a retrospective. No opportunistic edits.

---

## Spec delta

This is a skill-documentation tidy, not a narrative or workshop amendment — **honest null spec delta**. Precedent for naming null spec deltas on tidy/housekeeping sessions: `housekeeping-delete-may-15-handoff`, `skills-tidy-ai-skills-sync`, `aspire-reconcile-and-port-band`, `fix-marten9-projection-source-gen`.

---

## Source-of-truth precedence

For each gap, the source of truth is **the working code** in `src/CritterCab.Dispatch/FareQuoting/FareQuoteAutomation.cs` and `src/CritterCab.Dispatch/CandidateSelection/CandidateSelectionAutomation.cs` (plus their marker interfaces and `Program.cs`'s registration), then the retro evidence, then external docs. No skill body is being corrected here (this is new authorship, not a fix to existing prose), but the same precedence applies to what the new skill claims.

---

## Orientation files (read in order)

1. **[`docs/skills/DEBT.md`](../skills/DEBT.md)** — the two rows to drain, already grouped under one heading naming this exact decision (new skill vs. bolt-on).
2. **[`docs/retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md`](../retrospectives/implementations/005-dispatch-slice-5-3-candidates-selected.md)** — original gap evidence.
3. **[`docs/skills/wolverine-handlers/SKILL.md`](../skills/wolverine-handlers/SKILL.md)** — the hub skill considered as a bolt-on target. Its own charter is explicitly trigger-agnostic and already delegates to three named protocol-specific siblings; a fourth trigger shape (event-forwarded automation) is a natural fourth sibling, not an extension of the hub.
4. **[`docs/skills/marten-wolverine-aggregates/SKILL.md`](../skills/marten-wolverine-aggregates/SKILL.md)** — the other plausible bolt-on target, considered and rejected: every worked example there is command-handler shaped and it never mentions `UseFastEventForwarding`.
5. **[`docs/skills/_template/SKILL.md`](../skills/_template/SKILL.md)** — authoring template; this is a from-scratch skill, not a fix.
6. **Working code** (canonical reference for the new skill's content):
   - `src/CritterCab.Dispatch/FareQuoting/FareQuoteAutomation.cs`, `IFareQuoteOutcome.cs`, `FareQuoted.cs`, `FareQuoteFailed.cs`
   - `src/CritterCab.Dispatch/CandidateSelection/CandidateSelectionAutomation.cs`, `ICandidateSelectionOutcome.cs`, `CandidatesSelected.cs`, `NoCandidatesAvailable.cs`
   - `src/CritterCab.Dispatch/Program.cs` — **both** registration prerequisites: `IntegrateWithWolverine(i => i.UseFastEventForwarding = true)` and `opts.Discovery.CustomizeHandlerDiscovery(d => d.Includes.WithNameSuffix("Automation"))`. The DEBT row only names the first; the working code shows a second, independent prerequisite (handler-discovery customization) that is equally load-bearing — Wolverine's default discovery convention does not recognize `*Automation`-suffixed classes without it. This is new grounding the tidy session found, not just a transcription of the retro.
7. **[`docs/skills/README.md`](../skills/README.md)** — navigation surfaces to update (cluster index, tag index, entry-point hubs, Phase 2 cross-reference graph).

---

## Working pattern

- **One session, one PR.** PR title: `tidy: skills — wolverine-marten-automation handler shape`.
- **New-skill authorship, not a bolt-on.** Per the critter-skill-auditor's Phase 1 discovery for this session: author `docs/skills/wolverine-marten-automation/SKILL.md` as a new skill (`cluster: wolverine`), not an extension of `wolverine-handlers` or `marten-wolverine-aggregates`. Both existing skills were read and ruled out explicitly (see orientation files #3–4).
- **Ground every claim in the working code**, not just the retro's paraphrase. The retro is evidence a gap once existed; the code is the reference for what the pattern actually looks like today.
- **No opportunistic edits.** Application code in `src/CritterCab.Dispatch/` is not touched — it is already correct and serves as the reference.
- **`DEBT.md` rows are removed in the same PR.** Move drained rows to `## Recently drained` under a `### 2026-07-02 — wolverine-marten-automation tidy` heading; update the document history line.
- **`README.md` updated in the same PR** per its own "README update on each new skill addition" convention: cluster index, tag index, entry-point hubs (new row under "Wolverine handler authoring"), and the Phase 2 cross-reference graph (a new skill node is a meaningful topology change, not just a content fix — warrants a graph update per the README's own stated threshold).
- **Retrospective committed in the same PR**, at `docs/retrospectives/skills-tidy-wolverine-marten-automation.md`.

---

## Deliverable plan

1. **`docs/skills/wolverine-marten-automation/SKILL.md`** — new skill, `cluster: wolverine`, `tags: [wolverine, marten, automation, event-sourcing, decider-pattern]`. Sections:
   - **When to apply this skill** — authoring/reviewing a `*Automation` class reacting to a domain event already on a stream; explicit anti-trigger pointing to `wolverine-handlers`/`marten-wolverine-aggregates` for command handlers.
   - **Prerequisites** — `wolverine-handlers`, `marten-wolverine-aggregates`.
   - **Registration: two prerequisites, not one** — `UseFastEventForwarding` (forwards stream events into Wolverine) **and** `CustomizeHandlerDiscovery(...WithNameSuffix("Automation"))` (makes `*Automation`-suffixed classes visible to discovery at all). Cite `Program.cs`. Name the failure mode: forgetting either means the automation compiles clean and never fires — no exception, no log, silent no-op.
   - **Event-Triggered Automation Handler Shape** — naming convention (`<X>Automation`, load-bearing given discovery customization above, not cosmetic); `Handle(TriggerEvent @event, [WriteAggregate(nameof(TriggerEvent.StreamIdProperty))] Aggregate, ...)`; the non-first-stream-event claim grounded in `CandidateSelectionAutomation` keying off `FareQuoted` (the *second* event on the `RideRequest` stream); note the absence of a `Validate`/`Before` method in both real examples as an observed characteristic, not a mandated rule (see Decisions to flag #2).
   - **Marker-Interface Union Return Type** — pattern, the `DetermineEventCaptureHandling` mechanical-inertness explanation (compile-time documentation only, zero effect on what gets persisted), when to reach for it (2+ mutually exclusive terminal events), both real examples (`IFareQuoteOutcome`, `ICandidateSelectionOutcome`).
   - **Common pitfalls** — forgetting either registration prerequisite; expecting the marker interface to change persistence; naming an automation `*Handler` (silently invisible to discovery).
   - **See also** — Upstream: `wolverine-handlers`, `marten-wolverine-aggregates`. Downstream: none yet. External: ai-skills `wolverine-handlers-declarative-persistence` for generic `[WriteAggregate]` mechanics.
2. **`docs/skills/README.md`**:
   - Cluster index: add `wolverine-marten-automation` to the `wolverine` cluster row.
   - Tag index: add to `wolverine`, `marten`, `handlers`, and `decider-pattern` tag rows.
   - Entry-point hubs: new row under "Wolverine handler authoring" — `Authoring an event-triggered automation handler | wolverine-marten-automation | wolverine-handlers, marten-wolverine-aggregates | testing-fundamentals`.
   - Cross-reference graph: add a new node to the Phase 2 mermaid diagram (`WH --> WMA`, `MWA --> WMA`), classed `phase2`, plus a one-sentence addition to the Phase 2 prose paragraph.
3. **`docs/skills/DEBT.md`** — remove both rows from `## Open debt`; add `### 2026-07-02 — wolverine-marten-automation tidy` under `## Recently drained` naming both fixes and the new skill; update the document history.
4. **`docs/retrospectives/skills-tidy-wolverine-marten-automation.md`** — retro per [`docs/retrospectives/README.md`](../retrospectives/README.md) format. Root-level (spans a new skill file plus a README plus DEBT.md — same shape as the first skill-tidy retro).

---

## Out of scope

- **Application code changes** in `src/CritterCab.Dispatch/`. Already correct; serves as the reference for this tidy.
- **Item 2 of the post-W006 handoff** (Telemetry `v1` proto authorship). Separate session, separate PR.
- **Resolving whether automations should have a `Validate`/`Before` step.** Neither real example has one; this session documents that as an observed characteristic and flags it as an open question rather than inventing a rule no design session has settled.
- **Cross-skill consistency reconciliation** between `wolverine-handlers`, `marten-wolverine-aggregates`, and the new skill beyond what's needed for accurate `See also` cross-references.
- **The bundling-rule gap** flagged past-threshold in retro 005 but deliberately not registered in `DEBT.md` (no target skill named). Stays open for a future session that can ground the target.

---

## Decisions to flag during the session

1. **New skill vs. bolt-on** — resolved via critter-skill-auditor Phase 1 discovery before this prompt was authored: new skill `docs/skills/wolverine-marten-automation/`, `cluster: wolverine`. Both alternative homes were read and explicitly ruled out (see orientation files #3–4).
2. **Whether the missing `Validate`/`Before` step is worth a rule or just an observation.** Lean: document the absence as an observed characteristic of both real examples (automations react to already-committed events; there's no "reject the trigger" step the way an inbound command has a precondition to reject), explicitly flagged as an open question rather than a mandated convention — no design session has settled whether that's deliberate.

---

## What this prompt's retrospective should specifically capture

- Whether the two-registration-prerequisite finding (both `UseFastEventForwarding` *and* `CustomizeHandlerDiscovery`) was already implied by the DEBT row or is a genuinely new grounding this session surfaced.
- Whether "new skill" was the right call versus the two considered-and-rejected bolt-on homes, now that the skill exists and can be read as a finished artifact.
- Whether the `Validate`/`Before`-absence open question should convert into a DEBT row of its own, or stays as a skill-body caveat.
