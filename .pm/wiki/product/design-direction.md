---
title: Product Design Direction
createdAt: 2026-08-01T05:48:09.1092000Z
modifiedAt: 2026-08-07T19:32:01.9362020Z
---

## Approved direction

Starfall is a spiritual successor, not an exact MU remake. It targets comfortable sustained combat, learned hunting geometry, visible earned power, physical world drops, free trade, meaningful risk, and long-term transformations that cosmetics cannot counterfeit.

The coordinator design document remains the current full product-design source of truth. This child page records only implementation-facing invariants:

- skills change the geometry of efficient play;
- equipment and progression remain visibly truthful;
- items belong to the world before a build;
- classes remain asymmetric and hybrids viable;
- the shared world makes other players structurally relevant;
- server authority is never delegated to animation or client presentation;
- first wings conclude the eventual first end-to-end public arc.

## Deliverable roadmap

M0 and M1 preserve completed foundation history. M2 remains a completed legacy planning bucket whose contents proved the graybox, connected walking, camps, bounded monsters, Basic simulation and player-life simulation. M2 is not renamed or reused; unfinished work has moved out.

New milestones describe independently demonstrable outcomes rather than project phases:

- M4 Development Instrumentation proves the Starfall debug shell, one development-command envelope/dispatcher and `ping` through the development console.
- M5 Connected Basic Arrow proves connected intent through authoritative outcome, bow-body animation, one equipment-free rendered bow and visual arrow, hit feedback, monster damage/death, Combat diagnostics and a native end-to-end run.
- M6 Authoritative Mana proves integer Mana configuration, simulation, serialization, exchange, development diagnostics and regeneration independently of spells.
- M7 Connected Movement Quality v1 proves bounded remote interpolation and local correction diagnostics under deterministic network fixtures.

Fire Arrow and Arrow Rain later consume Basic and Mana without depending on a permanent HUD. Progression, Resource HUD, Player Life integration, Inventory, Equipment, Physical Drops, Ranger presentation, proper Editor scene work and Balance Lab scenarios remain milestone-free planning handles until the owner activates each concrete deliverable.

Draft 0 remains experimental rather than final content or balance. Authoritative spatial and physics state uses finite Box3D-native single-precision metres, while discrete resources use integer arithmetic and time uses fixed ticks. Stable identities and explicit ordering—not native query order—determine gameplay outcomes. Initial networking preserves finite IEEE-754 spatial values; quantization remains a future measured protocol decision. Client arrows, hovering, animation, effects, cameras and presentation smoothing never decide authoritative outcomes.

Starfall selects game-specific characters, equipment, monsters and zone composition. ChronoFall owns only genuinely reusable presentation/cooking contracts, supplied-source provenance and stable-ID staging. The first Basic bow proof is deliberately independent of Inventory, Equipment, starter loadouts and Ranger content.

Transformations, wings, mounts and companions remain milestone-free roadmap inputs until the owner activates a concrete deliverable. No empty or initiative-shaped milestone represents them. First wings still conclude the distinct eventual public arc. Economy, stands, reputation/PvP, persistence, crafting, multiple zones, final service topology, prestige class, transformations, events and territory remain later design inputs.

## Activation and dependency rule

Milestone triggers represent accepted capability prerequisites once. Tasks order implementation inside one deliverable or deliberately coordinate overlapping milestones; they do not repeat Simulation-to-Simulation, Protocol-to-Protocol, World-to-World, and Client-to-Client dependency fans merely because a downstream feature consumes delivered seams.

A task contract names the exact API, adapter, dispatcher, codec, or presentation seam it consumes. Cross-milestone task dependencies remain only when milestones intentionally overlap and one task must wait, when a trigger does not guarantee the needed capability, or when canonical cross-project ownership must remain explicit.

Future Fire Arrow, Arrow Rain, permanent HUD, Player Life, Progression, Inventory, Equipment, Physical Drops, Editor, and Pressure Cooker grooming must apply this rule rather than recreate the superseded fan-out.

Draft 0: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/product/first-playable-zone-draft-0`.

Regional zone contract: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-zone-contract`.
