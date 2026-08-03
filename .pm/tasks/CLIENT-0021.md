---
id: CLIENT-0021
title: Present a technical player through the local world adapter
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0020
createdAt: 2026-08-03T07:29:06.0319190Z
modifiedAt: 2026-08-03T07:29:43.2765760Z
---

Prove the reusable Client snapshot/fact-to-presentation boundary in the local walking graybox.

Acceptance criteria:
- Display the already-proven technical humanoid without claiming it is the selected dark-elf archer.
- Keep left-click output as movement intent only.
- Drive one client-owned world-presentation adapter with a deterministic authoritative-style player movement fixture.
- Present position, facing and locomotion without referencing Starfall.World or Starfall.Simulation and without deciding authoritative movement.
- Make CLIENT-0009 translate real protocol snapshots into this same adapter rather than implementing a second movement-presentation path.
- Do not select/cook the final archer, connect networking, implement prediction authority, combat, equipment or a general scene/entity framework.