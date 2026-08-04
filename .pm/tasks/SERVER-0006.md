---
id: SERVER-0006
title: Host one generic technical player state
track: SERVER
milestone: M2
dependsOn:
- SERVER-0004
createdAt: 2026-08-03T07:29:06.8178370Z
modifiedAt: 2026-08-04T11:58:42.6764280Z
---

Establish the smallest concrete world-owned player-state bucket after the provisional graybox is loaded.

Acceptance criteria:
- Define a stable world-local entity identity and one generic technical authoritative player record.
- Own explicit creation, lookup and removal with deterministic ordering.
- Represent only the state required by later authoritative movement; do not depend on the dark-elf archer, class, combat kit, equipment or selected presentation assets.
- Avoid a generic entity/component framework, ECS, persistence, networking, gameplay session admission, monsters or combat.

## Notes

- 2026-08-04 11:58 UTC - Implemented the bounded world-owned technical player state. `WorldEntityId` is a positive world-instance-local `ulong`; each runtime allocates from 1 with checked monotonic arithmetic, never reuses removed IDs, and fails explicitly after issuing `ulong.MaxValue`. `WorldPlayerState` is immutable and contains only identity, finite ground position, finite planar velocity, and normalized planar facing. Creation is Running-only, removal is allowed while Running or Draining, ordered snapshots are defensively copied under the runtime lock, draining retains players, and stopping clears them without resetting allocation. The standalone host creates player 1 at `town_safe`'s `(100,0,25)` respawn anchor with zero velocity and `+Z` facing; it remains independent of admission sessions and selected gameplay/content.

  Validation: scoped formatting was applied to the task-owned C# changes and `git diff --check` passed. Debug and Release solution builds passed with zero warnings/errors. Both configurations passed 152 tests: 25 Architecture, 42 Client, 14 Content, 24 Protocol, and 47 World. A finite 60-tick World run reported `technicalPlayer=1 players=1` at READY, retained `players=1` at DRAINING, and cleared `players=0` at STOPPED. The World output contains only Starfall.Content, Starfall.Protocol, Starfall.Simulation, and Starfall.World assemblies plus runtime metadata; no client or graphics artifacts. `pm doctor` passed and linked-family reread reported all three projects available/readable/write-trusted with zero warnings. No visual or owner-feel validation was required.