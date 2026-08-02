---
id: SERVER-0005
title: Exchange commands and snapshots for one world
track: SERVER
milestone: M2
dependsOn:
- SERVER-0003
- SERVER-0004
- PROTOCOL-0004
- SIM-0008
createdAt: 2026-08-02T07:31:45.6180010Z
modifiedAt: 2026-08-02T07:32:00.5762460Z
---

Connect admitted gameplay sessions to the vertical-slice protocol, route validated client commands into the authoritative world, and publish bounded snapshots and events for the first zone. Prove session isolation and continued world operation during identity, chat, and operations outages. Do not add persistence, multiple worlds, chat, or a generic hosting framework.