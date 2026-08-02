---
title: First Playable Zone — Draft 0
createdAt: 2026-08-02T15:54:34.7409020Z
modifiedAt: 2026-08-02T15:54:34.7409020Z
---

## Status and purpose

Draft 0 is the provisional technical vertical slice for Starfall M2. Its numbers, distances, content identities, action timing, and presentation choices are deterministic Content and Balance Lab inputs, not final balance or final art.

It proves one connected, server-authoritative hunting loop. It does not pull the first-wings public release, trade stands, crafting, economy, persistence, PvP, multiple zones, final service topology, or a general world/terrain/ability/AI framework into M2.

## Zone brief

The map is approximately 200 x 200 metres. An approximately 50 x 50 metre protected town sits near one edge and contains a configured respawn anchor and two or three landmark buildings. The buildings may read as an inn, workshop, and market or storage area, but they do not imply NPC, crafting, commerce, storage, or interaction systems.

One clear exit reaches a junction with three experimental branches:

| Branch | Approximate travel | Camp geometry | Composition |
| --- | ---: | --- | --- |
| Short | 25 m | Broad, open circle | starter_flyer_light |
| Medium | 45 m | Elongated or divided | Both starter flyers |
| Long | 70 m | Tight bowl or constrained lanes | Emphasizes starter_flyer_heavy |

Flat terrain is acceptable. Grass and dirt-path surfaces, rocks/boulders, sparse nature props, and landmark buildings make routes and spaces readable. Collision, navigation, camp shapes, and respawn anchors are deterministic gameplay/authoring inputs; visual meshes never replace them. This slice does not require streaming, terrain deformation, a biome system, or a general world format.

## Provisional class and combat kit

The first class fantasy is a dark-elf archer. The character begins in an appropriate non-equipment underlayer with a basic wooden bow, visually presented arrows, and no equipped armour. Arrows are unlimited for Draft 0; there is no ammunition inventory or purchasing. The first armour family is a visibly meaningful Ranger/leather set that may be earned piece by piece.

| Action | Target | Displayed damage | Internal damage |
| --- | --- | ---: | ---: |
| Basic Arrow | One enemy | 3 | 300 |
| Fire Arrow | Selected enemy | 7 | 700 |
| Arrow Rain | Ground circle, each valid victim | 5 | 500 |

The provisional control hypothesis is left-click ground to move, right-click a valid enemy to select it and request Basic Arrow, 1 to request Fire Arrow on the selected target, and 2 then right-click a valid ground point to request Arrow Rain. Escape or an approved empty-ground action cancels targeting.

Input is intent only. The server decides range, facing requirements, valid targets and victims, damage, mana, death, cadence, and success.

## Deterministic numerical contracts

One displayed health or mana point equals 100 authoritative internal units. The player begins with 2,500 health units (25 displayed HP). Primary attributes use ordinary integers, probabilities use an explicit integer representation such as basis points, and authoritative time uses fixed simulation ticks. Authoritative gameplay state does not use floating point merely because presentation does.

Level 2 requires 40 XP. Each later requirement uses nearest-integer half-up arithmetic:

`next = (previous * 115 + 50) / 100`

The accepted Draft 0 level 2-20 sequence is:

`40, 46, 53, 61, 70, 81, 93, 107, 123, 141, 162, 186, 214, 246, 283, 325, 374, 430, 495`

Reward selection is deterministic under an authoritative seed. starter_flyer_light awards 1-3 XP and starter_flyer_heavy awards 2-8 XP.

## Authority and arrow presentation

Basic Arrow and Fire Arrow do not create authoritative spatial projectile entities and do not perform server-side projectile collision or travel simulation. The server validates an action and resolves its outcome at an explicit deterministic fixed tick. Protocol facts carry enough action, target, timing, resource, and outcome information for coherent client release, flight, and impact.

Arrow Rain resolves its authoritative victim set, deterministic victim order, and damage at an explicit fixed tick. Falling arrows and impacts are presentation. Client-rendered arrows never decide collision, victims, damage, mana, or success.

Exact windup, resolve timing, visual trajectory, reconciliation, attack interruption, cadence, ranges, mana/regeneration/costs, and Arrow Rain radius remain focused Balance Lab or presentation inputs.

## Monsters, camps, town, and respawn

The two neutral prototype identities are:

- `starter_flyer_light`: 700 internal HP (7 displayed);
- `starter_flyer_heavy`: 2,000 internal HP (20 displayed).

The intended breakpoints are three Basic Arrows, one Fire Arrow, or two Arrow Rain hits for the light flyer; and seven Basic Arrows, three Fire Arrows, or four Arrow Rain hits for the heavy flyer.

Both remain ordinary ground-plane authoritative entities with positions, radii, movement, targeting, and camp bounds. Hovering is presentation only. The smallest viable authoritative behavior is camp-bounded awareness, approach/pursuit, attack, disengage, and return. The client may use yaw, bobbing, lunging or pulsing, hit flash, and simple death presentation. Draft 0 does not require altitude, flight navigation, vertical combat, locomotion cycles, foot placement, IK, retargeting, or a generic monster skeletal pipeline.

The town is protected: hostile player actions are rejected within it, monsters cannot enter it and disengage at its boundary, and defeated players return to the configured town respawn anchor. Respawn delay and restored health/mana remain configurable Balance Lab inputs.

## Asset ownership and source direction

Starfall owns the identity and selection of its dark elf, underlayer and Ranger pieces, bow and arrows, monsters, and zone composition. Every selection records exact pack-relative source paths. ChronoFall owns supplied-source provenance, reusable rendering/cooking contracts, and stable-ID staging. Coordinator acquisition tasks consume completed selections and stage only exact approved inputs. Generated client content remains ignored. No entire asset pack enters a runtime manifest or cook.

Currently supplied and established Quaternius sources include:

- `Universal Base Characters[Standard]`;
- `Universal Animation Library[Standard]`;
- `Universal Animation Library 2[Standard]`;
- `Modular Character Outfits - Fantasy[Standard]`;
- `Medieval Village MegaKit[Standard]`;
- `Medieval Weapons Pack by @Quaternius`.

Quaternius remains selected for humanoid characters, the reference skeleton, animations, modular armour/clothing, and the initial village source. Kenney and Quaternius weapons may both be evaluated by task-owned selection. The existing UAL1 cook remains valid historical and technical evidence; `Sword_Attack` is not an acceptable bow placeholder.

The following official CC0 packs are prospective inputs only until their exact supplied files, hashes, licence evidence, formats, scale, materials, rigs, and compatibility are inspected:

- Universal Animation Library 2 Full: likely bow-animation evidence including BOW_NOTCH, BOW_SHOOT, and bounded aim clips; only a minimum approved clip set may be cooked.
- [Modular Sci-Fi MegaKit](https://quaternius.com/packs/modularscifimegakit.html): preferred first inspection for temporary small alien, grub, or hovering starter-flyer candidates.
- [Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html): fallback monster source if the sci-fi candidates are unsuitable.
- [Stylized Nature MegaKit](https://quaternius.com/packs/stylizednaturemegakit.html): preferred focused nature candidate, initially perhaps two or three rocks, one or two trees, one bush, and one or two grass/flower clumps.
- Fantasy Props MegaKit: optional exact landmark dressing such as a few inn, workshop, or market/storage props.
- Ultimate RPG Pack: deferred, unselected pickup-like candidate only if a later physical-item task proves an exact unmet need.

Prospective packs are not dependencies. The map graybox must not depend on a purchase. Engine-specific vegetation/wind shaders are not assumed portable; Draft 0 uses supported glTF material inputs or a deliberately simple shared material path. Grass and dirt-path ground treatments remain separate experimental inputs.

The monster selection may reject every candidate. It first determines whether each selected representation is static, rigidly animated, or skeletal. Only then may reviewed follow-up attach the smallest correct coordinator acquisition and rendering/cooking prerequisites; it must not defensively require both static and skeletal paths.

## Balance Lab evidence

The Balance Lab must exercise the same content and authoritative rules as the world. Scenarios cover all three camp geometries, all three actions, fixed-tick outcome timing, deterministic victim ordering and rewards, monster pursuit/attack/return, protected-town disengagement, player damage/death/respawn, levels 1-20, drops, and visible equipment progression.

Still-configurable inputs include player mana and regeneration, skill costs, action cadence and range, action interruption, Arrow Rain radius, monster damage/cadence/movement/aggro, drop tables, item modifiers, level-up gains, respawn delay, restored resources, projectile visual timing, and overall pacing.

## Shared attachment continuity

Draft 0 requires a narrow coordinator proof of one rendered socketed static attachment, initially the selected bow. The broader deferred coordinator task `SHARED-0007` remains the owner of general attachment presentation for later weapons, shields, backpacks, wings, and other categories. When implemented, it must review and reuse the narrow bow proof rather than independently recreating the capability. Existing deferred consumers of `SHARED-0007` remain intact.

## Durable identity

This brief is `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/product/first-playable-zone-draft-0`. It is provisional product input. Task files own executable acceptance boundaries and canonical cross-project dependency wiring.