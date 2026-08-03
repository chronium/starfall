---
id: CLIENT-0024
title: Capture deterministic Draft 0 graybox views
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0020
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0022
createdAt: 2026-08-03T15:16:34.0023740Z
modifiedAt: 2026-08-03T17:08:33.7779060Z
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

## Notes

- 2026-08-03 17:08 UTC - Implementation and validation evidence (2026-08-03):

  - Starfall.Client now owns `--capture-graybox-suite <directory>`, rendering the exact F1-F7 presets through the same RecordFrame path as the interactive preview into caller-owned 1920x1080 GPU targets.
  - The suite freezes `Idle_Loop` at 0.500 seconds, stable preset/file order, exact dimensions, full opacity, non-flat frames and distinct FNV fingerprints before writing PNG files.
  - The coordinator boundary is limited to one-shot SDL GPU readback, RGBA/BGRA normalization and PNG encoding; only Starfall.Client consumes it. World, Simulation, BalanceLab, Content, Protocol and Editor remain presentation-free.
  - Two native macOS ARM64 Metal runs produced byte-identical PNG files and identical fingerprints for all seven views. The corrected coordinator compositor produced an exact 7680x2256 four-by-two sheet.
  - Owner visually accepted the framing and explicitly approved preservation on 2026-08-03.
  - Preserved only `docs/project-history/2026-08-03-draft-0-graybox-capture/contact-sheet.png`; SHA-256 `7b94a01f3b62255c3450f205311252dd444b62a77d19fa5ec01e2cf3dd847095`. Seven raw captures and temporary sheets remain outside source control.
  - Debug build succeeded with zero warnings/errors; Debug tests passed 89/89.
  - Release build succeeded with zero warnings/errors; Release tests passed 89/89.
  - Starfall.Client and both changed test projects pass `dotnet format --verify-no-changes`. Whole-solution formatting reports only the existing coordinator-pinned SDL3-CS IDE1006 naming warnings.
  - PM MCP validation and `pm doctor` passed; `git diff --check` passed; Royale remained clean and unchanged.
  - Wiki: `development/draft-0-graybox-capture-suite`, `product/first-playable-zone-draft-0`, and `architecture/overview` record the capture recipe, evidence and ownership boundary.