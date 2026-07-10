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
  - `src/CritterCab.Telemetry/TelemetryPolicy/TelemetryPolicyBootstrap.cs` — `IInitialData` idempotent seed (ADR-011; Option-A/B-in-Marten reconciliation flagged — see "design meets code")
  - `tests/CritterCab.Telemetry.Tests/` — test project, `TelemetryServiceSmokeTest`, `TelemetryTestFixture` (Postgres + `ResetToSeedAsync`), `TelemetryPolicy/Slice1TelemetryPolicyTests.cs` (3 GWTs)
  - `apphost.cs` — Telemetry service + `crittercab_telemetry` DB (ports 5315/5316); **no Kafka** (deferred)
  - `CritterCab.slnx`, `Directory.Packages.props` (`WolverineFx.Http.FluentValidation` 6.17.0)
  - `docs/decisions/011-configuration-as-events-bootstrap.md` — **2026-07-10 Amendment** (Marten `IInitialData` realization of Option A; LWW write for config singletons) — added mid-session per user direction after the Phase-2 audit
  - `docs/skills/DEBT.md` — config-as-events bootstrap-seed pattern row (two design questions resolved by the ADR-011 amendment; skill codification remains open) + Wolverine.HTTP FluentValidation-wiring row
  - `docs/workshops/006-telemetry-event-model.md` `## Document History` — slice-1-realized entry (spec-delta closure step 4)
  - `docs/prompts/README.md` — Implementations index entry; `docs/prompts/implementations/006-...` Status Pending → Complete
  - This retro + `docs/retrospectives/README.md` index entry
- **Outcome:** Skeleton + slice 1 implemented end-to-end. `dotnet build CritterCab.slnx` green; smoke test green locally (DB-less). The Marten-backed slice-1 Alba tests (Testcontainers-Postgres) were verified via **CI** — local Docker's container-start path was wedged for the whole session (`docker run` hung; `docker ps` responded), the environmental condition the handoff flagged. **Final CI (run 29119259592): 15/15 green — CritterCab.Telemetry 4/4 (smoke + 3 slice-1 GWTs) + CritterCab.Dispatch 11/11 (no regression).** Two CI iterations: the first push failed the reject GWT (200 instead of 400 — validators unregistered; see below); the fix (`opts.UseFluentValidation()`) landed it. Second service in the repo; opening PR of the W006 transport chain, PR [#42](https://github.com/erikshafer/CritterCab/pull/42).

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

- **CI caught a bug local tooling structurally could not.** The first CI run failed the reject GWT (200, not 400): `UseFluentValidationProblemDetailMiddleware()` resolves `IValidator<T>` from DI, but nothing registered the validators — the HTTP middleware alone is only half the wiring. `dotnet build` and the DB-less smoke test both passed; only the Postgres-backed reject test exercised the validation path, and local Docker was wedged. The fix is `opts.UseFluentValidation()` in `UseWolverine` (the `WolverineFx.FluentValidation` assembly-scan that registers validators — the pairing the Wolverine HTTP sample uses). **Lesson: for a Docker-wedged session, leaning on CI is not just acceptable, it's load-bearing — the smoke test's green gave false confidence about a validation path it never touches.** As the repo's first FluentValidation use, this two-call wiring (`UseFluentValidation` + `UseFluentValidationProblemDetailMiddleware`) is undocumented in any skill → registered in `DEBT.md`.

### Design meets code — the Phase-2 audit's real finding (resolved in-session via ADR-011 amendment)

The critter-skill-auditor Phase 2 pass caught a genuine design-vs-implementation gap the build and smoke test could not: **W006 §6.1/§9 lock the seed as "ADR-011 Option A (migration-time seed)", but the idiomatic Marten realization (`IInitialData` via `.InitializeWith<T>()`) runs at *host startup* — structurally ADR-011's Option B, the option the ADR rejected over a multi-instance seed race.** The reconciliation: `IInitialData` *is* Marten's seam for Option A when driven by the JasperFx `resources setup` deploy step, but it also self-seeds at every host start, so absent that deploy step it carries Option B's race. The race is benign here (idempotent guard + full-replacement make a double-seed converge) and irrelevant at single-instance MVP scale. ADR-011 was authored design-only, before any Marten implementation, and its A/B framing assumed a separate SQL-migration phase Marten does not have. **This wants an ADR-011 amendment describing the Marten realization — a durable decision deferred to the user, not resolved in this PR.** Artifacts were corrected to stop bare-claiming "Option A"; both this and the write-path concurrency question below are registered in `DEBT.md` as questions the future canonical-pattern skill must *resolve*, not just describe.

A second, softer Phase-2 finding: **`ConfigureTelemetryPolicyEndpoint` appends with no optimistic-concurrency check** (`session.Events.Append(id, event)`, no expected version), because `[WriteAggregate]` has no documented binding to a well-known *constant* `Guid` when the command carries no id field. Deliberate last-writer-wins is defensible for full-replacement config (the workshop itself says the singleton has "no state-transition invariant to defend beyond full-replacement"), but it deviates from ADR-011's generic "optimistic concurrency on the singleton stream."

**Resolution (user-directed mid-flight expansion):** on the user's explicit instruction, both questions were settled in this PR by amending ADR-011 (the [2026-07-10 Amendment](../../decisions/011-configuration-as-events-bootstrap.md#amendment--2026-07-10-marten-realization-via-iinitialdata)): `IInitialData` is recorded as the canonical Marten realization of Option A (deploy-time apply + idempotent host-start safety net; race mitigated by the guard + full-replacement), and last-writer-wins is recorded as the accepted config-singleton write semantic (no optimistic concurrency; manual constant-id append). This expanded the session beyond the prompt's "no ADR fires" plan — captured here per the workflow's mid-flight-expansion rule; the prompt is left as authored (historical record), not retroactively re-scoped. The `DEBT.md` config-as-events row now points at the amendment's answers, leaving only the skill codification open.

---

## Methodology refinements

- **PR-scope for a multi-slice transport build is a durable decision worth an explicit up-front confirmation.** The full item-2 build is ~4 PRs; the handoff's "skeleton + first slice, then one slice per PR" plus the slice dependency graph made config-as-events the unambiguous first slice, but *how ambitious PR A is* genuinely forked (the user's "light up gRPC + Kafka" framing vs the one-slice-per-PR cadence). Confirming the scope before authoring the prompt avoided building the wrong-sized PR.
- **A stale JasperFx source checkout silently weakens `jasperfx-source-verifier`.** Two consecutive verifier runs this session hit the same wall (Wolverine 5.37.2 vs consumed 6.17). The verifier's honesty about it is what made the answers usable. Worth a standing note: verifier verdicts should always carry the checked-out version, and "verify against version X" tasks should confirm the local checkout can reach X first.

---

## Outstanding items / next-session inputs

- **ADR-011 amendment — DONE this PR (user-directed).** Reconciled Option A/B for the Marten `IInitialData` realization and recorded LWW as the config-singleton write semantic. Both design questions closed.
- **DEBT (now unblocked):** config-as-events bootstrap-seed pattern (Marten `IInitialData` idempotent guard + seed payload + LWW write) — ADR-011's explicitly-deferred skill, groundable from this reference impl per the amendment's answers. New skill or `marten-wolverine-aggregates` extension is the tidy session's call.
- **Auditor-surfaced later-arc skill gaps (not registered this PR — they belong to the slices that hit them):** recurring/scheduled sweep-handler shape (slice 4 eviction), non-event-sourced document write path (slice 4 `LastKnownPosition`), Kafka publish-first/no-outbox convention (slice 3), windowed gRPC client-streaming discipline (slice 2). The `wolverine-grpc-bidirectional-handlers` skill's illustrative examples are also stale vs the shipped proto (fictional `PushTelemetry`/split-service names) — flag when slice 2 lands.
- **CLAUDE.md status line** ("first vertical slice … all other BCs pre-workshop") is now stale — Telemetry is a second service with code. Deferred to a `tidy: housekeeping` pass (not edited opportunistically here).
- **Pre-existing `"CritterCab Dispatch API"`/Swagger and NU1903 (`Microsoft.OpenApi` 2.0.0 vuln)** — unrelated, carried.
- **Next PR (B):** slice 4 (`LastKnownPosition` + eviction sweep) + slice 2 (gRPC ingest, hand-wired against `IMessageBus`); adds the Kafka Testcontainer. Then PR C (slice 3 Kafka publish; wires Kafka into apphost; fires the topic-naming ADR candidate) and PR D (slice 5 Dispatch consumer, Kafka half).
- **Full-weight 6.17 / 9.14 verification** owed if/when `C:\Code\JasperFx\*` is refreshed to the consumed tags (user-owned).

---

## Spec delta — landed?

- ✅ **`docs/workshops/006-telemetry-event-model.md` `## Document History` (2026-07-10 entry)** — records §6.1 designed → realized in code, naming the seed, the `throttlePolicyVersion`-as-long view, the two in-code firsts, and that none of §11's ADR candidates fired. Substantive delta as the prompt planned.
- ✅ **None of W006 §11's three ADR candidates fired**, as scoped — Kafka topic-naming → slice 3; stream-processing-4th-shape → best evidenced by slices 2–4; windowed client-streaming → slice 2. Deliberate deferrals.
- ➕ **ADR-011 was *amended* mid-session (unplanned, user-directed)** — distinct from the §11 candidates (a refinement of an existing decision, not a new one firing). The prompt planned "no ADR fires"; the Phase-2 audit surfaced a real A/B-for-Marten gap and the user directed resolving it in-PR. The [2026-07-10 Amendment](../../decisions/011-configuration-as-events-bootstrap.md#amendment--2026-07-10-marten-realization-via-iinitialdata) records `IInitialData` as the canonical Marten Option-A realization and LWW for config singletons. Honest expansion of the session's spec delta beyond the prompt.
