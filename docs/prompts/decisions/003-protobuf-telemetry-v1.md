# Prompt 003 — Author Telemetry v1 Protobuf Contracts

| Field | Value |
|---|---|
| **Status** | Complete (2026-07-04). Produced proto files in `/protos/crittercab/telemetry/v1/`; added the repo's first scoped `buf.yaml` lint exception. |
| **Authored** | 2026-07-04 |
| **Target artifacts** | `/protos/crittercab/telemetry/v1/report_locations.proto`, `/protos/crittercab/telemetry/v1/driver_location_updated.proto` |
| **Companion artifacts** | `/protos/buf.yaml` (scoped `ignore_only` lint exception); `docs/retrospectives/decisions/003-protobuf-telemetry-v1.md` |
| **Source-of-truth dependencies** | [`docs/workshops/006-telemetry-event-model.md`](../../workshops/006-telemetry-event-model.md) §10 (proto surface), §6.2 (ingest), §6.3 (Kafka publish), §4 (UL); [`docs/decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md`](../../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md); [`docs/decisions/009-protobuf-contracts-as-first-class-artifacts.md`](../../decisions/009-protobuf-contracts-as-first-class-artifacts.md); [`docs/skills/protobuf-contracts/SKILL.md`](../../skills/protobuf-contracts/SKILL.md); [`docs/skills/cli-grpc-tooling/SKILL.md`](../../skills/cli-grpc-tooling/SKILL.md) |
| **Workflow position** | Third proto-authoring session (after PR #4 dispatch/common, and the pricing bundle in slice 5.2). First Telemetry contract; first gRPC **client-streaming** surface and first **Kafka event** message proto in CritterCab. Item 2 of the [post-tidy-38 handoff](../../planning/2026-07-02-post-tidy38-telemetry-protos-handoff.md). |

---

## Framing — why this session exists

Workshop 006 §10 named the Telemetry `v1` protobuf surface as an owed follow-up (PR #4 precedent): the `TelemetryService` client-streaming ingest RPC plus its message pair, and the `DriverLocationUpdated` Kafka event that supplies Dispatch's `NearbyAvailableDrivers` projection per ADR-018. The design is fully locked — R1–R8, the five W006 slices, and ADR-018 stand without re-litigation. This session is **transcription of a locked design into wire contracts**, not new design work. It is the smallest code-adjacent move that unblocks the later implementation chain (the `CritterCab.Telemetry` service, gRPC client-streaming ingest, the first Kafka topic, and the Dispatch consumer side).

---

## Goal

Author the two Telemetry `v1` protobuf files under `/protos/crittercab/telemetry/v1/` — the `TelemetryService.ReportLocations` client-streaming ingest (with `LocationPing` / `LocationIngestAck`) and the `DriverLocationUpdated` Kafka event message — and add a file-scoped `buf.yaml` lint exception that preserves the ubiquitous-language message names.

## Spec delta

- **W006 §10 realized:** the candidate protobuf surface (`TelemetryService.ReportLocations(stream LocationPing) → LocationIngestAck`; `DriverLocationUpdated`) lands as authored `.proto` files under `protos/crittercab/telemetry/v1/`. W006's `## Document History` records the realization.
- **New forward-constraint recorded on the Telemetry implementation path:** WolverineFx.Grpc has no client-streaming auto-codegen adapter on the verified 5.x line (fails fast at startup); the implementation session must hand-wire `ReportLocations` against `IMessageBus` or re-verify 6.8 support first. Captured here and in the retro, not folded into the locked design.
- **buf governance first:** the repo's first scoped `ignore_only` lint exception (the two `RPC_*_STANDARD_NAME` rules, telemetry ingest file only), preserving `LocationPing` / `LocationIngestAck` as first-class UL wire names.

---

## Decisions locked by this session

| Decision | Resolution | Source |
|---|---|---|
| Proto file location | `/protos/crittercab/telemetry/v1/` | ADR-009, `protobuf-contracts` skill |
| File organization | One file per contract (`report_locations.proto`, `driver_location_updated.proto`) — the shipped-repo precedent, not the skill's single-`telemetry.proto` diagram | `dispatch/v1/*.proto`, `pricing/v1/get_fare_quote.proto` precedent |
| Package / namespace | `crittercab.telemetry.v1` / `option csharp_namespace = "CritterCab.Telemetry.V1"` | `protobuf-contracts` skill |
| Service name | `TelemetryService` (not `Telemetry`) — `SERVICE_SUFFIX` conformance, mechanical, semantics preserved | buf `STANDARD`; retro-001 enum-prefix precedent |
| RPC request/response naming | **Keep UL names** `LocationPing` / `LocationIngestAck`; except the two `RPC_*_STANDARD_NAME` rules for `report_locations.proto` via scoped `buf.yaml` `ignore_only` | User sign-off 2026-07-04; W006 §4 UL; verified buf `ignore_only` mechanism |
| Coordinate representation | **Flat `lat` / `lon` scalars** on both messages (workshop's flat shape; `lon` spelling matches `common.v1.Location` for repo-wide consistency; no vestigial `street_address` on a ping) | User sign-off 2026-07-04 |
| `throttle_policy_version` type | **`int64`** — the numeric Marten singleton stream version (verified `long`), per W006 §6.1 | User sign-off 2026-07-04; jasperfx-source-verifier |
| `throttle_policy_version` on ack | Non-optional (§10 / §6.2 authority; always present after ADR-011 bootstrap seed — resolves the §4 `?` ambiguity toward the proto-surface sections) | W006 §10, §6.2 |
| `h3_cell` type | `string` (opaque published-language key; id-stringification convention); `h3_resolution` carried separately as `int32` | `protobuf-contracts` id convention; W006 §6.3 |
| `driver_id` on `LocationPing` | **Absent** — from the authenticated principal, never the payload (R5) | W006 §2.2, §4 |
| `driver_id` on `DriverLocationUpdated` | **Present** — the Kafka partition key (R7) | W006 §6.3 |
| Money/enum types | None needed — no monetary or categorical fields in these three messages | — |
| buf codegen | **Deferred** to the implementation session — no `CritterCab.Telemetry.csproj` exists yet; `buf.gen.yaml` already configured | User sign-off 2026-07-04; PR #4 precedent |
| `.gitignore` | No change — proto-gen exclusions already present (added by PR #4) | — |

---

## Design decisions flagged during authoring

1. **RPC standard-name vs. ubiquitous language (the load-bearing call).** buf `STANDARD` enforces `RPC_REQUEST_STANDARD_NAME` / `RPC_RESPONSE_STANDARD_NAME`, which reject `LocationPing` / `LocationIngestAck` as the RPC input/output types. These are first-class W006 §4 UL terms — a single GPS reading and the window ack. Resolved (user sign-off) by keeping the domain names and adding a **file-scoped** `ignore_only` exception in `buf.yaml`, leaving every other proto fully `STANDARD`. This matches the `protobuf-contracts` skill's own streaming example (`rpc PushTelemetry(stream LocationPing) …`) and buf's documented per-file exception mechanism. A latent inconsistency surfaced: the skill's example itself wouldn't pass the repo's own `STANDARD` gate — registered as a retro follow-up, not fixed here (no opportunistic edits).
2. **`lat`/`lon` vs `lat`/`lng`.** The shipped `common.v1.Location` uses `lon`; W006 prose uses `lng`. Resolved (user sign-off) toward `lon` for repo-wide coordinate consistency, treating field spelling as a mechanical convention (parallel to retro-001's enum-prefix / `PACKAGE_DIRECTORY_MATCH` calls) while keeping the workshop's *flat* message shape.
3. **Reuse `common.v1.Location` vs. flat scalars.** Reuse is more DRY and matches `ride_assigned.proto`'s pickup/dropoff, but drags a meaningless `street_address` onto a high-volume GPS ping. Resolved (user sign-off) toward flat scalars.
4. **WolverineFx.Grpc client-streaming gap.** Verified: no auto-codegen adapter for client-streaming on the 5.x line (startup `NotSupportedException`). Does **not** change the locked contract (R5 fixes v1 as client-streaming; bidirectional is a v2 deferral) — recorded as an implementation forward-constraint.

---

## Orientation files (read in order)

1. **[`docs/workshops/006-telemetry-event-model.md`](../../workshops/006-telemetry-event-model.md)** — §10 for the proto surface; §6.2 for the ingest RPC + `LocationPing`/`LocationIngestAck` fields; §6.3 for `DriverLocationUpdated`; §4 for the UL definitions.
2. **[`docs/decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md`](../../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md)** — the parent decision the Kafka supplier half realizes.
3. **[`docs/decisions/009-protobuf-contracts-as-first-class-artifacts.md`](../../decisions/009-protobuf-contracts-as-first-class-artifacts.md)** — the governing ADR.
4. **[`docs/skills/protobuf-contracts/SKILL.md`](../../skills/protobuf-contracts/SKILL.md)** and **[`docs/skills/cli-grpc-tooling/SKILL.md`](../../skills/cli-grpc-tooling/SKILL.md)** — file layout, naming, versioning, the buf lint/breaking gates.
5. **[`docs/prompts/decisions/001-protobuf-ride-assigned.md`](./001-protobuf-ride-assigned.md)** and its retro — the proto-authorship precedent (`/protos/` layout, buf config, workshop-as-source-of-truth discipline).

---

## Deliverable plan

1. `/protos/crittercab/telemetry/v1/report_locations.proto` — `TelemetryService.ReportLocations` (client-streaming) + `LocationPing` + `LocationIngestAck`.
2. `/protos/crittercab/telemetry/v1/driver_location_updated.proto` — `DriverLocationUpdated` message-only Kafka event (mirrors `ride_assigned.proto`'s header-comment shape).
3. `/protos/buf.yaml` — add `lint.ignore_only` scoping `RPC_REQUEST_STANDARD_NAME` + `RPC_RESPONSE_STANDARD_NAME` to `report_locations.proto`, with a rationale comment.
4. `docs/prompts/README.md` — add this prompt's entry to the Decisions index.
5. `docs/retrospectives/decisions/003-protobuf-telemetry-v1.md` + `docs/retrospectives/README.md` index entry.
6. `docs/workshops/006-telemetry-event-model.md` — `## Document History` line recording the §10 realization (spec-delta closure loop).

---

## Out of scope

- **gRPC handler / service implementation, the `CritterCab.Telemetry` project, Kafka wiring, the Dispatch consumer** — all later implementation sessions. No C# this session.
- **buf code generation** — deferred; no consumer project exists (see decisions table).
- **The ASB / Driver-Profile availability half** of the ADR-018 join — a forward-constraint to an un-workshopped BC; modeling or building it here would be cross-BC over-reach (handoff constraint #2).
- **`TelemetryPolicyConfigured` / `ConfigureTelemetryPolicy` contracts** — that stream is HTTP + Marten event-sourced (config-as-events, ADR-011), not a gRPC or Kafka wire surface; no proto owed for it.
- **Bidirectional streaming ingest** — v2 (R5); the locked v1 contract is client-streaming.
- **Fixing the `protobuf-contracts` skill's stale single-file directory diagram and its `STANDARD`-incompatible streaming example** — surfaced, registered for a future `tidy: skills` session, not edited here (no opportunistic edits).
