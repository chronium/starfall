---
id: SERVER-0011
title: Exchange inventory and equipment commands and facts
track: SERVER
milestone: M2
dependsOn:
- SERVER-0010
- PROTOCOL-0010
- GAME-0003
- GAME-0005
createdAt: 2026-08-03T07:29:08.0982340Z
modifiedAt: 2026-08-03T07:29:43.3820700Z
---

Route bounded inventory/equipment commands and publish authoritative state and corrections.

Acceptance criteria:
- Exchange inventory, equipped-slot, select/equip/unequip intent, compatibility, replacement, stat-change and rejection facts using the approved protocol extension.
- Preserve server authority and deterministic ordering.
- Do not implement modular armour rendering, persistence, trade, crafting, economy or a generic item protocol.