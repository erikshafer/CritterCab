# Post D→B→C Handoff — 2026-05-07

> **Purpose:** Session-handoff note after completing the D→B→C implementation roadmap from the morning orientation. Captures what landed, what drifted, skill-file debt, and the next-session decision point. Disposable once the next session orients.

---

## What landed (PR #4, branch `protobuf-dispatch-business-events`)

Five commits completing Steps D, B, and C from the morning orientation:

1. **Step D — Protobuf contracts.** `/protos/` directory established with buf v2 workspace. Three Dispatch business-event messages (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`) plus shared `Location` type in `common/v1/`. First exercise of ADR-009.

2. **Step B — Dispatch service skeleton.** Solution infrastructure (`CritterCab.slnx`, `Directory.Build.props`, `Directory.Packages.props`), `src/CritterCab.Dispatch/` with Marten + Wolverine wiring, `tests/CritterCab.Dispatch.Tests/` with Alba smoke test, and `apphost.cs` (Aspire single-file AppHost). Wolverine packages updated to 5.38.0.

3. **Step C — Slice 5.1 `RideRequested`.** First vertical slice: `POST /api/rides/request` endpoint, `RideRequest` aggregate (event-sourced), `RideRequested` event, `ActiveRequestsByRider` inline multi-stream projection (one-active-request-per-rider guard), `RequestTimeline` inline single-stream projection. Three Alba integration tests with Testcontainers (happy path, duplicate-rider rejection, concurrent riders). All 4 tests pass.

4. **Skill-file fixes.** `RunOaktonCommandsAsync` → `RunJasperFxCommands` across 3 skills. `protobuf-contracts` directory layout corrected to match `PACKAGE_DIRECTORY_MATCH`.

---

## Skill-file debt (not yet fixed)

The session surfaced 8 skill-file gaps across D/B/C retrospectives. Three were fixed in-flight. Five remain:

| Skill | Gap | Retro source |
|---|---|---|
| `marten-wolverine-aggregates`, `marten-projections` | `IEvent<T>` namespace is `JasperFx.Events`, not `Marten.Events` | Step C |
| `marten-projections` | `SingleStreamProjection<T>` is actually `SingleStreamProjection<TDoc, TId>` (two type params), and lives in `Marten.Events.Aggregation` not `Marten.Events.Projections`. `MultiStreamProjection` is in `Marten.Events.Projections` — namespaces are swapped from what the skill shows. | Step C |
| `marten-projections` | `ProjectionLifecycle` namespace is `JasperFx.Events.Projections`, not `Marten.Events.Projections` | Step C |
| `service-bootstrap` | Missing `AddWolverineHttp()` prerequisite for `MapWolverineEndpoints()` | Step C |
| `service-bootstrap` | `TimeProvider` must be registered in DI (`builder.Services.AddSingleton(TimeProvider.System)`) for handlers that inject it | Step C |

These are all Marten 8.x / JasperFx extraction namespace changes. A dedicated skill-file update pass would be efficient — one session touching the affected skills rather than fixing them piecemeal.

---

## Workflow compliance check

The session followed the `prompt → execute → retrospective` loop for all three steps. Each step had its own prompt in `docs/prompts/` and a retrospective appended to that prompt. The session correctly stopped at the implementation boundary rather than continuing into design-phase work (Trips workshop or new narratives).

**One concern raised by Erik at session close:** the PR scope expanded beyond the original "protobuf contracts" intent to include the service skeleton and first slice. Future sessions should scope PRs more tightly — one step per PR, or at most a closely related pair (D+B is defensible; adding C stretched it).

**Workflow gap noted:** the project's `narrative → prompt → execute → retrospective` loop was followed for implementation, but no new design artifacts were produced this session. The next session should return to the design phase (workshop or narrative authoring) before more implementation, per ADR-004's two-phase structure.

---

## Next-session decision point

The morning orientation doc (`2026-05-07-orientation-and-next-steps.md`) framed the post-D→B→C choice as:

### A — Trips workshop (second event model)

- Dispatch's slices 5.10 and 5.12 encode contracts that Trips is the receiving side of.
- Second workshop is where methodology conventions ossify into reusable patterns.
- Trips is the richest event-sourcing domain — best second specimen.
- **Do D (protobuf for `RideAssigned`) before A.** ← This is done. The proto contract exists. Trips workshop can now consume it from the receiver's side.

### E — More Dispatch narratives (decline, expire, cancel, abandon)

- Slices 5.6–5.9, 5.11–5.12 are not yet narrativized.
- Driver-decline journey is "strongest candidate for narrative #3" per narrative 002's deferral list.
- Temporal-automation slice (5.7, `OfferExpired`) hasn't been rendered at the narrative layer.
- Diminishing returns on convention discovery (methodology log entry 001 predicted this).

### Lean

**A is probably more load-bearing now that code exists.** Having a running Dispatch service makes the Trips workshop more concrete — the Trips workshop can reference actual proto contracts and real event shapes rather than hypothetical ones. The second workshop also stress-tests the workshop methodology in ways that more Dispatch narratives won't.

But this is Erik's call, not a prescription.

---

## Artifacts inventory (updated)

| Layer | Count | Notes |
|---|---|---|
| Workshops | 1 | Dispatch (complete, v0.2) |
| Narratives | 2 | Both Dispatch happy path (rider POV, driver POV) |
| Skills | 39 | Phase 5 closed; 5 gaps surfaced this session |
| ADRs | 10 committed, 8 candidates | Candidates still open with explicit triggers |
| Prompts | 5 | 1 workshop, 2 narrative, 1 decision, 2 implementation (all complete) |
| Proto files | 4 | 3 dispatch business events + 1 shared Location |
| Service projects | 1 | Dispatch (Marten, 1 slice implemented) |
| Test projects | 1 | 4 tests passing (1 smoke + 3 integration) |

---

## Document history

- **2026-05-07 (evening).** Authored at session close after D→B→C completion. The morning orientation doc (`2026-05-07-orientation-and-next-steps.md`) is now fully acted upon for steps D, B, C; its A-vs-E decision remains open.
