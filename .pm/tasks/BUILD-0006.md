---
id: BUILD-0006
title: Adopt shared network transport in Starfall process hosts
track: BUILD
milestone: M2
priority: low
dependsOn:
- BUILD-0003
- BUILD-0005
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0023
createdAt: 2026-08-04T16:34:38.4151040Z
modifiedAt: 2026-08-04T16:42:25.8712180Z
---

Adopt the completed coordinator-owned low-level network transport through the two Starfall process composition roots before connected walking.

Acceptance criteria:
- Depend canonically on pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0023 and preserve the existing Starfall foundation/reference-policy prerequisites.
- Reference only $(ChronoFallFamilyRoot)src/ChronoFall.Network.Transport.LiteNetLib/ChronoFall.Network.Transport.LiteNetLib.csproj directly from Starfall.Client and Starfall.World, with ShouldUnsetParentConfigurationAndPlatform=false. The BCL contracts project and pinned LiteNetLib source remain transitive.
- Add focused internal Client and World composition factories returning INetworkTransport backed by LiteNetLibNetworkTransport. Do not add a shared Starfall networking project or change the local product dependency graph.
- Extend executable architecture checks so Client and World have exact, independently enforced family-source allowlists while Simulation retains only the approved Box3D boundary.
- Prove Content, Protocol, Simulation, Editor and Balance Lab do not reference or emit the network transport, and prove World remains free of client, SDL, GPU, ImGui, rendering, editor and presentation artifacts.
- Add focused source-consumption tests for both process-local factories and inspect Client/World build outputs.
- Record the Starfall ownership boundary and wire this task as a real prerequisite of CLIENT-0009.

This task does not open runtime listeners, connect peers, define Starfall channels, delivery policy, frames or admission serialization, provision keys, bind peers to gameplay sessions, change disconnect/reconnect policy, implement encryption, or alter Client/World runtime behavior. Those product-specific decisions remain with a separately planned CLIENT-0009 cycle. Do not activate CLIENT-0009, modify Royale, push, or begin another task.

## Notes

- 2026-08-04 16:42 UTC - Implemented the approved family-source transport adoption without changing runtime behavior. Starfall.Client and Starfall.World now directly reference only the coordinator LiteNetLib adapter through ChronoFallFamilyRoot and each owns an internal INetworkTransport factory. Architecture tests enforce exact per-process family allowlists and require the contracts, adapter and LiteNetLib assemblies only in Client and World outputs; the remaining product outputs stay transport-free and World stays presentation-free.

  Validation:
  - dotnet restore Starfall.slnx: passed.
  - Debug build: passed with 0 warnings and 0 errors.
  - Debug tests: 214 passed (Protocol 45, Content 14, Simulation 16, Client 43, World 61, Architecture 35).
  - Release build: passed with 0 warnings and 0 errors.
  - Release tests: 214 passed with the same project counts.
  - Focused dotnet format verification for all modified C# files: passed.
  - Full-solution format verification reaches pinned coordinator third-party sources and reports pre-existing LiteNetLib whitespace/encoding and SDL3-CS naming diagnostics; no third-party files were changed.
  - Release output inspection confirmed ChronoFall.Network.Transport.dll, ChronoFall.Network.Transport.LiteNetLib.dll and LiteNetLib.dll in Client and World only.
  - PM validation and pm doctor passed; linked family inspection returned no warnings.
  - git diff --check passed.

  Durable ownership is recorded in architecture/network-transport-adoption and the architecture, repository-workflow and bootstrap pages. CLIENT-0009 now has BUILD-0006 as a real prerequisite and retains all Starfall-specific packet, channel, admission, polling, session, reconnect and development-security decisions.