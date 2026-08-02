---
id: PROTOCOL-0004
title: Implement vertical-slice protocol serialization
track: PROTOCOL
milestone: M2
dependsOn:
- PROTOCOL-0002
- PROTOCOL-0003
createdAt: 2026-08-02T07:31:45.1162170Z
modifiedAt: 2026-08-02T07:32:00.5457260Z
---

Implement deterministic serialization and validation for the approved admission and gameplay protocol contracts, including envelopes, versioning, bounded payloads, commands, authoritative events, and snapshots. Add compatibility and malformed-input tests. Keep transport policy, chat, persistence, and game rules outside this task.