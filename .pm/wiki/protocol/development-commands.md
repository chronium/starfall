---
title: Development Commands
createdAt: 2026-08-06T15:30:00.4857760Z
modifiedAt: 2026-08-06T16:34:20.6701250Z
---

## Purpose

Starfall uses one bounded development-only command envelope through its ImGui text console. The World remains authoritative: a Client submits a request through an admitted transport peer, the session-bound dispatcher selects a feature-owned handler, and the World returns one correlated success or rejection.

This is durable engineering instrumentation, not a production administration API or a stable gameplay feature protocol.

## Facts and bounds

`DevelopmentCommandSequence` is a non-zero unsigned 64-bit correlation value. The World consumes fresh sequences monotonically per admitted gameplay session before lookup or handler execution. A duplicate or lower sequence is rejected, and one session's sequence does not affect another.

`DevelopmentCommandId` contains 1-64 lowercase ASCII letters, digits or underscores and begins with a letter. A request carries zero to eight ordered argument tokens. Each token contains 1-64 printable non-whitespace ASCII bytes. Arguments are immutable defensive snapshots.

A success or rejection repeats the sequence and command identity and carries 1-512 printable single-line ASCII diagnostic bytes. Diagnostic text is engineering output only; stable authoritative feature state uses its own feature protocol.

Rejection reasons are `UnknownCommand`, `InvalidArguments`, `StaleOrDuplicateSequence` and `HandlerRejected`.

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

Channel 8 carries reliable-ordered World results:

| Kind | Layout | Bound |
| --- | --- | --- |
| 1 Succeeded | kind:u8, sequence:u64, command identity, diagnostic-length:u16, diagnostic | at most 588 bytes |
| 2 Rejected | kind:u8, sequence:u64, command identity, reason:u8, diagnostic-length:u16, diagnostic | at most 589 bytes |

Decoders reject unknown kinds, invalid enum values, zero sequences, malformed ASCII, impossible lengths/counts, truncation and trailing bytes without throwing. Encoders validate a complete canonical fact before returning a new exact-length payload.

## Authority and dispatch

Requests contain no account, session, player or actor identity. The World derives authority from the admitted transport peer and gameplay session. Every admitted connected player may currently invoke every registered development command. There is no launch gate, availability packet, role, permission or per-command authorization policy.

The dispatcher copies an ordinal handler registry, rejects duplicate command identities, consumes a fresh sequence before dispatch and returns bounded correlated results. Malformed payloads, wrong delivery and missing admitted-session ownership are protocol violations. Valid command rejections do not disconnect the gameplay session. Unexpected handler exceptions are logged by the World and become a generic `HandlerRejected` result without exposing implementation details.

`ping` is the only registered command in SERVER-0015. No legacy alias is retained; every identity other than an explicitly registered command is rejected as unknown. It accepts no arguments and returns:

~~~text
pong world=<world> channel=<channel> tick=<tick> session=<session> player=<entity>
~~~

The fields are development diagnostics, not a stable machine-readable gameplay fact.

## Client console

CLIENT-0031 exposes the envelope through the interactive native Client. `T` opens a bottom-left ImGui console only while the master debug shell is visible; `F12` remains the global shell toggle and `--debug-ui-hidden` prevents `T` from opening it until the shell is shown. Open input captures gameplay keyboard and pointer input. `Enter` submits and closes, `Escape` cancels and closes without exiting, and Up/Down navigate the last 32 non-empty attempts.

The input is bounded to 584 ASCII bytes and uses spaces to separate the command identity from at most eight protocol arguments. It defines no quoting or escaping. The console retains 128 result lines. While closed, at most six recent lines remain over the game view for ten seconds and then fade over two seconds with a transparent, non-interactive background.

The connected Client allocates positive monotonic sequences, permits at most 64 outstanding-or-unconsumed command lifecycles, correlates exact command identities and exposes valid success or rejection results without disconnecting gameplay. Malformed, misdelivered, duplicate or inconsistently correlated results are protocol violations. Local preview accepts console input but reports that an authoritative World connection is required.

## Compatibility and exclusions

The additive development channels do not increment gameplay protocol version 1. Current Client and World source are expected to move together, and no legacy development-command decoder or migration promise exists. SERVER-0015 removes the earlier unconsumed availability and disabled shapes rather than retaining dormant compatibility.

A future roles or authorization design must be justified and reviewed when it has a concrete requirement. This contract defines no command discovery, scripting language, filesystem access, shell execution, administration, moderation or remote operations. Feature-owned Mana, inventory and player-life commands remain later task scope.