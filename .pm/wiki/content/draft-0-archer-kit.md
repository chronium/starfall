---
title: Draft 0 Archer Kit
createdAt: 2026-08-05T06:16:02.6168200Z
modifiedAt: 2026-08-05T12:31:01.7374890Z
---

## Status

CONTENT-0003 freezes the bounded Draft 0 content catalog for the provisional first playable class. These are deterministic prototype inputs, not final balance, final character art, simulation behavior, protocol, cooking or presentation.

## Stable catalog

- Class identity: `dark_elf_archer`
- Initial health: 2,500 authoritative units, corresponding to 25 displayed health at 100 units per displayed point.
- Probability scale: integer basis points from 0 through 10,000.
- Primary attributes remain ordinary integers, but their names and starting values are deliberately unresolved.
- Authoritative action time is represented in fixed integer simulation ticks.

Action order is contractual and must remain visible:

| Order | Identity | Target kind | Damage | Mana |
| ---: | --- | --- | ---: | --- |
| 1 | `basic_arrow` | selected entity | 300 authoritative units / 3 displayed | no |
| 2 | `fire_arrow` | selected entity | 700 authoritative units / 7 displayed | yes |
| 3 | `arrow_rain` | ground circle | 500 authoritative units / 5 displayed per valid victim | yes |

The catalog exposes this ordered action list as an immutable value. Identity validation uses lowercase ASCII letters, digits and underscores, beginning with a letter.

## Ammunition and authority

`Draft0AmmunitionPolicy.Unlimited` means authoritative attacks consume no ammunition resource and require no ammunition inventory or purchasing. It does not prohibit the Client from presenting a nocked, released or travelling arrow.

Input remains intent. Simulation decides action validity, target or victim set, damage, resource expenditure, death and exact fixed-tick outcome. Basic Arrow and Fire Arrow create no authoritative spatial projectile. Arrow Rain creates no authoritative falling-arrow entities. Client animation, weapon/arrow attachment, trajectory, impacts and effects only present protocol facts.

## Authoritative Basic Arrow

`SIM-0004` freezes the first executable combat inputs: a 12-metre inclusive ground-plane centre-to-centre range, a 12-tick / 0.20-second resolve delay, and a 48-tick / 0.80-second start-to-start cadence at 60 Hz. An accepted request stops current movement, faces the selected monster, and consumes the cadence window. A later accepted movement intent before resolution cancels the shot; rejected movement does not.

Resolution occurs only at `startTick + 12`. The actor must still exist, remain stationary, and keep the target within range and an inclusive 45-degree facing cone; the monster must still exist with positive health. The rule applies 300 requested integer damage units, clamps effective health reduction at zero, and marks defeat only on the transition to zero. Cancellation and defeat are deterministic facts. There is no authoritative arrow entity, travel, collision, line-of-sight test, auto-repeat, ammunition consumption, mana, or presentation.

World resolves same-tick actions in ascending actor identity order. Nonlethal hits replace immutable monster state while preserving entity and spawn facts. First defeat removes the monster exactly once through its existing fixed-slot vacancy seam at the resolve tick; the same slot remains eligible for replenishment 600 ticks later.

## Downstream ownership

- SIM-0004 owns Basic Arrow range, facing requirement, cadence, movement interruption, windup/start tick and resolve tick.
- SIM-0009 owns authoritative mana capacity/current state, fixed-tick regeneration, Fire Arrow cost, range, cadence, interruption and timing.
- SIM-0007 owns Arrow Rain cost, cast range, radius, cadence, interruption, resolve timing and deterministic victim ordering.
- EDITOR-0005 supplies and compares exact candidate values in deterministic Balance Lab scenarios without silently promoting them to defaults.
- CLIENT-0018 owns Basic/Fire notch, release, client-only travel, impact timing and reconciliation.
- CLIENT-0010 owns Arrow Rain targeting and presentational falling-arrow/effect timing.
- CLIENT-0011 owns the later equipped bow, nocked arrow, socket, aim and IK presentation.
- SERVER-0008 later exchanges commands and authoritative outcomes after the domain and protocol prerequisites exist.

## Explicit gaps

No current task owns promotion of Balance Lab evidence into one selected connected-M2 combat preset. That focused grooming must exist before SERVER-0008 activates; this catalog does not invent the preset.

Primary-attribute taxonomy and starting values are also unresolved. They are nonblocking for the current three-action kit and require later task-owned design before progression rules consume them.

Durable slice context: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/product/first-playable-zone-draft-0