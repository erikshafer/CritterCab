---
name: cli-jasperfx
description: "JasperFx command-line surface inside Cab services — `dotnet run --project <service> -- <command>` against a service whose `Program.cs` ends with `RunJasperFxCommands(args)`. Covers general commands (`help`, `check-env`, `describe`), Weasel schema management (`db-apply`, `db-assert`, `db-patch`, `db-dump`, `db-list`), JasperFx resource lifecycle (`resources setup|teardown|clear|check|statistics|list`), code generation (`codegen preview|write|delete|test`), projection management (`projections list|rebuild|run`), Wolverine diagnostics (`wolverine-diagnostics codegen-preview|describe-routing` with `--handler`, `--route`, `--grpc`, `--all`), Wolverine capability export (`capabilities`), message storage (`storage counts|clear|rebuild|release|replay`, `clear-handled`), and Marten healing (`marten --advance|--correct|--reset`). Use whenever the task involves invoking a service's CLI surface — schema migrations, projection rebuilds, codegen previews, routing audits, message-store inspection, or dev-time diagnostics."
cluster: infrastructure
tags: [cli, jasperfx, codegen, schema, db-apply, weasel, projections, capabilities, wolverine-diagnostics, describe-routing, resources, storage, healing, ci]
---

# JasperFx CLI

Every Cab service exposes a CLI surface through JasperFx's command framework. When `Program.cs` ends with `app.RunJasperFxCommands(args)`, the service binary becomes both a runnable host and a CLI tool for schema management, code generation previews, routing audits, and runtime diagnostics. Run `dotnet run --project src/CritterCab.Trips -- describe-routing` and you get the full Trips service routing topology printed to the terminal — same binary, same bootstrap, different entry point.

This is the skill's center of gravity: **the CLI is the service**. There's no separate admin tool, no out-of-band schema migrator, no dedicated diagnostics binary. Every command runs the service's full `Program.cs` (or builds the host without starting it), which means every command sees the same connection strings, the same Marten/Polecat configuration, the same Wolverine handlers, and the same routing rules that the service runs in production.

The CLI surface comes from five overlapping contributors, each registering commands at startup:

- **JasperFx core** — general lifecycle: `help`, `run`, `check-env`, `describe`, `codegen`, `resources`.
- **JasperFx.Events** — projection management: `projections` (with `list`/`rebuild`/`run` actions).
- **Weasel** (used by Marten and Polecat) — schema migrations: `db-apply`, `db-assert`, `db-patch`, `db-dump`, `db-list`.
- **Wolverine** — diagnostics, capabilities, and message storage: `wolverine-diagnostics`, `capabilities`, `storage`, `clear-handled`.
- **Marten** — event store healing: `marten` (with `--advance`, `--correct`, `--reset` flags).

Cab services compose these together by referencing `WolverineFx.Marten` (or `WolverineFx.Polecat`) and adding `RunJasperFxCommands(args)` to `Program.cs`. The full surface is then available without per-command registration.

---

## When to apply this skill

Use this skill when:

- Applying a schema migration to a Cab service's database (`db-apply`).
- Verifying schema is in sync with code (`db-assert`).
- Inspecting Wolverine routing for a specific message or the whole topology (`wolverine-diagnostics describe-routing`).
- Previewing generated handler/HTTP/gRPC adapter code (`wolverine-diagnostics codegen-preview` or `codegen preview`).
- Pre-compiling generated code into the project for production (`codegen write`).
- Inspecting Wolverine's inbox/outbox state (`storage counts`).
- Replaying dead-lettered messages (`storage replay`).
- Rebuilding a Marten projection from CLI without booting the host (`projections rebuild`).
- Healing a stuck Marten event store (`marten --advance` / `--correct`).
- Exporting a Wolverine service's capabilities to JSON for contract tooling (`capabilities`).
- Setting up local databases without booting the full Aspire AppHost (`resources setup`).
- Running CLI commands as part of CI/CD pipelines (codegen verification, schema gating, routing audits).

Do NOT use this skill for:

- Operating Cab's Aspire AppHost — `cli-aspire`.
- Authoring the `Program.cs` registration — `service-bootstrap`.
- Service-side handler code, codegen attributes, or runtime configuration — the relevant Wolverine/Marten skills.
- Production deployment scripting — out of scope; this skill covers the CLI surface, not the deployment story around it.

---

## How invocation works

Cab service `Program.cs` files end with:

```csharp
return await app.RunJasperFxCommands(args);
```

When you invoke the service binary with arguments, JasperFx routes to the matching command instead of running the host. When you invoke without arguments (or with the default `run` command), it runs the host normally.

Every CLI invocation in this skill follows the shape:

```bash
dotnet run --project src/CritterCab.Trips --no-launch-profile -- <command> [args] [flags]
```

The pieces:

- `--project src/CritterCab.Trips` — which service's binary to run.
- `--no-launch-profile` — skip `launchSettings.json` so URLs and environment variables don't get applied (the CLI doesn't need them, and applied URLs can interfere with tooling).
- `--` — separator: everything after is passed to the application, not to `dotnet`.
- `<command> [args] [flags]` — the JasperFx command and its arguments.

For brevity the rest of this skill abbreviates this as:

```bash
dotnet run --project src/CritterCab.Trips -- <command>
```

When you're already in the service directory, `dotnet run -- <command>` works too. When the service is composed by Aspire, the Aspire-orchestrated host is separate from CLI invocations — the CLI builds its own host instance per command.

### Listing available commands

```bash
dotnet run --project src/CritterCab.Trips -- help
```

Lists every registered command with a one-line description. Use this whenever you're unsure what's available — particularly after pulling changes that might have added Wolverine extensions (gRPC, ASB, Kafka) which can register additional commands.

For details on one command:

```bash
dotnet run --project src/CritterCab.Trips -- help db-apply
dotnet run --project src/CritterCab.Trips -- help wolverine-diagnostics
```

---

## General lifecycle commands

### `check-env` — environment validation

Runs every registered environment check against the service. Marten and Wolverine both register checks for things like database connectivity, schema presence, required transport endpoints, and so on:

```bash
dotnet run --project src/CritterCab.Trips -- check-env
dotnet run --project src/CritterCab.Trips -- check-env --file ./artifacts/env-check.txt
```

Use as the first diagnostic when something feels off in a service's startup. The `--file` flag writes a structured report — useful in CI for failing builds on environment regressions.

### `describe` — service description

Writes a description of the running application: registered services, configuration, referenced assemblies, Marten stores, Wolverine handlers, and any custom `ISystemPart` describers contributed by extensions:

```bash
dotnet run --project src/CritterCab.Trips -- describe
dotnet run --project src/CritterCab.Trips -- describe --file ./artifacts/trips-description.txt
```

Cab uses this for two things in particular:

- **Onboarding documentation.** New contributors run `describe` against a service to get a complete picture of what's registered without reading every file.
- **CI artifact for change reviews.** Comparing `describe` output between branches surfaces changes to handler registration, transport routing, or store configuration that diff-only review might miss.

Four scoping flags shape the output:

```bash
# Enumerate the system parts available to describe (no full output)
dotnet run --project src/CritterCab.Trips -- describe --list

# Scope output to a single system part by title
dotnet run --project src/CritterCab.Trips -- describe --title "Wolverine Routing"

# Pick parts interactively from a multi-select prompt
dotnet run --project src/CritterCab.Trips -- describe --interactive

# Write to file (HTML output if the file extension is .html)
dotnet run --project src/CritterCab.Trips -- describe --file ./artifacts/trips.html
```

The `--file` flag detects HTML extensions and emits color-preserving HTML; otherwise it emits plain text. Useful for CI artifacts.

---

## Schema management — Weasel commands

Marten and Polecat both delegate schema migration to Weasel, which contributes five commands. These are the closest thing to the "alembic" or "Flyway" surface in Cab — apply, assert, patch, dump, list.

### `--database` flag (all Weasel commands)

Every Weasel command accepts an optional `--database <identifier>` flag. When the service registers a single Weasel-backed database (the typical Cab service), the flag isn't needed. When the service registers multiple — e.g., a primary Marten store plus a Wolverine durability store on a separate database, or a service with ancillary Marten stores — the flag scopes the operation to one of them. Identifier matching is by either the database's `Identifier` string or a partial URI match on its subject/database URI.

Use `db-list` (below) to discover available identifiers when you're not sure what the service registered.

### `db-apply` — apply pending schema migrations

```bash
dotnet run --project src/CritterCab.Trips -- db-apply
```

Detects schema drift between the service's declared model (Marten document types, event store schema, Wolverine envelope tables) and the database, then applies the difference. Idempotent: re-running against an in-sync database is a no-op.

This is the canonical pre-deploy step for Cab services. CI/CD pipelines should run `db-apply` before flipping the deployment to a new version, so the schema is ready when the new binary starts.

### `db-assert` — verify schema is in sync

```bash
dotnet run --project src/CritterCab.Trips -- db-assert
```

Returns non-zero exit code if schema drift exists. The opposite of `db-apply` — it doesn't change anything, just reports.

Two practical uses:

- **Post-deploy gate.** After `db-apply` runs in CI, follow with `db-assert` as a paranoid check. Catches schema-application failures that didn't surface as `db-apply` errors.
- **Pre-PR check.** Run locally before opening a PR to confirm your local Marten/Polecat changes haven't drifted from the schema you expect.

### `db-patch` — generate migration SQL

```bash
dotnet run --project src/CritterCab.Trips -- db-patch ./artifacts/trips-migration.sql
```

Writes the SQL needed to bring the database in sync with the model to the named file. Also writes a matching **drop file** alongside it (Weasel generates both an apply and a drop script per migration so a rollback path is always immediately available). Doesn't apply anything. Useful when:

- A DBA needs to review schema changes before they hit production.
- The deployment target uses a managed migration pipeline (Azure SQL DACPAC, Liquibase, Flyway) and Cab's pipeline is to feed Weasel-generated SQL into that tool rather than calling `db-apply` directly.

Two flags worth knowing:

```bash
# Override the AutoCreate behavior; default is CreateOrUpdate
dotnet run --project src/CritterCab.Trips -- db-patch ./artifacts/migration.sql --auto-create All

# Wrap the generated script in a transaction
dotnet run --project src/CritterCab.Trips -- db-patch ./artifacts/migration.sql --transactional-script
```

`--auto-create` accepts standard Weasel `AutoCreate` values (`All`, `CreateOrUpdate`, `CreateOnly`, `None`). `--transactional-script` is recommended for any production-bound script — failures roll back rather than leaving the database in a half-migrated state.

### `db-dump` — dump current schema

```bash
dotnet run --project src/CritterCab.Trips -- db-dump ./artifacts/trips-schema.sql
```

Writes the current declared schema (what Marten/Polecat would create from scratch) to a file. Useful for:

- Generating bootstrap scripts for new environments.
- Comparing schema across deployment environments.
- Auditing what a service would create on a fresh database, decoupled from migration history.

Two flags:

```bash
# Split the SQL by feature into separate files in a directory (-f shorthand)
dotnet run --project src/CritterCab.Trips -- db-dump ./artifacts/trips-schema/ --by-feature

# Wrap the generated script in a transaction
dotnet run --project src/CritterCab.Trips -- db-dump ./artifacts/trips-schema.sql --transactional-script
```

`--by-feature` is the right choice when reviewing schema changes — one file per Marten/Polecat feature (event store, document storage, projection tables, daemon state) makes diffing across versions much cleaner. The destination becomes a directory rather than a single file.

### `db-list` — enumerate registered databases

```bash
dotnet run --project src/CritterCab.Trips -- db-list
```

Lists every Weasel-managed database registered in the service: `Identifier`, `DatabaseUri`, `SubjectUri`, and tenant IDs (when multi-tenant). Use this to discover what `--database <identifier>` should be set to when scoping any other Weasel command, particularly in services with multiple stores.

---

## Resource lifecycle — `resources` command

JasperFx contributes a higher-level `resources` command that operates across every "stateful resource" registered by the host. For Cab services this includes Marten document stores, Polecat document stores, Wolverine envelope storage, and any other `IStatefulResource` from extensions:

```bash
dotnet run --project src/CritterCab.Trips -- resources <action>
```

Six actions, each scoped by optional `--type <type>`, `--name <name>`, and `--timeout <seconds>` flags (default timeout 60). `--type` filters by resource type (`Marten`, `Polecat`, `WolverineEnvelopes`, etc.); `--name` filters by the specific resource's name within that type:

| Action | Effect |
|---|---|
| `setup` | Create resources if missing; run any required setup. Equivalent to "make sure everything's in place." |
| `teardown` | Remove resources entirely. Drops schemas, tables, queues. Destructive; use only in dev. |
| `clear` | Clear runtime state without removing the resource. Empties tables, drains queues, leaves schema in place. |
| `check` | Validate resource health. Non-destructive; reports problems. |
| `statistics` | Print a status summary per resource. |
| `list` | Enumerate registered resources. |

```bash
dotnet run --project src/CritterCab.Trips -- resources setup --timeout 120
dotnet run --project src/CritterCab.Trips -- resources list
dotnet run --project src/CritterCab.Trips -- resources clear   # dev only
dotnet run --project src/CritterCab.Trips -- resources teardown --type Marten  # scope to one resource type
dotnet run --project src/CritterCab.Trips -- resources check --name trips-events  # scope to one named resource
```

### `db-apply` vs. `resources setup`

Both create database schema; they differ in scope:

- **`db-apply`** is Weasel-specific — it operates on schema migrations for Marten and Polecat. It doesn't touch Wolverine envelope storage initialization that lives outside the schema layer.
- **`resources setup`** is everything-at-once — it runs every registered `IStatefulResource.Setup()`, which includes schema migrations *and* any non-schema initialization (queue creation, topic provisioning, leader-election state).

The practical rule for Cab:

- **CI/CD pre-deploy** → `db-apply`. Fast, schema-only, predictable.
- **Local dev fresh start** → `resources setup`. Catches everything in one command without thinking about which subsystem owns which initialization step.
- **Test fixtures** → neither. Tests use Testcontainers and `CleanAllMartenDataAsync` per `testing-integration`.

---

## Code generation — `codegen` command

JasperFx code generation produces compiled handler adapters, HTTP endpoint wrappers, gRPC service stubs, and similar runtime glue at startup. The `codegen` command exposes four actions to control how that generated code is managed:

```bash
dotnet run --project src/CritterCab.Trips -- codegen <action>
```

Four actions:

| Action | Effect |
|---|---|
| `preview` (default) | Print every piece of generated code to stdout. Doesn't write anything. |
| `write` | Write generated code to disk in the project's `Internal/Generated/` directory. Required for `TypeLoadMode.Static`. |
| `delete` | Remove all previously-written generated code from disk. |
| `test` | Verify codegen produces compilable output. CI gate. |

### `codegen preview` — see what gets generated

```bash
dotnet run --project src/CritterCab.Trips -- codegen preview
dotnet run --project src/CritterCab.Trips -- codegen preview --type WolverineHandlers
dotnet run --project src/CritterCab.Trips -- codegen preview --start
```

The `--type` flag scopes the preview to a specific generator (e.g. `WolverineHandlers`, `HttpEndpoints`, `MartenAggregateHandler`). Use when full preview output is overwhelming and you want to focus on one layer.

The `--start` flag actually starts the host (rather than just building it) so codegen participants registered during host startup are discovered. Cab services that use Wolverine.Http endpoints registered via `app.MapWolverineEndpoints()` need `--start` for previews to show the HTTP chains — the chains aren't registered until host startup runs `Configure()`.

For previewing one specific handler, route, or gRPC service, `wolverine-diagnostics codegen-preview` (covered below) is more targeted than the JasperFx-level `codegen preview` — read that section for the granular surface.

### `codegen write` — write generated code to disk

```bash
dotnet run --project src/CritterCab.Trips -- codegen write
```

Writes generated code into `src/CritterCab.Trips/Internal/Generated/`. Required when the service's `JasperFxOptions` use `TypeLoadMode.Static` for production code mode (which precompiles handlers into the assembly rather than generating them at startup).

The Cab convention per `service-bootstrap`:

- **Development** — `TypeLoadMode.Auto`. Generated code lives in memory; no disk artifacts; iteration is fast.
- **Production** — `TypeLoadMode.Static`. Generated code lives in `Internal/Generated/` and is compiled into the assembly; startup is faster and code-generation runtime cost vanishes.

The CI/CD step that builds production artifacts runs `codegen write` before `dotnet publish` to emit the generated files, then commits them as part of the build artifact (or to source if Cab adopts that convention later).

### `codegen test` — CI gate

```bash
dotnet run --project src/CritterCab.Trips -- codegen test
```

Runs codegen and verifies the output is syntactically valid C# that the C# compiler accepts. Returns non-zero exit code if anything fails. The canonical CI gate that catches:

- Handler signatures Wolverine can't generate adapters for.
- HTTP endpoints with conflicting routes.
- gRPC service mappings with mismatched proto and C# types.
- Marten projection handlers missing required `Apply` overloads.

Run on every PR. Fast — usually under five seconds per service.

### `codegen delete` — clean up generated artifacts

```bash
dotnet run --project src/CritterCab.Trips -- codegen delete
```

Removes everything in `Internal/Generated/`. Useful when:

- Switching from `Static` to `Auto` mode and you want to clean up stale files.
- Generated code from an older codegen version has gone stale and you want to regenerate from scratch.

`codegen delete` followed by `codegen write` is the canonical refresh pattern.

---

## Wolverine diagnostics — `wolverine-diagnostics`

The Wolverine-specific diagnostics command. Two sub-commands today, with rich flag-based scoping. This is the most consequential command surface for Cab debugging because Cab uses many transports (gRPC, Kafka, ASB) and routing rules across them are easy to misconfigure.

```bash
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics <sub-command> [flags]
```

### `describe-routing` — message routing topology

Shows how Wolverine routes outgoing messages: which transports they go to, which subscriptions match, what serialization applies.

```bash
# Routing for one specific message type
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics describe-routing TripCompleted

# Full routing topology — every known message type
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics describe-routing --all
```

The `MessageType` argument accepts:

- Full type name (`CritterCab.Trips.Events.TripCompleted`).
- Short class name (`TripCompleted`).
- Aliases declared via `[MessageIdentity]`.

Use `describe-routing` whenever:

- A message you expected to be published to ASB ended up on no transport (`tracked.NoRoutes` per `testing-integration`).
- You're refactoring transport assignments and want to verify the new state before deploying.
- You're auditing every cross-service message Cab emits, in preparation for a contract review.

The `--all` flag is verbose but invaluable when you need the complete picture — pipe to a file for review:

```bash
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics describe-routing --all > ./artifacts/trips-routing.txt
```

### `codegen-preview` — targeted code preview

Previews generated code for a single handler, HTTP route, or gRPC service. More precise than the JasperFx-level `codegen preview`, which dumps everything.

```bash
# Preview the adapter Wolverine generates for one handler
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics codegen-preview --handler CritterCab.Trips.StartTrip

# Short class name works
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics codegen-preview --handler StartTrip

# Or the handler class name itself
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics codegen-preview --handler StartTripHandler

# Preview an HTTP endpoint adapter
dotnet run --project src/CritterCab.Trips -- wolverine-diagnostics codegen-preview --route "POST /api/trips"

# Preview a proto-first gRPC service stub (when Cab adds gRPC)
dotnet run --project src/CritterCab.Pricing -- wolverine-diagnostics codegen-preview --grpc PricingService
```

This is the highest-leverage debugging tool when a handler's behavior surprises you. Wolverine generates adapter code that wires up validation, transactions, side-effect cascading, and aggregate loading — when the runtime behavior doesn't match expectations, the adapter source tells you exactly what's actually executing. Faster than reading Wolverine source; more precise than runtime logging.

The `--handler`, `--route`, and `--grpc` flags are mutually exclusive — pass one per invocation.

### Additional surfaces in ai-skills

ai-skills `wolverine-observability-command-line-diagnostics` covers two surfaces Cab's skill doesn't enumerate:

- **`wolverine-diagnostics describe-resiliency`** — third sub-command alongside `describe-routing` and `codegen-preview`. Inspects active error-handling and circuit-breaker configuration scoped to an endpoint, a message type, or `--all` for full-system audit. The natural pair with `describe-routing` for pre-go-live verification: routing tells you *where* messages go, resiliency tells you *what happens when they fail*.
- **Programmatic equivalents.** `opts.DescribeHandlerMatch(typeof(Handler))` for handler-discovery diagnostics in code; `bus.PreviewSubscriptions(message)` for routing preview in tests; `host.SetupResources()`/`TeardownResources()`/`ResetResourceState()` and `IMessageStore.Admin.RebuildAsync()`/`ClearAllAsync()` for the test-fixture and admin-endpoint equivalents of the CLI surface. Cab's tests use Testcontainers + `CleanAllMartenDataAsync` rather than these programmatic resource APIs (per `testing-integration`), but the APIs are the right primitive for in-process admin endpoints when those are needed.

ai-skills also documents a useful `Environment.GetCommandLineArgs().Contains("codegen")` guard for disabling persistence connections during codegen-only runs — prevents the host from trying to reach databases that aren't available at codegen time (e.g., when running CLI from CI without an Aspire-orchestrated database).

---

## Wolverine capabilities — `capabilities`

Writes Wolverine's discovered service capabilities (handlers, transports, subscriptions, message types) to a JSON file:

```bash
dotnet run --project src/CritterCab.Trips -- capabilities
# Default: writes to wolverine.json in the working directory

dotnet run --project src/CritterCab.Trips -- capabilities ./artifacts/trips-capabilities.json
```

The output is a structured `ServiceCapabilities` document that describes what messages the service publishes, what it consumes, and over which transports. Three concrete uses:

- **Contract documentation.** Generate `capabilities` output as a build artifact and publish it alongside service docs so consumers can see the message contract without reading source.
- **Cross-service routing audits.** Diff the capabilities across services to surface mismatched message types or unbound publishes.
- **CI gates on contract changes.** A `capabilities` file committed to the repo, regenerated and diffed in CI, fails PRs that change a service's external contract without acknowledgment.

The command starts the host fully before writing — it sees the same routing rules and serializers that `aspire run` would. Reach for it when `describe-routing --all` is the right tool for human reading and `capabilities` is the right tool for machine consumption.

---

## Wolverine message storage — `storage` and `clear-handled`

Wolverine persists inbox/outbox state to a database for durability. The `storage` command exposes operations against that state:

```bash
dotnet run --project src/CritterCab.Trips -- storage <action>
```

Five actions:

| Action | Effect |
|---|---|
| `counts` (default) | Print row counts for inbox, outbox, scheduled, dead-letter tables. |
| `clear` | Delete every row across all envelope tables. Destructive. |
| `rebuild` | Drop and recreate envelope storage schema. Destructive. |
| `release` | Release any in-flight envelopes assigned to nodes that aren't running. |
| `replay` | Replay dead-lettered messages back into the inbox. |

### `storage counts` — health check

```bash
dotnet run --project src/CritterCab.Trips -- storage counts
```

Daily-ops telemetry. A growing dead-letter count means handlers are failing silently; a growing scheduled count means timers aren't firing; a growing outbox count means a transport is unreachable. Run as a periodic check or wire into Cab's observability story (per `observability-tracing`, Phase 3).

### `storage replay` — reprocess dead-lettered messages

```bash
# Replay everything in the dead-letter queue
dotnet run --project src/CritterCab.Trips -- storage replay

# Replay only failures of one exception type
dotnet run --project src/CritterCab.Trips -- storage replay --exception-type Npgsql.NpgsqlException
```

`--exception-type` (or `-t`) scopes the replay to envelopes that died with a specific exception. Useful after fixing a known bug — replay only the messages that hit it, leave the others for manual triage.

### `storage release` — recover from node failures

```bash
dotnet run --project src/CritterCab.Trips -- storage release
```

When a service node dies unexpectedly (OOM, k8s eviction, dev-machine crash), envelopes assigned to that node sit in "in-flight" state until a heartbeat timeout. `storage release` immediately frees them so other nodes can pick them up. Useful in dev when you've hard-killed a service and want it picked up by the next run without waiting for the heartbeat timeout.

### `clear-handled` — purge handled inbox entries

```bash
dotnet run --project src/CritterCab.Trips -- clear-handled
```

Wolverine retains inbox entries marked `Handled` for a configurable window to support deduplication. `clear-handled` purges them immediately. Useful when:

- The inbox is large enough to slow startup queries.
- A test left behind handled messages you want gone before the next run.
- Cleaning up after a load test that produced millions of handled entries.

---

## Projection management — `projections`

The `projections` command from `JasperFx.Events` is the canonical CLI for working with Marten projections outside the running daemon. It supports three actions:

```bash
dotnet run --project src/CritterCab.Trips -- projections <action> [flags]
```

| Action | Effect |
|---|---|
| `run` (default) | Run projections continuously — same as the embedded daemon. Useful when running the projection daemon as a separate process from the API host. |
| `list` | Enumerate registered projections, their type, and current state. |
| `rebuild` | Rebuild one or more projections from scratch. The canonical CLI rebuild path. |

### Flags

Five flags scope what the command operates on:

- `--projection <name>` — limit to a single named projection. Use the projection's registered name (typically the type name).
- `--store <uri>` — limit to a specific event store by subject URI. Only meaningful when the service registers multiple stores.
- `--database <identifier>` — limit to a specific database within the store(s). Multi-database services only.
- `--tenant <id>` — limit to a single tenant's database. The whole database containing the tenant is operated on.
- `--shard-timeout <duration>` — override the shard timeout. Useful for slow rebuilds against large event stores.
- `--advance` — advance the projection high-water mark to the latest event sequence as part of the operation.

### Common patterns

```bash
# Inspect what's registered
dotnet run --project src/CritterCab.Trips -- projections list

# Rebuild one projection from scratch
dotnet run --project src/CritterCab.Trips -- projections rebuild --projection ActiveTripsProjection

# Rebuild every projection — heavy operation, use with care
dotnet run --project src/CritterCab.Trips -- projections rebuild

# Tenant-scoped rebuild
dotnet run --project src/CritterCab.Trips -- projections rebuild --projection RiderActivity --tenant rider-acme

# Run projections as a standalone process (deployment scenario where API and projection daemon are separate)
dotnet run --project src/CritterCab.Trips -- projections
```

### When to reach for `projections rebuild` vs daemon API

Two paths exist for rebuilding projections:

- **`projections rebuild` CLI.** Standalone process. The API host doesn't need to be running. Right for one-shot rebuilds, deployment hooks, and CI/CD jobs that need to apply a projection schema change to production data.
- **Daemon API in code (`IProjectionDaemon.RebuildProjectionAsync(...)`).** Inline within a running service. Right for programmatic flows — administrative endpoints, scheduled jobs, integration tests that need to rebuild between scenarios.

For Cab specifically, the choice is operational:

- **Production projection schema change** → `projections rebuild --projection <name>` from the deployment pipeline. Fully observable from logs; doesn't tie up the API.
- **Admin endpoint or scheduled job** → daemon API in code per `marten-async-daemon`.
- **Test fixture cleanup** → neither; tests use `WaitForNonStaleProjectionDataAsync` after appending events, per `testing-integration`.

---

## Marten event store healing — `marten`

Marten contributes a single CLI command for advanced event store operations. Reach for it when the daemon is stuck or the high-water mark is wrong:

```bash
dotnet run --project src/CritterCab.Trips -- marten <flags>
```

Four flags, mutually-mostly-exclusive:

| Flag | Effect |
|---|---|
| `--advance` | Advance the high-water mark to the highest detected event sequence. |
| `--correct` | Correct event progression after database hiccups (e.g. failover left the projection state inconsistent). |
| `--reset` | Reset all Marten data. Equivalent to `resources clear` for the Marten store. |
| `--tenant-id <id>` | Limit operations to a single tenant. |

```bash
# After a Postgres failover that left the daemon stuck
dotnet run --project src/CritterCab.Trips -- marten --correct

# Manual catch-up of HWM after an event-loader misbehavior
dotnet run --project src/CritterCab.Trips -- marten --advance

# Reset (dev only)
dotnet run --project src/CritterCab.Trips -- marten --reset

# Tenant-scoped reset
dotnet run --project src/CritterCab.Trips -- marten --reset --tenant-id rider-acme
```

`--correct` and `--advance` are read-mostly and safe to run against production when needed. `--reset` is destructive — never run against production; reserve for dev fixture cleanup or test environment reset.

For projection rebuilds, see the dedicated `projections rebuild` command above — it's the canonical CLI rebuild path. The `marten` command focuses on healing event-stream progression and clearing data, not on projection management.

---

## Composing CLI invocations with Aspire

The Aspire AppHost (per `aspire`) and the JasperFx CLI are independent paths to running a service:

- **`aspire run`** — boots all Cab services through the AppHost. Each service runs as the `run` command (default).
- **`dotnet run --project <service> -- <command>`** — runs one service's CLI command. The Aspire AppHost is not involved.

When you want to run a CLI command against a service that's already orchestrated by Aspire, **don't try to attach to the running Aspire host**. The CLI invocation builds its own host instance, reads connection strings from the same configuration sources, and exits. Two parallel host instances against the same database are fine for read-mostly commands (`describe-routing`, `describe`, `storage counts`); for destructive commands (`db-apply`, `resources clear`, `storage rebuild`) make sure the Aspire-orchestrated service isn't running, or work against an `aspire run --isolated` instance to avoid contention.

Practical Cab patterns:

- **Local debugging.** `aspire run` running in one terminal; `dotnet run --project src/CritterCab.Trips -- describe-routing TripCompleted` in another. Read-mostly, totally fine.
- **Pre-deploy migration.** CI/CD pipeline runs `dotnet run --project src/CritterCab.Trips -- db-apply` against the production database. No Aspire involvement; Aspire is local-dev only.
- **Test environment reset.** `aspire stop`, then `dotnet run --project src/CritterCab.Trips -- resources clear`, then `aspire run` again for a fresh state.

---

## CI/CD usage patterns

The JasperFx CLI is well-suited to CI gates. The canonical Cab pipeline shape:

### Pre-deploy schema gate

```bash
# Generate the migration to a file for review
dotnet run --project src/CritterCab.Trips --no-launch-profile -- db-patch ./artifacts/trips-migration.sql

# (Reviewer pauses here in production deployments)

# Apply
dotnet run --project src/CritterCab.Trips --no-launch-profile -- db-apply

# Verify
dotnet run --project src/CritterCab.Trips --no-launch-profile -- db-assert
```

### Pre-deploy code generation

```bash
# Write generated code into the project for Static-mode production builds
dotnet run --project src/CritterCab.Trips --no-launch-profile -- codegen write

# Verify the result compiles
dotnet build src/CritterCab.Trips
```

### PR validation gates

```bash
# Codegen integrity — fail PR if Wolverine can't generate handlers
dotnet run --project src/CritterCab.Trips --no-launch-profile -- codegen test

# Routing audit — capture topology for review-time diff
dotnet run --project src/CritterCab.Trips --no-launch-profile -- wolverine-diagnostics describe-routing --all > ./artifacts/trips-routing.txt

# Environment integrity
dotnet run --project src/CritterCab.Trips --no-launch-profile -- check-env
```

### Operational diagnostics

```bash
# Daily storage health check
dotnet run --project src/CritterCab.Trips --no-launch-profile -- storage counts

# After deploying a fix, replay anything that died with the now-fixed exception
dotnet run --project src/CritterCab.Trips --no-launch-profile -- storage replay --exception-type CritterCab.Trips.Domain.InvariantViolationException
```

### Exit codes

Every JasperFx command returns 0 on success, non-zero on failure. CI scripts can chain them with `&&` or use `set -e` (bash) for fail-fast behavior. The exit code is the contract; no parsing of stdout required.

---

## Common pitfalls

- **Forgetting the `--` separator.** `dotnet run --project src/CritterCab.Trips db-apply` interprets `db-apply` as a `dotnet run` argument and fails confusingly. Always include `--` before the JasperFx command.
- **Forgetting `--no-launch-profile`.** Without it, `launchSettings.json` URLs and environment variables apply, which can cause the CLI to bind ports it doesn't need or pick up dev-only configuration. Inconsequential for read-only commands; can produce confusing failures for `db-apply` against the wrong database.
- **Running `resources teardown` against shared infra.** Drops schemas across every registered `IStatefulResource`. In dev with one developer's database, fine; against any shared environment, catastrophic. Prefer `resources clear` if the schemas should remain.
- **Running `marten --reset` against production.** Wipes Marten data. Use `--correct` or `--advance` for healing; `--reset` is a dev/test-only tool.
- **Confusing `codegen preview` (JasperFx) with `wolverine-diagnostics codegen-preview` (Wolverine).** The former dumps everything; the latter targets a single handler/route/gRPC service. They serve different needs — when you want one specific adapter, the Wolverine-targeted form is right.
- **Forgetting `codegen preview --start` for HTTP chains.** Wolverine.Http endpoints are registered during host startup via `MapWolverineEndpoints()`, not during host build. Without `--start`, the preview won't include them and you'll wonder why your route isn't there.
- **Treating `db-apply` as the schema source of truth.** `db-apply` brings the database in sync with the model. The model lives in code (Marten document type configuration, event registrations, Polecat schema definitions). Schema drift is solved by changing code and re-running `db-apply`, not by editing SQL directly.
- **Skipping `db-assert` after `db-apply` in CI.** `db-apply` can succeed in some failure modes that leave residual drift (manual SQL ran outside the pipeline, partial migration, race against concurrent deploys). The paranoid `db-assert` follow-up costs nothing and catches these.
- **Running CLI commands while `aspire run` holds the same database.** Read-mostly commands are fine; destructive commands (`db-apply` in some configurations, `resources clear`, `storage rebuild`) can race or conflict. Stop the Aspire-orchestrated instance first, or run Aspire with `--isolated`.
- **Expecting `storage replay` to fix the underlying bug.** It re-enqueues dead-lettered messages; if the bug is still there, they re-die. Replay only after deploying a fix.
- **Running `codegen write` without `dotnet build` after.** Generated files won't be in the assembly until rebuild. Always pair `write` with a build.
- **Forgetting that gRPC handlers need gRPC packages registered to surface in `describe-routing`.** A handler whose transport binding is missing won't show up as routed; it'll show up as `NoRoutes` in tests. The fix is in `Program.cs` (per `service-bootstrap`), not in the CLI.
- **Running `marten --reset` per-tenant when the goal is full reset.** Without `--tenant-id`, `--reset` is global. The flag scopes; absence of the flag means "all tenants."
- **Reaching for the daemon API in code when `projections rebuild` would do.** The CLI command exists precisely so production rebuilds don't require running an admin endpoint inside the API. For one-shot rebuilds against a deployed environment, the CLI is the right primitive.
- **Forgetting the `--database` flag in multi-database services.** Weasel commands operate on every registered database by default unless scoped. In a service with a primary Marten store plus an ancillary store on a separate database, `db-apply` runs against both — usually fine, but worth knowing.
- **Using `clear-handled` for routine cleanup.** Wolverine's automatic cleanup window handles this. Reach for `clear-handled` only when there's a specific reason — large inbox slowing startup, post-load-test cleanup, debugging dedup behavior.

---

## See also

**Upstream** — generic Wolverine CLI fundamentals this skill builds on. ai-skills (license required, install via `npx skills add`):

- `wolverine-observability-command-line-diagnostics` (primary) — Wolverine slice of the JasperFx CLI: `RunJasperFxCommands` enablement, `describe` with section-by-section troubleshooting framing, `capabilities` JSON export, schema commands (`db-apply`, `db-assert`, `db-patch`, `db-dump`, `db-list` with `-d` flag for multi-database), `storage` (rebuild/clear/release), `resources` (setup/teardown), `check-env`, `wolverine-diagnostics describe-routing`, `wolverine-diagnostics codegen-preview`, **`wolverine-diagnostics describe-resiliency`** (covered there but not in Cab), programmatic equivalents (`opts.DescribeHandlerMatch`, `bus.PreviewSubscriptions`, `host.SetupResources`/`TeardownResources`/`ResetResourceState`, `IMessageStore.Admin`), and the disabling-persistence-for-codegen-runs pattern. Cab's skill extends this with the comprehensive five-contributor surface (JasperFx core / JasperFx.Events / Weasel / Wolverine / Marten), full `projections` and `marten` command surfaces, decision matrices (`db-apply` vs `resources setup`; `projections rebuild` vs daemon API), Aspire composition guidance, CI/CD pipeline patterns, and 17 Cab-specific pitfalls.

**Prerequisites** — Cab-internal skills to load first:

- `service-bootstrap` — the `Program.cs` registration that ends with `RunJasperFxCommands(args)`. Without that line, none of these commands exist on a service.
- `csharp-coding-standards` — invocation conventions, formatting; relevant when scripting CLI invocations alongside other tooling.

**Sibling skills:**

- `aspire` — the AppHost shape; how Aspire-orchestrated services compose against the same `Program.cs` that CLI invocations build a host from.
- `cli-aspire` — the Aspire CLI surface; pairs with this skill for the dev-time inner loop. Aspire orchestrates services; JasperFx CLI invokes commands inside them.
- `marten-async-daemon` — daemon configuration; the `marten` CLI heals what the daemon can't recover automatically. `projections rebuild` is the CLI surface; the daemon API is the in-code surface.
- `marten-projections` — projection lifecycle; `projections rebuild` and `--advance` map directly to the operations described there.
- `marten-aggregates`, `marten-wolverine-aggregates` — aggregate handler shapes; `wolverine-diagnostics codegen-preview --handler` shows the generated adapter.
- `wolverine-handlers`, `wolverine-http-handlers`, `wolverine-messaging-handlers` — handler shapes; `wolverine-diagnostics describe-routing` audits what the runtime actually routes.
- `dynamic-consistency-boundary` — DCB write-path configuration; covered by `describe-routing` for outgoing-event routing.

**Downstream:**

- `wolverine-grpc-handlers` (Phase 3) — gRPC routing; `wolverine-diagnostics codegen-preview --grpc` is the dedicated preview surface.
- `wolverine-kafka` (Phase 3) — Kafka transport; routing visible via `describe-routing`.
- `wolverine-azure-service-bus` (Phase 3) — ASB transport; same.
- `wolverine-sagas` (Phase 4) — saga state inspection via `describe`; saga storage covered by `storage counts`.
- `polecat-event-sourcing` (Phase 4) — Polecat-specific schema management goes through the same Weasel commands (`db-apply`, `db-assert`).
- `testing-advanced` (Phase 4) — multi-host orchestration; dedicated CLI patterns for advanced test fixture composition.

**External:**

- [JasperFx CLI Documentation](https://jasperfx.github.io/jasperfx/) — the canonical reference for the JasperFx command framework.
- [Marten CLI Documentation](https://martendb.io/configuration/cli/) — Marten-specific commands and the `marten` healing command.
- [Wolverine Diagnostics Documentation](https://wolverinefx.io/guide/diagnostics/) — `wolverine-diagnostics` commands, `describe-routing`, codegen previews.
- [Wolverine Storage Commands](https://wolverinefx.io/guide/durability/cli.html) — `storage` actions, `clear-handled`, dead-letter replay.
- [Weasel Database Migration Tools](https://github.com/JasperFx/weasel) — the Weasel-contributed `db-apply`, `db-assert`, `db-patch`, `db-dump` commands.
- [JasperFx Code Generation](https://jasperfx.github.io/jasperfx/codegen/cli.html) — `codegen` actions, `TypeLoadMode` semantics.
