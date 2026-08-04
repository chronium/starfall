---
id: CLIENT-0009
title: Connect and synchronize the walking player
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0021
- SERVER-0005
- PROTOCOL-0004
- BUILD-0006
createdAt: 2026-08-02T07:31:45.8634390Z
modifiedAt: 2026-08-04T16:34:54.3931190Z
---

Connect the runnable Starfall client to one admitted world session and replace the local movement fixture with authoritative player snapshots.

Prerequisite:
- BUILD-0006 adopts the completed coordinator-owned shared transport through the Starfall Client and World composition roots. This task must not activate until that dependency is complete.

Planning contract:
- The owner-approved CLIENT-0009 plan must define Starfall-owned channel and delivery assignments, admission request/accept/reject serialization, Client and World polling/composition, transport-peer to gameplay-session binding, disconnect/reconnect behavior, development key provisioning, and the protected-development-transport stance. Do not infer these product decisions from the opaque shared transport.

Acceptance criteria:
- Send approved ground-point movement intent and consume bounded snapshots/corrections from SERVER-0005 through the approved transport boundary.
- Translate protocol facts into the exact Client-owned snapshot/fact-to-presentation adapter proven by CLIENT-0021; do not create a parallel movement presentation path.
- Preserve finite single-precision metre spatial facts, stable identity, fixed ticks and authoritative stale-fact/reconciliation handling.
- Keep monsters, combat, drops, progression, equipment, persistence, lobby UI, quantization and client gameplay authority out of scope.