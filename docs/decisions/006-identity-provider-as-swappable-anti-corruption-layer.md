# ADR-006: Identity Provider as Swappable Anti-Corruption Layer

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab requires an identity provider for rider and driver authentication. That provider emits lifecycle events — user registered, email verified, account suspended — that have domain consequences in other bounded contexts. A newly registered rider needs a profile. A newly approved driver needs to become available for dispatch. These are not identity concerns; they are domain concerns that happen to originate at the identity boundary.

The identity provider is not the same thing across all environments. In production, the lean is toward Entra External ID for its Azure alignment and enterprise readiness. For local development and demos, a self-hosted OIDC provider (OpenIddict, or Keycloak as a likely second option) is more practical than requiring every contributor to provision a cloud identity service. In a demo context, the identity provider needs to issue short-lived tokens without ceremony, not manage a production tenant.

If the domain model in each service absorbs provider-specific types, claims shapes, and event schemas, then swapping the provider — between environments or between versions of the same provider's API — requires touching every service that has coupled to it. That coupling is not hypothetical: Microsoft Graph change notification payloads, Entra-specific JWT claim names, and OIDC discovery endpoint formats differ across providers in ways that surface in integration code.

The question is how to prevent provider-specific details from propagating into the domain.

## Options Considered

### Option A — Direct provider integration per service

Each service that needs identity information integrates directly with the provider: parsing provider-specific JWT claims, subscribing to provider-specific lifecycle webhook payloads, and referencing provider-specific user identifiers where they appear in the domain model.

This is the lowest-ceremony path initially. There is no translation layer to build or maintain. Each service handles exactly the identity signals it needs.

The cost appears over time and across environments. Every service that reads Entra-specific claims must be updated when those claims change. Every service that subscribes to Microsoft Graph change notifications must be updated or reconfigured when the provider changes. The provider's object identifiers, claim names, and event schemas become implicit contracts that show up everywhere, not in one place. Swapping the provider for local development or demos becomes an integration project, not a configuration change.

### Option B — Shared identity library

A shared library abstracts the identity provider. All services reference the library for token-parsing helpers and lifecycle event types. The library owns the provider-specific knowledge; services program against the library's abstractions.

This is better than Option A in that provider-specific knowledge is localized to one artifact. The problem is the artifact's relationship to the services: a shared library across separately deployable services (ADR-002) creates a shared dependency that must be versioned, published, and kept consistent across service deployments. A provider API change or a breaking update to the library requires coordinated updates across all consumers. The library becomes a hidden coupling point — logically centralized but physically distributed across every service that deploys it.

There is also a category error in the approach: the library crosses deployment boundaries, but the problem it solves is an integration concern that belongs at a service boundary, not in a library.

### Option C — Identity BC as the anti-corruption layer

The Identity bounded context is the single point of contact with the identity provider. It subscribes to provider-specific lifecycle events (Microsoft Graph change notifications over ASB, direct provider webhooks, or equivalent), translates them into domain events, and publishes those domain events to the rest of the system. No other service knows which provider is running or what its event schema looks like.

Token validation at each service boundary remains standard OIDC: services validate JWT Bearer tokens against the provider's JWKS endpoint. That is a security boundary, not an integration concern, and it does not create domain coupling — the claims a service extracts from a validated token are generic OIDC claims (subject, email, roles), not provider-proprietary types.

Swapping the provider changes what the Identity BC integrates with. It does not change what any other service consumes.

## Decision

**Option C.** The Identity BC is the anti-corruption layer between the identity provider and the rest of the domain. It translates provider-specific lifecycle events into domain events. No service outside Identity knows or cares which provider is running.

**Entra External ID** is the production identity provider for riders and drivers. Microsoft Graph change notifications carry Entra lifecycle events into the ASB backbone; Identity subscribes to those and publishes domain events (rider registered, driver account created) in return.

**OpenIddict** is the demo-mode provider. It is chosen for its active maintenance, strong .NET integration, and permissive licensing, which are appropriate for an open-source reference architecture. Where Entra issues tokens and emits lifecycle events in production, OpenIddict does so in demo mode. The Identity BC's translation logic is the only thing that changes between the two.

**Keycloak** is the most likely second alternative for local development, given its widespread use and full OIDC support. The design accommodates it without requiring it.

Operations users (internal tooling, live map, administrative commands) authenticate via a workforce Entra tenant, which is a separate concern from rider and driver identity. That integration is explicitly parked until the Operations BC is actively being built.

## Consequences

The Identity BC is intentionally thin. Its primary job is translation: turning provider-specific signals into domain events that the rest of the system understands. It is not a user management service or an authorization authority; it is a boundary.

Domain events published by Identity carry domain identifiers, not provider object IDs. If a service needs to correlate a domain actor with an identity provider record, that mapping lives in Identity — not in the service that needs it.

Adding or swapping an identity provider touches one service. The operational constraint is that Identity must be kept current with the provider's event schema; that cost is real but it is bounded and isolated.

The parked decision on Operations identity is a consequence of this approach: when the Operations BC is built, a second provider integration (workforce Entra tenant) will be added to Identity or handled as a parallel ACL at the Operations boundary. That decision will be made in context, not now.
