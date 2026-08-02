---
title: Product Design Direction
createdAt: 2026-08-01T05:48:09.1092000Z
modifiedAt: 2026-08-02T18:32:50.6991550Z
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

## Kickoff scope

M0 establishes repository and architecture boundaries. M1 integrates proven shared character presentation. M2 proves one connected technical vertical slice defined by the provisional Draft 0 brief: a protected-town zone, three camp geometries, a dark-elf archer with Basic Arrow, Fire Arrow, and Arrow Rain, bounded hostile behavior, deterministic progression and drops, truthful Ranger/leather equipment, and Balance Lab evidence from the same authoritative rules.

Draft 0 remains experimental rather than final content or balance. Authoritative spatial and physics state uses finite Box3D-native single-precision metres, while discrete resources use integer arithmetic and time uses fixed ticks. Stable identities and explicit ordering—not native query order—determine gameplay outcomes. Initial networking preserves finite IEEE-754 spatial values; quantization remains a future measured protocol decision. Client arrows, hovering, animation, effects, cameras, smoothing, and reconciliation present server outcomes but never decide them. Starfall selects game-specific characters, equipment, monsters, and zone composition; ChronoFall owns only genuinely reusable presentation/cooking contracts, supplied-source provenance, and stable-ID staging.

M3 records deferred content, authority, and presentation contracts for transformations, wings, mounts, and companions; it has no milestone priority and is outside the technical-slice critical path. First wings still conclude the distinct eventual first end-to-end public arc. Broader economy, stands, reputation/PvP, persistence, crafting, multiple zones, final service topology, prestige class, transformations, events, and territory remain later design inputs.

Draft 0: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/product/first-playable-zone-draft-0`.

Regional zone contract: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-zone-contract`.
