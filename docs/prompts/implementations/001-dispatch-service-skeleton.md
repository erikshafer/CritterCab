# Prompt 001 — Bootstrap the Dispatch Service Skeleton

| Field | Value |
|---|---|
| **Status** | Complete (2026-05-07). Produced `src/CritterCab.Dispatch/`, `tests/CritterCab.Dispatch.Tests/`, `apphost.cs`. Solution builds, smoke test passes. |
| **Authored** | 2026-05-07 |
| **Target artifacts** | `src/CritterCab.Dispatch/`, `tests/CritterCab.Dispatch.Tests/`, `CritterCab.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `apphost.cs` |
| **Source-of-truth dependencies** | [`docs/skills/adding-a-service/SKILL.md`](../../skills/adding-a-service/SKILL.md), [`docs/skills/service-bootstrap/SKILL.md`](../../skills/service-bootstrap/SKILL.md), [`docs/skills/aspire/SKILL.md`](../../skills/aspire/SKILL.md), [`docs/skills/vertical-slice-organization/SKILL.md`](../../skills/vertical-slice-organization/SKILL.md) |
| **Workflow position** | First implementation session; produces the first runnable code in the repository; the "blueprint architecture" step from Dilger's SDD note |

---

## Framing — why this session exists

CritterCab has design artifacts (workshop, narratives, skills, ADRs) and proto contracts (Step D), but no runnable code. This session stands up Dispatch as a runnable but logic-free service — the canonical scaffold every future service will mirror. This is Dilger's "blueprint architecture" step: hand-build the template before turning slice-by-slice implementation loose.

---

## Goal

Produce a buildable, testable Dispatch service skeleton with solution infrastructure, Aspire AppHost, Marten registration, health checks, and a paired test project with one smoke test. No domain logic, no handlers, no slices.

---

## Orientation files (read in order)

1. **[`docs/skills/adding-a-service/SKILL.md`](../../skills/adding-a-service/SKILL.md)** — solution layout, project file baseline, database naming, Aspire registration, test project pairing.
2. **[`docs/skills/service-bootstrap/SKILL.md`](../../skills/service-bootstrap/SKILL.md)** — `Program.cs` composition root patterns.
3. **[`docs/skills/aspire/SKILL.md`](../../skills/aspire/SKILL.md)** — single-file `apphost.cs`, resource registration, connection-string injection.
4. **[`docs/skills/vertical-slice-organization/SKILL.md`](../../skills/vertical-slice-organization/SKILL.md)** — file organization within the service (no technical-layer folders).

---

## Deliverable plan

1. `Directory.Build.props` — shared compiler settings (C# 14, net10.0, nullable, implicit usings).
2. `Directory.Packages.props` — central package versions for all Wolverine, Marten, Aspire, and test packages.
3. `CritterCab.slnx` — solution file with src/ and tests/ folders.
4. `src/CritterCab.Dispatch/CritterCab.Dispatch.csproj` — Marten-based service project.
5. `src/CritterCab.Dispatch/Program.cs` — minimal composition root: Marten + Wolverine wiring, health checks, `RunJasperFxCommands`.
6. `src/CritterCab.Dispatch/appsettings.json` — minimal config.
7. `src/CritterCab.Dispatch/README.md` — what the service owns.
8. `tests/CritterCab.Dispatch.Tests/CritterCab.Dispatch.Tests.csproj` — test project with Alba + xUnit + Shouldly.
9. `tests/CritterCab.Dispatch.Tests/DispatchServiceSmokeTest.cs` — one Alba-based smoke test verifying health endpoint.
10. `apphost.cs` — single-file Aspire AppHost at repo root with Postgres + Dispatch service wiring.

---

## Out of scope

- Domain events, commands, handlers, aggregates — that's Step C (slice 5.1).
- gRPC service definitions — lands when the first gRPC handler is implemented.
- Transport configuration (ASB, Kafka) — lands when the first cross-service flow is implemented.
- CI/CD pipeline — future.
- Production deployment configuration — future.

---

## Retrospective

### What happened

All deliverables produced. The Dispatch service skeleton is buildable and testable. `dotnet build CritterCab.slnx` succeeds, `dotnet test` passes the Alba-based health-endpoint smoke test. The `apphost.cs` is authored but not yet runnable (requires `dotnet workload install aspire`).

### Infrastructure already in place

`Directory.Build.props`, `Directory.Packages.props`, and `CritterCab.slnx` already existed with version pins. Wolverine packages were updated from 5.32.0 to 5.38.0 (latest).

### Skill-file gaps surfaced

1. **`service-bootstrap` skill says `RunOaktonCommandsAsync(args)`.** The actual API in JasperFx 1.30+ / Wolverine 5.38 is `app.RunJasperFxCommands(args)` (returns `Task<int>`, lives in `JasperFx` namespace, not `Oakton`). The Oakton-named methods still exist as compatibility shims but the canonical method is now JasperFx-branded. The skill should be updated.

2. **`service-bootstrap` skill says `?? throw` for connection strings.** The `aspire` skill explicitly says this pattern breaks Alba test fixtures because the `ConfigureServices` override fires after `Program.cs` reads `IConfiguration`. The correct pattern is `if (!string.IsNullOrEmpty(...))` guard. The two skills contradict each other; the aspire skill is correct.

3. **`service-bootstrap` skill doesn't mention `AddHealthChecks()`.** `MapHealthChecks("/health")` requires `builder.Services.AddHealthChecks()` to be called first. When Aspire's `AddServiceDefaults()` is present it handles this, but without Aspire the explicit registration is needed.

### Decisions made during authoring

- **Wolverine packages updated to 5.38.0.** The `Directory.Packages.props` had 5.32.0; updated to latest since this is the first code session.
- **`apphost.cs` uses Aspire 13.3.0** (latest) rather than the 13.2.2 pinned in `Directory.Packages.props`. The skill documents 13.2.2 but 13.3.0 is current.
- **No `AddServiceDefaults()` in Program.cs yet.** Requires the Aspire workload and a ServiceDefaults project or package. Deferred until `aspire run` is first exercised.
- **No transport packages in csproj.** Only `WolverineFx`, `WolverineFx.Http`, and `WolverineFx.Marten`. ASB and Kafka references land when the first cross-service flow is implemented.

### What's next

Step C: Slice 5.1 implementation (`RideRequested`) — first real domain logic in the skeleton.
