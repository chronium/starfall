---
title: Architecture Overview
createdAt: 2026-08-01T05:48:09.1031030Z
modifiedAt: 2026-08-05T06:16:38.0229970Z
---

## Purpose

Starfall is an independently owned server-authoritative MMORPG and a linked child of ChronoFall. It owns its PM project, source history, architecture, simulation, protocol, content, presentation integration, editor/Balance Lab, build, release and commits. ChronoFall owns family coordination, proven shared modules and the pinned Starfall commit.

The canonical full-client development environment is the shallow coordinator family checkout. Independent ownership does not require isolated full-client builds.

## Foundation and service boundaries

Starfall separates native Client presentation, headless World orchestration, authoritative Simulation, transport-neutral Protocol, Content, Editor authoring and headless Balance Lab.

Once admitted, an active player's gameplay session does not depend on identity, chat or operations remaining available. PROTOCOL-0002 owns the signed sfjt1 admission contract. SERVER-0003 consumes tickets atomically and creates world-owned sessions independently of gameplay message serialization.

World/Simulation own movement, combat, character state, monsters, camps, progression, drops and equipment. Client owns input presentation, rendering, animation, IK, effects, camera and smoothing. Headless projects never depend on SDL, GPU, ImGui, renderer, editor UI or presentation assets.

Logical identity/lobby, world, chat, operations and persistence boundaries do not require one process per concept. Physical topology and persistence degradation remain evidence-gated.

`SERVER-0002` implements the first authoritative world/channel host: explicit Protocol-owned world and channel identities, a fresh lifecycle-scoped world-instance identity, `Created -> Running -> Draining -> Stopped`, and a fixed 60 Hz scheduler. Finite validation advances an exact positive tick count without wall-clock pacing; persistent execution uses a monotonic clock, caps catch-up at five ticks per outer-loop cycle, reports backlog clamps, and drains on Ctrl+C.

`SERVER-0004` binds the validated immutable `Draft0GrayboxCatalog.FirstPlayable` input to every current runtime before it enters `Running`. World now owns the provisional zone/town, route/camp, proxy and spawn inputs while implementing no monster state, collision/navigation behavior, physics, protection enforcement or protocol exchange. Direct immutable Content consumption is deliberate evidence, not a serialized map or general scene framework.

`SERVER-0006` adds one generic world-owned technical player at the catalog respawn anchor after startup. Its immutable state contains a world-instance-local monotonic identity, ground position, planar velocity and normalized facing. Creation, lookup, ordered defensive snapshots and lifecycle-scoped removal are explicit; IDs never reuse or wrap, and stopping clears players.

`SIM-0008` adds bounded authoritative movement without binding the player to a session or Protocol. Starfall.Simulation consumes finite ground destinations at 60 Hz, uses a provisional 4.0 m/s speed and 0.35 m radius by 1.8 m tall mover capsule, and replaces whole World-owned immutable states under the runtime lock. The four zone-to-walkable boundary strips and seven catalog proxies are created in stable order as Box3D collision. Accepted intent replaces the prior destination; rejected intent leaves it intact; arrival clamps exactly; a hit moves to the safe cast fraction, stops and clears the destination. The protected town remains traversable by players; hostile-action, monster-exclusion and respawn enforcement remain `SIM-0011`.

## Foundation assembly graph

~~~text
Starfall.Content
Starfall.Protocol
Starfall.Simulation -> Content + approved coordinator ChronoFall.Box3D source
Starfall.World -> Content, Protocol, Simulation + approved coordinator network adapter source
Starfall.Client -> Content, Protocol + approved coordinator character-presentation and network adapter source
Starfall.Editor -> Content
Starfall.BalanceLab -> Content, Simulation
~~~

Content and Protocol remain product-dependency-free. Simulation never depends on Protocol. Client never references World or Simulation. World is the composition boundary between protocol, content and simulation. Editor authoring stays separate from compact runtime data; Balance Lab remains headless.

CLIENT-0006 adds only the approved coordinator character-presentation source set to Client through ChronoFallFamilyRoot. BUILD-0006 adds only the coordinator network adapter to Client and World; its BCL contracts and pinned LiteNetLib source remain transitive. The local Starfall product graph does not change. Content, Protocol, Simulation, Editor and Balance Lab remain network-transport-free, and World remains presentation-free.

CLIENT-0009 activates those internal Client/World factories through narrow process-owned hosts. Starfall Protocol supplies exact admission/walking codecs and public channel constants without referencing transport. Client and World own polling, delivery assignments, peer/session binding, development public-key configuration and disconnect cleanup. No generic dispatcher, framing system or transport dependency enters another product project.

`tools/Starfall.DevelopmentAdmission` is a development executable outside the product graph. It references only Protocol and BCL cryptography. `tests/Starfall.ConnectedWalking.Tests` is the explicit cross-process integration-test boundary; it does not alter runtime dependency direction.

Future EDITOR-0007 may add a separately explicit, architecture-tested static-rendering allowlist for Editor; no such reference exists merely because the task is planned.

`EDITOR-0003` preserves Editor and BalanceLab as libraries while defining their separate responsibilities. Any Balance Lab executable-host choice belongs to `EDITOR-0004`. Authoritative and presentation outputs must derive from the same fully validated authoring revision, share stable cross-output identities, and be emitted together only after complete validation; runtime consumers never depend on editor document or object identity.

Editor and Balance Lab contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/editor-and-balance-lab-boundaries

Network adoption contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/network-transport-adoption

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
SERVER-0003 + SERVER-0006 + SIM-0008 + PROTOCOL-0004 -> SERVER-0005
parent SHARED-0023 + BUILD-0003 + BUILD-0005 -> BUILD-0006
SERVER-0005 + PROTOCOL-0004 + CLIENT-0021 + BUILD-0006 -> CLIENT-0009
~~~

This visible milestone is now executable over real UDP loopback. A development-only tool issues a one-minute signed ticket for the exact world instance; World consumes it, creates and binds one authoritative player, and Client sends ground intent and renders current authoritative snapshots/corrections through the same adapter proven locally.

The shared transport remains opaque infrastructure. Starfall owns exact channels/delivery, admission datagrams, peer/session binding and disconnect policy. Plaintext is restricted to literal loopback. No interpolation, client movement authority, monsters, combat, persistence, service topology or production security has been inferred.

## Map authoring boundary

CONTENT-0006 owns durable gameplay/layout requirements. CONTENT-0014 depends on it and owns provisional executable graybox coordinates, regions, proxy geometry, coarse collision/navigation and sample spawns.

EDITOR-0007 later authors one proper Draft 0 scene from the durable requirements, graybox evidence and exact selected/staged assets. It compiles separate outputs:

- SERVER-0012 consumes only authoritative regions/collision/navigation/respawn/camp/spawn inputs;
- CLIENT-0016 consumes only visual placements and staged presentation assets.

This boundary does not create a general map, terrain, scene, streaming or reflective runtime component framework.

## Numerical contract

Integer authoritative state covers HP, mana, damage, XP, levels, currency, item counts and discrete stats. Probabilities use explicitly scaled integers. Authoritative time uses fixed integer ticks.

Spatial/physics authority uses finite Box3D-native single-precision metres. Content authoring uses BCL-only immutable System.Numerics-backed values with identical units/precision and rejects NaN, infinity and out-of-zone data. Simulation converts components one-to-one without maintaining a parallel integer-millimetre model. Stable identities and explicit sorting protect gameplay from unordered native query results.

Completed coordinator task `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0021` supplies the only approved headless family-source reference: `$(ChronoFallFamilyRoot)src/ChronoFall.Box3D/ChronoFall.Box3D.csproj`. `SIM-0008` consumes that managed project directly from Starfall.Simulation; raw bindings and the matching native library remain transitive. Starfall retains entity identity, fixed-tick scheduling, collision layers, content conversion and movement outcomes.

CONTENT-0003 freezes the provisional `dark_elf_archer` catalog at 2,500 health units and the ordered `basic_arrow`, `fire_arrow`, `arrow_rain` actions with 300, 700 and 500 damage units. The catalog records deterministic identities and integer inputs only; Simulation owns validity and outcomes, while Client owns animation, attachments, projectiles and effects. Durable contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-archer-kit

## Family source and asset boundaries

Starfall may consume only explicitly approved parent projects through ChronoFallFamilyRoot and never depends on Royale. Parent shared modules never depend on Starfall. Generated client content enters only through stable-project-ID staging and remains ignored.

Starfall owns game-specific selection and composition. ChronoFall owns supplied-source provenance, reusable cooking/rendering contracts and stable-ID staging. The generated graybox has no asset-selection gate. The proper Editor-authored scene uses only exact selected/staged assets.

Draft 0: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/product/first-playable-zone-draft-0

Zone contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-zone-contract

Service contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/service-availability-and-ownership

World lifecycle contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/world-channel-lifecycle

Family contract: pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/wiki/architecture/shared-engine-boundaries

CLIENT-0024 consumes the coordinator's bounded one-shot SDL GPU readback and PNG writer only from Starfall.Client. Starfall retains ownership of its render loop, offscreen targets, camera presets, animation sample, capture recipe and output policy; headless projects remain free of presentation and image dependencies.

Capture contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/development/draft-0-graybox-capture-suite