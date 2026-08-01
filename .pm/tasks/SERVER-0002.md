---
id: SERVER-0002
title: Host one world channel and one small zone
track: SERVER
milestone: M2
dependsOn:
- PROTOCOL-0002
- CONTENT-0003
createdAt: 2026-08-01T05:46:47.9049220Z
modifiedAt: 2026-08-01T06:49:24.2847870Z
---

Run one server-authoritative world/channel containing the first small zone, connections, authoritative entity state, fixed simulation, and headless lifecycle.

## Availability requirements

- The world owns active gameplay sessions after consuming the approved admission handoff.
- Active sessions do not synchronously depend on identity/lobby, chat, or operations availability.
- Model the world/channel as an independent lifecycle and state owner so unrelated worlds can continue when one fails, even though the first slice hosts only one.
- Keep combat, characters, inventory, equipment, progression, drops, monsters, camps, zones, and world entities under world authority.
- Do not silently choose persistence-outage semantics; use the documented deferred contract.
- Do not extract logical boundaries into separate deployables without evidence and a later approved topology decision.