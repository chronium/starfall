---
title: Basic Arrow Presentation Inputs
createdAt: 2026-08-06T17:15:11.4897310Z
modifiedAt: 2026-08-06T17:15:11.4897310Z
---

## Status and ownership

Starfall task `CONTENT-0011` freezes the exact provisional presentation inputs for the connected Basic Arrow milestone. This is a selection contract only. ChronoFall remains responsible for source provenance, bounded cooking and stable-ID staging; later Starfall Client tasks own animation sequencing, semantic sockets, attachment transforms and projectile presentation.

These inputs are technical placeholders, not final dark-elf art, equipment, a Ranger loadout or a public release selection.

## Technical character

The existing cooked technical humanoid remains selected as the complete temporary character body:

- coordinator path: `assets/Quaternius/Universal Animation Library[Standard]/Unreal-Godot/UAL1_Standard.glb`;
- source SHA-256: `69591853d817488edaa8fd9bf8fc1d821eaeaf789f8627b3cd23b41c4ed67997`;
- mesh: `Mannequin`;
- skin: `Armature`;
- cooked identity already consumed by Starfall: `quaternius-ual1-standard`;
- selected neutral and locomotion clips: `Idle_Loop`, then `Walk_Loop`.

The technical mannequin fulfills the whole provisional character role. No separate underlayer, Universal Base Character, Ranger armour, starter loadout or equipment definition is selected.

## Bow-body clips

The exact private-package input is `Unreal-Godot/UAL2.glb` from the owner-supplied **Universal Animation Library 2 Source** snapshot. The source remains outside every public family repository. Its recorded SHA-256 is `866c2ee822d30f0ceed521f50a5e84316d58ee4487d0b02158370bb988452416`.

Basic Arrow selects only:

| Clip | Duration | Samples | Role |
| --- | ---: | ---: | --- |
| `Bow_Notch` | 2.500000 s | 76 | one-shot notch/draw body motion |
| `Bow_Aim_Neutral` | 2.500000 s | 76 | neutral held-aim body motion |
| `Bow_Shoot` | 0.666667 s | 21 | release and body recovery |

The clips are non-root-motion, use 30 Hz LINEAR TRS channels, and target the same ordered 65-joint hierarchy as the UAL1 technical mannequin. Ordered joint names, parents, local rest transforms and inverse-bind matrices match exactly, so the proven combination requires no retargeting.

`Bow_Shoot` frame 3 at 100 ms is the owner-reviewed provisional body-release marker. It remains presentation evidence only and must be revalidated with the real rigid bow and arrow visible. No distinct recovery clip is selected; later presentation returns from `Bow_Shoot` into the current idle or locomotion state.

The 2.5-second authored notch clip does not fit the authoritative Basic Arrow 12-tick / 0.20-second windup unchanged. `CLIENT-0007` owns the later task-planned sampling, cropping, acceleration or blending policy. This selection does not change gameplay timing. `Bow_Aim_Up` and `Bow_RapidShoot_Loop` remain unselected Arrow Rain candidates. `Idle_No_Loop`, `Yes`, `Sword_Attack` and the rest of the 134-clip UAL2 library are excluded.

Coordinator evidence: `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/wiki/assets/quaternius-ual2-source-bow-evaluation`.

## Static bow and arrow

The selected source pack is **Medieval Weapons Pack by @Quaternius**, supplied under CC0 1.0. Project policy retains Quaternius credit.

### Bow

- source: `assets/Quaternius/Medieval Weapons Pack by @Quaternius/OBJ/Bow_Wooden.obj`;
- material library: matching `Bow_Wooden.mtl`;
- OBJ SHA-256: `788c9e72bdd839a86704113ed4809a96cfedf09441bb3f98f383a7abfe751e6d`;
- MTL SHA-256: `545318d522d6ab3f0f4942cd5fc25001fcc9c1a722cef2d04555009721847a54`;
- structure: one 370-vertex, 363-face mesh using `DarkWood`, `LightWood` and `White`;
- raw OBJ bounds: 1.467922 by 5.436604 by 0.243410 source units.

### Arrow

- source: `assets/Quaternius/Medieval Weapons Pack by @Quaternius/OBJ/Arrow.obj`;
- material library: matching `Arrow.mtl`;
- OBJ SHA-256: `6960c207e3a8e6f2f09cbfd31b7fe990119cd260ef692729c498738a86698bf1`;
- MTL SHA-256: `cee901eef3fabe40154cc3a13ed3d64181aac886767fb1132382667332c6891f`;
- structure: one 120-vertex, 110-face mesh using `LightWood`, `Steel`, `LightSteel` and `Red`;
- raw OBJ bounds: 0.274145 by 0.238274 by 2.733824 source units.

The matching pack licence is `assets/Quaternius/Medieval Weapons Pack by @Quaternius/License.txt`, SHA-256 `d32abf5eb61a5d20c582525c2ee9d8d42d86401d6b3ea0a2d5283fcaecaa35b9`.

Read-only Blender inspection found one identity-transformed mesh per source file. The Blend scenes declare no physical unit system; their dimensions use the same numbers with the exporter's Y/Z axis bridge. A uniform `0.25` metres-per-source-unit conversion is the explicit first acquisition candidate, producing a roughly 1.36-metre bow and 0.68-metre arrow. `ASSET-0006` must verify and freeze or reject that conversion through deterministic cooking and native human-scale review. Selection does not claim the source units are metres.

OBJ/MTL is selected over FBX or a new GLB export because the existing shared static cooker already supports exact OBJ/MTL inputs. No manual conversion or new importer is required. The bow remains rigid; no bow/string animation is selected.

## Downstream ownership and exclusions

- `ASSET-0004`: acquire, cook and stage the exact technical character and three UAL2 bow clips without changing the historical UAL1 cook.
- `ASSET-0006`: acquire, scale, cook and stage exactly `Bow_Wooden` and `Arrow`.
- `SHARED-0020`: prove the reusable socketed-static-render boundary with a harness-local transform.
- `CLIENT-0007`: map authoritative Basic facts into the selected body-animation sequence.
- `CLIENT-0011`: define Starfall's provisional semantic hand socket and local bow transform, then validate native placement.
- `CLIENT-0018`: own arrow nocking, visual detachment, travel, impact and stale-presentation cleanup.

This task selects no socket transform, grip, off-hand IK, aiming system, equipment state, ammunition inventory, projectile authority, effects library, monster art, cursor, HUD or final character assets.