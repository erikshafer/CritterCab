# Retrospective — Tidy: Aspire Reconcile to 13.4.3 and Local-Dev Port Band

## Metadata

- **Triggering prompt:** [`docs/prompts/aspire-reconcile-and-port-band.md`](../prompts/aspire-reconcile-and-port-band.md)
- **Status:** Complete
- **Date authored:** 2026-06-13
- **Output artifacts:**
  - `apphost.cs` — Aspire directives `13.3.0` → `13.4.3`; **new** `#:property ManagePackageVersionsCentrally=false` (CPM opt-out); Postgres `.WithHostPort(5390)`; Dispatch pinned `5310`/`5311` with `launchProfileName: null`
  - `Properties/launchSettings.json` (**new**) — AppHost dashboard/OTLP/resource/MCP endpoints pinned to `5300–5307`
  - `Directory.Packages.props` — clarifying comment on the Aspire mirror entries (now `13.4.3`)
  - `docs/skills/aspire/SKILL.md` — version-of-record `13.2.2` → `13.4.3`; corrected the "Directory.Packages.props pins the AppHost versions" claim; **new ## Port allocation section**; CPM-opt-out directive + pitfall; dashboard determinism note
  - `docs/skills/adding-a-service/SKILL.md` — fixed the AppHost-location contradiction (`src/AppHost/apphost.cs` → repo-root `apphost.cs`); slot-convention references; conceptual sample updated
  - `README.md` — Aspire `13.3` → `13.4.3`; dashboard URL `17068` → `5300`; `Properties/` in the structure map
  - `CLAUDE.md` — Aspire `13.3` → `13.4.3`
  - `docs/prompts/README.md`, `docs/retrospectives/README.md` — index entries
- **Outcome:** Aspire reconciled to one version across code and docs; CritterCab's `53xx` local-dev port band pinned and documented as a durable convention. Surfaced and fixed a latent build break: the file-based AppHost did not restore under the repo's Central Package Management.

---

## Framing

A recent dependency bump (commit `2f83e66`) moved the package line to the 2026 Critter Stack release and added OpenAPI/SwaggerUI to Dispatch, but left Aspire drifted across `apphost.cs` (13.3.0), `Directory.Packages.props` (13.4.3), the `aspire` skill (13.2.2), and README/CLAUDE (13.3), with no port pinning anywhere. This session reconciled the version and pinned a compact, collision-free port band so CritterCab can run alongside `crittermart`, `mmo-reconnect`, and `CritterBids` on one machine. First `tidy: aspire` session.

---

## Outcome summary

| Area | Change |
|---|---|
| Version reconcile | Every Aspire **version-of-record** → `13.4.3` (directives, skill, README, CLAUDE). Historical "changed in 13.2" / "added in 13.2" notes deliberately left intact. |
| Port band | `53xx` pinned: dashboard/OTLP/resource/MCP `5300–5307` (launchSettings), Dispatch `5310`/`5311` + Postgres `5390` (apphost.cs). Documented with a cross-project band registry and the `+5` slot convention. |
| Build fix | `#:property ManagePackageVersionsCentrally=false` added — the AppHost did not restore under CPM before this. |
| Doc correctness | Corrected the skill's claim that `Directory.Packages.props` pins the AppHost's Aspire versions; fixed the AppHost-location contradiction between two skills. |

---

## What worked

- **Verifying the mechanism empirically instead of asserting it.** The prompt flagged "does a file-based apphost honor an adjacent `Properties/launchSettings.json`?" as a session-time decision. A 6-line throwaway file-based program proved it does — `dotnet run echoenv.cs` applied the first profile by default and set both the env var and `ASPNETCORE_URLS` from `applicationUrl`, with no `--launch-profile`. That turned an assumption into a fact and is reusable for every future Critter file-based AppHost.
- **`dotnet build apphost.cs` as the compile-verification step.** Building the file-based AppHost (no containers) both confirmed the endpoint API surface (`WithHostPort`, `WithHttpsEndpoint`, `WithHttpEndpoint` all exist on the 13.4.3 resource types) **and** surfaced the CPM break (NU1008/NU1009) that a docs-only pass would have missed entirely.
- **The cross-`C:\Code` survey before choosing a band.** Mapping what `crittermart` (`51xx`), `mmo-reconnect` (`52xx`), and `CritterBids` (scatter) already occupy made `53xx` a verified-clear, sequence-coherent choice rather than a guess — and the survey itself is now captured in the prompt as the collision map of record.

---

## What was harder than expected

- **The prompt's premise was wrong, and the session proved it.** The prompt (and the inventory that preceded it) stated the four `Aspire.Hosting.*` entries in `Directory.Packages.props` were "inert" because "a file-based AppHost bypasses central package management." That is false. `dotnet run apphost.cs` generates a synthetic `apphost.csproj` **at the repo root**, which inherits `Directory.Packages.props` and therefore enables CPM for the AppHost. The inline `#:package ...@version` directives then throw NU1008, and the SDK's implicit `Aspire.Hosting.AppHost` reference throws NU1009 — the AppHost fails to restore. The recent CPM work had silently broken `dotnet run apphost.cs`. Per the prompts-README convention the prompt was **not** rewritten after authoring; this retro carries the correction. The fix (`#:property ManagePackageVersionsCentrally=false`) keeps the file-based AppHost self-contained and is verified by a clean `dotnet build apphost.cs`.
- **Surgical version bumping.** A blanket `13.2.2 → 13.4.3` would have rewritten accurate version-history into fiction (the service-discovery key-format change, the MCP server, the TypeScript-AppHost preview all genuinely landed in 13.2). The bump had to distinguish version-of-record from historical-fact; the rule applied was "replace the `13.2.2` patch-pinned mentions and version-of-record prose; leave 'changed/added in 13.2' notes."

---

## Methodology refinements that emerged

- **File-based program + repo-wide CPM is a standing trap.** Any `.cs` file run via `dotnet run` under a `Directory.Packages.props` with `ManagePackageVersionsCentrally=true` needs `#:property ManagePackageVersionsCentrally=false` (or no inline `#:package` versions). This is now documented as a pitfall in the `aspire` skill and applies to the polyglot/TypeScript and any future scratch-file AppHost variants too. Worth remembering across the Critter family, not just CritterCab.
- **Compile-verify infra changes even in a "tidy" session.** This tidy touched a `.cs` file that the doc-only framing treated as settled. The build step is what caught the latent break. A `tidy:` that touches buildable code should compile it, not just edit prose.
- **"Verify, don't assert" earns its keep on library mechanics.** Two of the session's load-bearing facts (launchSettings honored for file-based apps; the CPM conflict) were the opposite of, or absent from, prior assumptions. Both came from running the toolchain, not from memory or docs alone.

---

## Outstanding items / next-session inputs

Carried forward as explicit follow-up sessions (named out-of-scope in the prompt):

1. **Wolverine / Marten / Polecat version doc-drift.** README badge/table, `CLAUDE.md`, and `docs/vision/README.md` still cite Wolverine 5.32/5.39, Marten 8.35, Polecat 3.1 while packages are on the 2026 line (Wolverine 6.8). Aspire-only lines were reconciled here; the rest is a dedicated version-reconcile session.
2. **`WolverineFx.Grpc` / `WolverineFx.Polecat` pinned to 5.38.0** while core Wolverine is 6.8.0. Needs verification against the feed/local source — is 5.38 the latest published, or did the bump miss them? gRPC is the project's reason for being, so this is high-priority.
3. **Build `CritterCab.ServiceDefaults`.** Fully specified in the `aspire-service-defaults` skill, not yet built; Dispatch hand-rolls health checks and emits no telemetry. Scoped to its own session by the user.
4. **`"CritterBids API"` Swagger title bug** in `src/CritterCab.Dispatch/Program.cs:83` — copy-paste leak from the sibling repo. One-line tidy.
5. **`MessagePack 2.5.192` NU1903 high-severity advisory** (transitive, surfaced during `dotnet build`). Pre-existing; triage in a dependency-hygiene pass.
6. **`packages.jasperfx.net` returned 403** for `microsoft.extensions.diagnostics.healthchecks.abstractions` during restore (build fell back and succeeded). Likely a `nuget.config` feed-ordering/auth quirk worth checking.

**Verification note:** the AppHost was **compile-verified** (`dotnet build apphost.cs`) and the launchSettings mechanism was **empirically proven**, but a full `aspire run` (which boots the Postgres container via Docker) was **not** executed this session. Recommended final confirmation: run `aspire run` once and confirm the dashboard binds `https://localhost:5300` and Dispatch binds `5310`/`5311`.

---

## Spec delta — landed?

**Null delta, as named.** No canonical narrative or workshop is amended — this is infrastructure/maintenance. The durable artifact produced is a *convention* (the `53xx` port band + `+5` slot rule), documented in the `aspire` and `adding-a-service` skills, not a spec amendment. Fourth honest null-edge exercise of the spec-delta convention.

---

## Quantitative summary

- **Files modified:** 7 (`apphost.cs`, `Directory.Packages.props`, two skills, `README.md`, `CLAUDE.md`, plus the two index READMEs). **Files created:** 3 (`Properties/launchSettings.json`, this prompt, this retro).
- **Net new behavior:** AppHost restores again (was broken); dashboard + all endpoints deterministic within `5300–5399`.
- **Verification:** `dotnet build apphost.cs` clean (2 pre-existing warnings noted); launchSettings mechanism proven via throwaway probe; full `aspire run` deferred to a manual check.
- **Follow-ups queued:** 6 (see above).
