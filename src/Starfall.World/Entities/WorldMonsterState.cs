using Starfall.Content.Zones;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Monsters;

namespace Starfall.World.Entities;

internal sealed class WorldMonsterState
{
    internal WorldMonsterState(
        Draft0MonsterBehaviorState behavior,
        int healthUnits,
        ulong spawnedAtTick)
        : this(behavior, healthUnits, healthUnits, spawnedAtTick)
    {
    }

    internal WorldMonsterState(
        Draft0MonsterBehaviorState behavior,
        int healthUnits,
        int maximumHealthUnits,
        ulong spawnedAtTick)
    {
        if (behavior.EntityId.Value == 0)
            throw new ArgumentException("Monster behavior state must be valid.", nameof(behavior));
        if (maximumHealthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumHealthUnits));
        if (healthUnits <= 0 || healthUnits > maximumHealthUnits)
            throw new ArgumentOutOfRangeException(nameof(healthUnits));

        Behavior = behavior;
        HealthUnits = healthUnits;
        MaximumHealthUnits = maximumHealthUnits;
        SpawnedAtTick = spawnedAtTick;
    }

    internal Draft0MonsterBehaviorState Behavior
    {
        get;
    }

    internal WorldEntityId EntityId => Behavior.EntityId;

    internal string CampId => Behavior.CampId;

    internal string SpawnId => Behavior.SpawnId;

    internal string ArchetypeId => Behavior.ArchetypeId;

    internal GroundPoint Position => Behavior.Position;

    internal int HealthUnits
    {
        get;
    }

    internal int MaximumHealthUnits
    {
        get;
    }

    internal ulong SpawnedAtTick
    {
        get;
    }

    internal WorldMonsterState WithHealth(int healthUnits) =>
        new(
            Behavior,
            healthUnits,
            MaximumHealthUnits,
            SpawnedAtTick);

    internal WorldMonsterState WithBehavior(Draft0MonsterBehaviorState behavior)
    {
        if (behavior.EntityId != EntityId ||
            !string.Equals(behavior.CampId, CampId, StringComparison.Ordinal) ||
            !string.Equals(behavior.SpawnId, SpawnId, StringComparison.Ordinal) ||
            !string.Equals(behavior.ArchetypeId, ArchetypeId, StringComparison.Ordinal) ||
            behavior.Home != Behavior.Home ||
            behavior.CollisionRadiusMetres != Behavior.CollisionRadiusMetres)
        {
            throw new ArgumentException(
                "Behavior replacement must preserve monster identity, ownership, home and collision radius.",
                nameof(behavior));
        }

        return new WorldMonsterState(behavior, HealthUnits, MaximumHealthUnits, SpawnedAtTick);
    }
}
