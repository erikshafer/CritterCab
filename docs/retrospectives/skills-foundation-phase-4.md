# Retrospective — CritterCab Skill Library, Phase 4

## Metadata

- **Triggering prompt:** [`docs/prompts/skills-foundation-phase-4-handoff.md`](../prompts/skills-foundation-phase-4-handoff.md)
- **Status:** Complete
- **Date authored:** 2026-05-06
- **Output artifacts:**
  - 9 new skill files under `docs/skills/`
  - In-concert update to `docs/skills/observability-tracing/SKILL.md` (`AddMeter("Wolverine:*")` wildcard form)
  - Phase 4 close-out edits to `docs/skills/README.md` (status table, cluster index, entry-point hubs, Phase 4 Mermaid graph, Phase 5 forward-looking note)
  - Phase 5 handoff prompt at `docs/prompts/skills-foundation-phase-5-handoff.md`
- **Outcome:** All 9 planned Phase 4 skills authored, source-verified, and integrated into the navigation hub. Phase 5 has a clear handoff. Project skill count: 39.

---

## Framing

Phase 4 was the "complexity arrives" phase: sagas, Polecat as an alternative event store, the polyglot Go service, bidirectional gRPC, complete observability, and advanced testing. By design the largest authoring phase of the foundation plan. The triggering prompt allocated a recommended order, target line counts per skill, and an explicit pause-after-each-skill discipline.

The retrospective records what was authored, what methodology refinements emerged mid-session, what was harder than expected, and what Phase 5 inherits.

---

## Outcome summary

| # | Skill | Cluster | Lines | Notes |
|---|---|---|---|---|
| 1 | `wolverine-grpc-bidirectional-handlers` | `wolverine` | 270 | Handler-once-per-request invariant pinned via source verification |
| 2 | `grpc-vs-other-transports` | `grpc` | 249 | Four-axis decision framework |
| 3 | `wolverine-sagas` | `wolverine` | 461 | Saga-ID resolution six-rule cascade verified; `[ProcessManagerIdentity]` framing reflects pending JasperFx work |
| 4 | `distributed-saga-considerations` | `distributed-services` | 278 | Pattern companion to `wolverine-sagas` |
| 5 | `polecat-event-sourcing` | `polecat` | 690 | v3.1-aware after mid-session bump |
| 6 | `polecat-document-store` | `polecat` | 526 | v3.1-aware after mid-session bump |
| 7 | `polyglot-go-service` | `polyglot` | 396 | Aspire `AddContainer` default; `AddExecutable` documented as escape hatch |
| 8 | `observability-metrics` | `observability` | 458 | Pre-existed at session start; reconciled and patched rather than re-authored |
| 9 | `testing-advanced` | `testing` | 650 | Largest skill; trimmed across three rounds (855 → 787 → 697 → 650) |

Total Phase 4 skill content: 3,978 lines. README close-out additions: ~78 lines. Total Phase 4 deliverable: ~4,050 lines of documentation.

---

## What worked

- **The forward-reference scan as the first step per skill.** Every skill started with a PowerShell pass against all existing `SKILL.md` files for references to the about-to-author skill name. The scan catalogs every commitment the skill must honor before the first word is written. Across Phase 4 this surfaced 24 forward-references for `testing-advanced` alone, every one of which was honored in the final draft.
- **Source verification before authoring, not after.** Every claim about Wolverine, Marten, or Polecat behavior in a skill was verified against the actual source repos at `C:\Code\JasperFx\` before the claim landed in the skill body. This caught (a) misremembered API names (e.g., `MessagesFailed = "wolverine-execution-failure"` not `"wolverine-messages-failed"`), (b) version-specific behavior changes (the Polecat v3.0 → v3.1 deltas), and (c) the difference between handler-once-per-request and handler-once-per-stream in bidirectional gRPC.
- **Pause-after-each-skill discipline.** No auto-cascade. Every skill ended with a handoff summary and explicit waiting for the user's greenlight before the next began. This kept review tight and prevented mid-session drift across nine skills.
- **Reading 1–2 nearby skills before authoring a new one.** Style calibration via reading existing Phase 3 skills (and earlier Phase 4 ones once landed) kept tone, structure, and section conventions consistent without explicit re-derivation.
- **Audit checklist applied uniformly.** Every authored skill went through: line count check, claimed-API verification, forward-reference resolution check, cross-skill reference validity check. The audit caught issues before they shipped (notably the `irrationality-tracking` typo in observability-metrics tags and the stale "this skill patches observability-tracing" framing).

---

## What was harder than expected

### The pre-existing `observability-metrics` skill

When the methodology reached the `observability-metrics` slot, a complete 458-line skill already existed on disk, modified earlier the same day. It had not been authored in the active session and was not recorded in the working transcript. The skill turned out to be high-quality and source-grounded, requiring only two cleanup edits (a tag typo and softening of the "this skill patches observability-tracing" framing, since the patch had already been applied to the upstream skill in concert).

The lesson: **the methodology must check whether a skill already exists before authoring.** This wasn't in the original Phase 4 prompt's per-skill steps. It was added implicitly after the `observability-metrics` discovery (and made explicit at the start of the `testing-advanced` slot via `Filesystem:get_file_info` before any other action). The Phase 5 prompt encodes this as an explicit step in the working pattern.

### `testing-advanced` line budget overrun

The first draft of `testing-advanced` came in at 855 lines against a 500–600 line target — 43% over the upper end. The skill genuinely covers 11 distinct advanced patterns (multi-host, gRPC streaming, dynamic-DB-per-fixture, vhost isolation, Testcontainers, test-token factories, saga timeouts, OTel verification, polyglot, failure injection, plus pitfalls), so the topic count was correct, but per-section code blocks were bloated.

Three trim rounds brought it to 650 lines (24% reduction). The trims targeted: redundant fixture class boilerplate (collapsed to prose where the structure was xUnit-standard), duplicate Marten-vs-Polecat fixture examples (collapsed Polecat to "structurally identical to Marten above"), and full code wrappers around test methods (kept the test method, dropped the wrapping fixture class).

650 is still 8% over the 600 upper end. Defensible: the skill is the largest in Phase 4 by design, every code block remaining is load-bearing, and further trimming would touch actual content not boilerplate.

The lesson: **for large skills, plan the line budget per section before writing.** If the target is 500–600 lines and the topic count is 11, that's ~50 lines per section average — and code examples need to fit within that. Drafting in one shot then trimming is correct methodology, but starting with an explicit per-section budget would shorten the trim cycle.

### Mid-session Polecat v3.0 → v3.1 version bump

Partway through authoring `polecat-event-sourcing` and `polecat-document-store`, the Polecat repo at `C:\Code\JasperFx\polecat` advanced from v3.0 to v3.1. The v3.1 release was additive (no breaking changes) but introduced a substantive new feature: `IDocumentStore` now implements `IDocumentStoreUsageSource` (from `JasperFx.Events`), with the `AddPolecat` registration auto-bridging the discovery surface for monitoring tools (CritterWatch, Wolverine `ServiceCapabilities.readDocumentStores`).

This required a patch pass on both Polecat skills (frontmatter version bumps, body additions for the v3.1 monitoring discovery story) and a forward-reference commitment to `observability-metrics`, where the discovery story properly belongs (the actual observability surface vs. the schema/configuration surface in the Polecat skills).

The lesson: **reference repos are live and can change mid-phase.** The methodology should surface git revision info at the start of any session that depends on third-party source verification, so version-related discoveries are framed against a known baseline rather than against silent assumption.

### `wolverine-sagas` framing under pending JasperFx work

The `wolverine-sagas` skill landed during a period when JasperFx was actively considering a first-class `ProcessManager<TState>` framework type for Wolverine 6 / Marten 9 (the comparison document I produced for the JasperFx core team is in active discussion). The skill had to articulate Cab's saga conventions in a way that reads correctly today (using `Saga<TState>` with current Wolverine 5.32+ APIs) without prematurely committing to terminology that might shift if `ProcessManager<TState>` lands.

The resolution was a "current API" framing that names what's there now and notes the terminology is settled at the framework level even if the framework's saga-vs-process-manager story evolves. The Phase 5 reconciliation will revisit this if ai-skills has framing decisions on the same axis.

---

## Methodology refinements that emerged

These are added to the Phase 5 prompt explicitly so they don't have to be re-discovered:

1. **Check if the skill already exists** as the second step of the per-skill workflow (after the forward-reference scan). Use `Filesystem:get_file_info` to confirm absence. The pre-existing `observability-metrics` was the trigger.
2. **Explicit pause-and-report between skills.** No auto-cascade across the nine-skill arc. Phase 4 maintained this discipline cleanly; Phase 5 should too even though per-skill work is shorter.
3. **Per-section line budgets for large skills.** Phase 4 hit this only on `testing-advanced` and at first draft, not at planning time. Future authoring of similarly-broad skills should set a per-section target before drafting.
4. **Source-revision baseline at session start.** The Polecat v3.1 discovery showed that reference repos can advance mid-phase. Logging the HEAD revision of each reference repo at session start gives the next session a clear "what version was this verified against."
5. **Forward-reference validation as a deliverable check.** Each skill's audit included verifying that every promise made in upstream skills was honored. Phase 4 had 24+ such promises pointing at `testing-advanced` alone; tracking these formally (rather than relying on memory) made the audit reliable.

---

## Per-skill highlights

Brief notes on substantive decisions per skill. Detailed source-verification entries are in the working transcript.

### `wolverine-grpc-bidirectional-handlers`

- Pinned: handler invoked **once per inbound request**, not once per stream (verified in `GrpcServiceChain.cs § ForwardBidiStreamToMessageBusFrame`).
- Pinned: `[Validate]`, `[WolverineBefore]`, `[WolverineAfter]` are NOT woven into bidi handlers (explicit skip in source).
- Documented two workaround patterns for client-streaming (Pattern A: split-proto-service recommended; Pattern B: fully hand-written single service).

### `grpc-vs-other-transports`

- Four axes: streaming shape, latency budget, durability requirement, fan-out shape.
- Explicit endpoint mode coverage: `Inline | BufferedInMemory` only.

### `wolverine-sagas`

- Saga-ID resolution six-rule cascade (originally drafted as "five rules" — corrected via source check).
- `IRevisioned` from `Marten.Metadata` throwing `JasperFx.ConcurrencyException` for optimistic concurrency.
- Cab use cases named: `DispatchOfferTimeoutSaga`, `TripCompletionSaga`, `RiderOnboardingSaga`, `TripArrivalSaga`.

### `distributed-saga-considerations`

- Pattern companion. Garcia-Molina/Salem 1987 paper cited at canonical ACM DOI.

### `polecat-event-sourcing`

- The big v3.1 patch surface. Documents `PolecatOps`, DCB write-side methods, `Snapshot<T>` as `SingleStreamProjection<T, TId>` registration shortcut, the `Saga.Version` framework-managed convention (NOT `IRevisioned`), and the v3.1 `IDocumentStoreUsageSource` discovery story.

### `polecat-document-store`

- Sessions, identity strategies (Guid / int / long / string / strong-typed wrappers), LINQ via `session.Query<T>()` compiled to T-SQL JSON functions, soft deletes (3 opt-ins), 9 patching operations, concurrency via `IVersioned` (Guid) or `IRevisioned` (int) auto-detected and mutually exclusive.

### `polyglot-go-service`

- Decided: geospatial matchmaking sidecar for Dispatch BC, subscribing to Telemetry Kafka topic.
- Aspire integration default: `AddContainer` with `WithDockerfile`. `AddExecutable` documented as escape hatch.
- OTLP propagation via `otelgrpc.NewServerHandler()` for cross-language span continuity.

### `observability-metrics`

- Pre-existing. Two cleanup edits applied (tag typo `irrationality-tracking` → `progression-tracking`; softened "this skill patches observability-tracing" framing since the patch had already been applied to the upstream).
- All claims verified against `MetricsConstants.cs`, `WolverineRuntime.cs` L70, `PersistenceMetrics.cs` L21–44, `Marten\Services\OpenTelemetryOptions.cs` L60–62, `Polecat\Internal\OpenTelemetry\OpenTelemetryOptions.cs` L33, `Polecat\StoreOptions.cs` L315–317.

### `testing-advanced`

- The terminal-node skill — every Phase 4 implementation skill funnels into it.
- Wolverine.Tracking primitives verified: `host.TrackActivity().AlsoTrack(host2, host3)`, `WaitForMessageToBeReceivedAt<T>(IHost host)`, `IncludeExternalTransports()`, `DoNotAssertOnExceptionsDetected()`, `PlayScheduledMessagesAsync(TimeSpan)`, `AddStage(...)`.
- Three trim rounds (855 → 787 → 697 → 650).

---

## README close-out

Phase 4 ended with phase-boundary maintenance on `docs/skills/README.md`:

- Status table: Phase 4 → **Complete (9 skills)**.
- Cluster tables: 7 entries moved Planned → Authored (`wolverine`, `polecat`, `distributed-services`, `grpc`, `polyglot`, `testing`, `observability`). All Planned columns now `—`.
- Entry-point hubs: 5 new rows + 3 new sections (Sagas and orchestration, Polecat event-sourced and document work, Polyglot services). Existing rows updated to remove "(Phase 4)" forward-reference markers and add concrete downstream skills.
- Phase 4 Mermaid graph in violet (`#dda0dd`) parallel to the phase1/phase2/phase3 color scheme. 11 anchor nodes from earlier phases + 9 Phase 4 nodes + 23 edges showing dependencies and the testing-advanced terminal-node pattern.
- Phase 4 retrospective paragraph after the graph; Phase 5 forward-looking paragraph before the ai-skills companion section.
- Phase 3 retrospective sentence past-tensed.

---

## Outstanding items / Phase 5 inputs

Items the Phase 5 reconciliation pass inherits or should consider:

- **`wolverine-sagas` and `ProcessManager<TState>`.** If JasperFx ships a `ProcessManager<TState>` type in Wolverine 6 / Marten 9, the saga skill's framing will need a follow-up update. Phase 5's reconciliation against ai-skills may surface ai-skills' position on this terminology.
- **Polecat skills ahead of ai-skills coverage.** Polecat is newer than Marten/Wolverine; ai-skills may not yet have parallel coverage. If absent, both Polecat skills are upstream-contribution candidates.
- **`testing-advanced` length.** At 650 lines (8% over the 600 upper end), the skill is dense but defensible. Phase 5 reconciliation may identify generic test-mechanic sections that could thin further if ai-skills covers them.
- **Forward-reference language.** Several Phase 4 skills carry "(Phase 4)" or "(Phase 5)" inline phase markers in their See Also blocks. Once Phase 5 lands, the "(Phase 4)" markers can be removed; the "(Phase 5)" markers may need updating depending on Phase 5's outcomes.
- **`Directory.Packages.props` prerequisites.** Multiple Phase 4 skills flagged unwritten package additions (Wolverine.Polecat, OpenIddict for identity, OpenTelemetry.Exporter.InMemory, Testcontainers.Kafka, Testcontainers.ServiceBus, polyglot Go-side dependencies). These are surfaced in skill bodies but not yet committed in the actual `Directory.Packages.props` file. The first implementation slice that lands a Phase 4 capability will need to commit them.
- **Source-revision baselines not recorded.** The Polecat v3.0 → v3.1 discovery suggests Phase 4 should have logged HEAD revisions of each JasperFx repo at session start. It didn't. Phase 5 prompt's pre-flight checks include this.

---

## Quantitative summary

- **Phase 4 skills authored:** 9 (including the pre-existing `observability-metrics` reconciled in place).
- **Total skill-file lines:** 3,978 across the 9 Phase 4 skills.
- **Concurrent updates:** 1 (in-concert update to `observability-tracing` for the `Wolverine:*` wildcard form).
- **README phase-boundary additions:** ~78 lines.
- **Phase 5 prompt:** 294 lines, drafted at session end as the closing artifact.
- **Trim rounds on `testing-advanced`:** 3 (saved 205 lines, 24% reduction).
- **Mid-session reference-repo version bumps:** 1 (Polecat v3.0 → v3.1).
- **Pre-existing skills discovered:** 1 (`observability-metrics`).
- **Skills source-verified against `C:\Code\JasperFx\`:** 9 of 9.
- **Forward-reference promises honored:** all (audited per skill at completion).

CritterCab skill library at end of Phase 4: **39 skills** across Phases 1–4. Phase 5 (reconciliation against ai-skills) is the remaining work in the published plan.
