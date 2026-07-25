# ADR-019: Transport-Agnostic Topic Naming

**Status:** Accepted  
**Date:** 2026-07-24

## Context

[ADR-014](./014-asb-topic-naming-convention.md) codified `<source-bc>.<event-name-kebab>` as CritterCab's topic-naming convention, drawn from the seven Azure Service Bus topics Workshops 001 and 002 had produced. It was scoped to ASB because ASB was the only broker anything had been modeled against — the ADR's own title says so.

CritterCab now has a Kafka topic. Workshop 006 §6.3 locks Telemetry's `DriverLocationUpdated` publication to `telemetry.driver-location-updated`, and the implementation session for that slice is the first code in the repository that produces to a broker other than the (still unbuilt) ASB. W006 §11 anticipated exactly this and registered "generalize ADR-014 transport-agnostically" as an ADR candidate whose trigger was *the first Kafka topic landing*. That trigger has fired.

Two things make this worth a decision rather than an assumption.

**First, the convention had already drifted in the skill layer.** [`docs/skills/wolverine-kafka/SKILL.md`](../skills/wolverine-kafka/SKILL.md) § Topic naming convention was written ahead of any implementation and independently proposed a *different* rule for Kafka: topics named `<bc>.<descriptive-name>`, with the explicit guidance that a topic "carries a descriptive name rather than a message-type name" because "a topic like `telemetry.location-pings` may carry `LocationPing` messages today and an enriched `LocationPingV2` tomorrow." Its illustrative topics (`telemetry.location-pings`, `telemetry.demand-signals`, `pricing.surge-updates`) were speculative — none had ever been published. So the repository held two conventions that produce similar-looking names from genuinely different rules, and nothing had forced the question until a real topic needed a name.

**Second, ADR-014 bundles two operational decisions with the name**, and it is not obvious that either survives the move to Kafka. ADR-014 mandates `SessionId = <canonical-id>` for ordering and Wolverine outbox coordination between the topic publication and the local event append. Kafka has no sessions, and W006 §6.3 explicitly rejects the outbox for this flow. A naming ADR that quietly carried those clauses across would import ASB semantics into a transport that cannot honor them.

## Options Considered

### Option A — Per-transport conventions

Let each transport keep the convention that suits it: ASB stays on ADR-014's `<source-bc>.<event-name-kebab>`, and Kafka adopts the stream-descriptive `<bc>.<descriptive-name>` the `wolverine-kafka` skill proposed.

This option has a real argument behind it, and it is not the one it appears to be. It is not "different brokers, different habits" — it is that Kafka topics and ASB topics have genuinely different lifetimes. An ASB topic carries discrete business events that a consumer subscribes to and acknowledges; a Kafka topic is a durable, replayable log whose retention outlives any individual payload shape. Naming a log after the message type that happens to be in it today invites a rename the moment the payload evolves, and renaming a Kafka topic is not a rename — it is a migration with dual-write and consumer cutover.

The cost is that CritterCab would have to explain, at every future BC's first publication, which rule applies before anyone could name anything. Ownership tooling would need two glob patterns. And the premise is weaker than it looks in this codebase specifically: CritterCab versions its contracts in the proto package path (`crittercab.telemetry.v1`, per [ADR-009](./009-protobuf-contracts-as-first-class-artifacts.md)), so payload evolution is already handled a layer down. A `v2` with breaking changes gets a new proto package and, if the break is real, a new topic regardless of how the old one was named — which is the migration this option was trying to avoid, arriving anyway.

### Option B — One convention across transports: `<source-bc>.<event-name-kebab>`

ADR-014's Option C generalizes unchanged. `dispatch.ride-assigned` on ASB and `telemetry.driver-location-updated` on Kafka are the same rule applied twice.

The naming rule is the part of ADR-014 that was never actually ASB-specific — nothing in `<source-bc>.<event-name-kebab>` depends on sessions, subscriptions, or dead-letter semantics. Ownership stays legible from the name alone, `dispatch.*` and `telemetry.*` keep working as glob patterns across a heterogeneous transport estate, and a contributor asking "what does Telemetry publish?" gets one answer rather than one per broker. It also keeps the topic name aligned with the protobuf message name, which is the artifact under review — an operator reading `telemetry.driver-location-updated` in a log line can find `driver_location_updated.proto` without a lookup table.

The cost is Option A's argument, accepted rather than dismissed: a Kafka topic named after its event is a topic that reads oddly if the event is ever substantially reshaped without a version bump. CritterCab takes that cost because ADR-009 makes the reshape-without-a-version-bump case the one that should not happen.

### Option C — Encode the transport or version in the name

`kafka.telemetry.driver-location-updated`, or `telemetry.driver-location-updated.v1`.

Transport in the name is redundant with the connection: a consumer already knows which broker it is talking to, and encoding it makes a transport migration a rename of every topic. Version in the name duplicates the proto package's version segment and creates a second place for the two to disagree. Neither earns its characters in every log line.

## Decision

**Option B.** CritterCab names topics `<source-bc>.<event-name-kebab>` on **every** transport. ADR-014's naming rule is hereby transport-agnostic; `telemetry.driver-location-updated` is its first Kafka instance.

**ADR-014 is extended, not superseded.** It remains Accepted and remains authoritative for Azure Service Bus. What this ADR generalizes is its naming rule only. Its two travelling operational decisions **do not generalize**, and the per-transport table below is the load-bearing content of this decision:

| Concern | Azure Service Bus (ADR-014) | Kafka (this ADR, W006 §6.3) |
|---|---|---|
| Topic name | `<source-bc>.<event-name-kebab>` | `<source-bc>.<event-name-kebab>` — same rule |
| Ordering mechanism | `SessionId = <canonical-id>` ([ADR-013](./013-shared-cross-bc-identifier.md)); ASB delivers a session in publish order | Partition key = the stream's natural partition (`driverId` for driver telemetry); per-partition ordering is a broker property |
| Publish/persist coupling | Wolverine outbox — the topic publication commits in the same transaction as the local event append | **No outbox.** Publish first, then persist; inline send so a broker rejection prevents the local write |
| Duplicate handling | Outbox makes duplicates rare; consumers are idempotent by handler | At-least-once by design; consumers dedup on `(partition key, server-stamped timestamp)` |
| Payload serialization | Wolverine's default envelope JSON | Endpoint-scoped binary protobuf where the payload is a generated contract type (ADR-009) |

The distinction to carry forward: **the name is a convention; the delivery semantics are a property of the transport and the flow.** Naming them in one ADR is what keeps a future BC from inheriting ASB's outbox guarantees by way of a shared topic-name rule.

Where the `wolverine-kafka` skill's topic-naming and serialization guidance conflicts with this ADR, this ADR wins; the skill is corrected in the session that ships this decision.

## Consequences

Every future CritterCab publication is named the same way regardless of broker. Identity's `identity.rider-registered`, Payments' `payments.fare-settled`, and Pricing's `pricing.surge-updated` inherit the convention whether they land on ASB or Kafka, and none of those BCs has to re-derive the question or discover the skill-vs-ADR conflict this decision resolves.

Ownership tooling stays single-pattern. "What does Telemetry publish?" is answerable with `telemetry.*` across the whole estate. A topic inventory that spans brokers groups by BC without a per-transport translation step — which matters more as CritterCab's transport count grows from two to three.

The per-transport table becomes the thing new flows consult, not the name. A BC adding its first Kafka topic now has a written answer for the questions that actually bite — how ordering is achieved, whether the outbox applies, how consumers dedup — instead of inferring them from an ASB-shaped ADR. This is the clause most likely to be extended: a third transport adds a column rather than a competing convention.

Renaming a topic remains expensive, and this ADR does not make it cheaper. It accepts Option A's argument on the merits and answers it with ADR-009 rather than with a naming escape hatch: if a payload's evolution is breaking enough to make `driver-location-updated` a misleading topic name, it is breaking enough to need a new proto package and a deliberate consumer migration. The naming convention is not the mechanism that absorbs contract drift; the contract layer is.

`telemetry.driver-location-updated` is the reference instance. It is produced with partition key `driverId`, dedup key `serverReceivedAt`, no outbox, inline sending, and binary protobuf — every row of the Kafka column exercised at once, in code, under test.
