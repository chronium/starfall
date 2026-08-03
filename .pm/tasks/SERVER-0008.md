---
id: SERVER-0008
title: Exchange combat commands and outcomes
track: SERVER
milestone: M2
dependsOn:
- SERVER-0005
- SERVER-0007
- PROTOCOL-0007
- SIM-0004
- SIM-0009
- SIM-0007
- SIM-0011
createdAt: 2026-08-03T07:29:07.3311690Z
modifiedAt: 2026-08-03T07:29:43.3453980Z
---

Route Draft 0 combat intent and publish authoritative combat outcomes through connected world sessions.

Acceptance criteria:
- Consume the approved combat serialization for Basic Arrow, Fire Arrow and Arrow Rain.
- Route validated commands into the proven simulation behaviors and publish action timing, rejection, victims, integer resource/damage, defeat and respawn facts.
- Require the exact monster server-exchange prerequisite; never depend on a Client task.
- Preserve presentational arrows and server-authoritative fixed-tick outcomes.
- Do not add projectile entities, persistence, chat or a generic messaging/ability framework.