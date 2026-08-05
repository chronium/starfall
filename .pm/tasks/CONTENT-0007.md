---
id: CONTENT-0007
title: Define starter flyers and three camp compositions
track: CONTENT
milestone: M2
dependsOn:
- CONTENT-0006
- CONTENT-0014
createdAt: 2026-08-02T07:29:12.0679120Z
modifiedAt: 2026-08-05T06:44:18.6090600Z
---

Define the bounded Draft 0 starter-monster identities and the exact ordered camp assignments that consume the executable graybox catalog.

Acceptance criteria:
- Define the provisional technical identities starter_flyer_light and starter_flyer_heavy in that authoritative order.
- Record 700 and 2,000 authoritative health units (7 and 20 displayed HP) using the neutral Draft 0 gameplay scale.
- Treat “flyer” and hovering as presentation only: both archetypes remain authoritative ground-plane occupants with no airborne movement, navigation, targeting, or collision semantics.
- Define exactly ten ordered assignments: three light monsters in camp_easy; two light then two heavy monsters in camp_mixed; and three heavy monsters in camp_hard.
- Copy the exact approved spawn identities and GroundPoint values from Draft0GrayboxCatalog.FirstPlayable without retaining graybox spawn objects.
- Expose immutable definitions and an aggregate Draft 0 catalog with structural validation separated from canonical first-playable validation.
- Keep body/collision radius, movement speed, deterministic target selection and tie-breaking, awareness, pursuit/leash, attack range/damage/cadence, and return behavior evidence-gated to SIM-0010.
- Do not define camp capacity, spawn templates, replenishment, respawn timing, random selection, authoritative entity identities, runtime ownership, presentation, asset selection, or a generic monster framework.

## Implementation notes

Implemented the immutable `Draft0StarterMonsterCatalog.FirstPlayable` with the ordered `starter_flyer_light` and `starter_flyer_heavy` definitions and all ten exact graybox spawn assignments. Extracted `Draft0GameplayScales` into the neutral Content namespace and updated the archer catalog regression coverage.

Structural definitions snapshot mutable inputs into `ImmutableArray`, reject default/empty/null/duplicate input, and keep canonical Draft 0 validation in the aggregate catalog. The aggregate enforces exact archetype/camp order, known references, complete ordered spawn coverage, and exact copied `GroundPoint` values. It owns no behavior tuning, runtime entities, spawning, assets or presentation.

Groomed todo `SIM-0010` before activation so it explicitly owns body/collision radius, speed, target selection/tie-breaking, awareness, pursuit/leash, attack range/damage/cadence and return behavior. `CONTENT-0007` now directly depends on completed `CONTENT-0014`.

Validation:
- focused Starfall.Content suite: 26/26 passed;
- full Debug build: succeeded with 0 warnings and 0 errors;
- full Debug tests: 242/242 passed;
- full Release build: succeeded with 0 warnings and 0 errors;
- full Release tests: 242/242 passed;
- no native or visual validation required because this task changes deterministic BCL-only content and documentation only.
