---
id: SERVER-0003
title: Admit world joins and own gameplay sessions
track: SERVER
milestone: M2
dependsOn:
- SERVER-0002
- PROTOCOL-0004
createdAt: 2026-08-02T07:29:12.5879430Z
modifiedAt: 2026-08-02T07:32:00.5595680Z
---

Implement the approved signed world-join ticket consumption path and create world-owned active gameplay sessions. Cover expiry, replay protection, failure responses, and continued gameplay when identity/lobby, chat, or operations are unavailable. Do not implement accounts, lobby UI, chat, persistence topology, or gameplay rules.