using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Monsters;
using Starfall.Simulation.Movement;
using Starfall.World.Entities;
using Starfall.World.Lifecycle;

namespace Starfall.World.Tests;

public sealed class WorldMonsterBehaviorIntegrationTests
{
    [Fact]
    public void World_advances_behavior_and_publishes_ordered_attack_facts_without_player_health()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        runtime.Start();
        WorldPlayerState player = runtime.CreateTechnicalPlayer();
        MovePlayerIntoEasyCamp(runtime, player.EntityId);

        var attacks = new List<Draft0MonsterAttackResolution>();
        for (var tick = 0; tick < 180; tick++)
        {
            runtime.Step();
            attacks.AddRange(runtime.LastMonsterAttackResolutions);
            Assert.Equal(
                runtime.LastMonsterAttackResolutions.OrderBy(static attack => attack.AttackerEntityId.Value),
                runtime.LastMonsterAttackResolutions);
        }

        Assert.True(
            attacks.Count > 0,
            string.Join(
                "; ",
                runtime.Monsters.Select(monster =>
                    $"{monster.SpawnId}:{monster.Behavior.Mode}:{monster.Position.XMetres},{monster.Position.ZMetres}:{monster.Behavior.TargetEntityId}")));
        Assert.All(attacks, attack =>
        {
            Assert.Equal(player.EntityId, attack.TargetEntityId);
            Assert.Equal(100, attack.RequestedDamageUnits);
        });
        Assert.True(runtime.TryGetPlayer(player.EntityId, out WorldPlayerState? retainedPlayer));
        Assert.NotNull(retainedPlayer);
        Assert.Equal(new GroundPoint(70.0f, 65.0f), retainedPlayer.Position);
    }

    [Fact]
    public void Killed_monsters_cannot_publish_a_same_tick_attack_and_replacements_wait_one_tick()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        runtime.Start();
        WorldMonsterState removed = runtime.Monsters[0];

        Assert.True(runtime.RemoveMonster(removed.EntityId));
        for (var tick = 0; tick < 599; tick++)
            runtime.Step();
        runtime.Step();

        WorldMonsterState replacement = Assert.Single(
            runtime.Monsters,
            monster => monster.SpawnId == removed.SpawnId);
        Assert.Equal(Draft0MonsterBehaviorMode.Idle, replacement.Behavior.Mode);
        Assert.DoesNotContain(
            runtime.LastMonsterAttackResolutions,
            attack => attack.AttackerEntityId == replacement.EntityId);

        runtime.Step();
        Assert.Equal(601UL, runtime.CurrentTick);
        Assert.True(runtime.TryGetMonster(replacement.EntityId, out WorldMonsterState? retained));
        Assert.NotNull(retained);
    }

    [Fact]
    public void Draining_continues_behavior_and_stop_clears_behavior_outputs()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        runtime.Start();
        WorldPlayerState player = runtime.CreateTechnicalPlayer();
        MovePlayerIntoEasyCamp(runtime, player.EntityId);
        runtime.BeginDrain();

        runtime.Step();

        Assert.Equal(WorldChannelLifecycleState.Draining, runtime.State);
        Assert.Contains(runtime.Monsters, static monster => monster.Behavior.TargetEntityId is not null);

        runtime.Stop();

        Assert.Equal(WorldChannelLifecycleState.Stopped, runtime.State);
        Assert.Empty(runtime.LastMonsterAttackResolutions);
        Assert.Empty(runtime.Monsters);
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

        throw new InvalidOperationException("Technical player did not reach the behavior fixture position.");
    }

    private static void MovePlayerIntoEasyCamp(
        WorldChannelRuntime runtime,
        WorldEntityId playerId)
    {
        MovePlayerTo(runtime, playerId, new GroundPoint(100.0f, 70.0f));
        MovePlayerTo(runtime, playerId, new GroundPoint(70.0f, 65.0f));
    }

    private static WorldChannelRuntime CreateRuntime() =>
        new(
            new("world_1"),
            new("channel_1"),
            new(Guid.NewGuid()),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
}
