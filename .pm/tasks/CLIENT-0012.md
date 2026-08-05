---
id: CLIENT-0012
title: Send Basic Arrow intent from connected controls
track: CLIENT
milestone: M2
priority: medium
dependsOn:
- CLIENT-0009
- CLIENT-0023
- PROTOCOL-0007
- SERVER-0008
createdAt: 2026-08-02T07:52:11.1885940Z
modifiedAt: 2026-08-05T19:47:21.4497360Z
---

Send Basic Arrow intent through the proven connected player and monster paths.

Acceptance criteria:
- Right-click a live connected monster placeholder to select it and request Basic Arrow.
- Pick against the latest authoritative monster positions and collision radii; choose the nearest valid positive hit and break equal-distance ties by ascending entity identity.
- Preserve only the bounded selected-target state needed by later Fire Arrow controls.
- Use PROTOCOL-0007 and the exact SERVER-0008 exchange path.
- Send intent only; the World decides range, facing, cancellation, damage, death, timing and success.
- Consume authoritative rejection/cancellation diagnostics while existing CLIENT-0023 health-change and defeat presentation shows the visible result.
- Do not implement Fire Arrow, Arrow Rain, cursor styling, movement markers, bow animation, projectiles, damage prediction or generic input binding.