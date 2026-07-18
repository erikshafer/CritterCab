# Retrospective — CI: Bump Pinned GitHub Actions off the Node 20 Deprecation

## Metadata

- **Triggering prompt:** [`docs/prompts/ci-bump-actions-node-deprecation.md`](../prompts/ci-bump-actions-node-deprecation.md)
- **Status:** Complete
- **Date authored:** 2026-07-17
- **Output artifacts:**
  - `.github/workflows/dotnet.yml` — four `uses:` version bumps
  - `docs/prompts/ci-bump-actions-node-deprecation.md` (new) — the triggering prompt
  - `docs/prompts/README.md` — new entry under "Multi-artifact prompts (root)"
  - `docs/retrospectives/ci-bump-actions-node-deprecation.md` (new, this file)
  - `docs/retrospectives/README.md` — matching entry
- **Outcome:** `actions/checkout`, `actions/setup-dotnet`, `actions/cache`, and `actions/upload-artifact` bumped from `@v4` to their current latest majors (`v7`, `v6`, `v6`, `v7` respectively), resolving the Node 20 deprecation CI had been annotating since at least PR #41.

---

## Framing

First `tidy: ci` session. Flagged twice — by the package-refresh retro and the post-PR-42 handoff — as a non-blocking CI annotation worth clearing. Picked up as part of a small housekeeping sweep alongside the `CLAUDE.md` status-line fix, while the Telemetry gRPC chain sits externally gated on an upstream Wolverine fix.

---

## Outcome summary

| Action | Before | After |
|---|---|---|
| `actions/checkout` | `v4` | `v7` |
| `actions/setup-dotnet` | `v4` | `v6` |
| `actions/cache` | `v4` | `v6` |
| `actions/upload-artifact` | `v4` | `v7` |

Verified live against GitHub (`gh api repos/<org>/<repo>/tags` and `.../releases/latest`) rather than trusting the "v5" figure the source retros mentioned in passing — that number was already stale by the time this session ran (two of the four actions have moved past v5 to v6/v7). Bumping straight to current latest avoids re-deferring the same annotation in a future session.

---

## What worked

- **Checking live tags instead of trusting the prior retro's casual version mention.** The source handoffs said "bump to v5" as a rough gesture, written when v5 was current; by 2026-07-17 that was already behind. A quick `gh api` check turned an assumption into a verified fact before it shipped.

---

## What was harder than expected

- **No local test path for a GitHub Actions version bump.** Unlike the package refresh (which could restore + build + unit-test locally before pushing), a workflow-file change is only verifiable by CI itself. Verification is deferred to the PR's CI run rather than a pre-push local check.

---

## Methodology refinements that emerged

- **Don't trust a stale version number carried in an old retro/handoff, even when it's the reason the session exists.** Re-verify against the live source (here, GitHub's own tag/release API) before committing to a specific version — the same discipline `jasperfx-source-verifier` applies to Wolverine/Marten API claims applies just as well to third-party Action pins.

---

## Outstanding items / next-session inputs

- **CI run for this PR is the verification step** — confirm green before considering this session's outcome final. If the workflow's `Verify solution completeness` step or any downstream step breaks under the new majors, that's a same-PR fix (still in-scope: the file this session already touches).
- **NU1903 (`Microsoft.OpenApi` vuln)** — already resolved separately (pinned to 2.7.5 in a prior direct package bump), not carried forward as an item here.

---

## Spec delta — landed?

**Null, as planned.** No narrative or workshop is amended — CI workflow configuration is infrastructure, not a canonical spec artifact.
