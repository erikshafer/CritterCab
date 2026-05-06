---
name: wolverine-grpc-handlers
description: "Proto-first gRPC service handlers in CritterCab using Wolverine 5.32+. Covers the [WolverineGrpcService] stub pattern, unary handlers (Task<TResponse> bus.InvokeAsync<T>), server-streaming handlers (IAsyncEnumerable<TResponse> bus.StreamAsync<T>), the Validate / [WolverineBefore] / [WolverineAfter] middleware surface, AIP-193 exception → StatusCode mapping (default table plus opts.MapException<T>() overrides), opt-in google.rpc.Status rich error details, the Kestrel HTTP/2 + AddGrpc + AddWolverineGrpc + MapWolverineGrpcServices bootstrap, client-side Grpc.Net.Client typed clients with Aspire service discovery, and the wolverine-diagnostics codegen-preview --grpc surface for inspecting generated wrappers. Use when authoring or modifying a service-side gRPC handler for unary or server-streaming RPCs. Client-streaming and bidirectional patterns live in wolverine-grpc-bidirectional-handlers (Phase 4)."
cluster: wolverine
tags: [grpc, wolverine, proto-first, streaming, server-streaming, unary, aip-193, exception-mapping, validate, wolverine-grpc-service, message-bus, http2]
---

# Wolverine gRPC Handlers

CritterCab implements gRPC services proto-first per ADR-009 and `protobuf-contracts`: the `.proto` file is the contract, protoc generates the service base classes, and the C# handlers live as ordinary Wolverine handlers behind a thin Wolverine-generated wrapper.

The single most useful idea in this skill: **a Cab gRPC handler is just a Wolverine handler.** When a `RequestRide` RPC arrives at the Dispatch service, Wolverine's generated wrapper calls `bus.InvokeAsync<RequestRideResponse>(request, ct)`. That dispatches to a regular `Handle(RequestRideRequest)` method living anywhere in the service's vertical slice. The handler doesn't know it's behind gRPC, doesn't import anything from `Grpc.*`, and the same handler body would work behind an HTTP endpoint or a messaging transport without changes.

The wiring is what's gRPC-specific: a `[WolverineGrpcService]`-marked abstract stub that derives from the proto-generated base class, plus the bootstrap that turns it on. Wolverine code-generates the concrete subclass at startup, overrides every RPC method, and forwards each call to the message bus. Tooling (`wolverine-diagnostics codegen-preview --grpc`) shows exactly what was emitted.

This skill covers **unary** and **server-streaming** RPCs — the two shapes that account for nearly every Cab gRPC interaction (`RequestRide`, `StreamDriverOffers`, `WatchTripStatus`, `CompleteTrip`, `RequestQuote`). Client-streaming (`PushTelemetry` per `transport-selection`) and bidirectional patterns are deferred to `wolverine-grpc-bidirectional-handlers` (Phase 4) — and Wolverine 5.32 doesn't yet auto-generate client-streaming wrappers, so that skill also covers the hand-written workaround.

---

## When to apply this skill

Use this skill when:

- Authoring a unary or server-streaming gRPC handler for a Cab service.
- Setting up the `[WolverineGrpcService]` stub class for a service's first gRPC surface.
- Bootstrapping a service for gRPC (Kestrel HTTP/2, `AddGrpc`, `AddWolverineGrpc`, `MapWolverineGrpcServices`).
- Mapping a domain exception to a specific gRPC `StatusCode`, or opting in to rich `google.rpc.Status` error details.
- Wiring a Cab service-to-service gRPC client call (e.g., Trips calling Pricing).
- Adding `Validate`, `[WolverineBefore]`, or `[WolverineAfter]` middleware to the stub.
- Diagnosing a generated wrapper via `wolverine-diagnostics codegen-preview --grpc`.

Do NOT use this skill for:

- Authoring or reviewing the `.proto` file itself — `protobuf-contracts`.
- Choosing whether the flow should be gRPC at all — `transport-selection`.
- Client-streaming (`stream TRequest → TResponse`) or bidirectional handlers — `wolverine-grpc-bidirectional-handlers` (Phase 4).
- gRPC as a Wolverine **messaging transport** (`ListenAtGrpcPort`, `ToGrpcEndpoint`) for routed cross-service messages — that's a separate concept, deferred to advanced patterns. The "gRPC transport" wires the bus over gRPC; this skill wires gRPC service endpoints to bus handlers.
- buf, grpcurl, Evans CLI usage — `cli-grpc-tooling` (Phase 3).
- Integration testing gRPC handlers — `testing-integration` covers the fixture; gRPC-specific scenario assembly lives in `testing-advanced` (Phase 4).

---

## Mental model

The proto-first pipeline has four moving parts and exactly one place where the contract crosses into Cab's code:

```
.proto file
    │
    ▼  (protoc generates at build time)
TripsBase abstract class — the proto-generated server base
    │
    ▼  (Cab authors a stub deriving from it)
public abstract partial class TripsGrpcService : Trips.TripsBase
[WolverineGrpcService]
    │
    ▼  (Wolverine generates the concrete wrapper at startup)
TripsGrpcHandler : TripsGrpcService — overrides every RPC method
                                      forwards to bus.InvokeAsync<T> or bus.StreamAsync<T>
    │
    ▼  (Wolverine routes to the actual handler in the vertical slice)
public static class StartTripHandler
{
    public static StartTripResponse Handle(StartTripRequest request, ...)
        => ...
}
```

The user-authored stub is empty — it just attaches `[WolverineGrpcService]` to opt the proto-generated base class into Wolverine's code-generation pipeline. Everything else is generated. Per `cli-jasperfx`, the generated wrapper is named `{ProtoServiceName}GrpcHandler` and is visible via `wolverine-diagnostics codegen-preview --grpc <ServiceName>`.

The handlers themselves are not gRPC-aware. They take the request type, return the response type (or `IAsyncEnumerable<TResponse>` for streaming), and live in the same vertical slice as any other Wolverine handler per `vertical-slice-organization`. This means the same handler, tested as a pure unit test per `testing-fundamentals`, runs unchanged under integration tests against the real gRPC pipeline per `testing-integration`.

### Why this shape and not code-first

Two alternative paths exist in Wolverine.Grpc:

- **Code-first** with `[ServiceContract]` interfaces (via `protobuf-net.Grpc`) — Wolverine generates a concrete implementation of the interface that forwards to the bus. ADR-009 rejects this path: the contract is the `.proto` file, not the C# interface.
- **Hand-written** services not marked `[WolverineGrpcService]` — the user implements every RPC method directly, calling `IMessageBus` if they want. Used as the workaround for client-streaming RPCs that Wolverine 5.32 doesn't yet auto-generate (covered in `wolverine-grpc-bidirectional-handlers`).

Cab uses proto-first auto-generation everywhere it can. Hand-written stubs only appear where Wolverine's code-gen doesn't reach yet (today: client-streaming).

---

## Bootstrap

A Cab service that exposes gRPC adds four things to its `Program.cs` per `service-bootstrap`:

```csharp
using JasperFx;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wolverine;
using Wolverine.Grpc;

var builder = WebApplication.CreateBuilder(args);

// 1. Kestrel must speak HTTP/2 on the gRPC endpoint. Cab uses HTTPS in dev
//    (Aspire generates the dev cert), HTTP/2-over-TLS in production.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(7233, listen =>
    {
        listen.Protocols = HttpProtocols.Http2;
        listen.UseHttps();
    });
});

// 2. Wolverine itself
builder.Host.UseWolverine(opts =>
{
    opts.ApplicationAssembly = typeof(Program).Assembly;
    // ... per service-bootstrap
});

// 3. ASP.NET Core gRPC host. Use AddGrpc() for proto-first (Cab's path).
//    Code-first scenarios use AddCodeFirstGrpc() — Cab does not.
builder.Services.AddGrpc();

// 4. Wolverine-gRPC integration: registers the exception interceptor,
//    the GrpcGraph that drives discovery, and per-call DI plumbing.
builder.Services.AddWolverineGrpc(opts =>
{
    // Optional: per-exception StatusCode overrides
    opts.MapException<TripNotFoundException>(StatusCode.NotFound);
    opts.MapException<DispatchUnavailableException>(StatusCode.Unavailable);

    // Optional: gRPC-scoped middleware applied to all chains
    opts.AddMiddleware<TripCorrelationMiddleware>();
});

var app = builder.Build();
app.UseRouting();

// 5. Discovers every [WolverineGrpcService] abstract stub in the application
//    assembly, code-generates the concrete wrapper for each, and maps it.
app.MapWolverineGrpcServices();

return await app.RunJasperFxCommands(args);

public partial class Program;
```

### Why each line

- **`AddGrpc()`** — the stock ASP.NET Core gRPC host. Required for proto-first because the proto-generated `*Base` classes are ASP.NET Core gRPC service types. `AddCodeFirstGrpc()` from the protobuf-net.Grpc package is the alternative for code-first scenarios; Cab does not use it.
- **`AddWolverineGrpc()`** — registers the `WolverineGrpcExceptionInterceptor` (which applies the AIP-193 exception → status code mapping), the `GrpcGraph` that drives stub discovery, and integrates Wolverine's chain pipeline into the gRPC call path. Idempotent: repeat calls re-run the configuration callback against the same options instance, so additive registrations accumulate without duplication.
- **`MapWolverineGrpcServices()`** — runs after `UseRouting()`. Discovers every type in the application assemblies whose name ends with `GrpcService` or that carries `[WolverineGrpcService]`. Abstract stubs trigger code-generation; concrete classes are mapped directly.
- **HTTPS via Kestrel** — gRPC requires HTTP/2. Browsers and most production gRPC clients require TLS for HTTP/2. The Aspire dev cert handles local development; production receives a real cert from the deployment platform per Cab's eventual deployment ADR. The `GreeterProtoFirstGrpc` sample in Wolverine's repo runs HTTP/2 unencrypted on `5003` for simplicity — Cab does not follow that pattern.

### Why no separate gRPC port from the API

A Cab service with both Wolverine HTTP endpoints and gRPC services typically listens on a single Kestrel endpoint that supports both protocols. ASP.NET Core's HTTP/1.1+HTTP/2 negotiation lets the gRPC client and the HTTP client share the same port. When the service is gRPC-only (e.g., a future internal-only Pricing service), bind a single HTTP/2-only listener as in the example above.

---

## The `[WolverineGrpcService]` stub

For each gRPC service the Cab service hosts, write one stub class:

```csharp
// src/CritterCab.Trips/TripsGrpcService.cs
using CritterCab.Trips.V1;       // proto-generated namespace
using Wolverine.Grpc;

namespace CritterCab.Trips;

/// <summary>
///     Wolverine-managed proto-first gRPC stub for the Trips service.
///     The proto-generated <see cref="Trips.TripsBase"/> class supplies the
///     gRPC contract. Wolverine code-generates a concrete subclass at startup
///     that overrides every RPC and forwards it to the message bus.
/// </summary>
[WolverineGrpcService]
public abstract partial class TripsGrpcService : Trips.TripsBase;
```

Three things to know about this declaration:

- **`abstract`** is required. The stub doesn't implement the RPC methods — Wolverine's generated subclass does. Concrete stubs are valid only for the hand-written path (client-streaming workaround), which lives in `wolverine-grpc-bidirectional-handlers`.
- **`partial`** is optional but conventional in Cab. Adding `Validate` methods, `[WolverineBefore]` middleware, or other supporting code in a sibling `TripsGrpcService.Validate.cs` file keeps the discovery declaration crisp.
- **Naming**: `<Service>GrpcService` matches the convention `MapWolverineGrpcServices` uses for discovery (name suffix `GrpcService`). The `[WolverineGrpcService]` attribute is required for proto-first stubs regardless — discovery checks for the suffix OR the attribute, but the attribute is what tells Wolverine "code-generate the wrapper" rather than "map this class directly."

The generated wrapper's name is `{ProtoServiceName}GrpcHandler` — for `service Trips { ... }` in the `.proto`, the wrapper is `TripsGrpcHandler`. This is what you pass to `wolverine-diagnostics codegen-preview --grpc`:

```bash
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics codegen-preview --grpc TripsGrpcHandler
# Or just the proto service name — the command normalizes it
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics codegen-preview --grpc Trips
```

---

## Unary handlers

The simplest case: one request, one response.

### Proto declaration

```protobuf
service Trips {
  rpc StartTrip(StartTripRequest) returns (StartTripResponse);
  rpc CompleteTrip(CompleteTripRequest) returns (CompleteTripResponse);
}
```

### Handler shape

The handler is an ordinary Wolverine handler per `wolverine-handlers`:

```csharp
// src/CritterCab.Trips/StartTripFeature/StartTripHandler.cs
using CritterCab.Trips.V1;          // proto-generated request/response types
using JasperFx.Events;
using Marten;

namespace CritterCab.Trips.StartTripFeature;

public static class StartTripHandler
{
    public static async Task<StartTripResponse> Handle(
        StartTripRequest request,
        IDocumentSession session,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var tripId = Guid.Parse(request.TripId);
        var startedAt = clock.GetUtcNow();

        var started = new TripStarted(
            tripId,
            Guid.Parse(request.RiderId),
            Guid.Parse(request.DriverId),
            ToGeoLocation(request.Pickup),
            startedAt);

        session.Events.StartStream<Trip>(tripId, started);
        await session.SaveChangesAsync(cancellationToken);

        return new StartTripResponse
        {
            TripId = request.TripId,
            StartedAt = Timestamp.FromDateTimeOffset(startedAt),
        };
    }
}
```

### What's actually happening at runtime

Wolverine generates `TripsGrpcHandler.StartTrip(StartTripRequest, ServerCallContext)` as:

```csharp
// Generated — never check this in
public override Task<StartTripResponse> StartTrip(
    StartTripRequest request,
    ServerCallContext context)
{
    return _bus.InvokeAsync<StartTripResponse>(request, context.CancellationToken);
}
```

`_bus.InvokeAsync<StartTripResponse>` matches the request type to the handler graph and dispatches to `StartTripHandler.Handle`. The cancellation token from the gRPC `ServerCallContext` flows to the handler — handlers should always accept `CancellationToken` and pass it to async operations.

Sync handlers work too (`public static StartTripResponse Handle(StartTripRequest request)`). Wolverine wraps them with the same dispatch path; the cancellation token is still flowed by the framework but not visible to the handler body.

### Naming and placement

- The handler lives in the request's vertical slice (`src/CritterCab.Trips/StartTripFeature/`) per `vertical-slice-organization`. Not in a `Grpc/` folder; the gRPC-ness of the request type doesn't change where it lives.
- The handler class is named `<RequestType>Handler` (`StartTripHandler`, not `TripsHandler`). One handler class per RPC. This matches Wolverine's discovery convention and keeps the slice atomic.
- The proto-generated request/response types use the `csharp_namespace` set in the `.proto` (`CritterCab.Trips.V1`). The handler receives them directly — no DTO translation layer. Per ADR-009, the contract is the model at the boundary.

---

## Server-streaming handlers

One request, a stream of responses. Cab's canonical examples are `StreamDriverOffers` (Dispatch fans driver offers to a candidate driver client) and `WatchTripStatus` (Trips streams updates to the rider client).

### Proto declaration

```protobuf
service Dispatch {
  rpc StreamDriverOffers(StreamDriverOffersRequest) returns (stream DriverOffer);
}
```

### Handler shape

The handler returns `IAsyncEnumerable<TResponse>` and accepts a `[EnumeratorCancellation] CancellationToken`:

```csharp
// src/CritterCab.Dispatch/StreamDriverOffersFeature/StreamDriverOffersHandler.cs
using System.Runtime.CompilerServices;
using CritterCab.Dispatch.V1;
using Marten;

namespace CritterCab.Dispatch.StreamDriverOffersFeature;

public static class StreamDriverOffersHandler
{
    public static async IAsyncEnumerable<DriverOffer> Handle(
        StreamDriverOffersRequest request,
        IQuerySession session,
        IDriverOfferStream offerStream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var driverId = Guid.Parse(request.DriverId);

        await foreach (var offer in offerStream.SubscribeAsync(driverId, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return offer;
        }
    }
}
```

### What's actually happening at runtime

Wolverine generates the gRPC method as an `await foreach` over `bus.StreamAsync<DriverOffer>`:

```csharp
// Generated — never check this in
public override async Task StreamDriverOffers(
    StreamDriverOffersRequest request,
    IServerStreamWriter<DriverOffer> responseStream,
    ServerCallContext context)
{
    await foreach (var item in _bus.StreamAsync<DriverOffer>(request, context.CancellationToken))
    {
        await responseStream.WriteAsync(item, context.CancellationToken);
    }
}
```

`_bus.StreamAsync<DriverOffer>` dispatches the request to the handler graph; the handler's `IAsyncEnumerable<DriverOffer>` is consumed item-by-item and each item is written to the gRPC stream writer.

### `[EnumeratorCancellation]` is required

The `[EnumeratorCancellation]` attribute on the `CancellationToken` parameter tells the C# compiler which parameter receives the cancellation token from `WithCancellation()` calls on the resulting `IAsyncEnumerable`. Without it, the compiler emits a warning and Wolverine cannot flow the gRPC server's `ServerCallContext.CancellationToken` into the handler's enumeration — which means the handler keeps running after the client disconnects.

This is non-negotiable for streaming handlers. Always include `[EnumeratorCancellation] CancellationToken cancellationToken`.

### Backpressure

Wolverine's generated wrapper writes each item to the stream writer with `await`. The await suspends until the gRPC framework signals the write completed (which respects HTTP/2 flow control). When the client is slow, the handler's `IAsyncEnumerable` enumeration suspends naturally — no backpressure code in the handler.

The handler's responsibility is to:

- Honor cancellation (`cancellationToken.ThrowIfCancellationRequested()` at appropriate points, or pass `cancellationToken` to async operations that respect it).
- Avoid producing items faster than the consumer can drain them when buffered upstream sources are involved (e.g., a Marten event subscription) — the natural `await` chain makes this work without explicit code as long as the producer respects the cancellation token.

### Naming the handler matches the request type

Same convention as unary: `StreamDriverOffersHandler` lives in `StreamDriverOffersFeature/` and handles `StreamDriverOffersRequest`. The fact that it returns `IAsyncEnumerable<DriverOffer>` rather than a single response is just the handler's shape, not the slice's shape.

---

## Validate short-circuit

Wolverine recognizes a `Validate` or `ValidateAsync` method on the stub class that returns `Status?` (nullable `Grpc.Core.Status`). When non-null, the generated wrapper throws `RpcException(status)` before dispatching to the handler.

```csharp
// src/CritterCab.Trips/TripsGrpcService.Validate.cs
using CritterCab.Trips.V1;
using Grpc.Core;
using Wolverine.Grpc;

namespace CritterCab.Trips;

public abstract partial class TripsGrpcService
{
    public static Status? Validate(StartTripRequest request)
    {
        if (string.IsNullOrEmpty(request.TripId))
            return new Status(StatusCode.InvalidArgument, "trip_id is required");

        if (string.IsNullOrEmpty(request.RiderId))
            return new Status(StatusCode.InvalidArgument, "rider_id is required");

        if (request.Pickup is null)
            return new Status(StatusCode.InvalidArgument, "pickup is required");

        return null;
    }
}
```

When `Validate` returns null, the call proceeds. When non-null, the wrapper throws `RpcException` and the handler never runs. The client sees the gRPC status code and detail message directly.

### `Validate` vs `ValidateAsync`

Both are recognized. Use `ValidateAsync` only when validation needs an async source (e.g., checking a database for tenant existence); prefer the synchronous `Validate` for shape-checking which is the dominant case.

### Multiple RPCs in one service

A `Validate` method per request type — the shape is:

```csharp
public abstract partial class TripsGrpcService
{
    public static Status? Validate(StartTripRequest request) => /* ... */;
    public static Status? Validate(CompleteTripRequest request) => /* ... */;
    public static Status? Validate(WatchTripStatusRequest request) => /* ... */;
}
```

Method overload resolution dispatches to the right one. Wolverine matches by parameter type — there's no method-name-per-RPC convention.

### Validate is for shape; not business rules

`Validate` should check structural correctness — required fields present, IDs parseable, ranges sensible. Business rules ("can this rider start a trip when they have an outstanding payment?") belong in the handler, where they have access to the document session, message bus, and time provider. The pattern here mirrors HTTP's `IProblemDetails` short-circuit per `wolverine-http-handlers` — it's the gRPC counterpart for shape validation.

### When to opt into rich validation errors

For services that wire FluentValidation (via `Wolverine.FluentValidation.Grpc`), the rich error path covers structured validation failures returned as `google.rpc.BadRequest` payloads attached to a single `INVALID_ARGUMENT` status. Cab does not currently commit FluentValidation; the `Validate` short-circuit above is the canonical Cab pattern. If FluentValidation lands later, the rich-error-details opt-in is `opts.UseGrpcRichErrorDetails()` on `WolverineOptions`.

---

## `[WolverineBefore]` and `[WolverineAfter]` middleware

For per-service cross-cutting concerns (correlation IDs, audit logging, custom metrics), declare static methods on the stub class. Wolverine's middleware policy weaves them into the generated wrapper:

```csharp
// src/CritterCab.Trips/TripsGrpcService.Middleware.cs
using CritterCab.Trips.V1;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Wolverine.Attributes;

namespace CritterCab.Trips;

public abstract partial class TripsGrpcService
{
    [WolverineBefore]
    public static (Activity? activity, IDisposable? scope) Before(
        StartTripRequest request,
        ServerCallContext context,
        ILogger<TripsGrpcService> logger)
    {
        var activity = Cab.Trips.ActivitySource.StartActivity("Trips.StartTrip");
        activity?.SetTag("trip.id", request.TripId);
        activity?.SetTag("rider.id", request.RiderId);

        var scope = logger.BeginScope("trip.start tripId={TripId} riderId={RiderId}",
            request.TripId, request.RiderId);

        return (activity, scope);
    }

    [WolverineAfter]
    public static void After(Activity? activity, IDisposable? scope)
    {
        activity?.Dispose();
        scope?.Dispose();
    }
}
```

The middleware methods receive whatever the request handler can: the request itself, services from DI, the `ServerCallContext`, and other contextual values. Anything they `return` becomes available to subsequent frames (including `[WolverineAfter]` methods and the handler).

### Service-wide middleware via `WolverineGrpcOptions`

For middleware that applies to every gRPC chain in the service (not just one stub), register on `WolverineGrpcOptions`:

```csharp
builder.Services.AddWolverineGrpc(opts =>
{
    opts.AddMiddleware<TripCorrelationMiddleware>();
});

// Where TripCorrelationMiddleware is a class with conventional Before/After methods:
public class TripCorrelationMiddleware
{
    public static void Before(ServerCallContext context, ILogger<TripCorrelationMiddleware> logger)
    {
        var correlationId = context.RequestHeaders.GetValue("x-correlation-id")
            ?? Guid.CreateVersion7().ToString();
        context.UserState["correlation-id"] = correlationId;
    }
}
```

`opts.AddMiddleware<T>(filter)` accepts an optional predicate to scope to a subset of chains (e.g., only the `Trips` proto service):

```csharp
opts.AddMiddleware<AuditLoggingMiddleware>(chain =>
    chain is GrpcServiceChain g && g.ProtoServiceName == "Trips");
```

---

## Exception → StatusCode mapping (AIP-193)

Wolverine's gRPC integration applies a default exception → `StatusCode` table at the adapter boundary, following [Google AIP-193](https://google.aip.dev/193). Handlers throw ordinary .NET exceptions; clients see canonical gRPC status codes without per-handler mapping code.

### Default table

| Exception thrown | StatusCode returned |
|---|---|
| `RpcException` (any) | preserves the original `StatusCode` |
| `OperationCanceledException` | `Cancelled` |
| `TimeoutException` | `DeadlineExceeded` |
| `ArgumentException` (and derivatives) | `InvalidArgument` |
| `KeyNotFoundException` | `NotFound` |
| `FileNotFoundException`, `DirectoryNotFoundException` | `NotFound` |
| `UnauthorizedAccessException` | `PermissionDenied` |
| `InvalidOperationException` | `FailedPrecondition` |
| `NotImplementedException`, `NotSupportedException` | `Unimplemented` |
| Anything else | `Internal` |

In practice for Cab handlers, two patterns are common:

```csharp
// 1. Throw the canonical .NET exception when it fits.
public static async Task<TripDetailsResponse> Handle(GetTripDetailsRequest request, IQuerySession session, ...)
{
    var trip = await session.LoadAsync<Trip>(Guid.Parse(request.TripId), cancellationToken);
    if (trip is null)
        throw new KeyNotFoundException($"Trip {request.TripId} not found");
    // → client sees StatusCode.NotFound automatically
    return MapToResponse(trip);
}

// 2. Throw RpcException directly when the canonical .NET exception doesn't fit the case.
public static async Task<RequestRideResponse> Handle(RequestRideRequest request, ...)
{
    if (await DriversAreAllBusy(...))
        throw new RpcException(new Status(StatusCode.ResourceExhausted, "No drivers available"));
    // ...
}
```

### Per-exception override

For domain exceptions where the default mapping is wrong, register an explicit override on `WolverineGrpcOptions`:

```csharp
builder.Services.AddWolverineGrpc(opts =>
{
    opts.MapException<TripNotFoundException>(StatusCode.NotFound);
    opts.MapException<DispatchUnavailableException>(StatusCode.Unavailable);
    opts.MapException<RiderHasOutstandingBalanceException>(StatusCode.FailedPrecondition);
});
```

The override table is checked before the default — application-specific mappings always win. Inheritance is respected: a mapping for `MyBaseException` also matches `MyDerivedException` unless a more-specific entry exists. Later registrations win over earlier ones for the same type.

### Rich error details (opt-in)

For richer error payloads — `google.rpc.Status` with structured detail messages like `BadRequest`, `PreconditionFailure`, `ResourceInfo` — opt in via `opts.UseGrpcRichErrorDetails()`:

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.UseGrpcRichErrorDetails(config =>
    {
        // Optional: enable a default ErrorInfo payload on every error
        config.EnableDefaultErrorInfo();
    });
});
```

This is most useful when a validation adapter (e.g., FluentValidation) is registered: validation failures become structured `BadRequest` payloads attached to the `INVALID_ARGUMENT` status. Without an adapter the rich-details path is a no-op — there's no cost to calling `UseGrpcRichErrorDetails()` even in services that don't yet have rich validation.

Cab's default posture is the basic AIP-193 mapping. Reach for rich error details when a service surface needs to communicate structured failure detail to clients (typically: complex validation flows, or rate-limit feedback with retry-after metadata).

---

## Service-to-service gRPC clients

Server-side handlers are this skill's primary scope, but the inverse — Cab's Trips service calling Cab's Pricing service — uses the standard `Grpc.Net.Client` typed client generated by protoc.

### Aspire wiring

Per `aspire`, the Trips service's `apphost.cs` reference declares a dependency on Pricing:

```csharp
var pricing = builder.AddProject<Projects.CritterCab_Pricing>("pricing");
var trips = builder.AddProject<Projects.CritterCab_Trips>("trips")
    .WithReference(pricing)
    .WaitFor(pricing);
```

Trips gets `services__pricing__https__0` injected — the Pricing service's HTTPS endpoint URL.

### Client registration in Trips

```csharp
// src/CritterCab.Trips/Program.cs (additions)
builder.Services.AddGrpcClient<Pricing.PricingClient>(o =>
{
    // Aspire's service-discovery resolver substitutes the actual URL at runtime.
    o.Address = new Uri("https+http://pricing");
});
```

`Grpc.Net.ClientFactory` and the Aspire service-discovery extensions handle URL resolution — the `https+http://pricing` form is the same convention covered in `aspire`. The generated `PricingClient` comes from the proto-generated stubs in Trips (since Trips references Pricing's `.proto` file via `<Protobuf Include="..." GrpcServices="Client" />` in the `.csproj`).

### Calling the client from a handler

Inject the typed client like any other service:

```csharp
public static async Task<StartTripResponse> Handle(
    StartTripRequest request,
    Pricing.PricingClient pricingClient,
    IDocumentSession session,
    CancellationToken cancellationToken)
{
    var quote = await pricingClient.RequestQuoteAsync(
        new QuoteRequest { /* ... */ },
        cancellationToken: cancellationToken);

    // ... use quote.EstimatedFare, etc.
}
```

### Exception symmetry on the client side

Wolverine.Grpc's client interceptor performs the inverse of the server's AIP-193 mapping: an incoming `RpcException` with `StatusCode.NotFound` is rethrown as `KeyNotFoundException` (with the original `RpcException` preserved on `InnerException`). Trips can `catch (KeyNotFoundException)` instead of branching on `RpcException.StatusCode` — the trip-not-found case from a downstream Pricing call surfaces as the same exception type as a missing record from a local Marten query.

This is opt-in on the client side via the same `AddWolverineGrpc()` call — when Trips registers `services.AddWolverineGrpc()`, the client-side interceptor is wired automatically alongside the server-side one.

---

## Tooling

Two CLI surfaces from `cli-jasperfx` are particularly relevant when working on gRPC handlers:

```bash
# Inspect the generated wrapper for a proto service
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics codegen-preview --grpc TripsGrpcHandler
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics codegen-preview --grpc Trips

# Verify routing — which transport does StartTripRequest go to?
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics describe-routing StartTripRequest
```

The first reveals the exact generated wrapper code — useful when handler behavior surprises you and you want to see what's actually executing. The second confirms the request type is being routed locally to the handler graph (rather than to an external transport) — a common mismatch when Wolverine's routing conventions are unfamiliar.

For client-side inspection during development, `grpcurl` and `Evans` are the standard tools, covered in `cli-grpc-tooling` (Phase 3).

---

## Common pitfalls

- **Concrete stub class instead of abstract.** A non-abstract `[WolverineGrpcService]` stub deriving from a proto base fails at startup with `InvalidOperationException` from Wolverine's `AssertNoConcreteProtoStubs` check. The reason: a concrete stub has no remaining abstract proto methods left to override, so Wolverine's generated subclass would silently no-op. The stub MUST be `abstract` for the proto-first auto-generation path. Concrete `[WolverineGrpcService]` classes that DON'T derive from a proto base are valid — they go through the hand-written chain path covered in `wolverine-grpc-bidirectional-handlers`.
- **Missing `[EnumeratorCancellation]` on streaming handler's CancellationToken.** Compiler warns; Wolverine can't flow the gRPC server's cancellation token to the handler's enumeration. The handler keeps running after the client disconnects. Always include the attribute.
- **Returning `Task<IAsyncEnumerable<T>>` from a streaming handler.** The shape is `IAsyncEnumerable<T>` — async-iterator semantics are baked in via `yield return`. Returning a `Task` of the async enumerable defeats Wolverine's discovery; the handler won't match the `bus.StreamAsync<T>` dispatch.
- **Putting the handler in a `Grpc/` folder.** The handler's slice is the request, not the protocol. `StartTripRequest` lives in `StartTripFeature/` per `vertical-slice-organization`, regardless of whether it arrived via gRPC, HTTP, or a messaging transport.
- **Importing `Grpc.Core` types into the handler body.** The handler is protocol-agnostic. If a handler needs gRPC metadata, that's a smell — push the gRPC-specific concern up into a `[WolverineBefore]` middleware on the stub class.
- **`Validate` returning a value that isn't `Status?`.** Wolverine's middleware discovery looks for the specific `Status?` return type to wire the short-circuit. A method named `Validate` returning `bool` or `string` is treated as ordinary middleware, not a validation gate.
- **Throwing `Exception` from a handler and expecting `Internal`.** Yes, that's the default — but it produces an opaque error to the client. Either throw a more specific .NET exception (which the AIP-193 table maps), or define a domain exception and register `opts.MapException<T>()` for it. Reserve `Exception` for genuine "this should not happen" cases that operators investigate.
- **Forgetting `await using` on the gRPC channel.** Client-side: `GrpcChannel` is `IAsyncDisposable`. Long-lived clients registered via `AddGrpcClient<T>` are managed by `Grpc.Net.ClientFactory` — no explicit dispose needed. Direct `GrpcChannel.ForAddress(...)` usage requires `await using`.
- **Plaintext HTTP/2 in production-like environments.** Wolverine's sample uses unencrypted HTTP/2 for simplicity. Cab uses HTTPS in dev (Aspire dev cert) and TLS in production. The `AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true)` flag the sample uses is appropriate only for sample code, never for Cab.
- **Not handling `OperationCanceledException` distinctly.** When a streaming handler is cancelled (client disconnect), it throws `OperationCanceledException`, which the AIP-193 table maps to `Cancelled`. This is correct — but if the handler catches all exceptions (`catch (Exception)` instead of letting it propagate), the cancellation becomes invisible and the framework can't terminate the stream cleanly.
- **Confusing the gRPC service stub with a Wolverine handler.** They're different things in different roles. The stub (`TripsGrpcService`) is empty and exists for the wrapper code-gen; the handler (`StartTripHandler.Handle`) does the actual work. Adding handler logic to the stub is wrong on two counts: the stub is abstract (so the code wouldn't run anyway), and the gRPC-ness of the request type doesn't change where the handler should live.
- **Trying to use `[WolverineGrpcService]` for a client-streaming RPC.** Wolverine 5.32 doesn't auto-generate wrappers for client-streaming methods — startup fails fast with a clear error. Use the hand-written workaround from `wolverine-grpc-bidirectional-handlers`.

---

## See also

**Prerequisites** — Cab-internal skills to load first if unfamiliar:

- `protobuf-contracts` — the contract authoring conventions that produce the proto-generated `*Base` class this skill's stubs derive from.
- `transport-selection` — when to choose gRPC at all, and which streaming mode fits the flow shape.
- `service-bootstrap` — the broader `Program.cs` shape that this skill's bootstrap section extends.
- `wolverine-handlers` — the underlying handler shape (`Handle(TRequest)`, dependency injection conventions, sync vs async semantics) that gRPC-routed messages share with HTTP and messaging.
- `vertical-slice-organization` — where the handler files live in the service's source tree.

**Sibling skills:**

- `wolverine-http-handlers` — HTTP endpoint handlers; Cab services typically expose both, sharing the same handler bodies through Wolverine's discovery.
- `wolverine-messaging-handlers` — messaging handlers; same handler shape, different transport.
- `marten-wolverine-aggregates` — aggregate-loading handlers (`[WriteAggregate]`, `[Aggregate]`) work behind gRPC RPCs the same way they work behind HTTP endpoints.
- `aspire` — service discovery and HTTPS-with-dev-cert wiring that the gRPC client setup depends on.
- `cli-jasperfx` — `wolverine-diagnostics codegen-preview --grpc` and `describe-routing` for gRPC-specific debugging.

**Downstream:**

- `wolverine-grpc-bidirectional-handlers` (Phase 4) — client-streaming and bidirectional patterns, including the hand-written workaround for client-streaming since Wolverine 5.32 doesn't auto-generate that shape.
- `cli-grpc-tooling` (Phase 3) — buf, grpcurl, Evans CLI invocations.
- `testing-integration` — fixture pattern for integration-testing a service with gRPC endpoints.
- `testing-advanced` (Phase 4) — gRPC-specific scenario assembly, in-process gRPC clients via `WebApplicationFactory`, streaming-test patterns.
- `observability-tracing` (Phase 3) — the OpenTelemetry spans the `[WolverineBefore]` middleware emits flow into the same trace pipeline as HTTP and messaging handlers.

**External:**

- [Wolverine gRPC documentation](https://wolverinefx.net/guide/grpc/) — Wolverine 5.32+ gRPC integration guide.
- [Google AIP-193 — Errors](https://google.aip.dev/193) — the canonical exception → gRPC status code mapping table this skill follows.
- [`google.rpc.Status`](https://github.com/googleapis/googleapis/blob/master/google/rpc/status.proto) — the rich-error-details message type used by `UseGrpcRichErrorDetails()`.
- [ASP.NET Core gRPC services documentation](https://learn.microsoft.com/aspnet/core/grpc/) — Kestrel HTTP/2 configuration, `AddGrpc()`, `MapGrpcService<T>()`.
- [`Grpc.Net.Client` documentation](https://learn.microsoft.com/aspnet/core/grpc/client) — typed client patterns, channel management, interceptors.
