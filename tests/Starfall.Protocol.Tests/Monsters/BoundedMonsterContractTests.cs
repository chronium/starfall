using System.Collections.Immutable;
using System.Numerics;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Tests.Monsters;

public sealed class BoundedMonsterContractTests
{
    [Fact]
    public void Snapshot_copies_ordered_inputs_and_preserves_exact_facts()
    {
        var live = new List<LiveMonsterSnapshot>
        {
            CreateLive(1, MonsterBehaviorKind.Idle),
            CreateLive(2, MonsterBehaviorKind.Pursuing, targetId: 100),
        };
        var defeated = new List<DefeatedMonsterSnapshot>
        {
            CreateDefeated(3, defeatedAtTick: 9),
        };

        var snapshot = new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(7),
            10,
            live,
            defeated);

        live.Clear();
        defeated.Clear();

        Assert.Equal(7UL, snapshot.Sequence.Value);
        Assert.Equal(10UL, snapshot.SimulationTick);
        Assert.Equal([1UL, 2UL], snapshot.LiveMonsters.Select(static monster => monster.EntityId.Value));
        Assert.Equal([3UL], snapshot.DefeatedMonsters.Select(static monster => monster.EntityId.Value));
        Assert.Equal(MonsterBehaviorKind.Pursuing, snapshot.LiveMonsters[1].Behavior);
        Assert.Equal(100UL, snapshot.LiveMonsters[1].TargetEntityId?.Value);
        Assert.Equal(300, snapshot.LiveMonsters[1].CurrentHealthUnits);
        Assert.Equal(700, snapshot.LiveMonsters[1].MaximumHealthUnits);
        Assert.Equal(9UL, snapshot.DefeatedMonsters[0].DefeatedAtTick);
    }

    [Fact]
    public void Sequence_and_archetype_identities_are_bounded_and_canonical()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonsterSnapshotSequence(0));
        Assert.Equal("42", new MonsterSnapshotSequence(42).ToString());

        Assert.ThrowsAny<ArgumentException>(() => new MonsterArchetypeId(""));
        Assert.ThrowsAny<ArgumentException>(() => new MonsterArchetypeId("Starter_flyer"));
        Assert.ThrowsAny<ArgumentException>(() => new MonsterArchetypeId("1_starter"));
        Assert.ThrowsAny<ArgumentException>(() => new MonsterArchetypeId("starter-flyer"));
        Assert.ThrowsAny<ArgumentException>(() => new MonsterArchetypeId(new string('a', 65)));

        string maximum = $"a{new string('0', 63)}";
        Assert.Equal(maximum, new MonsterArchetypeId(maximum).Value);
        Assert.Equal("starter_flyer_light", new MonsterArchetypeId("starter_flyer_light").ToString());
    }

    [Theory]
    [InlineData(MonsterBehaviorKind.Idle, false)]
    [InlineData(MonsterBehaviorKind.Returning, false)]
    [InlineData(MonsterBehaviorKind.Pursuing, true)]
    [InlineData(MonsterBehaviorKind.Attacking, true)]
    public void Behavior_target_contract_is_explicit(MonsterBehaviorKind behavior, bool requiresTarget)
    {
        LiveMonsterSnapshot snapshot = CreateLive(
            1,
            behavior,
            requiresTarget ? 100UL : null);

        Assert.Equal(requiresTarget, snapshot.TargetEntityId is not null);
        Assert.ThrowsAny<ArgumentException>(() => CreateLive(
            1,
            behavior,
            requiresTarget ? null : 100UL));
    }

    [Fact]
    public void Live_facts_reject_invalid_spatial_health_and_identity_values()
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateLive(0, MonsterBehaviorKind.Idle));
        Assert.ThrowsAny<ArgumentException>(() => CreateLive(1, MonsterBehaviorKind.Idle, facing: Vector2.Zero));
        Assert.ThrowsAny<ArgumentException>(() => CreateLive(
            1,
            MonsterBehaviorKind.Idle,
            velocity: new Vector2(float.NaN, 0.0f)));
        Assert.ThrowsAny<ArgumentException>(() => CreateLive(
            1,
            MonsterBehaviorKind.Idle,
            collisionRadius: 0.0f));
        Assert.ThrowsAny<ArgumentException>(() => CreateLive(
            1,
            MonsterBehaviorKind.Pursuing,
            targetId: 1));
        Assert.ThrowsAny<ArgumentException>(() => CreateLive(
            1,
            MonsterBehaviorKind.Idle,
            currentHealth: 0));
        Assert.ThrowsAny<ArgumentException>(() => CreateLive(
            1,
            MonsterBehaviorKind.Idle,
            currentHealth: 701,
            maximumHealth: 700));
    }

    [Fact]
    public void Defeated_facts_preserve_last_transform_and_zero_tick_is_valid()
    {
        DefeatedMonsterSnapshot defeated = CreateDefeated(5, defeatedAtTick: 0);

        Assert.Equal(5UL, defeated.EntityId.Value);
        Assert.Equal("starter_flyer_light", defeated.ArchetypeId.Value);
        Assert.Equal(new GroundPosition(5.0f, 6.0f), defeated.LastPosition);
        Assert.Equal(Vector2.UnitX, defeated.LastFacing);
        Assert.Equal(0UL, defeated.DefeatedAtTick);

        Assert.ThrowsAny<ArgumentException>(() => new DefeatedMonsterSnapshot(
            new WorldEntityId(5),
            new MonsterArchetypeId("starter_flyer_light"),
            new GroundPosition(5.0f, 6.0f),
            Vector2.Zero,
            0));
    }

    [Fact]
    public void Batch_rejects_default_null_unordered_duplicate_future_and_oversized_inputs()
    {
        Assert.ThrowsAny<ArgumentException>(() => new BoundedMonsterSnapshot(
            default,
            0,
            [],
            []));
        Assert.Throws<ArgumentNullException>(() => new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(1),
            0,
            null!,
            []));
        Assert.ThrowsAny<ArgumentException>(() => new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(1),
            0,
            default(ImmutableArray<LiveMonsterSnapshot>),
            []));
        Assert.ThrowsAny<ArgumentException>(() => new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(1),
            0,
            [CreateLive(2, MonsterBehaviorKind.Idle), CreateLive(1, MonsterBehaviorKind.Idle)],
            []));
        Assert.ThrowsAny<ArgumentException>(() => new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(1),
            0,
            [CreateLive(1, MonsterBehaviorKind.Idle)],
            [CreateDefeated(1, 0)]));
        Assert.ThrowsAny<ArgumentException>(() => new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(1),
            5,
            [],
            [CreateDefeated(1, 6)]));
        Assert.ThrowsAny<ArgumentException>(() => new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(1),
            0,
            Enumerable.Range(1, 11).Select(index => CreateLive((ulong)index, MonsterBehaviorKind.Idle)),
            []));
    }

    [Fact]
    public void Empty_and_ten_entry_snapshots_are_valid_bounds()
    {
        var empty = new BoundedMonsterSnapshot(new MonsterSnapshotSequence(1), 0, [], []);
        var full = new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(2),
            10,
            Enumerable.Range(1, 5).Select(index => CreateLive((ulong)index, MonsterBehaviorKind.Idle)),
            Enumerable.Range(6, 5).Select(index => CreateDefeated((ulong)index, 9)));

        Assert.Empty(empty.LiveMonsters);
        Assert.Empty(empty.DefeatedMonsters);
        Assert.Equal(BoundedMonsterSnapshot.MaxEntries, full.LiveMonsters.Length + full.DefeatedMonsters.Length);
    }

    private static LiveMonsterSnapshot CreateLive(
        ulong entityId,
        MonsterBehaviorKind behavior,
        ulong? targetId = null,
        Vector2? velocity = null,
        Vector2? facing = null,
        float collisionRadius = 0.5f,
        int currentHealth = 300,
        int maximumHealth = 700) =>
        new(
            entityId == 0 ? default : new WorldEntityId(entityId),
            new MonsterArchetypeId("starter_flyer_light"),
            new GroundPosition(10.0f, 20.0f),
            velocity ?? Vector2.Zero,
            facing ?? Vector2.UnitX,
            collisionRadius,
            behavior,
            targetId is { } value ? new WorldEntityId(value) : null,
            currentHealth,
            maximumHealth);

    private static DefeatedMonsterSnapshot CreateDefeated(ulong entityId, ulong defeatedAtTick) =>
        new(
            new WorldEntityId(entityId),
            new MonsterArchetypeId("starter_flyer_light"),
            new GroundPosition((float)entityId, (float)entityId + 1.0f),
            Vector2.UnitX,
            defeatedAtTick);
}
