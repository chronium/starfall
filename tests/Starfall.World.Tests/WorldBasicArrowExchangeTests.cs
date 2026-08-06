using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Compatibility;
using Starfall.Simulation.Combat;
using Starfall.Simulation.Movement;
using Starfall.World.Admission;
using Starfall.World.Combat;
using Starfall.World.Entities;
using Starfall.World.Lifecycle;

namespace Starfall.World.Tests;

public sealed class WorldBasicArrowExchangeTests
{
    private const long NowUnixMilliseconds = 1_800_000_000_000;

    [Fact]
    public void Accepted_command_derives_actor_from_session_and_resolves_to_requester_correlation()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);
        WorldMonsterState target = PrepareCombatFixture(runtime, session);
        var exchange = new WorldBasicArrowExchange(runtime);

        WorldBasicArrowCommandOutcome outcome = exchange.HandleCommand(
            session.SessionId,
            EncodeCommand(1, target.EntityId.Value));

        Assert.Equal(WorldBasicArrowCommandDisposition.Accepted, outcome.Disposition);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeAccepted(outcome.Payload, out BasicArrowAccepted? accepted));
        Assert.NotNull(accepted);
        Assert.Equal(1UL, accepted.Sequence.Value);
        Assert.Equal(session.PlayerEntityId.Value, accepted.ActorEntityId.Value);
        Assert.Equal(target.EntityId.Value, accepted.TargetEntityId.Value);
        Assert.Equal(runtime.CurrentTick, accepted.StartTick);
        Assert.Equal(runtime.CurrentTick + Draft0BasicArrowTuning.Draft0ResolveDelayTicks, accepted.ResolveTick);

        StepTo(runtime, accepted.ResolveTick);
        WorldBasicArrowOutcomePublication publication = Assert.Single(
            exchange.CaptureResolutions(runtime.LastBasicArrowResolutions));
        Assert.Equal(session.SessionId, publication.SessionId);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeResolved(publication.Payload, out BasicArrowResolved? resolved));
        Assert.NotNull(resolved);
        Assert.Equal(300, resolved.RequestedDamageUnits);
        Assert.Equal(300, resolved.EffectiveDamageUnits);
        Assert.False(resolved.TargetDefeated);
        Assert.True(runtime.TryGetMonster(target.EntityId, out WorldMonsterState? damaged));
        Assert.Equal(400, damaged!.HealthUnits);
    }

    [Fact]
    public void Protected_town_rejection_consumes_sequence_and_stale_or_duplicate_commands_are_silent()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);
        WorldMonsterState target = runtime.Monsters[0];
        var exchange = new WorldBasicArrowExchange(runtime);

        WorldBasicArrowCommandOutcome rejected = exchange.HandleCommand(
            session.SessionId,
            EncodeCommand(5, target.EntityId.Value));
        WorldBasicArrowCommandOutcome duplicate = exchange.HandleCommand(
            session.SessionId,
            EncodeCommand(5, target.EntityId.Value));
        WorldBasicArrowCommandOutcome stale = exchange.HandleCommand(
            session.SessionId,
            EncodeCommand(4, target.EntityId.Value));

        Assert.Equal(WorldBasicArrowCommandDisposition.Rejected, rejected.Disposition);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeRejected(rejected.Payload, out BasicArrowRejected? fact));
        Assert.Equal(BasicArrowRejectionReason.ActorInProtectedTown, fact!.Reason);
        Assert.Equal(WorldBasicArrowCommandDisposition.StaleOrDuplicate, duplicate.Disposition);
        Assert.Null(duplicate.Payload);
        Assert.Equal(WorldBasicArrowCommandDisposition.StaleOrDuplicate, stale.Disposition);
        Assert.Null(stale.Payload);
    }

    [Fact]
    public void Movement_cancellation_uses_the_exact_pending_command_correlation_and_clears_it()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);
        WorldMonsterState target = PrepareCombatFixture(runtime, session);
        var exchange = new WorldBasicArrowExchange(runtime);
        Assert.Equal(
            WorldBasicArrowCommandDisposition.Accepted,
            exchange.HandleCommand(session.SessionId, EncodeCommand(3, target.EntityId.Value)).Disposition);

        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            runtime.SubmitMovementIntent(session.PlayerEntityId, new GroundPoint(70.0f, 70.0f)));
        WorldBasicArrowOutcomePublication publication = Assert.Single(
            exchange.CaptureResolutions(runtime.LastBasicArrowResolutions));

        Assert.True(ConnectedBasicArrowCodec.TryDecodeCanceled(publication.Payload, out BasicArrowCanceled? canceled));
        Assert.NotNull(canceled);
        Assert.Equal(3UL, canceled.Sequence.Value);
        Assert.Equal(BasicArrowCancellationReason.CanceledByMovement, canceled.Reason);
        Assert.Empty(exchange.CaptureResolutions([]));
    }

    [Fact]
    public void Malformed_unknown_and_removed_sessions_are_bounded_and_isolated()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession first = Admit(runtime, 1);
        WorldGameplaySession second = Admit(runtime, 2);
        var exchange = new WorldBasicArrowExchange(runtime);

        Assert.Equal(
            WorldBasicArrowCommandDisposition.MalformedPayload,
            exchange.HandleCommand(first.SessionId, [1, 2, 3]).Disposition);
        Assert.Equal(
            WorldBasicArrowCommandDisposition.UnknownSession,
            exchange.HandleCommand(
                new GameplaySessionId(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")),
                EncodeCommand(1, runtime.Monsters[0].EntityId.Value)).Disposition);

        exchange.RemoveSession(first.SessionId);
        WorldBasicArrowCommandOutcome secondOutcome = exchange.HandleCommand(
            second.SessionId,
            EncodeCommand(1, runtime.Monsters[0].EntityId.Value));
        Assert.Equal(WorldBasicArrowCommandDisposition.Rejected, secondOutcome.Disposition);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeRejected(secondOutcome.Payload, out BasicArrowRejected? rejection));
        Assert.Equal(second.PlayerEntityId.Value, rejection!.ActorEntityId.Value);
    }

    private static WorldChannelRuntime CreateRuntime()
    {
        var runtime = new WorldChannelRuntime(
            new WorldId("world_1"),
            new ChannelId("channel_1"),
            new WorldInstanceId(Guid.Parse("40000000-0000-0000-0000-000000000001")),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
        runtime.Start();
        return runtime;
    }

    private static WorldGameplaySession Admit(WorldChannelRuntime runtime, int ordinal)
    {
        var claims = new WorldJoinTicketClaims(
            new JoinTicketId(Guid.Parse($"10000000-0000-0000-0000-{ordinal:D12}")),
            new AccountId(Guid.Parse($"20000000-0000-0000-0000-{ordinal:D12}")),
            new CharacterId(Guid.Parse($"30000000-0000-0000-0000-{ordinal:D12}")),
            runtime.WorldId,
            runtime.ChannelId,
            runtime.InstanceId,
            NowUnixMilliseconds,
            NowUnixMilliseconds + 30_000);
        WorldJoinAdmissionOutcome outcome = runtime.ConsumeTicketAndCreateSession(
            claims,
            StarfallGameplayProtocol.CurrentVersion,
            NowUnixMilliseconds);
        GameplaySessionId sessionId = Assert.IsType<WorldJoinAccepted>(outcome.Accepted).SessionId;
        Assert.True(runtime.TryGetGameplaySession(sessionId, out WorldGameplaySession? session));
        return Assert.IsType<WorldGameplaySession>(session);
    }

    private static WorldMonsterState PrepareCombatFixture(
        WorldChannelRuntime runtime,
        WorldGameplaySession session)
    {
        MovePlayerTo(runtime, session.PlayerEntityId, new GroundPoint(100.0f, 70.0f));
        MovePlayerTo(runtime, session.PlayerEntityId, new GroundPoint(70.0f, 65.0f));
        return Assert.Single(runtime.Monsters, static monster => monster.SpawnId == "spawn_easy_03");
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

    private static void StepTo(WorldChannelRuntime runtime, ulong targetTick)
    {
        while (runtime.CurrentTick < targetTick)
            runtime.Step();
    }

    private static byte[] EncodeCommand(ulong sequence, ulong target) =>
        ConnectedBasicArrowCodec.EncodeCommand(new BasicArrowCommand(
            new CombatCommandSequence(sequence),
            new Starfall.Protocol.Movement.WorldEntityId(target)));
}
