---
title: World and Channel Lifecycle
createdAt: 2026-08-04T08:25:28.2799600Z
modifiedAt: 2026-08-04T17:50:35.7057230Z
---

## Purpose

This page records the bounded executable contract established by `SERVER-0002` for one authoritative Starfall world/channel, the provisional immutable layout binding added by `SERVER-0004`, the lifecycle-local admission/session boundary added by `SERVER-0003`, the generic technical player state added by `SERVER-0006`, authoritative movement added by `SIM-0008`, and the admitted-session walking exchange added by `SERVER-0005`. It remains the timing and content-ownership foundation for later gameplay.

## Identity

Every invocation requires both:

- `--world <id>`: the logical world identity;
- `--channel <id>`: the logical channel identity.

Both values use the existing Protocol identity contract: 1-64 lowercase ASCII letters, digits or underscores, beginning with a letter. The host creates a fresh non-empty `WorldInstanceId` for every invocation. The instance identity distinguishes lifecycle incarnations of the same semantic world/channel and is not owner-configurable.

Each runtime owns its identity, tick, lifecycle state, selected immutable layout and entity identity sequence without static mutable world state. Independent invocations therefore do not share gameplay authority merely because their semantic identities match.

`WorldEntityId` is a positive unsigned 64-bit identity meaningful only together with the owning `WorldInstanceId`. A new runtime begins at 1. Allocation uses checked monotonic arithmetic: identities never wrap, reset or re-enter the available pool during that runtime, and exhaustion fails explicitly.

## Lifecycle

The legal state sequence is:

```text
Created -> Running -> Draining -> Stopped
```

Only `Running` is eligible to accept admission or create technical players. Entering `Draining` closes those seams immediately while retaining existing world-owned sessions and players, allowing fixed ticks and explicit player removal to continue. `Stopped` terminates and clears remaining in-memory sessions, consumed-ticket records and players without resetting the runtime's entity identity sequence. Offline command-line mode configures no keys or admission transport, so a standalone run has no sessions; it creates one technical player after entering `Running`. Connected mode instead configures verification keys and a listener, creates no technical player, and admits session-bound players while `Running`. Repeated drain while already draining and repeated stop while already stopped are harmless. Other invalid transitions fail explicitly.

Ctrl+C requests graceful shutdown. It does not add an operations service, remote supervisor or hot-path dependency.

## Provisional loaded layout

Before entering `Running`, the World composition root binds exactly `Draft0GrayboxCatalog.FirstPlayable` to the runtime. The catalog is already validated immutable executable Content, so World retains it directly instead of introducing a second runtime map model, serialization format or loader abstraction.

The loaded input preserves the durable 200 x 200 metre envelope, 5..195 metre walkable bounds, protected-town description and respawn anchor, four ordered route corridors, three ordered camp regions, seven ordered proxy blocks and ten branch/local ordered sample spawns. `SIM-0008` interprets only the four zone-to-walkable strips and seven proxies as coarse collision. It does not interpret routes as paths, spawn monsters, or enforce hostile-action and monster protection rules.

All current world/channel invocations use this one provisional layout. A zone-selection interface is deferred until more than one real authoritative input exists. `SERVER-0012` later replaces this disposable catalog with the bounded authoritative output of the proper Editor-authored Draft 0 scene.

## Technical player state

After entering `Running`, the standalone host creates one generic authoritative player at `town_safe`'s configured respawn anchor `(100,0,25)`, with zero planar velocity and normalized `+Z` facing. This command-line fixture is separate from admitted gameplay sessions and remains useful for deterministic lifecycle checks.

Successful admission now atomically creates a distinct generic player at the same configured anchor and binds its immutable `WorldEntityId` to the new gameplay session. Rejected and replayed admissions create no player. The binding is host-owned context: connected movement commands carry no entity identity, so a session cannot nominate another player.

`WorldPlayerState` is an immutable World-owned bucket containing its world-local entity identity, finite ground position, finite planar velocity in metres/second, normalized planar facing, 0.35 m radius by 1.8 m tall collision capsule, and latest movement outcome. Lookup never exposes mutable runtime-owned state. Movement replaces whole states under the runtime lock; ordered defensive snapshots therefore remain stable after later movement, creation or removal. Ordering is ascending `WorldEntityId`, independent of dictionary or native query enumeration.

Standalone technical-player creation is allowed only in `Running`; removal is allowed in `Running` and `Draining`, but the technical removal seam rejects any session-bound player. Draining retains admitted players. Stopping clears them. Distinct runtime instances each own an independent identity sequence beginning at 1.

`Draft0PlayerMovementSimulation` owns a zero-gravity Box3D collision world and accepts entity-targeted finite ground destinations. Movement uses a provisional 4.0 m/s speed at 60 Hz. A destination outside capsule-adjusted walkable bounds or overlapping a proxy is rejected without replacing the current destination. Unobstructed motion advances directly; arrival clamps exactly; a sweep hit moves to Box3D's safe fraction, reports blocked, clears the destination and does not slide or retry. Players do not collide with one another in this task. The town remains traversable by players; `SIM-0011` later owns hostile-action rejection, monster exclusion/disengagement, defeat and respawn.

## Fixed-step scheduling

Authoritative time advances only through integer ticks at 60 Hz. No gameplay API receives variable frame time.

Persistent execution uses a monotonic clock and an accumulator. One outer-loop cycle executes at most five catch-up ticks. If the host remains more than one tick behind after that budget, the remaining backlog is clamped to one step and the clamp counter increments. This protects the host from an unbounded spiral while making lost wall-clock backlog visible in final diagnostics.

The optional `--run-ticks <positive>` mode advances exactly the requested number of ticks without wall-clock pacing. It exists for deterministic validation and automation, not as a second simulation model.

## Process diagnostics

A successful run prints exactly one line for each lifecycle checkpoint:

- `STARFALL_WORLD_READY` with world, channel, fresh instance, exact zone/town identities, branch/route/proxy/spawn counts, technical-player identity, player count, 60 Hz rate and running state;
- `STARFALL_WORLD_DRAINING` with the same identity, final tick and retained player count;
- `STARFALL_WORLD_STOPPED` with final tick, cleared player count, catch-up clamp count, stop reason and stopped state.

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

`Starfall.World` remains the headless composition root over Content, Protocol, Simulation and the one approved coordinator transport adapter. Its output remains free of SDL, GPU, editor and presentation dependencies.

Offline mode still creates one standalone technical player and supports finite or persistent execution. Connected mode requires `--listen-port` plus one or more repeatable `--verification-key <key-id>=<public-pem-path>` values, creates no player before admission, and cannot combine with `--run-ticks`.

`WorldConnectedWalkingNetworkHost` owns one caller-polled peer/session registry. It rejects non-loopback endpoints before parsing, enforces the exact admission and walking channel/delivery contract, binds accepted peers to gameplay sessions, routes movement commands through `WorldWalkingExchange`, publishes latest snapshots, and sends immediate corrections. Network errors and one-peer send failures are diagnostic and isolate cleanup to that peer/session; they do not stop the world.

Disconnect atomically removes the active gameplay session, walking publication state, authoritative player and Simulation mover while the world is Running or Draining. Entity IDs are never reused. Draining continues to poll and serve existing sessions but rejects new admission. There is no reconnect grace or resumable session in this slice.

Combat, monsters, persistence, protected non-loopback transport, multiple-world hosting and final deployment topology remain separately owned.

## Non-goals

This contract does not create a generic entity/component framework, ECS, character controller, navigation/pathfinding framework, message-framing system or generic exchange host. Connected mode creates one bounded loopback development socket and exposes only the approved admission and walking exchanges. It does not provision production verification keys, support protected non-loopback transport, persist sessions or player state, expose health endpoints, configure logging/metrics, supervise processes, call identity/chat/operations, or decide final physical deployment topology. Loading the single immutable provisional catalog is not a general map, terrain, scene, streaming or asset format.