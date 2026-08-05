using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Monsters;
using Starfall.Simulation.Movement;
using Starfall.Simulation.Players;
using Starfall.World.Entities;

namespace Starfall.World.Tests;

public sealed class WorldEntityStateTests
{
    [Fact]
    public void World_entity_identity_requires_a_positive_value()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldEntityId(0));
        Assert.Equal(42UL, new WorldEntityId(42).Value);
        Assert.Equal("42", new WorldEntityId(42).ToString());
    }

    [Fact]
    public void Identity_sequence_allocates_monotonically_without_reuse()
    {
        var sequence = new WorldEntityIdSequence();

        Assert.Equal(1UL, sequence.Allocate().Value);
        Assert.Equal(2UL, sequence.Allocate().Value);
        Assert.Equal(3UL, sequence.Allocate().Value);
    }

    [Fact]
    public void Identity_sequence_allocates_the_final_value_once_then_fails_explicitly()
    {
        var sequence = new WorldEntityIdSequence(ulong.MaxValue);

        Assert.Equal(ulong.MaxValue, sequence.Allocate().Value);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => sequence.Allocate());
        Assert.Equal("The world entity identity space is exhausted.", exception.Message);
    }

    [Fact]
    public void Identity_sequence_rejects_a_zero_start()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldEntityIdSequence(0));
    }

    [Fact]
    public void Player_state_requires_finite_velocity_and_normalized_facing()
    {
        WorldEntityId entityId = new(1);
        GroundPoint position = new(100.0f, 25.0f);

        Assert.Throws<ArgumentException>(() => new WorldPlayerState(
            entityId,
            position,
            new Vector2(float.NaN, 0.0f),
            Vector2.UnitY,
            new PlayerCollisionCapsule(0.35f, 1.8f),
            GroundMovementTickOutcome.Idle,
            2_500,
            Draft0PlayerLifeStatus.Active,
            null));
        Assert.Throws<ArgumentException>(() => new WorldPlayerState(
            entityId,
            position,
            Vector2.Zero,
            Vector2.Zero,
            new PlayerCollisionCapsule(0.35f, 1.8f),
            GroundMovementTickOutcome.Idle,
            2_500,
            Draft0PlayerLifeStatus.Active,
            null));
        Assert.Throws<ArgumentException>(() => new WorldPlayerState(
            entityId,
            position,
            Vector2.Zero,
            new Vector2(2.0f, 0.0f),
            new PlayerCollisionCapsule(0.35f, 1.8f),
            GroundMovementTickOutcome.Idle,
            2_500,
            Draft0PlayerLifeStatus.Active,
            null));
    }

    [Fact]
    public void Player_life_state_requires_consistent_health_and_respawn_state()
    {
        WorldEntityId entityId = new(1);
        GroundPoint position = new(100.0f, 25.0f);
        PlayerCollisionCapsule collision = new(0.35f, 1.8f);

        Assert.Throws<ArgumentException>(() => new WorldPlayerState(
            entityId, position, Vector2.Zero, Vector2.UnitY, collision,
            GroundMovementTickOutcome.Idle, 0, Draft0PlayerLifeStatus.Active, null));
        Assert.Throws<ArgumentException>(() => new WorldPlayerState(
            entityId, position, Vector2.Zero, Vector2.UnitY, collision,
            GroundMovementTickOutcome.Idle, 1, Draft0PlayerLifeStatus.Defeated, 10));
        Assert.Throws<ArgumentException>(() => new WorldPlayerState(
            entityId, position, Vector2.Zero, Vector2.UnitY, collision,
            GroundMovementTickOutcome.Idle, 0, Draft0PlayerLifeStatus.Defeated, null));
    }

    [Fact]
    public void Monster_state_requires_stable_identity_and_positive_health()
    {
        WorldEntityId entityId = new(1);
        GroundPoint position = new(55.0f, 65.0f);
        Draft0MonsterBehaviorState behavior = CreateBehavior(entityId, position);

        Assert.Throws<ArgumentException>(() => new WorldMonsterState(
            default,
            700,
            0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldMonsterState(
            behavior,
            0,
            0));

        var monster = new WorldMonsterState(
            behavior,
            700,
            42);
        Assert.Equal(entityId, monster.EntityId);
        Assert.Equal("camp_easy", monster.CampId);
        Assert.Equal("spawn_easy_01", monster.SpawnId);
        Assert.Equal("starter_flyer_light", monster.ArchetypeId);
        Assert.Equal(position, monster.Position);
        Assert.Equal(700, monster.HealthUnits);
        Assert.Equal(700, monster.MaximumHealthUnits);
        Assert.Equal(42UL, monster.SpawnedAtTick);

        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldMonsterState(
            behavior,
            701,
            700,
            0));
    }

    private static Draft0MonsterBehaviorState CreateBehavior(
        WorldEntityId entityId,
        GroundPoint position) =>
        new(
            entityId,
            "camp_easy",
            "spawn_easy_01",
            "starter_flyer_light",
            position,
            position,
            Vector2.Zero,
            Vector2.UnitX,
            0.45f,
            Draft0MonsterBehaviorMode.Idle,
            null,
            0);
}
