# CritterCab v2 evaluation: methodologist and maintainer lens

| | |
|---|---|
| **Decision** | Should CritterCab become a v2, and in what form (new repo, in place, remaster and continue, continue as is)? |
| **Lens** | Event Modeling methodologist and open-source maintainer: method fidelity, the design conversation the artifacts record, what a practitioner landing on GitHub learns, what one maintainer with two sibling repos can sustain |
| **Model** | Claude Opus (independent subagent; did not see the toolchain evaluation or know one existed) |
| **Shared input** | [`crittercab-v2-evaluation-framing.md`](./crittercab-v2-evaluation-framing.md), received identically by both evaluators |
| **Decision owner** | Erik Shafer |
| **Date** | 2026-10-05 |
| **Method** | CritterBids `dual-evaluation` skill; comparison at [`crittercab-v2-evaluation-comparison.md`](./crittercab-v2-evaluation-comparison.md) |

Text below is the evaluator's output, unedited except for this header.

---

## Decision framing

The question is whether CritterCab should become a "v2" in the CritterSupply to CritterMart sense (a deliberate version that acts on lessons, folds bounded contexts, simplifies what got out of hand and drops what did not earn its keep), and in which of four forms: a new repository, a declared boundary in place, an unversioned remaster, or continuing from the 2026-07-25 handoff. I evaluate it as an Event Modeling methodologist and as a solo open-source maintainer with two sibling repos. I treat the design corpus as the primary asset and ask the honest fate of each piece.

**Recommendation in one line:** an argued hybrid of Options 2 and 3. Declare v2 in place with a tag and a vision v1.0, keep the append-only corpus where it sits, cut the bounded-context inventory from eleven to five, and make the first new slice "driver comes online" in a new Driver Profile service.

I verified the framing against the repo at `4d8bde4` (clean `main`, zero tags, two remote tidy branches, no `*.emodel.yaml` or `.feature` file anywhere). I spot-checked all six workshops' slice walks, `docs/research/methodology-log.md`, the last handoff, `docs/skills/README.md`, `docs/skills/DEBT.md`, the `event-modeling` skill, and ADRs 002, 003 and 010. Where I rely on the framing without re-checking, I say so.

## Evaluation points

### 1. At 2 of 11 bounded contexts, "v2" is a reset of scope and conventions, and that still deserves a boundary

The CritterMart lineage condensed systems that had been built. CritterCab has about 2,300 lines of service code against 641 KB of workshop markdown (my `wc -c` on `docs/workshops/`) and, per the framing, roughly 18,300 lines of skills. There is almost no logic to simplify. What got out of hand is the promise surface:

- eleven tentative BCs in `docs/vision/README.md`, plus Trust & Safety and Notifications introduced by workshops but absent from the inventory;
- three transports, two stores, three SPAs, Go, Redis, three identity providers and an Azure deployment;
- a skills library whose own README declares "All five phases complete as of 2026-05-06" (`docs/skills/README.md` §Status), seven weeks before the repo's own state-of-the-repo note recorded that the showcase premise was about 0% demonstrated (`docs/planning/2026-06-25-state-of-the-repo-transport-and-critterwatch.md`, line 15).

So the asset is the design corpus and v2 is a reset of what the repo promises. A scope cut from eleven BCs to about five is not a tidy, though. A practitioner landing on the README needs one unambiguous line between "what this repo once intended" and "what this repo is". That rules out Option 3 as stated and points to a boundary that costs almost nothing: a tag and a vision v1.0.

### 2. The slices are not uniformly slices, which determines how the six workshops transcribe

This is the finding that matters most for the portfolio decision. The repo's own guide states the rule: each scenario is tied to exactly one command or one view (`docs/research/event-modeling-workshop-guide.md`, Step 7 at line 180 and the restatement at line 267). The workshops drift from it progressively.

W001 mostly holds the line. §5.1 has a pattern, a command table with a Source column, the emitted event, the views fed and three GWT sketches. Even so, §5.5 is recorded as a Command "with a cascaded multi-event emission that subsumes what would otherwise be a separate Automation", and §5.9 as "Two distinct automations reaching a shared terminal event".

W004 abandons the unit. §6.5 is a "Continue handler" plus a Translation-out plus a Translation-in pair across three events. §6.7 is an "Atomic triple-emit". §6.8 is an "Atomic quadruple-emit". These are handler-shaped units, cut where Wolverine's transaction boundary falls. Dymitruk's slice boundary is one state change or one view.

A count discrepancy points the same way. `docs/workshops/README.md` describes W004 as 10 base slices plus one sub-slice, and the framing counts 11. The file has twelve slice headings (6.1 to 6.10 plus 6.4b and 6.7b). The corpus total is therefore 45 slice sections, not 44, and the number of true slices after a re-cut is higher still.

A curated `*.emodel.yaml` will demand one pattern per slice. Transcription is therefore:

- near-mechanical for W002, W005 and the realized W001 §5.1 to §5.3;
- a re-cut for W001 §5.5 and §5.9;
- a re-model for W004.

### 3. The pattern slot was colonised by framework and transport vocabulary

The workshops declare Dymitruk's four patterns at the top of each slice walk (W001 §5 preamble: "pattern type (Command / View / Automation / Translation)"). By W004 the slot reads "Command (continue handler)" (§6.2, §6.3) and "Translation-in (Klefter) + start-handler" (§6.1). W005 §6.3 puts `[AggregateHandler]` / `FetchForWriting` in the pattern line. W002 §6.6 puts Wolverine `Events` + `OutgoingMessages` there. W006 goes furthest: "Kafka publication of a high-volume telemetry stream" (§6.3) and "Overwrite-in-place document (location-of-record) + periodic eviction" (§6.4).

The retros record the lesson "workshop vocabulary wins over framework naming". The workshops did not apply it to their own pattern slot.

The portfolio decision makes the fix structural. The authored surface carries Dymitruk's four pattern names and nothing else. JasperFx's descriptor terms appear only in the rung derived from the running app. The markdown minutes may say what they like.

I would not introduce Dilger's State Change / State View vocabulary into CritterCab. It appears nowhere in the corpus (confirmed by grep). Adding a second pattern vocabulary to minutes that already blend a third would cost precision and buy nothing. If the portfolio wants a vocabulary crosswalk, it belongs in one portfolio-level document, stated as a correspondence and never as synonymy.

### 4. Two of Dymitruk's seven steps were effectively skipped, and one check was never run

**Storyboard (step 3).** There is exactly one wireframe in the whole corpus, W001 §5.1 (my grep for wireframe headings returns 1, 0, 0, 0, 0 across the five EM workshops). W002's driver-app and rider-app commands have no screen. W004's applicant, adjudicator and operator have none. For a method whose premise is that screens expose missing information, that is a real loss, and it explains why narratives were bolted on as a separate layer.

**Information completeness.** The term does not occur in any workshop, and the `event-modeling` skill never names it. The command side is done informally through W001's Source column. The view side stops at event-level "Feeders" (for example W005 §6.4), not at field level.

**Unresolved inconsistencies.** The framing reports W001 §5.2's Reads-list inconsistency as unresolved since May. That is exactly the class of defect the completeness check exists to catch.

A v2 slice's definition of done should therefore include a field-level completeness pass, and a wireframe or an explicit "no screen: machine actor" statement.

### 5. Telemetry is honestly modelled as "not an event-sourced BC", and that should be said more plainly

W006 §3.2 decides that Telemetry is stream-processing with one event-sourced stream (§3.4). Slices 6.2 to 6.4 store no events, and W006 §6.2 says so directly. That is a correct domain call, well argued by the grill resolutions.

The methodological consequence is that slices 6.2 to 6.4 are not Event Model slices. They are a design note for a processing component. Their GWT format differs from the other workshops because they are specifying an algorithm (the `shouldPublish` predicate), not a state change.

W006 §6.5 is by its own heading a W001 §5.3 amendment: a Dispatch Translation slice feeding a Dispatch view.

The honest v2 treatment:

- Telemetry's authored model carries the one Command slice (§6.1, `TelemetryPolicyConfigured`).
- Dispatch's model carries the Translation and View of §6.5.
- §6.2 to §6.4 stay as minutes and tests.

Forcing them into the model to make the slice count look complete would be toolchain enthusiasm in methodological dress.

### 6. The design conversation is well recorded, but mostly a record of ratification

The corpus is unusually candid about this. W003's retro records that none of the roughly 20 proposed leans was redirected, and names "lean-confirmation rather than independent-domain-expert mode" as a live alternative explanation (`docs/retrospectives/workshops/003-onboarding-domain-storytelling.md`, line 88). W005 and W006 open by stating that all design leans were pre-resolved by the grill-with-docs pass. W006 had no prompt at all, because the grill note was the prompt.

The grill is the best single technique in the repo. But a workshop in which every call is pre-resolved is a transcription session, and the minutes then record assent.

The portfolio decision gives v2 a way to restore discovery: the canvas is the modelling room for new chapters. The Driver Profile chapter should be walked on the canvas with the grill limited to boundary questions (methodology log entry 006's second axis). Slice discovery should happen on the board.

### 7. The methodology log is the most transferable artifact in the repo

Six entries, each with a trigger, an observation, and stated confirm and disconfirm conditions. Entry 005 names its own confound (grilling intensity versus Domain Storytelling as upstream). Entry 006 refines it into the two-axis routing heuristic: vocabulary richness routes to Domain Storytelling, boundary nuance routes to a grill. A practitioner learns more from these four pages than from the 215 KB Phase 5 retro.

Two things follow:

- **Entries 001 to 003 are about rendering, not modelling.** They concern the narrative layer: weighting of Context/Interaction/Response and the two fidelity layers. Narratives exist for W001 only, were skipped for Telemetry by recorded decision, and do not exist for Trips, Onboarding or Identity. The narrative layer is a prose duplicate of the missing storyboard. In v2 it should stop being a phase in the sequence and become optional. The two existing narratives stay as exhibits.
- **The log should not stay CritterCab's.** It outlives any repo boundary and is the first candidate for the portfolio's "one home" question.

### 8. Asserted status is the main source of drift, and the portfolio rule already bans it

Three of five EM workshop headers are stale. I confirmed:

- W004 and W005 both say "In progress (v0.1)" although both are closed.
- W001's header says v0.4 while its Document History runs to v0.8, with v0.7 listed before v0.6.
- `docs/workshops/README.md` still calls W001 "Complete (v0.3)".

Document History entries assert "now have runnable Alba coverage". The workshops index carries a done/pending table. The README, CLAUDE.md and AGENTS.md assert versions and progress that are false (framing, Drift at HEAD). The retro lesson is already written: routing-layer files decay silently while append-only artifacts stay honest.

"No asserted status in any file" turns that lesson into a rule. Realization is read from the derived rung and from Bobcat bindings. v2 should delete status prose and not refresh it. That makes PR #43 (the CLAUDE.md status refresh the handoff calls "doubly stale") a close, not a merge.

### 9. Design ran ahead of code and left a debt graph, not just a backlog

Methodology log entry 004 celebrates forward-constraints between workshops. The other side is `docs/workshops/README.md`: W004 alone generated 12 forward-constraints across five unmodeled BCs (Identity, Driver Profile, Notifications, Trust & Safety, Operations). Around them sit pending proto authorship for Trips, Onboarding, Identity and Telemetry, five pending ADR candidates, and a pending mid-trip cancellation workshop.

Every one is a promise to a BC that may never exist. Under the v1 conventions they can never be closed, only carried.

A v2 that folds the BC list must release them explicitly, in one place. Otherwise the next maintainer (or the next agent session) will treat them as live.

### 10. The apparatus outweighs the product, and the cadence rules produced that outcome

Over the repo's life there were 8 implementation PRs out of 44 (18%, per the framing). The corpus holds 33 retros, 32 prompts and 10 handoffs. The skills library has about 14 skills describing technology with no code behind them, and its own retros record that skills authored ahead of code "contradicted each other and the APIs from day one".

The `event-modeling` skill is the sharpest example. It files Translation and Automation under "Adjunct Patterns" sourced to "Filip Klefter" and "Anders Bruun Olsen" (`docs/skills/event-modeling/SKILL.md`, lines 133 to 137). The repo's own research note correctly names Marc Klefter and Jake Bruun, and correctly says both patterns are Dymitruk's 2019 primitives that those authors extend (`docs/research/event-modeling-canonical-sources.md`, lines 54 to 55, 141 to 143, 249 to 250). A method skill that contradicts the method research two directories away is the cost of authoring ahead of use.

"One prompt, one session, one PR with the retro inside" is a sound rule that scaled badly for one person. It makes every decision cost three documents. v2 should keep:

- the spec delta and the design-return interleave;
- verify-before-wiring;
- prove-the-regression-test-fails.

It should shrink the retro to a section of the PR body unless the session produced a cross-cutting observation, which goes to the methodology log.

### 11. What v1 got right, and v2 must not lose

**GWT as the contract held.** Test method names in `tests/CritterCab.Dispatch.Tests/CandidateSelection/Slice53CandidatesSelectedTests.cs` correspond one-to-one with W001 §5.3's three named scenarios (W001 Document History, v0.7). That is the precondition for `[BobcatSlice]` binding of existing tests, and it is already met.

**Realization amended the model.** W006's Document History records the amendments made as its slices were realized (five across the workshop, per the framing). The handoff shows the model learning from code and still refusing to pre-empt another BC's vocabulary. The `DriverAvailabilityChanged` placeholder "describes the shape of the hole".

**Other keepers:** the W002 §3 aggregate-identity sidebar, the same-PR context-map cadence (four exercises), the recorded narrative skip, and "do not fabricate availability to keep a demo alive". These are what a practitioner should find quickly.

### 12. Technology should be admitted by the model, then by a project reference

The Event Model does not name a store or a broker. So the test I apply is: does a modelled, in-scope slice need this capability, and is it CritterCab's to own in the portfolio map?

| Technology | Modelled where | Verdict for v2 |
|---|---|---|
| gRPC client-streaming, Kafka, H3 | W006 §6.2 to §6.5, built | Stay |
| gRPC unary (fare quote) | W001 §5.2, stub | Stay; realized when Trips hosts Pricing |
| gRPC server or bidirectional streaming | W001 §5.4 to §5.7 (offers) | Stay; Dispatch's next arc |
| Polecat | Not a model concern; portfolio orphan | Add, at the Driver Profile skeleton (point 13) |
| Azure Service Bus | W001 §5.10, W002 §6.9 and §6.10 | Demote from "committed" to deferred until Trips publishes |
| SignalR and three SPAs | One wireframe in the corpus | Park; screens return to the model first |
| Redis | Nothing | Drop |
| Go polyglot | Nothing (`buf.gen.yaml` output unused) | Park explicitly; owner call |
| Entra, OpenIddict, Keycloak | W005, unrealized | Deferred with Identity |
| Azure deployment (ADR-007) | Nothing; no URL is required | Park |
| CritterWatch plus RabbitMQ backplane (ADR-017) | Derived rung | Owner question (licence) |

The seven pinned-but-unreferenced packages should go. v2's rule should be: no pin without a referencing project, no skill without code that exercises it, and no ADR calling an unbuilt technology "committed".

Several accepted ADRs need amendment in a later session:

- ADR-002: its 6-to-8 count, and the dangling "ADR-010 finalizes the service topology" (lines 42 and 54).
- ADR-004: the phase sequence.
- ADR-005: ASB's status.
- ADR-007: the reachable-URL clause.
- ADR-010: line 36, "Polecat does not provide event sourcing".
- ADR-016: the frontend commitment.

ADR-003's supersession trigger has not fired. Stoat is BSL and commercial, not permissively licensed, and the portfolio decision excludes a build-kit loop in any case.

### 13. Polecat: choice (a), in the first arc, at the Driver Profile skeleton

I take (a) over (b) on a methodological ground. Payments has no workshop, no narrative and no slice. Choosing a store for it would be a package reference leading the model. Driver Profile is the next chapter to be modelled on every reading of the handoff. It is plain business events (availability transitions, vehicle changes). It is small, and service-per-BC removes CritterBids' two-main-stores failure by construction.

The store is chosen when a service skeleton is created, and the cadence rules already allow "skeleton plus first slice" as one PR. So Polecat enters with Driver Profile's first slice, not earlier and not as a retrofit.

The cost is concrete: a SQL Server image in a CI pipeline that already pre-pulls three images serially because of registry saturation, with an unenforced pull list (handoff, "Things not captured", item 1).

To keep that arc to one new infrastructure variable, I would carry the first availability edge over Kafka, which CI already runs, and defer ASB. That contradicts ADR-018's ASB half. It also needs source verification that Wolverine's durable outbox can back a Kafka publish for this edge. ADR-019's table records "Kafka no outbox" as CritterCab's current choice, and retro 009's worst defect was an offline driver left dispatchable, so delivery guarantees matter here. If that verification fails, ASB comes in with Driver Profile and the arc is simply larger.

## Bounded contexts: survive, fold, drop

| BC (v1 inventory) | v2 fate | Deployable |
|---|---|---|
| Dispatch | Survives; core | Dispatch |
| Telemetry | Survives; stream-processing | Telemetry |
| Driver Profile | Survives; first new chapter; vehicle sub-domain folds in | Driver Profile (Polecat) |
| Trips | Survives; second arc | Trips |
| Pricing | Folds into Trips' deployable as the unary gRPC counterparty (the vision's own candidate merge; closes W001 §10 item 1 and §11 candidate 4) | Trips |
| Identity | Survives, deferred to a third arc | Identity |
| Rider Profile | Folds into Identity's deployable (vision candidate merge; W005 grill 1 keeps profile ownership logically distinct) | Identity |
| Onboarding | Dropped from build scope; W003 and W004 kept as the DS-to-EM exhibit | None |
| Payments, Ratings, Operations | Dropped | None |
| Trust & Safety, Notifications, Documents | Never admitted | None |

That is five BCs in five deployables at full extent, and three services in the first arc. Operations loses nothing real: every operator action already modelled is a config-as-events Command slice inside its owning BC (W001 §5.11, W002 §6.11, W004 §6.10, W006 §6.1).

Dropping Onboarding's build is the cut I am least comfortable with. It is the repo's richest methodology content. It is also the largest unrealized logic (process manager, FCRA two-phase notice, two vendor ACLs, 194 KB) and the workshop that needs a full re-model (point 2).

## The fate of the six workshops under the portfolio decision

All six remain where they are as minutes. None is edited beyond a single closing Document History line. None is moved.

- **W001:** transcribed in stages. §5.1 to §5.3 first, with existing tests bound. §5.4 to §5.10 at Dispatch's next arc, with §5.5 and §5.9 re-cut, the §5.2 Reads inconsistency resolved and each of the 13 open parking-lot items given a disposition. §5.12 arrives with Trips.
- **W002:** held intact and transcribed at the Trips arc. It is the cleanest and the best candidate for a canvas import if the portfolio wants one.
- **W003:** complete and frozen as the Domain Storytelling exhibit.
- **W004:** frozen and not transcribed. If Onboarding returns, it is a new chapter on the canvas with W004 as input.
- **W005:** held and transcribed at the Identity arc.
- **W006:** §6.1 goes into Telemetry's model, §6.5 into Dispatch's, and §6.2 to §6.4 remain minutes.

The curated file should point to its minutes for provenance. The workshop must not point forward with a status line, which would reintroduce asserted status.

## Recommendation

**Option 2 in place, executed with Option 3's restraint.** The repository, its issue and PR history and its URL stay. A tag marks v1 at the current head once the two open PRs are disposed of (merge #44, close #43). The vision moves to v1.0 with the five-BC inventory, goals re-ranked (Event Modeling practice first, product showcase second) and the technology list cut per point 12.

I depart from Option 2 on the `v1/` or `archive/` path. The append-only corpus does not move:

- The repo's own experience is that deleting one planning note broke 14 cross-references in 7 files.
- The retros and handoffs cite each other and PR numbers densely.
- Under the portfolio decision the workshops are minutes, and minutes do not become wrong by ageing.

The tag is the archive. What leaves the tree is only what is misleading when read as current: the roughly 14 code-less skills, the seven unreferenced pins, the unused Go generator output, agent configs whose globs point at nothing, and all status prose in routing files.

**Why not Option 1.** A new repository strands the one thing that makes the corpus trustworthy, its linkage to PR history. It adds a fourth repo to a portfolio already carrying three Wolverine pins, three skill libraries and two DEBT ledgers. And it implies the v1 code was wrong, when its 57 test cases include mutation-verified guards and a real cross-service flow. Seeding a new repo "from the parts that survive" would in practice copy both services and most of `docs/`, which is Option 2 with broken links.

**Why not Option 3 as stated.** Without a boundary, the vision becomes v0.9 and the eleven-BC promise erodes ambiguously. The released forward-constraints have no single place to be released. A visitor cannot tell which of 19 Accepted ADRs still bind.

**Why not Option 4.** The Driver Profile workshop, run under v1 conventions, would produce a seventh markdown workshop as the record on the very day the portfolio decided markdown is the minutes. A `tidy: skills` session would polish a library a third of which describes unbuilt technology. Both add to the apparatus that point 10 identifies as the problem. The handoff's instinct (design return, Driver Profile first) is right. Its vehicle is obsolete.

## Costs and risks

- **Cosmetic boundary.** A tag and a vision bump are cheap, and the apparatus could regrow. The only real guard is the admission rule in point 12 and a retro format that is small by default.
- **Clone and context weight.** The corpus stays at full weight in the clone and in agent context. The routing files must direct agents away from v1 prompts and retros unless asked. This is a real, continuing cost of not moving files.
- **Stranded work.** Freezing Onboarding strands about 235 KB of the best work and the only DS-to-EM pair. The second data point that methodology log entry 005 asks for may never arrive in this repo.
- **Thinner domain.** Five BCs makes CritterCab a less "realistic" ride-sharing system. There is no payment, no rating and no onboarding. A reader looking for breadth will find CritterMart or CritterBids broader.
- **Tooling outside the owner's control.** The first authored model depends on Bobcat, on file conventions from the CritterMart Orders test, and possibly on CritterWatch 1.1. The framing could not confirm that any has shipped. If they slip, CritterCab's v2 has a boundary and no executable layer.
- **Ratio gets worse first.** The reset is two or three more non-implementation PRs on a repo dormant since July whose ratio is already 18% implementation.
- **CI fragility.** Polecat brings SQL Server into CI, which the handoff describes as already marginal on a 2-core runner.
- **ADR re-opening.** Deferring ASB re-opens ADR-005, ADR-014 and ADR-018, which were carefully argued. Six ADRs sit Accepted but known-wrong until the amendment session happens.
- **Solo ratification.** The canvas changes the room, not the number of people in it.

## What I would do first

1. **Close the boundary, in at most two PRs with no slice work in them.** Dispose of #43 and #44 and tag. Then one docs PR:
   - vision v1.0;
   - a README with no status prose;
   - CLAUDE.md and AGENTS.md reduced to one source with no version claims;
   - a single "released forward-constraints and retired candidates" note listing every promise to a dropped BC;
   - the workshops index rewritten to state the minutes rule once;
   - the ADR index annotated with the six due for amendment (authored in a later session).
2. **A small tidy PR.** Remove the unreferenced pins and code-less skills, and replace or correct the `event-modeling` skill. It also settles the test-class naming DEBT row: binding by attribute makes the `Slice{N}` name unnecessary as a mapping device, so the skill's rule can stand or fall on taste.
3. **Model Driver Profile on the canvas as a new chapter.** Precede it with a short grill restricted to the two boundary questions the handoff names: the four transitions versus the collapsed placeholder, and the availability-before-location drop. Include wireframes for the driver's go-online screen and a field-level completeness pass.
4. **Author the Dispatch model after CritterMart reports.** Once the CritterMart Orders test has settled the file conventions, author the curated model for the already-realized slices (W001 §5.1 to §5.3, W006 §6.1 and §6.5) and bind the existing tests.

**The first slice of v2 is "driver comes online".** A Command in Driver Profile produces `DriverCameOnline`. It is published and translated into the availability side of Dispatch's `AvailableDriver`. W001 §5.3's happy-path scenario then passes end to end with real positions and real availability, where today a real run correctly yields `NoCandidatesAvailable`. It is one Command slice, one Translation slice and one existing View. It is the first slice modelled on the canvas, the first with scaffolded Gherkin, and the first on Polecat.

## Residual questions only the owner can close

1. Is Onboarding meant to be the portfolio's lived Process Manager via Handlers example? If so, it moves from frozen to the third arc, ahead of Identity, and W004 is re-modelled.
2. Is ASB demoted to deferred, with the first availability edge on Kafka? Or does ADR-018's ASB half stand and enter with Driver Profile, despite the larger first arc?
3. Is Go polyglot parked or dropped? It is listed as a CritterCab capability in the portfolio map and has no model element behind it.
4. Does the always-on derived rung require CritterWatch in CritterCab, and therefore a licence and the RabbitMQ backplane of ADR-017? Or is there a lighter producer?
5. Where is the one home for the methodology substrate (the methodology log, the `event-modeling` skill, the nine duplicated skills)? Does CritterCab stop carrying a local copy as part of v2?
6. Is it acceptable that Payments, and with it any home for Fisher, has no place in CritterCab?
7. Will there be any frontend within v2's first year? If not, are wireframes in the model the only screens?
8. Is a reachable deployment still a goal, or does ADR-007 become dormant?
9. Would you invite an outside Event Modeling practitioner to review one chapter per arc, as a counter to lean-confirmation?
10. Have Bobcat and CritterWatch 1.1 shipped, and has the CritterMart Orders test concluded? Step 4 above waits on those answers. Steps 1 to 3 do not.
11. Does "v2" appear publicly at all? One alternative is a tag named for what it marks (for example a v1 design-phase tag), with the README simply describing the repo as it now is.
