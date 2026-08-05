---
id: SERVER-0007
title: Exchange bounded monster snapshots
track: SERVER
milestone: M2
dependsOn:
- SERVER-0005
- PROTOCOL-0005
- SIM-0010
- SIM-0011
createdAt: 2026-08-03T07:29:07.0749230Z
modifiedAt: 2026-08-05T18:19:56.8164650Z
---

Extend the existing admitted gameplay-session host with bounded authoritative monster snapshots.

Acceptance criteria:
- Map the approved live monster identity, archetype, transform, behavior/target, collision radius and current/maximum health facts from immutable World state without moving simulation rules into Protocol or World exchange code.
- Retain an ordered defeated-monster tombstone for each lethally vacated placement slot and repeat it until that exact slot replenishes with a fresh entity identity. Technical removal and lifecycle shutdown do not fabricate defeat facts.
- Publish one latest full snapshot per admitted session at most once per observed simulation tick, including an initial tick-zero snapshot, using an independent checked per-session sequence.
- Assign channel 4 with Sequenced delivery. Preserve existing admission, walking-command, walking-snapshot and correction bytes and channels.
- Preserve deterministic live/tombstone ordering, the ten-slot bound, peer/session/world isolation, draining behavior and focused cleanup.
- Keep one gameplay network host with separate walking and monster exchange seams; do not introduce a generic event bus, multiplex framing or a second world host.
- Keep the completed connected-walking client usable by validating and temporarily ignoring well-formed monster snapshots before or after admission. Malformed or misrouted data remains a failure; CLIENT-0023 owns retained consumption and presentation.
- Keep monster simulation in Starfall.Simulation and presentation in Starfall.Client.
- Do not add combat commands, selected assets, rendering, persistence, interest management, smoothing or a scalable production cadence contract.

## Implementation notes

- Added one `WorldGameplayNetworkHost` with separate walking and monster exchanges. Channel 4 uses `Sequenced` full-state payloads; admission and walking channels/bytes are unchanged.
- Added independent checked per-session monster snapshot sequences, initial tick-zero publication and at-most-once publication for each observed simulation tick. Catch-up cycles expose only the latest completed state.
- World now preserves immutable maximum health and retains lethal defeated-monster state by exact placement slot. Tombstones repeat for current and newly admitted sessions until that slot replenishes with a fresh entity ID. Technical removal and shutdown do not fabricate defeat facts.
- The existing connected-walking client validates and ignores well-formed monster snapshots before or after admission so SERVER-0007 can land without breaking the completed walking milestone. CLIENT-0023 remains the owner of retained consumption and presentation.
- Added authoritative mapping, ordering, health/behavior/target, sequence, lifecycle, tombstone, replenishment, channel/delivery, malformed compatibility and real LiteNetLib loopback coverage.
- Validation: Debug and Release solution builds succeeded with zero warnings; all 334 tests passed in both configurations (37 Architecture, 58 Client, 1 ConnectedWalking, 31 Content, 69 Protocol, 46 Simulation, 92 World). Focused `dotnet format --verify-no-changes` passed for every changed C# file. The solution-wide formatter continues to report pre-existing whitespace findings only in unchanged `Draft0BasicArrowRules.cs`.
- Starfall `pm doctor` and `git diff --check` passed. Debug and Release World outputs contain only Content, Protocol, Simulation, approved Box3D/network transport assemblies and ordinary host files; no Client, SDL, GPU, ImGui, editor or presentation artifacts are present.
- No visual checkpoint is applicable: this task is a headless authoritative exchange and deliberately does not render received monsters.
