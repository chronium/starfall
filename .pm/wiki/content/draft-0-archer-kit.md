---
title: Draft 0 Archer Kit
createdAt: 2026-08-05T06:16:02.6168200Z
modifiedAt: 2026-08-07T08:35:56.4055180Z
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

Input remains intent. Simulation decides action validity, target or victim set, damage, resource expenditure, death and exact fixed-tick outcome. The completed Basic Arrow baseline resolves without an authoritative spatial projectile; the approved successor gives Basic Arrow and later Fire Arrow a bounded authoritative straight projectile with frozen aim and first-contact collision. Arrow Rain remains a fixed-tick victim-set action and creates no authoritative falling-arrow entities. Client animation, weapon/arrow attachment, reconstructed trajectory, impacts and effects only present protocol facts.

## Authoritative Basic Arrow

`SIM-0004` freezes the first executable combat inputs: a 12-metre inclusive ground-plane centre-to-centre range, a 12-tick / 0.20-second resolve delay, and a 48-tick / 0.80-second start-to-start cadence at 60 Hz. An accepted request stops current movement, faces the selected monster, and consumes the cadence window. A later accepted movement intent before resolution cancels the shot; rejected movement does not.

Resolution currently occurs only at `startTick + 12`. The actor must still exist, remain stationary, and keep the target within range and an inclusive 45-degree facing cone; the monster must still exist with positive health. The completed baseline applies 300 requested integer damage units, clamps effective health reduction at zero, and marks defeat only on the transition to zero. Cancellation and defeat are deterministic facts. Planned `CONTENT-0017` and `SIM-0013` supersede only the post-acceptance outcome path: aim freezes at acceptance, release occurs after 12 ticks without recalculating range or facing, and a straight authoritative projectile applies the same damage to the first contact. Auto-repeat, ammunition consumption, mana and client authority remain excluded.

World resolves same-tick actions in ascending actor identity order. Nonlethal hits replace immutable monster state while preserving entity and spawn facts. First defeat removes the monster exactly once through its existing fixed-slot vacancy seam at the resolve tick; the same slot remains eligible for replenishment 600 ticks later.

## Downstream ownership

- SIM-0004 owns completed authoritative Basic Arrow range, facing, cadence, movement interruption, windup/start tick, resolve tick, integer monster damage and monster death.
- PROTOCOL-0006/0007 own Basic-only connected facts and deterministic serialization. The client request carries command sequence and target only; World derives the acting entity from the admitted session.
- SERVER-0008 exchanges Basic only. SIM-0011 is retained for defeated-player/protected-town action rejection, not player-life transport.
- CLIENT-0012 owns deterministic connected target selection and Basic intent plus the first native connected authority proof. CLIENT-0023 already presents authoritative monster health, hit flash and death.
- CONTENT-0011 selects the existing technical UAL1 mannequin as the complete temporary body, UAL1 `Idle_Loop`/`Walk_Loop`, UAL2 `Bow_Notch`/`Bow_Aim_Neutral`/`Bow_Shoot`, and the Quaternius `Bow_Wooden` and `Arrow` OBJ/MTL sources. No separate underlayer or Ranger armour is selected. Exact paths, evidence and limitations are at pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/basic-arrow-presentation-inputs.
- CLIENT-0007 owns Basic bow-body animation and the later policy for fitting the authored clips to authoritative timing. CLIENT-0011 owns Starfall's provisional semantic hand socket, local bow transform, rendering and native placement. Completed CLIENT-0018 owns the historical synthetic visual arrow and impact; planned CLIENT-0037 replaces its timer with authoritative spawn/terminal-driven presentation.
- CLIENT-0019 owns the ImGui Combat diagnostic and terminal M5 proof: target health, 300 internal / 3 displayed damage, accepted/rejected/cancelled result and monster death. It does not establish floating combat text or a permanent target HUD.
- CONTENT-0016, SIM-0012, PROTOCOL-0014, SERVER-0016 and CLIENT-0032 own independent M6 Mana from inputs through diagnostics. Death/respawn policy is a later lifecycle integration decision.
- SIM-0009 later owns Fire-specific cost, range, cadence, interruption and timing while consuming completed Mana and the Basic action lifecycle. Its protocol/server/client tasks do not own Mana or depend on the permanent HUD.
- SIM-0007 later owns Rain-specific cost, target/radius/cadence/interruption/timing/order while consuming completed Mana and the action contract proven reusable by Fire.
- Equipment, Ranger mapping, permanent HUD, cursor/target-marker feedback and player-life presentation remain separate later deliverables.

Completed fact contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/protocol/connected-basic-arrow

## Explicit gaps

The completed SIM-0004 and connected monster/player-life evidence are sufficient for M5 Connected Basic Arrow. M5 owns monster death only; it neither transports nor presents player defeat/respawn.

M6 Authoritative Mana is independently demonstrable before spells. Fire Arrow and Arrow Rain remain milestone-free and evidence-gated until each deliverable activates. Their later planning may compare combined action, sustain and interruption behavior, but no combined Balance Lab mega-scenario blocks Basic or Mana.

Primary-attribute taxonomy and starting values remain unresolved. They are nonblocking for the current Basic Arrow proof and require later task-owned design before progression rules consume them.

Durable slice context: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/product/first-playable-zone-draft-0

## Authoritative straight-projectile successor

The focused successor chain is `CONTENT-0017 -> SIM-0013 -> PROTOCOL-0016 -> SERVER-0017 -> CLIENT-0037`. Fire Arrow later reuses the same bounded straight-projectile primitive, facts, lifecycle and Client presentation seam. Arrow Rain does not. Full ownership and deterministic ordering are recorded at pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/gameplay/draft-0-straight-projectiles.
