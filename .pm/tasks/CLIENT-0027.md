---
id: CLIENT-0027
title: Send Fire Arrow intent from connected controls
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0012
- PROTOCOL-0011
- SERVER-0013
createdAt: 2026-08-05T19:46:45.6872510Z
modifiedAt: 2026-08-05T19:47:21.9477630Z
---

Add the focused connected Fire Arrow control after Basic Arrow target selection is proven.

Acceptance criteria:
- Pressing 1 requests Fire Arrow against the currently selected live valid target through the approved Fire Arrow protocol and server exchange.
- Preserve the selected-target identity established by CLIENT-0012 and ignore repeated key events.
- Consume authoritative acceptance, rejection, cancellation and outcome facts without predicting mana, damage, death, timing or success.
- Keep cursor styling, bow animation, projectiles and effects in their separately owned tasks.
- Do not add Arrow Rain targeting, generic input binding or local gameplay authority.