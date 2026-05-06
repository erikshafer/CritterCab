---
name: polyglot-go-service
description: "The Go service in CritterCab — `cab-go`, a polyglot sidecar that consumes the same protobuf contracts as the .NET services and demonstrates cross-language interop within the project. Cab's specific use case is a geospatial matchmaking service for Dispatch (subscribes to the Telemetry Kafka topic for driver position pings, maintains an in-memory spatial index, answers FindNearestDrivers gRPC queries) — Go's goroutine concurrency model fits the high-fanout-low-CPU-per-request shape. Covers the project layout (services/cab-go/ with cmd/, internal/, gen/, go.mod, Dockerfile), the buf generate workflow that produces Go stubs at services/cab-go/gen/ from /protos/buf.gen.yaml (go.mod path github.com/crittercab/cab-go), Aspire integration via AddContainer (Cab default — Dockerfile-based) or AddExecutable (alternative for native go run during inner-loop), the WithReference/WaitFor/WithEndpoint patterns that wire Dispatch's gRPC client to the Go service through Aspire service discovery, gRPC health-checking protocol for Aspire dashboard liveness, OTLP environment-variable propagation for traces/metrics/logs to the dashboard, and bearer-token authentication via the same boundary documented in identity-acl. Out of scope: Go-language idioms, Go module ecosystem choices beyond proto consumption, deployment topology beyond Aspire local-dev orchestration. The skill is Cab-specific patterns at the polyglot boundary, not a Go tutorial."
cluster: polyglot
tags: [go, polyglot, grpc, protobuf, buf-generate, aspire, addcontainer, addexecutable, matchmaking, dispatch-bc, otlp, health-checking]
---

# The Go Service: `cab-go`

CritterCab is a polyglot reference architecture. Most services are .NET, but the project deliberately includes one Go service — `cab-go` — to demonstrate that the protobuf-first contract surface (per `protobuf-contracts`) genuinely supports cross-language consumers and that Aspire's local-dev orchestration handles non-.NET resources cleanly. This skill documents the patterns Cab uses at that polyglot boundary.

The single most useful framing: **`cab-go` is a normal Cab service that happens to be written in Go.** It consumes the same `.proto` files from `/protos/`, runs in the same Aspire AppHost graph, participates in the same OpenTelemetry pipeline, accepts the same Cab-issued bearer tokens at its boundary, and shows up in the same Aspire dashboard. The Go-ness is an implementation detail; the contract surface is identical to a .NET service. What's different is the toolchain (`buf generate` instead of MSBuild's `<Protobuf Include>`), the Aspire registration shape (`AddContainer` instead of `AddProject`), and a handful of Go-specific operational patterns (gRPC health-checking via the standard protocol, OTLP propagation through environment variables instead of Aspire's .NET auto-instrumentation).

This skill assumes `protobuf-contracts` for the proto authoring conventions, `cli-grpc-tooling` for the `buf generate` workflow and the canonical `/protos/buf.gen.yaml` configuration that produces Go stubs, `aspire` for the AppHost shape and `WithReference`/`WaitFor` patterns, and `identity-acl` for the bearer-token boundary the Go service enforces. It is **not** a Go tutorial — Go module choices, library selection, and idiomatic Go style are out of scope. What's in scope is the proto consumption boundary, the Aspire wiring, and the operational integration with Cab's existing observability and identity stories.

**Cab's specific use case for `cab-go`:** a **geospatial matchmaking service** that supports the Dispatch BC. The matchmaker subscribes to the Telemetry BC's Kafka topic for driver position pings, maintains an in-memory spatial index (R-tree or geohash grid) keyed by driver, and exposes a gRPC `FindNearestDrivers(GeoPoint, int K) → DriverCandidates` RPC that Dispatch calls during ride matching. The reason this shape fits Go specifically: high fanout (thousands of position updates per second from Kafka) with low per-request CPU (an R-tree lookup is a handful of microseconds), goroutines map naturally to "one goroutine per Kafka partition consumer plus one per gRPC request," and Go's standard library covers everything needed without ceremony. The same workload in .NET would work but wouldn't exercise the polyglot demonstration the project exists to provide.

---

## When to apply this skill

Use this skill when:

- Adding the `cab-go` service to a Cab AppHost graph for the first time.
- Wiring a .NET Cab service (Dispatch, primarily) to call `cab-go` via gRPC through Aspire service discovery.
- Producing Go stubs from `/protos/` via `buf generate` — the configuration in `/protos/buf.gen.yaml` is documented here and in `cli-grpc-tooling`.
- Choosing between `AddContainer` (Dockerfile-based) and `AddExecutable` (native binary) for the Go service in the AppHost.
- Setting up the Go service's OTLP propagation, gRPC health-checking, or bearer-token boundary.
- Diagnosing why Dispatch can't reach `cab-go` through Aspire's service-discovery shape, or why traces don't span across the .NET → Go hop.

Do NOT use this skill for:

- Authoring `.proto` files — `protobuf-contracts` covers naming, layout, and the breaking-change governance.
- The `buf generate` configuration in detail — `cli-grpc-tooling` § `buf generate` and `buf.gen.yaml` is canonical.
- Aspire fundamentals — `aspire` covers the AppHost shape, `WithReference`, `WaitFor`, and service discovery from the .NET side.
- Identity and authentication — `identity-acl` covers the bearer-token boundary; this skill says how `cab-go` enforces it, not how Cab issues tokens.
- gRPC handler patterns on the .NET side — `wolverine-grpc-handlers` and `wolverine-grpc-bidirectional-handlers` cover those.
- Go language patterns or library selection — out of scope. The skill assumes the reader can author Go code; it documents the boundaries and integration points, not the body.

---

## Why Cab has a Go service

Cab is a reference architecture. One of its goals is to demonstrate that the contract surface (proto-first, per ADR-009) is genuinely language-neutral, not just .NET-with-extra-steps. A pure-.NET project can claim that property; a polyglot project actually exercises it. `cab-go` is the smallest credible Go service that exercises:

- **Proto consumption** in a non-.NET language. The same `.proto` files compile via `buf generate` to Go stubs that interop transparently with the .NET-generated stubs over the wire. This is the headline demonstration.
- **Cross-language gRPC** between a Go server (`cab-go`) and a .NET client (Dispatch). HTTP/2 framing, status codes, deadlines, and metadata propagate identically.
- **Cross-language Kafka consumption.** `cab-go` subscribes to a Kafka topic produced by .NET services (Telemetry); the Confluent Go client and the Wolverine.Kafka producer interoperate at the protocol level without ceremony. This validates that the proto-encoded message envelope from `wolverine-kafka` is portable.
- **Cross-language OpenTelemetry** propagation. A trace started by a mobile-client gRPC call into Dispatch, which then calls `cab-go`, surfaces in the Aspire dashboard as a single trace tree spanning both processes — provided OTLP env-vars are wired correctly. This is the integration the polyglot story has to actually deliver.
- **Cross-language identity propagation.** Bearer tokens issued by Cab's identity surface (per `identity-acl`) carry into `cab-go` over gRPC metadata and validate against the same OpenIddict-issued JWT that the .NET services accept.

The matchmaker use case was chosen because it's the smallest non-trivial workload that exercises all five demonstrations simultaneously, and because the geospatial-fanout shape is one Go is genuinely well-suited to. Alternative use cases the project considered:

- **CDN-edge proximity worker.** Plausible but Cab doesn't have an established edge story; introducing one alongside the polyglot story doubles the demonstration surface and dilutes both.
- **Pricing surge calculator.** Plausible but would require carving compute out of the .NET-implemented Pricing BC, which complicates the BC-integrity story for marginal demonstration value.

The matchmaker stays the recommended use case unless a future ADR commits otherwise.

---

## Project layout

The Go service lives at `services/cab-go/` in the Cab repo:

```
services/cab-go/
├── cmd/
│   └── cab-go/
│       └── main.go              # Entry point: bootstrap, OTLP, gRPC server start, Kafka consumer start
├── internal/
│   ├── matchmaker/              # Domain: spatial index, candidate selection
│   ├── server/                  # gRPC server adapters (proto stubs → matchmaker calls)
│   ├── consumer/                # Kafka consumer for telemetry pings
│   ├── auth/                    # Bearer-token validation (mirrors identity-acl boundary)
│   └── observability/           # OTLP setup, health-check registration
├── gen/                         # buf generate output — gitignored
│   └── critter/
│       └── cab/
│           └── ...              # Generated Go packages from /protos/
├── go.mod                       # Module: github.com/crittercab/cab-go
├── go.sum
├── Dockerfile                   # Used by Aspire AddContainer
└── README.md                    # How to build and run locally
```

The structure follows the standard Go layout: `cmd/<binary-name>/main.go` for the entry point, `internal/` for non-exported domain code that other Go modules can't import. The generated proto code lands in `gen/` per the `/protos/buf.gen.yaml` configuration documented in `cli-grpc-tooling` § `buf generate`. The `gen/` directory is gitignored — `buf generate` is part of the build, not source.

The Go module path is **`github.com/crittercab/cab-go`** (committed in `protos/buf.gen.yaml` via the `go_package_prefix` managed-mode override). This matters because the generated code's `option go_package` lines are derived from this prefix; changing it requires updating the buf configuration AND running `buf generate` AND committing the regenerated stubs in any consumer that vendored them.

`go.mod` declares the module:

```go
module github.com/crittercab/cab-go

go 1.23

require (
    google.golang.org/grpc v1.66.0
    google.golang.org/protobuf v1.34.0
    github.com/confluentinc/confluent-kafka-go/v2 v2.5.0
    go.opentelemetry.io/otel v1.30.0
    go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracegrpc v1.30.0
    go.opentelemetry.io/otel/exporters/otlp/otlpmetric/otlpmetricgrpc v1.30.0
    go.opentelemetry.io/otel/sdk v1.30.0
)
```

Specific module versions are not committed here — they live in `go.mod` and `go.sum` and update on their own cadence. The point of listing them is to call out the dependencies the operational integration needs: gRPC, protobuf-go, the Confluent Kafka Go client (matching the Wolverine.Kafka producer side), and the OpenTelemetry trace + metric OTLP-gRPC exporters.

The `Dockerfile` for the Aspire `AddContainer` path is conventional Go-multi-stage:

```dockerfile
# Stage 1: build
FROM golang:1.23-alpine AS build
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 go build -o /out/cab-go ./cmd/cab-go

# Stage 2: runtime
FROM gcr.io/distroless/static-debian12
COPY --from=build /out/cab-go /cab-go
USER nonroot:nonroot
EXPOSE 50051
ENTRYPOINT ["/cab-go"]
```

`distroless/static-debian12` is the minimal runtime image — no shell, no package manager, just the binary. The matchmaker uses Confluent's Go Kafka client which links against `librdkafka`; if Cab settles on a CGo-dependent Kafka client, swap the build stage to `golang:1.23-bookworm` and the runtime to `gcr.io/distroless/base-debian12` or a glibc-based image. The default above assumes a pure-Go Kafka client (e.g., `segmentio/kafka-go`) — verify against the actual choice.

---

## The matchmaker domain (briefly)

Enough to make the integration concrete. Not a Go tutorial.

**Inbound flow — Kafka:** the Telemetry BC produces position pings to a Kafka topic (`telemetry.driver-positions` per `wolverine-kafka` conventions). Each ping is a proto-encoded message: `DriverId`, `GeoPoint { Lat, Lon }`, `Timestamp`. The Go service runs one Kafka consumer goroutine per topic partition, deserializes the proto, and updates an in-memory spatial index keyed by `DriverId`. The index is a concurrent-safe R-tree or geohash grid; choice of structure is a Go-internal decision out of scope here.

**Outbound flow — gRPC server:** Dispatch (the .NET service) calls `MatchmakerService.FindNearestDrivers(GeoPoint origin, int32 k) → DriverCandidates`. The Go service runs a gRPC server bound to a port (`50051` by Cab convention), looks up the K nearest drivers from the spatial index, and returns their `DriverId`s plus distances. The proto contract lives in `/protos/critter/cab/dispatch/matchmaker_service.proto` per `protobuf-contracts` § File Layout.

**State:** the spatial index is in-memory only. If the service restarts, it rebuilds from the Kafka topic's retained history (the Telemetry topic retention is sized to cover this). No database, no persistent state in `cab-go`. This is intentional — the matchmaker is a CQRS read-model rebuilt from the upstream Kafka log.

The handler shape inside `cab-go` is roughly:

```go
// internal/server/matchmaker_grpc.go
type matchmakerServer struct {
    pb.UnimplementedMatchmakerServiceServer
    index *matchmaker.SpatialIndex
}

func (s *matchmakerServer) FindNearestDrivers(
    ctx context.Context,
    req *pb.FindNearestDriversRequest,
) (*pb.FindNearestDriversResponse, error) {
    candidates := s.index.NearestK(req.Origin, int(req.K))
    return &pb.FindNearestDriversResponse{Candidates: candidates}, nil
}
```

The proto-generated types (`pb.FindNearestDriversRequest`, etc.) come from `gen/critter/cab/dispatch/`. Authentication, telemetry, and the Kafka consumer wiring happen in surrounding code documented in the relevant sections below.

---

## Aspire integration: `AddContainer` vs `AddExecutable`

The AppHost `apphost.cs` registers `cab-go` alongside the .NET services. Two options:

- **`AddContainer`** — Aspire builds and runs the Docker image declared by `Dockerfile`. Production-shaped: the same artifact runs in the AppHost as in deployment. The Go binary doesn't need to exist on the dev's machine; only Docker does.
- **`AddExecutable`** — Aspire runs the binary directly via `go run` or a prebuilt binary. Faster inner loop (no Docker build per change), but requires the dev to have the Go toolchain installed and adds a divergence between AppHost shape and production shape.

**Cab's default: `AddContainer` with the Dockerfile.** Reasons: consistency with how non-.NET resources are normally orchestrated, no toolchain assumption on the dev's machine, and the build is cached so changes to non-Go files (proto regeneration, config) don't trigger rebuilds. The trade-off is Docker-build latency on the first `cab-go` change of a session; subsequent in-session iteration is fast because Docker layer caching kicks in.

`AddExecutable` is the documented escape hatch for two cases: dev machines where Docker is unavailable (or excessively slow), and active development on `cab-go` itself where the inner-loop iteration matters more than AppHost-shape consistency. Both are minority cases; `AddContainer` is the recommended default.

### `AddContainer` registration

```csharp
// apphost.cs
var matchmaker = builder
    .AddContainer("cab-go", "cab-go")
    .WithDockerfile("../services/cab-go")
    .WithHttpEndpoint(port: 50051, targetPort: 50051, name: "grpc")
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT",
        builder.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"])
    .WithEnvironment("OTEL_SERVICE_NAME", "cab-go")
    .WithEnvironment("CAB_AUTH_AUTHORITY",
        identity.GetEndpoint("http").Url);

dispatch
    .WithReference(matchmaker.GetEndpoint("grpc"))
    .WaitFor(matchmaker);
```

The mechanics:

- **`AddContainer("cab-go", "cab-go")`** — the first argument is the Aspire resource name (used for logs, dashboard labels, service-discovery keys); the second is the Docker image name. Cab uses the same string for both since the resource and image have a 1:1 relationship.
- **`WithDockerfile("../services/cab-go")`** — points Aspire at the Dockerfile context. Aspire builds the image as part of `dotnet run` against the AppHost.
- **`WithHttpEndpoint(port: 50051, targetPort: 50051, name: "grpc")`** — exposes the gRPC port. The `name: "grpc"` is the named-endpoint key Dispatch references via `matchmaker.GetEndpoint("grpc")`. Naming endpoints (rather than relying on a default) is the Cab convention from the existing `aspire` skill — explicit names survive refactoring.
- **`WithReference(kafka)`** — wires Aspire service discovery so `cab-go`'s environment receives `services__kafka__bootstrap_servers` (or the equivalent `services:` config keys per Aspire 13.2's service-discovery shape). The Go process reads these env-vars to resolve the Kafka broker.
- **`WithEnvironment(...)`** — passes Cab-specific env-vars: the OTLP endpoint URL, the service name for OTEL resource attribution, and the identity authority URL for bearer-token validation.

### `AddExecutable` alternative

```csharp
var matchmaker = builder
    .AddExecutable("cab-go", "go", "../services/cab-go", "run", "./cmd/cab-go")
    .WithHttpEndpoint(port: 50051, targetPort: 50051, name: "grpc")
    .WithReference(kafka)
    .WaitFor(kafka)
    // ... environment variables identical to the AddContainer case
    ;
```

`AddExecutable` arguments: resource name, the executable to run (`go`), the working directory, then arguments. The `go run` path is dev-only — for prebuilt binaries, use the path to the compiled artifact: `AddExecutable("cab-go", "../services/cab-go/bin/cab-go")`.

---

## Service discovery from Dispatch

Dispatch's `Program.cs` resolves the matchmaker endpoint through Aspire's service-discovery integration. The same pattern as service-to-service calls within .NET (per `aspire` § Service Discovery), only the target happens to be the Go service:

```csharp
// In Dispatch's Program.cs
builder.Services.AddGrpcClient<MatchmakerService.MatchmakerServiceClient>(client =>
{
    client.Address = new Uri("https+http://cab-go");
});
```

The `https+http://cab-go` URL is Aspire's service-discovery convention: the scheme list is the preference order, and `cab-go` is the resource name. Aspire's runtime injects the resolved endpoint via the `services:` config keys (Aspire 13.2's service-discovery breaking change updated the config key shape; the URL form on the .NET side is unchanged). The named endpoint `"grpc"` is the default when only one endpoint exists; for multi-endpoint resources, use `https+http://_grpc.cab-go` per Aspire's named-endpoint convention.

---

## Health checking

gRPC has a standard health-checking protocol (`grpc.health.v1.Health`) that Aspire 13.2's dashboard recognizes for non-.NET resources. The Go service registers a health server alongside the matchmaker server:

```go
// internal/server/health.go
import (
    "google.golang.org/grpc/health"
    healthpb "google.golang.org/grpc/health/grpc_health_v1"
)

func registerHealthServer(grpcServer *grpc.Server) *health.Server {
    healthServer := health.NewServer()
    healthpb.RegisterHealthServer(grpcServer, healthServer)
    healthServer.SetServingStatus("", healthpb.HealthCheckResponse_SERVING)
    healthServer.SetServingStatus("critter.cab.dispatch.MatchmakerService",
        healthpb.HealthCheckResponse_SERVING)
    return healthServer
}
```

Liveness rules Cab follows:

- The empty-string service `""` represents the overall process — set to `SERVING` once the gRPC listener is up AND the Kafka consumer has joined its consumer group AND the spatial index has caught up to the high-water-mark.
- Per-service entries (`"critter.cab.dispatch.MatchmakerService"`) reflect that specific service's readiness — usually identical to the overall status, but separable if the service ever exposes multiple gRPC services with different dependency profiles.
- During startup (before Kafka catch-up), set status to `NOT_SERVING`. This makes Aspire dashboard show the resource as "starting" rather than "healthy with empty data," which prevents Dispatch from sending real traffic before the index is populated.

The Aspire AppHost's `WaitFor(matchmaker)` on the Dispatch dependency uses the gRPC health-check (when an HTTP-style health probe isn't configured) — Dispatch starts only after `cab-go` reports `SERVING`.

---

## OpenTelemetry: traces, metrics, logs

The Go service participates in the same Aspire dashboard observability story as the .NET services. The integration is via the OTLP gRPC exporters and three environment variables Aspire injects:

- `OTEL_EXPORTER_OTLP_ENDPOINT` — the Aspire dashboard's OTLP receiver. Aspire injects this automatically when the resource doesn't have it set; Cab sets it explicitly via `WithEnvironment` in the AppHost (above) for documentary clarity.
- `OTEL_SERVICE_NAME` — `"cab-go"`. Tags every trace/metric/log emitted by this process so the dashboard groups them under the right resource.
- `OTEL_RESOURCE_ATTRIBUTES` — Aspire's Conventions inject things like `service.instance.id` automatically. Don't override unless you know what you're displacing.

Wiring inside `cab-go`:

```go
// internal/observability/otel.go
import (
    "go.opentelemetry.io/otel"
    "go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracegrpc"
    "go.opentelemetry.io/otel/sdk/resource"
    sdktrace "go.opentelemetry.io/otel/sdk/trace"
    semconv "go.opentelemetry.io/otel/semconv/v1.27.0"
)

func setupTracing(ctx context.Context) (*sdktrace.TracerProvider, error) {
    exporter, err := otlptracegrpc.New(ctx,
        otlptracegrpc.WithInsecure())  // Aspire dev OTLP is insecure HTTP/2
    if err != nil { return nil, err }

    res := resource.NewWithAttributes(
        semconv.SchemaURL,
        semconv.ServiceName("cab-go"),
    )

    tp := sdktrace.NewTracerProvider(
        sdktrace.WithBatcher(exporter),
        sdktrace.WithResource(res),
    )
    otel.SetTracerProvider(tp)
    return tp, nil
}
```

The gRPC server is wired with the OTel interceptor so spans propagate across the Dispatch → `cab-go` boundary:

```go
import otelgrpc "go.opentelemetry.io/contrib/instrumentation/google.golang.org/grpc/otelgrpc"

grpcServer := grpc.NewServer(
    grpc.StatsHandler(otelgrpc.NewServerHandler()),
)
```

Result: a trace started by a mobile-client gRPC call into Dispatch shows up in the Aspire dashboard with the Dispatch span as the parent and the `cab-go` `FindNearestDrivers` span as a child, with timing correlated. This is the cross-language OTel demonstration the polyglot story exists to deliver.

---

## Authentication: bearer-token boundary

Dispatch calls `cab-go` over gRPC carrying a Cab-issued bearer token in the `authorization` metadata. `cab-go` enforces the same boundary documented in `identity-acl` — validate the JWT against Cab's identity authority, extract the relevant claims, and reject unauthenticated calls.

Cab's identity surface (per `identity-acl`) is OpenIddict-backed. The `cab-go` service:

1. Reads `CAB_AUTH_AUTHORITY` from the environment (set by the AppHost via `WithEnvironment` to the identity service's HTTP endpoint).
2. Fetches the JWKS at `{authority}/.well-known/jwks.json` at startup and refreshes periodically.
3. Registers a gRPC unary-server interceptor that validates the `authorization` header against the JWKS for every inbound call.
4. Rejects calls with no token, expired tokens, or tokens whose signing key isn't in the JWKS, returning `Unauthenticated` (gRPC status code 16).
5. Trusts the .NET-side identity boundary for claim semantics — `cab-go` doesn't re-implement the ACL translation that `identity-acl` documents; it accepts the issued token as proof of authentication and reads the claims it needs (`sub`, `aud`).

The interceptor shape is conventional and out of scope for this skill in detail — `golang.org/x/oauth2/jwt` and `github.com/golang-jwt/jwt/v5` plus a JWKS fetcher are the standard choices. The point is that the boundary EXISTS and is symmetric with the .NET services. A Go service that skipped this boundary would silently accept unauthenticated calls — a real security hole, not just a polyglot inconsistency.

For inner-loop development, Cab's AppHost runs identity in demo mode (per `identity-acl` § Demo Mode). The `cab-go` service treats demo-mode tokens identically — same JWKS, same validation. There's no separate "Go-side dev shortcut."

---

## Common pitfalls

- **Editing `gen/` by hand.** The directory is build output. Edits get overwritten by the next `buf generate`. If a generated file needs adjustment, the answer is either updating the proto, updating `/protos/buf.gen.yaml`, or — rarely — adding a Go-side wrapper in `internal/`. Never edit `gen/` directly.

- **Forgetting to gitignore `gen/`.** The directory must be in `.gitignore` per ADR-009 (generated code is build artifact, not source). A first-time committer who notices the directory and adds it to git is fighting the convention. Reference `cli-grpc-tooling` § `buf generate` if uncertain.

- **Mismatched Go module path.** The `go_package_prefix` in `protos/buf.gen.yaml` must match the `module` declaration in `go.mod`. Cab pins both to `github.com/crittercab/cab-go`. If they drift, `buf generate` produces import paths the consumer can't resolve and you get a confusing compile error chain.

- **Wiring `AddContainer` with `WithReference` to a .NET service expecting Aspire's connection-string-shaped config.** `WithReference(kafka)` works because Aspire's Kafka resource exposes broker URLs that any client can consume. `WithReference(tripsDb)` would set Postgres connection-string env vars that the Go service has no machinery to consume out of the box. If `cab-go` needs a database, treat it like any cross-language consumer — read raw connection-string env vars yourself, don't rely on .NET-shaped helpers.

- **Skipping the gRPC health server.** Without the standard health-checking protocol, Aspire's `WaitFor(matchmaker)` on Dispatch can't tell when `cab-go` is actually ready, and the dashboard shows the resource as perpetually "starting." Register the health server, set status to `SERVING` once the Kafka consumer has caught up, and Aspire's orchestration works as designed.

- **Reporting `SERVING` before the spatial index is populated.** A health check that only reflects "the gRPC listener is up" lets Dispatch send `FindNearestDrivers` queries to an empty index, which returns no candidates — silently wrong. The status transition to `SERVING` must include "the Kafka consumer has caught up to the high-water-mark." Until then, report `NOT_SERVING`.

- **Forgetting OTLP propagation across the language boundary.** A trace started in Dispatch that calls `cab-go` without the OTel gRPC server interceptor will show two unrelated traces in the dashboard — one ending at the Dispatch span, one starting fresh in `cab-go`. The `otelgrpc.NewServerHandler()` server stats handler is the single line that makes the propagation work; it's easy to forget on the Go side because the .NET side gets it for free from Aspire.

- **Using `OTEL_EXPORTER_OTLP_ENDPOINT` without `WithInsecure()`.** Aspire's local-dev OTLP endpoint is insecure HTTP/2 (no TLS) by design. The Go OTel SDK defaults to TLS and fails silently when the endpoint isn't reachable on TLS. Set `otlptracegrpc.WithInsecure()` for local dev. Production deployments (a separate, future concern) would configure TLS appropriately.

- **Treating `cab-go` as bypassing identity.** A Go service implementing the same RPCs without the bearer-token interceptor accepts unauthenticated calls. This is a real security hole even in dev — and worse, a confusing one because the .NET side validates correctly and the bug only manifests if a client deliberately calls the Go service directly. Mirror `identity-acl`'s boundary; don't treat Go as a free pass.

- **Choosing `AddExecutable` as the default because it's faster.** The Docker-build latency on `AddContainer` is real but bounded; the cost of having "the dev's Go toolchain version" diverge from "the Dockerfile's pinned Go version" is a real source of "works on my machine" bugs. Cab's default is `AddContainer`; flip to `AddExecutable` only when actively iterating on `cab-go` itself or when Docker is genuinely unavailable.

- **Letting the Kafka consumer's group ID drift from the Wolverine.Kafka producer's expectations.** If `cab-go` consumes `telemetry.driver-positions`, its consumer group should be `cab-go-matchmaker` (or similar BC-scoped name). Reusing a group ID that another consumer already uses splits partitions across consumers in unintended ways. `wolverine-kafka` § Consumer Group Conventions covers the .NET-side rule; Cab applies the same rule symmetrically here.

- **Assuming Go-side library choices cascade into Cab's contract surface.** They don't. Library selection inside `cab-go` is a Go-internal decision — Confluent vs `segmentio/kafka-go`, R-tree vs geohash grid, `golang-jwt` vs `lestrrat-go/jwx`. Document the choices in `services/cab-go/README.md`; don't surface them in proto contracts or AppHost shape.

---

## See also

**Upstream** — load these first:

- `protobuf-contracts` — the `.proto` file conventions, `option go_package` semantics, the `/protos/` workspace layout. Authoritative for everything proto-side.
- `cli-grpc-tooling` § `buf generate` and `buf.gen.yaml` — the canonical Go code-gen path. The `/protos/buf.gen.yaml` configuration is documented there; this skill consumes its output.
- `aspire` — the AppHost shape, `WithReference`/`WaitFor`/`WithEndpoint` patterns, the service-discovery convention. This skill applies the same patterns to a non-.NET resource.

**Sibling skills:**

- `wolverine-grpc-handlers` — the .NET side of the .NET → Go gRPC call. Dispatch's handler that calls `cab-go` follows the unary-handler pattern from there.
- `wolverine-kafka` — the producer side of the Kafka topic `cab-go` consumes. Topic naming conventions, payload serialization, consumer group rules carry across.
- `identity-acl` — the bearer-token boundary `cab-go` enforces. Mirrors the .NET-side ACL boundary.
- `transport-selection` — confirms gRPC is the right transport for the Dispatch → `cab-go` synchronous read. (`grpc-vs-other-transports` goes one level deeper.)

**Downstream:**

- `observability-tracing` — distributed traces span the .NET → Go boundary; Aspire dashboard surfaces the cross-language traces.
- `observability-metrics` (Phase 4) — metrics from `cab-go` flow through the same OTLP pipeline; `cab-go`-specific metrics (Kafka consumer lag, spatial-index size, query latency) surface in the dashboard alongside the .NET service metrics.
- `testing-advanced` (Phase 4) — integration tests that span the polyglot boundary (Testcontainers running `cab-go` alongside .NET test hosts, asserting cross-process traces or end-to-end matching flows).

**External:**

- ADR-009 in [`docs/decisions/`](../../decisions/) — protobuf contracts as first-class artifacts; the foundation for cross-language consumption.
- [Aspire `AddContainer` documentation](https://aspire.dev/) — the canonical reference for Aspire's container resource type. (Aspire's docs site is moving fast at 13.2; verify the exact API at the time of integration.)
- [Aspire `AddExecutable` documentation](https://aspire.dev/) — the alternative resource type for native binaries.
- [gRPC Health Checking Protocol](https://github.com/grpc/grpc/blob/master/doc/health-checking.md) — the standard Aspire's dashboard recognizes for non-.NET resources.
- [OpenTelemetry Go contrib — `otelgrpc`](https://github.com/open-telemetry/opentelemetry-go-contrib/tree/main/instrumentation/google.golang.org/grpc/otelgrpc) — the gRPC instrumentation that propagates spans across the language boundary.
- [Buf documentation — `buf generate`](https://buf.build/docs/generate/usage/) — the code-generation tool Cab uses to produce Go stubs.
- ai-skills `polyglot-go-service` — generic patterns from JasperFx if/when published; this skill is Cab-specific by design but a generic upstream may complement it.
