---
title: Connected Walking Facts
createdAt: 2026-08-04T14:02:01.5363760Z
modifiedAt: 2026-08-04T14:25:25.0627130Z
---

## Purpose

This page records the transport-neutral connected-walking facts established by `PROTOCOL-0003`. They derive from the generic world-owned player proven by `SERVER-0006` and the authoritative 60 Hz movement proven by `SIM-0008`.

Protocol owns immutable facts and structural validation only. Simulation remains authoritative for movement outcomes, World binds admitted sessions to players and validates the active zone, and Client presents authoritative snapshots.

## Command boundary

`GroundMovementCommand` carries:

- a positive `MovementIntentSequence`;
- one finite `GroundPosition` containing single-precision X/Z metres.

The command deliberately carries no entity identity. `SERVER-0005` must resolve the admitted gameplay session to its world-owned player before submitting intent to Simulation. A client cannot nominate another entity to control.

Intent sequences begin at 1 and are allocated monotonically with checked arithmetic within one gameplay session. They do not wrap, reset or re-enter the available range during that session. Exact duplicate, stale and gap handling belongs to the later server-exchange contract.

Protocol accepts any finite X/Z metre components. It does not hard-code the Draft 0 200 x 200 metre envelope. World validates the destination against the currently loaded zone and walkable/collision policy.

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

Snapshot sequence is the freshness order for connected-walking facts within one gameplay session. Routine snapshots and corrections share that sequence stream. Simulation ticks may repeat or skip between emitted facts, so consumers do not use tick or arrival order as a substitute for snapshot sequence. Producer allocation and consumer stale-fact handling belong to `SERVER-0005` and `CLIENT-0009`.

## Correction fact

`PlayerMovementCorrection` correlates one processed intent sequence with one complete authoritative snapshot. The embedded snapshot must acknowledge the same intent sequence.

The fact does not define why correction was required. Rejection, collision, exchange and later reconciliation policies remain with their task-owned domains. Client presentation never changes the authoritative result.

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

- `PROTOCOL-0004` owns deterministic serialization, bounded packet representation and malformed-input rejection for these exact facts.
- `SERVER-0005` owns session-to-player mapping, active-zone validation, checked sequence allocation, intent submission and snapshot/correction exchange.
- `CLIENT-0009` owns consuming the latest facts and translating them into the presentation adapter proven by `CLIENT-0021`.
- Monster, combat, progression, drop and inventory/equipment facts remain in their focused later tasks.

This contract does not add wire framing, sockets, transport choice, quantization, prediction, smoothing, a generic entity/message framework or gameplay authority to Client.

Architecture overview: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/overview

World lifecycle: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/world-channel-lifecycle