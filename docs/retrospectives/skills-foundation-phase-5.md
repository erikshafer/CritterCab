# Retrospective — CritterCab Skill Library, Phase 5 (Reconciliation)

## Metadata

- **Triggering prompt:** [`docs/prompts/skills-foundation-phase-5-handoff.md`](../prompts/skills-foundation-phase-5-handoff.md)
- **Status:** In progress
- **Date authored:** 2026-05-06 (running record; updated as Phase 5 progresses)
- **Output artifacts:** Per-skill trims to `docs/skills/*/SKILL.md` files; this retrospective; eventual README close-out.
- **Outcome:** _(to be filled in at session end)_

---

## Framing

Phase 5 is the reconciliation pass against the JasperFx ai-skills library. Unlike Phases 1–4, no new skills are authored; instead, each of the 39 existing Cab skills is checked against ai-skills for direct equivalents, deduplicated where appropriate, and flagged for upstream-contribution where Cab's coverage exceeds ai-skills'.

Methodology per skill (mirrors Phase 4 cadence):

1. Identify ai-skills counterpart.
2. Read both end-to-end.
3. Audit Cab's sections against four categories (no equivalent / direct equivalent / upstream-contribution candidate / upstream-replacement candidate).
4. Pause and report findings to the user before any edits.
5. Apply trims after greenlight.
6. Record outcome here.

Convention adopted in Phase 5: Cab `See Also` sections distinguish three flavors of cross-reference:

- **`Upstream`** — the authoritative external reference library this Cab skill defers to. In practice this means **ai-skills** (license required, install via `npx skills add`). Entries drop the `ai-skills` prefix since the heading establishes scope. The lead-in line names the licensing once. **This is a semantic shift from Phases 1–4**, where `Upstream` meant Cab-internal load-first prerequisites.
- **`Prerequisites`** — Cab-internal skills to load first if unfamiliar with the project conventions assumed by this skill. This is the new label for what Phases 1–4 called `Upstream`.
- **`External`** — non-ai-skills external references (Wolverine docs, blog posts, papers, RFCs).

The `Upstream` section comes first in `See Also` (foundational reference), followed by `Prerequisites` (Cab orientation), then `Sibling skills`, `Downstream`, and `External`.

The rename `Upstream` → `Prerequisites` happens as part of each skill's Phase 5 reconciliation, alongside the addition of the new `Upstream` (ai-skills) block. Cross-references in other skills are unaffected because they reference skill names, not section names within those skills.

---

## Pre-flight (recorded at session start)

- **ai-skills location confirmed:** `C:\Code\JasperFx\ai-skills\skills\` (locally cloned, ~70 skills covering Wolverine, Marten, Polecat, integrations, testing, observability, architecture).
- **Working environment:** Filesystem MCP + Windows-MCP, same as Phase 4.
- **No new skills queued:** confirmed; Phase 5 is reconciliation only.
- **Phase 4 retrospective:** read; methodology lessons absorbed (pause-after-each-skill, source-verify-not-memory, check-if-already-edited, forward-reference scans).
- **Phase 5 retrospective baseline:** none — this file is being authored from scratch as Phase 5 progresses.

Reference repo HEAD revisions at session start: _(not captured — would have been ideal per Phase 4 methodology lesson; capture at next session if Phase 5 spans multiple sessions)._

---

## Per-skill reconciliation table

| # | Skill | Tier | ai-skills counterpart | Category | Lines saved | Notes |
|---|---|---|---|---|---|---|
| 1 | `wolverine-handlers` | 1 | `wolverine-handlers-fundamentals` (+ 7 fragmented siblings) | Direct equivalent (deduplicated) | ~18 | Hub-skill structure preserved; trimmed Lambda Factory anti-pattern + Logger Convention duplication; promoted `External` ai-skills entries to new `Upstream (ai-skills)` block |
| 2 | `wolverine-http-handlers` | 1 | `wolverine-http-fundamentals` + `wolverine-http-marten-integration` + `wolverine-http-hybrid-handlers` | Direct equivalent (deduplicated) | ~60 | Trimmed Bare Event Return shape-2 elaboration, full Concrete-Return-Types-vs-IResult table, and generic Route Binding rules; renamed Route Binding section to "Aggregate ID — Cab Convention"; applied three-block See Also; 2 upstream-contribution candidates flagged |
| 3 | `wolverine-messaging-handlers` | 1 | `wolverine-messaging-message-routing` + `wolverine-messaging-resiliency-policies` | Direct equivalent (deduplicated) | ~26 | Trimmed PublishAsync/InvokeAsync code blocks, OutgoingMessages explanatory paragraph, ScheduleAsync explanation, redundant CLI commands; preserved Cab-specific routing-rule pre-flight, decision matrix, inbound handler idempotency pattern; added `wolverine-messaging-resiliency-policies` upstream cross-ref despite no Cab parallel; 1 upstream-contribution candidate flagged |
| 4 | `wolverine-grpc-handlers` | 1 | _none — ahead of ai-skills_ | **No equivalent** + upstream-contribution candidate (Erik is planned author) | 0 (rename only) | Light pass: renamed `Upstream` → `Prerequisites`; removed misleading forward-looking placeholder for not-yet-published `wolverine-grpc` ai-skills + the install/license note. Erik is the planned author of the future ai-skills `wolverine-grpc` skill — Cab `wolverine-grpc-handlers` should be revisited once that publishes |
| 5 | `wolverine-grpc-bidirectional-handlers` | 1 | _none — ahead of ai-skills_ | **No equivalent** + part of Erik's planned upstream work | 0 (rename only) | Light pass: identical pattern to Skill 4. Renamed `Upstream` → `Prerequisites`; removed the same forward-looking placeholder + install/license note. Erik's planned ai-skills `wolverine-grpc` skill will likely cover both this skill's content (client-streaming hand-written workaround + bidirectional) and the Skill 4 content (unary + server-streaming) |
| 6 | `wolverine-kafka` | 1 | `wolverine-integrations-kafka` + `wolverine-messaging-resiliency-policies` | Direct equivalent (deduplicated) | ~125 | Most aggressive trim yet. Heavy code-block duplication: trimmed Direct connection, ConsumeOnly, Convention-based routing, Multi-topic listeners, Transport-level/per-topic group ID overrides, GroupId stamping, Default envelope serialization, Raw JSON interop, Custom envelope mapper, native DLT, Retry policies, Circuit breakers, Tombstones; preserved EH Emulator constraint, Cab BC topic table + handlers, partition key rationale (driver_id/zone_id), ProcessInline decision framing, Schema Registry decision, Azure Event Hubs section (EH Emulator + ConfigureClient SASL), 12-bullet Common pitfalls; 2 upstream-contribution candidates flagged (EH Emulator AutoProvision constraint, BatchMessagesOf<T> for batch consumption) |
| 7 | `wolverine-azure-service-bus` | 1 | `wolverine-integrations-azure-service-bus` + `wolverine-messaging-resiliency-policies` | Direct equivalent (deduplicated) | ~160 | Largest trim of Phase 5 (8.2% byte reduction). Heavy code-block duplication trimmed across Bootstrap (Managed Identity, AutoProvision, AutoPurgeOnStartup, System queues), Topics (Publishing, Subscribing, Subscription rules, Configuring properties), Queues (Publishing, Listening, Configuring), Sessions (Setting session ID, Sessions on subscriptions, ExclusiveNodeWithSessions), DLQ (Native, Wolverine routing, Circuit breakers), Scheduled delivery, Custom envelope mapper. **Erik flagged that the existing MassTransit/NServiceBus interop section was incorrect** — Cab does not use them and never will. Section removed entirely (not trimmed). Preserved Cab-specific content: Aspire connection-string integration, BC examples (TripCompleted, RiderRegistered, ProcessPayment), partition rationale, MaxDeliveryCount-vs-Wolverine-retries footgun, Aspire emulator package status, default envelope mapping table, local-vs-production table, 12-bullet Common pitfalls. 1 upstream-contribution candidate flagged (MaxDeliveryCount × Wolverine retries multiplicative interaction) |
| 8 | `wolverine-sagas` | 1 | _none — ai-skills has no saga skill_ | **No equivalent** + observed coverage gap (no active author) | ~3 (rename only) | Light pass: counterpart `wolverine-sagas-saga-pattern` was assumed to exist but verified absent in ai-skills (no `wolverine-sagas-*` skills exist; only saga test fixtures inside `wolverine-converting-from-masstransit`/`-nservicebus`). Renamed `Upstream` → `Prerequisites`; removed forward-looking placeholder + install/license note. Cab's 39 KB skill stands as authoritative reference. ProcessManager<TState> framework references confirmed absent from body (only generic-pattern uses survive: `process-manager` tag for searchability and "process managers that do everything" anti-pattern phrasing on line 423 — both retained as standard pattern terminology). 1 upstream-contribution candidate flagged with **Observed gap** priority (no current author) |
| 9 | `marten-aggregates` | 1 | `marten-projections-single-stream` + `marten-aggregate-handler-workflow` | Direct equivalent (deduplicated) | ~3 net (~5 saved from Snapshot Strategies + External trims, partially offset by ~5 added in new Upstream block) | First design-and-conventions skill in Phase 5 — minimal duplication by design, since Cab top framing explicitly defers mechanics to ai-skills. Trimmed Snapshot Strategies (removed Live-vs-Inline mechanic table; kept Cab "live aggregation default" guidance + cross-ref to `marten-projections-single-stream` § Projection lifecycles). Restructured See Also to three-block convention with new Upstream block referencing both ai-skills counterparts. Removed forward-looking placeholder for non-existent `marten-event-sourcing-fundamentals` (4th forward-looking placeholder removed in Phase 5; Skills 4, 5, 8, 9). Preserved entirely: When-to-event-source decision framework, Canonical Aggregate Shape (Trip + 6 conventions), Decider Pattern Critter Stack realization table, Stream Identity (incl. UUID v5 deterministic IDs), Apply Method Conventions (4 sub-rules), Aggregate Field Conventions, RideOffer worked example, 9-bullet Common Pitfalls. No upstream-contribution candidates flagged — content is genuinely Cab-specific design conventions (UUID v5 namespace, live default, ImmutableList preference, event-first parameter order, no-throw rule), not pitfalls or coverage gaps in ai-skills. |
| 10 | `marten-wolverine-aggregates` | 1 | `marten-aggregate-handler-workflow` (primary) + `wolverine-handlers-declarative-persistence` + `wolverine-handlers-fundamentals` | Direct equivalent (deduplicated) | ~9 net body content (+~1 See Also; net file +247 bytes) | Hybrid skill — partly design+conventions (decider-pattern positioning, `nameof()` pin, UUIDNext convention, `MartenOps.StartStream` Action-overload), partly mechanic content overlapping ai-skills. Trimmed Identity resolution (4-step Wolverine resolution chain → cross-ref to ai-skills § Aggregate identity conventions; kept Cab `nameof()` pin rationale), Concurrency style (Optimistic/Exclusive table → Cab pin paragraph + cross-ref to ai-skills § Optimistic concurrency for Version-property + VersionSource surface), Manual Session Calls anti-pattern (collapsed leading prose, expression-bodied correct example, cross-ref to ai-skills § Common anti-patterns). Added cross-ref note to Return-Type Cheat Sheet (Cab table is genuinely more comprehensive — 10 rows vs ai-skills' 5 — so kept Cab and pointed users to ai-skills for return-types fundamentals). Restructured See Also to three-block convention with new Upstream block referencing all 3 ai-skills counterparts. Preserved entirely: top framing, When to apply, Two Canonical Shapes (StartTrip/CompleteTrip BC examples), `AlwaysEnforceConsistency` (not in ai-skills), Multi-Stream Handlers (TransferRiderToDriver with `[ReadAggregate]`), Stream Identity (UUIDNext convention, MD5 warning, Action overload), Anti-Pattern: Generating Stream ID for Auto-Assigned, Anti-Pattern: WriteAggregate on read handler, Worked Example: AcceptOffer, 9-bullet Common Pitfalls. 1 upstream-contribution candidate flagged (`MartenOps.StartStream` Action-overload for capturing assigned ID). Skill name flagged for potential post-Phase-5 rename (Erik noted he doesn't love `marten-wolverine-aggregates`). **Revisit applied post-Skill-16: `[ConsistentAggregate]` / `[ConsistentAggregateHandler]` coverage added** as a new H2 section between Multi-Stream Handlers and Stream Identity, closing cross-cutting Cab coverage gap #8. File 24,631 → 31,398 bytes (+6,767 bytes / +27%) — largest single per-skill edit in Phase 5 by byte count. See per-skill detail § Skill 10 revisit (post-Skill-16). |
| 11 | `marten-projections` | 1 | `marten-projections-single-stream` + `marten-projections-multi-stream` + `marten-projections-event-enrichment` + `marten-projections-composite` + `marten-projections-raise-side-effects` | Direct equivalent (deduplicated; partial coverage — 3 ai-skills topics have no Cab parallel) | ~41 net (~46 saved from body trims; +5 added in 5-entry Upstream block) | Heavy-mid trim of a hybrid skill: ai-skills splits projection coverage across 6 dedicated skills (5 referenced; `flat-table` skipped per Erik). Trimmed Three Lifecycles (collapsed mechanic-comparison table to Cab-default-and-use-case framing; cross-ref to ai-skills § Projection lifecycles), Single-Stream Shape 2 (compressed code, removed redundant generic-form example, kept parameterless-constructor footgun), Single-method Evolve alternative (removed code example since ai-skills covers; kept Cab decision criteria), Multi-Stream Projections (collapsed "Two things to internalize" + "Multi-stream gotchas" 3-bullet list to single Cab-pin paragraph + cross-ref to ai-skills `marten-projections-multi-stream`), Event Projections (folded explanation into intro), Soft-Delete Pattern (two H3 sections collapsed to bold paragraphs), Lookup-Document Pattern (minor ai-skills cross-ref add). Restructured See Also to three-block convention with **5-entry Upstream block** (largest Phase 5 Upstream block to date) — single-stream, multi-stream, event-enrichment, composite, raise-side-effects. Removed 5th forward-looking placeholder for non-existent `marten-event-sourcing-fundamentals` (4th time the same nonexistent skill has surfaced). **3 new Cab coverage gaps revealed** (event enrichment, composite projections, RaiseSideEffects — the largest single-skill gap discovery of Phase 5). Preserved entirely: top framing (Marten 8.0 / Chassaing snapshot+evolve alignment), Three Projection Recipes table, Shape 1 Self-aggregating, Multi-stream BC example (DriverLifetimeStats), Event Projections BC example (TripReceipt), IoC-Injected Projections (TripPricingProjection + identity-acl tie-in), Lifecycle Decision Guide (5-step flow), 8-bullet Anti-Patterns (incl. registration silent-failure footgun — upstream candidate). 1 upstream-contribution candidate flagged (projection registration silent-failure parallel to routing-rule pre-flight). |
| 12 | `marten-querying` | 1 | `marten-advanced-indexes-and-query-optimization` + `marten-advanced-optimization` | Direct equivalent (deduplicated; minimal overlap — most of Cab content has no ai-skills counterpart) | ~5 net (~5 from forward-looking placeholder + install/license note removal; +6 added in new 2-entry Upstream block) | Genuinely light pass — essentially a See Also restructure. Cab's 33 KB skill covers Sessions (4 kinds + identity-map effects), LINQ (Include, Stats), **Compiled Queries with detailed gotchas** (4 silent failures + IQueryPlanning paging pattern), Batched Queries, Query Plans (`QueryListPlan`), Raw JSON, Raw SQL (`AdvancedSql` with schema resolution + ROW() syntax), Metadata queries (soft-delete extensions, last-modified), **modern Wolverine HTTP-first streaming API** (`StreamOne<T>`/`StreamMany<T>`/`StreamAggregate<T>` return types), and Common pitfalls. **Most of this has no ai-skills counterpart at all** — ai-skills covers indexes (entirely separate topic Cab doesn't touch) and bootstrap-level optimization (mostly relevant to other Cab skills like `marten-projections` and the upcoming `marten-async-daemon`). Restructured See Also to three-block convention with new 2-entry Upstream block (both marked **No Cab parallel** for indexes / overlaps-elsewhere for optimization). Removed 6th forward-looking placeholder for `marten-event-sourcing-fundamentals` (same nonexistent skill, 4th time across Cab; 6th cumulative removal in Phase 5). Removed install/license note (now in Upstream lead-in). **Significant Cab coverage gap revealed**: Cab has zero coverage of index design — entire ai-skills `marten-advanced-indexes-and-query-optimization` skill (`Index()`, `[DuplicateField]`, `GinIndexJsonData()`, `UniqueIndex`, `IndexMethod`, `TenancyScope`, full-text). Erik's pre-audit framing ("querying is also performance through indexes") accurately predicted this gap. **Two upstream-contribution candidates flagged** (Erik praised the candidates): (1) compiled query footgun list (4 silent failures + `IQueryPlanning` pattern — ai-skills' compiled-query coverage is one paragraph); (2) modern Wolverine HTTP-first streaming API (`StreamOne<T>`/`StreamMany<T>`/`StreamAggregate<T>` return types — ai-skills `marten-advanced-optimization` covers only the older extension-method API). Preserved entirely: all 12 body sections (top framing, When to apply, Sessions, Decision Matrix, Standard LINQ, Compiled Queries, Batched Queries, Query Plans, Raw JSON, Raw SQL, Metadata, Streaming JSON to HTTP, Common pitfalls). |
| 13 | `marten-async-daemon` | 1 | `marten-advanced-async-daemon-deep-dive` (primary) + `marten-advanced-optimization` + `marten-projections-event-enrichment` + `marten-projections-composite` | Direct equivalent (deduplicated; partial coverage — Cab and ai-skills cover overlapping but substantially different surfaces) | ~5 net body content (+~5 See Also; net file +1,435 bytes) | Hybrid skill — partly mechanic-overlap (Daemon Modes, Error Handling defaults), partly Cab-exclusive (April 2026 features, position strategies, health checks). Trimmed Error Handling Defaults (removed 14-line skip-flag code example covered in ai-skills; kept Cab framing about asymmetry rationale + tombstone events / `StaleSequenceThreshold` subsection). Added cross-ref notes to Throughput and Performance Knobs section (basic knobs in ai-skills; April 2026 additions Cab-specific) and Async-Only Performance Hooks section (defer to projections skills for full surface). Restructured See Also to three-block convention with **4-entry Upstream block** (largest after Skill 11). Removed 7th forward-looking placeholder for `marten-event-sourcing-fundamentals` (4th time across Cab; 7th cumulative removal in Phase 5 — pattern thoroughly established at this point). **6th Cab coverage gap revealed**: operational/troubleshooting daemon surface — ai-skills covers programmatic daemon control (`StopAgentAsync`, `IProjectionCoordinator`, `AllProjectionProgress`), production tuning (`HealthCheckPollingTime`, `CheckAssignmentPeriod`, `InboxStaleTime`, `OutboxStaleTime`), durability metrics (`wolverine-inbox-count` etc.), cold-start optimization (`codegen write`), SQL diagnostics for node assignments, and `NodeAssignmentHealthCheckTracing` settings — none of which Cab covers. **Three upstream-contribution candidates flagged**: (1) April 2026 daemon improvements (`EnableEventTypeIndex` + Adaptive event loader, Marten 8.29+) entirely missing from ai-skills' deep-dive; (2) Position strategies for new projections (`SubscribeFromPresent`/`SubscribeFromTime`/`SubscribeFromSequence`/`SubscribeAsInlineToAsync`) entirely missing from ai-skills; (3) Health check `gracePeriod` parameter for K8s rolling restarts (ai-skills doesn't cover health checks at all). Preserved entirely: top framing, When to apply, **What the Daemon Is Doing** (runtime model: shards, high-water mark, processing loop, polling cadence), Daemon Modes (table + Cab default + Critical-rule anti-pattern), Tombstone events / StaleSequenceThreshold, Throughput knobs (BatchSize, CacheLimitPerTenant, UseIdentityMapForAggregates, EnableAdvancedAsyncTracking, EnableEventSkippingInProjectionsOrSubscriptions, **EnableEventTypeIndex** with diagnostic-signal framing, **Adaptive event loader** with 3-strategy fallback chain), Rebuilds (full, single-stream, teardown, **position strategies** 4-strategy set), Async-Only Performance Hooks (`EnrichEventsAsync` with async-only-pipeline limitation, composite projections), Health Checks (`AddMartenAsyncDaemonHealthCheck` with `gracePeriod` for K8s rolling restarts), Observability (per-projection metrics: processed/gap/skipped + Cab-configurable OtelPrefix), Common pitfalls (9 bullets). |
| 14 | `dynamic-consistency-boundary` | 1 | `marten-advanced-dynamic-consistency-boundary` | Direct equivalent (deduplicated; **Cab substantially exceeds ai-skills** — ai-skills provides basic three-part pattern intro + brief decision guidance, Cab adds the entire footgun + operational surface) | ~3 net (~3 saved from forward-looking placeholder + install/license note removal; +6 added in new substantial Upstream entry + Prerequisites heading) | Genuinely light pass — essentially See Also restructure. Cab's 32 KB skill covers the basic three-part `[BoundaryModel]` pattern (Load + Handle + state) plus the entire footgun surface and operational layer that ai-skills' 7.6 KB counterpart doesn't address. Restructured See Also to three-block convention with new 1-entry Upstream block carrying explicit "**Cab's coverage substantially exceeds ai-skills' coverage**" framing and a comprehensive enumeration of what Cab adds (manual `FetchForWritingByTags`, `Validate`/`HandlerContinuation` hook, `ValidateAsync` incompatibility, boundary state `Id` requirement, tag-type single-property record requirement under .NET 10, `StartStream`-drops-tags behavior, `DcbConcurrencyException`-vs-`ConcurrencyException` sibling rule, testing patterns, 8-step checklist). Removed 8th forward-looking placeholder for `marten-event-sourcing-fundamentals` (5th time across Cab; 8th cumulative removal in Phase 5). Removed install/license note (now in Upstream lead-in). **Most footgun-rich skill in Phase 5** — 7 upstream-contribution candidates flagged, the highest count from any single skill. Also surfaced an **ai-skills content drift observation** (see new section): ai-skills' DCB skill claims DCB is "currently implemented in Polecat" with `[BoundaryModel]` and `EventTagQuery` described as Polecat-specific, but the file is named `marten-advanced-dynamic-consistency-boundary` (Marten-prefixed) and Marten DCB clearly exists. Cab demonstrates Marten DCB end-to-end with the `Marten.Events.Dcb.DcbConcurrencyException` namespace and `opts.Events.RegisterTagType<>().ForAggregate<>()` on Marten store options. Preserved entirely: all 13 body sections including top framing, When to apply, When to Reach for DCB (Cab decision matrix + offer-acceptance scenario), Two Patterns One Preferred, Canonical `[BoundaryModel]` Pattern (Load + Handle + return-value table + Validate + ValidateAsync incompatibility), Manual `FetchForWritingByTags` Pattern, EventTagQuery DSL, Boundary State Aggregate, Tag Type Registration, Tagging Writes, Concurrency, Testing, Implementation Checklist, Common pitfalls (9 bullets). |
| 15 | `polecat-event-sourcing` | 2 | `polecat-setup-and-decision-guide` (primary) + `polecat-cross-stream-operations` + `critterstack-arch-new-project-wolverine-polecat` | Direct equivalent (deduplicated; partial overlap — Cab is ahead on DCB / saga concurrency / v3.0+v3.1 framing / polling daemon / subscriptions; ai-skills is ahead on MCP server / [ConsistentAggregate] attributes / FakeEventStream stub) | ~12 net body content (~12 saved from Appending events code-block trim + Schema management AutoCreate enumeration trim + forward-looking placeholder + install/license note removal; +8 added in new 3-entry Upstream block + Prerequisites heading); net file −17 bytes (essentially neutral) | Hybrid skill — moderate mechanic overlap with ai-skills' three Polecat skills (~30.7 KB total) plus significant Cab value-add (DCB Polecat namespace coverage, Polecat saga concurrency story resolving the `wolverine-sagas` deferred verification, polling daemon vs LISTEN/NOTIFY framing, EventForwarding via `SubscribeToEvent<T>`, v3.0 schema breaking change context, v3.1 `IDocumentStoreUsageSource` publishing, Cab Payments BC narrative). Trimmed Appending events (collapsed 14-line code enumeration to 3-line Cab convention + cross-ref to ai-skills `polecat-setup-and-decision-guide`); Schema management (removed AutoCreate enumeration, kept Cab CLI workflow + cross-ref). Restructured See Also to three-block convention with **3-entry Upstream block** (`polecat-setup-and-decision-guide` primary, `polecat-cross-stream-operations`, `critterstack-arch-new-project-wolverine-polecat`). Removed 9th forward-looking placeholder (the most broken yet — referenced `polecat-event-sourcing` and `polecat-document-store` by name when the actual ai-skills are differently named). **3 Cab coverage gaps revealed (entries #7, #8, #9)**: Polecat MCP server, `[ConsistentAggregate]` / `[ConsistentAggregateHandler]` attribute shortcuts (cross-cutting gap, also affects Skill 10), `FakeEventStream<T>` unit-test stub. **8 upstream-contribution candidates flagged** (entries #24–31). **ai-skills content drift observation #2**: ai-skills `polecat-cross-stream-operations` reinforces the Skill 14 drift, claiming DCB "Availability: Polecat only" — folded into existing tracker entry #1 rather than added as new. Preserved entirely: top framing, When to apply, Polecat in CritterCab, Setup and bootstrap + v3.1 monitoring discovery, Core schema, FetchForWriting and WriteToAggregate, Projection lifecycles, v3.0 Snapshot<T>() shortcut, Multi-stream and other projection types, Live aggregation, **Dynamic Consistency Boundary** (full Polecat-namespace coverage), **Async daemon** (polling vs LISTEN/NOTIFY architectural framing), Subscriptions + EventForwarding + UseFastEventForwarding/UseWolverineManagedEventSubscriptionDistribution, Schema management CLI workflow, **Wolverine integration** (PolecatOps factory + aggregate handlers + saga storage + Polecat saga concurrency story resolving wolverine-sagas deferred verification), Cab use case Payments BC narrative, Common pitfalls (12 bullets). |
| 16 | `polecat-document-store` | 2 | `polecat-setup-and-decision-guide` (primary, bootstrap-side overlap only) + `polecat-cross-stream-operations` + `critterstack-arch-new-project-wolverine-polecat` | **No substantive equivalent + entire-skill-creation candidate (Observed gap, no active author)** — ai-skills has no dedicated document-database skill for either Marten or Polecat; the three Polecat ai-skills cover only bootstrap-side document-store registration, leaving session shapes / identity strategies / LINQ / soft deletes / patching / document concurrency uncovered | ~2 net body content (cross-ref additions to Sessions and Storing-and-loading sections); +14 added in new Upstream block + Cab-exceeds framing paragraph + Prerequisites heading; net file +2,440 bytes (+7.8%) | Light pass on body — Cab is so far ahead of ai-skills here that there's almost nothing to trim. Two minor cross-ref note additions (Sessions section noting `LightweightSession()` mention in `polecat-setup-and-decision-guide`; Storing-and-loading section noting basic `Store`/`Append` example in same). Restructured See Also to three-block convention with **3-entry Upstream block** (`polecat-setup-and-decision-guide` primary noting bootstrap-only doc-store coverage, `polecat-cross-stream-operations` event-side, `critterstack-arch-new-project-wolverine-polecat` bootstrap) plus a substantial "**Cab's coverage substantially exceeds ai-skills' coverage of the document database**" framing paragraph enumerating the 9 doc-store surfaces Cab adds. Removed 10th forward-looking placeholder (same broken pattern as Skill 15 — placeholdered names completely wrong vs actual ai-skills names; second occurrence in Phase 5). Net file +2,440 bytes — comprehensive Cab content + new substantive Upstream block produces net-positive byte change (methodology refinement #8 territory). **One consolidated upstream-contribution candidate flagged** (entry #32, **Observed gap** priority): the entire `polecat-document-store` surface as an entire-skill-creation candidate, parallel to `wolverine-sagas` (entry #9). Reasoning: the patterns hang together as a coherent topic; fragmenting into 7-8 sub-candidates would risk losing the "what's in/out of Polecat vs Marten" calibration framing that depends on the surface being viewed as a whole. **No new Cab coverage gaps revealed** — Skill 15 already surfaced the relevant doc-store-adjacent gaps. **No new ai-skills content drift** — the Skill 14/15 DCB-Polecat-only drift doesn't surface here because doc-store-side audit doesn't traverse DCB content. Preserved entirely: top framing, When to apply, Polecat document-store in CritterCab, Sessions (lightweight default + identity-map + query), Storing and loading documents, Identity strategies, LINQ querying with Marten differences, Raw SQL via `IAdvancedSql`, Soft deletes (3 opt-ins + LINQ extensions + restoration + HardDelete), Patching (full operations + by-predicate), Optimistic concurrency (IVersioned vs IRevisioned + auto-detection + mutual exclusivity), What's NOT in Polecat that's in Marten (calibration), Cab use case Payments BC documents, Common pitfalls (12 bullets). **Tier 2 complete (2/2).** |
| 17 | `csharp-coding-standards` | 3 | _none — out-of-scope topic for ai-skills_ | **No equivalent — out-of-scope sub-category (new in Phase 5)** | ~1 line removed (tangential ai-skills External reference); file 28,888 → 28,771 bytes (−117 bytes) | **Lightest pass in Phase 5 to date** — even lighter than Skills 4/5/8 rename-only passes because no `Upstream` block exists to rename (foundational skill, nothing to load first) and no forward-looking placeholder to remove (first Phase 5 skill with none present). Single edit: removed tangential `ai-skills wolverine-handlers-fundamentals` reference from External (redundant with the Downstream chain through `wolverine-handlers` which has its own proper Upstream block). All 18 H2 sections preserved entirely — every section is general C# / .NET 8+ / .NET 10 idiom or Cab-specific design convention with zero ai-skills overlap. Coding standards are project-specific by nature; ai-skills' charter is Critter Stack ecosystem (Wolverine + Marten + Polecat + integrations + testing + observability + critterstack-arch), not general .NET. **No upstream-contribution candidates** — out-of-scope topic doesn't generate candidates because flagging would require ai-skills to expand into general .NET coding standards, which isn't the library's purpose. **New "No equivalent" sub-category surfaced** (methodology refinement #10): "out-of-scope topic for ai-skills" — distinct from the prior three sub-shapes (in-scope unauthored Active per Skills 4/5; in-scope unauthored Observed gap per Skills 8/16; out-of-scope here). **Tier 3 begun (1/18).** |
| 18 | `domain-event-conventions` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 19 | `event-modeling` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 20 | `protobuf-contracts` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 21 | `transport-selection` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 22 | `grpc-vs-other-transports` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 23 | `adding-a-service` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 24 | `service-bootstrap` | 3 | `critterstack-arch-new-project-wolverine-marten` (primary) + `-polecat` + `wolverine-handlers-ioc-and-service-optimization` | Direct equivalent (deduplicated; **hybrid skill** — significant mechanic overlap on Marten/Polecat bootstrap basics + Cab-specific composition framing) | ~1 line removed (External cleanup; 3 ai-skills entries promoted to Upstream) + ~5 lines added in Marten section cross-ref note + ~5 lines added in Upstream block; net file +2,206 bytes (+11.4%) | Hybrid skill — Cab covers what ai-skills covers (AddMarten options, IntegrateWithWolverine, CritterStackDefaults, RunOaktonCommandsAsync) plus Cab-specific framing (6-step Service Composition Contract, Aspire-injected configuration with explicit null checks, environment-aware schema management with explicit production migration rationale, environment-aware ServiceLocationPolicy, Polecat-Backed Service Differences section, per-service variation patterns for gRPC/Kafka/publish-only/BFF, connection-string casing convention snake_case Postgres vs PascalCase SQL Server, 8-bullet Common Pitfalls). Restructured See Also to three-block convention with **3-entry Upstream block** (the three ai-skills bootstrap counterparts already referenced in External). Added cross-ref callout in Marten section pointing to ai-skills for greenfield-recommended options Cab doesn't enumerate. **Multiple Cab coverage gaps confirmed** (entries #5 [closed for Marten greenfield options bundle], plus index-registration gap from Skill 12 audit prediction validated): Cab service-bootstrap doesn't enumerate `EventAppendMode.Quick`, `UseArchivedStreamPartitioning`, `EnableEventSkippingInProjectionsOrSubscriptions`, `Projections.UseIdentityMapForAggregates`, `Projections.EnableAdvancedAsyncTracking`, `DisableNpgsqlLogging`, `Durability.EnableInboxPartitioning`, `InboxStaleTime`/`OutboxStaleTime`, `UnknownMessageBehavior = DeadLetterQueue`, `Policies.AutoApplyTransactions()` policy, or `WolverineFx.Http.Marten` metapackage convention with version-alignment warning. **Four upstream-contribution candidates flagged** (entries #36-39): (1) 6-step Service Composition Contract framing as universal floor for any service; (2) Aspire-injected configuration with explicit null-check at startup pattern; (3) Environment-aware ServiceLocationPolicy (warn dev, hard-fail prod); (4) Connection-string casing convention (snake_case Postgres vs PascalCase SQL Server). No forward-looking placeholders removed (none present). No ai-skills content drift. Methodology refinement #8 territory (net-positive bytes for hybrid bootstrap skill due to substantive Upstream block + cross-ref note). Preserved entirely: top framing with explicit boundary vs `adding-a-service`, When-to-apply / does NOT govern, Service Composition Contract 6 steps, full Marten-Backed Service Canonical Program.cs Trips example, Polecat-Backed Service Differences, Per-Service Configuration Variation 4 patterns, Observability Bootstrap, Health Check Conventions, Test Project Composition, 8-bullet Common Pitfalls. |
| 25 | `vertical-slice-organization` | 3 | `critterstack-arch-vertical-slice-fundamentals` | Direct equivalent (deduplicated; partial overlap — ai-skills covers the introductory layer, Cab covers project-specific conventions) | ~1 line removed (tangential External ref) + ~1 line added (Core Principle cross-ref) + ~6 in new Upstream block; net file +799 bytes (+4.4%) | **First Tier 3 partial-overlap reconciliation.** Cab's 18 KB skill applies VSA fundamentals with project-specific conventions; ai-skills' 12 KB counterpart covers the introductory layer (VSA principle, Wolverine-vs-MediatR comparison, slice testing, why-not-Clean/Onion/Hexagonal, decision guidance). Light pass: restructured See Also to three-block convention with new 1-entry Upstream block; existing Upstream → Prerequisites; dropped tangential `ai-skills wolverine-handlers-fundamentals` from External (covered transitively via `wolverine-handlers` Cab skill); added Core Principle cross-ref note. **Three upstream-contribution candidates flagged** (entries #33-35): (1) type-grouped files anti-pattern with merge-conflict framing; (2) migration-suffix naming prohibition; (3) AI Agent counter-pressure framing — meta-relevant since ai-skills is loaded by AI agents during scaffolding. No forward-looking placeholders removed (none present). No Cab coverage gaps revealed. Methodology refinement #8 territory (net-positive bytes for design-and-conventions skill). Preserved entirely: all 14 sections including scope demarcation, capability-folder naming, file naming reference table, validator-nested-inside-command rule, type-grouped files anti-pattern, domain event conventions, Slices Stay Within One Service (ADR-002), Shared/ 3+ threshold, no migration suffixes, acceptable technical folders, AI Agent counter-pressure, Trips Service worked example, 7-bullet pitfalls. |
| 26 | `distributed-saga-considerations` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 27 | `identity-acl` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 28 | `polyglot-go-service` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 29 | `aspire` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 30 | `cli-aspire` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 31 | `cli-jasperfx` | 3 | `wolverine-observability-command-line-diagnostics` | Direct equivalent (deduplicated; **hybrid skill** — Cab is the comprehensive canonical reference for all five CLI contributors, ai-skills focuses on the Wolverine subset with unique coverage of resiliency-policy + programmatic APIs) | ~5 lines added (Additional Surfaces sub-section) + ~3 lines added in new Upstream block + ~1 line removed (vague "if/when JasperFx publishes" External note); net file +2,663 bytes (+6.3%) | Cab's 42 KB skill is the canonical reference covering all five contributors (JasperFx core / JasperFx.Events / Weasel / Wolverine / Marten); ai-skills' 14 KB skill focuses on the Wolverine slice with unique coverage Cab is missing. Restructured See Also to three-block convention with new 1-entry Upstream block. Added "Additional surfaces in ai-skills" sub-section under Wolverine diagnostics pointing readers to ai-skills for the gaps. Cleaned up vague External note that pre-dated Phase 5 reconciliation (Cab no longer needs to wonder "if/when JasperFx publishes" — the counterpart exists). **Three Cab coverage gaps surfaced** (entries #10, #11, #12): (1) `wolverine-diagnostics describe-resiliency` command — Cab doesn't cover this third sub-command alongside `describe-routing` and `codegen-preview`, which inspects error-handling and circuit-breaker config scoped to endpoint/message-type/`--all`; (2) programmatic CLI APIs (`opts.DescribeHandlerMatch`, `bus.PreviewSubscriptions`, `host.SetupResources`/`TeardownResources`/`ResetResourceState`, `IMessageStore.Admin.RebuildAsync`/`ClearAllAsync`) for in-process equivalents to CLI commands — Cab is CLI-focused; (3) disabling-persistence-for-codegen-runs guard (`Environment.GetCommandLineArgs().Contains("codegen")`) for codegen-time hosts that can't reach databases. **Four upstream-contribution candidates flagged** (entries #40-43): (1) Five-contributors framing (JasperFx core / JasperFx.Events / Weasel / Wolverine / Marten) — clarifies which extension contributes which command; (2) `db-apply` vs `resources setup` decision matrix (Weasel-specific vs everything-at-once); (3) `--no-launch-profile` invocation pattern with rationale (avoids URL/env-var conflicts); (4) CI/CD pipeline shape with exit code contract (pre-deploy schema gate, codegen integrity, PR validation, operational diagnostics). No forward-looking placeholders removed (none present). No ai-skills content drift. Preserved entirely: full surface across all five contributors including general lifecycle, Weasel schema management with `--database` scoping, `resources` lifecycle, `db-apply` vs `resources setup` decision matrix, `codegen` four actions, Wolverine diagnostics with `--handler`/`--route`/`--grpc` flags, `capabilities`, `storage` five actions + `clear-handled`, `projections` with five flags, `projections rebuild` vs daemon API decision matrix, `marten` healing flags, Composing CLI invocations with Aspire, CI/CD usage patterns, 17 Common Pitfalls. |
| 32 | `cli-grpc-tooling` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 33 | `cli-kafka-tooling` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 34 | `cli-azure-messaging` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 35 | `observability-tracing` | 4 | `wolverine-observability-opentelemetry-setup` | Direct equivalent (deduplicated; **partial overlap** — ai-skills combines tracing+metrics in one skill, Cab splits across observability-tracing + observability-metrics) | ~10 lines added (Marten async daemon spans cross-ref + per-message-type/InvokeAsync suppression cross-ref) + ~3 lines added in Upstream block + 1 line removed (forward-looking placeholder); net file growth | Hybrid skill — ai-skills combines OTEL tracing+metrics into one skill; Cab splits across two (Skill 35 tracing + Skill 36 metrics). Restructured See Also to three-block convention with new 1-entry Upstream block. **Removed forward-looking placeholders** for non-existent `opentelemetry-dotnet` and `aspire-observability` ai-skills. **Three Cab coverage gaps surfaced** (entries #13-15): (1) Marten async daemon spans (`marten.{name}.all.execution`/`loading`/`grouping` per-shard + `marten.daemon.highwatermark`) not enumerated in Cab's tracing skill; (2) `opts.InvokeTracing = InvokeTracingMode.Full` for mediator-mode `InvokeAsync()` not covered; (3) `[WolverineLogging(telemetryEnabled: false)]` attribute approach not covered (only per-endpoint flag covered). **Three upstream-contribution candidates flagged** (entries #44-46): (1) manual-AddSource as the single most important idea framing; (2) per-BC ActivitySource convention `CritterCab.{BcName}`; (3) trace context propagation diagram. No ai-skills content drift surfaced here. Preserved entirely: 3-layer foundation framing, full Bootstrap pattern, Wolverine spans (3 primary + semantic attributes + handler-specific + saga + activity events + operational), Marten spans (connection + verbose), Trace context propagation diagram, Per-BC conventions, Aspire dashboard view, Sampling strategy, Audit tags, 12 Common Pitfalls. |
| 36 | `observability-metrics` | 4 | `wolverine-observability-metrics-and-auditing` (primary) + `wolverine-observability-grafana-dashboard-templates` | Direct equivalent (deduplicated; **Cab substantially exceeds ai-skills** — Cab is canonical comprehensive metrics reference; ai-skills covers basic surface + audit middleware + Grafana dashboards) | ~5 lines added (Upstream block with Cab-exceeds framing + ai-skills-content-drift call-out) + 1 line removed (forward-looking placeholder); net file growth | Cab's 45 KB metrics skill substantially exceeds ai-skills' coverage. Restructured See Also to three-block convention with new 2-entry Upstream block. **Removed forward-looking placeholder** for non-existent `ai-skills observability-metrics`. **ai-skills content drift surfaced** (extending tracker entry from Skill 14): `wolverine-observability-metrics-and-auditing` incorrectly uses `AddMeter("Wolverine")` exact-match; Wolverine's Meter is `Wolverine:{ServiceName}` requiring wildcard `"Wolverine:*"`. ai-skills' separate `wolverine-observability-opentelemetry-setup` correctly notes the per-service-suffix — the two ai-skills are internally inconsistent. Worth flagging to JasperFx. **Three Cab coverage gaps surfaced** (entries #16-18): (1) audit middleware logging pattern (`AuditMiddleware` with Before/Finally + Stopwatch); (2) writing audit events to Marten via After hook; (3) Prometheus exporter setup with `MapPrometheusScrapingEndpoint()`. (Note: Grafana PromQL examples likely covered by `wolverine-observability-grafana-dashboard-templates`; not separately tracked.) **Five upstream-contribution candidates flagged** (entries #47-51): (1) wildcard `AddMeter("Wolverine:*")` registration with rationale; (2) per-BC custom Meter naming convention `CritterCab.{BcName}` matching ActivitySource; (3) Polecat 3.1 `IDocumentStoreUsageSource` discovery surface; (4) Wolverine instrument view bucket boundaries with Cab-tuned defaults for `wolverine-execution-time`; (5) cardinality risk analysis with Metric View `TagKeys` filtering. No forward-looking placeholders present after cleanup. Preserved entirely: full Wolverine instrument catalog (11 instruments incl. observable gauges with `.{databaseName}` suffix), Marten instruments (event.append + projection counters), Polecat 3.1 monitoring discovery (DocumentStoreUsage + EventStoreUsage descriptors), `EnableExtendedProgressionTracking` daemon health columns, ASP.NET Core / HTTP / runtime instrumentation, per-BC custom Meter convention, Aspire dashboard view, view bucket boundaries + cardinality, cross-language metrics from `cab-go`, 12 Common Pitfalls. |
| 37 | `testing-fundamentals` | 4 | _none — ai-skills doesn't currently publish a unit-testing-fundamentals skill_ (most relevant adjacent: `wolverine-testing-test-parallelization`, `wolverine-testing-integration`) | **No equivalent — out-of-scope sub-category** (ai-skills' testing surface starts at integration level; Cab's pure-function unit testing fundamentals are project-specific) | ~6 lines added (Upstream block with adjacent ai-skills + Cab-scope framing) + ~1 line removed (forward-looking placeholder); net file growth | Restructured See Also to three-block convention. **Removed forward-looking placeholder** for non-existent `critter-stack-testing-fundamentals`. New Upstream block lists adjacent ai-skills (`wolverine-testing-test-parallelization` for parallelization patterns; `wolverine-testing-integration` for the integration follow-up Cab defers to). **Sub-category: out-of-scope topic for ai-skills** (refinement #10 territory; ai-skills covers integration testing, framework pickers, and parallelization but not pure-function unit testing fundamentals which is generic .NET testing). No upstream-contribution candidates flagged — unit testing patterns are largely generic .NET hygiene. No Cab coverage gaps revealed. Preserved entirely: committed test stack (xUnit 2.9.3, Shouldly, Testcontainers 4.11.0, FakeTimeProvider), per-service test project organization, xUnit lifecycle (IAsyncLifetime/IClassFixture/ICollectionFixture), Unit testing pure handlers with full StartTrip example, Aggregate Apply tests, Testing validators, FakeTimeProvider per-test instantiation rule, Testing failure paths (validation/Before short-circuit/exceptions), Shouldly conventions, 11 Common Pitfalls. |
| 38 | `testing-integration` | 4 | `wolverine-testing-integration` (primary) + `-integration-marten` + `-with-testcontainers` + `-with-aspire` + `-test-parallelization` | Direct equivalent (deduplicated; **partial overlap** — ai-skills covers fragmented integration testing topics across 5 skills, Cab provides comprehensive single-skill coverage with project-specific TestFixture pattern) | ~7 lines added (Upstream block with 5-entry framing) + ~1 line removed (forward-looking placeholder); net file growth | Cab's 40 KB integration testing skill consolidates patterns spread across 5 ai-skills. Restructured See Also to three-block convention with new 5-entry Upstream block. **Removed forward-looking placeholder** for non-existent `critter-stack-integration-testing`. ai-skills counterparts: `-integration` (baseline `IAlbaHost.For<Program>`, `ExecuteAndWaitAsync`, tracked sessions, `RunWolverineInSoloMode`, `DisableAllExternalWolverineTransports`), `-integration-marten` (`CleanAllMartenDataAsync` vs `ResetAllMartenDataAsync`, `WaitForNonStaleProjectionDataAsync`, `IInitialData`, the race condition), `-with-testcontainers` (Postgres/SQL Server/Kafka/ServiceBus container builders, image pinning, unique naming), `-with-aspire` (Aspire-orchestrated test composition), `-test-parallelization` (xUnit parallelization strategies). Cab's skill applies these with project-specific framing: per-service TestFixture pattern with Testcontainer-per-fixture; `JasperFxEnvironment.AutoStartHost = true` requirement; `ConfigureServices`-not-`ConfigureAppConfiguration` connection-string override rule; `MartenDaemonModeIsSolo()` for solo daemon; dispose-path exception swallows for shutdown races; Strategy 1 (sequential, default) vs Strategy 2 (parallel with unique IDs) call-outs; tracked-session timeout adjustments under parallel load. No upstream-contribution candidates flagged — most Cab-specific framings are project-implementation details. No Cab coverage gaps revealed (Cab consolidates all relevant ai-skills surfaces). Preserved entirely: integration test mental model, full TestFixture pattern with Trips example, Collection Definition + Test Class Lifecycle, race condition every event-sourced test hits, `ExecuteAndWaitAsync` API, tracked-session configuration (Timeout/IncludeExternalTransports/AlsoTrack/DoNotAssertOnExceptionsDetected), async projection waiting (`WaitForNonStaleProjectionDataAsync`/`WaitForConditionAsync`/`PauseThenCatchUpOnMartenDaemonActivity`), HTTP scenarios via Alba, scheduled message testing (`PlayScheduledMessagesAsync`), Testcontainers patterns, IInitialData seeding, Parallelization strategy, 12 Common Pitfalls. |
| 39 | `testing-advanced` | 4 | `wolverine-testing-alba` + `wolverine-testing-integration` + `wolverine-testing-test-parallelization` + `wolverine-testing-with-testcontainers` (foundational; advanced patterns largely Cab-specific) | Direct equivalent (deduplicated; **partial overlap** — ai-skills covers foundation that testing-advanced extends; advanced patterns themselves don't have ai-skills counterparts) | ~6 lines added (Upstream block with 4 foundational ai-skills + Cab-extends framing) + ~1 line removed (forward-looking placeholder); net file growth | Restructured See Also to three-block convention. **Removed forward-looking placeholder** for non-existent `ai-skills testing-advanced`. New 4-entry Upstream block listing foundational ai-skills (Alba, integration, parallelization, Testcontainers) that this skill extends. Most of testing-advanced's surface is genuinely advanced and Cab-specific without direct ai-skills counterparts: multi-host `TrackActivity().AlsoTrack()` scenarios, gRPC streaming test harnesses (in-process via `WebApplicationFactory<Program>` for unary/server-streaming/bidirectional/client-streaming), dynamic database-per-fixture isolation (PostgreSQL schema-per-fixture + SQL Server database-per-fixture), OpenTelemetry in-memory exporter verification of span trees and metric counters with `MeterListener`, polyglot boundary tests (`cab-go` alongside .NET hosts via Testcontainers), OpenIddict-shaped JWT test-token factories, middleware-based fault injection. **Two upstream-contribution candidates flagged** (entries #52-53): (1) namespace-per-fixture isolation pattern as the canonical parallel-fixture-isolation technique (RabbitMQ vhost / Kafka topic prefix / ASB queue prefix / database+schema scoping); (2) `host.TrackActivity().AlsoTrack(host2, host3)` multi-host pattern with `WaitForMessageToBeReceivedAt<T>(host)` per-host wait conditions — Wolverine 5.32+ surface that ai-skills doesn't cover. No Cab coverage gaps revealed (advanced patterns are Cab-specific by definition). No ai-skills content drift surfaced. Preserved entirely: Multi-host scenarios with TrackActivity.AlsoTrack, gRPC streaming test harnesses, Dynamic database-per-fixture (PostgreSQL schema-per-fixture + SQL Server), RabbitMQ vhost isolation, Testcontainers Kafka and ASB patterns with IncludeExternalTransports, Test-token factories for OpenIddict-shaped JWTs, Advanced saga timeout testing, Span and metric verification with OTel in-memory exporter, Polyglot boundary tests with cab-go, Failure-injection patterns. **Tier 4 complete (5/5).** **Erik's queue complete — Skills 24, 25, 31, 35-39 reconciled.** |

---

## Per-skill detail

### 1. `wolverine-handlers`

**Counterpart(s).** Cab's hub skill maps to a fragmented set of 9 ai-skills:

- `wolverine-handlers-fundamentals` — the closest single counterpart (handler shape, discovery, return types, Logger convention).
- `wolverine-handlers-pure-functions` — decider pattern, A-Frame.
- `wolverine-handlers-a-frame-architecture` — infrastructure-at-edges principle.
- `wolverine-handlers-railway-programming` — `Validate` / `ProblemDetails` pipeline.
- `wolverine-handlers-declarative-persistence` — `[Entity]`, `[WriteAggregate]`, `[ReadAggregate]`.
- `wolverine-handlers-middleware` — full lifecycle, `OnException`, scoping.
- `wolverine-handlers-ioc-and-service-optimization` — service location, codegen, Lamar.
- `marten-aggregate-handler-workflow` — full aggregate workflow.
- (`wolverine-handlers-efcore`, `query-plans` — orthogonal; not Cab-relevant.)

Cab's hub-skill structure is structurally compatible: it acts as a Cab-specific orienting layer that defers protocol-specific work to siblings (`wolverine-http-handlers`, `wolverine-messaging-handlers`, `wolverine-grpc-handlers`, `wolverine-grpc-bidirectional-handlers`) and generic mechanics to the 8 ai-skills above.

**Section categorization.**

| Section | Category | Action |
|---|---|---|
| Top framing + sibling list | Cab-specific | Kept; already serves as the "why this skill exists alongside ai-skills" paragraph |
| When to apply this skill | Cab-specific | Kept |
| Cab-specific orientation (`MartenOps`/`PolecatOps` deltas, `[WriteAggregate]` parameter-level decision, recommended ai-skills reading order) | Cab-specific | Kept |
| CritterCab Handler Shape (`AcceptOffer` example + 6 bullets) | Cab-specific | Kept |
| Validate vs ValidateAsync (three-layer FluentValidation→Validate→ValidateAsync ordering) | Cab-specific extension | Kept |
| Handler Return Types — Orientation (table) | Cab-specific projection of return types onto Cab shapes | Kept |
| Anti-Pattern: `IStartStream` not returned | Upstream-contribution candidate (Marten-side) + Cab-specific (Polecat-side) | Kept; flagged for upstream contribution |
| Anti-Pattern: Lambda Factory Service Registrations | Duplicates ai-skills `wolverine-handlers-ioc-and-service-optimization` | **Trimmed** to brief Cab convention + upstream pointer |
| Logger Convention | Duplicates ai-skills `wolverine-handlers-fundamentals` | **Trimmed** to single-line note + upstream pointer |
| Diagnosing Handler Issues | Cab-specific framing (symptom→command table) | Kept |
| See Also › External (8 ai-skills entries) | Restructure | **Promoted** to new `Upstream (ai-skills)` block |

**Trim impact.** ~18 lines removed (Lambda Factory section ~22 → ~5; Logger Convention ~6 → 2; External → restructured into Upstream (ai-skills) + minimal External, net ~+3 from new heading and framing). Skill went from 322 → ~304 lines.

**Convention established.** This skill set the Phase 5 `See Also` convention. After Erik's review, the convention was refined to a cleaner three-block structure (instead of the awkward two-`Upstream` form initially applied):

- **`Upstream`** — ai-skills counterparts (the authoritative external reference). Comes first in `See Also`. License/install note in the lead-in line; entries drop the `ai-skills` prefix (heading establishes scope).
- **`Prerequisites`** — Cab-internal load-first skills (renamed from what Phases 1–4 called `Upstream`).
- **`External`** — non-ai-skills external references only (Wolverine docs link, blog posts).

The `Upstream` → `Prerequisites` rename is part of every Phase 5 skill reconciliation. Subsequent skills follow the same three-block layout and ordering.

**Upstream-contribution candidate flagged.** The `session.Events.StartStream<T>(...)` direct-call silent-failure footgun (Marten-side) belongs in a future ai-skills `wolverine-handlers-fundamentals` or `marten-aggregate-handler-workflow` revision. Cab will continue carrying it until ai-skills covers it.

---

### 2. `wolverine-http-handlers`

**Counterpart(s).** Three direct counterparts in ai-skills:

- `wolverine-http-fundamentals` — generic HTTP integration: routing, parameter binding, OpenAPI inference from signatures, ProblemDetails-based validation, return type conventions.
- `wolverine-http-marten-integration` — aggregate handler endpoints (`[Aggregate]`/`[WriteAggregate]`), aggregate identity resolution chain, `UpdatedAggregate`, document operations.
- `wolverine-http-hybrid-handlers` — single handler serving both HTTP and message-bus paths via `[WolverineVerb]`, `MiddlewareScoping` for context-gated middleware.

Note: ai-skills uses `[Aggregate]` and `[WriteAggregate]` interchangeably across the marten-integration skill (mostly `[Aggregate]`); Cab standardized on `[WriteAggregate]` per `wolverine-handlers`. Cab convention preserved across edits.

**Section categorization.**

| Section | Category | Action |
|---|---|---|
| Top framing + When to apply | Cab-specific | Kept (already names ai-skills counterparts as "Do NOT use this skill for" pointers) |
| Anti-Pattern: Wrong Tuple Order | Cab-specific articulation of an ai-skills rule (footgun framing) | Kept |
| Anti-Pattern: Mixed Route + JSON Body on Compound Handlers | **Upstream-contribution candidate** — limitation absent from ai-skills | Kept; flagged for upstream contribution |
| Anti-Pattern: Bare Event Return on Aggregate-Handler HTTP Endpoint | Duplicates ai-skills (covered in both `fundamentals` + `marten-integration`) | **Trimmed** — kept brief callout + Cab `[WriteAggregate]` example with `TripCancelled`; deferred elaborate two-correct-shape treatment to ai-skills cross-reference |
| Concrete Return Types vs `IResult` (with mapping table) | Duplicates ai-skills (covered comprehensively in `fundamentals`) | **Trimmed heavily** — short Cab note + cross-reference; dropped the table |
| Route Binding (binding rule + `nameof()` convention + identity resolution chain) | Generic mechanics duplicate ai-skills; `nameof()` convention is Cab-specific | **Trimmed and renamed** to "Aggregate ID — Cab Convention" — kept only Cab's `nameof()`-for-refactor-safety convention; cross-referenced ai-skills for binding rules and identity resolution |
| Diagnosing Endpoint Issues | Cab-specific framing | Kept |
| See Also | Restructure | Applied three-block convention from Skill 1 (`Upstream` ai-skills → `Prerequisites` Cab-internal → `Sibling skills` → `Downstream` → `External`) |

**Trim impact.** ~60 lines removed across three sections (Bare Event Return ~38 → ~14; Concrete Return Types vs IResult ~30 → ~10; Route Binding ~22 → ~12, renamed). File went from 11,829 → 10,567 bytes (~10% size reduction).

**Upstream-contribution candidates flagged.**

1. **Mixed route + JSON body breaks compound handlers.** The compound handler pattern (`Validate`/`Handle` shared-state) fails when the endpoint mixes route parameters and a JSON body — Wolverine's parameter resolution can't see across the route-vs-body boundary at the validation step. Genuine framework limitation absent from ai-skills `wolverine-http-fundamentals`.
2. **Tuple-ordering silent failure on HTTP endpoints.** ai-skills states the rule ("first element of a tuple is the HTTP response body") but doesn't articulate what fails silently when the rule is violated — `IStartStream` getting serialized to the response body and the stream never starting is hard to diagnose without the explicit footgun callout. Closer call than #1; the rule is in ai-skills but the failure-mode framing is Cab-specific.

---

### 3. `wolverine-messaging-handlers`

**Counterpart(s).** Two direct counterparts in ai-skills:

- `wolverine-messaging-message-routing` — bus method semantics, routing rule precedence, endpoint types, transactional outbox/inbox, message scheduling, partitioned messaging, topic publishing. Direct counterpart.
- `wolverine-messaging-resiliency-policies` — retry strategies, circuit breakers, dead letter queues, compensating actions. **Cab does not have a parallel skill.** Per Erik's call, this is a future Cab skill candidate but not a Phase 5 priority. The ai-skills counterpart is referenced in Cab's new `Upstream` block with a note about the missing Cab parallel.

**Section categorization.**

| Section | Category | Action |
|---|---|---|
| Top framing + When to apply + Do NOT use this skill for | Cab-specific | Kept (already names ai-skills counterparts) |
| The Routing Rule Pre-Flight (footgun + 4-step checklist) | Cab-specific articulation + checklist; **upstream-contribution candidate** | Kept; flagged for upstream contribution |
| OutgoingMessages: Transactional Outbox | Mostly duplicates ai-skills | **Trimmed** — kept Cab BC example, consolidated paragraphs, added cross-reference |
| Anti-Pattern: `bus.PublishAsync` Inside a Handler | Duplicates ai-skills | **Trimmed heavily** — dropped 15-line WRONG/CORRECT code block, kept brief callout + ScheduleAsync exception note + cross-reference |
| Anti-Pattern: `bus.InvokeAsync` for Fire-and-Forget Work | Duplicates ai-skills (which covers it more thoroughly with nested-transaction risk) | **Trimmed** — collapsed code example to minimal Cab BC, integrated nested-transaction risk into prose, cross-referenced ai-skills for elaboration |
| `bus.*` Method Decision Matrix (table) | Cab-specific projection (adds cascading-via-`OutgoingMessages` row that ai-skills' equivalent table doesn't have) | Kept (genuine value-add) |
| ScheduleAsync for Delayed Delivery | Largely duplicates ai-skills | **Trimmed** — kept Cab BC `DispatchOffer`/`ExpireOfferIfNotAccepted` example, dropped redundant explanation, integrated routing-rule reminder + cross-reference |
| Inbound Message Handlers (idempotency + aggregate-stream-existence guard) | Cab-specific (idempotency-via-stream-existence is Cab convention) | Kept |
| Diagnosing Messaging Issues | Generic CLI duplicates ai-skills; symptom table is Cab-specific framing | **Trimmed** — kept symptom table, reduced 3 CLI commands to 1 canonical example, added ai-skills CLI cross-reference |
| See Also | Restructure | Applied three-block convention; expanded `Upstream` block to include `wolverine-messaging-resiliency-policies` despite no Cab parallel |

**Trim impact.** ~26 lines removed (substantially less than the ~50 estimated). File went from 15,072 → 14,963 bytes (~0.7% size reduction). The line-count trim is real but the byte-count reduction is small because cross-reference prose and the expanded `Upstream` block compensate for code-block removal. See methodology refinement #3.

**Upstream-contribution candidate flagged.** The `OutgoingMessages`-without-routing-rule silent-failure footgun. ai-skills articulates `PublishAsync`'s "silent if no subscribers" semantic in the bus-methods comparison table, but doesn't frame the parallel scenario for cascading returns via `OutgoingMessages` where a missing routing rule causes silent message loss. This is the more common Cab failure mode (because `OutgoingMessages` is the preferred pattern, not `PublishAsync`) and the framing as a pre-flight checklist could benefit ai-skills.

**Cab coverage gap noted.** No dedicated `messaging-resiliency` Cab skill exists. Per Erik's call, this is a future Cab skill candidate but deferred past Phase 5. The Cab `wolverine-messaging-handlers` skill points readers to the ai-skills counterpart directly.

---

### 4. `wolverine-grpc-handlers`

**Counterpart(s).** None. ai-skills does not currently have any `wolverine-grpc-*` skill. Erik confirmed he is the planned author of the future ai-skills `wolverine-grpc` skill, after which Cab `wolverine-grpc-handlers` should be revisited for true reconciliation.

**Section categorization.** No section trims applied — every section is Cab-specific (proto-first conventions per ADR-009, the `[WolverineGrpcService]` stub pattern, AIP-193 exception mapping with Cab's `opts.MapException<T>()` overrides, Aspire HTTPS dev-cert wiring, Cab BCs throughout). The skill stands as the authoritative reference until ai-skills publishes a parallel.

**Edits applied (light pass).**

1. **Rename `Upstream` → `Prerequisites`** in the See Also block per the Phase 5 convention.
2. **Remove forward-looking placeholder** for the not-yet-published ai-skills `wolverine-grpc` skill (the line read "ai-skills `wolverine-grpc` — generic Wolverine.Grpc patterns if/when JasperFx publishes one. Complements this skill."). The placeholder misleads readers into searching for a skill that doesn't exist; better to add the entry once ai-skills publishes.
3. **Remove standard install/license note** ("All ai-skills installed via `npx skills add` (license required).") since the External block now contains only public-web links.

**No new `Upstream` block** — nothing to put in it. The Cab skill is the authoritative reference today.

**Trim impact.** ~3 lines removed (rename-only pass; no body trim). Skill is 39 KB, the largest Cab skill so far; no body content needed reconciliation.

**Upstream-contribution status.** Erik is the planned author of the future ai-skills `wolverine-grpc` skill. This is **active upstream work**, not a Phase-5 discovery to add to the post-Phase-5 backlog. When that ai-skills skill ships, the Cab skill should be revisited and likely thinned to its Cab-specific layer (Aspire dev-cert, AIP-193 mapping, ADR-009 proto-first conventions, Cab BC examples).

---

### 5. `wolverine-grpc-bidirectional-handlers`

**Counterpart(s).** None. Same status as Skill 4: ai-skills has no `wolverine-grpc*` skill today, and Erik is the planned author of the future ai-skills `wolverine-grpc` skill. That single ai-skills skill will likely cover both this skill's content (client-streaming hand-written workaround for Wolverine 5.32's missing auto-generation, plus bidirectional patterns) and Skill 4's content (unary + server-streaming auto-generated path).

**Section categorization.** No section trims applied — every section is Cab-specific (proto-first hand-written client-streaming workaround, `IAsyncStreamReader<T>` draining patterns, AIP-193 interceptor on hand-written chains, Cab BCs `PushTelemetry`/driver-rider real-time exchange). The 32 KB skill stands as the authoritative reference until ai-skills publishes a parallel.

**Edits applied (light pass, identical to Skill 4).**

1. **Rename `Upstream` → `Prerequisites`** in the See Also block.
2. **Remove forward-looking placeholder** for the not-yet-published ai-skills `wolverine-grpc` skill (same line as Skill 4).
3. **Remove standard install/license note** (no longer needed when External has only public-web links).

**No new `Upstream` block** — nothing to put in it.

**Trim impact.** ~3 lines removed (rename-only pass). 32 KB skill, content unchanged.

**Upstream-contribution scope.** Folded into the existing roadmap entry #5, which now scopes both Cab gRPC skills as the Cab-side basis for Erik's planned ai-skills `wolverine-grpc` work.

---

### 6. `wolverine-kafka`

**Counterpart(s).** Two ai-skills counterparts:

- `wolverine-integrations-kafka` — generic Wolverine + Kafka transport: setup, topic binding, consumer groups, partition-based sequential processing, delivery semantics, raw JSON interop, tombstones, multi-region named brokers. Direct counterpart.
- `wolverine-messaging-resiliency-policies` — retry/circuit-breaker/DLQ. **No Cab parallel** (same scenario as Skill 3); the Cab skill cross-references retry policies and circuit breakers, so the resiliency-policies upstream entry is included in the new `Upstream` block.

**Section categorization.** The Cab Kafka skill had the most generic-mechanic duplication of any reconciled skill so far. ~30 sections audited; ~17 substantively trimmed; rest kept as Cab-specific value-add.

**Trimmed sections (~125 raw lines removed):**

- Bootstrap > Direct connection (Cab BC code block dropped, cross-referenced)
- Bootstrap > AutoProvision (mechanic explanation tightened; **EH Emulator constraint kept** as the Cab-discovered value-add)
- Bootstrap > ConsumeOnly (collapsed to 1-line + cross-ref)
- Publishing > Named topic routing (kept Cab BC routing rules; trimmed mechanic explanation)
- Publishing > Convention-based routing (collapsed to brief Cab-doesn't-use note)
- Listening > Single-topic listeners (kept Cab `LocationPingHandler` example; trimmed redundant explanation)
- Listening > Multi-topic listeners (collapsed to brief note + cross-ref)
- Listening > Batch processing (kept Cab BC `LocationPingBatchHandler` example; tightened explanation)
- Consumer groups > Transport-level override (collapsed to 1-line + cross-ref)
- Consumer groups > Per-topic override (kept the "replaces parent" caveat in prose; dropped code example, cross-ref to ai-skills)
- Consumer groups > GroupId stamping (collapsed to brief note + cross-ref)
- Serialization > Default envelope serialization (tightened; framed as Cab default)
- Serialization > Raw JSON interop (kept Cab BC publisher/listener pair; dropped redundant explanation)
- Serialization > Custom envelope mapper (collapsed to escape-hatch note)
- DLT > Native DLT (collapsed to single paragraph + cross-ref)
- DLT > Retry policies (collapsed; cross-ref to `wolverine-messaging-resiliency-policies`)
- DLT > Circuit breakers (collapsed; cross-ref to resiliency-policies)
- Tombstones (collapsed mechanic; **kept Cab note** that GPS pings/demand signals use time-based retention)
- See Also (restructured to three-block convention; added `wolverine-messaging-resiliency-policies` to `Upstream`)

**Preserved entirely:**

- Top framing + When to apply + Mental model diagram
- Bootstrap > Aspire-injected connection (`UseKafkaUsingNamedConnection`)
- Topic naming convention (`<bc>.<descriptive-name>`) + Cab BC topic table
- Publishing > Partition keys (driver_id/zone_id rationale)
- Listening > ProcessInline (high-throughput vs durable-inbox decision framing)
- Consumer groups > Default group ID (per-service convention)
- Serialization > Schema Registry (out-of-scope rationale + protobuf forward-look)
- DLT > Poison pill handling (acceptable-loss tradeoff for GPS streams)
- Azure Event Hubs section (EH Emulator + `ConfigureClient` SASL pattern)
- Tracing
- Common pitfalls (12 bullets, mostly Cab-specific value-adds)

**Trim impact.** ~125 raw lines removed, file 26,990 → 25,542 bytes (~5.4% size reduction — the largest reduction in Phase 5 so far). The 50% rule of thumb (methodology refinement #3) **did not apply** here because most trims removed code blocks (which are short, dense lines) rather than prose; for skills with heavy code-block duplication, actual line trim approaches `~1.0 × raw_lines_removed`. Methodology refinement updated below.

**Upstream-contribution candidates flagged.** Two:

1. **EH Emulator AutoProvision constraint.** ai-skills `wolverine-integrations-kafka` doesn't mention Azure Event Hubs Emulator at all. The constraint that the EH Emulator supports producer/consumer Kafka APIs but NOT admin APIs (so `AutoProvision` fails) is a genuine pitfall for any Wolverine+Kafka deployment targeting Azure. Cab's framing ("use real Kafka container locally; switch to EH Emulator only for EH-specific integration tests") is also Cab-discovered guidance worth upstreaming.
2. **`BatchMessagesOf<T>` for batch consumption.** ai-skills omits this entirely. The mechanic is generic Wolverine; Cab's framing pairs it with high-volume Kafka topics. Both the mechanic existence and the Kafka-pairing rationale could be upstream additions.

---

### 7. `wolverine-azure-service-bus`

**Counterpart(s).** Two ai-skills counterparts:

- `wolverine-integrations-azure-service-bus` — generic Wolverine + ASB transport. Direct counterpart.
- `wolverine-messaging-resiliency-policies` — retry/circuit-breaker/DLQ. **No Cab parallel** (third skill in Phase 5 to point to this; pattern from Skills 3 and 6).

**Section categorization.** Similar size and duplication shape to Kafka (Skill 6) — 28 KB / heavy code-block duplication. ~34 sections audited; ~17 substantively trimmed; 1 section **removed entirely** (see correction below); rest kept as Cab-specific value-add.

**Trimmed sections (~160 raw lines removed, 8.2% byte reduction — largest of Phase 5):**

- Bootstrap: Managed Identity, AutoProvision, AutoPurgeOnStartup, System queues
- Topics: Publishing, Subscribing, Subscription rules, Configuring subscription properties
- Queues: Publishing, Listening, Configuring queue properties (kept the duplicate-detection rationale)
- Sessions: Setting session ID, Sessions on subscriptions, ExclusiveNodeWithSessions
- DLQ: Native dead-lettering, Wolverine's DLQ routing, Circuit breakers (cross-ref to resiliency-policies)
- Scheduled delivery, Custom envelope mapper
- See Also (restructured to three-block convention; added `wolverine-messaging-resiliency-policies` to `Upstream`)

**Section removed entirely:**

- **MassTransit and NServiceBus interop.** The pre-Phase-5 Cab skill had a section covering `UseMassTransitInterop` and `UseNServiceBusInterop` framed as "migration aids." Erik flagged during the audit pause: **Cab does not use MassTransit or NServiceBus and never will.** The section was incorrect content that didn't reflect Cab's actual usage — likely drifted in from ai-skills or generic Wolverine documentation during the original authoring. Removed in full (not trimmed). This is the first content-correction edit in Phase 5; all prior trims have been about deduplication, not factual correction.

**Preserved entirely:**

- Top framing + Mental model + Aspire `Aspire.Hosting.Azure.ServiceBus` package-not-yet-committed status note
- When to apply
- Bootstrap > Connection string (local dev) (Aspire integration is Cab-specific)
- Topics > Convention-based topic routing (brief Cab-doesn't-use note)
- Sessions > When to use sessions (Cab BC examples) and Sessions > Enabling sessions on a queue (still has substantial Cab framing)
- DLQ > Retry policies (**MaxDeliveryCount-vs-Wolverine-retries footgun**, kept as the Cab value-add)
- Serialization > Default envelope mapping table (more comprehensive than ai-skills' equivalent)
- Local development > Aspire integration, Testcontainers, local-vs-production table (all Cab-specific)
- Tracing
- Common pitfalls (12 bullets)

**Trim impact.** ~160 raw lines removed, file 27,801 → 25,530 bytes (~8.2% size reduction — the largest in Phase 5). Confirms methodology refinement #6: code-block-heavy duplication compresses ~1:1.

**Upstream-contribution candidate flagged.** One:

1. **MaxDeliveryCount × Wolverine retries multiplicative interaction.** ai-skills mentions both retry types separately but doesn't articulate how they compose: a Wolverine retry of N inside ASB `MaxDeliveryCount` of M means up to N×M total processing attempts. This composition footgun is Cab-discovered framing.

**MT/NSB interop NOT flagged as upstream candidate.** Cab can't speak to ai-skills' coverage of tools Cab doesn't use. The fact that ai-skills also omits MT/NSB interop is observable but Cab is not the right source for an upstream contribution there.

---

### 8. `wolverine-sagas`

**Counterpart(s).** None. Search of ai-skills (`C:\Code\JasperFx\ai-skills\skills`) confirmed: no `wolverine-sagas-*` skills exist; only saga test fixtures (`03_saga.cs`) inside the MasterTransit/NServiceBus conversion skills. Erik confirmed during the audit that he had assumed a `wolverine-sagas-saga-pattern` counterpart existed but verification showed it doesn't. **No active author identified** — unlike the gRPC skills (Erik is the planned author), no one is currently writing a saga ai-skills skill that we know of.

**Section categorization.** No section trims applied. Every section is Cab-specific or covers Wolverine saga mechanics that ai-skills doesn't address. The 39 KB skill stands as the authoritative reference.

**Edits applied (light pass per methodology refinement #5).**

1. **Rename `Upstream` → `Prerequisites`** in the See Also block.
2. **Remove forward-looking placeholder** for the not-yet-published ai-skills `wolverine-sagas` skill (the line read "ai-skills `wolverine-sagas` — generic Wolverine saga patterns if/when JasperFx publishes one. Complements this skill.").
3. **Remove standard install/license note** since the External block now contains only public-web links.

**No new `Upstream` block** — nothing to put in it.

**ProcessManager<TState> references audit.** Per Erik's earlier guidance to remove framework-design references that cause confusion, the body was searched for `ProcessManager`, `process manager`, `Wolverine 6`, `future version`, `first-class`, etc. Findings:

- Line 5 (frontmatter `tags`): `process-manager` is one of 12 taxonomy tags for searchability. Generic pattern reference, not framework-design. **Retained.**
- Line 423 (Common pitfalls): "Sagas with twelve `Handle` methods become 'process managers that do everything' and lose the workflow clarity that justified the saga" — standard EIP/DDD pattern terminology used to describe an anti-pattern. **Retained.**
- No other forward-looking framework-design references found in the body.

No edits made on the ProcessManager front. The two surviving uses are legitimate pattern terminology, not the kind of "head's up about a planned framework type" content Erik wanted stripped.

**Trim impact.** ~3 lines removed (rename-only pass). 39 KB skill, content otherwise unchanged.

**Upstream-contribution status.** Recorded with **Observed gap** priority — ai-skills has no saga skill at all and there's no known active author. Different from gRPC (where Erik is the planned author, **Active** priority) and different from footgun-style additions (where the ai-skills skill exists, _TBD at close-out_ priority).

---

### 9. `marten-aggregates`

**Counterpart(s).** Two ai-skills counterparts cover the mechanics this skill defers to:

- `marten-projections-single-stream` — single-stream projection mechanics including Apply/Create method conventions and self-aggregating snapshot pattern; the Inline/Async/Live lifecycle comparison. The closest match for the underlying aggregate evolution mechanic.
- `marten-aggregate-handler-workflow` — the full Marten + Wolverine aggregate handler workflow (FetchForWriting, `[WriteAggregate]`, optimistic concurrency). More relevant for Skill 10 (`marten-wolverine-aggregates`) but cross-referenced here as the handler-side of the decider-pattern split.

**First design-and-conventions skill in Phase 5.** Cab's `marten-aggregates` explicitly positions itself as design+conventions, with mechanics deferred to ai-skills (top framing: "The generic Marten event-sourcing mechanics... are documented authoritatively in JasperFx ai-skills. **This skill documents the aggregate shape and the project-specific decisions that govern aggregate design in CritterCab.**"). Duplication is minimal by design.

**Section categorization.** 12 sections audited; 1 substantively trimmed; rest preserved as Cab-specific value-add.

**Trimmed sections:**

- Snapshot Strategies (~5 lines saved): removed Live-vs-Inline mechanic comparison table; kept the Cab "live aggregation default" guidance and BC-specific code; added cross-ref to ai-skills `marten-projections-single-stream` § Projection lifecycles.
- See Also restructure: applied three-block convention. New Upstream block with both ai-skills counterparts. Existing Upstream block became Prerequisites. External block trimmed (forward-looking placeholder + install/license note removed).

**Forward-looking placeholder removed:**

- `ai-skills marten-event-sourcing-fundamentals` — doesn't exist in ai-skills. 4th such placeholder removed in Phase 5 (after Skills 4, 5, 8). Pattern emerging: pre-Phase-5 Cab skills had a habit of placeholdering not-yet-published ai-skills counterparts that never materialized.

**Preserved entirely:**

- Top framing (explicit Cab-positioning-vs-ai-skills statement)
- When to apply this skill
- When to Reach for an Event-Sourced Aggregate (Cab BC decision framework: Trip/RideOffer/DriverApplication vs DriverProfile/RiderPreferences/VehicleRegistry)
- Canonical Aggregate Shape (Trip example with 6 named conventions)
- The Decider Pattern in Critter Stack Idiom (theory + Cab realization table)
- Stream Identity and the Create Method (incl. UUID v5 deterministic IDs and `MartenOps.StartStream` usage)
- Apply Method Conventions (one-per-event-type, parameter order, IEvent<T> for metadata, no-throw, commutativity)
- Aggregate Field Conventions (Id, Status enum, nullable timestamps, value objects, no nav refs)
- Worked Example: RideOffer (second BC-specific example, contrasting shape vs Trip)
- Common Pitfalls (9 bullets)

**Trim impact.** Net ~+186 bytes (~5 lines saved from Snapshot Strategies and External, partially offset by ~5 lines added in new Upstream block). Consistent with methodology refinement #6 — design-and-conventions skills with minimal mechanic duplication produce minimal byte changes; the reconciliation value is See Also restructure and forward-looking placeholder removal, not byte-saving.

**Upstream-contribution candidates.** None flagged. Cab content is genuinely Cab-specific design conventions (UUID v5 namespace approach, "live aggregation default," ImmutableList preference, event-first parameter order, no-throw-in-Apply rule). These are project-specific decisions, not pitfalls or coverage gaps in ai-skills.

**New methodology lesson #8 candidate.** Design-and-conventions skills behave differently from mechanic-heavy skills in reconciliation. The trim is genuinely minimal because the Cab content is by design *not* duplicating ai-skills. Future audits of similar skills (likely several remaining Tier 1 Marten skills, plus most of Tier 3) should expect this shape and not over-aggressively trim Cab-specific framing in search of reduction.

---

### 10. `marten-wolverine-aggregates`

**Counterpart(s).** Three ai-skills counterparts, with `marten-aggregate-handler-workflow` as the primary direct-equivalent and the other two referenced for adjacent surface area:

- `marten-aggregate-handler-workflow` (primary) — FetchForWriting automation, `[WriteAggregate]` vs `[AggregateHandler]`, return types, optimistic concurrency via Version property + VersionSource override, multi-stream patterns, missing-aggregate handling (Required/OnMissing/MissingMessage), HTTP `[Aggregate]` integration, ProblemDetails validation, testing patterns (StubEventStream).
- `wolverine-handlers-declarative-persistence` — broader `[Entity]`/`[WriteAggregate]`/`[ReadAggregate]` declarative-persistence surface.
- `wolverine-handlers-fundamentals` — generic handler shape, return-types overview, IoC patterns.

**Hybrid skill.** Unlike Skill 9 (pure design+conventions), this skill mixes design-and-conventions content (decider-pattern positioning, Cab's `nameof()` pin, UUIDNext UUID v5 convention, `MartenOps.StartStream` Action-overload pattern) with mechanic content that overlaps ai-skills (Identity resolution chain, Optimistic/Exclusive concurrency table, return types, Manual Session Calls anti-pattern). Trim is moderate — between Skill 7's heavy mechanic-trim shape and Skill 9's pure-conventions shape.

**Section categorization.** 15 sections audited; 4 substantively trimmed; rest preserved as Cab-specific value-add.

**Trimmed sections:**

- **Identity resolution** (~5 lines saved): removed the 4-step Wolverine resolution chain numbered list; kept Cab's `nameof()` pin rationale; cross-ref to ai-skills § Aggregate identity conventions for the full chain.
- **Concurrency style** (~3 lines saved): removed Optimistic/Exclusive comparison table; kept the Cab pin ("Optimistic") with its rationale; cross-ref to ai-skills § Optimistic concurrency for the Version-property auto-detection and VersionSource override surface.
- **Return-Type Cheat Sheet** (~0 net): added a single sentence cross-ref ("The table below extends ai-skills `marten-aggregate-handler-workflow` § Return types for events with the tuple combinations Cab handlers commonly use."). Cab's 10-row table is genuinely more comprehensive than ai-skills' 5-row table (Cab covers `IStartStream`, `IMartenOp`, `IEnumerable<IMartenOp>`, all the tuple shapes, `IResult` for HTTP, async via Task), so kept the Cab table and pointed to ai-skills for fundamentals rather than the reverse.
- **Manual Session Calls anti-pattern** (~3 lines saved): collapsed leading prose, expression-bodied the correct-example handler, removed the standalone explanatory paragraph at the end (folded into intro); cross-ref to ai-skills § Common anti-patterns for the parallel framing.

**See Also restructure:** applied three-block convention. New Upstream block with all 3 ai-skills counterparts (most detailed Upstream block of Phase 5 so far). Existing Upstream block became Prerequisites. Existing Sibling/Downstream/External blocks already used `**bold**` style; minor adjustments. Removed install/license note from External (now redundant with Upstream block lead-in).

**Preserved entirely:**

- Top framing (decider-pattern handler-side positioning + atomicity guarantee statement)
- When to apply this skill
- The Two Canonical Shapes (StartTrip/CompleteTrip BC examples — Cab BCs are the value-add)
- `[WriteAggregate]` Parameter Resolution → `AlwaysEnforceConsistency` (not in ai-skills)
- Multi-Stream Handlers (TransferRiderToDriver BC example with `[ReadAggregate]` — ai-skills' multi-stream example doesn't show ReadAggregate mixed in)
- Stream Identity: Auto-Assigned vs Deterministic (UUIDNext convention, MD5-based-deterministic-IDs warning, `MartenOps.StartStream` Action-overload pattern)
- Anti-Pattern: Generating the Stream ID for an Auto-Assigned Aggregate (incl. Action-overload pattern for capturing the assigned ID)
- Anti-Pattern: `[WriteAggregate]` on a Handler That Doesn't Append (Cab-specific framing; ai-skills covers OutgoingMessages-when-meant-Events but not this)
- Worked Example: AcceptOffer (Cab BC example)
- Common Pitfalls (9 bullets — routing-rule pre-flight, MD5 ID warning, AlwaysEnforceConsistency reflexivity, etc.)

**Trim impact.** ~9 lines of body content removed; net file +247 bytes (Upstream block additions outweigh trims, similar shape to Skill 9). Consistent with methodology refinement #6 — prose-heavy duplication compresses ~0.5×. The reconciliation gains here are the proper Upstream block (3 detailed counterpart entries), the in-section cross-refs (Identity, Concurrency, Return-Type, Manual Session Calls all now point to specific ai-skills sections), and the removal of redundant install/license note.

**Upstream-contribution candidate flagged.** One:

1. **`MartenOps.StartStream` Action-overload for capturing assigned ID.** Cab shows the pattern `MartenOps.StartStream<T>(s => { assignedId = s.Id; }, events)` for handlers that need the auto-assigned stream ID for the integration message body. ai-skills covers `MartenOps.StartStream<T>(events)` and `MartenOps.StartStream<T>(id, events)` but not this Action overload. Genuinely useful surface; ai-skills could add it as a brief note. Footgun-style addition (the surface exists; the docs miss it).

**Future cleanup item recorded** (see Cab cleanup roadmap below).

**No forward-looking placeholders removed.** All 3 ai-skills counterparts referenced in the External block (now Upstream) verified present in `C:\Code\JasperFx\ai-skills\skills`.

**Revisit (post-Skill-16): `[ConsistentAggregate]` and `[ConsistentAggregateHandler]` coverage added.**

Surfaced as a cross-cutting Cab coverage gap (entry #8) during Skill 15 (`polecat-event-sourcing`) audit — Cab's `marten-wolverine-aggregates` covered the `AlwaysEnforceConsistency = true` underlying flag but not the attribute shortcuts that ai-skills `polecat-cross-stream-operations` documents with four real-world scenarios. Per Erik's directive, the revisit happened at the end of the Polecat session (after Skill 16) before Tier 3 began.

**New H2 section added:** `## [ConsistentAggregate] and [ConsistentAggregateHandler] — read-then-decide consistency`. Inserted between `## Multi-Stream Handlers` and `## Stream Identity: Auto-Assigned vs Deterministic` — the natural neighbor since the new section extends multi-stream loading with the read-then-decide-without-events variant.

Structure: intro framing the gap (`AlwaysEnforceConsistency = true` is the underlying flag; the attribute shortcuts are the preferred surface); "When to use which" decision table comparing `[WriteAggregate]` / `[ReadAggregate]` / `[ConsistentAggregate]` / `[ConsistentAggregateHandler]`; handler-level `[ConsistentAggregateHandler]` with full Cab BC code example (**idempotency guard** — `ConfirmDriverArrival` on Dispatch BC's `RideOffer` aggregate); parameter-level `[ConsistentAggregate]` with full Cab BC code example (**protect secondary stream** — `AssignDriverToTrip` writing Trip + reading Driver via `[ConsistentAggregate]`); brief Cab BC mentions of two additional patterns (**authorization precondition** — `ApproveDriverApplication` on Onboarding BC's `DriverApplication` aggregate; **capacity / reservation** — `AcceptZoneAssignment` on Dispatch BC's `DispatchZone` aggregate); cross-store note pointing to `polecat-event-sourcing` § Wolverine integration with the assertion that the attribute shapes transfer unchanged.

All four Cab BC scenarios are Marten-backed (Cab posture: Marten default, Polecat for Payments only). The four scenarios mirror ai-skills `polecat-cross-stream-operations` four real-world patterns with Cab vocabulary.

**Handler shape:** Used `IEnumerable<object>` + yield for no-op branches (Option A from the audit, Erik-approved). The `yield break` shape reads cleaner than nullable tuple returns for read-then-decide patterns and matches ai-skills' canonical shape directly. The parameter-level scenario uses tuple return `(DriverAssigned, OutgoingMessages)` because that handler doesn't have a no-op branch — throws on validation failure.

**Update to existing `### AlwaysEnforceConsistency` H3:** forward-link to the new H2 section as the preferred attribute-driven surface; the underlying flag stays documented for completeness with simplified text ("Default is `false`." — dropped the prior "Use only when..." sentence since the full guidance now lives in the new section).

**Trim impact.** Pure content addition — no body content removed. ~95 lines added (1 H2 section + intro + decision table + 6 H3 sub-headers + 2 full code examples + 2 brief pattern mentions + cross-store note). File 24,631 → 31,398 bytes (+6,767 bytes / +27%). **Largest single per-skill edit in Phase 5 by byte count** (next-largest: Skill 16's +2,440 bytes / +7.8%).

**Closes Cab coverage gap entry #8 and Cab cleanup roadmap entry #2.** The cross-store note in the new Skill 10 section explicitly forwards Polecat-backed handler readers to `polecat-event-sourcing` § Wolverine integration with the assertion that the attribute shapes transfer unchanged — sufficient coverage for the cross-cutting gap, no separate revisit needed for Skill 15.

**Methodology note.** This revisit demonstrates a Phase 5 pattern not previously seen: a content-addition revisit triggered by a cross-cutting coverage gap surfaced in a later-tier skill's audit. The standard Phase 5 cadence (per-skill audit → pause → report → greenlight → apply) was applied to the revisit too, with the modification that "what to add" was the planning question rather than "what to trim". Recorded as methodology refinement #9.

---

### 11. `marten-projections`

**Counterpart(s).** Five ai-skills counterparts (per Erik's scoping — `marten-projections-flat-table` skipped because Cab has no use for flat-table projections):

- `marten-projections-single-stream` (primary) — single-stream projection mechanics: Apply/Create method conventions, explicit `Evolve` method, lifecycle behavior, conditional deletes, rebuilds, testing.
- `marten-projections-multi-stream` — Identity routing, fan-out patterns (`Identities<T>`), custom groupers with DB lookups, ViewProjection, time-segmented projections, composite identity keys.
- `marten-projections-event-enrichment` — `EnrichEventsAsync` to avoid N+1 queries; declarative enrichment API. **No Cab parallel.**
- `marten-projections-composite` — composite projections, staged execution, `Updated<T>`/`ProjectionDeleted<T>`/`References<T>` synthetic events, chained projections. **No Cab parallel.**
- `marten-projections-raise-side-effects` — `RaiseSideEffects` override for publishing messages, appending events, or enqueuing work atomically with a projection update. **No Cab parallel.**

**Largest Upstream block of Phase 5.** Five detailed entries in the Cab Upstream block, more than any prior skill. Reflects ai-skills' projection-coverage architecture (one skill per projection-pattern category) versus Cab's single combined skill.

**3 new Cab coverage gaps revealed** (the largest single-skill gap discovery of Phase 5):

1. **Event enrichment (`EnrichEventsAsync`)** — N+1 mitigation in projections.
2. **Composite projections** — staged execution / chained projections / synthetic events.
3. **`RaiseSideEffects`** — publishing messages or appending events from a projection atomically.

These are recorded in Cab coverage gaps tracker. Worth surfacing prominently in Phase 5 close-out and considering for post-Phase-5 authoring (or, alternatively, accepting the gaps as "defer to ai-skills" since the ai-skills coverage is comprehensive).

**Section categorization.** 15 sections audited; 7 substantively trimmed; rest preserved as Cab-specific value-add.

**Trimmed sections:**

- **Three Lifecycles** (~5 lines saved): collapsed mechanic-comparison table ("When events are applied" / "Tradeoff" columns) into Cab-default-and-use-case framing; absorbed standalone CritterCab default paragraph; cross-ref to ai-skills `marten-projections-single-stream` § Projection lifecycles.
- **Single-Stream Shape 2** (~12 lines saved): compressed code formatting, removed redundant generic-form code example (`opts.Projections.Add<TripProjection>(...)`), kept the parameterless-constructor footgun rationale renamed as **Footgun** for visibility.
- **Single-method `Evolve` alternative** (~14 lines saved): removed entire code example (covered in ai-skills `marten-projections-single-stream`); kept the Cab decision criteria for when `Evolve` fits over multiple `Apply` methods (and vice versa).
- **Multi-Stream Projections** (~10 lines saved): collapsed the 2-bullet "Two things to internalize" and 3-bullet "Multi-stream gotchas" sections into a single Cab-pin paragraph ("multi-stream projections almost always run async") plus a single highlighted gotcha (foreign-key-missing case); cross-ref to ai-skills `marten-projections-multi-stream` for the full surface.
- **Event Projections** (~3 lines saved): folded the trailing explanation paragraph (about the missing `snapshot` parameter) into the intro paragraph as a Note callout.
- **Soft-Delete Pattern** (~6 lines saved): collapsed the two H3 sections (Marker / Returning null) into bold-paragraph form; cross-ref to ai-skills § Conditional deletes.
- **Lookup-Document Pattern** (~1 line saved): added ai-skills `marten-projections-multi-stream` cross-ref alongside the Marten docs link; minor wording.
- **See Also restructure** (+5 net): applied three-block convention. New 5-entry Upstream block with all referenced ai-skills counterparts (3 marked **No Cab parallel**). Existing Upstream block became Prerequisites. External block trimmed (forward-looking placeholder + install/license note + redundant `marten-aggregate-handler-workflow` entry removed).

**Forward-looking placeholder removed:**

- `ai-skills marten-event-sourcing-fundamentals` — 5th forward-looking placeholder removed in Phase 5 (Skills 4, 5, 8, 9, 11). Same nonexistent skill referenced 4 times across Cab; pattern is now well-established.

**Preserved entirely:**

- Top framing (Marten 8.0 / Chassaing snapshot+evolve alignment + January 2026 release context)
- When to apply this skill
- Three Projection Recipes table (Cab use cases for single-stream / multi-stream / event projections)
- Single-Stream Shape 1 — Self-aggregating snapshot (preferred for write-side aggregates)
- Multi-stream BC example (DriverLifetimeStats with Identity<T> routing for TripStarted/TripCompleted)
- Event Projection BC example (TripReceipt)
- IoC-Injected Projections (TripPricingProjection + IPriceLookup; AddProjectionWithServices<T> + identity-acl tie-in for Microsoft Graph)
- Lifecycle Decision Guide (5-step Cab-specific decision flow)
- 8-bullet Anti-Patterns (Apply+Evolve mixing, inline multi-stream, speculative inline, live for hot reads, AddProjectionWithServices with self-aggregating, multi-stream-needing-aggregate-load, registration silent-failure parallel to routing-rule footgun, snapshot mutation rules)

**Trim impact.** ~46 lines of body content removed; +5 lines added in new 5-entry Upstream block. Net ~41 lines saved; file 25,593 → 24,446 bytes (-1,147 bytes, -4.5%). Solid mid-range trim — sits between Skill 6/7 (heavy mechanic deduplication, ~125–160 lines) and Skill 9 (minimal design+conventions, ~3 lines).

**Upstream-contribution candidate flagged.** One:

1. **Projection registration silent-failure footgun.** Cab's anti-pattern bullet calls out: "A `SingleStreamProjection<TDoc, TId>` subclass that's never added to `opts.Projections` does nothing. The compiler doesn't catch this — silent failure, like the most consequential CritterCab footgun in `wolverine-messaging-handlers`." ai-skills' `marten-projections-single-stream` covers projection registration but doesn't (per the section heads I audited) explicitly call out the silent-failure mode. Parallel to the `OutgoingMessages`-without-routing-rule footgun (Skill 3, roadmap entry #4) and the projection-registration-without-`opts.Projections.Add` mode is genuinely Cab-discovered framing. _TBD at close-out_ priority.

---

### 12. `marten-querying`

**Counterpart(s).** Two ai-skills counterparts, both small (~13 KB combined) relative to Cab's 33 KB skill:

- `marten-advanced-indexes-and-query-optimization` (5.9 KB) — index design (computed, GIN, duplicated fields, unique, partial, multi-column, full-text), `[DuplicateField]` attribute, `IndexMethod` options, `TenancyScope`, computed-vs-duplicated decision matrix. Brief query-optimization tips at the end.
- `marten-advanced-optimization` (6.8 KB) — bootstrap-level performance tuning: `EventAppendMode.Quick` vs `Rich`, snapshot lifecycle strategies (overlaps with Cab `marten-projections`), identity-map for aggregates, mandatory stream types, async daemon error handling (will overlap with `marten-async-daemon`), projection throughput options (`IncludeType<T>`, `BatchSize`, `CacheLimitPerTenant`), daemon progress monitoring, lightweight sessions, Marten.AspNetCore JSON streaming (older extension-method API only).

**Most of Cab's content has no ai-skills counterpart at all.** Cab's 33 KB skill is comprehensive coverage of querying patterns that ai-skills doesn't address in any single skill. Auditing the 12 body sections, only fragments overlap (lightweight session default, brief Select() projections, brief compiled query mention) — the rest is genuine Cab value-add.

**Significant Cab coverage gap revealed (5th in Phase 5, arguably the most consequential).**

Cab has **zero coverage of index design** — the entire ai-skills `marten-advanced-indexes-and-query-optimization` skill is uncovered: `Index()`, `[DuplicateField]`, `GinIndexJsonData()`, `UniqueIndex()`, `IndexMethod` options, `TenancyScope.PerTenant`, partial indexes, full-text search (`FullTextIndex`), computed-vs-duplicated decision matrix. This is consequential because indexes directly affect query performance (the core topic of this skill); a Cab service using `marten-querying` patterns without an index strategy will hit unindexed-JSONB-scan footguns. Skill 24 (`service-bootstrap`) audit will need to verify whether Cab covers index registration there — if not, this is a clear post-Phase-5 authoring candidate.

Erik's pre-audit framing accurately predicted this: "querying is also making things performant through indexes, query optimizations such as compiled queries, etc. So we may have some things to potentially *add* or at least reference upstream." The reference upstream is via the new Upstream block; the *add* candidates are surfaced as roadmap entries #12 and #13.

**Section categorization.** 13 sections audited; 0 substantively trimmed in the body; See Also restructured.

**Trimmed:**

- **See Also restructure** (~5 net): applied three-block convention. New 2-entry Upstream block (`marten-advanced-indexes-and-query-optimization` marked **No Cab parallel**, `marten-advanced-optimization` noted as overlapping-elsewhere). Existing Upstream block became Prerequisites. External block trimmed (forward-looking placeholder + install/license note removed). `ai-skills marten-aggregate-handler-workflow` retained in External as supplementary.

**Forward-looking placeholder removed:**

- `ai-skills marten-event-sourcing-fundamentals` — 6th forward-looking placeholder removed in Phase 5 (Skills 4, 5, 8, 9, 11, 12). Same nonexistent skill referenced 4 separate times across Cab (Skills 9, 11, 12 — all Marten skills); pattern is now thoroughly established.

**Preserved entirely (all 12 body sections):**

- Top framing (Cab-positioning-vs-ai-skills statement — "this skill documents the query-side patterns, gotchas, and Cab-specific opinions on top of those mechanics")
- When to apply this skill
- Sessions and Their Purpose (4 session kinds: Lightweight/Identity/DirtyTracked/Query; identity-map consequences for queries; Lightweight pin)
- Decision Matrix — Which Query Shape (Cab-specific table mapping situation to query shape)
- Standard LINQ (Include for ad-hoc JOINs, Stats for paging metadata)
- Compiled Queries (the **substantial Cab value-add**: 4 silent-failure footguns, IQueryPlanning paging pattern with `[MartenIgnore]`, compiled-query interface reference, Include inside compiled queries)
- Batched Queries
- Query Plans (`QueryListPlan<T>` specification pattern)
- Raw JSON (`session.Json` API + `AsJson()` LINQ extension + `ToJsonArray()`/`ToJsonFirstOrDefault()` variants)
- Raw SQL (`session.QueryAsync<T>` for simple WHERE + `session.AdvancedSql.QueryAsync<T,T2,T3>` for full SQL with ROW() syntax + schema resolution rules)
- Metadata in Queries (always-present columns, opt-in marker interfaces, soft-delete query behavior with `Marten.Linq.SoftDeletes`, last-modified queries)
- Streaming JSON to HTTP (modern `StreamOne<T>`/`StreamMany<T>`/`StreamAggregate<T>` return types with 404-semantics-differ note + compiled-query overloads + extension-method legacy API)
- Common pitfalls (11 bullets, all Cab-specific)

**Trim impact.** Net ~5 lines saved, +6 lines added in new Upstream block, file 33,073 → 33,942 bytes (+869 bytes / +2.6%). Confirms methodology refinement #8: design-and-conventions skills (where Cab content is largely Cab value-add by design) produce minimal trims and may show net-positive byte counts because the new Upstream block (with detailed counterpart entries) outweighs the modest mechanic trims. The reconciliation value here is See Also restructure + forward-looking placeholder removal + 2 substantial upstream-contribution candidates flagged + 1 significant Cab coverage gap surfaced.

**Upstream-contribution candidates flagged.** Two:

1. **Compiled query footgun list.** Cab's section enumerates 4 silent failures (async LINQ operators in `QueryIs()` body, primary constructors that the planner can't inspect, `ToList()`/`ToArray()` in body throwing from expression-rewriting, boolean fields not matching to command parameters) plus the `IQueryPlanning` + `[MartenIgnore]` paging pattern with the unique-value rule. ai-skills' `marten-advanced-indexes-and-query-optimization` mentions compiled queries in one paragraph as an optimization tip — none of the gotchas. The footgun list is substantial Cab-discovered framing worth upstreaming. _TBD at close-out_.
2. **Modern Wolverine HTTP-first streaming API (`StreamOne<T>` / `StreamMany<T>` / `StreamAggregate<T>` return types).** ai-skills' `marten-advanced-optimization` covers Marten.AspNetCore JSON streaming using only the **older extension-method API** (`WriteArray`, `WriteById`, `WriteSingle`). The modern return-type API — which integrates with Wolverine's `IResult` pipeline, generates OpenAPI metadata automatically without `[ProducesResponseType]`, and is the recommended approach for Wolverine-HTTP services — is missing. ai-skills even calls out `[ProducesResponseType]` as a manual workaround, which the new API obviates. Cab covers it in detail with the 404-semantics-differ note for `StreamOne` (404 on no result) / `StreamMany` (empty array, never 404) / `StreamAggregate` (404 on no events) plus the `IDocumentSession` requirement footgun for `StreamAggregate`. _TBD at close-out_.

---

### 13. `marten-async-daemon`

**Counterpart(s).** Four ai-skills counterparts with the deep-dive as primary:

- `marten-advanced-async-daemon-deep-dive` (primary, 9.2 KB) — Solo/HotCold/Wolverine-managed mode mechanics, error-handling configuration, programmatic daemon control (`StopAgentAsync`, `IProjectionCoordinator`, `AllProjectionProgress`), production tuning (`HealthCheckPollingTime`, `CheckAssignmentPeriod`, `InboxStaleTime`, `OutboxStaleTime`), durability metrics, cold-start optimization, test fixtures (`MartenDaemonModeIsSolo`, `RunWolverineInSoloMode`), SQL diagnostics for node assignments, `NodeAssignmentHealthCheckTracing` settings.
- `marten-advanced-optimization` — bootstrap-level performance tuning that overlaps the daemon: `EventAppendMode.Quick` vs `Rich`, projection throughput options, identity-map for aggregates, mandatory stream types.
- `marten-projections-event-enrichment` — already referenced from Skill 11; the daemon skill cross-refs it for `EnrichEventsAsync` mechanic surface.
- `marten-projections-composite` — already referenced from Skill 11; the daemon skill cross-refs it for composite projection mechanic surface.

**Hybrid skill.** Cab's 27 KB skill covers a partially-overlapping but substantially different surface than ai-skills' 9.2 KB deep-dive. Auditing 11 sections: 2 substantive overlaps (Daemon Modes, Error Handling Defaults), 1 mixed (Throughput knobs — basic knobs overlap, April 2026 additions are Cab-exclusive), and 8 sections that are largely Cab value-add (runtime model pedagogy, Tombstone events, position strategies, single-stream rebuild, custom teardown, async-only performance hooks with daemon-pipeline implications, health checks, per-projection observability metrics, Cab-specific pitfalls).

**6th Cab coverage gap revealed: operational/troubleshooting daemon surface.**

ai-skills covers operational details Cab doesn't currently address anywhere: programmatic daemon control (`StopAgentAsync`, `IProjectionCoordinator.DaemonForMainDatabase()`), production tuning settings (`HealthCheckPollingTime`, `CheckAssignmentPeriod`, `InboxStaleTime`, `OutboxStaleTime`), durability metrics (`wolverine-inbox-count`, `wolverine-outbox-count`, `wolverine-scheduled-count`, `DurabilityMetricsEnabled`), cold-start optimization (`codegen write` for AOT), SQL diagnostics (`wolverine_node_assignments`/`wolverine_node_records` queries for node leadership inspection), and `NodeAssignmentHealthCheckTracing` settings (sampling controls). Most are essentially operational daemon troubleshooting topics. Cab can either (a) author a new operational-daemon Cab skill post-Phase-5, or (b) accept this as "defer to ai-skills directly." Documented honestly in the Cab Upstream block with **"Cab does not currently cover"** language.

**Section categorization.** 11 sections audited; 1 substantively trimmed (Error Handling Defaults); 2 augmented with cross-ref intros (Throughput, Async-Only Performance Hooks); See Also restructured.

**Trimmed sections:**

- **Error Handling Defaults** (~14 lines saved): removed the 14-line skip-flag code example (continuous + rebuild flags); kept the Cab framing paragraph about asymmetry rationale and the dead-letter-queue note (`DeadLetterEvent`); cross-ref to ai-skills `marten-advanced-async-daemon-deep-dive` § Error handling configuration. Tombstone events / `StaleSequenceThreshold` subsection preserved unchanged.

**Cross-ref intros added:**

- **Throughput and Performance Knobs** (+2 lines): brief note at top that mechanic basics (`BatchSize`, `CacheLimitPerTenant`, `UseIdentityMapForAggregates`) are also covered in ai-skills, with the framing that the April 2026 additions below (`EnableEventTypeIndex` + adaptive event loader) are Cab-specific coverage of post-Marten-8.29 features.
- **Async-Only Performance Hooks** (+2 lines): brief note that the full mechanic surface for `EnrichEventsAsync` is documented in ai-skills `marten-projections-event-enrichment` and composite projections in `marten-projections-composite`; Cab's coverage focuses on daemon-pipeline implications.

**See Also restructure** (+5 net): applied three-block convention. New 4-entry Upstream block (`marten-advanced-async-daemon-deep-dive` as primary with explicit "Cab does not currently cover" language for operational surface; `marten-advanced-optimization`, `marten-projections-event-enrichment`, `marten-projections-composite` as supplementary). Existing Upstream block became Prerequisites. External block trimmed (forward-looking placeholder + install/license note removed).

**Forward-looking placeholder removed:**

- `ai-skills marten-event-sourcing-fundamentals` — 7th forward-looking placeholder removed in Phase 5 (Skills 4, 5, 8, 9, 11, 12, 13). Same nonexistent skill referenced 4 separate times across Cab (Skills 9, 11, 12, 13 — all Marten skills); pattern is now thoroughly established and the count alone is now the punchline.

**Preserved entirely:**

- Top framing ("single highest-leverage Marten configuration knob")
- When to apply this skill
- **What the Daemon Is Doing** (runtime model pedagogy: shards, high-water mark, 5-step processing loop, fast/slow polling cadence)
- Daemon Modes (3-row table + Wolverine-managed code example + Cab default subsection + Critical rule for not combining HotCold + Wolverine-managed)
- Tombstone events and sequence gaps (Cab framing of `StaleSequenceThreshold` adjustment for long-running write transactions)
- Throughput knobs (BatchSize, CacheLimitPerTenant, UseIdentityMapForAggregates, EnableAdvancedAsyncTracking, EnableEventSkippingInProjectionsOrSubscriptions, **EnableEventTypeIndex** with diagnostic signal framing, **Adaptive event loader** with 3-strategy fallback chain explained)
- Rebuilds (full rebuild via daemon, single-stream rebuild, teardown options including custom rules, **4-strategy position strategies** for new projections)
- Async-Only Performance Hooks (`EnrichEventsAsync` with the async-only-pipeline limitation note; composite projections with daemon-side rebuild-as-unit pin)
- Health Checks (`AddMartenAsyncDaemonHealthCheck` with `gracePeriod` for K8s rolling restart scenarios)
- Observability (per-projection metrics: processed/gap/skipped with gap-histogram-as-alerting-signal framing; configurable `OtelPrefix` and `ActivitySource`)
- Common pitfalls (9 bullets, all Cab-specific or Cab-focused framing)

**Trim impact.** Net body lines: ~14 saved (Error Handling code example) - 4 added in cross-ref intros = ~10 lines saved in body. See Also: ~+5 lines net (4-entry Upstream block additions outweigh External trims). Total: ~5 lines net saved; file 26,881 → 28,316 bytes (+1,435 bytes / +5.3%). Net-positive byte count for the same reason as Skills 9, 10, 12: comprehensive Cab content + new substantial Upstream block outweighs the modest mechanic trim.

**Upstream-contribution candidates flagged.** Three:

1. **April 2026 daemon improvements (`EnableEventTypeIndex` + Adaptive event loader, Marten 8.29+).** ai-skills' `marten-advanced-async-daemon-deep-dive` doesn't mention either feature — the most consequential daemon performance additions of 2026. Cab covers `EnableEventTypeIndex` with the diagnostic signal framing ("reach for this when the daemon log emits the timeout-and-fallback warning") and the adaptive event loader's 3-strategy fallback chain (Normal → Skip-ahead → Window-step). Both belong in ai-skills' deep-dive at the next revision. _TBD at close-out_.
2. **Position strategies for new projections** (`SubscribeFromPresent` / `SubscribeFromTime` / `SubscribeFromSequence` / `SubscribeAsInlineToAsync`). ai-skills' deep-dive doesn't cover any of the 4 strategies. This is a critical operational topic for introducing async projections to systems with existing events — without it, every new projection forces a full historical rebuild. Cab covers all 4 with use-case framing for each. _TBD at close-out_.
3. **Health check `gracePeriod` parameter for K8s rolling restarts.** ai-skills doesn't cover health checks (`AddMartenAsyncDaemonHealthCheck`) at all. Cab's framing of the `gracePeriod` parameter (exempts the daemon from lag check during configured startup window) is operationally important for containerized deployments — without it, K8s kills pods before they're stable. Lower priority than #1 and #2 since the entire health-check topic is missing, not just one sub-feature. _TBD at close-out_.

---

### 14. `dynamic-consistency-boundary`

**Counterpart.** One ai-skills counterpart — `marten-advanced-dynamic-consistency-boundary` (7.6 KB) — covering basic three-part pattern (state + Load + Handle), `EventTagQuery` fluent API, brief `IEventBoundary<T>` mention, DCB-vs-standard-multi-stream comparison, return-value handling, one anti-pattern, decision guidance. Cab's 32 KB skill covers the same intro surface and adds the entire footgun layer + operational guidance.

**Cab substantially exceeds ai-skills' coverage.** ai-skills' DCB skill is shallow relative to Cab's. Cab adds:

- The manual `FetchForWritingByTags` pattern with full `IEventBoundary<T>` surface and conditional/idempotent append example.
- The `Validate` pre-handler hook returning `HandlerContinuation`.
- The `ValidateAsync` + `[BoundaryModel]` silent-handler-discovery-failure footgun.
- The boundary state `public Guid Id { get; set; }` requirement under Marten 8.
- The tag-type single-property record requirement under .NET 10 (raw `Guid` fails because `Guid.Variant`/`Guid.Version` properties were added in .NET 10).
- The `StartStream`-drops-tags behavior with the mandatory-stream-type-declaration workaround pattern.
- The `DcbConcurrencyException`-vs-`ConcurrencyException` sibling rule (both need explicit retry registration).
- Unit + integration testing patterns.
- An 8-step implementation checklist.
- 9-bullet Common pitfalls (all Cab-discovered or Cab-focused).

This is a "direct equivalent (Cab substantially exceeds)" reconciliation — the counterpart exists but Cab is far ahead. Similar shape to Skill 12 (`marten-querying`).

**Most footgun-rich skill in Phase 5.** Seven upstream-contribution candidates flagged from a single skill — the highest count from any reconciliation in Phase 5. The candidates cluster around three failure modes:

1. **Boot-time and handler-discovery failures** (#17 `[BoundaryModel]` on `Validate` → CS0128, #18 `ValidateAsync` + `[BoundaryModel]` silent failure, #20 raw `Guid` `InvalidValueTypeException`).
2. **Late-surfacing failures at runtime or fixture teardown** (#19 `AndEventsOfType` runtime `ArgumentException`, #21 boundary state `Id` `InvalidDocumentException` at fixture teardown).
3. **Silent behavior changes** (#22 `StartStream` drops tags, #23 `DcbConcurrencyException` not caught by `ConcurrencyException` retry policies).

These are exactly the kinds of footguns that cost teams real production hours to diagnose. They belong in ai-skills' DCB coverage at the next revision.

**ai-skills content drift observed.** Notable observation surfaced during the audit: ai-skills' `marten-advanced-dynamic-consistency-boundary` claims **"DCB is currently implemented in Polecat. The `[BoundaryModel]` attribute and `EventTagQuery` are Polecat-specific."** This contradicts both the file naming (Marten-prefixed) and the demonstrable presence of Marten DCB (`Marten.Events.Dcb.DcbConcurrencyException`, `opts.Events.RegisterTagType<>().ForAggregate<>()` on Marten store options, the `src/Persistence/MartenTests/Dcb/` test directory in the Wolverine repo). The skill was likely written when DCB was Polecat-only and not updated when Marten gained the implementation. Documented in new "ai-skills content drift observed" section below; flagged for Erik to bring to JasperFx team if appropriate.

**Section categorization.** 14 sections audited; 0 substantively trimmed; See Also restructured.

**Trimmed:**

- **See Also restructure** (~3 net): applied three-block convention. New 1-entry Upstream block with explicit "Cab's coverage substantially exceeds" framing and comprehensive enumeration of what Cab adds beyond the basic intro. Existing Upstream block became Prerequisites. External block trimmed (forward-looking placeholder + install/license note removed). `ai-skills marten-aggregate-handler-workflow` retained in External as supplementary.

**Forward-looking placeholder removed:**

- `ai-skills marten-event-sourcing-fundamentals` — 8th forward-looking placeholder removed in Phase 5 (Skills 4, 5, 8, 9, 11, 12, 13, 14). Same nonexistent skill referenced 5 separate times across Cab (Skills 9, 11, 12, 13, 14 — all Marten skills); pattern is exhaustively established at this point.

**Preserved entirely (all 13 body sections):**

- Top framing (DCB framing + Sara Pellegrini citation + Cab-positioning-vs-ai-skills statement)
- When to apply this skill
- When to Reach for DCB (Cab decision matrix with 4 scenarios + Cab's primary DCB scenario: offer acceptance with 3 invariants spanning Offer/Driver/RideRequest streams)
- Two Patterns, One Preferred (canonical vs manual + when each fits)
- The Canonical `[BoundaryModel]` Pattern (Load + Handle + return-value shapes table + Validate + ValidateAsync incompatibility note + critical-rule about not double-attributing)
- The Manual `FetchForWritingByTags` Pattern (full code example + IEventBoundary<T> surface enumeration + idempotent-append example)
- EventTagQuery — the Shared DSL (fluent vs imperative styles + AndEventsOfType requirement footgun)
- The Boundary State Aggregate (Apply method conventions, nullable parameter handling, `Required = true` alternative)
- Tag Type Registration (single-property record requirement, raw Guid `InvalidValueTypeException` rationale tied to .NET 10's `Guid.Variant`/`Version` properties)
- Tagging Writes (AddTag/WithTag, StartStream drops tags, mandatory stream type declaration workaround pattern)
- Concurrency (DcbConcurrencyException vs ConcurrencyException sibling rule + Polecat namespace parallel)
- Testing (unit tests with plain state objects + integration tests through Wolverine)
- Implementation Checklist (8 numbered steps)
- Common pitfalls (9 bullets, all Cab-discovered or Cab-focused)

**Trim impact.** Net body content unchanged; ~3 lines saved in External block (forward-looking placeholder + install/license note); ~6 lines added in new Upstream block + Prerequisites heading. File 32,374 → 33,183 bytes (+809 bytes / +2.5%). Light pass with net-positive byte count, consistent with methodology refinement #8 for skills where Cab content substantially exceeds ai-skills.

**Upstream-contribution candidates flagged.** Seven (highest count from any single skill in Phase 5):

1. **`[BoundaryModel]` on `Validate` parameter → CS0128 codegen error** (entry #17). Wolverine codegen produces "duplicate local variable" when the attribute appears on both `Validate` and `Handle` methods. The pipeline hook should receive the projected state by plain-parameter injection automatically; only `Handle` needs the attribute. Boot-time failure.
2. **`ValidateAsync` + `[BoundaryModel]` → silent handler discovery failure** (entry #18). Combination doesn't compose; Wolverine's handler discovery silently skips it. The synchronous `Validate` works correctly. Most insidious of the seven because the failure mode is silent.
3. **`AndEventsOfType` required after `For()`/`Or(tag)` in fluent form** (entry #19). Without it, `FetchForWritingByTags` throws `ArgumentException` at runtime. The imperative `.Or<TEvent, TTag>(...)` form doesn't have this trap because event type and tag are specified together.
4. **Tag types must be single-property records — raw `Guid` fails under .NET 10** (entry #20). `Guid` has multiple public instance properties (notably `Variant` and `Version` since .NET 10), so `RegisterTagType<Guid>` throws `InvalidValueTypeException` at boot. Particularly relevant timing — this footgun didn't exist before .NET 10 made `Guid.Version`/`Variant` public; older Marten/Polecat tutorials may show raw-Guid registration that no longer works.
5. **Boundary state `public Guid Id { get; set; }` requirement under Marten 8** (entry #21). Marten 8 registers boundary state types as documents; missing `Id` surfaces as `InvalidDocumentException` **at fixture teardown** rather than at boot or save time — easy to miss until tests fail with confusing error messages. The canonical Wolverine repo `SubscriptionState` examples *omit* `Id`; Cab on Marten 8 needs it.
6. **`StartStream` drops tags** (entry #22). `MartenOps.StartStream` and `session.Events.StartStream` re-wrap raw event objects internally — pre-wrapped `IEvent` instances passed in lose their tags. The right path for tagged events is `BuildEvent` + `WithTag` + `Append(streamId, wrapped)`. Cab-discovered behavior; the workaround pattern (StartStream then PendingChanges.AddTag for the mandatory-stream-type case) is novel.
7. **`DcbConcurrencyException` and `ConcurrencyException` are siblings, not parent-child** (entry #23). A retry policy that catches one does not catch the other. `OnException<MartenException>` catches both but is too broad. Both need explicit registration: `OnException<ConcurrencyException>().RetryWithCooldown(...)` AND `OnException<DcbConcurrencyException>().RetryWithCooldown(...)`. The same applies to Polecat (`Polecat.Events.Dcb.DcbConcurrencyException`).

My read on prioritization (for close-out synthesis): #17, #18, #20, #21, #23 are highest-priority because they fail at boot/handler-discovery/fixture-teardown and are easy to miss; #19 and #22 are lower priority because they fail at the right moment (loud and quickly diagnosable).

---

### 15. `polecat-event-sourcing`

**Counterpart(s).** Three ai-skills counterparts totaling ~30.7 KB:

- `polecat-setup-and-decision-guide` (10.6 KB, primary) — Marten-vs-Polecat decision matrix, SQL Server 2025 + native JSON column type, NuGet packages, basic event-sourcing API, schema management, projections, async daemon basics, **Polecat MCP server (`app.MapPolecatMcp()`)**, anti-patterns.
- `polecat-cross-stream-operations` (16 KB) — multiple `[WriteAggregate]` parameters with `VersionSource`, **`[ConsistentAggregate]` / `[ConsistentAggregateHandler]` attributes** for read-then-decide patterns, four real-world scenarios (idempotency guard, authorization precondition, capacity reservation, secondary-stream protection), **`FakeEventStream<T>` unit-test stub**, cross-stream-vs-DCB table.
- `critterstack-arch-new-project-wolverine-polecat` (4.1 KB) — bootstrap-focused; lighter than the setup-and-decision-guide.

**Hybrid reconciliation: partial overlap, mostly Cab value-add.** Phase 4's framing that "Cab Polecat coverage may be ahead of ai-skills" is partially true:

- **Cab is ahead on:** DCB (full Polecat-namespace coverage parallel to `dynamic-consistency-boundary`), Polecat saga concurrency story (resolves the `wolverine-sagas` deferred verification), v3.0 schema breaking change context, v3.0 `Snapshot<T>()` framing as registration shortcut, v3.1 `IDocumentStoreUsageSource` publishing, polling daemon vs LISTEN/NOTIFY architectural framing, subscriptions + `EventForwarding` via `SubscribeToEvent<T>`, `UseFastEventForwarding` + `UseWolverineManagedEventSubscriptionDistribution` decision framework, cross-store `IntegrateWithWolverine` confusion warning, Cab Payments BC narrative.
- **ai-skills is ahead on:** Polecat MCP server, `[ConsistentAggregate]` / `[ConsistentAggregateHandler]` attribute shortcuts (with four real-world scenarios), `FakeEventStream<T>` unit-test stub.

**Section categorization.** 18 sections audited; 2 substantively trimmed; See Also restructured.

**Trimmed sections:**

- **Appending events** (~10 lines saved): collapsed 14-line code-block enumeration (`StartStream` variants + `Append` + `AppendOptimistic` + `AppendExclusive` + `ArchiveStream` + `TombstoneStream`) to 3-line Cab convention; cross-ref to ai-skills `polecat-setup-and-decision-guide` § Event sourcing with Polecat for the full overload surface. Kept the `Append`-vs-`AppendOptimistic` distinction prose and Cab Payments BC convention paragraph.
- **Schema management with Weasel.SqlServer** (~5 lines saved): folded the `AutoCreate.CreateOrUpdate` / `AutoCreate.None` bullet enumeration into intro paragraph cross-ref; kept v3.0 schema change context and CLI workflow convention (`db-apply` vs `db-assert` for production deployments).

**See Also restructure** (+8 net): applied three-block convention. New 3-entry Upstream block with `polecat-setup-and-decision-guide` as primary + the other two ai-skills entries. Each entry explicitly notes what's in ai-skills that Cab doesn't currently cover (MCP server, `[ConsistentAggregate]` attributes, `FakeEventStream<T>` stub). Existing Upstream block became Prerequisites. External block trimmed (broken forward-looking placeholder removed).

**Forward-looking placeholder removed (the most broken yet):**

- `ai-skills polecat-event-sourcing and polecat-document-store` (in those exact names) — 9th forward-looking placeholder removed in Phase 5, and the most broken yet. Neither name exists in ai-skills; the actual three Polecat skills are differently named (`polecat-setup-and-decision-guide`, `polecat-cross-stream-operations`, `critterstack-arch-new-project-wolverine-polecat`). The placeholder appears to have been authored by speculation about ai-skills naming conventions that didn't pan out. **First Phase 5 placeholder where the placeholdered skill names are completely wrong (rather than just nonexistent).**

**Preserved entirely:**

- Top framing (Polecat as Marten with the engine swapped + v3.0 headline + v3.1 monitoring + the three differences from Marten)
- When to apply this skill / Do NOT use this skill for
- Polecat in CritterCab (Cab posture: Marten default, Polecat for Payments BC only, no mixing within a single BC)
- Setup and bootstrap (`AddPolecat` + `IntegrateWithWolverine` + v3.1 monitoring discovery via `IDocumentStoreUsageSource`)
- Core schema (`pc_events`, `pc_streams` without snapshot columns post-v3.0, `pc_event_progression`, `pc_doc_*`, `pc_event_tag_*`)
- FetchForWriting and WriteToAggregate
- Projection lifecycles (Inline / Live / Async + Cab Payments convention)
- Single-stream projections and the v3.0 `Snapshot<T>()` shortcut (with three v3.0-specific notes)
- Multi-stream and other projection types (defers to `marten-projections`)
- Live aggregation
- **Dynamic Consistency Boundary** (Polecat namespace coverage: tag types via `RegisterTagType<TTag>`, tagging events via `BuildEvent` + `WithTag`, querying by tags via `QueryByTagsAsync` / `AggregateByTagsAsync` / `EventsExistAsync`, `FetchForWritingByTags` + `IEventBoundary<T>`, `Polecat.Events.Dcb.DcbConcurrencyException`, tag routing)
- Async daemon (polling vs LISTEN/NOTIFY + SQL Server `LEAD()` window function for high-water-mark gap detection + `DaemonSettings.StaleSequenceThreshold` + `WaitForNonStaleProjectionDataAsync` patterns)
- Subscriptions (`SubscriptionBase` + `EventForwarding` via `SubscribeToEvent<T>().TransformedTo(...)` + `UseFastEventForwarding` + `UseWolverineManagedEventSubscriptionDistribution`)
- Schema management CLI workflow (`db-apply` / `db-assert`)
- **Wolverine integration** (`PolecatOps` factory: `StartStream` / `Store` / `Insert` / `Update` / `Delete` / `StoreMany` / `StoreObjects` / `DeleteWhere` / `Nothing` plus tenant-scoped overloads; aggregate handlers via `[WriteAggregate]`; saga storage via `PolecatPersistenceFrameProvider`; **the Polecat saga concurrency story** with verified `Saga.Version` vs `IRevisioned` distinction resolving the `wolverine-sagas` deferred verification; `EventForwarding` for cross-BC publication)
- Cab use case: Payments BC (full event flow `PaymentRequested` → `PaymentAuthorized` / `PaymentRejected` → `PaymentCaptured` → `PaymentSettled` → `RefundIssued`; projections including `Snapshot<Payment>` Inline + `MerchantSettlement` Async MultiStream + `PaymentAudit` Async EventProjection to flat table; DCB usage with `PaymentRequestId` + `MerchantId` tag types; subscriptions for receipt emails + settlement batch processing)
- Common pitfalls (12 bullets — incl. cross-store `IntegrateWithWolverine` confusion, mixing `PolecatOps` and `session.Events`, `IRevisioned`-on-saga, `MartenOps`-against-Polecat-service, `AutoCreate`-in-production, `UseNativeJsonType` mismatch with pre-2025 SQL Server)

**Trim impact.** ~12 lines body content saved, +8 lines See Also additions. File 50,573 → 50,556 bytes (−17 bytes / essentially unchanged). Body trims and See Also additions essentially balance — methodology refinement #8 territory. Comprehensive Cab content + new substantial Upstream block produces near-zero net byte change.

**Three Cab coverage gaps revealed (entries #7, #8, #9):**

1. **Polecat MCP server (`app.MapPolecatMcp()`)** — built-in MCP server for AI agent integration with tools `get_event_store_configuration`, `list_known_event_types`, `list_projections`. Operationally relevant for any team using Claude Code or similar agents against a Polecat-backed service. ai-skills `polecat-setup-and-decision-guide` covers it; Cab has zero coverage.
2. **`[ConsistentAggregate]` / `[ConsistentAggregateHandler]` attribute shortcuts** — Cab's `marten-wolverine-aggregates` covers `AlwaysEnforceConsistency = true` (the underlying mechanism) but neither that skill nor `polecat-event-sourcing` covers the attribute shortcuts. ai-skills `polecat-cross-stream-operations` covers them with four substantial real-world scenarios (idempotency guard, authorization precondition, capacity reservation, secondary-stream protection). **Cross-cutting gap** — affects both Cab's Marten-side and Polecat-side coverage. Skill 10 (`marten-wolverine-aggregates`) flagged for revisit at end of Polecat session per Erik's directive.
3. **`FakeEventStream<T>` unit-test stub** — ai-skills `polecat-cross-stream-operations` provides a copy-paste-ready stub for unit-testing handlers taking `IEventStream<T>` parameters without infrastructure. Cab's testing coverage doesn't include this pattern.

**Eight upstream-contribution candidates flagged (entries #24–31):**

1. v3.0 `Snapshot<T>()` framing as registration shortcut (entry #24).
2. v3.0 schema breaking change documentation (entry #25).
3. v3.1 `IDocumentStoreUsageSource` publishing (entry #26).
4. Polecat saga concurrency story (entry #27).
5. Polling daemon vs LISTEN/NOTIFY architectural framing (entry #28).
6. EventForwarding cross-BC publication via `SubscribeToEvent<T>` (entry #29).
7. `UseFastEventForwarding` + `UseWolverineManagedEventSubscriptionDistribution` decision framework (entry #30).
8. Cross-store `IntegrateWithWolverine` confusion warning for mixed Marten + Polecat solutions (entry #31).

This is the second footgun-rich skill of Phase 5 (after Skill 14's seven candidates); it brings the Phase 5 candidate total to 31. The candidates here are mostly architectural/operational rather than failure-mode footguns — distinct from Skill 14's pure footgun cluster.

**ai-skills content drift observation #2.** ai-skills `polecat-cross-stream-operations` reinforces the Skill 14 DCB-Polecat-only drift: its DCB-vs-cross-stream table claims **"Availability: Polecat only"** and references "the **Dynamic Consistency Boundary (Polecat)** skill" — but Marten DCB clearly exists (per Skill 14 audit). This is the second occurrence of the same drift across ai-skills' Polecat surface. Folded into the existing tracker entry #1 rather than added as a new entry.

**Cleanup roadmap addition.** Skill 10 (`marten-wolverine-aggregates`) revisit for `[ConsistentAggregate]` / `[ConsistentAggregateHandler]` coverage scheduled for end of Polecat session (after Skill 16) per Erik's directive. Recorded as Cab cleanup roadmap entry #2.

---

### 16. `polecat-document-store`

**Counterpart(s).** Three ai-skills counterparts (same set as Skill 15) totaling ~30.7 KB, but with **only bootstrap-side overlap on the document-store surface**:

- `polecat-setup-and-decision-guide` (10.6 KB, primary) — touches the document store at the bootstrap level only: registered services (`IDocumentStore`/`IDocumentSession`/`IQuerySession`), basic `Store` example in the event-sourcing section, document type registration via `IConfigurePolecat`, schema management for `pc_doc_*` tables, Polecat MCP server. **No substantive coverage of session shapes, identity strategies, LINQ surface, soft deletes, patching, or document-level concurrency.**
- `polecat-cross-stream-operations` (16 KB) — event-sourcing-side cross-stream patterns; not document-store-relevant directly.
- `critterstack-arch-new-project-wolverine-polecat` (4.1 KB) — bootstrap-focused; document-store coverage limited to the registration shape.

**ai-skills has no dedicated document-database skill** for either Marten or Polecat — a notable topic gap on the ai-skills side. ai-skills' Marten coverage is conspicuously event-sourcing-heavy (10 of 13 Marten-prefixed skills are event-sourcing topics); the document-database surface that Cab uses for non-event-sourced state in the Payments BC has no upstream parallel.

**Audit conclusion: shape similar to Skill 8 (`wolverine-sagas`) — Cab is the authoritative reference.** This is essentially a "no substantive equivalent + entire-skill-creation candidate" reconciliation, with light cross-refs to the bootstrap-side setup overlap. Closer to Skill 8's saga-skill shape than to the partial-overlap Skill 15 shape.

**Section categorization.** 13 sections audited; 0 substantively trimmed; 2 cross-ref notes added; See Also restructured.

**Light cross-ref additions (no content removal):**

- **Sessions: lightweight is the default** (+1 line): added a one-line cross-ref note that ai-skills `polecat-setup-and-decision-guide` § Basic setup mentions `LightweightSession()` in the bootstrap context; the three-shape enumeration and Polecat-3.0-default-flip framing remain Cab value-add.
- **Storing and loading documents** (+1 line): added a one-line cross-ref note that basic `Store` and event-side `Append` shapes appear in ai-skills `polecat-setup-and-decision-guide` § Event sourcing with Polecat; the asymmetric Store-vs-Insert-vs-Update semantics and Cab convention remain Cab value-add.

**See Also restructure** (+14 net): applied three-block convention. New 3-entry Upstream block with `polecat-setup-and-decision-guide` as primary (explicitly noting **"No substantive coverage of session shapes, identity strategies, LINQ surface, soft deletes, patching, or document-level concurrency"** — those are documented in this Cab skill) + the other two ai-skills entries with brief notes on their orthogonal scope. Plus a substantial **"Cab's coverage substantially exceeds ai-skills' coverage of the document database"** framing paragraph enumerating the 9 doc-store surfaces Cab adds, with the explicit observation that **"ai-skills has no dedicated document-database skill for either Marten or Polecat — a notable topic gap on the ai-skills side."** Existing Upstream block became Prerequisites. External block trimmed (broken forward-looking placeholder removed; install/license note now in Upstream lead-in).

**Forward-looking placeholder removed:**

- `ai-skills polecat-document-store and polecat-event-sourcing` (in those exact names) — 10th forward-looking placeholder removed in Phase 5, second time the placeholdered names are wholly wrong rather than just nonexistent (Skill 15 was the first). Same broken pattern: speculation about ai-skills naming conventions that didn't pan out. The actual three Polecat ai-skills are differently named.

**Preserved entirely:**

- Top framing (Polecat as Marten with engine swapped + doc-store companion to event-sourcing)
- When to apply this skill / Do NOT use this skill for
- Polecat document-store in CritterCab (Cab posture: Payments BC document/event mix; same `IDocumentStore` and `IDocumentSession` for both)
- Sessions: lightweight is the default (three session shapes + Polecat-3.0-default-flip explanation + session options)
- Storing and loading documents (`Store`/`Insert`/`Update` asymmetry + `LoadAsync`/`LoadManyAsync` + Cab convention)
- Identity strategies (Guid auto-assigned / HiLo numeric / string application-assigned / strong-typed wrapper auto-handling)
- LINQ querying (standard LINQ via `session.Query<T>()` + differences from Marten: T-SQL JSON functions vs PostgreSQL jsonb operators + Polecat doesn't ship every Marten extension + `QueryForNonStaleData()` parallel)
- Raw SQL via `IAdvancedSql`
- Soft deletes (3 opt-in mechanisms: `[SoftDeleted]` attribute / `ISoftDeleted` interface / `opts.Policies` configuration; LINQ extensions: `MaybeDeleted` / `IsDeleted` / `DeletedSince` / `DeletedBefore`; restoration via `UndoDeleteWhere`; `HardDelete` force-delete)
- Patching (full operations enumeration: `Set` / `Increment` / `Append` / `AppendIfNotExists` / `Insert` / `Remove` / `Duplicate` / `Rename` / `Delete` + by-predicate patching + wire-bandwidth advantage framing)
- Optimistic concurrency (`IVersioned` Guid-based vs `IRevisioned` int-based + auto-detection + mutual exclusivity + explicit version checks via `UpdateExpectedVersion` / `UpdateRevision` + the `JasperFx.ConcurrencyException` retry pattern + the saga distinction crossing back to `polecat-event-sourcing`)
- What's NOT in Polecat that's in Marten (calibration section: append modes other than QuickAppend, PostgreSQL full-text search, identity-mapped default, niche extension methods)
- Cab use case: Payments BC documents (`MerchantConfig`, `PaymentSummary`, `RefundRequest`, `PaymentLifecycleSaga` state)
- Common pitfalls (12 bullets — incl. lightweight-as-opt-in misconception, Insert-vs-Store confusion, load-just-to-patch overhead, IRevisioned-on-saga, mutually-exclusive-concurrency-modes, soft-deleting-saga-state, PostgreSQL-jsonb-operators-in-raw-SQL, unit-of-work-boundary, soft-deleted-query-filtering, patch-by-predicate-zero-rows-silent, string-IDs-without-setting, mixing-event-and-document-across-multiple-sessions)

**Trim impact.** ~2 lines added in body cross-refs (no content removed), +14 lines added in See Also (3-entry Upstream block + Cab-exceeds framing paragraph + Prerequisites heading). File 31,093 → 33,533 bytes (+2,440 bytes / +7.8%). Net-positive byte count is the dominant outcome — comprehensive Cab content + new substantive Upstream block. Methodology refinement #8 territory: skills where Cab content is explicitly value-add over ai-skills produce net-positive byte counts because the new Upstream block (with detailed counterpart entries explicitly framing what Cab adds) outweighs any modest body trims.

**One consolidated upstream-contribution candidate flagged (entry #32, Observed gap priority):**

The entire `polecat-document-store` surface as an entire-skill-creation candidate, parallel to `wolverine-sagas` (entry #9). Cab's 31 KB skill (now 34 KB after the See Also restructure) covers session shapes, Store/Insert/Update asymmetry, identity strategies, LINQ surface with Marten differences, Raw SQL, soft-delete mechanisms with LINQ extensions and restoration, patching API, document-level optimistic concurrency, Marten-feature-omission calibration, Cab BC narrative, and 12 pitfalls — none of which has a substantive ai-skills counterpart.

**Reasoning for one consolidated entry rather than fragmenting:** The patterns hang together as a coherent topic (document database operations on the Critter Stack). Splitting into 7-8 individual sub-candidates (lightweight-as-default, IVersioned-vs-IRevisioned auto-detection, soft-delete three-opt-ins, patching API operations, HiLo numeric IDs, strong-typed ID wrapper auto-handling, what's-NOT-in-Polecat calibration, UndoDeleteWhere/HardDelete restoration patterns) would risk losing the calibration framing that depends on the surface being viewed as a whole. If JasperFx eventually authors this skill, Cab content becomes substantial fuel — same model as `wolverine-sagas` (entry #9) and `wolverine-grpc-handlers` + `wolverine-grpc-bidirectional-handlers` (entry #5). Priority **Observed gap** because no known active author for an ai-skills document-database skill (distinct from gRPC where Erik is the planned author).

**No new Cab coverage gaps revealed.** Skill 15 already surfaced the relevant doc-store-adjacent gaps (#7 Polecat MCP server is the closest, but already tracked). No new gaps from Skill 16.

**No new ai-skills content drift.** The Skill 14/15 DCB-Polecat-only drift doesn't surface here because doc-store-side audit doesn't traverse DCB content. Existing tracker entry stands.

**Tier 2 complete (2/2).** Skill 16 closes Tier 2 (Polecat skills). Per Erik's directive, the next step is the Skill 10 (`marten-wolverine-aggregates`) revisit for `[ConsistentAggregate]` / `[ConsistentAggregateHandler]` coverage before Tier 3 begins.

---

### 17. `csharp-coding-standards`

**Counterpart(s).** **None — out-of-scope topic for ai-skills.** Verified by enumerating the full ai-skills directory (70 skills): no `csharp-*`, `dotnet-*`, `coding-*`, `style-*`, `language-*`, or `conventions-*` skills exist. ai-skills' library scope is entirely Critter Stack ecosystem-focused (Wolverine + Marten + Polecat + integrations + testing + observability + critterstack-arch). General .NET coding standards are project-specific and inherently outside ai-skills' charter.

**New "No equivalent" sub-category surfaced (methodology refinement #10).** Phase 5's "No equivalent" category previously had two sub-shapes:

| Sub-category | Skills | Upstream-contribution status |
|---|---|---|
| In-scope topic, unauthored, **active author** | 4, 5 (gRPC) | Active (Erik) |
| In-scope topic, unauthored, **observed gap** | 8 (sagas), 16 (polecat-document-store) | Observed gap |
| **Out-of-scope topic for ai-skills** | **17** | **N/A — not a candidate** |

Coding standards are project-specific by nature; every consuming project has its own conventions. JasperFx ai-skills wouldn't be the right home for them. Cab's `csharp-coding-standards` is the authoritative reference by definition; there's no upstream parallel to point to and no candidate for upstream contribution.

**Section categorization.** 18 H2 sections audited; 0 trimmed; See Also lightly trimmed (1 line).

All 18 sections are either general C# language features applied to Cab's domain or Cab-specific design conventions. Zero overlap with ai-skills:

- Records and Immutability (incl. `Trip.Apply` example with explicit cross-ref to `marten-aggregates`; `required` modifier guidance)
- Sealed by Default
- Collection Patterns (`IReadOnly*` for DTOs vs `Immutable*` for aggregates — Cab-specific design rationale for the split + performance escape hatch)
- Value Object Pattern (full structure with JsonConverter; `GeoLocation`, `LicensePlate` examples)
- FluentValidation (validators nested inside command record convention)
- Modern Guard Clauses (.NET 8+ `ArgumentException.ThrowIfX` family)
- Status Enums (single-source-of-truth pattern over multiple booleans)
- Factory Methods (incl. `TimeProvider` parameter convention)
- Nullable Reference Types (incl. message field nullability rules)
- Pattern Matching
- Async/Await
- `IAsyncEnumerable<T>` Patterns (`WithCancellation`, `[EnumeratorCancellation]`; gRPC sub-section cross-refs `wolverine-grpc-handlers`)
- GUIDs (`Guid.CreateVersion7()` default; UUID v5 cross-ref to `marten-aggregates`)
- `DateTimeOffset` (always UTC convention)
- `TimeProvider` for Testable Time (with `FakeTimeProvider` integration)
- Decimal and Financial Calculations (banker's rounding warning; `MidpointRounding.AwayFromZero` for explicit non-banker's behavior)
- .NET 10 Gotcha: `Guid.Variant` and `Guid.Version` (cross-cutting Cab footgun also covered in Skill 14 `dynamic-consistency-boundary` § Tag types and roadmap entry #20)
- Naming Conventions (Cab-specific table)

**Edit applied (single line removal):**

- **Removed tangential `ai-skills wolverine-handlers-fundamentals` reference** from the External block. Redundant with the Downstream chain through `wolverine-handlers` (which itself has a proper Upstream block referencing `wolverine-handlers-fundamentals` since Skill 1's reconciliation). Readers wanting generic Wolverine handler idioms should follow the chain through `wolverine-handlers`.

**No `Upstream` → `Prerequisites` rename** — no existing `Upstream` block (this is a foundational skill with nothing to load first).

**No new `Upstream` block** — no ai-skills counterpart to populate it with.

**No forward-looking placeholder removed** — none present. **First skill in Phase 5 with no forward-looking placeholder to remove.** This is meaningful: pre-Phase-5 Cab had a habit of placeholdering not-yet-published ai-skills counterparts, but the habit doesn't extend to topics that are out-of-scope for ai-skills (which makes sense — there's no plausible future ai-skills skill to placeholder for).

**No upstream-contribution candidates flagged.** The topic is out-of-scope for ai-skills; flagging upstream contribution would require ai-skills to expand into general .NET coding standards, which isn't the library's purpose.

**No Cab coverage gaps revealed.** Foundational/Cab-specific skill; no ai-skills topics that Cab is missing surface here.

**No ai-skills content drift.**

**Trim impact.** Net 1 line removed from External block. File 28,888 → 28,771 bytes (−117 bytes / −0.4%). **Lightest pass in Phase 5 to date** — even lighter than Skills 4/5/8 (rename-only) because there's no `Upstream` block to rename and no forward-looking placeholder to remove. This is the bottom of the trim spectrum: a true zero-content-impact reconciliation that's purely a documentation hygiene pass.

**Tier 3 begun (1/18).** Skill 17 is the first Tier 3 reconciliation. Pattern expectation for remaining Tier 3 skills: most are likely to be similar Cab-specific patterns with low or no ai-skills overlap. The "out-of-scope topic for ai-skills" sub-category will likely repeat for several upcoming Tier 3 skills (`vertical-slice-organization`, `identity-acl`, `polyglot-go-service` are early candidates by name). Other Tier 3 skills may have partial overlaps where Cab applies generic patterns to Critter Stack contexts (`adding-a-service`, `service-bootstrap` likely overlap with `critterstack-arch-new-project-wolverine-marten` and similar arch skills).

---

### 25. `vertical-slice-organization`

**Counterpart.** `critterstack-arch-vertical-slice-fundamentals` (12.1 KB, direct counterpart). The two skills are **complementary, not duplicates**: ai-skills covers the introductory/why layer (VSA principle, Wolverine-vs-MediatR comparison, complete slice example, aggregate file convention, why-not-Clean/Onion/Hexagonal, slice testing, query slices, message handler slices, decision guidance); Cab covers the project-specific *how* (intra-service scope, capability-folder naming, file naming reference table, validator-nested-inside-command rule, type-grouped-files anti-pattern, domain event subscription handler colocation, ADR-002 cross-service boundary, Shared/ 3+ threshold, migration-suffix prohibition, acceptable technical folders, AI Agent counter-pressure framing, Cab BC worked example).

Note: `critterstack-arch-modular-monolith` is adjacent but **not a counterpart** — Cab's skill explicitly scopes itself to intra-service organization; modular-monolith composition is inter-service, deferred to ADR-002 and `adding-a-service`.

**Audit shape similar to Skill 12 (`marten-querying`)** — counterpart exists for the introductory layer; most Cab content is project-specific value-add.

**Section categorization.** 15 sections audited; 0 trimmed; 1 cross-ref note added; See Also restructured.

**Edits applied:**

- **Core Principle cross-ref note** (+1 line): added one sentence pointing to `critterstack-arch-vertical-slice-fundamentals` for VSA fundamentals, why-not-Clean/Onion/Hexagonal, slice testing, decision guidance — all upstream context Cab correctly defers.
- **See Also restructure** (+~6 net): three-block convention. New 1-entry Upstream block with `critterstack-arch-vertical-slice-fundamentals` as primary counterpart noting what Cab adds (project-specific naming conventions, file colocation rules, anti-patterns, AI-agent counter-pressure framing). Existing Upstream block became Prerequisites. Dropped tangential `ai-skills wolverine-handlers-fundamentals` reference from External (redundant with the chain through `wolverine-handlers` Cab skill which has its own proper Upstream block).

**No body trims** — Cab content is all project-specific or Cab-vocabulary-specific. The introductory framing in Core Principle is brief enough to stand without compression.

**No forward-looking placeholders removed** — none present.

**Three upstream-contribution candidates flagged** (entries #33, #34, #35):

1. **Type-grouped files anti-pattern** (`DispatchCommands.cs` / `DispatchEvents.cs` containing 10+ records). ai-skills' VSA fundamentals doesn't explicitly call this out; Cab's framing about merge conflicts when multiple developers work on the same BC and obscured intent is operationally important.
2. **Migration-suffix naming prohibition** (no `*ES`, `*V2`, `*New`, `*Refactor`, `*Polecat` in file/class names). ai-skills doesn't address persistence-migration naming hygiene. Cab's principle ("file names describe the business operation, not the persistence strategy or migration history") is generic enough to apply to any project.
3. **AI Agent counter-pressure framing**. Cab's explicit guidance to override the default layered-architecture instinct when AI agents scaffold new services. ai-skills doesn't address AI scaffolding behavior — but ai-skills *is* loaded by AI agents during scaffolding, so the framing is meta-relevant to the ai-skills library itself.

**No Cab coverage gaps revealed.** ai-skills covers Wolverine-vs-MediatR comparison, why-not-Clean/Onion/Hexagonal, slice testing examples, decision guidance — all upstream context that Cab's narrower-scope skill correctly defers. Not gaps.

**No ai-skills content drift.**

**Trim impact.** Net file +799 bytes (+4.4%). Methodology refinement #8 territory — design-and-conventions skill produces small net-positive byte change because the new Upstream block (with detailed counterpart entry) outweighs the modest ~1-line trim and ~1-line cross-ref addition.

**Preserved entirely:** all 15 H2 sections including top framing with explicit scope demarcation, When to apply / does NOT govern, Core Principle (✅/❌ comparison), Folder Naming (capability table), File Naming (reference table), Why colocate validator + handler, type-grouped files anti-pattern, Domain Events (one-per-file + subscription handler colocation), Slices Stay Within One Service (ADR-002), Shared Types Within a Service (3+ threshold), No Migration-Suffix Naming, Acceptable Technical Folder, AI Agent Defaults Counter-Pressure, Worked Example: Trips Service, Common Pitfalls (7 bullets).

---

## Methodology refinements emerging in Phase 5

_(updated as the reconciliation progresses)_

1. **`See Also` three-block convention** (from Skill 1, refined after review). `Upstream` (ai-skills) → `Prerequisites` (Cab-internal) → `Sibling skills` → `Downstream` → `External`. Adopted as the Phase 5 standard; subsequent skills follow it. The rename `Upstream` → `Prerequisites` happens uniformly across every Phase 5-reconciled skill.
2. **License framing in `Upstream` lead-in.** Each skill's `Upstream` block lead-in mentions "ai-skills (license required, install via `npx skills add`)" once. ai-skills content is never inlined into Cab skills — only skill names are referenced. This honors the proprietary/licensed status of ai-skills.
3. **Trim estimates are systematically high** (from Skill 3). Naive line-counting of removed code blocks overestimates net trim because cross-reference prose and expanded `Upstream` blocks (with 3 detailed entries replacing 3 one-liners) consume most of the savings. Skill 3 estimated ~50 lines saved; actual was ~26. Future estimates should account for the prose-and-upstream-block offset — a useful rule of thumb is `actual_trim ≈ 0.5 × raw_lines_removed`.
4. **Cross-referencing ai-skills counterparts without a Cab parallel** (from Skill 3). When ai-skills covers a topic that Cab doesn't have a dedicated skill for (e.g., `wolverine-messaging-resiliency-policies`), the Cab `Upstream` block can include the ai-skills entry with a brief note that no Cab parallel exists. This is honest about Cab's current coverage gaps and points the reader to the authoritative upstream.
5. **Handling "No equivalent in ai-skills" cases** (from Skill 4). When ai-skills has no counterpart today, the reconciliation pass is a light rename-only: `Upstream` → `Prerequisites`, remove any forward-looking placeholders for not-yet-published ai-skills counterparts (they mislead readers into searching for nonexistent skills), and skip the new `Upstream` block entirely. The Cab skill remains the authoritative reference until ai-skills publishes a parallel. If an ai-skills counterpart is actively planned (with a known author), record it in the upstream-contribution roadmap with an "Active" priority rather than "_TBD_".
6. **Trim ratio depends on duplication shape** (refined from Skill 6). The 50% rule of thumb (refinement #3) applies when duplication is mostly **prose** — cross-reference paragraphs and expanded `Upstream` blocks consume most of the savings. When duplication is mostly **code blocks** (Skill 6's case), trims remove dense short lines that don't get fully replaced by prose; actual trim approaches `~1.0 × raw_lines_removed`. Skill 6 estimated ~120 lines, actual was ~125 — nearly 1:1. Skill 7 confirmed: ~160 lines actual (8.2% byte reduction). Future estimates should consider whether the target sections are code-heavy (closer to 1:1) or prose-heavy (closer to 0.5:1).
7. **Section presence in Cab implies Cab uses it** (from Skill 7). When auditing a Cab skill, distinguish between two categories of content the audit may flag for action: (a) **mechanic duplicates ai-skills** → trim/cross-reference (the standard pattern through Skills 1–6); (b) **tool/pattern Cab doesn't use** → remove entirely. Skill 7 surfaced this when Erik flagged that the MassTransit/NServiceBus interop section was incorrect: Cab doesn't use MT/NSB and never will. The section was likely content drift from ai-skills or generic Wolverine docs during original authoring. The audit should explicitly check: does Cab actually use the patterns this section describes? If no, remove rather than trim. Future Phase 5 audits should look for content-drift sections of this kind.
8. **Design-and-conventions skills produce minimal trims** (from Skill 9). When a Cab skill is explicitly positioned as design+conventions with mechanics deferred to ai-skills (e.g., `marten-aggregates`), duplication is minimal by design and the trim is correspondingly small. Net byte change can even be slightly *positive* because the new Upstream block (with detailed counterpart entries) outweighs the modest mechanic trims. The reconciliation value for these skills is See Also restructure and forward-looking placeholder removal, not byte-saving. Audits of similar skills should expect this shape and not over-aggressively trim Cab-specific framing in search of reduction. Likely candidates upcoming: several remaining Tier 1 Marten skills, plus most of Tier 3 (Cab-specific patterns).
9. **Content-addition revisit pattern for cross-cutting gaps** (from Skill 10 revisit, post-Skill-16). When a per-skill audit surfaces a coverage gap that affects an *earlier* Cab skill rather than the one being audited (here: Skill 15's audit revealed `[ConsistentAggregate]` / `[ConsistentAggregateHandler]` was missing from Skill 10's `marten-wolverine-aggregates`), schedule a content-addition revisit for the affected earlier skill. Apply the standard Phase 5 cadence (audit → pause → report → greenlight → apply) but with **"what to add"** replacing **"what to trim"** as the planning question. The revisit should reference the canonical ai-skills patterns (here: ai-skills `polecat-cross-stream-operations` four real-world scenarios) but rebuild the content with Cab BC vocabulary and Cab convention shapes (here: handler shape `IEnumerable<object>` + yield, Cab BC scenarios in Dispatch / Trips / Onboarding). Document the revisit as a sub-entry under the original per-skill detail (rather than a separate timeline entry) so the per-skill record stays organized by skill. Cross-cutting gaps that span multiple Cab skills can often be closed by adding to one skill plus a cross-store note (here: Skill 10's new section closes both the Marten-side gap and — via cross-store note pointing to `polecat-event-sourcing` § Wolverine integration — the Polecat-side gap, with no separate Skill 15 revisit needed). Future Phase 5 cross-cutting gaps surfaced in later-tier audits should follow this pattern.
10. **Out-of-scope topic sub-category for "No equivalent" reconciliations** (from Skill 17). Phase 5's "No equivalent" category previously had two sub-shapes: (a) in-scope topic with active author (Skills 4, 5 gRPC — Erik's roadmap, Active priority); (b) in-scope topic with no active author (Skills 8 sagas, 16 polecat-document-store — Observed gap priority). Skill 17 (`csharp-coding-standards`) surfaces a third sub-shape: **out-of-scope topic for ai-skills**. ai-skills' library scope is the Critter Stack ecosystem; general .NET coding standards are project-specific and inherently outside that scope. Out-of-scope topics produce **zero upstream-contribution candidates** because flagging would require ai-skills to expand its scope, which isn't the library's purpose. Reconciliation is a single-line tangential-reference cleanup at most; no `Upstream` block, no `Prerequisites` rename, no forward-looking placeholder removal. **Identification heuristic:** if the Cab skill's body has zero overlap with any ai-skills skill (regardless of how distant the topic), and the topic isn't plausibly within Critter Stack ecosystem authoring scope, it's out-of-scope rather than an unauthored gap. Likely upcoming Tier 3 candidates by name: `vertical-slice-organization`, `identity-acl`, `polyglot-go-service`, possibly `event-modeling`. Other Tier 3 skills may have partial overlaps where Cab applies generic patterns to Critter Stack contexts (`adding-a-service`, `service-bootstrap` likely overlap with `critterstack-arch-new-project-wolverine-marten` and similar arch skills).
11. _(more entries to come)_

---

## Upstream-contribution roadmap

Compiled list of Cab patterns/sections flagged as upstream-contribution candidates during Phase 5. To be synthesized at Phase 5 close-out into a prioritized list for post-Phase-5 follow-up.

| # | Skill | Pattern / section | Rationale | Priority |
|---|---|---|---|---|
| 1 | `wolverine-handlers` | `session.Events.StartStream<T>(...)` direct-call silent-failure footgun (Marten-side) | Generic Wolverine+Marten pitfall not covered in any current ai-skills handler skill; reproduces silently and is hard to diagnose | _TBD at close-out_ |
| 2 | `wolverine-http-handlers` | Mixed route + JSON body breaks compound handlers | Genuine framework limitation — `Validate`/`Handle` shared-state pattern fails because Wolverine's parameter resolution can't see across the route-vs-body boundary at the validation step | _TBD at close-out_ |
| 3 | `wolverine-http-handlers` | Tuple-ordering silent failure on HTTP endpoints | ai-skills states the rule but doesn't articulate the failure mode (`IStartStream` serialized to response body, stream never starts) — Cab's footgun framing is the value-add | _TBD at close-out_ |
| 4 | `wolverine-messaging-handlers` | `OutgoingMessages`-without-routing-rule silent-failure footgun | ai-skills covers `PublishAsync` no-subscriber semantics but doesn't frame the parallel scenario for `OutgoingMessages` cascading returns; this is the more common Cab failure mode because `OutgoingMessages` is the preferred pattern | _TBD at close-out_ |
| 5 | `wolverine-grpc-handlers` + `wolverine-grpc-bidirectional-handlers` | Both Cab gRPC skills — proto-first auto-generated path (unary + server-streaming) AND hand-written workaround for client-streaming + bidirectional patterns, `[WolverineGrpcService]` stub, AIP-193 exception mapping, Wolverine 5.32+ wiring | **Erik is the planned author** of the future ai-skills `wolverine-grpc` skill. Cab content across both skills is the basis for that work; once ai-skills publishes, both Cab skills should be revisited and likely thinned to their Cab-specific layers (Aspire dev-cert, ADR-009 conventions, Cab BC examples, hand-written workaround status as Wolverine evolves). | **Active** (Erik's roadmap, not TBD) |
| 6 | `wolverine-kafka` | EH Emulator AutoProvision constraint | ai-skills `wolverine-integrations-kafka` doesn't mention Azure Event Hubs Emulator at all. The EH Emulator's lack of Kafka admin APIs (so `AutoProvision()` fails) is a genuine pitfall for any Wolverine+Kafka deployment targeting Azure. Cab's framing ("use real Kafka container locally; switch to EH Emulator only for EH-specific integration tests") is Cab-discovered guidance worth upstreaming. | _TBD at close-out_ |
| 7 | `wolverine-kafka` | `BatchMessagesOf<T>` for batch Kafka consumption | ai-skills omits batch consumption entirely. The mechanic is generic Wolverine; Cab's framing pairs it with high-volume Kafka topics where per-message invocation overhead is wasteful. Both the mechanic existence and the Kafka-pairing rationale could be upstream additions. | _TBD at close-out_ |
| 8 | `wolverine-azure-service-bus` | MaxDeliveryCount × Wolverine retries multiplicative interaction | ai-skills mentions both retry types separately but doesn't articulate how they compose: a Wolverine retry of N inside ASB `MaxDeliveryCount` of M means up to N×M total processing attempts. This composition footgun is Cab-discovered framing. | _TBD at close-out_ |
| 9 | `wolverine-sagas` | Entire skill — Wolverine saga patterns including the `Saga` base class, method-name conventions (Start/StartOrHandle/Handle/Orchestrate/NotFound), saga-ID resolution cascade, valid saga-ID types, TimeoutMessage scheduling, saga-not-found semantics, Marten + Polecat persistence, optimistic concurrency via IRevisioned, the static-allowed-vs-instance-required asymmetry | ai-skills has no `wolverine-sagas-*` skill at all. Cab's 39 KB skill is comprehensive coverage of an entire ai-skills topic gap. **No active author** — unlike the gRPC skills (Erik is the planned author), no one in the JasperFx ecosystem is writing this currently as far as we know. If someone decides to author it, Cab content is substantial fuel. | **Observed gap** (no active author) |
| 10 | `marten-wolverine-aggregates` | `MartenOps.StartStream` Action-overload for capturing assigned stream ID | ai-skills covers `MartenOps.StartStream<T>(events)` (auto-assigned) and `MartenOps.StartStream<T>(id, events)` (explicit ID) but not the Action-overload pattern: `MartenOps.StartStream<T>(s => { assignedId = s.Id; }, events)`. Genuinely useful when the handler needs the auto-assigned ID for an integration message body. Cab uses this pattern; ai-skills could add it as a brief note in § Starting new streams. | _TBD at close-out_ |
| 11 | `marten-projections` | Projection registration silent-failure footgun | ai-skills' `marten-projections-single-stream` covers projection registration but doesn't (per the heads I audited) explicitly call out the silent-failure mode: a `SingleStreamProjection<TDoc, TId>` subclass that's never added to `opts.Projections` does nothing, and the compiler doesn't catch this. Parallel to the `OutgoingMessages`-without-routing-rule footgun (entry #4). Genuinely Cab-discovered framing worth upstreaming. | _TBD at close-out_ |
| 12 | `marten-querying` | Compiled query footgun list | Cab's Compiled Queries section enumerates 4 silent failures (async operators in `QueryIs()`, primary constructors not inspectable by the planner, `ToList()`/`ToArray()` in body, boolean fields as query parameters) plus the `IQueryPlanning` + `[MartenIgnore]` paging pattern. ai-skills' `marten-advanced-indexes-and-query-optimization` mentions compiled queries in one paragraph as a brief tip — none of the gotchas. The footgun list is substantial Cab-discovered framing worth upstreaming. | _TBD at close-out_ |
| 13 | `marten-querying` | Modern Wolverine HTTP-first streaming API (`StreamOne<T>` / `StreamMany<T>` / `StreamAggregate<T>` return types) | ai-skills' `marten-advanced-optimization` covers Marten.AspNetCore JSON streaming using only the older extension-method API (`WriteArray`, `WriteById`, `WriteSingle`). The modern return-type API integrates with Wolverine's `IResult` pipeline and generates OpenAPI metadata automatically without `[ProducesResponseType]`. ai-skills even calls out `[ProducesResponseType]` as a manual workaround, which the new API obviates. Cab covers it in detail with 404-semantics-differ for `StreamOne`/`StreamMany`/`StreamAggregate` and the `IDocumentSession` requirement footgun. | _TBD at close-out_ |
| 14 | `marten-async-daemon` | April 2026 daemon improvements (`EnableEventTypeIndex` + Adaptive event loader, Marten 8.29+) | ai-skills' `marten-advanced-async-daemon-deep-dive` doesn't mention either feature. The opt-in `(type, seq_id)` composite B-tree index dramatically speeds up rebuilds for projections that filter on a small subset of event types over millions of events; the adaptive event loader's 3-strategy fallback (Normal → Skip-ahead → Window-step) handles the no-index case automatically and emits the diagnostic signal that suggests enabling the index. **The most consequential daemon performance additions of 2026** — belongs in ai-skills' deep-dive at next revision. | _TBD at close-out_ |
| 15 | `marten-async-daemon` | Position strategies for new projections (`SubscribeFromPresent` / `SubscribeFromTime` / `SubscribeFromSequence` / `SubscribeAsInlineToAsync`) | ai-skills' deep-dive doesn't cover any of the 4 strategies. Critical operational topic — without it, every new async projection introduced to a system with existing events forces a full historical rebuild. Cab covers all 4 with use-case framing for each. The fourth strategy (`SubscribeAsInlineToAsync`) is the specific helper for promoting an existing inline projection to async without rebuilding. | _TBD at close-out_ |
| 16 | `marten-async-daemon` | Health check `gracePeriod` parameter for K8s rolling restarts | ai-skills doesn't cover async daemon health checks (`AddMartenAsyncDaemonHealthCheck`) at all. Cab's framing of the `gracePeriod` parameter exempts the daemon from the lag check during the configured startup window — essential for containerized deployments where K8s would otherwise kill the pod before it's stable. Lower priority than entries #14 and #15 because the entire health-check topic is missing rather than one sub-feature. | _TBD at close-out_ |
| 17 | `dynamic-consistency-boundary` | `[BoundaryModel]` on `Validate` parameter → CS0128 codegen error | Wolverine codegen produces "duplicate local variable" when the attribute appears on both `Validate` and `Handle` methods. The pipeline hook receives the projected state by plain-parameter injection automatically; only `Handle` needs the attribute. ai-skills doesn't cover `Validate` hooks for `[BoundaryModel]` at all. Boot-time failure with confusing diagnostic. | _TBD at close-out_ |
| 18 | `dynamic-consistency-boundary` | `ValidateAsync` + `[BoundaryModel]` silent handler discovery failure | Combination doesn't compose; Wolverine's handler discovery silently skips it. The synchronous `Validate` works correctly. Most insidious DCB footgun because the failure mode is silent — the handler simply isn't discovered, no error message indicates why. ai-skills doesn't mention the incompatibility. | _TBD at close-out_ |
| 19 | `dynamic-consistency-boundary` | `AndEventsOfType` required after `For()`/`Or(tag)` in fluent form | Without it, `FetchForWritingByTags` throws `ArgumentException` at runtime. The imperative `.Or<TEvent, TTag>(...)` form doesn't have this trap because event type and tag are specified together. ai-skills shows the fluent form without flagging the requirement. | _TBD at close-out_ |
| 20 | `dynamic-consistency-boundary` | Tag types must be single-property records — raw `Guid` fails under .NET 10 | `Guid` has multiple public instance properties (notably `Variant` and `Version` since .NET 10), so `RegisterTagType<Guid>` throws `InvalidValueTypeException` at boot via `JasperFx.Core.Reflection.ValueTypeInfo.ForType` requiring exactly one public gettable instance property. Particularly relevant timing — this footgun didn't exist before .NET 10 made `Guid.Version`/`Variant` public; older tutorials may show raw-Guid registration that no longer works. ai-skills doesn't cover tag-type registration constraints. | _TBD at close-out_ |
| 21 | `dynamic-consistency-boundary` | Boundary state `public Guid Id { get; set; }` requirement under Marten 8 | Marten 8 registers boundary state types as documents; missing `Id` surfaces as `InvalidDocumentException` **at fixture teardown** rather than at boot or save time — easy to miss until tests fail with confusing error messages. The canonical Wolverine repo `SubscriptionState` examples *omit* `Id`, but they're tested under conditions where the missing identity doesn't surface. Cab on Marten 8 needs it. ai-skills shows examples without `Id`. | _TBD at close-out_ |
| 22 | `dynamic-consistency-boundary` | `StartStream` drops tags | `MartenOps.StartStream` and `session.Events.StartStream` re-wrap raw event objects internally — pre-wrapped `IEvent` instances passed in lose their tags. The right path for tagged events is `BuildEvent` + `WithTag` + `Append(streamId, wrapped)`. Cab-discovered behavior; the workaround pattern for `UseMandatoryStreamTypeDeclaration = true` (StartStream then `PendingChanges.Streams().Single().Events.Single().AddTag(...)`) is novel. ai-skills doesn't cover this. | _TBD at close-out_ |
| 23 | `dynamic-consistency-boundary` | `DcbConcurrencyException` and `ConcurrencyException` are siblings, not parent-child | A retry policy that catches one does not catch the other. `OnException<MartenException>` catches both but is too broad. Both need explicit registration: `OnException<ConcurrencyException>().RetryWithCooldown(...)` AND `OnException<DcbConcurrencyException>().RetryWithCooldown(...)`. The same applies to Polecat (`Polecat.Events.Dcb.DcbConcurrencyException`). ai-skills doesn't cover concurrency exception hierarchy. | _TBD at close-out_ |
| 24 | `polecat-event-sourcing` | v3.0 `Snapshot<T>()` framing as registration shortcut | Cab's framing of how `Snapshot<T>()` builds and registers a `SingleStreamProjection<T, TId>` internally — with the aggregate persisting to standard `pc_doc_{type}` document tables — is conceptually clearer than ai-skills' brief mention. The framing helps readers understand Polecat 3.0's removal of the special `pc_streams.snapshot` / `snapshot_version` columns and the unification with Marten's snapshot model. ai-skills covers `Snapshot<T>` registration but doesn't make the registration-shortcut framing explicit. | _TBD at close-out_ |
| 25 | `polecat-event-sourcing` | v3.0 schema breaking change documentation (pc_streams snapshot columns removed) | Important migration context Cab covers prominently in the top framing and Common pitfalls (the "Assuming `pc_streams` carries the snapshot" pitfall). ai-skills' three Polecat skills don't mention the v3.0 schema change at all. Anyone migrating from pre-v3.0 Polecat would benefit from this being upstream. | _TBD at close-out_ |
| 26 | `polecat-event-sourcing` | v3.1 `IDocumentStoreUsageSource` publishing for monitoring discovery | Polecat 3.1 made `IDocumentStore` implement `IDocumentStoreUsageSource` so monitoring tools (CritterWatch, Wolverine `ServiceCapabilities.readDocumentStores`) can discover the store via `services.GetServices<IDocumentStoreUsageSource>()`. Purely additive monitoring infrastructure. The matching `EnableExtendedProgressionTracking` opt-in adds `heartbeat` / `agent_status` / `pause_reason` / `running_on_node` columns to `pc_event_progression` for CritterWatch alerting. Not in ai-skills. | _TBD at close-out_ |
| 27 | `polecat-event-sourcing` | Polecat saga concurrency story (Saga.Version managed by Wolverine, NOT IRevisioned auto-detection) | Cab resolves the `wolverine-sagas` deferred verification with verified explanation including the comment from `PolecatPersistenceFrameProvider.DetermineUpdateFrame`: *"Wolverine's Saga type uses Version property which is handled by the saga persistence framework."* `IRevisioned` opt-in is for non-saga documents only. Significant footgun resolution: adding `IRevisioned` to a saga doesn't activate revision-aware updates and signals to readers that you've conflated two different concurrency contracts. ai-skills doesn't cover this distinction. | _TBD at close-out_ |
| 28 | `polecat-event-sourcing` | Polling daemon vs LISTEN/NOTIFY architectural framing | Cab's explicit framing of this as a deliberate architectural tradeoff — "switching to PostgreSQL because polling is bad is solving a problem at the wrong layer" — plus the SQL Server `LEAD()` window function for high-water-mark gap detection are operational guidance ai-skills doesn't have. The 500ms default polling interval is by design (SQL Server lacks pub/sub primitives), and tuning `DaemonSettings` is acceptable, but the architectural decision is upstream of the daemon configuration. | _TBD at close-out_ |
| 29 | `polecat-event-sourcing` | EventForwarding cross-BC publication via `SubscribeToEvent<T>().TransformedTo(...)` | The `PolecatIntegration.SubscribeToEvent<T>()` extension exposes a transformation that wraps `IEvent<T>` into an integration message published as a Wolverine message. The transformation runs in the same outbox transaction as the Polecat event append, so cross-BC publication is atomic with the event commit. Wolverine integration pattern not in ai-skills. | _TBD at close-out_ |
| 30 | `polecat-event-sourcing` | `UseFastEventForwarding` + `UseWolverineManagedEventSubscriptionDistribution` decision framework | Two `PolecatIntegration` options with operational implications: `UseFastEventForwarding` publishes events through Wolverine's messaging infrastructure on `SaveChangesAsync` without ordering guarantees (faster but no ordering); `UseWolverineManagedEventSubscriptionDistribution` distributes async projection and subscription processing across cluster nodes via Wolverine's agent framework. Cab covers both with decision criteria (Cab Payments BC: ordering matters for compliance, so `UseFastEventForwarding = false`). ai-skills covers only the latter, in passing. | _TBD at close-out_ |
| 31 | `polecat-event-sourcing` | Cross-store `IntegrateWithWolverine` confusion warning for mixed Marten + Polecat solutions | The extension method exists on both `MartenConfigurationExpression` and `PolecatConfigurationExpression`. In a service that registers BOTH (rare in Cab but supported), `.IntegrateWithWolverine()` must be called on whichever is the primary store for that service — Wolverine's saga storage and message storage need exactly one home. Cab's pitfall about exactly-one-primary-store is operationally important; ai-skills doesn't cover this edge case. | _TBD at close-out_ |
| 32 | `polecat-document-store` | Entire skill — Polecat document database surface | ai-skills has no dedicated document-database skill for either Marten or Polecat. The three Polecat ai-skills (`polecat-setup-and-decision-guide`, `polecat-cross-stream-operations`, `critterstack-arch-new-project-wolverine-polecat`) cover only bootstrap-side document-store registration. Cab's 31 KB skill covers session shapes (lightweight default vs identity-map vs query, Polecat-3.0-default-flip), Store/Insert/Update asymmetric semantics, identity strategies (Guid auto-assigned / HiLo numeric / string application-assigned / strong-typed wrapper auto-handling), LINQ surface and Marten-vs-Polecat differences (T-SQL JSON functions vs PostgreSQL jsonb operators), Raw SQL via `IAdvancedSql`, soft-delete 3 opt-in mechanisms with LINQ extensions and restoration patterns (`UndoDeleteWhere`) and `HardDelete` force-delete, patching API (`Set`/`Increment`/`Append`/`AppendIfNotExists`/`Insert`/`Remove`/`Duplicate`/`Rename`/`Delete` + by-predicate), document-level optimistic concurrency (`IVersioned` Guid-based vs `IRevisioned` int-based + auto-detection + mutual exclusivity + explicit version checks), and the calibration section listing Marten features deliberately omitted from Polecat. Substantial fuel for a future ai-skills `polecat-document-database` and/or `marten-document-database` pair. **No active author known** — same status as `wolverine-sagas` (entry #9) and parallel to gRPC's entry #5 in shape but Observed-gap-priority rather than Active. | **Observed gap** (no active author) |
| 33 | `vertical-slice-organization` | Type-grouped files anti-pattern (`DispatchCommands.cs` / `DispatchEvents.cs` containing 10+ records) | ai-skills' `critterstack-arch-vertical-slice-fundamentals` doesn't explicitly call out the type-grouped-files failure mode. Cab's framing — merge conflicts when multiple developers work on different features in the same BC, obscured intent (the folder shows you `DispatchCommands.cs` and `DispatchEvents.cs` but not what the BC does) — is operationally important for any team adopting VSA. Worth upstreaming as an explicit anti-pattern. | _TBD at close-out_ |
| 34 | `vertical-slice-organization` | Migration-suffix naming prohibition (no `*ES`, `*V2`, `*New`, `*Refactor`, `*Polecat` in file/class names) | ai-skills doesn't address persistence-migration naming hygiene. Cab's principle — "file names should describe the business operation, not the persistence strategy or migration history" — is generic enough to apply to any project that's gone through a store migration. The temporary-suffix-during-migration cleanup rule is also worth upstreaming. | _TBD at close-out_ |
| 35 | `vertical-slice-organization` | AI Agent counter-pressure framing | Cab's explicit guidance to override the default layered-architecture instinct when AI agents scaffold new services. ai-skills doesn't address AI scaffolding behavior at all — but ai-skills *is* loaded by AI agents during scaffolding, so the framing is meta-relevant to the ai-skills library itself. The explicit list of forbidden generic folder names (`Commands/`, `Events/`, `Queries/`, `Handlers/`, `Validators/`, `Aggregates/`, `Data/`, `Entities/`, `Models/`, `Persistence/`) is operationally useful as a reorganize-before-proceeding checkpoint. | _TBD at close-out_ |
| 36 | `service-bootstrap` | 6-step Service Composition Contract framing as universal floor for any service | Cab's explicit framing of the six things every service's `Program.cs` does in roughly the same order: build host + Aspire config; register Wolverine; wire store; configure observability; map endpoints; run. ai-skills' new-project skills walk through a complete Program.cs but don't extract the universal pattern. The six-step contract is project-agnostic and pedagogically useful as a scaffolding checklist. | _TBD at close-out_ |
| 37 | `service-bootstrap` | Aspire-injected configuration with explicit null-check at startup pattern | The `?? throw new InvalidOperationException("Missing connection string: <name>")` idiom at every connection-string read is Cab's fail-fast response to Aspire mis-wiring. ai-skills shows `connectionString!` (null-forgiving) which silently coerces a missing connection string into an empty string and produces opaque downstream errors. Cab's explicit null-check throws at startup with a precise message naming the missing connection string. Worth upstreaming as a defensive bootstrap pattern. | _TBD at close-out_ |
| 38 | `service-bootstrap` | Environment-aware ServiceLocationPolicy (warn dev, hard-fail prod) | `opts.ServiceLocationPolicy = builder.Environment.IsDevelopment() ? ServiceLocationPolicy.AllowedButWarn : ServiceLocationPolicy.NotAllowed` — catches lambda-factory regressions in CI/prod while keeping dev warnings non-blocking. ai-skills' `wolverine-handlers-ioc-and-service-optimization` documents `ServiceLocationPolicy` values but doesn't address the environment-awareness pattern. Generic operational hygiene worth upstreaming. | _TBD at close-out_ |
| 39 | `service-bootstrap` | Connection-string casing convention (snake_case Postgres vs PascalCase SQL Server) | Cab's per-store naming rule: Postgres connections use `crittercab_<service_name>` (snake_case lowercase identifier-friendly); SQL Server uses `CritterCab_<ServiceName>` (PascalCase per SQL Server convention). The pair-of-conventions pattern keeps multi-store solutions consistent and prevents mistakes when copying connection strings between BCs. ai-skills doesn't address store-specific casing. Worth upstreaming for projects mixing Marten + Polecat. | _TBD at close-out_ |
| 40 | `cli-jasperfx` | Five-contributors framing (JasperFx core / JasperFx.Events / Weasel / Wolverine / Marten) | Cab's explicit enumeration of which extension contributes which command. ai-skills' counterpart focuses on the Wolverine subset and doesn't break out the contributor structure. The five-contributors framing is pedagogically useful for any project using the JasperFx CLI — helps readers understand why some commands appear/disappear depending on which packages are referenced. Worth upstreaming as the introductory framing for the broader `jasperfx-cli-fundamentals` skill if/when ai-skills expands beyond Wolverine-only CLI coverage. | _TBD at close-out_ |
| 41 | `cli-jasperfx` | `db-apply` vs `resources setup` decision matrix (Weasel-specific vs everything-at-once) | Cab explicitly differentiates these two near-overlapping commands: `db-apply` is Weasel-specific (Marten + Polecat schema migrations); `resources setup` is everything-at-once (every registered `IStatefulResource.Setup()` including non-schema initialization like queue creation, topic provisioning, leader-election state). The practical rule (CI/CD pre-deploy → `db-apply`; local dev fresh start → `resources setup`) is operationally important. ai-skills covers both commands but doesn't extract the decision rule. Worth upstreaming. | _TBD at close-out_ |
| 42 | `cli-jasperfx` | `--no-launch-profile` invocation pattern with rationale | Cab's CLI invocations consistently use `dotnet run --project <path> --no-launch-profile -- <command>`. The rationale: skip `launchSettings.json` so URLs and environment variables don't apply (the CLI doesn't need them, and applied URLs can interfere with tooling). ai-skills' `wolverine-observability-command-line-diagnostics` doesn't mention this flag. Generic .NET pattern worth upstreaming as a CLI invocation hygiene rule. | _TBD at close-out_ |
| 43 | `cli-jasperfx` | CI/CD pipeline shape with exit code contract | Cab's CI/CD usage patterns section establishes the canonical pipeline shape: pre-deploy schema gate (`db-patch` for review → `db-apply` for application → `db-assert` for verification); pre-deploy code generation (`codegen write` → `dotnet build`); PR validation gates (`codegen test`, `describe-routing --all` artifact, `check-env`); operational diagnostics (`storage counts`, `storage replay --exception-type`); exit-code contract (every JasperFx command returns 0/non-zero, `set -e`-friendly). ai-skills mentions `db-assert` for CI but doesn't extract the full pipeline shape. Worth upstreaming as a CI/CD reference architecture for the JasperFx CLI. | _TBD at close-out_ |
| 44 | `observability-tracing` | Manual-AddSource as single-most-important idea framing | ai-skills' OTEL setup skill covers `AddSource("Wolverine")` and `AddSource("Marten")` registration but doesn't emphasize the silent-drop failure mode (without registration, `StartActivity()` returns null, no spans created, no error logged). Cab's framing as "the single most important idea" plus the explicit symptom description ("I see ASP.NET Core spans but no Wolverine handler or Marten database spans") is operationally critical for any team enabling OpenTelemetry on a Critter Stack project. Worth upstreaming as the primary framing of the OTEL skill. | _TBD at close-out_ |
| 45 | `observability-tracing` | Per-BC ActivitySource convention `CritterCab.{BcName}` | Cab's explicit naming convention for per-service ActivitySource (matching service name structure exactly). Generic per-service ActivitySource naming pattern that any project would benefit from. Pairs with the same convention for per-BC Meters in observability-metrics (entry #48). Worth upstreaming as a generic naming convention. | _TBD at close-out_ |
| 46 | `observability-tracing` | Trace context propagation diagram across transports | Cab's ASCII-art diagram showing how `ParentId` flows through Kafka headers / ASB ApplicationProperties / gRPC metadata across services. Pedagogically valuable; ai-skills' OTEL skill mentions correlation but doesn't visualize the cross-transport flow. Worth upstreaming as a teaching aid. | _TBD at close-out_ |
| 47 | `observability-metrics` | Wildcard `AddMeter("Wolverine:*")` registration with rationale | Wolverine's Meter is `Wolverine:{ServiceName}` (per-service-suffixed); the exact-match `AddMeter("Wolverine")` silently drops every Wolverine metric. ai-skills' `wolverine-observability-metrics-and-auditing` incorrectly uses the exact-match form (also flagged as ai-skills content drift below). The wildcard form `"Wolverine:*"` is the correct prefix-match registration per OpenTelemetry .NET SDK. Worth upstreaming as both a correction to ai-skills and a generic pattern for any per-service-suffixed Meter. | _TBD at close-out_ |
| 48 | `observability-metrics` | Per-BC custom Meter naming convention matching ActivitySource | Cab's convention: Meter name = `CritterCab.{BC}` exactly matching the ActivitySource name from `observability-tracing`. Same convention applied to two different OTEL signals keeps the dashboard organized. Pairs with entry #45. Worth upstreaming as a generic per-service Meter naming pattern. | _TBD at close-out_ |
| 49 | `observability-metrics` | Polecat 3.1 `IDocumentStoreUsageSource` monitoring discovery surface | Polecat 3.1 added a descriptor-publishing mechanism distinct from OpenTelemetry metrics: `IDocumentStore` implements `IDocumentStoreUsageSource`; consumers (CritterWatch, Wolverine `ServiceCapabilities.readDocumentStores`) call `TryCreateUsage()` to enumerate `DocumentStoreUsage` (database identity, schema, per-document `DocumentMappingDescriptor` with DDL) and `EventStoreUsage` (DCB tag types via typed `TagTypeDescriptor` list). Plus `EnableExtendedProgressionTracking` adds `heartbeat`/`agent_status`/`pause_reason`/`running_on_node` columns to `pc_event_progression` for daemon health alerting. Brand new in Polecat 3.1; ai-skills doesn't cover. Substantial fuel for a future ai-skills `polecat-monitoring-discovery` skill. | **Observed gap** (no active author) |
| 50 | `observability-metrics` | View bucket boundaries with Cab-tuned defaults for `wolverine-execution-time` | Cab's explicit Metric View configuration overriding default histogram bucket boundaries (`Boundaries = new double[] { 0.5, 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000 }`) for handler-execution latency. ai-skills doesn't address bucket-boundary tuning. Generic operational pattern for any service whose handler latency profile differs from default HTTP-latency-shaped distributions. Worth upstreaming. | _TBD at close-out_ |
| 51 | `observability-metrics` | Cardinality risk analysis with Metric View `TagKeys` filtering | Cab's analysis of `tenant.id` as the dominant cardinality concern in multi-tenant services, with three mitigation strategies (drop tag via View, bucket tenants into tiers, cap unique values via custom processor). ai-skills doesn't address cardinality concerns at all. Operationally critical for any production OTel deployment with multi-tenant usage. Worth upstreaming. | _TBD at close-out_ |
| 52 | `testing-advanced` | Namespace-per-fixture isolation pattern | Cab's canonical pattern for parallel-fixture isolation across transports and storage: RabbitMQ vhost-per-fixture, Kafka topic prefix-per-fixture, ASB queue prefix-per-fixture, PostgreSQL schema-per-fixture, SQL Server database-per-fixture. Generalizes to any namespaced infrastructure. ai-skills' `wolverine-testing-test-parallelization` covers parallelization mechanics but not the namespace-isolation pattern. Worth upstreaming as a generic parallel-test-isolation reference. | _TBD at close-out_ |
| 53 | `testing-advanced` | Multi-host `TrackActivity().AlsoTrack(host2, host3)` pattern | Wolverine 5.32+ multi-host tracking primitive: `host.TrackActivity().AlsoTrack(otherHost)` returns the same `TrackedSessionConfiguration` so chaining continues; `WaitForMessageToBeReceivedAt<T>(host)` provides per-host wait conditions to prevent premature completion under inter-host hop delays. ai-skills' integration testing skills don't cover this surface. Worth upstreaming as a multi-host integration testing pattern. | _TBD at close-out_ |

## Cab cleanup roadmap (post-Phase-5 followups)

Reconciliation surfaces things that should change but aren't part of the Phase 5 scope (which is reconciliation only — no authoring, no restructuring, no renaming). Captured here for the post-Phase-5 cleanup pass.

| # | Skill | Item | Notes |
|---|---|---|---|
| 1 | `marten-wolverine-aggregates` | Skill rename | Erik noted (Skill 10 audit) that he doesn't love the name `marten-wolverine-aggregates`. ai-skills counterpart is named `marten-aggregate-handler-workflow`. Possible alternatives: `aggregate-command-handlers`, `marten-aggregate-handlers`, `aggregate-handler-workflow`. Defer to post-Phase-5 cleanup. |
| 2 | `marten-wolverine-aggregates` | ✅ Skill 10 revisit for `[ConsistentAggregate]` / `[ConsistentAggregateHandler]` coverage — **COMPLETED 2026-05-06** | Surfaced in Skill 15 (`polecat-event-sourcing`) audit; completed in-session after Skill 16 per Erik's directive. New H2 section `[ConsistentAggregate] and [ConsistentAggregateHandler] — read-then-decide consistency` added between Multi-Stream Handlers and Stream Identity. Includes "When to use which" decision table, full Cab BC code examples for handler-level (idempotency guard via `ConfirmDriverArrival` on `RideOffer`) and parameter-level (protect-secondary-stream via `AssignDriverToTrip` writing Trip + reading Driver), plus brief mentions of authorization precondition (`ApproveDriverApplication` on `DriverApplication`) and capacity reservation (`AcceptZoneAssignment` on `DispatchZone`) patterns. Cross-store note for Polecat parallel. File 24,631 → 31,398 bytes (+6,767 bytes / +27%). Coverage gap entry #8 closed. |

## Cab content corrections during reconciliation

Reconciliation occasionally surfaces Cab content that doesn't reflect Cab's actual usage — content drift from ai-skills or generic Wolverine docs during original authoring. These are corrected in place during the per-skill pass.

| # | Skill | Section removed | Reason |
|---|---|---|---|
| 1 | `wolverine-azure-service-bus` | MassTransit and NServiceBus interop | Cab does not use MassTransit or NServiceBus and never will (per Erik). The section was likely content drift; not Cab-relevant. |

## ai-skills content drift observed

Reconciliation also surfaces drift in the *upstream* (ai-skills) library — content that has fallen out of date relative to current Marten/Wolverine/Polecat releases. Cab does not act on these directly (they're upstream concerns), but they're worth flagging to the JasperFx team for the next ai-skills revision.

| # | ai-skills skill(s) | Drift observation | Surfaced during |
|---|---|---|---|
| 1 | `marten-advanced-dynamic-consistency-boundary` + `polecat-cross-stream-operations` | Both ai-skills present DCB as Polecat-only. `marten-advanced-dynamic-consistency-boundary` claims **"DCB is currently implemented in Polecat. The `[BoundaryModel]` attribute and `EventTagQuery` are Polecat-specific."** `polecat-cross-stream-operations` reinforces this in its DCB-vs-cross-stream table with **"Availability: Polecat only"** and a reference to "the **Dynamic Consistency Boundary (Polecat)** skill." Both contradict the demonstrable presence of Marten DCB (`Marten.Events.Dcb.DcbConcurrencyException`, `opts.Events.RegisterTagType<>().ForAggregate<>()` on Marten store options, the `src/Persistence/MartenTests/Dcb/` test directory in the Wolverine repo). Both skills were likely written when DCB was Polecat-only and not updated when Marten gained the implementation. The drift appears in two separate ai-skills, suggesting it predates current Marten DCB and warrants a coordinated revision pass on ai-skills' DCB story. | Skill 14 (`dynamic-consistency-boundary`) audit; reinforced by Skill 15 (`polecat-event-sourcing`) audit |

## Cab coverage gaps revealed

ai-skills covers topics that Cab has no parallel skill for. These are documented honestly via cross-ref in the Cab `Upstream` block (per methodology refinement #4) so that readers know to load the ai-skills directly. Authoring a Cab parallel is a post-Phase-5 decision — in some cases the ai-skills coverage may be sufficient and Cab doesn't need its own skill.

| # | Topic | ai-skills counterpart | Cab status | Notes |
|---|---|---|---|---|
| 1 | Wolverine messaging resiliency policies (retry strategies, circuit breakers, dead-letter queues, compensating actions) | `wolverine-messaging-resiliency-policies` | No parallel skill | Surfaced in Skill 3 (`wolverine-messaging-handlers`); the same ai-skills counterpart is also referenced as Upstream from Skills 6, 7, 10. |
| 2 | Event enrichment for projections (`EnrichEventsAsync` to avoid N+1 queries) | `marten-projections-event-enrichment` | No parallel skill | Surfaced in Skill 11 (`marten-projections`). ai-skills coverage is dedicated; Cab may not need its own skill if the ai-skills coverage is loaded directly when this pattern is used. |
| 3 | Composite projections (staged execution, chained projections, synthetic events) | `marten-projections-composite` | No parallel skill | Surfaced in Skill 11. Same logic — ai-skills coverage dedicated, may not need Cab parallel. |
| 4 | `RaiseSideEffects` from projections (publishing messages, appending events, atomic side effects) | `marten-projections-raise-side-effects` | No parallel skill | Surfaced in Skill 11. Same logic. |
| 5 | Marten index design (computed indexes, GIN, duplicated fields, unique/partial/multi-column/full-text indexes, `IndexMethod` options, `TenancyScope`) | `marten-advanced-indexes-and-query-optimization` | No parallel skill | Surfaced in Skill 12 (`marten-querying`). **Most consequential coverage gap revealed in Phase 5** — indexes directly affect query performance, the core topic of the skill that surfaced the gap. Skill 24 (`service-bootstrap`) audit will verify whether Cab covers index registration there; if not, this is a clear post-Phase-5 authoring candidate. |
| 6 | Operational/troubleshooting daemon surface (programmatic daemon control via `StopAgentAsync`/`IProjectionCoordinator`/`AllProjectionProgress`, production tuning settings `HealthCheckPollingTime`/`CheckAssignmentPeriod`/`InboxStaleTime`/`OutboxStaleTime`, durability metrics `wolverine-inbox-count`/`wolverine-outbox-count`/`wolverine-scheduled-count`, cold-start optimization with `codegen write`, SQL diagnostics for `wolverine_node_assignments`/`wolverine_node_records`, `NodeAssignmentHealthCheckTracing` sampling) | `marten-advanced-async-daemon-deep-dive` | No parallel skill | Surfaced in Skill 13 (`marten-async-daemon`). Documented honestly in Cab `Upstream` block with explicit "Cab does not currently cover" language. Post-Phase-5 decision: author a new operational-daemon Cab skill, or accept as "defer to ai-skills directly" since the ai-skills coverage is comprehensive. |
| 7 | Polecat MCP server (`app.MapPolecatMcp()` — built-in MCP for AI agent integration with `get_event_store_configuration` / `list_known_event_types` / `list_projections` tools) | `polecat-setup-and-decision-guide` | No parallel skill | Surfaced in Skill 15 (`polecat-event-sourcing`). Operationally relevant for teams using Claude Code or similar agents against a Polecat-backed service. Documented honestly in Cab `Upstream` block; post-Phase-5 decision: author Cab coverage or accept as defer-to-ai-skills. |
| 8 | `[ConsistentAggregate]` / `[ConsistentAggregateHandler]` attribute shortcuts (read-then-decide patterns: idempotency guard, authorization precondition, capacity reservation, secondary-stream protection) | `polecat-cross-stream-operations` (and presumably the parallel Marten skill) | ✅ **Closed in Skill 10 revisit (2026-05-06)** — new H2 section `[ConsistentAggregate] and [ConsistentAggregateHandler] — read-then-decide consistency` added to `marten-wolverine-aggregates` covering both attributes with full Cab BC code examples (idempotency guard, protect-secondary-stream) and brief mentions of the other two patterns (authorization precondition, capacity reservation) | Surfaced in Skill 15 as cross-cutting gap (Marten + Polecat). Marten-side closed via Skill 10 revisit. Polecat-side considered sufficiently covered by the cross-store note in Skill 10's new section ("attribute names, semantics, and `JasperFx.ConcurrencyException` retry pattern transfer unchanged" + reference to `polecat-event-sourcing` § Wolverine integration) — no separate Skill 15 revisit needed. Cab cleanup roadmap entry #2 completed. |
| 9 | `FakeEventStream<T>` unit-test stub for handlers taking `IEventStream<T>` parameters | `polecat-cross-stream-operations` | No parallel skill | Surfaced in Skill 15. ai-skills provides a copy-paste-ready stub for unit-testing handlers without infrastructure. Useful pattern for any cross-stream handler test. |

---

## Cross-cutting passes (Pass A / Pass B / Pass C)

To be executed after all 39 per-skill reconciliations complete.

- **Pass A** — Validate every `See Also › External` ai-skills reference across the library. Promote correct entries to `Upstream (ai-skills)`; remove or correct false promises.
- **Pass B** — README cluster tables and entry-point hubs reviewed for adjustments (most likely no changes needed; topology unchanged).
- **Pass C** — Synthesize the upstream-contribution roadmap into a prioritized post-Phase-5 follow-up plan.

---

## Quantitative summary

_(updated at session end)_

- Skills reconciled: **25 / 39** (+ Skill 10 revisit completed in-session) — **Erik's queue complete: Skills 24, 25, 31, 35–39 all reconciled in this batch**
- Total lines trimmed across 25 reconciliations: ~525 cumulative; Skill 10 revisit was a pure content-addition (~95 lines added); Skills 17, 25, 24, 31, 35-39 were predominantly reorganization + cross-ref additions (net byte growth typical of design-and-conventions and partial-overlap shapes)
- Direct-equivalent (deduplicated): **18** (Skills 1-3, 6, 7, 9-16, 24, 25, 31, 35, 36, 38)
- No equivalent — sub-category breakdown: **7 total**
  - In-scope, unauthored, Active priority: 2 (Skills 4, 5 gRPC under Erik's active roadmap)
  - In-scope, unauthored, Observed gap priority: 2 (Skill 8 sagas; Skill 16 polecat-document-store)
  - Out-of-scope topic for ai-skills: 3 (Skill 17 csharp-coding-standards; Skill 37 testing-fundamentals; Skill 39 partial — advanced patterns largely Cab-specific without direct counterparts)
- Upstream-contribution candidates: **53** total (+ 18 since last summary: #36-43 from Skill 24 + #31 cli-jasperfx; #44-53 from Tier 4 — OTEL framing, per-BC conventions, Polecat 3.1 monitoring as Observed gap, view buckets, cardinality, multi-host testing)
- Upstream-replacement candidates: 0
- Cab coverage gaps revealed: **17 total** (1 closed in Skill 10 revisit; **16 remain open**)
  - From Skills 24, 31: gaps re: greenfield Marten options, durability optimizations, AutoApplyTransactions policy, index registration, WolverineFx.Http.Marten metapackage, `describe-resiliency` command, programmatic CLI APIs, disabling-persistence-for-codegen-runs
  - From Tier 4: gaps re: Marten async daemon spans, `InvokeTracingMode.Full`, `[WolverineLogging(telemetryEnabled: false)]` attribute, audit middleware logging pattern, audit-events-to-Marten via After hook, Prometheus exporter setup
- Cab content corrections: 1 (no change)
- ai-skills content drift observed: **2 tracker entries** (DCB-Polecat-only drift from Skill 14/15; AddMeter exact-match-vs-wildcard inconsistency between `wolverine-observability-metrics-and-auditing` and `wolverine-observability-opentelemetry-setup` from Skill 36)
- Forward-looking placeholders removed: **15** (Skills 4, 5, 8, 9, 11, 12, 13, 14, 15, 16; Skill 17 had none; Skills 35-39 — 5 placeholders removed in this batch: `opentelemetry-dotnet`, `aspire-observability`, `observability-metrics`, `critter-stack-testing-fundamentals`, `critter-stack-integration-testing`, `testing-advanced`)
- Cab cleanup items deferred to post-Phase-5: 1 (Skill 10 rename remains)
- Methodology refinements: 10 documented (refinement #10 added: out-of-scope topic sub-category)
- Tier 1 complete (14 / 14); Tier 2 complete (2 / 2); Skill 10 revisit complete; **Tier 3 partial (4 / 18)** — Skills 17, 24, 25, 31 done; **Tier 4 complete (5 / 5)** — Skills 35-39 done. **Phase 5 substantive deliverable complete: every counterpart-rich skill across all four tiers has been reconciled. Remaining 14 Tier 3 skills (18-23, 26-30, 32-34) are the predicted out-of-scope cluster — separate decision whether to include them.**

---

## Phase 5 close-out (2026-05-06)

Phase 5 closed at the substantive deliverable. The decision to stop at 25/39 reconciled skills (Path A in the close-out discussion) reflects the reconciliation-value distribution: every skill with meaningful ai-skills overlap has been reconciled, and the remaining 14 skills are the predicted out-of-scope cluster where the value of further per-skill passes is low. The cumulative work captured in this retrospective — 53 upstream-contribution candidates, 16 open Cab coverage gaps, 2 ai-skills content drift entries, 10 methodology refinements, 15 forward-looking placeholders removed — is the substantive Phase 5 output.

### Final reconciliation status

| Tier | Status | Count |
|---|---|---|
| Tier 1 (Wolverine + Marten core) | ✅ Complete | 14 / 14 |
| Tier 2 (Polecat) | ✅ Complete | 2 / 2 |
| Skill 10 revisit (`[ConsistentAggregate]` content addition) | ✅ Complete | 1 |
| Tier 3 (Cab-specific patterns) — substantive | ✅ Done | 4 / 18 (Skills 17, 24, 25, 31) |
| Tier 3 (Cab-specific patterns) — deferred | 🔄 Phase 6 follow-up | 14 / 18 (Skills 18-23, 26-30, 32-34) |
| Tier 4 (Observability + Testing) | ✅ Complete | 5 / 5 |
| **Substantive total** | ✅ **Complete** | **25 / 39** |

### Phase 6 follow-up scope — 14 unreconciled skills

The 14 remaining skills are characterized below by their predicted reconciliation shape. The 3 "substantive" entries are the only ones expected to surface meaningful audit findings; the 11 "placeholder cleanup" entries are predicted to produce zero or near-zero upstream-contribution candidates and zero coverage gaps.

#### Substantive Phase 6 follow-ups (3)

1. **Skill 21 — `transport-selection`**: ai-skills has individual integration skills (`wolverine-integrations-kafka`/`-azure-service-bus`/`-rabbitmq`/etc.) but no decision-matrix skill for choosing between transports. **Likely Observed-gap entire-skill-creation candidate** — Cab may be filling a real ai-skills gap. Worth a closer look.

2. **Skill 23 — `adding-a-service`**: Different scope from Skill 24 (service-bootstrap). Cab's `adding-a-service` is *how to add a new BC project to a multi-service repo* (csproj template, database provisioning, Aspire registration, paired test project), while ai-skills' new-project skills cover *greenfield from scratch*. Will reference the same `critterstack-arch-new-project-*` family as Skill 24 but for project-skeleton concerns.

3. **Skill 29 — `aspire`**: Aspire shows up scattered across multiple ai-skills (`wolverine-testing-with-aspire`, `critterstack-arch-new-project-wolverine-marten` Aspire injection, `wolverine-observability-opentelemetry-setup` Aspire dashboard) but **no dedicated AppHost skill exists**. Cab's Aspire skill probably has Cab-specific orchestration patterns + AppHost composition. **Possibly Observed-gap candidate** for a future ai-skills `critterstack-arch-aspire-fundamentals`.

#### Placeholder-cleanup Phase 6 follow-ups (11)

These are predicted ~5-minute Skill-17-style passes: remove forward-looking placeholders, possibly add a tangential cross-ref, restructure See Also if applicable. Each will produce zero or near-zero upstream-contribution candidates.

- Skill 18 — `domain-event-conventions` (project-specific vocabulary; possible cross-ref to `marten-projections-event-enrichment`)
- Skill 19 — `event-modeling` (Adam Dymitruk methodology; ai-skills has nothing on workshop methodology)
- Skill 20 — `protobuf-contracts` (proto-first conventions; might fold into Erik's future `wolverine-grpc` skill, entries #5 candidate)
- Skill 22 — `grpc-vs-other-transports` (likely folds into Erik's future gRPC skill)
- Skill 26 — `distributed-saga-considerations` (multi-saga coordination; same ai-skills landscape as Skill 8 — entry #9 covers saga territory broadly)
- Skill 27 — `identity-acl` (OpenIddict-shaped JWT ACL; ai-skills has nothing on identity)
- Skill 28 — `polyglot-go-service` (ai-skills is .NET-only)
- Skill 30 — `cli-aspire` (Microsoft's Aspire CLI; not Critter Stack)
- Skill 32 — `cli-grpc-tooling` (`protoc`, `grpcurl`, `evans` — generic gRPC ecosystem)
- Skill 33 — `cli-kafka-tooling` (Confluent CLI, `kafkactl` — generic Kafka)
- Skill 34 — `cli-azure-messaging` (Azure CLI for Service Bus / Event Hubs)

### Cross-cutting passes status

The Phase 5 plan included three cross-cutting passes (Pass A: validate every See Also › External ai-skills reference; Pass B: README cluster tables and entry-point hubs review; Pass C: synthesize upstream-contribution roadmap). Their close-out status:

| Pass | Status |
|---|---|
| Pass A — See Also External ai-skills validation | ✅ **Done implicitly** through per-skill reconciliation. Every reconciled skill's Upstream block was structured per the three-block convention with proper ai-skills cross-references. Forward-looking placeholders for non-existent ai-skills (15 total) were systematically removed. |
| Pass B — README cluster tables review | 🔄 **Deferred to Phase 6.** README has been updated with a Phase 5 close-out status note, but the cluster tables and entry-point hubs haven't been audited against the reconciled See Also structure. Low-priority cleanup. |
| Pass C — Upstream-contribution roadmap synthesis | ✅ **Done implicitly** through per-skill candidate flagging. The 53-entry roadmap captures every candidate with priority sub-categorization (Active / Observed gap / TBD). Synthesizing into a prioritized post-Phase-5 follow-up plan happens at the moment of the next JasperFx core team conversation — the roadmap is ready. |

### Top-priority Phase 6 follow-ups (post-Phase-5)

Ordered by impact to the upstream contribution conversation with the JasperFx core team:

1. **ai-skills content drift correction** (entry #36 territory — the `AddMeter("Wolverine:*")` wildcard fix to `wolverine-observability-metrics-and-auditing`). Operationally critical; would silently drop every Wolverine metric in projects following the example as written. Worth surfacing first.
2. **Active gRPC skills** (entries #4, #5 — `wolverine-grpc-handlers` + `wolverine-grpc-bidirectional-handlers`). Erik is the planned author; Cab provides the source material.
3. **Observed-gap entire-skill-creation candidates**:
   - `wolverine-sagas` (entry #9) — ai-skills saga gap
   - `polecat-document-store` (entry #32) — ai-skills document-database gap
   - `polecat-monitoring-discovery` (entry #49) — Polecat 3.1 `IDocumentStoreUsageSource` discovery surface
   - Possibly `transport-selection` (Phase 6 Skill 21 audit) — ai-skills transport decision-matrix gap
   - Possibly `critterstack-arch-aspire-fundamentals` (Phase 6 Skill 29 audit) — ai-skills dedicated Aspire skill gap
4. **High-value generic patterns** worth folding into existing ai-skills (subset of the 32 footgun-style entries):
   - Manual-AddSource as single-most-important idea framing (entry #44)
   - Wildcard `AddMeter("Wolverine:*")` registration with rationale (entry #47, paired with #36 correction)
   - 6-step Service Composition Contract framing (entry #36)
   - Aspire-injected configuration with explicit null-check pattern (entry #37)
   - 4 daemon footguns from Skills 13/14 (April 2026 daemon improvements, position strategies, etc.)
   - DCB footgun surface from Skill 14 (the highest-density single-skill candidate count, 7 entries)

### Cab cleanup roadmap remaining

- **Skill 10 (`marten-wolverine-aggregates`) rename** — to `marten-aggregates-handlers` or similar to clarify the Marten-specific handler shape vs. the more generic naming. Deferred to Phase 6 cleanup. The Skill 10 revisit completed in Phase 5 closed the `[ConsistentAggregate]` coverage gap; the rename is the remaining cleanup item.

### Phase 5 deliverable summary

What Phase 5 produced:

- **25 reconciled skills** with proper three-block See Also convention (Upstream / Prerequisites / Sibling skills / Downstream / External) and ai-skills cross-references where counterparts exist.
- **53 upstream-contribution candidates** captured with priority sub-categorization and detailed rationale, ready for the next JasperFx core team conversation.
- **16 open Cab coverage gaps** documented for future Cab content additions (when ai-skills surfaces aren't already cross-referenced from the gap location).
- **2 ai-skills content drift entries** surfaced for JasperFx team awareness (DCB-Polecat-only framing in `marten-advanced-dynamic-consistency-boundary` + `polecat-cross-stream-operations`; `AddMeter` exact-match-vs-wildcard inconsistency between `wolverine-observability-metrics-and-auditing` and `wolverine-observability-opentelemetry-setup`).
- **15 forward-looking placeholders removed** from Cab skill See Also sections (references to non-existent ai-skills that pre-dated the reconciliation pass).
- **10 methodology refinements** documented in this retrospective covering reconciliation patterns surfaced during the work — directly applicable to Phase 6 and any future reconciliation passes against external skill libraries.
- **3 entire-skill-creation candidates** identified for Cab as upstream contributions (gRPC pair via Erik's active roadmap, sagas as Observed gap, polecat-document-store as Observed gap, plus polecat-monitoring-discovery surfaced in Tier 4).
- **README updated** with Phase 5 close-out status; full reconciliation record preserved in this document for the post-Phase-5 conversation surface.

**Phase 5 substantive deliverable complete. Closed 2026-05-06.**
