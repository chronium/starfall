---
id: CLIENT-0012
title: Send combat and skill intent from connected controls
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0009
- CLIENT-0023
- PROTOCOL-0007
- SERVER-0008
- SIM-0004
- SIM-0009
- SIM-0007
createdAt: 2026-08-02T07:52:11.1885940Z
modifiedAt: 2026-08-03T07:30:50.7266850Z
---

Send complete Draft 0 combat and skill intent through the proven connected player and monster paths.

Acceptance criteria:
- Right-click a valid enemy to select and request Basic Arrow.
- Press 1 to request Fire Arrow against the selected valid target.
- Press 2 then right-click a valid ground point to request Arrow Rain; Escape or approved empty-ground input cancels targeting.
- Use the combat protocol and exact SERVER-0008 exchange path.
- Send intent only; the server decides range, facing, victims, damage, mana, death, timing and success.
- Present rejections/corrections without local outcome authority.
- Do not implement projectile collision, damage prediction, generic input binding or unrelated controls.