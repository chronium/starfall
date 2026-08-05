---
id: CLIENT-0022
title: Present placeholder monsters from deterministic fixtures
track: CLIENT
milestone: M2
dependsOn:
- CLIENT-0021
- CONTENT-0007
createdAt: 2026-08-03T07:29:06.2919720Z
modifiedAt: 2026-08-05T13:30:16.0981100Z
---

Present bounded generated placeholder starter monsters in the local walking graybox before authoritative monster behavior exists.

Acceptance criteria:
- Build deterministic local presentation facts from CONTENT-0007's exact ten ordered assignments, stable spawn identities, archetype identities and approved ground-plane positions.
- Reuse the existing Client presentation boundary and shared static renderer with one generated forward-readable box-creature mesh; distinguish light and heavy archetypes through frozen uniform scale and colour.
- Present stable fixture identity, archetype, ground-plane position, diagnostic facing and gentle client-only hover/bob without changing Content values or implying altitude authority.
- Show these fixtures only in local preview and capture modes. Connected mode remains without monsters until CLIENT-0023 consumes authoritative world snapshots.
- Do not implement pursuit, attacks, lunge/return, hit feedback, death presentation, networking, monster AI, asset selection/acquisition, a skeletal monster pipeline or a generic entity renderer.

## Notes

- 2026-08-05 13:30 UTC - Implemented the visible-first local monster proof. Starfall.Client now derives ten immutable presentation snapshots from Draft0StarterMonsterCatalog.FirstPlayable and the exact ordered graybox spawns, renders one generated forward-readable 48-vertex/72-index box-creature mesh through the shared static renderer, distinguishes 1.0 m cyan light and 1.5 m orange heavy archetypes, faces each fixture toward its camp entry, and applies only a deterministic identity-phased 0.12 m/1.5 s client hover. Local preview and capture modes show the fixtures; connected mode remains monster-free for CLIENT-0023. Added focused fixture, validation, transform, hover, archetype, and geometry tests; updated architecture/roadmap/content wiki and README boundaries. Validation: full Starfall.slnx Debug and Release suites passed 279 tests each; focused Client suite passed 54 tests after formatting; deterministic macOS ARM64 SDL GPU capture emitted seven distinct 1920x1080 frames; pm doctor and git diff --check passed; family inspection reported three readable/write-trusted projects and zero warnings. Owner accepted the native placement, scale, colours, facing and hover on 2026-08-05 and chose preservation. Curated checkpoint: docs/project-history/2026-08-05-visible-placeholder-monsters/contact-sheet.png, SHA-256 208f782cd2fbffdc7e972aa77de1265b3a0627b4fb5827e26252bedb9915a286. Raw captures remain outside source control.