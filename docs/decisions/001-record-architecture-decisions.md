# ADR-001: Record Architecture Decisions

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab is a reference architecture with a long design phase ahead of it. Decisions about deployment models, transport choices, identity integration, development methodology, and service topology will accumulate over months. These decisions have dependencies on each other, and some will be revisited as the project matures.

Without a durable record, decisions live in chat history, vision documents, and memory. That creates three problems. First, the rationale behind a decision fades faster than the decision itself — six months from now, the *what* is visible in the code, but the *why* is gone. Second, contributors (human or AI) who arrive after the decision was made have no way to evaluate whether the context that drove it still holds. Third, decisions that should be revisited are never revisited, because nobody remembers the conditions under which they were made.

A lightweight, version-controlled decision record addresses all three. Each record captures the context, the options considered, the choice made, and the consequences — including what was traded away. The record is append-only in spirit: decisions are superseded, not edited, so the history of reasoning is preserved.

## Options Considered

### Option A — No formal decision records

Decisions are captured implicitly in the vision document, code comments, and commit messages. This is the lowest-ceremony option and works well for small projects where the decision-maker is also the only contributor and the project's lifetime is short.

The risk is that CritterCab is explicitly designed to outlive any single session of work. The vision document is a living artifact that reflects current state, not historical reasoning. Code comments capture implementation rationale, not architectural alternatives. Commit messages are too granular to carry strategic context.

### Option B — Heavyweight decision documents

Decisions are captured in long-form documents with extensive analysis, stakeholder sign-off sections, and formal review processes. This is appropriate for organizations where decisions have compliance implications or where multiple teams need to formally agree.

CritterCab is maintained by a small team (currently one person) building a reference architecture. The overhead of formal review processes would slow decision-making without adding proportional value. The formality would also discourage recording smaller decisions that nonetheless matter.

### Option C — Lightweight ADRs in version control

Decisions are captured as short markdown files in `docs/decisions/`, following a consistent template. Each record is small enough to write in a few minutes, lives alongside the code it governs, and evolves through supersession rather than editing. This is the approach popularized by Michael Nygard and adopted widely in the software architecture community.

## Decision

**Option C.** CritterCab records significant architectural decisions as lightweight ADRs in `docs/decisions/`.

Each ADR follows this structure:

- **Title.** A short noun phrase describing the decision, prefixed with a three-digit number (e.g., `001-record-architecture-decisions.md`).
- **Status.** One of: `Proposed`, `Accepted`, `Superseded by ADR-XXX`, or `Deprecated`.
- **Date.** The date the decision was accepted.
- **Context.** The forces at play — what problem exists, what constraints apply, what triggered the need for a decision.
- **Options Considered.** The alternatives evaluated, with a brief assessment of each. This section exists because understanding what was *not* chosen, and why, is often more useful than understanding what was chosen.
- **Decision.** The choice made, stated clearly.
- **Consequences.** What follows from the decision — both the benefits and the costs. Tradeoffs are named, not hidden.

Guidelines for when to write an ADR:

- A decision that affects how services communicate, how data is stored, how the system is deployed, or how the development workflow operates deserves an ADR.
- A decision that is easily reversible (a library choice that can be swapped in an afternoon, a naming convention for test files) does not need an ADR unless the reasoning is non-obvious.
- When in doubt, write the ADR. A short record that turns out to be unnecessary costs five minutes. A missing record that would have prevented a week of re-derivation costs much more.

ADRs are not retrospectives, prompts, or vision-document sections. They answer one question: *why does the system work this way?* Other artifacts answer other questions.

## Consequences

Every significant architectural decision from this point forward is recorded as an ADR before (or concurrently with) the work that implements it. The `docs/decisions/README.md` index is updated when new ADRs are added.

Superseded decisions are not deleted. When a decision is revisited and changed, a new ADR is written, and the original's status is updated to reference the new one. This preserves the reasoning chain.

The cost is the time to write each record. The benefit is that future contributors — including the original decision-maker returning after a break — can reconstruct *why* the system is shaped the way it is without re-deriving the reasoning from scratch.
