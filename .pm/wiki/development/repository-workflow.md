---
title: Repository Workflow
createdAt: 2026-08-02T08:58:34.7128420Z
modifiedAt: 2026-08-02T08:58:34.7128420Z
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

The repository pins .NET SDK 10.0.301 and owns its build properties and package versions. It must build from the Starfall checkout without coordinator-relative imports, project references, or a Royale dependency.

## Foundation validation

Run from the Starfall repository root:

```sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx --no-restore
dotnet test Starfall.slnx --no-restore --no-build
```

The architecture tests enforce the expected solution projects, library-only foundation, approved direct project-reference graph, repository-local reference/import paths, absence of product package dependencies during the foundation, and headless exclusion of client/editor/rendering dependencies.

When an approved task changes a dependency or executable boundary, update the tests and `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/overview` together.

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