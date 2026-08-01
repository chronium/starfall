---
id: BUILD-0002
title: Create MMO solution and repository foundation
track: BUILD
milestone: M0
dependsOn:
- ARCH-0004
createdAt: 2026-08-01T05:46:46.9425230Z
modifiedAt: 2026-08-01T06:49:24.2631000Z
---

Create the independently buildable Starfall repository and solution skeleton after architecture approval, with project boundaries, tests, launch/build lifecycle, repository policy, and PM/wiki sources of truth. Do not implement gameplay or service infrastructure.

## Architecture requirements

- Encode strict dependency boundaries for client presentation, headless world/server, authoritative simulation, protocol, content, editor/authoring, and Balance Lab.
- Preserve the logical identity/lobby, world, chat, operations, and persistence ownership contract without scaffolding one executable per logical boundary.
- Permit a small initial executable set, potentially identity/lobby and world only; final physical topology remains deferred.
- Keep world-session code independent of continuing identity authorization, chat delivery, and operations availability.
- Keep the future Angular/ASP.NET operations control plane separate from the editor and Balance Lab.
- Enforce that headless projects do not reference SDL, GPU, ImGui, rendering, editor UI, or presentation assets.