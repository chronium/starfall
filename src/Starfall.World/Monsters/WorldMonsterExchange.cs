using Starfall.Protocol.Admission;
using Starfall.Protocol.Monsters;
using Starfall.World.Lifecycle;

namespace Starfall.World.Monsters;

internal sealed record WorldMonsterSnapshotPublication(
    GameplaySessionId SessionId,
    byte[] Payload);

internal sealed class WorldMonsterExchange
{
    private readonly WorldChannelRuntime runtime;

    internal WorldMonsterExchange(WorldChannelRuntime runtime)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    internal IReadOnlyList<WorldMonsterSnapshotPublication> CaptureSnapshots() =>
        runtime.CaptureMonsterSnapshots()
            .Select(static publication => new WorldMonsterSnapshotPublication(
                publication.SessionId,
                BoundedMonsterSnapshotCodec.Encode(publication.Snapshot)))
            .ToArray();
}
