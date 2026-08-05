---
id: CLIENT-0023
title: Connect placeholder monster presentation to world snapshots
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0009
- CLIENT-0022
- PROTOCOL-0005
- SERVER-0007
createdAt: 2026-08-03T07:29:06.5613970Z
modifiedAt: 2026-08-05T13:18:20.2106660Z
---

Replace deterministic local monster fixtures with real bounded monster snapshots while reusing and extending the local presentation adapter.

Acceptance criteria:
- Consume the approved monster protocol facts received through the monster server-exchange path.
- Preserve stable identities, explicit ordering, ground-plane authority and the generated placeholder rendering path without selected final assets.
- Extend the Client adapter with the approved behavior/target, health, disengage/return and death facts; own corresponding client-only lunge/return, hit/death and hover effects without deciding outcomes.
- Remove the local fixtures in connected mode rather than creating a second snapshot-to-presentation path.
- Do not implement monster AI, combat authority, asset selection/acquisition or a generic entity renderer.