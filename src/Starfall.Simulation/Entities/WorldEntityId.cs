using System.Globalization;

namespace Starfall.Simulation.Entities;

public readonly record struct WorldEntityId
{
    public WorldEntityId(ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "World entity identities must be positive.");

        Value = value;
    }

    public ulong Value
    {
        get;
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
