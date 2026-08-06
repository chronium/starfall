---
id: SERVER-0012
title: Adopt the editor-authored Draft 0 authoritative map
track: SERVER
priority: none
dependsOn:
- EDITOR-0007
- SERVER-0004
createdAt: 2026-08-03T07:29:08.3531020Z
modifiedAt: 2026-08-06T06:44:47.2866100Z
---

Replace provisional graybox inputs with the focused Editor task's compiled authoritative Draft 0 layout.

Acceptance criteria:
- Load only bounded regions, collision/navigation, respawn, camp and spawn inputs intended for the authoritative world.
- Preserve stable identities, finite metre values and deterministic ordering.
- Keep presentation meshes, materials, textures, SDL/GPU and editor UI out of World and Simulation outputs.
- Do not add streaming, terrain, a general map format or additional zones.