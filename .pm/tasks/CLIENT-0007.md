---
id: CLIENT-0007
title: Present locomotion, Basic Arrow, and player reactions
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0009
- CLIENT-0012
- SIM-0004
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0008
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0009
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/ASSET-0004
createdAt: 2026-08-01T05:46:49.7988710Z
modifiedAt: 2026-08-02T16:32:42.2431940Z
---

Present authoritative player locomotion, Basic Arrow action timing, hit reactions, and death through the shared character boundary.

Acceptance criteria:
- Consume the exact coordinator-acquired archer and compatible bow-animation inputs through canonical ASSET-0004; Sword_Attack is not an acceptable bow placeholder.
- Present locomotion and the authoritative Basic Arrow action/reaction facts without deciding combat outcomes.
- Preserve coherent notch/release timing inputs for later native validation and visual-arrow work.
- Keep selection, cooking/provenance, and gameplay/presentation ownership boundaries explicit.
- Do not implement Fire Arrow visuals, Arrow Rain, projectile entities, equipment sockets, weapon IK, retargeting, or a general animation graph.