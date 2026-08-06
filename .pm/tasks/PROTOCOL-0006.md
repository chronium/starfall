---
id: PROTOCOL-0006
title: Define first connected combat facts
track: PROTOCOL
milestone: M5
priority: high
dependsOn:
- SIM-0004
- SIM-0011
createdAt: 2026-08-03T07:29:08.8739060Z
modifiedAt: 2026-08-06T06:42:59.3209610Z
---

Define transport-neutral facts for the connected Basic Arrow deliverable.

Acceptance criteria:
- Define a Basic Arrow request with a non-zero command sequence and target entity identity. The client never supplies the acting entity.
- World derives the authoritative actor from the admitted gameplay session; actor identity appears only in authoritative outcome facts.
- Carry stable action, actor and target identities, authoritative start and resolve ticks, acceptance, rejection, cancellation, 300 internal-unit damage, effective damage and monster defeat.
- Retain SIM-0011 only for defeated-player and protected-town action rejection; do not transport player health, defeat, restoration or respawn here.
- Preserve the decision that arrows are presentation and no action creates an authoritative spatial projectile entity.
- Do not implement encoding, server exchange, Fire Arrow, Arrow Rain, mana, client presentation, chat or persistence.