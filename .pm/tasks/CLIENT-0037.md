---
id: CLIENT-0037
title: Adopt authoritative Basic Arrow projectile presentation
track: CLIENT
milestone: M5
dependsOn:
- CLIENT-0018
- PROTOCOL-0016
- SERVER-0017
createdAt: 2026-08-07T08:31:17.8801000Z
modifiedAt: 2026-08-07T08:56:24.5968190Z
---

Replace CLIENT-0018's completed synthetic Basic Arrow flight with presentation reconstructed from authoritative projectile spawn and terminal facts.

Acceptance criteria:
- Preserve the selected arrow asset, right-hand nocking, socketed bow, static rendering and hit-feedback foundations proven by CLIENT-0018.
- Preserve the existing 12-tick notch/aim preparation after acceptance, then begin Bow_Shoot exactly six ticks before the authoritative release tick so its frame-3 marker aligns with projectile spawn at start tick plus 18.
- Keep the arrow visibly nocked until the matching authoritative spawn fact exists. Never detach or begin flight from animation time alone; if spawn is delayed, hold before release rather than presenting an unconfirmed projectile.
- Preserve existing rejected-state handling and ensure rejection never starts or detaches an arrow.
- Remove the frozen-target 150 ms synthetic flight and consume authoritative projectile spawn facts instead.
- Reconstruct the straight visual trajectory at the authoritative speed from projectile identity, frozen origin/direction and release tick.
- Terminate presentation from Hit, Blocked or TravelExhausted facts and clean up cancellation, disconnect and malformed/unresolved state without stale arrows or impacts.
- Use terminal Hit fields only for presentation and diagnostics; never mutate canonical monster health or death from them.
- Tolerate independent monster snapshot/tombstone ordering.
- Do not add local collision, damage authority, a second projectile simulation, Fire effects, Arrow Rain, ammunition or a generic effect framework.