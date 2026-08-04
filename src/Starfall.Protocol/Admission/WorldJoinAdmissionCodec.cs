using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Starfall.Protocol.Admission;

public static class WorldJoinAdmissionCodec
{
    public const byte SchemaVersion = 1;
    public const int MaximumRequestPayloadLength = 516;
    public const int AcceptedPayloadLength = 18;
    public const int RejectedPayloadLength = 3;

    private const byte RequestKind = 1;
    private const byte AcceptedKind = 2;
    private const byte RejectedKind = 3;

    public static byte[] EncodeRequest(WorldJoinRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Ticket.Any(static value => value > 0x7f))
            throw new ArgumentException("Join request must contain one canonical ASCII ticket.", nameof(request));
        byte[] ticket = Encoding.ASCII.GetBytes(request.Ticket);
        if (ticket.Length is 0 or > WorldJoinTicketCodec.MaximumTokenLength)
        {
            throw new ArgumentException("Join request must contain one canonical ASCII ticket.", nameof(request));
        }

        byte[] payload = new byte[4 + ticket.Length];
        payload[0] = SchemaVersion;
        payload[1] = RequestKind;
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2), checked((ushort)ticket.Length));
        ticket.CopyTo(payload, 4);
        return payload;
    }

    public static bool TryDecodeRequest(ReadOnlySpan<byte> payload, [NotNullWhen(true)] out WorldJoinRequest? request)
    {
        request = null;
        if (payload.Length is < 5 or > MaximumRequestPayloadLength ||
            payload[0] != SchemaVersion || payload[1] != RequestKind)
        {
            return false;
        }

        int length = BinaryPrimitives.ReadUInt16BigEndian(payload[2..]);
        if (length is 0 or > WorldJoinTicketCodec.MaximumTokenLength || payload.Length != 4 + length)
            return false;
        ReadOnlySpan<byte> ticketBytes = payload[4..];
        if (ticketBytes.ContainsAnyExceptInRange((byte)0, (byte)0x7f))
            return false;

        try
        {
            request = new WorldJoinRequest(Encoding.ASCII.GetString(ticketBytes));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static byte[] EncodeAccepted(WorldJoinAccepted accepted)
    {
        ArgumentNullException.ThrowIfNull(accepted);
        if (!accepted.SessionId.IsValid)
            throw new ArgumentException("Accepted admission must contain a valid session.", nameof(accepted));
        byte[] payload = new byte[AcceptedPayloadLength];
        payload[0] = SchemaVersion;
        payload[1] = AcceptedKind;
        if (!accepted.SessionId.Value.TryWriteBytes(payload.AsSpan(2), bigEndian: true, out int written) || written != 16)
            throw new InvalidOperationException("Could not encode gameplay session identity.");
        return payload;
    }

    public static bool TryDecodeAccepted(ReadOnlySpan<byte> payload, [NotNullWhen(true)] out WorldJoinAccepted? accepted)
    {
        accepted = null;
        if (payload.Length != AcceptedPayloadLength || payload[0] != SchemaVersion || payload[1] != AcceptedKind)
            return false;
        Guid sessionId = new(payload[2..], bigEndian: true);
        if (sessionId == Guid.Empty)
            return false;
        accepted = new WorldJoinAccepted(new GameplaySessionId(sessionId));
        return true;
    }

    public static byte[] EncodeRejected(WorldJoinRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        if (!Enum.IsDefined(rejected.Reason))
            throw new ArgumentException("Rejected admission must contain a supported reason.", nameof(rejected));
        return [SchemaVersion, RejectedKind, checked((byte)rejected.Reason)];
    }

    public static bool TryDecodeRejected(ReadOnlySpan<byte> payload, [NotNullWhen(true)] out WorldJoinRejected? rejected)
    {
        rejected = null;
        if (payload.Length != RejectedPayloadLength || payload[0] != SchemaVersion || payload[1] != RejectedKind ||
            !Enum.IsDefined((WorldJoinRejectionReason)payload[2]))
        {
            return false;
        }
        rejected = new WorldJoinRejected((WorldJoinRejectionReason)payload[2]);
        return true;
    }
}
