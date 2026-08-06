---
id: CLIENT-0030
title: Add the Starfall development debug shell
track: CLIENT
milestone: M4
dependsOn:
- CLIENT-0029
createdAt: 2026-08-06T06:41:23.7475280Z
modifiedAt: 2026-08-06T14:04:51.6979890Z
---

Provide the compact debug menu, concern-specific windows, F12 visibility toggle, --debug-ui-hidden launch behavior, correct input capture and gameplay suppression, and minimal in-memory window state. Local layout persistence is excluded from v1 unless trivial and separately approved.

## Notes

- 2026-08-06 14:04 UTC - Implemented the Starfall-owned development debug shell in interactive local and connected Client previews.

  Scope and contracts:
  - Added a compact Debug menu with independent read-only World / Session and Presentation / Rendering windows; no feature commands, console, permanent HUD, generic data dump, or disk persistence.
  - Added non-repeated F12 whole-shell visibility and the interactive-only --debug-ui-hidden modifier. Window choices remain process-local and survive whole-shell hide/show.
  - Routed SDL events through ImGui first, kept OS close and F12 global, and suppressed conflicting pointer, keyboard, text, Escape, camera, speed, move and Basic Arrow handling from capture ownership.
  - Injected ImGui's bitmap default development font. Native inspection exposed that Starfall's depth-enabled scene pass was incompatible with the shared backend's color-only SDL GPU pipeline; the Client now records ImGui last through a separate color-load pass after the unchanged scene pass.
  - Character validation and hidden deterministic capture remain backend-free. World, Simulation, Protocol, Content, Editor and Balance Lab remain ImGui-free.

  Automated validation:
  - Debug solution suites: 415 tests pass. One loopback test initially encountered a transient UDP bind failure immediately after manual World shutdown; its two-test project passed on the required isolated rerun.
  - Release solution build: 0 warnings, 0 errors.
  - Release solution suites: 415 tests pass.
  - Client content probe: quaternius-ual1-standard, 65 joints, Idle_Loop/Walk_Loop/Sword_Attack.
  - Hidden seven-view capture suite retained exact fingerprints: e668208dea46f904, 9b08f56034585eaf, 8391b2cb4c089c7e, 31512e8b5b2094c0, e5beeedc16cd627f, 0a4f95e2d861170d, a017df7907dcbf97.

  Owner native validation on macOS ARM64:
  - Confirmed readable menu/window text after the color-only overlay correction.
  - Confirmed independent windows, F12 hide/show, capture-safe input, normal outside-UI controls and resizing in local mode.
  - Confirmed --debug-ui-hidden starts cleanly and F12 reveals the shell.
  - Confirmed connected Ready/session/entity/tick diagnostics, input capture, movement, Basic Arrow, resizing and clean close against a fresh loopback World.
  - Owner accepted the behavior. No history artifact was requested.