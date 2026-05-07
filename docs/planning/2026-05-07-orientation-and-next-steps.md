# Orientation and Next Steps — 2026-05-07

> **Author:** Claude (Opus 4.7), at Erik's request after a week-long break.
> **Purpose:** Capture the post-vacation orientation conversation so it can be re-read fresh and acted on. Not a formal artifact layer — this is a session-handoff note. Move, fold, or delete once acted upon.

---

## Morning quick-start

Today's lean: **D → B → C, then either A or E** (defined below). If you only re-read one thing, re-read the lean section at the bottom.

If you want to start moving immediately:

1. Author `docs/prompts/decisions/001-protobuf-ride-assigned.md` (Candidate D) — smallest, lowest-cost commitment, opens `/protos/`.
2. Then `docs/prompts/implementations/001-dispatch-service-skeleton.md` (Candidate B) — first runnable code, no domain logic yet.
3. Then `docs/prompts/implementations/002-dispatch-slice-5-1-ride-requested.md` (Candidate C) — first real slice end-to-end.

---

## Where you actually are

### Design artifacts produced

- **Workshops: 1 of ~6–8** — Dispatch (complete, v0.2, 2026-04-24).
- **Narratives: 2** — both Dispatch, both happy path.
  - 001 = rider POV (slices 5.1, 5.2, 5.3, 5.4, 5.5, 5.10).
  - 002 = driver POV (5.4, 5.5, 5.10).
- **Skills: 39** across 5 phases. Closed 2026-05-06. Phase 5 was a reconciliation pass against `ai-skills`, not new authoring. The 14 skills tagged for Phase 6 placeholder cleanup are not blockers.
- **ADRs: 10** committed. **8 *candidate* ADRs** surfaced by workshop 001 are still open with explicit triggers — most fire on "first implementation session" or "second BC."
- **Research notes**, including Dilger's Spec-Driven Development article (the closest published cousin to your workflow) and `docs/research/agents-in-event-models.md` (Klefter / Bruun pattern extensions).
- **Methodology log: 3 entries** (001 about convention diversification across artifact layers; 002 about C/I/R weighting tracking commander/recipient; 003 about two-layer fidelity and forward-constraints on un-modeled BCs).

### Design artifacts NOT produced

- **No `docs/prompts/implementations/` directory yet** — no implementation prompt has ever fired.
- **No retrospectives outside the skills phases** — narrative and workshop sessions have inline retros; no implementation retros exist.
- **No `/protos/` directory yet** — ADR-009 not yet exercised in concrete form.
- **No Domain Storytelling or Context Mapping artifacts as standalone workshop outputs.** ADR-004 placed those as prerequisites; you skipped them for Dispatch and it worked, but they remain on the table for future BCs.

---

## Insight

`★ Insight ─────────────────────────────────────`
- ADR-004 frames steps 1–3 (Context Map → Domain Storytelling → Event Model) as **a one-time pre-code phase**, then steps 4–6 (Narrative → Prompt → Implementation+Retro) as **a per-slice loop**. You're stalled at the boundary between those two phases.
- Workshop 001 §12.8 named *two* explicit follow-ups: (a) the Trips workshop, and (b) a Protobuf authorship session for `dispatch/v1/ride_assigned.proto`. Both have been waiting since 2026-04-24.
- Narrative 002 left an authorial-call **forward-constraint** for Trips' future workshop (the rider-name surfacing on the driver-app trip-mode UI, methodology log entry 003). That is the kind of debt that gets paid by the next workshop or gets re-decided by ad-hoc implementation drift.
`─────────────────────────────────────────────────`

---

## Candidate next steps

Tradeoffs spelled out so you can re-decide in the morning. Several can run in parallel if your appetite allows.

### A — Trips workshop (the next event model)

Workshop §12.8 follow-up. Dispatch's 5.10 and 5.12 slices encode contracts that Trips is the receiving side of; until Trips' workshop runs, those slices are half-modeled. You would also exercise the §12.6 methodology adjustments (proactive projections from slice 1, pre-walk aggregate-identity sidebar, sub-slice numbering, deferred Protobuf authorship).

- **Cost:** pushes "actual coding" further out.
- **Value:** the second event model is where workshop conventions ossify into reusable methodology. Trips is also the richest event-sourcing domain you have — best second specimen.

### B — Bootstrap the Dispatch service skeleton (first implementation prompt)

Author `prompts/implementations/001-dispatch-service-skeleton.md` and stand up Dispatch as a **runnable but logic-free** service: solution layout, `Program.cs`, Aspire AppHost, Marten registration, health checks, test project. No slices implemented yet — just the canonical scaffold every other service will mirror. This is Dilger's **"Blueprint Architecture"** step: hand-build the template before turning the agent loose. Skills `adding-a-service`, `service-bootstrap`, `aspire`, and `vertical-slice-organization` all have a stake.

- **Cost:** one session, no domain progress.
- **Value:** the moment something runs, the project mode shifts from designing-about-code to writing-code, and the skill library finally gets exercised against a real surface.

### C — Slice 5.1 implementation prompt (`RideRequested`)

Smallest command slice with the cleanest narrative coverage (narrative 001 Moment 1). Tests the full SDD loop on real code: skill files load → narrative drives spec → Wolverine.HTTP endpoint + Marten aggregate produced → Alba scenario written → retrospective closes.

- **Prerequisite:** requires B to have happened, or has to absorb skeleton-bootstrap into its scope.
- **Value:** the first code in the repo, on a slice with two narratives backing it.

### D — Protobuf authorship session for `RideAssigned`

Standalone workshop §12.8 follow-up. Establishes the `/protos/` directory layout, namespace and versioning conventions, and the build wiring that ADR-009 has been promising. Smallest code-adjacent move you can make.

- **Cost:** low.
- **Value:** unblocks the Dispatch→Trips ASB topic, exercises ADR-009, gives you a reviewable "wire contract as PR" pattern. Forces the `/protos/` decisions while context is fresh, *before* the Trips workshop tries to consume the contract from the receiver's side.

### E — More Dispatch narratives (decline, expire, cancel, abandon)

Workshop 001's slices 5.6, 5.7, 5.8, 5.9, 5.11, 5.12 are not yet narrativized. Narrative 002's deferral list explicitly names the **driver-decline journey** as "strongest candidate for narrative #3." A temporal-automation slice (5.7, `OfferExpired`) has not yet been rendered at the narrative layer, which is a methodology gap worth closing.

- **Cost:** possibly diminishing returns on convention discovery — methodology log entry 001 predicted narrative 002 would not generate much new convention work, and entry 003 partially confirmed that.
- **Value:** failure-path coverage before code; lets you test the temporal-automation rendering convention.

---

## The lean

**Sequence: D → B → C, then either A or E.**

- **D first** because it is the smallest concrete commitment, exercises ADR-009, and forces the `/protos/` layout decisions while context is fresh.
- **B second** because every implementation prompt needs a service to host code, and the Dilger SDD note in your research is explicit that a hand-built blueprint is non-optional before you turn slice-by-slice work loose.
- **C third** because slice 5.1 is the simplest command slice and the one with the deepest narrative coverage, so you will get the cleanest "the loop works" signal from it.
- **A or E** after that, depending on whether the second workshop or more failure-path narratives feels more load-bearing once you have felt the first three sessions land.

### What to avoid

**Do not jump straight to A (Trips workshop) without first doing D (Protobuf for `RideAssigned`).** The Trips workshop will produce its own Translation-in slice that consumes that proto, and you do not want the Trips workshop deciding the proto's shape from the receiver's side without Dispatch having committed it from the sender's side first.

---

## Open questions to resolve in the morning

These are the things I would want a sentence on before kicking off Candidate D:

1. **`/protos/` location.** ADR-009 and `protobuf-contracts` skill say "shared directory separate from any single service's project directory" — convention is `/protos/` at the repo root. Confirm before authoring.
2. **Repo / solution structure.** Single solution at the root with one `.csproj` per service, or one solution per service? `adding-a-service` skill probably already settled this; verify before B.
3. **Whether to fold B and C into one session or two.** Dilger's blueprint advice is "2–3 representative slices manually." Bootstrapping the skeleton plus slice 5.1 in a single session is a defensible reading of "first representative slice." Two sessions with a retrospective between them is the more cautious reading.

None of these are blockers — flag them when you start the relevant prompt.

---

## Document history

- **2026-05-07.** Authored at Erik's request as a session-handoff note after a week-long vacation. Captures the orientation conversation that re-grounded the project's "what's next" after the skills foundation closed on 2026-05-06.
