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
modifiedAt: 2026-08-03T07:29:43.3072960Z
---

Replace deterministic monster fixtures with real bounded monster snapshots while reusing the local presentation adapter.

Acceptance criteria:
- Consume the approved monster protocol facts received through the monster server-exchange path.
- Preserve stable identities, explicit ordering, ground-plane authority and client-only hover/effects.
- Reuse the placeholder-monster rendering path without selected final assets.
- Do not implement monster AI, combat authority, asset selection/acquisition or a second snapshot-to-presentation adapter.