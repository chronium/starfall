---
id: SERVER-0008
title: Exchange first connected combat commands and outcomes
track: SERVER
milestone: M2
priority: medium
dependsOn:
- SERVER-0005
- SERVER-0007
- PROTOCOL-0007
- SIM-0004
- SIM-0011
createdAt: 2026-08-03T07:29:07.3311690Z
modifiedAt: 2026-08-05T19:47:21.3470960Z
---

Route the first connected Basic Arrow command and publish authoritative combat and player-life outcomes through admitted World sessions.

Acceptance criteria:
- Decode Basic Arrow commands, bind the actor to the admitted gameplay session and validate the target against World-owned monster state.
- Route valid commands into the proven SIM-0004 behavior and publish authoritative timing, rejection, cancellation, 300-unit damage and target defeat facts.
- Publish bounded authoritative player-health, defeat and protected-town respawn facts from SIM-0011.
- Continue publishing monster health and defeat through the existing SERVER-0007 snapshot contract.
- Preserve presentational arrows and server-authoritative fixed-tick outcomes.
- Do not add Fire Arrow, Arrow Rain, projectile entities, persistence, chat or a generic messaging/ability framework.