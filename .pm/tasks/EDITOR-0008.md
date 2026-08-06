---
id: EDITOR-0008
title: Establish the Starfall editor UI foundation
track: EDITOR
priority: none
dependsOn:
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0024
createdAt: 2026-08-05T07:39:43.2672680Z
modifiedAt: 2026-08-05T10:47:00.9299440Z
---

Create Starfall's native editor UI foundation over the approved coordinator-owned SDL GPU ImGui/ImGuizmo backend.

Ownership:
- Starfall owns the editor executable, application loop, window/device/render scheduling, design tokens, fonts, dock layout, thin editor-specific UI vocabulary and synthetic showcase.
- ChronoFall owns only the caller-controlled backend in pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0024.
- Royale remains unchanged. Completed Starfall EDITOR-0003/CLIENT-0020 and Royale editor tasks are architectural evidence, not dependencies.

Acceptance criteria:
- Depend only on canonical coordinator task pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0024.
- Introduce the focused Starfall.Editor native desktop composition boundary and exact approved coordinator-source allowlist; keep World, Simulation and BalanceLab free of SDL, GPU, ImGui and editor UI.
- Implement the proposed v0.1 central design tokens, proportional/monospace typography roles, DPI-aware metrics and contrast checks, subject to native showcase review.
- Provide a thin Starfall-specific immediate-mode vocabulary for panel chrome, toolbars, tabs, hierarchy rows, property rows, transform/vector fields, asset references, search, breadcrumbs, thumbnails, diagnostics, status, empty states, menus, context menus, tooltips, popups, modal dialogs, semantic buttons and viewport overlays.
- Use semantic colour tokens centrally. Do not scatter arbitrary feature-local style literals; specialized layout metrics must be named and owned by the applicable primitive or surface.
- Preserve the hierarchy / viewport / inspector / resizable lower-dock layout and render deterministic, explicitly synthetic showcase states without depending on CONTENT-0014 or inventing durable content identities.
- Demonstrate no selection, selected transformable model, spawn-like object, inline validation error, populated/empty assets, warning/error diagnostics, active transform tools, keyboard focus, menu/popup/modal/tooltip states and expanded/collapsed lower dock.
- Validate macOS ARM64 native execution and obtain owner visual review. Linux x64 native validation is not currently required; Windows validation begins only when Windows becomes a supported family target.
- Do not implement a real authoring document, reflection inspector, docking framework, asset database, Pressure Cooker, runtime UI, generic GUI toolkit or Royale reskin.

Scheduling gate:
- This task is intentionally deferred at priority none even though SHARED-0024 is complete and the dependency is technically ready.
- The generated Draft 0 graybox remains sufficient for current gameplay work.
- Reconsider this task after the connected Basic Arrow loop is natively playable and owner-validated through CLIENT-0007, unless the owner explicitly reprioritizes it earlier. This is an evidence gate, not a source dependency.