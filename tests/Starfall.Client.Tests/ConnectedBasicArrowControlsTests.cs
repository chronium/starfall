using System.Numerics;
using Starfall.Client;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;

namespace Starfall.Client.Tests;

public sealed class ConnectedBasicArrowControlsTests
{
    [Fact]
    public void Pointer_actions_freeze_primary_combat_and_secondary_movement()
    {
        Assert.Equal(Draft0PointerAction.BasicArrow, Draft0PointerControls.Resolve(Draft0PointerButton.Primary, connected: true));
        Assert.Equal(Draft0PointerAction.None, Draft0PointerControls.Resolve(Draft0PointerButton.Primary, connected: false));
        Assert.Equal(Draft0PointerAction.Move, Draft0PointerControls.Resolve(Draft0PointerButton.Secondary, connected: true));
        Assert.Equal(Draft0PointerAction.Move, Draft0PointerControls.Resolve(Draft0PointerButton.Secondary, connected: false));
    }

    [Fact]
    public void Picker_hits_the_visible_cylinder_and_rejects_misses_or_targets_behind_the_ray()
    {
        BoundedMonsterSnapshot snapshot = Snapshot(
            Monster(1, 0.0f, 0.0f, 0.45f, "starter_flyer_light"),
            Monster(2, 0.0f, -20.0f, 0.65f, "starter_flyer_heavy"));

        Assert.True(ConnectedBasicArrowTargeting.TryPick(
            Ray(new Vector3(0.0f, 1.0f, -10.0f), Vector3.UnitZ), snapshot, out WorldEntityId target));
        Assert.Equal(1UL, target.Value);
        Assert.False(ConnectedBasicArrowTargeting.TryPick(
            Ray(new Vector3(5.0f, 1.0f, -10.0f), Vector3.UnitZ), snapshot, out _));
    }

    [Fact]
    public void Picker_chooses_nearest_positive_hit_then_ascending_entity_identity()
    {
        BoundedMonsterSnapshot nearest = Snapshot(
            Monster(1, 0.0f, 2.0f, 0.45f, "starter_flyer_light"),
            Monster(2, 0.0f, 0.0f, 0.45f, "starter_flyer_light"));
        Assert.True(ConnectedBasicArrowTargeting.TryPick(
            Ray(new Vector3(0.0f, 0.5f, -10.0f), Vector3.UnitZ), nearest, out WorldEntityId nearestTarget));
        Assert.Equal(2UL, nearestTarget.Value);

        BoundedMonsterSnapshot tied = Snapshot(
            Monster(3, 0.0f, 0.0f, 0.45f, "starter_flyer_light"),
            Monster(9, 0.0f, 0.0f, 0.45f, "starter_flyer_light"));
        Assert.True(ConnectedBasicArrowTargeting.TryPick(
            Ray(new Vector3(0.0f, 0.5f, -10.0f), Vector3.UnitZ), tied, out WorldEntityId tiedTarget));
        Assert.Equal(3UL, tiedTarget.Value);
    }

    [Fact]
    public void Selection_clears_on_a_miss_or_when_the_authoritative_target_is_no_longer_live()
    {
        var selection = new ConnectedBasicArrowSelection();
        BoundedMonsterSnapshot live = Snapshot(Monster(7, 0.0f, 0.0f, 0.45f, "starter_flyer_light"));
        Assert.True(selection.SelectOrClear(Ray(new Vector3(0.0f, 0.5f, -10.0f), Vector3.UnitZ), live));
        Assert.Equal(7UL, selection.SelectedTarget!.Value.Value);

        Assert.True(selection.Reconcile(Snapshot()));
        Assert.Null(selection.SelectedTarget);

        Assert.True(selection.SelectOrClear(Ray(new Vector3(0.0f, 0.5f, -10.0f), Vector3.UnitZ), live));
        Assert.False(selection.SelectOrClear(Ray(new Vector3(5.0f, 0.5f, -10.0f), Vector3.UnitZ), live));
        Assert.Null(selection.SelectedTarget);
    }

    [Fact]
    public void Camera_world_ray_round_trips_through_the_existing_ground_picker()
    {
        var camera = new PerspectiveIsometricCamera(
            new Starfall.Content.Zones.GroundPoint(100.0f, 100.0f),
            PerspectiveIsometricCameraSettings.Draft0);
        Assert.True(camera.TryCreateWorldRay(new Vector2(0.5f), 1920, 1080, out PerspectiveWorldRay ray));
        Assert.InRange(MathF.Abs(ray.Direction.Length() - 1.0f), 0.0f, 1e-5f);
        Assert.True(camera.TryPickGround(
            new Vector2(0.5f),
            1920,
            1080,
            new Starfall.Content.Zones.GroundBounds(
                new Starfall.Content.Zones.GroundPoint(0.0f, 0.0f),
                new Starfall.Content.Zones.GroundPoint(200.0f, 200.0f)),
            out Starfall.Content.Zones.GroundPoint ground));
        Assert.InRange(MathF.Abs(ground.XMetres - 100.0f), 0.0f, 0.01f);
        Assert.InRange(MathF.Abs(ground.ZMetres - 100.0f), 0.0f, 0.01f);
    }

    private static PerspectiveWorldRay Ray(Vector3 origin, Vector3 direction) => new(origin, Vector3.Normalize(direction));

    private static BoundedMonsterSnapshot Snapshot(params LiveMonsterSnapshot[] monsters) =>
        new(new MonsterSnapshotSequence(1), 10, monsters, []);

    private static LiveMonsterSnapshot Monster(ulong identity, float x, float z, float radius, string archetype) =>
        new(
            new WorldEntityId(identity),
            new MonsterArchetypeId(archetype),
            new GroundPosition(x, z),
            Vector2.Zero,
            Vector2.UnitY,
            radius,
            MonsterBehaviorKind.Idle,
            targetEntityId: null,
            currentHealthUnits: 700,
            maximumHealthUnits: 700);
}
