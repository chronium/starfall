---
id: PROTOCOL-0007
title: Serialize first connected combat facts
track: PROTOCOL
milestone: M2
priority: medium
dependsOn:
- PROTOCOL-0004
- PROTOCOL-0006
createdAt: 2026-08-03T07:29:09.1256850Z
modifiedAt: 2026-08-05T19:47:21.2467950Z
---

Implement deterministic bounded serialization for the approved first connected-combat fact contract.

Acceptance criteria:
- Encode Basic Arrow intent, authoritative timing, acceptance, rejection, cancellation, integer damage, target defeat and bounded player-life/respawn facts.
- Reject malformed, ambiguous, unsupported, non-canonical or out-of-bound values deterministically.
- Preserve non-zero command/entity identities, fixed ticks and the admitted-session actor binding without embedding simulation rules.
- Do not encode Fire Arrow, Arrow Rain, ground-target points or mana.
- Do not implement server routing, presentation, projectile entities or a generic protocol framework.