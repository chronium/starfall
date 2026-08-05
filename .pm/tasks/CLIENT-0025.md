---
id: CLIENT-0025
title: Present semantic pointer-intent cursors
track: CLIENT
priority: none
dependsOn:
- CLIENT-0005
- CLIENT-0020
- CLIENT-0012
- CONTENT-0015
createdAt: 2026-08-05T19:24:38.6058590Z
modifiedAt: 2026-08-05T19:25:14.6477520Z
---

Present client-owned pointer feedback for the meaning of the next world click without changing movement or combat authority.

Acceptance criteria:
- Reuse the proven ground picking, Draft 0 graybox identities, and connected Basic Arrow target-selection seam; do not create a second click interpretation path.
- Classify the current world hover deterministically as hostile monster, blocking geometry, walkable ground, or invalid/no world target, with the most specific valid hit winning.
- Show the selected Kenney hostile-target, prohibited, and movement cursor for those states; use the ordinary safe fallback cursor for invalid, absent, corrupt, or not-yet-staged optional content.
- Treat hover feedback as advisory: attack feedback does not promise range or success, and blocked/movement feedback never replaces World validation or authoritative correction.
- Define exact cursor hotspots, logical-window/DPI behavior, focus loss, window leave/re-entry, and restoration of the ordinary cursor.
- Prefer SDL custom cursor support after exact acquired inputs are available; do not implement an in-scene cursor, generic interaction registry, gameplay targeting authority, UI framework, or Basic Arrow behavior.
- Before activation, attach and complete the future canonical coordinator acquisition dependency produced from CONTENT-0015's exact selection.