---
id: SERVER-0004
title: Load and own the provisional Draft 0 graybox
track: SERVER
milestone: M2
dependsOn:
- SERVER-0002
- CONTENT-0014
createdAt: 2026-08-02T07:29:12.8441180Z
modifiedAt: 2026-08-04T08:59:55.6949370Z
---

Load the provisional executable Draft 0 graybox into one authoritative world/channel.

Acceptance criteria:
- Load the bounded envelope, protected town, respawn anchor, proxy landmarks, route/camp regions, coarse collision/navigation and deterministic sample spawn inputs.
- Preserve finite single-precision metre components, stable identities and explicit ordering.
- Own the loaded world layout without yet creating player or monster state.
- Keep protection and gameplay behavior in Simulation rather than rendering or authoring data.
- Do not implement entities, movement, monsters, combat, progression, persistence, asset presentation, streaming, terrain systems or additional zones.

## Notes

- 2026-08-04 08:59 UTC - Implemented direct ownership of the immutable provisional Draft 0 graybox in each world/channel runtime.

  Behavior and boundaries:
  - `WorldChannelRuntime` now requires a non-null `Draft0GrayboxLayout` before lifecycle start.
  - The World composition root binds exactly `Draft0GrayboxCatalog.FirstPlayable`; no zone selector, file loader, serializer, copy format or parallel runtime map model was introduced.
  - The runtime preserves the exact Content envelope, town/respawn input, branch/route/camp order, seven proxy inputs and ten branch/local ordered sample spawns.
  - The deterministic `READY` line now reports zone/town identities and branch, route, proxy and spawn counts.
  - Loaded regions and proxies remain immutable inputs only. No protection enforcement, collision/navigation construction, Box3D, entities, spawning, movement, combat, persistence, presentation or networking was added.
  - The existing project-reference graph is unchanged and World remains presentation-free.

  Validation:
  - Focused World tests: 31 passed; new coverage proves exact catalog ownership/order, independent lifecycle/tick state with safely shared immutable definitions, and null-layout rejection.
  - Architecture tests: 24 passed, including the exact expanded `READY` contract and headless artifact boundary.
  - Complete Debug suite: 135 passed (Architecture 24, Client 42, Content 14, Protocol 24, World 31).
  - Complete Release suite: 135 passed with the same project counts.
  - Debug and Release solution builds succeeded with 0 warnings and 0 errors.
  - Scoped formatting verification passed after rerunning outside the filesystem sandbox because Roslyn's first build-host named-pipe bind was denied by the sandbox.
  - Native finite run loaded the exact layout summary and stopped at tick 60.
  - Native persistent run loaded the same summary, handled Ctrl+C at tick 214, and drained/stopped with zero backlog clamps.
  - `pm doctor`, linked-family inspection (3 available/readable/trusted members, 0 warnings), and `git diff --check` passed.

  Documentation:
  - Updated README.
  - Updated `architecture/world-channel-lifecycle`, `architecture/overview`, and `content/draft-0-zone-contract` to distinguish direct provisional Content consumption from later authored-map, collision/navigation and gameplay behavior.

  No visual checkpoint or owner visual validation applies to this headless-only task.