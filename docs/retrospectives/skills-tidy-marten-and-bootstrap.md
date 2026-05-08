# Retrospective — Skill Tidy: Marten 8.x / JasperFx Namespaces and Service-Bootstrap Registrations

## Metadata

- **Triggering prompt:** [`docs/prompts/skills-tidy-marten-and-bootstrap.md`](../prompts/skills-tidy-marten-and-bootstrap.md)
- **Status:** Complete
- **Date authored:** 2026-05-08
- **Output artifacts:**
  - `docs/skills/marten-projections/SKILL.md` — added "Namespaces" cheat-sheet table
  - `docs/skills/marten-wolverine-aggregates/SKILL.md` — added one-row "Namespaces" cheat-sheet
  - `docs/skills/service-bootstrap/SKILL.md` — added "Service that exposes Wolverine.HTTP endpoints" subsection; added two Common Pitfalls bullets (`TimeProvider` and `AddWolverineHttp()`); fixed one residual "Oakton CLI surface" prose drift
  - `docs/skills/DEBT.md` — drained all 7 open rows; populated "Recently drained" section
  - `docs/prompts/skills-tidy-marten-and-bootstrap.md` (new) — the prompt that triggered this session
  - `docs/prompts/README.md` — added the new prompt to the index
- **Outcome:** First tidy session complete. All 7 currently-open DEBT.md rows drained in one PR. Methodology specimen captured for future tidy sessions; this is the first exercise of the dedicated `tidy: skills` PR convention introduced in [`docs/prompts/README.md`](../prompts/README.md#session-and-pr-cadence).

---

## Framing

The post-D→B→C session (PR #4) surfaced 7 skill-file gaps and registered them in [`docs/skills/DEBT.md`](../skills/DEBT.md). This session drained the entire backlog in one PR — small enough to be reviewable, related-enough by root cause (Marten 8.x / JasperFx namespace extractions + Wolverine HTTP/DI registration prerequisites) to be coherent.

This is also the first interleave under the design-return cadence rule from [`docs/prompts/README.md`](../prompts/README.md#session-and-pr-cadence). After the D→B→C run of three implementation-adjacent sessions, the next PR is intentionally not another implementation slice.

---

## Outcome summary

| Skill | Rows drained | Approach |
|---|---|---|
| `marten-projections` | 4 | New "Namespaces" cheat-sheet table near the top of the skill |
| `marten-wolverine-aggregates` | 1 | New small "Namespaces" cheat-sheet (one row, with deferral note) |
| `service-bootstrap` | 2 | New "Wolverine.HTTP endpoints" subsection in Per-Service Configuration Variation; two new Common Pitfalls bullets |

Plus one prose-pass: `service-bootstrap` line 326 "Oakton CLI surface" → "JasperFx CLI surface" (residual drift from PR #4's in-flight `RunOaktonCommandsAsync` → `RunJasperFxCommands` code-block fix; flagged in the prompt as decision-to-flag #3).

`Open debt` reset to empty. `Recently drained` populated with the per-skill summary. Document history extended.

---

## What worked

- **Source-of-truth precedence (working code → retro → external docs).** This was the prompt's most important rule and it paid off immediately. The retro claimed `marten-projections` showed `SingleStreamProjection<T>` (single type parameter) and that this needed correction. Reading the skill body showed it already used `SingleStreamProjection<TDoc, TId>` (two type parameters) consistently. The working code in `RequestTimeline.cs` confirmed two parameters is the actual API. **Without the precedence rule, the tidy would have re-fixed something already fixed.** This single observation is the strongest argument for keeping tidy sessions as a deliberate practice rather than an ad-hoc one.
- **Per-skill batching with one PR.** All 7 fixes across 3 skills landed in commits granular enough to per-skill revert, but bundled in one PR for review. Felt right for a backlog this size.
- **The "Namespaces" cheat-sheet pattern.** Adding a small table near the top of each affected skill is more durable than scattering namespace mentions through the prose, and gives future tidies a predictable place to add rows.
- **The DEBT.md "Out of scope" carve-out.** The temptation to expand into ai-skills lean-out work was real (Erik mentioned at conversation start "we have more tidying to do"). The carve-out gave a concrete reason to defer: that's not what this session is for.

---

## What was harder than expected

- **Retro evidence that doesn't survive a re-read.** One of the four `marten-projections` DEBT rows was partially superseded by the time this session ran — the type-parameter shape was already correct in the skill body. The DEBT row was registered from the retro, not re-verified against the skill at registration time. This isn't a process failure (DEBT registration happened correctly per the convention as written), but it does mean tidy sessions have to verify each row against current skill state, not just retro text.
- **Counting the backlog.** The original DEBT.md document-history line said "Five entries... four `marten-*` + two `service-bootstrap`" — both numbers were wrong. Actual count: 4 + 1 + 2 = 7 rows across 3 skills. The error was authored in the same conversation as the prompt and propagated because no counter-pass was done at registration. Corrected in the drain commit.

---

## Methodology refinements that emerged

The prompt's "What this retro should specifically capture" section asked for explicit answers on four questions. Below.

### 1. Did the "no opportunistic edits" rule hold, and at what cost?

**Mostly held — one judgment call.** The doc-history line of DEBT.md said "Five entries" when the actual count was 7. I corrected the line as part of the drain commit. This is technically opportunistic — the count typo wasn't a DEBT row, just authoring drift in a meta-line of the same file being drained. The judgment was: factual errors in the same file as a session's actual deliverable are reasonable to correct in-flight, even when they aren't the session's named scope.

**Recommendation for the next tidy:** the rule should be tightened to "no opportunistic edits to *other files*." Edits to the file actively being modified — including correcting factual errors authored in the same conversation — are in-bounds. Worth lifting into the DEBT.md conventions section.

### 2. Was one PR for all 7 fixes the right scope, or would per-skill PRs have been better?

**One PR was right for 7 rows across 3 skills.** All 7 share root causes (Marten 8.x / JasperFx extractions for the `marten-*` rows; Wolverine.HTTP and DI prerequisites for the `service-bootstrap` rows). Splitting per-skill would have produced three short PRs that each took roughly the same review effort as one combined PR.

**Recommendation:** for tidies up to ~10 rows across ≤3 skills, one PR. For tidies that span more skills or have unrelated root causes, split per-skill (one PR per affected skill). DEBT.md's existing convention ("Group rows by skill file. A tidy session can then plan one PR per affected skill rather than touching everything at once") already accommodates both modes; the threshold is the new piece worth recording.

### 3. Was the source-of-truth precedence (working code → retro → external docs) sufficient?

**Yes, and it was load-bearing.** The marten-projections type-parameter row would have been re-fixed without the precedence rule. The retro evidence was a snapshot at session close; the skill body had moved on; only the working code was up-to-date.

**Recommendation:** lift the precedence into the DEBT.md conventions section as a tidy-session expectation. Suggested phrasing: *"Tidy sessions verify each row against current skill state and current working code before fixing; the retro is evidence the gap once existed, not proof it still does."*

### 4. Which conventions should be lifted into permanent rules?

Three candidates emerged:

- **The "Namespaces" cheat-sheet pattern** for skills that document type-heavy APIs across multiple namespaces. Could become an optional section in `docs/skills/_template/SKILL.md`. Future tidies that surface namespace gaps then add rows to existing tables rather than authoring new sections.
- **The "no opportunistic edits to other files" rule** (refinement of the prompt's "no opportunistic edits"). Update `docs/prompts/README.md`'s Session and PR cadence section, or DEBT.md's conventions section.
- **The source-of-truth precedence rule for tidy sessions.** Add to DEBT.md's conventions section.

These three refinements are **not authored in this session** — they are inputs to a follow-up housekeeping prompt, the next tidy's prompt, or the workshops/README §12.8 follow-ups micro-PR.

---

## Outstanding items / next-session inputs

- **Methodology refinements above** are inputs to the next housekeeping pass. None blocks the next session.
- **The lean for the next session** per the post-D→B→C handoff and the design-return cadence rule: **A — Trips workshop**. The skill-tidy interleave is now done; the next major design session is Trips. The Trips workshop will be the second specimen for methodology hardening (workshop-level conventions ossifying into reusable patterns) and will consume the proto contracts authored in PR #4 from the receiver's side.
- **The workshops/README §12.8 follow-ups index** proposed during prompt sketching is still pending. Small enough to ride with the Trips workshop's prompt or be its own micro-PR; not load-bearing.
- **Phase 6 placeholder cleanup** (the 14 skills tagged during Phase 5) and **lean-out work to reduce overlap with JasperFx ai-skills** remain explicitly out of scope per DEBT.md's carve-out. Both have their own scope and own potential prompts.

---

## Quantitative summary

- **Skills modified:** 3 (`marten-projections`, `marten-wolverine-aggregates`, `service-bootstrap`)
- **Rows drained:** 7
- **Commits:** 6 across the branch (1 prompt-only commit by Erik; 1 prompts/README index-update fix-up; 3 per-skill `tidy:` commits; 1 closing commit with DEBT drain, retro, and retros/README index update). Original plan was 5 with prompt + index bundled; the prompt landed in its own commit before the index work began, so they ended up split.
- **Edit footprint:** small. Two new `## Namespaces` section headers; one new `### Service that exposes Wolverine.HTTP endpoints` subsection; two new Common Pitfalls bullets; one prose substitution. Zero structural restructuring.
- **Out-of-scope items deferred:** ai-skills lean-out, Phase 6 placeholder cleanup, cross-skill consistency reconciliation, opportunistic style edits, the workshops/README §12.8 follow-ups index.
