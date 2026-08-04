---
title: Client World Presentation Adapter
createdAt: 2026-08-04T07:46:57.3592710Z
modifiedAt: 2026-08-04T14:55:48.3110060Z
---

## Ownership and purpose

Starfall.Client owns one narrow snapshot/fact-to-presentation adapter for the local and connected walking milestones. CLIENT-0021 proves it with `local_technical_player`; CLIENT-0009 must translate accepted protocol snapshots into this same adapter instead of creating a second movement-presentation path.

The adapter is Client-internal. It does not change Starfall's project graph or create a shared engine, scene/entity framework, wire contract or gameplay dependency.

## Snapshot and presentation contract

The input is one validated immutable technical-player snapshot containing:

- a non-empty identity;
- a non-negative fixed tick;
- a finite ground-plane position in metres;
- a finite X/Z velocity in metres per second;
- a finite normalized X/Z facing direction.

The stateless adapter maps that one snapshot directly into an invertible rigid world transform using the shared right-handed, Y-up, local-`+Z`-forward convention. It derives only idle versus walking presentation. It does not order snapshots, validate gameplay, move an entity, interpolate, smooth, predict, reconcile, collide or navigate.

The live renderer intentionally presents only the latest completed 60 Hz fixture snapshot. Slight stepping on a higher-refresh display is accepted evidence. Any interpolation or presentation smoothing requires a later focused owner-planned task and must remain separate from authoritative movement and connected reconciliation.

## Deterministic local fixture

The local stand-in begins at `Draft0GrayboxCatalog.FirstPlayable.Town.RespawnAnchor`, currently `(100,0,25)`, facing `+Z`. Left-click remains movement intent. The fixture consumes the latest intent on later fixed ticks and moves directly toward it without collision, navigation, pathfinding, protected-town enforcement or gameplay acceptance.

Speed is stored as integer tenths of a metre per second:

- default: `40` = 4.0 m/s;
- allowed range: `1` through `120` = 0.1 through 12.0 m/s;
- numpad `+`/`-`: exact one-tenth session-local changes;
- repeated key events and main-keyboard `+`/`-` are ignored.

At 60 Hz, the default step is 1/15 metre before destination clamping. The latest destination replaces the prior destination. Arrival clamps without overshoot, produces zero velocity and retains the last facing.

These values are disposable client-fixture inputs, not Content, Balance Lab or authoritative gameplay conclusions.

## Locomotion and camera presentation

The technical UAL1 cook remains unchanged. Starfall selects its existing `Idle_Loop` and `Walk_Loop` clips and owns a small Client-local locomotion policy. State changes blend for 0.25 seconds through the shared stateless pose blender; repeated requests do not restart or finish transitions. `Walk_Loop` uses the deliberately simple presentation-only cadence `sqrt(planar speed / 1.0 m/s)`: 1.0 m/s maps to 1x, the 4.0 m/s default maps to 2x and 9.0 m/s maps to 3x. Idle remains at normal cadence and the 0.25-second blend remains wall-clock based. This reduces obvious sliding without claiming that a sped-up walk replaces later locomotion-band selection.

F1 follows the latest presented player position directly. Its distance begins at 22.5 metres; Up/Down tune it by 0.5 metre from 10.0 through 60.0 metres. F2 through F7 retain their exact fixed diagnostic cameras and ignore Up/Down. Camera tuning is session-local and changes no Content, picking plane or gameplay state.

The title displays the active view, one-decimal speed derived from integer tenths, and active camera distance.

## Capture and downstream continuity

The CLIENT-0024 seven-view capture suite remains historical graybox evidence. It supplies an explicit idle snapshot at `(100,0,100)` and the frozen 22.5-metre F1 distance through the same adapter/render path; it is not the live gameplay spawn.

`SERVER-0005` now produces encoded authoritative snapshots and corrections through a bounded in-process World exchange. `CLIENT-0009` later replaces only the deterministic fixture source with decoded accepted protocol facts, preserving this adapter and presentation path.

That Client work must not activate merely because its current PM dependencies are complete. A separately planned coordinator-owned shared transport boundary and focused Starfall adoption/wiring cycle must first provide the real client/server delivery seam. Socket choice, message framing, connection lifecycle, stale-fact handling and any reconciliation remain outside the local fixture and outside `SERVER-0005`.