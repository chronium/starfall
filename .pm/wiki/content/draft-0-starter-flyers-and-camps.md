---
title: Draft 0 Starter Flyers and Camp Compositions
createdAt: 2026-08-05T06:49:56.3861690Z
modifiedAt: 2026-08-06T06:48:58.8791910Z
---

## Purpose and ownership

CONTENT-0007 freezes the bounded Draft 0 starter-monster identities and their exact ordered placement into the executable graybox. Starfall Content owns these immutable game-specific inputs.

This catalog does not own authoritative entities, runtime spawning, behavior, presentation, assets, or balance tuning. The identities are provisional technical labels rather than final species names.

## Shared numerical scale

`Draft0GameplayScales` is neutral Starfall Content policy shared by character and monster catalogs:

- one displayed health or mana point equals 100 authoritative resource units;
- a full probability scale is 10,000 basis points.

The starter-monster catalog therefore records:

| Ordered archetype | Authoritative health | Displayed health |
| --- | ---: | ---: |
| `starter_flyer_light` | 700 | 7 |
| `starter_flyer_heavy` | 2,000 | 20 |

“Flyer” and hovering are presentation only. Both archetypes remain ordinary authoritative ground-plane occupants. The catalog grants no altitude, airborne movement, flight navigation, vertical targeting, or special collision semantics.

## Exact camp compositions

`Draft0StarterMonsterCatalog.FirstPlayable` consumes the completed `Draft0GrayboxCatalog.FirstPlayable` values. It copies spawn identities and exact `GroundPoint` values; it does not retain graybox spawn objects.

| Camp | Ordered assignments |
| --- | --- |
| `camp_easy` | `spawn_easy_01 -> starter_flyer_light`; `spawn_easy_02 -> starter_flyer_light`; `spawn_easy_03 -> starter_flyer_light` |
| `camp_mixed` | `spawn_mixed_01 -> starter_flyer_light`; `spawn_mixed_02 -> starter_flyer_light`; `spawn_mixed_03 -> starter_flyer_heavy`; `spawn_mixed_04 -> starter_flyer_heavy` |
| `camp_hard` | `spawn_hard_01 -> starter_flyer_heavy`; `spawn_hard_02 -> starter_flyer_heavy`; `spawn_hard_03 -> starter_flyer_heavy` |

The catalog describes exactly ten initial assignments in stable branch/local order. Array order is authoritative; there is no parallel ordinal field.

The aggregate catalog validates the exact archetype and camp ordering, known archetype references, exact spawn coverage, and exact position equality against the executable graybox. Structural definitions independently validate lowercase ASCII identities, positive health, immutable nonempty collections, non-null entries, and ordinal identity uniqueness.

## Fixed-slot camp policy

`Draft0CampPolicyCatalog.FirstPlayable` binds the approved layouts and compositions into three immutable fixed-slot policies:

| Ordered camp | Geometry | Capacity / initial population | Authoritative seed | Replenishment delay |
| --- | --- | ---: | ---: | ---: |
| `camp_easy` | Broad open circle | 3 | 1 | 600 ticks / 10 seconds |
| `camp_mixed` | Elongated or divided | 4 | 2 | 600 ticks / 10 seconds |
| `camp_hard` | Tight bowl or constrained approach | 3 | 3 | 600 ticks / 10 seconds |

Every approved assignment is both one initially occupied slot and its exact valid ground-plane replenishment placement. Capacity therefore equals initial population and placement-slot count. A slot becomes vacant only when World later reports authoritative removal of its occupant. It becomes eligible again at `removedAtTick + 600` using checked unsigned tick arithmetic; overflow fails explicitly.

When multiple vacancies become eligible together, the reusable Simulation schedule orders them by eligible tick, canonical camp order and canonical slot order. The policy creates no entity, occupancy state or spawn application. Those remain `SIM-0006` responsibilities.

Seeds are explicit provisional Balance Lab inputs but are not consumed in Draft 0 because fixed slots contain no random choice. Adding randomized placement or selection requires separate evidence and an approved deterministic algorithm.

`SIM-0006` now applies this policy inside each World runtime. Entering Running fills all ten slots at tick zero in canonical camp/slot order. World-owned immutable monster state records the slot's camp, spawn, archetype, exact ground point, full catalog health, opaque entity identity and spawn tick. Removing an occupant validates checked eligibility before making the slot vacant; overflow preserves occupancy. After fixed-tick advancement, due replacements use the same slot/archetype/point and full health with a fresh identity.

Players and monsters share one checked monotonic world-local identity sequence. Exact numeric IDs are deliberately not content or simulation contracts. Snapshot ordering and simultaneous replenishment remain deterministic, while actual numeric values remain opaque.

## Bounded authoritative behavior

`SIM-0010` binds these content identities to immutable behavior tunings without adding behavior values to Content:

| Archetype | Ground radius | Speed | Awareness | Attack range | Requested damage | Cadence |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `starter_flyer_light` | 0.45 m | 2.5 m/s | 10 m | 1.25 m | 100 units | 60 ticks |
| `starter_flyer_heavy` | 0.65 m | 1.8 m/s | 12 m | 1.5 m | 200 units | 90 ticks |

At 60 Hz, movement advances by the configured speed divided by 60. A monster acquires the nearest active, unprotected player inside its own camp and awareness radius, breaking equal-distance ties by ascending world entity identity. Awareness gates acquisition only: a retained target remains valid beyond awareness while it stays active, outside the inclusive protected town and inside the camp.

The monster pursues on the ground plane, stops in inclusive attack range and resolves its first attack immediately when a tick begins in range. Later attacks resolve on the exact checked cadence tick. Attack requests are immutable and ordered by monster identity.

`SIM-0011` applies those requests to the player's immutable integer health in the same order. The Draft 0 maximum and restored health are 2,500 units. A first lethal application creates one defeat transition, removes movement eligibility and starts a checked 180-tick respawn schedule; later requests that tick remain visible as requests but cannot mutate a defeated target.

Leaving the camp, entering `town_safe`, becoming defeated or disappearing starts deterministic return. Returning monsters cannot reacquire until they reach their exact home point and become idle on a completed tick. Camp footprints and homes must not intersect the protected town, and movement fails before entering it. Monster centres remain inside their radius-inset camp footprint and use the same World-owned static boundary/proxy collision environment as players.

Defeated players retain entity/session identity, receive connected movement corrections during lockout and respawn at the exact town anchor tick with restored health, zero velocity and `+Z` facing. The full-health three-second values remain configurable EDITOR-0005 inputs. Mana remains SIM-0009 scope. There is no pathfinding, dynamic-body avoidance, sliding, altitude, airborne authority or general sanctuary framework.

## Downstream ownership

- Completed `SIM-0003` owns the immutable camp-policy inputs and pure replenishment schedule.
- Completed `SIM-0006` owns authoritative World runtime occupancy, immutable monster records, shared identity allocation, validated removal handling and fixed-tick spawn application.
- Completed `SIM-0004` owns Basic Arrow integer damage/death and applies first monster defeat through the existing validated removal seam. M5 Connected Basic Arrow consumes that death outcome, but monster replenishment remains the separately completed camp-lifecycle responsibility of SIM-0003/SIM-0006 rather than part of the attack deliverable.
- Completed `SIM-0010` owns evidence-backed body/collision radius, movement speed, deterministic target selection and tie-breaking, awareness, pursuit/leash, attack range/damage/cadence, disengagement and return behavior.
- Completed `SIM-0011` owns ordered player damage application, the inclusive protected-town boundary, deterministic disengagement, one defeat transition, movement/action lockout and exact 180-tick respawn of the same player/session identity.
- `CLIENT-0022` owns the generated local placeholder proof at these exact ten ordered spawn assignments.
- `CLIENT-0023` replaces connected local fixtures with bounded world snapshots and presents behavior, target, health, disengage/return and retained death facts through the existing placeholder adapter.
- `CONTENT-0013` owns exact selected monster presentation inputs. It remains milestone-free and `priority: none` until that visual deliverable activates.
- Client presentation may hover, bob, lunge, pulse, flash on hit or present death without changing authoritative ground-plane state; only the gentle hover belongs to CLIENT-0022.

Running and Draining use the same deterministic camp and player-lifecycle rules; draining blocks new admission/technical creation but continues retained gameplay until its separately owned deadline policy. Stopping clears entities, occupancy, pending replenishments and pending respawns without reusing identities.

The starter-monster composition catalog itself still contains no runtime capacity state, spawn templates, replenishment schedule, authoritative entity identity, asset choice or presentation contract. The separate camp-policy catalog adds only immutable inputs; World owns application and outcomes.

`SIM-0004` applies `basic_arrow` to immutable monster health values. Nonlethal hits replace only current health while preserving monster identity and all placement facts. The 700-unit light archetype is defeated by its third 300-unit request; the 2,000-unit heavy archetype by its seventh. Effective overkill is clamped to remaining health, defeat occurs once, and first defeat enters the existing vacancy schedule at the action's exact resolve tick. This does not add presentation, drops, rewards or random replacement.

Durable identity: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/draft-0-starter-flyers-and-camps.