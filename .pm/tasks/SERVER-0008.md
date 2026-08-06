---
id: SERVER-0008
title: Exchange first connected combat commands and outcomes
track: SERVER
milestone: M5
priority: high
dependsOn:
- SERVER-0005
- SERVER-0007
- PROTOCOL-0007
- PROTOCOL-0015
- SIM-0004
- SIM-0011
createdAt: 2026-08-03T07:29:07.3311690Z
modifiedAt: 2026-08-06T10:20:19.8546520Z
---

Route connected Basic Arrow requests and publish authoritative Basic outcomes through admitted World sessions.

Acceptance criteria:
- Decode Basic Arrow requests, derive the actor from the admitted gameplay session and validate the target against World-owned monster state.
- Route valid requests into the proven SIM-0004 behavior and publish authoritative timing, rejection, cancellation, 300-unit damage and monster-defeat facts.
- Continue publishing monster health and defeat through the existing SERVER-0007 snapshot contract.
- Consume SIM-0011 only for defeated-player and protected-town action rejection.
- Do not publish player health, defeat, restoration or respawn as part of this task.
- Preserve presentational arrows and server-authoritative fixed-tick outcomes.
- Do not add Fire Arrow, Arrow Rain, projectile entities, persistence, chat or a generic messaging or ability framework.