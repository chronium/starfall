---
id: PROTOCOL-0004
title: Implement vertical-slice protocol serialization
track: PROTOCOL
milestone: M2
dependsOn:
- PROTOCOL-0002
- PROTOCOL-0003
createdAt: 2026-08-02T07:31:45.1162170Z
modifiedAt: 2026-08-02T18:27:44.0983040Z
---

Implement deterministic serialization for the bounded Draft 0 protocol contract.

Acceptance criteria:
- Serialize the approved movement, targeting, Basic Arrow, Fire Arrow, Arrow Rain, monster, spatial, resource, damage, death, protected-town, respawn and progression facts.
- Preserve integer authoritative resources, fixed ticks, stable identities, explicit resolve timing, deterministic victim ordering and bounded collection sizes.
- Initially serialize the actual finite single-precision IEEE-754 metre components used by the Box3D-native simulation; protocol-boundary quantization remains a later bandwidth optimization and must not create a second authoritative coordinate system.
- Reject malformed, non-finite, out-of-range, out-of-zone, ambiguous or unsupported values deterministically.
- Test round trips and malformed inputs without embedding simulation rules in protocol code.
- Do not introduce authoritative projectile entities, chat delivery, persistence or a generic messaging framework.