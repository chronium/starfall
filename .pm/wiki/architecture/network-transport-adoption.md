---
title: Network Transport Adoption
createdAt: 2026-08-04T16:38:15.6691070Z
modifiedAt: 2026-08-05T18:52:35.8393240Z
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

`CLIENT-0009` completes the first concrete use of the BUILD-0006 composition factories.

Starfall assigns channel 0 reliable ordered admission, channel 1 reliable sequenced commands, channel 2 sequenced routine snapshots, and channel 3 reliable ordered corrections. Protocol remains free of transport references; it publishes only channel byte constants and exact fact codecs. Client and World alone own LiteNetLib polling and process lifecycle.

The World binds each admitted peer to one world gameplay session/player and disconnect cleans up only that ownership chain. The Client connects only to a literal loopback address, uses a ticket file rather than a raw command-line bearer token, waits up to ten seconds for admission plus the initial snapshot, and does not reconnect or resume. Plaintext loopback is approved solely for this development proof. Protected non-loopback transport, production key provisioning and broader packet policy remain future contracts.

## Bounded monster snapshot continuation

SERVER-0007 extends the same admitted gameplay-session host rather than adding another listener or exchange framework. Starfall assigns channel 4 to bounded full-state monster snapshots with `Sequenced` delivery. Each session owns an independent checked sequence and receives an initial tick-zero snapshot plus at most one latest snapshot per observed simulation tick. Admission and movement bytes, delivery modes and channels remain unchanged.

Because channel ordering is independent, a monster snapshot may arrive before admission acceptance. CLIENT-0023 retains the latest valid channel-4 batch before or after acceptance without making monster availability part of admission readiness. It ignores stale independent monster sequences, rejects backwards ticks on newer batches and preserves malformed-payload or delivery failures. Client maps the retained facts through the existing placeholder presentation adapter; no generic framing, reconnect behavior or transport dependency was added.
