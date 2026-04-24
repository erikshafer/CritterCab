# CritterCab — Structural Constraints (Layer 1 Rules)

These rules encode the architectural non-negotiables from accepted ADRs. They apply to all implementation sessions, across all bounded contexts and services. If a situation appears to require deviating from a rule, surface it in the session retrospective rather than silently departing from it.

---

## Service Boundaries — ADR-002

**Never add a project reference from one service to another.**
Services share no application-layer code. Cross-service dependencies are expressed as wire contracts (proto files, message types), not project references.

**Never access another service's data store directly.**
Each service owns its data store exclusively. Reading data from another service requires a gRPC call or a Wolverine message to that service — not a direct query against its database.

**Never call another service's handler in-process across a service boundary.**
In-process handler calls do not cross service boundaries. Use Wolverine message dispatch (over the appropriate transport) instead.

**All cross-service communication is gRPC or Wolverine messages — nothing else.**
No REST clients, no HTTP calls, no shared libraries that cross deployment boundaries. The wire is the only crossing point.

**Dispatch and Telemetry are always separate deployable units.**
Their operational profiles are incompatible: Telemetry is high-volume and append-only; Dispatch is latency-sensitive. They may never be grouped into a shared deployment. All other BC groupings are deferred to ADR-010.

---

## Transport Selection — ADR-005

**gRPC for service-to-service calls and streaming interactions.**
- Unary: commands and queries between services
- Server-streaming: offer fan-out to driver candidates, live operations dashboards
- Client-streaming: GPS ingest from mobile clients into the Telemetry service
- Bidirectional: interactive trip-communication flows

**Kafka for high-volume, append-only streams.**
GPS pings from Telemetry to downstream consumers. Surge-pricing signal ingestion. These flows are unidirectional, append-only, and tolerant of brief consumer lag.

**Azure Service Bus for domain events crossing service boundaries.**
Rider registered, driver approved, trip completed, and similar business events. These are correctness-sensitive: they need dead-letter queues, session ordering, and topic-based routing.

**Do not use Kafka for business domain events.**
Kafka lacks the dead-lettering, session ordering, and operational features that business events require. Using it for business events pushes accidental complexity onto the project.

**Do not use RabbitMQ.**
RabbitMQ is explicitly excluded. The three committed transports cover all required flow shapes. Do not introduce a fourth transport.

**When implementing a new inter-service flow, identify its shape before choosing a transport.**
High-volume, append-only → Kafka. Request-response or streaming interaction → gRPC. Reliable cross-service domain event → ASB. Shape determines transport; do not default to any one choice.

---

## Identity Boundary — ADR-006

**Only the Identity bounded context integrates directly with the identity provider.**
No service outside Identity subscribes to Entra lifecycle webhooks, parses Microsoft Graph change notification payloads, or holds provider-specific configuration. Identity absorbs provider-specific details and publishes domain events in their place.

**No service outside Identity parses provider-specific JWT claims.**
Standard OIDC claims (subject, email, roles) are acceptable at token-validation boundaries. Provider-proprietary claim names and schemas are Identity's concern only.

**Domain events published by Identity carry domain identifiers, not provider object IDs.**
If a service needs to correlate a domain actor with a provider record, that mapping lives inside Identity. Other services work with domain IDs exclusively.

**Operations user authentication (workforce Entra tenant) is parked.**
Do not implement Operations user identity integration until the Operations BC is actively being built. The approach (second integration inside Identity, or a parallel ACL at the Operations boundary) is an open decision.

---

## Protobuf Contracts — ADR-009

**Proto files are hand-authored before the code that implements or consumes them.**
The contract is designed first. Client and server stubs are generated from the `.proto` file at build time. Never derive a `.proto` file from C# types.

**Proto files reside in a shared directory separate from any single service's project directory.**
Their cross-service nature must be structurally visible. A `.proto` file inside a service's project directory falsely implies single-service ownership. The specific shared directory (conventionally `/protos` at the repository root) is established when the first proto file is authored.

**Generated C# stubs are build artifacts — do not check them in.**
Only the `.proto` source files are versioned. Generated code is produced at build time and excluded from source control.

**Every change to a `.proto` file must be classified as breaking or non-breaking in the PR description.**
- Non-breaking: adding optional fields, adding new RPC methods.
- Breaking: removing or renaming fields or methods, reassigning field numbers.

Confirm non-breaking changes explicitly. Breaking changes require a migration plan for all downstream consumers before the PR is merged.

---

## Spec-Anchored Development — ADR-003

**When narratives exist for the work being implemented, load them before generating any code.**
The narrative at `docs/narratives/` is the architectural reference for the session. Code implements what the narrative specifies, not what seems reasonable at the time.

**When generated code diverges from the narrative, surface the divergence explicitly.**
Either the code is wrong (correct it) or the narrative is wrong (update it). The retrospective is where this resolution is recorded and committed. Do not silently resolve the conflict.

**Never implement behavior the narrative does not describe without flagging it in the retrospective.**
If implementation reveals that undescribed behavior is necessary, that is a modeling gap. Record it. The narrative should be updated to reflect what was learned.

**The retrospective is part of every session's definition of done.**
A session that closes without a retrospective leaves the spec-code relationship unaudited. The PR that contains the implementation contains the retrospective.

---

## Document History

- **v0.1** (2026-04-23): Initial Layer 1 rules. Sourced from ADR-002 (service boundaries), ADR-003 (spec-anchored development), ADR-005 (transport selection), ADR-006 (identity ACL), and ADR-009 (Protobuf contracts). Layer 2 (ubiquitous language) and Layer 3 (code conventions) to be added after Context Mapping and Event Modeling sessions.
