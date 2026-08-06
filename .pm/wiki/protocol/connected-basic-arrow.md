---
title: Connected Basic Arrow Protocol
createdAt: 2026-08-06T08:30:27.7701150Z
modifiedAt: 2026-08-06T10:29:34.2824180Z
---

## Purpose

This contract carries the first connected `basic_arrow` command and its authoritative lifecycle facts. It is transport-neutral. PROTOCOL-0007 owns its deterministic binary representation, SERVER-0008 owns admitted-session binding and exchange, and CLIENT-0012 owns connected target selection and intent.

## Identities and authority

`CombatCommandSequence` is a positive opaque correlation value. `CombatActionId` is a bounded lowercase ASCII identity; this contract fixes the action to `basic_arrow`.

A `BasicArrowCommand` contains only:

- its command sequence;
- the requested target world-entity identity;
- the fixed Basic Arrow action identity implied by the fact type.

The Client never supplies an actor. World retrieves the player entity from the admitted gameplay session. Every authoritative outcome then carries that actor identity, the requested target identity, the action identity and the originating command sequence.

Sequence monotonicity, replay handling, session ownership and delivery policy remain World/exchange responsibilities rather than fact-constructor behavior.

## Lifecycle facts

- `BasicArrowAccepted` carries authoritative start and strictly later resolve ticks.
- `BasicArrowRejected` carries the decision tick and one reachable connected rejection: unavailable actor or target, defeated actor, protected-town lockout, an already-pending action, cadence not ready, coincident target or target out of range.
- `BasicArrowCanceled` carries the accepted start, scheduled resolve, actual cancellation tick and one cancellation reason. Cancellation may occur from the start tick through the scheduled resolve tick.
- `BasicArrowResolved` occurs at its resolve tick and carries exactly 300 requested internal damage units, effective damage from 1 through 300, and whether that application defeated the target.

Tick zero is valid. Command sequences and entity identities are non-zero. Accepted and terminal facts require distinct actor and target identities.

The typed Basic Arrow command makes the simulation's `WrongAction` start result noncanonical at this boundary. The World's global player/monster identity space and monster-target lookup make actor-as-target noncanonical. SERVER-0008 must treat either as an internal mapping defect rather than inventing an exchange fact.

## Proven rule mapping

The later World adapter maps the completed authoritative rule without making Protocol depend on Simulation:

| Simulation evidence | Protocol fact |
| --- | --- |
| accepted pending action | `BasicArrowAccepted` |
| `UnknownActor` | rejected: `ActorUnavailable` |
| `UnknownTarget` | rejected: `TargetUnavailable` |
| defeated actor | rejected or canceled: `ActorDefeated` |
| protected town | rejected: `ActorInProtectedTown` |
| pending/cadence/range/coincident checks | corresponding rejection |
| accepted movement during windup | canceled: `CanceledByMovement` |
| unavailable/moving/out-of-range/outside-facing at resolution | corresponding cancellation |
| authoritative integer damage | `BasicArrowResolved` |

The existing bounded monster snapshot stream remains the authoritative ongoing health and defeat state. Combat outcomes explain the action result; they do not duplicate the full monster snapshot.

## Deterministic binary representation

`ConnectedBasicArrowCodec` uses exact big-endian datagrams under the gameplay protocol version accepted at admission. Every payload starts with one payload-kind byte, action-identity length `11`, and canonical ASCII `basic_arrow`; it does not repeat a packet-local version. The payload kinds are command `1`, accepted `2`, rejected `3`, canceled `4`, and resolved `5`. The fixed action bytes are carried explicitly so another action cannot be decoded as Basic Arrow.

| Payload | Exact length | Body after the 13-byte header |
| --- | ---: | --- |
| command | 29 bytes | command sequence, target entity |
| accepted | 53 bytes | command sequence, actor, target, start tick, resolve tick |
| rejected | 46 bytes | command sequence, actor, target, decision tick, rejection reason |
| canceled | 62 bytes | command sequence, actor, target, start tick, resolve tick, cancellation tick, cancellation reason |
| resolved | 62 bytes | command sequence, actor, target, start tick, resolve tick, requested damage, effective damage, defeated flag |

Sequences, entity identities and ticks are unsigned 64-bit big-endian values. Damage uses signed 32-bit big-endian integers, matching authoritative monster-health representation. Reasons and the defeated flag use one byte. The only canonical defeated values are `0` and `1`.

Every encoder validates the complete source fact before allocating and returns a new exact-length byte array. Every `TryDecode` path rejects unsupported kinds, wrong action bytes, truncation, trailing bytes, zero required identities, actor-as-target, impossible timing, unsupported reasons, noncanonical damage, and invalid flags without throwing. Tick zero remains valid. The public kind inspection validates only the complete fixed header and exact kind-specific length; the corresponding decoder still performs full fact validation.

This is a focused Basic Arrow datagram family governed by `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/protocol/gameplay-protocol-compatibility`. It establishes evidence and malformed-input conventions for later Fire Arrow planning without creating a generic ability protocol, message framework, channel assignment, or transport dispatcher.

## Explicit exclusions

The binary contract does not assign channels, delivery, replay or monotonic-sequence policy, World routing, or admitted-session binding. SERVER-0008 owns those exchange decisions and derives the authoritative actor from the admitted session.

It also does not define client controls, animation, bow or arrow rendering, spatial projectile entities, line-of-sight, Fire Arrow, Arrow Rain, Mana, player health, player defeat/restoration/respawn, chat or persistence.

Arrows, flight and impact remain client-owned presentation of an authoritative fixed-tick result.