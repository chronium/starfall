---
id: PROTOCOL-0002
title: Define lobby admission and signed world-join tickets
track: PROTOCOL
milestone: M0
dependsOn:
- ARCH-0004
- BUILD-0002
createdAt: 2026-08-01T05:46:47.1759960Z
modifiedAt: 2026-08-03T06:13:05.8719740Z
---

Define and prove the authenticated lobby-to-world admission contract as a BCL-only, transport-neutral Starfall.Protocol capability.

## Acceptance criteria

- Define strongly typed ticket, account, character, world-instance, and gameplay-session GUID identities plus bounded lowercase semantic world and channel identities.
- Define claims for ticket, account, selected character, world, channel, lifecycle-specific world instance, issue time, and expiry time.
- Implement the provisional compact `sfjt1` ticket codec with canonical bounded encoding, ECDSA P-256/SHA-256 signatures in IEEE-P1363 format, signed key identifiers, and public-key-only world validation.
- Enforce a maximum 60-second lifetime, five seconds of clock skew, explicit caller-supplied validation time, exact destination matching, and a new world-instance identity for every lifecycle.
- Support overlapping verification keys by signed key ID without storing keys, credentials, or secrets in the repository.
- Define admission request, accepted-session, and bounded rejection facts. Detailed cryptographic diagnostics remain internal.
- Specify that the world verifies first, then atomically consumes the ticket ID once before creating its own gameplay session. Replay storage and session creation remain SERVER-0003 responsibilities.
- Document the exact format, validation order, key-rotation boundary, replay semantics, bearer-token handling, and continued independence of active sessions from identity, chat, and operations.
- Add focused protocol tests and preserve the existing project graph, BCL-only protocol boundary, and headless isolation.
- Do not implement identity/lobby services, networking transport, replay storage, world sessions, persistence, chat, gameplay messages, key provisioning, JWT dependencies, or deployment infrastructure. Gameplay commands, events, and snapshots remain PROTOCOL-0003 scope.

## Notes

- 2026-08-03 06:00 UTC - Approved implementation plan: add an executable BCL-only sfjt1 admission contract using ECDSA P-256/SHA-256, public-key-only world validation, hybrid opaque/semantic identities, world/channel/lifecycle-instance audience binding, a 60-second maximum lifetime with five seconds of skew, canonical bounded encoding, key rotation, deterministic validation results, and request/accept/reject facts. Replay state and active session creation remain SERVER-0003-owned; transport framing remains PROTOCOL-0004-owned. No identity service, network host, persistence, gameplay protocol, external package, key material, or deployment topology is included.
- 2026-08-03 06:13 UTC - Implemented the executable BCL-only world-admission contract. Added validated hybrid identities, bounded request/accept/reject facts, the canonical sfjt1 payload and compact token codec, NIST P-256/SHA-256 IEEE-P1363 signing, public-key-only verification key rings with signed key IDs and rotation overlap, explicit time/audience validation, and coarse failure mapping. Added the durable protocol wiki and reconciled the service and architecture summaries. Replay storage, atomic consumption, world-session creation, transport framing, identity services, persistence, and key provisioning remain with their planned owners.

  Validation:
  - dotnet restore Starfall.slnx: passed; 15 projects restored/current.
  - dotnet build Starfall.slnx --no-restore: passed with 0 warnings and 0 errors.
  - focused Starfall.Protocol.Tests: passed 24/24.
  - full dotnet test Starfall.slnx --no-restore --no-build: passed 52/52 (24 protocol, 23 architecture, 5 content).
  - dotnet format verification: Starfall-owned code passed; the full solution reported only the existing ignored SDL3-CS naming warnings.
  - Starfall.Protocol has no project references and no package references.
  - Starfall.World output contains only World, Content, Protocol, Simulation and .NET host artifacts; no SDL, GPU, ImGui, editor, shader, texture, or presentation artifact entered headless output.
  - PM validation and pm doctor passed; the linked family has 3 available/readable/trusted members and 0 warnings.
  - git diff --check passed. No native or owner visual validation was required because this task changes no runtime UI, controls, rendering, audio, camera, or gameplay feel.
  - No production key material, coordinator source, Royale files, child-to-child dependency, generated content, or external package was added.