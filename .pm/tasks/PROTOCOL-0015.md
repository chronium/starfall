---
id: PROTOCOL-0015
title: Negotiate one gameplay protocol version at world admission
track: PROTOCOL
milestone: M5
priority: high
dependsOn:
- CLIENT-0009
- PROTOCOL-0007
- SERVER-0007
createdAt: 2026-08-06T10:20:02.6270950Z
modifiedAt: 2026-08-06T10:36:09.5032100Z
---

Replace packet-local schema bytes with one exact-match gameplay protocol version established during world admission.

Acceptance criteria:
- Define one strongly typed non-zero byte-backed ProtocolVersion and current Starfall gameplay protocol version 1.
- Carry the offered version in WorldJoinRequest and the selected version in WorldJoinAccepted.
- Reject unsupported versions as IncompatibleProtocolVersion before ticket validation, ticket consumption, player creation or gameplay-session creation.
- Record the selected version in the world-owned gameplay session and connected Client session.
- Preserve the exact-match-only contract: no ranges, downgrade, feature negotiation or simultaneous legacy layouts.
- Remove the repeated schema-version byte from connected walking, monster snapshot and Basic Arrow gameplay payloads while preserving exact lengths, kinds, bounds, deterministic encoding and malformed-input rejection.
- Preserve valid early cross-channel snapshot reordering by decoding under the exact version offered by the Client without becoming ready before acceptance.
- Keep sfjt1 join tickets and persisted formats independently versioned. Build/content compatibility remains separate.
- Update durable protocol and roadmap documentation, golden fixtures, focused tests and real loopback admission evidence.
- Do not implement Basic Arrow exchange, new gameplay messages, generic framing, old-layout compatibility readers or per-message version negotiation.

## Notes

- 2026-08-06 10:36 UTC - Implemented the atomic gameplay-protocol compatibility migration.

  Decisions and behavior:
  - Added the non-zero byte-backed ProtocolVersion contract with StarfallGameplayProtocol.CurrentVersion = 1.
  - World admission now compares the offered version before ticket verification or any ticket/session/player mutation, returns IncompatibleProtocolVersion on mismatch, and records the selected version in both World and Client sessions.
  - Request and accepted admission facts carry the offered/selected version. Rejection is the version-neutral 2-byte kind/reason bootstrap fact.
  - Removed packet-local schema bytes from connected walking, bounded monster snapshots, and Basic Arrow. Golden layouts now use 16/65/73-byte walking payloads, an 18-byte/1,208-byte-max monster format, and 29/53/46/62/62-byte Basic Arrow payloads.
  - Preserved early cross-channel movement/monster snapshots without making the Client ready before exact-version acceptance.
  - Retained sfjt1 and persisted-format versioning as independent contracts. No downgrade, version ranges, legacy readers, generic framing, or per-message negotiation were introduced.
  - Wired every still-todo gameplay codec/exchange owner to PROTOCOL-0015 so later packet work must consume the connection-level compatibility contract.

  Validation:
  - dotnet restore Starfall.slnx: up to date.
  - Debug solution build: 0 warnings, 0 errors.
  - Debug solution tests: 380 passed (37 Architecture, 63 Client, 1 real UDP connected-walking loopback, 31 Content, 107 Protocol, 46 Simulation, 95 World).
  - Release solution build: 0 warnings, 0 errors.
  - Release solution tests: the same 380 passed.
  - Real UDP/LiteNetLib loopback asserts the Client selected protocol version 1 and still admits, snapshots, moves, corrects, and cleans up.
  - Incompatible-version tests prove rejection precedes ticket validation/consumption and session/player creation; a valid retry succeeds.
  - Changed-file dotnet format verification passed. Repository-wide formatting still reports only the pre-existing untouched Draft0BasicArrowRules.cs whitespace findings and third-party naming warnings.
  - Debug and Release Starfall.World output/deps inspection found no SDL, ImGui, CharacterPresentation, Client, or Editor contamination.
  - git diff --check passed.
  - pm doctor passed with only the existing legacy_milestone_schema and empty M3 warnings.
  - Linked family reread returned all three projects available/readable/write-trusted with zero warnings.
  - Coordinator PM validation passed with no issues.

  Durable documentation:
  - Added protocol/gameplay-protocol-compatibility.
  - Updated admission, walking, monster, Basic Arrow, world lifecycle, and bootstrap roadmap pages to the connection-level contract and exact current layouts.

  No visual or gameplay-feel checkpoint is required for this wire-contract migration.