---
name: cli-grpc-tooling
description: "CLI tools for working with gRPC and protobuf in CritterCab: buf for proto-file governance (lint, format, breaking-change detection, code generation, the workspace config at /protos/buf.yaml, and the GitHub Actions buf-breaking gate), grpcurl for command-line gRPC calls (unary and server-streaming, plaintext vs Aspire dev-cert TLS, reflection-based and proto-file-based modes), and Evans for an interactive gRPC REPL with auto-completion. Covers per-tool installation, the canonical Cab invocations, the dev-cert handling for Aspire-orchestrated services, and the reflection registration that enables both grpcurl and Evans to discover services without local proto files. Use when authoring or modifying buf configuration, running proto validation locally or in CI, calling a Cab gRPC service from the command line for smoke tests or debugging, or exploring an unfamiliar service interactively."
cluster: infrastructure
tags: [cli, grpc, buf, grpcurl, evans, proto, breaking-change, lint, reflection, ci, adr-009, tls, dev-cert]
---

# gRPC CLI Tooling

Three tools cover the command-line surface for gRPC and protobuf work in CritterCab:

- **buf** — the proto-file governance CLI. Lints `.proto` files against the style guide, detects breaking changes against `main`, formats consistently, generates code (Cab uses this for the Go service; C# stubs come from `.csproj` `<Protobuf Include="..." />` items at build time). The `buf breaking` check is the canonical CI gate that ADR-009 commits to.
- **grpcurl** — the curl equivalent for gRPC. Sends one-shot unary or server-streaming requests over the wire, against either reflection-discovered or proto-file-described services. The everyday tool for "does this endpoint actually work?" smoke tests.
- **Evans** — an interactive REPL for gRPC services. Auto-completes service names, method names, and field types as you type; useful when exploring an unfamiliar service or composing nontrivial requests. CLI mode (non-REPL) is also available for scripted use.

The three are complementary, not redundant. **buf** is for the contract; **grpcurl** is for one-shot calls; **Evans** is for interactive exploration. A typical Cab debugging session might run all three in the same hour: `buf lint` to confirm the proto change is well-formed, `grpcurl` to send a single test call, and Evans to drill into a method whose request shape isn't memorized.

This skill operationalizes the parts of `protobuf-contracts` that the contract skill deliberately deferred: the buf workspace configuration at `/protos/buf.yaml`, the GitHub Actions workflow that runs `buf breaking`, and the per-tool invocation patterns. It also fulfills the forward references from `wolverine-grpc-handlers` to the client-side test surface.

---

## When to apply this skill

Use this skill when:

- Authoring or modifying `/protos/buf.yaml` or `/protos/buf.gen.yaml`.
- Running `buf lint`, `buf breaking`, `buf format`, or `buf generate` locally.
- Wiring or modifying the GitHub Actions workflow that gates `buf breaking` in PRs.
- Sending a one-shot gRPC request from the command line (smoke tests, debugging, CI checks).
- Exploring an unfamiliar Cab gRPC service interactively before writing client code.
- Configuring TLS handling (Aspire dev cert vs production cert) for grpcurl or Evans.
- Setting up gRPC reflection in a Cab service for dev-time tooling.

Do NOT use this skill for:

- Authoring the `.proto` file itself — `protobuf-contracts`.
- Server-side handler implementation — `wolverine-grpc-handlers` (Phase 3) and `wolverine-grpc-bidirectional-handlers` (Phase 4).
- Service-internal CLI commands (`db-apply`, `wolverine-diagnostics codegen-preview`, etc.) — `cli-jasperfx`.
- Aspire AppHost CLI — `cli-aspire`.
- Integration-test fixtures using gRPC clients in C# — `testing-integration` (and `testing-advanced` for streaming patterns in Phase 4).

---

## Tools at a glance

| Tool | Primary use | Cab daily-driver scenario |
|---|---|---|
| `buf` | Proto governance: lint, breaking-change detection, formatting, Go-side code generation | Pre-PR validation; CI breaking-change gate; pre-commit hook formatting |
| `grpcurl` | One-shot gRPC calls | Smoke-testing a service after deploy; reproducing a bug from the command line; CI smoke checks |
| `Evans` | Interactive REPL | Exploring an unfamiliar service; composing a non-trivial nested request once; demos |

Cab installs all three via the developer's package manager (Homebrew on macOS, scoop or winget on Windows). Pinning is not required — these are dev tools, not runtime dependencies.

---

## buf — proto governance

### Installation

```bash
# macOS
brew install bufbuild/buf/buf

# Windows (scoop)
scoop install buf

# Linux (one approach — see https://buf.build/docs/installation for others)
BIN="/usr/local/bin" && \
  VERSION="latest" && \
  curl -sSL "https://github.com/bufbuild/buf/releases/${VERSION}/download/buf-$(uname -s)-$(uname -m)" \
    -o "${BIN}/buf" && chmod +x "${BIN}/buf"
```

Verify:

```bash
buf --version
```

### The Cab buf workspace

Cab uses a single buf v2 workspace at `/protos/buf.yaml` covering every proto module in the repository. This is the single source of truth for lint and breaking-change rules across all services.

```yaml
# /protos/buf.yaml
version: v2

# Each entry is a module — a directory tree of .proto files that buf treats as a unit.
# Modules in the workspace can import each other without explicit deps declarations.
modules:
  - path: common
  - path: dispatch
  - path: trips
  - path: telemetry
  - path: pricing
  - path: identity
  # Add a module per service that exposes a gRPC contract.

# Workspace-level lint defaults. Module entries can override these by setting
# their own lint block — but Cab doesn't, on purpose. Uniform rules across all
# services keep cross-service contracts consistent.
lint:
  use:
    - STANDARD
  # Cab does NOT include the UNARY_RPC category. That category contains
  # RPC_NO_CLIENT_STREAMING and RPC_NO_SERVER_STREAMING, which would forbid
  # the streaming RPCs Cab uses deliberately (StreamDriverOffers,
  # WatchTripStatus, PushTelemetry — see transport-selection). Since
  # UNARY_RPC isn't in STANDARD, Cab simply doesn't enable it.

# Workspace-level breaking-change defaults. FILE is conservative and matches
# what most teams need; it catches every wire-incompatible change.
breaking:
  use:
    - FILE
```

### Why these specific choices

- **`version: v2`** — the current buf config schema. The v1 / v1beta1 schemas are deprecated and use the now-defunct `buf.work.yaml` for workspaces; v2 collapses workspace and module config into one file.
- **`lint.use: [STANDARD]`** — buf's recommended baseline. Includes `BASIC` plus style rules like `ENUM_ZERO_VALUE_SUFFIX` (enforces `_UNSPECIFIED` per `protobuf-contracts`), `FIELD_LOWER_SNAKE_CASE`, `SERVICE_SUFFIX` (enforces the `Service` suffix Cab already uses), and `RPC_REQUEST_RESPONSE_UNIQUE` / `RPC_REQUEST_STANDARD_NAME` / `RPC_RESPONSE_STANDARD_NAME` (which together enforce the `<Method>Request` / `<Method>Response` pattern).
- **No `UNARY_RPC` category** — that category contains the rules that forbid streaming. Cab uses streaming deliberately (`StreamDriverOffers`, `WatchTripStatus`, `PushTelemetry`), so this category is omitted, not relaxed via `except`.
- **`breaking.use: [FILE]`** — buf's most conservative setting. Catches every change that breaks consumers at the wire level. The alternative `WIRE` skips some name-only checks; Cab uses the stricter `FILE` because the C#/Go consumers regenerate stubs and would notice name changes too.

### `buf lint` — style enforcement

```bash
# From the repo root, lint every module in the workspace
cd protos && buf lint
```

`buf lint` runs against the workspace defined by the nearest `buf.yaml` discovered upward from the working directory. Running it from `/protos` (or with `buf lint protos` from the repo root) is the canonical Cab invocation.

Output is per-file, with line and column numbers. A clean run prints nothing and exits 0:

```
$ buf lint
$ echo $?
0
```

A run with violations prints the violation list and exits non-zero:

```
$ buf lint
dispatch/v1/dispatch.proto:14:3:Field name "RideRequestID" should be lower_snake_case, such as "ride_request_id".
dispatch/v1/dispatch.proto:23:1:RPC method "request_ride" should be PascalCase, such as "RequestRide".
$ echo $?
100
```

`buf lint` is the canonical pre-commit and pre-PR check. CI runs it too — lint failures fail the build.

### `buf format` — formatting

```bash
# Format in place (the daily-driver invocation)
buf format -w

# Print the formatted version of one file without modifying it
buf format dispatch/v1/dispatch.proto

# Diff what would change without modifying files
buf format -d
```

`buf format` is the proto equivalent of `go fmt` or `dotnet format`. Cab convention: run with `-w` after every nontrivial proto change. CI runs `buf format -d` and fails the build if any output is produced — formatting drift is rejected at the PR boundary.

### `buf breaking` — the canonical CI gate

This is the single most important command in the skill. ADR-009 commits to mechanical breaking-change detection at the PR boundary; `buf breaking` is the implementation.

```bash
# Locally: check the working directory against main
buf breaking --against '.git#branch=main'

# Or against a specific tag
buf breaking --against '.git#tag=v1.2.0'

# Or against a remote
buf breaking --against 'https://github.com/your-org/crittercab.git#branch=main'
```

The `--against` argument is the baseline. `.git#branch=main` reads the `main` branch from the local Git repository — fast and offline. The remote form fetches over HTTPS — useful in CI where the checkout might not have full Git history.

Example output for a breaking change:

```
$ buf breaking --against '.git#branch=main'
dispatch/v1/dispatch.proto:14:1:Previously present field "1" with name "ride_request_id" on message "RequestRideRequest" was deleted.
$ echo $?
100
```

A clean run is silent and exits 0. The non-zero exit code is what gates the PR.

### `buf build` — building images

`buf build` produces a **buf image** — a serialized `FileDescriptorSet` that other commands consume:

```bash
# Build an image for the workspace (writes to bufimage.binpb by default)
buf build -o bufimage.binpb

# Build with explicit format
buf build -o bufimage.json
```

Cab doesn't typically run `buf build` directly. It's relevant when:

- Diffing schema across major versions for a release announcement (build images for v1 and v2, then compare).
- Producing a static descriptor set to feed into a tool that doesn't support `.proto` source directly (rare for Cab; mostly relevant for some BSR-adjacent tooling).

For the daily breaking-change check, `buf breaking --against '.git#branch=main'` does the build implicitly.

### `buf generate` and `buf.gen.yaml` — for the Go service

C# stubs come from `.csproj`'s `<Protobuf Include="..." />` items per `protobuf-contracts` § Generated Code Policy — protoc runs at build time as part of the .NET build pipeline. `buf generate` is **not used for C# in Cab**.

For the Go service (per `polyglot-go-service` in Phase 4), `buf generate` is the canonical code-gen path. The configuration lives at `/protos/buf.gen.yaml`:

```yaml
# /protos/buf.gen.yaml
version: v2

# Clean output directories before generating — prevents stale files from
# accumulating when proto files are removed.
clean: true

# Managed mode applies file-level options (csharp_namespace, go_package, etc.)
# automatically based on rules. Cab does NOT use managed mode for csharp_namespace
# (that's pinned per-file in the .proto so it's visible at the contract layer
# per protobuf-contracts § File Layout). For go_package, managed mode is fine
# because the Go service is downstream of the contract.
managed:
  enabled: true
  override:
    - file_option: go_package_prefix
      value: github.com/crittercab/cab-go/gen

plugins:
  # Go message types (structs).
  - remote: buf.build/protocolbuffers/go
    out: ../services/cab-go/gen
    opt: paths=source_relative

  # Go gRPC client + server stubs.
  - remote: buf.build/grpc/go
    out: ../services/cab-go/gen
    opt:
      - paths=source_relative
      - require_unimplemented_servers=false
```

```bash
# Run from /protos
buf generate
```

Output goes to `services/cab-go/gen/` (relative to the `/protos` workspace). The generated files are in `.gitignore` per ADR-009 — they're build artifacts, not source.

### CI integration: GitHub Actions workflow

The breaking-change gate runs on every PR. The Cab convention is a dedicated workflow file:

```yaml
# .github/workflows/proto-governance.yml
name: Proto Governance

on:
  pull_request:
    paths:
      - 'protos/**'
      - '.github/workflows/proto-governance.yml'

jobs:
  buf:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout (with full history for breaking-change comparison)
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Set up buf
        uses: bufbuild/buf-setup-action@v1

      - name: Lint
        uses: bufbuild/buf-lint-action@v1
        with:
          input: protos

      - name: Format check (no changes allowed)
        run: |
          cd protos
          buf format -d --exit-code

      - name: Breaking-change check
        uses: bufbuild/buf-breaking-action@v1
        with:
          input: protos
          against: 'https://github.com/${{ github.repository }}.git#branch=main,subdir=protos'
```

Three gates: lint, format, and breaking-change. Each gate is mandatory; a failure on any one blocks merge.

The `bufbuild/*-action@v1` actions are maintained by Buf and pin the buf version per workflow. The `against:` URL points back to the `main` branch's `protos/` subdirectory. `fetch-depth: 0` is required because `actions/checkout`'s default shallow clone doesn't include the history needed for the comparison.

### Locally running the full CI gate

```bash
# Reproduce what CI runs, in order:
cd protos
buf lint
buf format -d --exit-code  # exits non-zero if any formatting drift
buf breaking --against '.git#branch=main'
```

This three-line sequence is the canonical pre-PR check. Bind it to a Make target or a pre-push hook if it helps the inner loop.

---

## grpcurl — command-line gRPC calls

### Installation

```bash
# macOS
brew install grpcurl

# Windows (scoop)
scoop install grpcurl

# Linux / Go-installed
go install github.com/fullstorydev/grpcurl/cmd/grpcurl@latest
```

Verify:

```bash
grpcurl -version
```

### TLS flags — the daily-driver point of confusion

grpcurl has three mutually-exclusive transport modes. The flag names are subtle:

| Flag | Meaning | Cab use case |
|---|---|---|
| (no flag) | TLS, full cert verification | Production-like environments with a real cert |
| `-insecure` | TLS, **skip cert verification** | Aspire-orchestrated dev (self-signed dev cert) |
| `-plaintext` | **No TLS at all** (h2c) | Sample code, isolated test environments — **not Cab's daily flow** |

The naming trips up newcomers: `-insecure` does NOT mean "no TLS" (that's `-plaintext`); it means "TLS, but trust whatever cert the server presents." For Cab dev work against Aspire, `-insecure` is the right flag because the Aspire dev cert is self-signed.

```bash
# Cab dev — Aspire dev cert
grpcurl -insecure localhost:7233 list

# Production-like with a real cert
grpcurl crittercab-trips.example.com:443 list

# Sample code that runs HTTP/2 unencrypted (e.g., Wolverine's GreeterProtoFirstGrpc)
grpcurl -plaintext localhost:5003 list
```

### Listing services and methods (with reflection)

When a Cab service has gRPC reflection enabled (see § Reflection setup below), grpcurl can discover the service surface at runtime without proto files:

```bash
# List all services on the server
grpcurl -insecure localhost:7233 list

# Output:
# crittercab.trips.v1.Trips
# grpc.reflection.v1.ServerReflection
# grpc.reflection.v1alpha.ServerReflection

# List the methods on one service
grpcurl -insecure localhost:7233 list crittercab.trips.v1.Trips

# Output:
# crittercab.trips.v1.Trips.StartTrip
# crittercab.trips.v1.Trips.CompleteTrip
# crittercab.trips.v1.Trips.WatchTripStatus

# Describe a service in proto-like syntax
grpcurl -insecure localhost:7233 describe crittercab.trips.v1.Trips

# Describe a single message type
grpcurl -insecure localhost:7233 describe crittercab.trips.v1.StartTripRequest
```

`describe` is the most useful exploratory command — it prints a proto-like definition that's enough to compose a JSON request body.

### Calling a unary RPC

```bash
# Inline JSON
grpcurl -insecure \
  -d '{"trip_id": "01928d4c-7e91-7c2e-9d3e-1234567890ab", "rider_id": "01928d4c-7e91-7c2e-9d3e-fedcba987654", "driver_id": "01928d4c-7e91-7c2e-9d3e-aaaabbbbcccc"}' \
  localhost:7233 \
  crittercab.trips.v1.Trips/StartTrip

# Read the request from a file via stdin
cat start-trip-request.json | grpcurl -insecure \
  -d @ \
  localhost:7233 \
  crittercab.trips.v1.Trips/StartTrip
```

The method path uses `<package>.<service>/<method>` — note the slash, not a dot, between service and method. Output is JSON by default.

### Calling a server-streaming RPC

The same `-d` and method-path convention, but grpcurl prints each streamed response as it arrives:

```bash
grpcurl -insecure \
  -d '{"trip_id": "01928d4c-7e91-7c2e-9d3e-1234567890ab"}' \
  localhost:7233 \
  crittercab.trips.v1.Trips/WatchTripStatus

# Output streams:
# {
#   "tripId": "01928d4c-7e91-7c2e-9d3e-1234567890ab",
#   "status": "DRIVER_ASSIGNED",
#   "occurredAt": "2026-05-05T18:32:01.123Z"
# }
# {
#   "tripId": "01928d4c-...",
#   "status": "DRIVER_ARRIVED",
#   "occurredAt": "2026-05-05T18:34:17.842Z"
# }
# ...
```

Press Ctrl+C to terminate the client — the gRPC framework propagates the cancellation to the server, and the streaming handler's `[EnumeratorCancellation] CancellationToken` (per `wolverine-grpc-handlers`) terminates cleanly.

### Authentication and metadata headers

`-H` sets a single header / metadata entry. Repeat for multiple:

```bash
grpcurl -insecure \
  -H 'authorization: Bearer eyJ...' \
  -H 'x-correlation-id: 01928d4c-7e91-7c2e-9d3e-aaabbbcccddd' \
  -d '{"trip_id": "..."}' \
  localhost:7233 \
  crittercab.trips.v1.Trips/StartTrip
```

For Cab's identity-ACL flow (per `identity-acl` in Phase 3), this is how a developer obtains a dev-mode token from the local OpenIddict server and exercises a service endpoint directly — bypassing the API gateway / browser flow that production clients use.

### Proto-file mode (when reflection isn't available)

In production, reflection is typically disabled (per § Reflection setup below). For grpcurl against a production-style endpoint without reflection:

```bash
# Point grpcurl at the .proto sources directly
grpcurl -insecure \
  -import-path ./protos \
  -proto trips/v1/trips.proto \
  -d '{"trip_id": "..."}' \
  crittercab-trips.example.com:443 \
  crittercab.trips.v1.Trips/StartTrip
```

`-import-path` is the equivalent of protoc's `-I` flag — the directory(ies) where imports are resolved. `-proto` is the entry-point file. Multiple `-proto` flags are allowed.

### Output formatting

```bash
# Default: pretty JSON
grpcurl -insecure localhost:7233 ...

# Text format (the proto canonical text format)
grpcurl -insecure -format text ...

# Quiet (just the response body, useful for piping into jq)
grpcurl -insecure -format json ... | jq '.tripId'

# Verbose (request/response timing, header/trailer detail)
grpcurl -insecure -v ...
```

`-v` is the right flag when a response is not what was expected — it shows the request and response headers, status code, and trailers.

---

## Evans — interactive gRPC REPL

### Installation

```bash
# macOS
brew tap ktr0731/evans
brew install evans

# Windows (scoop)
scoop install evans

# Linux / Go-installed
go install github.com/ktr0731/evans@latest
```

Verify:

```bash
evans --version
```

### REPL mode against a Cab service

```bash
# Reflection mode (recommended for dev)
evans -r --host localhost --port 7233 --tls --cacert <path-to-aspire-dev-cert> repl

# When the dev cert isn't readily exportable: skip cert verification with --insecure-skip-verify
# (older Evans versions used --insecure for this; --insecure-skip-verify is current)
evans -r --host localhost --port 7233 --tls repl
```

Inside the REPL:

```
crittercab.trips.v1.Trips@localhost:7233> show service
+-------+-------------------+-----------------+------------------+
| TRIPS | RPC               | REQUEST         | RESPONSE         |
+-------+-------------------+-----------------+------------------+
|       | StartTrip         | StartTripRequest | StartTripResponse |
|       | CompleteTrip      | CompleteTripRequest | CompleteTripResponse |
|       | WatchTripStatus   | WatchTripStatusRequest | WatchTripStatusResponse |
+-------+-------------------+-----------------+------------------+

crittercab.trips.v1.Trips@localhost:7233> call StartTrip
trip_id (TYPE_STRING) => 01928d4c-7e91-7c2e-9d3e-1234567890ab
rider_id (TYPE_STRING) => 01928d4c-7e91-7c2e-9d3e-fedcba987654
driver_id (TYPE_STRING) => 01928d4c-7e91-7c2e-9d3e-aaaabbbbcccc
{
  "tripId": "01928d4c-7e91-7c2e-9d3e-1234567890ab",
  "startedAt": "2026-05-05T18:42:11.234Z"
}
```

Evans prompts for each field in the request message. Tab-completion works at every prompt — type a few characters of a service or method name and tab will complete it. For nested messages, Evans descends into each sub-message; `--dig-manually` toggles to opt-in dig mode (useful when most sub-fields should remain at defaults).

### Useful REPL commands

| Command | Purpose |
|---|---|
| `show package` | List packages discovered via reflection |
| `package <name>` | Switch to a package (changes the prompt) |
| `show service` | List services in the current package |
| `service <name>` | Switch to a service |
| `show message` | List message types |
| `desc <type>` | Describe a message type's fields |
| `call <method>` | Call an RPC, prompting for each field |
| `call --enrich <method>` | Call with full headers/trailers in the response |
| `call --repeat <method>` | Repeat the previous call with the same input |
| `header <key>=<value>` | Set a header for subsequent calls |
| `quit` | Exit the REPL |

### CLI mode (non-REPL)

For scripted use — e.g., a CI smoke test step — Evans CLI mode is more ergonomic than grpcurl for repeated invocations:

```bash
echo '{"trip_id": "01928d4c-..."}' | evans \
  -r --host localhost --port 7233 --tls \
  cli call crittercab.trips.v1.Trips.StartTrip
```

Or with a request file:

```bash
evans -r --host localhost --port 7233 --tls \
  cli call --file start-trip-request.json \
  crittercab.trips.v1.Trips.StartTrip
```

For one-off CLI calls, grpcurl is usually shorter to type. Reach for Evans CLI mode when scripting many calls in sequence and the per-call repetition of host/port/TLS flags becomes annoying — Evans reads `.evans.toml` from the working directory for default values:

```toml
# .evans.toml — checked in to the repo for shared defaults; per-developer
# overrides go in ~/.config/evans/config.toml
[default]
host = "localhost"
port = "7233"
reflection = true
tls = true
```

### When to reach for Evans vs. grpcurl

- **One-shot call, request shape memorized** → grpcurl. Shorter to type.
- **Don't remember the field names** → Evans REPL. Auto-completion + per-field prompts make exploration faster.
- **Demo / showing-someone-the-service** → Evans REPL. The interactive prompts read better in a screenshare.
- **Scripted in CI** → grpcurl. More portable, simpler binary.
- **Streaming RPC with many request items** → Evans REPL. The interactive prompt for each sent item is much easier than constructing inline JSON.

---

## Reflection setup in Cab services

Both grpcurl and Evans can work with local proto files (via `-import-path` / `-proto` for grpcurl, `--proto` for Evans), but reflection-based discovery is dramatically faster for the inner loop. Cab enables reflection in development and disables it in production.

### Server registration (per service)

```csharp
// src/CritterCab.Trips/Program.cs (additions to the bootstrap from
// service-bootstrap and wolverine-grpc-handlers)

builder.Services.AddGrpc();
builder.Services.AddWolverineGrpc();

// Dev-only: register the gRPC reflection service.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddGrpcReflection();
}

var app = builder.Build();
app.UseRouting();
app.MapWolverineGrpcServices();

// Dev-only: map the reflection endpoint.
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}
```

Two registrations: `AddGrpcReflection()` adds the reflection service to DI; `MapGrpcReflectionService()` exposes it as an endpoint. Both are dev-only.

### Why dev-only

gRPC reflection lets any caller enumerate the entire service surface — every package, service, method, and message type. In a production environment, this is information disclosure: it tells a potential attacker exactly what endpoints exist and what shapes they accept. The convention across the gRPC ecosystem is reflection in dev, no reflection in production. Cab follows that convention.

If a need for reflection-like discovery in production arises (e.g., an internal admin tool that needs runtime introspection), the right answer is a separate admin-only gRPC endpoint behind authentication, not enabling reflection on the main service.

### Verifying reflection is on

```bash
grpcurl -insecure localhost:7233 list
```

If reflection is registered, this returns the service list. If it isn't:

```
Failed to list services: server does not support the reflection API
```

That's the cue to go check the bootstrap.

---

## Common patterns

### Pre-PR validation

```bash
cd protos
buf format -w           # apply formatting
buf lint                # confirm style
buf breaking --against '.git#branch=main'   # confirm no breaking changes

# If all three pass, the proto change is ready for PR.
```

### Local debugging session

```bash
# Terminal 1: run the service through Aspire
aspire run

# Terminal 2: explore the service interactively
evans -r --host localhost --port 7233 --tls repl
crittercab.trips.v1.Trips@localhost:7233> call StartTrip
# ...
```

### CI smoke test (post-deploy gate)

```bash
# After a deploy, confirm the gRPC surface is up before flipping the load balancer
grpcurl crittercab-trips-staging.example.com:443 list \
  | grep -q 'crittercab.trips.v1.Trips' \
  || { echo "Trips service not exposing expected surface"; exit 1; }

grpcurl \
  -d '{"trip_id":"smoke-test-01928d4c"}' \
  crittercab-trips-staging.example.com:443 \
  crittercab.trips.v1.Trips/GetTripDetails \
  | jq -e '.tripId == "smoke-test-01928d4c"' \
  || { echo "Trips smoke test failed"; exit 1; }
```

In production-style environments without reflection, swap to proto-file mode (`-import-path ./protos -proto trips/v1/trips.proto`).

### Reproducing a bug from a server log

When a server log shows a request body and a failing response, paste the request body into a JSON file and replay with grpcurl:

```bash
# Server log line shows the request, capture it to file:
echo '{"trip_id": "...", "rider_id": "..."}' > /tmp/repro.json

# Replay against the service
grpcurl -insecure -d @ -v \
  localhost:7233 \
  crittercab.trips.v1.Trips/StartTrip \
  < /tmp/repro.json
```

The `-v` flag is the value here — it shows the response status, trailers, and headers, which usually contain the diagnostic detail the log line was missing.

---

## Common pitfalls

- **Confusing `-plaintext` and `-insecure` in grpcurl.** `-plaintext` is "no TLS at all" (h2c). `-insecure` is "TLS but skip cert verification." For Aspire-orchestrated Cab services in dev, the right flag is `-insecure` because Aspire uses HTTPS with a self-signed dev cert. Using `-plaintext` against an HTTPS endpoint produces "first record does not look like a TLS handshake."
- **Forgetting `fetch-depth: 0` in the GitHub Actions checkout.** The `actions/checkout@v4` default does a shallow clone without history. `buf breaking` against `.git#branch=main` needs that history to compute the comparison. Without it, the action fails with a confusing "could not resolve reference" error.
- **Running `buf breaking` against `HEAD~1` instead of `main`.** Compares to the parent commit, not to the merge target. Useful for some forensic tasks but not for the PR gate — the right baseline is the merge target (`main`).
- **Adding `RPC_NO_CLIENT_STREAMING` or `RPC_NO_SERVER_STREAMING` to `lint.except`.** Those rules aren't in `STANDARD`; they're in the `UNARY_RPC` extra category that Cab doesn't enable. Adding them to `except` is a no-op because they're not active to begin with.
- **Generating C# stubs with `buf generate`.** Cab's C# stubs come from `.csproj` `<Protobuf Include="..." />` items, not from `buf generate`. `buf.gen.yaml` only configures the Go-side code-gen. Adding a C# plugin to `buf.gen.yaml` produces stubs that aren't wired into the .NET build pipeline — they go stale immediately.
- **Reflection enabled in production.** Cab's environment guard (`if (Environment.IsDevelopment())`) keeps reflection out of prod. A bug that flips this guard to "always enabled" is an information-disclosure regression that should fail review.
- **Forgetting that the method-path separator is a slash, not a dot.** grpcurl: `<host:port> <package>.<service>/<method>`. Confusing `Trips.StartTrip` (would-be method) with `Trips/StartTrip` (correct) produces "method not found" errors.
- **Trying to call a streaming RPC with grpcurl by sending one request and expecting one response.** Server-streaming returns multiple JSON objects, separated by line breaks. The shell command does NOT terminate when one response arrives — it keeps reading until the server closes the stream or Ctrl+C is hit.
- **Running `buf format -d` and missing the exit code.** Without `--exit-code`, `buf format -d` prints the diff but exits 0 even when output exists. The CI invocation must include `--exit-code` to fail on formatting drift.
- **Hand-editing files in `services/cab-go/gen/` (or any `obj/Generated/` for C#).** Generated files are build artifacts. Edits are lost on the next regenerate. Modify the `.proto`, regenerate.
- **Pinning buf to a specific version in `buf.yaml`.** `buf.yaml` doesn't have a buf-version field. Version pinning happens in CI via `bufbuild/buf-setup-action@v1`'s `version:` parameter, and locally by whatever version the developer's package manager installed. This is intentional — buf has been backwards-compatible across patch and minor versions.
- **Calling Evans with `--insecure` and getting a TLS handshake error.** Older Evans versions used `--insecure` for "skip cert verification"; current versions use `--insecure-skip-verify`. The `-insecure` flag in current Evans is for explicit no-TLS, similar to grpcurl's `-plaintext`. When the daily-driver flag is wrong, the symptom is "EOF" or "connection reset by peer."
- **Running `buf lint` from outside the workspace root.** `buf lint` from `/` (with no arguments) fails because there's no `buf.yaml` in the working directory. Running with the explicit input path (`buf lint protos`) or `cd protos && buf lint` is the right invocation.

---

## See also

**Upstream** — load these first:

- `protobuf-contracts` — the contract authoring rules that this skill operationalizes via buf. The `/protos/buf.yaml` rules in this skill enforce the conventions in `protobuf-contracts`.
- `transport-selection` — the higher-level decision of when to use gRPC at all; shapes which streaming kinds appear in the proto files this skill validates.

**Sibling skills:**

- `wolverine-grpc-handlers` — server-side handler authoring; this skill's grpcurl/Evans invocations target services authored per that skill.
- `aspire` — the Aspire dev cert that this skill's `-insecure` flag is for; `aspire run` is the orchestration that produces the `localhost:7233` endpoint grpcurl/Evans call into.
- `cli-aspire` — the Aspire CLI for orchestrating; complements this skill for the dev inner loop (Aspire orchestrates; gRPC tools call into the orchestrated services).
- `cli-jasperfx` — the in-service CLI surface; `wolverine-diagnostics codegen-preview --grpc` is the server-side complement to grpcurl/Evans's client-side inspection.

**Downstream:**

- `polyglot-go-service` (Phase 4) — the Go service that consumes `buf generate` output from `/protos/buf.gen.yaml`.
- `wolverine-grpc-bidirectional-handlers` (Phase 4) — client-streaming and bidirectional patterns; grpcurl supports both via stdin streaming and Evans REPL supports them interactively.
- `testing-integration` — integration tests use in-process gRPC clients via `WebApplicationFactory`, not external CLI tools, but the JSON shapes used in CLI smoke tests carry over.
- `identity-acl` (Phase 3) — the OpenIddict-issued tokens that grpcurl's `-H 'authorization: Bearer ...'` carries in dev-mode tests.
- `observability-tracing` (Phase 3) — when a grpcurl call surfaces a problem, the traces it produces flow into the same OpenTelemetry pipeline as the rest of Cab's calls.

**External:**

- ai-skills `buf-tooling` and `grpc-cli` — generic Critter-Stack-adjacent skills if/when JasperFx publishes them. Complements this skill.
- All ai-skills installed via `npx skills add` (license required).
- [Buf documentation](https://buf.build/docs) — canonical reference for buf v2 configuration, lint rules, breaking-change rules, and the BSR.
- [Buf style guide](https://buf.build/docs/best-practices/style-guide) — the conventions `lint.use: [STANDARD]` enforces.
- [Buf lint rules and categories](https://buf.build/docs/lint/rules) — full rule reference; the `UNARY_RPC` category is the one Cab deliberately does NOT include.
- [grpcurl on GitHub](https://github.com/fullstorydev/grpcurl) — the canonical grpcurl reference, including the full flag list and the `-plaintext` vs `-insecure` documentation.
- [Evans on GitHub](https://github.com/ktr0731/evans) — REPL and CLI mode docs, `.evans.toml` configuration.
- [gRPC server reflection protocol](https://github.com/grpc/grpc/blob/master/doc/server-reflection.md) — the protocol grpcurl and Evans use to discover services at runtime.
- [ASP.NET Core gRPC reflection](https://learn.microsoft.com/aspnet/core/grpc/test-tools) — `AddGrpcReflection()` and `MapGrpcReflectionService()` documentation, with the dev-only guidance Cab follows.
- ADR-009 in [`docs/decisions/`](../../decisions/) — the decision that produces the buf governance Cab implements here.
