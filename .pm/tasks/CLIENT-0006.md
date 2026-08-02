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
modifiedAt: 2026-08-02T12:43:18.3156600Z
---

Integrate the parent-owned shared skinned-character presentation foundation into Starfall from source in the canonical coordinator family checkout. Reference exactly the approved coordinator projects through `$(ChronoFallFamilyRoot)`, consume the coordinator-owned generated client cook/copy output, and prove the client build and runtime presentation path without a Royale dependency.

Preserve Starfall-owned gameplay, protocol, content, launch, build/release decisions, and commits. Keep World, Simulation, BalanceLab, Content, Protocol, and Editor free of shared presentation, SDL/GPU, and generated client content. The coordinator remains responsible for shared source, SDL3-CS acquisition, the selected Quaternius cook, and the copy workflow; this task owns only Starfall client integration.

## Acceptance criteria

- Starfall.Client references exactly `ChronoFall.CharacterPresentation`, `ChronoFall.CharacterPresentation.Cooking`, and `ChronoFall.CharacterPresentation.SdlGpu` through `$(ChronoFallFamilyRoot)`; it does not reference Royale or SDL3-CS directly.
- The stable-ID coordinator staging workflow remains the only source of the ignored selected Quaternius client cook, deterministic provenance, and CC0 evidence.
- The client build fails with an actionable staging instruction when the expected generated cook is absent and copies only the known cook, provenance, licence evidence, compiled shared shaders, and verified native runtime into bounded client output locations.
- Running Starfall.Client with no arguments opens a bounded native SDL GPU preview, loads the cooked humanoid, continuously samples `Idle_Loop`, and exits through Escape or window close.
- `--validate-character-content` validates the exact cooked asset, skeleton, and clip selection without initializing SDL; unknown arguments fail with exit code 2.
- Starfall owns SDL initialization, window/device/swapchain/depth lifetime, camera, frame timing, command buffers, submission, runtime mapping, and diagnostics while the shared renderer owns only its existing upload/draw resources.
- World, Simulation, BalanceLab, Content, Protocol, and Editor remain free of shared presentation, SDL/GPU, shaders, and generated client content.
- Debug and Release restore/build/test validation, native macOS ARM64 execution, explicit owner visual confirmation, PM validation, and repository/submodule review pass before completion.
- Starfall architecture and workflow documentation record the implemented integration and reproduction commands.
- The focused Starfall commit is followed by the verified automatic pointer-only coordinator handoff under this canonical child task; no coordinator PM task is created.

Do not redesign coordinator source, introduce package/feed distribution, add literal parent-relative or absolute checkout paths, copy raw source assets, stabilize the provisional cooked format, leak presentation into headless projects, add gameplay/networking/equipment/UI/editor behavior, or retain a visual-history artifact without owner approval.

## Notes

- 2026-08-02 12:43 UTC - Implemented the approved Starfall client integration through the canonical coordinator family checkout.

  - Starfall.Client now references exactly ChronoFall.CharacterPresentation, ChronoFall.CharacterPresentation.Cooking, and ChronoFall.CharacterPresentation.SdlGpu through ChronoFallFamilyRoot. Solution builds preserve Debug/Release configuration across the external source graph; SDL3-CS remains a transitive coordinator-owned source project.
  - The client build consumes only the stable-ID workflow's ignored Quaternius cook, deterministic provenance, CC0 evidence, compiled MSL/SPIR-V shaders, and macOS ARM64 SDL runtime. A missing-stage probe fails before build with the exact recovery command.
  - No-argument launch now owns a bounded native SDL GPU window/device/swapchain/depth/frame lifecycle, samples Idle_Loop, evaluates the shared pose/palette, and renders through the shared skinned renderer. The non-graphical --validate-character-content path validates asset quaternius-ual1-standard, 65 joints, and clips Idle_Loop, Walk_Loop, and Sword_Attack.
  - World, Simulation, BalanceLab, Content, Protocol, Editor, Royale, coordinator shared source, and raw supplied assets remain unchanged and presentation-free.

  Validation on 2026-08-02:
  - Stable-ID staging succeeded for prj_pkIpzx0fzFD4URjvqBuYrGZF. The staged and copied .cfskel hashes both equal 37d2ecd2c614a4cc74fe359906c84408432100f0338b86d7ce4f4dddb6b585d3; provenance hashes both equal bbe46b17fa0882e3ba5cdc46093a67df3224b6d5892aa5463ee6d386fce9d8c9.
  - Debug and Release solution builds passed with 0 warnings/errors; the direct Release client build compiled the approved coordinator projects and SDL3-CS source in Release. All 23 architecture/runtime-content tests passed in both Debug and Release.
  - The content validation command exited 0 with the exact asset/joint/clip summary. An intentionally missing stage exited 1 with the documented stable-ID staging command.
  - Focused dotnet format verification passed. Whole-solution format remains noisy only for pre-existing ignored SDL3-CS source style; no third-party file changed.
  - Native macOS ARM64 Metal launch exited 0 after printing start, controls, and stop diagnostics. The owner confirmed the character was correctly framed and Idle_Loop appeared correct. The owner chose not to retain a history screenshot because it would duplicate the existing ChronoFall character checkpoint.
  - pm doctor passed in ChronoFall, Starfall, and Royale; family inspection returned all three available/readable/write-trusted with zero warnings. git diff --check passed, generated output remains ignored/untracked, and no sibling or coordinator source change is included.