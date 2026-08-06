using System.Collections;
using System.Numerics;
using ChronoFall.CharacterPresentation;
using ChronoFall.CharacterPresentation.SdlGpu;
using Starfall.Client;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;

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
            Assert.Equal(Vector2.Zero, snapshots[index].VelocityMetresPerSecond);
            Assert.Null(snapshots[index].CollisionRadiusMetres);
            Assert.Equal(MonsterBehaviorKind.Idle, snapshots[index].Behavior);
            Assert.Null(snapshots[index].TargetIdentity);
            int expectedHealth = catalog.Archetypes.Single(
                archetype => archetype.Id == snapshots[index].ArchetypeId).AuthoritativeHealthUnits;
            Assert.Equal(expectedHealth, snapshots[index].CurrentHealthUnits);
            Assert.Equal(expectedHealth, snapshots[index].MaximumHealthUnits);
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
    public void SnapshotRejectsMalformedPresentationFacts()
    {
        var point = new GroundPoint(55.0f, 65.0f);

        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(identity: "", position: point));
        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(archetypeId: " ", position: point));
        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(position: point, facing: Vector2.Zero));
        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(position: point, facing: new(float.NaN, 1.0f)));
        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(position: point, velocity: new(float.PositiveInfinity, 0.0f)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateSnapshot(position: point, collisionRadiusMetres: 0.0f));
        Assert.Throws<ArgumentNullException>(() =>
            CreateSnapshot(position: point, behavior: MonsterBehaviorKind.Attacking));
        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(position: point, targetIdentity: "entity_2"));
        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(
                identity: "entity_2",
                position: point,
                behavior: MonsterBehaviorKind.Pursuing,
                targetIdentity: "entity_2"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateSnapshot(position: point, currentHealthUnits: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateSnapshot(position: point, currentHealthUnits: 701));
    }

    [Fact]
    public void AdapterAppliesFrozenArchetypeScaleColorFacingAndHover()
    {
        var position = new GroundPoint(55.0f, 65.0f);
        Draft0MonsterPresentationSnapshot snapshot = CreateSnapshot(
            identity: "fixture_spawn_easy_01",
            position: position,
            facing: Vector2.UnitX);

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
            CreateSnapshot(
                identity: snapshot.Identity,
                archetypeId: Draft0MonsterPresentationAdapter.HeavyArchetypeId,
                position: snapshot.Position,
                facing: snapshot.Facing,
                currentHealthUnits: 2_000,
                maximumHealthUnits: 2_000),
            0.0);
        Assert.Equal(Draft0MonsterPresentationAdapter.HeavyScaleMetres, heavy.UniformScaleMetres);
        Assert.Equal(Draft0MonsterPresentationAdapter.HeavyColor, heavy.BaseColor);
        Assert.Equal(position, snapshot.Position);
    }

    [Fact]
    public void HoverIsDeterministicPeriodicAndPresentationOnly()
    {
        Draft0MonsterPresentationSnapshot snapshot = CreateSnapshot(
            identity: "fixture_spawn_mixed_01",
            position: new GroundPoint(95.0f, 122.0f));

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
    public void ConnectedPresentationMapsOrderedAuthoritativeFactsWithoutChangingGroundState()
    {
        var presentation = new Draft0ConnectedMonsterPresentation();
        var source = new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(7),
            42,
            [
                Live(
                    2,
                    Draft0MonsterPresentationAdapter.LightArchetypeId,
                    new GroundPosition(55.0f, 65.0f),
                    new Vector2(1.5f, 0.0f),
                    Vector2.UnitX,
                    0.45f,
                    MonsterBehaviorKind.Pursuing,
                    99,
                    400,
                    700),
                Live(
                    10,
                    Draft0MonsterPresentationAdapter.HeavyArchetypeId,
                    new GroundPosition(145.0f, 104.0f),
                    Vector2.Zero,
                    Vector2.UnitY,
                    0.65f,
                    MonsterBehaviorKind.Idle,
                    null,
                    2_000,
                    2_000),
            ],
            []);

        Assert.True(presentation.Accept(source, 3.0));
        Draft0MonsterPresentationState[] states = presentation.CreateLiveStates(3.0).ToArray();

        Assert.Equal(7UL, presentation.LastSequence);
        Assert.Equal(["entity_2", "entity_10"], states.Select(static state => state.Snapshot.Identity));
        Draft0MonsterPresentationSnapshot light = states[0].Snapshot;
        Assert.Equal(42UL, light.Tick);
        Assert.Equal(new GroundPoint(55.0f, 65.0f), light.Position);
        Assert.Equal(new Vector2(1.5f, 0.0f), light.VelocityMetresPerSecond);
        Assert.Equal(Vector2.UnitX, light.Facing);
        Assert.Equal(0.45f, light.CollisionRadiusMetres);
        Assert.Equal(MonsterBehaviorKind.Pursuing, light.Behavior);
        Assert.Equal("entity_99", light.TargetIdentity);
        Assert.Equal(400, light.CurrentHealthUnits);
        Assert.Equal(700, light.MaximumHealthUnits);
        Assert.Equal(light.Position.XMetres, states[0].World.M41);
        Assert.Equal(light.Position.ZMetres, states[0].World.M43);
    }

    [Fact]
    public void ConnectedPresentationAppliesOnlyObservedAttackHitAndReturnEffects()
    {
        var initiallyAttacking = new Draft0ConnectedMonsterPresentation();
        Assert.True(initiallyAttacking.Accept(Snapshot(
            sequence: 1,
            tick: 1,
            Live(
                2,
                behavior: MonsterBehaviorKind.Attacking,
                targetEntityId: 99)), 0.0));
        Draft0MonsterPresentationState initialAttack = Assert.Single(
            initiallyAttacking.CreateLiveStates(0.1));
        Assert.Equal(initialAttack.Snapshot.Position.ZMetres, initialAttack.World.M43);

        var presentation = new Draft0ConnectedMonsterPresentation();
        Assert.True(presentation.Accept(Snapshot(
            sequence: 1,
            tick: 1,
            Live(
                2,
                behavior: MonsterBehaviorKind.Pursuing,
                targetEntityId: 99,
                currentHealthUnits: 700)), 0.0));

        Assert.True(presentation.Accept(Snapshot(
            sequence: 2,
            tick: 2,
            Live(
                2,
                behavior: MonsterBehaviorKind.Attacking,
                targetEntityId: 99,
                currentHealthUnits: 400)), 1.0));

        Draft0MonsterPresentationState effectStart = Assert.Single(presentation.CreateLiveStates(1.0));
        Assert.Equal(Vector3.One, effectStart.BaseColor);
        Draft0MonsterPresentationState lungePeak = Assert.Single(presentation.CreateLiveStates(1.1));
        Assert.InRange(
            lungePeak.World.M43 - lungePeak.Snapshot.Position.ZMetres,
            Draft0MonsterPresentationAdapter.AttackLungeDistanceMetres - 1e-5f,
            Draft0MonsterPresentationAdapter.AttackLungeDistanceMetres + 1e-5f);

        Assert.False(presentation.Accept(Snapshot(
            sequence: 2,
            tick: 2,
            Live(
                2,
                behavior: MonsterBehaviorKind.Attacking,
                targetEntityId: 99,
                currentHealthUnits: 400)), 1.2));
        Assert.True(presentation.Accept(Snapshot(
            sequence: 3,
            tick: 3,
            Live(
                2,
                behavior: MonsterBehaviorKind.Attacking,
                targetEntityId: 99,
                currentHealthUnits: 400)), 2.0));
        Draft0MonsterPresentationState expired = Assert.Single(presentation.CreateLiveStates(2.0));
        Assert.Equal(expired.Snapshot.Position.ZMetres, expired.World.M43);
        Assert.Equal(Draft0MonsterPresentationAdapter.LightColor, expired.BaseColor);

        Assert.True(presentation.Accept(Snapshot(
            sequence: 4,
            tick: 4,
            Live(2, behavior: MonsterBehaviorKind.Returning)), 3.0));
        Draft0MonsterPresentationState returning = Assert.Single(presentation.CreateLiveStates(3.0));
        Assert.Equal(MonsterBehaviorKind.Returning, returning.Snapshot.Behavior);
        Assert.Null(returning.Snapshot.TargetIdentity);
        Assert.NotEqual(Draft0MonsterPresentationAdapter.LightColor, returning.BaseColor);
        Assert.Equal(returning.Snapshot.Position.XMetres, returning.World.M41);
        Assert.Equal(returning.Snapshot.Position.ZMetres, returning.World.M43);
    }

    [Fact]
    public void ConnectedPresentationRetainsOneDeathCollapseAndAcceptsFreshReplacementIdentity()
    {
        var presentation = new Draft0ConnectedMonsterPresentation();
        Assert.True(presentation.Accept(Snapshot(1, 10, Live(2)), 0.0));
        Assert.True(presentation.Accept(new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(2),
            11,
            [],
            [Defeated(2, 11)]), 1.0));

        Draft0MonsterDefeatPresentationState deathStart = Assert.Single(presentation.CreateDefeatStates(1.0));
        Assert.Equal("entity_2", deathStart.Snapshot.Identity);
        Assert.Equal(55.0f, deathStart.World.M41);
        Assert.Equal(65.0f, deathStart.World.M43);

        Assert.True(presentation.Accept(new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(3),
            12,
            [],
            [Defeated(2, 11)]), 1.2));
        Draft0MonsterDefeatPresentationState collapsed = Assert.Single(presentation.CreateDefeatStates(1.45));
        Assert.InRange(collapsed.World.M42, 0.039f, 0.041f);
        Assert.True(Matrix4x4.Decompose(collapsed.World, out Vector3 scale, out _, out _));
        Assert.Equal(scale.X, scale.Y);
        Assert.Equal(scale.X, scale.Z);
        _ = new StaticMeshDraw(
            collapsed.World,
            Matrix4x4.Identity,
            collapsed.BaseColor,
            -Vector3.UnitY);
        Assert.Equal(Draft0MonsterPresentationAdapter.DeathColor, collapsed.BaseColor);
        Assert.Empty(presentation.CreateDefeatStates(1.46));

        Assert.True(presentation.Accept(Snapshot(4, 13, Live(3)), 2.0));
        Draft0MonsterPresentationState replacement = Assert.Single(presentation.CreateLiveStates(2.0));
        Assert.Equal("entity_3", replacement.Snapshot.Identity);
        Assert.Empty(presentation.CreateDefeatStates(2.0));

        Assert.True(presentation.Accept(new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(5),
            14,
            [],
            []), 3.0));
        Assert.Empty(presentation.CreateLiveStates(3.0));
    }

    [Fact]
    public void AdapterRejectsUnsupportedArchetypesAndInvalidPresentationTimes()
    {
        Draft0MonsterPresentationSnapshot snapshot = CreateSnapshot(
            archetypeId: "unknown",
            position: new GroundPoint(1.0f, 2.0f));

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

    private static Draft0MonsterPresentationSnapshot CreateSnapshot(
        string identity = "fixture",
        string archetypeId = Draft0MonsterPresentationAdapter.LightArchetypeId,
        ulong tick = 0,
        GroundPoint? position = null,
        Vector2? velocity = null,
        Vector2? facing = null,
        float? collisionRadiusMetres = null,
        MonsterBehaviorKind behavior = MonsterBehaviorKind.Idle,
        string? targetIdentity = null,
        int currentHealthUnits = 700,
        int maximumHealthUnits = 700) =>
        new(
            identity,
            archetypeId,
            tick,
            position ?? new GroundPoint(55.0f, 65.0f),
            velocity ?? Vector2.Zero,
            facing ?? Vector2.UnitY,
            collisionRadiusMetres,
            behavior,
            targetIdentity,
            currentHealthUnits,
            maximumHealthUnits);

    private static BoundedMonsterSnapshot Snapshot(
        ulong sequence,
        ulong tick,
        params LiveMonsterSnapshot[] live) =>
        new(new MonsterSnapshotSequence(sequence), tick, live, []);

    private static LiveMonsterSnapshot Live(
        ulong entityId,
        string archetypeId = Draft0MonsterPresentationAdapter.LightArchetypeId,
        GroundPosition? position = null,
        Vector2? velocity = null,
        Vector2? facing = null,
        float collisionRadiusMetres = 0.45f,
        MonsterBehaviorKind behavior = MonsterBehaviorKind.Idle,
        ulong? targetEntityId = null,
        int currentHealthUnits = 700,
        int maximumHealthUnits = 700) =>
        new(
            new WorldEntityId(entityId),
            new MonsterArchetypeId(archetypeId),
            position ?? new GroundPosition(55.0f, 65.0f),
            velocity ?? Vector2.Zero,
            facing ?? Vector2.UnitY,
            collisionRadiusMetres,
            behavior,
            targetEntityId is { } target ? new WorldEntityId(target) : null,
            currentHealthUnits,
            maximumHealthUnits);

    private static DefeatedMonsterSnapshot Defeated(ulong entityId, ulong defeatedAtTick) =>
        new(
            new WorldEntityId(entityId),
            new MonsterArchetypeId(Draft0MonsterPresentationAdapter.LightArchetypeId),
            new GroundPosition(55.0f, 65.0f),
            Vector2.UnitY,
            defeatedAtTick);

    private static void AssertVector(Vector2 expected, Vector2 actual)
    {
        Assert.InRange(MathF.Abs(expected.X - actual.X), 0.0f, 1e-5f);
        Assert.InRange(MathF.Abs(expected.Y - actual.Y), 0.0f, 1e-5f);
    }
}
