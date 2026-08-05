---
title: Bounded Monster Snapshots
createdAt: 2026-08-05T17:41:38.0022610Z
modifiedAt: 2026-08-05T17:41:38.0022610Z
---

## Purpose

PROTOCOL-0005 defines Starfall's first bounded full-state monster snapshot facts and deterministic binary serialization. The contract carries already-proven authoritative monster state from World to later Client presentation without giving Protocol simulation rules or transport ownership.

The implementation lives in `Starfall.Protocol.Monsters`. Protocol remains product-dependency-free and transport-neutral.

## Fact boundary

A `BoundedMonsterSnapshot` carries:

- a positive monster-specific snapshot sequence;
- the authoritative fixed simulation tick, including valid tick zero;
- immutable live-monster entries;
- immutable defeated-monster tombstones.

A batch contains at most ten total entries, matching the Draft 0 placement-slot bound. Live entries and tombstones are each strictly ordered by ascending world entity identity. Identities are unique across the complete batch.

A live entry includes:

- stable positive world entity identity;
- a 1–64 byte lowercase ASCII archetype identity;
- finite ground position and planar velocity in single-precision metres;
- normalized facing and positive collision radius;
- `Idle`, `Pursuing`, `Attacking` or `Returning` behavior;
- an explicit target only for pursuing or attacking;
- positive current and maximum integer health.

`Returning` is the explicit disengage/return fact. Camp identity, spawn identity, home point, attack schedule, damage decisions and authoring objects remain private to authoritative World/Simulation.

## Loss-tolerant defeat facts

Authoritative defeat removes a live monster immediately. A sequenced snapshot packet may be dropped, so disappearance alone cannot reliably communicate death or distinguish it from another removal.

A defeated tombstone therefore carries the former entity and archetype identities, last authoritative position/facing and authoritative defeat tick. SERVER-0007 must repeat that tombstone in later full snapshots until the corresponding placement slot replenishes. The replacement receives a new entity identity and supersedes the tombstone.

This is bounded retained state, not a reliable event stream or combat-result protocol. A client joining while the slot is vacant receives the current tombstone. A client joining after replenishment needs no historical death replay.

## Binary contract

`BoundedMonsterSnapshotCodec` schema version 1 uses a standalone packet payload:

1. version byte;
2. unsigned 64-bit snapshot sequence;
3. unsigned 64-bit simulation tick;
4. one-byte live and defeated counts;
5. ordered live entries;
6. ordered defeated entries.

Integers and IEEE-754 single bit patterns are big-endian. Archetype identities use one length byte followed by canonical ASCII bytes. Optional targets use a canonical flag plus unsigned 64-bit value: absent requires zero; present requires a positive identity. Health uses positive signed 32-bit integers.

The maximum payload is 1,209 bytes: a 19-byte header plus ten maximum-sized live entries. Encoding validates the complete fact and throws `ArgumentException` before returning output. Decoding accepts only exact complete payloads, rejects trailing bytes and malformed/non-canonical values, and never throws for arbitrary input.

Finite floats are required and negative zero is non-canonical. Facing uses the same `1e-4` normalization tolerance as connected walking.

The entry digest or generic framing concepts do not exist here. Existing admission and connected-walking payload bytes remain unchanged.

## Ownership and next consumers

- PROTOCOL-0005 owns facts, validation and deterministic serialization only.
- SERVER-0007 will map World state into these facts, allocate sequences, retain tombstones and choose the transport channel/delivery contract.
- CLIENT-0023 will consume the snapshots through the existing placeholder-monster presentation adapter.
- Later combat protocol tasks own action/result facts such as damage source, attack timing and effects.

No source asset, renderer, monster AI, combat command, network exchange, persistence or generic entity/message framework is introduced by this contract.