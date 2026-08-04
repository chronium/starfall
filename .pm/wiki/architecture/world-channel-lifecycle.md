---
title: World and Channel Lifecycle
createdAt: 2026-08-04T08:25:28.2799600Z
modifiedAt: 2026-08-04T08:56:16.3147490Z
---

## Purpose

This page records the bounded executable contract established by `SERVER-0002` for one authoritative Starfall world/channel and the provisional immutable layout binding added by `SERVER-0004`. It is the timing and content-ownership foundation for later world-local state, admission and gameplay; none of those later capabilities are implemented here.

## Identity

Every invocation requires both:

- `--world <id>`: the logical world identity;
- `--channel <id>`: the logical channel identity.

Both values use the existing Protocol identity contract: 1-64 lowercase ASCII letters, digits or underscores, beginning with a letter. The host creates a fresh non-empty `WorldInstanceId` for every invocation. The instance identity distinguishes lifecycle incarnations of the same semantic world/channel and is not owner-configurable.

Each runtime owns its identity, tick, lifecycle state and selected immutable layout without static mutable world state. Independent invocations therefore do not share gameplay authority merely because their semantic identities match.

## Lifecycle

The legal state sequence is:

```text
Created -> Running -> Draining -> Stopped
```

Only `Running` is eligible to accept future admissions. Entering `Draining` closes that seam immediately. A draining runtime may continue fixed ticks so later session-owning work can define orderly session completion; the current empty runtime has no sessions and stops immediately. Repeated drain while already draining and repeated stop while already stopped are harmless. Other invalid transitions fail explicitly.

Ctrl+C requests graceful shutdown. It does not add an operations service, remote supervisor or hot-path dependency.

## Provisional loaded layout

Before entering `Running`, the World composition root binds exactly `Draft0GrayboxCatalog.FirstPlayable` to the runtime. The catalog is already validated immutable executable Content, so World retains it directly instead of introducing a second runtime map model, serialization format or loader abstraction.

The loaded input preserves the durable 200 x 200 metre envelope, 5..195 metre walkable bounds, protected-town description and respawn anchor, four ordered route corridors, three ordered camp regions, seven ordered proxy blocks and ten branch/local ordered sample spawns. These are owned inputs only. The runtime does not yet enforce protection, construct collision/navigation, create entities, spawn monsters or interpret routes as paths.

All current world/channel invocations use this one provisional layout. A zone-selection interface is deferred until more than one real authoritative input exists. `SERVER-0012` later replaces this disposable catalog with the bounded authoritative output of the proper Editor-authored Draft 0 scene.

## Fixed-step scheduling

Authoritative time advances only through integer ticks at 60 Hz. No gameplay API receives variable frame time.

Persistent execution uses a monotonic clock and an accumulator. One outer-loop cycle executes at most five catch-up ticks. If the host remains more than one tick behind after that budget, the remaining backlog is clamped to one step and the clamp counter increments. This protects the host from an unbounded spiral while making lost wall-clock backlog visible in final diagnostics.

The optional `--run-ticks <positive>` mode advances exactly the requested number of ticks without wall-clock pacing. It exists for deterministic validation and automation, not as a second simulation model.

## Process diagnostics

A successful run prints exactly one line for each lifecycle checkpoint:

- `STARFALL_WORLD_READY` with world, channel, fresh instance, exact zone/town identities, branch/route/proxy/spawn counts, 60 Hz rate and running state;
- `STARFALL_WORLD_DRAINING` with the same identity and final tick;
- `STARFALL_WORLD_STOPPED` with final tick, catch-up clamp count, stop reason and stopped state.

Missing or invalid arguments write a deterministic error to standard error and exit with code 2. Fatal lifecycle failures exit with code 1. Finite completion and Ctrl+C shutdown exit with code 0.

## Validation commands

From the Starfall repository after restore/build:

```sh
dotnet run --project src/Starfall.World/Starfall.World.csproj --no-restore --no-build -- \
  --world world_1 --channel channel_1 --run-ticks 60

dotnet run --project src/Starfall.World/Starfall.World.csproj --no-restore --no-build -- \
  --world world_1 --channel channel_1
```

Stop the second command with Ctrl+C and verify the same instance identity appears in all lifecycle lines and the final tick is positive.

## Ownership and next seams

`Starfall.World` is the headless composition root over Content, Protocol and Simulation. Its output remains free of SDL, GPU, editor and presentation dependencies.

Later focused tasks own:

- `SERVER-0006`: stable world-local identity and one technical player;
- `SERVER-0003`: ticket consumption, admission exchange and gameplay-session creation;
- `SIM-0008`: authoritative click-to-move after its shared Box3D prerequisite completes;
- `PROTOCOL-0003` and `PROTOCOL-0004`: proven connected-walking facts and deterministic serialization;
- `SERVER-0005`: connected movement exchange.

Those tasks may consume this lifecycle but must not retroactively turn it into a generic host framework.

## Non-goals

This contract does not create entities, sessions, physics worlds or network sockets, construct collision/navigation behavior, validate join tickets, persist state, expose health endpoints, configure logging/metrics, supervise processes, call identity/chat/operations, or decide final physical deployment topology. Loading the single immutable provisional catalog is not a general map, terrain, scene, streaming or asset format.