---
id: PROTOCOL-0009
title: Add physical drop facts and serialization
track: PROTOCOL
milestone: M2
dependsOn:
- GAME-0004
- PROTOCOL-0004
- PROTOCOL-0007
createdAt: 2026-08-03T07:29:09.6478270Z
modifiedAt: 2026-08-03T07:29:43.4470830Z
---

Add the focused bidirectional Draft 0 physical-drop protocol extension.

Acceptance criteria:
- Define and deterministically encode bounded drop state, collection intent, reservation, expiry, success and rejection facts.
- Preserve stable identity, explicit ordering and malformed-input rejection.
- Combine facts and serialization as one cohesive extension of the established envelope.
- Do not implement drop rules, presentation, inventory/equipment, persistence, trade or economy.