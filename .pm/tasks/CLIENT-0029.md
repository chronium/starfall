---
id: CLIENT-0029
title: Adopt the shared ImGui backend in Starfall.Client
track: CLIENT
milestone: M4
dependsOn:
- CLIENT-0020
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0026
createdAt: 2026-08-06T06:41:23.5102280Z
modifiedAt: 2026-08-06T07:58:45.1840080Z
---

Add the approved family-source reference and instantiate the caller-controlled shared ImGui backend in Starfall.Client. Verify the exact source allowlist, Client-only native assets, lifecycle compatibility, and headless isolation. Exclude windows, menus, commands, visibility behavior, and permanent game UI.