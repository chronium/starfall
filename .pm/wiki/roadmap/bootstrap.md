---
title: Bootstrap Roadmap
createdAt: 2026-08-01T05:48:09.1150770Z
modifiedAt: 2026-08-05T10:47:23.3215500Z
---

## Execution standard

Tasks own one primary behavior/contract, focused validation and one task-scoped commit. Milestones organize capability; actual execution follows dependency and evidence order rather than finishing every earlier-labelled task first.

No feature task activates automatically. Every task requires a fresh owner-directed Plan-mode pass.

## M0 — Completed foundation history

M0 contains only completed historical foundation work. Authoritative readback found PROTOCOL-0003 and EDITOR-0003 were the only unfinished M0 members; SF-0009 moves both to M2 without relocating completed history.

Completed foundations include repository/project boundaries, runnable Client/World shells, family source policy and reference tests, automatic gitlink handoff, architecture/service ownership, and the completed PROTOCOL-0002 signed admission contract.

## M1 — Shared character foundation

CLIENT-0006 remains the sole M1 implementation. It proves coordinator-source character presentation and the technical humanoid/Idle_Loop in Starfall.Client without gameplay, networking or headless presentation leakage.

## M2 — First playable zone

Draft 0 develops through native-presentation and authoritative-simulation lanes that converge only after their concepts exist.

### Content ownership

- CONTENT-0006: completed durable zone/layout requirements.
- CONTENT-0014: provisional executable graybox coordinates, regions, proxies, coarse collision/navigation and neutral sample spawns; depends explicitly on CONTENT-0006.
- CONTENT-0003: completed dark-elf archer and ordered three-action catalog; it does not gate the generic technical walking player.
- CONTENT-0007: exact provisional starter-flyer identities, integer health and ten ordered graybox spawn assignments. It consumes CONTENT-0014 but owns no runtime spawning or behavior values.
- CONTENT-0008: progression/item inputs.
- CONTENT-0011/0012/0013: exact archer, proper-scene and monster presentation selection.

The generated graybox never depends on selected assets. Exact selection-to-coordinator-acquisition paths remain canonical and evidence-gated. Durable monster catalog: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-starter-flyers-and-camps`.

### Local walking graybox

~~~text
CONTENT-0006 -> CONTENT-0014

CLIENT-0005 + CLIENT-0006 + CONTENT-0014
  + parent SHARED-0018
  -> CLIENT-0020  generated local graybox
  -> CLIENT-0021  technical player + reusable world adapter
~~~

CLIENT-0021 is the first visible walking milestone: generated graybox, isometric camera, ground picking, technical humanoid, click intent and deterministic authoritative-style movement fixture. It has no connection, selected-final-asset gate or client gameplay authority.

### Authoritative walking lane

~~~text
BUILD-0003 -> SERVER-0002  60 Hz empty world/channel lifecycle

SERVER-0002 + CONTENT-0014
  -> SERVER-0004  load/own provisional graybox
  -> SERVER-0006  stable world-local identity + one generic technical player
  -> SIM-0008     authoritative click-to-move
~~~

SERVER-0006 deliberately does not depend on CONTENT-0003. SIM-0008 depends on domain state/content rather than Protocol.

Coordinator SHARED-0021 is the allocated bounded shared Box3D runtime prerequisite. SF-0009 Cycle 3 attached its canonical URI to SIM-0008, so the dependency is valid but waiting while SHARED-0021 remains todo. SIM-0008 must not activate or consume shared Box3D source until SHARED-0021 completes and SIM-0008 receives its own approved implementation plan.

### Connected walking world

~~~text
PROTOCOL-0002 -> SERVER-0003

SERVER-0006 + SIM-0008
  -> PROTOCOL-0003  connected-walking facts
  -> PROTOCOL-0004  deterministic serialization

parent SHARED-0023 + BUILD-0003 + BUILD-0005
  -> BUILD-0006  process-local shared transport composition

SERVER-0003 + SERVER-0006 + SIM-0008 + PROTOCOL-0004
  -> SERVER-0005

SERVER-0005 + PROTOCOL-0004 + CLIENT-0021 + BUILD-0006
  -> CLIENT-0009
~~~

CLIENT-0009 completes the second visible milestone: a signed loopback admission creates one world-owned player, left-click sends intent, World/Simulation decides movement and collision, and Client renders latest authoritative snapshots/corrections through CLIENT-0021's exact adapter.

The first exchange is deliberately bounded: channel-specific datagrams over the approved shared transport, plaintext literal loopback only, one-minute development tickets, no reconnect/resume, and immediate disconnect cleanup. Production key provisioning, protected non-loopback transport, smoothing, monsters and combat remain future task-owned work.

### Camps and connected monsters

~~~text
CONTENT-0007 -> SIM-0003
SIM-0003 + SERVER-0006 -> SIM-0006
CONTENT-0003 + SIM-0006 + SIM-0008 -> SIM-0004
SIM-0004 + SIM-0006 + SIM-0008 + CONTENT-0007 -> SIM-0010
SIM-0010 + SIM-0008 + CONTENT-0014 -> SIM-0011

CLIENT-0021 + CONTENT-0007 + SIM-0010
  -> CLIENT-0022  local placeholder monster fixtures

PROTOCOL-0004 + SIM-0006 + SIM-0010 + SIM-0011
  -> PROTOCOL-0005
SERVER-0005 + PROTOCOL-0005 + SIM-0010 + SIM-0011
  -> SERVER-0007
CLIENT-0009 + CLIENT-0022 + PROTOCOL-0005 + SERVER-0007
  -> CLIENT-0023  connected placeholder monsters

CLIENT-0023 + CONTENT-0013 + parent ASSET-0008
  -> CLIENT-0017  exact selected monster presentation
~~~

Placeholder monsters use generated shapes or separately approved temporary assets. Exact presentation remains evidence-gated.

CONTENT-0007 is the immutable domain input: light then heavy archetype order, 700/2,000 health units and the exact 3-light / 2-light-plus-2-heavy / 3-heavy assignment split across the ten graybox spawns. It does not own capacity, runtime entities or numeric behavior.

`SIM-0003` owns camp capacity, deterministic seed inputs and replenishment policy. `SIM-0006` owns runtime entities. `SIM-0010` owns the evidence-backed body/collision radius, movement speed, deterministic target selection/tie-break, awareness, pursuit/leash, attack range/damage/cadence and return behavior. `EDITOR-0005` may later author or visualize placement but does not own those authoritative values.

### Combat contract and exchange

~~~text
CONTENT-0003 + SIM-0006 + SIM-0008
  -> SIM-0004  Basic Arrow + integer damage/death

SIM-0004 + CONTENT-0003
  -> SIM-0009  Fire Arrow
  -> SIM-0007  Arrow Rain

SIM-0004 + SIM-0009 + SIM-0007 + SIM-0011
  -> PROTOCOL-0006  combat facts
PROTOCOL-0004 + PROTOCOL-0006
  -> PROTOCOL-0007  combat serialization

SERVER-0005 + SERVER-0007 + PROTOCOL-0007
  + SIM-0004 + SIM-0009 + SIM-0007 + SIM-0011
  -> SERVER-0008

CLIENT-0009 + CLIENT-0023 + PROTOCOL-0007 + SERVER-0008
  + SIM-0004 + SIM-0009 + SIM-0007
  -> CLIENT-0012
~~~

CONTENT-0003 supplies stable identities, ordered actions, integer health/damage and unlimited-ammunition semantics. SIM-0004 owns Basic Arrow's exact range/facing/cadence/interruption/tick inputs; SIM-0009 owns mana state/regeneration and Fire inputs; SIM-0007 owns Arrow Rain cost/range/radius/cadence/interruption/timing/order. EDITOR-0005 compares candidate values without promoting defaults.

No current task promotes Balance Lab evidence into one selected connected-M2 combat preset. Groom that focused ownership before SERVER-0008 activates. Primary-attribute taxonomy and starting values also remain an explicit nonblocking gap.

SERVER-0008 depends on the exact monster server exchange, never a Client task. Basic/Fire/Rain arrows remain presentation and all outcomes resolve at authoritative fixed ticks.

CLIENT-0007/0010/0011/0018/0019 retain focused locomotion, action, weapon, projectile, targeting and feedback presentation after these gates.

Durable catalog: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-archer-kit

### Progression, drops and equipment transport

~~~text
GAME-0002 + PROTOCOL-0007 -> PROTOCOL-0008
SERVER-0008 + PROTOCOL-0008 + GAME-0002 -> SERVER-0009
SERVER-0009 -> CLIENT-0015

GAME-0004 + PROTOCOL-0004 + PROTOCOL-0007 -> PROTOCOL-0009
SERVER-0008 + PROTOCOL-0009 + GAME-0004 -> SERVER-0010
SERVER-0010 -> CLIENT-0013

GAME-0003 + GAME-0005 + PROTOCOL-0009 -> PROTOCOL-0010
SERVER-0010 + PROTOCOL-0010 + GAME-0003 + GAME-0005 -> SERVER-0011
SERVER-0011 -> CLIENT-0014
~~~

Combat separates facts, serialization and World exchange because the action lifecycle is broad and reused by several consumers. The smaller feature extensions combine facts/serialization deliberately while retaining separate Server exchange ownership.

### Proper Editor-authored map

The proper editor path now has an explicit UI and interaction foundation without making auxiliary polish block runtime adoption:

~~~text
parent SHARED-0024
  -> EDITOR-0008  native Starfall UI foundation and synthetic showcase
  -> EDITOR-0009  interaction state and generic command history

EDITOR-0003 + EDITOR-0009
  + CONTENT-0006 + CONTENT-0014 + CONTENT-0012
  + parent SHARED-0018/0019 + ASSET-0007
  -> EDITOR-0007  real Draft 0 document, commands and compilation

EDITOR-0007 + SERVER-0004
  -> SERVER-0012  authoritative compiled map

CLIENT-0020 + EDITOR-0007
  + parent SHARED-0018/0019 + ASSET-0007
  -> CLIENT-0016  rendered compiled scene

EDITOR-0007
  -> EDITOR-0010  auxiliary Assets / Validation / Log / status polish
~~~

EDITOR-0008 depends only on the canonical coordinator SHARED-0024 task. Completed Starfall/Royale editor and presentation work is architectural evidence, not dependency wiring. The foundation owns the Starfall executable, proposed tokens/fonts/primitives and deterministic synthetic showcase; it does not consume CONTENT-0014.

EDITOR-0009 owns the shared selection/action-routing state, keyboard focus, transform-tool state, UI preference persistence and generic command history. Generic history executes, undoes, redoes and tracks dirty checkpoints; EDITOR-0007 owns concrete Draft 0 commands and mutation rules.

EDITOR-0007 remains the first real Draft 0 authoring document. It owns actual hierarchy concepts, synchronized hierarchy/viewport/inspector selection, picking, transforms, bounded inspectors, inline validation and separate authoritative/client outputs from one fully validated revision with stable cross-output identities. Synthetic showcase objects never establish content identity.

EDITOR-0010 depends only on EDITOR-0007, consumes the interaction-owned persistence machinery transitively and polishes the real Assets, Validation, Log and status adapters. It does not block SERVER-0012, CLIENT-0016 or first scene authoring.

Durable design language: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/development/editor-design-language

### Scheduling gate

Completion of `SHARED-0024` makes the native UI foundation technically available; it does not make editor work the next product priority. `EDITOR-0008` remains in M2 but is intentionally priority none while the generated Draft 0 graybox is sufficient.

Reconsider `EDITOR-0008` after the connected Basic Arrow loop is natively playable and owner-validated through `CLIENT-0007`, unless the owner explicitly reprioritizes it earlier. This is an evidence and scheduling gate rather than a source dependency: canonical `SHARED-0024` remains the task's only dependency.

### Numerical policy

Integer authoritative values: HP, mana, damage, XP, levels, currency, item counts, discrete stats and fixed ticks. Probabilities use explicit integer scales.

Spatial/physics authority: finite Box3D-native single-precision metres. Content authoring: BCL-only immutable System.Numerics-backed single-precision metre values. Simulation conversion is one-to-one with no integer-millimetre model. Stable identities and explicit ordering protect gameplay from unordered native queries.

## Balance Lab and later work

EDITOR-0003 establishes Editor/Balance Lab separation in M2. EDITOR-0004 through EDITOR-0006 retain headless deterministic scenario ownership.

CONTENT-0010 remains deferred outside Draft 0. First-wings/public release, trade, crafting, economy, persistence, PvP, multiple zones and final topology remain outside this walking-slice restructuring.

## Shared-source gate

Starfall never depends on Royale. Parent modules never depend on Starfall. Approved coordinator source remains rooted through ChronoFallFamilyRoot and requires an exact per-consumer allowlist.

The direct network boundary is narrow: Client and World may reference only the coordinator-owned LiteNetLib adapter project, which transitively supplies the transport contracts and pinned upstream implementation. Protocol, Content, Simulation, Editor and Balance Lab remain transport-free. The composition factories do not start listeners or define Starfall packet policy.

Character cooks remain client-only, ignored and stable-ID staged. Generated client content, raw supplied assets and presentation dependencies never enter World or other headless outputs.

Network adoption contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/network-transport-adoption