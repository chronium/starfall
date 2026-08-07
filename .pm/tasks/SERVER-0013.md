---
id: SERVER-0013
title: Exchange Fire Arrow commands and outcomes
track: SERVER
priority: none
dependsOn:
- SERVER-0017
- PROTOCOL-0011
- SIM-0009
- SERVER-0016
createdAt: 2026-08-05T19:46:45.1151160Z
modifiedAt: 2026-08-07T08:32:06.3099470Z
---

Route focused Fire Arrow commands and outcomes through the proven authoritative straight-projectile World path and completed Mana integration.

Acceptance criteria:
- Bind each decoded command to its admitted player and validate the selected target through World-owned state.
- Invoke SIM-0009 and completed Mana behavior, then publish Fire-specific acceptance, rejection, cancellation, mana expenditure and the reused projectile spawn/terminal facts.
- Reuse SERVER-0017 projectile allocation, fixed-step lifecycle, first-contact collision ordering and connected exchange instead of creating a Fire-specific projectile runtime.
- Preserve Basic Arrow behavior and the monster snapshot/tombstone contract.
- Do not add client presentation, permanent HUD, Arrow Rain, persistence or generic ability/message/projectile infrastructure.