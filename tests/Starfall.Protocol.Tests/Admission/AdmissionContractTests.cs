using Starfall.Protocol.Admission;
using Starfall.Protocol.Compatibility;

namespace Starfall.Protocol.Tests.Admission;

public sealed class AdmissionContractTests
{
    [Fact]
    public void OpaqueIdentitiesRejectEmptyGuids()
    {
        Assert.Throws<ArgumentException>(() => new JoinTicketId(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new AccountId(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new CharacterId(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new WorldInstanceId(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new GameplaySessionId(Guid.Empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData("World_1")]
    [InlineData("world-1")]
    [InlineData("1_world")]
    [InlineData("world one")]
    public void SemanticIdentitiesRejectNonCanonicalValues(string value)
    {
        Assert.Throws<ArgumentException>(() => new WorldId(value));
        Assert.Throws<ArgumentException>(() => new ChannelId(value));
    }

    [Fact]
    public void SemanticIdentitiesAcceptBoundedLowercaseValues()
    {
        Assert.Equal("world_1", new WorldId("world_1").Value);
        Assert.Equal("channel_1", new ChannelId("channel_1").Value);
        Assert.Equal(new string('a', 64), new WorldId(new string('a', 64)).Value);
        Assert.Throws<ArgumentException>(() => new WorldId(new string('a', 65)));
    }

    [Fact]
    public void ClaimsRejectInvalidLifetimeAndDefaultIdentities()
    {
        WorldJoinTicketClaims valid = TestAdmission.CreateClaims();

        Assert.Throws<ArgumentException>(() => new WorldJoinTicketClaims(
            default,
            valid.AccountId,
            valid.CharacterId,
            valid.WorldId,
            valid.ChannelId,
            valid.WorldInstanceId,
            valid.IssuedAtUnixMilliseconds,
            valid.ExpiresAtUnixMilliseconds));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestAdmission.CreateClaims(expiresAfterMilliseconds: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestAdmission.CreateClaims(
            expiresAfterMilliseconds: WorldJoinTicketCodec.MaximumLifetimeMilliseconds + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldJoinTicketClaims(
            valid.TicketId,
            valid.AccountId,
            valid.CharacterId,
            valid.WorldId,
            valid.ChannelId,
            valid.WorldInstanceId,
            long.MinValue,
            long.MinValue + 1));
    }

    [Fact]
    public void AdmissionFactsRejectInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProtocolVersion(0));
        Assert.Throws<ArgumentException>(() => new WorldJoinRequest(default, "ticket"));
        Assert.Throws<ArgumentException>(() => new WorldJoinRequest(StarfallGameplayProtocol.CurrentVersion, ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldJoinRequest(
            StarfallGameplayProtocol.CurrentVersion,
            new string('a', WorldJoinTicketCodec.MaximumTokenLength + 1)));
        Assert.Throws<ArgumentException>(() => new WorldJoinAccepted(default, new GameplaySessionId(Guid.NewGuid())));
        Assert.Throws<ArgumentException>(() => new WorldJoinAccepted(StarfallGameplayProtocol.CurrentVersion, default));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldJoinRejected((WorldJoinRejectionReason)999));

        GameplaySessionId sessionId = new(Guid.Parse("55555555-5555-5555-5555-555555555555"));
        WorldJoinAccepted accepted = new(StarfallGameplayProtocol.CurrentVersion, sessionId);
        Assert.Equal(sessionId, accepted.SessionId);
        Assert.Equal(StarfallGameplayProtocol.CurrentVersion, accepted.SelectedProtocolVersion);
        Assert.Equal(
            WorldJoinRejectionReason.AlreadyConsumed,
            new WorldJoinRejected(WorldJoinRejectionReason.AlreadyConsumed).Reason);
    }
}
