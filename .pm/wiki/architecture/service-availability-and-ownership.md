---
title: Service Availability and Ownership
createdAt: 2026-08-01T06:48:13.9601190Z
modifiedAt: 2026-08-04T10:17:41.6762690Z
---

## Decision

Define Starfall's logical service ownership and availability boundaries now. Defer the final physical process and deployment topology until the authoritative vertical slice produces operational and failure-testing evidence.

The governing invariant is:

> Once admitted to a world, an active player's gameplay session must not depend on authentication, chat, or management services remaining available.

This decision fixes ownership and dependency direction. It does not require one process per logical boundary.

## Logical ownership

| Boundary | Owns | Must not own or control |
|---|---|---|
| Identity and lobby | Accounts, credentials, authentication, realm/world listing, character-summary display, creation/selection orchestration, and short-lived signed world-join tickets | Active gameplay authorization, combat, inventory, equipment, progression, or world entities |
| Realm/world | Authoritative movement, combat, character rules and state, inventory, equipment, progression, drops, monsters, zones, worlds/channels, and active gameplay sessions | Account credentials, chat delivery, or process supervision |
| Chat | Local, global, private, guild, and event message routing, moderation, and delivery | Combat events, loot ownership, progression authority, or any prerequisite for gameplay |
| Operations control plane | Process health, start/drain/stop operations, configuration, logs, metrics, development multipliers, and diagnostics | Gameplay authority, direct browser-to-process supervision, or any gameplay hot-path dependency |
| Persistence | Durability mechanisms and records for identity-owned account state and gameplay-owned character state, behind explicit consistency and degradation contracts | Domain rules, continuous gameplay authorization, or active world-session authority |

Identity owns accounts and authentication. Starfall gameplay owns authoritative character rules and character data. The lobby may consume a read-optimized character-summary projection and orchestrate creation or selection, but that projection is not the gameplay source of truth.

## Admission and gameplay session

The intended handoff is:

1. The player authenticates and enters the lobby.
2. The lobby retrieves character summaries.
3. The player selects or requests creation of a character and selects a world/channel.
4. Identity/lobby issues a short-lived signed world-join ticket.
5. The selected world validates and consumes the ticket and creates its own gameplay session.
6. The active gameplay session no longer calls identity for continuing authorization.

The executable admission contract is defined by `PROTOCOL-0002` at `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/protocol/world-admission-and-join-tickets`. It uses a versioned ECDSA P-256 ticket bound to the selected account, character, world, channel, and lifecycle-specific world instance. Worlds validate with locally configured public keys, then atomically consume the unique ticket ID before creating a session. `SERVER-0003` implements that responsibility as lifecycle-local in-memory replay and session state behind a synchronized World boundary. The admitted session retains no live identity, chat, or operations dependency.

## Availability behaviour

| Unavailable boundary | Required behaviour |
|---|---|
| Identity/lobby | Existing admitted gameplay sessions continue. New authentication, lobby, roster, selection, and admission operations may be unavailable. |
| Chat | Gameplay continues. Chat UI may report degradation. Combat, loot, equipment, progression, and other critical notifications continue through the game protocol. |
| Operations control plane | Gameplay continues without process-management or management-UI availability. Running worlds do not poll the control plane to remain authorized or healthy. |
| One world/channel | Sessions owned by that world may fail or recover according to its lifecycle. Unrelated worlds/channels continue and share no mutable gameplay authority with it. |
| Persistence | Behaviour is intentionally unresolved. The design must later specify admission, journaling/buffering, restricted durable operations, draining, recovery, and consistency before claiming safe degradation. |

The active-session invariant deliberately does not promise transparent persistence failure. A world owns current authoritative session state, but durable outcomes require an approved outage and recovery policy.

## Worlds and channels

Each world/channel is an independent lifecycle and authoritative state owner. It owns its live population, monsters, drops, camps, progression events, and active sessions. Cross-world features must not create synchronous dependencies that stop unrelated worlds.

`SERVER-0002` establishes the executable empty-world lifecycle contract at `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/world-channel-lifecycle`. Every process invocation requires explicit world and channel identities and creates a fresh world-instance identity. Running is the only admission-eligible state; draining immediately rejects future admission while retaining existing sessions and allowing their authoritative fixed ticks to continue until stop. `SERVER-0003` adds the bounded admission and session seam: running accepts validated joins, draining rejects new joins while retaining existing sessions, and stopping clears lifecycle-local session and replay state. The current command-line host still has no key provisioning or network admission transport, so its standalone lifecycle run creates no sessions and stops immediately after entering drain. This remains independent of process supervision.

Physical host granularity remains open. The current executable hosts one explicitly selected logical world/channel per invocation as the smallest evidence-producing boundary; that is not a final deployment-topology decision. A later host may run one or more logical worlds, but the design must preserve separable state, lifecycle, and interfaces so failure-isolation evidence can determine whether worlds require separate processes or hosts.

## Chat boundary

Gameplay treats chat as optional. Local chat may ask the world to determine proximity recipients or may consume suitable presence/location information published by the world. Chat still owns message routing, moderation, delivery, retry, and user-visible delivery failure.

Gameplay-critical information never relies on chat delivery. Combat results, loot ownership, progression changes, equipment changes, and authoritative event feedback use the game protocol.

## Operations control plane

The intended management interface is a small Angular application backed by an ASP.NET operations API. The Angular client calls the API; it does not supervise processes directly.

The operations API may coordinate health, start, drain, stop, configuration, logs, metrics, development multipliers, and diagnostics through explicit adapters. Operations infrastructure is outside the gameplay hot path, and its failure has no effect on active gameplay.

The operations application is not the Starfall content editor or Balance Lab.

## Initial deployment envelope

Logical boundaries do not initially require separate deployables. The first implementation may use strict modules and perhaps only identity/lobby and world executables. Chat and operations may remain modules or stubs until independent failure behaviour is ready to test.

Co-hosting is a deployment convenience, not permission for world-session code to call identity, chat, or operations synchronously. Avoid premature microservice granularity, distributed transactions, and infrastructure whose only purpose is maximizing deployable count.

## Monster camps

Do not extract monster camps into a separate runtime service. The authoritative chain is:

```text
camp definition
  -> spawn and replenishment policy
  -> authoritative world entities
```

The world owns spawning, monsters, deaths, replenishment, drops, and actual entities. The editor and headless Balance Lab may share camp definitions, policy data, validation, and deterministic simulation rules without sharing a runtime service.

A future population director may recommend spawn pressure. The world remains authoritative and continues from its last valid policy when that director is unavailable.

## Existing authority and headless rules

This decision preserves the existing server-authoritative boundary. Worlds decide gameplay outcomes and durable-intent changes; clients present authoritative state and events. Animation, rendering, IK, effects, cameras, and smoothing never authorize gameplay.

Headless server, simulation, and Balance Lab code must not depend on SDL windowing, SDL GPU, ImGui, rendering, editor UI, or presentation assets. Shared authoring data compiles into compact Starfall runtime data rather than forcing a reflective runtime component system.

## Deferred persistence contract

Before persistence implementation or topology is approved, a later evidence-driven architecture task must decide:

- whether persistence outages block new admission or only durable operations;
- which state may be journaled or buffered, for how long, and where;
- which economy, trade, inventory, or progression actions must pause;
- when a world drains or terminates sessions rather than accumulating risk;
- idempotency, retry, conflict, ordering, and reconciliation rules;
- recovery-point and recovery-time expectations;
- how partial saves and cross-boundary failures are detected and repaired.

No implementation may silently choose these semantics.

## Evidence gate for physical topology

The final topology decision follows the vertical slice and should use evidence from:

- world tick and network hot paths;
- module dependency and state-ownership audits;
- failure injection for identity, chat, operations, one world, and persistence;
- world/channel lifecycle and recovery experiments;
- headless deterministic simulation;
- operational complexity, local development ergonomics, and deployment cost;
- observed reasons to extract a boundary rather than assumed microservice benefits.

The result may retain modules, split processes, or use multiple host shapes. Ownership and the active-session invariant remain fixed regardless of that choice.

## Non-goals

This decision does not implement identity, lobby, chat, persistence, operations UI/API, process supervision, production world orchestration, or deployment infrastructure. The bounded empty world/channel executable is lifecycle evidence, not an operations control plane or final topology. This decision does not choose a persistence model, process count, container topology, distributed transaction scheme, population director, or general service framework.