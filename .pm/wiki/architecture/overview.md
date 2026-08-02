---
title: Architecture Overview
createdAt: 2026-08-01T05:48:09.1031030Z
modifiedAt: 2026-08-02T12:09:40.9389410Z
---

## Purpose

Starfall is an independently owned and useful server-authoritative MMORPG inspired by classic MU Online. It remains a child of ChronoFall for family roadmap and shared-engine coordination but owns its PM project, source history, architecture, simulation, protocol, content, editor, build/release decisions, and commits. The canonical full-client development environment is the coordinator family checkout; independent ownership does not require an isolated full-client build.

## Planned boundaries

The foundation separates native client presentation, headless world orchestration, authoritative simulation, transport-neutral protocol, content, editor authoring, and a headless Balance Lab. It also preserves logical ownership boundaries for identity/lobby, realm/world, chat, operations, and persistence. `BUILD-0002` realized the approved boundaries as independently buildable .NET libraries and executable dependency tests. `BUILD-0003` makes only `Starfall.Client` and `Starfall.World` executable composition roots with bounded startup-and-exit behavior; later tasks still own the client presentation runtime and fixed-step authoritative world lifecycle.

Once admitted to a world, an active player's gameplay session does not depend on authentication, chat, or management services remaining available. Identity admits; the selected world consumes a short-lived signed join ticket and owns the gameplay session. Chat and operations remain optional from gameplay's perspective. Persistence degradation requires a later explicit contract.

Servers own world state, movement, combat, monsters, camps, progression, drops, equipment, and persistent-intent outcomes. Clients own input presentation, rendering, animation, IK, effects, cameras, and smoothing. Headless projects never depend on SDL windowing/GPU, ImGui, rendering, or editor code.

Logical boundaries do not initially require separate processes. Strict modules and a small number of executables are acceptable while the vertical slice gathers evidence for the final physical topology. Starfall may consume explicitly approved parent-owned shared projects from source through the canonical family checkout, but never depends on Royale. Parent shared modules never depend on Starfall.

## Foundation assembly graph

The initial direct project-reference graph is:

```text
Starfall.Content
Starfall.Protocol
Starfall.Simulation -> Content
Starfall.World -> Content, Protocol, Simulation
Starfall.Client -> Content, Protocol
Starfall.Editor -> Content
Starfall.BalanceLab -> Content, Simulation
```

Content and Protocol remain product-dependency-free. Simulation owns deterministic authoritative rules and does not depend on Protocol. World is the later headless orchestration boundary between protocol, content, and simulation. Client never references World or Simulation. Editor remains an authoring boundary, while Balance Lab consumes the same deterministic content and simulation without a live-world or presentation dependency.

The graph above records Starfall-local product dependencies. A later approved client task may add the allowlisted coordinator presentation projects as source references; those references do not change Starfall gameplay ownership and may never enter Content, Protocol, Simulation, World, Editor, or BalanceLab.

`Starfall.Client` and `Starfall.World` are executable boundaries; Content, Protocol, Simulation, Editor, and BalanceLab remain libraries. The two foundation shells accept no arguments, report deterministic startup, and exit without a runtime loop. This process split follows the client/server trust, platform, and authority boundary: it does not require identity, chat, operations, or persistence to become separate deployables. Changes to this graph require an approved task, updated dependency tests, and an updated Starfall architecture contract.

## Family source-consumption boundary

`Directory.Build.props` defines the single overridable `ChronoFallFamilyRoot` property. Its default resolves the parent coordinator directory in the canonical shallow family checkout. Only `Starfall.Client` may later reference the explicitly approved `ChronoFall.CharacterPresentation`, `ChronoFall.CharacterPresentation.Cooking`, and `ChronoFall.CharacterPresentation.SdlGpu` projects through that root.

Literal parent traversal, absolute checkout paths, arbitrary property-rooted dependencies, coordinator imports, direct Royale references, and direct SDL3-CS references are not approved. The coordinator retains ownership of shared source, its actual source-built SDL3-CS dependency, and the generated client cook/copy workflow. `CLIENT-0006` will own the Starfall references and consumption of ignored generated content after the coordinator contract is complete. NuGet/feed distribution remains deferred until real integrations or independent release and CI needs justify it.

## Initial vertical slice

The initial slice is one world/channel, one small zone, one class, shaped monster spots, basic attacks, one geometric AoE, experience, physical drops, visible equipment, and a Balance Lab using the same authoritative rules. Identity/lobby, chat, operations, and persistence implementation depth remain deferred even though their ownership and availability boundaries are defined. Trade stands, the full economy, wings progression, territory, and the complete public release also remain deferred.

Starfall service contract: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/service-availability-and-ownership`.

Family contracts: `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/wiki/architecture/shared-engine-boundaries`.