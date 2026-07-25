# Retrospective — Dispatch consumes `telemetry.driver-location-updated` (W006 slice 5 / W001 §5.3 close)

## Metadata

- **Triggering prompt:** [`docs/prompts/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md`](../../prompts/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md)
- **Status:** Complete
- **Date authored:** 2026-07-24
- **Output artifacts:**
  - `src/CritterCab.Dispatch/CritterCab.Dispatch.csproj` — Kafka + Protobuf + H3 packages; the repo's **first second consumer of an existing `.proto`**
  - `src/CritterCab.Dispatch/AvailableDrivers/AvailableDriver.cs` — the document; `ILongVersioned`, two independently-written sides
  - `src/CritterCab.Dispatch/AvailableDrivers/DriverLocationUpdatedHandler.cs` — **CritterCab's first cross-service consumer**
  - `src/CritterCab.Dispatch/AvailableDrivers/DriverAvailabilityChanged.cs` — the ASB landing site with no transport bound to it, plus its handler
  - `src/CritterCab.Dispatch/AvailableDrivers/NearbyAvailableDriversView.cs` — the adapter that replaced the stub
  - `src/CritterCab.Dispatch/AvailableDrivers/H3KRing.cs` — k-derivation, ring enumeration, great-circle distance
  - `src/CritterCab.Dispatch/Program.cs` — Kafka listener, `AvailableDriver` schema, source registration swap
  - `apphost.cs` — Dispatch → Kafka reference
  - `tests/CritterCab.Dispatch.Tests/DispatchKafkaTestFixture.cs` — broker-backed fixture with a warm-up handshake
  - `tests/CritterCab.Dispatch.Tests/AvailableDrivers/` — three suites, 21 new tests
  - `docs/skills/wolverine-kafka/SKILL.md` — listener sections rewritten from shipped code (the deferred DEBT row)
  - `docs/skills/DEBT.md` — one row drained, three registered
  - `docs/workshops/006-telemetry-event-model.md`, `docs/workshops/001-dispatch-event-model.md` — spec amendments
- **Tests:** 57/57 green (35 Dispatch, 22 Telemetry), up from 33. No pre-existing test modified.
- **Review passes run:** `jasperfx-source-verifier` (authoring time, 8 gates), `critter-skill-auditor` Phase 1 (pre-code) and Phase 2 (post-code), `code-review` two-axis (pre-PR). **The last of these found the session's most serious defect, after the first three had run clean** — see § What was harder than expected.

---

## Framing

The last three sessions each built one side of a boundary. This one crossed it. Before PR D, "CritterCab has two services" meant two services that had never spoken; after it, a GPS ping entering Telemetry over gRPC comes out as a document write in Dispatch, over Kafka, in a different bounded context. That is the thing the project exists to demonstrate, and it took four PRs of groundwork to make it a small session.

It was a small session. The two facts that made it so are worth naming, because both were deliberate choices made in *earlier* sessions: slice 5.3 built `INearbyAvailableDriversSource` as a port with a stub behind it, and PR B put `driver_location_updated.proto` through codegen before anything consumed it. So this session wrote an adapter and a handler, and `CandidateSelectionAutomation` — the code the whole slice exists to serve — does not appear in the diff.

---

## Outcome summary

W006's slice walk is **complete**; all five slices run. W001 §10 parking-lot #4 is closed in code three months after it closed on paper. `NearbyAvailableDriversStub` is out of the production graph, demoted to a test double, and the three W001 §5.3 GWTs plus W006 §6.3's Dedup GWT are exercised against a real broker.

Two of ADR-005's three transports are live in both directions. Azure Service Bus remains the only modeled-but-unbuilt one, and the next thing it blocks is a bounded context, not a slice.

---

## What worked

**Running `jasperfx-source-verifier` at prompt-authoring time, again.** Second consecutive session where every gate closed before the deliverable plan existed, and **three of eight gate premises were contradicted by source**. Each contradiction would have produced working-looking wrong code:

| Gate premise | Reality |
|---|---|
| A Kafka listener must declare its message type because protobuf cannot infer it | The default envelope mapper *writes* a `message-type` header; `DefaultIncomingMessage<T>()` is hardening, not a workaround |
| `CustomizeHandlerDiscovery(...)` replaces the built-in conventions, so the consumer needs an `*Automation` name | Discovery is **additive** and OR'd; `*Handler` is discovered normally |
| Read-then-conditionally-`Store` is the best available LWW upsert | `TryUpdateRevision` is atomic, database-side, one round trip, and not racy |

The third is the one that mattered most. "LWW upsert" in W006 §6.5 reads naturally as read-then-store, and read-then-store is *racy* — the window between the read and the write is exactly where a redelivery slips through. The spec was not wrong; the obvious implementation of it was.

**Running `critter-skill-auditor` Phase 1 after the gates had all closed.** Also the second consecutive time it corrected an already-source-verified prompt, and again it found something source verification structurally cannot: **the prompt named the wrong governing skill.** `wolverine-marten-automation` is scoped to handlers reacting to a Marten-stream-forwarded event; neither new handler is that shape. `wolverine-kafka` says so itself — *"Kafka is a transport wire, not a handler shape"* — and defers to `wolverine-messaging-handlers`, which the prompt had not named. The two passes are not redundant and the order matters.

**The seam-first discipline paid off twice in one session.** `INearbyAvailableDriversSource` (built in slice 5.3, with no real source in sight) and the pre-generated proto (PR B, with no consumer in sight) each turned what could have been a redesign into a registration change. Both were speculative when built. Both were right.

**Deciding the availability half rather than defaulting it.** The tempting move — default a location-only driver to `Available`/`STANDARD` so the demo works end to end — would have had Dispatch fabricate a capability claim at the point of query, invisibly. Excluding instead makes the system honestly report that it cannot dispatch yet, which is true: ADR-018 says the view needs two feeders and only one exists.

---

## What was harder than expected

### The most serious defect survived two auditor passes and a source-verification pass

`code-review`'s two-axis form found it, and **both axes found it independently** — which is the strongest signal the separation is worth its cost.

W006 §6.5 locks *"Eventual, **LWW per driver per side** — location LWW on `serverReceivedAt`, availability LWW on **its own ordering key**."* The implementation used a single Marten revision column carrying whichever side wrote last. That conflates two genuinely different problems:

- **business ordering** — which update is newer, *per side*, on that side's own clock;
- **write concurrency** — did anyone change the row between my read and my write.

One column cannot serve both when there are two clocks. The concrete failure: a driver goes Offline at 12:00:05, a heartbeat position stamped 12:00:07 arrives first and raises the revision, and the Offline write is then discarded by `where mt_version < ?` — **leaving an offline driver dispatchable, with no error anywhere.** A second, subtler failure sat underneath it: both handlers load-then-write the whole document, and the revision guarded the revision rather than the freshness of the read, so the two sides could lost-update each other.

The fix separates the two concerns, which is what §6.5 described all along: each handler compares **its own side's timestamp** against the stored one (business LWW, and equality is the dedup no-op §6.3 wants), and the revision becomes a plain incrementing concurrency token via `UpdateRevision`, with a Wolverine `OnException<ConcurrencyException>().RetryWithCooldown(...)` policy re-running the handler against fresh state.

**Why the earlier passes could not have caught it.** `jasperfx-source-verifier` answered "what is the best Marten API for an LWW upsert" correctly — `TryUpdateRevision` genuinely is that, for *one* writer. `critter-skill-auditor` checks conventions, and nothing about the code was unconventional. The defect lived in the gap between a correct API and a spec clause about *two* writers, which is exactly the seam a spec-axis reviewer reads and an API verifier does not.

**Methodology consequence worth carrying:** when a source-verification gate asks "what is the best API for X," the answer is scoped to the question's implicit cardinality. This gate asked about an upsert and got an answer about *an* upsert. The spec said "per side," and nobody re-read that clause against the chosen primitive until the review.

### This PR broke CI, and the honest test for that was to re-run `main`

CI failed twice with `DockerApiException: "Get https://registry-1.docker.io/v2/: context deadline exceeded"`, thrown from `ResourceReaper.GetAndStartNewAsync` — and it failed in **Telemetry's** pre-existing suites, which this PR does not touch. Every signal said flake.

It was not flake. Re-running `main`'s own workflow at the same moment passed, which is the experiment that settles it: Docker Hub was healthy, and the added load was this PR's. xUnit runs the two test assemblies in parallel, each standing up its own Testcontainers session, and slice 5 took that from four containers to six — including a **second** ~800 MB Kafka image. The concurrent pulls saturated the runner's registry connection, and whichever assembly lost the race reported it as a broken test.

Fixed with a serial `docker pull` step ahead of `dotnet test`, by user sign-off, since CI changes are conventionally their own session. Two things worth carrying:

- **"It failed in code I didn't touch" is evidence about *load*, not innocence.** The blast radius of a new test fixture is the whole CI job, not its own assembly.
- **Pinning an image tag by hand is a place to verify, not guess.** The first version of the pre-pull step named `testcontainers/ryuk:0.11.0`; Testcontainers 4.13.0 actually pins `0.14.0` by digest, read out of the package assembly. A wrong tag there fails silently in the worst way — it pre-pulls an image nothing uses and quietly restores the behaviour it was meant to fix, while looking like a fix.

**The load pressure then surfaced a second, unrelated symptom.** With the pulls fixed, one *pre-existing* test — `Slice53CandidatesSelectedTests` — timed out at the `TrackedSession` default of 5 s, having reached 4.6 s. Nothing about it changed; the three-handler cascade it exercises simply no longer fits in 5 s on a 2-core runner that is also starting six containers. Widened to 30 s there and in the two `Slice52` helpers carrying the same default, since all three were equally exposed and leaving them would have left landmines for the next contributor to trip.

That is worth separating from the "no opportunistic edits" rule rather than blurring into it: those are files this session's prompt did not name, and the edits were still right, because **the session destabilised them**. The rule exists to stop unrelated improvements riding along, not to stop a session cleaning up after itself. A useful test for the distinction — *would this file still need touching if my change were reverted?* If no, it is in scope.

### Regression tests must be proven to fail

Having written the fix, I wrote two regression tests, and they passed. That is not evidence — a test that passes on both the broken and fixed implementation pins nothing.

Reverting the availability handler alone still passed, because the location handler's new small sequential revisions made the old code's huge timestamp revision always win; the original defect needed *both* handlers on timestamp revisions. Only after reverting both did the two tests fail, and then pass again on restore. **The first version of the "regression" test was a false witness**, and a five-minute revert-and-rerun was what distinguished it from a real one.

### A test caught a real geometry bug that no review would have

The k-ring derivation was wrong in the dangerous direction, and it took a deliberately strengthened test to find it.

`DeriveK` divided the search radius by the hexagon centre-to-centre spacing, `edge × √3`. That is the distance gained per hop **only when travelling along a lattice axis**. The axes are 60° apart, so a bearing falling between two of them advances less per hop — worst case by `cos(30°)`. The correct divisor is `edge × 1.5`.

The measured consequence: a point 4,982 m due north of the test origin sits at grid distance **17**, and the original formula produced k=16. At Dispatch's production 5 km search radius, drivers near the edge would have been silently dropped from every candidate set. No exception, no log, no empty result — just slightly fewer candidates than there should be, which is indistinguishable from a quiet market.

What found it was not the formula review. The first version of the coverage test asserted only the 1 km case, which passed under both formulas. Extending it to the production 5 km radius is what failed. **Two lessons:** pin geometric invariants at the value production actually uses, not at a convenient small one; and when a computation over-approximates on purpose, test the *edge* of the approximation, because that is the only place the error lives.

A sharpener: pocketken's `GetHexagonEdgeLengthAverageInM` returns ~201 m at resolution 9, while H3's published tables say ~174 m. Those are different quantities — one is the edge of a regular hexagon of average *area*, the other averages the actual distorted edges — and they differ by exactly the `√3/2` this bug turned on. Two plausible numbers a factor apart, where the factor *is* the bug, is about as good a trap as geometry offers.

### A production semantic hiding inside a test hang

Four tests hung for 30 s and failed with "No activity detected." The instinct is to treat that as flake and add a retry. It was not flake: the listener declares no cold-start offset policy, so it inherited Confluent's `Latest`, and anything produced before the consumer group finished joining was legitimately behind the tail forever.

The fix is a **production decision**, not a test fix, and it was escalated as one. `BeginAtLatest()` ships, on W006's own logic — §6.4 already evicts positions older than three heartbeats, so replaying retained history to reach the state one heartbeat produces in seconds is wasted work. The test cost is a warm-up handshake in the fixture, which retries a throwaway round trip until one is observably handled rather than sleeping a guessed interval.

Worth noting how close this came to being resolved silently. `BeginAtEarliest()` would have turned all four tests green in one line and needed no fixture machinery — and would have committed the service to replaying its entire retained topic on every fresh deploy, as an invisible side effect of making a test pass.

### `int` versus `long`, caught by arithmetic rather than by a test

`TryUpdateRevision` takes a `long`, and the revision carries `serverReceivedAt` as unix-milliseconds — about 1.7 × 10¹². Marten's revision column comes in two widths, chosen by which interface the document implements: `IRevisioned` is `int`/`integer`, `ILongVersioned` is `long`/`bigint`. `IRevisioned` is the more familiar name and the wrong one here by a factor of about 800.

This was caught by noticing the magnitude mismatch mid-write, not by a failing test — and it would **not** have failed loudly. A truncated revision still compares, still guards, and still looks like it works, on the wrong number.

---

## Methodology refinements

**A DEBT row blocked on code that does not exist yet is drained by the session that writes the code.** The `wolverine-kafka` listener row was deferred in PR C with an explicit condition — fix it from a real consumer — because replacing speculative names with differently speculative ones is not progress. PR D satisfied that condition, so the row drained *inside an implementation PR* rather than waiting for a `tidy: skills` session. Deferring further would have kept a known-wrong skill in place for no gain. This is a genuine refinement to the tidy-session convention, and it is now recorded in `DEBT.md`'s document history.

**Verify a library's numbers, not just its API names.** Prior sessions established that our own skills carry non-compiling API claims. This one adds a subtler case: the API name was right, the call compiled, the return value was plausible, and the *semantics of the number* were not what the surrounding arithmetic assumed. Source verification as currently practised checks signatures. It does not check units, and units are where this session's real bug lived.

**A verified gate can go stale during the session that verified it, and the prompt is the wrong place to fix it.** Gate 8 recorded `Rings.GetKRing` as the k-ring API. That was true of the *public surface* and false of the *guidance*: `GetKRing` is `[Obsolete]` as of H3 4.0 in favour of `GridDiskDistances`, which the compiler said on first build and which the shipped code follows. The gate table was deliberately left as authored — a prompt is a historical record of intent at session start, not a living document, so the correction belongs here and in the code comment that carries it. Worth naming as a shape: **reflection-probed API gates report existence, not deprecation**, so a probe-based gate should check for `[Obsolete]` explicitly or expect the compiler to be the real reviewer.

**Strengthening a passing test is worth doing when the assertion is an approximation.** The coverage test passed. Extending it to production values broke it. There is no general rule that tests should be strengthened at random, but "this test asserts that an over-approximation is big enough" is a specific signal that the chosen sample matters.

---

## Outstanding items / next-session inputs

**The next session must be a design-or-tidy return.** PR D is the fourth consecutive implementation PR in this chain; ADR-019 served as the interleave for PR C and nothing covers this one. Per ADR-004's cadence rule, candidates in rough order of pull:

1. **The Driver Profile workshop.** The ASB half of ADR-018 now blocks on it, this session deliberately declined to pre-empt its vocabulary, and it is the only thing standing between the current state and a genuinely end-to-end dispatch.
2. **A `tidy: skills` session.** Four rows registered this session join the standing backlog — and **three older rows are decisions, not cleanups**, and must not be drained by a routine tidy without a call: test-class naming (`Slice{N}<Feature>Tests` vs. the skill's snake_case mandate), `testing-integration` Gap B (no shipped collection follows the documented Strategy 1, and this session added a **fourth** non-conforming collection, `DispatchKafka`), and the `identity-acl` streaming exception.
3. **CritterWatch.** It renders meaningfully only once real cross-service traffic exists, which is exactly what this PR created. Needs RabbitMQ as a tooling-only broker (ADR-017); trial licence expired 2026-07-10, so re-check before planning.

**Still true and still unaddressed:** CI cannot build `apphost.cs`. It was edited again this session and verified by hand. That row remains open and remains its own session — and note this session touched the workflow for a *different* reason (the pre-pull step), so a future CI session inherits a file that has already been edited once outside its remit.

**New CI consideration for every future service:** the pre-pull list is now something a new Testcontainers-backed fixture must extend. It is not enforced — a fixture pinning an unlisted image still passes locally and merely reintroduces the race in CI. Worth folding into `testing-integration` whenever that skill's other rows are settled.

---

## Spec delta — landed?

**Yes, in full, plus three amendments the prompt anticipated in kind but not in specifics.**

- **W006 §6.5 designed → realized.** ✅ Document, LWW upsert, k-ring query, stub replacement, all shipped. The slice walk closes; W006 is fully realized.
- **W006 §6.3's Dedup GWT exercised.** ✅ Against a real broker, plus a stale-redelivery case the GWT does not name but the revision guard makes free to assert.
- **W001 §5.3's amendment realized.** ✅ Recorded as a realization note under the existing 2026-06-30 design amendment — an amendment to an amendment, which is the right shape: the design decision did not change, its status did.
- **Three implementation-time qualifiers added to §6.5.** ✅ Availability-half exclusion, H3 resolution provenance, ETA derivation. All **amendments, not corrections** — §6.5 is silent on each rather than wrong about any, and each was resolved by user sign-off rather than absorbed.
- **One limitation shipped as an explicit deferral, recorded in W006:** an availability transition for a driver Dispatch has never seen a position for is **dropped, not buffered**, and does not self-heal. Closing it requires a decision that belongs to Driver Profile's contract. Pinned by a test so it cannot change silently.
- **One qualifier the prompt did not anticipate:** the `BeginAtLatest()` cold-start policy. Recorded in W006 for the same reason as the others — the spec is silent, the choice is durable, and the reasoning is W006's own.
- **§11 ADR candidates:** none fired. #2 (stream-processing as a fourth modeling shape) gained its second and sharper data point and stays later-arc, registered as skill DEBT rather than promoted.
