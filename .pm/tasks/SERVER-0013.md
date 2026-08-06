---
id: SERVER-0013
title: Exchange Fire Arrow commands and outcomes
track: SERVER
priority: none
dependsOn:
- SERVER-0008
- PROTOCOL-0011
- SIM-0009
- SERVER-0016
createdAt: 2026-08-05T19:46:45.1151160Z
modifiedAt: 2026-08-06T06:43:25.3101260Z
---

Route focused Fire Arrow commands and outcomes through the proven Basic World path and completed Mana integration.

Acceptance criteria:
- Bind each decoded command to its admitted player and validate the selected target through World-owned state.
- Invoke SIM-0009 and completed Mana behavior, then publish authoritative timing, acceptance, rejection, cancellation, mana expenditure, damage and defeat facts.
- Extend SERVER-0008 without changing Basic Arrow or the monster snapshot contract.
- Preserve presentational arrows and fixed-tick server authority.
- Do not add projectile entities, client presentation, permanent HUD, Arrow Rain, persistence or a generic ability/message framework.