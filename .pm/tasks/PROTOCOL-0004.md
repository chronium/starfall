---
id: PROTOCOL-0004
title: Implement vertical-slice protocol serialization
track: PROTOCOL
milestone: M2
dependsOn:
- PROTOCOL-0002
- PROTOCOL-0003
createdAt: 2026-08-02T07:31:45.1162170Z
modifiedAt: 2026-08-02T15:52:42.6516720Z
---

Implement deterministic serialization for the bounded Draft 0 protocol contract.

Acceptance criteria:
- Serialize the approved movement, targeting, Basic Arrow, Fire Arrow, Arrow Rain, monster, resource, damage, death, protected-town, respawn, and progression facts.
- Preserve integer authoritative resources, fixed ticks, stable action identities, explicit resolve timing, deterministic victim ordering, and bounded collection sizes.
- Reject malformed, out-of-range, ambiguous, or unsupported values deterministically.
- Test round trips and malformed inputs without embedding simulation rules in protocol code.
- Do not introduce authoritative projectile entities, chat delivery, persistence, or a generic messaging framework.