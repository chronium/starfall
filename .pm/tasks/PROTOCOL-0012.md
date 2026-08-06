---
id: PROTOCOL-0012
title: Add Arrow Rain facts and serialization
track: PROTOCOL
priority: none
dependsOn:
- SIM-0007
- PROTOCOL-0007
createdAt: 2026-08-05T19:46:44.8390510Z
modifiedAt: 2026-08-06T06:43:25.6214780Z
---

Extend the proven first connected-combat contract with focused Arrow Rain facts and deterministic serialization.

Acceptance criteria:
- Define a finite ground-target Arrow Rain command tied to the admitted actor, with non-zero command sequence, stable action identity, authoritative start/resolve ticks, acceptance, rejection and cancellation.
- Carry integer mana expenditure, bounded radius, deterministic ordered victims, 500-unit per-victim damage, effective damage and defeat facts from SIM-0007 without embedding simulation rules.
- Reuse the bounded envelope and malformed-input conventions established by PROTOCOL-0007.
- Preserve presentational falling arrows and authoritative fixed-tick victim resolution.
- Do not implement server routing, client targeting, projectile entities, effects, a generic ability protocol or Fire Arrow.