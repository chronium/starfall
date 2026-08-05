---
id: SERVER-0014
title: Exchange Arrow Rain commands and outcomes
track: SERVER
milestone: M2
dependsOn:
- SERVER-0008
- PROTOCOL-0012
- SIM-0007
createdAt: 2026-08-05T19:46:45.3982550Z
modifiedAt: 2026-08-05T19:47:21.8483490Z
---

Route focused Arrow Rain commands and publish authoritative outcomes through the connected World session.

Acceptance criteria:
- Bind each decoded command to its admitted player and validate the finite target point through existing World-owned state.
- Invoke the proven SIM-0007 behavior and publish authoritative timing, acceptance, rejection, cancellation, integer mana expenditure, ordered victims, damage and defeat facts.
- Extend the SERVER-0008 combat exchange without changing Basic Arrow behavior or the existing monster snapshot contract.
- Preserve presentational falling arrows and fixed-tick server authority.
- Do not add projectile entities, client presentation, Fire Arrow, persistence or a generic ability/message framework.