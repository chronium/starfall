---
title: Draft 0 Authoritative Straight Projectiles
createdAt: 2026-08-07T08:32:43.1791170Z
modifiedAt: 2026-08-07T08:32:43.1791170Z
---

## Status

This is the owner-approved successor to the completed connected Basic Arrow baseline. It is a planned contract, not current runtime behavior.

Completed `CLIENT-0018` remains historical evidence for exact arrow acquisition, right-hand nocking, Bow_Shoot frame-3 detachment, static rendering, deterministic 150 ms client-only travel and impact feedback. `CLIENT-0037` will remove only that synthetic flight after the authoritative Content, Simulation, Protocol and World prerequisites complete.

## Ownership and task chain

```text
CONTENT-0017
  -> SIM-0013
    -> PROTOCOL-0016
      -> SERVER-0017
        -> CLIENT-0037
          -> CLIENT-0019
```

- Content owns stable identities and tunable damage, release delay, speed, radius, maximum travel, selection range, facing and cadence inputs.
- Simulation owns accepted-action authority, frozen aiming, release/cancellation semantics, projectile state, first-contact collision, deterministic ordering, integer damage and post-release independence.
- Protocol owns shared facts, codecs and validation.
- World owns checked projectile-entity allocation, fixed-step lifecycle, collision-query integration and connected exchange.
- Client owns visual reconstruction from authoritative spawn and terminal facts. It never decides collision, damage, health or death.

The implementation is a narrowly parameterized Draft 0 straight-projectile primitive reusable by Basic Arrow and Fire Arrow. It is not a generalized projectile, ability or effect framework. Arrow Rain remains a fixed-tick ordered victim-set action with presentation-only falling arrows.

## Frozen Basic Arrow inputs

- action identity: `basic_arrow`;
- requested damage: 300 internal units / 3 displayed points;
- release delay: 18 fixed ticks;
- speed: 60 metres/second;
- projectile radius: 0.05 metre;
- maximum authoritative travel: 12 metres;
- selection range: inclusive 12 metres;
- facing cone: inclusive 45 degrees;
- cadence: 48 ticks;
- ammunition: unlimited and not represented as inventory.

Spatial inputs are finite single-precision ground-plane metres at the simulation boundary. Discrete resources remain integers and time remains fixed ticks.

## Acceptance, frozen aim and release

At acceptance the authoritative rule:

1. validates the selected live target, inclusive range, facing and stationary actor;
2. records the target identity and its current ground-plane position;
3. records the actor's current ground-plane release origin;
4. freezes the normalized direction from that origin to the captured target position;
5. schedules release at start tick plus 18.

Aim never updates during windup. The target may move away from the frozen trajectory.

Monster movement occurs before release validation. Release validation checks only that the actor remains alive and available at the frozen origin and that the original target still exists and is alive. It never recalculates aim, range or facing. Actor movement or invalidation and original-target death/disappearance cancel before release. Target movement alone does not.

A released projectile is an independent World entity. It continues after shooter movement, defeat or disconnect and after original-target movement or death. Another monster may intercept it.

## Fixed-step collision and ordering

Newly released projectiles begin advancing on the following simulation tick. At 60 Hz and 60 metres/second, each full tick advances at most one metre.

- Move monsters before release validation and projectile advancement.
- Advance projectiles in ascending projectile identity.
- Use continuous swept ground-plane collision against the currently live monster circles expanded by projectile radius and the approved static graybox boundaries/proxy footprints.
- Resolve the earliest contact.
- Equal monster contacts use ascending monster entity identity.
- Static collision wins an exact static/monster tie.
- Contact exactly at 12 metres wins over `TravelExhausted`.
- Apply 300 damage only to the first contacted live monster.
- Static contact and travel exhaustion apply no damage.
- Each later projectile observes damage/death produced by earlier projectiles in the same tick.
- Resolve projectile damage before monster attacks are applied.
- Discard an already-generated monster attack if its attacker was killed by a projectile.

There is no homing, gravity, altitude, player/PvP collision, dynamic Box3D projectile body, ammunition resource or authoritative visual mesh.

## Protocol and presentation

The negotiated gameplay protocol remains version 1. Basic Arrow layouts are replaced in place because there are no independently deployed consumers, compatibility populations, replays or external tools requiring the development-only layout. No legacy reader or dual path is retained.

The replacement lifecycle contains:

- actor-free command;
- accepted fact with actor, original target, start tick and release tick;
- pre-release canceled fact;
- projectile-spawn fact with correlation, positive projectile identity, actor, original target, release tick, finite origin, normalized direction and required trajectory inputs;
- terminal fact with projectile identity, terminal tick/position and exactly one of `Hit`, `Blocked` or `TravelExhausted`.

Only `Hit` carries contacted monster, requested/effective damage and defeat evidence. Those fields support presentation and diagnostics; the Client must not use them to mutate canonical health or death. Monster snapshots and tombstones remain authoritative state.

Spawn and terminal facts use the existing reliable ordered combat-outcome path. No projectile snapshot stream is introduced. Client presentation reconstructs the straight trajectory and tolerates independent ordering of monster snapshots/tombstones.

## Fire Arrow continuity and exclusions

`SIM-0009`, `PROTOCOL-0011`, `SERVER-0013` and `CLIENT-0027` reuse this straight-projectile contract. Fire owns its own Mana cost, 700-unit damage, cadence/timing and effects; it does not create a parallel projectile runtime or presenter.

This work does not implement Fire Arrow, Arrow Rain, equipment, ammunition, inventory, permanent HUD, floating damage text, persistence, flight navigation or a generic projectile framework.