using Starfall.Simulation.Entities;

namespace Starfall.World.Entities;

internal sealed class WorldEntityIdSequence
{
    private ulong nextValue;
    private bool exhausted;

    internal WorldEntityIdSequence(ulong firstValue = 1)
    {
        if (firstValue == 0)
            throw new ArgumentOutOfRangeException(nameof(firstValue), "The first world entity identity must be positive.");

        nextValue = firstValue;
    }

    internal WorldEntityId Allocate()
    {
        if (exhausted)
            throw new InvalidOperationException("The world entity identity space is exhausted.");

        var identity = new WorldEntityId(nextValue);
        try
        {
            nextValue = checked(nextValue + 1);
        }
        catch (OverflowException)
        {
            exhausted = true;
        }

        return identity;
    }
}
