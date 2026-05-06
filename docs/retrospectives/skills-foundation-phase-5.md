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
| 7 | `wolverine-azure-service-bus` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 8 | `wolverine-sagas` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 9 | `marten-aggregates` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 10 | `marten-wolverine-aggregates` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 11 | `marten-projections` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 12 | `marten-querying` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 13 | `marten-async-daemon` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 14 | `dynamic-consistency-boundary` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 15 | `polecat-event-sourcing` | 2 | _pending_ | _pending_ | _pending_ | _pending_ |
| 16 | `polecat-document-store` | 2 | _pending_ | _pending_ | _pending_ | _pending_ |
| 17 | `csharp-coding-standards` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 18 | `domain-event-conventions` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 19 | `event-modeling` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 20 | `protobuf-contracts` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 21 | `transport-selection` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 22 | `grpc-vs-other-transports` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 23 | `adding-a-service` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 24 | `service-bootstrap` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 25 | `vertical-slice-organization` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 26 | `distributed-saga-considerations` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 27 | `identity-acl` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 28 | `polyglot-go-service` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 29 | `aspire` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 30 | `cli-aspire` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 31 | `cli-jasperfx` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 32 | `cli-grpc-tooling` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 33 | `cli-kafka-tooling` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 34 | `cli-azure-messaging` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 35 | `observability-tracing` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |
| 36 | `observability-metrics` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |
| 37 | `testing-fundamentals` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |
| 38 | `testing-integration` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |
| 39 | `testing-advanced` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |

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

## Methodology refinements emerging in Phase 5

_(updated as the reconciliation progresses)_

1. **`See Also` three-block convention** (from Skill 1, refined after review). `Upstream` (ai-skills) → `Prerequisites` (Cab-internal) → `Sibling skills` → `Downstream` → `External`. Adopted as the Phase 5 standard; subsequent skills follow it. The rename `Upstream` → `Prerequisites` happens uniformly across every Phase 5-reconciled skill.
2. **License framing in `Upstream` lead-in.** Each skill's `Upstream` block lead-in mentions "ai-skills (license required, install via `npx skills add`)" once. ai-skills content is never inlined into Cab skills — only skill names are referenced. This honors the proprietary/licensed status of ai-skills.
3. **Trim estimates are systematically high** (from Skill 3). Naive line-counting of removed code blocks overestimates net trim because cross-reference prose and expanded `Upstream` blocks (with 3 detailed entries replacing 3 one-liners) consume most of the savings. Skill 3 estimated ~50 lines saved; actual was ~26. Future estimates should account for the prose-and-upstream-block offset — a useful rule of thumb is `actual_trim ≈ 0.5 × raw_lines_removed`.
4. **Cross-referencing ai-skills counterparts without a Cab parallel** (from Skill 3). When ai-skills covers a topic that Cab doesn't have a dedicated skill for (e.g., `wolverine-messaging-resiliency-policies`), the Cab `Upstream` block can include the ai-skills entry with a brief note that no Cab parallel exists. This is honest about Cab's current coverage gaps and points the reader to the authoritative upstream.
5. **Handling "No equivalent in ai-skills" cases** (from Skill 4). When ai-skills has no counterpart today, the reconciliation pass is a light rename-only: `Upstream` → `Prerequisites`, remove any forward-looking placeholders for not-yet-published ai-skills counterparts (they mislead readers into searching for nonexistent skills), and skip the new `Upstream` block entirely. The Cab skill remains the authoritative reference until ai-skills publishes a parallel. If an ai-skills counterpart is actively planned (with a known author), record it in the upstream-contribution roadmap with an "Active" priority rather than "_TBD_".
6. **Trim ratio depends on duplication shape** (refined from Skill 6). The 50% rule of thumb (refinement #3) applies when duplication is mostly **prose** — cross-reference paragraphs and expanded `Upstream` blocks consume most of the savings. When duplication is mostly **code blocks** (Skill 6's case), trims remove dense short lines that don't get fully replaced by prose; actual trim approaches `~1.0 × raw_lines_removed`. Skill 6 estimated ~120 lines, actual was ~125 — nearly 1:1. Future estimates should consider whether the target sections are code-heavy (closer to 1:1) or prose-heavy (closer to 0.5:1).
7. _(more entries to come)_

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

## Cab coverage gaps revealed

Reconciliation surfaces topics where ai-skills has dedicated coverage but Cab doesn't. These are candidates for future Cab skills (post-Phase 5) but were deliberately not added during Phase 5 reconciliation, which is reconciliation-only.

| # | Topic | ai-skills counterpart | Cab status | Priority |
|---|---|---|---|---|
| 1 | Messaging resiliency (retry, circuit breaker, DLQ, compensating actions) | `wolverine-messaging-resiliency-policies` | No Cab parallel; `wolverine-messaging-handlers` references the ai-skills counterpart in `Upstream` | Future (per Erik, not a Phase 5 priority) |

---

## Cross-cutting passes (Pass A / Pass B / Pass C)

To be executed after all 39 per-skill reconciliations complete.

- **Pass A** — Validate every `See Also › External` ai-skills reference across the library. Promote correct entries to `Upstream (ai-skills)`; remove or correct false promises.
- **Pass B** — README cluster tables and entry-point hubs reviewed for adjustments (most likely no changes needed; topology unchanged).
- **Pass C** — Synthesize the upstream-contribution roadmap into a prioritized post-Phase-5 follow-up plan.

---

## Quantitative summary

_(updated at session end)_

- Skills reconciled: 6 / 39
- Total lines trimmed: ~235
- Direct-equivalent (deduplicated): 4
- No equivalent: 2 (both gRPC skills, ahead of ai-skills, scoped under Erik's active upstream roadmap)
- Upstream-contribution candidates: 7 (6 footgun-style additions + 1 entire-skill-creation covering both gRPC skills, the latter actively planned by Erik)
- Upstream-replacement candidates: 0
- Cab coverage gaps revealed: 1 (messaging resiliency)
