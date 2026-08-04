# Starfall

Starfall is a server-authoritative MMORPG inspired by classic MU Online. It is an independently useful child repository in the ChronoFall project family and owns its simulation, protocol, content, presentation integration, editor and Balance Lab, build, and release lifecycle.

The repository contains the approved library boundaries, architecture tests, a native generated-graybox and shared-character player client, and a headless 60 Hz world/channel lifecycle with a provisional loaded zone. The World runtime owns generic technical players, bounded authoritative click-to-move, graybox collision, signed admission sessions, and the first loopback connected-walking exchange.

## Foundation commands

Run from the Starfall repository root:

```sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx --no-restore
dotnet test Starfall.slnx --no-restore --no-build
```

The authoritative Simulation consumes the coordinator-owned Box3D source boundary. From the ChronoFall family root, prepare the pinned macOS ARM64 native artifact before the Starfall build when it is not already present:

```sh
sh thirdparty/build-box3d-macos.sh
```

Linux x64 uses `thirdparty/build-box3d-linux.sh`. Windows and standalone package/feed distribution remain deferred.

## Foundation runtime checks

First stage the selected character-presentation cook from the ChronoFall coordinator root:

```sh
scripts/cook-character-presentation-for-client.sh \
  --project-id prj_pkIpzx0fzFD4URjvqBuYrGZF
```

After restoring and building Starfall, run a deterministic one-second headless world validation and the non-graphical client content probe independently:

```sh
dotnet run --project src/Starfall.World/Starfall.World.csproj --no-restore --no-build -- \
  --world world_1 --channel channel_1 --run-ticks 60
dotnet run --project src/Starfall.Client/Starfall.Client.csproj --no-restore --no-build -- \
  --validate-character-content
```

Run the same empty world/channel in real time until Ctrl+C with:

```sh
dotnet run --project src/Starfall.World/Starfall.World.csproj --no-restore --no-build -- \
  --world world_1 --channel channel_1
```

Both identities are required and use the Protocol contract: 1-64 lowercase ASCII letters, digits or underscores, beginning with a letter. The host creates a fresh world-instance identity and binds the immutable `Draft0GrayboxCatalog.FirstPlayable` input before entering `Running`. It then creates one generic technical player at the catalog town respawn anchor, with zero velocity and `+Z` facing. Its `READY` diagnostic reports that entity's world-local identity, player count, exact zone/town identities and stable branch, route, proxy and spawn counts. It advances only fixed 60 Hz integer ticks before reporting `DRAINING` with the retained player and `STOPPED` after lifecycle-owned player state is cleared. A real-time host caps catch-up at five ticks per outer-loop cycle and reports any backlog clamps. The finite mode advances exactly the requested positive tick count without wall-clock pacing.

`Starfall.World` also owns a narrow admission exchange over the existing signed-ticket Protocol facts. A running world validates a ticket against locally supplied public keys, consumes its ticket ID once, creates one generic player at the configured town respawn anchor, and binds that immutable world-local entity identity to the transport peer and in-memory gameplay session. The session then accepts encoded connected-walking commands and publishes encoded snapshots or authoritative corrections without calling identity, chat, or operations. Draining rejects new joins while retaining admitted sessions, players, command handling and publication; disconnect terminates only that peer's session/player/movement state; stopping clears all lifecycle-owned state.

For the first connected development run, create an ignored P-256 key pair:

```sh
dotnet run --project tools/Starfall.DevelopmentAdmission/Starfall.DevelopmentAdmission.csproj --no-restore --no-build -- \
  generate-key --key-id development --output-directory artifacts/development-admission
```

Start the World on a loopback development port and copy the fresh `instance=` value from `STARFALL_WORLD_READY`:

```sh
dotnet run --project src/Starfall.World/Starfall.World.csproj --no-restore --no-build -- \
  --world world_1 --channel channel_1 --listen-port 7777 \
  --verification-key development=artifacts/development-admission/development.public.pem
```

In another terminal, issue a one-minute ticket for that exact instance and launch the connected Client:

```sh
dotnet run --project tools/Starfall.DevelopmentAdmission/Starfall.DevelopmentAdmission.csproj --no-restore --no-build -- \
  issue-ticket --key-id development --key-directory artifacts/development-admission \
  --world world_1 --channel channel_1 --world-instance <instance-guid> \
  --output artifacts/development-admission/world_1-channel_1.ticket
dotnet run --project src/Starfall.Client/Starfall.Client.csproj --no-restore --no-build -- \
  --connect-address 127.0.0.1 --connect-port 7777 \
  --join-ticket-file artifacts/development-admission/world_1-channel_1.ticket
```

The development admission tool never prints raw tickets or private keys and refuses to overwrite its outputs. The World receives only the public key. Connected plaintext transport is deliberately restricted to literal loopback peers; protected non-loopback transport and production key provisioning remain future task-owned decisions.

Launch the persistent native SDL GPU preview with no client arguments:

```sh
dotnet run --project src/Starfall.Client/Starfall.Client.csproj --no-restore --no-build
```

The preview loads the staged Quaternius technical humanoid, presents `Idle_Loop` and `Walk_Loop`, and renders the generated Draft 0 graybox from `Draft0GrayboxCatalog.FirstPlayable`. Flat-colour presentation layers distinguish the walkable ground, protected town, round-capped routes, three camp footprints, outer boundary, exact-height collision proxies, critical anchors, and sample spawns. These generated diagnostics do not select environment assets or change Content, collision, navigation, or gameplay state.

The development window opens at 1920 x 1080. F1 follows the presented technical player; Up and Down tune that view from 10.0 through 60.0 metres in 0.5-metre steps. F2 selects the whole-zone overview, F3 the town, F4 the junction, and F5 through F7 the easy, mixed, and hard camps. Tab cycles the views; F2 through F7 remain fixed diagnostics and ignore Up/Down. Repeated key events are ignored. Number keys remain reserved for later skill controls; there is no free pan, rotation, or mouse-wheel zoom.

The live humanoid begins at the catalog respawn anchor `(100,0,25)` as `local_technical_player`, initially facing `+Z`. The deterministic stand-in advances direct-to-target movement on 60 Hz ticks at an initial 4.0 metres/second. Numpad `+` and `-` tune a session-local integer-tenths setting from 0.1 through 12.0 metres/second in exact 0.1 steps. The title bar displays the active view, speed and camera distance. The historical seven-view capture suite remains frozen to its explicit idle `(100,0,100)` CLIENT-0005 framing fixture and 22.5-metre F1 distance.

Left-click produces and logs a finite ground movement intent using the currently selected camera when the deterministic ray-to-ground result lies inside the durable 200 x 200 metre zone. In local preview mode, a clearly provisional Client-local fixture consumes the intent and emits fixed-tick snapshots through the same stateless presentation adapter used by connected protocol facts. It performs no collision, navigation, pathfinding, town enforcement or gameplay validation. In connected mode, left-click sends sequenced ground intent only; the World owns validation, collision and movement and the Client renders only the latest accepted snapshot or correction without interpolation, smoothing, prediction or reconciliation. Numpad speed tuning is disabled in connected mode. The technical `Walk_Loop` uses the deliberately simple square-root cadence `sqrt(speed / 1.0 m/s)`, giving 1x at 1.0 m/s and 2x at the 4.0 m/s default. This reduces obvious foot sliding without claiming to replace proper locomotion bands. Right-click and skill keys remain reserved for later connected combat-intent work. Escape or the window close control exits the preview.

The `--validate-character-content` probe loads and validates the same runtime cook without initializing SDL; unknown client arguments fail with exit code 2.

`Starfall.World` is the headless authoritative world-server boundary; its name does not imply a client-side world or a decision to split every logical service into its own process. It owns the provisional Draft 0 graybox input together with world/channel identity, lifecycle, fixed-step scheduler and world-local technical-player state. Simulation consumes finite ground intent at 60 Hz, moves a 0.35 m radius by 1.8 m tall capsule at the provisional 4.0 m/s speed, and stops and clears its destination at the first Box3D obstacle hit. The four walkable-boundary strips and seven catalog proxies are collision; the protected town remains traversable by players. Routes are not paths, and player-to-player collision is deferred. The layout remains validated immutable Content rather than a serialized map or general scene format. Admitted sessions bind to their own generic player and exchange connected-walking payloads over the approved shared transport. Combat, monsters and broader gameplay remain later task-owned work.

Read `AGENTS.md` before beginning work. Durable architecture and workflow documentation lives in Starfall's PM wiki.
