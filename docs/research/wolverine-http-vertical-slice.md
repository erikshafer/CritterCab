# Wolverine.HTTP and Vertical Slice Architecture — Notes for CritterCab

> **Source article:** [Like Vertical Slice Architecture? Meet Wolverine.HTTP](https://jeremydmiller.com/2026/04/22/like-vertical-slice-architecture-meet-wolverine-http/) by Jeremy D. Miller (2026-04-22)
>
> **Companion sources referenced inline:**
> - [Wolverine.HTTP Guide](https://wolverinefx.io/guide/http/)
> - [From Minimal API to Wolverine.HTTP](https://wolverinefx.io/tutorials/from-minimal-api.html)
> - [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/) — Jimmy Bogard
> - [Wolverine for MediatR Users](https://wolverinefx.io/guide/http/mediator.html)

---

## Why This Matters for CritterCab

CritterCab's architectural commitments and the philosophy of this article are unusually well-aligned. The project is already organized around journey-shaped narratives, command/event slices from Event Modeling workshops, and Wolverine as the core messaging fabric. Miller's article is, in effect, a statement of *which HTTP shape* makes that style of work feel cheap rather than ceremonial — and a defense of why that shape exists.

The single strongest argument Miller makes for CritterCab's purposes is not stylistic but correctness-oriented: **HTTP endpoints in an event-driven system frequently need to write state and publish a message atomically.** Wolverine.HTTP collapses that into one transaction by default. Every slice that originates at the edge of a CritterCab service — *Rider Books a Ride*, *Driver Accepts Trip*, *Trip Completes* — has this exact shape. The slice is "save the aggregate event(s) and emit an integration message." Building each one against raw `IMessageBus` calls would require defending that atomicity per slice; building them against Wolverine.HTTP defends it once.

A secondary alignment: Miller cites informal evidence that **AI-assisted coding works better against vertical slices than layered architectures**. CritterCab's narrative → prompt → execute → retrospective loop is a bet on exactly that proposition.

---

## The Philosophy in Three Layers

The article participates in a three-layer conversation. Reading the article in isolation captures only the top layer.

### Layer 1 — Bogard's Vertical Slice Architecture (the foundation)

Bogard's 2017 framing is the philosophical floor everyone in this conversation is building on. Two ideas from his original post matter:

1. **The architecture is built around distinct requests, encapsulating and grouping all concerns from front-end to back.** Code is organized by feature flow, not by technical layer. The unit of design is the use case, not the noun.

2. **"Minimize coupling between slices, and maximize coupling in a slice."** This single sentence inverts the layered-architecture instinct. Layered architectures minimize coupling *across* layers (controller-service-repository) and tolerate coupling *across* features within a layer. Vertical slices do the opposite. Cohesion lives inside the slice; isolation lives between slices.

Bogard's stated frustration with layered/onion/clean architecture is that they apply rigid rules — "Controller MUST talk to a Service that MUST use a Repository" — to "a minority of the typical requests in a system." Most requests do not need that ceremony. Forcing it produces over-abstraction, mock-heavy tests, and patterns that resist the actual shape of the work.

### Layer 2 — Wolverine.HTTP's design (the framework response)

Wolverine.HTTP's documentation positions the framework as "conducive to vertical slice architecture approaches with significantly lower code ceremony than other .NET web frameworks." Three specific design choices implement that:

- **Endpoints are static class methods, not controllers.** No base class, no constructor injection, no inheritance hierarchy. Every dependency is explicit in the method signature; the method is unit-testable in isolation without mocking infrastructure.

- **Type signatures drive metadata.** OpenAPI shapes, parameter binding, service injection — all inferred from the method signature. No `[FromServices]`, `[FromQuery]`, `[FromBody]` decoration. The framework reads the parameter list and figures it out.

- **Code generation replaces runtime container resolution.** Wolverine generates the glue code at compile time via source generation. No runtime IoC dictionary lookups, no reflection per request, no boxing.

The framework is built **on top of** ASP.NET Core, not around it. Authentication, authorization, rate limiting, output caching, and OpenAPI/Swagger continue to work without Wolverine-specific configuration. This is non-trivial — many ceremony-reducing frameworks achieve their reductions by replacing pieces of ASP.NET Core. Wolverine.HTTP composes with them.

### Layer 3 — The Miller article's specific argument (the rallying point)

Miller's article surfaces three pain points the framework is designed to neutralize:

| Pain point | Miller's framing |
|---|---|
| **MVC controllers** | "Balloon with constructor-injected dependencies" — large classes that obscure their actual collaborators |
| **Minimal API** | Handlers "scattered across multiple files" via `app.MapGet(...)` calls — discovery and navigation are difficult |
| **Mediator libraries** | Bring "their own ceremony and a seam that can make unit testing harder than it should be" |

The mediator critique is the most pointed. The mediator pattern (MediatR-style) was originally a response to controller bloat — a way to push handler logic out of the controller and into focused command/query classes. Miller's argument is that the *handler-as-the-endpoint* shape that Wolverine.HTTP enables makes the mediator's indirection unnecessary. The handler is already a separate class; you do not need a layer between the route and the handler when the route *is* the handler. The Wolverine docs reinforce this with a performance note: "classic mediator usage" pays the cost of extra service location and dictionary lookups that the integrated approach eliminates.

---

## The Transactional Outbox as the Center of Gravity

This is the section to read twice if pressed for time. Miller positions the outbox not as a feature among features but as the reason Wolverine.HTTP exists in its current form.

The problem statement, in Miller's words: *"In any event-driven architecture, HTTP endpoints frequently need to do two things atomically: save data to the database **and** publish a message or event. If you do these as two separate operations and something crashes between them, you've lost a message — or worse, written corrupted state."*

The standard industry response is the [transactional outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html) — write the outgoing message to a table in the same transaction as the state change, then have a separate process forward outbox rows to the actual broker. The pattern is well-known, but implementing it manually in a Minimal API endpoint is non-trivial: the developer must understand the pattern, set up the outbox table, write the dispatcher, integrate it with the handler, and remember to use it on every endpoint that publishes.

Miller's claim is that **Wolverine.HTTP makes the pattern automatic**. Any message returned from an endpoint method — whether as a direct return, part of a tuple, or via a cascading mechanism — is enrolled in the same transaction as the handler's database work. The developer writes a return statement; the framework guarantees the atomicity.

The phrasing he uses for the resulting correctness gap is sharp:

> "This is the kind of correctness that is genuinely difficult to achieve with raw `IMessageBus` calls in Minimal API, and it comes for free in Wolverine.HTTP."

> "No other HTTP endpoint library in .NET has any such smooth integration."

For CritterCab specifically, the implication is that *every* edge-of-service slice — the moment a user action becomes a domain event that needs to propagate to other bounded contexts — gets this guarantee without slice-level coordination work.

---

## Middleware Philosophy: Before/After vs. IEndpointFilter

Miller is critical of ASP.NET Core's `IEndpointFilter` model. His specific complaint:

> "You write a class that implements an interface with a single `InvokeAsync` method that receives an `EndpointFilterInvocationContext`, and you have to dig values out by index or type from the context object. It's not especially readable, and composing multiple filters is verbose."

The Wolverine.HTTP alternative is a **declarative `Before` and `After` method model**. Middleware classes expose ordinary methods named `Before` (or `BeforeAsync`) and `After`. Crucially:

- These methods accept the **same parameters as the endpoint handler** — request body, IoC services, `HttpContext`, even values produced by earlier middleware in the chain.
- A `Before` method may return an `IResult` to short-circuit the request, enabling clean validation patterns.
- The wiring is generated at compile time. No runtime reflection, no boxing, no per-request allocation overhead.

The canonical example Miller cites: built-in FluentValidation middleware in which "every validation failure becomes a `ProblemDetails` response with no boilerplate in the handler itself." The handler stays focused on the happy path; the cross-cutting concern lives where it belongs and composes via the same parameter-injection mechanism the handler uses.

The conceptual point is that **middleware and handlers share a programming model**. They are not two different idioms with different rules; they are the same idiom applied at different points in the request lifecycle. This consistency is what makes the ceremony reduction durable rather than superficial.

---

## What Vertical Slices Mean in This Framing

It is worth being precise about the term, because *vertical slice architecture* is sometimes used loosely.

**In Bogard's original framing**, a vertical slice is a folder containing every type involved in one request — request DTO, response DTO, validator, handler, pipeline behaviors, etc. The slice is the unit of organization in the file tree.

**In Wolverine.HTTP's framing**, a slice is even tighter: ideally a single static class with a single endpoint method, plus any tightly-coupled supporting types. The handler *is* the endpoint *is* the message-publish trigger *is* the unit of testing. There is no surrounding scaffolding because the framework supplies the scaffolding via code generation.

**For CritterCab**, both framings are useful. The narrative slices that come out of Event Modeling workshops naturally produce one HTTP endpoint per command, plus one handler per emitted event for the receiving services. Each of those is a candidate Wolverine.HTTP endpoint or message handler. The journey-shaped narrative spans many slices; each slice is small.

The clearest way to state the alignment: **Event Modeling narratives produce a list of slices. Wolverine.HTTP produces a one-class-per-slice runtime shape.** The slice count in the narrative roughly equals the file count in the implementation.

---

## Distilled Principles

Five principles emerge from reading the three sources together. They are rough rules of thumb, not laws.

1. **Locality over layering.** When a bug is filed against a feature, the developer should be able to open one file. When a feature is removed, the developer should be able to delete one file. Layered architectures violate this; vertical slices restore it.

2. **Explicit dependencies over hidden state.** Method-level parameter injection is preferable to constructor-level field injection because the dependency surface of a slice is visible at the point of use, not buried in a class declaration.

3. **Coupling lives inside the slice; isolation lives between slices.** This is Bogard's coupling principle, restated. A slice that needs three internal helper types is fine. Two slices that share a helper type need a reason.

4. **Atomicity is a framework concern, not a slice concern.** The transactional outbox should be invisible at the slice level. If every slice has to remember to opt into atomicity, atomicity will be missing somewhere.

5. **Middleware and handlers should share an idiom.** If cross-cutting concerns require a different programming model than handlers, the cross-cutting concerns will be where the bugs live.

---

## Decisions Drawn from This Reading

Direction committed during the reading session, recorded here for later promotion to a skill file or ADR.

### Validation lives at the HTTP boundary, not in the aggregate

CritterCab leans **heavily** on Wolverine.HTTP's FluentValidation integration via the compound `Before()` / `Validate()` handlers. Aggregates stay lean and focused on state transitions per the decider pattern (Aggregate Workflow Pattern).

**The seam:**

| Concern | Where it lives | What it rejects |
|---|---|---|
| Input validity (well-formedness) | FluentValidation validator, applied via `Before()` / `Validate()` | Malformed requests — empty IDs, missing required fields, out-of-range values, format violations |
| State-transition validity (legality) | Aggregate's decide step | Illegal transitions — accepting a trip already in flight, cancelling a completed trip, rider booking when blocked |

**Rationale.** Domain-level validation is the right answer for domain entities in non-vertical-slice architectures. CritterCab is deliberately not that. Aggregates here aim to be immutable, lean, and to-the-point — `with`-based state evolution against static `Apply` methods. Handlers hold decision power on purpose; that is the decider pattern's whole premise. Handlers therefore need "protection" at the boundary, and FluentValidation in a `Before()`/`Validate()` middleware delivers that without bloating either the handler or the aggregate.

**Slice file inventory implication.** A slice with non-trivial input requirements has four artifacts: request DTO, **validator**, handler, and (when state-touching) the aggregate's `Apply`/`Decide` methods. The validator is co-located with the slice, not lifted into a shared infrastructure layer.

**Promotion path.** This belongs in a Wolverine.HTTP skill file once the first non-trivial input-validation slice is implemented. An ADR may follow if the BC count grows enough that the rule needs a canonical citation.

### Endpoints are `static`; tests are Alba scenarios first

CritterCab's Wolverine.HTTP endpoints default to **static methods on static classes**. Instance endpoints are reserved for cases where a specific obstruction makes static genuinely unworkable, and any such case requires explicit justification in the slice's prompt or retrospective.

**Why static is the default.** Miller's argument for static endpoints is that they have no class state, no base class, no constructor-injected fields — every dependency arrives through method parameters, and the method is trivially callable in isolation. The instinct that "instance methods are more testable" is an artifact of MVC-era testing, where you needed constructor injection to substitute mocks. Wolverine.HTTP eliminates that need: the parameter list *is* the dependency surface, and the method is already a pure function over those inputs. Reaching for instance form to gain "testability" solves a problem that does not exist in this framework.

**Why Alba carries the testing weight.** Pure unit tests against a static endpoint exercise the handler's decision logic in isolation — useful when that logic is non-trivial enough to warrant input-shape-driven testing. They do not, however, exercise the things that actually break slices in production:

- Middleware ordering (Did `Validate()` run before the handler? Did `Before()` short-circuit when expected?)
- Parameter binding (Did the request body, route values, and query parameters get attributed correctly?)
- Outbox enrolment (Did the cascading message land in the same transaction as the state change?)
- OpenAPI metadata (Does the published shape match what the framework actually accepts?)
- Cross-cutting policies (Did authorization, rate limiting, output caching compose as expected?)

[Alba](https://jasperfx.github.io/alba/) — the Critter Stack's integration test host for ASP.NET Core — runs the real pipeline against the real host. Every concern in the list above is exercised by an Alba scenario the same way it is exercised by a real HTTP request. Mocks cannot reproduce that; integration test hosts can.

**The two-tier test split for a CritterCab slice:**

| Tier | Question it answers | Tool |
|---|---|---|
| Pure unit (when warranted) | Does the handler's decision logic produce the right output for these inputs? | Direct call to the static method; xUnit/NUnit assertions |
| Slice-scoped integration | Does the slice work end-to-end through the real pipeline — binding, validation, handler, outbox, response? | Alba scenario |

Most slices need only the integration tier. The pure unit tier earns its place when the handler's decision branches are numerous enough that exercising them via HTTP is wasteful — typically state-transition-heavy aggregate handlers where Alba covers the happy path and direct calls cover the long tail.

**Promotion path.** This belongs alongside the Wolverine.HTTP skill file as a *test conventions* skill file, authored once the first slice with both tiers exists. The conventions worth codifying then: file-organization for Alba scenarios within the slice folder, test naming, fixture sharing, and the rule for when pure unit tests join an Alba scenario rather than replace it.

---

## Open Questions for CritterCab

Reading these sources surfaces decisions CritterCab has not yet committed to. These are noted for later — not resolved here.

- **Endpoint vs. message-handler split per slice.** Some slices arrive at a service via HTTP (Rider's mobile app posts a booking). Others arrive via Wolverine message (a different service publishes a "TripAccepted" event the Pricing BC reacts to). The article discusses the HTTP shape; the Wolverine message shape is symmetric. The slice template should probably accommodate both. Defer until the second narrative is implemented.

- **Where the outbox sits per service.** Wolverine's outbox can ride on Marten (event-sourced services) or Polecat (document-store services). CritterCab's bounded contexts will not all use the same persistence. The outbox configuration is per-service. Defer until the first persistence-touching slice is implemented.

---

## References

- **Article (primary):** Jeremy D. Miller, *Like Vertical Slice Architecture? Meet Wolverine.HTTP*, 2026-04-22 — https://jeremydmiller.com/2026/04/22/like-vertical-slice-architecture-meet-wolverine-http/
- **Wolverine.HTTP Guide:** https://wolverinefx.io/guide/http/
- **From Minimal API tutorial:** https://wolverinefx.io/tutorials/from-minimal-api.html
- **Wolverine for MediatR Users:** https://wolverinefx.io/guide/http/mediator.html
- **Vertical Slice Architecture (Bogard):** https://www.jimmybogard.com/vertical-slice-architecture/
- **Transactional Outbox Pattern (Richardson):** https://microservices.io/patterns/data/transactional-outbox.html
