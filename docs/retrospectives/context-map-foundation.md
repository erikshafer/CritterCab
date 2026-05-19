# Retrospective — Context-Map Foundation

| Field | Value |
|---|---|
| **Triggering prompt** | [`docs/prompts/context-map-foundation.md`](../prompts/context-map-foundation.md) |
| **Status** | Complete |
| **Date** | 2026-05-19 |
| **Output artifacts** | [`docs/context-map/README.md`](../context-map/README.md) (new); [`docs/vision/README.md`](../vision/README.md) (v0.3 → v0.4 + §Methodology cross-reference); [`docs/prompts/README.md`](../prompts/README.md) (index entry); [`docs/retrospectives/README.md`](../retrospectives/README.md) (this entry) |
| **One-line outcome** | First-class context-map artifact authored from distributed source materials; closes a methodology commitment open since the vision doc's v0.1. |

---

## Framing

This session executed Session B of the two follow-up sessions queued by the 2026-05-15 spec-delta + context-map handoff. Its job was rolling up cross-BC relationships from ADRs 006, 013, 014 and Workshops 001 and 002 into a single named artifact using DDD strategic-design vocabulary — a roll-up, deliberately not a re-decision.

---

## Outcome summary

- **6 edges authored** in the context-map artifact: two fully locked (Dispatch ↔ Trips, both directions); one partly-locked (Identity → external provider as ACL + Identity → consumer BCs as PL, with the Trips concrete consumer locked via W002 §6.12 and three others pending); three deferred with explicit pending-workshop markers (Trips → seven-way fan-out, Telemetry → Dispatch, intra-actor topology including Onboarding → Driver Profile and the discovered Driver Profile → Dispatch).
- **Vision doc bumped to v0.4** with a cross-reference from §Methodology's DDD strategic-design bullet to the new artifact.
- **ADR-016 ("Context Map as Living Artifact") deferred** per the session-start lean. Trigger to revisit recorded in the retro and in the artifact's [§Update cadence](../context-map/README.md#update-cadence).
- **Index entries** added in `prompts/README.md` and `retrospectives/README.md`.
- **One discovered edge** added beyond the prompt's six-edge enumeration: Driver Profile → Dispatch (surfaced from W001 §5.3's `CandidatesSelected` translation-slice), included with user sign-off as a structural pair to Telemetry → Dispatch.

---

## What worked

- **Per-edge interactive walk with sign-off** mirrored Workshop 001 §12.4's slice cadence and prevented retroactive churn — the artifact only ever reflected agreed-upon naming. The cadence transfers cleanly from workshop slice walks to non-slice strategic-design artifact authoring.
- **Pre-authored leans on every edge** let each sign-off be a focused yes/no/push-back decision rather than an open-ended discussion. The "give leaning opinions on questions" feedback memory was load-bearing throughout.
- **Mermaid for the diagram.** Edge-label expressiveness was sufficient for the seven-pattern DDD vocabulary plus the locked-vs-deferred dashed-line convention plus external-system shape (subroutine) for the identity provider node. PlantUML fallback was not needed.
- **Roll-up framing held.** No ADR was re-litigated. ADR-006's ACL stance, ADR-013's shared-identifier stance, and ADR-014's topic-naming convention were all named-and-cross-referenced; none were re-decided. The prompt's "do not re-decide any relationship already committed" watch-out was honored.
- **Explicit deferrals with named expected-resolution triggers** consistently applied at the diagram layer (dashed lines + italicized expected-pattern labels) and the prose layer (§Pending workshops table with per-BC trigger conditions).
- **Asymmetry framing on Dispatch ↔ Trips** (edge #1 CS/PL handoff + edge #2 CF terminal feedback) made the *value of directed-edge context maps* visible. The same BC pair carries different patterns per direction; a symmetric "BC pair → one pattern" reading would have hidden the modeling work each direction's negotiation embodied.
- **Surfacing newly-discovered-but-out-of-prompt-scope items as user-decision questions** (Driver Profile → Dispatch, Trust & Safety inventory gap, Rider Profile enumeration drift) preserved both honesty to the source materials and discipline against scope creep. The `AskUserQuestion` pattern with leaning recommendations matched the per-edge sign-off rhythm.

---

## What was harder than expected

- **Two handoff-doc inaccuracies were real**, as the prompt's pre-work flagged in advance:
  1. **W001 has no §13.** The handoff doc listed "Workshop §13 forward-constraints in W001 and W002" as source material. W001's analogous cross-BC content is in §6 (Translation Slices at BC Boundaries), §7 (Temporal Automation Slices), §9 (Candidate Protobuf Contract Surface), and §11 (ADR Candidates). The artifact's source citations were authored against the correct W001 sections.
  2. **Narratives 001 and 002 already have Document History sections.** The handoff doc claimed they didn't; both are populated and the narratives README already names the section as body-section #7. This inaccuracy primarily affects Session A (the spec-delta encoding session) rather than this session, but flagged here per the prompt's instruction so the planning-doc convention learns from the slip.
- **The prompt's six-edge enumeration was incomplete relative to the source materials.** W001 §5.3's `CandidatesSelected` translation-slice names "Telemetry + Driver Profile" as joint counterparty BCs; the prompt named Telemetry → Dispatch as edge #5 but did not name Driver Profile → Dispatch. Surfaced as a user-decision question; included with user sign-off. The lesson: even prompts authored carefully against the source materials can miss edges; surface-and-flag is safer than silent inclusion or silent omission.
- **Trust & Safety inventory gap.** W002 §10 names Trust & Safety as a downstream consumer of two `trips.*` topics, but the vision doc's v0.1 BC inventory does not include it. The context map could either (a) silently extend the inventory, (b) silently omit Trust & Safety, or (c) flag the drift explicitly. Option (c) won; the artifact's §Pending workshops names Trust & Safety as a candidate BC not yet in the inventory and flags the drift for a follow-up vision-doc tidy session.
- **Diagram density rose faster than expected** as edges accumulated. The current Mermaid LR layout is workable at 13 edges but may need either subgraph grouping or a layout-direction swap (LR → TD) once W003 lands and 2–4 new edges are added in the Identity workshop's PR. Worth flagging for the next workshop's session-runner.

---

## Methodology reflections — answering the four framing questions from the prompt

The prompt named four specific questions to capture in this retro. Answering each directly:

1. **Did the per-edge walk produce a cleaner artifact than a top-down "describe-all-relationships" pass would have?** Yes, materially. The per-edge cadence forced each relationship-pattern call to be defended on its own evidence (CS vs. CF for edge #2; ACL + PL split for edge #3; supplier-locked-consumer-presumed for edge #4) rather than absorbed into a single sweeping framing. The CS/CF asymmetry on Dispatch ↔ Trips would likely have been collapsed in a top-down pass.
2. **Did any pre-committed relationship need re-litigation despite the no-re-decide watch-out?** No. Every cross-reference to ADR-006, ADR-013, ADR-014 named-and-quoted rather than reopened. The two areas with genuine choice (T&S handling, Driver Profile → Dispatch inclusion) were both *new* questions surfaced by the artifact, not re-decisions of pre-committed material.
3. **How many edges needed explicit deferral markers, and what does that ratio say about workshop coverage?** Six of seven edge-groups carry at least one dashed arrow. Of 13 cross-BC arrows on the diagram, 4 are locked (Dispatch → Trips, Trips → Dispatch, Provider → Identity ACL, Identity → Trips PL) and 9 are dashed pending-workshop. The 4:9 locked-to-pending ratio is a useful baseline data point: it reflects exactly the workshop coverage state (2 of 11+1 BCs workshopped) and confirms that the artifact will substantially fill in over the next several workshops. The dashed-line discipline preserves visibility into the gap rather than disguising it.
4. **Did ADR-016 land this session or get deferred?** Deferred per the session-start lean. The cadence rule is best codified after it has been exercised on its first real update (W003 will be the first natural amendment). Trigger to revisit recorded in the artifact's §Update cadence and in this retro's §Outstanding items.

---

## Methodology refinements that emerged

- **Per-edge sign-off cadence is a reusable pattern for any artifact with N independent decisions.** Workshop slice walks pioneered it; this session confirms it transfers cleanly to context-map authoring. Likely transfers to ADR-bundle authoring (already exercised via [`prompts/decisions/002-bundled-pattern-adrs.md`](../prompts/decisions/002-bundled-pattern-adrs.md)). Worth carrying forward to any future artifact with this shape — including, plausibly, narrative authoring that spans many moments.
- **Surface-and-flag for newly-discovered scope is now the default discipline.** Three discovered items in this session (Driver Profile → Dispatch, Trust & Safety, Rider Profile drift) were all surfaced as user-decision questions with leaning recommendations rather than silently included or silently omitted. The pattern preserves honesty to source materials, discipline against scope creep, and a durable record of the call in the retro.
- **Dashed-arrows-with-italicized-labels at the diagram layer** is the right visual encoding for "pattern expected, not yet locked." The dashed line marks the deferral; the italic label preserves the expected pattern; the per-edge prose names the expected-resolution trigger. Three-part deferral protocol; worth applying to any future diagram-bearing artifact.
- **Strategic-design vocabulary is coarser than tactical-design vocabulary, and that's the point.** This session resisted the temptation to invent finer-grained patterns (e.g., "PL-with-presumed-conformist-consumers") and instead used the seven canonical patterns with paired labels (`CS/PL`) and explicit pending-state markers where the canonical vocabulary alone was insufficient. The DDD canon held; no project-specific patterns were minted.
- **Cross-cutting concerns belong in framing prose, not as edge labels.** ADR-013's shared canonical identifier, ADR-014's topic naming, and ADR-009's protobuf wire format all govern multiple edges; naming any of them as an edge pattern (e.g., "shared kernel" for the canonical ID) would over-claim. The framing-prose treatment correctly captures their cross-cutting nature.

---

## Outstanding items / next-session inputs

- **ADR-016 ("Context Map as Living Artifact") — deferred.** **Trigger to revisit:** W003 lands (Identity is the strongest candidate per the artifact's §Pending workshops) and the context map is amended in the same PR. If the amendment runs cleanly, the cadence is empirically validated and the ADR question shifts from "what should the discipline say?" to "is this worth committing as a project-wide discipline?"
- **Vision-doc inventory amendment for Trust & Safety.** Currently flagged in the artifact's §Pending workshops; the actual vision-doc amendment should happen either as part of Trust & Safety's eventual workshop or as a deliberate inventory-amendment tidy session, whichever comes first.
- **Diagram density review at W003 close.** The Mermaid LR layout is workable at 13 edges; adding 2–4 Identity-workshop edges will likely push it toward needing subgraph grouping or an LR → TD swap. The W003 session-runner should review the diagram's legibility at close.
- **Session A — [spec-delta closure-loop encoding](../prompts/encode-spec-delta-closure-loop.md) — is next in queue** per the May 15 handoff. Now that Session B is shipped, Session A's retro can include the `Spec delta — landed?` line as its first real exercise of the new convention. Worth noting: this session is the last session in CritterCab to run *before* the spec-delta convention exists; it is a baseline data point against which Session A's discipline will be evaluated.
- **Pricing-location and Operations-decomposition ADR candidates** (W001 §11 ADR-candidates #4 and the Operations decomposition open question from the vision doc) remain open. They fire when their respective BCs are workshopped; until then they sit in the artifact's §Pending workshops with named triggers.

---

## Quantitative summary

| Metric | Count |
|---|---|
| Edges authored on the diagram | 13 cross-BC arrows across 6 edge-groups |
| Locked edges (solid arrows) | 4 |
| Deferred edges (dashed arrows) | 9 |
| BCs with completed workshops | 2 (Dispatch, Trips) |
| BCs pending workshop in the vision-doc inventory | 9 |
| Candidate BCs surfaced beyond the vision-doc inventory | 1 (Trust & Safety) |
| ADRs cross-referenced | 4 (006, 009, 013, 014) |
| ADRs re-litigated | 0 |
| Workshop sections cross-referenced | W001 §5.3/§5.10/§5.12/§6/§7/§9/§11; W002 §6.1/§6.9/§6.10/§6.12/§7/§10/§13 |
| Discovered edges added beyond the prompt's enumeration | 1 (Driver Profile → Dispatch) |
| Deferred ADRs | 1 (ADR-016) |
| Methodology refinements named for carry-forward | 5 |
