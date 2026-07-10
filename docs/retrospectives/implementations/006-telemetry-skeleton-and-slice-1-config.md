# Retrospective — Telemetry Service Skeleton + Slice 1 (`TelemetryPolicyConfigured`)

## Metadata

- **Triggering prompt:** [`docs/prompts/implementations/006-telemetry-skeleton-and-slice-1-config.md`](../../prompts/implementations/006-telemetry-skeleton-and-slice-1-config.md)
- **Status:** Complete
- **Date authored:** 2026-07-10
- **Output artifacts:**
  - `src/CritterCab.Telemetry/CritterCab.Telemetry.csproj` — `Microsoft.NET.Sdk.Web`, mirrors Dispatch + `WolverineFx.Http.FluentValidation`
  - `src/CritterCab.Telemetry/Program.cs` — composition root: Marten (`crittercab_telemetry`), `AddEventType<TelemetryPolicyConfigured>`, `LiveStreamAggregation<TelemetryPolicy>`, `.InitializeWith<TelemetryPolicyBootstrap>()`, FluentValidation middleware, `RunJasperFxCommands`
  - `src/CritterCab.Telemetry/{appsettings.json,README.md}`
  - `src/CritterCab.Telemetry/TelemetryPolicy/TelemetryPolicyConfigured.cs` — singleton config event (full-replacement)
  - `src/CritterCab.Telemetry/TelemetryPolicy/ConfigureTelemetryPolicy.cs` — command + nested `AbstractValidator` + `[WolverinePost]` endpoint + `TelemetryPolicyResponse`
  - `src/CritterCab.Telemetry/TelemetryPolicy/TelemetryPolicy.cs` — self-aggregating view; static `Create`/`Apply`; `long Version`
  - `src/CritterCab.Telemetry/TelemetryPolicy/TelemetryPolicyStream.cs` — well-known singleton stream id
  - `src/CritterCab.Telemetry/TelemetryPolicy/TelemetryPolicyBootstrap.cs` — `IInitialData` idempotent seed (ADR-011 Option A)
  - `tests/CritterCab.Telemetry.Tests/` — test project, `TelemetryServiceSmokeTest`, `TelemetryTestFixture` (Postgres + `ResetToSeedAsync`), `TelemetryPolicy/Slice1TelemetryPolicyTests.cs` (3 GWTs)
  - `apphost.cs` — Telemetry service + `crittercab_telemetry` DB (ports 5315/5316); **no Kafka** (deferred)
  - `CritterCab.slnx`, `Directory.Packages.props` (`WolverineFx.Http.FluentValidation` 6.17.0)
  - `docs/skills/DEBT.md` — config-as-events bootstrap-seed pattern row (ADR-011's deferred follow-up)
  - `docs/workshops/006-telemetry-event-model.md` `## Document History` — slice-1-realized entry (spec-delta closure step 4)
  - `docs/prompts/README.md` — Implementations index entry; `docs/prompts/implementations/006-...` Status Pending → Complete
  - This retro + `docs/retrospectives/README.md` index entry
- **Outcome:** Skeleton + slice 1 implemented end-to-end. `dotnet build CritterCab.slnx` green; smoke test green locally (DB-less). The 3 slice-1 Alba tests (Testcontainers-Postgres) were verified via **CI** — local Docker's container-start path was wedged for the whole session (`docker run` hung; `docker ps` responded), the environmental condition the handoff flagged. Full-suite count confirmed green on CI: **[CI: N/N tests green — fill from CI run].** Second service in the repo; opening PR of the W006 transport chain.

---

## Framing

First implementation session of the W006 Telemetry chain and CritterCab's first real transport build — though this opening PR lands **no transport**. The transport slices have a dependency spine (slice 2's gRPC ingest reads slice 1's `TelemetryPolicy` view and evaluates its trigger against slice 4's `LastKnownPosition`), so config-as-events is the dependency-correct first slice, and it is self-contained (Marten singleton + HTTP + Postgres). Scoped as **skeleton + first slice in one PR** (named cadence exception; mirrors the Dispatch skeleton + slice-5.1 precedent), with the PR-scope choice confirmed with the user up front against the alternatives (skeleton-only; skeleton + gRPC ingest).

---

## Outcome summary

`CritterCab.Telemetry` stands up as the second service. Slice 1 realizes the `TelemetryPolicy` config-as-events singleton: `ConfigureTelemetryPolicy` (boundary-validated) appends a full-replacement `TelemetryPolicyConfigured` to a well-known singleton stream; `TelemetryPolicy` is its own live-stream aggregation exposing `throttlePolicyVersion` as a `long` (the Marten stream version, matching the proto's `int64`); an `IInitialData` migration-time seed idempotently plants the documented defaults with the `system-bootstrap` audit marker. Two firsts in code: **first config-as-events instance** (ADR-011's third instance overall; Dispatch/Onboarding were design-only) and **first FluentValidation boundary validation**. Kafka is deliberately absent from `apphost.cs` — transport wires with the slice that needs it (slice 3), the same deferral the Dispatch skeleton made.

---

## What worked

- **Verify-before-wiring paid off exactly as intended.** Four `jasperfx-source-verifier` gates ran before any gate-dependent code committed; all four survived the build with a single fix. The only compile error was a missing `using Wolverine.Http.FluentValidation;` (gate 4's extension-method namespace) — caught in the first `dotnet build`, not at runtime.
- **The 6.17 gRPC re-verification corrected the handoff's premise.** The handoff asked to re-verify the client-streaming gap "against 6.17"; the verifier found the local `C:\Code\JasperFx\wolverine` checkout is stale at **V5.37.2** (no 6.1x/6.17 tag present), so a full-weight 6.17 answer was impossible locally. Verdict recorded honestly: hand-wire `ReportLocations` against `IMessageBus` — verified must-hand-wire *through 5.37.2*, 6.17 unverified; hand-wiring is safe either way. This is a **slice-2 (PR B) concern and did not touch this PR**, so it gated nothing here.
- **Splitting the build into gate-independent vs gate-dependent passes kept momentum during the verifier waits.** Solution/apphost/appsettings/README/test-harness/event-record/command+validator all landed while the verifier ran; only the aggregate, seed, endpoint, and `Program.cs` waited on the gates.
- **The singleton-isolation reset (`ResetToSeedAsync`) resolved a real hazard cleanly.** A config-as-events singleton means all slice-1 tests share one stream — order-dependent without isolation. Wiping event data and re-running the same `IInitialData` seed before each test gives per-test isolation and doubles as a live exercise of the seed itself.

---

## What was harder than expected

- **Local Docker was wedged the entire session** (container-`start` hung; `docker ps` fine) — the exact condition the handoff flagged. Restarting Docker Desktop would have disrupted the user's local WSL/containers without consent, so the session leaned on **CI as the test gate** (its Testcontainers works; it ran the full suite green for PR #41). Consequence: the 3 slice-1 integration tests could not be run locally; the build + DB-less smoke test were the local signals, CI the integration signal.
- **"Verify against 6.17" was not literally satisfiable** because the JasperFx source checkout trails the consumed package line by a major version. Named as a residual: a full-weight 6.17 gRPC verdict (and full-weight confirmation of gates 1–3 against 9.14, which were verified against slightly-older-but-stable Marten source) requires refreshing `C:\Code\JasperFx\*` to the consumed tags — a separate, user-owned action, not a CritterCab task.

---

## Methodology refinements

- **PR-scope for a multi-slice transport build is a durable decision worth an explicit up-front confirmation.** The full item-2 build is ~4 PRs; the handoff's "skeleton + first slice, then one slice per PR" plus the slice dependency graph made config-as-events the unambiguous first slice, but *how ambitious PR A is* genuinely forked (the user's "light up gRPC + Kafka" framing vs the one-slice-per-PR cadence). Confirming the scope before authoring the prompt avoided building the wrong-sized PR.
- **A stale JasperFx source checkout silently weakens `jasperfx-source-verifier`.** Two consecutive verifier runs this session hit the same wall (Wolverine 5.37.2 vs consumed 6.17). The verifier's honesty about it is what made the answers usable. Worth a standing note: verifier verdicts should always carry the checked-out version, and "verify against version X" tasks should confirm the local checkout can reach X first.

---

## Outstanding items / next-session inputs

- **DEBT registered:** config-as-events bootstrap-seed pattern (Marten `IInitialData` idempotent guard + seed payload) — ADR-011's explicitly-deferred skill, now groundable from this reference impl. New skill or `marten-wolverine-aggregates` extension is the tidy session's call.
- **Auditor-surfaced later-arc skill gaps (not registered this PR — they belong to the slices that hit them):** recurring/scheduled sweep-handler shape (slice 4 eviction), non-event-sourced document write path (slice 4 `LastKnownPosition`), Kafka publish-first/no-outbox convention (slice 3), windowed gRPC client-streaming discipline (slice 2). The `wolverine-grpc-bidirectional-handlers` skill's illustrative examples are also stale vs the shipped proto (fictional `PushTelemetry`/split-service names) — flag when slice 2 lands.
- **CLAUDE.md status line** ("first vertical slice … all other BCs pre-workshop") is now stale — Telemetry is a second service with code. Deferred to a `tidy: housekeeping` pass (not edited opportunistically here).
- **Pre-existing `"CritterCab Dispatch API"`/Swagger and NU1903 (`Microsoft.OpenApi` 2.0.0 vuln)** — unrelated, carried.
- **Next PR (B):** slice 4 (`LastKnownPosition` + eviction sweep) + slice 2 (gRPC ingest, hand-wired against `IMessageBus`); adds the Kafka Testcontainer. Then PR C (slice 3 Kafka publish; wires Kafka into apphost; fires the topic-naming ADR candidate) and PR D (slice 5 Dispatch consumer, Kafka half).
- **Full-weight 6.17 / 9.14 verification** owed if/when `C:\Code\JasperFx\*` is refreshed to the consumed tags (user-owned).

---

## Spec delta — landed?

- ✅ **`docs/workshops/006-telemetry-event-model.md` `## Document History` (2026-07-10 entry)** — records §6.1 designed → realized in code, naming the seed, the `throttlePolicyVersion`-as-long view, the two in-code firsts, and that none of §11's ADR candidates fired. Substantive delta as the prompt planned.
- ✅ **No ADR fired**, exactly as scoped — the three W006 §11 candidates are all later-arc (Kafka topic-naming → slice 3; stream-processing-4th-shape → best evidenced by slices 2–4; windowed client-streaming → slice 2). Named as deliberate deferrals, closure loop honest.
