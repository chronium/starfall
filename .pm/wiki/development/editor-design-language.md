---
title: Starfall Editor Design Language
createdAt: 2026-08-05T07:41:56.3992770Z
modifiedAt: 2026-08-05T10:47:23.3109750Z
---

## Purpose

Starfall's editor should feel like a deliberate authoring instrument rather than a debug interface assembled from raw ImGui widgets. The proven shell remains appropriate:

- hierarchy on the left;
- viewport as the dominant central surface;
- inspector on the right;
- a resizable, collapsible lower dock beneath the viewport;
- viewport tools immediately above or over the viewport.

The design language changes widget vocabulary, visual hierarchy and interaction consistency. It does not authorize a new docking framework, scene format, ECS, reflection inspector, Pressure Cooker or runtime UI.

The governing principle is:

> Present authoring concepts rather than internal data structures or raw UI-library widgets.

Coordinator roadmap: pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/wiki/roadmap/starfall-editor-ui-foundation

## Ownership and sequence

~~~text
parent SHARED-0024
  -> EDITOR-0008  native UI foundation and synthetic showcase
  -> EDITOR-0009  interaction state and generic command history
  -> EDITOR-0007  real Draft 0 document, commands and compilation
     ├── SERVER-0012 / CLIENT-0016
     └── EDITOR-0010 auxiliary Assets / Validation / Log / status polish
~~~

ChronoFall owns the caller-controlled SDL GPU ImGui/ImGuizmo backend. Starfall owns its editor executable, application loop, window/device scheduling, design tokens, fonts, layout, UI vocabulary, interaction state, documents, inspectors and workflows. Royale remains unchanged.

EDITOR-0008 depends only on canonical SHARED-0024. Completed Starfall EDITOR-0003 and CLIENT-0020 and completed Royale editor work are architectural evidence, not PM dependencies.

### Scheduling gate

Completion of `SHARED-0024` makes the native UI foundation technically available; it does not make editor work the next product priority. `EDITOR-0008` remains in M2 but is intentionally priority none while the generated Draft 0 graybox is sufficient.

Reconsider `EDITOR-0008` after the connected Basic Arrow loop is natively playable and owner-validated through `CLIENT-0007`, unless the owner explicitly reprioritizes it earlier. This is an evidence and scheduling gate rather than a source dependency: canonical `SHARED-0024` remains the task's only dependency.

## Proposed v0.1 theme tokens

These values are proposed defaults subject to native showcase and contrast review. Role names are durable; exact values may be tuned together rather than overridden ad hoc by features.

### Colour roles

| Role | Proposed sRGB | Use |
| --- | --- | --- |
| Canvas | `#0B0D10` | Application and viewport-clear background |
| Surface | `#111419` | Primary panel surface |
| SurfaceRaised | `#171B20` | Headers, menus and elevated rows |
| SurfaceOverlay | `#1D2228` | Popups, tooltips and modal content |
| Control | `#20262D` | Inputs and inactive controls |
| ControlHover | `#29313A` | Hovered controls and rows |
| ControlPressed | `#313B45` | Pressed controls |
| BorderSubtle | `#2A3037` | Panel and row separation |
| BorderStrong | `#424A54` | Focused/elevated boundaries |
| TextPrimary | `#E7E9EC` | Ordinary readable text |
| TextSecondary | `#A9B0B9` | Secondary labels and metadata |
| TextMuted | `#747D88` | Disabled and low-priority context |
| Accent | `#B98959` | Active tool, focus and primary selection accent |
| AccentHover | `#C79A69` | Hovered accent |
| AccentPressed | `#9D7047` | Pressed accent |
| Selection | `#463728` | Selected-row fill paired with Accent edge |
| Focus | `#D6A56F` | Keyboard focus outline |
| Info | `#6C9BC4` | Informational diagnostics |
| Success | `#72AC7B` | Successful validation/state |
| Warning | `#D1A04F` | Warnings and unsaved attention |
| Error | `#D46A65` | Errors and destructive action |
| AxisX | `#D66363` | X axis only |
| AxisY | `#71B373` | Y axis only |
| AxisZ | `#648FD2` | Z axis only |
| Scrim | `#00000099` | Modal background |

Selection, focus, warning, error, success and disabled states use shape, iconography, text or border treatment as well as colour. No bright colour is reused indiscriminately across tabs, headings, fields and actions.

### Typography

- Body: 14 px modern proportional UI face at 1x scale.
- Compact label/menu/tab: 13 px proportional.
- Panel title: 14 px semibold proportional.
- Section title: 13 px semibold proportional.
- Caption/metadata: 12 px proportional.
- Log, path, identifier and diagnostic values: 13 px monospace.
- Do not use monospace for normal hierarchy, menus, buttons or property labels.
- Font metrics and rasterization scale with effective DPI; text is not rendered below 12 physical pixels.
- The selected proportional and monospace fonts require licence/provenance records and native glyph review before being frozen.

### Spacing and dimensions

The base spacing scale is `2, 4, 6, 8, 12, 16, 24` logical pixels.

- Standard control/property row: 28 px.
- Compact toolbar/menu/tab: 26 px.
- Panel header: 30 px.
- Status bar: 24 px.
- Standard input interior: 24 px.
- Standard icon: 16 px; compact icon 14 px; viewport tool icon up to 18 px.
- Panel padding: 8 px; section separation: 12 px; row gap: 2 px; inline control gap: 6 px.
- Border and separator: 1 physical pixel after DPI snapping.
- Panel corners: square. Controls: 2 px radius. Popups/tooltips/modals: at most 3 px.
- Inspector property label column begins at a named 116 px v0.1 metric and remains resizable where practical.
- Asset thumbnail/tile dimensions are named asset-surface metrics rather than global spacing tokens.

DPI scaling applies to typography, spacing, hit targets, borders and icons. At 100% scale, interactive targets should normally be at least 24 px high; compact exceptions require clear separation and native usability evidence.

### Contrast

Automated or deterministic token tests should verify normal text against its intended surfaces at a target contrast ratio of at least 4.5:1 and large text, borders and meaningful UI-state indicators at least 3:1. Native review additionally covers focus visibility, disabled readability and colour-vision-independent warning/error distinction.

## State treatment

- Hover: ControlHover or a subtle raised row; never selection styling.
- Pressed: ControlPressed with a one-pixel inset or stronger border.
- Selected: Selection fill, Accent edge and normal readable text.
- Focused: one-pixel Focus outline distinct from selection.
- Disabled: TextMuted plus reduced contrast; preserve readable label and layout.
- Warning/error: semantic icon, severity label and message; colour is supporting evidence.
- Unsaved: small Warning marker beside document title and status text, not a permanent full-surface tint.
- Missing/unsupported: explicit icon and text; never an unexplained blank thumbnail.
- Primary action: Accent fill used sparingly.
- Default action: neutral Control fill.
- Destructive action: neutral until intent is clear, then Error border/text/fill in confirmation.
- Confirmation: reserved for irreversible, external-file, cascading or non-restorable work. Ordinary undoable object deletion is immediate.

## Thin editor UI vocabulary

Feature code should express editor concepts through focused helpers instead of repeatedly assembling raw controls.

| Primitive | Responsibility | Explicit limit |
| --- | --- | --- |
| EditorPanel / PanelHeader | Panel surface, title, optional count/action and consistent padding | Does not own docking or application layout |
| CompactToolbar / ToolButton | Grouped transform/view tools, active/disabled states and shortcuts | Not a generic command ribbon |
| DockTab | Selected, hovered, attention and close/collapse treatment | Does not implement a docking framework |
| HierarchySection | Group heading, disclosure and one contextual add menu | Not a scene storage container |
| HierarchyRow | Depth, icon, label, selection and supported hidden/locked/invalid states | Does not own object identity |
| InspectorSection | Conceptual grouping and disclosure | Not a reflected component renderer |
| PropertyRow | Aligned label/value/help/error layout | Does not infer fields from arbitrary objects |
| VectorEditor / TransformEditor | Axis-aware numeric editing, mixed state and reset/revert affordance | Not a generic math inspector |
| AssetReferenceField | Stable selected reference, missing/invalid state and browse/clear action | Not an asset database |
| SearchField / Breadcrumbs | Query, clear action and location navigation | Scoped to the owning surface |
| ThumbnailTile | Folder/asset distinction, preview/fallback and selection states | Does not cook or generate arbitrary previews |
| DiagnosticRow | Severity, object/property context and navigation action | Not an observability system |
| StatusBadge / StatusBar | Compact meaningful state and current operation | No permanent DPI/FPS/debug noise |
| EmptyState | Cause, recovery action and restrained explanation | No oversized decorative cards |
| ViewportOverlay | Compact tool, mode and diagnostics placement | Does not consume permanent viewport area |
| MenuBar / MenuItem | Conventional grouped commands, shortcuts, enabled and checked state | Does not mirror every toolbar control |
| ContextMenu | Selection/context-specific actions with safe grouping | Does not conceal primary workflow |
| Tooltip | Short explanation plus shortcut and disabled reason | Not long-form documentation |
| Popup | Temporary non-destructive choice or focused editor | Not a second window architecture |
| ModalDialog | Blocking irreversible/cross-resource decision | Not used for ordinary undoable delete |
| EditorButton | Default, primary and destructive semantic treatments | No feature-local button colours |

Semantic colours always use central tokens. Specialized layout metrics are allowed only when named and owned by their primitive or surface; arbitrary feature-local style literals are not.

## Surface language

### Hierarchy

Section headers, ordinary objects and nested relationships are visibly distinct. A section owns a compact contextual add button/menu; repeated `+ Add` rows are not objects. Selection, invalidity and supported hidden/locked/disabled state have separate indicators.

Names describe authoring concepts such as world geometry, regions, routes, camps, spawn points, navigation/collision inputs, lights and presentation objects. They do not expose raw storage or ECS vocabulary. Hierarchy, viewport and inspector consume one editor selection.

### Viewport and toolbar

The viewport remains dominant. Selection, translate, rotate and scale are grouped as one tool. Local/world orientation, snapping, focus selection, edit/runtime mode and debug overlays use coherent grouped controls, active states and tooltips. Diagnostics normally appear as compact overlays or lower-dock entries.

Keyboard shortcuts are displayed in menus and tooltips. Repeated key events and editor shortcuts are ignored while an applicable text/value field owns keyboard input.

### Inspector

The header shows selected-object name, type and meaningful state. Sections follow authoring concepts, with aligned two-column property rows, readable labels, axis-aware vectors, coherent booleans/enums/asset references and local validation.

Project paths, build output, global counts and unrelated map facts do not fill empty inspector space. There is no reflection-driven universal inspector. EDITOR-0007 owns each bounded Draft 0 inspector and its mutation rules.

### Assets

Assets use breadcrumbs/location, search and useful type filters. Folders and assets are distinct. Thumbnail size is proportionate; filenames truncate or wrap predictably and expose full text in a tooltip. Selected, hovered, missing, invalid, unsupported and no-preview states are explicit. Machine metadata such as `.DS_Store` is excluded.

This surface does not define Pressure Cooker, an asset database, package manager, content bundle or general filesystem browser.

### Validation

Each entry provides severity icon/text, actionable message, affected object/property and selection/focus navigation where available. Warnings and errors never depend on colour alone. Inline inspector validation and the consolidated list share stable diagnostic identity rather than duplicating unrelated messages.

### Log

Log messages use restrained monospace text, readable severity, filtering and search. The surface is for editor/runtime-development evidence, not full observability.

### Lower dock and status

The lower dock preserves selected tab, height and collapsed state through EDITOR-0009 machinery and can yield its space to the viewport. EDITOR-0010 consumes and validates that machinery.

Status prioritizes saved/dirty state, validation result, active edit/runtime mode, background operation and concise selection context. DPI, FPS and detailed renderer state belong in an optional diagnostics overlay, debug menu or tooltip.

### Menus, tooltips and popups

Menus follow platform-familiar grouping and keyboard navigation. Focused items are visibly outlined; disabled items explain unavailable actions when useful. Context menus apply to the current selection/context. Tooltips are concise and consistently delayed. Popups are dismissible focused choices. Modal dialogs trap focus, name the consequence and present ordered default/cancel/destructive actions.

## Interaction ownership

EDITOR-0009 owns generic selection/action routing, input-focus rules, tool state, cancellation, UI-state persistence, focus requests and generic command history. Generic history owns execution, undo, redo and dirty checkpoints.

EDITOR-0007 owns real Draft 0 document identity, viewport picking, concrete transform/duplicate/delete/copy/paste commands, validation rules and compiler mutations. Runtime consumers never depend on editor object or document identity.

Edit mode and any later runtime preview are explicit states. Presentation previews never become authoritative simulation. An unavailable or invalid document produces a designed empty/error state rather than partially emitting runtime outputs.

## Deterministic showcase

EDITOR-0008 uses explicitly synthetic examples labelled as showcase fixtures. It must exercise:

1. no selection;
2. one selected transformable model;
3. one spawn-like authoring fixture;
4. an inline inspector validation error;
5. populated and empty asset views;
6. warnings and errors in the lower dock;
7. active selection/translate/rotate/scale tools;
8. keyboard-focused menu/property state;
9. tooltip, popup and modal treatments;
10. expanded and collapsed lower dock.

Synthetic names never establish content identity. EDITOR-0007 later uses only identities already present in `Draft0GrayboxCatalog.FirstPlayable` and exact task-selected assets.

Native comparison captures should use stable window dimensions, DPI, content, panel sizes, selected states and camera framing. Owner review may tune palette, density, fonts and framing without reopening the architecture decision.

## Capability classification

- Current capability needing restyling: proven hierarchy/viewport/inspector/lower-dock layout, SDL GPU ImGui hosting and ImGuizmo interaction in family evidence.
- Existing concept needing UX refinement: transform toolbar, hierarchy creation actions, inspector grouping, dock tabs and diagnostic presentation.
- Missing foundation: Starfall editor executable/native consumption, central tokens/fonts/primitives, single interaction state, command history, persistence and real Draft 0 document.
- Later polish: production asset browsing, actionable consolidated validation, searchable log and status treatment.
- Explicitly deferred: generic docking, reflection inspector, scene framework, terrain/streaming, Pressure Cooker, runtime UI and Royale migration.