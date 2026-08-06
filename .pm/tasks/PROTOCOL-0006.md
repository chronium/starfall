---
id: PROTOCOL-0006
title: Define first connected combat facts
track: PROTOCOL
milestone: M5
priority: high
dependsOn:
- SIM-0004
- SIM-0011
createdAt: 2026-08-03T07:29:08.8739060Z
modifiedAt: 2026-08-06T08:33:22.1422490Z
---

Define transport-neutral facts for the connected Basic Arrow deliverable.

Acceptance criteria:
- Define a Basic Arrow request with a non-zero command sequence and target entity identity. The client never supplies the acting entity.
- World derives the authoritative actor from the admitted gameplay session; actor identity appears only in authoritative outcome facts.
- Carry stable action, actor and target identities, authoritative start and resolve ticks, acceptance, rejection, cancellation, 300 internal-unit damage, effective damage and monster defeat.
- Retain SIM-0011 only for defeated-player and protected-town action rejection; do not transport player health, defeat, restoration or respawn here.
- Preserve the decision that arrows are presentation and no action creates an authoritative spatial projectile entity.
- Do not implement encoding, server exchange, Fire Arrow, Arrow Rain, mana, client presentation, chat or persistence.

## Notes

- 2026-08-06 08:33 UTC - Implemented the transport-neutral connected Basic Arrow fact contract.

  - Added bounded `CombatCommandSequence` and `CombatActionId` values plus the canonical `basic_arrow` identity.
  - Added an actor-free `BasicArrowCommand` and immutable accepted, rejected, canceled and resolved authoritative facts. Outcomes correlate command, World-derived actor, target and fixed ticks; resolved facts require exactly 300 requested damage units and bounded effective damage.
  - Kept unreachable `WrongAction` and actor-as-target states outside the connected fact surface. Added explicit reachable rejection/cancellation reasons without referencing Simulation.
  - Added focused contract tests for identity bounds, actor-free command shape, tick-zero handling, temporal boundaries, every reason, malformed/default values, exact damage and defeat facts.
  - Added pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/protocol/connected-basic-arrow and linked it from the architecture overview and Draft 0 archer contract.
  - Preserved boundaries: no codec, transport/channel, World, Simulation, Client, projectile, player-life, Mana, Fire/Rain, asset or project-reference changes. Starfall.Protocol still has no project or package references.
  - Validation: restore succeeded; Debug and Release solution builds succeeded with zero warnings using the final single-worker build; all 361 tests passed in both configurations (37 Architecture, 62 Client, 1 ConnectedWalking, 31 Content, 92 Protocol, 46 Simulation, 92 World); focused formatting verification and git diff --check passed. PM validation passed with only the existing legacy-milestone-schema and empty-M3 warnings; linked-family inspection reported zero warnings.
  - No native or visual validation was required. The coordinator audit checklist remains unchanged because M5 Connected Basic Arrow is not yet complete.