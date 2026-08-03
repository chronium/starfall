# Starfall

Starfall is a server-authoritative MMORPG inspired by classic MU Online. It is an independently useful child repository in the ChronoFall project family and owns its simulation, protocol, content, presentation integration, editor and Balance Lab, build, and release lifecycle.

The repository contains the approved library boundaries, architecture tests, a native shared-character preview in the player client, and a bounded executable shell for the authoritative world host. Gameplay, networking, and the authoritative world loop remain future task-owned work.

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

The preview loads the staged Quaternius humanoid, continuously samples `Idle_Loop`, and uses the provisional Draft 0 perspective-isometric camera. Its current presentation inputs are a 28-degree vertical field of view, 42-degree downward pitch, 45-degree diagonal yaw, and 22.5-metre focus distance; there is no camera pan, rotation, or zoom yet. The development window opens at 1920 x 1080.

Left-click produces and logs a finite ground movement intent when the deterministic ray-to-ground result lies inside the durable 200 x 200 metre zone. It does not move the character or decide whether movement succeeds. Right-click and skill keys remain reserved for later connected combat-intent work. Escape or the window close control exits the preview.

The `--validate-character-content` probe loads and validates the same runtime cook without initializing SDL; unknown client arguments fail with exit code 2.

`Starfall.World` is the headless authoritative world-server boundary; its name does not imply a client-side world or a decision to split every logical service into its own process. It still exits after its bounded startup probe. Later tasks own its fixed-step world lifecycle and all gameplay and networking.

Read `AGENTS.md` before beginning work. Durable architecture and workflow documentation lives in Starfall's PM wiki.
