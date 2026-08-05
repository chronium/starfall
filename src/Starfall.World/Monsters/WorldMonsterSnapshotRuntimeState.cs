using Starfall.Protocol.Admission;
using Starfall.Protocol.Monsters;
using Starfall.World.Admission;

namespace Starfall.World.Monsters;

internal sealed class WorldMonsterSnapshotSessionState
{
    internal WorldMonsterSnapshotSessionState(WorldGameplaySession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal WorldGameplaySession Session
    {
        get;
    }

    internal MonsterSnapshotSequenceAllocator SnapshotSequences
    {
        get;
    } = new();

    internal ulong? LastPublishedTick
    {
        get; set;
    }
}

internal sealed record WorldMonsterSnapshot(
    GameplaySessionId SessionId,
    BoundedMonsterSnapshot Snapshot);
