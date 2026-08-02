---
title: Repository Workflow
createdAt: 2026-08-02T08:58:34.7128420Z
modifiedAt: 2026-08-02T12:39:46.6050900Z
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
.agents/skills/
```

`BUILD-0002` established every product project as a library so compile-time ownership and dependency direction existed without placeholder gameplay or service types. `BUILD-0003` changes only `Starfall.Client` and `Starfall.World` into executable composition roots. Content, Protocol, Simulation, Editor, and BalanceLab remain libraries.

The repository pins .NET SDK 10.0.301 and owns its build properties and package versions. `BUILD-0002` established a standalone library foundation; that historical result remains valid. The canonical environment for future full-client builds is now the shallow coordinator family checkout. Starfall may consume an approved coordinator source allowlist through `$(ChronoFallFamilyRoot)`, but never references Royale or imports coordinator build policy.

## Foundation validation

First stage the selected client cook from the coordinator root by stable project ID:

```sh
scripts/cook-character-presentation-for-client.sh \
  --project-id prj_pkIpzx0fzFD4URjvqBuYrGZF
```

Then run from the Starfall repository root:

```sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx --no-restore
dotnet test Starfall.slnx --no-restore --no-build
```

After the solution build, prove the headless world shell and the client content path independently:

```sh
dotnet run --project src/Starfall.World/Starfall.World.csproj --no-restore --no-build
dotnet run --project src/Starfall.Client/Starfall.Client.csproj --no-restore --no-build -- \
  --validate-character-content
```

Run the native presentation preview with:

```sh
dotnet run --project src/Starfall.Client/Starfall.Client.csproj --no-restore --no-build
```

The preview loads the staged Quaternius humanoid, loops `Idle_Loop` through SDL GPU, and closes with Escape or the window close control. The content probe loads the same runtime cook without initializing SDL and prints a deterministic asset, joint, and clip summary. Unknown arguments fail with exit code 2. `Starfall.World` means the headless authoritative world-server host; it is not a client-side world and does not imply one executable per logical service. `SERVER-0002` owns the fixed-step world lifecycle, while `CLIENT-0006` owns the first presentation runtime integration.

The architecture tests enforce the expected solution projects, exact executable/library split, bounded Client and World process startup, argument rejection, approved direct project-reference graph, absence of product package dependencies, and headless output exclusion of client/editor/rendering artifacts. They also enforce the exact client-only coordinator source allowlist and reject literal repository escapes, absolute paths, arbitrary property-rooted references, coordinator imports, and Royale references.

When an approved task changes a dependency or executable boundary, update the tests and `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/overview` together.

## Coordinator family checkout

`Directory.Build.props` declares `ChronoFallFamilyRoot` once. When no override is supplied, it normalizes the directory above the Starfall checkout, matching the expected `coordinator/starfall` topology. An override must identify an equivalent coordinator root; individual projects must not invent their own parent-relative or absolute source paths. The same file defines `StarfallRepositoryRoot` for repository-local generated-content routing, so Client project files do not scatter relative traversal.

The implemented source-consumption boundary is limited to `Starfall.Client` references to:

- `ChronoFall.CharacterPresentation`;
- `ChronoFall.CharacterPresentation.Cooking`;
- `ChronoFall.CharacterPresentation.SdlGpu`.

`CLIENT-0006` adds and validates exactly these references after coordinator `SHARED-0016` established the source and generated-content workflow. Client preserves the selected Debug or Release configuration across references that are external to `Starfall.slnx`, including their nested coordinator references. Coordinator-owned SDL3-CS remains compiled from its checked-out source transitively through the shared SDL GPU project; Starfall neither references, acquires, nor pins it independently.

Generated character content remains under the workflow-owned ignored `artifacts/chronofall/character-presentation/client/` tree. The coordinator command validates stable linked identity, the resolved checkout, gitlink ownership, the exact ignored and untracked output boundary, known files, and symlink safety before writing. Starfall.Client copies only the cooked asset, provenance, CC0 evidence, shared shaders, and the supported native runtime into its build output. Raw supplied assets, generated output, and presentation dependencies never enter headless projects.

## PM workflow

Starfall tasks and wiki pages are owned by stable project `prj_pkIpzx0fzFD4URjvqBuYrGZF`.

- Inspect the active project and linked family before task work.
- Use `project: starfall` for Starfall reads and mutations initiated from the coordinator.
- Preserve task ownership returned by PM.
- Move only an approved task to `in-progress`.
- Inspect every mutation receipt for the Starfall project ID and Starfall-only changed paths.
- Update task notes and durable wiki documentation before completion.
- Complete, validate, and commit inside Starfall; then perform the verified pointer-only coordinator handoff in the same approved cycle and stop.

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