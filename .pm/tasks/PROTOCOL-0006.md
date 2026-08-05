---
id: PROTOCOL-0006
title: Define first connected combat facts
track: PROTOCOL
milestone: M2
priority: medium
dependsOn:
- SIM-0004
- SIM-0011
createdAt: 2026-08-03T07:29:08.8739060Z
modifiedAt: 2026-08-05T19:47:21.0957700Z
---

Define transport-neutral facts for the first connected combat slice after Basic Arrow and player-life behavior are proven.

Acceptance criteria:
- Define a Basic Arrow command with non-zero command sequence and target entity identity; the admitted World session, not client payload, supplies the actor.
- Carry stable action, actor and target identities, authoritative start/resolve ticks, acceptance, rejection, cancellation, 300-unit damage, effective damage and target defeat facts.
- Carry bounded authoritative player health, defeat, protected-town respawn and restoration facts from SIM-0011.
- Preserve the decision that arrows are presentation and no action creates an authoritative spatial projectile entity.
- Do not implement encoding, server exchange, Fire Arrow, Arrow Rain, mana, client presentation, chat or persistence.