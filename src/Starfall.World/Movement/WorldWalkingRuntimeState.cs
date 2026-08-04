using Starfall.Protocol.Admission;
using Starfall.Protocol.Movement;
using Starfall.World.Admission;

namespace Starfall.World.Movement;

internal sealed class WorldWalkingSessionState
{
    internal WorldWalkingSessionState(WorldGameplaySession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal WorldGameplaySession Session
    {
        get;
    }

    internal MovementSnapshotSequenceAllocator SnapshotSequences { get; } = new();

    internal MovementIntentSequence? LastProcessedIntentSequence
    {
        get; set;
    }

    internal ulong? LastPublishedTick
    {
        get; set;
    }
}

internal sealed record WorldWalkingCommandResult(
    WorldWalkingCommandDisposition Disposition,
    PlayerMovementCorrection? Correction = null);

internal sealed record WorldWalkingSnapshot(
    GameplaySessionId SessionId,
    PlayerMovementSnapshot Snapshot);
