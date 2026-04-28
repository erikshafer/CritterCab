# EF Core with Wolverine — Notes for CritterCab

> **Source article:** [EF Core is Better with Wolverine](https://jeremydmiller.com/2026/04/21/ef-core-is-better-with-wolverine/) by Jeremy D. Miller (2026-04-21)
>
> **Companion sources referenced inline:**
> - [Wolverine EF Core Durability Guide](https://wolverinefx.io/guide/durability/efcore/)
> - [Wolverine EF Core Outbox and Inbox](https://wolverinefx.io/guide/durability/efcore/outbox-and-inbox.html)
> - [Wolverine SQL Server Persistence](https://wolverinefx.io/guide/durability/sqlserver.html)
> - [Weasel EF Core Migrations](https://weasel.jasperfx.net/efcore/migrations.html)
> - ["Classic" .NET Domain Events with Wolverine and EF Core](https://jeremydmiller.com/2025/12/04/classic-net-domain-events-with-wolverine-and-ef-core/) — predecessor post from December 2025

---

## Why This Matters for CritterCab

CritterCab is Critter-Stack-first by commitment. The vision doc names Marten on PostgreSQL as the default for event-sourcing-heavy contexts (Trips, likely Dispatch) and Polecat on SQL Server for contexts where transactional integrity dominates (Payments). EF Core is not currently named as a CritterCab persistence option.

That makes Miller's article most useful to CritterCab as **reference material rather than commitment-altering input**. Two scenarios make it relevant:

1. **A bounded context arrives with a fit that justifies EF Core specifically.** The most plausible candidates inside CritterCab's tentative BC list are *Operations* (cross-cutting read models that may benefit from rich relational queries) and any future BC that integrates with a legacy relational schema not under our control. Polecat covers most SQL Server cases, but EF Core remains in scope where its ecosystem (migrations from existing schemas, library compatibility, team familiarity) is the deciding factor.
2. **The pattern transfers.** Wolverine's EF Core integration story is the same atomicity-and-outbox story Wolverine tells with Marten. The shape — handler returns events, framework guarantees the writes happen in one transaction with the outbox enrolment — is durable across persistence choices. Reading the EF Core variant clarifies what the framework is doing under any persistence backend, and validates that swapping persistence under a slice is a configuration concern, not a slice-rewrite concern.

The article also matters beyond CritterCab. Sibling showcases (CritterSupply, CritterBids) and other client work may reach for EF Core. The conventions captured here travel with the choice.

---

## The Core Argument

The article positions Wolverine as a serious development-and-production-time partner for EF Core users. The implicit framing is that **Critter Stack ergonomics are not Marten-exclusive**: dev-time auto-migration, test data seeding and reset, declarative persistence helpers, and the transactional outbox are all available with EF Core as the persistence layer.

Miller acknowledges that the original Critter Stack motivation was Marten plus Wolverine as an end-to-end Event Sourcing and CQRS solution. The pivot is pragmatic: most .NET shops already use EF Core, and Wolverine reaches further if it meets those teams where they are. The technical claim that follows from the strategic claim is that Wolverine has invested heavily enough in its EF Core integration to make EF Core a peer of Marten in Wolverine systems, not a second-class citizen.

What makes this credible is the specific feature inventory the article walks through. Each item is a documented Marten capability that has been reimagined for EF Core:

| Marten capability | EF Core counterpart in Wolverine |
|---|---|
| "Just works" startup migrations | Wolverine-managed EF Core diff migrations via Weasel |
| `IInitialData` test seeding | `IInitialData<TDbContext>` with the same shape |
| Database reset between tests | `ResetAllDataAsync<T>()` extension on the host |
| `[Aggregate]` declarative loading | `[Entity]` declarative loading with required-or-404 semantics |
| Batch querying | EF Core futures-based batching for `[Entity]` parameters |
| Transactional outbox with single-roundtrip writes | Same, via the optimised DbContext registration |

The pattern is consistent: Marten features are productised into the EF Core integration, often via Weasel and shared internal infrastructure. The transactional outbox is the linchpin that ties everything together.

---

## Bootstrapping Shape

The minimum viable EF Core + Wolverine setup has four moving parts:

1. **The `WolverineFx.EntityFrameworkCore` package.** Required for the dev-time accelerators, transactional middleware, and inbox/outbox integration. The article notes the package is technically optional only if none of those features are wanted, which in practice means it is always installed.
2. **Standard `AddDbContext<T>` registration**, with one Wolverine-specific tweak: `optionsLifetime: ServiceLifetime.Singleton`. The article calls this out as a real performance gain for Wolverine's internal DbContext usage. This is exactly the kind of "easy to miss, materially matters" detail that should live in a skill file rather than be rediscovered per project.
3. **`opts.PersistMessagesWithSqlServer(connectionString)`** to set up Wolverine's own message persistence (the inbox/outbox tables). This is a separate concern from the application DbContext: Wolverine needs its own storage for durable messaging, and the SQL Server persistence module supplies it.
4. **`opts.UseEntityFrameworkCoreTransactions()`** to enable the transactional middleware. This is what makes the handler-returns-event pattern enrol the outgoing message in the same transaction as the EF Core writes.

The optimised variant replaces the standard registration with `AddDbContextWithWolverineIntegration<T>()`. This single call:

- Tunes service lifetimes for Wolverine's internal DbContext access patterns.
- Wires direct mappings for Wolverine's inbox and outbox storage so they share the application's DbContext.
- Enables EF Core to batch the application's `SaveChangesAsync` SQL with the outbox writes into a single database round trip.

The article frames the round-trip reduction in stark terms: database chattiness is the single most common killer of enterprise application performance. Cutting two round trips to one per handler is not a micro-optimisation at scale.

---

## The Handler Shape

The reference handler in the article is striking for what it does *not* contain:

- No `async`/`await` ceremony. The handler is a static method returning a synchronous result.
- No explicit transaction management.
- No `IMessageBus` injection or `bus.PublishAsync()` call to emit the cascading event.
- No try/catch around the persistence-and-publish coordination.

The handler accepts the command and the DbContext, mutates the DbContext (adding an entity), and returns a domain event. Wolverine wraps the call with the EF Core transactional middleware, calls `SaveChangesAsync()` on the DbContext, enrols the returned event in the same transaction via the outbox, and dispatches the event after the transaction commits.

This shape is identical to the equivalent Marten-backed handler. From a slice-design perspective, **the persistence backend is invisible at the handler level**. The slice author writes the same shape regardless of whether the underlying store is Marten or EF Core. This is a real architectural property: it means swapping persistence under a slice (in either direction) is a configuration concern, not a slice-rewrite concern.

The predecessor article (December 2025, *"Classic" .NET Domain Events with Wolverine and EF Core*) goes further and argues for embedding business logic directly in the handler — pure functions over the inputs, with state changes expressed as returned events — rather than burying logic in entity types. This is consistent with Wolverine's vertical-slice idiom and matches how CritterCab plans to write Trips and Dispatch handlers against Marten.

---

## Performance Lens: Why the Optimised DbContext Matters

The single-round-trip claim deserves to be unpacked because it is the article's strongest production-time argument.

In a naïve EF Core + outbox setup, a handler that writes an entity and emits a message produces at least these database operations:

- `INSERT` (or `UPDATE`) for the application entity via EF Core's change tracker.
- `INSERT` for the outbox row containing the outgoing message.

If these are issued as separate commands against separate DbContexts (or against the same DbContext but as separate `SaveChanges` calls), the cost is two database round trips. Across a high-throughput handler, that doubles the network-latency contribution to handler duration.

The Wolverine-optimised DbContext maps the inbox/outbox tables into the application's DbContext. EF Core then batches both writes into a single round trip via its native command-batching mechanism. The handler still produces the same logical effect — application state plus outbox row, both in one transaction — but the wire-level cost is halved.

The implication for CritterCab: any service that reaches for EF Core in production should default to the optimised DbContext, not the standard registration. The optimised form is not an advanced feature; it is the production form. The standard `AddDbContext<T>()` is what you write when you are first wiring a project up, before you commit to Wolverine for messaging.

---

## Dev-time Ergonomics: Marten-style Features for EF Core

Three features in the article close the dev-time gap between EF Core and Marten.

### Wolverine-managed migrations

`opts.UseEntityFrameworkCoreWolverineManagedMigrations()` plus `opts.Services.AddResourceSetupOnStartup()` lets Wolverine diff the EF Core model against the live database at startup and apply missing DDL automatically. This is the EF Core analogue of Marten's "just works" migration story — modify your mappings, restart the host, the database catches up. The diff engine lives in Weasel.

The article positions this as a faster iteration loop than EF Core Migrations (the classic `dotnet ef migrations add` flow). For development and integration-test scenarios this is true. For production, the feature should be off — production migrations want explicit, reviewed change scripts, not silent runtime patches. The Weasel docs cover the opt-out story.

### `IInitialData<TDbContext>` for test seeding

A class implementing `IInitialData<TDbContext>` populates baseline data into the DbContext when invoked. Registered via `AddInitialData<TDbContext, TSeed>()`. This is the EF Core counterpart to Marten's `IInitialData<IDocumentSession>`.

### `ResetAllDataAsync<T>()` for test cleanup

The host extension method walks the DbContext's mapped tables, deletes rows in foreign-key-aware order, then re-applies registered `IInitialData<T>` seeders. Marten users will recognise the pattern; Respawn users will recognise the use case. The convenience is that the operation is integrated with the Wolverine host and the registered DbContext, so test fixtures do not need a separate Respawn configuration.

The combined effect is that an EF Core integration test in a Wolverine project can have the same `reset → seed → arrange → act → assert` rhythm as a Marten integration test. For CritterCab, where Alba is the integration-test host and the testing rhythm is committed at the project level, this matters: no test infrastructure has to be rebuilt because a service chose EF Core.

---

## Declarative Persistence: `[Entity]` and Futures Batching

This is the section most analogous to the Marten aggregate handler workflow already in heavy use across CritterBids.

The `[Entity]` attribute marks a handler parameter as something Wolverine should load by ID from the registered DbContext. Wolverine infers the ID from a naming convention against the incoming command (e.g., `BacklogItemId` on the command maps to a `BacklogItem` entity). `Required = true` makes the missing-entity case short-circuit the handler — for an HTTP endpoint, that becomes a `400 ProblemDetails` response automatically. The mapping can be made explicit when the convention does not fit.

Two consequences worth calling out:

1. **The handler stays a pure function over loaded inputs.** The asynchronous load-by-ID-with-null-check boilerplate that typically clutters command handlers is eliminated. The handler signature itself documents what the slice depends on.
2. **Multiple `[Entity]` parameters are batched.** Wolverine uses an EF Core futures mechanism to fetch all `[Entity]`-marked parameters in one round trip. The article's example loads both a `BacklogItem` and a `Sprint` for a "commit to sprint" handler — two queries become one round trip without the slice author writing any batching code.

For CritterCab, the conceptual fit is direct. Many CritterCab slices will need to load existing entities by ID before mutating them: a "Driver Goes Online" handler needs the driver, a "Trip Completes" handler needs the trip aggregate, a "Rider Cancels Booking" handler needs both the booking and the rider's profile. The Marten-backed contexts get this via the aggregate handler workflow. Any EF Core-backed context could get the same ergonomics via `[Entity]`.

---

## Beyond the Article

The article links to several feature pages without going deep. Worth noting in inventory form, ranked by likely CritterCab relevance if EF Core is adopted:

| Feature | What it gives you | Likely CritterCab relevance |
|---|---|---|
| **Saga storage with EF Core** | Saga state lives in the same DbContext as related domain entities, sharing a transaction with them | High — sagas that touch EF Core-backed domain state should not span two persistence stores |
| **Domain events integration** | EF Core entities raise domain events; Wolverine harvests them at `SaveChangesAsync` and publishes via the outbox | Medium — relevant if a BC's modelling style favours entity-raised events over handler-returned cascading messages |
| **Multi-tenancy support** | Tenant resolution against the EF Core integration | Low for CritterCab specifically; CritterCab is single-tenant by design |
| **Operation side effects** | A functional-style way to express side effects from handlers without imperative bus calls | Medium — aligns with the handler-as-pure-function idiom |
| **Query Plans (Specification pattern)** | Reusable, testable query objects | Medium-High for read-heavy contexts (Operations) |
| **Batch Querying** | Multiple queries combined into a single round trip in a handler | High for any handler that loads more than one entity |

Each of these features should get its own skill file entry if and when the corresponding feature is exercised in CritterCab.

---

## Application to CritterCab

CritterCab's persistence commitments do not currently include EF Core. The path by which EF Core might enter the project is narrow but real:

- **Operations BC as the most plausible candidate.** The BC is described as "cross-cutting read models and administrative commands" and consumes events from most other contexts. Cross-cutting read models that need joins across multiple denormalised projections may fit a relational ORM more naturally than a document store. EF Core would compete with Polecat for this slot. The deciding factor is whether the read models look more like documents (Polecat) or more like joinable relational tables (EF Core).
- **A future legacy-integration BC.** If CritterCab grows a context that needs to integrate with an existing relational schema not under our control — for example, a fictional dispatch-partner booking system — EF Core's tooling for working with externally-defined schemas would be the deciding factor.
- **A bounded context where developer team familiarity weighs heavily.** This is a soft factor for a reference architecture but a real one for production systems built on the Critter Stack. The article exists in part to support that scenario.

In none of these cases is EF Core obviously preferable today. Polecat is the committed SQL Server option, and the BCs that would benefit most from a relational persistence story are not yet under active modelling. **Treat the EF Core option as available and well-supported, not as committed.**

---

## Decisions Drawn from This Reading

Provisional. These would crystallise into skill content if and when EF Core is adopted in any CritterCab service. Each is conditional on that adoption.

### EF Core registration always uses the Wolverine-optimised form

`AddDbContextWithWolverineIntegration<T>()` rather than `AddDbContext<T>()`, in any CritterCab service that adopts EF Core. The single-round-trip optimisation is not an advanced feature; it is the production form. Standard registration is appropriate only for prototypes or for the brief period between project creation and Wolverine wiring.

**Rationale.** The optimised registration is strictly better when Wolverine is in use: it tunes service lifetimes, maps the inbox/outbox tables into the application DbContext, and enables batched round trips. The cost of "remembering to use the optimised form" is a one-line difference; the cost of forgetting is doubled per-handler database round trips and a measurable production-time performance loss.

**Promotion path.** Wolverine + EF Core skill file, authored when an EF Core BC is implemented.

### Dev-time managed migrations are on; production-time migrations are explicit

`UseEntityFrameworkCoreWolverineManagedMigrations()` plus `AddResourceSetupOnStartup()` is the dev-and-test default. The same configuration is **not** used in production. Production migrations follow the explicit, reviewed-script flow regardless of whether the local-development experience uses auto-diff.

**Rationale.** The dev-time iteration speedup is real and matches the Marten-style "just works" experience the project commits to. The production-time risk — silent runtime patching of database schemas — is the wrong trade-off for any environment under SLA. The two-track approach (auto in dev/test, explicit in prod) is the same posture Marten projects adopt, and is the same posture CritterCab should take.

**Promotion path.** Same skill file, with a clearly-marked environment-specific configuration block.

### Test rhythm is `ResetAllDataAsync<T>` plus `IInitialData<T>`

Integration tests against an EF Core BC use Wolverine's reset and seed integration rather than reaching for Respawn or hand-rolled fixtures. The cleaner test rhythm matches what Marten BCs already get and keeps the project's Alba scenario style consistent across persistence choices.

**Promotion path.** Test conventions skill file, alongside the Wolverine.HTTP test conventions noted in [`wolverine-http-vertical-slice.md`](./wolverine-http-vertical-slice.md).

### `[Entity]` is preferred over manual loads in EF Core handlers

For any EF Core handler that loads an entity by ID with a "must exist" precondition, the `[Entity(Required = true)]` attribute replaces hand-rolled load-and-null-check code. Multiple `[Entity]` parameters in one handler get batched automatically; the handler author does not write batching code.

**Promotion path.** Same Wolverine + EF Core skill file. The convention matches the Marten aggregate-handler-workflow convention closely enough that the two skill files cross-reference each other.

### Service lifetime tweak is non-negotiable

When the optimised registration is not used (rare; see above), `optionsLifetime: ServiceLifetime.Singleton` on the standard `AddDbContext<T>()` registration is mandatory. This is the kind of detail that gets dropped when copying registration snippets between projects, and the cost is silent: Wolverine's internal DbContext usage becomes more expensive without any visible signal at the slice level.

**Promotion path.** Skill file note at registration. Worth a Roslyn analyser or ArchUnitNET test if and when one is added (per the [Living Architecture Documentation](./living-architecture-documentation.md) recommendations).

---

## Open Questions

Decisions deferred until a CritterCab BC actually adopts EF Core.

- **Polecat vs. EF Core for the Operations BC.** Both are viable for SQL Server. The deciding factor is the shape of the cross-cutting read models, which has not yet been modelled. Defer until Operations is under active modelling.
- **Domain-event harvesting style.** Wolverine's EF Core integration supports both handler-returned cascading messages (the modern style the article's main example uses) and entity-raised domain events harvested at `SaveChangesAsync` (the predecessor article's "classic" style). CritterCab leans toward the cascading-message style for consistency with the Marten aggregate handler workflow, but the trade-off is worth a deliberate session before commitment.
- **Saga persistence when a saga touches multiple BCs' data.** If a saga lives in BC A but coordinates state that ends up in BC B's data store, the saga storage choice and the cross-BC coordination story interact. Defer until the first cross-BC saga is identified.
- **Whether EF Core ever appears at all.** Honest framing: it may not. The current commitment is Marten plus Polecat plus Redis, and nothing in the modelling so far suggests an EF Core gap. This research note exists primarily so that *if* the question arises, the answer is not a re-derivation from scratch.

---

## Related CritterCab Research

- [Wolverine.HTTP and Vertical Slice Architecture](./wolverine-http-vertical-slice.md) — The HTTP-side companion to this note. Same atomicity-and-outbox philosophy, applied at the HTTP boundary rather than the persistence boundary. The two notes share the test conventions decision and the "framework concern, not a slice concern" framing for atomicity.
- [Living Architecture Documentation](./living-architecture-documentation.md) — Context for the analyser/ArchUnitNET promotion path mentioned above. Convention enforcement is the natural next layer when Wolverine + EF Core conventions firm up.

---

## References

- **Article (primary):** Jeremy D. Miller, *EF Core is Better with Wolverine*, 2026-04-21 — https://jeremydmiller.com/2026/04/21/ef-core-is-better-with-wolverine/
- **Predecessor article:** Jeremy D. Miller, *"Classic" .NET Domain Events with Wolverine and EF Core*, 2025-12-04 — https://jeremydmiller.com/2025/12/04/classic-net-domain-events-with-wolverine-and-ef-core/
- **Wolverine EF Core durability guide:** https://wolverinefx.io/guide/durability/efcore/
- **Wolverine EF Core outbox/inbox:** https://wolverinefx.io/guide/durability/efcore/outbox-and-inbox.html
- **Wolverine SQL Server persistence:** https://wolverinefx.io/guide/durability/sqlserver.html
- **Weasel EF Core migrations:** https://weasel.jasperfx.net/efcore/migrations.html
- **Wolverine EF Core sagas:** https://wolverinefx.io/guide/durability/efcore/sagas.html
- **Wolverine EF Core operations:** https://wolverinefx.io/guide/durability/efcore/operations.html
- **Wolverine EF Core multi-tenancy:** https://wolverinefx.io/guide/durability/efcore/multi-tenancy.html
- **Wolverine EF Core domain events:** https://wolverinefx.io/guide/durability/efcore/domain-events.html
- **Wolverine EF Core query plans:** https://wolverinefx.io/guide/durability/efcore/query-plans.html
- **Wolverine EF Core batch queries:** https://wolverinefx.io/guide/durability/efcore/batch-queries.html
