using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Compatibility;
using Starfall.Protocol.Monsters;
using Starfall.Simulation.Combat;
using Starfall.Simulation.Movement;
using Starfall.World.Admission;
using Starfall.World.Combat;
using Starfall.World.Entities;
using Starfall.World.Lifecycle;
using Starfall.World.Monsters;

namespace Starfall.World.Tests;

public sealed class WorldMonsterExchangeTests
{
    private const long NowUnixMilliseconds = 1_800_000_000_000;

    [Fact]
    public void Initial_capture_maps_all_monsters_once_per_session_in_stable_order()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession first = Admit(runtime, 1);
        WorldGameplaySession second = Admit(runtime, 2);
        var exchange = new WorldMonsterExchange(runtime);

        IReadOnlyList<WorldMonsterSnapshotPublication> publications = exchange.CaptureSnapshots();

        Assert.Equal([first.SessionId, second.SessionId], publications.Select(static value => value.SessionId));
        Assert.All(publications, publication =>
        {
            BoundedMonsterSnapshot snapshot = Decode(publication);
            Assert.Equal(1UL, snapshot.Sequence.Value);
            Assert.Equal(0UL, snapshot.SimulationTick);
            Assert.Equal(10, snapshot.LiveMonsters.Length);
            Assert.Empty(snapshot.DefeatedMonsters);
            Assert.Equal(
                Enumerable.Range(1, 10).Select(static value => (ulong)value),
                snapshot.LiveMonsters.Select(static monster => monster.EntityId.Value));
            Assert.All(snapshot.LiveMonsters, static monster =>
            {
                Assert.Equal(MonsterBehaviorKind.Idle, monster.Behavior);
                Assert.Null(monster.TargetEntityId);
                Assert.Equal(monster.MaximumHealthUnits, monster.CurrentHealthUnits);
            });
        });
        Assert.Empty(exchange.CaptureSnapshots());

        runtime.Step();
        Assert.All(exchange.CaptureSnapshots(), publication =>
        {
            BoundedMonsterSnapshot snapshot = Decode(publication);
            Assert.Equal(2UL, snapshot.Sequence.Value);
            Assert.Equal(1UL, snapshot.SimulationTick);
        });
    }

    [Fact]
    public void Lethal_defeat_repeats_for_current_and_new_sessions_until_exact_slot_replenishes()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession first = Admit(runtime, 1);
        var exchange = new WorldMonsterExchange(runtime);
        _ = Assert.Single(exchange.CaptureSnapshots());
        MovePlayerTo(runtime, first.PlayerEntityId, new GroundPoint(100.0f, 70.0f));
        MovePlayerTo(runtime, first.PlayerEntityId, new GroundPoint(70.0f, 65.0f));
        WorldMonsterState original = Assert.Single(
            runtime.Monsters,
            static monster => monster.SpawnId == "spawn_easy_03");

        DefeatWithBasicArrow(runtime, first.PlayerEntityId, original.EntityId);
        ulong defeatedAtTick = runtime.CurrentTick;
        WorldDefeatedMonsterState retained = Assert.Single(runtime.DefeatedMonsters);

        BoundedMonsterSnapshot defeated = Decode(Assert.Single(exchange.CaptureSnapshots()));
        Assert.Equal(9, defeated.LiveMonsters.Length);
        DefeatedMonsterSnapshot tombstone = Assert.Single(defeated.DefeatedMonsters);
        Assert.Equal(original.EntityId.Value, tombstone.EntityId.Value);
        Assert.Equal(original.ArchetypeId, tombstone.ArchetypeId.Value);
        Assert.Equal(retained.LastPosition.XMetres, tombstone.LastPosition.XMetres);
        Assert.Equal(retained.LastPosition.ZMetres, tombstone.LastPosition.ZMetres);
        Assert.Equal(retained.LastFacing, tombstone.LastFacing);
        Assert.Equal(defeatedAtTick, tombstone.DefeatedAtTick);

        runtime.Step();
        DefeatedMonsterSnapshot repeated = Assert.Single(
            Decode(Assert.Single(exchange.CaptureSnapshots())).DefeatedMonsters);
        Assert.Equal(tombstone.EntityId, repeated.EntityId);
        Assert.Equal(tombstone.DefeatedAtTick, repeated.DefeatedAtTick);

        WorldGameplaySession second = Admit(runtime, 2);
        WorldMonsterSnapshotPublication joined = Assert.Single(
            exchange.CaptureSnapshots(),
            publication => publication.SessionId == second.SessionId);
        Assert.Single(Decode(joined).DefeatedMonsters);

        while (runtime.CurrentTick < defeatedAtTick + Draft0CampPolicyCatalog.ReplenishmentDelayTicks)
            runtime.Step();

        WorldMonsterState replacement = Assert.Single(
            runtime.Monsters,
            static monster => monster.SpawnId == "spawn_easy_03");
        Assert.NotEqual(original.EntityId, replacement.EntityId);
        Assert.Empty(runtime.DefeatedMonsters);
        BoundedMonsterSnapshot replenished = Decode(
            Assert.Single(exchange.CaptureSnapshots(), publication => publication.SessionId == first.SessionId));
        Assert.Empty(replenished.DefeatedMonsters);
        Assert.Contains(replenished.LiveMonsters, monster => monster.EntityId.Value == replacement.EntityId.Value);
    }

    [Fact]
    public void Capture_maps_authoritative_behavior_target_transform_and_damaged_health()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        WorldGameplaySession session = Admit(runtime, 1);
        var exchange = new WorldMonsterExchange(runtime);
        _ = Assert.Single(exchange.CaptureSnapshots());
        MovePlayerTo(runtime, session.PlayerEntityId, new GroundPoint(100.0f, 70.0f));
        MovePlayerTo(runtime, session.PlayerEntityId, new GroundPoint(70.0f, 65.0f));
        WorldMonsterState target = Assert.Single(
            runtime.Monsters,
            static monster => monster.SpawnId == "spawn_easy_03");
        BasicArrowStartEvaluation start = runtime.SubmitBasicArrow(
            new BasicArrowIntent("basic_arrow", session.PlayerEntityId, target.EntityId));
        PendingBasicArrow pending = Assert.IsType<PendingBasicArrow>(start.PendingAction);
        while (runtime.CurrentTick < pending.ResolveTick)
            runtime.Step();

        BoundedMonsterSnapshot snapshot = Decode(Assert.Single(exchange.CaptureSnapshots()));
        Assert.Contains(snapshot.LiveMonsters, static monster =>
            monster.Behavior is MonsterBehaviorKind.Pursuing or MonsterBehaviorKind.Attacking);
        foreach (WorldMonsterState authoritative in runtime.Monsters)
        {
            LiveMonsterSnapshot mapped = Assert.Single(
                snapshot.LiveMonsters,
                monster => monster.EntityId.Value == authoritative.EntityId.Value);
            Assert.Equal(authoritative.ArchetypeId, mapped.ArchetypeId.Value);
            Assert.Equal(authoritative.Position.XMetres, mapped.Position.XMetres);
            Assert.Equal(authoritative.Position.ZMetres, mapped.Position.ZMetres);
            Assert.Equal(authoritative.Behavior.VelocityMetresPerSecond, mapped.VelocityMetresPerSecond);
            Assert.Equal(authoritative.Behavior.Facing, mapped.Facing);
            Assert.Equal(authoritative.Behavior.CollisionRadiusMetres, mapped.CollisionRadiusMetres);
            Assert.Equal(MapBehavior(authoritative.Behavior.Mode), mapped.Behavior);
            Assert.Equal(authoritative.Behavior.TargetEntityId?.Value, mapped.TargetEntityId?.Value);
            Assert.Equal(authoritative.HealthUnits, mapped.CurrentHealthUnits);
            Assert.Equal(authoritative.MaximumHealthUnits, mapped.MaximumHealthUnits);
        }

        LiveMonsterSnapshot damaged = Assert.Single(
            snapshot.LiveMonsters,
            monster => monster.EntityId.Value == target.EntityId.Value);
        Assert.Equal(400, damaged.CurrentHealthUnits);
        Assert.Equal(700, damaged.MaximumHealthUnits);
    }

    [Fact]
    public void Technical_removal_omits_live_state_without_fabricating_defeat()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        _ = Admit(runtime, 1);
        var exchange = new WorldMonsterExchange(runtime);
        _ = Assert.Single(exchange.CaptureSnapshots());
        WorldMonsterState removed = runtime.Monsters[0];

        Assert.True(runtime.RemoveMonster(removed.EntityId));
        runtime.Step();

        BoundedMonsterSnapshot snapshot = Decode(Assert.Single(exchange.CaptureSnapshots()));
        Assert.Equal(9, snapshot.LiveMonsters.Length);
        Assert.Empty(snapshot.DefeatedMonsters);
        Assert.Empty(runtime.DefeatedMonsters);
    }

    [Fact]
    public void Draining_continues_exchange_and_stop_clears_session_publication()
    {
        WorldChannelRuntime runtime = CreateRuntime();
        _ = Admit(runtime, 1);
        var exchange = new WorldMonsterExchange(runtime);
        Assert.Single(exchange.CaptureSnapshots());

        runtime.BeginDrain();
        runtime.Step();
        Assert.Single(exchange.CaptureSnapshots());

        runtime.Stop();
        Assert.Empty(exchange.CaptureSnapshots());
    }

    [Fact]
    public void Snapshot_sequence_allocates_the_final_value_once_then_fails_explicitly()
    {
        var allocator = new MonsterSnapshotSequenceAllocator(ulong.MaxValue);

        Assert.Equal(ulong.MaxValue, allocator.Allocate().Value);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => allocator.Allocate());
        Assert.Equal("The monster snapshot sequence space is exhausted.", exception.Message);
    }

    private static void DefeatWithBasicArrow(
        WorldChannelRuntime runtime,
        Starfall.Simulation.Entities.WorldEntityId actorId,
        Starfall.Simulation.Entities.WorldEntityId targetId)
    {
        for (var shot = 0; shot < 3; shot++)
        {
            BasicArrowStartEvaluation start = runtime.SubmitBasicArrow(
                new BasicArrowIntent("basic_arrow", actorId, targetId));
            Assert.Equal(BasicArrowStartDisposition.Accepted, start.Disposition);
            PendingBasicArrow pending = Assert.IsType<PendingBasicArrow>(start.PendingAction);
            while (runtime.CurrentTick < pending.ResolveTick)
                runtime.Step();
            BasicArrowResolution resolution = Assert.Single(runtime.LastBasicArrowResolutions);
            Assert.Equal(BasicArrowResolutionDisposition.Resolved, resolution.Disposition);
            if (shot < 2)
            {
                while (runtime.CurrentTick < pending.NextAllowedStartTick)
                    runtime.Step();
            }
        }
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

        throw new InvalidOperationException("Connected player did not reach the combat fixture position.");
    }

    private static WorldChannelRuntime CreateRuntime()
    {
        var runtime = new WorldChannelRuntime(
            new WorldId("world_1"),
            new ChannelId("channel_1"),
            new WorldInstanceId(Guid.Parse("50000000-0000-0000-0000-000000000001")),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
        runtime.Start();
        return runtime;
    }

    private static WorldGameplaySession Admit(WorldChannelRuntime runtime, int ordinal)
    {
        var claims = new WorldJoinTicketClaims(
            new JoinTicketId(Guid.Parse($"10000000-0000-0000-0000-{ordinal:D12}")),
            new AccountId(Guid.Parse($"20000000-0000-0000-0000-{ordinal:D12}")),
            new CharacterId(Guid.Parse($"30000000-0000-0000-0000-{ordinal:D12}")),
            runtime.WorldId,
            runtime.ChannelId,
            runtime.InstanceId,
            NowUnixMilliseconds,
            NowUnixMilliseconds + 30_000);
        WorldJoinAdmissionOutcome outcome = runtime.ConsumeTicketAndCreateSession(
            claims,
            StarfallGameplayProtocol.CurrentVersion,
            NowUnixMilliseconds);
        GameplaySessionId sessionId = Assert.IsType<WorldJoinAccepted>(outcome.Accepted).SessionId;
        Assert.True(runtime.TryGetGameplaySession(sessionId, out WorldGameplaySession? session));
        return Assert.IsType<WorldGameplaySession>(session);
    }

    private static BoundedMonsterSnapshot Decode(WorldMonsterSnapshotPublication publication)
    {
        Assert.True(BoundedMonsterSnapshotCodec.TryDecode(
            publication.Payload,
            out BoundedMonsterSnapshot? snapshot));
        return Assert.IsType<BoundedMonsterSnapshot>(snapshot);
    }

    private static MonsterBehaviorKind MapBehavior(
        Starfall.Simulation.Monsters.Draft0MonsterBehaviorMode mode) =>
        mode switch
        {
            Starfall.Simulation.Monsters.Draft0MonsterBehaviorMode.Idle => MonsterBehaviorKind.Idle,
            Starfall.Simulation.Monsters.Draft0MonsterBehaviorMode.Pursuing => MonsterBehaviorKind.Pursuing,
            Starfall.Simulation.Monsters.Draft0MonsterBehaviorMode.Attacking => MonsterBehaviorKind.Attacking,
            Starfall.Simulation.Monsters.Draft0MonsterBehaviorMode.Returning => MonsterBehaviorKind.Returning,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
}
