---
title: Editor and Balance Lab Boundaries
createdAt: 2026-08-04T11:24:42.1457150Z
modifiedAt: 2026-08-04T11:24:42.1457150Z
---

## Purpose

Starfall keeps content authoring and deterministic balance analysis as separate product boundaries. They may share validated Content definitions and authoritative Simulation rules, but they do not share editor UI, document identity, or a runtime service.

For `EDITOR-0003`, both `Starfall.Editor` and `Starfall.BalanceLab` remain libraries. `EDITOR-0004` owns any later Balance Lab executable-host decision and scaffolding.

## Ownership

| Boundary | Owns | Does not own |
| --- | --- | --- |
| Editor | Authoring documents and tool state, validation feedback, visual placement, and compiler orchestration | Authoritative runtime entities, gameplay execution, Balance Lab scenarios, operations supervision, or a reflective runtime object model |
| Balance Lab | Deterministic scenario inputs, seeds, fixed-step execution over authoritative rules, and machine-readable analysis outputs | Editor documents/UI, live World orchestration, Client presentation, production service hosting, or operations control |
| Content | BCL-only immutable definitions and validation inputs that may be shared by Editor, Simulation, World, and Balance Lab according to the approved project graph | Editor document/object identity or UI state |
| Simulation | Deterministic authoritative rules reusable by World and Balance Lab | Rendering, editor UI, process supervision, or authoring-document semantics |
| World | Authoritative runtime entities, sessions, camps, spawning, deaths, replenishment, drops, and active gameplay lifecycle | Content authoring UI or balance-analysis presentation |

Camp definitions and spawn/replenishment policy inputs may be shared through Content, and their deterministic rules may be shared through Simulation. Actual camp entities and outcomes remain World-owned. This sharing does not create a camp service.

## Authoring compilation boundary

Authoring representation is distinct from compact runtime representation. Runtime consumers never depend on an editor document, editor object identity, inspector model, undo state, selection state, gizmo, or UI framework.

A later focused authoring workflow may compile separate authoritative and presentation outputs. Both outputs must:

- derive from the same fully validated authoring revision;
- use stable cross-output identities where the outputs refer to the same authored concept;
- be emitted only after complete validation succeeds.

If complete validation fails, neither output is emitted. The authoritative output contains only data required by headless runtime consumers. The presentation output contains only data required by client presentation. This contract does not predefine either schema and does not create a general scene format, terrain system, streaming system, or reflective Unity-style runtime component framework.

`EDITOR-0007` owns the first concrete Draft 0 authoring document, validation rules, stable-identity scheme, and bounded authoritative/presentation output schemas.

## Balance Lab boundary

Balance Lab is headless and deterministic. It may consume Starfall.Content and Starfall.Simulation, matching the approved foundation graph. Scenario definitions provide explicit content inputs, authoritative seeds, and fixed simulation steps. Results are machine-readable evidence rather than hidden UI state.

Balance Lab must not depend on or emit:

- SDL, GPU, ImGui, Blurg, renderer, shader, texture, image, or presentation artifacts;
- Starfall.Client or Starfall.Editor;
- Starfall.World process orchestration or live World state;
- Starfall.Protocol unless a later approved task demonstrates a concrete analysis need.

`EDITOR-0004` owns the initial deterministic Balance Lab harness and any decision to introduce an executable composition root. `EDITOR-0005` and `EDITOR-0006` own focused scenarios and reporting. Those tasks must preserve this boundary rather than expanding Editor or Balance Lab into a shared runtime service.

## Operations separation

The future Angular management application and ASP.NET operations API are a distinct operations-control-plane product. They may eventually expose health, start, drain, stop, configuration, logs, metrics, development multipliers, and diagnostics through explicit adapters.

The operations control plane is neither the content Editor nor Balance Lab, never directly supervises processes from the browser, and never enters the gameplay hot path. Its availability has no effect on active gameplay sessions.

Service ownership: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/service-availability-and-ownership

Architecture overview: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/overview