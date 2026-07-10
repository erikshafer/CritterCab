# Post-PR-41 Handoff — Telemetry Chain: First Transport Build — 2026-07-10

> **Purpose:** Session-handoff note after the narrative-layer decision (PR #40) and the
> Critter Stack package refresh (PR #41) both merged. Orients the next session on the **first
> real transport build**. Thin by intent — the authoritative item-2 detail lives in the
> 2026-07-04 handoff (referenced below), not re-stated here. Disposable once the next session
> orients.

---

## Where we are (verified 2026-07-10)

- **`main` is clean at `01dc8bb68`** (`/post-merge` verified: HEAD subject, sha, and files
  match PR #41). Working tree clean apart from untracked planning notes.
- **PR #40 merged** — the Telemetry **narrative-layer decision is DONE**: the narrative layer
  **does not apply** (no protagonist-perceivable moment). Recorded in
  [`docs/narratives/README.md`](../narratives/README.md) § "When the narrative layer does not
  apply" + retro [`docs/retrospectives/telemetry-narrative-layer-decision.md`](../retrospectives/telemetry-narrative-layer-decision.md).
  **Do not redo or relitigate this.**
- **PR #41 merged** — Critter Stack **package refresh**: WolverineFx family **6.8.0 → 6.17.0**
  (Marten 9.14.0 transitively). Build green, **CI 11/11 passing**. Retro:
  [`docs/retrospectives/critter-stack-package-refresh.md`](../retrospectives/critter-stack-package-refresh.md).
  **The whole repo is now on WolverineFx 6.17** — this changes one forward-constraint (below).
- **W006 Telemetry design remains locked** — R1–R8, five slices, ADR-018. Not up for re-grill.

---

## The next session: first real transport build (item 2)

**The authoritative scope, dependency map, and constraints for this build live in
[`2026-07-04-post-pr39-telemetry-next-steps-handoff.md`](./2026-07-04-post-pr39-telemetry-next-steps-handoff.md)
§ "The next open items" → item 2.** Read it — do not expect this note to duplicate it. In
short: stand up the `CritterCab.Telemetry` service skeleton, `ReportLocations` gRPC
client-streaming ingest, the first Kafka topic (`telemetry.driver-location-updated`), and the
Dispatch consumer side (replace `NearbyAvailableDriversStub` with the Kafka ⋈ ASB
Dispatch-local view — **Kafka half only**). Likely spans more than one PR (skeleton + first
slice may share a PR per the named exception; beyond that, one slice per PR).

### What changed since the 2026-07-04 handoff was written (deltas to apply)

1. **⚠ Re-verify the gRPC client-streaming gap against WolverineFx 6.17 FIRST.** The 2026-07-04
   handoff's load-bearing forward-constraint — "WolverineFx.Grpc has no client-streaming
   auto-codegen adapter; `ReportLocations` fails fast at startup; hand-wire against
   `IMessageBus`" — was verified on a **pre-6.0 / 5.x** local checkout. The repo is now on
   **6.17**. Before hand-wiring the workaround, re-verify (via `jasperfx-source-verifier`
   against the 6.17 source) whether the client-streaming adapter now exists. It may be fixed;
   do not assume either way. Still do **not** "upgrade" R5's client-streaming to bidirectional
   to dodge the gap (bidirectional is a v2 deferral).
2. **Local Docker Desktop is currently wedged** — Testcontainers container `start` hangs before
   any app code runs (even `docker rm -f` blocked). **Restart Docker Desktop** before running
   the suite locally, or lean on CI as the test gate (it ran the full suite green for PR #41).
   This is environmental, not code.

---

## Guardrails carried forward (unchanged)

1. **Design is locked — do not re-grill.** R1–R8, the five W006 slices, ADR-018 stand.
2. **Build only the Kafka half of the join.** The ASB / Driver-Profile-availability half is an
   ADR-018 forward-constraint to an un-workshopped BC — do not model or build it.
3. **Verify before wiring.** Every gRPC/Kafka API claim goes through `jasperfx-source-verifier`
   before code commits to it. Run `critter-skill-auditor` Phase 1 before, Phase 2 after.
4. **Session discipline.** One prompt = one session = one PR; no opportunistic edits; retro
   ships in the session PR; name the spec delta; branch + PR (never commit to `main`).
5. **Three W006 §11 ADR candidates fire during the build** (see 2026-07-04 handoff item-5 list):
   Kafka topic-naming generalization; stream-processing as 4th modeling shape; windowed gRPC
   client-streaming (may be a skill, not an ADR — decide at authoring).

---

## Suggested skills (invoke as relevant)

- **`critter-skill-auditor`** — Phase 1 (skill discovery) before cutting code; Phase 2 after.
- **`jasperfx-source-verifier`** — for every Wolverine/Marten/Kafka/gRPC API claim, especially
  the 6.17 client-streaming re-verification above. Local sources under `C:\Code\JasperFx\`.
- **`ai-skills-consultant`** — for the gRPC-integration + Kafka-subscription patterns that go
  beyond CritterCab's own skills (JasperFx ai-skills has deeper guidance).
- **CritterCab `docs/skills/`** — `wolverine-grpc-handlers` (client-streaming shape),
  `wolverine-marten-automation`, and the messaging/HTTP handler-shape siblings.
- **`/post-merge` → `/handoff` → `/blurb`** — the close-out ritual at session end.

---

## Orientation files (read in order)

1. [`2026-07-04-post-pr39-telemetry-next-steps-handoff.md`](./2026-07-04-post-pr39-telemetry-next-steps-handoff.md)
   — **the authoritative item-2 scope + constraints.** Its item 1 is now discharged (PR #40).
2. [W006 Telemetry Event Model](../workshops/006-telemetry-event-model.md) — §6.2 (ingest),
   §6.3 (Kafka publish), §6.4 (store + eviction), §6.5 (Dispatch consumer view), §11 (ADRs).
3. [ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md)
   + [ADR-005](../decisions/005-transport-selection-by-flow-type.md).
4. The shipped protos: `protos/crittercab/telemetry/v1/report_locations.proto` +
   `driver_location_updated.proto`, and retro
   [`docs/retrospectives/decisions/003-protobuf-telemetry-v1.md`](../retrospectives/decisions/003-protobuf-telemetry-v1.md).
5. [State-of-the-repo transport + CritterWatch plan](2026-06-25-state-of-the-repo-transport-and-critterwatch.md).

---

## Housekeeping (non-blocking)

- **Three stale local branches** remain (all merged): `ci/solution-completeness-guard` (#36),
  `narratives/telemetry-narrative-layer-decision` (#40), `protos/telemetry-v1` (#39). Safe to
  `git branch -D` at will.
- **CI Node.js 20 deprecation** on pinned Actions (`actions/cache@v4`, `checkout@v4`,
  `setup-dotnet@v4`, `upload-artifact@v4`) — a future `tidy: ci` could bump them.
- Pre-existing **NU1903** (`Microsoft.OpenApi` 2.0.0 vuln) still open — unrelated to recent work.

---

## Document history

- **2026-07-10.** Authored after PR #40 (narrative decision) and PR #41 (package refresh)
  merged and `/post-merge` verified `main` at `01dc8bb68`. Thin pointer to the 2026-07-04
  handoff's still-authoritative item 2, with two deltas (re-verify gRPC gap on 6.17; Docker
  restart) and the narrative decision marked done.
