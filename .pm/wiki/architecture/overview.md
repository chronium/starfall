---
title: Architecture Overview
createdAt: 2026-08-01T05:48:09.1031030Z
modifiedAt: 2026-08-01T05:48:09.1031030Z
---

## Purpose

Starfall is an independently useful server-authoritative MMORPG inspired by classic MU Online. It remains a child of ChronoFall for family roadmap and shared-engine coordination but owns its architecture, simulation, protocol, content, editor, build, and release lifecycle.

## Planned boundaries

The foundation separates native client/presentation, headless server, authoritative simulation, protocol/replication, content, editor/authoring, and a headless Balance Lab. The exact project graph is owned by `ARCH-0004` and must be approved before implementation.

Servers own world state, movement, combat, monsters, progression, drops, equipment, and persistent outcomes. Clients own input presentation, rendering, animation, IK, effects, cameras, and smoothing. Headless projects never depend on SDL windowing/GPU, ImGui, rendering, or editor code.

Starfall may consume parent-owned shared modules but never depends on Royale. Parent shared modules never depend on Starfall.

## Initial vertical slice

The initial slice is one world/channel, one small zone, one class, shaped monster spots, basic attacks, one geometric AoE, experience, physical drops, visible equipment, and a Balance Lab using the same authoritative rules. Accounts, persistence depth, trade stands, full economy, wings progression, territory, and the complete public release remain deferred.

Family contracts: `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/wiki/architecture/shared-engine-boundaries`.