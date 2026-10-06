# Prompt — CritterCab v2 boundary: close v1, declare v2 in place

| Field | Value |
|---|---|
| **Status** | **DRAFT, UNFROZEN.** Do not execute. Becomes Ready only after Erik records the decision in [`docs/planning/2026-10-05-crittercab-v2-evaluation.md`](../planning/2026-10-05-crittercab-v2-evaluation.md) § Decision and resolves the bracketed owner calls below. Authored 2026-10-05 in the v2 evaluation session. |
| **Authored** | 2026-10-05 (draft) |
| **Target artifacts** | Git tag at `4d8bde4`; `docs/vision/README.md` (v1.0, from [`docs/research/crittercab-v2-vision-draft.md`](../research/crittercab-v2-vision-draft.md)); `docs/decisions/README.md` (v1-record preface; no ADR authored); `docs/planning/2026-MM-DD-released-forward-constraints.md` (new); `README.md`, `CLAUDE.md`, `AGENTS.md` (rewritten from the running code, collapsed to one source, no status or version claims); `docs/workshops/README.md` (minutes rule; stale "Complete"/"In progress" wording removed); `docs/rules/structural-constraints.md` (ADR-017 carve-out); `src/CritterCab.Dispatch/README.md`, `src/CritterCab.Telemetry/README.md` (rewritten from code); `docs/workshops/001`, `004`, `005` (one closing Document History line each); `CritterCab.AppHost/` (new project replacing `apphost.cs`) and `CritterCab.slnx`; `Directory.Packages.props` (pruned; Wolverine bumped); `protos/buf.gen.yaml` (Go output removed); `.github/workflows/dotnet.yml` (apphost build, `buf lint`, enforced pre-pull); `.github/dependabot.yml` (new); `docs/skills/archive/` (fourteen skills moved); `docs/skills/event-modeling/SKILL.md` (corrected); `docs/skills/DEBT.md` (cleanup rows drained, decision rows resolved); `tests/**` (test classes renamed to slice names; no behaviour change); `.claude/settings.local.json`, `.claude/agents/*`, `.codex/agents/*` (trimmed or corrected); this prompt's retro. |
| **Source-of-truth dependencies** | The evaluation's § Decision; the comparison's § 4 summary and residual answers; the vision draft; ADR-002, 003, 005, 010, 017, 018, 019; `docs/prompts/README.md` § Session and PR cadence (this session invokes the named exception); the running code in `src/` and `tests/`. Skills: `aspire`, `service-bootstrap`, `testing-integration`, `testing-fundamentals`, `protobuf-contracts`, `event-modeling`. |
| **Workflow position** | First v2 session. Declared cadence exception: one prompt, one session, **two PRs if the Wolverine bump or the apphost conversion breaks a suite** (documents PR, then build PR); otherwise one. No handler appears in the diff. Followed by the Driver Profile canvas chapter, then the first declared files [owner call: timing relative to CritterMart's Orders experiment]. |

---

## Framing — why this session exists

The 2026-07-25 handoff said the next session must be a design-or-tidy return. The portfolio decided on 2026-10-05 that the Event Model's authored surface is a committed curated file, that status is never asserted, and that markdown workshops are minutes. The v2 evaluation found that CritterCab's code is a seed worth keeping and that what needs versioning is what the repository claims about itself: an eleven-context inventory, three transports with one built, seven packages pinned for code that does not exist, fourteen skills for technology with no code behind them, and routing files that describe a repo from three PRs ago. This session draws the line. It changes no handler, moves no workshop, retro or ADR, and writes no ADR.

## Goal

Close v1 at `4d8bde4` with a tag and declare v2 in place: a vision v1.0, an ADR-index preface, routing files that assert nothing, a build that CI can see in full, a package list with no pin that lacks a referencing project, a skills library with no skill that lacks exercising code, and test classes named by slice so the first declared files have stable keys.

## Spec delta

- No slice moves between declared and realized; this session touches no model content.
- `docs/vision/README.md` moves from v0.8 (eleven tentative contexts) to v1.0 (five contexts, five deployables, build order; goals re-ranked; admission rule).
- Every forward-constraint and ADR candidate addressed to a context outside the v2 build list is released, in one dated note, with its origin cited.
- ADRs 001 to 019 are declared the v1 record in the index preface; no ADR's status field changes in this session.

## Orientation files (read in order)

1. This prompt end to end, including § Owner calls.
2. `docs/planning/2026-10-05-crittercab-v2-evaluation.md` § Decision, then Pass 2 and Pass 4.
3. `docs/research/crittercab-v2-evaluation-comparison.md` § 3 (uniquely-caught items) and § 4.
4. `docs/research/crittercab-v2-vision-draft.md` (the text to make live, with brackets resolved).
5. `docs/prompts/README.md` § Session and PR cadence (the exception this session invokes) and § Scope.
6. `docs/planning/2026-07-25-post-pr47-w006-complete-design-return-due.md` § Things not captured in any artifact (items 1, 4, 6).
7. `src/CritterCab.Dispatch/Program.cs`, `src/CritterCab.Telemetry/Program.cs`, both `.csproj` files, `apphost.cs`, `Directory.Packages.props`, `.github/workflows/dotnet.yml`: the ground truth the routing files are rewritten from.
8. `docs/skills/DEBT.md` (all eighteen rows) and `docs/skills/README.md` § Companion: JasperFx ai-skills.
9. `docs/skills/event-modeling/SKILL.md` against `docs/research/event-modeling-canonical-sources.md` (author names, building blocks, patterns).

## Working pattern

- Branch off `main`; never commit to `main`. Tag `4d8bde4` **before** the first change on the branch [owner call: tag name; `v1` is the plain choice].
- Dispose of the two open PRs first and separately: merge #44, close #43 with a comment pointing here.
- Verify before wiring (`jasperfx-source-verifier` at prompt-freeze time, HEAD of each checkout logged in this prompt): the Wolverine target version's `WolverineFx.Grpc` client-streaming auto-generation still holds; `Aspire.AppHost.Sdk` project shape for the target Aspire version; `buf` CI action and version.
- Rewrite routing files **from the code, not from prior routing files**; delete every sentence that asserts progress, version, or completeness.
- Renaming test classes: mechanical, one commit, no assertion changes; the suite count (54 methods, 57 cases) is the invariant.
- Run the gRPC and Kafka suites locally after the Wolverine bump and after the apphost conversion; if either breaks, split into two PRs at that point and say so in the retro.
- Skill-auditor Phase 1 before touching `docs/skills/`, Phase 2 after; `code-review` before opening the PR even though no handler changes (the two-axis form has caught doc-to-code contradictions before).
- No opportunistic edits beyond the target-artifact list; anything surfaced goes to `DEBT.md`.
- Surface the full PR URL(s) on open.

## Deliverable plan (in order)

1. Tag; branch protection on `main`; dispose of #43 and #44.
2. `docs/vision/README.md` v1.0; `docs/decisions/README.md` preface; the released-forward-constraints note; `docs/workshops/README.md` minutes rule; closing Document History lines on W001, W004, W005; `docs/rules/structural-constraints.md` carve-out.
3. `README.md`, `CLAUDE.md`, `AGENTS.md` collapsed and rewritten; both service READMEs rewritten.
4. `CritterCab.AppHost` project replacing `apphost.cs`; slnx updated; CI builds it; CPM re-enabled for it; Aspire pins reconciled.
5. `Directory.Packages.props` pruned to referenced packages; Wolverine bumped; `buf.gen.yaml` Go output removed; `buf lint` in CI; dependabot; pre-pull list enforced (a test that reads the fixtures' image names and asserts the workflow lists them, or a generated step).
6. `docs/skills/archive/` with the fourteen code-less skills; `event-modeling` corrected (Marc Klefter, Jake Bruun; Dymitruk's four patterns first-class; information completeness named; Dymitruk-to-JasperFx mapping table; examples aligned with W001 and W004); DEBT drained and decision rows resolved (test naming dissolves with slice names; collection convention amended to the four shipped collections; `identity-acl` row archived with its skill).
7. Test classes renamed to slice names.
8. Agent configs and `.claude/settings.local.json` trimmed.
9. Retro, inside the PR.

## Out of scope

Any handler, projection, document or endpoint. Any workshop content. Any `*.emodel.yaml` (a later session, timed by the owner). Any ADR (ADR-020, the v2 topology, is proposed in the evaluation and needs sign-off). Driver Profile. Polecat, SQL Server, the ASB emulator, CritterWatch and RabbitMQ (all later arcs). Narratives. Package bumps other than Wolverine and what the apphost conversion requires.

## Owner calls to resolve before freezing

[Tag name and whether "v2" appears publicly.] **Resolved 2026-10-05:** ASB kept, built at the availability slice. **Resolved 2026-10-05:** Pricing folds into Trips. [Identity's form.] [Go: parked with trigger, or dropped.] [CritterWatch licence and whether RabbitMQ enters the apphost in this session or later.] [Retro template: full or shortened.] **Resolved 2026-10-05:** declared files are authored after CritterMart's Orders experiment reports; only the slice renaming is in this session.

## Follow-on sessions

Driver Profile chapter on the canvas (grill limited to the four-transitions and availability-before-location questions; wireframes; field-level completeness; export committed). First declared files for Dispatch and Telemetry from the realized slices, with two bindings. Driver Profile skeleton (Polecat). Transport groundwork (ASB emulator or Kafka edge). The "driver comes online" slice.
