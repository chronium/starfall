---
id: PROTOCOL-0007
title: Serialize first connected combat facts
track: PROTOCOL
milestone: M5
priority: high
dependsOn:
- PROTOCOL-0004
- PROTOCOL-0006
createdAt: 2026-08-03T07:29:09.1256850Z
modifiedAt: 2026-08-06T06:42:59.4416160Z
---

Implement deterministic bounded serialization for the connected Basic Arrow fact contract.

Acceptance criteria:
- Encode Basic Arrow intent, authoritative actor and target facts, timing, acceptance, rejection, cancellation, integer damage and monster defeat.
- Reject malformed, ambiguous, unsupported, non-canonical or out-of-bound values deterministically.
- Preserve non-zero command and entity identities, fixed ticks, and admitted-session actor binding without embedding simulation rules.
- Do not encode player-life or respawn payloads, Fire Arrow, Arrow Rain, ground-target points or mana.
- Do not implement server routing, presentation, projectile entities or a generic protocol framework.