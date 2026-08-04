---
title: Repository Workflow
createdAt: 2026-08-02T08:58:34.7128420Z
modifiedAt: 2026-08-04T17:26:13.2772600Z
---

## Purpose

This page records Starfall's independent repository layout, foundation validation, PM routing, and the boundary between library setup and runnable process work.

## Repository layout

```text
Starfall.slnx
Directory.Build.props
Directory.Packages.props
global.json
src/
  Starfall.Content/
  Starfall.Protocol/
  Starfall.Simulation/
  Starfall.World/
  Starfall.Client/
  Starfall.Editor/
  Starfall.BalanceLab/
tests/
  Starfall.Architecture.Tests/
  Starfall.Client.Tests/
  Starfall.Content.Tests/
  Starfall.Protocol.Tests/
  Starfall.World.Tests/
.agents/skills/
```

`BUILD-0002` established every product project as a library so compile-time ownership and dependency direction existed without placeholder gameplay or service types. `BUILD-0003` changes only `Starfall.Client` and `Starfall.World` into executable composition roots. Content, Protocol, Simulation, Editor, and BalanceLab remain libraries.

The repository pins .NET SDK 10.0.301 and owns its build properties and package versions. `BUILD-0002` established a standalone library foundation; that historical result remains valid. The canonical environment for future full-client builds is now the shallow coordinator family checkout. Starfall may consume an approved coordinator source allowlist through `$(ChronoFallFamilyRoot)`, but never references Royale or imports coordinator build policy.

For `EDITOR-0003`, `Starfall.Editor` and `Starfall.BalanceLab` remain libraries. `EDITOR-0004` owns any later Balance Lab executable-host decision and scaffolding. The durable authoring, compilation, headless-analysis, and operations separation is recorded at pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/editor-and-balance-lab-boundaries.

## Foundation validation

First stage the selected client cook from the coordinator root by stable project ID:

~~~sh
scripts/cook-character-presentation-for-client.sh \
  --project-id prj_pkIpzx0fzFD4URjvqBuYrGZF
~~~

Then validate Starfall:

~~~sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx -m:1 --no-restore
dotnet test Starfall.slnx -m:1 --no-restore --no-build
~~~

The solution includes a real UDP loopback connected-walking integration test. It generates an ephemeral P-256 ticket, admits one player, moves through the authoritative World/Simulation path, receives an invalid-destination correction, disconnects, and verifies atomic session/player cleanup.

For a native connected run, use `tools/Starfall.DevelopmentAdmission` to generate ignored keys, copy the fresh world-instance GUID from the connected World READY diagnostic, issue one ticket, and launch Client with a literal loopback address and ticket-file path. The exact commands live in the repository README. Generated keys and tickets stay under ignored `artifacts/`; never commit or log them.

Debug and Release builds/tests are required before completion. Native owner validation must cover authoritative walking, collision correction, camera/animation continuity and disconnect. Captures remain a separate owner-curated visual-checkpoint decision.

## Coordinator family checkout

`Directory.Build.props` declares `ChronoFallFamilyRoot` once. When no override is supplied, it normalizes the directory above the Starfall checkout, matching the expected `coordinator/starfall` topology. An override must identify an equivalent coordinator root; individual projects must not invent parent-relative or absolute source paths. The same file defines `StarfallRepositoryRoot` for repository-local generated-content routing.

The exact direct family-source references are:

- `Starfall.Client`: `ChronoFall.CharacterPresentation`, `ChronoFall.CharacterPresentation.Cooking`, `ChronoFall.CharacterPresentation.SdlGpu`, and `ChronoFall.Network.Transport.LiteNetLib`;
- `Starfall.World`: `ChronoFall.Network.Transport.LiteNetLib`;
- `Starfall.Simulation`: `ChronoFall.Box3D`;
- Content, Protocol, Editor and Balance Lab: none.

Every direct reference is rooted at `$(ChronoFallFamilyRoot)` and preserves the selected Debug or Release configuration. Nested coordinator dependencies remain transitive: Client does not reference SDL3-CS or network contracts/upstream LiteNetLib directly, World does not reference those network dependencies directly, and Simulation does not reference raw Box3D bindings directly.

`CLIENT-0006` owns character-presentation adoption and generated-content staging. `BUILD-0006` owns the transport source boundary and inert factories. `CLIENT-0009` owns the active Starfall packet policy, Client/World hosts and the development admission tool. The tool sits outside the product graph and references only Protocol.

Generated character content remains under the workflow-owned ignored `artifacts/chronofall/character-presentation/client/` tree. Development admission keys/tickets remain under ignored `artifacts/` or another owner-local location and are never committed. Raw supplied assets, generated output, bearer tickets, private keys and presentation dependencies never enter headless outputs.

Network adoption contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/network-transport-adoption

## PM workflow

Starfall tasks and wiki pages are owned by stable project `prj_pkIpzx0fzFD4URjvqBuYrGZF`.

- Inspect the active project and linked family before task work.
- Use `project: starfall` for Starfall reads and mutations initiated from the coordinator.
- Preserve task ownership returned by PM.
- Move only an approved task to `in-progress`.
- Inspect every mutation receipt for the Starfall project ID and Starfall-only changed paths.
- Update task notes and durable wiki documentation before completion.
- Complete, validate, and commit inside Starfall; then perform the verified pointer-only coordinator handoff in the same approved cycle and stop.

When owner-requested code review follows a completed task and no unrelated task has superseded it, continue the same task when the findings are directly attributable to its implementation, documentation, or tests and remain inside its approved contracts. Re-read and reopen that task, record the review findings, correct and validate them, return it to `done`, and create a focused follow-up commit under the same task ID. Create a new task when review introduces independent product or architecture scope, a new dependency or contract decision, substantial deferred work, different ownership, or no longer belongs coherently to the most recently completed task. Never absorb unrelated findings into a convenient prior task.

Never edit `.pm/` manually. Plain task IDs are Starfall-local; cross-project references use canonical `pm://project/<stable-project-id>/...` URIs.

## Agent routing

Read `AGENTS.md` and load only the skills needed for the task:

- `starfall-pm-workflow` for PM, linked ownership, receipts, notes, and wiki;
- `starfall-architecture-boundaries` for authority, service availability, and project dependencies;
- `starfall-build-validation` for restore, build, tests, architecture gates, and evidence;
- `starfall-source-control-review` for dirty-tree handling, review, focused commits, and coordinator handoff.

Specialized domain skills should be added only when real implementation creates durable repository-specific workflows.

## Coordinator handoff

A Starfall task commits only Starfall-owned files in this repository. After the task is complete and committed, return to the verified coordinator checkout in the same approved cycle; do not create or mutate a coordinator PM task for the mechanical handoff. Verify the stable Starfall project ID, reciprocal declarations, committed path hint and tracked gitlink, expected child `HEAD`, ancestry from the recorded pin, clean Starfall and sibling worktrees, and absence of unrelated coordinator changes. If every check passes, stage only the Starfall gitlink and create a pointer-only coordinator commit whose subject begins with the Starfall task ID and whose body records `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/task/<task-id>`, the stable project ID, and the pinned commit. If any check fails, stop and resume the same handoff after resolution without creating a `SUBMODULE` task. Pushing remains owner-directed and ordered Starfall first, coordinator second. Starfall commits never include coordinator source or Royale changes.