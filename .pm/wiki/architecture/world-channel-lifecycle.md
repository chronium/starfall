---
title: World and Channel Lifecycle
createdAt: 2026-08-04T08:25:28.2799600Z
modifiedAt: 2026-08-04T08:25:28.2799600Z
---

## Purpose

This page records the bounded executable contract established by `SERVER-0002` for one empty authoritative Starfall world/channel. It is the timing and lifecycle foundation for later loaded content, world-local state, admission and gameplay; none of those later capabilities are implemented here.

## Identity

Every invocation requires both:

- `--world <id>`: the logical world identity;
- `--channel <id>`: the logical channel identity.

Both values use the existing Protocol identity contract: 1-64 lowercase ASCII letters, digits or underscores, beginning with a letter. The host creates a fresh non-empty `WorldInstanceId` for every invocation. The instance identity distinguishes lifecycle incarnations of the same semantic world/channel and is not owner-configurable.

Each runtime owns its identity, tick and lifecycle state without static mutable world state. Independent invocations therefore do not share gameplay authority merely because their semantic identities match.

## Lifecycle

The legal state sequence is:

```text
Created -> Running -> Draining -> Stopped
```

Only `Running` is eligible to accept future admissions. Entering `Draining` closes that seam immediately. A draining runtime may continue fixed ticks so later session-owning work can define orderly session completion; the current empty runtime has no sessions and stops immediately. Repeated drain while already draining and repeated stop while already stopped are harmless. Other invalid transitions fail explicitly.

Ctrl+C requests graceful shutdown. It does not add an operations service, remote supervisor or hot-path dependency.

## Fixed-step scheduling

Authoritative time advances only through integer ticks at 60 Hz. No gameplay API receives variable frame time.

Persistent execution uses a monotonic clock and an accumulator. One outer-loop cycle executes at most five catch-up ticks. If the host remains more than one tick behind after that budget, the remaining backlog is clamped to one step and the clamp counter increments. This protects the host from an unbounded spiral while making lost wall-clock backlog visible in final diagnostics.

The optional `--run-ticks <positive>` mode advances exactly the requested number of ticks without wall-clock pacing. It exists for deterministic validation and automation, not as a second simulation model.

## Process diagnostics

A successful run prints exactly one line for each lifecycle checkpoint:

- `STARFALL_WORLD_READY` with world, channel, fresh instance, 60 Hz rate and running state;
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

- `SERVER-0004`: load and own the provisional graybox;
- `SERVER-0006`: stable world-local identity and one technical player;
- `SERVER-0003`: ticket consumption, admission exchange and gameplay-session creation;
- `SIM-0008`: authoritative click-to-move after its shared Box3D prerequisite completes;
- `PROTOCOL-0003` and `PROTOCOL-0004`: proven connected-walking facts and deterministic serialization;
- `SERVER-0005`: connected movement exchange.

Those tasks may consume this lifecycle but must not retroactively turn it into a generic host framework.

## Non-goals

This contract does not load a zone, create entities, sessions, physics worlds or network sockets, validate join tickets, persist state, expose health endpoints, configure logging/metrics, supervise processes, call identity/chat/operations, or decide final physical deployment topology.