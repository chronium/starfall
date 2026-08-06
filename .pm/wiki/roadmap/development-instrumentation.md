---
title: Development Instrumentation
createdAt: 2026-08-06T06:46:50.0961910Z
modifiedAt: 2026-08-06T15:57:45.9403710Z
---

## Deliverable

A developer can open Starfall's debug GUI, issue one harmless authoritative command through either a typed control or the console, receive the correlated result, and hide the entire GUI without affecting gameplay.

This is milestone M4. It is durable engineering instrumentation with no gameplay-protocol compatibility promise.

## Dependency path

~~~text
pm://project/prj_E7QP3LUocfY7k3PYM-EQOlqc/task/SHARED-0026
  -> CLIENT-0029  adopt shared ImGui backend in Starfall.Client
  -> CLIENT-0030  Starfall debug shell

PROTOCOL-0004 + PROTOCOL-0015
  -> PROTOCOL-0013  development-only command/result envelope

SERVER-0005 + PROTOCOL-0013
  -> SERVER-0015  admitted-session dispatcher and Ping World

CLIENT-0030 + PROTOCOL-0013 + SERVER-0015
  -> CLIENT-0031  console and typed Ping World proof
~~~

SHARED-0026, CLIENT-0029, CLIENT-0030 and PROTOCOL-0013 are complete. Starfall.Client consumes the approved caller-controlled ImGui backend only in interactive local and connected native previews. The Starfall-owned shell adds a compact `Debug` menu, independent read-only `World / Session` and `Presentation / Rendering` windows, non-repeated `F12` whole-shell visibility, and the interactive-only `--debug-ui-hidden` launch modifier. Window choices persist only in memory for the process.

The shell suppresses conflicting pointer, keyboard and text gameplay input from the backend's capture flags while leaving OS close and `F12` global. Starfall injects ImGui's bitmap default development font and records the UI last through a color-load pass after its depth-enabled scene pass. Character-content validation and the hidden deterministic graybox capture suite remain backend-free with unchanged fingerprints.

PROTOCOL-0013 established channels 7 and 8 plus the bounded request/result envelope. SERVER-0015 then removed the unconsumed availability/disabled shapes, bound every request to its admitted transport session, added per-session monotonic sequencing, installed the focused feature-handler dispatcher and registered the zero-argument `ping_world` command. Every admitted connected player may currently invoke every registered development command; no gate, role or permission machinery exists. CLIENT-0031 remains the typed and console frontend proof.

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