---
id: CLIENT-0005
title: Prototype isometric mouse-driven control
track: CLIENT
milestone: M2
dependsOn:
- BUILD-0003
- CONTENT-0006
createdAt: 2026-08-01T05:46:48.1349880Z
modifiedAt: 2026-08-02T15:52:42.7098250Z
---

Prototype the Draft 0 isometric mouse-driven camera and movement input boundary.

Acceptance criteria:
- Provide the provisional isometric camera and deterministic screen-to-ground mapping.
- Use left-click on valid ground to produce movement intent only.
- Keep authoritative movement, collision, and acceptance on the server.
- Reserve right-click enemy and skill-key behavior for the connected combat-intent task.
- Do not add third-person controls, gameplay authority, combat, a general camera framework, or final interaction polish.