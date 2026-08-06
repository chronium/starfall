using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Starfall.Protocol.Development;

public enum DevelopmentCommandResultPayloadKind : byte
{
    Availability = 1,
    Succeeded = 2,
    Rejected = 3,
}

public static class DevelopmentCommandCodec
{
    public const int MinimumRequestPayloadLength = 11;
    public const int MaximumRequestPayloadLength = 594;
    public const int AvailabilityPayloadLength = 2;
    public const int MaximumSucceededPayloadLength = 588;
    public const int MaximumRejectedPayloadLength = 589;
    public const int MaximumDiagnosticByteLength = DevelopmentCommandText.MaximumDiagnosticByteLength;

    public static byte[] EncodeRequest(DevelopmentCommandRequest request)
    {
        ValidateRequest(request);

        int length = checked(
            10 +
            request.CommandId.Value.Length +
            request.Arguments.Sum(static argument => 1 + argument.Length));
        byte[] payload = new byte[length];
        WriteUInt64(payload, 0, request.Sequence.Value);
        int offset = 8;
        offset = WriteIdentifier(payload, offset, request.CommandId);
        payload[offset++] = checked((byte)request.Arguments.Length);
        foreach (string argument in request.Arguments)
        {
            payload[offset++] = checked((byte)argument.Length);
            Encoding.ASCII.GetBytes(argument, payload.AsSpan(offset));
            offset += argument.Length;
        }

        return payload;
    }

    public static bool TryDecodeRequest(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out DevelopmentCommandRequest? request)
    {
        request = null;
        if (payload.Length is < MinimumRequestPayloadLength or > MaximumRequestPayloadLength)
            return false;

        ulong sequence = ReadUInt64(payload, 0);
        int offset = 8;
        if (sequence == 0 || !TryReadIdentifier(payload, ref offset, out DevelopmentCommandId commandId) ||
            offset >= payload.Length)
        {
            return false;
        }

        int argumentCount = payload[offset++];
        if (argumentCount > DevelopmentCommandRequest.MaximumArgumentCount)
            return false;

        var arguments = ImmutableArray.CreateBuilder<string>(argumentCount);
        for (int index = 0; index < argumentCount; index++)
        {
            if (offset >= payload.Length)
                return false;
            int argumentLength = payload[offset++];
            if (argumentLength is 0 or > DevelopmentCommandRequest.MaximumArgumentByteLength ||
                payload.Length - offset < argumentLength)
            {
                return false;
            }

            ReadOnlySpan<byte> argumentBytes = payload.Slice(offset, argumentLength);
            if (!IsPrintableAscii(argumentBytes, allowSpace: false))
                return false;
            arguments.Add(Encoding.ASCII.GetString(argumentBytes));
            offset += argumentLength;
        }

        if (offset != payload.Length)
            return false;

        request = new DevelopmentCommandRequest(
            new DevelopmentCommandSequence(sequence),
            commandId,
            arguments.MoveToImmutable());
        return true;
    }

    public static byte[] EncodeAvailability(DevelopmentCommandAvailability availability)
    {
        if (!availability.IsValid)
            throw new ArgumentException("Development command availability must be valid.", nameof(availability));

        return
        [
            (byte)DevelopmentCommandResultPayloadKind.Availability,
            (byte)availability.State,
        ];
    }

    public static bool TryDecodeAvailability(
        ReadOnlySpan<byte> payload,
        out DevelopmentCommandAvailability availability)
    {
        availability = default;
        if (payload.Length != AvailabilityPayloadLength ||
            payload[0] != (byte)DevelopmentCommandResultPayloadKind.Availability ||
            !Enum.IsDefined((DevelopmentCommandAvailabilityState)payload[1]))
        {
            return false;
        }

        availability = new DevelopmentCommandAvailability((DevelopmentCommandAvailabilityState)payload[1]);
        return true;
    }

    public static byte[] EncodeSucceeded(DevelopmentCommandSucceeded succeeded)
    {
        ValidateSucceeded(succeeded);
        byte[] payload = CreateResultPayload(
            DevelopmentCommandResultPayloadKind.Succeeded,
            succeeded.Sequence,
            succeeded.CommandId,
            succeeded.Diagnostic,
            rejectionReason: null);
        return payload;
    }

    public static bool TryDecodeSucceeded(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out DevelopmentCommandSucceeded? succeeded)
    {
        succeeded = null;
        if (!TryReadResultHeader(
                payload,
                DevelopmentCommandResultPayloadKind.Succeeded,
                out DevelopmentCommandSequence sequence,
                out DevelopmentCommandId commandId,
                out int offset) ||
            !TryReadDiagnostic(payload, offset, out string? diagnostic))
        {
            return false;
        }

        succeeded = new DevelopmentCommandSucceeded(sequence, commandId, diagnostic);
        return true;
    }

    public static byte[] EncodeRejected(DevelopmentCommandRejected rejected)
    {
        ValidateRejected(rejected);
        byte[] payload = CreateResultPayload(
            DevelopmentCommandResultPayloadKind.Rejected,
            rejected.Sequence,
            rejected.CommandId,
            rejected.Diagnostic,
            rejected.Reason);
        return payload;
    }

    public static bool TryDecodeRejected(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out DevelopmentCommandRejected? rejected)
    {
        rejected = null;
        if (!TryReadResultHeader(
                payload,
                DevelopmentCommandResultPayloadKind.Rejected,
                out DevelopmentCommandSequence sequence,
                out DevelopmentCommandId commandId,
                out int offset) ||
            offset >= payload.Length ||
            !Enum.IsDefined((DevelopmentCommandRejectionReason)payload[offset]))
        {
            return false;
        }

        var reason = (DevelopmentCommandRejectionReason)payload[offset++];
        if (!TryReadDiagnostic(payload, offset, out string? diagnostic))
            return false;

        rejected = new DevelopmentCommandRejected(sequence, commandId, reason, diagnostic);
        return true;
    }

    public static bool TryReadResultPayloadKind(
        ReadOnlySpan<byte> payload,
        out DevelopmentCommandResultPayloadKind kind)
    {
        kind = default;
        if (payload.IsEmpty || !Enum.IsDefined((DevelopmentCommandResultPayloadKind)payload[0]))
            return false;
        kind = (DevelopmentCommandResultPayloadKind)payload[0];
        return true;
    }

    private static byte[] CreateResultPayload(
        DevelopmentCommandResultPayloadKind kind,
        DevelopmentCommandSequence sequence,
        DevelopmentCommandId commandId,
        string diagnostic,
        DevelopmentCommandRejectionReason? rejectionReason)
    {
        int length = checked(
            12 + commandId.Value.Length + diagnostic.Length + (rejectionReason.HasValue ? 1 : 0));
        byte[] payload = new byte[length];
        payload[0] = (byte)kind;
        WriteUInt64(payload, 1, sequence.Value);
        int offset = WriteIdentifier(payload, 9, commandId);
        if (rejectionReason is { } reason)
            payload[offset++] = (byte)reason;
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(offset), checked((ushort)diagnostic.Length));
        offset += 2;
        Encoding.ASCII.GetBytes(diagnostic, payload.AsSpan(offset));
        return payload;
    }

    private static bool TryReadResultHeader(
        ReadOnlySpan<byte> payload,
        DevelopmentCommandResultPayloadKind expectedKind,
        out DevelopmentCommandSequence sequence,
        out DevelopmentCommandId commandId,
        out int offset)
    {
        sequence = default;
        commandId = default;
        offset = 0;
        int maximumLength = expectedKind == DevelopmentCommandResultPayloadKind.Succeeded
            ? MaximumSucceededPayloadLength
            : MaximumRejectedPayloadLength;
        int minimumLength = expectedKind == DevelopmentCommandResultPayloadKind.Succeeded ? 14 : 15;
        if (payload.Length < minimumLength || payload.Length > maximumLength || payload[0] != (byte)expectedKind)
            return false;

        ulong sequenceValue = ReadUInt64(payload, 1);
        offset = 9;
        if (sequenceValue == 0 || !TryReadIdentifier(payload, ref offset, out commandId))
            return false;

        sequence = new DevelopmentCommandSequence(sequenceValue);
        return true;
    }

    private static bool TryReadDiagnostic(ReadOnlySpan<byte> payload, int offset, [NotNullWhen(true)] out string? diagnostic)
    {
        diagnostic = null;
        if (payload.Length - offset < 2)
            return false;
        int diagnosticLength = BinaryPrimitives.ReadUInt16BigEndian(payload[offset..]);
        offset += 2;
        if (diagnosticLength is 0 or > MaximumDiagnosticByteLength || payload.Length - offset != diagnosticLength)
            return false;
        ReadOnlySpan<byte> diagnosticBytes = payload[offset..];
        if (!IsPrintableAscii(diagnosticBytes, allowSpace: true))
            return false;
        diagnostic = Encoding.ASCII.GetString(diagnosticBytes);
        return true;
    }

    private static int WriteIdentifier(Span<byte> payload, int offset, DevelopmentCommandId commandId)
    {
        payload[offset++] = checked((byte)commandId.Value.Length);
        Encoding.ASCII.GetBytes(commandId.Value, payload[offset..]);
        return offset + commandId.Value.Length;
    }

    private static bool TryReadIdentifier(
        ReadOnlySpan<byte> payload,
        ref int offset,
        out DevelopmentCommandId commandId)
    {
        commandId = default;
        if (offset >= payload.Length)
            return false;
        int length = payload[offset++];
        if (length is 0 or > DevelopmentCommandId.MaximumByteLength || payload.Length - offset < length)
            return false;
        ReadOnlySpan<byte> bytes = payload.Slice(offset, length);
        if (!IsIdentifierAscii(bytes))
            return false;
        commandId = new DevelopmentCommandId(Encoding.ASCII.GetString(bytes));
        offset += length;
        return true;
    }

    private static bool IsIdentifierAscii(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty || bytes[0] is < (byte)'a' or > (byte)'z')
            return false;
        foreach (byte value in bytes)
        {
            if (value is not (>= (byte)'a' and <= (byte)'z') and
                not (>= (byte)'0' and <= (byte)'9') and
                not (byte)'_')
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsPrintableAscii(ReadOnlySpan<byte> bytes, bool allowSpace)
    {
        byte minimum = allowSpace ? (byte)' ' : (byte)'!';
        foreach (byte value in bytes)
        {
            if (value < minimum || value > (byte)'~')
                return false;
        }
        return !bytes.IsEmpty;
    }

    private static void ValidateRequest(DevelopmentCommandRequest? request)
    {
        if (request is null || !request.Sequence.IsValid || !request.CommandId.IsValid ||
            request.Arguments.IsDefault || request.Arguments.Length > DevelopmentCommandRequest.MaximumArgumentCount ||
            request.Arguments.Any(static argument => !DevelopmentCommandText.IsValidArgument(argument)))
        {
            throw new ArgumentException("Development command request must be a complete canonical fact.", nameof(request));
        }
    }

    private static void ValidateSucceeded(DevelopmentCommandSucceeded? succeeded)
    {
        if (succeeded is null)
            throw new ArgumentException("Development command success must be a complete canonical fact.", nameof(succeeded));
        try
        {
            DevelopmentCommandFactValidation.ValidateResult(
                succeeded.Sequence,
                succeeded.CommandId,
                succeeded.Diagnostic);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException("Development command success must be a complete canonical fact.", nameof(succeeded), exception);
        }
    }

    private static void ValidateRejected(DevelopmentCommandRejected? rejected)
    {
        if (rejected is null || !Enum.IsDefined(rejected.Reason))
            throw new ArgumentException("Development command rejection must be a complete canonical fact.", nameof(rejected));
        try
        {
            DevelopmentCommandFactValidation.ValidateResult(
                rejected.Sequence,
                rejected.CommandId,
                rejected.Diagnostic);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException("Development command rejection must be a complete canonical fact.", nameof(rejected), exception);
        }
    }

    private static ulong ReadUInt64(ReadOnlySpan<byte> payload, int offset) =>
        BinaryPrimitives.ReadUInt64BigEndian(payload[offset..]);

    private static void WriteUInt64(Span<byte> payload, int offset, ulong value) =>
        BinaryPrimitives.WriteUInt64BigEndian(payload[offset..], value);
}
