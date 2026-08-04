# Starfall

Starfall is a server-authoritative MMORPG inspired by classic MU Online. It is an independently useful child repository in the ChronoFall project family and owns its simulation, protocol, content, presentation integration, editor and Balance Lab, build, and release lifecycle.

The repository contains the approved library boundaries, architecture tests, a native generated-graybox and shared-character preview in the player client, and a bounded executable shell for the authoritative world host. Gameplay, networking, and the authoritative world loop remain future task-owned work.

## Foundation commands

Run from the Starfall repository root:

```sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx --no-restore
dotnet test Starfall.slnx --no-restore --no-build
```

## Foundation runtime checks

First stage the selected character-presentation cook from the ChronoFall coordinator root:

```sh
scripts/cook-character-presentation-for-client.sh \
  --project-id prj_pkIpzx0fzFD4URjvqBuYrGZF
```

After restoring and building Starfall, run the headless world shell and non-graphical client content probe independently:

```sh
dotnet run --project src/Starfall.World/Starfall.World.csproj --no-restore --no-build
dotnet run --project src/Starfall.Client/Starfall.Client.csproj --no-restore --no-build -- \
  --validate-character-content
```

Launch the persistent native SDL GPU preview with no client arguments:

```sh
dotnet run --project src/Starfall.Client/Starfall.Client.csproj --no-restore --no-build
```

The preview loads the staged Quaternius technical humanoid, presents `Idle_Loop` and `Walk_Loop`, and renders the generated Draft 0 graybox from `Draft0GrayboxCatalog.FirstPlayable`. Flat-colour presentation layers distinguish the walkable ground, protected town, round-capped routes, three camp footprints, outer boundary, exact-height collision proxies, critical anchors, and sample spawns. These generated diagnostics do not select environment assets or change Content, collision, navigation, or gameplay state.

The development window opens at 1920 x 1080. F1 follows the presented technical player; Up and Down tune that view from 10.0 through 60.0 metres in 0.5-metre steps. F2 selects the whole-zone overview, F3 the town, F4 the junction, and F5 through F7 the easy, mixed, and hard camps. Tab cycles the views; F2 through F7 remain fixed diagnostics and ignore Up/Down. Repeated key events are ignored. Number keys remain reserved for later skill controls; there is no free pan, rotation, or mouse-wheel zoom.

The live humanoid begins at the catalog respawn anchor `(100,0,25)` as `local_technical_player`, initially facing `+Z`. The deterministic stand-in advances direct-to-target movement on 60 Hz ticks at an initial 4.0 metres/second. Numpad `+` and `-` tune a session-local integer-tenths setting from 0.1 through 12.0 metres/second in exact 0.1 steps. The title bar displays the active view, speed and camera distance. The historical seven-view capture suite remains frozen to its explicit idle `(100,0,100)` CLIENT-0005 framing fixture and 22.5-metre F1 distance.

Left-click produces and logs a finite ground movement intent using the currently selected camera when the deterministic ray-to-ground result lies inside the durable 200 x 200 metre zone. Input does not directly mutate the rendered character: a clearly provisional Client-local fixture consumes the intent and emits fixed-tick snapshots through the same stateless presentation adapter reserved for later real protocol facts. It performs no collision, navigation, pathfinding, town enforcement or gameplay validation. Rendering uses only the latest completed snapshot without interpolation, smoothing, prediction or reconciliation. The technical `Walk_Loop` uses the deliberately simple square-root cadence `sqrt(speed / 1.0 m/s)`, giving 1x at 1.0 m/s and 2x at the 4.0 m/s default. This reduces obvious foot sliding without claiming to replace proper locomotion bands. Right-click and skill keys remain reserved for later connected combat-intent work. Escape or the window close control exits the preview.

The `--validate-character-content` probe loads and validates the same runtime cook without initializing SDL; unknown client arguments fail with exit code 2.

`Starfall.World` is the headless authoritative world-server boundary; its name does not imply a client-side world or a decision to split every logical service into its own process. It still exits after its bounded startup probe. Later tasks own its fixed-step world lifecycle and all gameplay and networking.

Read `AGENTS.md` before beginning work. Durable architecture and workflow documentation lives in Starfall's PM wiki.
