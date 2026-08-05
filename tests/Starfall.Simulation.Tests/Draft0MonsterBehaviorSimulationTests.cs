using System.Collections.Immutable;
using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Monsters;

namespace Starfall.Simulation.Tests;

public sealed class Draft0MonsterBehaviorSimulationTests
{
    private const float Tolerance = 1e-4f;
    private static readonly WorldEntityId LightId = new(1);
    private static readonly WorldEntityId HeavyId = new(2);
    private static readonly WorldEntityId PlayerId = new(100);

    [Fact]
    public void First_playable_tunings_preserve_the_approved_readable_contrast()
    {
        Draft0MonsterBehaviorTuning light = Draft0MonsterBehaviorTunings.FirstPlayable.GetRequired(
            "starter_flyer_light");
        Draft0MonsterBehaviorTuning heavy = Draft0MonsterBehaviorTunings.FirstPlayable.GetRequired(
            "starter_flyer_heavy");

        AssertTuning(light, 0.45f, 2.5f, 10.0f, 1.25f, 100, 60);
        AssertTuning(heavy, 0.65f, 1.8f, 12.0f, 1.5f, 200, 90);
    }

    [Fact]
    public void Tunings_are_immutable_validated_inputs_instead_of_algorithm_constants()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTuning(collisionRadius: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTuning(speed: 0.0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTuning(awareness: float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTuning(attackRange: 0.25f));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTuning(damage: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTuning(cadence: 0));
        Assert.Throws<ArgumentException>(() => new Draft0MonsterBehaviorTuningCatalog(
        [
            CreateTuning(),
            CreateTuning(),
        ]));
        Assert.Throws<ArgumentException>(() => new Draft0MonsterBehaviorTuningCatalog(
            default(ImmutableArray<Draft0MonsterBehaviorTuning>)));
    }

    [Fact]
    public void Target_selection_uses_distance_then_entity_identity_regardless_of_input_order()
    {
        using var first = CreateSimulation();
        using var second = CreateSimulation();
        RegisterLight(first);
        RegisterLight(second);
        var higher = new Draft0MonsterPlayerTarget(new WorldEntityId(102), new GroundPoint(60.0f, 65.0f));
        var lower = new Draft0MonsterPlayerTarget(new WorldEntityId(101), new GroundPoint(50.0f, 65.0f));

        Draft0MonsterBehaviorState firstState = Assert.Single(first.Step([higher, lower], 1).Monsters);
        Draft0MonsterBehaviorState secondState = Assert.Single(second.Step([lower, higher], 1).Monsters);

        Assert.Equal(new WorldEntityId(101), firstState.TargetEntityId);
        Assert.Equal(firstState, secondState);
        Assert.Equal(Draft0MonsterBehaviorMode.Pursuing, firstState.Mode);
    }

    [Fact]
    public void Awareness_requires_the_player_to_be_inside_the_owning_camp()
    {
        using var simulation = CreateSimulation();
        RegisterLight(simulation);

        Draft0MonsterBehaviorState outsideCamp = Assert.Single(simulation.Step(
        [
            new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(45.0f, 65.0f)),
        ], 1).Monsters);
        Draft0MonsterBehaviorState outsideAwareness = Assert.Single(simulation.Step(
        [
            new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(65.0f, 75.0f)),
        ], 2).Monsters);

        Assert.Equal(Draft0MonsterBehaviorMode.Idle, outsideCamp.Mode);
        Assert.Null(outsideCamp.TargetEntityId);
        Assert.Equal(Draft0MonsterBehaviorMode.Idle, outsideAwareness.Mode);
        Assert.Null(outsideAwareness.TargetEntityId);
    }

    [Fact]
    public void Retained_target_can_cross_awareness_but_disengages_at_the_camp_boundary()
    {
        using var simulation = CreateSimulation();
        GroundPoint home = RegisterLight(simulation).Home;
        var acquired = new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(60.0f, 65.0f));
        var beyondAwarenessInsideCamp = new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(70.0f, 70.0f));
        var outsideCamp = new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(76.0f, 70.0f));

        Assert.Equal(PlayerId, Assert.Single(simulation.Step([acquired], 1).Monsters).TargetEntityId);
        Draft0MonsterBehaviorState retained = Assert.Single(simulation.Step([beyondAwarenessInsideCamp], 2).Monsters);
        Draft0MonsterBehaviorState returning = Assert.Single(simulation.Step([outsideCamp], 3).Monsters);

        Assert.Equal(PlayerId, retained.TargetEntityId);
        Assert.Equal(Draft0MonsterBehaviorMode.Pursuing, retained.Mode);
        Assert.Null(returning.TargetEntityId);
        Assert.Equal(Draft0MonsterBehaviorMode.Returning, returning.Mode);
        Assert.True(Vector2.Distance(ToPlane(returning.Position), ToPlane(home)) <
            Vector2.Distance(ToPlane(retained.Position), ToPlane(home)));
    }

    [Fact]
    public void Return_reaches_exact_home_before_reacquiring_on_a_later_tick()
    {
        using var simulation = CreateSimulation();
        GroundPoint home = RegisterLight(simulation).Home;
        var player = new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(60.0f, 65.0f));
        simulation.Step([player], 1);

        Draft0MonsterBehaviorState state = default;
        for (ulong tick = 2; tick < 240; tick++)
        {
            state = Assert.Single(simulation.Step([], tick).Monsters);
            if (state.Position == home && state.Mode == Draft0MonsterBehaviorMode.Idle)
                break;
        }

        Assert.Equal(home, state.Position);
        Assert.Equal(Draft0MonsterBehaviorMode.Idle, state.Mode);
        Assert.Null(state.TargetEntityId);

        Draft0MonsterBehaviorState reacquired = Assert.Single(simulation.Step([player], 240).Monsters);
        Assert.Equal(PlayerId, reacquired.TargetEntityId);
    }

    [Fact]
    public void Attack_resolves_immediately_in_range_then_obeys_exact_cadence()
    {
        using var simulation = CreateSimulation();
        RegisterLight(simulation);
        var player = new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(56.0f, 65.0f));

        Draft0MonsterBehaviorStep first = simulation.Step([player], 1);
        Draft0MonsterAttackResolution firstAttack = Assert.Single(first.Attacks);
        Assert.Equal(LightId, firstAttack.AttackerEntityId);
        Assert.Equal(PlayerId, firstAttack.TargetEntityId);
        Assert.Equal(1UL, firstAttack.ResolvedAtTick);
        Assert.Equal(100, firstAttack.RequestedDamageUnits);
        Assert.Equal(61UL, Assert.Single(first.Monsters).NextAllowedAttackTick);

        for (ulong tick = 2; tick < 61; tick++)
            Assert.Empty(simulation.Step([player], tick).Attacks);
        Assert.Single(simulation.Step([player], 61).Attacks);
    }

    [Fact]
    public void Attack_tick_overflow_fails_explicitly()
    {
        using var simulation = CreateSimulation();
        RegisterLight(simulation);
        var player = new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(56.0f, 65.0f));

        Assert.Throws<OverflowException>(() => simulation.Step([player], ulong.MaxValue));
    }

    [Fact]
    public void Pursuit_uses_fixed_tick_speed_and_never_crosses_static_proxy_geometry()
    {
        using var simulation = CreateSimulation();
        GroundPoint home = new(95.0f, 122.0f);
        simulation.RegisterMonster(
            LightId,
            "camp_mixed",
            "spawn_mixed_01",
            "starter_flyer_light",
            home);
        var player = new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(102.0f, 128.0f));

        Draft0MonsterBehaviorState first = Assert.Single(simulation.Step([player], 1).Monsters);
        Assert.Equal(2.5f, first.VelocityMetresPerSecond.Length(), Tolerance);

        Draft0MonsterBehaviorState state = first;
        for (ulong tick = 2; tick <= 300; tick++)
            state = Assert.Single(simulation.Step([player], tick).Monsters);

        Draft0ProxyBlock divider = Assert.Single(
            Draft0GrayboxCatalog.FirstPlayable.Proxies,
            static proxy => proxy.Id == "mixed_divider");
        float nearestX = Math.Clamp(
            state.Position.XMetres,
            divider.Footprint.Minimum.XMetres,
            divider.Footprint.Maximum.XMetres);
        float nearestZ = Math.Clamp(
            state.Position.ZMetres,
            divider.Footprint.Minimum.ZMetres,
            divider.Footprint.Maximum.ZMetres);
        float deltaX = state.Position.XMetres - nearestX;
        float deltaZ = state.Position.ZMetres - nearestZ;
        Assert.True(
            (deltaX * deltaX) + (deltaZ * deltaZ) >=
            (state.CollisionRadiusMetres * state.CollisionRadiusMetres) - Tolerance,
            $"Monster ended at ({state.Position.XMetres}, {state.Position.ZMetres}).");
        Assert.Equal(Draft0MonsterBehaviorMode.Pursuing, state.Mode);
    }

    [Fact]
    public void Radius_inset_keeps_a_monster_center_inside_its_circular_camp()
    {
        using var simulation = CreateSimulation();
        RegisterLight(simulation);
        var boundaryPlayer = new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(75.0f, 70.0f));

        Draft0MonsterBehaviorState state = default;
        for (ulong tick = 1; tick <= 600; tick++)
            state = Assert.Single(simulation.Step([boundaryPlayer], tick).Monsters);

        Draft0CampLayout camp = Draft0GrayboxCatalog.FirstPlayable.Branches[0].Camp;
        float distance = Vector2.Distance(ToPlane(state.Position), ToPlane(camp.Center));
        Assert.True(distance <= camp.RadiusMetres - state.CollisionRadiusMetres + Tolerance);
    }

    [Fact]
    public void Step_returns_immutable_entity_order_and_rejects_conflicting_inputs()
    {
        using var simulation = CreateSimulation();
        simulation.RegisterMonster(
            HeavyId,
            "camp_hard",
            "spawn_hard_01",
            "starter_flyer_heavy",
            new GroundPoint(140.0f, 104.0f));
        RegisterLight(simulation);

        Draft0MonsterBehaviorStep step = simulation.Step([], 1);
        Assert.Equal([LightId, HeavyId], step.Monsters.Select(static state => state.EntityId));
        Assert.Throws<ArgumentException>(() => simulation.Step(
        [
            new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(60.0f, 65.0f)),
            new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(61.0f, 65.0f)),
        ], 2));
        Assert.Throws<ArgumentException>(() => simulation.Step(
        [
            new Draft0MonsterPlayerTarget(LightId, new GroundPoint(60.0f, 65.0f)),
        ], 2));
        Assert.Throws<ArgumentException>(() => simulation.Step(
            default(ImmutableArray<Draft0MonsterPlayerTarget>),
            2));
    }

    [Fact]
    public void Repeated_fixed_tick_inputs_produce_identical_states_and_attacks()
    {
        using var first = CreateSimulation();
        using var second = CreateSimulation();
        RegisterLight(first);
        RegisterLight(second);
        var player = new Draft0MonsterPlayerTarget(PlayerId, new GroundPoint(60.0f, 65.0f));

        for (ulong tick = 1; tick <= 180; tick++)
        {
            Draft0MonsterBehaviorStep firstStep = first.Step([player], tick);
            Draft0MonsterBehaviorStep secondStep = second.Step([player], tick);
            Assert.Equal(firstStep.Monsters.ToArray(), secondStep.Monsters.ToArray());
            Assert.Equal(firstStep.Attacks.ToArray(), secondStep.Attacks.ToArray());
        }
    }

    private static Draft0MonsterBehaviorSimulation CreateSimulation() =>
        new(
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0MonsterBehaviorTunings.FirstPlayable);

    private static Draft0MonsterBehaviorState RegisterLight(Draft0MonsterBehaviorSimulation simulation) =>
        simulation.RegisterMonster(
            LightId,
            "camp_easy",
            "spawn_easy_01",
            "starter_flyer_light",
            new GroundPoint(55.0f, 65.0f));

    private static Draft0MonsterBehaviorTuning CreateTuning(
        float collisionRadius = 0.45f,
        float speed = 2.5f,
        float awareness = 10.0f,
        float attackRange = 1.25f,
        int damage = 100,
        ulong cadence = 60) =>
        new(
            "starter_flyer_light",
            collisionRadius,
            speed,
            awareness,
            attackRange,
            damage,
            cadence);

    private static void AssertTuning(
        Draft0MonsterBehaviorTuning tuning,
        float collisionRadius,
        float speed,
        float awareness,
        float attackRange,
        int damage,
        ulong cadence)
    {
        Assert.Equal(collisionRadius, tuning.CollisionRadiusMetres);
        Assert.Equal(speed, tuning.MovementSpeedMetresPerSecond);
        Assert.Equal(awareness, tuning.AwarenessRadiusMetres);
        Assert.Equal(attackRange, tuning.AttackRangeMetres);
        Assert.Equal(damage, tuning.OutgoingDamageUnits);
        Assert.Equal(cadence, tuning.AttackCadenceTicks);
    }

    private static Vector2 ToPlane(GroundPoint point) =>
        new(point.XMetres, point.ZMetres);
}
