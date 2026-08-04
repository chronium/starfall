---
id: SERVER-0005
title: Exchange walking commands and player snapshots
track: SERVER
milestone: M2
dependsOn:
- SERVER-0003
- SERVER-0006
- SIM-0008
- PROTOCOL-0004
createdAt: 2026-08-02T07:31:45.6180010Z
modifiedAt: 2026-08-04T14:59:36.8426920Z
---

Connect admitted gameplay sessions to the proven connected-walking protocol through a narrow in-process World-host exchange.

Acceptance criteria:
- Successful admission atomically creates one technical world-owned player at the configured respawn anchor and binds its immutable entity identity to the gameplay session; rejected or replayed admission creates no player.
- Decode connected-walking command payloads using the existing protocol codec, resolve the host-context session, and route valid newer non-zero intent sequences to authoritative movement for that session's player.
- Accept sequence gaps, ignore stale or duplicate commands, and return bounded dispositions for malformed payloads and unknown sessions without mutating gameplay state or sequencing.
- Treat accepted commands as acknowledged by the next routine snapshot. For a newer command rejected by authoritative spatial validation, immediately return one correction whose acknowledgement equals the corrected intent sequence.
- Publish an initial snapshot at the session's current admission tick, including tick zero, then at most one latest routine snapshot per session per later fixed tick; do not queue skipped snapshot history.
- Order routine publications by monotonic player entity ID. Allocate checked monotonic per-session snapshot sequences beginning at one; fail explicitly on exhaustion without wrapping or reuse.
- Map World and Simulation state one-to-one into protocol facts and canonicalize signed zero before encoding.
- Preserve active sessions, bound players, command handling, and publication while draining. Stopping clears them. Prevent the technical removal seam from removing a session-bound player.
- Preserve session/world isolation and continued world operation during identity, chat and operations outages.
- Keep the exchange in-process. Do not add sockets, transport framing, Client changes, monsters, combat, progression, drops, inventory/equipment, persistence, multiple worlds, chat, or a generic hosting/message framework.
- Record that CLIENT-0009 still requires separately planned shared transport adoption before activation.

## Implementation notes

- Added immutable admitted-session player binding plus lock-confined per-session intent acknowledgement, publication tick and checked snapshot-sequence state.
- Added a narrow in-process World walking exchange over the existing PROTOCOL-0004 codecs. No Client, socket, framing or generic transport implementation was introduced.
- Added focused admission and walking-exchange coverage for initial/current-tick publication, latest-only capture, accepted gaps, stale/duplicate suppression, authoritative corrections, malformed/unknown inputs, session isolation and ordering, drain/stop behavior, bound-player removal protection, signed-zero canonicalization and sequence exhaustion.
- Updated the World lifecycle, connected-walking, service-availability, architecture overview and Client adapter wiki contracts. CLIENT-0009 remains todo and now records its unallocated shared-transport prerequisite.

Validation:
- `dotnet build Starfall.slnx -c Debug -m:1 --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test Starfall.slnx -c Debug -m:1 --no-restore --no-build`: 207 passed.
- `dotnet build Starfall.slnx -c Release -m:1 --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test Starfall.slnx -c Release -m:1 --no-restore --no-build`: 207 passed.
- Finite 60-tick World run completed through running, draining and stopped with one standalone technical player and zero catch-up clamps.
- Release World artifact/dependency inspection found no SDL, GPU, ImGui, editor or character-presentation contamination.
- `pm doctor`, linked-family inspection and `git diff --check`: passed; family warnings: 0.
- `dotnet format --verify-no-changes` reports only the existing pinned SDL3-CS IDE1006 naming warnings and exits 2; no Starfall formatting diagnostic remains.