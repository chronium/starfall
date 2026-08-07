---
title: Authoritative Mana
createdAt: 2026-08-06T06:46:50.1027990Z
modifiedAt: 2026-08-07T19:32:02.1428060Z
---

## Deliverable

Configured integer Mana initializes, consumes, clamps, regenerates, restores, serializes, exchanges and is proven through authoritative development commands and a Resource diagnostic before Fire Arrow or Arrow Rain owns it.

This is milestone M6. Mana is independent of spells and the permanent player HUD.

## Activation and internal dependency path

M6 becomes eligible from three capability triggers:

~~~text
delivered M4
  -> development_instrumentation_available

completed PROTOCOL-0015
  -> gameplay_protocol_v1_available

completed SERVER-0005 + completed CLIENT-0009
  -> connected_world_available

all three active triggers
  -> M6 Authoritative Mana active
~~~

Those delivered capabilities guarantee the debug shell/console/result path, connection-level gameplay-protocol-v1 negotiation, and admitted connected-World exchange. M6 tasks name and consume those seams without repeating cross-milestone dependency edges.

Internal implementation order remains explicit:

~~~text
CONTENT-0003
  -> CONTENT-0016  provisional Mana inputs
  -> SIM-0012      authoritative state, consumption and regeneration
  -> PROTOCOL-0014 deterministic Mana facts and serialization
  -> SERVER-0016  session Mana exchange and feature-owned debug handlers
  -> CLIENT-0032  Resource diagnostics and native proof
~~~

`PROTOCOL-0014` consumes the delivered protocol-v1 negotiation contract. `SERVER-0016` consumes the delivered admitted World/session exchange and common development-command dispatcher. `CLIENT-0032` consumes the delivered debug shell, console, and correlated-result path while retaining Mana-owned diagnostics.

Fire Arrow later consumes the delivered Mana capability; its future activation must not recreate per-layer Mana dependency fans. Arrow Rain consumes the established Mana and combat-action capabilities. Neither owns Mana capacity, regeneration, or lifecycle behavior.

## Numerical and lifecycle boundary

Health and Mana use 100 authoritative internal units per displayed point. Mana values and regeneration use checked integer arithmetic and fixed simulation ticks. CONTENT-0016 freezes provisional maximum, initial, regeneration rate, delay and ordering as Balance Lab inputs.

SIM-0012 exposes a clean lifecycle seam but does not decide what death or respawn does to Mana. Completed same-entity player respawn remains historical evidence; a later Player Life integration task will freeze the cross-resource policy.

## Development proof

SERVER-0016 registers consume-1000, empty and refill operations through the shared development dispatcher. CLIENT-0032 invokes the same typed command representation used by the console and displays authoritative current/maximum Mana, corrections and regeneration.

These commands have no compatibility promise and are not stable gameplay protocol. The native proof does not require Fire Arrow, Arrow Rain, a permanent HUD or death/respawn behavior.