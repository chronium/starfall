---
id: SERVER-0011
title: Exchange authoritative inventory state
track: SERVER
priority: none
dependsOn:
- SERVER-0005
- PROTOCOL-0010
- GAME-0003
createdAt: 2026-08-03T07:29:08.0982340Z
modifiedAt: 2026-08-06T06:44:37.3740850Z
---

Route bounded Inventory commands and publish authoritative inventory state and corrections through admitted gameplay sessions.

Acceptance criteria:
- Bind inventory commands to the admitted player and exchange item/slot identity, move/swap intent, full/invalid rejection and correction through PROTOCOL-0010.
- Preserve server authority and deterministic ordering.
- Keep Inventory behavior independent of the development console; a later native injection proof may consume Development Instrumentation without making it a domain prerequisite.
- Do not exchange equipment or physical-drop state.
- Do not implement presentation, persistence, trade, crafting, economy or a generic item protocol.