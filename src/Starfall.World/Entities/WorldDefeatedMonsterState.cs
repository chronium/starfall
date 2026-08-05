using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;

namespace Starfall.World.Entities;

internal sealed class WorldDefeatedMonsterState
{
    internal WorldDefeatedMonsterState(
        WorldEntityId entityId,
        string spawnId,
        string archetypeId,
        GroundPoint lastPosition,
        Vector2 lastFacing,
        ulong defeatedAtTick)
    {
        if (entityId.Value == 0)
            throw new ArgumentException("Defeated monster identity must be valid.", nameof(entityId));
        ArgumentException.ThrowIfNullOrWhiteSpace(spawnId);
        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);
        if (!float.IsFinite(lastFacing.X) || !float.IsFinite(lastFacing.Y) ||
            MathF.Abs(lastFacing.Length() - 1.0f) > 1e-4f)
        {
            throw new ArgumentException("Defeated monster facing must be finite and normalized.", nameof(lastFacing));
        }

        EntityId = entityId;
        SpawnId = spawnId;
        ArchetypeId = archetypeId;
        LastPosition = lastPosition;
        LastFacing = lastFacing;
        DefeatedAtTick = defeatedAtTick;
    }

    internal WorldEntityId EntityId
    {
        get;
    }

    internal string SpawnId
    {
        get;
    }

    internal string ArchetypeId
    {
        get;
    }

    internal GroundPoint LastPosition
    {
        get;
    }

    internal Vector2 LastFacing
    {
        get;
    }

    internal ulong DefeatedAtTick
    {
        get;
    }
}
