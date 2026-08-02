---
id: PROTOCOL-0003
title: Define gameplay commands, events, and snapshot contract
track: PROTOCOL
milestone: M0
dependsOn:
- PROTOCOL-0002
createdAt: 2026-08-02T07:29:11.5694060Z
modifiedAt: 2026-08-02T18:27:44.0802610Z
---

Define the authoritative Draft 0 gameplay command, event and snapshot facts.

Acceptance criteria:
- Define entity-target intents for Basic Arrow and Fire Arrow and point-target intent for Arrow Rain.
- Carry stable action identity, actor, target or target point, action start/windup/resolve ticks, acceptance or rejection, authoritative victims, damage, resource expenditure and outcomes.
- Represent health, mana, damage and other discrete authoritative resources as integers and time as fixed ticks.
- Represent authoritative position, velocity, orientation, collision dimensions, ranges and other spatial/physics facts as finite single-precision IEEE-754 components in metres matching Box3D-native precision.
- Preserve stable entity identities and explicit ordering for facts or query results whose source order is not guaranteed.
- Include bounded monster state and player defeat, protected-town return and respawn facts needed by clients.
- Basic Arrow and Fire Arrow create no authoritative spatial projectile entity or server-side travel/collision simulation.
- Arrow Rain resolves its authoritative victim set and damage at an explicit deterministic tick; its falling arrows are presentation.
- Keep transport, quantization, chat, persistence and generic ability frameworks out of scope.