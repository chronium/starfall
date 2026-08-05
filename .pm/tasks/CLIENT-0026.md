---
id: CLIENT-0026
title: Present issued movement-target feedback
track: CLIENT
priority: none
dependsOn:
- CLIENT-0009
- CLIENT-0020
- CONTENT-0015
createdAt: 2026-08-05T19:25:06.4917930Z
modifiedAt: 2026-08-05T19:25:14.7637060Z
---

Present the player's issued movement destination as a bounded client-owned ground marker without changing movement authority.

Acceptance criteria:
- Reuse the connected movement command sequence, acknowledgement, correction, and ground-picking path; do not introduce a second destination or movement state.
- Show the exact selected Kenney Crosshair Pack image on a small alpha-blended ground-aligned textured quad after a movement intent is issued.
- Define deterministic requested, acknowledged, corrected/rejected, replaced, arrived, timeout, and fade behavior from existing Client/Protocol facts; the marker never asserts that a destination was accepted before authoritative evidence.
- Use a presentation-only layer/depth treatment that avoids z-fighting without modifying Content coordinates, collision, navigation, or picking.
- Retain clean-clone launch and diagnostics when marker content is absent, corrupt, unsupported, or unresolved.
- Validate native readability at the established camera distances and avoid obscuring the technical player, monsters, routes, or target selection.
- Do not add decals, terrain projection, path previews, footsteps, navigation, command prediction, a general effects system, or combat presentation.
- Before activation, attach and complete the future canonical coordinator acquisition dependency produced from CONTENT-0015's exact selection.