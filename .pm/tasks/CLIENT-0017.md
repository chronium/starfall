---
id: CLIENT-0017
title: Present starter flyer monsters
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0009
- SIM-0010
- CONTENT-0013
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/ASSET-0008
createdAt: 2026-08-02T15:49:18.3357340Z
modifiedAt: 2026-08-02T16:32:42.2711380Z
---

Present synchronized starter_flyer_light and starter_flyer_heavy state using the exact selected and coordinator-staged temporary monster inputs. Client-only yaw, hovering/bobbing, lunging or pulsing, hit flash, return, and simple death presentation are sufficient and must consume authoritative state/events only.

Authoritative monsters remain ground-plane entities; presentation never creates altitude, flight navigation, vertical combat, collision, targeting, damage, or AI. Static or rigid presentation is acceptable. Canonical ASSET-0008 keeps acquisition evidence-gated; its later reviewed plan attaches only the static, rigid, or skeletal prerequisite proven by completed selection.

Do not require locomotion cycles, foot placement, IK, retargeting, or a generic monster skeletal pipeline.