---
id: SERVER-0003
title: Admit world joins and own gameplay sessions
track: SERVER
milestone: M2
dependsOn:
- SERVER-0002
- PROTOCOL-0002
createdAt: 2026-08-02T07:29:12.5879430Z
modifiedAt: 2026-08-03T07:56:12.0520850Z
---

Implement the completed signed world-join ticket consumption path, its narrow host-specific admission exchange, and world-owned active gameplay sessions.

Acceptance criteria:
- Bind only the PROTOCOL-0002 join request and accept/reject facts to the World host boundary; receive one bounded join request and return the approved admission result without introducing a general networking or framing framework.
- Validate the PROTOCOL-0002 ticket contract, enforce atomic single consumption and bind the admitted character/session to the intended world lifecycle.
- Cover expiry, replay protection, failure responses and continued active gameplay when identity/lobby, chat or operations are unavailable.
- Remain independent of PROTOCOL-0004 connected-walking serialization and all movement/combat exchange.
- Do not implement accounts, lobby UI, chat, persistence topology, movement, combat, generic service hosting or general transport infrastructure.