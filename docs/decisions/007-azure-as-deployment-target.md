# ADR-007: Azure as Deployment Target

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab has a stated goal of being actually deployed, not just runnable locally. Conference demos need a reachable URL. Contributors evaluating the reference architecture should be able to point at a live system, not just a repository. Local-only projects communicate "here is how it might work" rather than "here is how it does work."

Two earlier decisions shape the deployment context. ADR-005 commits to Azure Service Bus as the business-event transport. ADR-006 commits to Entra External ID as the production identity provider. Both are Azure-native services. The question is whether the deployment platform should complete that story — making Azure the full operational environment — or whether CritterCab should run its compute on a separate host while integrating with Azure services at the network boundary.

The specific hosting model within Azure (Container Apps, App Service, AKS, or something else) is not yet decided. That question depends on the number of services, the traffic patterns, and the operational tooling that proves most practical once a cross-service integration is demonstrable end-to-end.

## Options Considered

### Option A — Independent VPS hosting

CritterCab deploys its services to a Linux VPS (Hetzner or equivalent). The Azure service commitments (ASB, Entra) remain; the compute runs outside Azure and calls Azure services over the network.

This is the lower-cost option in terms of Azure spend and does not require Azure-specific deployment knowledge. A VPS provides a known, controllable environment with predictable pricing.

The cost is coherence. CritterCab becomes a story about a system that uses some Azure services but runs its compute elsewhere. That framing makes the Azure integration look incidental rather than intentional. For a reference architecture whose purpose includes demonstrating how .NET services work well on Azure, running outside Azure undercuts the demonstration.

### Option B — Cloud-agnostic containerized deployment

CritterCab targets a generic container runtime (Kubernetes or Docker Compose on any host). The deployment artifacts are portable; the choice of cloud is left open.

Cloud-agnostic deployment is genuinely valuable in production systems where portability is a requirement. For CritterCab, it is a non-goal. The project is not trying to prove that the Critter Stack runs anywhere; it is trying to show that it runs well with Azure. Designing for portability that will never be exercised adds complexity without adding to the story the project tells.

### Option C — Azure as the deployment platform

CritterCab runs its services in Azure. The specific hosting model is deferred until the first cross-service integration is demonstrable end-to-end — at that point, the number of services, their communication patterns, and their resource footprints will be known well enough to choose between Container Apps, App Service, and AKS.

With Azure as the platform, the Entra External ID integration, the Azure Service Bus business-event backbone, and the compute all live within the same operational boundary. Networking between services and Azure-native services is internal rather than external. Azure-native observability tools (Application Insights, Azure Monitor) are available alongside OpenTelemetry. The full system tells a coherent Azure story.

## Decision

**Option C.** Azure is CritterCab's deployment platform. This is a project goal, not merely an infrastructure convenience. CritterCab is a "works well with Azure" story for .NET developers and teams invested in the Microsoft ecosystem. The Azure commitment makes the Entra and ASB decisions coherent parts of a platform story rather than isolated service choices.

The specific hosting model — Container Apps, App Service, or AKS — is deferred to a future ADR. That decision will be made once the first cross-service integration is demonstrable end-to-end and the operational shape of the system is visible enough to choose well.

## Consequences

Azure is the production target for all CritterCab services. Local development continues to use local equivalents where they exist (the ASB emulator, a local OIDC provider), so contributors are not required to provision Azure resources to run the project.

Deferring the specific hosting model is a deliberate choice, not an oversight. Container Apps, App Service, and AKS have meaningfully different operational models — cost profiles, scaling behavior, networking configuration — and choosing among them before the service topology is finalized (ADR-010) risks picking for the wrong shape. The constraint is: the first deployment uses whichever Azure hosting model fits best at the time, and the choice is recorded in the superseding ADR.

The Azure commitment broadens CritterCab's intended audience. The project speaks to developers evaluating the Critter Stack whether or not they use Azure; for those who are already invested in the Azure ecosystem, it provides a complete picture of how Wolverine, Marten, Entra, and ASB compose in a real deployment.
