---
id: CLIENT-0016
title: Render the Draft 0 first-zone scene
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0009
- CONTENT-0006
- CONTENT-0012
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0018
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0019
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/ASSET-0007
createdAt: 2026-08-02T15:49:18.0890200Z
modifiedAt: 2026-08-02T16:32:42.2523220Z
---

Render the bounded Draft 0 scene from synchronized Starfall zone content: flat grass treatment, dirt-path material inputs, protected-town landmarks, three readable branch/camp spaces, and selected rocks, vegetation, and props. Preserve deterministic collision/navigation as separate authoritative content.

Use only exact selected and coordinator-staged assets through approved family-source contracts. Engine-specific vegetation/wind shaders are not assumed portable; use supported glTF materials or a deliberately simple shared material path. Canonical SHARED-0018, SHARED-0019, and ASSET-0007 dependencies gate the static renderer, deterministic cook/staging, and exact zone acquisition.

Do not create terrain, streaming, biome, vegetation/wind, general scene, final-art, NPC, crafting, commerce, storage, or interaction systems.