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
modifiedAt: 2026-08-03T07:29:43.1892330Z
---

Render a generated local Draft 0 walking graybox before networking or selected environment assets.

Acceptance criteria:
- Render generated ground, protected-town, route, camp, outer-boundary, proxy-geometry and collision/debug visuals from CONTENT-0014.
- Reuse CLIENT-0005 isometric camera and deterministic ground picking plus the existing CLIENT-0006 native presentation foundation.
- Require no world connection, selected environment asset, static cook, general scene format, terrain system or gameplay authority.
- Keep generated primitives sufficient; optional temporary assets require separate approved provenance and may not become an acceptance gate.