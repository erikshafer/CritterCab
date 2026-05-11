# ADR-011: Configuration-as-Events Bootstrap Strategy

**Status:** Accepted  
**Date:** 2026-05-10

## Context

Two bounded contexts now adopt **configuration-as-events** for operator-tunable parameters. Workshop 001 §5.11 established the pattern with `DispatchPolicyConfigured` events on a singleton `DispatchPolicy` stream; Workshop 002 §6.11 confirmed it with `TripsPolicyConfigured` events on a singleton `TripsPolicy` stream. The shape is consistent across both BCs: a singleton aggregate per BC, full-replacement command/event semantics, cross-parameter validation via Wolverine compound handlers, Marten optimistic concurrency on the singleton stream.

The pattern is one of Bruun's Event Modeling notations and earns its place in CritterCab through two structural properties: "what was the policy at time T?" is answerable from the event log alone, and the pattern composes with Bruun's *carry-the-value* discipline (W001 §5.4, W002 §6.3) — when a downstream event captures a policy-derived value at decision time, the value survives subsequent policy changes intact, and the audit trail explains itself.

Every consuming slice depends on the policy stream being non-empty. Workshop 001's slices 5.2, 5.3, 5.4, 5.7, and 5.9 all read parameters from the projected `DispatchPolicy` view; Workshop 002's slice 6.3 reads `noShowTimeoutSeconds` from the projected `TripsPolicy` view. If a configurable BC starts with an empty policy stream, those slices have no defaults to fall back on — and scattering defaults across N call-sites would defeat the centralization the pattern exists to provide.

Something must seed the stream first. The seam exists because the pattern is event-sourced; a settings-table or config-service approach would not have it. The question this ADR settles is what mechanism does the seeding, and where the seed event's cause lives — so future configurable BCs adopt a single canonical approach rather than re-deriving the choice each time.

## Options Considered

### Option A — Migration-time seed

The seed event is appended by the database migration (or deployment script) that creates the policy stream's storage. The migration is idempotent: it checks whether the singleton stream is empty and appends the seed event only if no policy events exist. The seed payload is literal in the migration script — not a code constant — with `operatorId = "system-bootstrap"` as a recognizable durable marker and `configuredAt` set from the migration's execution time.

The seed becomes part of the deployment artifact. It runs exactly once per environment, before any service instance accepts traffic. A future reader looking for "where did the initial policy come from?" finds it in the migration file alongside the schema change that introduced the BC's policy stream — the same audit trail used for every other change to the BC's storage shape.

### Option B — Startup self-seed

The service checks at startup; if the policy stream is empty, it appends a seed event from baked-in code defaults. The check uses Marten's `FetchStreamStateAsync` against the singleton's well-known stream ID; the append is conditional on the result.

The seed event still exists, but its cause is in application bootstrap code rather than alongside schema migrations. A reader looking for "where did the initial policy come from?" has to find the bootstrap class. The deeper issue is concurrency: in a horizontally-scaled deployment, multiple service instances start simultaneously and race to seed. Marten's optimistic concurrency catches the conflict, but the loser's failure mode is opaque — a `ConcurrencyException` at startup may indicate "a peer seeded first" or "a real operator collision," and the bootstrap code must distinguish the two cases to know whether to retry or alert.

### Option C — Refuse-until-configured

The service starts but rejects all traffic until an operator has explicitly configured policy via the admin command. No seed event exists; the first event on the policy stream is always operator-initiated.

This couples first-deployment readiness across BCs that should be independently deployable (ADR-002): a configurable BC cannot serve any traffic until the Operations BC and admin UI are live and an operator has acted. It also breaks the "service is healthy at deploy time" property that simplifies CI/CD readiness gating; deployment success and operational readiness become two separate states that must each be observed.

## Decision

**Option A.** CritterCab seeds policy streams via migration-time scripts that idempotently append a `<BC>PolicyConfigured` event with documented defaults.

The seed payload lives in the migration script itself, not in code constants. It carries `operatorId = "system-bootstrap"`, a `reason` field describing what the seed is doing (e.g., `"Initial deployment defaults"`), and a `configuredAt` timestamp from the migration's execution time.

The migration guard pattern is: load stream state for the singleton's well-known ID; if empty, append the seed event; otherwise no-op. This makes re-running the migration on an already-seeded environment safe — a property that matters when migrations are part of the deployment pipeline and may be applied opportunistically against environments at varying states.

This is what both workshops' reference seeds (Workshop 001 §5.11; Workshop 002 §6.11) were already structurally illustrating. The `operatorId = "system-bootstrap"` and `reason = "Initial deployment defaults"` fields make sense if the seed is migration-driven, where they serve as audit signals that the event came from infrastructure rather than an operator. They make less sense if the seed comes from in-process startup code, where `operatorId` would more naturally be empty or service-instance-derived.

## Consequences

Each new configurable BC adds one migration containing a seed event for its singleton policy stream. Seed values are committed alongside schema changes and traceable through the same review and audit trail as every other change to the BC's storage shape.

When a configurable BC adds a parameter post-launch, the migration that introduces the new field also appends a follow-on `<BC>PolicyConfigured` seed event with the new parameter included in the full-replacement payload. The seed history forms a record of how policy defaults evolved across deployments — a property useful for audit and dispute resolution that would not exist if defaults lived in code.

The migration script becomes a pseudo-source-of-truth for "what defaults does this BC ship with?" — the same authority a `defaults.json` would hold, but reachable via the event log without reading deployment artifacts.

Regional-singleton policy streams (Workshop 001 §5.11 post-MVP enhancement) extend the pattern: when per-region policy is adopted, the migration seeds N events (one per region) using the same idempotent-guard pattern. This ADR codifies the bootstrap mechanism; per-region extension does not require superseding it.

A skill file codifying the migration template — idempotent guard plus seed event payload — is identified as a follow-up. It is not authored in this session; the gap is recorded for the DEBT.md ledger and addressed when the first migration is written during implementation.
