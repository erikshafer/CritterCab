# Frontend Decisions: A Survey of Sibling Repositories

Curated research notes from the **frontends of two sibling repositories** in the author's `C:\Code` workspace — `mmo-reconnect` and `CritterBids` — read directly at the source level. The focus is the tech and design decisions those frontends have already made, so that CritterCab can adopt the convergent ones deliberately, depart from the divergent ones with eyes open, and resolve the one genuine tension (live-update transport) on its own terms rather than inheriting an answer that conflicts with its thesis.

This document is **survey output only**. It alters no CritterCab artifact. It exists to make the eventual unpark of the [vision doc's parked frontend decision](../vision/README.md) (`§Explicitly Parked → Frontend architecture`) a reasoned step rather than a cold start. The companion design-return session — bumping the vision and resolving these decisions in an ADR — is deliberately deferred to its own session.

---

## About this document

### Why these two repos, and why "authoritative"

The author has three sibling frontends in flight (`mmo-reconnect`, `CritterBids`, `crittermart`). For this survey, **MMO Reconnect and CritterBids were treated as authoritative** and `crittermart` was excluded, on the author's assessment that the first two carry the most refined decisions and implementations. The two chosen repos are also usefully *different kinds of app*, which is what makes the comparison productive: one is a content/discovery site, the other a real-time multi-surface SPA.

### What counts as a source here

Unlike the ride-sharing lessons doc, the sources here are not blog posts — they are **the repositories' own files**, read on 2026-06-16. Every claim below traces to one of:

**MMO Reconnect** (`C:\Code\mmo-reconnect\src\web`):
- `package.json` — the dependency and script surface.
- `src/index.css` — the Tailwind v4 `@theme` brand tokens.
- `src/api.ts` — the data layer (fetch + TanStack Query).
- `src/App.tsx` — the hand-rolled routing decision.
- `src/` tree — component/page organization and colocated `.test.tsx` files.

**CritterBids** (`C:\Code\CritterBids\client`):
- `package.json` (workspace root) + `bidder/`, `ops/`, `seller/`, `shared/` workspace manifests.
- `shared/src/signalr/provider.tsx` — the generic SignalR→Query provider factory.
- `shared/src/signalr/connection.ts` — the anonymous vs. token connection builders.
- `shared/src/theme.css` — the shared shadcn/ui Tailwind v4 CSS-variable theme.
- ADR references cited *in the manifests' own descriptions* (ADR 004/012/013/024/025/026), read as labels rather than opened in full.

### How to read the "applicability to CritterCab" notes

Each finding closes with an applicability note. These are opinionated but non-prescriptive: they identify where a sibling decision transfers cleanly, where it must be adapted for ride-sharing, and — most importantly for the transport question — where CritterCab should deliberately *not* follow a sibling's path because doing so would undercut the project's reason to exist.

---

## Headline finding — the parked trigger has already fired

CritterCab's vision parks the frontend with an explicit unpark condition (`docs/vision/README.md` §Explicitly Parked):

> **Frontend architecture.** Live-update transport (SignalR, gRPC-web streaming, or WebSockets), component library, and app structure are deferred pending lessons from the CritterBids frontend work. **The trigger is: CritterBids lands on a stable live-update pattern**, or CritterCab reaches the point where a driver or rider UI is needed to make further backend progress meaningful.

It has landed one. `CritterBids/client/shared/src/signalr/provider.tsx` is a generic, packaged, reusable live-update pattern — `createSignalRProvider<TMessage>(displayName)` returning a `{ Provider, useHub, useListen, useConnectionState }` quad, shared across three SPAs via the `@critterbids/shared` workspace. The trigger condition is satisfied on the doc's own terms. An unpark is now *justified*, not merely *desired*.

---

## Finding 1 — The two repos share a de-facto house stack

Both frontends ride the leading edge of the React ecosystem, and they converge almost exactly:

| Concern | MMO Reconnect | CritterBids (all SPAs) |
|---|---|---|
| Framework | React 19.2 | React 19.2 |
| Build | Vite 8 + `@vitejs/plugin-react` (Oxc) | Vite 8 + `@vitejs/plugin-react` (Oxc) |
| Language | TypeScript ~6.0 | TypeScript ^6.0 |
| Styling | Tailwind v4 (`@tailwindcss/vite`), `@theme` tokens, no `tailwind.config.js` | Tailwind v4 (`@tailwindcss/vite`), CSS-variable theme |
| Server state | TanStack Query v5.101 | TanStack Query v5.101 |
| Tests | Vitest 4 + Testing Library + jsdom | Vitest 4 + Testing Library + jsdom |
| Lint | ESLint flat config (`typescript-eslint`) | ESLint flat config (`typescript-eslint`) |
| Node | (unset) | `>=22` (engines) |

This convergence is itself the most important finding. It means CritterCab is not choosing a frontend stack from scratch — it is choosing whether to **standardize on a stack the author already has working muscle memory for across two repositories**. The marginal risk of adopting React 19 / Vite 8 / TS 6 / Tailwind v4 / TanStack Query is near zero, because both siblings prove it in anger.

### Applicability to CritterCab

- Adopt the convergent stack as CritterCab's frontend baseline. The decision cost is low and the consistency dividend (shared tooling, shared review instincts, portable snippets) is real.
- The one number worth a deliberate nod: **Node `>=22`** is pinned in CritterBids' workspace root. CritterCab should pin a Node floor explicitly rather than inherit whatever is installed.

---

## Finding 2 — The divergences track domain shape, not fashion

The two apps differ in eight load-bearing ways, and every difference is explained by what the app *is* rather than by taste drift:

| Decision | MMO Reconnect (discovery/content site) | CritterBids (live auction platform) |
|---|---|---|
| App shape | Single app | **Monorepo**: `bidder` / `ops` / `seller` SPAs + `shared` + `e2e` (ADR 025) |
| Real-time | None — request/response + Query `staleTime` | **SignalR** v10, generic provider → Query cache bridge |
| Routing | **Hand-rolled path switch** in `App.tsx`; explicit YAGNI on a router | **TanStack Router** |
| Rendering | **SSR + prerender** (SSG public routes, client-only authed routes) | Pure SPA + **PWA** (`vite-plugin-pwa`) |
| Wire contracts | Hand-written TS interfaces mirroring server DTOs, with inline provenance | **Zod schemas** in `shared` — *"the frontend analogue of CritterBids.Contracts"* |
| Component layer | Bare Tailwind, hand-rolled components | **shadcn/ui** stack: `class-variance-authority` + `clsx` + `tailwind-merge` |
| Auth / transport | Cookie session (`credentials: 'include'`), Discord OAuth full-page redirect | **Bearer token** via SignalR `accessTokenFactory`; `WebSockets` + `skipNegotiation` |
| Forms | Native form + coded-error→copy mapping | **react-hook-form** + `@hookform/resolvers` (Zod) in `seller` |

The discipline shows in MMO Reconnect's `App.tsx`, which documents its own restraint:

> *"Hand-rolled path switch. ... A real client router arrives with the path-param routes ... — YAGNI until then (decided S1.1; revisited S3.1)."*

That is a deliberately *under*-engineered choice, justified inline and dated to the session that made it. The lesson for CritterCab is not "hand-roll your router" — it is "let the app's actual route shape decide, and record the decision where the next reader will find it."

### Applicability to CritterCab

- **CritterCab is closer to CritterBids than to MMO Reconnect.** Live trip/dispatch state, a driver app watching offers, a rider app watching trip status, an ops live map — these are real-time surfaces, so CritterBids' choices (a router, push transport, a contract package, a component system) transfer, and MMO Reconnect's SSG/cookie/no-router choices apply mainly to any *public marketing* surface CritterCab grows later.
- **Adopt shadcn/ui + Zod-contracts from CritterBids, not MMO Reconnect's hand-rolled equivalents.** At CritterCab's intended surface count, the component system and a typed wire-contract package pay for themselves; MMO Reconnect skipped them because a single small content app didn't need them.

---

## Finding 3 — The reusable asset is the transport→cache bridge, not "SignalR"

The single most portable piece of engineering across these repos is CritterBids' `createSignalRProvider<TMessage>`. Its shape matters more than its transport:

- The factory takes a message type parameter and a `displayName`, and returns a `Provider` plus three hooks (`useHub`, `useListen`, `useConnectionState`).
- The `Provider` is parameterised by **behavior, not protocol**: `createConnection`, `parseMessage(payload) → TMessage | null`, and crucially `applyMessage(queryClient, message)`.
- On each inbound message it parses, **writes the result into the TanStack Query cache** via `applyMessage`, then fans out to imperative listeners.
- It owns reconnect lifecycle (`onreconnecting` / `onreconnected` / `onclose`), surfaces a `HubConnectionState` + `lastError`, and exposes an `onReconnected(connection, queryClient)` seam for cache invalidation after a gap.

The connection construction is split deliberately (`connection.ts`): `createAnonymousConnection` (auto-reconnect, default negotiation) for public hubs, and `createTokenConnection` (`WebSockets` + `skipNegotiation` + `accessTokenFactory`) for authenticated hubs.

The reason this is the key asset: **the component contract is transport-agnostic.** Components call `useListen` / `useConnectionState` and read from the Query cache. They do not know the bytes arrived over SignalR. Swap `createConnection` + `parseMessage` + `applyMessage` for a gRPC-web streaming reader and the entire component layer is unchanged.

### Applicability to CritterCab

- **Lift the pattern, parameterise the transport.** CritterCab can keep the exact provider/hook shape (push transport → `applyMessage` → Query cache → declarative components) while supplying a gRPC-web streaming `createConnection` instead of a SignalR `HubConnection`.
- The **anonymous-vs-token connection split** maps directly onto CritterCab's surfaces: an ops dashboard or authed rider/driver app needs `accessTokenFactory` against Entra-issued tokens; a public surface does not.
- The **`onReconnected` cache-invalidation seam** is exactly the hook CritterCab will need for "driver app reconnects after a tunnel and must reconcile the offers/trip state it missed" — the ride-sharing analogue of the State-Digest reconciliation noted in [ride-sharing-lessons-learned.md](./ride-sharing-lessons-learned.md) Lesson 10.

---

## Finding 4 — CritterBids' monorepo segmentation maps 1:1 onto CritterCab's audiences

CritterBids' `client` is an npm-workspaces monorepo (ADR 025) with three audience-segmented SPAs over one shared package:

- `bidder` — the buyer-facing app.
- `ops` — the staff operations dashboard (separate `StaffToken` auth gate, `OperationsHub`, six dashboard views over `/api/operations/*` with an ADR-026 "cache bridge").
- `seller` — the seller console (adds `react-hook-form` for richer forms).
- `shared` — the wire-contract surface: parameterised SignalR provider/hooks, the shared Tailwind v4 theme, and Zod schemas. Explicitly described as *"the frontend analogue of CritterBids.Contracts."*
- `e2e` — a Playwright harness.

The segmentation is by audience, and each SPA is independently buildable (`tsc --noEmit && vite build`) while sharing theme, contracts, and transport plumbing through `@critterbids/shared`.

### Applicability to CritterCab

- The audience mapping is almost mechanical: **`bidder / ops / seller` → `rider / driver / operations`.** CritterCab's vision already names rider, driver, and Operations surfaces; the monorepo shape gives each its own deployable SPA with a shared contracts/theme/transport core.
- The **`shared` package as "the frontend analogue of the backend Contracts assembly"** is the most architecturally resonant idea for CritterCab, because it expresses in TypeScript exactly what CritterCab's context map already says in DDD vocabulary: published languages translate at the boundary. A `@crittercab/shared` (or per-published-language packages) would be the frontend embodiment of that principle — though CritterCab must decide whether *one* shared package or *per-BC* contract packages better fits its separately-deployed-services stance (see Open Questions).
- The **independent-build-per-SPA** property aligns with CritterCab's "separately deployable" ethos better than a single bundled app would.

---

## Finding 5 — The one tension CritterCab cannot inherit: SignalR vs. gRPC-web

CritterBids chose **SignalR** as its live-update transport, and it works well. But CritterCab's *entire reason to exist* is showcasing **Wolverine's gRPC feature set** (vision §Goals, Primary #1), and the parked note explicitly lists **gRPC-web streaming** as a candidate transport alongside SignalR and WebSockets.

This is a genuine conflict, not a detail:

- Adopting SignalR wholesale would give CritterCab a proven, low-risk path — and would simultaneously route the most visible real-time surfaces *around* the very technology the project was built to demonstrate.
- Choosing gRPC-web keeps the frontend on-thesis (the browser exercises the same gRPC story as the backend), but it is **unproven in either sibling repo** — neither has shipped gRPC-web in the browser, so this is net-new spike territory with real unknowns (proxy/Envoy or Wolverine-native gRPC-web support, streaming ergonomics, reconnect semantics, tooling maturity).

The transferable lesson is the **architecture** (Finding 3: transport → cache bridge → declarative components), with the **transport slot left deliberately open** for a gRPC-web decision. This must be made explicitly in an ADR with the tradeoff named — proven-but-off-thesis (SignalR) versus on-thesis-but-unproven (gRPC-web) — not defaulted to CritterBids' answer because it is the answer already sitting in a sibling repo.

### Applicability to CritterCab

- Treat live-update transport as an **open ADR decision**, seeded by but not bound to CritterBids' SignalR choice.
- A **gRPC-web spike** is a prerequisite to deciding honestly. Until the project knows whether gRPC-web streaming into a React app is ergonomic with the Critter Stack, the SignalR-vs-gRPC-web tradeoff is being argued in the abstract.

---

## Gaps neither sibling answers for CritterCab

Two CritterCab needs are unaddressed by either repo, because neither's domain required them:

1. **Map library.** Ride-sharing is map-centric (live driver positions, pickup/dropoff, route, ops live map). Neither MMO Reconnect nor CritterBids has a geospatial UI, so neither offers a tested choice (MapLibre GL, Leaflet, deck.gl, Mapbox GL, Google Maps). This remains parked in the vision (`§Explicitly Parked → Map library for frontend`) and is now the **most CritterCab-specific open frontend decision** — the siblings cannot retire it. It also couples to the backend H3/geospatial choices already noted in [ride-sharing-lessons-learned.md](./ride-sharing-lessons-learned.md) Lesson 4.

2. **gRPC-web in the browser.** As Finding 5 details, neither repo has exercised gRPC-web. This is both a gap and the crux of the transport decision.

Both gaps are **discovery/spike work**, not decisions that can be lifted from a sibling. They are the parts of CritterCab's frontend that are genuinely novel relative to the author's prior work.

---

## Open questions surfaced for CritterCab

Questions worth carrying into the frontend-unpark design-return session (and, where they survive, onto the vision doc's open-questions list):

1. **Live-update transport: SignalR, gRPC-web streaming, or WebSockets?** The defining decision. On-thesis (gRPC-web) vs. proven (SignalR). Requires a spike before it can be decided honestly. (Finding 5.)
2. **One shared contracts package, or per-BC packages?** CritterBids uses a single `@critterbids/shared`. CritterCab's separately-deployed-services stance and BC-owned-language convention may argue for per-published-language packages instead. (Finding 4.)
3. **Monorepo or separate frontend repos?** CritterBids co-locates three SPAs in one `client` workspace. CritterCab's "separately deployable per BC" ethos raises the question of whether the frontends should likewise be separable — and whether a shared package across repo boundaries is worth the tooling cost.
4. **Map library choice**, and whether the choice is driven by the backend geospatial representation (H3 cells, GeoJSON) it must render. (Gaps #1.)
5. **Rendering model per surface.** MMO Reconnect's SSG-for-public / client-for-authed split is a good template if CritterCab grows a marketing surface; the rider/driver/ops apps are SPA-shaped. Worth an explicit per-surface decision rather than one global answer.
6. **Auth integration shape.** CritterBids uses bearer tokens via `accessTokenFactory`; CritterCab's Entra External ID + swappable-provider stance (ADR 006) means the token-acquisition layer is its own concern the siblings only partially model.

---

## Recommended next step

A single design-return session that:

1. **Unparks the frontend** in `docs/vision/README.md`, bumping it to v0.6, adopting the convergent stack (Finding 1) and the audience-SPA monorepo shape + transport→cache-bridge pattern (Findings 3–4) as the working direction.
2. **Opens an ADR for the live-update transport decision** (Finding 5), naming the SignalR-vs-gRPC-web tradeoff and gating it on a gRPC-web spike rather than resolving it by default.
3. **Re-files the map-library and gRPC-web items** from "parked, awaiting CritterBids" to "open, awaiting a CritterCab spike," since the CritterBids-dependent trigger has now been discharged by this survey.

The transport decision deserves its own ADR with the tradeoff named, not a paragraph folded into a vision bump — which is why the unpark is scoped as a separate session from this survey.

---

## Document history

- **v0.1** (2026-06-16): Initial survey. Primary sources: the `mmo-reconnect` (`src/web`) and `CritterBids` (`client`) frontends, read at the source level on 2026-06-16; `crittermart` excluded per the author's refinement assessment. Five findings + two gaps + six open questions + a recommended unpark sequence. Records that the vision doc's parked-frontend trigger ("CritterBids lands on a stable live-update pattern") is now satisfied. Intended to seed a separate design-return session that bumps the vision to v0.6 and opens the live-update-transport ADR.
