---
id: SERVER-0002
title: Run a headless fixed-step world/channel lifecycle
track: SERVER
milestone: M2
dependsOn:
- BUILD-0003
createdAt: 2026-08-01T05:46:47.9049220Z
modifiedAt: 2026-08-04T08:27:55.2094370Z
---

Implement one headless world/channel process lifecycle with deterministic fixed-step scheduling, start/drain/stop behavior, isolated world identity, and empty authoritative state ownership. Prove that the world host has no client presentation, SDL, GPU, ImGui, editor UI, chat, identity, operations, or persistence hot-path dependency. Admission and zone/entity hosting are separate tasks.

## Notes

- 2026-08-04 08:27 UTC - Implemented the bounded empty authoritative world/channel lifecycle.

  Behavior and contracts:
  - Starfall.World now requires explicit Protocol-owned `--world` and `--channel` identities and creates a fresh non-empty `WorldInstanceId` per invocation.
  - The isolated runtime owns `Created -> Running -> Draining -> Stopped`, a per-instance integer tick, and an admission-eligibility seam that is true only while running.
  - Authoritative scheduling is fixed at 60 Hz. Real-time execution uses a monotonic accumulator, at most five catch-up ticks per outer-loop cycle, one-step backlog clamping and a reported clamp count.
  - `--run-ticks <positive>` advances exactly the requested ticks without wall-clock pacing. Ctrl+C drains and stops the persistent host.
  - No zone, entities, sessions, Box3D, network, persistence, chat, operations infrastructure or presentation dependency was introduced.

  Validation:
  - Added Starfall.World.Tests with 29 passing tests covering parsing, required/duplicate/malformed inputs, lifecycle transitions, independent identity/tick ownership, exact finite runs, cancellation and accumulator cap/clamp behavior.
  - Complete Debug suite: 133 passed (Architecture 24, Client 42, Content 14, Protocol 24, World 29).
  - Complete Release suite: 133 passed with the same project counts.
  - Debug and Release solution builds succeeded with 0 warnings and 0 errors.
  - Scoped `dotnet format --verify-no-changes` passed for World, World tests and architecture tests.
  - Native headless finite run produced READY/DRAINING/STOPPED for exactly 60 ticks with one stable instance identity and zero clamps.
  - Native persistent run handled Ctrl+C at tick 253, then produced DRAINING/STOPPED with reason=shutdown and zero clamps.
  - Invalid `--run-ticks 0` exited with code 2 and a deterministic diagnostic.
  - Architecture validation proves the exact solution graph, finite process output, required identities, and presentation-free World output.
  - `pm doctor`, linked-family inspection (3 available/readable/trusted members, 0 warnings), and `git diff --check` passed.

  Documentation:
  - Updated README and repository workflow commands.
  - Added `architecture/world-channel-lifecycle`.
  - Updated architecture overview and service-availability ownership to distinguish this lifecycle evidence from admission, gameplay, operations and final deployment topology.

  No visual checkpoint applies to this headless task.