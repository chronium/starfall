---
name: starfall-pm-workflow
description: Manage Starfall tasks and wiki through PM MCP, including linked-family inspection, Starfall project selection, dependency readiness, state transitions, mutation receipts, task notes, wiki updates, project validation, completion, and coordinator handoff.
---

# Starfall PM Workflow

## Ground Linked Work

1. Call `get_project` and `list_linked_projects`.
2. Confirm Starfall resolves as `prj_pkIpzx0fzFD4URjvqBuYrGZF`, is readable, and has local write trust before linked mutations.
3. Review every structured warning and preserve the owning project returned by each read.
4. Run PM validation from the Starfall checkout before and after mutations.

Use `project: starfall` for one-project reads and every Starfall mutation from the coordinator. Use `family: true` only for family reads; never combine it with `project`.

## Select And Execute One Task

- In Plan mode, call `get_next_task(readyOnly: true, project: starfall)` unless the owner directs a task.
- Read the selected task with both its local ID and Starfall selector. Inspect completed, waiting, missing, unavailable, and invalid dependencies.
- Planning never mutates PM or moves state.
- After approval, re-read the task, confirm readiness, and move only it to `in-progress`.
- Complete only after implementation, tests, durable notes/wiki, and required owner validation.
- Commit the task in Starfall and stop. Do not begin the next recommendation.

## Mutate Safely

Never edit `.pm/` directly. Use the narrowest PM MCP mutation and inspect every receipt:

- `projectId` must be `prj_pkIpzx0fzFD4URjvqBuYrGZF`;
- changed paths must belong only to Starfall;
- one operation must affect exactly one repository.

Plain dependency IDs are Starfall-local. Persist parent dependencies and wiki references only as `pm://project/<stable-project-id>/...` URIs; aliases and paths are selectors, not durable identity.

## Keep Durable Context

Record decisions, exact validation commands/results, known limitations, owner validation, and commit implications in task notes. Update the Starfall wiki with architecture, protocol, format, content, setup, and workflow contracts in the same task that changes them.
