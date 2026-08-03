---
title: Draft 0 Graybox Capture Suite
createdAt: 2026-08-03T15:53:33.6569670Z
modifiedAt: 2026-08-03T17:08:13.6378870Z
---

## Purpose and ownership

`CLIENT-0024` gives Starfall.Client a deterministic review path for the generated Draft 0 graybox. Starfall owns its seven camera presets, technical humanoid fixture, scene composition, fixed animation sample, filenames and output policy. It consumes the bounded coordinator screenshot contract through the already-approved `ChronoFall.CharacterPresentation.SdlGpu` family source reference.

The coordinator helper owns only one-shot SDL GPU texture readback, RGBA/BGRA normalization and PNG encoding. It does not own Starfall's window, device, command acquisition, render pass, target, camera, animation state or capture schedule. Starfall never references Royale.

## Command and fixed recipe

From the Starfall checkout inside the canonical family checkout:

```sh
dotnet run --project src/Starfall.Client/Starfall.Client.csproj \
  --no-restore --no-build -- \
  --capture-graybox-suite artifacts/CLIENT-0024/run-a
```

The command creates a hidden SDL window only to establish the existing Starfall device/shader environment. It renders directly into a 1,920 by 1,080 caller-owned GPU texture through the same `RecordFrame` path as the interactive preview. It does not capture the desktop, window chrome or another process and performs no focus/input automation.

All frames sample `Idle_Loop` at exactly 0.500 seconds. This freezes technical presentation evidence; animation remains client-owned and has no gameplay authority.

| Order | Preset | File |
| ---: | --- | --- |
| 1 | player-fixture | `01-player-fixture.png` |
| 2 | overview | `02-overview.png` |
| 3 | town | `03-town.png` |
| 4 | junction | `04-junction.png` |
| 5 | easy-camp | `05-easy-camp.png` |
| 6 | mixed-camp | `06-mixed-camp.png` |
| 7 | hard-camp | `07-hard-camp.png` |

Before writing, Starfall verifies exact dimensions, fully opaque RGBA pixels, non-flat content and a distinct fingerprint for every view. PNG output goes only to the explicit caller-selected directory and is never added to a runtime manifest. Raw runs and temporary sheets remain ignored.

## Deterministic native evidence

Two independent native macOS ARM64 Metal runs produced identical runtime fingerprints and byte-identical PNG files:

| Preset | Runtime fingerprint | PNG SHA-256 |
| --- | --- | --- |
| player-fixture | `e668208dea46f904` | `4a8d34344c74ae091888d3d55c7893b560635998298d387569d8281bbea49dfd` |
| overview | `879e493f70918db3` | `b286279ed048763c9c7e01e2f1a2110e713f6587e486e713667a2560dde6071a` |
| town | `8391b2cb4c089c7e` | `87e22d91c8663777a9d0bde1b451f47da6c6f22cf62bb9997593d5e20cef7005` |
| junction | `7780b7ed19deddf0` | `af0ff7b9a1d31a53cec06fce7650bd54040cd85745c8949806c0cc5051c80219` |
| easy-camp | `1739773ce12c5706` | `9c7d57c1f2d842450c835969c632f1d24a3d3593dce2f0a3848d70a990619a47` |
| mixed-camp | `2e9c7b1a6ac42fe8` | `8085597a5316ff4c480f11516fddc37457aa4bc2d92fc8a307ceb223e4fd91a8` |
| hard-camp | `8c2a0b49728853a1` | `ea4223936636502475213f8b9bb9a98437f5cf5ffa3ac2e937f2ac9c76f47260` |

The corrected coordinator compositor arranges these exact PNGs into a labeled four-by-two 7,680 by 2,256 review sheet. The empty eighth cell is intentional. Permanent retention remains an explicit owner decision under the Starfall visual-checkpoint workflow.

The owner accepted the native framing and explicitly approved permanent retention on 2026-08-03. Starfall preserves only the curated sheet at `docs/project-history/2026-08-03-draft-0-graybox-capture/contact-sheet.png` (7,680 by 2,256 RGBA PNG; SHA-256 `7b94a01f3b62255c3450f205311252dd444b62a77d19fa5ec01e2cf3dd847095`). The seven raw captures remain ignored.

## Boundaries

Capture does not mutate Content coordinates, the technical humanoid fixture, presentation-only Y offsets, camera framing, picking, movement intent or authoritative state. Client is the only Starfall project that consumes SDL GPU/PNG code. World, Simulation, BalanceLab, Content, Protocol and Editor remain presentation-free.

This task adds no image decoder, asynchronous thumbnail queue, editor catalogue, video capture, render graph, scene framework, runtime manifest or Royale migration.