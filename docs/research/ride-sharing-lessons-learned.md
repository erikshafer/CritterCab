# Ride-Sharing Engineering: Lessons Learned

Curated research notes from first-party engineering blogs and talks by architects who have worked on ride-sharing and last-mile logistics platforms. The focus is lessons that transfer to **CritterCab's** vision — a distributed, event-driven reference architecture for ride-sharing on the Critter Stack (Wolverine, Marten, Polecat) with gRPC as a first-class transport and Kafka for high-volume telemetry.

This document is not a system-design explainer. Plenty of those exist. The goal here is to capture *why* these companies ended up where they did, so that CritterCab can make deliberate choices (or deliberate departures) rather than accidentally reinventing the same wheels badly.

---

## About this document

### What counts as a good source

Ride-sharing is one of the most heavily copied system-design interview topics on the internet, which means most search results are derivative essays describing what Uber "does" based on much older talks. To keep this research honest, sources were tiered:

- **Tier 1 — first-party engineering blogs and conference talks from named engineers at the company.** The Uber engineering blog, Grab engineering blog, DoorDash engineering, Lyft engineering, and named QCon/InfoQ talks by identified architects (Matt Ranney, Matt Klein, Yuri Shkuro, Ankit Srivastava).
- **Tier 2 — first-party technical retrospectives republished by senior leaders.** Josh Clemm's "Brief History of Scaling Uber" (Senior Director of Engineering at Uber Eats) is the best example.
- **Tier 3 — careful secondary synthesis.** High Scalability's summary of Matt Ranney's 2015 InfoQ talk is cited because it's the most widely referenced version of that talk and preserves Matt's original framing.

Secondary "Medium explainer" articles and system-design-interview sites were read to triangulate but are not cited here.

### How to read the "applicability to CritterCab" notes

Each lesson ends with a short note on how it maps to CritterCab's vision. These notes are opinionated but not prescriptive — they identify where the lesson cleanly fits, where it needs to be scaled down for a 6-to-8-service project, and where CritterCab should deliberately *not* follow the hyperscaler path.

---

## Primary sources consulted

Uber engineering blog:
- "Introducing Domain-Oriented Microservice Architecture" — Adam Gluck, 2020. (`uber.com/blog/microservice-architecture`)
- "Uber's Fulfillment Platform: Ground-up Re-architecture to Accelerate Uber's Go/Get Strategy" — Ashwin Neerabail, Ankit Srivastava, Kamran Massoudi, Madan Thangavelu, Uday Kiran Medisetty, 2021. (`uber.com/blog/fulfillment-platform-rearchitecture`)
- "Uber's Real-Time Push Platform" — Anirudh Raja, Uday Kiran Medisetty, Madan Thangavelu, Nilesh Mahajan, 2020. (`uber.com/blog/real-time-push-platform`)
- "Uber's Next Gen Push Platform on gRPC" — Shahbaz Kaladiya, Xinlin Peng, 2022.
- "H3: Uber's Hexagonal Hierarchical Spatial Index" — Isaac Brodsky, 2018.
- "Uber Engineering's Ringpop" — 2015.
- "The Uber Engineering Tech Stack, Parts I & II" — 2016.
- "Disaster recovery for multi-region Kafka at Uber" — Yupeng Fu, Mingmin Chen, 2022.
- "Enabling Seamless Kafka Async Queuing with Consumer Proxy" — 2021.
- "Introducing uForwarder" — 2026.

Grab engineering blog:
- "Pharos — Searching Nearby Drivers on Road Network at Scale" — Hao Wu, Minglei Su, Thanh Dat Le, Nuo Xu, Guanfeng Wang, Mihai Stroe, 2020.
- "Serving Driver-partners Data at Scale Using Mirror Cache".
- "Understanding Supply & Demand in Ride-hailing Through the Lens of Data".
- "DispatchGym: Grab's Reinforcement Learning Research Framework".

DoorDash engineering:
- "Using ML and Optimization to Solve DoorDash's Dispatch Problem" — Alex Weinstein, Jianzhe Luo, 2021.
- "Next-Generation Optimization for Dasher Dispatch at DoorDash".

Lyft engineering and associated talks:
- Matt Klein, "Lyft's Envoy: Embracing a Service Mesh" — QCon NY 2018.

Cross-company:
- Matt Ranney, "Scaling Uber's Real-time Market Platform" — InfoQ, 2015 (summarized by High Scalability).
- Yuri Shkuro, "Conquering Microservices Complexity @Uber with Distributed Tracing" — InfoQ.
- Josh Clemm, "Brief History of Scaling Uber" — LinkedIn/High Scalability, 2024.
- Cadence documentation and original Uber engineering posts on durable workflow orchestration.

---

## Lesson 1 — The service boundary that matters is the *domain*, not the microservice

Adam Gluck's 2020 DOMA article is the single most important piece of writing about ride-sharing architecture for CritterCab's purposes, because it is a direct retrospective on the pathology that happens when you take "microservices" too literally.

Uber reached roughly 2,200 critical microservices and calculated that half of them churned every 1.5 years. The practical effect was that any nontrivial feature required coordination across many services owned by many teams, and any attempt to modify a shared platform turned into a migration across hundreds of upstream callers. Gluck describes this as "can't live with them, can't live without them" — microservices had delivered the deployment and ownership benefits, but complexity had grown faster than those benefits.

DOMA's answer is four concepts:

- **Domains** group one-or-many related microservices around a logical role (matching, fares, map search). A domain may contain a single service; the shape of "domain" is logical, not physical.
- **Layer design** specifies who is allowed to depend on whom. Uber chose five layers: infrastructure, business, product, presentation, edge. Things drift downward over time as previously product-specific logic generalizes.
- **Gateways** are the single entry point into a domain. Upstream consumers depend only on the gateway contract, not on the individual services behind it. This is what makes it possible to rewrite the guts of a domain without triggering a fleet-wide migration.
- **Extensions** are the mechanism that lets another team inject logic into your domain without modifying your code. Uber uses logic extensions (provider/plugin pattern on a typed interface) and data extensions (Protobuf `Any` for arbitrary attached payloads).

The insight underneath all four is simple: Uber realized that a microservice architecture is not an architecture at all, it's a deployment strategy, and the actual architecture needs to be designed on top of it using the principles of Domain-Driven Design, Clean Architecture, and SOA.

### Applicability to CritterCab

CritterCab's 6-to-8-service target is three orders of magnitude smaller than Uber's. The layer design and gateway patterns are overkill at that scale and would introduce ceremony that nobody benefits from. The useful takeaway is:

1. **The bounded context is the unit of design, not the deployable.** This is already a CritterCab principle — the vision explicitly allows Identity+Rider Profile to fold into one service, Pricing to fold into Trips early, etc. DOMA validates that instinct at scale.
2. **If a domain contains more than one service, it should still expose a single point of entry.** If Trips eventually splits into Trips-write and Trips-read, callers outside the Trips domain should not know which they're talking to.
3. **The extension pattern is worth remembering for the Operations BC.** Operations is cross-cutting and will want to inject logic into many places (manual trip reassignment, suspension, incident response). A typed extension interface is the right seam; raw cross-service mutation is not.

---

## Lesson 2 — Dispatch is the center of gravity; everything else orbits it

Every ride-sharing engineering blog agrees on this point even though they use different names. At Uber the matching service is DISCO (dispatch optimization). At Grab it's the fulfillment stack anchored by Pharos. At DoorDash it's DeepRed. Matt Ranney's 2015 InfoQ talk is unusually clear on why dispatch is the hard part:

- Supply and demand are both dynamic in real time. Drivers move continuously; riders open and close the app; both can change their minds.
- Ranking candidates by straight-line distance gives wrong answers. The relevant metric is ETA along the road network, which depends on live traffic and the road graph.
- The optimization problem isn't "closest driver to this rider." It's "best assignment across all currently-open riders and all currently-available drivers, possibly including drivers whose current trips will end soon." Matt frames this as a real-time traveling-salesman-flavored problem.
- Handing a trip to a driver is a write into a stateful system under concurrent pressure — the same driver can be offered by two riders simultaneously, the same rider can be assigned to two drivers simultaneously.

Uber's 2021 Fulfillment Platform rewrite article reinforces this: when they re-architected, three of the things they explicitly called out as hard were concurrent read-modify-writes to the same entity, writes involving multiple entities (driver + trip + waypoints), and writes involving multiple instances of multiple entities (batch offers containing several trips). All three are Dispatch-flavored problems.

DoorDash's DeepRed article organizes dispatch into three layers that generalize well:

1. **Offer candidate generator** — given a new order, determine which Dashers are eligible.
2. **ML layer** — estimate what would happen if each candidate offer were taken (ETAs, batching potential, cascading effects).
3. **Optimization layer** — make the assignment decision under multiple objectives.

For a reference architecture at CritterCab's scale, the ML layer collapses to a ranking function and the optimization layer can be a reasonably simple Hungarian-algorithm or MIP-flavored solver in the first generation. But the three-way split is still the right mental model.

### Applicability to CritterCab

- CritterCab's vision places Dispatch at the gravitational center of the system and says it is the expected home of gRPC streaming surfaces. Every ride-sharing blog independently reaches the same conclusion.
- The three-layer DeepRed decomposition is a good template for CritterCab's Dispatch BC: a **candidate service** (driver filtering, backed by Telemetry), an **offer service** (fan-out to drivers, acceptance/decline/timeout), and an **assignment service** (the actual matching call). Whether these are three services or three vertical slices inside one Dispatch service can be deferred until Event Modeling.
- **Dispatch is where Wolverine's gRPC feature set earns its slot.** Server-streaming for offer fan-out to driver apps, client-streaming or unary for driver accept/decline, bidirectional for ops dashboards watching dispatch live. Every one of these flows shows up in Uber and Grab blogs as the thing they had to work hardest to make fast.

---

## Lesson 3 — Driver location is a separate concern from everything else

Every mature ride-sharing stack separates the geospatial index for driver location from everything else, for reasons that are specific to how fast-moving GPS data behaves.

Uber's Ringpop post describes the progression: the first "all active vehicles in memory" system searched every single car for each pickup request, which does not scale. The later Geospatial service is a separate domain that owns only location, distributes that load across workers via consistent hashing, and answers queries from Dispatch. Location data is described as fleeting — database storage is not the right substrate, because the data is being rewritten every few seconds per driver.

Grab's Pharos article goes further and makes the case explicit. Pharos is a distributed in-memory driver store with these deliberate properties:

- In-memory, not backed by a general-purpose database, because driver positions change too fast for a durable store to be the first write path.
- Partitioned by city (plus vertical, e.g. four-wheel vs. motorbike vs. pedestrian, because the road graph differs per vehicle class).
- P99 latency of 10ms for driver update and 50ms for nearby queries in production.
- Favors high throughput and availability over strong consistency. The team explicitly accepts that KNN results may be slightly stale because the update rate is high enough that occasional staleness doesn't meaningfully affect allocation quality.

The architectural lesson is that fast-moving location state is a *telemetry* problem, not a *trip* problem. They have different durability requirements, different consistency tolerances, different query patterns, and — critically — different failure modes. If the location service degrades, Dispatch wants to fail over to stale-but-available state rather than block all trip creation. If the trip store degrades, you very much want trips to stop being created, not to silently lose money.

### Applicability to CritterCab

- CritterCab's vision already makes Telemetry its own BC, explicitly upstream of Dispatch with no opinions on trips or business logic. This is the right call and every primary source agrees.
- Telemetry is the natural home for **Wolverine's Kafka transport.** GPS pings fit Kafka's append-only, high-volume model. The surge-pricing input path is the same shape — Uber explicitly describes active-active Kafka backing their surge pricing (see Kafka disaster recovery article), with Flink jobs consuming the trip-event stream.
- The durable "location-of-record" in Telemetry is probably Marten-backed Postgres at CritterCab scale, fed from the Kafka stream. An in-memory structure like Pharos is not worth building for a demo project — Postgres with PostGIS or an H3-indexed table is sufficient for the volume a showcase will produce.
- **What to copy from Pharos is the consistency tolerance**, not the implementation. Telemetry reads from Dispatch should be explicit about accepting stale-within-N-seconds data. Don't build Telemetry as if it were a transactional store.

---

## Lesson 4 — Geospatial indexing is coarse-then-fine, and the coarse filter is a grid

Uber's H3 (hexagonal hierarchical spatial index) and Google's S2 (square hierarchical spatial index) solve the same problem: partition the Earth into cells with unique IDs so that "what is near this point" becomes a cell-membership lookup. Uber used S2 first, then built and open-sourced H3 because hexagons have uniform distance between a cell center and all six neighbors (squares have two distances), which matters for urban analytics and surge heatmaps.

The pattern in every blog is the same:

1. Use grid cells as the **sharding key** for the location store. The cell ID becomes the hash key for routing a location update to the right worker.
2. Use grid cells as the **coarse filter** for nearby queries. A pickup point's cell plus its ring of neighbor cells gives the candidate set. This throws away 99% of the driver population in one indexed lookup.
3. Only after the coarse filter, compute **routing distance** (or ETA) via the road network for each remaining candidate. Straight-line distance ranking is the common beginner mistake — Grab's Pharos article shows a canonical example where the straight-line nearest driver has a longer road-network ETA than a different driver on the "wrong side" of a highway.

Grab's Pharos uses OpenStreetMap graphs partitioned by city and vehicle class, with Adaptive Radix Trees (ART) indexing drivers by both driver ID and edge-based-node (EBN) ID for bidirectional lookup. That level of sophistication is not needed at CritterCab's scale, but the coarse-then-fine pattern absolutely is.

### Applicability to CritterCab

- Postgres with the **`h3`** and **`h3_postgis`** extensions is the pragmatic choice for CritterCab's Telemetry BC. It gives you cell-ID indexing and neighbor queries in Postgres without introducing a new datastore, and Marten runs on Postgres anyway. The H3 reference implementation has first-class C bindings and community .NET bindings; the choice is effectively between calling the library from the .NET side or pushing the H3 function calls down into Postgres.
- For the final "nearest driver" ranking, CritterCab should use routing distance or ETA, not haversine. OSRM (Open Source Routing Machine) or Valhalla are the standard open-source engines here. A straight-line fallback is fine for a showcase, but should be labeled as a shortcut in an ADR so future readers know why it looks naive.
- **The sharding-key lesson is subtle but important.** Uber uses the cell ID as the sharding key for writes so that all updates for a cell go to the same worker, which gives them natural application-level serialization. In Marten/Postgres the equivalent is indexing by H3 cell ID and letting Postgres handle concurrent writes — but if CritterCab ever partitions Telemetry across nodes, the cell ID is the right partition key.

---

## Lesson 5 — Trips are event-sourced state machines and nothing else

Uber's 2021 Fulfillment Platform rewrite is the cleanest argument on the internet for CritterCab's choice to event-source Trips in Marten. The rewrite moved Uber away from their 2014 architecture, which had these properties (all of which sound familiar to anyone who has read about distributed NoSQL systems):

- Entities stored in Cassandra with Redis as a fallback cache.
- Application-level locking via Ringpop and a serial queue per owning worker.
- Saga-based compensation for multi-entity transactions.
- Explicit prioritization of availability over consistency.

Problems enumerated in the article:

- Last-write-wins semantics in Cassandra meant split-brain situations (deploys, region failovers) could cause concurrent writes to overwrite each other.
- Multi-entity writes went through ad-hoc RPC coordination and required constant reconciliation. Between the operations of a logical transaction, the system was internally inconsistent.
- Debugging across the saga, the cache tiers, the application lock, and the eventually-consistent store was "really difficult" — their words.

The new architecture chose three things deliberately:

1. **A NewSQL store** (Google Cloud Spanner) for transactional primitives with horizontal scalability. External consistency guarantees, server-side transaction buffering, cross-table cross-shard transactions.
2. **Statecharts** (Harel-style hierarchical state machines) as the modeling primitive for each fulfillment entity. States, transitions, triggers — with triggers exposed as RPC calls from external systems. Nested states give abstraction levels so that zooming in on "on-trip" reveals sub-states for en-route, arrived, started, in-progress, and so on.
3. **A Business Transaction Coordinator** that takes a DAG of entity triggers and orchestrates them within a single read-write transaction. For flows that need at-least-once side effects (writing to Kafka, sending push notifications), a "Latent Asynchronous Task Execution" (LATE) component commits post-commit tasks into a table alongside the main transaction and guarantees their execution via workers.

The architectural conclusion is: **event-sourcing plus strong transactional storage plus an explicit state machine gives you back the properties that the "eventual consistency at all costs" generation gave up.**

### Applicability to CritterCab

- The Trips BC in CritterCab is a textbook fit for Marten's event sourcing. The vision already calls it "the richest event-sourcing domain in the system." Uber's rewrite is the mature argument for why that's correct — the fulfillment timeline (requested → offered → accepted → en-route → arrived → started → in-progress → completed/cancelled/disputed) is a hierarchical state machine whose history you genuinely want to keep.
- **The Wolverine `[WriteAggregate]` pattern is the Critter-Stack equivalent of Uber's Business Transaction Coordinator for the single-aggregate case.** Erik's CritterBids work established the pattern; CritterCab inherits it. For multi-aggregate flows (Trip acceptance writing to Trips and Driver Profile, or Trip completion writing to Trips and triggering Payments), the Wolverine saga story is the equivalent of the Business Transaction Coordinator and LATE table.
- **Uber's "LATE" pattern maps onto Wolverine's outbox.** Commit post-commit work transactionally with the aggregate write, let a worker drain it with at-least-once delivery. This is not a new insight, but having Uber's scaling experience behind it is reassuring when someone asks "why do we need the outbox?"
- **Statecharts are underused in .NET DDD.** It's worth considering whether a formal statechart framework for Trip lifecycle would pay off in CritterCab. The alternative is implicit state machines inside aggregate handlers, which is what CritterBids' saga pattern already uses. A formal statechart could serve as a documentation artifact that trace back to Event Modeling swimlanes.

---

## Lesson 6 — Transport choice follows the flow, and the flow types are actually distinct

CritterCab's vision principle "Transport split by flow type, not by convenience" turns out to be the exact conclusion Uber reached after a decade of pain. Uber uses all of the following, and they do so for specific reasons:

- **gRPC over HTTP/2 with Protobuf** for service-to-service RPC. The 2022 "Next Gen Push Platform on gRPC" article documents their migration away from SSE+JSON to gRPC bidirectional streaming for the mobile push path, with measured payload-size reductions from Protobuf binary encoding vs. JSON.
- **Kafka** for high-throughput async messaging: pub-sub between microservices, stream processing (Flink/Samza), database changelog transport, ingestion into the data lake. The Kafka disaster-recovery article describes their deployment as processing trillions of messages and multiple petabytes per day. The uForwarder post explains that Kafka's partition-per-consumer-instance model doesn't match many pub-sub use cases well at scale, which is why they built a push-based consumer proxy that translates Kafka consumption into gRPC calls downstream.
- **TChannel** (Uber's own bidirectional RPC protocol, inspired by Twitter's Finagle) was the predecessor to their gRPC adoption. Matt Ranney's 2015 talk names "getting out of the HTTP and JSON business" as an explicit goal — TChannel was reported as 20x faster than HTTP at the time.
- **RAMEN** (Realtime Asynchronous MEssaging Network) is the mobile-client push path, now on gRPC. This is separate from inter-service messaging.

The DOMA article makes the argument for why you don't let the transport leak. Gateways abstract the contract; the fact that a particular domain writes to Kafka internally vs. calling another service via gRPC is a domain-internal decision.

A second, equally important lesson: **the consumer proxy pattern matters once you have heterogeneous languages.** Uber's uForwarder post spells out that building and maintaining Kafka client libraries in Go, Java, Python, and Node.js was creating maintenance overhead that the proxy eliminated. Consumer Proxy lives in one language; all consumers talk to it over gRPC.

### Applicability to CritterCab

- CritterCab's transport matrix maps cleanly onto the battle-tested pattern:
  - **gRPC via Wolverine** for service-to-service RPC, dispatch offer fan-out (server-streaming), GPS ingest from driver app (client-streaming), ops dashboards (bidirectional).
  - **Kafka via Wolverine** for high-volume telemetry and stream-processing inputs.
  - **Azure Service Bus (with ASB emulator locally)** for the business-event backbone. ASB gives you topics, dead-lettering, session ordering, and sign-up-event bridging from Entra via Microsoft Graph.
- The RAMEN journey is specifically worth reading before designing any mobile-facing streaming surface in CritterCab. The three shortcomings Uber called out in SSE are exactly the reasons to use gRPC bidirectional streaming:
  - Loss of acknowledgements because acks were only sent every 30s (SSE cadence).
  - Connection stability inconsistencies across client platforms.
  - Unidirectional transport preventing real-time RTT measurement or binary payloads.
- **The Critter Stack is a polyglot-free environment by design** (.NET everywhere except the one planned Go service), which means CritterCab does not need uForwarder-flavored abstractions. This is a real simplification — Uber built a whole proxy layer to paper over language diversity that CritterCab doesn't have.

---

## Lesson 7 — Push beats poll, and the evolution is SSE → gRPC

The Uber RAMEN story is the single most concentrated lesson on real-time mobile backends in the public record.

At peak before RAMEN, 80% of Uber API gateway traffic was mobile polling. The problems were exactly what you'd expect: aggressive polling drained batteries, generated backend load, multiplied during cold-start when every feature pulled latest state at once, and degraded on poor networks. The team built RAMEN with four design principles: ease of migration from polling, ease of development, reliability (at-least-once delivery with retries), and wire efficiency.

The first RAMEN generation used Server-Sent Events (SSE) over HTTP/1.1 persistent connections with a simple sequence-number-based protocol:

- Client opens `/ramen/receive?seq=0`, server responds with SSE stream.
- Server sends messages with incrementing sequence numbers.
- If the client reconnects with a higher sequence number, the server treats that as an ack and flushes older messages.
- Out-of-band `/ramen/ack?seq=N` every 30s for good-network cases.
- 4-second heartbeats, 7-second client-side timeout before reconnection.

Each message has priority (high/medium/low), TTL, and deduplication configuration as metadata. The payload generation is done by the existing API gateway — pull APIs and push APIs share the same business logic, so switching a feature from pull to push is mostly a configuration change.

Scale: the first generation ran on Node.js + Ringpop + Redis, hit ~70K QPS and 600K concurrent streaming connections, and broke down because Ringpop's gossip protocol membership-convergence time grew with ring size. Generation two was rebuilt on Netty + ZooKeeper + Apache Helix + Cassandra, reached 1.5M concurrent connections and >250K messages per second, and was then migrated to gRPC bidirectional streaming for the reasons in Lesson 6.

### Applicability to CritterCab

- **The pull-vs-push decision for CritterCab should default to push for any live-updating surface.** Driver offers, trip status changes visible to riders, live map updates — all of these are push, not polling.
- The three-level priority metadata pattern (high/medium/low with different reliability semantics) is worth lifting directly. Offer delivery is high-priority; ops dashboard updates can be low.
- **Server-Sent Events remain a perfectly reasonable choice** for a showcase-scale project with simple one-way feeds (rider app watching trip status). Uber used SSE successfully for multiple years. The case for going straight to gRPC bidirectional is the bidirectionality (ack-on-same-stream), binary efficiency, and first-class multi-language implementations. Since CritterCab is already committed to gRPC for service-to-service via Wolverine 5.32, using gRPC for the push path too has low marginal cost.
- Uber's "the gateway is responsible for both pull and push payload generation" pattern is a useful shape for CritterCab's Presentation layer (BFF / live read models). Whatever projects into Redis for the live map can also be what a gRPC push sends.

---

## Lesson 8 — Contracts are first-class design artifacts, not implementation detail

DOMA calls this "clean interfaces for domains." Matt Ranney's talk calls out Thrift contracts as foundational. The more recent gRPC/Protobuf Uber posts (including the OpenSearch gRPC integration story from early 2026) all return to the same theme: when the contract is explicit, typed, and versioned, rewriting the implementation becomes feasible. When the contract is implicit or ad-hoc-JSON, rewriting requires synchronized migrations across every caller.

Two specific practices are worth calling out:

1. **Prototool / Buf-style tooling** for linting, formatting, generating stubs, and checking for breaking changes per package. Uber built Prototool and later recommended migrating to Buf. The point is that schema evolution is a first-class CI concern.
2. **Protobuf `Any` for data extensions.** This is how Uber lets other teams attach arbitrary typed payloads to their platform requests without bloating core data models. It is a disciplined escape hatch — the core service never deserializes Any payloads; it only passes them through to logic extensions that know the type.

### Applicability to CritterCab

- CritterCab's vision already states "Contracts are first-class. Protobuf service and message definitions are design artifacts, not implementation detail." Uber's experience is the direct justification for that principle.
- **The contract-review discipline needs tooling to survive past the first sprint.** Buf (`buf lint`, `buf breaking`) is the modern equivalent of Prototool and should be in CritterCab's CI from very early. This is especially important because the Go polyglot service means the Protobuf files are the only shared truth across language boundaries.
- **Protobuf `Any` is a pattern to have in the back pocket, not to reach for early.** CritterCab's scale doesn't justify an extension architecture yet, but when Operations wants to decorate a trip with an investigation-tag without Trips caring what the tag is, `Any` is the right shape.

---

## Lesson 9 — Observability has to exist on day one; otherwise it never does

Jaeger — the distributed-tracing system CritterCab's vision calls out as a candidate alongside the .NET Aspire dashboard — was created at Uber by Yuri Shkuro's team because the microservices architecture had become un-debuggable without it. The InfoQ talk "Conquering Microservices Complexity @Uber with Distributed Tracing" lays out the before-state: an incident investigation required an engineer to read logs across 50 services owned by 12 different teams. With Jaeger, the same investigation collapsed to a single trace view.

The industry has since converged on OpenTelemetry as the instrumentation standard, with Jaeger as one backend among several. Lyft's Envoy story (Matt Klein's QCon NY 2018 talk) reaches the same conclusion from a different angle: the service mesh was adopted at Lyft *specifically* because implementing consistent observability across services written in PHP, Python, Node, and Go was impossible without standardizing at the network layer. Klein reports 100% trace coverage with no gaps between services after Envoy's rollout.

Two specific patterns from these talks:

1. **Trace IDs are propagated from the edge.** Every incoming client request gets a unique ID at the ingress, and that ID is used for log correlation across every service. The rider app → API gateway → Dispatch → Trips → Payments path is one trace.
2. **Dashboards link to traces, not the other way around.** An engineer looking at a dashboard of high-latency services clicks through to an actual trace that illustrates the problem. Metrics tell you which services are slow; traces tell you where the time went.

### Applicability to CritterCab

- CritterCab's vision places "Observability from day one" as a design principle. Good. Uber and Lyft's experiences are the strongest possible argument: neither company could retrofit observability after the fact cheaply.
- **OpenTelemetry is the correct bet.** It's the successor to both OpenTracing and OpenCensus and has native support across Wolverine, Marten, ASP.NET Core, and gRPC. The Aspire dashboard ingests OTel; Jaeger ingests OTel; Azure Monitor ingests OTel. The decision is where to *send* traces, not how to *produce* them.
- For a polyglot stack with a Go service, OTel is the *only* bet. The Go SDK is mature and interoperable.
- **The practical first slice.** A request from the rider app through the API gateway, into Dispatch, into Telemetry, back into Dispatch, into Trips should produce a single trace with visible spans for each hop. If that doesn't work on Day 1, the observability principle has failed. This is a good acceptance criterion for the first end-to-end narrative in CritterCab.

---

## Lesson 10 — Make everything killable and retryable, because failure is the common case

Matt Ranney's 2015 talk contains the most quoted Uber engineering line on the internet, which paraphrased is: at scale, there's no such thing as graceful shutdown — what you need to practice is what happens when things break unexpectedly. The talk's specific prescriptions:

- **Everything must be retryable.** Which requires every operation to be idempotent. Retrying a dispatch cannot dispatch a driver twice. Retrying a charge cannot charge a card twice.
- **Everything must be killable.** Crash-only design means there is no difference between a normal shutdown and an unexpected crash. If you find yourself writing cleanup logic that runs on SIGTERM but not on SIGKILL, that logic is a liability.
- **Small pieces.** Pairs are bad — losing one of a pair halves your capacity. Build to enough redundancy that killing any single thing is a non-event.
- **Load balancers are also failure points.** If Service A talks to Service B through a load balancer and the LB dies, Service A should route around it. Client-side load-balancing logic is required for genuine resilience.
- **Kill the databases too.** This forced database technology choices — Riak over MySQL for some data, Ringpop over Redis for others — because individual Redis instances were "expensive to have go away."

The Uber Fulfillment article has a quieter but related point: when they moved to Spanner, they deliberately chose a NewSQL store that handles contention and deadlock detection in the engine, rather than pushing it to application-level locks. The Ringpop-era pattern of "each key has a unique owning worker that serializes writes" was elegant but fragile — if the worker died mid-transaction, recovery got hard.

### Applicability to CritterCab

- CritterCab is not running at a scale where chaos engineering is worth its own role. But the crash-only discipline is free to adopt and pays immediate dividends: every handler idempotent, every message carrying a correlation ID, every cross-service call assumed to be retried.
- **Wolverine's built-in retry, DLQ, and scheduled-retry primitives are the Critter-Stack answer to the "make everything retryable" mandate.** They are already idiomatic in CritterBids. CritterCab inherits the discipline for free.
- **The pairs lesson applies to CritterCab's single-Redis and single-Postgres assumption.** A showcase that deploys to Hetzner with a single PostgreSQL instance is fine, but the dependency on that single instance should be explicit in an ADR and the failure mode documented. Uber's "kill the database to make sure" discipline is overkill, but pretending the database can never go down is the actual mistake.
- **The Uber datacenter-failover-via-driver-phones trick is not applicable, but the underlying idea is.** Uber sends an encrypted State Digest to each driver phone periodically. On datacenter failover, the phones replay their state into the new datacenter, which bootstraps in-flight trips. The generalizable lesson is that the client is a durable store you already have access to, and for some failure modes, client-side state replay is the right recovery mechanism. For CritterCab this matters if Dispatch loses in-memory offer state — the driver apps know which offers they're holding, and the recovery path should ask them.

---

## Lesson 11 — Payments and money flows belong to a different consistency regime

Uber's "Building High Throughput Payment Account Processing" (March 2026) and Josh Clemm's scaling-history article both describe the money stack as materially different from the trip stack. The core observations are:

- Payment processing requires strong consistency and idempotent operations at the transaction level. You cannot retry a capture and double-charge the rider. You cannot lose a driver payout.
- The interaction with external providers (Stripe, Braintree, bank ACH, etc.) is what drives most of the complexity. Retries across system boundaries are not your system's retries; they're the provider's retries plus yours.
- Event sourcing remains useful for audit and reconciliation, but the primary store typically favors ACID transactional integrity over flexible timeline queries.

Uber's older scaling-history material notes that they moved trip data to Schemaless (MySQL-based horizontal sharded store) while keeping a more conservative database stack for payments specifically. Josh Clemm's article links out to the Uber "money stack" post for the same reason.

DoorDash interview-prep material makes the same point more bluntly: payment processing typically requires strong consistency and idempotent operations, while dasher location updates tolerate eventual consistency. Design the data path per concern, not as a single store.

### Applicability to CritterCab

- CritterCab's vision correctly identifies Payments as the most likely candidate for Polecat on SQL Server. This is the right call and every primary source agrees. Trips is event-sourced; Payments is transactional.
- **The pattern is: Trips emits integration events for payment-worthy moments (trip started → authorize, trip completed → capture, trip disputed → refund), and Payments consumes them idempotently.** ASB is the correct transport for this if it's in the stack (business-event backbone semantics); Kafka is acceptable; Wolverine's durable outbox makes either safe from the producer side.
- **Idempotency keys are mandatory on every external-provider call.** This is the kind of detail that looks obvious in the vision doc and is easy to miss in the first Payments slice. Every call to Stripe (or the test double) needs an idempotency key derived from the trip ID and event type, so that a retry from Wolverine re-presents the same key.
- The audit-log property of event sourcing is still valuable for Payments. Consider a hybrid: Polecat document storage for the "current state" query path (balance, captures, refunds), plus a dedicated `payment_events` append-only log. This is essentially what Uber does but built with Critter Stack primitives.

---

## Lesson 12 — Failure isolation at the pod/region level, and why it matters

The Uber Fulfillment pre-rewrite architecture used "pods" — groupings of services required to execute fulfillment flows, with cities mapped onto pods by various criteria. The rationale was to reduce blast radius below the region level: a pod failure affects a subset of cities, not the whole region. The Marketplace Storage Gateway (MSG) abstracted Cassandra and kept redundant clusters within a region, and cross-region replication was asynchronous.

Uber's Kafka disaster-recovery article goes further. Producers always publish locally to regional clusters. Messages are replicated to aggregate clusters for a global view. Some workloads (surge pricing is the example) run active-active in both regions with a coordinator that picks which region's computation is authoritative at any given time.

The Lyft Envoy story adds a different angle: Envoy's retry and circuit-breaking logic lives in the sidecar proxy, so any upstream service failure manifests to the downstream caller as a bounded-latency failure rather than a cascading stall.

### Applicability to CritterCab

- Pod-level failure isolation is outside CritterCab's scope. Deploying a single region is the committed plan.
- **What does apply is the circuit-breaker/retry discipline at every service boundary.** Wolverine provides the retry primitives; Polly-style circuit breaking is a small addition at the HTTP/gRPC client layer. Every external dependency (Stripe, Entra, Microsoft Graph) gets wrapped.
- **Active-active surge pricing is a remarkable pattern and worth documenting as a CritterCab aspiration even if it's not built.** The idea: both regions compute surge; a coordinator picks the primary; if the primary's computation goes stale, the secondary's is authoritative. This generalizes to any workload where the computation is cheap and the coordination is the hard part. CritterCab's Pricing BC could be designed to permit this even if a single region is the initial deployment.

---

## Lesson 13 — Durable workflow engines are the right answer for long-lived orchestration

Uber built Cadence (now maintained at Uber; Temporal is the spiritual successor from the same team) specifically because coordinating long-running, stateful workflows across microservices using queues and ad-hoc state machines was turning into a bug farm. The key properties Cadence provides:

- **Event-sourced workflow state.** Every decision the workflow makes, every timer it sets, every activity it invokes is recorded in a workflow history. Recovery from a crashed worker is deterministic replay.
- **Durable timers.** A workflow can sleep for 30 days and reliably wake up, regardless of how many times the worker process has died in between.
- **Signals.** External events push data into a running workflow without polling.
- **Unique workflow ID enforcement.** Business-entity IDs can be used as workflow IDs; Cadence guarantees at-most-one running workflow per ID via atomic uniqueness checks.

This is directly relevant because CritterBids' Auction Closing saga pattern is solving a flavor of the same problem that Cadence solves at Uber's scale: an entity (auction, in that case) has a long-running lifecycle with timers, external signals, and compensating actions. Wolverine's `Saga` pattern covers the CritterBids case well. The CritterCab question is whether Dispatch and Trips need Cadence/Temporal-class durability, or whether Wolverine sagas are sufficient.

### Applicability to CritterCab

- **CritterCab doesn't need Temporal.** The scale doesn't justify introducing a new runtime, and the Wolverine saga primitive plus Marten event sourcing covers the actual orchestration patterns CritterCab requires.
- **But the mental model from Cadence is worth borrowing:** design Trip orchestration and Dispatch offer lifecycles as explicit, durable workflows with named signals, not as a scatter of handlers reacting to events. The Auction Closing saga in CritterBids is already a good prototype for this. CritterCab's Trip lifecycle and Dispatch offer lifecycle should follow the same shape.
- **The "unique workflow ID per business entity" pattern is worth lifting directly.** In Marten terms, the aggregate ID and the saga ID should be the same value where possible, so that there is one canonical identifier per ride that spans Dispatch → Trips → Payments → Ratings.

---

## Lesson 14 — "Let builders build" eventually has to be re-reined in

Josh Clemm's scaling-history article describes Uber's early culture as deliberately decentralized — "Let Builders Build" was one of the earliest cultural values. This got Uber from 10 engineers to 600 cities before the cracks showed. By 2018 the company had thousands of microservices, thousands of code repositories, multiple solutions to the same problem, and degraded developer productivity.

Project Ark was the response: consolidate on official backend languages (Java and Go), deprecate Python and JavaScript for backend services, reduce code repos from 12,000 down to per-language monorepos, define standardized architectural layers, and enforce gateway/extension patterns via DOMA.

The lesson is not that decentralization was wrong. It's that unconstrained decentralization has a timeline, and the timeline ends before you can intuitively sense it ending.

### Applicability to CritterCab

- CritterCab is not going to accumulate thousands of microservices. The question is the micro-version: will CritterCab accumulate unplanned patterns across BCs? Three Marten configurations? Two different saga conventions? A "we should probably fix this" list that never shrinks?
- **The Critter Stack skill files pattern (established in CritterSupply and CritterBids) is CritterCab's Project Ark equivalent from day one.** The skill files define how to build a Wolverine handler, a Marten projection, a saga, a cross-BC integration. The retrospective discipline at the end of each session reinforces them.
- The specific risk for CritterCab is gRPC. Because Wolverine 5.32 gRPC is new to the Critter Stack, and because the vision leans hard on it, there's a real risk of each service adopting slightly different patterns for stream lifecycle, error handling, cancellation semantics. **A `grpc-streaming.md` skill should be written early, probably before the second service's first streaming slice.**

---

## What NOT to copy from hyperscalers

A blunt list of things that appear in every Uber/Lyft/Grab engineering blog but are actively wrong for CritterCab at its scale and purpose:

1. **Don't build your own service mesh.** Lyft built Envoy because they needed to solve polyglot observability across hundreds of services in production. CritterCab should use Wolverine's built-in RPC observability plus OpenTelemetry plus, if truly needed, a small sidecar for the Go service. Istio/Envoy complexity is not free.

2. **Don't build Ringpop or Pharos.** In-memory distributed hash rings with gossip protocols solve real problems at 70K+ QPS with 100+ nodes. CritterCab's Telemetry BC will run fine on a single Postgres instance with H3 indexing for a showcase.

3. **Don't adopt Spanner or CockroachDB.** The NewSQL-over-sharded-MySQL tradeoff becomes real above a transactional rate Critter Stack projects will not approach. PostgreSQL is the right answer for Marten, and SQL Server is the right answer for Polecat/Payments. The Fulfillment rewrite is interesting to read for the *reasoning* about consistency vs. scalability, not as a template to copy.

4. **Don't build a consumer proxy over Kafka.** uForwarder exists because Uber has 1,000+ consumer services in five languages. CritterCab has one language plus one Go service, and Wolverine's Kafka transport handles the consumer pattern natively.

5. **Don't adopt Statecharts as a framework.** Uber's move to Statecharts was a rewrite of a legacy implicit state machine embedded in code generation. CritterCab is greenfield, and Wolverine sagas + Marten aggregates already give you state-machine-flavored semantics. Having statecharts as a mental model is valuable; shipping a statechart runtime is not.

6. **Don't overbuild the matching algorithm.** DoorDash's DeepRed uses reinforcement learning, Gurobi, and a team of optimization researchers. Grab's GrabShare is formulated as weighted maximal matching. For a showcase, a Hungarian-algorithm assignment or even a greedy nearest-available algorithm is sufficient. Make the architecture ready for a better algorithm to drop in later — don't build the better algorithm first.

7. **Don't ship your own workflow engine.** See Lesson 13. Wolverine sagas are sufficient.

---

## Open questions these sources surface for CritterCab

The research turned up several questions worth putting on the open-questions list in the vision document (or at minimum discussing in Event Modeling):

1. **How does CritterCab recover in-flight trip state after a Dispatch service crash?** Uber's driver-phone State Digest trick is one answer. Marten event sourcing plus a durable Wolverine saga is another. The choice affects both the Trips and Dispatch BCs.

2. **Is Kafka justified for CritterCab's telemetry volume, or is Postgres LISTEN/NOTIFY enough for a showcase?** A real conference demo will not produce trillions of GPS pings. The answer is probably "Kafka is justified for the showcase value, not the volume value" — but that's a decision worth making deliberately, not by default.

3. **Does CritterCab publish a single gRPC contract per BC, or one per service inside a BC?** The DOMA gateway pattern would say one per BC. Wolverine's gRPC support tends toward one per service. For 6-to-8 services this may not matter; for the contract-discipline principle, it matters now.

4. **Which BCs produce integration events vs. domain events?** The Critter Stack convention (established in CritterSupply) uses `OutgoingMessages` for integration events emitted from handlers. CritterCab should document which events cross BC boundaries (and thus go to the ASB/Kafka backbone) and which stay internal to Marten's event store.

5. **What is the minimum viable observability slice for the first end-to-end trip?** The answer should be something like: trace ID from rider app → Dispatch → Trips → Payments, with every span named, durations recorded, and at least one Jaeger-or-Aspire screenshot in a retrospective. This is a specific acceptance criterion, not a principle.

6. **Go service boundary: which BC does it live in?** The vision says Go is committed; Rust is deferred. The choice of *which* BC is implemented in Go affects the contract-design work. Telemetry is a defensible choice (high-throughput, simple service, good showcase for gRPC wire-level interop). Dispatch is also defensible (streaming-heavy, but also the most business-logic-heavy service). This should be explicit in an ADR.

---

## Recommended follow-up reading

If only three articles can be read in full:

1. **"Uber's Fulfillment Platform: Ground-up Re-architecture" (2021)** — the cleanest public retrospective on moving a ride-sharing fulfillment stack from NoSQL+eventual-consistency to NewSQL+explicit-state-machines. Directly applicable to CritterCab's Trips design.

2. **"Uber's Real-Time Push Platform" (2020) + "Uber's Next Gen Push Platform on gRPC" (2022)** — read together, the complete evolution of a mobile real-time protocol from polling through SSE to gRPC bidirectional streaming. Directly applicable to the rider-app and driver-app streaming surfaces.

3. **Matt Ranney, "Scaling Uber's Real-time Market Platform" (InfoQ, 2015)** — the talk from which most of the internet's knowledge of Uber's architecture derives. Still the clearest explanation of why dispatch is hard, why geospatial indexing is coarse-then-fine, and why availability trumps consistency at the location-data layer.

Secondary reading, ordered by CritterCab relevance:

- **Adam Gluck, "Introducing Domain-Oriented Microservice Architecture" (2020)** — for the context-map-to-deployment-boundary conversation.
- **Grab, "Pharos — Searching Nearby Drivers on Road Network at Scale" (2020)** — for the road-network-distance vs. haversine conversation in the Telemetry/Dispatch interface.
- **DoorDash, "Using ML and Optimization to Solve DoorDash's Dispatch Problem"** — for the three-layer dispatch decomposition (candidate → ML → optimization).
- **Josh Clemm, "Brief History of Scaling Uber"** — for the organizational narrative; useful when explaining to stakeholders why standardization matters.
- **Yuri Shkuro, "Conquering Microservices Complexity @Uber with Distributed Tracing"** — for the observability-first argument.
- **Matt Klein, "Lyft's Envoy: Embracing a Service Mesh"** — for the cross-cutting concerns argument; useful even if CritterCab does not adopt a mesh.

---

## Document history

- **v0.1** (2026-04-22): Initial research pass. Primary sources consulted: Uber, Grab, DoorDash, Lyft engineering blogs plus InfoQ/QCon talks. Curated into 14 lessons with explicit CritterCab applicability notes plus anti-patterns and open questions. Intended to complement `docs/vision/README.md` and inform early Event Modeling sessions.
