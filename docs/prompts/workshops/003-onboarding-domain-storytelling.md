# Prompt — Pilot Domain Storytelling against the Onboarding BC

| Field | Value |
|---|---|
| **Status** | Authored (not yet run); session pending |
| **Authored** | 2026-05-27 |
| **Target artifact** | `docs/workshops/003-onboarding-domain-story.md` (new — first Domain Storytelling artifact in CritterCab) |
| **Source-of-truth dependencies** | [`docs/vision/README.md`](../../vision/README.md) §Tentative Bounded Contexts → Onboarding and §Goals → Secondary #4; [`docs/context-map/README.md`](../../context-map/README.md) (Onboarding's deferred edges); [`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) (committed Identity boundary); [`docs/research/domain-discovery-playbook.md`](../../research/domain-discovery-playbook.md) (Nick Tune facilitation vocabulary); Hofer & Schwentner, *Domain Storytelling* (Addison-Wesley 2021) — notation reference |
| **Workflow position** | First Domain Storytelling exercise in CritterCab. First design-phase artifact for the Onboarding BC. Pilots the methodology committed to in vision doc v0.1 §Secondary Goal #4 ("Pilot Domain Storytelling") but not yet exercised in ~13 months of project history. Returns to design work from a 7-PR tidy/research stretch (PRs since #21 have been CI tidy, README refresh, Event Modeling research, ai-skills sync, Claude Code agents, ai-skills companion naming, pipeline export template), satisfying the design-return cadence rule. |

---

## Spec delta

This session is **spec-creating**, not spec-amending. The closure-loop convention applies here as a creation event:

- **No existing canonical spec is amended.** Onboarding has no workshop, no narrative, no event model. This session opens the BC's spec layer for the first time.
- **`docs/workshops/003-onboarding-domain-story.md` is created** — first Domain Storytelling artifact in the project. Establishes notation conventions (Hofer/Schwentner pictogram set adapted to markdown), granularity / viewpoint / purity choices, and story-scope decisions for future DS sessions to follow.
- **`docs/vision/README.md` v0.5 entry** — retires the "Pilot Domain Storytelling" goal from open-aspiration status. The history entry's content is decided at session close based on retro outcome: either marked **Exercised** (DS earns a permanent slot in the design-phase toolkit) or **Tried once, retired** (the experiment did not pay off and the methodology is not adopted). The contingency is itself a small precedent for contingent-deliverable prompts.
- **`docs/context-map/README.md` may gain a Document History entry** *if applicable* — only if Phase 4 findings surface a boundary realignment between Onboarding / Identity / Driver Profile that affects an already-named edge. No context-map impact is a fine outcome to land honestly.

---

## Framing

Three reasons to run this session now:

1. **Pilot a methodology we committed to and haven't exercised.** Vision doc v0.1 (2026-04-21) lists Domain Storytelling as a secondary goal. CLAUDE.md acknowledges it remains unexercised. Thirteen months of open aspiration is precisely the failure mode the spec-delta closure-loop discipline is supposed to prevent. Run it or formally retire it; this session forces the call.
2. **Lower the risk on the eventual Onboarding event model.** Onboarding has multiple human actors with overlapping-but-not-identical vocabularies (applicant, candidate, partner, driver-partner, driver, vetted-driver, suspended-driver — when does each label apply?). Eliciting that language before naming events in a future Workshop 004 prevents freezing ambiguity into event names. The Onboarding / Identity / Driver Profile seam in particular is murky and benefits from bottom-up boundary discovery before further top-down ADR work.
3. **Test the vision doc's BC split.** CritterCab's eleven BCs were named in v0.1 top-down. DS lassos emerge bottom-up from observed language shifts in the story. If the lassos don't match the named BCs at the Onboarding seam, that's a finding the context map and the eventual Onboarding event model both need before they can be authored confidently.

This session is **methodology-piloting plus design-artifact creation in one PR.** The bundling is a minor stretch of one-prompt-one-PR but defensible because the artifact *is* the piloting evidence; separating them produces two PRs whose only relationship is "this PR's existence proves that PR's session ran." The bundling is explicit and named here so future readers do not take it as precedent for unrelated session bundling.

---

## Goal

Run a Domain Storytelling session against the Onboarding BC, produce one captured artifact covering driver-vetting from application through approval (happy path plus two failure paths), and produce enough methodology notes to decide whether DS earns a permanent slot in CritterCab's design phase going forward.

---

## Working pattern (solo + AI adaptation)

DS is conventionally a multi-stakeholder workshop technique. CritterCab is solo (Erik) plus AI (Claude). The session adapts DS to this shape; the adaptation is itself part of what the session pilots, and the retro records whether the adaptation worked or needs convention changes.

**Adaptation premises:**

- **User plays the domain expert.** Informed by the Phase 0 research note (see below) and general familiarity with consumer-facing platform onboarding patterns. Not pretending to be a real ops insider — the goal is plausible-domain, not field-research-grade.
- **Claude facilitates and captures.** Asks DS-canonical questions: who acts? what work object is passed? in what sequence? what happens when X is rejected? what's the actor's word for this thing? Captures each story as a numbered sequence in markdown with actor labels, work-object labels, and present-tense activity verbs.
- **Markdown notation** rather than diagram tooling. Sequence-numbered list per story, with explicit actor / verb / work object / target. Lassos rendered as section headings or callout boxes. The choice keeps the artifact in-repo and reviewable inline; future sessions may evaluate whether to graduate to egon.io or similar.

**Phases run in order, with sign-off per phase:**

- **Phase 0 — Research note authoring.** Claude drafts `docs/research/ride-sharing-driver-onboarding-domain-note.md` covering industry baselines, vocabulary candidates, background-check patterns, document-verification patterns, edge cases worth probing, and boundary candidates. User reads, corrects any claims they do not buy, accepts. The note is **frozen** before Phase 1 begins — a finding that the note got something wrong becomes a retro entry rather than an in-flight edit.
- **Phase 1 — Story 1 capture: happy path.** Applicant submits documents, passes background check, becomes an active driver. Capture, replay, sign off.
- **Phase 2 — Story 2 capture: document-rejection path.** One document fails verification, applicant re-uploads, the application eventually proceeds.
- **Phase 3 — Story 3 capture: background-check-failure path.** Terminal rejection. What happens to partial application data, how the actor is notified, whether reapplication is possible.
- **Phase 4 — Vocabulary disambiguation pass.** Replay all three diagrams together; flag every noun with more than one candidate name (synonyms to collapse) and every name with more than one candidate referent (homonyms to split). Resolve in-session or flag as a finding for Workshop 004.
- **Phase 5 — Findings synthesis.** Author the artifact's footer: vocabulary findings, BC-boundary findings (do the lassos match the named BCs?), open questions for Workshop 004.
- **Phase 6 — Vision doc v0.5 bump.** Decide Exercised vs. Retired based on Phase 5 findings plus session-runner reading of whether the technique paid its keep. Author the document-history entry accordingly.
- **Phase 7 — Retro + index updates.** Standard close-out. Retro includes the methodology-specific "Solo-DS adaptation" subsection answering the questions named below.

Sign-off discipline: per phase. Each phase confirms before the next starts. No silent rollovers.

---

## Orientation files (read in order)

1. **[`docs/vision/README.md`](../../vision/README.md) §Tentative Bounded Contexts → Onboarding and §Goals → Secondary #4** — the v0.1 top-down assumption to test and the methodology commitment to either exercise or retire.
2. **[`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md)** — what already belongs to Identity. The Onboarding / Identity boundary is partly committed; DS surfaces dependencies but should not try to relitigate ADR-006.
3. **[`docs/context-map/README.md`](../../context-map/README.md)** — Onboarding's relationships are currently deferred. Pre-reading clarifies what the session can amend vs. what it can only flag for a future context-map session.
4. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §1–§3** (intro sections only) — for voice and structure consistency in the new workshop artifact. The DS artifact's body differs from EM's, but framing prose should feel like a sibling.
5. **[`docs/research/domain-discovery-playbook.md`](../../research/domain-discovery-playbook.md)** — Nick Tune facilitation vocabulary. Part 3 (Make Scale Explicit) and Part 4 (Diverse Question Formats) both apply during capture. Worth a pre-session reread even though the playbook lives in `docs/research/`.
6. **The Phase 0 research note** — read after authoring, before Phase 1 begins. Comes online during the session itself rather than as a pre-session prerequisite.

If physical access to Hofer & Schwentner's *Domain Storytelling* is unavailable, [egon.io](https://egon.io) documentation covers the notation adequately for a first session. The primitive set is tiny: actor, work object, activity (numbered arrow with verb), annotation, group (lasso).

---

## Deliverable plan

| File | Status | Purpose |
|---|---|---|
| `docs/research/ride-sharing-driver-onboarding-domain-note.md` | New | Phase 0 grounding material. Explicitly non-normative (lives in `docs/research/`, not `docs/workshops/` or `docs/skills/`). |
| `docs/research/README.md` | Edit | New index entry for the research note. |
| `docs/workshops/003-onboarding-domain-story.md` | New | The DS artifact. Three stories + vocabulary findings + BC-boundary findings + open questions for Workshop 004. |
| `docs/workshops/README.md` | Edit | New index entry. Brief callout that this is the first DS artifact (distinct from EM artifacts in the same directory). |
| `docs/vision/README.md` | Edit | v0.5 document-history bump. Content (Exercised or Retired) decided at session close. |
| `docs/prompts/workshops/003-onboarding-domain-storytelling.md` | Pre-existing | This prompt — landed in the prior small PR that authored this file. |
| `docs/prompts/README.md` | Pre-existing | This prompt's index entry — landed in the prior small PR. |
| `docs/retrospectives/workshops/003-onboarding-domain-storytelling.md` | New | Retro. Includes the methodology-specific "Solo-DS adaptation" subsection. First entry in `retrospectives/workshops/`. |
| `docs/retrospectives/README.md` | Edit | New index entry. |
| `docs/context-map/README.md` | Edit *(conditional)* | Document-history entry if Phase 4 findings warrant an amendment to a named edge. Skipped if not. |

---

## Out of scope

- **Event Modeling for Onboarding.** A future Workshop 004 session. DS feeds it; does not substitute for it.
- **ADR authorship for any boundary realignment surfaced.** If DS reveals that the Onboarding / Identity / Driver Profile split needs adjusting, flag it in the retro and queue an ADR session for a future PR. Do not author the ADR inside this PR.
- **Code.** No `src/` or `tests/` changes.
- **Updates to `docs/rules/structural-constraints.md`.** Layer 2 (ubiquitous language) and Layer 3 (code conventions) are overdue per a separate observation, but their authoring is its own session.
- **Onboarding narrative authoring.** The DS artifact feeds the eventual narrative; does not substitute for it.
- **Multi-BC DS expansion.** This session covers Onboarding only. Operations and the Identity / Riders / Drivers seam are deferred to future DS sessions if-and-only-if this pilot earns a continued role.
- **Promoting the research note's claims to canonical project knowledge.** The note is non-normative grounding material. Any of its findings that should become spec do so by appearing in the DS artifact (Phase 5 findings) or in a future Workshop 004 — never by elevating the note itself.
- **Cross-artifact tidy.** Out-of-scope per the general no-opportunistic-edits rule.

---

## Methodology questions to resolve in the retro

The retro's "Solo-DS adaptation" subsection answers these. They do not need answers up front; they are the session's reflective output.

1. Did the solo + AI adaptation produce usable output, or did it collapse into the user describing what the system does (pure-system-viewpoint drift)?
2. Did the three-story format surface vocabulary divergence the user had not seen, or did it confirm what the vision doc already implied?
3. Did the BC-boundary findings warrant a context-map amendment, or did the existing edges hold?
4. Should DS notation conventions for CritterCab be codified in a skill file or referenced as a recurring pattern, or does the artifact's preamble suffice for now?
5. Does running DS for Onboarding (a vocabulary-rich BC) generalize to a future session for Operations (also vocabulary-rich), or is the technique's value specific to onboarding-shaped domains?
6. Does the research-note-as-Phase-0 pattern survive — was the note actually useful to the user during capture, or was it overhead?

The answers shape:

- Whether DS earns a permanent slot in CritterCab's design-phase toolkit (the vision doc Phase 6 decision).
- Whether a future "solo Domain Storytelling" skill file is warranted.
- Whether the research-note-as-Phase-0 pattern is reusable for other methodology-piloting sessions.
