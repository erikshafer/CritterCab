# State of the Repo — Transport Activation & CritterWatch — 2026-06-25

> **Purpose:** A verified "where are we, really" snapshot plus a decision-kickstart for getting
> gRPC, Kafka, and ASB into *running code* (not just into the model), and for standing up
> CritterWatch to observe them. Every claim below was re-checked against current source on
> 2026-06-25 — stale-memory corrections are called out inline with **[CORRECTION]**. Disposable
> once the decisions it sets up are made.

---

## 0. TL;DR

- **The build is green** (`dotnet build CritterCab.slnx` → 0 warnings, 0 errors) and the design
  corpus is excellent and far ahead of the code.
- **The showcase premise is ~0% demonstrated.** gRPC, Kafka, and ASB — the entire reason
  CritterCab exists — are declared as packages and modeled richly in workshops, but **not one is
  wired in a single line of `.cs`**. All three built slices are in-process Marten event sourcing,
  which the sibling showcases already demonstrate.
- **[CORRECTION] The biggest perceived risk to gRPC is gone.** `WolverineFx.Grpc` and
  `WolverineFx.Polecat` are published through **6.14.0** on nuget.org (the whole Wolverine suite is
  at 6.14.0). The 5.38 pins in `Directory.Packages.props` are a stale island, not an upstream block.
  gRPC on the Wolverine 6.x line is available *today* — the fix is a version bump.
- **Kafka is the farthest-away priority.** It lives entirely in the **Telemetry BC, which has not
  been workshopped**, and is gated behind the deferred Kafka-vs-gRPC decision (ADR-candidate #3).
- **CritterWatch is not wired in CritterCab.** CritterMart has a complete, copyable integration
  (ADR-017). **CritterWatch requires RabbitMQ** for its telemetry/control queues — it cannot run
  over ASB today (see §4.2 — an earlier draft of this doc claimed an ASB path; that was a misread
  and is corrected). So CritterCab **will spin up RabbitMQ specifically as CritterWatch's dependency**:
  a tooling-only broker, *not* a domain transport. Domain flows stay gRPC/Kafka/ASB per ADR-005;
  that ADR's "RabbitMQ is not used" clause needs a one-line carve-out for tooling. CritterWatch
  renders meaningfully only once real cross-service traffic exists, so it should follow the first
  real transport flow, not precede it. **The trial license expires 2026-07-10** (CritterMart
  ADR-017) — a hard clock if a CW demo is wanted soon.

---

## 1. Verified current state

### 1.1 Code reality (one service, three slices, zero transports)

| Dimension | Reality (verified 2026-06-25) |
|---|---|
| Services built | **1** — `CritterCab.Dispatch` (`src/CritterCab.Dispatch`) |
| Slices built | **3** of 12 modeled — 5.1 `RideRequested`, 5.2 `FareQuoted` (happy + failure), 5.3 `CandidatesSelected` + `NoCandidatesAvailable` |
| Tests | 11 Alba integration tests passing (per retro 005 / handoff; build green) |
| Persistence | Marten event sourcing on PostgreSQL, inline projections, `UseLightweightSessions` |
| Triggering | HTTP (`POST` ride request) → in-process Wolverine event forwarding (`UseFastEventForwarding`) |
| Transports wired | **None.** No gRPC, no Kafka, no ASB in any `.cs` file |
| Second service | None — so nothing *calls* anything over the wire |

`src/CritterCab.Dispatch/CritterCab.Dispatch.csproj` references only `WolverineFx`,
`WolverineFx.RuntimeCompilation`, `WolverineFx.Http`, `WolverineFx.Marten`, plus OpenAPI/Swagger.
The Kafka/ASB/gRPC/SignalR packages exist in `Directory.Packages.props` (central versions) but are
referenced by **no project**. A `grep` for transport wiring across `src/` matches only compiled DLLs
and `obj/` artifacts — never source.

> **The single most important fact in this document:** CritterCab today demonstrates exactly what
> its two predecessors already demonstrate — single-service, in-process, event-sourced CQRS. The
> distributed, multi-transport story that is its entire reason for existing is modeled but unbuilt.

### 1.2 The protobuf contracts exist as design artifacts only

`protos/` contains five authored `.proto` files (`common/v1/location`, `dispatch/v1/ride_assigned`,
`dispatch/v1/ride_request_cancelled`, `dispatch/v1/ride_request_abandoned`, `pricing/v1/get_fare_quote`)
plus `buf.yaml` / `buf.gen.yaml`. None are run through codegen, none are referenced by a `.csproj`,
and there is no service that implements or calls them. They are first-class *contracts* (ADR-009)
with no generated code behind them yet.

### 1.3 Design is far ahead of code (this is a strength, but it widens the gap)

| Artifact layer | Count | Notes |
|---|---|---|
| Workshops | 5 | Dispatch (v0.4, all 12 slices walked), Trips (v… modeled), Onboarding DS + Event Model, Identity |
| ADRs | 16 | through ADR-016 (Frontend Live-Update Transport) |
| Context map | v0.4 | 7 edges, most dashed pending unbuilt-BC workshops |
| Narratives | 2 | 001 rider books a ride, 002 driver accepts a ride |
| Skills | ~45 local | includes `wolverine-kafka`, `wolverine-grpc-handlers`, `wolverine-azure-service-bus`, `polyglot-go-service`, `transport-selection` — all written, none exercised by code |

The modeling has run through five BCs; the code has run through three slices of one BC. The slices
that got built (5.1–5.3) are precisely the in-process ones — fare quoting and candidate selection
are modeled as gRPC/Telemetry boundaries but were built against **stubs**
(`PricingClientStub`, `NearbyAvailableDriversStub`).

### 1.4 Package / version reality

`Directory.Packages.props`:

| Package | Pinned | Latest on nuget | Note |
|---|---|---|---|
| `WolverineFx`, `.Http`, `.Marten`, `.AzureServiceBus`, `.Kafka`, `.SignalR`, `.RuntimeCompilation` | **6.8.0** | 6.14.0 | Marten 9 / Wolverine 6 line; ~6 minors behind |
| `WolverineFx.Grpc` | **5.38.0** | **6.14.0** | **[CORRECTION]** stale island — 6.x exists |
| `WolverineFx.Polecat` | **5.38.0** | **6.14.0** | **[CORRECTION]** stale island — 6.x exists (Polecat unused so far) |
| Marten (via WolverineFx.Marten) | 9 line | — | conventional projections must be `partial` (no runtime codegen) — already observed |
| Aspire | 13.4.3 | — | pinned inline in `apphost.cs` (opts out of CPM) |

**[CORRECTION] to memory `project_marten9_wolverine6_migration_in_progress`:** the note framed
"Grpc/Polecat 5.38-vs-6.8" as drift "still owed," implying uncertainty about whether the 6.x line
was reachable. It is fully reachable — `WolverineFx.Grpc` 6.14.0 is on nuget.org now. The build
*does* compile with the 5.38/6.8 mix, but running gRPC against Wolverine 6.8 on a cross-major
package island is an unnecessary risk to carry into the project's #1 priority. **Bump
Grpc + Polecat onto the 6.x line before any gRPC slice** (either match the core at 6.8, or refresh
the whole suite to 6.14 in one tidy pass).

---

## 2. Where each transport enters the modeled plan

This is the dependency map that should drive any re-prioritization. Transports are not freely
re-orderable — each is gated by what it physically requires.

### gRPC — nearest, but needs a consumer

| Modeled at | Shape | What it physically needs |
|---|---|---|
| Slice 5.2 `FareQuoted` | gRPC **unary** `GetFareQuote` → Pricing (W001 §5.2, `get_fare_quote.proto`) | Built today as `PricingClientStub`. Needs a Pricing service (or a gRPC stand-in) to become real |
| Slice 5.4 `OfferSent` | gRPC **server-streaming** of `ActiveOffersForDriver` to the driver app — "the canonical Wolverine 5.32 showcase shape" (W001 §5.4) | The projection (built at 5.4) + a streaming RPC surface + a client to stream to (driver app / Go polyglot / test harness) |
| Telemetry → Dispatch | gRPC **unary** geospatial query (one of two candidate shapes) | The deferred ADR-candidate #3 decision + a Telemetry service |

### Azure Service Bus — needs a second service to be a *real* cross-service flow

| Modeled at | Shape | What it needs |
|---|---|---|
| Slice 5.5 / 5.10 `RideAssigned` | ASB business event `dispatch.ride-assigned`, outbox-coordinated, published to **Trips** (W001 §5.10, ADR-014) | A Trips consumer service (modeled in W002, unbuilt) + ASB emulator locally |
| Slices 5.8 / 5.9 | ASB `RideRequestCancelled` / `RideRequestAbandoned` to Payments/Pricing/Operations | Same — downstream consumers, all unbuilt |

ASB is *reachable within the slice order* (it's slice 5.10), but 5.10 depends on 5.4–5.5 (offers
must be sent and accepted before `RideAssigned` has a real trigger), and it depends on a second
service existing to consume the publication. You cannot cleanly pull ASB forward without that chain.

### Kafka — structurally the farthest away

Kafka appears **nowhere in the Dispatch event model**. It lives entirely in the **Telemetry BC**
(high-volume GPS pings), which:

1. **Has no workshop** — it is the most-deferred BC in the project.
2. Is gated behind **ADR-candidate #3** (W001 §10 parking-lot #4 / context-map edge #5): the
   unresolved Kafka-vs-gRPC choice for how Telemetry supplies geospatial data to Dispatch.

So "just add Kafka" is not available off the shelf. Kafka requires a Telemetry workshop *and* the
ADR that the workshop forces. The upside: Telemetry is also where gRPC **client-streaming** (mobile
GPS ingest) naturally lives — so a single Telemetry build could light up **both** priority transports.

---

## 3. Deferred / dangling items (verified live, not from memory)

| Item | Status today | Source |
|---|---|---|
| Swagger title says `"CritterBids API"` | **Still wrong** — `src/CritterCab.Dispatch/Program.cs:94` | handoff §Carried housekeeping |
| Grpc/Polecat 5.38 vs 6.8 version island | **Live** — and now known to be a trivial bump (§1.4) | `Directory.Packages.props:24,26` |
| Skill-debt at threshold: marker-interface union return + event-triggered automation shape | **Live** — both patterns appeared 3× (encoding threshold) but `docs/skills/DEBT.md` shows *no open rows* | handoff §Skill-file debt; DEBT.md (process gap: threshold debt is in the retro/handoff, never registered in DEBT.md) |
| Bundling-rule skill encoding | **Live** — past threshold, queued for a `tidy:` session | handoff |
| Workshop §5.2 Reads-list inconsistency | **Live** — carried from retro 004 | handoff |
| `protos/` not wired to codegen (buf pipeline unexercised) | **Live** — design artifacts only | §1.2 |
| Telemetry workshop (Kafka-bearing BC) | **Not started** — gates Kafka + ADR-candidate #3 | context-map §Pending workshops |
| Six of eleven BCs unworkshopped (Telemetry, Driver/Rider Profile, Pricing, Payments, Ratings, Operations) | Pending | context-map §Pending workshops |
| Trust & Safety + Notifications: candidate BCs not in vision inventory | Inventory drift flagged | context-map §Pending workshops |
| Frontend: 3 open questions (map library, contracts-package shape, monorepo) | Open since v0.6 | vision §Open Questions |
| ADR-016 "Context Map as Living Artifact" (cadence) | Deferred — cadence validated 3× but not ADR'd | context-map §Update cadence |

**Process note:** the marker-interface / automation-shape skill debt is "at threshold" per the
handoff, but `DEBT.md` has zero open rows. The debt is real but unregistered — drain it (or register
it) in the next `tidy: skills` PR so it doesn't evaporate between sessions.

---

## 4. CritterWatch — status, tension, and the CritterMart reference

### 4.1 Status: not wired here; fully wired next door

CritterCab references **no** CritterWatch packages and has no console host. CritterMart, by contrast,
has a shipped, documented integration we can copy almost verbatim:

- **ADR-017** (`C:\Code\crittermart\docs\decisions\017-critterwatch-integrated.md`) — the decision record.
- **AppHost wiring** (`CritterMart.AppHost/Program.cs`) — `postgres.AddDatabase("critterwatch")`,
  a `critterwatch-console` project resource, services `.WaitFor(critterwatch)`.
- **Console host** (`CritterMart.CritterWatch/Program.cs`) — `builder.AddCritterWatch(conn, …,
  enableClusterPartitioning: false)` + `app.UseCritterWatch()`.
- **The hard-won gotchas** (all in ADR-017 + the `critterwatch-install` skill):
  - Run the console as **`ASPNETCORE_ENVIRONMENT=Production`** *and* explicitly
    `AddUserSecrets(...)` — otherwise the dev-tier fallback silently masks the real license.
  - Name the project resource `critterwatch-console` (the DB owns `critterwatch`; Aspire resource
    names share one case-insensitive namespace).
  - `enableClusterPartitioning: false` for a single node; `.Sequential()` listener for ordering.
  - Dedicated `critterwatch` database, not a schema in the app DB.

### 4.2 The transport tension (and its resolution)

CritterWatch monitored services publish telemetry to a well-known queue and listen on a private
control queue. CritterMart uses **RabbitMQ** for this. **CritterCab deliberately excludes RabbitMQ**
(ADR-005, "RabbitMQ is not used"). So the copy-paste path conflicts with a committed decision.

> **[CORRECTION 2026-06-25]** An earlier draft of this doc "resolved" the tension by claiming
> CritterWatch could run over **Azure Service Bus**, citing `UseShardedAzureServiceBusQueues` in
> `WolverineOptionsExtensions.cs`. **That was wrong.** That symbol appears in a *comment* listing
> sharded-cluster-topology helper names in the abstract (alongside `UseShardedAmazonSqsQueues`) —
> it is not a verified ASB telemetry/control-queue path for the console. The `critterwatch-install`
> skill, its `REFERENCE.md`, and the only shipped integration (CritterMart ADR-017) are **RabbitMQ
> end to end**. Treat "CritterWatch over ASB" as a *wouldn't-it-be-nice*, not a capability — at
> least not yet.

**Resolution:** **CritterWatch requires RabbitMQ, so CritterCab will run RabbitMQ** — stood up
purely as CritterWatch's telemetry/control backplane. This is **operational tooling infrastructure,
not a domain transport.** The distinction matters and must stay sharp:

- **Domain flows** (service-to-service, telemetry, business events) remain **gRPC / Kafka / ASB**
  per ADR-005. RabbitMQ carries **zero** domain messages.
- **RabbitMQ exists only so the monitoring console can function** — exactly the CritterMart ADR-017
  shape, except CritterCab has no broker at all today, so this is a *net-new* container that exists
  solely for tooling.

This carves out ADR-005's "RabbitMQ is not used" clause: not-used *for domain messaging* remains
true; RabbitMQ-as-CritterWatch-dependency is the named exception. That carve-out is the subject of
the planned **CritterCab ADR-017** (mirroring CritterMart's), which records RabbitMQ-for-tooling as
a committed decision and back-references ADR-005. No ASB alternative is offered there, because none
exists yet.

### 4.3 The timing lesson from CritterMart

CritterMart's ADR-013 *deferred* CritterWatch until messaging slices 4.2–4.7 produced real cross-BC
traffic, then ADR-017 integrated it once "the console renders meaningfully." **CritterCab has zero
cross-service traffic today.** A CritterWatch console pointed at the current single in-process
Dispatch node would show one node and an empty topology — technically working, demo-empty.

**Recommendation:** wire CritterWatch (over its **RabbitMQ** backplane) **immediately after the
first real cross-service flow lands** (so the topology, DLQ, and node-health panels have something
to show), not before. If the 2026-07-10 trial deadline forces the issue, a thin "monitor the single
node now" install is possible — but it under-sells the tool.

---

## 5. Re-prioritization options

The user's priority order is **gRPC ≈ Kafka ≫ ASB** (ASB is already well-exercised in CritterMart;
gRPC and Kafka need community adoption). Here are the candidate moves, each with leverage and risk.

### Option A — Continue Dispatch in slice order (5.4 `OfferSent` next)
- **Gets:** gRPC server-streaming at 5.4 (the canonical Wolverine showcase shape).
- **Leverage:** Lowest process risk — respects the workshop→code discipline; W001 §5.4 GWTs exist.
- **Cost:** Server-streaming needs a *client* to stream to (no driver app exists). Touches **no
  Kafka**, and ASB stays gated behind building Trips. Stays single-service — doesn't prove the
  distributed boundary the project is premised on.

### Option B — Pull forward the first cross-service flow: Dispatch → Trips over ASB
- **Gets:** First genuine multi-service deployment + ASB + Wolverine outbox + the `buf` codegen
  pipeline, all at once. Proves the boundary.
- **Leverage:** Trips is already modeled (W002); `ride_assigned.proto` is already authored.
- **Cost:** ASB is the *lowest* user priority. Requires slices 5.4–5.5 first (RideAssigned needs a
  real OfferAccepted trigger). Big first step (two hosts, outbox, multi-host integration tests).

### Option C — Telemetry-first spike: gRPC client-streaming **and** Kafka in one BC ⭐
- **Gets:** **Both** top priorities at once — gRPC **client-streaming** (mobile → Telemetry GPS
  ingest) and **Kafka** (Telemetry → Dispatch high-volume ping stream). Forces ADR-candidate #3
  (the decision that *unlocks* Kafka).
- **Leverage:** Highest transport-coverage-per-unit-work. Telemetry is structurally independent
  ("no opinions about trips or business logic"), so it can be built without the rest of the lifecycle.
- **Cost:** Telemetry **has no workshop** — needs a design-phase session first (a "design return"
  per the ADR-004 interleave). Greenfield BC. Forces a deferred architectural decision (which is the
  point, but it's real design work).

### Option D — gRPC end-to-end with the polyglot Go client (capstone)
- **Gets:** Proves the wire-level interop story — the polyglot goal — by having a non-.NET service
  consume a Dispatch gRPC surface.
- **Leverage:** Highest "honesty" payoff; nothing proves a contract like a second language reading it.
- **Cost:** Needs a gRPC surface built first (Option A or C) and a greenfield Go service. Best as a
  *follow-on* to A or C, not a starting move.

---

## 6. Recommended sequence (phased transport activation)

A sequence that front-loads the two priorities while keeping the design-first discipline intact:

**Phase 0 — Foundation tidy (low-risk, unblocks everything). One `tidy:` PR.**
1. Bump `WolverineFx.Grpc` + `WolverineFx.Polecat` off the 5.38 island onto the 6.x line (match
   core at 6.8, or refresh the whole suite to 6.14). *This sits directly on the gRPC critical path.*
2. Fix the `"CritterBids API"` Swagger title (`Program.cs:94`).
3. Register/drain the marker-interface + automation-shape skill debt in `DEBT.md`.

**Phase 1 — First real wire. Choose the leverage point (see §7 decision):**
- *If transport breadth wins:* **Option C** — workshop Telemetry, then build GPS ingest as gRPC
  client-streaming + the ping stream as Kafka. Lights up both priorities and resolves ADR-candidate #3.
- *If fastest-to-distributed wins:* **Option A→B** — finish Dispatch 5.4–5.5 (gRPC server-streaming
  surface lands at 5.4), then build a thin Trips consumer for the 5.10 ASB handoff.

**Phase 2 — CritterWatch over RabbitMQ.** Once Phase 1 produces cross-service or multi-listener
traffic, copy CritterMart's ADR-017 integration **as-is** (RabbitMQ backplane — do *not* try to swap
in ASB; that path does not exist, see §4.2). Stand up a tooling-only RabbitMQ container for the
console, land CritterCab's own ADR-017 (RabbitMQ-as-CritterWatch-dependency, carving out ADR-005),
and add the MessagePack CVE suppression CritterMart carries. Now the topology/DLQ/node panels have
something to show. (Mind the 2026-07-10 trial clock.)

**Phase 3 — Polyglot Go gRPC consumer (Option D).** Capstone: a Go service reads the Dispatch
gRPC surface at the wire level, proving the contract's interop story.

My lean: **Phase 0 unconditionally, then Phase 1 = Option C.** It is the only single move that
advances *both* gRPC and Kafka, and it forces the one deferred decision (ADR-candidate #3) that is
currently blocking the Kafka half of the project's identity. The cost — a Telemetry workshop — is
design work the project owes anyway and does well.

---

## 7. Risks of re-prioritizing (the user asked explicitly)

| Risk | Severity | Detail / mitigation |
|---|---|---|
| **Design-discipline drift** | Medium | The project's whole method is workshop→narrative→prompt→code. Option C needs a Telemetry workshop *first* (do it — don't skip to code). Pulling Dispatch slices out of timeline order (e.g., jumping to 5.10 ASB before 5.4–5.5) breaks the build chain: `RideAssigned` has no real trigger without `OfferAccepted`. Mitigation: respect intra-arc dependencies; only re-order at BC granularity, not slice granularity. |
| **Version-island risk on the critical path** | **Now Low [CORRECTION]** | Previously feared as "gRPC may not exist on 6.x." Refuted — Grpc 6.14.0 is published. Reduced to a routine bump. Verify runtime (not just compile) behavior once a gRPC slice exists. |
| **Second-service overhead** | Medium-High | The first cross-service flow forces *all* the distributed infra the project has only modeled: outbox config, Aspire multi-service orchestration, `buf` codegen actually wired, integration tests spanning two hosts (Aspire.Testing or paired Alba hosts). This is a genuine "first one's expensive" step; budget a full session for plumbing alone. |
| **Kafka is gated behind a decision, not just code** | Medium | Kafka cannot be built until ADR-candidate #3 (Kafka-vs-gRPC geospatial supply) is decided, which needs the Telemetry workshop. There is no shortcut to Kafka that skips this. Option C confronts it head-on (good); any other path leaves Kafka indefinitely deferred. |
| **CritterWatch trial clock** | Medium (time-boxed) | Trial expires **2026-07-10**. After that, the console drops to read-only Free tier unless a paid license is sourced — which reopens CritterMart ADR-013's CI/private-feed question (the auth-gated `packages.jasperfx.net` feed 401s on public CI). If a CW demo matters before mid-July, that's a hard deadline; otherwise plan for the paid-tier feed decision. Also: CritterWatch 0.9.1 transitively pulls a CVE'd MessagePack (suppressed in CritterMart) — inherit that suppression. |
| **Stub-to-real seams already exist (low risk, upside)** | Low | `PricingClientStub`, `NearbyAvailableDriversStub`, and the `ForwardingPricingClient`/`ForwardingNearbyDriversSource` indirection were built precisely so a real transport can slot in without reshaping handlers. The seams are ready; this de-risks Phases 1–3. |

---

## 8. Decisions to kickstart (what we owe ourselves next)

1. **Phase 1 leverage point:** Option C (Telemetry: gRPC client-streaming + Kafka, max breadth) vs.
   Option A→B (finish Dispatch arc, gRPC server-streaming + the Trips ASB handoff, max continuity)?
2. **Version policy:** match Grpc/Polecat to the core at 6.8, or refresh the whole suite to 6.14 in
   one pass? (Either way, get off the 5.38 island before a gRPC slice.)
3. **CritterWatch timing:** wire it now (thin, single-node, beat the trial clock) or after Phase 1
   (richer topology)? Transport is **settled: RabbitMQ** (tooling-only broker — CritterWatch cannot
   use ASB yet, §4.2); the only open question is *when* to stand it up, plus landing CritterCab's
   ADR-017.
4. **ADR-candidate #3 ownership:** is the Telemetry workshop the venue to decide Kafka-vs-gRPC for
   geospatial supply, or do we want a standalone transport-decision session first?

---

## 9. Evidence index (for re-grounding)

- Code state: `src/CritterCab.Dispatch/Program.cs`, `…/CritterCab.Dispatch.csproj`, `apphost.cs`,
  `grep` for transport wiring (only DLL/obj hits).
- Versions: `Directory.Packages.props:22–30`; nuget flat-index for `wolverinefx.grpc` /
  `.polecat` / `wolverinefx` → 6.14.0 (queried 2026-06-25).
- Transport plan: `docs/workshops/001-dispatch-event-model.md` §§5.2, 5.4, 5.10, §10 parking-lot;
  `docs/decisions/005-transport-selection-by-flow-type.md`; `docs/context-map/README.md` edges #1–#7.
- CritterWatch: `C:\Code\crittermart\docs\decisions\017-critterwatch-integrated.md`;
  `CritterMart.AppHost/Program.cs`; `CritterMart.CritterWatch/Program.cs`;
  `~/.claude/skills/critterwatch-install/SKILL.md` + `REFERENCE.md` (both RabbitMQ end to end —
  the authoritative source that CritterWatch requires RabbitMQ; `WolverineOptionsExtensions.cs`'s
  `UseShardedAzureServiceBusQueues` mention is an abstract comment, *not* an ASB telemetry path).
- Prior handoff: `docs/planning/2026-06-16-post-slice-5-3-handoff.md`.

---

## Document history

- **2026-06-25.** Authored as a state-of-the-repo + transport-activation decision kickstart.
  Re-verified all claims against current source; corrected two stale assumptions (Grpc/Polecat 6.x
  availability; the "0 transports wired" reality vs. the rich transport modeling).
- **2026-06-25 (correction).** Fixed a factual error in the first draft: CritterWatch **cannot**
  run over Azure Service Bus today (the `UseShardedAzureServiceBusQueues` "evidence" was an abstract
  code comment, not a working path). **CritterWatch requires RabbitMQ.** CritterCab will run a
  tooling-only RabbitMQ broker as CritterWatch's dependency — not a domain transport — carving out
  ADR-005's "RabbitMQ is not used" clause (planned CritterCab ADR-017). Corrected §0, §4.2, §4.3,
  §6 Phase 2, §8 decision #3, and §9.
