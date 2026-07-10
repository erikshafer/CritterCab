# Retrospective — Critter Stack Package Refresh

- **Trigger:** Direct user request (a "detour" ahead of the next layer-decision follow-up) to update the Critter Stack packages and run a full build + test. No prompt document — a small, well-scoped maintenance task.
- **Status:** Complete (2026-07-10).
- **Output artifacts:** [`Directory.Packages.props`](../../Directory.Packages.props) (WolverineFx family 6.8.0 → 6.17.0). This retro.
- **Outcome:** Clean minor-line refresh — build green with zero code changes; **CI full suite 11/11 passing** on the new versions. Discharges the "full-suite 6.x refresh still owed" item tracked since PR #35.

## Framing

Since the Grpc/Polecat version island was closed to 6.8 (PR #35), a full-suite refresh of the Critter Stack packages to the current 6.x line had been owed but not taken. This detour discharges it ahead of the first real transport build (the next Telemetry session), so that build starts from current packages rather than a stale floor.

## Outcome summary

| Package | Before | After | How pinned |
|---|---|---|---|
| WolverineFx.* (9 entries) | 6.8.0 | **6.17.0** | explicit in CPM |
| Marten | — | **9.14.0** | transitive via WolverineFx.Marten |
| JasperFx | — | 2.24.1 | transitive |
| Weasel | — | 9.16.2 | transitive |
| Alba | 8.5.2 | 8.5.2 | explicit — already latest |

- **Restore:** green — resolved WolverineFx 6.17.0 + Marten 9.14.0 cleanly.
- **Build:** `dotnet build CritterCab.slnx` — 0 errors, **0 code changes**.
- **Tests:** CI (clean Linux Docker) — `Failed: 0, Passed: 11, Skipped: 0, Total: 11`. PR #41 run `29114354509`.

## What worked

- **CPM keeps the bump to one line-item family.** Only the WolverineFx.* packages are pinned; Marten/JasperFx/Weasel flow transitively, so bumping WolverineFx pulled their aligned versions automatically. No risk of pinning a Marten version out of step with the Wolverine release intends.
- **CI as the authoritative test gate when local Docker is flaky.** Rather than fight a wedged Docker Desktop, pushing the branch let the full Testcontainers-backed suite run in a clean environment — and the same 11 tests that hung locally passed in 13s there. This is the more authoritative verification regardless.
- **Diagnosing the failure locus before blaming the bump.** Every local failure was at `DispatchTestFixture.InitializeAsync()` `.StartAsync()` — before any Wolverine/Marten code runs — which correctly located the fault as Docker infra, not the version change. Confirmed three ways (warm images, Ryuk disabled, both).

## What was harder than expected

- **Local Docker Desktop was in a degraded state.** The `docker` CLI half-worked, but the .NET managed named-pipe client (`Docker.DotNet`, used by Testcontainers) hung on container `start`; even `docker rm -f` blocked. "Create works, start hangs" is the signature of a wedged engine — a restart is the fix, but that's the user's environment call. The lesson: when Testcontainers fails at container start (not app code), suspect the Docker engine and fall back to CI rather than chasing package or test-code causes.

## Methodology refinements that emerged

- **When local Testcontainers is blocked, push to CI for the test gate** instead of stalling. The clean CI Docker environment is both a workaround and the more authoritative signal. Applies to any package/infra change on this repo.

## Outstanding items / next-session inputs

- **Restart Docker Desktop before the next session that needs Testcontainers locally** (the Telemetry transport build) — the engine is currently wedged. Environmental, not code.
- **Housekeeping (not blocking):** CI annotates a **Node.js 20 deprecation** on the pinned GitHub Actions (`actions/cache@v4`, `checkout@v4`, `setup-dotnet@v4`, `upload-artifact@v4`) — a future `tidy: ci` could bump those. The pre-existing **NU1903** warning (`Microsoft.OpenApi` 2.0.0 high-severity vuln) also remains and is unrelated to this bump.
- **Forward note for the Telemetry build:** this bump moved WolverineFx to 6.17 — worth re-verifying against 6.17 whether the client-streaming auto-codegen gap recorded at 5.x/pre-6.0 (the `ReportLocations` / `IMessageBus` forward-constraint) still holds before hand-wiring.

## Spec delta — landed?

**Null (maintenance).** No narrative or workshop is amended — a dependency version bump touches no canonical spec. The verification (build + CI 11/11) is the deliverable; this retro is the record.
