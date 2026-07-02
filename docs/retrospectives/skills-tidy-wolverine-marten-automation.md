# Retrospective — Skill Tidy: Wolverine + Marten Event-Triggered Automation Handler Shape

## Metadata

- **Triggering prompt:** [`docs/prompts/skills-tidy-wolverine-marten-automation.md`](../prompts/skills-tidy-wolverine-marten-automation.md)
- **Status:** Complete
- **Date authored:** 2026-07-02
- **Output artifacts:**
  - `docs/skills/wolverine-marten-automation/SKILL.md` (new) — event-triggered automation handler shape, the two-part registration prerequisite, and the marker-interface union return-type pattern
  - `docs/skills/README.md` — cluster index, tag index (`wolverine`, `marten`, `handlers`, `decider-pattern`), new entry-point-hub row, Phase 2 cross-reference graph node
  - `docs/skills/DEBT.md` — both rows drained; `Open debt` reset to empty; document history extended
  - `docs/prompts/skills-tidy-wolverine-marten-automation.md` (new) — the prompt that triggered this session
- **Outcome:** Second skill-tidy session complete. Both at-threshold `DEBT.md` rows drained in one PR via a new skill, not a bolt-on. Item 1 of the [post-W006 handoff](../planning/2026-07-02-post-w006-next-steps-handoff.md)'s ordered table.

---

## Framing

Retro 005 (slice 5.3, `CandidateSelectionAutomation`) surfaced two skill-file gaps — the marker-interface union return type and the event-triggered automation handler shape — that didn't block the session-runner. Both were registered in `DEBT.md` on 2026-06-25 as one grouped entry, explicitly leaving open whether to extend `wolverine-handlers` or author a new `wolverine-marten-automation` skill.

This session drains that backlog as the recommended pre-step ahead of the Telemetry proto authorship + implementation chain — the post-W006 handoff's framing was that a small, low-risk tidy sharpens the critter-skill-auditor's Phase 1 discovery right before the biggest implementation session CritterCab has attempted (a second service, gRPC client-streaming, first Kafka topic).

---

## Outcome summary

| Item | Resolution |
|---|---|
| New skill vs. bolt-on | **New skill.** `wolverine-handlers`'s own charter is explicitly trigger-agnostic and already delegates to three protocol-specific siblings (HTTP, messaging, gRPC); event-forwarded automation is a natural fourth. `marten-wolverine-aggregates` was also read and ruled out — every worked example there is command-handler shaped and it never mentions `UseFastEventForwarding`. |
| Marker-interface union return type | Documented in the new skill with both real instances (`IFareQuoteOutcome`, `ICandidateSelectionOutcome`) and the `DetermineEventCaptureHandling` mechanical-inertness explanation. |
| Event-triggered automation handler shape | Documented with both real instances, the non-first-stream-event grounding (`CandidateSelectionAutomation` keys off `FareQuoted`, the stream's second event), and the observed absence of a `Validate`/`Before` step flagged as an open question rather than a rule. |
| Registration prerequisites | Documented as **two** independent prerequisites, not the one the DEBT row named — see below. |

`Open debt` reset to empty. `Recently drained` gained a `### 2026-07-02` heading. `docs/skills/README.md` updated across all four navigation surfaces the README's own convention names (cluster index, tag index, entry-point hubs, cross-reference graph).

---

## What worked

- **Delegating Phase 1 discovery to the critter-skill-auditor before drafting the prompt.** The auditor agent read both candidate bolt-on skills end-to-end, read the actual automation source files, and returned a clear recommendation (new skill) with the specific reasoning (charter mismatch, no command-handler-vs-automation blur) *before* any skill content was drafted. This avoided the failure mode of starting to write inside `wolverine-handlers` and only later discovering the fit was wrong.
- **Reading the real code myself before trusting the auditor's summary.** The auditor's report was accurate, but reading `Program.cs` directly surfaced a detail its report didn't call out explicitly: automations require **two** independent registration steps (`UseFastEventForwarding` *and* `CustomizeHandlerDiscovery(...WithNameSuffix("Automation"))`), not the one the DEBT row named. The DEBT row's phrasing — "a static automation reacting to a domain event (via `UseFastEventForwarding`)" — reads as if that's the whole registration story. It isn't. This is exactly the source-of-truth precedence rule (working code → retro → external docs) paying off a second time: the retro/DEBT-row evidence undersold the registration surface, and only the code caught it.
- **The two-row DEBT entry already naming the open decision.** Because the 2026-06-25 registration explicitly framed "extend `wolverine-handlers` or warrant a new skill" as the tidy session's decision to make, there was no ambiguity about what this session owed — verify the fit, decide, document.

---

## What was harder than expected

- **Deciding how much to say about the missing `Validate`/`Before` step.** Neither real example has one, which could read as either "automations never need validation" (a rule) or "hasn't come up yet" (an absence). Inventing a rule from two data points felt premature — no design session has actually settled this — so the skill documents it as an *observed characteristic* with an explicit "this is open" caveat rather than a mandated convention. Worth watching whether a third automation instance resolves the question one way or the other.
- **Whether the Phase 2 cross-reference graph edit counted as "meaningful topology change."** The README's own convention gates graph updates on that judgment call. A brand-new skill node with real upstream edges felt like an easy yes, but it's worth naming explicitly: the bar in practice was "does this add a node," not "does this add a node with non-trivial new edges." A future skill addition with a single upstream edge would meet the same bar under this reading.

---

## Methodology refinements that emerged

The prompt's "What this retro should specifically capture" section asked three questions. Below.

### 1. Was the two-registration-prerequisite finding already implied by the DEBT row, or new?

**New.** The DEBT row names only `UseFastEventForwarding`. The `CustomizeHandlerDiscovery(...WithNameSuffix("Automation"))` line in `Program.cs` is an equally load-bearing, independently-silent-when-missing prerequisite that neither the DEBT row nor retro 005's excerpt (per the auditor's report) called out. This is now the skill's most concrete pitfall — worth flagging to future DEBT-row authors that "cite the registration API" rows should be re-verified against the full composition root, not just the specific line a retro remembered.

### 2. Was "new skill" the right call, now that it exists as a finished artifact?

**Yes.** With the skill written, it reads as a clean fourth sibling alongside `wolverine-http-handlers`/`wolverine-messaging-handlers`/`wolverine-grpc-handlers` — same "hub delegates, sibling owns trigger-specific shape" structure `wolverine-handlers` already established. Nothing in the finished skill wanted to live inside `wolverine-handlers`'s trigger-agnostic charter or `marten-wolverine-aggregates`'s command-handler-shaped examples.

### 3. Should the `Validate`/`Before`-absence open question become its own DEBT row?

**No, not yet.** It's not a known gap with a known fix — it's an open design question with only two data points. A DEBT row implies "we know what the skill should say and haven't said it yet," which isn't the case here. Leaving it as an explicit in-skill caveat is more honest; promote to a DEBT row (or a design-phase question) only if a third automation instance actually needs a precondition step and the answer becomes visible in code.

### 4. A convention ambiguity surfaced by Phase 2 audit: where should this prompt/retro pair live?

The critter-skill-auditor's Phase 2 verification pass flagged a real ambiguity, not a violation: `docs/retrospectives/README.md` states a one-off single-new-skill session "would produce a retro in `skills/`," and this session is, in one reading, exactly that. It landed at root instead, following the first skill-tidy session's precedent, on the reasoning that this session also drains `DEBT.md` and touches all four `docs/skills/README.md` navigation surfaces — broader than "a single skill file," matching the shape that put the first tidy at root. Neither README explicitly disambiguates "new skill + DEBT drain" from "new skill alone." Worth tightening in a future housekeeping pass: candidate rule is *"a session whose deliverable is a single new skill file plus a DEBT.md drain is a `tidy:` session and lives at root; a session that authors a new skill with no DEBT.md involvement is a per-skill session and lives in `skills/`."* Not authored here — an input to the next housekeeping prompt, per this session's own no-opportunistic-edits scope.

---

## Outstanding items / next-session inputs

- **The root-vs-`skills/` placement ambiguity** (§ Methodology refinements #4) is an input to a future housekeeping pass tightening `docs/retrospectives/README.md`'s subdirectory rule.
- **The `Validate`/`Before`-absence open question** stays as an in-skill caveat, not a DEBT row (see above). Revisit if a third automation instance surfaces a real need.
- **The bundling-rule gap** flagged past-threshold in retro 005 but never registered in `DEBT.md` (no target skill named) remains open for a future session that can ground the target skill.
- **Item 2 of the post-W006 handoff** (Telemetry `v1` proto authorship per W006 §10) is the next recommended session — unaffected by this tidy, but this tidy was explicitly sequenced to precede it.

---

## Spec delta — landed?

Named null in the prompt (skill-documentation tidy, not a narrative/workshop amendment) — landed as named. No narrative or workshop document-history entry required.

---

## Quantitative summary

- **Skills modified:** 1 new (`wolverine-marten-automation`); 0 existing skills edited (both candidate bolt-on homes were read, not modified — the fit-check itself was the deliverable, not a change to either).
- **Rows drained:** 2
- **DEBT.md:** `Open debt` → empty; one new `Recently drained` heading; document history extended.
- **README.md surfaces touched:** 4 (cluster index, tag index across 4 tag rows, entry-point hubs, Phase 2 cross-reference graph + node classification).
- **Out-of-scope items deferred:** Telemetry proto authorship (handoff item 2), the bundling-rule DEBT gap, the `Validate`/`Before` open question, all application code in `src/CritterCab.Dispatch/`.
