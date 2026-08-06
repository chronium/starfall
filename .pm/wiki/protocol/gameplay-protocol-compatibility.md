---
title: Gameplay Protocol Compatibility
createdAt: 2026-08-06T10:28:11.1704800Z
modifiedAt: 2026-08-06T10:28:11.1704800Z
---

## Purpose

Starfall establishes wire compatibility once when a Client is admitted to a World. Gameplay packet layouts are implied by the accepted gameplay protocol version plus the transport channel and, where a channel carries several facts, the message kind. Hot gameplay packets do not repeat a schema-version byte.

This contract is distinct from build identity, content compatibility, signed join-ticket representation and persisted formats.

## Version contract

`ProtocolVersion` is a non-zero byte-backed value. `StarfallGameplayProtocol.CurrentVersion` is currently `1`.

A Client offers exactly one version in `WorldJoinRequest`. The World supports exactly its current version:

1. decode the bounded admission request;
2. compare the offered version with the current version;
3. reject `IncompatibleProtocolVersion` before ticket verification, ticket consumption, player creation or gameplay-session creation when they differ;
4. validate and consume the signed ticket only after compatibility succeeds;
5. return the selected version in `WorldJoinAccepted`;
6. record that selected version in both the world-owned gameplay session and the connected Client session.

There is no range, downgrade, feature negotiation or simultaneous legacy-layout support. An accepted version must equal the Client's offer. Version zero is invalid.

The version changes only for an intentionally incompatible Client/World communication boundary. Creating a fact or codec does not by itself increment the version. Every exchange task must decide whether its actual emitted/accepted behavior remains compatible.

## Admission bootstrap

Channel 0 uses reliable ordered delivery:

| Fact | Exact layout |
| --- | --- |
| Join request | offered protocol version byte, kind `1`, unsigned 16-bit big-endian ticket length, then 1–512 canonical ASCII ticket bytes; 5–516 bytes |
| Accepted | selected protocol version byte, kind `2`, then the 16-byte gameplay-session GUID in RFC 4122 network byte order; 18 bytes |
| Rejected | kind `3`, then one bounded rejection-reason byte; 2 bytes |

The admission codec accepts any non-zero offered/selected version as a structurally valid bootstrap fact. World and Client policy perform the exact compatibility decision. A rejection has no selected version because no compatible gameplay session exists.

The current request and accepted version-1 bytes remain unchanged from the earlier packet-local representation; their first byte now has connection-level meaning. The rejection representation is version-neutral.

## Gameplay layouts

Current gameplay codecs contain no packet-local version:

| Channel or family | Layout discriminator |
| --- | --- |
| Movement command | channel 1 |
| Movement snapshot | channel 2 |
| Movement correction | channel 3 |
| Monster snapshot | channel 4 |
| Basic Arrow lifecycle | its eventual exchange channel plus `BasicArrowPayloadKind` and the bounded action identity |

Walking payload lengths are 16, 65 and 73 bytes. The monster header is 18 bytes with a maximum 1,208-byte payload. Basic Arrow uses a 13-byte kind/action header with 29, 53, 46, 62 and 62-byte command/accepted/rejected/canceled/resolved payloads.

All existing exact-length, canonical-number, identity, ordering, acknowledgement, bound and malformed-input rules remain in force.

## Cross-channel admission ordering

Reliable admission acceptance and sequenced gameplay snapshots may arrive in different channel order. Under the exact-match-only contract, the Client may validate and retain a well-formed early movement or monster snapshot using the version it offered, but it does not become ready until acceptance confirms that exact version.

Supporting a selected version different from the offer would require a later handshake change that prevents gameplay publication until the Client learns the selection. It must not be added as an implicit decoder branch.

## Independent versions

The `sfjt1` prefix independently versions the signed, stored and parsed join-ticket object. Cooked bundles and other persisted formats keep their own schema versions because they exist outside a negotiated connection.

Gameplay protocol version 1 makes no assertion about executable builds, content manifests, asset availability or persistence compatibility.

## Future compatibility

If Starfall later supports multiple connection versions simultaneously, admission must expose an explicit supported-version policy and the World must choose genuinely version-specific codecs from the recorded session version. Old decoders and upgrade behavior require their own reviewed task and fixtures.

Per-message versions are introduced only if one accepted connection genuinely needs to distinguish multiple layouts for the same message kind. A constant that merely rejects every value except the current one is not compatibility support.