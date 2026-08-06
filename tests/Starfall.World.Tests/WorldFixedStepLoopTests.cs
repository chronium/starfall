using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Compatibility;
using Starfall.Simulation.Movement;
using Starfall.World.Admission;
using Starfall.World.Combat;
using Starfall.World.Entities;
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

    [Fact]
    public void Realtime_catch_up_exposes_each_tick_before_later_ticks_overwrite_terminal_outcomes()
    {
        WorldChannelRuntime runtime = CreateRunningRuntime();
        WorldGameplaySession session = Admit(runtime);
        MovePlayerTo(runtime, session.PlayerEntityId, new GroundPoint(100.0f, 70.0f));
        MovePlayerTo(runtime, session.PlayerEntityId, new GroundPoint(70.0f, 65.0f));
        WorldMonsterState target = Assert.Single(runtime.Monsters, static monster => monster.SpawnId == "spawn_easy_03");
        var exchange = new WorldBasicArrowExchange(runtime);
        WorldBasicArrowCommandOutcome accepted = exchange.HandleCommand(
            session.SessionId,
            ConnectedBasicArrowCodec.EncodeCommand(new BasicArrowCommand(
                new CombatCommandSequence(1),
                new Starfall.Protocol.Movement.WorldEntityId(target.EntityId.Value))));
        Assert.Equal(WorldBasicArrowCommandDisposition.Accepted, accepted.Disposition);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeAccepted(accepted.Payload, out BasicArrowAccepted? acceptedFact));

        while (runtime.CurrentTick + 3 < acceptedFact!.ResolveTick)
            runtime.Step();
        var accumulator = new FixedStepAccumulator();
        var publications = new List<WorldBasicArrowOutcomePublication>();

        FixedStepAdvanceResult result = WorldFixedStepLoop.AdvanceRealtimeCycle(
            accumulator,
            runtime,
            WorldFixedStepLoop.FixedStepSeconds * 5,
            () => publications.AddRange(exchange.CaptureResolutions(runtime.LastBasicArrowResolutions)));

        Assert.Equal(5, result.TicksRun);
        WorldBasicArrowOutcomePublication publication = Assert.Single(publications);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeResolved(publication.Payload, out BasicArrowResolved? resolved));
        Assert.Equal(acceptedFact.ResolveTick, resolved!.ResolveTick);
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

    private static WorldGameplaySession Admit(WorldChannelRuntime runtime)
    {
        const long now = 1_800_000_000_000;
        var claims = new WorldJoinTicketClaims(
            new JoinTicketId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            new CharacterId(Guid.NewGuid()),
            runtime.WorldId,
            runtime.ChannelId,
            runtime.InstanceId,
            now,
            now + 30_000);
        WorldJoinAdmissionOutcome outcome = runtime.ConsumeTicketAndCreateSession(
            claims,
            StarfallGameplayProtocol.CurrentVersion,
            now);
        GameplaySessionId sessionId = Assert.IsType<WorldJoinAccepted>(outcome.Accepted).SessionId;
        Assert.True(runtime.TryGetGameplaySession(sessionId, out WorldGameplaySession? session));
        return Assert.IsType<WorldGameplaySession>(session);
    }

    private static void MovePlayerTo(
        WorldChannelRuntime runtime,
        Starfall.Simulation.Entities.WorldEntityId playerId,
        GroundPoint destination)
    {
        Assert.Equal(GroundMovementIntentDisposition.Accepted, runtime.SubmitMovementIntent(playerId, destination));
        for (var tick = 0; tick < 2_000; tick++)
        {
            runtime.Step();
            Assert.True(runtime.TryGetPlayer(playerId, out WorldPlayerState? player));
            if (player!.Position == destination)
                return;
        }

        throw new InvalidOperationException("Connected player did not reach the combat fixture position.");
    }
}
