---
id: PROTOCOL-0014
title: Serialize authoritative mana facts
track: PROTOCOL
milestone: M6
dependsOn:
- SIM-0012
createdAt: 2026-08-06T06:41:22.7690090Z
modifiedAt: 2026-08-07T19:31:01.1389650Z
---

Define stable mana facts and deterministic serialization distinct from development commands, including current and maximum values, fixed ticks, corrections, exact bounds, and malformed-input rejection.

It consumes the delivered connection-level gameplay-protocol-v1 negotiation contract and does not define a Basic-specific compatibility layer.