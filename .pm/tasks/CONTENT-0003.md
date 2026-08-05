---
id: CONTENT-0003
title: Define the provisional dark-elf archer and three-action kit
track: CONTENT
milestone: M2
dependsOn:
- BUILD-0002
createdAt: 2026-08-01T05:46:47.6706230Z
modifiedAt: 2026-08-05T06:18:29.0688540Z
---

Define stable Draft 0 content identities and deterministic tuning inputs for the provisional dark-elf archer.

Acceptance criteria:
- Define stable class and action identities for Basic Arrow, Fire Arrow, and Arrow Rain.
- Record 2,500 authoritative health units (25 displayed points), integer mana/resources, ordinary-integer primary attributes, explicit integer probability representation, and fixed simulation ticks.
- Record Draft 0 displayed damage of 3, 7, and 5 respectively, represented as 300, 700, and 500 authoritative units.
- Treat mana, regeneration, skill costs, cadence, ranges, interruption, and resolve timing as configurable Balance Lab inputs.
- Define unlimited arrows for the slice: arrows are presented, but ammunition inventory and purchasing do not exist.
- Keep this task to deterministic content contracts; do not implement simulation, protocol, presentation, cooking, or final balance.

## Notes

- 2026-08-05 06:18 UTC - Implemented the bounded Draft 0 archer content contract.
  - Added immutable `Draft0ArcherKitDefinition` and ordered `basic_arrow`, `fire_arrow`, `arrow_rain` definitions for `dark_elf_archer`, with 2,500 health units, 300/700/500 damage units, 100 resource units per displayed point, 10,000 basis points and unlimited authoritative ammunition.
  - Centralized the existing lowercase-ASCII Content identity rule without adding a general content framework.
  - Added five focused Content tests covering exact catalog values/order, scale correspondence, structural immutable copying and constructor rejection boundaries.
  - Groomed SIM-0004, SIM-0009, SIM-0007 and EDITOR-0005 through PM MCP. SIM-0004 now directly depends on CONTENT-0003; every receipt identified `prj_pkIpzx0fzFD4URjvqBuYrGZF` and exactly one expected Starfall task file.
  - Added `content/draft-0-archer-kit` and updated the Draft 0 brief, architecture overview and bootstrap roadmap. The wiki records the later connected-combat-preset promotion gap and unresolved primary-attribute taxonomy/defaults.
  - Validation: `pm doctor` passed; linked-family inspection returned three readable/trusted members and zero warnings; `dotnet format Starfall.slnx --no-restore --include src tests tools` passed; Debug and Release solution builds passed with zero warnings/errors; Debug and Release suites each passed 235 tests; `git diff --check` passed.
  - No simulation, protocol, presentation, cooking, asset, Royale or coordinator-source implementation was added. No native or visual validation was required.