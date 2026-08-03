---
id: SERVER-0003
title: Admit world joins and own gameplay sessions
track: SERVER
milestone: M2
dependsOn:
- SERVER-0002
- PROTOCOL-0002
createdAt: 2026-08-02T07:29:12.5879430Z
modifiedAt: 2026-08-03T07:30:50.6314360Z
---

Implement the completed signed world-join ticket consumption path and create world-owned active gameplay sessions.

Acceptance criteria:
- Validate the PROTOCOL-0002 ticket contract, enforce atomic single consumption and bind the admitted character/session to the intended world lifecycle.
- Cover expiry, replay protection, failure responses and continued active gameplay when identity/lobby, chat or operations are unavailable.
- Remain independent of gameplay command/snapshot serialization.
- Do not implement accounts, lobby UI, chat, persistence topology, movement, combat or generic service hosting.