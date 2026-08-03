---
title: Architecture Overview
createdAt: 2026-08-01T05:48:09.1031030Z
modifiedAt: 2026-08-03T08:32:46.8546610Z
---

## Purpose

Starfall is an independently owned server-authoritative MMORPG and a linked child of ChronoFall. It owns its PM project, source history, architecture, simulation, protocol, content, presentation integration, editor/Balance Lab, build, release and commits. ChronoFall owns family coordination, proven shared modules and the pinned Starfall commit.

The canonical full-client development environment is the shallow coordinator family checkout. Independent ownership does not require isolated full-client builds.

## Foundation and service boundaries

Starfall separates native Client presentation, headless World orchestration, authoritative Simulation, transport-neutral Protocol, Content, Editor authoring and headless Balance Lab.

Once admitted, an active player's gameplay session does not depend on identity, chat or operations remaining available. PROTOCOL-0002 owns the signed sfjt1 admission contract. SERVER-0003 consumes tickets atomically and creates world-owned sessions independently of gameplay message serialization.

World/Simulation own movement, combat, character state, monsters, camps, progression, drops and equipment. Client owns input presentation, rendering, animation, IK, effects, camera and smoothing. Headless projects never depend on SDL, GPU, ImGui, renderer, editor UI or presentation assets.

Logical identity/lobby, world, chat, operations and persistence boundaries do not require one process per concept. Physical topology and persistence degradation remain evidence-gated.

## Foundation assembly graph

~~~text
Starfall.Content
Starfall.Protocol
Starfall.Simulation -> Content
Starfall.World -> Content, Protocol, Simulation
Starfall.Client -> Content, Protocol
Starfall.Editor -> Content
Starfall.BalanceLab -> Content, Simulation
~~~

Content and Protocol remain product-dependency-free. Simulation never depends on Protocol. Client never references World or Simulation. World is the composition boundary between protocol, content and simulation. Editor authoring stays separate from compact runtime data; Balance Lab remains headless.

CLIENT-0006 adds only the approved coordinator character-presentation source set to Client through ChronoFallFamilyRoot. Future EDITOR-0007 may add a separately explicit, architecture-tested static-rendering allowlist for Editor; no such reference exists merely because the task is planned.

## Evidence-driven vertical-slice order

Starfall does not complete a speculative backend before producing visible client evidence.

The native-presentation lane proves:

1. CONTENT-0014 provisional executable graybox;
2. CLIENT-0005 camera/ground picking;
3. CLIENT-0020 generated graybox rendering;
4. CLIENT-0021 technical humanoid and reusable world-presentation adapter;
5. CLIENT-0022 placeholder monster presentation from deterministic fixtures.

The authoritative lane proves:

1. SERVER-0002 60 Hz world lifecycle;
2. SERVER-0004 loaded graybox;
3. SERVER-0006 one concrete generic technical player;
4. SIM-0008 authoritative movement;
5. camp spawning, Basic Arrow, bounded monster behavior and protected-town return.

Protocol is derived from those proven concepts. PROTOCOL-0003/0004 first cover only connected player walking. SERVER-0005 exchanges those commands/snapshots and CLIENT-0009 maps them into the same adapter used by the local fixture. Monsters then extend the contract through PROTOCOL-0005, SERVER-0007 and CLIENT-0023. Combat facts/serialization/exchange follow proven combat simulation.

Progression, physical drops and inventory/equipment each retain focused protocol and server-exchange extensions rather than expanding the connected-walking contract.

## Two visible milestones

### Local walking graybox

Generated graybox, isometric camera, ground picking, technical humanoid, click intent and deterministic authoritative-style movement fixture. No connection, selected final assets or client gameplay authority.

### Connected walking world

~~~text
PROTOCOL-0002 -> SERVER-0003
PROTOCOL-0003 -> PROTOCOL-0004
SERVER-0003 + SERVER-0006 + SIM-0008 + PROTOCOL-0004
  -> SERVER-0005
  -> CLIENT-0009
~~~

This milestone proves real world-owned player identity/state, authoritative movement, serialized snapshots, world-host exchange and Client adapter reuse. Monsters do not block it.

## Map authoring boundary

CONTENT-0006 owns durable gameplay/layout requirements. CONTENT-0014 depends on it and owns provisional executable graybox coordinates, regions, proxy geometry, coarse collision/navigation and sample spawns.

EDITOR-0007 later authors one proper Draft 0 scene from the durable requirements, graybox evidence and exact selected/staged assets. It compiles separate outputs:

- SERVER-0012 consumes only authoritative regions/collision/navigation/respawn/camp/spawn inputs;
- CLIENT-0016 consumes only visual placements and staged presentation assets.

This boundary does not create a general map, terrain, scene, streaming or reflective runtime component framework.

## Numerical contract

Integer authoritative state covers HP, mana, damage, XP, levels, currency, item counts and discrete stats. Probabilities use explicitly scaled integers. Authoritative time uses fixed integer ticks.

Spatial/physics authority uses finite Box3D-native single-precision metres. Content authoring uses BCL-only immutable System.Numerics-backed values with identical units/precision and rejects NaN, infinity and out-of-zone data. Simulation converts components one-to-one without maintaining a parallel integer-millimetre model. Stable identities and explicit sorting protect gameplay from unordered native query results.

Coordinator SHARED-0021 is the allocated bounded shared Box3D runtime prerequisite, and SF-0009 Cycle 3 has attached its canonical URI to SIM-0008. The dependency is valid but waiting while SHARED-0021 remains todo; SIM-0008 must not activate or consume shared Box3D source until SHARED-0021 completes and SIM-0008 receives its own approved implementation plan.

## Family source and asset boundaries

Starfall may consume only explicitly approved parent projects through ChronoFallFamilyRoot and never depends on Royale. Parent shared modules never depend on Starfall. Generated client content enters only through stable-project-ID staging and remains ignored.

Starfall owns game-specific selection and composition. ChronoFall owns supplied-source provenance, reusable cooking/rendering contracts and stable-ID staging. The generated graybox has no asset-selection gate. The proper Editor-authored scene uses only exact selected/staged assets.

Draft 0: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/product/first-playable-zone-draft-0

Zone contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-zone-contract

Service contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/service-availability-and-ownership

Family contract: pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/wiki/architecture/shared-engine-boundaries
