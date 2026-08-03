using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Starfall.Protocol.Admission;

public sealed class WorldJoinTicketVerificationKey
{
    private readonly byte[] subjectPublicKeyInfo;

    public WorldJoinTicketVerificationKey(string keyId, ReadOnlySpan<byte> subjectPublicKeyInfo)
    {
        WorldJoinTicketCodec.ValidateKeyId(keyId, nameof(keyId));
        if (subjectPublicKeyInfo.IsEmpty)
            throw new ArgumentException("Verification key must not be empty.", nameof(subjectPublicKeyInfo));

        try
        {
            using ECDsa verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out int bytesRead);
            if (bytesRead != subjectPublicKeyInfo.Length || !WorldJoinTicketCodec.IsP256(verifier))
                throw new ArgumentException("Verification key must be one complete ECDSA P-256 public key.", nameof(subjectPublicKeyInfo));
        }
        catch (CryptographicException exception)
        {
            throw new ArgumentException("Verification key must be valid SubjectPublicKeyInfo.", nameof(subjectPublicKeyInfo), exception);
        }

        KeyId = keyId;
        this.subjectPublicKeyInfo = subjectPublicKeyInfo.ToArray();
    }

    public string KeyId
    {
        get;
    }

    internal ReadOnlySpan<byte> SubjectPublicKeyInfo => subjectPublicKeyInfo;
}

public sealed class WorldJoinTicketVerificationKeyRing
{
    private readonly IReadOnlyDictionary<string, WorldJoinTicketVerificationKey> keys;

    public WorldJoinTicketVerificationKeyRing(IEnumerable<WorldJoinTicketVerificationKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var copied = new Dictionary<string, WorldJoinTicketVerificationKey>(StringComparer.Ordinal);
        foreach (WorldJoinTicketVerificationKey key in keys)
        {
            if (key is null)
                throw new ArgumentException("Verification keys must not contain null entries.", nameof(keys));
            if (!copied.TryAdd(key.KeyId, key))
                throw new ArgumentException($"Verification key ID '{key.KeyId}' is duplicated.", nameof(keys));
        }

        if (copied.Count == 0)
            throw new ArgumentException("At least one verification key is required.", nameof(keys));

        this.keys = copied;
    }

    public int Count => keys.Count;

    internal bool TryGet(string keyId, out WorldJoinTicketVerificationKey? key) => keys.TryGetValue(keyId, out key);
}

public static class WorldJoinTicketCodec
{
    public const long MaximumLifetimeMilliseconds = 60_000;
    public const long AllowedClockSkewMilliseconds = 5_000;
    public const int MaximumTokenLength = 512;

    private const string Version = "sfjt1";
    private const int GuidByteCount = 16;
    private const int SignatureByteCount = 64;
    private const int FixedPayloadByteCount = (GuidByteCount * 4) + (sizeof(long) * 2) + 2;
    private const int MaximumPayloadByteCount = FixedPayloadByteCount + (AdmissionIdentity.MaximumSemanticLength * 2);
    private const int MaximumKeyIdLength = 64;

    public static string Issue(
        WorldJoinTicketClaims claims,
        string keyId,
        ECDsa signingKey)
    {
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentNullException.ThrowIfNull(signingKey);
        ValidateKeyId(keyId, nameof(keyId));
        if (!IsP256(signingKey))
            throw new ArgumentException("Signing key must use ECDSA P-256.", nameof(signingKey));

        byte[] payload = EncodePayload(claims);
        string payloadSegment = EncodeBase64Url(payload);
        string signingInput = $"{Version}.{keyId}.{payloadSegment}";
        byte[] signature;
        try
        {
            signature = signingKey.SignData(
                Encoding.ASCII.GetBytes(signingInput),
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (CryptographicException exception)
        {
            throw new ArgumentException("Signing key must contain an ECDSA P-256 private key.", nameof(signingKey), exception);
        }

        if (signature.Length != SignatureByteCount)
            throw new CryptographicException("ECDSA P-256 produced an unexpected signature length.");

        string token = $"{signingInput}.{EncodeBase64Url(signature)}";
        if (token.Length > MaximumTokenLength)
            throw new InvalidOperationException("Encoded join ticket exceeds the protocol limit.");

        return token;
    }

    public static WorldJoinTicketValidationResult Validate(
        string? token,
        WorldJoinTicketVerificationKeyRing verificationKeys,
        WorldJoinTicketAudience expectedAudience,
        long nowUnixMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(verificationKeys);
        if (!expectedAudience.IsValid)
            throw new ArgumentException("Expected ticket audience is invalid.", nameof(expectedAudience));
        ValidateUnixMilliseconds(nowUnixMilliseconds, nameof(nowUnixMilliseconds));

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaximumTokenLength)
            return Invalid();

        string[] segments = token.Split('.');
        if (segments.Length != 4 || segments[0] != Version || !IsValidKeyId(segments[1]))
            return Invalid();

        string keyId = segments[1];
        if (!verificationKeys.TryGet(keyId, out WorldJoinTicketVerificationKey? verificationKey))
            return Invalid();
        if (!TryDecodeBase64Url(segments[2], MaximumPayloadByteCount, out byte[] payload) ||
            !TryDecodeBase64Url(segments[3], SignatureByteCount, out byte[] signature) ||
            signature.Length != SignatureByteCount)
        {
            return Invalid();
        }

        string signingInput = $"{Version}.{keyId}.{segments[2]}";
        try
        {
            using ECDsa verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(verificationKey!.SubjectPublicKeyInfo, out int bytesRead);
            if (bytesRead != verificationKey.SubjectPublicKeyInfo.Length || !IsP256(verifier) ||
                !verifier.VerifyData(
                    Encoding.ASCII.GetBytes(signingInput),
                    signature,
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            {
                return Invalid();
            }
        }
        catch (CryptographicException)
        {
            return Invalid();
        }

        if (!TryDecodePayload(payload, out WorldJoinTicketClaims? claims))
            return Invalid();

        long futureBoundary;
        long expiryBoundary;
        try
        {
            futureBoundary = checked(nowUnixMilliseconds + AllowedClockSkewMilliseconds);
            expiryBoundary = checked(claims!.ExpiresAtUnixMilliseconds + AllowedClockSkewMilliseconds);
        }
        catch (OverflowException)
        {
            return Invalid();
        }

        if (claims.IssuedAtUnixMilliseconds > futureBoundary)
            return Invalid();
        if (nowUnixMilliseconds >= expiryBoundary)
        {
            return WorldJoinTicketValidationResult.Rejected(
                WorldJoinTicketValidationFailure.ExpiredTicket);
        }
        if (claims.WorldId != expectedAudience.WorldId ||
            claims.ChannelId != expectedAudience.ChannelId ||
            claims.WorldInstanceId != expectedAudience.WorldInstanceId)
        {
            return WorldJoinTicketValidationResult.Rejected(
                WorldJoinTicketValidationFailure.WrongDestination);
        }

        return WorldJoinTicketValidationResult.Accepted(claims);
    }

    internal static void ValidateKeyId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (!IsValidKeyId(value))
        {
            throw new ArgumentException(
                $"Key IDs must contain 1-{MaximumKeyIdLength} ASCII letters, digits, underscores or hyphens.",
                parameterName);
        }
    }

    internal static bool IsP256(ECDsa key)
    {
        try
        {
            ECParameters parameters = key.ExportParameters(includePrivateParameters: false);
            return key.KeySize == 256 && string.Equals(
                parameters.Curve.Oid.Value,
                ECCurve.NamedCurves.nistP256.Oid.Value,
                StringComparison.Ordinal);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool IsValidKeyId(string? value) =>
        value is { Length: > 0 and <= MaximumKeyIdLength } &&
        value.All(static character =>
            character is >= 'a' and <= 'z' ||
            character is >= 'A' and <= 'Z' ||
            character is >= '0' and <= '9' ||
            character is '_' or '-');

    private static byte[] EncodePayload(WorldJoinTicketClaims claims)
    {
        byte[] worldBytes = Encoding.ASCII.GetBytes(claims.WorldId.Value);
        byte[] channelBytes = Encoding.ASCII.GetBytes(claims.ChannelId.Value);
        byte[] payload = new byte[FixedPayloadByteCount + worldBytes.Length + channelBytes.Length];
        Span<byte> destination = payload;
        int offset = 0;

        WriteGuid(destination, ref offset, claims.TicketId.Value);
        WriteGuid(destination, ref offset, claims.AccountId.Value);
        WriteGuid(destination, ref offset, claims.CharacterId.Value);
        WriteSemantic(destination, ref offset, worldBytes);
        WriteSemantic(destination, ref offset, channelBytes);
        WriteGuid(destination, ref offset, claims.WorldInstanceId.Value);
        BinaryPrimitives.WriteInt64BigEndian(destination[offset..], claims.IssuedAtUnixMilliseconds);
        offset += sizeof(long);
        BinaryPrimitives.WriteInt64BigEndian(destination[offset..], claims.ExpiresAtUnixMilliseconds);

        return payload;
    }

    private static bool TryDecodePayload(
        ReadOnlySpan<byte> payload,
        out WorldJoinTicketClaims? claims)
    {
        claims = null;
        if (payload.Length < FixedPayloadByteCount || payload.Length > MaximumPayloadByteCount)
            return false;

        int offset = 0;
        if (!TryReadGuid(payload, ref offset, out Guid ticketId) ||
            !TryReadGuid(payload, ref offset, out Guid accountId) ||
            !TryReadGuid(payload, ref offset, out Guid characterId) ||
            !TryReadSemantic(payload, ref offset, out string? worldId) ||
            !TryReadSemantic(payload, ref offset, out string? channelId) ||
            !TryReadGuid(payload, ref offset, out Guid worldInstanceId) ||
            payload.Length - offset != sizeof(long) * 2)
        {
            return false;
        }

        long issuedAtUnixMilliseconds = BinaryPrimitives.ReadInt64BigEndian(payload[offset..]);
        offset += sizeof(long);
        long expiresAtUnixMilliseconds = BinaryPrimitives.ReadInt64BigEndian(payload[offset..]);

        try
        {
            claims = new WorldJoinTicketClaims(
                new JoinTicketId(ticketId),
                new AccountId(accountId),
                new CharacterId(characterId),
                new WorldId(worldId!),
                new ChannelId(channelId!),
                new WorldInstanceId(worldInstanceId),
                issuedAtUnixMilliseconds,
                expiresAtUnixMilliseconds);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void WriteGuid(Span<byte> destination, ref int offset, Guid value)
    {
        if (!value.TryWriteBytes(destination[offset..], bigEndian: true, out int bytesWritten) ||
            bytesWritten != GuidByteCount)
        {
            throw new InvalidOperationException("Could not encode a join-ticket GUID.");
        }

        offset += GuidByteCount;
    }

    private static void WriteSemantic(Span<byte> destination, ref int offset, ReadOnlySpan<byte> value)
    {
        destination[offset++] = checked((byte)value.Length);
        value.CopyTo(destination[offset..]);
        offset += value.Length;
    }

    private static bool TryReadGuid(ReadOnlySpan<byte> source, ref int offset, out Guid value)
    {
        if (source.Length - offset < GuidByteCount)
        {
            value = default;
            return false;
        }

        value = new Guid(source.Slice(offset, GuidByteCount), bigEndian: true);
        offset += GuidByteCount;
        return true;
    }

    private static bool TryReadSemantic(ReadOnlySpan<byte> source, ref int offset, out string? value)
    {
        value = null;
        if (offset >= source.Length)
            return false;

        int length = source[offset++];
        if (length is <= 0 or > AdmissionIdentity.MaximumSemanticLength || source.Length - offset < length)
            return false;

        ReadOnlySpan<byte> encoded = source.Slice(offset, length);
        foreach (byte character in encoded)
        {
            if (character > 0x7f)
                return false;
        }

        value = Encoding.ASCII.GetString(encoded);
        offset += length;
        return AdmissionIdentity.IsValidSemantic(value);
    }

    private static string EncodeBase64Url(ReadOnlySpan<byte> value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryDecodeBase64Url(string value, int maximumDecodedLength, out byte[] decoded)
    {
        decoded = [];
        if (string.IsNullOrEmpty(value) || value.Contains('=') || value.Length % 4 == 1 ||
            value.Any(static character =>
                character is not (>= 'a' and <= 'z') &&
                character is not (>= 'A' and <= 'Z') &&
                character is not (>= '0' and <= '9') &&
                character is not '-' and not '_'))
        {
            return false;
        }

        int estimatedLength = (value.Length * 3) / 4;
        if (estimatedLength > maximumDecodedLength)
            return false;

        string padded = value.Replace('-', '+').Replace('_', '/');
        padded += (value.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty,
        };

        try
        {
            decoded = Convert.FromBase64String(padded);
            return decoded.Length <= maximumDecodedLength &&
                string.Equals(EncodeBase64Url(decoded), value, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            decoded = [];
            return false;
        }
    }

    private static void ValidateUnixMilliseconds(long value, string parameterName)
    {
        try
        {
            _ = DateTimeOffset.FromUnixTimeMilliseconds(value);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Validation time must be a representable Unix millisecond value.");
        }
    }

    private static WorldJoinTicketValidationResult Invalid() =>
        WorldJoinTicketValidationResult.Rejected(WorldJoinTicketValidationFailure.InvalidTicket);
}
