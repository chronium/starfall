---
title: Development Commands
createdAt: 2026-08-06T15:30:00.4857760Z
modifiedAt: 2026-08-06T15:30:00.4857760Z
---

## Purpose

Starfall uses one bounded development-only command envelope for typed ImGui controls and the text console. The World remains authoritative: the Client submits a request, the admitted-session dispatcher applies the development gate and selects a feature-owned handler, and the World returns one correlated success or rejection.

This is durable engineering instrumentation, not a production administration API or a stable gameplay feature protocol.

## Facts and bounds

`DevelopmentCommandSequence` is a non-zero unsigned 64-bit correlation value. Per-session monotonic acceptance belongs to the World dispatcher.

`DevelopmentCommandId` contains 1-64 lowercase ASCII letters, digits or underscores and begins with a letter. A request carries zero to eight ordered argument tokens. Each token contains 1-64 printable non-whitespace ASCII bytes. Arguments are immutable defensive snapshots.

A success or rejection repeats the sequence and command identity and carries 1-512 printable single-line ASCII diagnostic bytes. Diagnostic text is engineering output only; stable authoritative feature state uses its own feature protocol.

Rejection reasons are `Disabled`, `UnknownCommand`, `InvalidArguments`, `StaleOrDuplicateSequence` and `HandlerRejected`. The availability state is explicitly `Disabled` or `Enabled`.

## Wire layouts

All unsigned integers are big-endian. The accepted connection protocol version plus channel and result kind selects the layout; there is no packet-local schema byte.

Channel 7 carries reliable-ordered requests:

~~~text
sequence:u64
command-id-length:u8
command-id:ASCII
argument-count:u8
repeat argument-count times:
  argument-length:u8
  argument:ASCII
~~~

The request is 11-594 bytes.

Channel 8 carries reliable-ordered World facts:

| Kind | Layout | Bound |
| --- | --- | --- |
| 1 Availability | kind:u8, state:u8 | exactly 2 bytes |
| 2 Succeeded | kind:u8, sequence:u64, command identity, diagnostic-length:u16, diagnostic | at most 588 bytes |
| 3 Rejected | kind:u8, sequence:u64, command identity, reason:u8, diagnostic-length:u16, diagnostic | at most 589 bytes |

Decoders reject unknown kinds, invalid enum values, zero sequences, malformed ASCII, impossible lengths/counts, truncation and trailing bytes without throwing. Encoders validate a complete canonical fact before returning a new exact-length payload.

## Authority and enablement

Requests contain no account, session, player or actor identity. The World derives authority from the admitted transport peer and gameplay session. A later dispatcher task owns the explicit host development gate, publishes availability after admission, enforces per-session sequencing and returns `Disabled` when a valid request races a disabled gate.

Availability is not a role, permission, account entitlement or remote-operations policy. Missing or disabled development commands never change gameplay-session availability.

## Compatibility and exclusions

The additive development channels do not increment gameplay protocol version 1. Current Client and World source are expected to move together, and no legacy development-command decoder or migration promise exists. Development-only incompatibility does not redefine the stable gameplay facts carried by the negotiated gameplay protocol.

This contract defines no `ping_world`, Mana, inventory, player-life or other feature command. It also defines no parser, dispatcher, handler registry, console, scripting language, command discovery, filesystem access, shell execution, roles, administration, moderation or remote operations.