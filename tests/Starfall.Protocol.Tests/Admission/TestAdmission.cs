using System.Security.Cryptography;
using Starfall.Protocol.Admission;

namespace Starfall.Protocol.Tests.Admission;

internal static class TestAdmission
{
    internal const long NowUnixMilliseconds = 1_800_000_000_000;

    internal static WorldJoinTicketClaims CreateClaims(
        long expiresAfterMilliseconds = 30_000,
        WorldId? worldId = null,
        ChannelId? channelId = null,
        WorldInstanceId? worldInstanceId = null,
        long? issuedAtUnixMilliseconds = null) => new(
        new JoinTicketId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        new AccountId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
        new CharacterId(Guid.Parse("33333333-3333-3333-3333-333333333333")),
        worldId ?? new WorldId("world_1"),
        channelId ?? new ChannelId("channel_1"),
        worldInstanceId ?? new WorldInstanceId(Guid.Parse("44444444-4444-4444-4444-444444444444")),
        issuedAtUnixMilliseconds ?? NowUnixMilliseconds,
        checked((issuedAtUnixMilliseconds ?? NowUnixMilliseconds) + expiresAfterMilliseconds));

    internal static WorldJoinTicketAudience Audience(WorldJoinTicketClaims claims) => new(
        claims.WorldId,
        claims.ChannelId,
        claims.WorldInstanceId);

    internal static WorldJoinTicketVerificationKeyRing Ring(string keyId, ECDsa key) => new(
    [
        new WorldJoinTicketVerificationKey(keyId, key.ExportSubjectPublicKeyInfo()),
    ]);
}
