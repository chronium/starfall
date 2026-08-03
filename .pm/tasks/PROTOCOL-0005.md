---
id: PROTOCOL-0005
title: Add bounded monster facts and serialization
track: PROTOCOL
milestone: M2
dependsOn:
- PROTOCOL-0004
- SIM-0006
- SIM-0010
- SIM-0011
createdAt: 2026-08-03T07:29:08.6092280Z
modifiedAt: 2026-08-03T07:29:43.4027730Z
---

Extend the established connected-walking protocol with bounded monster snapshot facts and deterministic serialization.

Acceptance criteria:
- Represent stable monster identity, archetype, transform, behavior/target, integer health, death, disengage and return state.
- Preserve fixed ticks, finite single-precision metre components, explicit ordering and bounded collection sizes.
- Add deterministic round-trip and malformed/non-finite rejection coverage.
- Combine facts and serialization deliberately as one bounded extension of the proven snapshot envelope.
- Do not add combat actions, asset presentation, AI rules or a generic message framework.