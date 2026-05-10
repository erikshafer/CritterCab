# Prompt 002 — Second Event Modeling Workshop (Trips)

| Field | Value |
|---|---|
| **Status** | Authored |
| **Authored** | 2026-05-08 |
| **Target artifact** | `docs/workshops/002-trips-event-model.md` |
| **Companion artifacts** | [`docs/workshops/README.md`](../../workshops/README.md) (Workshops list + §12.8 follow-ups index — close "Trips workshop pending"); conditionally [`docs/research/methodology-log.md`](../../research/methodology-log.md) (entry 004 if warranted) |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) (the canonical workshop; especially §5.10, §5.12, §11, §12), [`docs/narratives/001-rider-books-a-ride.md`](../../narratives/001-rider-books-a-ride.md), [`docs/narratives/002-driver-accepts-a-ride.md`](../../narratives/002-driver-accepts-a-ride.md), [`docs/decisions/004-design-phase-workflow-sequence.md`](../../decisions/004-design-phase-workflow-sequence.md), [`docs/research/methodology-log.md`](../../research/methodology-log.md) entry 003 |
| **Workflow position** | Second BC workshop. Returns to the **design phase** per ADR-004's design-return cadence rule, after four housekeeping PRs (#7–#10) drained the post-workshop-001 backlog. |

---

## Framing — why this session exists

ADR-004's design-return cadence rule fired at the close of PR #10. After Workshop 001, the project ran a sequence of design and tidy work (narratives 001 + 002, ADR drafting, two implementation PRs, four housekeeping PRs) and the cadence rule now points back at the design phase before any further Dispatch implementation can run.

Workshop 001's §12.8 already named the next major design session: **"Trips BC workshop — strongly indicated. Slice 5.10's intake and slice 5.12's contract stub both depend on Trips committing its side."** That entry is tracked as **pending** in [`docs/workshops/README.md`](../../workshops/README.md) § Workshop follow-ups, and closing it is part of this session's deliverable.

Two of Dispatch's outbound shapes are already authored as receiver-side starting points for Trips (PR #4): the three business-event protos under `/protos/crittercab/dispatch/v1/`. Trips' translation-in slice for `RideAssigned` consumes the proto Dispatch already publishes; the workshop validates that the consumer side fits.

Beyond producing a second workshop, this session has three secondary jobs:

1. **Apply Workshop 001's §12.6 methodology adjustments.** All five are listed below; the workshop's effectiveness against them is the principal §12 retrospective input.
2. **Create the second canonical data point for ADR candidates #6 and #7.** Both ADR triggers fire at this workshop (see §9). The workshop's handling of aggregate identity (#6) and shared cross-BC identifier (#7) is the evidence that lifts both candidates to authored ADRs.
3. **Test the two-layer-fidelity convention** from methodology log entry 003. Three forward-constraints from narratives 001 + 002 land in this workshop. How they are honored, overridden, or partially-honored becomes entry 003's first confirm-or-disconfirm artifact.

---

## Goal

Run CritterCab's second Event Modeling workshop end-to-end and produce a durable artifact for the **Trips bounded context**, structurally parallel to [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md).

---

## Scope question to settle at session start

**Lock scope before walking slices.** Three candidates:

| Option | Span | Notes |
|---|---|---|
| **A. Tight (happy path only)** | Trips intake of `RideAssigned` → en-route → at-pickup → in-progress → `TripCompleted`. No cancellation, no no-show. | Cleanest scope but *asymmetric with Workshop 001's slice 5.12* — Dispatch is already wired to consume `AssignmentOutcomeRecorded` from Trips, which presupposes Trips having outcome events to translate. Choosing A leaves slice 5.12 with no counterpart. |
| **B. Tight + cancellation paths (recommended)** | Option A plus: pre-pickup rider-initiated cancellation, pre-pickup driver-initiated cancellation, no-show timeout (Bruun temporal automation pattern). | Mirrors Workshop 001's treatment — happy path PLUS the alternates that Dispatch's translation-in surface presupposes. Yields Trips' counterpart for slice 5.12. Likely 12–14 slices total. |
| **C. Broad (full lifecycle + failure modes)** | Option B plus: mid-trip cancellation (rider, driver, emergency), in-trip route deviations, payment-authorization failures at trip start. | Bloated for one session. Mid-trip cancellation has its own compensation/driver-protection surface that warrants a dedicated slice family — better as a follow-up workshop or a deferred section in this one. |

**Locked: Option B (tight + cancellation paths).** Rationale: Workshop 001's slice 5.12 (`AssignmentOutcomeRecorded` translation-in) needs a Trips-side counterpart for the cross-BC contract to be meaningful. No-show timeout is a Bruun pattern that pairs naturally with cancellation in narrative ordering (per §12.6 #6).

If during the walk a slice from Option C surfaces as load-bearing for the workshop's coherence, capture it as a parking-lot item rather than expanding scope.

---

## What this session inherits from Workshop 001 (no re-litigation)

The following are **locked** by Workshop 001 and the artifacts that followed it. Do not propose alternatives:

- **Workshop artifact shape** — Scope Statement, Ubiquitous Language glossary, Event List, per-slice walkthrough, cross-reference tables (Translation slices, temporal automations, configuration-as-events, Protobuf surface), Parking Lot / Open Questions, ADR Candidates, §12 nine-subsection Retrospective. See [`docs/workshops/README.md`](../../workshops/README.md) § Conventions.
- **Slice notation conventions** — events past tense; commands imperative; views/projections named ubiquitously (`ActiveRequestsByRider`, `OffersAwaitingExpiry*`); automations as green stickies; temporal automations with the asterisk-suffix todo-list view per Bruun.
- **Klefter decision-event pattern** — when a slice coordinates external systems AND a decision is made locally, promote the decision to a local event.
- **Bruun temporal-automation pattern** — todo-list read model with asterisk suffix, clock-rewind glyph on time-driven automations, configuration-as-events for tunable thresholds.
- **Configuration-as-events pattern** — `*PolicyConfigured` events consolidating operator-tunable parameters; bootstrap discussed when applied (the canonical bootstrap-strategy ADR is candidate #5 from Workshop 001).
- **Shared cross-BC identifier convention** — `tripId` equals `rideRequestId` per Workshop 001 ADR-candidate #7. This workshop authors the second data point for that ADR (see §9 below).
- **`RideAssigned` proto contract** — already authored at `/protos/crittercab/dispatch/v1/ride_assigned.proto` (PR #4). Treat it as input to the translation-in slice; do not re-author or re-shape.
- **§12.7 calibration** — pair open questions with leaning opinions; prefer informative depth over brevity; use ubiquitous language; assume DDD / CQRS / Event Sourcing / EDA as working background.

If any of these need to flex during the walk, that is a methodology log entry, not per-workshop re-debate.

---

## What this session adopts as new practice (per Workshop 001's §12.6)

All five §12.6 adjustments apply here. Each is operationalized below.

1. **Proactive projections from slice 1.** Name candidate projections (read models, downstream consumer views) as flows are walked, from the very first slice — not retroactively at workshop close. Workshop 001 picked this up mid-workshop and had to backfill; Trips starts with it.
2. **Pre-walk aggregate-identity sidebar** — see §7 below. 30-minute section before slice 1.
3. **Calibrate cadence to slice complexity.** Heavy slices (concurrency invariants, aggregate design, cross-BC handoff, cancellation race conditions) warrant extended discussion; lighter slices walked briskly. Don't spend equal time on each.
4. **Defer Protobuf authorship.** Any new Trips business-event protobuf contracts (e.g., trip outcome events for Dispatch / Pricing / Ratings consumers) defer to a follow-up authorship session under ADR-009 — same shape as PR #4. Name the protos that will be needed; do not author them inline.
5. **Number sub-slices explicitly when cascades occur** (5.5a / 5.5b convention from Workshop 001).
6. **Pair temporal-automation slices with their feeder slice in narrative order**, not just at cross-reference. The no-show-timeout Bruun automation should immediately follow the at-pickup slice, not be deferred to a cross-reference table.

---

## Aggregate-identity sidebar (run BEFORE slice 1)

Per §12.6 #2, the aggregate-identity decision shapes every subsequent slice and is hard to retrofit cleanly. This workshop's sidebar serves a second purpose: it is the **canonical second data point for ADR-candidate #6** (aggregate-per-invariant, not aggregate-per-noun). Workshop 001 named the pattern via `RideRequest` + `Offer`; Trips' aggregate-identity decision either confirms the pattern (lifts #6 from candidate to authored ADR) or surfaces a counter-shape that revises it.

**Candidate load-bearing invariants for Trips, with leans:**

1. **Lifecycle-monotonicity (recommended).** Trip walks a strict state machine (intake → en-route-to-pickup → at-pickup → in-progress → completed/cancelled). No skipping, no reversing. If load-bearing, aggregate = `Trip`, stream-per-trip, all lifecycle events on one stream.
2. **At-most-one-trip-per-ride-request (idempotent intake).** Stream keyed by `tripId` (= `rideRequestId` per ADR-candidate #7) enforces uniqueness natively via Marten's stream identity. Likely a *structural consequence* of #1 + the shared-ID decision rather than a separately-load-bearing invariant — but worth surfacing so the workshop confirms.
3. **Single-active-trip-per-driver.** Probably not load-bearing on Trips — enforced upstream at Dispatch's offer-acceptance step. Worth naming to rule out.
4. **In-trip cancellation rules.** Once rider is in vehicle, cancellation paths differ (compensation, driver protection). Likely a *transition rule within* invariant #1 rather than a separate invariant — workshop confirms.

**Lean for the sidebar's outcome:**

- Single `Trip` aggregate, full lifecycle on one stream, no sub-entities. Driver-progression states (en-route, at-pickup, in-progress) are lifecycle transitions on the Trip aggregate (status enum + nullable timestamps per `marten-aggregates` field conventions), *not* separate aggregates with reference IDs and *not* sub-entities on the parent stream.
- Separately-identifiable concerns kept as their own aggregates with ID-only references (loose coupling): driver-position telemetry → out-of-aggregate (Kafka stream, not a Trips aggregate); payment events → Pricing/Payments BC; rating events → Ratings BC.

**Two related questions the sidebar must answer:**

- **Sub-aggregates?** Trips' candidates: driver-progression states (lean: lifecycle transitions on the Trip aggregate, not sub-entities) vs. anything that needs its own concurrency boundary (lean: nothing in Trips' scope). Compare-and-contrast with Dispatch's `Offer` sub-entity-of-`RideRequest` to make the ADR-#6 evidence explicit.
- **What's split off?** High-frequency driver-position telemetry (Kafka, not the aggregate). Payment events (separate BC). Rider/driver ratings (separate BC). Naming the *exclusions* explicitly so the aggregate boundary reads as deliberate, not under-specified.

The sidebar's output goes into the workshop artifact as a §X "Aggregate identity" section, slotted between Scope Statement and Slice walk. The decision is recorded with its rationale and the ADR-#6-evidence framing called out.

---

## Forward-constraints inherited from narratives 001 + 002

Methodology log entry 003 establishes the **two-layer fidelity convention**: narrative authorial-call assumptions about un-modeled BCs become *forward-constraints* on the un-modeled BC's eventual workshop. The workshop must honor or override each constraint with documented reasoning.

Three forward-constraints from narratives 001 + 002 land in this workshop:

1. **Trips' intake is idempotent on `rideRequestId` (= `tripId`).** Stated by both narratives ([001 line 151](../../narratives/001-rider-books-a-ride.md), [002 line 174](../../narratives/002-driver-accepts-a-ride.md)). Aligns directly with ADR-candidate #7. Almost certainly honored — the workshop's intake slice should encode it as a slice-level invariant (likely Slice 1 of the walk).
2. **Trips surfaces rider name to driver post-acceptance.** Methodology log entry 003's specific subject ([narrative 002, Moment 2 second `Response.` paragraph](../../narratives/002-driver-accepts-a-ride.md#moment-2--dani-accepts-the-trip-is-his)). Implies Trips' driver-trip-view projection joins `riderId` to a rider-profile lookup on intake. **This is the substantive one — it has cardinality, transport, and consistency implications.** Three real options the workshop must weigh:
   - **2a. Embed `riderDisplayName` on the intake event.** Denormalize identity-domain data into Dispatch's outbound contract. Fast and synchronous-from-Trips' vantage but couples Dispatch's contract to Identity-BC display concerns. Would require revising the already-authored `RideAssigned` proto.
   - **2b. Trips synchronously fetches rider name from Identity at intake.** Clean separation, but intake becomes dependent on Identity's gRPC availability — a hard runtime coupling for what should be the most-resilient cross-BC handoff in the system.
   - **2c. Trips' projection is eventually consistent on rider name.** Trip aggregate exists immediately on intake (carrying `riderId` only); driver-trip-view projection asynchronously enriches with rider name when Identity publishes `RiderRegistered` / `RiderProfileUpdated` business events Trips subscribes to. Cleanest separation but introduces "name briefly missing" race against Dani's screen transition (forward-constraint #3 below).
   - **Lean: 2c.** Honors structural-constraints' identity-as-ACL discipline (Identity is swappable; other BCs don't couple to provider types or sync calls). The "name briefly missing" race is mitigated by inline projection updates on a hot path; the workshop should weigh whether the timing-budget is feasible (folds into forward-constraint #3).
3. **Trips' projection drives the driver-app transition from offer-mode to trip-mode.** Implies a projection shape that supports the driver-app's trip-mode view AND a timing budget — Dani experiences the screen transition as instantaneous, which constrains how fast Trips' inline-or-near-inline projection must commit relative to the `RideAssigned` consumption. The workshop should: name the projection (e.g., `DriverActiveTrip` or `DriverTripView*`); state its update lifecycle (Inline vs. Async); and document the timing budget as a candidate ADR if the answer turns out to be "tight enough to merit explicit decision."

The workshop artifact's close should include a **§X "Forward-constraints handled"** section that documents each of the three constraints with disposition: honored / overridden / partially-honored. This becomes methodology log entry 003's first concrete confirm-or-disconfirm artifact.

---

## ADR triggers that fire at this workshop

Workshop 001 §11 surfaced 8 ADR candidates with explicit triggers. Four of them have triggers that fire (or potentially fire) at Workshop 002:

**Firm fires:**

- **ADR-candidate #6 — Aggregate-per-invariant pattern.** Trigger: *"first implementation session touching the Dispatch aggregate, **or the Trips BC modeling session.**"* Trips' aggregate-identity sidebar (§7) is the second data point. Workshop should be deliberate about authoring the ADR-shape — even if the candidate isn't promoted to a full ADR within this session, the workshop's §11 should re-state the candidate with the new evidence woven in.
- **ADR-candidate #7 — Shared ride identifier across BCs.** Trigger: *"Trips BC modeling session **or** the first cross-BC integration."* Trips' translation-in slice (Slice 1, mirror of Dispatch slice 5.10) is exactly where this needs to be settled. Cheap to lock in here; expensive to fix later.

**Conditional fires (depend on Trips' shape decisions during the walk):**

- **ADR-candidate #5 — Configuration-as-events bootstrap strategy.** Trigger: *"second BC that adopts configuration-as-events."* Open question: does Trips have policy worth the pattern? Candidates: trip-completion criteria, no-show timeouts, mid-trip cancellation rules. If yes (likely, given no-show timeouts), this ADR fires here — the workshop should name a `TripsPolicyConfigured` slice and surface the bootstrap-strategy decision as either authored or deferred to the ADR.
- **ADR-candidate #8 — ASB topic naming convention.** Trigger: *"second BC's first ASB publication, or the Identity BC workshop."* If Trips publishes any business event to ASB (likely — trip outcome events to Pricing / Payments / Ratings), the convention from `dispatch.ride-assigned` becomes the second instance and the candidate fires.

**Doesn't fire at Trips:** ADR-candidates #1, #2, #3 (Dispatch-internal); #4 (waits for Pricing being actively modeled).

The workshop's §11 should re-list these candidates with their post-workshop status: *promoted to authored ADR* / *evidence added, candidate refined* / *trigger fired, deferred to dedicated authorship session*.

---

## Cast

- **Trips** — the workshop's primary BC.
- **Dispatch** — upstream sender. Already-modeled. Trips consumes `RideAssigned`; Trips publishes `AssignmentOutcomeRecorded`-equivalent back to Dispatch (the slice 5.12 mirror).
- **Pricing** — downstream consumer. Receives trip-outcome event for fare finalization. Workshop scope: name the publication, do not model Pricing's side.
- **Payments** — downstream consumer. Authorization at trip start (offstage in narrative 002 line 88) and capture at trip completion. Scope: name; do not model.
- **Ratings** — downstream consumer. Receives trip-completion event to invite rider/driver to rate. Scope: name; do not model.
- **Telemetry** — high-frequency driver-position feed. Out-of-Trips-aggregate per the sidebar lean. Scope: name as a Kafka transport boundary; do not model.
- **Identity** — rider-profile source for forward-constraint #2. Scope: name as ACL boundary per `identity-acl` skill convention; do not model Identity itself.
- **Recurring narrative characters** — Maya Okafor (rider) and Dani Rivera (driver) named in narratives 001 and 002. Workshop slices that walk the pickup or in-trip moments may invoke them by name for continuity, optionally; the workshop is free to use generic role nouns ("the rider", "the driver") instead since the workshop is structural rather than journey-shaped.

---

## Orientation files (read in order before starting)

1. **[`CLAUDE.md`](../../../CLAUDE.md)** — the routing layer. Skim if recently read; pay attention to Architectural Non-Negotiables.
2. **[`docs/vision/README.md`](../../vision/README.md)** — canonical project overview. Pay attention to the Trips bounded context characterization (tentative scope, post-acceptance ride lifecycle responsibility).
3. **[`docs/research/event-modeling-workshop-guide.md`](../../research/event-modeling-workshop-guide.md)** — the methodology reference. Refresh on Lesson 2 (notation), Lesson 3 (Translation slices), and the applicability notes. Trips is translation-heavy on both ends (intake from Dispatch, outcome to multiple downstream BCs).
4. **[`docs/research/agents-in-event-models.md`](../../research/agents-in-event-models.md)** — Bruun temporal-automation pattern (no-show timeout slice will use it) and Klefter decision-event pattern.
5. **[`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md)** — structural rules. Identity-as-ACL is load-bearing for forward-constraint #2.
6. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md)** — the canonical workshop. Read in full. Pay particular attention to: §3 Ubiquitous Language; §5.10 (Dispatch's send side of the intake Trips consumes); §5.12 (Dispatch's translation-in for Trips outcome — the slice this workshop must produce a counterpart for); §11 (ADR candidates and their triggers); §12 (full retrospective, especially §12.6 adjustments and §12.7 calibration).
7. **[`docs/workshops/README.md`](../../workshops/README.md)** — workshop conventions + § Workshop follow-ups (the index entry this session closes).
8. **[`docs/narratives/001-rider-books-a-ride.md`](../../narratives/001-rider-books-a-ride.md)** — rider-side spine narrative; Moment 5's Trips handoff is the rider-vantage forward-constraint source.
9. **[`docs/narratives/002-driver-accepts-a-ride.md`](../../narratives/002-driver-accepts-a-ride.md)** — driver-side spine narrative; Moment 2's `Response.` second paragraph is the rider-name-surfacing forward-constraint source. Read the cross-references and deferred-items sections in full.
10. **[`docs/narratives/README.md`](../../narratives/README.md)** — particularly the "Two-layer fidelity" section that establishes the forward-constraint mechanism.
11. **[`docs/research/methodology-log.md`](../../research/methodology-log.md)** — entry 003 specifically. The workshop's handling of the three forward-constraints is entry 003's confirm-or-disconfirm artifact; a methodology log entry 004 is conditional on the outcome.
12. **[`docs/decisions/004-design-phase-workflow-sequence.md`](../../decisions/004-design-phase-workflow-sequence.md)** — the design-return cadence rule that triggered this session.
13. **`/protos/crittercab/dispatch/v1/`** — already-authored Dispatch business-event protos (PR #4). Receiver-side starting points for Trips' translation-in slices. Treat as input; do not re-author or re-shape.
14. **[`docs/prompts/README.md`](../README.md)** — prompts conventions. Skim for metadata-block format.
15. **[`docs/prompts/workshops/001-dispatch-event-modeling.md`](./001-dispatch-event-modeling.md)** — first-workshop prompt for shape comparison.

---

## Working pattern

Same interactive cadence as Workshop 001:

- Lock scope (Option A vs. B vs. C — locked to B, but confirm at session start) before walking slices.
- Run the **aggregate-identity sidebar** (§7) before Slice 1. Output captured as a workshop §X section.
- Walk slices section-by-section. **Calibrate cadence per §12.6 #3** — heavy slices (intake idempotency + ID-sharing, no-show Bruun automation, cancellation race conditions) get extended discussion; lighter slices walked briskly. Pause for sign-off after each.
- **Pair open questions with leaning opinions** per §12.7 calibration. Pre-author leans for non-trivial decisions.
- **Name candidate projections from Slice 1** per §12.6 #1. Do not retroactively backfill at workshop close.
- **Number sub-slices explicitly** when cascades occur (e.g., 5.4a / 5.4b for cancellation cascades).
- **Pair temporal-automation slices with their feeder slice in narrative order** per §12.6 #6. The no-show-timeout slice immediately follows the at-pickup slice.
- **Defer protobuf authorship** per §12.6 #4. Name the protos that will be needed; capture as a post-workshop authorship session under ADR-009 (parallel to PR #4).
- At session close: aggregate parking lot, ADR candidates (re-list Workshop 001's #5/#6/#7/#8 with post-workshop status, plus any new candidates), forward-constraints handled section, retrospective per the §12 nine-subsection convention.
- Commit only after explicit sign-off.

---

## Deliverable plan

1. **Workshop artifact** at `docs/workshops/002-trips-event-model.md`, structurally parallel to Workshop 001.
2. **Index update** in [`docs/workshops/README.md`](../../workshops/README.md) — add Workshop 002's entry to the Workshops list AND update the § Workshop follow-ups index to mark "Trips BC workshop — pending" as **done** with the closing artifact link. Add a new § "From Workshop 002 — Trips Event Model §12.8" sub-section enumerating any new follow-ups.
3. **Conditional methodology log entry 004** — write only if a cross-cutting observation surfaces meeting entry 001's three criteria (spans / wouldn't-fit-in-retro / predicts). Two strong candidates: confirmation or disconfirmation of entry 003's two-layer-fidelity-as-forward-constraint prediction; observation about workshop-cadence adjustments from §12.6 actually applied. If no entry is warranted, none is written.
4. **Retrospective** appended to the workshop artifact as §12, same nine-subsection shape as Workshop 001's §12.
5. **Out-of-bounds (note up-front, do not include):** No protobuf authorship, no skill-file edits, no Dispatch revisions, no implementation code. New protos and any skill-file gaps surfaced go to PR-9-style follow-ups (DEBT.md or new prompt).

### Definition of done

- Workshop artifact committed at v0.2 (or whatever version the session-close warrants), structurally parallel to Workshop 001.
- All three forward-constraints handled with explicit disposition in a "Forward-constraints handled" section.
- Aggregate-identity sidebar captured as a §X section with ADR-#6 evidence framing called out.
- §11 ADR candidates re-list with post-workshop status for #5, #6, #7, #8 plus any new candidates.
- §12 nine-subsection retrospective complete.
- `docs/workshops/README.md` updated with Workshops list row and § Workshop follow-ups entry closing.
- Methodology log entry 004 written *if* warranted; silence is fine if not.

---

## What this session deliberately does NOT carry

- **Protobuf authorship** — deferred per §12.6 #4 and ADR-009. Name protos needed; do not author. New authorship is a separate session (PR-4-style).
- **Pricing / Payments / Ratings BC modeling** — Trips publishes business events to these BCs; they consume. Their own workshops are separate sessions.
- **Identity BC modeling** — the rider-name forward-constraint surfaces an Identity-side projection requirement (e.g., publishing `RiderRegistered` / `RiderProfileUpdated`), but Identity's full event model is a separate workshop.
- **Telemetry / high-frequency driver-position transport modeling** — Kafka-shaped, out-of-aggregate, deserves its own workshop or design session.
- **Dispatch revisions** — Workshop 001 is locked. If Trips' translation-in surfaces a needed change to Dispatch's outbound contract (e.g., the rider-name-on-event option 2a above), that becomes a parking-lot item and a separate decision; do not edit Workshop 001 or PR #4 protos in this session.
- **Implementation code** — pure design. No C#, no Go, no proto edits.
- **Skill-file edits** — Trips workshop may surface gaps in skill files (e.g., `marten-aggregates` worked example shapes vs. the Trip aggregate that emerges). Capture as DEBT.md rows or as a follow-up `tidy: skills` PR per PR-7-style; do not edit skill files in-session.
- **Mid-trip cancellation, in-trip route deviation, payment-failure-mid-trip** — Option C scope; held as parking-lot items if they surface.
- **ADR authorship** — Workshop 001 surfaced ADR *candidates*, not authored ADRs. Same applies here. Promotions of candidates #5/#6/#7/#8 are noted as *triggered* and *evidence-backed*; the actual ADR authoring is a follow-up session per ADR-009-style.

---

## Memory inheritance

The applicable feedback memories carry into this session via `MEMORY.md`. No restatement needed:

- Proactive projection proposals during artifact authoring.
- Critter Stack primitives over bespoke alternatives.
- BC-owned enums.
- Wolverine Aggregate Workflow = Decider Pattern with immutable-Apply preference.
- Communication preferences (depth, ubiquitous language, leaning opinions, DDD-background).
- Explicit deferrals during artifact authoring with cumulative-tracking nuance.

---

## Starting move

When the new session begins:

1. **Read the orientation files in order** (§Orientation files above).
2. **Confirm scope.** Locked at prompt-authoring time to Option B (tight + cancellation paths). Validate with the user; if any movement, surface it as the first decision before the sidebar runs.
3. **Run the aggregate-identity sidebar** (§7) before any slice. ~30 minutes. Capture output as the workshop's §X "Aggregate identity" section. State the ADR-#6 evidence framing explicitly.
4. **Propose the slice ordering.** Walk the workshop's spine slices first (intake → en-route → at-pickup → in-progress → completed), with the no-show-timeout Bruun automation slotted immediately after at-pickup per §12.6 #6. Cancellation slices follow the happy path, with sub-slice numbering per §12.6 #5. Translation-out slices (to Dispatch / Pricing / Payments / Ratings) come after the lifecycle slices.
5. **Walk slices.** Pause for sign-off after each. Calibrate cadence per §12.6 #3.
6. **At session close:** aggregate parking lot; re-list §11 ADR candidates with post-workshop status; write the "Forward-constraints handled" section; write §12 retrospective; update `docs/workshops/README.md` (Workshops list + Workshop follow-ups index); optionally append methodology log entry 004.

Don't batch the whole workshop into one output. Workshop sessions are interactive, slice by slice — Workshop 001's cadence is the precedent.
