---
id: CONTENT-0014
title: Define the provisional Draft 0 executable graybox
track: CONTENT
milestone: M2
dependsOn:
- CONTENT-0006
createdAt: 2026-08-02T18:26:57.8750190Z
modifiedAt: 2026-08-03T10:02:41.8396740Z
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

## Notes

- 2026-08-03 10:02 UTC - Implemented the provisional executable Draft 0 graybox in `Starfall.Content`.

  - Added `Draft0GrayboxCatalog.FirstPlayable` with the approved 200 m zone/190 m walkable inset, `town_safe`, exit and junction, ordered 25/45/70 m routes, three camp footprints and entry anchors, seven collidable diagnostic proxies, and ten neutral sample spawns.
  - Added focused immutable BCL/`System.Numerics` content types for town, route, camp, branch, proxy and spawn inputs. Validation rejects non-finite or invalid values, out-of-owner geometry, ordinal identity collisions, obstructed anchors/spawns, mismatched durable branch order/geometry/length, and malformed circular camps. Thick route presentation uses swept segments with round endpoint caps and remains non-authoritative.
  - Updated `content/draft-0-zone-contract` with exact identities, coordinates, ordering, presentation semantics and the disposable graybox boundary.
  - Added 9 focused executable-graybox tests; `Starfall.Content.Tests` now passes 14 tests.
  - Validation: `dotnet restore Starfall.slnx`; `dotnet build Starfall.slnx -m:1 --no-restore` (0 warnings/errors); `dotnet test Starfall.slnx -m:1 --no-restore --no-build` (23 architecture, 14 content, 24 protocol tests); `dotnet format Starfall.slnx --no-restore --verify-no-changes` (passed, with existing third-party SDL3-CS naming diagnostics); and `pm doctor` passed.
  - No Box3D, SDL, rendering, editor, serialization, movement, monster, asset or general map/scene-format work was added.