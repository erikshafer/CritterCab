# Prompt 005 — Fourth Event Modeling Workshop (Identity)

| Field | Value |
|---|---|
| **Status** | Authored (not yet run); session pending. Pre-grill draft — leans on three hard questions are marked as grill-with-docs candidates. |
| **Authored** | 2026-05-27 |
| **Target artifact** | `docs/workshops/005-identity-event-model.md` |
| **Companion artifacts** | [`docs/workshops/README.md`](../../workshops/README.md) (Workshops list + § Workshop follow-ups — close the Identity-workshop forward-constraint rows from W002 §14.8 and W004 §X+1); conditionally [`docs/research/methodology-log.md`](../../research/methodology-log.md) (entry 006 if warranted — strong candidate: the entry-005 contrast test); conditionally [`docs/context-map/README.md`](../../context-map/README.md) (edge #3 Identity outbound family — lock the consumer-facing PL edges that Identity's publications enable) |
| **Source-of-truth dependencies** | [`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) (**the locked ACL stance — this workshop is its first full exercise, not a relitigation**); [`docs/context-map/README.md`](../../context-map/README.md) edge #3 (Identity ↔ provider ACL + Identity → consumer-BC PL family); [`docs/workshops/002-trips-event-model.md`](../../workshops/002-trips-event-model.md) §13 forward-constraint #2 (`identity.rider-registered` + `identity.rider-profile-updated` required publications) + §14.6 methodology refinements; [`docs/workshops/004-onboarding-event-model.md`](../../workshops/004-onboarding-event-model.md) §6.1 + §X+1 #1–#3 (`identity.driver-registered` forward-constraint: `subjectId` + `registeredAt` payload, at-least-once delivery, `DriverRegistered` proto authorship); [`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md) (shape conventions + §12.6 methodology); [`docs/research/methodology-log.md`](../../research/methodology-log.md) entry 005 (the DS-as-upstream finding this workshop tests the boundary of) |
| **Workflow position** | Fourth Event Modeling workshop; fifth design-phase artifact overall (after W001 EM, W002 EM, W003 DS, W004 EM). **First BC whose primary domain job is being an anti-corruption layer.** First workshop "pulled into existence" by accumulated forward-constraints converging from three prior workshops (W002, W003, W004) rather than by a single prior workshop's "next workshop" pointer. **EM-direct — no Domain Storytelling pre-step** (Identity is a thin/mechanical BC, not vocabulary-rich; consistent with W001/W002 precedent and methodology log entry 005's scoping of DS to vocabulary-rich BCs). Returns to the **design phase** per ADR-004's design-return cadence rule after the W004 PR. |

---

## Spec delta

This session is **spec-creating** for Identity's event model, **spec-confirming** for the three accumulated inbound-contract forward-constraints (W002 + W003 + W004), and **possibly pattern-introducing** if Identity's ACL shape proves to be a distinct CritterCab modeling pattern (translation-dominant / thin-or-no-aggregate) alongside aggregate-cluster (W001/W002) and Process Manager via Handlers (W004).

- **`docs/workshops/005-identity-event-model.md` is created** — first event model for the Identity BC. Commits the provider-ACL translation-in slices, the domain-event publication slices, and the modeling shape of a thin ACL boundary.
- **The three accumulated forward-constraints are honored or refined with documented reasoning** in a §X "Forward-constraints handled" section (mirrors W002 §13). `identity.rider-registered` and `identity.driver-registered` almost certainly honored; `identity.rider-profile-updated` is the one to scrutinize — see the boundary question in §Scope.
- **Locked ADRs gain evidence:**
  - **ADR-006 (Identity-as-swappable-ACL)** — **first full exercise.** Not a relitigation; the workshop realizes the stance as concrete translation slices. The provider (Entra / OpenIddict / Keycloak) is modeled provider-agnostically per ADR-006; per-provider specifics are config, not events.
  - **ADR-014 (ASB topic naming)** — `identity.*` topics (`identity.rider-registered`, `identity.driver-registered`, possibly `identity.rider-profile-updated`). Fourth BC to apply the convention.
  - **ADR-013 (shared cross-BC identifier)** — **nuance, not a new participant.** Identity's `subjectId` is an *intra-BC* identifier that other BCs reference as a foreign key — ADR-013 explicitly scopes such IDs out ("Identity's user IDs ... remain BC-owned; this ADR governs only cross-BC identity continuity for shared lifecycles"). The workshop confirms this reading rather than adding Identity to the canonical-ID chain.
- **Possible new §11 ADR candidate — ACL-as-BC / translation-dominant modeling pattern.** If Identity's event model turns out to be almost-all-Translation-slices with a thin-or-absent central aggregate, that's a third-or-fourth CritterCab modeling pattern worth delineating (when is a BC "just an ACL" vs. when does it warrant a real aggregate?). Surface as a candidate; do not author.
- **No existing spec is amended retroactively.** W001/W002/W003/W004 stay locked; W005 honors or refines via documented reasoning in §X, not via edits to prior artifacts. Same-PR closure of the W002 §14.8 + W004 §X+1 Identity-forward-constraint follow-up rows (index entries) is in-bounds per the same-file-edit rule.

---

## Framing — why this session exists

Three prior workshops left forward-constraints on Identity's eventual workshop, and they have now accumulated to the point where Identity is the BC with the most downstream work waiting on it:

- **W002 §13 forward-constraint #2** requires Identity to publish `identity.rider-registered` and `identity.rider-profile-updated` so Trips' `RiderProfileSnapshot` projection (W002 §6.12) can surface rider name to the driver post-acceptance.
- **W003 Story 1 step 4 + W004 §6.1** require Identity to publish `identity.driver-registered` so Onboarding's intake slice can create the Application process stream — with a specific payload requirement (`subjectId` + `registeredAt`), at-least-once delivery, and a `DriverRegistered` proto to author.

The [context map](../../context-map/README.md) names Identity "the strongest near-term candidate" in its §Pending workshops, and edge #3 (Identity ACL + outbound PL family) is the single most pre-committed relationship pattern in CritterCab — every part locked by [ADR-006](../../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md). This workshop turns the pre-committed stance into a concrete event model and locks the supplier side of the PL edges that three downstream BCs are waiting on.

Identity is deliberately the **opposite shape** from Onboarding (W004). Where Onboarding was a coordination-dominated Process Manager with rich parallel gates, Identity is — per the vision doc — *"deliberately thin... mostly an anti-corruption layer translating provider events into domain events."* The vision doc's Microsoft Graph deferral ("current plan covers sign-up events only") scopes the workshop tightly. This thinness is the whole point of the methodology question below: it tests where methodology log entry 005's DS-as-default-pre-EM lean stops applying.

Two secondary jobs:

1. **Provide the entry-005 contrast data point.** W004 was DS-fed (vocabulary-rich BC); W005 is EM-direct (thin/mechanical BC). The retro answers: did EM-direct on a thin BC surface mid-walk vocabulary friction that DS would have caught, or did the thin BC genuinely not need a DS pre-step? Either answer confirms or refines entry 005's scoping; methodology log entry 006 is conditional on it.
2. **Test whether a pure-ACL BC needs a new modeling pattern.** If Identity's event model is almost-all-Translation-slices with a thin-or-absent aggregate, that's a candidate third-or-fourth CritterCab modeling pattern. The §Modeling-pattern sidebar settles the shape; §11 captures whether it warrants its own ADR candidate.

---

## Goal

Run CritterCab's fourth Event Modeling workshop end-to-end and produce a durable artifact for the **Identity bounded context**, structurally parallel to [`002-trips-event-model.md`](../../workshops/002-trips-event-model.md), realizing ADR-006's locked ACL stance as concrete translation-in + publication slices and honoring (or refining) the three accumulated inbound-contract forward-constraints.

---

## Scope question to settle at session start

**Locked at prompt-authoring time to: Tight — rider + driver registration (sign-up) flows + their domain-event publications, provider-agnostic ACL.** Confirm with the user before the sidebar.

| Option | Span | Decision |
|---|---|---|
| **A. Tight (locked)** | Rider registration sign-up flow → `identity.rider-registered`; driver registration sign-up flow → `identity.driver-registered`. Provider-ACL translation-in modeled provider-agnostically (Entra / OpenIddict / Keycloak as one abstract OIDC-provider boundary). Honors the three forward-constraints. | **Locked.** Mirrors the vision-doc's "sign-up events only" scoping and ADR-006's swappable-provider stance. Likely 6–9 slices — thinner than W004. |
| **B. Tight + fuller user lifecycle** | Adds Microsoft Graph fuller-lifecycle events: password reset, MFA enrollment, account block/unblock, email re-verification. | Deferred. The vision doc explicitly defers Microsoft Graph integration depth ("current plan covers sign-up events only ... deferred until the Identity BC is actively being worked"). Out of scope unless the user pulls it in. |
| **C. Tight + workforce/Operations identity** | Adds the workforce Entra tenant for Operations users (ADR-006's parked decision). | Out of scope. Parked per ADR-006 and structural-constraints; trigger is "Operations BC actively being built." |

**Three boundary questions to settle during the walk (grill-with-docs candidates — pre-leans below):**

1. **Is `identity.rider-profile-updated` honestly an Identity event?** W002 §13 #2 demanded it, but the vision doc puts *profile data* (saved addresses, payment methods, notification preferences) in the **Rider Profile BC** ("downstream of Identity"). **Lean: Identity publishes `identity.rider-registered` (account existence — genuinely Identity's) but NOT `identity.rider-profile-updated` (profile data is Rider Profile's concern).** The W002 forward-constraint conflated account-existence with profile-data; W005 refines it — Trips' `RiderProfileSnapshot` enrichment should subscribe to a *Rider Profile* event, not an Identity event, for the display-name surface. This is a forward-constraint *refinement* (override with documented reasoning), not a simple honor. **Strong grill candidate.**
2. **Does Identity carry a thin per-actor aggregate, or is it translation-dominant with no real aggregate?** **Lean: a thin `IdentityAccount` aggregate per registered actor** (events: `RiderRegistered`, `DriverRegistered`, maybe `EmailVerified`) so account-existence has a queryable local record and the published domain events have a stream to append to — but the workshop should weigh "almost-stateless translation" honestly. See §Modeling-pattern sidebar.
3. **Does the rider-becomes-driver case (one human, both roles) need modeling?** Research note §7 (W003's Phase 0) flagged "the rider-becomes-driver" edge — existing rider opens a driver application; does the Identity account carry over? **Lean: one `IdentityAccount` per human (`subjectId`), capable of carrying both rider and driver registration facts; the actor isn't two accounts.** Worth confirming, since it shapes whether `RiderRegistered` and `DriverRegistered` land on the same stream or separate streams.

---

## What this session inherits (no re-litigation)

Locked by prior workshops + ADRs. Do not propose alternatives:

- **Workshop artifact shape** — Scope Statement, **Modeling-pattern sidebar** (§3, adapted for the ACL-shape question — structurally parallel to W002 §3 / W004 §3), Ubiquitous Language glossary, Event List, per-slice walkthrough, cross-reference tables, **§X "Forward-constraints handled"** (mirrors W002 §13), Parking Lot / Open Questions, ADR Candidates, Retrospective. See W002 + W004 for the template.
- **Slice notation conventions** — events past tense; commands imperative; views/projections named ubiquitously; Translation slices per the Klefter pattern (translation-in with a decision → local decision-event; pure routing/enrichment translation → no local event, per Klefter Post 3 + W002 §6.12's non-Klefter counter-example).
- **Klefter decision-event pattern** — Identity's provider-translation is the textbook case: provider lifecycle event → translated domain event. Whether each translation carries a *decision* (warranting a local event) or is *pure routing* (no local event) is a per-slice call. ADR-006 frames Identity as "turning provider-specific signals into domain events" — most Identity slices will be Klefter translation-in.
- **ASB topic naming convention (ADR-014)** — `identity.*` topics. Fourth BC to apply. `identity.rider-registered`, `identity.driver-registered`, session-keyed by `subjectId`.
- **Shared cross-BC identifier (ADR-013) — the nuance.** Identity's `subjectId` is an intra-BC ID other BCs reference as a foreign key; ADR-013 explicitly scopes intra-BC IDs out. Do NOT treat Identity as a canonical-ID-chain participant. (Contrast: Onboarding mints `applicationId == driverProfileId` per ADR-013 — that's a cross-BC lifecycle ID; `subjectId` is not.)
- **Per-workshop methodology refinements** — W001 §12.6 (all five) and W002 §14.6 (all five) carry forward. EM-direct means no DS-findings-handled section (there's no DS artifact); the §X section here is "Forward-constraints handled" per W002 §13.
- **§12.7 calibration** — pair open questions with leaning opinions; prefer informative depth over brevity; use ubiquitous language; assume DDD / CQRS / Event Sourcing / EDA as working background.

---

## What this session adopts as new practice

**W001 §12.6 + W002 §14.6 carry forward.** Identity-specific notes:

1. **Proactive projections from slice 1.** Identity's candidate projections are thin but real: `RegisteredActorsBySubject` (subjectId → registration facts; the lookup substrate other BCs' ACL-consumers conceptually mirror), possibly `IdentityAuditTimeline` (per-actor registration/verification history for ops/compliance).
2. **Pre-walk modeling-pattern sidebar** — see §Modeling-pattern sidebar. Run before slice 1. Settles the thin-aggregate-vs-translation-dominant question.
3. **Calibrate cadence to slice complexity.** Identity's slices are mostly light (translation-in + publish). The heavy ones: the modeling-pattern sidebar; the `rider-profile-updated` boundary refinement; the rider-becomes-driver actor-correlation question.
4. **Defer Protobuf authorship.** Name the `identity.*` protos needed (`RiderRegistered`, `DriverRegistered`); defer authorship to a follow-up session under ADR-009. The `DriverRegistered` proto is the one W004 §X+1 #3 already named.
5. **§X "Forward-constraints handled"** — document the three accumulated constraints with disposition (honor / refine / override). This is the entry-005-contrast-relevant section.

**One W005-specific new practice:**

- **§X+1 "Forward-constraints generated"** — Identity is mostly a *terminus* for forward-constraints (it receives them), but its slices may generate a few outbound (e.g., on Rider Profile's eventual workshop if the `rider-profile-updated` refinement reassigns that publication to Rider Profile; on Operations' workshop for the parked workforce-tenant decision). Consolidate per W002 §14.6 #2.

---

## Modeling-pattern sidebar (run BEFORE slice 1)

Per W001 §12.6 #2 + W004 §3, the modeling-shape decision shapes every slice. Identity's sidebar settles a question neither W001/W002 (aggregate-cluster) nor W004 (Process Manager) answered: **what's the modeling shape of a BC whose primary job is translation, with little domain state of its own?**

**The question:** Does Identity warrant a real aggregate, or is it translation-dominant (Translation slices publish domain events with thin-or-no local state)?

**Framings to consider:**

| Framing | Sketch | Lean |
|---|---|---|
| **Thin per-actor aggregate (`IdentityAccount`)** | One stream per `subjectId`; events `RiderRegistered`, `DriverRegistered`, optionally `EmailVerified`; publications fire from the same handler that appends. Account-existence is a queryable local record. | **Recommended.** Gives the published domain events a stream to append to (Klefter discipline: Identity records the *consequence* of provider translation locally), supports the `RegisteredActorsBySubject` projection, and handles the rider-becomes-driver case cleanly (both registration facts on one `subjectId` stream). |
| **Translation-dominant (no aggregate)** | Provider webhook → translation handler → published domain event, with no local stream; state (if any) lives only in projections. | Considered. Honest for a truly stateless ACL, but loses the local audit record and the queryable account-existence substrate; makes the rider-becomes-driver correlation harder. Probably too thin even for Identity. |

**What the sidebar produces:** the `IdentityAccount` design (or the decision to go aggregate-less), the event catalogue, and the ACL-as-BC modeling-pattern framing for §11 (is this a new CritterCab pattern, or just "a small aggregate-cluster"?).

**ADR-evidence framing the sidebar must state:**

- **ADR-006** — first full exercise; the sidebar's translation-slice shape *is* the ACL stance realized.
- **ADR-013** — `subjectId` is intra-BC (foreign-key-referenced by other BCs), NOT a canonical-ID-chain participant. State explicitly.
- **New §11 candidate (conditional)** — ACL-as-BC / translation-dominant pattern, IF the shape proves distinct enough from aggregate-cluster to warrant delineation.

---

## Forward-constraints inherited (the three accumulated constraints)

The workshop's §X "Forward-constraints handled" documents disposition for each:

| # | Forward-constraint | Source | Pre-lean disposition |
|---|---|---|---|
| 1 | Identity publishes `identity.rider-registered` (account existence) | W002 §13 #2 | **Honor.** Account-existence is genuinely Identity's. Session-keyed by `subjectId` per ADR-014. |
| 2 | Identity publishes `identity.rider-profile-updated` (profile data) | W002 §13 #2 | **Refine / override (grill candidate).** Profile data is Rider Profile BC's concern per the vision doc. Lean: this publication belongs to Rider Profile, not Identity; W005 reassigns it as a forward-constraint on Rider Profile's eventual workshop. Trips' `RiderProfileSnapshot` should subscribe to a Rider Profile event for display-name. |
| 3 | Identity publishes `identity.driver-registered` (`subjectId` + `registeredAt`; at-least-once; `DriverRegistered` proto) | W003 Story 1 step 4 + W004 §6.1 / §X+1 #1–#3 | **Honor.** Onboarding's intake (W004 Slice 6.1) consumes this. Payload + delivery semantics already specified by W004; W005 commits the producer side. |

---

## ADR triggers that fire at this workshop

- **ADR-006 (Identity-as-swappable-ACL)** — **first full exercise.** Not a new evidence point in the "pattern across BCs" sense (it's a single-BC decision); rather, the workshop is the canonical realization of the stance. §11 re-states ADR-006 with the concrete translation-slice realization woven in.
- **ADR-014 (ASB topic naming)** — `identity.*` topics. Fourth-BC instance of the convention. Confirms.
- **ADR-013 (shared cross-BC identifier)** — **nuance-confirmation, not new evidence.** Identity's `subjectId` is intra-BC; ADR-013 explicitly scopes it out. §11 confirms the reading.
- **NEW candidate (conditional) — ACL-as-BC / translation-dominant modeling pattern.** Surfaces only if the sidebar concludes Identity's shape is distinct enough from aggregate-cluster to warrant delineation. Sibling to ADR-012 (aggregate-per-invariant) and the W004 Process Manager candidate. Trigger to author: first Identity implementation slice, if the pattern holds.

---

## Cast

- **Identity** — the workshop's primary BC. ACL between the OIDC provider and the domain.
- **OIDC provider** — external actor (Entra External ID in prod, OpenIddict in demo, Keycloak for local dev). Modeled provider-agnostically as one abstract boundary per ADR-006. Microsoft Graph change notifications are the prod transport for lifecycle events; modeled as the abstract inbound, not Graph-specifically.
- **Onboarding** — downstream consumer of `identity.driver-registered` (W004 Slice 6.1). Already-modeled; W005 commits the producer side of the contract W004 consumes.
- **Trips** — downstream consumer of `identity.rider-registered` (W002 §6.12 `RiderProfileSnapshot`). Already-modeled.
- **Rider Profile** — downstream consumer of `identity.rider-registered`; **candidate new publisher of the profile-updated event** if forward-constraint #2 is reassigned (grill outcome). Pending workshop.
- **Driver Profile** — downstream consumer of `identity.driver-registered` (alongside Onboarding). Pending workshop.
- **Operations** — workforce-tenant identity is parked per ADR-006; not modeled. Named only to mark the deferral.

---

## Orientation files (read in order before starting)

1. **[`CLAUDE.md`](../../../CLAUDE.md)** — routing layer. Architectural Non-Negotiables + Identity-as-ACL.
2. **[`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md)** — **the locked ACL stance. Read in full.** This workshop realizes it; it does not relitigate it.
3. **[`docs/vision/README.md`](../../vision/README.md) §Tentative Bounded Contexts → Identity + §Identity tech-stack + §Deferred (Microsoft Graph depth; Operations tenant)** — the thinness characterization + scope deferrals.
4. **[`docs/context-map/README.md`](../../context-map/README.md) edge #3** — Identity ACL + outbound PL family. The supplier side this workshop locks.
5. **[`docs/workshops/002-trips-event-model.md`](../../workshops/002-trips-event-model.md)** — closest template (§3 sidebar, §6.12 non-Klefter Translation-in, §13 forward-constraints-handled, §14.6 refinements). §6.12 is the consumer side of `identity.rider-registered`; §13 #2 is the forward-constraint this workshop honors/refines.
6. **[`docs/workshops/004-onboarding-event-model.md`](../../workshops/004-onboarding-event-model.md)** — §6.1 (the consumer side of `identity.driver-registered` + the exact payload/delivery requirements) + §X+1 #1–#3 (the forward-constraints on this workshop) + §3 (modeling-pattern sidebar shape to mirror).
7. **[`docs/workshops/001-dispatch-event-model.md`](../../workshops/001-dispatch-event-model.md)** — shape conventions + §12.6 methodology.
8. **[`docs/research/agents-in-event-models.md`](../../research/agents-in-event-models.md)** — Klefter translation-decision pattern (Identity is translation-heavy; most slices are Klefter translation-in). Post 3 + W002 §6.12's non-Klefter counter-example are the calibration.
9. **[`docs/research/event-modeling-workshop-guide.md`](../../research/event-modeling-workshop-guide.md)** — Lesson 3 (Translation slices) especially. Identity is the most translation-dominant BC yet.
10. **[`docs/rules/structural-constraints.md`](../../rules/structural-constraints.md)** — Identity Boundary (ADR-006) section is load-bearing.
11. **[`docs/research/methodology-log.md`](../../research/methodology-log.md) entry 005** — the DS-as-upstream finding whose boundary this EM-direct thin-BC workshop tests. The retro's methodology question confirms or refines it.
12. **[`docs/workshops/README.md`](../../workshops/README.md)** — conventions + § Workshop follow-ups (the Identity forward-constraint rows this session closes).
13. **[`docs/prompts/workshops/004-onboarding-event-modeling.md`](./004-onboarding-event-modeling.md)** — most recent workshop prompt for shape comparison.
14. **[`docs/prompts/README.md`](../README.md)** — prompts conventions including `## Spec delta` cadence.

---

## Working pattern

Same interactive cadence as W002 / W004:

- **Lock scope at session start** (Option A — tight, rider + driver registration, provider-agnostic ACL). Confirm before sidebar.
- **Run the modeling-pattern sidebar** before slice 1. Settle thin-aggregate-vs-translation-dominant + state the ADR-006/013 framing. Capture as §3.
- **Settle the three boundary questions** (`rider-profile-updated` reassignment; aggregate shape; rider-becomes-driver correlation) with leans — these are the grill-with-docs candidates if a grill pass runs before the workshop.
- **Propose slice ordering.** Spine: rider-registration translation-in → `identity.rider-registered` publication; driver-registration translation-in → `identity.driver-registered` publication; then any verification/lifecycle slices in scope; then the projection slices. Translation-in slices dominate.
- **Walk slices.** Pause for sign-off after each. Light cadence for the mechanical translation slices; extended for the boundary-question slices.
- **Pair open questions with leaning opinions** per §12.7.
- **Defer protobuf authorship** per W001 §12.6 #4. Name `RiderRegistered` + `DriverRegistered`; do not author.
- **At session close:** §X "Forward-constraints handled" (the three, with dispositions); §X+1 "Forward-constraints generated" (any outbound, esp. the `rider-profile-updated` reassignment to Rider Profile); §11 ADR Candidates (ADR-006 first-exercise, ADR-014 fourth-instance, ADR-013 nuance, conditional ACL-as-BC candidate); §12 retrospective with the entry-005-contrast methodology question; update `docs/workshops/README.md` (W005 row + close the Identity forward-constraint follow-up rows + new follow-ups subsection); optionally amend the context map (lock edge #3's consumer-facing PL edges that Identity's publications enable); optionally append methodology log entry 006.
- **Commit only after explicit sign-off** per phase.

---

## Deliverable plan

| File | Status | Purpose |
|---|---|---|
| `docs/workshops/005-identity-event-model.md` | New | Workshop artifact, structurally parallel to W002. Includes §3 modeling-pattern sidebar, §X "Forward-constraints handled," §X+1 "Forward-constraints generated," §11 ADR Candidates, §12 retrospective with the entry-005-contrast methodology question. |
| `docs/workshops/README.md` | Edit | W005 list row. Close the Identity forward-constraint follow-up rows (W002 §14.8 + W004 §X+1 Identity rows). New "From Workshop 005 Follow-ups" subsection. |
| `docs/retrospectives/workshops/005-identity-event-modeling.md` | New | Retro in CritterCab standard format. Third entry in `retrospectives/workshops/`. Includes the entry-005-contrast methodology subsection. |
| `docs/retrospectives/README.md` | Edit | New index entry. |
| `docs/research/methodology-log.md` | Edit *(conditional)* | Entry 006 *if* the entry-005-contrast yields a cross-cutting observation meeting entry 001's three criteria. Strong candidate either way (confirms or refines entry 005's DS-scoping). |
| `docs/context-map/README.md` | Edit *(conditional)* | Lock edge #3's consumer-facing PL edges that Identity's publications enable (the supplier side is now committed); reassign `rider-profile-updated` to Rider Profile if the grill so resolves. Honest "no further context-map impact beyond the supplier-side lock" is a fine outcome. |

### Definition of done

- Workshop artifact committed, structurally parallel to W002.
- The three accumulated forward-constraints handled with explicit disposition (honor / refine / override) in §X.
- Modeling-pattern sidebar captured as §3 with ADR-006 first-exercise + ADR-013 nuance framing.
- §11 re-lists ADR-006 (first exercise), ADR-014 (fourth instance), ADR-013 (nuance), conditional ACL-as-BC candidate.
- §12 retrospective complete with the entry-005-contrast methodology question answered.
- `docs/workshops/README.md` updated (W005 row + Identity follow-up closures + new follow-ups subsection).
- Methodology log entry 006 written *if* warranted; silence is fine.

---

## What this session deliberately does NOT carry

- **Protobuf authorship** — deferred per W001 §12.6 #4 + ADR-009. Name `RiderRegistered` + `DriverRegistered`; do not author.
- **Rider Profile / Driver Profile / Operations BC modeling** — Identity publishes; they consume. Their own workshops are separate.
- **Microsoft Graph fuller-lifecycle integration** — password reset / MFA / account block. Deferred per vision doc (Option B out of scope).
- **Workforce/Operations identity** — parked per ADR-006 (Option C out of scope).
- **Per-provider modeling** — the provider is abstract (one OIDC-provider boundary). Entra/OpenIddict/Keycloak specifics are config, not events, per ADR-006.
- **Implementation code** — pure design.
- **ADR authorship** — ADR-006 already authored; the conditional ACL-as-BC candidate is named, not authored.
- **Edits to prior workshops** — W001/W002/W003/W004 stay locked. The `rider-profile-updated` refinement is documented as a W005 forward-constraint reassignment, NOT an edit to W002.

---

## Memory inheritance

The applicable feedback memories carry into this session via `MEMORY.md`. No restatement needed: proactive projections; Critter Stack primitives over bespoke; BC-owned enums; Decider Pattern with immutable-Apply preference; communication preferences (depth, UL, leans, DDD-background); explicit deferrals; keep READMEs current alongside session work; validation at HTTP boundary; static endpoints / Alba-first tests; no Claude attribution on commits/PRs.

---

## Methodology question to resolve in the retro

**Did EM-direct on a thin/mechanical BC surface mid-walk vocabulary friction that a DS pre-step would have caught, or did the thin BC genuinely not need DS?**

This is the contrast test for methodology log entry 005. Entry 005 found DS-as-upstream materially raises EM quality *for vocabulary-rich BCs* and scoped DS out for "narrow, mechanical BCs." Identity is the boundary case. Evidence shape: count any vocabulary scrambles or boundary surprises that surfaced mid-walk (the `rider-profile-updated` boundary is the prime candidate — would a DS have caught it earlier?); compare the friction profile against W004 (DS-fed) and W001/W002 (EM-direct, but those weren't framed against entry 005).

If EM-direct ran clean (no friction DS would have caught), that **confirms** entry 005's scoping (thin BCs don't need DS). If a meaningful boundary surprise surfaced mid-walk that a DS would have front-loaded, that **refines** entry 005 (the DS-vs-thin-BC boundary is fuzzier than "vocabulary-rich vs. mechanical"). Either way, methodology log entry 006 is a strong candidate — it closes the confirm-or-disconfirm loop entry 005 opened.

---

## Starting move

When the new session begins:

1. **Read the orientation files in order** (§Orientation files above). ADR-006 is load-bearing — read in full; it is the stance this workshop realizes, not relitigates.
2. **Confirm scope.** Locked to Option A (tight — rider + driver registration, provider-agnostic ACL). Validate with user; surface any movement as the first decision.
3. **Settle the three boundary questions** (`rider-profile-updated` reassignment; aggregate shape; rider-becomes-driver correlation) — pre-leans above. If a grill-with-docs pass runs before the workshop, these are its primary targets.
4. **Run the modeling-pattern sidebar.** Settle thin-aggregate-vs-translation-dominant; state ADR-006 first-exercise + ADR-013 nuance framing. Capture as §3.
5. **Propose slice ordering.** Translation-in-dominant spine (rider-registration → publish; driver-registration → publish), then projections.
6. **Walk slices.** Pause for sign-off after each. Light cadence for mechanical translation slices; extended for boundary-question slices.
7. **At session close:** §X "Forward-constraints handled"; §X+1 "Forward-constraints generated"; §11 ADR Candidates; §12 retrospective with the entry-005-contrast question; README updates; conditional methodology log entry 006; conditional context-map edge-#3 supplier-side lock.

Don't batch the whole workshop into one output. Workshop sessions are interactive, slice by slice — W001/W002/W004 cadence is the precedent.
