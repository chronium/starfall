using System.Security.Cryptography;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Compatibility;
using Starfall.Simulation.Entities;
using Starfall.World.Admission;
using Starfall.World.Lifecycle;

namespace Starfall.World.Tests;

public sealed class WorldJoinAdmissionExchangeTests
{
    private const long NowUnixMilliseconds = 1_800_000_000_000;

    [Fact]
    public void Valid_ticket_creates_one_world_owned_session_with_exact_claim_binding()
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldChannelRuntime runtime = CreateRuntime(start: true);
        WorldJoinTicketClaims claims = CreateClaims(runtime);
        WorldJoinAdmissionExchange exchange = CreateExchange(runtime, signingKey);

        WorldJoinAdmissionOutcome outcome = exchange.Handle(
            IssueRequest(claims, signingKey),
            NowUnixMilliseconds);

        Assert.True(outcome.IsAccepted);
        WorldJoinAccepted accepted = Assert.IsType<WorldJoinAccepted>(outcome.Accepted);
        Assert.Null(outcome.Rejected);
        Assert.True(runtime.TryGetGameplaySession(accepted.SessionId, out WorldGameplaySession? session));
        Assert.NotNull(session);
        Assert.Equal(accepted.SessionId, session.SessionId);
        Assert.Equal(StarfallGameplayProtocol.CurrentVersion, accepted.SelectedProtocolVersion);
        Assert.Equal(accepted.SelectedProtocolVersion, session.ProtocolVersion);
        Assert.Equal(claims.AccountId, session.AccountId);
        Assert.Equal(claims.CharacterId, session.CharacterId);
        Assert.Equal(runtime.InstanceId, session.WorldInstanceId);
        Assert.True(session.PlayerEntityId.Value > runtime.Monsters.Max(static monster => monster.EntityId.Value));
        Assert.True(runtime.TryGetPlayer(session.PlayerEntityId, out var player));
        Assert.NotNull(player);
        Assert.Equal(runtime.Layout.Town.RespawnAnchor, player.Position);
        Assert.Equal(1, runtime.ActiveSessionCount);
        Assert.Equal(1, runtime.PlayerCount);
        Assert.Equal(1, runtime.ConsumedTicketCount);
    }

    [Fact]
    public void Invalid_expired_and_wrong_destination_tickets_are_rejected_without_consumption()
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldChannelRuntime runtime = CreateRuntime(start: true);
        WorldJoinAdmissionExchange exchange = CreateExchange(runtime, signingKey);

        AssertRejected(
            exchange.Handle(
                new WorldJoinRequest(StarfallGameplayProtocol.CurrentVersion, "not-a-ticket"),
                NowUnixMilliseconds),
            WorldJoinRejectionReason.InvalidTicket);

        WorldJoinTicketClaims expired = CreateClaims(
            runtime,
            ticketId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
            expiresAtUnixMilliseconds: NowUnixMilliseconds + 1_000);
        AssertRejected(
            exchange.Handle(
                IssueRequest(expired, signingKey),
                expired.ExpiresAtUnixMilliseconds + WorldJoinTicketCodec.AllowedClockSkewMilliseconds),
            WorldJoinRejectionReason.ExpiredTicket);

        WorldJoinTicketClaims wrongDestination = CreateClaims(
            runtime,
            ticketId: Guid.Parse("10000000-0000-0000-0000-000000000002"),
            worldId: new WorldId("world_2"));
        AssertRejected(
            exchange.Handle(IssueRequest(wrongDestination, signingKey), NowUnixMilliseconds),
            WorldJoinRejectionReason.WrongDestination);

        Assert.Equal(0, runtime.ActiveSessionCount);
        Assert.Equal(0, runtime.PlayerCount);
        Assert.Equal(0, runtime.ConsumedTicketCount);
    }

    [Fact]
    public void Incompatible_protocol_is_rejected_before_ticket_validation_or_consumption_and_can_retry()
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldChannelRuntime runtime = CreateRuntime(start: true);
        WorldJoinAdmissionExchange exchange = CreateExchange(runtime, signingKey);
        WorldJoinRequest compatible = IssueRequest(CreateClaims(runtime), signingKey);
        var incompatible = new WorldJoinRequest(new ProtocolVersion(2), compatible.Ticket);

        AssertRejected(
            exchange.Handle(incompatible, NowUnixMilliseconds),
            WorldJoinRejectionReason.IncompatibleProtocolVersion);
        Assert.Equal(0, runtime.ActiveSessionCount);
        Assert.Equal(0, runtime.PlayerCount);
        Assert.Equal(0, runtime.ConsumedTicketCount);

        WorldJoinAdmissionOutcome retried = exchange.Handle(compatible, NowUnixMilliseconds);
        Assert.True(retried.IsAccepted);
        Assert.Equal(1, runtime.ActiveSessionCount);
        Assert.Equal(1, runtime.PlayerCount);
        Assert.Equal(1, runtime.ConsumedTicketCount);
    }

    [Fact]
    public void Runtime_rejects_an_invalid_protocol_before_consuming_a_validated_ticket()
    {
        WorldChannelRuntime runtime = CreateRuntime(start: true);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            runtime.ConsumeTicketAndCreateSession(
                CreateClaims(runtime),
                default,
                NowUnixMilliseconds));

        Assert.Equal("protocolVersion", exception.ParamName);
        Assert.Equal(0, runtime.ActiveSessionCount);
        Assert.Equal(0, runtime.PlayerCount);
        Assert.Equal(0, runtime.ConsumedTicketCount);
    }

    [Fact]
    public void A_valid_request_rejected_before_running_is_not_consumed_and_can_retry()
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldChannelRuntime runtime = CreateRuntime(start: false);
        WorldJoinAdmissionExchange exchange = CreateExchange(runtime, signingKey);
        WorldJoinRequest request = IssueRequest(CreateClaims(runtime), signingKey);

        AssertRejected(
            exchange.Handle(request, NowUnixMilliseconds),
            WorldJoinRejectionReason.WorldNotAcceptingAdmissions);
        Assert.Equal(0, runtime.ConsumedTicketCount);

        runtime.Start();
        Assert.True(exchange.Handle(request, NowUnixMilliseconds).IsAccepted);
        Assert.Equal(1, runtime.ActiveSessionCount);
        Assert.Equal(1, runtime.PlayerCount);
        Assert.Equal(1, runtime.ConsumedTicketCount);
    }

    [Fact]
    public void Concurrent_replay_attempts_accept_exactly_once()
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldChannelRuntime runtime = CreateRuntime(start: true);
        WorldJoinAdmissionExchange exchange = CreateExchange(runtime, signingKey);
        WorldJoinRequest request = IssueRequest(CreateClaims(runtime), signingKey);
        var outcomes = new WorldJoinAdmissionOutcome[16];

        Parallel.For(
            0,
            outcomes.Length,
            index => outcomes[index] = exchange.Handle(request, NowUnixMilliseconds));

        Assert.Single(outcomes, static outcome => outcome.IsAccepted);
        Assert.Equal(
            outcomes.Length - 1,
            outcomes.Count(static outcome =>
                outcome.Rejected?.Reason == WorldJoinRejectionReason.AlreadyConsumed));
        Assert.Equal(1, runtime.ActiveSessionCount);
        Assert.Equal(1, runtime.PlayerCount);
        Assert.Equal(1, runtime.ConsumedTicketCount);
    }

    [Fact]
    public void Admission_prunes_elapsed_replay_records_without_removing_sessions()
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldChannelRuntime runtime = CreateRuntime(start: true);
        WorldJoinAdmissionExchange exchange = CreateExchange(runtime, signingKey);
        WorldJoinTicketClaims first = CreateClaims(
            runtime,
            ticketId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
            expiresAtUnixMilliseconds: NowUnixMilliseconds + 1_000);
        WorldJoinTicketClaims second = CreateClaims(
            runtime,
            ticketId: Guid.Parse("20000000-0000-0000-0000-000000000002"),
            expiresAtUnixMilliseconds: NowUnixMilliseconds + 30_000);

        Assert.True(exchange.Handle(IssueRequest(first, signingKey), NowUnixMilliseconds).IsAccepted);
        Assert.True(exchange.Handle(
            IssueRequest(second, signingKey),
            first.ExpiresAtUnixMilliseconds + WorldJoinTicketCodec.AllowedClockSkewMilliseconds).IsAccepted);

        Assert.Equal(2, runtime.ActiveSessionCount);
        Assert.Equal(2, runtime.PlayerCount);
        Assert.Equal(1, runtime.ConsumedTicketCount);
    }

    [Fact]
    public void Draining_retains_sessions_rejects_new_joins_and_stop_clears_lifecycle_state()
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldChannelRuntime runtime = CreateRuntime(start: true);
        WorldJoinAdmissionExchange exchange = CreateExchange(runtime, signingKey);
        WorldJoinAdmissionOutcome accepted = exchange.Handle(
            IssueRequest(CreateClaims(runtime), signingKey),
            NowUnixMilliseconds);
        GameplaySessionId sessionId = Assert.IsType<WorldJoinAccepted>(accepted.Accepted).SessionId;

        runtime.Step();
        runtime.BeginDrain();

        WorldJoinTicketClaims next = CreateClaims(
            runtime,
            ticketId: Guid.Parse("30000000-0000-0000-0000-000000000002"));
        AssertRejected(
            exchange.Handle(IssueRequest(next, signingKey), NowUnixMilliseconds),
            WorldJoinRejectionReason.WorldNotAcceptingAdmissions);
        Assert.True(runtime.TryGetGameplaySession(sessionId, out _));
        Assert.Equal(1, runtime.ActiveSessionCount);
        Assert.Equal(1, runtime.ConsumedTicketCount);

        runtime.Step();
        runtime.Stop();

        Assert.False(runtime.TryGetGameplaySession(sessionId, out _));
        Assert.Equal(0, runtime.ActiveSessionCount);
        Assert.Equal(0, runtime.PlayerCount);
        Assert.Equal(0, runtime.ConsumedTicketCount);
        Assert.Equal(2UL, runtime.CurrentTick);
    }

    private static WorldChannelRuntime CreateRuntime(bool start)
    {
        var runtime = new WorldChannelRuntime(
            new WorldId("world_1"),
            new ChannelId("channel_1"),
            new WorldInstanceId(Guid.Parse("40000000-0000-0000-0000-000000000001")),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
        if (start)
            runtime.Start();
        return runtime;
    }

    private static WorldJoinTicketClaims CreateClaims(
        WorldChannelRuntime runtime,
        Guid? ticketId = null,
        long expiresAtUnixMilliseconds = NowUnixMilliseconds + 30_000,
        WorldId? worldId = null) =>
        new(
            new JoinTicketId(ticketId ?? Guid.Parse("10000000-0000-0000-0000-000000000000")),
            new AccountId(Guid.Parse("50000000-0000-0000-0000-000000000001")),
            new CharacterId(Guid.Parse("60000000-0000-0000-0000-000000000001")),
            worldId ?? runtime.WorldId,
            runtime.ChannelId,
            runtime.InstanceId,
            NowUnixMilliseconds,
            expiresAtUnixMilliseconds);

    private static WorldJoinAdmissionExchange CreateExchange(
        WorldChannelRuntime runtime,
        ECDsa signingKey) =>
        new(
            runtime,
            new WorldJoinTicketVerificationKeyRing(
            [
                new WorldJoinTicketVerificationKey(
                    "test_key",
                    signingKey.ExportSubjectPublicKeyInfo()),
            ]));

    private static WorldJoinRequest IssueRequest(
        WorldJoinTicketClaims claims,
        ECDsa signingKey) =>
        new(
            StarfallGameplayProtocol.CurrentVersion,
            WorldJoinTicketCodec.Issue(claims, "test_key", signingKey));

    private static void AssertRejected(
        WorldJoinAdmissionOutcome outcome,
        WorldJoinRejectionReason expectedReason)
    {
        Assert.False(outcome.IsAccepted);
        Assert.Null(outcome.Accepted);
        Assert.Equal(expectedReason, Assert.IsType<WorldJoinRejected>(outcome.Rejected).Reason);
    }
}
