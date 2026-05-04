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
