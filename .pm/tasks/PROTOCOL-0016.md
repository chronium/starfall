---
id: PROTOCOL-0016
title: Replace Basic Arrow protocol with projectile facts
track: PROTOCOL
milestone: M5
dependsOn:
- SIM-0013
- PROTOCOL-0007
- PROTOCOL-0015
createdAt: 2026-08-07T08:31:17.3990070Z
modifiedAt: 2026-08-07T08:56:24.7112160Z
---

Replace the connected Basic Arrow fact and binary layouts in place with the authoritative straight-projectile lifecycle.

Acceptance criteria:
- Keep the admission-negotiated gameplay protocol at version 1 and retain no obsolete Basic Arrow compatibility reader or dual layout.
- Preserve actor-free Basic Arrow commands and admitted-session actor authority.
- Preserve BasicArrowRejected with its existing decision timing, validation rules and bounded rejection-reason semantics.
- Preserve accepted and pre-release canceled lifecycle semantics, and replace the resolved outcome with projectile-spawn and projectile-terminal facts.
- Accepted facts carry correlation, actor, original target, start tick and release tick.
- Spawn facts carry correlation, positive projectile identity, actor, original target, release tick, finite frozen ground-plane origin and normalized direction, plus the trajectory inputs required for deterministic presentation.
- Terminal facts carry projectile identity, terminal tick/position and exactly one reason: Hit, Blocked or TravelExhausted. Hit alone carries the contacted monster, requested/effective damage and defeat evidence.
- Treat Hit fields as presentation/diagnostic evidence; ongoing canonical monster health and death remain owned by snapshots and tombstones.
- Validate every fact before exact-length encoding and reject malformed, noncanonical, truncated or trailing-byte payloads deterministically.
- Do not add projectile snapshots, server routing, client behavior, Fire-specific facts, Arrow Rain or a generic protocol framework.