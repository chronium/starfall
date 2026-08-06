---
title: World and Channel Lifecycle
createdAt: 2026-08-04T08:25:28.2799600Z
modifiedAt: 2026-08-06T07:12:01.0686450Z
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

`WorldPlayerState` is an immutable World-owned bucket containing its world-local entity identity, finite ground position, finite planar velocity in metres/second, normalized planar facing, 0.35 m radius by 1.8 m tall collision capsule, latest movement outcome, current integer health, life state and optional respawn tick. The shared life tuning supplies maximum and restored health. Lookup never exposes mutable runtime-owned state. World replaces whole states under the runtime lock; ordered defensive snapshots therefore remain stable after later movement, damage, defeat, respawn, creation or removal. Ordering is ascending `WorldEntityId`, independent of dictionary or native query enumeration.

Draft 0 players begin active at 2,500 of 2,500 health. Ordered monster attack requests reduce only active targets. The first lethal application clamps health to zero, creates one defeat transition, clears movement eligibility and pending Basic Arrow state, and schedules respawn at the checked current tick plus 180. The defeated player and admitted session keep the same identity. Connected movement intents during lockout are consumed and receive authoritative corrections rather than moving the entity.

At the exact respawn tick, World restores the same entity to `town_safe`'s configured respawn anchor with 2,500 health, zero velocity, `+Z` facing and active movement registration. Hostile player actions are rejected while the actor is inside the inclusive protected-town footprint. This completed lifecycle adds no placeholder Mana state. M6 independently owns Mana through CONTENT-0016, SIM-0012, PROTOCOL-0014, SERVER-0016 and CLIENT-0032; a later Player Life integration freezes the same-entity respawn policy for Mana.

Standalone technical-player creation is allowed only in `Running`; removal is allowed in `Running` and `Draining`, but the technical removal seam rejects any session-bound player. Draining retains admitted players. Stopping clears them. Distinct runtime instances own independent opaque identity sequences; tests verify repeatability for identical creation order without promising a literal player ID.

`Draft0PlayerMovementSimulation` owns a zero-gravity Box3D collision world and accepts entity-targeted finite ground destinations. Movement uses a provisional 4.0 m/s speed at 60 Hz. A destination outside capsule-adjusted walkable bounds or overlapping a proxy is rejected without replacing the current destination. Unobstructed motion advances directly; arrival clamps exactly; a sweep hit moves to Box3D's safe fraction, reports blocked, clears the destination and does not slide or retry. Players do not collide with one another in this task. The town remains traversable by players while its protected-area rule rejects hostile player actions and excludes monsters.

## Monster population state

`SIM-0006` binds the validated `Draft0StarterMonsterCatalog.FirstPlayable` and `Draft0CampPolicyCatalog.FirstPlayable` inputs to the same loaded graybox. World validates their camp geometry, ordered placement identities, archetypes and exact positions before startup; it does not invent a second monster catalog.

`WorldMonsterState` is an immutable World-owned record containing opaque world-local entity identity, camp identity, placement-slot identity, archetype identity, exact ground point, current and maximum integer health and spawn tick. Initial health is 700 units for `starter_flyer_light` and 2,000 units for `starter_flyer_heavy`. Ordered defensive snapshots and lookup never expose mutable runtime occupancy.

Startup fills every approved fixed slot in canonical camp then slot order at tick zero. A successful authoritative removal makes only that slot vacant at the current tick. The complete vacancy set is validated through the checked Simulation schedule before occupancy changes, so eligibility overflow preserves the existing monster. Unknown or already-removed identities are harmless false results.

After each fixed World tick advances, every due vacancy is applied in eligibility, canonical camp and canonical slot order. A replacement uses the same camp, slot, archetype, exact position and full initial health, but receives a fresh entity identity and the current spawn tick. Capacity is exactly 3/4/3 and no placement slot can hold more than one entity. The current fixed-slot rule consumes no seed or randomness.

`SIM-0004` now extends this occupancy with Basic Arrow integer damage and first-defeat removal; `SIM-0010` owns collision radius, movement, target selection, awareness, pursuit, outgoing attacks and return. No monster protocol or presentation is emitted here.

## Bounded monster behavior scheduling

`SIM-0010` adds an immutable behavior state to every World-owned monster without changing its content or entity identity. World owns one static Draft 0 Box3D collision environment per runtime and shares it between authoritative player movement and monster movement. Simulation owns the deterministic behavior transition and collision queries; Content remains Box3D-free.

Each fixed tick supplies an immutable active-player target snapshot to the monster lane. Idle monsters acquire only active players outside the inclusive protected town, inside their own camp and awareness radius, ordered by squared distance then world entity identity. Pursuing monsters retain that target outside awareness while it remains active, outside town and inside the camp. Missing, defeated, protected or out-of-camp targets cause return; returning monsters cannot reacquire before reaching their exact home point and becoming idle.

Pursuit and return advance at configured speed divided by 60, remain ground-plane and radius-inset inside the owning camp, and respect static boundaries and proxy footprints. Camp footprints and monster homes are rejected if they intersect the protected town; movement fails before crossing into it. The bounded rule deliberately has no pathfinding, sliding, dynamic-body avoidance, altitude or navigation graph.

A monster beginning a tick inside inclusive range emits an immediate ordered attack request, then waits until its checked cadence tick. The request records attacker, target, resolve tick and requested integer damage. `SIM-0011` applies requests in monster-identity order only while the target remains active. The first lethal application creates one defeat transition; later same-tick requests remain observable but cannot mutate the defeated player.

World replaces each immutable monster record with the new behavior state while preserving health and spawn tick. Defeat removes both occupancy and behavior atomically. Replenishment registers fresh idle behavior after the behavior phase, so a new monster cannot acquire or attack on its creation tick. Draining continues behavior and player lifecycle; Stopping clears both.

## Basic Arrow combat scheduling

`SIM-0004` adds one bounded authoritative combat lane without changing the world lifecycle or project graph. Simulation owns immutable Basic Arrow intent, tuning, pending-action, cancellation/resolution and integer-damage facts. World owns per-actor pending/cadence state, immutable player/monster replacement and application to the fixed-slot monster population.

At an accepted start tick `T`, World stops the actor's current destination, zeroes velocity, faces the target, records resolution at `T + 12` and reserves the next start at `T + 48`. Accepted movement before resolution cancels the shot while preserving cadence. After movement advances and the world tick increments, due shots resolve in ascending actor identity order before camp replenishment. Nonlethal damage replaces monster health; first defeat validates and creates the vacancy at the resolve tick. Running and Draining share these rules. Player/session removal clears that actor's combat state, and Stop clears all pending combat state.

The completed SIM-0004 seam remains local and headless. M5 Connected Basic Arrow now owns the bounded continuation: Basic-only facts/serialization, admitted-session actor derivation, World exchange, connected intent and presentation convergence. SIM-0011 participates only in defeated-player and protected-town rejection. Player health, defeat, restoration and same-entity town respawn are not Basic transport or presentation scope.

The Basic action creates no authoritative projectile entity. Client bow animation and the visual arrow/impact present authoritative timing and outcomes. Camp replenishment remains the independently completed camp-lifecycle behavior and is not part of the attack deliverable.

## Fixed-step scheduling

Authoritative time advances only through integer ticks at 60 Hz. No gameplay API receives variable frame time.

Persistent execution uses a monotonic clock and an accumulator. One outer-loop cycle executes at most five catch-up ticks. If the host remains more than one tick behind after that budget, the remaining backlog is clamped to one step and the clamp counter increments. This protects the host from an unbounded spiral while making lost wall-clock backlog visible in final diagnostics.

The optional `--run-ticks <positive>` mode advances exactly the requested number of ticks without wall-clock pacing. It exists for deterministic validation and automation, not as a second simulation model.

One World tick has this frozen bounded order:

1. advance the shared static collision environment once;
2. apply authoritative movement for active players;
3. increment the checked World tick;
4. restore every player whose checked respawn tick is now due;
5. resolve due Basic Arrows in actor-identity order;
6. advance surviving monster behavior and emit attack requests in monster-identity order;
7. apply those requests to active players in the emitted order;
8. apply camp replacements whose eligibility is less than or equal to the new tick.

Basic Arrow defeat therefore removes monster behavior before the monster phase. A player respawned at step 4 is active for later phases on that exact tick but remains inside protected town, so hostile player actions are rejected and monsters cannot target it there. A lethal monster request at step 7 begins the 180-tick lockout from that resolve tick. A replacement created after behavior cannot acquire or attack until the next tick. A monster removed at tick `T` remains absent through `T + 599` and is recreated exactly at `T + 600`. Running and Draining use the same order.

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

## Deliverable continuations

M4 Development Instrumentation adds one explicitly gated, development-only admitted-session command dispatcher. Its Ping World handler and later feature-owned Mana handlers are engineering instrumentation with no stable gameplay-protocol compatibility promise.

M6 Authoritative Mana adds per-session checked integer Mana, fixed-tick regeneration, separate Mana facts/serialization, World exchange and feature-owned development commands. Mana exposes a lifecycle seam; a later Player Life integration task defines how the already-established same-entity respawn affects Mana.

## Ownership and next seams

`Starfall.World` remains the headless composition root over Content, Protocol, Simulation and the one approved coordinator transport adapter. It owns runtime camp occupancy, concrete monster entities, shared static collision-world lifetime and fixed-tick orchestration while remaining free of SDL, GPU, editor and presentation dependencies.

Offline mode creates one standalone technical player and supports finite or persistent execution. Connected mode requires `--listen-port` plus one or more repeatable `--verification-key <key-id>=<public-pem-path>` values, creates no player before admission, and cannot combine with `--run-ticks`.

`WorldGameplayNetworkHost` owns one caller-polled peer/session registry. It rejects non-loopback endpoints before parsing, binds accepted peers to gameplay sessions, routes commands through the focused `WorldWalkingExchange`, publishes walking snapshots/corrections and independently publishes bounded monster snapshots through `WorldMonsterExchange`. Network errors and one-peer send failures are diagnostic and isolate cleanup to that peer/session; they do not stop the world.

Every active session has independent walking and monster publication state. Monster channel 4 uses sequenced full snapshots at most once per observed simulation tick. World captures live and defeated facts under its runtime lock; live and tombstone arrays remain ordered by entity identity and together remain bounded to the ten placement slots. A lethal removal retains the last authoritative state by slot until exact-slot replenishment, while technical removal and Stop do not fabricate death. Draining continues publication and deterministic simulation; disconnect or Stop clears the applicable session publication state.

Disconnect atomically removes the active gameplay session, walking publication state, authoritative player and Simulation mover while the world is Running or Draining. Entity IDs are never reused. Draining continues to poll and serve existing sessions and ordinary deterministic camp simulation but rejects new admission. There is no reconnect grace or resumable session in this slice.

Bounded monster behavior and requested-damage attack facts remain inside World/Simulation. `SIM-0011` owns player damage/defeat/protected-town/respawn application; PROTOCOL-0005 and SERVER-0007 expose only approved authoritative state and retained defeat facts. CLIENT-0023 retains and presents connected monster snapshots without changing World authority. Combat exchange, persistence, protected non-loopback transport, multiple-world hosting and final deployment topology remain separately owned.

## Non-goals

This contract does not create a generic entity/component framework, ECS, character controller, navigation/pathfinding framework, message-framing system or generic exchange host. Connected mode creates one bounded loopback development socket and exposes only the approved admission and walking exchanges. It does not provision production verification keys, support protected non-loopback transport, persist sessions or player state, expose health endpoints, configure logging/metrics, supervise processes, call identity/chat/operations, or decide final physical deployment topology. Loading the single immutable provisional catalog is not a general map, terrain, scene, streaming or asset format.