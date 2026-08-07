---
id: CONTENT-0017
title: Freeze Draft 0 straight-projectile inputs
track: CONTENT
milestone: M5
dependsOn:
- CONTENT-0003
createdAt: 2026-08-07T08:31:16.9029770Z
modifiedAt: 2026-08-07T08:31:38.1480110Z
---

Freeze the bounded tunable Content inputs for Draft 0 straight-projectile actions, beginning with Basic Arrow.

Acceptance criteria:
- Define one narrowly parameterized immutable Draft 0 straight-projectile input that Basic Arrow and later Fire Arrow can share without creating a generalized projectile framework.
- Preserve the stable `basic_arrow` action identity and freeze 300 internal damage units, an 18-tick release delay, 60 metres/second speed, 0.05-metre projectile radius, 12-metre maximum travel, the existing inclusive 12-metre selection range, 45-degree facing constraint and 48-tick cadence.
- Keep discrete gameplay values and ticks deterministic; require finite positive spatial inputs in metres.
- Keep unlimited ammunition unchanged.
- Own inputs and validation only. Do not implement simulation, collision, protocol, World lifecycle, client presentation, Fire Arrow mana/effects or Arrow Rain.