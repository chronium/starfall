---
id: EDITOR-0009
title: Establish Starfall editor interaction state
track: EDITOR
priority: none
dependsOn:
- EDITOR-0008
createdAt: 2026-08-05T07:39:58.1645660Z
modifiedAt: 2026-08-06T06:44:48.0823660Z
---

Establish Starfall's editor interaction state and generic command-history foundation over the styled UI shell.

Acceptance criteria:
- Depend only on EDITOR-0008.
- Own a single editor selection/action-routing state shared by hierarchy, viewport and inspector adapters; synthetic fixtures may prove routing, but real Draft 0 object identity remains EDITOR-0007.
- Own keyboard-focus rules, shortcut suppression while text/value fields are active, active transform-tool/orientation/snap state, Escape cancellation, focus-selection requests, context-menu routing and edit-versus-runtime mode indication.
- Own persistence machinery for lower-dock height/collapsed state/selected tab and applicable panel/tool state. Persist stable user UI preferences, never editor-document or runtime object identity.
- Provide generic command history that owns command execution, undo, redo and dirty checkpoints. Concrete document commands and mutation rules remain in EDITOR-0007.
- Make ordinary reversible deletion immediate and undoable when concrete commands arrive; require confirmation only for irreversible, external-file, cascading or otherwise non-restorable operations.
- Define unsaved-change indication, focus navigation, tooltip/shortcut consistency and deterministic interaction tests using synthetic showcase state.
- Do not implement Draft 0 hierarchy objects, viewport picking against real scene data, concrete transform/delete/duplicate commands, document serialization, compiler outputs or runtime simulation.