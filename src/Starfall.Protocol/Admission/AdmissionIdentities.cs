namespace Starfall.Protocol.Admission;

public readonly record struct JoinTicketId
{
    public JoinTicketId(Guid value)
    {
        AdmissionIdentity.RequireNonEmpty(value, nameof(value));
        Value = value;
    }

    public Guid Value
    {
        get;
    }

    internal bool IsValid => Value != Guid.Empty;

    public override string ToString() => Value.ToString("D");
}

public readonly record struct AccountId
{
    public AccountId(Guid value)
    {
        AdmissionIdentity.RequireNonEmpty(value, nameof(value));
        Value = value;
    }

    public Guid Value
    {
        get;
    }

    internal bool IsValid => Value != Guid.Empty;

    public override string ToString() => Value.ToString("D");
}

public readonly record struct CharacterId
{
    public CharacterId(Guid value)
    {
        AdmissionIdentity.RequireNonEmpty(value, nameof(value));
        Value = value;
    }

    public Guid Value
    {
        get;
    }

    internal bool IsValid => Value != Guid.Empty;

    public override string ToString() => Value.ToString("D");
}

public readonly record struct WorldInstanceId
{
    public WorldInstanceId(Guid value)
    {
        AdmissionIdentity.RequireNonEmpty(value, nameof(value));
        Value = value;
    }

    public Guid Value
    {
        get;
    }

    internal bool IsValid => Value != Guid.Empty;

    public override string ToString() => Value.ToString("D");
}

public readonly record struct GameplaySessionId
{
    public GameplaySessionId(Guid value)
    {
        AdmissionIdentity.RequireNonEmpty(value, nameof(value));
        Value = value;
    }

    public Guid Value
    {
        get;
    }

    internal bool IsValid => Value != Guid.Empty;

    public override string ToString() => Value.ToString("D");
}

public readonly record struct WorldId
{
    public WorldId(string value)
    {
        AdmissionIdentity.ValidateSemantic(value, nameof(value));
        Value = value;
    }

    public string Value
    {
        get;
    }

    internal bool IsValid => AdmissionIdentity.IsValidSemantic(Value);

    public override string ToString() => Value ?? string.Empty;
}

public readonly record struct ChannelId
{
    public ChannelId(string value)
    {
        AdmissionIdentity.ValidateSemantic(value, nameof(value));
        Value = value;
    }

    public string Value
    {
        get;
    }

    internal bool IsValid => AdmissionIdentity.IsValidSemantic(Value);

    public override string ToString() => Value ?? string.Empty;
}

internal static class AdmissionIdentity
{
    internal const int MaximumSemanticLength = 64;

    internal static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Admission identities must not be empty.", parameterName);
    }

    internal static void ValidateSemantic(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (!IsValidSemantic(value))
        {
            throw new ArgumentException(
                $"Admission identities must contain 1-{MaximumSemanticLength} lowercase ASCII letters, digits or underscores and begin with a letter.",
                parameterName);
        }
    }

    internal static bool IsValidSemantic(string? value) =>
        value is { Length: > 0 and <= MaximumSemanticLength } &&
        value[0] is >= 'a' and <= 'z' &&
        value.All(static character =>
            character is >= 'a' and <= 'z' ||
            character is >= '0' and <= '9' ||
            character == '_');
}
