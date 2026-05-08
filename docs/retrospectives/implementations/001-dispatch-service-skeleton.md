# Retrospective — Bootstrap the Dispatch Service Skeleton

## Metadata

- **Triggering prompt:** [`docs/prompts/implementations/001-dispatch-service-skeleton.md`](../../prompts/implementations/001-dispatch-service-skeleton.md)
- **Status:** Complete
- **Date authored:** 2026-05-07
- **Output artifacts:**
  - `src/CritterCab.Dispatch/` — Wolverine + Marten service project
  - `src/CritterCab.Dispatch/Program.cs` — minimal composition root
  - `tests/CritterCab.Dispatch.Tests/` — test project with Alba + xUnit + Shouldly
  - `apphost.cs` — single-file Aspire AppHost
  - `Directory.Build.props`, `Directory.Packages.props`, `CritterCab.slnx` — solution infrastructure (already existed; updated)
- **Outcome:** Dispatch service skeleton buildable and testable. `dotnet build` succeeds, `dotnet test` passes the Alba-based health-endpoint smoke test. First runnable code in the repository.

---

## Framing

CritterCab had design artifacts (workshop, narratives, skills, ADRs) and proto contracts, but no runnable code. This session stood up Dispatch as a runnable but logic-free service — the canonical scaffold every future service will mirror. This is Dilger's "blueprint architecture" step: hand-build the template before turning slice-by-slice implementation loose.

---

## Outcome summary

All deliverables produced. The Dispatch service skeleton is buildable and testable. The `apphost.cs` is authored but not yet runnable (requires `dotnet workload install aspire`).

---

## What worked

- **Infrastructure already in place.** `Directory.Build.props`, `Directory.Packages.props`, and `CritterCab.slnx` already existed with version pins. The session updated rather than created.

---

## What was harder than expected

- **Skill-file contradictions.** The `service-bootstrap` and `aspire` skills contradicted each other on connection-string handling (`?? throw` vs `if (!string.IsNullOrEmpty(...))` guard). The `aspire` skill was correct — the `?? throw` pattern breaks Alba test fixtures.

---

## Skill-file gaps surfaced

1. **`service-bootstrap` skill says `RunOaktonCommandsAsync(args)`.** The actual API in JasperFx 1.30+ / Wolverine 5.38 is `app.RunJasperFxCommands(args)`. The Oakton-named methods still exist as compatibility shims but the canonical method is now JasperFx-branded.
2. **`service-bootstrap` skill says `?? throw` for connection strings.** The `aspire` skill explicitly says this pattern breaks Alba test fixtures because the `ConfigureServices` override fires after `Program.cs` reads `IConfiguration`. The correct pattern is `if (!string.IsNullOrEmpty(...))` guard. The two skills contradict each other; the aspire skill is correct.
3. **`service-bootstrap` skill doesn't mention `AddHealthChecks()`.** `MapHealthChecks("/health")` requires `builder.Services.AddHealthChecks()` to be called first. When Aspire's `AddServiceDefaults()` is present it handles this, but without Aspire the explicit registration is needed.

---

## Decisions made during authoring

- **Wolverine packages updated to 5.38.0.** The `Directory.Packages.props` had 5.32.0; updated to latest since this is the first code session.
- **`apphost.cs` uses Aspire 13.3.0** rather than the 13.2.2 pinned in `Directory.Packages.props`. The skill documents 13.2.2 but 13.3.0 is current.
- **No `AddServiceDefaults()` in Program.cs yet.** Requires the Aspire workload and a ServiceDefaults project or package. Deferred until `aspire run` is first exercised.
- **No transport packages in csproj.** Only `WolverineFx`, `WolverineFx.Http`, and `WolverineFx.Marten`. ASB and Kafka references land when the first cross-service flow is implemented.

---

## Outstanding items / next-session inputs

- Step C: Slice 5.1 implementation (`RideRequested`) — first real domain logic in the skeleton.
