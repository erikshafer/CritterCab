# Retrospective — Telemetry Slice 3 (`DriverLocationUpdated` → Kafka)

## Metadata

- **Triggering prompt:** [`docs/prompts/implementations/008-telemetry-slice-3-kafka-publish.md`](../../prompts/implementations/008-telemetry-slice-3-kafka-publish.md)
- **Status:** Complete
- **Date authored:** 2026-07-24
- **Output artifacts:**
  - `Directory.Packages.props` — `WolverineFx.Protobuf` 6.21.0 (lockstep with the other eleven `WolverineFx*` entries); `Confluent.Kafka` 2.14.0 for the test project. `WolverineFx.Kafka`, `Aspire.Hosting.Kafka` and `Testcontainers.Kafka` were already pinned and needed no version work.
  - `src/CritterCab.Telemetry/CritterCab.Telemetry.csproj` — `WolverineFx.Kafka` + `WolverineFx.Protobuf` references
  - `src/CritterCab.Telemetry/ReportLocations/KafkaDriverLocationPublisher.cs` — **the whole behavioral change of the PR**; one method
  - `src/CritterCab.Telemetry/ReportLocations/IDriverLocationPublisher.cs` — comments only; the interface declaration is byte-identical
  - `src/CritterCab.Telemetry/Program.cs` — Kafka transport behind a connection-string guard, publishing rule (`SendInline` + `UseIdempotentProducer` + endpoint-scoped `UseProtobufSerialization`), `Durability.UseSyncRetryBlock`
  - `apphost.cs` — `AddKafka("kafka", port: 5392)`, persistent lifetime, referenced + waited on by Telemetry
  - `tests/CritterCab.Telemetry.Tests/TelemetryKafkaTestFixture.cs` — second fixture, Postgres + a real broker
  - `tests/CritterCab.Telemetry.Tests/ReportLocations/Slice3KafkaPublishTests.cs` — the round trip, asserted on the raw record
  - `tests/CritterCab.Telemetry.Tests/ReportLocations/Slice3PublishOrderingTests.cs` — a failed publish leaves no baseline
  - `tests/CritterCab.Telemetry.Tests/TelemetryTestFixture.cs` — `RecordingDriverLocationPublisher` gains `FailNextPublish`
  - **`docs/decisions/019-transport-agnostic-topic-naming.md`** — new ADR; `docs/decisions/014-*.md` status line + scope note; `docs/decisions/README.md` index
  - `docs/skills/wolverine-kafka/SKILL.md` — topic-naming and serialization sections corrected, two pitfalls added, publish-path examples reconciled against shipped code
  - `docs/skills/DEBT.md` — 3 new rows
  - `docs/workshops/006-telemetry-event-model.md` `## Document History` — §6.3 realized; §11 candidate #1 discharged; the publish-first qualifier recorded
  - `docs/prompts/README.md` — Implementations index entry
  - This retro
- **Outcome:** Slice 3 implemented end-to-end. **33/33 green locally** (Telemetry 22, Dispatch 11), 0 warnings. CritterCab's **first Kafka topic** and **second live transport**. `ReportLocationsHandler.cs` does not appear in the diff.

---

## Framing

PR B's job was to make gRPC real. This one's job was to make Kafka real, and it was deliberately set up to be small: PR B had already put `driver_location_updated.proto` through codegen and typed the `IDriverLocationPublisher` seam against the generated `DriverLocationUpdated`, so the contract, the payload construction, the publish trigger and the publish-first ordering were all shipped and under test before this session started. What remained was one implementation class and the host wiring behind it.

That setup held. The session's actual difficulty was somewhere the prompt only half-anticipated: **not in writing the producer, but in discovering that the workshop's reasoning about it rested on an assumption no one had written down.**

---

## Outcome summary

The seam paid off exactly as designed. `KafkaDriverLocationPublisher` is one method — `bus.PublishAsync(update, new DeliveryOptions { PartitionKey = update.DriverId })` — and the diff touches no handler, no payload construction, and no test that was asserting on the trigger. The prompt's instruction that `ReportLocationsHandler.cs` should not appear in the diff was met literally, which is the cleanest available evidence that a seam introduced one PR early was introduced at the right place.

Everything else that made the session interesting came from two sources: a source-verification pass that found a gap between spec and library defaults, and a skill-discovery pass that found the repo already disagreed with itself about Kafka.

---

## What worked

**Source-verifying the transport before authoring the prompt, not before writing the code.** Prompt 007 ran its gate pass partly *during* the session and carried several gates open into implementation. This session ran a full `jasperfx-source-verifier` pass against local `wolverine @ V6.21.0-12-ge08abdeb3` while the prompt was still being drafted, and all six gates closed before a line of the prompt's deliverable plan was written. That ordering is what surfaced the delivery-semantics problem early enough to become a signed-off fork rather than a mid-session surprise — and it is the single practice most worth repeating.

**The Phase 1 auditor pass earned its cost, and would have been cheap to skip.** It corrected the prompt in four places *after* the prompt had already been source-verified and committed, which is the useful data point: source verification and convention discovery catch different classes of error and neither substitutes for the other. It found a governing skill the prompt had missed entirely (`wolverine-kafka`), overturned the prompt's bootstrap lean, found the Kafka port already allocated, and surfaced both skill-vs-spec contradictions.

**Two tests, two different questions, two different hosts.** The round-trip test reads the raw Kafka record — asserting the partition key and that the value is binary protobuf the generated parser accepts — on its own fixture with a real broker. The ordering test uses a throwing publisher and no broker at all. Keeping them apart meant the slice-1/2/4 suites never wait on Kafka, and it kept each test honest about what it actually proves.

**Asserting the partition key was not incidental.** It is the one assertion in the suite guarding a failure that is otherwise invisible: without a key the records still arrive, the test still sees a message, and only the per-driver ordering silently breaks. A test that merely confirmed "something landed on the topic" would have passed against a materially broken system.

---

## What was harder than expected

**Nothing about the producer.** Worth stating plainly, because the estimate was right for once: the code half of this session was as small as the seam promised.

**The Aspire example in our own skill does not compile.** `builder.AddKafka("kafka").WithHostPort(5392)` — copied from `aspire/SKILL.md` § Port allocation — fails with CS1929 on Aspire 13.4.6, because `WithHostPort` belongs to the Kafka *UI* container resource, not the broker. The port takes a constructor argument instead. This is a small fix but a pointed one: it went unnoticed because `apphost.cs` is a file-based app with no `.csproj`, so CI's "Verify solution completeness" step does not reach it, and nothing mechanically checks either the apphost or the examples written against it. Registered as a DEBT row; the CI gap remains its own session.

**Choosing a Testcontainers Kafka image is not a free choice.** `confluentinc/confluent-local:7.6.1` — the KRaft image Wolverine's own docker-compose uses — produced a container that exited 1 with `No 'meta.properties' found`. Testcontainers' `KafkaBuilder` injects a startup script built around `cp-kafka`'s entrypoint, and confluent-local expects its log directory formatted by `kafka-storage.sh` first; the combination yields a broker that reads `zookeeper.properties`, finds nothing, and dies. `confluentinc/cp-kafka:7.6.1` works. Pinned with a comment, because the failure mode reads as a Kafka problem rather than an image-compatibility problem.

---

### Design meets code — the assumption W006 §6.3 did not know it was making

This is the session's substantive finding, and it is the kind that only surfaces at implementation.

W006 §6.3 argues for publish-before-store from failure-mode asymmetry, and the argument is genuinely good: a failed *store* after a good publish costs a duplicate the consumer's `(driverId, serverReceivedAt)` dedup absorbs, while a failed *publish* after a good store costs a **miss** Dispatch cannot detect. The workshop chose the benign branch and wrote down why.

What it could not have known is that the conclusion depends on a precondition it never states: **the publish's outcome has to be known by the time the store runs.** Under Wolverine's defaults it is not, twice over.

1. A Kafka publishing endpoint defaults to `BufferedInMemory`, which batches into an in-process queue. `await PublishAsync(...)` returns before the broker has seen the record.
2. Even `SendInline()` — which does await the broker ack — is insufficient on its own, because `InlineSendingAgent` selects its retry block on `DurabilitySettings.UseSyncRetryBlock`, which has no initializer and defaults to `false`. The resulting async `RetryBlock.PostAsync` catches the exception, logs it, re-posts to a background block, and **returns success**.

So the naive implementation — write the two calls in the order §6.3 specifies — would have produced a system where publish-first is true in statement order only, every broker failure lands in the branch §6.3 rejected, and nothing anywhere looks wrong. The workshop's reasoning would have been quietly inverted by a default.

The session ships `SendInline()` + `UseSyncRetryBlock = true` + `UseIdempotentProducer()`, which makes a rejection throw, skip the upsert, leave the baseline stale, and let the driver's next ping republish. **Recorded in W006's Document History as an amendment, not a correction** — §6.3's argument stands; it simply did not name its own precondition.

Two honest caveats on it. `UseSyncRetryBlock` is **process-global**, not per-endpoint; affordable because Telemetry publishes to exactly one transport, and a second publisher should re-weigh it rather than inherit it. And the half of the claim that says *Wolverine's inline sender surfaces a broker rejection as an exception at all* rests on source verification plus the endpoint configuration, **not on a test** — `Slice3PublishOrderingTests` covers the handler's response to a throwing publisher, which is CritterCab's half of the contract. Breaking a live broker mid-test to cover the other half would be slow and flaky for a claim already traced to source; naming the gap here is the better trade, but it is a gap.

---

### The repo already disagreed with itself about Kafka

`wolverine-kafka/SKILL.md` was written well ahead of any implementation, and had independently arrived at two positions the shipping code contradicts.

**Topic naming.** The skill proposed `<bc>.<descriptive-name>` with the explicit rule that a topic "carries a descriptive name rather than a message-type name," reasoning that a topic outlives any one payload shape. ADR-014 and W006 §6.3 mandate `<source-bc>.<event-name-kebab>`, which *is* a message-type name. Both rules produce similar-looking strings from genuinely different logic, so nothing forced the question until a real topic needed naming. Spec beat skill — but the skill's argument was good enough to deserve a real answer rather than a dismissal, and giving it one is what turned ADR-019 from a rubber stamp into a decision with something at stake. The answer: CritterCab versions contracts in the proto package path (ADR-009), so payload evolution is handled a layer below the topic name, and a break large enough to make the event name misleading needs a new proto package and a deliberate consumer migration regardless.

**Serialization.** The skill said "Cab uses Wolverine's default JSON serialization" and deferred protobuf to "a future phase." Since the payload is a `Google.Protobuf.IMessage` generated from the contract of record, JSON would have left ADR-009's proto governing the C# type but not the wire. This was **escalated to user sign-off rather than absorbed as a session-runner call** — a written convention saying "not yet" is a different thing from a convention that is merely silent, and overriding it is not the session runner's to decide alone.

Both corrections rode in-PR under the session-runner-blocking exception, on the same reasoning PR B used for the gRPC skills: a session cannot follow a skill that contradicts the ADR it is authoring. The scope was held to the publish path; the listener/consumer-group/batching examples still name a `LocationPing` → `telemetry.location-pings` pairing that never existed, which is now *actively* misleading because `LocationPing` is a real type doing a different job. Those describe slice 5, so they are a DEBT row for PR D to fix from a shipped consumer rather than swap for differently speculative names.

---

## Methodology refinements

- **Run the source-verification pass during prompt authoring, not at session start.** The gap it found here changed a signed-off fork. Discovered mid-session it would have changed code already written.
- **Source verification and convention discovery are not substitutes.** This prompt was thoroughly gate-verified and still had four convention-level errors the Phase 1 auditor caught, including a missing governing skill. Both passes, in that order.
- **A skill that says "not yet" is a decision, not a silence.** When a session's plan overrides one, that belongs in front of the user. When a skill is merely absent on a point, the session runner decides. This session had one of each — protobuf serialization (escalated) and endpoint-scoped-vs-global (decided) — and the line held usefully.
- **When correcting a skill in-session, scope the correction to what the session actually shipped.** The publish path was corrected against real code; the listener half was left with a warning and a DEBT row, because correcting it now would mean inventing the consumer PR D is going to build.

---

## Outstanding items / next-session inputs

- **PR D — W006 slice 5**, the last pending slice: Dispatch consumes `telemetry.driver-location-updated` into the `AvailableDriver` view, replacing `NearbyAvailableDriversStub`, and closes the W001 §5.3 amendment. It also owns §6.3's **Dedup GWT**, which asserts consumer behavior and could not be tested here. Two constraints already verified for it: `ListenToKafkaTopic(...).ConfigureConsumer(c => c.GroupId = ...)` is the listener shape (there is no `.GroupId(string)`), and the listener **must** carry `UseProtobufSerialization()` because `ProtobufMessageSerializer.ReadFromData(byte[])` throws — only the `(Type, Envelope)` overload works.
- **Three new DEBT rows**: `wolverine-kafka` listener examples (drain with PR D), `transport-selection`'s missing built-vs-modeled status axis (two of three transports are now built), and the `aspire` `AddKafka` example.
- **Design-return cadence.** ADR-019 served as this run's interleave, so the counter is satisfied — but ASB is now the only modeled-and-unbuilt transport, and the Driver Profile workshop is the prerequisite for the ASB half of ADR-018's join. That is the natural design-side successor once slice 5 lands.
- **CLAUDE.md's status line is further out of date than it was**, and PR [#43](https://github.com/erikshafer/CritterCab/pull/43) — already stale on arrival for describing a transport-less Telemetry — is now stale in one more respect. Re-read its diff before merging; accurate now is *two services, two live transports, W006 slices 1/2/3/4 realized.*
- **The CI-cannot-see-`apphost.cs` gap bit again** (this session edited the file and caught a compile error only locally). Still its own session; extend the existing "Verify solution completeness" step rather than adding a guard.

---

## Spec delta — landed?

**Yes, in full, plus one amendment the prompt anticipated only in outline.**

- **W006 §6.3 designed → realized.** Topic, partition key, dedup key, and publish-first/no-outbox coupling are all concrete and under test. First Kafka topic; second live transport. Recorded in [W006 `## Document History`](../../workshops/006-telemetry-event-model.md#document-history) (2026-07-24, second entry).
- **W006 §11 candidate #1 fired as an ADR**, as planned: [ADR-019 — Transport-Agnostic Topic Naming](../../decisions/019-transport-agnostic-topic-naming.md), generalizing ADR-014's naming rule across transports while explicitly withholding its two ASB-specific operational clauses. ADR-014's status line now cross-references it. Candidate #2 remains later-arc; #3 discharged in PR B.
- **The publish-first qualifier landed as a spec amendment**, which the prompt named as a spec-delta line before the session knew how load-bearing it would be. W006 §6.3's ordering guarantee is now recorded as a configuration commitment rather than a free consequence of statement order.
- **One delta the prompt did not name:** ADR-019 also resolves a skill-vs-ADR contradiction on topic naming that predated this session. That was discovered by the Phase 1 pass after the prompt's spec-delta section was written, and it is the more durable half of what ADR-019 does.
