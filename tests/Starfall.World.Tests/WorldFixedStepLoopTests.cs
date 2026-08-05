using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.World.Lifecycle;

namespace Starfall.World.Tests;

public sealed class WorldFixedStepLoopTests
{
    [Fact]
    public void Finite_run_advances_exactly_the_requested_ticks()
    {
        WorldChannelRuntime runtime = CreateRunningRuntime();

        WorldRunResult result = WorldFixedStepLoop.RunFinite(runtime, 120, CancellationToken.None);

        Assert.Equal(120UL, result.TicksRun);
        Assert.Equal(120UL, runtime.CurrentTick);
        Assert.Equal(0UL, result.CatchUpClampCount);
    }

    [Fact]
    public void Finite_run_honours_preexisting_cancellation_without_advancing()
    {
        WorldChannelRuntime runtime = CreateRunningRuntime();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        WorldRunResult result = WorldFixedStepLoop.RunFinite(runtime, 120, cancellation.Token);

        Assert.Equal(0UL, result.TicksRun);
        Assert.Equal(0UL, runtime.CurrentTick);
    }

    [Fact]
    public void Accumulator_waits_until_one_complete_fixed_step()
    {
        var accumulator = new FixedStepAccumulator();
        var ticks = 0;

        FixedStepAdvanceResult before = accumulator.Advance(
            WorldFixedStepLoop.FixedStepSeconds * 0.5,
            () => ticks++);
        FixedStepAdvanceResult after = accumulator.Advance(
            WorldFixedStepLoop.FixedStepSeconds * 0.5,
            () => ticks++);

        Assert.Equal(0, before.TicksRun);
        Assert.Equal(1, after.TicksRun);
        Assert.Equal(1, ticks);
        Assert.False(before.BacklogClamped);
        Assert.False(after.BacklogClamped);
    }

    [Fact]
    public void Accumulator_caps_catch_up_and_clamps_excess_backlog_to_one_step()
    {
        var accumulator = new FixedStepAccumulator();
        var ticks = 0;

        FixedStepAdvanceResult overloaded = accumulator.Advance(
            WorldFixedStepLoop.FixedStepSeconds * 20,
            () => ticks++);
        FixedStepAdvanceResult clampedRemainder = accumulator.Advance(0, () => ticks++);

        Assert.Equal(WorldFixedStepLoop.MaximumCatchUpTicksPerCycle, overloaded.TicksRun);
        Assert.True(overloaded.BacklogClamped);
        Assert.Equal(1, clampedRemainder.TicksRun);
        Assert.False(clampedRemainder.BacklogClamped);
        Assert.Equal(WorldFixedStepLoop.MaximumCatchUpTicksPerCycle + 1, ticks);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Accumulator_rejects_invalid_elapsed_time(double elapsedSeconds)
    {
        var accumulator = new FixedStepAccumulator();

        Assert.Throws<ArgumentOutOfRangeException>(() => accumulator.Advance(elapsedSeconds, () => { }));
    }

    private static WorldChannelRuntime CreateRunningRuntime()
    {
        var runtime = new WorldChannelRuntime(
            new("world_1"),
            new("channel_1"),
            new(Guid.NewGuid()),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
        runtime.Start();
        return runtime;
    }
}
