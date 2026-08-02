---
id: PROTOCOL-0003
title: Define gameplay commands, events, and snapshot contract
track: PROTOCOL
milestone: M0
dependsOn:
- PROTOCOL-0002
createdAt: 2026-08-02T07:29:11.5694060Z
modifiedAt: 2026-08-02T15:52:42.6417640Z
---

Define the authoritative Draft 0 gameplay command, event, and snapshot facts.

Acceptance criteria:
- Define entity-target intents for Basic Arrow and Fire Arrow and point-target intent for Arrow Rain.
- Carry stable action identity, actor, target or target point, action start/windup/resolve ticks, acceptance or rejection, authoritative victims, damage, resource expenditure, and outcomes.
- Represent health, mana, damage, and other authoritative resources as integers and time as fixed ticks.
- Include bounded monster state and player defeat, protected-town return, and respawn facts needed by clients.
- Basic Arrow and Fire Arrow create no authoritative spatial projectile entity or server-side travel/collision simulation.
- Arrow Rain resolves its authoritative victim set and damage at an explicit deterministic tick; its falling arrows are presentation.
- Keep transport, chat, persistence, and generic ability frameworks out of scope.