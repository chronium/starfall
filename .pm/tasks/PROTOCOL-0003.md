---
id: PROTOCOL-0003
title: Define gameplay commands, events, and snapshot contract
track: PROTOCOL
milestone: M0
dependsOn:
- PROTOCOL-0002
createdAt: 2026-08-02T07:29:11.5694060Z
modifiedAt: 2026-08-02T07:30:17.2540230Z
---

Define the first vertical-slice gameplay protocol independently of lobby admission: client intent commands, authoritative events, snapshot identity and sequencing, baseline/delta policy, failure handling, and version boundaries. Keep combat, loot, progression, and equipment notifications on the game protocol; do not implement transport, chat, persistence, or game rules.