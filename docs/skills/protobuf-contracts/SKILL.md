---
name: protobuf-contracts
description: "Conventions for hand-authored .proto files in CritterCab: file layout, naming, field numbering, versioning, breaking-vs-non-breaking classification, and the buf CI enforcement gate. Use when designing, modifying, or reviewing a cross-service contract."
cluster: grpc
tags: [protobuf, grpc, contracts, adr-009, buf, versioning, governance]
---

# Protobuf Contracts

Conventions for hand-authored `.proto` files in CritterCab. This skill operationalizes ADR-009: protobuf service and message definitions are first-class design artifacts, authored before the code that implements or consumes them.

The contract is the design. The C# stubs and Go stubs are build outputs. Reviews and PR governance focus on the `.proto` file; generated code is excluded from source control.

## When to apply this skill

Use this skill when:

- Authoring a new `.proto` file for a service or shared message library.
- Modifying an existing `.proto` file (any modification — additive or otherwise).
- Reviewing a PR that touches `.proto` files.
- Classifying a proto change as breaking or non-breaking for the PR description.
- Designing a service's gRPC surface during or after Event Modeling.
- Setting up `buf.yaml` or the CI breaking-change check.

Do NOT use this skill for:

- gRPC handler implementation in C# — see `wolverine-grpc-services` (Phase 3).
- gRPC streaming-mode patterns and backpressure — see `wolverine-grpc-services` and `wolverine-grpc-client-streaming` (Phase 3).
- buf CLI invocation details — see `cli-grpc-tooling` (Phase 3).
- Choosing between gRPC and other transports — see `transport-selection` (Phase 1).

---

## Contract-First Workflow

The order is fixed: contract, review, generate, implement.

1. **Author the `.proto` file by hand.** Do not derive it from C# types. Tools that derive proto from code (e.g., `protobuf-net.Grpc`'s code-first mode) are not used in CritterCab.
2. **Review the `.proto` change as an API contract**, not as implementation code. The review bar is "what does this commit consumers to over time?", not "does this compile?"
3. **Run `buf breaking` against `main`** before requesting review. If it flags changes, classify them in the PR description (breaking vs non-breaking, with migration plan if breaking).
4. **Generate stubs at build time.** Generated C# and Go code is not checked in.
5. **Implement handlers and consumers** against the generated stubs.

**The PR that adds a new `.proto` file may be merged before the PR that consumes it.** Splitting contract and implementation across PRs is encouraged when it makes the review bar visible. The contract review and the implementation review have different concerns.

---

## File Layout, Package, and Namespace

Per ADR-009 and `structural-constraints.md`: proto files reside in a dedicated `/protos` directory at the repository root. Their cross-service nature must be structurally visible — a `.proto` inside a service's project directory falsely implies single-service ownership.

### Directory structure

```
/protos/
├── buf.yaml                     # buf workspace configuration
├── buf.gen.yaml                 # buf code generation configuration
├── common/
│   └── v1/
│       └── geo.proto            # shared types (GeoLocation, Money, etc.)
├── dispatch/
│   └── v1/
│       └── dispatch.proto       # the Dispatch service contract
├── trips/
│   └── v1/
│       └── trips.proto          # the Trips service contract
└── telemetry/
    └── v1/
        └── telemetry.proto      # the Telemetry service contract
```

Convention: `/protos/<service-or-package>/v<major-version>/<package-name>.proto`. The version directory is part of the path so that v2 introduces `/protos/dispatch/v2/dispatch.proto` alongside the v1 file rather than overwriting it.

### Package naming

Pattern: `crittercab.<service-or-package>.v<major>`.

```protobuf
// In dispatch/v1/dispatch.proto:
package crittercab.dispatch.v1;

// In common/v1/geo.proto:
package crittercab.common.v1;
```

The version is part of the package name, not just the directory. Buf enforces this via its `PACKAGE_DIRECTORY_MATCH` lint rule.

### C# namespace

Override the default protoc-gen-csharp namespace to PascalCase with the explicit version:

```protobuf
option csharp_namespace = "CritterCab.Dispatch.V1";
```

This is pinned per-file via the `option` line. Mirroring the package version in the namespace surfaces version transitions at every reference site in C#.

---

## Naming Conventions

The proto-side conventions below differ from C# conventions; protoc-gen-csharp handles the case translation automatically.

### Services and methods

```protobuf
service DispatchService {
  // Unary: command-style, imperative verb
  rpc RequestRide(RequestRideRequest) returns (RequestRideResponse);

  // Server-streaming: typically "Stream<Plural>" or "Watch<Plural>"
  rpc StreamDriverOffers(StreamDriverOffersRequest) returns (stream DriverOffer);

  // Client-streaming: typically "Send<Plural>" or "Push<Plural>"
  rpc PushTelemetry(stream LocationPing) returns (PushTelemetryResponse);

  // Bidirectional: typically "Subscribe", "Connect", or domain-specific
  rpc SubscribeTripUpdates(stream TripUpdateRequest) returns (stream TripUpdate);
}
```

- **Service name:** `<Domain>Service`, PascalCase. `DispatchService`, `TripsService`, `TelemetryService`.
- **Method name:** PascalCase verb phrase. Match the command name from Event Modeling where applicable (`RequestRide`, `AcceptOffer`).
- **Request/response message names:** `<Method>Request` and `<Method>Response`. Use this pattern even for trivial methods — it makes adding fields later non-breaking. Don't pass scalars or unwrapped messages directly.

### Messages

PascalCase, descriptive. Match the domain term, not the technical wrapper.

```protobuf
message RequestRideRequest { ... }
message DriverOffer { ... }
message LocationPing { ... }
```

### Fields

`snake_case` in proto; protoc-gen-csharp produces PascalCase in C# automatically.

```protobuf
message RequestRideRequest {
  string ride_request_id = 1;          // → C# RideRequestId
  string rider_id = 2;                 // → C# RiderId
  crittercab.common.v1.GeoLocation pickup = 3;
  crittercab.common.v1.GeoLocation dropoff = 4;
  google.protobuf.Timestamp requested_at = 5;
}
```

### Enums

`SCREAMING_SNAKE_CASE` for values. Per the buf style guide and protobuf best practices, every value name is **prefixed with the enum type name** so that values do not collide across enums in the same package. The first value (number 0) is always `<ENUM_NAME>_UNSPECIFIED`.

```protobuf
enum OfferOutcome {
  OFFER_OUTCOME_UNSPECIFIED = 0;
  OFFER_OUTCOME_ACCEPTED    = 1;
  OFFER_OUTCOME_REJECTED    = 2;
  OFFER_OUTCOME_EXPIRED     = 3;
}
```

The `_UNSPECIFIED = 0` value is required by proto3 semantics — proto3 has no way to distinguish "field not set" from "field set to default" for non-`optional` scalar fields, so the zero value of every enum must be a meaningful "I haven't decided" value. Treat the unspecified value as a deserialization-time error in handlers, not as a default.

---

## Type Conventions

### Scalars and well-known types

| Domain concept | Proto type | Notes |
|---|---|---|
| Identifier (UUID v7) | `string` | Stringified UUID. Stored as the canonical UUID string format. |
| Timestamp | `google.protobuf.Timestamp` | Maps to `DateTimeOffset` via protoc-gen-csharp helpers. |
| Duration | `google.protobuf.Duration` | Maps to `TimeSpan` via helpers. |
| Money amount | Custom `Money` message (in `common/v1/money.proto`) | Never use `float`/`double` for money. See `csharp-coding-standards` § Decimal Calculations. |
| Geographic coordinate | Custom `GeoLocation` (in `common/v1/geo.proto`) | Two `double` fields; validation lives in the consumer's value object. |
| Free-text string | `string` | UTF-8. |
| Binary blob | `bytes` | Avoid for primary identifiers. |
| Boolean | `bool` | |
| Counted integer | `int32` | Reserve `int64` for values that may exceed 2^31. |

`google.protobuf.Timestamp` and `google.protobuf.Duration` are imported from `google/protobuf/timestamp.proto` and `google/protobuf/duration.proto` respectively. Both ship with the protoc compiler.

### Shared types

Types used by more than one service live in a shared package under `/protos/common/v<version>/`. The canonical example is `GeoLocation`:

```protobuf
// /protos/common/v1/geo.proto
syntax = "proto3";

package crittercab.common.v1;

option csharp_namespace = "CritterCab.Common.V1";

message GeoLocation {
  double latitude  = 1;
  double longitude = 2;
}
```

Consumers import:

```protobuf
import "common/v1/geo.proto";

message RequestRideRequest {
  crittercab.common.v1.GeoLocation pickup  = 3;
  crittercab.common.v1.GeoLocation dropoff = 4;
}
```

Shared messages are governed exactly as service-specific ones — every change classified as breaking or non-breaking. A breaking change to a shared message has more consumers, not fewer; the bar is higher, not lower.

### Money as a canonical shared type

Monetary values appear across Pricing, Payments, and Trips. Modeling them as primitives invites the `float`/`double` mistake at every reference site, and propagates rounding errors across calculation chains. Use a custom `Money` message in the shared package:

```protobuf
// /protos/common/v1/money.proto
syntax = "proto3";

package crittercab.common.v1;

option csharp_namespace = "CritterCab.Common.V1";

// A monetary amount with explicit currency. Never use `float` or `double`
// for money; rounding errors accumulate across calculation chains.
message Money {
  // Currency code, ISO 4217 (e.g., "USD", "EUR"). Required.
  string currency_code = 1;

  // Whole units of currency. Combined with `nanos` for the full value.
  // Example: $5.50 = { units: 5, nanos: 500_000_000 }
  int64 units = 2;

  // Fractional units in nanos (10^-9). Range -999_999_999 to +999_999_999.
  // Must have the same sign as `units`.
  int32 nanos = 3;
}
```

This shape mirrors `google.type.Money` from googleapis. Cab's version lives in the project's own `common/v1` package rather than importing googleapis, so the shared-type governance stays inside the project's `buf breaking` scope.

### Optional fields

proto3's `optional` keyword makes field presence detectable. Use `optional` when the consumer must distinguish "not set" from "set to default":

```protobuf
message RequestRideResponse {
  string ride_request_id     = 1;
  OfferOutcome outcome       = 2;
  optional string rejection_reason = 3;  // present only when outcome != ACCEPTED
}
```

Do not use `optional` reflexively for every field. For required-by-business fields (e.g., `ride_request_id` above), the absence of `optional` is the right shape.

---

## Field Numbering and Reserved Numbers

Field numbers are part of the wire format. Once assigned, they are never reused.

**Numbering strategy:**

- Field numbers 1–15 use 1 byte on the wire. Assign them to fields that appear in every message.
- Field numbers 16–2047 use 2 bytes. Assign them to less-frequent fields.
- Numbers 19000–19999 are reserved by the protobuf implementation. Do not use.

**When removing a field:** add a `reserved` clause to the message so the field number cannot be reused by a future change. Reserve both the number and the field name:

```protobuf
message DriverOffer {
  reserved 5, 9;
  reserved "deprecated_eta_seconds", "old_offer_token";

  string offer_id     = 1;
  string driver_id    = 2;
  string trip_id      = 3;
  GeoLocation pickup  = 4;
  // 5 was once `int32 deprecated_eta_seconds` — never reuse the number
  google.protobuf.Duration eta = 6;
  // ...
}
```

`buf breaking` checks the `reserved` clauses and will flag any attempt to reuse a removed field number or name.

---

## Streaming Method Conventions

The four streaming modes correspond to different proto declarations:

```protobuf
// Unary
rpc RequestRide(RequestRideRequest) returns (RequestRideResponse);

// Server-streaming (one request, stream of responses)
rpc StreamDriverOffers(StreamDriverOffersRequest) returns (stream DriverOffer);

// Client-streaming (stream of requests, one response)
rpc PushTelemetry(stream LocationPing) returns (PushTelemetryResponse);

// Bidirectional (stream of requests, stream of responses)
rpc SubscribeTripUpdates(stream TripUpdateRequest) returns (stream TripUpdate);
```

Naming guidance:

- **Unary:** verb phrase matching the command. `RequestRide`, `AcceptOffer`, `CompleteTrip`.
- **Server-streaming:** `Stream<Plural>` (continuous) or `Watch<Plural>` (events). `StreamDriverOffers`, `WatchTripStatus`.
- **Client-streaming:** `Push<Plural>` or `Send<Plural>`. `PushTelemetry`, `SendBreadcrumbs`.
- **Bidirectional:** `Subscribe<X>`, `Connect<X>`, or domain-specific. `SubscribeTripUpdates`.

Implementation patterns and backpressure considerations are covered by `wolverine-grpc-handlers` (Phase 3, unary + server-streaming) and `wolverine-grpc-bidirectional-handlers` (Phase 4, client-streaming + bidirectional). The proto file declares the shape; the C# handler implements it.

**Note on buf lint defaults.** Buf provides an opt-in lint category called `UNARY_RPC` containing `RPC_NO_CLIENT_STREAMING` and `RPC_NO_SERVER_STREAMING`, intended for projects whose RPC framework can't ferry streaming calls (e.g., Twirp). CritterCab uses streaming RPCs deliberately — the project exists in part to exercise Wolverine 5.32's streaming support — so the `UNARY_RPC` category is **not** added to `lint.use` in `buf.yaml`. (It isn't enabled by default; `STANDARD` doesn't include it.) The actual `buf.yaml` configuration lives in `cli-grpc-tooling` (Phase 3); the streaming methods themselves are a deliberate design choice, fully compatible with the buf rules Cab does enforce.

---

## Versioning and Evolution

### Default classification table

When a `.proto` change is reviewed, classify it against this table. When in doubt, classify as breaking — the cost of an over-classification is a small CI message; the cost of an under-classification is a production incident.

| Change | Default classification | Notes |
|---|---|---|
| Add a field with a new number | Non-breaking | Old code ignores the field; new code reads it. |
| Add an `optional` field | Non-breaking | Same as above with explicit presence detection. |
| Add a new RPC method to a service | Non-breaking | Old clients don't call it. |
| Add a new service to a package | Non-breaking | |
| Add an enum value (proto3) | Non-breaking, but flag | Consumers must handle unknown values gracefully. |
| Remove a field | **Breaking** | Reserve the number and name; do not reuse. |
| Rename a field | **Breaking** at code level | Wire is by number, but C#/Go generated names change. Consumers must regenerate. |
| Change a field type | **Breaking** | Wire format changes. Even compatible-on-paper changes (e.g., `int32` ↔ `int64`) can break consumers. |
| Reassign a field number | **Breaking** (catastrophic) | Old data is interpreted as the new field type. Never do this. |
| Remove an enum value | **Breaking** | Consumers may have switch statements that no longer cover the removed case. |
| Remove an RPC method | **Breaking** | Old clients calling it fail at runtime. |
| Change an RPC's streaming mode | **Breaking** | Unary ↔ stream is a wire-level change. |
| Add `optional` to an existing field | Non-breaking, but flag | Wire format unchanged; API gains presence detection. |
| Remove `optional` from a field | **Breaking** | Consumers may rely on presence detection. |

### Major version transitions

A breaking change that consumers cannot adopt incrementally is signaled with a new major version. For example, replacing the `DispatchService` with a redesigned interface produces `crittercab.dispatch.v2` in `/protos/dispatch/v2/dispatch.proto`. The v1 service continues to exist until all consumers migrate; the v2 service is the new canonical contract.

This is rare. Most changes can be expressed additively within v1.

---

## Governance: PR Description and CI Enforcement

ADR-009 makes governance explicit. Every PR that modifies a `.proto` file does two things:

### 1. Classifies the change in the PR description

The PR template (or the PR description, if no template) includes an explicit declaration:

```markdown
## Proto Change Classification

- [ ] No proto changes
- [x] Non-breaking proto changes — list:
  - Added `optional string rejection_reason` to `RequestRideResponse` (field number 3).
- [ ] Breaking proto changes — list and migration plan:
  - (none)
```

For breaking changes, the migration plan names the affected consumers and the order of deployment.

### 2. Passes the `buf breaking` CI check

A required CI status check runs `buf breaking --against '.git#branch=main'` on every PR. The check fails if the PR introduces a breaking change as defined by buf's default rules. This is the mechanical enforcement of ADR-009; the PR description is the human-readable justification.

When `buf breaking` flags a change that the author believes is intentional and acceptable, the PR description must call it out and the migration plan must be in place. The CI gate exists to prevent *accidental* breakage; deliberate breakage with a migration plan can override it (typically by adding a buf-ignore line with a comment, reviewed at PR time).

The `buf.yaml` configuration and the GitHub Actions workflow that runs the check are documented in `cli-grpc-tooling` (Phase 3).

---

## Generated Code Policy

Generated stubs are build artifacts. They are not checked in.

```
.gitignore additions:
**/obj/Grpc/**
**/obj/Generated/**
*.pb.go
*_grpc.pb.go
```

Each service's `.csproj` references the relevant `.proto` files via `<Protobuf Include="..." />` items, and protoc generates the stubs into the `obj/` directory at build time. The Go service uses `protoc-gen-go` and `protoc-gen-go-grpc` against the same proto files.

If a contributor modifies a generated `.cs` or `.go` file by hand, the change is lost on the next build. This is the intended behavior — the contract is the proto file, not the generated code.

---

## Forward-Looking Note: Protobuf as Unified Schema Language

ADR-009's Future Consideration section notes Erik's interest in extending protobuf beyond gRPC — to Kafka messages, ASB messages, Marten-persisted events, and Polecat-persisted events. The motivation: a single schema language across every boundary where data crosses a process or service edge.

This is **not a current commitment.** It is a future experiment that would, if pursued, produce its own ADR. This skill governs gRPC contract conventions; if the experiment proceeds, those new boundaries inherit the same conventions (file location, package naming, breaking-change classification, buf governance) by extension.

When designing a new proto today, optimize for gRPC use. If the unified-schema experiment lands later, the same proto files become the source of truth for additional transports without requiring rewrites.

---

## Common Pitfalls

- **Authoring C# types first and deriving the proto.** This is exactly the workflow ADR-009 rules out. Author the `.proto` first; generate the C# stubs; then implement.
- **Reusing a removed field number.** Catastrophic. Always `reserved`. `buf breaking` catches this; reading buf's output is part of the workflow.
- **Defining a shared type inside a service's package.** `GeoLocation` defined in `crittercab.dispatch.v1` and imported by `crittercab.trips.v1` puts Trips in a position where a Dispatch contract change affects it. Shared types live in `crittercab.common.v<n>`.
- **Skipping `_UNSPECIFIED = 0` on enums.** proto3 requires the zero value to be unspecified. Without it, a default-valued enum field is indistinguishable from one explicitly set to the first real value.
- **Using `float` or `double` for money.** Use the custom `Money` message in `common/v1/money.proto`. See `csharp-coding-standards` § Decimal Calculations.
- **Importing `google.protobuf.Empty` for empty requests or responses.** Even when an RPC has no fields today, define a custom `<Method>Request` and `<Method>Response`. Adding fields to a custom message later is non-breaking; replacing `Empty` with a custom message is breaking. The buf style guide flags this and `buf lint` will catch it unless explicitly relaxed.
- **Treating proto changes as implementation changes in PR descriptions.** A renamed field is not a "small refactor" at the wire level. The PR description classifies it explicitly.
- **Modifying generated code by hand.** Lost on next build. Modify the `.proto` and regenerate.
- **Versioning by adding `V2`-suffixed messages within `v1`.** If the change is breaking enough to warrant a new message, the package version bumps. Don't hide major-version transitions inside the v1 namespace.

---

## See also

**Upstream** — load these first if unfamiliar:

- `csharp-coding-standards` — naming, sealed records, decimal handling that informs proto type choices.
- `domain-event-conventions` — domain event naming. Cross-service integration events implemented via proto inherit the past-tense rule for event names.

**Downstream** — natural follow-ups when proto contracts are in hand:

- `cli-grpc-tooling` — `grpcurl`, `buf` (lint, breaking, format, generate), `Evans` (Phase 3).
- `wolverine-grpc-handlers` — handler patterns for unary and server-streaming RPCs (Phase 3).
- `wolverine-grpc-bidirectional-handlers` — handler patterns for client-streaming and bidirectional RPCs (Phase 4).
- `transport-selection` — when to choose gRPC vs Kafka or ASB for a given flow (Phase 1).
- `polyglot-go-service` — the Go service consumes the same protos (Phase 4).

**External:**

- ADR-009 in [`docs/decisions/`](../../decisions/) — protobuf contracts as first-class artifacts.
- [`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md) § Protobuf Contracts — the immutable rules.
- [Buf style guide](https://buf.build/docs/best-practices/style-guide/) — naming and structure conventions buf enforces.
- [Protobuf Language Guide (proto3)](https://protobuf.dev/programming-guides/proto3/) — the canonical reference for proto3 semantics.
- [Wolverine gRPC documentation](https://wolverinefx.net/guide/grpc/) — Wolverine 5.32+ gRPC integration.
