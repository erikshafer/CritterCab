# Prompt — Fix: Marten 9 Conventional Projections Must Be `partial` for Source-Gen Dispatch

| Field | Value |
|---|---|
| **Status** | Pending (sketched 2026-06-13; awaiting review before execution) |
| **Authored** | 2026-06-13 |
| **Target artifacts** | `src/CritterCab.Dispatch/RideRequesting/ActiveRideRequest.cs`, `src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs`, `src/CritterCab.Dispatch/FareQuoting/FareQuoteAttempts.cs`, `docs/retrospectives/fix-marten9-projection-source-gen.md` (new) |
| **Source-of-truth dependencies** | The CI failure on PR #31 / `main` run `27477344974`; the runtime `InvalidProjectionException` message; the JasperFx `marten-migration-v8-to-v9` ai-skill (runtime-codegen removal); the working test suite (`dotnet test CritterCab.slnx`) as the green/red oracle |
| **Workflow position** | Out-of-band CI-unblocking fix off `main`. First `fix:` PR in the repo. Lands **before** the Aspire PR (#31), which rebases onto the fixed `main` to go green. |

---

## Framing — why this session exists

The dependency bump in commit `2f83e66` moved the project to the 2026 Critter Stack line (Marten 9 / JasperFx 2.0). Marten 9 **removed runtime codegen** for conventional projections: `Apply` / `Create` / `ShouldDelete` methods are now dispatched by the compile-time `JasperFx.Events.SourceGenerator`, with **no runtime fallback**. The generator can only emit a dispatcher by augmenting a `partial` class. CritterCab's three conventional projection subclasses were not declared `partial`, so at host startup the projection graph throws `InvalidProjectionException: No source-generated dispatcher found for …`. This reds the entire test suite (7 of 8 fail at `DispatchTestFixture.InitializeAsync`).

The breakage is pre-existing on `main` (its CI for `2f83e66` is `completed failure`); PR #31 (Aspire reconcile) was merely the first PR-triggered CI run to surface it. This fix is therefore scoped narrowly to un-red `main` — not the broader Marten 9 / Wolverine 6 migration audit, which remains its own follow-up.

---

## Goal

Declare the three conventional projection subclasses `partial` so the Marten 9 source generator emits their dispatchers, restoring a green `dotnet test CritterCab.slnx`. Author a retrospective. No opportunistic edits.

---

## Spec delta

**None.** A build/runtime fix to existing implementation code; no narrative or workshop is amended. Honest null delta.

---

## Orientation files (read in order)

1. **This prompt** and the CI failure log on PR #31.
2. **`src/CritterCab.Dispatch/RideRequesting/ActiveRideRequest.cs`** — `ActiveRequestsByRiderProjection : MultiStreamProjection<ActiveRideRequest, Guid>`.
3. **`src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs`** — `RequestTimelineProjection : SingleStreamProjection<RequestTimeline, Guid>`.
4. **`src/CritterCab.Dispatch/FareQuoting/FareQuoteAttempts.cs`** — `FareQuoteAttemptsProjection : SingleStreamProjection<FareQuoteAttempts, Guid>`.
5. JasperFx `marten-migration-v8-to-v9` ai-skill — the runtime-codegen-removal section.

---

## Working pattern

- **Branch off `main`, not off the Aspire branch.** This fix must land independently so `main` goes green; the Aspire PR rebases afterward.
- **Verify against the real test suite.** `dotnet test CritterCab.slnx` (Docker up for Testcontainers Postgres) is the oracle — red before, green after. A docs-only or build-only check is insufficient because the failure is a startup-time runtime exception, not a compile error.
- **Minimal change.** `partial` keyword only. `RideRequest` (self-aggregating `LiveStreamAggregation<RideRequest>` target) does **not** need `partial` per the exception message — leave it untouched.
- **No opportunistic edits.** Other Marten 9 / Wolverine 6 breaking-change surfaces and the version doc-drift are out of scope (their own session).
- **Commit/push only on the user's say-so.**

---

## Deliverable plan

1. `ActiveRideRequest.cs` — `public class ActiveRequestsByRiderProjection` → `public partial class …`.
2. `RequestTimeline.cs` — `public class RequestTimelineProjection` → `public partial class …`.
3. `FareQuoteAttempts.cs` — `public class FareQuoteAttemptsProjection` → `public partial class …`.
4. Verify `dotnet test CritterCab.slnx` is green (8/8).
5. `docs/retrospectives/fix-marten9-projection-source-gen.md` — new retro.

---

## Out of scope

- **The rest of the Marten 9 / Wolverine 6 migration audit** (IRevisioned vs ILongVersioned, inline-lambda projection removal, ServiceLocationPolicy default flip, etc.) — its own session. This fix only addresses the one symptom redding CI.
- **Wolverine/Marten/Polecat version doc-drift** in README/CLAUDE/vision — deferred, as in the Aspire session.
- **Adding a direct `Marten` package reference or analyzer config** unless verification proves the source generator's analyzer asset does not flow transitively through `WolverineFx.Marten`.

---

## Decisions to flag during the session

1. **Does the source generator's analyzer asset flow transitively** (`WolverineFx.Marten` → `Marten`), or does CritterCab.Dispatch need a direct `Marten` reference / analyzer-asset include? If `partial` alone turns the suite green, the analyzer flows and no `.csproj` change is needed.
2. **Whether any further Marten 9 runtime failures surface** once the projection-registration error clears. If the suite reveals additional v9 breakage, capture it as a follow-up — do not expand this fix.
