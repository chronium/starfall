---
id: CLIENT-0016
title: Render the editor-authored Draft 0 scene
track: CLIENT
priority: none
dependsOn:
- CLIENT-0020
- EDITOR-0007
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0018
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0019
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/ASSET-0007
createdAt: 2026-08-02T15:49:18.0890200Z
modifiedAt: 2026-08-06T06:44:47.3878430Z
---

Render the proper Draft 0 scene from the focused Editor task's compiled client-presentation output.

Acceptance criteria:
- Consume only bounded client placement data and exact coordinator-staged assets.
- Render approved grass/path treatment, protected-town landmarks, readable branch/camp spaces and selected rocks, vegetation and props.
- Preserve authoritative collision/navigation as a separate compiled output consumed by SERVER-0012.
- Reuse the local graybox camera/presentation path without requiring networking.
- Use supported materials or a deliberately simple shared material path; do not assume engine-specific vegetation/wind shaders.
- Do not create terrain, streaming, biome, general scene, final-art, NPC, crafting, commerce, storage or interaction systems.