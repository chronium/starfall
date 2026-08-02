---
id: CONTENT-0008
title: Define Draft 0 progression, starter equipment, and drops
track: CONTENT
milestone: M2
dependsOn:
- CONTENT-0003
- CONTENT-0007
createdAt: 2026-08-02T07:29:12.3306580Z
modifiedAt: 2026-08-02T15:52:42.6032870Z
---

Define provisional progression, equipment, and reward inputs for the Draft 0 slice.

Acceptance criteria:
- Define levels 1 through 20, with level 2 requiring 40 XP and each later requirement computed by nearest-integer half-up arithmetic: next = (previous * 115 + 50) / 100.
- Record the accepted level 2-20 sequence: 40, 46, 53, 61, 70, 81, 93, 107, 123, 141, 162, 186, 214, 246, 283, 325, 374, 430, 495.
- Define deterministic authoritative-seed XP awards of 1-3 for starter_flyer_light and 2-8 for starter_flyer_heavy.
- Define the initial non-equipment underlayer, equipped wooden bow, unlimited arrows with no ammunition item, and one first Ranger/leather armour family earned visibly over time.
- Keep exact drop tables, modifiers, level gains, pacing, health/mana restoration, and respawn timing configurable Balance Lab inputs.
- Do not implement runtime rules, presentation, economy, persistence, or final balance.