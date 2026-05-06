---
name: polecat-document-store
description: "Polecat 3.1+ as a document database in CritterCab — the SQL-Server-backed sibling to Marten's document store, used in the Payments BC where compliance and audit constraints place the data tier on SQL Server. Covers session shapes (lightweight is the default in Polecat — opposite of Marten 7-and-earlier), storing and loading documents (Store as upsert, Insert fails if exists, Update fails if missing, SaveChangesAsync as the unit-of-work flush), identity strategies (Guid auto-assigned, int/long via HiLo sequences, string manually, strong-typed ID wrapper records auto-handled), LINQ querying via session.Query<T>() with the differences from Marten flagged (T-SQL JSON path syntax under JSON_VALUE/JSON_QUERY/OPENJSON rather than PostgreSQL jsonb operators), soft deletes (three opt-ins: [SoftDeleted] attribute, ISoftDeleted interface, opts.Policies.ForDocument<T> or AllDocumentsSoftDeleted, with the LINQ extensions MaybeDeleted/IsDeleted/DeletedSince/DeletedBefore and the UndoDeleteWhere restoration pattern), patching via session.Patch<T>(id) with Set/Increment/Append/AppendIfNotExists/Insert/Remove/Duplicate/Rename/Delete operations compiled to SQL Server JSON_MODIFY statements, and document-side optimistic concurrency via IVersioned (Guid) or IRevisioned (int) — both auto-detected and mutually exclusive — throwing JasperFx.ConcurrencyException on conflict. Cab uses this skill alongside polecat-event-sourcing for the Payments BC's read-side documents (MerchantConfig, payment summaries, refund records). Pairs with marten-querying the way polecat-event-sourcing pairs with marten-aggregates."
cluster: polecat
tags: [polecat, document-store, sql-server, linq, soft-deletes, patching, irevisioned, iversioned, hilo, lightweight-session, payments-bc]
---

# Polecat as a Document Database

Polecat is a full document database alongside its event-sourcing surface, and the document side is what Cab uses for all the non-event-sourced state in the Payments BC — `MerchantConfig` lookup data, `RefundRequest` transient records, query-side `PaymentSummary` projections, and the saga-state documents Wolverine persists for `PaymentLifecycleSaga`. This skill is the document-database companion to `polecat-event-sourcing`. The pairing mirrors how `marten-querying` pairs with `marten-aggregates` on the Marten side.

The defining framing is the same as for the event-sourcing skill: **Polecat is Marten with the engine swapped.** The document API surface — `IDocumentStore`, `IDocumentSession`, `IQuerySession`, `Store`/`Insert`/`Update`/`Delete`, LINQ via `session.Query<T>()`, soft deletes, patching, optimistic concurrency — translates almost directly from Marten patterns. The differences are in the engine layer: T-SQL JSON functions instead of PostgreSQL jsonb operators, lightweight sessions as the default rather than the opt-in, and a deliberately narrower feature set than Marten ships (Polecat's authoring stance is "include what's worth keeping; drop what isn't").

This skill assumes `polecat-event-sourcing` for the engine baseline (SQL Server 2025 native `json` column type, `pc_doc_{type}` table convention, Weasel.SqlServer schema management, `AddPolecat` + `IntegrateWithWolverine` bootstrap), and `marten-querying` for the LINQ patterns and decision frameworks that transfer directly. This skill documents the Polecat-specific surface and the differences worth knowing.

---

## When to apply this skill

Use this skill when:

- Working in CritterCab's Payments BC on document operations (non-event-sourcing) — storing `MerchantConfig`, loading payment summaries, patching settlement-batch metadata.
- Writing LINQ queries against documents in a Polecat-backed service — `session.Query<T>().Where(...).ToListAsync()` and friends.
- Adding soft-delete or patch semantics to a Polecat document type.
- Choosing between `IVersioned` and `IRevisioned` for a document's concurrency model.
- Designing identity strategies for a new document type — Guid vs HiLo numeric vs string vs strongly-typed ID wrapper.
- Diagnosing a session-shape decision (lightweight vs identity-map vs query session) in a Polecat-backed handler.

Do NOT use this skill for:

- Event-sourcing operations on Polecat — `polecat-event-sourcing` covers `StartStream`, `Append`, `FetchForWriting`, projections, the async daemon, DCB, subscriptions.
- Marten document operations — `marten-querying` is the canonical reference for the Marten-on-PostgreSQL story; the LINQ patterns transfer here, but PostgreSQL-specific operators and JSONB query forms don't.
- Wolverine saga storage internals — `wolverine-sagas` § Persistence covers how the Polecat saga frame provider wires saga state through `IDocumentSession`.
- The full Polecat surface — see `polecat-event-sourcing` for everything event-store-related; this skill is documents-only.

---

## Polecat document-store in CritterCab

Cab's Payments BC has both event-sourced state (the `Payment` aggregate, with its event stream) AND non-event-sourced state (configuration, lookup, read-side projections). The same `IDocumentStore` instance handles both. A single `IDocumentSession` enrolls events and document operations into one transaction — `SaveChangesAsync()` flushes everything atomically, with event-store operations processed first so inline projections can create documents from events in the same commit.

Concretely, in Payments:

- **Event-sourced**: `Payment` (stream root) and its Snapshot<Payment> projection (also a document).
- **Document-only**: `MerchantConfig` (per-merchant settings), `PaymentSummary` (read-side document projection from events), `RefundRequest` (transient state for the refund saga), `Wolverine.Saga` documents (e.g., `PaymentLifecycleSaga`'s state).

This skill focuses on the document-only patterns. The event-sourced documents (the snapshot projection target) are documented in `polecat-event-sourcing` § Single-stream projections.

---

## Sessions: lightweight is the default

Polecat ships with three session shapes, identical in purpose to Marten's but with one key default flipped:

```csharp
// Default: no identity tracking, fastest, the right choice for CQRS handlers
await using var session = store.LightweightSession();

// Identity-map session: tracks loaded documents by ID
await using var session = store.OpenSession(DocumentTracking.IdentityMap);

// Query-only session: cannot Store, Insert, Update, or Delete
await using var session = store.QuerySession();
```

Polecat 3.0 makes **lightweight** the default — opposite of Marten 7 and earlier where `LightweightSession()` was an opt-in over an identity-mapped default. The lightweight default is the right call for Cab's CQRS handlers: each handler load returns a fresh instance, no cross-handler tracking confusion, and the in-memory cost is bounded.

Identity-map sessions exist for the cases where you load the same document multiple times in one logical operation and want pointer-equality between the loads. Cab almost never uses these — handlers are short-lived and don't load the same document twice. Query sessions are the explicit read-only choice for query handlers; reaching for them communicates intent ("this handler will not mutate state") even though the lightweight session would refuse a Store call to a query-shaped session anyway.

### Session options

Sessions accept a `SessionOptions` for tenant scoping, command timeouts, metadata (correlation/causation/last-modified-by — carried through to the event store metadata columns when those are enabled), and isolation levels. For the full options surface and the listener mechanism (`IDocumentSessionListener`), see the Polecat sessions documentation.

For explicit transaction control with custom isolation:

```csharp
await using var session = await store.OpenSessionAsync(new SessionOptions
{
    IsolationLevel = IsolationLevel.Serializable,
});
```

Cab convention: in Wolverine handlers, the chain wires up the session for you and you accept it as a parameter. `IDocumentSession` is the write-capable parameter; `IQuerySession` is the read-only one. Don't open sessions manually inside a Wolverine handler — the chain owns the unit of work.

---

## Storing and loading documents

The three save operations have distinct semantics:

```csharp
// Upsert: insert if new, update if exists
session.Store(merchantConfig);

// Insert: throws if a document with the same ID already exists
session.Insert(new MerchantConfig { Id = merchantId, ... });

// Update: throws if the document doesn't already exist
var config = await session.LoadAsync<MerchantConfig>(merchantId);
config.SettlementSchedule = newSchedule;
session.Update(config);

// All three are queued; flush via SaveChangesAsync
await session.SaveChangesAsync();
```

Loading:

```csharp
// Single document
var config = await session.LoadAsync<MerchantConfig>(merchantId);
// Returns null if the document doesn't exist

// Many by ID
var configs = await session.LoadManyAsync<MerchantConfig>(id1, id2, id3);
// Returns IReadOnlyList<MerchantConfig>; missing IDs are silently dropped

// String key with strong-typed wrapper
var doc = await session.LoadAsync<MyDoc>(strongId);
```

Cab convention: prefer `Store` for the common "insert-or-update" case. Reach for `Insert` and `Update` only when the asymmetric throw semantics are part of the business rule (e.g., "this should only ever be a new merchant" or "fail loudly if the config disappeared between load and update").

---

## Identity strategies

Polecat's identity strategies parallel Marten's, with HiLo as the primary numeric-ID generator:

| Identity type | Strategy | Notes |
|---|---|---|
| `Guid` | Auto-assigned on `Store`/`Insert` if `Guid.Empty` | Cab's default for Payments documents |
| `int` / `long` | HiLo sequence | Configured per-document via `[HiloSequence]` or globally via `opts.HiloSequenceDefaults` |
| `string` | Application-assigned | Document must have `Id` set before storing |
| Strongly-typed wrapper | Auto-handled per the inner primitive | `record struct OrderId(Guid Value)` works without configuration |

```csharp
// Guid: auto-assigned
var doc = new MerchantConfig();         // Id is Guid.Empty
session.Store(doc);                     // After save, Id is a generated Guid

// HiLo for numeric:
public class Invoice
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
}

var invoice = new Invoice();            // Id is 0
session.Store(invoice);                 // After save, Id is from HiLo sequence

// Strongly-typed Guid wrapper
public record struct PaymentSummaryId(Guid Value);

public class PaymentSummary
{
    public PaymentSummaryId Id { get; set; }
    public PaymentStatus Status { get; set; }
}

var summary = new PaymentSummary();     // Id.Value is Guid.Empty
session.Store(summary);                 // After save, Id.Value is assigned
```

Cab convention: Guid for all Payments documents that aren't naturally string-keyed. The Trips/Dispatch BCs (Marten-backed) use Guid v7 stream IDs per the broader Cab convention; Payments mirrors this for consistency, even though the underlying SQL Server column is `uniqueidentifier` rather than PostgreSQL's `uuid`.

---

## LINQ querying

Standard LINQ via `session.Query<T>()`. The provider compiles expressions to T-SQL using SQL Server JSON functions:

```csharp
var active = await session.Query<MerchantConfig>()
    .Where(x => x.Status == "Active")
    .OrderBy(x => x.MerchantName)
    .ToListAsync();

var found = await session.Query<MerchantConfig>()
    .FirstOrDefaultAsync(x => x.MerchantId == merchantId);

var count = await session.Query<PaymentSummary>()
    .CountAsync(x => x.Status == PaymentStatus.Captured);

var page = await session.Query<PaymentSummary>()
    .OrderByDescending(x => x.CreatedAt)
    .Skip(50).Take(25)
    .ToListAsync();
```

### Differences from Marten

The LINQ surface is intentionally Marten-shaped. Three kinds of differences worth knowing:

- **JSON path syntax under the hood.** Marten compiles to PostgreSQL `jsonb` operators (`->`, `->>`, `@>`, `?`, etc.). Polecat compiles to T-SQL JSON functions (`JSON_VALUE`, `JSON_QUERY`, `JSON_MODIFY`, `OPENJSON`). For typical LINQ this is invisible — `x => x.Status == "Active"` works on both. The differences surface only when reaching for engine-specific operators (e.g., Marten's `Matches` for full-text search uses PostgreSQL `tsvector`; Polecat would need a different approach).
- **Polecat doesn't ship every Marten extension.** Compiled queries, batched queries, query plans, and raw SQL all exist (mirrors of Marten's `IQueryPlan`, `BatchedQuery`, `IAdvancedSql`). Some niche Marten features don't have Polecat equivalents — verify by checking the Polecat docs or source before assuming a Marten extension method works.
- **`QueryForNonStaleData()`** is the LINQ extension for waiting on the async daemon to catch up before running the query, parallel to Marten's same-named extension. Documented in `polecat-event-sourcing` § Async daemon.

For the broader query-shape decision matrix (when to use compiled queries vs raw SQL vs batched queries), `marten-querying` is canonical. The decision framework transfers; the syntax for any compiled or raw SQL form needs to be T-SQL rather than PostgreSQL.

### Raw SQL when LINQ won't fit

Polecat exposes raw SQL via `IAdvancedSql`:

```csharp
var results = await session.AdvancedSql.QueryAsync<MerchantConfig>(
    "SELECT data FROM payments.pc_doc_merchantconfig WHERE JSON_VALUE(data, '$.tier') = @tier",
    new { tier = "Gold" });
```

Schema name and table name follow the Polecat convention (`{schema}.pc_doc_{type}`). The query must return the `data` column for document materialization. Use raw SQL sparingly; LINQ covers nearly every Cab need.

---

## Soft deletes

Polecat supports both hard deletes (the default) and soft deletes (logical deletion). Three ways to opt a document into soft-delete behavior:

```csharp
// Option 1: Attribute on the type
[SoftDeleted]
public class MerchantConfig { ... }

// Option 2: Implement ISoftDeleted (gives you in-memory access to deletion state)
public class MerchantConfig : ISoftDeleted
{
    public Guid Id { get; set; }
    public bool Deleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    // ...
}

// Option 3: Configured per document (or all documents) via policies
opts.Policies.ForDocument<MerchantConfig>(mapping =>
{
    mapping.DeleteStyle = DeleteStyle.SoftDelete;
});

// Or globally:
opts.Policies.AllDocumentsSoftDeleted();
```

When soft-deleted is enabled, `session.Delete<T>(id)` flips an `is_deleted` flag and stamps `deleted_at = SYSDATETIMEOFFSET()` rather than removing the row. Standard queries automatically filter out soft-deleted documents, so reads continue to behave as if the document were gone:

```csharp
session.Delete<MerchantConfig>(merchantId);
await session.SaveChangesAsync();

var doc = await session.LoadAsync<MerchantConfig>(merchantId);
// doc is null — soft-deleted documents are filtered from default queries
```

### Querying soft-deleted documents

LINQ extensions surface deleted documents when needed:

```csharp
using Polecat.Linq;

// Include both live and deleted
var all = await session.Query<MerchantConfig>()
    .Where(x => x.MaybeDeleted())
    .ToListAsync();

// Only deleted
var deleted = await session.Query<MerchantConfig>()
    .Where(x => x.IsDeleted())
    .ToListAsync();

// Deleted within a time window
var recentlyDeleted = await session.Query<MerchantConfig>()
    .Where(x => x.DeletedSince(cutoff))
    .ToListAsync();

var oldDeleted = await session.Query<MerchantConfig>()
    .Where(x => x.DeletedBefore(cutoff))
    .ToListAsync();
```

### Restoring and force-deleting

```csharp
// Restore soft-deleted documents matching a predicate
session.UndoDeleteWhere<MerchantConfig>(x => x.MerchantId == merchantId);
await session.SaveChangesAsync();

// Force a permanent delete even when soft-delete is configured
session.HardDelete<MerchantConfig>(merchantId);
await session.SaveChangesAsync();
```

Cab convention: soft-delete `MerchantConfig` and other lookup data where audit history matters; hard-delete short-lived records like `RefundRequest` (the saga clears these on completion). Saga-state documents follow Wolverine's `MarkCompleted()` lifecycle, which uses hard delete by default — soft-deleting completed sagas would leak storage.

---

## Patching (partial updates)

The patching API updates documents in place via T-SQL `JSON_MODIFY()`, without loading the full document into memory:

```csharp
// Patch a single document by ID
session.Patch<MerchantConfig>(merchantId)
    .Set(x => x.Status, "Suspended");

// Multiple operations on one document
session.Patch<MerchantConfig>(merchantId)
    .Set(x => x.Status, "Active")
    .Set(x => x.LastReviewedAt, DateTimeOffset.UtcNow)
    .Increment(x => x.ReviewCount, 1);

await session.SaveChangesAsync();
```

### Available operations

```csharp
// Set a property (top-level or nested)
.Set(x => x.Status, "Completed")
.Set(x => x.Address.City, "Omaha")

// Increment numeric (negative for decrement)
.Increment(x => x.ItemCount, 1)
.Increment(x => x.ItemCount, -1)

// Append to a collection
.Append(x => x.Tags, "priority")
.AppendIfNotExists(x => x.Tags, "priority")

// Insert at a specific position
.Insert(x => x.Tags, "urgent", index: 0)

// Remove from a collection
.Remove(x => x.Tags, "obsolete")

// Duplicate a value to another path
.Duplicate(x => x.BillingAddress, x => x.ShippingAddress)

// Rename a JSON property (string → expression)
.Rename("oldPropertyName", x => x.NewPropertyName)

// Delete a property entirely
.Delete("obsoleteField")
.Delete(x => x.ObsoleteField)
```

### Patching by predicate

```csharp
// Patch all documents matching a where clause
session.Patch<MerchantConfig>(x => x.Tier == "Bronze")
    .Set(x => x.Tier, "Silver");

await session.SaveChangesAsync();
```

Each operation compiles to a `JSON_MODIFY(data, '$.path', value)` statement. Collection operations use `OPENJSON` and `STRING_AGG` to manipulate JSON arrays in-place. The wire bandwidth advantage is real: only the modified path travels to SQL Server, not the full document.

Cab convention: patch when the operation is single-property or small-property-set and the document is large or rarely loaded for other reasons. Load-mutate-store is fine for small documents in handlers that already have the document in memory.

---

## Optimistic concurrency

Polecat supports two document-level concurrency modes, mutually exclusive per document type:

### `IVersioned` — Guid-based

Each save generates a new `Guid` version; concurrent saves with stale versions throw `ConcurrencyException`.

```csharp
public class MerchantConfig : IVersioned
{
    public Guid Id { get; set; }
    public Guid Version { get; set; }
    public string Tier { get; set; } = "";
}

var config = await session.LoadAsync<MerchantConfig>(merchantId);
// config.Version is auto-populated from the database

config.Tier = "Gold";
session.Store(config);
await session.SaveChangesAsync();
// On conflict: throws JasperFx.ConcurrencyException
```

### `IRevisioned` — int-based

Integer revision counter that increments on each save:

```csharp
public class PaymentSummary : IRevisioned
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public PaymentStatus Status { get; set; }
}

// Same usage shape; conflict throws JasperFx.ConcurrencyException
```

### Auto-detection and explicit overrides

Polecat auto-detects which interface a document implements and wires the appropriate concurrency check. Both can also be configured manually:

```csharp
opts.Policies.ForDocument<MerchantConfig>(mapping =>
{
    mapping.UseOptimisticConcurrency = true;  // Guid-based
    // OR
    mapping.UseNumericRevisions = true;        // int-based
});
```

The two are mutually exclusive — choose one per type. Cab convention: `IRevisioned` for documents where the revision number is operationally useful (audit logs, "version 7 of merchant config"); `IVersioned` for documents where the conflict-detection semantic matters but the version display doesn't (Guid versions are opaque to UIs anyway).

### Explicit version checks

```csharp
session.UpdateExpectedVersion(config, expectedGuidVersion);  // For IVersioned
session.UpdateRevision(summary, expectedRevision: 3);        // For IRevisioned
```

Use these when the version came from outside the session (e.g., a client passed it in an HTTP request as an `If-Match` ETag) and you need to enforce the check explicitly.

### The exception

Both modes throw `JasperFx.ConcurrencyException` on conflict — the Critter Stack-wide concurrency type that `polecat-event-sourcing` and `wolverine-sagas` also rely on. Retry policy convention is the same:

```csharp
// On a Wolverine handler chain
chain.OnException<JasperFx.ConcurrencyException>()
    .RetryWithCooldown(100.Milliseconds(), 250.Milliseconds());
```

### Important: this is the *document* concurrency story

The `IRevisioned` auto-detection covers regular Polecat documents. **It does NOT apply to Wolverine sagas** — saga `Version` is managed by the Wolverine saga persistence framework, not by Polecat's `IRevisioned` auto-detection. `polecat-event-sourcing` § The Polecat saga concurrency story has the full resolution.

---

## What's NOT in Polecat that's in Marten

Calibration for developers carrying Marten habits. Polecat's authoring stance is "include what's worth keeping; drop the technical baggage." Some Marten features are deliberately not in Polecat:

- **Append modes other than QuickAppend.** Marten supports `Inline`, `Quick`, and `RichWrite` strategies for event appending; Polecat ships only QuickAppend (direct `INSERT` with `OUTPUT inserted.seq_id`). No mode to configure. (Documented in `polecat-event-sourcing`.)
- **PostgreSQL-specific full-text search.** Marten's `tsvector`-based full-text search isn't ported. SQL Server has its own full-text search story; Polecat doesn't currently expose a LINQ extension over it.
- **Identity-mapped sessions as the default.** Lightweight is the default in Polecat; opt into `OpenSession(DocumentTracking.IdentityMap)` if you need it.
- **Some niche Marten extension methods.** Verify against the Polecat docs or source before assuming a method transfers — most do, a few don't. Compiled queries, batched queries, query plans, raw SQL, and JSON-path queries all exist; smaller helpers may not.

If Cab encounters a Marten feature missing from Polecat for a Payments-BC need, the answer is usually one of: (a) re-shape the operation to use what Polecat does support, (b) drop down to raw SQL via `IAdvancedSql`, or (c) reconsider whether the BC's data tier is the right call. Cab does not patch Polecat to add missing features — that's upstream JasperFx work.

---

## Cab use case: Payments BC documents

The Payments BC's document-store usage:

- **`MerchantConfig`** — per-merchant settings (tier, settlement schedule, supported currencies, payment-provider credentials reference). Read on every payment authorization. `IRevisioned` for audit history. Soft-deleted (compliance retention).
- **`PaymentSummary`** — read-side document projection. Updated by the inline `Snapshot<Payment>` registration documented in `polecat-event-sourcing` § Single-stream projections. Read by the GET `/api/payments/{id}` HTTP handler. Hard-deleted only when the underlying stream is tombstoned (rare).
- **`RefundRequest`** — transient state for the refund saga. Created when a refund is initiated, deleted when the saga completes. Hard-delete is correct here — the audit trail is on the event side (`RefundIssued` event), not the document side.
- **`PaymentLifecycleSaga`** — Wolverine saga state. Persisted via `IDocumentSession` per `wolverine-sagas` § Persistence. `Saga.Version` managed by the Wolverine saga framework — explicitly NOT `IRevisioned` (per the resolution in `polecat-event-sourcing`).

Typical patterns: patch `MerchantConfig` to bump review timestamps without reloading; LINQ-query `PaymentSummary` for recent-payment lists by merchant. Both follow the standard Wolverine handler shape from `wolverine-handlers` — handler accepts `IDocumentSession` (write) or `IQuerySession` (read), returns the data or void, the chain handles `SaveChangesAsync` via `AutoApplyTransactions`.

---

## Common pitfalls

- **Reaching for `LightweightSession()` thinking it's an opt-in.** In Polecat 3.0+, `store.LightweightSession()` is calling the default — you can also write `store.OpenSession()` and get the same shape. The opt-in is for the identity-map session via `OpenSession(DocumentTracking.IdentityMap)`. Don't mentally translate Marten 7 habits where lightweight was the explicit choice.

- **Calling `Insert` when you mean `Store`.** `Insert` throws if the document already exists. For "insert if new, update if existing," use `Store`. The asymmetry-throw semantic is part of the contract — don't rely on `Insert` to silently behave like an upsert.

- **Loading a document just to patch one property.** `session.Patch<T>(id).Set(...)` doesn't require loading first. It compiles to a single `UPDATE ... JSON_MODIFY(data, ...)` statement. The load-mutate-store pattern is fine for small documents but unnecessary for targeted updates.

- **Adding `IRevisioned` to a Wolverine saga.** Polecat's `IRevisioned` auto-detection applies to regular documents only. Wolverine sagas use `Saga.Version` managed by the saga persistence framework — adding `IRevisioned` doesn't activate revision-aware updates for sagas. (`polecat-event-sourcing` § The Polecat saga concurrency story has the full resolution.)

- **Configuring both `UseOptimisticConcurrency` and `UseNumericRevisions` on the same type.** They're mutually exclusive. Choose `IVersioned` or `IRevisioned` per document type and let auto-detection wire the right one. Manual configuration is for cases where the type can't implement the interface (e.g., third-party types).

- **Soft-deleting saga state documents.** Wolverine's `MarkCompleted()` lifecycle uses hard delete by default — that's correct. Configuring `[SoftDeleted]` on a saga type would leak completed sagas into the table forever. Sagas aren't an audit-history surface; the events the saga emitted are.

- **Writing PostgreSQL JSONB operators in raw SQL on Polecat.** The `pc_doc_*` tables use SQL Server's `json` (or `nvarchar(max)`) — query with `JSON_VALUE`, `JSON_QUERY`, `JSON_MODIFY`, `OPENJSON`. Marten habits pasting `data->>'status' = 'Active'` won't compile against SQL Server.

- **Forgetting the unit-of-work boundary.** Sessions accumulate operations until `SaveChangesAsync()`. Operations that look immediate (`session.Store(...)`, `session.Delete<T>(id)`) don't hit the database until the flush call. In a Wolverine handler, the chain calls `SaveChangesAsync` for you (via `AutoApplyTransactions`); outside of a chain, you call it yourself.

- **Querying soft-deleted documents and being surprised they're filtered.** Default queries hide soft-deleted documents — that's the feature. Use `MaybeDeleted()`, `IsDeleted()`, `DeletedSince`, or `DeletedBefore` extensions to surface them. If you find yourself routinely needing to see deleted documents, reconsider whether soft-delete is the right model for that type.

- **Patching with a where-clause expecting Polecat to fail loudly when no rows match.** Patching by predicate updates *zero or more* rows; zero matches is silent. If "must update at least one row" is part of the contract, query first and assert before patching.

- **String IDs without setting them.** Polecat won't auto-generate a string ID for you — that's the contract. If you `session.Store(new MyDoc())` with `MyDoc.Id` being `string` and unset, the call fails. Set the ID explicitly before storing.

- **Mixing event-store and document-store operations across multiple sessions.** A single `IDocumentSession` enrolls both event and document operations; `SaveChangesAsync()` flushes them atomically. Splitting events into one session and documents into another loses the atomicity guarantee. (`polecat-event-sourcing` § Wolverine integration covers this from the event-store angle.)

---

## See also

**Upstream** — load these first:

- `polecat-event-sourcing` — the engine baseline (SQL Server 2025 native `json` type, `pc_doc_{type}` table convention, `AddPolecat` + `IntegrateWithWolverine` bootstrap, Weasel.SqlServer schema management). The companion to this skill on the event-store side.
- `marten-querying` — the canonical LINQ patterns and decision frameworks. Most of marten-querying transfers to Polecat unchanged; the engine-specific differences are documented here.
- `service-bootstrap` § Polecat-Backed Service: The Differences — the per-service registration shape that wires the document store into the Wolverine handler context.

**Sibling skills:**

- `marten-aggregates` — document-shape conventions on the Marten side; the patterns transfer.
- `marten-async-daemon` — projection daemon mechanics; the concepts transfer (with the polling vs LISTEN/NOTIFY engine difference covered in `polecat-event-sourcing`).
- `wolverine-sagas` — saga state persistence via `IDocumentSession`. The saga-concurrency-via-`Saga.Version` story (NOT `IRevisioned`) connects through here.
- `wolverine-handlers` — handler-chain patterns that consume `IDocumentSession` and `IQuerySession` as parameters.

**Downstream:**

- `testing-integration` — integration-test fixtures for Polecat-backed services; document fixtures via `LightweightSession()`.
- `testing-advanced` (Phase 4) — multi-service scenarios involving Polecat-backed Payments and Marten-backed BCs interacting.
- `cli-jasperfx` — `db-apply` and `db-assert` for Polecat document and event schema; `storage counts` for document-table inspection.
- `observability-tracing` — Polecat OpenTelemetry options surface session and query traces; the same `opts.OpenTelemetry` configuration shapes apply.

**External:**

- [Polecat documentation — Documents section](https://polecat.jasperfx.net/documents/) — canonical reference.
- [Polecat sessions](https://polecat.jasperfx.net/documents/sessions) — session shapes, options, listeners, ejecting.
- [Polecat storing](https://polecat.jasperfx.net/documents/storing) — Store / Insert / Update / SaveChangesAsync semantics, identity strategies.
- [Polecat soft deletes](https://polecat.jasperfx.net/documents/deletes) — three opt-ins, LINQ extensions, restoration patterns.
- [Polecat patching](https://polecat.jasperfx.net/documents/partial-updates-patching) — the `Patch<T>` API and operations.
- [Polecat concurrency](https://polecat.jasperfx.net/documents/concurrency) — `IVersioned` vs `IRevisioned`, auto-detection, `JasperFx.ConcurrencyException`.
- ai-skills `polecat-document-store` and `polecat-event-sourcing` — generic Polecat patterns from JasperFx if/when published.
