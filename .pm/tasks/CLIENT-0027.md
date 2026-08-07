---
id: CLIENT-0027
title: Send Fire Arrow intent from connected controls
track: CLIENT
priority: none
dependsOn:
- CLIENT-0012
- CLIENT-0037
- PROTOCOL-0011
- SERVER-0013
createdAt: 2026-08-05T19:46:45.6872510Z
modifiedAt: 2026-08-07T08:32:06.4198780Z
---

Add the focused connected Fire Arrow control after Basic Arrow target selection and authoritative projectile presentation are proven.

Acceptance criteria:
- Pressing 1 requests Fire Arrow against the currently selected live valid target through the approved Fire Arrow protocol and server exchange.
- Preserve the selected-target identity established by CLIENT-0012 and ignore repeated key events.
- Consume authoritative Fire acceptance, rejection, cancellation, mana and reused projectile spawn/terminal facts without predicting resource expenditure, collision, damage, death, timing or success.
- Reuse CLIENT-0037's authoritative straight-projectile presentation instead of creating another flight path; Fire-specific visual differentiation remains separately owned.
- Do not add Arrow Rain targeting, a generic input-binding system, local gameplay authority or a second projectile presenter.