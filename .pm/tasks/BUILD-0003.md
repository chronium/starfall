---
id: BUILD-0003
title: Add runnable client and world host shells
track: BUILD
milestone: M0
dependsOn:
- BUILD-0002
createdAt: 2026-08-02T07:29:11.3141820Z
modifiedAt: 2026-08-02T12:12:08.0145630Z
---

Add minimal runnable Starfall client and headless world-host executable shells plus a reproducible local build and launch workflow on top of the approved solution boundaries. Prove dependency-direction checks and process startup only. Do not add gameplay, networking, identity, chat, operations, persistence, rendering integration, or service infrastructure.

## Acceptance criteria

- Change only Starfall.Client and Starfall.World from libraries to executables; Content, Protocol, Simulation, Editor, and BalanceLab remain libraries.
- Preserve the exact approved Starfall-local project-reference graph and add no package, coordinator, Royale, SDL, GPU, rendering, editor, or native dependency.
- Add direct composition-root programs that accept no arguments, write deterministic foundation-shell startup messages, and exit successfully.
- Reject any supplied argument with a concise deterministic stderr message and exit code 2.
- Do not add a long-running loop, fixed-step world lifecycle, cancellation hosting, configuration, dependency injection, logging framework, networking, services, gameplay, or presentation.
- Update architecture tests to enforce the executable/library split, launch both built shells with bounded timeouts, verify output and exit codes, verify argument rejection, and protect the headless World output from client/presentation artifacts.
- Update README and Starfall architecture/repository-workflow wiki documentation with exact commands, the bounded startup-and-exit behavior, and the distinction between the authoritative world host and final physical service topology.
- Restore, build, test, and format-check Debug and Release configurations; run both shells directly; inspect PM, family, diffs, staged scope, output artifacts, and submodule state.
- Record exact evidence in task notes, commit only Starfall-owned task scope, then perform the automatic pointer-only coordinator handoff under canonical BUILD-0003 ownership.
- No visual validation, history artifact, push, next-task selection, or downstream implementation is included.

## Notes

- 2026-08-02 12:12 UTC - Implemented the bounded runnable Client and authoritative World host foundation.

  - Changed only Starfall.Client and Starfall.World to executable output types; the remaining five product projects stay libraries and the approved direct project-reference graph is unchanged.
  - Added direct composition roots. With no arguments each prints its deterministic foundation-shell startup message and exits 0; any argument writes a deterministic stderr message and exits 2.
  - Added no runtime loop, cancellation host, configuration, dependency injection, logging package, networking, gameplay, service, SDL/GPU, rendering, editor, coordinator, or Royale dependency.
  - Replaced the library-only architecture assertion with the exact executable/library contract. Added real subprocess startup and rejection tests with 10-second timeouts and a World-output presentation-artifact gate.
  - Updated README, architecture/overview, and development/repository-workflow with exact commands, bounded behavior, and clarification that Starfall.World is the headless authoritative world-server host rather than a client-side world or a decision to split every logical service.

  Validation:
  - Baseline before implementation: restore/build passed with 0 warnings and 15/15 tests.
  - dotnet restore Starfall.slnx -m:1 --disable-build-servers: passed; all projects up to date.
  - Debug build: passed with 0 warnings and 0 errors.
  - Debug tests: passed 20/20.
  - Release build: passed with 0 warnings and 0 errors.
  - Release tests: passed 20/20.
  - dotnet format Starfall.slnx --verify-no-changes --no-restore: passed.
  - Direct Client and World no-argument runs printed the expected startup messages and exited 0.
  - Direct Client and World --unexpected runs printed the expected rejection messages and exited 2.
  - Debug World output contains only its app host/runtime files plus Starfall.Content, Starfall.Protocol, and Starfall.Simulation managed assemblies and symbols; no client, editor, shared-presentation, SDL, GPU, ImGui, shader, texture, or presentation artifact is present.
  - Starfall pm doctor and git diff --check passed.
  - Linked-family inspection returned all three projects available, readable, and write-trusted with zero warnings. BUILD-0003 remained the sole active family task during implementation.
  - Every linked PM mutation receipt targeted prj_pkIpzx0fzFD4URjvqBuYrGZF and only expected Starfall task/wiki paths.
  - Coordinator and Royale source remain untouched; the coordinator currently observes only the dirty Starfall worktree, not a staged gitlink.
  - No visual or history-artifact gate applies because this task creates no window, controls, rendering, audio, camera, or gameplay.