# Retrospective — Workshop 005 Identity Event Model

| Field | Value |
|---|---|
| **Triggering prompt** | [`docs/prompts/workshops/005-identity-event-modeling.md`](../../prompts/workshops/005-identity-event-modeling.md) (grill-with-docs refined; 7-resolution history in the prompt) |
| **Status** | Complete |
| **Date** | 2026-05-28 |
| **Output artifacts** | [`docs/workshops/005-identity-event-model.md`](../../workshops/005-identity-event-model.md) (new — first Identity event model; first in-repo ACL-translation-dominant reference); this retro (new — third entry in `retrospectives/workshops/`); [`docs/research/methodology-log.md`](../../research/methodology-log.md) (entry 006 — two-axis distinction); [`docs/workshops/README.md`](../../workshops/README.md) (W005 row + Identity forward-constraint follow-up closures); [`docs/retrospectives/README.md`](../README.md) (index entry); [`docs/context-map/README.md`](../../context-map/README.md) (edge #3 supplier-side lock) |
| **One-line outcome** | The thinnest CritterCab workshop — 4 slices, 3 events, 3 projections — establishing **ACL-translation-dominant as CritterCab's third modeling shape**, with the grill-with-docs pass having front-loaded every design call so the walk was near-frictionless. |

---

## Framing

W005 is CritterCab's fourth Event Modeling workshop and the first for a BC whose primary domain job is being an anti-corruption layer (ADR-006). Two firsts: it is the **first workshop pulled into existence by accumulated forward-constraints** converging from three prior workshops (W002, W003, W004) rather than a single "next workshop" pointer, and it establishes the **ACL-translation-dominant modeling shape** — CritterCab's third, alongside aggregate-per-invariant (W001/W002) and Process Manager via Handlers (W004).

It was run **EM-direct** (no Domain Storytelling pre-step), and a **grill-with-docs pass (7 resolutions) preceded the workshop** — sharpening the prompt's leans into locked decisions so the workshop session itself was largely transcription plus a thin slice walk.

---

## Outcome summary

- **One workshop event model** (`005-identity-event-model.md`, v0.5) — §2 Scope (Option A), §3 ACL-translation-dominant sidebar, §4 UL, §5 Event List (3 events), §6 Slice Walk (4 slices), §10 Protobuf surface, §X Forward-constraints handled, §X+1 Forward-constraints generated, §11 ADR Candidates, §12 Retrospective.
- **First in-repo ACL-translation-dominant reference.** No load-bearing invariant in scope; thin `IdentityAccount` stream for record-keeping/projection, not invariant protection. CritterCab's third modeling shape.
- **All three accumulated forward-constraints dispositioned:** `identity.rider-registered` honored; `identity.rider-profile-updated` **refined/overridden** (reassigned to Rider Profile — grill #1); `identity.driver-registered` honored per W004's exact contract.
- **W004 Onboarding-intake gap closed** with no W004 edit (grills #3/#4 — role-registration-fact semantics make `identity.driver-registered` fire for rider-becomes-driver too).
- **`subjectId` ADR-006 compliance made explicit** (grill #6) — Identity-minted domain ID, provider-`sub` mapping inside Identity, never published.
- **Methodology log entry 006 written** — the two-axis distinction (DS surfaces vocabulary; grill-with-docs surfaces boundaries).

---

## What worked

- **Grill-with-docs as a pre-workshop prompt-sharpening pass produced a near-frictionless walk.** Seven grills resolved scope, the modeling shape, and every boundary question before the session ran. The workshop was transcription + a thin slice walk with zero mid-walk design debates — the strongest evidence yet that grilling a prompt up front pays off for boundary-nuanced BCs.
- **The ACL-translation-dominant shape held honestly.** Stress-testing for a load-bearing invariant (ADR-012's lens) found none in scope, and the workshop committed to translation-dominant rather than forcing a thin aggregate. The thinness was honest, not under-specification.
- **The 6.2/6.3 contrast (net-new `StartStream` vs. append-to-existing)** is a clean teaching artifact for the role-registration-fact pattern and the one-account-per-human correlation — and it's what closes the W004 intake gap.
- **`subjectId` ADR-006 compliance caught and documented** (grill #6) — a latent issue that had propagated implicitly through W003/W004 (was `subjectId` the raw OIDC `sub`?) is now explicitly resolved as Identity-minted-domain.
- **The opposite-direction ADR-012 reframe.** W004 found Onboarding *too coordination-heavy* for ADR-012; W005 found Identity *too thin*. Two BCs, opposite directions, same conclusion: ADR-012 is one pattern among several. That symmetry strengthens the modeling-shape taxonomy.

---

## What was harder than expected

- **Almost nothing — and that is the finding.** The grill did the hard work; the workshop was light. The session's value was concentrated in the grill, not the walk. This is itself a methodology data point (see entry 006).
- **The one in-workshop framing the grill didn't pre-cover** — the provider-lifecycle-event inbound per ADR-006 (§3.4), which refines W003's DS-level "Identity SENDS verification email" simplification — fell out cleanly from ADR-006 without friction. Worth noting that ADR-006 being locked did real work here.
- **`ProviderSubjectIndex` as a silent inline dependency** is the W005 analog of W004's `BackgroundCheckVendorCaseIndex` — a *recurring* translation-dominant-BC pattern (an inline foreign-key index to route/idempotency-check inbound signals keyed by a foreign ID rather than the domain key). Worth carrying forward as a named expectation for future ACL BCs.

---

## Methodology refinements that emerged

- **ACL-translation-dominant is CritterCab's third modeling shape.** The discriminator is ADR-012's own question: *is there a load-bearing invariant?* None → translation-dominant (Identity); one fitting a single stream → aggregate-per-invariant (W001/W002); coordination-dominated → Process Manager (W004). This three-way taxonomy is the durable output.
- **Translation-dominant BCs need an inline foreign-key index** (`ProviderSubjectIndex` / `BackgroundCheckVendorCaseIndex`) to route and idempotency-check inbound signals keyed by a foreign ID. A named, recurring pattern.
- **The two-axis methodology distinction (the headline refinement, → entry 006):** DS-vs-EM-direct is about *vocabulary richness*; *boundary nuance* is a separate axis handled by grill-with-docs regardless of whether DS ran. Identity validated entry 005's DS-scoping precisely *because* its nuances were boundary calls the grill caught, not vocabulary the walk would have stumbled on.
- **Grill-with-docs before a workshop** (not just before implementation) is a strong move for vocabulary-thin-but-boundary-nuanced BCs.

---

## Outstanding items / next-session inputs

- **ACL-as-BC ADR candidate** — trigger: first Identity implementation slice. The "not currently" caveat (account-linking/merging could surface an invariant) is an input.
- **3 forward-constraints generated** (§X+1): Rider Profile must publish a profile-updated event (the reassigned `rider-profile-updated`); Rider/Driver Profile consume the `identity.*-registered` events; Identity proto authorship (`RiderRegistered`, `DriverRegistered`).
- **Identity business-event Protobuf authorship** — candidate bundled-proto session (PR #4 precedent); W004 §X+1 #3 already named `DriverRegistered`.
- **The `subjectId` ADR-006-compliance clarification** is now explicit for any future implementation touching W003/W004/W005 — `subjectId` is Identity-minted-domain, never the raw `sub`.

---

## Spec delta — landed?

**Yes — landed as named, with the predicted refinement.** The prompt's spec delta named the session spec-creating for Identity, spec-confirming for the three forward-constraints, and possibly pattern-introducing.

| Prompt prediction | Landed? | Notes |
|---|---|---|
| `005-identity-event-model.md` created | ✅ | First Identity event model; first ACL-translation-dominant reference. |
| Three forward-constraints honored/refined in §X | ✅ | 2 honored, 1 refined/reassigned (rider-profile-updated → Rider Profile). |
| ADR-006 first full exercise; ADR-014 fourth instance; ADR-013 nuance | ✅ | All as predicted. |
| ACL-as-BC pattern candidate | ✅ — **firm, not conditional** | Grill #2 de-conditionalized it: Identity has no load-bearing invariant, so the pattern is real. |
| No prior-workshop edits | ✅ | W002's `rider-profile-updated` constraint refined via documented override, not by editing W002. |

The grill-with-docs pass (committed before the workshop ran) is itself part of the spec-delta closure — the prompt's leans were sharpened into locked decisions, then the workshop exercised them. **Context-map edge #3 amended in the same PR** (supplier-side lock) per the §Update cadence — third exercise of the convention.

---

## Quantitative summary

| Metric | Count |
|---|---|
| Slices walked | 4 (6.1 + 6.2/6.3 paired + 6.4) |
| Events | 3 (`EmailVerified`, `RiderRegistered`, `DriverRegistered`) |
| Projections | 3 (`ProviderSubjectIndex` + `RegisteredActorsBySubject` inline; `IdentityAuditTimeline` async) |
| Outbound publications | 2 (`identity.rider-registered`, `identity.driver-registered`) |
| Forward-constraints handled | 3 (2 honored, 1 refined/reassigned) |
| Forward-constraints generated | 3 |
| Grill-with-docs resolutions (pre-workshop) | 7 |
| New ADR candidates | 1 (ACL-as-BC / translation-dominant — third modeling shape) |
| Locked ADRs touched | ADR-006 (first exercise), ADR-014 (fourth instance), ADR-013 (nuance), ADR-012 (not-a-new-point) |
| Protobuf contracts named (deferred) | 2 outbound |
| Methodology log entries written | 1 (entry 006 — two-axis distinction) |
| New feedback memories warranted | 0 |
| Workshop artifact final version | v0.5 |
| Relative size | Thinnest CritterCab workshop to date |
