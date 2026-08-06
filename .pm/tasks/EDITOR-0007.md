---
id: EDITOR-0007
title: Author and compile the proper Draft 0 scene
track: EDITOR
priority: none
dependsOn:
- EDITOR-0003
- CONTENT-0006
- CONTENT-0014
- CONTENT-0012
- EDITOR-0009
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0018
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0019
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/ASSET-0007
createdAt: 2026-08-03T07:29:10.1430770Z
modifiedAt: 2026-08-06T06:44:47.8310180Z
---

Create the first real Starfall Editor workflow for the proper Draft 0 scene after the generated graybox and editor interaction foundation have provided evidence.

Ownership:
- Consume EDITOR-0009 for shared selection/action routing, input focus, tool state, generic command history and UI-state persistence.
- Own the actual Draft 0 authoring document, real hierarchy concepts, viewport picking, transforms, carefully designed inspectors, concrete document commands, validation navigation and bounded compilation.
- Use only identities already established by Draft0GrayboxCatalog.FirstPlayable and exact selected assets; synthetic UI-showcase examples do not become content identities.
- Auxiliary Assets/Validation/Log/status polish remains EDITOR-0010 and does not block this task or its runtime consumers.

Acceptance criteria:
- Author one bounded scene from CONTENT-0006 requirements, CONTENT-0014 graybox evidence and exact CONTENT-0012/coordinator-staged assets.
- Present understandable authoring concepts for world geometry, routes, camps, protected town, landmarks, spawn points, navigation/collision inputs and presentation objects rather than raw storage or runtime implementation details.
- Keep hierarchy, viewport and inspector synchronized through one selection; support real picking, focus selection, translate/rotate/scale, local/world orientation, snapping and inline validation.
- Implement concrete undoable document commands for applicable transform, duplicate, delete, copy and paste operations. Ordinary reversible delete is immediate and undoable; confirm only irreversible, external-file, cascading or non-restorable operations.
- Group the inspector by authoring concept with aligned property rows, axis-aware values, meaningful reset/revert actions and validation beside affected properties. Do not create a reflection-driven universal inspector.
- Compile separate deterministic authoritative and client-presentation outputs from the same fully validated authoring revision, with stable cross-output identities; emit neither output if complete validation fails.
- Any coordinator static-rendering source consumption must be explicitly allowlisted and architecture-tested.
- Add deterministic document/command/compiler tests and native visual validation using real no-selection, selected-model, spawn-like, invalid-property and transform-tool states.
- Do not create a general scene/terrain/biome system, docking framework, reflective component system, runtime editor, streaming, NPC, crafting, commerce or additional zones.