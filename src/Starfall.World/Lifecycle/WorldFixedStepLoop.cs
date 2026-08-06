using System.Diagnostics;

namespace Starfall.World.Lifecycle;

internal readonly record struct WorldRunResult(
    ulong TicksRun,
    ulong CatchUpClampCount);

internal readonly record struct FixedStepAdvanceResult(
    int TicksRun,
    bool BacklogClamped);

internal sealed class FixedStepAccumulator
{
    private double _accumulatedSeconds;

    internal FixedStepAdvanceResult Advance(double elapsedSeconds, Action tick)
    {
        ArgumentNullException.ThrowIfNull(tick);
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedSeconds),
                elapsedSeconds,
                "Elapsed time must be finite and non-negative.");
        }

        _accumulatedSeconds += elapsedSeconds;
        var ticksRun = 0;
        while (_accumulatedSeconds >= WorldFixedStepLoop.FixedStepSeconds &&
            ticksRun < WorldFixedStepLoop.MaximumCatchUpTicksPerCycle)
        {
            tick();
            _accumulatedSeconds -= WorldFixedStepLoop.FixedStepSeconds;
            ticksRun++;
        }

        bool backlogClamped = _accumulatedSeconds >= WorldFixedStepLoop.FixedStepSeconds;
        if (backlogClamped)
            _accumulatedSeconds = WorldFixedStepLoop.FixedStepSeconds;

        return new(ticksRun, backlogClamped);
    }
}

internal static class WorldFixedStepLoop
{
    internal const int TickRateHz = 60;
    internal const int MaximumCatchUpTicksPerCycle = 5;
    internal const double FixedStepSeconds = 1.0 / TickRateHz;

    internal static FixedStepAdvanceResult AdvanceRealtimeCycle(
        FixedStepAccumulator accumulator,
        WorldChannelRuntime runtime,
        double elapsedSeconds,
        Action? postTick = null)
    {
        ArgumentNullException.ThrowIfNull(accumulator);
        ArgumentNullException.ThrowIfNull(runtime);
        return accumulator.Advance(elapsedSeconds, () =>
        {
            runtime.Step();
            postTick?.Invoke();
        });
    }

    internal static WorldRunResult RunFinite(
        WorldChannelRuntime runtime,
        int runTicks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (runTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(runTicks));

        ulong startingTick = runtime.CurrentTick;
        for (var index = 0; index < runTicks && !cancellationToken.IsCancellationRequested; index++)
            runtime.Step();

        return new(runtime.CurrentTick - startingTick, 0);
    }

    internal static async Task<WorldRunResult> RunRealtimeAsync(
        WorldChannelRuntime runtime,
        CancellationToken cancellationToken,
        Action? postTick = null,
        Action? postCycle = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var stopwatch = Stopwatch.StartNew();
        TimeSpan previous = stopwatch.Elapsed;
        ulong startingTick = runtime.CurrentTick;
        ulong catchUpClampCount = 0;
        var accumulator = new FixedStepAccumulator();

        while (!cancellationToken.IsCancellationRequested)
        {
            TimeSpan current = stopwatch.Elapsed;
            double elapsedSeconds = (current - previous).TotalSeconds;
            previous = current;

            FixedStepAdvanceResult advance = AdvanceRealtimeCycle(
                accumulator,
                runtime,
                elapsedSeconds,
                postTick);
            if (advance.BacklogClamped)
                catchUpClampCount = checked(catchUpClampCount + 1);

            postCycle?.Invoke();

            if (advance.TicksRun == 0)
            {
                try
                {
                    await Task.Delay(1, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        return new(runtime.CurrentTick - startingTick, catchUpClampCount);
    }
}
