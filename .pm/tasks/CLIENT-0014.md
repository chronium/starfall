---
id: CLIENT-0014
title: Present and interact with the provisional inventory
track: CLIENT
priority: none
dependsOn:
- GAME-0003
- PROTOCOL-0010
- SERVER-0011
createdAt: 2026-08-02T07:52:11.6712840Z
modifiedAt: 2026-08-06T06:44:37.4810330Z
---

Present and interact with the authoritative fixed-slot Inventory after the permanent GUI foundation needed by this surface exists.

Acceptance criteria:
- Show one provisional player inventory and its stable item/slot identities.
- Support visible selection, move and swap intent plus full/invalid rejection and authoritative correction.
- Add only the container, focus, selection, disabled/rejection and correction UI required by this proof.
- The future native validation task may use development-injected items, but Inventory itself does not depend on the console.
- Do not implement equipment slots, physical-drop pickup, modular armour rendering, persistence, trading, crafting or a general GUI framework.