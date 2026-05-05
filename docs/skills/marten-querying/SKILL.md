---
name: marten-querying
description: "Querying read-side documents and projections with Marten in CritterCab — standard LINQ, compiled queries, batched queries, query plans, raw JSON/SQL paths, and streaming JSONB straight to HTTP responses. Use when reading documents, querying projections, or designing a read-side endpoint."
cluster: marten
tags: [marten, querying, linq, compiled-queries, batched-queries, query-plans, raw-sql, json, http, performance]
---

# Marten Querying

Reading data from Marten in CritterCab. The query side has more variety than the write side: standard LINQ for the everyday case, compiled queries for hot paths, batched queries for bundling round trips, raw JSON and raw SQL when LINQ doesn't fit, and a JSON-to-HTTP streaming path that bypasses C# deserialization entirely.

The generic Marten query mechanics — LINQ provider rules, document-store internals, JSONB column layout — live in the JasperFx ai-skills library. **This skill documents the query-side patterns, gotchas, and Cab-specific opinions on top of those mechanics.** Aggregate shape is in `marten-aggregates`; projection registration is in `marten-projections`; handler-side aggregate loads (`[WriteAggregate]`, `MartenOps`) are in `marten-wolverine-aggregates`.

## When to apply this skill

Use this skill when:

- Writing a read-side endpoint or handler that queries documents or projections.
- Choosing between standard LINQ, a compiled query, a batched query, or raw SQL.
- Designing a high-throughput JSON read endpoint.
- Reviewing query code in PRs.
- Adding paging, JOIN/Include, or scalar aggregations to a Marten query.

Do NOT use this skill for:

- Aggregate shape and `Apply` methods — see `marten-aggregates`.
- Registering projections (live, inline, async) — see `marten-projections`.
- Handler patterns that load aggregates and append events — see `marten-wolverine-aggregates`.
- DCB-tagged queries and decision queries that span aggregates — see `dynamic-consistency-boundary` (Phase 2).
- Polecat queries — see `polecat-document-store` (Phase 4); the patterns mirror Marten's but the API surface differs in places.

---

## Sessions and Their Purpose

The session is the unit of work that executes queries and (for read-write sessions) tracks pending changes. Picking the right session shape matters for both correctness and performance.

### The two interfaces `AddMarten()` registers

`AddMarten()` registers two session interfaces in the DI container:

| Interface | Use for | Backed by |
|---|---|---|
| `IQuerySession` | Read-only operations | A read-only session — no `Store`, `Delete`, or `SaveChangesAsync`. |
| `IDocumentSession` | Read **and** write operations | A lightweight session by default. |

For pure-read endpoints, **inject `IQuerySession`.** It's narrower, expresses intent at the signature, and prevents accidental writes. Reach for `IDocumentSession` only when the same handler reads and writes — including event-sourced aggregate workflows where `MartenOps` and `[WriteAggregate]` need a document session.

`StreamAggregate<T>` is the one common read case where `IDocumentSession` is required: the live-aggregation path registers operations on the session internally. That's covered in § Streaming JSON to HTTP — usually a signal that a projected document plus `StreamOne<T>` is the better shape.

### The four session kinds (opening from `IDocumentStore` directly)

Most Cab code never opens a session from `IDocumentStore` — DI hands you one. Background jobs, daemons, and tests sometimes need explicit control:

| Factory method | Identity map | Dirty checking | When to use |
|---|---|---|---|
| `LightweightSession()` | No | No | **CritterCab default.** Fastest, lowest memory, manual `Store`/`Delete`. |
| `IdentitySession()` | Yes | No | A long compute that loads the same document repeatedly and benefits from the cached instance. |
| `DirtyTrackedSession()` | Yes | Yes | Code that mutates loaded entities and expects Marten to detect the changes. Heaviest; rarely needed in Cab. |
| `QuerySession()` | No | n/a | Read-only — same shape as the injected `IQuerySession`. |

Lightweight is the recommended default per the Marten team and is what `AddMarten()` wires up. Don't reach for the heavier kinds without a concrete reason. Avoid the legacy bare `OpenSession()` overload — the explicit factory methods are clearer, and the Marten team has signaled the bare overload may be dropped in a future version.

### Identity map — what it does to queries

When a session has an identity map (Identity or DirtyTracked), `Load<T>(id)` and `Query<T>().Where(t => t.Id == id)` return the **same instance** within the same session — Marten caches the materialized object after the first hit. Good for consistency (you can't end up with two divergent copies of the same logical document in one unit of work); bad for memory if a long-lived session loads many distinct documents.

Two consequences worth knowing:

- **Lightweight sessions return a fresh instance every time.** Code that relies on reference equality across reads will break under lightweight sessions.
- **User-supplied SQL bypasses the identity map.** `session.QueryAsync<T>("where ...")` and `AdvancedSql.QueryAsync<T>(...)` always hydrate fresh instances regardless of session kind. If a raw-SQL query disagrees with a LINQ query in the same session, this is likely the cause.

Configuration of the default session type — and any per-handler overrides — belongs in `service-bootstrap`. The deep mechanics (transaction lifecycle, isolation levels, session listeners, multi-tenant overlays) defer to ai-skills.

---

## Decision Matrix — Which Query Shape

Pick the shape by the read pattern, not by what feels familiar.

| Situation | Shape |
|---|---|
| Standard read, low-to-moderate frequency | `session.Query<T>().Where(...).ToListAsync(ct)` |
| Reusable, hot-path query | `ICompiledQuery<T>` / `ICompiledListQuery<T>` |
| Multiple independent queries in one request | `session.CreateBatchQuery()` with `IBatchedQuery` |
| Reusable query usable in both single and batch contexts | `QueryListPlan<T>` |
| Need raw JSON string (e.g., to forward to a cache) | `session.Json.FindByIdAsync<T>` / `.ToJsonArray()` / `.ToJsonFirstOrDefault()` |
| Simple WHERE clause that LINQ can't express cleanly | `session.QueryAsync<T>("where ...")` |
| Multi-table JOIN, custom aggregations, full SQL control | `session.AdvancedSql.QueryAsync<T1, T2, T3>(...)` |
| Stream JSON document(s) directly to an HTTP response | `StreamOne<T>` / `StreamMany<T>` / `StreamAggregate<T>` (return type) |

**CritterCab default:** standard LINQ for everyday reads. Promote to a compiled query when profiling identifies a real hot path. Reach for the streaming JSON return types (`StreamOne<T>` etc.) for new HTTP endpoints — see § Streaming JSON to HTTP for the Cab opinion on why.

---

## Standard LINQ

The everyday case. `IQuerySession` exposes `Query<T>()` returning `IMartenQueryable<T>`, which supports LINQ, async terminal operators, and Marten extensions like `Include`, `Stats`, and `AsJson`.

```csharp
public static class GetActiveDriversEndpoint
{
    [WolverineGet("/api/drivers/active")]
    public static Task<IReadOnlyList<DriverProfile>> Get(
        IQuerySession session,
        CancellationToken ct) =>
        session.Query<DriverProfile>()
            .Where(d => d.Status == DriverStatus.Online)
            .OrderBy(d => d.DisplayName)
            .ToListAsync(ct);
}
```

Most LINQ flavor mirrors the standard provider with a few Marten-specific extensions covered below. For the comprehensive surface (operators supported, JSONB query translation, `Where` predicate rules, child-collection handling) defer to ai-skills.

### `Include` for ad-hoc JOINs

`Include` lets one query hydrate documents from a related collection in the same round trip — useful for read models that follow ID references.

```csharp
var related = new List<DriverProfile>();
var trips = await session.Query<TripSummary>()
    .Include<DriverProfile>(t => t.DriverId, related)
    .Where(t => t.RegionId == regionId)
    .ToListAsync(ct);
// `related` is populated with the matching drivers.
```

### `Stats` for paging metadata

When paging, `Stats(out _, statistics)` populates a `QueryStatistics` instance with the unfiltered total count without a second round trip.

```csharp
var stats = new QueryStatistics();
var page = await session.Query<TripSummary>()
    .Where(t => t.RiderId == riderId)
    .Stats(out _, stats)
    .OrderByDescending(t => t.CompletedAt)
    .Skip(skip)
    .Take(take)
    .ToListAsync(ct);
var total = stats.TotalResults;
```

---

## Compiled Queries

Compiled queries pre-parse the LINQ expression once and reuse the SQL plan on every subsequent call — eliminating Expression-tree traversal on hot paths. Use when a query is stable, frequently called, and parameterized only by its public properties.

### Critical gotchas

These bite silently. Read before writing one.

- **Do not use async LINQ operators in the expression body.** The expression in `QueryIs()` is parsed statically — `FirstOrDefaultAsync`, `ToListAsync`, etc. break query planning. Use the synchronous equivalents inside `QueryIs()` and `await` at the call site.
- **Do not use C# primary constructors.** Marten's plan inspector cannot detect them. Use a parameterless constructor with init-able properties.
- **Do not call `ToList()` or `ToArray()` in the expression body.** Use `ICompiledListQuery<TDoc>` (returns `IEnumerable<T>`); calling `ToList()` inside `QueryIs()` throws from the underlying expression-rewriting library.
- **Boolean fields cannot be query parameters.** Marten's planner cannot match `bool` properties to command arguments. Work around with an enum or integer.

```csharp
// ❌ WRONG — async operator, ToList(), primary ctor
public class ActiveTripsQuery(Guid driverId) : ICompiledListQuery<TripSummary>
{
    public Expression<Func<IMartenQueryable<TripSummary>, IEnumerable<TripSummary>>> QueryIs() =>
        q => q.Where(t => t.DriverId == driverId).ToListAsync(); // async operator + primary ctor
}

// ✅ CORRECT — sync operators, parameterless ctor, async at call site
public class ActiveTripsQuery : ICompiledListQuery<TripSummary>
{
    public Guid DriverId { get; set; }

    public Expression<Func<IMartenQueryable<TripSummary>, IEnumerable<TripSummary>>> QueryIs() =>
        q => q.Where(t => t.DriverId == DriverId && t.Status == TripStatus.InProgress)
              .OrderByDescending(t => t.StartedAt);
}

var trips = await session.QueryAsync(new ActiveTripsQuery { DriverId = driverId }, ct);
```

### Interface reference

| Use case | Interface |
|---|---|
| Single document, no transform | `ICompiledQuery<TDoc>` |
| Single result with `Select()` transform | `ICompiledQuery<TDoc, TOut>` |
| List, no transform | `ICompiledListQuery<TDoc>` |
| List with `Select()` transform | `ICompiledListQuery<TDoc, TOut>` |

```csharp
public class TripById : ICompiledQuery<TripSummary>
{
    public Guid Id { get; set; }

    public Expression<Func<IMartenQueryable<TripSummary>, TripSummary>> QueryIs() =>
        q => q.FirstOrDefault(t => t.Id == Id);
}

public class TripIdsByRider : ICompiledListQuery<TripSummary, Guid>
{
    public Guid RiderId { get; set; }

    public Expression<Func<IMartenQueryable<TripSummary>, IEnumerable<Guid>>> QueryIs() =>
        q => q.Where(t => t.RiderId == RiderId).Select(t => t.Id);
}
```

### Paging with `IQueryPlanning`

When a compiled query has a computed property (typical for paging — `SkipCount = (Page - 1) * PageSize`), the property breaks Marten's planner. Implementing `IQueryPlanning` lets the planner set unique values during plan inspection.

```csharp
public class PagedTrips : ICompiledListQuery<TripSummary>, IQueryPlanning
{
    public int PageSize { get; set; } = 20;

    [MartenIgnore]                  // computed; not a DB parameter
    public int Page { private get; set; } = 1;

    public int SkipCount => (Page - 1) * PageSize;

    public Guid RiderId { get; set; }

    public QueryStatistics Statistics { get; } = new();

    public Expression<Func<IMartenQueryable<TripSummary>, IEnumerable<TripSummary>>> QueryIs() =>
        q => q.Where(t => t.RiderId == RiderId)
              .OrderByDescending(t => t.CompletedAt)
              .Stats(out _, Statistics)
              .Skip(SkipCount)
              .Take(PageSize);

    public void SetUniqueValuesForQueryPlanning()
    {
        // Values must produce unique SkipCount and PageSize for parameter mapping.
        Page = 3;
        PageSize = 20;
        RiderId = Guid.NewGuid();
    }
}

var query = new PagedTrips { Page = 2, PageSize = 10, RiderId = riderId };
var trips = await session.QueryAsync(query, ct);
var total = query.Statistics.TotalResults;
```

The `[MartenIgnore]` attribute and the unique-value rule are non-obvious; both have to be right or the plan compiles successfully but maps parameters wrong at runtime. Verify with at least one integration test per paged compiled query.

### `Include` inside a compiled query

The included collection must be a property on the query class — Marten populates it as part of execution.

```csharp
public class TripWithDriver : ICompiledQuery<TripSummary>
{
    public Guid TripId { get; set; }
    public IList<DriverProfile> Drivers { get; } = new List<DriverProfile>();

    public Expression<Func<IMartenQueryable<TripSummary>, TripSummary>> QueryIs() =>
        q => q.Include(t => t.DriverId, Drivers)
              .FirstOrDefault(t => t.Id == TripId);
}
```

---

## Batched Queries

`session.CreateBatchQuery()` bundles multiple independent queries into a single PostgreSQL round trip. Each registration returns a `Task<T>` that resolves when `Execute()` runs. Useful for endpoints or handlers that need several unrelated read models at once.

```csharp
var batch = session.CreateBatchQuery();

var tripTask    = batch.Load<TripSummary>(tripId);
var bidsTask    = batch.Query<RideOfferView>()
                       .Where(o => o.TripId == tripId)
                       .OrderByDescending(o => o.DispatchedAt)
                       .ToList();
var driverTask  = batch.Load<DriverProfile>(driverId);
var countTask   = batch.Query<TripSummary>().Count(t => t.RiderId == riderId);

await batch.Execute(ct);

var trip      = await tripTask;
var offers    = await bidsTask;
var driver    = await driverTask;
var tripCount = await countTask;
```

Compiled queries are valid inside a batch, with the same `Query(...)` syntax:

```csharp
var batch = session.CreateBatchQuery();
var activeTask = batch.Query(new ActiveTripsQuery { DriverId = driverId });
var pendingTask = batch.Query(new PendingOffersQuery { DriverId = driverId });
await batch.Execute(ct);
```

---

## Query Plans

`QueryListPlan<TItem>` is Marten's specification-pattern abstraction: a reusable query usable both directly (`session.QueryByPlanAsync`) and inside a batch (`batch.QueryByPlan`). Useful when the same query needs to run from multiple call sites and you want one source of truth.

```csharp
public sealed class ActiveTripsForRegion : QueryListPlan<TripSummary>
{
    public Guid RegionId { get; }

    public ActiveTripsForRegion(Guid regionId) => RegionId = regionId;

    public override IQueryable<TripSummary> Query(IQuerySession session) =>
        session.Query<TripSummary>()
            .Where(t => t.RegionId == RegionId && t.Status == TripStatus.InProgress)
            .OrderBy(t => t.StartedAt);
}

// Direct execution
var trips = await session.QueryByPlanAsync(new ActiveTripsForRegion(regionId), ct);

// In a batch
var batch = session.CreateBatchQuery();
var northTask = batch.QueryByPlan(new ActiveTripsForRegion(northRegionId));
var southTask = batch.QueryByPlan(new ActiveTripsForRegion(southRegionId));
await batch.Execute(ct);
```

`QueryListPlan<T>` returns `IReadOnlyList<T>`. For non-list returns, implement `IQueryPlan<T>` (single result) or `IBatchQueryPlan<T>` directly. Most Cab cases want the list shape, so `QueryListPlan<T>` is the default.

**When to reach for a query plan over a compiled query:** when the planner gotchas (no computed properties without `IQueryPlanning`, no async operators, no primary constructors, no boolean parameters) get in the way and the query isn't on a measured hot path. Query plans run as ordinary LINQ; compiled queries trade flexibility for performance.

---

## Raw JSON

Marten stores documents as JSONB. These APIs return the raw JSON string without deserializing — useful when forwarding to a cache, returning to clients verbatim, or interop with non-.NET code.

```csharp
// By ID
var json = await session.Json.FindByIdAsync<TripSummary>(tripId, ct);

// LINQ-based single result
var json = await session.Query<TripSummary>()
    .Where(t => t.Id == tripId)
    .ToJsonFirstOrDefault();      // null if no match
// Variants: ToJsonFirst() throws on miss; ToJsonSingle() / ToJsonSingleOrDefault() require exactly one match.

// Array
var jsonArray = await session.Query<TripSummary>()
    .Where(t => t.Status == TripStatus.InProgress)
    .OrderBy(t => t.StartedAt)
    .ToJsonArray();
```

`AsJson()` placed before a terminal operator works the same way and composes with `Select()` for shape projection at the SQL level:

```csharp
var json = await session.Query<TripSummary>()
    .OrderByDescending(t => t.StartedAt)
    .Select(t => new { t.Id, t.RiderId, t.DriverId })
    .ToJsonFirstOrDefault();
// → {"Id":"...","RiderId":"...","DriverId":"..."}
```

---

## Raw SQL

Drop down to raw SQL when LINQ won't express the query cleanly. Two entry points with different ergonomics.

### `session.QueryAsync<T>` — simple WHERE clauses

Marten treats SQL not starting with `SELECT` as a WHERE clause appended to the document table query. The `WHERE` keyword is optional — Marten adds it if missing.

```csharp
// Implicit "select data from mt_doc_tripsummary where ..."
var trips = await session.QueryAsync<TripSummary>(
    "where data ->> 'Status' = ?", "InProgress");

// Full SELECT for scalar results
var count = (await session.QueryAsync<int>(
    "select count(*) from mt_doc_tripsummary")).First();

// Custom placeholder character (default is '?')
var trips = await session.QueryAsync<TripSummary>('$',
    "where data ->> 'DriverId' = $", driverId.ToString());
```

If `T` is an Npgsql-mapped type (`int`, `Guid`, `string`, `DateTimeOffset`), Marten reads the first column directly. Otherwise it deserializes the first column as JSON.

### `session.AdvancedSql.QueryAsync<T>` — full SQL control

`AdvancedSql` gives full SQL control with Marten's result mapping. Supports up to three return types as a tuple via `ROW()` wrapping.

**Schema resolution.** Always resolve table names through the schema resolver — never hardcode:

```csharp
var schema = session.DocumentStore.Options.Schema;
var trips = schema.For<TripSummary>();           // e.g. "public.mt_doc_tripsummary"
var bare  = schema.For<TripSummary>(qualified: false); // "mt_doc_tripsummary"
```

**Column order matters for document types.** `SELECT` must return columns in this exact order:

1. `id` (required)
2. `data` (required)
3. `mt_doc_type` — only for document hierarchies
4. `mt_version` — only if versioning is enabled
5. Other metadata columns (`mt_last_modified`, `correlation_id`, etc.) — only if mapped
6. `mt_deleted`, `mt_deleted_at` — only if soft-delete is mapped

To inspect the right column order for a given document type:

```csharp
var cmd = session.Query<TripSummary>().ToCommand();
Console.WriteLine(cmd.CommandText);
```

```csharp
var schema = session.DocumentStore.Options.Schema;

// Document query
var trips = await session.AdvancedSql.QueryAsync<TripSummary>(
    $"select id, data from {schema.For<TripSummary>()} order by data ->> 'StartedAt'",
    ct);

// Multiple types via ROW() — joining trips and offers with a paging total
var results = await session.AdvancedSql.QueryAsync<TripSummary, RideOfferView, long>(
    $"""
    select
      row(t.id, t.data),
      row(o.id, o.data),
      row(count(*) over())
    from
      {schema.For<TripSummary>()} t
    join
      {schema.For<RideOfferView>()} o on (o.data ->> 'TripId')::uuid = t.id
    where
      t.data ->> 'RegionId' = $1
    order by t.data ->> 'StartedAt' desc
    limit $2 offset $3
    """,
    ct,
    regionId.ToString(), pageSize, skip);

// Stream large result sets without buffering
await foreach (var ping in session.AdvancedSql.StreamAsync<TelemetryPing>(
    $"select id, data from {schema.For<TelemetryPing>()} where data ->> 'DriverId' = $1",
    ct,
    driverId.ToString()))
{
    // process each ping
}
```

`AdvancedSql.StreamAsync<T>` returns an `IAsyncEnumerable<T>` and avoids loading the full result set into memory — relevant for Telemetry-class workloads where a query can return tens of thousands of rows.

---

## Metadata in Queries

Marten attaches metadata columns to every document table — some always present, some opt-in. On the query side this metadata is mostly invisible, but it surfaces in a few places worth knowing.

### What's stored

Always present:

- `mt_last_modified` — timestamp of the last update; indexed.
- `mt_dotnet_type` — full .NET type name. Informational, not used by Marten itself.
- `mt_version` — sequential GUID used for optimistic concurrency.

Opt-in (configured at bootstrap, often by implementing the marker interfaces below):

- `mt_deleted`, `mt_deleted_at` — soft-delete columns when the document type implements `ISoftDeleted` or is configured for soft-delete.
- `correlation_id`, `causation_id`, `last_modified_by` — tracking columns when the document type implements `ITracked` or tracking is enabled globally.

The marker interfaces in `Marten.Metadata` both enable the relevant column AND surface the metadata as a property on the document itself: `IVersioned` (`Guid Version`), `IRevisioned` (`int Version` for numeric concurrency), `ISoftDeleted` (`bool Deleted`, `DateTimeOffset? DeletedAt`), and `ITracked` (correlation/causation/last-modified-by).

### Soft-delete query behavior

When a document type is soft-delete-enabled, **queries auto-filter deleted documents.** To override, the query must reach into the `Marten.Linq.SoftDeletes` namespace:

```csharp
using Marten.Linq.SoftDeletes;

// Default — only non-deleted
var active = await session.Query<DriverProfile>().ToListAsync(ct);

// Include deleted
var all = await session.Query<DriverProfile>()
    .Where(d => d.MaybeDeleted()).ToListAsync(ct);

// Only deleted
var purged = await session.Query<DriverProfile>()
    .Where(d => d.IsDeleted()).ToListAsync(ct);

// Time-bounded
var recent = await session.Query<DriverProfile>()
    .Where(d => d.DeletedSince(DateTimeOffset.UtcNow.AddDays(-7)))
    .ToListAsync(ct);
```

Forgetting the `using` is a common cause of "method not found" errors — the extensions live in a namespace you don't normally need.

### Last-modified queries

`mt_last_modified` is indexed on every document type. Useful for "what changed since" patterns and cache-invalidation reads:

```csharp
using Marten.Linq.LastModified;

var recent = await session.Query<DriverProfile>()
    .Where(d => d.ModifiedSince(DateTimeOffset.UtcNow.AddMinutes(-5)))
    .ToListAsync(ct);
```

Bootstrap-side configuration of metadata defaults — what's enabled globally vs. per-document, the `DisableInformationalFields` policy — belongs in `service-bootstrap`.

---

## Streaming JSON to HTTP

Marten can stream JSONB straight from PostgreSQL to the HTTP response — no C# deserialization, no JSON serializer pass, no GC pressure on either side. The package is `Marten.AspNetCore` (referenced transitively via `WolverineFx.Marten` in Cab services).

CritterCab is Wolverine-HTTP-first. **Prefer the return-type API (`StreamOne<T>`, `StreamMany<T>`, `StreamAggregate<T>`) for new endpoints.** The extension-method API (`WriteSingle`, `WriteArray`, `WriteById`, `WriteLatest`) is fine for MVC controllers and middleware, but Cab doesn't have those — every endpoint is a Wolverine HTTP handler.

### `StreamOne<T>`, `StreamMany<T>`, `StreamAggregate<T>` (preferred)

These three types implement `IResult` and `IEndpointMetadataProvider`, so Wolverine's existing pipeline picks them up with no extra wiring and OpenAPI metadata is generated automatically. No `[ProducesResponseType]` attribute required.

```csharp
using Marten.AspNetCore;

// Single document — 404 on miss
[WolverineGet("/api/trips/{id:guid}")]
public static StreamOne<TripSummary> GetTrip(Guid id, IQuerySession session)
    => new(session.Query<TripSummary>().Where(t => t.Id == id));

// Array — empty array on no match, never 404
[WolverineGet("/api/trips/active")]
public static StreamMany<TripSummary> GetActive(IQuerySession session)
    => new(session.Query<TripSummary>().Where(t => t.Status == TripStatus.InProgress));

// Event-sourced aggregate snapshot — 404 on miss
[WolverineGet("/api/trips/{id:guid}/state")]
public static StreamAggregate<Trip> GetTripState(Guid id, IDocumentSession session)
    => new(session, id);
```

**404 semantics differ across the three types.** `StreamOne<T>` returns 404 when the query produces no result. `StreamAggregate<T>` returns 404 when no events exist for the stream. `StreamMany<T>` returns an empty JSON array (`[]`) and never 404s — matches REST array idiom and avoids "is empty an error?" ambiguity.

**Customization properties.** All three expose `OnFoundStatus` (override the default 200) and `ContentType` (override `application/json`):

```csharp
return new StreamOne<TripSummary>(query) { OnFoundStatus = 201, ContentType = "application/json" };
```

**Compiled-query overloads** exist as `StreamOne<TDoc, TOut>` and `StreamMany<TDoc, TOut>`, taking a session plus a compiled query:

```csharp
[WolverineGet("/api/trips/active")]
public static StreamMany<TripSummary, IEnumerable<TripSummary>> GetActiveCompiled(IQuerySession session)
    => new(session, new ActiveTripsAcrossSystem());
```

This is the highest-throughput path for stable hot endpoints: SQL plan compiled once, JSON never leaves the database as a C# string, OpenAPI metadata generated from the compiled query's output type.

**`StreamAggregate<T>` requires `IDocumentSession`**, not `IQuerySession`. Inject the document session only where reads are tightly bound to event-sourced aggregates; avoid creating a write session purely for reads.

### Extension-method API (legacy or non-Wolverine endpoints)

The original API is still supported and necessary in MVC controllers, middleware, or any code that holds an `HttpContext` rather than returning an `IResult`:

```csharp
session.Query<TripSummary>().Where(t => t.Id == id).WriteSingle(httpContext);
session.Query<TripSummary>().Where(t => t.Status == TripStatus.InProgress).WriteArray(httpContext);
session.Json.WriteById<TripSummary>(id, httpContext);
session.Events.WriteLatest<Trip>(id, httpContext);

// Compiled query overloads
session.WriteOne(new TripById { Id = id }, httpContext);
session.WriteArray(new ActiveTripsAcrossSystem(), httpContext);

// Raw SQL → HTTP
session.WriteJson(
    "select data from mt_doc_tripsummary where data ->> 'Status' = ?",
    httpContext,
    "InProgress");
```

`session.WriteJson(sql, httpContext, params)` is worth knowing — it streams a raw-SQL result set directly to the response without an intermediate type. Use sparingly; document queries written in raw SQL skip Marten's column-order safety net.

### Caveat: no anti-corruption layer

The streaming APIs serve exactly what's persisted. If the on-the-wire shape needs to differ from the stored shape (renaming, field omission, computed columns), introduce a projection (see `marten-projections`) or a `Select()` transform before the streaming call. Streaming APIs are not a place to evolve client contracts mid-flight.

---

## Common pitfalls

- **Compiled query with an async operator inside `QueryIs()`.** Silent: the plan compiles, then crashes at first execution with a misleading error. Use sync operators in the body; `await` only at the call site.
- **Compiled query with a primary constructor.** Marten's plan inspector cannot detect the constructor parameters and silently misroutes them. Use a parameterless constructor with init-able properties.
- **Compiled query with computed paging properties but no `IQueryPlanning` implementation.** The plan caches the wrong `Skip`/`Take` values and every call returns the same page. Always implement `IQueryPlanning` when computed properties are present.
- **Hardcoded table names in `AdvancedSql` queries.** Schema names drift across environments, multi-tenancy, and store-isolation boundaries. Always go through `session.DocumentStore.Options.Schema.For<T>()`.
- **Streaming endpoints that bypass the projection layer.** If clients need a different shape than what's persisted, introduce a projection — don't reach for raw SQL with a `Select()` transform every time. Streaming sells its performance; that performance disappears the moment the query gets baroque.
- **`StreamAggregate<T>` with `IQuerySession`.** Won't compile — requires `IDocumentSession`. If the surrounding endpoint only needs reads, the right answer is usually to switch to a projected document and use `StreamOne<T>` instead of pulling in a write session.
- **Speculative compiled queries.** Compiled queries trade flexibility for performance and add real cognitive load (the gotchas list above). Use them only on profiled hot paths; standard LINQ is the right default.
- **`WriteSingle` vs `WriteOne`.** `WriteSingle` is the IQueryable extension; `WriteOne` is the compiled-query overload on `IQuerySession`. Easy to swap accidentally — let the compiler tell you which is which by leaning on the return-type API.
- **Reading projected data immediately after writing in tests.** Async projections are eventually consistent — a query right after `SaveChangesAsync` may see stale data. See `testing-integration` (Phase 2) for `WaitForNonStaleProjectionDataAsync`.
- **Injecting `IDocumentSession` for a read-only endpoint.** Works mechanically, but signals "writes happen here" to readers and tooling. Inject `IQuerySession` for pure reads; the narrower interface prevents accidental `Store`/`Delete` calls and makes the read-only intent explicit.
- **Soft-delete extensions silently filtered out.** `MaybeDeleted()`, `IsDeleted()`, `DeletedSince()`, `DeletedBefore()` live in `Marten.Linq.SoftDeletes`. Without the `using`, the methods aren't visible — the compiler error reads like "method not found" rather than "missing namespace."

---

## See also

**Upstream** — load these first:

- `marten-aggregates` — aggregate shape and the snapshot/evolve idiom that produces queryable state.
- `marten-projections` — single-stream, multi-stream, and event projections; lifecycle choice; what gets queried here.
- `csharp-coding-standards` — `IReadOnlyList<T>` vs `Immutable*` collections on DTOs; positional records for query shapes.
- `service-bootstrap` — `AddMarten` configuration; document-type registration; serializer settings that affect JSON shape on the wire.

**Sibling skills:**

- `marten-wolverine-aggregates` — handler-side aggregate loads with `[WriteAggregate]` and `MartenOps`. Different concern from query-side; both share the same `IDocumentSession` mechanics.
- `marten-async-daemon` — daemon configuration that governs when async projections become queryable (Phase 2).
- `dynamic-consistency-boundary` — DCB-tagged queries and decision queries that span aggregate boundaries (Phase 2).

**Downstream:**

- `testing-integration` — `WaitForNonStaleProjectionDataAsync`, projection rebuild patterns, query assertions in tests (Phase 2).
- `wolverine-http-handlers` — the broader HTTP endpoint conventions; how the streaming return types compose with the rest of the pipeline.
- `polecat-document-store` — Polecat's parallel querying API for SQL Server services (Phase 4).

**External:**

- ai-skills `marten-event-sourcing-fundamentals` — generic Marten event-store mechanics that underpin event-sourced aggregate queries.
- ai-skills `marten-aggregate-handler-workflow` — the complete read-and-write workflow reference.
- All ai-skills installed via `npx skills add` (license required).
- [Marten Querying Documents](https://martendb.io/documents/querying/) — the canonical querying reference.
- [Marten Compiled Queries](https://martendb.io/documents/querying/compiled-queries.html) — compiled-query patterns and gotchas in depth.
- [Marten Batched Queries](https://martendb.io/documents/querying/batched-queries.html).
- [Marten Advanced SQL Queries](https://martendb.io/documents/querying/advanced-sql.html).
- [Marten.AspNetCore JSON Streaming](https://martendb.io/documents/aspnetcore.html).
- [Wolverine HTTP — Streaming JSON Responses](https://wolverine.netlify.app/guide/http/marten.html#streaming-json-responses) — the return-type API reference.
