---
id: CLIENT-0019
title: Validate connected Basic Arrow with Combat diagnostics
track: CLIENT
milestone: M5
dependsOn:
- CLIENT-0012
- CLIENT-0007
- CLIENT-0011
- CLIENT-0037
- CLIENT-0031
createdAt: 2026-08-02T15:49:18.8424300Z
modifiedAt: 2026-08-07T08:32:06.5254910Z
---

Close the Connected Basic Arrow milestone through one ImGui Combat diagnostic and an owner-validated native end-to-end run.

Acceptance criteria:
- Show the authoritative projectile identity and terminal Hit, Blocked or TravelExhausted reason alongside authoritative target health, 300 internal units / 3 displayed damage, accepted/rejected/canceled state and monster-death outcome.
- Treat terminal Hit fields as presentation/diagnostic evidence only; monster snapshots and tombstones remain the source of canonical health/death.
- Use the completed Development Instrumentation shell and console infrastructure without defining a second debug protocol.
- Prove a connected player selects a real connected placeholder monster, sends Basic intent, receives World authority, presents bow-body animation, renders one socketed bow and authoritative visual arrow, shows hit feedback, reduces monster health and presents death.
- The game view needs the arrow, hit flash and death; this task does not establish floating combat text or a permanent target HUD.
- Obtain explicit macOS ARM64 owner validation.
- Do not add Mana, Fire Arrow, Arrow Rain, player-life presentation, permanent HUD, equipment, drops or progression.