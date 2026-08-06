---
id: PROTOCOL-0013
title: Define the development command envelope
track: PROTOCOL
milestone: M4
dependsOn:
- PROTOCOL-0004
- PROTOCOL-0015
createdAt: 2026-08-06T06:41:22.5139150Z
modifiedAt: 2026-08-06T15:32:22.6209780Z
---

Define one development-only command/result envelope with non-zero sequences, bounded payloads, deterministic encoding and decoding, enablement and rejection facts, and an explicit no-compatibility promise. This task defines no gameplay feature commands.

## Notes

- 2026-08-06 15:32 UTC - Implemented the bounded development-only command envelope.

  Contract:
  - Added non-zero ulong command sequences, 1-64 byte lowercase ASCII command identities, immutable ordered zero-to-eight argument tokens, explicit enabled/disabled availability, correlated success/rejection facts, and the five approved rejection reasons.
  - Added deterministic big-endian codecs on channels 7 and 8 with 594/588/589-byte maximum bounds, strict exact-consumption decoding, printable ASCII diagnostics, non-throwing malformed-input rejection, and no packet-local schema version.
  - Kept requests actor/session-free; admitted-session authority, monotonic sequence enforcement, the development gate, dispatcher, Ping World and all feature commands remain SERVER-0015 or later task ownership.
  - Gameplay protocol version remains 1. The additive development path has no compatibility promise.

  Durable documentation:
  - Added protocol/development-commands.
  - Updated protocol/gameplay-protocol-compatibility and roadmap/development-instrumentation.

  Validation:
  - Starfall pm doctor passed before implementation.
  - Focused Protocol build passed with 0 warnings/errors; focused Protocol tests passed 130/130.
  - Debug restore/build passed with 0 warnings/errors; full Debug tests passed 438/438.
  - Release build passed with 0 warnings/errors; full Release tests passed 438/438.
  - Architecture tests passed 40/40 in both configurations.
  - Changed-file dotnet format verification passed.
  - git diff --check passed.
  - No native or visual validation was required for this Protocol-only task.