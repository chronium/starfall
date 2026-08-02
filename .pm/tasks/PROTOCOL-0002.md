---
id: PROTOCOL-0002
title: Define lobby admission and signed world-join tickets
track: PROTOCOL
milestone: M0
dependsOn:
- ARCH-0004
- BUILD-0002
createdAt: 2026-08-01T05:46:47.1759960Z
modifiedAt: 2026-08-02T07:30:17.2454910Z
---

Define the authenticated lobby-to-world admission contract: character/world selection inputs, short-lived signed ticket claims, signing and validation boundaries, expiry, replay protection, single consumption, and failure responses. The world creates its own active gameplay session and performs no continuing identity authorization. This is a contract task only; gameplay commands, events, and snapshots belong to PROTOCOL-0003.