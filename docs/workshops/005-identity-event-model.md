# Workshop 005 — Identity Event Model

**Status:** In progress (v0.1, 2026-05-28). Scope confirmed Option A (rider + driver registration sign-up incl. verification sub-flow; provider-agnostic ACL). §3 modeling pattern locked: **ACL-translation-dominant** — CritterCab's third modeling shape. §4 Ubiquitous Language + §5 Event List scaffolded. Slice walk pending. All design leans pre-resolved by the grill-with-docs pass (grills #1–#7; see triggering prompt).
**Started:** 2026-05-28.
**Facilitator / modeler:** Erik Shafer (solo).
**AI collaborator:** Claude (Opus 4.7), rotating through Facilitator, Developer, Skeptic, and Domain-Expert personas per `docs/research/event-modeling-workshop-guide.md` Lesson 8.
**Triggering prompt:** [`docs/prompts/workshops/005-identity-event-modeling.md`](../prompts/workshops/005-identity-event-modeling.md) (grill-with-docs refined; resolution history in the prompt).
**Methodology reference:** `docs/research/event-modeling-workshop-guide.md`.
**Adjunct patterns:** `docs/research/agents-in-event-models.md` (Klefter translation-decision events — Identity is the most translation-dominant BC yet).
**Structural constraints honored:** `docs/rules/structural-constraints.md` (ADR-002, ADR-005, **ADR-006 — this workshop is its first full exercise**, ADR-009).
**Workshop 001/002/004 inheritance:** All conventions carry forward without re-litigation (W001 §12.6 all-five; W002 §14.6 all-five). EM-direct — no DS pre-step (Identity is thin/mechanical, not vocabulary-rich; consistent with methodology log entry 005's DS-scoping).
**Modeling shape (NEW for W005):** ACL-translation-dominant — CritterCab's third modeling shape, alongside aggregate-per-invariant (W001/W002) and Process Manager via Handlers (W004).

---

## 1. Session Log

| Session | Date | Duration | Phases covered | Notes |
|---|---|---|---|---|
| 1 | 2026-05-28 | Pending completion | Scope confirmation (Option A, grill-locked); §3 ACL-translation-dominant sidebar; §4 UL; §5 Event List scaffold; slice ordering; slice walk | Fourth EM workshop; first BC whose primary job is an anti-corruption layer. First workshop pulled into existence by accumulated forward-constraints from three prior workshops (W002, W003, W004). EM-direct. Seven grill-with-docs resolutions pre-locked the design before the session ran (rider-profile-updated reassignment; translation-dominant shape; one-account-per-human; role-registration-fact trigger semantics; EM-direct-vs-boundary-nuance two-axis distinction; subjectId Identity-minted provenance; scope incl. verification). |

---

## 2. Scope Statement

### 2.1 In scope

The Identity bounded context's **account-registration lifecycle** for both riders and drivers, modeled as an anti-corruption layer over a swappable OIDC provider per ADR-006:

- **Rider registration** — provider lifecycle translation-in → `RiderRegistered` → `identity.rider-registered` publication.
- **Driver registration** — provider lifecycle translation-in → `DriverRegistered` → `identity.driver-registered` publication. Role-registration-fact semantics (grill #4): fires on every `DriverRegistered` append, net-new or rider-becomes-driver.
- **Email/magic-link verification sub-flow** (grill #7) — `EmailVerified` as a Klefter translation of the provider's verification lifecycle signal. "Sign-up" = the full account-creation arc.
- **Rider-becomes-driver** (grill #3) — one `IdentityAccount` per human (`subjectId`); `DriverRegistered` appends to the existing account stream.
- **`subjectId` minting + provider-`sub` mapping** (grill #6) — Identity mints the domain `subjectId` (UUIDv7) and maps the provider's raw `sub` to it internally.
- **Identity projections** — `RegisteredActorsBySubject`, `IdentityAuditTimeline`.

### 2.2 At the boundary

- **OIDC provider → Identity (translation-in).** The provider (Entra External ID prod / OpenIddict demo / Keycloak local) handles authentication — magic-link issuance, token minting, the verification click. Identity translates the provider's *lifecycle events* (Microsoft Graph change notifications in prod) into domain events. Modeled provider-agnostically (one abstract OIDC-provider boundary) per ADR-006.
- **Identity → Onboarding (publication).** `identity.driver-registered` (consumed by W004 Slice 6.1). Producer side committed here.
- **Identity → Trips (publication).** `identity.rider-registered` (consumed by W002 §6.12 `RiderProfileSnapshot`). Producer side committed here.
- **Identity → Rider Profile / Driver Profile (publication).** Both consume `identity.*-registered`. Pending their workshops.

### 2.3 Out of scope

- **`identity.rider-profile-updated`** — reassigned to Rider Profile BC (grill #1; documented override of W002 §13 #2). Identity owns registration-time facts; Rider Profile owns the mutable profile.
- **Magic-link / token / auth mechanics** — the provider's, per ADR-006.
- **Microsoft Graph fuller-lifecycle** (password reset, MFA, account block) — vision-doc deferral (Option B).
- **Workforce/Operations Entra tenant** — parked per ADR-006 (Option C).
- **Account-linking / merging** — out of scope; the "not currently" caveat on the no-invariant finding (grill #2).
- **Implementation code; ADR authorship; per-provider modeling** — pure design.

### 2.4 Structural constraints honored

- Only Identity integrates with the provider (ADR-006). Provider-specific details (raw `sub`, claim shapes) do not appear in published domain events — `subjectId` is Identity-minted-domain (grill #6).
- Cross-BC business events go via ASB per `<source-bc>.<event-name-kebab>` (ADR-005, ADR-014).
- New protobuf contracts named in §10, not authored inline (ADR-009).

### 2.5 Decisions locked during scope-setting (all grill-resolved)

| Decision | Resolution | Grill |
|---|---|---|
| Scope | Option A — rider + driver registration incl. verification sub-flow; provider-agnostic | #7 |
| `rider-profile-updated` | Reassigned to Rider Profile (override of W002 §13 #2) | #1 |
| Modeling pattern | ACL-translation-dominant; third CritterCab modeling shape | #2 |
| Account correlation | One `IdentityAccount` per human (`subjectId`) | #3 |
| `identity.driver-registered` semantics | Role-registration-fact; fires on every `DriverRegistered` append; preserves W004 intake | #4 |
| `subjectId` provenance | Identity-minted domain UUIDv7; provider-`sub` mapping inside Identity; intra-BC FK (not ADR-013 chain) | #6 |

---

## 3. Modeling pattern and process state

### 3.1 Why this section exists

Per W001 §12.6 #2 / W002 §3 / W004 §3, the modeling-shape decision shapes every slice. Identity's sidebar settles a shape neither aggregate-cluster (W001/W002) nor Process Manager (W004) covers: **a BC whose primary job is translation, with no domain state of its own worth protecting.** Grill #2 resolved it; this section commits the resolution.

### 3.2 Decision — ACL-translation-dominant (grill #2)

**Identity is modeled as a translation-dominant ACL, NOT aggregate-per-invariant.** Stress-testing for a load-bearing invariant (the sidebar's job under ADR-012) found none *in scope*: account-uniqueness-per-`subjectId` is a structural consequence of stream-keying (W002 §3's own reasoning); email-uniqueness is the provider's (ADR-006); account-merging is out of scope; registration-fact monotonicity is trivial. The "not currently" caveat stands — account-linking/merging, if later modeled, could surface a real invariant and reopen this call.

This makes Identity **CritterCab's third modeling shape**, alongside aggregate-per-invariant (W001/W002) and Process Manager via Handlers (W004). The §11 ACL-as-BC ADR candidate is firm. Symmetry worth noting: W004 argued Onboarding was *too coordination-heavy* for ADR-012; Identity is *too thin* for it. Both reinforce that ADR-012 is one pattern among several.

### 3.3 `IdentityAccount` — the thin backing stream

```csharp
public class IdentityAccount
{
    public Guid Id { get; set; }            // subjectId — Identity-MINTED domain UUIDv7 (grill #6), NOT the provider's raw OIDC sub
    public bool IsRider { get; set; }        // RiderRegistered seen
    public bool IsDriver { get; set; }       // DriverRegistered seen
    public bool EmailVerified { get; set; }
    public string? InitialDisplayName { get; set; }  // from the provider profile claim at sign-up; edits are Rider Profile's (grill #1)
    public DateTimeOffset RegisteredAt { get; set; }

    public void Apply(EmailVerified e) => EmailVerified = true;
    public void Apply(RiderRegistered e) { IsRider = true; InitialDisplayName = e.DisplayName; RegisteredAt = e.OccurredAt; }
    public void Apply(DriverRegistered e) { IsDriver = true; RegisteredAt = e.OccurredAt; }   // appends to existing stream for rider-becomes-driver (grill #3/#4)
}
```

The stream exists for **record-keeping** (Klefter: record the translation consequence locally) and projection-backing — **not invariant protection**. One stream per human (`subjectId`), carrying both rider and driver registration facts (grill #3). No concurrency boundary is defended; Marten stream-keying gives uniqueness for free, but that's structural, not load-bearing. Per W001 §12.5's Decider-pattern preference, mutable `Apply` is the Marten norm for plain record-keeping state types (as in W004's `ApplicationState`).

### 3.4 The inbound — provider lifecycle events, not magic-link mechanics (ADR-006)

Per ADR-006, **the OIDC provider handles authentication — magic-link issuance, token minting, the verification click.** Identity does *not* send verification emails or validate clicks; it **translates the provider's lifecycle events** (Microsoft Graph change notifications in prod) into domain events.

This refines W003 Story 1 steps 1–4, which depicted "Identity SENDS verification email / CREATES account" — a DS-level simplification with Identity as an Onboarding-upstream black box. W005 corrects it per ADR-006: Identity's inbound is the *provider's* "user registered / email verified" lifecycle signal; the magic-link mechanics are the provider's and are not modeled here. **Every Identity slice is therefore a Klefter translation-in:** provider lifecycle event → translated domain event → optional ASB publication.

### 3.5 `subjectId` provenance (grill #6)

`subjectId` is an **Identity-minted domain UUIDv7**, not the provider's raw OIDC `sub`. The provider-`sub` → `subjectId` mapping lives *inside* Identity (ADR-006: "the mapping lives in Identity"); downstream BCs only ever see the domain `subjectId`. The name `subjectId` is kept (established across W003/W004); this workshop documents the distinction so it isn't read as the raw `sub`. Per ADR-013 it is intra-BC, FK-referenced — *not* a canonical-chain participant (distinct from W004's `applicationId == driverProfileId` lifecycle key; the driver-onboarding lifecycle deliberately carries two IDs).

### 3.6 What's split off

| Concern | Where | Why |
|---|---|---|
| Magic-link / token / auth mechanics | OIDC provider | ADR-006 — provider does auth; Identity translates. |
| Mutable rider profile (addresses, payment, prefs, display-name edits) | Rider Profile BC | Grill #1 — Identity owns registration-time facts. |
| Driver vetting lifecycle | Onboarding BC | W003/W004 — Identity publishes `driver-registered`; Onboarding owns the lifecycle. |
| Workforce/Operations identity | Parked (ADR-006) | Out of scope. |
| Microsoft Graph fuller-lifecycle | Deferred | Vision-doc deferral; Option B out of scope. |
| Account-linking / merging | Deferred | "Not currently" caveat on the no-invariant finding. |

### 3.7 ADR-evidence framing

- **ADR-006** — first full exercise; the translation-slice shape *is* the ACL stance realized. `subjectId` Identity-minted (grill #6).
- **ADR-014** — `identity.*` topics; fourth-BC instance of the convention.
- **ADR-013** — `subjectId` is intra-BC, not a canonical-chain participant (grill #6 nuance).
- **NEW §11 candidate (firm per grill #2)** — ACL-as-BC / translation-dominant; CritterCab's third modeling shape.

### 3.8 Decisions locked in this section

| Decision | Resolution |
|---|---|
| Modeling pattern | ACL-translation-dominant; no load-bearing invariant in scope ("not currently"). Third CritterCab modeling shape. |
| `IdentityAccount` | Thin stream per `subjectId` for record-keeping/projection, not invariant protection. Both rider+driver facts on one stream. |
| `subjectId` | Identity-minted domain UUIDv7; provider-`sub` mapping inside Identity. Intra-BC FK handle. |
| Inbound | Provider lifecycle events (ADR-006); magic-link mechanics are the provider's, not modeled. Every slice is Klefter translation-in. |
| `identity.driver-registered` | Role-registration-fact; fires on every `DriverRegistered` append (grill #4). |
| Initial display name | On `RiderRegistered` (from provider claim); edits are Rider Profile's (grill #1). |

---

## 4. Ubiquitous Language

| Term | Definition | Notes |
|---|---|---|
| **Identity Account** | The thin per-actor record-keeping stream, keyed on `subjectId`. Carries registration facts (rider/driver) + verification. | Not a consistency boundary — record-keeping/projection substrate (grill #2). |
| **Subject ID** | Identity-minted domain UUIDv7 actor handle. The provider-`sub` maps to it inside Identity. | Grill #6. Intra-BC FK referenced by other BCs; NOT the raw OIDC `sub`; NOT an ADR-013 canonical-chain participant. |
| **OIDC Provider** | The external authentication provider (Entra External ID / OpenIddict / Keycloak). Handles magic-link, tokens, verification. | ADR-006. Modeled provider-agnostically as one abstract boundary. |
| **Registration fact** | A `RiderRegistered` or `DriverRegistered` event — "this `subjectId` is registered in this role." | Grill #4 — role-registration-fact, not strictly account-creation. Both can land on one stream (grill #3). |
| **Translation-in (Klefter)** | A slice converting a provider lifecycle event into a CritterCab domain event on the `IdentityAccount` stream. | Every Identity slice is this shape (§3.4). |
| **Initial display name** | The display name captured from the provider profile claim at registration, carried on `RiderRegistered`. | Grill #1 — edits are Rider Profile's, not Identity's. |

---

## 5. Event List (chronological — populated per slice)

| # | Event | Slice | Key payload | Lane |
|---|---|---|---|---|
| 1 | `EmailVerified` | 6.1 | `subjectId` (Identity-minted UUIDv7), `verifiedAt` | Identity (account-level, role-agnostic; Klefter translation of provider verification signal; first event on the `IdentityAccount` stream) |
| 2 | `RiderRegistered` | 6.1 | `subjectId`, `displayName?` (initial, from provider profile claim — edits are Rider Profile's), `registeredAt` | Identity (role-registration fact; `providerSub` NOT on event per ADR-006; publishes `identity.rider-registered`) |
| 3 | `DriverRegistered` | 6.2 / 6.3 | `subjectId`, `registeredAt` | Identity (role-registration fact; net-new at 6.2 / appends to existing account at 6.3; publishes `identity.driver-registered` with `subjectId`+`registeredAt` per W004 §6.1's contract) |

Scaffold by category (§3.6):

| Category | Events | First slice |
|---|---|---|
| Verification (translated) | `EmailVerified` | 6.1 |
| Registration (translated) | `RiderRegistered`, `DriverRegistered` | 6.1 / 6.2 |
| Outbound publications | `identity.rider-registered`, `identity.driver-registered` | 6.1 / 6.2 |

---

## 6. Slice Walk

Every slice is a Klefter translation-in (provider lifecycle event → domain event on the `IdentityAccount` stream → optional ASB publication). Thin, translation-dominant spine.

### Proposed slice ordering

| Slice | Pattern | Trigger | Events / publications |
|---|---|---|---|
| **6.1 Rider sign-up** | Translation-in (Klefter) + stream-creating + publication | Provider verified-registration lifecycle signal (rider flow) | Mints `subjectId` + maps provider-`sub`; `EmailVerified` + `RiderRegistered` (carries initial display name); publishes `identity.rider-registered`. Heavy-ish — establishes the `subjectId`-minting + provider-`sub`-mapping + translation-in shape the workshop reuses. |
| **6.2 Driver sign-up** (paired with 6.1) | Translation-in + stream-creating + publication | Provider verified-registration lifecycle signal (driver flow) | `EmailVerified` + `DriverRegistered`; publishes `identity.driver-registered`. Role-registration-fact semantics (grill #4). |
| **6.3 Rider-becomes-driver** | Translation-in (append to existing stream) + publication | Existing rider (account exists) enters the driver flow | `DriverRegistered` appends to the existing `IdentityAccount` stream (no new `EmailVerified`); publishes `identity.driver-registered`. Demonstrates grills #3 + #4 — closes the W004 intake gap. Light. |
| **6.4 Identity projections** | View | All Identity events | `RegisteredActorsBySubject` + `IdentityAuditTimeline`. Light. |

**Cadence:** 6.1 heavy; 6.2 paired-and-brisk; 6.3 + 6.4 light. **Not walked (out of scope §2.3):** `rider-profile-updated` (reassigned to Rider Profile); Microsoft Graph fuller-lifecycle; workforce tenant; account-linking/merging.

### 6.1 Slice 1 — Rider sign-up (Translation-in + stream-creating + publication)

**Pattern:** Translation-in (Klefter) + stream-creating + outbound publication.
**Lane:** Identity for the handler + local events; provider lifecycle inbound (Microsoft Graph change notification in prod, abstracted); ASB outbound to consumers.
**Trigger:** Provider verified-registration lifecycle signal for a rider-flow sign-up.

#### Flow on the board

```
   ┌─────────────────────────────────────┐
   │ OIDC provider (Entra / OpenIddict /  │  external — handles magic-link,
   │  Keycloak — abstract boundary)       │  token, the verification click
   │  User completes rider sign-up +      │  (NOT modeled — ADR-006)
   │  email verification                  │
   │  → emits lifecycle signal            │
   └──────────────┬──────────────────────┘
                  │ Microsoft Graph change notification (prod) / webhook
                  │ abstracted as: ProviderUserVerified
                  │ { providerSub, email, displayName?, flowType: Rider, verifiedAt }
                  ▼
   ┌─────────────────────────────────────┐
   │ Identity Translation-in handler      │  plain handler (NOT [AggregateHandler])
   │ Klefter: translate provider signal   │  — stream-creating, MartenOps.StartStream
   │ Idempotent on providerSub via        │
   │   ProviderSubjectIndex               │
   │ Mints subjectId (domain UUIDv7);     │
   │ records providerSub → subjectId map  │
   └──────────────┬──────────────────────┘
                  │ MartenOps.StartStream<IdentityAccount>(
                  │   subjectId, EmailVerified, RiderRegistered)
                  │ + OutgoingMessages → identity.rider-registered
                  ▼
   ┌─────────────────────────────────────┐
   │ EmailVerified + RiderRegistered      │  orange events (IdentityAccount stream)
   └──────────────┬──────────────────────┘
                  │
       ┌──────────┴───────────────────────┐
       ▼                                   ▼
   ASB publication                      Views fed:
   identity.rider-registered             • RegisteredActorsBySubject (inline; idempotency + lookup)
   { subjectId, displayName,             • ProviderSubjectIndex (inline; providerSub → subjectId)
     registeredAt }                      • IdentityAuditTimeline (async)
   session-keyed by subjectId
   → Trips (RiderProfileSnapshot),
     Rider Profile (pending)
```

#### Inbound contract (provider lifecycle, abstracted)

| Aspect | Value |
|---|---|
| Source | OIDC provider lifecycle event. Prod: Microsoft Graph change notification → ASB. Abstracted as `ProviderUserVerified`. |
| Treatment | Provider-agnostic per ADR-006. The magic-link/token/click mechanics are the provider's — **not modeled.** Identity translates the *lifecycle signal*. |
| Payload (domain-relevant) | `providerSub` (raw OIDC `sub` — used only for the internal mapping, never published), `email`, `displayName?` (profile claim), `flowType: Rider`, `verifiedAt` |
| Delivery | At-least-once. Idempotent on `providerSub`. |

#### `subjectId` minting + provider-`sub` mapping (grill #6)

The handler mints `subjectId = Guid.CreateVersion7(clock.GetUtcNow())` — an **Identity-domain UUIDv7, never the raw `providerSub`.** The `providerSub → subjectId` correlation is recorded in the **`ProviderSubjectIndex`** projection (inline — load-bearing for idempotency on redelivery and for any future provider-initiated lifecycle event keyed by `providerSub`). The raw `providerSub` lives only in this internal index; it is **never carried on a published `identity.*` event** (ADR-006 — domain events carry domain IDs).

#### Idempotency contract

Handler keyed on `providerSub` via `ProviderSubjectIndex`. If a `subjectId` already exists for this `providerSub`, the handler is a no-op (provider redelivery doesn't spawn a duplicate account). Race-condition fallback: catch `ExistingStreamIdCollisionException` from `MartenOps.StartStream` and silent-ack — same discipline as W004 §6.1. `ProviderSubjectIndex` and `RegisteredActorsBySubject` are **inline** projections (silent dependency — webhook/redelivery routing breaks if not inline; flagged parallel to W004 §3.12 friction-point #7/#9).

#### Events

**`EmailVerified`** — `{ subjectId (UUIDv7, stream key), verifiedAt }`. Account-level fact (role-agnostic). Stream-creating's first event. Klefter translation of the provider's verification lifecycle signal (grill #7).

**`RiderRegistered`** — `{ subjectId, displayName? (initial, from provider profile claim — edits are Rider Profile's per grill #1), registeredAt }`. Role-registration fact. The `providerSub` is **not** on the event (ADR-006).

#### Outbound publication — `identity.rider-registered`

| Aspect | Value |
|---|---|
| Topic | `identity.rider-registered` per ADR-014 (fourth-BC instance) |
| Session key | `subjectId` |
| Payload | `{ subjectId, displayName?, registeredAt }` — domain IDs only; no `providerSub` |
| Consumers | Trips (`RiderProfileSnapshot`, W002 §6.12 — initial display name); Rider Profile (pending) |
| Wire format | Protobuf `RiderRegistered` (deferred authorship, §10) |

#### GWT sketches

**Happy path — net-new rider**
```
Given: no IdentityAccount stream exists for providerSub P
When: ProviderUserVerified { providerSub: P, email, displayName: "Maya O.", flowType: Rider, verifiedAt: T } arrives
Then: subjectId S = CreateVersion7() is minted; ProviderSubjectIndex[P] = S
  And: IdentityAccount stream created at S with [EmailVerified{S,T}, RiderRegistered{S,"Maya O.",T}]
  And: identity.rider-registered { subjectId: S, displayName: "Maya O.", registeredAt: T } published
  And: RegisteredActorsBySubject + ProviderSubjectIndex updated inline; IdentityAuditTimeline async
```

**At-least-once redelivery (idempotency)**
```
Given: ProviderSubjectIndex[P] = S already exists (prior delivery)
When: ProviderUserVerified { providerSub: P, ... } arrives again
Then: handler observes existing subjectId; silent-ack; no second stream, no re-publish
```

**Race-condition redelivery**
```
Given: no index entry yet; ProviderSubjectIndex async-behind
When: two redelivered signals for P race past the lookup into MartenOps.StartStream
Then: first commit wins; second fails ExistingStreamIdCollisionException → silent-ack
  And: net result is one account, one publication
```

#### Candidate projections

| Projection | Audience | Shape | Status |
|---|---|---|---|
| `ProviderSubjectIndex` | Identity-internal (idempotency + provider-`sub` mapping) | `providerSub → subjectId` | **Slice-6.1 inline.** Load-bearing; never exposes `providerSub` outside Identity. |
| `RegisteredActorsBySubject` | Identity-internal lookup + downstream-audit | `subjectId → { isRider, isDriver, emailVerified, registeredAt }` | **Slice-6.1 inline.** |
| `IdentityAuditTimeline` | Ops / compliance | per-`subjectId` registration + verification history | **Slice-6.1 async.** |

#### Decisions locked in this slice

| Decision | Resolution |
|---|---|
| Pattern | Klefter translation-in + stream-creating (`MartenOps.StartStream`, plain handler, not `[AggregateHandler]`) + outbound publication. |
| Inbound | Provider lifecycle signal (abstract `ProviderUserVerified`); magic-link mechanics not modeled (ADR-006). |
| `subjectId` | Identity-minted UUIDv7; `providerSub → subjectId` in `ProviderSubjectIndex` (inline); raw `providerSub` never published. |
| Events | `EmailVerified` (account-level, role-agnostic) + `RiderRegistered` (role-registration fact, carries initial display name). Both appended at StartStream. |
| Publication | `identity.rider-registered` (ADR-014, session-keyed by `subjectId`); domain IDs only. |
| Idempotency | `providerSub` via `ProviderSubjectIndex` + `ExistingStreamIdCollisionException` catch. Inline projections are a silent dependency. |
| Display name | Initial value on `RiderRegistered` from provider claim; edits are Rider Profile's (grill #1). |

#### Cross-references

- **Forward (6.2):** Driver sign-up is structurally parallel; reuses the minting + translation-in + idempotency shape.
- **Forward (6.3):** Rider-becomes-driver appends `DriverRegistered` to the stream this slice creates.
- **Forward-constraint honored:** `identity.rider-registered` for Trips' `RiderProfileSnapshot` (W002 §13 #2, the account-existence half).
- **ADR-006:** First full exercise — provider-`sub` stays inside Identity; domain `subjectId` published. **ADR-014:** fourth-BC topic instance.

### 6.2 Slice 2 — Driver sign-up (net-new driver; paired with 6.3)

**Pattern:** Translation-in (Klefter) + stream-creating + publication. Structurally identical to 6.1.
**Trigger:** Provider verified-registration lifecycle signal, `flowType: Driver`, for a `providerSub` with no existing account.

Same shape as 6.1 with role substitutions: mints `subjectId`; records `providerSub → subjectId`; `MartenOps.StartStream<IdentityAccount>(subjectId, EmailVerified, DriverRegistered)`; publishes `identity.driver-registered { subjectId, registeredAt }` (session-keyed by `subjectId`).

**Event — `DriverRegistered`** — `{ subjectId, registeredAt }`. Role-registration fact. **No `displayName`** on the publication payload — driver display-name surfacing isn't a W004 consumer need (Onboarding consumes `subjectId` only); a driver display name, if later needed, comes from Driver Profile (parallel to grill #1's rider reasoning). `providerSub` not on the event (ADR-006).

**Publication — `identity.driver-registered`** — `{ subjectId, registeredAt }`. Consumed by **W004 Slice 6.1's Onboarding intake** (creates the Application process stream) and Driver Profile (pending). Honors the W003/W004 forward-constraint with the exact payload W004 §6.1 specified (`subjectId` + `registeredAt`), at-least-once.

**GWT — net-new driver:** structurally identical to 6.1's happy path; substitutes `DriverRegistered` / `identity.driver-registered` and `flowType: Driver`.

**Decisions locked:** same as 6.1 (translation-in + StartStream + publication; idempotency via `providerSub`); `identity.driver-registered` payload is `subjectId` + `registeredAt` per W004 §6.1's specified contract; no display name on the driver publication.

### 6.3 Slice 3 — Rider-becomes-driver (append to existing account; paired with 6.2)

**Pattern:** Translation-in (Klefter) + **append-to-existing-stream** (`[AggregateHandler]` / `FetchForWriting`, NOT stream-creating) + publication.
**Trigger:** Provider verified-registration lifecycle signal, `flowType: Driver`, for a `providerSub` that **already has an `IdentityAccount`** (existing rider).

Exercises grills #3 + #4 and closes the W004 intake gap.

#### The contrast with 6.2 — the key teaching point

| | 6.2 net-new driver | 6.3 rider-becomes-driver |
|---|---|---|
| `ProviderSubjectIndex[providerSub]` | absent | **present** (existing `subjectId`) |
| Stream operation | `MartenOps.StartStream` (new) | **append to existing** `IdentityAccount` |
| `subjectId` | minted fresh | **reused** (the existing one) |
| `EmailVerified` | emitted (first verification) | **NOT re-emitted** (already verified) |
| `DriverRegistered` | emitted | **emitted** (appends as second registration fact) |
| `identity.driver-registered` | published | **published** (role-registration-fact semantics — grill #4) |

The handler branches on `ProviderSubjectIndex` lookup: index-miss → 6.2 path (StartStream); index-hit → 6.3 path (append `DriverRegistered`, guard against a duplicate already present). Both publish `identity.driver-registered` — which is exactly why W004's single intake trigger works for both (grill #4).

#### Flow on the board

```
   Provider signal { providerSub: P (existing rider), flowType: Driver, verifiedAt }
                  │
                  ▼
   ┌─────────────────────────────────────┐
   │ Identity Translation-in handler      │
   │ ProviderSubjectIndex[P] → S (HIT)    │  ← existing account
   │ Load IdentityAccount stream S        │
   │ Guard: IsDriver already? → no-op     │  (idempotency: don't double-register)
   └──────────────┬──────────────────────┘
                  │ append DriverRegistered to stream S
                  │ + OutgoingMessages → identity.driver-registered
                  ▼
   ┌─────────────────────────────────────┐
   │ DriverRegistered (appended to S)     │  state.IsDriver = true
   │ (no EmailVerified — already verified)│  (now both rider AND driver)
   └──────────────┬──────────────────────┘
                  ▼
   ASB: identity.driver-registered { subjectId: S, registeredAt }
   → W004 Onboarding intake creates Application (idempotent on subjectId)
```

#### GWT sketches

**Rider-becomes-driver — happy path**
```
Given: IdentityAccount S exists with [EmailVerified, RiderRegistered]; ProviderSubjectIndex[P] = S; state.IsDriver = false
When: ProviderUserVerified { providerSub: P, flowType: Driver, verifiedAt: T2 } arrives
Then: DriverRegistered { subjectId: S, registeredAt: T2 } appends to stream S
  And: state.IsDriver = true (one account, both rider + driver facts)
  And: identity.driver-registered { subjectId: S, registeredAt: T2 } published
  And: W004 Onboarding intake creates the Application (idempotent on S)
  And: NO new EmailVerified; NO new account
```

**Idempotent re-signal (already a driver)**
```
Given: state.IsDriver = true (DriverRegistered already on stream S)
When: ProviderUserVerified { providerSub: P, flowType: Driver } arrives again
Then: guard fires (IsDriver already true); no-op; no second DriverRegistered, no re-publish
```

#### Decisions locked

| Decision | Resolution |
|---|---|
| Pattern | Translation-in + append-to-existing (`[AggregateHandler]`/`FetchForWriting`) + publication. Distinct from 6.1/6.2's stream-creating shape. |
| Branch logic | `ProviderSubjectIndex` lookup: miss → StartStream (6.2); hit → append (6.3). One handler, two paths. |
| `EmailVerified` | Not re-emitted for an already-verified account. |
| `DriverRegistered` idempotency | Guard on `state.IsDriver` — don't double-register. |
| `identity.driver-registered` fires for both | Role-registration-fact semantics (grill #4) — net-new and rider-becomes-driver both publish; preserves W004's single intake trigger. |

#### Cross-references

- **Grills #3 + #4 realized:** one account per human; role-registration-fact trigger; W004 intake gap closed without a W004 edit.
- **Forward-constraint honored:** `identity.driver-registered` for W004 §6.1 (both net-new and rider-becomes-driver paths).

### 6.4 Slice 4 — Identity projections (View)

**Pattern:** View. Consolidates the three projections the translation slices feed. No new events.

| Projection | Audience | Shape | Lifecycle | Feeders |
|---|---|---|---|---|
| `ProviderSubjectIndex` | Identity-internal (idempotency + provider-`sub` mapping) | `providerSub → subjectId` | **Inline** (load-bearing for redelivery idempotency + handler branch logic 6.2/6.3) | `EmailVerified` (stream-creating). Never exposes `providerSub` outside Identity (ADR-006). |
| `RegisteredActorsBySubject` | Identity-internal lookup; downstream-audit substrate | `subjectId → { isRider, isDriver, emailVerified, displayName?, registeredAt }` | **Inline** | `EmailVerified`, `RiderRegistered`, `DriverRegistered` |
| `IdentityAuditTimeline` | Ops / compliance | per-`subjectId` chronological registration + verification history | **Async** (Marten daemon) — not on a latency-sensitive path | All Identity events |

**Why these three and no more:** the workshop is genuinely thin. Identity publishes facts other BCs consume via their *own* projections (Trips' `RiderProfileSnapshot`, Onboarding's intake) — Identity doesn't build cross-BC read models; that's each consumer's job. `ProviderSubjectIndex` is the one Identity-specific load-bearing projection (the ACL's internal mapping); the other two are lookup + audit.

**Decisions locked:** three projections; `ProviderSubjectIndex` + `RegisteredActorsBySubject` inline; `IdentityAuditTimeline` async. No downstream-consumer projections built in Identity.

**Cross-reference:** `ProviderSubjectIndex`'s inline requirement is the W005 analog of W004's `BackgroundCheckVendorCaseIndex` silent dependency — a translation-dominant BC needs an inline index to route/idempotency-check inbound provider signals that arrive keyed by a foreign ID (`providerSub`) rather than the domain key (`subjectId`).



---

## 10. Candidate Protobuf Contract Surface

Names only, per ADR-009 and W001 §12.6 #4.

**Inbound (provider lifecycle — not CritterCab-authored):**

| Contract | Direction | Notes |
|---|---|---|
| `ProviderUserVerified` (abstract) | OIDC provider → Identity | Microsoft Graph change-notification schema in prod; provider-owned. CritterCab does not author it; Identity translates it. |

**Outbound (new, deferred authorship):**

| Contract | Direction | Topic | Notes |
|---|---|---|---|
| `RiderRegistered` | Identity → Trips / Rider Profile | `identity.rider-registered`, session-keyed by `subjectId` | Payload `{ subjectId, displayName?, registeredAt }`. Domain IDs only (no `providerSub`). |
| `DriverRegistered` | Identity → Onboarding / Driver Profile | `identity.driver-registered`, session-keyed by `subjectId` | Payload `{ subjectId, registeredAt }` per W004 §6.1's exact contract. |

Files under `/protos/crittercab/identity/v1/`. Authorship session per PR #4 precedent; W004 §X+1 #3 already named the `DriverRegistered` proto as a forward-constraint — now both are concretely named.

---

## X. Forward-constraints handled (the three accumulated inbound constraints)

| # | Forward-constraint | Source | Disposition |
|---|---|---|---|
| 1 | Identity publishes `identity.rider-registered` | W002 §13 #2 | **Honored** (Slice 6.1). Account-existence + initial display name; consumed by Trips' `RiderProfileSnapshot`. |
| 2 | Identity publishes `identity.rider-profile-updated` | W002 §13 #2 | **Refined / overridden** (grill #1). Profile data is Rider Profile's; reassigned to Rider Profile's workshop. `identity.rider-registered` carries the *initial* display name; Rider Profile owns *edits*. Documented override — W002 stays locked; W005 refines via reasoning, not edit. |
| 3 | Identity publishes `identity.driver-registered` (`subjectId` + `registeredAt`; at-least-once; `DriverRegistered` proto) | W003 Story 1 step 4 + W004 §6.1 / §X+1 #1–#3 | **Honored** (Slices 6.2 + 6.3). Exact payload per W004's contract. Role-registration-fact semantics (grill #4) extend it to fire for rider-becomes-driver too, closing W004's intake gap. |

---

## X+1. Forward-constraints generated

| # | Forward-constraint | Source | Target workshop |
|---|---|---|---|
| 1 | **Rider Profile must publish a profile-updated event** (the reassigned `rider-profile-updated`); Trips' `RiderProfileSnapshot` subscribes to it for display-name *edits* (Identity supplies only the initial value). | Grill #1 / Slice 6.1 | Rider Profile workshop (pending) |
| 2 | Rider Profile + Driver Profile consume `identity.rider-registered` / `identity.driver-registered` (account-existence). | Slices 6.1 / 6.2 | Rider Profile + Driver Profile workshops |
| 3 | Identity business-event Protobuf authorship — `RiderRegistered`, `DriverRegistered` under `/protos/crittercab/identity/v1/`. | §10 | Proto-authorship session (PR #4 precedent) |

---

## 11. ADR Candidates

**New candidate (firm per grill #2):**

- **ACL-as-BC / translation-dominant modeling pattern** — CritterCab's **third modeling shape**, alongside aggregate-per-invariant (ADR-012; W001/W002) and Process Manager via Handlers (W004 candidate). Delineates when a BC is "just an ACL" (no load-bearing invariant; streams are record-keeping/projection substrate, not consistency boundaries; almost-all-Translation-slices) vs. when it warrants aggregate-per-invariant or a Process Manager. Evidence: Identity (W005). Trigger to author: first Identity implementation slice. **"Not currently" caveat:** if account-linking/merging is later modeled, a load-bearing invariant may emerge and reopen the translation-dominant-vs-aggregate call.

**Locked ADRs — status after W005:**

- **ADR-006 (Identity-as-swappable-ACL)** — **first full exercise.** Not a new "pattern across BCs" evidence point (single-BC decision); W005 is its canonical realization as concrete translation slices. `subjectId` Identity-minted (grill #6) is the ADR-006 compliance point made explicit.
- **ADR-014 (ASB topic naming)** — **fourth-BC instance** (`identity.rider-registered`, `identity.driver-registered`). Confirms.
- **ADR-013 (shared cross-BC identifier)** — **nuance confirmed, not a new participant.** `subjectId` is intra-BC, FK-referenced; ADR-013 explicitly scopes intra-BC IDs out. Distinct from W004's `applicationId == driverProfileId` lifecycle key.
- **ADR-012 (aggregate-per-invariant)** — **not a new evidence point** (Identity is translation-dominant, not an aggregate-cluster) — the second BC after Onboarding (W004) to honestly *not* extend ADR-012's base, from the opposite direction (too thin vs. too coordination-heavy).

---

## 12. Retrospective

Nine-subsection shape per W001 §12 / W002 §14 / W004 §12, plus §12.10 for the methodology question. The full CritterCab-format session retro lives at [`docs/retrospectives/workshops/005-identity-event-modeling.md`](../retrospectives/workshops/005-identity-event-modeling.md).

### 12.1 Intent vs. outcome

Goal: produce Identity's event model, EM-direct, honoring the three accumulated forward-constraints; pilot the ACL-translation-dominant shape; provide the entry-005 contrast data point. **Outcome:** 4 slices, 3 events, 3 projections — the thinnest CritterCab workshop, as predicted. ACL-translation-dominant locked as the third modeling shape. All three forward-constraints dispositioned (2 honored, 1 refined/reassigned). `subjectId` ADR-006 compliance made explicit. W004 intake gap closed (grills #3/#4). **Goal met.**

### 12.2 What worked

- **The grill-with-docs pass front-loaded every design call.** The workshop was transcription + a thin slice walk — no mid-walk design debates. Seven grills resolved scope, modeling shape, and every boundary question before the session ran.
- **The translation-dominant shape held.** Every slice was a Klefter translation-in; no aggregate-invariant ceremony. The thinness was honest, not under-specification.
- **The 6.2/6.3 contrast (net-new StartStream vs. append-to-existing)** is a clean teaching artifact for the role-registration-fact pattern + one-account-per-human correlation.
- **`subjectId` ADR-006 compliance caught + made explicit** (grill #6) — a latent issue that had propagated implicitly through W003/W004 is now documented.

### 12.3 What was hard / friction

- **Almost nothing — and that's the finding.** The grill did the hard work; the workshop was light. The one substantive in-workshop framing (provider-lifecycle-event inbound per ADR-006, refining W003's DS simplification, §3.4) wasn't a grill target but fell out cleanly from ADR-006.
- **The `ProviderSubjectIndex` inline dependency** is the W005 analog of W004's `BackgroundCheckVendorCaseIndex` — a recurring translation-dominant-BC pattern worth noting for future ACL BCs.

### 12.4–12.5 Meta-decisions + patterns

- **NEW pattern: ACL-translation-dominant** (third CritterCab modeling shape). The "is there a load-bearing invariant?" question (ADR-012's lens) is the discriminator: none → translation-dominant; one fitting a single stream → aggregate-per-invariant; coordination-dominated → Process Manager.
- **Translation-dominant BCs need an inline foreign-key index** (`ProviderSubjectIndex` / `BackgroundCheckVendorCaseIndex`) to route + idempotency-check inbound signals keyed by a foreign ID.
- Grill-with-docs as a prompt-sharpening pass *before* the workshop produces a near-frictionless walk.

### 12.6 Adjustments for the next workshop

- For thin ACL BCs, the grill *is* most of the design work; budget the workshop session as short.
- W001 §12.6 + W002 §14.6 adjustments continue to apply.

### 12.7 Quality signal

User signed off "as drafted" at every gate; the grill (7 branches) did the substantive deciding, with genuine engagement. No new feedback memories warranted.

### 12.8 Follow-ups

- ACL-as-BC ADR candidate (trigger: first Identity implementation slice).
- 3 forward-constraints generated (§X+1) — Rider Profile profile-updated reassignment; Rider/Driver Profile consumption; Identity proto authorship.
- Methodology log entry 006 (two-axis distinction — see §12.10).

### 12.9 Workshop status

**Complete (v0.5, 2026-05-28).** First in-repo ACL-translation-dominant reference; first EM workshop pulled into existence by accumulated forward-constraints; thinnest CritterCab workshop.

### 12.10 Methodology question — EM-direct on a thin BC (entry-005 contrast)

**Did EM-direct on a thin/mechanical BC surface mid-walk vocabulary friction a DS pre-step would have caught? No — and the grill #5 two-axis distinction is the finding.** The W005 grill surfaced three real nuances, but all were **ownership-boundary** calls (which BC owns profile data; one-account-per-human; which BC's event triggers Onboarding intake) — *zero* **vocabulary-divergence** findings. DS surfaces vocabulary divergence; grill-with-docs surfaces boundary calls. Identity was **boundary-nuanced but not vocabulary-rich**, so EM-direct was correct *and* the boundary nuances were caught by the grill regardless of DS.

This **confirms** methodology log entry 005's DS-scoping (thin/mechanical BCs don't need DS for vocabulary) and **refines** it with a two-axis model: the DS-vs-EM-direct axis is about *vocabulary richness*; *boundary nuance* is a separate axis handled by grill-with-docs. **Methodology log entry 006 records this** — the cross-cutting observation closing the confirm-or-refine loop entry 005 opened, spanning W004 (DS-fed, vocabulary-rich) and W005 (EM-direct, boundary-nuanced).

---

## Document History

- **v0.1** (2026-05-28): Header + §1 Session Log + §2 Scope (Option A, grill-locked) + §3 Modeling pattern sidebar (ACL-translation-dominant — third CritterCab modeling shape; thin `IdentityAccount` stream; `subjectId` Identity-minted provenance; provider-lifecycle-event inbound per ADR-006) + §4 Ubiquitous Language + §5 Event List scaffold. All design leans pre-resolved by the grill-with-docs pass (grills #1–#7). Slice walk pending.
- **v0.2** (2026-05-28): §6 slice ordering committed (4 slices; translation-dominant spine). Slice 6.1 walked — Rider sign-up: Klefter translation-in of the provider verified-registration lifecycle signal (magic-link mechanics not modeled per ADR-006), `subjectId` minted as Identity-domain UUIDv7 with `providerSub → subjectId` recorded in the inline `ProviderSubjectIndex` (raw `providerSub` never published — ADR-006), stream-creating via `MartenOps.StartStream<IdentityAccount>` with `EmailVerified` + `RiderRegistered`, `identity.rider-registered` publication (ADR-014 fourth-BC instance; carries initial display name per grill #1; domain IDs only), idempotency on `providerSub` + `ExistingStreamIdCollisionException` catch, three projections (`ProviderSubjectIndex` + `RegisteredActorsBySubject` inline, `IdentityAuditTimeline` async). Honors the `identity.rider-registered` forward-constraint (W002 §13 #2 account-existence half). Event List updated with events #1-#2. Slices 6.2-6.4 pending.
- **v0.3** (2026-05-28): Slices 6.2 + 6.3 walked as paired walk. 6.2 Driver sign-up (net-new): structurally parallel to 6.1; `DriverRegistered` (no display name on the driver publication — Onboarding consumes `subjectId` only); `identity.driver-registered { subjectId, registeredAt }` per W004 §6.1's exact contract. 6.3 Rider-becomes-driver: **append-to-existing-stream** (`[AggregateHandler]`/`FetchForWriting`, not stream-creating) on a `ProviderSubjectIndex` hit; reuses the existing `subjectId`; no new `EmailVerified`; `DriverRegistered` appends with an `IsDriver` idempotency guard; `identity.driver-registered` publishes (role-registration-fact semantics). One handler, two paths (index-miss → StartStream; index-hit → append). **Realizes grills #3 + #4 and closes the W004 Onboarding-intake gap with no W004 edit** — both net-new and rider-becomes-driver publish `identity.driver-registered`, so W004's single intake trigger fires for both. Event List updated with event #3. Slice 6.4 pending.
- **v0.4** (2026-05-28): Slice 6.4 walked — Identity projections (View): `ProviderSubjectIndex` (inline; provider-`sub` → `subjectId`; load-bearing for idempotency + 6.2/6.3 branch logic; never exposes `providerSub`), `RegisteredActorsBySubject` (inline; lookup + audit substrate), `IdentityAuditTimeline` (async; ops/compliance). No downstream-consumer projections built in Identity — consumers project from the published `identity.*` events. `ProviderSubjectIndex` noted as the W005 analog of W004's `BackgroundCheckVendorCaseIndex` silent dependency (translation-dominant BCs need an inline foreign-key index). **Slice walk complete: 4 slices, 3 events, 3 projections — the thinnest CritterCab workshop, as the grill predicted.**
- **v0.5** (2026-05-28): Session close. §10 Protobuf surface (`RiderRegistered`, `DriverRegistered` named; provider inbound not CritterCab-authored). §X Forward-constraints handled (3: `identity.rider-registered` honored; `identity.rider-profile-updated` refined/overridden → reassigned to Rider Profile per grill #1; `identity.driver-registered` honored per W004's contract). §X+1 Forward-constraints generated (3: Rider Profile profile-updated reassignment; Rider/Driver Profile consumption; Identity proto authorship). §11 ADR Candidates (**ACL-as-BC / translation-dominant firm new candidate — CritterCab's third modeling shape**; ADR-006 first full exercise; ADR-014 fourth instance; ADR-013 nuance confirmed; ADR-012 honestly-not-a-new-evidence-point from the opposite direction to W004). §12 Retrospective (nine-subsection + §12.10 entry-005-contrast: EM-direct confirmed; the grill-#5 two-axis distinction — DS surfaces vocabulary, grill-with-docs surfaces boundaries — drives methodology log entry 006). **Workshop status: complete.** Remaining close-out deliverables (separate retro file, methodology log entry 006, README updates, conditional context-map edge-#3 supplier-side lock) land in further commits on this branch; combined PR per the path-2 decision.
