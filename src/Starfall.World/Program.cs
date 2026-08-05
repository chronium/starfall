using System.Security.Cryptography;
using ChronoFall.Network.Transport;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.World.Entities;
using Starfall.World.Launch;
using Starfall.World.Lifecycle;
using Starfall.World.Networking;

return await WorldProgram.RunAsync(args);

internal static class WorldProgram
{
    private const string ProcessName = "Starfall.World";

    internal static async Task<int> RunAsync(string[] arguments)
    {
        WorldLaunchOptions options;
        try
        {
            options = WorldLaunchOptions.Parse(arguments);
        }
        catch (WorldLaunchOptionsException exception)
        {
            Console.Error.WriteLine($"{ProcessName}: {exception.Message}");
            return 2;
        }

        using var shutdown = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdown.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        var runtime = new WorldChannelRuntime(
            options.WorldId,
            options.ChannelId,
            new(Guid.NewGuid()),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
        WorldConnectedWalkingNetworkHost? connectedHost = null;

        try
        {
            runtime.Start();
            if (options.IsConnected)
            {
                connectedHost = new WorldConnectedWalkingNetworkHost(
                    WorldNetworkTransportFactory.Create(),
                    runtime,
                    LoadVerificationKeys(options.VerificationKeyPaths));
                connectedHost.Start(options.ListenPort!.Value);
            }
            WorldPlayerState? technicalPlayer = options.IsConnected ? null : runtime.CreateTechnicalPlayer();
            Console.WriteLine(
                $"STARFALL_WORLD_READY world={runtime.WorldId} channel={runtime.ChannelId} " +
                $"instance={runtime.InstanceId} zone={runtime.Layout.Specification.Id} " +
                $"town={runtime.Layout.Town.Id} branches={runtime.Layout.Branches.Count} " +
                $"routes={runtime.Layout.Branches.Count + 1} proxies={runtime.Layout.Proxies.Count} " +
                $"spawns={runtime.Layout.Branches.Sum(static branch => branch.SampleSpawns.Count)} " +
                $"mode={(options.IsConnected ? "connected" : "offline")} " +
                $"listenPort={options.ListenPort?.ToString() ?? "none"} " +
                $"technicalPlayer={technicalPlayer?.EntityId.ToString() ?? "none"} players={runtime.PlayerCount} " +
                $"monsters={runtime.MonsterCount} " +
                $"tickRate={WorldFixedStepLoop.TickRateHz} state=running");

            WorldRunResult result;
            string stopReason;
            if (options.RunTicks is int runTicks)
            {
                result = WorldFixedStepLoop.RunFinite(runtime, runTicks, shutdown.Token);
                stopReason = result.TicksRun == (ulong)runTicks ? "finite" : "shutdown";
            }
            else
            {
                if (options.IsConnected)
                {
                    result = await WorldFixedStepLoop.RunRealtimeAsync(
                        runtime,
                        shutdown.Token,
                        connectedHost!.Pump);
                }
                else
                {
                    result = await WorldFixedStepLoop.RunRealtimeAsync(runtime, shutdown.Token);
                }
                stopReason = "shutdown";
            }

            runtime.BeginDrain();
            Console.WriteLine(
                $"STARFALL_WORLD_DRAINING world={runtime.WorldId} channel={runtime.ChannelId} " +
                $"instance={runtime.InstanceId} ticks={runtime.CurrentTick} " +
                $"players={runtime.PlayerCount} monsters={runtime.MonsterCount} state=draining");

            connectedHost?.Dispose();
            connectedHost = null;
            runtime.Stop();
            Console.WriteLine(
                $"STARFALL_WORLD_STOPPED world={runtime.WorldId} channel={runtime.ChannelId} " +
                $"instance={runtime.InstanceId} ticks={runtime.CurrentTick} " +
                $"players={runtime.PlayerCount} monsters={runtime.MonsterCount} " +
                $"catchUpClamps={result.CatchUpClampCount} " +
                $"reason={stopReason} state=stopped");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"{ProcessName}: fatal world lifecycle failure: {exception.Message}");
            return 1;
        }
        finally
        {
            connectedHost?.Dispose();
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static WorldJoinTicketVerificationKeyRing LoadVerificationKeys(
        IReadOnlyDictionary<string, string> paths)
    {
        var keys = new List<WorldJoinTicketVerificationKey>(paths.Count);
        foreach ((string keyId, string path) in paths)
        {
            using ECDsa key = ECDsa.Create();
            key.ImportFromPem(File.ReadAllText(path));
            keys.Add(new WorldJoinTicketVerificationKey(keyId, key.ExportSubjectPublicKeyInfo()));
        }
        return new WorldJoinTicketVerificationKeyRing(keys);
    }
}
