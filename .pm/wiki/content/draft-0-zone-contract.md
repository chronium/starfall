---
title: Draft 0 Zone Contract
createdAt: 2026-08-02T18:32:00.1104310Z
modifiedAt: 2026-08-02T18:32:00.1104310Z
---

## Ownership

Starfall Content owns this deterministic Draft 0 regional contract. `CONTENT-0006` defines the map envelope, protected-town targets, branch identities, approximate travel distances, camp-geometry intent, and surface/boundary identities.

`CONTENT-0014` owns the exact graybox layout: coordinates, polygons, path centre lines, camp extents, collision/navigation inputs, respawn anchor, and authored placements. This page does not integrate Box3D, choose a binding or pinned dependency, author a general world format, or replace exact-layout work.

## Coordinate contract

Authoritative spatial and physics state uses Box3D-native single-precision floating-point metres. Content authoring represents finite ground-plane positions and dimensions with BCL-only immutable value types backed by `System.Numerics.Vector3`. Ground-plane values require `Y = 0`; the first zone occupies inclusive X/Z bounds from `0` to `200` metres.

A later simulation boundary may convert these values one-to-one into Box3D-native vector types. It must not silently change unit scale or precision. Visual meshes, terrain materials, and props never replace deterministic collision, navigation, camp, town, or respawn inputs.

## Draft 0 regional targets

| Region/input | Stable identity | Draft 0 target |
| --- | --- | --- |
| Zone | `draft_0_first_playable_zone` | Approximately 200 x 200 metres |
| Town | `town_safe` | Approximately 50 x 50 metres, near an edge, protected, one exit, configured respawn anchor, two or three landmark buildings |
| Short branch | `branch_short` | Approximately 25 metres to a broad, open circular easy camp |
| Medium branch | `branch_medium` | Approximately 45 metres to an elongated or divided mixed camp |
| Long branch | `branch_long` | Approximately 70 metres to a tight bowl or constrained hard-camp approach |
| Exterior surface | `surface_grass` | Flat grass treatment is acceptable |
| Route surface | `surface_dirt_path` | Dirt paths connect town, junction, and camps |
| Boundary/separation | `boundary_rocks_boulders` | Rocks and boulders define the outer boundary, separate spaces, and add landmarks |

The branch distances and dimensions are experimental inputs. They are not exact placements and must remain inspectable Balance Lab/content inputs.

## Numerical and determinism policy

Spatial and physics state uses finite single-precision metres. Discrete gameplay state remains integer: one displayed HP or mana point is 100 internal units, primary attributes are integers, probabilities use explicit integer scales, and authoritative time uses fixed integer ticks.

Native physics and spatial-query iteration order is not an authority contract. Simulation assigns stable entity identities and applies explicit deterministic ordering wherever order can affect gameplay outcomes. Client prediction and reconciliation must tolerate authoritative float corrections.

Initial networking serializes the actual finite IEEE-754 spatial values selected by the authoritative simulation. Quantization, fixed-point conversion, and compression remain future protocol-boundary decisions supported by measurements; they are not implied by this content contract.

## Validation and deferred decisions

The regional contract validates finite values, ground-plane use, positive dimensions, containment, stable unique identifiers, immutable collections, three ordered branches, and a town that fits the zone. Tests must remain BCL-only and deterministic.

Deferred work includes:

- exact graybox coordinates and geometry in `CONTENT-0014`;
- Box3D version, bindings, build integration, and simulation ownership;
- collision and navigation compilation;
- network encoding, quantization, and reconciliation tuning;
- client rendering and asset selection;
- a reusable terrain, streaming, biome, or general world/component framework.

Durable identity: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-zone-contract`.