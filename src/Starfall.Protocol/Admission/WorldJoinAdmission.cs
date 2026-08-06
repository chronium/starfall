using Starfall.Protocol.Compatibility;

namespace Starfall.Protocol.Admission;

public sealed class WorldJoinTicketClaims
{
    public WorldJoinTicketClaims(
        JoinTicketId ticketId,
        AccountId accountId,
        CharacterId characterId,
        WorldId worldId,
        ChannelId channelId,
        WorldInstanceId worldInstanceId,
        long issuedAtUnixMilliseconds,
        long expiresAtUnixMilliseconds)
    {
        if (!ticketId.IsValid)
            throw new ArgumentException("Ticket identity must not be empty.", nameof(ticketId));
        if (!accountId.IsValid)
            throw new ArgumentException("Account identity must not be empty.", nameof(accountId));
        if (!characterId.IsValid)
            throw new ArgumentException("Character identity must not be empty.", nameof(characterId));
        if (!worldId.IsValid)
            throw new ArgumentException("World identity is invalid.", nameof(worldId));
        if (!channelId.IsValid)
            throw new ArgumentException("Channel identity is invalid.", nameof(channelId));
        if (!worldInstanceId.IsValid)
            throw new ArgumentException("World instance identity must not be empty.", nameof(worldInstanceId));

        ValidateUnixMilliseconds(issuedAtUnixMilliseconds, nameof(issuedAtUnixMilliseconds));
        ValidateUnixMilliseconds(expiresAtUnixMilliseconds, nameof(expiresAtUnixMilliseconds));

        long lifetime;
        try
        {
            lifetime = checked(expiresAtUnixMilliseconds - issuedAtUnixMilliseconds);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUnixMilliseconds),
                "Ticket lifetime is outside the supported range.");
        }

        if (lifetime is <= 0 or > WorldJoinTicketCodec.MaximumLifetimeMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUnixMilliseconds),
                $"Ticket lifetime must be between 1 and {WorldJoinTicketCodec.MaximumLifetimeMilliseconds} milliseconds.");
        }

        TicketId = ticketId;
        AccountId = accountId;
        CharacterId = characterId;
        WorldId = worldId;
        ChannelId = channelId;
        WorldInstanceId = worldInstanceId;
        IssuedAtUnixMilliseconds = issuedAtUnixMilliseconds;
        ExpiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
    }

    public JoinTicketId TicketId
    {
        get;
    }

    public AccountId AccountId
    {
        get;
    }

    public CharacterId CharacterId
    {
        get;
    }

    public WorldId WorldId
    {
        get;
    }

    public ChannelId ChannelId
    {
        get;
    }

    public WorldInstanceId WorldInstanceId
    {
        get;
    }

    public long IssuedAtUnixMilliseconds
    {
        get;
    }

    public long ExpiresAtUnixMilliseconds
    {
        get;
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
                "Ticket timestamps must be representable Unix millisecond values.");
        }
    }
}

public readonly record struct WorldJoinTicketAudience
{
    public WorldJoinTicketAudience(
        WorldId worldId,
        ChannelId channelId,
        WorldInstanceId worldInstanceId)
    {
        if (!worldId.IsValid)
            throw new ArgumentException("World identity is invalid.", nameof(worldId));
        if (!channelId.IsValid)
            throw new ArgumentException("Channel identity is invalid.", nameof(channelId));
        if (!worldInstanceId.IsValid)
            throw new ArgumentException("World instance identity must not be empty.", nameof(worldInstanceId));

        WorldId = worldId;
        ChannelId = channelId;
        WorldInstanceId = worldInstanceId;
    }

    public WorldId WorldId
    {
        get;
    }

    public ChannelId ChannelId
    {
        get;
    }

    public WorldInstanceId WorldInstanceId
    {
        get;
    }

    internal bool IsValid => WorldId.IsValid && ChannelId.IsValid && WorldInstanceId.IsValid;
}

public enum WorldJoinTicketValidationFailure
{
    InvalidTicket = 0,
    ExpiredTicket = 1,
    WrongDestination = 2,
}

public sealed class WorldJoinTicketValidationResult
{
    private WorldJoinTicketValidationResult(
        WorldJoinTicketClaims? claims,
        WorldJoinTicketValidationFailure? failure)
    {
        Claims = claims;
        Failure = failure;
    }

    public bool IsValid => Claims is not null;

    public WorldJoinTicketClaims? Claims
    {
        get;
    }

    public WorldJoinTicketValidationFailure? Failure
    {
        get;
    }

    public WorldJoinRejectionReason ToRejectionReason() => Failure switch
    {
        WorldJoinTicketValidationFailure.InvalidTicket => WorldJoinRejectionReason.InvalidTicket,
        WorldJoinTicketValidationFailure.ExpiredTicket => WorldJoinRejectionReason.ExpiredTicket,
        WorldJoinTicketValidationFailure.WrongDestination => WorldJoinRejectionReason.WrongDestination,
        null => throw new InvalidOperationException("A valid ticket has no rejection reason."),
        _ => throw new InvalidOperationException("Ticket validation returned an unsupported failure."),
    };

    internal static WorldJoinTicketValidationResult Accepted(WorldJoinTicketClaims claims) => new(claims, null);

    internal static WorldJoinTicketValidationResult Rejected(WorldJoinTicketValidationFailure failure) => new(null, failure);
}

public enum WorldJoinRejectionReason
{
    InvalidTicket = 0,
    ExpiredTicket = 1,
    AlreadyConsumed = 2,
    WrongDestination = 3,
    WorldNotAcceptingAdmissions = 4,
    IncompatibleProtocolVersion = 5,
}

public sealed class WorldJoinRequest
{
    public WorldJoinRequest(ProtocolVersion protocolVersion, string ticket)
    {
        if (protocolVersion.Value == 0)
            throw new ArgumentException("Gameplay protocol version must be valid.", nameof(protocolVersion));
        ArgumentException.ThrowIfNullOrWhiteSpace(ticket);
        if (ticket.Length > WorldJoinTicketCodec.MaximumTokenLength)
            throw new ArgumentOutOfRangeException(nameof(ticket), "Join ticket exceeds the protocol limit.");

        ProtocolVersion = protocolVersion;
        Ticket = ticket;
    }

    public ProtocolVersion ProtocolVersion
    {
        get;
    }

    public string Ticket
    {
        get;
    }
}

public sealed class WorldJoinAccepted
{
    public WorldJoinAccepted(ProtocolVersion selectedProtocolVersion, GameplaySessionId sessionId)
    {
        if (selectedProtocolVersion.Value == 0)
            throw new ArgumentException("Selected gameplay protocol version must be valid.", nameof(selectedProtocolVersion));
        if (!sessionId.IsValid)
            throw new ArgumentException("Gameplay session identity must not be empty.", nameof(sessionId));

        SelectedProtocolVersion = selectedProtocolVersion;
        SessionId = sessionId;
    }

    public ProtocolVersion SelectedProtocolVersion
    {
        get;
    }

    public GameplaySessionId SessionId
    {
        get;
    }
}

public sealed class WorldJoinRejected
{
    public WorldJoinRejected(WorldJoinRejectionReason reason)
    {
        if (!Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason));

        Reason = reason;
    }

    public WorldJoinRejectionReason Reason
    {
        get;
    }
}
