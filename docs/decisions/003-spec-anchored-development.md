# ADR-003: Spec-Anchored Development

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab commits to a structured design methodology: an Event Modeling workshop produces a blueprint; NDD-informed narratives in `docs/narratives/` distill that blueprint into journey-scoped specifications; prompt documents in `docs/prompts/` drive implementation sessions from those narratives. The methodology is not in question. What requires a decision is the *authority relationship* between the specifications the methodology produces and the code that implements them.

That question became a live decision point in 2025–2026 when Spec-Driven Development matured from a framing into a product category. Tools like Amazon Kiro, Xolvio's Auto platform, and prooph board's code-generation pipeline implement what the SDD literature calls *spec-as-source*: the specification is the authoritative artifact, and code is derived from it. A change to the spec causes the agent to regenerate or patch the code; the spec and the code are never out of sync because the code is always downstream. This is a different contract than the one most specification-adjacent methodologies (BDD, TDD, Event Modeling) have historically offered.

The choice of authority relationship has structural consequences for the project. It determines what tooling is required, what the update cycle looks like when implementation reveals a modeling gap, and how contributors — human or AI — should behave when spec and code disagree.

## Options Considered

### Option A — Spec-free

Implementation proceeds directly from the Event Modeling workshop output and informal session discussion. No persistent narrative layer sits between workshop artifacts and code. The Event Model is produced once, consulted during development, and allowed to drift as the system evolves.

This is the approach most projects actually take, whether intentionally or by default. It is low-overhead and does not require anyone to maintain a parallel specification layer. For a short-horizon project where the original contributors remain engaged and the scope is stable, it works.

For CritterCab, the "Capture intent in durable, structured form" design principle is load-bearing. The project is explicitly designed to be maintained across sessions, contributors, and time. A solo-maintainer project with long gaps between sessions needs more durable recorded intent than a co-located team with continuous context. Without a specification layer, the rationale behind domain decisions evaporates, and every AI-assisted session re-derives context from code rather than from intent.

### Option B — Spec-as-source

The Event Model and narratives are authoritative. Code is generated or regenerated from them by an agent. When the spec changes, the agent patches the implementation. The spec and code are kept in sync by the toolchain, not by discipline.

This is the model commercial SDD platforms (Auto, Kiro, prooph board's code-generation pipeline) implement. The appeal is real: the gap between "design is done" and "implementation is done" narrows, and the gap between "spec was updated" and "code reflects the update" disappears. For a team producing many slices at a fast cadence, the productivity upside is substantial.

The costs are also real, and they are specific to CritterCab's situation. Spec-as-source requires a platform — either a commercial product or a purpose-built agent pipeline — that understands the specification format, can interpret it, and can generate and patch Critter Stack code from it. No such platform exists for the Critter Stack today. Building one from scratch to support CritterCab's development would make the toolchain a project deliverable, which would distort the project's actual purpose. Commercial platforms (Auto uses its own Zod-backed narrative schema; Kiro uses its own spec format) impose a format and an ecosystem dependency that CritterCab's open-source reference-architecture mission does not justify — contributors should not need to license a commercial tool to understand or extend the project.

### Option C — Spec-anchored

The Event Model and NDD-informed narratives are the architectural reference. They describe intent, domain behavior, and the reasoning behind design choices. Code is authoritative for runtime behavior — the code does what it does, regardless of what a narrative says. Drift between spec and code is detected by the retrospective at session close, not by automated tooling. When drift is detected, spec or code is updated (whichever is wrong) and committed in the same PR as the retrospective.

Spec-anchored is distinct from spec-first in one critical way: the specifications are kept current. A spec-first document is written before coding begins and then abandoned; it is a snapshot of intent at one moment, accurate at that moment, stale afterward. A spec-anchored document is a living artifact maintained in lockstep with the code, via disciplined retrospective review, for the life of the project.

This approach requires no external platform beyond what CritterCab already uses — git, markdown, and a consistent retrospective habit. It is also the approach that makes the project's documentation actually useful to contributors, because the narratives reflect the current state of the system rather than the state it was in when the session that produced them ran.

## Decision

**Option C.** CritterCab uses spec-anchored development. The Event Model and narratives in `docs/narratives/` are the architectural reference. Code in the service projects is authoritative for runtime behavior. When the two disagree after a session, the retrospective for that session identifies the disagreement, and the correct artifact is updated in the same PR that closes the session.

The sync mechanism is explicit retrospective review, not automated derivation. Every retrospective closes with the question: *did this slice's implementation teach us anything that should update the Event Model or the narrative?* If yes, that update is part of the session's PR, not a follow-up.

CritterCab does not adopt spec-as-source commercial platforms. This is a deliberate non-adoption: the project's open-source reference-architecture purpose requires that contributors be able to understand, run, and extend the project without licensing external tooling.

## Consequences

The narrative layer at `docs/narratives/` is a first-class project deliverable with the same maintenance expectations as the code. A narrative that has drifted from the implementation is a defect, caught at retrospective time.

AI-assisted sessions load the relevant narrative before generating implementation. When the generated code diverges from what the narrative specifies, the divergence is surfaced in the retrospective and resolved — either by correcting the code or by updating the narrative to reflect what was learned. The retrospective is where "the model was wrong" becomes "the model is updated" rather than "the model is ignored."

The cost of this approach is retrospective discipline. A session that closes without a retrospective leaves the spec-code relationship unaudited. For that reason, the retrospective is not optional: it is part of the session's definition of done, and the PR that contains the implementation contains the retrospective.

Adopting a spec-as-source workflow in the future would supersede this ADR. The trigger for that reconsideration would be the emergence of a Critter-Stack-aware code-generation platform with permissive licensing — at that point, the balance of costs shifts.
