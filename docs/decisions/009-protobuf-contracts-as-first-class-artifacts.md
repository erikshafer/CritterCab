# ADR-009: Protobuf Contracts as First-Class Artifacts

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab uses gRPC for service-to-service communication and streaming (ADR-005). gRPC is defined by Protobuf service and message definitions — `.proto` files. These files describe the wire contract between services: what operations exist, what data they accept, and what data they return. Every generated client and server stub is derived from them.

The question is what kind of artifact a `.proto` file is. The answer is not fixed by the tooling. In a code-first gRPC workflow, the proto file is a byproduct: a developer writes C# interfaces and types, a tool generates the `.proto` from the code, and the `.proto` is a serialization detail that happens to live in the repository. In a contract-first workflow, the `.proto` file is the authoritative source: it is written by hand, reviewed as a design decision, and code is generated from it. The two approaches produce equivalent wire behavior but have different implications for how contracts evolve and how much friction exists when a service's consumers need to coordinate around a change.

CritterCab's service boundaries are real (ADR-002). A change to a `.proto` definition can silently break any service that depends on it. With 6–8 separately deployable services, each potentially running at a different version in a staged deployment, the cost of treating proto files as implementation detail — changed freely, reviewed casually, with no explicit versioning discipline — accumulates quickly.

## Options Considered

### Option A — Code-first: proto files generated from C# types

Developers write C# interfaces and data transfer objects. Tools like `protobuf-net.Grpc` derive the `.proto` representation from the code. The `.proto` file, if it exists in the repository at all, is a generated artifact rather than an authored one.

This approach minimizes ceremony for developers already writing C#. The service definition lives where the code lives, in a language they know, without a separate design step for the contract.

The cost is governance. A generated `.proto` is as easy to change as the C# type it is derived from, which means a field rename or a method signature change passes through the same review as any other refactor rather than being flagged as a potential breaking change. In a distributed system, this is not a safe default. Changes that look local in C# are not local at the wire — any consumer compiled against the previous contract is affected, whether or not the repository history makes that visible.

### Option B — Contract-first, but treated as implementation detail

Proto files are hand-authored and live in the repository, but they are not governed differently from other source files. They are modified in the same PR as the handler changes they accompany, reviewed with the same bar as implementation code, and versioned only informally.

This is better than Option A in that the `.proto` is the source of truth for the wire contract and developers must consciously write it rather than having it generated. The gap is that its governance is still implementation-grade rather than API-grade. In practice, this means breaking changes are identified only when a consumer fails to compile after pulling — not at review time, and not before the change is merged.

### Option C — Contract-first, proto files as first-class design artifacts

Proto files are hand-authored, live in a known location in the repository, and are governed as API contracts. A change to a `.proto` file is treated as a breaking-change candidate until explicitly assessed otherwise. Field additions and service additions are non-breaking by default; field removals, field renames, number reassignments, and service method removals are breaking by default. Changes that are confirmed non-breaking require that confirmation to be explicit in the PR, not assumed.

This is the approach appropriate for any interface that crosses a deployment boundary. Public HTTP APIs are governed this way as a matter of course; gRPC service definitions are the same class of artifact and deserve the same treatment.

## Decision

**Option C.** Protobuf service and message definitions are first-class design artifacts. They are authored by hand before the code that implements or consumes them is written. They live in a known location in the repository. Changes to `.proto` files are reviewed with the care given to API contracts, not with the care given to implementation files.

Operationally, this means:

- Proto files reside in a dedicated location in the repository, separate from any single service's project directory, so their cross-service nature is structurally visible.
- A PR that modifies a `.proto` file identifies, in its description, whether the change is breaking. Non-breaking changes (adding optional fields, adding new RPC methods) are confirmed as non-breaking. Breaking changes (removing or renaming fields or methods, reassigning field numbers) are accompanied by a migration plan for consumers.
- Proto files precede implementation: the contract is designed and reviewed before client or server stubs are generated.

## Consequences

The discipline of treating proto files as API contracts slows down changes that would otherwise be quick code edits. This is the intended effect. The cost is friction on individual changes; the benefit is that breaking changes at the wire level are never accidental.

Generating code from proto files (rather than the reverse) means the generated C# stubs are not checked in — they are produced at build time. The `.proto` files are what gets reviewed; the generated code is a build artifact.

The polyglot service (Go, per the vision doc) participates over gRPC. Because proto files are the authoritative contract rather than C#-derived artifacts, the Go service is a first-class consumer of the same contract. The contract is language-neutral by construction, not by retrofit.

---

## Future Consideration

Erik has noted interest in exploring Protobuf as a unified schema language across all serialization boundaries in CritterCab — not only gRPC wire format, but also Kafka messages, Azure Service Bus messages, Marten-persisted events, and Polecat-persisted events. The idea is a single schema language governing every boundary where data crosses a process or service edge.

This is not a current commitment. It is a future experiment. The motivations are real: a single schema format reduces the number of serialization vocabularies contributors need to know, makes the contract between a producer and any consumer explicit regardless of transport, and could simplify the story around schema evolution across transports.

If and when this experiment is pursued, it will produce its own ADR. At that point, this ADR's scope (gRPC contract governance) would remain in force; the new ADR would extend the principle to additional boundaries. The two would be additive rather than superseding.
