using System.Globalization;

namespace Starfall.Protocol.Compatibility;

public readonly record struct ProtocolVersion
{
    public ProtocolVersion(byte value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Gameplay protocol versions must be positive.");

        Value = value;
    }

    public byte Value
    {
        get;
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public static class StarfallGameplayProtocol
{
    public static ProtocolVersion CurrentVersion { get; } = new(1);
}
