using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using ChronoFall.CharacterPresentation;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Monsters;

namespace Starfall.Client;

internal readonly record struct Draft0MonsterPresentationSnapshot
{
    private const float FacingLengthTolerance = 1e-4f;

    internal Draft0MonsterPresentationSnapshot(
        string identity,
        string archetypeId,
        ulong tick,
        GroundPoint position,
        Vector2 velocityMetresPerSecond,
        Vector2 facing,
        float? collisionRadiusMetres,
        MonsterBehaviorKind behavior,
        string? targetIdentity,
        int currentHealthUnits,
        int maximumHealthUnits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);
        if (!float.IsFinite(velocityMetresPerSecond.X) ||
            !float.IsFinite(velocityMetresPerSecond.Y))
        {
            throw new ArgumentException("Monster velocity must be finite.", nameof(velocityMetresPerSecond));
        }
        if (!float.IsFinite(facing.X) ||
            !float.IsFinite(facing.Y) ||
            MathF.Abs(facing.Length() - 1.0f) > FacingLengthTolerance)
        {
            throw new ArgumentException("Monster facing must be finite and normalized.", nameof(facing));
        }
        if (collisionRadiusMetres is { } radius && (!float.IsFinite(radius) || radius <= 0.0f))
            throw new ArgumentOutOfRangeException(nameof(collisionRadiusMetres));
        if (!Enum.IsDefined(behavior))
            throw new ArgumentOutOfRangeException(nameof(behavior));
        if (behavior is MonsterBehaviorKind.Pursuing or MonsterBehaviorKind.Attacking)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(targetIdentity);
            if (string.Equals(identity, targetIdentity, StringComparison.Ordinal))
                throw new ArgumentException("A monster cannot target itself.", nameof(targetIdentity));
        }
        else if (targetIdentity is not null)
        {
            throw new ArgumentException("Idle and returning monsters cannot retain a target.", nameof(targetIdentity));
        }
        if (maximumHealthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumHealthUnits));
        if (currentHealthUnits <= 0 || currentHealthUnits > maximumHealthUnits)
            throw new ArgumentOutOfRangeException(nameof(currentHealthUnits));

        Identity = identity;
        ArchetypeId = archetypeId;
        Tick = tick;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
        CollisionRadiusMetres = collisionRadiusMetres;
        Behavior = behavior;
        TargetIdentity = targetIdentity;
        CurrentHealthUnits = currentHealthUnits;
        MaximumHealthUnits = maximumHealthUnits;
    }

    internal string Identity
    {
        get;
    }

    internal string ArchetypeId
    {
        get;
    }

    internal ulong Tick
    {
        get;
    }

    internal GroundPoint Position
    {
        get;
    }

    internal Vector2 VelocityMetresPerSecond
    {
        get;
    }

    internal Vector2 Facing
    {
        get;
    }

    internal float? CollisionRadiusMetres
    {
        get;
    }

    internal MonsterBehaviorKind Behavior
    {
        get;
    }

    internal string? TargetIdentity
    {
        get;
    }

    internal int CurrentHealthUnits
    {
        get;
    }

    internal int MaximumHealthUnits
    {
        get;
    }
}

internal readonly record struct Draft0MonsterPresentationState(
    Draft0MonsterPresentationSnapshot Snapshot,
    Matrix4x4 World,
    Vector3 BaseColor,
    float UniformScaleMetres);

internal readonly record struct Draft0MonsterDefeatPresentationSnapshot(
    string Identity,
    string ArchetypeId,
    ulong DefeatedAtTick,
    GroundPoint LastPosition,
    Vector2 LastFacing);

internal readonly record struct Draft0MonsterDefeatPresentationState(
    Draft0MonsterDefeatPresentationSnapshot Snapshot,
    Matrix4x4 World,
    Vector3 BaseColor);

internal static class Draft0LocalMonsterFixture
{
    internal const int ExpectedCount = 10;
    internal const string IdentityPrefix = "fixture_";

    internal static IReadOnlyList<Draft0MonsterPresentationSnapshot> Create(
        Draft0GrayboxLayout layout,
        Draft0StarterMonsterCatalogDefinition catalog)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(catalog);
        if (layout.Branches.Count != catalog.Camps.Length)
            throw new ArgumentException("The local monster fixture requires matching layout and catalog camps.");

        Dictionary<string, int> healthByArchetype = catalog.Archetypes.ToDictionary(
            static archetype => archetype.Id,
            static archetype => archetype.AuthoritativeHealthUnits,
            StringComparer.Ordinal);

        var snapshots = new List<Draft0MonsterPresentationSnapshot>(ExpectedCount);
        for (var campIndex = 0; campIndex < catalog.Camps.Length; campIndex++)
        {
            Draft0BranchLayout branch = layout.Branches[campIndex];
            Draft0CampCompositionDefinition camp = catalog.Camps[campIndex];
            if (!string.Equals(branch.Camp.Id, camp.CampId, StringComparison.Ordinal) ||
                branch.SampleSpawns.Count != camp.Assignments.Length)
            {
                throw new ArgumentException("The local monster fixture requires matching camp identities and assignments.");
            }

            for (var assignmentIndex = 0; assignmentIndex < camp.Assignments.Length; assignmentIndex++)
            {
                Draft0CampSpawnAssignment assignment = camp.Assignments[assignmentIndex];
                Draft0SampleSpawn spawn = branch.SampleSpawns[assignmentIndex];
                if (!string.Equals(spawn.Id, assignment.SpawnId, StringComparison.Ordinal) ||
                    spawn.Point != assignment.Point)
                {
                    throw new ArgumentException("The local monster fixture requires exact ordered spawn assignments.");
                }

                Vector2 facing = ToPlane(branch.Camp.EntryAnchor) - ToPlane(assignment.Point);
                if (facing.LengthSquared() <= 1e-12f)
                    throw new ArgumentException($"Spawn '{assignment.SpawnId}' coincides with its camp entry anchor.");
                snapshots.Add(new Draft0MonsterPresentationSnapshot(
                    IdentityPrefix + assignment.SpawnId,
                    assignment.ArchetypeId,
                    tick: 0,
                    assignment.Point,
                    Vector2.Zero,
                    Vector2.Normalize(facing),
                    collisionRadiusMetres: null,
                    MonsterBehaviorKind.Idle,
                    targetIdentity: null,
                    healthByArchetype[assignment.ArchetypeId],
                    healthByArchetype[assignment.ArchetypeId]));
            }
        }

        if (snapshots.Count != ExpectedCount)
        {
            throw new InvalidOperationException(
                $"The Draft 0 local monster fixture requires {ExpectedCount} snapshots, received {snapshots.Count}.");
        }

        return new ReadOnlyCollection<Draft0MonsterPresentationSnapshot>(snapshots.ToArray());
    }

    private static Vector2 ToPlane(GroundPoint point) =>
        new(point.XMetres, point.ZMetres);
}

internal static class Draft0MonsterPresentationAdapter
{
    internal const string LightArchetypeId = "starter_flyer_light";
    internal const string HeavyArchetypeId = "starter_flyer_heavy";
    internal const float LightScaleMetres = 1.0f;
    internal const float HeavyScaleMetres = 1.5f;
    internal const float GroundClearanceMetres = 0.25f;
    internal const float HoverAmplitudeMetres = 0.12f;
    internal const float HoverPeriodSeconds = 1.5f;
    internal const float AttackLungeDurationSeconds = 0.20f;
    internal const float AttackLungeDistanceMetres = 0.18f;
    internal const float HitFlashDurationSeconds = 0.12f;
    internal const double DeathDurationSeconds = 0.45;
    internal const float DeathFinalVerticalScale = 0.08f;
    internal const float ReturningDesaturation = 0.20f;

    internal static readonly Vector3 LightColor = new(0.20f, 0.72f, 0.90f);
    internal static readonly Vector3 HeavyColor = new(0.90f, 0.38f, 0.16f);
    internal static readonly Vector3 DeathColor = new(0.30f, 0.04f, 0.03f);

    internal static Draft0MonsterPresentationState Adapt(
        Draft0MonsterPresentationSnapshot snapshot,
        double presentationSeconds,
        double? attackLungeStartedAtSeconds = null,
        double? hitFlashStartedAtSeconds = null)
    {
        ValidatePresentationTime(presentationSeconds, nameof(presentationSeconds));
        ValidateEffectStart(attackLungeStartedAtSeconds, presentationSeconds, nameof(attackLungeStartedAtSeconds));
        ValidateEffectStart(hitFlashStartedAtSeconds, presentationSeconds, nameof(hitFlashStartedAtSeconds));

        (float scale, Vector3 color) = GetArchetypePresentation(snapshot.ArchetypeId);

        if (snapshot.Behavior == MonsterBehaviorKind.Returning)
        {
            float luminance = Vector3.Dot(color, new Vector3(0.2126f, 0.7152f, 0.0722f));
            color = Vector3.Lerp(color, new Vector3(luminance), ReturningDesaturation);
        }
        if (hitFlashStartedAtSeconds is { } hitStart)
        {
            float hitProgress = EffectProgress(presentationSeconds, hitStart, HitFlashDurationSeconds);
            if (hitProgress < 1.0f)
                color = Vector3.Lerp(color, Vector3.One, 1.0f - hitProgress);
        }

        float phase = CreateStablePhase(snapshot.Identity);
        float bob = HoverAmplitudeMetres * MathF.Sin(
            (float)(presentationSeconds * Math.Tau / HoverPeriodSeconds) + phase);
        float centreY = GroundClearanceMetres + (scale * 0.5f) + bob;
        float forwardOffset = 0.0f;
        if (attackLungeStartedAtSeconds is { } attackStart)
        {
            float attackProgress = EffectProgress(presentationSeconds, attackStart, AttackLungeDurationSeconds);
            if (attackProgress < 1.0f)
                forwardOffset = AttackLungeDistanceMetres * MathF.Sin(MathF.PI * attackProgress);
        }
        float yaw = MathF.Atan2(snapshot.Facing.X, snapshot.Facing.Y);
        Matrix4x4 world =
            Matrix4x4.CreateScale(scale) *
            Matrix4x4.CreateRotationY(yaw) *
            Matrix4x4.CreateTranslation(
                snapshot.Position.XMetres + (snapshot.Facing.X * forwardOffset),
                centreY,
                snapshot.Position.ZMetres + (snapshot.Facing.Y * forwardOffset));
        return new(snapshot, world, color, scale);
    }

    internal static Draft0MonsterDefeatPresentationState AdaptDefeat(
        Draft0MonsterDefeatPresentationSnapshot snapshot,
        double presentationSeconds,
        double startedAtSeconds)
    {
        ValidatePresentationTime(presentationSeconds, nameof(presentationSeconds));
        ValidatePresentationTime(startedAtSeconds, nameof(startedAtSeconds));
        if (startedAtSeconds > presentationSeconds)
            throw new ArgumentOutOfRangeException(nameof(startedAtSeconds));

        (float scale, Vector3 color) = GetArchetypePresentation(snapshot.ArchetypeId);
        float progress = EffectProgress(presentationSeconds, startedAtSeconds, DeathDurationSeconds);
        float verticalScale = scale * float.Lerp(1.0f, DeathFinalVerticalScale, progress);
        float clearance = GroundClearanceMetres * (1.0f - progress);
        float centreY = clearance + (verticalScale * 0.5f);
        float yaw = MathF.Atan2(snapshot.LastFacing.X, snapshot.LastFacing.Y);
        Matrix4x4 world =
            Matrix4x4.CreateScale(scale, verticalScale, scale) *
            Matrix4x4.CreateRotationY(yaw) *
            Matrix4x4.CreateTranslation(
                snapshot.LastPosition.XMetres,
                centreY,
                snapshot.LastPosition.ZMetres);
        return new(snapshot, world, Vector3.Lerp(color, DeathColor, progress));
    }

    internal static bool IsDeathVisible(double presentationSeconds, double startedAtSeconds)
    {
        ValidatePresentationTime(presentationSeconds, nameof(presentationSeconds));
        ValidatePresentationTime(startedAtSeconds, nameof(startedAtSeconds));
        if (startedAtSeconds > presentationSeconds)
            throw new ArgumentOutOfRangeException(nameof(startedAtSeconds));
        return presentationSeconds - startedAtSeconds <= DeathDurationSeconds;
    }

    private static (float Scale, Vector3 Color) GetArchetypePresentation(string archetypeId) =>
        archetypeId switch
        {
            LightArchetypeId => (LightScaleMetres, LightColor),
            HeavyArchetypeId => (HeavyScaleMetres, HeavyColor),
            _ => throw new ArgumentException(
                $"Unsupported Draft 0 monster archetype '{archetypeId}'.",
                nameof(archetypeId)),
        };

    private static float EffectProgress(double presentationSeconds, double startedAtSeconds, double durationSeconds) =>
        Math.Clamp((float)((presentationSeconds - startedAtSeconds) / durationSeconds), 0.0f, 1.0f);

    private static void ValidatePresentationTime(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0)
            throw new ArgumentOutOfRangeException(parameterName, "Presentation time must be finite and non-negative.");
    }

    private static void ValidateEffectStart(double? value, double presentationSeconds, string parameterName)
    {
        if (value is not { } start)
            return;
        ValidatePresentationTime(start, parameterName);
        if (start > presentationSeconds)
            throw new ArgumentOutOfRangeException(parameterName, "An effect cannot start after the sampled presentation time.");
    }

    private static float CreateStablePhase(string identity)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (char character in identity)
            hash = unchecked((hash ^ character) * prime);
        return (hash % 1024) * (MathF.Tau / 1024.0f);
    }
}

internal sealed class Draft0ConnectedMonsterPresentation
{
    private readonly Dictionary<string, LivePresentation> live = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DefeatPresentation> defeated = new(StringComparer.Ordinal);
    private ImmutableArray<string> liveOrder = [];
    private ImmutableArray<string> defeatedOrder = [];

    internal ulong LastSequence
    {
        get; private set;
    }

    internal bool Accept(BoundedMonsterSnapshot snapshot, double presentationSeconds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!double.IsFinite(presentationSeconds) || presentationSeconds < 0.0)
            throw new ArgumentOutOfRangeException(nameof(presentationSeconds));
        if (snapshot.Sequence.Value <= LastSequence)
            return false;

        var nextLive = new Dictionary<string, LivePresentation>(snapshot.LiveMonsters.Length, StringComparer.Ordinal);
        var nextLiveOrder = ImmutableArray.CreateBuilder<string>(snapshot.LiveMonsters.Length);
        foreach (LiveMonsterSnapshot source in snapshot.LiveMonsters)
        {
            Draft0MonsterPresentationSnapshot mapped = Map(source, snapshot.SimulationTick);
            live.TryGetValue(mapped.Identity, out LivePresentation previous);
            bool previouslyObserved = previous.Snapshot.Identity is not null;
            double? lungeStartedAt = previouslyObserved &&
                previous.Snapshot.Behavior != MonsterBehaviorKind.Attacking &&
                mapped.Behavior == MonsterBehaviorKind.Attacking
                    ? presentationSeconds
                    : mapped.Behavior == MonsterBehaviorKind.Attacking
                        ? previous.LungeStartedAtSeconds
                        : null;
            double? hitStartedAt = previouslyObserved && mapped.CurrentHealthUnits < previous.Snapshot.CurrentHealthUnits
                ? presentationSeconds
                : previous.HitStartedAtSeconds;
            nextLive.Add(mapped.Identity, new LivePresentation(mapped, lungeStartedAt, hitStartedAt));
            nextLiveOrder.Add(mapped.Identity);
        }

        var nextDefeated = new Dictionary<string, DefeatPresentation>(snapshot.DefeatedMonsters.Length, StringComparer.Ordinal);
        var nextDefeatedOrder = ImmutableArray.CreateBuilder<string>(snapshot.DefeatedMonsters.Length);
        foreach (DefeatedMonsterSnapshot source in snapshot.DefeatedMonsters)
        {
            Draft0MonsterDefeatPresentationSnapshot mapped = Map(source);
            double startedAt = defeated.TryGetValue(mapped.Identity, out DefeatPresentation previous)
                ? previous.StartedAtSeconds
                : presentationSeconds;
            nextDefeated.Add(mapped.Identity, new DefeatPresentation(mapped, startedAt));
            nextDefeatedOrder.Add(mapped.Identity);
        }

        live.Clear();
        foreach ((string identity, LivePresentation state) in nextLive)
            live.Add(identity, state);
        defeated.Clear();
        foreach ((string identity, DefeatPresentation state) in nextDefeated)
            defeated.Add(identity, state);
        liveOrder = nextLiveOrder.MoveToImmutable();
        defeatedOrder = nextDefeatedOrder.MoveToImmutable();
        LastSequence = snapshot.Sequence.Value;
        return true;
    }

    internal ImmutableArray<Draft0MonsterPresentationState> CreateLiveStates(double presentationSeconds)
    {
        var states = ImmutableArray.CreateBuilder<Draft0MonsterPresentationState>(liveOrder.Length);
        foreach (string identity in liveOrder)
        {
            LivePresentation state = live[identity];
            states.Add(Draft0MonsterPresentationAdapter.Adapt(
                state.Snapshot,
                presentationSeconds,
                state.LungeStartedAtSeconds,
                state.HitStartedAtSeconds));
        }
        return states.ToImmutable();
    }

    internal ImmutableArray<Draft0MonsterDefeatPresentationState> CreateDefeatStates(double presentationSeconds)
    {
        var states = ImmutableArray.CreateBuilder<Draft0MonsterDefeatPresentationState>();
        foreach (string identity in defeatedOrder)
        {
            DefeatPresentation state = defeated[identity];
            if (Draft0MonsterPresentationAdapter.IsDeathVisible(presentationSeconds, state.StartedAtSeconds))
            {
                states.Add(Draft0MonsterPresentationAdapter.AdaptDefeat(
                    state.Snapshot,
                    presentationSeconds,
                    state.StartedAtSeconds));
            }
        }
        return states.ToImmutable();
    }

    private static Draft0MonsterPresentationSnapshot Map(LiveMonsterSnapshot source, ulong tick) =>
        new(
            ToPresentationIdentity(source.EntityId.Value),
            source.ArchetypeId.Value,
            tick,
            new GroundPoint(source.Position.XMetres, source.Position.ZMetres),
            source.VelocityMetresPerSecond,
            source.Facing,
            source.CollisionRadiusMetres,
            source.Behavior,
            source.TargetEntityId is { } target ? ToPresentationIdentity(target.Value) : null,
            source.CurrentHealthUnits,
            source.MaximumHealthUnits);

    private static Draft0MonsterDefeatPresentationSnapshot Map(DefeatedMonsterSnapshot source) =>
        new(
            ToPresentationIdentity(source.EntityId.Value),
            source.ArchetypeId.Value,
            source.DefeatedAtTick,
            new GroundPoint(source.LastPosition.XMetres, source.LastPosition.ZMetres),
            source.LastFacing);

    private static string ToPresentationIdentity(ulong entityId) =>
        "entity_" + entityId.ToString(CultureInfo.InvariantCulture);

    private readonly record struct LivePresentation(
        Draft0MonsterPresentationSnapshot Snapshot,
        double? LungeStartedAtSeconds,
        double? HitStartedAtSeconds);

    private readonly record struct DefeatPresentation(
        Draft0MonsterDefeatPresentationSnapshot Snapshot,
        double StartedAtSeconds);
}

internal static class Draft0MonsterPlaceholderMesh
{
    internal const string MeshName = "starfall-draft-0-placeholder-monster";
    internal const string SectionName = "placeholder_monster_box_creature";
    internal const int ExpectedVertexCount = 48;
    internal const int ExpectedIndexCount = 72;

    internal static StaticMeshDefinition Create()
    {
        var vertices = new List<StaticVertex>(ExpectedVertexCount);
        var indices = new List<uint>(ExpectedIndexCount);
        AddBox(vertices, indices, new Vector3(-0.5f), new Vector3(0.5f));
        AddBox(
            vertices,
            indices,
            new Vector3(-0.18f, -0.15f, 0.5f),
            new Vector3(0.18f, 0.15f, 0.85f));

        if (vertices.Count != ExpectedVertexCount || indices.Count != ExpectedIndexCount)
            throw new InvalidOperationException("Draft 0 placeholder-monster geometry changed unexpectedly.");
        return new StaticMeshDefinition(
            MeshName,
            vertices,
            indices,
            [new StaticMeshSection(SectionName, 0, indices.Count)]);
    }

    private static void AddBox(
        List<StaticVertex> vertices,
        List<uint> indices,
        Vector3 minimum,
        Vector3 maximum)
    {
        AddQuad(
            vertices,
            indices,
            new(minimum.X, minimum.Y, maximum.Z),
            new(maximum.X, minimum.Y, maximum.Z),
            new(maximum.X, maximum.Y, maximum.Z),
            new(minimum.X, maximum.Y, maximum.Z),
            Vector3.UnitZ);
        AddQuad(
            vertices,
            indices,
            new(maximum.X, minimum.Y, minimum.Z),
            new(minimum.X, minimum.Y, minimum.Z),
            new(minimum.X, maximum.Y, minimum.Z),
            new(maximum.X, maximum.Y, minimum.Z),
            -Vector3.UnitZ);
        AddQuad(
            vertices,
            indices,
            new(maximum.X, minimum.Y, minimum.Z),
            new(maximum.X, maximum.Y, minimum.Z),
            new(maximum.X, maximum.Y, maximum.Z),
            new(maximum.X, minimum.Y, maximum.Z),
            Vector3.UnitX);
        AddQuad(
            vertices,
            indices,
            new(minimum.X, minimum.Y, maximum.Z),
            new(minimum.X, maximum.Y, maximum.Z),
            new(minimum.X, maximum.Y, minimum.Z),
            new(minimum.X, minimum.Y, minimum.Z),
            -Vector3.UnitX);
        AddQuad(
            vertices,
            indices,
            new(minimum.X, maximum.Y, maximum.Z),
            new(maximum.X, maximum.Y, maximum.Z),
            new(maximum.X, maximum.Y, minimum.Z),
            new(minimum.X, maximum.Y, minimum.Z),
            Vector3.UnitY);
        AddQuad(
            vertices,
            indices,
            new(minimum.X, minimum.Y, minimum.Z),
            new(maximum.X, minimum.Y, minimum.Z),
            new(maximum.X, minimum.Y, maximum.Z),
            new(minimum.X, minimum.Y, maximum.Z),
            -Vector3.UnitY);
    }

    private static void AddQuad(
        List<StaticVertex> vertices,
        List<uint> indices,
        Vector3 first,
        Vector3 second,
        Vector3 third,
        Vector3 fourth,
        Vector3 normal)
    {
        uint start = checked((uint)vertices.Count);
        vertices.Add(new StaticVertex(first, normal));
        vertices.Add(new StaticVertex(second, normal));
        vertices.Add(new StaticVertex(third, normal));
        vertices.Add(new StaticVertex(fourth, normal));
        indices.AddRange([start, start + 1, start + 2, start, start + 2, start + 3]);
    }
}
