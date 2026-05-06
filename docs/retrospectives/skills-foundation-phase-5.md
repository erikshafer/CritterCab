# Retrospective — CritterCab Skill Library, Phase 5 (Reconciliation)

## Metadata

- **Triggering prompt:** [`docs/prompts/skills-foundation-phase-5-handoff.md`](../prompts/skills-foundation-phase-5-handoff.md)
- **Status:** In progress
- **Date authored:** 2026-05-06 (running record; updated as Phase 5 progresses)
- **Output artifacts:** Per-skill trims to `docs/skills/*/SKILL.md` files; this retrospective; eventual README close-out.
- **Outcome:** _(to be filled in at session end)_

---

## Framing

Phase 5 is the reconciliation pass against the JasperFx ai-skills library. Unlike Phases 1–4, no new skills are authored; instead, each of the 39 existing Cab skills is checked against ai-skills for direct equivalents, deduplicated where appropriate, and flagged for upstream-contribution where Cab's coverage exceeds ai-skills'.

Methodology per skill (mirrors Phase 4 cadence):

1. Identify ai-skills counterpart.
2. Read both end-to-end.
3. Audit Cab's sections against four categories (no equivalent / direct equivalent / upstream-contribution candidate / upstream-replacement candidate).
4. Pause and report findings to the user before any edits.
5. Apply trims after greenlight.
6. Record outcome here.

Convention adopted in Phase 5: Cab `See Also` sections distinguish three flavors of cross-reference:

- **`Upstream`** — the authoritative external reference library this Cab skill defers to. In practice this means **ai-skills** (license required, install via `npx skills add`). Entries drop the `ai-skills` prefix since the heading establishes scope. The lead-in line names the licensing once. **This is a semantic shift from Phases 1–4**, where `Upstream` meant Cab-internal load-first prerequisites.
- **`Prerequisites`** — Cab-internal skills to load first if unfamiliar with the project conventions assumed by this skill. This is the new label for what Phases 1–4 called `Upstream`.
- **`External`** — non-ai-skills external references (Wolverine docs, blog posts, papers, RFCs).

The `Upstream` section comes first in `See Also` (foundational reference), followed by `Prerequisites` (Cab orientation), then `Sibling skills`, `Downstream`, and `External`.

The rename `Upstream` → `Prerequisites` happens as part of each skill's Phase 5 reconciliation, alongside the addition of the new `Upstream` (ai-skills) block. Cross-references in other skills are unaffected because they reference skill names, not section names within those skills.

---

## Pre-flight (recorded at session start)

- **ai-skills location confirmed:** `C:\Code\JasperFx\ai-skills\skills\` (locally cloned, ~70 skills covering Wolverine, Marten, Polecat, integrations, testing, observability, architecture).
- **Working environment:** Filesystem MCP + Windows-MCP, same as Phase 4.
- **No new skills queued:** confirmed; Phase 5 is reconciliation only.
- **Phase 4 retrospective:** read; methodology lessons absorbed (pause-after-each-skill, source-verify-not-memory, check-if-already-edited, forward-reference scans).
- **Phase 5 retrospective baseline:** none — this file is being authored from scratch as Phase 5 progresses.

Reference repo HEAD revisions at session start: _(not captured — would have been ideal per Phase 4 methodology lesson; capture at next session if Phase 5 spans multiple sessions)._

---

## Per-skill reconciliation table

| # | Skill | Tier | ai-skills counterpart | Category | Lines saved | Notes |
|---|---|---|---|---|---|---|
| 1 | `wolverine-handlers` | 1 | `wolverine-handlers-fundamentals` (+ 7 fragmented siblings) | Direct equivalent (deduplicated) | ~18 | Hub-skill structure preserved; trimmed Lambda Factory anti-pattern + Logger Convention duplication; promoted `External` ai-skills entries to new `Upstream (ai-skills)` block |
| 2 | `wolverine-http-handlers` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 3 | `wolverine-messaging-handlers` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 4 | `wolverine-grpc-handlers` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 5 | `wolverine-grpc-bidirectional-handlers` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 6 | `wolverine-kafka` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 7 | `wolverine-azure-service-bus` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 8 | `wolverine-sagas` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 9 | `marten-aggregates` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 10 | `marten-wolverine-aggregates` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 11 | `marten-projections` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 12 | `marten-querying` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 13 | `marten-async-daemon` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 14 | `dynamic-consistency-boundary` | 1 | _pending_ | _pending_ | _pending_ | _pending_ |
| 15 | `polecat-event-sourcing` | 2 | _pending_ | _pending_ | _pending_ | _pending_ |
| 16 | `polecat-document-store` | 2 | _pending_ | _pending_ | _pending_ | _pending_ |
| 17 | `csharp-coding-standards` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 18 | `domain-event-conventions` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 19 | `event-modeling` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 20 | `protobuf-contracts` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 21 | `transport-selection` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 22 | `grpc-vs-other-transports` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 23 | `adding-a-service` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 24 | `service-bootstrap` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 25 | `vertical-slice-organization` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 26 | `distributed-saga-considerations` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 27 | `identity-acl` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 28 | `polyglot-go-service` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 29 | `aspire` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 30 | `cli-aspire` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 31 | `cli-jasperfx` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 32 | `cli-grpc-tooling` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 33 | `cli-kafka-tooling` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 34 | `cli-azure-messaging` | 3 | _pending_ | _pending_ | _pending_ | _pending_ |
| 35 | `observability-tracing` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |
| 36 | `observability-metrics` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |
| 37 | `testing-fundamentals` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |
| 38 | `testing-integration` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |
| 39 | `testing-advanced` | 4 | _pending_ | _pending_ | _pending_ | _pending_ |

---

## Per-skill detail

### 1. `wolverine-handlers`

**Counterpart(s).** Cab's hub skill maps to a fragmented set of 9 ai-skills:

- `wolverine-handlers-fundamentals` — the closest single counterpart (handler shape, discovery, return types, Logger convention).
- `wolverine-handlers-pure-functions` — decider pattern, A-Frame.
- `wolverine-handlers-a-frame-architecture` — infrastructure-at-edges principle.
- `wolverine-handlers-railway-programming` — `Validate` / `ProblemDetails` pipeline.
- `wolverine-handlers-declarative-persistence` — `[Entity]`, `[WriteAggregate]`, `[ReadAggregate]`.
- `wolverine-handlers-middleware` — full lifecycle, `OnException`, scoping.
- `wolverine-handlers-ioc-and-service-optimization` — service location, codegen, Lamar.
- `marten-aggregate-handler-workflow` — full aggregate workflow.
- (`wolverine-handlers-efcore`, `query-plans` — orthogonal; not Cab-relevant.)

Cab's hub-skill structure is structurally compatible: it acts as a Cab-specific orienting layer that defers protocol-specific work to siblings (`wolverine-http-handlers`, `wolverine-messaging-handlers`, `wolverine-grpc-handlers`, `wolverine-grpc-bidirectional-handlers`) and generic mechanics to the 8 ai-skills above.

**Section categorization.**

| Section | Category | Action |
|---|---|---|
| Top framing + sibling list | Cab-specific | Kept; already serves as the "why this skill exists alongside ai-skills" paragraph |
| When to apply this skill | Cab-specific | Kept |
| Cab-specific orientation (`MartenOps`/`PolecatOps` deltas, `[WriteAggregate]` parameter-level decision, recommended ai-skills reading order) | Cab-specific | Kept |
| CritterCab Handler Shape (`AcceptOffer` example + 6 bullets) | Cab-specific | Kept |
| Validate vs ValidateAsync (three-layer FluentValidation→Validate→ValidateAsync ordering) | Cab-specific extension | Kept |
| Handler Return Types — Orientation (table) | Cab-specific projection of return types onto Cab shapes | Kept |
| Anti-Pattern: `IStartStream` not returned | Upstream-contribution candidate (Marten-side) + Cab-specific (Polecat-side) | Kept; flagged for upstream contribution |
| Anti-Pattern: Lambda Factory Service Registrations | Duplicates ai-skills `wolverine-handlers-ioc-and-service-optimization` | **Trimmed** to brief Cab convention + upstream pointer |
| Logger Convention | Duplicates ai-skills `wolverine-handlers-fundamentals` | **Trimmed** to single-line note + upstream pointer |
| Diagnosing Handler Issues | Cab-specific framing (symptom→command table) | Kept |
| See Also › External (8 ai-skills entries) | Restructure | **Promoted** to new `Upstream (ai-skills)` block |

**Trim impact.** ~18 lines removed (Lambda Factory section ~22 → ~5; Logger Convention ~6 → 2; External → restructured into Upstream (ai-skills) + minimal External, net ~+3 from new heading and framing). Skill went from 322 → ~304 lines.

**Convention established.** This skill set the Phase 5 `See Also` convention. After Erik's review, the convention was refined to a cleaner three-block structure (instead of the awkward two-`Upstream` form initially applied):

- **`Upstream`** — ai-skills counterparts (the authoritative external reference). Comes first in `See Also`. License/install note in the lead-in line; entries drop the `ai-skills` prefix (heading establishes scope).
- **`Prerequisites`** — Cab-internal load-first skills (renamed from what Phases 1–4 called `Upstream`).
- **`External`** — non-ai-skills external references only (Wolverine docs link, blog posts).

The `Upstream` → `Prerequisites` rename is part of every Phase 5 skill reconciliation. Subsequent skills follow the same three-block layout and ordering.

**Upstream-contribution candidate flagged.** The `session.Events.StartStream<T>(...)` direct-call silent-failure footgun (Marten-side) belongs in a future ai-skills `wolverine-handlers-fundamentals` or `marten-aggregate-handler-workflow` revision. Cab will continue carrying it until ai-skills covers it.

---

## Methodology refinements emerging in Phase 5

_(updated as the reconciliation progresses)_

1. **`See Also` three-block convention** (from Skill 1, refined after review). `Upstream` (ai-skills) → `Prerequisites` (Cab-internal) → `Sibling skills` → `Downstream` → `External`. Adopted as the Phase 5 standard; subsequent skills follow it. The rename `Upstream` → `Prerequisites` happens uniformly across every Phase 5-reconciled skill.
2. **License framing in `Upstream` lead-in.** Each skill's `Upstream` block lead-in mentions "ai-skills (license required, install via `npx skills add`)" once. ai-skills content is never inlined into Cab skills — only skill names are referenced. This honors the proprietary/licensed status of ai-skills.
3. _(more entries to come)_

---

## Upstream-contribution roadmap

Compiled list of Cab patterns/sections flagged as upstream-contribution candidates during Phase 5. To be synthesized at Phase 5 close-out into a prioritized list for post-Phase-5 follow-up.

| # | Skill | Pattern / section | Rationale | Priority |
|---|---|---|---|---|
| 1 | `wolverine-handlers` | `session.Events.StartStream<T>(...)` direct-call silent-failure footgun (Marten-side) | Generic Wolverine+Marten pitfall not covered in any current ai-skills handler skill; reproduces silently and is hard to diagnose | _TBD at close-out_ |

---

## Cross-cutting passes (Pass A / Pass B / Pass C)

To be executed after all 39 per-skill reconciliations complete.

- **Pass A** — Validate every `See Also › External` ai-skills reference across the library. Promote correct entries to `Upstream (ai-skills)`; remove or correct false promises.
- **Pass B** — README cluster tables and entry-point hubs reviewed for adjustments (most likely no changes needed; topology unchanged).
- **Pass C** — Synthesize the upstream-contribution roadmap into a prioritized post-Phase-5 follow-up plan.

---

## Quantitative summary

_(updated at session end)_

- Skills reconciled: 1 / 39
- Total lines trimmed: ~18
- Direct-equivalent (deduplicated): 1
- No equivalent: 0
- Upstream-contribution candidates: 1
- Upstream-replacement candidates: 0
