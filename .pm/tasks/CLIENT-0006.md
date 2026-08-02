---
id: CLIENT-0006
title: Integrate shared character presentation foundation
track: CLIENT
milestone: M1
dependsOn:
- BUILD-0003
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0001
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0016
createdAt: 2026-08-01T05:46:49.3319000Z
modifiedAt: 2026-08-02T09:48:06.0282290Z
---

Integrate the parent-owned shared skinned-character presentation foundation into Starfall from source in the canonical coordinator family checkout. Reference only the approved coordinator projects through `$(ChronoFallFamilyRoot)`, consume the coordinator-owned generated client cook/copy output, and prove the client build and runtime presentation path without a Royale dependency.

Preserve Starfall-owned gameplay, protocol, content, launch, build/release decisions, and commits. Keep World, Simulation, BalanceLab, Content, Protocol, and Editor free of shared presentation, SDL/GPU, and generated client content. The coordinator remains responsible for shared source, SDL3-CS acquisition, the selected Quaternius cook, and the copy workflow; this task owns only Starfall client integration.

Do not redesign coordinator source, introduce package/feed distribution, add literal parent-relative or absolute checkout paths, copy raw source assets, stabilize the provisional cooked format, leak presentation into headless projects, or advance the coordinator gitlink.