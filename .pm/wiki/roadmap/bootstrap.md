---
title: Bootstrap Roadmap
createdAt: 2026-08-01T05:48:09.1150770Z
modifiedAt: 2026-08-02T07:34:10.3924290Z
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

`BUILD-0002` is the only dependency-ready feature task after this grooming pass.

## M1 — Shared character presentation

`CLIENT-0006` integrates the canonical parent presentation foundation only after `BUILD-0003`. It must not use parent-relative source references. Before activation, it requires a canonical dependency on a coordinator-owned task that establishes independent acquisition of shared binaries and cooked content.

Presentation is split by independently verifiable outcome:

- `CONTENT-0004`: modular armour and truthful body-region hiding;
- `CONTENT-0009`: starter weapon/socket attachment mappings;
- `CONTENT-0010`: bounded material and palette variants;
- `CLIENT-0007`: locomotion, basic attack, damage, and death presentation;
- `CLIENT-0010`: the geometric skill and bounded gameplay effects;
- `CLIENT-0011`: equipped weapon alignment, aim offsets, and off-hand IK.

Each task consumes authoritative Starfall state and the smallest relevant canonical coordinator contract. Animation, rendering, effects, and IK remain presentation-only.

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

Balance Lab work is split into the deterministic harness `EDITOR-0004`, camp/combat scenarios `EDITOR-0005`, and progression/drop/equipment metrics `EDITOR-0006`. Only the resulting evidence gates deferred topology decision `ARCH-0005`.

## M3 — Deferred transformations and companions

`CONTENT-0005` and `SIM-0005` remain future contract tasks. `CLIENT-0008` is a deliberately deferred presentation umbrella and must be split into wings, mounts, and companions before implementation. M3 has no milestone priority and is outside the vertical-slice critical path.

## Remaining cross-project action

Starfall cannot own how coordinator shared modules are distributed. The coordinator must create and complete a focused shared-acquisition task, after which `CLIENT-0006` must gain its canonical `pm://project/.../task/...` dependency. `SF-0004` records this contract gap but does not fabricate a parent task or mutate the coordinator repository.
