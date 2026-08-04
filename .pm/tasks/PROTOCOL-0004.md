---
id: PROTOCOL-0004
title: Serialize connected walking commands and player snapshots
track: PROTOCOL
milestone: M2
dependsOn:
- PROTOCOL-0003
createdAt: 2026-08-02T07:31:45.1162170Z
modifiedAt: 2026-08-04T14:22:05.0505620Z
---

Implement deterministic fixed-layout serialization for the bounded connected-walking facts established by PROTOCOL-0003.

Acceptance criteria:
- Provide separate command, snapshot and correction codecs with schema byte 1, exact public payload lengths and unsigned 64-bit big-endian integer fields.
- Preserve finite canonical single-precision metre values and reuse the PROTOCOL-0003 facing and collision-capsule fact validation without adding zone or gameplay policy.
- Require non-zero command intent, snapshot, entity, corrected-intent and every present acknowledgement sequence; allow simulation tick zero; encode an absent acknowledgement only as flag 0 plus sequence zero.
- Require every correction snapshot to acknowledge exactly the corrected intent sequence.
- Reject unsupported versions, invalid flags, non-canonical negative zero, non-finite or structurally invalid values, every truncated payload and every payload with trailing bytes through non-throwing TryDecode APIs.
- Validate the complete source fact before encoding; malformed inputs throw ArgumentException, and successful encoders return newly allocated exact-length payloads without exposing partial output.
- Test golden bytes, deterministic repeats, round trips, exact length rejection, malformed facts and arbitrary malformed payload handling.
- Keep active-zone validation in World and keep transport framing, sockets, exchange, quantization, prediction, smoothing, monster/combat/progression/drop/inventory facts and generic messaging outside this task.

## Completion evidence

Implemented `ConnectedWalkingCodec` as three schema-version-1 fixed-layout codecs: 17-byte command, 66-byte snapshot and 74-byte correction. Integer fields are unsigned 64-bit big-endian values; float fields preserve canonical finite IEEE 754 single-precision metre bits. Absent acknowledgements encode only as flag 0 plus sequence 0, present acknowledgements require a non-zero sequence, and corrections require an acknowledgement equal to their corrected intent.

The decoder accepts tick zero, rejects every truncated or extended payload, unsupported versions, invalid flags, zero required identities/sequences, negative zero, non-finite values, malformed facing/capsules and inconsistent corrections without throwing. Encoders validate complete source facts before returning newly allocated exact-length arrays. No framing, transport, exchange, active-zone validation, gameplay rule or product dependency was added.

Validation on 2026-08-04:
- `dotnet restore Starfall.slnx`: all projects up to date.
- Focused `dotnet test tests/Starfall.Protocol.Tests/Starfall.Protocol.Tests.csproj -m:1 --no-restore`: 45 passed.
- Debug `dotnet build Starfall.slnx -m:1 --no-restore`: succeeded with 0 warnings and 0 errors.
- Debug `dotnet test Starfall.slnx -m:1 --no-restore --no-build`: 197 passed.
- Release `dotnet build Starfall.slnx -c Release -m:1 --no-restore`: succeeded with 0 warnings and 0 errors.
- Release `dotnet test Starfall.slnx -c Release -m:1 --no-restore --no-build`: 197 passed.
- Protocol dependency boundary remains enforced by the passing 30 architecture tests.
- No native or visual validation was required for this Protocol-only task.