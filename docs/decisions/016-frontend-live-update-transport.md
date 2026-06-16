# ADR-016: Frontend Live-Update Transport

**Status:** Accepted  
**Date:** 2026-06-16

## Context

CritterCab's three audience-facing frontend surfaces — the rider app, the driver app, and the ops dashboard — require real-time push updates from the backend:

- The driver app receives offer deliveries and trip-state changes as they occur.
- The rider app watches trip progress during an active ride.
- The ops dashboard renders a live map of active drivers and trips.

None of these surfaces are satisfied by request/response polling: the latency is too high for offer delivery, and polling a live map at the required refresh rate is wasteful. A server-push transport is required.

The choice of browser-side push transport is governed by a different set of constraints than the service-to-service choices in ADR-005. ADR-005 covers gRPC (service-to-service calls and streaming), Kafka (high-volume telemetry), and Azure Service Bus (business events). Browser-to-backend push is a distinct fourth category: a client that cannot run a native gRPC stack, connecting over HTTP/HTTPS to a server that must push updates as they occur. ADR-005's "transport split by flow type" principle applies — the question is which transport fits the browser-client push shape — but the options differ.

The [sibling-repo frontend survey](../research/frontend-survey-sibling-repos.md) identified a reusable architectural pattern from CritterBids: a transport-agnostic push→Query-cache-bridge where the component contract (`useListen`, `useConnectionState`, `applyMessage`) is decoupled from the underlying transport. The transport is plugged in at the `createConnection` seam; components do not know whether the bytes arrived over SignalR or any other mechanism. This architecture is the primary transferable asset from the survey — the transport slot is a separate decision.

The survey also named a tension: CritterBids uses SignalR (proven), while CritterCab's primary goal of showcasing Wolverine's gRPC feature set (vision §Goals Primary #1) raised gRPC-web streaming as the on-thesis alternative. Both were evaluated.

## Options Considered

### Option A — SignalR (`@microsoft/signalr` v10)

SignalR provides a managed WebSocket hub abstraction over ASP.NET Core. It handles connection negotiation, reconnect lifecycle, and hub method invocation. For authenticated surfaces, it accepts a bearer token via `accessTokenFactory`, which maps to CritterCab's Entra-issued token acquisition (ADR-006) without additional ceremony.

CritterBids already ships a generic `createSignalRProvider<TMessage>` that returns a `{ Provider, useHub, useListen, useConnectionState }` quad shared across three SPAs. The pattern is proven, actively maintained, and part of the existing muscle memory across multiple repositories.

The cost: SignalR routes the most visible real-time frontend surfaces through a Microsoft-specific WebSocket framework rather than through gRPC. For the browser-client category, this is a deliberate pragmatic departure from the on-thesis path.

### Option B — gRPC-web streaming

gRPC-web is a browser-compatible variant of gRPC using HTTP/1.1 framing or HTTP/2 without server push. ASP.NET Core supports it natively via `UseGrpcWeb()`. Choosing gRPC-web would align the browser transport with the backend gRPC surfaces.

The cost: gRPC-web is niche and experimental in the browser context. Neither sibling repository has shipped it. Ergonomics — tooling maturity, streaming reconnect semantics, React DX with generated TypeScript clients — are unproven in this stack. The on-thesis argument is real but the gRPC thesis is demonstrated convincingly by the backend service mesh; the browser transport does not need to carry the same demonstration burden.

### Option C — Raw WebSockets or Server-Sent Events

Direct WebSocket connections or SSE streams without a framework. Lowest ceremony to establish, but requires hand-rolling connection management, reconnect logic, and message dispatch. The ops live map's fan-out requirements and the driver app's reconnect-after-gap semantics make the missing infrastructure costs real, not theoretical.

## Decision

**Option A.** CritterCab adopts **SignalR** (`@microsoft/signalr` v10) as the browser-client live-update transport for the rider app, driver app, and ops dashboard.

The architectural pattern adopted is the transport-agnostic push→Query-cache-bridge from CritterBids' `createSignalRProvider<TMessage>`:

- The `Provider` is parameterised by behavior — `createConnection`, `parseMessage(payload) → TMessage | null`, and `applyMessage(queryClient, message)` — not by protocol.
- On each inbound message, the bridge writes the result into the TanStack Query cache via `applyMessage`, then fans out to imperative listeners.
- `useConnectionState` surfaces `HubConnectionState` + `lastError` for UI resilience.
- The `onReconnected(connection, queryClient)` seam handles cache invalidation after a reconnect gap — the ride-sharing analogue of state-digest reconciliation when a driver or rider app reconnects after a network drop.

Connection construction follows CritterBids' anonymous/token split: an anonymous connection (auto-reconnect, default negotiation) for any public surfaces; a token connection (`WebSockets` + `skipNegotiation` + `accessTokenFactory` acquiring Entra-issued tokens per ADR-006) for authenticated rider, driver, and ops surfaces.

The gRPC thesis (vision §Goals Primary #1) is demonstrated by the backend service mesh — service-to-service calls, server-streaming offer delivery between services, client-streaming GPS ingest. ADR-005's three-transport commitment governs those flows. SignalR governs the distinct browser-client push category without conflicting with ADR-005's scope.

Consistency with the proven house stack (shared with CritterBids) is an explicit factor: the pattern and its tooling are already exercised across multiple active projects, reducing the marginal integration risk to near zero.

## Consequences

The component contract (`useListen`, `useConnectionState`, `applyMessage`) is transport-agnostic. Components in the rider, driver, and ops SPAs do not couple to SignalR directly; they read from the TanStack Query cache and call the hook surface. The `createConnection` seam is explicitly left open: the transport is swappable at that seam without touching component code.

SignalR hub endpoints live on the ASP.NET Core host of the relevant backend service (or a dedicated BFF layer). They are standard ASP.NET Core SignalR hubs, not Wolverine endpoints. Backend-to-hub fan-out uses Wolverine message handlers or background services publishing to the hub context, keeping the Wolverine handler graph intact on the server side.

The ops dashboard's live map requires higher-frequency driver location updates than the rider or driver apps. SignalR's WebSocket transport handles this at expected ops-dashboard concurrency (tens of simultaneous ops users).

The shared contracts package shape — one `@crittercab/shared` or per-BC packages — is explicitly deferred to the first frontend implementation session. See §Open Questions in the vision doc.

Cross-references: [ADR-005](./005-transport-selection-by-flow-type.md) (transport precedent; browser-client push is the fourth category not covered by ADR-005), [ADR-006](./006-identity-provider-as-swappable-anti-corruption-layer.md) (token acquisition for `accessTokenFactory` on authenticated hub connections).
