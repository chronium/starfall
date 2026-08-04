---
title: Connected Walking Facts
createdAt: 2026-08-04T14:02:01.5363760Z
modifiedAt: 2026-08-04T17:18:04.4887950Z
---

## Purpose

This page records the transport-neutral connected-walking facts established by `PROTOCOL-0003`. They derive from the generic world-owned player proven by `SERVER-0006` and the authoritative 60 Hz movement proven by `SIM-0008`.

Protocol owns immutable facts and structural validation only. Simulation remains authoritative for movement outcomes, World binds admitted sessions to players and validates the active zone, and Client presents authoritative snapshots.

## Command boundary

`GroundMovementCommand` carries:

- a positive `MovementIntentSequence`;
- one finite `GroundPosition` containing single-precision X/Z metres.

The command deliberately carries no entity identity. `SERVER-0005` resolves the admitted gameplay session supplied by host context to its immutable world-owned player before submitting intent to Simulation. A client cannot nominate another entity to control.

Intent sequences begin at 1 and are client-allocated monotonically with checked arithmetic within one gameplay session. The World exchange accepts any sequence newer than the last processed value, including gaps that may result from transport loss. Exact duplicates and stale values are ignored without changing movement, acknowledgements or snapshot sequencing. A malformed payload or unknown session returns a bounded host disposition and produces no gameplay fact.

Protocol accepts any finite X/Z metre components. It does not hard-code the Draft 0 200 x 200 metre envelope. World validates the destination through the currently loaded authoritative Simulation layout and collision policy.

## Authoritative snapshot

`PlayerMovementSnapshot` carries:

- a positive `MovementSnapshotSequence`;
- an unsigned fixed `SimulationTick`, where tick 0 is valid;
- a positive `WorldEntityId`;
- finite authoritative position and planar velocity in metres;
- finite normalized planar facing;
- finite positive collision capsule radius and height;
- an optional last-processed movement-intent sequence.

`WorldEntityId` is local to one world instance. It is not an account, character, session or persistent identity and must not be retained across world-instance replacement.

Routine snapshots and corrections share one checked monotonic snapshot sequence per gameplay session, beginning at 1 and failing explicitly on exhaustion. Initial state may be captured at the session's admission tick, including tick zero. Later routine capture emits at most one latest snapshot for each session at each newer fixed tick; skipped captures do not create a queued historical backlog. Multi-session routine output is ordered by bound world entity identity, not dictionary enumeration.

Simulation ticks may repeat across synchronous corrections or skip between emitted facts, so consumers use snapshot sequence—not tick or arrival order—as the freshness order. `CLIENT-0009` owns stale-fact handling after the transport boundary is approved.

## Correction fact

`PlayerMovementCorrection` correlates one processed intent sequence with one complete authoritative snapshot. The embedded snapshot must acknowledge the same intent sequence.

`SERVER-0005` emits exactly one immediate correction when a newer valid command is rejected by authoritative walkable-bound or proxy validation. Rejection still counts as processing that intent. Accepted commands are instead acknowledged by the next routine snapshot. Corrections report current authoritative state and never let presentation change the result.

The fact does not encode a rejection reason. Collision, exchange and later reconciliation policies remain with their task-owned domains.

## Deterministic serialization

`PROTOCOL-0004` freezes three independent schema-version-1 payloads. These are fact codecs, not transport frames:

| Fact | Exact bytes | Layout |
| --- | ---: | --- |
| `GroundMovementCommand` | 17 | version 1; intent sequence 8; destination X 4; destination Z 4 |
| `PlayerMovementSnapshot` | 66 | version 1; snapshot sequence 8; tick 8; entity 8; position X/Z 8; velocity X/Z 8; facing X/Z 8; capsule radius/height 8; acknowledgement flag 1; acknowledgement sequence 8 |
| `PlayerMovementCorrection` | 74 | version 1; corrected intent sequence 8; the 65-byte snapshot body without a nested version |

Every integer field is an unsigned 64-bit big-endian value except the one-byte schema version and acknowledgement flag. Floats are their IEEE 754 single-precision bit patterns in big-endian byte order. Encoders return a newly allocated array of the exact public length. Decoders require that exact length and reject trailing bytes; framing, message kinds and length prefixes belong to the later World-host exchange.

Command intent, snapshot, entity, corrected-intent and every present acknowledgement sequence must be non-zero. Tick zero is valid. A missing acknowledgement has exactly one encoding: flag 0 and sequence 0. A present acknowledgement uses flag 1 and a non-zero sequence. Corrections require a present acknowledgement equal to the corrected sequence.

Only finite canonical float encodings are accepted. Positive zero is canonical; negative-zero encodings are rejected to preserve one deterministic byte representation. Facing normalization and capsule dimensions reuse the fact contract's validation and tolerance. Protocol does not reject finite positions for being outside a particular zone.

`EncodeCommand`, `EncodeSnapshot` and `EncodeCorrection` validate their complete source facts before allocating their result and throw `ArgumentException` for null or malformed facts. Their `TryDecode...` counterparts are non-throwing for untrusted payloads and expose no public error taxonomy.

## Numerical and dependency contract

Spatial facts use finite BCL `System.Numerics` single-precision metre values. Fixed simulation ticks, entity identities and sequences remain integers. Future HP, mana, damage, XP, levels, currency, item counts and discrete stats remain integer-valued; none are added to the walking snapshot.

`Starfall.Protocol` remains product-dependency-free. It does not reference Content, Simulation, World, Client, Box3D, SDL or coordinator source. The similarly named Simulation and Protocol identity/spatial types are explicit ownership-boundary values; World performs the later one-to-one mapping.

## Downstream ownership

- `PROTOCOL-0004` owns deterministic fact serialization and malformed-input rejection.
- `SERVER-0005` owns session-to-player resolution, authoritative destination validation, snapshot sequencing and corrections.
- `CLIENT-0009` owns the first transport exchange and stale-fact consumer.

The fixed channel/delivery assignment is:

| Channel | Fact | Delivery |
| --- | --- | --- |
| 0 | admission request, accept or reject | reliable ordered |
| 1 | movement command | reliable sequenced |
| 2 | routine latest snapshot | sequenced |
| 3 | authoritative correction | reliable ordered |

These are transport datagrams, not nested generic frames. The connected Client allocates positive command sequences monotonically, retains only newer global snapshot sequences, permits nondecreasing simulation ticks, requires stable entity identity, and rejects acknowledgements beyond the last command it sent. Corrections replace the latest authoritative snapshot. No interpolation, prediction, reconciliation math or local movement authority is introduced.

The World polls transport each outer fixed-step cycle and publishes only the latest post-catch-up snapshot. Admission also publishes the current tick immediately, including tick zero. This keeps snapshot evidence explicit rather than fabricating an historical backlog.