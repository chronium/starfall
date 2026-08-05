using Starfall.Content.Zones;
using Starfall.Simulation.Entities;

namespace Starfall.World.Entities;

internal sealed class WorldMonsterState
{
    internal WorldMonsterState(
        WorldEntityId entityId,
        string campId,
        string spawnId,
        string archetypeId,
        GroundPoint position,
        int healthUnits,
        ulong spawnedAtTick)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campId);
        ArgumentException.ThrowIfNullOrWhiteSpace(spawnId);
        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);
        if (healthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(healthUnits));

        EntityId = entityId;
        CampId = campId;
        SpawnId = spawnId;
        ArchetypeId = archetypeId;
        Position = position;
        HealthUnits = healthUnits;
        SpawnedAtTick = spawnedAtTick;
    }

    internal WorldEntityId EntityId
    {
        get;
    }

    internal string CampId
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

    internal GroundPoint Position
    {
        get;
    }

    internal int HealthUnits
    {
        get;
    }

    internal ulong SpawnedAtTick
    {
        get;
    }

    internal WorldMonsterState WithHealth(int healthUnits) =>
        new(
            EntityId,
            CampId,
            SpawnId,
            ArchetypeId,
            Position,
            healthUnits,
            SpawnedAtTick);
}
