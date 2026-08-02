---
id: CLIENT-0012
title: Send combat and skill intent from connected controls
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0009
- PROTOCOL-0004
- SIM-0004
- SIM-0009
- SIM-0007
createdAt: 2026-08-02T07:52:11.1885940Z
modifiedAt: 2026-08-02T15:52:42.7186750Z
---

Send the complete Draft 0 combat and skill intent from connected isometric controls.

Acceptance criteria:
- Right-click a valid enemy to select it and request Basic Arrow.
- Press 1 to request Fire Arrow against the selected valid target.
- Press 2 to enter Arrow Rain ground-targeting mode, then right-click a valid point to request the cast.
- Escape or an approved empty-ground action cancels targeting without sending a cast.
- Send intent only; the server decides range, facing, victims, damage, mana, death, timing, and success.
- Present rejections and authoritative corrections without local outcome authority.
- Do not implement projectile collision, damage prediction, generic input binding, or unrelated controls.