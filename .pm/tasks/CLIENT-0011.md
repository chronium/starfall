---
id: CLIENT-0011
title: Render one provisional socketed Basic bow
track: CLIENT
milestone: M5
dependsOn:
- pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0020
createdAt: 2026-08-02T07:33:29.3670540Z
modifiedAt: 2026-08-06T06:43:00.0081990Z
---

Render one selected bow through a deliberately narrow Starfall-owned Basic Arrow attachment boundary.

Acceptance criteria:
- Own one provisional semantic hand-socket identity and the local bow transform required by Starfall.
- Consume the exact selected and staged bow input plus canonical SHARED-0020, whose shared harness proof uses only a harness-local technical transform.
- Render the bow on the technical or selected humanoid and obtain native placement validation from the owner.
- Keep authoritative combat outcomes and action timing outside the attachment.
- Do not depend on equipment state, starter loadouts, CONTENT-0009 or GAME-0005.
- Do not implement aiming, off-hand IK, generalized grip definitions, arbitrary attachment categories, arrow projectiles, armour mapping or final art.

## Notes

- 2026-08-07 - Implemented the Starfall-owned provisional Basic bow attachment. The semantic socket `basic-bow-left-hand` resolves UAL1 joint `hand_l`; the rigid local transform freezes the owner-reviewed 0.09 m grip offset, +0.03 m palm-depth offset, 80-degree twist and -70-degree roll. Row-vector placement is `bowLocal * socketModel * characterWorld`, and the same evaluated global pose drives both the skinning palette and socket.
- 2026-08-07 - Starfall.Client now validates, copies and loads only the selected `quaternius-medieval-weapons-bow-wooden.cfmesh`, its provenance and the Medieval Weapons CC0 licence. The successful stable-ID staging workflow reproduced cook SHA-256 `4c0ab766e7c622c0f52ff0ade3cb1992c6d96664233a4695fc049a3a9b1d642e`, provenance SHA-256 `d99a010fe7f357019413e624ca1c239092475d2edb52b61a8528bc775f5bce8e` and licence SHA-256 `d32abf5eb61a5d20c582525c2ee9d8d42d86401d6b3ea0a2d5283fcaecaa35b9`. The staged arrow remains excluded from Client output.
- 2026-08-07 - Validation passed: Debug and Release solution builds completed with zero warnings/errors; Debug and Release test suites each passed 470 tests; `--validate-character-content` resolved the exact character and bow identities; architecture/output inspection found the bounded seven Client content files and no character/static presentation artifacts in World, Simulation or Balance Lab. Native macOS ARM64 validation covered idle and repeated right-click walking; the owner confirmed the bow stays correctly placed in the left fist with the approved orientation and follows the hand without visible lag or separation.
- 2026-08-07 - Scope remains presentation-only. Historical graybox capture fingerprints were preserved. Bow-body action sequencing, arrow nocking/release/travel, combat timing, aiming, off-hand IK, equipment, generalized attachments, material mapping and final art remain with their existing owners.

- 2026-08-07 - Visual-checkpoint review: the owner chose to skip preservation because this in-world bow view overlaps the already-preserved shared socket proof. No screenshot or project-history artifact was added.
