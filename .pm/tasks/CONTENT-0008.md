---
id: CONTENT-0008
title: Define Draft 0 XP curve and reward inputs
track: CONTENT
priority: none
dependsOn:
- CONTENT-0007
createdAt: 2026-08-02T07:29:12.3306580Z
modifiedAt: 2026-08-06T06:43:44.8675170Z
---

Define provisional Draft 0 progression inputs without owning equipment, bows, Ranger presentation or physical-drop tables.

Acceptance criteria:
- Define levels 1 through 20, with level 2 requiring 40 XP and deterministic integer growth.
- Before implementation, the owner must explicitly freeze the 1.15 rounding rule. The persisted nearest-integer half-up sequence remains Draft 0 evidence; the proposed ceiling alternative is nextRequirement = checked((previousRequirement * 115 + 99) / 100). Do not silently change either rule or sequence.
- Define deterministic authoritative-seed XP awards of 1-3 for starter_flyer_light and 2-8 for starter_flyer_heavy.
- Keep exact pacing and level rewards configurable Balance Lab inputs.
- Do not define starter equipment, Ranger armour, bow ownership, inventory, item identities, drop tables, runtime rules, presentation, economy, persistence or final balance.