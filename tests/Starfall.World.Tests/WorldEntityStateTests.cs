using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Movement;
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
            GroundMovementTickOutcome.Idle));
        Assert.Throws<ArgumentException>(() => new WorldPlayerState(
            entityId,
            position,
            Vector2.Zero,
            Vector2.Zero,
            new PlayerCollisionCapsule(0.35f, 1.8f),
            GroundMovementTickOutcome.Idle));
        Assert.Throws<ArgumentException>(() => new WorldPlayerState(
            entityId,
            position,
            Vector2.Zero,
            new Vector2(2.0f, 0.0f),
            new PlayerCollisionCapsule(0.35f, 1.8f),
            GroundMovementTickOutcome.Idle));
    }

    [Fact]
    public void Monster_state_requires_stable_identity_and_positive_health()
    {
        WorldEntityId entityId = new(1);
        GroundPoint position = new(55.0f, 65.0f);

        Assert.Throws<ArgumentException>(() => new WorldMonsterState(
            entityId,
            "",
            "spawn_easy_01",
            "starter_flyer_light",
            position,
            700,
            0));
        Assert.Throws<ArgumentException>(() => new WorldMonsterState(
            entityId,
            "camp_easy",
            "",
            "starter_flyer_light",
            position,
            700,
            0));
        Assert.Throws<ArgumentException>(() => new WorldMonsterState(
            entityId,
            "camp_easy",
            "spawn_easy_01",
            "",
            position,
            700,
            0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldMonsterState(
            entityId,
            "camp_easy",
            "spawn_easy_01",
            "starter_flyer_light",
            position,
            0,
            0));

        var monster = new WorldMonsterState(
            entityId,
            "camp_easy",
            "spawn_easy_01",
            "starter_flyer_light",
            position,
            700,
            42);
        Assert.Equal(entityId, monster.EntityId);
        Assert.Equal("camp_easy", monster.CampId);
        Assert.Equal("spawn_easy_01", monster.SpawnId);
        Assert.Equal("starter_flyer_light", monster.ArchetypeId);
        Assert.Equal(position, monster.Position);
        Assert.Equal(700, monster.HealthUnits);
        Assert.Equal(42UL, monster.SpawnedAtTick);
    }
}
