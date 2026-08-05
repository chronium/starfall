---
id: PROTOCOL-0005
title: Add bounded monster facts and serialization
track: PROTOCOL
milestone: M2
dependsOn:
- PROTOCOL-0004
- SIM-0006
- SIM-0010
- SIM-0011
createdAt: 2026-08-03T07:29:08.6092280Z
modifiedAt: 2026-08-05T17:45:30.6017130Z
---

Extend the established connected-walking protocol with bounded, full-state monster snapshot facts and deterministic serialization.

Acceptance criteria:
- Represent stable monster identity, bounded archetype identity, ground position, planar velocity, facing, collision radius, behavior/target, current and maximum integer health, disengage/return, and explicit defeat facts.
- Carry at most ten ordered placement-slot states as live entries plus defeat tombstones; retain tombstones in later server snapshots until the corresponding placement slot replenishes so sequenced packet loss cannot erase death evidence.
- Use an independent positive monster snapshot sequence, fixed simulation ticks, finite canonical single-precision metre components, immutable ordered collections, and strict duplicate/ordering validation.
- Add a deterministic versioned big-endian codec with exact bounds, golden fixtures, round-trip coverage, and non-throwing rejection of malformed, non-canonical, truncated, or trailing input.
- Keep transport/channel assignment, authoritative fact production and tombstone retention in SERVER-0007, and presentation consumption in CLIENT-0023.
- Do not add combat actions, transport exchange, asset presentation, AI rules, generic message framing, or changes to existing connected-walking payloads.

## Completion evidence

- Added immutable bounded live-monster and retained defeat-tombstone facts in Starfall.Protocol.Monsters, with an independent positive sequence, exact ten-slot bound, canonical archetype identities, strict ordering/uniqueness, behavior/target consistency and current/maximum integer health.
- Added schema-v1 deterministic big-endian serialization. The exact maximum payload is 1,209 bytes; encoders validate before allocation and decoders reject malformed, non-canonical, truncated or trailing input without throwing.
- Added golden-byte, round-trip, maximum-size, hostile-input, collection-immutability, ordering, health, behavior and tombstone tests while preserving all existing walking fixtures.
- Documented the durable contract at pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/protocol/bounded-monster-snapshots and updated the architecture overview and M2 roadmap.
- Validation: Debug and Release solution builds succeeded with zero warnings; all 324 tests passed in both configurations (37 Architecture, 54 Client, 1 ConnectedWalking, 31 Content, 69 Protocol, 46 Simulation, 86 World). Focused dotnet-format verification passed for all changed C# files. Starfall.Protocol still has no project references; git diff --check and PM doctor passed; the linked family reported zero warnings.
- No World, Simulation, Client, transport-channel, asset, native, visual or project-reference changes were made. SERVER-0007 remains the owner of authoritative fact production, tombstone retention and exchange.