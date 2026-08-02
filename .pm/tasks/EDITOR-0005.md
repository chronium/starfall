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
modifiedAt: 2026-08-02T15:52:42.7883250Z
---

Run deterministic headless Draft 0 camp, combat, sustain, defeat, and return scenarios.

Acceptance criteria:
- Exercise the easy broad/open, mixed elongated or divided, and hard tight/bowl camp inputs.
- Cover Basic Arrow, Fire Arrow, Arrow Rain, integer damage breakpoints, mana and cadence inputs, deterministic victims, and fixed-tick resolve timing.
- Cover bounded monster awareness, pursuit, attacks, disengage/return, protected-town exclusion, player damage, defeat, respawn, and safe-town return.
- Keep exact ranges, costs, regeneration, monster damage/cadence, interruption, respawn delay, restored resources, and pacing configurable inputs.
- Reuse authoritative rules and content; do not create simulator-only gameplay or visual systems.