---
id: SERVER-0013
title: Exchange Fire Arrow commands and outcomes
track: SERVER
milestone: M2
dependsOn:
- SERVER-0008
- PROTOCOL-0011
- SIM-0009
createdAt: 2026-08-05T19:46:45.1151160Z
modifiedAt: 2026-08-05T19:47:21.7418400Z
---

Route focused Fire Arrow commands and publish authoritative outcomes through the connected World session.

Acceptance criteria:
- Bind each decoded command to its admitted player and validate the selected target through the existing World-owned entity state.
- Invoke the proven SIM-0009 behavior and publish authoritative timing, acceptance, rejection, cancellation, integer mana expenditure, damage and defeat facts.
- Extend the SERVER-0008 combat exchange without changing Basic Arrow behavior or the existing monster snapshot contract.
- Preserve presentational arrows and fixed-tick server authority.
- Do not add projectile entities, client presentation, Arrow Rain, persistence or a generic ability/message framework.