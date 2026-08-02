---
id: CLIENT-0014
title: Add inventory and equipment interaction
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0013
- GAME-0003
- GAME-0005
- PROTOCOL-0004
createdAt: 2026-08-02T07:52:11.6712840Z
modifiedAt: 2026-08-02T07:52:35.0759120Z
---

Present authoritative starter inventory and equipped-slot state, allow bounded select/equip/unequip interaction, send intent, and reconcile ownership, compatibility, replacement, stat-change, and rejection events. Keep item rules server-authoritative. Do not implement modular armour rendering, weapon aim/IK, persistence, trading, crafting, or a general UI framework.