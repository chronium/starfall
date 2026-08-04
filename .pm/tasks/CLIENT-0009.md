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
modifiedAt: 2026-08-04T17:33:01.3984190Z
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

## Notes

- 2026-08-04 17:33 UTC - Implemented the first connected walking world through the approved shared transport boundary. Starfall now owns bounded admission request/accept/reject serialization, fixed channel and delivery assignments, loopback-only development hosting, ticket-to-session binding, caller-polled World exchange, authoritative movement commands/snapshots/corrections, disconnect cleanup, and a development P-256 key/ticket tool. The connected Client reuses TechnicalPlayerPresentationAdapter and the existing native presentation path; it does not add prediction, interpolation, or reconciliation smoothing. A later focused client-presentation task should use observed network jitter or correction snapping as evidence before adding smoothing.

  Validation: focused dotnet format completed for src/tests/tools; Debug and Release solution builds passed with zero warnings/errors; all 228 tests passed in each configuration (Architecture 37, Client 46, ConnectedWalking 1, Content 14, Protocol 49, Simulation 16, World 65), including a real UDP loopback admission/movement/correction/disconnect test. Coordinator and Starfall PM doctor passed, linked-family inspection returned three readable/write-trusted projects with zero warnings, git diff --check passed, Royale remained clean, and neither child gitlink changed during implementation. Native macOS ARM64 validation admitted one world-owned session, moved entity_1 from client intent through authoritative Box3D state and serialized snapshots, exercised collision correction, camera presets/zoom, animation, and clean client disconnect; the World logged the session ending and drained with zero players. Owner confirmed movement, collision correction, cameras, and animation all looked correct. The owner explicitly chose to skip visual-checkpoint preservation because a still image would not communicate the connected movement milestone.