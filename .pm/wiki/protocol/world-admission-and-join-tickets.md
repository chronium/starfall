---
title: World Admission and Join Tickets
createdAt: 2026-08-03T06:08:22.0865090Z
modifiedAt: 2026-08-04T10:16:06.2111050Z
---

## Purpose

`PROTOCOL-0002` defines Starfall's provisional authenticated handoff from identity/lobby to one world lifecycle. The ticket is a short-lived bearer credential. It proves that identity/lobby selected one account, character, configured world, configured channel, and live world instance; it does not create or continuously authorize an active gameplay session.

The world validates using locally configured public keys. After successful admission, gameplay does not call identity, chat, or operations to remain authorized.

## Public contract

The protocol defines:

- opaque non-empty GUID identities for the ticket, account, character, world lifecycle instance, and world-owned gameplay session;
- lowercase configured world and channel identities using 1-64 ASCII letters, digits, or underscores and beginning with a letter;
- ticket claims containing those identities plus issue and expiry times in Unix milliseconds;
- a join request carrying the compact ticket;
- a successful response carrying a newly generated gameplay-session ID;
- bounded rejection reasons: invalid ticket, expired ticket, already consumed, wrong destination, and world not accepting admissions.

Account and character IDs contain no personal display information. The ticket does not contain credentials, character state, authorization roles, chat state, gameplay state, or persistence data.

## Compact ticket version 1

The provisional representation is:

```text
sfjt1.<key-id>.<base64url-payload>.<base64url-signature>
```

`sfjt1` fixes ECDSA over NIST P-256 with SHA-256. Signatures use the 64-byte IEEE P1363 fixed-field concatenation. The exact ASCII signing input is:

```text
sfjt1.<key-id>.<base64url-payload>
```

The key ID is therefore authenticated. It contains 1-64 ASCII letters, digits, underscores, or hyphens. Base64url segments omit padding and must use their canonical representation.

The payload is encoded in this exact order:

| Field | Encoding |
| --- | --- |
| Ticket ID | 16-byte RFC 4122 GUID in network byte order |
| Account ID | 16-byte RFC 4122 GUID in network byte order |
| Character ID | 16-byte RFC 4122 GUID in network byte order |
| World ID | One-byte length followed by canonical ASCII bytes |
| Channel ID | One-byte length followed by canonical ASCII bytes |
| World-instance ID | 16-byte RFC 4122 GUID in network byte order |
| Issued at | Signed 64-bit big-endian Unix milliseconds |
| Expires at | Signed 64-bit big-endian Unix milliseconds |

The decoded payload is bounded to 84-210 bytes and the complete token to 512 ASCII characters. Extra segments, extra payload bytes, padding, non-canonical base64url, invalid text, empty GUIDs, unsupported versions, and out-of-range timestamps are rejected.

This format is explicitly versioned and provisional. A replacement requires a later protocol decision and compatibility plan; it must not be changed implicitly by transport work.

## Signing and key rotation

Identity/lobby owns ECDSA private keys and ticket issuance. Worlds receive only SubjectPublicKeyInfo public keys and resolve the signed key ID from a locally configured key ring. Protocol code stores no keys, credentials, or secrets.

Rotation uses overlapping public keys. Identity begins signing with a new key ID after worlds can verify it. A previous public key remains available for at least the maximum ticket lifetime plus clock-skew allowance, then may be removed. Unknown keys fail closed and do not require an online identity call.

The repository contains no production key generation, storage, distribution, or configuration. Those operational concerns require later task-owned work.

## Lifetime and audience

A ticket lifetime must be between 1 and 60,000 milliseconds. Validation receives an explicit current Unix-millisecond value. A five-second clock-skew allowance applies:

- an issue time more than five seconds in the verifier's future is invalid;
- a ticket is expired when verifier time reaches expiry plus five seconds.

Every ticket is bound to an exact world ID, channel ID, and lifecycle-specific world-instance ID. Each new world lifecycle must generate a new non-empty instance ID. Identity/lobby learns the currently advertised instance during admission; this admission-time data flow does not become an active-session dependency. Tickets issued for an earlier process lifecycle are wrong-destination tickets after restart.

## Validation and admission order

A world performs the bounded work in this order:

1. Reject an oversized or malformed envelope.
2. Resolve the authenticated key ID from local public-key configuration.
3. Verify the ECDSA signature before interpreting claims.
4. Decode and validate the canonical payload and lifetime.
5. Apply issue-time and expiry rules using the explicit clock.
6. Match world, channel, and world-instance audience exactly.
7. Atomically consume the unique ticket ID before creating a gameplay session.
8. Generate a new world-owned gameplay-session ID and continue independently of identity.

Steps 1-6 are implemented by `Starfall.Protocol`. `SERVER-0003` implements steps 7-8 in `Starfall.World` behind one synchronized world-lifecycle gate. The gate makes ticket consumption, lifecycle eligibility, session creation, draining, and stopping one world-local ownership boundary.

Exactly one concurrent admission may consume a ticket. A failed attempt before consumption may retry while the ticket remains valid. A failure after consumption never makes the credential reusable; the client must return to identity/lobby for a new ticket. Consumed ticket IDs are retained through expiry plus the five-second skew and lazily pruned when a later cryptographically valid request reaches the world boundary. A world restart changes the world-instance ID and discards the lifecycle-local replay set rather than recovering or distributing it.

An active in-memory session retains only its new session ID plus the admitted account, character, and world-instance identities. The raw bearer ticket is never retained. Draining rejects new admission while retaining existing sessions and fixed-step execution. Stopping terminates and clears the remaining session and replay registries.

## Failure and diagnostic boundary

Protocol validation exposes only invalid ticket, expired ticket, or wrong destination. The world adds already consumed and world not accepting admissions when it owns those facts. Malformed encoding, unknown keys, bad signatures, excessive lifetime, and future issuance map to invalid ticket rather than exposing cryptographic detail to clients.

Implementations may record bounded counters for internal diagnosis, but must never log raw tickets, signatures, private keys, or complete bearer credentials.

## Transport and availability boundaries

`PROTOCOL-0002` owns the self-contained ticket encoding and transport-neutral admission facts. `SERVER-0003` implements the narrow host-specific binding as an internal in-process World exchange that receives one bounded join request and returns exactly one existing accept or reject fact. It is the invocation seam for a later protected host transport, not a socket, serializer, framing layer, or generic message dispatcher. The current command-line entry point does not configure public keys or expose admission over a network.

TLS or an equivalent protected transport remains required because signatures provide authenticity and integrity, not confidentiality. This task does not implement transport security.

An unavailable identity/lobby prevents new admission but cannot terminate or continuously reauthorize sessions already owned by a world. Chat and operations remain optional from gameplay's perspective. Persistence degradation is still unresolved and is not implied by admission success.

## Non-goals

This bounded admission path does not implement accounts, credentials, lobby UI, character summaries, identity services, network sockets, transport security or framing, persistent or distributed replay storage, durable sessions, persistence, chat, gameplay commands, gameplay events, snapshots, key provisioning, JWT, a generic token framework, or physical deployment topology.