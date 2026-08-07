---
id: SERVER-0017
title: Integrate authoritative projectile lifecycle and exchange
track: SERVER
milestone: M5
dependsOn:
- SIM-0013
- PROTOCOL-0016
- SERVER-0008
createdAt: 2026-08-07T08:31:17.6401620Z
modifiedAt: 2026-08-07T08:31:38.4960090Z
---

Integrate the approved Basic Arrow projectile lifecycle into the headless World and connected exchange.

Acceptance criteria:
- Allocate positive checked monotonic projectile world-entity identities without reuse.
- Bind commands to admitted actors, invoke the authoritative frozen-aim rule and own release, advancement, collision-query integration, damage ordering and removal in the fixed-step World lifecycle.
- Move monsters before release validation and projectile advancement. Do not advance a projectile on its release tick.
- Advance projectiles by ascending projectile ID, resolve their damage before monster attacks are applied, and discard any generated monster attack whose attacker was killed by a projectile.
- Publish accepted/canceled/spawn/terminal facts to the requesting admitted session over the existing reliable ordered combat outcome path while preserving monster snapshots/tombstones as canonical state.
- Keep released projectiles alive after shooter movement, defeat or disconnect and after original-target movement or death.
- Preserve headless isolation and gameplay protocol version 1.
- Do not add Fire Arrow, Arrow Rain, projectile snapshot streaming, persistence, a generic exchange host or presentation code.