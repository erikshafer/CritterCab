# CritterCab v2: dual-evaluation comparison

| | Evaluation A | Evaluation B |
|---|---|---|
| **File** | [`crittercab-v2-evaluation-methodologist.md`](./crittercab-v2-evaluation-methodologist.md) | [`crittercab-v2-evaluation-toolchain.md`](./crittercab-v2-evaluation-toolchain.md) |
| **Lens** | Event Modeling methodologist and open-source maintainer: method fidelity, the recorded design conversation, what a practitioner learns on GitHub, what one maintainer can sustain | Critter Stack toolchain architect: what runs, what is pinned but unused, what the first declared `*.emodel.yaml` and the first Bobcat binding need, Polecat under service-per-BC, the cost of keeping transports and stores honest in CI |
| **Model** | Claude Opus | Claude Fable 5.1 |
| **Shared input** | [`crittercab-v2-evaluation-framing.md`](./crittercab-v2-evaluation-framing.md), received identically; neither evaluator saw the other or knew a second evaluation existed | |
| **Decision owner** | Erik Shafer | |
| **Date** | 2026-10-05 | |

Method: the CritterBids `dual-evaluation` skill (`CritterBids/.claude/skills/dual-evaluation/SKILL.md`). The framing withheld the owner's lean, if any; it also withheld the sibling CritterMart session's progress. Both evaluators verified claims against the live repo at `4d8bde4`.

---

## 1. Where they agree (strong signal)

Convergence is listed only where the two chains reached the same place by different reasoning.

**1.1 v2 is a reset of what the repository claims, not a rewrite of code, and it deserves a boundary.**
A: "There is almost no logic to simplify. What got out of hand is the promise surface" (eleven BCs, three transports, two stores, three SPAs, Go, Redis, three IdPs, Azure); "a practitioner landing on the README needs one unambiguous line between 'what this repo once intended' and 'what this repo is'". B: "The code is a seed, not a system"; what needs versioning is "the declared surface around the code: 19 Accepted ADRs, a v0.8 vision with eleven BCs, a `Directory.Packages.props` with seven pins nothing references, 44 skill directories, and routing files". A argues from the reader; B argues from the build. Same artifact gets the boundary: the claims, not the code.

**1.2 Option 2, in place, carrying Option 3's content; no `v1/` or `archive/` path for the append-only corpus.**
A: "The tag is the archive"; moving files repeats the lesson that "deleting one planning note broke 14 cross-references in 7 files", and "under the portfolio decision the workshops are minutes, and minutes do not become wrong by ageing". B: "everything of value that is not code is cross-referenced by path and by PR number"; a seeded repo "either carries all of it (and then it is the same repo with a new URL and no history) or leaves it behind". Both reject Option 1 for the same structural reason (the cross-reference web) and Option 3 for the same reason (no marker tells a reader which of 19 Accepted ADRs still bind). B adds the cadence argument: the boundary is "the legitimate exception the cadence already admits, applied once, on purpose, with a tag".

**1.3 Option 4 would run the next workshop with an obsolete vehicle.**
A: a seventh markdown workshop "as the record on the very day the portfolio decided markdown is the minutes". B: a workshop "under an `event-modeling` skill that misnames its sources", producing "minutes with no declared file to land in, and then a second session would have to transcribe them". Both keep the handoff's instinct (design return, Driver Profile first) and reject its form.

**1.4 The first new slice is "driver comes online": a Command in Driver Profile whose event is translated into the availability side of Dispatch's `AvailableDriver`.**
A reaches it from the model: "one Command slice, one Translation slice and one existing View", the first modelled on the canvas and the first with scaffolded Gherkin. B reaches it from the code: "the repo already made it", because the placeholder handler "describes the shape of the hole" and "a real end-to-end run correctly yields `NoCandidatesAvailable`" until the second feeder exists. Both make it the slice where Polecat enters.

**1.5 Polecat placement is (a) Driver Profile, in the first arc, at the skeleton PR; Fisher stays out.**
A: Payments-first "would be a package reference leading the model"; Driver Profile is next on every reading and "service-per-BC removes CritterBids' two-main-stores failure by construction". B: Payments-first "has the exact defect ADR-018 rejected in its Option A: a producer with no consumer"; Driver Profile "has a consumer on day one". Both name the SQL Server image in an already-saturated CI pre-pull list as the concrete cost. Both correct ADR-010's "Polecat does not provide event sourcing".

**1.6 Pins, skills and ADRs are admitted only by exercised code; "declared ahead of exercised" is the v1 failure mode.**
A: "no pin without a referencing project, no skill without code that exercises it, and no ADR calling an unbuilt technology 'committed'". B: "Nothing is pinned, configured, wired in the apphost, or described in a skill unless CI exercises it in the same PR or the next one". Both count roughly fourteen code-less skills and seven unreferenced pins; both want the Go output removed from `buf.gen.yaml`.

**1.7 Workshops stay where they are as minutes; transcription to the declared surface is staged by arc and covers realized slices first.**
A: W001 §5.1 to §5.3 and W006 §6.1 and §6.5 first, W002 at the Trips arc, W004 "frozen and not transcribed". B: "Curate the eight realized slices first; add Trips' twelve only when Trips enters the arc, and never transcribe W004 or W005 until their BCs do", because a fully transcribed file would be "the next 'skill written ahead of code'". Different vocabulary, identical sequencing.

**1.8 BC inventory shrinks to about five deployables; Onboarding, Payments, Operations and the never-admitted candidates leave the build list; Rider Profile folds into Identity; Ratings folds into Trips.**
A: five BCs in five deployables, three in the first arc. B: "five hosts with Identity as a possible sixth". Both keep Dispatch, Telemetry, Driver Profile, Trips and some form of Identity. Both are uneasy about Onboarding (A: "the cut I am least comfortable with"; B: "valuable minutes; far from the transport showcase").

**1.9 The `event-modeling` skill needs correcting regardless (author names, pattern vocabulary, information completeness), and Wolverine process-manager words stay out of the pattern slot.**
A documents the colonisation of the pattern slot (W004 "continue handler", W005 `[AggregateHandler]`, W006 "Kafka publication") and the storyboard and completeness gaps. B: "W004's 'continue handler' and 'start handler' are Wolverine process-manager words and must not leak into either the declared file or the pattern slot". Both place the Dymitruk-to-JasperFx mapping in one table.

**1.10 Routing files shrink to routing; no asserted status anywhere.**
A turns the retro lesson into a rule and concludes PR #43 (the stale CLAUDE.md refresh) "a close, not a merge". B: routing files "are short and point at the declared files and the running app". Both read the stale workshop headers as the same disease.

**1.11 Bobcat and CritterWatch 1.1 are unconfirmed; the declared file does not wait on them, the binding and the aggregated derived picture do.**
A: "the first authored model depends on Bobcat, on file conventions from the CritterMart Orders test, and possibly on CritterWatch 1.1". B: "The declared file does not depend on Bobcat; the binding does", and "the derived rung is still 'on' at the descriptor level inside each host; it is only the aggregated picture that waits".

---

## 2. Where they diverge (the interesting differences)

**2.1 Azure Service Bus: demote and carry the first availability edge over Kafka (A) versus keep ASB and build it at the availability slice (B).**
A: "To keep that arc to one new infrastructure variable, I would carry the first availability edge over Kafka, which CI already runs, and defer ASB", conceding it "contradicts ADR-018's ASB half" and needs source verification of outbox-backed Kafka publishing because retro 009's worst defect was "an offline driver left dispatchable". B: ASB "is the transport whose carrying cost in CI is highest", yet "Drop ASB and both ADRs become fiction"; routing availability over Kafka "would quietly demolish the argument ADR-018 was written to make".
*Significance.* This is the largest divergence and it is a values call: CI economy and a single new variable per arc (A) against the integrity of the repo's best teaching artifact and the transport-per-flow-shape thesis (B). It decides whether v2's first arc has two or three infrastructure additions and whether ADR-005, ADR-014 and ADR-018 are amended or merely annotated. Note that A's own fallback ("if that verification fails, ASB comes in with Driver Profile") is B's recommendation, so the question reduces to: does the owner want the availability edge to be the first ASB edge, accepting the emulator and its SQL Edge dependency, or the second Kafka edge.

**2.2 Pricing: fold into Trips (A) versus keep as a tiny service (B).**
A folds it "as the unary gRPC counterparty (the vision's own candidate merge; closes W001 §10 item 1 and §11 candidate 4)". B keeps it "as a tiny service" because it is "the unary gRPC mode; `get_fare_quote.proto` exists; Dispatch already has the port", and maps the four gRPC modes onto services (client-streaming Telemetry, unary Pricing, server-streaming and bidirectional Trips).
*Significance.* Service count five versus six, and whether the primary goal (all four gRPC modes) is demonstrable before Trips exists. A fold loses nothing modelled; a tiny service is a cheap unary host and a cheap place for Fisher if the owner ever changes his mind on Fisher (neither evaluator proposed that; recorded here only as a consequence). Owner's call; it affects the vision's inventory line, not the first arc.

**2.3 The apphost: convert to a `.csproj` in the solution (B) versus not addressed (A).**
B makes it a boundary deliverable: "The AppHost is a project in the solution" so the completeness guard and CI build it, ending the two-week-unnoticed breakage class. A does not mention the apphost at all.
*Significance.* A uniquely toolchain concern that closes the oldest open CI DEBT row and removes the `ManagePackageVersionsCentrally=false` trap. No methodological downside. Should enter the boundary prompt regardless of which recommendation wins.

**2.4 Skills library: remove the code-less skills (A) versus archive them under `docs/skills/archive/` (B).**
A: "What leaves the tree is only what is misleading when read as current: the roughly 14 code-less skills". B: archive, because "the retros cite them by name and nothing in code links to them".
*Significance.* Small. Both remove them from the active library. B's path preserves retro citations; A's relies on the tag. B's is the lower-regret move and is consistent with 1.2's reasoning about cross-references.

**2.5 Sequencing of the first v2 session: boundary PR first (B) versus boundary docs PR, small tidy, then the Driver Profile canvas workshop before any declared file (A).**
A's step 4 defers the Dispatch declared file "after CritterMart reports" so file conventions are inherited. B authors the two declared files inside the boundary PR, "with slice names replacing slice numbers in both files and in the test class names".
*Significance.* Whether CritterCab authors a curated file before the CritterMart Orders experiment settles conventions (per-service placement, naming, what the schema holds). B's residual question concedes the point ("so the three repos agree before a second one commits to it"). The safer sequencing is A's on this item: boundary and tidy now, workshop on the canvas now, first declared file once CritterMart has reported, unless the owner already knows the conventions from the JasperFx side.

**2.6 Retros and cadence: shrink the retro to a PR-body section by default (A) versus keep the retro-per-PR habit (B).**
A: the one-prompt-one-PR-with-retro rule "makes every decision cost three documents"; keep spec delta, design-return interleave, verify-before-wiring and prove-the-regression-fails, and route cross-cutting observations to the methodology log. B lists "the retro-per-PR habit" among the things v2 keeps unchanged.
*Significance.* Maintainer load against evidence quality. The retros were the richest source for this evaluation; the methodology log was the most transferable. A middle path: retro stays mandatory but its template shrinks, with the methodology log as the home for anything cross-cutting.

**2.7 Identity: a deferred third-arc service (A) versus "service status open, may be a per-service ACL library" (B).**
*Significance.* Changes the ceiling by one and reopens ADR-006's Option B (shared identity library), which ADR-006 rejected for the shared-dependency reason. B's framing should be read against that ADR before it is adopted.

---

## 3. Anything only one evaluation caught?

These survive regardless of which recommendation wins and should enter the boundary prompt's scope or the vision draft explicitly.

**Only A caught:**
- The slices are not uniformly slices. W001 §5.5 and §5.9 bundle an Automation into a Command; W004's §6.5, §6.7 and §6.8 are "handler-shaped units, cut where Wolverine's transaction boundary falls" ("atomic triple-emit", "atomic quadruple-emit"). A curated file demands one pattern per slice, so W004 is a re-model, not a transcription, and W001 §5.5 and §5.9 are a re-cut. The corpus has 45 slice sections, not 44.
- Two of Dymitruk's seven steps were effectively skipped: there is exactly one wireframe in the corpus (W001 §5.1), and the information-completeness check is never named or performed; W001 §5.2's Reads-list inconsistency is "exactly the class of defect the completeness check exists to catch". v2's slice definition of done gains a field-level completeness pass and a wireframe or an explicit "no screen: machine actor" statement.
- The narrative layer is "a prose duplicate of the missing storyboard"; make it optional rather than a phase in ADR-004's sequence.
- W006 §6.2 to §6.4 are not Event Model slices; they specify an algorithm. Telemetry's authored model carries §6.1 only; §6.5 belongs to Dispatch's model.
- The design conversation is "mostly a record of ratification": zero of ~20 leans redirected in W003, W005 and W006 pre-resolved by grills. The canvas restores discovery only if the grill is limited to boundary questions (methodology log entry 006's second axis).
- The methodology log is the most transferable artifact and the first candidate for the portfolio's "one home".
- "No asserted status" makes PR #43 a close, not a merge.
- A single "released forward-constraints and retired candidates" note is needed when the BC list folds; W004 alone generated 12 promises to five BCs that may never exist.
- Do not introduce Dilger's vocabulary into CritterCab; keep the crosswalk at portfolio level, as correspondence, never synonymy.
- The test-class naming DEBT row dissolves once binding is by attribute.

**Only B caught:**
- The declared file is per service because the derived rung is per host and `docs/rules/structural-constraints.md` forbids cross-service references; the cross-service slice appears in both files as a Translation slice referencing the other by name; the context map remains the edge record.
- Slice names must replace slice numbers (in the file and in test class names) before anything can merge by name; W006 §6.5 is a Dispatch slice numbered inside the Telemetry workshop.
- What the derived rung cannot show and the declared file must carry: `AvailableDriver` is a document, not a projection; the throttle grain; the stubs; the forward constraint; recall-versus-correctness.
- First binding is W001 slice 5.1 (HTTP plus Marten, no broker); second is the cross-service slice, to prove one named slice can span two hosts and two test assemblies.
- The ASB emulator itself depends on SQL Edge, so "ASB only" is never one container.
- CritterWatch is what aggregates per-host derived rungs into one picture under service-per-BC; the rules file still forbids RabbitMQ with no ADR-017 carve-out.
- The apphost should be a `.csproj` in the slnx; the pre-pull list should be generated or checked by a test; add dependabot and branch protection; bump Wolverine to the current 6.3x line with the gRPC and Kafka suites as the gate and the client-streaming behaviour re-verified.
- The Polecat skill is written against 3.x and upstream is 5.x; every Polecat API in it is unverified.
- The `Directory.Packages.props` comment annotating the SignalR client "for Relay BC push assertions" refers to a CritterBids BC that appears in no CritterCab workshop.
- ADR-003's supersession trigger is arguably fired by Bobcat and the declared rung, licence permitting (A says it has not fired because Stoat is BSL; the two read the trigger against different products).

---

## 4. Summary for the decision

| Question | Answer from the evaluations |
|---|---|
| Is v2 meaningful at 2 of 11 BCs? | Unanimous: yes, as a reset of claims and conventions around a design corpus that is the asset; the code is a seed and survives unchanged. |
| New repo, in place, remaster, or continue? | Unanimous: Option 2 in place behind a tag and a vision v1.0, carrying Option 3's content; no file moves for the append-only corpus; Option 1 strands the cross-reference web; Option 3 leaves no marker; Option 4 runs the next workshop with an obsolete vehicle. |
| What happens to the old repo? | Unanimous: nothing moves; a tag marks the end of v1; the two stale tidy branches are merged or closed; B adds branch protection and dependabot. |
| Which BCs survive, fold, drop; service count? | Converged: Dispatch, Telemetry, Driver Profile (Polecat), Trips, Identity (form open); Rider Profile into Identity; Ratings into Trips; Onboarding, Payments, Operations and the never-admitted candidates out of the build list. **Split** on Pricing: fold into Trips (A) or tiny unary service (B). Five or six hosts. |
| What v1 must not repeat? | Unanimous: declared ahead of exercised (pins, skills, ADRs, status prose); asserted status; the pattern slot colonised by framework words. A adds: skipped storyboard and completeness steps; ratification instead of discovery; apparatus outweighing product. B adds: apphost invisible to CI; unenforced pre-pull list. |
| What v1 must keep? | Unanimous: GWT as the contract that maps to tests; seams one PR early; spec-delta loop; verify-before-wiring at prompt authoring; two-axis code review; HEAD logging of verified sources; context-map cadence; the modeling-shape taxonomy; "do not fabricate availability". |
| Tech: stays, goes, added? | Unanimous: gRPC, Kafka, Marten, H3, Alba, Testcontainers, Aspire stay; Redis, Go output, SignalR pins, SPAs, Azure deployment go or park; Polecat, CritterWatch (licence permitting), Bobcat, `*.emodel.yaml` per service added. **Split** on ASB (2.1) and on the apphost conversion (B only). |
| Event Modeling from the first commit? | Unanimous: Driver Profile is the first chapter modelled on the canvas, exported and imported; curated file is the record; existing workshops are minutes; realized slices transcribed first; W004 not transcribed; `event-modeling` skill corrected first. A adds wireframes and field-level completeness to the slice definition of done; B adds slice naming and per-service files. |
| First slice and its prompt? | Unanimous on the slice: "driver comes online" (Driver Profile Command, Translation into Dispatch's availability side). Unanimous that a boundary session precedes it. **Split** on whether the first declared files are authored in the boundary session (B) or after CritterMart reports (A). |
| Polecat placement? | Unanimous: (a) Driver Profile, first arc, at the skeleton PR, after the workshop and before the slice; Fisher out. |

**Residual questions only the owner can close.**

1. ASB: first availability edge on Kafka with ASB deferred (A), or ASB built at the availability slice with the emulator cost accepted (B)?
2. Pricing: fold into Trips or keep as a tiny unary service?
3. When is the first declared file authored: in the boundary session, or after the CritterMart Orders experiment settles conventions?
4. Is Onboarding frozen, or is it the portfolio's lived Process Manager via Handlers example and therefore a later arc with W004 re-modelled?
5. Identity as a host or as an ACL inside each service (read against ADR-006's rejected Option B)?
6. Go polyglot: parked with a trigger, or dropped?
7. CritterWatch licence and the RabbitMQ backplane: renew and wire, or accept per-host descriptors without the aggregated picture for now?
8. Retro cadence: keep retro-per-PR as is, or shrink the template with the methodology log as the cross-cutting home?
9. Does "v2" appear publicly (tag name, README wording), or is the tag named for what it marks?
10. Methodology substrate: one portfolio home, vendored with a lockfile like the Pocock skills?

---

## 5. Decision (owner, date)

### Decision (Erik Shafer, 2026-10-05)

**Decided:** Option 2, v2 in place, carrying Option 3's content, exactly as both evaluations converged on in §1. Tag v1 at `4d8bde4`; vision v1.0; nothing in the append-only corpus moves. Residual questions 1 to 3 closed: ASB stays and is built at the availability slice (B's position, accepting the emulator cost for ADR-018's sake); the first declared files are authored after CritterMart's Orders experiment reports (A's sequencing), with slice renaming in the boundary; Pricing folds into Trips (A). Polecat: (a) Driver Profile at the skeleton PR; Fisher out. Residual questions 4 to 10 are parked to the boundary prompt's owner calls. The full accepted-pieces list is recorded in `docs/planning/2026-10-05-crittercab-v2-evaluation.md` § Decision, which is the citable record; this section points there rather than restating it.

---

## Document history

- **v0.2** (2026-10-05): §5 Decision recorded; residual questions 1 to 3 closed, 4 to 10 parked.
- **v0.1** (2026-10-05): Authored after both evaluations completed. No decision recorded.
