---
title: Repository Workflow
createdAt: 2026-08-02T08:58:34.7128420Z
modifiedAt: 2026-08-02T09:49:09.5075740Z
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

All product projects are libraries during `BUILD-0002`. They establish compile-time ownership and dependency direction without placeholder gameplay or service types. `BUILD-0003` separately owns runnable client and world-host shells plus the local launch workflow.

The repository pins .NET SDK 10.0.301 and owns its build properties and package versions. `BUILD-0002` established a standalone library foundation; that historical result remains valid. The canonical environment for future full-client builds is now the shallow coordinator family checkout. Starfall may consume an approved coordinator source allowlist through `$(ChronoFallFamilyRoot)`, but never references Royale or imports coordinator build policy.

## Foundation validation

Run from the Starfall repository root:

```sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx --no-restore
dotnet test Starfall.slnx --no-restore --no-build
```

The architecture tests enforce the expected solution projects, library-only foundation, approved direct project-reference graph, absence of product package dependencies during the foundation, and headless exclusion of client/editor/rendering dependencies. They also enforce the exact client-only coordinator source allowlist and reject literal repository escapes, absolute paths, arbitrary property-rooted references, coordinator imports, and Royale references.

When an approved task changes a dependency or executable boundary, update the tests and `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/overview` together.

## Coordinator family checkout

`Directory.Build.props` declares `ChronoFallFamilyRoot` once. When no override is supplied, it normalizes the directory above the Starfall checkout, matching the expected `coordinator/starfall` topology. An override must identify an equivalent coordinator root; individual projects must not invent their own parent-relative or absolute source paths.

The currently approved future source consumers are limited to `Starfall.Client` references to:

- `ChronoFall.CharacterPresentation`;
- `ChronoFall.CharacterPresentation.Cooking`;
- `ChronoFall.CharacterPresentation.SdlGpu`.

No reference is added by `SF-0006`. `CLIENT-0006` will add and validate them after coordinator `SHARED-0016` establishes the source and generated-content workflow. Coordinator-owned SDL3-CS remains compiled from its checked-out source transitively through the shared SDL GPU project; Starfall does not acquire or pin it independently in this policy task.

Generated character content will remain under a task-owned ignored child `artifacts/` tree. The future coordinator workflow must validate the destination by stable linked-project identity and resolved checkout path, then refuse to write unless its exact owned output tree is ignored and contains no tracked files. Raw supplied assets, generated output, and presentation dependencies never enter headless projects.

## PM workflow

Starfall tasks and wiki pages are owned by stable project `prj_pkIpzx0fzFD4URjvqBuYrGZF`.

- Inspect the active project and linked family before task work.
- Use `project: starfall` for Starfall reads and mutations initiated from the coordinator.
- Preserve task ownership returned by PM.
- Move only an approved task to `in-progress`.
- Inspect every mutation receipt for the Starfall project ID and Starfall-only changed paths.
- Update task notes and durable wiki documentation before completion.
- Complete, validate, commit inside Starfall, and stop.

Never edit `.pm/` manually. Plain task IDs are Starfall-local; cross-project references use canonical `pm://project/<stable-project-id>/...` URIs.

## Agent routing

Read `AGENTS.md` and load only the skills needed for the task:

- `starfall-pm-workflow` for PM, linked ownership, receipts, notes, and wiki;
- `starfall-architecture-boundaries` for authority, service availability, and project dependencies;
- `starfall-build-validation` for restore, build, tests, architecture gates, and evidence;
- `starfall-source-control-review` for dirty-tree handling, review, focused commits, and coordinator handoff.

Specialized domain skills should be added only when real implementation creates durable repository-specific workflows.

## Coordinator handoff

A Starfall task commits only in this repository. The coordinator's gitlink remains unchanged until a separately selected and approved coordinator `SUBMODULE` task advances it to the reviewed child commit. Starfall commits never include coordinator source or Royale changes.