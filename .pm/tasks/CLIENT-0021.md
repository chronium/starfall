---
id: CLIENT-0021
title: Present a technical player through the local world adapter
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0020
createdAt: 2026-08-03T07:29:06.0319190Z
modifiedAt: 2026-08-04T08:06:34.3154310Z
---

Prove the reusable Client snapshot/fact-to-presentation boundary in the local walking graybox.

Acceptance criteria:
- Display the already-proven technical humanoid without claiming it is the selected dark-elf archer.
- Keep left-click output as movement intent only.
- Drive one client-owned world-presentation adapter with a deterministic authoritative-style player movement fixture.
- Present position, facing and locomotion without referencing Starfall.World or Starfall.Simulation and without deciding authoritative movement.
- Make CLIENT-0009 translate real protocol snapshots into this same adapter rather than implementing a second movement-presentation path.
- Do not select/cook the final archer, connect networking, implement prediction authority, combat, equipment or a general scene/entity framework.

## Notes

- 2026-08-04 08:06 UTC - Implemented the local walking presentation proof through the shared Client adapter.

  - Added deterministic 60 Hz `local_technical_player` fixture starting at the Draft 0 respawn anchor, with latest-intent direct movement, snapshot position/velocity/facing facts, and stateless snapshot-to-world/idle-walk presentation.
  - Added exact integer-tenths speed tuning (default 40 = 4.0 m/s, range 1-120) on non-repeated numpad +/- and exposed speed plus camera distance in the native title/diagnostics.
  - Made F1 follow the latest presented player and added non-repeated Up/Down 0.5 m distance tuning from 10-60 m; F2-F7 remain fixed.
  - Selected the existing Idle_Loop and Walk_Loop from the unchanged technical cook, retained a 0.25 s wall-clock crossfade, and applied the owner-approved presentation-only square-root cadence sqrt(planar speed / 1.0 m/s), producing 1x at 1.0 m/s and 2x at the 4.0 m/s default.
  - Preserved the historical seven-view capture recipe as an explicit idle (100,0,100) adapter fixture with unchanged fingerprints.
  - Documented the reusable adapter contract and CLIENT-0009 continuity. No interpolation, smoothing, prediction, reconciliation, collision, navigation, gameplay authority, networking, final archer selection or content cooking was added.

  Validation:
  - dotnet restore Starfall.slnx: passed.
  - Debug and Release full solution builds: passed with 0 warnings/errors.
  - Debug and Release full solution tests: 103/103 passed in each configuration (23 architecture, 42 Client, 14 Content, 24 Protocol).
  - dotnet format whitespace --verify-no-changes: passed. Full analyzer-format reports only existing coordinator-pinned SDL3-CS naming warnings outside Starfall.
  - Non-graphical character-content probe: passed for the 65-joint cook and Idle_Loop/Walk_Loop/Sword_Attack.
  - Native seven-view 1920x1080 capture suite: passed; all seven frozen fingerprints remained unchanged.
  - Headless World, Simulation and Balance Lab outputs contain no SDL, ChronoFall.CharacterPresentation, SimpleMesh, native graphics or generated client content.
  - pm doctor, linked-family resolution and git diff --check: passed with no family warnings.
  - Owner native validation on 2026-08-04: 60 Hz stepping was not noticeable; movement, camera and controls worked; after review-driven tuning, the 4.0 m/s default with 2x square-root walk cadence was judged perfectly reasonable and acceptable for the technical placeholder.
  - No project-history artifact was requested for this routine walking checkpoint.