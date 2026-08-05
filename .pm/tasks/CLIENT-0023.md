---
id: CLIENT-0023
title: Connect placeholder monster presentation to world snapshots
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0009
- CLIENT-0022
- PROTOCOL-0005
- SERVER-0007
createdAt: 2026-08-03T07:29:06.5613970Z
modifiedAt: 2026-08-05T19:01:48.4387700Z
---

Replace deterministic local monster fixtures with real bounded monster snapshots while reusing and extending the local presentation adapter.

Acceptance criteria:
- Consume the approved monster protocol facts received through the monster server-exchange path.
- Preserve stable identities, explicit ordering, ground-plane authority and the generated placeholder rendering path without selected final assets.
- Extend the Client adapter with the approved behavior/target, health, disengage/return and death facts; own corresponding client-only lunge/return, hit/death and hover effects without deciding outcomes.
- Remove the local fixtures in connected mode rather than creating a second snapshot-to-presentation path.
- Do not implement monster AI, combat authority, asset selection/acquisition or a generic entity renderer.

## Notes

- 2026-08-05 19:01 UTC - Implemented the connected placeholder-monster presentation path.

  - Connected Client retains the latest valid channel-4 bounded monster snapshot independently of movement readiness, ignores stale sequences, and rejects a newer sequence with a backwards simulation tick.
  - The shared local/connected adapter maps exact protocol order and stable entity identities to authoritative ground-plane transforms, while Client-only presentation adds the approved hover, attack-transition lunge, returning desaturation, health-decrease flash, and bounded tombstone collapse.
  - Initial observations do not fabricate transition effects; repeated tombstones do not restart death; no interpolation, prediction, smoothing, health bars, combat authority, or asset selection was added.
  - Updated durable protocol, network, lifecycle, content, roadmap, overview, and README documentation.
  - Validation: focused formatting passed; Debug and Release builds completed with zero warnings/errors; all 338 Starfall tests passed in both configurations (37 architecture, 62 client, 1 connected UDP loopback, 31 content, 69 protocol, 46 simulation, 92 world); PM doctor and git diff --check passed; linked-family inspection reported three available/readable/write-trusted projects and zero warnings.
  - Native macOS ARM64 validation used a real loopback World and signed admission ticket. The Client reported 10 authoritative monsters and the owner confirmed connected movement, pursuit/lunge, disengagement/return, and debug presentation appeared to work correctly.
  - Hit/death effects remain deterministically test-covered because no connected combat-input path exists yet.
  - The owner explicitly skipped project-history preservation because the movement behavior is not captured well by a still image or small screenshot series.