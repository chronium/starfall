---
id: CLIENT-0007
title: Present Basic Arrow bow-body animation
track: CLIENT
milestone: M5
dependsOn:
- CLIENT-0012
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0008
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0009
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/ASSET-0004
createdAt: 2026-08-01T05:46:49.7988710Z
modifiedAt: 2026-08-06T06:42:59.8582850Z
---

Present connected locomotion and Basic Arrow bow-body animation through the shared character boundary.

Acceptance criteria:
- Consume the exact coordinator-acquired archer and compatible bow-animation inputs through canonical ASSET-0004; Sword_Attack is not an acceptable bow placeholder.
- Drive locomotion and notch, aim, release and recovery from authoritative Basic Arrow timing facts without deciding combat outcomes.
- Preserve coherent release timing evidence for the separately owned bow, visual-arrow and impact tasks.
- Reuse established blending and layering contracts while keeping selection, cooking/provenance and product presentation ownership explicit.
- Do not own player hit/death reactions, monster hit/death presentation, Fire Arrow visuals, Arrow Rain, projectile entities, equipment sockets, weapon IK, retargeting or a general animation graph.