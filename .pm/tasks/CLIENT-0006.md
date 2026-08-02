---
id: CLIENT-0006
title: Integrate shared character presentation foundation
track: CLIENT
milestone: M1
dependsOn:
- BUILD-0003
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0001
createdAt: 2026-08-01T05:46:49.3319000Z
modifiedAt: 2026-08-02T07:30:17.4221600Z
---

Integrate the parent-owned shared skinned-character presentation foundation into Starfall without introducing a dependency on Royale or parent-relative source references. Preserve Starfall-specific gameplay and protocol ownership. Before this task can be activated, add a canonical dependency on a coordinator-owned task that establishes independent child acquisition for shared binaries and cooked content; that coordinator contract is intentionally not fabricated by SF-0004.