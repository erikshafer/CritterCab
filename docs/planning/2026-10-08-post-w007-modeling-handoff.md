# CritterCab handoff — after the W007 Driver Profile modelling session

**Written:** 2026-10-08 · **Repo:** `C:\Code\CritterCab`, `main` @ `547f082`, clean. **This session changed nothing in the repo**; it was a design session (grill + seven-step Event Modeling pass) held in text with Erik.
**Previous handoff (still valid for everything not covered here):** [`docs/planning/2026-10-08-post-pr50-v2-boundary-handoff.md`](./2026-10-08-post-pr50-v2-boundary-handoff.md); its "Small queue" (branch protection, Dependabot #51–#55, stale remote branches, DEBT tidy rows, noted drift) is **untouched and still open**.

## The one artifact to read

**[`docs/planning/2026-10-08-w007-driver-profile-modeling-ledger.md`](./2026-10-08-w007-driver-profile-modeling-ledger.md)** is the complete record of the session. It covers every grill decision with the rejected alternatives, steps 1–7 with field origins and destinations, the completeness findings F1–F6, decisions D1–D4, the final names of the 11 slices, the Given/When/Then highlights, the revisions to earlier documents, the items handed forward, the code changes the implementation will need, the ASB verification, and the outputs still owed. **Build from it; don't re-derive it or re-grill.** It sits beside this handoff in `docs/planning/`.

**Git state of these notes:** this handoff, the ledger and the post-PR #50 handoff were copied into `docs/planning/` **untracked** on `main` (no commit, since there are no direct commits to `main`). Commit them on the minutes PR's branch: the ledger is that PR's source, and W006's grill record (`2026-06-25-w006-telemetry-grill-resolutions.md`) set the precedent for keeping it.

The full Given/When/Then text is in the session transcript. The ledger holds faithful summaries of every scenario, which is enough to rewrite them in minutes form.

## Next session: the W007 minutes PR

One prompt, one session, one PR (`docs/prompts/README.md` § Session and PR cadence). Author the prompt first, and put the retro in the PR using the five-section template (`docs/retrospectives/README.md`). Branch off `main`.

**Deliverables. Ledger § "Outputs still owed" is authoritative; summary:**
1. **The canvas export from EventModelers.AI, committed.** ⚠ The session ran in text. **Ask Erik whether the board has been drawn.** If it hasn't, drawing it from the ledger is the first job, and the export has to exist before the minutes are written.
2. `docs/workshops/007-driver-profile-event-model.md`, written as **minutes** (the rule is at the top of `docs/workshops/README.md`; follow the structure of W006). It includes the **amendment table for W001 §5.3 and W006 §6.5** (D3). W001 and W006 are append-only and are **not** edited.
3. `docs/context-map/README.md`: edge #6 Driver Profile → Dispatch goes from **dashed to solid**, as Customer–Supplier plus Published Language (D2). Also record the Identity-arc handover on edge #3 (ledger § Step 6).
4. **The ADR-014 amendment (Erik decided it rides in this PR).** Limit the ordering guarantee to "within one subscription and one session". Replace the cross-topic claim with "a consumer needing order across topics rebuilds it from a server-stamped time". Cite Microsoft's `message-sessions.md` (verified with ctx7 `/microsoftdocs/azure-docs`; evidence in the ledger). **Get Erik's sign-off on the wording.** Use the format in `docs/decisions/`.
5. **Re-affirm ADR-013.** Driver Profile is the first context to mint `driverId` in the driver lifecycle. The v1-ADR rule is in AGENTS.md and in the preface of `docs/decisions/README.md`.
6. **Don't author `driverprofile.emodel.yaml`.** Declared files wait for CritterMart's Orders experiment.

**Also record in the minutes:** the methodology observations in the ledger (Erik ratified every recommendation from step 1 on, which the skill says is itself a finding, along with where the board corrected the grill), and the vision's Driver Profile scope being only partly covered (service area is deferred). Cross-cutting lessons go to `docs/research/methodology-log.md` as the next entry after 007.

**Out of scope for that PR:** any `src/` or `tests/` change. The code changes listed in the ledger (deleting the placeholder, making the location side nullable, the resolution-read null filter, flipping the pinning test, the wire guards) belong to the "driver comes online" slice PR, after the skeleton and transport-groundwork PRs (sequence in the previous handoff).

## Sequence after the minutes (unchanged from the previous handoff)

First declared files for Dispatch and Telemetry → Driver Profile skeleton PR (Polecat, SQL Server) → transport-groundwork PR (ASB emulator, SQL Edge) → the "driver comes online" slice (its acceptance scenario is in the ledger, step 7) → CritterWatch wiring. The **ghost driver** (ADR-018's deferred staleness ceiling) is now due on the Dispatch side; schedule it, and don't let it vanish.

## Conventions

- No Claude attribution on commits or PRs. Always surface the full PR URL.
- Routing files state no status. Minutes are minutes, not the record.
- Nothing is admitted ahead of code: no protos under `protos/crittercab/driverprofile/` until the slice that compiles them.

## Suggested skills

- **For the minutes PR:** read `docs/skills/event-modeling/SKILL.md` (a repo file, not a registered skill) for minutes vocabulary and the slice definition of done. `domain-modeling` helps with UL and context-map wording. Use `code-review` before opening the PR. Close out with `post-merge` → `handoff` → `blurb`.
- **If the canvas still needs drawing:** `claude-in-chrome` (load the browser tools in one ToolSearch call) to work on the EventModelers.AI board with Erik, and `anthropic-skills:event-modeling-workshop` if it's available.
- **For the ADR-014 wording:** use the `ctx7` CLI (Erik's global rule) with `/microsoftdocs/azure-docs` if more ASB evidence is needed.
- **For the small queue (separate sessions):** the `jasperfx-source-verifier` agent for Dependabot pin checks, and the `critter-skill-auditor` agent for DEBT tidies.
