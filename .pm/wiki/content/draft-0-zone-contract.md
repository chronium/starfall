---
title: Draft 0 Zone Contract
createdAt: 2026-08-02T18:32:00.1104310Z
modifiedAt: 2026-08-03T07:31:43.8266550Z
---

## Ownership

Starfall Content owns the deterministic Draft 0 regional contracts through two non-overlapping tasks.

CONTENT-0006 is completed durable product input. It owns the approximately 200 x 200 metre envelope, protected-town targets, stable branch identities and approximate travel distances, camp-geometry intent, landmark purpose, and surface/boundary identities. It does not own executable coordinates, a renderable scene, asset placement, or a general map format.

CONTENT-0014 depends explicitly on CONTENT-0006 and owns the provisional executable graybox. It defines finite metre coordinates, bounded regions, landmark proxy blocks, route/camp corridors, coarse collision/navigation inputs, respawn and sample spawn points, and stable ordering. The graybox is genuine disposable development evidence rather than the proper authored map. It does not select or place the complete environment asset set.

EDITOR-0007 later authors the proper Draft 0 scene from these durable requirements, graybox evidence, and exact selected/staged assets. It compiles separate bounded authoritative and client-presentation outputs. SERVER-0012 consumes only the authoritative output; CLIENT-0016 consumes only the presentation output.

## Coordinate and numerical contract

Authoritative spatial and physics state uses Box3D-native single-precision floating-point metres. Starfall.Content authoring uses BCL-only immutable spatial values backed by System.Numerics with the same precision and units. Ground-plane values require Y = 0; the first zone occupies inclusive X/Z bounds from 0 to 200 metres.

CONTENT-0014 validates finite values, positive dimensions, containment, stable unique identifiers, immutable collections and deterministic ordering. NaN, infinity, duplicate identities, invalid dimensions and out-of-zone values are rejected.

A later Simulation boundary converts authoring components one-to-one into Box3D-native values. It must not change scale or precision, introduce a parallel integer-millimetre model, or make Starfall.Content depend on Box3D.

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

## Deferred decisions

Cycle 1 does not select a Box3D dependency. SIM-0008 records that a later coordinator grooming cycle must allocate the bounded shared acquisition/integration prerequisite, followed by the approved Starfall wiring continuation before SIM-0008 may activate.

Other deferred work includes the exact proper Editor-authored scene, selected environment placement, production collision/navigation compilation, protocol quantization and reconciliation tuning, and any reusable terrain, streaming, biome or general world/component framework.

Durable identity: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-zone-contract.
