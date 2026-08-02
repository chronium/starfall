# Repository Instructions

## Operating Model

Starfall is an independently useful server-authoritative MMORPG and a linked child of the ChronoFall coordinator. Starfall owns its PM project, source history, product architecture, simulation, protocol, content, presentation integration, editor and Balance Lab, build, release, and commits.

ChronoFall owns family planning, cross-project contracts, proven shared modules, distribution experiments, and the pinned Starfall gitlink. Starfall may consume coordinator-owned shared artifacts through an approved acquisition contract, but it must not reference coordinator source by relative path. Starfall never depends on Royale.

Inspect the selected task, PM wiki, project graph, nearby code, tests, repository status, and linked-family state before changing contracts. Ask the owner before selecting a new dependency, protocol or file format, authority rule, service topology, persistence behavior, renderer/native integration, platform policy, or product rule not already approved.

## Authority And Availability

- The world owns authoritative movement, combat, character state, monsters, camps, inventory, equipment, progression, drops, and active gameplay sessions.
- Clients send intent and present authoritative state and events. Rendering, animation, IK, effects, cameras, UI feedback, and smoothing never decide gameplay outcomes.
- Once admitted to a world, an active gameplay session must not depend on identity, chat, or operations remaining available.
- Identity admits through short-lived signed join tickets; the world consumes the ticket and owns the gameplay session.
- Gameplay-critical events use the game protocol. Chat delivery is optional from gameplay's perspective.
- Each world/channel owns an independent lifecycle and state. Final physical deployment topology and persistence degradation remain deferred decisions.
- Camps remain a world-simulation subsystem: definition to spawn/replenishment policy to world-owned entities.
- Headless world, simulation, and Balance Lab code must not depend on SDL, GPU, ImGui, rendering, editor UI, or presentation assets.

Canonical service documentation: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/service-availability-and-ownership`.

## Foundation Project Graph

- `Starfall.Content`: Starfall-owned definitions and validation inputs; no product-project dependencies.
- `Starfall.Protocol`: transport-neutral wire contracts; no product-project dependencies.
- `Starfall.Simulation`: deterministic authoritative rules; depends only on Content.
- `Starfall.World`: headless world orchestration; depends on Content, Protocol, and Simulation.
- `Starfall.Client`: client presentation and input mapping; depends on Content and Protocol, never Simulation or World.
- `Starfall.Editor`: authoring boundary; currently depends only on Content.
- `Starfall.BalanceLab`: headless deterministic analysis; depends on Content and Simulation.

Change this graph only through an approved task that updates architecture tests and the PM wiki. Logical identity, chat, operations, and persistence boundaries do not justify placeholder projects or one process per concept.

Keep authoring representations separate from compact runtime data. Do not introduce a reflective Unity-style runtime component system, generic service framework, distributed transactions, or speculative abstractions.

## Code Quality

- Keep project folders and namespaces aligned.
- Prefer direct, readable C# over compressed or ceremonial abstractions.
- Organize substantial domains into cohesive folders before files become unrelated collections.
- Create assemblies only for real authority, deployment, platform, ownership, or test boundaries.
- Keep composition roots focused on lifecycle and wiring.
- Add an interface only for a concrete substitution, external dependency, isolation, or test need.
- Follow `.editorconfig`; preserve nullable and deterministic build settings.

## PM And Wiki

Every implementation change requires one Starfall-owned PM task managed through PM MCP.

1. In Plan mode, inspect the linked family, call `get_next_task(readyOnly: true, project: starfall)` unless the owner selects a task, then read it with the Starfall selector.
2. Planning does not edit source or PM, grant trust, or move task state.
3. After owner approval, re-read the task and dependencies, then move only it to `in-progress`.
4. Implement the approved scope, validate it, update durable notes and wiki pages, and obtain owner validation for visual, UI, control, audio, camera, or gameplay-feel criteria.
5. Move the task to `done` only when implementation, validation, documentation, and required owner validation are complete.
6. Commit the focused Starfall change and stop. Do not select another task automatically.

Never edit `.pm/` manually. Use an explicit `project: starfall` selector when mutating Starfall from the coordinator. Verify each receipt identifies project `prj_pkIpzx0fzFD4URjvqBuYrGZF` and only Starfall paths. Persist cross-project references only as canonical `pm://project/<stable-id>/...` URIs.

The PM wiki is the durable source of truth for architecture, protocols, formats, content contracts, setup, validation, and workflows. Update it with the task that changes a contract.

## Git And Coordinator Handoff

Inspect both Starfall and coordinator status before implementation. Preserve all existing work and stop on mixed or surprising changes.

- Commit child work inside Starfall with a subject beginning `[TASK-ID]`.
- Do not include coordinator source, Royale, or a gitlink update in a Starfall commit.
- After a Starfall task is committed, the coordinator requires a separately selected and approved `SUBMODULE` task to advance the gitlink.
- Never reset, discard, absorb, or hide unrelated child or coordinator work.

## Validation

For foundation changes, run:

```sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx --no-restore
dotnet test Starfall.slnx --no-restore --no-build
```

Also validate relevant skills, PM state, `git diff --check`, project references, repository status, and the coordinator's submodule status. Choose focused validation for later domain work, but always protect headless outputs from client graphics dependencies.

Rendering, UI, controls, animation, camera, audio, and gameplay feel require explicit owner validation. When a result is a meaningful visual milestone rather than routine evidence, show the best candidate and ask whether to preserve, revise, or skip it; never commit a history artifact automatically.

## Skill Routing

Load the smallest useful set:

- `starfall-pm-workflow`: task selection, linked ownership, PM mutations, receipts, notes, wiki, and completion.
- `starfall-architecture-boundaries`: project graph, authority, service availability, headless separation, and dependency decisions.
- `starfall-build-validation`: .NET restore/build/test, architecture gates, artifact inspection, and validation evidence.
- `starfall-source-control-review`: dirty-tree handling, focused child commits, review, and coordinator gitlink separation.

Create specialized domain skills only after real implementation workflows demonstrate reusable guidance.
