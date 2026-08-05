---
id: CLIENT-0019
title: Present combat resources and targeting feedback
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0012
- CLIENT-0027
- CLIENT-0028
- CONTENT-0003
- PROTOCOL-0007
- PROTOCOL-0011
- PROTOCOL-0012
- SERVER-0008
- SIM-0011
createdAt: 2026-08-02T15:49:18.8424300Z
modifiedAt: 2026-08-05T19:47:22.3470000Z
---

Present authoritative integer player health/mana, selected-target state, skill readiness, Arrow Rain targeting/cancellation, action rejection, damage, defeat, protected-town respawn, and correction feedback for the connected Draft 0 client.

One displayed health or mana point equals 100 authoritative internal units. Consume protocol state/events only; never predict authoritative costs, hits, victims, death, or respawn. Keep experience/level presentation in CLIENT-0015. Do not create a general UI, notification, persistence, or account framework.