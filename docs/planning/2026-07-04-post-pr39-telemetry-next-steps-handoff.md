# Post-PR-39 Handoff — Telemetry Chain: Narrative Decision → First Transport Build — 2026-07-04

> **Purpose:** Session-handoff note after PR #39 (`Telemetry v1 protobuf contracts`) merged and
> `/post-merge` verified local `main`. Orients the next session on the remaining Telemetry chain.
> High-level by intent — durable detail lives in the referenced artifacts, not here. Disposable
> once the next session orients, per this directory's convention. Supersedes and replaces
> `2026-07-02-post-tidy38-telemetry-protos-handoff.md` (deleted — its item 2 shipped in PR #39).

---

## Where we are (verified 2026-07-04)

- **PR #39 merged** as `d78bd82` on `main`; `/post-merge` fast-forwarded and verified local `main`
  (HEAD subject + all 8 changed files confirmed against the PR; tree clean).
- **Stale local branch `protos/telemetry-v1`** still present (normal after squash-merge). Delete
  at will: `git branch -D protos/telemetry-v1`.
- **What PR #39 shipped:** the Telemetry `v1` proto surface (W006 §10) under
  `protos/crittercab/telemetry/v1/` — `report_locations.proto` (`TelemetryService.ReportLocations`
  client-streaming + `LocationPing`/`LocationIngestAck`) and `driver_location_updated.proto`
  (`DriverLocationUpdated` Kafka event). First gRPC client-streaming + first Kafka-event proto in
  CritterCab. Repo's first scoped `buf.yaml` `ignore_only` (kept the UL message names). Prompt +
  retro at `docs/{prompts,retrospectives}/decisions/003-protobuf-telemetry-v1.md`; W006
  `## Document History` records the §10 realization. All three buf gates (lint/format/breaking)
  verified green via the npm-distributed `@bufbuild/buf`.
- **The W006 Telemetry design remains locked** — R1–R8, the five slices, and ADR-018 carry forward
  without re-litigation.

---

## The next open items (in order)

### 1. Narrative-layer decision (small, do first)

Does a thin **driver-device journey narrative** apply to Telemetry, or is the narrative step
**explicitly skipped with recorded rationale**? Telemetry is machine-to-machine (W006 took the
EM-direct path, no Domain Storytelling, for the same reason). A journey narrative may not fit — but
per the workflow the decision and its rationale must be **recorded** (a short note / narrative-README
entry), not silently skipped. This is a design-layer call, likely its own tiny session or folded
into the first implementation prompt's framing.

### 2. First real transport build in code (the main event)

The first implementation that lights up **both** gRPC client-streaming (GPS ingest) and Kafka (the
throttled ping stream): the `CritterCab.Telemetry` service skeleton, `ReportLocations` ingest, the
first Kafka topic (`telemetry.driver-location-updated`), and the Dispatch consumer side — replacing
`NearbyAvailableDriversStub` with the Kafka ⋈ ASB Dispatch-local view (W006 slice 5 / ADR-018). This
is CritterCab's first transport wiring in a single line of code and will likely span more than one
session/PR (skeleton + first slice may share a PR per the named exception; beyond that, one slice per
PR).

**⚠ Load-bearing forward-constraint for item 2** (verified via `jasperfx-source-verifier` in PR #39;
recorded in retro 003, W006 Document History, and the transports-state memory): **WolverineFx.Grpc
has no client-streaming auto-codegen adapter on the verified 5.x line** — a `[WolverineGrpcService]`-
marked stub declaring `ReportLocations` **fails fast at startup** (`NotSupportedException`; only
unary/server/bidi are wired). The `.proto` is fine (plain proto3). The build must **hand-wire
`ReportLocations` against `IMessageBus`** (documented workaround) OR **re-verify 6.8 first** — the
local Wolverine checkout was pre-6.0 and could not confirm whether 6.8 added the adapter. Do **not**
assume auto-gen works for client-streaming.

---

## Guidelines + constraints (carried forward unchanged)

1. **The design is locked — do not re-grill.** R1–R8, the five W006 slices, ADR-018 stand. R5 fixes
   v1 as client-streaming (bidirectional is a v2 deferral — do not "upgrade" it to dodge the
   Wolverine gap).
2. **Build only the Kafka half of the join.** The ASB / Driver-Profile-availability half is an
   ADR-018 forward-constraint to an un-workshopped BC — do not model or build it to make the join
   "complete."
3. **Session discipline holds.** One prompt = one session = one PR; no opportunistic edits; retro
   ships in the session PR; name the spec delta.
4. **Verify before wiring.** Every gRPC/Kafka API claim goes through `jasperfx-source-verifier`
   before code commits to it. Load `/critter-skill-auditor` (Phase 1 before, Phase 2 after) and
   `/jasperfx-source-verifier`.
5. **Three W006 §11 ADR candidates fire during the build:** (a) Kafka topic-naming convention —
   generalize ADR-014 transport-agnostically, triggers when the first Kafka topic lands in code;
   (b) stream-processing as the fourth modeling shape — firm, triggers at the first Telemetry impl
   slice; (c) windowed gRPC client-streaming ingest — may fit better as a `docs/skills/` skill than
   an ADR, decide at the first gRPC ingest slice.
6. **CritterWatch trial expires 2026-07-10.** A demo before then argues for moving promptly through
   this chain — not for wiring CritterWatch against an empty topology.

---

## Two skill gaps registered (not blocking; for a future `tidy: skills` session)

Surfaced in PR #39's retro, deliberately not fixed there (no opportunistic edits):

- `protobuf-contracts` skill's directory diagram shows a stale single monolithic `telemetry.proto`;
  the repo uses one file per contract.
- `protobuf-contracts` skill's streaming example (`rpc PushTelemetry(stream LocationPing) …`) would
  itself fail the repo's own `buf STANDARD` gate; it should show the `ignore_only` pattern or use
  standard-named request messages.

Also observed: `docs/prompts/README.md`'s Decisions index is missing its `002-bundled-pattern-adrs`
entry (pre-existing) — housekeeping.

---

## Orientation files (read in order)

1. [W006 Telemetry Event Model](../workshops/006-telemetry-event-model.md) — §6.2 (ingest), §6.3
   (Kafka publish), §6.4 (store + eviction), §6.5 (Dispatch consumer view), §11 (ADR candidates).
2. [ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md) and
   [ADR-005](../decisions/005-transport-selection-by-flow-type.md) (transport-by-flow-shape).
3. The shipped protos: `protos/crittercab/telemetry/v1/report_locations.proto` +
   `driver_location_updated.proto`, and the retro
   [`docs/retrospectives/decisions/003-protobuf-telemetry-v1.md`](../retrospectives/decisions/003-protobuf-telemetry-v1.md).
4. [State-of-the-repo transport + CritterWatch plan](2026-06-25-state-of-the-repo-transport-and-critterwatch.md)
   — the full re-prioritization analysis behind this chain.

---

## Document history

- **2026-07-04.** Authored after PR #39 merged and `/post-merge` verified local `main`. Supersedes
  and replaces `2026-07-02-post-tidy38-telemetry-protos-handoff.md` (deleted — its item 2 shipped in
  PR #39; items 3–4 carry forward here as the narrative decision + first transport build).
