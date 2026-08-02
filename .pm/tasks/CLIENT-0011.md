---
id: CLIENT-0011
title: Present the starter bow, aim, and IK
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0007
- CLIENT-0012
- CONTENT-0009
- GAME-0005
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0010
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0011
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0012
createdAt: 2026-08-02T07:33:29.3670540Z
modifiedAt: 2026-08-02T15:52:42.7460010Z
---

Present the selected starter wooden bow and nocked arrow from authoritative equipment state.

Acceptance criteria:
- Consume the exact Starfall bow/arrow attachment definitions and shared socket, grip, aim-reference, and IK contracts.
- Render the equipped bow and presented arrows with bounded aiming and off-hand support appropriate to the selected assets.
- Keep equipment authority, action success, and combat timing server-owned.
- Do not implement projectile outcomes, armour mapping, generic attachment categories, final animation graphs, or new asset acquisition.