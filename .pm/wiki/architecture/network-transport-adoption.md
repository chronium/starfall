---
title: Network Transport Adoption
createdAt: 2026-08-04T16:38:15.6691070Z
modifiedAt: 2026-08-04T16:38:15.6691070Z
---

## Decision

Starfall task `BUILD-0006` adopts the completed coordinator transport `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0023` at the two process composition roots required for connected walking.

Only `Starfall.Client` and `Starfall.World` directly reference:

```text
$(ChronoFallFamilyRoot)src/ChronoFall.Network.Transport.LiteNetLib/ChronoFall.Network.Transport.LiteNetLib.csproj
```

The BCL-only `ChronoFall.Network.Transport` contracts and coordinator-pinned LiteNetLib source are transitive. Starfall does not reference the contracts project or upstream checkout directly. No package, feed, absolute path, literal parent traversal or Royale reference is introduced.

Each process root owns a small internal factory that returns `INetworkTransport` backed by `LiteNetLibNetworkTransport`. The factories are composition seams only: this task does not instantiate them from either executable entry point or alter runtime behavior.

## Dependency and artifact boundary

Content, Protocol, Simulation, Editor and Balance Lab do not reference or emit the network transport. Simulation retains only its independently approved coordinator Box3D source reference. Client retains the approved character-presentation references plus the network adapter. World gains only the network adapter and remains headless and presentation-free.

Client and World outputs contain the shared transport contracts, LiteNetLib adapter and LiteNetLib assembly. No shared network assembly enters the other Starfall product outputs. The local Starfall product-project graph remains unchanged.

Protocol stays transport-independent. The shared transport carries opaque packets and does not own Starfall facts, deterministic codecs, frames, channels, delivery policy, admission, sessions or gameplay exchange.

## Availability and authority

A low-level transport connection never admits a player. Starfall World must consume a valid signed join ticket and create its own gameplay session before accepting gameplay intent. Once admitted, the active session remains independent of identity, chat and operations availability.

The coordinator transport does not supply confidentiality. Protected transport remains required for join tickets. This adoption task opens no listener, provisions no key and makes no deployment or local-development security decision.

## Connected walking continuation

`CLIENT-0009` depends on `BUILD-0006` and remains todo. Its separately approved plan must define:

- product channel and delivery assignments;
- admission request, accept and reject serialization;
- Client and World polling/composition;
- transport-peer to gameplay-session binding;
- disconnect and reconnect behavior;
- development key provisioning;
- the protected-development-transport stance.

That task must reuse the presentation adapter proven by `CLIENT-0021` and the in-process authoritative exchange proven by `SERVER-0005`. It must not infer product policy from the shared opaque transport.

Coordinator ownership: `pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/wiki/architecture/shared-network-transport`

Connected walking facts: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/protocol/connected-walking-facts`