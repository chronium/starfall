using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Monsters;

public readonly record struct MonsterSnapshotSequence
{
    public MonsterSnapshotSequence(ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Monster snapshot sequences must be positive.");

        Value = value;
    }

    public ulong Value
    {
        get;
    }

    internal bool IsValid => Value != 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public readonly record struct MonsterArchetypeId
{
    public const int MaxByteLength = 64;

    public MonsterArchetypeId(string value)
    {
        Validate(value, nameof(value));
        Value = value;
    }

    public string Value
    {
        get;
    }

    internal bool IsValid => IsValidValue(Value);

    public override string ToString() => Value ?? string.Empty;

    internal static bool IsValidValue(string? value) =>
        value is { Length: > 0 and <= MaxByteLength } &&
        value[0] is >= 'a' and <= 'z' &&
        value.All(static character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    private static void Validate(string? value, string parameterName)
    {
        if (!IsValidValue(value))
        {
            throw new ArgumentException(
                $"Monster archetype identities must contain 1-{MaxByteLength} lowercase ASCII letters, digits or underscores and begin with a letter.",
                parameterName);
        }
    }
}

public enum MonsterBehaviorKind : byte
{
    Idle = 0,
    Pursuing = 1,
    Attacking = 2,
    Returning = 3,
}

public sealed class LiveMonsterSnapshot
{
    internal const float FacingLengthTolerance = 1e-4f;

    public LiveMonsterSnapshot(
        WorldEntityId entityId,
        MonsterArchetypeId archetypeId,
        GroundPosition position,
        Vector2 velocityMetresPerSecond,
        Vector2 facing,
        float collisionRadiusMetres,
        MonsterBehaviorKind behavior,
        WorldEntityId? targetEntityId,
        int currentHealthUnits,
        int maximumHealthUnits)
    {
        if (!entityId.IsValid)
            throw new ArgumentException("Monster entity identity must be valid.", nameof(entityId));
        if (!archetypeId.IsValid)
            throw new ArgumentException("Monster archetype identity must be valid.", nameof(archetypeId));
        if (!position.IsValid)
            throw new ArgumentException("Monster position must be finite.", nameof(position));
        if (!IsFinite(velocityMetresPerSecond))
            throw new ArgumentException("Monster velocity must be finite.", nameof(velocityMetresPerSecond));
        if (!IsValidFacing(facing))
            throw new ArgumentException("Monster facing must be finite and normalized.", nameof(facing));
        if (!float.IsFinite(collisionRadiusMetres) || collisionRadiusMetres <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(collisionRadiusMetres));
        if (!Enum.IsDefined(behavior))
            throw new ArgumentOutOfRangeException(nameof(behavior));
        if (targetEntityId is { } target && (!target.IsValid || target == entityId))
            throw new ArgumentException("A present target identity must be valid and distinct from the monster.", nameof(targetEntityId));
        if (behavior is MonsterBehaviorKind.Pursuing or MonsterBehaviorKind.Attacking)
        {
            if (targetEntityId is null)
                throw new ArgumentException("Pursuing and attacking monsters require a target.", nameof(targetEntityId));
        }
        else if (targetEntityId is not null)
        {
            throw new ArgumentException("Idle and returning monsters cannot retain a target.", nameof(targetEntityId));
        }
        if (maximumHealthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumHealthUnits));
        if (currentHealthUnits <= 0 || currentHealthUnits > maximumHealthUnits)
            throw new ArgumentOutOfRangeException(nameof(currentHealthUnits));

        EntityId = entityId;
        ArchetypeId = archetypeId;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
        CollisionRadiusMetres = collisionRadiusMetres;
        Behavior = behavior;
        TargetEntityId = targetEntityId;
        CurrentHealthUnits = currentHealthUnits;
        MaximumHealthUnits = maximumHealthUnits;
    }

    public WorldEntityId EntityId
    {
        get;
    }

    public MonsterArchetypeId ArchetypeId
    {
        get;
    }

    public GroundPosition Position
    {
        get;
    }

    public Vector2 VelocityMetresPerSecond
    {
        get;
    }

    public Vector2 Facing
    {
        get;
    }

    public float CollisionRadiusMetres
    {
        get;
    }

    public MonsterBehaviorKind Behavior
    {
        get;
    }

    public WorldEntityId? TargetEntityId
    {
        get;
    }

    public int CurrentHealthUnits
    {
        get;
    }

    public int MaximumHealthUnits
    {
        get;
    }

    internal static bool IsValidFacing(Vector2 value) =>
        IsFinite(value) && MathF.Abs(value.Length() - 1.0f) <= FacingLengthTolerance;

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}

public sealed class DefeatedMonsterSnapshot
{
    public DefeatedMonsterSnapshot(
        WorldEntityId entityId,
        MonsterArchetypeId archetypeId,
        GroundPosition lastPosition,
        Vector2 lastFacing,
        ulong defeatedAtTick)
    {
        if (!entityId.IsValid)
            throw new ArgumentException("Defeated monster identity must be valid.", nameof(entityId));
        if (!archetypeId.IsValid)
            throw new ArgumentException("Monster archetype identity must be valid.", nameof(archetypeId));
        if (!lastPosition.IsValid)
            throw new ArgumentException("Last monster position must be finite.", nameof(lastPosition));
        if (!LiveMonsterSnapshot.IsValidFacing(lastFacing))
            throw new ArgumentException("Last monster facing must be finite and normalized.", nameof(lastFacing));

        EntityId = entityId;
        ArchetypeId = archetypeId;
        LastPosition = lastPosition;
        LastFacing = lastFacing;
        DefeatedAtTick = defeatedAtTick;
    }

    public WorldEntityId EntityId
    {
        get;
    }

    public MonsterArchetypeId ArchetypeId
    {
        get;
    }

    public GroundPosition LastPosition
    {
        get;
    }

    public Vector2 LastFacing
    {
        get;
    }

    public ulong DefeatedAtTick
    {
        get;
    }
}

public sealed class BoundedMonsterSnapshot
{
    public const int MaxEntries = 10;

    public BoundedMonsterSnapshot(
        MonsterSnapshotSequence sequence,
        ulong simulationTick,
        IEnumerable<LiveMonsterSnapshot> liveMonsters,
        IEnumerable<DefeatedMonsterSnapshot> defeatedMonsters)
    {
        if (!sequence.IsValid)
            throw new ArgumentException("Monster snapshot sequence must be valid.", nameof(sequence));

        ImmutableArray<LiveMonsterSnapshot> copiedLive = Copy(liveMonsters, nameof(liveMonsters));
        ImmutableArray<DefeatedMonsterSnapshot> copiedDefeated = Copy(defeatedMonsters, nameof(defeatedMonsters));
        if (copiedLive.Length + copiedDefeated.Length > MaxEntries)
            throw new ArgumentException($"A monster snapshot can contain at most {MaxEntries} total entries.");

        ValidateStrictOrder(copiedLive, static monster => monster.EntityId, nameof(liveMonsters));
        ValidateStrictOrder(copiedDefeated, static monster => monster.EntityId, nameof(defeatedMonsters));

        var identities = new HashSet<ulong>();
        foreach (LiveMonsterSnapshot monster in copiedLive)
        {
            if (!identities.Add(monster.EntityId.Value))
                throw new ArgumentException("Monster identities must be unique across the snapshot.");
        }
        foreach (DefeatedMonsterSnapshot monster in copiedDefeated)
        {
            if (monster.DefeatedAtTick > simulationTick)
                throw new ArgumentException("A defeat tick cannot be later than the snapshot tick.", nameof(defeatedMonsters));
            if (!identities.Add(monster.EntityId.Value))
                throw new ArgumentException("Monster identities must be unique across the snapshot.");
        }

        Sequence = sequence;
        SimulationTick = simulationTick;
        LiveMonsters = copiedLive;
        DefeatedMonsters = copiedDefeated;
    }

    public MonsterSnapshotSequence Sequence
    {
        get;
    }

    public ulong SimulationTick
    {
        get;
    }

    public ImmutableArray<LiveMonsterSnapshot> LiveMonsters
    {
        get;
    }

    public ImmutableArray<DefeatedMonsterSnapshot> DefeatedMonsters
    {
        get;
    }

    private static ImmutableArray<T> Copy<T>(IEnumerable<T> values, string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values is ImmutableArray<T> immutable && immutable.IsDefault)
            throw new ArgumentException("Default immutable arrays are not valid input.", parameterName);

        ImmutableArray<T> copy = values.ToImmutableArray();
        if (copy.Any(static value => value is null))
            throw new ArgumentException("Monster snapshot collections cannot contain null entries.", parameterName);
        return copy;
    }

    private static void ValidateStrictOrder<T>(
        ImmutableArray<T> values,
        Func<T, WorldEntityId> identity,
        string parameterName)
    {
        for (int index = 1; index < values.Length; index++)
        {
            if (identity(values[index - 1]).Value >= identity(values[index]).Value)
                throw new ArgumentException("Monster entries must be strictly ordered by ascending entity identity.", parameterName);
        }
    }
}
