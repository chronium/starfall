---
id: PROTOCOL-0007
title: Serialize combat action and outcome facts
track: PROTOCOL
milestone: M2
dependsOn:
- PROTOCOL-0004
- PROTOCOL-0006
createdAt: 2026-08-03T07:29:09.1256850Z
modifiedAt: 2026-08-03T07:29:43.4238720Z
---

Implement deterministic bounded serialization for the approved Draft 0 combat fact contract.

Acceptance criteria:
- Encode the three action intents, timing, rejection, ordered victims, integer resource/damage, defeat and respawn facts.
- Reject malformed, ambiguous, unsupported, non-finite or out-of-bound values deterministically.
- Preserve fixed ticks and finite single-precision metre target points without embedding simulation rules.
- Do not implement server routing, presentation, projectile entities or a generic protocol framework.