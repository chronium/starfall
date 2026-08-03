---
id: SERVER-0007
title: Exchange bounded monster snapshots
track: SERVER
milestone: M2
dependsOn:
- SERVER-0005
- PROTOCOL-0005
- SIM-0010
- SIM-0011
createdAt: 2026-08-03T07:29:07.0749230Z
modifiedAt: 2026-08-03T07:29:43.3337840Z
---

Extend the connected world with bounded authoritative monster snapshots.

Acceptance criteria:
- Publish approved monster identity, transform, behavior/target, health, death, disengage and return facts through existing active world sessions.
- Preserve deterministic ordering and world/session isolation.
- Keep monster simulation in Starfall.Simulation and presentation in Starfall.Client.
- Do not add combat commands, asset presentation, persistence, a generic event bus or a second world host.