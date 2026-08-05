using System.Collections.ObjectModel;
using System.Numerics;
using ChronoFall.CharacterPresentation;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;

namespace Starfall.Client;

internal readonly record struct Draft0MonsterPresentationSnapshot
{
    private const float FacingLengthTolerance = 1e-4f;

    internal Draft0MonsterPresentationSnapshot(
        string identity,
        string archetypeId,
        ulong tick,
        GroundPoint position,
        Vector2 facing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);
        if (!float.IsFinite(facing.X) ||
            !float.IsFinite(facing.Y) ||
            MathF.Abs(facing.Length() - 1.0f) > FacingLengthTolerance)
        {
            throw new ArgumentException("Monster facing must be finite and normalized.", nameof(facing));
        }

        Identity = identity;
        ArchetypeId = archetypeId;
        Tick = tick;
        Position = position;
        Facing = facing;
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

    internal Vector2 Facing
    {
        get;
    }
}

internal readonly record struct Draft0MonsterPresentationState(
    Draft0MonsterPresentationSnapshot Snapshot,
    Matrix4x4 World,
    Vector3 BaseColor,
    float UniformScaleMetres);

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
                    Vector2.Normalize(facing)));
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

    internal static readonly Vector3 LightColor = new(0.20f, 0.72f, 0.90f);
    internal static readonly Vector3 HeavyColor = new(0.90f, 0.38f, 0.16f);

    internal static Draft0MonsterPresentationState Adapt(
        Draft0MonsterPresentationSnapshot snapshot,
        double presentationSeconds)
    {
        if (!double.IsFinite(presentationSeconds) || presentationSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(presentationSeconds),
                "Presentation time must be finite and non-negative.");
        }

        (float scale, Vector3 color) = snapshot.ArchetypeId switch
        {
            LightArchetypeId => (LightScaleMetres, LightColor),
            HeavyArchetypeId => (HeavyScaleMetres, HeavyColor),
            _ => throw new ArgumentException(
                $"Unsupported Draft 0 monster archetype '{snapshot.ArchetypeId}'.",
                nameof(snapshot)),
        };

        float phase = CreateStablePhase(snapshot.Identity);
        float bob = HoverAmplitudeMetres * MathF.Sin(
            (float)(presentationSeconds * Math.Tau / HoverPeriodSeconds) + phase);
        float centreY = GroundClearanceMetres + (scale * 0.5f) + bob;
        float yaw = MathF.Atan2(snapshot.Facing.X, snapshot.Facing.Y);
        Matrix4x4 world =
            Matrix4x4.CreateScale(scale) *
            Matrix4x4.CreateRotationY(yaw) *
            Matrix4x4.CreateTranslation(
                snapshot.Position.XMetres,
                centreY,
                snapshot.Position.ZMetres);
        return new(snapshot, world, color, scale);
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
