---
id: PROTOCOL-0002
title: Establish protocol and replication boundary
track: PROTOCOL
milestone: M0
dependsOn:
- ARCH-0004
- BUILD-0002
createdAt: 2026-08-01T05:46:47.1759960Z
modifiedAt: 2026-08-01T06:49:24.2757570Z
---

Define the first Starfall command, admission, gameplay-session, and snapshot boundaries for the persistent-world vertical slice without borrowing Royale game-specific protocol.

## Contract requirements

- Define the handoff from authenticated lobby selection to a short-lived signed world-join ticket.
- Decide ticket claims, signing/validation, expiry, replay protection, consumption, and failure responses during this task's owner-approved plan.
- The selected world consumes the ticket and creates a world-owned gameplay session; active gameplay performs no continuing identity-service authorization calls.
- Keep combat events, loot ownership, progression, equipment changes, and other gameplay-critical notifications on the game protocol.
- Keep chat routing and delivery outside the gameplay protocol while allowing explicit presence or proximity inputs where needed.
- Do not implement chat, persistence topology, distributed transactions, or a generic service framework.