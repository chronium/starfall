---
title: Bounded Monster Snapshots
createdAt: 2026-08-05T17:41:38.0022610Z
modifiedAt: 2026-08-05T18:27:19.0603880Z
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

A defeated tombstone therefore carries the former entity and archetype identities, last authoritative position/facing and authoritative defeat tick. SERVER-0007 repeats that tombstone in later full snapshots until the corresponding placement slot replenishes. The replacement receives a new entity identity and supersedes the tombstone.

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

## Implemented exchange

SERVER-0007 maps immutable World-owned state into this contract and publishes it from the existing gameplay network host. Channel 4 carries standalone monster snapshots with `Sequenced` delivery; admission remains channel 0, movement commands channel 1, movement snapshots channel 2 and movement corrections channel 3. No generic framing or multiplex envelope was introduced.

Every admitted gameplay session owns an independent checked monster-snapshot sequence beginning at one. It receives one latest full snapshot at most once per observed fixed tick, including an initial tick-zero snapshot. Repeated host polls without a new tick send nothing, while a catch-up cycle may skip intermediate ticks and publish only the latest completed state. At the 1,209-byte maximum and nominal 60 Hz this Draft 0 bound is approximately 70.8 KiB/s per client before transport overhead. That is accepted evidence for ten prototype slots, not a scalable cadence or interest-management contract.

World retains one immutable defeated state keyed by the authoritative placement slot only after lethal damage. The tombstone repeats in every later snapshot while the slot remains vacant, including to a session admitted during that vacancy. Exact-slot replenishment removes the tombstone and publishes the fresh entity identity. Technical removal and lifecycle shutdown do not fabricate defeat facts.

Admission acceptance and snapshot channels may reorder. Until CLIENT-0023 owns retained monster consumption, the completed connected-walking client validates and ignores well-formed channel-4 packets before or after acceptance. Malformed or misrouted monster data remains a connection failure. CLIENT-0023 must replace that compatibility seam with latest-snapshot retention and presentation without creating a second adapter.

## Ownership and next consumers

- PROTOCOL-0005 owns facts, validation and deterministic serialization only.
- SERVER-0007 maps World state into these facts, allocates per-session sequences, retains tombstones and owns channel-4 sequenced delivery.
- CLIENT-0023 will replace the temporary validate-and-ignore compatibility path with retained consumption through the existing placeholder-monster presentation adapter.
- Later combat protocol tasks own action/result facts such as damage source, attack timing and effects.

SERVER-0007 introduces only the focused exchange described above. No source asset, renderer, monster AI, combat command, persistence, interest management or generic entity/message framework is introduced.