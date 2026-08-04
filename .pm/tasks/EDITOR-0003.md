---
id: EDITOR-0003
title: Establish editor and Balance Lab boundaries
track: EDITOR
milestone: M2
dependsOn:
- ARCH-0004
- BUILD-0002
createdAt: 2026-08-01T05:46:47.4219030Z
modifiedAt: 2026-08-04T11:26:30.9743070Z
---

Define separate authoring editor and headless Balance Lab processes. Authoring representations may compile to compact runtime data; do not create a reflective Unity-style runtime component system.

## Boundary requirements

- Share deterministic authoritative rules, camp definitions, and spawn/replenishment policy models where appropriate without sharing editor UI or a runtime service.
- Keep the Balance Lab headless and free of SDL, GPU, ImGui, rendering, editor UI, and presentation assets.
- Keep authoring representation separate from compact world-simulation data.
- Treat the future Angular/ASP.NET operations application as a distinct control-plane product, not part of the content editor or Balance Lab.
- Do not implement operations infrastructure in this task.

## Notes

- 2026-08-04 11:26 UTC - Implemented the approved Editor and Balance Lab boundary contract.

  - Created pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/editor-and-balance-lab-boundaries and linked it from architecture/overview and development/repository-workflow.
  - Kept Starfall.Editor and Starfall.BalanceLab as libraries for this task; EDITOR-0004 owns any later Balance Lab executable-host decision and scaffolding.
  - Recorded that authoritative and presentation outputs derive from the same fully validated authoring revision, use stable cross-output identities, and emit only after complete validation; runtime consumers never depend on editor document or object identity.
  - Added a recursive Balance Lab built-output architecture test rejecting Client, Editor, Protocol, World-host, ChronoFall character presentation, SDL/GPU/ImGui/Blurg/rendering, shader, texture, and image artifacts.
  - Confirmed Debug and Release Balance Lab outputs contain only BalanceLab, Content, and Simulation assemblies/symbols plus the BalanceLab deps file.
  - Validation: pm doctor passed; dotnet restore passed; Debug and Release builds passed with 0 warnings/errors; Debug and Release test runs each passed 142 tests (25 architecture, 42 client, 14 content, 24 protocol, 37 world); scoped dotnet format verification passed for FoundationDependencyTests.cs; git diff --check passed; linked family reread returned 3 available/readable/write-trusted projects and 0 warnings.
  - The solution-wide formatter additionally reports pre-existing whitespace diagnostics in unchanged World admission/lifecycle files and upstream SDL naming warnings. Those unrelated files were not modified.
  - No project references, output types, public APIs, schemas, runtime formats, UI, executable hosts, service infrastructure, native behavior, or visual output changed.