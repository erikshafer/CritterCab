# Prompt — Unpark Frontend Architecture (Vision v0.6 + Live-Update-Transport ADR)

| Field | Value |
|---|---|
| **Status** | Pending (authored 2026-06-16; awaiting execution as a separate design-return session) |
| **Authored** | 2026-06-16 |
| **Session kind** | Design-return (vision amendment + new ADR). **Not** a `tidy:` session — it introduces new architectural commitments, so it carries its own descriptive commit/PR subject. |
| **Target artifacts** | `docs/vision/README.md` (unpark frontend + map library; version bump to v0.6); `docs/decisions/016-frontend-live-update-transport.md` (new ADR — confirm `016` is still the next free number against the index at session start); `docs/decisions/README.md` (ADR-016 index row); `docs/prompts/frontend-architecture-unpark.md` (this prompt's index entry, bundled with the prompt commit); `docs/prompts/README.md` (index entry); `docs/retrospectives/frontend-architecture-unpark.md` (retro); `docs/retrospectives/README.md` (index entry) |
| **Source-of-truth dependencies** | [`docs/research/frontend-survey-sibling-repos.md`](../research/frontend-survey-sibling-repos.md) (**the primary input** — distilled from the `mmo-reconnect` and `CritterBids` frontends; cite the survey, do not re-derive from the raw repos); [`docs/vision/README.md`](../vision/README.md) §Explicitly Parked (Frontend architecture; Map library), §Tentative Technology Stack, §Goals (Primary #1 — gRPC thesis), §Design Principles; [`docs/decisions/005-transport-selection-by-flow-type.md`](../decisions/005-transport-selection-by-flow-type.md) (the frontend transport choice is an instance of this principle); [`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md) (token acquisition for authed surfaces couples here); [`docs/decisions/004-design-phase-workflow-sequence.md`](../decisions/004-design-phase-workflow-sequence.md) (design-return cadence) |
| **Produced by** | Conversation on 2026-06-16 that authored the survey (deliverable #1) and agreed to split the unpark into its own session (deliverable #2). |

---

## Framing — why this session exists

CritterCab's frontend has been parked since vision v0.1 (2026-04-21). The vision's own unpark trigger was *"CritterBids lands on a stable live-update pattern."* The [sibling-repo frontend survey](../research/frontend-survey-sibling-repos.md) confirms that trigger has fired: `CritterBids/client/shared/src/signalr/provider.tsx` is a generic, packaged `createSignalRProvider<TMessage>` that bridges a push transport into the TanStack Query cache, shared across three SPAs. The dependency the park was waiting on is discharged.

This session does the unpark. It is a **design-return** per [ADR-004](../decisions/004-design-phase-workflow-sequence.md) and the design-return cadence — a deliberate step back into the design phase after the recent implementation/tidy/workshop stretch, not a slice. Its job is to move the frontend from "parked, awaiting CritterBids" to "working direction committed, with the one genuinely contested decision (live-update transport) given a home and a gate."

The survey already did the comparison work and named the tension. This session's job is **judgment, not research**: decide what the vision adopts as working direction, decide how the transport decision is recorded, and resolve (or explicitly re-file) the parked items. The survey is the input; do not relitigate its findings.

The crux, carried verbatim from the survey: CritterBids chose **SignalR**, but CritterCab exists to showcase **Wolverine's gRPC** (vision §Goals Primary #1), and the parked note lists **gRPC-web streaming** as a candidate. Adopting SignalR wholesale would route the most visible real-time surfaces *around* the project's reason to exist. The transferable asset is the **architecture** (transport → Query-cache bridge → declarative components), with the **transport slot** decided on CritterCab's terms — not inherited.

---

## Goal

Amend `docs/vision/README.md` to **v0.6**, unparking frontend architecture: adopt the survey's convergent house stack and audience-SPA monorepo shape as the working direction, and re-file the two parked frontend items (frontend architecture, map library) out of `§Explicitly Parked`. Author **ADR-016 — Frontend Live-Update Transport**, recording the transport-agnostic push→Query-cache-bridge architecture and the SignalR-vs-gRPC-web tradeoff, with the concrete transport selection gated on a gRPC-web spike.

---

## Spec delta

No narrative or workshop spec is amended — this is a vision-level design-return, not a slice, so the canonical narrative/workshop spec gains nothing. Named honestly as a **null narrative/workshop delta** (the same honest-null pattern as `housekeeping-delete-may-15-handoff`, `skills-tidy-ai-skills-sync`, `workshops/003`, `aspire-reconcile-and-port-band`, `fix-marten9-projection-source-gen`).

The session's actual deltas land at the vision/decision layer, not the spec layer:
- The vision doc gains a frontend tech-stack + app-structure direction and loses two `§Explicitly Parked` entries.
- The canonical decision record gains ADR-016.

---

## Orientation files (read in order)

1. **[`docs/research/frontend-survey-sibling-repos.md`](../research/frontend-survey-sibling-repos.md)** — the whole input. Read first and in full. Findings 1–5, the two gaps, and the six open questions are the menu this session decides from. Its "Recommended next step" is the skeleton of this session's deliverable plan.
2. **[`docs/vision/README.md`](../vision/README.md) §Explicitly Parked** — the two entries to re-file ("Frontend architecture"; "Map library for frontend"). Note the exact wording of the trigger so the v0.6 Document History entry can cite it as discharged.
3. **[`docs/vision/README.md`](../vision/README.md) §Tentative Technology Stack + §Goals** — where the frontend stack direction lands, and the gRPC thesis (Primary #1) that the transport decision must not undercut.
4. **[`docs/decisions/005-transport-selection-by-flow-type.md`](../decisions/005-transport-selection-by-flow-type.md)** — "transport split by flow type, not convenience." The frontend live-update transport is a new instance of this exact principle; ADR-016 should cross-reference it as precedent, not re-argue it.
5. **[`docs/decisions/006-identity-provider-as-swappable-anti-corruption-layer.md`](../decisions/006-identity-provider-as-swappable-anti-corruption-layer.md)** — authed surfaces (driver/rider/ops) acquire tokens against Entra; the SignalR `accessTokenFactory` shape the survey noted couples to this. Relevant to ADR-016's auth paragraph; not re-decided here.
6. **[`docs/prompts/context-map-foundation.md`](./context-map-foundation.md)** — the structural precedent: a design-return that bumped the vision and opened an ADR. Mirror its working-pattern discipline (surface-the-lean → sign-off → author) and its retro shape.

---

## Working pattern

Interactive, decision-at-a-time, with sign-off on each durable call before drafting prose. This session is **judgment-dense and short on mechanics** — most of the effort is in five named decisions, not in volume of text. Do not batch the whole session into one output.

Sequence:

1. **Confirm scope and the ADR number** (`016` free per the index at authoring; re-verify). Confirm the one-bundled-PR shape.
2. **Walk the five "Decisions to flag" below, one at a time.** Pre-author the lean, surface it with the survey citation, get sign-off, then move on. These decisions *are* the session.
3. **Draft ADR-016** per the [`docs/decisions/` format already established there](../decisions/) (the CritterCab format — **not** the vendored `grill-with-docs/ADR-FORMAT.md`, per the CLAUDE.md precedence table). Surface the draft for sign-off.
4. **Amend the vision to v0.6** — adopt the stack/monorepo direction in §Tentative Technology Stack, re-file the two parked items, add the Document History entry citing the discharged trigger and ADR-016.
5. **Compose the retro**, prompt index entry, and retro index entry. Open the bundled PR.

Sign-off discipline (per memory): drive the non-durable choices (ADR filename slug, section ordering, diagram syntax) but hold for explicit sign-off on every durable call — the five decisions and the ADR's accepted/proposed status above all.

---

## Decisions to flag during the session

1. **How is the transport decision recorded — Accepted-with-default, or Proposed-pending-spike?**
   *Lean:* author ADR-016 as **Accepted**, deciding (a) the transport-agnostic push→Query-cache-bridge architecture (ported from CritterBids' `createSignalRProvider` shape, transport parameterised) and (b) a **default to gRPC-web streaming** to stay on-thesis, **gated on a spike** proving gRPC-web streaming into React is ergonomic with the Critter Stack, with **SignalR as the named fallback** if the spike fails. This is decisive enough to be Accepted while honest about the open spike; the transport *leaf* is confirmed by a successor amendment post-spike. *Fallback:* if the user prefers ADRs only land fully-resolved, record ADR-016 as **Proposed** (CritterCab's first), or defer the ADR entirely and carry the decision as a vision open-question until the spike runs. Surface for sign-off — this is the load-bearing call of the session.

2. **Does the stack + monorepo adoption need its own ADR, or is the vision doc enough?**
   *Lean:* **vision doc only.** The convergent stack (React 19 / Vite 8 / TS 6 / Tailwind v4 / TanStack Query) and the audience-SPA monorepo shape (`rider/driver/operations`) are low-contention "working direction," the same register as the existing Tentative Technology Stack. Reserve ADR weight for the genuinely contested transport decision. Re-evaluate if the user wants the monorepo-vs-separate-repos tension (open question #3) settled now rather than flagged.

3. **Map library — re-file to open-question, or pick now?**
   *Lean:* **re-file, do not pick.** The choice couples to the backend geospatial representation (H3 cells / GeoJSON — see [ride-sharing-lessons-learned.md](../research/ride-sharing-lessons-learned.md) Lesson 4) and deserves its own evaluation. Move it from `§Explicitly Parked` to an open question naming the candidates (MapLibre GL / Leaflet / deck.gl) and its backend coupling. Flag a follow-up research/spike session.

4. **gRPC-web spike — name it as a follow-up, what kind?**
   *Lean:* a **throwaway implementation spike**, not a research doc — the open question is ergonomics (Wolverine-native gRPC-web vs. an Envoy/proxy hop, streaming reconnect semantics, React DX), which needs code, not reading. ADR-016's gate points at it. Name it as a distinct successor session; do not start it here.

5. **One shared contracts package vs. per-BC packages (survey open question #2).**
   *Lean:* **flag as a vision open-question, do not decide.** It only bites once frontend implementation starts, and it interacts with CritterCab's separately-deployed-services stance. Record it; defer it.

---

## Deliverable plan

1. **`docs/decisions/016-frontend-live-update-transport.md`** — new ADR in the established `docs/decisions/` format. Names the candidates (SignalR / gRPC-web streaming / WebSockets-SSE), the tradeoff (proven-off-thesis vs. on-thesis-unproven), the decision per flag #1, the spike-gate, the cross-reference to ADR-005 (transport-by-flow) and ADR-006 (token acquisition), and the consequence that the transport→cache-bridge component contract is transport-agnostic by design.
2. **`docs/decisions/README.md`** — add the ADR-016 row.
3. **`docs/vision/README.md`** — adopt the frontend stack + monorepo direction in §Tentative Technology Stack; remove "Frontend architecture" and "Map library for frontend" from §Explicitly Parked (re-filed as open questions per flags #3/#5); add §Open Questions entries (map library; one-vs-many contracts package; monorepo-vs-separate-repos); bump to **v0.6** with a Document History entry citing the discharged trigger, the survey, and ADR-016.
4. **`docs/prompts/README.md`** — add this prompt's entry under `Multi-artifact prompts (root)` (bundle with the prompt commit).
5. **`docs/retrospectives/frontend-architecture-unpark.md`** — retro per the [retrospectives README](../retrospectives/README.md). Capture: whether the SignalR-vs-gRPC-web call held at Accepted-with-default or slid to Proposed/deferred; whether the stack adoption stayed vision-level or grew an ADR; how many parked items became open questions vs. resolved; the spike and map-library follow-ups queued.
6. **`docs/retrospectives/README.md`** — add the retro entry.

### Definition of done

- ADR-016 committed in the CritterCab format, status and content signed off per flag #1.
- Vision doc at v0.6: stack/monorepo direction adopted, both parked frontend items re-filed, open questions added, Document History entry citing the discharged trigger + survey + ADR-016.
- Prompt + index entry bundled commit; retro + index entry committed.
- Follow-up sessions named (gRPC-web spike; map-library selection) in the retro and/or vision open-questions — not started.
- Bundled PR opened with a descriptive (non-`tidy:`) subject.

---

## Out of scope

- **The gRPC-web spike itself.** Named as a successor session; ADR-016 gates on it but does not perform it.
- **Map-library selection.** Re-filed to an open question; the actual pick is a later session (flag #3).
- **Any frontend code.** No SPA is scaffolded, no monorepo created, no package installed. This session is vision + ADR only.
- **The one-vs-many contracts-package and monorepo-vs-separate-repos questions.** Flagged in the vision, deferred (flags #2/#5).
- **Editing the survey artifact.** `docs/research/frontend-survey-sibling-repos.md` is frozen research; it is an *input*, not a deliverable. Do not amend it (no opportunistic cross-file edits per the cadence rule).
- **`crittermart`'s frontend.** Excluded from the survey by the author's refinement assessment; not reintroduced here.
- **Re-litigating ADR-005 or ADR-006.** ADR-016 cross-references them as precedent; it does not re-decide transport-by-flow or the identity ACL.

---

## Suggested skills

For the next session-runner (the handoff skill's requested section, tailored to this session):

- **`grill-with-docs`** — strongly recommended *before* ADR-016 is drafted. This is design-phase planning where a decision is being stress-tested against the project's own documented language and decisions (the gRPC thesis, ADR-005, the survey's named tension). The CLAUDE.md precedence overrides apply: use `docs/decisions/` ADR format (not the vendored `ADR-FORMAT.md`), and do not create a root `CONTEXT.md`.
- **`find-docs`** (or the `ctx7` CLI per the global rule) — to verify *current* facts when writing ADR-016's tradeoff: gRPC-web streaming support and browser ergonomics, `@microsoft/signalr` v10 transport options, and TanStack Query cache-write patterns. Do not assert library capabilities from memory in an ADR that will be cited later.
- **Deferred to the successor spike session, not this one:** `jasperfx-source-verifier` and the `ai-skills-consultant` agent — for verifying Wolverine's gRPC-web surface against the local JasperFx checkout when the spike runs. Out of scope here (no code), named so the spike session inherits the pointer.
- **`blurb`** — at session close, to hand off to whatever comes next (the spike or the map-library session).

---

## Memory inheritance

Applicable feedback memories carry via `MEMORY.md`; no restatement needed. Most load-bearing for this session:

- **Communication depth + ubiquitous language + leaning opinions** — pre-author a lean on each of the five decisions; surface the genuinely contested ones (flag #1 above all) for sign-off.
- **Propose Critter Stack primitives, not bespoke alternatives** — the transport decision's on-thesis pole *is* the Critter Stack primitive (Wolverine gRPC); frame gRPC-web as the Critter-Stack-native option, SignalR as the proven-but-off-thesis alternative.
- **Explicit deferrals during artifact authoring** — load-bearing. The map library, the spike, and the two contracts/monorepo questions are all deferred *with named triggers*, not silently dropped.
- **Keep READMEs current alongside session work** — the vision Document History, decisions index, prompts index, and retros index all update in the same PR.
- **No Claude attribution on commits or PRs** — at commit/PR time.
- **Driver-mode when the user signals a block** — drive the non-durable choices (slug, ordering); hold sign-off on the five decisions and the ADR status.
- **Static endpoints / Alba-first / validation-at-HTTP-boundary** — not directly applicable (no code), but relevant context if ADR-016's consequences touch the BFF/HTTP surface the live-update transport rides over.

---

## Starting move

1. **Read the orientation files in order** — the survey first and in full.
2. **Confirm scope + ADR-016 number** against `docs/decisions/README.md`; confirm the one-bundled-PR shape.
3. **Optionally run `grill-with-docs`** on the unpark plan before drafting, to pressure-test the transport decision against the gRPC thesis and ADR-005.
4. **Walk the five decisions** one at a time — lean → citation → sign-off — starting with flag #1 (the transport-recording call), since the ADR's shape depends on it.
5. **Draft ADR-016**, surface for sign-off, then **amend the vision to v0.6**.
6. **Compose the retro**, index entries, and open the bundled PR (descriptive subject, not `tidy:`).

Session estimate: 45–75 minutes — judgment-dense, low-volume. The risk is over-deciding (picking the map library, or resolving the transport without the spike); the discipline is to decide the *direction* and *gate* cleanly and let the spike and follow-ups do their jobs.
