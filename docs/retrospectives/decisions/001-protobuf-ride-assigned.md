# Retrospective — Author Dispatch Business-Event Protobuf Contracts

## Metadata

- **Triggering prompt:** [`docs/prompts/decisions/001-protobuf-ride-assigned.md`](../../prompts/decisions/001-protobuf-ride-assigned.md)
- **Status:** Complete
- **Date authored:** 2026-05-07
- **Output artifacts:**
  - `/protos/crittercab/common/v1/location.proto` — shared `Location` message
  - `/protos/crittercab/dispatch/v1/ride_assigned.proto` — `RideAssigned` message
  - `/protos/crittercab/dispatch/v1/ride_request_cancelled.proto` — `RideRequestCancelled` message
  - `/protos/crittercab/dispatch/v1/ride_request_abandoned.proto` — `RideRequestAbandoned` message
  - `/protos/buf.yaml` — buf v2 workspace configuration
  - `/protos/buf.gen.yaml` — code generation configuration (C# and Go stubs)
  - `.gitignore` update — generated-code exclusions
- **Outcome:** First proto-authoring session complete. Established `/protos/` directory layout and buf configuration. Four proto files authored per ADR-009.

---

## Framing

Workshop 001 §12.8 named this as an explicit follow-up: author `dispatch/v1/ride_assigned.proto` and the two sibling business-event protos as the first concrete cross-BC contracts under ADR-009. This session created the `/protos/` directory, established the buf workspace, and delivered the three Dispatch business-event message definitions that downstream consumers (Trips, Payments/Pricing, Operations, Rider Profile) will subscribe to.

---

## Outcome summary

Four proto files delivered: one shared `Location` type in `common/v1/` and three Dispatch business-event messages (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`). Buf workspace configured with `STANDARD` lint rules and `PACKAGE_DIRECTORY_MATCH`.

---

## What worked

- **Workshop as domain source of truth.** Where the workshop and skill disagreed (e.g., `Location` vs `GeoLocation`, `lat`/`lon` vs `latitude`/`longitude`), the workshop was followed. The workshop carries the domain reasoning; the skill carries the mechanical convention.
- **Deferring `common/v1/money.proto`.** The workshop's inline approach (int64 minor units + separate currency string) is simpler and sufficient for these three messages. The shared `Money` type defers until a second BC needs it.

---

## What was harder than expected

- **`PACKAGE_DIRECTORY_MATCH` vs skill example.** The skill's directory layout example showed `/protos/dispatch/v1/` with package `crittercab.dispatch.v1`, but buf rejects that mismatch. The actual structure must be `/protos/crittercab/dispatch/v1/`. The package naming convention was authoritative; the layout example was simplified.

---

## Skill-file gaps surfaced

- **`protobuf-contracts` skill's directory layout example** shows `/protos/dispatch/v1/` but the `PACKAGE_DIRECTORY_MATCH` rule (which the skill itself prescribes) requires `/protos/crittercab/dispatch/v1/`. The skill should be updated to show the correct nesting.

---

## Decisions made during authoring

1. **Directory structure follows package names.** Buf's `PACKAGE_DIRECTORY_MATCH` lint rule requires the directory path to mirror the package name exactly.
2. **`Location` over `GeoLocation`.** Followed the workshop. The `street_address` field is load-bearing for pickup/dropoff display; pure coordinate types are insufficient.
3. **Money as inline minor units, not `Money` message.** Deferred `common/v1/money.proto` until a second BC needs shared money semantics.
4. **Enum prefix convention applied.** Workshop defined enum values without the buf-required type-name prefix (e.g., `CHANGED_MIND`). Applied buf convention: `CANCELLATION_REASON_CHANGED_MIND`, etc. Mechanical — domain semantics preserved.

---

## Outstanding items / next-session inputs

- Step B: Bootstrap the Dispatch service skeleton.
