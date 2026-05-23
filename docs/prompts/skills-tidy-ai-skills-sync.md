# Prompt — Skill Tidy: Sync to ai-skills @ b0d0f7d

| Field | Value |
|---|---|
| **Status** | Complete (executed 2026-05-23; retro at [`docs/retrospectives/skills-tidy-ai-skills-sync.md`](../retrospectives/skills-tidy-ai-skills-sync.md)) |
| **Authored** | 2026-05-22 |
| **Target artifacts** | `docs/skills/wolverine-http-handlers/SKILL.md` (verify; edit if Cab text overlaps strengthened upstream content), `docs/skills/marten-projections/SKILL.md` (verify-only; no edits expected), `docs/retrospectives/skills-foundation-phase-5.md` (close out Status / Outcome / document history — natural conclusion of the reconciliation thread this session continues), `docs/retrospectives/skills-tidy-ai-skills-sync.md` (new), `docs/prompts/README.md` (index entry under Multi-artifact prompts) |
| **Sync target** | ai-skills @ `b0d0f7d` (HEAD as of 2026-05-22 evening at `C:\Code\JasperFx\ai-skills\`). |
| **Source-of-truth dependencies** | Upstream ai-skills clone at `C:\Code\JasperFx\ai-skills\` (the sync target); [`docs/retrospectives/skills-foundation-phase-5.md`](../retrospectives/skills-foundation-phase-5.md) (baseline reconciliation state — what Cab kept Cab-specific and what it now defers to upstream for); current Cab skill files (read each immediately before editing it). |
| **Workflow position** | Second skill-tidy session; first ai-skills upstream-sync session. Lighter-cadence successor to Phase 5's full reconciliation pass. |

---

## Framing — why this session exists

Phase 5 (closed 2026-05-06) was the canonical reconciliation pass against the JasperFx ai-skills library: every Cab skill audited against its upstream counterpart, Cab content trimmed where ai-skills covered it authoritatively. Eighteen commits have landed in ai-skills since then. Of those, fourteen are infrastructure (sidebar reconciliation, Buy CTAs, install URL fixes, version bumps, the new `added_in` frontmatter convention) and four are content (HTTP idiomatic + style strengthening across three upstream skills; Marten projection `IncludeType<T>()` scope clarification across three upstream skills; first cut at migration skills for the latest Marten / Wolverine releases).

The delta is small enough that a full re-reconciliation would be over-scoped. This session is the lighter-cadence successor: walk the substantive content commits, identify Cab counterparts that overlap, propagate only the overlap, close Phase 5's still-`In progress` retro, and create the sync-floor record so the next sync has a precise commit baseline.

The session may produce **zero content changes to Cab skill files**. That is a valid outcome and is consistent with how aggressively Phase 5 trimmed Cab against upstream — what remains in Cab is Cab-specific content that upstream's "strengthen" commits (which sharpen idiomatic-vs-interop framing in fundamentals-level upstream skills) are unlikely to overlap with. The retrospective is the primary deliverable regardless: it records the sync floor, captures the verification reasoning, and creates the durable trail for the next sync.

---

## Goal

Sync Cab's skill library to ai-skills @ `b0d0f7d`. Verify Cab counterparts against the four content-commit changes; apply propagation only where overlap actually exists. Close out the Phase 5 retrospective. Author this session's retro as the sync-floor record.

---

## Spec delta

No canonical spec is amended by this session. The session targets the skill catalog (a downstream tooling layer, not a domain spec) and the Phase 5 reconciliation retro (a methodology record). The narrative + workshop trees are unaffected.

(This is the second prompt in CritterCab's history to honestly name spec delta as null. First was [`housekeeping-delete-may-15-handoff.md`](./housekeeping-delete-may-15-handoff.md). The pattern is by-design — see [`docs/prompts/README.md` § Spec delta cadence](./README.md#spec-delta-cadence).)

---

## Source-of-truth precedence

For each upstream-side change considered for propagation, the precedence is:

1. **Upstream ai-skills @ `b0d0f7d`** (the sync target — authoritative for what changed and how).
2. **Cab's existing skill content** (authoritative for what Cab still owns Cab-specifically, per Phase 5's trim discipline).
3. **Cab's working code in `src/CritterCab.Dispatch/`** (authoritative when upstream guidance disagrees with what Cab's code actually does — Cab follows its code, then notes the divergence).

If a content commit upstream describes a pattern that Cab's code uses differently — pause and surface the conflict rather than rewriting Cab's skill to match upstream. Upstream is the sync source; it is not a license to retroactively re-litigate Cab decisions.

---

## Orientation files (read in order)

1. **[`docs/retrospectives/skills-foundation-phase-5.md`](../retrospectives/skills-foundation-phase-5.md)** — the baseline reconciliation. Required reading for understanding what Cab kept Cab-specific in each affected skill and what it now defers to upstream for. Note its `Status: In progress` field.
2. **Upstream commit log since 2026-05-06**, captured below in § Upstream delta. The full delta is also re-derivable from the clone at `C:\Code\JasperFx\ai-skills\`.
3. **`C:\Code\JasperFx\ai-skills\skills\wolverine-http-fundamentals\SKILL.md`** — the upstream skill that received the most substantive strengthen content (~162 added lines across two commits). Phase 5 merged this skill plus `wolverine-http-hybrid-handlers` and `wolverine-http-marten-integration` into the Cab hub `wolverine-http-handlers`.
4. **`C:\Code\JasperFx\ai-skills\skills\marten-projections-single-stream\SKILL.md`** and **`marten-projections-multi-stream\SKILL.md`** — the upstream skills that received the `IncludeType<T>()` scope clarification. Phase 5 reconciled these into the Cab hub `marten-projections`.
5. **[`docs/skills/wolverine-http-handlers/SKILL.md`](../skills/wolverine-http-handlers/SKILL.md)** — Cab counterpart for the HTTP delta. Read end-to-end before deciding any propagation; grep first for the upstream-changed terminology (see § Working pattern step 2).
6. **[`docs/skills/marten-projections/SKILL.md`](../skills/marten-projections/SKILL.md)** — Cab counterpart for the projections delta. Verify-only; a prior grep confirms no current `IncludeType<T>` content (see § Decisions to flag #2).
7. **[`docs/prompts/skills-tidy-marten-and-bootstrap.md`](./skills-tidy-marten-and-bootstrap.md)** + its retro — the structural precedent for skill-tidy session shape (working pattern, source-of-truth precedence, no-opportunistic-edits discipline). This session inherits its discipline; the difference is the trigger (upstream sync vs. DEBT.md drain).

---

## Upstream delta (sync target = `b0d0f7d`, floor = Phase 5 close `~ 2026-05-06`)

Eighteen commits since the floor. Categorized:

### Content commits (potentially propagate)

| SHA | Subject | Upstream files touched | Cab counterpart |
|---|---|---|---|
| `81eede0` | Strengthen Wolverine HTTP idiomatic guidance (success/failure-path terminology, anti-ceremony around result wrappers / `InvokeAsync` / transactional defaults, new "Default style policy" + new "Edge cases & gotchas" sections + new "Carrying Minimal API ceremony" anti-pattern) | `wolverine-http-fundamentals` (+132), `wolverine-http-hybrid-handlers` (+10), `wolverine-http-marten-integration` (+4), `wolverine-converting-from-minimal-api` (+24) | `docs/skills/wolverine-http-handlers/SKILL.md` |
| `f7eb20e` | Strengthen Wolverine HTTP style guidance (default Wolverine-native endpoint style; discourage Minimal-API result-wrapper ceremony unless dynamic branching needed) | `wolverine-http-fundamentals` (+30), `wolverine-http-hybrid-handlers` (+28), `wolverine-http-marten-integration` (+11), `wolverine-converting-from-minimal-api` (+27) | `docs/skills/wolverine-http-handlers/SKILL.md` |
| `da99a7a` | Marten projections: scope `IncludeType<T>()` to interface/base-type `Apply` methods (Marten infers from concrete event types otherwise) | `marten-projections-single-stream`, `marten-projections-multi-stream`, `marten-advanced-optimization` | `docs/skills/marten-projections/SKILL.md` (verify-only — grep at prompt-authoring time showed no current `IncludeType<T>` content) |
| `1d7851b` + `3a8647f` + `c7a94d5` | First cut at migration skills for latest Marten + Wolverine; cover the `WolverineFx.RuntimeCompilation` requirement; drop `Saga.Version int->long` content | `wolverine-migration-v5-to-v6`, `marten-migration-v8-to-v9` (both NEW skills upstream) | **No Cab counterpart** — Cab does not currently document migration content. Defer cross-ref decisions to a future session where a Cab skill reaches for migration guidance. |

### Infrastructure commits (do NOT propagate)

Fourteen commits covering: `added_in` frontmatter stamping (new upstream convention; Cab opt-in is a separate methodology decision), version bumps to 1.3.0 and 1.4.0, Buy CTAs and UTM tracking across upstream docs, install URL formatting fixes, sidebar reconciliation with the new Miscellaneous fallback, hardcoded skill-count removal, support links. None of these touch skill-body content; none propagate to Cab.

### Catalog growth (no immediate Cab counterpart needed)

Upstream catalog has grown from ~70 to 75 skills since Phase 5 close. The three new skills (`marten-migration-v8-to-v9`, `wolverine-migration-v5-to-v6`, `wolverine-integrations-critterwatch-setup`) have no current Cab counterpart. Upstream's `wolverine-grpc-handlers` / `wolverine-grpc-bidirectional-handlers` equivalents are still unpublished (Phase 5 retro noted Erik is the planned upstream author); this session does not re-evaluate Cab's "ahead of ai-skills" disposition for the two Cab gRPC skills.

---

## Working pattern

- **One session, one PR.** PR title: `tidy: skills — sync to ai-skills @ b0d0f7d`. Targets above plus this prompt's retro ship together.
- **Per-Cab-skill batching inside the PR.** Group the diff so each affected Cab skill (if any) is reviewable as a coherent unit. One commit per skill is reasonable; or one combined commit if commits are not load-bearing. Mention which upstream SHA the change corresponds to in the commit body.
- **Pause-after-each-skill cadence.** Inherited from Phase 5 methodology. For each Cab counterpart in the table above: read it end-to-end, propose findings + proposed edits to the user, **pause for sign-off** before applying any edits. The session may legitimately conclude that no edits are warranted on a given skill — say so explicitly rather than fabricating overlap.
- **Verify before edit.** Per source-of-truth precedence: grep Cab's skill for the upstream-changed terminology *first*, then read the matched context, *then* decide whether the change applies. A pre-prompt grep already showed `wolverine-http-handlers` does not currently use "sad path" / "happy path" / "InvokeAsync" — and `marten-projections` does not currently use `IncludeType<T>`. The session-runner re-runs these greps as the first step on each skill in case authoring drift has occurred between prompt-authoring and session-running.
- **No opportunistic edits.** Inherited from the first skill-tidy session and now permanent rule (see [`docs/prompts/README.md` § Scope: no opportunistic edits to other files](./README.md#scope-no-opportunistic-edits-to-other-files)). If a non-sync-related issue surfaces in any skill during this session, capture it as a new `DEBT.md` row for a future tidy. Do not expand this session's scope.
- **Retrospective committed in the same PR**, at `docs/retrospectives/skills-tidy-ai-skills-sync.md`. Per the retrospectives README, this is a root-level retro because the session spans multiple skills + a methodology record.
- **Phase 5 retro tidy is in-scope, not opportunistic.** It is explicitly named in the Target artifacts row of the metadata block; it is the natural conclusion of the reconciliation thread this session continues. Close out its `Status` (`In progress` → `Complete`) and `Outcome` (`(to be filled in at session end)` → a one-paragraph summary derivable from the existing reconciliation table). Append a brief document-history line noting close-out by this session.

---

## Deliverable plan

1. **`docs/skills/wolverine-http-handlers/SKILL.md`** — verify against `81eede0` + `f7eb20e`.
   - Pre-prompt grep showed no `sad path` / `happy path` / `InvokeAsync` content; re-run the grep at session start to confirm no drift.
   - If the re-grep still shows no overlap, **no edits** to this file. Record the verification reasoning in the retro.
   - If overlap is found (e.g., a passage Phase 5 left in Cab that the upstream strengthen-commits sharpened), propose the targeted alignment to the user, pause for sign-off, then apply.
2. **`docs/skills/marten-projections/SKILL.md`** — verify against `da99a7a`.
   - Pre-prompt grep showed no `IncludeType<T>` content; re-run at session start.
   - If the re-grep still shows no overlap, **no edits**. Record the verification reasoning in the retro.
3. **`docs/retrospectives/skills-foundation-phase-5.md`** — close-out edits:
   - `Status: In progress` → `Status: Complete (closed 2026-05-22 by skills-tidy-ai-skills-sync session)`.
   - `Outcome: (to be filled in at session end)` → one-paragraph summary derived from the existing reconciliation table (39 skills audited; X trims applied; Y upstream-contribution candidates flagged; Cab's `See Also` three-block convention introduced).
   - Append a one-line entry under the existing structure (or a small `## Document history` section if none exists) noting the close-out date and the SHA that became the next sync floor.
4. **`docs/retrospectives/skills-tidy-ai-skills-sync.md`** — new retrospective. Sections per [`docs/retrospectives/README.md`](../retrospectives/README.md): metadata, framing, outcome summary, what worked, what was harder than expected, **`Spec delta — landed?` confirmed null per § Spec delta above**, outstanding items / next-session inputs. Record the sync floor SHA (`b0d0f7d`) and the upstream-skills inventory snapshot (75 skills total) so the next sync has a precise baseline.
5. **`docs/prompts/README.md`** — add a new entry under `### Multi-artifact prompts (root)` immediately after the housekeeping-delete-may-15-handoff line. Format mirrors the existing entries. Status starts as Pending; the session-runner updates to Complete in the same PR after the session lands.

---

## Out of scope

- **Adopting `added_in` (or any Cab analog) in Cab's skill frontmatter.** Separate methodology decision; deferred to a future micro-prompt if Cab's catalog grows large enough to warrant it. Captured for visibility, not acted on here.
- **Re-evaluating Cab's `wolverine-grpc-handlers` and `wolverine-grpc-bidirectional-handlers` against upstream.** Phase 5 disposition (Cab ahead of upstream; Erik planned upstream author) stands until the upstream `wolverine-grpc` skill publishes.
- **Adding `Upstream` cross-references in Cab skills to the new upstream migration skills.** No Cab section currently mentions Marten v8→v9 or Wolverine v5→v6 migration concepts; cross-refs without a host paragraph would be orphans. Revisit if a future Cab session reaches for migration content.
- **Lean-out work to reduce overlap with ai-skills.** Long-standing carve-out per `DEBT.md`. Separate scope.
- **Phase 6 placeholder cleanup** (the 14 skills tagged during Phase 5 reconciliation). Separate scope.
- **Style, clarity, or restructuring edits** to any Cab skill beyond what an upstream-sync row in § Upstream delta strictly requires. If a clarity issue is noticed mid-session, file a new `DEBT.md` row.
- **Re-trimming Cab skills that overlap upstream content Phase 5 already trimmed.** Phase 5's trim decisions are not re-litigated. If the strengthen commits sharpen an upstream passage Phase 5 deferred to upstream for, Cab inherits the sharpening passively (Cab points to upstream; upstream changed; the cross-reference still resolves correctly). No Cab edit needed.
- **Adding any new skill to Cab.** Sync sessions move content between layers, not into new layers.
- **Application code changes** in `src/CritterCab.Dispatch/` or any other source tree.

---

## Decisions to flag during the session

1. **If the re-grep of `wolverine-http-handlers/SKILL.md` finds zero overlap with the strengthen commits' content** (the expected outcome) — confirm the no-edit decision explicitly in the retro rather than treating "no edit" as failure to find work. The retro should name the verification path (grep terms + their hit counts) so the next sync can re-verify cheaply.
2. **If the re-grep of `marten-projections/SKILL.md` finds zero `IncludeType<T>` content** (the expected outcome) — same discipline. Confirm in the retro with verification path. Cab may legitimately not need to document `IncludeType<T>` if its projection examples consistently use concrete event types in their `Apply` signatures, which the upstream clarification explicitly notes is the case where `IncludeType<T>` is unnecessary.
3. **If either grep surfaces overlap** — propose the targeted alignment to the user, pause for sign-off, then apply. Do not adopt upstream's terminology shifts (e.g., "sad path" → "failure path", "happy path" → "success path") mechanically across Cab text; align only where the matched Cab passage is fundamentally upstream-derived rather than Cab-specific framing. Cab's voice is not subordinated to upstream's voice on Cab-specific content.
4. **Phase 5 retro `Outcome` paragraph wording** — derive from the existing reconciliation table. The retro already contains all the data; the close-out is a synthesis, not new information. Pause for sign-off before committing the synthesis.
5. **Whether to record an upstream-skills inventory snapshot in this session's retro** (75 skills, three new since Phase 5 close, named) — lean yes, as it makes the next sync's "what's new" trivially computable. Cost is one paragraph; benefit is the sync-floor discipline.

---

## What this prompt's retrospective should specifically capture

Beyond the standard retro shape, this session is the **methodology specimen** for future upstream-sync sessions (just as `skills-tidy-marten-and-bootstrap` was for DEBT-drain tidy sessions). Capture explicitly:

- **Did the sync floor SHA pattern work?** Recording `b0d0f7d` as the next sync's floor and capturing the upstream-catalog inventory at sync time. If this works, future sync sessions can be triggered by `git -C <ai-skills> log <last-sync-sha>..HEAD` returning a non-trivial content delta.
- **Was the verify-and-noop discipline natural or did it create pressure to find work?** If the latter, capture the methodology refinement for future sync sessions.
- **Did the pause-after-each-skill cadence remain valuable when the per-skill outcome was "no edit"?** If the cadence is overkill for verify-only steps, consider proposing a refinement (e.g., bundle verify-only steps into a single review pause, reserve per-skill pause for edits).
- **Was the upstream-delta categorization (content / infrastructure / catalog growth) the right lens?** Phase 5 used a per-skill audit lens; this session uses an upstream-commit-delta lens. The two are duals; both have a place. Capture which lens this session found more efficient and why.
- **Phase 5 retro close-out** — did closing it in this session feel like natural continuation of the reconciliation thread, or did it feel like opportunistic scope expansion? The metadata block explicitly named it in-scope; the retro should confirm whether that naming was a correct read of the session-and-PR-cadence rule's "same-thread" boundary.

These observations feed the methodology log and any future sync-session prompt template.
