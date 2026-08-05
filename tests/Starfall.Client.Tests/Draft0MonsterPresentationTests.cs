using System.Collections;
using System.Numerics;
using ChronoFall.CharacterPresentation;
using Starfall.Client;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;

namespace Starfall.Client.Tests;

public sealed class Draft0MonsterPresentationTests
{
    private static readonly string[] ExpectedFixtureIdentities =
    [
        "fixture_spawn_easy_01",
        "fixture_spawn_easy_02",
        "fixture_spawn_easy_03",
        "fixture_spawn_mixed_01",
        "fixture_spawn_mixed_02",
        "fixture_spawn_mixed_03",
        "fixture_spawn_mixed_04",
        "fixture_spawn_hard_01",
        "fixture_spawn_hard_02",
        "fixture_spawn_hard_03",
    ];

    [Fact]
    public void LocalFixtureCopiesTheExactOrderedStarterAssignments()
    {
        Draft0GrayboxLayout layout = Draft0GrayboxCatalog.FirstPlayable;
        Draft0StarterMonsterCatalogDefinition catalog = Draft0StarterMonsterCatalog.FirstPlayable;

        IReadOnlyList<Draft0MonsterPresentationSnapshot> snapshots =
            Draft0LocalMonsterFixture.Create(layout, catalog);
        Draft0CampSpawnAssignment[] assignments = catalog.Camps
            .SelectMany(static camp => camp.Assignments)
            .ToArray();

        Assert.Equal(Draft0LocalMonsterFixture.ExpectedCount, snapshots.Count);
        Assert.Equal(ExpectedFixtureIdentities, snapshots.Select(static snapshot => snapshot.Identity));
        for (var index = 0; index < snapshots.Count; index++)
        {
            Assert.Equal(assignments[index].ArchetypeId, snapshots[index].ArchetypeId);
            Assert.Equal(assignments[index].Point, snapshots[index].Position);
            Assert.Equal(0UL, snapshots[index].Tick);
            Assert.InRange(snapshots[index].Facing.Length(), 0.9999f, 1.0001f);
        }

        IList readOnlyView = Assert.IsAssignableFrom<IList>(snapshots);
        Assert.True(readOnlyView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => readOnlyView.Add(snapshots[0]));
    }

    [Fact]
    public void LocalFixtureFacesEachAssignmentTowardItsCampEntry()
    {
        Draft0GrayboxLayout layout = Draft0GrayboxCatalog.FirstPlayable;
        IReadOnlyList<Draft0MonsterPresentationSnapshot> snapshots =
            Draft0LocalMonsterFixture.Create(layout, Draft0StarterMonsterCatalog.FirstPlayable);

        var snapshotIndex = 0;
        foreach (Draft0BranchLayout branch in layout.Branches)
        {
            foreach (Draft0SampleSpawn spawn in branch.SampleSpawns)
            {
                Vector2 expected = Vector2.Normalize(new Vector2(
                    branch.Camp.EntryAnchor.XMetres - spawn.Point.XMetres,
                    branch.Camp.EntryAnchor.ZMetres - spawn.Point.ZMetres));
                AssertVector(expected, snapshots[snapshotIndex].Facing);
                snapshotIndex++;
            }
        }
    }

    [Fact]
    public void SnapshotRejectsInvalidIdentityArchetypeAndFacing()
    {
        var point = new GroundPoint(55.0f, 65.0f);

        Assert.Throws<ArgumentException>(() =>
            new Draft0MonsterPresentationSnapshot("", Draft0MonsterPresentationAdapter.LightArchetypeId, 0, point, Vector2.UnitY));
        Assert.Throws<ArgumentException>(() =>
            new Draft0MonsterPresentationSnapshot("fixture", " ", 0, point, Vector2.UnitY));
        Assert.Throws<ArgumentException>(() =>
            new Draft0MonsterPresentationSnapshot("fixture", Draft0MonsterPresentationAdapter.LightArchetypeId, 0, point, Vector2.Zero));
        Assert.Throws<ArgumentException>(() =>
            new Draft0MonsterPresentationSnapshot("fixture", Draft0MonsterPresentationAdapter.LightArchetypeId, 0, point, new(float.NaN, 1.0f)));
    }

    [Fact]
    public void AdapterAppliesFrozenArchetypeScaleColorFacingAndHover()
    {
        var position = new GroundPoint(55.0f, 65.0f);
        var snapshot = new Draft0MonsterPresentationSnapshot(
            "fixture_spawn_easy_01",
            Draft0MonsterPresentationAdapter.LightArchetypeId,
            0,
            position,
            Vector2.UnitX);

        Draft0MonsterPresentationState state = Draft0MonsterPresentationAdapter.Adapt(snapshot, 0.0);

        Assert.Equal(Draft0MonsterPresentationAdapter.LightScaleMetres, state.UniformScaleMetres);
        Assert.Equal(Draft0MonsterPresentationAdapter.LightColor, state.BaseColor);
        Assert.Equal(position.XMetres, state.World.M41);
        Assert.Equal(position.ZMetres, state.World.M43);
        Assert.InRange(
            state.World.M42,
            Draft0MonsterPresentationAdapter.GroundClearanceMetres + 0.5f - Draft0MonsterPresentationAdapter.HoverAmplitudeMetres,
            Draft0MonsterPresentationAdapter.GroundClearanceMetres + 0.5f + Draft0MonsterPresentationAdapter.HoverAmplitudeMetres);
        AssertVector(Vector2.UnitX, TransformForward(state.World));

        Draft0MonsterPresentationState heavy = Draft0MonsterPresentationAdapter.Adapt(
            new Draft0MonsterPresentationSnapshot(
                snapshot.Identity,
                Draft0MonsterPresentationAdapter.HeavyArchetypeId,
                snapshot.Tick,
                snapshot.Position,
                snapshot.Facing),
            0.0);
        Assert.Equal(Draft0MonsterPresentationAdapter.HeavyScaleMetres, heavy.UniformScaleMetres);
        Assert.Equal(Draft0MonsterPresentationAdapter.HeavyColor, heavy.BaseColor);
        Assert.Equal(position, snapshot.Position);
    }

    [Fact]
    public void HoverIsDeterministicPeriodicAndPresentationOnly()
    {
        var snapshot = new Draft0MonsterPresentationSnapshot(
            "fixture_spawn_mixed_01",
            Draft0MonsterPresentationAdapter.LightArchetypeId,
            0,
            new GroundPoint(95.0f, 122.0f),
            Vector2.UnitY);

        Draft0MonsterPresentationState first = Draft0MonsterPresentationAdapter.Adapt(snapshot, 0.25);
        Draft0MonsterPresentationState repeated = Draft0MonsterPresentationAdapter.Adapt(snapshot, 0.25);
        Draft0MonsterPresentationState nextPeriod = Draft0MonsterPresentationAdapter.Adapt(
            snapshot,
            0.25 + Draft0MonsterPresentationAdapter.HoverPeriodSeconds);

        Assert.Equal(first.World, repeated.World);
        Assert.InRange(MathF.Abs(first.World.M42 - nextPeriod.World.M42), 0.0f, 1e-5f);
        Assert.Equal(new GroundPoint(95.0f, 122.0f), snapshot.Position);
        Assert.Equal(0.0f, snapshot.Position.Metres.Y);
    }

    [Fact]
    public void AdapterRejectsUnsupportedArchetypesAndInvalidPresentationTimes()
    {
        var snapshot = new Draft0MonsterPresentationSnapshot(
            "fixture",
            "unknown",
            0,
            new GroundPoint(1.0f, 2.0f),
            Vector2.UnitY);

        Assert.Throws<ArgumentException>(() => Draft0MonsterPresentationAdapter.Adapt(snapshot, 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Draft0MonsterPresentationAdapter.Adapt(snapshot, -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Draft0MonsterPresentationAdapter.Adapt(snapshot, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Draft0MonsterPresentationAdapter.Adapt(snapshot, double.PositiveInfinity));
    }

    [Fact]
    public void PlaceholderMeshHasFrozenForwardReadableGeometry()
    {
        StaticMeshDefinition mesh = Draft0MonsterPlaceholderMesh.Create();

        Assert.Equal(Draft0MonsterPlaceholderMesh.MeshName, mesh.Name);
        Assert.Equal(Draft0MonsterPlaceholderMesh.ExpectedVertexCount, mesh.Vertices.Count);
        Assert.Equal(Draft0MonsterPlaceholderMesh.ExpectedIndexCount, mesh.Indices.Count);
        StaticMeshSection section = Assert.Single(mesh.Sections);
        Assert.Equal(Draft0MonsterPlaceholderMesh.SectionName, section.MaterialName);
        Assert.Equal(0, section.StartIndex);
        Assert.Equal(Draft0MonsterPlaceholderMesh.ExpectedIndexCount, section.IndexCount);
        Assert.All(mesh.Indices, index => Assert.InRange(index, 0U, (uint)mesh.Vertices.Count - 1));
        Assert.All(mesh.Vertices, vertex => Assert.InRange(vertex.Normal.Length(), 0.9999f, 1.0001f));

        Assert.Equal(-0.5f, mesh.Vertices.Min(static vertex => vertex.Position.X));
        Assert.Equal(0.5f, mesh.Vertices.Max(static vertex => vertex.Position.X));
        Assert.Equal(-0.5f, mesh.Vertices.Min(static vertex => vertex.Position.Y));
        Assert.Equal(0.5f, mesh.Vertices.Max(static vertex => vertex.Position.Y));
        Assert.Equal(-0.5f, mesh.Vertices.Min(static vertex => vertex.Position.Z));
        Assert.Equal(0.85f, mesh.Vertices.Max(static vertex => vertex.Position.Z));
    }

    private static Vector2 TransformForward(Matrix4x4 world)
    {
        Vector3 transformed = Vector3.TransformNormal(Vector3.UnitZ, world);
        return Vector2.Normalize(new Vector2(transformed.X, transformed.Z));
    }

    private static void AssertVector(Vector2 expected, Vector2 actual)
    {
        Assert.InRange(MathF.Abs(expected.X - actual.X), 0.0f, 1e-5f);
        Assert.InRange(MathF.Abs(expected.Y - actual.Y), 0.0f, 1e-5f);
    }
}
