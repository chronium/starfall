---
id: SERVER-0014
title: Exchange Arrow Rain commands and outcomes
track: SERVER
priority: none
dependsOn:
- SERVER-0008
- PROTOCOL-0012
- SIM-0007
- SERVER-0016
createdAt: 2026-08-05T19:46:45.3982550Z
modifiedAt: 2026-08-06T06:43:25.7199980Z
---

Route focused Arrow Rain commands and outcomes through the established connected combat host and completed Mana integration.

Acceptance criteria:
- Bind each decoded command to its admitted player and validate the finite target point through World-owned state.
- Invoke SIM-0007 and publish authoritative timing, acceptance, rejection, cancellation, mana expenditure, ordered victims, damage and defeat facts.
- Extend SERVER-0008 without changing Basic Arrow or the monster snapshot contract.
- Preserve presentational falling arrows and fixed-tick server authority.
- Do not add projectile entities, client presentation, permanent HUD, Fire Arrow presentation, persistence or a generic ability/message framework.