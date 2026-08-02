# Starfall

Starfall is a server-authoritative MMORPG inspired by classic MU Online. It is an independently useful child repository in the ChronoFall project family and owns its simulation, protocol, content, presentation integration, editor and Balance Lab, build, and release lifecycle.

The repository contains the approved library boundaries, architecture tests, and bounded executable shells for the player client and authoritative world host. The shells currently prove process startup only: they print a deterministic readiness message and exit without starting gameplay, networking, rendering, or a world loop.

## Foundation commands

Run from the Starfall repository root:

```sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx --no-restore
dotnet test Starfall.slnx --no-restore --no-build
```

## Foundation process smoke

After the solution build, run the two composition roots independently:

```sh
dotnet run --project src/Starfall.World/Starfall.World.csproj --no-restore --no-build
dotnet run --project src/Starfall.Client/Starfall.Client.csproj --no-restore --no-build
```

`Starfall.World` is the headless authoritative world-server boundary; its name does not imply a client-side world or a decision to split every logical service into its own process. Both foundation shells currently accept no arguments and exit immediately after confirming startup. Later tasks own the fixed-step world lifecycle and client presentation runtime.

Read `AGENTS.md` before beginning work. Durable architecture and workflow documentation lives in Starfall's PM wiki.
