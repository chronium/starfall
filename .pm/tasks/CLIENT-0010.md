---
id: CLIENT-0010
title: Present Arrow Rain targeting and effects
track: CLIENT
priority: none
dependsOn:
- CLIENT-0007
- PROTOCOL-0012
- CLIENT-0028
createdAt: 2026-08-02T07:33:29.1280240Z
modifiedAt: 2026-08-06T06:43:25.9236490Z
---

Present the authoritative Draft 0 Arrow Rain action.

Acceptance criteria:
- Show bounded ground-targeting radius and valid/invalid target-point feedback from the connected control flow.
- Present action timing, client-only falling arrows, impacts, and victim feedback from authoritative resolve-tick and victim facts.
- Ensure visual arrows never decide victims, collision, damage, mana, or success.
- Keep trajectory, windup, resolve, and reconciliation presentation inputs explicit and tunable.
- Do not absorb Fire Arrow, build a generic effects framework, or add authoritative projectile simulation.