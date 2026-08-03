---
id: CLIENT-0024
title: Capture deterministic Draft 0 graybox views
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0020
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0022
createdAt: 2026-08-03T15:16:34.0023740Z
modifiedAt: 2026-08-03T15:16:38.4480590Z
---

Integrate the coordinator-owned SDL GPU screenshot boundary into Starfall's existing Draft 0 native presentation path.

Acceptance criteria:
- Depend on the completed canonical coordinator screenshot task through the approved ChronoFallFamilyRoot source-consumption boundary; do not reference or copy Royale.
- Extend only Starfall.Client and its architecture allowlist/tests; World, Simulation, BalanceLab, Content, Protocol and Editor remain free of screenshot, SDL GPU and PNG dependencies.
- Add an explicit diagnostic capture-suite command that renders the exact F1-F7 camera presets through the same graybox and character presentation path used by the interactive preview.
- Capture player fixture, overview, town, junction, easy camp, mixed camp and hard camp in stable order with stable filenames, a frozen animation sample and no window chrome, focus automation or operating-system screenshot dependency.
- Emit exact PNG files into an explicit caller-selected output directory without adding them to runtime manifests or source control.
- Preserve Content coordinates, presentation-only layer offsets, camera framing and server-authority boundaries; screenshot generation must not mutate gameplay or content state.
- Validate the seven captures as correctly sized, opaque, nonblank and materially distinct, and use the corrected coordinator compositor to produce one labeled 4x2 review sheet.
- Obtain owner visual confirmation before completion. Preserve a curated Starfall project-history artifact only after explicit owner approval; keep raw captures ignored.
- Do not add asynchronous editor thumbnails, video capture, a general render graph, a general image framework or Royale adoption.