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
modifiedAt: 2026-08-06T11:40:15.3305100Z
---

Send Basic Arrow intent through the proven connected player and monster paths.

Acceptance criteria:
- Right-click valid ground to request movement in local and connected modes; left-click a live connected monster placeholder to select it and request Basic Arrow.
- A left-click miss clears the selected target and sends no command. Local preview left-click emits no combat intent.
- Pick bounded vertical presentation cylinders against the latest authoritative live-monster positions and collision radii; choose the nearest valid positive hit and break equal-distance ties by ascending entity identity.
- Preserve only the bounded selected-target state needed by later Fire Arrow controls and clear it when the authoritative target is no longer live.
- Use PROTOCOL-0007 and the exact SERVER-0008 exchange path. Correlate accepted, rejected, canceled and resolved facts without assuming their order relative to monster snapshots.
- Send intent only; World decides range, facing, cancellation, damage, death, timing and success.
- Natively validate Client and World together: valid intent, deterministic target choice, authoritative acceptance/rejection/cancellation, visible hit flash, health reduction and connected monster defeat.
- Do not wait for animation, bow, arrow, ImGui or permanent HUD work.
- Do not implement Fire Arrow, Arrow Rain, cursor styling, movement markers, damage prediction or generic input binding.

## Notes

- 2026-08-06 11:40 UTC - Implemented connected Basic Arrow client intent and Draft 0 pointer semantics. Right-click now submits movement in local and connected modes; connected left-click uses a deterministic camera ray against latest live monster vertical cylinders, selects the nearest target with stable entity-ID tie-breaking, clears selection on misses or target removal, and sends the bounded Basic Arrow command on channels 5/6 with requester correlation, explicit accepted/rejected/canceled/resolved diagnostics, checked monotonic sequences, and bounded outstanding state. Updated the future Arrow Rain contract so left-click confirms ground targeting while right-click remains movement. Native validation on macOS ARM64 confirmed right-click movement, live target selection, exact 300-unit/3-point resolution, cadence and busy rejection, target-unavailable selection clearing, hit feedback, repeated light-monster defeats, and the owner-approved shrinking death presentation. The first native run exposed a non-uniform defeated transform rejected by the shared static renderer; corrected it to deterministic uniform collapse and added a regression proving StaticMeshDraw acceptance. Debug and Release solution builds completed with zero warnings/errors. All 400 tests passed in Debug; all Release projects passed, with Starfall.World.Tests passing 105/105 on the required isolated rerun after one aggregate test-host abort. Focused Client tests pass 72/72, scoped formatting and git diff --check pass, and no authoritative outcome is derived from client picking or presentation.