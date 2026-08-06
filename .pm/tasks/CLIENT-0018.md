---
id: CLIENT-0018
title: Present the Basic Arrow projectile and impact
track: CLIENT
milestone: M5
dependsOn:
- CLIENT-0007
- CLIENT-0011
- CLIENT-0012
createdAt: 2026-08-02T15:49:18.5915080Z
modifiedAt: 2026-08-06T06:43:00.1357470Z
---

Present the connected Basic Arrow release, client-only arrow travel, impact and readable hit feedback from authoritative Basic facts.

Acceptance criteria:
- Reuse the approved bow-body release timing and socketed bow presentation.
- Schedule visual release, travel and impact from authoritative action, target and resolve facts.
- Keep the visual arrow client-owned: it never decides collision, damage, success, death or authoritative timing.
- Handle rejection, cancellation and correction without leaving stale projectiles or impacts.
- Do not add Fire Arrow, Arrow Rain, ammunition inventory, server projectile simulation or a generic projectile/effect framework.