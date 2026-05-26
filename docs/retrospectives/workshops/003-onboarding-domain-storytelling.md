# Retrospective — Workshop 003 Onboarding Domain Storytelling

| Field | Value |
|---|---|
| **Triggering prompt** | [`docs/prompts/workshops/003-onboarding-domain-storytelling.md`](../../prompts/workshops/003-onboarding-domain-storytelling.md) |
| **Status** | Complete |
| **Date** | 2026-05-26 |
| **Output artifacts** | [`docs/workshops/003-onboarding-domain-story.md`](../../workshops/003-onboarding-domain-story.md) (new — first DS artifact); [`docs/research/ride-sharing-driver-onboarding-domain-note.md`](../../research/ride-sharing-driver-onboarding-domain-note.md) (new — Phase 0 grounding material); [`docs/vision/README.md`](../../vision/README.md) (v0.4 → v0.5: DS marked Exercised + one new open question); [`docs/context-map/README.md`](../../context-map/README.md) (v0.1 → v0.2: edge #6 prose amendment + Pending workshops Onboarding row update); [`docs/research/README.md`](../../research/README.md), [`docs/workshops/README.md`](../../workshops/README.md), [`docs/retrospectives/README.md`](./..//README.md) (index entries) |
| **One-line outcome** | First Domain Storytelling exercise in CritterCab produced a substantive Onboarding spec artifact, methodology pilot landed as **Exercised**, and the context-map edge #6 amendment empirically validated the [§Update cadence](../../context-map/README.md#update-cadence) convention on its first real exercise. |

---

## Framing

This session executed the methodology-piloting-plus-design-artifact-creation bundle the prompt deliberately framed as "one prompt, one session, one PR" with the bundling rationale named upfront. The artifact *is* the piloting evidence; separating them would have produced two PRs whose only relationship was "this PR's existence proves that PR's session ran."

It is also the **first DS exercise in CritterCab** after ~13 months of the methodology being listed as an open vision-doc aspiration. The session opened a methodology question CritterCab had been avoiding and forced the call: Exercised or Retired. The retrospective records what made the call land where it did.

---

## Outcome summary

- **One DS artifact authored** with three stories (happy path; document-rejection recoverable; background-check terminal-rejection) plus §Findings synthesis (vocabulary findings, BC-boundary findings, 19 open questions for W004).
- **One research note authored** as Phase 0 grounding material — non-normative, frozen at session start; pattern itself flagged for evaluation in the retro.
- **Methodology pilot decision: Exercised.** DS committed as a permanent design-phase technique alongside Event Modeling. Vision doc bumped to v0.5.
- **Context-map amendment landed in the same PR** per the W003 amendment to edge #6 (approval-terminal-gating refinement) plus Pending-workshops row update. First exercise of the context-map's [§Update cadence](../../context-map/README.md#update-cadence) convention.
- **One vision-doc open question escalated** (OQ-14 → vision-doc-level: suspension / reinstatement / deactivation BC placement).
- **Three notation devices established** for future DS sessions (repeat-aggregation, time-passage, reference-block) as CritterCab adaptations of canonical DS for markdown-as-substrate and solo + AI as participant set.

---

## What worked

- **Per-phase sign-off discipline.** Sign-off operated at the phase boundary (8 sign-off moments across the session); capture iterated freely within each phase. Prevented late-session churn — the artifact only ever reflected agreed-upon content. Mirrors the per-edge cadence from [context-map-foundation](../context-map-foundation.md) and the per-slice cadence from W001 / W002.

- **Explicit-lean discipline.** Each capture batch ended with 1–3 focused questions, each accompanied by a leaning recommendation. The user signed off, redirected, or escalated. The pattern avoided both the consultant failure mode (Claude pre-authors entire stories for review) and the over-facilitated failure mode (Claude asks open-ended questions with no proposed direction). Across the 3-story walk, the user redirected zero leans — but the discipline of *naming* the leans surfaced choices that might otherwise have been silently committed.

- **The three-story-with-deliberate-variation structure.** Story 1 (happy path) established vocabulary and BC lassos. Story 2 (document-rejection recoverable) exercised the per-document state machine without adding new actors. Story 3 (background-check terminal-rejection) added one new actor type (Onboarding adjudicator) and one new work-object type (adjudication case), and was the only story to surface vocabulary collisions across noun/verb categories (H4 "review" / "under-review" / "REVIEWS" collision). Each story earned its keep on a different axis.

- **Phase 0 research note as session prerequisite.** Pre-authored grounding material (~280 lines) gave the user a stable reference for vocabulary candidates, vendor patterns, and boundary candidates — and committed those references *before* Phase 1 began, so they didn't get re-litigated mid-capture. Frozen-at-session-start preserved the note's stability as ground truth even when DS findings contradicted it (no contradictions surfaced in this session, but the discipline was in place).

- **Reference-block convention (Story 2 steps 20–28).** Compressed the BG-check / approval / activation arc that was identical to Story 1 into a single citation step. Avoided ~9 steps of duplicative capture. The convention has the cost of a hidden cross-story dependency (if Story 1's referenced steps change, Story 2's reference inherits the change), but the cost is acceptable for stories explicitly framed as variations on a shared base.

- **Surface-and-flag for newly-discovered scope.** Three items surfaced during capture as user-decision questions with leans rather than silent inclusion or silent omission: V6 (under-review → under-verification rename), OQ-14 (suspension BC placement escalation), B2 (approval-terminal-gating finding warranting context-map amendment). Mirrors the discipline established in [context-map-foundation](../context-map-foundation.md). Pattern transfers cleanly across artifact types.

- **The context-map amendment ran cleanly in the same PR.** The §Update cadence convention's first real exercise was the [W003 amendment to edge #6](../../context-map/README.md#6-intra-actor-topology-deferred). The amendment was prose-only (no diagram changes); the relationship-pattern label held; the trigger to revisit ADR-016 is now empirically reached.

---

## What was harder than expected

- **The vocabulary yield-per-story curve did not decrease monotonically.** Stories 1 and 2 produced fewer new vocabulary items than expected; Story 3 produced *more* than Stories 1+2 combined (1 new actor type, 1 new work-object type, 4 new verbs). The implication: failure paths that introduce new actor types are vocabulary-rich; failure paths that only re-sequence existing actors are vocabulary-thin. Three stories may undersample vocabulary-rich BCs where additional failure paths (e.g., suspension, reactivation, mid-trip-incident escalation) would surface meaningful new content. Worth keeping in mind for future DS sessions.

- **Several batches were resolved with "go with the leans"** rather than genuine pushback. This is partly a positive signal (the leans were calibrated well) and partly a risk signal (the user may have been operating in lean-confirmation mode rather than independent-domain-expert mode). Hard to distinguish without a multi-participant DS session as a control. The methodology questions §Q1 below digs into this directly.

- **Step 26's modeling of applicant non-action as a step** felt strained as a DS primitive. "DOES NOT DISPUTE within the window" is regulatorily significant and load-bearing for step 27 to fire, but it bends DS's positive-activity convention. The right modeling is probably Bruun-style temporal automation (per [W001 §5.7](../../workshops/001-dispatch-event-model.md) — "window expired" fires step 27), but that pattern belongs to Event Modeling, not DS. Captured as OQ-6 for W004; flagged here as a notation-limitation insight.

- **The two notation devices in Story 1 (repeat-aggregation at step 13; time-passage at step 17) and the third in Story 2 (reference-block at steps 20–28) accumulated faster than expected** — three custom adaptations across three stories. None individually felt like a stretch, but collectively they make the artifact's notation conventions section (§3.2) substantial. The cost is that future DS sessions inherit the conventions; the benefit is that they don't have to re-invent them. Worth flagging that markdown-as-DS-substrate genuinely *requires* these adaptations.

- **The Phase 5 findings synthesis was longer than expected** (3 subsections, 19 numbered open questions, 7 BC findings, 8 W004-flagged vocabulary items, 1 cross-cutting pattern). The structure held but the volume was greater than the prompt's framing implied. Worth flagging for future Phase-5-style synthesis sessions: budget more time.

- **A mid-session API error interrupted Phase 7's first artifact write.** The session recovered cleanly (single retry) but the disruption highlighted that a long session producing a large artifact carries some non-zero risk of unrecoverable interruption. Mitigation for future sessions: consider writing the artifact incrementally per phase rather than at session close. The tradeoff is fragmentation of the artifact's voice; the gain is interruption resilience.

---

## Methodology refinements that emerged

- **Three notation devices for markdown-as-DS-substrate** are now established (repeat-aggregation, time-passage, reference-block). Future CritterCab DS sessions inherit them; the W003 artifact's §3.2 is the canonical reference. If a fourth device emerges during a future DS session, that session's preamble should establish it explicitly and update the convention.

- **The Phase 0 research-note pattern survives as reusable.** The note's job (grounding the session with industry-typical baselines and vocabulary / boundary candidates) is replicable for other vocabulary-rich BCs. Recommended for future DS sessions targeting BCs the user has not worked in professionally (Operations, Trust & Safety would be likely candidates).

- **Explicit-lean discipline as a solo + AI adaptation primitive.** The pattern — capture batch → 1–3 focused questions → each with a lean → user confirms / redirects / escalates — is the methodology innovation that made the solo + AI adaptation work. Not specific to DS; transferable to any solo + AI session where the AI is facilitating and the user is the domain authority.

- **Methodology-pilot sessions warrant a binary keep/retire decision at session close.** Vision doc v0.5's entry sets the precedent. The discipline prevents methodology aspirations from accumulating indefinitely as open vision-doc goals; each gets a pilot session, gets evaluated against a defined yield, gets a committed answer. Future methodology pilots (specification-by-example, EventStorming workshops, bounded-context-canvas authoring) follow this pattern.

- **Context-map updates in the workshop's PR is now empirically validated.** The [§Update cadence](../../context-map/README.md#update-cadence) trigger to revisit ADR-016 has fired. The cadence ran cleanly on its first real exercise. The ADR-016 question shifts from "what should the discipline say?" to "is this worth committing as a project-wide discipline?" — answerable now in a future ADR-authoring session.

---

## Solo-DS adaptation — answering the prompt's six methodology questions

The prompt named six specific questions to capture in this retro's methodology subsection. Answering each directly:

### Q1. Did the solo + AI adaptation produce usable output, or did it collapse into the user describing what the system does (pure-system-viewpoint drift)?

**Produced usable output, with one structural caveat.** The artifact's three stories carry actor-first vocabulary throughout — actors *submit*, *upload*, *re-upload*, *click*, *dispute* — and the system-only steps (e.g., Story 1 step 8 "Onboarding RECORDS personal-info section as complete") are scaffold between actor activities rather than the substance. The pure-system-viewpoint failure mode did not materialize.

The structural caveat: the user did not redirect any of the ~20 leans Claude proposed across the session. That is consistent with the leans being calibrated well (the working hypothesis) but is also consistent with the user operating in lean-confirmation rather than independent-domain-expert mode (the alternative). A multi-participant DS session, or a solo session with deliberate red-team prompts, would be needed to distinguish empirically. Worth keeping in mind for any future methodology where the AI is doing substantial framing.

### Q2. Did the three-story format surface vocabulary divergence the user had not seen, or did it confirm what the vision doc already implied?

**Surfaced divergence not visible from the vision doc alone.** Specifically:

- **H4** (the "review" / "under-review" / "REVIEWS" three-way collision) was invisible until Story 3's adjudicator action introduced REVIEWS as a verb adjacent to the document-state noun. Pure vision-doc reading would not have caught this; only the second-and-third-story walk did.
- **H5** (per-document "rejected" vs. application-terminal "rejected") was invisible until Stories 2 and 3 were walked back-to-back. The vision doc uses "rejected" in only one sense; the two-scope distinction emerged from the stories.
- **The "Onboarding-as-multi-vendor-ACL-aggregator" cross-cutting pattern** was invisible until three vendor boundaries (OIDC provider, document-verification vendor, background-check vendor) had been laid alongside each other in the stories. Reading any one boundary in isolation would not have surfaced the pattern.
- **B2 (the approval-terminal-gated DP handoff)** was invisible until Story 3 demonstrated by *absence* that the cross-BC publication exists for the approval terminal but not the rejection terminal. A single-story or vision-doc reading would have presumed symmetry.

Vocabulary that DS *confirmed* rather than discovered: the prospective driver → applicant → driver lifecycle, the "no rejected applicant sub-state" finding, and the deliberate avoidance of "profile" for application data. These confirmations are valuable but were not the high-yield findings; the divergence findings were.

### Q3. Did the BC-boundary findings warrant a context-map amendment, or did the existing edges hold?

**Yes — one prose amendment landed in the same PR.** Edge #6's prose now carries the approval-terminal-gating refinement per B2. The relationship pattern label held (expected CS pending W004); no diagram changes were needed. The Pending-workshops row for Onboarding also updated to note that W003 (DS) has shipped while W004 (event model) remains pending.

The amendment is small but specific. The pattern (DS produces an actionable context-map refinement worth amending in the same PR) is now reusable for future DS sessions.

### Q4. Should DS notation conventions for CritterCab be codified in a skill file or referenced as a recurring pattern, or does the artifact's preamble suffice for now?

**Preamble suffices for now; codification deferred until at least one more DS session has exercised the conventions.** The W003 artifact's §3 establishes the notation conventions (markdown sequence format, lasso convention, three notation devices). A future DS session targeting another vocabulary-rich BC (likely Operations, possibly Trust & Safety or Rider Profile) should *reference* W003's §3 rather than re-inventing the conventions. If a third DS session lands and the conventions hold without modification, that's the trigger to lift them into a skill file (probable location: `docs/skills/methodology/domain-storytelling/SKILL.md`).

The discipline mirrors the [DEBT.md tidy convention's source-of-truth precedence](../../skills/DEBT.md) — codify after the pattern has been exercised, not before.

### Q5. Does running DS for Onboarding (a vocabulary-rich BC) generalize to a future session for Operations (also vocabulary-rich), or is the technique's value specific to onboarding-shaped domains?

**Working hypothesis: generalizes well, especially to vocabulary-rich BCs with multi-actor workflows and human-in-the-loop activities.** Onboarding's value-yield drivers were (a) multiple actor types including humans, (b) vendor boundaries with foreign vocabulary, (c) failure paths with different terminal shapes, (d) a vocabulary lifecycle (prospective driver → applicant → driver) with transition moments worth naming. Operations shares all four of these properties (multiple actor types including operators / dispatchers / support staff; multiple inbound and outbound BCs; failure paths during incident response; vocabulary lifecycle around incidents and tickets).

DS value would likely be *lower* for narrow, mechanical BCs (e.g., a pure projection-fed Telemetry BC where actors are systems and the work objects are GPS pings). Worth flagging that DS is not universally applicable; the methodology section in the vision doc should reflect this (probably via future refinement, not in this session's bump).

### Q6. Does the research-note-as-Phase-0 pattern survive — was the note actually useful to the user during capture, or was it overhead?

**Useful, but the value was front-loaded.** The note's heaviest use was during Phase 1 framing (where the user committed to the "prospective driver → applicant → driver" vocabulary lifecycle and the "interest signal" start point — both directly informed by the note's §3.1 and §2.1). By Phase 3 the note was background context rather than active reference.

The pattern's overhead is real (one phase of authoring before capture begins) but the alternative (DS session starting cold without industry-baseline grounding) would have produced thinner Phase 1 framing and likely a less coherent first story. The pattern survives as recommended for future methodology-piloting sessions in vocabulary-rich BCs. Lighter-weight grounding (a single-page note rather than ~280 lines) may suffice for BCs the user knows better professionally; the depth should scale to the user's pre-existing domain familiarity.

The note's *frozen-at-session-start* discipline was load-bearing: when Phase 4 surfaced findings (e.g., H4 rename) that would have prompted in-flight edits to the note, the freeze discipline kept the note stable and re-routed the finding to the DS artifact's §Findings. The discipline preserves the note's value as "what the user knew going in" — a baseline that the session's findings can be measured against.

---

## Outstanding items / next-session inputs

- **Workshop 004 — Onboarding event model** is the natural successor session. Inherits all 19 open questions from W003 §5.3 plus the V1–V8 vocabulary recommendations from §5.1.
- **Vision-doc OQ on suspension lifecycle BC placement** (escalated to v0.5 §Open Questions) waits for the suspension lifecycle to be first modeled — likely a future workshop after W004.
- **Codification of DS notation conventions** as a skill file deferred per Q4 above; trigger is "one more DS session exercises the conventions without modification."
- **ADR-016 (Context Map as Living Artifact)** trigger empirically reached per the [context-map §Update cadence](../../context-map/README.md#update-cadence) revisit condition — this session amended the artifact in the same PR and the cadence ran cleanly. The ADR question is now answerable in a future ADR-authoring session.
- **Methodology-pilot precedent for binary keep/retire at session close** now established; reusable for future methodology pilots (specification-by-example, EventStorming workshops, bounded-context-canvas authoring, etc.).

---

## Spec delta — landed?

**Yes — landed substantially as named.** The prompt's [§Spec delta](../../prompts/workshops/003-onboarding-domain-storytelling.md#spec-delta) named the session as spec-creating (not spec-amending) and predicted four specific landings:

| Prompt prediction | Landed? | Notes |
|---|---|---|
| `docs/workshops/003-onboarding-domain-story.md` created | ✅ | First DS artifact in CritterCab; establishes notation conventions for future sessions. |
| `docs/vision/README.md` v0.5 entry deciding Exercised vs. Retired | ✅ — **Exercised** | DS committed as a permanent design-phase technique. |
| `docs/context-map/README.md` Document History entry *if applicable* | ✅ — applicable | Edge #6 prose amendment per B2 finding; Pending-workshops Onboarding row update; v0.1 → v0.2 with full document-history entry. **First exercise of the [§Update cadence](../../context-map/README.md#update-cadence) convention.** |
| "No existing canonical spec amended" prediction | **Partial deviation** | The prompt predicted no existing-spec amendments; in practice the vision doc + context map both received same-PR amendments (an open question added to vision doc; an edge-prose refinement landed on context map). Both amendments are consistent with the prompt's "if applicable" qualifier on the context-map line and with the methodology-pilot bump's natural shape. The vision-doc *Open Questions* addition was not explicitly predicted in §Spec delta but is small and consistent with §Spec delta's overall framing. Worth flagging as a refinement: methodology-pilot sessions naturally produce a small open-question addition to the vision doc (the escalation pattern), and this should be incorporated into future methodology-pilot prompt templates. |

The one prompt-named deliverable that *didn't* materialize as a contingency: the prompt named the Phase 6 outcome as either "Exercised" or "Retired" but did not predict which would land. Phase 6 resolved Exercised based on §Phase 5 findings yield plus session-runner judgment, recorded in this retro's §Solo-DS adaptation Q1–Q6 above.

---

## Quantitative summary

| Metric | Count |
|---|---|
| Phases run with sign-off | 8 (Phase 0–7, no skips) |
| Stories captured | 3 (happy path; document-rejection recoverable; background-check terminal-rejection) |
| Total numbered steps across all stories | 23 + 28 + 28 = **79 numbered steps** |
| Explicitly walked steps across all stories | 23 + 19 + 11 = **53 walked steps** |
| Reference-block compressions | 1 (Story 2 steps 20–28) |
| Other notation devices used | 2 (repeat-aggregation, time-passage — each used in Story 1 and inherited by Stories 2, 3 via reference-block) |
| CritterCab BC lassos surfaced | 3 (Identity, Onboarding, Driver Profile) |
| External actors introduced | 2 (document-verification vendor, background-check vendor) |
| Human-reviewer actor types introduced | 1 (Onboarding adjudicator) — first manual-human actor in any CritterCab workshop |
| New work-object types introduced | 1 (adjudication case) |
| Activity verbs introduced | 20, clustering into 6 categories with no leftover |
| Vocabulary findings resolved in-session | 3 (R1, R2, "profile" avoidance) |
| Vocabulary findings flagged for W004 | 8 (V1–V8) |
| Cross-cutting patterns surfaced | 1 (Onboarding-as-multi-vendor-ACL-aggregator) |
| BC-boundary findings | 7 (B1–B7) |
| Context-map amendments landed in this PR | 1 (edge #6 prose + Pending-workshops row + Document History v0.2 entry) |
| Vision-doc open questions added | 1 (OQ-14 escalated: suspension lifecycle BC placement) |
| Open questions for Workshop 004 | 14 (OQ-1 through OQ-13 plus OQ-14 escalated; 5 additional OQ-15 through OQ-19 explicitly deferred to future sessions) |
| Total open questions (W004 + deferred) | **19** |
| Files authored | 3 (Phase 0 research note; workshop artifact; this retro) |
| Files amended | 5 (vision/README; context-map/README; research/README; workshops/README; retrospectives/README) |
| Methodology pilot decision | Exercised |
| Mid-session API errors | 1 (recovered cleanly on retry; flagged in §What was harder than expected) |
