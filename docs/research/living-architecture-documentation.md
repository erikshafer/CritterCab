# Living Architecture Documentation — Notes for CritterCab

> **Source series:** Two blog posts by Nick Tune, October 2025.
> - [Defining a DSL for Extracting Software Architecture as Living Documentation](https://nick-tune.me/blog/2025-10-31-defining-a-dsl-for-extracting-software-architecture-as-livin/) — Oct 31
> - [Enforcing Software Architecture Living Documentation Conventions](https://nick-tune.me/blog/2025-10-31-enforcing-software-architecture-living-documentation-convent/) — Oct 31

---

## Why This Matters for CritterCab

CritterCab is designed as a reference architecture — a showcase of DDD-aligned service boundaries, Wolverine messaging patterns, and event sourcing with Marten. For a reference architecture, the living documentation *is* the product. Diagrams that drift from the code, or documentation that requires manual maintenance, undermine the entire purpose.

Tune's series addresses exactly this: how to extract architectural information from code automatically, and — critically — how to ensure that the conventions the extraction relies on are actually present and enforced. The second step is what makes the first trustworthy.

The series was developed in TypeScript, but every concept is directly transferable to C# with .NET-native equivalents.

---

## Part 1: Defining a DSL for Architecture Extraction

### The Core Problem

Writing a bespoke extraction script per codebase — one that walks the AST, identifies types, and builds a dependency graph — is feasible but expensive to maintain. Tune's answer is a `ModelElement` DSL: a typed, declarative description of what architectural elements look like in a given codebase, decoupled from the mechanics of how the extraction works.

### ModelElement Detection Strategies

Tune defines four detection strategies, each suited to different codebases and convention styles:

**1. Naming convention detection**
```typescript
export const repositoryDefinition = defineModelElement({
  type: 'Repository',
  detection: typeSuffix('Repository'),
})
```
Works well for new codebases with consistent naming. Brittle if conventions drift.

**2. Type-based detection (interface implementation)**
```typescript
export const domainEventDefinition = defineModelElement({
  type: 'DomainEvent',
  detection: extendsOrImplements(DomainEvent),
})
```
Preferred when types carry semantic meaning through their inheritance or interface hierarchy. More reliable than naming.

**3. Directory and method pattern detection**
```typescript
export const useCaseDefinition = defineModelElement({
  type: 'UseCase',
  detection: directoryPattern('**/use-cases/**', {
    hasMethod: { anyOf: ['apply', 'handle', 'execute'] },
  }),
})
```
Useful for legacy codebases lacking consistent type-level conventions.

**4. Decorator-based detection with custom field extraction**
```typescript
export const httpEndpointDefinition = defineModelElement({
  type: 'HttpEndpoint',
  detection: decorator(HTTPEndpoint, 'method'),
  customFields: {
    path: fromDecoratorArgs('path'),
    method: fromDecoratorArgs('method'),
  },
})
```
The most expressive option. Extracts metadata from attribute arguments, not just type presence.

### The Enterprise Scale Insight

For a single codebase, a custom extraction script is reasonable. For multiple codebases (the scenario CritterCab represents — multiple services, each with its own conventions), the per-codebase cost multiplies. Tune's answer is to decouple two things that are often conflated:

- **What** a model element is (definition — shared platform-level concept)
- **How** to find it in this specific codebase (detection rule — per-team configuration)

```typescript
const rules = {
  Repository: { detection: typeSuffix('Repository') },
  HTTPEndpoint: { detection: decorator(HTTPEndpoint, 'method') },
} satisfies AwesomePlatformModelElementDetectionRules
```

The type constraint (`satisfies`) ensures that per-team rules cover all required model element types without over-specifying the detection method. Different services can use different detection conventions without diverging on what concepts exist.

### Rivière

Tune is developing [Rivière](https://github.com/NTCoding/riviere), an open-source toolkit for extracting and visualizing flow-based architecture from codebases. It traces how operations actually execute through a system and uses AI assistance to analyze code and generate living documentation. Worth watching as CritterCab's implementation matures.

---

## Part 2: Enforcing Conventions

### The Trust Problem

Living documentation generated from code is only trustworthy if the conventions the extraction relies on are actually present. A diagram claiming that `OrderRepository` is a Repository means nothing if the codebase contains types named `OrderRepository` that are not actually repositories.

Tune's second article addresses this: before you trust the output, you must enforce the input.

### The @Role() Decorator Approach (TypeScript)

The proposed solution adds an explicit architectural marker to every class:

```typescript
@Role('Repository')
class OrderRepository implements IOrderRepository {
  // ...
}
```

Valid roles: `Repository`, `Aggregate`, `HttpEndpoint`, `EventHandler`, `Ignored`.

An ESLint rule enforces this by:
- Detecting class declarations missing a `@Role()` decorator
- Validating that the role value is one of the approved set
- Auto-excluding test files
- Providing clear, actionable error messages

The key benefit over naming conventions: **IDE feedback without running a command.** The lint error appears immediately, before any extraction or CI run.

### Alternatives

**ts-arch** — a library specifically for architectural unit testing in TypeScript. Expresses architectural rules as test assertions:
```typescript
expect(files().inSrc('repositories')).toHaveDecorator('@Role', 'Repository')
```

**JSDoc enforcement** — for functional/function-based codebases where class decorators don't apply.

**Specialized decorators** — for complex requirements like event handler validation, a specialized decorator can extract information from the actual message subscription types rather than requiring manual specification:
```typescript
@EventHandler(OrderPlaced)
async handle(event: OrderPlaced) { ... }
// Extraction infers: this is an event handler for OrderPlaced — no manual annotation needed
```

---

## C# / .NET Equivalents

CritterCab is a C# codebase. The TypeScript tooling Tune uses (`ts-morph`, ESLint, `ts-arch`) does not apply directly, but well-established .NET equivalents exist.

### Custom Attributes as @Role() Equivalents

C# attributes are the natural analogue to TypeScript decorators:

```csharp
[ArchitecturalRole(Role.Repository)]
public class OrderRepository : IOrderRepository { ... }

[ArchitecturalRole(Role.Aggregate)]
public class Order { ... }

[ArchitecturalRole(Role.EventHandler)]
public class OrderPlacedHandler : IMessageHandler<OrderPlaced> { ... }
```

Define `Role` as an enum and `ArchitecturalRole` as a custom attribute. Extraction tooling reads the attribute; enforcement tooling validates its presence.

### ArchUnitNET for Convention Enforcement

[ArchUnitNET](https://archunitnet.readthedocs.io/) is the .NET equivalent of ts-arch. It expresses architectural rules as unit tests:

```csharp
[Fact]
public void Repositories_must_be_annotated_with_ArchitecturalRole()
{
    Classes().That().HaveNameEndingWith("Repository")
             .Should().HaveCustomAttribute(typeof(ArchitecturalRole))
             .Check(architecture);
}
```

ArchUnitNET can enforce:
- Namespace/project membership rules (no cross-bounded-context references)
- Dependency direction (domain layer cannot reference infrastructure layer)
- Annotation presence (all aggregates must carry a marker attribute)
- Forbidden dependencies (no service-to-service project references)

### Roslyn Analyzers

For IDE-integrated enforcement (equivalent to ESLint), Roslyn analyzers fire in the editor without a separate command:

- Flag types that look like repositories (by name convention or interface) but lack the `[ArchitecturalRole]` attribute
- Flag cross-bounded-context `using` statements
- Flag Wolverine handler types that are not in the expected namespace

Writing a custom Roslyn analyzer is more effort than ESLint, but the principle is identical and the IDE integration is equivalent.

### Wolverine and Marten as Natural Detection Anchors

CritterCab's stack provides semantic hooks that make many naming-convention detections unnecessary:

| Architectural concept | Natural detection in CritterCab |
|---|---|
| Message handler | Implements `IMessageHandler<T>` or uses `[WolverineGet]`/`[WolverinePost]` |
| Domain event | Published via Wolverine's message bus; extends a marker base type |
| Aggregate (Marten) | Uses `[AggregateHandler]` or is the target of `IEventStore.For<T>()` |
| Document | Implements `IDocument` or is registered in `StoreOptions` |
| gRPC endpoint | Implements a Protobuf-generated service base class |
| Saga | Extends `Saga` or implements `ISaga<T>` in Wolverine |

This means a CritterCab extraction DSL can lean heavily on type-based detection (`extendsOrImplements`) rather than brittle naming conventions. The framework is doing the annotation work already.

---

## Application to CritterCab

### Bounded Context Membership

CritterCab's multi-service structure means the most important architectural convention to enforce is **bounded context membership**: no type in one service should reference types from another service's domain.

ArchUnitNET makes this expressible as a test:
```csharp
Classes().That().ResideInNamespace("CritterCab.Dispatch")
         .Should().NotDependOnAnyTypesThat()
         .ResideInNamespace("CritterCab.Rides")
         .Check(architecture);
```

This enforces one of CritterCab's non-negotiables (no cross-service internal references) in code, not prose.

### Transport Convention Enforcement

CritterCab's transport decision (Kafka for high-volume telemetry, Azure Service Bus for business events, gRPC for service calls) could be enforced architecturally:

- Types in `Telemetry` namespace must not reference Azure Service Bus types
- gRPC endpoint types must derive from Protobuf-generated base classes (not custom interfaces)
- High-volume event types must not be published via Azure Service Bus endpoints

These rules are currently expressed as prose in CLAUDE.md and the vision doc. ArchUnitNET tests would make them enforceable before a PR merges.

### Diagram Generation

Once extraction conventions and their enforcement are in place, a Mermaid diagram can be generated from the extracted model. For CritterCab as a reference project, auto-generated diagrams in the repository serve as verifiable documentation: if the code compiles and the architecture tests pass, the diagram is correct.

---

## Actionable Recommendations

Ordered by implementation phase (these apply once code exists):

1. **Define a `[BoundedContext("...")]` attribute for every service project.** Apply it to the assembly or to a central assembly-info file. This becomes the extraction anchor that identifies which service a type belongs to.

2. **Write ArchUnitNET tests for cross-service reference violations.** These are the highest-value architectural tests for CritterCab's non-negotiables. Start with the two most different bounded contexts.

3. **Leverage Wolverine interfaces for handler detection over naming conventions.** When writing extraction tooling, prefer `typeof(IMessageHandler<>)` as the detection anchor rather than class name patterns. The framework interface is the authoritative marker.

4. **Consider a `[ArchitecturalRole]` attribute for types that don't have a framework anchor.** Domain services, value objects, and pure domain model types won't implement framework interfaces. A simple attribute keeps the extraction clean.

5. **Wire architecture tests into the CI gate.** ArchUnitNET tests run as ordinary xUnit/NUnit tests. Adding them to the build verification step ensures architectural drift is caught at PR time, not in a diagram review.

6. **Evaluate Rivière as CritterCab matures.** Tune's toolkit targets flow-based extraction — tracing how a message moves through handlers, sagas, and projections. This is directly relevant to CritterCab's Wolverine-heavy message flows and could generate verifiable sequence diagrams automatically.
