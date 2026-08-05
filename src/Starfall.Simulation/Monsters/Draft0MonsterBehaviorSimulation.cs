using System.Collections.Immutable;
using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Movement;

namespace Starfall.Simulation.Monsters;

public enum Draft0MonsterBehaviorMode
{
    Idle,
    Pursuing,
    Attacking,
    Returning,
}

public sealed class Draft0MonsterBehaviorTuning
{
    public Draft0MonsterBehaviorTuning(
        string archetypeId,
        float collisionRadiusMetres,
        float movementSpeedMetresPerSecond,
        float awarenessRadiusMetres,
        float attackRangeMetres,
        int outgoingDamageUnits,
        ulong attackCadenceTicks)
    {
        MonsterBehaviorIdentityRules.Validate(archetypeId, nameof(archetypeId));
        RequirePositiveFinite(collisionRadiusMetres, nameof(collisionRadiusMetres));
        RequirePositiveFinite(movementSpeedMetresPerSecond, nameof(movementSpeedMetresPerSecond));
        RequirePositiveFinite(awarenessRadiusMetres, nameof(awarenessRadiusMetres));
        RequirePositiveFinite(attackRangeMetres, nameof(attackRangeMetres));
        if (attackRangeMetres < collisionRadiusMetres)
            throw new ArgumentOutOfRangeException(nameof(attackRangeMetres));
        if (outgoingDamageUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(outgoingDamageUnits));
        if (attackCadenceTicks == 0)
            throw new ArgumentOutOfRangeException(nameof(attackCadenceTicks));

        ArchetypeId = archetypeId;
        CollisionRadiusMetres = collisionRadiusMetres;
        MovementSpeedMetresPerSecond = movementSpeedMetresPerSecond;
        AwarenessRadiusMetres = awarenessRadiusMetres;
        AttackRangeMetres = attackRangeMetres;
        OutgoingDamageUnits = outgoingDamageUnits;
        AttackCadenceTicks = attackCadenceTicks;
    }

    public string ArchetypeId
    {
        get;
    }

    public float CollisionRadiusMetres
    {
        get;
    }

    public float MovementSpeedMetresPerSecond
    {
        get;
    }

    public float AwarenessRadiusMetres
    {
        get;
    }

    public float AttackRangeMetres
    {
        get;
    }

    public int OutgoingDamageUnits
    {
        get;
    }

    public ulong AttackCadenceTicks
    {
        get;
    }

    private static void RequirePositiveFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0.0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}

public sealed class Draft0MonsterBehaviorTuningCatalog
{
    private readonly IReadOnlyDictionary<string, Draft0MonsterBehaviorTuning> byArchetypeId;

    public Draft0MonsterBehaviorTuningCatalog(IEnumerable<Draft0MonsterBehaviorTuning> tunings)
    {
        ArgumentNullException.ThrowIfNull(tunings);
        if (tunings is ImmutableArray<Draft0MonsterBehaviorTuning> immutableTunings && immutableTunings.IsDefault)
            throw new ArgumentException("Default immutable arrays are not valid input.", nameof(tunings));

        ImmutableArray<Draft0MonsterBehaviorTuning> copy = tunings.ToImmutableArray();
        if (copy.IsEmpty || copy.Any(static tuning => tuning is null))
            throw new ArgumentException("A monster behavior catalog requires non-null tunings.", nameof(tunings));
        if (copy.Select(static tuning => tuning.ArchetypeId).Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException("Monster behavior archetype identities must be unique.", nameof(tunings));

        Tunings = copy;
        byArchetypeId = copy.ToDictionary(static tuning => tuning.ArchetypeId, StringComparer.Ordinal);
    }

    public ImmutableArray<Draft0MonsterBehaviorTuning> Tunings
    {
        get;
    }

    public Draft0MonsterBehaviorTuning GetRequired(string archetypeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);
        if (!byArchetypeId.TryGetValue(archetypeId, out Draft0MonsterBehaviorTuning? tuning))
            throw new KeyNotFoundException($"No monster behavior tuning exists for '{archetypeId}'.");
        return tuning;
    }
}

public static class Draft0MonsterBehaviorTunings
{
    public static Draft0MonsterBehaviorTuningCatalog FirstPlayable
    {
        get;
    } =
        new(
        [
            new Draft0MonsterBehaviorTuning(
                "starter_flyer_light",
                0.45f,
                2.5f,
                10.0f,
                1.25f,
                100,
                60),
            new Draft0MonsterBehaviorTuning(
                "starter_flyer_heavy",
                0.65f,
                1.8f,
                12.0f,
                1.5f,
                200,
                90),
        ]);
}

public readonly record struct Draft0MonsterPlayerTarget
{
    public Draft0MonsterPlayerTarget(WorldEntityId entityId, GroundPoint position)
    {
        if (entityId.Value == 0)
            throw new ArgumentException("Player target identity must be valid.", nameof(entityId));
        EntityId = entityId;
        Position = position;
    }

    public WorldEntityId EntityId
    {
        get;
    }

    public GroundPoint Position
    {
        get;
    }
}

public readonly record struct Draft0MonsterBehaviorState
{
    private const float FacingLengthTolerance = 1e-4f;

    public Draft0MonsterBehaviorState(
        WorldEntityId entityId,
        string campId,
        string spawnId,
        string archetypeId,
        GroundPoint home,
        GroundPoint position,
        Vector2 velocityMetresPerSecond,
        Vector2 facing,
        float collisionRadiusMetres,
        Draft0MonsterBehaviorMode mode,
        WorldEntityId? targetEntityId,
        ulong nextAllowedAttackTick)
    {
        if (entityId.Value == 0)
            throw new ArgumentException("Monster identity must be valid.", nameof(entityId));
        MonsterBehaviorIdentityRules.Validate(campId, nameof(campId));
        MonsterBehaviorIdentityRules.Validate(spawnId, nameof(spawnId));
        MonsterBehaviorIdentityRules.Validate(archetypeId, nameof(archetypeId));
        if (!IsFinite(velocityMetresPerSecond))
            throw new ArgumentException("Monster velocity must be finite.", nameof(velocityMetresPerSecond));
        if (!IsFinite(facing) || MathF.Abs(facing.Length() - 1.0f) > FacingLengthTolerance)
            throw new ArgumentException("Monster facing must be finite and normalized.", nameof(facing));
        if (!float.IsFinite(collisionRadiusMetres) || collisionRadiusMetres <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(collisionRadiusMetres));
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode));
        if (targetEntityId is { Value: 0 })
            throw new ArgumentException("A present target identity must be valid.", nameof(targetEntityId));

        EntityId = entityId;
        CampId = campId;
        SpawnId = spawnId;
        ArchetypeId = archetypeId;
        Home = home;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
        CollisionRadiusMetres = collisionRadiusMetres;
        Mode = mode;
        TargetEntityId = targetEntityId;
        NextAllowedAttackTick = nextAllowedAttackTick;
    }

    public WorldEntityId EntityId
    {
        get;
    }

    public string CampId
    {
        get;
    }

    public string SpawnId
    {
        get;
    }

    public string ArchetypeId
    {
        get;
    }

    public GroundPoint Home
    {
        get;
    }

    public GroundPoint Position
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

    public Draft0MonsterBehaviorMode Mode
    {
        get;
    }

    public WorldEntityId? TargetEntityId
    {
        get;
    }

    public ulong NextAllowedAttackTick
    {
        get;
    }

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}

public readonly record struct Draft0MonsterAttackResolution
{
    public Draft0MonsterAttackResolution(
        WorldEntityId attackerEntityId,
        WorldEntityId targetEntityId,
        ulong resolvedAtTick,
        int requestedDamageUnits)
    {
        if (attackerEntityId.Value == 0)
            throw new ArgumentException("Monster attacker identity must be valid.", nameof(attackerEntityId));
        if (targetEntityId.Value == 0)
            throw new ArgumentException("Player target identity must be valid.", nameof(targetEntityId));
        if (attackerEntityId == targetEntityId)
            throw new ArgumentException("A monster cannot attack itself.", nameof(targetEntityId));
        if (requestedDamageUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedDamageUnits));

        AttackerEntityId = attackerEntityId;
        TargetEntityId = targetEntityId;
        ResolvedAtTick = resolvedAtTick;
        RequestedDamageUnits = requestedDamageUnits;
    }

    public WorldEntityId AttackerEntityId
    {
        get;
    }

    public WorldEntityId TargetEntityId
    {
        get;
    }

    public ulong ResolvedAtTick
    {
        get;
    }

    public int RequestedDamageUnits
    {
        get;
    }
}

public sealed class Draft0MonsterBehaviorStep
{
    internal Draft0MonsterBehaviorStep(
        ImmutableArray<Draft0MonsterBehaviorState> monsters,
        ImmutableArray<Draft0MonsterAttackResolution> attacks)
    {
        Monsters = monsters;
        Attacks = attacks;
    }

    public ImmutableArray<Draft0MonsterBehaviorState> Monsters
    {
        get;
    }

    public ImmutableArray<Draft0MonsterAttackResolution> Attacks
    {
        get;
    }
}

public sealed class Draft0MonsterBehaviorSimulation : IDisposable
{
    private readonly IReadOnlyDictionary<string, Draft0CampLayout> camps;
    private readonly Draft0MonsterBehaviorTuningCatalog tunings;
    private readonly Draft0GroundCollisionWorld collisionWorld;
    private readonly bool advancesCollisionWorld;
    private readonly bool ownsCollisionWorld;
    private readonly Dictionary<WorldEntityId, MonsterMover> monsters = [];
    private bool disposed;

    public Draft0MonsterBehaviorSimulation(
        Draft0GrayboxLayout layout,
        Draft0MonsterBehaviorTuningCatalog tunings)
        : this(
            layout ?? throw new ArgumentNullException(nameof(layout)),
            tunings ?? throw new ArgumentNullException(nameof(tunings)),
            new Draft0GroundCollisionWorld(layout),
            advancesCollisionWorld: true,
            ownsCollisionWorld: true)
    {
    }

    internal Draft0MonsterBehaviorSimulation(
        Draft0GrayboxLayout layout,
        Draft0MonsterBehaviorTuningCatalog tunings,
        Draft0GroundCollisionWorld collisionWorld,
        bool advancesCollisionWorld,
        bool ownsCollisionWorld)
    {
        ArgumentNullException.ThrowIfNull(layout);
        this.tunings = tunings ?? throw new ArgumentNullException(nameof(tunings));
        camps = layout.Branches.ToDictionary(static branch => branch.Camp.Id, static branch => branch.Camp, StringComparer.Ordinal);
        this.collisionWorld = collisionWorld ?? throw new ArgumentNullException(nameof(collisionWorld));
        this.advancesCollisionWorld = advancesCollisionWorld;
        this.ownsCollisionWorld = ownsCollisionWorld;
    }

    public int MonsterCount
    {
        get
        {
            ThrowIfDisposed();
            return monsters.Count;
        }
    }

    public Draft0MonsterBehaviorState RegisterMonster(
        WorldEntityId entityId,
        string campId,
        string spawnId,
        string archetypeId,
        GroundPoint home)
    {
        ThrowIfDisposed();
        if (entityId.Value == 0)
            throw new ArgumentException("Monster identity must be valid.", nameof(entityId));
        MonsterBehaviorIdentityRules.Validate(campId, nameof(campId));
        MonsterBehaviorIdentityRules.Validate(spawnId, nameof(spawnId));
        MonsterBehaviorIdentityRules.Validate(archetypeId, nameof(archetypeId));
        if (monsters.ContainsKey(entityId))
            throw new InvalidOperationException($"Monster {entityId} is already registered for behavior.");
        if (!camps.TryGetValue(campId, out Draft0CampLayout? camp))
            throw new ArgumentException($"Unknown Draft 0 camp '{campId}'.", nameof(campId));

        Draft0MonsterBehaviorTuning tuning = tunings.GetRequired(archetypeId);
        if (!ContainsMonsterCenter(camp, home, tuning.CollisionRadiusMetres))
            throw new ArgumentOutOfRangeException(nameof(home), home, "Monster home must lie inside its radius-inset camp footprint.");
        if (collisionWorld.OverlapsProxy(home, tuning.CollisionRadiusMetres))
            throw new ArgumentException("Monster home overlaps collidable proxy geometry.", nameof(home));

        Vector2 facing = DirectionOrFallback(home, camp.EntryAnchor, Vector2.UnitY);
        var state = new Draft0MonsterBehaviorState(
            entityId,
            campId,
            spawnId,
            archetypeId,
            home,
            home,
            Vector2.Zero,
            facing,
            tuning.CollisionRadiusMetres,
            Draft0MonsterBehaviorMode.Idle,
            null,
            0);
        monsters.Add(entityId, new MonsterMover(state));
        return state;
    }

    public bool RemoveMonster(WorldEntityId entityId)
    {
        ThrowIfDisposed();
        return monsters.Remove(entityId);
    }

    public bool TryGetMonster(WorldEntityId entityId, out Draft0MonsterBehaviorState state)
    {
        ThrowIfDisposed();
        if (monsters.TryGetValue(entityId, out MonsterMover? monster))
        {
            state = monster.State;
            return true;
        }

        state = default;
        return false;
    }

    public Draft0MonsterBehaviorStep Step(
        IEnumerable<Draft0MonsterPlayerTarget> playerTargets,
        ulong currentTick)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(playerTargets);
        if (playerTargets is ImmutableArray<Draft0MonsterPlayerTarget> immutablePlayers && immutablePlayers.IsDefault)
            throw new ArgumentException("Default immutable arrays are not valid input.", nameof(playerTargets));

        Draft0MonsterPlayerTarget[] players = playerTargets
            .OrderBy(static player => player.EntityId.Value)
            .ToArray();
        if (players.Select(static player => player.EntityId).Distinct().Count() != players.Length)
            throw new ArgumentException("Player target identities must be unique.", nameof(playerTargets));
        if (players.Any(player => monsters.ContainsKey(player.EntityId)))
            throw new ArgumentException("Player and monster identities must be disjoint.", nameof(playerTargets));

        IReadOnlyDictionary<WorldEntityId, Draft0MonsterPlayerTarget> playersById =
            players.ToDictionary(static player => player.EntityId);
        if (advancesCollisionWorld)
            collisionWorld.Step();

        var attacks = ImmutableArray.CreateBuilder<Draft0MonsterAttackResolution>();
        foreach (MonsterMover monster in monsters.OrderBy(static pair => pair.Key.Value).Select(static pair => pair.Value))
        {
            if (Advance(monster, players, playersById, currentTick) is { } attack)
                attacks.Add(attack);
        }

        ImmutableArray<Draft0MonsterBehaviorState> states = monsters
            .OrderBy(static pair => pair.Key.Value)
            .Select(static pair => pair.Value.State)
            .ToImmutableArray();
        return new Draft0MonsterBehaviorStep(states, attacks.ToImmutable());
    }

    public void Clear()
    {
        ThrowIfDisposed();
        monsters.Clear();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        monsters.Clear();
        if (ownsCollisionWorld)
            collisionWorld.Dispose();
        disposed = true;
    }

    private Draft0MonsterAttackResolution? Advance(
        MonsterMover monster,
        IReadOnlyList<Draft0MonsterPlayerTarget> players,
        IReadOnlyDictionary<WorldEntityId, Draft0MonsterPlayerTarget> playersById,
        ulong currentTick)
    {
        Draft0MonsterBehaviorState state = monster.State;
        Draft0CampLayout camp = camps[state.CampId];
        Draft0MonsterBehaviorTuning tuning = tunings.GetRequired(state.ArchetypeId);

        if (state.Mode == Draft0MonsterBehaviorMode.Returning)
        {
            AdvanceReturn(monster, camp, tuning);
            return null;
        }

        Draft0MonsterPlayerTarget? target = null;
        if (state.TargetEntityId is { } retainedId &&
            playersById.TryGetValue(retainedId, out Draft0MonsterPlayerTarget retained) &&
            camp.Contains(retained.Position))
        {
            target = retained;
        }

        if (state.TargetEntityId is not null && target is null)
        {
            monster.State = WithState(
                state,
                state.Position,
                Vector2.Zero,
                state.Facing,
                Draft0MonsterBehaviorMode.Returning,
                null,
                state.NextAllowedAttackTick);
            AdvanceReturn(monster, camp, tuning);
            return null;
        }

        target ??= SelectTarget(state, camp, tuning, players);
        if (target is null)
        {
            monster.State = WithState(
                state,
                state.Position,
                Vector2.Zero,
                state.Facing,
                Draft0MonsterBehaviorMode.Idle,
                null,
                state.NextAllowedAttackTick);
            return null;
        }

        Vector2 difference = ToPlane(target.Value.Position) - ToPlane(state.Position);
        float distance = difference.Length();
        Vector2 facing = distance == 0.0f ? state.Facing : difference / distance;
        if (distance > tuning.AttackRangeMetres + Draft0GrayboxLayout.ValidationToleranceMetres)
        {
            monster.State = MoveToward(
                state,
                camp,
                tuning,
                target.Value.Position,
                tuning.AttackRangeMetres,
                Draft0MonsterBehaviorMode.Pursuing,
                target.Value.EntityId);
            return null;
        }

        ulong nextAllowedAttackTick = state.NextAllowedAttackTick;
        Draft0MonsterAttackResolution? attack = null;
        if (currentTick >= nextAllowedAttackTick)
        {
            nextAllowedAttackTick = checked(currentTick + tuning.AttackCadenceTicks);
            attack = new Draft0MonsterAttackResolution(
                state.EntityId,
                target.Value.EntityId,
                currentTick,
                tuning.OutgoingDamageUnits);
        }

        monster.State = WithState(
            state,
            state.Position,
            Vector2.Zero,
            facing,
            Draft0MonsterBehaviorMode.Attacking,
            target.Value.EntityId,
            nextAllowedAttackTick);
        return attack;
    }

    private void AdvanceReturn(
        MonsterMover monster,
        Draft0CampLayout camp,
        Draft0MonsterBehaviorTuning tuning)
    {
        Draft0MonsterBehaviorState state = monster.State;
        float distance = Vector2.Distance(ToPlane(state.Position), ToPlane(state.Home));
        if (distance <= Draft0GrayboxLayout.ValidationToleranceMetres)
        {
            monster.State = WithState(
                state,
                state.Home,
                Vector2.Zero,
                DirectionOrFallback(state.Home, camp.EntryAnchor, state.Facing),
                Draft0MonsterBehaviorMode.Idle,
                null,
                state.NextAllowedAttackTick);
            return;
        }

        Draft0MonsterBehaviorState moved = MoveToward(
            state,
            camp,
            tuning,
            state.Home,
            0.0f,
            Draft0MonsterBehaviorMode.Returning,
            null);
        if (moved.Position == state.Home)
        {
            moved = WithState(
                moved,
                moved.Home,
                Vector2.Zero,
                DirectionOrFallback(moved.Home, camp.EntryAnchor, moved.Facing),
                Draft0MonsterBehaviorMode.Idle,
                null,
                moved.NextAllowedAttackTick);
        }

        monster.State = moved;
    }

    private Draft0MonsterBehaviorState MoveToward(
        Draft0MonsterBehaviorState state,
        Draft0CampLayout camp,
        Draft0MonsterBehaviorTuning tuning,
        GroundPoint target,
        float stopDistanceMetres,
        Draft0MonsterBehaviorMode mode,
        WorldEntityId? targetEntityId)
    {
        Vector2 current = ToPlane(state.Position);
        Vector2 difference = ToPlane(target) - current;
        float distance = difference.Length();
        if (distance == 0.0f || distance <= stopDistanceMetres)
        {
            return WithState(
                state,
                state.Position,
                Vector2.Zero,
                state.Facing,
                mode,
                targetEntityId,
                state.NextAllowedAttackTick);
        }

        Vector2 direction = difference / distance;
        float maximumStep = tuning.MovementSpeedMetresPerSecond / Draft0PlayerMovementSimulation.TickRateHz;
        float stepLength = MathF.Min(distance - stopDistanceMetres, maximumStep);
        Vector2 requested = direction * stepLength;
        GroundPoint unclamped = new(current.X + requested.X, current.Y + requested.Y);
        GroundPoint clamped = ClampToCampInset(camp, unclamped, tuning.CollisionRadiusMetres);
        Vector2 campTranslation = ToPlane(clamped) - current;
        float fraction = collisionWorld.CastRoundGroundMover(
            state.Position,
            tuning.CollisionRadiusMetres,
            campTranslation);
        Vector2 actualTranslation = campTranslation * fraction;
        GroundPoint position = new(current.X + actualTranslation.X, current.Y + actualTranslation.Y);
        if (Vector2.DistanceSquared(ToPlane(position), ToPlane(target)) <=
            Draft0GrayboxLayout.ValidationToleranceMetres * Draft0GrayboxLayout.ValidationToleranceMetres)
        {
            position = target;
        }

        return WithState(
            state,
            position,
            actualTranslation * Draft0PlayerMovementSimulation.TickRateHz,
            direction,
            mode,
            targetEntityId,
            state.NextAllowedAttackTick);
    }

    private static Draft0MonsterPlayerTarget? SelectTarget(
        Draft0MonsterBehaviorState monster,
        Draft0CampLayout camp,
        Draft0MonsterBehaviorTuning tuning,
        IReadOnlyList<Draft0MonsterPlayerTarget> players)
    {
        float maximumDistanceSquared = tuning.AwarenessRadiusMetres * tuning.AwarenessRadiusMetres;
        return players
            .Where(player => camp.Contains(player.Position))
            .Select(player => new
            {
                Player = player,
                DistanceSquared = Vector2.DistanceSquared(ToPlane(monster.Position), ToPlane(player.Position)),
            })
            .Where(candidate => candidate.DistanceSquared <= maximumDistanceSquared)
            .OrderBy(static candidate => candidate.DistanceSquared)
            .ThenBy(static candidate => candidate.Player.EntityId.Value)
            .Select(static candidate => (Draft0MonsterPlayerTarget?)candidate.Player)
            .FirstOrDefault();
    }

    private static bool ContainsMonsterCenter(
        Draft0CampLayout camp,
        GroundPoint point,
        float radiusMetres)
    {
        if (camp.Geometry == Draft0CampGeometry.BroadOpenCircle)
        {
            float maximumRadius = camp.RadiusMetres - radiusMetres;
            return maximumRadius >= 0.0f &&
                Vector2.DistanceSquared(ToPlane(point), ToPlane(camp.Center)) <=
                (maximumRadius + Draft0GrayboxLayout.ValidationToleranceMetres) *
                (maximumRadius + Draft0GrayboxLayout.ValidationToleranceMetres);
        }

        return point.XMetres >= camp.Bounds.Minimum.XMetres + radiusMetres &&
            point.XMetres <= camp.Bounds.Maximum.XMetres - radiusMetres &&
            point.ZMetres >= camp.Bounds.Minimum.ZMetres + radiusMetres &&
            point.ZMetres <= camp.Bounds.Maximum.ZMetres - radiusMetres;
    }

    private static GroundPoint ClampToCampInset(
        Draft0CampLayout camp,
        GroundPoint point,
        float radiusMetres)
    {
        if (camp.Geometry != Draft0CampGeometry.BroadOpenCircle)
        {
            return new GroundPoint(
                Math.Clamp(
                    point.XMetres,
                    camp.Bounds.Minimum.XMetres + radiusMetres,
                    camp.Bounds.Maximum.XMetres - radiusMetres),
                Math.Clamp(
                    point.ZMetres,
                    camp.Bounds.Minimum.ZMetres + radiusMetres,
                    camp.Bounds.Maximum.ZMetres - radiusMetres));
        }

        Vector2 center = ToPlane(camp.Center);
        Vector2 offset = ToPlane(point) - center;
        float maximumRadius = camp.RadiusMetres - radiusMetres;
        if (offset.LengthSquared() <= maximumRadius * maximumRadius)
            return point;
        if (offset == Vector2.Zero)
            return camp.Center;
        Vector2 clamped = center + (Vector2.Normalize(offset) * maximumRadius);
        return new GroundPoint(clamped.X, clamped.Y);
    }

    private static Draft0MonsterBehaviorState WithState(
        Draft0MonsterBehaviorState previous,
        GroundPoint position,
        Vector2 velocity,
        Vector2 facing,
        Draft0MonsterBehaviorMode mode,
        WorldEntityId? targetEntityId,
        ulong nextAllowedAttackTick) =>
        new(
            previous.EntityId,
            previous.CampId,
            previous.SpawnId,
            previous.ArchetypeId,
            previous.Home,
            position,
            velocity,
            facing,
            previous.CollisionRadiusMetres,
            mode,
            targetEntityId,
            nextAllowedAttackTick);

    private static Vector2 DirectionOrFallback(
        GroundPoint from,
        GroundPoint to,
        Vector2 fallback)
    {
        Vector2 difference = ToPlane(to) - ToPlane(from);
        return difference == Vector2.Zero ? fallback : Vector2.Normalize(difference);
    }

    private static Vector2 ToPlane(GroundPoint point) =>
        new(point.XMetres, point.ZMetres);

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);

    private sealed class MonsterMover(Draft0MonsterBehaviorState state)
    {
        internal Draft0MonsterBehaviorState State { get; set; } = state;
    }
}

internal static class MonsterBehaviorIdentityRules
{
    internal static void Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value[0] is < 'a' or > 'z' || value.Any(static character =>
                character is not (>= 'a' and <= 'z') &&
                character is not (>= '0' and <= '9') &&
                character != '_'))
        {
            throw new ArgumentException(
                "Monster behavior identities must use lowercase ASCII letters, digits and underscores and begin with a letter.",
                parameterName);
        }
    }
}
