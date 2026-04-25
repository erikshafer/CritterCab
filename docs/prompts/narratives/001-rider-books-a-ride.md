# Prompt 001 — Author the First NDD-informed Narrative

| Field | Value |
|---|---|
| **Status** | Pending |
| **Authored** | 2026-04-25 |
| **Target artifact** | `docs/narratives/001-rider-books-a-ride.md` (to be produced) |
| **Companion artifact** | `docs/narratives/README.md` (to be updated if format conventions get established) |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) |
| **Workflow position** | First exercise of the narrative document layer; downstream of workshop 001, upstream of any implementation prompt |

---

## Framing — why this session exists

This session is the first concrete test of CritterCab's narrative document layer. The Dispatch event model (workshop 001) just completed; the artifact at `docs/workshops/001-dispatch-event-model.md` is the source of truth for what this narrative implements. The narrative format itself is parked as an open question in the vision doc; this session settles it (or at least picks the dialect to pilot).

The vision doc commits CritterCab to an **NDD-informed** approach (Sam Hatoum / Xolvio), not an NDD-implemented one. No commercial Auto platform, no xolvio/NDK dependency. Structured markdown is the right family; this session picks the dialect.

The narrative-driven gap this fills is real: the vision doc explicitly identifies the workshop-to-implementation handoff as a methodology gap. Workshops produce timelines and slice GWTs; implementations need journey-shaped specs that thread multiple slices into a coherent story. Narratives are the bridge.

---

## Goal

Author the first NDD-informed narrative for CritterCab, covering the rider's happy-path journey through Dispatch (`RideRequested` through `RideAssigned`), and pilot a narrative format dialect by deciding which structural shape it uses.

---

## Orientation files (read in order before starting)

1. **`C:\Code\CritterCab\CLAUDE.md`** — the routing layer. Skim for conventions and non-negotiables.
2. **`C:\Code\CritterCab\docs\vision\README.md`** — canonical project overview. Pay attention to:
   - The Methodology section's NDD framing (lines that name "NDD-informed").
   - The "Capture intent in durable, structured form" design principle (explicit kinship with event-sourcing philosophy).
   - The Open Questions section's note on narrative format remaining unresolved.
3. **`C:\Code\CritterCab\docs\narratives\README.md`** — currently a stub. This session may need to *establish* the narrative format conventions, not just apply existing ones. Update it as part of the deliverable if a format decision is locked.
4. **`C:\Code\CritterCab\docs\workshops\001-dispatch-event-model.md`** — the source of truth for what this narrative implements. Read fully:
   - Scope statement (§2) — what the narrative is bounded by.
   - Ubiquitous Language (§3) — terms the narrative uses.
   - Event List (§4) — the events the happy path traces through.
   - Slice walks (§5.1, §5.2, §5.3, §5.4, §5.5, §5.10) — the slices the happy-path narrative implements.
   - Retrospective (§12) — methodology learnings carried into this session.
5. **`C:\Code\CritterCab\docs\research\sdd-event-model-to-code.md`** — the workflow downstream of narratives. Informs what the narrative needs to carry so that the eventual implementation prompt can lift directly from it.
6. **`C:\Code\CritterCab\docs\research\event-modeling-workshop-guide.md`** — Lesson 13 specifically (Mapping Event Modeling output to Narrative-Driven Development). Sets the conceptual framing.

---

## Working pattern

Same interactive cadence as the Dispatch workshop:

- Propose format candidates first; let the user pick. Then walk the narrative section-by-section (or moment-by-moment, depending on chosen format), pausing for sign-off before committing each piece to the file.
- Don't batch the whole narrative into one output.
- Pair open questions with a leaning opinion when raising them — the user has explicitly called this out as helpful in past sessions.
- Commit only after explicit sign-off; no speculative artifact content.
- Each section should close with a brief "decisions locked" note where format-shaping decisions were made, so the next narrative inherits them rather than re-litigating.

---

## Format options

The format itself is an open question per the vision doc. Propose 2–3 candidate format shapes — concretely, with a short sample fragment of each — and let the user pick one to pilot. Mirror the iterative pattern from the Dispatch workshop: propose, discuss, lock, then walk.

Candidates worth presenting (not exhaustive; propose alternatives if better ones surface):

1. **Freeform markdown with structured headers.** Sections like `Setting`, `Cast`, `Moments`. Each Moment carries `Context` (what's true so far) + `Interaction` (user action or system trigger) + `Response` (system events emitted, views updated). Cross-references workshop slices by event name. Most readable; least format enforcement.

2. **Gherkin-flavored / BDD-shaped.** `Feature` + `Scenario` per moment with `Given/When/Then`. Aligned with NDD's BDD heritage and with the workshop's GWT scenarios. Risk: Gherkin's prose-fighting tendency fragments the rider's journey arc across scenarios.

3. **Schema-backed structured markdown** (Xolvio NDK-inspired but not the library). YAML frontmatter (slug, scope, BCs touched, slices implemented, journey perspective) plus typed body sections (Cast, Setting, Moment[]). Highest format discipline; lintable; downstream-tool-friendly. Cost: upfront convention work and bikeshedding risk.

### Evaluation criteria to weigh when picking

- **Readability cold.** Can someone walking into the project understand the rider's journey from this file alone, without opening the workshop artifact?
- **Implementability.** Can a future prompt document reference this cleanly, pulling the right context for an implementation slice?
- **Format-drift resistance.** Does the structure prevent informal slop as more narratives accumulate?
- **Iteration ergonomics.** When a workshop slice changes, how mechanical is the narrative update?
- **Tool-friendliness.** Could a future LLM workflow generate or validate narratives against this format without bespoke parsing?

### Standing preference

NDD-informed, not NDD-implemented (vision doc v0.2 commits to this). No xolvio/NDK dependency; no commercial Auto-platform format. Structured markdown is the right family — pick the dialect that fits the criteria above.

---

## Voice and perspective

NDD's principle is "told from the user's perspective." A ride-sharing journey has two users — rider and driver — intersecting at acceptance. Decide explicitly whether this narrative is:

- (a) **Single-perspective** (rider only) with the driver as an offstage actor;
- (b) **Multi-perspective** with named POV switches at key moments;
- (c) **Parallel** — two perspective columns or two narratives kept in sync.

Pick one and document the choice in the narratives README. **My lean: (a)** for the spine narrative — keep it in the rider's voice, treat the driver as a participant the rider observes. Multi-perspective complexity belongs in journey-pair narratives later if at all.

---

## Cross-reference discipline

Each moment in the narrative cites the specific workshop slice(s) it implements (e.g., "implements Slice 5.1 + 5.2"). The workshop artifact already has stable slice numbers; lean on them. **Do not** restate the GWT scenarios from the workshop — reference them by slice number. The narrative's job is the journey arc, not the test specification.

When the narrative needs domain terms, use the workshop's Ubiquitous Language (§3): Ride Request, Offer, Candidate, Outstanding Offer, Dispatch Round, Fare Quote, etc. Drift into generic software vocabulary is a smell.

---

## What the narrative does NOT carry

- **No code.** Pure design.
- **No implementation choices** (transport, projection mechanism, aggregate shape — those are workshop concerns or implementation concerns, not narrative concerns).
- **No architectural decisions** — flag any that surface during authoring as ADR candidates, do not resolve in-narrative.
- **No GWT test specifications duplicated from the workshop** — reference, don't restate.

The narrative carries: domain meaning, user-perspective story, journey arc, moment-level state transitions. Everything else is referenced from workshops, skills, or ADRs.

---

## Deliverable plan

1. **A single narrative file** at `docs/narratives/001-rider-books-a-ride.md` covering the happy-path rider journey through Dispatch. Scope: from `RideRequested` through `RideAssigned`. Must trace explicitly to workshop slices 5.1, 5.2, 5.3, 5.4, 5.5, and 5.10. Failure paths (cancel, decline, expire, abandon) are explicitly **out of scope** for this narrative — they belong in subsequent narratives, not as branches inside the spine.

2. **An updated `docs/narratives/README.md`** if (and only if) format conventions get established. The README should cover:
   - What a narrative is in CritterCab's document layering (the workshop-output → narrative → prompt → code chain).
   - The chosen format and a short rationale for the pick.
   - File-naming convention (numeric prefix + slug, mirroring workshops).
   - How narratives reference workshop slices and how slices reference back.
   - Voice and perspective conventions.
   - When a new narrative is warranted vs. extending an existing one.

3. **A retrospective** appended to the narrative file (CritterBids convention; same shape as the Dispatch workshop's §12). Cover: what worked, what was hard, patterns established for the next narrative, format adjustments to consider before narrative #2.

### Definition of done

- Narrative committed.
- Format choice locked (or, if format itself proves harder than expected, the README captures what was learned and what to try in narrative #2).
- Retrospective committed in the same file or as a sibling.
- A short "narrative #2 candidate list" identified — likely candidates: driver-side journey, cancellation journey, abandonment journey, Trust & Safety flag journey.

---

## Out of scope for this session

- Authoring narrative #2 (driver-side, cancellation, etc.).
- Authoring the first prompt document for implementation (downstream from narrative).
- ADR drafting (separate session).
- Protobuf authorship (separate session).
- Trips BC Event Modeling workshop (separate session).

If any of these get pulled in opportunistically during the session, surface the scope expansion explicitly before doing the work, and reflect it in the retrospective.

---

## Memory inheritance

The five feedback memories captured during the Dispatch workshop apply to this session via `MEMORY.md`. No re-statement needed; just be aware they shape behavior:

- **Proactive projection proposals** — name speculative projections with audience, shape, feeders, and status during modeling/authoring.
- **Critter Stack primitives over bespoke alternatives** — implementation mechanism suggestions lead with Wolverine / Marten / Alba primitives.
- **BC-owned enums by default** — cross-BC enums map at the boundary, not shared.
- **Wolverine Aggregate Workflow = Decider Pattern** — `(command, state) → events`; immutable record aggregates with static `Apply` methods using `with`.
- **Communication preferences** — depth over brevity, ubiquitous language, DDD/CQRS/ES/EDA assumed background, leaning opinions on questions.

---

## Starting move

When the new session begins:

1. Read the orientation files in order.
2. Confirm understanding of the workshop artifact's scope (specifically slices 5.1–5.5 and 5.10, the happy-path slices).
3. Propose 2–3 format candidates with sample fragments. Let the user pick.
4. Once format is locked, propose a voice/perspective convention. Let the user pick.
5. Walk the narrative section-by-section. Pause for sign-off after each.
6. At session close, write the retrospective and (if format conventions were established) update `docs/narratives/README.md`.
