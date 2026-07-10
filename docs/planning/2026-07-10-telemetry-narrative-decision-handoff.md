# Telemetry Narrative-Layer Decision — Handoff — 2026-07-10

> **Purpose:** Session-handoff note scoping a single small decision: does a thin driver-device
> journey narrative apply to Telemetry, or is the narrative step explicitly skipped with recorded
> rationale? This is item 1 of the ordered next-steps list from
> [`2026-07-04-post-pr39-telemetry-next-steps-handoff.md`](./2026-07-04-post-pr39-telemetry-next-steps-handoff.md).
> Disposable once this session orients and decides.

---

## Where we are (verified 2026-07-10)

- **PR #39 merged** (`d78bd82`, Telemetry `v1` protobuf contracts). `main` is clean and up to date.
- **Workshop 006** (Telemetry Event Model, stream-processing) + **ADR-018** are locked design —
  R1–R8, five slices. Do not re-litigate.
- Per the session workflow ([`CLAUDE.md`](../../CLAUDE.md) §Session Workflow, step 4), a narrative
  is normally authored between the workshop and the implementation prompt, threading workshop
  slices into one user's coherent journey. **No narrative exists yet for Telemetry**, and none of
  the ordered next-steps items skip straight to implementation without addressing this.

---

## The decision this session makes

**Question:** Does Telemetry warrant a narrative (per [`docs/narratives/README.md`](../narratives/README.md)), or does the narrative step get explicitly skipped for this bounded context?

**Why this is even a question:** Telemetry is machine-to-machine — a driver device streams GPS
pings via gRPC client-streaming; Dispatch consumes a Kafka-joined view. There is no human turn-taking
moment the way narrative 001 (rider books a ride) or 002 (driver accepts a ride) dramatize. Workshop
006 itself took the EM-direct path (no Domain Storytelling) for the same reason — the workshop found
no dialogic language boundary worth surfacing before modeling.

**The narratives README's own criteria** (read it in full before deciding) are useful tests here:

- Narratives are *journey-scoped, user-perspective specs* — "single-named-protagonist by default,"
  with **Context / Interaction / Response** Moments. Interaction is framed as "a user action... or a
  system trigger (an automation reacts to a prior event, an external boundary returns a value)" —
  so a system-triggered Moment is *within* the format's stated scope; a fully headless flow with no
  perceiving protagonist at all is the harder case.
- A driver-device journey *could* still have a protagonist (the driver, experiencing background
  location sharing, perhaps a "sharing your location" indicator) even if most of the interesting
  behavior (Kafka publish, Dispatch-side consumption) happens off-stage from that protagonist's
  perception. The README's omniscient-narrator convention explicitly allows this: "the narrator is
  omniscient about the system... but governs *what is dramatized as user experience* by what the
  protagonist actually perceives... This is what permits Moments where the system does most of the
  work (automation-driven slices) while keeping the journey voice intact." That's arguably
  Telemetry's exact shape.
- Against that: Workshop 006's slices (ingest → Kafka publish → store/eviction → Dispatch consumer
  view) are almost entirely **inter-service plumbing** with no rider- or driver-facing UI surface
  named anywhere in the workshop or ADR-018. If there is no protagonist-perceivable moment at all —
  not even a "location sharing is on" indicator — the format may have nothing to dramatize, and
  forcing a narrative would produce a document that violates the "do not carry: UX/UI design" and
  "no meta-labels" guardrails just to exist.

**This session's job is not to guess — it's to check whether a driver-perceivable moment actually
exists** by re-reading W006 §6.2–§6.5 for any driver-app-visible detail (even something as thin as
"driver app is sharing location," a permission prompt, or a status indicator). If one exists, thread
it into a thin narrative. If none exists, skip — but *record* the skip and its rationale, since per
the workflow this decision cannot be silently dropped.

---

## If the answer is "skip"

Do not silently omit the artifact. Options for where the rationale lives (pick one and note it in
this session's retro):

- A short entry appended to [`docs/narratives/README.md`](../narratives/README.md) — a new section
  or line noting Telemetry as a case where the narrative layer does not apply, with the rationale
  (no protagonist-perceivable moment; matches Workshop 006's EM-direct precedent for the same
  reason). This keeps the decision discoverable from the narratives layer itself, the way a future
  reader would look for it.
- Alternatively, fold the decision + rationale into the **framing section of the first Telemetry
  implementation prompt** (item 2 of the 2026-07-04 handoff) rather than a standalone artifact,
  since the 2026-07-04 handoff already floated this as one option ("likely its own tiny session or
  folded into the first implementation prompt's framing"). If chosen, this session's deliverable is
  then a short paragraph, not a new file — decide based on how it reads once drafted, not in the
  abstract.

Either way, name the decision explicitly. "No narrative — see rationale in X" is a two-sentence
addition, not a documentation project.

## If the answer is "a thin narrative applies"

Follow [`docs/narratives/README.md`](../narratives/README.md) exactly: frontmatter schema, prose
Moments labeled Context/Interaction/Response (no bullets), single named protagonist (the driver),
cite Workshop 006 slices via `Implements:` lines, do not restate GWT, do not carry transport/
implementation choices into the prose. Given the workshop's almost entirely machine-to-machine
shape, expect this narrative — if warranted — to be unusually short (plausibly 1–2 Moments), not a
full W006-length document.

---

## Guardrails carried forward

1. **Do not re-grill the Telemetry design.** R1–R8, the five slices, ADR-018 stand as committed.
2. **This session decides the narrative question only.** It does not start the transport build
   (item 2 of the 2026-07-04 handoff) — that is the next session after this one closes.
3. **Session discipline holds:** one prompt = one session = one PR (or, if this resolves in a
   single short sitting without needing a formal prompt document, still ships as its own PR with
   its own retro — check [`docs/prompts/README.md`](../prompts/README.md) for whether a decision
   this small needs a full prompt document or can retro directly).
4. **Name the spec delta.** Whatever narratives/README.md or the eventual prompt gains from this
   session is the closure-loop artifact — record it in that file's own `## Document history`.

---

## Orientation files (read in order)

1. [`docs/workshops/006-telemetry-event-model.md`](../workshops/006-telemetry-event-model.md) —
   §6.2–§6.5 especially; look for any driver-app-visible detail.
2. [`docs/narratives/README.md`](../narratives/README.md) — full format, especially "Voice and
   perspective" and "When a new narrative is warranted" / "not warranted."
3. [ADR-018](../decisions/018-candidate-projection-ownership-and-telemetry-geospatial-supply.md).
4. [`2026-07-04-post-pr39-telemetry-next-steps-handoff.md`](./2026-07-04-post-pr39-telemetry-next-steps-handoff.md)
   — full ordered next-steps list; this handoff is a drill-down on its item 1 only.

---

## Document history

- **2026-07-10.** Authored to scope item 1 (narrative-layer decision) of the 2026-07-04 post-PR-39
  handoff as its own small session, ahead of item 2 (first transport build).
