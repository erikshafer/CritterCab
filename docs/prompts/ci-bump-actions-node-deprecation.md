# Prompt — CI: Bump Pinned GitHub Actions off the Node 20 Deprecation

| Field | Value |
|---|---|
| **Status** | Complete (2026-07-17) |
| **Authored** | 2026-07-17 |
| **Target artifacts** | `.github/workflows/dotnet.yml`, `docs/prompts/ci-bump-actions-node-deprecation.md` (this prompt), `docs/prompts/README.md` (index update), `docs/retrospectives/ci-bump-actions-node-deprecation.md` (retro), `docs/retrospectives/README.md` (index update) |
| **Source-of-truth dependencies** | `docs/retrospectives/critter-stack-package-refresh.md` ("CI annotates a Node.js 20 deprecation on the pinned GitHub Actions... a future `tidy: ci` could bump those"); `docs/planning/2026-07-10-post-pr42-telemetry-next-pr-b-handoff.md` (repeats the same flag); GitHub's `actions/*` tag/release history (checked live via `gh api repos/<org>/<repo>/tags` and `.../releases/latest`, 2026-07-17) |
| **Workflow position** | Standalone `tidy: ci` micro-PR — first instance of this tidy area (per `docs/prompts/README.md` § Commit subjects, a new area is fine as a one-off without joining the established list). |

---

## Spec delta

None. CI workflow configuration is infrastructure, not a canonical spec artifact — no narrative or workshop is amended.

---

## Framing

Two prior retros (`critter-stack-package-refresh.md`, the post-PR-42 handoff) both flagged the same non-blocking CI annotation: `actions/checkout@v4`, `actions/setup-dotnet@v4`, `actions/cache@v4`, and `actions/upload-artifact@v4` are all pinned to Node 20-based runner versions that GitHub has deprecated. Picked up now as part of a small housekeeping sweep while the Telemetry gRPC chain sits externally gated on an upstream Wolverine fix.

---

## Goal

Bump each of the four pinned Actions to its current latest major version, fully resolving the Node 20 deprecation rather than leapfrogging one major and re-deferring.

---

## Working pattern

- Verified current latest major tags live against GitHub (not assumed from the original retro's "v5" mention, which was already one-to-two majors stale by 2026-07-17): `actions/checkout` → v7, `actions/setup-dotnet` → v6, `actions/cache` → v6, `actions/upload-artifact` → v7.
- Single-file workflow edit; no application code touched.
- Verification happens via CI itself (the next push/PR run) rather than local emulation — GitHub Actions version bumps aren't locally testable without `act` or similar, which this repo doesn't use.

---

## Deliverable plan

1. `.github/workflows/dotnet.yml` — four `uses:` version bumps.
2. This prompt file.
3. `docs/prompts/README.md` — new entry under "Multi-artifact prompts (root)".
4. `docs/retrospectives/ci-bump-actions-v5.md` — retro, confirming the CI run's outcome once observed.
5. `docs/retrospectives/README.md` — matching entry.

---

## Out of scope

- CLAUDE.md status-line fix (separate `tidy: housekeeping` session, same source handoff's housekeeping list).
- Deleting stale merged remote branches (administrative, not a code session).
- The pre-existing NU1903 (`Microsoft.OpenApi` vuln) advisory — already resolved separately (pinned to 2.7.5 in a prior direct package bump), not part of this session's scope.
- Any further CI workflow changes (matrix builds, additional jobs, caching strategy) beyond the version bump itself.
