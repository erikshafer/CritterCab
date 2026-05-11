# Prompt 002 — Bundled ADR Authorship (Patterns + One NFR)

| Field | Value |
|---|---|
| **Status** | Complete (2026-05-10). Five ADRs (011–015) authored at `Accepted`; ADR index updated; W001 §11 + W002 §12 cross-references updated. See [retrospective](../../retrospectives/decisions/002-bundled-pattern-adrs.md). |
| **Authored** | 2026-05-10 |
| **Target artifacts** | `docs/decisions/011-configuration-as-events-bootstrap.md`, `docs/decisions/012-aggregate-per-invariant.md`, `docs/decisions/013-shared-cross-bc-identifier.md`, `docs/decisions/014-asb-topic-naming-convention.md`, `docs/decisions/015-driver-app-projection-timing-budget.md` |
| **Companion artifacts** | [`docs/decisions/README.md`](../../decisions/README.md) (index update — five new rows); cross-reference updates in [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §11 and [`docs/workshops/002-trips-event-model.md`](../../workshops/002-trips-event-model.md) §12 (replace "ADR-candidate #N" with "ADR-0NN"); [`docs/prompts/README.md`](../README.md) (this prompt's entry, if maintained) |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §11 (ADR candidates with explicit triggers), §5.11 (config-as-events), §5.5/§5.10 (aggregate + shared identifier), §5.10 (ASB topic naming); [`docs/workshops/002-trips-event-model.md`](../../workshops/002-trips-event-model.md) §3 (aggregate-per-invariant evidence), §6.1/§6.9 (shared-identifier round-trip + ASB topics), §6.11 (config-as-events second-BC adoption), §6.1 (timing budget); [`docs/decisions/001-record-architecture-decisions.md`](../../decisions/001-record-architecture-decisions.md) (ADR template); per-ADR sources in §6 below |
| **Workflow position** | Follow-up #2 from Workshop 002 §14.8. Bundled ADR authorship session per Workshop 001 §12.6 #4 ("ADR authorship is its own session, not in-workshop drafting"). Five candidates have accumulated: four were named in Workshop 001 §11 (April 2026) and acquired second-BC evidence in Workshop 002 (May 2026); the fifth is new from Workshop 002 slice 6.1. |

---

## Framing — why this session exists

Workshop 001 §11 (2026-04-24) named eight ADR candidates with explicit triggers. Four of those triggers fired during Workshop 002 (2026-05-09) — #5 (config-as-events bootstrap), #6 (aggregate-per-invariant), #7 (shared cross-BC identifier), #8 (ASB topic naming). Each now has two-BC evidence with consistent reasoning across both Workshop 001 and Workshop 002. Workshop 002 also surfaced a new candidate (driver-app projection timing budget) at slice 6.1 that has one-BC source but is structurally an NFR with a measurable observability hook.

ADR authorship has been deferred since April 2026 per the per-workshop discipline ("ADR candidates are captured during modeling, not deferred — but ADR authorship is itself a separate session" — Workshop 001 §12.4 + §12.6 #4). The deferral was deliberate: write the candidate state during the workshop, accumulate two-BC evidence before authoring, then author when the evidence is in. With Workshop 002 closed, the evidence is in; this session pays down the accumulated debt in one bundled PR.

---

## Goal

Author five ADRs (numbers 011–015) capturing patterns confirmed across two bounded contexts plus one cross-cutting NFR. Update the ADR index. Update Workshop 001 §11 and Workshop 002 §12 cross-references to point at the new ADR numbers (replacing "ADR-candidate #N" placeholders). One bundled PR per Workshop 001's preference for related design work.

---

## Scope question to settle at session start

**One bundled session, five ADRs, one PR.** Locked. Rationale:
- Each ADR is small (~50-80 lines per ADR-001/004/005 reference shapes); total session output is ~300-400 lines.
- The pattern ADRs (012, 013, 014) reference each other — bundling them keeps consistency visible during authoring.
- One PR keeps the cross-reference updates (workshops §11/§12) atomic with the ADR creations.

Two real open questions need resolution mid-session — flagged per-ADR in §6. If either turns out to require dedicated investigation that exceeds the session budget, the relevant ADR can land at status `Proposed` rather than `Accepted`, with the open question recorded in its Context section.

---

## ADR numbering

Existing ADRs: 001–010 (see [`docs/decisions/README.md`](../../decisions/README.md)). Next available: 011+.

| Workshop candidate | New ADR | Title |
|---|---|---|
| Workshop 001 §11 #5 | **ADR-011** | Configuration-as-Events Bootstrap Strategy |
| Workshop 001 §11 #6 | **ADR-012** | Aggregate-per-Invariant |
| Workshop 001 §11 #7 | **ADR-013** | Shared Cross-BC Identifier |
| Workshop 001 §11 #8 | **ADR-014** | ASB Topic Naming Convention |
| Workshop 002 §12 (new) | **ADR-015** | Driver-App Projection Timing Budget |

Numbering follows candidate-discovery order from Workshop 001 §11 (#5/#6/#7/#8 in their original W001 sequence), with the new W002 candidate appended as ADR-015.

---

## Per-ADR substance

Each ADR follows the canonical template from [`ADR-001`](../../decisions/001-record-architecture-decisions.md): Status, Date, Context, Options Considered, Decision, Consequences. Aim for ~50-80 lines per ADR consistent with ADR-001/004/005's shape.

### ADR-011 — Configuration-as-Events Bootstrap Strategy

**Status:** Accepted (target).
**Sources:** [W001 §5.11 + §11 candidate #5](../../workshops/001-dispatch-event-model.md), [W002 §6.11 + §3.6](../../workshops/002-trips-event-model.md).
**Pattern (already established by two BCs):** Operator-tunable parameters event-sourced via `<BC>PolicyConfigured` events on a singleton aggregate; full-replacement semantics; cross-parameter validation via Wolverine compound handlers.
**Open question — the load-bearing decision for this ADR:** *How does a configurable BC reach a valid policy state on first deployment?* Three candidates from W001 §11:
- (a) **Migration-time seed** — append a seed `*PolicyConfigured` event with documented defaults during database migration / deployment script.
- (b) **Startup self-seed** — service checks at startup; if stream is empty, appends a seed event from baked-in defaults.
- (c) **Refuse-until-configured** — service starts but rejects all traffic until an operator has explicitly configured policy via the admin command.

**Lean: Option (a).** Migration-time seed has the cleanest auditable trail (the seed event has a real `configuredAt` timestamp from migration time and an `operatorId` of `"system-bootstrap"` matching W001 §5.11's reference seed). Option (b) hides the seed in startup logic which is harder to audit and creates a race if multiple service instances start simultaneously. Option (c) is operationally heavy. Document the decision; capture the rejected options' tradeoffs.

**Consequences to surface:** Each new configurable BC adds one migration with a single seed event; the migration is idempotent if it checks for an empty stream before appending; the seed values are documented in the migration script and traceable through the event log.

### ADR-012 — Aggregate-per-Invariant

**Status:** Accepted (target).
**Sources:** [W001 §5.5 + §11 candidate #6](../../workshops/001-dispatch-event-model.md), [W002 §3 sidebar](../../workshops/002-trips-event-model.md).
**Pattern (confirmed across two BCs):** The aggregate boundary follows the **invariant being protected**, not the noun in the domain language. Workshop 001's `RideRequest` + `Offer` (offer is sub-entity, invariant = at-most-one-`OfferAccepted`-per-request). Workshop 002's `Trip` + driver-progression-states (states are sub-states, invariant = lifecycle-monotonicity).

**No major open questions** — codify the pattern as observed.

**Options Considered to surface:**
- (a) **Aggregate-per-noun** — every domain noun is its own aggregate. Forces cross-aggregate coordination; distributed locking for multi-noun invariants.
- (b) **Aggregate-per-invariant** (chosen) — invariant drives the boundary; sub-entities or sub-states share the parent stream; Marten optimistic concurrency on the parent stream protects natively.

**Sub-entities vs. sub-states discipline (key clarification):** Sub-entities (Workshop 001's `Offer` under `RideRequest`) have their own identity but no independent lifecycle — their events are appended to the parent stream. Sub-states (Workshop 002's driver-progression states under `Trip`) are status-enum + nullable-timestamps on the parent aggregate, not separate entities. Both are valid expressions of the pattern; choose between them based on whether the sub-thing has identity worth tracking separately.

**Consequences to surface:** Sub-entity reference IDs are local to the aggregate's stream (e.g., `offerId` lives on `OfferSent` events on the `RideRequest` stream); cross-stream coordination is avoided; design effort moves from "name the entities" to "name the invariants."

### ADR-013 — Shared Cross-BC Identifier

**Status:** Accepted (target).
**Sources:** [W001 §5.10 + §11 candidate #7](../../workshops/001-dispatch-event-model.md), [W002 §3.6 + §6.1](../../workshops/002-trips-event-model.md).
**Pattern (round-trip confirmed across two BCs):** The same opaque value flows across all bounded contexts that participate in a single domain lifecycle. `rideRequestId` (Dispatch) = `tripId` (Trips) = `paymentReference`-equivalent (Payments) = `rideId` (Ratings).

**No major open questions** — codify the pattern.

**Options Considered to surface:**
- (a) **Per-BC IDs with cross-reference table** — every cross-BC join traverses a mapping table; Trip aggregate carries a `rideRequestId` reference back to Dispatch.
- (b) **Shared canonical ID** (chosen) — same opaque value (UUID v7) across all BCs in the same lifecycle; cross-BC queries are direct lookups, not joins.

**Consequences to surface:** Eliminates cross-reference tables for ride-related queries; simplifies cross-BC analytics (a `CrossBcTripTimeline` projection joins on the same key across BCs); requires that the *first* BC to mint the ID (Dispatch, at `SubmitRideRequest`) commits to a wire format every downstream BC accepts (UUID v7); future BCs joining this lifecycle inherit the ID rather than minting their own.

### ADR-014 — ASB Topic Naming Convention

**Status:** Accepted (target).
**Sources:** [W001 §5.10 + §9 + §11 candidate #8](../../workshops/001-dispatch-event-model.md), [W002 §6.9 + §10](../../workshops/002-trips-event-model.md).
**Pattern (confirmed across two BCs):** Azure Service Bus topics use `<source-bc>.<event-name-kebab>` naming. Two BCs now apply consistently: Dispatch's three topics (`dispatch.ride-assigned`, `dispatch.ride-request-cancelled`, `dispatch.ride-request-abandoned`) and Trips' four topics (`trips.trip-completed`, `trips.trip-cancelled-by-rider`, `trips.trip-cancelled-by-driver`, `trips.trip-abandoned-as-no-show`).

**No major open questions** — codify the convention.

**Options Considered to surface:**
- (a) **Per-team or ad-hoc naming** — names emerge organically; risks inconsistency.
- (b) **`<event-name-kebab>` only** — drops the source-BC prefix; loses ownership signal in the topic name.
- (c) **`<source-bc>.<event-name-kebab>`** (chosen) — source-BC prefix makes ownership explicit and keeps topics globally unique; kebab-case for the event name matches HTTP-style conventions and is consistent with the `protobuf-contracts` skill's other naming choices.

**Decisions to lock:** Session keying is `<canonical-id>` per ADR-013 (e.g., `tripId` / `rideRequestId`); session ordering is strict per session (per ride/trip); no guarantee across sessions. Outbox coordination via Wolverine's `OutgoingMessages` for atomicity with the local commit (Workshop 001 §5.5 pattern).

**Consequences to surface:** Topic names are predictable from the source BC + event name without lookup; cross-BC contracts are discoverable via topic name conventions (a consumer wanting Dispatch's events knows to subscribe under `dispatch.*`); future BCs follow the convention without re-deciding.

### ADR-015 — Driver-App Projection Timing Budget

**Status:** Accepted (target). *Or* `Proposed` if the numeric SLO requires more investigation than the session budget allows.
**Sources:** [W002 §6.1](../../workshops/002-trips-event-model.md), [narrative 002 Moment 2 forward-constraint #3](../../narratives/002-driver-accepts-a-ride.md).
**NFR (single BC origin, one-BC evidence):** The driver-app trip-mode transition (post-`OfferAccepted` → post-`TripMatched` projection arrival → trip-mode UI render) must complete inside a *subjectively instantaneous* time budget so the driver experiences no visible lag between accepting an offer and receiving the trip's pickup navigation.

**Open question — the load-bearing decision for this ADR:** *What numeric SLO target operationalizes "subjectively instantaneous"?* Industry references suggest p95 < 100-250ms for tap-to-render UI transitions. Sub-options:
- (a) **No numeric target** — qualitative budget only; rely on judgment.
- (b) **Hard target, e.g., p95 < 100ms** — aggressive but achievable with inline projection + ASB low-latency mode + gRPC server-streaming push at near-region scale.
- (c) **Tiered target, e.g., p95 < 200ms within region, p95 < 500ms across regions** — accommodates multi-region deployments.

**Lean: (b) p95 < 200ms for the v1 single-region deployment**, with the explicit caveat that multi-region targets are deferred until cross-region deployment is on the roadmap (parking-lot territory). Authoring (b) keeps the SLO measurable via Workshop 002 slice 6.1's `MatchingLatencyMetrics` projection (`dispatchAssignedAt → matchedAt` interval distribution) and the slice 6.5's `DriverAppActivationLatency` projection (`matchedAt → enRouteAt` interval).

**Decisions to lock:** Mechanism (inline `DriverTripView` projection on `TripMatched` commit, async for non-critical-path projections) — already locked at W002 slice 6.1, this ADR codifies it. Observability mechanism — `MatchingLatencyMetrics` projection per slice 6.1's "candidate projections" table, promoted from `defer-but-pin` to `must-author-when-implementation-starts`.

**Consequences to surface:** SLO becomes a measurable cross-cutting NFR, not aspirational text; future projection-design work on latency-critical surfaces (e.g., rider-app live-tracking view) inherits the same discipline; SLO breaches are observable from day one rather than discovered post-incident.

---

## Order of authorship

Walk in ADR-number order: **011 → 012 → 013 → 014 → 015**.

Rationale:
- ADR-011 (config-as-events bootstrap) has the load-bearing open question; tackling it first sets the session's depth tone.
- ADR-012/013/014 are codifications of confirmed patterns; they walk briskly once the workshop sources are in hand.
- ADR-015 (NFR) is structurally different from the other four; tackling it last keeps the pattern-ADR rhythm coherent.

Each ADR walks in the same per-section cadence: Context → Options Considered → Decision → Consequences. Pause for sign-off after each ADR before starting the next.

---

## Authorship guidance

**Apply the ADR template strictly** ([ADR-001](../../decisions/001-record-architecture-decisions.md) defines it). Each ADR has: Title (with three-digit prefix), Status, Date, Context, Options Considered, Decision, Consequences. Don't invent new sections.

**Voice match:** ADRs are durable artifacts. Match the prose style of ADR-004 / ADR-005 — calm, specific, well-reasoned. Avoid editorializing. State the decision; explain the tradeoffs; move on.

**Options Considered is not optional.** Every ADR includes at least two options (the chosen one plus one or two rejected). Naming what was rejected and *why* is the most useful part of an ADR for future readers per ADR-001's "understanding what was *not* chosen, and why, is often more useful."

**Cross-reference workshops, but do not restate them.** ADR Context sections cite Workshop 001 §X or Workshop 002 §Y for evidence; the ADR itself codifies, doesn't rehash.

**Status field discipline.** `Accepted` for the four pattern ADRs (evidence is solid). For ADR-015, prefer `Accepted` if the user signs off on a numeric SLO mid-session; otherwise `Proposed` with the SLO question recorded in Context.

---

## Orientation files (read in order before starting)

1. **[`docs/decisions/001-record-architecture-decisions.md`](../../decisions/001-record-architecture-decisions.md)** — the canonical ADR template and writing guidelines.
2. **[`docs/decisions/004-design-phase-workflow-sequence.md`](../../decisions/004-design-phase-workflow-sequence.md)** and **[`docs/decisions/005-transport-selection-by-flow-type.md`](../../decisions/005-transport-selection-by-flow-type.md)** — voice and depth references; ADR-005 is the closest analogue to several of this session's pattern ADRs (Options A/B/C structure, decision rationale, consequence enumeration).
3. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §11** — original ADR candidate writeups with triggers; primary source for ADRs 011–014.
4. **[`docs/workshops/002-trips-event-model.md`](../../workshops/002-trips-event-model.md) §3, §6.1, §6.11, §6.9, §12** — second-BC evidence for ADRs 011–014; primary source for ADR-015.
5. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §5.5, §5.10, §5.11** — slice-level details for the four pattern ADRs.
6. **[`docs/workshops/002-trips-event-model.md`](../../workshops/002-trips-event-model.md) §6.1, §6.6, §6.11, §6.9** — slice-level details mirroring W001's evidence.
7. **[`docs/decisions/README.md`](../../decisions/README.md)** — index requiring five new rows.

---

## Working pattern

- Read orientation files in order before starting (per §Orientation).
- Walk ADRs **one at a time** in 011 → 015 order. For each ADR:
  - Pre-author the Context paragraph drawing on workshop sources.
  - Pre-author the Options Considered section with all candidates (chosen + rejected) named.
  - Pre-author the Decision section with explicit lean.
  - Pre-author the Consequences section.
  - Pause for sign-off; commit the ADR file; move to the next.
- For ADRs 011 and 015 (the two with open questions), surface the question with a leaning opinion per memory `feedback_communication_depth_ubiquitous_language.md`. Don't ask without leans.
- After all five ADRs are signed off, update `docs/decisions/README.md` with five new rows.
- After README update, update `docs/workshops/001-dispatch-event-model.md` §11 and `docs/workshops/002-trips-event-model.md` §12 to replace "ADR-candidate #N" references with "ADR-0NN" links. This keeps cross-references current; minimal edit; per-workshop bookkeeping.
- Commit cadence: one commit per ADR (or one bundled commit at session close — author's call). PR shape: one bundled PR titled to reflect the five-ADR scope.

---

## Deliverable plan

1. **`docs/decisions/011-configuration-as-events-bootstrap.md`** — new ADR; ~70 lines.
2. **`docs/decisions/012-aggregate-per-invariant.md`** — new ADR; ~60 lines.
3. **`docs/decisions/013-shared-cross-bc-identifier.md`** — new ADR; ~60 lines.
4. **`docs/decisions/014-asb-topic-naming-convention.md`** — new ADR; ~60 lines.
5. **`docs/decisions/015-driver-app-projection-timing-budget.md`** — new ADR; ~70 lines (NFR; includes the SLO discussion).
6. **`docs/decisions/README.md`** — add five new rows to the ADR index.
7. **`docs/workshops/001-dispatch-event-model.md` §11** — replace "ADR-candidate #5/#6/#7/#8" wording with "ADR-011/012/013/014" links. Bump W001 status to v0.4 with a Document History entry.
8. **`docs/workshops/002-trips-event-model.md` §12** — replace candidate-number references with ADR-number links. Bump W002 status to v0.13 with a Document History entry.
9. **(Optional) `docs/prompts/README.md`** — if maintained, add this prompt's entry. (Skip if the README isn't being kept current.)
10. **Retrospective entry** appended to this prompt document at session close (mirrors the `001-protobuf-ride-assigned.md` retrospective convention if it has one; otherwise a brief close-of-session note in this prompt's Status field).

### Definition of done

- Five ADR files committed at `Accepted` status (or `Proposed` for ADR-015 if numeric SLO is deferred).
- ADR index updated with five new rows.
- Workshop 001 §11 and Workshop 002 §12 cross-reference updates committed.
- Bundled PR opened with all changes, title and body explicitly enumerating the five ADRs and the cross-reference updates.
- No code changes; no protobuf changes.

---

## What this session deliberately does NOT carry

- **No code, no protobuf authorship, no service skeletons.** This is pure ADR work.
- **No skill-file authoring.** ADRs may surface skill-file gaps (e.g., the bootstrap mechanism in ADR-011 might warrant a `migration-bootstrap` skill); those become parking-lot items, not in-session work.
- **No retrospective edits to workshops 001 / 002 beyond cross-reference updates.** §12.4 of W001 ("§§§") and §14.4 of W002 are durable artifacts. The cross-reference update is mechanical (replacing "ADR-candidate #5" with "ADR-011"); larger workshop edits are out of scope.
- **No new ADR candidates.** This session writes the five named candidates. New candidates surfaced during authoring go to a parking-lot section (in this prompt, or a follow-up note).
- **No mid-trip cancellation workshop, no Identity workshop, no Pricing workshop.** Those are separate design sessions.
- **Force-push or history rewrite for prior commits.** N/A; not relevant here.

---

## Memory inheritance

The applicable feedback memories carry into this session via `MEMORY.md`. No restatement needed:

- **Critter Stack primitives over bespoke alternatives** — relevant for ADR-011 (bootstrap mechanism: prefer Marten / Wolverine primitives over custom seeding logic).
- **BC-owned enums** — relevant for ADR-013 (shared identifier is the value, not the field name; field names can vary per BC).
- **Wolverine Aggregate Workflow = Decider Pattern** — relevant for ADR-012 (aggregate-per-invariant ties to the Decider pattern's `decide(command, state) → events`).
- **Communication preferences (depth, ubiquitous language, leaning opinions)** — relevant throughout; pre-author leans on ADR-011's bootstrap question and ADR-015's SLO target.
- **Explicit deferrals during artifact authoring** — relevant for ADR-015's multi-region deferral and ADR-011's "skill-file gap parked, not authored."
- **Static endpoints, Alba-first tests** — not directly relevant (no code in this session) but applies if any ADR's consequence section discusses testing implications.
- **No Claude attribution on commits or PRs** — relevant at commit/PR time. This session's commits and PR omit `Co-Authored-By: Claude` trailers and any "Generated with Claude Code" footer.
- **Keep READMEs current alongside session work** — relevant for the `docs/decisions/README.md` index update; it must land in the same PR as the ADRs.

---

## Starting move

When the new session begins:

1. **Read the orientation files in order** (§Orientation files above).
2. **Confirm scope.** Five ADRs in one bundled session, one PR. Validate with the user; if any movement, surface it before authoring.
3. **Walk ADR-011 first.** Surface the bootstrap-mechanism open question with the leaning opinion (option a, migration-time seed). Get sign-off on the lean before drafting the full ADR text. Author the ADR; sign-off; commit.
4. **Walk ADRs 012, 013, 014.** Each is mostly codification; each should walk briskly (~15-20 minutes per ADR). Sign-off and commit per-ADR.
5. **Walk ADR-015 last.** Surface the numeric SLO open question with the leaning opinion (p95 < 200ms for v1 single-region). Status decision: `Accepted` if user signs off on the lean; `Proposed` if more investigation warranted. Author the ADR; sign-off; commit.
6. **Update `docs/decisions/README.md`** with five new rows.
7. **Update Workshop 001 §11 and Workshop 002 §12** cross-references (mechanical replacement of "ADR-candidate #N" with "ADR-0NN" links). Bump W001 to v0.4 and W002 to v0.13 with Document History entries.
8. **Open bundled PR.** Title: descriptive, listing the five ADRs in scope. Body: per-ADR summary + cross-reference update note + test plan (read-through checklist for each ADR).
9. **Append retrospective note to this prompt's Status field** indicating completion.

Don't batch the whole session into one output. ADR sessions, like workshop sessions, are interactive — one ADR at a time, sign-off after each. Total session estimate: 90–120 minutes.
