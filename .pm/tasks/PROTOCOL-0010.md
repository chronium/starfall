---
id: PROTOCOL-0010
title: Add inventory facts and serialization
track: PROTOCOL
priority: none
dependsOn:
- GAME-0003
- PROTOCOL-0015
createdAt: 2026-08-03T07:29:09.8915960Z
modifiedAt: 2026-08-06T10:20:20.1690260Z
---

Define and deterministically serialize the bounded provisional Inventory contract.

Acceptance criteria:
- Carry inventory state, stable item and slot identity, move/swap intent, correction, full/invalid rejection and deterministic ordering.
- Reject malformed, unsupported, ambiguous and out-of-bound payloads.
- Combine facts and serialization as one focused extension of the established connected envelope.
- Do not include equipment, physical-drop state, item rules, presentation, persistence, trade, crafting or a generic item protocol.