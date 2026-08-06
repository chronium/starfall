---
id: CLIENT-0031
title: Add the development command console
track: CLIENT
milestone: M4
dependsOn:
- CLIENT-0030
- PROTOCOL-0013
- SERVER-0015
createdAt: 2026-08-06T06:41:23.9937500Z
modifiedAt: 2026-08-06T16:45:51.2948490Z
---

Add a bounded Minecraft-like ImGui development command console opened with T, with retained history, transparent fading output, admitted-session request/result correlation, and the canonical zero-argument ping command. Keep F12 as the master debug-UI visibility control and preserve the existing read-only diagnostic windows. Remove the obsolete ping_world identity and typed-button frontend rather than retaining aliases or compatibility paths. Prove native macOS behavior and preserve headless isolation.

## Notes

- 2026-08-06 16:45 UTC - Implemented and owner-validated the bounded Starfall development command console.

  Scope and contracts:
  - Added the Minecraft-like ImGui console opened by non-repeated T while the master debug shell is visible. F12 and --debug-ui-hidden remain the master visibility boundary; open console input captures gameplay keyboard and pointer input.
  - Added bounded 584-byte printable-ASCII parsing, canonical space-separated command and argument handling, 32-entry attempt history, 128-entry transcript, Up/Down history navigation, Enter submit/close and Escape cancel/close.
  - Added the transparent closed transcript: at most six recent lines hold for 10 seconds and fade over 2 seconds without background or hit testing.
  - Extended the production connected Client session with checked monotonic development-command sequences, exact command/result correlation, reliable-ordered channels 7/8, and a maximum of 64 outstanding-or-unconsumed lifecycles. Valid authoritative rejections remain console results; malformed, misdelivered, duplicate or inconsistent results remain protocol violations.
  - Renamed the only registered command to canonical ping and removed the earlier handler/identity rather than retaining an alias or compatibility path. The World returns the existing session-bound pong diagnostic.
  - Local preview accepts console input and reports that a connected World session is required. No typed command button, command discovery, permissions, administration, scripting, filesystem access or stable gameplay compatibility contract was added.
  - Preserved the existing read-only World / Session and Presentation / Rendering windows and headless/presentation dependency boundaries.

  Validation:
  - Debug solution build: succeeded before final Release validation.
  - Debug solution suites: 466 tests passed.
  - Release solution build: 0 warnings, 0 errors.
  - Release solution suites: all 466 tests passed. The first aggregate Release run encountered two concurrent Box3D native-lifetime failures in existing World tests; the complete 115-test World suite passed immediately in isolated rerun.
  - Real UDP coverage uses the production ConnectedWalkingClientSession to send ping and consume its correlated authoritative pong.
  - Character-content probe: quaternius-ual1-standard, 65 joints, Idle_Loop/Walk_Loop/Sword_Attack.
  - Hidden seven-view capture suite retained exact fingerprints: e668208dea46f904, 9b08f56034585eaf, 8391b2cb4c089c7e, 31512e8b5b2094c0, e5beeedc16cd627f, 0a4f95e2d861170d, a017df7907dcbf97.
  - PM doctor passed; family inspection reported all three projects available, readable and write-trusted with zero warnings.

  Owner native validation on macOS ARM64:
  - Confirmed T open/focus, canonical ping and correlated pong, transparent hold/fade output, history navigation, Escape close without application exit, F12 master hide, T suppression while hidden, resizing and unaffected gameplay input.
  - Confirmed the local no-World path reports the required connected-session diagnostic.
  - Owner accepted the behavior and chose to preserve the transparent closed-console checkpoint as shown.

  Visual checkpoint:
  - docs/project-history/2026-08-06-connected-development-console/connected-development-console.png
  - 2,032 by 1,220, 8-bit RGBA PNG, non-interlaced
  - SHA-256 7940abe379f82f9948e6625d57f1e36b10ffd95e91469c1abd9b5310b2f21040