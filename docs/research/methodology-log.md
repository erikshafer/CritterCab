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
