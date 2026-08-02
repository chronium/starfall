---
title: Bootstrap Roadmap
createdAt: 2026-08-01T05:48:09.1150770Z
modifiedAt: 2026-08-02T16:33:20.3593410Z
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

Draft 0 is the provisional M2 technical vertical slice. Its durable design input is `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/product/first-playable-zone-draft-0`; exact tuning remains Content and Balance Lab input rather than final balance.

Content and evidence-gated selection are split into focused contracts:

- `CONTENT-0003`: provisional dark-elf archer and Basic Arrow, Fire Arrow, and Arrow Rain identities;
- `CONTENT-0006`: exact Draft 0 zone, protected town, branches, camp geometry, collision, and navigation inputs;
- `CONTENT-0007`: neutral starter-flyer identities, health, and camp compositions;
- `CONTENT-0008`: levels 1-20, deterministic XP, starter bow/underlayer, first Ranger/leather family, and drop inputs;
- `CONTENT-0011`: exact archer, outfit, bow/arrow, and minimum compatible bow-clip selection;
- `CONTENT-0012`: exact zone presentation asset selection;
- `CONTENT-0013`: exact temporary monster presentation selection, including static/rigid/skeletal evidence.

Prospective packs are not dependencies. Selection tasks record exact pack-relative paths and may reject candidates. Coordinator acquisition consumes completed selections and stages only exact inputs. Generated client content remains ignored.

The reviewed canonical selection-to-acquisition gates are:

```text
CONTENT-0011 -> parent ASSET-0004  exact archer and bow-animation inputs
CONTENT-0011 -> parent ASSET-0005  exact Ranger equipment inputs
CONTENT-0011 -> parent ASSET-0006  exact bow and arrow inputs
CONTENT-0012 -> parent ASSET-0007  exact zone presentation inputs
CONTENT-0013 -> parent ASSET-0008  evidence-gated monster inputs
```

The parent tasks are identified by canonical `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/...` references in the owning Starfall consumers. Selection never depends on acquisition. Monster acquisition retains no speculative static or skeletal prerequisite until completed selection proves the smallest correct path.

The connected world spine remains:

```text
BUILD-0003 -> SERVER-0002  headless fixed-step world lifecycle
PROTOCOL-0002 -> PROTOCOL-0003 -> PROTOCOL-0004  deterministic Draft 0 facts and serialization
CONTENT-0006 + SERVER-0002 -> SERVER-0004  first-zone entity state
SERVER-0004 + PROTOCOL-0004 -> SIM-0008  authoritative click-to-move
SERVER-0002 + PROTOCOL-0004 -> SERVER-0003  world-owned admitted sessions
SERVER-0003 + SERVER-0004 + SIM-0008 -> SERVER-0005  command/snapshot exchange
CLIENT-0005 + CLIENT-0006 + SERVER-0005 -> CLIENT-0009  synchronized client
```

Combat, monster behavior, protection, and intent are explicit:

```text
CONTENT-0007 -> SIM-0003 -> SIM-0006  camps, replenishment, and world-owned monsters
SIM-0006 + SIM-0008 + PROTOCOL-0004 -> SIM-0004  Basic Arrow, integer damage, death
SIM-0004 + CONTENT-0003 -> SIM-0009  Fire Arrow
SIM-0004 + CONTENT-0003 -> SIM-0007  Arrow Rain
SIM-0004 + SIM-0006 + SIM-0008 + CONTENT-0007 -> SIM-0010  bounded monster behavior
SIM-0010 + SIM-0008 + CONTENT-0006 -> SIM-0011  protected town and respawn
CLIENT-0009 + SIM-0004 + SIM-0009 + SIM-0007 -> CLIENT-0012  connected action intent
```

Basic Arrow and Fire Arrow do not create authoritative projectile entities. All three actions resolve at explicit fixed ticks. The protocol carries enough action, target, victim, timing, resource, and outcome facts for client-only arrows and effects.

Zone and monster presentation remain product integration:

```text
CLIENT-0009 + CONTENT-0006 + CONTENT-0012 + parent SHARED-0018/0019 + ASSET-0007 -> CLIENT-0016  first-zone scene
CLIENT-0009 + SIM-0010 + CONTENT-0013 + parent ASSET-0008 -> CLIENT-0017  starter-flyer presentation
CLIENT-0007 + CLIENT-0011 + SIM-0009 + PROTOCOL-0004 -> CLIENT-0018  Basic/Fire arrows
CLIENT-0012 + CONTENT-0003 + PROTOCOL-0004 + SIM-0011 -> CLIENT-0019  resources/targeting
```

Gameplay-specific character presentation stays in M2:

- `CONTENT-0004`: first Ranger/leather family and body-region rules after parent `ASSET-0005`;
- `CONTENT-0009`: starter bow/arrow attachment definitions after parent `ASSET-0006` and narrow `SHARED-0020`;
- `CLIENT-0007`: locomotion, Basic Arrow, reactions, and death after parent `ASSET-0004`;
- `CLIENT-0010`: Arrow Rain targeting and effects;
- `CLIENT-0011`: equipped bow, aim, and IK;
- `CLIENT-0018`: client-only Basic/Fire projectile presentation.

`CONTENT-0010` material/palette variants are explicitly deferred outside Draft 0. Parent `SHARED-0020` owns the narrow socketed static bow proof. Parent `SHARED-0007` remains the broad deferred attachment task, depends on that proof, and must review and reuse it while preserving its later consumers.

Connected progression remains independently testable:

```text
CLIENT-0012 + GAME-0004 -> CLIENT-0013  physical drops
CLIENT-0013 + GAME-0003 + GAME-0005 -> CLIENT-0014  inventory/equipment
CLIENT-0012 + GAME-0002 -> CLIENT-0015  XP/level feedback
CLIENT-0012 + SIM-0011 -> CLIENT-0019  health/mana/death/respawn feedback
```

Balance Lab work is `EDITOR-0004`, then `EDITOR-0005` for the same camp/combat/sustain/protection rules, then `EDITOR-0006` for progression/drop/equipment metrics. Its evidence gates deferred topology decision `ARCH-0005`.

All feature tasks remain todo until separately selected, planned, approved, and activated. The first-wings public release, trade stands, crafting, economy, persistence, PvP, multiple zones, and final topology are not M2 scope.

## M3 — Deferred transformations and companions

`CONTENT-0005` and `SIM-0005` remain future contract tasks. `CLIENT-0008` is a deliberately deferred presentation umbrella and must be split into wings, mounts, and companions before implementation. M3 has no milestone priority and is outside the vertical-slice critical path.

## Shared-source gate

Starfall does not own coordinator shared source, SDL3-CS acquisition, or character-content cooking. Completed canonical task `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0016` owns the narrow source-consumption and generated client cook/copy contract, and `CLIENT-0006` persists and consumes that exact dependency.

`SF-0006` established only Starfall policy, the single family-root property, executable allowlist gates, documentation, and task grooming. `CLIENT-0006` is the separately approved implementation that adds the references and consumes generated client content. NuGet packages, feeds, content packages, and independent full-client distribution remain deferred until real child integrations or release/CI evidence make them valuable.