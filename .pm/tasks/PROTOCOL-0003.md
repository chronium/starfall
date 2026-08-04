---
id: PROTOCOL-0003
title: Define connected walking commands and player snapshot facts
track: PROTOCOL
milestone: M2
dependsOn:
- SERVER-0006
- SIM-0008
createdAt: 2026-08-02T07:29:11.5694060Z
modifiedAt: 2026-08-04T14:04:12.7978360Z
---

Define the transport-neutral connected-walking contract from the proven world-owned player and authoritative movement behavior.

Acceptance criteria:
- Define a session-bound ground movement command carrying a positive monotonic intent sequence and finite single-precision X/Z metre destination. The command does not carry an entity ID; SERVER-0005 maps the admitted session to its owned world player.
- Define positive world-instance-local entity identity, positive movement snapshot sequencing, fixed unsigned simulation ticks, and complete authoritative player snapshots carrying position, velocity, normalized facing, collision capsule dimensions and an optional last-processed intent sequence.
- Define a correction fact that correlates one processed intent with a complete authoritative snapshot. Do not invent correction-reason categories or reconciliation policy.
- Keep Draft 0 zone bounds out of the durable fact types. Protocol validates finite structural values; World validates destinations against the loaded zone.
- Define snapshot sequence as the freshness order within one gameplay session. Producers use checked monotonic allocation without wrap/reuse; tick zero and no processed intent are valid for the initial snapshot.
- Preserve integer discrete resources and fixed ticks. This task adds no HP, mana, combat, progression, drop or equipment state.
- Keep the existing Client adapter presentational; CLIENT-0009 later maps these real facts into that same path.
- Do not implement serialization, transport framing, World exchange, Client networking, prediction, smoothing, quantization, monsters, combat, chat, persistence or a generic message/entity framework.

## Notes

- 2026-08-04 14:04 UTC - Implemented the transport-neutral connected-walking facts in Starfall.Protocol.Movement: session-bound sequenced destination commands, world-instance-local entity identity, ordered fixed-tick authoritative snapshots, finite X/Z metre state and collision dimensions, optional processed-intent acknowledgement, and correlated correction facts. Protocol does not hard-code Draft 0 bounds or carry entity identity in client commands; later World exchange maps the admitted session to its player and validates the loaded zone. No serialization, exchange, World/Client integration, prediction, smoothing, combat or generic message framework was added. Added the durable connected-walking protocol wiki and updated architecture/lifecycle ownership. Validation: focused Protocol tests 32/32; Debug build 0 warnings/0 errors and full solution 184/184; Release build 0 warnings/0 errors and full solution 184/184; architecture tests 30/30; PM readback and linked family reported no warnings; git diff --check passed. No native or visual validation was required for this transport-neutral contract.