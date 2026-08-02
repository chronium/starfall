---
name: starfall-build-validation
description: Restore, build, test, and validate Starfall's .NET solution and architecture boundaries. Use for managed-code validation, dependency-graph checks, headless artifact inspection, skill validation, PM validation, completion evidence, or later native and visual test selection.
---

# Starfall Build Validation

## Foundation Commands

Run from the Starfall repository root:

```sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx --no-restore
dotnet test Starfall.slnx --no-restore --no-build
```

The pinned SDK is in `global.json`. Restore may require network access; distinguish dependency or environment failures from product defects.

## Select Validation

- Policy/wiki only: validate skill metadata, PM state, links, diffs, and repository status.
- Managed code or project graph: run the full foundation commands and architecture tests.
- Protocol/simulation: add deterministic contract, malformed-input, and authoritative-rule tests.
- Native/rendering/UI/input: add automated coverage plus supported native execution and explicit owner validation.
- Packaging: inspect actual output contents and record each verified platform.

Do not invent commands the repository does not configure.

## Protect Boundaries

Architecture tests are an executable contract. Verify direct project references, repository-local imports, library versus executable ownership, and absence of presentation/editor dependencies from World, Simulation, and BalanceLab.

Inspect headless artifacts when a dependency changes. They must exclude SDL, GPU, ImGui, shaders, textures, editor UI, and client presentation assets.

Before completion, run PM validation, `git diff --check`, review the complete Starfall diff and staged list, confirm the coordinator gitlink was not staged, and record exact results in the task note.
