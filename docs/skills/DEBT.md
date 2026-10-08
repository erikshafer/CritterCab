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

### `csharp-coding-standards` — § Geospatial Values mandates a `GeoLocation` type that does not exist

- **Gap:** § Geospatial Values says not to pass raw `(double, double)` tuples and to use one `GeoLocation` record everywhere. **No `GeoLocation` exists in the codebase.** Dispatch has `Shared/Location.cs` (`record Location(double Lat, double Lon, string? StreetAddress)`); Telemetry's `H3CellIndexer.TryComputeCell(double, double, int)` and Dispatch's `H3KRing` take raw degrees, deliberately mirrored so the two services compute identical cell ids.
- **This is a modelling decision, not a cleanup:** per-context value objects translated at the boundary (consistent with the context-owned-enums rule), or raw degrees accepted at the H3 binding layer with the rule scoped to domain surfaces. **Re-pointed 2026-10-08 at the Driver Profile chapter**, the next session to introduce a location-bearing type, which decides it.
- **Retro source:** [`retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md`](../retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md).

### Stale revision comments in Dispatch source and tests (not a skill row)

- **Gap:** after PR #47's fix, `AvailableDriver`'s revision is a `+1` counter written with `session.UpdateRevision(doc, existing.Version + 1)`, and staleness is judged by comparing `ServerReceivedAt`. Several comments still describe the earlier design: `src/CritterCab.Dispatch/Program.cs:64-67` and `AvailableDrivers/AvailableDriver.cs:71` name `TryUpdateRevision` (no code calls it); `AvailableDriver.cs:47-50` and `:75-81` describe the revision as a unix-ms timestamp and justify `ILongVersioned` by that magnitude; `Program.cs:182` and `:198` credit the "revision guard" with the deduplication the timestamp comparison now does; `tests/CritterCab.Dispatch.Tests/AvailableDrivers/NearbyAvailableDriversConsumerTests.cs:75-82` repeats the timestamp framing. `CritterCab.Dispatch.csproj:43` also says the protobuf skill "does not currently draw that distinction — see DEBT", which the boundary session's `protobuf-contracts` fix made untrue.
- **Why here:** the boundary session could not touch handler or document files (its prompt forbids it), and a comment that lies about a concurrency guard is the kind of drift that produced the original bug. Fix in the next PR that touches `AvailableDrivers/`; comments only, no behaviour change.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md) (surfaced by skill-auditor Phase 1).

### Surviving skills still name archived skills

- **Gap:** fifteen skills moved to [`archive/`](./archive/README.md) on 2026-10-08. No relative link broke (every cross-reference is a backtick name), but about 150 mentions in some 24 surviving skills still point readers at them as live (for example `wolverine-messaging-handlers`, `cli-jasperfx`, `grpc-vs-other-transports`, `marten-querying`, `marten-aggregates`, `marten-projections`, `wolverine-grpc-handlers`, `wolverine-handlers`, `wolverine-kafka`, `cli-grpc-tooling`, `cli-kafka-tooling`). The boundary session marked them "(archived)" only in the twelve skills it edited.
- **Fix shape:** one `tidy: skills` pass: mark or drop each mention; where a mention carries guidance (e.g. "use a saga here"), keep the guidance and drop the pointer. The archive README already tells readers how to read an unmarked mention.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `adding-a-service` — still describes the file-based AppHost

- **Gap:** S:70-72, :106, :212-217, :244, :376-377 assume a repo-root `apphost.cs`, `#:` directives and the CPM opt-out; S:223 and :246 add SQL Server and the ASB/Event Hubs emulators; Step 6 (S:54) lacks the action the project form now requires (a `ProjectReference` in `src/CritterCab.AppHost/CritterCab.AppHost.csproj`, which generates the `Projects.*` accessor, plus the resource in `AppHost.cs`). S:358's slnx pitfall understates the consequence: CI's completeness guard now fails the build.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `cli-aspire` — file-based AppHost, stale version, a config file that does not exist

- **Gap:** S:10, :35, :69, :75, :84, :113, :276, :299, :303, :557, :564, :573 assume `apphost.cs` and `#:sdk`/`#:package` directives; S:61 says Aspire 13.2.2 (the SDK is 13.4.6); S:384-415 show and claim a committed `aspire.config.json` (none exists); S:398 and :554 give dashboard ports 17000/15000/17213 (the dashboard is pinned to 5300/5301); S:285 names `Aspire.Hosting.AzureServiceBus` (the package is `Aspire.Hosting.Azure.ServiceBus`).
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `cli-grpc-tooling` — Go codegen and a governance workflow that do not exist

- **Gap:** S:12, :48, :123, :216-260, :732, :755 describe `buf generate` for a Go service (`services/cab-go`), which was dropped; S:727 says `buf.gen.yaml` is Go-only and warns against a C# plugin, the inverse of the real file (two C# plugins, no Go). S:82-116 show a `modules:` list and omit the scoped `ignore_only` the real `protos/buf.yaml` has. S:107-108 list streaming RPCs that do not exist (the only one is `ReportLocations`). S:18, :262-317, :305 describe a `proto-governance.yml` workflow with mandatory lint, format and breaking gates; the real gate is one lint-only step in `.github/workflows/dotnet.yml`.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `cli-kafka-tooling` — wrong topic, port, consumer group and wire format

- **Gap:** the topic is `telemetry.location-pings` throughout (the real one is `telemetry.driver-location-updated`); the broker is `localhost:9092` with "Aspire maps it dynamically" (S:399; it is pinned to 5392); the consumer group is `dispatch-service` (S:196, :379; it is `dispatch`); S:312 gives the dashboard as 17220 (5300); S:320 wires Pricing to Kafka (no Pricing service); S:391 assumes JSON with a `message-type` header (the wire is binary protobuf with `DefaultIncomingMessage<T>`); S:409-416 describe a dead-letter topic Cab does not use.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `wolverine-kafka` and `marten-querying` — revision guidance that predates the counter fix

- **Gap:** `wolverine-kafka` S:393 frames a pitfall around `TryUpdateRevision` and "if the revision is a timestamp", the pattern retro 009 found to be a bug (the shipped guard is a timestamp comparison plus a `+1` revision; see `marten-wolverine-aggregates` § Plain Documents). `marten-querying` S:477 lists `IRevisioned` as the numeric-concurrency marker and does not mention `ILongVersioned`, which shipped code uses.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `wolverine-marten-automation` — the "`*Handler` is invisible" claim is wrong

- **Gap:** S:97 and S:242 say naming an automation `*Handler` hides it from discovery. Discovery is additive: `CustomizeHandlerDiscovery(...WithNameSuffix("Automation"))` adds a suffix, and Wolverine's built-in `Handler` suffix still applies (`src/CritterCab.Dispatch/Program.cs:133-136`; `DriverLocationUpdatedHandler` is discovered that way). Only the vocabulary objection holds.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `marten-wolverine-aggregates` — "silently dropped" claim unverified

- **Gap:** § Anti-Pattern: Manual Session Calls Inside an Aggregate Handler says a manual `Append` in a `[WriteAggregate]` handler is "silently dropped". Neither service enables `AutoApplyTransactions`, which bears on the claim; it has not been verified against Wolverine source. Verify (jasperfx-source-verifier) and correct or cite.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `service-bootstrap` and `wolverine-grpc-handlers` — residue in untouched sections

- **Gap:** `service-bootstrap`: the `ServiceLocationPolicy.AlwaysAllowed` pitfall (~S:489) concerns an option no service sets; the illustrative `CritterStackDefaults` block uses `TypeLoadMode.Dynamic`, unverified after the Wolverine 6 / Marten 9 runtime-codegen removal; ~S:476 says the implicit `Main` suffices for `AlbaHost.For<Program>()`, but both services declare `public partial class Program { }`. `wolverine-grpc-handlers` S:653 cites a Trips `apphost.cs` dependency on Pricing; neither exists. `wolverine-handlers` S:112-116 says FluentValidation "runs first" with no wiring (the two-call wiring is now in `wolverine-http-handlers`).
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### "(Phase N)" tags on surviving skills

- **Gap:** cross-references such as "(Phase 2)" (e.g. `marten-wolverine-aggregates` S:883-888, `wolverine-http-handlers` S:290-291) record the order the library was authored in. With the phase plan retired they read as status; drop them in the archived-names pass above.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `testing-fundamentals` — residue the naming carve-out did not reach

- **Gap:** the package table (S:41-52) is out of date (Alba 8.5.2 vs 8.5.3, Test.Sdk 18.4.0 vs 18.8.1, Testcontainers 4.11.0 vs 4.13.0) and lists packages that are not pinned (`Testcontainers.MsSql`, `Testcontainers.ServiceBus`, `Microsoft.Extensions.TimeProvider.Testing`); S:62-109 and :664 require a `TestEnvironmentInitializer` `[ModuleInitializer]` no test project has; S:105 says `dotnet test` runs the projects sequentially (CI runs the two assemblies in parallel); S:138, :145 describe a `Fixtures/` folder (fixtures sit at each project's root); S:249-262 still show a `const Name` collection and an `IAsyncLifetime` test class with `CleanAllMartenDataAsync`, contradicting `testing-integration`; S:32, :691 point at SQL Server and emulator patterns `testing-integration` no longer carries.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `protobuf-contracts`, `domain-event-conventions`, `transport-selection` — residue outside the drained sections

- **Gap:** `protobuf-contracts` S:27-28 name skills that do not exist (`wolverine-grpc-services`, `wolverine-grpc-client-streaming`); its "Money as a canonical shared type" section prescribes a `Money` message while both shipped contracts carry `int64 *_minor_units` plus `string currency` — **a decision, not a cleanup**: adopt `Money` at the next contract that carries an amount, or record the minor-units shape as the convention. `domain-event-conventions` S:70-76, :254 still say namespaces are flat `CritterCab.{ServiceName}`, contradicting the folder namespaces that ship and that `vertical-slice-organization` now documents (whose S:409 in turn says events sit "at the project root level"); S:90, :100, :271 use `GeoLocation` (see the `GeoLocation` row). `transport-selection` S:63, :88 say Kafka carries raw GPS pings consumed by Dispatch and Pricing; raw pings arrive over gRPC, Kafka carries the throttled `DriverLocationUpdated`, and there is no Pricing consumer. Not a skill file: the root `.gitignore` still ignores `*.pb.go` / `*_grpc.pb.go`, which nothing generates since the Go output was removed.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

### `DispatchTestFixture` does not follow the fixture conventions the others do

- **Gap:** `tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs:12-13` sets neither a unique `.WithName($"...-{Guid.NewGuid():N}")` nor `.WithImagePullPolicy(PullPolicy.Missing)`, which the three other fixtures and `testing-integration` now document. Test-code drift, not a skill gap; fix in the next PR that touches Dispatch's tests.
- **Retro source:** [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

---

## Recently drained

### 2026-10-08 — the boundary session drained the ledger

17 of 18 rows closed via [`prompts/crittercab-v2-boundary.md`](../prompts/crittercab-v2-boundary.md), each verified against shipped code first (skill-auditor Phase 1). Retro at [`retrospectives/crittercab-v2-boundary.md`](../retrospectives/crittercab-v2-boundary.md).

- **Drained into skills (13):** config-as-events seed and plain-document write path, including the non-event-sourced document in an event-sourced service (`marten-wolverine-aggregates`, two new sections; its manual-session anti-pattern scoped to aggregate handlers); FluentValidation two-call wiring (`wolverine-http-handlers`); recurring work (`wolverine-marten-automation`, timer-driven section and widened scope); feature-folder collision (`vertical-slice-organization`); published event contract versus borrowed value type (`protobuf-contracts`); forward-constraint placeholder (`domain-event-conventions`); status axis (`transport-selection`); optional connection-string guard (`service-bootstrap`, reconciled with `aspire`); `AddKafka` example and the AppHost section (`aspire`, rewritten for the project form); test-class naming, resolved as snake_case methods in PascalCase classes named by slice (`testing-fundamentals`); `testing-integration` Gap A (API names, constructor-form builders) and Gap B (the four shipped collections, parallel with per-fixture isolation and inline reset, recorded as the convention).
- **Closed by the workflow (2):** the unenforced pre-pull list (CI guard in `.github/workflows/dotnet.yml`, mutation-tested) and CI not building the AppHost (it is a project in `CritterCab.slnx`).
- **Archived with their skills (2):** the `wolverine-grpc-bidirectional-handlers` rewrite and the `identity-acl` streaming caveat.
- **Left open (1):** `GeoLocation`, re-pointed at the Driver Profile chapter.

### 2026-07-24 — `wolverine-kafka` listener sections, from the shipped consumer (PR D)

1 row drained *inside an implementation PR* rather than a `tidy: skills` session, which is the point of it: the row was deferred in PR C precisely because it could only be fixed honestly once a real consumer existed, and PR D is the session that built one.

- **`wolverine-kafka` listener/consumer-group/batching examples named a message type that was never published.** § Listening rewritten around the shipped `DriverLocationUpdatedHandler` and the real `telemetry.driver-location-updated` listener config; § Consumer groups grounded in the verified `ConsumerConfig.GroupId ??= ServiceName` default plus the reason Cab pins it explicitly; § Batch processing and § Dead letter topics re-illustrated and **marked as mechanics Cab does not use** (the DLQ section gained the reasoning: a self-healing position feed makes a dead-lettered message worth less than the cost of a topic to inspect); the stale ⚠ banner above § Bootstrap removed. The "fold in when drained" note (a hypothetical Pricing consumer) is resolved. Three new subsections capture what building the consumer taught: handler discovery as a **precondition for deserialization**, `DefaultIncomingMessage<T>()` as hardening rather than a requirement, and the `BeginAtLatest()`/`BeginAtEarliest()` cold-start choice with its testing consequence. Four new pitfalls. Retro at [`retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md`](../retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md).

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

---

## Document history

- **2026-05-08.** Initial authoring. Seven rows from the post-D→B→C session — five `marten-*` Marten 8.x / JasperFx namespace extractions plus two `service-bootstrap` registration prerequisites. Three other gaps from the same session (`RunOaktonCommandsAsync` → `RunJasperFxCommands`, `protobuf-contracts` directory layout, `service-bootstrap`/`aspire` connection-string contradiction) were fixed in-flight under the session-runner-blocking exception and do not appear here.
- **2026-05-08 (later same day).** Initial 7-row backlog drained via the first skill-tidy session. `Open debt` reset to empty. Retro at [`docs/retrospectives/skills-tidy-marten-and-bootstrap.md`](../retrospectives/skills-tidy-marten-and-bootstrap.md).
- **2026-06-25.** Registered two at-threshold rows surfaced by retro 005 (slice 5.3) and carried in the 2026-06-16 post-slice-5.3 handoff: the marker-interface union return type and the event-triggered automation handler shape, both grouped under `wolverine-handlers` (or a possible new `wolverine-marten-automation` skill). Registering, not fixing — the fix is a future `tidy: skills` session. The **bundling-rule encoding** gap (also flagged past-threshold in retro 005 and the handoff) was deliberately *not* registered: neither source names a target skill, and this file's convention requires a row to name the skill. It stays for a session that can ground the target.
- **2026-07-24 (PR C).** Registered six rows from the slice-3 Kafka session: `wolverine-kafka` listener-example refresh (deferred to PR D, which builds the consumer those examples describe), `transport-selection`'s missing built-vs-modeled status axis, the `aspire` skill's non-compiling `AddKafka` port example, and, from the Phase 2 audit and the two-axis code review, `service-bootstrap`'s undocumented optional connection-string guard, `testing-integration`'s two wrong Testcontainers API names plus its unfollowed collection convention, and the CI-cannot-build-`apphost.cs` gap. Two *other* `wolverine-kafka` gaps found the same session were **fixed in-flight** under the session-runner-blocking exception and do not appear here: its topic-naming section proposed a rule contradicting ADR-014/ADR-019, and its serialization section mandated JSON where the session ships protobuf — a session cannot follow a skill that contradicts the ADR it is authoring.
- **2026-07-02.** Drained both 2026-06-25 rows via a new `docs/skills/wolverine-marten-automation/SKILL.md` skill (critter-skill-auditor Phase 1 discovery ruled out both `wolverine-handlers` and `marten-wolverine-aggregates` as bolt-on homes). `Open debt` reset to empty. Item 1 of the [post-W006 handoff](../planning/2026-07-02-post-w006-next-steps-handoff.md)'s ordered table. Retro at [`docs/retrospectives/skills-tidy-wolverine-marten-automation.md`](../retrospectives/skills-tidy-wolverine-marten-automation.md).
- **2026-07-24 (PR D).** Drained the `wolverine-kafka` listener row **inside an implementation PR**, which is a first for this file and worth naming as a pattern: the row was registered in PR C with an explicit "fix it from the shipped consumer" condition, and PR D is the session that satisfied that condition. A row whose fix is *blocked on code that does not exist yet* is drained by the session that writes the code, not by a later `tidy: skills` session — deferring it further would only have kept a known-wrong skill in place for no gain. Registered five new rows the same session: `protobuf-contracts`' undrawn line between a published event contract and a borrowed shared type, a second and sharper instance of the missing non-event-sourced-document-in-an-event-sourced-BC guidance, the unnamed "forward-constraint placeholder message" shape, `csharp-coding-standards`' `GeoLocation` mandate for a type that does not exist, and the unenforced CI pre-pull list this session had to introduce. Also folded a companion note into the standing `testing-integration` Gap B row (its per-test cleanup convention has no adherents either) and corrected that row's collection count, which was stale by one. The three decision-class rows (test-class naming, `testing-integration` Gap B, `identity-acl`) were deliberately left alone — each needs a call, not a tidy.
- **2026-10-08 (boundary session).** Drained, closed or archived 17 of 18 rows (see Recently drained); `GeoLocation` re-pointed at the Driver Profile chapter. Registered the rows the session surfaced: stale revision comments in Dispatch, archived-skill names in surviving skills, the file-based-AppHost residue in `adding-a-service` and `cli-aspire`, Go and governance-workflow residue in `cli-grpc-tooling`, stale topic and wire details in `cli-kafka-tooling`, revision guidance in `wolverine-kafka` and `marten-querying`, the `*Handler` discovery claim in `wolverine-marten-automation`, an unverified claim in `marten-wolverine-aggregates`, residue in `service-bootstrap`, `wolverine-grpc-handlers`, `testing-fundamentals`, `protobuf-contracts` (including an open `Money` decision), `domain-event-conventions` and `transport-selection`, phase tags, and `DispatchTestFixture`.
