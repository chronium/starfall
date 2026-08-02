---
id: SERVER-0004
title: Load the first zone and host authoritative entity state
track: SERVER
milestone: M2
dependsOn:
- SERVER-0002
- CONTENT-0014
createdAt: 2026-08-02T07:29:12.8441180Z
modifiedAt: 2026-08-02T18:27:28.8702100Z
---

Load and host the deterministic Draft 0 zone in one authoritative world/channel.

Acceptance criteria:
- Load the approximately 200 x 200 metre zone, protected town, respawn anchor, landmark footprints, branch/camp areas, collision, navigation, and deterministic entity-placement inputs.
- Host player and monster world state as ordinary ground-plane entities with authoritative positions and radii.
- Keep town protection and gameplay rules in simulation rather than rendering or authoring data.
- Do not implement monsters, combat, progression, persistence, streaming, terrain systems, or additional zones.