---
name: _template
description: "CritterCab skill authoring template. NOT AN ACTIVATABLE SKILL. Copy this directory to start a new skill; see inline comments for conventions."
cluster: core
tags: [meta, template]
---

<!--
  CritterCab Skill Template
  =========================
  This file is the canonical starting point for every new skill in this repo.

  How to use:
    1. Copy this directory:
       cp -r docs/skills/_template docs/skills/<your-skill-name>
    2. Rename the directory to match the `name` field (kebab-case).
    3. Fill in the frontmatter and replace placeholder content.
    4. Update See Also references with actual skill names.
    5. If the skill grows past ~500 lines, move deep-dive material into
       references/<topic>.md files within the skill directory.

  ---------------------------------------------------------------------------
  Frontmatter fields
  ---------------------------------------------------------------------------
  name (required)
    kebab-case. MUST match the directory name. Loaded at agent startup
    alongside the description.

  description (required)
    What the skill does + when to use it. Loaded at agent startup for every
    skill in the library, so every word counts. Format suggestion:
      "<topic>: <key concepts>. Use when <activation trigger>."
    Aim for under ~200 characters. Lead with the activation trigger if the
    skill is rarely used; lead with the topic if it's foundational.

  cluster
    Discoverability metadata. Single value. Pick one of:

      Product/library clusters:
        core             - universal patterns not tied to a specific tool
        wolverine        - Wolverine-specific patterns
        marten           - Marten-specific patterns
        polecat          - Polecat-specific patterns
        aspire           - Aspire (programming model) patterns

      Topic/concern clusters:
        grpc                  - gRPC and Protobuf, across products
        transports            - messaging transports (Kafka, ASB, etc.)
        distributed-services  - cross-cutting service architecture
        identity              - identity ACL, OIDC, provider integration
        polyglot              - non-.NET services (Go, etc.)
        testing               - test patterns and tooling
        observability         - tracing, metrics, dashboards
        cli-tooling           - CLI tools across the stack

    Disambiguation rule when a skill spans both axes (e.g., wolverine-kafka):
    pick the cluster that captures the PRIMARY VALUE of the skill. The
    secondary axis goes in `tags`.
      wolverine-kafka.md         -> cluster: transports, tags: [wolverine, ...]
      wolverine-grpc-services.md -> cluster: grpc,       tags: [wolverine, ...]
      wolverine-message-handlers -> cluster: wolverine   (no more-specific topic)
      marten-aggregates          -> cluster: marten      (no more-specific topic)
      cli-aspire                 -> cluster: cli-tooling, tags: [aspire, ...]
      aspire (programming model) -> cluster: aspire,     (primary value is the model)

    When in doubt, use `core`.

  tags
    Freeform concept tags for filtering and grouping. Mix stack and concern
    (wolverine, marten, polecat, grpc, kafka, asb, eventhubs, aspire,
    testing, observability, design, deployment, etc.). 3-6 tags is typical.

  ai_skills_prerequisite (optional)
    Name of the JasperFx ai-skills skill that covers the underlying generic
    mechanics. Use when this skill defers to ai-skills for foundational
    content and only documents project-specific decisions on top.

  ---------------------------------------------------------------------------
  Length guideline (pragmatic, not strict)
  ---------------------------------------------------------------------------
  Aim for SKILL.md under 500 lines. The agentskills.io spec recommends this
  ceiling; we honor it as a guideline. When content earns more space and
  reads better as continuous prose, let it run — cohesion beats arbitrary
  line caps. Move conditionally-loaded deep-dive material into references/.
-->

# Skill Title

> One-line summary of what this skill teaches.
> *Optional second line: which ai-skills skill provides the underlying generic mechanics, if applicable.*

## When to apply this skill

Use this skill when:

- Concrete activation trigger (task type or task description)
- Another concrete activation trigger

Do NOT use this skill when:

- Anti-trigger description, with a pointer to the skill that should be used instead.

## Prerequisites

<!--
  Optional. Delete this entire section if there are no prerequisites.

  Prerequisites are skills the agent should have loaded (or be familiar
  with) before this one will make sense. Distinct from See Also -> Upstream
  in that prerequisites are *required* context, not just useful neighbors.
-->

- `[other-cab-skill]` — brief reason this skill assumes its content.
- ai-skills `[ai-skills-name]` — brief reason. Assumes installed at user level via `npx skills add`.

## (Core content sections — replace heading and structure to fit the skill)

<!--
  The main material. Section structure varies per skill type:

    Decision-aid skill:
      - List of decisions, each with criteria and the rationale for the
        recommended choice.

    Pattern skill:
      - Pattern name -> when to use -> example -> pitfalls.

    Convention skill:
      - Rule -> rationale -> example. One subsection per rule.

    CLI skill:
      - Command -> when to reach for it -> expected output -> common flags.

    Reference skill:
      - Topic -> definition -> usage notes -> cross-references.

  Use code fences for code, command lines, and configuration. Tag with
  language for syntax highlighting (csharp, bash, yaml, json, proto).
  Inline code with single backticks for type names, command names, file
  paths, and short identifiers.
-->

(replace with actual content)

## Common pitfalls

<!--
  Optional. Delete if none apply.

  Things that look right but aren't. The shape that catches an agent or a
  fresh contributor going faster than they should. One short paragraph per
  pitfall is usually enough.
-->

- **Pitfall name.** Brief description of the mistake and why it bites.

## See also

<!--
  Cross-reference format:
    - Use backticks around skill names.
    - Group as Upstream (load first), Downstream (natural follow-ups),
      and External (ai-skills, ADRs, vision docs, narratives).
    - One short clause per reference saying what's covered there.
    - This section is the spine of the skill graph; keep it accurate.
-->

**Upstream** — load these first if unfamiliar:

- `[upstream-skill]` — what it covers that this skill assumes.

**Downstream** — natural follow-ups when this skill's content is settled:

- `[downstream-skill]` — what comes next.

**External:**

- ai-skills `[skill-name]` — generic mechanics. Install via `npx skills add` (license required).
- ADR-XXX in [`docs/decisions/`](../../decisions/) — decision context.
- [`docs/vision/README.md`](../../vision/README.md) § Section name — project-level rationale.
