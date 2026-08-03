---
title: First Playable Zone — Draft 0
createdAt: 2026-08-02T15:54:34.7409020Z
modifiedAt: 2026-08-03T15:18:50.5342080Z
---

## Status and purpose

Draft 0 is Starfall M2's provisional technical vertical slice. Its layout, numbers, identities, timing and presentation choices are deterministic Content and Balance Lab inputs rather than final balance, final art or a public first-wings release.

Development deliberately grows through visible evidence instead of implementing an entire backend before a client can exercise it. Native presentation and authoritative simulation evolve separately, then converge through protocol contracts derived from proven behavior.

Trade stands, crafting, economy, persistence, PvP, multiple zones, wings, final service topology and general world/terrain/ability/AI frameworks remain outside Draft 0.

## Two visible milestones

### Local walking graybox

The first visible milestone requires:

- a generated provisional graybox;
- isometric camera and deterministic ground picking;
- the already-proven technical humanoid;
- left-click movement intent;
- a deterministic authoritative-style movement fixture;
- one Client-owned snapshot/fact-to-presentation adapter.

It requires no connection, selected final assets, World/Simulation dependency in Client, or client gameplay authority. CLIENT-0009 later maps real protocol snapshots into exactly the same adapter.

### Provisional camera and input contract

CLIENT-0005 establishes the perspective-isometric projection and deterministic ground-picking seam: a 28-degree vertical field of view, 42-degree downward pitch and 45-degree diagonal yaw from positive X/Z. SDL logical-window pointer coordinates are normalized before the camera uses the current drawable-pixel aspect ratio. Picking inverts the view-projection matrix, constructs a perspective ray, intersects the authoritative/content Y = 0 ground plane, and accepts only finite points inside caller-supplied ground bounds.

CLIENT-0020 consumes `Draft0GrayboxCatalog.FirstPlayable` and renders one deterministic 36-section, 870-vertex, 1,554-index generated presentation mesh through the approved shared static renderer. Section order preserves ground; south/north/west/east boundaries; town; exit and branch routes; camps; proxies; respawn/exit/junction/camp-entry anchors; and branch/local sample spawns. Exact source identities remain diagnostic section names.

Presentation-only layers prevent z-fighting without changing Content or authority: walkable ground stays at Y = 0, town/camps use Y = 0.01, routes use Y = 0.02, and markers begin at Y = 0.03. Boundary and proxy boxes begin at Y = 0; proxies retain their exact Content footprints and heights. Round route caps and the long-route corner are the deterministic union of one quad per centreline segment and one 16-wedge disc per centreline point.

The flat-colour diagnostic palette uses grass `(0.18,0.32,0.16)`, safe-town teal `(0.10,0.38,0.48)`, dirt `(0.50,0.32,0.14)`, easy/mixed/hard camp colours `(0.24,0.52,0.24)`, `(0.62,0.43,0.12)`, and `(0.58,0.18,0.16)`, plus role-specific boundary, proxy, anchor and spawn colours. These are disposable graybox diagnostics, not final materials or asset selection.

The native 1920 x 1080 preview exposes seven fixed views without creating a free-camera system: F1 player framing, F2 overview, F3 town, F4 junction, and F5-F7 easy/mixed/hard camps. Tab cycles; repeated key events are ignored. Number keys remain reserved for Fire Arrow and Arrow Rain. The window title and console identify the active view. F1 and the five local area views use a 1-to-300-metre clipping range. The whole-zone F2 overview uses 100-to-800 metres. These tightened presentation frusta preserve the approved 0.01/0.02-metre diagnostic layers with the D32 floating-point depth buffer; they do not change Content geometry or picking.

A valid left-button press creates only a `GroundMovementIntent` destination using the active camera and logs it. It does not move the humanoid, run collision, accept movement or mutate authoritative state. Right-click and skill keys remain unhandled until their focused tasks.

The technical humanoid remains at `(100,0,100)` only as the approved CLIENT-0005 close-framing fixture. It is not the gameplay spawn. CLIENT-0021 must decide whether its deterministic authoritative-style trace begins at the catalog respawn anchor `(100,0,25)`, and later CLIENT-0009 must reuse the resulting snapshot/fact-to-presentation adapter.

### Connected walking world

The second visible milestone requires:

- PROTOCOL-0002 admission feeding SERVER-0003 world-owned gameplay sessions;
- one 60 Hz world lifecycle and loaded provisional graybox;
- one generic world-owned technical player identity/state;
- authoritative movement;
- connected-walking facts, deterministic serialization and SERVER-0005 exchange;
- CLIENT-0009 reconciliation through the locally proven adapter.

Monsters extend the connected contract afterward and do not block connected player movement.

## Zone direction

The durable CONTENT-0006 requirements remain:

- approximately 200 x 200 metres;
- an approximately 50 x 50 metre protected town near one edge;
- a configured respawn anchor and two or three landmark intentions;
- one exit leading to a junction;
- short/easy, medium/mixed and long/hard branches around 25, 45 and 70 metres;
- broad/open, elongated/divided and constrained camp geometry;
- flat grass treatment, dirt route semantics and an outer rock/boulder boundary.

CONTENT-0014 turns only those requirements into provisional executable graybox coordinates, regions, proxy blocks, coarse collision/navigation and sample spawns. Generated planes, lines and boxes are sufficient; it does not author the complete map or depend on asset selection.

EDITOR-0007 later authors the proper scene from the durable requirements, graybox evidence and exact selected/staged assets. It compiles separate authoritative and client outputs for SERVER-0012 and CLIENT-0016.

## Provisional class and combat kit

The connected walking slice uses a generic technical player and does not depend on class or combat content. The later class/combat lane owns the dark-elf archer through CONTENT-0003.

The intended class begins in a non-equipment underlayer with a basic wooden bow, visually presented unlimited arrows and no equipped armour. The first armour family is a visibly meaningful Ranger/leather set.

| Action | Target | Displayed damage | Internal damage |
| --- | --- | ---: | ---: |
| Basic Arrow | One enemy | 3 | 300 |
| Fire Arrow | Selected enemy | 7 | 700 |
| Arrow Rain | Ground circle, each valid victim | 5 | 500 |

Right-click enemy requests Basic Arrow, 1 requests Fire Arrow on the selected target, and 2 enters Arrow Rain ground targeting. Input is intent only. The server decides validity, range, facing, victims, damage, mana, cadence, death and success.

## Numerical contract

Integer authoritative state includes HP, mana, damage, XP, levels, currency, item counts and discrete stats. One displayed HP or mana point equals 100 internal units; the Draft 0 player starts with 2,500 health units. Probabilities use explicit integer scaling and authoritative time uses fixed integer ticks.

Authoritative spatial/physics state uses finite Box3D-native single-precision metres. Starfall.Content uses BCL-only immutable System.Numerics-backed authoring values with the same units and precision, rejecting NaN, infinity and out-of-zone data. Simulation converts components one-to-one and does not maintain a parallel integer-millimetre coordinate model. Stable identity and explicit ordering isolate gameplay outcomes from native query order.

Level 2 requires 40 XP. Later requirements use nearest-integer half-up arithmetic:

next = (previous * 115 + 50) / 100

Accepted level 2-20 requirements:

40, 46, 53, 61, 70, 81, 93, 107, 123, 141, 162, 186, 214, 246, 283, 325, 374, 430, 495

## Authority and arrow presentation

Basic Arrow and Fire Arrow resolve at explicit authoritative fixed ticks without spatial projectile entities or server-side travel/collision. Arrow Rain resolves an explicitly ordered victim set and damage at an authoritative tick.

Protocol facts later carry action, target, timing, resource and outcome information. Client-rendered arrows, flight, falling arrows, impacts and effects never decide collision, victims, damage, mana or success.

## Monsters, camps, town and respawn

Prototype identities remain:

- starter_flyer_light: 700 internal HP;
- starter_flyer_heavy: 2,000 internal HP.

They remain ground-plane authoritative entities. Hovering is presentation. The smallest authoritative behavior is camp-bounded awareness, pursuit, attack, disengage and return. The town rejects hostile actions, excludes monsters and owns the configured defeat/respawn anchor.

The local placeholder-monster task uses generated shapes or separately approved temporary assets and deterministic fixtures. A focused protocol/server/client extension later connects real monster snapshots. Exact selected monster assets remain a separate CONTENT-0013 and coordinator ASSET-0008 path.

## Asset ownership and source direction

Starfall owns selection of its dark elf, underlayer/Ranger pieces, bow/arrows, monsters and proper zone composition. ChronoFall owns supplied-source provenance, reusable rendering/cooking contracts and stable-ID staging. Selection records exact pack-relative paths; acquisition stages only exact approved inputs; generated client content remains ignored.

Established supplied Quaternius sources include Universal Base Characters[Standard], Universal Animation Library[Standard], Universal Animation Library 2[Standard], Modular Character Outfits - Fantasy[Standard], Medieval Village MegaKit[Standard], and Medieval Weapons Pack by @Quaternius.

Quaternius remains selected for humanoids, the reference skeleton, animation, armour/clothing and the initial village source. Kenney and Quaternius weapons remain candidates. The existing UAL1 cook is historical/technical evidence; Sword_Attack is not a bow placeholder.

Prospective packs remain non-dependencies until supplied and inspected: Universal Animation Library 2 Full, Modular Sci-Fi MegaKit, Ultimate Monsters, Stylized Nature MegaKit, Fantasy Props MegaKit and Ultimate RPG Pack. No whole pack enters a runtime cook or manifest.

## Balance Lab evidence

Balance Lab eventually exercises the same authoritative content/rules: movement, three camp geometries, three actions, deterministic timing/order/rewards, monster pursuit/attack/return, protected-town disengagement, damage/death/respawn, progression, drops and equipment.

Tuning inputs remain configurable: mana/regeneration/costs, cadence/range/interruption, Arrow Rain radius, monster behavior/damage, drops/modifiers, level gains, respawn resources, visual projectile timing and pacing.

## Shared attachment continuity

The narrow coordinator socketed-bow proof remains distinct from broader deferred SHARED-0007 attachment presentation. Later broad work must review and reuse the narrow proof.

Durable identity: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/product/first-playable-zone-draft-0.
