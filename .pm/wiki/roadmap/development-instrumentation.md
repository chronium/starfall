---
title: Development Instrumentation
createdAt: 2026-08-06T06:46:50.0961910Z
modifiedAt: 2026-08-06T06:46:50.0961910Z
---

## Deliverable

A developer can open Starfall's debug GUI, issue one harmless authoritative command through either a typed control or the console, receive the correlated result, and hide the entire GUI without affecting gameplay.

This is milestone M4. It is durable engineering instrumentation with no gameplay-protocol compatibility promise.

## Dependency path

~~~text
parent coordinator Client-consumption boundary
  -> CLIENT-0029  adopt shared ImGui backend in Starfall.Client
  -> CLIENT-0030  Starfall debug shell

PROTOCOL-0004
  -> PROTOCOL-0013  development-only command/result envelope

SERVER-0005 + PROTOCOL-0013
  -> SERVER-0015  admitted-session dispatcher and Ping World

CLIENT-0030 + PROTOCOL-0013 + SERVER-0015
  -> CLIENT-0031  console and typed Ping World proof
~~~

CLIENT-0029 is intentionally `priority: none` until the separately owned coordinator Cycle 2 allocates the exact Client-consumption prerequisite and Cycle 3 attaches its canonical URI. Planning and implementation must not begin while that dependency is absent.

## Ownership

ChronoFall owns only the reusable caller-controlled ImGui backend, native boundary, family-source allowlist and headless exclusion. Starfall owns the debug shell, menu, concern-specific windows, `F12`, `--debug-ui-hidden`, input capture, command envelope, World dispatcher, console and product diagnostics.

ImGui button and console text frontends converge on the same development-command representation and dispatcher. Feature tasks own their registered commands and authoritative behavior.

## V1 boundary

V1 includes:

- a compact menu and separate windows for separate concerns;
- `F12` visibility toggle and hidden-at-launch behavior;
- correct gameplay-input suppression while ImGui captures input;
- one bounded development-only command/result envelope;
- admitted-session binding, deterministic dispatch and correlated results;
- a harmless Ping World command available from both a typed control and the console;
- macOS ARM64 native validation and headless-project isolation.

V1 excludes layout persistence unless trivial and separately approved, roles, permissions, administration, remote operations, permanent game UI, feature commands, and any promise of long-term protocol compatibility.