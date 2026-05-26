# Workshop 003 — Onboarding Domain Story

**Status:** Complete (v0.1, 2026-05-26). First Domain Storytelling artifact in CritterCab. Three stories captured (happy path, document-rejection recoverable path, background-check terminal-rejection path). Vocabulary disambiguation pass complete. BC-boundary findings produced. 19 open questions queued for Workshop 004.
**Started / completed:** 2026-05-26 (single session).
**Facilitator / capture:** Erik Shafer (domain expert role; sign-off authority) and Claude (Opus 4.7; facilitator and capture).
**Methodology references:** Hofer & Schwentner, *Domain Storytelling* (Addison-Wesley 2021); [`docs/research/domain-discovery-playbook.md`](../research/domain-discovery-playbook.md) (Nick Tune facilitation vocabulary); [`docs/research/ride-sharing-driver-onboarding-domain-note.md`](../research/ride-sharing-driver-onboarding-domain-note.md) (Phase 0 grounding material; non-normative).
**Structural constraints honored:** [`docs/rules/structural-constraints.md`](../rules/structural-constraints.md) (ADR-002, ADR-006). No code; no ADR authorship.
**Triggering prompt:** [`docs/prompts/workshops/003-onboarding-domain-storytelling.md`](../prompts/workshops/003-onboarding-domain-storytelling.md).
**Retrospective:** [`docs/retrospectives/workshops/003-onboarding-domain-storytelling.md`](../retrospectives/workshops/003-onboarding-domain-storytelling.md).

---

## 1. Session log

| Session | Date | Duration | Phases covered | Notes |
|---|---|---|---|---|
| 1 | 2026-05-26 | Full session (single sitting) | All 8 phases (0 research note; 1–3 story capture; 4 vocabulary disambiguation; 5 findings synthesis; 6 vision-doc v0.5; 7 retro + index updates) | First Domain Storytelling exercise in CritterCab. Solo + AI adaptation per the prompt's working pattern. Per-phase sign-off discipline; no silent rollovers. Methodology decision at Phase 6: **Exercised** (DS committed as a permanent design-phase technique). |

---

## 2. Scope statement

### 2.1 In scope

The Onboarding bounded context's **driver-vetting lifecycle**, from a prospective driver expressing interest through becoming an active dispatchable driver, with two failure paths captured alongside the happy path:

- **Identity creation** as the upstream entry point — visible inside Story 1 to surface the Identity → Onboarding handoff explicitly rather than presuming it.
- **Application intake** — personal information, SSN, FCRA-compliant background-check consent.
- **Document collection and verification** — driver's license, vehicle registration, insurance certificate. Per-document state machine surfaced empirically through Story 2's rejection-recovery flow.
- **Background check** — vendor request, async status updates, terminal disposition. Vendor-vocabulary translation at the ACL boundary.
- **Adjudication** — human reviewer (Onboarding adjudicator) for vendor-recommended-review cases. First manual-human actor in any CritterCab workshop.
- **Approval and Driver-Profile activation** — cross-BC handoff to Driver Profile via ASB business event per ADR-014 conventions.
- **FCRA two-phase rejection** — pre-adverse-action notice, dispute window, final adverse-action notice, terminal-rejected application state.

### 2.2 At the boundary (modeled as actor crossings or ACL translations, not as internal behavior)

- **Identity ↔ OIDC provider.** Pre-committed per [ADR-006](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md). Visible in Story 1 steps 1–4 (Identity creation arc) as the upstream end of the actor lifecycle. Identity's own provider-side translation is not modeled here.
- **Onboarding ↔ document-verification vendor.** External ACL boundary. Vendor returns structured rejection reasons that Onboarding translates to a CritterCab-domain enum.
- **Onboarding ↔ background-check vendor.** External ACL boundary. Vendor uses its own actor name ("candidate") and status vocabulary ("Pending," "Clear," "Consider," "Suspended"); Onboarding translates at the boundary.
- **Onboarding → Driver Profile (approval terminal).** Cross-BC publication via ASB business event on the approval terminal only — *not* on the rejection terminal (per §5.2 finding B2).
- **Identity → other BCs (downstream).** Identity publishes `identity.driver-registered` per the [W002 §13 forward-constraint #2 pattern](002-trips-event-model.md) extended from the rider side. Downstream consumers of the driver-registered event are not modeled here.

### 2.3 Out of scope

- **Suspension / reinstatement / deactivation lifecycle.** The vision doc v0.1 places these under Onboarding's scope; W003 surfaced finding B6 (these may belong to Trust & Safety / Operations rather than Onboarding). The vetting lifecycle is exercised; the disqualification lifecycle is not. **Escalated to vision-doc v0.5 as an open question** rather than resolved in this session.
- **Re-application.** Whether and how a rejected applicant can re-apply (waiting period, re-vetting, fresh aggregate vs. amended aggregate) is deferred to W004.
- **Vehicle inspection.** Jurisdiction-conditional gating step omitted from Story 1 to keep the happy path clean.
- **Selfie / liveness-check identity matching.** A real identity-verification flow alongside document verification; explicitly not exercised in Story 2.
- **Vehicle as a sub-domain.** Multi-vehicle scenarios, per-vehicle approval lifecycle, vehicle-document renewal. Out of scope in all three stories.
- **Cross-jurisdiction movement.** Approved driver in city A wanting to drive in city B.
- **Expiry-driven re-vetting.** Document expiry (license renewal, insurance renewal) mid-driver-lifetime.
- **Vendor-side data correction.** Vendor revising a previously-Clear result after approval.
- **Operations BC's reviewer tooling.** The adjudicator is modeled as an actor; the tools they use are an Operations concern.
- **Code / events / commands / aggregate boundaries.** DS captures activities and vocabulary; event modeling (W004) commits to event names, command shapes, and aggregate boundaries.

### 2.4 Structural constraints honored

- All vendor boundaries (document-verification, background-check) and the OIDC provider boundary (referenced from Identity) are external actors outside any CritterCab BC lasso. No CritterCab BC absorbs vendor vocabulary or schemas. Consistent with [ADR-006](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md).
- The Onboarding → Driver Profile handoff at Story 1 step 22 / Story 2 step 28 is annotated as an ASB business event per [ADR-014](../decisions/014-asb-topic-naming-convention.md) topic naming convention. Specific topic name (`onboarding.driver-approved` vs. `onboarding.application-approved`) deferred to W004.
- No protobuf surface is sketched in this artifact. Proto-file authorship is a downstream task per [ADR-009](../decisions/009-protobuf-contracts-as-first-class-artifacts.md) and follows from W004.

### 2.5 Decisions locked during scope-setting and capture

| Decision | Resolution | Locked at |
|---|---|---|
| Story 1 start point | At interest signal (full happy path; Identity creation visible inside the story) | Phase 1 open |
| Actor lifecycle vocabulary | prospective driver → applicant → driver, with transitions at application begin and Driver Profile activation | Phase 1 open + Phase 4 R1/R2 |
| Document-state model on rejection-then-re-upload | Mutate (same document identity preserved through rejection cycle) | Story 2 Q2a.2 |
| Vendor-rejection notification | Translated reason (CritterCab-domain enum); not vendor's raw string | Story 2 Q2b.1 |
| Background-check failure shape (Story 3) | Vendor returns "Consider" → human adjudication → reject | Story 3 Q3a.1 |
| Adjudicator BC placement | Onboarding (pre-approval adjudication is part of the vetting lifecycle) | Story 3 Q3a.2 |
| FCRA two-phase rejection | Modeled explicitly (pre-adverse-action notice → dispute window → final adverse-action notice) | Story 3 Q3a.3 |
| Document-verification vendor visibility | External actor outside Onboarding lasso (parallel to ADR-006's stance toward OIDC) | Story 1 Q3.1 |
| Approval auto-flow | Auto-approves when all gates clear (no manual reviewer in happy path) | Story 1 Q5.1 |
| Methodology pilot decision | **Exercised** — DS committed as a permanent design-phase technique | Phase 6 |

---

## 3. Methodology and notation

### 3.1 Domain Storytelling — primer

Domain Storytelling (Hofer & Schwentner) is a workshop technique for surfacing language boundaries between actors in a domain *before* committing to event names, aggregate boundaries, or BC splits. Stories capture sequenced activities between actors and work objects, told from an external-observer viewpoint, using a small notation set: **actor**, **work object**, **activity** (numbered arrow with verb), **annotation**, **group** (lasso). The discipline complements Event Modeling by surfacing **what people say** before Event Modeling surfaces **what changes**.

This is the first DS exercise in CritterCab. The vision-doc v0.1 listed "Pilot Domain Storytelling" as a secondary methodology goal; the goal had been open for ~13 months before this session.

### 3.2 Notation conventions for this artifact

This being CritterCab's first DS artifact, the notation conventions are established here and may be referenced by future DS sessions:

**Step format:**

```
N. ACTOR — VERB (capitalized) — WORK_OBJECT → TARGET (optional)
   ↳ annotation line (multiple permitted)
```

**Lassos** are rendered as horizontal-rule-separated subsections inside each Story, named with the candidate CritterCab BC the activities-inside-the-lasso belong to (e.g., **━━ Onboarding lasso ━━**).

**External actors** (non-CritterCab BCs — vendors, regulatory frameworks, OIDC providers) appear with descriptive lowercase names (`document-verification vendor`, `background-check vendor`). They are not enclosed in lassos because they do not represent CritterCab BCs.

**Notation devices used:**

| Device | First introduced | Purpose |
|---|---|---|
| **Repeat-aggregation** | Story 1 step 13 | "Steps N–M repeat for {other work objects}" — compresses a captured pattern across multiple repetitions. Tradeoff: loses per-iteration detail; gains narrative focus. |
| **Time-passage** | Story 1 step 17 | "(time passes — async work continues)" — captures regulatorily-significant or domain-significant waiting periods without modeling them as actor activities. Tradeoff: not a canonical DS primitive; introduced to honor genuine domain asynchrony. |
| **Reference-block** | Story 2 step 20 | "Steps N–M: identical to Story K steps X–Y" — compresses arcs that recur across multiple stories. Tradeoff: introduces hidden dependency between stories (if Story K's referenced steps change, Story N's reference inherits the change). |

These three devices are CritterCab adaptations of canonical DS, justified by markdown's text-only nature (vs. physical board / digital diagram tooling) and by the absence of multiple participants who would otherwise demand fully-walked stories.

### 3.3 Solo + AI adaptation

DS is conventionally a multi-stakeholder workshop technique. CritterCab is solo (Erik) plus AI (Claude). The adaptation:

- **Erik plays the domain expert.** Informed by the Phase 0 research note ([`docs/research/ride-sharing-driver-onboarding-domain-note.md`](../research/ride-sharing-driver-onboarding-domain-note.md)) and general familiarity with consumer-platform onboarding patterns. Not pretending to be a field-research-grade insider — the goal is plausible-domain, not ethnographic-accuracy.
- **Claude facilitates and captures.** Asks DS-canonical questions (who acts? what work object passes? in what sequence? what's the actor's word for this thing?), captures each story as a numbered sequence with explicit actor / verb / work-object / target, and surfaces genuine choice-points as questions with leans.
- **Per-step batches with explicit-lean discipline.** Each capture batch ends with 1–3 focused questions, each accompanied by a leaning recommendation. The user signs off, redirects, or escalates. Avoids both the consultant pattern (Claude pre-authors entire stories for review) and the over-facilitated pattern (Claude asks open-ended questions with no proposed direction).
- **Per-phase sign-off, not per-step sign-off.** Sign-off discipline operates at the phase boundary; capture within a phase iterates freely.

The adaptation's success at avoiding pure-system-viewpoint drift is assessed in the retrospective (§7 link).

### 3.4 Phase 0 research note as a session prerequisite

The session began with a research-note authoring step ([`docs/research/ride-sharing-driver-onboarding-domain-note.md`](../research/ride-sharing-driver-onboarding-domain-note.md)) that grounded the DS session in industry-typical onboarding shape. The note is **non-normative** (lives in `docs/research/`, frozen at Phase 0 close) and provided:

- Industry-typical lifecycle stages (7 stages, ~§2.1 of the note)
- Vocabulary candidates with lifecycle moments (§3 of the note)
- Background-check vendor patterns including the "Consider" intermediate status (§4)
- Document-verification vendor patterns and the new-entity-vs-state-machine modeling question (§5)
- Boundary-candidate framing for Onboarding ↔ Identity / Driver Profile / T&S / Operations / vehicle sub-domain (§6)
- Edge cases worth probing (§7)

The note's pattern (Phase-0 research as a session prerequisite) is itself flagged for retrospective evaluation — see §7 link for whether it survives as a reusable pattern for future methodology-piloting sessions.

---

## 4. Stories

Three stories — happy path, document-rejection recoverable path, background-check terminal-rejection path. Each told from external-observer viewpoint with pure granularity (one path per story; no mid-story branching) and to-be purity (greenfield CritterCab; no as-is system to depict).

### 4.1 Story 1 — Happy path

**Trigger:** A person decides to become a CritterCab driver.

**━━ Identity lasso ━━**

```
 1. prospective driver  SUBMITS contact info        → Identity
       ↳ email, phone, city; driver-signup landing page

 2. Identity            SENDS verification email    → prospective driver
       ↳ magic link with embedded token

 3. prospective driver  CLICKS verification link    → Identity

 4. Identity            CREATES Identity account    FOR prospective driver
       ↳ OIDC subject exists; account authenticated
       ↳ Identity PUBLISHES identity.driver-registered (per W002 §13 pattern)
```

**━━ Onboarding lasso ━━**

```
 5. prospective driver  BEGINS application          → Onboarding
       ↳ explicit "Start application" click on driver dashboard
       ↳ application aggregate comes into existence
       ↳ ⟦ vocabulary transition: prospective driver → applicant ⟧

 6. applicant           SUBMITS personal info       → Onboarding
       ↳ legal name, DOB, address, city of operation

 7. applicant           SUBMITS SSN + BG-check consent → Onboarding
       ↳ FCRA-compliant signed authorization

 8. Onboarding          RECORDS personal-info section as complete

 9. applicant           UPLOADS driver's license    → Onboarding

10. Onboarding          SUBMITS driver's license    → document-verification vendor
       ↳ external actor; ACL pattern parallel to Identity ↔ OIDC (ADR-006)

11. document-verification vendor  RETURNS "verified, authentic"  → Onboarding
       ↳ document authentic; photo matches stated identity

12. Onboarding          RECORDS driver's license as accepted

13. (steps 9–12 repeat for vehicle registration and insurance certificate;
     all three documents return "verified, authentic")

14. Onboarding          RECORDS documents section as complete

15. Onboarding          REQUESTS background check   → background-check vendor
       ↳ SSN + DOB + name under the step-7 authorization
       ↳ ⟦ vocabulary annotation: vendor labels the actor "candidate"
          — homonym flagged for Phase 4 ⟧
       ↳ fires now because both consent (step 7) and documents-complete
          (step 14) prerequisites are met

16. background-check vendor  RETURNS case ID + "Pending" → Onboarding
       ↳ synchronous ack; actual checks run async

17. (time passes — vendor performs SSN trace, criminal-history search,
     sex-offender registry check, MVR pull)

18. background-check vendor  PUSHES "Clear" status  → Onboarding
       ↳ webhook delivery
       ↳ Clear = no disqualifying findings; no manual review needed

19. Onboarding          RECORDS background-check section as complete
       ↳ all three sections now complete

20. Onboarding          APPROVES application
       ↳ auto-approves: all gates clear, no human reviewer needed
       ↳ application transitions to "approved" terminal state

21. Onboarding          NOTIFIES applicant OF approval
       ↳ email + push: "You're approved to drive with CritterCab!"

22. Onboarding          PUBLISHES approval event    → Driver Profile
       ↳ business event over ASB per ADR-014
       ↳ candidate topic name: onboarding.driver-approved
          (or onboarding.application-approved — W004 to lock)
       ↳ ⟦ BC handoff: Onboarding's terminal → Driver Profile's intake ⟧
```

**━━ Driver Profile lasso ━━**

```
23. Driver Profile      ACTIVATES driver
       ↳ Driver Profile aggregate comes into existence (or transitions to active);
          W004 to lock when DP comes into existence per research note §6.2
       ↳ vehicles attached during application carry over
       ↳ ⟦ vocabulary transition: applicant → driver ⟧
       ↳ driver is now addressable for dispatch
```

**[Story 1 ends — driver is active and dispatchable.]**

---

### 4.2 Story 2 — Document-rejection (recoverable) path

**Trigger:** A person decides to become a CritterCab driver. Their first driver's license upload fails image-quality verification; they re-upload successfully and the application proceeds to approval.

**Steps 1–8: identical to Story 1.** Identity creation, application start, personal info, consent, section-complete tick.

**━━ Onboarding lasso (divergence at step 9) ━━**

```
 9. applicant           UPLOADS driver's license    → Onboarding
       ↳ first attempt; photo quality is poor (blurry / poor lighting)

10. Onboarding          SUBMITS driver's license    → document-verification vendor

11. document-verification vendor  RETURNS "rejected — image quality"  → Onboarding
       ↳ structured reason code; vendor-side automated rejection
       ↳ no manual review requested by vendor

12. Onboarding          RECORDS driver's license as rejected
       ↳ per-document state transition: under-review → rejected
       ↳ ⟦ document identity preserved through rejection cycle ⟧
       ↳ application is NOT terminal — applicant can re-upload

13. Onboarding          NOTIFIES applicant OF license rejection
       ↳ email + dashboard banner
       ↳ includes translated reason: "Photo too blurry — try again with
          better lighting"
       ↳ vocabulary note: applicant remains "applicant"
          (the document is rejected, not the applicant)

14. applicant           RE-UPLOADS driver's license → Onboarding
       ↳ second attempt
       ↳ same document identity preserved; state transitions
          rejected → under-review

15. Onboarding          SUBMITS driver's license (rev 2) → document-verification vendor

16. document-verification vendor  RETURNS "verified, authentic"  → Onboarding
       ↳ re-upload clears

17. Onboarding          RECORDS driver's license as accepted
       ↳ per-document state: under-review → accepted
       ↳ rejected state persists in document history (W004 to lock storage model)

18. (steps 9–12 from Story 1 repeat for vehicle registration and
     insurance certificate; both accepted on first attempt)

19. Onboarding          RECORDS documents section as complete
       ↳ all three documents in accepted state
```

**Steps 20–28: reference-block to Story 1.**

```
20–28. Balance of story: identical to Story 1 steps 15–23.
       ↳ BG check requested → returns Pending → returns Clear via webhook
       ↳ Onboarding records BG section complete → auto-approves application
       ↳ Onboarding notifies applicant → publishes onboarding.driver-approved → DP
       ↳ Driver Profile activates driver
       ↳ ⟦ vocabulary transition: applicant → driver at step 28 ⟧
```

**[Story 2 ends — driver is active and dispatchable, with one rejected document persisting in history.]**

---

### 4.3 Story 3 — Background-check failure (terminal rejection)

**Trigger:** A person decides to become a CritterCab driver. Their application proceeds cleanly through document verification, but the background-check vendor returns a "Consider" status. The Onboarding adjudicator reviews and rejects. The FCRA two-phase adverse-action notice runs to completion. The application terminates as rejected.

**Steps 1–17: identical to Story 1.** Identity creation, application, documents accepted, BG check requested, vendor returns Pending, time passes.

**━━ Onboarding lasso (divergence at step 18) ━━**

```
18. background-check vendor  PUSHES "Consider" status  → Onboarding
       ↳ vendor identifies findings requiring human judgment
       ↳ example findings: minor traffic violation, ambiguous SSN-name match,
          county-record near-miss
       ↳ vendor vocabulary: "Consider" → CritterCab vocabulary: "manual
          review required" (ACL translation per Story 2 step 11 precedent)

19. Onboarding          RECORDS BG-check status as "manual review required"
       ↳ application is NOT yet rejected; awaits adjudication

20. Onboarding          QUEUES adjudication case        → Onboarding adjudicator
       ↳ NEW work-object: adjudication case
       ↳ adjudication case lifecycle is separate from the application's
       ↳ references the application + vendor findings

21. Onboarding adjudicator  REVIEWS adjudication case
       ↳ first manual-actor activity across all three stories
       ↳ reviewer inspects vendor's findings + applicant's application
          data + relevant policy

22. Onboarding adjudicator  ADJUDICATES adjudication case  → reject
       ↳ decision: BG-check findings disqualifying per platform policy

23. Onboarding          CLOSES adjudication case
       ↳ outcome: reject
       ↳ triggers application's rejection arc (steps 24–28)
       ↳ application is NOT yet rejected — FCRA two-phase machinery runs first

24. Onboarding          ISSUES pre-adverse-action notice  → applicant
       ↳ FCRA-required: includes the BG-check report itself
       ↳ starts the dispute window (typically 5–7 business days)

25. (time passes — dispute window open; applicant may dispute through vendor
     or provide additional info; happy-ish path: applicant does nothing)

26. applicant           DOES NOT DISPUTE within the window
       ↳ alternative paths (dispute filed; new info provided) — out of scope
       ↳ regulatorily-significant non-action; permits step 27 to fire

27. Onboarding          ISSUES final adverse-action notice  → applicant
       ↳ FCRA-required final rejection notification

28. Onboarding          RECORDS application as rejected  (terminal)
       ↳ application aggregate enters terminal-rejected state
       ↳ aggregate persists for historical record (no cleanup modeled)
       ↳ Identity account persists unchanged — actor can still log in as
          rider; rejection is an Onboarding concern, not an Identity concern
       ↳ NO outbound publication to Driver Profile (never became a driver)
       ↳ NO outbound publication to T&S / Operations modeled — speculative
          consumer relationships for W004
       ↳ vocabulary: applicant remains "applicant" — no transition
```

**[Story 3 ends — application terminally rejected; Identity persists; actor remains "applicant"; no BC handoff out of Onboarding.]**

---

### 4.4 Cross-story metrics

| Dimension | Story 1 | Story 2 | Story 3 |
|---|---|---|---|
| Total numbered steps | 23 | 28 | 28 |
| Explicitly walked steps | 23 | 19 (1–8 shared; 9–19 new) | 11 (1–17 shared; 18–28 new) |
| CritterCab BC lassos touched | 3 (Identity, Onboarding, Driver Profile) | 3 | **2** (Identity, Onboarding — no DP) |
| External actors visible | 2 (doc-verification, BG-check vendors) | 2 | 2 |
| Manual-human-reviewer actors | 0 | 0 | 1 (Onboarding adjudicator) |
| New work-object types introduced beyond Story 1 | — | 0 (re-uses document) | 1 (adjudication case) |
| Vocabulary transitions | 2 (prospective driver → applicant → driver) | 2 | 1 (no applicant → driver) |
| BC handoffs | 2 (Identity → Onboarding; Onboarding → DP) | 2 | 1 (Identity → Onboarding only) |
| Notation devices introduced | 2 (repeat-aggregation step 13; time-passage step 17) | 1 (reference-block steps 20–28) | 0 |

---

## 5. Findings

### 5.1 Vocabulary findings

**Resolved in-session:**

- The human actor's lifecycle is **3 vocabulary states with 2 transitions**: *prospective driver* → *applicant* (at application begin, step 5 of all stories) → *driver* (at Driver Profile activation, Stories 1–2). Pre-approval "applicant" persists through terminal rejection — no "rejected applicant" sub-state is needed in vocabulary; the application's terminal-rejected state carries the information.
- **"Profile" is deliberately avoided** for the application's biographical data; "personal info" used instead. Identity → "Identity account"; "Driver Profile" applies only to the post-activation BC aggregate. Avoidance is intentional and worth preserving in W004 to prevent the homonym from leaking back in.
- **"Driver" is reserved for post-activation actors.** Industry alternatives ("partner," "vetted driver," "driver-partner") rejected for CritterCab use.

**Cross-cutting pattern surfaced (flag for naming as a CritterCab convention):**

**Onboarding (with Identity) is a multi-vendor ACL aggregator.** Three vendor boundaries surfaced inside or adjacent to the Onboarding BC, all following the same ACL-translation pattern committed in [ADR-006](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) for the OIDC provider: vendor vocabulary translates at the boundary into CritterCab-domain vocabulary. The pattern repeats at:

- Identity ↔ OIDC provider (committed by ADR-006)
- Onboarding ↔ document-verification vendor (Story 2 step 11)
- Onboarding ↔ background-check vendor (Story 1 step 15; intensified in Story 3 step 18)

Three instances of the same pattern across one BC family is enough repetition to consider naming the convention explicitly — candidate name: "vendor-ACL boundary," or as a generalization of ADR-006 with a successor ADR.

**Flagged for Workshop 004 (vocabulary work that belongs in event modeling):**

| # | Concern | Pre-recommendation |
|---|---|---|
| V1 | Cross-BC approval event topic name | `onboarding.driver-approved` or `onboarding.application-approved` — W004 picks |
| V2 | "Documents section" vs. "documents" vs. "required document collection" | Stories used "documents section"; neutral |
| V3 | "Adjudication case" naming | Stories used "adjudication case"; confirm against future Operations BC vocabulary |
| V4 | Three "approved" moments need distinct names | Distinct event names per moment proposed: `ApplicationApproved` / `ApplicantNotifiedOfApproval` / `DriverActivated` |
| V5 | "Case" homonym (BG-check vendor case vs. adjudication case) | Recommend prefix: "vendor case" vs. "adjudication case" |
| V6 | "Review" / "under-review" / "REVIEWS" three-way collision | Recommend rename document state to **"under-verification"** to break the homonym; verb REVIEWS stays for human-actor action |
| V7 | "Rejected" — per-document vs. application-terminal | Recommend distinguishing as **"document-rejected"** and **"application-rejected"** in W004 event naming |
| V8 | Vendor reason-code normalization enum | Translated reasons (e.g., `INSUFFICIENT_QUALITY`) need a canonical enum |

**Activity-verb taxonomy (positive coherence signal):**

Total verbs introduced across the three stories: **20** — cluster cleanly into 6 categories with no leftover:

| Category | Verbs |
|---|---|
| Information transfer | SUBMITS, SENDS, RETURNS, PUSHES, ISSUES, NOTIFIES, PUBLISHES |
| State commits | CREATES, RECORDS, APPROVES, ACTIVATES, CLOSES |
| User actions | CLICKS, BEGINS, UPLOADS, RE-UPLOADS |
| Reviewer actions | QUEUES, REVIEWS, ADJUDICATES |
| Workflow signals | REQUESTS |
| Non-actions | DOES NOT DISPUTE |

### 5.2 BC-boundary findings

The three vision-doc-named BCs (Identity, Onboarding, Driver Profile) emerged as natural lassos in the stories. **The split survives empirical testing through DS.**

**B1. Identity → Onboarding handoff is structurally consistent across all three stories.** Steps 1–4 (Identity creation) → step 5 (Onboarding application begin). The handoff is gated by Identity's account-creation terminal. ADR-006's ACL stance for the OIDC provider holds; `identity.driver-registered` (per W002 §13 pattern for the rider side, extended here for the driver side) is the cross-BC signal.

**B2. Onboarding → Driver Profile handoff is gated by the *approval* terminal, not by Onboarding-the-BC.** Stories 1 and 2 cross into Driver Profile at step 23 / step 28; Story 3 never reaches Driver Profile despite Onboarding's substantial work through FCRA rejection. **Asymmetry:** the cross-BC publication exists for the approval terminal; *no equivalent cross-BC publication exists for the rejection terminal in the stories as captured*. Whether one should exist (notification to T&S, Operations, etc.) is W004's question.

This finding is actionable enough to **amend the context-map's edge #6 prose in the same PR as this session** (per the [context-map's update cadence](../context-map/README.md#update-cadence)).

**B3. Driver Profile lifecycle start is unresolved by DS.** Stories 1 and 2 modeled Driver Profile as activating at the handoff event (step 23). Whether Driver Profile *comes into existence at application start* (placeholder, vehicles accumulated during onboarding), *at approval* (push from Onboarding event), or *lazily* (on first driver action) — the three orderings named in research note §6.2 — remains a W004 question. The stories committed to "DP is downstream of approval"; they did not commit to *when* DP exists.

**B4. Vendor boundaries are first-class actors, not absorbed inside CritterCab BCs.** Both the document-verification vendor and background-check vendor sit outside any CritterCab lasso. Onboarding ACLs both. Combined with Identity ACL'ing the OIDC provider, CritterCab now has **three ACL boundaries on vendor relationships in this BC family** — enough pattern-repetition to consider naming (per the §5.1 cross-cutting pattern).

**B5. The Onboarding adjudicator is the first manual-human actor in any CritterCab workshop.** Adjudicator vocabulary established as Onboarding-internal (not Trust & Safety, not Operations). The placement is consistent with research note §6.3's distinction (pre-approval adjudication = Onboarding; post-approval suspension = T&S / Operations) and survives Phase 5 review.

**B6. No Trust & Safety lasso emerged.** T&S is a candidate BC per [W002 §10 fan-out tables](002-trips-event-model.md) and the [context-map's §Pending workshops](../context-map/README.md#pending-workshops), but Stories 1–3 — which cover only the vetting lifecycle — gave T&S no role. The vision-doc claim that Onboarding owns "application, document upload, background check, approval, suspension, reinstatement" was *not exercised* for the suspension/reinstatement portion; those concerns are not modeled in any DS story. **Escalated to vision-doc v0.5 as an open question** rather than resolved in this session.

**B7. The "vehicle as sub-domain" question** (research note §6.5) did not surface in the stories. Vehicle registration was treated as a single document type alongside license and insurance. Whether vehicle-approval has its own lifecycle (multi-vehicle, per-vehicle approval, etc.) was not exercised. W004 should consider — or push to a separate vehicle workshop / BC consideration.

**Context-map impact:**

| Edge | Pattern before DS | Pattern after DS | Action |
|---|---|---|---|
| Identity → Onboarding (edge #3 outbound) | Presumed PL pending workshop | Presumed PL still pending workshop; structural shape of handoff confirmed at Story 1 step 4 | **No change to edge label.** W004 still locks. |
| Onboarding → Driver Profile (edge #6) | Expected CS, pending both workshops | Expected CS confirmed; gated on approval terminal specifically (B2) | **Amend edge #6 prose in this PR.** Adds the approval-terminal-gating note. |
| Trust & Safety relationships | Speculative candidate BC | Not exercised; vision-doc claim untested (B6) | **No change to context map.** T&S edge remains speculative. |

### 5.3 Open questions for Workshop 004

Grouped by type.

**Event-modeling concerns (clear W004 scope):**

- **OQ-1.** Topic name for cross-BC approval event: `onboarding.driver-approved` vs. `onboarding.application-approved`.
- **OQ-2.** Document-state storage model: same aggregate as application (state machine on document IDs) vs. separate document aggregates referencing the application.
- **OQ-3.** Document history persistence: per the mutate model (Story 2 Q2a.2 resolved), how is the rejected-then-accepted history recorded?
- **OQ-4.** Adjudication-case storage: separate aggregate referencing the application, or sub-stream of the application?
- **OQ-5.** Pre-adverse-action vs. final-adverse-action notice events — two distinct events, or single event with phase indicator?
- **OQ-6.** Window-expiry firing the final adverse-action notice — Bruun-style temporal automation pattern (per [W001 §5.7](001-dispatch-event-model.md) precedent)?
- **OQ-7.** Three "approved" moments — distinct event names per V4 recommendation?
- **OQ-8.** Document state "under-review" rename to "under-verification" per V6 recommendation?
- **OQ-9.** Vendor reason-code normalization enum — define canonical CritterCab vocabulary.

**BC-boundary concerns (W004 or future strategic-design work):**

- **OQ-10.** Rejection outbound publications: does Onboarding publish to T&S, Operations, or anywhere else on terminal rejection (per B2)?
- **OQ-11.** Re-application policy: can a rejected applicant re-apply? Under what waiting period or re-vetting requirements?
- **OQ-12.** Driver Profile creation timing: application-start (placeholder) / approval (push) / lazy (on first driver action) — per B3 / research note §6.2?
- **OQ-13.** Vehicle as sub-domain: vehicle-approval lifecycle inside Onboarding, inside Driver Profile, or its own BC concern (per B7 / research note §6.5)?
- **OQ-14.** Suspension / reinstatement / deactivation lifecycle ownership: Onboarding (per vision-doc claim) or T&S / Operations (per research note §6.3 alternative)? **Escalated to vision-doc v0.5** as a vision-doc-level open question rather than a W004 question.

**Out-of-scope for both DS and W004 (defer to future sessions):**

- **OQ-15.** Vehicle inspection (Story 1 Q3.2 skipped): when introduced, what BC owns the inspection lifecycle?
- **OQ-16.** Selfie / liveness-check identity matching (Story 2 Q2a.1 path-not-taken): an identity-verification flow separate from document verification.
- **OQ-17.** Cross-jurisdiction movement (research note §7): approved driver in city A wants to drive in city B.
- **OQ-18.** Document expiry-driven re-vetting (research note §7): license renewal mid-driver-lifetime; ownership question (Onboarding still in the lifecycle vs. Driver Profile vs. dedicated Documents BC).
- **OQ-19.** Vendor-side data correction (research note §7): vendor revises a previously-Clear result months after approval.

---

## 6. Cross-references

| Artifact | Relationship |
|---|---|
| [`docs/prompts/workshops/003-onboarding-domain-storytelling.md`](../prompts/workshops/003-onboarding-domain-storytelling.md) | Triggering prompt |
| [`docs/retrospectives/workshops/003-onboarding-domain-storytelling.md`](../retrospectives/workshops/003-onboarding-domain-storytelling.md) | Retrospective (includes Solo-DS-adaptation methodology assessment) |
| [`docs/research/ride-sharing-driver-onboarding-domain-note.md`](../research/ride-sharing-driver-onboarding-domain-note.md) | Phase 0 grounding material (non-normative, frozen at session start) |
| [`docs/research/domain-discovery-playbook.md`](../research/domain-discovery-playbook.md) | Nick Tune facilitation vocabulary; Part 3 (Make Scale Explicit) and Part 4 (Diverse Question Formats) referenced during capture |
| [`docs/vision/README.md`](../vision/README.md) v0.5 | Methodology pilot decision (Exercised); new open question (OQ-14 suspension lifecycle ownership) |
| [`docs/context-map/README.md`](../context-map/README.md) | Edge #6 (Onboarding → Driver Profile) prose amendment per B2 |
| [`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) | Pre-committed ACL pattern; precedent for the vendor-ACL pattern surfaced as a §5.1 cross-cutting finding |
| [`docs/decisions/014-asb-topic-naming-convention.md`](../decisions/014-asb-topic-naming-convention.md) | Topic-naming convention for the cross-BC approval event (V1 / OQ-1) |
| [`docs/workshops/001-dispatch-event-model.md`](001-dispatch-event-model.md) | Cross-referenced for the Bruun-style temporal-automation pattern (OQ-6) and the established workshop voice |
| [`docs/workshops/002-trips-event-model.md`](002-trips-event-model.md) | Cross-referenced for the `identity.*` outbound pattern (B1) and for the candidate-BC inventory drift (B6 references T&S as candidate BC) |

---

## 7. Retrospective

The retrospective lives at [`docs/retrospectives/workshops/003-onboarding-domain-storytelling.md`](../retrospectives/workshops/003-onboarding-domain-storytelling.md) per the prompt's deliverable plan and per the [retrospectives README](../retrospectives/README.md#subdirectory-layout) subdirectory convention.

The retro addresses six methodology questions seeded by the prompt's [§Methodology questions to resolve in the retro](../prompts/workshops/003-onboarding-domain-storytelling.md#methodology-questions-to-resolve-in-the-retro):

1. Did the solo + AI adaptation produce usable output, or did it collapse into pure-system-viewpoint drift?
2. Did the three-story format surface vocabulary divergence the user had not seen?
3. Did the BC-boundary findings warrant a context-map amendment?
4. Should DS notation conventions be codified in a skill file?
5. Does the technique generalize to Operations or is it onboarding-specific?
6. Does the research-note-as-Phase-0 pattern survive?

The retro also includes the standard CritterCab format conventions per the [retrospectives README](../retrospectives/README.md#format-conventions-inside-a-retro-file): metadata block, framing, outcome summary, what worked, what was harder than expected, methodology refinements, outstanding items / next-session inputs, spec delta — landed?, quantitative summary.

---

## 8. Document history

- **v0.1** (2026-05-26): Foundation Domain Storytelling artifact authored from the [W003 prompt](../prompts/workshops/003-onboarding-domain-storytelling.md). Three stories captured (happy path, document-rejection recoverable, background-check terminal-rejection); vocabulary disambiguation pass producing 8 W004-flagged items plus 1 cross-cutting pattern; BC-boundary findings producing 7 named findings plus 1 context-map amendment recommendation (edge #6 prose); 19 open questions queued for W004. Methodology decision: **Exercised** — DS committed as a permanent design-phase technique per [`docs/vision/README.md`](../vision/README.md) v0.5. First DS artifact in CritterCab; establishes notation conventions (markdown sequence; repeat-aggregation, time-passage, and reference-block notation devices) for future DS sessions.
