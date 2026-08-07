---
id: CLIENT-0018
title: Present the Basic Arrow projectile and impact
track: CLIENT
milestone: M5
dependsOn:
- CLIENT-0007
- CLIENT-0011
- CLIENT-0012
createdAt: 2026-08-02T15:49:18.5915080Z
modifiedAt: 2026-08-07T08:29:07.7027970Z
---

Present the connected Basic Arrow release, client-only arrow travel, impact and readable hit feedback from authoritative Basic facts.

Acceptance criteria:
- Reuse the approved bow-body release timing and socketed bow presentation.
- Schedule visual release, travel and impact from authoritative action, target and resolve facts.
- Keep the visual arrow client-owned: it never decides collision, damage, success, death or authoritative timing.
- Handle rejection, cancellation and correction without leaving stale projectiles or impacts.
- Do not add Fire Arrow, Arrow Rain, ammunition inventory, server projectile simulation or a generic projectile/effect framework.

## Notes

- 2026-08-07 08:29 UTC - Completed the approved client-only Basic Arrow presentation contract.

  - Added the exact staged `quaternius-medieval-weapons-arrow` static mesh to the Client-only content boundary.
  - Nocks the arrow from the evaluated `hand_r` pose, emits the reviewed Bow_Shoot frame-3 release marker once, follows a frozen resolved target through a deterministic 150 ms synthetic flight, holds impact for 80 ms, and triggers presentation-only hit feedback.
  - Rejection, cancellation, mismatched release and missing-target paths leave no stale projectile or impact.
  - Authority remains unchanged: the current resolved Basic Arrow fact decides damage/death; the visual arrow does not decide collision or gameplay.
  - Owner native validation confirmed correct arrow orientation, detachment and presentation behavior. The owner subsequently approved a future authoritative straight-projectile direction; this completed synthetic implementation remains historical evidence and will be replaced only by a separately planned successor Client task.

  Validation:
  - `dotnet restore Starfall.slnx` — passed.
  - `dotnet build Starfall.slnx --no-restore` — passed with 0 warnings and 0 errors.
  - `dotnet test Starfall.slnx --no-restore --no-build` — passed 481 tests.
  - `pm doctor` — passed with the existing legacy-milestone-schema and empty-M3 warnings.