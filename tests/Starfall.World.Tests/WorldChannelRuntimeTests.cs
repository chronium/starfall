using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.World.Lifecycle;

namespace Starfall.World.Tests;

public sealed class WorldChannelRuntimeTests
{
    [Fact]
    public void Owns_explicit_identity_and_independent_tick_state()
    {
        WorldChannelRuntime first = CreateRuntime(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"));
        WorldChannelRuntime second = CreateRuntime(Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"));

        first.Start();
        second.Start();
        first.Step();
        first.Step();
        second.Step();

        Assert.Equal("world_1", first.WorldId.Value);
        Assert.Equal("channel_1", first.ChannelId.Value);
        Assert.NotEqual(first.InstanceId, second.InstanceId);
        Assert.Same(first.Layout, second.Layout);
        Assert.Equal(2UL, first.CurrentTick);
        Assert.Equal(1UL, second.CurrentTick);
    }

    [Fact]
    public void Owns_the_exact_validated_draft_0_layout_without_reordering_inputs()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());

        Assert.Same(Draft0GrayboxCatalog.FirstPlayable, runtime.Layout);
        Assert.Equal(
            new GroundBounds(new GroundPoint(5.0f, 5.0f), new GroundPoint(195.0f, 195.0f)),
            runtime.Layout.WalkableBounds);
        Assert.Equal("town_safe", runtime.Layout.Town.Id);
        Assert.Equal(new GroundPoint(100.0f, 25.0f), runtime.Layout.Town.RespawnAnchor);
        Assert.Equal(
            ["branch_short", "branch_medium", "branch_long"],
            runtime.Layout.Branches.Select(static branch => branch.Id));
        Assert.Equal(
            ["route_town_exit", "route_branch_short", "route_branch_medium", "route_branch_long"],
            runtime.Layout.Branches
                .Select(static branch => branch.Route.Id)
                .Prepend(runtime.Layout.ExitRoute.Id));
        Assert.Equal(
            ["camp_easy", "camp_mixed", "camp_hard"],
            runtime.Layout.Branches.Select(static branch => branch.Camp.Id));
        Assert.Equal(
        [
            "landmark_west_south",
            "landmark_east_south",
            "landmark_west_north",
            "mixed_divider",
            "hard_bowl_wall_west",
            "hard_bowl_wall_east",
            "hard_bowl_wall_north",
        ],
        runtime.Layout.Proxies.Select(static proxy => proxy.Id));
        Assert.Equal(
        [
            "spawn_easy_01",
            "spawn_easy_02",
            "spawn_easy_03",
            "spawn_mixed_01",
            "spawn_mixed_02",
            "spawn_mixed_03",
            "spawn_mixed_04",
            "spawn_hard_01",
            "spawn_hard_02",
            "spawn_hard_03",
        ],
        runtime.Layout.Branches.SelectMany(static branch => branch.SampleSpawns).Select(static spawn => spawn.Id));
    }

    [Fact]
    public void Rejects_a_missing_world_layout()
    {
        Assert.Throws<ArgumentNullException>(() => new WorldChannelRuntime(
            new("world_1"),
            new("channel_1"),
            new(Guid.NewGuid()),
            null!));
    }

    [Fact]
    public void Moves_through_created_running_draining_and_stopped_states()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());

        Assert.Equal(WorldChannelLifecycleState.Created, runtime.State);
        Assert.False(runtime.AcceptingAdmissions);

        runtime.Start();
        Assert.Equal(WorldChannelLifecycleState.Running, runtime.State);
        Assert.True(runtime.AcceptingAdmissions);

        runtime.Step();
        runtime.BeginDrain();
        runtime.BeginDrain();
        Assert.Equal(WorldChannelLifecycleState.Draining, runtime.State);
        Assert.False(runtime.AcceptingAdmissions);

        runtime.Step();
        runtime.Stop();
        runtime.Stop();
        Assert.Equal(WorldChannelLifecycleState.Stopped, runtime.State);
        Assert.Equal(2UL, runtime.CurrentTick);
    }

    [Fact]
    public void Rejects_illegal_lifecycle_transitions()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(runtime.Step);
        Assert.Throws<InvalidOperationException>(runtime.BeginDrain);
        Assert.Throws<InvalidOperationException>(runtime.Stop);

        runtime.Start();
        Assert.Throws<InvalidOperationException>(runtime.Start);
        runtime.Stop();
        Assert.Throws<InvalidOperationException>(runtime.Start);
        Assert.Throws<InvalidOperationException>(runtime.Step);
        Assert.Throws<InvalidOperationException>(runtime.BeginDrain);
    }

    private static WorldChannelRuntime CreateRuntime(Guid instanceId) =>
        new(
            new("world_1"),
            new("channel_1"),
            new(instanceId),
            Draft0GrayboxCatalog.FirstPlayable);
}
