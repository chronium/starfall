---
id: CONTENT-0014
title: Define the provisional Draft 0 executable graybox
track: CONTENT
milestone: M2
dependsOn:
- CONTENT-0006
createdAt: 2026-08-02T18:26:57.8750190Z
modifiedAt: 2026-08-03T07:30:50.5735200Z
---

Define the smallest deterministic executable graybox derived from the completed Draft 0 requirements.

Acceptance criteria:
- Preserve the explicit CONTENT-0006 durable-requirements prerequisite.
- Define finite single-precision metre coordinates and bounded regions for the outer envelope, protected town, respawn anchor, landmark proxy blocks, exit, junction, three route corridors and three camp areas.
- Define only simple proxy geometry, coarse collision/navigation inputs and deterministic sample spawn points in stable order.
- Use BCL-only immutable spatial values backed by System.Numerics; reject NaN, infinity, invalid dimensions, duplicates and out-of-zone values.
- Preserve one-to-one component conversion into later Box3D-native single-precision metre values without introducing a parallel integer-millimetre coordinate model.
- Treat the result as provisional executable evidence until the focused Editor task authors and compiles the proper scene.
- Do not select/place the complete asset-authored map, add Box3D/SDL/rendering/editor dependencies, implement movement/monsters, or create a general map, scene, terrain, navigation or streaming format.