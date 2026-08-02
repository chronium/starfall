---
id: CONTENT-0014
title: Author the exact Draft 0 graybox layout
track: CONTENT
milestone: M2
dependsOn:
- CONTENT-0006
createdAt: 2026-08-02T18:26:57.8750190Z
modifiedAt: 2026-08-02T18:27:28.8522810Z
---

Author one exact deterministic Draft 0 graybox layout using the completed regional zone contract.

Acceptance criteria:
- Place the protected town, configured respawn anchor, two or three landmark footprints, one exit and one junction inside the 200 x 200 metre envelope.
- Place short, medium and long route centre-lines, navigation corridors and camp areas that satisfy the approved approximate travel targets and open, divided and constrained hunting geometries.
- Define exact finite single-precision metre collision blockers, outer boundary treatment and deterministic entity-placement inputs in stable order.
- Validate every point, region and dimension against the zone bounds and reject non-finite, invalid, duplicate or out-of-zone data.
- Keep authoring values BCL-only; later simulation performs a one-to-one component conversion into Box3D-native types without changing unit or precision.
- Do not select or cook assets, add Box3D, render the scene, implement movement, monsters or simulation, or create a general scene, terrain, navigation or world format.