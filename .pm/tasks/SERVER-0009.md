---
id: SERVER-0009
title: Publish progression facts
track: SERVER
milestone: M2
dependsOn:
- SERVER-0008
- PROTOCOL-0008
- GAME-0002
createdAt: 2026-08-03T07:29:07.5901340Z
modifiedAt: 2026-08-03T07:29:43.3601610Z
---

Publish authoritative Draft 0 experience and level facts through existing connected gameplay sessions.

Acceptance criteria:
- Publish bounded XP awards, current progression and level-up facts from GAME-0002 using the approved protocol extension.
- Preserve integer XP and deterministic ordering/corrections.
- Do not compute progression in the client, add persistence, final pacing or a generic event framework.