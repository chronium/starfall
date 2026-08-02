# Starfall

Starfall is a server-authoritative MMORPG inspired by classic MU Online. It is an independently useful child repository in the ChronoFall project family and owns its simulation, protocol, content, presentation integration, editor and Balance Lab, build, and release lifecycle.

The repository currently contains library boundaries and architecture tests only. Runnable client and world-host shells belong to PM task `BUILD-0003`.

## Foundation commands

Run from the Starfall repository root:

```sh
dotnet restore Starfall.slnx
dotnet build Starfall.slnx --no-restore
dotnet test Starfall.slnx --no-restore --no-build
```

Read `AGENTS.md` before beginning work. Durable architecture and workflow documentation lives in Starfall's PM wiki.
