# ADR-017: RabbitMQ for CritterWatch

**Status:** Accepted  
**Date:** 2026-06-25

## Context

CritterCab treats observability as a day-one feature (vision §"Observability as a feature", §Design Principles "Observability from day one"). Part of that story is **CritterWatch** — JasperFx's monitoring console for Wolverine/Marten/Polecat systems — which renders live node/agent/endpoint health and messaging topology alongside the OpenTelemetry traces in the Aspire dashboard. The two are complementary, not redundant: OpenTelemetry shows request traces; CritterWatch shows operational messaging state.

CritterWatch carries a hard infrastructure dependency that [ADR-005](./005-transport-selection-by-flow-type.md) did not anticipate. Monitored services publish telemetry to a well-known queue and listen on a private per-service control queue so the console can push commands back (pause projections, DLQ replay). In CritterWatch 0.9.x the transport for this telemetry/control plane is **RabbitMQ** — that is what the `critterwatch-install` skill playbook and its `REFERENCE.md` target end to end, and what the only shipped sibling integration (CritterMart's own ADR-017) runs on. RabbitMQ is not a preference here; it is the supported path.

This collides, on the surface, with ADR-005, whose closing line states flatly that "RabbitMQ is not used." ADR-005 reached that conclusion about CritterCab's **domain flows**: gRPC, Kafka, and ASB cover the three domain flow shapes, and a fourth broker would add cost without new capability. CritterWatch's telemetry plane is not a domain flow — it is operational instrumentation for a third-party monitoring tool — so ADR-005's reasoning does not actually speak to it, even though its wording reads as an outright ban on the broker.

The question is therefore not *which* transport to run CritterWatch on (CritterWatch answers that: RabbitMQ), but how to admit the broker its tooling requires without muddying ADR-005's domain-transport commitment.

## Options Considered

### Option A — Do not run CritterWatch

Rely solely on OpenTelemetry traces in the Aspire dashboard and skip the console entirely. This avoids any new infrastructure and keeps ADR-005's "RabbitMQ is not used" line literally true.

The cost is the loss of the operational-state view CritterWatch uniquely provides — live messaging topology, node/agent health, DLQ inspection, projection control. For a reference architecture whose transports are the headline feature, watching those transports operate is exactly the demo material the project wants. Dropping CritterWatch to save one local-dev broker is a poor trade — and it forfeits a goal (observability from day one) the project has held since the vision's first draft.

### Option B — Provision RabbitMQ as CritterWatch's backplane

Run a RabbitMQ broker in the local-dev orchestration (Aspire) **solely** to carry CritterWatch's telemetry and control queues. Domain flows never touch it. RabbitMQ enters the system as tooling infrastructure, on the same footing as the dedicated Postgres database CritterWatch keeps for its own event store.

### A note on the absence of an ASB alternative

The natural question — *CritterCab already commits to Azure Service Bus; why not run CritterWatch on ASB and avoid a new broker?* — has a flat answer: there is **no supported ASB backplane for CritterWatch today.** An earlier draft of the state-of-the-repo planning note claimed otherwise, citing `UseShardedAzureServiceBusQueues` in CritterWatch's `WolverineOptionsExtensions`. That was a misread: the symbol appears only in an abstract doc-comment listing sharded-cluster-topology helper names (alongside `UseShardedAmazonSqsQueues`), not as a verified telemetry/control path for the console. The install playbook, its reference, and every shipped integration are RabbitMQ. So ASB is not a rejected-on-merits alternative — it is simply not a capability that exists yet. If a supported ASB (or Kafka) backplane for CritterWatch ships later, revisiting this is cheap; until then, RabbitMQ is the only road.

## Decision

**Option B.** CritterCab provisions **RabbitMQ as CritterWatch's telemetry/control backplane**, and only that. CritterWatch depends on RabbitMQ; CritterCab runs RabbitMQ for CritterWatch. Simple as that.

This is a scoped, deliberate carve-out from ADR-005, not a reversal of it. The framing mirrors [ADR-016](./016-frontend-live-update-transport.md), which admitted SignalR as a *fourth category* (browser-client push) that ADR-005's service-to-service scope did not cover. RabbitMQ here is a *fifth category*: third-party tooling infrastructure. The discipline ADR-005 protects — domain transport chosen by flow shape — is untouched:

- **Domain flows remain gRPC, Kafka, and ASB**, exactly as ADR-005 specifies. No domain event, command, query, or stream is routed over RabbitMQ. The broker carries **zero** domain messages.
- **RabbitMQ exists only to satisfy CritterWatch's dependency.** It is provisioned in the Aspire AppHost as a container, referenced by the CritterWatch console host and by each monitored service's CritterWatch client wiring, and by nothing else. A reader of the AppHost sees RabbitMQ wired exclusively to the CritterWatch resources, which makes its tooling-only role self-evident.
- **The integration follows CritterMart's ADR-017 playbook:** a dedicated `critterwatch` Postgres database (not a schema in an app database), a single-node console (`enableClusterPartitioning: false` with a `.Sequential()` listener for ordering), the console run as `Production` with explicit user-secrets loading so the JasperFx trial license actually validates, and the `critterwatch-console` resource-name workaround for Aspire's shared, case-insensitive namespace.

ADR-005's closing line is narrowed accordingly: RabbitMQ is not used **for domain flows**; it is run as CritterWatch's backplane. ADR-005 is *amended with a cross-reference rather than superseded* — its domain-transport decision is unchanged.

## Consequences

The local-dev orchestration gains a RabbitMQ container — a net-new broker, since CritterCab has none today. This is a real operational cost (a fourth broker to start alongside Kafka, the ASB emulator, and Postgres), but it is bounded to tooling and does not propagate into the domain. Keeping RabbitMQ strictly on the CritterWatch plane preserves the project's clean "domain transport chosen by flow shape" teaching story: someone studying CritterCab to learn transport selection sees gRPC/Kafka/ASB for the domain and RabbitMQ for the monitoring console. The separation is itself instructive — observability infrastructure rides whatever its tool requires, independent of the domain's choices.

CritterWatch's value is gated on real cross-service traffic; its topology and DLQ panels are empty without it. The broker and the console therefore earn their place when the first multi-service transport flow lands — the same timing lesson CritterMart's ADR-013→017 records. Provisioning RabbitMQ ahead of that produces a working-but-empty console.

Inherited operational notes from CritterWatch 0.9.x (per CritterMart's ADR-017), tracked where CritterWatch is actually wired rather than here: the trial license expires 2026-07-10 and drops to a read-only Free tier afterward; CritterWatch 0.9.1 transitively pulls a CVE'd MessagePack that must be suppressed (`NuGetAuditSuppress`); and the console must run as `Production` or it silently masks the real license tier. Production hosting of the CritterWatch broker (versus a local-dev container) is deferred until the deployment model firms up (ADR-007), consistent with the project's deferral discipline.

Cross-references: [ADR-005](./005-transport-selection-by-flow-type.md) (the domain-transport decision this carves out from; amended with a back-reference), [ADR-016](./016-frontend-live-update-transport.md) (the precedent for admitting an out-of-scope transport category without reversing ADR-005), [ADR-007](./007-azure-as-deployment-target.md) (deployment context for the deferred production-broker question). External reference: CritterMart's ADR-017 (`docs/decisions/017-critterwatch-integrated.md` in the CritterMart repo) and the `critterwatch-install` skill, which together hold the integration playbook this ADR commits to following.
