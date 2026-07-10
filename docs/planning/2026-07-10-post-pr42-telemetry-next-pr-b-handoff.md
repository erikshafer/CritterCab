# Post-PR-42 Handoff — Telemetry Chain: Next is PR B (slices 4 + 2) — 2026-07-10

> **Purpose:** Session-handoff note after PR #42 (`CritterCab.Telemetry` skeleton + W006 slice 1
> config-as-events, plus an in-PR ADR-011 amendment) merged and `/post-merge` verified local
> `main`. Orients the next session on **PR B — the first transport slices**. Thin by intent;
> durable detail lives in the referenced artifacts. Disposable once the next session orients.
> **Supersedes** the three older untracked handoffs still in this directory (2026-07-04,
> 2026-07-10-post-pr41, 2026-07-10-narrative-decision) — their open items all shipped in #40/#42.

---

## Where we are (verified 2026-07-10)

- **`main` is clean at `4d288bd`** (`/post-merge` verified: HEAD subject `Telemetry: service
  skeleton + slice 1 … (#42)`, all 23 files match the session's deliverables incl. the ADR-011
  amendment; tree clean apart from untracked planning notes). All four stale local branches
  deleted (`telemetry/skeleton-and-slice-1-config` #42, plus #36/#39/#40).
- **PR #42 shipped** — CritterCab's **second service** (`CritterCab.Telemetry`) + **W006 slice 1**
  realized: `TelemetryPolicyConfigured` config-as-events singleton (`IInitialData` bootstrap seed,
  `TelemetryPolicy` self-aggregating view with `long` `throttlePolicyVersion`, boundary
  FluentValidation). **First config-as-events instance in code** and **first FluentValidation use**.
  **CI 15/15 green** (Telemetry 4 + Dispatch 11). **No transport yet** — config-as-events is the
  dependency-correct first slice.
- **ADR-011 amended in-PR** (`docs/decisions/011-…#amendment--2026-07-10-…`) — resolves the
  Phase-2 audit's Option-A/B-for-Marten finding: `IInitialData` is the canonical Marten realization
  of Option A (deploy-time apply + idempotent host-start safety net); config singletons use
  **last-writer-wins** (no optimistic concurrency; manual constant-id `Append`). **Do not relitigate.**
- **W006 design remains locked** — R1–R8, five slices, ADR-018.

---

## ⚠ Environmental: local Docker is wedged — lean on CI

**Local Docker's container-lifecycle path is stuck** — `docker run` and even `docker rm -f` time
out (exit 124), and a Docker Desktop reboot did **not** clear it (orphaned `Created`-state
containers: Testcontainers `ryuk` + a `postgres:18-alpine`). **Decision (user-confirmed): stop
chasing it; lean on CI as the test gate.** CI is proven reliable (15/15 on #42) and *caught a bug
local checks structurally could not* (the FluentValidation-registration miss — only the
Postgres-backed test exercised it). The loop is: **`dotnet build CritterCab.slnx` + the DB-less
smoke test locally (Docker-free, seconds) → push → CI runs the Testcontainers/Kafka-backed tests
(~1 min).** A real fix is a `wsl --shutdown` + Docker Desktop "Clean/Purge data" — the user's
environmental call, on their timeline; not a repo task. (User is rebooting the machine at handoff
time — re-probe `docker run --rm hello-world` at next session start; if it returns, local
Testcontainers are back and this whole section is moot.)

---

## The next session: PR B — slices 4 + 2 (first transport)

**Authoritative item-2 scope + constraints live in
[`2026-07-04-post-pr39-telemetry-next-steps-handoff.md`](./2026-07-04-post-pr39-telemetry-next-steps-handoff.md)
§ item 2 and W006 §6.2/§6.4.** In short: build **slice 4 (`LastKnownPosition` overwrite-in-place
document + heartbeat-absence eviction sweep)** together with **slice 2 (gRPC `ReportLocations`
client-streaming ingest)** — they pair because slice 2's publish-trigger evaluates against slice 4's
`LastKnownPosition` baseline (and reads slice 1's `TelemetryPolicy` view, now built). This is
CritterCab's **first gRPC surface in code** and adds the **Kafka Testcontainer** to the Telemetry
test harness. Likely one PR (slices 4+2 are coupled); if it gets large, slice 4 can precede slice 2.

### Load-bearing constraint (verified — re-confirm shape, not existence)

**Hand-wire `ReportLocations` against `IMessageBus`.** WolverineFx.Grpc has **no client-streaming
auto-codegen adapter** — a `[WolverineGrpcService]`-marked stub declaring `ReportLocations` fails
fast at startup (`NotSupportedException`; `GrpcServiceChain.AssertNoUnsupportedStreamingKinds`).
Verified must-hand-wire **through Wolverine V5.37.2**; the local `C:\Code\JasperFx\wolverine`
checkout is **stale at 5.37.2** (no 6.1x/6.17 tag), so 6.17 could not be confirmed — hand-wiring
works either way and is the safe default. **Shape:** a proto-first gRPC class deriving the
Grpc.Tools-generated `…Base`, **NOT** marked `[WolverineGrpcService]`, that drains
`IAsyncStreamReader<LocationPing>` and forwards to `IMessageBus.InvokeAsync<LocationIngestAck>`.
Do **not** "upgrade" R5's client-streaming to bidirectional to dodge the gap (bidi is a v2 deferral).
For a full-weight 6.17 verdict, refresh `C:\Code\JasperFx\*` to the consumed tags first (user-owned).

### Then

- **PR C** — slice 3 (`DriverLocationUpdated` → Kafka topic `telemetry.driver-location-updated`,
  partition `driverId`, publish-first/no-outbox). First Kafka topic in code; **wires Kafka into
  `apphost.cs`** (deliberately deferred until now); fires the Kafka-topic-naming ADR candidate.
- **PR D** — slice 5 (Dispatch consumer: replace `NearbyAvailableDriversStub` with the Kafka-fed
  `AvailableDriver` document view — **Kafka half of the join ONLY**; the ASB availability half is an
  ADR-018 forward-constraint to the un-workshopped Driver-Profile BC — do not model or build it).

---

## Guardrails carried forward (unchanged)

1. **Design is locked — do not re-grill.** R1–R8, five W006 slices, ADR-018, and the ADR-011
   amendment all stand.
2. **Build only the Kafka half of the join** (slice 5 / PR D).
3. **Verify before wiring.** Every gRPC/Kafka API claim through `jasperfx-source-verifier` —
   **and always record the checked-out JasperFx version in the verdict** (the local checkout trails
   the consumed line: Wolverine 5.37.2 vs 6.17; Marten similar). Run `critter-skill-auditor` Phase 1
   before cutting code, Phase 2 after.
4. **Session discipline.** One prompt = one session = one PR (the skeleton+first-slice exception is
   already spent); no opportunistic edits; retro ships in the session PR; name the spec delta;
   branch + PR, never commit to `main`; **no Claude attribution** (settings hardened —
   `includeCoAuthoredBy: false` + empty `attribution` in `.claude/settings.local.json`).
5. **W006 §11 ADR candidates that fire in PR B/C:** windowed gRPC client-streaming (PR B — may be a
   `docs/skills/` skill rather than an ADR; decide at authoring); stream-processing as the 4th
   modeling shape (best evidenced once the document/Kafka core lands, PR B/C); Kafka topic-naming
   (PR C). None are owed in a specific PR unless the build warrants — decide at authoring.

---

## Open DEBT (unblocked; for a future `tidy: skills` session, not PR B)

- **config-as-events bootstrap-seed skill** — now unblocked by the ADR-011 amendment; groundable
  from the Telemetry reference impl (`TelemetryPolicyBootstrap` etc.).
- **Wolverine.HTTP FluentValidation two-call wiring** (`wolverine-http-handlers` addendum) — the
  `UseFluentValidation()` (register) + `UseFluentValidationProblemDetailMiddleware()` (resolve)
  pairing; wiring only the middleware silently passes invalid input as 200 (the bug CI caught).
- Later-arc gaps for PR B/C/D: recurring/scheduled sweep-handler shape (slice 4 eviction),
  non-event-sourced document write path (slice 4), Kafka publish-first/no-outbox convention
  (slice 3), and the stale `wolverine-grpc-bidirectional-handlers` examples (slice 2).

---

## Orientation files (read in order)

1. [`2026-07-04-post-pr39-telemetry-next-steps-handoff.md`](./2026-07-04-post-pr39-telemetry-next-steps-handoff.md)
   — authoritative item-2 scope + constraints (its item 1 discharged by #40, slice 1 by #42).
2. [W006 §6.2 (ingest), §6.4 (store + eviction), §6.3 (Kafka), §6.5 (Dispatch consumer), §11](../workshops/006-telemetry-event-model.md).
3. [ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md),
   [ADR-005](../decisions/005-transport-selection-by-flow-type.md),
   [ADR-011 + its 2026-07-10 Amendment](../decisions/011-configuration-as-events-bootstrap.md).
4. Shipped protos `protos/crittercab/telemetry/v1/{report_locations,driver_location_updated}.proto`;
   the slice-1 reference impl under `src/CritterCab.Telemetry/TelemetryPolicy/`; retro
   [`retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md`](../retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md).

---

## Housekeeping (non-blocking)

- **Three older untracked handoffs** in this directory (2026-07-04, 2026-07-10-post-pr41,
  2026-07-10-narrative-decision) are superseded by this note; safe to delete when convenient.
- **CLAUDE.md status line** ("Dispatch has its first vertical slice … all other BCs remain
  pre-workshop") is now stale — Telemetry is a second service with code. `tidy: housekeeping` candidate.
- **CI Node.js 20 deprecation** on pinned Actions (`actions/*@v4`) — a future `tidy: ci` could bump.
- Pre-existing **NU1903** (`Microsoft.OpenApi` 2.0.0 vuln) still open — unrelated.

---

## Document history

- **2026-07-10.** Authored after PR #42 merged and `/post-merge` verified `main` at `4d288bd`.
  Records the CI-lean decision (local Docker wedged, reboot did not clear), PR B scope (slices 4+2),
  the still-must-hand-wire gRPC constraint (verified through 5.37.2; 6.17 unconfirmed), and the
  ADR-011 amendment as locked. Supersedes the three older handoffs in this directory.
