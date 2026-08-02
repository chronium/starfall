---
title: Architecture Overview
createdAt: 2026-08-01T05:48:09.1031030Z
modifiedAt: 2026-08-02T08:58:18.3729700Z
---

## Purpose

Starfall is an independently useful server-authoritative MMORPG inspired by classic MU Online. It remains a child of ChronoFall for family roadmap and shared-engine coordination but owns its architecture, simulation, protocol, content, editor, build, and release lifecycle.

## Planned boundaries

The foundation separates native client presentation, headless world orchestration, authoritative simulation, transport-neutral protocol, content, editor authoring, and a headless Balance Lab. It also preserves logical ownership boundaries for identity/lobby, realm/world, chat, operations, and persistence. `BUILD-0002` realizes the approved boundaries as independently buildable .NET libraries and executable dependency tests; runnable client and world-host shells remain owned by `BUILD-0003`.

Once admitted to a world, an active player's gameplay session does not depend on authentication, chat, or management services remaining available. Identity admits; the selected world consumes a short-lived signed join ticket and owns the gameplay session. Chat and operations remain optional from gameplay's perspective. Persistence degradation requires a later explicit contract.

Servers own world state, movement, combat, monsters, camps, progression, drops, equipment, and persistent-intent outcomes. Clients own input presentation, rendering, animation, IK, effects, cameras, and smoothing. Headless projects never depend on SDL windowing/GPU, ImGui, rendering, or editor code.

Logical boundaries do not initially require separate processes. Strict modules and a small number of executables are acceptable while the vertical slice gathers evidence for the final physical topology. Starfall may consume parent-owned shared modules but never depends on Royale. Parent shared modules never depend on Starfall.

## Foundation assembly graph

The initial direct project-reference graph is:

```text
Starfall.Content
Starfall.Protocol
Starfall.Simulation -> Content
Starfall.World -> Content, Protocol, Simulation
Starfall.Client -> Content, Protocol
Starfall.Editor -> Content
Starfall.BalanceLab -> Content, Simulation
```

Content and Protocol remain product-dependency-free. Simulation owns deterministic authoritative rules and does not depend on Protocol. World is the later headless orchestration boundary between protocol, content, and simulation. Client never references World or Simulation. Editor remains an authoring boundary, while Balance Lab consumes the same deterministic content and simulation without a live-world or presentation dependency.

All seven projects are libraries during the foundation task. Identity, chat, operations, and persistence remain logical ownership boundaries rather than placeholder assemblies or immediate deployables. Changes to this graph require an approved task, updated dependency tests, and an updated Starfall architecture contract.

## Initial vertical slice

The initial slice is one world/channel, one small zone, one class, shaped monster spots, basic attacks, one geometric AoE, experience, physical drops, visible equipment, and a Balance Lab using the same authoritative rules. Identity/lobby, chat, operations, and persistence implementation depth remain deferred even though their ownership and availability boundaries are defined. Trade stands, the full economy, wings progression, territory, and the complete public release also remain deferred.

Starfall service contract: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/service-availability-and-ownership`.

Family contracts: `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/wiki/architecture/shared-engine-boundaries`.