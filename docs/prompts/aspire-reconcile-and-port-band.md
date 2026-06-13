# Prompt — Tidy: Aspire Reconcile to 13.4.3 and Local-Dev Port Band

| Field | Value |
|---|---|
| **Status** | Pending (sketched 2026-06-13; awaiting review before execution) |
| **Authored** | 2026-06-13 |
| **Target artifacts** | `apphost.cs`, `Directory.Packages.props`, `docs/skills/aspire/SKILL.md`, `docs/skills/adding-a-service/SKILL.md`, `README.md`, `CLAUDE.md`, `docs/prompts/README.md` (index), `docs/retrospectives/README.md` (index), `docs/retrospectives/aspire-reconcile-and-port-band.md` (new) |
| **Source-of-truth dependencies** | The working `apphost.cs` (canonical for the current AppHost shape); the cross-`C:\Code` port survey captured in this prompt (canonical for the collision map); the JasperFx Aspire docs (`/dotnet/docs-aspire`) for the endpoint-pinning API surface |
| **Workflow position** | First `tidy: aspire` session. Pure infrastructure/maintenance reconcile — no new architectural commitment beyond documenting an already-agreed local-dev port convention. One-off `tidy:` subject (joins the established area list only if a second `tidy: aspire` session lands). |

---

## Framing — why this session exists

A recent dependency bump (commit `2f83e66`) lifted the package line to the 2026 Critter Stack release (Wolverine 6.8, Marten 9-era) and added OpenAPI/SwaggerUI to Dispatch, but left the Aspire layer **drifted across three sources** and the local-dev ports **unpinned**:

- `apphost.cs` declares Aspire at `13.3.0` (inline `#:sdk` / `#:package`).
- `Directory.Packages.props` declares the four `Aspire.Hosting.*` packages at `13.4.3` — but a file-based AppHost bypasses central package management, so those entries are currently **inert**.
- `docs/skills/aspire/SKILL.md` documents `13.2.2` throughout.
- `README.md` / `CLAUDE.md` say `13.3`.

Separately, nothing pins ports: no `launchSettings.json`, no `WithHttpEndpoint` calls, no dashboard-port config. Every endpoint takes Aspire's random high-port assignment, which collides unpredictably with the other Critter-family projects developed in parallel on the same machine (`CritterBids`, `crittermart`, `mmo-reconnect`, retired `CritterSupply`).

This session reconciles Aspire to one version and pins a compact, collision-free local-dev port band for CritterCab, documenting the band as a durable convention in the Aspire skills.

### The port survey (captured here as the collision map of record)

Read-only survey across `C:\Code` on 2026-06-13. Library-source checkouts (`JasperFx/*`, `wolverine`, `marten`, `polecat`, `eventuous`) are out of scope — only the user's parallel-developed app projects collide in practice:

| Project | Style | Occupies |
|---|---|---|
| `CritterBids` | Aspire random scatter | dashboard 17019/15237, OTLP 21029/19240–19241, resource 22025/20263, Api 5180/7180 |
| `crittermart` | Tidy `*090` family | dashboard 17090/15090, OTLP 21090/19090, resource 22090/20090, services 5101–5104 |
| `mmo-reconnect` | Compact `52xx` block | api 5200, apphost 5210, OTLP 5211, resource 5212 |
| `CritterSupply` (retired) | Aspire random scatter | dashboard 17265/15144, OTLP 21250/19288, MCP 23042/18130, resource 22096/20027 |

`53xx` is verified clear across all four. Sequence rationale: `crittermart`=51xx, `mmo-reconnect`=52xx, `CritterCab`=53xx.

---

## Goal

Reconcile every Aspire version reference to `13.4.3`, pin CritterCab's local-dev endpoints to the `53xx` band, and document the band + per-service slot convention in the Aspire skills. Author a retrospective. No opportunistic edits.

---

## Spec delta

**None.** No canonical narrative or workshop is amended — this is an infrastructure/maintenance reconcile, not a domain-spec change. The durable artifact this session produces is a *convention* (the port band), documented in the Aspire skills, not a spec amendment. Fourth honest null-edge exercise of the spec-delta convention (prior: `housekeeping-delete-may-15-handoff`, `skills-tidy-ai-skills-sync`, `workshops/003`).

---

## The port band (target state)

```
CritterCab — band 5300–5399   (compact; mirrors mmo-reconnect's 52xx style)

AppHost / dashboard
  5300 / 5301   dashboard          https / http
  5302 / 5303   OTLP               https / http
  5304 / 5305   resource service   https / http
  5306 / 5307   MCP endpoint       https / http   (for `aspire agent init`)

Services — 5-port slots: slot (https), slot+1 (http), slot+2 (gRPC if dedicated)
  5310 5311 5312   Dispatch
  5315 5316 5317   (next service)
  ...              (5-apart → 16 slots; ample for the 6–8 target services)

Infra host ports (pinned for stable local connection strings)
  5390 Postgres   5391 SqlServer   5392 Kafka   5393 ASB emulator
```

**Convention to document:** each new service claims the next free `+5` slot from `5310`; ports are `slot` (https), `slot+1` (http), `slot+2` (reserved gRPC). gRPC normally rides the HTTPS endpoint via Kestrel HTTP/2 multiplexing, so `+2` is reserved, used only if a service needs a dedicated gRPC listener. Only Dispatch is pinned this session (it is the only service that exists); the convention covers the rest.

---

## Orientation files (read in order)

1. **This prompt** — the port band and survey are the spec.
2. **`apphost.cs`** — the AppHost being reconciled; current shape (Postgres `18-alpine` persistent, `crittercab_dispatch` db, Dispatch project).
3. **`Directory.Packages.props`** — the (inert) `Aspire.Hosting.*` entries; confirm they are already `13.4.3`.
4. **`docs/skills/aspire/SKILL.md`** — `13.2.2` references to bump; the home for the new port-allocation section; confirms the AppHost-at-repo-root stance.
5. **`docs/skills/adding-a-service/SKILL.md`** — carries the AppHost-location contradiction to fix (`src/AppHost/apphost.cs` → repo-root `apphost.cs`) and gains a reference to the slot convention.
6. **A sibling `launchSettings.json`** (e.g. `C:\Code\mmo-reconnect\src\MmoReconnect.AppHost\Properties\launchSettings.json`) — reference for the `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` / `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL` / `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL` env-var keys.

---

## Working pattern

- **One session, one PR.** Subject: `tidy: aspire — reconcile to 13.4.3 and pin local-dev port band`.
- **Verify the API surface against the working AppHost and the Aspire docs before editing.** Per-service endpoint pinning uses `WithHttpsEndpoint(port:)` / `WithHttpEndpoint(port:)` on the `AddProject<>` resource; the Postgres host port uses `WithHostPort(int)`. Confirm exact signatures rather than trusting memory.
- **Resolve the file-based-apphost dashboard-port mechanism empirically** before committing it (see Decisions to flag #1). The dashboard/OTLP/resource/MCP ports are env-var driven; the open question is the cleanest committable home for those env vars under a file-based AppHost.
- **No opportunistic edits.** The Wolverine/Marten/Polecat version drift in `README.md`/`CLAUDE.md` is explicitly deferred even though those files are edited here for the Aspire line — touch only Aspire-version content. Capture other surfaced issues in the retro as next-session inputs, not as in-flight fixes.
- **Retrospective committed in the same PR**, root-level (`docs/retrospectives/aspire-reconcile-and-port-band.md`) — multi-file, mixed-type session.
- **Commit/push only on the user's say-so.**

---

## Deliverable plan

1. **`apphost.cs`**
   - `#:sdk Aspire.AppHost.Sdk@13.3.0` → `@13.4.3`; `#:package Aspire.Hosting.PostgreSQL@13.3.0` → `@13.4.3`.
   - Pin Dispatch: `.WithHttpsEndpoint(port: 5310).WithHttpEndpoint(port: 5311)` on the `AddProject<Projects.CritterCab_Dispatch>("dispatch")` chain (consider `launchProfileName: null` since the service has no launch profile).
   - Pin Postgres host port: `.WithHostPort(5390)` on the `AddPostgres("postgres")` chain.
   - Configure dashboard `5300`/`5301`, OTLP `5302`/`5303`, resource service `5304`/`5305`, MCP `5306`/`5307` via the mechanism resolved in Decisions-to-flag #1.
2. **`Directory.Packages.props`** — confirm the four `Aspire.Hosting.*` entries are `13.4.3`; add a one-line comment noting they are reference versions for a future `.csproj` AppHost and are not consumed by the current file-based `apphost.cs` (which pins inline). Do not remove them.
3. **`docs/skills/aspire/SKILL.md`** — bump every `13.2.2` → `13.4.3` (SDK directive, package table, prose); add a new **## Port allocation** section with the `53xx` band table and the `+5` slot convention; confirm/keep the AppHost-at-repo-root statements.
4. **`docs/skills/adding-a-service/SKILL.md`** — fix the AppHost-location contradiction (`src/AppHost/apphost.cs` → repo-root `apphost.cs`) in the layout diagram, the naming table, and the Aspire-registration prose; add a one-line reference to the port-slot convention pointing at the `aspire` skill's new section.
5. **`README.md`** — Aspire `13.3` → `13.4.3` (badge/table); update the dashboard-URL example (`17068` → `5300`). Aspire-only; leave Wolverine/Marten/Polecat lines alone.
6. **`CLAUDE.md`** — Aspire `13.3` → `13.4.3` in the Technology Stack table. Aspire-only.
7. **`docs/prompts/README.md`** — add this prompt to the Multi-artifact (root) index.
8. **`docs/retrospectives/README.md`** — add the retro to the Multi-artifact (root) index.
9. **`docs/retrospectives/aspire-reconcile-and-port-band.md`** — new retro per the format in `docs/retrospectives/README.md`.

---

## Out of scope

- **Wolverine / Marten / Polecat version doc-drift** in `README.md`, `CLAUDE.md`, `docs/vision/README.md`. Deferred to a dedicated version-reconcile session even though some target files are edited here for Aspire — Aspire-version content only.
- **The `WolverineFx.Grpc` / `WolverineFx.Polecat` 5.38-vs-6.8 mismatch.** Needs a verification pass against the feed/local source; its own follow-up.
- **Building `CritterCab.ServiceDefaults`.** Designed in the `aspire-service-defaults` skill, not yet built; the user scoped it to a separate session.
- **The `"CritterBids API"` Swagger title bug** in `src/CritterCab.Dispatch/Program.cs`. A one-line tidy for a follow-up.
- **Adding future infra resources** (SqlServer, Kafka, ASB emulator) to `apphost.cs`. Each lands with its BC.
- **Regularizing the sibling repos** (`CritterBids` etc.) onto compact bands. Those are separate repos and separate concerns.

---

## Decisions to flag during the session

1. **File-based-apphost dashboard-port mechanism.** Sibling AppHosts set `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` / `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL` / `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL` (and the dashboard URL) in `Properties/launchSettings.json`. CritterCab's AppHost is file-based and has no such file. Verify whether `dotnet run apphost.cs` / `aspire run` honors a `Properties/launchSettings.json` adjacent to `apphost.cs`; if yes, add one pinned to `5300–5307`. If not, document the env-var fallback (a committed env file or shell export) and pin what reliably pins. Do not guess — verify, then commit the mechanism that works.
2. **Whether to set `launchProfileName: null`** on `AddProject<>` for Dispatch, given the service has no launch profile and we want the AppHost-declared ports to be authoritative.
3. **Comment vs. removal for the inert `Aspire.Hosting.*` entries** in `Directory.Packages.props`. Lean: keep + comment (they document intended versions and serve a future `.csproj` AppHost). Flag if the file-based model makes them actively misleading.

---

## What this prompt's retrospective should specifically capture

- Whether the file-based-apphost port mechanism (Decisions-to-flag #1) resolved to launchSettings, an env file, or something else — this is reusable knowledge for every future Critter file-based AppHost.
- Whether the `53xx` band held without surprises once `aspire run` came up (did any resource still grab a random port outside the band?).
- Whether the `+5` slot convention as documented is unambiguous enough for the next `adding-a-service` session to apply without re-deciding.
- The running list of deferred follow-ups (version doc-drift, Grpc/Polecat mismatch, ServiceDefaults, Swagger title) as explicit next-session inputs.
