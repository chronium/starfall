---
id: PROTOCOL-0007
title: Serialize first connected combat facts
track: PROTOCOL
milestone: M5
priority: high
dependsOn:
- PROTOCOL-0004
- PROTOCOL-0006
createdAt: 2026-08-03T07:29:09.1256850Z
modifiedAt: 2026-08-06T06:42:59.4416160Z
---

Implement deterministic bounded serialization for the connected Basic Arrow fact contract.

Acceptance criteria:
- Encode Basic Arrow intent, authoritative actor and target facts, timing, acceptance, rejection, cancellation, integer damage and monster defeat.
- Reject malformed, ambiguous, unsupported, non-canonical or out-of-bound values deterministically.
- Preserve non-zero command and entity identities, fixed ticks, and admitted-session actor binding without embedding simulation rules.
- Do not encode player-life or respawn payloads, Fire Arrow, Arrow Rain, ground-target points or mana.
- Do not implement server routing, presentation, projectile entities or a generic protocol framework.

## Notes

- 2026-08-06 - Implemented deterministic schema-v1 serialization for the completed connected Basic Arrow facts.
  - Added exact big-endian command, accepted, rejected, canceled and resolved payloads with public lengths of 30, 54, 47, 63 and 63 bytes.
  - Every payload carries the fixed canonical ASCII `basic_arrow` identity and a bounded payload kind. Client commands remain actor-free; authoritative outcomes carry distinct non-zero actor and target identities.
  - Encoders validate complete facts before returning new exact-length arrays. Non-throwing decoders reject unsupported headers, wrong action bytes, truncation, trailing bytes, invalid identities, timing, reasons, damage and defeated flags. Tick zero remains valid.
  - Added golden-byte, deterministic-repeat, round-trip, exact-length, all-reason, boundary, malformed-source and arbitrary-hostile-input tests. No transport channel, routing, simulation, Client, projectile, player-life, Mana, Fire Arrow, Arrow Rain, dependency or project-reference change was introduced.
  - Updated pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/protocol/connected-basic-arrow with the frozen grammar and downstream ownership.
  - Validation: restore succeeded; focused Protocol tests passed 106/106; Debug and Release solution builds succeeded with 0 warnings and 0 errors; all 375 tests passed in both configurations (37 Architecture, 62 Client, 1 ConnectedWalking, 31 Content, 106 Protocol, 46 Simulation, 92 World); focused formatting verification and `git diff --check` passed; Starfall.Protocol still has no project references; PM doctor passed and linked-family inspection reported zero warnings. No native or visual validation was required.
