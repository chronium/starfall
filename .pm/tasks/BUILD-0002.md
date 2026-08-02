---
id: BUILD-0002
title: Establish Starfall repository and solution boundaries
track: BUILD
milestone: M0
dependsOn:
- ARCH-0004
createdAt: 2026-08-01T05:46:46.9425230Z
modifiedAt: 2026-08-02T07:30:17.2200000Z
---

Create the independently buildable Starfall .NET solution, repository policy, and compile-time project graph for client presentation, headless world hosting, authoritative simulation, protocol, content, editor authoring, Balance Lab, and tests. Add dependency-direction validation and PM/wiki routing. Create library and test skeletons only; runnable process shells and local launch workflow belong to BUILD-0003. Do not implement gameplay, networking, rendering integration, services, or persistence.