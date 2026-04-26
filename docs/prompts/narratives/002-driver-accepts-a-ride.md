# Prompt 002 — Author the Driver-Side Journey Narrative

| Field | Value |
|---|---|
| **Status** | Pending |
| **Authored** | 2026-04-25 |
| **Target artifact** | `docs/narratives/002-driver-accepts-a-ride.md` (slug subject to confirmation once scope is locked) |
| **Companion artifact** | [`docs/narratives/README.md`](../../narratives/README.md) (updated only if format conventions extend; expected to be small or zero) |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md), [`docs/narratives/001-rider-books-a-ride.md`](../../narratives/001-rider-books-a-ride.md) (canonical narrative example) |
| **Workflow position** | Second narrative; pairs structurally with narrative 001 (same incident, opposing POV); downstream of workshop 001 |

---

## Framing — why this session exists

Narrative 001 piloted the narrative document layer with the rider's happy-path journey through Dispatch. Narrative #2 exercises the format under a structurally different protagonist: the **driver** who receives the offer, deliberates, and accepts. Same incident as narrative 001 — Maya's request, Dani's acceptance — narrated from the other side of the matching moment.

Beyond producing a second narrative, this session has three secondary jobs:

1. **Test the format conventions under a different POV.** Narrative 001's protagonist *initiated*; narrative #2's protagonist *receives*. Format guardrails (prose-paragraph Moment bodies, single-named-protagonist, multi-slice Moment convention) should hold without modification — but if they don't, surface that in the retrospective.
2. **Validate the methodology log's first prediction.** Entry 001 of [`docs/research/methodology-log.md`](../../research/methodology-log.md) claims that narrative #2 will produce minimal new convention work — mostly *applied* conventions, with small or zero README revisions. Narrative #2's session is the first empirical test of that prediction.
3. **Consume items from narrative 001's deferred list.** Several items in narrative 001's *"Separate narratives"* and *"UX-or-UI"* deferred buckets become in-scope for this narrative (driver-app offer-card experience, the accept gesture, driver-side notes-for-driver visibility, the driver's experience of the offer countdown).

---

## Goal

Author narrative #2: the driver's happy-path journey covering offer receipt, deliberation, acceptance, and the system-level handoff to Trips, all from Dani Rivera's perspective.

---

## Scope question to settle at session start

**Lock scope before walking Moments.** Two candidates:

| Option | Span | Slice coverage | Notes |
|---|---|---|---|
| **A. Minimal (recommended)** | From `OfferSent` (driver receives offer) through `RideAssigned` (driver-side experience of assignment) | Workshop slices 5.4, 5.5, 5.10 — driver POV | Mirrors narrative 001's tight scoping. Driver onboarding is a precondition absorbed by Setting (parallel to narrative 001's "Maya is authenticated, account in good standing"). Yields ~3 Moments. |
| **B. Broader** | From `DriverCameOnline` through `RideAssigned` | Adds Driver Profile BC events (`DriverCameOnline`) to the citation set | Going-online becomes a Moment 0; narrative gains 1 beat but cites a BC (Driver Profile) that has no workshop yet. Methodology smell — narratives shouldn't cite slice numbers that don't exist. |

**Lean: Option A.** Narrative 001 used Setting to absorb upstream preconditions ("Maya is authenticated"); symmetric treatment for narrative #2's Setting would say "Dani has been online for two hours, driving STANDARD-class in the service area." This honors the README's *"When a new narrative is warranted"* rules — a different precondition along the same matching moment is a Setting concern, not a Moment concern. Picking Option B would force the narrative to cite slices from an un-modeled BC, which violates the cross-reference discipline.

If Option B is chosen, surface it as an open question in the retrospective: *"should narratives cite slice numbers that don't yet exist in any workshop?"*

---

## What this session inherits (no re-litigation)

The following are **locked** in [`docs/narratives/README.md`](../../narratives/README.md). Do not propose alternatives:

- **Format dialect** — Candidate C (NDD-informed structured markdown) with both guardrails (prose-paragraph Moment bodies; bounded frontmatter vocabulary).
- **Frontmatter schema v1** — `slug, status, journey, perspective, scope, bounded_contexts, boundaries_touched, slices_implemented, canonical_id`.
- **Single-named-protagonist** voice convention.
- **Notation conventions** — code-style backticks for events and named projections; plain text for ordinary domain nouns.
- **Slice citation discipline** — `Implements:` line per Moment; do not restate workshop GWT.
- **Cumulative deferral section** at session close, bucketed by the seven disposition tags.
- **Multi-slice Moment convention** — paragraphs grow under existing labels; new labels are not introduced.
- **Retrospective shape** — workshop §12 nine-subsection convention.

If any of these need to flex during narrative #2's walk, that is a methodology log entry, not a per-narrative re-debate.

---

## What this session adopts as new practice (per narrative 001's retrospective)

- **Per-Moment "Things deliberately not included" subsection from Moment 1.** Narrative 001 picked this up mid-walk; narrative #2 starts with it.
- **Pre-walk POV asymmetry sidebar.** See section below.
- **Lean toward dropping elapsed-time references** unless the time is structurally load-bearing (the twenty-second offer expiry is load-bearing; "less than a minute ago"-type decoration is not).

---

## Cast (pre-identified by narrative 001)

- **Dani Rivera** — the protagonist. Named in narrative 001's Cast as "the driver who ultimately gets the assignment." This narrative dramatizes his vantage.
- **Maya Okafor** — offstage participant. Named in narrative 001's Cast as the rider; she becomes the offstage rider in narrative #2 — symmetric to Dani's role in narrative #1.
- **Dispatch** — the narrating system.
- **Trips** — offstage; receives the cross-BC handoff at acceptance.
- **The other four candidate drivers** — offstage. Crucially, **Dani is not aware of them**. From his vantage, only his offer exists. The narrator may name them as system fact via narrator omniscience (just as narrative 001 named Dani when Maya was unaware), but they are not dramatized as part of Dani's experience.

---

## POV asymmetry sidebar (run before walking Moments)

Per narrative 001's retrospective, identify *before authoring* which workshop slices Dani:

- **Directly experiences** — the offer arriving on his phone; the countdown clock; tapping Accept; the assignment confirmation; "you've got the trip."
- **Observes as system response** — the offer card refreshing or vanishing; the system's response after his Accept tap.
- **Is unaware of** — Maya's submission (5.1); fare quoting (5.2); candidate selection (5.3) including his own selection as candidate; the sibling drivers' parallel offers (5.4 from sibling perspective); the sibling revocation cascade after his accept (5.5b); the cross-BC publication mechanics to Trips (5.10).

Slices Dani is unaware of either *do not appear in the narrative* (faithful POV) or appear *only as narrator-omniscient system facts* without being dramatized as Dani's experience.

This drives the Context/Interaction/Response balance per Moment. Expect for Option A:

- **Moment 1 (slice 5.4 from driver POV):** Dani receives Maya's offer — the offer card, the countdown, the embedded fare/route/notes. Slices 5.1–5.3 do not appear; the offer "just arrives" from Dani's vantage.
- **Moment 2 (slice 5.5 from driver POV):** Dani taps Accept. His Interaction *is* the slice's command; this contrasts with narrative 001's Moment 5 where the Interaction was a system event Maya observed. Sibling revocations are narrator-named system facts; not dramatized.
- **Moment 3 (slice 5.5 → 5.10 handoff from driver POV):** Dani sees "you've got the trip — Maya, four minutes to pickup." The handoff to Trips is narrator-omniscient background; Dani's experience is the trip-mode UI taking over.

Optional: collapse Moments 2 and 3 into one beat using the multi-slice Moment convention (paragraphs grow, labels don't), parallel to narrative 001's Moment 5. The agent should propose this structure to the user before walking.

---

## Authorial decisions worth flagging during the walk

- **Offer-card experience.** Narrative 001 deferred this to UX-or-UI bucket. Narrative #2's driver-protagonist makes the offer card front-and-center. Lean: narrate the *experience* (countdown, available actions, offer terms visible) without designing the *screen* (no pixel-grain UX).
- **Maya's notes-for-driver.** Maya wrote "meet at side entrance" in narrative 001's Moment 1. Narrative #2's Moment 1 should pay this off — Dani actually reads the note. This is the v1 visibility decision (notes visible on `OfferSent`); workshop's discrimination-mitigation post-MVP discussion stays out of scope.
- **Deliberation interval.** Narrative 001 said Dani took 8 seconds before tapping Accept. Narrative #2 has the inverse problem: dramatizing 8 seconds of internal deliberation without anthropomorphizing. Lean: externalize — show the countdown ticking, the offer terms refreshing on screen, perhaps Dani's surroundings — not Dani's internal monologue.
- **Acceptance gesture.** Single tap. Narrate it as deliberate without making it agonized.
- **The other four candidates.** Faithful narration: Dani sees only his offer; the four siblings exist only as narrator-omniscient system fact. Avoid "Dani knew four other drivers were also seeing this" — he didn't.
- **The "decision worth keeping" framing.** Narrative 001 used this for Klefter decision-events at slices 5.2, 5.3, 5.5. Narrative #2 may reuse it where a system decision is being recorded; consistency across narratives is itself a convention.
- **Bruun carry-the-value framing.** Narrative 001 rendered as "computed once at send-time and carried on the event itself." Reusable in narrative #2's Moment 1 (the offer's `expiresAt` was computed at send-time; from Dani's vantage, it's just the countdown clock — but the carrying-the-value is what makes the timer immune to mid-flight policy changes).

---

## Orientation files (read in order before starting)

1. **[`CLAUDE.md`](../../../CLAUDE.md)** — the routing layer. Skim if recently read.
2. **[`docs/narratives/README.md`](../../narratives/README.md)** — *the canonical reference for format conventions, voice, slice citations, deferral discipline, notation. Read in full.* This is the operational manual; narrative #2 inherits everything in it.
3. **[`docs/narratives/001-rider-books-a-ride.md`](../../narratives/001-rider-books-a-ride.md)** — the canonical example. Read in full, especially Moment 4 (offer fan-out from rider POV) and Moment 5 (acceptance + handoff). Narrative #2 narrates the *same slices from the other side*; comparing the two renderings is structurally important.
4. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md)** — workshop source-of-truth. Slices 5.4, 5.5, 5.10 are the spine; §3 Ubiquitous Language for terms; §5.5's concurrency invariants are load-bearing for Option A scope.
5. **[`docs/research/methodology-log.md`](../../research/methodology-log.md)** — entry 001 predicts narrative #2's shape. Worth holding in mind during authoring; the retrospective should note whether the prediction held.
6. **[`docs/vision/README.md`](../../vision/README.md)** — NDD-informed framing. Skim if recently read.
7. **[`docs/prompts/README.md`](../README.md)** — prompts conventions; skim for the metadata-block format and naming.

---

## Working pattern

Same interactive cadence as narrative 001:

- Lock scope (Option A vs. B) explicitly with the user **before** walking Moments.
- Run the POV asymmetry sidebar before the first Moment.
- Walk Moments section-by-section. Pause for sign-off after each.
- Each Moment proposal includes authorial calls + "Things deliberately not included" with disposition tags.
- Pair open questions with leaning opinions.
- At session close: aggregate per-Moment deferrals into `## Deferred from this narrative`; write the retrospective; update `docs/narratives/README.md` Index.
- Commit only after explicit sign-off.

---

## Deliverable plan

1. **Narrative file** at `docs/narratives/002-driver-accepts-a-ride.md` (slug subject to change once scope is locked; symmetric to `001-rider-books-a-ride.md`).
2. **Index update** in [`docs/narratives/README.md`](../../narratives/README.md) — add narrative #2's row to the Index table at session close.
3. **Optional: small README revisions** if the walk surfaces a format edge the existing conventions don't cover. *Methodology log entry 001 predicts these will be zero or trivial.*
4. **Retrospective** appended to the narrative file, same shape as narrative 001's retrospective.
5. **Methodology log entry 002 (conditional)** — write only if a cross-cutting observation surfaces that meets entry 001's three criteria (spans / wouldn't-fit-in-retro / predicts). Most likely candidate: confirmation or disconfirmation of entry 001's prediction. If no entry is warranted, none is written; silence is fine.

### Definition of done

- Narrative file committed with `status: accepted` in its frontmatter at session close.
- Cumulative deferral section + retrospective committed in the same file.
- README Index row added.
- Methodology log entry 002 written *if* warranted.

---

## What this session deliberately does NOT carry

- **Pre-acceptance driver lifecycle** (Driver Profile BC) — if Option A scope is locked, this lives entirely in Setting. If Option B is chosen, this becomes its own narrative, not narrative #2's spine.
- **Post-acceptance trip lifecycle** (en-route, arrived, started, in progress, completed) — Trips' timeline; future narrative.
- **Driver-side failure paths** — declined offers (slice 5.6), expired offers (slice 5.7), accepting after expiry, accepting an already-assigned ride. Each warrants its own narrative.
- **Trust & Safety paths** — separate narrative.
- **Format extensions** — if the walk surfaces a needed format extension, raise it; my prediction (per methodology log entry 001) is that none will be needed.
- **Driver onboarding business logic** (vetting, document upload, background check) — Onboarding BC; not modeled.
- **Decline / cancel races during Dani's deliberation** — failure modes; alternate-path narratives.
- **Driver vehicle make/model on screen** — UX-or-UI; narrative #1 deferred this and the same disposition applies.

---

## Memory inheritance

The applicable feedback memories carry into this session via `MEMORY.md`. No restatement needed:

- Proactive projection proposals during artifact authoring.
- Critter Stack primitives over bespoke alternatives.
- BC-owned enums.
- Wolverine Aggregate Workflow = Decider Pattern.
- Communication preferences (depth, ubiquitous language, leaning opinions).
- **Explicit deferrals during artifact authoring** with cumulative-tracking nuance — captured during narrative 001 and reinforced at Moment 4 sign-off. Especially relevant here.

---

## Starting move

When the new session begins:

1. Read the orientation files in order.
2. **Lock scope (Option A vs. B) explicitly with the user.** This is the most consequential session-start decision.
3. Confirm Cast — Dani protagonist; Maya offstage; sibling drivers offstage and (crucially) outside Dani's awareness.
4. Run the POV asymmetry sidebar — name which slices Dani directly experiences, observes, is unaware of.
5. Propose Setting (time, place, Dani's online state, vehicle, current location relative to Maya's pickup, policy posture inherited or differing from narrative 001).
6. Propose the Moment count and structure (3 separate Moments, or 2 Moments using the multi-slice convention for slices 5.5 + 5.10).
7. Walk Moments. Pause for sign-off after each.
8. At session close: aggregate deferrals, write retrospective, update README Index, optionally add methodology log entry 002.
