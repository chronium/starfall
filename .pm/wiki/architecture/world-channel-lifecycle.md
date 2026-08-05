---
title: World and Channel Lifecycle
createdAt: 2026-08-04T08:25:28.2799600Z
modifiedAt: 2026-08-05T12:32:02.5004750Z
---

## Purpose

This page records the bounded executable contract established by `SERVER-0002` for one authoritative Starfall world/channel, the provisional immutable layout binding from `SERVER-0004`, admission/session ownership from `SERVER-0003`, generic player state from `SERVER-0006`, authoritative movement from `SIM-0008`, connected walking from `SERVER-0005`, and World-owned Draft 0 monster occupancy from `SIM-0006`. It remains the timing and content-ownership foundation for later gameplay.

## Identity

Every invocation requires both:

- `--world <id>`: the logical world identity;
- `--channel <id>`: the logical channel identity.

Both values use the existing Protocol identity contract: 1-64 lowercase ASCII letters, digits or underscores, beginning with a letter. The host creates a fresh non-empty `WorldInstanceId` for every invocation. The instance identity distinguishes lifecycle incarnations of the same semantic world/channel and is not owner-configurable.

Each runtime owns its identity, tick, lifecycle state, selected immutable layout, camp population and one entity identity sequence without static mutable world state. Independent invocations therefore do not share gameplay authority merely because their semantic identities match.

`WorldEntityId` is a positive unsigned 64-bit identity meaningful only together with the owning `WorldInstanceId`. Players and monsters allocate from the same checked monotonic sequence. Identities never wrap, reset or re-enter the available pool during that runtime, and exhaustion fails explicitly. Allocation is repeatable for an identical creation sequence, but exact numeric IDs for particular monsters or the first player are opaque implementation results rather than simulation or protocol promises.

## Lifecycle

The legal state sequence is:

```text
Created -> Running -> Draining -> Stopped
```

Entering `Running` atomically creates the ten approved Draft 0 monsters before admissions or standalone technical-player creation can occur. Only `Running` accepts new admission or creates technical players.

Entering `Draining` closes those creation seams immediately while retaining existing sessions, players, monsters, camp occupancy and pending replenishments. Fixed ticks, player movement, explicit player/monster removal and due camp replenishment continue under the same gameplay rules. Draining does not introduce different combat or simulation semantics; the later drain-deadline policy owns disconnect or migration of remaining sessions.

`Stopped` terminates and clears sessions, consumed-ticket records, players, monsters, placement occupancy and pending replenishments without resetting the entity identity sequence. Offline mode creates one technical player after entering `Running`; connected mode creates no technical player and admits session-bound players while `Running`. Repeated drain while already draining and repeated stop while already stopped are harmless. Other invalid transitions fail explicitly.

Ctrl+C requests graceful shutdown. It does not add an operations service, remote supervisor or hot-path dependency.

## Provisional loaded layout

Before entering `Running`, the World composition root binds exactly `Draft0GrayboxCatalog.FirstPlayable` to the runtime. The catalog is already validated immutable executable Content, so World retains it directly instead of introducing a second runtime map model, serialization format or loader abstraction.

The loaded input preserves the durable 200 x 200 metre envelope, 5..195 metre walkable bounds, protected-town description and respawn anchor, four ordered route corridors, three ordered camp regions, seven ordered proxy blocks and ten branch/local ordered sample spawns. `SIM-0008` interprets only the four zone-to-walkable strips and seven proxies as coarse collision. It does not interpret routes as paths, spawn monsters, or enforce hostile-action and monster protection rules.

All current world/channel invocations use this one provisional layout. A zone-selection interface is deferred until more than one real authoritative input exists. `SERVER-0012` later replaces this disposable catalog with the bounded authoritative output of the proper Editor-authored Draft 0 scene.

## Technical player state

After entering `Running`, the standalone host creates one generic authoritative player at `town_safe`'s configured respawn anchor `(100,0,25)`, with zero planar velocity and normalized `+Z` facing. This command-line fixture is separate from admitted gameplay sessions and remains useful for deterministic lifecycle checks.

Successful admission atomically creates a distinct generic player at the same configured anchor and binds its immutable `WorldEntityId` to the new gameplay session. Rejected and replayed admissions create no player. The binding is host-owned context: connected movement commands carry no entity identity, so a session cannot nominate another player.

`WorldPlayerState` is an immutable World-owned bucket containing its world-local entity identity, finite ground position, finite planar velocity in metres/second, normalized planar facing, 0.35 m radius by 1.8 m tall collision capsule, and latest movement outcome. Lookup never exposes mutable runtime-owned state. Movement replaces whole states under the runtime lock; ordered defensive snapshots therefore remain stable after later movement, creation or removal. Ordering is ascending `WorldEntityId`, independent of dictionary or native query enumeration.

Standalone technical-player creation is allowed only in `Running`; removal is allowed in `Running` and `Draining`, but the technical removal seam rejects any session-bound player. Draining retains admitted players. Stopping clears them. Distinct runtime instances own independent opaque identity sequences; tests verify repeatability for identical creation order without promising a literal player ID.

`Draft0PlayerMovementSimulation` owns a zero-gravity Box3D collision world and accepts entity-targeted finite ground destinations. Movement uses a provisional 4.0 m/s speed at 60 Hz. A destination outside capsule-adjusted walkable bounds or overlapping a proxy is rejected without replacing the current destination. Unobstructed motion advances directly; arrival clamps exactly; a sweep hit moves to Box3D's safe fraction, reports blocked, clears the destination and does not slide or retry. Players do not collide with one another in this task. The town remains traversable by players; `SIM-0011` later owns hostile-action rejection, monster exclusion/disengagement, defeat and respawn.

## Monster population state

`SIM-0006` binds the validated `Draft0StarterMonsterCatalog.FirstPlayable` and `Draft0CampPolicyCatalog.FirstPlayable` inputs to the same loaded graybox. World validates their camp geometry, ordered placement identities, archetypes and exact positions before startup; it does not invent a second monster catalog.

`WorldMonsterState` is an immutable World-owned record containing opaque world-local entity identity, camp identity, placement-slot identity, archetype identity, exact ground point, current integer health and spawn tick. Initial health is 700 units for `starter_flyer_light` and 2,000 units for `starter_flyer_heavy`. Ordered defensive snapshots and lookup never expose mutable runtime occupancy.

Startup fills every approved fixed slot in canonical camp then slot order at tick zero. A successful authoritative removal makes only that slot vacant at the current tick. The complete vacancy set is validated through the checked Simulation schedule before occupancy changes, so eligibility overflow preserves the existing monster. Unknown or already-removed identities are harmless false results.

After each fixed World tick advances, every due vacancy is applied in eligibility, canonical camp and canonical slot order. A replacement uses the same camp, slot, archetype, exact position and full initial health, but receives a fresh entity identity and the current spawn tick. Capacity is exactly 3/4/3 and no placement slot can hold more than one entity. The current fixed-slot rule consumes no seed or randomness.

`SIM-0004` now extends this occupancy with Basic Arrow integer damage and first-defeat removal; `SIM-0010` owns collision radius, movement, target selection, awareness, pursuit, outgoing attacks and return. No monster protocol or presentation is emitted here.

## Basic Arrow combat scheduling

`SIM-0004` adds one bounded authoritative combat lane without changing the world lifecycle or project graph. Simulation owns immutable Basic Arrow intent, tuning, pending-action, cancellation/resolution and integer-damage facts. World owns per-actor pending/cadence state, immutable player/monster replacement and application to the fixed-slot monster population.

At an accepted start tick `T`, World stops the actor's current destination, zeroes velocity, faces the target, records resolution at `T + 12` and reserves the next start at `T + 48`. Accepted movement before resolution cancels the shot while preserving cadence. After movement advances and the world tick increments, due shots resolve in ascending actor identity order before camp replenishment. Nonlethal damage replaces monster health; first defeat validates and creates the vacancy at the resolve tick. Running and Draining share these rules. Player/session removal clears that actor's combat state, and Stop clears all pending combat state.

The current seam is intentionally local and headless. It creates no Protocol encoding, network exchange, Client presentation, projectile entity, generic ability runtime, monster behavior, protected-town rule, drop, XP or respawn behavior.

## Fixed-step scheduling

Authoritative time advances only through integer ticks at 60 Hz. No gameplay API receives variable frame time.

Persistent execution uses a monotonic clock and an accumulator. One outer-loop cycle executes at most five catch-up ticks. If the host remains more than one tick behind after that budget, the remaining backlog is clamped to one step and the clamp counter increments. This protects the host from an unbounded spiral while making lost wall-clock backlog visible in final diagnostics.

The optional `--run-ticks <positive>` mode advances exactly the requested number of ticks without wall-clock pacing. It exists for deterministic validation and automation, not as a second simulation model.

World first applies authoritative player movement, then increments the checked World tick, resolves due Basic Arrows in actor-identity order, and finally applies camp replacements whose eligibility is less than or equal to that new tick. A monster removed at tick `T` is therefore absent through `T + 599` and recreated exactly at `T + 600`. This ordering is identical in Running and Draining.

## Process diagnostics

A successful run prints exactly one line for each lifecycle checkpoint:

- `STARFALL_WORLD_READY` with world, channel, fresh instance, exact zone/town identities, branch/route/proxy/spawn counts, opaque technical-player identity, player count, monster count, 60 Hz rate and running state;
- `STARFALL_WORLD_DRAINING` with the same identity, final tick, retained player count and retained monster count;
- `STARFALL_WORLD_STOPPED` with final tick, cleared player and monster counts, catch-up clamp count, stop reason and stopped state.

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

`Starfall.World` remains the headless composition root over Content, Protocol, Simulation and the one approved coordinator transport adapter. It owns runtime camp occupancy and concrete monster entities while remaining free of SDL, GPU, editor and presentation dependencies.

Offline mode creates one standalone technical player and supports finite or persistent execution. Connected mode requires `--listen-port` plus one or more repeatable `--verification-key <key-id>=<public-pem-path>` values, creates no player before admission, and cannot combine with `--run-ticks`.

`WorldConnectedWalkingNetworkHost` owns one caller-polled peer/session registry. It rejects non-loopback endpoints before parsing, enforces the exact admission and walking channel/delivery contract, binds accepted peers to gameplay sessions, routes movement commands through `WorldWalkingExchange`, publishes latest snapshots, and sends immediate corrections. Network errors and one-peer send failures are diagnostic and isolate cleanup to that peer/session; they do not stop the world.

Disconnect atomically removes the active gameplay session, walking publication state, authoritative player and Simulation mover while the world is Running or Draining. Entity IDs are never reused. Draining continues to poll and serve existing sessions and ordinary deterministic camp simulation but rejects new admission. There is no reconnect grace or resumable session in this slice.

Monster behavior and outgoing combat, combat/monster protocol and presentation, persistence, protected non-loopback transport, multiple-world hosting and final deployment topology remain separately owned.

## Non-goals

This contract does not create a generic entity/component framework, ECS, character controller, navigation/pathfinding framework, message-framing system or generic exchange host. Connected mode creates one bounded loopback development socket and exposes only the approved admission and walking exchanges. It does not provision production verification keys, support protected non-loopback transport, persist sessions or player state, expose health endpoints, configure logging/metrics, supervise processes, call identity/chat/operations, or decide final physical deployment topology. Loading the single immutable provisional catalog is not a general map, terrain, scene, streaming or asset format.