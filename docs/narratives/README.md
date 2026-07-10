# CritterCab Narratives

Narratives are the journey-scoped domain specs for CritterCab. Each narrative captures a user journey as a sequence of moments through time, told from a single user's perspective: context, interaction, and system response at each moment. Narratives sit between Event Modeling workshop output and implementation prompt documents.

This README is the operational manual for authoring narratives. For the rationale behind the document layer and CritterCab's NDD-informed framing, see [`docs/vision/README.md`](../vision/README.md). For the canonical example, see [`001-rider-books-a-ride.md`](./001-rider-books-a-ride.md) and its retrospective.

## Where narratives sit in the document layering

```
Workshop (event model + slices, ubiquitous language)
        │
        ▼
Narrative (journey-scoped, user-perspective spec)
        │
        ▼
Prompt document (implementation build order)
        │
        ▼
Code (implementation)
```

Workshops produce point-in-time event-model artifacts. Narratives thread multiple workshop slices into one user's coherent experience. Implementation prompt documents reference narratives; prompts close after their session, narratives persist.

Per the vision doc's *"Capture intent in durable, structured form"* principle, narratives outlive the prompts and code that satisfy them. They are the most stable artifact in the chain.

## Format

CritterCab pilots an **NDD-informed structured-markdown format**. No NDK / Auto-platform dependency. Format dialect locked at narrative 001's authoring; the rationale is captured in that narrative's retrospective.

### File naming

Numeric prefix + slug, mirroring workshops:

- `001-rider-books-a-ride.md`
- `002-driver-accepts-and-starts-trip.md` *(planned)*

The numeric prefix sorts narratives chronologically by *authoring*, not by journey ordering.

### Frontmatter — v1 bounded schema

YAML frontmatter has a **bounded vocabulary** (guardrail #2). The keys below are the entire v1 set; adding a key requires revising this README first.

```yaml
---
slug: <numeric-slug>                  # e.g., 001-rider-books-a-ride
status: draft | accepted | superseded
journey: <protagonist-role>           # rider, driver, operator
perspective: single-rider | single-driver | single-operator | <named>
scope: happy-path | <named-failure> | <named-edge-case>
bounded_contexts: [<BC>, ...]         # primary BCs the journey lives in
boundaries_touched: [<BC>, ...]       # other BCs whose state the journey crosses
slices_implemented: [<slice-num>, ...] # workshop slice numbers covered
canonical_id: <field-name>            # the ID that carries the journey across BCs
---
```

### Body structure

Top-level sections, in order:

1. **Title heading and intro paragraph.** What this narrative is; what's in scope; what isn't.
2. **`## Cast`** — bulleted list of actors. Single named protagonist; other actors named with onstage/offstage status.
3. **`## Setting`** — paragraphs establishing time, place, policy posture, and inherited conditions that subsequent Moments reference without re-stating.
4. **`## Moment N — <one-line beat>`** — one section per Moment, in journey order.
5. **`## Deferred from this narrative`** — cumulative aggregation of per-Moment deferred items, bucketed by disposition.
6. **`## Retrospective`** — session retrospective in the workshop §12 shape.
7. **`## Document History`** — version log.

Each `## Document History` entry names the prompt whose session produced the amendment and confirms what the prompt's spec delta added (a new moment, a forward-constraint update, an amended GWT cross-reference, etc.). This is the closure-loop's fourth step from [prompts README § Spec delta cadence](../prompts/README.md#spec-delta-cadence); the entry is the artifact's own record of the cumulative spec-delta history.

### Moment body structure

Each Moment is composed of **prose paragraphs labeled by phase** (guardrail #1) — not bulleted fields:

```
## Moment N — <one-line beat>

**Implements:** slice X.Y[, slice X.Z…].

**Context.** What is true going in. Prior events, view contents the
protagonist sees, policy posture inherited from Setting.

**Interaction.** What happens this beat. May be a user action (protagonist
taps, types, submits) or a system trigger (an automation reacts to a prior
event, an external boundary returns a value).

**Response.** What the system does in response — events emitted, views
updated, state visible to the protagonist on screen. May span multiple
paragraphs when the Moment covers multiple workshop slices (see
"Multi-slice Moments" below).

**Why this matters to the <protagonist>.** *(optional)* Used only when this
Moment encodes a protagonist-visible invariant or constraint worth
surfacing. Skip when the Moment is self-explanatory.
```

**Bullets are not allowed inside a Moment body.** Bulleted fields turn the narrative into a JSON document with extra steps and break the journey voice.

### Multi-slice Moments

When the protagonist experiences multiple workshop slices as a single beat (e.g., narrative 001's Moment 5 spans slices 5.5 and 5.10), **the Moment body grows in paragraphs, not in section labels.** The `Response.` block becomes multiple paragraphs under one label; new labels are not introduced. The `Implements:` line cites both slices.

## Voice and perspective

Single-named-protagonist by default. The protagonist is named in Cast, observed throughout, and is the only actor whose experience is dramatized.

The narrator is **omniscient about the system** — it can name facts the protagonist doesn't perceive (events committed, projections updated, downstream BCs notified) — but governs *what is dramatized as user experience* by what the protagonist actually perceives. This is what permits Moments where the system does most of the work (automation-driven slices) while keeping the journey voice intact.

Multi-perspective (named POV switches) and parallel (two-column / two-narrative-pair) approaches are deliberate deviations from the default. Use only when single-perspective genuinely fails to render the journey faithfully. Document the deviation in the narrative's intro paragraph and update this README if it becomes a recurring pattern.

## Slice citations

Every Moment cites the workshop slice(s) it implements via the `Implements:` line. Workshop slice numbers are stable; lean on them.

**Do not restate the workshop's Given/When/Then scenarios.** The workshop is the test specification; the narrative is the journey. Restating GWT in narrative form duplicates the workshop and pulls the narrative into the wrong artifact layer.

### Bidirectional referencing (forward-looking)

Workshop slices may cite the narratives that implement them via a `Narratives:` cross-reference line, mirroring the slice-citation discipline in reverse. Workshop 001 was authored before any narrative existed; its slices do not yet carry narrative back-references. Future workshops should adopt the convention from authoring time.

## Notation conventions

- **Code-style backticks** for domain event names and named projection/view names: `RideRequested`, `OfferAccepted`, `ActiveOffersForDriver`, `OffersAwaitingExpiry*`.
- **Plain text** for ordinary domain nouns from the workshop's Ubiquitous Language: Ride Request, Trip, Offer, Dispatch Round, Fare Quote.
- The `*` suffix on todo-list projections (Bruun convention) is preserved in narratives.

Domain language uses the workshop's Ubiquitous Language. Drift into generic software vocabulary is a smell.

## What narratives carry, and don't

**Carry:** domain meaning, user-perspective story, journey arc, moment-level state transitions, system-fact-as-observed-by-protagonist.

**Do not carry:**

- Code or pseudocode.
- Implementation choices — transport (gRPC, ASB, Kafka), projection mechanism, aggregate shape, library primitives. Those belong to skill files.
- Architectural decisions — flag any that surface during authoring as ADR candidates; do not resolve in-narrative.
- GWT test specifications — reference workshop slices by number; do not restate.
- UX / UI design — note app behavior at the rider-experience grain ("status screen ticks forward to…"); don't design the screens.

## Two-layer fidelity

Each narrative has two natural fidelity layers:

- **Locked prose** answers to *narrator-omniscient voice fidelity*. The narrative reads as a story; the narrator is omniscient about the system; meta-labels ("by assumption…", "to be confirmed…", "if Trips' workshop honors this…") break the spell and don't belong here.
- **Authorial calls** captured during the proposal phase answer to *workshop and methodology fidelity*. Self-aware decisions, deferrals, and assumptions about un-modeled BC behavior live here. Authorial calls appear in the session retrospective and feed the methodology log; they do not appear in the locked narrative body.

When a narrative renders behavior that depends on a BC that hasn't been event-modeled yet (e.g., narrative 002's Moment 2 references Trips' rider-name surfacing on the driver-app trip-mode UI), capture the dependency as an authorial-call assumption rather than as inline meta-text. The captured assumption becomes a *forward-constraint*: the un-modeled BC's eventual workshop must honor or override it.

Convention introduced in narrative 002's session (2026-05-04); see methodology log entry 003.

## Per-Moment and cumulative deferral discipline

Every Moment carries (in its proposal phase) a *"Things deliberately not included"* subsection that names what was consciously omitted with a disposition tag. At session close, those omissions consolidate into a **`## Deferred from this narrative`** section, bucketed by disposition. The section mirrors workshop 001's `§10 Parking Lot` and `§11 ADR Candidates` at the narrative layer — it is a project-level backlog feeder, not a transparency footnote.

### Disposition tags (v1)

| Tag | Meaning |
|---|---|
| `defer` | Will revisit; trigger not yet known. |
| `post-MVP` | Beyond v1 scope; flagged for later release. |
| `separate-narrative` | Belongs to a different journey. |
| `separate-workshop` | Belongs to a BC not yet event-modeled. |
| `implementation-detail` | Skill file or ADR territory. |
| `alternate-path-failure` | A failure mode of the same journey; warrants its own narrative. |
| `UX-or-UI-detail` | App design; belongs to design artifacts. |

## When a new narrative is warranted

A new narrative is warranted when:

- The protagonist is different (rider vs. driver vs. operator).
- The journey's terminal outcome is different (assigned vs. cancelled vs. abandoned).
- The journey crosses a *new* set of BCs that prior narratives don't cover.
- The journey exercises a structurally distinct flow (e.g., scheduled rides, pooled rides, multi-leg trips).

A new narrative is *not* warranted for:

- A different policy posture along the same journey (Setting absorbs this).
- A different specific failure mode of the same Moment (per-Moment deferral / cumulative section captures this).
- A different concrete protagonist with the same role and journey shape (named protagonist in Cast carries variability).

When in doubt, prefer extending an existing narrative's Setting or per-Moment alternate-path subsection over forking a new narrative.

## When the narrative layer does not apply

The section above governs *variations of a journey that has a protagonist*. This section governs the prior question: **does the bounded context have a journey to render at all?** Some do not.

A narrative's load-bearing requirement is the [Voice and perspective](#voice-and-perspective) rule — the omniscient narrator dramatizes *what the protagonist actually perceives*. That convention tolerates Moments where the system does most of the work off-stage, but it still hangs on the protagonist perceiving **something**: a view they read (Context), an action they take or a trigger they witness (Interaction), a state change visible to them on screen (Response). A **headless / machine-to-machine bounded context** — one whose slices are entirely inter-service plumbing or server-side processing with no protagonist-perceivable surface — has all three phases empty. There is nothing to dramatize, and forcing a narrative would require **inventing** UX surfaces (a status indicator, a permission prompt) that the workshop never modeled — violating the [`Do not carry: UX / UI design`](#what-narratives-carry-and-dont) guardrail and the [two-layer-fidelity](#two-layer-fidelity) "no confabulation" discipline.

**The test is empirical, not presumed.** Before skipping, read the BC's workshop for *any* protagonist-app-visible detail — a "sharing is on" indicator, a permission prompt, a status surface, an action the protagonist takes or witnesses. If one exists, thread it into a (possibly very short) narrative. If none exists, skip — but **record the skip and its rationale here**; per the [session workflow](../../CLAUDE.md) the narrative step cannot be silently dropped.

This mirrors, one layer up, the same call an Event Modeling workshop makes when it takes the **EM-direct path and skips Domain Storytelling** for a machine-to-machine BC with no human language boundary (W005, W006). No perceiving protagonist, no journey to tell.

### Skipped narratives (recorded)

| BC / Workshop | Decision | Rationale |
|---|---|---|
| **Telemetry** ([Workshop 006](../workshops/006-telemetry-event-model.md)) | Narrative layer **does not apply** (recorded 2026-07-10). | Machine-to-machine throughout: a driver device streams GPS pings over gRPC client-streaming; Dispatch consumes a Kafka-joined view. W006 §6.2 processing is entirely server-side; the UL (§4) defines *driver session* as "a logical concept, decoupled from any physical stream"; no driver-app-visible surface — indicator, permission prompt, or status screen — is named anywhere in W006 or [ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md). No protagonist-perceivable moment exists to dramatize. Matches W006's own EM-direct precedent (it skipped Domain Storytelling for the same reason). See [retro](../retrospectives/telemetry-narrative-layer-decision.md). |

## Retrospective convention

Every narrative ships its retrospective in the same file, appended after the `## Deferred from this narrative` section. The retrospective shape mirrors workshop 001's §12 with narrative-flavored content. See narrative 001's retrospective for the canonical structure.

## Index

| # | Status | Journey | Scope | Slices |
|---|---|---|---|---|
| [001](./001-rider-books-a-ride.md) | Accepted | Rider | Happy path | 5.1, 5.2, 5.3, 5.4, 5.5, 5.10 |
| [002](./002-driver-accepts-a-ride.md) | Accepted | Driver | Happy path | 5.4, 5.5, 5.10 |

*Telemetry (Workshop 006) is intentionally **not** narrativized — a headless machine-to-machine BC with no protagonist-perceivable moment. See [§ When the narrative layer does not apply](#when-the-narrative-layer-does-not-apply).*

## Document history

- **v0.1** (2026-04-25): Initial authoring conventions established alongside narrative 001. Format dialect locked (NDD-informed structured markdown; no NDK dependency). Frontmatter schema v1 bounded. Moment body structure (prose-paragraph labels) locked. Single-named-protagonist voice convention locked. Cumulative deferral discipline established with seven disposition tags. Bidirectional referencing convention proposed (forward-looking; not yet adopted in workshops).
- **v0.2** (2026-05-04): Added "Two-layer fidelity" section per narrative 002's session — locked prose stays in narrator-omniscient voice; assumptions about un-modeled BC behavior live in the authorial-call layer as forward-constraints on later workshops. Added narrative 002 row to the Index.
- **v0.3** (2026-07-10): Added the "When the narrative layer does not apply" section — the criterion for a headless / machine-to-machine BC with no protagonist-perceivable moment, its empirical test, and a "Skipped narratives (recorded)" table. First entry: **Telemetry (Workshop 006)** — narrative layer does not apply; no driver-app-visible surface exists in W006 to dramatize, matching W006's own EM-direct (Domain-Storytelling-skip) precedent. Recorded per the session-workflow rule that the narrative step cannot be silently dropped. Session produced no narrative and no prompt doc; a disposable planning note (`docs/planning/2026-07-10-telemetry-narrative-decision-handoff.md`) served as the prompt-equivalent (W005/W006 precedent). See [retro](../retrospectives/telemetry-narrative-layer-decision.md).
