# ADR-011: Critter Stack as Foundational Technology

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab exists to showcase the Critter Stack, a family of open-source .NET libraries maintained by JasperFx. The project was conceived specifically to demonstrate Wolverine's gRPC feature set, which shipped in Wolverine 5.32, against a domain whose natural shape exercises all four gRPC modes. The other Critter Stack libraries — Marten, Polecat, Weasel, and Alba — extend the demonstration surface to event sourcing, document storage, schema management, and integration testing.

This is a founding-premise ADR. It records the technology adoption formally but does not record a contested decision: the stack was not evaluated against alternatives the way transport selection (ADR-005) was. CritterCab is a Critter Stack showcase; alternatives to the Critter Stack would produce a different project with a different purpose.

ADRs 001–009 reference Wolverine, Marten, and Polecat as settled facts without formally recording the adoption. This ADR closes that gap and documents what each component owns in the architecture.

## Options Considered

### Option A — Critter Stack

The integrated JasperFx family: Wolverine (handlers, messaging, gRPC, transports), Marten (PostgreSQL document store and event sourcing), Polecat (SQL Server document store), Alba (integration test host), with Weasel (database schema management) and the JasperFx core package as implicit dependencies.

The libraries are designed to compose: Wolverine.Marten integrates the message bus with Marten's event store; Marten and Polecat both use Weasel for schema management; Alba wraps the ASP.NET Core host with Wolverine-aware test infrastructure. Choosing the stack as a family is not incidental — the integration surface between these libraries is part of what CritterCab demonstrates.

### Option B — Standard .NET ecosystem equivalents

MassTransit or NServiceBus for messaging. Entity Framework Core for persistence. xUnit with WebApplicationFactory for integration testing. These are well-understood alternatives for distributed .NET services.

This option is not a serious candidate. CritterCab's purpose is to showcase the Critter Stack. Replacing it with standard equivalents would produce a competent distributed systems reference but would not serve the project's primary goal and would not demonstrate Wolverine's gRPC feature set.

## Decision

**Option A.** The Critter Stack is CritterCab's foundational technology. Each library has defined ownership within the architecture:

**Wolverine** (minimum 5.32): All message handler discovery, routing, and dispatch. All transport configuration (Kafka, Azure Service Bus, gRPC). The local in-process bus for publishing events from handlers. gRPC service surfaces and their generated stub consumption. Handlers do not know which transport delivered a message — Wolverine's bus configuration owns that routing (ADR-005).

**Marten**: Event sourcing and document storage for services backed by PostgreSQL. Marten owns the event stream, projection definitions, and document schema for any service that runs on PostgreSQL. Raw SQL, Dapper-as-ORM, and Entity Framework are not used where Marten provides an equivalent capability.

**Polecat**: Document storage for services backed by SQL Server. Where a service runs on SQL Server, Polecat is the document store. Polecat does not provide event sourcing; services requiring event sourcing run on PostgreSQL with Marten.

**Alba**: Integration test host for all service-level tests. Alba wraps the ASP.NET Core + Wolverine host for request/response testing, message assertion, and in-memory bus verification without external infrastructure. WebApplicationFactory without Alba is not used for service-level tests.

**Weasel**: Database schema management. Used implicitly by Marten and Polecat; not configured independently.

**JasperFx** (core package): Shared utilities across the stack. Used implicitly.

## Consequences

All message handling and transport configuration is owned by Wolverine. A handler does not choose how its message arrives or departs — that is Wolverine's routing configuration. This keeps handler code free of transport concerns; ADR-005's transport-per-flow-type rule is expressed in Wolverine configuration, not in handler implementations.

Event sourcing is exclusively a Marten concern. Append-only event streams, projections, and event-sourced aggregates all use Marten's API. Implementations that store domain events in hand-rolled tables or use an alternative event store are out of scope.

Persistence follows the library appropriate to the service's database engine. A service on PostgreSQL uses Marten. A service on SQL Server uses Polecat. There is no ORM layer independent of these two.

Where a Critter Stack library does not yet support a capability CritterCab needs, the response is to raise the issue upstream or accept the constraint — not to introduce application-layer workarounds that bypass the stack.

Contributors need Critter Stack familiarity. The Context7 section in CLAUDE.md provides LLM-accessible documentation for Wolverine (`/jasperfx/wolverine`), Marten (`/jasperfx/marten`), and Polecat (`/jasperfx/polecat`).

Wolverine 5.32 is the effective minimum version. Earlier versions do not include gRPC support, which is the project's primary showcase target.
