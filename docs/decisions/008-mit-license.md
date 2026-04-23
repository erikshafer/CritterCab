# ADR-008: MIT License

**Status:** Accepted  
**Date:** 2026-04-23

## Context

CritterCab is an open-source reference architecture. Its purpose is to be read, studied, forked, and adapted by developers evaluating the Critter Stack. A license is required for that use to be unambiguous. Without one, the default in most jurisdictions is all-rights-reserved, which prevents the very thing the project exists to enable.

The choice of license is a signal about what the project is for. A restrictive license (GPL-family) communicates that the maintainer has conditions on downstream use. A permissive license (MIT, Apache 2.0) communicates that the project wants to be used as widely as possible with minimal friction.

## Options Considered

### Option A — No license

The repository exists on GitHub without a LICENSE file. GitHub's terms of service allow public viewing, but forking for use in another project is not legally permitted without explicit permission.

This is an oversight posture, not a deliberate one. It maximizes legal ambiguity while providing no protection that a real license would not also provide. Not appropriate for a project that is explicitly intended to be a reference.

### Option B — GPL or AGPL

A copyleft license requires that derivative works be distributed under the same terms. This is appropriate when the project's goal is to ensure that improvements flow back to the commons.

CritterCab is a reference architecture, not a library or a platform. Developers evaluating the Critter Stack do not "distribute" a ride-sharing reference implementation — they read it, learn from it, and apply the patterns in their own systems. Copyleft creates legal uncertainty about whether adapting patterns from a GPL-licensed reference architecture requires GPL-licensing the adapter's own system. That uncertainty is friction that directly opposes the project's goal.

### Option C — Apache 2.0

Apache 2.0 is a permissive license with one addition over MIT: an explicit patent grant. Contributors license any patents they hold that are practiced by their contribution, and the license terminates for anyone who brings a patent claim against the project.

Apache 2.0 is reasonable and widely used. The patent grant is a genuine addition for projects where patent exposure is a concern. For CritterCab, patent exposure is not a meaningful concern — the project implements well-known patterns against existing open-source libraries, and its contributors are not large organizations with active patent portfolios.

### Option D — MIT

MIT is the most permissive common license. It allows use, modification, distribution, and sublicensing with no conditions beyond attribution. It is the license used by Wolverine, Marten, Polecat, and the JasperFx project family.

## Decision

**Option D.** CritterCab is MIT licensed.

MIT aligns with the JasperFx ecosystem CritterCab showcases, imposes zero friction on developers who want to fork, adapt, or reference the code, and requires no legal analysis on the part of adopters. The absence of a patent clause is an acceptable gap given the project's nature and contributor profile.

## Consequences

The LICENSE file in the repository root governs all content in the repository unless a file specifies otherwise. Attribution is required when the code is redistributed; reading and adapting the patterns does not require attribution.

Future contributors implicitly license their contributions under MIT by submitting a PR. No Contributor License Agreement is required.
