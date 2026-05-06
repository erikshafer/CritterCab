---
name: polecat-event-sourcing
description: "Polecat 3.1+ as an event store and document database in CritterCab — the SQL-Server-2025-backed Critter Stack member that Cab uses for the Payments BC where compliance, audit, and the org-mandated SQL Server data tier outweigh PostgreSQL's defaults. Covers the v3.0 schema (pc_events, pc_streams without snapshot columns, pc_event_progression, per-document pc_doc_{type}, per-tag pc_event_tag_{tag}), event-store fundamentals (StreamIdentity, QuickAppend single-strategy, Append / StartStream / FetchForWriting / WriteToAggregate / AppendOptimistic / AppendExclusive), the v3.0 Snapshot<T>() shortcut as a SingleStreamProjection registration form (snapshots now persist to standard pc_doc_{type} tables, not special pc_streams columns), projection lifecycles (Inline / Live / Async), live aggregation (AggregateStreamAsync, FetchLatest), the Dynamic Consistency Boundary surface (Polecat.Events.Dcb namespace with EventTagQuery / IEventBoundary<T> / FetchForWritingByTags / DcbConcurrencyException / EventsExistAsync / RegisterTagType<TTag> on opts.Events), the polling-based async daemon and how it differs from Marten's LISTEN/NOTIFY model, subscriptions via SubscriptionBase, Weasel.SqlServer schema management with the JasperFx db-apply / db-assert CLI workflow, the Wolverine integration via PolecatOps factory (StartStream, Store, Insert, Update, Delete with tenant-scoped overloads) and IPolecatOp side-effect contract, the v3.1 IDocumentStoreUsageSource publishing for monitoring-tool discovery (CritterWatch, Wolverine ServiceCapabilities), and the resolved deferred verification from wolverine-sagas: Polecat sagas use Saga.Version handled by Wolverine's saga persistence framework, not Polecat's IRevisioned auto-detection (which applies to non-saga documents). Cab uses Polecat for Payments BC; default elsewhere remains Marten."
cluster: polecat
tags: [polecat, sql-server, event-sourcing, dcb, snapshot-projection, single-stream-projection, async-daemon, polling, weasel, polecat-ops, integratewithwolverine, payments-bc, v3-breaking-change]
---

# Polecat as an Event Store

Polecat is the SQL-Server-2025-backed member of the Critter Stack — a port of Marten's event-sourcing and document-database surface to SQL Server, in a single package. CritterCab uses Polecat for the **Payments bounded context** because that BC has organizational SQL-Server-only constraints (audit trail tooling, regulatory reporting, the security team's data-tier policies). Every other Cab BC defaults to Marten on PostgreSQL per the project vision; Polecat is the deliberate exception, not the default.

The single most useful framing: **Polecat is Marten with the engine swapped.** The API surface is intentionally near-identical — `IDocumentStore`, `IDocumentSession`, `IQuerySession`, `session.Events.StartStream<T>(...)`, `session.Events.AggregateStreamAsync<T>(...)`, projections, the async daemon, even the LINQ provider. Most patterns in `marten-aggregates`, `marten-wolverine-aggregates`, `marten-projections`, `marten-async-daemon`, and `dynamic-consistency-boundary` translate directly. This skill documents what's the same, what's different, and what's specifically v3.0 of Polecat that Cab is committed to.

The v3.0 headline change Cab cares about: **the `pc_streams` table no longer carries `snapshot` / `snapshot_version` columns.** Snapshots are now a registration shortcut over `SingleStreamProjection<T, TId>` — `opts.Projections.Snapshot<T>(SnapshotLifecycle.Inline)` builds and registers a single-stream projection internally, and the aggregate persists to the standard `pc_doc_{type}` document table just like any other projected document. There is no special "snapshot storage path" anymore. This brings Polecat in line with how Marten's `Snapshot<T>()` API works conceptually and removes a column-level coupling that existed in earlier Polecat releases.

Polecat differs from Marten in three places worth knowing up front: the engine is SQL Server 2025 with the native `json` column type (or `nvarchar(max)` for older instances via `UseNativeJsonType = false`), there's only **one append strategy** (QuickAppend — direct `INSERT` with `OUTPUT inserted.seq_id`; no `Inline` vs `Quick` choice), and the async daemon **polls** for new events rather than using PostgreSQL's `LISTEN/NOTIFY`. Schema management uses **Weasel.SqlServer** rather than Weasel.Postgresql; the JasperFx CLI commands (`db-apply`, `db-assert`) work identically.

This skill assumes `service-bootstrap` for the Aspire-injected configuration shape and `wolverine-sagas` for saga storage with the Polecat persistence frame provider. It assumes the reader has read at minimum `marten-aggregates` (the patterns transfer almost directly) and ideally `marten-projections` and `dynamic-consistency-boundary` (the parallels are exact except for the namespace).

---

## When to apply this skill

Use this skill when:

- Working in CritterCab's Payments BC (the only Polecat-backed BC at the time of writing).
- Adding a new Polecat-backed service to Cab where SQL Server is required.
- Designing event-sourcing patterns that need the Polecat-specific differences from Marten — projection lifecycle choices, async daemon polling configuration, Weasel.SqlServer schema management, or the v3.0 Snapshot<T> registration form.
- Implementing DCB on the Payments side (`Polecat.Events.Dcb` namespace) where multiple payment streams or related streams need cross-stream consistency.
- Wiring `PolecatOps` factory calls in Wolverine handlers — `StartStream`, `Store`, `Insert`, `Update`, `Delete`.
- Diagnosing the asymmetry between Polecat document `IRevisioned` (auto-detected) and Wolverine saga `Version` (managed by Wolverine's saga persistence framework).

Do NOT use this skill for:

- Marten patterns — `marten-aggregates`, `marten-wolverine-aggregates`, `marten-projections`, `marten-async-daemon`, `marten-querying`. The vast majority of Cab work uses Marten; Polecat is the Payments-BC-specific exception.
- Polecat's document-database (non-event-sourcing) surface — `polecat-document-store` (Phase 4) covers `IDocumentSession.Store/Load/Query`, LINQ querying, soft deletes, and patching.
- Saga implementation patterns — `wolverine-sagas`. This skill resolves the deferred Polecat-saga concurrency verification but doesn't re-derive the saga programming model.
- DCB pattern decisions — `dynamic-consistency-boundary` is the canonical pattern skill; this skill documents the Polecat namespace differences.
- Comparing Polecat to Marten as a database-platform decision — that decision is made at the BC level via the Cab vision document; this skill assumes the choice has been made.

---

## Polecat in CritterCab

Cab's posture, settled in the project vision and the corresponding ADR:

- **Default to Marten** for any new BC. PostgreSQL is the project's preferred data tier for event-sourcing-heavy contexts (Trips, Dispatch, Telemetry, Onboarding, Identity, Rider/Driver Profile, etc.). Marten gets the most attention in the Critter Stack and has the deepest tooling.
- **Use Polecat for Payments**. The Payments BC has SQL-Server-only constraints arising from audit and compliance tooling that the organization standardizes on. The data-residency, encryption, and audit-log policies are documented at the SQL-Server-instance level, and the cost of replicating them to a separate PostgreSQL instance is judged not worth the engineering effort. Polecat exists to make this kind of constraint compatible with Critter Stack patterns rather than forcing the BC into a different stack entirely.
- **No mixing within a single BC**. A Cab BC is either Marten-backed or Polecat-backed end-to-end. Don't introduce both into one service — the saga storage, document storage, and event store all live in the same database, and bridging across is more complexity than it's worth.

The v3.1 minimum is what Cab targets. The v3.0 PR's narrowly-scoped breaking change (pc_streams snapshot columns removed; Snapshot<T> restored as a SingleStreamProjection registration shortcut) brought Polecat's surface into alignment with Marten's, which lets Cab's skill library treat them as parallel rather than divergent. The v3.1 release adds `IDocumentStoreUsageSource` publishing on `IDocumentStore` for monitoring-tool discovery (see § Setup and bootstrap below) — purely additive, no breaking changes from v3.0.

---

## Setup and bootstrap

The Payments service `Program.cs` follows `service-bootstrap`'s shape with a Polecat substitution for Marten:

```csharp
// src/CritterCab.Payments/Program.cs (excerpt)
using Polecat;
using Wolverine.Polecat;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(); // Aspire

builder.Services.AddPolecat(opts =>
{
    opts.ConnectionString = builder.Configuration.GetConnectionString("payments")!;
    opts.DatabaseSchemaName = "payments";

    // SQL Server 2025 native json column. Set to false for nvarchar(max) on older instances.
    opts.UseNativeJsonType = true;

    // Stream identity — Guid is Cab's convention for Payments
    opts.Events.StreamIdentity = StreamIdentity.AsGuid;

    // Optional: opt into correlation/causation/headers metadata
    opts.Events.EnableCorrelationId = true;
    opts.Events.EnableCausationId = true;
}).IntegrateWithWolverine();

builder.Host.UseWolverine(opts =>
{
    opts.Policies.AutoApplyTransactions();
    // … per service-bootstrap
});
```

Two pieces matter:

- **`services.AddPolecat(...)`** registers `IDocumentStore`, `ISessionFactory`, scoped `IDocumentSession` and `IQuerySession`, and the configured `StoreOptions`. The single-argument-string form (`AddPolecat(connectionString)`) is also valid but Cab prefers the lambda form for explicitness about schema name and event-store options.
- **`.IntegrateWithWolverine()`** is the bridge — it registers the `PolecatPersistenceFrameProvider` for saga storage, wires up the SQL-Server-backed Wolverine message store (transactional outbox), enrolls Polecat documents and event streams into Wolverine's transactional middleware, and registers the `PolecatOps` codegen sources. Without this call, Wolverine handlers cannot use Polecat's `IDocumentSession` or emit `PolecatOps` side effects.

Convention: connection strings come from Aspire (`builder.Configuration.GetConnectionString("payments")` resolves the AppHost-injected SQL Server connection). Schema name matches the BC name in lowercase. `AutoCreateSchemaObjects` defaults to `CreateOrUpdate` for development; production environments should set `AutoCreate.None` and use the JasperFx CLI `db-apply` workflow instead (see § Schema management).

Lightweight sessions are the default in Polecat — unlike Marten 7-and-earlier where you opted into `LightweightSession()`. Cab doesn't override this; the lightweight default is the right choice for CQRS workflows.

### v3.1 monitoring discovery (purely additive)

Polecat 3.1 made `IDocumentStore` implement `IDocumentStoreUsageSource` (from `JasperFx.Events`), and the `AddPolecat` registration auto-bridges this so monitoring tools — **CritterWatch** and Wolverine's `ServiceCapabilities.readDocumentStores` — can discover the store via `services.GetServices<IDocumentStoreUsageSource>()`. The `DocumentStoreUsage` descriptor exposes database info, schema name, `AutoCreateSchemaObjects` policy, and per-document mappings. The parallel `EventStoreUsage` descriptor exposes the event-store configuration including registered DCB tag types via a typed `TagTypeDescriptor` list. No user-facing API change — purely tooling-discovery infrastructure for observability tools to introspect a running Polecat-backed service. The matching `EnableExtendedProgressionTracking` opt-in on `EventStoreOptions` adds the `heartbeat`, `agent_status`, `pause_reason`, and `running_on_node` columns to `pc_event_progression` for CritterWatch alerting. The full operational use of these surfaces is documented in `observability-metrics` (Phase 4).

---

## Core schema

Polecat creates these tables under the configured schema (e.g., `payments.pc_events`):

| Table | Purpose |
|---|---|
| `pc_events` | All events with sequence IDs, stream references, JSON event data, optional metadata columns |
| `pc_streams` | Stream metadata: stream id, version, type, timestamps. **As of v3.0, no snapshot columns** |
| `pc_event_progression` | Async daemon progress per projection or subscription (last sequence processed) |
| `pc_doc_{type}` | One per document type, including projection target documents and the v3.0 `Snapshot<T>()` aggregates |
| `pc_event_tag_{tag}` | One per registered DCB tag type, composite PK `(value, seq_id)`, FK to `pc_events.seq_id` with cascade-delete |

The `pc_` prefix is fixed (Polecat uses it the way Marten uses `mt_`). The schema name comes from `opts.DatabaseSchemaName`.

Stream identity is configured on `opts.Events.StreamIdentity`:

- `StreamIdentity.AsGuid` — `pc_streams.id` is `uniqueidentifier`. Cab's Payments default.
- `StreamIdentity.AsString` — `pc_streams.id` is `nvarchar(...)`. Use when a natural-key string identifier exists for the aggregate.

Cab uses `AsGuid` for Payments because payment-request IDs are UUIDs anyway and the integer space gives no advantage at Payments' volume.

---

## Appending events

The write-side surface lives on `session.Events` (typed as `IEventOperations`). The most-used overloads:

```csharp
// Start a brand-new stream (throws if it already exists)
session.Events.StartStream<Payment>(paymentId, new PaymentRequested(...));
session.Events.StartStream<Payment>(streamKey, new PaymentRequested(...));     // string-keyed stream
session.Events.StartStream<Payment>(new PaymentRequested(...));                  // auto-generated Guid

// Append to an existing stream (creates it if missing — beware of this for new streams)
session.Events.Append(streamId, new PaymentAuthorized(...));

// Append with explicit expected version (optimistic concurrency at append time)
session.Events.Append(streamId, expectedVersion: 3, new PaymentCaptured(...));

// Optimistic-concurrency variant: reads the current version, sets ExpectedVersionOnServer
await session.Events.AppendOptimistic(streamId, ct, new PaymentCaptured(...));

// Pessimistic-locking variant: opens a transaction and holds an exclusive row lock
await session.Events.AppendExclusive(streamId, ct, new PaymentCaptured(...));

// Mark a stream archived (soft delete; pc_events rows remain but the stream is hidden)
session.Events.ArchiveStream(streamId);

// Tombstone (hard DELETE — events gone permanently)
session.Events.TombstoneStream(streamId);

// All operations are queued; nothing hits the database until SaveChangesAsync
await session.SaveChangesAsync();
```

Two append helpers earn explicit calls because they're easy to confuse:

- **`Append`** unconditionally appends. If the expected version is wrong, `ConcurrencyException` is thrown at `SaveChangesAsync` time; if the stream doesn't exist, it's created. Use when concurrency is enforced elsewhere (DCB, exclusive lock, or you genuinely have first-writer-wins semantics).
- **`AppendOptimistic`** reads the current version first to set `ExpectedVersionOnServer`. Throws `NonExistentStreamException` if the stream doesn't exist; throws `ConcurrencyException` on version mismatch. This is the common-case "append to existing stream with conflict detection" pattern.

Cab convention for Payments: use `StartStream<Payment>` for the first event of a new payment, `AppendOptimistic` for subsequent events, and reserve `Append(streamId, expectedVersion, ...)` for cases where the version is known from a prior `FetchForWriting` load.

---

## FetchForWriting and WriteToAggregate

For the load-decide-append cycle, prefer the higher-level helpers:

```csharp
// Fetch the projected aggregate state and return a writable handle
var stream = await session.Events.FetchForWriting<Payment>(paymentId);
var payment = stream.Aggregate;     // current state, or null if stream doesn't exist
stream.AppendOne(new PaymentCaptured(amount, capturedAt));
await session.SaveChangesAsync();   // optimistic concurrency check at save time

// One-shot variant — fetch, mutate, save
await session.Events.WriteToAggregate<Payment>(paymentId, stream =>
{
    var payment = stream.Aggregate ?? throw new InvalidOperationException();
    if (payment.Status == PaymentStatus.Pending)
    {
        stream.AppendOne(new PaymentAuthorized(authCode, DateTimeOffset.UtcNow));
    }
});

// Pessimistic variant — exclusive lock on the stream row until SaveChangesAsync
var stream = await session.Events.FetchForExclusiveWriting<Payment>(paymentId);
```

The aggregate type passed to `FetchForWriting<T>` must be self-aggregating — the same conventions as Marten and the same shape as a `SingleStreamProjection<T, TId>`. Either `T` has a static `Create(SomeEvent) → T` method plus instance `Apply(SomeOtherEvent)` methods, or a custom `SingleStreamProjection<T, TId>` is registered.

`FetchLatest<T>(id)` is the read-only counterpart — projects the aggregate without returning a writable handle. Useful inside query handlers.

---

## Projection lifecycles

Polecat supports three projection lifecycles, identical in concept to Marten:

| Lifecycle | When applied | Use case |
|---|---|---|
| **Inline** | Same transaction as the event append | Strong consistency: read-after-write reads see the projection |
| **Live** | On-demand, every read replays from `pc_events` | Always-current; no projection storage needed; cost is read latency |
| **Async** | Background daemon picks up appended events and updates the projection table | Eventually consistent; scales better; needs daemon to be running |

Cab convention for Payments:

- **Inline** for the canonical `Payment` summary aggregate via the v3.0 `Snapshot<T>()` shortcut. Read-after-write of "what's the current state of this payment?" must be reliable.
- **Async** for cross-payment dashboards (e.g., daily settlement totals, per-merchant aggregations). These are eventually consistent and high-volume.
- **Live** rarely. Cab uses Live only for low-frequency, high-cardinality read patterns where storing a projection isn't worthwhile — currently zero in Payments.

---

## Single-stream projections and the v3.0 `Snapshot<T>()` shortcut

The `Snapshot<T>()` API is Polecat 3.0's headline feature for Cab: a registration shortcut that builds a `SingleStreamProjection<T, TId>` internally and persists the aggregate to the standard `pc_doc_{type}` document table.

```csharp
// In AddPolecat configuration
opts.Projections.Snapshot<Payment>(SnapshotLifecycle.Inline);

// Self-aggregating Payment type with conventional Create/Apply
public class Payment
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CapturedAt { get; set; }

    public static Payment Create(PaymentRequested e) => new()
    {
        Id = e.PaymentId,
        Amount = e.Amount,
        Currency = e.Currency,
        Status = PaymentStatus.Pending,
        CreatedAt = e.RequestedAt,
    };

    public void Apply(PaymentAuthorized e) => Status = PaymentStatus.Authorized;
    public void Apply(PaymentCaptured e)
    {
        Status = PaymentStatus.Captured;
        CapturedAt = e.CapturedAt;
    }
}
```

Three v3.0-specific notes:

- **The aggregate is a regular projected document.** `await session.LoadAsync<Payment>(paymentId)` returns the projection from `pc_doc_payment` — no special "snapshot" code path. This makes Cab's read-side handlers identical to non-event-sourced document loads.
- **`SnapshotLifecycle.Live` is intentionally not supported.** For live aggregation, call `session.Events.AggregateStreamAsync<Payment>(paymentId)` directly without registering a `Snapshot<T>` projection.
- **Composite projections support `composite.Snapshot<T>()`.** Inside a composite, snapshots run as Async (composites are async-only).

Use `opts.Projections.Add<TProjection>(ProjectionLifecycle.Inline)` instead of `Snapshot<T>()` when you need a custom `SingleStreamProjection<T, TId>` subclass that overrides projection behavior, or any other projection type (multi-stream, event projections, flat tables).

---

## Multi-stream and other projection types

Polecat ships the same projection-type vocabulary as Marten:

- **`SingleStreamProjection<T, TId>`** — one projection document per stream. The `Snapshot<T>()` shortcut covers the common case.
- **`MultiStreamProjection<TDoc, TId>`** — one projection document derived from events across multiple streams. Use for read models where the document key isn't a stream id (e.g., per-merchant settlement summary spanning many payment streams).
- **`EventProjection`** — imperative projection where you write `Project(...)` and `Operations` directly. Useful when the projection's logic doesn't fit a per-document apply pattern (e.g., recording an audit trail in a flat table).
- **Flat-table projections** — when the projection target is a relational table rather than a JSON document. Use when SQL queries from external tooling need straightforward column access.

Cab's Payments uses `SingleStreamProjection` (via `Snapshot<Payment>()`) for the per-payment summary, plus an `EventProjection` writing to a flat `payment_audit` table for compliance reporting.

For full coverage of these projection types, see `marten-projections` — the API shape is identical except for the `Polecat.Projections` namespace.

---

## Live aggregation

`AggregateStreamAsync<T>` replays events from `pc_events` on every call:

```csharp
// Inside a query handler
public async Task<Payment?> Handle(GetPayment query, IQuerySession session)
{
    return await session.Events.AggregateStreamAsync<Payment>(query.PaymentId);
}

// FetchLatest is functionally identical for read-only callers
var payment = await session.Events.FetchLatest<Payment>(paymentId);

// String-keyed stream variant
var payment = await session.Events.FetchLatest<Payment>(streamKey);
```

Live aggregation always reads the latest events (no daemon lag) at the cost of replaying. Cab uses live aggregation only for paths where the projection isn't worth maintaining — currently uncommon in Payments.

---

## Dynamic Consistency Boundary (DCB)

DCB is for cross-stream consistency: a command's invariants span multiple event streams selected by tag query, enforced atomically. The mechanics mirror Marten's almost exactly. The differences are namespace and the v3.0-specific note that Polecat uses a single append strategy.

### Registering tag types

Tag types are strong-typed identifiers — record types wrapping a primitive value (`Guid`, `string`, `int`, `long`, or `short`). Register during store configuration:

```csharp
public record PaymentRequestId(Guid Value);
public record MerchantId(Guid Value);

builder.Services.AddPolecat(opts =>
{
    opts.ConnectionString = "...";
    opts.Events.RegisterTagType<PaymentRequestId>();
    opts.Events.RegisterTagType<MerchantId>();
});
```

Each registered tag type produces a `pc_event_tag_{tag}` table with composite PK `(value, seq_id)` and a foreign key to `pc_events.seq_id` with cascade delete.

### Tagging events

Use `IEventOperations.BuildEvent` plus `WithTag` to attach tags before appending:

```csharp
var @event = session.Events
    .BuildEvent(new PaymentCaptured(paymentId, amount, capturedAt))
    .WithTag(new PaymentRequestId(paymentId))
    .WithTag(new MerchantId(merchantId));

session.Events.Append(streamId, @event);
```

Events can carry multiple tags of different types. Tags persist to their per-tag tables in the same transaction as the event.

### Querying events by tags

```csharp
using Polecat.Events.Dcb;

var query = EventTagQuery
    .For(new PaymentRequestId(paymentId))
    .AndEventsOfType<PaymentCaptured, PaymentFailed>();

var events = await session.Events.QueryByTagsAsync(query);

// Aggregate matching events into a projection
var summary = await session.Events.AggregateByTagsAsync<PaymentSummary>(query);

// Lightweight existence check — no event materialization
var exists = await session.Events.EventsExistAsync(query);
```

Events are returned ordered by sequence number (global append order). `AggregateByTagsAsync` returns `null` if no matching events are found.

### FetchForWritingByTags and IEventBoundary

For DCB write semantics — load matching events, return a writable boundary, enforce consistency at save time:

```csharp
var boundary = await session.Events.FetchForWritingByTags<PaymentSummary>(query);
var summary = boundary.Aggregate;     // current view, or null

// Enforce business invariants here
if (summary?.OutstandingHolds > 0) throw new InvalidOperationException(...);

// AppendOne / AppendMany — events must have tags set via WithTag
boundary.AppendOne(
    session.Events.BuildEvent(new HoldReleased(paymentId))
        .WithTag(new PaymentRequestId(paymentId))
);

await session.SaveChangesAsync();
// At save time, Polecat runs an EXISTS check for new matching events with seq_id > LastSeenSequence.
// If matched, throws DcbConcurrencyException.
```

`IEventBoundary<T>` exposes:

- `Aggregate` — current state from the matching events.
- `LastSeenSequence` — highest sequence seen at fetch time; the consistency check threshold.
- `Events` — the matching event list (`IReadOnlyList<IEvent>`).
- `AppendOne(object)`, `AppendMany(params object[])`, `AppendMany(IEnumerable<object>)` — all events must be pre-tagged via `WithTag` or `BuildEvent`.

### DcbConcurrencyException

Thrown when new events matching the same tag query were appended after the boundary was established:

```csharp
namespace Polecat.Events.Dcb;

public class DcbConcurrencyException : ConcurrencyException
{
    public DcbConcurrencyException(EventTagQuery query, long lastSeenSequence) { ... }
    public EventTagQuery Query { get; }
    public long LastSeenSequence { get; }
}
```

Inherits from `JasperFx.ConcurrencyException` (the Critter Stack-wide concurrency type), not from `Marten.ConcurrencyException`. Cab's `chain.OnException<DcbConcurrencyException>().RetryWithCooldown(...)` retry policy is the standard pattern, mirroring the equivalent for Marten's `ConcurrencyException` documented in `marten-wolverine-aggregates` and `wolverine-sagas`.

### Tag routing

Events appended via `IEventBoundary.AppendOne()` are automatically routed to streams based on their tags — each tag value becomes the stream identity. Events with the same tag value land in the same stream.

---

## Async daemon

The daemon processes async projections and subscriptions. Two architectural notes that matter for Cab:

- **Polecat polls; Marten uses LISTEN/NOTIFY.** PostgreSQL has a native pub/sub primitive Marten leverages for sub-second projection latency. SQL Server doesn't, so Polecat polls `pc_events` for new sequence IDs at a configurable interval (default 500ms). For Payments BC's volume this is fine; for ultra-low-latency projection requirements, Marten on PostgreSQL would be a better fit — but that decision is upstream of this skill.
- **High-water-mark detection uses SQL Server's `LEAD()` window function** to detect sequence gaps. This prevents the daemon from processing events out of order when concurrent writers create gaps in `pc_events.seq_id`.

Configuration on `StoreOptions`:

```csharp
opts.DaemonSettings.StaleSequenceThreshold = 1000;
```

The daemon starts automatically when the .NET host starts (provided async projections or subscriptions are registered).

### Waiting for non-stale data

Two patterns for tests and rare runtime read-after-write needs:

```csharp
// Block until all projections catch up to the current high water mark
await store.WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(30));

// Or per-query: defer the LINQ query until projections have caught up
var summary = await session.Query<PaymentSummary>()
    .QueryForNonStaleData()
    .Where(x => x.Status == "Captured")
    .ToListAsync();
```

Cab uses `WaitForNonStaleProjectionDataAsync` extensively in `testing-integration` patterns (saga timeout tests against Polecat-backed services).

---

## Subscriptions

Subscriptions process events for **side effects** — sending notifications, calling external systems, triggering workflows. Distinct from projections (which build idempotent read models).

```csharp
public class PaymentNotificationSubscription : SubscriptionBase
{
    public override async Task ProcessEventsAsync(
        EventRange page,
        ISubscriptionController controller,
        IDocumentOperations operations,
        CancellationToken ct)
    {
        foreach (var evt in page.Events)
        {
            if (evt.Data is PaymentCaptured captured)
            {
                await SendReceiptEmail(captured);
            }
        }
    }
}

// Registration
opts.Projections.Subscribe(new PaymentNotificationSubscription());
```

Subscriptions run sequentially per registration, with progression tracked in `pc_event_progression`. Unlike projections, subscriptions are **not expected to be idempotent or replayable** — re-running them would cause duplicate side effects.

### Wolverine integration for event publication

Two Wolverine-specific options on the `PolecatIntegration` extension expose how events flow through Wolverine:

- **`UseFastEventForwarding`** — events are published through Wolverine's messaging infrastructure on `SaveChangesAsync`. No ordering guarantees, but distributes faster than ordered subscriptions. Default `false`.
- **`UseWolverineManagedEventSubscriptionDistribution`** — Wolverine's agent framework distributes async projection and subscription processing across nodes in a cluster. Default `false`.

```csharp
.IntegrateWithWolverine(integration =>
{
    integration.UseFastEventForwarding = true;
    integration.UseWolverineManagedEventSubscriptionDistribution = true;
});
```

For Cab Payments BC: `UseFastEventForwarding = false` (compliance benefits from strict event ordering), `UseWolverineManagedEventSubscriptionDistribution = true` if Payments runs multi-node.

---

## Schema management with Weasel.SqlServer

Polecat delegates all DDL to **Weasel.SqlServer** (sibling of Weasel.Postgresql for Marten). The schema lifecycle is diff-based — Weasel compares the desired schema (computed from configuration) against the actual database, generates DDL for differences, and applies it transactionally.

Behavior depends on `opts.AutoCreateSchemaObjects`:

- **`AutoCreate.CreateOrUpdate`** (default) — auto-creates new tables, adds new columns. **Never drops anything.**
- **`AutoCreate.None`** — no runtime schema changes. Use the JasperFx CLI for production deployments.

The `pc_streams` v3.0 schema change (removed `snapshot`/`snapshot_version` columns) is one Weasel diff applies as part of an upgrade — but only for schemas where Polecat owns the migration. Cab's convention is to target v3.0 from the start in Payments, so the migration is moot for Cab specifically.

### CLI workflow

Per `cli-jasperfx`, the JasperFx CLI commands operate identically on Polecat and Marten stores:

- **`db-apply`** — apply all configured schema changes against the configured store(s). Production deployment workflow: deploy code → run `db-apply` → start the service.
- **`db-assert`** — exit non-zero if the database doesn't match the configured schema. Production health-check workflow: run after deployment to verify the schema was applied correctly.

Both commands compose with multi-database setups (multiple Polecat stores, mixed Marten + Polecat) by walking every registered `IDocumentStore` in the host.

For local development, Cab leans on `AutoCreate.CreateOrUpdate` plus the Aspire-managed SQL Server container; for staging/production, `AutoCreate.None` plus the explicit `db-apply` step is the convention.

---

## Wolverine integration

Polecat's Wolverine integration is the bridge between Wolverine handlers and Polecat's `IDocumentSession`. The bootstrap call `.IntegrateWithWolverine()` registers the codegen sources, the persistence frame provider, and the SQL Server-backed message store.

### `PolecatOps` factory

Wolverine handlers return Polecat side effects via the `PolecatOps` static factory — mirroring `MartenOps` for the Marten side. The methods produce `IPolecatOp` values that Wolverine's codegen apply to the chain's `IDocumentSession`:

```csharp
using Wolverine.Polecat;

public static (PaymentCreatedResponse, IStartStream) Handle(
    RequestPayment cmd,
    IPaymentValidator validator)
{
    validator.Validate(cmd);

    var requested = new PaymentRequested(cmd.PaymentId, cmd.Amount, cmd.Currency, DateTimeOffset.UtcNow);
    var start = PolecatOps.StartStream<Payment>(cmd.PaymentId, requested);

    var response = new PaymentCreatedResponse(start.StreamId);
    return (response, start);
}
```

The factory exposes:

- **`PolecatOps.StartStream<T>(streamId, params events)`** / **`StartStream<T>(streamKey, params events)`** / **`StartStream<T>(params events)`** (auto-generated Guid).
- **`PolecatOps.Store<T>(doc)`**, **`Insert<T>(doc)`**, **`Update<T>(doc)`**, **`Delete<T>(doc)`**.
- **`PolecatOps.StoreMany<T>(...)`**, **`StoreObjects(params object[] documents)`** for mixed-type batches.
- **`PolecatOps.DeleteWhere<T>(expression)`**, **`Delete<T>(id)`** with overloads for `Guid`/`int`/`long`/`string` IDs.
- Tenant-scoped overloads of every operation accepting an explicit `tenantId` parameter (for the conjoined-tenancy and database-per-tenant scenarios).
- **`PolecatOps.Nothing()`** — returns a `NoOp` for chains that want to declare a side-effect return type but skip the operation conditionally.

The factory mirrors Marten's `MartenOps` near-1:1; switching a handler from one to the other is a using-directive change plus the factory namespace. Note: Cab's strong convention is to use `IStartStream` (the interface) as the return type rather than `StartStream<T>` (the concrete type) — this matches the documented Wolverine pattern and lets the test side mock the operation without binding to the concrete type.

### Aggregate handlers

`[WriteAggregate]` works against Polecat the same way it works against Marten — see `marten-wolverine-aggregates` for the canonical pattern. Wolverine's codegen detects Polecat's `IDocumentSession`-backed persistence via `PolecatPersistenceFrameProvider` and emits the same load-decide-append shape with `FetchForWriting<T>` under the hood. The aggregate type's `Create`/`Apply` conventions are identical.

### Saga storage

Saga state documents persist via Polecat's `IDocumentSession`, with the `PolecatPersistenceFrameProvider` handling the load/save/delete frames per `wolverine-sagas` § Persistence. The frame provider determines the saga ID type by inspecting the saga's `Id` property type directly — distinct from Marten's path through the document-type registry.

### The Polecat saga concurrency story (resolves the wolverine-sagas deferred verification)

`wolverine-sagas` § Concurrency deferred the question of whether `IRevisioned` is the opt-in for Polecat-backed sagas the way it is for Marten-backed sagas. The verified answer:

**It is not.** Polecat's `DocumentMapping` auto-detects `IRevisioned` types and applies revision-based optimistic concurrency for **non-saga documents** (verified via `Polecat.Tests.Versioning.revisioned_operations` — `session.Insert(doc)` increments `Version`; concurrent `session.Store(doc)` with stale version throws `JasperFx.ConcurrencyException`). For **Wolverine sagas specifically**, a comment in the Polecat-Wolverine integration reads: *"Wolverine's Saga type uses Version property which is handled by the saga persistence framework."* The `PolecatPersistenceFrameProvider.DetermineUpdateFrame` emits a regular `IDocumentSession.Update` call (not `UpdateRevision`), and the Version property on `Wolverine.Saga` is managed by the Wolverine saga chain — not by Polecat's `IRevisioned` auto-detection.

The practical consequence for Cab Payments BC sagas:

- **Don't** add `Marten.Metadata.IRevisioned` (or any `IRevisioned`) to a Polecat-backed saga class. The interface isn't part of Polecat's saga path; adding it has no effect at best and confuses readers at worst.
- **Do** declare `public int Version { get; set; }` (no `new` — the saga's Version IS the saga base class's Version). The Wolverine saga framework increments and checks it on every chain.
- **Do** use `chain.OnException<JasperFx.ConcurrencyException>().RetryWithCooldown(...)` to retry on conflict — same as for Marten-backed sagas, same exception type because Polecat throws the JasperFx-namespaced base class.

For non-saga documents in the Payments BC (e.g., a `MerchantConfig` document), `IRevisioned` from `JasperFx.Events` (or the equivalent Polecat metadata namespace) IS the right opt-in for revision tracking — the auto-detection in `DocumentMapping` activates revision-aware updates and throws on conflict. The asymmetry is deliberate: Wolverine sagas have their own version-management contract that predates and is independent of Polecat's document-revision model.

### EventForwarding for cross-BC publication

The `PolecatIntegration` extension exposes `SubscribeToEvent<T>()` for declaring transformations that publish Polecat events as Wolverine messages to other BCs:

```csharp
.IntegrateWithWolverine(integration =>
{
    integration.SubscribeToEvent<PaymentCaptured>()
        .TransformedTo(@event => new PaymentSettledIntegrationEvent(@event.Data.PaymentId, @event.Data.Amount));
});
```

This registers a transformation that wraps `IEvent<PaymentCaptured>` into the integration message Cab's Trips BC subscribes to over ASB. The transformation runs in the same outbox transaction as the Polecat event append, so cross-BC publication is atomic with the event commit.

---

## Cab use case: Payments BC

The Payments BC's primary stream is the `Payment` aggregate, identified by Guid. The event flow:

1. **`PaymentRequested`** — starts the stream (`PolecatOps.StartStream<Payment>` from a `RequestPayment` command handler).
2. **`PaymentAuthorized`** or **`PaymentRejected`** — emitted by an authorization handler; appends to the stream.
3. **`PaymentCaptured`** — emitted by a capture handler after the authorization succeeds and the merchant completes the order.
4. **`PaymentSettled`** — emitted by a settlement subscription processing batch settlement files from the payment provider.
5. **`RefundIssued`** — emitted by a refund handler (separate from the original payment stream's flow but tagged with the same `PaymentRequestId`).

Projections:

- **`Payment` (Inline `Snapshot<T>`)** — the canonical per-payment view; written to `pc_doc_payment`. Read by every payment-status query.
- **`MerchantSettlement` (Async `MultiStreamProjection`)** — daily settlement totals per merchant; aggregates events across many payment streams via `MerchantId` correlation.
- **`PaymentAudit` (Async `EventProjection` to flat table)** — every event row written to `payment_audit` for compliance queries from external SQL tooling.

DCB usage:

- Tag types: `PaymentRequestId`, `MerchantId`. Registered at startup via `opts.Events.RegisterTagType<TTag>()`.
- Cross-stream invariant example: when issuing a refund, the saga must verify no concurrent refund or chargeback has been issued against the same payment — `FetchForWritingByTags<PaymentRefundView>(EventTagQuery.For(new PaymentRequestId(id)).AndEventsOfType<RefundIssued, ChargebackReceived>())` enforces consistency at save time, throwing `DcbConcurrencyException` on race.

Subscriptions:

- **`PaymentNotificationSubscription`** — sends receipt emails on `PaymentCaptured`. Side effect; not idempotent.
- **`SettlementBatchSubscription`** — processes `PaymentSettled` events into the external accounting system.

The Payments BC also runs a Wolverine saga (`PaymentLifecycleSaga`) that orchestrates the payment lifecycle, including the cross-BC `TripCompletionSaga` interaction documented in `wolverine-sagas` § Cab use cases. The Polecat-saga concurrency story above applies to that saga.

---

## Common pitfalls

- **Reaching for `IRevisioned` on a Polecat-backed Wolverine saga.** The `IRevisioned` opt-in is for non-saga documents; sagas use the `Saga.Version` property managed by the Wolverine saga persistence framework. Adding `IRevisioned` to a saga doesn't activate revision-aware updates and signals to readers that you've conflated two different concurrency contracts.

- **Forgetting `IntegrateWithWolverine()` after `AddPolecat`.** Without the integration call, Wolverine handlers can't use Polecat's `IDocumentSession` or emit `PolecatOps` side effects. The symptom is "the chain throws because no `IDocumentSession` is registered for this handler." `service-bootstrap` § Polecat-Backed Service: The Differences covers the registration requirement.

- **Using `MartenOps.StartStream` against a Polecat-backed service.** The factories aren't interchangeable — `MartenOps` returns `IMartenOp` side effects that target Marten's `IDocumentSession`. Importing `Wolverine.Polecat` and using `PolecatOps.StartStream<T>` is the correct pattern; the codegen recognizes `IPolecatOp` and applies it to Polecat's session. (`wolverine-handlers` § Anti-Pattern lists this explicitly.)

- **Assuming `pc_streams` carries the snapshot.** As of v3.0, snapshots persist to standard `pc_doc_{type}` document tables. SQL queries that read from `pc_streams.snapshot` (legacy of pre-3.0 Polecat) won't work. The migration is to `Snapshot<T>(SnapshotLifecycle.Inline)` registration plus `session.LoadAsync<T>(streamId)` reads — same surface as any other projected document.

- **Treating `Append` as if it implies optimistic concurrency.** It doesn't — bare `Append(streamId, ...)` will create the stream if missing and append unconditionally. For load-decide-append loops, use `AppendOptimistic` (reads the current version first) or capture the version from a prior `FetchForWriting`.

- **`SnapshotLifecycle.Live` instead of `AggregateStreamAsync` for live aggregation.** Live is intentionally not a `Snapshot<T>` lifecycle. For on-demand aggregation, call `session.Events.AggregateStreamAsync<T>(streamId)` directly without registering a `Snapshot<T>` projection. Trying `opts.Projections.Snapshot<T>(SnapshotLifecycle.Live)` will fail.

- **Inline projection without `opts.Policies.AutoApplyTransactions()`.** Inline projections run in the same transaction as the event append, but only if Wolverine's chain has the `SaveChangesAsync` postprocessor wired. Without `AutoApplyTransactions`, the chain has no save call and the projection is silently lost. (`wolverine-sagas` § Persistence covers this for sagas; the same requirement applies to inline projections.)

- **Polling daemon misinterpreted as "broken" because it's slower than Marten's LISTEN/NOTIFY.** The default 500ms polling interval is by design — SQL Server doesn't have a native pub/sub primitive. Tuning `DaemonSettings` is acceptable; switching to PostgreSQL because "polling is bad" is solving a problem at the wrong layer. The decision to use Polecat is upstream of this skill and assumes the polling tradeoff is acceptable.

- **Subscriptions that try to be idempotent.** Subscriptions are designed for side effects; replaying them by design causes duplicate effects. If a workflow needs idempotent replay, model it as a projection (writing to `pc_doc_*`) and downstream consumers query the projection. Don't build "idempotent subscriptions" — that's a projection wearing the wrong shape.

- **Mixing `PolecatOps.StartStream` and `session.Events.StartStream` in the same handler.** Both work, but they have different lifecycle semantics. `PolecatOps.StartStream` is an `IPolecatOp` side effect Wolverine applies to the session at codegen-emitted call sites. `session.Events.StartStream` is direct invocation. Cab convention: use `PolecatOps` for handler return values (the standard pattern); use `session.Events` directly only inside imperative blocks where the handler's signature isn't a tuple-return.

- **Forgetting that Polecat documents and event streams share the same `IDocumentSession`.** The session is a single unit-of-work — operations against documents and events both flush on `SaveChangesAsync`. This is the same as Marten and is exactly what makes the outbox guarantee work, but it bites when developers expect "document store" and "event store" to be separate session contexts.

- **Setting `UseNativeJsonType = true` against pre-2025 SQL Server.** The native `json` column type is a SQL Server 2025 feature. For older instances, set `opts.UseNativeJsonType = false` to fall back to `nvarchar(max)`. Cab targets SQL Server 2025 in Aspire-managed local development and in deployed environments, so the default `true` is correct — but if a reader inherits a pre-2025 instance, the option matters.

- **Auto-create schema in production.** `AutoCreate.CreateOrUpdate` is wonderful for development and a footgun in production — schema changes happen at startup, on every deploy, transactionally. For deployed environments, set `AutoCreate.None` and use the JasperFx CLI `db-apply` workflow (per `cli-jasperfx`). The migration is then explicit, reviewable, and rollback-able.

- **Cross-store `IntegrateWithWolverine` confusion in mixed Marten + Polecat solutions.** The extension method exists on both `MartenConfigurationExpression` and `PolecatConfigurationExpression`. In a service that registers BOTH (rare in Cab, but the saga-storage path supports it), call `.IntegrateWithWolverine()` on whichever is the **primary** store for that service — Wolverine's saga storage and message storage need exactly one home. Cab's convention: a service is either Marten-backed or Polecat-backed, never both. The extension method's existence on both expressions doesn't license dual-store services within a single BC.

---

## See also

**Upstream** — load these first:

- `service-bootstrap` — the composition root pattern this skill builds on. § Polecat-Backed Service: The Differences covers the per-service `AddPolecat` + `IntegrateWithWolverine` registration.
- `marten-aggregates` — aggregate-shape conventions; the patterns transfer almost directly. The differences are the Polecat namespaces and the v3.0 Snapshot<T> shortcut.
- `marten-wolverine-aggregates` — `[WriteAggregate]` and `MartenOps` patterns; reads as a parallel template for `PolecatOps`.
- `dynamic-consistency-boundary` — the canonical DCB pattern skill; this skill documents only the Polecat namespace differences. Mental model, decision framework, and pitfalls live there.
- `wolverine-sagas` — saga base class, handler-method conventions, persistence wiring. This skill resolves the deferred verification at L300/L448 of that skill (Polecat sagas use `Saga.Version`, not `IRevisioned`).

**Sibling skills:**

- `polecat-document-store` (Phase 4) — Polecat's document-database surface (`Store`, `Load`, `Query`, LINQ, soft deletes, patching). Reading this skill plus the document-store skill covers the full Polecat surface Cab uses.
- `marten-projections` — the projection-type vocabulary (single-stream, multi-stream, event projections, flat tables) carries directly to Polecat. The shape and lifecycle decisions are identical.
- `marten-async-daemon` — the daemon-architecture concepts (high water mark, event loader, batch processing, progression tracking) carry directly. The mechanism difference (polling vs LISTEN/NOTIFY) is documented here; the operational patterns are the same.
- `wolverine-handlers` — `PolecatOps.StartStream` is documented there as the Polecat-backed alternative to `MartenOps.StartStream`.
- `cli-jasperfx` — `db-apply`, `db-assert`, `describe`, `storage counts` operate identically on Polecat and Marten stores.

**Downstream:**

- `testing-integration` — saga timeout tests against Polecat-backed services use `WaitForNonStaleProjectionDataAsync` extensively. The Polecat-side fixture pattern parallels the Marten one.
- `testing-advanced` (Phase 4) — multi-saga and cross-BC scenarios involving Polecat-backed Payments interacting with Marten-backed Trips.
- `observability-tracing` — Polecat's OpenTelemetry options (`opts.OpenTelemetry`) emit traces that span the same way Marten's do; cross-BC traces flow through both transparently.
- `observability-metrics` (Phase 4) — Polecat metrics (Polecat meter), session counters, projection-daemon health metrics.
- `identity-acl` — auth-context propagation through Polecat-backed services, including the conjoined and database-per-tenant tenancy paths.

**External:**

- [Polecat documentation](https://polecat.jasperfx.net/) — the canonical reference. The v3.0 changes are reflected in the events/snapshots and events/dcb pages.
- [Polecat: SQL-Server-Backed Event Store (GitHub)](https://github.com/JasperFx/polecat) — repo, samples, issue tracker.
- [Wolverine + Polecat tutorial](https://wolverinefx.net/tutorials/cqrs-with-polecat) — end-to-end CQRS example pairing the two; the Cab pattern lineage.
- [Polecat events overview](https://polecat.jasperfx.net/events/) — the v3.0-current event-store surface.
- [Polecat DCB documentation](https://polecat.jasperfx.net/events/dcb) — tag types, EventTagQuery, IEventBoundary semantics.
- [Polecat snapshots documentation](https://polecat.jasperfx.net/events/snapshots) — the v3.0 Snapshot<T> shortcut, lifecycle options, the under-the-hood SingleStreamProjection registration.
- [Polecat async daemon documentation](https://polecat.jasperfx.net/events/projections/async-daemon) — polling architecture, high water mark detection, daemon settings.
- [Polecat schema migrations](https://polecat.jasperfx.net/schema/migrations) — Weasel.SqlServer integration, AutoCreate behavior.
- ai-skills `polecat-event-sourcing` and `polecat-document-store` — generic Polecat patterns from JasperFx if/when published, complementing Cab's positioning.
- ADR in [`docs/decisions/`](../../decisions/) covering the Critter Stack as foundational technology and the per-BC engine choice between Marten and Polecat.
- [`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md) — immutable rules including the per-BC engine-choice constraint.
