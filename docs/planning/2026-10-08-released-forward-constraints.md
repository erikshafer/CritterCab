# Released forward-constraints and retired ADR candidates

**Written:** 2026-10-08, in the boundary session ([`docs/prompts/crittercab-v2-boundary.md`](../prompts/crittercab-v2-boundary.md)). **Authority:** [`docs/planning/2026-10-05-crittercab-v2-evaluation.md`](./2026-10-05-crittercab-v2-evaluation.md) § Decision and its 2026-10-08 addendum; [`docs/vision/README.md`](../vision/README.md) v1.0.

Unlike most notes in this directory, this one is not disposable: it is the single place that says which promises the workshops made to bounded contexts that v2 does not build. The vision builds five contexts (Dispatch, Telemetry, Driver Profile, Trips with Pricing and Ratings folded in as slices, Identity with Rider Profile folded in). Everything the minutes addressed to a context outside that list is listed here so that no later session treats it as live. Nothing in the cited files is edited; the minutes keep saying what they said, and this note is what to read alongside them.

Three dispositions are used:

- **Released.** The constraint is no longer carried. If its target ever enters the build list, the target's own canvas chapter re-derives what it needs from the minutes; nothing here binds that chapter.
- **Folded.** The target becomes slices inside a surviving context (Pricing and Ratings into Trips, Rider Profile into Identity). The *content* of the constraint survives as an input to that context's arc; the constraint *as addressed to a separate host or BC* is closed.
- **Parked (Onboarding only).** Onboarding is parked with a named trigger, not dropped: it returns, if at all, after the Identity arc ships `identity.driver-registered`, as a new canvas chapter with W003 and W004 as input, off the five-deployable ceiling until then. Its entries are frozen with it, not released.

Numbering follows each document's own: W004 and W005 put forward-constraints in a section titled **§X+1** and DS findings in **§X**, with ADR candidates in **§11**; W001's ADR candidates are §11 and its parking lot §10; W002's ADR candidates are §12 and its forward-constraints table §13.

---

## Onboarding — parked with its trigger

| Origin | What it says | Disposition |
|---|---|---|
| W004 §11, NEW candidate #1 | Process Manager via Handlers as a modeling pattern ADR. | Released from the live ADR-candidate list (the prompt's "W004's two ADR candidates release"); returns with Onboarding only by being re-derived in its canvas re-model, which is the trigger, not this note. |
| W004 §11, NEW candidate #2 | Multi-vendor ACL absorbed inside the process manager, generalizing ADR-006 (paired with #1). | As above. |
| W004 §11, sub-discipline; §12.10 Q2 | Push for workflow continuation, pull for information flow; the webhook-routing friction (`BackgroundCheckVendorCaseIndex`) as an input to candidate #1. | Parked inside the candidate pair. |
| W004 §11, ADR-015 bullet | ADR-015 amendment once Onboarding latency measurements exist. | Parked. |
| W004 §X+1 #1–#3 (Slice 6.1) | Identity publishes `identity.driver-registered` (at-least-once) and authors `DriverRegistered`. | Parked as Onboarding's ask. Identity publishes the event anyway for its own reasons (W005 Slices 6.2/6.3, §X #3); proto authorship stays with Identity's arc. |
| W004 §X+1 #4–#5 (Slice 6.8) | Driver Profile consumes `onboarding.driver-approved` and creates the profile at `driverProfileId == applicationId`; `DriverApproved` proto. | Parked. Driver Profile's first chapter does not depend on Onboarding; when Onboarding returns, the edge is drawn on the canvas then. |
| W004 §X+1 #6–#12 | Addressed to Notifications, Trust & Safety and Operations (listed under those targets below). | Released with their targets; Onboarding's side is parked. |
| W004 §10, §12.8 | `/protos/crittercab/onboarding/v1/` (`DriverApproved`, `AdverseActionNoticeRequired`, deferred `ApplicantApprovalNotificationRequired`). | Parked; not authored. |
| W004 §6.3, §6.4b, §6.5, §6.7b, §6.10; §X.3 OQ-3, OQ-11, OQ-13; §12.8 | Parking lot: sensitive-PII vault, document retry-budget enforcement, vendor `Suspended` flow, FCRA dispute filing, re-application policy, vehicle as a sub-domain, `DocumentTimeline`. | Parked. (Vehicle as a sub-domain also folds into Driver Profile per the vision; that is Driver Profile's call on its own canvas.) |
| W004 §12.8 | Two skill follow-ups (per-vendor reason-code mapping; blob-storage handles). Never registered in `docs/skills/DEBT.md`. | Parked; deliberately not registered, since no Onboarding code exists. |
| W003 §5.3 OQ-14 to OQ-19 | Suspension ownership, vehicle inspection, selfie or liveness matching, cross-jurisdiction moves, document-expiry re-vetting, vendor revising a result after approval. | Parked. |
| Workshops README, "From W003" row 3 | Codify the Domain Storytelling notation as a skill. | Parked: only Onboarding exercised DS. |
| Context map edges #3 (Identity → Onboarding), #6 (Onboarding → Driver Profile), #7 (Onboarding → Operations, Notifications) | Supplier sides locked by W004/W005; consumer sides pending. | Parked; the edges stay on the map as drawn and are not extended. |

## Pricing — folded into Trips

| Origin | What it says | Disposition |
|---|---|---|
| W001 §10 #1 | Pricing's physical location: inside Trips or a separate BC. | Closed by the fold. |
| W001 §11 #4 | Pricing-location ADR candidate. | Closed by the fold; to be recorded by the proposed ADR-020 (v2 topology) when signed off. |
| W001 §2.2, §5.2, §9; `protos/crittercab/pricing/v1/get_fare_quote.proto` | Dispatch → Pricing unary `GetFareQuote`. | Folded: the contract survives as the unary call Dispatch makes to Trips' Pricing slices. |
| W001 §5.8, §5.9; W002 §2.2, §6.10, §7, §10 | Pricing consumes cancellation, abandonment, completion and no-show topics for fee and fare-finalization logic. | Folded into Trips' arc as input. |
| W001 §10 #3, §2.3; W002 §2.3; W006 §2.3, §3.5, §12; ADR-005, ADR-019 (`pricing.surge-updated`) | Surge pricing, including a Kafka surge consumer. | Released: surge is post-MVP and nothing in the build list needs it. ADR text stays as written until a PR touches ADR-005 or ADR-019. |
| W001 §5.6, §5.7, §5.8, §5.9, §5.12; W002 §6.4, §6.6 | Deferred projections naming Pricing as an audience (`RequestFailurePatternsByRegion`, `ExpiryHotspotsByRegion` and others). | Released. |
| W002 §4 ("Trip ID"); ADR-013 Consequences | Pricing receives the canonical ride identifier. | Folded: holds inside Trips without a boundary. |
| Context map edge #4 (Trips → Pricing) and the deferred Dispatch → Pricing edge | Presumed conformist. | Folded; the edge becomes internal to Trips. |

## Ratings — folded into Trips

| Origin | What it says | Disposition |
|---|---|---|
| W002 §2.2, §3.4, §6.10, §7, §10 | Ratings consumes `trips.trip-completed` for rating invitations. | Folded: a post-`TripCompleted` slice in Trips. |
| W001 §5.10, §11 #7; ADR-013; ADR-002 Option B | Ratings' `rideId` is the canonical identifier; weaker case for a separate service. | Folded. |
| Context map edge #4 (Trips → Ratings) | Presumed conformist. | Folded; internal to Trips. |

## Rider Profile — folded into Identity

| Origin | What it says | Disposition |
|---|---|---|
| W005 §X+1 #1; W005 §X #2, §3.6; W002 §13 #2, §6.12; ADR-014 (names `identity.rider-profile-updated`) | The reassigned rider-profile-updated event that Trips' `RiderProfileSnapshot` subscribes to. | Folded: Identity's arc publishes it (or decides not to). The W005 grill's logical separation of profile from identity stays an input to that arc. |
| W005 §X+1 #2 | Rider Profile consumes `identity.rider-registered`. | Folded: internal to Identity. |
| W001 §5.8, §5.9, §5.10, §6, §9, §10 #9; W002 §6.4, §6.10, §10 | Rider Profile consumes Dispatch and Trips topics; rider-suspension propagation; `RiderNoShowRate`; `RiderCancellationRateByReason`. | Folded into Identity's arc as input where it is about the rider's profile; the rest released (no rider notifications are built). |
| Context map edge #3 (Identity → Rider Profile), edge #4 (Trips → Rider Profile) | Supplier locked, consumer dashed; presumed conformist. | Folded. |

## Payments — released

| Origin | What it says |
|---|---|
| W001 §2.3, §5.8, §5.10, §5.12, §9 | Payment authorization assumed upstream; cancellation-fee logic; `paymentReference`; `CrossBcTraceProjection`; `WastedAssignmentAudit`; consumer of `AssignmentOutcomeRecorded`. |
| W002 §2.2, §2.3, §3.4, §6.4, §6.5, §6.10, §7, §10, §11 #5 | Authorization at trip start, capture on completion, void or refund on cancellation; `PaymentAuthAnchorEvents`; authorization failures as a Payments concern. |
| W001 §11 #7 / ADR-013; ADR-002 Option B; ADR-019 (`payments.fare-settled`) | Payments as a separate service inheriting the canonical identifier; a topic-name example. |
| Context map edge #4 (Trips → Payments) and the potential Dispatch → Payments edge; narrative 002 "Separate workshops" | Presumed conformist, possibly customer-supplier. |

All released. ADR text stays as written until a PR touches ADR-002, ADR-013 or ADR-019. Trips' payload choices made for Payments (for example `riderId` echoed on `trips.trip-completed`) are Trips' own and are reconsidered on Trips' canvas.

## Operations — released

| Origin | What it says |
|---|---|
| W004 §X+1 #9, #10, #11, #12 | Adjudicator queue and SLA tracking over `AdjudicatorQueueView*`; Operations owns workforce identity for `adjudicatorId`; vendor no-response escalation; may consume the approval-notification topic. |
| W004 §3.10, §6.1, §6.6, §6.9, §6.10; W003 §2.3, §5.1 V3, §5.2 B5, §5.3 OQ-10, OQ-14 | Adjudicator tooling, onboarding-funnel metrics, policy admin UI, reviewer vocabulary. |
| W001 §2.3, §5.6–§5.12, §6, §9; W002 §2.3, §6.10, §6.11, §7, §10 | Manual reassignment; consumer of Dispatch and Trips topics; the admin surface authenticated "by API gateway + Operations BC". |
| W006 §6.4, §6.5, §12 | `ActiveDriversByCell`, `CandidatePoolHealth`. |
| ADR-006 Decision and Consequences; W005 §2.3, §3.6 | The workforce Entra tenant, parked until Operations is built. |
| Context map edges #4 and #7; narrative 001 "Separate narratives" | Presumed conformist from Trips; expected customer-supplier from Onboarding; the operator reassignment journey. |

All released. Every operator action already modelled (W001 §5.11, W002 §6.11, W004 §6.10, W006 §6.1) is a config-as-events command inside its owning context; where those slices say the operator is "authenticated upstream (API gateway + Operations BC)", the authentication question returns to Identity's arc. ADR-006's parked workforce-tenant decision stays parked as written.

## Trust & Safety — released

| Origin | What it says |
|---|---|
| W004 §X+1 #8, §6.9, §X.3 OQ-10e | T&S pulls Onboarding's non-approval terminal facts via `OnboardingTerminalFactsView`. |
| W001 §10 #13, #14; §5.6, §5.8; W002 §6.8 | `notesForDriver` visibility; `SAFETY_CONCERN` as a distinct T&S command, event and route. |
| W002 §6.2, §6.6, §6.10, §7, §10; W001 §5.6–§5.12 | Consumer of driver-cancellation and no-show topics; fraud, route-deviation and reliability projections. |
| W003 §5.2 B6, §5.3 OQ-10, OQ-14; context map edge #4; narratives 001 and 002 | No T&S boundary emerged; presumed conformist; the T&S flag journey. |

All released. `SAFETY_CONCERN` stays out of the Dispatch and Trips reason enums, as the minutes already decided.

## Notifications — released

| Origin | What it says |
|---|---|
| W004 §X+1 #6, #7, #12; §2.2, §3.10, §6.7, §6.7b, §10, §X.3 OQ-10d | Consume `onboarding.adverse-action-notice-required` (both FCRA phases) and send email or SMS; author its proto; maybe the approval-notification topic. |
| W004 §X+1 closing note, §12.8; workshops README "From W004" row 4; context map edge #7 | Notifications was never in the vision's inventory; whether it is a BC or part of Operations was open. |

Released. Notifications was never admitted; the FCRA notice obligation belongs to Onboarding and is parked with it.

## Documents — released

| Origin | What it says |
|---|---|
| W003 §5.3 OQ-18 (source: `docs/research/ride-sharing-driver-onboarding-domain-note.md`) | Document-expiry re-vetting: Onboarding, Driver Profile, or a dedicated Documents BC. |

Released as a BC question. The re-vetting question itself is parked with Onboarding.

## The Go service — released (dropped 2026-10-08)

| Origin | What it says |
|---|---|
| ADR-009 Consequences | A Go service participates over gRPC as a first-class consumer of the same contracts. |
| `docs/planning/2026-06-25-state-of-the-repo-transport-and-critterwatch.md` | "Option D — gRPC end-to-end with the polyglot Go client"; "Phase 3 — Polyglot Go gRPC consumer". |
| `protos/buf.gen.yaml` (Go plugins), and the `polyglot-go-service`, `testing-advanced`, `observability-metrics`, `cli-grpc-tooling`, `protobuf-contracts`, `transport-selection` and `aspire` skills | Go codegen, a `cab-go` matchmaking service, polyglot boundary tests, OTLP metrics from Go. |

Released. The Go plugins left `buf.gen.yaml` and `polyglot-go-service` moved to `docs/skills/archive/` in the same session; ADR-009's sentence stays as written until a PR touches ADR-009. The protobuf contracts remain the door if a non-.NET consumer is ever wanted.

---

## What stays live

Not released, listed so the boundary is unambiguous:

- **W005 §11** — ACL-as-BC / translation-dominant as a modeling shape; trigger: Identity's first implementation slice.
- **W006 §11 #2** — stream-processing as a modeling shape (proposed with W005's as one "modeling shapes" ADR when both contexts are declared); **W006 §11 #3** — the windowed client-streaming ingest pattern, which may land as a skill instead. **W006 §11 #1** is already authored as ADR-019; the W006 file and the workshops index still list it as pending, which is minutes drift, not a live candidate.
- **W001 §11 #1 and #2** (service topology, fan-out topology) — resolved by the evaluation's context disposition and to be proposed as ADR-020 with W001 §11 #4.
- Every constraint between the five built contexts (for example W006 §12's Driver Profile availability on Azure Service Bus, which is the first v2 slice).
