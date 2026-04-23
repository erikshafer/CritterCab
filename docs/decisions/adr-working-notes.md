# ADR Working Notes

Working scratchpad capturing decisions, feedback, and context from the April 23, 2026 planning session. This file exists so the next session can pick up ADR drafting without re-deriving context from chat history.

**Delete this file once all proposed ADRs are written and committed.**

---

## Template

ADR-001 (`docs/decisions/001-record-architecture-decisions.md`) establishes the template. Every ADR uses these sections: Status, Date, Context, Options Considered, Decision, Consequences. Read 001 before drafting any new ADR.

## Ground Rules (from Erik)

- **No cross-project references.** Do not mention CritterSupply or CritterBids in any ADR. Each ADR should stand on its own and explain why CritterCab is making this decision based on CritterCab's own goals, constraints, and domain. No "unlike CritterSupply's monolith..." framing.
- **Tradeoffs are explicit.** The vision doc (`docs/vision/README.md`) says "ADRs capture the options considered, not just the winner." Every ADR needs genuine options with genuine assessments, not a straw-man setup for the chosen option.
- **Tone should match 001.** Concise, direct prose. No bullet-point walls. No hedging language. State the decision clearly.

---

## Proposed ADR Index

Status key: `DONE` = written and committed, `READY` = decision is made and can be drafted now, `SOON` = needs a triggering event or one more discussion round before drafting.

| Number | Title | Status | Notes |
|--------|-------|--------|-------|
| 001 | Record Architecture Decisions | DONE | Committed 2026-04-23. The meta ADR. |
| 002 | Distributed Services per Bounded Context | READY | See detailed notes below. |
| 003 | Spec-Anchored Development | READY | See detailed notes below. |
| 004 | Design-Phase Workflow Sequence | READY | See detailed notes below. |
| 005 | Transport Selection by Flow Type | READY | See detailed notes below. **Important corrections from Erik.** |
| 006 | Identity Provider as Swappable Anti-Corruption Layer | READY | See detailed notes below. |
| 007 | Azure as Deployment Target | READY | See detailed notes below. |
| 008 | MIT License | READY | See detailed notes below. |
| 009 | Protobuf Contracts as First-Class Artifacts | READY | See detailed notes below. **Has an aspirational note from Erik.** |
| 010 | Service Topology | SOON | Triggered by Event Modeling workshop step 6 (swim lanes). |
| 011 | Narrative Format | SOON | Triggered by writing first 2-3 narratives in candidate formats post-Event-Modeling. |
| 012 | Event Modeling Tooling | SOON | Triggered by first workshop completion. **Erik may have access to a new Event Modeling platform (beta/early access). Do not assume Miro.** |
| 013 | Polyglot Service Language and Boundary | SOON | Triggered by first slice crossing the .NET/Go boundary. |

---

## Detailed Notes per ADR

### ADR-002: Distributed Services per Bounded Context

**Decision:** Each bounded context (or small, justified group) is a separately deployable service with its own data store. No shared databases. No cross-service project references. Cross-service communication is exclusively gRPC calls or Wolverine messages.

**Why this matters for CritterCab:** The ride-sharing domain naturally decomposes into services with different scaling profiles (Telemetry is high-volume append-only; Dispatch is latency-sensitive matching; Payments is transactional integrity). The project also exists to showcase Wolverine's gRPC feature set, which is most meaningfully demonstrated when gRPC is the actual wire between real service boundaries, not an abstraction over in-process calls.

**Options to evaluate:** Modular monolith, microservices (one per BC), pragmatic middle ground (services per BC but with deliberate grouping where separate deployment isn't justified). The decision is the pragmatic middle — target 6-8 deployable services from 11 BCs, with the specific groupings deferred to ADR-010 after Event Modeling.

**Do not reference CritterSupply or CritterBids.** Justify from CritterCab's own goals and domain.

---

### ADR-003: Spec-Anchored Development

**Decision:** The Event Model and NDD narratives are the architectural reference. Code is authoritative for runtime behavior. Drift between model and code is detected by retrospective, not by automated derivation. Explicitly not spec-as-source (no commercial platform dependency) and not spec-free.

**Context to include:** The vision doc's "Capture intent in durable, structured form" design principle. The research doc (`docs/research/event-modeling-workshop-guide.md`, Lesson 12) evaluates the spec-first / spec-anchored / spec-as-source spectrum and recommends spec-anchored for CritterCab's scale and open-source mission.

**Key consequence:** Every retrospective includes the question "did this slice's implementation teach us anything that should update the Event Model or the narrative?" Model updates happen in the same PR as the retro.

---

### ADR-004: Design-Phase Workflow Sequence

**Decision locked in by Erik:** The pre-code design phase follows this sequence:

1. **Context Mapping** — name upstream/downstream relationships between BCs, identify anti-corruption layers, published languages, and conformist relationships. Feeds into Event Modeling swim lanes and the service topology decision (ADR-010).
2. **Domain Storytelling** — surface language boundaries between BCs. Where the same word means different things to different actors (e.g., "trip" means different things to rider, driver, payments, ops). Produces cleaner event names for Event Modeling.
3. **Event Modeling** — the primary design tool. Three-session workshop producing events, commands, views, swim lanes, slices, and GWT scenarios. See `docs/research/event-modeling-workshop-guide.md` for the full plan.
4. **NDD Narratives** — journey-scoped domain specs authored from Event Modeling slice output. Captured in `docs/narratives/`. Format is deliberately left open for now (see ADR-011).
5. **Prompt Authoring** — task-scoped build orders referencing narratives and skills. One slice per prompt, one prompt per session.
6. **Implementation** — code produced in session, closed with retrospective. Retrospective feeds back into Event Model and narratives.

**Key principle:** This sequence has a feedback loop. Retrospectives can update any upstream artifact. The sequence is not waterfall; it is the *initial* order of operations. Subsequent work iterates within the loop.

**Erik's note:** "We can always reflect and be iterative, but this sounds great."

---

### ADR-005: Transport Selection by Flow Type

**Decision:** Transport is chosen per flow shape, not defaulted.

- **gRPC** (via Wolverine 5.32) for service-to-service calls and streaming. Unary for commands/queries, server-streaming for offer delivery and dashboards, client-streaming for GPS ingest, bidirectional where interactive flows justify it.
- **Kafka** (via Wolverine's Kafka transport) for high-volume telemetry (GPS pings, breadcrumb trails) and stream-processing inputs (surge pricing signals).
- **Azure Service Bus** for the business-event backbone. Sign-up events from Entra via Microsoft Graph, cross-service domain events that benefit from ASB features (dead-letter queues, sessions, scheduled delivery).

**IMPORTANT CORRECTION FROM ERIK:** ASB is **not** deferred or optional. It is a committed transport. The project will use Azure Service Bus. The timing is what's flexible — Kafka-only for the first few milestones, with ASB introduced when Entra integration lands or when business-event features are needed. But the decision to use ASB is made. If that decision were ever reversed, a new ADR would supersede this one.

**Framing:** "Kafka-only for initial milestones" is a phasing decision within the ADR, not a deferral of the ASB decision itself. The ADR should state clearly: CritterCab uses three transports. The rollout is phased. Kafka comes first because the telemetry use case is the most immediate. ASB comes second when the business-event backbone is needed.

---

### ADR-006: Identity Provider as Swappable Anti-Corruption Layer

**Decision:** The Identity BC translates provider-specific events into domain events at the boundary. No other service knows or cares who issues tokens or emits user-lifecycle events.

**Providers:**
- **Entra External ID** — production identity provider for riders and drivers.
- **OpenIddict** — demo-mode identity provider. Chosen for active maintenance, strong .NET integration, and permissive licensing suitable for an open-source reference architecture.
- The design supports alternative OIDC providers (Keycloak is the most likely second implementation for local development).

**Entra ID (workforce tenant)** for Operations users is a separate concern, explicitly parked until the Operations BC is actively being built.

---

### ADR-007: Azure as Deployment Target

**Decision:** Azure is CritterCab's deployment platform. This is a project goal, not just an infrastructure choice — CritterCab is a "works well with Azure" story.

**Erik's framing:** This is a "3rd or 4th goal" of the project. It speaks to developers and teams who are invested in the Microsoft/Azure ecosystem, whether they frame it that way or not.

**Specific Azure services (Container Apps vs. App Service vs. AKS) are deferred** until the first cross-service integration is demonstrable end-to-end. The ADR captures Azure as the platform commitment; a future ADR captures the specific hosting model.

**This also supports:** Entra External ID for identity, Azure Service Bus for business events, and potentially Azure-native observability (Application Insights alongside or instead of Jaeger). The Azure commitment makes the ASB and Entra choices more coherent — they are part of a platform story, not isolated tool choices.

---

### ADR-008: MIT License

**Decision:** MIT license. Same license the JasperFx projects use. Maximizes adoption friction-free for a reference architecture whose purpose is to showcase the Critter Stack.

**This is a short ADR.** Options: MIT, Apache 2.0, GPL variants, no license. MIT wins because the project's goal is maximum visibility and zero friction for developers evaluating the Critter Stack. Apache 2.0 is a reasonable alternative (adds patent protection) but is less common in the JasperFx ecosystem.

---

### ADR-009: Protobuf Contracts as First-Class Artifacts

**Decision:** gRPC service and message definitions (.proto files) are design artifacts. They are reviewed with the same care as API contracts and evolved with intention, not generated as afterthoughts.

**Erik's aspirational note (not yet a decision, but capture it):** Erik is interested in exploring protobufs as the contract format not just for gRPC but for *all* transports and event persistence — Kafka messages, ASB messages, Marten events, Polecat events. The idea is a single schema language governing all serialization boundaries. This is not a current commitment. It is a future experiment. The ADR should capture it as a "future consideration" or "open question" section, not as part of the decision. If and when this experiment happens, it would get its own ADR.

**What IS decided:** Proto files are first-class. They live in a known location in the repo. They are versioned. They are reviewed. Changes to proto files are treated as breaking-change candidates until proven otherwise.

---

### ADR-010: Service Topology (SOON)

**Not ready to draft.** The 11 tentative BCs need to collapse into 6-8 deployable services. This decision is triggered by Event Modeling workshop step 6 (swim lanes / Apply Conway's Law).

**Current candidates for grouping (from vision doc):**
- Identity + Rider Profile → Riders service
- Identity + Driver Profile → Drivers service
- Pricing into Trips (or separate from day one)
- Ratings into Trips (or separate from day one)
- Dispatch stays separate in all configurations
- Telemetry stays separate in all configurations

---

### ADR-011: Narrative Format (SOON)

**Not ready to draft.** The format for `docs/narratives/` files is an open question. Options under consideration: freeform markdown with structured headers, Gherkin-flavored templates, stricter schema-backed format.

**Erik's note:** "I want us to leverage all of those where we think they are most applicable. So let's be careful and make notes of what is working and what isn't. Communication is key."

**Trigger:** Write the first 2-3 narratives in candidate formats after the Event Modeling workshop. Compare what holds up in practice. Then write the ADR.

---

### ADR-012: Event Modeling Tooling (SOON)

**Not ready to draft.**

**IMPORTANT: Do not assume Miro.** Erik indicated he may have access to a new platform — possibly a beta, public test, or early access program — that is purpose-built for Event Modeling. The earlier research doc recommended Miro for the first workshop; that recommendation may be overtaken by events. Wait for Erik to confirm the tooling before drafting this ADR.

**Trigger:** First workshop completion, or earlier if the new platform materializes.

---

### ADR-013: Polyglot Service Language and Boundary (SOON)

**Not ready to draft.** Go is the planned language for the first non-.NET service. The specific BC it lives in and the Protobuf contracts governing the integration are not yet decided.

**Trigger:** First slice that crosses the .NET/Go boundary during implementation.

---

## Other Decisions Locked In (not ADR-worthy on their own, but part of the record)

- **Skills directory (`docs/skills/`) is intentionally empty.** Skills will be fleshed out as their own process/session. Don't pre-populate them. But don't forget they need to happen — the first implementation prompts will need to bootstrap skills alongside code.
- **CI/CD (`.github/` workflows) deferred until runnable code exists.** No action needed now.
- **The vision doc (`docs/vision/README.md`) is at v0.2.** It will need a v0.3 update after the ADRs are written, to cross-reference them and to remove items from the "Open Questions" section that the ADRs have resolved.
- **The retrospective loop includes a model-update step.** Every retrospective asks: "Did this slice's implementation teach us anything that should update the Event Model or the narrative?" If yes, those updates go in the same PR as the retro. This is part of the spec-anchored regime (ADR-003).

---

## Suggested Order for Drafting

The ADRs that are READY can be drafted in any order, but this sequence minimizes forward references:

1. ~~001 — Record Architecture Decisions~~ (DONE)
2. 002 — Distributed Services per Bounded Context (foundational — everything else assumes this)
3. 005 — Transport Selection by Flow Type (depends on 002's service model)
4. 006 — Identity Provider as Swappable ACL (depends on 002's service boundary concept)
5. 007 — Azure as Deployment Target (independent but contextualizes 005's ASB commitment)
6. 003 — Spec-Anchored Development (independent, methodology-focused)
7. 004 — Design-Phase Workflow Sequence (depends on 003's spec-anchored framing)
8. 009 — Protobuf Contracts as First-Class Artifacts (depends on 002 and 005)
9. 008 — MIT License (independent, can go anywhere)

---

## Key Reference Files

The next session should read these files for context:

- `docs/decisions/001-record-architecture-decisions.md` — the ADR template
- `docs/decisions/README.md` — the ADR index (update as each ADR is committed)
- `docs/vision/README.md` — the vision doc (source of truth for goals, BCs, tech stack, design principles, open questions)
- `docs/research/event-modeling-workshop-guide.md` — the Event Modeling research (Lessons 12 and 13 specifically inform ADR-003 and ADR-004)
- `docs/research/ride-sharing-lessons-learned.md` — ride-sharing engineering research (informs ADR-002 and ADR-005)
- `CLAUDE.md` — the AI development guidelines routing layer
- This file (`docs/decisions/adr-working-notes.md`) — then delete it when done

---

## Document History

- **2026-04-23:** Created from the initial ADR planning session. Captures decisions from chat, Erik's corrections, and drafting guidance for the next session.
