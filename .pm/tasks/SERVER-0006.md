---
id: SERVER-0006
title: Host one generic technical player state
track: SERVER
milestone: M2
dependsOn:
- SERVER-0004
createdAt: 2026-08-03T07:29:06.8178370Z
modifiedAt: 2026-08-03T07:29:43.3225370Z
---

Establish the smallest concrete world-owned player-state bucket after the provisional graybox is loaded.

Acceptance criteria:
- Define a stable world-local entity identity and one generic technical authoritative player record.
- Own explicit creation, lookup and removal with deterministic ordering.
- Represent only the state required by later authoritative movement; do not depend on the dark-elf archer, class, combat kit, equipment or selected presentation assets.
- Avoid a generic entity/component framework, ECS, persistence, networking, gameplay session admission, monsters or combat.