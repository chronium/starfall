---
id: SERVER-0015
title: Dispatch development commands through admitted sessions
track: SERVER
milestone: M4
dependsOn:
- SERVER-0005
- PROTOCOL-0013
createdAt: 2026-08-06T06:41:23.0233770Z
modifiedAt: 2026-08-06T15:52:02.7665180Z
---

Bind development commands to admitted gameplay sessions, dispatch every registered command for every admitted player, enforce per-session monotonic sequencing, return correlated results, and add the harmless ping_world handler. Remove the unused global availability/disabled wire paths. Exclude roles, permissions, administration, remote operations, gameplay feature commands, and speculative authorization machinery.

## Completion notes

- Development commands are available to every admitted gameplay session. No launch gate, disabled result, role, permission, or speculative authorization path remains.
- Channel 7 accepts reliable-ordered requests only after admission. The World owns an ordinal handler registry, consumes every fresh per-session sequence, returns correlated success or bounded rejection payloads on channel 8, and treats malformed payloads or wrong delivery as protocol violations.
- `ping_world` accepts no arguments and returns deterministic world, channel, tick, session, and player diagnostics. Handler exceptions are logged server-side and expose only a generic rejection to the client.
- The obsolete availability payload and disabled rejection were removed rather than retained behind compatibility decoding. The compact result wire kinds are success and rejection only.
- Unit coverage exercises protocol golden bytes and malformed inputs, dispatcher registration and sequencing, multi-session isolation, bounded handler failures, host routing and cleanup, and valid rejection behavior. A real UDP loopback proves admission followed by a correlated `ping_world` result.
- Validation: `dotnet restore Starfall.slnx`; `dotnet build Starfall.slnx --no-restore -m:1` (zero warnings and errors); `dotnet test Starfall.slnx --no-restore --no-build -m:1` (449 passed). An earlier full run encountered one transient existing Box3D fixed-step test failure; that test passed in isolation, the World suite then passed, and the final complete run passed cleanly.
- No native or visual checkpoint was required. The Client debug frontend remains owned by CLIENT-0031.
