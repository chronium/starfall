---
id: PROTOCOL-0011
title: Add Fire Arrow facts and serialization
track: PROTOCOL
priority: none
dependsOn:
- SIM-0009
- PROTOCOL-0016
- PROTOCOL-0014
createdAt: 2026-08-05T19:46:44.5364970Z
modifiedAt: 2026-08-07T08:34:29.5302580Z
---

Extend the proven connected combat envelope with focused Fire Arrow facts while reusing the authoritative straight-projectile protocol contract.

Acceptance criteria:
- Define an entity-target Fire Arrow command tied to the admitted actor, with non-zero command sequence, stable action and target identities and Fire-specific acceptance, rejection, cancellation and mana expenditure.
- Reuse PROTOCOL-0016 projectile spawn/terminal identities, frozen trajectory evidence, terminal reasons, codec bounds and malformed-input conventions instead of defining a parallel projectile layout.
- Carry 700-unit requested/effective damage and target-defeat evidence only for a projectile Hit; canonical monster state remains on snapshots/tombstones.
- Keep gameplay protocol version 1. This development-only Fire work replaces its layouts in place and retains no legacy compatibility path; a future protocol-version change requires a separate owner-approved real compatibility boundary.
- Reuse PROTOCOL-0014 mana facts without defining mana behavior.
- Do not implement server routing, client controls, permanent HUD, generic ability/projectile protocols or Arrow Rain.