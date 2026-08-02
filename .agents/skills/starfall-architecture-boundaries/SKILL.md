---
name: starfall-architecture-boundaries
description: Protect Starfall project ownership, dependency direction, server authority, service availability, headless isolation, editor versus runtime data, and coordinator integration. Use for project references, new assemblies, gameplay/client boundaries, services, persistence, protocols, shared modules, or authoring designs.
---

# Starfall Architecture Boundaries

## Preserve The Foundation Graph

- Content and Protocol have no product-project dependencies.
- Simulation depends only on Content.
- World depends on Content, Protocol, and Simulation.
- Client depends on Content and Protocol; never World or Simulation.
- Editor currently depends only on Content.
- BalanceLab depends on Content and Simulation.

Treat changes to this graph as contract decisions. Update the dependency tests and architecture wiki with the approved task.

## Preserve Authority

Keep movement, combat, character state, monsters, camps, inventory, equipment, progression, drops, and session ownership authoritative in World and Simulation. Client input expresses intent; rendering, animation, IK, effects, UI, cameras, and smoothing only present authoritative outcomes.

Keep World, Simulation, and BalanceLab free of SDL, GPU, ImGui, rendering, editor UI, and presentation assets.

## Preserve Availability

An admitted gameplay session must continue without identity, chat, or operations. The world consumes a short-lived join ticket and owns the session. Gameplay-critical events use Protocol, not chat. Worlds/channels remain independent lifecycle and state owners.

Do not turn logical identity, chat, operations, or persistence boundaries into placeholder services or deployables. Physical topology and persistence degradation require their evidence-gated architecture task.

Keep camps inside authoritative world simulation. Share deterministic definitions and policies with Editor and BalanceLab without creating a camp service.

## Preserve Repository Ownership

Starfall owns game-specific code and may consume approved coordinator projects from source in the canonical family checkout. Root every such reference at the single `$(ChronoFallFamilyRoot)` property; never use literal parent traversal, absolute checkout paths, arbitrary property roots, or Royale references. Only the client may consume the currently approved character-presentation source set. Do not move Starfall gameplay, protocol, content, build, or release concerns into shared modules.

Independent repository and product ownership does not imply an isolated full-client build. Headless projects must remain buildable without coordinator presentation, SDL, GPU, or generated client content dependencies.

Keep authoring models separate from compact runtime data. Reject speculative generic engines, reflective runtime component systems, distributed transactions, and abstractions without a concrete task need.
