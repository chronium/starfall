---
id: CLIENT-0029
title: Adopt the shared ImGui backend in Starfall.Client
track: CLIENT
milestone: M4
dependsOn:
- CLIENT-0020
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0026
createdAt: 2026-08-06T06:41:23.5102280Z
modifiedAt: 2026-08-06T13:17:11.2961380Z
---

Add the approved family-source reference and instantiate the caller-controlled shared ImGui backend in Starfall.Client. Verify the exact source allowlist, Client-only native assets, lifecycle compatibility, and headless isolation. Exclude windows, menus, commands, visibility behavior, and permanent game UI.

## Notes

- 2026-08-06 13:17 UTC - Implemented the approved family-source adoption through `$(ChronoFallFamilyRoot)src/ChronoFall.EditorUi.SdlGpu/ChronoFall.EditorUi.SdlGpu.csproj`. Only interactive local and connected previews instantiate the caller-controlled backend; Starfall continues to own SDL lifecycle, render scheduling and gameplay input, and the backend records an empty frame last in the existing render pass. Hidden deterministic captures and `--validate-character-content` remain backend-free. Added exact source-allowlist, Client-only managed/native output and headless-leakage architecture coverage. Interactive windows are resizable; no debug surface, capture suppression, menu, command or visibility behavior was added.

  Validation: native bridge rebuilt for macOS ARM64; stable-ID character staging succeeded with SHA-256 `37d2ecd2c614a4cc74fe359906c84408432100f0338b86d7ce4f4dddb6b585d3`; Debug build passed with 403 tests; Release build passed and all seven Release projects passed (403 tests total). Two independent aggregate runs exposed the existing intermittent Box3D native allocation failure in different Simulation/World tests; each affected project passed immediately in isolation (46/46 and 105/105), while this task changes only Client/UI integration. Character validation retained exact `quaternius-ual1-standard`, 65 joints and `Idle_Loop,Walk_Loop,Sword_Attack`. Seven 1920x1080 hidden capture fingerprints remained `e668208dea46f904`, `9b08f56034585eaf`, `8391b2cb4c089c7e`, `31512e8b5b2094c0`, `e5beeedc16cd627f`, `0a4f95e2d861170d`, `a017df7907dcbf97`. PM doctor, family warning review and `git diff --check` passed.

  Owner validation on macOS ARM64 confirmed the corrected connected window resized, no debug UI was visible, and connected movement plus Basic Arrow outcomes, damage and monster defeat worked. The client exited cleanly when its window closed; the World then drained and stopped with zero catch-up clamps.