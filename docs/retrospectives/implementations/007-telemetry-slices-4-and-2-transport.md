# Retrospective — Telemetry Slice 4 (`LastKnownPosition` + eviction) + Slice 2 (gRPC `ReportLocations` ingest)

## Metadata

- **Triggering prompt:** [`docs/prompts/implementations/007-telemetry-slices-4-and-2-transport.md`](../../prompts/implementations/007-telemetry-slices-4-and-2-transport.md)
- **Status:** Complete
- **Date authored:** 2026-07-24
- **Output artifacts:**
  - `Directory.Packages.props` — all 11 `WolverineFx*` entries 6.19.0 → **6.21.0 in lockstep**; new `Grpc.AspNetCore` / `Grpc.Tools` / `Google.Protobuf` (2.76.0 / 2.76.0 / 3.31.1) and `pocketken.H3` 4.5.0.1
  - `src/CritterCab.Telemetry/CritterCab.Telemetry.csproj` — first `<Protobuf>` items in the repo; `report_locations.proto` (`GrpcServices="Both"`) + `driver_location_updated.proto` (`GrpcServices="None"`)
  - `src/CritterCab.Telemetry/LastKnownPosition/LastKnownPosition.cs` — plain Marten document; first non-event-sourced write path
  - `src/CritterCab.Telemetry/LastKnownPosition/EvictStalePositions.cs` — sweep command + handler (`HardDeleteWhere`, threshold from policy)
  - `src/CritterCab.Telemetry/LastKnownPosition/LastKnownPositionEvictionService.cs` — logic-free `BackgroundService` timer
  - `src/CritterCab.Telemetry/ReportLocations/TelemetryGrpcService.cs` — empty `[WolverineGrpcService]` stub
  - `src/CritterCab.Telemetry/ReportLocations/ReportLocationsHandler.cs` — the ingest; per-ping pipeline + publish trigger
  - `src/CritterCab.Telemetry/ReportLocations/H3CellIndexer.cs` — the H3 binding, wrapped for one reason
  - `src/CritterCab.Telemetry/ReportLocations/{IDriverPrincipalAccessor,IDriverLocationPublisher}.cs` — two ready-to-swap seams + dev stubs
  - `src/CritterCab.Telemetry/Program.cs` — `AddGrpc` / `AddWolverineGrpc` / `MapWolverineGrpcServices`, seam registrations, `AddHostedService` inside the Marten guard
  - `tests/CritterCab.Telemetry.Tests/` — `Slice4LastKnownPositionTests` (6), `H3CellIndexerTests` (5), `Slice2ReportLocationsTests` (5); fixture gains `ConfigureTestServices`, `RecordingDriverLocationPublisher`, `CreateGrpcChannel`
  - `apphost.cs` — **fixed a two-week-old compile break** (missing `#:project` for Telemetry); gRPC rides the existing HTTPS endpoint
  - `protos/crittercab/telemetry/v1/report_locations.proto` — stale forward-constraint comment corrected
  - `docs/skills/wolverine-grpc-handlers/SKILL.md` — 7 stale claims corrected + new **Client-streaming handlers** section (deliverable 13a)
  - `docs/skills/wolverine-grpc-bidirectional-handlers/SKILL.md` — superseded banners + mental-model/frontmatter corrections only (13b)
  - `docs/skills/DEBT.md` — 6 new rows
  - `docs/workshops/006-telemetry-event-model.md` `## Document History` — slices 2 + 4 realized; forward-constraint closed
  - `docs/prompts/README.md` — Implementations index entry
  - This retro
- **Outcome:** Both slices implemented end-to-end. **31/31 green locally and in CI** (Telemetry 20, Dispatch 11), 0 warnings, first-try CI pass. Docker was working again this session, so Testcontainers ran locally rather than leaning on CI as at PR #42. PR [#45](https://github.com/erikshafer/CritterCab/pull/45).

---

## Framing

This is the session the whole project has been pointing at. gRPC, Kafka and ASB have been modeled across five workshops and sixteen ADRs and wired in **zero lines of code**; `protos/` has held authored contracts with nothing consuming them since PR #39. This session makes the first of them real.

It was also gated for three months on a genuine library limitation — WolverineFx.Grpc could not auto-generate `stream in → unary out` — which every prior handoff carried forward as "hand-wire `ReportLocations` against `IMessageBus`." That constraint closed four days before the session ran.

---

## Outcome summary

Four commits, one per chunk plus the prompt. Chunk order was dependency-driven and held: version bump + codegen → slice 4 → slice 2 → docs. Slice 4 preceding slice 2 mattered — the trigger needs a real baseline to evaluate against, and building it the other way would have meant stubbing the document twice.

**Five firsts in code:** first gRPC surface serving traffic, first client-streaming RPC, first proto codegen, first non-event-sourced document write path, first recurring/scheduled work.

---

## What worked

**The Verify-before-wiring ledger paid for itself, and it paid off most where it was wrong.** Eleven gates were source-verified before any code. Gate 5 is the case in point: the prompt's own authoring hypothesis — that Wolverine has a recurring-message primitive reachable via `IScheduledJobProcessor` or `PublishMessage<T>().ToLocalQueue()` — was **wrong on both counts**, and the 2026-07-21 trace caught it and replaced it with the real idiom (a plain `BackgroundService`, template `HeartbeatBackgroundService`). Had that not been traced, the session would have spent its budget hunting an API that does not exist. A verification pass that only ever confirms is not doing its job; this one disconfirmed twice (gates 5 and 8a) and closed the rest.

**Gate 11 dissolved rather than being worked around.** The worry was that Wolverine's client-streaming fixture builds a raw `WebApplication` + `UseTestServer()`, while CritterCab standardizes on Alba. Alba wraps `WebApplicationFactory`, which runs on `TestServer` underneath — so `Host.GetTestServer().CreateHandler()` feeds a `GrpcChannel` directly. No parallel host recipe, no new test packages, Alba-first default intact. The prompt's fallback ("a dedicated gRPC fixture, document the deviation") was never needed.

**Two under-specifications were escalated rather than silently resolved.** W006 §3.3/§6.4 disagree with the prompt's deliverable 4 on the `LastKnownPosition` field set, and §6.2 names an `accuracyMeters` threshold while fixing no value. Both went to the user as explicit forks with recommendations. The first produced a genuinely better answer than either source document had: since §6.4 locks upsert-on-publish-only, `serverReceivedAt` and `lastPublishedAt` are the same instant **by construction**, so one field serves both the trigger baseline and the eviction key — a simplification neither the workshop nor the prompt had noticed.

**The pinning tests were designed to fail, not to pass.** Both load-bearing tests in this session assert something a naive version would miss:

- *H3.* "The cell is valid" passes with lat/lon swapped — a swapped ping indexes to a valid cell in the Indian Ocean. So the test computes the point down **both** API paths, which are mirror opposites on axis order *and* units, and asserts equality; two wrongs cannot cancel. A separate test proves the swapped input yields a *different* cell, which is what makes the first test meaningful.
- *Eviction threshold.* "A stale document is deleted" passes against a hardcoded 90 seconds. So the same 10-second-old document is asserted to **survive** under the seeded 30s heartbeat and be **swept** under a reconfigured 1s one. Only the pair distinguishes "reads policy" from "coincidentally matches the default."

**The DI guard was mutation-verified.** `IMessageBus` is scoped, a `BackgroundService` is a singleton. The eviction shell takes `IServiceScopeFactory` for that reason, and rather than assert the guard works, the session temporarily injected the bus directly and confirmed `CallSiteValidator` fails host construction — then reverted. The comment claiming protection is now a claim that was tested.

---

## What was harder than expected

**`apphost.cs` had not compiled since PR #42.** The Telemetry service block was added without its `#:project` directive, so `Projects.CritterCab_Telemetry` never existed. It was found only because this session had to touch the file.

The durable finding is not the one-line fix — it is **why it survived two weeks**. CI builds `CritterCab.slnx` and even has a "Verify solution completeness" step that fails when a `.csproj` on disk is missing from the solution. The apphost is a **file-based app with no `.csproj`**, so it falls through a guard that already exists and was written for exactly this class of mistake. The fix is to extend that step to build `apphost.cs`, not to invent a new check. Flagged as a next-session input rather than done here: CI changes are their own scope.

**Nothing else in the session was structurally hard**, which is itself worth recording. The client-streaming shape — three months of forward-constraint, two skills' worth of workaround documentation, a standing entry in every handoff — took an empty one-line stub and a handler that imports nothing from `Grpc.*`. The cost was never in the implementation; it was in the library gap, and once that closed the design absorbed it without a ripple.

---

### Design meets code — three things the workshop could not have known

1. **Middleware does not weave for client-streaming**, so per-ping validation *must* live in the handler. W006 §6.2 lists validation as pipeline step 2 without saying where it runs; slice 1 established "validate at the HTTP boundary, the aggregate stays thin" as the CritterCab pattern. This slice cannot follow it — a before-frame needs a concrete request at method entry and a stream cannot supply one. The asymmetry is now recorded in the proto, the skill, and `Program.cs` beside the FluentValidation line it contrasts with, because a reader who sees only the handler will otherwise read it as drift.

2. **`HardDeleteWhere` vs `DeleteWhere` is a domain decision wearing an API costume.** Marten's `DeleteWhere` is *conditional*: configure the document for soft-deletes later and it silently sets a flag instead of removing the row. §6.4's "Return" GWT requires an evicted driver to find **no** baseline so the trigger republishes immediately. Choosing the verb is choosing whether that GWT survives a future configuration change.

3. **The `identity-acl` gRPC auth pattern does not apply to this shape.** That skill documents caller identity via `[WolverineBefore]` middleware reading `ServerCallContext` — which depends on the same single-request binding that client-streaming lacks. The seam this slice built (`IDriverPrincipalAccessor` over `IHttpContextAccessor`) was designed from the middleware constraint directly, not from the skill; the skill's silence on the exception is now a DEBT row.

---

## Methodology refinements

- **A prompt's own hypotheses need verifying, not just the library's API.** Gate 5's correction was to the *prompt*, and the prompt was authored by the same process that would have executed it. The verification pass earns its cost specifically by being able to contradict its author.
- **Escalate under-specification as a fork with a recommendation, not as a blocker or a silent default.** Three forks were raised this session; all three took the recommendation, and one produced a better model than either source doc. The cost of asking was one round-trip each.
- **A regression guard deserves a mutation check when it is guarding something invisible.** The scoped/singleton failure is a startup crash, not a test failure — nothing in the normal suite would have noticed the guard rotting.
- **Fix-in-passing needs a stated reason.** `apphost.cs` was repaired because a deliverable targeted a file that did not build; the no-opportunistic-edits rule held everywhere else, including the test-naming violation, which was deferred to DEBT precisely because fixing it slice-locally would have deepened the inconsistency.

---

## Outstanding items / next-session inputs

- **CI does not build `apphost.cs`.** Extend the existing "Verify solution completeness" step to cover file-based apps. This is the finding, not the fix that landed.
- **Six DEBT rows registered** (`docs/skills/DEBT.md`): the `wolverine-grpc-bidirectional-handlers` structural rewrite (its ~145-line hand-written-workaround body is bannered, not removed); test-class naming across all three Telemetry suites; no skill for plain-document write paths; no skill for recurring work; the `identity-acl` streaming exception; and the feature-folder/type-name collision now on its third occurrence.
- **Gate 9 was deliberately not exercised.** Whether cascading messages and `[Transactional]`/outbox middleware weave for a handler whose message type is `IAsyncEnumerable<T>` remains unverified — the design sidesteps it by fanning out through injected dependencies called directly rather than through cascaded messages. Recorded here as prompt 007 gate 9 asked: if a future slice makes this handler emit cascading messages, that question becomes live.
- **`MaxAccuracyMeters = 100` is invented at implementation time.** W006 §6.2 names the threshold and fixes no value. It follows §6.4's precedent (documented constant, not a policy param, promote in v2) but it is a number this session chose, and W006's Document History says so.
- **Slice 3 (PR C)** swaps `LoggingDriverLocationPublisher` for the real Kafka producer. The seam already carries the generated `DriverLocationUpdated`, so only the implementation changes.

---

## Spec delta — landed?

**Yes, in full.**

- **W006 §6.2 designed → realized.** The client-streaming ingest, per-ping pipeline and `shouldPublish` trigger are running code, exercised by real gRPC streams.
- **W006 §6.4 designed → realized.** `LastKnownPosition` and the eviction sweep are running code.
- **The client-streaming forward-constraint is closed, not carried.** Recorded in W006's Document History, corrected in the proto, and corrected in both gRPC skills.
- **W006 §11's *windowed gRPC client-streaming ingest* candidate fired as a skill, not an ADR** — as §6.2 and the handoff both leaned. The auto-generated shape plus its middleware caveat is library mechanics, not an architectural choice CritterCab made; it belongs in `wolverine-grpc-handlers`, where it now lives. The other two §11 candidates (Kafka topic-naming, stream-processing-as-4th-shape) remain later-arc, as the prompt named them.
