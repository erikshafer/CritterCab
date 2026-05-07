# Prompt 001 — Author Dispatch Business-Event Protobuf Contracts

| Field | Value |
|---|---|
| **Status** | Complete (2026-05-07). Produced proto files in `/protos/crittercab/dispatch/v1/` and `/protos/crittercab/common/v1/`; established buf workspace at `/protos/`. |
| **Authored** | 2026-05-07 |
| **Target artifacts** | `/protos/crittercab/dispatch/v1/ride_assigned.proto`, `/protos/crittercab/dispatch/v1/ride_request_cancelled.proto`, `/protos/crittercab/dispatch/v1/ride_request_abandoned.proto`, `/protos/crittercab/common/v1/location.proto` |
| **Companion artifacts** | `/protos/buf.yaml`, `/protos/buf.gen.yaml` |
| **Source-of-truth dependencies** | [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) §9 (proto surface) and §12.8 (follow-ups); [`docs/decisions/009-protobuf-contracts-as-first-class-artifacts.md`](../../decisions/009-protobuf-contracts-as-first-class-artifacts.md); [`docs/skills/protobuf-contracts/SKILL.md`](../../skills/protobuf-contracts/SKILL.md) |
| **Workflow position** | First proto-authoring session; exercises ADR-009 for the first time; establishes `/protos/` directory layout and buf configuration |

---

## Framing — why this session exists

Workshop 001 §12.8 named this as an explicit follow-up: author `dispatch/v1/ride_assigned.proto` and the two sibling business-event protos as the first concrete cross-BC contracts under ADR-009. The `/protos/` directory does not exist yet. This session creates it, establishes the buf workspace, and delivers the three Dispatch business-event message definitions that downstream consumers (Trips, Payments/Pricing, Operations, Rider Profile) will subscribe to.

This is the smallest code-adjacent move available — no service code, no handlers, just the wire contracts that ADR-009 has been promising since 2026-04-23.

---

## Goal

Establish the `/protos/` directory at the repository root with buf workspace configuration, author the three Dispatch business-event protobuf message definitions (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`), and extract the shared `Location` type into `common/v1/`.

---

## Decisions locked by this session

| Decision | Resolution | Source |
|---|---|---|
| Proto file location | `/protos/` at repo root | ADR-009, `protobuf-contracts` skill |
| Package naming | `crittercab.<domain>.v<major>` | `protobuf-contracts` skill |
| C# namespace override | `option csharp_namespace = "CritterCab.<Domain>.V<Major>";` | `protobuf-contracts` skill |
| `Location` ownership | `common/v1/` — shared type, not dispatch-owned | `protobuf-contracts` skill § Shared Types |
| `FareBreakdown` ownership | `dispatch/v1/` — dispatch-specific, not shared | Workshop 001 §9 |
| `VehicleClass` ownership | `dispatch/v1/` — dispatch-specific for now | Workshop 001 §9; may migrate to `common/` if a second BC needs it |
| Money representation | `int64` minor units + separate `string currency` field | Workshop 001 §9 (explicit design) |
| Field naming | `snake_case` in proto | `protobuf-contracts` skill |
| Enum zero value | `<ENUM_NAME>_UNSPECIFIED = 0` always | `protobuf-contracts` skill |
| Generated code | Not checked in; `.gitignore` updated | ADR-009 |

---

## Design decisions to flag during authoring

1. **`Location` field names: `lat`/`lon` vs `latitude`/`longitude`.** Workshop uses `lat`/`lon`; `protobuf-contracts` skill example uses `latitude`/`longitude`. Lean: follow the workshop — it is the domain source of truth and `lat`/`lon` is idiomatic in geospatial APIs.
2. **Whether to author `common/v1/money.proto` now.** The skill prescribes a `Money` message. The workshop's design uses `int64` minor units with a separate currency string — simpler and already integer-safe. Lean: defer `Money` until a second BC needs a shared money type; the workshop's inline approach is sufficient for these three messages.
3. **`CancellationReason` and `AbandonmentReason` enum prefix style.** Buf lint requires enum values prefixed with the enum type name. Workshop defines them without prefix. This session applies the buf convention.

---

## Orientation files (read in order)

1. **[`docs/decisions/009-protobuf-contracts-as-first-class-artifacts.md`](../../decisions/009-protobuf-contracts-as-first-class-artifacts.md)** — the governing ADR.
2. **[`docs/skills/protobuf-contracts/SKILL.md`](../../skills/protobuf-contracts/SKILL.md)** — operational conventions for file layout, naming, versioning, buf enforcement.
3. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md)** — §9 for `RideAssigned` full proto shape; §5.8 for `RideRequestCancelled` event fields; §5.9 for `RideRequestAbandoned` event fields; §12.8 for the follow-up directive.

---

## Deliverable plan

1. `/protos/buf.yaml` — buf v2 workspace configuration with `STANDARD` lint rules and `PACKAGE_DIRECTORY_MATCH`.
2. `/protos/buf.gen.yaml` — code generation configuration (C# and Go stubs).
3. `/protos/crittercab/common/v1/location.proto` — shared `Location` message.
4. `/protos/crittercab/dispatch/v1/ride_assigned.proto` — `RideAssigned` message per workshop §9.
5. `/protos/crittercab/dispatch/v1/ride_request_cancelled.proto` — `RideRequestCancelled` message per workshop §5.8.
6. `/protos/crittercab/dispatch/v1/ride_request_abandoned.proto` — `RideRequestAbandoned` message per workshop §5.9.
7. `.gitignore` update — add generated-code exclusions per `protobuf-contracts` skill.
8. Update `docs/prompts/README.md` — add this prompt's entry to the Index.

---

## Out of scope

- **gRPC service definitions** (`service DispatchService { ... }`) — these protos are business-event messages published to ASB, not gRPC service contracts. Service definitions come with the service skeleton (Step B).
- **`common/v1/money.proto`** — deferred until a second BC needs shared money semantics.
- **CI workflow for `buf breaking`** — the `cli-grpc-tooling` skill (Phase 3) owns CI; this session establishes the local config only.
- **C# or Go code generation** — generated stubs are build artifacts produced when a service `.csproj` exists. No service project exists yet.
- **Handler implementation** — no code; contracts only.

---

## Retrospective

### What happened

All deliverables produced. The session established the `/protos/` directory with buf v2 workspace configuration and authored four proto files: one shared `Location` type in `common/v1/` and three Dispatch business-event messages (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`).

### Decisions made during authoring

1. **Directory structure follows package names.** Buf's `PACKAGE_DIRECTORY_MATCH` lint rule requires the directory path to mirror the package name exactly. The skill's example layout showed `/protos/dispatch/v1/` with package `crittercab.dispatch.v1`, but buf rejects this mismatch. Actual structure is `/protos/crittercab/dispatch/v1/`. This is the correct behavior — the skill's layout example was simplified; the package naming convention was authoritative.

2. **`Location` over `GeoLocation`.** Workshop used `Location` with `lat`/`lon`/`optional street_address`. Skill example used `GeoLocation` with `latitude`/`longitude`. Followed the workshop (domain source of truth). The `street_address` field is load-bearing for pickup/dropoff display; pure coordinate types are insufficient.

3. **Money as inline minor units, not `Money` message.** Workshop's explicit design uses `int64` minor units with a separate `currency` string. Deferred `common/v1/money.proto` until a second BC needs shared money semantics. The workshop's approach is simpler, already integer-safe, and fits the business-event shape where currency is a message-level concern.

4. **Enum prefix convention applied.** Workshop defined enum values without the buf-required type-name prefix (e.g., `CHANGED_MIND`). Applied buf convention: `CANCELLATION_REASON_CHANGED_MIND`, `ABANDONMENT_REASON_MAX_ROUNDS_EXHAUSTED`, etc. This is mechanical — the domain semantics are preserved.

### Skill-file gaps surfaced

- **`protobuf-contracts` skill's directory layout example** shows `/protos/dispatch/v1/` but the `PACKAGE_DIRECTORY_MATCH` rule (which the skill itself prescribes) requires `/protos/crittercab/dispatch/v1/`. The skill should be updated to show the correct nesting. (Not blocking — the convention is clear from the lint rule; the example is just misleading.)

### What's next

Step B: Bootstrap the Dispatch service skeleton (`docs/prompts/implementations/001-dispatch-service-skeleton.md`).
