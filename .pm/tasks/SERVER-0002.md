---
id: SERVER-0002
title: Run a headless fixed-step world/channel lifecycle
track: SERVER
milestone: M2
dependsOn:
- BUILD-0003
createdAt: 2026-08-01T05:46:47.9049220Z
modifiedAt: 2026-08-02T07:30:17.2966720Z
---

Implement one headless world/channel process lifecycle with deterministic fixed-step scheduling, start/drain/stop behavior, isolated world identity, and empty authoritative state ownership. Prove that the world host has no client presentation, SDL, GPU, ImGui, editor UI, chat, identity, operations, or persistence hot-path dependency. Admission and zone/entity hosting are separate tasks.