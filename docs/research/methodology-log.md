# CritterCab Methodology Log

This file is an **append-only journal of cross-cutting methodology observations** that surface during sessions but don't fit cleanly inside any single session's retrospective. Each entry captures a pattern noticed *across* artifact layers, sessions, or methodology techniques — the kind of observation a per-session retro can't make because it would violate the retro's scope.

## What this file is

- A durable home for cross-cutting methodology learnings. Each entry is a small, dated observation paired with what it implies for future sessions.
- Append-only. Entries are not edited after writing; if an observation is later disconfirmed, a follow-up entry records the correction.
- Time-boxed pilot. Decision to keep, fold, or remove the file is revisited at narrative #2's close (per the methodology-log proposal during narrative 001's session, 2026-04-25).

## What this file is NOT

- A replacement for per-session retrospectives. Workshop §12 retros and narrative retrospectives stay layer-bounded and stay where they are.
- An ADR backlog. ADRs capture architectural decisions; this log captures methodological observations that may *eventually* harden into ADRs but typically don't.
- A vision doc surrogate. The vision doc captures *committed* methodology choices; this log captures *observed* patterns that may or may not become commitments.

## When to write an entry

Write an entry only when a session produces a cross-cutting observation that meets all three criteria:

1. **Spans** multiple artifact layers (e.g., a pattern visible in workshops *and* narratives) or multiple sessions.
2. **Wouldn't fit** inside that session's per-session retro without violating its scope.
3. **Predicts** something about how the methodology will or should evolve.

If no entry is warranted, no entry is written. Silence is fine; the file stays legitimate by being silent when there's nothing cross-cutting to say.

## Entry format

```
### Entry NNN — <one-line title> (YYYY-MM-DD)

**Trigger.** What session or artifact prompted the observation.

**Observation.** The cross-cutting pattern itself, in plain language.

**Implication.** What this predicts or changes for future sessions, and how a future entry would confirm or disconfirm it.
```

Numbers are zero-padded to three digits, mirroring narrative and workshop numbering.

---

## Entries

### Entry 001 — Conventions diversify across artifact layers as document layering matures (2026-04-25)

**Trigger.** Closing observation during narrative 001's authoring session ([`docs/narratives/001-rider-books-a-ride.md`](../narratives/001-rider-books-a-ride.md)). Three artifact layers received coordinated convention work in one session: the narrative file (Moments + Cast + Setting structure), the cumulative deferral aggregation (per-Moment + session-close pattern), and [`docs/narratives/README.md`](../narratives/README.md) (operational manual). Workshop 001 had previously consolidated all of *its* conventions inside the workshop document itself (§10 Parking Lot, §11 ADR Candidates, §12 Retrospective).

**Observation.** As CritterCab's document layering matures, conventions stop fitting inside any single artifact. Workshop 001 produced its conventions in one document because workshops were the only formal artifact at the time. Narrative 001 produced its conventions across three artifacts because the narrative file, the README, and a feedback memory entry all needed coordinated convention statements. The diversification is not accidental — each location captures the convention at the grain where it's operationally needed (per-narrative example, README-as-manual, durable-cross-session memory). Forcing all conventions into one location would have mis-shaped them.

**Implication.** Narrative #2 should *not* produce three layers of new convention work. The README is now the canonical reference; narrative #2's conventions will mostly be applied rather than defined, with only small README revisions. The convention-diversification cost is paid once per artifact type at first-use, not per session.

A future entry would confirm this if: (a) narrative #2 closes without significant README revisions and without surfacing methodology-grade observations of its own; or (b) the first prompt document, the first ADR for a new BC, or any other first-use-of-a-new-artifact-type session repeats the three-layer convention pattern observed here. A future entry would disconfirm this if: narrative #2 produces substantial convention churn despite inheriting the README, in which case the assumption that "convention cost is paid once per artifact type" is weaker than it appears today.

### Entry 002 — Command-pattern slice C/I/R weighting tracks protagonist's relation to the command (2026-05-04)

**Trigger.** Authoring narrative 002 ([`docs/narratives/002-driver-accepts-a-ride.md`](../narratives/002-driver-accepts-a-ride.md)). Workshop slice 5.5 (`OfferAccepted` + sibling revocation cascade) rendered in both narrative 001 (rider POV) and narrative 002 (driver POV) with strikingly different Context/Interaction/Response weighting despite being the same workshop slice. In narrative 001's Moment 5, Maya's Interaction was a thirteen-word sentence ("Eight seconds in, Dani Rivera... taps Accept on his phone") and her Response was the load-bearing paragraph. In narrative 002's Moment 2, Dani's Interaction is the load-bearing beat (he is the source of the `AcceptOffer` command) and the Response renders the system's atomic commit and downstream handoff.

**Observation.** Command-pattern slices render with different per-Moment weight depending on the protagonist's relation to the slice's command. Protagonist-as-commander expands the Interaction; protagonist-as-recipient expands the Response. The intrinsic shape of the slice (workshop-level command + event + view) does not determine the narrative-level weighting — the protagonist's vantage does. Methodology log entry 001 predicted that narrative 002 would produce minimal new convention work; that prediction held at the README/format-conventions level (one small extension; see entry 003) but underweighted observation yield (this entry plus entry 003). Worth recording as a calibration on entry 001 itself: convention churn and methodology-observation yield are different metrics; predicting one doesn't predict the other.

**Implication.** When authoring future narratives, calibrate per-Moment C/I/R weighting up front based on the protagonist's relation to each slice's command, not the slice's intrinsic shape. The pre-walk POV asymmetry sidebar (introduced in narrative 001's adjustment list) should explicitly note "is the protagonist a commander or recipient at this slice?" alongside its existing "directly experiences / observes / is unaware of" categorization. A future entry would confirm if narrative 003 (driver-decline, where Dani is again the commander of the `DeclineOffer` slice) renders Moment 1's Interaction as load-bearing without re-discovering the principle. A future entry would disconfirm if a future narrative finds a command-pattern slice where C/I/R weighting doesn't track the commander/recipient distinction cleanly.

### Entry 003 — Narratives have two fidelity layers; assumptions about un-modeled BCs live at the authorial-call layer (2026-05-04)

**Trigger.** Authoring narrative 002, Moment 2, second `Response.` paragraph ([`docs/narratives/002-driver-accepts-a-ride.md`](../narratives/002-driver-accepts-a-ride.md)). The prose renders Dani's trip-mode UI as carrying "Maya's name at the top." Workshop §5.10's `RideAssigned` carries `riderId` only — surfacing the rider's *name* to the driver-app post-acceptance assumes Trips' projection joins `riderId` to a rider-profile name on intake. The assumption is structurally reasonable but is not workshop-committed. The narrative format had no explicit convention for handling assumptions about un-modeled-BC behavior.

**Observation.** Narratives have two natural fidelity layers with different rules. *Locked prose* answers to narrator-omniscient voice fidelity: the narrative reads as a story; the narrator is omniscient about the system; meta-labels ("by assumption", "to be confirmed", "if BC X honors this") break the spell and don't belong. *Authorial calls* (captured during the proposal phase, surfaced in the retrospective) answer to workshop and methodology fidelity: self-aware decisions, deferrals, and assumptions about un-modeled-BC behavior live here. The two layers serve different fidelity contracts; mixing them — putting workshop-fidelity meta into the prose, or letting prose-fidelity flourish into the authorial calls — degrades both. As with entry 002, this observation calibrates entry 001 itself: a small README extension was made to `docs/narratives/README.md` (a "Two-layer fidelity" subsection), modestly disconfirming the entry-001 prediction that no README work would be needed.

**Implication.** Future narratives that render behavior depending on un-modeled BCs should adopt the two-layer convention from Moment 1 of authoring. Captured authorial-call assumptions become *forward-constraints* on the un-modeled BC's eventual workshop — the workshop must honor or override them. A future entry would confirm this if Trips' workshop, when authored, honors narrative 002's rider-name-surfacing forward-constraint (or explicitly overrides it with documented reasoning). A future entry would disconfirm if a future narrative reverts to inline meta-labels for un-modeled-BC assumptions, *or* if a future workshop ignores narrative-layer forward-constraints and re-decides the same questions.

### Entry 004 — Workshops forward-constraint other workshops; mid-sidebar research grounding is a legitimate move (2026-05-09)

**Trigger.** Workshop 002 ([`docs/workshops/002-trips-event-model.md`](../workshops/002-trips-event-model.md)) surfaced two cross-cutting observations: (a) slice 6.12's `DriverTripView` enrichment design forward-constraints Identity's eventual workshop to publish `RiderRegistered` and `RiderProfileUpdated` business events — the first instance of a workshop directly forward-constraining another workshop (vs. entry 003's narrative→workshop pattern); (b) the §3 aggregate-identity sidebar required mid-sidebar web search to ground first-state vocabulary (`Matched` chosen over `Intaken` and `Confirmed` based on Uber/Lyft/system-design-literature canonical naming) — a vocabulary-grounding move Workshop 001 §12.6 didn't explicitly anticipate.

**Observation.** Two patterns surfaced this session, both meeting entry 001's spans/wouldn't-fit-in-retro/predicts criteria:

*Pattern A — workshops forward-constraint other workshops directly.* Methodology log entry 003 framed forward-constraints as a *narrative→workshop* pattern. Workshop 002 surfaced a structurally identical pattern operating at a different layer: *workshop→workshop*. When workshop A's design says "BC B must publish event X" (Trips' slice 6.12 says Identity must publish `RiderRegistered`), that's a forward-constraint on B's eventual workshop. The two layers form a chain: narrative N forward-constrains workshop W; workshop W forward-constrains workshops W' and W'' as the design propagates. A separate observation: this also surfaced a *backward* forward-constraint — Workshop 002's slice 6.9 surfaced two enum gaps (`RIDER_NO_SHOW`, `ASSIGNMENT_COMPLETED_NORMALLY`) in Workshop 001 §5.12's preferred shape, generating a Workshop 001 *revision* requirement. So the chain is bidirectional in practice: forward-constraints flow forward to un-modeled BCs, and revision-constraints flow backward to already-modeled BCs.

*Pattern B — mid-sidebar research grounding for vocabulary work.* Workshop 001 §12.6 #2's "pre-workshop sidebar on aggregate identity" implicitly assumed the sidebar's vocabulary work was internal (decisions among the team's own framings). Workshop 002's first-state vocabulary work required *external* grounding — three internally-generated candidates (`Intaken`, `Confirmed`, `Matched`) were narrowed via web search of Uber/Lyft API conventions and system-design literature, which surfaced `Matched` as the industry-canonical term. The web search took one user-turn but materially improved vocabulary fidelity. This is a legitimate facilitation move that future workshops should adopt explicitly when industry conventions exist for the BC's domain.

As with entries 002 and 003, this observation calibrates entry 001 itself: Workshop 002's pre-walk plan didn't anticipate either pattern, but both surfaced organically and both produced reusable assets (§13b "Forward-Constraints Generated" section as a structural addition; mid-sidebar research as a cadence move).

**Implication.** Future workshops should:

1. **Add a §13b "Forward-Constraints Generated" section** consolidating outbound forward-constraints on un-modeled BCs' eventual workshops. Workshop 002 documented these inline at slice 6.12 but did not consolidate them; future workshops should surface them explicitly so the chain is auditable. Workshop 002's §13 captured *inbound* constraints; the symmetric outbound-section makes both directions visible.
2. **Capture revision-constraints on prior workshops as parking-lot items with disposition.** Workshop 002 §11 surfaced two Workshop 001 §5.12 enum-gap items; future workshops finding similar patterns should follow the same convention.
3. **Add "vocabulary scan against industry conventions" to the §12.6 #2 pre-walk sidebar.** Workshop 002's web search for ride-sharing API canonical naming should be a deliberate sidebar step, not a mid-sidebar improvisation. Adjustment: when the BC has industry analogues (ride-sharing, e-commerce, payments, etc.), the sidebar includes a 5-minute vocabulary scan against major industry implementations before locking the first-state event/state names.

A future entry would confirm pattern A if Identity's eventual workshop honors Workshop 002's `RiderRegistered`/`RiderProfileUpdated` forward-constraints (or explicitly overrides them), AND if Workshop 001 receives the §5.12 enum revision in a future revision PR. A future entry would disconfirm if forward-constraints from earlier workshops are systematically ignored or re-decided in later workshops, suggesting the workshop→workshop fidelity contract is weaker than narrative→workshop.

A future entry would confirm pattern B if subsequent workshops with industry analogues (Pricing, Payments) explicitly run vocabulary scans against industry conventions in their sidebars and surface fewer mid-walk vocabulary iterations. A future entry would disconfirm if vocabulary work continues to surface mid-walk despite the sidebar adjustment, suggesting the grounding move's value is local rather than systematic.

### Entry 005 — Domain Storytelling as upstream input materially raises Event-Modeling-artifact quality for vocabulary-rich BCs (2026-05-27)

**Trigger.** Workshop 004 ([`docs/workshops/004-onboarding-event-model.md`](../workshops/004-onboarding-event-model.md)) — CritterCab's first Event Modeling workshop to consume a Domain Storytelling artifact ([W003](../workshops/003-onboarding-domain-story.md)) as its primary upstream input, rather than the narratives W001 and W002 consumed. The prompt's methodology question Q1 asked whether DS-as-upstream produced a materially higher-quality EM artifact than narratives-only would have, comparing against W002's narrative-pair upstream.

**Observation.** DS-as-upstream front-loaded the vocabulary work that, in a narrative-fed workshop, surfaces mid-walk. W002's first-state vocabulary required three iterations plus a mid-sidebar web-research turn (`Intaken → Confirmed → Matched`); W004 had zero mid-walk vocabulary scrambles, because W003's §5.1 V1–V8 had already resolved every internal disambiguation and the pre-walk scan confirmed zero collisions against W001/W002. Beyond vocabulary: all 7 of W003's BC-boundary findings encoded as explicit slice dispositions with no fresh debate; all 14 W004-scoped open questions were answerable with leans traced directly to W003 findings; and W003's named cross-cutting pattern (multi-vendor ACL aggregator) gave W004 a ready-made architectural framing that shaped the modeling-pattern sidebar's "framings considered and rejected" reasoning. The vocabulary-divergence findings W003's own retro flagged as invisible from vision-doc reading alone (the "review"/"under-review"/"REVIEWS" collision; per-document vs. application-terminal "rejected"; the multi-vendor ACL pattern; the approval-terminal-gated handoff asymmetry) were exactly the findings that made W004 low-friction.

This observation spans the DS and EM artifact layers, it's a cross-session comparison (W004 vs. W002 upstream mechanisms) that no single per-session retro can make, and it predicts something about methodology evolution — so it meets entry 001's three criteria.

**The honest confound.** Much of W004's smoothness traces to the prompt's four `grill-with-docs` resolutions (locking the modeling pattern, the canonical identifier, the OQ-10 split, and the paired ADR structure before the session ran), not to DS-as-upstream alone. W002's narrative-pair upstream received nothing comparable in prompt-grilling intensity, so W004 alone cannot fully isolate the DS-vs-narrative variable. The DS contribution is nonetheless isolable: the vocabulary front-loading and findings-as-leans surface is attributable to DS specifically, independent of the grilling that amplified it into a low-friction walk.

**Implication.** Domain Storytelling earns a place as a **default pre-Event-Modeling step for vocabulary-rich BCs** — those with multiple actor types (especially human actors), vendor boundaries carrying foreign vocabulary, failure paths with distinct terminal shapes, and a vocabulary lifecycle with transition moments worth naming (Onboarding had all four; W003 retro Q5 predicted Operations and Trust & Safety share the profile). DS would likely add little for narrow, mechanical BCs (a projection-fed Telemetry BC where actors are systems and work objects are GPS pings).

A future entry would **confirm** this if a second DS→EM pair (e.g., an Operations DS followed by an Operations EM workshop) shows the same vocabulary-front-loading benefit with *less* prompt-grilling intensity than W004 had — isolating the DS contribution from the grilling confound. A future entry would **disconfirm** if a future DS→EM pair surfaces substantial mid-walk vocabulary churn despite DS-as-upstream, suggesting W004's smoothness was mostly grilling and the DS-as-default-pre-EM placement isn't warranted on DS's own merits.
