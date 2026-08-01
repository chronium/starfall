---
id: EDITOR-0004
title: Add headless balance simulation from authoritative rules
track: EDITOR
milestone: M2
dependsOn:
- GAME-0002
- EDITOR-0003
createdAt: 2026-08-01T05:46:49.1008610Z
modifiedAt: 2026-08-01T06:49:24.3074900Z
---

Extend the Balance Lab to run deterministic headless simulations of the same authoritative combat, camp, and progression rules and report progression and spot metrics without client rendering.

Use the same camp definitions and spawn/replenishment policies consumed by the authoritative world while keeping actual live entities and runtime ownership in the world. Do not introduce a camp service, operations control plane, editor UI dependency, or presentation dependency.