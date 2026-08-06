---
id: EDITOR-0010
title: Polish Starfall editor auxiliary surfaces
track: EDITOR
priority: none
dependsOn:
- EDITOR-0007
createdAt: 2026-08-05T07:40:19.7828820Z
modifiedAt: 2026-08-05T07:40:19.7917480Z
---

Polish the real Starfall editor's auxiliary Assets, Validation, Log and status surfaces after the bounded Draft 0 authoring workflow exists.

Acceptance criteria:
- Depend only on EDITOR-0007. EDITOR-0009 is transitive through EDITOR-0007 and must not be duplicated as a direct dependency.
- Consume and validate EDITOR-0009's lower-dock/tab/panel persistence machinery rather than creating a second persistence owner.
- Make the asset surface provide breadcrumbs/location, search, useful type filters, distinct folders/assets, proportionate thumbnails, truncation/tooltips and clear selected/hovered/missing/invalid/unsupported/empty states.
- Exclude irrelevant filesystem metadata such as .DS_Store and degrade cleanly when preview imagery is unavailable.
- Make validation rows expose severity by icon/text as well as colour, explain the affected authoring object/property and select/focus it through EDITOR-0007 adapters where possible.
- Make logs use restrained monospace text with severity, filtering and search without becoming an observability platform.
- Keep the resizable lower dock collapsible and preserve selected tab, height and collapsed state through interaction-owned persistence.
- Keep status focused on actionable saved/dirty state, validation result, active edit/runtime mode and background operations; place DPI and technical metrics in diagnostics rather than permanent status.
- Add native visual and interaction validation over real EDITOR-0007 adapters.
- Do not create an asset database, Pressure Cooker, package manager, content bundle, general filesystem browser or runtime UI, and do not block SERVER-0012 or CLIENT-0016.