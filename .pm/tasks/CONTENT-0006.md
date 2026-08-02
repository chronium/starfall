---
id: CONTENT-0006
title: Define the Draft 0 first playable zone
track: CONTENT
milestone: M2
dependsOn:
- BUILD-0002
createdAt: 2026-08-02T07:29:11.8166540Z
modifiedAt: 2026-08-02T18:35:14.7354900Z
---

Define the validated Draft 0 regional zone contract and BCL-only spatial authoring inputs without freezing the exact graybox placement.

Acceptance criteria:
- Define finite single-precision metre value types backed by System.Numerics.Vector3 for ground authoring inputs, with a 200 x 200 metre ground-space envelope and explicit out-of-zone rejection.
- Record the approximately 50 x 50 metre protected-town target near one map edge, a required respawn anchor, two-to-three landmark requirement, one exit and one junction.
- Define stable short, medium and long branch identities with experimental travel targets around 25, 45 and 70 metres.
- Define broad/open, elongated or divided, and tight/bowl camp-geometry requirements plus flat ground, grass, dirt-path and outer rock-boundary semantics.
- Record the hybrid numerical policy: integer gameplay resources and discrete rules, integer fixed ticks and probability scales, and Box3D-native floating-point metres for later authoritative spatial/physics state.
- Keep exact town, landmark, respawn, route, camp, collision and navigation placement in a focused dependent graybox task.
- Do not add Box3D, serialization, simulation, protocol, rendering, asset selection, terrain, streaming, biome, general world-format or final-art implementation.

## Notes

- 2026-08-02 18:35 UTC - Implemented the approved regional Draft 0 zone contract without authoring the exact graybox or integrating Box3D.

  - Added BCL-only immutable ground-plane value types backed by System.Numerics.Vector3 and a validated Draft0ZoneCatalog contract for the 200 x 200 metre envelope, 50 x 50 metre protected-town target, ordered 25/45/70 metre branches, camp-geometry intent, and stable surface/boundary identities.
  - Recorded the hybrid numerical policy: finite single-precision metre spatial/physics state; integer resources, attributes, probabilities, and fixed ticks; stable entity IDs and explicit gameplay ordering; initial finite IEEE-754 protocol values with quantization deferred.
  - Added Starfall.Content.Tests with five deterministic validation tests and registered it in the solution and architecture allowlist.
  - Created todo follow-up CONTENT-0014 for exact coordinates, shapes, paths, collision/navigation inputs, and placements. Rewired SERVER-0004, SIM-0008, SIM-0011, and CLIENT-0016 to the exact-layout task while preserving regional consumers on CONTENT-0006.
  - Groomed PROTOCOL-0003, PROTOCOL-0004, SIM-0008, and CLIENT-0009 to preserve the approved numerical, ordering, serialization, and reconciliation boundaries. No Box3D version, binding, dependency, or integration was selected.
  - Added wiki content/draft-0-zone-contract and updated the Draft 0 brief, architecture overview, product direction, and bootstrap graph.

  Validation:
  - dotnet restore Starfall.slnx passed.
  - Debug and Release solution builds passed with zero warnings and errors.
  - Debug and Release tests passed: 23 architecture + 5 content tests in each configuration.
  - Targeted dotnet format verify passed for Starfall.Content, Starfall.Content.Tests, and Starfall.Architecture.Tests. Whole-solution format verification reaches the approved source-built SDL3-CS checkout and reports its existing IDE1006 naming diagnostics; no third-party files were changed.
  - Starfall and coordinator pm doctor passed; family inspection returned all three projects readable/write-trusted with zero warnings.
  - PM rereads confirmed CONTENT-0014 and all exact-layout consumers remain todo and blocked; only CONTENT-0006 was active before completion.
  - git diff --check passed.