# Prompt — Author the Context-Map Foundation Artifact

| Field | Value |
|---|---|
| **Status** | Pending (authored 2026-05-15; awaiting review before execution) |
| **Authored** | 2026-05-15 |
| **Target artifacts** | `docs/context-map/README.md` (new directory + foundation artifact); `docs/vision/README.md` (cross-reference + version bump to v0.4); `docs/decisions/016-context-map-as-living-artifact.md` *(optional — session-start judgment call)*; `docs/decisions/README.md` (index update, only if ADR-016 lands); `docs/prompts/README.md` (this prompt's index entry, bundled with the prompt commit per the [skill-tidy retro lesson](../retrospectives/skills-tidy-marten-and-bootstrap.md)); `docs/retrospectives/context-map-foundation.md` (retro); `docs/retrospectives/README.md` (index entry) |
| **Source-of-truth dependencies** | [`docs/vision/README.md`](../vision/README.md) §Methodology + §Tentative Bounded Contexts; [`docs/decisions/004-design-phase-workflow-sequence.md`](../decisions/004-design-phase-workflow-sequence.md) (step #1 = Context Mapping); [`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md); [`docs/decisions/013-shared-cross-bc-identifier.md`](../decisions/013-shared-cross-bc-identifier.md); [`docs/decisions/014-asb-topic-naming-convention.md`](../decisions/014-asb-topic-naming-convention.md); [`docs/workshops/001-dispatch-event-model.md`](../workshops/001-dispatch-event-model.md) §6 (Translation Slices at BC Boundaries), §7, §9, §11; [`docs/workshops/002-trips-event-model.md`](../workshops/002-trips-event-model.md) §13 (Forward-Constraints Handled) + slice-walk translation slices; [`docs/research/agents-in-event-models.md`](../research/agents-in-event-models.md) (Klefter translation-slice pattern) |
| **Workflow position** | Closes a long-open methodology commitment. The vision doc names "first-class context map" as a top-line goal since v0.1 (2026-04-21); ADR-004 names "Context Mapping" as step #1 of the design-phase workflow sequence (2026-04-23). Two workshops (Dispatch, Trips) have shipped without the artifact existing — implicit context-mapping has been workable at two BCs but won't scale through the seven-to-nine BC range the vision projects. This session lands the artifact before BC #3 (Identity, the strongest near-term candidate) is workshopped. |

---

## Framing — why this session exists

CritterCab has been operating in silent violation of ADR-004 step #1 for three weeks. The vision doc has named "first-class context map" as a methodology goal since v0.1, and ADR-004 makes it a hard prerequisite for Event Modeling work — yet two workshops have shipped without the artifact landing. The implicit mapping worked while only Dispatch ↔ Trips existed; with Identity, Pricing, Payments, Ratings, Operations, Onboarding, Driver Profile, and Rider Profile all pending (target 7–9 BCs), implicit mapping will not survive.

The artifact's content largely exists — it is distributed across [ADR-006](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) (Identity ACL), [ADR-013](../decisions/013-shared-cross-bc-identifier.md) (shared identifier as the cross-BC value-flow primitive), [ADR-014](../decisions/014-asb-topic-naming-convention.md) (ASB topic naming as the cross-BC publication mechanism), [W001 §6](../workshops/001-dispatch-event-model.md) (translation slices at BC boundaries), [W002 §13](../workshops/002-trips-event-model.md) (forward-constraints handled, plus the new outbound forward-constraint on Identity's eventual workshop), and the [Klefter translation-slice research note](../research/agents-in-event-models.md). This session's job is to **roll up** that distributed content into a single artifact named in DDD strategic-design vocabulary — not to re-decide any relationship already committed by an ADR or workshop.

The artifact's update cadence is the secondary deliverable: when a new BC workshop runs, the context map must be updated in that workshop's PR (the same way W002 already updated W001's §5.12 enum when its override surfaced gaps). This cadence is what makes the artifact "living" rather than a one-shot snapshot. Whether that cadence warrants ADR-016 status is a session-start judgment call (see Decisions to flag).

---

## Goal

Author `docs/context-map/README.md` as the foundation context-map artifact: prose framing the artifact's purpose and update cadence; a Mermaid diagram naming each BC as a node and each cross-BC relationship as a labeled edge with its DDD relationship pattern; per-edge prose sections naming the relationship pattern with rationale and cross-references to the source ADRs / workshop sections; explicit deferral markers for relationships involving pre-workshop BCs.

Bump the vision doc to v0.4 with the cross-reference added. Optionally author ADR-016 ("Context Map as Living Artifact") if the cadence rule warrants ADR-status durability.

---

## Scope question to settle at session start

**One bundled PR for the context-map README + vision-doc cross-reference + optional ADR-016 + this prompt and its retro.** Defensible as a single-artifact session per the one-prompt-one-PR cadence — the ADR-016 question lives or dies as a session-start judgment call, and if it lands, it lands as a *consequence* of authoring the artifact (the artifact's existence is what motivates the cadence rule).

**If ADR-016 is deferred**, the PR is README + vision-doc-cross-reference + prompt/retro, and the ADR question becomes a parking-lot item for a follow-up session triggered when (a) a new workshop lands and exposes the cadence question for real, or (b) the artifact's first update reveals what the cadence rule actually needs to say.

Recommended ADR-016 disposition: **defer.** The cadence rule is best authored after the first real update lands (when W003 — most likely Identity — adds a new BC and the map must be amended); that first update will surface the cadence-rule mechanics empirically rather than hypothetically. The session-runner should flag this question and lean toward defer unless something surfaces during authoring that argues otherwise.

---

## Pre-work — handoff document inaccuracies to flag

The handoff doc that motivated this session (`docs/planning/2026-05-15-spec-delta-and-context-map-handoff.md`) contains two inaccuracies the session-runner should notice rather than blindly follow:

1. **W001 has no §13.** The handoff lists "Workshop §13 forward-constraints in W001 and W002" as source material. Only W002 has a §13 (Forward-Constraints Handled), introduced in W002 because narratives didn't yet exist when W001 was authored. For W001, the analogous cross-BC content lives in **§6 (Translation Slices at BC Boundaries — cross-reference)**, **§7 (Temporal Automation Slices — cross-reference)**, **§9 (Candidate Protobuf Contract Surface)**, and **§11 (ADR Candidates)**. Source citations in the artifact should reflect this, not the handoff's wording.
2. **Narratives DO already have Document History sections.** Both [narrative 001](../narratives/001-rider-books-a-ride.md) (v0.1, 2026-04-25) and [narrative 002](../narratives/002-driver-accepts-a-ride.md) (v0.1, 2026-05-04) have populated `## Document History` sections; the narratives README already names it as body-section #7. This inaccuracy affects Session A (spec-delta methodology), not this session — flagged here so it doesn't leak into the context-map artifact's narrative cross-references.

Neither inaccuracy changes the substance of what this session produces. They affect the prose; surface them in the retro under "what was harder than expected" so the planning-doc convention learns from the slip.

---

## Orientation files (read in order)

1. **[`docs/vision/README.md`](../vision/README.md) §Tentative Bounded Contexts** — the canonical BC list (Identity, Onboarding, Rider Profile, Driver Profile, Telemetry, Dispatch, Trips, Pricing, Payments, Ratings, Operations). Eleven candidates, collapsing to 6–8 deployable services. The context map names *bounded contexts*, not deployable services — the collapsing question is parked.
2. **[`docs/decisions/004-design-phase-workflow-sequence.md`](../decisions/004-design-phase-workflow-sequence.md)** — context mapping is step #1; this artifact closes a load-bearing methodology commitment. Reference the ADR in the README's framing prose.
3. **[`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md)** — the Identity ↔ rest-of-system relationship is decided here as **anti-corruption layer** (Identity-side). The context map must reflect this verbatim, not re-litigate it.
4. **[`docs/decisions/013-shared-cross-bc-identifier.md`](../decisions/013-shared-cross-bc-identifier.md)** — every cross-BC relationship participating in a shared ride lifecycle inherits the canonical UUID v7 identifier. This is the *value-flow* substrate for several relationships and should be cross-referenced from every edge that crosses a ride-lifecycle boundary.
5. **[`docs/decisions/014-asb-topic-naming-convention.md`](../decisions/014-asb-topic-naming-convention.md)** — the publication mechanism for business-event-bearing relationships. Cross-reference from every published-language edge.
6. **[`docs/workshops/001-dispatch-event-model.md`](../workshops/001-dispatch-event-model.md) §6, §7, §9, §11** — Dispatch's cross-BC topology as captured in the first workshop. Specifically: §6 names the translation slices at BC boundaries; §9 names the outbound Protobuf contract surface; §11 names the ADR candidates that committed cross-BC relationships.
7. **[`docs/workshops/002-trips-event-model.md`](../workshops/002-trips-event-model.md) §13 + slice 6.9/6.10 + slice 6.12** — Trips' inbound (from Dispatch + Identity) and outbound (to Dispatch, Pricing, Payments, Ratings, Driver Profile, Trust & Safety, Operations) topology. §13 is the consolidated forward-constraints view; slice 6.9 + 6.10 are the four-way terminal-event fan-out; slice 6.12 is the non-Klefter Translation-in pattern from Identity (a `RiderProfileSnapshot` projection driven by Identity events).
8. **[`docs/research/agents-in-event-models.md`](../research/agents-in-event-models.md)** — Klefter translation-slice pattern is the *tactical implementation* of certain relationship types (published-language inbound becomes a translation slice with a local decision-event). The context map cites this for the implementation grain; the strategic relationship pattern is named separately at the edge.

---

## Working pattern

The session walks **one cross-BC edge at a time**, in a deliberate order — analogous to the bundled-ADR session's per-ADR walk with sign-off between artifacts. Each edge gets: a name (source BC → target BC), a DDD relationship pattern (one of: partnership, customer-supplier, conformist, shared-kernel, published-language, anti-corruption-layer, separate-ways, open-host-service), a rationale paragraph citing the source ADR / workshop section, and (where applicable) a tactical-implementation cross-reference to the translation-slice pattern.

Order of edge authorship (the order that makes the rationale read cleanest):

1. **Dispatch → Trips** (customer-supplier with published language; handoff of canonical ride ID via `RideAssigned` published as `dispatch.ride-assigned` per ADR-014; downstream Trips is the customer, upstream Dispatch is the supplier). The most evidence-rich edge — every workshop has touched it.
2. **Trips → Dispatch** (the reverse direction — Trips publishes terminal events back to Dispatch as published language; Dispatch is a *conformist* consumer that updates `OutboundRideTerminationStatus` per W001 §5.12 / W002 §6.9 override). Asymmetric pair with edge #1 — Dispatch is the supplier of trip handoff but a conformist consumer of trip terminations. Worth naming the asymmetry explicitly.
3. **Identity → rest-of-system** (anti-corruption layer Identity-side per ADR-006; published-language outbound to consumer BCs via ASB business events). The strongest pre-committed relationship pattern in CritterCab; the ACL stance is locked, the published-language outbound is locked, the inbound to consumer BCs is each consumer's own translation-slice (cf. W002 slice 6.12).
4. **Trips → Pricing / Payments / Ratings / Driver Profile / Trust & Safety / Operations** (published-language fan-out per W002 §6.9/§6.10 + ADR-014). Six edges sharing one pattern; can walk as a paired group with per-target rationale rather than one-at-a-time. Each downstream consumer is presumed-conformist pending its own workshop.
5. **Telemetry → Dispatch** (upstream supplier of geospatial index — relationship is customer-supplier with Dispatch as customer; specific pattern shape pending Telemetry's workshop). Defer relationship-pattern naming with a clear "pending workshop" marker per the explicit-deferrals discipline.
6. **Onboarding → Driver Profile** and **Identity → Rider Profile / Driver Profile** (intra-actor relationship topology; both legs pending workshops). Defer with markers.

For each edge:
- Pre-author the edge name and the relationship-pattern lean.
- Surface the lean for sign-off (especially for edges #2 — the conformist-reverse — and #4's six-way fan-out where the conformist label is the most defensible-but-fragile call).
- Author the per-edge prose paragraph.
- Update the diagram in-place with the labeled edge.
- Pause for sign-off; move to the next edge.

After all edges are signed off:
- Add the artifact's framing prose (overview, update cadence, relationship to vision doc and ADR-004).
- Bump vision doc to v0.4 with cross-reference and Document History entry.
- Decide ADR-016 question (lean: defer; see Scope question).
- Compose the retro per the [retrospectives README](../retrospectives/README.md) conventions.

---

## Deliverable plan

1. **`docs/context-map/README.md`** — new artifact. ~150–250 lines. Structure:
   - Title and one-paragraph framing (what this artifact is, what it isn't, its update cadence).
   - Cross-reference to vision doc §Methodology + ADR-004 step #1 (closes the methodology commitment).
   - Mermaid diagram: each BC as a node, each cross-BC relationship as a labeled edge with its DDD relationship pattern abbreviated on the edge label (e.g., `CS/PL` for customer-supplier with published language, `ACL` for anti-corruption layer).
   - Per-edge prose section walking each relationship in the diagram with full pattern name, rationale, and source cross-references.
   - "Pending workshops" subsection consolidating the deferred relationship-pattern calls (Telemetry, Pricing, Ratings, Payments, Operations, Onboarding, Rider Profile, Driver Profile) with what each pending workshop is expected to resolve.
   - "Update cadence" subsection: when a new BC workshop lands, the context map updates in that workshop's PR.
2. **`docs/vision/README.md`** — add cross-reference to the new artifact in §Methodology (specifically the DDD strategic-design bullet); bump version to v0.4 with a Document History entry naming the artifact addition.
3. **`docs/decisions/016-context-map-as-living-artifact.md`** *(optional, session-start decision)* — ADR per the canonical template if the cadence rule warrants ADR status. Lean: defer until the first update lands (when W003 is run and the map is amended).
4. **`docs/decisions/README.md`** — add ADR-016 row only if #3 lands.
5. **`docs/prompts/README.md`** — add this prompt's entry under `Multi-artifact prompts (root)`. Bundle with #1 in one commit per the [skill-tidy retro lesson](../retrospectives/skills-tidy-marten-and-bootstrap.md) on prompt-and-index commit bundling.
6. **`docs/retrospectives/context-map-foundation.md`** — retro per the [retrospectives README](../retrospectives/README.md) format. Capture: whether the per-edge walk produced cleaner artifact than a top-down "describe-all-relationships" pass would have; whether any pre-committed relationship (ADR-006, ADR-013, ADR-014) needed re-litigation despite the no-re-decide watch-out; how many edges needed explicit deferral markers and what that ratio says about workshop coverage; whether ADR-016 ended up landing this session or being deferred.
7. **`docs/retrospectives/README.md`** — add retro entry under `Multi-artifact retros (root)`.

### Definition of done

- `docs/context-map/README.md` committed with all 6 named edges authored or explicitly deferred.
- Vision doc bumped to v0.4 with cross-reference and Document History entry.
- ADR-016 decision made and acted on (either authored or deferred with reasoning recorded in the retro).
- Prompt + index entry bundled commit.
- Retro + index entry committed.
- Bundled PR opened with all changes.

---

## Decisions to flag during the session

1. **ADR-016 ("Context Map as Living Artifact") — land it or defer?** Lean: defer until the first real update lands (W003 forces the cadence rule into empirical territory). The artifact's existence is what motivates the rule; codifying the rule before the rule has been exercised is premature.

2. **Diagram format: Mermaid vs. PlantUML.** Lean: Mermaid for in-repo legibility on GitHub (renders natively; PlantUML requires server-side rendering or a plugin). PlantUML is the acceptable second choice if Mermaid's edge-labeling expressiveness turns out to be insufficient for the seven-pattern DDD vocabulary.

3. **Whether to name the Trips → Dispatch reverse edge as "conformist" or "customer-supplier (reversed)".** Lean: conformist, because Dispatch consumes the four distinct terminal events (`trips.trip-completed`, `trips.trip-cancelled-by-rider`, `trips.trip-cancelled-by-driver`, `trips.trip-abandoned-as-no-show`) and maps them to its own `OutboundRideTerminationStatus` enum per W002 §6.9's override of W001 §5.12. Dispatch shapes its model to Trips' published language without negotiation — that is the conformist pattern. Worth surfacing for sign-off because the same BC pair (Dispatch, Trips) carries different patterns in each direction; the asymmetry is the point.

4. **How to label edges whose pattern is pending a workshop.** Three candidates: (a) draw the edge with a `?` label, (b) draw the edge with the *expected* pattern in italics or a dashed line, (c) omit the edge entirely until its workshop lands. Lean: (b) — dashed line, expected pattern in italics, with a per-edge prose paragraph in the "Pending workshops" subsection naming what the workshop is expected to resolve. This is the explicit-deferrals discipline applied at the diagram layer.

5. **Whether to name "shared canonical identifier" as a *shared kernel* edge between every participating BC.** Lean: **no**. ADR-013 explicitly carries the value, not the type — the field names are BC-owned (`rideRequestId` in Dispatch, `tripId` in Trips, etc.) and only the wire-format value is shared. Naming this a shared kernel would over-claim the coupling. Note it as a cross-cutting *value-flow primitive* in the artifact's framing prose, not as an edge pattern.

---

## Out of scope

- **Service topology decisions.** The context map names *bounded contexts*, not deployable services. The 11-BCs-to-6–8-services collapsing question (vision doc §Open Questions) is unaffected by this session.
- **Re-litigating ADR-006, ADR-013, or ADR-014.** The Identity ACL stance, the shared-identifier stance, and the ASB topic naming stance are committed. The context map *names and cross-references*, does not re-decide. If authoring surfaces a tension with an existing ADR, flag it for a follow-up session — do not modify the ADR in this PR.
- **New ADR authorship beyond ADR-016.** If the context-map authoring surfaces new ADR candidates (it might — e.g., a candidate for "how the context map gets updated when a workshop runs"), capture them as parking-lot items in the artifact or retro, not as in-session ADR drafts.
- **Workshop authorship.** The Identity, Pricing, Payments, Ratings, Telemetry, Onboarding, Rider Profile, Driver Profile, Operations workshops are all separate sessions. The context map defers explicitly on their relationship patterns rather than pre-empting their workshop output.
- **Spec-delta methodology refinement.** That is Session A's job per the handoff doc. This session is the last session in CritterCab to run *before* the spec-delta convention exists. Its retro should note that as a baseline data point.
- **Narrative authorship or revision.** Narratives 001 and 002 stand as-is; their forward-constraint outputs feed this artifact but are not modified by it.
- **Vision-doc restructuring beyond the cross-reference + version bump.** The §Methodology section's bullet on "DDD strategic design" gets the cross-reference; no other vision-doc edits.

---

## Memory inheritance

The applicable feedback memories carry into this session via `MEMORY.md`. No restatement needed:

- **Communication depth + ubiquitous language + leaning opinions** — relevant throughout; pre-author leans on every edge's relationship pattern; surface the ones with genuine choice (especially the Trips → Dispatch conformist call) for sign-off.
- **BC-owned enums** — relevant for naming the canonical-identifier as value-flow-not-shared-kernel (decision #5 above).
- **Critter Stack primitives** — relevant for cross-references to ASB (Wolverine Azure Service Bus transport), Marten (event store for projection-driven translation slices), etc.
- **Explicit deferrals during artifact authoring** — load-bearing for this session. Six of eleven BCs lack workshops; their edges must be explicitly deferred with named expected-resolution triggers, not silently omitted.
- **Proactive projection proposals** — relevant if any translation-slice cross-reference surfaces a projection that hasn't been pinned yet (e.g., the `CrossBcRideTimeline` projection ADR-013 mentions speculatively). Propose with audience + shape + feeders + status even when deferring.
- **Keep READMEs current alongside session work** — load-bearing. Vision doc cross-reference + version bump lands in the same PR as the artifact; prompts README + retros README index entries land too.
- **No Claude attribution on commits or PRs** — relevant at commit/PR time. Omit `Co-Authored-By: Claude` trailers and "Generated with Claude Code" PR footers.
- **Static endpoints, Alba-first tests; validation at HTTP boundary** — not directly applicable (no code in this session) but applies if any edge's tactical-implementation cross-reference touches HTTP-boundary work.
- **Prune textureless detail in narrative prose** — applies to per-edge prose. Each edge's rationale paragraph should be tight; no atmospheric flavor.
- **Driver-mode when user signals a mental block** — applies to non-durable choices during the per-edge walk (edge order, diagram syntax choices, label abbreviation conventions). Keep sign-off discipline for the relationship-pattern calls themselves.

---

## Starting move

When the new session begins:

1. **Read the orientation files in order** (§Orientation files above).
2. **Confirm scope** — one bundled PR for the foundation artifact, vision doc cross-reference, optional ADR-016, prompt, and retro. Validate with the user; surface any movement before authoring.
3. **Settle the ADR-016 question** at session start. Lean: defer with a parking-lot note in the retro and a follow-up trigger ("revisit when W003 lands and forces the first update"). If user prefers to author now, the ADR slots into the per-edge walk's tail end after all edges are authored.
4. **Walk edge #1 (Dispatch → Trips)** first. Surface the customer-supplier + published-language lean with citations to W001 §5.10's `RideAssigned`, ADR-013's canonical-ID handoff, ADR-014's `dispatch.ride-assigned` topic. Get sign-off on the relationship pattern lean before drafting the full edge prose. Author the prose; add the diagram edge; sign-off; move to edge #2.
5. **Walk edges #2 through #6** per the order in §Working pattern. Each gets surface-the-lean → sign-off → prose-and-diagram-edit → sign-off → next.
6. **Compose the artifact's framing prose** (intro paragraph, update-cadence subsection, pending-workshops subsection) after all edges are authored.
7. **Bump vision doc to v0.4** with cross-reference and Document History entry.
8. **Compose the retro** per the retrospectives README, with the four specific framing questions named in deliverable #6.
9. **Open bundled PR.** Title: descriptive, naming the context-map foundation and the vision-doc cross-reference. Body: per-edge summary, ADR-016 disposition, test plan (read-through checklist for each edge, diagram-renders-correctly check).

Don't batch the whole session into one output. Per-edge walks are interactive — one edge at a time, sign-off after each relationship-pattern call. Total session estimate: 60–90 minutes.
