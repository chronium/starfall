---
title: Bootstrap Roadmap
createdAt: 2026-08-01T05:48:09.1150770Z
modifiedAt: 2026-08-06T07:12:01.0469360Z
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

M2 is a completed historical planning bucket. It preserves evidence from the graybox, walking, connected monsters, camp lifecycle, Basic simulation and player-life simulation, but it is not a deliverable-shaped milestone and receives no new work. All unfinished tasks have moved out without rewriting completed history.

Draft 0 now advances through independently demonstrable deliverable milestones. A milestone may depend on earlier work, but it must close with its own observable outcome. Native-presentation and authoritative-simulation lanes converge only after their concepts exist.

### Content ownership

- CONTENT-0006: completed durable zone/layout requirements.
- CONTENT-0014: provisional executable graybox coordinates, regions, proxies, coarse collision/navigation and neutral sample spawns; depends explicitly on CONTENT-0006.
- CONTENT-0003: completed dark-elf archer and ordered three-action catalog; it does not gate the generic technical walking player.
- CONTENT-0007: exact provisional starter-flyer identities, integer health and ten ordered graybox spawn assignments. It consumes CONTENT-0014 but owns no runtime spawning or behavior values.
- CONTENT-0008: deferred XP-curve and reward inputs only; it no longer owns equipment, Ranger presentation, bows or physical-drop tables.
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

### Deferred pointer and movement-intent feedback

These tasks are deliberately separate from the Basic Arrow end-to-end proof and currently have no milestone with explicit priority none:

```text
CONTENT-0015  exact Kenney Cursor/Crosshair selection only
  ├── CLIENT-0025  semantic movement / prohibited / hostile-target cursors
  └── CLIENT-0026  issued movement-target ground marker
```

CLIENT-0025 also waits on the established ground/graybox picking and connected Basic Arrow target-selection path. CLIENT-0026 consumes the completed connected movement command/acknowledgement path. Neither task changes World authority.

CONTENT-0015 may inspect the external Kenney All-in-One v3.6.0 compilation but cannot copy or cook it. If exact inputs are approved, a later focused coordinator acquisition task stages only those files with provenance. Its canonical dependency must be attached to the Client tasks before either activates. No coordinator ID is invented during this Starfall cycle.

The initial marker path is one alpha-blended textured ground quad with presentation-only depth separation. Decals, path previews, navigation, generic interactions and a general effects system remain outside these tasks.

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

CLIENT-0021 + CONTENT-0007
  -> CLIENT-0022  visible local placeholder monster fixtures

PROTOCOL-0004 + SIM-0006 + SIM-0010 + SIM-0011
  -> PROTOCOL-0005
SERVER-0005 + PROTOCOL-0005 + SIM-0010 + SIM-0011
  -> SERVER-0007
CLIENT-0009 + CLIENT-0022 + PROTOCOL-0005 + SERVER-0007
  -> CLIENT-0023  connected placeholder monsters

CLIENT-0023 + CONTENT-0013 + parent ASSET-0008
  -> CLIENT-0017  exact selected monster presentation
~~~

CLIENT-0022 deliberately preceded authoritative behavior: generated shapes first proved visible presence, stable identity, archetype distinction and client-only hover at the exact ten placements. Completed SIM-0010 then added bounded movement/targeting/attack requests, and completed SIM-0011 applies those requests to player health, protected-town lockout, defeat and exact respawn while preserving entity/session identity.

PROTOCOL-0005 defines the bounded full-state monster snapshot seam without inventing behavior: ordered live entries carry authoritative transform/behavior/target/health, while bounded defeat tombstones repeat until the corresponding placement slot replenishes. SERVER-0007 now maps and exchanges those facts per admitted session on sequenced channel 4 from the same gameplay host. CLIENT-0023 now retains the latest valid stream and renders ordered live/tombstone facts through the existing placeholder adapter; connected mode no longer uses local monster fixtures. Durable contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/protocol/bounded-monster-snapshots

EDITOR-0005 remains the later owner for comparing monster, sustain and respawn inputs. The selected 180-tick/full-health respawn is provisional evidence. Mana is independently owned by M6 through CONTENT-0016, SIM-0012, PROTOCOL-0014, SERVER-0016 and CLIENT-0032; a later Player Life integration decides how respawn affects Mana. Exact source-asset presentation remains evidence-gated to CONTENT-0013, parent ASSET-0008 and CLIENT-0017.

### Combat contract and exchange

The first connected combat proof follows the already-completed Basic Arrow, player-life and connected-monster behavior instead of waiting for every Draft 0 skill. Its protocol and exchange carry Basic Arrow only; SIM-0011 remains a real prerequisite for defeated-player and protected-town rejection, not player-life transport ownership:

~~~text
CONTENT-0003 + SIM-0006 + SIM-0008
  -> SIM-0004  Basic Arrow + integer monster damage/death
SIM-0010 + SIM-0008 + CONTENT-0014
  -> SIM-0011  player damage/defeat/protected-town respawn

SIM-0004 + SIM-0011
  -> PROTOCOL-0006  first connected-combat facts
PROTOCOL-0004 + PROTOCOL-0006
  -> PROTOCOL-0007  deterministic serialization

SERVER-0005 + SERVER-0007 + PROTOCOL-0007
  + SIM-0004 + SIM-0011
  -> SERVER-0008  first connected-combat exchange

CLIENT-0009 + CLIENT-0023 + PROTOCOL-0007 + SERVER-0008
  -> CLIENT-0012  right-click Basic Arrow intent
~~~

PROTOCOL-0006/0007, SERVER-0008 and CLIENT-0012 have explicit high priority. They form the first dependency-ready end-to-end path: the admitted session supplies the actor, right-click selects one live connected monster, World validates and resolves Basic Arrow at fixed ticks, and the existing connected monster stream presents health loss, hit flash and defeat. Three accepted resolved hits defeat `starter_flyer_light`; seven defeat `starter_flyer_heavy`.

Mana is an independent prerequisite rather than Fire or Rain-owned state:

~~~text
CONTENT-0003
  -> CONTENT-0016  provisional Mana inputs
  -> SIM-0012      authoritative Mana behavior
  -> PROTOCOL-0014 Mana facts + serialization

SERVER-0005 + SIM-0012 + PROTOCOL-0014 + SERVER-0015
  -> SERVER-0016  authoritative Mana exchange
CLIENT-0031 + PROTOCOL-0014 + SERVER-0016
  -> CLIENT-0032  Resource diagnostics and native Mana proof
~~~

Fire Arrow and Arrow Rain remain separately deferred consumers:

~~~text
SIM-0004 + CONTENT-0003 + SIM-0012
  -> SIM-0009  Fire-specific behavior
PROTOCOL-0007 + PROTOCOL-0014 + SIM-0009
  -> PROTOCOL-0011  Fire facts + serialization
SERVER-0008 + SERVER-0016 + PROTOCOL-0011 + SIM-0009
  -> SERVER-0013  Fire exchange
CLIENT-0012 + PROTOCOL-0011 + SERVER-0013
  -> CLIENT-0027  key-1 Fire intent

SIM-0004 + CONTENT-0003 + SIM-0012 + SIM-0009
  -> SIM-0007  Rain-specific behavior
PROTOCOL-0007 + SIM-0007
  -> PROTOCOL-0012  Rain facts + serialization
SERVER-0008 + SERVER-0016 + PROTOCOL-0012 + SIM-0007
  -> SERVER-0014  Rain exchange
CLIENT-0012 + PROTOCOL-0012 + SERVER-0014
  -> CLIENT-0028  key-2 ground-target intent
~~~

CONTENT-0003 supplies stable identities, ordered actions, integer health/damage and unlimited-ammunition semantics. SIM-0004 owns Basic Arrow; SIM-0012 owns Mana; SIM-0009 owns only Fire-specific cost/range/facing/cadence/interruption/timing; SIM-0007 owns only Rain-specific cost/target/radius/cadence/interruption/timing/order.

For M5 presentation, CLIENT-0007 owns Basic bow-body animation, CLIENT-0011 owns one provisional semantic hand socket and rendered bow, CLIENT-0018 owns the Basic-only visual arrow and impact, and CLIENT-0019 owns Combat diagnostics plus terminal native Basic validation. Fire presentation is allocated only when Fire activates. CLIENT-0010 remains the later terminal Rain targeting/effects task. CLIENT-0025/0026 remain separately deferred cursor and movement-marker work.

No authoritative spatial projectile exists. Basic/Fire arrows and Rain effects remain presentation; World decides action validity, timing, resource expenditure, victims, damage and defeat.

Durable catalog: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-archer-kit

### Deferred progression, inventory, equipment and drops

These are valid later deliverables, not one transport chain and not M2 work.

- Progression owns XP/level behavior and later permanent progression feedback. It does not own equipment or physical drops.
- Inventory is proven first with exact provisional development items and one fixed-slot player inventory.
- Equipment is a later sibling consumer of completed Inventory and initially proves state changes only, without stats or character rendering.
- Physical Drops is another sibling consumer of Inventory and later proves kill, drop, collect and visible inventory insertion.
- Development injection may validate Inventory, but the domain, protocol, exchange and permanent GUI do not depend architecturally on the console.
- Ranger mapping follows Inventory, Equipment and exact asset selection. The first Basic bow remains equipment-free.

All current future tasks in these lanes are milestone-free with `priority: none`. Their focused milestone/task graphs are allocated only when the owner activates the deliverable.

### Proper Editor-authored map

EDITOR-0007 through EDITOR-0010, SERVER-0012, CLIENT-0016 and their selection work are individually deferred, milestone-free and `priority: none`. They are not pre-grouped into a deliverable; each future planning pass must use actual authoring needs rather than this historical sketch.

The proper editor path retains an explicit UI and interaction foundation without making auxiliary polish block runtime adoption:

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

Completion of `SHARED-0024` makes the native UI foundation technically available; it does not make editor work the next product priority. `EDITOR-0008` is milestone-free with `priority: none` while the generated Draft 0 graybox is sufficient.

Reconsider `EDITOR-0008` after the connected Basic Arrow loop is natively playable and owner-validated through `CLIENT-0007`, unless the owner explicitly reprioritizes it earlier. This is an evidence and scheduling gate rather than a source dependency: canonical `SHARED-0024` remains the task's only dependency.

### Numerical policy

Integer authoritative values: HP, mana, damage, XP, levels, currency, item counts, discrete stats and fixed ticks. Probabilities use explicit integer scales.

Spatial/physics authority: finite Box3D-native single-precision metres. Content authoring: BCL-only immutable System.Numerics-backed single-precision metre values. Simulation conversion is one-to-one with no integer-millimetre model. Stable identities and explicit ordering protect gameplay from unordered native queries.

## Deliverable milestones after M2

### M4 — Development Instrumentation

M4 proves one shared ImGui-backed Starfall debug shell, one development-only command envelope and dispatcher, and one harmless Ping World available from typed UI and console frontends.

Durable roadmap: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/roadmap/development-instrumentation

### M5 — Connected Basic Arrow

M5 converges the already-proven Basic simulation with connected intent/outcomes, bow-body animation, one provisional socketed bow, one client-only visual arrow and impact, exact Combat diagnostics, connected monster damage/death and native owner validation. It excludes Mana, Fire Arrow, Arrow Rain, equipment, Ranger loadouts, player respawn presentation and permanent combat UI.

### M6 — Authoritative Mana

M6 establishes integer Mana end to end before any spell owns it: content inputs, authoritative consumption/regeneration, facts/serialization, World exchange, feature-owned development commands, Resource diagnostics and native proof.

Durable roadmap: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/roadmap/authoritative-mana

### M7 — Connected Movement Quality v1

M7 independently proves buffered remote interpolation and local correction diagnostics under deterministic network fixtures. Prediction and reconciliation remain evidence-gated for a later version.

Durable roadmap: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/roadmap/connected-movement-quality-v1

## Balance Lab and later work

EDITOR-0003 preserves the completed Editor/Balance Lab separation as M2 history. EDITOR-0004 through EDITOR-0006 remain individually milestone-free, priority-none placeholders. The harness is planned immediately before its first real scenario; the previous combined combat/progression/drop/equipment scope must be split from actual evidence at activation.

CONTENT-0010 remains deferred outside Draft 0. First-wings/public release, trade, crafting, economy, persistence, PvP, multiple zones and final topology remain outside this walking-slice restructuring.

## Shared-source gate

Starfall never depends on Royale. Parent modules never depend on Starfall. Approved coordinator source remains rooted through ChronoFallFamilyRoot and requires an exact per-consumer allowlist.

The direct network boundary is narrow: Client and World may reference only the coordinator-owned LiteNetLib adapter project, which transitively supplies the transport contracts and pinned upstream implementation. Protocol, Content, Simulation, Editor and Balance Lab remain transport-free. The composition factories do not start listeners or define Starfall packet policy.

Character cooks remain client-only, ignored and stable-ID staged. Generated client content, raw supplied assets and presentation dependencies never enter World or other headless outputs.

Network adoption contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/network-transport-adoption