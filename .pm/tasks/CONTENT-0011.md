---
id: CONTENT-0011
title: Select provisional Basic archer presentation inputs
track: CONTENT
milestone: M5
dependsOn:
- CONTENT-0003
- CLIENT-0006
createdAt: 2026-08-02T15:48:39.7060610Z
modifiedAt: 2026-08-06T17:16:09.3197430Z
---

Select the exact Starfall-owned presentation inputs for the connected Basic Arrow proof: the existing technical humanoid as the complete provisional character body, one bow, one arrow, and the minimum compatible idle, locomotion, notch, neutral-aim and release clips.

Acceptance criteria:
- Record exact coordinator pack-relative paths, formats, hashes, raw scale evidence, skeleton/rest-transform compatibility, limitations and downstream owner visual-review needs.
- Treat the completed technical humanoid as the whole temporary character presentation; do not select a separate underlayer, final dark-elf model, Ranger armour or starter loadout.
- Treat completed EXPERIMENT-0014 as architectural and visual evidence; do not add it as a dependency because this task consumes no experiment artifact.
- Select only the verified compatible minimum clips from the existing UAL1 cook and owner-supplied private UAL2 Source evidence.
- Preserve the historical UAL1 cook unchanged; Sword_Attack is not an acceptable bow placeholder.
- Record timing and placement gaps for their existing downstream owners without deciding animation retiming, socket transforms, grip, projectile behavior or authoritative combat timing here.
- Do not copy, cook, convert, retarget, render, attach or integrate assets.

## Notes

- 2026-08-06 17:16 UTC - Completed the approved provisional Basic Arrow presentation selection.

  Selections:
  - Kept the existing `quaternius-ual1-standard` technical mannequin as the complete temporary body; no separate underlayer, final dark-elf model, Ranger armour, equipment or starter loadout was selected.
  - Kept UAL1 `Idle_Loop` and `Walk_Loop`, and selected only private UAL2 Source `Bow_Notch`, `Bow_Aim_Neutral` and `Bow_Shoot`. Exact-skeleton evidence requires no retargeting. `Bow_Shoot` frame 3 / 100 ms remains the provisional presentation release marker.
  - Selected Quaternius Medieval Weapons Pack OBJ/MTL `Bow_Wooden` and `Arrow` with exact hashes and CC0 evidence. OBJ/MTL avoids a manual GLB export and uses the already-supported static-cooker input path.
  - Recorded raw unitless source dimensions and a 0.25 metres-per-source-unit acquisition candidate; ASSET-0006 must verify or reject it through deterministic cooking and native scale review.
  - Recorded the 2.5-second `Bow_Notch` versus 0.20-second authoritative windup mismatch for CLIENT-0007. No animation retiming or gameplay timing changed.

  Documentation:
  - Added `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/basic-arrow-presentation-inputs`.
  - Updated the Draft 0 archer ownership section to link the selection and preserve downstream task boundaries.

  Validation:
  - Every PM mutation receipt identified Starfall project `prj_pkIpzx0fzFD4URjvqBuYrGZF` and only the expected task/state/wiki paths.
  - Starfall `pm doctor` passed.
  - Linked-family inspection reported three readable/trusted members and zero warnings.
  - Exact UAL1, OBJ, MTL and licence hashes matched the recorded values.
  - Private absolute-path search and `git diff --check` passed.
  - Diff review found only Starfall PM/wiki changes; no source, supplied asset, generated output, Royale, coordinator PM or gitlink change occurred.
  - No build, cook or native run was required for this selection-only task. Existing EXPERIMENT-0014 evidence and the owner's explicit selections provide the current visual basis; equipped appearance remains gated downstream.