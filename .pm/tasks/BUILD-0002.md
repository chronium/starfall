---
id: BUILD-0002
title: Establish Starfall repository and solution boundaries
track: BUILD
milestone: M0
dependsOn:
- ARCH-0004
createdAt: 2026-08-01T05:46:46.9425230Z
modifiedAt: 2026-08-02T09:00:42.7255760Z
---

Create the independently buildable Starfall .NET 10 foundation: repository policy, repository-local skill routing, solution/build configuration, library boundaries for content, protocol, authoritative simulation, headless world orchestration, client presentation, editor authoring, and Balance Lab, plus dependency-direction tests and durable wiki documentation.

## Acceptance criteria

- Pin the approved .NET 10 SDK and create Starfall-owned solution, build, package, formatting, and test configuration without parent-relative imports or references.
- Create library-only boundaries for Starfall.Content, Starfall.Protocol, Starfall.Simulation, Starfall.World, Starfall.Client, Starfall.Editor, and Starfall.BalanceLab.
- Encode the approved direct graph: Simulation -> Content; World -> Content, Protocol, Simulation; Client -> Content, Protocol; Editor -> Content; BalanceLab -> Content, Simulation; Content and Protocol remain product-dependency-free.
- Keep Protocol transport-neutral, authoritative rules out of Client, and SDL/GPU/ImGui/rendering/editor dependencies out of World, Simulation, and BalanceLab.
- Do not scaffold identity, chat, operations, or persistence projects; logical service boundaries do not imply immediate deployables.
- Add architecture tests for the expected solution projects, library output types, direct dependency graph, repository-local references/imports, and forbidden headless dependencies.
- Add Starfall AGENTS.md and the smallest useful repository-local skills for PM/wiki workflow, architecture, build validation, and source-control/review.
- Update Starfall architecture and repository-workflow wiki documentation with the concrete graph, commands, routing, and BUILD-0003 executable boundary.
- Restore, build, and test successfully from the Starfall checkout; validate skills and PM state; record exact results in task notes.
- Commit only Starfall-owned files. Do not modify ChronoFall or Royale source, either gitlink, shared acquisition, runtime behavior, services, persistence, networking, rendering, gameplay, or runnable process shells.

## Notes

- 2026-08-02 09:00 UTC - Implemented the approved independent Starfall foundation.

  Created Starfall.slnx and Starfall-owned .NET 10.0.301 build, package, formatting, and test configuration. Added library-only projects Starfall.Content, Starfall.Protocol, Starfall.Simulation, Starfall.World, Starfall.Client, Starfall.Editor, and Starfall.BalanceLab with the approved direct project-reference graph. Added Starfall.Architecture.Tests with six executable gates covering the exact solution set, library output types, approved direct references, absence of product packages, headless exclusion of presentation/editor dependencies, and repository-local project/import paths. No placeholder domain APIs, executable shells, services, persistence, networking, rendering, gameplay, shared acquisition, parent-relative references, or Royale dependencies were added.

  Added Starfall AGENTS.md, README.md, and four focused local skills for PM workflow, architecture boundaries, build validation, and source-control/review. Updated architecture/overview and created development/repository-workflow. Every linked mutation receipt targeted project prj_pkIpzx0fzFD4URjvqBuYrGZF and only Starfall .pm paths.

  Validation:
  - dotnet restore Starfall.slnx: passed.
  - dotnet build Starfall.slnx --no-restore: passed with 0 warnings and 0 errors.
  - dotnet test Starfall.slnx --no-restore --no-build: passed 6/6.
  - dotnet format Starfall.slnx --verify-no-changes --no-restore: passed.
  - PM doctor from the Starfall checkout: passed.
  - git diff --check: passed.
  - Linked-family inspection: 3 available/readable/trusted members, 0 warnings.
  - The supplied quick_validate.py could not start because the host Python lacks PyYAML; no dependency was installed. Equivalent read-only checks confirmed all four skill folders have matching names, non-placeholder descriptions, required frontmatter, interface metadata, and default prompts referencing the correct $skill name.

  No native or visual validation was required because this task creates no runnable or visual behavior. Coordinator source, Royale, and both recorded gitlinks remain untouched. A separate coordinator SUBMODULE-0002 task owns advancing the Starfall gitlink after this child commit.