---
id: CLIENT-0028
title: Send Arrow Rain intent from connected controls
track: CLIENT
priority: none
dependsOn:
- CLIENT-0012
- PROTOCOL-0012
- SERVER-0014
createdAt: 2026-08-05T19:46:45.9800640Z
modifiedAt: 2026-08-06T11:31:34.3983310Z
---

Add the focused connected Arrow Rain ground-targeting control after Basic Arrow input is proven.

Acceptance criteria:
- Pressing 2 enters bounded Arrow Rain targeting mode; left-click submits a valid finite ground point and Escape cancels without sending an action. Right-click retains its movement meaning.
- Ignore repeated key events and use the approved Arrow Rain protocol and server exchange.
- Consume authoritative acceptance, rejection, cancellation and outcome facts without predicting mana, victims, damage, death, timing or success.
- Keep cursor styling, targeting visuals, falling arrows and effects in their separately owned tasks.
- Do not add Fire Arrow controls, generic input binding or local gameplay authority.