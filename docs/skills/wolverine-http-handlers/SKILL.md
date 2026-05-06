---
name: wolverine-http-handlers
description: "HTTP endpoint patterns in CritterCab — route binding, mixed route+body shapes, [EmptyResponse], concrete return types vs IResult, OpenAPI inference, and the silent-failure footguns specific to HTTP handlers. Use when authoring or reviewing any [WolverinePost]/[WolverineGet]/etc. endpoint."
cluster: wolverine
tags: [wolverine, handlers, http, endpoints, openapi, routes]
---

# Wolverine HTTP Handlers

HTTP-specific handler patterns in CritterCab. Wolverine's `WolverineFx.Http` integration turns a static method into an HTTP endpoint via attributes (`[WolverinePost]`, `[WolverineGet]`, `[WolverinePut]`, `[WolverineDelete]`). The handler shape is the same as any Wolverine handler, but the trigger surface (route binding, request body deserialization, response serialization) introduces patterns and footguns that don't appear elsewhere.

This skill assumes you've loaded `wolverine-handlers` first — the general handler shape, validation pipeline, and `IStartStream` semantics live there. This skill picks up at HTTP-specific decisions.

## When to apply this skill

Use this skill when:

- Authoring an HTTP endpoint as a Wolverine handler — `[WolverinePost]`, `[WolverineGet]`, `[WolverinePut]`, `[WolverineDelete]`, `[WolverinePatch]`.
- Designing the request/response shape of an endpoint.
- Choosing between compound-handler and single-method-endpoint patterns.
- Diagnosing endpoint-specific issues: 200 returned but events missing, OpenAPI schemas incomplete, route parameters not binding.
- Reviewing a PR that adds or modifies an HTTP endpoint.

Do NOT use this skill for:

- General handler shape and conventions — see `wolverine-handlers` (load first).
- Hybrid handlers that serve both HTTP and message-bus paths from one class — see ai-skills `wolverine-http-hybrid-handlers`.
- Aggregate-specific HTTP integration patterns (`UpdatedAggregate`, custom identity sources via `[FromHeader]`/`[FromClaim]`/etc.) — see ai-skills `wolverine-http-marten-integration`.

---

## Anti-Pattern: Wrong Tuple Order on HTTP Endpoints

Tuple order is positional and load-bearing. The HTTP response type goes **first**.

```csharp
// ❌ WRONG — IStartStream serialized as the HTTP response body; CreationResponse lost
public static (IStartStream, CreationResponse<Guid>) Handle(...) { ... }

// ✅ CORRECT
public static (CreationResponse<Guid>, IStartStream) Handle(...) { ... }
```

The same rule applies to triple-tuples: `(IResult, Events, OutgoingMessages)`, never `(Events, IResult, OutgoingMessages)`. When in doubt, the HTTP-response element is first.

---

## Anti-Pattern: Mixed Route + JSON Body on Compound Handlers

The compound handler pattern (`Validate`/`Handle` on the same class with shared state) does not work when the endpoint mixes route parameters and a JSON body. `Validate` cannot access the deserialized body — Wolverine's parameter resolution for compound handler methods can't see across the route-vs-body boundary at the validation step.

```csharp
// ❌ FAILS — Validate cannot reach the JSON body
public static class UpdateTripFareHandler
{
    public static ProblemDetails Validate(Guid tripId, decimal newFare) { /* no access to body */ }
    [WolverinePut("/api/trips/{tripId}/fare")]
    public static IResult Handle(Guid tripId, UpdateTripFareRequest request, ...) { ... }
}

// ✅ CORRECT — direct single-method endpoint when route + body mix
public sealed record UpdateTripFareRequest(decimal NewFare);

[WolverinePut("/api/trips/{tripId}/fare")]
public static async Task<IResult> UpdateTripFare(
    Guid tripId,
    UpdateTripFareRequest request,
    IDocumentSession session,
    TimeProvider time,
    CancellationToken ct)
{
    var trip = await session.Events.AggregateStreamAsync<Trip>(tripId, token: ct);
    if (trip is null) return Results.NotFound();
    if (trip.Status != TripStatus.InProgress) return Results.Conflict();

    session.Events.Append(tripId, new TripFareUpdated(tripId, request.NewFare, time.GetUtcNow()));
    await session.SaveChangesAsync(ct);
    return Results.Ok();
}
```

**When each pattern fits:**

| Endpoint shape | Pattern |
|---|---|
| All parameters from JSON body | Compound handler (`Validate`/`Handle`) |
| Route parameters only (with `[WriteAggregate]` for the loaded aggregate) | Compound handler |
| Mixed route + JSON body | Direct single-method endpoint |
| `DELETE` with no body | Direct single-method endpoint |
| `GET` with route + query parameters | Direct single-method endpoint |

---

## Anti-Pattern: Bare Event Return on an Aggregate-Handler HTTP Endpoint

Subtle and silent. On an HTTP endpoint with `[WriteAggregate]`, a bare event return is treated as the HTTP response body and serialized — the event is *not* appended to the stream.

```csharp
// ❌ FAILS SILENTLY — TripCancelled serialized to response body, never appended to stream
[WolverinePost("/api/trips/{tripId}/cancel")]
public static TripCancelled Cancel(
    [WriteAggregate(nameof(CancelTrip.TripId))] Trip trip, CancelTrip cmd, TimeProvider time)
    => new TripCancelled(trip.Id, cmd.Reason, time.GetUtcNow());

// ✅ [EmptyResponse] suppresses the body, event appended, 204 returned
[WolverinePost("/api/trips/{tripId}/cancel"), EmptyResponse]
public static TripCancelled Cancel(
    [WriteAggregate(nameof(CancelTrip.TripId))] Trip trip, CancelTrip cmd, TimeProvider time)
    => new TripCancelled(trip.Id, cmd.Reason, time.GetUtcNow());
```

**Cab rule:** never return a bare event type from an aggregate-handler HTTP endpoint. Either `[EmptyResponse]` + bare event (single event, no integration message), or an explicit tuple with `Results.NoContent()` first when also publishing integration messages — see ai-skills `wolverine-http-marten-integration` § Returning events as HTTP response body for the elaborated tuple shape.

---

## Concrete Return Types vs IResult

Cab convention: prefer concrete return types so Wolverine infers OpenAPI metadata automatically. Reserve `IResult` for two cases — endpoints with genuinely runtime-variable response shapes (conditional redirects, content negotiation), and the `Results.NoContent()` first element of tuple returns on aggregate-handler endpoints (see § Bare Event Return).

```csharp
// ✅ Concrete — OpenAPI infers 200 + Trip schema; nullable infers 200-or-404
[WolverineGet("/api/trips/{tripId}")]
public static Task<Trip?> GetTrip(Guid tripId, IDocumentSession session) =>
    session.LoadAsync<Trip>(tripId);
```

For the full return-type → OpenAPI mapping (concrete `T`, nullable `T?`, `CreationResponse<T>`, `AcceptResponse`, `ProblemDetails` auto-400), see ai-skills `wolverine-http-fundamentals` § Return type conventions.

---

## Aggregate ID — Cab Convention

Cab uses the positional override `[WriteAggregate(nameof(Cmd.SomeId))]` on aggregate-handler endpoints, even when Wolverine's convention-based resolution would work without it. The `nameof()` argument is refactor-safe and makes the linkage between command property and aggregate ID source explicit at the handler.

```csharp
[WolverinePost("/api/trips/{tripId}/cancel"), EmptyResponse]
public static TripCancelled Cancel(
    [WriteAggregate(nameof(CancelTrip.TripId))] Trip trip,
    CancelTrip cmd, TimeProvider time)
    => new TripCancelled(trip.Id, cmd.Reason, time.GetUtcNow());
```

For the full identity-resolution chain (route segment matching, header, claim, custom method via `[Aggregate(FromHeader = ...)]` etc.), see ai-skills `wolverine-http-marten-integration` § Aggregate identity resolution. For generic route parameter binding rules (route vs query vs body, `[FromHeader]`, `[FromServices]`), see ai-skills `wolverine-http-fundamentals` § Parameter binding.

---

## Diagnosing Endpoint Issues

Two commands particularly relevant for HTTP endpoints. Run from the service's directory.

```bash
# Preview the generated endpoint code by route. First stop when route binding misbehaves
# or [WriteAggregate] doesn't load the aggregate.
dotnet run -- wolverine-diagnostics codegen-preview --route "POST /api/trips"

# Preview by handler class name. Useful when the route-vs-name match is unclear.
dotnet run -- wolverine-diagnostics codegen-preview --handler StartTripHandler
```

| Symptom | First check |
|---|---|
| Endpoint returns 200 but event not persisted | Bare-event-return on aggregate handler — see § that anti-pattern. Run `codegen-preview --route` and look for `IStartStream` or event-append interception. |
| Route parameter not binding | `codegen-preview --route` — verify the route attribute and parameter name match. |
| OpenAPI shows no response schema | Returning `IResult` instead of a concrete type — see § Concrete Return Types vs IResult. |
| `[WriteAggregate]` aggregate is null in handler | Identity resolution failed — check the `nameof()` argument or the convention chain (see § Route Binding). |

---

## See also

**Upstream** — generic Wolverine HTTP mechanics this skill defers to. ai-skills (license required, install via `npx skills add`):

- `wolverine-http-fundamentals` — generic HTTP integration: routing, parameter binding, OpenAPI inference from signatures, ProblemDetails-based validation, return type conventions.
- `wolverine-http-marten-integration` — aggregate handler endpoints (`[Aggregate]`/`[WriteAggregate]`), aggregate identity resolution chain, `UpdatedAggregate`, document operations.
- `wolverine-http-hybrid-handlers` — single handler serving both HTTP and message-bus paths via `[WolverineVerb]`, `MiddlewareScoping` for context-gated middleware.

**Prerequisites** — Cab-internal skills to load first if unfamiliar:

- `wolverine-handlers` — general handler shape, validation pipeline, `IStartStream` semantics, service registration conventions, logger convention.
- `csharp-coding-standards` — sealed records, `TimeProvider`, modern guard clauses.

**Sibling skills:**

- `wolverine-messaging-handlers` — message-bus handler patterns (routing rules, `OutgoingMessages` outbox, scheduled delivery).
- `wolverine-grpc-handlers` (Phase 3) — gRPC unary and server-streaming.
- `wolverine-grpc-bidirectional-handlers` (Phase 4) — gRPC client-streaming and bidirectional.

**Downstream** — natural follow-ups:

- `marten-wolverine-aggregates` — `[WriteAggregate]` mechanics for aggregate-handler endpoints (Phase 2).
- `cli-jasperfx` — full CLI surface for endpoint diagnostics (Phase 2).

**External:**

- [Wolverine HTTP Endpoints Guide](https://wolverinefx.net/guide/http/).
