---
title: Bootstrap Roadmap
createdAt: 2026-08-01T05:48:09.1150770Z
modifiedAt: 2026-08-02T12:33:17.3406530Z
---

## Execution standard

Starfall roadmap entries are executable tasks, not milestone-sized promises. Each task should own one primary behavior or contract, one focused commit, and one independently meaningful validation story. Broad deferred placeholders must be split again before activation when their implementation contract becomes concrete.

## M0 — Repository foundation

`ARCH-0004` is complete. The executable foundation path is:

```text
BUILD-0002  Establish repository, solution, project boundaries, and dependency tests
  -> BUILD-0003  Add runnable client/world shells and local launch workflow
```

After `BUILD-0002`, `PROTOCOL-0002` defines signed lobby-to-world admission and `EDITOR-0003` establishes editor/Balance Lab boundaries. `PROTOCOL-0003` separately defines gameplay commands, authoritative events, and snapshots.

`BUILD-0002` established the standalone library foundation. `SF-0006` subsequently adopts the coordinator-family source-consumption policy without adding shared references or changing runtime behavior.

## M1 — Shared character foundation

`CLIENT-0006` is the sole M1 implementation task. It integrates the canonical parent presentation foundation after `BUILD-0003` and completed coordinator task `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0016` established the approved source-consumption and generated client cook/copy workflow. Its bounded native preview proves the cooked Quaternius humanoid and `Idle_Loop`; gameplay-driven presentation remains M2 work.

M1 uses only the central `$(ChronoFallFamilyRoot)` property for approved coordinator projects. It must not use literal parent traversal, absolute paths, arbitrary external property roots, coordinator imports, or Royale references, and it must preserve Starfall-owned gameplay, protocol, content, build/release decisions, and commits. Gameplay-specific presentation is part of the M2 playable-zone phase, keeping milestones chronological rather than treating them as unordered capability buckets.

## M2 — First playable zone

Content is split into independently reviewable contracts:

- `CONTENT-0003`: first playable class and starter skills;
- `CONTENT-0006`: first small zone;
- `CONTENT-0007`: starter monsters and camp placements;
- `CONTENT-0008`: starter items, progression, and drop tables.

The connected world spine is:

```text
BUILD-0003 -> SERVER-0002  headless fixed-step world lifecycle
PROTOCOL-0002 -> PROTOCOL-0003 -> PROTOCOL-0004  deterministic serialization
CONTENT-0006 + SERVER-0002 -> SERVER-0004  first-zone entity state
SERVER-0004 + PROTOCOL-0004 -> SIM-0008  authoritative player movement
SERVER-0002 + PROTOCOL-0004 -> SERVER-0003  world-owned admitted sessions
SERVER-0003 + SERVER-0004 + SIM-0008 -> SERVER-0005  command/snapshot exchange
CLIENT-0005 + CLIENT-0006 + SERVER-0005 -> CLIENT-0009  synchronized client
```

Connected-player interaction is explicit:

```text
CLIENT-0009 + SIM-0004 + SIM-0007 -> CLIENT-0012  combat/skill intent and targeting
CLIENT-0012 + GAME-0004 -> CLIENT-0013  physical-drop presentation and collection
CLIENT-0013 + GAME-0003 + GAME-0005 -> CLIENT-0014  inventory/equipment interaction
CLIENT-0012 + GAME-0002 -> CLIENT-0015  experience and level-up feedback
```

Monster and combat work is split into:

```text
CONTENT-0007 -> SIM-0003  camp and replenishment policy
SIM-0003 + SERVER-0004 -> SIM-0006  authoritative monster entities
SIM-0006 + SIM-0008 + PROTOCOL-0004 -> SIM-0004  basic attack, damage, death
SIM-0004 + CONTENT-0003 -> SIM-0007  one geometric area skill
```

Authoritative progression is four separate outcomes:

- `GAME-0002`: experience and bounded character progression;
- `GAME-0003`: item identity, ownership, and inventory;
- `GAME-0004`: physical world drops, reservation, and collection;
- `GAME-0005`: equipping and authoritative item effects.

Gameplay-specific character presentation also belongs to M2:

- `CONTENT-0004`: modular armour and truthful body-region hiding;
- `CONTENT-0009`: starter weapon/socket attachment mappings;
- `CONTENT-0010`: bounded material and palette variants;
- `CLIENT-0007`: locomotion, basic attack, damage, and death presentation;
- `CLIENT-0010`: the geometric skill and bounded gameplay effects;
- `CLIENT-0011`: equipped weapon alignment, aim offsets, and off-hand IK.

All client tasks consume authoritative Starfall state. Input sends intent only; animation, rendering, effects, UI, and IK never decide gameplay outcomes.

Balance Lab work is split into the deterministic harness `EDITOR-0004`, camp/combat scenarios `EDITOR-0005`, and progression/drop/equipment metrics `EDITOR-0006`. Only the resulting evidence gates deferred topology decision `ARCH-0005`.

## M3 — Deferred transformations and companions

`CONTENT-0005` and `SIM-0005` remain future contract tasks. `CLIENT-0008` is a deliberately deferred presentation umbrella and must be split into wings, mounts, and companions before implementation. M3 has no milestone priority and is outside the vertical-slice critical path.

## Shared-source gate

Starfall does not own coordinator shared source, SDL3-CS acquisition, or character-content cooking. Completed canonical task `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0016` owns the narrow source-consumption and generated client cook/copy contract, and `CLIENT-0006` persists and consumes that exact dependency.

`SF-0006` established only Starfall policy, the single family-root property, executable allowlist gates, documentation, and task grooming. `CLIENT-0006` is the separately approved implementation that adds the references and consumes generated client content. NuGet packages, feeds, content packages, and independent full-client distribution remain deferred until real child integrations or release/CI evidence make them valuable.