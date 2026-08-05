using System.Numerics;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Simulation.Combat;
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
    public void Start_creates_the_exact_initial_monster_population_in_canonical_order()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());

        Assert.Empty(runtime.Monsters);
        Assert.Equal(0, runtime.MonsterCount);

        runtime.Start();

        WorldMonsterState[] monsters = runtime.Monsters.ToArray();
        Draft0CampSpawnAssignment[] expectedAssignments = Draft0CampPolicyCatalog.FirstPlayable.Camps
            .SelectMany(static camp => camp.PlacementSlots)
            .ToArray();
        string[] expectedCampIds = Draft0CampPolicyCatalog.FirstPlayable.Camps
            .SelectMany(static camp => camp.PlacementSlots.Select(_ => camp.Camp.Id))
            .ToArray();
        IReadOnlyDictionary<string, int> expectedHealth = Draft0StarterMonsterCatalog.FirstPlayable.Archetypes
            .ToDictionary(static archetype => archetype.Id, static archetype => archetype.AuthoritativeHealthUnits);

        Assert.Equal(10, runtime.MonsterCount);
        Assert.Equal(expectedAssignments.Length, monsters.Length);
        for (var index = 0; index < monsters.Length; index++)
        {
            WorldMonsterState monster = monsters[index];
            Draft0CampSpawnAssignment assignment = expectedAssignments[index];
            Assert.Equal(expectedCampIds[index], monster.CampId);
            Assert.Equal(assignment.SpawnId, monster.SpawnId);
            Assert.Equal(assignment.ArchetypeId, monster.ArchetypeId);
            Assert.Equal(assignment.Point, monster.Position);
            Assert.Equal(expectedHealth[assignment.ArchetypeId], monster.HealthUnits);
            Assert.Equal(0UL, monster.SpawnedAtTick);
            if (index > 0)
                Assert.True(monster.EntityId.Value > monsters[index - 1].EntityId.Value);
            Assert.True(runtime.TryGetMonster(monster.EntityId, out WorldMonsterState? found));
            Assert.Same(monster, found);
        }
    }

    [Fact]
    public void Rejects_a_missing_world_layout()
    {
        Assert.Throws<ArgumentNullException>(() => new WorldChannelRuntime(
            new("world_1"),
            new("channel_1"),
            new(Guid.NewGuid()),
            null!,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable));

        Assert.Throws<ArgumentNullException>(() => new WorldChannelRuntime(
            new("world_1"),
            new("channel_1"),
            new(Guid.NewGuid()),
            Draft0GrayboxCatalog.FirstPlayable,
            null!,
            Draft0CampPolicyCatalog.FirstPlayable));

        Assert.Throws<ArgumentNullException>(() => new WorldChannelRuntime(
            new("world_1"),
            new("channel_1"),
            new(Guid.NewGuid()),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
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

        Assert.True(player.EntityId.Value > runtime.Monsters.Max(static monster => monster.EntityId.Value));
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
        Assert.True(first.EntityId.Value < third.EntityId.Value);
        Assert.True(third.EntityId.Value < fourth.EntityId.Value);
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
        Assert.Throws<InvalidOperationException>(() => runtime.RemovePlayer(new WorldEntityId(ulong.MaxValue)));

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

        Assert.Equal(
            first.Monsters.Select(static monster => monster.EntityId),
            second.Monsters.Select(static monster => monster.EntityId));
        Assert.Equal(first.CreateTechnicalPlayer().EntityId, second.CreateTechnicalPlayer().EntityId);
    }

    [Fact]
    public void Removal_replenishes_the_same_slot_at_the_exact_eligible_tick_with_a_fresh_identity()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        runtime.Start();
        WorldPlayerState player = runtime.CreateTechnicalPlayer();
        WorldMonsterState original = Assert.Single(
            runtime.Monsters,
            static monster => monster.SpawnId == "spawn_easy_02");
        IReadOnlyList<WorldMonsterState> snapshot = runtime.Monsters;

        Assert.True(runtime.RemoveMonster(original.EntityId));
        Assert.False(runtime.RemoveMonster(original.EntityId));
        Assert.False(runtime.TryGetMonster(original.EntityId, out _));
        Assert.Equal(9, runtime.MonsterCount);

        for (var tick = 0; tick < 599; tick++)
            runtime.Step();

        Assert.DoesNotContain(runtime.Monsters, static monster => monster.SpawnId == "spawn_easy_02");
        runtime.Step();

        WorldMonsterState replacement = Assert.Single(
            runtime.Monsters,
            static monster => monster.SpawnId == "spawn_easy_02");
        Assert.NotEqual(original.EntityId, replacement.EntityId);
        Assert.True(replacement.EntityId.Value > original.EntityId.Value);
        Assert.True(replacement.EntityId.Value > player.EntityId.Value);
        Assert.Equal(original.CampId, replacement.CampId);
        Assert.Equal(original.ArchetypeId, replacement.ArchetypeId);
        Assert.Equal(original.Position, replacement.Position);
        Assert.Equal(original.HealthUnits, replacement.HealthUnits);
        Assert.Equal(600UL, replacement.SpawnedAtTick);
        Assert.Equal(10, runtime.MonsterCount);

        Assert.Equal(10, snapshot.Count);
        Assert.Same(original, Assert.Single(snapshot, monster => monster.EntityId == original.EntityId));
    }

    [Fact]
    public void Simultaneous_replenishment_applies_canonical_camp_and_slot_order()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        runtime.Start();
        string[] reverseRemovalOrder = ["spawn_hard_02", "spawn_mixed_03", "spawn_easy_03"];

        foreach (string spawnId in reverseRemovalOrder)
        {
            WorldMonsterState monster = Assert.Single(runtime.Monsters, candidate => candidate.SpawnId == spawnId);
            Assert.True(runtime.RemoveMonster(monster.EntityId));
        }

        for (var tick = 0; tick < 600; tick++)
            runtime.Step();

        WorldMonsterState[] replacements = runtime.Monsters
            .Where(static monster => monster.SpawnedAtTick == 600)
            .ToArray();
        Assert.Equal(
            ["spawn_easy_03", "spawn_mixed_03", "spawn_hard_02"],
            replacements.Select(static monster => monster.SpawnId));
        Assert.True(replacements[0].EntityId.Value < replacements[1].EntityId.Value);
        Assert.True(replacements[1].EntityId.Value < replacements[2].EntityId.Value);
    }

    [Fact]
    public void Draining_continues_existing_monster_simulation_and_stop_clears_it()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        runtime.Start();
        WorldMonsterState removed = runtime.Monsters[0];
        runtime.BeginDrain();

        Assert.True(runtime.RemoveMonster(removed.EntityId));
        for (var tick = 0; tick < 600; tick++)
            runtime.Step();

        Assert.Equal(WorldChannelLifecycleState.Draining, runtime.State);
        Assert.Equal(10, runtime.MonsterCount);
        Assert.Contains(runtime.Monsters, monster =>
            monster.SpawnId == removed.SpawnId && monster.EntityId != removed.EntityId);

        runtime.Stop();
        Assert.Equal(0, runtime.MonsterCount);
        Assert.Empty(runtime.Monsters);
        Assert.False(runtime.TryGetMonster(removed.EntityId, out _));
        Assert.Throws<InvalidOperationException>(() => runtime.RemoveMonster(removed.EntityId));
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

    [Fact]
    public void Basic_arrow_stops_faces_resolves_exact_damage_and_replenishes_defeat_once()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        runtime.Start();
        WorldPlayerState player = runtime.CreateTechnicalPlayer();
        MovePlayerTo(runtime, player.EntityId, new GroundPoint(100.0f, 70.0f));
        MovePlayerTo(runtime, player.EntityId, new GroundPoint(70.0f, 65.0f));

        WorldMonsterState original = Assert.Single(
            runtime.Monsters,
            static monster => monster.SpawnId == "spawn_easy_03");
        IReadOnlyList<WorldMonsterState> originalSnapshot = runtime.Monsters;
        ulong? previousStartTick = null;

        for (var shot = 0; shot < 3; shot++)
        {
            BasicArrowStartEvaluation started = runtime.SubmitBasicArrow(
                new BasicArrowIntent("basic_arrow", player.EntityId, original.EntityId));
            Assert.Equal(BasicArrowStartDisposition.Accepted, started.Disposition);
            PendingBasicArrow pending = Assert.IsType<PendingBasicArrow>(started.PendingAction);
            Assert.Equal(runtime.CurrentTick, pending.StartTick);
            Assert.Equal(pending.StartTick + 12, pending.ResolveTick);
            Assert.Equal(pending.StartTick + 48, pending.NextAllowedStartTick);
            if (previousStartTick is { } prior)
                Assert.Equal(48UL, pending.StartTick - prior);
            previousStartTick = pending.StartTick;

            Assert.True(runtime.TryGetPlayer(player.EntityId, out WorldPlayerState? stopped));
            Assert.NotNull(stopped);
            Assert.Equal(Vector2.Zero, stopped.VelocityMetresPerSecond);
            Assert.Equal(-Vector2.UnitX, stopped.Facing);
            Assert.Equal(1, runtime.PendingBasicArrowCount);
            if (shot == 0)
            {
                Assert.Equal(
                    GroundMovementIntentDisposition.OutsideWalkableBounds,
                    runtime.SubmitMovementIntent(player.EntityId, new GroundPoint(5.0f, 100.0f)));
                Assert.Equal(1, runtime.PendingBasicArrowCount);
            }

            for (var tick = 0; tick < 11; tick++)
            {
                runtime.Step();
                Assert.Empty(runtime.LastBasicArrowResolutions);
                Assert.True(runtime.TryGetMonster(original.EntityId, out _));
            }

            runtime.Step();
            BasicArrowResolution resolution = Assert.Single(runtime.LastBasicArrowResolutions);
            Assert.Equal(BasicArrowResolutionDisposition.Resolved, resolution.Disposition);
            AuthoritativeDamageResult damage = Assert.IsType<AuthoritativeDamageResult>(resolution.Damage);
            Assert.Equal(300, damage.RequestedDamageUnits);
            Assert.Equal(shot == 2 ? 100 : 300, damage.AppliedDamageUnits);
            Assert.Equal(shot == 0 ? 400 : shot == 1 ? 100 : 0, damage.RemainingHealthUnits);
            Assert.Equal(shot == 2, damage.Defeated);
            Assert.Equal(0, runtime.PendingBasicArrowCount);

            if (shot < 2)
            {
                Assert.True(runtime.TryGetMonster(original.EntityId, out WorldMonsterState? damaged));
                Assert.NotNull(damaged);
                Assert.Equal(damage.RemainingHealthUnits, damaged.HealthUnits);
                while (runtime.CurrentTick < pending.NextAllowedStartTick)
                    runtime.Step();
            }
        }

        Assert.False(runtime.TryGetMonster(original.EntityId, out _));
        Assert.Equal(9, runtime.MonsterCount);
        Assert.Equal(700, Assert.Single(originalSnapshot, monster => monster.EntityId == original.EntityId).HealthUnits);

        ulong defeatedAtTick = runtime.CurrentTick;
        for (var tick = 0; tick < 599; tick++)
            runtime.Step();
        Assert.DoesNotContain(runtime.Monsters, static monster => monster.SpawnId == "spawn_easy_03");
        runtime.Step();

        WorldMonsterState replacement = Assert.Single(
            runtime.Monsters,
            static monster => monster.SpawnId == "spawn_easy_03");
        Assert.NotEqual(original.EntityId, replacement.EntityId);
        Assert.Equal(700, replacement.HealthUnits);
        Assert.Equal(defeatedAtTick + 600, replacement.SpawnedAtTick);
        Assert.Equal(10, runtime.MonsterCount);
    }

    [Fact]
    public void Basic_arrow_rejects_unknown_facts_and_follows_world_lifecycle()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());
        BasicArrowIntent unknown = new("basic_arrow", new WorldEntityId(100), new WorldEntityId(101));

        Assert.Throws<InvalidOperationException>(() => runtime.SubmitBasicArrow(unknown));
        runtime.Start();
        Assert.Equal(BasicArrowStartDisposition.UnknownActor, runtime.SubmitBasicArrow(unknown).Disposition);

        WorldPlayerState player = runtime.CreateTechnicalPlayer();
        Assert.Equal(
            BasicArrowStartDisposition.UnknownTarget,
            runtime.SubmitBasicArrow(new BasicArrowIntent("basic_arrow", player.EntityId, new WorldEntityId(101))).Disposition);
        WorldMonsterState distant = runtime.Monsters[0];
        Assert.Equal(
            BasicArrowStartDisposition.TargetOutOfRange,
            runtime.SubmitBasicArrow(new BasicArrowIntent("basic_arrow", player.EntityId, distant.EntityId)).Disposition);

        runtime.Stop();
        Assert.Throws<InvalidOperationException>(() => runtime.SubmitBasicArrow(unknown));
    }

    private static void MovePlayerTo(
        WorldChannelRuntime runtime,
        WorldEntityId playerId,
        GroundPoint destination)
    {
        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            runtime.SubmitMovementIntent(playerId, destination));
        for (var tick = 0; tick < 2_000; tick++)
        {
            runtime.Step();
            Assert.True(runtime.TryGetPlayer(playerId, out WorldPlayerState? player));
            Assert.NotNull(player);
            if (player.Position == destination)
                return;
            Assert.NotEqual(GroundMovementTickOutcome.Blocked, player.MovementOutcome);
        }

        throw new InvalidOperationException("Technical player did not reach the combat fixture position.");
    }

    private static WorldChannelRuntime CreateRuntime(Guid instanceId) =>
        new(
            new("world_1"),
            new("channel_1"),
            new(instanceId),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
}
