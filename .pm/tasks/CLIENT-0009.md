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
modifiedAt: 2026-08-04T14:56:30.1020880Z
---

Connect the runnable Starfall client to one admitted world session and replace the local movement fixture with authoritative player snapshots.

Planning prerequisite:
- Before activation, plan and complete the coordinator-owned shared transport boundary, then adopt and wire it through a focused Starfall continuation. SERVER-0005 proves only the bounded in-process World exchange; current PM dependencies do not yet represent this unallocated cross-project prerequisite.

Acceptance criteria:
- Send approved ground-point movement intent and consume bounded snapshots/corrections from SERVER-0005 through the approved transport boundary.
- Translate protocol facts into the exact Client-owned snapshot/fact-to-presentation adapter proven by CLIENT-0021; do not create a parallel movement presentation path.
- Preserve finite single-precision metre spatial facts, stable identity, fixed ticks and authoritative stale-fact/reconciliation handling.
- Keep monsters, combat, drops, progression, equipment, persistence, lobby UI, quantization and client gameplay authority out of scope.