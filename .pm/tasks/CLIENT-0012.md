---
id: CLIENT-0012
title: Send Basic Arrow intent from connected controls
track: CLIENT
milestone: M5
priority: high
dependsOn:
- CLIENT-0009
- CLIENT-0023
- PROTOCOL-0007
- SERVER-0008
createdAt: 2026-08-02T07:52:11.1885940Z
modifiedAt: 2026-08-06T06:42:59.6471900Z
---

Send Basic Arrow intent through the proven connected player and monster paths.

Acceptance criteria:
- Right-click a live connected monster placeholder to select it and request Basic Arrow.
- Pick against the latest authoritative monster positions and collision radii; choose the nearest valid positive hit and break equal-distance ties by ascending entity identity.
- Preserve only the bounded selected-target state needed by later Fire Arrow controls.
- Use PROTOCOL-0007 and the exact SERVER-0008 exchange path.
- Send intent only; World decides range, facing, cancellation, damage, death, timing and success.
- Natively validate Client and World together: valid intent, deterministic target choice, authoritative acceptance/rejection/cancellation, visible hit flash, health reduction and connected monster defeat.
- Do not wait for animation, bow, arrow, ImGui or permanent HUD work.
- Do not implement Fire Arrow, Arrow Rain, cursor styling, movement markers, damage prediction or generic input binding.