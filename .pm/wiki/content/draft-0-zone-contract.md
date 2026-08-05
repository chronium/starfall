---
title: Draft 0 Zone Contract
createdAt: 2026-08-02T18:32:00.1104310Z
modifiedAt: 2026-08-05T06:50:17.8882250Z
---

## Ownership

Starfall Content owns the deterministic Draft 0 regional contracts through two non-overlapping tasks.

CONTENT-0006 is completed durable product input. It owns the approximately 200 x 200 metre envelope, protected-town targets, stable branch identities and approximate travel distances, camp-geometry intent, landmark purpose, and surface/boundary identities. It does not own executable coordinates, a renderable scene, asset placement, or a general map format.

CONTENT-0014 depends explicitly on CONTENT-0006 and owns the provisional executable graybox. It defines finite metre coordinates, bounded regions, landmark proxy blocks, route/camp corridors, coarse collision/navigation inputs, respawn and sample spawn points, and stable ordering. The graybox is genuine disposable development evidence rather than the proper authored map. It does not select or place the complete environment asset set.

EDITOR-0007 later authors the proper Draft 0 scene from these durable requirements, graybox evidence, and exact selected/staged assets. It compiles separate bounded authoritative and client-presentation outputs. SERVER-0012 consumes only the authoritative output; CLIENT-0016 consumes only the presentation output.

## Coordinate and numerical contract

Authoritative spatial and physics state uses Box3D-native single-precision floating-point metres. Starfall.Content authoring uses BCL-only immutable spatial values backed by System.Numerics with the same precision and units. Ground-plane values require Y = 0; the first zone occupies inclusive X/Z bounds from 0 to 200 metres.

CONTENT-0014 validates finite values, positive dimensions, containment, stable unique identifiers, immutable collections and deterministic ordering. NaN, infinity, duplicate identities, invalid dimensions and out-of-zone values are rejected.

`SIM-0008` converts these authoring components one-to-one into Box3D-native values. It does not change scale or precision, introduce a parallel integer-millimetre model, or make Starfall.Content depend on Box3D.

Discrete gameplay state remains integer: HP, mana, damage, XP, levels, currency, item counts and discrete stats. Probabilities use an explicitly scaled integer representation. Authoritative time uses integer fixed simulation ticks. Native physics/query iteration order is never trusted when order can affect gameplay; stable entity identities and explicit sorting establish gameplay ordering.

Initial networking preserves the actual finite IEEE-754 spatial values used by the authoritative simulation. Quantization and compression remain later measured protocol decisions rather than a second coordinate system.

## Draft 0 regional targets

| Region/input | Stable identity | Durable target |
| --- | --- | --- |
| Zone | draft_0_first_playable_zone | Approximately 200 x 200 metres |
| Town | town_safe | Approximately 50 x 50 metres near an edge, protected, one exit, configured respawn anchor and two or three landmark intentions |
| Short branch | branch_short | Approximately 25 metres to a broad open easy camp |
| Medium branch | branch_medium | Approximately 45 metres to an elongated or divided mixed camp |
| Long branch | branch_long | Approximately 70 metres to a constrained hard-camp approach |
| Exterior surface | surface_grass | Flat grass treatment is acceptable |
| Route surface | surface_dirt_path | Dirt paths connect town, junction and camps |
| Boundary/separation | boundary_rocks_boulders | Rocks/boulders communicate outer bounds and separation |

The graybox may represent these with flat colour, lines, planes and boxes. Temporary assets are optional, separately approved presentation inputs and never replace deterministic regions/collision/navigation.

## Provisional executable graybox

`Draft0GrayboxCatalog.FirstPlayable` freezes the disposable executable input for the local walking graybox. Coordinates below use `(X,Z)` metres and are ordered exactly as listed.

| Input | Identity | Exact value |
| --- | --- | --- |
| Walkable bounds | — | `(5,5)` through `(195,195)` inside the durable `(0,0)` through `(200,200)` zone |
| Protected town | `town_safe` | `(75,5)` through `(125,55)` |
| Respawn anchor | — | `(100,25)` |
| Town exit anchor | — | `(100,55)` |
| Exit junction | — | `(100,70)` |

| Route | Ordered centreline | Width | Validated length |
| --- | --- | --- | --- |
| `route_town_exit` | `(100,55) -> (100,70)` | 8 m | 15 m |
| `route_branch_short` | `(100,70) -> (75,70)` | 6 m | 25 m |
| `route_branch_medium` | `(100,70) -> (100,115)` | 6 m | 45 m |
| `route_branch_long` | `(100,70) -> (145,70) -> (145,95)` | 6 m | 70 m |

A route's thick diagnostic presentation is its swept centreline plus round endpoint caps with radius equal to half its width. This makes the short route visibly overlap the circular easy camp instead of merely touching its tangent. The footprint is presentation semantics, not a movement constraint or navigation format.

| Branch/camp | Bounds and geometry | Entry anchor |
| --- | --- | --- |
| `branch_short` / `camp_easy` | `(45,55)` through `(75,85)`; actual circular footprint centred at `(60,70)` with 15 m radius | `(75,70)` |
| `branch_medium` / `camp_mixed` | `(90,115)` through `(110,150)`; elongated/divided footprint | `(100,115)` |
| `branch_long` / `camp_hard` | `(130,95)` through `(160,125)`; tight bowl/constrained approach | `(145,95)` |

Collidable diagnostic proxies remain in this stable order:

1. `landmark_west_south`: town landmark, `(80,12)` through `(94,26)`, height 8 m.
2. `landmark_east_south`: town landmark, `(106,12)` through `(120,26)`, height 8 m.
3. `landmark_west_north`: town landmark, `(80,34)` through `(94,48)`, height 7 m.
4. `mixed_divider`: camp divider, `(99,126)` through `(101,140)`, height 2 m.
5. `hard_bowl_wall_west`: camp wall, `(130,99)` through `(134,125)`, height 3 m.
6. `hard_bowl_wall_east`: camp wall, `(156,99)` through `(160,125)`, height 3 m.
7. `hard_bowl_wall_north`: camp wall, `(134,121)` through `(156,125)`, height 3 m.

Neutral sample spawns are also stable and branch-local: easy uses `spawn_easy_01 (55,65)`, `spawn_easy_02 (60,75)`, and `spawn_easy_03 (65,65)`; mixed uses `spawn_mixed_01 (95,122)`, `spawn_mixed_02 (105,122)`, `spawn_mixed_03 (95,144)`, and `spawn_mixed_04 (105,144)`; hard uses `spawn_hard_01 (140,104)`, `spawn_hard_02 (150,104)`, and `spawn_hard_03 (145,114)`.

Construction validates ordinal global identity uniqueness; finite dimensions and coordinates; zone, walkable, owner and actual-camp containment; proxy ownership; unobstructed critical anchors and spawns; route linkage and lengths with a 0.001 m tolerance; and square bounds plus boundary entry for the circular camp. Collections are copied into read-only views.

`SERVER-0004` binds this exact immutable catalog to each current world/channel runtime before lifecycle start. World preserves the Content object and its ordering directly rather than copying it into a parallel map model. The deterministic `READY` diagnostic identifies `draft_0_first_playable_zone`, `town_safe`, three branches, four routes, seven proxies and ten sample spawns. `SIM-0008` converts the four outer strips and seven ordered proxies into bounded Box3D collision while leaving routes non-navigational, sample spawns non-entities, and protected-town hostile/monster/respawn enforcement to `SIM-0011`.

These remain coarse Content inputs with no Box3D dependency. Starfall.Simulation alone owns their provisional Box3D conversion and movement policy; they still add no renderer, editor, serialization or monster contract and are expected to be replaced by the later editor-authored scene.

### Starter-monster binding

CONTENT-0007 consumes these neutral sample spawns through `Draft0StarterMonsterCatalog.FirstPlayable`. It copies each stable identity and exact `GroundPoint` value rather than retaining `Draft0SampleSpawn` objects.

The exact stable binding is three light assignments in `camp_easy`; two light then two heavy assignments in `camp_mixed`; and three heavy assignments in `camp_hard`. All ten assignments preserve the branch/local spawn order above. Position validation uses exact value equality because it copies already-approved authoring values; it introduces no second spatial tolerance.

The content catalog does not turn sample spawns into runtime entities or define capacity, templates, replenishment, timing or random selection. `SIM-0003` owns capacity/seeds/replenishment, `SIM-0006` owns runtime entities, and `SIM-0010` owns numeric behavior. Durable catalog: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-starter-flyers-and-camps`.

## Deferred decisions

Completed coordinator task `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0021` supplies the child-independent Box3D mechanics consumed by `SIM-0008`. Starfall owns the stable creation order, entity identities, collision layers, 60 Hz schedule, direct movement rule and stop-and-clear outcome.

Other deferred work includes the exact proper Editor-authored scene, selected environment placement, production collision/navigation compilation, protocol quantization and reconciliation tuning, and any reusable terrain, streaming, biome or general world/component framework.

Durable identity: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-zone-contract.
