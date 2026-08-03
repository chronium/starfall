---
id: SERVER-0004
title: Load and own the provisional Draft 0 graybox
track: SERVER
milestone: M2
dependsOn:
- SERVER-0002
- CONTENT-0014
createdAt: 2026-08-02T07:29:12.8441180Z
modifiedAt: 2026-08-03T07:30:50.6418410Z
---

Load the provisional executable Draft 0 graybox into one authoritative world/channel.

Acceptance criteria:
- Load the bounded envelope, protected town, respawn anchor, proxy landmarks, route/camp regions, coarse collision/navigation and deterministic sample spawn inputs.
- Preserve finite single-precision metre components, stable identities and explicit ordering.
- Own the loaded world layout without yet creating player or monster state.
- Keep protection and gameplay behavior in Simulation rather than rendering or authoring data.
- Do not implement entities, movement, monsters, combat, progression, persistence, asset presentation, streaming, terrain systems or additional zones.