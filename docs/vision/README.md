- **v0.3** (2026-04-23): Cross-referenced ADRs 001–009 throughout. Committed Azure as the deployment platform (ADR-007) and Azure Service Bus as a planned transport (ADR-005). Removed resolved items from Open Questions and Explicitly Parked. Added ADR references to Design Principles.# CritterCab Vision

## What This Is

CritterCab is a reference architecture for a ride-sharing platform, built on the Critter Stack (Wolverine, Marten, Polecat, Weasel, Alba) for .NET. It joins CritterSupply and CritterBids as the third showcase in a series, each chosen to illustrate a different slice of the Critter Stack's capabilities against a different domain.

Where CritterSupply explores a modular monolith for e-commerce and CritterBids explores saga-driven auction orchestration, CritterCab focuses on distributed, event-driven services communicating over gRPC. The project exists in large part to showcase Wolverine's gRPC feature set, which shipped in Wolverine 5.32. The ride-sharing domain was chosen because its natural shape (driver location streaming, rider-driver matching, trip lifecycle) exercises every mode of gRPC communication while providing room for event sourcing, high-volume telemetry, and multi-transport messaging.

This document captures the current state of our thinking as **version 0.7**. Bounded contexts, technology choices, and design principles will shift as Event Modeling and real implementation pressure the design. When they do, this document gets updated, and significant decisions get recorded as ADRs in [docs/decisions](../decisions).

## Goals

### Primary

1. **Showcase Wolverine's gRPC feature set.** CritterCab's existence is driven by the Wolverine 5.32 gRPC release. The project exercises gRPC in all four modes (unary, server-streaming, client-streaming, bidirectional) against natural domain use cases: driver GPS streaming, dispatch offer fan-out, ride requests, and real-time driver-rider interactions.
2. **Demonstrate how event-driven and event-sourced patterns compose with gRPC.** The Critter Stack's existing strengths (event sourcing in Marten, messaging in Wolverine, document storage in Polecat) do not go away because gRPC joined the party. CritterCab shows how synchronous gRPC surfaces and asynchronous event-driven flows cooperate inside a single system.

### Secondary

1. **First-hand experience with Wolverine's Kafka transport.** Kafka is a goal, not an accident. High-volume telemetry (GPS pings, trip breadcrumbs) is a natural Kafka use case, and the project is a vehicle to get real experience with the transport.
2. **Introduce a polyglot service.** A non-.NET service (currently planned as Go) exercises the gRPC contract at the wire level rather than at the .NET-to-.NET abstraction level. This proves the contract's interoperability story and keeps the project honest about what gRPC actually is.
3. **Apply Event Modeling more thoroughly.** Earlier showcases used Event Modeling pragmatically. CritterCab leans into it more deliberately as the primary design tool, including at the context-map level, not just the slice level.
4. **Pilot Domain Storytelling.** Stefan Hofer's technique complements Event Modeling and has not been used in the other showcases. Ride-sharing is a good fit; the stories write themselves. *(First exercised in [Workshop 003 — Onboarding Domain Story](../workshops/003-onboarding-domain-story.md); pilot successful — DS committed as a permanent design-phase technique. See v0.5 below.)*
5. **Pilot an NDD-informed approach to spec-driven development.** A genuine gap in the current workflow is the handoff from Event Modeling output to implementation. CritterCab experiments with a structured narrative layer (see Methodology, below) that sits between workshop artifacts and code.
6. **Lean harder into DDD strategic design.** A first-class context map, published languages between contexts, explicit anti-corruption layers where appropriate. The strategic side of DDD gets skipped in most real projects; CritterCab does not skip it.

### Tertiary

1. **Build a credible Azure story.** Entra External ID for rider and driver identity, Azure Service Bus (with the ASB emulator for local development) for a business-event backbone, and Azure as the deployment platform (ADR-007). This supports a future Omaha Azure User Group talk and broadens the project's audience beyond the Critter Stack community.

## How CritterCab Differs from CritterSupply and CritterBids

CritterSupply is a modular monolith: eighteen bounded contexts, one deployable. CritterBids is event-driven with saga orchestration, still primarily a single-deployable demo. CritterCab pushes into distributed territory.

Specific differentiators:

**Deployment boundary.** CritterCab is the first showcase in the series where each (or most) bounded context becomes a separately deployable service. The number of services is deliberately modest (target 6 to 8, not 20+) to keep the project maintainable by a small team, but the boundary is real: each service owns its own data store and communicates with others exclusively through messages or gRPC calls.

**Transport diversity.** CritterCab is the first project in the series to use Wolverine's Kafka transport, and the first to combine multiple transports (gRPC, Kafka, and Azure Service Bus) in one system. The choice of transport per flow is a deliberate design concern, not a default.

**Polyglot services.** A non-.NET service (Go, most likely) participates in the system as a peer over gRPC. This is a first for the series and a direct consequence of leaning into gRPC as a design choice.

**Real identity integration.** Earlier showcases treated identity lightly. CritterCab integrates a real OIDC provider (Entra External ID is the current lean) and treats identity as a first-class concern, including events flowing from the identity provider into the domain.

**Observability as a feature.** Distributed tracing across gRPC, Wolverine, Kafka, and ASB is part of the story from day one rather than bolted on at the end. The trace itself becomes demo material.

**Actual deployment.** CritterSupply and CritterBids are primarily local-development showcases. CritterCab aims to run somewhere reachable in Azure (ADR-007) so that conference demos can include a link the audience can hit.

**Methodology intensity.** Event Modeling more thoroughly, Domain Storytelling as a new addition, DDD context maps as a leading artifact, and an NDD-informed narrative layer sitting between workshop output and prompts. The methodology work is a deliverable in its own right, not scaffolding around the code.

## Tentative Bounded Contexts (v0.1)

The following is our current working list, captured before Event Modeling has been applied in earnest. Expect it to shift. In particular, the question of which contexts become separately deployable services and which fold together is deliberately deferred to the modeling work.

**Identity.** Authentication and account existence for both riders and drivers. Deliberately thin, especially given the lean toward a real OIDC provider. The project-owned Identity code is mostly an anti-corruption layer translating provider events into domain events.

**Onboarding.** Driver vetting lifecycle: application, document upload, background check, approval, suspension, reinstatement. Split from Identity because the lifecycle, compliance requirements, and domain language are genuinely different. A driver can exist in Identity long before they are cleared to drive.

**Rider Profile.** Rider-side personal data: saved addresses, payment methods on file, notification preferences. Downstream of Identity.

**Driver Profile.** Driver-side personal data: registered vehicles, current availability (online, offline, on break), service area, preferences. Downstream of Identity and Onboarding.

**Telemetry.** High-volume GPS ingest and location-of-record for active drivers. Maintains the geospatial index that Dispatch queries. This is the Kafka-heavy context. Upstream of Dispatch; has no opinions about trips or business logic.

**Dispatch.** Matching riders to drivers. Owns the ride-request lifecycle from request through assignment: candidate selection, offer fan-out, accept or decline, timeout, re-dispatch. The gravitational center of the system and the primary home of gRPC streaming surfaces. Hands off to Trips once a driver accepts.

**Trips.** Trip lifecycle from acceptance through completion or cancellation. Event-sourced. Owns the source-of-truth timeline: en route, arrived, started, in progress, completed, disputed. The richest event-sourcing domain in the system.

**Pricing.** Fare quoting, surge calculation, final fare computation, adjustments. Consumes aggregated signals from Dispatch and Trips. A candidate for Kafka stream processing on the surge side. Could plausibly live inside Trips early on and split out later.

**Payments.** Authorization at trip start, capture at trip end, refunds, driver payouts. Transactional integrity matters more than event-sourcing flexibility here, so this is the most likely candidate for Polecat on SQL Server.

**Ratings.** Post-trip feedback from both sides, average rating projections, low-rating review triggers. Small context but cleanly separable from Trips.

**Operations.** Internal tooling: live map, manual trip reassignment, driver suspension, historical queries, incident response. Cross-cutting read models and administrative commands. Consumer of most other contexts; producer of a few administrative commands.

Eleven bounded contexts, likely collapsing to six or eight deployable services. Candidate merges during detailed modeling include Identity plus Rider Profile and Identity plus Driver Profile (into Riders and Drivers services respectively), Pricing into Trips, and Ratings into Trips. Dispatch is expected to remain its own service in all configurations, as is Telemetry.

## Tentative Technology Stack (v0.1)

### Runtime and Framework

- .NET 10 / C# 14
- Critter Stack: Wolverine, Marten, Polecat, Weasel, Alba
- Wolverine 5.32+ specifically, for the gRPC feature set

### Architecture Style

- Services per bounded context (when the separate deployment is justified)
- Event-driven throughout
- Event sourcing where it fits; Trips is the clearest case, Dispatch likely a second
- Vertical slices within each service

### Messaging Transports

Planned:

- **gRPC** via Wolverine 5.32, for service-to-service and client-to-service streaming. Unary for commands and queries, server-streaming for offer delivery and ops dashboards, client-streaming for GPS ingest, and bidirectional where interactive flows justify it.
- **Kafka** via Wolverine's Kafka transport, for high-volume telemetry (GPS pings, breadcrumb trails) and as the likely input path for stream-processing concerns (surge pricing signals).
- **Azure Service Bus** (with the ASB emulator used for local development) for the business-event backbone. Sign-up events flowing from Entra via Microsoft Graph notifications into ASB is a natural landing pattern.

Transport selection by flow type is governed by ADR-005.

### Data Stores

- **PostgreSQL via Marten** for document storage and event sourcing. The default store for event-sourcing-heavy contexts (Trips, likely Dispatch).
- **SQL Server via Polecat** for contexts where transactional integrity dominates. Payments is the clearest candidate.
- **Redis** backing BFF APIs and live read models. The ops dashboard live map is a likely consumer.

### Identity

- **Entra External ID** for rider and driver authentication (current lean, not committed).
- **Entra ID** (workforce tenant) for Operations users (current lean, not committed).
- **OpenIddict** as the demo-mode identity provider, chosen for its active maintenance, strong .NET integration, and permissive licensing suitable for an open-source reference architecture.
- **Microsoft Graph** change notifications bridging provider events into the ASB backbone.

The Identity BC is designed around the principle that **the identity provider is swappable**. The project commits to at least one Azure-native provider and should remain compatible with a generic OIDC provider (Keycloak being the most likely second implementation). The Identity BC owns the translation from provider-specific events to domain events so that no other service becomes coupled to the provider.

### Methodology

The project uses several complementary design techniques, chosen to reinforce each other rather than compete:

- **Event Modeling** (Adam Dymitruk style) as the primary design tool for building time-based system models
- **Domain Storytelling** (Stefan Hofer) as a complementary workshop technique
- **DDD strategic design**: context maps, ubiquitous language per context, explicit upstream and downstream relationships with anti-corruption layers where appropriate. The canonical cross-BC relationship inventory lives at [`docs/context-map/README.md`](../context-map/README.md), updated in the same PR as any workshop that adds a new BC or amends an existing relationship
- **Prompt-document-driven sessions** with retrospectives, following the same workflow pattern established in CritterBids

The distinguishing methodological addition in CritterCab, not present in the prior showcases, is an **NDD-informed approach to spec-driven development**. Narrative-Driven Development was created by Sam Hatoum at Xolvio as a concrete dialect of the broader Spec-Driven Development movement. NDD synthesizes BDD (Given/When/Then), EventStorming, Specification by Example, DDD, and User Story Mapping into structured narratives: sequences of moments through time told from the user's perspective, where each moment captures context, interaction, and system response.

CritterCab adopts NDD's principles while remaining tool-agnostic. The project does not use the commercial Auto platform or its Zod-backed schema format directly. Instead, narratives are captured as structured markdown in `docs/narratives/` and serve as the durable, journey-scoped layer between Event Modeling workshop output and prompt documents. The framing of "NDD-informed" is deliberate: the principles drive the work, but the specific format evolves as we write the first narratives and learn what fits the Critter Stack and ride-sharing domain.

The resulting document layers, each with a clear job:

- **Workshops** (`docs/workshops/`) capture session output from Event Modeling and Domain Storytelling exercises.
- **Narratives** (`docs/narratives/`) are the journey-scoped, NDD-informed domain specs that persist across prompts.
- **Skills** (`docs/skills/`) are component specs: technical patterns, conventions, and constraints per bounded context or technology.
- **Prompts** (`docs/prompts/`) are task-scoped build orders that reference narratives and skills to drive a specific implementation session.
- **Retrospectives** (`docs/retrospectives/`) close the feedback loop after each session.

### Observability

- **OpenTelemetry** distributed tracing across gRPC, Wolverine, Kafka, and ASB
- **.NET Aspire dashboard** or Jaeger for trace visualization (TBD)
- **CritterWatch** (JasperFx's Wolverine/Marten/Polecat monitoring console) for live node/agent/endpoint health and messaging topology — complementary to OpenTelemetry's request traces, not redundant with them. CritterWatch depends on RabbitMQ for its telemetry/control plane, which CritterCab provisions as tooling-only infrastructure ([ADR-017](../decisions/017-rabbitmq-for-critterwatch.md))

### Deployment

Azure (ADR-007). The specific hosting model — Container Apps, App Service, or AKS — is deferred until the first cross-service integration is demonstrable end-to-end. Actually-deployed is a stated goal. Conference demos include a reachable URL.

### Frontend

The following working direction was adopted in v0.6 based on the [sibling-repo frontend survey](../research/frontend-survey-sibling-repos.md) and ADR-016. It is working direction, not a final commitment — revisions are expected as frontend implementation begins.

**Stack (convergent with CritterBids and mmo-reconnect):**

- React 19 / Vite 8 / TypeScript 6
- Tailwind v4 (`@tailwindcss/vite`, CSS-variable theme, no `tailwind.config.js`)
- TanStack Query v5 (server-state cache)
- TanStack Router (client routing)
- shadcn/ui + `class-variance-authority` + `clsx` + `tailwind-merge` (component layer)
- Zod schemas in the shared package (wire-contract types — the frontend analogue of the backend Contracts assembly)
- react-hook-form + `@hookform/resolvers/zod` (forms with validation)
- Vitest 4 + Testing Library + jsdom (unit/component tests)
- Playwright (end-to-end, in the `e2e` workspace)
- ESLint flat config (`typescript-eslint`)
- Node `>=22` (engine floor)

**Live-update transport:** SignalR (`@microsoft/signalr` v10) via the transport-agnostic push→Query-cache-bridge pattern from CritterBids. Components call `useListen`/`useConnectionState` and read from the TanStack Query cache; the SignalR transport is plugged in at the `createConnection` seam and is not visible to component code. See ADR-016.

**App structure:** audience-SPA monorepo with three independently-buildable SPAs sharing a contracts/theme/transport core:

- `rider/` — rider-facing app (trip booking, live trip tracking)
- `driver/` — driver-facing app (offer receipt, trip mode, GPS context)
- `operations/` — internal ops dashboard (live map, manual admin commands)
- `shared/` — shared contracts package, SignalR provider factory, Tailwind v4 theme (see §Open Questions for package-shape decision)
- `e2e/` — Playwright harness

### Deliberately Not Using

- **RabbitMQ — for domain flows.** No domain event, command, query, or stream rides RabbitMQ; the showcase goals favor breadth of transport experience, and Kafka and ASB earn the domain slots. **One scoped exception:** RabbitMQ *is* provisioned as the telemetry/control backplane for the **CritterWatch** monitoring console, which depends on it ([ADR-017](../decisions/017-rabbitmq-for-critterwatch.md)). That is tooling infrastructure, not a domain transport — the same out-of-scope-category move ADR-016 made for browser-client push (SignalR).
- **Rust (for now).** Go is the first polyglot choice. Rust remains a candidate for a second polyglot service if and when one is justified.
- **Clean Architecture and Onion Architecture.** The project uses Critter Stack idioms and vertical slices rather than imported abstractions.

## Design Principles

The following principles guide decisions when they come up. They are descriptive rather than prescriptive: they capture the reasoning we have already used, so that future decisions can apply the same reasoning consistently.

**Services per bounded context, not per whim** (ADR-002). A service exists when a bounded context justifies a separate deployment. Splits are motivated by domain boundaries, independent scaling requirements, or ownership boundaries, not by the number of services being a virtue in itself. When in doubt, contexts stay inside a single service until a reason to split them appears.

**Transport split by flow type, not by convenience** (ADR-005). The choice between gRPC, Kafka, and ASB follows the shape of the flow. High-volume append-only telemetry goes to Kafka. Business events that want topics, dead-lettering, and session ordering go to ASB. Service-to-service calls and streaming client interactions go to gRPC. A transport is not chosen because it is familiar; it is chosen because the flow fits it.

**Identity provider is swappable** (ADR-006). The Identity BC is an anti-corruption layer. Provider-specific events are translated into domain events at the boundary so that no other service knows or cares who issues tokens or emits user-lifecycle events. In production the provider is Entra External ID. For local development, Keycloak is the expected alternative. For demos, OpenIddict issues short-lived tokens.

**Contracts are first-class** (ADR-009). Protobuf service and message definitions are design artifacts, not implementation detail. They are reviewed with the care given to API contracts and evolved with intention.

**Capture intent in durable, structured form** (ADR-003). Design decisions, domain behavior, and user journeys are captured as first-class artifacts in the repository, not in chat windows or ticketing systems. The principle is familiar to event-sourcing practitioners: durable, append-only records of intent outlive any particular implementation, and the current state of the system becomes reconstructible from them. In CritterCab, narratives play this role at the journey level and skills play it at the component level. Prompts are transient build orders that reference them; code is transient implementation that satisfies them. When intent lives only in a chat window or a closed ticket, it evaporates, and the next contributor (human or AI) has to re-derive it from scratch.

**Event Modeling first, code second** (ADR-004). Workshops produce artifacts. Artifacts produce narratives. Narratives produce prompt documents. Prompt documents produce implementation. Code that does not trace back to a modeled scenario is treated with suspicion.

**Observability from day one.** Tracing, metrics, and log correlation are part of the first slice, not the last. The question "where did this request go?" should be answerable without writing custom instrumentation after the fact.

**Tradeoffs are explicit** (ADR-001). When a decision involves a tradeoff (and most do), the tradeoff is named. ADRs capture the options considered, not just the winner. The surrounding design docs acknowledge costs, not just benefits.

## Explicitly Parked

The following decisions are intentionally deferred. Revisiting them too early risks premature commitment; revisiting them too late risks drift. Each has a trigger that indicates the right time to pick it up.

**Microsoft Graph integration depth.** Current plan covers sign-up events only. Full user-lifecycle integration (password resets, MFA enrollment, account blocks) is deferred until the Identity BC is actively being worked.

**Operations tenant.** Real workforce Entra tenant setup is deferred. For now, the Operations side is stubbed. The trigger is: Operations BC is actively being built, and the auth story for ops users becomes load-bearing.

**Rust as a second polyglot language.** Go is the first polyglot service. Rust remains a candidate for a second service if and when one is motivated by actual need.

## Open Questions

Questions that are currently unresolved and that will resolve through Event Modeling, early implementation, or a future session of explicit decision-making:

- Final mapping of the 11 bounded contexts to deployable services. Target range is 6 to 8.
- Does Pricing remain inside Trips or split from day one?
- Does Ratings remain inside Trips or split from day one?
- Does Operations decompose further (operations-read, operations-admin) or remain a single service?
- Final form of the conference-demo scenario: pre-seeded simulated drivers only, audience-member drivers in addition, or both?
- Final narrative format: freeform markdown with structured headers, a Gherkin-flavored template, or a stricter schema-backed format? The NDD-informed direction is committed; the specific template evolves as we write the first narratives.
- **Suspension / reinstatement / deactivation BC placement.** Vision-doc v0.1 places these under Onboarding's lifecycle scope; [Workshop 003](../workshops/003-onboarding-domain-story.md) §5.2 finding B6 surfaced the alternative that the post-approval suspension lifecycle may belong to a Trust & Safety or Operations BC rather than Onboarding (the vetting lifecycle and the disqualification lifecycle share an actor but differ in trigger and reviewer). **Trigger to resolve:** when the suspension lifecycle is first modeled (likely a future workshop after Onboarding's W004).
- **Map library for the frontend.** Ride-sharing is map-centric: live driver positions, pickup/dropoff pins, route overlays, ops live map. Candidates include MapLibre GL, Leaflet, and deck.gl. The choice couples to the backend geospatial representation (H3 cells vs. GeoJSON — see `docs/research/ride-sharing-lessons-learned.md` Lesson 4) and deserves its own evaluation once that representation is settled. Neither sibling repository (mmo-reconnect, CritterBids) has a geospatial UI, so no tested choice can be inherited. **Trigger to resolve:** a dedicated map-library spike session, after the Telemetry BC's geospatial representation is decided.
- **Shared contracts package shape: one `@crittercab/shared` or per-BC packages?** CritterBids uses a single shared package. CritterCab's separately-deployed-services stance and BC-owned-language convention may argue for per-published-language packages instead, embodying in TypeScript what the context map says in DDD vocabulary. This question only becomes load-bearing when the first frontend package is bootstrapped. **Trigger to resolve:** the first frontend implementation session.
- **Frontend monorepo or separate frontend repos?** The `rider/driver/operations` SPA structure adopted in v0.6 co-locates three SPAs in one npm-workspaces monorepo (mirroring CritterBids). CritterCab's "separately deployable per BC" ethos raises the question of whether the frontends should likewise be separable — and whether a shared package across repo boundaries is worth the tooling cost. **Trigger to resolve:** the first frontend implementation session, once the deployment model for the frontend surfaces is clearer.

## Related Documents

The project's documentation is organized as layered artifacts, each with a distinct job:

- [Workshops](../workshops) capture Event Modeling and Domain Storytelling session output.
- [Narratives](../narratives) are the journey-scoped domain specs that persist across prompts.
- [Skills](../skills) are the component-scoped specs: implementation patterns, conventions, and technical constraints.
- [Prompts](../prompts) are task-scoped build orders that reference narratives and skills.
- [Retrospectives](../retrospectives) close the feedback loop after each implementation session.

Cross-cutting:

- [ADRs](../decisions) capture significant decisions as they are made.
- [Research](../research) captures exploratory work and spikes.

## Document History

- **v0.1** (2026-04-21): Initial capture of project vision, goals, tentative bounded contexts, tentative technology stack, design principles, and parked decisions.
- **v0.2** (2026-04-21): Committed to an NDD-informed approach to spec-driven development, acknowledging Sam Hatoum's work at Xolvio on Narrative-Driven Development. Added `docs/narratives/` as a distinct document layer alongside workshops, skills, prompts, and retrospectives. Added the "Capture intent in durable, structured form" design principle, with a note on its kinship with event-sourcing philosophy. Clarified the layered structure of the project's documentation in the Related Documents section.
- **v0.3** (2026-04-23): Cross-referenced ADRs 001–009 throughout. Committed Azure as the deployment platform (ADR-007) and Azure Service Bus as a planned transport (ADR-005). Removed resolved items from Open Questions and Explicitly Parked. Added ADR references to Design Principles.
- **v0.4** (2026-05-19): Added cross-reference to the new [`docs/context-map/README.md`](../context-map/README.md) foundation artifact from §Methodology's DDD strategic-design bullet. Closes the "first-class context map" methodology commitment that has been open since v0.1; the artifact rolls up cross-BC relationships from ADRs 006, 013, 014 and Workshops 001 and 002 into a single named place using DDD strategic-design vocabulary.
- **v0.5** (2026-05-26): Marked Domain Storytelling as **Exercised** per [Workshop 003 — Onboarding Domain Story](../workshops/003-onboarding-domain-story.md), closing the "Pilot Domain Storytelling" methodology commitment that has been open since v0.1. DS committed as a permanent design-phase technique alongside Event Modeling. Adds one new open question (suspension / reinstatement / deactivation BC placement) surfaced by W003 §5.2 finding B6.
- **v0.6** (2026-06-16): Unparked the frontend architecture. The vision's explicit unpark trigger — "CritterBids lands on a stable live-update pattern" — was discharged by the [sibling-repo frontend survey](../research/frontend-survey-sibling-repos.md) (2026-06-16), which confirmed that `CritterBids/client/shared/src/signalr/provider.tsx` is a generic, packaged `createSignalRProvider<TMessage>` shared across three SPAs. Adopted the convergent house stack (React 19 / Vite 8 / TS 6 / Tailwind v4 / TanStack Query) and the audience-SPA monorepo shape (`rider/driver/operations`) as working direction in §Tentative Technology Stack. Added ADR-016 (Frontend Live-Update Transport) — SignalR as the browser-client push transport, transport-agnostic push→Query-cache-bridge architecture. Removed "Frontend architecture" and "Map library for frontend" from §Explicitly Parked; added three new §Open Questions entries (map library, contracts-package shape, monorepo vs. separate repos). Drove by [`docs/prompts/frontend-architecture-unpark.md`](../prompts/frontend-architecture-unpark.md).
- **v0.7** (2026-06-25): Recorded the **CritterWatch / RabbitMQ** decision ([ADR-017](../decisions/017-rabbitmq-for-critterwatch.md)). Amended the §"Deliberately Not Using → RabbitMQ" bullet to narrow it to *domain flows* and name the one scoped exception — RabbitMQ is provisioned as CritterWatch's telemetry/control backplane (CritterWatch depends on it; an ASB backplane for the console does not exist yet). Added CritterWatch to §Observability. The carve-out mirrors ADR-016's "fifth category" framing (tooling infrastructure, not a domain transport); ADR-005's domain-transport decision is unchanged and was amended with a back-reference only.
