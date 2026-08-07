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

## Notes

- 2026-08-07 - Implemented the bounded Draft 0 straight-projectile Content contract.
  - Added immutable `Draft0StraightProjectileActionDefinition` and `Draft0StraightProjectileCatalog.BasicArrow`, referencing CONTENT-0003's canonical Basic Arrow action instead of duplicating its identity, target kind, 300-unit damage or Mana policy.
  - Frozen the approved 18-tick release delay, 60 m/s speed, 0.05-metre radius, 12-metre maximum travel and inclusive selection range, 45-degree facing threshold and 48-tick cadence while preserving unlimited ammunition.
  - Added focused validation for selected-entity actions, non-zero ticks, finite positive spatial metres, bounded facing and travel sufficient for the selection boundary.
  - Added five focused Content test cases/theories covering exact values and malformed inputs; updated the archer-kit and authoritative-straight-projectile wiki contracts through PM MCP.
  - Validation: `dotnet format Starfall.slnx --no-restore --include src tests tools` passed; restore was current; the solution build succeeded with zero warnings/errors; all 493 solution tests passed; `git diff --check` passed.
  - `pm doctor` passed with the existing legacy milestone-schema and empty-M3 warnings. Linked-family inspection reported all three members readable/write-trusted with zero resolution warnings.
  - No Simulation, collision, Protocol, World, Client, Fire Arrow, Arrow Rain, asset, native or visual work was included. `SIM-0013` remains the next independent consumer and was not activated.
