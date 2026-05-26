# Ride-Sharing Driver Onboarding — Domain Note

Background material for [Workshop 003 — Onboarding Domain Story](../workshops/003-onboarding-domain-story.md), per the [prompt's Phase 0 instruction](../prompts/workshops/003-onboarding-domain-storytelling.md#working-pattern-solo--ai-adaptation). The note grounds the Domain Storytelling session in industry-typical onboarding shape, vocabulary candidates, and boundary questions worth probing — so that the session has enough domain texture to be more than a system-viewpoint description of an imagined CritterCab.

This note is **non-normative**. It lives in `docs/research/` rather than `docs/workshops/` or `docs/skills/` for a reason: nothing here is committed CritterCab knowledge. Claims promoted to canonical project knowledge do so by surfacing in the DS artifact's §Findings (Phase 5) or in the eventual Workshop 004 (Onboarding event model) — never by being lifted directly from this note.

The note is **frozen** at the end of Phase 0. A finding that the note got something wrong becomes a retro entry, not an in-flight edit. The freeze keeps the artifact stable as ground truth for what the user "knew going in," even if the DS session corrects it.

---

## 1. Scope and posture

### 1.1 What this covers

Ride-sharing driver onboarding as observed across the industry — Uber, Lyft, Bolt, Grab, DiDi, Curb, Via, and the regional analogues. Specifically the lifecycle from a prospective driver expressing interest through becoming eligible to take rides, plus the post-approval lifecycle events (suspension, reinstatement, deactivation) that share an actor with onboarding but may belong to different BCs.

### 1.2 What this does not cover

- **Rider onboarding.** Rider sign-up is materially simpler — usually identity + payment method + maybe phone verification — and lives in a different lifecycle. Out of scope for the DS session and out of scope for this note.
- **Driver-app UX.** The wireframe sketches in this note are sufficient for capturing actor / verb / work-object relationships in the DS notation. Production-grade UX is a separate concern.
- **Specific vendor APIs.** Background-check and document-verification vendors (Checkr, Sterling, Onfido, Persona, Veriff, Jumio) are named where their *patterns* are relevant. Their actual API surfaces are not modeled.
- **Regulatory specifics by jurisdiction.** TLC (NYC), PSC (San Francisco), state TNC commissions, EU "VTC" licensing schemes vary enormously. The note treats them collectively as "jurisdiction-required external approvals" without modeling any one of them.

### 1.3 Posture

Plausible-domain, not field-research-grade. The user (Erik) has not worked at a ride-sharing platform. The note distills publicly-observable patterns from documented industry experience plus general familiarity with consumer-facing platform onboarding. Where a claim leans on educated guess rather than documented evidence, it is flagged with *(inference)*.

---

## 2. The shape of driver onboarding

### 2.1 The journey in seven stages

Industry-typical onboarding decomposes into a sequence of stages, though platforms differ on which stages exist as discrete steps and which collapse into a single screen:

1. **Interest signal.** Prospective driver visits a "Drive with us" or "Become a [Partner]" page. May provide email + city to receive a magic link; may immediately proceed to account creation.
2. **Identity account creation.** Authentication credentials set up (email/password, OAuth via Google or Apple, or phone-based OTP). This is the OIDC sign-up event — the moment the actor becomes an *authenticated entity* the platform knows about.
3. **Application intake.** Personal information collected: legal name, date of birth, address, SSN-or-equivalent, phone number, city of operation, sometimes a referral code.
4. **Document collection.** Driver's license (front + back, sometimes selfie for matching), vehicle registration, insurance certificate, sometimes business or TLC license, sometimes a vehicle photo. Documents are uploaded; each enters a per-document review lifecycle.
5. **Background check.** SSN trace, criminal history (county + state + federal), sex offender registry, motor vehicle record (MVR). Run by a third-party vendor. Returns asynchronously — typical SLA 1–7 days, but can stretch.
6. **Jurisdiction-specific approvals.** Some markets require a vehicle inspection slot, a TLC/PSC license number, or a city-issued permit. These are gating but external — the platform schedules or accepts evidence but does not perform the inspection itself.
7. **Approval and activation.** All gates clear → applicant becomes eligible to take rides. This may be a single event (an approval flip) or a graduated rollout (e.g., first 24 hours show "ready to drive" but no actual offers are routed, sometimes called *soft launch* or *first-day shadow*).

### 2.2 Where the platforms differ

- **Some platforms accept partial application starts** ("save and come back later"); others require a single-session completion.
- **Some platforms run the background check in parallel with document collection** (start it the moment SSN is captured, even if documents are still being uploaded); others gate it behind document completion.
- **Some platforms require Identity-account-first** (you must be an authenticated user before you can begin an application); others let you fill out an entire application unauthenticated and prompt for account creation only at submit.
- **Some platforms treat the vehicle as a separate sub-application** (one driver, multiple vehicles, each with its own document set); others fold the vehicle into the primary application.

These differences matter for boundary design: where the platforms diverge is exactly where CritterCab gets to make a deliberate choice.

### 2.3 Wireframe sketches (illustrative, not normative)

```
┌─ Become a Driver — Step 1 of 5 ────────────┐
│                                             │
│  Email:    [ ___________________ ]          │
│  Phone:    [ ___________________ ]          │
│  City:     [ Omaha            ▼ ]           │
│                                             │
│  [        Continue          ]               │
│                                             │
└─────────────────────────────────────────────┘

┌─ Driver Dashboard — Application in progress ┐
│                                             │
│  ✓ Identity verified                        │
│  ✓ Personal info                            │
│  ✓ Driver's license (under review)          │
│  ⚠ Vehicle registration — please re-upload  │
│  ○ Insurance certificate                    │
│  ○ Background check — awaiting documents    │
│                                             │
│  Estimated time to approval: 3–5 days       │
│                                             │
└─────────────────────────────────────────────┘
```

The dashboard shape is the most useful frame for the DS session: the actor (applicant) and the platform exchange information about a *work object* (the application) over multiple sittings, with the work object passing through several status transitions per sub-document. That is exactly the shape DS captures cleanly.

---

## 3. Vocabulary candidates

The Onboarding BC has at least one actor (the human becoming a driver) and at least one work object (the application). Both have many candidate names in industry use. The DS session's job is to surface which CritterCab should adopt and which it should reject — but it helps to have the candidates enumerated first.

### 3.1 Actor names — the human becoming a driver

| Candidate | Used by | Connotation | Lifecycle moment |
|---|---|---|---|
| **Prospective driver** | Mostly internal-Ops vocabulary | Has expressed interest; may or may not have an Identity account yet | Pre-application |
| **Applicant** | Common in HR-adjacent platforms | Has begun the application | During application |
| **Candidate** | Checkr and other background-check vendors; some platforms | Subject of a background-check workflow | During background check (vendor-imposed) |
| **Partner** | Lyft, Bolt, Grab | Marketing-friendly; positions the relationship as collaborative | Post-approval; sometimes also during application |
| **Driver-partner** | Uber (older usage) | Formal version of partner | Post-approval |
| **Driver** | Universal | The default; assumes can take rides | Post-approval, active |
| **Vetted driver** | Internal-Ops; some legal/compliance contexts | Background-check-cleared, but not necessarily eligible to drive | Post-approval, status indeterminate |
| **Suspended driver** | Universal | Was active, currently can't take rides; temporary | Post-approval, suspended |
| **Deactivated driver** | Uber, Lyft | Account terminated; more permanent than suspended | Post-approval, terminal |
| **Reactivated driver** | Internal-Ops | Returned from suspension | Post-approval, active again |

The **homonym risk** sits on "driver" — does it always mean active driver, or sometimes anyone who has ever applied? The **synonym risk** sits on applicant / candidate / prospective driver — do these refer to the same lifecycle moment or distinct ones?

CritterCab's vision-doc Onboarding description ("application, document upload, background check, approval, suspension, reinstatement") implies suspension and reinstatement are inside Onboarding. That's a non-default position — many platforms put suspension under a separate Trust & Safety or Operations BC because the *reason for suspension* (rider complaint, low rating, regulatory violation, etc.) does not originate in onboarding. Worth probing in Phase 4.

### 3.2 Work-object names

| Candidate | What it refers to | Lifecycle |
|---|---|---|
| **Application** | The driver's submission of self + documents + consents | From start to terminal (approved or rejected); afterwards may persist as historical record |
| **Profile** | The driver's persistent identity within the platform | Begins at some point during onboarding; persists forever |
| **Driver record** | Internal/Ops view of the driver including history | Begins at first contact; persists forever |
| **Vetting case** | The unit of background-check work | Begins at background-check start; ends at vendor return |
| **Document** | A single uploaded artifact (license, registration, etc.) | Per-document lifecycle: uploaded → under review → accepted / rejected → (if rejected) re-uploaded |

The **homonym risk** sits on "profile" — Identity has a profile concept (the OIDC user record); Onboarding may have one (the application's biographical data); Driver Profile BC has one (post-approval driver-specific data). Three "profiles," three different things. Probable Phase 4 finding.

The **synonym risk** sits on "application" vs. "vetting case" vs. "driver record" — these may collapse to one work object in CritterCab's model, or split into two or three. The DS session decides empirically by following whichever name the captured story uses naturally.

### 3.3 Activity / verb candidates

The DS notation captures activities as numbered arrows with verbs. Industry-typical verbs in this domain include:

- **submits** (application, document)
- **uploads** (document)
- **verifies** (document, identity, SSN)
- **reviews** (document, application — by manual reviewer)
- **approves / rejects** (document, application — by reviewer or by automated check)
- **clears / fails** (background check)
- **requests** (additional document, clarification — outbound to applicant)
- **resubmits** (document — after rejection)
- **suspends / reinstates / deactivates** (driver)
- **notifies** (applicant — about status changes)
- **adjudicates** (background-check result — manual reviewer disposition)

The DS session captures the verbs that emerge naturally from the story; this list is just primer.

---

## 4. Background-check patterns

Background checks are the most asynchronous part of onboarding and the most likely source of failure paths. Worth understanding their shape in detail because Phase 3's story is built around their terminal-rejection case.

### 4.1 Typical vendor flow

1. Platform collects applicant SSN + consent.
2. Platform issues a background-check request to the vendor (Checkr / Sterling / etc.) with the applicant's PII.
3. Vendor returns an immediate acknowledgment with a *case ID* and a *status* of `Pending`.
4. Vendor runs the actual checks asynchronously — typical SLA is 1–7 days for clean cases, weeks for cases requiring county-level court records or international components.
5. Vendor pushes status updates via webhook. Possible terminal statuses:
   - **Clear** — no disqualifying findings; platform may auto-approve this gate.
   - **Consider** — findings exist that require manual platform-side review. Platform-side adjudicator decides clear / reject.
   - **Suspended** — vendor cannot complete the check (e.g., SSN doesn't match the name; missing data). Applicant must provide additional information.
   - **Engaged** — case is paused awaiting applicant action (e.g., the applicant must respond to the vendor's identity-verification challenge).
   - **Disputed** — applicant has disputed the result through the vendor; case is in re-review.

### 4.2 What this means for CritterCab's model

- **Background-check status is itself a stateful sub-lifecycle.** Modeling it as a single boolean (`backgroundCheckPassed`) loses information. The DS session likely surfaces this naturally if Phase 3's story explores the `Consider`-then-adjudicate path.
- **Webhook-driven async status updates** are a textbook ACL pattern — vendor-specific events translated into domain events at the Onboarding boundary. *(inference)* This may end up mirroring the Identity BC's ACL stance against the OIDC provider.
- **Adjudication is a human-in-the-loop decision** that lives somewhere. Is it Onboarding's responsibility (the BC owns the vetting lifecycle) or Operations' (Operations owns "manual intervention by platform staff")? Worth probing.

### 4.3 FCRA and notification requirements (US-only, abbreviated)

In the US, the Fair Credit Reporting Act requires specific notifications when a background check leads to adverse action: pre-adverse-action notice (with the report attached), an adjudication waiting period, and a final adverse-action notice. This is regulatory machinery that the platform owes the applicant. It generates events with specific timing constraints that the model should not erase. *(Not deep-modeled here; flagged as a class of constraint.)*

---

## 5. Document-verification patterns

### 5.1 Typical document lifecycle

```
   uploaded
     ↓
   under-review (automated or manual)
     ↓
   ┌── accepted ──→ committed to the application
   ↓
   rejected ──→ applicant notified ──→ (optional) re-uploaded
                                            ↓
                                      back to under-review
```

Per-document state. An application is *complete* when every required document is in the `accepted` terminal. Documents have **expiry dates** (driver's license expiry, insurance certificate expiry); accepted documents can transition out of `accepted` when their expiry passes, re-entering an active state requiring renewal.

### 5.2 Verification mechanisms

- **Pure automated.** OCR + heuristic checks (license number format, expiry-date parse, photo quality). Common for first-pass screening; rejects blurry or wrong-document-type uploads immediately.
- **AI-based document verification.** Onfido, Veriff, Persona, Jumio. Compares the document photo to a selfie (liveness check); validates document authenticity against known-good templates. Returns clear / refer / fail.
- **Manual review.** A platform-side reviewer looks at the document and adjudicates. Often combined with automated as the fallback for ambiguous cases.

### 5.3 Re-upload patterns

The interesting modeling question: when a document is rejected and re-uploaded, is the re-upload a *new* document (new ID, new lifecycle, the old one persists as historical record) or an *update* to the existing document (same ID, status reverts to `under-review`, the rejection becomes part of the document's history)?

Both shapes appear in industry. The choice affects event modeling significantly — version-as-new-entity vs. version-as-state-machine has different event vocabularies. Phase 2's story will likely force this question.

---

## 6. Boundary candidates — Onboarding vs. adjacent BCs

The DS session's BC-boundary findings (Phase 5) are essentially answering: *do the lassos that emerge from the captured stories match the vision-doc's named BCs?* This section names the seams worth attention.

### 6.1 Onboarding ↔ Identity

[ADR-006](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) is firm: Identity is the ACL between the external OIDC provider and the rest of the system. The Identity BC publishes domain events (rider registered, driver account created) that other BCs consume.

The question DS surfaces: **when does the Onboarding application begin in relation to the Identity account?**

Three candidate orderings:

- **Identity-first.** Authenticate first; Onboarding application is downstream of an established Identity. This is the simpler model and matches how most platforms work in practice.
- **Onboarding-first.** Application started before authentication; Identity account created at submit time. Some platforms do this to reduce friction at the first step.
- **Parallel.** Identity created at step 1; Onboarding application also begins at step 1; they're entangled. Common in mobile-first sign-up flows.

CritterCab's ADR-006 + the vision-doc's "Onboarding is split from Identity" line jointly favor Identity-first. Worth confirming the DS story honors that ordering.

### 6.2 Onboarding ↔ Driver Profile

The vision doc says Driver Profile is "downstream of Identity and Onboarding" and holds "registered vehicles, current availability (online, offline, on break), service area, preferences." The context map flags Onboarding → Driver Profile as a *deferred edge* with expected pattern *customer-supplier* (edge #6 in [docs/context-map/README.md](../context-map/README.md#6-intra-actor-topology-deferred)).

The question DS surfaces: **when does Driver Profile come into existence?**

Three candidate orderings:

- **At application-start.** A placeholder Driver Profile exists from the moment Onboarding begins; it just isn't *active* until approval. Vehicles attached to the placeholder profile during application carry over directly into the active profile at approval.
- **At approval.** Driver Profile is created at the approval moment via an Onboarding → Driver Profile published event. Vehicles entered during application live in Onboarding's data; they're copied (or referenced) into Driver Profile at approval.
- **Lazily.** Driver Profile is created on first action that requires it (e.g., first time the driver goes online); the approval event is just a flag flip on the Onboarding side.

Each has different implications for where vehicle data, document data, and pre-approval activity logs live. Worth probing.

### 6.3 Onboarding ↔ Trust & Safety (candidate BC)

The vision-doc v0.1 inventory does not name Trust & Safety; the context map flags it as a candidate BC introduced by Workshop 002's fan-out tables. If the vision doc commits to suspension / reinstatement / deactivation as Onboarding concerns, T&S may not need to exist. If those concerns belong to T&S (or Operations), then Onboarding becomes narrower than the vision-doc description.

The question DS surfaces: **who owns the suspension lifecycle?**

The vetting lifecycle (application → approval) is clearly Onboarding. The disqualification lifecycle (active driver gets suspended → reviewed → reinstated-or-deactivated) shares an actor with Onboarding but has different *triggers* (complaint, rating, regulatory action — none of which originate in Onboarding) and different *reviewer* personae (T&S / Ops, not the document reviewer). Possibly two BCs sharing one actor.

### 6.4 Onboarding ↔ Operations

Manual review queues for document adjudication and background-check `Consider`-status adjudication need a human reviewer. That reviewer is plausibly Operations staff using Operations tooling, or plausibly Onboarding-specific staff using Onboarding tooling. Either way, the *act of adjudication* generates Onboarding-domain events; the *tool* is an Operations consumer.

### 6.5 Onboarding ↔ (vehicle as a sub-domain)

Most platforms treat **vehicles** as a first-class sub-domain: one driver may have multiple vehicles, each with its own document set (registration, insurance), each with its own approval lifecycle. The vision doc puts "registered vehicles" inside Driver Profile, but the *approval of a vehicle* feels like an Onboarding-shaped activity. Possibly the Driver Profile holds the *roster* of approved vehicles while Onboarding holds the *vehicle-approval workflow*. Possibly there's a Vehicle BC nobody has named yet. Probably flag-for-Workshop-004 rather than resolve in DS.

---

## 7. Edge cases worth probing

A non-exhaustive list of edge cases that DS stories can surface naturally if they arise, or that can be flagged in Phase 5 if they don't:

- **The half-applicant.** Someone who created an Identity account, started an application, then abandoned it for months. What state do they live in? Are they a "driver" in any sense?
- **The rider-becomes-driver.** Existing rider account opens a driver application. Does the existing Identity account carry over? Is there one actor or two?
- **The cross-jurisdiction move.** Approved driver in city A wants to drive in city B. Re-vetting? New application? Profile transfer? Worth knowing what CritterCab thinks it does (or doesn't model).
- **Expiry-driven re-vetting.** Driver's license expires while driver is active. New document required; old approval still valid pending re-upload. Who detects the expiry — Onboarding (still in the lifecycle?) or Driver Profile (post-approval state-holder?) or a generic Documents BC?
- **Vendor-side data correction.** Background-check vendor revises a previously-clear result (e.g., a court record surfaces months later). Does this re-open the Onboarding lifecycle?
- **Reinstatement after suspension.** What changes vs. initial approval? Is the document set re-verified? Is a new background check run?
- **Multi-vehicle.** As described in §6.5.

---

## 8. Sources

This note is grounding material, not academic synthesis. Sources are documented as **tiers** rather than enumerated link lists:

- **Tier 1 — Direct working knowledge.** General familiarity with consumer-platform onboarding patterns, OIDC sign-up flows, and the document-collection / background-check / approval shape. This is the bulk of the note.
- **Tier 2 — Industry-known vendor patterns.** Checkr, Sterling, Onfido, Persona, Veriff, Jumio are named because their patterns are widely-discussed in industry. Their actual API surfaces and SLAs are not cited because the note is not modeling any specific vendor.
- **Tier 3 — Regulatory frame.** FCRA references in §4.3 reflect known US regulatory machinery. Not deep-modeled.
- **Tier 0 — Inferences flagged inline.** Claims marked *(inference)* lean on educated guess rather than documented evidence. Worth weighting accordingly.

Where the DS session contradicts a claim in this note, the DS session wins — that is the whole point of running it.

---

## 9. What this note is not

- **Not a spec.** No event names, command names, or state-machine definitions are committed here. The DS session does that work.
- **Not a position paper.** Where boundary questions are raised (§6), candidate orderings are named without endorsing one. The DS session and the eventual Workshop 004 commit.
- **Not a research deliverable.** No new analysis was performed; the note synthesizes already-known patterns into one place for session readiness.
- **Not a place for new claims.** If the DS session surfaces something this note got wrong or didn't cover, that becomes a retro entry and (if material) a finding in the DS artifact's §Findings — not an edit here.

---

## Document history

- **v0.1** (2026-05-26): Authored as Phase 0 grounding material for [Workshop 003 — Onboarding Domain Story](../workshops/003-onboarding-domain-story.md). Non-normative; frozen at session start.
