---
id: SERVER-0010
title: Exchange physical drop commands and facts
track: SERVER
milestone: M2
dependsOn:
- SERVER-0008
- PROTOCOL-0009
- GAME-0004
createdAt: 2026-08-03T07:29:07.8496800Z
modifiedAt: 2026-08-03T07:29:43.3693760Z
---

Route physical-drop collection intent and publish authoritative drop state and outcomes.

Acceptance criteria:
- Exchange bounded drop identity, placement, ownership/reservation, expiry, collection success and rejection using the approved protocol extension.
- Preserve collect-once authority and deterministic ordering.
- Do not add item presentation assets, inventory/equipment behavior, persistence, trade, economy or a generic interaction framework.