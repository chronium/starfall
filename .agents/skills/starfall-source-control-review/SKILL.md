---
name: starfall-source-control-review
description: Inspect, review, stage, and commit Starfall work without mixing coordinator or sibling changes. Use for dirty-tree triage, focused task commits, diff review, authority and dependency review, validation evidence, or coordinator gitlink handoff.
---

# Starfall Source Control And Review

## Establish State

Inspect:

```sh
git status --short
git -C .. status --short
git -C .. submodule status
```

Proceed with a clean Starfall tree or one known coherent active-task change. Stop on mixed, surprising, or ambiguous edits. Never reset, discard, clean, overwrite, or absorb unrelated work.

## Review The Task

Review the complete Starfall diff for:

- scope against the active PM task;
- server/client authority leaks;
- forbidden headless presentation dependencies;
- unapproved dependency, protocol, format, service, or topology decisions;
- literal parent-relative paths, unapproved coordinator source references, or Royale references;
- missing tests, wiki updates, PM notes, or owner validation;
- generated, secret, cache, build-output, or accidental machine files.

Run the repository's documented validation and inspect the staged file list before committing.

For an owner-requested review continuation that remains inside the most recently completed task's scope, reopen that task and create a second focused commit under the same task ID. Do not manufacture a separate review task. Use a new task when the finding changes ownership, adds a dependency or contract decision, introduces independent scope, or is intentionally deferred.

The same focused-commit rule applies when an owner-approved grooming task is reopened solely to wire canonical coordinator dependencies that were allocated by the planned later grooming cycle. The follow-up commit must use the original grooming task ID and may contain only recorded dependency receipts and matching roadmap corrections; it must not absorb feature work or unrelated grooming.

## Commit And Hand Off

Commit only Starfall-owned files with a subject beginning `[TASK-ID]`. Do not stage coordinator files or the gitlink from inside the child task.

After the child task is complete and committed, return to the verified coordinator checkout in the same approved cycle. Do not create, select, activate, complete, or mutate a coordinator PM task for this mechanical handoff.

Before advancing the pointer, verify the Starfall stable project ID and reciprocal parent declaration, committed path hint and tracked gitlink, expected child `HEAD`, ancestry from the recorded pin, clean Starfall and sibling worktrees, and no unrelated coordinator changes. Stage only the Starfall gitlink. Inspect the complete staged submodule diff, then create a pointer-only coordinator commit whose subject begins with the Starfall task ID and whose body records the canonical task URI, stable project ID, and pinned commit.

If any check fails, stop and report it. Resume the same mechanical handoff after resolution without creating a `SUBMODULE` task. Do not automatically push or begin another Starfall task. When the owner requests a push, publish Starfall before the coordinator.

For an explicitly owner-approved taskless Starfall backlog-grooming commit, use `[PM]` for both the child commit and pointer-only coordinator commit. Record the stable Starfall project ID, pinned child commit, and concise grooming purpose in the pointer body; do not fabricate a canonical task URI. Preserve every other identity, ancestry, cleanliness, validation, and publish-order requirement.
