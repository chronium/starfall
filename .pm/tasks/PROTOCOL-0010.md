---
id: PROTOCOL-0010
title: Add inventory and equipment facts and serialization
track: PROTOCOL
milestone: M2
dependsOn:
- GAME-0003
- GAME-0005
- PROTOCOL-0009
createdAt: 2026-08-03T07:29:09.8915960Z
modifiedAt: 2026-08-03T07:29:43.4563090Z
---

Add the focused Draft 0 inventory/equipment protocol extension after authoritative item behavior exists.

Acceptance criteria:
- Define and deterministically encode bounded inventory/equipped state, select/equip/unequip intent, replacement, stat-change and rejection facts.
- Preserve stable identity, integer quantities/stats, explicit ordering and malformed-input rejection.
- Combine facts and serialization as one cohesive extension of the established envelope.
- Do not implement item rules, presentation, persistence, trade, crafting or a generic item protocol.