---
title: Client World Presentation Adapter
createdAt: 2026-08-04T07:46:57.3592710Z
modifiedAt: 2026-08-07T08:35:56.3911720Z
---

## Ownership and purpose

Starfall.Client owns one narrow snapshot/fact-to-presentation adapter for the local and connected walking milestones. CLIENT-0021 proves it with `local_technical_player`; CLIENT-0009 translates accepted protocol snapshots into this same adapter instead of creating a second movement-presentation path.

The adapter is Client-internal. It does not change Starfall's product graph or create a shared engine, scene/entity framework, wire contract or gameplay dependency.

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

The CLIENT-0024 seven-view capture suite remains historical graybox evidence. It supplies an explicit idle snapshot at `(100,0,100)` and the frozen 22.5-metre F1 distance; connected work does not mutate its fingerprints.

`CLIENT-0009` now proves the planned reuse boundary. The connected session decodes authoritative movement snapshots/corrections and converts them to the same immutable `TechnicalPlayerSnapshot` consumed by `TechnicalPlayerPresentationAdapter`. The native renderer, locomotion selection, camera focus and graybox rendering are unchanged. Left-click sends intent instead of mutating the local fixture; the latest accepted snapshot is rendered directly with no smoothing or prediction.

Local no-argument preview remains available with its 60 Hz fixture and Numpad speed tuning. Connected mode disables that tuning, preserves F1-F7/Tab and F1 Up/Down camera controls, and reports entity, latest tick and camera distance in the title. A disconnect or protocol failure ends connected mode; window close performs a clean disconnect.

If native observation later exposes snapshot stepping, a focused Client presentation-smoothing task should interpolate between authoritative samples while preserving corrections as authoritative replacement facts. That work must not add client movement authority or make the stateless adapter responsible for network reconciliation.

## Connected Basic Arrow action presentation

`CLIENT-0007` extends the connected presentation path without changing the movement adapter. The network session retains decoded Basic Arrow accepted, rejected, canceled and resolved facts in arrival order. The native Client drains those facts after producing the current locomotion pose, then applies one Client-owned upper-body action controller before skinning and socket evaluation.

The action controller:

- treats an accepted fact as permission to begin visual notch/aim sequencing;
- preserves the authoritative start and resolve ticks and never decides whether the action succeeds;
- begins `Bow_Shoot` only after a matching resolved fact, holding aim if that fact arrives late;
- emits the reviewed 100 ms / frame 3 body-release marker exactly once for later projectile presentation;
- returns canceled actions without a release and ignores rejections for unrelated active sequences;
- layers only the `spine_01` subtree, leaving the movement-produced root, pelvis and legs unchanged;
- feeds one final pose to both the GPU skinning palette and the existing left-hand bow socket.

This remains presentation state, not protocol or gameplay state. There is no authoritative spatial arrow, projectile collision, damage decision, Fire Arrow behavior, Arrow Rain behavior, off-hand IK, aim offset or general animation graph. The no-connection local fixture and deterministic capture suite continue to use locomotion only.

## Planned authoritative projectile adoption

Completed `CLIENT-0018` extends the connected presentation path with a synthetic 150 ms frozen-target visual flight after the reviewed body release marker. It is valid historical evidence for arrow loading, nocking, detachment, rendering, impact and stale-state cleanup; it remains client-only and does not decide collision or damage.

The approved successor is `CLIENT-0037`. It will preserve that presentation work while replacing the synthetic timer with authoritative projectile spawn and terminal facts produced by `CONTENT-0017`, `SIM-0013`, `PROTOCOL-0016` and `SERVER-0017`. The Client reconstructs the straight visual trajectory from authoritative facts and presents Hit, Blocked or TravelExhausted termination. A terminal Hit is presentation evidence only; monster snapshots and tombstones remain canonical health and defeat state.

Durable planned contract: pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/gameplay/draft-0-straight-projectiles.
