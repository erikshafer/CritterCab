# ADR-002: Distributed Services per Bounded Context

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab models the ride-sharing domain, which decomposes naturally into bounded contexts with sharply different operational profiles. Telemetry ingests high-volume, append-only GPS streams. Dispatch makes sub-second matching decisions under load with low-latency requirements. Payments requires transactional integrity over a reliable but not necessarily fast path. Identity manages session lifecycles independently of any particular ride flow. These are not incidental differences — they are load, consistency, and failure-isolation differences that accumulate into real deployment and scaling differences.

CritterCab also exists to showcase Wolverine's gRPC feature set. That demonstration is meaningful only if gRPC is the actual wire between real service boundaries, not an abstraction layer over in-process calls. An architecture that collapsed bounded contexts into a single deployable unit would reduce gRPC to a networking detail rather than a structural commitment.

The question is not whether to have service boundaries — the domain demands them — but where to draw them and what rules govern communication across them.

## Options Considered

### Option A — Modular monolith

All bounded contexts live in a single deployable unit, separated by namespace and project boundaries rather than deployment boundaries. Communication between modules is in-process; external interfaces are exposed as needed at the application perimeter.

This trades deployment simplicity for scaling flexibility. It is often the right choice when the bounded contexts have compatible operational profiles, the team is small, and independent scaling is not a near-term requirement.

For CritterCab, a monolith would undercut the project's primary purpose. gRPC as a showcase transport requires a real service boundary to cross; there is no meaningful demonstration of streaming or service-to-service communication if all parties share a process. The operational profile differences between Telemetry, Dispatch, and Payments are also real enough that a monolith would either over-provision most contexts or constrain the one that needs independent scaling.

### Option B — One deployable service per bounded context

Each of the 11 tentative bounded contexts is its own deployable unit, with its own data store and independent infrastructure. Services communicate exclusively over the wire.

This maximizes deployment independence and scaling granularity. For some contexts, the argument is strong: Dispatch, Telemetry, and Payments each have distinct enough operational profiles and failure boundaries that independent deployment is clearly justified.

For others, the argument is weaker. A Ratings service handling post-trip reviews does not require independent scaling from Trips in any scenario CritterCab is likely to exercise. A strict one-BC-one-service policy in those cases produces thin deployable units that add operational overhead without adding domain clarity or scaling benefit. Eleven services is also a larger maintenance surface than a small team building a reference architecture should carry without deliberate justification.

### Option C — Services per bounded context, with deliberate grouping

A bounded context is the default unit of deployment, but small contexts with compatible operational profiles may be grouped into a single service where independent deployment adds no value. Groupings are made explicitly and justified at the time they are made. Services are still separately deployable; the only question is whether two bounded contexts share a deployment unit.

This is the pragmatic middle: the discipline of distributed services where the domain and operational profile justify it, without the overhead where they do not.

## Decision

**Option C.** CritterCab targets 6–8 deployable services from 11 tentative bounded contexts. Each service owns its own data store exclusively. Services share no application-layer code with each other and hold no project references to each other. Cross-service communication is exclusively gRPC calls or Wolverine messages — no shared database access, no direct in-process handler calls across service boundaries.

The specific groupings (which bounded contexts share a deployment unit) are deferred to ADR-010, which will be written after the Event Modeling workshop's swim-lane step and the Context Mapping exercise that precedes it. The constraints governing all grouping decisions are established here: a bounded context may join another service's deployment only when their operational profiles are compatible, when independent scaling is not a near-term requirement, and when the grouping does not obscure a domain boundary that will need to surface later.

Contexts expected to remain separate services in all configurations, based on their operational profiles: Dispatch (latency-sensitive, gRPC-heavy) and Telemetry (high-volume Kafka-backed). All others are candidates for deliberate grouping pending the modeling work.

## Consequences

Service boundaries are load-bearing. Each service must be independently deployable, independently scalable, and failure-isolated. Data stores are owned by one service and accessed by no other.

The cross-service communication constraint is structural, not preferential. A handler in one service may not call another service's handler in-process. A service project may not reference another service's project. Where a bounded context boundary is ambiguous, the disambiguating question is: which service owns the data? Merging the data stores is not a resolution.

Every deployment unit that falls out of ADR-010 will need its own runtime, its own data store configuration, and its own Wolverine bus configuration. That operational surface is a real cost. The benefit is that each service can be reasoned about, deployed, and scaled independently — and that the gRPC boundaries the project exists to demonstrate are real.

ADR-010 finalizes the service topology once the Event Modeling swim lanes and Context Mapping session are complete.
