---
id: CLIENT-0009
title: Connect to the world and synchronize the first zone
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0005
- CLIENT-0006
- SERVER-0005
- PROTOCOL-0004
createdAt: 2026-08-02T07:31:45.8634390Z
modifiedAt: 2026-08-02T18:27:44.1069470Z
---

Connect the runnable Starfall client to one world session, send approved movement intent, consume snapshots and authoritative events, reconcile the local presented character, and display the synchronized first-zone state through the shared presentation foundation.

Consume finite single-precision metre spatial facts matching the server's Box3D-native precision. Client prediction may use the same fixed-step simulation only when it has the same inputs, initial state, creation order, Box3D build and application ordering; network latency still requires authoritative snapshot reconciliation.

Do not add combat presentation, drops, progression UI, persistence, lobby UI, protocol quantization, a parallel fixed-point coordinate model or client gameplay authority.