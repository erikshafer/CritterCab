# Prompt — Refresh CLAUDE.md's Stale Status Line (Telemetry Service Exists)

| Field | Value |
|---|---|
| **Status** | Complete (2026-07-17) |
| **Authored** | 2026-07-17 |
| **Target artifacts** | `CLAUDE.md` (status paragraph), `docs/prompts/refresh-claude-md-telemetry-status.md` (this prompt), `docs/prompts/README.md` (index update), `docs/retrospectives/refresh-claude-md-telemetry-status.md` (retro), `docs/retrospectives/README.md` (index update) |
| **Source-of-truth dependencies** | `docs/planning/2026-07-10-post-pr42-telemetry-next-pr-b-handoff.md` (flagged the drift: "CLAUDE.md status line ... is now stale — Telemetry is a second service with code"); `docs/workshops/` listing (W002 Trips, W004 Onboarding, W005 Identity, W006 Telemetry all authored); `src/` tree (Dispatch 3 slices, Telemetry skeleton + slice 1) |
| **Workflow position** | Standalone `tidy: housekeeping` micro-PR. No design-return cadence implication — this is a routing-layer accuracy fix, not a spec amendment. |

---

## Spec delta

None. `CLAUDE.md` is a routing layer, not a canonical spec artifact (narrative or workshop) — this session corrects a factual drift in that routing layer, it does not amend any narrative or workshop's Document History.

---

## Framing

`CLAUDE.md`'s status paragraph still read "The Dispatch service has its first vertical slice ... running end-to-end ... All other bounded contexts remain pre-workshop" — accurate as of the first Dispatch slice (PR #4, 2026-05-07) but stale since: Dispatch grew to three slices, Telemetry shipped as a second service with code (PR #42, 2026-07-10), and three more bounded contexts (Trips, Onboarding, Identity) gained completed Event Modeling workshops without ever having code. The post-PR-42 handoff flagged this drift explicitly as a `tidy: housekeeping` candidate. This is the front door of the repo — the file every session and every human visitor reads first — so its accuracy matters disproportionately to its size.

---

## Goal

Replace the stale status paragraph with one that accurately reflects: which services run in code (Dispatch, Telemetry) and their slice counts, which bounded contexts have completed design work but no code (Trips, Onboarding, Identity), and which remain pre-workshop (Rider Profile, Driver Profile, Pricing, Payments, Ratings, Operations).

---

## Working pattern

- Single-file content edit (`CLAUDE.md`), plus the standard prompt/retro/index bookkeeping.
- No opportunistic edits to other files — the CI Actions version bump surfaced in the same handoff is out of scope here (separate `tidy: ci` session).
- Keep the replacement to one paragraph, matching the file's stated role ("routing layer, not a manual") — point to `docs/vision/README.md` for full detail rather than enumerating there.

---

## Deliverable plan

1. `CLAUDE.md` — replace the second paragraph of the intro (the stale status line) with an accurate one-paragraph summary.
2. This prompt file.
3. `docs/prompts/README.md` — new entry under "Multi-artifact prompts (root)".
4. `docs/retrospectives/refresh-claude-md-telemetry-status.md` — retro.
5. `docs/retrospectives/README.md` — matching entry.

---

## Out of scope

- CI Actions v4→v5 bump (separate `tidy: ci` session, same handoff's housekeeping list).
- Deleting stale merged remote branches (administrative, not a code session).
- Any further vision-doc or workshop content changes — this session touches only `CLAUDE.md`'s routing-layer status line.
