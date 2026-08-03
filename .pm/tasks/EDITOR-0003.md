---
id: EDITOR-0003
title: Establish editor and Balance Lab boundaries
track: EDITOR
milestone: M2
dependsOn:
- ARCH-0004
- BUILD-0002
createdAt: 2026-08-01T05:46:47.4219030Z
modifiedAt: 2026-08-03T07:30:50.6222860Z
---

Define separate authoring editor and headless Balance Lab processes. Authoring representations may compile to compact runtime data; do not create a reflective Unity-style runtime component system.

## Boundary requirements

- Share deterministic authoritative rules, camp definitions, and spawn/replenishment policy models where appropriate without sharing editor UI or a runtime service.
- Keep the Balance Lab headless and free of SDL, GPU, ImGui, rendering, editor UI, and presentation assets.
- Keep authoring representation separate from compact world-simulation data.
- Treat the future Angular/ASP.NET operations application as a distinct control-plane product, not part of the content editor or Balance Lab.
- Do not implement operations infrastructure in this task.