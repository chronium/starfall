---
id: PROTOCOL-0008
title: Add progression facts and serialization
track: PROTOCOL
priority: none
dependsOn:
- GAME-0002
- PROTOCOL-0007
- PROTOCOL-0015
createdAt: 2026-08-03T07:29:09.3853040Z
modifiedAt: 2026-08-06T10:20:19.9606420Z
---

Add the focused one-way Draft 0 progression extension to the connected protocol.

Acceptance criteria:
- Define and deterministically encode XP awards, current progress, level and level-up facts.
- Preserve integer XP, the accepted level sequence, stable identity, fixed ticks and bounded values.
- Combine facts and serialization because this is a small one-way extension with no client command lifecycle.
- Do not implement progression rules, persistence, UI or final pacing.