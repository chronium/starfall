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
modifiedAt: 2026-08-04T17:52:01.0240060Z
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
- 2026-08-04 17:49 UTC - Review continuation: confirmed that valid cross-channel reordering could deliver the initial sequenced movement snapshot before the reliable-ordered admission acceptance, causing the Client to abort; confirmed admission request encoding replaced non-ASCII UTF-16 characters with `?` before validation; and confirmed stale durable lifecycle/first-playable prose still described networking as future or absent. Reopened CLIENT-0009 to retain a valid early initial snapshot until acceptance, reject noncanonical source ticket text before ASCII encoding, add focused regression tests, and correct only the directly attributable wiki statements.
- 2026-08-04 17:52 UTC - Review corrections completed. The Client now accepts only a well-formed sequenced movement snapshot from the expected peer/channel before admission acceptance, retains the newest valid snapshot, and keeps `IsReady` false until the reliable-ordered acceptance arrives; pre-admission corrections and malformed/misrouted data remain failures. The admission encoder validates the original UTF-16 ticket characters before ASCII encoding, so non-ASCII source text throws `ArgumentException` without substitution. Durable lifecycle, connected-walking, and Draft 0 pages now describe the completed socket milestone and cross-channel reordering behavior accurately.

  Validation: focused Client tests passed 47/47; focused Protocol tests passed 50/50; full Debug and Release builds succeeded with zero warnings/errors; all 230 tests passed in each configuration (Architecture 37, Client 47, ConnectedWalking 1, Content 14, Protocol 50, Simulation 16, World 65). No new native or visual validation was required because this follow-up changes admission ordering/error handling and documentation without changing rendering, controls, movement, collision, camera, or animation.