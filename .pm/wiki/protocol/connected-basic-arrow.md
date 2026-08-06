---
title: Connected Basic Arrow Protocol
createdAt: 2026-08-06T08:30:27.7701150Z
modifiedAt: 2026-08-06T08:30:27.7701150Z
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

## Explicit exclusions

This contract does not define encoding, channels, delivery, World routing, client controls, animation, bow or arrow rendering, spatial projectile entities, line-of-sight, Fire Arrow, Arrow Rain, Mana, player health, player defeat/restoration/respawn, chat or persistence.

Arrows, flight and impact remain client-owned presentation of an authoritative fixed-tick result.