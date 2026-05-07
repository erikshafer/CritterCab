---
name: cli-aspire
description: "Aspire 13.2 CLI surface for CritterCab — running, stopping, and inspecting the AppHost (`aspire run`, `aspire start`, `aspire stop`, `aspire ps`, `aspire describe --follow`); resource lifecycle commands (`aspire resource <name> start|stop|restart|rebuild`); CI/automation gates (`aspire wait`, `--non-interactive`, `--format json`, `--detach`); environment diagnostics (`aspire doctor`); parallel-work isolation (`--isolated` for worktrees and agent runs); package and project management (`aspire init`, `aspire add`, `aspire restore`, `aspire update`); secrets and certificates (`aspire secret`, `aspire certs`); telemetry export (`aspire export`); the unified `aspire.config.json` and `aspire config` family; and the `aspire agent init` / `aspire agent mcp` surface that wires Cab's AppHost into Claude Code via MCP. Use whenever the task involves operating, automating, or scripting against Cab's AppHost rather than authoring the AppHost itself."
cluster: infrastructure
tags: [aspire, cli, automation, ci, mcp, agent, secrets, certs, telemetry, isolation, detached, configuration]
---

# Aspire CLI

The Aspire CLI is the everyday entry point to Cab's AppHost. The `aspire` skill describes what `apphost.cs` declares; this skill describes how the CLI runs, inspects, isolates, automates, and connects agents to the running system. Cab is a Claude-driven, multi-service codebase — `cli-aspire` is the surface that makes both human-driven inner loops and Claude-driven agent loops viable.

Aspire 13.2 made the CLI **agent-native by design**. Every command supports non-interactive operation; every command that produces structured output supports `--format json`; resource lifecycle commands operate on running AppHosts without tearing them down. The same commands that a developer types in a terminal are what Claude Code or a GitHub Actions job invokes through STDIO — there is no separate "automation mode."

Two practical consequences worth internalizing up front:

- **Detached mode is first-class.** `aspire start` (or `aspire run --detach`) backgrounds the AppHost and returns control. `aspire ps`, `aspire describe`, `aspire wait`, and `aspire stop` operate against background AppHosts. This is the foundation for parallel work and CI/CD.
- **Isolation is a flag, not a fork.** `--isolated` runs the AppHost with randomized ports and isolated user secrets. Two Cab worktrees can run side-by-side; an agent can run a parallel AppHost without collision; CI can run multiple integration shards without contention. No special configuration required.

---

## When to apply this skill

Use this skill when:

- Operating Cab's AppHost from a terminal — running, stopping, restarting, inspecting.
- Authoring CI/CD scripts that interact with the AppHost (smoke tests, `aspire doctor` pre-checks, `aspire export` diagnostics capture).
- Setting up Claude Code or another agent to talk to the running AppHost via MCP.
- Diagnosing dev-time orchestration issues from the CLI rather than the dashboard.
- Managing secrets, certificates, or configuration for the AppHost.
- Adding integration packages or upgrading Aspire versions.
- Working in parallel git worktrees or running multiple AppHosts simultaneously.

Do NOT use this skill for:

- Authoring or modifying the AppHost itself (`apphost.cs`) — `aspire`.
- Service-side configuration consumption — `service-bootstrap`.
- Cab service CLI commands (`describe-routing`, `codegen-preview`, `db-apply`) — `cli-jasperfx` (next, Phase 2).
- Production deployment to Azure — out of scope for Aspire entirely.
- Integration test orchestration — `testing-integration`. The AppHost is not run in tests.

---

## Installation and updates

The Aspire CLI is a self-extracting bundle distributed independently of the .NET SDK. Install once per machine:

```bash
# macOS / Linux
curl -sSL https://aspire.dev/install.sh | bash

# Windows (PowerShell)
irm https://aspire.dev/install.ps1 | iex
```

After install, verify:

```bash
aspire --version
```

Cab targets Aspire 13.2.2; pin via `aspire.config.json` (covered below) so every developer's CLI matches.

To update the CLI in place:

```bash
aspire update --self
```

To update Aspire packages in `apphost.cs` and `Directory.Packages.props`:

```bash
aspire update
```

`aspire update` reads `Directory.Packages.props` and the `#:sdk`/`#:package` directives in `apphost.cs`, resolves the latest compatible versions, and applies them. Always inspect the diff before committing — it can move multiple packages at once and you want the bump to be intentional.

For Cab specifically, the upgrade flow is:

```bash
aspire update --self          # bump CLI
aspire update                 # bump packages and SDK directive
dotnet build                  # confirm clean compile
aspire run                    # smoke test
git diff apphost.cs Directory.Packages.props   # inspect
```

---

## The everyday inner loop

### `aspire run`

Starts the AppHost in the foreground. Provisions every container, starts every service, prints the dashboard URL with token, and streams console output until `Ctrl+C`.

```bash
aspire run
```

This is the default for active development — you want the dashboard URL visible, you want logs streaming to your terminal, and you stop the system by interrupting the foreground process.

Flags worth knowing:

- `--no-build` — skip the build step when artifacts are known good. Useful when iterating against an unchanged AppHost.
- `--isolated` — randomize ports and isolate secrets. See "Parallel work and isolation" below.
- `--format json` — emit structured startup data instead of human-readable output. Useful for editor integrations and CI.

### `aspire start` — detached mode

Backgrounds the AppHost and returns immediately. Equivalent to `aspire run --detach`:

```bash
aspire start                  # detached
aspire start --apphost ./apphost.cs   # explicit AppHost path
aspire start --isolated       # isolated background run
```

Detached mode is the right default when:

- An agent needs to operate the AppHost (Claude Code starts it, queries it, stops it).
- A CI pipeline runs the AppHost as a step rather than the entire job.
- You want to keep your terminal free while the AppHost runs.

### `aspire ps`

Lists running AppHosts:

```bash
aspire ps                     # human-readable table
aspire ps --resources         # include resource details
aspire ps --resources --format json   # for scripting
```

Useful when you've forgotten whether `aspire start` from earlier is still running, or when an agent needs to discover available AppHosts in a workspace.

### `aspire stop`

Stops a running AppHost:

```bash
aspire stop                   # stop the workspace AppHost
aspire stop --all             # stop every running AppHost on the machine
```

`aspire stop --all` is the safe escape hatch when worktrees, parallel runs, or interrupted CI jobs leave orphan AppHosts behind. Combined with `aspire ps` first, it's the standard "clean slate" reset.

---

## Resource control

When the AppHost is running, individual resources can be controlled without restarting the whole system. Aspire 13.2 reorganized these into a uniform `aspire resource <name> <command>` shape:

```bash
aspire resource trips restart      # restart the trips service
aspire resource trips stop         # stop without removing
aspire resource trips start        # start a stopped resource
aspire resource trips rebuild      # rebuild and restart (project resources only)
```

`rebuild` is the lever that matters most for Cab's iteration loop. When you change a Cab service's source, `aspire resource <name> rebuild` triggers a clean stop, build, and restart of just that service. The rest of the AppHost — Postgres, Kafka, other services — keeps running. This is dramatically faster than tearing down the AppHost and starting fresh.

The pre-13.2 names `resource-start`/`resource-stop`/`resource-restart` are gone; if you have script residue using them, migrate to the `aspire resource <name> <command>` form.

---

## Resource introspection

### `aspire describe`

Reports the running AppHost's resource graph and current state:

```bash
aspire describe                     # snapshot of all resources
aspire describe --follow            # stream state changes in real-time
aspire describe --format json       # structured snapshot
aspire describe --format json --follow   # NDJSON stream
```

This is the same data the dashboard surfaces, accessible from a terminal. The `--follow` mode underlies the VS Code Aspire panel and the Aspire MCP tools — when Claude Code asks "what's the state of trips?", it's effectively running this.

For Cab debugging, the typical flow is:

```bash
aspire start                        # background AppHost
aspire describe --follow            # in another terminal, watch state changes
# trigger the bug; observe the resource go to Error state in the stream
aspire resource trips restart       # rebuild and observe again
```

### `aspire wait`

Blocks until a resource reaches a target status, with a timeout:

```bash
aspire wait trips --status healthy --timeout 60
aspire wait postgres --status up --timeout 30
```

Status values are `healthy`, `up`, `down`. `wait` is the primitive that makes CI scripts sane:

```bash
# CI smoke test
aspire start --detach
aspire wait postgres --status healthy --timeout 60
aspire wait trips --status healthy --timeout 120
# now run integration smoke checks against the running AppHost
aspire stop
```

Without `wait`, CI scripts resort to `sleep 30` or polling loops — both flaky. With it, the script blocks exactly as long as needed and fails cleanly if the resource never becomes healthy.

---

## Diagnostics — `aspire doctor`

`aspire doctor` runs a comprehensive environment check:

```bash
aspire doctor
```

It validates:

- HTTPS development certificate status (and detects multiple competing certs).
- Container runtime availability (Docker / Podman) and version.
- .NET SDK installation and version.
- Container tunnel requirements for Docker Engine on Windows.
- WSL2 environment configuration on Windows.
- Agent configuration status — flags deprecated MCP settings carried over from older Aspire versions.

Output includes actionable recommendations when issues are detected. Run it as the first step when:

- A developer joins Cab and sets up their machine — verifies prerequisites are present.
- An agent is about to start a long-running task — confirms the environment is sane before committing to the work.
- An AppHost won't start and the error message is opaque — `doctor` often surfaces a missing cert or container runtime issue faster than reading logs.

In CI, `aspire doctor --format json` produces structured output you can fail the job on if any check returns a hard error.

---

## Parallel work and isolation — `--isolated`

The `--isolated` flag runs an AppHost with:

- Randomized port assignments (no collisions with other AppHosts).
- Isolated user secrets (the same `aspire secret` keys map to a per-instance store).
- Independent dashboard URL.

```bash
aspire run --isolated
aspire start --isolated
```

This is the killer feature for two Cab use cases:

**Git worktrees.** When you're working on two Cab branches simultaneously — e.g., the main worktree on `main` and a feature worktree on `feature/dispatch-streaming` — both can run their AppHosts at the same time. Without `--isolated`, the second `aspire run` fails on port conflicts.

**Parallel agent runs.** When Claude Code is running an agent task that needs the AppHost up, and you also want to run the AppHost yourself, `aspire start --isolated` (for the agent) and `aspire run` (for you) coexist. The agent gets its own randomized port set; you get the canonical ports. Two distinct dashboards, two distinct lifecycles, no contention.

**CI integration test sharding** also benefits — multiple test shards can each run `aspire start --isolated` for their own AppHost without coordinating ports.

The trade-off: isolated AppHosts have non-canonical URLs, so any service-discovery or cross-service URL that's hardcoded outside the AppHost (e.g., a manual `curl` script) needs to read the dashboard or `aspire describe` output to find the right URL. Isolated mode is for parallelism, not for "I want the AppHost on port 5000 specifically."

---

## Project and package management

### `aspire init`

Adds Aspire to an existing codebase. Cab will run this once when the AppHost is first created:

```bash
cd /path/to/CritterCab
aspire init
```

The CLI prompts for AppHost language (C# single-file vs traditional `.csproj` vs TypeScript), scaffolds the `apphost.cs` (or alternative), and writes the initial `aspire.config.json`. Subsequent runs are unnecessary unless re-scaffolding.

### `aspire add`

Adds an Aspire integration package. Aspire 13.2's fuzzy search makes this discoverable:

```bash
aspire add                    # interactive picker
aspire add postgres           # direct (matches Aspire.Hosting.PostgreSQL)
aspire add azureservicebus    # matches Aspire.Hosting.AzureServiceBus
aspire add kafka              # matches Aspire.Hosting.Kafka
```

For Cab, the canonical adds when wiring a new infrastructure resource are:

```bash
# When ASB Emulator wiring lands
aspire add azureservicebus

# When the frontend lands and needs Vite hosting
aspire add nodejs
```

`aspire add` updates `Directory.Packages.props` (Cab uses Central Package Management) and emits a `#:package` directive into `apphost.cs`. Inspect the diff before committing.

### `aspire restore`

Restores integration packages — runs implicitly on `aspire run`, but useful manually after pulling changes that touched `apphost.cs`:

```bash
aspire restore
```

For TypeScript AppHosts (future), `aspire restore` also regenerates the SDK code in `.modules/`. Cab is C#-AppHost today; this matters when the polyglot frontend lands.

### `aspire new` (rarely useful for Cab)

Scaffolds an entirely new starter app. Useful for prototypes; not relevant for Cab since the AppHost lives in an established repo. Skip unless you're starting a brand new spike outside Cab.

### `aspire update`

Already covered in "Installation and updates" above. The two-step flow is `aspire update --self` then `aspire update`.

---

## Secrets and certificates

### `aspire secret`

Manages user secrets for AppHost parameters declared with `AddParameter(..., secret: true)`. Aspire 13.2 made these first-class CLI commands so you no longer need the .NET CLI's `dotnet user-secrets`:

```bash
aspire secret set ApiKey super-secret-value
aspire secret get ApiKey
aspire secret list                       # all secrets for this AppHost
aspire secret list --format json         # for scripting
aspire secret delete ApiKey
aspire secret locate                     # show the underlying secrets file path
```

For Cab, secrets are most relevant for:

- Third-party API keys consumed by services (e.g., Stripe sandbox keys for Payments BC dev).
- Demo OpenIddict client secrets.
- Anything Cab service code reads from `IConfiguration` that shouldn't sit in `appsettings.json`.

The secret values back AppHost `AddParameter("ApiKey", secret: true)` calls and propagate to consuming services via `WithReference(parameter)`. Secret rotation is a `set` away.

### `aspire certs`

Manages the HTTPS development certificate Aspire generates:

```bash
aspire certs trust            # trust the current dev cert
aspire certs clean            # remove stale dev certs
```

Run `aspire certs trust` once per machine. Run `aspire certs clean` when:

- `aspire doctor` reports multiple competing dev certs.
- HTTPS endpoints in the AppHost throw cert validation errors despite trust having been done previously.
- Switching between Aspire major versions and the cert format changed.

After `clean`, run `trust` again to regenerate.

---

## Telemetry export — `aspire export`

Captures a snapshot of telemetry and resource data from a running AppHost into a zip file:

```bash
aspire export --output ./artifacts/aspire-export.zip
aspire export trips --output ./artifacts/trips-export.zip   # scope to one resource
```

Three concrete uses:

- **Bug reports.** Reproduce the bug, run `aspire export`, attach the zip. Includes structured logs, traces, and resource state at capture time. Massively reduces back-and-forth on "what was happening when it broke?"
- **CI diagnostics.** When a CI smoke test fails, capture an export before tearing down. Upload as a build artifact for later inspection.
- **Dashboard import.** The Aspire dashboard supports importing exported zips, so a teammate can replay your debug session in their own dashboard without rerunning the AppHost.

Pair `aspire export` with the dashboard's "Manage logs and telemetry" → Import flow when sharing diagnostic snapshots across the team.

---

## Configuration — `aspire.config.json`

Aspire 13.2 unified configuration into a single `aspire.config.json` at the repo root. It replaces the older `apphost.run.json` + `.aspire/settings.json` split. For Cab the file looks roughly like:

```json
{
  "appHost": {
    "path": "apphost.cs",
    "language": "csharp"
  },
  "sdk": {
    "version": "13.2.2"
  },
  "channel": "stable",
  "profiles": {
    "default": {
      "applicationUrl": "https://localhost:17000;http://localhost:15000"
    }
  }
}
```

Auto-migration handles existing projects: the first `aspire` command run against an old layout merges `.aspire/settings.json` and `apphost.run.json` into a new `aspire.config.json` and rebases relative paths.

The CLI exposes config management:

```bash
aspire config list                      # all settings, organized
aspire config list --all                # include feature flags
aspire config get appHost.path
aspire config set channel staging
```

Cab commits `aspire.config.json` to the repo so every developer's CLI sees the same SDK pin, channel, and launch profile. Per-developer overrides go to the user-level `globalsettings.json` (also `aspire.config.json`-shaped) outside the repo.

---

## Aspire agent — Claude Code integration

This is the most consequential CLI surface for Cab's Claude-driven workflow. Covered briefly in the `aspire` skill; here's the operational depth.

### `aspire agent init`

Renamed from `aspire mcp init` in Aspire 13.2. Detects supported agent environments (Claude Code, VS Code with GitHub Copilot, Cursor, OpenAI Codex, GitHub Copilot CLI) and writes the appropriate config files for each:

```bash
cd /path/to/CritterCab
aspire agent init
```

The CLI presents a two-step picker:

1. **Which agent environments to configure.** Multi-select; pick all that apply.
2. **Whether to install the Aspire skill file.** This is an Aspire-specific `SKILL.md` written into the agent's expected location (e.g., `.claude/skills/aspire/SKILL.md` for Claude Code). It teaches the agent how to use the Aspire CLI — distinct from Cab's own `docs/skills/` library.

Run once per machine when setting up a workspace. The generated configs use STDIO transport, launching `aspire agent mcp` as a subprocess when the agent connects.

### `aspire agent mcp`

The underlying MCP server. You almost never invoke this manually — it's launched by the agent's MCP client per the config that `agent init` wrote:

```bash
aspire agent mcp              # starts the MCP server (STDIO transport)
```

When connected to a running AppHost, the MCP server exposes:

- **Resource queries** — list resources, get state, get endpoints, get health.
- **Log access** — recent structured logs per resource, filterable by level.
- **Trace inspection** — distributed traces across services.
- **Integration discovery** — `list_integrations` and `get_integration_docs` tools.
- **AppHost management** — `list_apphosts`, `select_apphost` for workspaces with multiple AppHosts.

For Claude Code's role in Cab specifically:

- **Debugging.** "Why did the trips service error?" → Claude queries logs and traces directly, no copy-paste.
- **Resource lifecycle.** Claude can `aspire resource trips restart` after changing source code without needing a manual restart from the developer.
- **Diagnostic capture.** Claude can `aspire export` before suggesting a workaround, attaching the snapshot for later inspection.

### `aspire agent` and skill files

In addition to MCP, `aspire agent init` may install Aspire-specific skill files (`SKILL.md` and similar) into the agent's expected paths. These are **not** the same as Cab's `docs/skills/` library. Cab's skills follow `agentskills.io` conventions and live under `docs/skills/`; Aspire's skill files are scoped to teaching agents how to use the Aspire CLI itself. Both are useful and complementary; don't expect overlap.

---

## Documentation commands — `aspire docs`

Surfaces aspire.dev documentation from the terminal. Aspire 13.2 added these primarily for agent consumption (Claude Code can `aspire docs search "kafka"` rather than scraping the web):

```bash
aspire docs list                              # all available pages
aspire docs search "azure service bus"        # search
aspire docs search "kafka" --limit 5
aspire docs get redis-integration             # full page by slug
aspire docs get redis-integration --section "Add Redis resource"
aspire docs list --format json                # structured for scripting
```

Aspire's own note: "These commands are intended for use by AI agents; although developers can use these directly, they aren't ergonomic for manual use and the website is still the best place to access documentation." For human reading, stick to https://aspire.dev/docs/. For Claude Code research mid-task, these are the right tool.

---

## CI/CD usage patterns

Aspire 13.2's CLI is built for non-interactive CI from the ground up. Three flags carry most of the weight:

### `--non-interactive`

Suppresses all prompts. Required for any CLI invocation in a script:

```bash
aspire add azureservicebus --non-interactive
aspire update --non-interactive
```

If a command would normally prompt (e.g., agent picker for `aspire agent init`), `--non-interactive` either uses defaults or fails fast — never hangs the pipeline.

### `--format json`

Routes structured output to stdout, status messages to stderr. Use when piping into `jq`, building editor integrations, or collecting CI metrics:

```bash
aspire ps --format json | jq '.apphosts[].state'
aspire describe --format json > apphost-state.json
aspire wait trips --status healthy --timeout 60 --format json
```

The stdout/stderr separation is intentional: any non-JSON noise is on stderr, so `jq` and friends never break on unexpected output.

### `--detach`

Already covered, but worth restating in CI context: `aspire run --detach` (or its alias `aspire start`) is the only way to use the AppHost as a CI step rather than the entire job. The flow:

```yaml
# GitHub Actions snippet (illustrative; adapt to actual CI choice)
- name: Start Aspire AppHost
  run: aspire start --non-interactive --format json

- name: Wait for services
  run: |
    aspire wait postgres --status healthy --timeout 60
    aspire wait trips --status healthy --timeout 120

- name: Run smoke tests
  run: ./scripts/smoke.sh

- name: Capture diagnostics on failure
  if: failure()
  run: aspire export --output ./artifacts/aspire-failure.zip

- name: Stop AppHost
  if: always()
  run: aspire stop --all
```

Cab's CI hasn't been authored yet, but this is the canonical shape when it lands.

### Combine flags freely

Most commands accept multiple flags:

```bash
aspire start --detach --isolated --non-interactive --format json
aspire ps --resources --format json --non-interactive
```

`--non-interactive` is safe to add unconditionally; use it as a default for scripts even when no prompt is currently expected. Future Aspire versions may add prompts to commands that today don't have any, and the flag protects against silent CI hangs.

---

## Common pitfalls

- **`aspire run` blocks the terminal but the dashboard URL has rotated tokens.** The token in the printed URL changes per run. Bookmark the host (`localhost:17213`) only; refresh the URL each session.
- **Forgetting `aspire stop --all` after parallel runs.** Orphan AppHosts from interrupted CI jobs or worktree experiments hold ports. `aspire ps` lists them; `aspire stop --all` clears them.
- **Using `--isolated` when you needed canonical ports.** Isolated mode randomizes ports — any external script or browser bookmark expecting `localhost:5000` will miss. Use `aspire describe --format json` to discover the actual URL when in isolated mode.
- **Running `aspire add` and committing without inspecting the diff.** It modifies `Directory.Packages.props`, `apphost.cs`, and (for TypeScript) `.modules/`. Treat as a change like any other — review before commit.
- **Running `aspire update` without bumping `--self` first.** The CLI version and the AppHost SDK version can drift. The convention is `aspire update --self` first, then `aspire update`.
- **Using `aspire mcp init` instead of `aspire agent init`.** The `mcp` command was renamed in 13.2. Old docs and tutorials still reference `aspire mcp init`; the new form is `aspire agent init`. The old commands may still work as aliases for now, but the canonical form is `agent`.
- **Skipping `aspire doctor` when something feels off.** It catches surprisingly many issues — stale certs, missing container runtime, deprecated agent config — that produce confusing errors elsewhere. First step in diagnosis.
- **Treating `aspire docs` as documentation for humans.** It's an agent-facing surface. The website is the right place for browsing and reading; `aspire docs` is for programmatic retrieval.
- **Forgetting `--non-interactive` in CI scripts.** A new prompt added in a future Aspire release silently hangs the pipeline. Belt-and-suspenders: include the flag even when no prompt is currently expected.
- **Trusting `aspire certs trust` to fix all HTTPS issues.** When `clean` and `trust` don't resolve a cert problem, the issue is usually elsewhere — competing system certs from another tool, or platform-specific trust store quirks. `aspire doctor` will often surface the real cause.
- **Running `aspire init` inside an existing `apphost.cs` repo.** It scaffolds a new AppHost; in an established Cab checkout, this overwrites or conflicts with what's there. Run only when bootstrapping the AppHost for the first time.
- **Confusing `aspire run --no-build` with `aspire restore`.** `--no-build` skips the dotnet build step assuming artifacts are current; `aspire restore` re-fetches packages. They solve different problems — use `restore` after pulling changes, `--no-build` to iterate fast against unchanged code.

---

## See also

**Upstream** — load these first:

- `aspire` — what the AppHost declares; the `apphost.cs` shape this CLI operates against. Read first if you're new to Cab's Aspire setup.

**Sibling skills:**

- `service-bootstrap` — the consuming side of Aspire's connection-string injection; how Cab service `Program.cs` files read what the CLI made available.
- `testing-integration` — Testcontainers-based test fixtures; the parallel infrastructure path used in tests rather than Aspire. Tests never invoke this CLI.

**Downstream:**

- `cli-jasperfx` (next, Phase 2) — Cab service CLI commands (`describe-routing`, `codegen-preview`, `db-apply`); often run via `dotnet run --project ... -- <command>` against an Aspire-orchestrated host during dev debugging.
- `wolverine-azure-service-bus` (Phase 3) — when Cab adds ASB to the AppHost, `aspire add azureservicebus` is the entry point covered here; the package wiring lives in that skill.
- `wolverine-kafka` (Phase 3) — same shape for Kafka, already committed but service-side wiring deferred.
- `wolverine-grpc-handlers` (Phase 3) — uses Aspire service discovery; `aspire describe` is the diagnostic tool when gRPC routing misfires.
- `observability-tracing` (Phase 3) — the OTLP endpoint Aspire surfaces; `aspire export` captures traces for later inspection.

**External:**

- ai-skills — generic Critter Stack CLI skills if/when JasperFx publishes any. Complements this skill.
- All ai-skills installed via `npx skills add` (license required).
- [Aspire CLI command reference](https://aspire.dev/reference/cli/commands/) — every command, every flag, every version.
- [What's new in Aspire 13.2 — CLI section](https://aspire.dev/whats-new/aspire-13-2/#️-cli-enhancements) — the full enumeration of 13.2 CLI additions.
- [Use AI coding agents](https://aspire.dev/get-started/ai-coding-agents/) — the canonical guide for `aspire agent init` and the broader agent integration story.
- [Aspire MCP server](https://aspire.dev/get-started/aspire-mcp-server/) — MCP tools, security model, and what gets exposed to agents.
- [Install the Aspire CLI](https://aspire.dev/get-started/install-cli/) — installation paths, channels (stable/staging/dev), and platform-specific notes.
- [`aspire run` reference](https://aspire.dev/reference/cli/commands/aspire-run/), [`aspire start`](https://aspire.dev/reference/cli/commands/aspire-stop/), [`aspire describe`](https://aspire.dev/reference/cli/commands/aspire-describe/), [`aspire wait`](https://aspire.dev/reference/cli/commands/), [`aspire doctor`](https://aspire.dev/reference/cli/commands/), [`aspire secret`](https://aspire.dev/reference/cli/commands/aspire-secret/), [`aspire agent`](https://aspire.dev/reference/cli/commands/aspire-agent/), [`aspire agent init`](https://aspire.dev/reference/cli/commands/aspire-agent-init/), [`aspire docs`](https://aspire.dev/reference/cli/commands/aspire-docs/), [`aspire config`](https://aspire.dev/reference/cli/commands/aspire-config/), [`aspire update`](https://aspire.dev/reference/cli/commands/aspire-update/) — per-command reference pages.
