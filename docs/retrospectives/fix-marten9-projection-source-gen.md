# Retrospective — Fix: Marten 9 Conventional Projections Must Be `partial` for Source-Gen Dispatch

## Metadata

- **Triggering prompt:** [`docs/prompts/fix-marten9-projection-source-gen.md`](../prompts/fix-marten9-projection-source-gen.md)
- **Status:** Complete
- **Date authored:** 2026-06-13
- **Output artifacts:**
  - `src/CritterCab.Dispatch/RideRequesting/ActiveRideRequest.cs` — `ActiveRequestsByRiderProjection` → `partial`
  - `src/CritterCab.Dispatch/RideRequesting/RequestTimeline.cs` — `RequestTimelineProjection` → `partial`
  - `src/CritterCab.Dispatch/FareQuoting/FareQuoteAttempts.cs` — `FareQuoteAttemptsProjection` → `partial`
  - `docs/prompts/README.md`, `docs/retrospectives/README.md` — index entries
- **Outcome:** `dotnet test CritterCab.slnx` restored to 8/8 green. The Marten 9 source generator now emits dispatchers for the three conventional projection subclasses. Un-reds `main`; the Aspire PR (#31) rebases onto the fixed `main` to go green.

---

## Framing

The `2f83e66` bump to Marten 9 / JasperFx 2.0 removed runtime codegen for conventional projections — `Apply`/`Create` are now dispatched by the compile-time `JasperFx.Events.SourceGenerator`, which only augments `partial` classes. The three projection subclasses weren't `partial`, so every test died at host startup with `InvalidProjectionException: No source-generated dispatcher found …`. Pre-existing `main` breakage; surfaced by PR #31's CI.

---

## Outcome summary

| Projection | Base type | Change |
|---|---|---|
| `ActiveRequestsByRiderProjection` | `MultiStreamProjection<ActiveRideRequest, Guid>` | `partial` |
| `RequestTimelineProjection` | `SingleStreamProjection<RequestTimeline, Guid>` | `partial` |
| `FareQuoteAttemptsProjection` | `SingleStreamProjection<FareQuoteAttempts, Guid>` | `partial` |

`RideRequest` (self-aggregating `LiveStreamAggregation<RideRequest>` target) left untouched — self-aggregating types don't need `partial` per the exception message; confirmed still green.

Three keywords. Suite: red (7/8 failing) → green (8/8).

---

## What worked

- **The runtime exception was a precise spec.** JasperFx's `InvalidProjectionException` named the exact remedy (`partial` for convention-method subclasses; self-aggregating types exempt; analyzer-asset check). Reading it carefully made this a three-keyword fix rather than a Copilot-suggested refactor to explicit projection types (which would have been unnecessary churn).
- **`dotnet test` as the oracle, with Docker up.** The failure is a startup-time runtime exception, not a compile error, so only running the suite proves the fix. Local Docker let it be verified green before the PR rather than guessing and waiting on CI.
- **Branching off `main`, not the Aspire branch.** Kept the fix independently mergeable so `main` goes green on its own; the Aspire PR rebases cleanly afterward.

---

## What was harder than expected

- **Nothing, once diagnosed — but the diagnosis required confirming ownership.** The failure appeared on the Aspire PR's CI, which initially looked like the Aspire change broke something. Checking `gh run list --branch main` (run `27477344974` = failure on `2f83e66`) proved it pre-existing. The lesson: when CI fails on a PR whose diff doesn't touch the failing area, check `main`'s CI before assuming the PR caused it.

---

## Methodology refinements that emerged

- **Decisions-to-flag answered:**
  1. **The source generator's analyzer asset flows transitively** through `WolverineFx.Marten` → `Marten` — `partial` alone turned the suite green with no `.csproj` change. Nothing is registered in `Program.cs`; the generator is compile-time automagic, gated only on `partial`.
  2. **No further Marten 9 runtime breakage surfaced** once the projection-registration error cleared — all 8 tests pass, so the remaining v9/v6 migration audit has no *known* additional CI-blocking symptom (but is still owed as a deliberate pass).
- **The `partial` requirement is the headline Marten 8→9 migration trap for this codebase.** Every BC that lands a conventional projection subclass will hit it. Worth a line in the eventual migration-notes / `marten-projections` skill so the next service author declares `partial` from the start. Captured here as a next-session input rather than edited opportunistically.

---

## Outstanding items / next-session inputs

- **Rebase the Aspire PR (#31)** onto the fixed `main` once this merges; expect a trivial index-file merge (both PRs append a bullet to the prompts/retros indices).
- **The deferred Marten 9 / Wolverine 6 migration audit** still stands as its own session (IRevisioned/ILongVersioned, inline-lambda projection removal, `ServiceLocationPolicy` flip, etc.) plus the version doc-drift — none currently red CI, so lower urgency than they were an hour ago.
- **Document the `partial` requirement** in the `marten-projections` skill (or eventual migration notes) so new projection subclasses get it from the start.
- The other five Aspire-session follow-ups (Grpc/Polecat 5.38-vs-6.8, ServiceDefaults, Swagger title, MessagePack NU1903, JasperFx feed 403) are unchanged.

---

## Spec delta — landed?

**Null delta, as named.** A code fix to existing implementation; no narrative or workshop amended.

---

## Quantitative summary

- **Files changed:** 3 (one keyword each) + 2 index READMEs.
- **Test suite:** 7/8 failing → 8/8 passing (`dotnet test CritterCab.slnx`, Debug, Docker-backed Testcontainers Postgres).
- **`.csproj` changes:** none (analyzer flows transitively — verified).
