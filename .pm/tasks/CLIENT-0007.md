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
modifiedAt: 2026-08-07T07:00:17.3245630Z
---

Present connected locomotion and Basic Arrow bow-body animation through the shared character boundary.

Acceptance criteria:
- Consume the exact coordinator-acquired archer and compatible bow-animation inputs through canonical ASSET-0004; Sword_Attack is not an acceptable bow placeholder.
- Drive locomotion and notch, aim, release and recovery from authoritative Basic Arrow timing facts without deciding combat outcomes.
- Preserve coherent release timing evidence for the separately owned bow, visual-arrow and impact tasks.
- Reuse established blending and layering contracts while keeping selection, cooking/provenance and product presentation ownership explicit.
- Do not own player hit/death reactions, monster hit/death presentation, Fire Arrow visuals, Arrow Rain, projectile entities, equipment sockets, weapon IK, retargeting or a general animation graph.

## Notes

- 2026-08-07 07:00 UTC - Implemented the connected Basic Arrow technical bow-body animation path.

  - Loaded the exact staged `quaternius-ual2-source-bow-shot-body` cook and restricted it to `Bow_Notch`, `Bow_Aim_Neutral` and `Bow_Shoot`.
  - Rebound those clips only after exact 65-joint hierarchy, bind-pose and inverse-bind validation against the existing UAL1 technical mannequin; no retargeter or alternate character cook was introduced.
  - Added ordered retention of accepted, rejected, canceled and resolved Basic Arrow facts so the native presentation controller cannot lose same-poll lifecycle events.
  - Compressed the authoritative 12-tick / 0.20-second windup into nine ticks of complete clamped notch sampling plus three ticks blending into neutral aim. Resolution gates `Bow_Shoot`; late resolution holds aim.
  - Layered only the 53-joint `spine_01` subtree, preserving root, pelvis and leg locomotion exactly. One final pose drives GPU skinning and the existing left-hand bow socket.
  - Emitted the owner-reviewed presentation-only release marker exactly once at `Bow_Shoot` frame 3 / 100 ms. Cancellation returns without a marker; recovery is 0.15 seconds.
  - Preserved the no-connection local fixture and the existing seven-view capture fingerprints.

  Validation:
  - Debug build: succeeded with 0 warnings and 0 errors.
  - Debug full suite: 477 passed.
  - Release build: succeeded with 0 warnings and 0 errors.
  - Release full suite: 477 passed.
  - Character-content validation accepted the exact UAL1 character, UAL2 bow-body and static bow identities.
  - Native macOS ARM64 connected World/Client validation exercised idle/walk, accepted Basic Arrow, notch/aim, release, recovery and repeated attacks. The owner confirmed that it works well.
  - The World drained and stopped cleanly after the native check.
  - No screenshot was retained: this was motion/transition validation rather than a useful still-image project-history checkpoint.