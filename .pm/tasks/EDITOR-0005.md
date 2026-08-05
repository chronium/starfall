---
id: EDITOR-0005
title: Simulate camp and combat scenarios headlessly
track: EDITOR
milestone: M2
dependsOn:
- EDITOR-0004
- SIM-0004
- SIM-0009
- SIM-0006
- SIM-0007
- SIM-0010
- SIM-0011
createdAt: 2026-08-02T07:29:14.3266250Z
modifiedAt: 2026-08-05T06:15:21.9682200Z
---

Run deterministic headless Draft 0 camp, combat, sustain, defeat and return scenarios.

Acceptance criteria:
- Exercise the easy broad/open, mixed elongated or divided, and hard tight/bowl camp inputs.
- Supply and compare explicit candidate values for player mana capacity, regeneration, skill costs, Basic/Fire/Rain range and cadence, windup/resolve ticks, interruption, Arrow Rain radius, monster damage/cadence, respawn delay, restored resources and pacing.
- Cover Basic Arrow, Fire Arrow, Arrow Rain, integer damage breakpoints, deterministic victims and fixed-tick resolve timing.
- Cover bounded monster awareness, pursuit, attacks, disengage/return, protected-town exclusion, player damage, defeat, respawn and safe-town return.
- Report deterministic evidence without silently promoting candidate values into selected product defaults; promotion requires later task-owned review.
- Reuse authoritative rules and content; do not create simulator-only gameplay or visual systems.