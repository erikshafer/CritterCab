# Skill-File Debt

Rolling list of skill-file gaps surfaced during sessions but not yet fixed. Drained by dedicated `tidy: skills` PRs; rows are **removed** (not crossed out) when the corresponding skill is updated. The retros remain the durable record of how each gap was found.

This file is the working ledger between retros that surface gaps and the tidy sessions that fix them. Per [`docs/prompts/README.md`](../prompts/README.md#session-and-pr-cadence)'s **Session and PR cadence**, gaps are fixed in-session only when they block the session-runner; gaps merely *surfaced* by a session land here and are drained on a schedule.

---

## Conventions

- **Each row names the skill, the gap, and the retro source.** The retro is the durable evidence of how the gap was found, in case the fix needs to be re-grounded later.
- **Group rows by skill file.** A tidy session can then plan one PR per affected skill rather than touching everything at once.
- **Add rows in the same PR that adds the surfacing retro.** Authoring a retro that names skill-file gaps without registering them here is a workflow gap — the debt evaporates between sessions otherwise.
- **Remove rows when fixed.** This file is not a changelog. Commits and retros already record what changed and why.
- **A row's existence is not a commitment to fix it next.** Tidy sessions choose what to drain based on cluster, blast radius, and which upcoming sessions the fix would unblock.
- **Tidy sessions verify each row against current state before fixing** (source-of-truth precedence). Precedence: working code → retro evidence → external docs. The skill body itself is what's being corrected and cannot be its own reference. The retro is evidence the gap once existed, not proof it still does. (Lifted from the first skill-tidy retrospective: one of four `marten-projections` rows turned out to be already-superseded by the time the tidy ran.)
- **No opportunistic edits to other files during a tidy.** A tidy session's scope is the skills listed for fixing in this file plus the prompt + retro files actively being authored. Other files require their own session. See [`docs/prompts/README.md` § Scope: no opportunistic edits to other files](../prompts/README.md#scope-no-opportunistic-edits-to-other-files) for the general rule and rationale.

---

## Open debt

### config-as-events bootstrap-seed pattern (new skill or `marten-wolverine-aggregates` extension)

- **Gap:** No skill codifies the config-as-events seed — the Marten `IInitialData` idempotent guard (`FetchStreamStateAsync` → `StartStream<T>` only if empty) plus the `operatorId = "system-bootstrap"` / `reason = "Initial deployment defaults"` payload, and the full-replacement singleton-stream shape. [ADR-011](../decisions/011-configuration-as-events-bootstrap.md) (§ Consequences, final ¶) explicitly deferred codifying this "until the first migration is written during implementation." That reference impl now exists: `src/CritterCab.Telemetry/TelemetryPolicy/{TelemetryPolicyBootstrap,TelemetryPolicyStream,TelemetryPolicy,ConfigureTelemetryPolicy}.cs`.
- **Two design questions the reference impl raised are now RESOLVED** by the [ADR-011 Amendment (2026-07-10)](../decisions/011-configuration-as-events-bootstrap.md#amendment--2026-07-10-marten-realization-via-iinitialdata) — the skill can codify their answers rather than re-litigate them:
  1. **ADR-011 Option A vs B for the Marten idiom** — resolved: `IInitialData` (registered via `.InitializeWith<T>()`) is the canonical Marten realization of Option A; it seeds at the deploy-time apply step (`resources setup`) and idempotently at host start as a self-healing safety net; the multi-instance race is mitigated by the idempotent guard + full-replacement (a double-seed converges) and avoided by the deploy-time step / single-instance MVP.
  2. **Singleton write-path concurrency** — resolved: config-as-events singletons use **last-writer-wins** (no optimistic concurrency; full-replacement has no invariant to defend), and a manual `session.Events.Append(<well-known-constant-id>, event)` is the accepted reconfigure shape (`[WriteAggregate]` does not apply — no id field on the command).
- **Remaining open debt (the skill only):** ground the seed pattern from the reference impl per the amendment's answers.
- **Retro source:** [`retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md`](../retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md).

### Wolverine.HTTP FluentValidation boundary wiring (`wolverine-http-handlers` addendum)

- **Gap:** No skill documents the **two-call** wiring that HTTP boundary validation actually requires. `csharp-coding-standards` § FluentValidation shows the nested `AbstractValidator<T>` shape, but the boundary needs BOTH `opts.UseFluentValidation()` in `UseWolverine` (the `WolverineFx.FluentValidation` assembly-scan that *registers* `IValidator<>` into DI) **and** `opts.UseFluentValidationProblemDetailMiddleware()` in `MapWolverineEndpoints` (the `WolverineFx.Http.FluentValidation` middleware that *resolves* them into a 400 ProblemDetails). Wiring only the middleware silently passes invalid input through as 200 — a footgun CI caught on slice-1's first run (`src/CritterCab.Telemetry/Program.cs` is the repo's first FluentValidation instance and the reference wiring). A tidy session should add this to `wolverine-http-handlers` (both packages, both calls, the DI-resolution dependency between them).
- **Retro source:** [`retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md`](../retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md) (§ "CI caught a bug local tooling structurally could not").

### `wolverine-grpc-bidirectional-handlers` — structural rewrite (client-streaming section is obsolete)

- **Gap:** The skill is structurally premised on WolverineFx.Grpc being unable to auto-generate client-streaming, and documents a hand-written `IMessageBus` workaround at length. **6.21.0 auto-generates the shape**, so that section describes a workaround for a problem that no longer exists — and CritterCab now ships code that contradicts it (`src/CritterCab.Telemetry/ReportLocations/{TelemetryGrpcService,ReportLocationsHandler}.cs`, passing tests). PR #45 applied only a **superseded banner** plus the frontmatter and mental-model-table one-liners, under the session-runner-blocking exception; the body was deliberately left intact rather than half-rewritten. The tidy session should retitle the skill to bidirectional-only (or "both shapes, both auto-generated"), delete the hand-written-workaround section, and rewrite the client-streaming pitfalls that assert the opposite of current behavior.
- **Why it was not fixed in-session:** the correction that *was* blocking (a session cannot follow a skill telling it to hand-wire) landed in `wolverine-grpc-handlers`, which is now the home for the auto-generated client-streaming pattern. The bidirectional skill's rewrite is structural, not a line fix, and belongs in its own scoped session.
- **Retro source:** [`retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md`](../retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md).

### `testing-fundamentals` vs. shipped test-class naming (decide, then apply everywhere)

- **Gap:** The skill mandates snake_case test class names. **All three** Telemetry test classes use PascalCase `Slice{N}<Feature>Tests` — `Slice1TelemetryPolicyTests` (shipped in PR #42), plus `Slice4LastKnownPositionTests` and `Slice2ReportLocationsTests` (PR #45). The audit confirms the skill is unambiguous and names this exact failure mode ("mixing PascalCase and snake_case test names within one test project") in its own pitfalls.
- **This is a decision, not a cleanup.** Either the skill is right and all three rename, or the `Slice{N}` grouping is a deliberate CritterCab convention — test suites grouped by workshop slice rather than one class per handler, so a reader can map a test class to the slice in the event model — and the skill needs a carve-out. **Do not fix it slice-locally**: renaming only the newer two would deepen the inconsistency the pitfall warns about. Deferred from PR #45 by user sign-off, because renaming a file from a merged PR is exactly the opportunistic edit the scope rule forbids.
- **Retro source:** [`retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md`](../retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md).

### Plain-document (non-event-sourced) write path — no governing skill

- **Gap:** Nothing in `docs/skills/` covers writing to a Marten document that is **not** an aggregate or projection: `session.Store()` overwrite-in-place, bulk delete via `DeleteWhere<T>` / `HardDeleteWhere<T>`, and when last-writer-wins is the whole concurrency story rather than a gap in one. `marten-querying` is scoped to reads by its own charter; `marten-wolverine-aggregates` covers the event-sourced path and its "never call `SaveChangesAsync` yourself" rule is scoped to aggregate handlers, so it neither sanctions nor forbids the document-only shape. PR #45's gate work had to source these APIs directly from Marten's `IDocumentOperations.cs`. Reference impl: `src/CritterCab.Telemetry/LastKnownPosition/`.
- **One decision worth codifying:** prefer `HardDeleteWhere<T>` over `DeleteWhere<T>` when the domain requires the row to actually be gone. `DeleteWhere` is *conditional* — it silently becomes a soft-delete flag if the document type is ever configured for soft-deletes, which turns a correctness guarantee into a configuration coincidence.
- **Retro source:** [`retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md`](../retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md).

### Recurring/periodic work — no governing skill (`wolverine-marten-automation` addendum or new skill)

- **Gap:** No skill documents periodic work, and the shape is non-obvious in a Wolverine codebase because **Wolverine has no first-class recurring-message primitive** — a session's natural first guesses (`ScheduleAsync`, `PublishMessage<T>().ToLocalQueue()`) are one-shot delayed delivery and routing configuration respectively, neither of them a scheduler. The idiom, used by Wolverine's own internals, is a plain .NET `BackgroundService` looping on `Task.Delay` that calls `IMessageBus` each tick. `wolverine-marten-automation` is the nearest skill but is explicitly event-triggered.
- **Two things a skill must state**, both of which bit or nearly bit this session: (1) split the timer from the work — a logic-free `BackgroundService` shell plus a normal handler holding everything testable, so the recurrence stays untested and the behavior stays covered; (2) the shell **must** take `IServiceScopeFactory` and open a scope per tick, because a `BackgroundService` is a singleton and Wolverine registers `IMessageBus` as scoped — injecting the bus directly fails host construction via `CallSiteValidator` (confirmed by mutation in PR #45). Reference impl: `src/CritterCab.Telemetry/LastKnownPosition/LastKnownPositionEvictionService.cs`.
- **Retro source:** [`retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md`](../retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md).

### `identity-acl` — the gRPC auth pattern does not apply to client-streaming or bidi

- **Gap:** `identity-acl/SKILL.md` (§ around L111–136) documents gRPC caller identity as a `[WolverineBefore]` middleware method taking `ServerCallContext` and reading `context.GetHttpContext().User`. That mechanism depends on `Before`/`Validate` frames binding against a concrete single request instance — which is **exactly what Wolverine cannot do for client-streaming or bidirectional RPCs**, where the method begins with a stream, not a message. So the documented pattern silently does not apply to those two shapes, and the skill states no such caveat.
- **CritterCab now has the counter-example in shipped code.** `ReportLocations` resolves identity through a plain DI seam reading `IHttpContextAccessor` (`src/CritterCab.Telemetry/ReportLocations/IDriverPrincipalAccessor.cs`) precisely because no `[WolverineBefore]` seam exists for its shape. The fix is an `identity-acl` addendum scoping the `ServerCallContext` pattern to unary/server-streaming and pointing streaming shapes at the accessor seam.
- **Registering, not fixing:** `identity-acl` is outside PR #45's scope, and the session was not blocked by it — the seam was designed from the middleware constraint directly (verified against `GrpcServiceChain.cs:270-272`), not from the skill.
- **Retro source:** [`retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md`](../retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md).

### Feature-folder / type-name collision — unstated consequence of `vertical-slice-organization`

- **Gap:** `vertical-slice-organization` mandates capability-named feature folders and a `{Type}.cs` file per type. Whenever a capability and its primary type share a name, that produces a namespace whose own name shadows the type — `CritterCab.Telemetry.TelemetryPolicy.TelemetryPolicy`, now `CritterCab.Telemetry.LastKnownPosition.LastKnownPosition`. Referring to the type from a *sibling* namespace then resolves to the namespace instead, and needs a `using X = global::…` alias or full qualification. This has now happened three times (the `TelemetryPolicy` view, the slice-1 test, and slice 4), each handled the same way by precedent rather than by any documented rule.
- **Why it is worth a line rather than tolerating:** the failure is a confusing compile error at a call site far from the cause, and a fourth occurrence written without the alias would hit it cold. A one-line addendum naming the pattern and the alias remedy — in `vertical-slice-organization` or `csharp-coding-standards` — is enough; no structural change is implied, since the collision is a *consequence* of a convention the repo wants.
- **Retro source:** [`retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md`](../retrospectives/implementations/007-telemetry-slices-4-and-2-transport.md).

### `protobuf-contracts` — the shared-type anti-pattern does not distinguish a published event contract from a borrowed value type

- **Gap:** § Anti-patterns names as wrong "defining a shared type inside a service's package" and importing it cross-BC (`GeoLocation` in `dispatch.v1`, pulled into `trips.v1`). PR D's csproj wiring has the same literal shape — Dispatch compiles `crittercab/telemetry/v1/driver_location_updated.proto` — but is a different relationship: Telemetry *publishes* `DriverLocationUpdated` as its outbound event contract (ADR-009, ADR-018) and Dispatch subscribes to it, which is what published language means. The skill draws no line between the two, so the wiring reads as a violation of its own guidance.
- **Why it was not fixed in-session:** the fix is a new distinction in a skill this session was otherwise only consuming, and it wants stating once for all future producer/consumer pairs rather than as a footnote to the first one. PR D put the reasoning in the csproj comment so the next reader is not left cold, but the skill is where it belongs.
- **Retro source:** [`retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md`](../retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md).

### No skill covers a non-event-sourced document write path inside an **event-sourced** service

- **Gap:** Already registered from Telemetry's `LastKnownPosition` (PR #45), but PR D is the sharper instance and worth noting against the existing row. Telemetry is stream-processing throughout, so a plain document there is unremarkable. **Dispatch is CritterCab's canonical event-sourced BC** — aggregates, inline projections, the whole decider apparatus — and now also carries `AvailableDriver`: a plain document, LWW-only, deliberately not a projection, sitting beside all of it. Nothing tells a session when that mixture is correct rather than a modelling mistake, and W006 §6.5's reasoning (event-sourcing an inbound high-volume feed would reimport the volume the producer's throttle exists to suppress) is the general rule but lives in a workshop.
- **Why it is worth a line:** the next session to consume a high-volume feed in an event-sourced BC will re-derive this from scratch, and the plausible wrong answer — "we event-source everything here, so make it a projection" — is the expensive one.
- **Retro source:** [`retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md`](../retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md).

### No skill covers the "forward-constraint placeholder message" shape

- **Gap:** `DriverAvailabilityChanged` is event-shaped (past tense, `domain-event-conventions` naming) but is appended to no Marten stream, so §6's `AddEventType<T>()` registration does not apply; and it is not a real integration event either, because no transport is bound to it and only tests invoke it. Neither `domain-event-conventions` (Marten-stream-scoped) nor `wolverine-messaging-handlers` (assumes a transport) names this shape. PR D scoped it by mirroring W006 §6.5's document columns 1:1 so it describes the shape of the hole rather than guessing at an un-workshopped BC's vocabulary — a rule worth writing down.
- **Why it is worth a line rather than tolerating:** the pattern will recur every time a slice half-lands across a boundary, and without a rule the scoping discipline (mirror the locked view fields; invent no transitions) is re-derived or skipped.
- **Retro source:** [`retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md`](../retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md).

### `transport-selection` — no built-vs-modeled status axis

- **Gap:** The skill's § Phasing table has a "Lands when" column but no per-row status a session can flip when a transport actually ships. As of PR C two of three transports are **built** (gRPC serving traffic, Kafka publishing) and one remains **modeled only** (Azure Service Bus) — and nothing in the skill distinguishes them, so a reader cannot tell which guidance is grounded in shipped code and which is still design intent. That distinction bites precisely where the skill gets used: choosing a transport for a new flow.
- **Not fixed in-session by design:** adding a status axis is a structural change to a skill this session was otherwise only consuming, and prompt 008 named the contingency in advance (check first; register a row rather than invent a structure mid-session). A tidy session should settle the axis shape once, ideally alongside whichever session builds ASB and makes all three rows answerable.
- **Retro source:** [`retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md`](../retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md).

### `testing-integration` — two wrong API names, and a collection convention no shipped fixture follows

- **Gap A (wrong API, costs a compile error):** § Testcontainers patterns shows `WithPullPolicy(PullPolicy.Missing)`. On Testcontainers 4.13.0 the builder method is **`WithImagePullPolicy`**, and `PullPolicy` lives in **`DotNet.Testcontainers.Images`**, not `DotNet.Testcontainers.Configurations`. Copying the skill's line verbatim fails with CS1061 + CS0103; PR C did exactly that and had to source the real names from the package's XML docs.
- **Gap B (convention vs. reality):** § Parallelization *Strategy 1 (Cab default)* specifies `[CollectionDefinition(Name, DisableParallelization = true)]` with a `public const string Name`. **None of the three shipped collections do this** — `Dispatch`, `Telemetry`, and now `TelemetryKafka` all use a bare string literal with no `DisableParallelization`. This is a repo-wide deviation, not a slice-local one, so PR C deliberately did not "fix" only its own collection: doing so would have deepened exactly the kind of inconsistency the test-class-naming row above warns about. **Decide, then apply everywhere** — either the fixtures adopt Strategy 1, or the skill records that Cab runs collections in parallel and relies on per-fixture container isolation (which is what it actually does today, and what PR C's unique container names now make safe).
- **Related, and already fixed:** PR C added `.WithName($"...-{Guid.NewGuid():N}")` to both Telemetry fixtures. The skill's own pitfall ("Sharing a Testcontainer name across fixtures without `Guid.NewGuid()`") was correct and was being violated; it went unnoticed while the project had only one container and became live when slice 3 added a second Postgres.
- **Retro source:** [`retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md`](../retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md) (surfaced by the two-axis code review).

### CI cannot build `apphost.cs` — the "Verify solution completeness" step does not reach it

- **Gap:** `apphost.cs` is a file-based app with no `.csproj`, so CI's existing "Verify solution completeness" step falls through it and nothing builds the AppHost. It broke for two weeks unnoticed before PR #45 fixed it, and PR C hit a second compile error in it (the `AddKafka` port API) that only a local `dotnet build apphost.cs` caught. Any session editing the AppHost is currently its own CI.
- **Fix shape:** **extend the existing step, do not add a new guard.** Not a skill row in the usual sense — recorded here because this is where the repo tracks known gaps between sessions, and because two consecutive sessions have now paid for it. CI changes carry their own blast radius and warrant a scoped session.
- **Retro source:** [`retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md`](../retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md); first flagged in the post-PR-45 handoff.

### `service-bootstrap` — the optional connection-string guard is an undocumented sanctioned deviation

- **Gap:** The skill's canonical `Program.cs` examples all read a connection string with a mandatory `?? throw new InvalidOperationException(...)`. Telemetry's shipped `Program.cs` instead guards *optionally* — `if (!string.IsNullOrEmpty(connectionString))` — for both Marten (since PR #42) and now Kafka, so the service boots without a database or a broker and degrades to the logging publisher. That is deliberate and useful (a broker-less `dotnet run` stays worth doing, and the non-Kafka test suites need it), but nothing in the skill sanctions it, so each session re-derives the choice from local precedent rather than from a rule.
- **What a fix should say:** name both shapes and the condition that selects one. Mandatory-throw when the dependency is load-bearing for every code path; optional-guard when there is a meaningful degraded mode the service is expected to run in. Reference impl for the optional shape: `src/CritterCab.Telemetry/Program.cs`.
- **Retro source:** [`retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md`](../retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md) (surfaced by the Phase 2 audit).

### `aspire` — the `AddKafka` port-allocation example does not compile on 13.4.6

- **Gap:** § Port allocation's worked example shows `builder.AddKafka("kafka").WithHostPort(5392)`. On Aspire 13.4.6 `WithHostPort` is an extension on `IResourceBuilder<KafkaUIContainerResource>` — the Kafka **UI** container, not the broker — so the line fails with CS1929. The broker takes its port as a constructor argument: `builder.AddKafka("kafka", port: 5392)`. PR C hit this on first build. The reserved port **5392 itself was correct** and is now claimed.
- **Why it went unnoticed:** `apphost.cs` is a file-based app with no `.csproj`, and CI's "Verify solution completeness" step does not reach it — so nothing mechanically checks the apphost or examples written against it. That CI gap is its own session (CI changes carry their own blast radius); this row is only the skill fix.
- **Retro source:** [`retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md`](../retrospectives/implementations/008-telemetry-slice-3-kafka-publish.md).

---

## Recently drained

### 2026-07-24 — `wolverine-kafka` listener sections, from the shipped consumer (PR D)

1 row drained *inside an implementation PR* rather than a `tidy: skills` session, which is the point of it: the row was deferred in PR C precisely because it could only be fixed honestly once a real consumer existed, and PR D is the session that built one.

- **`wolverine-kafka` listener/consumer-group/batching examples named a message type that was never published.** § Listening rewritten around the shipped `DriverLocationUpdatedHandler` and the real `telemetry.driver-location-updated` listener config; § Consumer groups grounded in the verified `ConsumerConfig.GroupId ??= ServiceName` default plus the reason Cab pins it explicitly; § Batch processing re-illustrated and **marked as a mechanic Cab does not use**; the stale ⚠ banner above § Bootstrap removed. The "fold in when drained" note (a hypothetical Pricing consumer) is resolved. Three new subsections capture what building the consumer taught: handler discovery as a **precondition for deserialization**, `DefaultIncomingMessage<T>()` as hardening rather than a requirement, and the `BeginAtLatest()`/`BeginAtEarliest()` cold-start choice with its testing consequence. Four new pitfalls. Retro at [`retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md`](../retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md).

### 2026-07-02 — wolverine-marten-automation tidy

2 rows drained via [`prompts/skills-tidy-wolverine-marten-automation.md`](../prompts/skills-tidy-wolverine-marten-automation.md), authoring a new skill rather than extending an existing one:

- **Marker-interface union return type** and **event-triggered automation handler shape** (both grouped under one heading in the prior entry). Fixed by authoring [`docs/skills/wolverine-marten-automation/SKILL.md`](./wolverine-marten-automation/SKILL.md) — a new skill rather than a `wolverine-handlers` bolt-on, per that skill's own trigger-agnostic charter. Grounded in both real examples (`FareQuoteAutomation`, `CandidateSelectionAutomation`) plus a second registration prerequisite (`CustomizeHandlerDiscovery(...WithNameSuffix("Automation"))`) the original retro didn't name. Retro at [`retrospectives/skills-tidy-wolverine-marten-automation.md`](../retrospectives/skills-tidy-wolverine-marten-automation.md).

### 2026-05-08 — initial tidy

7 rows drained across 3 skills via [`prompts/skills-tidy-marten-and-bootstrap.md`](../prompts/skills-tidy-marten-and-bootstrap.md):

- **`marten-projections` (4 rows):** `IEvent<T>` namespace; `SingleStreamProjection`/`MultiStreamProjection` namespace asymmetry; `ProjectionLifecycle` namespace; `SingleStreamProjection<T>` → `SingleStreamProjection<TDoc, TId>` type-parameter shape. Fixed by adding a "Namespaces" cheat-sheet table near the top of the skill. **Note:** the type-parameter half of one row was already correct in the skill body (likely fixed in an earlier pass, not re-verified at DEBT-row registration); only the namespace half required edits. Captured as a methodology learning in the retro.
- **`marten-wolverine-aggregates` (1 row):** `IEvent<T>` namespace. Fixed by adding a small "Namespaces" cheat-sheet listing `IEvent<T>`'s `JasperFx.Events` location.
- **`service-bootstrap` (2 rows):** Missing `AddWolverineHttp()` prerequisite for `MapWolverineEndpoints()`; `TimeProvider` DI registration. Fixed by adding a new "Service that exposes Wolverine.HTTP endpoints" subsection in Per-Service Configuration Variation, plus two new Common Pitfalls bullets. Prose-pass also corrected one residual "Oakton CLI surface" reference to "JasperFx CLI surface" (decision-to-flag #3 from the prompt).

Older entries drop off; the retros and commits remain authoritative.

---

## Out of scope for this file

- **Author-time conventions** (style, structure, voice). Those belong in [`docs/skills/README.md`](./README.md) and [`_template/SKILL.md`](./_template/SKILL.md).
- **Cross-skill consistency tasks** (e.g., reconciling overlapping content between two skills). Wider scope than a debt row; warrants its own prompt rather than a one-line entry here.
- **Lean-out work to avoid overlap with JasperFx `ai-skills`.** A deliberate authoring decision, not a reactive debt item; track it in the relevant skill's authoring history or a dedicated prompt.
- **Phase 6 placeholder cleanup** (the 14 skills tagged during phase 5 reconciliation). That work has its own scope and lives in the skills-foundation phase plan, not here.

---

## Document history

- **2026-05-08.** Initial authoring. Seven rows from the post-D→B→C session — five `marten-*` Marten 8.x / JasperFx namespace extractions plus two `service-bootstrap` registration prerequisites. Three other gaps from the same session (`RunOaktonCommandsAsync` → `RunJasperFxCommands`, `protobuf-contracts` directory layout, `service-bootstrap`/`aspire` connection-string contradiction) were fixed in-flight under the session-runner-blocking exception and do not appear here.
- **2026-05-08 (later same day).** Initial 7-row backlog drained via the first skill-tidy session. `Open debt` reset to empty. Retro at [`docs/retrospectives/skills-tidy-marten-and-bootstrap.md`](../retrospectives/skills-tidy-marten-and-bootstrap.md).
- **2026-06-25.** Registered two at-threshold rows surfaced by retro 005 (slice 5.3) and carried in the 2026-06-16 post-slice-5.3 handoff: the marker-interface union return type and the event-triggered automation handler shape, both grouped under `wolverine-handlers` (or a possible new `wolverine-marten-automation` skill). Registering, not fixing — the fix is a future `tidy: skills` session. The **bundling-rule encoding** gap (also flagged past-threshold in retro 005 and the handoff) was deliberately *not* registered: neither source names a target skill, and this file's convention requires a row to name the skill. It stays for a session that can ground the target.
- **2026-07-24 (PR C).** Registered six rows from the slice-3 Kafka session: `wolverine-kafka` listener-example refresh (deferred to PR D, which builds the consumer those examples describe), `transport-selection`'s missing built-vs-modeled status axis, the `aspire` skill's non-compiling `AddKafka` port example, and, from the Phase 2 audit and the two-axis code review, `service-bootstrap`'s undocumented optional connection-string guard, `testing-integration`'s two wrong Testcontainers API names plus its unfollowed collection convention, and the CI-cannot-build-`apphost.cs` gap. Two *other* `wolverine-kafka` gaps found the same session were **fixed in-flight** under the session-runner-blocking exception and do not appear here: its topic-naming section proposed a rule contradicting ADR-014/ADR-019, and its serialization section mandated JSON where the session ships protobuf — a session cannot follow a skill that contradicts the ADR it is authoring.
- **2026-07-02.** Drained both 2026-06-25 rows via a new `docs/skills/wolverine-marten-automation/SKILL.md` skill (critter-skill-auditor Phase 1 discovery ruled out both `wolverine-handlers` and `marten-wolverine-aggregates` as bolt-on homes). `Open debt` reset to empty. Item 1 of the [post-W006 handoff](../planning/2026-07-02-post-w006-next-steps-handoff.md)'s ordered table. Retro at [`docs/retrospectives/skills-tidy-wolverine-marten-automation.md`](../retrospectives/skills-tidy-wolverine-marten-automation.md).
- **2026-07-24 (PR D).** Drained the `wolverine-kafka` listener row **inside an implementation PR**, which is a first for this file and worth naming as a pattern: the row was registered in PR C with an explicit "fix it from the shipped consumer" condition, and PR D is the session that satisfied that condition. A row whose fix is *blocked on code that does not exist yet* is drained by the session that writes the code, not by a later `tidy: skills` session — deferring it further would only have kept a known-wrong skill in place for no gain. Registered three new rows the same session: `protobuf-contracts`' undrawn line between a published event contract and a borrowed shared type, a second and sharper instance of the missing non-event-sourced-document-in-an-event-sourced-BC guidance, and the unnamed "forward-constraint placeholder message" shape. The three decision-class rows (test-class naming, `testing-integration` Gap B, `identity-acl`) were deliberately left alone — each needs a call, not a tidy.
