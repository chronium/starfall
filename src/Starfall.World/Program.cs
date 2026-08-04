using Starfall.World.Launch;
using Starfall.World.Lifecycle;

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
            new(Guid.NewGuid()));

        try
        {
            runtime.Start();
            Console.WriteLine(
                $"STARFALL_WORLD_READY world={runtime.WorldId} channel={runtime.ChannelId} " +
                $"instance={runtime.InstanceId} tickRate={WorldFixedStepLoop.TickRateHz} state=running");

            WorldRunResult result;
            string stopReason;
            if (options.RunTicks is int runTicks)
            {
                result = WorldFixedStepLoop.RunFinite(runtime, runTicks, shutdown.Token);
                stopReason = result.TicksRun == (ulong)runTicks ? "finite" : "shutdown";
            }
            else
            {
                result = await WorldFixedStepLoop.RunRealtimeAsync(runtime, shutdown.Token);
                stopReason = "shutdown";
            }

            runtime.BeginDrain();
            Console.WriteLine(
                $"STARFALL_WORLD_DRAINING world={runtime.WorldId} channel={runtime.ChannelId} " +
                $"instance={runtime.InstanceId} ticks={runtime.CurrentTick} state=draining");

            runtime.Stop();
            Console.WriteLine(
                $"STARFALL_WORLD_STOPPED world={runtime.WorldId} channel={runtime.ChannelId} " +
                $"instance={runtime.InstanceId} ticks={runtime.CurrentTick} " +
                $"catchUpClamps={result.CatchUpClampCount} reason={stopReason} state=stopped");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"{ProcessName}: fatal world lifecycle failure: {exception.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}
