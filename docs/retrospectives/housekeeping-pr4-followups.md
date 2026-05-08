# Retrospective — PR #4 Housekeeping: Workshop §12.8 Follow-ups Index and Handoff Note Annotation

## Metadata

- **Triggering prompt:** [`docs/prompts/housekeeping-pr4-followups.md`](../prompts/housekeeping-pr4-followups.md)
- **Status:** Complete
- **Date authored:** 2026-05-08
- **Output artifacts:**
  - `docs/workshops/README.md` — added `## Workshop follow-ups` section between `## Workshops` and `## Conventions`
  - `docs/planning/2026-05-07-post-d-b-c-handoff.md` — added `**Acted on (2026-05-08):**` annotation block immediately after the title, before the existing Purpose line
  - `docs/prompts/README.md` — renamed root-level subsection from `Skill-library tidy and phase-level prompts (root)` to `Multi-artifact prompts (root)`; added the new prompt entry
  - `docs/retrospectives/README.md` — renamed root-level subsection to match (`Multi-artifact retros (root)`); added this retro entry
  - `docs/prompts/housekeeping-pr4-followups.md` (new) — the prompt that triggered this session
  - `docs/retrospectives/housekeeping-pr4-followups.md` (new, this file) — the session retro
- **Outcome:** Both PR #4-era loose ends closed in one micro-PR. The workshops index now carries an aggregated follow-ups section. The post-D→B→C handoff note carries an acted-on annotation in place rather than being archived. Root-level subsections in prompts/README and retros/README renamed to a more general label that accommodates phase-level, tidy, and housekeeping entries without further renames as the section grows.

---

## Framing

Two PR #4-era loose ends were flagged in the skill-tidy retrospective as worth landing before the Trips workshop kicks off: (1) the workshops/README §12.8 follow-ups index proposed during prompt-sketching, and (2) the post-D→B→C handoff note's pending acted-on status. This session bundled both into one micro-PR per the cadence rule's "single coherent scope" principle.

---

## Outcome summary

| Deliverable | Approach |
|---|---|
| Workshops §12.8 follow-ups index | New `## Workshop follow-ups` section between `## Workshops` and `## Conventions`. Compact format with named-action-items table per workshop. ADR candidates and parking-lot items deferred via pointer-note rather than enumerated. |
| Post-D→B→C handoff annotation | `**Acted on (2026-05-08):**` callout block placed immediately after the title and before the Purpose line — visible-first ordering. Note remains in place per `docs/planning/README.md`'s "disposable by design" convention. |
| Root-level subsection rename (prompts/README, retros/README) | Renamed to `Multi-artifact prompts/retros (root)`. Names by structural shape (multi-artifact) rather than content kind (skill-library, phase-level, tidy). Locks in early — future entries land cleanly without further renames. |

---

## What worked

- **Bundling the prompt + prompts/README index entry into one commit held this round.** The skill-tidy retrospective's lesson (don't commit the prompt before the index update is authored) was applied successfully here. One commit, one logical change ("introduce this prompt into the project's index"). No fix-up commit needed — the failure mode flagged in the prior retro was actively avoided.
- **In-place annotation for the handoff note feels right.** The note's framing content (artifacts inventory, A-vs-E decision-point write-up) remains visible reference; the acted-on callout makes its current state legible without losing the historical context. No archive overhead.
- **The rename was cheaper than expected.** Two-entry subsection becomes three-entry under a more general label. Doing this with two existing entries is far cheaper than doing it later when more accumulate.
- **The §12.8 index format is conservative on purpose.** Only named action items get rows. ADR candidates (8 with explicit triggers) and parking-lot items (14) get a pointer-deferral, not enumeration. Compact section; doesn't duplicate workshop §10/§11; future maintenance burden stays low.

---

## What was harder than expected

Nothing surprising — the session was small, the deliverables were two, and the methodology was already calibrated by the skill-tidy session. The only minor friction was deciding the rename label: `Multi-artifact` won over `Cross-cutting and phase-level` and `Tidy and housekeeping` by being **structurally accurate** (describes what the section contains by *shape*, not by *content type*). Naming by content type — even with multiple types listed — calcifies as new types accumulate.

---

## Methodology refinements that emerged

- **Name index sections by structural shape, not by content kind.** When a section accumulates entries of different content kinds (phase-level retros + tidy retros + housekeeping retros + future kinds), naming by content kind requires renames every time a new kind arrives. Naming by shape (`Multi-artifact`) doesn't. Worth keeping in mind for future index sections — both inside this project and in any related index work.
- **Bundled prompt + index commit pattern is reproducible.** Two for two now (the failure case in skill-tidy taught the lesson; this session applied it). Treat as the default approach for future sessions: never commit the prompt without its index entry already drafted.

These refinements don't need their own encoding pass — they're small enough to ride alongside the next housekeeping PR (PR-B) which is already scoped to encode methodology refinements 1+2 from the skill-tidy retrospective. PR-B can pick these up too if appropriate.

---

## Outstanding items / next-session inputs

- **PR-B: encode methodology refinements 1+2** from the skill-tidy retrospective (and optionally the two refinements above). Same housekeeping queue; smaller scope. Updates DEBT.md conventions section + prompts/README cadence section with: "no opportunistic edits to *other files*" + source-of-truth precedence.
- **PR-C: add Namespaces pattern** to `docs/skills/_template/SKILL.md`. Slightly bigger micro-PR. Last housekeeping item in the queue.
- **Trips workshop** — the next major design session. Deferred until the housekeeping queue is drained.
- **Workshops README §12.8 backporting** — when the next workshop is authored, its §12.8 follow-ups should be added to the new index section in the same PR. Convention is now in place.

---

## Quantitative summary

- **Commits:** 4 (prompt + prompts/README rename + add entry; workshops/README §12.8 section; handoff annotation; retro + retros/README rename + add entry).
- **Files modified:** 4 (workshops/README, planning handoff note, prompts/README, retros/README) plus 2 new files (prompt, retro).
- **Edit footprint:** small. One new section header in workshops/README; one annotation block in handoff note; two subsection renames + two entry additions across prompts/README and retros/README; two new files.
- **Out-of-scope items deferred:** PR-B's rule encoding, PR-C's template addition, Trips workshop, Phase 6 placeholder cleanup, ai-skills lean-out, backporting bidirectional cross-references from each ADR candidate / parking-lot item to the new index.
