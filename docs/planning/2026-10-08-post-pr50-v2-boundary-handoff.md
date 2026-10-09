# CritterCab handoff — after PR #50 (v2 boundary merged)

**Written:** 2026-10-08 · **Repo:** `C:\Code\CritterCab`, `main` @ `547f082` ("CritterCab v2 boundary: close v1, declare v2 in place (#50)"), clean, only `main` locally · **PR:** https://github.com/erikshafer/CritterCab/pull/50 (merged, CI green)

## Where things stand (read these, not this summary)

- What v2 is and the build order: `docs/vision/README.md` (v1.0).
- What the boundary session did and found: `docs/retrospectives/crittercab-v2-boundary.md` (five-section template; its "Next-session inputs" is the authoritative list).
- The decision record behind v2: `docs/planning/2026-10-05-crittercab-v2-evaluation.md` § Decision + 2026-10-08 addendum.
- Promises the minutes made to contexts v2 does not build: `docs/planning/2026-10-08-released-forward-constraints.md`.
- Repo map for agents: `AGENTS.md` (`CLAUDE.md` is just `@AGENTS.md`).
- `v1` tag is at `4d8bde4`.

## Next design session: the Driver Profile chapter (run from the Claude Desktop agent)

This is a modelling session with Erik on the **EventModelers.AI canvas**, not repo work. It is the first chapter modelled under the v2 method, and it feeds the first v2 slice ("driver comes online": a Driver Profile Command producing `DriverCameOnline`, translated into the availability side of Dispatch's `AvailableDriver`, after which W001 §5.3's happy path passes end to end).

**Shape the boundary prompt fixed for it** (`docs/prompts/crittercab-v2-boundary.md` § Follow-on sessions, item 1):

1. **Grill first, limited to two boundary questions only**, so slice discovery happens on the board:
   - The **four transitions** (`DriverCameOnline`, `WentOnBreak`, `WentOffline`, `VehicleChanged`) against the collapsed placeholder `DriverAvailabilityChanged` (`src/CritterCab.Dispatch/AvailableDrivers/DriverAvailabilityChanged.cs`, whose header comment explains it mirrors `AvailableDriver`'s four availability-side fields 1:1). Replacing it must be a swap, not a migration.
   - The **availability-before-location drop**: today the availability handler drops the event when no `AvailableDriver` exists yet (location has not arrived). Decide: Driver Profile republishes on demand, or Dispatch buffers. An existing test pins the current drop.
2. **A real seven-step pass** (Brain Storming, The Plot, The Story Board, Identify Inputs, Identify Outputs, Apply Conway's Law, Elaborate Scenarios), with a **wireframe or an explicit "no screen: machine actor" statement per slice**, and the **field-level information-completeness check** ("every field has an origin and a destination"). Method and slice definition of done: `docs/skills/event-modeling/SKILL.md` (corrected this session; body above "In this repository" is repo-neutral).
3. **Outputs:** the canvas export committed; markdown minutes under `docs/workshops/` with the next number (**007**), written as minutes (the minutes rule is at the top of `docs/workshops/README.md`); `docs/context-map/README.md` updated in the same PR if a cross-context edge changes (the Driver Profile → Dispatch edge will).

**Inputs to have open:** W006 §6.5 and ADR-018 (the join Dispatch owns), W001 §5.3 (candidate selection; anticipates the four transitions), `docs/research/ride-sharing-lessons-learned.md`, the released-constraints note (Onboarding is parked; its W004 §X+1 #4–#5 asks of Driver Profile are parked, so Driver Profile's first chapter does not depend on Onboarding). Vehicle sub-domain folds into Driver Profile (vision). Store will be **Polecat on SQL Server**, but that enters at the later skeleton PR, not in this session. Transport for availability is **Azure Service Bus** (built at the slice; ADR-005/014/018/019).

**Decisions still open that this chapter touches:** `GeoLocation` value-object shape (DEBT row re-pointed here: per-context value objects with boundary translation, or raw degrees at the H3 layer). Do not author `driverprofile.emodel.yaml` yet: declared files wait for CritterMart's Orders experiment to settle placement, naming and schema.

**Sequence after the chapter** (vision / prompt § Follow-on): first declared files for Dispatch and Telemetry → Driver Profile skeleton PR (Polecat, SQL Server, AppHost + CI pre-pull changes; Apple Silicon isolation note) → transport groundwork PR (ASB emulator + SQL Edge) → the "driver comes online" slice → CritterWatch wiring.

## Small queue (back in Claude Code, any order, none blocks the chapter)

- **Branch protection on `main` (Erik, manual).** Was blocked in-session. Settings: require a PR (0 approvals), require the `Build & Test` check, block force pushes and deletions, don't enforce for admins (also in the PR #50 description).
- **Dependabot triage (5 open PRs):**
  - #51 wolverinefx group (9 updates): gate on the full suite; re-check `Directory.Packages.props`' gRPC pin comment ("pinned to exactly what WolverineFx.Grpc declares") against the new version's own pins.
  - #53 grpc group (3 updates): **close unless WolverineFx moves with it**; the gRPC pins must match WolverineFx.Grpc's declared dependencies. Consider folding the `grpc` group into `wolverinefx` in `.github/dependabot.yml` (a `tidy: ci`).
  - #52 aspire group: Dependabot moves `Directory.Packages.props` only; bump the SDK version in `src/CritterCab.AppHost/CritterCab.AppHost.csproj`'s `Project` element by hand in the same PR.
  - #54 testcontainers group: re-read the ryuk tag Testcontainers pins and update the `docker pull testcontainers/ryuk:…` line in `.github/workflows/dotnet.yml`; also likely clears a `NU1903` (SSH.NET) advisory.
  - #55 Confluent.Kafka 2.14.0 → 2.16.0: the comment in `Directory.Packages.props` says it is pinned "at the version Wolverine resolves"; check that before merging.
- **Remote branches** `tidy/ci-bump-actions-v5` and `tidy/housekeeping-claude-md-status-refresh` (PRs #44, #43, closed) still exist on origin; delete if wanted.
- **DEBT tidy rows** (`docs/skills/DEBT.md` § Open debt, ~15 rows): the biggest is the archived-skill-names sweep across ~24 surviving skills; others are file-based-AppHost residue in `adding-a-service` / `cli-aspire`, Go and governance residue in `cli-grpc-tooling`, stale topic/port details in `cli-kafka-tooling`, stale revision comments in Dispatch source, a `Money` decision in `protobuf-contracts`. Per the design-return cadence, interleave rather than batch-run.
- **Out-of-scope drift noted, not filed:** `docs/research/event-modeling-canonical-sources.md:57` says Dymitruk's step 3 is "time travel" (it is "The Story Board"); `docs/context-map/README.md:177` says "six of the eleven BCs"; W006 §11 #1 still lists the Kafka topic-naming candidate as pending though it is ADR-019 (minutes are append-only; the context map is not).

## Conventions that bit or mattered this session

- One prompt, one session, one PR; the retro rides in the PR in the **five-section template** (Metadata; What landed; Spec delta landed?; What disconfirmed; Next-session inputs). Cross-cutting lessons → `docs/research/methodology-log.md` (entry 007 is the newest); skill gaps → `DEBT.md`.
- Rule of admission: nothing pinned, wired in the AppHost, written as a skill, or called committed without exercising code in the same PR or the next.
- Routing files assert no status, version or counts; "v2" appears only in the vision text and the tag message.
- Commits and PRs carry no Claude attribution. Always surface the full PR URL.

## Suggested skills

- **For the Driver Profile chapter (Desktop agent):** `grill-with-docs` (limited to the two boundary questions above), the repo's `docs/skills/event-modeling/SKILL.md` (read it, it's not a registered skill there), `domain-modeling`, and, if available, `anthropic-skills:event-modeling-workshop`.
- **For Dependabot triage (Claude Code):** the `jasperfx-source-verifier` agent to read the new WolverineFx's pinned gRPC / Confluent versions; `post-merge` after each merge.
- **For a DEBT tidy:** the `critter-skill-auditor` agent (Phase 1 before, Phase 2 after), `code-review` before the PR.
- **Close-out:** `post-merge` → `handoff` → `blurb`.
