---
id: CLIENT-0012
title: Send combat and skill intent from connected controls
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0009
- PROTOCOL-0004
- SIM-0004
- SIM-0007
createdAt: 2026-08-02T07:52:11.1885940Z
modifiedAt: 2026-08-02T07:52:35.0503040Z
---

Map connected-player input and bounded target selection to the approved basic-attack and geometric-skill protocol commands. Cover target point/entity selection, range-facing inputs, command sequencing, local rejection feedback, and end-to-end authoritative responses. The client sends intent only; it never decides hits, victims, damage, death, cooldown success, or skill outcomes.