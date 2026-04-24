# CritterCab Rules Index

Rules are AI-optimized encodings of architectural constraints. Load the relevant rule file at the start of any implementation session. They distill ADR commitments into directives Claude can apply without re-reading full ADR prose.

## Files

- [`structural-constraints.md`](./structural-constraints.md) — Layer 1: service boundaries, transport selection, identity, Protobuf contracts, and spec-anchored workflow. Sourced from ADRs 002, 003, 005, 006, and 009.

Layer 2 (ubiquitous language per bounded context) and Layer 3 (code conventions) are added after the Context Mapping and Event Modeling sessions, respectively.
