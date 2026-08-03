---
id: CLIENT-0005
title: Prototype isometric mouse-driven control
track: CLIENT
milestone: M2
dependsOn:
- BUILD-0003
- CONTENT-0006
createdAt: 2026-08-01T05:46:48.1349880Z
modifiedAt: 2026-08-03T11:00:43.1325520Z
---

Prototype the Draft 0 isometric mouse-driven camera and movement input boundary.

Acceptance criteria:
- Provide the provisional isometric camera and deterministic screen-to-ground mapping.
- Use left-click on valid ground to produce movement intent only.
- Keep authoritative movement, collision, and acceptance on the server.
- Reserve right-click enemy and skill-key behavior for the connected combat-intent task.
- Do not add third-person controls, gameplay authority, combat, a general camera framework, or final interaction polish.

## Notes

- 2026-08-03 10:55 UTC - Implemented the approved perspective-isometric control proof with a fixed 28-degree vertical FOV, 42-degree downward pitch, 45-degree yaw from +X/+Z, 45 m focus distance, 0.1/300 m clip planes, and deterministic inverse-view-projection picking onto Y=0. Added a shared fact-to-presentation input boundary that emits GroundMovementIntent without moving or mutating the character. Added Starfall.Client.Tests and 14 focused camera, projection, picking, bounds, HiDPI, and malformed-input checks. Validation passed: Debug and Release builds; 75 Debug and 75 Release tests (Architecture 23, Client 14, Content 14, Protocol 24); dotnet format verification; non-graphical character-content probe; and git diff --check. Native owner validation on 2026-08-03 confirmed that framing and controls look correct. Captured left-click diagnostics produced finite, plausible in-zone coordinates around the (100,100) focus, including (100.848,103.529), (92.186,96.478), and (106.776,93.816); right-click did not emit movement intent, and Escape closed the preview. No movement, networking, simulation, map rendering, asset selection, or downstream task was implemented.
- 2026-08-03 11:00 UTC - Reviewed continuation after native use on a 3440 x 1440 display: changed the default development window from 960 x 720 to 1920 x 1080 and reduced the fixed camera focus distance from 45 m to 22.5 m so the technical humanoid is readable at the larger viewport. Preserved the 28-degree FOV, 42-degree pitch, 45-degree yaw, deterministic inverse-view-projection picker, intent-only input boundary, and fixed-camera exclusions. Updated the focused settings/round-trip tests, README, and Draft 0 wiki contract. The owner visually approved the revised framing on 2026-08-03. Validation passed with a clean focused Debug build, 14 Client tests, and the full 75-test suite in both Debug and Release; the full Release build completed with 0 warnings and 0 errors.