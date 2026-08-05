using Starfall.Protocol.Monsters;

namespace Starfall.World.Monsters;

internal sealed class MonsterSnapshotSequenceAllocator
{
    private ulong nextValue;
    private bool exhausted;

    internal MonsterSnapshotSequenceAllocator(ulong firstValue = 1)
    {
        if (firstValue == 0)
            throw new ArgumentOutOfRangeException(nameof(firstValue), "The first monster snapshot sequence must be positive.");

        nextValue = firstValue;
    }

    internal MonsterSnapshotSequence Allocate()
    {
        if (exhausted)
            throw new InvalidOperationException("The monster snapshot sequence space is exhausted.");

        var sequence = new MonsterSnapshotSequence(nextValue);
        try
        {
            nextValue = checked(nextValue + 1);
        }
        catch (OverflowException)
        {
            exhausted = true;
        }

        return sequence;
    }
}
