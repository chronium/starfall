---
id: CLIENT-0017
title: Present selected starter flyer assets
track: CLIENT
priority: none
dependsOn:
- CLIENT-0023
- CONTENT-0013
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/ASSET-0008
createdAt: 2026-08-02T15:49:18.3357340Z
modifiedAt: 2026-08-06T06:44:47.0597090Z
---

Replace connected placeholder monsters with the exact selected and coordinator-staged starter-flyer presentation inputs.

Acceptance criteria:
- Reuse the connected monster snapshot-to-presentation adapter and stable identities from CLIENT-0023.
- Present starter_flyer_light and starter_flyer_heavy with the exact evidence-gated representation selected by CONTENT-0013.
- Keep yaw, hovering/bobbing, lunging/pulsing, hit flash, return and simple death entirely client-owned.
- Preserve ground-plane authority and avoid altitude, flight navigation, vertical combat, collision, targeting, damage or AI decisions.
- Accept static or rigid presentation when selection supports it; do not require locomotion cycles, IK, retargeting or a generic monster skeletal pipeline.