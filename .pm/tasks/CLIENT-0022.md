---
id: CLIENT-0022
title: Present placeholder monsters from deterministic fixtures
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0021
- CONTENT-0007
- SIM-0010
createdAt: 2026-08-03T07:29:06.2919720Z
modifiedAt: 2026-08-03T07:29:43.2938970Z
---

Present bounded placeholder starter monsters in the local walking graybox after authoritative behavior exists.

Acceptance criteria:
- Drive the existing Client world-presentation adapter with deterministic starter-flyer state fixtures shaped by SIM-0010.
- Use generated shapes or separately approved temporary assets; do not wait on final monster selection/acquisition.
- Present ground-plane position, facing, simple hover/bob/lunge/return and death state without deciding AI, movement, targeting, damage or altitude.
- Do not connect networking, select monster source assets, add a skeletal monster pipeline or create a generic entity renderer.