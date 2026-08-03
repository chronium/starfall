---
id: CLIENT-0020
title: Render the local walking graybox
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0005
- CLIENT-0006
- CONTENT-0014
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0018
createdAt: 2026-08-03T07:29:05.7687670Z
modifiedAt: 2026-08-03T15:20:08.5336460Z
---

Render a generated local Draft 0 walking graybox before networking or selected environment assets.

Acceptance criteria:
- Render generated ground, protected-town, route, camp, outer-boundary, proxy-geometry and collision/debug visuals from CONTENT-0014.
- Reuse CLIENT-0005 isometric camera and deterministic ground picking plus the existing CLIENT-0006 native presentation foundation.
- Require no world connection, selected environment asset, static cook, general scene format, terrain system or gameplay authority.
- Keep generated primitives sufficient; optional temporary assets require separate approved provenance and may not become an acceptance gate.

## Notes

- 2026-08-03 15:20 UTC - Completed the generated Draft 0 local graybox presentation through the existing Starfall.Client/shared-source rendering boundary.

  Implementation evidence:
  - Built one deterministic shared mesh with 36 sections, 870 vertices and 1,554 indices in the approved section order.
  - Preserved exact Content footprints and identities while applying presentation-only Y layers: ground 0, town/camps 0.01, routes 0.02 and markers from 0.03.
  - Added exact boundary/proxy geometry, deterministic 16-wedge round route caps/joins, camp footprints, anchors and sample spawns.
  - Added F1-F7 direct camera presets plus Tab cycling; repeat events are ignored and number keys remain reserved.
  - Kept the technical humanoid at (100,0,100) only as the CLIENT-0005 framing fixture; no gameplay movement or authority was added.
  - Owner native validation confirmed layout, all seven views, controls and picking. Initial 0.1-metre near planes produced visible z-fighting; reviewed frusta of 1-300 metres for F1/local views and 100-800 metres for F2 removed it while preserving the approved 0.01/0.02-metre presentation layers.
  - The first OS-level contact-sheet attempt was explicitly rejected and is not preserved. Follow-up tasks SHARED-0022 and CLIENT-0024 now own deterministic in-render capture and curated evidence.

  Validation:
  - Debug and Release solution builds completed with 0 warnings and 0 errors.
  - Debug and Release solution tests each passed 85/85 (Architecture 23, Client 24, Content 14, Protocol 24).
  - dotnet format --verify-no-changes passed for owned source; it reported only the existing pinned SDL3-CS IDE1006 warnings.
  - Headless World, Simulation and BalanceLab outputs contain only their approved Starfall assemblies and no SDL, GPU, shader, image or presentation artifacts.
  - PM validation and family reads remained warning-free; Royale was unchanged.