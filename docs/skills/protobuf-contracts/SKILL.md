---
name: protobuf-contracts
description: "Conventions for hand-authored .proto files in CritterCab: file layout, naming, field numbering, versioning, breaking-vs-non-breaking classification, the published-event-contract vs. shared-value-type distinction, and the buf lint CI gate. Use when designing, modifying, or reviewing a cross-service contract."
cluster: grpc
tags: [protobuf, grpc, contracts, adr-009, buf, versioning, governance]
---

# Protobuf Contracts

Conventions for hand-authored `.proto` files in CritterCab. This skill operationalizes ADR-009: protobuf service and message definitions are first-class design artifacts, authored before the code that implements or consumes them.

The contract is the design. The C# stubs are build outputs. Reviews and PR governance focus on the `.proto` file; generated code is excluded from source control.

## When to apply this skill

Use this skill when:

- Authoring a new `.proto` file for a service or shared message library.
- Modifying an existing `.proto` file (any modification — additive or otherwise).
- Reviewing a PR that touches `.proto` files.
- Classifying a proto change as breaking or non-breaking for the PR description.
- Designing a service's gRPC surface during or after Event Modeling.
- Setting up `buf.yaml` or the CI lint step.

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
4. **Generate stubs at build time.** Generated C# code is not checked in.
5. **Implement handlers and consumers** against the generated stubs.

**The PR that adds a new `.proto` file may be merged before the PR that consumes it.** Splitting contract and implementation across PRs is encouraged when it makes the review bar visible. The contract review and the implementation review have different concerns.

---

## File Layout, Package, and Namespace

Per ADR-009 and `structural-constraints.md`: proto files reside in a dedicated `/protos` directory at the repository root. Their cross-service nature must be structurally visible — a `.proto` inside a service's project directory falsely implies single-service ownership.

### Directory structure

```
/protos/
├── buf.yaml                                # buf module config: STANDARD lint + one scoped ignore_only, FILE breaking
├── buf.gen.yaml                            # buf generate: the two C# plugins (protocolbuffers/csharp, grpc/csharp)
└── crittercab/
    ├── common/
    │   └── v1/
    │       └── location.proto              # shared value type: Location
    ├── dispatch/
    │   └── v1/
    │       ├── ride_assigned.proto         # Dispatch business-event contracts (ASB)
    │       ├── ride_request_abandoned.proto
    │       └── ride_request_cancelled.proto
    ├── pricing/
    │   └── v1/
    │       └── get_fare_quote.proto        # PricingService.GetFareQuote
    └── telemetry/
        └── v1/
            ├── report_locations.proto      # TelemetryService.ReportLocations (client-streaming)
            └── driver_location_updated.proto  # Telemetry's published Kafka event contract
```

One file per RPC or per message contract, named for it in `snake_case`.

Convention: `/protos/crittercab/<service-or-package>/v<major-version>/<file>.proto`. The directory path mirrors the protobuf package name (`crittercab.<service>.v<major>`) because buf's `PACKAGE_DIRECTORY_MATCH` lint rule requires them to match. The version directory is part of the path so that a new major version introduces `/protos/crittercab/dispatch/v2/...` alongside the v1 files rather than overwriting them.

### Package naming

Pattern: `crittercab.<service-or-package>.v<major>`.

```protobuf
// In crittercab/dispatch/v1/ride_assigned.proto:
package crittercab.dispatch.v1;

// In crittercab/common/v1/location.proto:
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

  // Client-streaming: verb + plural of what is streamed
  rpc ReportLocations(stream LocationPing) returns (LocationIngestAck);

  // Bidirectional: typically "Subscribe", "Connect", or domain-specific
  rpc SubscribeTripUpdates(stream TripUpdateRequest) returns (stream TripUpdate);
}
```

- **Service name:** `<Domain>Service`, PascalCase. `DispatchService`, `TripsService`, `TelemetryService`.
- **Method name:** PascalCase verb phrase. Match the command name from Event Modeling where applicable (`RequestRide`, `AcceptOffer`).
- **Request/response message names:** `<Method>Request` and `<Method>Response`. Use this pattern even for trivial methods — it makes adding fields later non-breaking. Don't pass scalars or unwrapped messages directly. The one shipped exception is `TelemetryService.ReportLocations(stream LocationPing) returns (LocationIngestAck)`, which keeps its ubiquitous-language message names from Workshop 006; `protos/buf.yaml` excepts `RPC_REQUEST_STANDARD_NAME` and `RPC_RESPONSE_STANDARD_NAME` for `crittercab/telemetry/v1/report_locations.proto` only, via `lint.ignore_only`. A new exception gets the same treatment: scoped to one file, with the rationale in a comment.

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
  crittercab.common.v1.Location pickup = 3;
  crittercab.common.v1.Location dropoff = 4;
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
| Money amount | Integer minor units + currency (`int64 fare_amount_minor_units`, `string currency` in `pricing/v1/get_fare_quote.proto`); a shared `Money` message is not yet authored (see below) | Never use `float`/`double` for money. See `csharp-coding-standards` § Decimal Calculations. |
| Geographic coordinate | `crittercab.common.v1.Location` (in `common/v1/location.proto`) | `double lat`, `double lon`, `optional string street_address`; validation lives in the consumer's value object. |
| Free-text string | `string` | UTF-8. |
| Binary blob | `bytes` | Avoid for primary identifiers. |
| Boolean | `bool` | |
| Counted integer | `int32` | Reserve `int64` for values that may exceed 2^31. |

`google.protobuf.Timestamp` and `google.protobuf.Duration` are imported from `google/protobuf/timestamp.proto` and `google/protobuf/duration.proto` respectively. Both ship with the protoc compiler.

### Shared value types vs. published event contracts

Two different relationships put one service's types in front of another. Keep them apart.

**A shared value type** is a building block more than one service's contracts embed — a location, a money amount. It lives in a neutral package under `/protos/crittercab/common/v<version>/`, owned by no bounded context. The shipped example is `Location`:

```protobuf
// /protos/crittercab/common/v1/location.proto
syntax = "proto3";

package crittercab.common.v1;

option csharp_namespace = "CritterCab.Common.V1";

// A geographic location with optional street address.
// Used across services wherever pickup/dropoff or position is represented.
message Location {
  double lat = 1;
  double lon = 2;
  optional string street_address = 3;
}
```

Consumers import it (`pricing/v1/get_fare_quote.proto` and `dispatch/v1/ride_assigned.proto` both do):

```protobuf
import "crittercab/common/v1/location.proto";

message GetFareQuoteRequest {
  crittercab.common.v1.Location pickup  = 2;
  crittercab.common.v1.Location dropoff = 3;
}
```

The anti-pattern is **borrowing a value type out of another bounded context's package** — a location type defined in `crittercab.dispatch.v1` and pulled into `crittercab.trips.v1`. That couples Trips to Dispatch through a type neither service owns as a contract: a Dispatch-internal change to it ripples into Trips. Value types used by more than one service go in `common`.

**A published event contract** is different: one bounded context owns it and publishes it, and others subscribe. Telemetry publishes `DriverLocationUpdated` (`protos/crittercab/telemetry/v1/driver_location_updated.proto`) as its outbound event on the Kafka topic `telemetry.driver-location-updated` (ADR-009, ADR-018), and Dispatch consumes it. The message stays in the publisher's package, `crittercab.telemetry.v1` — subscribers consuming the publisher's contract is what published language means, and moving it to `common` would erase who owns it.

How a published event contract is consumed in the repo:

- **Each service compiles the same `.proto` into its own assembly.** Both `src/CritterCab.Telemetry/CritterCab.Telemetry.csproj` and `src/CritterCab.Dispatch/CritterCab.Dispatch.csproj` (lines 44–49) include `..\..\protos\crittercab\telemetry\v1\driver_location_updated.proto` as a `<Protobuf>` item with `ProtoRoot="..\..\protos"`, a `Link`, and `GrpcServices="None"` (the file has messages only). There is no shared contracts assembly and no project reference across the boundary.
- **`option csharp_namespace` is the real coupling.** Wolverine's message identity is `Type.FullName`-based and assembly-agnostic, so both copies resolve to `CritterCab.Telemetry.V1.DriverLocationUpdated` and agree on the wire. The namespace option must stay identical for every compiling service, and the message must stay **top-level** — a nested type gets a `DeclaringType_` prefix in its Wolverine message-type name, so the two sides could stop matching.
- **The wire format is binary protobuf, scoped to the endpoint.** The publisher's topic endpoint calls `.UseProtobufSerialization()` (Telemetry `Program.cs`), and so does the subscriber's listener, which also declares `.DefaultIncomingMessage<DriverLocationUpdated>()` so the type resolves even without a `message-type` header (Dispatch `Program.cs`). Each service's HTTP surface stays JSON.

Both kinds are governed exactly as service-specific contracts — every change classified as breaking or non-breaking. A breaking change to either has more consumers, not fewer; the bar is higher, not lower.

### Money as a canonical shared type

Monetary values appear across Pricing, Payments, and Trips. Modeling them as floating-point primitives invites rounding errors across calculation chains. The shipped contracts carry money as integer minor units plus a currency string (`int64 fare_amount_minor_units` and `string currency` in both `pricing/v1/get_fare_quote.proto` and `dispatch/v1/ride_assigned.proto`). **No `common/v1/money.proto` exists.** If a shared `Money` message is authored, it takes this shape in the `common` package:

```protobuf
// /protos/crittercab/common/v1/money.proto
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

This shape mirrors `google.type.Money` from googleapis. Cab's version would live in the project's own `common/v1` package rather than importing googleapis, so the shared-type governance stays inside the project's `buf` scope.

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
  crittercab.common.v1.Location pickup = 4;
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
rpc ReportLocations(stream LocationPing) returns (LocationIngestAck);

// Bidirectional (stream of requests, stream of responses)
rpc SubscribeTripUpdates(stream TripUpdateRequest) returns (stream TripUpdate);
```

Naming guidance:

- **Unary:** verb phrase matching the command. `RequestRide`, `AcceptOffer`, `CompleteTrip`.
- **Server-streaming:** `Stream<Plural>` (continuous) or `Watch<Plural>` (events). `StreamDriverOffers`, `WatchTripStatus`.
- **Client-streaming:** verb + plural of what is streamed. `ReportLocations` (shipped: Telemetry's GPS ingest).
- **Bidirectional:** `Subscribe<X>`, `Connect<X>`, or domain-specific. `SubscribeTripUpdates`.

Implementation patterns are covered by `wolverine-grpc-handlers` (unary, server-streaming, and the client-streaming shape `ReportLocations` uses); bidirectional handlers were covered by `wolverine-grpc-bidirectional-handlers` (archived — no bidirectional RPC exists). The proto file declares the shape; the C# handler implements it.

**Note on buf lint defaults.** Buf provides an opt-in lint category called `UNARY_RPC` containing `RPC_NO_CLIENT_STREAMING` and `RPC_NO_SERVER_STREAMING`, intended for projects whose RPC framework can't ferry streaming calls (e.g., Twirp). CritterCab uses streaming RPCs deliberately — the project exists in part to exercise Wolverine 5.32's streaming support — so the `UNARY_RPC` category is **not** added to `lint.use` in `buf.yaml`. (It isn't enabled by default; `STANDARD` doesn't include it.) The actual configuration is `protos/buf.yaml` (`lint.use: [STANDARD]`); the streaming methods themselves are a deliberate design choice, fully compatible with the buf rules Cab does enforce.

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
| Rename a field | **Breaking** at code level | Wire is by number, but generated C# names change. Consumers must regenerate. |
| Change a field type | **Breaking** | Wire format changes. Even compatible-on-paper changes (e.g., `int32` ↔ `int64`) can break consumers. |
| Reassign a field number | **Breaking** (catastrophic) | Old data is interpreted as the new field type. Never do this. |
| Remove an enum value | **Breaking** | Consumers may have switch statements that no longer cover the removed case. |
| Remove an RPC method | **Breaking** | Old clients calling it fail at runtime. |
| Change an RPC's streaming mode | **Breaking** | Unary ↔ stream is a wire-level change. |
| Add `optional` to an existing field | Non-breaking, but flag | Wire format unchanged; API gains presence detection. |
| Remove `optional` from a field | **Breaking** | Consumers may rely on presence detection. |

### Major version transitions

A breaking change that consumers cannot adopt incrementally is signaled with a new major version. For example, replacing the `DispatchService` with a redesigned interface produces `crittercab.dispatch.v2` in `/protos/crittercab/dispatch/v2/dispatch.proto`. The v1 service continues to exist until all consumers migrate; the v2 service is the new canonical contract.

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

### 2. Passes `buf lint` in CI, and `buf breaking` before review

CI (`.github/workflows/dotnet.yml`, step "Lint protobuf contracts") runs `bufbuild/buf-action@v1` with buf `1.73.0` against the `protos` input, **lint only**: `lint: true`, and `format`, `breaking`, `push`, and `pr_comment` all `false`. It enforces `protos/buf.yaml`'s `STANDARD` rules with their one scoped `ignore_only` on every build, and fails the build on a lint violation.

`buf breaking` is **not** a CI gate. `protos/buf.yaml` configures it (`breaking.use: [FILE]`), and the author runs it locally against `main` before requesting review (Contract-First Workflow, step 3). When it flags a change the author believes is intentional, the PR description calls it out and the migration plan must be in place; the classification in step 1 is the governance record.

The `buf.yaml` configuration and CLI usage are documented in `cli-grpc-tooling`.

---

## Generated Code Policy

Generated stubs are build artifacts. They are not checked in.

```
.gitignore (relevant lines):
obj/
protos/gen/
```

Each service's `.csproj` references the `.proto` files it needs via `<Protobuf Include="..\..\protos\..." ProtoRoot="..\..\protos" Link="..." GrpcServices="..." />` items, and Grpc.Tools generates the stubs into `obj/` at build time. `GrpcServices` is `Both` for a file that declares a service the project serves (`report_locations.proto` in Telemetry) and `None` for a message-only file (`driver_location_updated.proto`, in both Telemetry and Dispatch). `protos/buf.gen.yaml` configures `buf generate` with the two C# plugins (`buf.build/protocolbuffers/csharp`, `buf.build/grpc/csharp`) writing to `protos/gen/csharp`; the service builds do not use it.

If a contributor modifies a generated `.cs` file by hand, the change is lost on the next build. This is the intended behavior — the contract is the proto file, not the generated code.

---

## Protobuf Beyond gRPC

ADR-009's Future Consideration section raises extending protobuf beyond gRPC — to Kafka messages, ASB messages, Marten-persisted events, and Polecat-persisted events — so one schema language covers every boundary where data crosses a process or service edge.

The Kafka part of that is real. `DriverLocationUpdated` crosses the Telemetry → Dispatch boundary as **binary protobuf** on `telemetry.driver-location-updated`: Telemetry's topic endpoint calls `.UseProtobufSerialization()` (`src/CritterCab.Telemetry/Program.cs`), and Dispatch's listener does the same. The `.proto` governs the wire, not only the C# type — so a Kafka event contract follows every convention in this skill (location under `/protos`, package naming, field numbering, breaking-change classification, `buf lint`) exactly as an RPC does. The `dispatch/v1` business-event protos are likewise authored for Azure Service Bus.

What is not a commitment: Marten-persisted events. Domain events stay C# records (per `domain-event-conventions`); extending protobuf to the event store would need its own ADR.

---

## Common Pitfalls

- **Authoring C# types first and deriving the proto.** This is exactly the workflow ADR-009 rules out. Author the `.proto` first; generate the C# stubs; then implement.
- **Reusing a removed field number.** Catastrophic. Always `reserved`. `buf breaking` catches this; reading buf's output is part of the workflow.
- **Borrowing a value type out of another bounded context's package.** A location type defined in `crittercab.dispatch.v1` and imported by `crittercab.trips.v1` couples Trips to a Dispatch-internal type neither owns as a contract. Shared value types live in `crittercab.common.v<n>` (`common/v1/location.proto`). This is not the same as subscribing to a **published event contract**: Dispatch compiling Telemetry's `driver_location_updated.proto` to consume `DriverLocationUpdated` is published language, and the message stays in `crittercab.telemetry.v1` (§ Shared value types vs. published event contracts).
- **Diverging `csharp_namespace` or nesting a published message.** Each subscriber compiles its own copy, and Wolverine matches by `Type.FullName`. Change the namespace option on one side, or nest the message, and the two services stop agreeing on the message type.
- **Skipping `_UNSPECIFIED = 0` on enums.** proto3 requires the zero value to be unspecified. Without it, a default-valued enum field is indistinguishable from one explicitly set to the first real value.
- **Using `float` or `double` for money.** Use integer minor units plus a currency, as `get_fare_quote.proto` and `ride_assigned.proto` do, or a shared `Money` message if one is authored in `common/v1` (none exists yet). See `csharp-coding-standards` § Decimal Calculations.
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
- `wolverine-grpc-handlers` — handler patterns for unary, server-streaming, and client-streaming RPCs.
- `wolverine-grpc-bidirectional-handlers` (archived) — bidirectional RPC handlers; no bidirectional RPC exists.
- `wolverine-kafka` — the Kafka transport that carries `DriverLocationUpdated` as binary protobuf.
- `transport-selection` — when to choose gRPC vs Kafka or ASB for a given flow (Phase 1).

**External:**

- ADR-009 in [`docs/decisions/`](../../decisions/) — protobuf contracts as first-class artifacts.
- [`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md) § Protobuf Contracts — the immutable rules.
- [Buf style guide](https://buf.build/docs/best-practices/style-guide/) — naming and structure conventions buf enforces.
- [Protobuf Language Guide (proto3)](https://protobuf.dev/programming-guides/proto3/) — the canonical reference for proto3 semantics.
- [Wolverine gRPC documentation](https://wolverinefx.net/guide/grpc/) — Wolverine 5.32+ gRPC integration.
