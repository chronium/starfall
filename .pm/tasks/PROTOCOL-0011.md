---
id: PROTOCOL-0011
title: Add Fire Arrow facts and serialization
track: PROTOCOL
milestone: M2
dependsOn:
- SIM-0009
- PROTOCOL-0007
createdAt: 2026-08-05T19:46:44.5364970Z
modifiedAt: 2026-08-05T19:47:21.5475320Z
---

Extend the proven first connected-combat contract with focused Fire Arrow facts and deterministic serialization.

Acceptance criteria:
- Define an entity-target Fire Arrow command tied to the admitted actor, with non-zero command sequence, stable action and target identity, authoritative start/resolve ticks, acceptance, rejection and cancellation.
- Carry integer mana expenditure, 700-unit damage, effective damage and target defeat facts from SIM-0009 without embedding simulation rules.
- Reuse the bounded envelope and malformed-input conventions established by PROTOCOL-0007.
- Preserve presentational arrows and authoritative fixed-tick outcomes.
- Do not implement server routing, client controls, projectile entities, effects, a generic ability protocol or Arrow Rain.