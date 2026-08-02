---
id: CLIENT-0015
title: Present experience and level-up feedback
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0012
- GAME-0002
- PROTOCOL-0004
createdAt: 2026-08-02T07:52:11.9212650Z
modifiedAt: 2026-08-02T15:52:42.7538940Z
---

Present deterministic Draft 0 experience and level progression from authoritative facts.

Acceptance criteria:
- Display current level, accumulated progress, the accepted level 2-20 XP requirements, awards, level-up feedback, and authoritative corrections.
- Preserve integer XP and the nearest-integer half-up requirement sequence.
- Never award XP or level locally.
- Keep health, mana, targeting, defeat, and respawn feedback in CLIENT-0019.
- Do not add final HUD art, economy, persistence, or unrelated progression systems.