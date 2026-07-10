# Prompt 006 — Bootstrap the Telemetry Service Skeleton + Slice 1 (`TelemetryPolicyConfigured`)

| Field | Value |
|---|---|
| **Status** | Complete (2026-07-10). Produced `src/CritterCab.Telemetry/` + `tests/CritterCab.Telemetry.Tests/`; build green, smoke test green locally, slice-1 tests verified on CI (local Docker wedged). Retro at [`docs/retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md`](../../retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md). |
| **Authored** | 2026-07-10 |
| **Target artifacts** | `src/CritterCab.Telemetry/` (new service), `tests/CritterCab.Telemetry.Tests/` (new test project), `CritterCab.slnx`, `apphost.cs`, `Directory.Packages.props` (FluentValidation additions), `docs/skills/DEBT.md`, `docs/workshops/006-telemetry-event-model.md` (Document History), `docs/prompts/README.md` (index entry), and this prompt's retro. |
| **Source-of-truth dependencies** | [W006 §3, §5, §6.1, §9](../../workshops/006-telemetry-event-model.md); [ADR-011](../../decisions/011-configuration-as-events-bootstrap.md); [ADR-005](../../decisions/005-transport-selection-by-flow-type.md); [ADR-018](../../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md) (context only). Skills: `adding-a-service`, `service-bootstrap`, `aspire`, `vertical-slice-organization`, `csharp-coding-standards`, `domain-event-conventions`, `marten-aggregates`, `marten-wolverine-aggregates`, `marten-projections`, `wolverine-http-handlers`, `testing-integration`, `testing-fundamentals`. |
| **Workflow position** | First implementation session of the W006 Telemetry chain. Opens CritterCab's first real transport build; this PR is the **skeleton + first slice** (named PR-cadence exception), and lands the config foundation the transport slices read. Second service in the repo (Dispatch is the reference). |

---

## Framing — why this session exists

The Telemetry chain's design is fully locked (W006 R1–R8, five slices, ADR-018) and its `v1` protobuf contracts are shipped (PR #39). The next move is the first transport wiring in code — but the transport slices have a dependency spine: slice 2's gRPC ingest reads slice 1's `TelemetryPolicy` view (h3Resolution, intervals, `throttlePolicyVersion`) and evaluates its publish trigger against slice 4's `LastKnownPosition`. Config-as-events is therefore the dependency-correct first slice, and it is self-contained (Marten singleton stream + HTTP + Postgres, no transport). This PR stands up the `CritterCab.Telemetry` service and realizes W006 slice 1, exactly mirroring the Dispatch skeleton + slice-5.1 precedent (PR precedent for the "skeleton + first slice share one PR" exception). gRPC ingest, Kafka publish, and the Dispatch consumer land in the follow-on PRs sequenced at the foot of this prompt.

**No narrative anchors this session.** PR #40 decided the narrative layer does not apply to Telemetry (no protagonist-perceivable moment; see [`narratives/README.md`](../../narratives/README.md) § "When the narrative layer does not apply"). The workshop is the direct spec anchor.

---

## Goal

Produce a buildable, testable `CritterCab.Telemetry` service skeleton **and** implement W006 slice 1: the `TelemetryPolicyConfigured` config-as-events singleton stream, the `ConfigureTelemetryPolicy` HTTP endpoint with boundary FluentValidation, the migration-time idempotent bootstrap seed (ADR-011 Option A), the `TelemetryPolicy` latest-policy view carrying `throttlePolicyVersion`, and Alba integration tests over Postgres.

---

## Spec delta

- **W006 §6.1 moves designed → realized in code.** The `TelemetryPolicy` view, the bootstrap seed, and `throttlePolicyVersion`-derived-from-stream-version become concrete; W006 `## Document History` records the realization.
- **ADR-011 gains its third instance and first in-code realization** (Dispatch/Onboarding were design-only). This session writes the first config-as-events bootstrap seed in the repo — the trigger ADR-011 (§ Consequences, final ¶) named for codifying the migration-template skill; that codification is registered as a `DEBT.md` row this session, not authored here.
- **No ADR fires in this PR.** W006 §11's three ADR candidates are all later-arc: Kafka topic-naming (slice 3 / PR C), stream-processing-as-4th-shape (best evidenced by the document/Kafka core, slices 2–4 / PR B), windowed gRPC client-streaming (slice 2 / PR B). Named here as explicit deferrals so the closure loop stays honest.

---

## Orientation files (read in order)

1. **[W006 §3 (stream-processing shape), §5 (event list), §6.1 (slice 1), §9 (config-as-events)](../../workshops/006-telemetry-event-model.md)** — the slice spec, seed defaults, validation rules, GWT sketches, and `throttlePolicyVersion` provenance.
2. **[ADR-011](../../decisions/011-configuration-as-events-bootstrap.md)** — Option A migration-time idempotent seed; `operatorId = "system-bootstrap"`, `reason = "Initial deployment defaults"`; the idempotent guard (load singleton stream state → append only if empty).
3. **Skeleton skills** — `docs/skills/adding-a-service/SKILL.md` (solution layout, own DB `crittercab_telemetry`, no cross-service `ProjectReference`, port slot **5315/5316** as the next `+5` after Dispatch's 5310), `docs/skills/service-bootstrap/SKILL.md` (composition-root contract; `AddEventType<T>()` mandatory; null-guard the connection string — never `?? throw`; `RunJasperFxCommands`), `docs/skills/aspire/SKILL.md`.
4. **Slice-1 skills** — `docs/skills/csharp-coding-standards/SKILL.md`, `docs/skills/marten-aggregates/SKILL.md` + `docs/skills/marten-wolverine-aggregates/SKILL.md` (singleton stream, static `Create`/`Apply`, full-replacement), `docs/skills/marten-projections/SKILL.md` (self-aggregating live-stream aggregation), `docs/skills/wolverine-http-handlers/SKILL.md` (boundary validation).
5. **Reference implementation** — `src/CritterCab.Dispatch/` (mirror `.csproj`, `Program.cs`, positional-record idiom) and `tests/CritterCab.Dispatch.Tests/DispatchTestFixture.cs` (Testcontainers-Postgres + `AlbaHost.For<Program>` + forwarding-singleton override pattern).

---

## Working pattern

- Interactive, sign-off per logical chunk: (a) skeleton stands up and smoke-tests green, then (b) slice 1 lands with its Alba tests. Confirm the skeleton builds before layering slice 1.
- **Verify-before-wiring gates first (see next section)** — resolve the four API claims before committing code that depends on them.
- Local Docker container-start is wedged (verified 2026-07-10: `docker run` hangs). **Restart Docker Desktop before running Testcontainers locally, or lean on CI as the test gate** (it ran the full suite green for PR #41). Do not treat a local test hang as a code failure without ruling out Docker.
- Positional `sealed record`s for the command/event/view — match the shipped Dispatch idiom (`RideRequested`, `CandidatesSelected`); positional parameters are inherently mandatory, satisfying the "no optional-with-default fields" invariant. Do **not** flag this against `csharp-coding-standards`' `required` *recommendation* — both styles are sanctioned; positional is the in-code convention.
- Branch from `main` (`telemetry/skeleton-and-slice-1-config` or similar); never commit to `main`. Retro ships in this PR. Run `critter-skill-auditor` Phase 2 after implementation.

---

## Verify before wiring (jasperfx-source-verifier — local `C:\Code\JasperFx\` source)

Four API claims this slice depends on. Confirm each against local Marten/Wolverine source before the code commits to it. **Caveat carried from the pre-flight gRPC check: the local JasperFx checkout is stale (Wolverine V5.37.2; Marten may likewise trail 9.14). Note the verified-version alongside each answer; where local source can't reach the consumed 9.14/6.17 line, treat the API as unconfirmed-for-target and prefer the shape that works across lines.**

1. **Marten `IInitialData` as the ADR-011 seed vehicle** — confirm `IInitialData.Populate` (or the current shape) runs after schema creation under `RunJasperFxCommands`, and that `opts.InitialData.Add(...)` (or equivalent) is the registration. This is the Marten realization of ADR-011's "migration-time seed."
2. **Aggregate/view stream-version property type** — the proto pins `int64 throttle_policy_version`. Confirm how a Marten single-stream aggregation exposes the stream version as a **`long`** (e.g. `ILongVersioned.Version` vs `IRevisioned.Version` (int)) so `throttlePolicyVersion` is sourced correctly, not truncated.
3. **Self-aggregating projection registration** — confirm the registration for `TelemetryPolicy` as its own live-stream aggregation (`Projections.LiveStreamAggregation<TelemetryPolicy>()` or a separate `SingleStreamProjection`), and whether Marten 9's source-generator requires the type to be `partial` (per the standing v9 constraint on conventional projections).
4. **Wolverine.HTTP FluentValidation package + registration** — confirm the exact package (`WolverineFx.Http.FluentValidation` or sibling) and the middleware registration (`opts.UseFluentValidationProblemDetailMiddleware()` or current API) that wires nested `AbstractValidator<T>` at the boundary. This is the repo's first FluentValidation use.

---

## Deliverable plan

**Skeleton**
1. `src/CritterCab.Telemetry/CritterCab.Telemetry.csproj` — `Microsoft.NET.Sdk.Web`; mirror Dispatch's package refs (`WolverineFx`, `WolverineFx.RuntimeCompilation`, `WolverineFx.Http`, `WolverineFx.Marten`, OpenAPI, SwaggerUI) **plus** the FluentValidation package (gate 4).
2. `src/CritterCab.Telemetry/Program.cs` — composition root mirroring Dispatch: Marten (`crittercab_telemetry`, `AddEventType<TelemetryPolicyConfigured>()`, the `TelemetryPolicy` projection, `IntegrateWithWolverine`, lightweight sessions), FluentValidation middleware, health checks, `AddWolverineHttp`, `UseWolverine(ServiceName = "Telemetry")`, `RunJasperFxCommands`, `public partial class Program`.
3. `src/CritterCab.Telemetry/appsettings.json`, `src/CritterCab.Telemetry/README.md` (what Telemetry owns; the stream-processing shape).
4. `CritterCab.slnx` — add both new projects. `apphost.cs` — add `crittercab_telemetry` Postgres database + the `telemetry` project (ports 5315/5316, `WaitFor` the db). **No Kafka in apphost this PR** (deferred to slice 3 / PR C — same precedent as the Dispatch skeleton deferring transport).
5. `tests/CritterCab.Telemetry.Tests/` — test project (Alba, xUnit, Shouldly, Testcontainers.PostgreSql) + `TelemetryTestFixture` (mirror `DispatchTestFixture`) + one smoke test over `/health`.

**Slice 1 (`TelemetryPolicy/` feature folder)**
6. `ConfigureTelemetryPolicy` command (`h3Resolution`, `heartbeatIntervalSeconds`, `minPublishIntervalSeconds`, `operatorId`, `reason`) with a **nested `AbstractValidator`**: all intervals positive; `heartbeatIntervalSeconds ≥ minPublishIntervalSeconds`; `h3Resolution` within H3's 0–15 sane bound.
7. `TelemetryPolicyConfigured` event (past-tense, positional record; full-replacement payload per ADR-011) + `AddEventType` registration.
8. `TelemetryPolicy` aggregate/view — static `Create(IEvent<TelemetryPolicyConfigured>)` + `Apply` (full replacement), carrying the three parameters + `throttlePolicyVersion` from the stream version (gate 2). `partial` if gate 3 requires it.
9. `ConfigureTelemetryPolicyEndpoint` (static `[WolverinePost]`) — appends `TelemetryPolicyConfigured` to the **well-known singleton stream** (a fixed deterministic stream id constant; document it). Full-replacement semantics.
10. Bootstrap seed via `IInitialData` (gate 1): load singleton stream state → if empty, append `TelemetryPolicyConfigured` with defaults `h3Resolution: 9`, `heartbeatIntervalSeconds: 30`, `minPublishIntervalSeconds: 5`, `operatorId: "system-bootstrap"`, `reason: "Initial deployment defaults"`. Re-running is a no-op.
11. Alba tests (mirror W006 §6.1 GWTs): **Bootstrap** (empty stream → migration seeds the defaults), **Reconfigure** (v1 → `ConfigureTelemetryPolicy` → v2, `throttlePolicyVersion` advances), **Reject** (`heartbeatIntervalSeconds: 0` → ProblemDetails, no event appended).

**Docs / ledger**
12. `docs/skills/DEBT.md` — register one row: codify the config-as-events bootstrap-seed pattern (Marten `IInitialData` idempotent guard) as a skill/skill-section, now that a reference implementation exists (ADR-011's deferred follow-up). Note the auditor-surfaced later-arc gaps (recurring-sweep handler, non-event-sourced document write path, Kafka publish-first) as retro observations — not registered this PR unless they block.
13. `docs/workshops/006-telemetry-event-model.md` `## Document History` — one line recording slice 1 realized in code (spec-delta closure).
14. `docs/prompts/README.md` — Implementations index entry for this prompt.
15. This prompt's retro at `docs/retrospectives/implementations/006-telemetry-skeleton-and-slice-1-config.md`.

---

## Out of scope

- **Slices 2–5** — gRPC `ReportLocations` ingest, `DriverLocationUpdated` → Kafka, `LastKnownPosition` store + eviction, and the Dispatch consumer view. These are the follow-on PRs below.
- **Any transport wiring** — no gRPC service, no Kafka producer/consumer, no Kafka resource in `apphost.cs`. Transport lands with the slice that first needs it (Dispatch-skeleton precedent).
- **The `NearbyAvailableDriversStub` replacement** in Dispatch (slice 5 / PR D).
- **Authoring the config-as-events migration-template skill** — DEBT row only; a later `tidy: skills` session codifies it from this reference impl.
- **H3 geospatial computation** — no cell math this PR (h3Resolution is stored as a policy int only; cell computation is slice 2).
- **CLAUDE.md status-line refresh** — the "first vertical slice / all other BCs pre-workshop" line goes stale once Telemetry has code; flag in the retro for a housekeeping pass, do not edit opportunistically here.
- **Firing any of W006 §11's three ADR candidates** — all later-arc (see Spec delta).

---

## Follow-on PR sequence (for arc context; not this session)

- **PR B** — Slice 4 (`LastKnownPosition` document + eviction sweep) + Slice 2 (gRPC `ReportLocations` client-streaming ingest, **hand-wired against `IMessageBus`** per the verified gRPC gap — through-5.37.2 confirmed, 6.17 unverified; hand-wiring works either way). Fires the windowed-client-streaming ADR/skill candidate; adds the Kafka Testcontainer.
- **PR C** — Slice 3 (`DriverLocationUpdated` → Kafka topic `telemetry.driver-location-updated`; publish-first, no outbox). First Kafka topic in code; fires the Kafka-topic-naming ADR candidate; adds Kafka to `apphost.cs`.
- **PR D** — Slice 5 (Dispatch consumer: replace `NearbyAvailableDriversStub` with the Kafka-fed `AvailableDriver` document view — **Kafka half of the join only**; the ASB availability half is an out-of-scope ADR-018 forward-constraint).
