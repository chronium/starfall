---
id: SERVER-0005
title: Exchange walking commands and player snapshots
track: SERVER
milestone: M2
dependsOn:
- SERVER-0003
- SERVER-0006
- SIM-0008
- PROTOCOL-0004
createdAt: 2026-08-02T07:31:45.6180010Z
modifiedAt: 2026-08-03T07:30:50.6512830Z
---

Connect admitted gameplay sessions to the proven connected-walking protocol.

Acceptance criteria:
- Route validated ground-point movement intent into authoritative movement for the session's world-owned player.
- Publish bounded fixed-tick player snapshots and corrections with stable identity and ordering.
- Preserve session/world isolation and continued world operation during identity, chat and operations outages.
- Do not exchange monsters, combat, progression, drops or inventory/equipment yet.
- Do not add persistence, multiple worlds, chat or a generic hosting/message framework.