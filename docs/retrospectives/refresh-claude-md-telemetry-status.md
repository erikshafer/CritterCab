# Retrospective — Refresh CLAUDE.md's Stale Status Line (Telemetry Service Exists)

## Metadata

- **Triggering prompt:** [`docs/prompts/refresh-claude-md-telemetry-status.md`](../prompts/refresh-claude-md-telemetry-status.md)
- **Status:** Complete
- **Date authored:** 2026-07-17
- **Output artifacts:**
  - `CLAUDE.md` — status paragraph rewritten
  - `docs/prompts/refresh-claude-md-telemetry-status.md` (new) — the triggering prompt
  - `docs/prompts/README.md` — new entry under "Multi-artifact prompts (root)"
  - `docs/retrospectives/refresh-claude-md-telemetry-status.md` (new, this file)
  - `docs/retrospectives/README.md` — matching entry
- **Outcome:** `CLAUDE.md`'s status line now names both services in code (Dispatch's three slices, Telemetry's skeleton + slice 1), the three design-only bounded contexts (Trips, Onboarding, Identity — workshops complete, no code), and the six still-pre-workshop contexts, replacing a line stale since PR #4.

---

## Framing

Flagged by the post-PR-42 handoff (`docs/planning/2026-07-10-post-pr42-telemetry-next-pr-b-handoff.md`) as a `tidy: housekeeping` candidate: "CLAUDE.md status line ... is now stale — Telemetry is a second service with code." Picked up as part of a small housekeeping sweep alongside a CI Actions version bump, while the Telemetry gRPC chain is externally gated on an upstream Wolverine fix.

---

## Outcome summary

The old line collapsed everything except Dispatch into "pre-workshop," which was true in May and false by July — three more workshops (Trips, Onboarding, Identity) had landed with zero code against them, a state the old binary couldn't express. The new paragraph distinguishes three states explicitly: **in code** (Dispatch, Telemetry), **designed but unbuilt** (Trips, Onboarding, Identity), and **pre-workshop** (the remaining six tentative bounded contexts).

---

## What worked

- **The handoff had already done the diagnosis.** No investigation needed — the drift and its cause were named in the prior session's handoff note; this session was pure execution.
- **Naming three states instead of two** turned out to be the accurate fix, not a scope creep — the original two-state framing (Dispatch done / everything else pre-workshop) was already wrong the moment Trips' workshop landed (W002, well before this session), it just hadn't been caught.

---

## What was harder than expected

Nothing. Single-file content fix with a well-defined source of truth (the `docs/workshops/` directory listing and the `src/` tree).

---

## Methodology refinements that emerged

None — this session applied existing convention (`tidy: housekeeping`, standard prompt/retro/index bookkeeping) without needing to extend it.

---

## Outstanding items / next-session inputs

- **CI Actions v4→v5 bump** — same handoff's housekeeping list, deliberately separate session (`tidy: ci`) per the no-opportunistic-edits scope rule.
- **Deleting 5 stale merged remote branches** (PRs #38–#42) — administrative, not a code session; left for the user to run directly (`git push origin --delete <branch>`), also flagged by this handoff.

---

## Spec delta — landed?

**Null, as planned.** No narrative or workshop is amended — `CLAUDE.md` is a routing layer, not a canonical spec artifact.
