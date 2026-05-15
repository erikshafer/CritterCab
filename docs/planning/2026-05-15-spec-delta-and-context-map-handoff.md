# Handoff — Spec Delta Methodology + Context Map Artifact

**Date:** 2026-05-15
**Author:** Erik + Claude (session conducted before machine swap)
**Why this exists:** Erik is hopping to another machine partway through the session that produced these decisions. The resumed session will start without this conversation's `~/.claude/projects/...` memory and without the conversation transcript; this note carries the decisions forward so the work can resume cold.
**Disposable:** Delete this file once both follow-up sessions (Session A — spec-delta methodology, Session B — context-map artifact) have shipped. The decisions captured here will then live durably in the updated prompt/retro conventions, in `docs/context-map/`, and in any ADRs authored along the way.

---

## Session context (one paragraph for the resumed-session reader)

The session opened with a check that the Matt Pocock skills (`.agents/skills/`) are properly installed and discoverable — all five present, all loaded; `zoom-out` is intentionally model-non-invocable via `disable-model-invocation: true` frontmatter (Pocock-by-design, not a defect — it is a user-typed `/zoom-out` slash command only). The session then refreshed CritterCab's overall state (15 ADRs accepted, two workshops complete, two narratives, one protobuf contract, still in design phase, latest PR was #15 bundling ADRs 011–015). Two methodology questions then emerged: would OpenSpec collide with CritterCab's SDD/NDD approach, and is context mapping in the workflow as an artifact? The decisions below are this session's answers to those questions.

---

## Decisions made this session

### 1. Do NOT adopt OpenSpec as a framework

OpenSpec's primary affordances (canonical spec, closure loop, feature-scoped change folder) substantially overlap with what CritterCab's narratives + prompts + retros already do. CritterCab already runs eight artifact layers (vision, workshops, narratives, ADRs, prompts, retros, skills, rules). Adding OpenSpec wholesale would be a ninth, paying methodology tax without commensurate leverage.

The decision is **not** "OpenSpec is ruled out forever." It is "OpenSpec is not the right move *now*, before any implementation sessions have run." If after the first 3–5 implementation sessions the lightweight borrow (decision 2 below) feels insufficient, OpenSpec re-enters consideration as an ADR-shaped proposal at that point.

### 2. DO borrow OpenSpec's closure-loop pattern as **"spec delta"**

The strongest borrowable property of OpenSpec is its *discipline of capturing what changes in the spec when a session runs, in spec-shaped terms* (distinct from what the prompt captures, which is process-shaped). Working name: **spec delta**. Considered alternatives: "spec change-log" (too past-tense), "closure delta" (too jargon-bound to OpenSpec lineage). "Spec delta" stays.

Concrete shape (to be authored in Session A):

- **Add a `Spec delta` section to the prompt convention.** 2–4 lines per prompt, naming what the canonical spec (narrative or workshop) will gain when the session ships, expressed in narrative/workshop terms. This is the OpenSpec change-proposal payload, expressed inside the artifact CritterCab already writes.
- **Add a `Spec delta` outcome confirmation to the retrospective convention.** The retro confirms whether the planned delta landed, names any divergence, and updates the narrative's document-history accordingly.
- **Narratives gain a Document-History section.** Workshops already have one; narratives currently do not. Backfill 001 and 002 in the same session that introduces the convention.
- **The closure loop becomes:** prompt's `Spec delta` section → session executes → retro confirms what landed → narrative document-history records the amendment → planning state advances.

**Why this matters to CritterCab specifically (resumed-session: do not lose this).** Prior CritterX showcases (CritterBids, CritterSupply) carried stale areas of the project without clear reflection on what changed and why. The spec-delta discipline is a structural defense against that failure mode. This is the user's explicit motivation — not academic methodology completeness.

### 3. DO author a context-map artifact

`docs/context-map/` with a README and a diagram, naming each cross-BC relationship in DDD strategic-design vocabulary (partnership, customer-supplier, conformist, shared-kernel, published-language, anti-corruption-layer, separate-ways, open-host-service). The artifact rolls up cross-BC information that is currently distributed across:

- [ADR-006](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) (Identity ACL)
- [ADR-013](../decisions/013-shared-cross-bc-identifier.md) (shared identifier)
- [ADR-014](../decisions/014-asb-topic-naming-convention.md) (ASB topic naming)
- Workshop §13 forward-constraints in [W001](../workshops/001-dispatch-event-model.md) and [W002](../workshops/002-trips-event-model.md)
- Klefter translation-slice pattern (see [`docs/research/agents-in-event-models.md`](../research/agents-in-event-models.md))

The vision doc ([`docs/vision/README.md`](../vision/README.md)) names "first-class context map" as a top-line methodology goal — this artifact closes a commitment that has been open since v0.1. With Dispatch ↔ Trips (2 BCs) the implicit mapping is workable; with Identity, Pricing, Ratings, Payments, Operations, Onboarding, Rider Profile pending (heading to 7–9 BCs total), implicit mapping will not scale.

---

## Follow-up sessions to author

Two independent sessions. Each gets its own prompt under `docs/prompts/` per the one-prompt-one-session convention. Either order works; recommended order below.

### Session B — Context-map artifact (recommended first)

**Deliverables:**
- `docs/context-map/README.md`:
  - Overview prose framing the artifact, its update cadence, and its relationship to the vision doc's strategic-design commitment
  - A diagram (Mermaid preferred for in-repo legibility) showing each BC as a node and each cross-BC relationship as a labeled edge
  - A per-edge prose section naming the DDD relationship pattern in use, with rationale
  - Cross-references to the ADRs and workshop §13 sections listed above
- Update [`docs/vision/README.md`](../vision/README.md) to cross-reference the new context-map artifact and bump its version (currently v0.3)
- Consider authoring **ADR-016** ("Context map as living artifact") to commit the artifact's update cadence — i.e., when a workshop adds a new BC, the context map updates in that workshop's PR. Decision on whether to author the ADR is a session-start call; the artifact can stand on its own without it.

**Prompt-authoring hints:**
- Closest precedents in the prompts directory: workshop prompts (design-phase artifact authoring) and the bundled ADR-authoring prompt (`docs/prompts/decisions/002-bundled-pattern-adrs.md`).
- Subdirectory: `docs/prompts/design/` is a candidate new subdirectory (mirroring the new `docs/context-map/`), but `docs/prompts/` at the top level with a descriptive slug is also fine — match existing precedent of top-level prompts like `docs/prompts/encode-tidy-methodology-refinements.md`.
- Slug suggestion: `context-map-foundation` or `001-context-map-foundation` (depending on subdirectory choice).

**Required reading for the session:**
- Vision doc §Methodology and §Tentative Bounded Contexts
- ADR-006, ADR-013, ADR-014
- Workshop §13 sections in both W001 and W002
- `docs/research/agents-in-event-models.md` (Klefter pattern is the tactical implementation of several relationship types — published-language inbound becomes a translation slice with a local decision-event)

**Watch-out:** Do not redefine relationships that ADRs have already committed (especially ADR-006's ACL stance and ADR-013's shared-identifier stance). The context map should *name* and *cross-reference*, not re-decide.

### Session A — Spec-delta methodology (recommended second)

**Deliverables:**
- Update [`docs/prompts/README.md`](../prompts/README.md) to include a "Spec delta" section requirement, plus a brief description of what the section captures and how it differs from process-shaped session intent
- Update [`docs/retrospectives/README.md`](../retrospectives/README.md) to require a "Spec delta — landed?" outcome confirmation in retros
- Update [`docs/narratives/README.md`](../narratives/README.md) to require a Document-History section on each narrative (mirroring the workshops convention), and backfill 001 and 002 with their pre-session-A histories synthesized from git log
- Consider authoring **ADR-NNN** ("Spec delta as closure-loop discipline") — depending on the resumed-session call, this may or may not warrant ADR status. The discipline is structural enough to plausibly warrant ADR-016 / ADR-017 (whichever number is next after Session B's potential ADR-016).

**Prompt-authoring hints:**
- Closest precedent: [`docs/prompts/encode-tidy-methodology-refinements.md`](../prompts/encode-tidy-methodology-refinements.md). This is a methodology-tidying session, not a workshop or implementation session.
- Slug suggestion: `encode-spec-delta-closure-loop.md` (matches the `encode-tidy-*` pattern for methodology refinements).

**Required reading for the session:**
- This handoff note
- The prompts README, retros README, and narratives README (the three files being updated)
- `docs/decisions/003-spec-anchored-development.md` (the existing SDD commitment that spec-delta refines, not replaces)
- `docs/decisions/004-design-phase-workflow-sequence.md` (the workflow-sequence commitment that spec-delta plugs into)
- For reference only — *do not adopt as a framework*: the OpenSpec project's README, accessible via search if needed. The decision is to borrow the closure-loop pattern, not the framework.

**Watch-out:** Do not over-specify the `Spec delta` section's format. 2–4 lines per prompt is the target; codifying a rigid sub-schema would defeat the lightweight intent.

---

## Sequencing rationale

**Recommended: B (context map) first, then A (spec-delta methodology).** Reasons:

1. Session B is more concretely scoped — the inputs (ADRs, workshop §13 sections, BC list from vision) are all in the repo, and the output shape (README + diagram + per-edge prose) is well-defined. It produces an immediate durable artifact.
2. Session B can run cleanly *without* the spec-delta convention in place; it would simply be the last session to use the pre-spec-delta prompt format.
3. Session A then has Session B as its first real test case — the resumed prompt template can be applied retroactively (in Session A's retro for itself, demonstrating the discipline) and prospectively (for all sessions after A).

Reverse ordering (A first) is also workable but means Session A's own prompt cannot itself include a Spec delta section (the convention doesn't exist yet at its session start). That introduces a small self-reference awkwardness but is not a blocker.

---

## Working norms (memory carry-forward for the resumed machine)

The original session ran with several user-feedback memories loaded that the new machine will not have. The key directives that apply to Sessions A and B:

- **Depth + ubiquitous language + leaning opinions.** Use domain vocabulary precisely (BCs, slices, projections, Klefter, Bruun, etc.). When the user asks a question with a genuine choice, give a clear lean rather than a neutral menu. Informative depth is preferred over terseness.
- **BC-owned enums; cross-BC translation at boundaries.** Do not propose shared enum types across BCs. Each BC owns its own; relationships translate at the boundary. Relevant to context-map authoring when naming relationship payloads.
- **Validation at the HTTP boundary, not the aggregate.** Aggregates only reject illegal state transitions. Wolverine.HTTP `Before()` / `Validate()` is the validation seam. Not directly bearing on Sessions A or B, but applies if any implementation pressure surfaces during the design work.
- **Critter Stack primitives, not bespoke alternatives.** When mechanisms come up, lead with Wolverine / Marten / Alba; do not list hand-rolled options as peers.
- **Static endpoints, Alba-first tests.** Wolverine.HTTP endpoints default to static; tests default to Alba scenarios. Not directly applicable here but applies if either session opens a tangent toward implementation.
- **No Claude attribution on commits or PRs.** Omit `Co-Authored-By: Claude` trailers and "Generated with Claude Code" PR footers. Commits and PRs attribute to the user's git credentials only.
- **Keep READMEs current alongside session work.** When a session touches a convention/pattern that a README should reflect, update the README in the same session. This is *especially* relevant to Sessions A and B because both touch READMEs directly as deliverables.
- **Prune textureless detail in narrative prose.** Tight first drafts. No invented atmospheric details with no scenario load.
- **Explicit deferrals during artifact authoring.** When something is deliberately omitted, name it and say why. Applies in spades to Session B (some BCs are still pre-workshop and their relationships cannot be fully named yet — defer explicitly).
- **Proactive projection proposals.** During slice walks or domain-modeling sessions, propose speculative projections with audience + shape + feeders + status even when deferring them. Applies to Session B if any projection-shaped relationships surface.
- **Driver-mode when the user signals a mental block.** If Erik says "you decide" or signals fatigue, take initiative on non-durable choices (branch names, mechanical edits, ordering decisions) but keep sign-off discipline for durable artifacts and irreversible operations.

**If syncing memory directories across machines is feasible**, the relevant memory entries are: `feedback_communication_depth_ubiquitous_language`, `feedback_bc_owned_enums`, `feedback_validation_at_http_boundary`, `feedback_critter_stack_primitives`, `feedback_static_endpoints_alba_first`, `feedback_no_claude_attribution`, `feedback_keep_readmes_current`, `feedback_prune_textureless_detail`, `feedback_explicit_deferrals`, `feedback_proactive_projections`, `feedback_driver_mode_on_user_block`. Each is a `.md` file under `C:\Users\<user>\.claude\projects\C--Code-CritterCab\memory\` on the original machine.

---

## What is explicitly NOT being decided here

- **OpenSpec is not ruled out permanently.** Re-evaluation candidate after 3–5 implementation sessions ship and the spec-delta borrow has been exercised against real change-flow.
- **ADR authorship for Sessions A and B is left to session-start judgment.** Both have plausible ADR shapes (context-map cadence; spec-delta discipline). Neither is required.
- **`docs/context-map/` directory structure beyond the README** — single-file vs. one-file-per-relationship — is a Session-B decision. Strong default: single README + single diagram for the v1; split if/when the artifact grows past one screen of edges.
- **Diagram format for the context map** (Mermaid vs. PlantUML vs. embedded SVG) is a Session-B call. Lean toward Mermaid for in-repo legibility on GitHub, but PlantUML is an acceptable second choice.
- **The `Spec delta` section's exact wording in the prompt template** is a Session-A call. Target 2–4 lines, narrative/workshop-terminology, but the prose convention is the session's to set.

---

## Cross-references

**Foundational artifacts (read before either session):**
- [`docs/vision/README.md`](../vision/README.md) (v0.3 — primary state-of-project doc; will need version bump in Session B)
- [`CLAUDE.md`](../../CLAUDE.md) (routing layer; do not exceed its scope guidance)

**Workflow conventions to update or honor:**
- [`docs/prompts/README.md`](../prompts/README.md) (Session A updates this)
- [`docs/retrospectives/README.md`](../retrospectives/README.md) (Session A updates this)
- [`docs/narratives/README.md`](../narratives/README.md) (Session A updates this)
- [`docs/decisions/README.md`](../decisions/README.md) (may need updating if either session authors an ADR)
- [`docs/planning/README.md`](./README.md) (defines this directory's conventions)

**Adjacent precedents:**
- [`docs/prompts/encode-tidy-methodology-refinements.md`](../prompts/encode-tidy-methodology-refinements.md) — model for Session A's prompt shape
- [`docs/prompts/decisions/002-bundled-pattern-adrs.md`](../prompts/decisions/002-bundled-pattern-adrs.md) — model for an ADR-authoring session, if either follow-up session decides to include an ADR
- Commit `7562dce` — prior handoff-note precedent in this directory (post-vacation orientation)

---

## Suggested first action on the resumed machine

1. Read this file end-to-end.
2. Confirm with Erik whether the recommended ordering (B first, then A) still holds, or whether priorities have shifted since this note was written.
3. Author the prompt for whichever session is going first. Use the `docs/prompts/encode-tidy-methodology-refinements.md` or `docs/prompts/decisions/002-bundled-pattern-adrs.md` as the structural template, depending on session shape.
4. Pause for prompt sign-off before executing.
