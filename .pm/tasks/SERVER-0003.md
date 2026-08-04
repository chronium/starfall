---
id: SERVER-0003
title: Admit world joins and own gameplay sessions
track: SERVER
milestone: M2
dependsOn:
- SERVER-0002
- PROTOCOL-0002
createdAt: 2026-08-02T07:29:12.5879430Z
modifiedAt: 2026-08-04T10:18:09.0294410Z
---

Implement the completed signed world-join ticket consumption path, its narrow host-specific admission exchange, and world-owned active gameplay sessions.

Acceptance criteria:
- Bind only the PROTOCOL-0002 join request and accept/reject facts to the World host boundary; receive one bounded join request and return the approved admission result without introducing a general networking or framing framework.
- Validate the PROTOCOL-0002 ticket contract, enforce atomic single consumption and bind the admitted character/session to the intended world lifecycle.
- Cover expiry, replay protection, failure responses and continued active gameplay when identity/lobby, chat or operations are unavailable.
- Remain independent of PROTOCOL-0004 connected-walking serialization and all movement/combat exchange.
- Do not implement accounts, lobby UI, chat, persistence topology, movement, combat, generic service hosting or general transport infrastructure.

## Notes

- 2026-08-04 10:18 UTC - Implemented the bounded World admission and gameplay-session ownership path.

  Behavior and boundaries:
  - Added an internal in-process World exchange over the existing PROTOCOL-0002 request and accept/reject facts; no Protocol type or serialization changed.
  - Cryptographic validation uses an explicit Unix-millisecond clock and an injected public verification-key ring before entering the synchronized world lifecycle gate.
  - The runtime atomically rechecks Running state, prunes elapsed replay records, consumes one ticket ID, and creates one lifecycle-local gameplay session bound to the admitted account, character and world-instance identities.
  - Concurrent replay attempts accept exactly once. Consumed IDs remain through ticket expiry plus the approved five-second skew and are lazily pruned by later cryptographically valid admission attempts.
  - Draining rejects new joins while retaining sessions and fixed-step execution. Stop clears remaining in-memory session and replay state.
  - The raw bearer ticket is never retained. Active sessions have no identity, chat or operations dependency.
  - The command-line host still configures no keys and exposes no network transport. Production key provisioning, transport security/framing, persistence, player state, movement and combat remain outside this task.

  Validation:
  - Focused Starfall.World build passed with 0 warnings/errors; focused World tests passed 37/37.
  - Full Debug solution restore/build passed with 0 warnings/errors; all 145 tests passed (Architecture 24, Client 42, Content 14, Protocol 24, World 37).
  - Full Release solution build passed with 0 warnings/errors; the same 145 tests passed.
  - Scoped dotnet format verification passed with no changes.
  - Finite headless run loaded the exact Draft 0 layout and stopped at tick 60 with zero catch-up clamps.
  - Persistent headless run loaded the same layout, handled Ctrl+C at tick 326, and drained/stopped with zero catch-up clamps.
  - World project references remain exactly Content, Protocol and Simulation. Its Debug output contains no SDL, GPU, ImGui, editor, client, shader, texture or shared presentation artifact.
  - pm doctor, linked-family inspection (3 available/readable/trusted members, 0 warnings), and git diff --check passed.

  Documentation:
  - Updated README and the admission, service-availability and world-lifecycle wiki pages.
  - All wiki mutation receipts identified Starfall project prj_pkIpzx0fzFD4URjvqBuYrGZF and only their expected page paths.

  No visual checkpoint or owner visual validation applies to this headless-only task.
