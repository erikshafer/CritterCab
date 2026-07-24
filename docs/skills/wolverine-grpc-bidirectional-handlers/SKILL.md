---
name: wolverine-grpc-bidirectional-handlers
description: "Proto-first bidirectional gRPC handlers in CritterCab using WolverineFx.Grpc 6.21+. Covers the bidirectional handler shape (IAsyncEnumerable<TResponse> Handle(TRequest, [EnumeratorCancellation] CancellationToken) — invoked once per inbound request, same as server-streaming), the Validate / [WolverineBefore] / [WolverineAfter] asymmetry (not woven into bidi methods), the AIP-193 exception interceptor that DOES still apply, and Cab's canonical bidirectional use case (SubscribeTripUpdates for driver-rider exchange). PARTIALLY SUPERSEDED: this skill's client-streaming content predates 6.21.0 and describes a hand-written workaround that is now obsolete — client-streaming is auto-generated and documented in wolverine-grpc-handlers. Use when authoring or modifying a service-side gRPC handler for bidirectional RPCs."
cluster: wolverine
tags: [grpc, wolverine, client-streaming, bidirectional, hand-written-stub, push-telemetry, subscribe-trip-updates, async-stream-reader, async-stream-writer, message-bus, enumerator-cancellation]
---

# Wolverine gRPC Bidirectional and Client-Streaming Handlers

> **⚠ PARTIALLY SUPERSEDED as of WolverineFx.Grpc 6.21.0.** This skill was written when client-streaming could not be auto-generated. **It now is** — via the same empty `[WolverineGrpcService]` stub as every other shape — and the auto-generated pattern lives in **`wolverine-grpc-handlers`**, which is the authority for it. Everything here about **bidirectional** streaming remains correct. The client-streaming sections below document a hand-written workaround that is **obsolete**: do not follow them, and do not hand-wire client-streaming. Their structural rewrite is a registered `tidy: skills` DEBT row (see `docs/skills/DEBT.md`); this banner exists so the repo does not actively contradict shipped, passing code (`src/CritterCab.Telemetry/ReportLocations/`).

CritterCab uses WolverineFx.Grpc 6.21+ for all four gRPC streaming modes per ADR-009 and `protobuf-contracts`. `wolverine-grpc-handlers` covers the three shapes with straightforward auto-generated wrappers (unary, server-streaming, client-streaming). This skill closes out the fourth: **bidirectional**, which Wolverine also auto-generates but with subtly different middleware semantics.

The single most useful idea: **a bidirectional gRPC handler in Cab looks exactly like a server-streaming handler** — one `TRequest` parameter, returning `IAsyncEnumerable<TResponse>`, with `[EnumeratorCancellation]` on the cancellation token. The "bidi" part is in the wire shape, not the handler signature. Wolverine's generated wrapper loops over each inbound request from the client and dispatches each one through `bus.StreamAsync<TResponse>`, pumping every yielded response back to the client. This means **the handler is invoked once per inbound request, not once per stream** — a subtle but consequential semantics that the bidirectional integration tests in Wolverine's source pin down explicitly.

Client-streaming *used to be* the opposite story: before 6.21.0 the `[WolverineGrpcService]` discovery path rejected proto-first stubs declaring client-streaming RPCs, throwing `NotSupportedException` at startup, and Cab worked around it with a hand-written concrete stub. **6.21.0 added the emit path, so that rejection no longer happens and the workaround is obsolete.** Client-streaming now uses the same empty stub as every other shape, with the handler taking `IAsyncEnumerable<TRequest>` and returning `Task<TResponse>` — documented in `wolverine-grpc-handlers`. This skill is now about the auto-generated **bidi** shape and its middleware asymmetries; its client-streaming sections are retained only as a legacy record pending rewrite.

This skill assumes the proto-first bootstrap from `wolverine-grpc-handlers` (Kestrel HTTP/2, `AddGrpc()`, `AddWolverineGrpc()`, `MapWolverineGrpcServices()`) and the proto-naming conventions from `protobuf-contracts`. Both apply unchanged to bidirectional and client-streaming surfaces.

---

## When to apply this skill

Use this skill when:

- Authoring a bidirectional gRPC handler (e.g., `SubscribeTripUpdates` on the Trips service).
- Reading legacy code or docs that reference the pre-6.21.0 hand-written client-streaming workaround, and needing to know what replaced it. **Authoring** a client-streaming handler is `wolverine-grpc-handlers`, not this skill.
- Deciding whether a flow that already lives in proto belongs as bidirectional or as client-streaming + server-streaming pair.
- Reviewing a PR that adds a hand-written client-streaming stub — to reject it. Since 6.21.0 the auto-generated path is correct and the workaround is obsolete.
- Wiring middleware (`Validate`, `[WolverineBefore]`, `[WolverineAfter]`) and confirming the asymmetries between the auto-generated and hand-written paths.

Do NOT use this skill for:

- Unary or server-streaming handlers — `wolverine-grpc-handlers` (Phase 3) covers those including the full bootstrap.
- Authoring or reviewing the `.proto` declaration itself (RPC naming conventions, `stream` keyword usage) — `protobuf-contracts` (Phase 1).
- Choosing whether a flow should be gRPC at all, or whether to use client-streaming vs Kafka — `transport-selection` (Phase 1) is the decision framework; `grpc-vs-other-transports` (Phase 4) is the finer-grained companion.
- gRPC as a Wolverine **messaging transport** (`ListenAtGrpcPort`, `ToGrpcEndpoint`) — separate concern, deferred to advanced patterns.
- buf, grpcurl, or Evans usage against streaming endpoints — `cli-grpc-tooling` (Phase 3) covers the CLI surface, including stdin streaming for grpcurl and the Evans REPL's per-item prompts.
- gRPC streaming test harnesses (in-process clients via `WebApplicationFactory`, scenario assembly) — `testing-advanced` (Phase 4).

---

## Mental model

Wolverine's gRPC integration recognizes four canonical RPC shapes via reflection over the proto-generated `*Base` class. The four shapes are classified by the `GrpcMethodKind` enum: `Unary`, `ServerStreaming`, `ClientStreaming`, `BidirectionalStreaming`. **As of 6.21.0 all four are wrapped automatically.**

| Shape | Proto declaration | Wolverine wrapping | Cab path |
|---|---|---|---|
| Unary | `rpc X(Req) returns (Resp);` | Auto-generated (`bus.InvokeAsync<Resp>`) | `wolverine-grpc-handlers` |
| Server-streaming | `rpc X(Req) returns (stream Resp);` | Auto-generated (`bus.StreamAsync<Resp>`) | `wolverine-grpc-handlers` |
| **Bidirectional** | `rpc X(stream Req) returns (stream Resp);` | **Auto-generated**, with middleware caveats | This skill |
| Client-streaming | `rpc X(stream Req) returns (Resp);` | Auto-generated (`bus.StreamAsync<TReq, TResp>`) — **since 6.21.0** | `wolverine-grpc-handlers` |

Both bidirectional and client-streaming take an `IAsyncStreamReader<TRequest>` on the wire. The difference is what the server returns: bidirectional returns an `IServerStreamWriter<TResponse>` (a stream of responses), client-streaming returns a single `Task<TResponse>` (one summary response). That single-response shape is why client-streaming needed its own emit path rather than riding the item-streaming `IMessageBus.StreamAsync<T>` — 6.21.0 added a `StreamAsync<TRequest, TResponse>` overload that folds a whole inbound stream into one reply. Before that overload existed, the shape was rejected at startup rather than half-supported.

**Both shapes share one consequence that has not changed:** neither weaves `Validate` / `[WolverineBefore]` / `[WolverineAfter]`, because a before-frame needs a concrete request instance at method entry and a stream cannot supply one. Validation and cross-cutting concerns are handler-side for both.

The discovery rules that drive each path are in `WolverineGrpcExtensions.IsCodeFirstGrpcServiceType` (matches name suffix `GrpcService` or the `[WolverineGrpcService]` attribute), `GrpcGraph.IsProtoFirstStub` (abstract + attribute + proto base), and `GrpcGraph.AssertNoConcreteProtoStubs` (rejects concrete `[WolverineGrpcService]` classes that derive from a proto base). The hand-written client-streaming workaround threads through these rules deliberately: a concrete class deriving from a proto base, **not marked `[WolverineGrpcService]`**, name ending in `GrpcService` so `MapWolverineGrpcServices()` discovers it for direct mapping.

---

## Bidirectional handlers (auto-generated)

Cab's canonical bidirectional case is `SubscribeTripUpdates` on the Trips service per `protobuf-contracts`: a rider client opens a duplex stream and exchanges trip-update events with the server during an active trip. Bidirectional is a powerful shape but rare in practice — most "stream both ways" flows in Cab decompose into a server-streaming RPC for the server-to-client direction and unary RPCs for the client-to-server side. Reach for bidirectional when the request and response streams are genuinely interleaved.

### Proto declaration

```protobuf
service Trips {
  rpc SubscribeTripUpdates(stream TripUpdateRequest) returns (stream TripUpdate);
}
```

### Stub and handler

The stub is identical in shape to the unary/server-streaming case from `wolverine-grpc-handlers` — abstract, partial, marked `[WolverineGrpcService]`, deriving from the proto-generated base:

```csharp
// src/CritterCab.Trips/TripsGrpcService.cs
[WolverineGrpcService]
public abstract partial class TripsGrpcService : Trips.TripsBase;
```

The handler takes a single `TripUpdateRequest` and returns an `IAsyncEnumerable<TripUpdate>` — exactly the same signature as a server-streaming handler:

```csharp
// src/CritterCab.Trips/SubscribeTripUpdatesFeature/SubscribeTripUpdatesHandler.cs
using System.Runtime.CompilerServices;
using CritterCab.Trips.V1;

namespace CritterCab.Trips.SubscribeTripUpdatesFeature;

public static class SubscribeTripUpdatesHandler
{
    public static async IAsyncEnumerable<TripUpdate> Handle(
        TripUpdateRequest request,
        ITripUpdateStream updates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tripId = Guid.Parse(request.TripId);

        await foreach (var update in updates.SubscribeAsync(tripId, request.Filter, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }
    }
}
```

`[EnumeratorCancellation]` is required, same as for server-streaming. Without it the gRPC framework's cancellation token cannot flow into the handler's enumeration and the handler keeps running after the client disconnects.

### How the wrapper actually drives the handler

The generated wrapper for a bidirectional method emits this shape (verified in `Wolverine.Grpc/GrpcServiceChain.cs` § `ForwardBidiStreamToMessageBusFrame`):

```csharp
// Generated — never check this in
public override async Task SubscribeTripUpdates(
    IAsyncStreamReader<TripUpdateRequest> requestStream,
    IServerStreamWriter<TripUpdate> responseStream,
    ServerCallContext context)
{
    while (await requestStream.MoveNext(context.CancellationToken))
    {
        var request = requestStream.Current;
        await foreach (var item in _bus.StreamAsync<TripUpdate>(request, context.CancellationToken))
        {
            await responseStream.WriteAsync(item, context.CancellationToken);
        }
    }
}
```

The crucial property: **the handler is invoked once per inbound request from the client.** Each `TripUpdateRequest` arriving on the client's request stream produces a fresh handler invocation; that invocation's `IAsyncEnumerable<TripUpdate>` is fully drained to the client before the wrapper reads the next inbound request. Wolverine's bidirectional integration test pins this down: an inbound sequence of `[("a", 2), ("b", 1), ("c", 3)]` produces `["a", "a", "b", "c", "c", "c"]` on the response stream — sequentially, in order.

This semantics fits "request-reply within an open duplex" naturally (each request triggers a burst of correlated responses). It does NOT fit "send me everything that's happened, ever, while I send you commands" (where one inbound request might want to interleave with responses driven by external events) — for that shape, decompose into a server-streaming RPC for the events plus separate unary RPCs for the commands. `SubscribeTripUpdates` fits the bidi shape because each `TripUpdateRequest` is a focused subscription change (e.g., "include driver-location updates" toggled mid-stream) and the responses are correlated to that change.

### Middleware caveat: Validate / [WolverineBefore] / [WolverineAfter] are NOT woven

`GrpcServiceChain.AssembleTypes` weaves `Validate`, `[WolverineBefore]`, and `[WolverineAfter]` middleware **only for unary, server-streaming, and bidi methods that have a single `TRequest` in scope before dispatch.** Bidi methods are explicitly excluded from middleware weaving — the source check is `if (rpc.Kind != GrpcMethodKind.BidirectionalStreaming)` guarding the before/after frame emission. The reason: the bidi method begins with an `IAsyncStreamReader<T>`, not a single message instance, so a `Validate(TripUpdateRequest)` method would have nothing to bind to before the loop starts.

Practical consequences for Cab bidi handlers:

- **Per-request shape validation** must live inside the handler body, not in a `Validate` short-circuit. Throw `RpcException` with the appropriate `Status` for invalid requests; the AIP-193 exception interceptor handles status-code mapping.
- **Cross-cutting middleware** that needs a per-request invocation (correlation IDs, audit logging) must run inside the handler too, or be expressed at a coarser granularity (e.g., per-call logging via the gRPC server interceptor pipeline rather than Wolverine's middleware).
- **Other RPC methods on the same stub** still get full middleware weaving — only bidi methods are excluded. A `TripsGrpcService` stub with both `StartTrip` (unary) and `SubscribeTripUpdates` (bidi) gets `Validate(StartTripRequest)` woven on the unary side and no middleware on the bidi side.

The AIP-193 exception interceptor (`WolverineGrpcExceptionInterceptor`) is registered at the `GrpcServiceOptions` level by `AddWolverineGrpc()`, so it applies to every RPC including bidi. Throwing `KeyNotFoundException` from a bidi handler still maps to `StatusCode.NotFound` per the default table from `wolverine-grpc-handlers`.

---

## Client-streaming handlers (hand-written workaround) — ⚠ OBSOLETE

> **⚠ SUPERSEDED as of WolverineFx.Grpc 6.21.0 — do not follow this section.**
>
> Client-streaming is auto-generated. The empty `[WolverineGrpcService]` stub works for it exactly as for unary and server-streaming, and the handler shape is
> `Task<TResponse> Handle(IAsyncEnumerable<TRequest>, …)`. See **`wolverine-grpc-handlers` § Client-streaming handlers**, which is the authority.
>
> Everything below documents the pre-6.21.0 hand-written workaround and is retained only as a legacy record. Its removal and this skill's retitling to bidirectional-only are a registered `tidy: skills` DEBT row. CritterCab ships auto-generated client-streaming today (`src/CritterCab.Telemetry/ReportLocations/`), so following this section would contradict working code.

Cab's canonical client-streaming case is `PushTelemetry` on the Telemetry service per `transport-selection`: a driver's mobile client streams GPS pings continuously into the Telemetry service, which acknowledges with a single `PushTelemetryResponse` summary when the client closes the stream. Client-streaming is the right shape for this — high-frequency, low-overhead inbound items where the response is incidental. The challenge is that Wolverine 5.32 doesn't generate the wrapper.

### Why Wolverine fails fast at startup

`GrpcServiceChain`'s constructor calls `AssertNoUnsupportedStreamingKinds` after classifying every RPC method on the proto base. Any method whose shape is `Task<TResponse> Method(IAsyncStreamReader<TRequest>, ServerCallContext)` is classified `GrpcMethodKind.ClientStreaming` and triggers a `NotSupportedException` with this verbatim guidance:

```
Proto-first gRPC stub <stub> declares RPC method(s) whose shape Wolverine cannot
yet code-generate. Supported today: unary, server-streaming, and bidirectional-
streaming. Client-streaming (stream TRequest → TResponse) has no adapter path yet.

Unsupported method(s):
  - PushTelemetry (ClientStreaming)

Workaround: move the affected RPC(s) into a separate service whose stub is NOT
marked [WolverineGrpcService], and implement those methods by hand
(calling IMessageBus directly).
```

The throw happens during Wolverine startup, before any RPC traffic. A service whose proto declares any client-streaming method on a `[WolverineGrpcService]`-marked stub will not start.

### The two hand-written patterns

Both patterns produce a concrete class that:

- Derives from a proto-generated `*Base` class (so the gRPC framework wires it as a service).
- Is **not** marked with `[WolverineGrpcService]` (so `AssertNoConcreteProtoStubs` doesn't reject it as a misuse).
- Has a name ending in `GrpcService` (so `IsCodeFirstGrpcServiceType` catches it for `MapWolverineGrpcServices()` to map directly via `MapGrpcService<T>(...)`).
- Does NOT implement a `[ServiceContract]` interface (so `IsHandWrittenServiceClass` rejects it — Cab is proto-first, not code-first via protobuf-net.Grpc).

The two patterns differ in how aggressively they isolate the client-streaming concern.

#### Pattern A — separate proto service (recommended for Cab)

The fail-fast error message recommends this pattern explicitly: split the proto service so the client-streaming RPC lives in its own `service` declaration. The unary/server-streaming/bidi RPCs stay on the original `[WolverineGrpcService]`-marked stub and benefit from Wolverine's middleware policy. The client-streaming RPC moves to a hand-written stub with no Wolverine codegen.

```protobuf
// /protos/telemetry/v1/telemetry.proto
service TelemetryQuery {
  rpc GetLatestPosition(GetLatestPositionRequest) returns (LocationPing);
  rpc StreamHistoricalPings(StreamHistoricalPingsRequest) returns (stream LocationPing);
}

service TelemetryIngest {
  rpc PushTelemetry(stream LocationPing) returns (PushTelemetryResponse);
}
```

```csharp
// src/CritterCab.Telemetry/TelemetryQueryGrpcService.cs
[WolverineGrpcService]
public abstract partial class TelemetryQueryGrpcService : TelemetryQuery.TelemetryQueryBase;

// src/CritterCab.Telemetry/TelemetryIngestGrpcService.cs
public class TelemetryIngestGrpcService(IMessageBus bus) : TelemetryIngest.TelemetryIngestBase
{
    public override async Task<PushTelemetryResponse> PushTelemetry(
        IAsyncStreamReader<LocationPing> requestStream,
        ServerCallContext context)
    {
        var pings = new List<LocationPing>(capacity: 256);

        while (await requestStream.MoveNext(context.CancellationToken))
        {
            pings.Add(requestStream.Current);
        }

        var ack = await bus.InvokeAsync<PushTelemetryResponse>(
            new PushTelemetryBatch(pings.AsReadOnly()),
            context.CancellationToken);

        return ack;
    }
}
```

The Wolverine handler that processes the batch lives in the same vertical slice and is a vanilla Wolverine handler:

```csharp
// src/CritterCab.Telemetry/PushTelemetryFeature/PushTelemetryHandler.cs
public static class PushTelemetryHandler
{
    public static async Task<PushTelemetryResponse> Handle(
        PushTelemetryBatch batch,
        ITelemetryIngestService ingest,
        CancellationToken cancellationToken)
    {
        var summary = await ingest.IngestAsync(batch.Pings, cancellationToken);
        return new PushTelemetryResponse
        {
            AcceptedCount = summary.AcceptedCount,
            RejectedCount = summary.RejectedCount,
        };
    }
}
```

Why this pattern is the Cab default:

- **Most RPCs keep full Wolverine middleware support.** The `TelemetryQuery` service goes through the auto-generated chain and gets `Validate`, `[WolverineBefore]`, `[WolverineAfter]`, and the middleware policy registered via `AddWolverineGrpc(opts => opts.AddMiddleware<T>())`.
- **The hand-written surface is minimized to one concrete method.** Easier to review, easier to test, smaller blast radius if Wolverine adds client-streaming support later (the migration is "split the proto service back together and convert the concrete stub to an abstract one with the attribute").
- **The client-streaming concern is named in the proto.** Buf governance (per `cli-grpc-tooling`) treats `TelemetryIngest` as its own service for breaking-change detection — adding RPCs to it doesn't pollute the `TelemetryQuery` surface.

#### Pattern B — fully hand-written single service

When splitting the proto service is awkward (e.g., the client-streaming RPC is logically inseparable from sibling RPCs), the entire stub goes hand-written:

```csharp
// src/CritterCab.Telemetry/TelemetryGrpcService.cs
public class TelemetryGrpcService(IMessageBus bus) : Telemetry.TelemetryBase
{
    public override Task<LocationPing> GetLatestPosition(
        GetLatestPositionRequest request,
        ServerCallContext context)
        => bus.InvokeAsync<LocationPing>(request, context.CancellationToken);

    public override async Task<PushTelemetryResponse> PushTelemetry(
        IAsyncStreamReader<LocationPing> requestStream,
        ServerCallContext context)
    {
        // ... as in Pattern A
    }
}
```

Every method must be implemented by hand — including the unary and server-streaming ones that would have been auto-generated. No `Validate` short-circuits, no `[WolverineBefore]` / `[WolverineAfter]` middleware, no per-chain registration via `opts.AddMiddleware<T>()`. The cost is real; reach for Pattern A unless the proto split would create more friction than the lost middleware.

### What still applies on the hand-written path

- **`WolverineGrpcExceptionInterceptor`** — registered at the `GrpcServiceOptions` level by `AddWolverineGrpc()`, so it intercepts every RPC including hand-written ones. AIP-193 mapping (`KeyNotFoundException` → `NotFound`, `OperationCanceledException` → `Cancelled`, etc.) and `opts.MapException<T>()` overrides both work.
- **Aspire service discovery and TLS** — the hand-written stub is just a concrete gRPC service; Kestrel's HTTP/2 binding, Aspire's dev cert, and `MapGrpcService<T>` registration all work identically to the auto-generated path.
- **Dependency injection** — the constructor of the hand-written stub is resolved through Cab's DI container per `ActivatorUtilities` semantics. `IMessageBus` is registered by Wolverine; inject it directly. The `WolverineGrpcServiceBase` helper class is for **code-first** services (with `[ServiceContract]` interfaces) and does not apply to proto-first hand-written stubs — its constructor convention is incompatible with deriving from a proto-generated `*Base` class.

### What does NOT apply on the hand-written path

- **`Validate(TRequest)` short-circuit.** Hand-written stubs are direct-mapped via `MapGrpcService<T>`, bypassing `GrpcServiceChain` entirely. No chain, no middleware policy, no validation frame emission.
- **`[WolverineBefore]` / `[WolverineAfter]` on the stub class.** Same reason — the policy that scans for these is part of the chain pipeline, which never runs for direct-mapped stubs.
- **`opts.AddMiddleware<T>()` on `WolverineGrpcOptions`.** Applies only to discovered chains.
- **`wolverine-diagnostics codegen-preview --grpc`** for the hand-written class — there's no generated code to preview. The CLI's `describe-routing` does still show how `IMessageBus.InvokeAsync<T>` calls inside the stub route to handlers.

Validation, logging, correlation, and metrics for the hand-written method need to live inside the method body (or be expressed at the gRPC interceptor pipeline level via `services.Configure<GrpcServiceOptions>(opts => opts.Interceptors.Add<T>())`).

---

## Backpressure

For both bidi and client-streaming, backpressure is handled by HTTP/2 flow control beneath the application code — `IServerStreamWriter<T>.WriteAsync` and `IAsyncStreamReader<T>.MoveNext` both `await` against the underlying transport, so the gRPC framework naturally suspends production when the client is slow to consume (bidi) and naturally suspends the server-side read loop when the client hasn't sent the next message yet (client-streaming).

The handler's responsibility is to:

- **Honor cancellation.** Pass `CancellationToken` to async operations, or call `cancellationToken.ThrowIfCancellationRequested()` between produced items. For hand-written client-streaming, use `context.CancellationToken` from the `ServerCallContext`.
- **Avoid unbounded buffering.** A hand-written client-streaming handler that accumulates the entire request stream into a list before processing (as in `PushTelemetry` above) is fine for bounded ingest sessions, but a handler that holds onto requests indefinitely creates memory pressure. For very long client-streaming sessions, dispatch each item incrementally via `bus.PublishAsync` rather than collecting and dispatching once at completion.
- **Yield often in bidi handlers.** The handler is invoked per inbound request; each invocation should not pre-buffer more than necessary. Yield items as they become available so HTTP/2 flow control has work to do.

For `PushTelemetry` specifically, the GPS-ping volume per session is bounded by trip duration and per-driver ping rate — accumulating a few thousand pings per trip into a list is fine. For an indefinite-duration ingest (e.g., a hypothetical "stream telemetry forever from this device"), `bus.PublishAsync` per item with periodic acks is the pattern to reach for.

---

## Tooling

For bidirectional handlers, `wolverine-diagnostics codegen-preview --grpc` works exactly as in `wolverine-grpc-handlers` — the generated wrapper for the bidi method is visible as part of the proto service's generated subclass. This is the fastest way to confirm Wolverine has correctly classified the method as `BidirectionalStreaming` and emitted the nested loop.

```bash
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics codegen-preview --grpc Trips
```

For hand-written client-streaming stubs, there's no generated code to preview. `wolverine-diagnostics describe-routing` against the inner message types (`PushTelemetryBatch` in Pattern A) still confirms the bus dispatch lands on the expected handler.

For client-side smoke testing of both shapes, `cli-grpc-tooling` covers grpcurl's stdin streaming for client-streaming RPCs and Evans's REPL prompting for bidirectional sessions.

---

## Common pitfalls

- **Treating the bidi handler as "called once per stream."** It's called once per inbound request from the client. The verified test in Wolverine's source — inbound `[("a", 2), ("b", 1), ("c", 3)]` produces `["a", "a", "b", "c", "c", "c"]` — is the canonical demonstration. If a flow needs "open a session, then exchange free-form messages," that's not a fit for Wolverine's bidi shape; decompose into server-streaming + unary instead.

- **Marking a proto-first stub `[WolverineGrpcService]` when the proto has a client-streaming RPC.** Service startup throws `NotSupportedException` from `AssertNoUnsupportedStreamingKinds` — no graceful skip, no log warning, the host fails to start. The error names the offending method and the workaround. Treat the throw as the design feedback it is: split the proto service (Pattern A) or hand-write the entire stub (Pattern B).

- **Trying to put `Validate` or `[WolverineBefore]` on a bidi method.** Not woven — `GrpcServiceChain` explicitly skips middleware emission for bidi methods. Per-request validation lives in the handler body. Other RPC kinds on the same stub still get full middleware; the asymmetry is per-method, not per-stub.

- **Trying to put `Validate` or `[WolverineBefore]` on a hand-written client-streaming stub.** Direct-mapped stubs bypass `GrpcServiceChain` entirely. No chain → no middleware policy. Validation and middleware live inside the stub method or at the gRPC interceptor level via `services.Configure<GrpcServiceOptions>(opts => opts.Interceptors.Add<T>())`.

- **Marking the hand-written client-streaming stub `[WolverineGrpcService]`.** Triggers `AssertNoConcreteProtoStubs` (concrete class + attribute + proto base = misuse) and the host fails to start with an `InvalidOperationException`. The hand-written workaround relies on the absence of the attribute — discovery works via the `GrpcService` name suffix.

- **Implementing `[ServiceContract]` interfaces on a hand-written proto-first stub.** That's the **code-first** path (`HandWrittenGrpcServiceChain`). For Cab's proto-first stubs, don't implement a `[ServiceContract]` interface — there isn't one (proto-generated `*Base` classes don't define them) and adding one diverts discovery into the wrong path.

- **Deriving the hand-written stub from `WolverineGrpcServiceBase`.** That helper is for code-first services. It supplies `IMessageBus Bus` via constructor injection and is incompatible with deriving from a proto-generated `*Base` class. For proto-first hand-written stubs, inject `IMessageBus` via the stub's own constructor (as in the Pattern A and Pattern B examples).

- **Returning `Task<IAsyncEnumerable<T>>` from a bidi handler instead of `IAsyncEnumerable<T>`.** The classifier in `GrpcServiceChain.ClassifyRpcMethod` matches the proto-side method shape; the handler-side shape is `IAsyncEnumerable<T>` with `yield return`. Wrapping in `Task<>` defeats the dispatch path the same way it does for server-streaming.

- **Missing `[EnumeratorCancellation]` on the bidi handler's `CancellationToken`.** Same trap as server-streaming. The compiler warns; Wolverine cannot flow `ServerCallContext.CancellationToken` into the handler's enumeration. Client disconnects don't terminate the handler.

- **Buffering all inbound requests in a hand-written client-streaming handler when the session is unbounded.** The Pattern A example accumulates into a list because a `PushTelemetry` session is bounded by trip duration. For sessions without a bounded length, dispatch per item with `bus.PublishAsync` or implement a periodic-batch flush rather than accumulate forever.

- **Forgetting to drain `IAsyncStreamReader<T>` in the hand-written client-streaming method.** The gRPC framework expects the server to read until `MoveNext(CancellationToken)` returns `false`. Returning the response before the request stream is drained closes the call from the server side mid-stream — clients see a `Cancelled` or `Internal` status and the trailing pings are lost without acknowledgement.

- **Plaintext HTTP/2 in production-like environments for streaming RPCs.** Same Cab default as `wolverine-grpc-handlers` — Aspire dev cert in dev, real cert in production. Never the `Http2UnencryptedSupport` AppContext switch outside of the Wolverine sample code.

---

## See also

**Prerequisites** — Cab-internal skills to load first if unfamiliar:

- `wolverine-grpc-handlers` — the unary and server-streaming handlers, the `[WolverineGrpcService]` discovery path, the bootstrap (`AddGrpc`, `AddWolverineGrpc`, `MapWolverineGrpcServices`), and the AIP-193 exception interceptor that this skill builds on directly.
- `protobuf-contracts` — the proto-side declaration conventions for `stream` keyword usage and the `Push*` / `Subscribe*` method-naming patterns.
- `transport-selection` — the decision framework that routes `PushTelemetry` to client-streaming over Kafka and routes real-time driver-rider exchange to bidirectional gRPC.
- `wolverine-handlers` — the underlying handler shape (DI conventions, sync vs async semantics) that gRPC-routed messages share with HTTP and messaging handlers.

**Sibling skills:**

- `wolverine-http-handlers` — HTTP endpoint handlers; same handler bodies frequently sit behind both gRPC and HTTP for unary requests.
- `wolverine-messaging-handlers` — messaging handlers; the inner handler that the hand-written `PushTelemetry` stub dispatches to (`PushTelemetryHandler.Handle(PushTelemetryBatch)`) is a vanilla messaging handler.
- `vertical-slice-organization` — where the handler files live in the service's source tree (the gRPC-ness of the request type doesn't change the slice).
- `cli-jasperfx` — `wolverine-diagnostics codegen-preview --grpc` and `describe-routing` for inspecting the auto-generated bidi wrapper and verifying inner-handler routing.

**Downstream:**

- `cli-grpc-tooling` (Phase 3) — grpcurl's stdin-streaming pattern for client-streaming RPCs and Evans's REPL prompting for bidirectional sessions.
- `testing-integration` — fixture pattern for in-process gRPC tests; the bidirectional integration test in Wolverine's source (`grpc_bidi_streaming_tests.cs`) demonstrates the shape Cab follows.
- `testing-advanced` (Phase 4) — gRPC streaming test harnesses (in-process clients via `WebApplicationFactory`, scenario assembly for bidi and client-streaming flows).
- `observability-tracing` (Phase 3) — the OpenTelemetry spans the hand-written stub emits flow into the same trace pipeline as auto-generated chains.
- `grpc-vs-other-transports` (Phase 4) — the finer-grained decision aid for ambiguous gRPC-vs-other cases, including when client-streaming vs Kafka.

**External:**

- [Wolverine gRPC documentation](https://wolverinefx.net/guide/grpc/) — the upstream documentation for the auto-generated paths.
- [gRPC streaming RPC concepts](https://grpc.io/docs/what-is-grpc/core-concepts/#rpc-life-cycle) — the canonical reference for the four streaming modes' wire semantics.
- [Google AIP-193 — Errors](https://google.aip.dev/193) — the canonical exception → status code mapping the interceptor follows even on hand-written paths.
- [`IAsyncStreamReader<T>` API reference](https://grpc.github.io/grpc/csharp/api/Grpc.Core.IAsyncStreamReader-1.html) — the inbound-request iterator the hand-written client-streaming method drains.
