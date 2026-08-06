using System.Collections.Immutable;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Compatibility;
using Starfall.Protocol.Development;
using Starfall.World.Admission;
using Starfall.World.Development;
using Starfall.World.Lifecycle;

namespace Starfall.World.Tests;

public sealed class WorldDevelopmentCommandDispatcherTests
{
    private static readonly long NowUnixMilliseconds = 1_800_000_000_000;

    [Fact]
    public void Ping_world_returns_exact_session_bound_diagnostic()
    {
        using RuntimeScope scope = CreateRuntime();
        WorldGameplaySession session = Admit(scope.Runtime, 1);
        var dispatcher = new WorldDevelopmentCommandDispatcher(
            scope.Runtime,
            [new PingWorldDevelopmentCommandHandler()]);

        WorldDevelopmentCommandOutcome outcome = dispatcher.Handle(
            session.SessionId,
            Encode(7, DevelopmentCommandIds.PingWorld));

        Assert.Equal(WorldDevelopmentCommandDisposition.Succeeded, outcome.Disposition);
        Assert.True(DevelopmentCommandCodec.TryDecodeSucceeded(
            outcome.Payload,
            out DevelopmentCommandSucceeded? succeeded));
        Assert.Equal(7UL, succeeded!.Sequence.Value);
        Assert.Equal(DevelopmentCommandIds.PingWorld, succeeded.CommandId);
        Assert.Equal(
            $"pong world=world_1 channel=channel_1 tick=0 session={session.SessionId} player={session.PlayerEntityId}",
            succeeded.Diagnostic);
    }

    [Fact]
    public void Ping_rejects_arguments_and_unknown_commands_without_losing_correlation()
    {
        using RuntimeScope scope = CreateRuntime();
        WorldGameplaySession session = Admit(scope.Runtime, 1);
        var dispatcher = new WorldDevelopmentCommandDispatcher(
            scope.Runtime,
            [new PingWorldDevelopmentCommandHandler()]);

        AssertRejected(
            dispatcher.Handle(session.SessionId, Encode(1, DevelopmentCommandIds.PingWorld, "extra")),
            1,
            DevelopmentCommandIds.PingWorld,
            DevelopmentCommandRejectionReason.InvalidArguments);
        var unknownId = new DevelopmentCommandId("unknown");
        AssertRejected(
            dispatcher.Handle(session.SessionId, Encode(2, unknownId)),
            2,
            unknownId,
            DevelopmentCommandRejectionReason.UnknownCommand);
    }

    [Fact]
    public void Every_fresh_sequence_is_consumed_and_sequence_state_is_per_session()
    {
        using RuntimeScope scope = CreateRuntime();
        WorldGameplaySession first = Admit(scope.Runtime, 1);
        WorldGameplaySession second = Admit(scope.Runtime, 2);
        var dispatcher = new WorldDevelopmentCommandDispatcher(
            scope.Runtime,
            [new PingWorldDevelopmentCommandHandler()]);
        var unknownId = new DevelopmentCommandId("unknown");

        AssertRejected(
            dispatcher.Handle(first.SessionId, Encode(5, unknownId)),
            5,
            unknownId,
            DevelopmentCommandRejectionReason.UnknownCommand);
        AssertRejected(
            dispatcher.Handle(first.SessionId, Encode(5, DevelopmentCommandIds.PingWorld)),
            5,
            DevelopmentCommandIds.PingWorld,
            DevelopmentCommandRejectionReason.StaleOrDuplicateSequence);
        AssertRejected(
            dispatcher.Handle(first.SessionId, Encode(4, DevelopmentCommandIds.PingWorld)),
            4,
            DevelopmentCommandIds.PingWorld,
            DevelopmentCommandRejectionReason.StaleOrDuplicateSequence);

        Assert.Equal(
            WorldDevelopmentCommandDisposition.Succeeded,
            dispatcher.Handle(second.SessionId, Encode(1, DevelopmentCommandIds.PingWorld)).Disposition);

        dispatcher.RemoveSession(first.SessionId);
        Assert.Equal(
            WorldDevelopmentCommandDisposition.Succeeded,
            dispatcher.Handle(first.SessionId, Encode(1, DevelopmentCommandIds.PingWorld)).Disposition);
    }

    [Fact]
    public void Handler_rejection_and_exception_are_bounded_results()
    {
        using RuntimeScope scope = CreateRuntime();
        WorldGameplaySession session = Admit(scope.Runtime, 1);
        var rejectedId = new DevelopmentCommandId("reject_test");
        var throwingId = new DevelopmentCommandId("throw_test");
        var dispatcher = new WorldDevelopmentCommandDispatcher(
            scope.Runtime,
            [
                new TestHandler(rejectedId, static (_, _) =>
                    WorldDevelopmentCommandHandlerResult.Reject(
                        DevelopmentCommandRejectionReason.HandlerRejected,
                        "rejected by handler")),
                new TestHandler(throwingId, static (_, _) =>
                    throw new InvalidOperationException("sensitive implementation detail")),
            ]);

        AssertRejected(
            dispatcher.Handle(session.SessionId, Encode(1, rejectedId)),
            1,
            rejectedId,
            DevelopmentCommandRejectionReason.HandlerRejected,
            "rejected by handler");
        AssertRejected(
            dispatcher.Handle(session.SessionId, Encode(2, throwingId)),
            2,
            throwingId,
            DevelopmentCommandRejectionReason.HandlerRejected,
            "handler failed");
    }

    [Fact]
    public void Malformed_payload_unknown_session_and_duplicate_registration_fail_explicitly()
    {
        using RuntimeScope scope = CreateRuntime();
        WorldGameplaySession session = Admit(scope.Runtime, 1);
        var ping = new PingWorldDevelopmentCommandHandler();
        var dispatcher = new WorldDevelopmentCommandDispatcher(scope.Runtime, [ping]);

        Assert.Equal(
            WorldDevelopmentCommandDisposition.MalformedPayload,
            dispatcher.Handle(session.SessionId, [1, 2, 3]).Disposition);
        Assert.Equal(
            WorldDevelopmentCommandDisposition.UnknownSession,
            dispatcher.Handle(new GameplaySessionId(Guid.NewGuid()), Encode(1, DevelopmentCommandIds.PingWorld)).Disposition);
        Assert.Throws<ArgumentException>(() =>
            new WorldDevelopmentCommandDispatcher(scope.Runtime, [ping, new PingWorldDevelopmentCommandHandler()]));
    }

    private static byte[] Encode(
        ulong sequence,
        DevelopmentCommandId commandId,
        params string[] arguments) => DevelopmentCommandCodec.EncodeRequest(
            new DevelopmentCommandRequest(
                new DevelopmentCommandSequence(sequence),
                commandId,
                arguments));

    private static void AssertRejected(
        WorldDevelopmentCommandOutcome outcome,
        ulong sequence,
        DevelopmentCommandId commandId,
        DevelopmentCommandRejectionReason reason,
        string? diagnostic = null)
    {
        Assert.Equal(WorldDevelopmentCommandDisposition.Rejected, outcome.Disposition);
        Assert.True(DevelopmentCommandCodec.TryDecodeRejected(
            outcome.Payload,
            out DevelopmentCommandRejected? rejected));
        Assert.Equal(sequence, rejected!.Sequence.Value);
        Assert.Equal(commandId, rejected.CommandId);
        Assert.Equal(reason, rejected.Reason);
        if (diagnostic is not null)
            Assert.Equal(diagnostic, rejected.Diagnostic);
    }

    private static RuntimeScope CreateRuntime()
    {
        var runtime = new WorldChannelRuntime(
            new WorldId("world_1"),
            new ChannelId("channel_1"),
            new WorldInstanceId(Guid.Parse("44444444-4444-4444-4444-444444444444")),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
        runtime.Start();
        return new RuntimeScope(runtime);
    }

    private static WorldGameplaySession Admit(WorldChannelRuntime runtime, int identity)
    {
        var claims = new WorldJoinTicketClaims(
            new JoinTicketId(GuidFrom(identity, 1)),
            new AccountId(GuidFrom(identity, 2)),
            new CharacterId(GuidFrom(identity, 3)),
            runtime.WorldId,
            runtime.ChannelId,
            runtime.InstanceId,
            NowUnixMilliseconds,
            NowUnixMilliseconds + 30_000);
        WorldJoinAdmissionOutcome outcome = runtime.ConsumeTicketAndCreateSession(
            claims,
            StarfallGameplayProtocol.CurrentVersion,
            NowUnixMilliseconds);
        Assert.True(outcome.IsAccepted);
        Assert.True(runtime.TryGetGameplaySession(outcome.Accepted!.SessionId, out WorldGameplaySession? session));
        return session!;
    }

    private static Guid GuidFrom(int identity, int kind) =>
        Guid.Parse($"{identity:D8}-0000-0000-0000-{kind:D12}");

    private sealed class RuntimeScope(WorldChannelRuntime runtime) : IDisposable
    {
        internal WorldChannelRuntime Runtime { get; } = runtime;
        public void Dispose() => Runtime.Stop();
    }

    private sealed class TestHandler(
        DevelopmentCommandId commandId,
        Func<WorldDevelopmentCommandContext, ImmutableArray<string>, WorldDevelopmentCommandHandlerResult> handle)
        : IWorldDevelopmentCommandHandler
    {
        public DevelopmentCommandId CommandId { get; } = commandId;

        public WorldDevelopmentCommandHandlerResult Handle(
            WorldDevelopmentCommandContext context,
            ImmutableArray<string> arguments) => handle(context, arguments);
    }
}
