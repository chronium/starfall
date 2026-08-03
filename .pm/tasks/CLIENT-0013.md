---
id: CLIENT-0013
title: Present and collect physical world drops
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0012
- GAME-0004
- PROTOCOL-0009
- SERVER-0010
createdAt: 2026-08-02T07:52:11.4311620Z
modifiedAt: 2026-08-03T07:30:50.7679960Z
---

Present authoritative physical drop state in the connected first zone, expose bounded selection and pickup interaction, send collection intent, and reconcile ownership, reservation, expiry, success, and rejection events. Do not grant items locally, implement inventory/equipment UI, persistence, trading, economy, or general interaction frameworks.