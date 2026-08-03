---
id: PROTOCOL-0003
title: Define connected walking commands and player snapshot facts
track: PROTOCOL
milestone: M2
dependsOn:
- SERVER-0006
- SIM-0008
createdAt: 2026-08-02T07:29:11.5694060Z
modifiedAt: 2026-08-03T07:30:50.5981580Z
---

Define the transport-neutral connected-walking contract only after concrete player state and authoritative movement exist.

Acceptance criteria:
- Define bounded ground-point movement intent plus stable world-local player/entity identity.
- Define fixed-tick player snapshots and correction facts carrying authoritative position, velocity, orientation and collision dimensions as finite single-precision metre components.
- Preserve integer discrete resources, integer fixed ticks, stable snapshot sequencing and explicit ordering.
- Keep the Client adapter presentational and require real snapshots to drive the same path proven by the local fixture.
- Do not define monsters, combat actions, damage/death, progression, drops, equipment, transport framing, quantization, chat, persistence or a generic message/entity framework.