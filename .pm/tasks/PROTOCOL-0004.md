---
id: PROTOCOL-0004
title: Serialize connected walking commands and player snapshots
track: PROTOCOL
milestone: M2
dependsOn:
- PROTOCOL-0003
createdAt: 2026-08-02T07:31:45.1162170Z
modifiedAt: 2026-08-03T07:30:50.6105630Z
---

Implement deterministic serialization for the bounded connected-walking contract.

Acceptance criteria:
- Serialize approved movement intent, stable identity, fixed-tick player snapshots and correction facts.
- Preserve finite Box3D-native single-precision metre components, integer discrete state, explicit sequencing and bounded collection sizes.
- Reject malformed, non-finite, out-of-range, out-of-zone, ambiguous or unsupported values deterministically.
- Test round trips and malformed inputs without embedding movement or gameplay rules in Protocol.
- Keep monster, combat, progression, drop and inventory/equipment extensions in their focused later tasks.
- Do not add quantization, authoritative projectiles, chat, persistence or a generic messaging framework.