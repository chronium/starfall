---
id: CLIENT-0013
title: Present and collect physical world drops
track: CLIENT
priority: none
dependsOn:
- CLIENT-0012
- GAME-0004
- PROTOCOL-0009
- SERVER-0010
- CLIENT-0014
createdAt: 2026-08-02T07:52:11.4311620Z
modifiedAt: 2026-08-06T07:11:23.1856270Z
---

Present authoritative physical-drop state and bounded pickup interaction after the completed Inventory Client proof exists.

Acceptance criteria:
- Show authoritative drop placement, ownership or reservation, expiry and collection state in the connected first zone.
- Provide the exact placeholder selection/pickup gesture approved during Plan mode, send collection intent and reconcile success or rejection.
- The terminal native proof must show the collected item appearing through the completed Inventory surface.
- Never grant items locally.
- Do not implement Equipment, persistence, trading, economy or a general interaction framework.