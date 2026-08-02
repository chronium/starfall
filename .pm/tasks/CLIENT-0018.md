---
id: CLIENT-0018
title: Present Basic and Fire Arrow projectiles
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0007
- CLIENT-0011
- SIM-0009
- PROTOCOL-0004
createdAt: 2026-08-02T15:49:18.5915080Z
modifiedAt: 2026-08-02T15:49:33.2306690Z
---

Present Basic Arrow and Fire Arrow notch/release, client-only arrow travel, impact, and readable Fire distinction from authoritative protocol facts. Use the approved bow, arrow, socket, grip, reference-point, aim, IK, and selected-animation inputs.

The server creates no spatial projectile entity and resolves each action at an explicit deterministic tick. Visual arrows never decide collision, damage, success, or timing. Validate visual trajectory, impact scheduling, and reconciliation against authoritative action/target/resolve facts. Do not add ammunition inventory, server projectile simulation, or a generic projectile/effect framework.