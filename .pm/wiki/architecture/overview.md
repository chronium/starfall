---
title: Architecture Overview
createdAt: 2026-08-01T05:48:09.1031030Z
modifiedAt: 2026-08-01T06:48:43.4879040Z
---

## Purpose

Starfall is an independently useful server-authoritative MMORPG inspired by classic MU Online. It remains a child of ChronoFall for family roadmap and shared-engine coordination but owns its architecture, simulation, protocol, content, editor, build, and release lifecycle.

## Planned boundaries

The foundation separates native client/presentation, headless server, authoritative simulation, protocol/replication, content, editor/authoring, and a headless Balance Lab. It also preserves logical ownership boundaries for identity/lobby, realm/world, chat, operations, and persistence. The project graph and dependency direction are owned by `ARCH-0004` and must be approved before implementation.

Once admitted to a world, an active player's gameplay session does not depend on authentication, chat, or management services remaining available. Identity admits; the selected world consumes a short-lived signed join ticket and owns the gameplay session. Chat and operations remain optional from gameplay's perspective. Persistence degradation requires a later explicit contract.

Servers own world state, movement, combat, monsters, camps, progression, drops, equipment, and persistent-intent outcomes. Clients own input presentation, rendering, animation, IK, effects, cameras, and smoothing. Headless projects never depend on SDL windowing/GPU, ImGui, rendering, or editor code.

Logical boundaries do not initially require separate processes. Strict modules and a small number of executables are acceptable while the vertical slice gathers evidence for the final physical topology. Starfall may consume parent-owned shared modules but never depends on Royale. Parent shared modules never depend on Starfall.

## Initial vertical slice

The initial slice is one world/channel, one small zone, one class, shaped monster spots, basic attacks, one geometric AoE, experience, physical drops, visible equipment, and a Balance Lab using the same authoritative rules. Identity/lobby, chat, operations, and persistence implementation depth remain deferred even though their ownership and availability boundaries are defined. Trade stands, the full economy, wings progression, territory, and the complete public release also remain deferred.

Starfall service contract: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/service-availability-and-ownership`.

Family contracts: `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/wiki/architecture/shared-engine-boundaries`.