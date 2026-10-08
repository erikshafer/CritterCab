# Retrospective — CritterCab v2 boundary: close v1, declare v2 in place

## Metadata

- **Triggering prompt:** [`docs/prompts/crittercab-v2-boundary.md`](../prompts/crittercab-v2-boundary.md) (frozen at Ready, 2026-10-08)
- **Status:** Complete
- **Date:** 2026-10-08
- **PR:** one PR from `docs/v2-boundary` (no split: the suite stayed green after the Wolverine bump and after the AppHost conversion). Outside the PR: tag `v1` pushed; #43 and #44 closed; branch protection left as a manual step (see Next-session inputs).
- **Files:** `src/CritterCab.AppHost/` (new: `CritterCab.AppHost.csproj`, `AppHost.cs` moved from `apphost.cs`, `Properties/launchSettings.json` moved from the root); `CritterCab.slnx`; `Directory.Packages.props`; `protos/buf.gen.yaml`; `.github/workflows/dotnet.yml`; `.github/dependabot.yml` (new); ten test files renamed under `tests/`; `.codex/agents/*.toml` and `.codex/agent-memory/*/.gitkeep` (new); `README.md`, `AGENTS.md`, `CLAUDE.md`; `src/CritterCab.Dispatch/README.md`, `src/CritterCab.Telemetry/README.md`; `docs/vision/README.md`; `docs/decisions/README.md`; `docs/planning/2026-10-08-released-forward-constraints.md` (new); `docs/workshops/README.md` and W001, W004, W005; `docs/rules/structural-constraints.md`; `docs/research/README.md`, `crittercab-v2-vision-draft.md`, `sdd-event-model-to-code.md`; `docs/skills/archive/` (fifteen skills moved, `README.md` new); `docs/skills/README.md`, `DEBT.md`, and the skills `event-modeling`, `aspire`, `service-bootstrap`, `marten-wolverine-aggregates`, `wolverine-marten-automation`, `wolverine-http-handlers`, `testing-fundamentals`, `testing-integration`, `vertical-slice-organization`, `protobuf-contracts`, `domain-event-conventions`, `transport-selection`; `docs/retrospectives/README.md`; `docs/prompts/README.md`; this file.

## What landed

| Deliverable | Result and evidence |
|---|---|
| Tag | `v1`, annotated, at `4d8bde4`, pushed. The message points at the evaluation and says that no code changed after `327916d` (#47). |
| Open PRs | #44 did not rebase onto `main` (conflict in `docs/prompts/README.md`), so it was closed and its four Actions bumps were folded into `dotnet.yml` (`checkout@v7`, `setup-dotnet@v6`, `cache@v6`, `upload-artifact@v7`, still the current majors on 2026-10-08). #43 was closed with a pointer to this prompt. |
| AppHost project | `src/CritterCab.AppHost/` replaces `apphost.cs`: identical resources, ports and comments; the launch profile moved in; it is in the slnx, so CI builds it. CPM applies. **Suite: 57/57.** |
| Build tidy | WolverineFx 6.21.0 → 6.38.0 (Marten 9.35.0 and JasperFx 2.69.3 resolve transitively, exactly as gate 3 predicted); no code change. Seven pins pruned; the set difference between pins and `PackageReference`s is now empty both ways. Go output removed from `buf.gen.yaml`. CI gained a `buf lint` step and a pre-pull guard. Dependabot added. **Suite: 57/57.** |
| Pre-pull guard | Green on the real tree. Red, by design, on three mutations: a fixture tag bump, a prefix-only match in the pull list, and a `.WithImage("…")` fixture. |
| `buf lint` | The first time it has ever run against this repo: buf 1.73.0 in Docker, exit 0 on all seven protos. |
| Test renames | Ten classes and files, 22 lines changed (class names, constructors, two comments). Invariant held: 54 `[Fact]`/`[Theory]` methods, 57 cases, all green. |
| Agent configs | `critter-skill-auditor` now globs `.agents/skills/**`; all four Codex agents write memory to `.codex/agent-memory/<agent>/`, created with a `.gitkeep`. |
| Documents | Vision v1.0 live (the fused v0.3 bullet on line 1 dropped, v0.1 to v0.8 carried verbatim); ADR-index preface; released-forward-constraints note; workshops minutes rule; closing lines on W001, W004, W005; ADR-017 carve-out in the rules; research index row and closing note. |
| Routing files | README, AGENTS.md (the content) and CLAUDE.md (`@AGENTS.md`), plus both service READMEs, rewritten from `Program.cs` and the slice folders. No version, count, status or "complete"; "v2" appears in none of them. The README's stale route (`POST /ride-requests`) became the real one (`POST /api/rides/request`). |
| Skills | Fifteen skills archived with an index. `event-modeling` corrected (author names; Dymitruk's four patterns first-class, with Klefter and Bruun as refinements; the seven steps with "The Story Board" as step 3; the field-level completeness check; a slice definition of done; one Dymitruk-to-JasperFx mapping table; every CritterCab example moved to a final section, so the body copies verbatim). Seventeen of eighteen DEBT rows drained, archived or closed by the workflow (see `DEBT.md` § Recently drained). The skills README was rebuilt across its four surfaces. |
| Retro template | Five required sections, methodology-log and DEBT routing, the one-line index rule. This retro is its first use. |

### Test-class rename mapping (merge keys for the declared-file session)

| Before | After | Minutes |
|---|---|---|
| `Slice1TelemetryPolicyTests` | `TelemetryPolicyConfiguredTests` | W006 §6.1 |
| `Slice2ReportLocationsTests` | `ReportLocationsTests` | W006 §6.2 |
| `Slice3KafkaPublishTests` | `DriverLocationPublishedTests` | W006 §6.3 |
| `Slice3PublishOrderingTests` | `DriverLocationPublishOrderingTests` | W006 §6.3 |
| `Slice4LastKnownPositionTests` | `LastKnownPositionTests` | W006 §6.4 |
| `Slice52FareQuotedHappyPathTests` | `FareQuotedHappyPathTests` | W001 §5.2 |
| `Slice52FareQuotedFailurePathTests` | `FareQuotedFailurePathTests` | W001 §5.2 |
| `Slice53CandidatesSelectedTests` | `CandidatesSelectedTests` | W001 §5.3 |
| `Slice5DriverLocationConsumerTests` | `NearbyAvailableDriversConsumerTests` | W006 §6.5 (Dispatch side) |
| `Slice5NearbyAvailableDriversViewTests` | `NearbyAvailableDriversViewTests` | W006 §6.5 (Dispatch side) |

Historical retros, prompts and handoffs keep the old names; this table bridges them.

### Session-start gates (5 and 9)

- **Gate 5, Aspire csproj shape.** On Aspire 13 the SDK goes in the `Project` element (`<Project Sdk="Aspire.AppHost.Sdk/13.4.6">`), `IsAspireHost` is gone, and `Aspire.Hosting.AppHost` is implied by the SDK (sources: `aspire.dev` via Context7, "Upgrade to Aspire 13.0 > AppHost template updates"; CritterMart's AppHost as a sibling reference). The prompt's NU1009 question was settled by experiment: putting the `Aspire.Hosting.AppHost` `PackageVersion` back reproduces `error NU1009 … implicitly defined and cannot define a PackageVersion item`, so the pin was dropped (the prompt's fallback) and CPM stays on. Added `UserSecretsId` (`crittercab-apphost`) so generated parameters persist across runs; the file-based form derived its id implicitly, so a developer's existing persistent Postgres container may need its volume reset once.
- **Gate 9, buf.** `bufbuild/buf-action` is on major `v1` (v1.6.0, 2026-09-22); its `version` input pins the CLI at buf 1.73.0 (2026-09-11). The step is lint-only, with `format`, `breaking`, `push` and `pr_comment` off. The Docker fallback was not needed except to run the lint locally once.

### `testing-advanced` disposition

Archived whole. Phase 1 compared every section against `tests/` and found none grounded as written. The nearest candidates were the gRPC streaming harness (it prescribes `WebApplicationFactory` and a hand-written stub, where the shipped tests feed Alba's `TestServer` into a `GrpcChannel` against generated code) and Kafka via Testcontainers (different image, no `PLAINTEXT://` stripping, topic-prefix isolation instead of a broker per fixture). The Kafka-fixture patterns the tests actually use went into `testing-integration`.

## Spec delta — landed?

Landed as planned. No slice moved between declared and realized, and no model content changed; the three workshop edits are closing Document History lines only. `docs/vision/README.md` is v1.0 (five contexts, five deployables, a build order; goals re-ranked; the admission rule; Go dropped; Onboarding parked with its trigger; Identity a host per ADR-006). Every forward-constraint and ADR candidate addressed to a context outside the build list is in `docs/planning/2026-10-08-released-forward-constraints.md`, with its origin cited. The ADR index preface declares 001 to 019 the v1 record, and no ADR's status changed.

## What disconfirmed

- **The tag's premise.** The prompt calls `4d8bde4` "the last v1 commit that touched code". It is a docs-only commit (the post-#47 handoff); the last commit that touched code is `327916d`. The tag still went at `4d8bde4`, the last v1 commit, with a message stating the fact. Nothing changes as a result, but the prompt's sentence is wrong.
- **Gate 13 undercounted.** The broken `.Codex\agent-memory` path was in all four `.codex/agents/*.toml` files, not two. All four were corrected; the target list's glob covered them.
- **My own pre-pull guard was blind to half the builder API.** As first written, it matched only `XBuilder("image")`. Phase 1 pointed out that `testing-integration` taught `.WithImage("image")`, so a fixture written from the skill would have passed the guard. The regex was extended and the mutation re-run. A guard tested only against the current code says nothing about code written from the skills.
- **My own README claim.** The first Dispatch README said `TryUpdateRevision` guards `AvailableDriver`. No code calls it; the writers call `UpdateRevision(doc, existing.Version + 1)`, and stale source comments still say otherwise (now a DEBT row). The routing-file rewrite was meant to come from the code; this sentence came from a comment. Phase 1 caught it.
- **A contradiction inside the prompt.** It says both that "W004's two ADR candidates release" and that "Onboarding's entries are marked parked-with-trigger, not released". The note resolves it: the candidates leave the live candidate list, and they return only if Onboarding's canvas re-model re-derives them.
- **A research note's step name.** `event-modeling-canonical-sources.md` gives Dymitruk's step 3 as "time travel". The primary source (eventmodeling.org, fetched this session) says "The Story Board", which is what the evaluation and the corrected skill use. The research note was out of scope and is left for a later session.
- **The archive's assumed blast radius.** Moving fifteen skill directories broke no relative link: surviving skills mention the archived ones only as backtick names (about 150 mentions in some 24 files). The archive README says how to read such a mention; cleaning them up is a DEBT row.
- **Stale local checkouts.** The local JasperFx checkout is at `V2.13.3`, against `2.69.3` resolved. `SlicePattern` was therefore verified from GitHub at `V2.69.3` (`src/JasperFx/Descriptors/EventModeling/SlicePattern.cs`), not from disk.
- **No forks escalated.** The prompt answered every owner question in advance, and nothing found this session needed the owner. Branch protection was blocked by the session's permission classifier, which is not a fork; it is a manual step for the owner.

## Next-session inputs

- **Branch protection (owner action).** The `gh api` call was denied in-session. Apply to `main`: require a pull request before merging (0 approvals); require the `Build & Test` status check; block force pushes and deletions; do not enforce for admins.
- **Dependabot will open grouped PRs.** `Aspire.*` in `Directory.Packages.props` can move without the SDK version in `CritterCab.AppHost.csproj`'s `Project` element; keep them on one line by hand. A `NU1903` advisory on `SSH.NET` 2025.1.0 (transitive via Testcontainers 4.13.0, which this session was barred from bumping) will likely be its first Testcontainers PR. Re-read the ryuk tag when Testcontainers moves.
- **New DEBT rows** (see `DEBT.md` § Open debt): stale revision comments in Dispatch source and tests; archived-skill names across surviving skills; contradictions in `adding-a-service`, `cli-aspire`, `cli-grpc-tooling` and `cli-kafka-tooling` that the AppHost conversion and Go drop left behind; and the rest of the skill-file items surfaced in this session. `GeoLocation` is re-pointed at the Driver Profile chapter.
- **Minutes drift, not fixed (minutes are append-only):** W006 §11 #1 and the workshops index's W006 follow-up row still call the Kafka topic-naming candidate pending, though it was authored as ADR-019.
- **Next session:** the Driver Profile chapter on the EventModelers.AI canvas, with the grill limited to the two boundary questions (the four transitions against the collapsed `DriverAvailabilityChanged` placeholder; the availability-before-location drop), a full seven-step pass with wireframes or "no screen" statements, and the field-level completeness check from the corrected `event-modeling` skill.
