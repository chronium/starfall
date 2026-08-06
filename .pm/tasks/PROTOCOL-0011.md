---
id: PROTOCOL-0011
title: Add Fire Arrow facts and serialization
track: PROTOCOL
priority: none
dependsOn:
- SIM-0009
- PROTOCOL-0007
- PROTOCOL-0014
createdAt: 2026-08-05T19:46:44.5364970Z
modifiedAt: 2026-08-06T06:43:25.2045640Z
---

Extend the proven connected Basic combat envelope with focused Fire Arrow facts and deterministic serialization.

Acceptance criteria:
- Define an entity-target Fire Arrow command tied to the admitted actor, with non-zero command sequence, stable action and target identities, authoritative start/resolve ticks, acceptance, rejection and cancellation.
- Carry authoritative mana expenditure and outcome facts from the completed Mana contract plus 700-unit damage, effective damage and target defeat from SIM-0009.
- Reuse PROTOCOL-0007 bounds and malformed-input conventions and PROTOCOL-0014 mana facts without defining mana behavior.
- Preserve presentational arrows and authoritative fixed-tick outcomes.
- Do not implement server routing, client controls, projectile entities, effects, permanent HUD, a generic ability protocol or Arrow Rain.