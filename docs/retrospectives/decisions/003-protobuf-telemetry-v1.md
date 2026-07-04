# Retrospective — Author Telemetry v1 Protobuf Contracts

## Metadata

- **Triggering prompt:** [`docs/prompts/decisions/003-protobuf-telemetry-v1.md`](../../prompts/decisions/003-protobuf-telemetry-v1.md)
- **Status:** Complete
- **Date authored:** 2026-07-04
- **Output artifacts:**
  - `/protos/crittercab/telemetry/v1/report_locations.proto` — `TelemetryService.ReportLocations` client-streaming ingest + `LocationPing` + `LocationIngestAck`
  - `/protos/crittercab/telemetry/v1/driver_location_updated.proto` — `DriverLocationUpdated` Kafka event (message-only)
  - `/protos/buf.yaml` — first scoped `ignore_only` lint exception in the repo
  - `docs/workshops/006-telemetry-event-model.md` — `## Document History` line (spec-delta closure)
  - `docs/prompts/README.md`, `docs/retrospectives/README.md` — index entries
- **Outcome:** Third proto-authoring session complete. W006 §10's owed Telemetry surface realized; first gRPC client-streaming and first Kafka-event proto in CritterCab; the ubiquitous-language message names preserved via a file-scoped buf lint exception.

---

## Framing

Item 2 of the post-tidy-38 handoff: transcribe W006 §10's fully-specified Telemetry proto surface into `/protos/crittercab/telemetry/v1/`, realizing the ADR-018 Kafka supplier half. Design-locked (R1–R8, ADR-018) — transcription and buf reconciliation, not new design.

---

## Outcome summary

Two proto files delivered — the client-streaming ingest surface and the Kafka event — plus a scoped `buf.yaml` exception. Four durable wire/config forks were put to the user and resolved: keep UL message names + scoped lint exception; flat `lat`/`lon` scalars; defer buf codegen; `int64` throttle-policy version. Both mandated agents (`critter-skill-auditor` Phase 1, `jasperfx-source-verifier`) ran before any proto was touched, per the handoff.

---

## What worked

- **Verify-before-wiring caught a real forward-constraint.** `jasperfx-source-verifier` surfaced that WolverineFx.Grpc has no client-streaming auto-codegen adapter on the verified 5.x line (startup `NotSupportedException`; only unary/server/bidi are wired). This does not touch the `.proto` (Wolverine imposes nothing on the contract), but it is exactly the kind of surprise that would blindside the implementation session. Recorded as a forward-constraint, not acted on — the locked design (R5: client-streaming v1) stands.
- **buf `STANDARD` rule set confirmed by docs, not memory.** `ctx7` + the verifier independently confirmed `STANDARD` includes `SERVICE_SUFFIX`, `RPC_REQUEST_STANDARD_NAME`, `RPC_RESPONSE_STANDARD_NAME` — so the UL-vs-lint conflict was real, and `ignore_only` (per-file) is the sanctioned resolution. `buf` is not installed locally, so getting this right by construction mattered; CI runs the gates.
- **The mechanical-vs-domain precedent from retro-001 held.** Where the collision was mechanical and semantics-preserving (`SERVICE_SUFFIX` → `TelemetryService`, field-name `lon`), the buf/repo convention won automatically. Where it would have destroyed UL (`LocationPing`/`LocationIngestAck`), it became a user decision — the right altitude for the split.
- **Shipped-repo precedent overrode the skill diagram cleanly.** One-file-per-contract (from `dispatch/v1` + `get_fare_quote.proto`) rather than the skill's single-`telemetry.proto` diagram; `driver_location_updated.proto` mirrors `ride_assigned.proto` exactly (message-only, Kafka/ASB header comment).

---

## What was harder than expected

- **The RPC-naming conflict is a genuine design-lock-vs-tooling collision, not an oversight.** W006 §10 locks names that the repo's own `buf.yaml` would reject. Neither "silently rename" nor "silently except" was acceptable; it needed to be surfaced as a user decision. The resolution (scoped exception) is defensible and self-documenting, but it means the repo now carries its first lint exception — a small governance precedent worth noting.
- **The `protobuf-contracts` skill is internally inconsistent with the repo's `STANDARD` gate.** Its canonical streaming example (`rpc PushTelemetry(stream LocationPing) …`) would itself fail `RPC_REQUEST_STANDARD_NAME`. Surfaced but not fixed (no opportunistic edits) — see below.

---

## Methodology refinements that emerged

- **When a design-lock names a wire shape that fails the repo's own lint gate, that is a user decision, surfaced explicitly** — do not silently conform (loses domain language) or silently except (hides a governance choice). The mechanical-vs-domain test from retro-001 is the discriminator: conform silently only when the change is mechanical *and* semantics-preserving.
- **A locally-uninstalled CLI gate (buf) shifts the burden to construction-time correctness + doc verification.** With no `buf lint` to run, confirming the `STANDARD` rule set against buf docs (`ctx7`) before authoring is not optional.

---

## Skill-file gaps surfaced (for a future `tidy: skills` session)

- **`protobuf-contracts` directory diagram is stale** — shows a single monolithic `telemetry.proto` per package; the shipped repo (and this session) use one file per contract. (Same class of gap retro-001 already noted about the `/protos/dispatch/v1/` nesting.)
- **`protobuf-contracts` streaming example is `STANDARD`-incompatible** — `rpc PushTelemetry(stream LocationPing) …` would fail `RPC_REQUEST_STANDARD_NAME` under the repo's own `buf.yaml`. The skill should either show the `ignore_only` pattern or use standard-named request messages in the example, and cross-reference the exception mechanism.

These are registered here, not fixed in this PR (no opportunistic edits). Candidates for `docs/skills/DEBT.md` if the next skill-tidy session doesn't absorb them directly.

---

## Outstanding items / next-session inputs

- **Narrative-layer decision** (next in the Telemetry chain): does a thin driver-device journey narrative apply to Telemetry, or is the narrative step explicitly skipped with recorded rationale?
- **Telemetry implementation prompt(s):** the `CritterCab.Telemetry` service skeleton, gRPC client-streaming ingest, the first Kafka topic, and the Dispatch consumer side. **Carry the WolverineFx.Grpc client-streaming forward-constraint into that session** — hand-wire `ReportLocations` against `IMessageBus`, or re-verify 6.8's adapter support first (the local checkout was pre-6.0 and could not confirm 6.8).
- **`TelemetryPolicyConfigured` config-as-events surface** (HTTP + Marten, not a proto) — its own implementation slice.

## Spec delta — landed?

Landed as planned. W006 §10's candidate protobuf surface is realized as `report_locations.proto` + `driver_location_updated.proto` under `protos/crittercab/telemetry/v1/`; W006's `## Document History` records the realization in this PR. The named forward-constraint (WolverineFx.Grpc client-streaming gap) and the first scoped `buf.yaml` `ignore_only` both landed as described. No divergence.
