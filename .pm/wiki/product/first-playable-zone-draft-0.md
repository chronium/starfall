---
title: First Playable Zone — Draft 0
createdAt: 2026-08-02T15:54:34.7409020Z
modifiedAt: 2026-08-05T15:03:57.5229490Z
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

The local milestone itself requires no connection, selected final assets, World/Simulation dependency in Client, or client gameplay authority. CLIENT-0009 now maps real protocol snapshots into exactly the same adapter for the completed connected walking milestone.

### Provisional camera, input and local presentation contract

CLIENT-0005 establishes the perspective-isometric projection and deterministic ground-picking seam: a 28-degree vertical field of view, 42-degree downward pitch and 45-degree diagonal yaw from positive X/Z. SDL logical-window pointer coordinates are normalized before the camera uses the current drawable-pixel aspect ratio. Picking inverts the view-projection matrix, constructs a perspective ray, intersects the authoritative/content Y = 0 ground plane, and accepts only finite points inside caller-supplied ground bounds.

CLIENT-0020 consumes `Draft0GrayboxCatalog.FirstPlayable` and renders one deterministic 36-section, 870-vertex, 1,554-index generated presentation mesh through the approved shared static renderer. Section order preserves ground; south/north/west/east boundaries; town; exit and branch routes; camps; proxies; respawn/exit/junction/camp-entry anchors; and branch/local sample spawns. Exact source identities remain diagnostic section names.

Presentation-only layers prevent z-fighting without changing Content or authority: walkable ground stays at Y = 0, town/camps use Y = 0.01, routes use Y = 0.02, and markers begin at Y = 0.03. Boundary and proxy boxes begin at Y = 0; proxies retain their exact Content footprints and heights. Round route caps and the long-route corner are the deterministic union of one quad per centreline segment and one 16-wedge disc per centreline point. The disposable flat-colour palette distinguishes ground, town, routes, camps, boundaries, proxies, anchors and spawns without selecting final materials or assets.

CLIENT-0021 completes the local walking milestone without adding gameplay authority. `local_technical_player` begins at the catalog respawn anchor `(100,0,25)`, facing `+Z`. Left-click remains an intent; a deterministic Client-local stand-in consumes the newest destination on later 60 Hz ticks and feeds one stateless snapshot/fact-to-presentation adapter. It moves directly without collision, navigation, pathfinding, town enforcement or gameplay acceptance.

The provisional speed is stored as integer tenths: `40` means 4.0 m/s and the session-local range is `1` through `120`. Numpad `+`/`-` change exactly one tenth and ignore repeat events. At the default speed one tick advances at most 1/15 metre before destination clamping.

The renderer consumes only the latest completed snapshot. It deliberately performs no position/facing interpolation, smoothing, prediction or reconciliation; visible higher-refresh stepping is evidence for a later focused presentation task rather than scope for this adapter. The technical cook remains unchanged and Starfall blends its existing `Idle_Loop` and `Walk_Loop` over 0.25 seconds as presentation. `Walk_Loop` uses the presentation-only square-root cadence `sqrt(planar speed / 1.0 m/s)`, giving 1x at 1.0 m/s and 2x at the 4.0 m/s default without changing authoritative movement. It reduces obvious sliding while high-speed walking remains evidence for a later locomotion-band task.

The native 1920 x 1080 preview retains seven bounded views without creating a free-camera system. F1 directly follows the latest presented position; Up/Down adjust only its session-local distance by 0.5 metre from 10.0 through 60.0 metres, beginning at 22.5 metres. F2 is the fixed overview, F3 the fixed town view, F4 the fixed junction view and F5-F7 the fixed easy/mixed/hard camp views; those diagnostics ignore Up/Down. Tab cycles and repeated key events are ignored. Number keys remain reserved for Fire Arrow and Arrow Rain. F1 and the five local area views retain 1-to-300-metre clipping; F2 retains 100-to-800 metres. The title and console expose view, speed and camera distance.

CLIENT-0024 remains deterministic historical graybox evidence. Its seven-view capture recipe explicitly supplies the idle `(100,0,100)` CLIENT-0005 fixture, frozen 22.5-metre F1 distance and `Idle_Loop` sample at 0.500 seconds through the same adapter/render path. The capture contract remains at `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/development/draft-0-graybox-capture-suite`.

Durable adapter contract: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/client-world-presentation-adapter`.

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

The connected walking slice uses a generic technical player and does not depend on class or combat content. The later class/combat lane now consumes CONTENT-0003's immutable Draft 0 catalog.

The provisional class identity is `dark_elf_archer`. It begins with 2,500 authoritative health units, unlimited authoritative ammunition, and the exact ordered actions `basic_arrow`, `fire_arrow`, `arrow_rain`. Unlimited ammunition means no ammunition resource, inventory or purchasing; it does not prohibit visual arrows.

| Action | Target | Displayed damage | Internal damage | Mana |
| --- | --- | ---: | ---: | --- |
| Basic Arrow | One selected enemy | 3 | 300 | no |
| Fire Arrow | One selected enemy | 7 | 700 | yes |
| Arrow Rain | Ground circle, each valid victim | 5 | 500 | yes |

The intended presentation still begins in a non-equipment underlayer with a basic wooden bow and visually presented arrows. The first armour family remains a visibly meaningful Ranger/leather set.

Right-click enemy requests Basic Arrow, 1 requests Fire Arrow on the selected target, and 2 enters Arrow Rain ground targeting. Input is intent only. Simulation decides validity, range, facing, victims, damage, mana, cadence, death and success.

Exact downstream ownership, tuning gaps and authority details: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-archer-kit

## Numerical contract

Integer authoritative state includes HP, mana, damage, XP, levels, currency, item counts and discrete stats. One displayed HP or mana point equals 100 internal units; the Draft 0 player starts with 2,500 health units. Probabilities use explicit integer scaling and authoritative time uses fixed integer ticks.

Authoritative spatial/physics state uses finite Box3D-native single-precision metres. Starfall.Content uses BCL-only immutable System.Numerics-backed authoring values with the same units and precision, rejecting NaN, infinity and out-of-zone data. Simulation converts components one-to-one and does not maintain a parallel integer-millimetre coordinate model. Stable identity and explicit ordering isolate gameplay outcomes from native query order.

Level 2 requires 40 XP. Later requirements use nearest-integer half-up arithmetic:

next = (previous * 115 + 50) / 100

Accepted level 2-20 requirements:

40, 46, 53, 61, 70, 81, 93, 107, 123, 141, 162, 186, 214, 246, 283, 325, 374, 430, 495

## Authority and arrow presentation

Basic Arrow now resolves through the authoritative `SIM-0004` rule. It uses an inclusive 12-metre ground-plane range, stops and faces the selected monster on acceptance, resolves 12 ticks later, and permits the next start after 48 ticks. Accepted movement during windup cancels the shot while retaining the cadence; rejected movement does not. At resolution the actor must remain stationary and the target must remain alive, in range and within an inclusive 45-degree facing cone.

The rule requests 300 integer damage units. A light monster reaches zero on the third hit and a heavy monster on the seventh; effective overkill is clamped while defeat occurs only once. World applies nonlethal immutable health replacement, then routes first defeat into the existing camp vacancy/replenishment lifecycle at the exact resolve tick.

Basic Arrow and Fire Arrow create no authoritative spatial projectile, server-side travel, projectile collision or line-of-sight query. Arrow Rain likewise resolves an explicitly ordered victim set and damage at an authoritative tick. Protocol facts later carry action, target, timing, cancellation, resource and outcome information; Client-rendered arrows, flight, falling arrows, impacts and effects never decide collision, victims, damage, mana or success.

## Monsters, camps, town and respawn

CONTENT-0007 freezes the immutable starter-monster catalog in this authoritative order:

- `starter_flyer_light`: 700 internal HP (7 displayed);
- `starter_flyer_heavy`: 2,000 internal HP (20 displayed).

The exact initial camp composition is three light assignments in `camp_easy`; two light followed by two heavy assignments in `camp_mixed`; and three heavy assignments in `camp_hard`. These ten assignments copy the graybox's stable spawn identities and exact ground points in branch/local order.

`SIM-0003` binds those inputs into fixed-slot policies. Easy, mixed and hard have capacity/full initial population 3/4/3, opaque seeds 1/2/3 and a 600-tick delay. `SIM-0006` turns them into concrete World-owned entities with immutable health and placement state.

`SIM-0010` now supplies the bounded behavior proof. Light flyers use a 0.45 m radius, 2.5 m/s movement, 10 m awareness, 1.25 m attack range, 100 requested damage and a 60-tick cadence. Heavy flyers use 0.65 m, 1.8 m/s, 12 m, 1.5 m, 200 requested damage and 90 ticks.

Monsters acquire the nearest player inside their own camp and awareness radius, with world entity identity breaking equal-distance ties. Retained targets may move beyond awareness but remain valid only while inside the camp. Monsters pursue without pathfinding, stop in inclusive attack range, attack immediately on the first in-range tick, then obey exact cadence. They disengage when the player leaves the camp or disappears, return to their exact home point without reacquiring, and become eligible to acquire again on a later tick.

Ground radii constrain collision and camp containment. Monsters share World's static graybox boundaries and proxies with player movement, remain radius-inset inside their camp footprint and never gain altitude or airborne simulation. “Flyer” remains a provisional technical identity; hovering, bobbing, lunging, hit flash and death visuals remain presentation only.

Attack resolutions carry requested integer damage and authoritative tick facts. `SIM-0011` still owns applying those requests to the player's 2,500 health, protected-town rejection/exclusion, player defeat, configurable delay/restoration and town-anchor respawn. The current work creates no generic AI, navigation, dynamic-body avoidance, drops, rewards or protocol.

Authoritative removal at tick `T` still vacates one slot only after checked scheduling. Replenishment at `T + 600` uses a fresh identity with the same camp, archetype, exact point and full health. Running and Draining use the same deterministic behavior and replenishment rules; Stopping clears entities, behavior and pending replacements.

The local placeholder-monster task remains generated client presentation. A focused protocol/server/client extension later connects real monster state and approved behavior facts. Exact selected monster assets remain a separate CONTENT-0013 and coordinator ASSET-0008 path.

Durable camp and behavior contract: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-starter-flyers-and-camps`.

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
