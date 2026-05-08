# Post-Housekeeping Handoff — 2026-05-08 (evening)

> **Purpose:** Cross-machine handoff note. Erik is swapping to a MacBook Pro for the evening. Captures where we're at after PR #10 merged and what the next session opens with. Disposable once the next session orients — fold or delete after the Trips workshop session begins.

---

## Where we are

The housekeeping queue surfaced by the skill-tidy retrospective is fully drained:

- **PR #7 / skill-tidy-marten-and-bootstrap** — drained Marten 8.x / JasperFx namespace and service-bootstrap registration debt; established the `tidy: skills` PR convention.
- **PR #8 / housekeeping-pr4-followups** — added the workshop §12.8 follow-ups index; annotated the post-D→B→C handoff as acted-on; renamed root-level subsections to `Multi-artifact`.
- **PR #9 / encode-tidy-methodology-refinements** — encoded "no opportunistic edits to other files" + source-of-truth precedence as permanent rules.
- **PR #10 / skill-template-namespaces-pattern** — lifted the Namespaces pattern into `docs/skills/_template/SKILL.md`.

ADR-004's design-return cadence rule is now actively pointing toward returning to the design phase before any further Dispatch implementation. That points at the **Trips workshop** — option A from the post-D→B→C lean, deferred until housekeeping was complete.

---

## What's next: the Trips workshop

This is a **regime change** from the housekeeping rhythm. Expect:

- A workshop prompt at `docs/prompts/workshops/002-trips-event-modeling.md` (matches the Workshop 001 naming pattern).
- The workshop session itself will produce `docs/workshops/002-trips-event-model.md` — likely 1500-2000+ lines like Workshop 001.
- Possibly multiple modeling sessions; Workshop 001 was effectively retroactive, Trips will be authored fresh.
- Workshop 001 §12.6 captured methodology adjustments worth applying to the second workshop: proactive projections from slice 1, pre-walk aggregate-identity sidebar, sub-slice numbering, deferred Protobuf authorship (the last is already handled in PR #4).
- Trips' §12.8 follow-ups should land in the `docs/workshops/README.md` index in the same PR — convention is in place from PR #8.

---

## Suggested first actions when you resume

1. Re-read [Workshop 001 §12.6 "Adjustments for the next BC workshop"](../workshops/001-dispatch-event-model.md) and §12.8 "Follow-ups generated".
2. Decide: brief Dispatch↔Trips context-mapping pass first (lightweight, helps frame the workshop's swim-lane decisions), or jump directly into authoring the Workshop 002 prompt.
3. If jumping directly: open `docs/prompts/workshops/002-trips-event-modeling.md` for sketching.

---

## Pointers to deep context

- **Workshop 001** (precedent for Trips' artifact shape): [`docs/workshops/001-dispatch-event-model.md`](../workshops/001-dispatch-event-model.md).
- **Latest retro** (closes housekeeping queue, captures the bundling-break observation): [`docs/retrospectives/skill-template-namespaces-pattern.md`](../retrospectives/skill-template-namespaces-pattern.md).
- **§12.8 follow-ups index** (Trips workshop listed there as pending): [`docs/workshops/README.md`](../workshops/README.md) § Workshop follow-ups.
- **Narrative 002's forward-constraint** (rider-name surfacing on driver-app UI; Trips' workshop should honor or override): see methodology log entry 003 in [`docs/research/methodology-log.md`](../research/methodology-log.md).
- **Proto contracts** authored in PR #4: `protos/crittercab/dispatch/v1/`. These are the receiver-side starting point for Trips' translation-in slices.

---

## Things to watch for

- **Bundling break for prompt + prompts/README index commits** (per PR #10 retro). When the prompt is committed independently before Claude starts execution, the prompt + index commit splits — same break shape as PR #7 and PR #10. Easiest avoided by letting Claude do both commits in sequence after sign-off. Two candidate conventions noted in PR #10's retro for encoding after a third instance.
- **Major design session size.** Don't expect the housekeeping cadence to translate. Workshop sessions are longer, surface more methodology observations, and may warrant new methodology log entries.

---

## Document history

- **2026-05-08 (evening).** Authored on the desktop before swapping to MacBook for the evening. Replaces `2026-05-07-post-d-b-c-handoff.md` as the active orientation note (the prior note was annotated as acted-on in PR #8 and remains in place per the planning README's "disposable by design" convention).
