---
id: CLIENT-0009
title: Connect and synchronize the walking player
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0021
- SERVER-0005
- PROTOCOL-0004
createdAt: 2026-08-02T07:31:45.8634390Z
modifiedAt: 2026-08-03T07:30:50.6940480Z
---

Connect the runnable Starfall client to one admitted world session and replace the local movement fixture with authoritative player snapshots.

Acceptance criteria:
- Send approved ground-point movement intent and consume bounded snapshots/corrections from SERVER-0005.
- Translate protocol facts into the exact Client-owned snapshot/fact-to-presentation adapter proven by CLIENT-0021; do not create a parallel movement presentation path.
- Preserve finite single-precision metre spatial facts, stable identity, fixed ticks and authoritative reconciliation.
- Keep monsters, combat, drops, progression, equipment, persistence, lobby UI, quantization and client gameplay authority out of scope.