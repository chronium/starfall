using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Movement;
using Starfall.World.Entities;
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

    [Fact]
    public void Creates_the_technical_player_from_the_catalog_respawn_anchor()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        runtime.Start();

        WorldPlayerState player = runtime.CreateTechnicalPlayer();

        Assert.Equal(new WorldEntityId(1), player.EntityId);
        Assert.Equal(runtime.Layout.Town.RespawnAnchor, player.Position);
        Assert.Equal(new GroundPoint(100.0f, 25.0f), player.Position);
        Assert.Equal(Vector2.Zero, player.VelocityMetresPerSecond);
        Assert.Equal(Vector2.UnitY, player.Facing);
        Assert.Equal(0.35f, player.Collision.RadiusMetres);
        Assert.Equal(1.8f, player.Collision.HeightMetres);
        Assert.Equal(GroundMovementTickOutcome.Idle, player.MovementOutcome);
        Assert.Equal(1, runtime.PlayerCount);
        Assert.True(runtime.TryGetPlayer(player.EntityId, out WorldPlayerState? found));
        Assert.Same(player, found);
    }

    [Fact]
    public void Orders_players_by_monotonic_identity_and_never_reuses_removed_ids()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        runtime.Start();
        WorldPlayerState first = runtime.CreateTechnicalPlayer();
        WorldPlayerState removed = runtime.CreateTechnicalPlayer();
        WorldPlayerState third = runtime.CreateTechnicalPlayer();

        Assert.True(runtime.RemovePlayer(removed.EntityId));
        Assert.False(runtime.RemovePlayer(removed.EntityId));
        WorldPlayerState fourth = runtime.CreateTechnicalPlayer();

        Assert.Equal(
            [first.EntityId, third.EntityId, fourth.EntityId],
            runtime.Players.Select(static player => player.EntityId));
        Assert.Equal(4UL, fourth.EntityId.Value);
        Assert.False(runtime.TryGetPlayer(removed.EntityId, out _));
    }

    [Fact]
    public void Player_snapshots_are_stable_after_later_creation_and_removal()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        runtime.Start();
        WorldPlayerState first = runtime.CreateTechnicalPlayer();
        IReadOnlyList<WorldPlayerState> snapshot = runtime.Players;

        WorldPlayerState second = runtime.CreateTechnicalPlayer();
        Assert.True(runtime.RemovePlayer(first.EntityId));

        Assert.Single(snapshot);
        Assert.Same(first, snapshot[0]);
        Assert.Equal(new GroundPoint(100.0f, 25.0f), snapshot[0].Position);
        Assert.Equal([second.EntityId], runtime.Players.Select(static player => player.EntityId));
    }

    [Fact]
    public void Player_lifecycle_retains_during_drain_and_clears_on_stop()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(runtime.CreateTechnicalPlayer);
        Assert.Throws<InvalidOperationException>(() => runtime.RemovePlayer(new WorldEntityId(1)));

        runtime.Start();
        WorldPlayerState removed = runtime.CreateTechnicalPlayer();
        WorldPlayerState retained = runtime.CreateTechnicalPlayer();
        runtime.BeginDrain();

        Assert.Equal(2, runtime.PlayerCount);
        Assert.Equal([removed.EntityId, retained.EntityId], runtime.Players.Select(static player => player.EntityId));
        Assert.Throws<InvalidOperationException>(runtime.CreateTechnicalPlayer);
        Assert.True(runtime.RemovePlayer(removed.EntityId));
        Assert.Same(retained, Assert.Single(runtime.Players));

        runtime.Stop();
        Assert.Equal(0, runtime.PlayerCount);
        Assert.Empty(runtime.Players);
        Assert.Throws<InvalidOperationException>(() => runtime.RemovePlayer(retained.EntityId));
    }

    [Fact]
    public void Independent_world_runtimes_allocate_their_own_identity_space()
    {
        WorldChannelRuntime first = CreateRuntime(Guid.NewGuid());
        WorldChannelRuntime second = CreateRuntime(Guid.NewGuid());
        first.Start();
        second.Start();

        Assert.Equal(new WorldEntityId(1), first.CreateTechnicalPlayer().EntityId);
        Assert.Equal(new WorldEntityId(1), second.CreateTechnicalPlayer().EntityId);
    }

    [Fact]
    public void Fixed_ticks_replace_immutable_player_state_with_authoritative_movement()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        runtime.Start();
        WorldPlayerState original = runtime.CreateTechnicalPlayer();

        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            runtime.SubmitMovementIntent(original.EntityId, new GroundPoint(104.0f, 25.0f)));
        for (var tick = 0; tick < 60; tick++)
            runtime.Step();

        Assert.True(runtime.TryGetPlayer(original.EntityId, out WorldPlayerState? moved));
        Assert.NotNull(moved);
        Assert.NotSame(original, moved);
        Assert.Equal(new GroundPoint(100.0f, 25.0f), original.Position);
        Assert.Equal(Vector2.Zero, original.VelocityMetresPerSecond);
        Assert.Equal(new GroundPoint(104.0f, 25.0f), moved.Position);
        Assert.Equal(Vector2.Zero, moved.VelocityMetresPerSecond);
        Assert.Equal(Vector2.UnitX, moved.Facing);
        Assert.Equal(GroundMovementTickOutcome.Arrived, moved.MovementOutcome);
    }

    [Fact]
    public void Existing_players_continue_to_accept_movement_while_draining()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        runtime.Start();
        WorldPlayerState player = runtime.CreateTechnicalPlayer();
        runtime.BeginDrain();

        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            runtime.SubmitMovementIntent(player.EntityId, new GroundPoint(101.0f, 25.0f)));
        runtime.Step();

        Assert.True(runtime.TryGetPlayer(player.EntityId, out WorldPlayerState? moved));
        Assert.NotNull(moved);
        Assert.True(moved.Position.XMetres > player.Position.XMetres);
        Assert.Equal(GroundMovementTickOutcome.Moving, moved.MovementOutcome);
    }

    [Fact]
    public void Movement_submission_follows_world_and_player_lifecycle()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        WorldEntityId unknown = new(1);

        Assert.Throws<InvalidOperationException>(() =>
            runtime.SubmitMovementIntent(unknown, new GroundPoint(100.0f, 25.0f)));
        runtime.Start();
        Assert.Equal(
            GroundMovementIntentDisposition.UnknownPlayer,
            runtime.SubmitMovementIntent(unknown, new GroundPoint(100.0f, 25.0f)));

        WorldPlayerState player = runtime.CreateTechnicalPlayer();
        Assert.True(runtime.RemovePlayer(player.EntityId));
        Assert.Equal(
            GroundMovementIntentDisposition.UnknownPlayer,
            runtime.SubmitMovementIntent(player.EntityId, new GroundPoint(100.0f, 30.0f)));

        runtime.Stop();
        Assert.Throws<InvalidOperationException>(() =>
            runtime.SubmitMovementIntent(player.EntityId, new GroundPoint(100.0f, 30.0f)));
    }

    private static WorldChannelRuntime CreateRuntime(Guid instanceId) =>
        new(
            new("world_1"),
            new("channel_1"),
            new(instanceId),
            Draft0GrayboxCatalog.FirstPlayable);
}
