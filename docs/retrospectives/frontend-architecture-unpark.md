# Retrospective — Frontend Architecture Unpark (Vision v0.6 + ADR-016)

| Field | Value |
|---|---|
| **Triggering prompt** | [`docs/prompts/frontend-architecture-unpark.md`](../prompts/frontend-architecture-unpark.md) |
| **Status** | Complete |
| **Date** | 2026-06-16 |
| **Output artifacts** | [`docs/decisions/016-frontend-live-update-transport.md`](../decisions/016-frontend-live-update-transport.md) (new); [`docs/decisions/README.md`](../decisions/README.md) (ADR-016 row); [`docs/vision/README.md`](../vision/README.md) (v0.6 — stack/monorepo direction, two parked items re-filed, three open questions added, Document History entry); [`docs/retrospectives/README.md`](../retrospectives/README.md) (this retro's index entry) |
| **Outcome** | ADR-016 Accepted (SignalR). Vision bumped to v0.6. Two parked frontend items retired. Three new open questions filed. Two successor sessions named (map-library spike; contracts-package session). |

---

## Framing

Design-return session per ADR-004, triggered by the sibling-repo frontend survey discharging the vision's explicit "CritterBids lands on a stable live-update pattern" unpark condition. The session's job was judgment, not research: decide what the vision adopts as working direction and record the transport decision in an ADR.

---

## Outcome summary

| Deliverable | Result |
|---|---|
| ADR-016 — Frontend Live-Update Transport | Authored and Accepted. SignalR (`@microsoft/signalr` v10) with transport-agnostic push→Query-cache-bridge architecture. |
| Vision v0.6 | Bumped. Frontend stack + audience-SPA monorepo adopted as working direction in §Tentative Technology Stack. |
| "Frontend architecture" — re-filed | Removed from §Explicitly Parked. Discharge cited in v0.6 Document History. |
| "Map library for frontend" — re-filed | Removed from §Explicitly Parked. Added to §Open Questions with candidates (MapLibre GL, Leaflet, deck.gl) and backend-coupling note. |
| §Open Questions — three new entries | Map library; contracts-package shape (one `@crittercab/shared` vs. per-BC); monorepo vs. separate repos. |
| Successor sessions named | Map-library spike; contracts-package/monorepo-shape session at first frontend implementation. |

---

## What worked

**grill-with-docs surfaced the load-bearing question early.** The first grill question — "does ADR-005 already place browser-client streaming in the gRPC column?" — immediately exposed the most important distinction the ADR had to make: ADR-005 governs service-to-service transport; browser-to-backend push is a distinct fourth category. Without that clarification, ADR-016 would have been written with the wrong framing.

**The transport decision resolved cleanly and fast.** The prompt's lean was "Accepted with gRPC-web as default, SignalR as fallback, pending spike." The user rejected gRPC-web as too niche and experimental before the grill reached question 2, collapsing four grilling questions to one answer: SignalR, Accepted, no spike, no gate. The decision is cleaner than the lean anticipated.

**Decision-at-a-time sign-off kept each call crisp.** Five decisions, each pre-authored with a lean and a citation, walked sequentially. No batching, no ambiguity about what was being decided. The two moot decisions (gRPC-web spike; fallback alignment with ADR-005) were dropped cleanly rather than argued through.

**The transport-agnostic bridge architecture survived the transport flip.** The core architectural pattern from the survey (push→Query-cache-bridge with the transport parameterised at `createConnection`) transferred intact regardless of which transport filled the slot. Committing to the architecture separately from the transport was the right structure.

---

## What was harder than expected

**The prompt's lean on flag #1 was overthought.** The "Accepted-with-default-gRPC-web-pending-spike" shape never came up in practice — the user resolved the transport question categorically before the grill reached it. The lean anticipated a tension that the user had already resolved. Useful to surface, but the sign-off pattern worked better than the multi-step fallback structure suggested.

**The vision doc header was stale.** The version header said "v0.4" when v0.5 had already been applied (a missed header update from the W003 session). Fixed incidentally during the v0.6 bump.

---

## Methodology refinements that emerged

**The grill-with-docs "first question" carries disproportionate leverage.** In this session the first question — scope of ADR-005 — collapsed the rest of the grilling tree. Investing in identifying the most foundational dependency-breaking question before starting the grill is worth explicit effort.

**Prompt leans should acknowledge that the user may have already resolved the tension.** The flag #1 lean spent significant prose on a three-option fallback structure for a decision the user had already made. A shorter lean — "our read is SignalR-or-gRPC-web; here's the tension; your call" — would have been more efficient.

---

## Outstanding items / next-session inputs

- **Map-library spike** — named successor session. Trigger: when Telemetry BC's geospatial representation (H3 cells vs. GeoJSON) is decided; spike is a throwaway implementation, not a research doc.
- **Contracts-package + monorepo shape** — named successor session at first frontend implementation. Trigger: when the first SPA package is bootstrapped. Two open questions (contracts-package shape; monorepo vs. separate repos) are filed in the vision doc and will be decided in that session.
- **SignalR hub design** — not started. Which services own hub endpoints, how Wolverine message handlers fan out to hub contexts, and whether a BFF layer is needed are all deferred to the first frontend implementation session.

---

## Spec delta — landed?

Null narrative/workshop spec delta, as planned and honestly named in the prompt. This is a vision-level design-return; no narrative or workshop spec was amended. Deltas land at the vision/decision layer: vision doc gains §Tentative Technology Stack frontend section and loses two §Explicitly Parked entries; decision record gains ADR-016. The null-delta pattern is established precedent (fifth instance: housekeeping-delete-may-15-handoff; skills-tidy-ai-skills-sync; workshops/003 retro; aspire-reconcile-and-port-band; fix-marten9-projection-source-gen).
