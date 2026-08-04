using Starfall.Protocol.Movement;

namespace Starfall.World.Movement;

internal sealed class MovementSnapshotSequenceAllocator
{
    private ulong nextValue;
    private bool exhausted;

    internal MovementSnapshotSequenceAllocator(ulong firstValue = 1)
    {
        if (firstValue == 0)
            throw new ArgumentOutOfRangeException(nameof(firstValue), "The first movement snapshot sequence must be positive.");

        nextValue = firstValue;
    }

    internal MovementSnapshotSequence Allocate()
    {
        if (exhausted)
            throw new InvalidOperationException("The movement snapshot sequence space is exhausted.");

        var sequence = new MovementSnapshotSequence(nextValue);
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
