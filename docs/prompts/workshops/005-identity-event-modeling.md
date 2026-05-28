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
- **New §11 ADR candidate — ACL-as-BC / translation-dominant modeling pattern (firm per grill #2).** Identity is almost-all-Translation-slices with a thin backing stream that exists for record-keeping, not invariant protection — CritterCab's third modeling shape, delineating when a BC is "just an ACL" (no load-bearing invariant; streams as audit/projection substrate) vs. when it warrants aggregate-per-invariant (W001/W002) or Process Manager via Handlers (W004). Surface as a candidate; do not author.
- **No existing spec is amended retroactively.** W001/W002/W003/W004 stay locked; W005 honors or refines via documented reasoning in §X, not via edits to prior artifacts. Same-PR closure of the W002 §14.8 + W004 §X+1 Identity-forward-constraint follow-up rows (index entries) is in-bounds per the same-file-edit rule.

---

## Grill-with-docs resolution history

Resolutions applied during the `grill-with-docs` pass (2026-05-27). The session-runner inherits these as locked decisions, not open questions. References to "grill #N" elsewhere in the prompt point back to this table.

| Grill | What was challenged | Resolution | Where it lands |
|---|---|---|---|
| **#1** | W002 §13 #2 wrote the forward-constraint as "Identity must publish `RiderRegistered` **and** `RiderProfileUpdated`" — but the vision doc assigns profile *data* (saved addresses, payment methods, notification preferences) to the Rider Profile BC, and ADR-006 frames Identity as a thin translation boundary, not a user-management/profile service. The W002 constraint conflated account-existence with profile-data at a time when neither Identity nor Rider Profile had been modeled. | **Refine/override.** Identity publishes `identity.rider-registered` (account-existence — a provider-translated lifecycle fact, carrying the *initial* display name from the OIDC profile claim at sign-up) and does **NOT** publish `identity.rider-profile-updated`. The mutable profile (including display-name *edits*) is Rider Profile's domain; Rider Profile publishes its own profile-updated event. Trips' `RiderProfileSnapshot` (W002 §6.12) subscribes to *both* `identity.rider-registered` (initial value) and the Rider Profile update event (edits). The `rider-profile-updated` forward-constraint is **reassigned to Rider Profile's eventual workshop**. Boundary: Identity owns registration-time facts; Rider Profile owns the mutable profile. | §Scope boundary-question #1 (locked); §Forward-constraints inherited #2 (refine/override); §X+1 generates the Rider Profile reassignment constraint |
| **#2** | The pre-grill lean ("thin `IdentityAccount` aggregate") hand-waved the question ADR-012 forces: *what invariant does the aggregate exist to protect?* Stress-testing candidate invariants found none load-bearing in scope — account-uniqueness-per-`subjectId` is a structural consequence of stream-keying (per W002 §3's own reasoning, not separately load-bearing); email-uniqueness is the *provider's* concern (ADR-006); account-merging/one-account-per-human is out of scope (sign-up only); registration-fact monotonicity is trivially satisfied. | **Translation-dominant, NOT aggregate-per-invariant.** Identity has no load-bearing invariant *in scope* ("not currently" — account-linking/merging would change this when eventually modeled). A thin `IdentityAccount` stream per `subjectId` exists for **record-keeping** (Klefter: record the translation consequence locally) and projection-backing, NOT invariant protection. This makes Identity CritterCab's **third modeling shape**: ACL-translation-dominant — mostly Translation slices, streams as audit/projection substrate rather than consistency boundaries. The §11 ACL-as-BC ADR candidate is **real, not conditional**. This is the *opposite-direction* call from W004 (Onboarding too coordination-heavy for ADR-012; Identity too thin for it) — both reinforce that ADR-012 is one pattern among several. | §Modeling-pattern sidebar (framing locked); §Spec delta (candidate de-conditionalized); §ADR triggers (candidate firm) |
| **#3** | Research note §7's rider-becomes-driver edge: does an existing rider who becomes a driver get a second account, or carry over? | **One `IdentityAccount` per human (`subjectId`).** Rider and driver registration facts accumulate on one stream; the actor is never two accounts. `RiderRegistered` and `DriverRegistered` land on the same `subjectId` stream. Follows directly from grill #2's one-stream-per-`subjectId` shape. | §Scope boundary-question #3 (locked) |
| **#4** | Stress-testing grill #3 against W004 §6.1 exposed a gap: W004's Onboarding intake is triggered by `identity.driver-registered`, but if that event means strictly "new account created," it never fires for a rider-becomes-driver (account already exists) — so rider-becomes-driver couldn't onboard. | **Role-registration-fact (option A).** `identity.driver-registered` publishes whenever `DriverRegistered` appends — net-new *or* rider-becomes-driver. **Preserves W004 §6.1's single intake trigger with no W004 edit** (idempotent on `subjectId`). Rejected B (strict account-creation → forward-constrains W004 to add a second intake path; cleaner boundary, bigger blast radius) and C (park it → leaves a known gap). Boundary holds: Identity owns account-and-role *existence* (ADR-006 puts "roles" in its purview); Onboarding owns the lifecycle. Semantic clarification for W004's implementation: the event means "this `subjectId` is registered as a driver," not strictly "new account created." | §Scope boundary-question #4 (locked); semantic clarification inherited by W004's implementation (no W004 artifact edit) |
| **#5** | The grill surfaced three real nuances (#1, #3, #4) — does that retroactively argue Identity was vocabulary-rich enough to have warranted a DS pre-step, weakening the EM-direct call? | **No — confirms EM-direct, and adds a distinction.** The three nuances were all **ownership-boundary** questions (which BC owns profile data; one-human-one-account; which BC's event triggers intake) — *not* **vocabulary** questions (a term meaning different things to different actors). DS surfaces vocabulary divergence (cf. W003-retro-Q2's "review/under-review/REVIEWS"); **grill-with-docs** surfaces strategic-design boundary calls. Identity was *boundary-nuanced but not vocabulary-rich*, so EM-direct was correct and the grill (not DS) was the right tool. **Refinement for the W005 retro / methodology log entry 006: the DS-vs-EM-direct axis is about vocabulary richness; boundary nuance is a separate axis handled by grill-with-docs regardless of whether DS ran.** | §Methodology question (retro framing sharpened); strengthens the entry-006 candidacy |
| **#6** | What *is* `subjectId` — Identity-minted domain ID, or the provider's raw OIDC `sub` claim? If it's the raw `sub`, then `identity.*` events (and W003/W004 carrying `subjectId` downstream) leak a provider object ID — an ADR-006 violation propagated across four workshops. The name `subjectId` invites the conflation. | **Identity-minted domain ID (UUIDv7), NOT the raw `sub`.** The provider-`sub` → `subjectId` mapping lives *inside* Identity (exactly where ADR-006 puts it); downstream BCs only ever see the domain `subjectId`. W003/W004's existing usage was carrying a domain ID all along; W005 makes that explicit. Name `subjectId` stays (established across three workshops; renaming ripples for no gain) — the workshop documents the distinction so it isn't read as the raw `sub`. ADR-013 nuance holds: `subjectId` is intra-BC, FK-referenced by other BCs, NOT a canonical-chain participant — distinct from the `applicationId == driverProfileId` lifecycle key W004 mints (the driver-onboarding lifecycle deliberately carries *two* IDs: the ADR-013 chain key + the Identity actor handle). | §Modeling-pattern sidebar ADR-evidence framing (ADR-006 provenance + ADR-013 nuance) |
| **#7** | Scope confirm: does "sign-up only" include the email/magic-link verification sub-flow, or only the terminal registration event? | **Full account-creation arc including verification (Option A confirmed).** "Sign-up" includes the email/magic-link verification sub-flow per W003 Story 1 steps 1–4 (which Identity owns the translation side of) — likely an `EmailVerified`/`VerificationCompleted` translated event, exactly the Klefter translated-decision shape this workshop exists to demonstrate. Terminal-registration-only was rejected as skipping a real provider-lifecycle translation W003 already depicted. Microsoft Graph fuller-lifecycle (Option B) and workforce/Operations tenant (Option C) stay out per vision-doc deferrals. | §Scope Option A (verification sub-flow clarified) |

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
| **A. Tight (locked)** | Rider registration sign-up flow → `identity.rider-registered`; driver registration sign-up flow → `identity.driver-registered`. **"Sign-up" = the full account-creation arc including the email/magic-link verification sub-flow (grill #7)** — likely an `EmailVerified`/`VerificationCompleted` translated event (Klefter), per W003 Story 1 steps 1–4 which Identity owns the translation side of. Provider-ACL translation-in modeled provider-agnostically (Entra / OpenIddict / Keycloak as one abstract OIDC-provider boundary). Honors the three forward-constraints. | **Locked.** Mirrors the vision-doc's "sign-up events only" scoping and ADR-006's swappable-provider stance. Likely 6–9 slices — thinner than W004. |
| **B. Tight + fuller user lifecycle** | Adds Microsoft Graph fuller-lifecycle events: password reset, MFA enrollment, account block/unblock, email re-verification. | Deferred. The vision doc explicitly defers Microsoft Graph integration depth ("current plan covers sign-up events only ... deferred until the Identity BC is actively being worked"). Out of scope unless the user pulls it in. |
| **C. Tight + workforce/Operations identity** | Adds the workforce Entra tenant for Operations users (ADR-006's parked decision). | Out of scope. Parked per ADR-006 and structural-constraints; trigger is "Operations BC actively being built." |

**Three boundary questions to settle during the walk (grill-with-docs candidates — pre-leans below):**

1. **Is `identity.rider-profile-updated` honestly an Identity event? — LOCKED (grill #1): no.** Identity publishes `identity.rider-registered` (account existence, carrying the *initial* display name from the OIDC profile claim at sign-up) but NOT `identity.rider-profile-updated`. The mutable profile (including display-name edits) is Rider Profile's domain; Rider Profile publishes its own profile-updated event. Trips' `RiderProfileSnapshot` (W002 §6.12) subscribes to *both* `identity.rider-registered` (initial value) and the Rider Profile update event (edits). The `rider-profile-updated` forward-constraint is **reassigned to Rider Profile's eventual workshop** (a documented override of W002 §13 #2, not a simple honor). Boundary: Identity owns registration-time facts; Rider Profile owns the mutable profile.
2. **Does Identity carry a thin per-actor aggregate, or is it translation-dominant with no real aggregate?** **Lean: a thin `IdentityAccount` aggregate per registered actor** (events: `RiderRegistered`, `DriverRegistered`, maybe `EmailVerified`) so account-existence has a queryable local record and the published domain events have a stream to append to — but the workshop should weigh "almost-stateless translation" honestly. See §Modeling-pattern sidebar.
3. **Rider-becomes-driver correlation — LOCKED (grill #3): one account per human.** One `IdentityAccount` stream per human (`subjectId`); rider and driver registration facts accumulate on that one stream. Maya-the-rider who later becomes a driver is one account with two registration facts, not two accounts. `RiderRegistered` and `DriverRegistered` land on the same `subjectId` stream.
4. **`identity.driver-registered` semantics + W004 intake interaction — LOCKED (grill #4): role-registration-fact.** `identity.driver-registered` publishes to ASB whenever a `DriverRegistered` event is appended to the `IdentityAccount` stream — net-new account *or* rider-becomes-driver. For a net-new driver, account-creation and driver-role-registration coincide; for rider-becomes-driver, only `DriverRegistered` appends (no new account) and the event still publishes. **This preserves W004 §6.1's single intake trigger with no W004 edit** (W004 consumes `identity.driver-registered`, idempotent on `subjectId`, either way). Boundary holds: Identity owns account-and-role *existence* facts (ADR-006 already puts "roles" in Identity's purview); Onboarding owns the vetting *lifecycle*. Closes the rider-becomes-driver onboarding gap rather than parking it. Semantic clarification for W004's eventual implementation: `identity.driver-registered` is "this `subjectId` is registered as a driver," not strictly "a new account was created."

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

**The question — RESOLVED (grill #2):** Does Identity have a load-bearing invariant (→ aggregate-per-invariant, ADR-012) or only structural record-keeping (→ translation-dominant)? **Answer: translation-dominant.** No load-bearing invariant in scope (account-uniqueness is a structural consequence of stream-keying; email-uniqueness is the provider's; account-merging is out of scope; registration-fact monotonicity is trivial).

**Locked shape: translation-dominant with a thin `IdentityAccount` backing stream.**

| Aspect | Decision |
|---|---|
| **Stream** | One `IdentityAccount` stream per `subjectId`; events `RiderRegistered`, `DriverRegistered`, optionally `EmailVerified`. **The stream exists for record-keeping (Klefter: record the translation consequence locally) and projection-backing — NOT invariant protection.** |
| **Publications** | Fire from the same handler that appends the local event (atomic local-event + outbound ASB publication, Wolverine outbox — same discipline as W001 §5.5 / W004 §6.8, but here protecting record-keeping atomicity, not an invariant). |
| **Why not aggregate-per-invariant** | No load-bearing invariant in scope ("not currently" — account-linking/merging would change this). The "at most one account per `subjectId`" uniqueness is structural-consequence stream-keying, not a protected invariant. |
| **Why not aggregate-less** | A truly stateless ACL would lose the local audit record + queryable account-existence substrate + clean rider-becomes-driver correlation. The thin backing stream earns its keep for record-keeping even without an invariant. |

**What the sidebar produces:** the `IdentityAccount` translation-dominant design, the event catalogue, and the **ACL-as-BC modeling-pattern framing for §11 — a real (not conditional) new CritterCab pattern**, CritterCab's third modeling shape alongside aggregate-per-invariant (W001/W002) and Process Manager via Handlers (W004).

**ADR-evidence framing the sidebar must state:**

- **ADR-006** — first full exercise; the sidebar's translation-slice shape *is* the ACL stance realized. **`subjectId` provenance (grill #6): Identity-minted domain ID (UUIDv7), NOT the provider's raw OIDC `sub` claim.** The provider-`sub` → `subjectId` mapping lives inside Identity (per ADR-006 "the mapping lives in Identity"); downstream BCs only ever see the domain `subjectId`. The name `subjectId` stays (established across W003/W004) but the workshop documents that it is *not* the raw `sub`.
- **ADR-013 (grill #6 nuance)** — `subjectId` is intra-BC, FK-referenced by other BCs, NOT a canonical-ID-chain participant. Distinct from the `applicationId == driverProfileId` lifecycle key W004 mints — the driver-onboarding lifecycle deliberately carries *two* IDs: the ADR-013 chain key (`applicationId`) and the Identity actor handle (`subjectId`). State explicitly.
- **New §11 candidate (firm per grill #2)** — ACL-as-BC / translation-dominant pattern. CritterCab's third modeling shape; not conditional.

---

## Forward-constraints inherited (the three accumulated constraints)

The workshop's §X "Forward-constraints handled" documents disposition for each:

| # | Forward-constraint | Source | Pre-lean disposition |
|---|---|---|---|
| 1 | Identity publishes `identity.rider-registered` (account existence) | W002 §13 #2 | **Honor.** Account-existence is genuinely Identity's. Session-keyed by `subjectId` per ADR-014. |
| 2 | Identity publishes `identity.rider-profile-updated` (profile data) | W002 §13 #2 | **Refined / overridden (grill #1).** Profile data is Rider Profile BC's concern per the vision doc. This publication is reassigned to Rider Profile's eventual workshop (documented override of W002 §13 #2). `identity.rider-registered` carries the *initial* display name; Rider Profile owns *edits*. Trips' `RiderProfileSnapshot` subscribes to both. |
| 3 | Identity publishes `identity.driver-registered` (`subjectId` + `registeredAt`; at-least-once; `DriverRegistered` proto) | W003 Story 1 step 4 + W004 §6.1 / §X+1 #1–#3 | **Honor.** Onboarding's intake (W004 Slice 6.1) consumes this. Payload + delivery semantics already specified by W004; W005 commits the producer side. |

---

## ADR triggers that fire at this workshop

- **ADR-006 (Identity-as-swappable-ACL)** — **first full exercise.** Not a new evidence point in the "pattern across BCs" sense (it's a single-BC decision); rather, the workshop is the canonical realization of the stance. §11 re-states ADR-006 with the concrete translation-slice realization woven in.
- **ADR-014 (ASB topic naming)** — `identity.*` topics. Fourth-BC instance of the convention. Confirms.
- **ADR-013 (shared cross-BC identifier)** — **nuance-confirmation, not new evidence.** Identity's `subjectId` is intra-BC; ADR-013 explicitly scopes it out. §11 confirms the reading.
- **NEW candidate (firm per grill #2) — ACL-as-BC / translation-dominant modeling pattern.** Identity's shape is distinct from both aggregate-cluster (no load-bearing invariant; streams are record-keeping/projection substrate, not consistency boundaries) and Process Manager (no long-running coordination). Sibling to ADR-012 (aggregate-per-invariant) and the W004 Process Manager candidate — CritterCab's third modeling shape. Trigger to author: first Identity implementation slice. Note the "not currently" caveat: if account-linking/merging is later modeled, a load-bearing invariant may emerge and reopen the aggregate-vs-translation-dominant call.

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

**Grill #5 already pre-sharpened the framing** (see §Grill-with-docs resolution history): the W005 grill-with-docs pass surfaced three *ownership-boundary* nuances (rider-profile-updated ownership; one-human-one-account; which BC's event triggers Onboarding intake) but *zero vocabulary-divergence* findings. That points the retro at a two-axis distinction rather than a single DS-yes/no call: **DS surfaces vocabulary divergence; grill-with-docs surfaces strategic-design boundary calls.** Identity was boundary-nuanced but not vocabulary-rich — so EM-direct was correct *and* the boundary nuances were caught by the grill regardless. The retro should confirm whether the *workshop walk itself* surfaces any vocabulary friction beyond what the grill already handled (lean: it won't — the grill + ADR-006's locked stance leave little vocabulary open), and entry 006 should record the two-axis distinction as the durable cross-cutting finding.

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
