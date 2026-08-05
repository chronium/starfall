using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Movement;
using Starfall.World.Admission;
using Starfall.World.Lifecycle;
using Starfall.World.Movement;

namespace Starfall.World.Tests;

public sealed class WorldWalkingExchangeTests
{
    private const long NowUnixMilliseconds = 1_800_000_000_000;

    [Fact]
    public void Initial_capture_publishes_exact_bound_state_once_at_admission_tick()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);
        var exchange = new WorldWalkingExchange(runtime);

        WorldWalkingSnapshotPublication publication = Assert.Single(exchange.CaptureSnapshots());
        PlayerMovementSnapshot snapshot = DecodeSnapshot(publication);

        Assert.Equal(session.SessionId, publication.SessionId);
        Assert.Equal(1UL, snapshot.Sequence.Value);
        Assert.Equal(0UL, snapshot.SimulationTick);
        Assert.Equal(session.PlayerEntityId.Value, snapshot.EntityId.Value);
        Assert.Equal(runtime.Layout.Town.RespawnAnchor.XMetres, snapshot.Position.XMetres);
        Assert.Equal(runtime.Layout.Town.RespawnAnchor.ZMetres, snapshot.Position.ZMetres);
        Assert.Equal(0, BitConverter.SingleToInt32Bits(snapshot.VelocityMetresPerSecond.X));
        Assert.Equal(0, BitConverter.SingleToInt32Bits(snapshot.VelocityMetresPerSecond.Y));
        Assert.Null(snapshot.LastProcessedIntentSequence);
        Assert.Empty(exchange.CaptureSnapshots());
    }

    [Fact]
    public void Accepted_command_moves_only_bound_player_and_is_acknowledged_next_tick()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession first = Admit(runtime, 1);
        WorldGameplaySession second = Admit(runtime, 2);
        var exchange = new WorldWalkingExchange(runtime);
        Assert.Equal(2, exchange.CaptureSnapshots().Count);

        WorldWalkingCommandOutcome outcome = exchange.HandleCommand(
            first.SessionId,
            EncodeCommand(1, 104.0f, 25.0f));

        Assert.Equal(WorldWalkingCommandDisposition.Accepted, outcome.Disposition);
        Assert.Null(outcome.CorrectionPayload);
        Assert.Empty(exchange.CaptureSnapshots());

        runtime.Step();
        IReadOnlyList<WorldWalkingSnapshotPublication> publications = exchange.CaptureSnapshots();
        Assert.Equal(2, publications.Count);
        PlayerMovementSnapshot firstSnapshot = DecodeSnapshot(
            Assert.Single(publications, publication => publication.SessionId == first.SessionId));
        PlayerMovementSnapshot secondSnapshot = DecodeSnapshot(
            Assert.Single(publications, publication => publication.SessionId == second.SessionId));

        Assert.Equal(1UL, firstSnapshot.LastProcessedIntentSequence?.Value);
        Assert.True(firstSnapshot.Position.XMetres > 100.0f);
        Assert.Null(secondSnapshot.LastProcessedIntentSequence);
        Assert.Equal(100.0f, secondSnapshot.Position.XMetres);
    }

    [Fact]
    public void Newer_sequence_gaps_are_accepted_while_stale_and_duplicate_commands_are_ignored()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);
        var exchange = new WorldWalkingExchange(runtime);

        Assert.Equal(
            WorldWalkingCommandDisposition.Accepted,
            exchange.HandleCommand(session.SessionId, EncodeCommand(5, 104.0f, 25.0f)).Disposition);
        Assert.Equal(
            WorldWalkingCommandDisposition.StaleOrDuplicate,
            exchange.HandleCommand(session.SessionId, EncodeCommand(5, 110.0f, 25.0f)).Disposition);
        Assert.Equal(
            WorldWalkingCommandDisposition.StaleOrDuplicate,
            exchange.HandleCommand(session.SessionId, EncodeCommand(4, 110.0f, 25.0f)).Disposition);

        PlayerMovementSnapshot snapshot = DecodeSnapshot(Assert.Single(exchange.CaptureSnapshots()));
        Assert.Equal(5UL, snapshot.LastProcessedIntentSequence?.Value);
    }

    [Theory]
    [InlineData(1UL, 0.0f, 25.0f)]
    [InlineData(2UL, 87.0f, 20.0f)]
    public void Authoritative_rejection_returns_one_matching_correction_without_state_change(
        ulong sequence,
        float destinationX,
        float destinationZ)
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);
        var exchange = new WorldWalkingExchange(runtime);

        WorldWalkingCommandOutcome outcome = exchange.HandleCommand(
            session.SessionId,
            EncodeCommand(sequence, destinationX, destinationZ));

        Assert.Equal(WorldWalkingCommandDisposition.Corrected, outcome.Disposition);
        Assert.NotNull(outcome.CorrectionPayload);
        Assert.True(ConnectedWalkingCodec.TryDecodeCorrection(
            outcome.CorrectionPayload,
            out PlayerMovementCorrection? correction));
        Assert.NotNull(correction);
        Assert.Equal(sequence, correction.CorrectedIntentSequence.Value);
        Assert.Equal(sequence, correction.AuthoritativeSnapshot.LastProcessedIntentSequence?.Value);
        Assert.Equal(100.0f, correction.AuthoritativeSnapshot.Position.XMetres);
        Assert.Equal(25.0f, correction.AuthoritativeSnapshot.Position.ZMetres);
        Assert.Empty(exchange.CaptureSnapshots());
    }

    [Fact]
    public void Malformed_payload_and_unknown_session_are_bounded_and_do_not_consume_sequences()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);
        var exchange = new WorldWalkingExchange(runtime);

        Assert.Equal(
            WorldWalkingCommandDisposition.MalformedPayload,
            exchange.HandleCommand(session.SessionId, [1, 2, 3]).Disposition);
        Assert.Equal(
            WorldWalkingCommandDisposition.UnknownSession,
            exchange.HandleCommand(
                new GameplaySessionId(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")),
                EncodeCommand(1, 104.0f, 25.0f)).Disposition);

        PlayerMovementSnapshot snapshot = DecodeSnapshot(Assert.Single(exchange.CaptureSnapshots()));
        Assert.Equal(1UL, snapshot.Sequence.Value);
        Assert.Null(snapshot.LastProcessedIntentSequence);
    }

    [Fact]
    public void Capture_orders_sessions_by_player_identity_and_publishes_only_latest_tick()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession first = Admit(runtime, 1);
        WorldGameplaySession second = Admit(runtime, 2);
        var exchange = new WorldWalkingExchange(runtime);

        runtime.Step();
        runtime.Step();
        runtime.Step();
        IReadOnlyList<WorldWalkingSnapshotPublication> publications = exchange.CaptureSnapshots();

        Assert.Equal([first.SessionId, second.SessionId], publications.Select(static value => value.SessionId));
        Assert.All(publications, publication =>
        {
            PlayerMovementSnapshot snapshot = DecodeSnapshot(publication);
            Assert.Equal(1UL, snapshot.Sequence.Value);
            Assert.Equal(3UL, snapshot.SimulationTick);
        });
        Assert.Empty(exchange.CaptureSnapshots());
    }

    [Fact]
    public void Draining_keeps_exchange_alive_and_stopping_clears_it()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);
        var exchange = new WorldWalkingExchange(runtime);
        Assert.Single(exchange.CaptureSnapshots());

        runtime.BeginDrain();
        Assert.Equal(
            WorldWalkingCommandDisposition.Accepted,
            exchange.HandleCommand(session.SessionId, EncodeCommand(1, 101.0f, 25.0f)).Disposition);
        runtime.Step();
        Assert.Single(exchange.CaptureSnapshots());

        runtime.Stop();
        Assert.Empty(exchange.CaptureSnapshots());
        Assert.Equal(
            WorldWalkingCommandDisposition.UnknownSession,
            exchange.HandleCommand(session.SessionId, EncodeCommand(2, 102.0f, 25.0f)).Disposition);
    }

    [Fact]
    public void Session_bound_players_cannot_be_removed_through_the_technical_seam()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => runtime.RemovePlayer(session.PlayerEntityId));

        Assert.Contains("session-bound player", exception.Message, StringComparison.Ordinal);
        Assert.True(runtime.TryGetPlayer(session.PlayerEntityId, out _));
    }

    [Fact]
    public void Snapshot_sequence_allocates_the_final_value_once_then_fails_explicitly()
    {
        var allocator = new MovementSnapshotSequenceAllocator(ulong.MaxValue);

        Assert.Equal(ulong.MaxValue, allocator.Allocate().Value);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => allocator.Allocate());
        Assert.Equal("The movement snapshot sequence space is exhausted.", exception.Message);
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
            NowUnixMilliseconds);
        GameplaySessionId sessionId = Assert.IsType<WorldJoinAccepted>(outcome.Accepted).SessionId;
        Assert.True(runtime.TryGetGameplaySession(sessionId, out WorldGameplaySession? session));
        return Assert.IsType<WorldGameplaySession>(session);
    }

    private static byte[] EncodeCommand(
        ulong sequence,
        float destinationX,
        float destinationZ) =>
        ConnectedWalkingCodec.EncodeCommand(new GroundMovementCommand(
            new MovementIntentSequence(sequence),
            new GroundPosition(destinationX, destinationZ)));

    private static PlayerMovementSnapshot DecodeSnapshot(WorldWalkingSnapshotPublication publication)
    {
        Assert.True(ConnectedWalkingCodec.TryDecodeSnapshot(
            publication.Payload,
            out PlayerMovementSnapshot? snapshot));
        return Assert.IsType<PlayerMovementSnapshot>(snapshot);
    }
}
