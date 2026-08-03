---
id: PROTOCOL-0006
title: Define combat action and outcome facts
track: PROTOCOL
milestone: M2
dependsOn:
- SIM-0004
- SIM-0009
- SIM-0007
- SIM-0011
createdAt: 2026-08-03T07:29:08.8739060Z
modifiedAt: 2026-08-03T07:29:43.4124470Z
---

Define transport-neutral Draft 0 combat commands and authoritative outcome facts after the domain behaviors exist.

Acceptance criteria:
- Define entity-target Basic Arrow and Fire Arrow intent plus point-target Arrow Rain intent.
- Carry stable action identity, actor/target, start/windup/resolve ticks, acceptance/rejection, ordered victims, integer damage/resource expenditure, defeat, protected-town return and respawn facts.
- Preserve the decision that arrows/effects are presentation and no action creates authoritative spatial projectile entities.
- Do not implement encoding, server exchange, simulation rules, generic abilities, chat or persistence.