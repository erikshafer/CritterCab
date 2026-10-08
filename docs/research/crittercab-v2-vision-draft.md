# CritterCab Vision v1.0 (DRAFT for v2)

> **Status: draft, not live; no brackets remain.** This is a research document holding the text of `docs/vision/README.md` v1.0 for CritterCab v2. It is not an edit to the live vision (v0.8). It becomes live in the boundary session ([`docs/prompts/crittercab-v2-boundary.md`](../prompts/crittercab-v2-boundary.md), Ready as of 2026-10-08), which replaces the live file wholesale with the text below and carries the v0.1 to v0.8 Document History entries over beneath the v1.0 entry. Every owner call that was bracketed on 2026-10-05 was resolved on 2026-10-08 and is recorded in [`docs/planning/2026-10-05-crittercab-v2-evaluation.md`](../planning/2026-10-05-crittercab-v2-evaluation.md) § Decision.

## What this is

CritterCab is a ride-sharing reference architecture built on the Critter Stack (Wolverine, Marten, Polecat, Alba) for .NET. It is the portfolio's distributed showcase: separately deployed services, one per bounded context, communicating only over gRPC calls and Wolverine messages, with the transport for each flow chosen by the flow's shape. It joins CritterMart (ecommerce, Event Sourcing with Marten) and CritterBids (auctions, sagas and real-time bidding) as the third public Critter project maintained by Erik Shafer.

This is version 1.0 of the vision and the first version written for CritterCab v2. v1 (April to July 2026, tagged `v1` at `4d8bde4`) produced two services, one cross-service flow, six workshops, nineteen ADRs and a methodology that ran ahead of its code. v2 keeps the code and the design record, cuts the inventory to what will be built, and makes the Event Model a declared, machine-readable artifact from the first commit. The reasoning is in the v2 evaluation and the dual-evaluation comparison; this document states the outcome.

## Goals

**Primary: practise Event Modeling properly, with the model as a checked artifact.** Every bounded context is modelled on the EventModelers.AI canvas as a real seven-step pass, exported, imported into a committed curated `*.emodel.yaml` per service (the authored surface), executed as Gherkin through Bobcat, and compared against the rung derived from the running application. Status is never asserted in any file; it is derived. Markdown workshops are the minutes of the conversation.

**Secondary: showcase the Critter Stack in a distributed, multi-transport, multi-store system.** Wolverine's gRPC feature set in all four modes across real service boundaries; Kafka for high-volume telemetry; Azure Service Bus for business events (built at the first availability slice; decided 2026-10-05); Marten on PostgreSQL and Polecat on SQL Server in sibling services, so "one handler across three databases" is a visible diff between services and CritterWatch's Event Store Explorer has a mixed-store system to show.

**Tertiary, parked with triggers:** rider, driver and operations frontends (trigger: Trips' first driver-app slice; screens come from the model's storyboard, not invented); Entra External ID (trigger: the Identity arc); an Azure deployment (trigger: a talk that needs a URL; none is committed); Onboarding as a bounded context (trigger: the Identity arc has shipped `identity.driver-registered`, its entry event; it returns only as a new canvas chapter with W003 and W004 as input and stays off the deployable ceiling until then).

## What CritterCab demonstrates today

A GPS ping enters Telemetry over a Wolverine gRPC client stream, is throttled on cell change or heartbeat, is published to Kafka as binary protobuf partitioned by driver, and lands as a last-writer-wins document in Dispatch, where an H3 k-ring query selects candidates. The availability side of that document has no feeder yet, so a real run honestly reports `NoCandidatesAvailable`. The first slice of v2 closes that gap.

## Bounded contexts and services

Five bounded contexts, five deployables at full extent, built in this order. This is a ceiling and an order, not an inventory with status; what exists is read from the declared files and the running AppHost.

**Dispatch** (Marten, PostgreSQL). Ride-request lifecycle from request through assignment: fare quoting, candidate selection, offer fan-out, acceptance, timeout, re-dispatch. Owns the `NearbyAvailableDrivers` view as a join of Telemetry's location stream and Driver Profile's availability events (ADR-018). Home of the server-streaming and bidirectional gRPC surfaces. Built: request, fare-quote automation, candidate selection, the Kafka consumer and the k-ring view.

**Telemetry** (Marten, PostgreSQL). High-volume GPS ingest over gRPC client streaming, throttled and published to Kafka; last-known-position document with eviction; one event-sourced stream for its policy. Stream-processing shape; availability-agnostic. Built in full for its first chapter.

**Driver Profile** (Polecat, SQL Server). The first new service of v2. Availability transitions (`DriverCameOnline`, `WentOnBreak`, `WentOffline`, `VehicleChanged`), registered vehicles, service area. Publishes availability as business events consumed by Dispatch. Vehicle sub-domain folds in.

**Trips** (Marten, PostgreSQL). Trip lifecycle from `TripMatched` through completion or cancellation, modelled in W002 as a single aggregate; Pricing (fare quoting over unary gRPC, surge later) and Ratings fold in as slices. Second arc.

**Identity** (Marten, PostgreSQL; a separately deployed host). The anti-corruption layer to the identity provider per ADR-006 Option C: the single point of contact with the provider, translating its lifecycle events into domain events; rider and driver registration; Rider Profile folds in. W005's translation-dominant shape. Third arc. A per-service ACL library was considered and declined because it is ADR-006's rejected Option B (decided 2026-10-08).

Not in v2's build list: Onboarding (parked with the trigger above; W003 and W004 remain as the Domain Storytelling to Event Modeling exhibit), Payments, Operations, Trust & Safety, Notifications, Documents. Every forward-constraint and ADR candidate addressed to these is released in the boundary session's note, not carried; Onboarding's are marked parked rather than released.

## Technology stack

| Concern | Choice | State |
|---|---|---|
| Language and runtime | C# 14, .NET 10 | |
| Handlers, HTTP, gRPC, transports | Wolverine (6.38.0 at the boundary; 5.32 remains the gRPC floor that motivated the project) | Built |
| Event sourcing and documents, PostgreSQL | Marten | Built |
| Event sourcing and documents, SQL Server | Polecat (current 5.x) | Enters at the Driver Profile skeleton |
| Integration tests | Alba, xUnit, Testcontainers | Built |
| Local orchestration | Aspire, as a project in the solution | Converted at the boundary |
| High-volume streams | Kafka via Wolverine | Built |
| Business events | Azure Service Bus via Wolverine | Enters at the availability slice (decided 2026-10-05) |
| Browser push | SignalR (ADR-016) | Parked with the frontend |
| Monitoring and the derived Event Model | CritterWatch, RabbitMQ as its backplane only (ADR-017) | Enters after the first v2 slice (decided 2026-10-08) |
| Executable specifications | Bobcat (Gherkin) | When shipped; declared files do not wait on it |
| Contracts | Protobuf, hand-authored under `protos/`, `buf lint` in CI | Two of seven compiled |
| Geospatial | H3 (pocketken.H3) | Built |

Rule of admission, in force from the boundary: nothing is pinned in `Directory.Packages.props`, wired in the AppHost, described in a skill, or called "committed" in an ADR unless code in this repository exercises it under CI in the same PR or the next.

## Methodology

**Event Modeling** (Adam Dymitruk) is the design method. The slice patterns are Dymitruk's Command, View, Automation and Translation; Klefter's translation-decision events and Bruun's temporal automations and configuration-as-events are refinements of Translation and Automation, not additions. The seven steps are run in full for each new chapter, including the storyboard: every slice carries a wireframe or an explicit "no screen: machine actor" statement, and a field-level information-completeness check is part of a slice's definition of done. JasperFx's descriptor terms are used inside the curated file; Wolverine's handler words never enter the pattern slot; Dilger's State Change / State View vocabulary is not used in this repository (the portfolio keeps a crosswalk).

**The authored surface** is the curated `*.emodel.yaml` committed per service. The EventModelers.AI canvas is the modelling room for every new chapter and an import path (export committed, `bobcat import-event-model`, curated diff reviewed in the PR), never the record. Slice names are the merge key; renaming a slice is a refactor. No file asserts status.

**Domain Storytelling** (Hofer) precedes Event Modeling for vocabulary-rich, multi-actor contexts (methodology log entry 005). **Grill-with-docs** precedes any workshop where ownership boundaries are nuanced (entry 006), and is limited to boundary questions so that slice discovery happens on the board.

**DDD strategic design** stays first-class: `docs/context-map/README.md` is updated in the same PR as any workshop that touches a cross-context edge.

**Sessions** follow one prompt, one session, one PR, with a spec delta stated in model-shaped terms (which slices move from declared to realized, which forward-constraint closes), source verification at prompt-authoring time with the HEAD of each verified upstream logged, the skill-auditor before and after code, and a two-axis code review before the PR. The retrospective rides in the PR in a five-section template (metadata; what landed; spec delta landed; what disconfirmed; next-session inputs); cross-cutting observations go to `docs/research/methodology-log.md`, skill gaps to `docs/skills/DEBT.md` (decided 2026-10-08). After two or three implementation PRs on one context, the next PR is a design or tidy return. Narratives (`docs/narratives/`) are optional exhibits, not a phase.

## Deliberately not using

RabbitMQ for domain flows (ADR-005; it runs only as CritterWatch's backplane, ADR-017). Redis. A Go or other polyglot service (dropped 2026-10-08; the protobuf contracts remain the door if a non-.NET consumer is ever wanted). Clean Architecture and Onion Architecture. A build-kit loop or status write-back to any modelling canvas. Fisher (its home in the portfolio is elsewhere). A statechart runtime, a workflow engine, a service mesh, a consumer proxy over Kafka (see `docs/research/ride-sharing-lessons-learned.md` § What NOT to copy).

## Design principles

Carried from v1 unchanged: services per bounded context, not per whim (ADR-002); transport split by flow type, not by convenience (ADR-005); identity provider is swappable (ADR-006); contracts are first-class (ADR-009); capture intent in durable, structured form (ADR-003); aggregate boundaries around invariants, not nouns (ADR-012); one canonical identifier per lifecycle across contexts (ADR-013); observability from day one; tradeoffs explicit (ADR-001).

New in v2: **the model is checked, not asserted** (the derived rung may disagree with the declared file, and that disagreement is the signal); **admission by exercised code** (the rule above); **workshop vocabulary wins over framework naming, including in the pattern slot**; **do not fabricate what the model has no source for** (the availability lesson of PR #47); **seams one PR early** (`INearbyAvailableDriversSource`, `IDriverLocationPublisher`).

## ADRs

ADRs 001 to 019 are the v1 record. Each is re-affirmed, amended or superseded by the first v2 PR that touches its subject; the index carries a preface saying so. Known at the boundary: 002 (service band), 003 (sync mechanism), 004 (narratives optional), 005 and 014 (ASB status), 007 (URL clause parked), 010 (Polecat is an event store; version), 015 and 016 (unexercised) are due for amendment; 006 is re-affirmed as written; 018 and 019 are the exercised teaching record. ADR-020 is proposed as the v2 topology decision (five contexts, five deployables, build order), resolving W001's unfired service-topology, fan-out-topology and Pricing-location candidates. All require explicit sign-off before authoring.

## Open questions

One, carried to the portfolio: the methodology substrate's home. Whether the shared pipeline and the shared skills (the corrected `event-modeling` skill first) move to one place the three repos vendor, using `.agents/skills/` and `skills-lock.json` as the vehicle the way the Pocock skills are vendored today, is a three-repo question and is decided in a portfolio session, not here (deferred 2026-10-08). Until then the `event-modeling` skill is written to be copied verbatim between repos.

## Related documents

Minutes: `docs/workshops/` (001 to 006). Declared files: beside each service's `Program.cs`, following whatever placement, naming and schema convention CritterMart's Orders experiment reports (decided 2026-10-05; none exist yet). Decisions: `docs/decisions/`. Context map: `docs/context-map/README.md`. Research: `docs/research/`, in particular the ride-sharing lessons, the methodology log and the canonical Event Modeling sources. Prompts and retrospectives: `docs/prompts/`, `docs/retrospectives/`. The v2 evaluation: `docs/planning/2026-10-05-crittercab-v2-evaluation.md` and `docs/research/crittercab-v2-evaluation-comparison.md`.

## Document history

- **v1.0 (draft, brackets resolved, 2026-10-08):** Owner calls applied from the grill session: Identity fixed as a separately deployed host (ADR-006 Option C); Go dropped and moved to "Deliberately not using"; Onboarding parked with the Identity-arc trigger; CritterWatch enters after the first v2 slice; the five-section retro template named in Methodology; the substrate's home left as the single open question with the vendoring vehicle named; `v1` tag name and Wolverine 6.38.0 recorded. Ready to be made live by the boundary session.
- **v1.0 (draft, 2026-10-05):** Proposed in the v2 evaluation session. Not live. Supersedes v0.8's eleven-context inventory with five contexts in five deployables and a build order; re-ranks goals (Event Modeling practice first, Critter Stack showcase second); adopts the portfolio's declared-model workflow; adds the admission rule; parks the frontend, Go, Entra and Azure deployment with triggers; records Identity's form, CritterWatch, Go, Onboarding's return and the retro template as open owner calls (ASB, Pricing and declared-file timing were decided 2026-10-05).
