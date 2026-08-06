---
id: SERVER-0008
title: Exchange first connected combat commands and outcomes
track: SERVER
milestone: M5
priority: high
dependsOn:
- SERVER-0005
- SERVER-0007
- PROTOCOL-0007
- PROTOCOL-0015
- SIM-0004
- SIM-0011
createdAt: 2026-08-03T07:29:07.3311690Z
modifiedAt: 2026-08-06T11:04:57.2041010Z
---

Route connected Basic Arrow requests and publish authoritative Basic outcomes through admitted World sessions.

Acceptance criteria:
- Decode Basic Arrow requests, derive the actor from the admitted gameplay session and validate the target against World-owned monster state.
- Route valid requests into the proven SIM-0004 behavior and publish authoritative timing, rejection, cancellation, 300-unit damage and monster-defeat facts.
- Continue publishing monster health and defeat through the existing SERVER-0007 snapshot contract.
- Consume SIM-0011 only for defeated-player and protected-town action rejection.
- Do not publish player health, defeat, restoration or respawn as part of this task.
- Preserve presentational arrows and server-authoritative fixed-tick outcomes.
- Do not add Fire Arrow, Arrow Rain, projectile entities, persistence, chat or a generic messaging or ability framework.

## Notes

- 2026-08-06 11:04 UTC - Implemented the connected Basic Arrow World exchange.

  Decisions and behavior:
  - reliable-sequenced channel 5 carries Basic Arrow commands; reliable-ordered channel 6 carries requester-only accepted, rejected, canceled and resolved outcomes;
  - the authoritative actor is always derived from the admitted gameplay session;
  - command sequences are monotonic per session, allow gaps, consume fresh values before evaluation and silently ignore stale/duplicate values;
  - movement and combat retain cross-channel arrival ordering, with accepted movement publishing an immediate windup cancellation;
  - terminal outcomes publish after every individual 60 Hz fixed tick, including catch-up ticks, before the runtime's bounded result batch can be overwritten;
  - monster health and defeat remain on the established sequenced monster snapshot stream, so outcome/snapshot cross-channel order is intentionally unspecified;
  - malformed payloads, wrong channel/delivery and unknown sessions are protocol violations; send failure and disconnect remove session correlation and runtime ownership.

  Validation:
  - dotnet restore Starfall.slnx: all projects up to date;
  - Debug and Release dotnet build Starfall.slnx --no-restore -m:1: succeeded with 0 warnings and 0 errors;
  - Debug and Release dotnet test Starfall.slnx --no-restore --no-build -m:1: 391/391 tests passed in each configuration (37 Architecture, 63 Client, 2 real LiteNetLib loopback, 31 Content, 107 Protocol, 46 Simulation, 105 World);
  - the real UDP proof admitted and moved one player, resolved three Basic Arrows against starter_flyer_light for 300/300/100 effective damage, observed 700 -> 400 -> 100 -> 0 health and received the existing defeat tombstone;
  - PM doctor passed with only the existing legacy milestone-schema and empty-M3 warnings;
  - git diff --check passed;
  - Release Starfall.World artifact inspection found no SDL, GPU, ImGui, Client, Editor, shader, texture or presentation dependency.

  Durable pages updated: protocol/connected-basic-arrow, protocol/gameplay-protocol-compatibility, architecture/world-channel-lifecycle and roadmap/bootstrap.

  No visual/native owner validation was required because this task changes only the headless authoritative exchange.