# CritterCab — Session Handoff (post-PR #47)

**Written:** 2026-07-25 · **Branch:** `main` @ `327916d` (clean, synced, verified) · **Tests:** 57/57 green

---

## Where things stand

PR [#47](https://github.com/erikshafer/CritterCab/pull/47) merged (squash → `327916d`). It realized W006 slice 5, and **W006's slice walk is now complete — all five slices run.**

**CritterCab has its first cross-service flow.** Until this PR, "two services" meant two services that had never spoken. A GPS ping now enters Telemetry over gRPC, survives the throttle, is published to Kafka, and lands as a document write in Dispatch — a different service, different transport, different bounded context.

**Do not re-derive any of this — it is all written down:**

| What | Where |
|---|---|
| Full session record, four review passes, the two defects caught pre-merge | `docs/retrospectives/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md` |
| The prompt that drove it: 8 closed gates, 4 resolved forks, Phase 1 skill corrections | `docs/prompts/implementations/009-dispatch-w006-slice-5-nearby-available-drivers.md` |
| Slice 5 realized + three implementation-time amendments | `docs/workshops/006-telemetry-event-model.md` § Document History (2026-07-24, **third** entry) |
| Parking-lot #4 closed in code | `docs/workshops/001-dispatch-event-model.md` §5.3 → "Realized in code (2026-07-24, PR D)" |
| Reference implementation | `src/CritterCab.Dispatch/AvailableDrivers/` (5 files) + `Program.cs` |
| The corrected Kafka listener guidance | `docs/skills/wolverine-kafka/SKILL.md` § Listening / § Consumer groups |

---

## THE NEXT SESSION MUST BE A DESIGN-OR-TIDY RETURN

This is the one instruction in this document that is not a suggestion.

PR #47 was the **fourth consecutive implementation PR** in the W006 chain. ADR-019 served as the design-return interleave for PR C; nothing covers PR D. `docs/prompts/README.md` § Design-return cadence: *"A fourth consecutive implementation PR against the same BC without a design-or-tidy interleave is a signal to pause and ask whether the design has drifted."* We are past that signal, not approaching it.

Three candidates, in order of pull:

### 1. The Driver Profile workshop (strongest)

The ASB half of ADR-018 now blocks on it, and PR #47 **deliberately declined to pre-empt its vocabulary**. Until it ships, a real end-to-end run correctly yields `NoCandidatesAvailable` — Dispatch has driver positions but has never been told anyone's availability or vehicle class.

What PR #47 left as its forward-constraints, all of which this workshop should answer:

- **The four transitions W001 §5.3 anticipates** (`DriverCameOnline` / `WentOnBreak` / `WentOffline` / `VehicleChanged`) versus the single collapsed `DriverAvailabilityChanged` placeholder Dispatch currently holds. The placeholder carries exactly the four availability-side fields W006 §6.5 locks and nothing more, on purpose — it describes the shape of the hole. Replacing it should be a swap, not a migration.
- **The availability-before-location drop.** An availability transition for a driver Dispatch has never seen a position for is *dropped, not buffered*, and does **not** self-heal (the heartbeat creates the document with a null availability side). Closing it needs a decision that belongs to Driver Profile: either it republishes current state on demand, or Dispatch buffers unmatched transitions. Pinned by a test so it cannot change silently.
- **It would make ASB real**, closing the last of ADR-005's three transports.

### 2. A `tidy: skills` session

**18 open DEBT rows.** Three of them are **decisions, not cleanups** — do not let a routine tidy drain them without a call:

- **test-class naming** — `Slice{N}<Feature>Tests` vs. the skill's snake_case mandate. Rename all suites *or* amend `testing-fundamentals`. Fixing slice-locally makes it worse.
- **`testing-integration` Gap B** — `[CollectionDefinition(Name, DisableParallelization = true)]` is the documented Cab default and **no shipped collection follows it** (now four: Dispatch, Telemetry, TelemetryKafka, DispatchKafka). A companion note was folded in by PR #47: the skill's per-test cleanup shape has zero adherents either.
- **`identity-acl` streaming exception** — carried since PR #45.

Two rows are near-duplicates and should merge when drained: *"Plain-document write path — no governing skill"* (PR #45, Telemetry) and *"No skill covers a non-event-sourced document write path inside an **event-sourced** service"* (PR #47, Dispatch — the sharper instance).

### 3. CritterWatch

It renders meaningfully only once real cross-service traffic exists — **which PR #47 is what created.** Needs RabbitMQ as a tooling-only broker (ADR-017). **Trial licence expired 2026-07-10; re-check before planning any work here.**

---

## Two open PRs, and one is now doubly stale

- **PR [#43](https://github.com/erikshafer/CritterCab/pull/43)** — refreshes `CLAUDE.md`'s status line. It was already stale before PR #47 and is now stale in one further respect. **It will merge cleanly and quietly wrong — re-read its diff before merging.** Accurate as of `327916d`: *two services; two live transports (gRPC serving traffic, Kafka publishing **and consuming**); W006 complete, all five slices realized; CritterCab's first cross-service flow running; ASB the only modeled-but-unbuilt transport.*
- **PR [#44](https://github.com/erikshafer/CritterCab/pull/44)** — CI Actions bump off the Node 20 deprecation. Unrelated and independent. Note the deprecation warning still appears on every CI run, so this is real.

Local branches `tidy/ci-bump-actions-v5` and `tidy/housekeeping-claude-md-status-refresh` back those two PRs — **do not delete them.** `telemetry/slice-3-kafka-publish` (PR #46, merged) is stale and safe to delete.

---

## Things not captured in any artifact — read these

1. **CI now pre-pulls Testcontainers images, and the list is unenforced.** `.github/workflows/dotnet.yml` gained a serial `docker pull` step because six containers across two parallel test assemblies saturated the runner's connection to Docker Hub. **Any new Testcontainers-backed fixture must add its image to that list** — a fixture pinning an unlisted image passes locally, passes review, and merely reintroduces the race in CI, where it surfaces as a `DockerApiException` in whichever *unrelated* suite lost the race. Also: the `testcontainers/ryuk` tag there is pinned by Testcontainers itself (4.13.0 → `0.14.0`, by digest) and must be re-read from the package on every Testcontainers bump. Now a DEBT row.

2. **Three pre-existing test helpers had their `TrackedSession` timeouts widened 5s → 30s** (`Slice53CandidatesSelectedTests`, both `Slice52` suites). Not flakiness in those tests — the added container load made the 5s default marginal on a 2-core runner. If a future session reduces CI container count, these can go back down, but there is no reason to.

3. **`opts.Durability.UseSyncRetryBlock = true` remains Telemetry-only and process-global.** It makes W006 §6.3's publish-before-store ordering real. Dispatch deliberately does **not** set it — it is a producer concern. If anything ever adds a second publisher to Telemetry, re-weigh it.

4. **Dispatch and Telemetry both compile `driver_location_updated.proto` independently.** This is not a shared assembly and must never become one. It works because Wolverine's message identity is `Type.FullName`-based and assembly-agnostic — so **the real coupling is the proto's `option csharp_namespace`, not the assembly**, and the message must stay top-level (a nested proto type gains a `DeclaringType_` prefix). Both facts are commented in `CritterCab.Dispatch.csproj`.

5. **`csharp-coding-standards` mandates a `GeoLocation` type that does not exist anywhere in the codebase.** Dispatch has `Shared/Location.cs`; Telemetry has raw doubles at the H3 binding layer; PR #47 mirrored Telemetry deliberately so the two services compute identical cell ids. A DEBT row now names the decision that has to come first: per-BC value objects with boundary translation, or scope the rule to domain surfaces only.

6. **CI still cannot build `apphost.cs`.** It was edited again in PR #47 and verified by hand. Note the workflow file has now been touched once *outside* that row's remit (the pre-pull step), so a CI session inherits a slightly different file than the row assumes.

---

## Methodology — what this session confirmed, and one new rule

**The four-pass sequence works, and each pass caught what the others structurally could not.** Run all of them, in this order:

1. **`jasperfx-source-verifier` at prompt-authoring time** (not session start). Second consecutive session where all gates closed before the deliverable plan existed; **three of eight premises were contradicted by source.**
2. **`critter-skill-auditor` Phase 1** before cutting code. Second consecutive session where it corrected an already-source-verified prompt — this time catching that the prompt **named the wrong governing skill**.
3. **`critter-skill-auditor` Phase 2** after implementation.
4. **`code-review` before opening the PR.** **This is where the session's worst defect was found, after the other three had run clean** — and both of its axes converged on it independently. Do not skip it because the earlier passes were green; that is precisely the condition under which it earns its keep.

**New rules worth keeping:**

- **A source-verification gate's answer is scoped to the question's implicit cardinality.** PR #47's gate asked "what is the best Marten API for an LWW upsert" and got a correct answer *for one writer*. The spec said "LWW per *side*", with two writers, and nobody re-read that clause against the chosen primitive until the review. The resulting bug left an offline driver dispatchable.
- **Prove a regression test fails.** PR #47's first pair of regression tests passed against both the broken and the fixed implementation. A five-minute revert-and-rerun is what separated a real test from a false witness.
- **Verify a library's *numbers*, not just its API names.** The H3 k-ring bug was a correct API call returning a plausible value whose units did not match the surrounding arithmetic. Source verification checks signatures; it does not check units.
- **Reflection-probed API gates report existence, not deprecation.** Gate 8 named `Rings.GetKRing`, which is `[Obsolete]`. The compiler was the real reviewer.
- **Cleaning up after your own change is not an opportunistic edit.** Useful test: *would this file still need touching if my change were reverted?* If no, it is in scope.
- **A DEBT row blocked on code that does not exist yet is drained by the session that writes the code** — not by a later tidy. PR #47 drained the `wolverine-kafka` listener row from its own shipped consumer, which is exactly why PR C deferred it.

---

## Conventions worth not relearning

- Branch + PR always; never commit to `main`. One prompt = one session = one PR; the retro ships **inside** that PR.
- Squash-merge — `main` is linear, prior PRs land as single commits with a `(#NN)` suffix.
- No Claude attribution in commits or PR bodies.
- No opportunistic edits to files outside the session's scope; register them in `DEBT.md` instead. *(Same-file edits are in-bounds; so is repairing what the session itself destabilised — see above.)*
- Surface the full PR URL when opening one.
- ADRs require explicit user sign-off before authoring.
- Docker works locally, so Testcontainers runs locally. **Do not lean on CI as the only gate** — and run `dotnet build apphost.cs` by hand after touching the AppHost, because CI cannot see it.
