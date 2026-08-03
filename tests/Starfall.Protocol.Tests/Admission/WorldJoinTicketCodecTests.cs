using System.Security.Cryptography;
using System.Text;
using Starfall.Protocol.Admission;

namespace Starfall.Protocol.Tests.Admission;

public sealed class WorldJoinTicketCodecTests
{
    [Fact]
    public void IssueAndValidateRoundTripsEveryClaimWithPublicKeyOnly()
    {
        WorldJoinTicketClaims expected = TestAdmission.CreateClaims();
        string token;
        WorldJoinTicketVerificationKeyRing keyRing;
        using (ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        {
            token = WorldJoinTicketCodec.Issue(expected, "identity_2026_08", signingKey);
            keyRing = TestAdmission.Ring("identity_2026_08", signingKey);
        }

        WorldJoinTicketValidationResult result = WorldJoinTicketCodec.Validate(
            token,
            keyRing,
            TestAdmission.Audience(expected),
            TestAdmission.NowUnixMilliseconds);

        Assert.True(result.IsValid);
        Assert.Null(result.Failure);
        WorldJoinTicketClaims actual = Assert.IsType<WorldJoinTicketClaims>(result.Claims);
        Assert.Equal(expected.TicketId, actual.TicketId);
        Assert.Equal(expected.AccountId, actual.AccountId);
        Assert.Equal(expected.CharacterId, actual.CharacterId);
        Assert.Equal(expected.WorldId, actual.WorldId);
        Assert.Equal(expected.ChannelId, actual.ChannelId);
        Assert.Equal(expected.WorldInstanceId, actual.WorldInstanceId);
        Assert.Equal(expected.IssuedAtUnixMilliseconds, actual.IssuedAtUnixMilliseconds);
        Assert.Equal(expected.ExpiresAtUnixMilliseconds, actual.ExpiresAtUnixMilliseconds);
        Assert.StartsWith("sfjt1.identity_2026_08.", token, StringComparison.Ordinal);
        Assert.DoesNotContain('=', token);
        Assert.Throws<InvalidOperationException>(() => result.ToRejectionReason());
    }

    [Fact]
    public void VerificationKeyRingSupportsRotationBySignedKeyId()
    {
        using ECDsa previousKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa currentKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldJoinTicketClaims claims = TestAdmission.CreateClaims();
        WorldJoinTicketVerificationKeyRing keyRing = new(
        [
            new WorldJoinTicketVerificationKey("previous", previousKey.ExportSubjectPublicKeyInfo()),
            new WorldJoinTicketVerificationKey("current", currentKey.ExportSubjectPublicKeyInfo()),
        ]);

        string token = WorldJoinTicketCodec.Issue(claims, "current", currentKey);
        WorldJoinTicketValidationResult result = WorldJoinTicketCodec.Validate(
            token,
            keyRing,
            TestAdmission.Audience(claims),
            TestAdmission.NowUnixMilliseconds);

        Assert.True(result.IsValid);
        Assert.Equal(2, keyRing.Count);
    }

    [Fact]
    public void PayloadEncodingUsesTheDocumentedCanonicalFieldOrder()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldJoinTicketClaims claims = new(
            new JoinTicketId(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff")),
            new AccountId(Guid.Parse("10213243-5465-7687-98a9-bacbdcedfe0f")),
            new CharacterId(Guid.Parse("20314253-6475-8697-a8b9-cadbecfd0e1f")),
            new WorldId("world_1"),
            new ChannelId("channel_1"),
            new WorldInstanceId(Guid.Parse("30415263-7485-96a7-b8c9-daebfc0d1e2f")),
            TestAdmission.NowUnixMilliseconds,
            TestAdmission.NowUnixMilliseconds + 30_000);

        string token = WorldJoinTicketCodec.Issue(claims, "key", key);
        byte[] payload = DecodeBase64Url(token.Split('.')[2]);

        Assert.Equal(98, payload.Length);
        Assert.Equal(
            "00112233445566778899AABBCCDDEEFF",
            Convert.ToHexString(payload.AsSpan(0, 16)));
        Assert.Equal(7, payload[48]);
        Assert.Equal("world_1", Encoding.ASCII.GetString(payload.AsSpan(49, 7)));
        Assert.Equal(9, payload[56]);
        Assert.Equal("channel_1", Encoding.ASCII.GetString(payload.AsSpan(57, 9)));
        Assert.Equal(
            "30415263748596A7B8C9DAEBFC0D1E2F",
            Convert.ToHexString(payload.AsSpan(66, 16)));
    }

    [Fact]
    public void AuthenticatedPayloadWithTrailingDataIsStillRejected()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldJoinTicketClaims claims = TestAdmission.CreateClaims();
        string[] segments = WorldJoinTicketCodec.Issue(claims, "key", key).Split('.');
        byte[] payload = DecodeBase64Url(segments[2]);
        byte[] extended = [.. payload, 0];
        string payloadSegment = EncodeBase64Url(extended);
        string signingInput = $"sfjt1.key.{payloadSegment}";
        byte[] signature = key.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        string token = $"{signingInput}.{EncodeBase64Url(signature)}";

        AssertInvalid(WorldJoinTicketCodec.Validate(
            token,
            TestAdmission.Ring("key", key),
            TestAdmission.Audience(claims),
            TestAdmission.NowUnixMilliseconds));
    }

    [Fact]
    public void UnknownKeysAndTamperingAreReportedOnlyAsInvalidTickets()
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa unrelatedKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldJoinTicketClaims claims = TestAdmission.CreateClaims();
        string token = WorldJoinTicketCodec.Issue(claims, "current", signingKey);
        WorldJoinTicketVerificationKeyRing unrelatedRing = TestAdmission.Ring("other", unrelatedKey);
        WorldJoinTicketVerificationKeyRing keyRing = new(
        [
            new WorldJoinTicketVerificationKey("current", signingKey.ExportSubjectPublicKeyInfo()),
            new WorldJoinTicketVerificationKey("other", signingKey.ExportSubjectPublicKeyInfo()),
        ]);

        AssertInvalid(WorldJoinTicketCodec.Validate(
            token,
            unrelatedRing,
            TestAdmission.Audience(claims),
            TestAdmission.NowUnixMilliseconds));

        string[] segments = token.Split('.');
        AssertInvalid(Validate(string.Join('.', "sfjt2", segments[1], segments[2], segments[3])));
        AssertInvalid(Validate(string.Join('.', segments[0], "other", segments[2], segments[3])));
        AssertInvalid(Validate(string.Join('.', segments[0], segments[1], Mutate(segments[2]), segments[3])));
        AssertInvalid(Validate(string.Join('.', segments[0], segments[1], segments[2], Mutate(segments[3]))));

        WorldJoinTicketValidationResult Validate(string candidate) => WorldJoinTicketCodec.Validate(
            candidate,
            keyRing,
            TestAdmission.Audience(claims),
            TestAdmission.NowUnixMilliseconds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sfjt1")]
    [InlineData("sfjt1.key.payload.signature.extra")]
    [InlineData("sfjt1.bad$key.payload.signature")]
    [InlineData("sfjt1.key.cGF5bG9hZA==.c2lnbmF0dXJl")]
    [InlineData("sfjt1.key.*.c2lnbmF0dXJl")]
    public void MalformedTokensAreRejectedWithoutThrowing(string? token)
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldJoinTicketClaims claims = TestAdmission.CreateClaims();

        AssertInvalid(WorldJoinTicketCodec.Validate(
            token,
            TestAdmission.Ring("key", key),
            TestAdmission.Audience(claims),
            TestAdmission.NowUnixMilliseconds));
    }

    [Fact]
    public void ValidationBindsWorldChannelAndLifecycleInstance()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldJoinTicketClaims claims = TestAdmission.CreateClaims();
        string token = WorldJoinTicketCodec.Issue(claims, "key", key);
        WorldJoinTicketVerificationKeyRing keyRing = TestAdmission.Ring("key", key);

        AssertWrongDestination(new WorldJoinTicketAudience(
            new WorldId("world_2"),
            claims.ChannelId,
            claims.WorldInstanceId));
        AssertWrongDestination(new WorldJoinTicketAudience(
            claims.WorldId,
            new ChannelId("channel_2"),
            claims.WorldInstanceId));
        AssertWrongDestination(new WorldJoinTicketAudience(
            claims.WorldId,
            claims.ChannelId,
            new WorldInstanceId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))));

        void AssertWrongDestination(WorldJoinTicketAudience audience)
        {
            WorldJoinTicketValidationResult result = WorldJoinTicketCodec.Validate(
                token,
                keyRing,
                audience,
                TestAdmission.NowUnixMilliseconds);
            Assert.False(result.IsValid);
            Assert.Equal(WorldJoinTicketValidationFailure.WrongDestination, result.Failure);
            Assert.Equal(WorldJoinRejectionReason.WrongDestination, result.ToRejectionReason());
        }
    }

    [Fact]
    public void ValidationUsesExplicitExpiryAndClockSkewBoundaries()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WorldJoinTicketClaims claims = TestAdmission.CreateClaims(expiresAfterMilliseconds: 1_000);
        string token = WorldJoinTicketCodec.Issue(claims, "key", key);
        WorldJoinTicketVerificationKeyRing keyRing = TestAdmission.Ring("key", key);
        WorldJoinTicketAudience audience = TestAdmission.Audience(claims);

        Assert.True(WorldJoinTicketCodec.Validate(
            token,
            keyRing,
            audience,
            claims.ExpiresAtUnixMilliseconds + WorldJoinTicketCodec.AllowedClockSkewMilliseconds - 1).IsValid);

        WorldJoinTicketValidationResult expired = WorldJoinTicketCodec.Validate(
            token,
            keyRing,
            audience,
            claims.ExpiresAtUnixMilliseconds + WorldJoinTicketCodec.AllowedClockSkewMilliseconds);
        Assert.False(expired.IsValid);
        Assert.Equal(WorldJoinTicketValidationFailure.ExpiredTicket, expired.Failure);
        Assert.Equal(WorldJoinRejectionReason.ExpiredTicket, expired.ToRejectionReason());

        Assert.True(WorldJoinTicketCodec.Validate(
            token,
            keyRing,
            audience,
            claims.IssuedAtUnixMilliseconds - WorldJoinTicketCodec.AllowedClockSkewMilliseconds).IsValid);
        AssertInvalid(WorldJoinTicketCodec.Validate(
            token,
            keyRing,
            audience,
            claims.IssuedAtUnixMilliseconds - WorldJoinTicketCodec.AllowedClockSkewMilliseconds - 1));
    }

    [Fact]
    public void SigningAndVerificationRejectWrongKeyShapes()
    {
        WorldJoinTicketClaims claims = TestAdmission.CreateClaims();
        using ECDsa publicOnly = ECDsa.Create();
        using ECDsa privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        publicOnly.ImportSubjectPublicKeyInfo(privateKey.ExportSubjectPublicKeyInfo(), out _);
        using ECDsa p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        Assert.Throws<ArgumentException>(() => WorldJoinTicketCodec.Issue(claims, "key", publicOnly));
        Assert.Throws<ArgumentException>(() => WorldJoinTicketCodec.Issue(claims, "key", p384));
        Assert.Throws<ArgumentException>(() => new WorldJoinTicketVerificationKey(
            "key",
            p384.ExportSubjectPublicKeyInfo()));
        Assert.Throws<ArgumentException>(() => new WorldJoinTicketVerificationKey("key", [1, 2, 3]));
        Assert.Throws<ArgumentException>(() => new WorldJoinTicketVerificationKeyRing([]));
        Assert.Throws<ArgumentException>(() => new WorldJoinTicketVerificationKeyRing(
        [
            new WorldJoinTicketVerificationKey("same", privateKey.ExportSubjectPublicKeyInfo()),
            new WorldJoinTicketVerificationKey("same", privateKey.ExportSubjectPublicKeyInfo()),
        ]));
    }

    private static string Mutate(string value)
    {
        char replacement = value[0] == 'A' ? 'B' : 'A';
        return replacement + value[1..];
    }

    private static string EncodeBase64Url(ReadOnlySpan<byte> value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] DecodeBase64Url(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');
        padded += (value.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty,
        };
        return Convert.FromBase64String(padded);
    }

    private static void AssertInvalid(WorldJoinTicketValidationResult result)
    {
        Assert.False(result.IsValid);
        Assert.Null(result.Claims);
        Assert.Equal(WorldJoinTicketValidationFailure.InvalidTicket, result.Failure);
        Assert.Equal(WorldJoinRejectionReason.InvalidTicket, result.ToRejectionReason());
    }
}
