using System.Numerics;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Simulation.Combat;
using Starfall.Simulation.Monsters;
using Starfall.Simulation.Movement;
using Starfall.Simulation.Players;
using Starfall.World.Entities;
using Starfall.World.Lifecycle;

namespace Starfall.World.Tests;

public sealed class WorldPlayerLifeIntegrationTests
{
    [Fact]
    public void Monster_damage_defeats_locks_and_respawns_the_same_player_at_the_exact_tick()
    {
        WorldChannelRuntime runtime = CreateRuntime(new Draft0PlayerLifeTuning(100, 100, 180));
        runtime.Start();
        WorldPlayerState initial = runtime.CreateTechnicalPlayer();
        Assert.Equal(100, initial.HealthUnits);
        Assert.True(initial.IsActive);

        MovePlayerTo(runtime, initial.EntityId, new GroundPoint(100.0f, 70.0f));
        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            runtime.SubmitMovementIntent(initial.EntityId, new GroundPoint(70.0f, 65.0f)));

        WorldPlayerState defeated = WaitForDefeat(runtime, initial.EntityId);
        ulong defeatedAtTick = runtime.CurrentTick;
        Assert.Equal(0, defeated.HealthUnits);
        Assert.Equal(Draft0PlayerLifeStatus.Defeated, defeated.LifeStatus);
        Assert.Equal(defeatedAtTick + 180, defeated.RespawnAtTick);
        Assert.Equal(Vector2.Zero, defeated.VelocityMetresPerSecond);
        Draft0AppliedMonsterDamage lethal = Assert.Single(runtime.LastAppliedMonsterDamage);
        Assert.Equal(initial.EntityId, lethal.Attack.TargetEntityId);
        Assert.Equal(100, lethal.Damage.AppliedDamageUnits);
        Assert.True(lethal.Damage.Defeated);
        Assert.True(initial.IsActive);
        Assert.Equal(100, initial.HealthUnits);

        Assert.Equal(
            GroundMovementIntentDisposition.UnknownPlayer,
            runtime.SubmitMovementIntent(initial.EntityId, new GroundPoint(100.0f, 70.0f)));
        Assert.Equal(
            BasicArrowStartDisposition.ActorDefeated,
            runtime.SubmitBasicArrow(new BasicArrowIntent(
                "basic_arrow",
                initial.EntityId,
                runtime.Monsters[0].EntityId)).Disposition);

        for (var tick = 0; tick < 179; tick++)
        {
            runtime.Step();
            Assert.Empty(runtime.LastPlayerRespawns);
        }

        Assert.True(runtime.TryGetPlayer(initial.EntityId, out WorldPlayerState? stillDefeated));
        Assert.NotNull(stillDefeated);
        Assert.False(stillDefeated.IsActive);

        runtime.Step();

        Draft0PlayerRespawnOutcome respawn = Assert.Single(runtime.LastPlayerRespawns);
        Assert.Equal(initial.EntityId, respawn.PlayerEntityId);
        Assert.Equal(defeatedAtTick + 180, respawn.RespawnedAtTick);
        Assert.Equal(runtime.Layout.Town.RespawnAnchor, respawn.Position);
        Assert.Equal(100, respawn.RestoredHealthUnits);
        Assert.True(runtime.TryGetPlayer(initial.EntityId, out WorldPlayerState? restored));
        Assert.NotNull(restored);
        Assert.True(restored.IsActive);
        Assert.Equal(100, restored.HealthUnits);
        Assert.Equal(runtime.Layout.Town.RespawnAnchor, restored.Position);
        Assert.Equal(Vector2.UnitY, restored.Facing);
        Assert.Equal(Vector2.Zero, restored.VelocityMetresPerSecond);
        Assert.Equal(
            BasicArrowStartDisposition.ActorInProtectedTown,
            runtime.SubmitBasicArrow(new BasicArrowIntent(
                "basic_arrow",
                initial.EntityId,
                runtime.Monsters[0].EntityId)).Disposition);
        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            runtime.SubmitMovementIntent(initial.EntityId, new GroundPoint(100.0f, 60.0f)));

        runtime.Step();
        Assert.Empty(runtime.LastPlayerRespawns);
        runtime.Stop();
    }

    [Fact]
    public void Protected_town_rejects_hostile_actions_and_monsters_never_enter_it()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        runtime.Start();
        WorldPlayerState player = runtime.CreateTechnicalPlayer();

        Assert.Equal(
            BasicArrowStartDisposition.ActorInProtectedTown,
            runtime.SubmitBasicArrow(new BasicArrowIntent(
                "basic_arrow",
                player.EntityId,
                runtime.Monsters[0].EntityId)).Disposition);

        for (var tick = 0; tick < 300; tick++)
        {
            runtime.Step();
            Assert.All(runtime.Monsters, monster =>
                Assert.False(runtime.Layout.Town.Bounds.Contains(monster.Position)));
            Assert.All(runtime.Monsters, monster => Assert.Null(monster.Behavior.TargetEntityId));
        }
        runtime.Stop();
    }

    [Fact]
    public void Draining_continues_the_exact_respawn_schedule()
    {
        WorldChannelRuntime runtime = CreateRuntime(new Draft0PlayerLifeTuning(100, 100, 2));
        runtime.Start();
        WorldPlayerState player = runtime.CreateTechnicalPlayer();
        MovePlayerTo(runtime, player.EntityId, new GroundPoint(100.0f, 70.0f));
        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            runtime.SubmitMovementIntent(player.EntityId, new GroundPoint(70.0f, 65.0f)));
        _ = WaitForDefeat(runtime, player.EntityId);
        runtime.BeginDrain();

        runtime.Step();
        Assert.Empty(runtime.LastPlayerRespawns);
        runtime.Step();
        Assert.Single(runtime.LastPlayerRespawns);
        Assert.Equal(WorldChannelLifecycleState.Draining, runtime.State);

        runtime.Stop();
        Assert.Empty(runtime.LastAppliedMonsterDamage);
        Assert.Empty(runtime.LastPlayerRespawns);
    }

    [Fact]
    public void Technical_removal_handles_a_defeated_player_without_movement_state()
    {
        WorldChannelRuntime runtime = CreateRuntime(new Draft0PlayerLifeTuning(100, 100, 180));
        runtime.Start();
        WorldPlayerState player = runtime.CreateTechnicalPlayer();
        MovePlayerTo(runtime, player.EntityId, new GroundPoint(100.0f, 70.0f));
        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            runtime.SubmitMovementIntent(player.EntityId, new GroundPoint(70.0f, 65.0f)));
        _ = WaitForDefeat(runtime, player.EntityId);

        Assert.True(runtime.RemovePlayer(player.EntityId));
        Assert.False(runtime.TryGetPlayer(player.EntityId, out _));
        Assert.Equal(
            GroundMovementIntentDisposition.UnknownPlayer,
            runtime.SubmitMovementIntent(player.EntityId, new GroundPoint(100.0f, 25.0f)));
        runtime.Stop();
    }

    private static WorldPlayerState WaitForDefeat(
        WorldChannelRuntime runtime,
        Starfall.Simulation.Entities.WorldEntityId playerId)
    {
        for (var tick = 0; tick < 600; tick++)
        {
            runtime.Step();
            Assert.True(runtime.TryGetPlayer(playerId, out WorldPlayerState? player));
            Assert.NotNull(player);
            if (!player.IsActive)
                return player;
        }

        throw new InvalidOperationException("Player was not defeated by the bounded monster fixture.");
    }

    private static void MovePlayerTo(
        WorldChannelRuntime runtime,
        Starfall.Simulation.Entities.WorldEntityId playerId,
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
        }

        throw new InvalidOperationException("Player did not reach the safe fixture point.");
    }

    private static WorldChannelRuntime CreateRuntime(Draft0PlayerLifeTuning? tuning = null) =>
        new(
            new("world_1"),
            new("channel_1"),
            new(Guid.NewGuid()),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable,
            tuning);
}
